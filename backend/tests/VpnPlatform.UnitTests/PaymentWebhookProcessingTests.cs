using Microsoft.EntityFrameworkCore;
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
    public async Task YooKassa_Webhook_Should_Save_Unknown_Payment_As_Rejected()
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
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
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

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, FixedClock clock)
    {
        var orderService = new OrderService(db, clock);
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var yooKassa = new YooKassaPaymentProvider(null!, providerAccounts, NullLogger<YooKassaPaymentProvider>.Instance, new TestHostEnvironment());
        var paymentProviderFactory = new PaymentProviderFactory(new IPaymentProvider[] { yooKassa });
        var nodeAllocation = new NodeAllocationService(db);
        var subscriptionService = new SubscriptionService(db, clock, nodeAllocation, new TestVpnProviderFactory());
        return new PaymentOrchestrator(db, paymentProviderFactory, new IPaymentWebhookVerifier[] { yooKassa }, providerAccounts, subscriptionService, clock);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<Order> SeedOrderGraphAsync(ApplicationDbContext db, DateTimeOffset now)
    {
        var user = new User { Id = Guid.NewGuid(), Email = "buyer@example.test", DisplayName = "Buyer", PasswordHash = "hash", ReferralCode = "buyer" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = "monthly", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "node-1", Host = "127.0.0.1", IpAddress = "127.0.0.1", Region = "test", Country = "RU", Datacenter = "test", Capacity = 100, Status = NodeStatus.Ready, HealthStatus = HealthStatus.Healthy, IsAvailableForNewUsers = true, Provider = "x3ui" };
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.YooKassa, Mode = PaymentProviderMode.Sandbox, Name = "local-yookassa", PublicName = "YooKassa", IsEnabled = true, IsDefault = true, ApiBaseUrl = "https://api.yookassa.ru/v3", ReturnUrl = "https://example.test/success", SecretKeyProtected = string.Empty, UseWebhookIpAllowList = false };
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
}
