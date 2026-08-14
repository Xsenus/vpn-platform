using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentWebhookRecoveryTests
{
    [Fact]
    public async Task Unknown_Payment_Webhook_Should_Process_When_Payment_Appears_On_Retry()
    {
        await using var fixture = await PaymentFixture.CreateAsync(includePayment: false);

        var first = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        Assert.Equal(PaymentWebhookEventStatus.Failed, (await fixture.Db.PaymentWebhookEvents.SingleAsync()).Status);

        fixture.AddPayment();
        await fixture.Db.SaveChangesAsync();
        var retry = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);

        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(PaymentWebhookEventStatus.Processed, (await fixture.Db.PaymentWebhookEvents.SingleAsync()).Status);
        Assert.Equal(1, await fixture.Db.Subscriptions.CountAsync());
        Assert.Equal(1, await fixture.Db.AccessCredentials.CountAsync());
    }

    [Fact]
    public async Task Stale_Received_Webhook_Should_Be_Reclaimed_And_Processed()
    {
        await using var fixture = await PaymentFixture.CreateAsync(includePayment: true);
        fixture.Db.PaymentWebhookEvents.Add(fixture.CreateWebhookEvent(
            PaymentWebhookEventStatus.Received,
            fixture.Clock.UtcNow.AddMinutes(-30)));
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(PaymentWebhookEventStatus.Processed, (await fixture.Db.PaymentWebhookEvents.SingleAsync()).Status);
        Assert.Equal(1, await fixture.Db.Subscriptions.CountAsync());
        Assert.Equal(1, fixture.VpnProvider.CreateCalls);
    }

    [Fact]
    public async Task Fresh_Received_Webhook_Should_Wait_For_Lease_Expiry_Before_Recovery()
    {
        await using var fixture = await PaymentFixture.CreateAsync(includePayment: true);
        fixture.Db.PaymentWebhookEvents.Add(fixture.CreateWebhookEvent(
            PaymentWebhookEventStatus.Received,
            fixture.Clock.UtcNow));
        await fixture.Db.SaveChangesAsync();

        var whileClaimed = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);

        Assert.False(whileClaimed.IsSuccess);
        Assert.True(whileClaimed.IsRetryable);
        Assert.Contains("in progress", whileClaimed.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await fixture.Db.Subscriptions.CountAsync());

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(11);
        var recovered = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);

        Assert.True(recovered.IsSuccess, recovered.Error);
        Assert.Equal(PaymentWebhookEventStatus.Processed, (await fixture.Db.PaymentWebhookEvents.SingleAsync()).Status);
        Assert.Equal(1, fixture.VpnProvider.CreateCalls);
    }

    [Fact]
    public async Task Verifier_Exception_Should_Be_Retryable_Without_Duplicating_Event()
    {
        await using var fixture = await PaymentFixture.CreateAsync(includePayment: true);
        fixture.Provider.VerificationFailuresRemaining = 1;

        var first = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);
        var failedEvent = await fixture.Db.PaymentWebhookEvents.AsNoTracking().SingleAsync();
        Assert.DoesNotContain("verifier-private-token", first.Error, StringComparison.Ordinal);
        Assert.Equal("Webhook verification failed.", first.Error);
        Assert.DoesNotContain("verifier-private-token", failedEvent.ErrorText, StringComparison.Ordinal);
        Assert.Contains("REDACTED", failedEvent.ErrorText, StringComparison.OrdinalIgnoreCase);
        var retry = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(1, await fixture.Db.PaymentWebhookEvents.CountAsync());
        Assert.Equal(PaymentWebhookEventStatus.Processed, (await fixture.Db.PaymentWebhookEvents.SingleAsync()).Status);
        Assert.Equal(1, fixture.VpnProvider.CreateCalls);
    }

    [Fact]
    public async Task Provisioning_Failure_Retry_Should_Resume_The_Same_Subscription()
    {
        await using var fixture = await PaymentFixture.CreateAsync(includePayment: true);
        fixture.VpnProvider.CreateFailuresRemaining = 1;

        var first = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);
        var retry = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(1, await fixture.Db.Subscriptions.CountAsync());
        Assert.Equal(1, await fixture.Db.AccessCredentials.CountAsync());
        Assert.Equal(2, fixture.VpnProvider.CreateCalls);
        Assert.Equal(OrderStatus.Completed, (await fixture.Db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task Completed_Order_Retry_Should_Reconcile_Activation_Marker()
    {
        await using var fixture = await PaymentFixture.CreateAsync(includePayment: true);
        var payment = await fixture.Db.Payments.Include(x => x.Order).SingleAsync();
        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = fixture.Clock.UtcNow;
        payment.IsActivationProcessed = false;
        payment.ActivationProcessedAt = null;
        payment.Order!.Status = OrderStatus.Completed;
        payment.Order.PaidAt = fixture.Clock.UtcNow;
        fixture.Db.PaymentWebhookEvents.Add(fixture.CreateWebhookEvent(
            PaymentWebhookEventStatus.Failed,
            fixture.Clock.UtcNow.AddMinutes(-1)));
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            fixture.RawWebhook,
            EmptyHeaders(),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True((await fixture.Db.Payments.SingleAsync()).IsActivationProcessed);
        Assert.Equal(PaymentWebhookEventStatus.Processed, (await fixture.Db.PaymentWebhookEvents.SingleAsync()).Status);
        Assert.Equal(0, fixture.VpnProvider.CreateCalls);
    }

    [Fact]
    public async Task Duplicate_Parse_Error_Should_Remain_Controlled_And_Idempotent()
    {
        await using var fixture = await PaymentFixture.CreateAsync(includePayment: false);
        fixture.Provider.ThrowOnParse = true;

        var first = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            "malformed-webhook",
            EmptyHeaders(),
            CancellationToken.None);
        var second = await fixture.Orchestrator.ProcessAsync(
            PaymentProvider.YooKassa,
            "malformed-webhook",
            EmptyHeaders(),
            CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(1, await fixture.Db.PaymentWebhookEvents.CountAsync());
        var webhookEvent = await fixture.Db.PaymentWebhookEvents.SingleAsync();
        Assert.Equal(PaymentWebhookEventStatus.Rejected, webhookEvent.Status);
        Assert.DoesNotContain("parse-private-token", webhookEvent.ErrorText, StringComparison.Ordinal);
        Assert.Contains("REDACTED", webhookEvent.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> EmptyHeaders() => new Dictionary<string, string>();

    private sealed class PaymentFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly PaymentProviderAccount _account;
        private readonly Order _order;

        private PaymentFixture(
            SqliteConnection connection,
            ApplicationDbContext db,
            MutableClock clock,
            RecoveryPaymentProvider provider,
            CountingVpnProvider vpnProvider,
            PaymentProviderAccount account,
            Order order)
        {
            _connection = connection;
            _account = account;
            _order = order;
            Db = db;
            Clock = clock;
            Provider = provider;
            VpnProvider = vpnProvider;

            var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
            var subscriptions = new SubscriptionService(db, clock, new NodeAllocationService(db), new TestVpnProviderFactory(vpnProvider));
            Orchestrator = new PaymentOrchestrator(
                db,
                new PaymentProviderFactory(new IPaymentProvider[] { provider }),
                new IPaymentWebhookVerifier[] { provider },
                accounts,
                subscriptions,
                clock);
        }

        public ApplicationDbContext Db { get; }
        public MutableClock Clock { get; }
        public RecoveryPaymentProvider Provider { get; }
        public CountingVpnProvider VpnProvider { get; }
        public PaymentOrchestrator Orchestrator { get; }
        public string RawWebhook => "event=paid&id=evt-webhook-recovery";

        public static async Task<PaymentFixture> CreateAsync(bool includePayment)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero));
            var provider = new RecoveryPaymentProvider();
            var vpnProvider = new CountingVpnProvider();
            var user = new User { Id = Guid.NewGuid(), Email = $"recovery-{Guid.NewGuid():N}@example.test", DisplayName = "Recovery Buyer", PasswordHash = "hash", ReferralCode = $"recovery-{Guid.NewGuid():N}" };
            var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Recovery monthly", Slug = $"recovery-{Guid.NewGuid():N}", Description = "Recovery", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
            var node = new VpnNode { Id = Guid.NewGuid(), Name = $"recovery-{Guid.NewGuid():N}", Host = "127.0.0.1", IpAddress = "127.0.0.1", Region = "test", Country = "RU", Datacenter = "test", Capacity = 100, Status = NodeStatus.Ready, HealthStatus = HealthStatus.Healthy, IsAvailableForNewUsers = true, Provider = "x3ui" };
            var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.YooKassa, Mode = PaymentProviderMode.Sandbox, Name = $"recovery-{Guid.NewGuid():N}", PublicName = "YooKassa", IsEnabled = true, IsDefault = true, ShopId = "recovery-shop", SecretKeyProtected = string.Empty, UseWebhookIpAllowList = false };
            var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PendingPayment, Amount = tariff.Price, Currency = tariff.Currency, ExpiresAt = clock.UtcNow.AddMinutes(15), IsFirstPurchase = true };

            db.Users.Add(user);
            db.Tariffs.Add(tariff);
            db.VpnNodes.Add(node);
            db.PaymentProviderAccounts.Add(account);
            db.Orders.Add(order);
            if (includePayment)
            {
                db.Payments.Add(CreatePayment(order, account, provider.PaymentId));
            }

            await db.SaveChangesAsync();
            return new PaymentFixture(connection, db, clock, provider, vpnProvider, account, order);
        }

        public void AddPayment()
        {
            Db.Payments.Add(CreatePayment(_order, _account, Provider.PaymentId));
        }

        public PaymentWebhookEvent CreateWebhookEvent(PaymentWebhookEventStatus status, DateTimeOffset receivedAt)
            => new()
            {
                Provider = PaymentProvider.YooKassa,
                ProviderPaymentId = Provider.PaymentId,
                ExternalEventId = Provider.EventId,
                EventType = "payment.succeeded",
                PayloadSha256 = "preseeded",
                RawPayload = RawWebhook,
                HeadersJson = "{}",
                Status = status,
                ReceivedAt = receivedAt
            };

        private static PaymentAttempt CreatePayment(Order order, PaymentProviderAccount account, string providerPaymentId)
            => new()
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentProviderAccountId = account.Id,
                Provider = PaymentProvider.YooKassa,
                ProviderMode = PaymentProviderMode.Sandbox,
                ProviderPaymentId = providerPaymentId,
                Amount = order.Amount,
                Currency = order.Currency,
                Status = PaymentStatus.Pending,
                IdempotencyKey = $"recovery-{Guid.NewGuid():N}"
            };

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class RecoveryPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier
    {
        public PaymentProvider Provider => PaymentProvider.YooKassa;
        public string PaymentId { get; } = $"recovery-payment-{Guid.NewGuid():N}";
        public string EventId => "evt-webhook-recovery";
        public bool ThrowOnParse { get; set; }
        public int VerificationFailuresRemaining { get; set; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            if (ThrowOnParse)
            {
                throw new InvalidOperationException("secret=parse-private-token");
            }

            return Task.FromResult(new PaymentWebhookParseResult(EventId, "payment.succeeded", PaymentId, PaymentStatus.Succeeded, rawBody, false, 490m, "RUB", true));
        }

        public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            if (VerificationFailuresRemaining > 0)
            {
                VerificationFailuresRemaining--;
                throw new InvalidOperationException("Authorization: Bearer verifier-private-token");
            }

            return Task.FromResult(new PaymentWebhookVerificationResult(true, "test-verifier", null));
        }

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class CountingVpnProvider : IVpnProvider
    {
        public string Name => "x3ui";
        public int CreateCalls { get; private set; }
        public int CreateFailuresRemaining { get; set; }

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
        {
            CreateCalls++;
            if (CreateFailuresRemaining > 0)
            {
                CreateFailuresRemaining--;
                throw new InvalidOperationException("Injected VPN provisioning failure.");
            }

            return Task.FromResult(new VpnProvisionResult($"recovery-{request.SubscriptionId:N}", "vless://recovery@example.test:443", "/qr/recovery.png", "/config/recovery.txt"));
        }

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken) => CreateAccessAsync(request, cancellationToken);
        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }

    private sealed class TestVpnProviderFactory(IVpnProvider provider) : IVpnProviderFactory
    {
        public IVpnProvider Get(string providerName) => provider;
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => "***";
    }
}
