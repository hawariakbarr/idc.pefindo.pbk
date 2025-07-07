
// DataAccess/DbConnectionFactory.cs (Updated)
using System.Data.Common;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Npgsql;
using idc.pefindo.pbk.Configuration;
using Helper;
using EncryptionApi.Services;

namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// Configuration-based PostgreSQL connection factory with encrypted password support
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseConfiguration _databaseConfig;
    private readonly string _decryptedPassword;
    private readonly ILogger<DbConnectionFactory> _logger;
    private readonly IEncryptionService _encryptionService;

    // Cache processed connection strings to avoid repeated string operations and decryption
    private readonly ConcurrentDictionary<string, string> _connectionStringCache = new();

    public DbConnectionFactory(
        IOptions<DatabaseConfiguration> databaseConfig,
        IConfiguration configuration,
        ILogger<DbConnectionFactory> logger,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _databaseConfig = databaseConfig.Value;
        _encryptionService = encryptionService;

        try
        {
            _logger.LogInformation("Initializing configuration-based DbConnectionFactory");

            // Validate configuration
            _databaseConfig.Validate();
            _logger.LogInformation("Database configuration validated successfully. Found {DatabaseCount} databases",
                _databaseConfig.Names.Count);

            // Get and decrypt the password once during initialization
            var encryptedPassword = GetEncryptedPasswordFromSources(configuration);
            if (string.IsNullOrEmpty(encryptedPassword))
            {
                throw new InvalidOperationException("DBEncryptedPassword not found in configuration sources.");
            }

            _logger.LogDebug("Found encrypted password, attempting decryption...");

            try
            {
                //_decryptedPassword = Encryption.DecryptString(encryptedPassword);
                _decryptedPassword = _encryptionService.DecryptString(encryptedPassword);
                _logger.LogInformation("Password decryption successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt database password");
                throw new InvalidOperationException("Failed to decrypt database password. Verify encryption key and password format.", ex);
            }

            // Clear encrypted password from memory
            encryptedPassword = string.Empty;

            _logger.LogInformation("Configuration-based connection factory initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize DbConnectionFactory");
            throw;
        }
    }

    /// <summary>
    /// Creates connection to default database (Idcbk)
    /// </summary>
    public async Task<DbConnection> CreateConnectionAsync()
    {
        return await CreateConnectionAsync(DatabaseKeys.Bk);
    }

    /// <summary>
    /// Creates connection using database key
    /// </summary>
    public async Task<DbConnection> CreateConnectionAsync(string databaseKey)
    {
        if (string.IsNullOrWhiteSpace(databaseKey))
            throw new ArgumentException("Database key cannot be null or empty", nameof(databaseKey));

        try
        {
            var databaseName = _databaseConfig.GetDatabaseName(databaseKey);
            _logger.LogDebug("Creating connection to database: {DatabaseName} (key: {DatabaseKey})",
                databaseName, databaseKey);

            // Get or create processed connection string for this database
            var connectionString = _connectionStringCache.GetOrAdd(databaseKey, key =>
            {
                var template = _databaseConfig.GetConnectionString(key);
                return template.Replace("{DBEncryptedPassword}", _decryptedPassword);
            });

            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            _logger.LogDebug("Successfully connected to database: {DatabaseName} (key: {DatabaseKey})",
                databaseName, databaseKey);
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create connection to database key: {DatabaseKey}", databaseKey);
            throw;
        }
    }

    /// <summary>
    /// Gets all available database keys
    /// </summary>
    public IEnumerable<string> GetAvailableDatabaseKeys()
    {
        return _databaseConfig.Names.Keys;
    }

    /// <summary>
    /// Validates all database connections for health checks
    /// </summary>
    public async Task<Dictionary<string, bool>> ValidateAllConnectionsAsync()
    {
        var results = new Dictionary<string, bool>();

        foreach (var databaseKey in _databaseConfig.Names.Keys)
        {
            try
            {
                using var connection = await CreateConnectionAsync(databaseKey);
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();
                results[databaseKey] = true;
                _logger.LogDebug("Database {DatabaseKey} validation successful", databaseKey);
            }
            catch (Exception ex)
            {
                results[databaseKey] = false;
                _logger.LogWarning(ex, "Database {DatabaseKey} validation failed", databaseKey);
            }
        }

        return results;
    }

    /// <summary>
    /// Gets encrypted password from multiple configuration sources
    /// </summary>
    private string? GetEncryptedPasswordFromSources(IConfiguration configuration)
    {
        // Priority 1: Environment variable
        var envPassword = Environment.GetEnvironmentVariable("DBEncryptedPassword");
        if (!string.IsNullOrEmpty(envPassword))
        {
            _logger.LogDebug("Using encrypted password from environment variable");
            return envPassword;
        }

        // Priority 2: Configuration file
        var configPassword = configuration["DBEncryptedPassword"];
        if (!string.IsNullOrEmpty(configPassword))
        {
            _logger.LogDebug("Using encrypted password from configuration");
            return configPassword;
        }

        return null;
    }
}