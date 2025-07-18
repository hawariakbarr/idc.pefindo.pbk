using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Services;

namespace idc.pefindo.pbk.Tests.Unit;

public class CycleDayValidationServiceTests
{
    private readonly Mock<IGlobalConfigRepository> _mockGlobalConfigRepository;
    private readonly Mock<ILogger<CycleDayValidationService>> _mockLogger;
    private readonly CycleDayValidationService _service;

    public CycleDayValidationServiceTests()
    {
        _mockGlobalConfigRepository = new Mock<IGlobalConfigRepository>();
        _mockLogger = new Mock<ILogger<CycleDayValidationService>>();
        _service = new CycleDayValidationService(_mockGlobalConfigRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetCurrentCycleDayConfigAsync_ValidConfig_ReturnsConfigValue()
    {
        // Arrange
        _mockGlobalConfigRepository.Setup(x => x.GetConfigValueAsync("GC31"))
            .ReturnsAsync("7");

        // Act
        var result = await _service.GetCurrentCycleDayConfigAsync();

        // Assert
        Assert.Equal("7", result);
        _mockGlobalConfigRepository.Verify(x => x.GetConfigValueAsync("GC31"), Times.Once);
    }

    [Fact]
    public async Task GetCurrentCycleDayConfigAsync_NullConfig_ReturnsDefaultValue()
    {
        // Arrange
        _mockGlobalConfigRepository.Setup(x => x.GetConfigValueAsync("GC31"))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetCurrentCycleDayConfigAsync();

        // Assert
        Assert.Equal("7", result); // Default value
    }

    [Theory]
    [InlineData("7", 0, false)]   // Outside tolerance (current day != 7)
    [InlineData("7", 7, true)]   // Within tolerance
    [InlineData("7", 14, true)] // Within tolerance
    [InlineData("invalid", 0, false)] // Invalid config
    public async Task ValidateCycleDayAsync_VariousScenarios_ReturnsExpectedResult(
        string configValue, int tolerance, bool expectedResult)
    {
        // Arrange
        _mockGlobalConfigRepository.Setup(x => x.GetConfigValueAsync("GC31"))
            .ReturnsAsync(configValue);

        // Act
        var result = await _service.ValidateCycleDayAsync(tolerance);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ValidateCycleDayAsync_ExceptionThrown_ReturnsFalse()
    {
        // Arrange
        _mockGlobalConfigRepository.Setup(x => x.GetConfigValueAsync("GC31"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.ValidateCycleDayAsync(0);

        // Assert
        Assert.False(result);
    }
}