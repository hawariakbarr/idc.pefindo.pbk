using Serilog;
using FluentValidation;
using Microsoft.OpenApi.Models;
using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.Services;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Utilities;
using idc.pefindo.pbk.Models.Validators;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Register configuration
builder.Services.Configure<PefindoConfig>(
    builder.Configuration.GetSection("PefindoConfig"));
builder.Services.Configure<DatabaseConfig>(
    builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<SimilarityConfig>(
    builder.Configuration.GetSection("SimilarityConfig"));

// Register database connection
builder.Services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

// Register repositories
builder.Services.AddScoped<IGlobalConfigRepository, GlobalConfigRepository>();

// Register services
builder.Services.AddScoped<ICycleDayValidationService, CycleDayValidationService>();
builder.Services.AddScoped<IIndividualProcessingService, IndividualProcessingService>();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<IndividualRequestValidator>();

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
    
    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
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
// Ensure the connection string is not null or empty before passing it to AddNpgSql
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("The connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddCheck("CustomHealthCheck", () => 
        HealthCheckResult.Healthy("The health check passed."))
    .AddCheck("AnotherHealthCheck", () =>
        HealthCheckResult.Degraded("Another health check is degraded."))
    .AddCheck("ThirdHealthCheck", () =>
        HealthCheckResult.Unhealthy("The third health check is unhealthy."));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IDC Pefindo PBK API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at app's root
    });
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting IDC Pefindo PBK API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
