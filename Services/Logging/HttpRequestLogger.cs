using System.Data;
using System.Data.Common;
using System.Text.Json;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models.Logging;
using idc.pefindo.pbk.Services.Interfaces.Logging;

namespace idc.pefindo.pbk.Services.Logging;

public class HttpRequestLogger : IHttpRequestLogger
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<HttpRequestLogger> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public HttpRequestLogger(IDbConnectionFactory connectionFactory, ILogger<HttpRequestLogger> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task LogRequestAsync(HttpRequestLogEntry entry)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO pefindo.bk_http_request_logs 
                (correlation_id, request_id, service_name, method, url, request_headers, request_body, 
                 response_status_code, response_headers, response_body, duration_ms, request_time, 
                 response_time, is_successful, error_message, created_at)
                VALUES (@correlation_id, @request_id, @service_name, @method, @url, @request_headers, 
                        @request_body, @response_status_code, @response_headers, @response_body, 
                        @duration_ms, @request_time, @response_time, @is_successful, @error_message, @created_at)
                RETURNING id";

            AddParameter(command, "@correlation_id", entry.CorrelationId);
            AddParameter(command, "@request_id", entry.RequestId);
            AddParameter(command, "@service_name", entry.ServiceName);
            AddParameter(command, "@method", entry.Method);
            AddParameter(command, "@url", entry.Url);
            AddParameter(command, "@request_headers", entry.RequestHeaders);
            AddParameter(command, "@request_body", entry.RequestBody);
            AddParameter(command, "@response_status_code", entry.ResponseStatusCode);
            AddParameter(command, "@response_headers", entry.ResponseHeaders);
            AddParameter(command, "@response_body", entry.ResponseBody);
            AddParameter(command, "@duration_ms", entry.DurationMs);
            AddParameter(command, "@request_time", entry.RequestTime);
            AddParameter(command, "@response_time", entry.ResponseTime);
            AddParameter(command, "@is_successful", entry.IsSuccessful);
            AddParameter(command, "@error_message", entry.ErrorMessage);
            AddParameter(command, "@created_at", DateTime.UtcNow);

            await command.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log HTTP request for correlation {CorrelationId}", entry.CorrelationId);
        }
    }

    public async Task<List<HttpRequestLogEntry>> GetRequestLogsByCorrelationIdAsync(string correlationId)
    {
        var logs = new List<HttpRequestLogEntry>();

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT * FROM pefindo.bk_http_request_logs 
                WHERE correlation_id = @correlation_id
                ORDER BY request_time";

            AddParameter(command, "@correlation_id", correlationId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new HttpRequestLogEntry
                {
                    Id = reader.GetInt64("id"),
                    CorrelationId = reader.GetString("correlation_id"),
                    RequestId = reader.GetString("request_id"),
                    ServiceName = reader.GetString("service_name"),
                    Method = reader.GetString("method"),
                    Url = reader.GetString("url"),
                    RequestHeaders = reader.IsDBNull("request_headers") ? null : reader.GetString("request_headers"),
                    RequestBody = reader.IsDBNull("request_body") ? null : reader.GetString("request_body"),
                    ResponseStatusCode = reader.IsDBNull("response_status_code") ? null : reader.GetInt32("response_status_code"),
                    ResponseHeaders = reader.IsDBNull("response_headers") ? null : reader.GetString("response_headers"),
                    ResponseBody = reader.IsDBNull("response_body") ? null : reader.GetString("response_body"),
                    DurationMs = reader.IsDBNull("duration_ms") ? null : reader.GetInt32("duration_ms"),
                    RequestTime = reader.GetDateTime("request_time"),
                    ResponseTime = reader.IsDBNull("response_time") ? null : reader.GetDateTime("response_time"),
                    IsSuccessful = reader.IsDBNull("is_successful") ? null : reader.GetBoolean("is_successful"),
                    ErrorMessage = reader.IsDBNull("error_message") ? null : reader.GetString("error_message"),
                    CreatedAt = reader.GetDateTime("created_at")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve HTTP request logs for correlation {CorrelationId}", correlationId);
        }

        return logs;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}