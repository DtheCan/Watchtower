## Watchtower - Мониторинг и контроль отказоустойчивости сервисов

### 1.Установка 
Склонируйте репозиторий в папку /opt/ комнадой
```bash
sudo git clone https://github.com/DtheCan/watchtower.git /opt/
```

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
Измените параметры:
Name
Host
Port
SshUser
SshPassword
BotToken
BotToken
На соответствующие вашим
```json
{
  "CheckIntervalSeconds": 5,
  "UnreachableCheckIntervalSeconds": 120,
  "LogPath": "/var/log/watchtower",
  "Services": [
    {
      "Name": "Название вашего сервиса",
      "Host": "Ваш айпи адрес сервера",
      "Port": "порт к примеру ( 80 ) без двойных ковычек",
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
- UnreachableCheckIntervalSeconds - интервал проверки сервера если не удалось подключистя
- LogPath - путь для хранения логов
- Services - список сервисов для мониторинга
    - Name - имя сервиса (для systemd) или имя процесса
    - Host - адрес сервера (для локального укажите "localhost")
    - Type - способ перезапуска (systemd / process / port)
- Telegram - настройки Telegram бота

Параметр в Type:
1. "systemd"
Что означает: сервис управляется через systemd — стандартный менеджер служб в Linux.
У него есть unit-файл (например, nginx.service), и им можно управлять командами systemctl start/stop/restart/status.

2. "process"
Что означает: сервис — это обычный процесс, запущенный вручную (не через systemd).
У него нет unit-файла, но есть имя процесса, по которому его можно найти.
Когда использовать: для самописных приложений, которые запускаются вручную или скриптом, без systemd.

3. "port"
Что означает: «я знаю только, что сервис должен слушать порт, но не знаю, как его перезапускать».
То есть ничего не перезапускает — просто пишет предупреждение в лог и в Telegram.

Ниже есть таблицы для популярных сервисов

### Логи
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

systemd — основной вариант (используй по умолчанию)
Подходит для всего, что установлено через apt / yum / dnf и управляется systemd. Name = имя unit-файла (без .service).

Категория	Сервис	Name в конфиге	Порт для проверки
Веб-сервер	Nginx	nginx	80 / 443
Веб-сервер	Apache	apache2 (Debian) / httpd (RHEL)	80 / 443
База данных	PostgreSQL	postgresql	5432
База данных	MySQL	mysql	3306
База данных	MariaDB	mariadb	3306
База данных	MongoDB	mongod	27017
Кэш	Redis	redis / redis-server	6379
Кэш	Memcached	memcached	11211
Очередь	RabbitMQ	rabbitmq-server	5672
Очередь	Kafka	kafka	9092
Контейнеры	Docker	docker	2375 / 2376
Виртуализация	libvirt	libvirtd	16509
VPN	WireGuard	wg-quick@wg0	51820
VPN	OpenVPN	openvpn@server	1194
Прокси	HAProxy	haproxy	80 / 443
Прокси	Traefik (если ставился как service)	traefik	80 / 443
Мониторинг	Prometheus	prometheus	9090
Мониторинг	Grafana	grafana-server	3000
Мониторинг	Node Exporter	node_exporter	9100
Логи	Filebeat	filebeat	—
Логи	Logstash	logstash	5044
Почта	Postfix	postfix	25 / 587
Почта	Dovecot	dovecot	143 / 993
DNS	Bind9	bind9 / named	53
DNS	dnsmasq	dnsmasq	53
DHCP	ISC DHCP	isc-dhcp-server	67
Файлы	Samba	smbd	445
Файлы	NFS	nfs-server	2049
CI/CD	GitLab Runner	gitlab-runner	—
CI/CD	Jenkins (если как service)	jenkins	8080
Веб-приложение	Gunicorn (через systemd unit)	myapp	8000
Веб-приложение	Uvicorn (через systemd unit)	myapp	8000
Node.js	PM2 (если через systemd)	pm2-root	3000


