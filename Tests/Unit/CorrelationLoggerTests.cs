using FluentAssertions;
using idc.pefindo.pbk.Services.Logging;
using idc.pefindo.pbk.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace idc.pefindo.pbk.Tests.Unit;

public class CorrelationLoggerTests
{
    private readonly Mock<ILogger<CorrelationLogger>> _mockLogger;

    public CorrelationLoggerTests()
    {
        _mockLogger = new Mock<ILogger<CorrelationLogger>>();
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WithValidParameters()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();

        // Act
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public async Task LogProcessStartAsync_ShouldAttemptDatabaseConnection_WhenCalled()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);
        
        var correlationId = "test-correlation-id";
        var requestId = "test-request-id";
        var processName = "TestProcess";
        var userId = "test-user";
        var sessionId = "test-session";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await logger.LogProcessStartAsync(correlationId, requestId, processName, userId, sessionId));

        // Since we're using MockDbConnectionFactory which throws InvalidOperationException,
        // we expect that exception, confirming that the method attempted to create a database connection
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("Mock database connection");
    }

    [Fact]
    public async Task LogProcessCompleteAsync_ShouldAttemptDatabaseConnection_WhenCalled()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);
        
        var correlationId = "test-correlation-id";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await logger.LogProcessCompleteAsync(correlationId));

        // Since we're using MockDbConnectionFactory which throws InvalidOperationException,
        // we expect that exception, confirming that the method attempted to create a database connection
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("Mock database connection");
    }

    [Fact]
    public async Task LogProcessFailAsync_ShouldAttemptDatabaseConnection_WhenCalled()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);
        
        var correlationId = "test-correlation-id";
        var errorMessage = "Test error message";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await logger.LogProcessFailAsync(correlationId, "Failed", errorMessage));

        // Since we're using MockDbConnectionFactory which throws InvalidOperationException,
        // we expect that exception, confirming that the method attempted to create a database connection
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("Mock database connection");
    }

    [Fact]
    public async Task UpdateProcessStatusAsync_ShouldAttemptDatabaseConnection_WhenCalled()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);
        
        var correlationId = "test-correlation-id";
        var status = "Updated";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await logger.UpdateProcessStatusAsync(correlationId, status));

        // Since we're using MockDbConnectionFactory which throws InvalidOperationException,
        // we expect that exception, confirming that the method attempted to create a database connection
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("Mock database connection");
    }

    [Fact]
    public async Task GetLogEntryAsync_ShouldAttemptDatabaseConnection_WhenCalled()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);
        
        var correlationId = "test-correlation-id";

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await logger.GetLogEntryAsync(correlationId));

        // Since we're using MockDbConnectionFactory which throws InvalidOperationException,
        // we expect that exception, confirming that the method attempted to create a database connection
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("Mock database connection");
    }

    [Fact]
    public async Task LogProcessStartAsync_WithNullUserId_ShouldAttemptDatabaseConnection()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await logger.LogProcessStartAsync("correlation-id", "request-id", "process-name", null, null));

        // Should still attempt database connection even with null optional parameters
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("Mock database connection");
    }

    [Fact]
    public async Task LogProcessFailAsync_WithDefaultStatus_ShouldAttemptDatabaseConnection()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await logger.LogProcessFailAsync("correlation-id"));

        // Should use default status parameter
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("Mock database connection");
    }

    [Fact]
    public async Task LogProcessCompleteAsync_WithDefaultStatus_ShouldAttemptDatabaseConnection()
    {
        // Arrange
        var mockDbConnectionFactory = new MockDbConnectionFactory();
        var logger = new CorrelationLogger(mockDbConnectionFactory, _mockLogger.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await logger.LogProcessCompleteAsync("correlation-id"));

        // Should use default status parameter
        exception.Should().BeOfType<InvalidOperationException>();
        exception!.Message.Should().Contain("Mock database connection");
    }
}