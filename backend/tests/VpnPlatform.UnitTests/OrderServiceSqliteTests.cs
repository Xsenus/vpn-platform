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

public class OrderServiceSqliteTests
{
    [Fact]
    public async Task ExpirePendingOrders_Should_Work_With_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "sqlite-order@test.local", DisplayName = "SQLite user", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "SQLite", Slug = "sqlite", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        db.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Amount = 100, Currency = "RUB", Status = OrderStatus.PendingPayment, ExpiresAt = now.AddMinutes(-1) },
            new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Amount = 100, Currency = "RUB", Status = OrderStatus.Completed, ExpiresAt = now.AddMinutes(-1) },
            new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Amount = 100, Currency = "RUB", Status = OrderStatus.PendingPayment, ExpiresAt = now.AddMinutes(5) });
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FixedClock(now));

        var expired = await service.ExpirePendingOrdersAsync();

        Assert.Equal(1, expired);
        Assert.Equal(1, await db.Orders.CountAsync(x => x.Status == OrderStatus.Expired));
        Assert.Equal(1, await db.Orders.CountAsync(x => x.Status == OrderStatus.Completed));
        Assert.Equal(1, await db.Orders.CountAsync(x => x.Status == OrderStatus.PendingPayment));
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Reuse_Renewal_Order_Only_For_Same_Subscription()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var firstSubscriptionId = Guid.NewGuid();
        var secondSubscriptionId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "sqlite-renewal@test.local", DisplayName = "SQLite renewal user", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "SQLite Renewal", Slug = "sqlite-renewal", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FixedClock(now));

        var first = await service.CreateOrderAsync(new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: firstSubscriptionId));
        var duplicateFirst = await service.CreateOrderAsync(new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: firstSubscriptionId));
        var second = await service.CreateOrderAsync(new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: secondSubscriptionId));

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(duplicateFirst.IsSuccess, duplicateFirst.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal(first.Value!.Id, duplicateFirst.Value!.Id);
        Assert.NotEqual(first.Value.Id, second.Value!.Id);
        Assert.Equal(firstSubscriptionId, first.Value.LinkedSubscriptionId);
        Assert.Equal(secondSubscriptionId, second.Value.LinkedSubscriptionId);
        Assert.Equal(2, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task ProcessSubscriptionLifecycle_Should_Work_With_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "sqlite-subscription@test.local", DisplayName = "SQLite user", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "SQLite", Slug = "sqlite-subscription", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        db.Subscriptions.AddRange(
            new Subscription { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = now.AddDays(-31), EndAt = now.AddMinutes(-1), GracePeriodEndAt = now.AddDays(3), SourceChannel = ChannelType.Web },
            new Subscription { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.GracePeriod, StartAt = now.AddDays(-40), EndAt = now.AddDays(-4), GracePeriodEndAt = now.AddMinutes(-1), SourceChannel = ChannelType.Web },
            new Subscription { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = now.AddDays(-1), EndAt = now.AddDays(1), GracePeriodEndAt = now.AddDays(4), SourceChannel = ChannelType.Web });
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new TestVpnProviderFactory());

        var processed = await service.ProcessLifecycleAsync();

        Assert.Equal(2, processed);
        Assert.Equal(1, await db.Subscriptions.CountAsync(x => x.Status == SubscriptionStatus.GracePeriod));
        Assert.Equal(1, await db.Subscriptions.CountAsync(x => x.Status == SubscriptionStatus.Expired));
        Assert.Equal(1, await db.Subscriptions.CountAsync(x => x.Status == SubscriptionStatus.Active));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider = new TestVpnProvider();
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TestVpnProvider : IVpnProvider
    {
        public string Name => "test";
        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult("test-access", "vless://test", "qr", "config"));

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
