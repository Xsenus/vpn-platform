using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentProviderSignatureTests
{
    [Fact]
    public async Task RoboKassa_ResultUrl_Signature_Should_Verify()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new RoboKassaPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));
        var shp = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Shp_account"] = "demo-merchant",
            ["Shp_order"] = "order-1",
            ["Shp_payment"] = "payment-1"
        };
        var signature = RoboKassaPaymentProvider.BuildRobokassaSignature("490.00", "123", "password2", shp, "MD5");
        var raw = $"OutSum=490.00&InvId=123&SignatureValue={signature}&Shp_account=demo-merchant&Shp_order=order-1&Shp_payment=payment-1";
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.RoboKassa,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "demo-merchant",
            WebhookSecretProtected = "password2",
            ExtraSettingsJson = "{}"
        };

        var parsed = await provider.ParseWebhookAsync(raw, new Dictionary<string, string>(), CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.True(verification.IsValid, verification.Error);
        Assert.Equal(PaymentStatus.Succeeded, parsed.Status);
        Assert.Equal("demo-merchant", parsed.ProviderAccountExternalId);
    }

    [Fact]
    public async Task YooMoney_Notification_Hmac_Should_Verify()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new YooMoneyPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));
        var form = new Dictionary<string, string>
        {
            ["notification_type"] = "p2p-incoming",
            ["operation_id"] = "op-1",
            ["amount"] = "490.00",
            ["currency"] = "643",
            ["datetime"] = "2026-04-29T10:00:00Z",
            ["sender"] = "4100",
            ["codepro"] = "false",
            ["label"] = "ym_payment_1",
            ["unaccepted"] = "false"
        };
        form["sign"] = YooMoneyPaymentProvider.BuildYooMoneySign(form, "notification-secret");
        var raw = string.Join("&", form.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.YooMoney,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "410000000000000",
            WebhookSecretProtected = "notification-secret"
        };

        var parsed = await provider.ParseWebhookAsync(raw, new Dictionary<string, string>(), CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.True(verification.IsValid, verification.Error);
        Assert.Equal(PaymentStatus.Succeeded, parsed.Status);
        Assert.Equal("RUB", parsed.Currency);
    }

    [Fact]
    public async Task YooKassa_Production_Should_Not_Accept_Local_Sandbox_Header()
    {
        var provider = new YooKassaPaymentProvider(null!, null!, NullLogger<YooKassaPaymentProvider>.Instance, new TestHostEnvironment(Environments.Production));
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "123",
            SecretKeyProtected = "secret"
        };
        var parsed = new PaymentWebhookParseResult("event-1", "payment.succeeded", "payment-1", PaymentStatus.Succeeded, "{}", false, 490m, "RUB", true, "123");
        var headers = new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" };

        var verification = await provider.VerifyAsync(account, parsed, "{}", headers, CancellationToken.None);

        Assert.False(verification.IsValid);
        Assert.Equal("production-sandbox-header-denied", verification.Method);
    }


    [Fact]
    public void PaymentProviderAccountDto_Should_Mask_Secrets_As_Boolean_Flags()
    {
        var account = new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Production,
            Name = "prod",
            PublicName = "YooKassa",
            IsEnabled = true,
            ShopId = "123456",
            SecretKeyProtected = "super-secret",
            WebhookSecretProtected = "webhook-secret"
        };

        var dto = PaymentProviderAccountService.MapToDto(account);

        Assert.True(dto.HasSecretKey);
        Assert.True(dto.HasWebhookSecret);
        Assert.DoesNotContain("super-secret", dto.ToString()!.ToLowerInvariant());
        Assert.DoesNotContain("webhook-secret", dto.ToString()!.ToLowerInvariant());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 4, 29, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(0, value.Length - visibleTail)) + value[^Math.Min(visibleTail, value.Length)..];
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public class AdditionalPaymentProviderSignatureTests
{
    [Theory]
    [InlineData(PaymentProvider.Stripe)]
    [InlineData(PaymentProvider.PayPal)]
    [InlineData(PaymentProvider.TBankAcquiring)]
    public async Task Credentialless_LocalSandbox_Refund_Should_Succeed_Without_Http(PaymentProvider providerType)
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var http = new HttpClient(new RejectHttpHandler());
        var factory = new StaticHttpClientFactory(http);
        IPaymentProvider provider = providerType switch
        {
            PaymentProvider.Stripe => new StripePaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            PaymentProvider.PayPal => new PayPalPaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            PaymentProvider.TBankAcquiring => new TBankAcquiringPaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        var account = new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = providerType,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            ShopId = $"local-{providerType.ToString().ToLowerInvariant()}"
        };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            Provider = providerType,
            ProviderPaymentId = $"payment-{Guid.NewGuid():N}",
            Amount = 490m,
            Currency = "RUB",
            Status = PaymentStatus.Succeeded
        };

        var result = await provider.RefundAsync(payment, account, 120m, "local sandbox refund", CancellationToken.None);

        Assert.Equal(RefundStatus.Succeeded, result.Status);
        Assert.Contains("sandbox", result.RefundId, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"status\":\"succeeded\"", result.RawResponse, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stripe_Webhook_Signature_Should_Verify()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new StripePaymentProvider(null!, accounts, new TestHostEnvironment(Environments.Production));
        var raw = "{\"id\":\"evt_1\",\"type\":\"checkout.session.completed\",\"data\":{\"object\":{\"id\":\"cs_test_1\",\"payment_status\":\"paid\",\"amount_total\":49000,\"currency\":\"rub\",\"metadata\":{\"orderId\":\"11111111111111111111111111111111\"}}}}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expected = HmacSha256Hex("whsec_test", $"{timestamp}.{raw}");
        var headers = new Dictionary<string, string> { ["Stripe-Signature"] = $"t={timestamp},v1={expected}" };
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.Stripe,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "acct_1",
            SecretKeyProtected = "sk_test",
            WebhookSecretProtected = "whsec_test"
        };

        var parsed = await provider.ParseWebhookAsync(raw, headers, CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, headers, CancellationToken.None);

        Assert.True(verification.IsValid, verification.Error);
        Assert.Equal(PaymentStatus.Succeeded, parsed.Status);
        Assert.Equal("cs_test_1", parsed.PaymentId);
        Assert.Equal(490m, parsed.Amount);
        Assert.Equal("RUB", parsed.Currency);
    }

    [Fact]
    public async Task CloudPayments_Content_Hmac_Should_Verify()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new CloudPaymentsPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));
        var raw = "InvoiceId=cp_1&TransactionId=tx_1&Amount=490.00&Currency=RUB";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("api-secret"));
        var headers = new Dictionary<string, string> { ["Content-HMAC"] = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw))) };
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.CloudPayments,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "pk_cloud",
            SecretKeyProtected = "api-secret"
        };

        var parsed = await provider.ParseWebhookAsync(raw, headers, CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, headers, CancellationToken.None);

        Assert.True(verification.IsValid, verification.Error);
        Assert.Equal("cp_1", parsed.PaymentId);
        Assert.Equal(PaymentStatus.Succeeded, parsed.Status);
    }

    [Fact]
    public void TBank_Token_Should_Use_Root_Fields_And_Password()
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["TerminalKey"] = "MerchantTerminalKey",
            ["Amount"] = "19200",
            ["OrderId"] = "00000",
            ["Description"] = "Подарочная карта на 1000 рублей"
        };

        var token = TBankAcquiringPaymentProvider.BuildTBankToken(values, "11111111111111");

        Assert.Equal("72dd466f8ace0a37a1f740ce5fb78101712bc0665d91a8108c7c8a0ccd426db2", token);
    }

    [Fact]
    public async Task Prodamus_Sign_Header_Should_Verify()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new ProdamusPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));
        var form = new Dictionary<string, string>
        {
            ["order_id"] = "pd_payment_1",
            ["payment_id"] = "tx_1",
            ["sum"] = "490.00",
            ["currency"] = "rub",
            ["status"] = "success"
        };
        var sign = ProdamusPaymentProvider.BuildProdamusSignature(form, "prodamus-secret");
        var raw = string.Join("&", form.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.Prodamus,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "payform",
            SecretKeyProtected = "prodamus-secret",
            WebhookSecretProtected = "prodamus-secret"
        };
        var headers = new Dictionary<string, string> { ["Sign"] = sign };

        var parsed = await provider.ParseWebhookAsync(raw, headers, CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, headers, CancellationToken.None);

        Assert.True(verification.IsValid, verification.Error);
        Assert.Equal(PaymentStatus.Succeeded, parsed.Status);
        Assert.Equal("pd_payment_1", parsed.PaymentId);
    }



    [Fact]
    public async Task CloudPayments_Invalid_Hmac_Should_Reject()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new CloudPaymentsPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));
        var raw = "InvoiceId=cp_1&TransactionId=tx_1&Amount=490.00&Currency=RUB&Event=pay";
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.CloudPayments,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "pk_cloud",
            SecretKeyProtected = "api-secret"
        };
        var headers = new Dictionary<string, string> { ["Content-HMAC"] = "invalid" };

        var parsed = await provider.ParseWebhookAsync(raw, headers, CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, headers, CancellationToken.None);

        Assert.False(verification.IsValid);
    }

    [Fact]
    public async Task CloudPayments_Missing_HostedCheckoutUrl_Should_FailClosed()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new CloudPaymentsPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));
        var request = CreatePaymentCreateRequest(PaymentProvider.CloudPayments, new PaymentProviderAccount
        {
            Provider = PaymentProvider.CloudPayments,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "pk_cloud",
            SecretKeyProtected = "api-secret",
            ExtraSettingsJson = "{}"
        });

        await Assert.ThrowsAsync<NotSupportedException>(() => provider.CreatePaymentAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CloudPayments_Unknown_Event_Should_Not_Map_To_Success()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new CloudPaymentsPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));

        var parsed = await provider.ParseWebhookAsync("InvoiceId=cp_1&TransactionId=tx_1&Amount=490.00&Currency=RUB&Event=unknown", new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(PaymentStatus.Unknown, parsed.Status);
        Assert.False(parsed.Paid.GetValueOrDefault());
    }

    [Fact]
    public async Task TBank_Invalid_Token_Should_Reject()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new TBankAcquiringPaymentProvider(new StaticHttpClientFactory(new HttpClient()), accounts, new TestHostEnvironment(Environments.Production));
        var raw = "TerminalKey=terminal&PaymentId=pay_1&OrderId=order_1&Amount=49000&Status=CONFIRMED&Success=true&Token=bad";
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.TBankAcquiring,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "terminal",
            SecretKeyProtected = "terminal-password"
        };

        var parsed = await provider.ParseWebhookAsync(raw, new Dictionary<string, string>(), CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(verification.IsValid);
    }

    [Fact]
    public async Task TBank_Unknown_Status_Should_Not_Map_To_Success_When_Success_Is_True()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new TBankAcquiringPaymentProvider(new StaticHttpClientFactory(new HttpClient()), accounts, new TestHostEnvironment(Environments.Production));

        var parsed = await provider.ParseWebhookAsync("TerminalKey=terminal&PaymentId=pay_1&OrderId=order_1&Amount=49000&Status=UNKNOWN&Success=true", new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(PaymentStatus.Unknown, parsed.Status);
        Assert.False(parsed.Paid.GetValueOrDefault());
    }

    [Fact]
    public async Task TBank_Confirmed_Status_Should_Not_Map_To_Success_When_Provider_Reports_Failure()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new TBankAcquiringPaymentProvider(new StaticHttpClientFactory(new HttpClient()), accounts, new TestHostEnvironment(Environments.Production));

        var parsed = await provider.ParseWebhookAsync("TerminalKey=terminal&PaymentId=pay_1&OrderId=order_1&Amount=49000&Status=CONFIRMED&Success=false", new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(PaymentStatus.Unknown, parsed.Status);
        Assert.False(parsed.Paid.GetValueOrDefault());
    }

    [Fact]
    public async Task Prodamus_Invalid_Signature_Should_Reject()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new ProdamusPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));
        var raw = "order_id=pd_payment_1&payment_id=tx_1&sum=490.00&currency=rub&status=success";
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.Prodamus,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "payform",
            SecretKeyProtected = "prodamus-secret",
            WebhookSecretProtected = "prodamus-secret"
        };
        var headers = new Dictionary<string, string> { ["Sign"] = "invalid" };

        var parsed = await provider.ParseWebhookAsync(raw, headers, CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, headers, CancellationToken.None);

        Assert.False(verification.IsValid);
    }

    [Fact]
    public async Task Prodamus_Missing_Status_Should_Not_Map_To_Success()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new ProdamusPaymentProvider(accounts, new TestHostEnvironment(Environments.Production));

        var parsed = await provider.ParseWebhookAsync("order_id=pd_payment_1&payment_id=tx_1&sum=490.00&currency=rub", new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(PaymentStatus.Unknown, parsed.Status);
        Assert.False(parsed.Paid.GetValueOrDefault());
    }

    [Fact]
    public async Task Stripe_Expired_Timestamp_Should_Reject()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new StripePaymentProvider(new StaticHttpClientFactory(new HttpClient()), accounts, new TestHostEnvironment(Environments.Production));
        var raw = "{\"id\":\"evt_1\",\"type\":\"checkout.session.completed\",\"data\":{\"object\":{\"id\":\"cs_test_1\",\"payment_status\":\"paid\",\"amount_total\":49000,\"currency\":\"rub\"}}}";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var expected = HmacSha256Hex("whsec_test", $"{timestamp}.{raw}");
        var headers = new Dictionary<string, string> { ["Stripe-Signature"] = $"t={timestamp},v1={expected}" };
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.Stripe,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            SecretKeyProtected = "sk_test",
            WebhookSecretProtected = "whsec_test"
        };

        var parsed = await provider.ParseWebhookAsync(raw, headers, CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, headers, CancellationToken.None);

        Assert.False(verification.IsValid);
    }

    [Fact]
    public async Task Stripe_Completed_Without_Amount_Should_Not_Map_To_Success()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new StripePaymentProvider(new StaticHttpClientFactory(new HttpClient()), accounts, new TestHostEnvironment(Environments.Production));
        var raw = "{\"id\":\"evt_1\",\"type\":\"checkout.session.completed\",\"data\":{\"object\":{\"id\":\"cs_test_1\",\"payment_status\":\"paid\",\"currency\":\"rub\"}}}";

        var parsed = await provider.ParseWebhookAsync(raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(PaymentStatus.Unknown, parsed.Status);
    }

    [Fact]
    public async Task Stripe_Completed_Without_Paid_Status_Should_Not_Map_To_Success()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new StripePaymentProvider(new StaticHttpClientFactory(new HttpClient()), accounts, new TestHostEnvironment(Environments.Production));
        var raw = "{\"id\":\"evt_1\",\"type\":\"checkout.session.completed\",\"data\":{\"object\":{\"id\":\"cs_test_1\",\"amount_total\":49000,\"currency\":\"rub\"}}}";

        var parsed = await provider.ParseWebhookAsync(raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(PaymentStatus.Unknown, parsed.Status);
        Assert.False(parsed.Paid);
    }

    [Fact]
    public async Task YooKassa_Status_Recheck_Should_Reject_Webhook_Amount_Mismatch()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new YooKassaPaymentProvider(
            new StaticHttpClientFactory(new HttpClient(new YooKassaStatusStubHandler())),
            accounts,
            NullLogger<YooKassaPaymentProvider>.Instance,
            new TestHostEnvironment(Environments.Production));
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "merchant-account",
            SecretKeyProtected = "provider-secret",
            ApiBaseUrl = "https://provider.test",
            UseWebhookIpAllowList = false
        };
        var parsed = new PaymentWebhookParseResult(
            "payment.succeeded:PAYMENT-1",
            "payment.succeeded",
            "PAYMENT-1",
            PaymentStatus.Succeeded,
            "{}",
            false,
            490m,
            "RUB",
            true);

        var verification = await provider.VerifyAsync(account, parsed, "{}", new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(verification.IsValid);
        Assert.Contains("amount", verification.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PayPal_Verify_Failure_Should_Reject()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var http = new HttpClient(new PayPalStubHandler("FAILURE")) { BaseAddress = new Uri("https://api-m.sandbox.paypal.com") };
        var provider = new PayPalPaymentProvider(new StaticHttpClientFactory(http), accounts, new TestHostEnvironment(Environments.Production));
        var raw = "{\"id\":\"WH-1\",\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"resource\":{\"id\":\"capture_1\",\"status\":\"COMPLETED\",\"custom_id\":\"11111111111111111111111111111111\",\"amount\":{\"value\":\"490.00\",\"currency_code\":\"RUB\"},\"supplementary_data\":{\"related_ids\":{\"order_id\":\"ORDER-1\"}}}}";
        var headers = PayPalHeaders();
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.PayPal,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            ShopId = "client-id",
            SecretKeyProtected = "client-secret",
            WebhookSecretProtected = "webhook-id"
        };

        var parsed = await provider.ParseWebhookAsync(raw, headers, CancellationToken.None);
        var verification = await provider.VerifyAsync(account, parsed, raw, headers, CancellationToken.None);

        Assert.False(verification.IsValid);
    }

    [Fact]
    public async Task PayPal_Capture_Completed_Should_Map_To_Success()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new PayPalPaymentProvider(new StaticHttpClientFactory(new HttpClient()), accounts, new TestHostEnvironment(Environments.Production));
        var raw = "{\"id\":\"WH-1\",\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"resource\":{\"id\":\"capture_1\",\"status\":\"COMPLETED\",\"custom_id\":\"11111111111111111111111111111111\",\"amount\":{\"value\":\"490.00\",\"currency_code\":\"RUB\"},\"supplementary_data\":{\"related_ids\":{\"order_id\":\"ORDER-1\"}}}}";

        var parsed = await provider.ParseWebhookAsync(raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(PaymentStatus.Succeeded, parsed.Status);
        Assert.Equal("ORDER-1", parsed.PaymentId);
        Assert.Equal(490m, parsed.Amount);
        Assert.Equal("RUB", parsed.Currency);
    }

    [Fact]
    public async Task PayPal_Approved_Order_Capture_Should_Reject_Mismatched_Amount()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var handler = new PayPalCaptureValidationStubHandler();
        var provider = new PayPalPaymentProvider(
            new StaticHttpClientFactory(new HttpClient(handler)),
            accounts,
            new TestHostEnvironment(Environments.Production));
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.PayPal,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "client-id",
            SecretKeyProtected = "client-secret",
            ApiBaseUrl = "https://api-m.paypal.test"
        };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Provider = PaymentProvider.PayPal,
            ProviderMode = PaymentProviderMode.Production,
            ProviderPaymentId = "ORDER-1",
            Amount = 490m,
            Currency = "RUB"
        };

        var result = await ((IPaymentApprovedOrderCaptureProvider)provider)
            .CaptureApprovedOrderAsync(payment, account, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
        Assert.Contains("amount or currency", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal($"capture-{payment.Id:N}", handler.CaptureRequestId);
    }

    [Fact]
    public async Task PayPal_Recheck_Should_Not_Trust_Completed_Order_Without_Capture_Proof()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new PayPalPaymentProvider(
            new StaticHttpClientFactory(new HttpClient(new PayPalOrderWithoutCaptureStubHandler())),
            accounts,
            new TestHostEnvironment(Environments.Production));
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.PayPal,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "client-id",
            SecretKeyProtected = "client-secret",
            ApiBaseUrl = "https://api-m.paypal.test"
        };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Provider = PaymentProvider.PayPal,
            ProviderMode = PaymentProviderMode.Production,
            ProviderPaymentId = "ORDER-1",
            Amount = 490m,
            Currency = "RUB"
        };

        var result = await provider.GetStatusAsync(payment, account, CancellationToken.None);

        Assert.Equal(PaymentStatus.Unknown, result.Status);
        Assert.Contains("no confirmed capture", result.StatusReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PayPal_Capture_Should_Remain_Retryable_When_Success_Response_Has_No_Capture_Proof()
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var provider = new PayPalPaymentProvider(
            new StaticHttpClientFactory(new HttpClient(new PayPalApprovedWithoutCaptureStubHandler())),
            accounts,
            new TestHostEnvironment(Environments.Production));
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.PayPal,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = "client-id",
            SecretKeyProtected = "client-secret",
            ApiBaseUrl = "https://api-m.paypal.test"
        };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Provider = PaymentProvider.PayPal,
            ProviderMode = PaymentProviderMode.Production,
            ProviderPaymentId = "ORDER-1",
            Amount = 490m,
            Currency = "RUB"
        };

        var result = await ((IPaymentApprovedOrderCaptureProvider)provider)
            .CaptureApprovedOrderAsync(payment, account, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsRetryable);
        Assert.Contains("did not return capture proof", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(PaymentProvider.Stripe)]
    [InlineData(PaymentProvider.PayPal)]
    public async Task Refund_Should_Recover_From_Malformed_Stored_Provider_Payload(PaymentProvider providerType)
    {
        await using var db = CreateDbContext();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), new FixedClock());
        var handler = new RefundRecoveryStubHandler(providerType);
        var factory = new StaticHttpClientFactory(new HttpClient(handler));
        IPaymentProvider provider = providerType switch
        {
            PaymentProvider.Stripe => new StripePaymentProvider(factory, accounts, new TestHostEnvironment(Environments.Production)),
            PaymentProvider.PayPal => new PayPalPaymentProvider(factory, accounts, new TestHostEnvironment(Environments.Production)),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        var account = new PaymentProviderAccount
        {
            Provider = providerType,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            ShopId = providerType == PaymentProvider.PayPal ? "client-id" : "stripe-account",
            SecretKeyProtected = "protected-secret"
        };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Provider = providerType,
            ProviderMode = PaymentProviderMode.Production,
            ProviderPaymentId = providerType == PaymentProvider.Stripe ? "cs_test_1" : "ORDER-1",
            Amount = 490m,
            Currency = "RUB",
            WebhookPayload = "{malformed",
            RawResponse = """{"payment_intent":123,"purchase_units":"invalid"}"""
        };

        var result = await provider.RefundAsync(payment, account, 190m, "operator refund", CancellationToken.None);

        Assert.Equal(RefundStatus.Succeeded, result.Status);
        Assert.Equal(providerType == PaymentProvider.Stripe ? "re_test_1" : "REFUND-1", result.RefundId);
        Assert.Equal(2 + (providerType == PaymentProvider.PayPal ? 1 : 0), handler.Requests.Count);
        Assert.Contains(handler.Requests, request => request.Method == HttpMethod.Get);
        Assert.Contains(handler.Requests, request => request.Method == HttpMethod.Post && request.Path.Contains("refund", StringComparison.OrdinalIgnoreCase));
        if (providerType == PaymentProvider.PayPal)
        {
            Assert.Contains(handler.Requests, request => request.Method == HttpMethod.Post && request.Path == "/v2/payments/captures/CAPTURE-1/refund");

            var getCount = handler.Requests.Count(request => request.Method == HttpMethod.Get);
            payment.WebhookPayload = """{"resource":{"id":"CAPTURE-1","status":"COMPLETED"}}""";
            var storedCaptureResult = await provider.RefundAsync(payment, account, 90m, "stored capture refund", CancellationToken.None);

            Assert.Equal(RefundStatus.Succeeded, storedCaptureResult.Status);
            Assert.Equal(getCount, handler.Requests.Count(request => request.Method == HttpMethod.Get));
            Assert.Equal(2, handler.Requests.Count(request => request.Method == HttpMethod.Post && request.Path == "/v2/payments/captures/CAPTURE-1/refund"));
        }
    }

    private static string HmacSha256Hex(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 4, 29, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(0, value.Length - visibleTail)) + value[^Math.Min(visibleTail, value.Length)..];
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static PaymentCreateRequest CreatePaymentCreateRequest(PaymentProvider provider, PaymentProviderAccount account)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TariffId = Guid.NewGuid(),
            Amount = 490m,
            Currency = "RUB",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            PaymentProvider = provider
        };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Provider = provider,
            Amount = order.Amount,
            Currency = order.Currency,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };
        return new PaymentCreateRequest(order, payment, account, "https://merchant.example/payments/return");
    }

    private static Dictionary<string, string> PayPalHeaders()
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["PAYPAL-AUTH-ALGO"] = "SHA256withRSA",
            ["PAYPAL-CERT-URL"] = "https://api-m.sandbox.paypal.com/cert.pem",
            ["PAYPAL-TRANSMISSION-ID"] = "transmission-id",
            ["PAYPAL-TRANSMISSION-SIG"] = "signature",
            ["PAYPAL-TRANSMISSION-TIME"] = DateTimeOffset.UtcNow.ToString("O")
        };

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public StaticHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class RejectHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new Xunit.Sdk.XunitException($"Local sandbox refund attempted HTTP: {request.Method} {request.RequestUri}");
    }

    private sealed class YooKassaStatusStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"id\":\"PAYMENT-1\",\"status\":\"succeeded\",\"paid\":true,\"amount\":{\"value\":\"489.00\",\"currency\":\"RUB\"},\"metadata\":{\"orderId\":\"11111111-1111-1111-1111-111111111111\"}}",
                    Encoding.UTF8,
                    "application/json")
            });
    }

    private sealed class PayPalStubHandler : HttpMessageHandler
    {
        private readonly string _verificationStatus;
        public PayPalStubHandler(string verificationStatus) => _verificationStatus = verificationStatus;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var json = path.Contains("/v1/oauth2/token", StringComparison.OrdinalIgnoreCase)
                ? "{\"access_token\":\"access-token\",\"token_type\":\"Bearer\"}"
                : $"{{\"verification_status\":\"{_verificationStatus}\"}}";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class PayPalCaptureValidationStubHandler : HttpMessageHandler
    {
        public string? CaptureRequestId { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var json = path switch
            {
                "/v1/oauth2/token" => """{"access_token":"access-token","token_type":"Bearer"}""",
                "/v2/checkout/orders/ORDER-1/capture" => """{"id":"ORDER-1","status":"COMPLETED","purchase_units":[{"payments":{"captures":[{"id":"CAPTURE-1","status":"COMPLETED","amount":{"value":"489.00","currency_code":"RUB"}}]}}]}""",
                _ => throw new Xunit.Sdk.XunitException($"Unexpected PayPal request: {request.Method} {path}")
            };
            if (path.EndsWith("/capture", StringComparison.OrdinalIgnoreCase))
            {
                CaptureRequestId = request.Headers.GetValues("PayPal-Request-Id").Single();
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class PayPalOrderWithoutCaptureStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var json = path switch
            {
                "/v1/oauth2/token" => """{"access_token":"access-token","token_type":"Bearer"}""",
                "/v2/checkout/orders/ORDER-1" => """{"id":"ORDER-1","status":"COMPLETED","purchase_units":[{"payments":{"captures":[]}}]}""",
                _ => throw new Xunit.Sdk.XunitException($"Unexpected PayPal request: {request.Method} {path}")
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class PayPalApprovedWithoutCaptureStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var json = path switch
            {
                "/v1/oauth2/token" => """{"access_token":"access-token","token_type":"Bearer"}""",
                "/v2/checkout/orders/ORDER-1/capture" => """{"id":"ORDER-1","status":"APPROVED"}""",
                "/v2/checkout/orders/ORDER-1" => """{"id":"ORDER-1","status":"APPROVED"}""",
                _ => throw new Xunit.Sdk.XunitException($"Unexpected PayPal request: {request.Method} {path}")
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class RefundRecoveryStubHandler(PaymentProvider provider) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            Requests.Add((request.Method, path));
            var isUnexpectedPayPalRefund = provider == PaymentProvider.PayPal
                && request.Method == HttpMethod.Post
                && path.Contains("/v2/payments/captures/", StringComparison.OrdinalIgnoreCase)
                && path != "/v2/payments/captures/CAPTURE-1/refund";
            var json = provider switch
            {
                PaymentProvider.Stripe when request.Method == HttpMethod.Get => """{"id":"cs_test_1","payment_status":"paid","payment_intent":"pi_test_1"}""",
                PaymentProvider.Stripe => """{"id":"re_test_1","status":"succeeded"}""",
                PaymentProvider.PayPal when path.Contains("oauth2/token", StringComparison.OrdinalIgnoreCase) => """{"access_token":"access-token","token_type":"Bearer"}""",
                PaymentProvider.PayPal when request.Method == HttpMethod.Get => """{"id":"ORDER-1","status":"COMPLETED","purchase_units":[{"payments":{"captures":[{"id":"CAPTURE-1","status":"COMPLETED"}]}}]}""",
                PaymentProvider.PayPal => """{"id":"REFUND-1","status":"COMPLETED"}""",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            };
            return Task.FromResult(new HttpResponseMessage(isUnexpectedPayPalRefund ? System.Net.HttpStatusCode.NotFound : System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
