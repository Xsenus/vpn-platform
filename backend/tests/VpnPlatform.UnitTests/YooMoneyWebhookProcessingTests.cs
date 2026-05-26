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

public class YooMoneyWebhookProcessingTests
{
    [Fact]
    public async Task YooMoney_Webhook_Should_Activate_Subscription_Once_And_Duplicate_Operation_Should_Be_Ignored()
    {
        await using var db = CreateDbContext();
        var (order, orchestrator, init, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var raw = BuildNotification(account, payment, operationId: "op-1");

        var first = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);
        var second = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal("Webhook already processed.", second.Value);
        Assert.Equal(payment.Id, (await db.Subscriptions.SingleAsync()).LastPaymentId);
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.PaymentWebhookEvents.CountAsync());
        Assert.Equal(PaymentStatus.Succeeded, (await db.Payments.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooMoney_Webhook_With_Invalid_Sign_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var (_, orchestrator, _, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var raw = BuildNotification(account, payment, signOverride: "bad-sign");

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooMoney_Webhook_With_Missing_Sign_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var (_, orchestrator, _, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var raw = BuildNotification(account, payment, includeSign: false);

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooMoney_Webhook_With_Wrong_Amount_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var (_, orchestrator, _, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var raw = BuildNotification(account, payment, amount: "489.00");

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooMoney_Webhook_With_Wrong_Label_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var (_, orchestrator, _, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var raw = BuildNotification(account, payment, labelOverride: "ym_unknown_payment");

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Theory]
    [InlineData("true", "false")]
    [InlineData("false", "true")]
    public async Task YooMoney_Webhook_With_Codepro_Or_Unaccepted_Should_Not_Activate(string codepro, string unaccepted)
    {
        await using var db = CreateDbContext();
        var (_, orchestrator, _, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var raw = BuildNotification(account, payment, codepro: codepro, unaccepted: unaccepted);

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Processed, (await db.PaymentWebhookEvents.SingleAsync()).Status);
        Assert.Equal(PaymentStatus.Pending, (await db.Payments.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooMoney_Webhook_With_NonRub_Currency_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var (_, orchestrator, _, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var raw = BuildNotification(account, payment, currency: "840");

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    [Fact]
    public async Task YooMoney_Webhook_For_Different_Receiver_Should_Not_Activate()
    {
        await using var db = CreateDbContext();
        var (_, orchestrator, _, payment, account) = await ArrangeInitializedPaymentAsync(db);
        var raw = BuildNotification(account, payment, receiverOverride: "410099999999999");

        var result = await orchestrator.ProcessAsync(PaymentProvider.YooMoney, raw, new Dictionary<string, string>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Rejected, (await db.PaymentWebhookEvents.SingleAsync()).Status);
    }

    private static async Task<(Order Order, PaymentOrchestrator Orchestrator, Result<PaymentInitResult> Init, PaymentAttempt Payment, PaymentProviderAccount Account)> ArrangeInitializedPaymentAsync(ApplicationDbContext db)
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var order = await SeedOrderGraphAsync(db, clock.UtcNow);
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var provider = new YooMoneyPaymentProvider(providerAccounts, new TestHostEnvironment(Environments.Production));
        var nodeAllocation = new NodeAllocationService(db);
        var subscriptionService = new SubscriptionService(db, clock, nodeAllocation, new TestVpnProviderFactory());
        var orchestrator = new PaymentOrchestrator(db, new PaymentProviderFactory(new IPaymentProvider[] { provider }), new IPaymentWebhookVerifier[] { provider }, providerAccounts, subscriptionService, clock);
        var init = await orchestrator.InitPaymentAsync(new(order.Id, PaymentProvider.YooMoney, "https://example.test/success"));
        Assert.True(init.IsSuccess, init.Error);
        var payment = await db.Payments.SingleAsync();
        var account = await db.PaymentProviderAccounts.SingleAsync();
        return (order, orchestrator, init, payment, account);
    }

    private static string BuildNotification(
        PaymentProviderAccount account,
        PaymentAttempt payment,
        string operationId = "op-1",
        string amount = "490.00",
        string currency = "643",
        string codepro = "false",
        string unaccepted = "false",
        string? labelOverride = null,
        string? receiverOverride = null,
        string? signOverride = null,
        bool includeSign = true)
    {
        var form = new Dictionary<string, string>
        {
            ["notification_type"] = "p2p-incoming",
            ["operation_id"] = operationId,
            ["amount"] = amount,
            ["currency"] = currency,
            ["datetime"] = "2026-04-29T10:00:00Z",
            ["sender"] = "410000000000001",
            ["receiver"] = receiverOverride ?? account.ShopId,
            ["codepro"] = codepro,
            ["label"] = labelOverride ?? payment.ProviderPaymentId,
            ["unaccepted"] = unaccepted
        };

        if (includeSign)
        {
            form["sign"] = signOverride ?? YooMoneyPaymentProvider.BuildYooMoneySign(form, "notification-secret");
        }

        return string.Join("&", form.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
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
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.YooMoney, Mode = PaymentProviderMode.Production, Name = "yoomoney", PublicName = "YooMoney", IsEnabled = true, IsDefault = true, ShopId = "410000000000000", ApiBaseUrl = "https://yoomoney.ru/quickpay/confirm", ReturnUrl = "https://example.test/success", SecretKeyProtected = string.Empty, WebhookSecretProtected = "notification-secret", ExtraSettingsJson = "{}" };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooMoney, Status = OrderStatus.PendingPayment, Amount = tariff.Price, Currency = tariff.Currency, ExpiresAt = now.AddMinutes(15), IsFirstPurchase = true };

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
