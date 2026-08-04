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

public class VpnAccessAutomationMvpTests
{
    [Fact]
    public async Task Node_Selection_Should_Ignore_Maintenance_Drain_Disabled_And_Fail_Closed_When_No_Node_Available()
    {
        await using var db = CreateDbContext();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = "monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true, AllowedRegionsCsv = "eu" };
        db.Tariffs.Add(tariff);
        db.VpnNodes.AddRange(
            new VpnNode { Name = "maintenance", Region = "eu", Status = NodeStatus.Maintenance, IsAvailableForNewUsers = true, Capacity = 10, UsedCapacity = 0, HealthStatus = HealthStatus.Healthy, SupportedProtocolsCsv = "vless" },
            new VpnNode { Name = "drain", Region = "eu", Status = NodeStatus.Draining, IsAvailableForNewUsers = true, Capacity = 10, UsedCapacity = 0, HealthStatus = HealthStatus.Healthy, SupportedProtocolsCsv = "vless" },
            new VpnNode { Name = "disabled", Region = "eu", Status = NodeStatus.Disabled, IsAvailableForNewUsers = true, Capacity = 10, UsedCapacity = 0, HealthStatus = HealthStatus.Healthy, SupportedProtocolsCsv = "vless" });
        await db.SaveChangesAsync();

        var service = new NodeAllocationService(db);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SelectNodeAsync(tariff));
        Assert.Equal(NodeAllocationService.NoAvailableNodeError, error.Message);

        db.VpnNodes.Add(new VpnNode { Name = "ready", Region = "eu", Status = NodeStatus.Ready, IsAvailableForNewUsers = true, Capacity = 10, UsedCapacity = 1, HealthStatus = HealthStatus.Healthy, SupportedProtocolsCsv = "vless" });
        await db.SaveChangesAsync();

