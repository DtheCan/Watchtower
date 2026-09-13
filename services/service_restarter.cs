using System.Diagnostics;
using Renci.SshNet;

namespace watchtower.services;

public class ServiceRestarter
{
    private readonly LogingService _logger;
    private readonly TelegramNotifier _telegram;

    public ServiceRestarter(LogingService logger, TelegramNotifier telegram)
    {
        _logger = logger;
        _telegram = telegram;
    }

    public async Task RestartServiceAsync(ServiceConfig service)
    {
        // Для логов — с хостом и портом
        var logId = $"{service.Name} ({service.Host}:{service.Port})";
        // Для Telegram — только имя сервиса
        var tgId = service.Name;

        _logger.Info($"Restarting {logId}...");
        await _telegram.SendMessageAsync($"🔄 Сервис «{tgId}» — DOWN. Перезапуск...");

        try
        {
            bool success = false;

            bool useSsh = !string.IsNullOrEmpty(service.Host)
                          && service.Host != "localhost"
                          && service.Host != "127.0.0.1";

            if (service.Type == "port")
            {
                _logger.Warning($"Cannot auto-restart {logId}: type=port (unknown unit).");
                await _telegram.SendMessageAsync($"⚠️ Сервис «{tgId}» — не могу перезапустить: тип 'port', не задан systemd-юнит.");
                return;
            }

            if (useSsh)
            {
                _logger.Info($"Restarting via SSH on {service.Host} ({service.Name}:{service.Port})");

                using var client = new SshClient(service.Host, service.SshUser, service.SshPassword);
                client.Connect();

                if (!client.IsConnected)
                {
                    _logger.Error($"Failed to connect to {service.Host} for {logId}");
                    await _telegram.SendMessageAsync($"❌ Сервис «{tgId}» — не удалось подключиться к серверу.");
                    return;
                }

                string command = service.Type switch
                {
                    "systemd" => $"systemctl restart {service.Name}",
                    "process" => $"pkill -f {service.Name} || true && {service.Name} &",
                    _ => $"systemctl restart {service.Name}"
                };

                var result = client.RunCommand(command);
                client.Disconnect();

                success = result.ExitStatus == 0;
                _logger.Info($"SSH restart {logId} exit code: {result.ExitStatus}");
            }
            else
            {
                if (OperatingSystem.IsLinux())
                {
                    if (service.Type == "systemd")
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "systemctl",
                            Arguments = $"restart {service.Name}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            await proc.WaitForExitAsync();
                            success = proc.ExitCode == 0;
                        }
                    }
                }
                else
                {
                    _logger.Warning($"Cannot restart locally on Windows for {logId}.");
                    await _telegram.SendMessageAsync($"⚠️ Сервис «{tgId}» — локальный рестарт на Windows невозможен.");
                    return;
                }
            }

            if (success)
            {
                _logger.Info($"{logId} restarted successfully.");
                await _telegram.SendMessageAsync($"✅ Сервис «{tgId}» — перезапущен.");

                await Task.Delay(5000);
                bool isRunning = await CheckServiceAfterRestart(service);
                if (isRunning)
                {
                    _logger.Info($"{logId} is running after restart.");
                    await _telegram.SendMessageAsync($"🟢 Сервис «{tgId}» — работает после перезапуска.");
                }
                else
                {
                    _logger.Warning($"{logId} still not running after restart!");
                    await _telegram.SendMessageAsync($"⚠️ Сервис «{tgId}» — всё ещё не работает после перезапуска!");
                }
            }
            else
            {
                _logger.Error($"Failed to restart {logId}.");
                await _telegram.SendMessageAsync($"❌ Сервис «{tgId}» — не удалось перезапустить.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Restart error for {logId}: {ex.Message}");
            await _telegram.SendMessageAsync($"⚠️ Сервис «{tgId}» — ошибка перезапуска: {ex.Message}");
        }
    }

    private async Task<bool> CheckServiceAfterRestart(ServiceConfig service)
    {
        try
        {
            bool useSsh = !string.IsNullOrEmpty(service.Host)
                          && service.Host != "localhost"
                          && service.Host != "127.0.0.1";

            if (!useSsh) return true;

            using var client = new SshClient(service.Host, service.SshUser, service.SshPassword);
            client.Connect();

            string command = $"ss -ltn 2>/dev/null | grep -q ':{service.Port} ' && echo RUNNING || echo STOPPED";
            var result = client.RunCommand(command);
            client.Disconnect();

            return result.Result?.Trim() == "RUNNING";
        }
        catch
        {
            return false;
        }
    }
}