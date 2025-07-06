using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using idc.pefindo.pbk.Services;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.DataAccess;

namespace idc.pefindo.pbk.Tests.Unit;

public class TokenManagerServiceTests
{
    private readonly Mock<IPefindoApiService> _mockPefindoApiService;
    private readonly Mock<IDbConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IGlobalConfigRepository> _mockGlobalConfigRepository;
    private readonly Mock<ILogger<TokenManagerService>> _mockLogger;
    private readonly TokenManagerService _service;

    public TokenManagerServiceTests()
    {
        _mockPefindoApiService = new Mock<IPefindoApiService>();
        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockGlobalConfigRepository = new Mock<IGlobalConfigRepository>();
        _mockLogger = new Mock<ILogger<TokenManagerService>>();
        
        _service = new TokenManagerService(
            _mockPefindoApiService.Object,
            _mockConnectionFactory.Object,
            _mockGlobalConfigRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetValidTokenAsync_WhenNoTokenCached_ShouldRequestNewToken()
    {
        // Arrange
        var expectedToken = "test-token-12345";
        _mockPefindoApiService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync(expectedToken);
        _mockGlobalConfigRepository.Setup(x => x.GetConfigValueAsync("GC39"))
            .ReturnsAsync("60");

        // Act
        var result = await _service.GetValidTokenAsync();

        // Assert
        Assert.Equal(expectedToken, result);
        _mockPefindoApiService.Verify(x => x.GetTokenAsync(), Times.Once);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithNullToken_ShouldReturnFalse()
    {
        // Act
        var result = await _service.IsTokenValidAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task InvalidateTokenAsync_ShouldClearCachedToken()
    {
        // Act & Assert (should not throw)
        await _service.InvalidateTokenAsync();
        
        var isValid = await _service.IsTokenValidAsync();
        Assert.False(isValid);
    }
}
