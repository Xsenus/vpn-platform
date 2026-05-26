using System.Diagnostics;

namespace VpnPlatform.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() ?? Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[HeaderName] = correlationId;
        context.Items[HeaderName] = correlationId;
        await _next(context);
    }
}
