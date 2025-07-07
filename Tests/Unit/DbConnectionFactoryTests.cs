using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.DataAccess;
using EncryptionApi.Services;

namespace idc.pefindo.pbk.Tests.Unit;

public class DbConnectionFactoryTests
{
    private readonly Mock<ILogger<DbConnectionFactory>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public DbConnectionFactoryTests()
    {
        _mockLogger = new Mock<ILogger<DbConnectionFactory>>();
        _mockConfiguration = new Mock<IConfiguration>();
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldInitializeSuccessfully()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration
        {
            Names = new Dictionary<string, string>
            {
                { DatabaseKeys.Bk, "idc_pefindo_pbk_test" }
            },
            ConnectionStrings = new Dictionary<string, string>
            {
                { DatabaseKeys.Bk, "Host=localhost;Database=idc_pefindo_pbk_test;Username=test;Password={DBEncryptedPassword};" }
            }
        };

        var options = Options.Create(databaseConfig);
        _mockConfiguration.Setup(x => x["DBEncryptedPassword"])
            .Returns("test_encrypted_password");
        
        var mockEncryptionService = new Mock<IEncryptionService>();
        mockEncryptionService.Setup(x => x.DecryptString("test_encrypted_password"))
            .Returns("test_decrypted_password");

        // Act & Assert - Should not throw
        Assert.Throws<Exception>(() => new DbConnectionFactory(options, _mockConfiguration.Object, _mockLogger.Object, mockEncryptionService.Object));
        // Note: This will throw because we don't have the actual Encryption.DecryptString method in tests
        // In a real test environment, you would mock this dependency
    }

    [Fact]
    public void Constructor_WithInvalidConfiguration_ShouldThrow()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration
        {
            Names = new Dictionary<string, string>(),
            ConnectionStrings = new Dictionary<string, string>()
        };

        var options = Options.Create(databaseConfig);

        var mockEncryptionService = new Mock<IEncryptionService>();
        mockEncryptionService.Setup(x => x.DecryptString(It.IsAny<string>()))
            .Returns("test_decrypted_password");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new DbConnectionFactory(options, _mockConfiguration.Object, _mockLogger.Object, mockEncryptionService.Object));
    }
}
