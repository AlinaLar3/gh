// FileAnalysisService/AppDbContextFactory.cs
// ���� ���� ������������ ������ ������������� Entity Framework Core � ������-�����
// �� �� ������������� � �� ����������� � ����� ������� ����������

using FileAnalysisService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO; // ��� Path.Combine � Directory
using System; // ��� InvalidOperationException

// �����: ����� ������ ���� � ������������ ���� �������� ������ ��� � ��� �� ������������ ����, ��� � Program.cs
// ���� Program.cs �� ���������� namespace, ����� �������� ���� ����� ��� namespace

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // ���� ����� ���������� ������������� EF Core (dotnet ef)
        // ����� ������� ������� ������������, ����� �������� ������ �����������

        Console.WriteLine("Using AppDbContextFactory for design-time DbContext creation."); // ��� �������

        var configuration = new ConfigurationBuilder()
            // ������������� ������� ���� ������ ������ ������������.
            // ����������� EF Core ������ ����������� �� �������� ����� �������,
            // �� ���� ���� ������ ����� ��������, ���� ���� ��������� �� ����� �������
            .SetBasePath(Directory.GetCurrentDirectory())
            // ��������� ����������� ����� appsettings
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // optional: true - ���� ����� ���, ��� �� ������
                                                                                   // ��������� ����������� ��� ����� ���������� ���������, ���������������� appsettings.json
                                                                                   // ��������� Environment = Development ���� ��� ����� ���������� ����� (DOTNET_ENVIRONMENT=Development)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // optional: true - ���� ����� ���, ��� �� ������
            .AddEnvironmentVariables() // ��������� �������������� ��������� ����� ���������� �����
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            // TODO: Log critical error - string.
            throw new InvalidOperationException("DefaultConnection connection string is not configured in appsettings. Design-time factory failed.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // ����������� UseSqlite, ��� ��� ��� ���� ��
        optionsBuilder.UseSqlite(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}