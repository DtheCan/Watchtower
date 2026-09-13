using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Renci.SshNet;

namespace watchtower.services;

public class HealthCheckService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly LogingService _logger;
    private readonly ServiceRestarter _restarter;
    private readonly TelegramNotifier _telegram;
    private readonly int _checkInterval;
    private readonly int _unreachableCheckInterval;
    private readonly List<ServiceConfig> _services;

    // host -> последнее известное состояние доступности
    private readonly ConcurrentDictionary<string, bool> _hostReachable = new();

    public HealthCheckService(
        IConfiguration config,
        LogingService logger,
        ServiceRestarter restarter,
        TelegramNotifier telegram)
    {
        _config = config;
        _logger = logger;
        _restarter = restarter;
        _telegram = telegram;

        _checkInterval = _config.GetValue<int>("CheckIntervalSeconds", 30);
        _unreachableCheckInterval = _config.GetValue<int>("UnreachableCheckIntervalSeconds", 120);
        _services = _config.GetSection("Services").Get<List<ServiceConfig>>() ?? new List<ServiceConfig>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Info("HealthCheckService started on Windows.");
        _logger.Info($"Monitoring {_services.Count} services...");
        _logger.Info($"CheckInterval={_checkInterval}s, UnreachableCheckInterval={_unreachableCheckInterval}s");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Разбиваем сервисы на "доступные" и "недоступные" хосты,
            // чтобы опрашивать их с разной периодичностью.
            var now = DateTime.UtcNow;

            var reachableTasks = _services
                .Where(s => _hostReachable.GetValueOrDefault(HostKey(s), true))
                .Select(s => CheckServiceAsync(s, stoppingToken));

            var unreachableTasks = _services
                .Where(s => !_hostReachable.GetValueOrDefault(HostKey(s), true))
                .Select(s => CheckServiceAsync(s, stoppingToken, isSlowProbe: true));

            try
            {
                await Task.WhenAll(reachableTasks.Concat(unreachableTasks));
            }
            catch (Exception ex)
            {
                _logger.Error($"Iteration error: {ex.Message}");
            }

            // Ждём минимум из двух интервалов; недоступные хосты будут
            // перепроверены только на следующей "медленной" итерации.
            await Task.Delay(TimeSpan.FromSeconds(_checkInterval), stoppingToken);
        }

        _logger.Info("HealthCheckService stopped.");
    }

    private static string HostKey(ServiceConfig s) => $"{s.Host}:{s.Port}";

    private async Task CheckServiceAsync(ServiceConfig service, CancellationToken ct, bool isSlowProbe = false)
    {
        // Для логов — с хостом и портом
        var logId = $"{service.Name} ({service.Host}:{service.Port})";
        // Для Telegram — только имя сервиса
        var tgId = service.Name;

        try
        {
            if (!isSlowProbe && !_hostReachable.GetValueOrDefault(HostKey(service), true))
                return;

            if (isSlowProbe)
            {
                await Task.Delay(TimeSpan.FromSeconds(_unreachableCheckInterval), ct);
            }

            var (reachable, isRunning) = await ProbeServiceAsync(service);

            if (!reachable)
            {
                _hostReachable[HostKey(service)] = false;
                _logger.Warning($"[UNREACHABLE] {logId} — host is not reachable. Will retry in {_unreachableCheckInterval}s.");
                await _telegram.SendMessageAsync($"⚠️ Сервис «{tgId}» — сервер недоступен, повтор через {_unreachableCheckInterval}с.");
                return;
            }

            bool wasUnreachable = _hostReachable.TryGetValue(HostKey(service), out var prev) && !prev;
            _hostReachable[HostKey(service)] = true;

            if (wasUnreachable)
            {
                _logger.Info($"[RECOVERED] {logId} — host reachable again.");
                await _telegram.SendMessageAsync($"🟢 Сервис «{tgId}» — сервер снова доступен.");
            }

            if (!isRunning)
            {
                _logger.Warning($"[DOWN] {logId} — service is DOWN.");
                await _restarter.RestartServiceAsync(service);
            }
            else
            {
                _logger.Info($"[OK] {logId} — service is running.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[CHECK-FAILED] {logId} — {ex.Message}");
            _hostReachable[HostKey(service)] = false;
            await _telegram.SendMessageAsync($"⚠️ Сервис «{tgId}» — ошибка проверки: {ex.Message}");
        }
    }

    /// <summary>
    /// Возвращает (hostReachable, serviceRunning).
    /// hostReachable = удалось ли вообще достучаться до хоста (SSH-коннект).
    /// serviceRunning = слушает ли приложение порт.
    /// </summary>
    private async Task<(bool hostReachable, bool serviceRunning)> ProbeServiceAsync(ServiceConfig service)
    {
        bool useSsh = !string.IsNullOrEmpty(service.Host)
                      && service.Host != "localhost"
                      && service.Host != "127.0.0.1";

        if (OperatingSystem.IsWindows() && useSsh)
        {
            return await ProbeViaSshAsync(service);
        }

        if (OperatingSystem.IsLinux() && !useSsh)
        {
            return await ProbeLocalAsync(service);
        }

        _logger.Warning($"Cannot check {service.Name}: unsupported platform/host combination.");
        return (false, false);
    }

    private async Task<(bool, bool)> ProbeViaSshAsync(ServiceConfig service)
    {
        try
        {
            using var client = new SshClient(service.Host, service.SshUser, service.SshPassword);
            client.Connect();

            if (!client.IsConnected)
            {
                _logger.Error($"SSH connect failed: {service.Host} (service {service.Name}:{service.Port})");
                return (false, false);
            }

            // Проверяем, что порт слушается.
            // Используем ss (есть почти везде) + fallback на nc.
            string cmd = $"ss -ltn 2>/dev/null | grep -q ':{service.Port} ' && echo RUNNING || echo STOPPED";

            var result = client.RunCommand(cmd);
            client.Disconnect();

            bool isRunning = result.Result?.Trim() == "RUNNING";
            return (true, isRunning);
        }
        catch (Exception ex)
        {
            _logger.Error($"SSH probe error for {service.Name}:{service.Port}@{service.Host}: {ex.Message}");
            return (false, false);
        }
    }

    private async Task<(bool, bool)> ProbeLocalAsync(ServiceConfig service)
    {
        // Локально проверяем, слушается ли порт
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"ss -ltn 2>/dev/null | grep -q ':{service.Port} ' && echo RUNNING || echo STOPPED\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return (true, false);

            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            return (true, output.Trim() == "RUNNING");
        }
        catch (Exception ex)
        {
            _logger.Error($"Local probe error for {service.Name}:{service.Port}: {ex.Message}");
            return (true, false);
        }
    }
}

public class ServiceConfig
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Type { get; set; } = "port"; // "port" | "systemd" | "process"
    public string SshUser { get; set; } = string.Empty;
    public string SshPassword { get; set; } = string.Empty;

    public override string ToString() => $"{Name} ({Host}:{Port})";
}