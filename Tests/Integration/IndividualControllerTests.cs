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
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task PostIndividual_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new
        {
            type_data = "",  // Invalid: empty
            name = "",       // Invalid: empty
            id_number = "123", // Invalid: too short
            cf_los_app_no = "", // Invalid: empty
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
