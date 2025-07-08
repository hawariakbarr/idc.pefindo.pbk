using Xunit;
using idc.pefindo.pbk.Configuration;

namespace idc.pefindo.pbk.Tests.Unit;

public class DatabaseConfigurationTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            Names = new Dictionary<string, string>
            {
                { "testdb", "test.database" }
            },
            ConnectionStrings = new Dictionary<string, string>
            {
                { "testdb", "Host=localhost;Database=test;" }
            }
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithEmptyNames_ShouldThrow()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            Names = new Dictionary<string, string>(),
            ConnectionStrings = new Dictionary<string, string>
            {
                { "testdb", "Host=localhost;Database=test;" }
            }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void Validate_WithMissingConnectionString_ShouldThrow()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            Names = new Dictionary<string, string>
            {
                { "testdb", "test.database" }
            },
            ConnectionStrings = new Dictionary<string, string>()
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void GetDatabaseName_WithValidKey_ShouldReturnName()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            Names = new Dictionary<string, string>
            {
                { "testdb", "test.database" }
            }
        };

        // Act
        var result = config.GetDatabaseName("testdb");

        // Assert
        Assert.Equal("test.database", result);
    }

    [Fact]
    public void GetDatabaseName_WithInvalidKey_ShouldThrow()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            Names = new Dictionary<string, string>
            {
                { "testdb", "test.database" }
            }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => config.GetDatabaseName("invalid"));
    }
}
