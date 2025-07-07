// Create Tests/Mocks/MockDbConnectionFactory.cs
using idc.pefindo.pbk.DataAccess;
using System.Data.Common;

public class MockDbConnectionFactory : IDbConnectionFactory
{
    public Task<DbConnection> CreateConnectionAsync()
    {
        // Return a mock connection that doesn't require a real database
        throw new NotImplementedException("Database not available in test environment");
    }

    public Task<DbConnection> CreateConnectionAsync(string databaseKey)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetAvailableDatabaseKeys()
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, bool>> ValidateAllConnectionsAsync()
    {
        throw new NotImplementedException();
    }
}