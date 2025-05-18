using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using FileStoringService.Data;
using SharedModels;
using FileStoringService.Services;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace FileStoringService.Controllers
{
    [Route("internal/files")]
    [ApiController]
    public class InternalFilesController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IFileStorageService _fileStorageService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        // private readonly ILogger<InternalFilesController> _logger; // TODO: Добавить логирование

        public InternalFilesController(
            AppDbContext dbContext,
            IFileStorageService fileStorageService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration
            /* , ILogger<InternalFilesController> logger */)
        {
            _dbContext = dbContext;
            _fileStorageService = fileStorageService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            // _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<FileUploadResponse>> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            string fileHash;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(fileContent);
                fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            var existingFile = await _dbContext.FilesMetadata.FirstOrDefaultAsync(f => f.FileHash == fileHash);

            if (existingFile != null)
            {
                // TODO: Log info: Duplicate file uploaded with hash {fileHash}, returning existing fileId {existingFile.Id}
                return Ok(new FileUploadResponse { FileId = existingFile.Id, Status = "DuplicateFound" });
            }
            else
            {
                var newFileId = Guid.NewGuid();
                // Пример формирования пути для сохранения
                var storageLocation = Path.Combine(DateTime.UtcNow.ToString("yyyy/MM/dd"), newFileId.ToString() + Path.GetExtension(file.FileName));

                var newFileMetadata = new FileMetadata
                {
                    Id = newFileId,
                    FileName = file.FileName,
                    FileHash = fileHash,
                    StorageLocation = storageLocation,
                    UploadTimestamp = DateTime.UtcNow
                };

                try
                {
                    await _fileStorageService.SaveFileAsync(storageLocation, fileContent); // Сохраняем контент
                    _dbContext.FilesMetadata.Add(newFileMetadata); // Сохраняем метаданные
                    await _dbContext.SaveChangesAsync();

                    var response = new FileUploadResponse { FileId = newFileId, Status = "Uploaded" };

                    // TODO: Log info: File {newFileMetadata.FileName} uploaded successfully with fileId {newFileId}
                    // Триггер анализа (запускаем асинхронно, не дожидаясь результата)
                    _ = TriggerAnalysisAsync(newFileId); // Использовать Task.Run или очередь сообщений для надежности

                    return CreatedAtAction(nameof(GetFileContent), new { id = newFileId }, response);
                }
                catch (Exception ex)
                {
                    // TODO: Log error during file upload and saving
                    // Если сохранение контента успешно, но запись в БД упала, может быть несогласованность.
                    // Для продакшна требуется более надежный механизм (например, Saga).
                    return StatusCode(500, "Error saving file metadata or content.");
                }
            }
        }

        // Метод для вызова File Analysis Service (упрощенно)
        private async Task TriggerAnalysisAsync(Guid fileId)
        {
            try
            {
                var analysisServiceUrl = _configuration["AnalysisService:Url"];
                if (string.IsNullOrEmpty(analysisServiceUrl))
                {
                    // TODO: Log critical error: Analysis Service URL not configured
                    return;
                }

                var client = _httpClientFactory.CreateClient();
                // Отправляем запрос на запуск анализа с FileId
                var response = await client.PostAsJsonAsync($"{analysisServiceUrl}/internal/analysis/analyze", new AnalysisTriggerRequest { FileId = fileId });

                if (response.IsSuccessStatusCode)
                {
                    // TODO: Log info: Analysis triggered successfully for fileId {fileId}
                }
                else
                {
                    // TODO: Log warning/error: Failed to trigger analysis for fileId {fileId}. Status: {response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}
                }
            }
            catch (Exception ex)
            {
                // TODO: Log exception during triggering analysis
            }
        }


        [HttpGet("{id}/content")]
        public async Task<ActionResult> GetFileContent(Guid id)
        {
            var fileMetadata = await _dbContext.FilesMetadata.FindAsync(id);

            if (fileMetadata == null)
            {
                // TODO: Log info: File metadata with id {id} not found
                return NotFound();
            }

            try
            {
                var fileContent = await _fileStorageService.ReadFileAsync(fileMetadata.StorageLocation);

                var mimeType = "application/octet-stream"; // Тип по умолчанию
                                                           // TODO: Add logic to determine mimeType based on fileMetadata.FileName

                return File(fileContent, mimeType, fileMetadata.FileName);
            }
            catch (FileNotFoundException)
            {
                // TODO: Log error: Metadata exists for file id {id}, but file content is missing at {fileMetadata.StorageLocation}
                return NotFound("File content not found in storage.");
            }
            catch (Exception ex)
            {
                // TODO: Log the exception
                return StatusCode(500, "Error reading file content from storage.");
            }
        }
    }
}