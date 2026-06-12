using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using VpnPlatform.Api.Controllers.Auth;
using VpnPlatform.Api.Controllers.Channels;
using VpnPlatform.Api.Controllers.Public;
using VpnPlatform.Api.Controllers.Webhooks;
using VpnPlatform.Api.Security;
using Xunit;

namespace VpnPlatform.UnitTests;

public class RateLimitingSecurityTests
{
    [Fact]
    public void Rate_Limit_Policies_Should_Have_Conservative_Defaults()
    {
        Assert.Equal(new[] { ApiRateLimitPolicies.AuthSensitive, ApiRateLimitPolicies.PublicCheckout, ApiRateLimitPolicies.Webhook }.OrderBy(x => x), ApiRateLimitPolicies.Defaults.Keys.OrderBy(x => x));

        Assert.Equal(10, ApiRateLimitPolicies.Defaults[ApiRateLimitPolicies.AuthSensitive].PermitLimit);
        Assert.Equal(20, ApiRateLimitPolicies.Defaults[ApiRateLimitPolicies.PublicCheckout].PermitLimit);
        Assert.Equal(120, ApiRateLimitPolicies.Defaults[ApiRateLimitPolicies.Webhook].PermitLimit);

        Assert.All(ApiRateLimitPolicies.Defaults.Values, settings =>
        {
            Assert.InRange(settings.PermitLimit, 1, 500);
            Assert.InRange(settings.WindowSeconds, 30, 300);
        });
    }

    [Theory]
    [InlineData(nameof(AuthController.Register))]
    [InlineData(nameof(AuthController.Login))]
    [InlineData(nameof(AuthController.Refresh))]
    [InlineData(nameof(AuthController.ForgotPassword))]
    [InlineData(nameof(AuthController.ResetPassword))]
    public void Sensitive_Auth_Endpoints_Should_Use_Auth_Rate_Limit(string methodName)
    {
        var method = typeof(AuthController).GetMethod(methodName);

        Assert.NotNull(method);
        Assert.Equal(ApiRateLimitPolicies.AuthSensitive, ReadMethodPolicy(method!));
    }

    [Theory]
    [InlineData(nameof(OrdersController.CreateCheckoutSession))]
    [InlineData(nameof(OrdersController.CreateAnonymousOrder))]
    public void Public_Checkout_Endpoints_Should_Use_Checkout_Rate_Limit(string methodName)
    {
        var method = typeof(OrdersController).GetMethod(methodName);

        Assert.NotNull(method);
        Assert.Equal(ApiRateLimitPolicies.PublicCheckout, ReadMethodPolicy(method!));
    }

    [Theory]
    [InlineData(typeof(PaymentWebhooksController))]
    [InlineData(typeof(ChannelWebhooksController))]
    public void Webhook_Controllers_Should_Use_Webhook_Rate_Limit(Type controllerType)
    {
        Assert.Equal(ApiRateLimitPolicies.Webhook, ReadTypePolicy(controllerType));
    }

    [Fact]
    public void Api_Program_Should_Register_Rate_Limiter_Middleware()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "Program.cs"));

        Assert.Contains("builder.Services.AddRateLimiter(ApiRateLimitPolicies.Configure)", program, StringComparison.Ordinal);
        Assert.Contains("app.UseRateLimiter()", program, StringComparison.Ordinal);
        Assert.Contains("app.UseCors(\"default\");", program, StringComparison.Ordinal);
        Assert.True(program.IndexOf("app.UseCors(\"default\");", StringComparison.Ordinal) < program.IndexOf("app.UseRateLimiter()", StringComparison.Ordinal));
        Assert.True(program.IndexOf("app.UseRateLimiter()", StringComparison.Ordinal) < program.IndexOf("app.UseAuthentication()", StringComparison.Ordinal));
    }

    private static string? ReadMethodPolicy(MethodInfo method)
        => method.GetCustomAttributes<EnableRateLimitingAttribute>().SingleOrDefault()?.PolicyName;

    private static string? ReadTypePolicy(Type type)
        => type.GetCustomAttributes<EnableRateLimitingAttribute>().SingleOrDefault()?.PolicyName;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "README.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
