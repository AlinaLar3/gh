// FileAnalysisService/Services/WordCloudApiClient.cs
using System;
using System.Collections.Generic;
using System.Net.Http; // ���������� IHttpClientFactory
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Linq;

namespace FileAnalysisService.Services
{
    public class WordCloudApiClient
    {
        // �������� ��� � HttpClient �� IHttpClientFactory
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // ����������� ������ ��������� IHttpClientFactory
        public WordCloudApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public Task<Dictionary<string, int>> GenerateWordCloudDataAsync(Dictionary<string, int> wordFrequencies, int topN = 50)
        {
            // � �������� ���������� ����� �� ������������� _httpClientFactory ��� �������� �������
            // ��������: var client = _httpClientFactory.CreateClient();
            // ... ������ ������ �������� HTTP API � �������������� 'client' ...

            var topWords = wordFrequencies.OrderByDescending(wf => wf.Value)
                                          .Take(topN)
                                          .ToDictionary(wf => wf.Key, wf => wf.Value);

            return Task.FromResult(topWords);
        }
    }
}