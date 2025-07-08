using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Models.Logging;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace idc.pefindo.pbk.Tests.Integration;

public class MockDbConnectionFactory : IDbConnectionFactory
{
    private readonly Dictionary<string, string> _availableDatabases = new()
    {
        { DatabaseKeys.Core, "idc.core_test" },
        { DatabaseKeys.En, "idc.en_test" },
        { DatabaseKeys.Bk, "idc.bk_test" }
    };

    public Task<DbConnection> CreateConnectionAsync()
    {
        throw new InvalidOperationException("Database not available in test environment. Use mock data instead.");
    }

    public Task<DbConnection> CreateConnectionAsync(string databaseKey)
    {
        if (!_availableDatabases.ContainsKey(databaseKey))
        {
            throw new ArgumentException($"Database key '{databaseKey}' not found in test configuration");
        }

        throw new InvalidOperationException($"Mock database connection for '{databaseKey}' not implemented. Use mock repositories instead.");
    }

    public IEnumerable<string> GetAvailableDatabaseKeys()
    {
        return _availableDatabases.Keys;
    }


    public Task<Dictionary<string, bool>> ValidateAllConnectionsAsync()
    {
        // Return all databases as healthy for testing
        var result = _availableDatabases.ToDictionary(
            kvp => kvp.Key,
            kvp => true
        );
        return Task.FromResult(result);
    }
}

public class MockGlobalConfigRepository : IGlobalConfigRepository
{
    private readonly Dictionary<string, string> _configs = new()
    {
        { "GC31", "7" },        // Cycle day
        { "GC33", "3" },        // Similarity check version
        { "GC34", "2" },        // Table version
        { "GC35", "0.8" },      // Name threshold
        { "GC36", "0.7" },      // Mother name threshold
        { "GC39", "60" }        // Token cache duration
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
    private static int _idCounter = 1;

    public Task<int> StoreSearchResultAsync(string appNo, int inquiryId, PefindoSearchResponse searchResponse)
        => Task.FromResult(_idCounter++);

    public Task<int> StoreReportDataAsync(string eventId, string appNo, int inquiryId, PefindoGetReportResponse reportResponse, string? pdfPath = null)
        => Task.FromResult(_idCounter++);

    public Task<int> StoreSummaryDataAsync(string appNo, IndividualData summaryData, string? pefindoId = null, string? searchId = null, string? eventId = null)
        => Task.FromResult(_idCounter++);

    public Task<IndividualData?> GetSummaryDataAsync(string appNo)
        => Task.FromResult<IndividualData?>(null);

    public Task<int> LogProcessingStepAsync(string appNo, string stepName, string status, JsonDocument? stepData = null, string? errorMessage = null, int? processingTimeMs = null)
        => Task.FromResult(_idCounter++);

    public Task<List<ProcessingLogEntry>> GetProcessingLogAsync(string appNo)
        => Task.FromResult(new List<ProcessingLogEntry>
        {
            new()
            {
                StepName = "cycle_day_validation",
                StepStatus = "SUCCESS",
                CreatedDate = DateTime.UtcNow.AddMinutes(-5),
                ProcessingTimeMs = 100
            },
            new()
            {
                StepName = "smart_search",
                StepStatus = "SUCCESS",
                CreatedDate = DateTime.UtcNow.AddMinutes(-4),
                ProcessingTimeMs = 500
            }
        });
}

public class MockPefindoApiService : IPefindoApiService
{
    public Task<string> GetTokenAsync()
        => Task.FromResult("mock_token_12345_" + DateTime.UtcNow.Ticks);

    public Task<bool> ValidateTokenAsync(string token)
        => Task.FromResult(token.StartsWith("mock_token_"));

    public Task<PefindoSearchResponse> SearchDebtorAsync(PefindoSearchRequest request, string token)
    {
        // Simulate different responses based on test data
        if (request.Params.Any(p => p.IdNo == "1234567890123456"))
        {
            return Task.FromResult(new PefindoSearchResponse
            {
                Code = "01",
                Status = "success",
                Message = "Data ditemukan",
                InquiryId = 12345,
                Data = new List<PefindoSearchData>
                {
                    new()
                    {
                        IdPefindo = 9999999,
                        SimilarityScore = 95.5m,
                        NamaDebitur = "John Doe",
                        IdNo = "1234567890123456",
                        IdType = "KTP",
                        ResponseStatus = "ALL_CORRECT"
                    }
                }
            });
        }

        return Task.FromResult(new PefindoSearchResponse
        {
            Code = "31",
            Status = "failed",
            Message = "Data tidak ditemukan"
        });
    }

