using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FileStoringService.Services
{
    public class LocalFileSystemStorageService : IFileStorageService
    {
        private readonly string _basePath;

        public LocalFileSystemStorageService(IConfiguration configuration)
        {
            _basePath = configuration["FileStorage:BasePath"];
            if (string.IsNullOrEmpty(_basePath))
            {
                // Замените на нормальное логирование в реальном приложении
                Console.WriteLine("FATAL ERROR: FileStorage:BasePath is not configured in appsettings.");
                throw new ArgumentNullException("FileStorage:BasePath is not configured.");
            }
            try
            {
                if (!Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                }
            }
            catch (Exception ex)
            {
                // Замените на нормальное логирование
                Console.WriteLine($"FATAL ERROR: Could not create file storage directory '{_basePath}'. Exception: {ex.Message}");
                throw; // Перевыбросить исключение
            }
        }

        public async Task SaveFileAsync(string location, byte[] content)
        {
            var fullPath = Path.Combine(_basePath, location);
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllBytesAsync(fullPath, content);
        }

        public async Task<byte[]> ReadFileAsync(string location)
        {
            var fullPath = Path.Combine(_basePath, location);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found at {fullPath}");
            }
            return await File.ReadAllBytesAsync(fullPath);
        }
    }
}