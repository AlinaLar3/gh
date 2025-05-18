using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using FileAnalysisService.Data;
using FileAnalysisService.Services;
using SharedModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Linq; // ��� Count()
using Microsoft.Extensions.DependencyInjection; // ��� CreateScope
using System.Collections.Generic; // ��� Dictionary

namespace FileAnalysisService.Controllers
{
    [Route("internal/analysis")]
    [ApiController]
    public class InternalAnalysisController : ControllerBase
    {
        // ������ ������� ������� � DbContext � �����������,
        // ����� ������������ Service, ������� ������������� ������ � ��
        private readonly IServiceScopeFactory _scopeFactory; // ��� �������� ������� ��������� � ������� ������
        private readonly FileContentClient _fileContentClient;
        private readonly TextAnalyzer _textAnalyzer;
        private readonly WordCloudApiClient _wordCloudApiClient;
        // private readonly ILogger<InternalAnalysisController> _logger; // TODO: �������� �����������

        public InternalAnalysisController(
            IServiceScopeFactory scopeFactory, // ���������� ������� �������� ���������
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

            // ������� ��������� ������� ��������� ��� �������� � ��
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
                    // ���� ������ Failed, ����� ������������� - ����� ������ ������ ������
                    return Ok($"Analysis status for file {fileId}: {existingResult.Status}.");
                }

                // ������� ������ � ������ ������� � ��
                var analysisResult = new AnalysisResult
                {
                    Id = Guid.NewGuid(),
                    FileId = fileId,
                    Status = AnalysisStatus.Pending,
                    AnalysisTimestamp = DateTime.UtcNow
                };
                dbContext.AnalysisResults.Add(analysisResult);
                await dbContext.SaveChangesAsync();

                // ��������� ��� ������� ������� � ������� ������
                // TODO: ����� ������������ �������� �������� ������� ����� (IHostedService, Hangfire, MQ Consumer)
                _ = Task.Run(() => PerformAnalysisAsync(analysisResult.Id, fileId)); // ���������� Task.Run

                return Ok($"Analysis triggered for file {fileId}. Analysis ID: {analysisResult.Id}");
            } // ������� ��������� �����������, DbContext �������������
        }

        // ���������� �����, ����������� ������ (������ �������� � ������� ������!)
        private async Task PerformAnalysisAsync(Guid analysisResultId, Guid fileId)
        {
            // ������� ��������� ������� ��������� ��� ������� � �������� � ������� ������
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // ����� �������� � ������ �������, ���� ��� ���������������� ��� Scoped ��� Transient

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

                    // 1. �������� ������� ����� �� File Storing Service
                    byte[] fileContent = await _fileContentClient.GetFileContentAsync(fileId);

                    if (fileContent == null || fileContent.Length == 0)
                    {
                        throw new Exception("Received empty or null file content from Storing Service.");
                    }
                    string text = Encoding.UTF8.GetString(fileContent); // ������������ UTF8 ��������� ��� .txt

                    // 2. �������� ������ ������
                    var analysisData = _textAnalyzer.Analyze(text);

                    // 3. ������� ������� Word Cloud API ��� ������������ ��������� ������
                    var wordCloudData = await _wordCloudApiClient.GenerateWordCloudDataAsync(analysisData.WordFrequencies);

                    // 4. ��������� ���������� � ��
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
                        analysisResult.ErrorMessage = ex.Message.Substring(0, Math.Min(ex.Message.Length, 250)); // �������� ��������� �� ������
                        analysisResult.AnalysisTimestamp = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync(); // ��������� ������ ������
                    }
                }
            } // ������� ��������� ��� ������� ������ �����������
        }


        [HttpGet("{fileId}")]
        public async Task<ActionResult<AnalysisResultDto>> GetAnalysisResult(Guid fileId)
        {
            // ������� ��������� ������� ��������� ��� ���������� �������
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

                // ������ Completed - ���������� ������ ���������
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
            } // ������� ��������� �����������
        }

        [HttpGet("{fileId}/status")]
        public async Task<ActionResult<AnalysisStatusDto>> GetAnalysisStatus(Guid fileId)
        {
            // ������� ��������� ������� ��������� ��� ���������� �������
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
            } // ������� ��������� �����������
        }
    }
}