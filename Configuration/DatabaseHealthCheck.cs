using idc.pefindo.pbk.DataAccess;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace idc.pefindo.pbk.Configuration;

/// <summary>
/// Health check for all configured databases
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IDbConnectionFactory connectionFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _connectionFactory.ValidateAllConnectionsAsync();
            var failedDatabases = results.Where(x => !x.Value).ToList();

            if (!failedDatabases.Any())
            {
                return HealthCheckResult.Healthy($"All {results.Count} databases are healthy",
                    new Dictionary<string, object> { ["databases"] = results });
            }

            var failedNames = string.Join(", ", failedDatabases.Select(x => x.Key));
            return HealthCheckResult.Degraded($"Failed databases: {failedNames}",
                data: new Dictionary<string, object> { ["databases"] = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}