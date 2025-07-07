using idc.pefindo.pbk.Models.Logging;

namespace idc.pefindo.pbk.Services.Interfaces.Logging;

public interface IHttpRequestLogger
{
    Task LogRequestAsync(HttpRequestLogEntry entry);
    Task<List<HttpRequestLogEntry>> GetRequestLogsByCorrelationIdAsync(string correlationId);
}

public interface IProcessStepLogger
{
    Task LogStepStartAsync(string correlationId, string requestId, string stepName, int stepOrder, object? inputData = null);
    Task LogStepCompleteAsync(string correlationId, string requestId, string stepName, object? outputData = null, int? durationMs = null);
    Task LogStepFailAsync(string correlationId, string requestId, string stepName, Exception ex, object? inputData = null, int? durationMs = null);
    Task<List<ProcessStepLogEntry>> GetProcessLogsByCorrelationIdAsync(string correlationId);
}

public interface IErrorLogger
{
    Task LogErrorAsync(string source, string message, Exception? exception = null, string? correlationId = null, object? additionalData = null);
    Task LogWarningAsync(string source, string message, string? correlationId = null, object? additionalData = null);
    Task LogCriticalAsync(string source, string message, Exception? exception = null, string? correlationId = null, object? additionalData = null);
}

public interface IAuditLogger
{
    Task LogActionAsync(string correlationId, string userId, string action, string? entityType = null, string? entityId = null, object? oldValue = null, object? newValue = null, string? ipAddress = null, string? userAgent = null);
}

public interface ICorrelationService
{
    string GetCorrelationId();
    string GetRequestId();
    void SetCorrelationContext(string correlationId, string requestId, string? userId = null);
}
