using System.ComponentModel.DataAnnotations;

namespace idc.pefindo.pbk.Models.Logging;

public class LogEntry
{
    public long Id { get; set; }

    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    [Required]
    public string RequestId { get; set; } = string.Empty;

    public string? UserId { get; set; }
    public string? SessionId { get; set; }

    [Required]
    public string ProcessName { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HttpRequestLogEntry
{
    public long Id { get; set; }

    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    [Required]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    public string Method { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;

    public string? RequestHeaders { get; set; }
    public string? RequestBody { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseHeaders { get; set; }
    public string? ResponseBody { get; set; }
    public int? DurationMs { get; set; }

    [Required]
    public DateTime RequestTime { get; set; }

    public DateTime? ResponseTime { get; set; }
    public bool? IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProcessStepLogEntry
{
    public long Id { get; set; }

    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    [Required]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    public string StepName { get; set; } = string.Empty;

    public int StepOrder { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    public string? InputData { get; set; }
    public string? OutputData { get; set; }
    public string? ErrorDetails { get; set; }
    public int? DurationMs { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ErrorLogEntry
{
    public long Id { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }

    [Required]
    public string LogLevel { get; set; } = string.Empty;

    [Required]
    public string Source { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }
    public string? StackTrace { get; set; }
    public string? UserId { get; set; }
    public string? AdditionalData { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogEntry
{
    public long Id { get; set; }

    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
