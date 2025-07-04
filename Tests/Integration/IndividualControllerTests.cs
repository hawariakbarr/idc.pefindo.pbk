using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Tests;

namespace idc.pefindo.pbk.Tests.Integration;

public class IndividualControllerTests : IntegrationTestBase
{
    public IndividualControllerTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/idcpefindo/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content);
    }

    [Fact]
    public async Task PostIndividual_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();

        // Act
        var response = await PostJsonAsync<IndividualResponse>("/idcpefindo/individual", request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal("SUCCESS", response.Data.Status);
        Assert.Equal(request.CfLosAppNo, response.Data.AppNo);
    }

    [Fact]
    public async Task PostIndividual_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new
        {
            type_data = "",  // Invalid: empty
            name = "",       // Invalid: empty
            // Missing required fields
        };

        var json = JsonSerializer.Serialize(invalidRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividual_WithMalformedJson_ReturnsBadRequest()
    {
        // Arrange
        var content = new StringContent("{invalid json", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
