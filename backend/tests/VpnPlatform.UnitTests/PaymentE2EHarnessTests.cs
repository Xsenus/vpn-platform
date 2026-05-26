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

public class PaymentE2EHarnessTests
{
    [Fact]
    public async Task YooKassa_LocalSandbox_E2E_Should_Activate_Subscription_Once()
    {
        await using var db = CreateDbContext();
        var clock = new MutableClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var user = await SeedUserCatalogNodeAndProviderAsync(db, clock.UtcNow);
        var tariff = await db.Tariffs.SingleAsync();

        var orderService = new OrderService(db, clock);
        var checkoutService = new CheckoutSessionService(db, clock, orderService);
        var orchestrator = CreateOrchestrator(db, clock);

        var checkout = await checkoutService.CreateAsync(
            new CreateCheckoutSessionCommand(tariff.Id, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, true, user.Email, "https://cabinet.example.test/payments"),
            CancellationToken.None);
        Assert.True(checkout.IsSuccess, checkout.Error);
        Assert.NotEqual(checkout.Value!.Token, (await db.CheckoutSessions.SingleAsync()).TokenHash);
        Assert.Equal(64, (await db.CheckoutSessions.SingleAsync()).TokenHash.Length);

        var claim = await checkoutService.ClaimAsync(new ClaimCheckoutSessionCommand(checkout.Value.Token, user.Id), CancellationToken.None);
        Assert.True(claim.IsSuccess, claim.Error);
        Assert.Equal(user.Id, claim.Value!.UserId);

        var init = await orchestrator.InitPaymentAsync(new PaymentInitCommand(claim.Value.Id, PaymentProvider.YooKassa, "https://cabinet.example.test/payments"), CancellationToken.None);
        Assert.True(init.IsSuccess, init.Error);
        Assert.StartsWith("yk_sandbox_", init.Value!.PaymentId, StringComparison.Ordinal);

        var webhook = $$"""
        {
          "type":"notification",
          "event":"payment.succeeded",
          "object":{
            "id":"{{init.Value.PaymentId}}",
            "status":"succeeded",
            "paid":true,
            "amount":{"value":"490.00","currency":"RUB"}
          }
        }
        """;
        var headers = new Dictionary<string, string> { ["X-YooKassa-Sandbox-Webhook"] = "true" };

        var firstWebhook = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, webhook, headers, CancellationToken.None);
        Assert.True(firstWebhook.IsSuccess, firstWebhook.Error);

        var paymentAfterFirstWebhook = await db.Payments.SingleAsync();
        var orderAfterFirstWebhook = await db.Orders.SingleAsync();
        var subscriptionAfterFirstWebhook = await db.Subscriptions.SingleAsync();
        var firstEndAt = subscriptionAfterFirstWebhook.EndAt;
        Assert.Equal(PaymentStatus.Succeeded, paymentAfterFirstWebhook.Status);
        Assert.True(paymentAfterFirstWebhook.IsActivationProcessed);
        Assert.Equal(OrderStatus.Completed, orderAfterFirstWebhook.Status);
        Assert.Equal(SubscriptionStatus.Active, subscriptionAfterFirstWebhook.Status);

        var duplicateWebhook = await orchestrator.ProcessAsync(PaymentProvider.YooKassa, webhook, headers, CancellationToken.None);
        Assert.True(duplicateWebhook.IsSuccess, duplicateWebhook.Error);
        Assert.Equal("Webhook already processed.", duplicateWebhook.Value);

        var subscriptionAfterDuplicate = await db.Subscriptions.SingleAsync();
        Assert.Equal(firstEndAt, subscriptionAfterDuplicate.EndAt);
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.AccessCredentials.CountAsync());
        Assert.Equal(1, await db.PaymentWebhookEvents.CountAsync());
        Assert.Equal(1, await db.TelegramBotNotifications.CountAsync(x => x.Type == "payment_succeeded"));
        var notification = await db.TelegramBotNotifications.SingleAsync(x => x.Type == "payment_succeeded");
        Assert.Contains("vless://test@example.test:443", notification.PayloadJson);
        Assert.Contains("Мои ключи", notification.PayloadJson);
        Assert.Contains("Продлить", notification.PayloadJson);
    }

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, MutableClock clock)
    {
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var yooKassa = new YooKassaPaymentProvider(null!, providerAccounts, NullLogger<YooKassaPaymentProvider>.Instance, new TestHostEnvironment(Environments.Development));
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

    private static async Task<User> SeedUserCatalogNodeAndProviderAsync(ApplicationDbContext db, DateTimeOffset now)
    {
        var user = new User { Id = Guid.NewGuid(), Email = "buyer@example.test", DisplayName = "Buyer", PasswordHash = "hash", ReferralCode = "buyer" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = "monthly", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "node-1", Host = "127.0.0.1", IpAddress = "127.0.0.1", Region = "test", Country = "RU", Datacenter = "test", Capacity = 100, Status = NodeStatus.Ready, HealthStatus = HealthStatus.Healthy, IsAvailableForNewUsers = true, Provider = "x3ui" };
        var account = new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            Name = "yookassa-local-sandbox",
            PublicName = "YooKassa",
            IsEnabled = true,
            IsDefault = true,
            ShopId = string.Empty,
            ApiBaseUrl = "https://api.yookassa.ru/v3",
            ReturnUrl = "https://cabinet.example.test/payments",
            SecretKeyProtected = string.Empty,
            WebhookSecretProtected = string.Empty,
            ExtraSettingsJson = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(user);
        db.TelegramAccounts.Add(new TelegramAccount
        {
            TelegramUserId = 777001,
            UserId = user.Id,
            Username = "buyer",
            LinkedAt = now,
            LastSeenAt = now
        });
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.PaymentProviderAccounts.Add(account);
        await db.SaveChangesAsync();
        return user;
    }

    private sealed class MutableClock : IClock
    {
        public MutableClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; set; }
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
