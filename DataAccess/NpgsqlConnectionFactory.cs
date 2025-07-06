using System.Data.Common;
using Npgsql;
using idc.pefindo.pbk.DataAccess;

namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// PostgreSQL connection factory implementation with proper async support
/// </summary>
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;

    public NpgsqlConnectionFactory(IConfiguration configuration, ILogger<NpgsqlConnectionFactory> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<DbConnection> CreateConnectionAsync()
    {
        try
        {
            _logger.LogDebug("Connection string: {ConnectionString}", _connectionString);

            var connection = new NpgsqlConnection(_connectionString);
            _logger.LogDebug("NpgsqlConnection created, attempting to open...");

            await connection.OpenAsync();
            _logger.LogDebug("Database connection opened successfully");

            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database connection. ConnectionString: {ConnectionString}", _connectionString);
            throw;
        }
    }
}
