using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace VpnPlatform.Api.Observability;

public sealed class ApiObservabilityMetrics
{
    private readonly ConcurrentDictionary<RequestMetricKey, RequestMetricValue> _requests = new();
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
    private long _durationMsTotal;
    private long _requestsCompleted;
    private long _requestsInFlight;
    private long _requestsStarted;

    public DateTimeOffset StartedAt => _startedAt;
    public TimeSpan Uptime => DateTimeOffset.UtcNow - _startedAt;
    public long RequestsStarted => Interlocked.Read(ref _requestsStarted);
    public long RequestsCompleted => Interlocked.Read(ref _requestsCompleted);
    public long RequestsInFlight => Interlocked.Read(ref _requestsInFlight);
    public long DurationMsTotal => Interlocked.Read(ref _durationMsTotal);

    public void OnRequestStarted()
    {
        Interlocked.Increment(ref _requestsStarted);
        Interlocked.Increment(ref _requestsInFlight);
    }

    public void OnRequestCompleted(string? method, string? route, int statusCode, long elapsedMs)
    {
        Interlocked.Increment(ref _requestsCompleted);
        Interlocked.Decrement(ref _requestsInFlight);
        Interlocked.Add(ref _durationMsTotal, Math.Max(0, elapsedMs));

        var key = new RequestMetricKey(
            NormalizeLabel(method),
            NormalizeLabel(route),
            $"{Math.Clamp(statusCode, 100, 599) / 100}xx");

        _requests.AddOrUpdate(
            key,
            _ => new RequestMetricValue(1, Math.Max(0, elapsedMs)),
            (_, value) => value.Add(Math.Max(0, elapsedMs)));
    }

    public string ToPrometheus()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# HELP vpnplatform_api_info VPN Platform API info");
        builder.AppendLine("# TYPE vpnplatform_api_info gauge");
        builder.AppendLine("vpnplatform_api_info 1");
        builder.AppendLine("# HELP vpnplatform_api_uptime_seconds API process uptime in seconds");
        builder.AppendLine("# TYPE vpnplatform_api_uptime_seconds gauge");
        builder.AppendLine($"vpnplatform_api_uptime_seconds {Math.Max(0, Uptime.TotalSeconds).ToString("F0", CultureInfo.InvariantCulture)}");
        builder.AppendLine("# HELP vpnplatform_http_requests_in_flight Current in-flight HTTP requests");
        builder.AppendLine("# TYPE vpnplatform_http_requests_in_flight gauge");
        builder.AppendLine($"vpnplatform_http_requests_in_flight {RequestsInFlight.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine("# HELP vpnplatform_http_requests_total Completed HTTP requests");
        builder.AppendLine("# TYPE vpnplatform_http_requests_total counter");

        foreach (var item in OrderedRequests())
        {
            builder.AppendLine(
                $"vpnplatform_http_requests_total{{method=\"{Escape(item.Key.Method)}\",route=\"{Escape(item.Key.Route)}\",status_family=\"{Escape(item.Key.StatusFamily)}\"}} {item.Value.Count.ToString(CultureInfo.InvariantCulture)}");
        }

        builder.AppendLine("# HELP vpnplatform_http_request_duration_ms_sum Total HTTP request duration in milliseconds");
        builder.AppendLine("# TYPE vpnplatform_http_request_duration_ms_sum counter");

        foreach (var item in OrderedRequests())
        {
            builder.AppendLine(
                $"vpnplatform_http_request_duration_ms_sum{{method=\"{Escape(item.Key.Method)}\",route=\"{Escape(item.Key.Route)}\",status_family=\"{Escape(item.Key.StatusFamily)}\"}} {item.Value.DurationMs.ToString(CultureInfo.InvariantCulture)}");
        }

        return builder.ToString();
    }

    private IEnumerable<KeyValuePair<RequestMetricKey, RequestMetricValue>> OrderedRequests()
    {
        return _requests
            .OrderBy(x => x.Key.Method, StringComparer.Ordinal)
            .ThenBy(x => x.Key.Route, StringComparer.Ordinal)
            .ThenBy(x => x.Key.StatusFamily, StringComparer.Ordinal);
    }

    private static string NormalizeLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var normalized = value.Trim();
        return normalized.Length <= 160 ? normalized : normalized[..160];
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private sealed record RequestMetricKey(string Method, string Route, string StatusFamily);

    private sealed record RequestMetricValue(long Count, long DurationMs)
    {
        public RequestMetricValue Add(long elapsedMs) => new(Count + 1, DurationMs + elapsedMs);
    }
}
