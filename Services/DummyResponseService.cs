using idc.pefindo.pbk.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Service for managing dummy API responses for development and testing
/// </summary>
public class DummyResponseService : IDummyResponseService
{
    private readonly ILogger<DummyResponseService> _logger;
    private readonly string _dummyResponseFilePath;
    private JsonDocument? _dummyResponses;
    private readonly object _lockObject = new();

    public bool IsLoaded => _dummyResponses != null;

    public DummyResponseService(ILogger<DummyResponseService> logger, string dummyResponseFilePath)
    {
        _logger = logger;
        _dummyResponseFilePath = dummyResponseFilePath;
    }

    public async Task LoadDummyResponsesAsync()
    {
        try
        {
            lock (_lockObject)
            {
                if (_dummyResponses != null)
                {
                    _logger.LogDebug("Dummy responses already loaded");
                    return;
                }
            }

            _logger.LogInformation("Loading dummy responses from: {FilePath}", _dummyResponseFilePath);

            if (!File.Exists(_dummyResponseFilePath))
            {
                throw new FileNotFoundException($"Dummy response file not found: {_dummyResponseFilePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(_dummyResponseFilePath);
            
            lock (_lockObject)
            {
                _dummyResponses = JsonDocument.Parse(jsonContent);
            }

            _logger.LogInformation("Successfully loaded dummy responses from file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dummy responses from file: {FilePath}", _dummyResponseFilePath);
            throw;
        }
    }

    public string GetTokenResponse(string scenario = "success")
    {
        EnsureLoaded();
        
        var tokenResponses = _dummyResponses!.RootElement.GetProperty("getToken");
        if (tokenResponses.TryGetProperty(scenario, out var response))
        {
            return response.GetRawText();
        }

        _logger.LogWarning("Token scenario '{Scenario}' not found, using default success", scenario);
        return tokenResponses.GetProperty("success").GetRawText();
    }

    public string GetValidateTokenResponse(string scenario = "success")
    {
        EnsureLoaded();
        
        var validateResponses = _dummyResponses!.RootElement.GetProperty("validateToken");
        if (validateResponses.TryGetProperty(scenario, out var response))
        {
            return response.GetRawText();
        }

        _logger.LogWarning("ValidateToken scenario '{Scenario}' not found, using default success", scenario);
        return validateResponses.GetProperty("success").GetRawText();
    }

    public string GetSearchResponse(string scenario = "perfectMatch")
    {
        EnsureLoaded();
        
        var searchResponses = _dummyResponses!.RootElement.GetProperty("search");
        if (searchResponses.TryGetProperty(scenario, out var response))
        {
            return response.GetRawText();
        }

        _logger.LogWarning("Search scenario '{Scenario}' not found, using default perfectMatch", scenario);
        return searchResponses.GetProperty("perfectMatch").GetRawText();
    }

    public string GetGenerateReportResponse(string scenario = "success")
    {
        EnsureLoaded();
        
        var generateResponses = _dummyResponses!.RootElement.GetProperty("generateReport");
        if (generateResponses.TryGetProperty(scenario, out var response))
        {
            return response.GetRawText();
        }

        _logger.LogWarning("GenerateReport scenario '{Scenario}' not found, using default success", scenario);
        return generateResponses.GetProperty("success").GetRawText();
    }

    public string GetReportResponse(string scenario = "successComplete")
    {
        EnsureLoaded();
        
        var reportResponses = _dummyResponses!.RootElement.GetProperty("getReport");
        if (reportResponses.TryGetProperty(scenario, out var response))
        {
            return response.GetRawText();
        }

        _logger.LogWarning("GetReport scenario '{Scenario}' not found, using default successComplete", scenario);
        return reportResponses.GetProperty("successComplete").GetRawText();
    }

    public string GetDownloadReportResponse(string scenario = "success")
    {
        EnsureLoaded();
        
        var downloadResponses = _dummyResponses!.RootElement.GetProperty("downloadReport");
        if (downloadResponses.TryGetProperty(scenario, out var response))
        {
            return response.GetRawText();
        }

        _logger.LogWarning("DownloadReport scenario '{Scenario}' not found, using default success", scenario);
        return downloadResponses.GetProperty("success").GetRawText();
    }

    public byte[] GetPdfReportResponse(string scenario = "success")
    {
        EnsureLoaded();
        
        var pdfResponses = _dummyResponses!.RootElement.GetProperty("downloadPdfReport");
        if (pdfResponses.TryGetProperty(scenario, out var response))
        {
            if (response.TryGetProperty("binaryData", out var binaryData))
            {
                return Encoding.UTF8.GetBytes(binaryData.GetString() ?? "");
            }
        }

        _logger.LogWarning("PDF scenario '{Scenario}' not found, using default", scenario);
        var defaultResponse = pdfResponses.GetProperty("success").GetProperty("binaryData").GetString() ?? "";
        return Encoding.UTF8.GetBytes(defaultResponse);
    }

    public string GetBulkResponse(string scenario = "success")
    {
        EnsureLoaded();
        
        var bulkResponses = _dummyResponses!.RootElement.GetProperty("bulk");
        if (bulkResponses.TryGetProperty(scenario, out var response))
        {
            return response.GetRawText();
        }

        _logger.LogWarning("Bulk scenario '{Scenario}' not found, using default success", scenario);
        return bulkResponses.GetProperty("success").GetRawText();
    }

    private void EnsureLoaded()
    {
        if (_dummyResponses == null)
        {
            throw new InvalidOperationException("Dummy responses not loaded. Call LoadDummyResponsesAsync() first.");
        }
    }

    public void Dispose()
    {
        _dummyResponses?.Dispose();
    }
}