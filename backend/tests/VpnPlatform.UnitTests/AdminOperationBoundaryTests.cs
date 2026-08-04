using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminOperationBoundaryTests
{
    [Fact]
    public async Task Subscription_Migration_Should_Validate_Entities_And_Create_Complete_Job()
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
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.AddRange(source, target, unavailableTarget);
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
        await db.SaveChangesAsync();
        subscription.CurrentAccessId = access.Id;
        await db.SaveChangesAsync();

        var controller = CreateController(db, adminId, new FixedClock(now));

        Assert.IsType<NotFoundObjectResult>(await controller.MigrateSubscription(Guid.NewGuid(), target.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscriptionWithoutSource.Id, target.Id, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscription.Id, source.Id, CancellationToken.None));
        Assert.IsType<NotFoundObjectResult>(await controller.MigrateSubscription(subscription.Id, Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.MigrateSubscription(subscription.Id, unavailableTarget.Id, CancellationToken.None));
        Assert.Empty(await db.MigrationJobs.ToListAsync());

        Assert.IsType<OkObjectResult>(await controller.MigrateSubscription(subscription.Id, target.Id, CancellationToken.None));

        var job = await db.MigrationJobs.Include(x => x.Items).SingleAsync();
        Assert.Equal(source.Id, job.SourceNodeId);
        Assert.Equal(target.Id, job.TargetNodeId);
        Assert.Equal(MigrationJobStatus.Planned, job.Status);
        Assert.Equal("single-subscription", job.Type);
        Assert.Equal(adminId, job.RequestedByUserId);
        Assert.Equal(now, job.RequestedAt);
        var item = Assert.Single(job.Items);
        Assert.Equal(subscription.Id, item.SubscriptionId);
        Assert.Equal(access.Id, item.OldAccessId);
        Assert.Equal(MigrationJobStatus.Planned, item.Status);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "subscription.migration.plan" && x.EntityId == subscription.Id.ToString());

        Assert.IsType<ConflictObjectResult>(await controller.MigrateSubscription(subscription.Id, target.Id, CancellationToken.None));
        Assert.Single(await db.MigrationJobs.ToListAsync());
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

        Assert.IsType<ConflictObjectResult>(await controller.Maintenance(archived.Id, CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.DisableMaintenance(archived.Id, CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.DisableAllocation(archived.Id, CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.EnableAllocation(archived.Id, CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.DisableServer(archived.Id, CancellationToken.None));
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

        Assert.IsType<OkObjectResult>(await controller.Maintenance(node.Id, CancellationToken.None));
        Assert.Equal(NodeStatus.Maintenance, node.Status);
        Assert.False(node.IsAvailableForNewUsers);

        Assert.IsType<OkObjectResult>(await controller.DisableMaintenance(node.Id, CancellationToken.None));
        Assert.Equal(NodeStatus.Ready, node.Status);
        Assert.True(node.IsAvailableForNewUsers);

        Assert.IsType<OkObjectResult>(await controller.DisableAllocation(node.Id, CancellationToken.None));
        Assert.Equal(NodeStatus.Draining, node.Status);
        Assert.False(node.IsAvailableForNewUsers);

        Assert.IsType<OkObjectResult>(await controller.EnableAllocation(node.Id, CancellationToken.None));
        Assert.Equal(NodeStatus.Ready, node.Status);
        Assert.True(node.IsAvailableForNewUsers);

        Assert.IsType<OkObjectResult>(await controller.DisableServer(node.Id, CancellationToken.None));
        Assert.Equal(NodeStatus.Disabled, node.Status);
        Assert.False(node.IsAvailableForNewUsers);

        Assert.Equal(5, await db.AuditLogs.CountAsync());
    }

    private static AdminOperationsController CreateController(ApplicationDbContext db, Guid adminId, IClock clock)
        => new(db, new ProvisioningService(db, clock), null!, null!, clock: clock)
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
