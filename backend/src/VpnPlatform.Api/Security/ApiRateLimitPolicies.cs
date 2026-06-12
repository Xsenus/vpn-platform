using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace VpnPlatform.Api.Security;

public sealed record ApiRateLimitPolicySettings(int PermitLimit, int WindowSeconds);

public static class ApiRateLimitPolicies
{
    public const string AuthSensitive = "auth-sensitive";
    public const string PublicCheckout = "public-checkout";
    public const string Webhook = "webhook";

    public static readonly IReadOnlyDictionary<string, ApiRateLimitPolicySettings> Defaults = new Dictionary<string, ApiRateLimitPolicySettings>
    {
        [AuthSensitive] = new(10, 60),
        [PublicCheckout] = new(20, 60),
        [Webhook] = new(120, 60)
    };

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/429",
                title = "Too many requests",
                status = StatusCodes.Status429TooManyRequests,
                detail = "Request rate limit exceeded. Please retry later."
            }, cancellationToken);
        };

        AddFixedWindowPolicy(options, AuthSensitive);
        AddFixedWindowPolicy(options, PublicCheckout);
        AddFixedWindowPolicy(options, Webhook);
    }

    private static void AddFixedWindowPolicy(RateLimiterOptions options, string policyName)
    {
        var settings = Defaults[policyName];
        options.AddPolicy(policyName, httpContext =>
        {
            var partitionKey = BuildPartitionKey(httpContext, policyName);
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });
    }

    private static string BuildPartitionKey(HttpContext context, string policyName)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        var ip = string.IsNullOrWhiteSpace(forwardedFor)
            ? context.Connection.RemoteIpAddress?.ToString()
            : forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        var normalizedIp = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip;
        return $"{policyName}:{normalizedIp}:{context.Request.Path.Value?.ToLowerInvariant()}";
    }
}
