// FileAnalysisService/AppDbContextFactory.cs
// Этот файл используется ТОЛЬКО инструментами Entity Framework Core в дизайн-тайме
// Он не компилируется и не запускается в вашем рабочем приложении

using FileAnalysisService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO; // Для Path.Combine и Directory
using System; // Для InvalidOperationException

// Важно: класс должен быть в пространстве имен верхнего уровня или в том же пространстве имен, что и Program.cs
// Если Program.cs не использует namespace, можно оставить этот класс без namespace

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Этот метод вызывается инструментами EF Core (dotnet ef)
        // Нужно вручную собрать конфигурацию, чтобы получить строку подключения

        Console.WriteLine("Using AppDbContextFactory for design-time DbContext creation."); // Для отладки

        var configuration = new ConfigurationBuilder()
            // Устанавливаем базовый путь поиска файлов конфигурации.
            // Инструменты EF Core обычно запускаются из корневой папки проекта,
            // но этот путь делает более надежным, даже если запускать из корня решения
            .SetBasePath(Directory.GetCurrentDirectory())
            // Загружаем стандартные файлы appsettings
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // optional: true - если файла нет, это не ошибка
                                                                                   // Загружаем специфичные для среды разработки настройки, переопределяющие appsettings.json
                                                                                   // Указываем Environment = Development явно или через переменные среды (DOTNET_ENVIRONMENT=Development)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // optional: true - если файла нет, это не ошибка
            .AddEnvironmentVariables() // Позволяет переопределить настройки через переменные среды
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            // TODO: Log critical error - string.
            throw new InvalidOperationException("DefaultConnection connection string is not configured in appsettings. Design-time factory failed.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Используйте UseSqlite, так как это ваша БД
        optionsBuilder.UseSqlite(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}