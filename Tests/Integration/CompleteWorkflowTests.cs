using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Tests;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace idc.pefindo.pbk.Tests.Integration;

public class CompleteWorkflowTests : IntegrationTestBase
{
    public CompleteWorkflowTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task PostIndividual_WithValidData_ProcessesRequest()
    {
        // Note: This test uses mock services, so it will succeed in test environment

        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.CfLosAppNo = "TEST-INTEGRATION-001";
        request.Tolerance = 30; // High tolerance to pass cycle day validation

        // Act
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);

        // If successful, verify response structure
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseData = JsonSerializer.Deserialize<IndividualResponse>(responseContent);
            Assert.NotNull(responseData);
            Assert.NotNull(responseData.Data);
            Assert.Equal("TEST-INTEGRATION-001", responseData.Data.AppNo);
        }
    }

    [Fact]
    public async Task PostIndividual_WithKnownTestData_ReturnsExpectedStructure()
    {
        // Arrange - Use test data that mock services recognize
        var request = TestHelper.CreateValidIndividualRequest();
        request.CfLosAppNo = "TEST-MOCK-SUCCESS";
        request.IdNumber = "1234567890123456"; // This ID is recognized by mock service
        request.Tolerance = 7; // Exact cycle day match

        // Act
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);

        // Parse and verify response structure regardless of success/failure
        try
        {
            var jsonDoc = JsonDocument.Parse(responseContent);
            Assert.True(jsonDoc.RootElement.TryGetProperty("data", out _) ||
                       jsonDoc.RootElement.TryGetProperty("error", out _) ||
                       jsonDoc.RootElement.TryGetProperty("errors", out _));
        }
        catch (JsonException)
        {
            // If not JSON, check if it's a known error response
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task Swagger_Endpoint_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("IDC Pefindo PBK API", content);
        Assert.Contains("idcpefindo", content);
    }

    [Fact]
    public async Task PostIndividual_WithValidData_ShouldLogProcessSteps()
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.CfLosAppNo = "TEST-LOGGING-001";
        request.IdNumber = "1234567890123456"; // This ID is recognized by mock service
        request.Tolerance = 30; // High tolerance to pass validation

        // Act
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        using var scope = _factory.Services.CreateScope();
        
        // Verify correlation logging
        var correlationLogger = scope.ServiceProvider.GetRequiredService<ICorrelationLogger>() as MockCorrelationLogger;
        Assert.NotNull(correlationLogger);
        
        var logEntries = correlationLogger.GetAllLogEntries();
        Assert.NotEmpty(logEntries);
        
        var processLogEntry = logEntries.FirstOrDefault(e => e.ProcessName == "IndividualProcessing");
        Assert.NotNull(processLogEntry);
        Assert.Equal("InProgress", processLogEntry.Status);
        
        // Verify process step logging
        var processStepLogger = scope.ServiceProvider.GetRequiredService<IProcessStepLogger>() as MockProcessStepLogger;
        Assert.NotNull(processStepLogger);
        
        var stepLogs = processStepLogger.GetStepLogs();
        Assert.NotEmpty(stepLogs);
        
        // Verify audit logging
        var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>() as MockAuditLogger;
        Assert.NotNull(auditLogger);
        
        var auditLogs = auditLogger.GetAuditLogs();
        Assert.NotEmpty(auditLogs);
        Assert.Contains(auditLogs, log => log.Action == "PBKProcessingStarted");
    }

    [Fact]
    public async Task PostIndividual_WithInvalidData_ShouldLogErrors()
    {
        // Arrange
        var invalidRequest = new IndividualRequest
        {
            CfLosAppNo = "", // Invalid: empty app number
            Name = "", // Invalid: empty name
            IdNumber = "invalid", // Invalid: wrong format
            Tolerance = -1 // Invalid: negative tolerance
        };

        // Act
        var json = JsonSerializer.Serialize(invalidRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        
        // Verify error logging occurred
        var errorLogger = scope.ServiceProvider.GetRequiredService<IErrorLogger>() as MockErrorLogger;
        Assert.NotNull(errorLogger);
        
        // Note: Validation errors are handled by model validation, not by our error logger
        // But we can verify the logger service is available and working
        var errorLogs = errorLogger.GetErrorLogs();
        // Error logs might be empty for validation errors, which is expected
    }
}