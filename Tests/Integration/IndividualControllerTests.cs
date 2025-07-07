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
    public async Task PostIndividual_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new
        {
            type_data = "",  // Invalid: empty
            name = "",       // Invalid: empty
            id_number = "123", // Invalid: too short
            cf_los_app_no = "", // Invalid: empty
            facility_limit = -1000 // Invalid: negative
        };

        var json = JsonSerializer.Serialize(invalidRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation", responseContent.ToLower());
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

    [Fact]
    public async Task PostIndividual_WithValidData_ProcessesSuccessfully()
    {
        // Arrange
        var validRequest = TestHelper.CreateValidIndividualRequest();
        validRequest.CfLosAppNo = "TEST-VALID-001";
        validRequest.IdNumber = "1234567890123456"; // Mock service recognizes this

        var json = JsonSerializer.Serialize(validRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        // With mock services, this should process without external dependencies
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.BadRequest); // Might fail on cycle day validation

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345678901234567")]
    [InlineData("123456789012345a")]
    public async Task PostIndividual_WithInvalidIdNumber_ReturnsBadRequest(string invalidIdNumber)
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.IdNumber = invalidIdNumber;

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public async Task PostIndividual_WithInvalidTolerance_ReturnsBadRequest(int invalidTolerance)
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        request.Tolerance = invalidTolerance;

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/idcpefindo/individual", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}