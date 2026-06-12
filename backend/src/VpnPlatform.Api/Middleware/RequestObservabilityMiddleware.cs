using System.Diagnostics;
using VpnPlatform.Api.Observability;

namespace VpnPlatform.Api.Middleware;

public sealed class RequestObservabilityMiddleware
{
    private const string CorrelationHeaderName = "X-Correlation-Id";
    private readonly ApiObservabilityMetrics _metrics;
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        ILogger<RequestObservabilityMiddleware> logger,
        ApiObservabilityMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task Invoke(HttpContext context)
    {
        _metrics.OnRequestStarted();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode <= 0
                ? StatusCodes.Status500InternalServerError
                : context.Response.StatusCode;
            var route = context.GetEndpoint()?.DisplayName
                ?? context.Request.Path.Value
                ?? "/";
            var correlationId = context.Items[CorrelationHeaderName]?.ToString()
                ?? context.TraceIdentifier;

            _metrics.OnRequestCompleted(context.Request.Method, route, statusCode, elapsedMs);
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                elapsedMs,
                correlationId);
        }
    }
}
