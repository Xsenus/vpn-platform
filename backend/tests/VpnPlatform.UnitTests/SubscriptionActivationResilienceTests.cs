using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SubscriptionActivationResilienceTests
{
    [Fact]
    public async Task Remote_Create_Local_Save_Failure_Should_Delete_Remote_Access_And_Retry_Cleanly()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var fixture = await SeedNewOrderAsync(db);
        var provider = new TrackingVpnProvider
        {
            AfterCreate = () => db.FailNextSaveCount = 1
        };
        var service = CreateService(db, provider, fixture.Now);

        var first = await service.ActivateOrRenewFromOrderAsync(fixture.Order, fixture.Payment);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        Assert.Equal(1, provider.DeleteCalls);
        Assert.Empty(await db.AccessCredentials.AsNoTracking().ToListAsync());
        Assert.Equal(OrderStatus.PartiallyProcessed, fixture.Order.Status);
        Assert.Equal(SubscriptionStatus.PendingActivation, (await db.Subscriptions.AsNoTracking().SingleAsync()).Status);

        provider.AfterCreate = null;
        var retry = await service.ActivateOrRenewFromOrderAsync(fixture.Order, fixture.Payment);

        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(2, provider.CreateCalls);
        Assert.Equal(1, provider.DeleteCalls);
        Assert.Single(await db.AccessCredentials.AsNoTracking().ToListAsync());
        Assert.Equal(1, (await db.VpnNodes.AsNoTracking().SingleAsync()).UsedCapacity);
    }

    [Fact]
    public async Task Caller_Cancellation_After_Remote_Create_Should_Delete_Remote_And_Propagate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var fixture = await SeedNewOrderAsync(db);
        using var cancellation = new CancellationTokenSource();
        var provider = new TrackingVpnProvider
        {
            AfterCreate = cancellation.Cancel
        };
        var service = CreateService(db, provider, fixture.Now);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ActivateOrRenewFromOrderAsync(fixture.Order, fixture.Payment, cancellation.Token));

        Assert.Equal(1, provider.DeleteCalls);
        Assert.Empty(await db.AccessCredentials.AsNoTracking().ToListAsync());
        Assert.Equal(OrderStatus.PartiallyProcessed, (await db.Orders.AsNoTracking().SingleAsync()).Status);
        Assert.Equal(SubscriptionStatus.PendingActivation, (await db.Subscriptions.AsNoTracking().SingleAsync()).Status);
        Assert.Contains(await db.AuditLogs.AsNoTracking().ToListAsync(), x => x.Action == "vpn_access.provisioning_cancelled");
    }

    [Fact]
    public async Task Cleanup_Failure_Should_Save_Sync_Marker_And_Retry_Without_Second_Create()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var fixture = await SeedNewOrderAsync(db);
        var provider = new TrackingVpnProvider
        {
            AfterCreate = () => db.FailNextSaveCount = 1,
            FailDelete = true
        };
        var service = CreateService(db, provider, fixture.Now);

        var first = await service.ActivateOrRenewFromOrderAsync(fixture.Order, fixture.Payment);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        Assert.Contains("reconciliation", first.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, provider.CreateCalls);
        Assert.Equal(1, provider.DeleteCalls);
        var marker = await db.AccessCredentials.AsNoTracking().SingleAsync();
        Assert.Equal(AccessCredentialStatus.SyncRequired, marker.Status);
        Assert.Equal(provider.ProviderAccessId, marker.ProviderAccessId);

        provider.AfterCreate = null;
        provider.FailDelete = false;
        var retry = await service.ActivateOrRenewFromOrderAsync(fixture.Order, fixture.Payment);

        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(1, provider.CreateCalls);
        Assert.Equal(1, provider.UpdateCalls);
        Assert.Equal(AccessCredentialStatus.Active, (await db.AccessCredentials.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Renewal_Retry_Should_Not_Extend_Subscription_Twice()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var fixture = await SeedRenewalOrderAsync(db);
        var provider = new TrackingVpnProvider { FailUpdateCount = 1 };
        var service = CreateService(db, provider, fixture.Now);

        var first = await service.ActivateOrRenewFromOrderAsync(fixture.Order, fixture.Payment);
        var retry = await service.ActivateOrRenewFromOrderAsync(fixture.Order, fixture.Payment);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        Assert.True(retry.IsSuccess, retry.Error);
        var subscription = await db.Subscriptions.AsNoTracking().SingleAsync();
        Assert.Equal(fixture.OriginalEndAt.AddDays(30), subscription.EndAt);
        Assert.Equal(1, subscription.RenewalCount);
        Assert.Equal(2, provider.UpdateCalls);
    }

    private static SubscriptionService CreateService(ApplicationDbContext db, IVpnProvider provider, DateTimeOffset now)
        => new(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(provider));

    private static FailingSaveApplicationDbContext CreateDb(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private static async Task<NewOrderFixture> SeedNewOrderAsync(ApplicationDbContext db)
    {
        var now = new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "activation-resilience@example.test", DisplayName = "Activation resilience", PasswordHash = "hash", ReferralCode = $"ref-{Guid.NewGuid():N}" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Resilience", Slug = $"resilience-{Guid.NewGuid():N}", DurationDays = 30, Price = 790m, Currency = "RUB", MaxDevices = 3 });
        db.VpnNodes.Add(Node(Guid.NewGuid()));
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PaymentReceived, Amount = 790m, Currency = "RUB", ExpiresAt = now.AddMinutes(15) };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, Provider = PaymentProvider.YooKassa, ProviderMode = PaymentProviderMode.Production, Status = PaymentStatus.Succeeded, Amount = 790m, Currency = "RUB", ProviderPaymentId = $"pay-{Guid.NewGuid():N}", PaidAt = now };
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return new NewOrderFixture(now, order, payment);
    }

    private static async Task<RenewalFixture> SeedRenewalOrderAsync(ApplicationDbContext db)
    {
        var now = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var node = Node(Guid.NewGuid());
        var originalEndAt = now.AddDays(10);
        db.Users.Add(new User { Id = userId, Email = "renewal-resilience@example.test", DisplayName = "Renewal resilience", PasswordHash = "hash", ReferralCode = $"ref-{Guid.NewGuid():N}" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Renewal", Slug = $"renewal-{Guid.NewGuid():N}", DurationDays = 30, Price = 890m, Currency = "RUB", MaxDevices = 2 });
        db.VpnNodes.Add(node);
        var subscription = new Subscription { Id = subscriptionId, UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = now.AddDays(-20), EndAt = originalEndAt, GracePeriodEndAt = BusinessRules.GetGracePeriodEnd(originalEndAt), SourceChannel = ChannelType.Web, CurrentServerId = node.Id };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();
        var access = new AccessCredential { Id = Guid.NewGuid(), SubscriptionId = subscriptionId, ProviderType = "x3ui", ProviderAccessId = $"client-{subscriptionId:N}", ServerId = node.Id, AccessUri = "vless://existing@example.test", Status = AccessCredentialStatus.Active, IssuedAt = now.AddDays(-20), LastSyncedAt = now, Revision = 1 };
        db.AccessCredentials.Add(access);
        subscription.CurrentAccessId = access.Id;
        await db.SaveChangesAsync();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Type = OrderType.Renewal, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PaymentReceived, Amount = 890m, Currency = "RUB", ExpiresAt = now.AddMinutes(15), ReferralContext = $"{{\"renewalSubscriptionId\":\"{subscriptionId}\"}}" };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, Provider = PaymentProvider.YooKassa, ProviderMode = PaymentProviderMode.Production, Status = PaymentStatus.Succeeded, Amount = 890m, Currency = "RUB", ProviderPaymentId = $"pay-{Guid.NewGuid():N}", PaidAt = now };
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return new RenewalFixture(now, order, payment, originalEndAt);
    }

    private static VpnNode Node(Guid id)
        => new()
        {
            Id = id,
            Name = $"node-{id:N}",
            Host = "vpn.example.test",
            IpAddress = "127.0.0.1",
            Provider = "x3ui",
            Region = "eu",
            Country = "DE",
            Datacenter = "test",
            Status = NodeStatus.Ready,
            Capacity = 100,
            SupportedProtocolsCsv = "vless",
            HealthStatus = HealthStatus.Healthy,
            Priority = 100,
            IsAvailableForNewUsers = true
        };

    private sealed class FailingSaveApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        public int FailNextSaveCount { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailNextSaveCount > 0)
            {
                FailNextSaveCount--;
                throw new DbUpdateException("Injected local persistence failure.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class SingleVpnProviderFactory(IVpnProvider provider) : IVpnProviderFactory
    {
        public IVpnProvider Get(string providerName) => provider;
    }

    private sealed class TrackingVpnProvider : IVpnProvider
    {
        public string Name => "x3ui";
        public string ProviderAccessId { get; } = $"client-{Guid.NewGuid():N}";
        public Action? AfterCreate { get; set; }
        public bool FailDelete { get; set; }
        public int FailUpdateCount { get; set; }
        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCalls++;
            AfterCreate?.Invoke();
            return Task.FromResult(Result(request, ProviderAccessId));
        }

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UpdateCalls++;
            if (FailUpdateCount > 0)
            {
                FailUpdateCount--;
                throw new InvalidOperationException("Injected remote update failure.");
            }

            return Task.FromResult(Result(request, ProviderAccessId));
        }

        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            DeleteCalls++;
            if (FailDelete)
            {
                throw new InvalidOperationException("Injected remote cleanup failure.");
            }

            return Task.CompletedTask;
        }

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);

        private static VpnProvisionResult Result(VpnProvisionRequest request, string providerAccessId)
            => new(providerAccessId, $"{request.Protocol}://client@example.test", string.Empty, "/config/test.json");
    }

    private sealed record NewOrderFixture(DateTimeOffset Now, Order Order, PaymentAttempt Payment);
    private sealed record RenewalFixture(DateTimeOffset Now, Order Order, PaymentAttempt Payment, DateTimeOffset OriginalEndAt);
}
