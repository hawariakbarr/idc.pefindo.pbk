using idc.pefindo.pbk.Services.Interfaces.Logging;

namespace idc.pefindo.pbk.Services.Logging;

public class CorrelationService : ICorrelationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CorrelationIdKey = "CorrelationId";
    private const string RequestIdKey = "RequestId";
    private const string UserIdKey = "UserId";

    public CorrelationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.ContainsKey(CorrelationIdKey) == true)
        {
            return context.Items[CorrelationIdKey]?.ToString() ?? GenerateCorrelationId();
        }

        var correlationId = GenerateCorrelationId();
        SetCorrelationId(correlationId);
        return correlationId;
    }

    public string GetRequestId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.ContainsKey(RequestIdKey) == true)
        {
            return context.Items[RequestIdKey]?.ToString() ?? GenerateRequestId();
        }

        var requestId = GenerateRequestId();
        SetRequestId(requestId);
        return requestId;
    }

    public void SetCorrelationContext(string correlationId, string requestId, string? userId = null)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            context.Items[CorrelationIdKey] = correlationId;
            context.Items[RequestIdKey] = requestId;
            if (!string.IsNullOrEmpty(userId))
            {
                context.Items[UserIdKey] = userId;
            }
        }
    }

    private void SetCorrelationId(string correlationId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            context.Items[CorrelationIdKey] = correlationId;
        }
    }

    private void SetRequestId(string requestId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            context.Items[RequestIdKey] = requestId;
        }
    }

    private static string GenerateCorrelationId() => $"corr-{Guid.NewGuid():N}";
    private static string GenerateRequestId() => $"req-{Guid.NewGuid():N}";
}
