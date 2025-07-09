using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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

                // Override connection strings to avoid health check issues
                testConfig.Add("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test_db;Username=test_user;Password=test_password;");

                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                // Remove real database services and logging services
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(IDbConnectionFactory) ||
                    d.ServiceType == typeof(IGlobalConfigRepository) ||
                    d.ServiceType == typeof(IPbkDataRepository) ||
                    d.ServiceType == typeof(IPefindoApiService) ||
                    d.ServiceType == typeof(ICorrelationLogger) ||
                    d.ServiceType == typeof(IProcessStepLogger) ||
                    d.ServiceType == typeof(IHttpRequestLogger) ||
                    d.ServiceType == typeof(IErrorLogger) ||
                    d.ServiceType == typeof(IAuditLogger) ||
                    d.ServiceType == typeof(ICorrelationService)).ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Register mock services for tests
                services.AddScoped<IDbConnectionFactory, MockDbConnectionFactory>();
                services.AddScoped<IGlobalConfigRepository, MockGlobalConfigRepository>();
                services.AddScoped<IPbkDataRepository, MockPbkDataRepository>();
                services.AddScoped<IPefindoApiService, MockPefindoApiService>();

                // Register mock logging services as SINGLETON to maintain state across scopes
                // This is critical for integration tests to capture log entries
                services.AddScoped<ICorrelationService, MockCorrelationService>(); // Keep scoped for per-request context
                services.AddSingleton<ICorrelationLogger, MockCorrelationLogger>(); // Changed to Singleton
                services.AddSingleton<IProcessStepLogger, MockProcessStepLogger>(); // Already singleton
                services.AddSingleton<IHttpRequestLogger, MockHttpRequestLogger>(); // Changed to Singleton
                services.AddSingleton<IErrorLogger, MockErrorLogger>(); // Changed to Singleton
                services.AddSingleton<IAuditLogger, MockAuditLogger>(); // Changed to Singleton

                // Remove all health check services and add a simple one that always returns healthy
                services.RemoveAll(typeof(Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck));
                // services.AddHealthChecks();
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