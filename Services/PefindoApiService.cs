using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Implementation of Pefindo PBK API client service
/// </summary>
public class PefindoApiService : IPefindoApiService
{
    private readonly HttpClient _httpClient;
    private readonly PefindoConfig _config;
    private readonly ILogger<PefindoApiService> _logger;
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public PefindoApiService(
        HttpClient httpClient,
        IOptions<PefindoConfig> config,
        ILogger<PefindoApiService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        
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
        try
        {
            _logger.LogDebug("Requesting token from Pefindo API");
            
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
            
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/getToken");
            request.Headers.Add("Authorization", $"Basic {credentials}");
            request.Headers.Add("Content-Type", "application/json");
            
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get token. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, content);
                throw new HttpRequestException($"Failed to get token: {response.StatusCode}");
            }
            
            var tokenResponse = JsonSerializer.Deserialize<PefindoTokenResponse>(content, _jsonOptions);
            
            if (tokenResponse?.Code != "01" || tokenResponse.Status != "success")
            {
                _logger.LogError("Token request failed. Code: {Code}, Status: {Status}, Message: {Message}",
                    tokenResponse?.Code, tokenResponse?.Status, tokenResponse?.Message);
                throw new InvalidOperationException($"Token request failed: {tokenResponse?.Message}");
            }
            
            _logger.LogDebug("Successfully obtained token from Pefindo API");
            return tokenResponse.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token from Pefindo API");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogDebug("Validating token with Pefindo API");
            
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/validateToken");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Content-Type", "application/json");
            
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token validation failed. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            
            var validationResponse = JsonSerializer.Deserialize<PefindoTokenResponse>(content, _jsonOptions);
            var isValid = validationResponse?.Code == "01" && validationResponse.Status == "success";
            
            _logger.LogDebug("Token validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    public async Task<PefindoSearchResponse> SearchDebtorAsync(PefindoSearchRequest request, string token)
    {
        try
        {
            _logger.LogDebug("Performing debtor search for reference: {ReferenceCode}", request.ReferenceCode);
            
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/product/search")
            {
                Content = content
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {token}");
            
            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Search response status: {StatusCode}, Content length: {ContentLength}", 
                response.StatusCode, responseContent.Length);
            
            var searchResponse = JsonSerializer.Deserialize<PefindoSearchResponse>(responseContent, _jsonOptions);
            
            if (searchResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize search response");
            }
            
            _logger.LogInformation("Search completed. Code: {Code}, Status: {Status}, Results: {Count}",
                searchResponse.Code, searchResponse.Status, searchResponse.Data.Count);
            
            return searchResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing debtor search");
            throw;
        }
    }

    public async Task<PefindoReportResponse> GenerateReportAsync(PefindoReportRequest request, string token)
    {
        try
        {
            _logger.LogDebug("Generating report for event ID: {EventId}", request.EventId);
            
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
                throw new InvalidOperationException("Failed to deserialize report generation response");
            }
            
            _logger.LogInformation("Report generation initiated. Code: {Code}, Status: {Status}, EventId: {EventId}",
                reportResponse.Code, reportResponse.Status, reportResponse.EventId);
            
            return reportResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for event ID: {EventId}", request.EventId);
            throw;
        }
    }

    public async Task<PefindoGetReportResponse> GetReportAsync(string eventId, string token)
    {
        try
        {
            _logger.LogDebug("Retrieving report for event ID: {EventId}", eventId);
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/product/getReport/event_id/{eventId}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            var reportResponse = JsonSerializer.Deserialize<PefindoGetReportResponse>(content, _jsonOptions);
            
            if (reportResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize get report response");
            }
            
            _logger.LogInformation("Report retrieval completed. Code: {Code}, Status: {Status}, EventId: {EventId}",
                reportResponse.Code, reportResponse.Status, reportResponse.EventId);
            
            return reportResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report for event ID: {EventId}", eventId);
            throw;
        }
    }

    public async Task<PefindoGetReportResponse> DownloadReportAsync(string eventId, string token, int? page = null, int? maxRecords = null)
    {
        try
        {
            _logger.LogDebug("Downloading big report for event ID: {EventId}, Page: {Page}, Max: {Max}", 
                eventId, page, maxRecords);
            
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
                throw new InvalidOperationException("Failed to deserialize download report response");
            }
            
            _logger.LogInformation("Big report download completed. Code: {Code}, Status: {Status}",
                reportResponse.Code, reportResponse.Status);
            
            return reportResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading big report for event ID: {EventId}", eventId);
            throw;
        }
    }

    public async Task<byte[]> DownloadPdfReportAsync(string eventId, string token)
    {
        try
        {
            _logger.LogDebug("Downloading PDF report for event ID: {EventId}", eventId);
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/product/downloadPdfReport/event_id/{eventId}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("PDF download failed. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"PDF download failed: {response.StatusCode}");
            }
            
            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            
            _logger.LogInformation("PDF report downloaded successfully. Size: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF report for event ID: {EventId}", eventId);
            throw;
        }
    }
}
