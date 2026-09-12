## Watchtower - Мониторинг и автоматический перезапуск сервисов

### Установка 
Файлы из релиза протативный расчитанный для OC Windows
Дотстаочно просто положить файлы в нужную папку и запустить
Не забдуьте создать конфигурационный файл рядом с файлами


### Настройка Telegram бота (опционально)
#### Создать бота:
Открыть Telegram, найти @BotFather
Отправить /newbot
Выбрать имя: <ВАШЕ_ИМЯ_БОТА>
Выбрать username: <ВАШ_ИМЯ_ПРОФИЛЯbot>
Скопировать полученный токен

### Получить Chat ID:
Добавить бота в свой чат/канал
Отправить любое сообщение боту
Перейти по ссылке: https://api.telegram.org/bot<ВАШ_ТОКЕН>/getUpdates
Найти "chat":{"id":123456789} в ответе

### Настройка конфигурации
Создайте файл /opt/wachtower/wachtower_configuration.json

```json
{
  "CheckIntervalSeconds": 5,
  "UnreachableCheckIntervalSeconds": 120,
  "LogPath": "/var/log/watchtower",
  "Services": [
    {
      "Name": "nginx",
      "Host": "ваш айпи адрес сервера",
      "Port": 80,
      "Type": "port",
      "SshUser": "root",
      "SshPassword": "ваш SSH-пароль"
    }
  ],
  "Telegram": {
    "BotToken": "Ваш тг-токен-бота",
    "ChatId": "ваш чат айди"
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
Логи сохраняются:
```
/var/log/wachtower-logs/
├── health_check_service/
├── service_restarter/
└── telegram_notifier/
```

для Виндовс dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
для Линукс dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

