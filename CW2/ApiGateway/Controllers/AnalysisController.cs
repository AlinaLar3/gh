using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using SharedModels;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic; // Для Dictionary в DTO

namespace ApiGateway.Controllers
{
    [Route("analysis")]
    [ApiController]// e.g., /analysis
    public class AnalysisController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        // private readonly ILogger<AnalysisController> _logger; // TODO: Добавить логирование

        public AnalysisController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration
            /* , ILogger<AnalysisController> logger */)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            // _logger = logger;
        }

        [HttpGet("{fileId}")]
        public async Task<ActionResult<AnalysisResultDto>> GetAnalysisResult(Guid fileId)
        {
            var analysisServiceUrl = _configuration["FileAnalysisService:Url"];
            if (string.IsNullOrEmpty(analysisServiceUrl))
            {
                // TODO: Log critical error
                return StatusCode(500, "File analysis service URL is not configured.");
            }

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{analysisServiceUrl}/internal/analysis/{fileId}";

            try
            {
                // TODO: Log info: Proxying get analysis result request for fileId {fileId} to Analysis Service
                var response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode) // 200 OK
                {
                    var resultDto = await response.Content.ReadFromJsonAsync<AnalysisResultDto>();
                    return Ok(resultDto);
                }
                else if (response.StatusCode == HttpStatusCode.Accepted) // 202 Accepted - анализ в процессе
                {
                    var statusDto = await response.Content.ReadFromJsonAsync<AnalysisStatusDto>();
                    // TODO: Log info: Analysis for fileId {fileId} is still {statusDto?.Status ?? "unknown"}
                    return Accepted(statusDto);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // TODO: Log warning
                    return NotFound($"Analysis result or file with ID {fileId} not found.");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError) // Например, если Analysis Service вернул 500 из-за Failed статуса анализа
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    // TODO: Log error
                    return StatusCode((int)response.StatusCode, $"Error during analysis: {errorContent}");
                }
                else
                {
                    // TODO: Log error response from Analysis Service
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Error from Analysis Service: {response.StatusCode}. {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                // TODO: Log the exception
                return StatusCode(503, $"File analysis service is unavailable or returned an error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // TODO: Log the exception
                return StatusCode(500, "An unexpected error occurred while retrieving the analysis result.");
            }
        }

        [HttpGet("{fileId}/status")]
        public async Task<ActionResult<AnalysisStatusDto>> GetAnalysisStatus(Guid fileId)
        {
            var analysisServiceUrl = _configuration["FileAnalysisService:Url"];
            if (string.IsNullOrEmpty(analysisServiceUrl))
            {
                // TODO: Log critical error
                return StatusCode(500, "File analysis service URL is not configured.");
            }

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{analysisServiceUrl}/internal/analysis/{fileId}/status";

            try
            {
                // TODO: Log info: Proxying get analysis status request for fileId {fileId} to Analysis Service
                var response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var statusDto = await response.Content.ReadFromJsonAsync<AnalysisStatusDto>();
                    return Ok(statusDto);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // TODO: Log warning
                    return NotFound($"Analysis status for file {fileId} not found.");
                }
                else
                {
                    // TODO: Log error response from Analysis Service
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Error from Analysis Service: {response.StatusCode}. {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                // TODO: Log the exception
                return StatusCode(503, $"File analysis service is unavailable or returned an error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // TODO: Log the exception
                return StatusCode(500, "An unexpected error occurred while retrieving the analysis status.");
            }
        }
    }
}