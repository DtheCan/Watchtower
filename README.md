## Watchtower - Мониторинг и автоматический перезапуск сервисов

### Установка 
Файлы из релиза протативный расчитанный для OC Windows
Дотстаочно просто положить файлы в нужную папку и запустить
Не забдуьте создать конфигурационный файл рядом с файлами


### Настройка Telegram бота (опционально)
#### Создать бота:
Открыть Telegram, найти @BotFather
Отправить /newbot
Выбрать имя: Watchtower Monitor
Выбрать username: watchtower_monitor_bot
Скопировать полученный токен

### Получить Chat ID:
Добавить бота в свой чат/канал
Отправить любое сообщение боту
Перейти по ссылке: https://api.telegram.org/bot<ВАШ_ТОКЕН>/getUpdates
Найти "chat":{"id":123456789} в ответе

### Настройка конфигурации
Отредактировать /opt/wachtower/wachtower_configuration.json

```json
{
  "CheckIntervalSeconds": 30,
  "LogPath": "/var/log/wachtower-logs",
  "Services": [
    {
      "Name": "nginx",
      "Host": "192.168.1.10",
      "Type": "systemd",
      "SshUser": "root",
      "SshPassword": "your_password"
    },
    {
      "Name": "mysql",
      "Host": "localhost",
      "Type": "systemd"
    }
  ],
  "Telegram": {
    "BotToken": "ВАШ_ТОКЕН_БОТА",
    "ChatId": "ВАШ_CHAT_ID"
  }
}
```
Параметры:
- CheckIntervalSeconds - интервал проверки в секундах
- LogPath - путь для хранения логов
- Services - список сервисов для мониторинга
    - Name - имя сервиса (для systemd) или имя процесса
    - Host - адрес сервера (для локального укажите "localhost")
    - Type - тип: systemd или process
- Telegram - настройки Telegram бота

### Логи
Логи созраняются:
```
/var/log/wachtower-logs/
├── health_check_service/
├── service_restarter/
└── telegram_notifier/
```

для Виндовс dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
для Линукс dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

