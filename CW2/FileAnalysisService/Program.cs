using FileAnalysisService.Data;
using FileAnalysisService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;
using System;
using System.Linq; // ��� GetPendingMigrations

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// ��������� EF Core ��� SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    // TODO: Log critical error
    throw new InvalidOperationException("DefaultConnection connection string is not configured in appsettings.");
}

// ��������, ��� ������� ��� ����� ���� ������ ����������, ���� ������ ������������� ����.
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
    options.UseSqlite(connectionString)); // ���������� UseSqlite
builder.Services.AddHttpClient();

// ����������� �������� ������� (Scoped, �.�. ��� ���������� Scoped DbContext � Client)
builder.Services.AddScoped<TextAnalyzer>();
builder.Services.AddScoped<WordCloudApiClient>();
builder.Services.AddScoped<FileContentClient>(); // ������ ��� ������ ������� �������

// ����������� HttpClient (IHttpClientFactory ��� ���������������� �� ���������)
// HttpClient<T> �������������� ������������� ��� ����������� T ��� Scoped ��� Transient
// builder.Services.AddHttpClient<FileContentClient>(); // ����� � ���, �� AddScoped<FileContentClient> ��� ����������

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileAnalysisService", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// �������������� ���������� �������� ��� ������ ���������� (������ ��� ���������� � SQLite)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.GetPendingMigrations().Any())
    {
        Console.WriteLine("Applying migrations for FileAnalysisService DbContext...");
        dbContext.Database.Migrate();
        Console.WriteLine("Migrations applied.");
    }
    else
    {
        Console.WriteLine("No pending migrations for FileAnalysisService DbContext or database is already up-to-date.");
    }
}

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileAnalysisService v1"));
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();