using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using FileAnalysisService.Data;
using FileAnalysisService.Services;
using SharedModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Linq; // Для Count()
using Microsoft.Extensions.DependencyInjection; // Для CreateScope
using System.Collections.Generic; // Для Dictionary

namespace FileAnalysisService.Controllers
{
    [Route("internal/analysis")]
    [ApiController]
    public class InternalAnalysisController : ControllerBase
    {
        // Вместо прямого доступа к DbContext в контроллере,
        // лучше использовать Service, который инкапсулирует работу с БД
        private readonly IServiceScopeFactory _scopeFactory; // Для создания области видимости в фоновой задаче
        private readonly FileContentClient _fileContentClient;
        private readonly TextAnalyzer _textAnalyzer;
        private readonly WordCloudApiClient _wordCloudApiClient;
        // private readonly ILogger<InternalAnalysisController> _logger; // TODO: Добавить логирование

        public InternalAnalysisController(
            IServiceScopeFactory scopeFactory, // Используем фабрику областей видимости
            FileContentClient fileContentClient,
            TextAnalyzer textAnalyzer,
            WordCloudApiClient wordCloudApiClient
            /* , ILogger<InternalAnalysisController> logger */)
        {
            _scopeFactory = scopeFactory;
            _fileContentClient = fileContentClient;
            _textAnalyzer = textAnalyzer;
            _wordCloudApiClient = wordCloudApiClient;
            // _logger = logger;
        }

        [HttpPost("analyze")]
        public async Task<ActionResult> TriggerAnalysis([FromBody] AnalysisTriggerRequest request)
        {
            if (request == null || request.FileId == Guid.Empty)
            {
                return BadRequest("Invalid request body. FileId is required.");
            }
            Guid fileId = request.FileId;

            // Создаем отдельную область видимости для проверки в БД
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var existingResult = await dbContext.AnalysisResults.FirstOrDefaultAsync(r => r.FileId == fileId);
                if (existingResult != null)
                {
                    if (existingResult.Status == AnalysisStatus.Completed || existingResult.Status == AnalysisStatus.Processing)
                    {
                        // TODO: Log warning
                        return Ok($"Analysis already exists or is in progress for file {fileId}. Status: {existingResult.Status}");
                    }
                    // Если статус Failed, можно перезапустить - здесь просто вернем статус
                    return Ok($"Analysis status for file {fileId}: {existingResult.Status}.");
                }

                // Создаем запись о начале анализа в БД
                var analysisResult = new AnalysisResult
                {
                    Id = Guid.NewGuid(),
                    FileId = fileId,
                    Status = AnalysisStatus.Pending,
                    AnalysisTimestamp = DateTime.UtcNow
                };
                dbContext.AnalysisResults.Add(analysisResult);
                await dbContext.SaveChangesAsync();

                // Запускаем сам процесс анализа в фоновом потоке
                // TODO: Лучше использовать надежный механизм фоновых задач (IHostedService, Hangfire, MQ Consumer)
                _ = Task.Run(() => PerformAnalysisAsync(analysisResult.Id, fileId)); // Используем Task.Run

                return Ok($"Analysis triggered for file {fileId}. Analysis ID: {analysisResult.Id}");
            } // Область видимости закрывается, DbContext утилизируется
        }

