// FileAnalysisService/Services/FileContentClient.cs
using System;
using System.Net.Http; // ���������� IHttpClientFactory
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace FileAnalysisService.Services
{
    public class FileContentClient
    {
        // �������� ��� � HttpClient �� IHttpClientFactory
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // ����������� ������ ��������� IHttpClientFactory
        public FileContentClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<byte[]> GetFileContentAsync(Guid fileId)
        {
            var storingServiceUrl = _configuration["FileStoringService:Url"];
            if (string.IsNullOrEmpty(storingServiceUrl))
            {
                // TODO: Log critical error
                throw new InvalidOperationException("FileStoringService:Url is not configured.");
            }

            var requestUrl = $"{storingServiceUrl}/internal/files/{fileId}/content";

            // ������� HttpClient � ������� �������
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                // TODO: Log error response
                throw new HttpRequestException($"Failed to get file content from Storing Service. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
    }
}