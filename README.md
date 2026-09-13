## Watchtower - Мониторинг и контроль отказоустойчивости сервисов

### 1.Установка 
Склонируйте репозиторий в папку /opt/ комнадой
```bash
sudo git clone https://github.com/DtheCan/watchtower.git /opt/
```
Можно и в другую папку но тогда нужно создать папку watchtower с файлом wachtower_configuration.json.
Так чтобы итоговый путь получился: /opt/watchtower/wachtower_configuration.json

### 2.Настройка Telegram бота (опционально)
#### Создать бота:
Открыть Telegram, найти @BotFather
Отправить /newbot
Выбрать имя: <ВАШЕ_ИМЯ_БОТА>
Выбрать username: <ВАШ_ИМЯ_ПРОФИЛЯ_bot>
Скопировать полученный токен
И зайдя в чат с ботом запустить и отправить что ни будь в чат с ботом.

### 3.Получить Chat ID:
Перейти по ссылке: https://api.telegram.org/bot<ВАШ_ТОКЕН>/getUpdates
Найти "chat":{"id":123456789} в ответе
поле id будет вашим чат id

### 4.Настройка конфигурации
в файле /opt/wachtower/wachtower_configuration.json

```json
{
  "CheckIntervalSeconds": 5,
  "UnreachableCheckIntervalSeconds": 120,
  "HealthyNotifyCount": 2,
  "LogPath": "/var/log/watchtower",
  "Services": [
    {
      "Name": "nginx",
      "Host": "ваш айпи адрес сервера",
      "Port": 80,
      "SshUser": "root",
      "SshPassword": "ваш SSH-пароль"
    }
  ],
  "Telegram": {
    "BotToken": "ваш токен бота",
    "ChatId": "ваш чат ID"
  }
}
```
Параметры:
- CheckIntervalSeconds - интервал проверки в секундах
- UnreachableCheckIntervalSeconds - интервал проверки сервера если не удалось подключистя в секундах
- HealthyNotifyCount - количество отправки сообщений в Телеграм боте, после которой отправка сообщения прекращается
- LogPath - путь для хранения логов
- Services - список сервисов для мониторинга и контроля
    - Name - имя сервиса (для systemd) или имя процесса
    - Host - адрес сервера (для локального укажите "localhost")
    - Port - порт на котором развернут сервис
    - SshUser - имя пользователя на таргет сервере
    - SshPassword - SSH пароль на таргет сервере
- Telegram - настройки Telegram бота
    - BotToken - токен бота которого вы создали в @botfather в телеграме
    - ChatId - id чата в который бот отправляет сообщения

### 5.Логи
Логи сохраняются:
```
/var/log/wachtower-logs/
├── health_check_service/
├── service_restarter/
└── telegram_notifier/
```

Если хотите просто собрать проект и запустить без докер
то для этого вам нужно будет установить dotnet, склонировать репозиторий и запустить эти команды:
```для Виндовс 
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```
```для Линукс 
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
