using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using SharedModels;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ApiGateway.Controllers
{
    [Route("files")]
    [ApiController] // e.g., /files
    public class FilesController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        // private readonly ILogger<FilesController> _logger; // TODO: Добавить логирование

        public FilesController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration
            /* , ILogger<FilesController> logger */)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            // _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<FileUploadResponse>> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var storingServiceUrl = _configuration["FileStoringService:Url"];
            if (string.IsNullOrEmpty(storingServiceUrl))
            {
                // TODO: Log critical error
                return StatusCode(500, "File storing service URL is not configured.");
            }

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{storingServiceUrl}/internal/files";

            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file.OpenReadStream());
            form.Add(fileContent, "file", file.FileName);

            try
            {
                // TODO: Log info: Proxying file upload request to Storing Service
                var response = await client.PostAsync(requestUrl, form);

                if (response.IsSuccessStatusCode)
                {
                    var uploadResponse = await response.Content.ReadFromJsonAsync<FileUploadResponse>();
                    return Ok(uploadResponse);
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    // TODO: Log warning/error
                    return BadRequest(errorContent);
                }
                else
                {
                    // TODO: Log error response from Storing Service
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Error from Storing Service: {response.StatusCode}. {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                // TODO: Log the exception
                return StatusCode(503, $"File storing service is unavailable or returned an error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // TODO: Log the exception
                return StatusCode(500, "An unexpected error occurred during file upload.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetFile(Guid id)
        {
            var storingServiceUrl = _configuration["FileStoringService:Url"];
            if (string.IsNullOrEmpty(storingServiceUrl))
            {
                // TODO: Log critical error
                return StatusCode(500, "File storing service URL is not configured.");
            }

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{storingServiceUrl}/internal/files/{id}/content";

            try
            {
                // TODO: Log info: Proxying get file content request for id {id} to Storing Service
                var response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    var contentType = response.Content.Headers.ContentType?.ToString();
                    var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName;

                    return File(contentStream, contentType ?? "application/octet-stream", fileName);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // TODO: Log warning
                    return NotFound($"File with ID {id} not found.");
                }
                else
                {
                    // TODO: Log error response from Storing Service
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Error from Storing Service: {response.StatusCode}. {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                // TODO: Log the exception
                return StatusCode(503, $"File storing service is unavailable or returned an error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // TODO: Log the exception
                return StatusCode(500, "An unexpected error occurred while retrieving the file.");
            }
        }
    }
}