        // Внутренний метод, выполняющий анализ (должен работать в фоновом режиме!)
        private async Task PerformAnalysisAsync(Guid analysisResultId, Guid fileId)
        {
            // Создаем отдельную область видимости для доступа к сервисам в фоновом потоке
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Можем получить и другие сервисы, если они зарегистрированы как Scoped или Transient

                var analysisResult = await dbContext.AnalysisResults.FindAsync(analysisResultId);
                if (analysisResult == null)
                {
                    Console.WriteLine($"Error: AnalysisResult with ID {analysisResultId} not found in background task.");
                    return;
                }

                analysisResult.Status = AnalysisStatus.Processing;
                await dbContext.SaveChangesAsync();

                try
                {
                    // TODO: Log info: Starting analysis for file {fileId} (AnalysisResultId: {analysisResultId})

                    // 1. Получить контент файла из File Storing Service
                    byte[] fileContent = await _fileContentClient.GetFileContentAsync(fileId);

                    if (fileContent == null || fileContent.Length == 0)
                    {
                        throw new Exception("Received empty or null file content from Storing Service.");
                    }
                    string text = Encoding.UTF8.GetString(fileContent); // Предполагаем UTF8 кодировку для .txt

                    // 2. Провести анализ текста
                    var analysisData = _textAnalyzer.Analyze(text);

                    // 3. Вызвать внешний Word Cloud API или использовать локальную логику
                    var wordCloudData = await _wordCloudApiClient.GenerateWordCloudDataAsync(analysisData.WordFrequencies);

                    // 4. Сохранить результаты в БД
                    analysisResult.ParagraphCount = analysisData.ParagraphCount;
                    analysisResult.WordCount = analysisData.WordCount;
                    analysisResult.SymbolCount = analysisData.SymbolCount;
                    analysisResult.WordCloudJson = JsonSerializer.Serialize(wordCloudData);
                    analysisResult.Status = AnalysisStatus.Completed;
                    analysisResult.AnalysisTimestamp = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync();
                    // TODO: Log info: Analysis completed successfully for file {fileId}
                }
                catch (Exception ex)
                {
                    // TODO: Log error: Analysis failed for file {fileId}: {ex.Message}
                    if (analysisResult != null)
                    {
                        analysisResult.Status = AnalysisStatus.Failed;
                        analysisResult.ErrorMessage = ex.Message.Substring(0, Math.Min(ex.Message.Length, 250)); // Обрезать сообщение об ошибке
                        analysisResult.AnalysisTimestamp = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync(); // Сохраняем статус ошибки
                    }
                }
            } // Область видимости для фоновой задачи закрывается
        }


        [HttpGet("{fileId}")]
        public async Task<ActionResult<AnalysisResultDto>> GetAnalysisResult(Guid fileId)
        {
            // Создаем отдельную область видимости для выполнения запроса
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var result = await dbContext.AnalysisResults.FirstOrDefaultAsync(r => r.FileId == fileId);

                if (result == null)
                {
                    // TODO: Log warning
                    return NotFound($"Analysis result not found for file {fileId}. Perhaps analysis was not triggered or file ID is incorrect.");
                }

                if (result.Status == AnalysisStatus.Pending || result.Status == AnalysisStatus.Processing)
                {
                    return Accepted(new AnalysisStatusDto { FileId = fileId, Status = result.Status.ToString() });
                }

                if (result.Status == AnalysisStatus.Failed)
                {
                    return StatusCode(500, $"Analysis failed for file {fileId}. Error: {result.ErrorMessage}");
                }

                // Статус Completed - возвращаем полный результат
                var resultDto = new AnalysisResultDto
                {
                    FileId = result.FileId,
                    ParagraphCount = result.ParagraphCount,
                    WordCount = result.WordCount,
                    SymbolCount = result.SymbolCount,
                    WordCloudData = !string.IsNullOrEmpty(result.WordCloudJson) ?
                                    JsonSerializer.Deserialize<Dictionary<string, int>>(result.WordCloudJson) :
                                    new Dictionary<string, int>(),
                    Status = result.Status.ToString(),
                    ErrorMessage = result.ErrorMessage
                };

                return Ok(resultDto);
            } // Область видимости закрывается
        }

        [HttpGet("{fileId}/status")]
        public async Task<ActionResult<AnalysisStatusDto>> GetAnalysisStatus(Guid fileId)
        {
            // Создаем отдельную область видимости для выполнения запроса
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var result = await dbContext.AnalysisResults.FirstOrDefaultAsync(r => r.FileId == fileId);

                if (result == null)
                {
                    // TODO: Log warning
                    return NotFound($"Analysis status not found for file {fileId}.");
                }

                var statusDto = new AnalysisStatusDto
                {
                    FileId = fileId,
                    Status = result.Status.ToString()
                };

                return Ok(statusDto);
            } // Область видимости закрывается
        }
    }
}