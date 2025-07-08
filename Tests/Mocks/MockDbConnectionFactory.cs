using idc.pefindo.pbk.DataAccess;
using System.Data.Common;

namespace idc.pefindo.pbk.Tests.Mocks;

public class MockDbConnectionFactory : IDbConnectionFactory
{
    private readonly Dictionary<string, string> _availableDatabases = new()
    {
        { DatabaseKeys.Core, "idc.core_test" },
        { DatabaseKeys.En, "idc.en_test" },
        { DatabaseKeys.Bk, "idc.bk_test" }
    };

    public Task<DbConnection> CreateConnectionAsync()
    {
        throw new InvalidOperationException("Database not available in test environment. Use mock data instead.");
    }

    public Task<DbConnection> CreateConnectionAsync(string databaseKey)
    {
        if (!_availableDatabases.ContainsKey(databaseKey))
        {
            throw new ArgumentException($"Database key '{databaseKey}' not found in test configuration");
        }

        throw new InvalidOperationException($"Mock database connection for '{databaseKey}' not implemented. Use mock repositories instead.");
    }

    public IEnumerable<string> GetAvailableDatabaseKeys()
    {
        return _availableDatabases.Keys;
    }

    public Task<Dictionary<string, bool>> ValidateAllConnectionsAsync()
    {
        // Return all databases as healthy for testing
        var result = _availableDatabases.ToDictionary(
            kvp => kvp.Key,
            kvp => true
        );
        return Task.FromResult(result);
    }
}