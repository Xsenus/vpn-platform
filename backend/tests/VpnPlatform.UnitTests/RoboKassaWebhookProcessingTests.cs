using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class RoboKassaWebhookProcessingTests
{
    [Fact]
    public async Task RoboKassa_Webhook_Should_Be_Idempotent_And_Activate_Subscription_Once()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);
        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.RoboKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);
        var payment = await db.Payments.SingleAsync();
        var account = await db.PaymentProviderAccounts.SingleAsync();
        var rawWebhook = BuildResultUrl(account, order, payment, init.Value!.PaymentId);

        var first = await orchestrator.ProcessAsync(PaymentProvider.RoboKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);
        var second = await orchestrator.ProcessAsync(PaymentProvider.RoboKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal("Webhook already processed.", second.Value);
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.PaymentWebhookEvents.CountAsync());
        Assert.Equal(PaymentStatus.Succeeded, (await db.Payments.SingleAsync()).Status);
    }

    [Fact]
    public async Task RoboKassa_Webhook_With_Invalid_Signature_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var (order, orchestrator, init, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var rawWebhook = BuildResultUrl(account, order, payment, init.Value!.PaymentId, signatureOverride: "bad-signature");

        var result = await orchestrator.ProcessAsync(PaymentProvider.RoboKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task RoboKassa_Webhook_With_Wrong_OutSum_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var (order, orchestrator, init, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var rawWebhook = BuildResultUrl(account, order, payment, init.Value!.PaymentId, outSum: "489.00");

        var result = await orchestrator.ProcessAsync(PaymentProvider.RoboKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task RoboKassa_Webhook_With_Unknown_InvId_Should_Be_Retryable()
    {
        await using var db = CreateDbContext();
        var (order, orchestrator, init, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var rawWebhook = BuildResultUrl(account, order, payment, init.Value!.PaymentId + "999", invIdOverride: init.Value.PaymentId + "999");

        var result = await orchestrator.ProcessAsync(PaymentProvider.RoboKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Failed, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task RoboKassa_Webhook_With_Wrong_ShpOrder_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var (order, orchestrator, init, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var wrongOrderId = Guid.NewGuid();
        var rawWebhook = BuildResultUrl(account, order, payment, init.Value!.PaymentId, orderIdOverride: wrongOrderId);

        var result = await orchestrator.ProcessAsync(PaymentProvider.RoboKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task RoboKassa_Webhook_For_Different_Provider_Account_Should_Not_Activate()
    {
        await using var db = CreateDbContext();
        var (order, orchestrator, init, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var rawWebhook = BuildResultUrl(account, order, payment, init.Value!.PaymentId, accountOverride: "other-merchant");

        var result = await orchestrator.ProcessAsync(PaymentProvider.RoboKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task RoboKassa_ResultUrl_Should_Not_Return_Success_For_Unknown_Status()
    {
        await using var db = CreateDbContext();
        var (order, orchestrator, init, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var rawWebhook = BuildResultUrl(account, order, payment, init.Value!.PaymentId, status: "FAIL");

        var result = await orchestrator.ProcessAsync(PaymentProvider.RoboKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    private static async Task<(Order Order, PaymentOrchestrator Orchestrator, Result<PaymentInitResult> Init, PaymentAttempt Payment, PaymentProviderAccount Account)> ArrangeInitializedPaymentAsync(ApplicationDbContext db)
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock);
        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.RoboKassa, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);
        var payment = await db.Payments.SingleAsync();
        var account = await db.PaymentProviderAccounts.SingleAsync();
        return (order, orchestrator, init, payment, account);
    }

    private static string BuildResultUrl(
        PaymentProviderAccount account,
        Order order,
        PaymentAttempt payment,
        string invId,
        string outSum = "490.00",
        string? invIdOverride = null,
        Guid? orderIdOverride = null,
        string? accountOverride = null,
        string? signatureOverride = null,
        string? status = null)
    {
        var merchant = accountOverride ?? account.ShopId;
        var orderId = orderIdOverride ?? order.Id;
        var actualInvId = invIdOverride ?? invId;
        var shp = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Shp_account"] = merchant,
            ["Shp_order"] = orderId.ToString("N"),
            ["Shp_payment"] = payment.Id.ToString("N")
        };
        if (!string.IsNullOrWhiteSpace(status))
        {
            shp["Shp_status"] = status;
        }

        var signature = signatureOverride ?? RoboKassaPaymentProvider.BuildRobokassaSignature(outSum, actualInvId, "password2", shp, "MD5");
        var raw = $"OutSum={Uri.EscapeDataString(outSum)}&InvId={Uri.EscapeDataString(actualInvId)}&SignatureValue={Uri.EscapeDataString(signature)}" +
                  $"&Shp_account={Uri.EscapeDataString(merchant)}&Shp_order={orderId:N}&Shp_payment={payment.Id:N}";
        return string.IsNullOrWhiteSpace(status) ? raw : raw + $"&Shp_status={Uri.EscapeDataString(status)}";
    }

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, FixedClock clock)
    {
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var roboKassa = new RoboKassaPaymentProvider(providerAccounts, new TestHostEnvironment(Environments.Production));
        var paymentProviderFactory = new PaymentProviderFactory(new IPaymentProvider[] { roboKassa });
        var nodeAllocation = new NodeAllocationService(db);
        var subscriptionService = new SubscriptionService(db, clock, nodeAllocation, new TestVpnProviderFactory());
        return new PaymentOrchestrator(db, paymentProviderFactory, new IPaymentWebhookVerifier[] { roboKassa }, providerAccounts, subscriptionService, clock);
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
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.RoboKassa, Mode = PaymentProviderMode.Sandbox, Name = "robokassa", PublicName = "Robokassa", IsEnabled = true, IsDefault = true, ShopId = "demo-merchant", ApiBaseUrl = "https://auth.robokassa.ru/Merchant/Index.aspx", ReturnUrl = "https://example.test/success", SecretKeyProtected = "password1", WebhookSecretProtected = "password2", ExtraSettingsJson = "{}" };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.RoboKassa, Status = OrderStatus.PendingPayment, Amount = tariff.Price, Currency = tariff.Currency, ExpiresAt = now.AddMinutes(15), IsFirstPurchase = true };

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

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
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
        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken) => CreateAccessAsync(request, cancellationToken);
        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }
}
