using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text;
using System.Text.Json;
using Xunit;

namespace idc.pefindo.pbk.Tests.Integration;

/// <summary>
/// Base class for integration tests with test server setup
/// </summary>
public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Set the correct content root
            builder.UseContentRoot(GetProjectPath());
            
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=idc_pefindo_pbk_test;Username=postgres;Password=postgres;",
                    ["PefindoConfig:BaseUrl"] = "https://mock-api.test.com",
                    ["PefindoConfig:Username"] = "test_user", 
                    ["PefindoConfig:Password"] = "test_password",
                    ["PefindoConfig:Domain"] = "mock-api.test.com",
                    ["TEST01"] = "1" // Enable test mode
                });
            });

            // In your test base class, register the mock:
            builder.ConfigureServices(services =>
            {
                // Register missing services for tests
                services.AddScoped<IDbConnectionFactory, MockDbConnectionFactory>();
                services.AddScoped<IGlobalConfigRepository, MockGlobalConfigRepository>();
                services.AddScoped<IPbkDataRepository, MockPbkDataRepository>();
                services.AddScoped<IPefindoApiService, MockPefindoApiService>();
            });
        });

        _client = _factory.CreateClient();
    }

    private static string GetProjectPath()
    {
        // Get the directory where the test assembly is located
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation)!);
        
        // Navigate up to find the solution root (where .sln file is)
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }
        
        if (directory == null)
        {
            throw new DirectoryNotFoundException("Could not find solution root directory");
        }
        
        // Return the path to the main project
        var projectPath = Path.Combine(directory.FullName, "idc.pefindo.pbk");
        if (!Directory.Exists(projectPath))
        {
            // If the project folder doesn't exist with that name, use the solution root
            projectPath = directory.FullName;
        }
        
        return projectPath;
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

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
