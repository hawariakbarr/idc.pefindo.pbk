using idc.pefindo.pbk.Configuration;
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
                // Add test database configuration
                var testConfig = TestHelper.CreateTestDatabaseConfiguration();

                // Add additional test configuration
                testConfig.Add("PefindoConfig:BaseUrl", "https://mock-api.test.com");
                testConfig.Add("PefindoConfig:Username", "test_user");
                testConfig.Add("PefindoConfig:Password", "test_password");
                testConfig.Add("PefindoConfig:Domain", "mock-api.test.com");
                testConfig.Add("TEST01", "1"); // Enable test mode
                testConfig.Add("SimilarityConfig:DefaultNameThreshold", "0.8");
                testConfig.Add("SimilarityConfig:DefaultMotherNameThreshold", "0.7");
                testConfig.Add("CycleDayConfig:ConfigCode", "GC31");
                testConfig.Add("CycleDayConfig:DefaultCycleDay", "7");

                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                // Remove real database services
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(IDbConnectionFactory) ||
                    d.ServiceType == typeof(IGlobalConfigRepository) ||
                    d.ServiceType == typeof(IPbkDataRepository) ||
                    d.ServiceType == typeof(IPefindoApiService)).ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Register mock services for tests
                services.AddScoped<IDbConnectionFactory, MockDbConnectionFactory>();
                services.AddScoped<IGlobalConfigRepository, MockGlobalConfigRepository>();
                services.AddScoped<IPbkDataRepository, MockPbkDataRepository>();
                services.AddScoped<IPefindoApiService, MockPefindoApiService>();

                // Remove health check that might fail in test environment
                var healthCheckDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DatabaseHealthCheck));
                if (healthCheckDescriptor != null)
                {
                    services.Remove(healthCheckDescriptor);
                }
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
