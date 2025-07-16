using EncryptionApi.Services;
using FluentValidation;
using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Handlers;
using idc.pefindo.pbk.Middleware;
using idc.pefindo.pbk.Models.Validators;
using idc.pefindo.pbk.Services;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using idc.pefindo.pbk.Services.Logging;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "IDC-Pefindo-PBK")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add HttpContextAccessor for correlation service
builder.Services.AddHttpContextAccessor();

// Register database configuration
builder.Services.Configure<DatabaseConfiguration>(
    builder.Configuration.GetSection("DatabaseConfiguration"));

// Register configuration
builder.Services.Configure<PefindoAPIConfig>(
    builder.Configuration.GetSection("PefindoAPIConfig"));
builder.Services.Configure<GlobalConfig>(
    builder.Configuration.GetSection("GlobalConfig"));
builder.Services.Configure<PDPConfig>(
    builder.Configuration.GetSection("PDPConfig"));

// Register database connection
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

// Register repositories
builder.Services.AddScoped<IGlobalConfigRepository, GlobalConfigRepository>();
builder.Services.AddScoped<IPbkDataRepository, PbkDataRepository>();

// Register HttpClient for Pefindo API
builder.Services.AddHttpClient<IPefindoApiService, PefindoApiService>();

// Register logging services
builder.Services.AddScoped<ICorrelationService, CorrelationService>();
builder.Services.AddScoped<ICorrelationLogger, CorrelationLogger>();
builder.Services.AddScoped<IHttpRequestLogger, HttpRequestLogger>();
builder.Services.AddScoped<IProcessStepLogger, ProcessStepLogger>();
builder.Services.AddScoped<IErrorLogger, ErrorLogger>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

// Register HTTP client with logging handler
builder.Services.AddTransient<HttpLoggingHandler>();
builder.Services.AddHttpClient<IPefindoApiService, PefindoApiService>()
    .AddHttpMessageHandler<HttpLoggingHandler>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PefindoAPIConfig>>().Value;
        client.BaseAddress = new Uri(config.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
    });

// Add Memory Cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 500; // Limit cache size
});


// Register dummy response service
builder.Services.AddSingleton<IDummyResponseService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<DummyResponseService>>();
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PefindoAPIConfig>>().Value;
    return new DummyResponseService(logger, config.DummyResponseFilePath);
});

// Register core services
builder.Services.AddScoped<ICycleDayValidationService, CycleDayValidationService>();
builder.Services.AddScoped<IPefindoApiService, PefindoApiService>();
builder.Services.AddScoped<ITokenManagerService, TokenManagerService>();
builder.Services.AddScoped<ISimilarityValidationService, SimilarityValidationService>();
builder.Services.AddScoped<IDataAggregationService, DataAggregationService>();
builder.Services.AddScoped<IIndividualProcessingService, IndividualProcessingService>();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<IndividualRequestValidator>();

// Add custom middleware
builder.Services.AddScoped<IEncryptionService, EncryptionService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IDC Pefindo PBK API",
        Version = "v1",
        Description = "Middleware API for Core Banking Decision Engine and Pefindo PBK integration",
        Contact = new OpenApiContact
        {
            Name = "IDC Development Team",
            Email = "dev@idc.com"
        }
    });

    // Add correlation ID to Swagger
    c.AddServer(new OpenApiServer
    {
        Url = "/",
        Description = "Current server"
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add global headers for correlation tracking
    c.AddSecurityDefinition("CorrelationId", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-Correlation-ID",
        Type = SecuritySchemeType.ApiKey,
        Description = "Correlation ID for request tracking"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("The connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IDC Pefindo PBK API v1");
        c.RoutePrefix = string.Empty; // Available at root
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.DisplayRequestDuration();
    });

    // Also serve at /swagger for convenience
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IDC Pefindo PBK API v1");
        c.RoutePrefix = "swagger";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.DisplayRequestDuration();
    });
}


// Add custom middleware in correct order
app.UseMiddleware<GlobalExceptionMiddleware>(); // Global exception handling
app.UseMiddleware<CorrelationMiddleware>(); // Correlation ID tracking


// Standard middleware
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

        // Add correlation ID if available
        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId ?? "Unknown");
        }
    };
});


app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Basic health check endpoint
app.MapHealthChecks("/health");

// Additional health check endpoints
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Add a simple info endpoint
app.MapGet("/info", () => new
{
    Application = "IDC Pefindo PBK API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    MachineName = Environment.MachineName
});

try
{
    Log.Information("Starting IDC Pefindo PBK API with comprehensive logging");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Application started at: {StartTime}", DateTime.UtcNow);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Application shutdown completed");
    Log.CloseAndFlush();
}

public partial class Program { }
