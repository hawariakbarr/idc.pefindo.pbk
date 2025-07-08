using Xunit;
using FluentAssertions;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Tests.Mocks;

namespace idc.pefindo.pbk.Tests.Unit;

public class DbConnectionFactoryTests
{
    [Fact]
    public void MockDbConnectionFactory_GetAvailableDatabaseKeys_ReturnsExpectedKeys()
    {
        // Arrange
        var mockFactory = new MockDbConnectionFactory();
        
        // Act
        var availableKeys = mockFactory.GetAvailableDatabaseKeys();
        
        // Assert
        availableKeys.Should().NotBeEmpty();
        availableKeys.Should().Contain(DatabaseKeys.Core);
        availableKeys.Should().Contain(DatabaseKeys.En);
        availableKeys.Should().Contain(DatabaseKeys.Bk);
    }

    [Fact]
    public async Task MockDbConnectionFactory_CreateConnection_ThrowsExpectedException()
    {
        // Arrange
        var mockFactory = new MockDbConnectionFactory();
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await mockFactory.CreateConnectionAsync());
    }

    [Fact]
    public async Task MockDbConnectionFactory_CreateConnectionWithKey_ThrowsExpectedException()
    {
        // Arrange
        var mockFactory = new MockDbConnectionFactory();
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await mockFactory.CreateConnectionAsync(DatabaseKeys.Bk));
    }

    [Fact]
    public async Task MockDbConnectionFactory_CreateConnectionWithInvalidKey_ThrowsArgumentException()
    {
        // Arrange
        var mockFactory = new MockDbConnectionFactory();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await mockFactory.CreateConnectionAsync("invalid_key"));
    }

    [Fact]
    public async Task MockDbConnectionFactory_ValidateAllConnections_ReturnsHealthyStatus()
    {
        // Arrange
        var mockFactory = new MockDbConnectionFactory();
        
        // Act
        var result = await mockFactory.ValidateAllConnectionsAsync();
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey(DatabaseKeys.Core);
        result.Should().ContainKey(DatabaseKeys.En);
        result.Should().ContainKey(DatabaseKeys.Bk);
        
        result[DatabaseKeys.Core].Should().BeTrue();
        result[DatabaseKeys.En].Should().BeTrue();
        result[DatabaseKeys.Bk].Should().BeTrue();
    }

    [Theory]
    [InlineData(DatabaseKeys.Core)]
    [InlineData(DatabaseKeys.En)]
    [InlineData(DatabaseKeys.Bk)]
    public void MockDbConnectionFactory_SupportedDatabaseKeys_AreValid(string databaseKey)
    {
        // Arrange
        var mockFactory = new MockDbConnectionFactory();
        
        // Act
        var availableKeys = mockFactory.GetAvailableDatabaseKeys();
        
        // Assert
        availableKeys.Should().Contain(databaseKey);
    }
}