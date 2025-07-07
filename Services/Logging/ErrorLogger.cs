using System.Data.Common;
using System.Text.Json;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models.Logging;
using idc.pefindo.pbk.Services.Interfaces.Logging;

namespace idc.pefindo.pbk.Services.Logging;

public class ErrorLogger : IErrorLogger
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<ErrorLogger> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ErrorLogger(
        IDbConnectionFactory connectionFactory,
        ICorrelationService correlationService,
        ILogger<ErrorLogger> logger)
    {
        _connectionFactory = connectionFactory;
        _correlationService = correlationService;
        _logger = logger;
    }

    public async Task LogErrorAsync(string source, string message, Exception? exception = null, string? correlationId = null, object? additionalData = null)
    {
        await LogAsync("Error", source, message, exception, correlationId, additionalData);
    }

    public async Task LogWarningAsync(string source, string message, string? correlationId = null, object? additionalData = null)
    {
        await LogAsync("Warning", source, message, null, correlationId, additionalData);
    }

    public async Task LogCriticalAsync(string source, string message, Exception? exception = null, string? correlationId = null, object? additionalData = null)
    {
        await LogAsync("Critical", source, message, exception, correlationId, additionalData);
    }

    private async Task LogAsync(string logLevel, string source, string message, Exception? exception, string? correlationId, object? additionalData)
    {
        try
        {
            var entry = new ErrorLogEntry
            {
                CorrelationId = correlationId ?? _correlationService.GetCorrelationId(),
                RequestId = _correlationService.GetRequestId(),
                LogLevel = logLevel,
                Source = source,
                Message = message,
                Exception = exception?.ToString(),
                StackTrace = exception?.StackTrace,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData, _jsonOptions) : null,
                CreatedAt = DateTime.UtcNow
            };

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO pefindo.bk_error_logs 
                (correlation_id, request_id, log_level, source, message, exception, stack_trace, 
                 user_id, additional_data, created_at)
                VALUES (@correlation_id, @request_id, @log_level, @source, @message, @exception, 
                        @stack_trace, @user_id, @additional_data, @created_at)
                RETURNING id";

            AddParameter(command, "@correlation_id", entry.CorrelationId);
            AddParameter(command, "@request_id", entry.RequestId);
            AddParameter(command, "@log_level", entry.LogLevel);
            AddParameter(command, "@source", entry.Source);
            AddParameter(command, "@message", entry.Message);
            AddParameter(command, "@exception", entry.Exception);
            AddParameter(command, "@stack_trace", entry.StackTrace);
            AddParameter(command, "@user_id", entry.UserId);
            AddParameter(command, "@additional_data", entry.AdditionalData);
            AddParameter(command, "@created_at", entry.CreatedAt);

            await command.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            // Log to standard logger if database logging fails
            _logger.LogError(ex, "Failed to log {LogLevel} to database. Original message: {Message}", logLevel, message);
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}