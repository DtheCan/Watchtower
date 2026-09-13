namespace watchtower.services;

public class ServiceRestarter
{
    private readonly LogingService _logger;
    private readonly TelegramNotifier _telegram;
    private readonly ServiceProbe _probe;

    public ServiceRestarter(LogingService logger, TelegramNotifier telegram, ServiceProbe probe)
    {
        _logger = logger;
        _telegram = telegram;
        _probe = probe;
    }

    public async Task RestartServiceAsync(ServiceConfig service)
    {
        var logId = $"{service.Name} ({service.Host}:{service.Port})";
        var tgId = service.Name;

        _logger.Info($"Restarting {logId}...");
        await _telegram.SendMessageAsync($"🔄 Сервис «{tgId}» — DOWN. Попытка перезапуска...");

        try
        {
            bool success = await _probe.RestartAsync(service);

            if (success)
            {
                _logger.Info($"{logId} restart command executed successfully.");
                await _telegram.SendMessageAsync($"✅ Сервис «{tgId}» — команда перезапуска выполнена.");

                await Task.Delay(5000);

                var (reachable, isRunning) = await _probe.CheckAsync(service);
                if (reachable && isRunning)
                {
                    _logger.Info($"{logId} is running after restart.");
                    await _telegram.SendMessageAsync($"🟢 Сервис «{tgId}» — работает в штатном режиме после перезапуска.");
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
}