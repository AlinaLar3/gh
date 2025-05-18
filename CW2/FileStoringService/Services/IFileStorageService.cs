// FileStoringService/Services/IFileStorageService.cs
using System.Threading.Tasks;

namespace FileStoringService.Services
{
    public interface IFileStorageService
    {
        Task SaveFileAsync(string location, byte[] content);
        Task<byte[]> ReadFileAsync(string location);
        // Task DeleteFileAsync(string location); // ��� ������ ����������
        // Task<bool> FileExistsAsync(string location); // ��� ������ ����������
    }
}