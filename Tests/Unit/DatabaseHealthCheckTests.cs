using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace idc.pefindo.pbk.Tests.Integration;

public class DatabaseHealthCheckTests : IntegrationTestBase
{
	public DatabaseHealthCheckTests(WebApplicationFactory<Program> factory) : base(factory)
	{
	}

	[Fact]
	public async Task Health_Endpoint_Returns_Success()
	{
		// Act
		var response = await _client.GetAsync("/health");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var content = await response.Content.ReadAsStringAsync();
		Assert.Contains("Healthy", content);
	}

	[Fact]
	public async Task Health_Endpoint_Returns_Valid_Json()
	{
		// Act
		var response = await _client.GetAsync("/health");

		// Assert
		response.EnsureSuccessStatusCode();

		var content = await response.Content.ReadAsStringAsync();

		// Should be valid JSON
		var healthResult = JsonSerializer.Deserialize<JsonElement>(content);
		Assert.True(healthResult.TryGetProperty("status", out _));
	}
}
