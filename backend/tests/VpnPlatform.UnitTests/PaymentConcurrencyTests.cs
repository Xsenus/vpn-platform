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

public class PaymentConcurrencyTests
{
    [Fact]
    public async Task Same_Webhook_In_Parallel_Should_Not_Create_Duplicate_Subscription_Or_Access()
    {
        var dbPath = CreateDatabasePath();
        var connectionString = CreateConnectionString(dbPath);
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 12, 9, 0, 0, TimeSpan.Zero));
        var paymentProvider = new TestPaymentProvider("evt-same-webhook");
        var vpnProvider = new CountingVpnProvider();
        var paymentId = await SeedGraphAsync(connectionString, clock.UtcNow, paymentProvider.PaymentId);

        try
        {
            await using var firstDb = CreateDbContext(connectionString);
            await using var secondDb = CreateDbContext(connectionString);
            var first = CreateOrchestrator(firstDb, clock, paymentProvider, vpnProvider);
            var second = CreateOrchestrator(secondDb, clock, paymentProvider, vpnProvider);

            var rawWebhook = "event=paid&id=evt-same-webhook";
            var results = await Task.WhenAll(
                first.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None),
                second.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None));

            Assert.All(results, result => Assert.True(result.IsSuccess, result.Error));

            await using var db = CreateDbContext(connectionString);
            Assert.Equal(1, await db.PaymentWebhookEvents.CountAsync());
            Assert.Equal(1, await db.Subscriptions.CountAsync());
            Assert.Equal(1, await db.AccessCredentials.CountAsync());
            Assert.Equal(1, vpnProvider.CreateAccessCalls);

            var payment = await db.Payments.SingleAsync(x => x.Id == paymentId);
            var order = await db.Orders.SingleAsync();
            Assert.Equal(PaymentStatus.Succeeded, payment.Status);
            Assert.True(payment.IsActivationProcessed);
            Assert.Equal(OrderStatus.Completed, order.Status);
        }
        finally
        {
            TryDeleteDatabase(dbPath);
        }
    }

    [Fact]
    public async Task Webhook_And_Recheck_In_Parallel_Should_Activate_Order_Once()
    {
        var dbPath = CreateDatabasePath();
        var connectionString = CreateConnectionString(dbPath);
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 12, 9, 5, 0, TimeSpan.Zero));
        var paymentProvider = new TestPaymentProvider("evt-webhook-recheck");
        var vpnProvider = new CountingVpnProvider();
        var paymentId = await SeedGraphAsync(connectionString, clock.UtcNow, paymentProvider.PaymentId);

        try
        {
            await using var webhookDb = CreateDbContext(connectionString);
            await using var recheckDb = CreateDbContext(connectionString);
            var webhookOrchestrator = CreateOrchestrator(webhookDb, clock, paymentProvider, vpnProvider);
            var recheckOrchestrator = CreateOrchestrator(recheckDb, clock, paymentProvider, vpnProvider);

            var rawWebhook = "event=paid&id=evt-webhook-recheck";
            var webhookTask = webhookOrchestrator.ProcessAsync(PaymentProvider.YooKassa, rawWebhook, new Dictionary<string, string>(), CancellationToken.None);
            var recheckTask = recheckOrchestrator.RecheckPaymentAsync(paymentId, CancellationToken.None);
            await Task.WhenAll(webhookTask, recheckTask);

            var webhookResult = await webhookTask;
            var recheckResult = await recheckTask;
            Assert.True(webhookResult.IsSuccess, webhookResult.Error);
            Assert.True(recheckResult.IsSuccess, recheckResult.Error);

            await using var db = CreateDbContext(connectionString);
            Assert.Equal(1, await db.Subscriptions.CountAsync());
            Assert.Equal(1, await db.AccessCredentials.CountAsync());
            Assert.Equal(1, vpnProvider.CreateAccessCalls);

            var payment = await db.Payments.SingleAsync(x => x.Id == paymentId);
            var order = await db.Orders.SingleAsync();
            Assert.Equal(PaymentStatus.Succeeded, payment.Status);
            Assert.True(payment.IsActivationProcessed);
            Assert.Equal(OrderStatus.Completed, order.Status);
        }
        finally
        {
            TryDeleteDatabase(dbPath);
        }
    }

    [Fact]
    public async Task Same_Refund_In_Parallel_Should_Call_Provider_Once()
    {
        var dbPath = CreateDatabasePath();
        var connectionString = CreateConnectionString(dbPath);
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero));
        var paymentProvider = new TestPaymentProvider("evt-parallel-refund");
        var vpnProvider = new CountingVpnProvider();
        var paymentId = await SeedGraphAsync(connectionString, clock.UtcNow, paymentProvider.PaymentId);

        try
        {
            await using (var seedDb = CreateDbContext(connectionString))
            {
                var payment = await seedDb.Payments.Include(x => x.Order).SingleAsync(x => x.Id == paymentId);
                payment.Status = PaymentStatus.Succeeded;
                payment.PaidAt = clock.UtcNow;
                payment.Order!.Status = OrderStatus.Completed;
                await seedDb.SaveChangesAsync();
            }

            await using var firstDb = CreateDbContext(connectionString);
            await using var secondDb = CreateDbContext(connectionString);
            var first = CreateOrchestrator(firstDb, clock, paymentProvider, vpnProvider);
            var second = CreateOrchestrator(secondDb, clock, paymentProvider, vpnProvider);

            var results = await Task.WhenAll(
                first.RefundPaymentAsync(paymentId, 490m, "same-request", CancellationToken.None),
                second.RefundPaymentAsync(paymentId, 490m, "same-request", CancellationToken.None));

            Assert.All(results, result => Assert.True(result.IsSuccess, result.Error));
            Assert.Equal(1, paymentProvider.RefundCalls);

            await using var db = CreateDbContext(connectionString);
            Assert.Equal(1, await db.Refunds.CountAsync());
            var paymentAfterRefund = await db.Payments.SingleAsync(x => x.Id == paymentId);
            Assert.Equal(PaymentStatus.Refunded, paymentAfterRefund.Status);
            Assert.Equal(490m, paymentAfterRefund.RefundedAmount);
        }
        finally
        {
            TryDeleteDatabase(dbPath);
        }
    }

    [Fact]
    public async Task Same_Payment_Init_In_Parallel_Should_Call_Provider_Once()
    {
        var dbPath = CreateDatabasePath();
        var connectionString = CreateConnectionString(dbPath);
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 4, 11, 0, 0, TimeSpan.Zero));
        var paymentProvider = new TestPaymentProvider("evt-parallel-init");
        var vpnProvider = new CountingVpnProvider();
        var orderId = await SeedOrderForInitializationAsync(connectionString, clock.UtcNow);

        try
        {
            await using var firstDb = CreateDbContext(connectionString);
            await using var secondDb = CreateDbContext(connectionString);
            var first = CreateOrchestrator(firstDb, clock, paymentProvider, vpnProvider);
            var second = CreateOrchestrator(secondDb, clock, paymentProvider, vpnProvider);

            var results = await Task.WhenAll(
                first.InitPaymentAsync(new PaymentInitCommand(orderId, PaymentProvider.YooKassa, "https://example.test/return"), CancellationToken.None),
                second.InitPaymentAsync(new PaymentInitCommand(orderId, PaymentProvider.YooKassa, "https://example.test/return"), CancellationToken.None));

            Assert.All(results, result => Assert.True(result.IsSuccess, result.Error));
            Assert.Equal(1, paymentProvider.InitCalls);
            Assert.Equal(results[0].Value!.PaymentId, results[1].Value!.PaymentId);

            await using var db = CreateDbContext(connectionString);
            Assert.Equal(1, await db.Payments.CountAsync());
            var payment = await db.Payments.SingleAsync();
            Assert.Equal(PaymentStatus.Pending, payment.Status);
            Assert.NotEmpty(payment.ConfirmationUrl);
        }
        finally
        {
            TryDeleteDatabase(dbPath);
        }
    }

    private static PaymentOrchestrator CreateOrchestrator(
        ApplicationDbContext db,
        FixedClock clock,
        TestPaymentProvider paymentProvider,
        CountingVpnProvider vpnProvider)
    {
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var paymentProviderFactory = new PaymentProviderFactory(new IPaymentProvider[] { paymentProvider });
        var nodeAllocation = new NodeAllocationService(db);
        var subscriptionService = new SubscriptionService(db, clock, nodeAllocation, new TestVpnProviderFactory(vpnProvider));
        return new PaymentOrchestrator(db, paymentProviderFactory, new IPaymentWebhookVerifier[] { paymentProvider }, providerAccounts, subscriptionService, clock);
    }

    private static async Task<Guid> SeedGraphAsync(string connectionString, DateTimeOffset now, string providerPaymentId)
    {
        await using var db = CreateDbContext(connectionString);
        await db.Database.EnsureCreatedAsync();

        var user = new User { Id = Guid.NewGuid(), Email = "buyer@example.test", DisplayName = "Buyer", PasswordHash = "hash", ReferralCode = $"buyer-{Guid.NewGuid():N}" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = $"monthly-{Guid.NewGuid():N}", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "node-1", Host = "127.0.0.1", IpAddress = "127.0.0.1", Region = "test", Country = "RU", Datacenter = "test", Capacity = 100, Status = NodeStatus.Ready, HealthStatus = HealthStatus.Healthy, IsAvailableForNewUsers = true, Provider = "x3ui" };
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.YooKassa, Mode = PaymentProviderMode.Sandbox, Name = "local-yookassa", PublicName = "YooKassa", IsEnabled = true, IsDefault = true, ApiBaseUrl = "https://api.yookassa.ru/v3", ReturnUrl = "https://example.test/success", SecretKeyProtected = string.Empty, UseWebhookIpAllowList = false };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PendingPayment, Amount = tariff.Price, Currency = tariff.Currency, ExpiresAt = now.AddMinutes(15), IsFirstPurchase = true };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentProviderAccountId = account.Id,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = providerPaymentId,
            IdempotencyKey = $"payment:{order.Id:N}:yookassa",
            Amount = order.Amount,
            Currency = order.Currency,
            Status = PaymentStatus.Pending,
            ConfirmationUrl = "https://example.test/pay",
            ReturnUrl = account.ReturnUrl,
            RawRequest = "{}",
            RawResponse = "{}"
        };

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        return payment.Id;
    }

    private static async Task<Guid> SeedOrderForInitializationAsync(string connectionString, DateTimeOffset now)
    {
        await using var db = CreateDbContext(connectionString);
        await db.Database.EnsureCreatedAsync();

        var user = new User { Id = Guid.NewGuid(), Email = $"init-{Guid.NewGuid():N}@example.test", DisplayName = "Init Buyer", PasswordHash = "hash", ReferralCode = $"init-{Guid.NewGuid():N}" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Init Monthly", Slug = $"init-{Guid.NewGuid():N}", Description = "Init", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.YooKassa, Mode = PaymentProviderMode.Sandbox, Name = $"init-{Guid.NewGuid():N}", PublicName = "YooKassa", IsEnabled = true, IsDefault = true, ApiBaseUrl = "https://api.yookassa.ru/v3", ReturnUrl = "https://example.test/return", SecretKeyProtected = string.Empty };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PendingPayment, Amount = tariff.Price, Currency = tariff.Currency, ExpiresAt = now.AddMinutes(15), IsFirstPurchase = true };

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    private static ApplicationDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static string CreateDatabasePath()
        => Path.Combine(Path.GetTempPath(), $"vpn-platform-concurrency-{Guid.NewGuid():N}.db");

    private static string CreateConnectionString(string dbPath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Cache = SqliteCacheMode.Shared,
            DefaultTimeout = 30
        };
        return builder.ToString();
    }

    private static void TryDeleteDatabase(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { dbPath, $"{dbPath}-wal", $"{dbPath}-shm" })
        {
            for (var attempt = 0; attempt < 3 && File.Exists(path); attempt++)
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                }
            }
        }
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

    private sealed class TestPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier
    {
        private readonly string _eventId;
        private int _initCalls;
        private int _refundCalls;

        public TestPaymentProvider(string eventId)
        {
            _eventId = eventId;
            PaymentId = $"pay-{eventId}";
        }

        public PaymentProvider Provider => PaymentProvider.YooKassa;
        public string PaymentId { get; }
        public int InitCalls => Volatile.Read(ref _initCalls);
        public int RefundCalls => Volatile.Read(ref _refundCalls);

        public async Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _initCalls);
            await Task.Delay(50, cancellationToken);
            return new PaymentInitResult(PaymentId, "https://example.test/pay", "{}");
        }

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentWebhookParseResult(_eventId, "payment.succeeded", PaymentId, PaymentStatus.Succeeded, rawBody, true, 490m, "RUB", true));

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Succeeded, "{}"));

        public async Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _refundCalls);
            await Task.Delay(50, cancellationToken);
            return new PaymentRefundResult($"refund-{Guid.NewGuid():N}", RefundStatus.Succeeded, "{}");
        }

        public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentWebhookVerificationResult(true, "test", null));
    }

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider;
        public TestVpnProviderFactory(IVpnProvider provider) => _provider = provider;
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class CountingVpnProvider : IVpnProvider
    {
        private int _createAccessCalls;

        public string Name => "x3ui";
        public int CreateAccessCalls => Volatile.Read(ref _createAccessCalls);

        public async Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _createAccessCalls);
            await Task.Delay(50, cancellationToken);
            return new VpnProvisionResult($"client-{request.SubscriptionId:N}", "vless://test@example.test:443", "/qr/test.png", "/config/test.txt");
        }

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
