using watchtower.services; // Подключаем ваши пространства имен

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем ваш кастомный файл конфигурации
builder.Configuration.AddJsonFile("watchtower-configuration.json", optional: false, reloadOnChange: true);

// 2. Регистрируем сервисы (Dependency Injection)
// LogingService, TelegramNotifier и т.д. должны быть Singleton, так как они используются фоновой службой.
builder.Services.AddSingleton<LogingService>();
builder.Services.AddSingleton<TelegramNotifier>();
builder.Services.AddSingleton<ServiceRestarter>();
builder.Services.AddSingleton<HealthCheckService>();

// 3. Регистрируем HealthCheckService как HostedService
// Это запустит вашу логику проверки в фоне вместе с веб-сервером.
builder.Services.AddHostedService<HealthCheckService>(provider => 
    provider.GetRequiredService<HealthCheckService>());

// Стандартные настройки Web API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();