    public Task<PefindoReportResponse> GenerateReportAsync(PefindoReportRequest request, string token)
        => Task.FromResult(new PefindoReportResponse
        {
            Code = "01",
            Status = "success",
            EventId = request.EventId,
            Message = "Proses membuat report sedang dikerjakan"
        });

    public Task<PefindoGetReportResponse> GetReportAsync(string eventId, string token)
        => Task.FromResult(new PefindoGetReportResponse
        {
            Code = "01",
            Status = "success",
            EventId = eventId,
            Message = "Laporan berhasil dibuat",
            Report = new PefindoReportData
            {
                Debitur = new PefindoDebiturInfo
                {
                    IdPefindo = 9999999,
                    NamaDebitur = "John Doe",
                    JmlFasilitas = 3,
                    MaxCurrDpd = 0,
                    MaxOverdueLast12Months = 0,
                    JmlPlafon = 100000m,
                    TotalAngsuranAktif = 25000m
                },
                ScoreInfo = new PefindoScoreInfo
                {
                    Score = "750",
                    RiskGrade = "A",
                    RiskDesc = "Low Risk"
                }
            }
        });

    public Task<PefindoGetReportResponse> DownloadReportAsync(string eventId, string token, int? page = null, int? maxRecords = null)
        => GetReportAsync(eventId, token);

    public Task<byte[]> DownloadPdfReportAsync(string eventId, string token)
        => Task.FromResult(Encoding.UTF8.GetBytes($"Mock PDF Report for event {eventId}"));
}

// Mock logging services
public class MockCorrelationService : ICorrelationService
{
    private string _correlationId;
    private string _requestId;
    private string? _userId;

    public MockCorrelationService(string? correlationId = null, string? requestId = null)
    {
        _correlationId = correlationId ?? $"mock-corr-{Guid.NewGuid():N}";
        _requestId = requestId ?? $"mock-req-{Guid.NewGuid():N}";
    }

    public string GetCorrelationId() => _correlationId;
    public string GetRequestId() => _requestId;
    public void SetCorrelationContext(string correlationId, string requestId, string? userId = null)
    {
        _correlationId = correlationId;
        _requestId = requestId;
        _userId = userId;
    }
}

public class MockCorrelationLogger : ICorrelationLogger
{
    private readonly List<LogEntry> _logEntries = new();

