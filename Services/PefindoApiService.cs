using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Implementation of Pefindo PBK API client service
/// </summary>
public class PefindoApiService : IPefindoApiService
{
    private readonly HttpClient _httpClient;
    private readonly PefindoConfig _config;
    private readonly ILogger<PefindoApiService> _logger;
    private readonly IErrorLogger _errorLogger;
    private readonly ICorrelationService _correlationService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public PefindoApiService(
        HttpClient httpClient,
        IOptions<PefindoConfig> config,
        ILogger<PefindoApiService> logger,
        IErrorLogger errorLogger,
        ICorrelationService correlationService)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _errorLogger = errorLogger;
        _correlationService = correlationService;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "IDC-Pefindo-PBK/1.0");
    }

    public async Task<string> GetTokenAsync()
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Requesting token from Pefindo API, correlation: {CorrelationId}", correlationId);

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/getToken");
            request.Headers.Add("Authorization", $"Basic {credentials}");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = $"Failed to get token. Status: {response.StatusCode}, Content: {content}";
                await _errorLogger.LogErrorAsync("PefindoApiService.GetToken", error, null, correlationId);
                throw new HttpRequestException(error);
            }

            var tokenResponse = JsonSerializer.Deserialize<PefindoTokenResponse>(content, _jsonOptions);

            if (tokenResponse?.Code != "01" || tokenResponse.Status != "success")
            {
                var error = $"Token request failed. Code: {tokenResponse?.Code}, Status: {tokenResponse?.Status}, Message: {tokenResponse?.Message}";
                await _errorLogger.LogErrorAsync("PefindoApiService.GetToken", error, null, correlationId);
                throw new InvalidOperationException(error);
            }

            // Return the raw JSON response so TokenManagerService can parse it properly
            _logger.LogInformation("Successfully obtained token from Pefindo API, correlation: {CorrelationId}", correlationId);
            return content; // Return raw JSON for proper parsing in TokenManagerService
        }
        catch (HttpRequestException httpEx)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.GetToken", "HTTP request error while getting token", httpEx, correlationId);
            throw;
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.GetToken", "Error getting token from Pefindo API", ex, correlationId);
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Validating token with Pefindo API for correlation {CorrelationId}", correlationId);

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/validateToken");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Content-Type", "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await _errorLogger.LogWarningAsync("PefindoApiService.ValidateToken",
                    $"Token validation failed. Status: {response.StatusCode}", correlationId);
                return false;
            }

            var validationResponse = JsonSerializer.Deserialize<PefindoTokenResponse>(content, _jsonOptions);
            var isValid = validationResponse?.Code == "01" && validationResponse.Status == "success";

            _logger.LogDebug("Token validation result: {IsValid} for correlation {CorrelationId}", isValid, correlationId);
            return isValid;
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.ValidateToken", "Error validating token", ex, correlationId);
            return false;
        }
    }

    public async Task<PefindoSearchResponse> SearchDebtorAsync(PefindoSearchRequest request, string token)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Performing debtor search for reference: {ReferenceCode}, correlation: {CorrelationId}",
                request.ReferenceCode, correlationId);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/product/search")
            {
                Content = content
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Search response status: {StatusCode}, correlation: {CorrelationId}",
                response.StatusCode, correlationId);

            var searchResponse = JsonSerializer.Deserialize<PefindoSearchResponse>(responseContent, _jsonOptions);

            if (searchResponse == null)
            {
                var error = "Failed to deserialize search response";
                await _errorLogger.LogErrorAsync("PefindoApiService.SearchDebtor", error, null, correlationId);
                throw new InvalidOperationException(error);
            }

            _logger.LogInformation("Search completed. Code: {Code}, Status: {Status}, Results: {Count}, correlation: {CorrelationId}",
                searchResponse.Code, searchResponse.Status, searchResponse.Data.Count, correlationId);

            return searchResponse;
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.SearchDebtor", "Error performing debtor search", ex, correlationId);
            throw;
        }
    }

    public async Task<PefindoReportResponse> GenerateReportAsync(PefindoReportRequest request, string token)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Generating report for event ID: {EventId}, correlation: {CorrelationId}",
                request.EventId, correlationId);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/product/generateReport")
            {
                Content = content
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            var reportResponse = JsonSerializer.Deserialize<PefindoReportResponse>(responseContent, _jsonOptions);

            if (reportResponse == null)
            {
                var error = "Failed to deserialize report generation response";
                await _errorLogger.LogErrorAsync("PefindoApiService.GenerateReport", error, null, correlationId);
                throw new InvalidOperationException(error);
            }

            _logger.LogInformation("Report generation initiated. Code: {Code}, Status: {Status}, EventId: {EventId}, correlation: {CorrelationId}",
                reportResponse.Code, reportResponse.Status, reportResponse.EventId, correlationId);

            return reportResponse;
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.GenerateReport",
                $"Error generating report for event ID: {request.EventId}", ex, correlationId);
            throw;
        }
    }

    public async Task<PefindoGetReportResponse> GetReportAsync(string eventId, string token)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogDebug("Retrieving report for event ID: {EventId}, correlation: {CorrelationId}", eventId, correlationId);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/product/getReport/event_id/{eventId}");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var reportResponse = JsonSerializer.Deserialize<PefindoGetReportResponse>(content, _jsonOptions);

            if (reportResponse == null)
            {
                var error = "Failed to deserialize get report response";
                await _errorLogger.LogErrorAsync("PefindoApiService.GetReport", error, null, correlationId);
                throw new InvalidOperationException(error);
            }

            _logger.LogInformation("Report retrieval completed. Code: {Code}, Status: {Status}, EventId: {EventId}, correlation: {CorrelationId}",
                reportResponse.Code, reportResponse.Status, reportResponse.EventId, correlationId);

            return reportResponse;
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.GetReport",
                $"Error retrieving report for event ID: {eventId}", ex, correlationId);
            throw;
        }
    }

    public async Task<PefindoGetReportResponse> DownloadReportAsync(string eventId, string token, int? page = null, int? maxRecords = null)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Downloading big report for event ID: {EventId}, Page: {Page}, Max: {Max}, correlation: {CorrelationId}",
                eventId, page, maxRecords, correlationId);

            var url = $"/api/v1/product/downloadReport/event_id/{eventId}";
            if (page.HasValue || maxRecords.HasValue)
            {
                var queryParams = new List<string>();
                if (page.HasValue) queryParams.Add($"page={page.Value}");
                if (maxRecords.HasValue) queryParams.Add($"max={maxRecords.Value}");
                url += "?" + string.Join("&", queryParams);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var reportResponse = JsonSerializer.Deserialize<PefindoGetReportResponse>(content, _jsonOptions);

            if (reportResponse == null)
            {
                var error = "Failed to deserialize download report response";
                await _errorLogger.LogErrorAsync("PefindoApiService.DownloadReport", error, null, correlationId);
                throw new InvalidOperationException(error);
            }

            _logger.LogInformation("Big report download completed. Code: {Code}, Status: {Status}, correlation: {CorrelationId}",
                reportResponse.Code, reportResponse.Status, correlationId);

            return reportResponse;
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.DownloadReport",
                $"Error downloading big report for event ID: {eventId}", ex, correlationId);
            throw;
        }
    }

    public async Task<byte[]> DownloadPdfReportAsync(string eventId, string token)
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Downloading PDF report for event ID: {EventId}, correlation: {CorrelationId}", eventId, correlationId);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/product/downloadPdfReport/event_id/{eventId}");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var error = $"PDF download failed. Status: {response.StatusCode}, Content: {errorContent}";
                await _errorLogger.LogErrorAsync("PefindoApiService.DownloadPdf", error, null, correlationId);
                throw new HttpRequestException(error);
            }

            var pdfBytes = await response.Content.ReadAsByteArrayAsync();

            _logger.LogInformation("PDF report downloaded successfully. Size: {Size} bytes, correlation: {CorrelationId}",
                pdfBytes.Length, correlationId);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.DownloadPdf",
                $"Error downloading PDF report for event ID: {eventId}", ex, correlationId);
            throw;
        }
    }
}
