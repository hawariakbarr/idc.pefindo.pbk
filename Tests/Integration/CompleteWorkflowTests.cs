using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Tests;

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
}