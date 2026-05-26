using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Payments;
using Xunit;

namespace VpnPlatform.UnitTests;

public class YooKassaPaymentTests
{
    [Theory]
    [InlineData("pending", false, PaymentStatus.Pending)]
    [InlineData("waiting_for_capture", true, PaymentStatus.WaitingConfirmation)]
    [InlineData("succeeded", true, PaymentStatus.Succeeded)]
    [InlineData("canceled", false, PaymentStatus.Cancelled)]
    [InlineData("unknown-paid-provider-state", true, PaymentStatus.Unknown)]
    [InlineData("unknown", false, PaymentStatus.Unknown)]
    public void YooKassa_Status_Mapper_Should_Map_Provider_Statuses(string providerStatus, bool paid, PaymentStatus expected)
    {
        var actual = YooKassaPaymentStatusMapper.MapPaymentStatus(providerStatus, paid);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("pending", RefundStatus.Pending)]
    [InlineData("succeeded", RefundStatus.Succeeded)]
    [InlineData("canceled", RefundStatus.Cancelled)]
    [InlineData("unknown", RefundStatus.Unknown)]
    public void YooKassa_Refund_Status_Mapper_Should_Map_Provider_Statuses(string providerStatus, RefundStatus expected)
    {
        var actual = YooKassaPaymentStatusMapper.MapRefundStatus(providerStatus);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task YooKassa_ParseWebhook_Should_Read_Event_PaymentId_And_Status()
    {
        var provider = new YooKassaPaymentProvider(null!, null!, NullLogger<YooKassaPaymentProvider>.Instance, new TestHostEnvironment());
        const string raw = """
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"22d6d597-000f-5000-9000-145f6df21d6f",
            "status":"succeeded",
            "paid":true,
            "amount":{"value":"490.00","currency":"RUB"}
          }
        }
        """;

        var parsed = await provider.ParseWebhookAsync(raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal("payment.succeeded:22d6d597-000f-5000-9000-145f6df21d6f", parsed.ExternalEventId);
        Assert.Equal("payment.succeeded", parsed.EventType);
        Assert.Equal("22d6d597-000f-5000-9000-145f6df21d6f", parsed.PaymentId);
        Assert.Equal(PaymentStatus.Succeeded, parsed.Status);
    }

    [Fact]
    public async Task YooKassa_LocalSandboxWebhookVerification_Should_Reject_When_Test_Header_Is_Missing()
    {
        var provider = new YooKassaPaymentProvider(null!, null!, NullLogger<YooKassaPaymentProvider>.Instance, new TestHostEnvironment());
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            SecretKeyProtected = string.Empty
        };
        var parsed = new VpnPlatform.Application.DTOs.PaymentWebhookParseResult(
            "payment.succeeded:yk_sandbox_1",
            "payment.succeeded",
            "yk_sandbox_1",
            PaymentStatus.Succeeded,
            "{}",
            false);

        var result = await provider.VerifyAsync(account, parsed, "{}", new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("local-sandbox-header", result.Method);
    }

    [Fact]
    public async Task YooKassa_LocalSandboxWebhookVerification_Should_Accept_When_Test_Header_Is_Present()
    {
        var provider = new YooKassaPaymentProvider(null!, null!, NullLogger<YooKassaPaymentProvider>.Instance, new TestHostEnvironment());
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            SecretKeyProtected = string.Empty
        };
        var parsed = new VpnPlatform.Application.DTOs.PaymentWebhookParseResult(
            "payment.succeeded:yk_sandbox_1",
            "payment.succeeded",
            "yk_sandbox_1",
            PaymentStatus.Succeeded,
            "{}",
            false);
        var headers = new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" };

        var result = await provider.VerifyAsync(account, parsed, "{}", headers, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Equal("local-sandbox-header", result.Method);
    }


    [Fact]
    public async Task YooKassa_LocalSandbox_CreatePayment_Should_Write_Request_Shape_Without_Secrets()
    {
        var provider = new YooKassaPaymentProvider(null!, null!, NullLogger<YooKassaPaymentProvider>.Instance, new TestHostEnvironment());
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 490m,
            Currency = "RUB",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Provider = PaymentProvider.YooKassa,
            Amount = 490m,
            Currency = "RUB",
            IdempotencyKey = "idem-1"
        };
        var account = new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            SecretKeyProtected = string.Empty,
            ReturnUrl = "https://example.test/return"
        };

        var result = await provider.CreatePaymentAsync(new VpnPlatform.Application.DTOs.PaymentCreateRequest(order, payment, account, "https://example.test/return"), CancellationToken.None);

        Assert.StartsWith("yk_sandbox_", result.PaymentId);
        Assert.Contains("paymentId=yk_sandbox_", result.RedirectUrl);
        Assert.Contains("\"amount\":490", payment.RawRequest);
        Assert.Contains("\"currency\":\"RUB\"", payment.RawRequest);
        Assert.DoesNotContain("secret", payment.RawRequest.ToLowerInvariant());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