        var node = await service.SelectNodeAsync(tariff);
        Assert.Equal("ready", node.Name);
    }

    [Fact]
    public async Task Access_Lifecycle_Actions_Should_Call_Provider_And_Write_History_And_Audit()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var provider = new TrackingVpnProvider();
        var service = new VpnAccessLifecycleService(db, new SingleVpnProviderFactory(provider), clock);
        var subscriptionId = Guid.NewGuid();
        var accessId = Guid.NewGuid();
        db.Subscriptions.Add(new Subscription { Id = subscriptionId, UserId = Guid.NewGuid(), TariffId = Guid.NewGuid(), Status = SubscriptionStatus.Active, StartAt = clock.UtcNow, EndAt = clock.UtcNow.AddDays(30) });
        db.AccessCredentials.Add(new AccessCredential { Id = accessId, SubscriptionId = subscriptionId, ProviderType = provider.Name, ProviderAccessId = "client-1", ServerId = Guid.NewGuid(), AccessUri = "vless://client", Status = AccessCredentialStatus.Active, LastSyncedAt = clock.UtcNow });
        await db.SaveChangesAsync();

        var disable = await service.DisableAccessAsync(accessId, "manual_admin_disable", "test", Guid.NewGuid(), CancellationToken.None);
        var enable = await service.EnableAccessAsync(accessId, "test", Guid.NewGuid(), CancellationToken.None);
        var sync = await service.SyncAccessAsync(accessId, "test", Guid.NewGuid(), CancellationToken.None);
        var reset = await service.ResetTrafficAsync(accessId, "test", Guid.NewGuid(), CancellationToken.None);

        Assert.True(disable.IsSuccess, disable.Error);
        Assert.True(enable.IsSuccess, enable.Error);
        Assert.True(sync.IsSuccess, sync.Error);
        Assert.True(reset.IsSuccess, reset.Error);
        Assert.Equal(1, provider.DisableCalls);
        Assert.Equal(1, provider.EnableCalls);
        Assert.Equal(1, provider.SyncCalls);
        Assert.Equal(1, provider.ResetCalls);
        Assert.True(await db.AccessCredentialHistories.CountAsync() >= 4);
        Assert.True(await db.AuditLogs.CountAsync() >= 4);
        Assert.Equal(AccessCredentialStatus.Active, (await db.AccessCredentials.SingleAsync()).Status);
    }

    [Fact]
    public async Task Provider_Disable_Error_Should_Not_Crash_Lifecycle_And_Should_Write_Error_History()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var provider = new TrackingVpnProvider { ThrowOnDisable = true };
        var service = new VpnAccessLifecycleService(db, new SingleVpnProviderFactory(provider), clock);
        var subscriptionId = Guid.NewGuid();
        var accessId = Guid.NewGuid();
        db.Subscriptions.Add(new Subscription { Id = subscriptionId, UserId = Guid.NewGuid(), TariffId = Guid.NewGuid(), Status = SubscriptionStatus.Active, StartAt = clock.UtcNow, EndAt = clock.UtcNow.AddDays(30) });
        db.AccessCredentials.Add(new AccessCredential { Id = accessId, SubscriptionId = subscriptionId, ProviderType = provider.Name, ProviderAccessId = "client-1", ServerId = Guid.NewGuid(), AccessUri = "vless://client", Status = AccessCredentialStatus.Active });
        await db.SaveChangesAsync();

        var result = await service.DisableAccessAsync(accessId, "manual_admin_disable", "test", null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AccessCredentialStatus.Error, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.True(await db.AccessCredentialHistories.AnyAsync(x => x.EventType.Contains("Failed")));
    }

    [Fact]
    public async Task Provider_Disable_Cancellation_Should_Persist_Unknown_State_And_Rethrow()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        using var cancellation = new CancellationTokenSource();
        var provider = new TrackingVpnProvider { CancelDisable = cancellation.Cancel };
        var service = new VpnAccessLifecycleService(db, new SingleVpnProviderFactory(provider), clock);
        var subscriptionId = Guid.NewGuid();
        var accessId = Guid.NewGuid();
        db.Subscriptions.Add(new Subscription { Id = subscriptionId, UserId = Guid.NewGuid(), TariffId = Guid.NewGuid(), Status = SubscriptionStatus.GracePeriod, StartAt = clock.UtcNow.AddDays(-30), EndAt = clock.UtcNow.AddDays(-1) });
        db.AccessCredentials.Add(new AccessCredential { Id = accessId, SubscriptionId = subscriptionId, ProviderType = provider.Name, ProviderAccessId = "client-cancelled", ServerId = Guid.NewGuid(), AccessUri = "vless://client", Status = AccessCredentialStatus.Active });
        await db.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.DisableAccessAsync(accessId, "expiry", "test", null, cancellation.Token));

        Assert.Equal(AccessCredentialStatus.SyncRequired, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Contains(await db.AccessCredentialHistories.ToListAsync(), x => x.EventType.EndsWith("Cancelled", StringComparison.Ordinal));
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "access.disable.cancelled");
    }

    [Theory]
    [InlineData("enable", AccessCredentialStatus.Disabled, AccessCredentialStatus.SyncRequired, "AccessEnableCancelled", "access.enable.cancelled")]
    [InlineData("sync", AccessCredentialStatus.Active, AccessCredentialStatus.Active, "AccessSyncCancelled", "access.sync.cancelled")]
    [InlineData("reset", AccessCredentialStatus.Active, AccessCredentialStatus.SyncRequired, "AccessTrafficResetCancelled", "access.reset_traffic.cancelled")]
    public async Task Provider_Action_Cancellation_Should_Persist_Safe_State_And_Rethrow(
        string operation,
        AccessCredentialStatus initialStatus,
        AccessCredentialStatus expectedStatus,
        string expectedHistory,
        string expectedAudit)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock();
        using var cancellation = new CancellationTokenSource();
        var provider = new TrackingVpnProvider { CancelOperation = operation, Cancel = cancellation.Cancel };
        var service = new VpnAccessLifecycleService(db, new SingleVpnProviderFactory(provider), clock);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var accessId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = $"{operation}@example.test", DisplayName = "Client", Status = UserStatus.Active });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Premium", Slug = $"premium-{operation}", DurationDays = 30, Price = 490, Currency = "RUB" });
        db.VpnNodes.Add(new VpnNode { Id = nodeId, Name = "NL-1", Host = "nl1.example.test", IpAddress = "127.0.0.1", Capacity = 100, UsedCapacity = 1 });
        db.Subscriptions.Add(new Subscription { Id = subscriptionId, UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = clock.UtcNow, EndAt = clock.UtcNow.AddDays(30) });
        db.AccessCredentials.Add(new AccessCredential { Id = accessId, SubscriptionId = subscriptionId, ProviderType = provider.Name, ProviderAccessId = $"client-{operation}", ServerId = nodeId, AccessUri = "vless://client", Status = initialStatus });
        await db.SaveChangesAsync();

        async Task Act() => await (operation switch
        {
            "enable" => service.EnableAccessAsync(accessId, "test", null, cancellation.Token),
            "sync" => service.SyncAccessAsync(accessId, "test", null, cancellation.Token),
            "reset" => service.ResetTrafficAsync(accessId, "test", null, cancellation.Token),
            _ => throw new InvalidOperationException($"Unsupported test operation: {operation}")
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(Act);

        db.ChangeTracker.Clear();
        Assert.Equal(expectedStatus, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Contains(await db.AccessCredentialHistories.ToListAsync(), x => x.EventType == expectedHistory);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == expectedAudit);
    }

    [Fact]
    public async Task Provider_Reset_Failure_Should_Persist_Reconciliation_State()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock();
        var provider = new TrackingVpnProvider { ThrowOnReset = true };
        var service = new VpnAccessLifecycleService(db, new SingleVpnProviderFactory(provider), clock);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var accessId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "reset-failure@example.test", DisplayName = "Client", Status = UserStatus.Active });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Premium", Slug = "premium-reset-failure", DurationDays = 30, Price = 490, Currency = "RUB" });
        db.VpnNodes.Add(new VpnNode { Id = nodeId, Name = "NL-1", Host = "nl1.example.test", IpAddress = "127.0.0.1", Capacity = 100, UsedCapacity = 1 });
        db.Subscriptions.Add(new Subscription { Id = subscriptionId, UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = clock.UtcNow, EndAt = clock.UtcNow.AddDays(30) });
        db.AccessCredentials.Add(new AccessCredential { Id = accessId, SubscriptionId = subscriptionId, ProviderType = provider.Name, ProviderAccessId = "client-reset-failure", ServerId = nodeId, AccessUri = "vless://client", Status = AccessCredentialStatus.Active });
        await db.SaveChangesAsync();

        var result = await service.ResetTrafficAsync(accessId, "test", null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(AccessCredentialStatus.SyncRequired, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Contains(await db.AccessCredentialHistories.ToListAsync(), x => x.EventType == "AccessTrafficResetFailed");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "access.reset_traffic.failed");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class SingleVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider;
        public SingleVpnProviderFactory(IVpnProvider provider) => _provider = provider;
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TrackingVpnProvider : IVpnProvider
    {
        public string Name => "x3ui";
        public int DisableCalls { get; private set; }
        public int EnableCalls { get; private set; }
        public int SyncCalls { get; private set; }
        public int ResetCalls { get; private set; }
        public bool ThrowOnDisable { get; init; }
        public bool ThrowOnReset { get; init; }
        public Action? CancelDisable { get; init; }
        public string? CancelOperation { get; init; }
        public Action? Cancel { get; init; }

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult($"client-{request.SubscriptionId:N}", "vless://client", "vless://client", "/config.json"));

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult($"client-{request.SubscriptionId:N}", "vless://client-renewed", "vless://client-renewed", "/config.json"));

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            DisableCalls += 1;
            CancelDisable?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (ThrowOnDisable) throw new InvalidOperationException("provider disabled with secret token value");
            return Task.CompletedTask;
        }

        public Task EnableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            EnableCalls += 1;
            CancelIfRequested("enable", cancellationToken);
            return Task.CompletedTask;
        }

        public Task<VpnUsageSnapshot> SyncAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            SyncCalls += 1;
            CancelIfRequested("sync", cancellationToken);
            return Task.FromResult(new VpnUsageSnapshot(providerAccessId, 1234, 1, DateTimeOffset.UtcNow));
        }

        public Task ResetTrafficAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            ResetCalls += 1;
            CancelIfRequested("reset", cancellationToken);
            if (ThrowOnReset) throw new InvalidOperationException("traffic reset outcome is unknown");
            return Task.CompletedTask;
        }

        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 1234, 1, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);

        private void CancelIfRequested(string operation, CancellationToken cancellationToken)
        {
            if (!string.Equals(CancelOperation, operation, StringComparison.Ordinal)) return;
            Cancel?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
