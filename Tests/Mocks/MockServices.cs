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
    private readonly Dictionary<string, string> _eventIdToInquiryId = new();
    private readonly Dictionary<string, DateTime> _eventIdToCreatedTime = new();
    private readonly Dictionary<string, bool> _eventIdToReportReady = new();

    public Task<string> GetTokenAsync()
    {
        // Simulate different scenarios based on current time for testing
        var now = DateTime.UtcNow;

        // Simulate token generation success (most common case)
        if (now.Millisecond % 100 < 85) // 85% success rate
        {
            return Task.FromResult($$"""
            {
                "code": "01",
                "status": "success",
                "message": "Token aktif",
                "data": {
                    "valid_date": "{{DateTime.UtcNow.AddHours(1):yyyy}}{{DateTime.UtcNow.AddHours(1).DayOfYear:D3}}{{DateTime.UtcNow.AddHours(1):HHmmss}}00",
                    "token": "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICI5SFh...mock_token_{{DateTime.UtcNow.Ticks}}"
                }
            }
            """);
        }

        // Simulate authentication failure (10% chance)
        if (now.Millisecond % 100 < 95)
        {
            return Task.FromResult("""
            {
                "code": "13",
                "status": "failed",
                "message": "username atau password salah"
            }
            """);
        }

        // Simulate IP access denied (5% chance)
        return Task.FromResult("""
        {
            "code": "17",
            "status": "failed",
            "message": "akses ditolak"
        }
        """);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        // Simulate token validation
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(false);

        // Valid tokens: mock tokens, test tokens, and specific test patterns
        var isValid = token.StartsWith("mock_token_") ||
                     token.StartsWith("eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICI5SFh") ||
                     token.Contains("test-token") ||
                     token.Contains("cached-token") ||
                     token.Contains("valid-token") ||
                     token.Contains("info-token") ||
                     token.Contains("token-to-invalidate");

        return Task.FromResult(isValid);
    }

    public Task<PefindoSearchResponse> SearchDebtorAsync(PefindoSearchRequest request, string token)
    {
        // Validate token first
        if (!ValidateTokenAsync(token).Result)
        {
            return Task.FromResult(new PefindoSearchResponse
            {
                Code = "06",
                Status = "failed",
                Message = "Invalid Token"
            });
        }

        // Simulate various search scenarios based on test data
        var firstParam = request.Params.FirstOrDefault();
        if (firstParam == null)
        {
            return Task.FromResult(new PefindoSearchResponse
            {
                Code = "21",
                Status = "failed",
                Message = "Parameter wajib diisi"
            });
        }

        // Test case 1: Perfect match scenario (KTP: 1234567890123456)
        if (firstParam.IdNo == "1234567890123456")
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
                        IdPefindo = 101110967000498,
                        SimilarityScore = 100.0m,
                        NamaDebitur = "INDIVIDU 101110967000498",
                        IdNo = "1234567890123456",
                        IdType = "KTP",
                        TanggalLahir = "1967-09-11",
                        Npwp = "101110967000498",
                        Alamat = "ALAMAT 101110967000498",
                        NamaGadisIbuKandung = "IBU 101110967000498",
                        ResponseStatus = "ALL_CORRECT"
                    }
                }
            });
        }

        // Test case 2: Multiple matches with different similarity scores
        if (firstParam.IdNo == "3150972902880002")
        {
            return Task.FromResult(new PefindoSearchResponse
            {
                Code = "01",
                Status = "success",
                Message = "Data ditemukan",
                InquiryId = 318,
                Data = new List<PefindoSearchData>
                {
                    new()
                    {
                        IdPefindo = 101110967000498,
                        SimilarityScore = 100.0m,
                        NamaDebitur = "INDIVIDU 101110967000498",
                        IdNo = "3150972902880002",
                        IdType = "KTP",
                        TanggalLahir = "1967-09-11",
                        Npwp = "101110967000498",
                        Alamat = "ALAMAT 101110967000498",
                        NamaGadisIbuKandung = "IBU 101110967000498",
                        ResponseStatus = "ALL_CORRECT"
                    },
                    new()
                    {
                        IdPefindo = 101110967000499,
                        SimilarityScore = 85.5m,
                        NamaDebitur = "INDIVIDU SERUPA 101110967000499",
                        IdNo = "3101110967000499",
                        IdType = "KTP",
                        TanggalLahir = "1967-09-12",
                        Npwp = "101110967000499",
                        Alamat = "ALAMAT SERUPA 101110967000499",
                        NamaGadisIbuKandung = "IBU SERUPA 101110967000499",
                        ResponseStatus = "SIMILARITY_CHECK_REQUIRED"
                    }
                }
            });
        }

        // Test case 3: Low similarity match
        if (firstParam.IdNo == "9999999999999999")
        {
            return Task.FromResult(new PefindoSearchResponse
            {
                Code = "01",
                Status = "success",
                Message = "Data ditemukan",
                InquiryId = 999,
                Data = new List<PefindoSearchData>
                {
                    new()
                    {
                        IdPefindo = 999999999,
                        SimilarityScore = 65.0m,
                        NamaDebitur = "NAMA BERBEDA",
                        IdNo = "9999999999999999",
                        IdType = "KTP",
                        TanggalLahir = "1990-01-01",
                        Npwp = "999999999",
                        Alamat = "ALAMAT BERBEDA",
                        NamaGadisIbuKandung = "IBU BERBEDA",
                        ResponseStatus = "LOW_SIMILARITY"
                    }
                }
            });
        }

        // Test case 4: Corporate search (NPWP)
        if (firstParam.IdType == "NPWP" && firstParam.IdNo == "555666777888999")
        {
            return Task.FromResult(new PefindoSearchResponse
            {
                Code = "01",
                Status = "success",
                Message = "Data ditemukan",
                InquiryId = 777,
                Data = new List<PefindoSearchData>
                {
                    new()
                    {
                        IdPefindo = 555666777888999,
                        SimilarityScore = 95.0m,
                        NamaDebitur = "PT PERTAMBANGAN INDONESIA",
                        IdNo = "555666777888999",
                        IdType = "NPWP",
                        TanggalLahir = null,
                        Npwp = "555666777888999",
                        Alamat = "JAKARTA SELATAN",
                        NamaGadisIbuKandung = null,
                        ResponseStatus = "ALL_CORRECT"
                    }
                }
            });
        }

        // Default case: Data not found
        return Task.FromResult(new PefindoSearchResponse
        {
            Code = "31",
            Status = "failed",
            Message = "Data tidak ditemukan"
        });
    }

    public Task<PefindoReportResponse> GenerateReportAsync(PefindoReportRequest request, string token)
    {
        // Validate token
        if (!ValidateTokenAsync(token).Result)
        {
            return Task.FromResult(new PefindoReportResponse
            {
                Code = "06",
                Status = "failed",
                Message = "Invalid Token"
            });
        }

        // Check if event_id already exists
        if (_eventIdToInquiryId.ContainsKey(request.EventId))
        {
            return Task.FromResult(new PefindoReportResponse
            {
                Code = "35",
                Status = "failed",
                EventId = request.EventId,
                Message = "event_id sudah ada, gunakan yang lain"
            });
        }

        // Store event_id mapping
        _eventIdToInquiryId[request.EventId] = request.InquiryId.ToString();
        _eventIdToCreatedTime[request.EventId] = DateTime.UtcNow;
        _eventIdToReportReady[request.EventId] = false;

        // Mark report as ready after a short delay (simulate async processing)
        Task.Delay(100).ContinueWith(_ => _eventIdToReportReady[request.EventId] = true);

        return Task.FromResult(new PefindoReportResponse
        {
            Code = "01",
            Status = "success",
            EventId = request.EventId,
            Message = "Proses membuat report sedang dikerjakan"
        });
    }

    public Task<PefindoGetReportResponse> GetReportAsync(string eventId, string token)
    {
        // Validate token
        if (!ValidateTokenAsync(token).Result)
        {
            return Task.FromResult(new PefindoGetReportResponse
            {
                Code = "06",
                Status = "failed",
                Message = "Invalid Token"
            });
        }

        // Check if event_id exists
        if (!_eventIdToInquiryId.ContainsKey(eventId))
        {
            return Task.FromResult(new PefindoGetReportResponse
            {
                Code = "34",
                Status = "failed",
                Message = "Request id tidak ditemukan"
            });
        }

        // Check if report is still processing
        if (!_eventIdToReportReady.GetValueOrDefault(eventId, false))
        {
            return Task.FromResult(new PefindoGetReportResponse
            {
                Code = "32",
                Status = "processing",
                EventId = eventId,
                Message = "Laporan masih dalam proses scoring"
            });
        }

        // Simulate big report scenario (5% chance)
        if (eventId.Contains("big-report") || DateTime.UtcNow.Millisecond % 100 < 5)
        {
            return Task.FromResult(new PefindoGetReportResponse
            {
                Code = "36",
                Status = "success",
                EventId = eventId,
                Message = "Kategori big report, gunakan method downloadReport"
            });
        }

        // Return complete report with comprehensive data
        return Task.FromResult(new PefindoGetReportResponse
        {
            Code = "01",
            Status = "success",
            EventId = eventId,
            Message = "Laporan berhasil dibuat",
            Report = new PefindoReportData
            {
                Header = new PefindoReportHeader
                {
                    IdReport = Guid.NewGuid().ToString(),
                    IdscoreId = "SC123456",
                    Username = "pbk_user",
                    TglPermintaan = DateTime.UtcNow,
                    NoReferensiDokumen = "REF-" + eventId,
                    Ktp = "1234567890123456",
                    Npwp = "101110967000498",
                    NamaDebitur = "INDIVIDU 101110967000498",
                    TanggalLahir = DateTime.Parse("1967-09-11"),
                    TempatLahir = "JAKARTA"
                },
                Debitur = new PefindoDebiturInfo
                {
                    IdPefindo = 101110967000498,
                    NamaDebitur = "INDIVIDU 101110967000498",
                    AlamatDebitur = "ALAMAT 101110967000498",
                    Email = "individu@email.com",
                    NamaGadisIbuKandung = "IBU 101110967000498",
                    JmlFasilitas = 5,
                    MaxCurrDpd = 0,
                    MaxOverdueLast12Months = 0,
                    JmlPlafon = 500000m,
                    TotalAngsuranAktif = 75000m,
                    WoContract = 0,
                    KualitasKreditTerburuk = "1",
                    TotalBakiDebet = 425000m,
                    TotalNilaiAgunan = 600000m
                },
                ScoreInfo = new PefindoScoreInfo
                {
                    Score = "750",
                    RiskGrade = "A",
                    RiskDesc = "Low Risk",
                    ScoreDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
                },
                Fasilitas = new List<PefindoFasilitas>
                {
                    new()
                    {
                        IdFasilitas = 1,
                        JenisFasilitas = "KREDIT",
                        NamaBank = "BANK MANDIRI",
                        Plafon = 200000m,
                        BakiDebet = 150000m,
                        DpdCurrent = 0,
                        KualitasKredit = "1",
                        TanggalMulai = DateTime.UtcNow.AddYears(-2),
                        TanggalBerakhir = DateTime.UtcNow.AddYears(3)
                    },
                    new()
                    {
                        IdFasilitas = 2,
                        JenisFasilitas = "KARTU KREDIT",
                        NamaBank = "BANK BCA",
                        Plafon = 50000m,
                        BakiDebet = 25000m,
                        DpdCurrent = 0,
                        KualitasKredit = "1",
                        TanggalMulai = DateTime.UtcNow.AddYears(-1),
                        TanggalBerakhir = DateTime.UtcNow.AddYears(2)
                    }
                }
            }
        });
    }

    public Task<PefindoGetReportResponse> DownloadReportAsync(string eventId, string token, int? page = null, int? maxRecords = null)
    {
        // This is for big reports - return the same structure but with pagination info
        var response = GetReportAsync(eventId, token).Result;

        if (response.Code == "01")
        {
            // Add pagination metadata to message
            response.Message = $"Laporan berhasil dibuat (Page: {page ?? 1}, MaxRecords: {maxRecords ?? 100})";
        }

        return Task.FromResult(response);
    }

    public Task<byte[]> DownloadPdfReportAsync(string eventId, string token)
    {
        // Validate token
        if (!ValidateTokenAsync(token).Result)
        {
            return Task.FromResult(Encoding.UTF8.GetBytes("Invalid Token"));
        }

        // Check if event_id exists
        if (!_eventIdToInquiryId.ContainsKey(eventId))
        {
            return Task.FromResult(Encoding.UTF8.GetBytes("Request id tidak ditemukan"));
        }

        // Generate mock PDF content
        var pdfContent = $@"%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages 2 0 R >>
endobj

2 0 obj
<< /Type /Pages /Kids [3 0 R] /Count 1 >>
endobj

3 0 obj
<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>
endobj

4 0 obj
<< /Length 44 >>
stream
BT
/F1 12 Tf
100 700 Td
(PEFINDO PBK Report - Event: {eventId}) Tj
ET
endstream
endobj

xref
0 5
0000000000 65535 f
0000000009 00000 n
0000000058 00000 n
0000000115 00000 n
0000000206 00000 n
trailer
<< /Size 5 /Root 1 0 R >>
startxref
294
%%EOF";

        return Task.FromResult(Encoding.UTF8.GetBytes(pdfContent));
    }
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
