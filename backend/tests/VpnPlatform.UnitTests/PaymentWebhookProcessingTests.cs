using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentWebhookProcessingTests
{
    [Fact]
    public async Task YooKassa_Webhook_Should_Activate_Subscription_Once_When_Delivered_Twice()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);

        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.YooKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);

        var rawWebhook = $$"""
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"{{init.Value!.PaymentId}}",
            "status":"succeeded",
            "paid":true,
            "amount":{"value":"490.00","currency":"RUB"}
          }
        }
        """;
        var headers = new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" };

        var first = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, headers, CancellationToken.None);
        var second = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, headers, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal("Webhook already processed.", second.Value);
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.AccessCredentials.CountAsync());
        Assert.Equal(1, await db.PaymentWebhookEvents.CountAsync());

        var payment = await db.Payments.SingleAsync();
        var completedOrder = await db.Orders.SingleAsync();
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.True(payment.IsActivationProcessed);
        Assert.Equal(OrderStatus.Completed, completedOrder.Status);
    }

    [Fact]
    public async Task YooKassa_Late_Cancelled_Webhook_Should_Not_Downgrade_Succeeded_Payment()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);

        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.YooKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);

        var succeededWebhook = $$"""
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"{{init.Value!.PaymentId}}",
            "status":"succeeded",
            "paid":true,
            "amount":{"value":"490.00","currency":"RUB"}
          }
        }
        """;
        var cancelledWebhook = $$"""
        {
          "type":"notification",
          "event":"payment.canceled",
          "object":{
            "id":"{{init.Value!.PaymentId}}",
            "status":"canceled",
            "paid":false,
            "amount":{"value":"490.00","currency":"RUB"}
          }
        }
        """;
        var headers = new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" };

        var paid = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, succeededWebhook, headers, CancellationToken.None);
        var cancelled = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, cancelledWebhook, headers, CancellationToken.None);

        Assert.True(paid.IsSuccess, paid.Error);
        Assert.False(cancelled.IsSuccess);
        Assert.Contains("Succeeded -> Cancelled", cancelled.Error, StringComparison.OrdinalIgnoreCase);

        var payment = await db.Payments.SingleAsync();
        var completedOrder = await db.Orders.SingleAsync();
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(OrderStatus.Completed, completedOrder.Status);
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync(x => x.EventType == "payment.canceled")).Status);
    }

    [Fact]
    public async Task YooKassa_Webhook_Should_Reject_Invalid_Local_Sandbox_Signature()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);

        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.YooKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);

        var rawWebhook = $$"""
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"{{init.Value!.PaymentId}}",
            "status":"succeeded",
            "paid":true
          }
        }
        """;

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.PaymentWebhookEvents.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooKassa_Failed_Webhook_Should_Queue_Telegram_Payment_Failed_Once_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        db.TelegramAccounts.Add(new TelegramAccount
        {
            TelegramUserId = 700700,
            UserId = order.UserId,
            Username = "buyer",
            LinkedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();
        var orchestrator = CreateOrchestrator(db, clock);

        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.YooKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);

        var rawWebhook = $$"""
        {
          "type":"notification",
          "event":"payment.canceled",
          "object":{
            "id":"{{init.Value!.PaymentId}}",
            "status":"canceled",
            "paid":false,
            "amount":{"value":"490.00","currency":"RUB"}
          }
        }
        """;
        var headers = new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" };

        var first = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, headers, CancellationToken.None);
        var second = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, headers, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal("Webhook already processed.", second.Value);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.PaymentWebhookEvents.CountAsync());
        Assert.Equal(PaymentStatus.Cancelled, (await db.Payments.SingleAsync()).Status);
        Assert.Equal(OrderStatus.Failed, (await db.Orders.SingleAsync()).Status);
        var notification = await db.TelegramBotNotifications.SingleAsync(x => x.Type == "payment_failed");
        Assert.Equal(700700, notification.TelegramUserId);
        Assert.Equal("pending", notification.Status);
        Assert.Contains("Платеж отменен", notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Купить VPN", notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }



    [Fact]
    public async Task YooKassa_Webhook_Should_Reject_Wrong_Amount()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);
        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.YooKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);

        var rawWebhook = $$"""
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"{{init.Value!.PaymentId}}",
            "status":"succeeded",
            "paid":true,
            "amount":{"value":"489.00","currency":"RUB"}
          }
        }
        """;

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooKassa_Webhook_Should_Reject_Wrong_Currency()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);
        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.YooKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);

        var rawWebhook = $$"""
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"{{init.Value!.PaymentId}}",
            "status":"succeeded",
            "paid":true,
            "amount":{"value":"490.00","currency":"USD"}
          }
        }
        """;

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooKassa_Webhook_Should_Save_Unknown_Payment_As_Retryable_Failure()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);

        const string rawWebhook = """
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"yk_sandbox_unknown",
            "status":"succeeded",
            "paid":true,
            "amount":{"value":"490.00","currency":"RUB"}
          }
        }
        """;

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Failed, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooKassa_Refund_Should_Be_Idempotent_For_Same_Amount_And_Reason()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);
        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.YooKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);
        var rawWebhook = $$"""
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"{{init.Value!.PaymentId}}",
            "status":"succeeded",
            "paid":true,
            "amount":{"value":"490.00","currency":"RUB"}
          }
        }
        """;
        var processed = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" }, CancellationToken.None);
        Assert.True(processed.IsSuccess, processed.Error);
        var payment = await db.Payments.SingleAsync();

        var first = await orchestrator.RefundPaymentAsync(payment.Id, 490m, "duplicate-test", CancellationToken.None);
        var second = await orchestrator.RefundPaymentAsync(payment.Id, 490m, "duplicate-test", CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal(first.Value!.ProviderRefundId, second.Value!.ProviderRefundId);
        Assert.Equal(1, await db.Refunds.CountAsync());
    }

    [Fact]
    public async Task PayPal_Approved_Order_Should_Retry_Unknown_Capture_And_Activate_Once_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 12, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        order.PaymentProvider = PaymentProvider.PayPal;
        var account = await db.PaymentProviderAccounts.SingleAsync();
        account.Provider = PaymentProvider.PayPal;
        account.Mode = PaymentProviderMode.Production;
        account.ShopId = "paypal-client-id";
        account.SecretKeyProtected = "paypal-client-secret";
        account.WebhookSecretProtected = "paypal-webhook-id";
        account.ApiBaseUrl = "https://api-m.paypal.test";
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentProviderAccountId = account.Id,
            Provider = PaymentProvider.PayPal,
            ProviderMode = PaymentProviderMode.Production,
            ProviderPaymentId = "ORDER-1",
            Amount = order.Amount,
            Currency = order.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = $"paypal-{Guid.NewGuid():N}"
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var handler = new PayPalCaptureStubHandler(payment.Id, order.Id);
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var provider = new PayPalPaymentProvider(
            new StaticHttpClientFactory(new HttpClient(handler)),
            accounts,
            new TestHostEnvironment { EnvironmentName = Environments.Production });
        var orchestrator = new PaymentOrchestrator(
            db,
            new PaymentProviderFactory(new IPaymentProvider[] { provider }),
            new IPaymentWebhookVerifier[] { provider },
            accounts,
            new SubscriptionService(db, clock, new NodeAllocationService(db), new TestVpnProviderFactory()),
            clock,
            new TestRuntimeEnvironment(Environments.Production));
        var rawWebhook = $$"""
        {
          "id":"WH-APPROVED-1",
          "event_type":"CHECKOUT.ORDER.APPROVED",
          "resource":{"id":"ORDER-1","status":"APPROVED"}
        }
        """;

        var unknownCapture = await orchestrator.ProcessAsync(PaymentProvider.PayPal, rawWebhook, PayPalHeaders(), CancellationToken.None);
        Assert.False(unknownCapture.IsSuccess);
        Assert.True(unknownCapture.IsRetryable);
        Assert.Equal(PaymentWebhookEventStatus.Failed, (await db.PaymentWebhookEvents.SingleAsync()).Status);
        Assert.Equal(PaymentStatus.Pending, (await db.Payments.SingleAsync()).Status);

        var retry = await orchestrator.ProcessAsync(PaymentProvider.PayPal, rawWebhook, PayPalHeaders(), CancellationToken.None);
        var duplicate = await orchestrator.ProcessAsync(PaymentProvider.PayPal, rawWebhook, PayPalHeaders(), CancellationToken.None);

        Assert.True(retry.IsSuccess, retry.Error);
        Assert.True(duplicate.IsSuccess, duplicate.Error);
        Assert.Equal("Webhook already processed.", duplicate.Value);
        Assert.Equal(PaymentStatus.Succeeded, (await db.Payments.SingleAsync()).Status);
        Assert.Equal(OrderStatus.Completed, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        var captures = handler.Requests.Where(x => x.Method == HttpMethod.Post && x.Path == "/v2/checkout/orders/ORDER-1/capture").ToList();
        Assert.Equal(2, captures.Count);
        Assert.All(captures, capture => Assert.Equal($"capture-{payment.Id:N}", capture.PayPalRequestId));
    }

    [Theory]
    [InlineData(PaymentProvider.YooKassa, "foreign-id")]
    [InlineData(PaymentProvider.Stripe, "foreign-id")]
    [InlineData(PaymentProvider.TBankAcquiring, "foreign-id")]
    [InlineData(PaymentProvider.YooKassa, "wrong-amount")]
    [InlineData(PaymentProvider.Stripe, "wrong-amount")]
    [InlineData(PaymentProvider.TBankAcquiring, "wrong-amount")]
    [InlineData(PaymentProvider.YooKassa, "missing-proof")]
    [InlineData(PaymentProvider.Stripe, "missing-proof")]
    [InlineData(PaymentProvider.TBankAcquiring, "missing-proof")]
    public async Task Manual_Recheck_Should_Reject_Mismatched_Succeeded_Payment_Response_On_Sqlite(PaymentProvider providerType, string mismatch)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 12, 9, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        order.PaymentProvider = providerType;
        var account = await db.PaymentProviderAccounts.SingleAsync();
        account.Provider = providerType;
        account.Mode = PaymentProviderMode.Production;
        account.Name = $"production-{providerType}";
        account.ShopId = providerType == PaymentProvider.Stripe ? "stripe-account" : "merchant-account";
        account.SecretKeyProtected = "provider-secret";
        account.ApiBaseUrl = "https://provider.test";
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentProviderAccountId = account.Id,
            Provider = providerType,
            ProviderMode = PaymentProviderMode.Production,
            ProviderPaymentId = "LOCAL-PAYMENT-1",
            Amount = order.Amount,
            Currency = order.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = $"recheck-{Guid.NewGuid():N}"
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var httpFactory = new StaticHttpClientFactory(new HttpClient(new MismatchedPaymentStatusStubHandler(providerType, payment.Id, order.Id, mismatch)));
        var hostEnvironment = new TestHostEnvironment { EnvironmentName = Environments.Production };
        IPaymentProvider provider = providerType switch
        {
            PaymentProvider.YooKassa => new YooKassaPaymentProvider(httpFactory, accounts, NullLogger<YooKassaPaymentProvider>.Instance, hostEnvironment),
            PaymentProvider.Stripe => new StripePaymentProvider(httpFactory, accounts, hostEnvironment),
            PaymentProvider.TBankAcquiring => new TBankAcquiringPaymentProvider(httpFactory, accounts, hostEnvironment),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        var subscriptionService = new SubscriptionService(db, clock, new NodeAllocationService(db), new TestVpnProviderFactory());
        var orchestrator = new PaymentOrchestrator(
            db,
            new PaymentProviderFactory(new[] { provider }),
            provider is IPaymentWebhookVerifier verifier ? new[] { verifier } : Array.Empty<IPaymentWebhookVerifier>(),
            accounts,
            subscriptionService,
            clock,
            new TestRuntimeEnvironment(Environments.Production));

        var result = await orchestrator.RecheckPaymentAsync(payment.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        var expectedError = mismatch switch
        {
            "foreign-id" => "identifier",
            "wrong-amount" => "amount",
            _ => "proof"
        };
        Assert.Contains(expectedError, result.Error, StringComparison.OrdinalIgnoreCase);
        db.ChangeTracker.Clear();
        Assert.Equal(PaymentStatus.Pending, (await db.Payments.SingleAsync()).Status);
        Assert.Equal(OrderStatus.PendingPayment, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(0, await db.AccessCredentials.CountAsync());
    }

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, FixedClock clock)
    {
        var orderService = new OrderService(db, clock);
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var yooKassa = new YooKassaPaymentProvider(null!, providerAccounts, NullLogger<YooKassaPaymentProvider>.Instance, new TestHostEnvironment());
        var paymentProviderFactory = new PaymentProviderFactory(new IPaymentProvider[] { yooKassa });
        var nodeAllocation = new NodeAllocationService(db);
        var subscriptionService = new SubscriptionService(db, clock, nodeAllocation, new TestVpnProviderFactory());
        return new PaymentOrchestrator(
            db,
            paymentProviderFactory,
            new IPaymentWebhookVerifier[] { yooKassa },
            providerAccounts,
            subscriptionService,
            clock,
            new TestRuntimeEnvironment(Environments.Development));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<Order> SeedOrderGraphAsync(ApplicationDbContext db, DateTimeOffset now)
    {
        var user = new User { Id = Guid.NewGuid(), Email = "buyer@example.test", DisplayName = "Buyer", PasswordHash = "hash", ReferralCode = "buyer" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = "monthly", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "node-1", Host = "127.0.0.1", IpAddress = "127.0.0.1", Region = "test", Country = "RU", Datacenter = "test", Capacity = 100, Status = NodeStatus.Ready, HealthStatus = HealthStatus.Healthy, IsAvailableForNewUsers = true, Provider = "x3ui" };
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.YooKassa, Mode = PaymentProviderMode.Sandbox, Name = "local-yookassa", PublicName = "YooKassa", IsEnabled = true, IsDefault = true, ShopId = "local-sandbox-shop", ApiBaseUrl = "https://api.yookassa.ru/v3", ReturnUrl = "https://example.test/success", SecretKeyProtected = string.Empty, UseWebhookIpAllowList = false };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PendingPayment, Amount = tariff.Price, Currency = tariff.Currency, ExpiresAt = now.AddMinutes(15), IsFirstPurchase = true };

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(0, value.Length - visibleTail)) + value[^Math.Min(visibleTail, value.Length)..];
    }

    private static Dictionary<string, string> PayPalHeaders()
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["PAYPAL-AUTH-ALGO"] = "SHA256withRSA",
            ["PAYPAL-CERT-URL"] = "https://api-m.paypal.test/cert.pem",
            ["PAYPAL-TRANSMISSION-ID"] = "transmission-id",
            ["PAYPAL-TRANSMISSION-SIG"] = "signature",
            ["PAYPAL-TRANSMISSION-TIME"] = "2026-08-12T08:00:00Z"
        };

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class PayPalCaptureStubHandler(Guid paymentId, Guid orderId) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string? PayPalRequestId)> Requests { get; } = [];
        public int CaptureFailuresRemaining { get; set; } = 1;
        public string CaptureAmount { get; set; } = "490.00";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var requestId = request.Headers.TryGetValues("PayPal-Request-Id", out var values) ? values.Single() : null;
            Requests.Add((request.Method, path, requestId));
            if (path == "/v2/checkout/orders/ORDER-1/capture" && CaptureFailuresRemaining > 0)
            {
                CaptureFailuresRemaining--;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("{\"name\":\"INTERNAL_SERVER_ERROR\"}", System.Text.Encoding.UTF8, "application/json")
                });
            }

            var json = path switch
            {
                "/v1/oauth2/token" => """{"access_token":"access-token","token_type":"Bearer"}""",
                "/v1/notifications/verify-webhook-signature" => """{"verification_status":"SUCCESS"}""",
                "/v2/checkout/orders/ORDER-1" => """{"id":"ORDER-1","status":"APPROVED"}""",
                "/v2/checkout/orders/ORDER-1/capture" => $$"""
                    {
                      "id":"ORDER-1",
                      "status":"COMPLETED",
                      "purchase_units":[{
                        "reference_id":"{{paymentId:N}}",
                        "custom_id":"{{orderId:N}}",
                        "payments":{"captures":[{
                          "id":"CAPTURE-1",
                          "status":"COMPLETED",
                          "amount":{"value":"{{CaptureAmount}}","currency_code":"RUB"}
                        }]}
                      }]
                    }
                    """,
                _ => throw new Xunit.Sdk.XunitException($"Unexpected PayPal request: {request.Method} {path}")
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class MismatchedPaymentStatusStubHandler(PaymentProvider provider, Guid paymentId, Guid orderId, string mismatch) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var responsePaymentId = mismatch == "foreign-id" ? "FOREIGN-PAYMENT-1" : "LOCAL-PAYMENT-1";
            var amount = mismatch == "wrong-amount" ? "489.00" : "490.00";
            var amountMinor = mismatch == "wrong-amount" ? 48900 : 49000;
            var metadata = mismatch == "missing-proof"
                ? string.Empty
                : $$"""
                  ,"metadata":{"orderId":"{{orderId}}","paymentAttemptId":"{{paymentId}}"}
                  """;
            var tbankProof = mismatch == "missing-proof"
                ? string.Empty
                : $$"""
                  ,"OrderId":"{{paymentId:N}}","Amount":{{amountMinor}},"TerminalKey":"merchant-account"
                  """;
            var json = provider switch
            {
                PaymentProvider.YooKassa => $$"""
                    {
                      "id":"{{responsePaymentId}}",
                      "status":"succeeded",
                      "paid":true,
                      "amount":{"value":"{{amount}}","currency":"RUB"}{{metadata}}
                    }
                    """,
                PaymentProvider.Stripe => $$"""
                    {
                      "id":"{{responsePaymentId}}",
                      "payment_status":"paid",
                      "amount_total":{{amountMinor}},
                      "currency":"rub"{{metadata}}
                    }
                    """,
                PaymentProvider.TBankAcquiring => $$"""
                    {
                      "Success":true,
                      "Status":"CONFIRMED",
                      "PaymentId":"{{responsePaymentId}}"{{tbankProof}}
                    }
                    """,
                _ => throw new Xunit.Sdk.XunitException($"Unexpected provider status request: {provider}")
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider = new TestVpnProvider();
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TestVpnProvider : IVpnProvider
    {
        public string Name => "x3ui";
        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult($"client-{request.SubscriptionId:N}", "vless://test@example.test:443", "/qr/test.png", "/config/test.txt"));

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => CreateAccessAsync(request, cancellationToken);

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken)
            => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));

        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken)
            => Task.FromResult(HealthStatus.Healthy);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestRuntimeEnvironment(string environmentName) : IRuntimeEnvironment
    {
        public string EnvironmentName { get; } = environmentName;
    }
}
