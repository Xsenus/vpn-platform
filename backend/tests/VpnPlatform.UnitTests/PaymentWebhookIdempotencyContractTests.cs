using Microsoft.EntityFrameworkCore;
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

public class PaymentWebhookIdempotencyContractTests
{
    public static IEnumerable<object[]> Providers()
        => Enum.GetValues<PaymentProvider>().Select(provider => new object[] { provider });

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Duplicate_Webhook_Should_Be_Idempotent_For_Each_Payment_Provider(PaymentProvider provider)
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 11, 9, 0, 0, TimeSpan.Zero));
        var graph = await SeedPaymentGraphAsync(db, provider, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock, provider, graph.Payment.ProviderPaymentId, "evt-1");

        var first = await orchestrator.ProcessAsync(provider, "event=paid&id=evt-1", new Dictionary<string, string>(), CancellationToken.None);
        var second = await orchestrator.ProcessAsync(provider, "event=paid&id=evt-1", new Dictionary<string, string>(), CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal("Webhook already processed.", second.Value);
        Assert.Equal(1, await db.PaymentWebhookEvents.CountAsync());
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.AccessCredentials.CountAsync());
        Assert.Equal(PaymentStatus.Succeeded, (await db.Payments.SingleAsync()).Status);
        Assert.Equal(OrderStatus.Completed, (await db.Orders.SingleAsync()).Status);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Duplicate_Webhook_Without_External_Event_Id_Should_Use_Payload_Hash_For_Each_Payment_Provider(PaymentProvider provider)
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 11, 9, 10, 0, TimeSpan.Zero));
        var graph = await SeedPaymentGraphAsync(db, provider, clock.UtcNow);
        var orchestrator = CreateOrchestrator(db, clock, provider, graph.Payment.ProviderPaymentId, string.Empty);
        const string rawWebhook = "event=paid&without_external_event_id=true";

        var first = await orchestrator.ProcessAsync(provider, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);
        var second = await orchestrator.ProcessAsync(provider, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal("Webhook already processed.", second.Value);
        var webhookEvent = await db.PaymentWebhookEvents.SingleAsync();
        Assert.StartsWith("payload:", webhookEvent.ExternalEventId, StringComparison.Ordinal);
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.AccessCredentials.CountAsync());
    }

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, IClock clock, PaymentProvider provider, string providerPaymentId, string externalEventId)
    {
        var fakeProvider = new IdempotentPaymentProvider(provider, providerPaymentId, externalEventId);
        var paymentProviderFactory = new PaymentProviderFactory(new IPaymentProvider[] { fakeProvider });
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var subscriptionService = new SubscriptionService(db, clock, new NodeAllocationService(db), new TestVpnProviderFactory());
        return new PaymentOrchestrator(db, paymentProviderFactory, new IPaymentWebhookVerifier[] { fakeProvider }, providerAccounts, subscriptionService, clock);
    }

    private static async Task<(Order Order, PaymentAttempt Payment)> SeedPaymentGraphAsync(ApplicationDbContext db, PaymentProvider provider, DateTimeOffset now)
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"buyer-{provider}@example.test".ToLowerInvariant(), DisplayName = "Buyer", PasswordHash = "hash", ReferralCode = $"buyer-{Guid.NewGuid():N}" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = $"monthly-{Guid.NewGuid():N}", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "node-1", Host = "127.0.0.1", IpAddress = "127.0.0.1", Region = "test", Country = "RU", Datacenter = "test", Capacity = 100, Status = NodeStatus.Ready, HealthStatus = HealthStatus.Healthy, IsAvailableForNewUsers = true, Provider = "x3ui" };
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = provider, Mode = PaymentProviderMode.Sandbox, Name = $"sandbox-{provider}", PublicName = provider.ToString(), IsEnabled = true, IsDefault = true, ShopId = $"shop-{provider}", SecretKeyProtected = string.Empty, WebhookSecretProtected = string.Empty, UseWebhookIpAllowList = false };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = provider, Status = OrderStatus.PendingPayment, Amount = tariff.Price, Currency = tariff.Currency, ExpiresAt = now.AddMinutes(15), IsFirstPurchase = true };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, PaymentProviderAccountId = account.Id, Provider = provider, ProviderMode = PaymentProviderMode.Sandbox, ProviderPaymentId = $"provider-payment-{Guid.NewGuid():N}", Amount = order.Amount, Currency = order.Currency, Status = PaymentStatus.Pending, IdempotencyKey = $"payment-{Guid.NewGuid():N}" };

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return (order, payment);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class IdempotentPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier
    {
        private readonly string _providerPaymentId;
        private readonly string _externalEventId;

        public IdempotentPaymentProvider(PaymentProvider provider, string providerPaymentId, string externalEventId)
        {
            Provider = provider;
            _providerPaymentId = providerPaymentId;
            _externalEventId = externalEventId;
        }

        public PaymentProvider Provider { get; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentInitResult(_providerPaymentId, "https://pay.example.test", "{}"));

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentWebhookParseResult(_externalEventId, "payment.succeeded", _providerPaymentId, PaymentStatus.Succeeded, rawBody, false, 490m, "RUB", true));

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentStatusResult(_providerPaymentId, PaymentStatus.Succeeded, "{}", "test"));

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentRefundResult($"refund-{payment.Id:N}", RefundStatus.Succeeded, "{}"));

        public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentWebhookVerificationResult(true, "test-verifier", null));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
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
}
