using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Vpn;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminOperationBoundaryTests
{
    [Theory]
    [InlineData("disable")]
    [InlineData("enable")]
    [InlineData("sync")]
    [InlineData("reset-traffic")]
    public async Task Access_Action_Without_Lifecycle_Service_Should_Fail_Closed_On_Sqlite(string action)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 12, 8, 0, 0, TimeSpan.Zero);
        var user = User(Guid.NewGuid());
        var tariff = new Tariff { Name = "Lifecycle boundary", Slug = $"lifecycle-{action}", DurationDays = 30, Price = 500m, Currency = "RUB", IsActive = true };
        var node = Node($"lifecycle-{action}", NodeStatus.Ready, true);
        var subscription = new Subscription
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Active,
            StartAt = now.AddDays(-1),
            EndAt = now.AddDays(29),
            CurrentServerId = node.Id
        };
        var access = new AccessCredential
        {
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderAccessId = $"provider-{action}",
            AccessUri = "vless://lifecycle-boundary",
            Status = AccessCredentialStatus.Active,
            Revision = 7,
            LastSyncedAt = now.AddMinutes(-15)
        };
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(subscription);
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid(), new FixedClock(now));
        var request = new AdminAccessActionHttpRequest("lifecycle unavailable");
        var result = action switch
        {
            "disable" => await controller.DisableAccessCredential(access.Id, request, CancellationToken.None),
            "enable" => await controller.EnableAccessCredential(access.Id, request, CancellationToken.None),
            "sync" => await controller.SyncAccessCredential(access.Id, request, CancellationToken.None),
            "reset-traffic" => await controller.ResetAccessTraffic(access.Id, request, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        db.ChangeTracker.Clear();
        var persisted = await db.AccessCredentials.SingleAsync(x => x.Id == access.Id);
        Assert.Equal(AccessCredentialStatus.Active, persisted.Status);
        Assert.Null(persisted.DisabledAt);
        Assert.Equal(7, persisted.Revision);
        Assert.Equal(now.AddMinutes(-15), persisted.LastSyncedAt);
        Assert.Empty(await db.AccessCredentialHistories.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Subscription_Migration_Should_Execute_And_Complete_Job()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 4, 7, 0, 0, TimeSpan.Zero);
        var adminId = Guid.NewGuid();
        var user = User(Guid.NewGuid());
        var tariff = new Tariff { Name = "Migration", Slug = "migration-boundary", DurationDays = 30, Price = 500m, Currency = "RUB", IsActive = true };
        var source = Node("source", NodeStatus.Ready, true);
        var target = Node("target", NodeStatus.Ready, true);
        var unavailableTarget = Node("archived", NodeStatus.Archived, false);
        var unhealthyTarget = Node("unhealthy", NodeStatus.Ready, true);
        unhealthyTarget.HealthStatus = HealthStatus.Unhealthy;
        var fullTarget = Node("full", NodeStatus.Ready, true);
        fullTarget.UsedCapacity = fullTarget.Capacity;
        source.PanelBaseUrl = "https://source-panel.example.test";
        source.UsedCapacity = 1;
        target.PanelBaseUrl = "https://target-panel.example.test";
        target.PanelInboundId = 2;
        unavailableTarget.PanelBaseUrl = "https://archived-panel.example.test";
        unhealthyTarget.PanelBaseUrl = "https://unhealthy-panel.example.test";
        fullTarget.PanelBaseUrl = "https://full-panel.example.test";
        var sourcePanel = new VpnPanel { Name = "source-panel", BaseUrl = source.PanelBaseUrl, Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, UsedCapacity = 1, Capacity = 100 };
        var targetPanel = new VpnPanel { Name = "target-panel", BaseUrl = target.PanelBaseUrl, Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 100 };
        var sourceInbound = new VpnInbound { VpnPanelId = sourcePanel.Id, ExternalInboundId = "1", Name = "source", Protocol = "vless", Port = 443, IsDefault = true, UsedCapacity = 1, Capacity = 100 };
        var targetInbound = new VpnInbound { VpnPanelId = targetPanel.Id, ExternalInboundId = "2", Name = "target", Protocol = "vless", Port = 8443, IsDefault = true, Capacity = 100 };
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.AddRange(source, target, unavailableTarget, unhealthyTarget, fullTarget);
        db.VpnPanels.AddRange(sourcePanel, targetPanel);
        db.VpnInbounds.AddRange(sourceInbound, targetInbound);
        await db.SaveChangesAsync();

        var subscription = new Subscription
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Active,
            StartAt = now.AddDays(-1),
            EndAt = now.AddDays(29),
            CurrentServerId = source.Id
        };
        var subscriptionWithoutSource = new Subscription
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.PendingActivation,
            StartAt = now,
            EndAt = now.AddDays(30)
        };
        db.Subscriptions.AddRange(subscription, subscriptionWithoutSource);
        await db.SaveChangesAsync();

        var access = new AccessCredential
        {
            SubscriptionId = subscription.Id,
            ServerId = source.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "migration-client",
            AccessUri = "vless://migration@example.test",
            Status = AccessCredentialStatus.Active,
            IssuedAt = now
        };
        db.AccessCredentials.Add(access);
        db.VpnClients.Add(new VpnClient
        {
            UserId = user.Id,
            SubscriptionId = subscription.Id,
            VpnPanelId = sourcePanel.Id,
            VpnInboundId = sourceInbound.Id,
            ExternalClientId = access.ProviderAccessId,
            Email = "migration@example.test",
            Uuid = Guid.NewGuid().ToString("D"),
            LimitIp = 2,
            ExpiryTime = subscription.EndAt,
            Enable = true,
            ConfigUri = access.AccessUri
        });
        await db.SaveChangesAsync();
        subscription.CurrentAccessId = access.Id;
        await db.SaveChangesAsync();

        var x3Ui = new X3UiPanelService(
            db,
            null!,
            null!,
            new FixedClock(now),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Vpn:X3Ui:Mode"] = "Sandbox" }).Build());
        var controller = CreateController(db, adminId, new FixedClock(now), x3UiPanelService: x3Ui);

        Assert.IsType<NotFoundObjectResult>(await controller.MigrateSubscription(Guid.NewGuid(), target.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscriptionWithoutSource.Id, target.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscription.Id, source.Id, CancellationToken.None));
        Assert.IsType<NotFoundObjectResult>(await controller.MigrateSubscription(subscription.Id, Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscription.Id, unavailableTarget.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscription.Id, unhealthyTarget.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscription.Id, fullTarget.Id, CancellationToken.None));
        Assert.Empty(await db.MigrationJobs.ToListAsync());

        subscription.Status = SubscriptionStatus.GracePeriod;
        subscription.EndAt = now.AddDays(-3);
        subscription.GracePeriodEndAt = now;
        await db.SaveChangesAsync();
        var expiredMigration = Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscription.Id, target.Id, CancellationToken.None));
        Assert.Contains("expired", JsonSerializer.Serialize(expiredMigration.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.MigrationJobs.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());

        subscription.Status = SubscriptionStatus.Active;
        subscription.EndAt = now.AddDays(29);
        subscription.GracePeriodEndAt = null;
        await db.SaveChangesAsync();

        var activeJob = new MigrationJob { SourceNodeId = source.Id, TargetNodeId = target.Id, Status = MigrationJobStatus.Running, Type = "single-subscription", RequestedAt = now };
        activeJob.Items.Add(new MigrationItem { SubscriptionId = subscription.Id, OldAccessId = access.Id, Status = MigrationJobStatus.Running });
        db.MigrationJobs.Add(activeJob);
        await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await controller.MigrateSubscription(subscription.Id, target.Id, CancellationToken.None));
        db.MigrationJobs.Remove(activeJob);
        await db.SaveChangesAsync();

        Assert.IsType<OkObjectResult>(await controller.MigrateSubscription(subscription.Id, target.Id, CancellationToken.None));

        var job = await db.MigrationJobs.Include(x => x.Items).SingleAsync();
        Assert.Equal(source.Id, job.SourceNodeId);
        Assert.Equal(target.Id, job.TargetNodeId);
        Assert.Equal(MigrationJobStatus.Completed, job.Status);
        Assert.Equal("single-subscription", job.Type);
        Assert.Equal(adminId, job.RequestedByUserId);
        Assert.Equal(now, job.RequestedAt);
        Assert.Equal(now, job.StartedAt);
        Assert.Equal(now, job.FinishedAt);
        var item = Assert.Single(job.Items);
        Assert.Equal(subscription.Id, item.SubscriptionId);
        Assert.Equal(access.Id, item.OldAccessId);
        Assert.Equal(access.Id, item.NewAccessId);
        Assert.Equal(MigrationJobStatus.Completed, item.Status);
        Assert.Equal(target.Id, (await db.Subscriptions.AsNoTracking().SingleAsync(x => x.Id == subscription.Id)).CurrentServerId);
        var migratedAccess = await db.AccessCredentials.AsNoTracking().SingleAsync(x => x.Id == access.Id);
        Assert.Equal(target.Id, migratedAccess.ServerId);
        Assert.Contains(":8443", migratedAccess.AccessUri);
        Assert.Equal(targetInbound.Id, (await db.VpnClients.AsNoTracking().SingleAsync()).VpnInboundId);
        Assert.Equal(0, (await db.VpnNodes.AsNoTracking().SingleAsync(x => x.Id == source.Id)).UsedCapacity);
        Assert.Equal(1, (await db.VpnNodes.AsNoTracking().SingleAsync(x => x.Id == target.Id)).UsedCapacity);
        var audits = await db.AuditLogs.AsNoTracking().ToListAsync();
        Assert.Contains(audits, x => x.Action == "subscription.migration.start" && x.EntityId == subscription.Id.ToString());
        Assert.Contains(audits, x => x.Action == "subscription.migration.complete" && x.EntityId == subscription.Id.ToString());
    }

    [Fact]
    public async Task Archived_Server_Mode_Actions_Should_Fail_Closed()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var archived = Node("archived", NodeStatus.Archived, false);
        db.VpnNodes.Add(archived);
        await db.SaveChangesAsync();
        var controller = CreateController(db, Guid.NewGuid(), new FixedClock(DateTimeOffset.UtcNow));

        Assert.IsType<ConflictObjectResult>(await controller.Maintenance(archived.Id, new ServerStateActionHttpRequest(archived.Revision), CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.DisableMaintenance(archived.Id, new ServerStateActionHttpRequest(archived.Revision), CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.DisableAllocation(archived.Id, new ServerStateActionHttpRequest(archived.Revision), CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.EnableAllocation(archived.Id, new ServerStateActionHttpRequest(archived.Revision), CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.DisableServer(archived.Id, new ServerStateActionHttpRequest(archived.Revision), CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.Precheck(archived.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.Provision(archived.Id, new QueueProvisionHttpRequest(DryRun: true), CancellationToken.None));

        await db.Entry(archived).ReloadAsync();
        Assert.Equal(NodeStatus.Archived, archived.Status);
        Assert.False(archived.IsAvailableForNewUsers);
        Assert.Empty(await db.ProvisioningRuns.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Active_Server_Mode_Actions_Should_Update_State_And_Audit_Each_Transition()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var node = Node("active", NodeStatus.Ready, true);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var controller = CreateController(db, Guid.NewGuid(), new FixedClock(new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero)));

        Assert.IsType<OkObjectResult>(await controller.Maintenance(node.Id, new ServerStateActionHttpRequest(node.Revision), CancellationToken.None));
        Assert.Equal(NodeStatus.Maintenance, node.Status);
        Assert.False(node.IsAvailableForNewUsers);

        Assert.IsType<OkObjectResult>(await controller.DisableMaintenance(node.Id, new ServerStateActionHttpRequest(node.Revision), CancellationToken.None));
        Assert.Equal(NodeStatus.Ready, node.Status);
        Assert.True(node.IsAvailableForNewUsers);

        Assert.IsType<OkObjectResult>(await controller.DisableAllocation(node.Id, new ServerStateActionHttpRequest(node.Revision), CancellationToken.None));
        Assert.Equal(NodeStatus.Draining, node.Status);
        Assert.False(node.IsAvailableForNewUsers);

        Assert.IsType<OkObjectResult>(await controller.EnableAllocation(node.Id, new ServerStateActionHttpRequest(node.Revision), CancellationToken.None));
        Assert.Equal(NodeStatus.Ready, node.Status);
        Assert.True(node.IsAvailableForNewUsers);

        Assert.IsType<OkObjectResult>(await controller.DisableServer(node.Id, new ServerStateActionHttpRequest(node.Revision), CancellationToken.None));
        Assert.Equal(NodeStatus.Disabled, node.Status);
        Assert.False(node.IsAvailableForNewUsers);

        Assert.Equal(5, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task Revoked_Access_Should_Reject_Admin_Qr_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero);
        var user = User(Guid.NewGuid());
        var tariff = new Tariff { Name = "Revoked", Slug = "revoked-admin-qr", DurationDays = 30, Price = 500m, Currency = "RUB", IsActive = true };
        var node = Node("revoked-qr", NodeStatus.Ready, true);
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var subscription = new Subscription
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Cancelled,
            StartAt = now.AddDays(-30),
            EndAt = now,
            CancelledAt = now
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var access = new AccessCredential
        {
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "revoked-admin-secret",
            AccessUri = "vless://revoked-admin-secret@example.test",
            Status = AccessCredentialStatus.Revoked,
            IssuedAt = now.AddDays(-30)
        };
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid(), new FixedClock(now), new SvgQrCodeGenerator(new FixedClock(now)));

        Assert.IsType<BadRequestObjectResult>(await controller.GetAccessCredentialQr(access.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Cancelled_Subscription_Should_Redact_Stale_Access_And_Reject_Admin_Operations_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 5, 8, 30, 0, TimeSpan.Zero);
        var user = User(Guid.NewGuid());
        var tariff = new Tariff { Name = "Cancelled", Slug = "cancelled-admin-boundary", DurationDays = 30, Price = 500m, Currency = "RUB", IsActive = true };
        var source = Node("cancelled-source", NodeStatus.Ready, true);
        var target = Node("cancelled-target", NodeStatus.Ready, true);
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.AddRange(source, target);
        await db.SaveChangesAsync();

        var accessId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Cancelled,
            StartAt = now.AddDays(-30),
            EndAt = now,
            CancelledAt = now,
            CurrentServerId = source.Id
        };
        var access = new AccessCredential
        {
            Id = accessId,
            SubscriptionId = subscription.Id,
            ServerId = source.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "cancelled-provider-secret",
            AccessUri = "vless://cancelled-admin-secret@example.test",
            QrCodePath = "vless://cancelled-admin-qr-secret@example.test",
            ConfigPath = "/configs/cancelled-admin-secret.json",
            Status = AccessCredentialStatus.Active,
            IssuedAt = now.AddDays(-30)
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();
        subscription.CurrentAccessId = accessId;
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            Guid.NewGuid(),
            new FixedClock(now),
            new SvgQrCodeGenerator(new FixedClock(now)),
            includeVpnLifecycleService: true);

        var accessList = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await controller.GetAccessCredentials(CancellationToken.None)).Value);
        Assert.Contains("Cancelled", accessList, StringComparison.Ordinal);
        Assert.DoesNotContain("cancelled-provider-secret", accessList, StringComparison.Ordinal);
        Assert.DoesNotContain("cancelled-admin-secret", accessList, StringComparison.Ordinal);
        Assert.IsType<BadRequestObjectResult>(await controller.GetAccessCredentialQr(access.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.EnableAccessCredential(access.Id, new AdminAccessActionHttpRequest("test"), CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.DisableAccessCredential(access.Id, new AdminAccessActionHttpRequest("test"), CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscription.Id, target.Id, CancellationToken.None));
        Assert.Empty(await db.MigrationJobs.ToListAsync());
        Assert.Empty(await db.AccessCredentialHistories.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Equal(AccessCredentialStatus.Active, (await db.AccessCredentials.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.GracePeriod)]
    public async Task Effective_Expiry_Should_Redact_Admin_Access_And_Reject_Qr_And_Enable_On_Sqlite(SubscriptionStatus status)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);
        var user = User(Guid.NewGuid());
        var tariff = new Tariff { Name = "Expired", Slug = $"expired-admin-boundary-{status}", DurationDays = 30, Price = 500m, Currency = "RUB", IsActive = true };
        var node = Node("expired-source", NodeStatus.Ready, true);
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var subscription = new Subscription
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = status,
            StartAt = now.AddDays(-33),
            EndAt = status == SubscriptionStatus.Active ? now.AddDays(10) : now.AddDays(-3),
            GracePeriodEndAt = now,
            CurrentServerId = node.Id
        };
        var access = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "expired-provider-secret",
            AccessUri = "vless://expired-admin-secret@example.test",
            QrCodePath = "vless://expired-admin-qr-secret@example.test",
            ConfigPath = "/configs/expired-admin-secret.json",
            Status = AccessCredentialStatus.Disabled,
            IssuedAt = now.AddDays(-30)
        };
        db.Subscriptions.Add(subscription);
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();
        subscription.CurrentAccessId = access.Id;
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            Guid.NewGuid(),
            new FixedClock(now),
            new SvgQrCodeGenerator(new FixedClock(now)),
            includeVpnLifecycleService: true);
        var accessList = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await controller.GetAccessCredentials(CancellationToken.None)).Value);

        Assert.Contains("\"IsTerminal\":true", accessList, StringComparison.Ordinal);
        Assert.Contains(JsonSerializer.Serialize(now).Trim('"'), accessList, StringComparison.Ordinal);
        Assert.DoesNotContain("expired-provider-secret", accessList, StringComparison.Ordinal);
        Assert.DoesNotContain("expired-admin-secret", accessList, StringComparison.Ordinal);
        Assert.IsType<BadRequestObjectResult>(await controller.GetAccessCredentialQr(access.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.EnableAccessCredential(access.Id, new AdminAccessActionHttpRequest("test"), CancellationToken.None));
        Assert.Empty(await db.AccessCredentialHistories.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Equal(AccessCredentialStatus.Disabled, (await db.AccessCredentials.SingleAsync()).Status);
    }

    private static AdminOperationsController CreateController(
        ApplicationDbContext db,
        Guid adminId,
        IClock clock,
        IQrCodeGenerator? qrCodeGenerator = null,
        X3UiPanelService? x3UiPanelService = null,
        bool includeVpnLifecycleService = false)
        => new(
            db,
            new ProvisioningService(db, clock),
            null!,
            null!,
            vpnAccessLifecycleService: includeVpnLifecycleService
                ? new VpnAccessLifecycleService(db, new UnexpectedVpnProviderFactory(), clock)
                : null,
            qrCodeGenerator: qrCodeGenerator,
            clock: clock,
            x3UiPanelService: x3UiPanelService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, adminId.ToString())],
                        "unit-test"))
                }
            }
        };

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private sealed class UnexpectedVpnProviderFactory : IVpnProviderFactory
    {
        public IVpnProvider Get(string providerName)
            => throw new InvalidOperationException($"VPN provider must not be called for guarded access: {providerName}.");
    }

    private static User User(Guid id)
        => new()
        {
            Id = id,
            Email = $"{id:N}@example.test",
            DisplayName = "Migration user",
            PasswordHash = "hash",
            ReferralCode = id.ToString("N")[..12],
            Status = UserStatus.Active
        };

    private static VpnNode Node(string suffix, NodeStatus status, bool available)
        => new()
        {
            Name = $"node-{suffix}",
            Host = $"{suffix}.example.test",
            IpAddress = "192.0.2.10",
            Provider = "x3ui",
            Region = "eu",
            Country = "NL",
            Status = status,
            HealthStatus = HealthStatus.Healthy,
            IsAvailableForNewUsers = available,
            Capacity = 100
        };

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
