using System.Data.Common;

namespace idc.pefindo.pbk.DataAccess;
/// <summary>
/// Factory for creating database connections with configuration-based multi-database support
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens a new database connection to the default database (PefindoPbk)
    /// </summary>
    Task<DbConnection> CreateConnectionAsync();

    /// <summary>
    /// Creates and opens a new database connection using database key
    /// </summary>
    /// <param name="databaseKey">Database key from DatabaseKeys (e.g., DatabaseKeys.Core)</param>
    Task<DbConnection> CreateConnectionAsync(string databaseKey);

    /// <summary>
    /// Gets all available database keys
    /// </summary>
    IEnumerable<string> GetAvailableDatabaseKeys();

    /// <summary>
    /// Validates all database connections
    /// </summary>
    Task<Dictionary<string, bool>> ValidateAllConnectionsAsync();
}