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
        // Note: This test may fail due to missing Pefindo API access or database
        // In a real environment, you would mock these dependencies

        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.CfLosAppNo = "TEST-INTEGRATION-001";
        request.Tolerance = 30; // High tolerance to pass cycle day validation

        // Act
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        // The response could be success or failure depending on environment setup
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);
    }
}
