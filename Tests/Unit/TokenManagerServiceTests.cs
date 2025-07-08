using Xunit;
using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using idc.pefindo.pbk.Services;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;

namespace idc.pefindo.pbk.Tests.Unit;

public class TokenManagerServiceTests : IDisposable
{
    private readonly Mock<IPefindoApiService> _mockPefindoApiService;
    private readonly Mock<ILogger<TokenManagerService>> _mockLogger;
    private readonly Mock<IErrorLogger> _mockErrorLogger;
    private readonly Mock<ICorrelationService> _mockCorrelationService;
    private readonly MemoryCache _memoryCache;
    private readonly TokenManagerService _service;
    private readonly Mock<IConfiguration> _mockConfigurationService;

    public TokenManagerServiceTests()
    {
        _mockPefindoApiService = new Mock<IPefindoApiService>();
        _mockLogger = new Mock<ILogger<TokenManagerService>>();
        _mockErrorLogger = new Mock<IErrorLogger>();
        _mockCorrelationService = new Mock<ICorrelationService>();

        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _mockCorrelationService.Setup(x => x.GetCorrelationId()).Returns("test-correlation-id");

        _mockConfigurationService = new Mock<IConfiguration>();

        _service = new TokenManagerService(
            _mockPefindoApiService.Object,
            _memoryCache,
            _mockLogger.Object,
            _mockErrorLogger.Object,
            _mockCorrelationService.Object,
            _mockConfigurationService.Object
        );
    }

    [Fact]
    public async Task GetValidTokenAsync_WhenNoTokenCached_ShouldRequestNewToken()
    {
        // Arrange
        var tokenResponse = CreateMockTokenResponse("test-token-123", "2024261509242633");
        _mockPefindoApiService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync(tokenResponse);

        // Act
        var result = await _service.GetValidTokenAsync();

        // Assert
        Assert.Equal("test-token-123", result);
        _mockPefindoApiService.Verify(x => x.GetTokenAsync(), Times.Once);

        // Verify token is cached
        var cacheInfo = _service.GetTokenCacheInfo();
        Assert.NotNull(cacheInfo);
        Assert.Equal("test-token-123", cacheInfo.Token);
    }

    [Fact]
    public async Task GetValidTokenAsync_WhenValidTokenCached_ShouldReturnCachedToken()
    {
        // Arrange
        var tokenResponse = CreateMockTokenResponse("cached-token-456", "2024261509242633");
        _mockPefindoApiService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync(tokenResponse);

        // First call to cache the token
        await _service.GetValidTokenAsync();

        // Act - Second call should return cached token
        var result = await _service.GetValidTokenAsync();

        // Assert
        Assert.Equal("cached-token-456", result);
        _mockPefindoApiService.Verify(x => x.GetTokenAsync(), Times.Once); // Should only be called once
    }

    [Fact]
    public async Task InvalidateTokenAsync_ShouldClearCachedToken()
    {
        // Arrange
        var tokenResponse = CreateMockTokenResponse("token-to-invalidate", "2024261509242633");
        _mockPefindoApiService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync(tokenResponse);

        // Cache a token first
        await _service.GetValidTokenAsync();
        Assert.NotNull(_service.GetTokenCacheInfo());

        // Act
        await _service.InvalidateTokenAsync();

        // Assert
        var cacheInfo = _service.GetTokenCacheInfo();
        Assert.Null(cacheInfo);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldAlwaysRequestNewToken()
    {
        // Arrange
        var firstTokenResponse = CreateMockTokenResponse("first-token", "2024261509242633");
        var secondTokenResponse = CreateMockTokenResponse("second-token", "2024261509242633");

        _mockPefindoApiService.SetupSequence(x => x.GetTokenAsync())
            .ReturnsAsync(firstTokenResponse)
            .ReturnsAsync(secondTokenResponse);

        // Act
        var firstToken = await _service.RefreshTokenAsync();
        var secondToken = await _service.RefreshTokenAsync();

        // Assert
        Assert.Equal("first-token", firstToken);
        Assert.Equal("second-token", secondToken);
        _mockPefindoApiService.Verify(x => x.GetTokenAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task IsTokenValidAsync_WhenNoTokenCached_ShouldReturnFalse()
    {
        // Act
        var result = await _service.IsTokenValidAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WhenValidTokenCached_ShouldReturnTrue()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddHours(1);
        var validDate = FormatValidDate(futureDate);
        var tokenResponse = CreateMockTokenResponse("valid-token", validDate);

        _mockPefindoApiService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync(tokenResponse);

        // Cache a token
        await _service.GetValidTokenAsync();

        // Act
        var result = await _service.IsTokenValidAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetTokenCacheInfo_WhenNoTokenCached_ShouldReturnNull()
    {
        // Act
        var result = _service.GetTokenCacheInfo();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTokenCacheInfo_WhenTokenCached_ShouldReturnCacheInfo()
    {
        // Arrange
        var tokenResponse = CreateMockTokenResponse("info-token", "2024261509242633");
        _mockPefindoApiService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync(tokenResponse);

        // Cache a token
        await _service.GetValidTokenAsync();

        // Act
        var result = _service.GetTokenCacheInfo();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("info-token", result.Token);
        Assert.Equal("2024261509242633", result.ValidDateOriginal);
        Assert.True(result.CachedAt <= DateTime.UtcNow);
    }

    private static string CreateMockTokenResponse(string token, string validDate)
    {
        return $$"""
        {
            "code": "01",
            "status": "success",
            "message": "Token aktif",
            "data": {
                "valid_date": "{{validDate}}",
                "token": "{{token}}"
            }
        }
        """;
    }

    private static string FormatValidDate(DateTime dateTime)
    {
        // Format: yyyyDDDHHmmssff
        var dayOfYear = dateTime.DayOfYear.ToString("D3");
        return $"{dateTime:yyyy}{dayOfYear}{dateTime:HHmmss}00";
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}