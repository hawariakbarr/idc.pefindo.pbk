using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Net.Sockets;

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
    private readonly IDummyResponseService? _dummyResponseService;

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
        ICorrelationService correlationService,
        IDummyResponseService? dummyResponseService = null)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _errorLogger = errorLogger;
        _correlationService = correlationService;
        _dummyResponseService = dummyResponseService;

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

        // If configured to use dummy responses, use them directly
        if (_config.UseDummyResponses && _dummyResponseService != null)
        {
            _logger.LogInformation("Using dummy response for token request, correlation: {CorrelationId}", correlationId);
            
            if (!_dummyResponseService.IsLoaded)
            {
                await _dummyResponseService.LoadDummyResponsesAsync();
            }
            
            return _dummyResponseService.GetTokenResponse("success");
        }

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
        catch (HttpRequestException httpEx) when (IsConnectionError(httpEx))
        {
            return await HandleConnectionErrorWithFallback(httpEx, correlationId, "token", () => 
                _dummyResponseService?.GetTokenResponse("success"));
        }
        catch (SocketException socketEx)
        {
            return await HandleConnectionErrorWithFallback(socketEx, correlationId, "token", () => 
                _dummyResponseService?.GetTokenResponse("success"));
        }
        catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        {
            return await HandleConnectionErrorWithFallback(timeoutEx, correlationId, "token", () => 
                _dummyResponseService?.GetTokenResponse("success"));
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

        // If configured to use dummy responses, use them directly
        if (_config.UseDummyResponses && _dummyResponseService != null)
        {
            _logger.LogInformation("Using dummy response for token validation, correlation: {CorrelationId}", correlationId);
            
            if (!_dummyResponseService.IsLoaded)
            {
                await _dummyResponseService.LoadDummyResponsesAsync();
            }
            
            var dummyResponse = _dummyResponseService.GetValidateTokenResponse("success");
            return !string.IsNullOrEmpty(dummyResponse);
        }

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
        catch (HttpRequestException httpEx) when (IsConnectionError(httpEx))
        {
            return await HandleConnectionErrorWithFallbackBool(httpEx, correlationId, "validateToken", () => 
                !string.IsNullOrEmpty(_dummyResponseService?.GetValidateTokenResponse("success")));
        }
        catch (SocketException socketEx)
        {
            return await HandleConnectionErrorWithFallbackBool(socketEx, correlationId, "validateToken", () => 
                !string.IsNullOrEmpty(_dummyResponseService?.GetValidateTokenResponse("success")));
        }
        catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        {
            return await HandleConnectionErrorWithFallbackBool(timeoutEx, correlationId, "validateToken", () => 
                !string.IsNullOrEmpty(_dummyResponseService?.GetValidateTokenResponse("success")));
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

        // If configured to use dummy responses, use them directly
        if (_config.UseDummyResponses && _dummyResponseService != null)
        {
            _logger.LogInformation("Using dummy response for search request, correlation: {CorrelationId}", correlationId);
            
            if (!_dummyResponseService.IsLoaded)
            {
                await _dummyResponseService.LoadDummyResponsesAsync();
            }
            
            var dummyResponse = _dummyResponseService.GetSearchResponse("perfectMatch");
            return JsonSerializer.Deserialize<PefindoSearchResponse>(dummyResponse, _jsonOptions) ?? 
                   throw new InvalidOperationException("Failed to deserialize dummy search response");
        }

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
        catch (HttpRequestException httpEx) when (IsConnectionError(httpEx))
        {
            return await HandleConnectionErrorWithFallbackGeneric(httpEx, correlationId, "search", () => 
            {
                var dummyResponse = _dummyResponseService?.GetSearchResponse("perfectMatch");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoSearchResponse>(dummyResponse, _jsonOptions) : null;
            });
        }
        catch (SocketException socketEx)
        {
            return await HandleConnectionErrorWithFallbackGeneric(socketEx, correlationId, "search", () => 
            {
                var dummyResponse = _dummyResponseService?.GetSearchResponse("perfectMatch");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoSearchResponse>(dummyResponse, _jsonOptions) : null;
            });
        }
        catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        {
            return await HandleConnectionErrorWithFallbackGeneric(timeoutEx, correlationId, "search", () => 
            {
                var dummyResponse = _dummyResponseService?.GetSearchResponse("perfectMatch");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoSearchResponse>(dummyResponse, _jsonOptions) : null;
            });
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

        // If configured to use dummy responses, use them directly
        if (_config.UseDummyResponses && _dummyResponseService != null)
        {
            _logger.LogInformation("Using dummy response for generate report request, correlation: {CorrelationId}", correlationId);
            
            if (!_dummyResponseService.IsLoaded)
            {
                await _dummyResponseService.LoadDummyResponsesAsync();
            }
            
            var dummyResponse = _dummyResponseService.GetGenerateReportResponse("success");
            return JsonSerializer.Deserialize<PefindoReportResponse>(dummyResponse, _jsonOptions) ?? 
                   throw new InvalidOperationException("Failed to deserialize dummy generate report response");
        }

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
        catch (HttpRequestException httpEx) when (IsConnectionError(httpEx))
        {
            return await HandleConnectionErrorWithFallbackGeneric(httpEx, correlationId, "generateReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetGenerateReportResponse("success");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoReportResponse>(dummyResponse, _jsonOptions) : null;
            });
        }
        catch (SocketException socketEx)
        {
            return await HandleConnectionErrorWithFallbackGeneric(socketEx, correlationId, "generateReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetGenerateReportResponse("success");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoReportResponse>(dummyResponse, _jsonOptions) : null;
            });
        }
        catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        {
            return await HandleConnectionErrorWithFallbackGeneric(timeoutEx, correlationId, "generateReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetGenerateReportResponse("success");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoReportResponse>(dummyResponse, _jsonOptions) : null;
            });
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

        // If configured to use dummy responses, use them directly
        if (_config.UseDummyResponses && _dummyResponseService != null)
        {
            _logger.LogInformation("Using dummy response for get report request, correlation: {CorrelationId}", correlationId);
            
            if (!_dummyResponseService.IsLoaded)
            {
                await _dummyResponseService.LoadDummyResponsesAsync();
            }
            
            var dummyResponse = _dummyResponseService.GetReportResponse("successComplete");
            return JsonSerializer.Deserialize<PefindoGetReportResponse>(dummyResponse, _jsonOptions) ?? 
                   throw new InvalidOperationException("Failed to deserialize dummy get report response");
        }

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
        catch (HttpRequestException httpEx) when (IsConnectionError(httpEx))
        {
            return await HandleConnectionErrorWithFallbackGeneric(httpEx, correlationId, "getReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetReportResponse("successComplete");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoGetReportResponse>(dummyResponse, _jsonOptions) : null;
            });
        }
        catch (SocketException socketEx)
        {
            return await HandleConnectionErrorWithFallbackGeneric(socketEx, correlationId, "getReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetReportResponse("successComplete");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoGetReportResponse>(dummyResponse, _jsonOptions) : null;
            });
        }
        catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        {
            return await HandleConnectionErrorWithFallbackGeneric(timeoutEx, correlationId, "getReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetReportResponse("successComplete");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoGetReportResponse>(dummyResponse, _jsonOptions) : null;
            });
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

        // If configured to use dummy responses, use them directly
        if (_config.UseDummyResponses && _dummyResponseService != null)
        {
            _logger.LogInformation("Using dummy response for download report request, correlation: {CorrelationId}", correlationId);
            
            if (!_dummyResponseService.IsLoaded)
            {
                await _dummyResponseService.LoadDummyResponsesAsync();
            }
            
            var dummyResponse = _dummyResponseService.GetDownloadReportResponse("success");
            return JsonSerializer.Deserialize<PefindoGetReportResponse>(dummyResponse, _jsonOptions) ?? 
                   throw new InvalidOperationException("Failed to deserialize dummy download report response");
        }

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
        catch (HttpRequestException httpEx) when (IsConnectionError(httpEx))
        {
            return await HandleConnectionErrorWithFallbackGeneric(httpEx, correlationId, "downloadReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetDownloadReportResponse("success");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoGetReportResponse>(dummyResponse, _jsonOptions) : null;
            });
        }
        catch (SocketException socketEx)
        {
            return await HandleConnectionErrorWithFallbackGeneric(socketEx, correlationId, "downloadReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetDownloadReportResponse("success");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoGetReportResponse>(dummyResponse, _jsonOptions) : null;
            });
        }
        catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        {
            return await HandleConnectionErrorWithFallbackGeneric(timeoutEx, correlationId, "downloadReport", () => 
            {
                var dummyResponse = _dummyResponseService?.GetDownloadReportResponse("success");
                return dummyResponse != null ? JsonSerializer.Deserialize<PefindoGetReportResponse>(dummyResponse, _jsonOptions) : null;
            });
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

        // If configured to use dummy responses, use them directly
        if (_config.UseDummyResponses && _dummyResponseService != null)
        {
            _logger.LogInformation("Using dummy response for PDF download request, correlation: {CorrelationId}", correlationId);
            
            if (!_dummyResponseService.IsLoaded)
            {
                await _dummyResponseService.LoadDummyResponsesAsync();
            }
            var dummyResponse = _dummyResponseService.GetDownloadPdfReportResponse("success");
            return JsonSerializer.Deserialize<byte[]>(dummyResponse, _jsonOptions) ??
                   throw new InvalidOperationException("Failed to deserialize dummy download pdf report response");
        }

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
        //catch (HttpRequestException httpEx) when (IsConnectionError(httpEx))
        //{
        //    return await HandleConnectionErrorWithFallbackBytes(httpEx, correlationId, "downloadPdfReport", () => 
        //        _dummyResponseService?.GetDownloadPdfReportResponse("success"));
        //}
        //catch (SocketException socketEx)
        //{
        //    return await HandleConnectionErrorWithFallbackBytes(socketEx, correlationId, "downloadPdfReport", () => 
        //        _dummyResponseService?.GetDownloadPdfReportResponse("success"));
        //}
        //catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        //{
        //    return await HandleConnectionErrorWithFallbackBytes(timeoutEx, correlationId, "downloadPdfReport", () => 
        //        _dummyResponseService?.GetDownloadPdfReportResponse("success"));
        //}
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("PefindoApiService.DownloadPdf",
                $"Error downloading PDF report for event ID: {eventId}", ex, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Check if the exception is a connection-related error
    /// </summary>
    private static bool IsConnectionError(Exception ex)
    {
        return ex.Message.Contains("connection") ||
               ex.Message.Contains("timeout") ||
               ex.Message.Contains("failed to respond") ||
               ex.Message.Contains("connected host has failed") ||
               ex.InnerException is SocketException;
    }

    /// <summary>
    /// Handle connection errors with fallback to dummy responses (string return)
    /// </summary>
    private async Task<string> HandleConnectionErrorWithFallback(Exception ex, string correlationId, string operation, Func<string?> getDummyResponse)
    {
        var errorMessage = $"Connection error during {operation} operation: {ex.Message}";
        await _errorLogger.LogErrorAsync($"PefindoApiService.{operation}", errorMessage, ex, correlationId);

        // Try to use dummy response as fallback
        if (_dummyResponseService != null)
        {
            try
            {
                if (!_dummyResponseService.IsLoaded)
                {
                    await _dummyResponseService.LoadDummyResponsesAsync();
                }

                var dummyResponse = getDummyResponse();
                if (!string.IsNullOrEmpty(dummyResponse))
                {
                    _logger.LogWarning("Using dummy response fallback for {Operation} due to connection error, correlation: {CorrelationId}", 
                        operation, correlationId);
                    return dummyResponse;
                }
            }
            catch (Exception dummyEx)
            {
                _logger.LogError(dummyEx, "Failed to load dummy response for {Operation} fallback, correlation: {CorrelationId}", 
                    operation, correlationId);
            }
        }

        // If no dummy response is available, rethrow the original exception
        throw ex;
    }

    /// <summary>
    /// Handle connection errors with fallback to dummy responses (bool return)
    /// </summary>
    private async Task<bool> HandleConnectionErrorWithFallbackBool(Exception ex, string correlationId, string operation, Func<bool> getDummyResponse)
    {
        var errorMessage = $"Connection error during {operation} operation: {ex.Message}";
        await _errorLogger.LogErrorAsync($"PefindoApiService.{operation}", errorMessage, ex, correlationId);

        // Try to use dummy response as fallback
        if (_dummyResponseService != null)
        {
            try
            {
                if (!_dummyResponseService.IsLoaded)
                {
                    await _dummyResponseService.LoadDummyResponsesAsync();
                }

                var dummyResponse = getDummyResponse();
                _logger.LogWarning("Using dummy response fallback for {Operation} due to connection error, correlation: {CorrelationId}", 
                    operation, correlationId);
                return dummyResponse;
            }
            catch (Exception dummyEx)
            {
                _logger.LogError(dummyEx, "Failed to load dummy response for {Operation} fallback, correlation: {CorrelationId}", 
                    operation, correlationId);
            }
        }

        // If no dummy response is available, return false
        return false;
    }

    /// <summary>
    /// Handle connection errors with fallback to dummy responses (generic return)
    /// </summary>
    private async Task<T> HandleConnectionErrorWithFallbackGeneric<T>(Exception ex, string correlationId, string operation, Func<T?> getDummyResponse) where T : class
    {
        var errorMessage = $"Connection error during {operation} operation: {ex.Message}";
        await _errorLogger.LogErrorAsync($"PefindoApiService.{operation}", errorMessage, ex, correlationId);

        // Try to use dummy response as fallback
        if (_dummyResponseService != null)
        {
            try
            {
                if (!_dummyResponseService.IsLoaded)
                {
                    await _dummyResponseService.LoadDummyResponsesAsync();
                }

                var dummyResponse = getDummyResponse();
                if (dummyResponse != null)
                {
                    _logger.LogWarning("Using dummy response fallback for {Operation} due to connection error, correlation: {CorrelationId}", 
                        operation, correlationId);
                    return dummyResponse;
                }
            }
            catch (Exception dummyEx)
            {
                _logger.LogError(dummyEx, "Failed to load dummy response for {Operation} fallback, correlation: {CorrelationId}", 
                    operation, correlationId);
            }
        }

        // If no dummy response is available, rethrow the original exception
        throw ex;
    }

    /// <summary>
    /// Handle connection errors with fallback to dummy responses (byte array return)
    /// </summary>
    private async Task<byte[]> HandleConnectionErrorWithFallbackBytes(Exception ex, string correlationId, string operation, Func<byte[]?> getDummyResponse)
    {
        var errorMessage = $"Connection error during {operation} operation: {ex.Message}";
        await _errorLogger.LogErrorAsync($"PefindoApiService.{operation}", errorMessage, ex, correlationId);

        // Try to use dummy response as fallback
        if (_dummyResponseService != null)
        {
            try
            {
                if (!_dummyResponseService.IsLoaded)
                {
                    await _dummyResponseService.LoadDummyResponsesAsync();
                }

                var dummyResponse = getDummyResponse();
                if (dummyResponse != null)
                {
                    _logger.LogWarning("Using dummy response fallback for {Operation} due to connection error, correlation: {CorrelationId}", 
                        operation, correlationId);
                    return dummyResponse;
                }
            }
            catch (Exception dummyEx)
            {
                _logger.LogError(dummyEx, "Failed to load dummy response for {Operation} fallback, correlation: {CorrelationId}", 
                    operation, correlationId);
            }
        }

        // If no dummy response is available, rethrow the original exception
        throw ex;
    }
}
