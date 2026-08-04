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

    private static async Task<(Guid SubscriptionId, Guid AccessId)> SeedSubscriptionWithDisabledAccessAsync(ApplicationDbContext db, DateTimeOffset now)
    {
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var accessId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "client@example.test", DisplayName = "Client", Status = UserStatus.Active });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Premium", Slug = "premium", DurationDays = 30, Price = 490, Currency = "RUB" });
        db.VpnNodes.Add(new VpnNode { Id = nodeId, Name = "NL-1", Host = "nl1.example.test", IpAddress = "127.0.0.1" });
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

    private sealed class TrackingVpnProvider(DateTimeOffset now) : IVpnProvider
    {
        public string Name => "x3ui";
        public int EnableCalls { get; private set; }
        public int SyncCalls { get; private set; }

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult("client-1", "vless://client-1", "vless://client-1", "/configs/client-1.json"));

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult("client-1", "vless://client-1", "vless://client-1", "/configs/client-1.json"));

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task EnableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            EnableCalls += 1;
            return Task.CompletedTask;
        }

        public Task<VpnUsageSnapshot> SyncAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            SyncCalls += 1;
            return Task.FromResult(new VpnUsageSnapshot(providerAccessId, 2048, 2, now));
        }

        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 2048, 2, now));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }
}
