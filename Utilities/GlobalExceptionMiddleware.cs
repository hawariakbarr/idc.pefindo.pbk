using System.Net;
using System.Text.Json;
using FluentValidation;

namespace idc.pefindo.pbk.Utilities;

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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var problemDetails = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Title = "Validation Error",
                Detail = validationEx.Message,
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },
            InvalidOperationException invalidOpEx => new ProblemDetails
            {
                Title = "Business Rule Violation",
                Detail = invalidOpEx.Message,
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },
            ArgumentException argEx => new ProblemDetails
            {
                Title = "Invalid Argument",
                Detail = argEx.Message,
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },
            TimeoutException timeoutEx => new ProblemDetails
            {
                Title = "Request Timeout",
                Detail = timeoutEx.Message,
                Status = (int)HttpStatusCode.RequestTimeout,
                Instance = context.Request.Path
            },
            HttpRequestException httpEx => new ProblemDetails
            {
                Title = "External Service Error",
                Detail = "Error communicating with external service",
                Status = (int)HttpStatusCode.BadGateway,
                Instance = context.Request.Path
            },
            _ => new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred",
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
