using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using Xunit;

namespace idc.pefindo.pbk.Tests.Integration;

/// <summary>
/// Base class for integration tests with test server setup
/// </summary>
public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=idc_pefindo_pbk_test;Username=postgres;Password=test_password;",
                    ["PefindoConfig:BaseUrl"] = "https://mock-api.test.com",
                    ["PefindoConfig:Username"] = "test_user",
                    ["PefindoConfig:Password"] = "test_password",
                    ["TEST01"] = "1" // Enable test mode
                });
            });
        });

        _client = _factory.CreateClient();
    }

    protected async Task<T?> PostJsonAsync<T>(string requestUri, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync(requestUri, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrEmpty(responseContent))
            return default(T);

        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
