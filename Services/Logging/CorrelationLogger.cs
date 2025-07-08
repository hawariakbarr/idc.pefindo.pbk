using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models.Logging;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using Npgsql;
using System.Data;

namespace idc.pefindo.pbk.Services.Logging
{
    public class CorrelationLogger : ICorrelationLogger
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<CorrelationLogger> _logger;

        public CorrelationLogger(IDbConnectionFactory dbConnectionFactory, ILogger<CorrelationLogger> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        public async Task LogProcessStartAsync(string correlationId, string requestId, string processName, string? userId = null, string? sessionId = null)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
                
                var sql = @"
                    INSERT INTO pefindo.bk_log_entries 
                    (correlation_id, request_id, user_id, session_id, process_name, start_time, status, created_at)
                    VALUES (@correlationId, @requestId, @userId, @sessionId, @processName, @startTime, @status, @createdAt)";

                using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
                command.Parameters.AddWithValue("@correlationId", correlationId);
                command.Parameters.AddWithValue("@requestId", requestId);
                command.Parameters.AddWithValue("@userId", (object?)userId ?? DBNull.Value);
                command.Parameters.AddWithValue("@sessionId", (object?)sessionId ?? DBNull.Value);
                command.Parameters.AddWithValue("@processName", processName);
                command.Parameters.AddWithValue("@startTime", DateTime.UtcNow);
                command.Parameters.AddWithValue("@status", "InProgress");
                command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Process started logged to correlation table: {CorrelationId} - {ProcessName}", 
                    correlationId, processName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log process start for correlation ID {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task LogProcessCompleteAsync(string correlationId, string status = "Success")
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);

                var sql = @"
                    UPDATE pefindo.bk_log_entries 
                    SET status = @status, end_time = @endTime
                    WHERE correlation_id = @correlationId";

                using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
                command.Parameters.AddWithValue("@correlationId", correlationId);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@endTime", DateTime.UtcNow);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No log entry found to update for correlation ID {CorrelationId}", correlationId);
                }
                else
                {
                    _logger.LogInformation("Process completion logged to correlation table: {CorrelationId} - {Status}", 
                        correlationId, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log process completion for correlation ID {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task LogProcessFailAsync(string correlationId, string status = "Failed", string? errorMessage = null)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);

                var sql = @"
                    UPDATE pefindo.bk_log_entries 
                    SET status = @status, end_time = @endTime, error_message = @errorMessage
                    WHERE correlation_id = @correlationId";

                using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
                command.Parameters.AddWithValue("@correlationId", correlationId);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@endTime", DateTime.UtcNow);
                command.Parameters.AddWithValue("@errorMessage", (object?)errorMessage ?? DBNull.Value);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No log entry found to update for correlation ID {CorrelationId}", correlationId);
                }
                else
                {
                    _logger.LogInformation("Process failure logged to correlation table: {CorrelationId} - {Status}", 
                        correlationId, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log process failure for correlation ID {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task UpdateProcessStatusAsync(string correlationId, string status)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);

                var sql = @"
                    UPDATE pefindo.bk_log_entries 
                    SET status = @status
                    WHERE correlation_id = @correlationId";

                using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
                command.Parameters.AddWithValue("@correlationId", correlationId);
                command.Parameters.AddWithValue("@status", status);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    _logger.LogWarning("No log entry found to update for correlation ID {CorrelationId}", correlationId);
                }
                else
                {
                    _logger.LogInformation("Process status updated in correlation table: {CorrelationId} - {Status}", 
                        correlationId, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update process status for correlation ID {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task<LogEntry?> GetLogEntryAsync(string correlationId)
        {
            try
            {
                using var connection = await _dbConnectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
                
                var sql = @"
                    SELECT id, correlation_id, request_id, user_id, session_id, process_name, 
                           start_time, end_time, status, error_message, created_at
                    FROM pefindo.bk_log_entries 
                    WHERE correlation_id = @correlationId";

                using var command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
                command.Parameters.AddWithValue("@correlationId", correlationId);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return new LogEntry
                    {
                        Id = reader.GetInt64("id"),
                        CorrelationId = reader.GetString("correlation_id"),
                        RequestId = reader.GetString("request_id"),
                        UserId = reader.IsDBNull("user_id") ? null : reader.GetString("user_id"),
                        SessionId = reader.IsDBNull("session_id") ? null : reader.GetString("session_id"),
                        ProcessName = reader.GetString("process_name"),
                        StartTime = reader.GetDateTime("start_time"),
                        EndTime = reader.IsDBNull("end_time") ? null : reader.GetDateTime("end_time"),
                        Status = reader.IsDBNull("status") ? null : reader.GetString("status"),
                        ErrorMessage = reader.IsDBNull("error_message") ? null : reader.GetString("error_message"),
                        CreatedAt = reader.GetDateTime("created_at")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get log entry for correlation ID {CorrelationId}", correlationId);
                throw;
            }
        }
    }
}