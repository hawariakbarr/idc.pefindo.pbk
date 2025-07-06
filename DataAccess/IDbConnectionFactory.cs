using System.Data.Common;

namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// Factory for creating database connections with proper async support
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens a new database connection
    /// </summary>
    /// <returns>Open database connection</returns>
    Task<DbConnection> CreateConnectionAsync();
}
