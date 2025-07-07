using System.Data;
using System.Data.Common;
using System.Text.Json;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models.Logging;
using idc.pefindo.pbk.Services.Interfaces.Logging;

namespace idc.pefindo.pbk.Services.Logging;

public class ProcessStepLogger : IProcessStepLogger
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ProcessStepLogger> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ProcessStepLogger(IDbConnectionFactory connectionFactory, ILogger<ProcessStepLogger> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task LogStepStartAsync(string correlationId, string requestId, string stepName, int stepOrder, object? inputData = null)
    {
        var entry = new ProcessStepLogEntry
        {
            CorrelationId = correlationId,
            RequestId = requestId,
            StepName = stepName,
            StepOrder = stepOrder,
            Status = "Started",
            InputData = inputData != null ? JsonSerializer.Serialize(inputData, _jsonOptions) : null,
            StartTime = DateTime.UtcNow
        };

        await LogStepAsync(entry);
    }

    public async Task LogStepCompleteAsync(string correlationId, string requestId, string stepName, object? outputData = null, int? durationMs = null)
    {
        await UpdateStepStatusAsync(correlationId, stepName, "Completed", outputData, null, durationMs);
    }

    public async Task LogStepFailAsync(string correlationId, string requestId, string stepName, Exception ex, object? inputData = null, int? durationMs = null)
    {
        var errorDetails = JsonSerializer.Serialize(new
        {
            message = ex.Message,
            stackTrace = ex.StackTrace,
            innerException = ex.InnerException?.Message
        }, _jsonOptions);

        await UpdateStepStatusAsync(correlationId, stepName, "Failed", inputData, errorDetails, durationMs);
    }

    public async Task<List<ProcessStepLogEntry>> GetProcessLogsByCorrelationIdAsync(string correlationId)
    {
        var logs = new List<ProcessStepLogEntry>();

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT * FROM pefindo.bk_process_step_logs
                WHERE correlation_id = @correlation_id
                ORDER BY step_order, start_time";

            AddParameter(command, "@correlation_id", correlationId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new ProcessStepLogEntry
                {
                    Id = reader.GetInt64("id"),
                    CorrelationId = reader.GetString("correlation_id"),
                    RequestId = reader.GetString("request_id"),
                    StepName = reader.GetString("step_name"),
                    StepOrder = reader.GetInt32("step_order"),
                    Status = reader.GetString("status"),
                    InputData = reader.IsDBNull("input_data") ? null : reader.GetString("input_data"),
                    OutputData = reader.IsDBNull("output_data") ? null : reader.GetString("output_data"),
                    ErrorDetails = reader.IsDBNull("error_details") ? null : reader.GetString("error_details"),
                    DurationMs = reader.IsDBNull("duration_ms") ? null : reader.GetInt32("duration_ms"),
                    StartTime = reader.GetDateTime("start_time"),
                    EndTime = reader.IsDBNull("end_time") ? null : reader.GetDateTime("end_time"),
                    CreatedAt = reader.GetDateTime("created_at")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve process step logs for correlation {CorrelationId}", correlationId);
        }

        return logs;
    }

    private async Task LogStepAsync(ProcessStepLogEntry entry)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO pefindo.bk_process_step_logs
                (correlation_id, request_id, step_name, step_order, status, input_data, output_data, 
                 error_details, duration_ms, start_time, end_time, created_at)
                VALUES (@correlation_id, @request_id, @step_name, @step_order, @status, @input_data, 
                        @output_data, @error_details, @duration_ms, @start_time, @end_time, @created_at)
                RETURNING id";

            AddParameter(command, "@correlation_id", entry.CorrelationId);
            AddParameter(command, "@request_id", entry.RequestId);
            AddParameter(command, "@step_name", entry.StepName);
            AddParameter(command, "@step_order", entry.StepOrder);
            AddParameter(command, "@status", entry.Status);
            AddParameter(command, "@input_data", entry.InputData);
            AddParameter(command, "@output_data", entry.OutputData);
            AddParameter(command, "@error_details", entry.ErrorDetails);
            AddParameter(command, "@duration_ms", entry.DurationMs);
            AddParameter(command, "@start_time", entry.StartTime);
            AddParameter(command, "@end_time", entry.EndTime);
            AddParameter(command, "@created_at", DateTime.UtcNow);

            await command.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log process step {StepName} for correlation {CorrelationId}",
                entry.StepName, entry.CorrelationId);
        }
    }

    private async Task UpdateStepStatusAsync(string correlationId, string stepName, string status, object? data, string? errorDetails, int? durationMs)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            var dataJson = data != null ? JsonSerializer.Serialize(data, _jsonOptions) : null;
            var endTime = DateTime.UtcNow;

            command.CommandText = @"
                UPDATE pefindo.bk_process_step_logs
                SET status = @status,
                    output_data = COALESCE(@output_data, output_data),
                    error_details = @error_details,
                    duration_ms = @duration_ms,
                    end_time = @end_time
                WHERE correlation_id = @correlation_id 
                  AND step_name = @step_name 
                  AND status = 'Started'";

            AddParameter(command, "@status", status);
            AddParameter(command, "@output_data", dataJson);
            AddParameter(command, "@error_details", errorDetails);
            AddParameter(command, "@duration_ms", durationMs);
            AddParameter(command, "@end_time", endTime);
            AddParameter(command, "@correlation_id", correlationId);
            AddParameter(command, "@step_name", stepName);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update process step {StepName} status for correlation {CorrelationId}",
                stepName, correlationId);
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