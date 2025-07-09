using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace idc.pefindo.pbk.Tests.Integration;

/// <summary>
/// Direct tests for logging services to ensure they work independently
/// </summary>
public class LoggingServiceTests : IntegrationTestBase
{
    public LoggingServiceTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task MockCorrelationLogger_ShouldStoreAndRetrieveLogEntries()
    {
        // Arrange
        var logger = _factory.Services.GetRequiredService<ICorrelationLogger>() as MockCorrelationLogger;
        Assert.NotNull(logger);

        logger.ClearLogs(); // Start with clean state

        var correlationId = Guid.NewGuid().ToString();
        var requestId = Guid.NewGuid().ToString();

        // Act
        await logger.LogProcessStartAsync(correlationId, requestId, "TestProcess", "testuser", "testsession");

        // Assert
        var entries = logger.GetAllLogEntries();
        Assert.Single(entries);

        var entry = entries.First();
        Assert.Equal(correlationId, entry.CorrelationId);
        Assert.Equal(requestId, entry.RequestId);
        Assert.Equal("TestProcess", entry.ProcessName);
        Assert.Equal("testuser", entry.UserId);
        Assert.Equal("InProgress", entry.Status);
    }

    [Fact]
    public async Task MockProcessStepLogger_ShouldStoreAndRetrieveStepLogs()
    {
        // Arrange
        var logger = _factory.Services.GetRequiredService<IProcessStepLogger>() as MockProcessStepLogger;
        Assert.NotNull(logger);

        logger.ClearLogs(); // Start with clean state

        var correlationId = Guid.NewGuid().ToString();
        var requestId = Guid.NewGuid().ToString();

        // Act
        await logger.LogStepStartAsync(correlationId, requestId, "TestStep", 1, new { Input = "test" });

        // Assert
        var logs = logger.GetStepLogs();
        Assert.Single(logs);

        var log = logs.First();
        Assert.Equal(correlationId, log.CorrelationId);
        Assert.Equal(requestId, log.RequestId);
        Assert.Equal("TestStep", log.StepName);
        Assert.Equal(1, log.StepOrder);
        Assert.Equal("Started", log.Status);
    }

    [Fact]
    public async Task MockAuditLogger_ShouldStoreAndRetrieveAuditLogs()
    {
        // Arrange
        var logger = _factory.Services.GetRequiredService<IAuditLogger>() as MockAuditLogger;
        Assert.NotNull(logger);

        logger.ClearLogs(); // Start with clean state

        var correlationId = Guid.NewGuid().ToString();

        // Act
        await logger.LogActionAsync(correlationId, "testuser", "TestAction", "TestEntity", "123");

        // Assert
        var logs = logger.GetAuditLogs();
        Assert.Single(logs);

        var log = logs.First();
        Assert.Equal(correlationId, log.CorrelationId);
        Assert.Equal("testuser", log.UserId);
        Assert.Equal("TestAction", log.Action);
    }

    [Fact]
    public void LoggingServices_AreRegisteredAsSingleton()
    {
        // Test that singleton registration works correctly
        var logger1 = _factory.Services.GetRequiredService<ICorrelationLogger>();
        var logger2 = _factory.Services.GetRequiredService<ICorrelationLogger>();

        Assert.Same(logger1, logger2);

        var stepLogger1 = _factory.Services.GetRequiredService<IProcessStepLogger>();
        var stepLogger2 = _factory.Services.GetRequiredService<IProcessStepLogger>();

        Assert.Same(stepLogger1, stepLogger2);

        var auditLogger1 = _factory.Services.GetRequiredService<IAuditLogger>();
        var auditLogger2 = _factory.Services.GetRequiredService<IAuditLogger>();

        Assert.Same(auditLogger1, auditLogger2);
    }

    [Fact]
    public async Task AllMockLoggers_CanBeUsedConcurrently()
    {
        // Test that all logging services work together
        var correlationLogger = _factory.Services.GetRequiredService<ICorrelationLogger>() as MockCorrelationLogger;
        var stepLogger = _factory.Services.GetRequiredService<IProcessStepLogger>() as MockProcessStepLogger;
        var auditLogger = _factory.Services.GetRequiredService<IAuditLogger>() as MockAuditLogger;
        var errorLogger = _factory.Services.GetRequiredService<IErrorLogger>() as MockErrorLogger;

        Assert.NotNull(correlationLogger);
        Assert.NotNull(stepLogger);
        Assert.NotNull(auditLogger);
        Assert.NotNull(errorLogger);

        // Clear all logs
        correlationLogger.ClearLogs();
        stepLogger.ClearLogs();
        auditLogger.ClearLogs();
        errorLogger.ClearLogs();

        var correlationId = Guid.NewGuid().ToString();
        var requestId = Guid.NewGuid().ToString();

        // Act - Use all loggers
        await correlationLogger.LogProcessStartAsync(correlationId, requestId, "TestProcess");
        await stepLogger.LogStepStartAsync(correlationId, requestId, "TestStep", 1);
        await auditLogger.LogActionAsync(correlationId, "testuser", "TestAction");
        await errorLogger.LogErrorAsync("TestSource", "Test error message", correlationId: correlationId);

        // Assert
        Assert.Single(correlationLogger.GetAllLogEntries());
        Assert.Single(stepLogger.GetStepLogs());
        Assert.Single(auditLogger.GetAuditLogs());
        Assert.Single(errorLogger.GetErrorLogs());

        // Verify correlation IDs match
        Assert.Equal(correlationId, correlationLogger.GetAllLogEntries().First().CorrelationId);
        Assert.Equal(correlationId, stepLogger.GetStepLogs().First().CorrelationId);
        Assert.Equal(correlationId, auditLogger.GetAuditLogs().First().CorrelationId);
        Assert.Equal(correlationId, errorLogger.GetErrorLogs().First().CorrelationId);
    }
}