using System.Data;
using Npgsql;
using idc.pefindo.pbk.DataAccess;

namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// PostgreSQL connection factory implementation with connection pooling
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

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            _logger.LogDebug("Database connection opened successfully");
            return connection;
        }
        catch (NpgsqlException npgsqlEx)
        {
            _logger.LogError(npgsqlEx, "PostgreSQL error while creating database connection");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database connection");
            throw new InvalidOperationException("Could not create database connection", ex);
        }
    }
}
