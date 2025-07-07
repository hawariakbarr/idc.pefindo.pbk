using System.Text;
using idc.pefindo.pbk.Models.Logging;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using idc.pefindo.pbk.Utilities;

namespace idc.pefindo.pbk.Handlers;

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly IHttpRequestLogger _httpLogger;
    private readonly ICorrelationService _correlationService;
    private readonly ILogger<HttpLoggingHandler> _logger;

    public HttpLoggingHandler(
        IHttpRequestLogger httpLogger,
        ICorrelationService correlationService,
        ILogger<HttpLoggingHandler> logger)
    {
        _httpLogger = httpLogger;
        _correlationService = correlationService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var correlationId = _correlationService.GetCorrelationId();
        var requestId = _correlationService.GetRequestId();

        // Determine service name from URL
        var serviceName = DetermineServiceName(request.RequestUri);

        var logEntry = new HttpRequestLogEntry
        {
            CorrelationId = correlationId,
            RequestId = requestId,
            ServiceName = serviceName,
            Method = request.Method.Method,
            Url = SensitiveDataSanitizer.SanitizeUrl(request.RequestUri?.ToString()),
            RequestTime = startTime,
            RequestHeaders = SerializeHeaders(request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))),
            RequestBody = await GetSanitizedRequestBodyAsync(request)
        };

        try
        {
            // Add correlation headers to outgoing request
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
            request.Headers.TryAddWithoutValidation("X-Request-ID", requestId);

            var response = await base.SendAsync(request, cancellationToken);

            logEntry.ResponseTime = DateTime.UtcNow;
            logEntry.DurationMs = (int)(logEntry.ResponseTime.Value - startTime).TotalMilliseconds;
            logEntry.ResponseStatusCode = (int)response.StatusCode;
            logEntry.IsSuccessful = response.IsSuccessStatusCode;
            logEntry.ResponseHeaders = SerializeHeaders(response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));
            logEntry.ResponseBody = SensitiveDataSanitizer.SanitizeForLogging(await response.Content.ReadAsStringAsync(cancellationToken));

            // Log async to avoid blocking
            _ = Task.Run(() => _httpLogger.LogRequestAsync(logEntry), cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            logEntry.ResponseTime = DateTime.UtcNow;
            logEntry.DurationMs = (int)(logEntry.ResponseTime.Value - startTime).TotalMilliseconds;
            logEntry.IsSuccessful = false;
            logEntry.ErrorMessage = ex.Message;

            // Log async to avoid blocking
            _ = Task.Run(() => _httpLogger.LogRequestAsync(logEntry), cancellationToken);

            throw;
        }
    }

    private static string DetermineServiceName(Uri? requestUri)
    {
        if (requestUri == null) return "Unknown";

        var host = requestUri.Host.ToLowerInvariant();
        var path = requestUri.AbsolutePath.ToLowerInvariant();

        return host switch
        {
            var h when h.Contains("pefindo") => path.Contains("pbk") ? "PefindoPBK" : "PefindoIDScore",
            var h when h.Contains("mock") => "MockService",
            _ => "ExternalService"
        };
    }

    private static string SerializeHeaders(Dictionary<string, string> headers)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(headers);
        }
        catch
        {
            return "{}";
        }
    }

    private static async Task<string?> GetSanitizedRequestBodyAsync(HttpRequestMessage request)
    {
        try
        {
            if (request.Content == null) return null;

            var content = await request.Content.ReadAsStringAsync();
            return SensitiveDataSanitizer.SanitizeForLogging(content);
        }
        catch
        {
            return "[Unable to read request body]";
        }
    }
}