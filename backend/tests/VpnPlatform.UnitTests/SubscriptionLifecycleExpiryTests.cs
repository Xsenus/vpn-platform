using System.Text.Json;
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
    public async Task DisableAccess_Error_Should_Not_Break_Lifecycle_And_Should_Write_Failure_History()
    {
        await using var db = CreateDbContext();
        var provider = new TrackingVpnProvider { ThrowOnDisable = true };
        var service = new SubscriptionService(db, new FixedClock(), new NodeAllocationService(db), new TestVpnProviderFactory(provider));
        var access = await SeedExpiredGraceSubscriptionAsync(db);

        var processed = await service.ProcessLifecycleAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var subscription = await db.Subscriptions.Include(x => x.CurrentAccess).SingleAsync();
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.Equal(AccessCredentialStatus.Error, subscription.CurrentAccess!.Status);
        Assert.Null(subscription.CurrentAccess.DisabledAt);
        Assert.Equal(new[] { access.ProviderAccessId }, provider.DisabledAccessIds);
        var history = await db.AccessCredentialHistories.SingleAsync();
        Assert.Equal("AccessDisableFailedOnExpiry", history.EventType);
        Assert.Contains("disable failed", history.NewValueJson, StringComparison.OrdinalIgnoreCase);
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
        subscription.CurrentAccessId = access.Id;

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(subscription);
        db.AccessCredentials.Add(access);
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
        return access;
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static string PayloadText(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return document.RootElement.GetProperty("text").GetString() ?? string.Empty;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 4, 30, 10, 0, 0, TimeSpan.Zero);
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
        public IReadOnlyList<string> DisabledAccessIds => _disabledAccessIds;

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult($"client-{request.SubscriptionId:N}", "vless://test", "vless://test", string.Empty));

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => CreateAccessAsync(request, cancellationToken);

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            _disabledAccessIds.Add(providerAccessId);
            if (ThrowOnDisable)
            {
                throw new InvalidOperationException("disable failed");
            }

            return Task.CompletedTask;
        }

        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }
}
