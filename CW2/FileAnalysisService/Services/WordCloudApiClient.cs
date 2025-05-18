// FileAnalysisService/Services/WordCloudApiClient.cs
using System;
using System.Collections.Generic;
using System.Net.Http; // Используем IHttpClientFactory
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Linq;

namespace FileAnalysisService.Services
{
    public class WordCloudApiClient
    {
        // Изменили тип с HttpClient на IHttpClientFactory
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Конструктор теперь принимает IHttpClientFactory
        public WordCloudApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public Task<Dictionary<string, int>> GenerateWordCloudDataAsync(Dictionary<string, int> wordFrequencies, int topN = 50)
        {
            // В реальной реализации здесь бы использовался _httpClientFactory для создания клиента
            // Например: var client = _httpClientFactory.CreateClient();
            // ... логика вызова внешнего HTTP API с использованием 'client' ...

            var topWords = wordFrequencies.OrderByDescending(wf => wf.Value)
                                          .Take(topN)
                                          .ToDictionary(wf => wf.Key, wf => wf.Value);

            return Task.FromResult(topWords);
        }
    }
}