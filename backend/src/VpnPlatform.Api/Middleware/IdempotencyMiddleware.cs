namespace VpnPlatform.Api.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(key))
            {
                context.Items["Idempotency-Key"] = key;
            }
        }

        await _next(context);
    }
}
