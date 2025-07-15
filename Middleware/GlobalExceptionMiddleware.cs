using System.Net;
using System.Text.Json;
using FluentValidation;

namespace idc.pefindo.pbk.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? "N/A";
            _logger.LogError(ex, "An unhandled exception occurred. Correlation ID: {CorrelationId}", correlationId);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Check if response has already started
        if (context.Response.HasStarted)
        {
            // Cannot modify headers or status code after response has started
            return;
        }

        context.Response.ContentType = "application/json";

        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var problemDetails = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Validation Error",
                Detail = string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage)),
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },
            InvalidOperationException invalidOpEx => new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Business Rule Violation",
                Detail = invalidOpEx.Message,
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },
            ArgumentException argEx when argEx is ArgumentNullException argNullEx => new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Missing Required Parameter",
                Detail = $"Required parameter '{argNullEx.ParamName}' is missing or null",
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },
            ArgumentException argEx => new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Invalid Argument",
                Detail = argEx.Message,
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },
            TimeoutException timeoutEx => new ProblemDetails
            {
                Type = "https://httpstatuses.com/408",
                Title = "Request Timeout",
                Detail = "The request timed out while processing",
                Status = (int)HttpStatusCode.RequestTimeout,
                Instance = context.Request.Path
            },
            TaskCanceledException taskEx when taskEx.InnerException is TimeoutException => new ProblemDetails
            {
                Type = "https://httpstatuses.com/408",
                Title = "Request Timeout",
                Detail = "The request was cancelled due to timeout",
                Status = (int)HttpStatusCode.RequestTimeout,
                Instance = context.Request.Path
            },
            HttpRequestException httpEx => new ProblemDetails
            {
                Type = "https://httpstatuses.com/502",
                Title = "External Service Error",
                Detail = isDevelopment ? httpEx.Message : "Error communicating with external service",
                Status = (int)HttpStatusCode.BadGateway,
                Instance = context.Request.Path
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Type = "https://httpstatuses.com/401",
                Title = "Unauthorized",
                Detail = "Access denied",
                Status = (int)HttpStatusCode.Unauthorized,
                Instance = context.Request.Path
            },
            NotImplementedException => new ProblemDetails
            {
                Type = "https://httpstatuses.com/501",
                Title = "Not Implemented",
                Detail = "This feature is not yet implemented",
                Status = (int)HttpStatusCode.NotImplemented,
                Instance = context.Request.Path
            },
            _ => new ProblemDetails
            {
                Type = "https://httpstatuses.com/500",
                Title = "Internal Server Error",
                Detail = isDevelopment ? exception.Message : "An unexpected error occurred",
                Status = (int)HttpStatusCode.InternalServerError,
                Instance = context.Request.Path
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Problem details class for structured error responses
/// </summary>
public class ProblemDetails
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
}
