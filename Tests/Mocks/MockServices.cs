using System.Data.Common;
using System.Text.Json;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;

namespace idc.pefindo.pbk.Tests.Integration;

public class MockDbConnectionFactory : IDbConnectionFactory
{
    public Task<DbConnection> CreateConnectionAsync()
    {
        throw new InvalidOperationException("Database not available in test environment. Use mock data instead.");
    }
}

public class MockGlobalConfigRepository : IGlobalConfigRepository
{
    private readonly Dictionary<string, string> _configs = new()
    {
        { "GC31", "7" },
        { "GC35", "0.8" },
        { "GC36", "0.7" },
        { "GC39", "60" }
    };

    public Task<string?> GetConfigValueAsync(string configCode)
    {
        _configs.TryGetValue(configCode, out var value);
        return Task.FromResult(value);
    }

    public Task<Dictionary<string, string>> GetMultipleConfigsAsync(params string[] configCodes)
    {
        var result = new Dictionary<string, string>();
        foreach (var code in configCodes)
        {
            if (_configs.TryGetValue(code, out var value))
                result[code] = value;
        }
        return Task.FromResult(result);
    }
}

public class MockPbkDataRepository : IPbkDataRepository
{
    public Task<int> StoreSearchResultAsync(string appNo, int inquiryId, PefindoSearchResponse searchResponse)
        => Task.FromResult(1);

    public Task<int> StoreReportDataAsync(string eventId, string appNo, int inquiryId, PefindoGetReportResponse reportResponse, string? pdfPath = null)
        => Task.FromResult(1);

    public Task<int> StoreSummaryDataAsync(string appNo, IndividualData summaryData, string? pefindoId = null, string? searchId = null, string? eventId = null)
        => Task.FromResult(1);

    public Task<IndividualData?> GetSummaryDataAsync(string appNo)
        => Task.FromResult<IndividualData?>(null);

    public Task<int> LogProcessingStepAsync(string appNo, string stepName, string status, JsonDocument? stepData = null, string? errorMessage = null, int? processingTimeMs = null)
        => Task.FromResult(1);

    public Task<List<ProcessingLogEntry>> GetProcessingLogAsync(string appNo)
        => Task.FromResult(new List<ProcessingLogEntry>());
}

public class MockPefindoApiService : IPefindoApiService
{
    public Task<string> GetTokenAsync()
        => Task.FromResult("mock_token_12345");

    public Task<bool> ValidateTokenAsync(string token)
        => Task.FromResult(true);

    public Task<PefindoSearchResponse> SearchDebtorAsync(PefindoSearchRequest request, string token)
        => Task.FromResult(new PefindoSearchResponse 
        { 
            Code = "31", 
            Status = "failed", 
            Message = "Mock: No data found in test environment" 
        });

    public Task<PefindoReportResponse> GenerateReportAsync(PefindoReportRequest request, string token)
        => Task.FromResult(new PefindoReportResponse());

    public Task<PefindoGetReportResponse> GetReportAsync(string eventId, string token)
        => Task.FromResult(new PefindoGetReportResponse());

    public Task<PefindoGetReportResponse> DownloadReportAsync(string eventId, string token, int? page = null, int? maxRecords = null)
        => Task.FromResult(new PefindoGetReportResponse());

    public Task<byte[]> DownloadPdfReportAsync(string eventId, string token)
        => Task.FromResult(new byte[0]);
}
