using System.Diagnostics;

namespace VpnPlatform.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string HeaderName = "X-Correlation-Id";
    private const int MaxHeaderLength = 128;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = NormalizeCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault())
            ?? Activity.Current?.Id
            ?? context.TraceIdentifier
            ?? Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation_id", correlationId);
        context.Response.Headers[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceIdentifier"] = context.TraceIdentifier ?? string.Empty
        }))
        {
            await _next(context);
        }
    }

    private static string? NormalizeCorrelationId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= MaxHeaderLength ? trimmed : trimmed[..MaxHeaderLength];
    }
}
