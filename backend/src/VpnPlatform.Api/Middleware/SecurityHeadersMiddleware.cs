namespace VpnPlatform.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private const string ApiContentSecurityPolicy = "default-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'";
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        if (!IsDevelopmentSwagger(context))
        {
            headers["Content-Security-Policy"] = ApiContentSecurityPolicy;
        }

        if (_environment.IsProduction())
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        await _next(context);
    }

    private bool IsDevelopmentSwagger(HttpContext context)
        => _environment.IsDevelopment()
            && context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
}
