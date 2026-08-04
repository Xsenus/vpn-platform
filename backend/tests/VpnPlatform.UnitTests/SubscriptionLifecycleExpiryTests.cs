using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SubscriptionLifecycleExpiryTests
{
    [Fact]
    public async Task Grace_Period_Subscription_Should_Expire_Disable_Access_And_Write_History()
    {
        await using var db = CreateDbContext();
        var provider = new TrackingVpnProvider();
        var service = new SubscriptionService(db, new FixedClock(), new NodeAllocationService(db), new TestVpnProviderFactory(provider));
        var access = await SeedExpiredGraceSubscriptionAsync(db, withTelegram: true);

        var processed = await service.ProcessLifecycleAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var subscription = await db.Subscriptions.Include(x => x.CurrentAccess).SingleAsync();
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.Equal(AccessCredentialStatus.Disabled, subscription.CurrentAccess!.Status);
        Assert.Equal(new FixedClock().UtcNow, subscription.CurrentAccess.DisabledAt);
        Assert.Equal(new[] { access.ProviderAccessId }, provider.DisabledAccessIds);
        var history = await db.AccessCredentialHistories.SingleAsync();
        Assert.Equal("AccessDisabledOnExpiry", history.EventType);
        Assert.Contains(access.ProviderAccessId, history.NewValueJson);
        var outbox = await db.OutboxMessages.SingleAsync(x => x.CorrelationId == $"subscription_expired:{subscription.Id:N}");
        Assert.Equal("NotificationRequested", outbox.Type);
        Assert.Contains("subscription_expired", outbox.PayloadJson, StringComparison.OrdinalIgnoreCase);
        var telegram = await db.TelegramBotNotifications.SingleAsync(x => x.Type == "subscription_expired");
        Assert.Contains("VPN-доступ отключен", PayloadText(telegram.PayloadJson), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DisableAccess_Error_Should_Keep_Grace_And_Retry_Before_Expiring()
    {
        await using var db = CreateDbContext();
        var provider = new TrackingVpnProvider { ThrowOnDisable = true };
        var clock = new FixedClock();
        var service = new SubscriptionService(db, clock, new NodeAllocationService(db), new TestVpnProviderFactory(provider));
        var access = await SeedExpiredGraceSubscriptionAsync(db);

        var processed = await service.ProcessLifecycleAsync(CancellationToken.None);

        Assert.Equal(0, processed);
        var subscription = await db.Subscriptions.Include(x => x.CurrentAccess).SingleAsync();
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
        Assert.Equal(AccessCredentialStatus.Error, subscription.CurrentAccess!.Status);
        Assert.Null(subscription.CurrentAccess.DisabledAt);
        Assert.Equal(1, subscription.LifecycleAttemptCount);
        Assert.Equal(clock.UtcNow.AddMinutes(5), subscription.LifecycleNextAttemptAt);
        Assert.Contains("disable failed", subscription.LifecycleLastError, StringComparison.OrdinalIgnoreCase);
        Assert.Null(subscription.LifecycleProcessingStartedAt);
        Assert.Null(subscription.LifecycleLeaseExpiresAt);
        Assert.Equal(new[] { access.ProviderAccessId }, provider.DisabledAccessIds);
        var history = await db.AccessCredentialHistories.SingleAsync();
        Assert.Equal("AccessDisableFailedOnExpiry", history.EventType);
        Assert.Contains("disable failed", history.NewValueJson, StringComparison.OrdinalIgnoreCase);
        Assert.False(await db.OutboxMessages.AnyAsync(x => x.CorrelationId.StartsWith("subscription_expired:")));

        provider.ThrowOnDisable = false;
        var retryClock = new FixedClock(clock.UtcNow.AddMinutes(6));
        var retryService = new SubscriptionService(db, retryClock, new NodeAllocationService(db), new TestVpnProviderFactory(provider));

        Assert.Equal(1, await retryService.ProcessLifecycleAsync(CancellationToken.None));
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.Equal(AccessCredentialStatus.Disabled, subscription.CurrentAccess.Status);
        Assert.Equal(2, subscription.LifecycleAttemptCount);
        Assert.Null(subscription.LifecycleNextAttemptAt);
        Assert.Null(subscription.LifecycleLastError);
        Assert.Single(await db.OutboxMessages.Where(x => x.CorrelationId.StartsWith("subscription_expired:")).ToListAsync());
    }

    [Fact]
    public async Task Expired_Lifecycle_Lease_Should_Be_Recovered()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var provider = new TrackingVpnProvider();
        await SeedExpiredGraceSubscriptionAsync(db);
        var subscription = await db.Subscriptions.SingleAsync();
        subscription.LifecycleAttemptCount = 1;
        subscription.LifecycleProcessingStartedAt = clock.UtcNow.AddMinutes(-10);
        subscription.LifecycleLeaseExpiresAt = clock.UtcNow.AddMinutes(-5);
        await db.SaveChangesAsync();
        var service = new SubscriptionService(db, clock, new NodeAllocationService(db), new TestVpnProviderFactory(provider));

        var processed = await service.ProcessLifecycleAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.Equal(2, subscription.LifecycleAttemptCount);
        Assert.Null(subscription.LifecycleProcessingStartedAt);
        Assert.Null(subscription.LifecycleLeaseExpiresAt);
        Assert.Single(provider.DisabledAccessIds);
    }

    [Fact]
    public async Task Concurrent_Lifecycle_Runs_Should_Disable_Access_Once()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-subscription-lifecycle-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Default Timeout=10;Pooling=False";
        var provider = new TrackingVpnProvider { BlockOnDisable = true };
        var clock = new FixedClock();

        try
        {
            await using (var setupDb = CreateSqliteDbContext(connectionString))
            {
                await setupDb.Database.EnsureCreatedAsync();
                await SeedExpiredGraceSubscriptionAsync(setupDb);
            }

            await using var firstDb = CreateSqliteDbContext(connectionString);
            await using var secondDb = CreateSqliteDbContext(connectionString);
            var firstService = new SubscriptionService(firstDb, clock, new NodeAllocationService(firstDb), new TestVpnProviderFactory(provider));
            var secondService = new SubscriptionService(secondDb, clock, new NodeAllocationService(secondDb), new TestVpnProviderFactory(provider));

            var firstRun = firstService.ProcessLifecycleAsync(CancellationToken.None);
            await provider.DisableStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var secondRun = secondService.ProcessLifecycleAsync(CancellationToken.None);
            provider.ReleaseDisable.TrySetResult();

            Assert.Equal(1, await firstRun);
            Assert.Equal(0, await secondRun);
            Assert.Single(provider.DisabledAccessIds);

            await using var assertDb = CreateSqliteDbContext(connectionString);
            var subscription = await assertDb.Subscriptions.AsNoTracking().SingleAsync();
            Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
            Assert.Equal(1, subscription.LifecycleAttemptCount);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Active_Expired_Subscription_Should_Move_To_Grace_Without_Disabling_Access_Yet()
    {
        await using var db = CreateDbContext();
        var provider = new TrackingVpnProvider();
        var service = new SubscriptionService(db, new FixedClock(), new NodeAllocationService(db), new TestVpnProviderFactory(provider));
        await SeedSubscriptionAsync(db, SubscriptionStatus.Active, new FixedClock().UtcNow.AddDays(-1), new FixedClock().UtcNow.AddDays(2), withTelegram: true);

        var processed = await service.ProcessLifecycleAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var subscription = await db.Subscriptions.Include(x => x.CurrentAccess).SingleAsync();
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
        Assert.Equal(AccessCredentialStatus.Active, subscription.CurrentAccess!.Status);
        Assert.Empty(provider.DisabledAccessIds);
        Assert.Empty(await db.AccessCredentialHistories.ToListAsync());
        var outbox = await db.OutboxMessages.SingleAsync(x => x.CorrelationId == $"subscription_expiring:{subscription.Id:N}");
        Assert.Equal("NotificationRequested", outbox.Type);
        Assert.Contains("subscription_expiring", outbox.PayloadJson, StringComparison.OrdinalIgnoreCase);
        var telegram = await db.TelegramBotNotifications.SingleAsync(x => x.Type == "subscription_expiring");
        Assert.Contains("льготный период", PayloadText(telegram.PayloadJson), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lifecycle_Notifications_Should_Be_Idempotent_On_Repeated_Run()
    {
        await using var db = CreateDbContext();
        var provider = new TrackingVpnProvider();
        var service = new SubscriptionService(db, new FixedClock(), new NodeAllocationService(db), new TestVpnProviderFactory(provider));
        await SeedExpiredGraceSubscriptionAsync(db, withTelegram: true);

        var first = await service.ProcessLifecycleAsync(CancellationToken.None);
        var second = await service.ProcessLifecycleAsync(CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(1, await db.OutboxMessages.CountAsync(x => x.CorrelationId.StartsWith("subscription_expired:")));
        Assert.Equal(1, await db.TelegramBotNotifications.CountAsync(x => x.Type == "subscription_expired"));
    }

    private static async Task<AccessCredential> SeedExpiredGraceSubscriptionAsync(ApplicationDbContext db, bool withTelegram = false)
        => await SeedSubscriptionAsync(db, SubscriptionStatus.GracePeriod, new FixedClock().UtcNow.AddDays(-10), new FixedClock().UtcNow.AddSeconds(-1), withTelegram);

    private static async Task<AccessCredential> SeedSubscriptionAsync(ApplicationDbContext db, SubscriptionStatus status, DateTimeOffset endAt, DateTimeOffset gracePeriodEndAt, bool withTelegram = false)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"user-{Guid.NewGuid():N}@example.test",
            DisplayName = "User",
            PasswordHash = "hash",
            RolesCsv = "User",
            Status = UserStatus.Active,
            ReferralCode = Guid.NewGuid().ToString("N")
        };
        var tariff = new Tariff
        {
            Id = Guid.NewGuid(),
            Name = "Monthly",
            Slug = Guid.NewGuid().ToString("N"),
            Description = "VPN monthly",
            DurationDays = 30,
            Price = 490m,
            Currency = "RUB",
            MaxDevices = 3,
            IsActive = true
        };
        var node = new VpnNode
        {
            Id = Guid.NewGuid(),
            Name = "node",
            Host = "127.0.0.1",
            IpAddress = "127.0.0.1",
            Provider = "x3ui",
            Region = "test",
            Country = "RU",
            Datacenter = "local",
            Status = NodeStatus.Ready,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 100,
            IsAvailableForNewUsers = true
        };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = status,
            StartAt = endAt.AddDays(-30),
            EndAt = endAt,
            GracePeriodEndAt = gracePeriodEndAt,
            CurrentServerId = node.Id
        };
        var access = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            ProviderType = "x3ui",
            ProviderAccessId = $"client-{subscription.Id:N}",
            ServerId = node.Id,
            AccessUri = "vless://client@test",
            QrCodePath = "vless://client@test",
            ConfigPath = string.Empty,
            Status = AccessCredentialStatus.Active,
            IssuedAt = endAt.AddDays(-30)
        };
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(subscription);
        if (withTelegram)
        {
            db.TelegramAccounts.Add(new TelegramAccount
            {
                UserId = user.Id,
                TelegramUserId = Random.Shared.NextInt64(10_000, 99_999),
                Username = "vpn_user",
                IsBlocked = false
            });
        }
        await db.SaveChangesAsync();

        db.AccessCredentials.Add(access);
        subscription.CurrentAccessId = access.Id;
        await db.SaveChangesAsync();
        return access;
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(string connectionString)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options);

    private static string PayloadText(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return document.RootElement.GetProperty("text").GetString() ?? string.Empty;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset? now = null) => UtcNow = now ?? new DateTimeOffset(2026, 4, 30, 10, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider;
        public TestVpnProviderFactory(IVpnProvider provider) => _provider = provider;
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TrackingVpnProvider : IVpnProvider
    {
        private readonly List<string> _disabledAccessIds = new();
        public string Name => "x3ui";
        public bool ThrowOnDisable { get; set; }
        public bool BlockOnDisable { get; set; }
        public IReadOnlyList<string> DisabledAccessIds => _disabledAccessIds;
        public TaskCompletionSource DisableStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseDisable { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult($"client-{request.SubscriptionId:N}", "vless://test", "vless://test", string.Empty));

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => CreateAccessAsync(request, cancellationToken);

        public async Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            _disabledAccessIds.Add(providerAccessId);
            DisableStarted.TrySetResult();
            if (BlockOnDisable)
            {
                await ReleaseDisable.Task.WaitAsync(cancellationToken);
            }
            if (ThrowOnDisable)
            {
                throw new InvalidOperationException("disable failed");
            }
        }

        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }
}
