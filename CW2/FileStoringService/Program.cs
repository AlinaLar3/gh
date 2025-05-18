using FileStoringService.Data;
using FileStoringService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;
using System; // Для AppContext

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Настройка EF Core для SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Убедитесь, что строка подключения не пустая
if (string.IsNullOrEmpty(connectionString))
{
    // TODO: Log critical error
    throw new InvalidOperationException("DefaultConnection connection string is not configured in appsettings.");
}

// Для SQLite относительные пути Data Source=... работают относительно каталога,
// из которого запущено приложение (обычно bin/Debug/netX.0).
// Убедимся, что каталог для файла базы данных существует, если указан относительный путь.
if (!Path.IsPathRooted(connectionString.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase)))
{
    var dbFilePath = connectionString.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase);
    var dbDirectory = Path.GetDirectoryName(Path.Combine(AppContext.BaseDirectory, dbFilePath));
    if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
    {
        Directory.CreateDirectory(dbDirectory);
    }
}


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)); // Используем UseSqlite

// Регистрация сервиса хранения файлов
builder.Services.AddScoped<IFileStorageService, LocalFileSystemStorageService>();

// Регистрация HttpClientFactory для вызовов к File Analysis Service
builder.Services.AddHttpClient();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileStoringService", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Автоматическое применение миграций при старте приложения (Удобно для разработки с SQLite)
// В продакшне миграции лучше применять отдельно или использовать другие стратегии
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Проверяем, создана ли база данных и применяем ожидающие миграции
    if (dbContext.Database.GetPendingMigrations().Any())
    {
        Console.WriteLine("Applying migrations for FileStoringService DbContext...");
        dbContext.Database.Migrate();
        Console.WriteLine("Migrations applied.");
    }
    else
    {
        Console.WriteLine("No pending migrations for FileStoringService DbContext or database is already up-to-date.");
    }
}


// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment()) // Обычно Swagger включают только в Development
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileStoringService v1"));
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();