    public Task LogProcessStartAsync(string correlationId, string requestId, string processName, string? userId = null, string? sessionId = null)
    {
        var entry = new LogEntry
        {
            Id = _logEntries.Count + 1,
            CorrelationId = correlationId,
            RequestId = requestId,
            ProcessName = processName,
            UserId = userId,
            SessionId = sessionId,
            StartTime = DateTime.UtcNow,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow
        };
        _logEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogProcessCompleteAsync(string correlationId, string status = "Success")
    {
        var entry = _logEntries.FirstOrDefault(e => e.CorrelationId == correlationId);
        if (entry != null)
        {
            entry.Status = status;
            entry.EndTime = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task LogProcessFailAsync(string correlationId, string status = "Failed", string? errorMessage = null)
    {
        var entry = _logEntries.FirstOrDefault(e => e.CorrelationId == correlationId);
        if (entry != null)
        {
            entry.Status = status;
            entry.EndTime = DateTime.UtcNow;
            entry.ErrorMessage = errorMessage;
        }
        return Task.CompletedTask;
    }

    public Task UpdateProcessStatusAsync(string correlationId, string status)
    {
        var entry = _logEntries.FirstOrDefault(e => e.CorrelationId == correlationId);
        if (entry != null)
        {
            entry.Status = status;
        }
        return Task.CompletedTask;
    }

    public Task<LogEntry?> GetLogEntryAsync(string correlationId)
    {
        var entry = _logEntries.FirstOrDefault(e => e.CorrelationId == correlationId);
        return Task.FromResult(entry);
    }

    public List<LogEntry> GetAllLogEntries() => _logEntries;
}

public class MockProcessStepLogger : IProcessStepLogger
{
    private readonly List<ProcessStepLogEntry> _stepLogs = new();

    public Task LogStepStartAsync(string correlationId, string requestId, string stepName, int stepOrder, object? inputData = null)
    {
        var entry = new ProcessStepLogEntry
        {
            Id = _stepLogs.Count + 1,
            CorrelationId = correlationId,
            RequestId = requestId,
            StepName = stepName,
            StepOrder = stepOrder,
            Status = "Started",
            InputData = inputData != null ? JsonSerializer.Serialize(inputData) : null,
            StartTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _stepLogs.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogStepCompleteAsync(string correlationId, string requestId, string stepName, object? outputData = null, int? durationMs = null)
    {
        var entry = _stepLogs.LastOrDefault(e => e.CorrelationId == correlationId && e.StepName == stepName);
        if (entry != null)
        {
            entry.Status = "Completed";
            entry.OutputData = outputData != null ? JsonSerializer.Serialize(outputData) : null;
            entry.DurationMs = durationMs;
            entry.EndTime = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task LogStepFailAsync(string correlationId, string requestId, string stepName, Exception ex, object? inputData = null, int? durationMs = null)
    {
        var entry = _stepLogs.LastOrDefault(e => e.CorrelationId == correlationId && e.StepName == stepName);
        if (entry != null)
        {
            entry.Status = "Failed";
            entry.ErrorDetails = ex.Message;
            entry.DurationMs = durationMs;
            entry.EndTime = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<List<ProcessStepLogEntry>> GetProcessLogsByCorrelationIdAsync(string correlationId)
    {
        var logs = _stepLogs.Where(e => e.CorrelationId == correlationId).ToList();
        return Task.FromResult(logs);
    }

    public List<ProcessStepLogEntry> GetStepLogs() => _stepLogs;
}

public class MockHttpRequestLogger : IHttpRequestLogger
{
    private readonly List<HttpRequestLogEntry> _httpLogs = new();

    public Task LogRequestAsync(HttpRequestLogEntry entry)
    {
        entry.Id = _httpLogs.Count + 1;
        entry.CreatedAt = DateTime.UtcNow;
        _httpLogs.Add(entry);
        return Task.CompletedTask;
    }

    public Task<List<HttpRequestLogEntry>> GetRequestLogsByCorrelationIdAsync(string correlationId)
    {
        var logs = _httpLogs.Where(e => e.CorrelationId == correlationId).ToList();
        return Task.FromResult(logs);
    }

    public List<HttpRequestLogEntry> GetHttpLogs() => _httpLogs;
}

public class MockErrorLogger : IErrorLogger
{
    private readonly List<ErrorLogEntry> _errorLogs = new();

    public Task LogErrorAsync(string source, string message, Exception? exception = null, string? correlationId = null, object? additionalData = null)
    {
        var entry = new ErrorLogEntry
        {
            Id = _errorLogs.Count + 1,
            CorrelationId = correlationId,
            LogLevel = "Error",
            Source = source,
            Message = message,
            Exception = exception?.ToString(),
            StackTrace = exception?.StackTrace,
            AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
            CreatedAt = DateTime.UtcNow
        };
        _errorLogs.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogWarningAsync(string source, string message, string? correlationId = null, object? additionalData = null)
    {
        var entry = new ErrorLogEntry
        {
            Id = _errorLogs.Count + 1,
            CorrelationId = correlationId,
            LogLevel = "Warning",
            Source = source,
            Message = message,
            AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
            CreatedAt = DateTime.UtcNow
        };
        _errorLogs.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogCriticalAsync(string source, string message, Exception? exception = null, string? correlationId = null, object? additionalData = null)
    {
        var entry = new ErrorLogEntry
        {
            Id = _errorLogs.Count + 1,
            CorrelationId = correlationId,
            LogLevel = "Critical",
            Source = source,
            Message = message,
            Exception = exception?.ToString(),
            StackTrace = exception?.StackTrace,
            AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
            CreatedAt = DateTime.UtcNow
        };
        _errorLogs.Add(entry);
        return Task.CompletedTask;
    }

    public List<ErrorLogEntry> GetErrorLogs() => _errorLogs;
}

public class MockAuditLogger : IAuditLogger
{
    private readonly List<AuditLogEntry> _auditLogs = new();

    public Task LogActionAsync(string correlationId, string userId, string action, string? entityType = null, string? entityId = null, object? oldValue = null, object? newValue = null, string? ipAddress = null, string? userAgent = null)
    {
        var entry = new AuditLogEntry
        {
            Id = _auditLogs.Count + 1,
            CorrelationId = correlationId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
            NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
        _auditLogs.Add(entry);
        return Task.CompletedTask;
    }

    public List<AuditLogEntry> GetAuditLogs() => _auditLogs;
}
