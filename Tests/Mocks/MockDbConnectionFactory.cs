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
}