using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminSubscriptionManagementTests
{
    [Fact]
    public async Task Activate_And_SyncSubscriptionAccess_Should_Update_Status_Access_And_History_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));
        var provider = new TrackingVpnProvider(clock.UtcNow);
        var controller = CreateController(db, provider, clock);
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var pendingLifecycle = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        pendingLifecycle.LifecycleAttemptCount = 3;
        pendingLifecycle.LifecycleProcessingStartedAt = clock.UtcNow.AddMinutes(-1);
        pendingLifecycle.LifecycleLeaseExpiresAt = clock.UtcNow.AddMinutes(4);
        pendingLifecycle.LifecycleNextAttemptAt = clock.UtcNow.AddMinutes(10);
        pendingLifecycle.LifecycleLastError = "previous provider failure";
        await db.SaveChangesAsync();

        var activated = await controller.ActivateSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("manual activation"), CancellationToken.None);
        var synced = await controller.SyncSubscriptionAccess(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator sync"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(activated);
        Assert.IsType<OkObjectResult>(synced);
        Assert.Equal(1, provider.EnableCalls);
        Assert.Equal(1, provider.SyncCalls);

        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Null(subscription.BlockReason);
        Assert.Null(subscription.CancelledAt);
        Assert.Equal(0, subscription.LifecycleAttemptCount);
        Assert.Null(subscription.LifecycleProcessingStartedAt);
        Assert.Null(subscription.LifecycleLeaseExpiresAt);
        Assert.Null(subscription.LifecycleNextAttemptAt);
        Assert.Null(subscription.LifecycleLastError);
        Assert.Equal(AccessCredentialStatus.Active, access.Status);
        Assert.Null(access.DisabledAt);
        Assert.Equal(clock.UtcNow, access.LastSyncedAt);
        Assert.True(access.Revision >= 2);

        var history = await db.AccessCredentialHistories.Where(x => x.AccessCredentialId == ids.AccessId).ToListAsync();
        Assert.Contains(history, x => x.EventType == "AccessEnabled");
        Assert.Contains(history, x => x.EventType == "AccessSynced");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "subscription.activate" && x.EntityId == ids.SubscriptionId.ToString());

        var syncJson = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(synced).Value);
        Assert.Contains("CurrentAccessId", syncJson, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", syncJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncSubscriptionAccess_Should_Reject_Subscription_Without_CurrentAccess()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));
        var controller = CreateController(db, new TrackingVpnProvider(clock.UtcNow), clock);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "user@example.test", DisplayName = "User", Status = UserStatus.Active });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Premium", Slug = "premium", DurationDays = 30, Price = 490, Currency = "RUB" });
        db.Subscriptions.Add(new Subscription { Id = subscriptionId, UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = clock.UtcNow, EndAt = clock.UtcNow.AddDays(30) });
        await db.SaveChangesAsync();

        var result = await controller.SyncSubscriptionAccess(subscriptionId, new AdminAccessActionHttpRequest("operator sync"), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("current VPN access", JsonSerializer.Serialize(badRequest.Value), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cancelled_Subscription_Sync_Should_Reject_Before_Provider_Call_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 8, 5, 7, 0, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = clock.UtcNow;
        access.Status = AccessCredentialStatus.Active;
        access.DisabledAt = null;
        await db.SaveChangesAsync();

        var provider = new TrackingVpnProvider(clock.UtcNow);
        var controller = CreateController(db, provider, clock);

        var result = await controller.SyncSubscriptionAccess(
            ids.SubscriptionId,
            new AdminAccessActionHttpRequest("operator sync"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cancelled", JsonSerializer.Serialize(badRequest.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.SyncCalls);
        Assert.Empty(await db.AccessCredentialHistories.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Equal(SubscriptionStatus.Cancelled, (await db.Subscriptions.SingleAsync()).Status);
        Assert.Equal(AccessCredentialStatus.Active, (await db.AccessCredentials.SingleAsync()).Status);
    }

    [Fact]
    public async Task Expired_Grace_Subscription_Sync_Should_Reject_Before_Provider_Call_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 8, 5, 7, 15, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        subscription.Status = SubscriptionStatus.GracePeriod;
        subscription.EndAt = clock.UtcNow.AddDays(-3);
        subscription.GracePeriodEndAt = clock.UtcNow;
        access.Status = AccessCredentialStatus.Active;
        access.DisabledAt = null;
        await db.SaveChangesAsync();

        var provider = new TrackingVpnProvider(clock.UtcNow);
        var controller = CreateController(db, provider, clock);
        var result = await controller.SyncSubscriptionAccess(
            ids.SubscriptionId,
            new AdminAccessActionHttpRequest("operator sync"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("expired", JsonSerializer.Serialize(badRequest.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.SyncCalls);
        Assert.Empty(await db.AccessCredentialHistories.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Direct_Access_Sync_Should_Wait_For_Subscription_Gate_And_Recheck_Cancelled_Status()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 8, 5, 7, 30, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        access.Status = AccessCredentialStatus.Active;
        access.DisabledAt = null;
        await db.SaveChangesAsync();

        var provider = new TrackingVpnProvider(clock.UtcNow);
        var controller = CreateController(db, provider, clock);
        var heldGate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(ids.SubscriptionId, CancellationToken.None);
        var syncTask = controller.SyncAccessCredential(ids.AccessId, new AdminAccessActionHttpRequest("operator sync"), CancellationToken.None);

        await Task.Delay(100);
        var waitedForGate = !syncTask.IsCompleted;
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = clock.UtcNow;
        await db.SaveChangesAsync();
        await heldGate.DisposeAsync();

        var result = await syncTask;
        Assert.True(waitedForGate);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cancelled", JsonSerializer.Serialize(badRequest.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.SyncCalls);
        Assert.Empty(await db.AccessCredentialHistories.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("extend", SubscriptionStatus.Expired, AccessCredentialStatus.Disabled, true)]
    [InlineData("activate", SubscriptionStatus.PendingActivation, AccessCredentialStatus.Disabled, true)]
    [InlineData("unblock", SubscriptionStatus.Blocked, AccessCredentialStatus.Disabled, true)]
    [InlineData("block", SubscriptionStatus.Active, AccessCredentialStatus.Active, false)]
    [InlineData("cancel", SubscriptionStatus.Active, AccessCredentialStatus.Active, false)]
    public async Task Subscription_Command_Should_Not_Mutate_Subscription_When_Vpn_Provider_Fails(
        string command,
        SubscriptionStatus initialSubscriptionStatus,
        AccessCredentialStatus initialAccessStatus,
        bool failEnable)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        subscription.Status = initialSubscriptionStatus;
        subscription.EndAt = command == "extend" ? clock.UtcNow.AddDays(-1) : clock.UtcNow.AddDays(10);
        subscription.BlockReason = command == "unblock" ? "manual hold" : "original reason";
        subscription.CancelledAt = null;
        access.Status = initialAccessStatus;
        access.DisabledAt = initialAccessStatus == AccessCredentialStatus.Disabled ? clock.UtcNow.AddHours(-1) : null;
        await db.SaveChangesAsync();

        var initialEndAt = subscription.EndAt;
        var initialBlockReason = subscription.BlockReason;
        var provider = new TrackingVpnProvider(
            clock.UtcNow,
            failEnable: failEnable,
            failDisable: command == "block",
            failDelete: command == "cancel");
        var controller = CreateController(db, provider, clock);

        var result = command switch
        {
            "extend" => await controller.ExtendSubscription(ids.SubscriptionId, new AdminSubscriptionExtendHttpRequest(30, "operator action"), CancellationToken.None),
            "activate" => await controller.ActivateSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            "unblock" => await controller.UnblockSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            "block" => await controller.BlockSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            "cancel" => await controller.CancelSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            _ => throw new InvalidOperationException($"Unknown test command: {command}")
        };

        Assert.IsType<BadRequestObjectResult>(result);
        db.ChangeTracker.Clear();
        var persistedSubscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var persistedAccess = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        Assert.Equal(initialSubscriptionStatus, persistedSubscription.Status);
        Assert.Equal(initialEndAt, persistedSubscription.EndAt);
        Assert.Equal(initialBlockReason, persistedSubscription.BlockReason);
        Assert.Null(persistedSubscription.CancelledAt);
        Assert.Equal(command == "cancel" ? AccessCredentialStatus.SyncRequired : AccessCredentialStatus.Error, persistedAccess.Status);
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action == $"subscription.{command}");
        var expectedAccessAudit = failEnable
            ? "access.enable.failed"
            : command == "cancel" ? "access.revoke.failed" : "access.disable.failed";
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == expectedAccessAudit);
        if (command == "cancel")
        {
            Assert.Equal(1, (await db.VpnNodes.SingleAsync()).UsedCapacity);
        }
    }

    [Fact]
    public async Task Unblock_Subscription_At_Paid_End_Should_Restore_GracePeriod_And_Enable_Access()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        subscription.Status = SubscriptionStatus.Blocked;
        subscription.EndAt = clock.UtcNow;
        subscription.GracePeriodEndAt = clock.UtcNow.AddDays(3);
        subscription.BlockReason = "manual hold";
        await db.SaveChangesAsync();
        var provider = new TrackingVpnProvider(clock.UtcNow);
        var controller = CreateController(db, provider, clock);

        Assert.IsType<OkObjectResult>(await controller.UnblockSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("reviewed"), CancellationToken.None));

        db.ChangeTracker.Clear();
        var persistedSubscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var persistedAccess = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        Assert.Equal(SubscriptionStatus.GracePeriod, persistedSubscription.Status);
        Assert.Null(persistedSubscription.BlockReason);
        Assert.Equal(AccessCredentialStatus.Active, persistedAccess.Status);
        Assert.Equal(1, provider.EnableCalls);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "subscription.unblock");
    }

    [Fact]
    public async Task Unblock_Subscription_At_Grace_End_Should_Expire_Without_Enabling_Access()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 10, 30, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        subscription.Status = SubscriptionStatus.Blocked;
        subscription.EndAt = clock.UtcNow.AddDays(-3);
        subscription.GracePeriodEndAt = clock.UtcNow;
        subscription.BlockReason = "manual hold";
        await db.SaveChangesAsync();
        var provider = new TrackingVpnProvider(clock.UtcNow);
        var controller = CreateController(db, provider, clock);

        Assert.IsType<OkObjectResult>(await controller.UnblockSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("reviewed"), CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal(SubscriptionStatus.Expired, (await db.Subscriptions.SingleAsync()).Status);
        Assert.Equal(AccessCredentialStatus.Disabled, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Equal(0, provider.EnableCalls);
    }

    [Theory]
    [InlineData("extend", SubscriptionStatus.Expired, AccessCredentialStatus.Disabled)]
    [InlineData("activate", SubscriptionStatus.PendingActivation, AccessCredentialStatus.Disabled)]
    [InlineData("unblock", SubscriptionStatus.Blocked, AccessCredentialStatus.Disabled)]
    [InlineData("block", SubscriptionStatus.Active, AccessCredentialStatus.Active)]
    [InlineData("cancel", SubscriptionStatus.Active, AccessCredentialStatus.Active)]
    public async Task Subscription_Command_Should_Fail_Closed_When_Access_Lifecycle_Is_Missing(
        string command,
        SubscriptionStatus initialSubscriptionStatus,
        AccessCredentialStatus initialAccessStatus)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 11, 0, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        subscription.Status = initialSubscriptionStatus;
        subscription.EndAt = command == "extend" ? clock.UtcNow.AddDays(-1) : clock.UtcNow.AddDays(10);
        subscription.BlockReason = command == "unblock" ? "manual hold" : "original reason";
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        access.Status = initialAccessStatus;
        access.DisabledAt = initialAccessStatus == AccessCredentialStatus.Disabled ? clock.UtcNow.AddHours(-1) : null;
        await db.SaveChangesAsync();
        var controller = new AdminOperationsController(db, null!, null!, null!, clock: clock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = command switch
        {
            "extend" => await controller.ExtendSubscription(ids.SubscriptionId, new AdminSubscriptionExtendHttpRequest(30, "operator action"), CancellationToken.None),
            "activate" => await controller.ActivateSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            "unblock" => await controller.UnblockSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            "block" => await controller.BlockSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            "cancel" => await controller.CancelSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            _ => throw new InvalidOperationException($"Unknown test command: {command}")
        };

        Assert.IsType<BadRequestObjectResult>(result);
        db.ChangeTracker.Clear();
        Assert.Equal(initialSubscriptionStatus, (await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId)).Status);
        Assert.Equal(initialAccessStatus, (await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId)).Status);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("extend", SubscriptionStatus.Expired, AccessCredentialStatus.Disabled, SubscriptionStatus.Active, AccessCredentialStatus.Active)]
    [InlineData("block", SubscriptionStatus.Active, AccessCredentialStatus.Active, SubscriptionStatus.Blocked, AccessCredentialStatus.Disabled)]
    [InlineData("unblock", SubscriptionStatus.Blocked, AccessCredentialStatus.Disabled, SubscriptionStatus.Active, AccessCredentialStatus.Active)]
    [InlineData("cancel", SubscriptionStatus.Active, AccessCredentialStatus.Active, SubscriptionStatus.Cancelled, AccessCredentialStatus.Revoked)]
    public async Task Subscription_Command_Should_Update_Subscription_After_Vpn_Provider_Succeeds(
        string command,
        SubscriptionStatus initialSubscriptionStatus,
        AccessCredentialStatus initialAccessStatus,
        SubscriptionStatus expectedSubscriptionStatus,
        AccessCredentialStatus expectedAccessStatus)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        subscription.Status = initialSubscriptionStatus;
        subscription.EndAt = command == "extend" ? clock.UtcNow.AddDays(-2) : clock.UtcNow.AddDays(10);
        subscription.BlockReason = command == "unblock" ? "manual hold" : null;
        access.Status = initialAccessStatus;
        access.DisabledAt = initialAccessStatus == AccessCredentialStatus.Disabled ? clock.UtcNow.AddHours(-1) : null;
        await db.SaveChangesAsync();
        var provider = new TrackingVpnProvider(clock.UtcNow);
        var controller = CreateController(db, provider, clock);

        var result = command switch
        {
            "extend" => await controller.ExtendSubscription(ids.SubscriptionId, new AdminSubscriptionExtendHttpRequest(30, "operator action"), CancellationToken.None),
            "block" => await controller.BlockSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            "unblock" => await controller.UnblockSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            "cancel" => await controller.CancelSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None),
            _ => throw new InvalidOperationException($"Unknown test command: {command}")
        };

        Assert.IsType<OkObjectResult>(result);
        db.ChangeTracker.Clear();
        var persistedSubscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var persistedAccess = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        Assert.Equal(expectedSubscriptionStatus, persistedSubscription.Status);
        Assert.Equal(expectedAccessStatus, persistedAccess.Status);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == $"subscription.{command}");
        var expectedAccessAudit = expectedAccessStatus switch
        {
            AccessCredentialStatus.Active => "access.enable",
            AccessCredentialStatus.Revoked => "access.revoke",
            _ => "access.disable"
        };
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == expectedAccessAudit);
        if (command == "extend") Assert.Equal(clock.UtcNow.AddDays(30), persistedSubscription.EndAt);
        if (command == "cancel")
        {
            Assert.Equal(clock.UtcNow, persistedSubscription.CancelledAt);
            Assert.Null(persistedSubscription.CurrentAccessId);
            Assert.Null(persistedSubscription.CurrentServerId);
            Assert.Equal(0, (await db.VpnNodes.SingleAsync()).UsedCapacity);
            Assert.Equal(1, provider.DeleteCalls);
        }
    }

    [Fact]
    public async Task Cancel_Subscription_Local_Save_Failure_Should_Roll_Back_And_Mark_Access_For_Reconciliation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new FailingSaveApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 12, 30, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        subscription.Status = SubscriptionStatus.Active;
        access.Status = AccessCredentialStatus.Active;
        access.DisabledAt = null;
        await db.SaveChangesAsync();
        var provider = new TrackingVpnProvider(clock.UtcNow)
        {
            AfterDelete = () => db.FailNextSave = true
        };
        var controller = CreateController(db, provider, clock);

        var result = await controller.CancelSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("operator action"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        db.ChangeTracker.Clear();
        var persistedSubscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var persistedAccess = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        Assert.Equal(SubscriptionStatus.Active, persistedSubscription.Status);
        Assert.Equal(ids.AccessId, persistedSubscription.CurrentAccessId);
        Assert.NotNull(persistedSubscription.CurrentServerId);
        Assert.Null(persistedSubscription.CancelledAt);
        Assert.Equal(AccessCredentialStatus.SyncRequired, persistedAccess.Status);
        Assert.Equal(1, (await db.VpnNodes.SingleAsync()).UsedCapacity);
        Assert.Equal(1, provider.DeleteCalls);
        Assert.Contains(await db.AccessCredentialHistories.ToListAsync(), x => x.EventType == "AccessRevokeUncertainOnSubscriptionCancel");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "access.revoke.failed");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "subscription.cancel.failed");
    }

    [Fact]
    public async Task Cancel_Subscription_Cancellation_After_Provider_Delete_Should_Roll_Back_And_Mark_Access_For_Reconciliation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.Include(x => x.CurrentAccess).SingleAsync();
        var access = subscription.CurrentAccess!;
        subscription.Status = SubscriptionStatus.Active;
        access.Status = AccessCredentialStatus.Active;
        access.DisabledAt = null;
        await db.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        var provider = new TrackingVpnProvider(clock.UtcNow)
        {
            AfterDelete = cancellation.Cancel
        };
        var controller = CreateController(db, provider, clock);

        await Assert.ThrowsAsync<OperationCanceledException>(() => controller.CancelSubscription(
            ids.SubscriptionId,
            new AdminAccessActionHttpRequest("operator action"),
            cancellation.Token));

        db.ChangeTracker.Clear();
        var persistedSubscription = await db.Subscriptions.SingleAsync();
        var persistedAccess = await db.AccessCredentials.SingleAsync();
        Assert.Equal(SubscriptionStatus.Active, persistedSubscription.Status);
        Assert.Equal(ids.AccessId, persistedSubscription.CurrentAccessId);
        Assert.NotNull(persistedSubscription.CurrentServerId);
        Assert.Null(persistedSubscription.CancelledAt);
        Assert.Equal(AccessCredentialStatus.SyncRequired, persistedAccess.Status);
        Assert.Equal(1, (await db.VpnNodes.SingleAsync()).UsedCapacity);
        Assert.Equal(1, provider.DeleteCalls);
        Assert.Contains(await db.AccessCredentialHistories.ToListAsync(), x => x.EventType == "AccessRevokeUncertainOnSubscriptionCancel");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "access.revoke.cancelled");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "subscription.cancel.failed");
    }

    [Fact]
    public async Task Cancel_Subscription_PreCancelled_Request_Should_Roll_Back_Without_Marking_Provider_State_Uncertain()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.Include(x => x.CurrentAccess).SingleAsync();
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentAccess!.Status = AccessCredentialStatus.Active;
        subscription.CurrentAccess.DisabledAt = null;
        await db.SaveChangesAsync();
        var provider = new TrackingVpnProvider(clock.UtcNow);
        var lifecycle = new VpnAccessLifecycleService(db, new TestVpnProviderFactory(provider), clock);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lifecycle.CancelSubscriptionAsync(
            subscription,
            "operator action",
            null,
            cancellation.Token));

        db.ChangeTracker.Clear();
        Assert.Equal(SubscriptionStatus.Active, (await db.Subscriptions.SingleAsync()).Status);
        Assert.Equal(AccessCredentialStatus.Active, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Equal(1, (await db.VpnNodes.SingleAsync()).UsedCapacity);
        Assert.Equal(0, provider.DeleteCalls);
        Assert.DoesNotContain(await db.AccessCredentialHistories.ToListAsync(), x => x.EventType == "AccessRevokeUncertainOnSubscriptionCancel");
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action == "access.revoke.cancelled");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "subscription.cancel.failed" && x.AfterJson.Contains("rolled_back_before_provider_mutation"));
    }

    [Fact]
    public async Task Repeated_Cancel_Should_Not_Delete_Access_Or_Release_Node_Twice()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var ids = await SeedSubscriptionWithDisabledAccessAsync(db, clock.UtcNow);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == ids.SubscriptionId);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == ids.AccessId);
        subscription.Status = SubscriptionStatus.Active;
        access.Status = AccessCredentialStatus.Active;
        access.DisabledAt = null;
        await db.SaveChangesAsync();
        var provider = new TrackingVpnProvider(clock.UtcNow);
        var controller = CreateController(db, provider, clock);

        Assert.IsType<OkObjectResult>(await controller.CancelSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("first"), CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.CancelSubscription(ids.SubscriptionId, new AdminAccessActionHttpRequest("second"), CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal(SubscriptionStatus.Cancelled, (await db.Subscriptions.SingleAsync()).Status);
        Assert.Equal(AccessCredentialStatus.Revoked, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Equal(0, (await db.VpnNodes.SingleAsync()).UsedCapacity);
        Assert.Equal(1, provider.DeleteCalls);
    }

    private static async Task<(Guid SubscriptionId, Guid AccessId)> SeedSubscriptionWithDisabledAccessAsync(ApplicationDbContext db, DateTimeOffset now)
    {
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var accessId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "client@example.test", DisplayName = "Client", Status = UserStatus.Active });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Premium", Slug = "premium", DurationDays = 30, Price = 490, Currency = "RUB" });
        db.VpnNodes.Add(new VpnNode { Id = nodeId, Name = "NL-1", Host = "nl1.example.test", IpAddress = "127.0.0.1", Capacity = 100, UsedCapacity = 1 });
        db.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            UserId = userId,
            TariffId = tariffId,
            Status = SubscriptionStatus.PendingActivation,
            StartAt = now.AddDays(-1),
            EndAt = now.AddDays(29),
            CurrentServerId = nodeId,
            BlockReason = "waiting_for_manual_activation",
            SuspendedAt = now.AddHours(-2)
        });
        db.AccessCredentials.Add(new AccessCredential
        {
            Id = accessId,
            SubscriptionId = subscriptionId,
            ServerId = nodeId,
            ProviderType = "x3ui",
            ProviderAccessId = "client-1",
            AccessUri = "vless://client-1",
            QrCodePath = "vless://client-1",
            ConfigPath = "/configs/client-1.json",
            Status = AccessCredentialStatus.Disabled,
            IssuedAt = now.AddDays(-1),
            DisabledAt = now.AddHours(-2),
            Revision = 1
        });
        await db.SaveChangesAsync();

        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == subscriptionId);
        subscription.CurrentAccessId = accessId;
        await db.SaveChangesAsync();

        return (subscriptionId, accessId);
    }

    private static AdminOperationsController CreateController(ApplicationDbContext db, TrackingVpnProvider provider, TestClock clock)
    {
        var secretProtector = new TestSecretProtector();
        var lifecycle = new VpnAccessLifecycleService(db, new TestVpnProviderFactory(provider), clock);
        var controller = new AdminOperationsController(
            db,
            provisioningService: null!,
            paymentOrchestrator: null!,
            paymentProviderAccounts: new PaymentProviderAccountService(db, secretProtector, clock),
            vpnAccessLifecycleService: lifecycle,
            secretProtector: secretProtector,
            vpnProviderFactory: new TestVpnProviderFactory(provider),
            clock: clock);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(0, value.Length - visibleTail)) + value[^Math.Min(visibleTail, value.Length)..];
    }

    private sealed class TestVpnProviderFactory(IVpnProvider provider) : IVpnProviderFactory
    {
        public IVpnProvider Get(string providerName) => provider;
    }

    private sealed class TrackingVpnProvider(DateTimeOffset now, bool failEnable = false, bool failDisable = false, bool failDelete = false) : IVpnProvider
    {
        public string Name => "x3ui";
        public int EnableCalls { get; private set; }
        public int DisableCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public int SyncCalls { get; private set; }
        public Action? AfterDelete { get; set; }

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult("client-1", "vless://client-1", "vless://client-1", "/configs/client-1.json"));

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult("client-1", "vless://client-1", "vless://client-1", "/configs/client-1.json"));

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            DisableCalls += 1;
            return failDisable
                ? Task.FromException(new InvalidOperationException("simulated provider disable failure"))
                : Task.CompletedTask;
        }

        public Task EnableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            EnableCalls += 1;
            return failEnable
                ? Task.FromException(new InvalidOperationException("simulated provider enable failure"))
                : Task.CompletedTask;
        }

        public Task<VpnUsageSnapshot> SyncAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            SyncCalls += 1;
            return Task.FromResult(new VpnUsageSnapshot(providerAccessId, 2048, 2, now));
        }

        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            DeleteCalls += 1;
            AfterDelete?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return failDelete
                ? Task.FromException(new InvalidOperationException("simulated provider delete failure"))
                : Task.CompletedTask;
        }
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 2048, 2, now));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }

    private sealed class FailingSaveApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        public bool FailNextSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new DbUpdateException("Injected cancellation persistence failure.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
