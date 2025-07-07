using idc.pefindo.pbk.Services.Interfaces.Logging;

namespace idc.pefindo.pbk.Middleware;

public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationService correlationService)
    {
        // Get or generate correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? correlationService.GetCorrelationId();

        var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
            ?? correlationService.GetRequestId();

        // Set correlation context
        correlationService.SetCorrelationContext(correlationId, requestId);

        // Add correlation ID to response headers
        context.Response.Headers.Append("X-Correlation-ID", correlationId);
        context.Response.Headers.Append("X-Request-ID", requestId);

        // Add to structured logging context
        using var logContext = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = requestId
        });

        await _next(context);
    }
}