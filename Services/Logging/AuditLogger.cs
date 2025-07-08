using System.Data.Common;
using System.Text.Json;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models.Logging;
using idc.pefindo.pbk.Services.Interfaces.Logging;

namespace idc.pefindo.pbk.Services.Logging;

public class AuditLogger : IAuditLogger
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AuditLogger> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AuditLogger(IDbConnectionFactory connectionFactory, ILogger<AuditLogger> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task LogActionAsync(string correlationId, string userId, string action, string? entityType = null,
        string? entityId = null, object? oldValue = null, object? newValue = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                CorrelationId = correlationId,
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue, _jsonOptions) : null,
                NewValue = newValue != null ? JsonSerializer.Serialize(newValue, _jsonOptions) : null,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO pefindo.bk_audit_logs 
                (correlation_id, user_id, action, entity_type, entity_id, old_value, new_value, 
                 timestamp, ip_address, user_agent, created_at)
                VALUES (@correlation_id, @user_id, @action, @entity_type, @entity_id, @old_value, 
                        @new_value, @timestamp, @ip_address, @user_agent, @created_at)
                RETURNING id";

            AddParameter(command, "@correlation_id", entry.CorrelationId);
            AddParameter(command, "@user_id", entry.UserId);
            AddParameter(command, "@action", entry.Action);
            AddParameter(command, "@entity_type", entry.EntityType);
            AddParameter(command, "@entity_id", entry.EntityId);
            AddParameter(command, "@old_value", entry.OldValue);
            AddParameter(command, "@new_value", entry.NewValue);
            AddParameter(command, "@timestamp", entry.Timestamp);
            AddParameter(command, "@ip_address", entry.IpAddress);
            AddParameter(command, "@user_agent", entry.UserAgent);
            AddParameter(command, "@created_at", entry.CreatedAt);

            await command.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit action {Action} for user {UserId}", action, userId);
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