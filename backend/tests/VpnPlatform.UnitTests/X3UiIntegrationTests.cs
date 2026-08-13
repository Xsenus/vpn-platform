using System.Security.Claims;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Vpn;
using Xunit;

namespace VpnPlatform.UnitTests;

public class X3UiIntegrationTests
{
    [Theory]
    [InlineData(typeof(VpnPanel))]
    [InlineData(typeof(VpnInbound))]
    [InlineData(typeof(VpnClient))]
    public void Managed_Vpn_Entity_Revision_Should_Be_A_Concurrency_Token(Type entityType)
    {
        using var db = CreateDbContext();

        var revision = db.Model.FindEntityType(entityType)?.FindProperty("Revision");

        Assert.NotNull(revision);
        Assert.True(revision.IsConcurrencyToken);
    }

    [Fact]
    public void Managed_Vpn_Text_Limits_Should_Override_The_Global_String_Convention()
    {
        using var db = CreateDbContext();
        var expected = new Dictionary<Type, IReadOnlyDictionary<string, int>>
        {
            [typeof(VpnPanel)] = new Dictionary<string, int> { [nameof(VpnPanel.Name)] = 200, [nameof(VpnPanel.BaseUrl)] = 2048, [nameof(VpnPanel.Region)] = 120, [nameof(VpnPanel.Login)] = 200, [nameof(VpnPanel.EncryptedPassword)] = 8192, [nameof(VpnPanel.DefaultInboundTemplateJson)] = 32768, [nameof(VpnPanel.LastError)] = 2000, [nameof(VpnPanel.Version)] = 120 },
            [typeof(VpnInbound)] = new Dictionary<string, int> { [nameof(VpnInbound.ExternalInboundId)] = 200, [nameof(VpnInbound.Name)] = 200, [nameof(VpnInbound.Protocol)] = 32, [nameof(VpnInbound.Listen)] = 255, [nameof(VpnInbound.SettingsJson)] = 32768, [nameof(VpnInbound.StreamSettingsJson)] = 32768, [nameof(VpnInbound.SniffingJson)] = 32768 },
            [typeof(VpnClient)] = new Dictionary<string, int> { [nameof(VpnClient.ExternalClientId)] = 200, [nameof(VpnClient.Email)] = 320, [nameof(VpnClient.Uuid)] = 100, [nameof(VpnClient.Flow)] = 100, [nameof(VpnClient.ConfigUri)] = 8192, [nameof(VpnClient.QrCodePayload)] = 8192, [nameof(VpnClient.SyncStatus)] = 100 }
        };

        foreach (var (entityType, properties) in expected)
        {
            var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(db.Model.FindEntityType(entityType));
            foreach (var (propertyName, maxLength) in properties)
            {
                Assert.Equal(maxLength, entity.FindProperty(propertyName)?.GetMaxLength());
            }
        }
    }

    [Fact]
    public async Task Panel_Update_Should_Reject_A_Stale_Revision_Without_Mutation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var panel = new VpnPanel
        {
            Name = "revision-panel",
            BaseUrl = "https://revision-panel.example.test",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100,
            Revision = 3
        };
        db.VpnPanels.Add(panel);
        await db.SaveChangesAsync();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.UpdatePanelAsync(panel.Id, new UpdateVpnPanelCommand(
            "stale-name", null, null, null, null, null, null, null, null, null, null, Revision: 2), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("changed", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("revision-panel", panel.Name);
        Assert.Equal(3, panel.Revision);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Inbound_Update_Should_Reject_A_Stale_Revision_Before_Provider_Mutation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var inbound = await db.VpnInbounds.SingleAsync(x => x.Id == ids.InboundId);
        inbound.Revision = 4;
        await db.SaveChangesAsync();
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.PatchInboundAsync(inbound.Id, NewInboundCommand(name: "stale-inbound") with { Revision = 3 }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("changed", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, remote.UpdateInboundCalls);
        Assert.Equal("vless", inbound.Name);
        Assert.Equal(4, inbound.Revision);
    }

    [Fact]
    public async Task Client_Action_Should_Reject_A_Stale_Revision_Before_Provider_Mutation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var client = await db.VpnClients.SingleAsync(x => x.Id == ids.ClientId);
        client.Enable = false;
        client.Revision = 5;
        await db.SaveChangesAsync();
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.EnableClientAsync(client.Id, expectedRevision: 4, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("changed", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, remote.UpdateClientCalls);
        Assert.False(client.Enable);
        Assert.Equal(5, client.Revision);
    }

    [Fact]
    public async Task Client_Migration_Should_Release_Reservation_When_Revision_Changes_During_Reservation()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new RevisionChangingSaveApplicationDbContext(options);
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var target = CreateMigrationInbound(ids.PanelId, "2", "migration-target", capacity: 10);
        db.VpnInbounds.Add(target);
        var client = await db.VpnClients.SingleAsync(x => x.Id == ids.ClientId);
        client.Revision = 6;
        await db.SaveChangesAsync();
        db.ChangeClientRevisionOnNextSave = client.Id;
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.MigrateClientAsync(client.Id, new MigrateVpnClientCommand(target.Id, Revision: 6), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("changed", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, remote.AddClientCalls);
        db.ChangeTracker.Clear();
        Assert.Equal(7, (await db.VpnClients.SingleAsync(x => x.Id == client.Id)).Revision);
        Assert.Equal(0, (await db.VpnPanels.SingleAsync(x => x.Id == ids.PanelId)).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync(x => x.Id == target.Id)).UsedCapacity);
    }

    [Fact]
    public async Task Panel_Diagnostics_Should_Be_Bounded_In_Sql_Before_Materialization()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new QueryCaptureInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var panel = new VpnPanel { Name = "bounded-panel", BaseUrl = "https://bounded-panel.example.test", Login = "admin", EncryptedPassword = "secret", Capacity = 100 };
        db.VpnPanels.Add(panel);
        db.PanelSyncRuns.AddRange(Enumerable.Range(0, 60).Select(index => new PanelSyncRun { VpnPanelId = panel.Id, StartedAt = DateTimeOffset.UtcNow.AddMinutes(-index) }));
        db.PanelHealthChecks.AddRange(Enumerable.Range(0, 60).Select(index => new PanelHealthCheck { VpnPanelId = panel.Id, CheckedAt = DateTimeOffset.UtcNow.AddMinutes(-index) }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();
        var service = new X3UiPanelService(db, new FakeX3UiClient(DateTimeOffset.UtcNow), new TestSecretProtector(), new FixedClock());

        Assert.Equal(50, (await service.GetSyncRunsAsync(panel.Id)).Count);
        Assert.Equal(50, (await service.GetHealthChecksAsync(panel.Id)).Count);

        Assert.Contains(interceptor.Commands, command => command.Contains("PanelSyncRuns", StringComparison.OrdinalIgnoreCase) && command.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command => command.Contains("PanelHealthChecks", StringComparison.OrdinalIgnoreCase) && command.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Panel_Diagnostics_Should_Order_Mixed_Offsets_By_Instant_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new QueryCaptureInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var panel = new VpnPanel { Name = "offset-panel", BaseUrl = "https://offset-panel.example.test", Login = "admin", EncryptedPassword = "secret", Capacity = 100 };
        var olderInstant = new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.FromHours(5));
        var newerInstant = new DateTimeOffset(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);
        var olderRun = new PanelSyncRun { VpnPanelId = panel.Id, StartedAt = olderInstant, CreatedAt = olderInstant, UpdatedAt = olderInstant };
        var newerRun = new PanelSyncRun { VpnPanelId = panel.Id, StartedAt = newerInstant, CreatedAt = newerInstant, UpdatedAt = newerInstant };
        var olderHealth = new PanelHealthCheck { VpnPanelId = panel.Id, CheckedAt = olderInstant, CreatedAt = olderInstant, UpdatedAt = olderInstant };
        var newerHealth = new PanelHealthCheck { VpnPanelId = panel.Id, CheckedAt = newerInstant, CreatedAt = newerInstant, UpdatedAt = newerInstant };
        var olderEvent = new PanelSyncEvent { PanelSyncRunId = newerRun.Id, EventType = "older", CreatedAt = olderInstant, UpdatedAt = olderInstant };
        var newerEvent = new PanelSyncEvent { PanelSyncRunId = newerRun.Id, EventType = "newer", CreatedAt = newerInstant, UpdatedAt = newerInstant };
        var user = new User { Email = "offset-client@example.test", DisplayName = "Offset client", PasswordHash = "hash", ReferralCode = "OFFSETCLIENT" };
        var tariff = new Tariff { Name = "Offset", Slug = "offset", DurationDays = 30, Price = 500m, Currency = "RUB", MaxDevices = 2 };
        var inbound = new VpnInbound { VpnPanelId = panel.Id, ExternalInboundId = "offset-inbound", Name = "Offset inbound", Port = 443 };
        var olderSubscription = new Subscription { UserId = user.Id, TariffId = tariff.Id, Status = SubscriptionStatus.Active, StartAt = olderInstant, EndAt = newerInstant.AddDays(30) };
        var newerSubscription = new Subscription { UserId = user.Id, TariffId = tariff.Id, Status = SubscriptionStatus.Active, StartAt = newerInstant, EndAt = newerInstant.AddDays(30) };
        var olderClient = new VpnClient
        {
            UserId = user.Id,
            SubscriptionId = olderSubscription.Id,
            VpnPanelId = panel.Id,
            VpnInboundId = inbound.Id,
            ExternalClientId = "offset-older",
            Email = "offset-older@example.test",
            Uuid = Guid.NewGuid().ToString(),
            ExpiryTime = newerInstant.AddDays(30),
            CreatedAt = olderInstant,
            UpdatedAt = olderInstant
        };
        var newerClient = new VpnClient
        {
            UserId = user.Id,
            SubscriptionId = newerSubscription.Id,
            VpnPanelId = panel.Id,
            VpnInboundId = inbound.Id,
            ExternalClientId = "offset-newer",
            Email = "offset-newer@example.test",
            Uuid = Guid.NewGuid().ToString(),
            ExpiryTime = newerInstant.AddDays(30),
            CreatedAt = newerInstant,
            UpdatedAt = newerInstant
        };
        db.AddRange(panel, olderRun, newerRun, olderHealth, newerHealth, olderEvent, newerEvent, user, tariff, inbound, olderSubscription, newerSubscription, olderClient, newerClient);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();
        var service = new X3UiPanelService(db, new FakeX3UiClient(DateTimeOffset.UtcNow), new TestSecretProtector(), new FixedClock());

        var runs = (await service.GetSyncRunsAsync(panel.Id)).ToList();
        var healthChecks = (await service.GetHealthChecksAsync(panel.Id)).ToList();
        var events = (await service.GetSyncEventsAsync(newerRun.Id)).ToList();
        var clients = (await service.GetClientsAsync(panel.Id)).ToList();

        Assert.Equal(newerRun.Id, runs[0].Id);
        Assert.Equal(newerHealth.Id, healthChecks[0].Id);
        Assert.Equal(olderEvent.Id, events[0].Id);
        Assert.Equal(newerClient.Id, clients[0].Id);
        Assert.Contains(interceptor.Commands, command => command.Contains("PanelSyncRuns", StringComparison.OrdinalIgnoreCase) && command.Contains("julianday", StringComparison.OrdinalIgnoreCase) && command.Contains("\"Id\" DESC", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command => command.Contains("PanelHealthChecks", StringComparison.OrdinalIgnoreCase) && command.Contains("julianday", StringComparison.OrdinalIgnoreCase) && command.Contains("\"Id\" DESC", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command => command.Contains("PanelSyncEvents", StringComparison.OrdinalIgnoreCase) && command.Contains("julianday", StringComparison.OrdinalIgnoreCase) && command.Contains("\"Id\"", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command => command.Contains("VpnClients", StringComparison.OrdinalIgnoreCase) && command.Contains("julianday", StringComparison.OrdinalIgnoreCase) && command.Contains("\"Id\" DESC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Vless_Config_Generator_Should_Use_Panel_And_Inbound_Data()
    {
        var panel = new VpnPanel { BaseUrl = "https://vpn.example.com:2053", Name = "EU" };
        var inbound = new VpnInbound
        {
            Protocol = "vless",
            Port = 443,
            StreamSettingsJson = "{\"network\":\"ws\",\"security\":\"tls\",\"tlsSettings\":{\"serverName\":\"vpn.example.com\"},\"wsSettings\":{\"path\":\"/ray\"}}"
        };
        var client = new VpnClient { SubscriptionId = Guid.NewGuid(), Uuid = "11111111-1111-1111-1111-111111111111", Email = "user@example.test", Flow = string.Empty };

        var uri = X3UiConfigUriGenerator.BuildUri(panel, inbound, client);

        Assert.StartsWith("vless://11111111-1111-1111-1111-111111111111@vpn.example.com:443", uri);
        Assert.Contains("type=ws", uri);
        Assert.Contains("security=tls", uri);
        Assert.Contains("path=%2Fray", uri);
    }

    [Fact]
    public void Trojan_Config_Generator_Should_Use_Panel_And_Inbound_Data()
    {
        var panel = new VpnPanel { BaseUrl = "https://vpn.example.com:2053", Name = "EU" };
        var inbound = new VpnInbound
        {
            Protocol = "trojan",
            Port = 443,
            StreamSettingsJson = "{\"network\":\"grpc\",\"security\":\"reality\",\"realitySettings\":{\"serverNames\":[\"vpn.example.com\"]},\"grpcSettings\":{\"serviceName\":\"vpn\"}}"
        };
        var client = new VpnClient { SubscriptionId = Guid.NewGuid(), Uuid = "trojan-password", Email = "user@example.test" };

        var uri = X3UiConfigUriGenerator.BuildUri(panel, inbound, client);

        Assert.StartsWith("trojan://trojan-password@vpn.example.com:443", uri);
        Assert.Contains("type=grpc", uri);
        Assert.Contains("security=reality", uri);
        Assert.Contains("sni=vpn.example.com", uri);
        Assert.Contains("serviceName=vpn", uri);
    }

    [Fact]
    public void Vmess_Config_Generator_Should_Use_Base64_Profile()
    {
        var panel = new VpnPanel { BaseUrl = "https://vpn.example.com:2053", Name = "EU" };
        var inbound = new VpnInbound
        {
            Protocol = "vmess",
            Port = 443,
            SettingsJson = "{\"clients\":[{\"security\":\"chacha20-poly1305\"}]}",
            StreamSettingsJson = "{\"network\":\"ws\",\"security\":\"tls\",\"tlsSettings\":{\"serverName\":\"vpn.example.com\"},\"wsSettings\":{\"path\":\"/vmess\",\"headers\":{\"Host\":\"cdn.example.com\"}}}"
        };
        var client = new VpnClient { SubscriptionId = Guid.NewGuid(), Uuid = "11111111-1111-1111-1111-111111111111", Email = "user@example.test" };

        var uri = X3UiConfigUriGenerator.BuildUri(panel, inbound, client);
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(uri["vmess://".Length..]));
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        Assert.StartsWith("vmess://", uri);
        Assert.Equal("2", root.GetProperty("v").GetString());
        Assert.Equal("vpn.example.com", root.GetProperty("add").GetString());
        Assert.Equal("443", root.GetProperty("port").GetString());
        Assert.Equal("11111111-1111-1111-1111-111111111111", root.GetProperty("id").GetString());
        Assert.Equal("ws", root.GetProperty("net").GetString());
        Assert.Equal("/vmess", root.GetProperty("path").GetString());
        Assert.Equal("cdn.example.com", root.GetProperty("host").GetString());
        Assert.Equal("tls", root.GetProperty("tls").GetString());
    }

    [Fact]
    public void Config_Generator_Should_Fail_When_Inbound_Is_Insufficient()
    {
        var uri = X3UiConfigUriGenerator.BuildUri(
            new VpnPanel { BaseUrl = "https://vpn.example.com" },
            new VpnInbound { Protocol = "shadowsocks", Port = 0 },
            new VpnClient { Uuid = "11111111-1111-1111-1111-111111111111" });

        Assert.Equal(string.Empty, uri);
    }

    [Theory]
    [InlineData("vless", "vless://11111111-1111-1111-1111-111111111111@[2001:db8::10]:443")]
    [InlineData("trojan", "trojan://11111111-1111-1111-1111-111111111111@[2001:db8::10]:443")]
    public void Config_Generator_Should_Bracket_Ipv6_Endpoint(string protocol, string expectedPrefix)
    {
        var uri = X3UiConfigUriGenerator.BuildUri(
            new VpnPanel { BaseUrl = "https://[2001:db8::10]:2053" },
            new VpnInbound { Protocol = protocol, Port = 443, StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}" },
            new VpnClient { SubscriptionId = Guid.NewGuid(), Uuid = "11111111-1111-1111-1111-111111111111" });

        Assert.StartsWith(expectedPrefix, uri, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Config_Generator_Should_Reject_Out_Of_Range_Port(int port)
    {
        var uri = X3UiConfigUriGenerator.BuildUri(
            new VpnPanel { BaseUrl = "https://vpn.example.test" },
            new VpnInbound { Protocol = "vless", Port = port },
            new VpnClient { Uuid = "11111111-1111-1111-1111-111111111111" });

        Assert.Empty(uri);
    }

    [Fact]
    public async Task Panel_Health_And_Sync_Should_Save_Health_And_Detect_Diffs()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, client, new TestSecretProtector(), clock);
        var controller = new AdminVpnPanelsController(service);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);

        var health = Assert.IsType<PanelHealthCheckDto>(Assert.IsType<OkObjectResult>(await controller.TestConnection(ids.PanelId, CancellationToken.None)).Value);
        Assert.IsType<OkObjectResult>(await controller.HealthCheck(ids.PanelId, CancellationToken.None));
        var sync = Assert.IsType<PanelSyncRunDto>(Assert.IsType<OkObjectResult>(await controller.Sync(ids.PanelId, CancellationToken.None)).Value);

        Assert.Equal("Healthy", health.Status);
        Assert.Equal("Succeeded", sync.Status);
        Assert.IsType<OkObjectResult>(await controller.GetSyncRuns(ids.PanelId, CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.GetSyncEvents(sync.Id, CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.GetHealthChecks(ids.PanelId, CancellationToken.None));
        Assert.Equal(HealthStatus.Healthy, (await db.VpnPanels.SingleAsync()).HealthStatus);
        Assert.True(await db.PanelHealthChecks.AnyAsync(x => x.Status == HealthStatus.Healthy));
        Assert.True(await db.PanelSyncEvents.AnyAsync(x => x.EventType == "orphan_client"));
        Assert.True(await db.PanelSyncEvents.AnyAsync(x => x.EventType == "expiry_mismatch"));
        Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.Action == "vpn_panel.health_check"));
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_panel.sync");
    }

    [Fact]
    public async Task Panel_Health_Should_Recover_One_Success_Record_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var panel = new VpnPanel
        {
            Name = "health-recovery-panel",
            BaseUrl = "https://health-recovery.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        };
        db.VpnPanels.Add(panel);
        await db.SaveChangesAsync();
        db.FailNextSave = true;
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.CheckHealthAsync(panel.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        db.ChangeTracker.Clear();
        var check = await db.PanelHealthChecks.SingleAsync();
        Assert.Equal(HealthStatus.Healthy, check.Status);
        Assert.Equal(HealthStatus.Healthy, (await db.VpnPanels.SingleAsync()).HealthStatus);
        Assert.Single(await db.AuditLogs.Where(x => x.Action == "vpn_panel.health_check.persistence_recovered").ToListAsync());
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_panel.health_check.failed");
    }

    [Fact]
    public async Task Panel_Health_Should_Not_Duplicate_Ambiguously_Committed_Result()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var panel = new VpnPanel
        {
            Name = "health-ambiguous-panel",
            BaseUrl = "https://health-ambiguous.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        };
        db.VpnPanels.Add(panel);
        await db.SaveChangesAsync();
        db.FailNextSaveAfterCommit = true;
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.CheckHealthAsync(panel.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        db.ChangeTracker.Clear();
        Assert.Single(await db.PanelHealthChecks.ToListAsync());
        Assert.Single(await db.AuditLogs.Where(x => x.Action == "vpn_panel.health_check").ToListAsync());
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_panel.health_check.persistence_recovered");
    }

    [Fact]
    public async Task Panel_Update_Should_Wait_For_Concurrent_Health_Check()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-panel-health-{Guid.NewGuid():N}.db");
        var healthStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHealth = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var options = SqliteOptions(databasePath);
            var clock = new FixedClock();
            var remote = new FakeX3UiClient(clock.UtcNow)
            {
                HealthCheckStarted = healthStarted,
                ReleaseHealthCheck = releaseHealth
            };
            Guid panelId;
            await using (var seedDb = new ApplicationDbContext(options))
            {
                await seedDb.Database.EnsureCreatedAsync();
                var panel = new VpnPanel { Name = "health-gate-panel", BaseUrl = "https://health-gate.example.test:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, Capacity = 100 };
                seedDb.VpnPanels.Add(panel);
                await seedDb.SaveChangesAsync();
                panelId = panel.Id;
            }

            await using var healthDb = new ApplicationDbContext(options);
            await using var updateDb = new ApplicationDbContext(options);
            var healthService = new X3UiPanelService(healthDb, remote, new TestSecretProtector(), clock, ProductionConfiguration());
            var updateService = new X3UiPanelService(updateDb, remote, new TestSecretProtector(), clock, ProductionConfiguration());
            var healthTask = healthService.CheckHealthAsync(panelId, CancellationToken.None);
            await healthStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var updateTask = updateService.UpdatePanelAsync(panelId, new UpdateVpnPanelCommand(
                Name: "updated-after-health",
                BaseUrl: null,
                Login: null,
                Password: null,
                Region: null,
                Capacity: null,
                SslVerificationMode: null,
                ApiVariant: null,
                AutoCreateInbound: null,
                DefaultInboundTemplateJson: null,
                Status: null), CancellationToken.None);
            await Task.Delay(100);

            Assert.False(updateTask.IsCompleted);
            releaseHealth.TrySetResult(true);
            Assert.True((await healthTask).IsSuccess);
            Assert.True((await updateTask).IsSuccess);

            await using var verifyDb = new ApplicationDbContext(options);
            var persisted = await verifyDb.VpnPanels.SingleAsync();
            Assert.Equal("updated-after-health", persisted.Name);
            Assert.Equal(HealthStatus.Healthy, persisted.HealthStatus);
            Assert.Equal(1, remote.HealthCheckCalls);
        }
        finally
        {
            releaseHealth.TrySetResult(true);
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Panel_Sync_Should_Close_Run_When_Panel_Is_Not_Configured()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var panel = new VpnPanel
        {
            Name = "not-configured",
            BaseUrl = "https://panel.example.test",
            Login = string.Empty,
            EncryptedPassword = string.Empty,
            Status = VpnPanelStatus.New
        };
        db.VpnPanels.Add(panel);
        await db.SaveChangesAsync();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.SyncPanelAsync(panel.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        var run = await db.PanelSyncRuns.SingleAsync(x => x.VpnPanelId == panel.Id);
        Assert.Equal(PanelSyncRunStatus.Failed, run.Status);
        Assert.NotNull(run.FinishedAt);
        Assert.DoesNotContain(await db.PanelSyncRuns.ToListAsync(), x => x.Status == PanelSyncRunStatus.Running);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_panel.sync.failed" && x.EntityId == panel.Id.ToString());
    }

    [Theory]
    [InlineData("ftp://panel.example.test", "secret", "Strict", "X3UiOfficial", "{}", "HTTP")]
    [InlineData("https://operator:secret@panel.example.test", "secret", "Strict", "X3UiOfficial", "{}", "credentials")]
    [InlineData("https://panel.example.test", "", "Strict", "X3UiOfficial", "{}", "Password")]
    [InlineData("https://panel.example.test", "secret", "unknown", "X3UiOfficial", "{}", "SSL")]
    [InlineData("https://panel.example.test", "secret", "Strict", "unknown", "{}", "API variant")]
    [InlineData("https://panel.example.test", "secret", "Strict", "X3UiOfficial", "[]", "JSON object")]
    public async Task Panel_Create_Should_Reject_Invalid_Configuration(
        string baseUrl,
        string password,
        string sslMode,
        string apiVariant,
        string templateJson,
        string expectedError)
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.CreatePanelAsync(new CreateVpnPanelCommand(
            "invalid-panel",
            baseUrl,
            "admin",
            password,
            "eu",
            100,
            sslMode,
            apiVariant,
            true,
            templateJson), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(expectedError, result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.VpnPanels.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Panel_Update_Should_Reject_Invalid_Configuration_Without_Mutation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var panel = new VpnPanel
        {
            Name = "original-panel",
            BaseUrl = "https://panel.example.test",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        };
        db.VpnPanels.Add(panel);
        await db.SaveChangesAsync();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.UpdatePanelAsync(panel.Id, new UpdateVpnPanelCommand(
            Name: "mutated-panel",
            BaseUrl: "not-a-url",
            Login: null,
            Password: null,
            Region: null,
            Capacity: 0,
            SslVerificationMode: "unknown",
            ApiVariant: "unknown",
            AutoCreateInbound: null,
            DefaultInboundTemplateJson: "[]",
            Status: "999"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("original-panel", panel.Name);
        Assert.Equal("https://panel.example.test", panel.BaseUrl);
        Assert.Equal(100, panel.Capacity);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Panel_Update_Should_Reject_Capacity_Below_Used_Slots()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var panel = new VpnPanel { Name = "used-panel", BaseUrl = "https://used-panel.example.test", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, Capacity = 10, UsedCapacity = 4 };
        db.VpnPanels.Add(panel);
        await db.SaveChangesAsync();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.UpdatePanelAsync(panel.Id, new UpdateVpnPanelCommand(
            Name: null,
            BaseUrl: null,
            Login: null,
            Password: null,
            Region: null,
            Capacity: 3,
            SslVerificationMode: null,
            ApiVariant: null,
            AutoCreateInbound: null,
            DefaultInboundTemplateJson: null,
            Status: null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("used capacity", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(10, panel.Capacity);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Panel_Create_And_Update_Should_Reject_Duplicate_Identity_Without_Mutation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var existing = new VpnPanel
        {
            Name = "existing-panel",
            BaseUrl = "https://existing.example.test",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        };
        var updated = new VpnPanel
        {
            Name = "updated-panel",
            BaseUrl = "https://updated.example.test",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        };
        db.VpnPanels.AddRange(existing, updated);
        await db.SaveChangesAsync();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock, ProductionConfiguration());

        var create = await service.CreatePanelAsync(new CreateVpnPanelCommand(
            "existing-panel",
            "https://new.example.test",
            "admin",
            "secret",
            "eu",
            100,
            "Strict",
            "X3UiOfficial",
            false,
            "{}"), CancellationToken.None);
        var update = await service.UpdatePanelAsync(updated.Id, new UpdateVpnPanelCommand(
            Name: null,
            BaseUrl: "https://existing.example.test/",
            Login: null,
            Password: null,
            Region: null,
            Capacity: null,
            SslVerificationMode: null,
            ApiVariant: null,
            AutoCreateInbound: null,
            DefaultInboundTemplateJson: null,
            Status: null), CancellationToken.None);

        Assert.False(create.IsSuccess);
        Assert.False(update.IsSuccess);
        Assert.Equal("https://updated.example.test", updated.BaseUrl);
        Assert.Equal(2, await db.VpnPanels.CountAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Panel_Sync_Cancellation_Should_Finalize_Run_And_Rethrow()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        using var cancellation = new CancellationTokenSource();
        var remote = new FakeX3UiClient(clock.UtcNow) { CancelGetInboundsWith = cancellation };
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SyncPanelAsync(ids.PanelId, cancellation.Token));

        db.ChangeTracker.Clear();
        var run = await db.PanelSyncRuns.SingleAsync(x => x.VpnPanelId == ids.PanelId);
        Assert.Equal(PanelSyncRunStatus.Failed, run.Status);
        Assert.NotNull(run.FinishedAt);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_panel.sync.cancelled" && x.EntityId == ids.PanelId.ToString());
    }

    [Fact]
    public async Task Panel_Sync_Cancellation_After_First_Inbound_Should_Not_Save_Partial_State()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        using var cancellation = new CancellationTokenSource();
        var remote = new FakeX3UiClient(clock.UtcNow)
        {
            Inbounds = new FailAfterFirstOnSecondEnumerationCollection(
                [AdditionalInbound(), ChangedInbound()],
                () =>
                {
                    cancellation.Cancel();
                    throw new OperationCanceledException(cancellation.Token);
                })
        };
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SyncPanelAsync(ids.PanelId, cancellation.Token));

        db.ChangeTracker.Clear();
        var inbound = await db.VpnInbounds.SingleAsync(x => x.VpnPanelId == ids.PanelId);
        Assert.Equal("vless", inbound.Name);
        Assert.Equal(443, inbound.Port);
        Assert.Empty(await db.PanelSyncEvents.ToListAsync());
        var run = await db.PanelSyncRuns.SingleAsync(x => x.VpnPanelId == ids.PanelId);
        Assert.Equal(PanelSyncRunStatus.Failed, run.Status);
    }

    [Fact]
    public async Task Panel_Sync_Failure_After_First_Inbound_Should_Not_Save_Partial_State()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var remote = new FakeX3UiClient(clock.UtcNow)
        {
            Inbounds = new FailAfterFirstOnSecondEnumerationCollection(
                [ChangedInbound(), AdditionalInbound()],
                () => throw new InvalidOperationException("mid-sync failure"))
        };
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.SyncPanelAsync(ids.PanelId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        db.ChangeTracker.Clear();
        var inbound = await db.VpnInbounds.SingleAsync(x => x.VpnPanelId == ids.PanelId);
        Assert.Equal("vless", inbound.Name);
        Assert.Equal(443, inbound.Port);
        Assert.Empty(await db.PanelSyncEvents.ToListAsync());
        var run = await db.PanelSyncRuns.SingleAsync(x => x.VpnPanelId == ids.PanelId);
        Assert.Equal(PanelSyncRunStatus.Failed, run.Status);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_panel.sync.failed");
    }

    [Fact]
    public async Task Panel_Update_Should_Edit_Settings_And_Preserve_Empty_Password()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock);
        var adminId = Guid.NewGuid();
        var controller = CreateAdminController(service, adminId);

        var created = Assert.IsType<VpnPanelDto>(Assert.IsType<OkObjectResult>(await controller.CreatePanel(new CreateVpnPanelCommand(
            "main-panel",
            "https://panel.example.test:2053/",
            "admin",
            "initial-secret",
            "eu",
            100,
            "Strict",
            "X3UiOfficial",
            false,
            "{}"), CancellationToken.None)).Value);

        var panel = await db.VpnPanels.SingleAsync();
        Assert.Equal(panel.Id, created.Id);
        Assert.IsType<OkObjectResult>(await controller.GetPanels(CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.GetPanel(panel.Id, CancellationToken.None));
        var originalPassword = panel.EncryptedPassword;

        var updated = Assert.IsType<VpnPanelDto>(Assert.IsType<OkObjectResult>(await controller.UpdatePanel(panel.Id, new UpdateVpnPanelCommand(
            Name: "edited-panel",
            BaseUrl: "https://edited-panel.example.test:2053/",
            Login: "root-admin",
            Password: "",
            Region: "us",
            Capacity: 250,
            SslVerificationMode: "AllowSelfSigned",
            ApiVariant: "ThreeXUi",
            AutoCreateInbound: true,
            DefaultInboundTemplateJson: "{\"remark\":\"auto-vless\"}",
            Status: "Active",
            Revision: created.Revision), CancellationToken.None)).Value);

        Assert.Equal("edited-panel", panel.Name);
        Assert.Equal("https://edited-panel.example.test:2053", panel.BaseUrl);
        Assert.Equal("root-admin", panel.Login);
        Assert.Equal("us", panel.Region);
        Assert.Equal(250, panel.Capacity);
        Assert.Equal(VpnSslVerificationMode.AllowSelfSigned, panel.SslVerificationMode);
        Assert.Equal(X3UiApiVariant.ThreeXUi, panel.ApiVariant);
        Assert.True(panel.AutoCreateInbound);
        Assert.Equal("{\"remark\":\"auto-vless\"}", panel.DefaultInboundTemplateJson);
        Assert.Equal(VpnPanelStatus.Active, panel.Status);
        Assert.Equal(originalPassword, panel.EncryptedPassword);
        Assert.Equal("edited-panel", updated.Name);
        var audits = await db.AuditLogs.ToListAsync();
        Assert.Contains(audits, x => x.Action == "vpn_panel.create" && x.ActorId == adminId.ToString());
        Assert.Contains(audits, x => x.Action == "vpn_panel.update" && x.ActorId == adminId.ToString() && x.BeforeJson != x.AfterJson);
        Assert.All(audits, x => Assert.DoesNotContain("initial-secret", $"{x.BeforeJson}{x.AfterJson}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Panel_Delete_Should_Remove_Unused_Panel()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock);
        var controller = new AdminVpnPanelsController(service);
        var panelId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel
        {
            Id = panelId,
            Name = "unused-panel",
            BaseUrl = "https://unused-panel.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.New,
            Capacity = 100
        });
        await db.SaveChangesAsync();

        var result = Assert.IsType<DeleteVpnPanelResultDto>(Assert.IsType<OkObjectResult>(await controller.DeletePanel(panelId, CancellationToken.None, revision: 0)).Value);

        Assert.True(result.Deleted);
        Assert.False(result.Archived);
        Assert.False(await db.VpnPanels.AnyAsync(x => x.Id == panelId));
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_panel.delete" && x.EntityId == panelId.ToString());
    }

    [Fact]
    public async Task Panel_Delete_Should_Disable_Panel_When_Operational_Data_Is_Linked()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock);
        var panelId = Guid.NewGuid();
        var inboundId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel
        {
            Id = panelId,
            Name = "linked-panel",
            BaseUrl = "https://linked-panel.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 100
        });
        db.VpnInbounds.Add(new VpnInbound
        {
            Id = inboundId,
            VpnPanelId = panelId,
            ExternalInboundId = "1",
            Name = "default-vless",
            Protocol = "vless",
            Port = 443,
            IsDefault = true,
            IsActive = true,
            Capacity = 100
        });
        db.VpnClients.Add(new VpnClient
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            VpnPanelId = panelId,
            VpnInboundId = inboundId,
            ExternalClientId = "client-1",
            Email = "user@example.test",
            Uuid = Guid.NewGuid().ToString("D"),
            ExpiryTime = clock.UtcNow.AddDays(30),
            Enable = true
        });
        db.PanelSyncRuns.Add(new PanelSyncRun { Id = Guid.NewGuid(), VpnPanelId = panelId, Status = PanelSyncRunStatus.Succeeded, StartedAt = clock.UtcNow });
        db.PanelHealthChecks.Add(new PanelHealthCheck { Id = Guid.NewGuid(), VpnPanelId = panelId, Status = HealthStatus.Healthy, CheckedAt = clock.UtcNow });
        await db.SaveChangesAsync();

        var result = await service.DeletePanelAsync(panelId, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value!.Deleted);
        Assert.True(result.Value.Archived);
        Assert.Equal(1, result.Value.LinkedInbounds);
        Assert.Equal(1, result.Value.LinkedClients);
        Assert.Equal(1, result.Value.LinkedSyncRuns);
        Assert.Equal(1, result.Value.LinkedHealthChecks);
        var panel = await db.VpnPanels.SingleAsync(x => x.Id == panelId);
        Assert.Equal(VpnPanelStatus.Disabled, panel.Status);
        Assert.Equal(HealthStatus.Unknown, panel.HealthStatus);
        Assert.Contains("disabled", panel.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_panel.archive" && x.EntityId == panelId.ToString());
    }

    [Theory]
    [InlineData("shadowsocks", "{\"network\":\"tcp\",\"security\":\"tls\"}", true, 443, "protocol")]
    [InlineData("vless", "{}", true, 443, "network")]
    [InlineData("vless", "{\"network\":\"tcp\",\"security\":\"tls\"}", false, 0, "port")]
    [InlineData("vless", "{\"network\":\"tcp\",\"security\":\"tls\"}", false, 443, "default")]
    public async Task Inbound_Create_Should_Validate_Protocol_Stream_Active_Default_And_Port(string protocol, string streamSettingsJson, bool isActive, int port, string expectedError)
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock);
        var panelId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel
        {
            Id = panelId,
            Name = "panel",
            BaseUrl = "https://panel.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        });
        await db.SaveChangesAsync();

        var result = await service.CreateInboundAsync(panelId, NewInboundCommand(
            protocol: protocol,
            port: port,
            streamSettingsJson: streamSettingsJson,
            isDefault: !isActive,
            isActive: isActive), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(expectedError, result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(await db.VpnInbounds.AnyAsync());
    }

    [Fact]
    public async Task Inbound_Create_Should_Delete_Remote_Copy_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());
        var panelId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel
        {
            Id = panelId,
            Name = "panel",
            BaseUrl = "https://panel.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        });
        await db.SaveChangesAsync();
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateInboundAsync(panelId, NewInboundCommand(), CancellationToken.None));

        Assert.Equal(new[] { "1" }, remote.DeletedInboundIds);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.VpnInbounds.ToListAsync());
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "vpn_inbound.create.failed");
        Assert.Contains("true", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inbound_Create_Should_Record_Manual_Cleanup_When_Remote_Compensation_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        remote.FailingInboundDeleteIds.Add("1");
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());
        var panelId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel { Id = panelId, Name = "panel", BaseUrl = "https://panel.example.test:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, Capacity = 100 });
        await db.SaveChangesAsync();
        db.FailNextSave = true;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateInboundAsync(panelId, NewInboundCommand(), CancellationToken.None));

        Assert.Contains("manual provider cleanup", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { "1" }, remote.DeletedInboundIds);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.VpnInbounds.ToListAsync());
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "vpn_inbound.create.compensation_failed");
        Assert.Contains("false", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inbound_Update_Should_Restore_Remote_State_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PatchInboundAsync(
            ids.InboundId,
            NewInboundCommand(name: "changed-vless", port: 8443, capacity: 250),
            CancellationToken.None));

        Assert.Equal(2, remote.UpdateInboundCalls);
        Assert.Equal("changed-vless", remote.InboundUpdateRequests[0].Remark);
        Assert.Equal("vless", remote.InboundUpdateRequests[1].Remark);
        Assert.Equal(443, remote.InboundUpdateRequests[1].Port);
        db.ChangeTracker.Clear();
        var persisted = await db.VpnInbounds.SingleAsync();
        Assert.Equal("vless", persisted.Name);
        Assert.Equal(443, persisted.Port);
        Assert.Equal(100, persisted.Capacity);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "vpn_inbound.update.failed");
        Assert.Contains("local_persistence_failed", audit.AfterJson, StringComparison.Ordinal);
        Assert.Contains("true", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inbound_Update_Should_Reject_Capacity_Below_Used_Slots_Before_Remote_Call()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var inbound = await db.VpnInbounds.SingleAsync(x => x.Id == ids.InboundId);
        inbound.Capacity = 10;
        inbound.UsedCapacity = 4;
        await db.SaveChangesAsync();

        var result = await service.PatchInboundAsync(
            ids.InboundId,
            NewInboundCommand(name: inbound.Name, capacity: 3),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("used capacity", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, remote.UpdateInboundCalls);
        Assert.Equal(10, inbound.Capacity);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Inbound_Update_Should_Compensate_Ambiguous_Remote_Failure()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        remote.FailingUpdateInboundCalls.Add(1);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);

        await Assert.ThrowsAsync<TimeoutException>(() => service.PatchInboundAsync(
            ids.InboundId,
            NewInboundCommand(name: "changed-vless", port: 8443),
            CancellationToken.None));

        Assert.Equal(2, remote.UpdateInboundCalls);
        Assert.Equal("changed-vless", remote.InboundUpdateRequests[0].Remark);
        Assert.Equal("vless", remote.InboundUpdateRequests[1].Remark);
        db.ChangeTracker.Clear();
        Assert.Equal("vless", (await db.VpnInbounds.SingleAsync()).Name);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "vpn_inbound.update.failed");
        Assert.Contains("remote_operation_failed", audit.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inbound_Update_Should_Record_Manual_Reconciliation_When_Compensation_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        remote.FailingUpdateInboundCalls.Add(2);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        db.FailNextSave = true;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PatchInboundAsync(
            ids.InboundId,
            NewInboundCommand(name: "changed-vless", port: 8443),
            CancellationToken.None));

        Assert.Contains("manual provider reconciliation", error.Message, StringComparison.OrdinalIgnoreCase);
        db.ChangeTracker.Clear();
        Assert.Equal("vless", (await db.VpnInbounds.SingleAsync()).Name);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "vpn_inbound.update.compensation_failed");
        Assert.Contains("reconciliationRequired", audit.AfterJson, StringComparison.Ordinal);
        Assert.Contains("true", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inbound_Update_Should_Serialize_Concurrent_Changes_Per_Panel()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-inbound-update-{Guid.NewGuid():N}.db");
        var updateStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseUpdate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var options = SqliteOptions(databasePath);
            var clock = new FixedClock();
            var remote = new FakeX3UiClient(clock.UtcNow)
            {
                UpdateInboundStarted = updateStarted,
                ReleaseUpdateInbound = releaseUpdate
            };
            Guid inboundId;
            await using (var seedDb = new ApplicationDbContext(options))
            {
                await seedDb.Database.EnsureCreatedAsync();
                var panel = new VpnPanel { Id = Guid.NewGuid(), Name = "panel", BaseUrl = "https://vpn.example.com:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 100 };
                var inbound = new VpnInbound { Id = Guid.NewGuid(), VpnPanelId = panel.Id, ExternalInboundId = "1", Name = "vless", Protocol = "vless", Port = 443, StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}", SettingsJson = "{\"clients\":[]}", IsDefault = true, IsActive = true, Capacity = 100 };
                seedDb.VpnPanels.Add(panel);
                seedDb.VpnInbounds.Add(inbound);
                await seedDb.SaveChangesAsync();
                inboundId = inbound.Id;
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var firstService = new X3UiPanelService(firstDb, remote, new TestSecretProtector(), clock, ProductionConfiguration());
            var secondService = new X3UiPanelService(secondDb, remote, new TestSecretProtector(), clock, ProductionConfiguration());

            var first = firstService.PatchInboundAsync(inboundId, NewInboundCommand(name: "first-edit", port: 8443), CancellationToken.None);
            await updateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var second = secondService.PatchInboundAsync(inboundId, NewInboundCommand(name: "second-edit", port: 9443), CancellationToken.None);
            await Task.Delay(100);

            Assert.False(second.IsCompleted);
            releaseUpdate.TrySetResult(true);
            Assert.True((await first).IsSuccess);
            Assert.True((await second).IsSuccess);

            Assert.Equal(new[] { "first-edit", "second-edit" }, remote.InboundUpdateRequests.Select(x => x.Remark));
            await using var verifyDb = new ApplicationDbContext(options);
            var persisted = await verifyDb.VpnInbounds.SingleAsync();
            Assert.Equal("second-edit", persisted.Name);
            Assert.Equal(9443, persisted.Port);
        }
        finally
        {
            releaseUpdate.TrySetResult(true);
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Inbound_Management_Should_Create_Edit_Toggle_And_Protect_Inactive_Default()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, client, new TestSecretProtector(), clock);
        var controller = new AdminVpnPanelsController(service);
        var panelId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel
        {
            Id = panelId,
            Name = "panel",
            BaseUrl = "https://panel.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        });
        var otherPanelId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel
        {
            Id = otherPanelId,
            Name = "other-panel",
            BaseUrl = "https://other-panel.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "us",
            Status = VpnPanelStatus.Active,
            Capacity = 100
        });
        db.VpnInbounds.Add(new VpnInbound
        {
            VpnPanelId = otherPanelId,
            ExternalInboundId = "10",
            Name = "other-vless",
            Protocol = "vless",
            Port = 9443,
            IsActive = true,
            Capacity = 100
        });
        await db.SaveChangesAsync();

        var first = Assert.IsType<VpnInboundDto>(Assert.IsType<OkObjectResult>(await controller.CreateInbound(panelId, NewInboundCommand(name: "main-vless", isDefault: true), CancellationToken.None)).Value);
        var second = Assert.IsType<VpnInboundDto>(Assert.IsType<OkObjectResult>(await controller.CreateInbound(panelId, NewInboundCommand(name: "backup-vmess", protocol: "VMESS", port: 8443, isDefault: true), CancellationToken.None)).Value);

        Assert.Equal(2, client.CreateInboundCalls);
        Assert.False((await db.VpnInbounds.SingleAsync(x => x.Id == first.Id)).IsDefault);
        var secondInbound = await db.VpnInbounds.SingleAsync(x => x.Id == second.Id);
        Assert.True(secondInbound.IsDefault);
        Assert.Equal("vmess", secondInbound.Protocol);
        var panelInbounds = Assert.IsAssignableFrom<IReadOnlyCollection<VpnInboundDto>>(Assert.IsType<OkObjectResult>(await controller.GetInbounds(panelId, CancellationToken.None)).Value);
        var allInbounds = Assert.IsAssignableFrom<IReadOnlyCollection<VpnInboundDto>>(Assert.IsType<OkObjectResult>(await controller.GetAllInbounds(CancellationToken.None)).Value);
        Assert.Equal(2, panelInbounds.Count);
        Assert.Equal(3, allInbounds.Count);
        Assert.Contains(allInbounds, x => x.VpnPanelId == otherPanelId && x.Name == "other-vless");

        var disabled = Assert.IsType<VpnInboundDto>(Assert.IsType<OkObjectResult>(await controller.PatchInbound(secondInbound.Id, NewInboundCommand(
            name: "backup-disabled",
            protocol: "vmess",
            port: 8443,
            isDefault: false,
            isActive: false) with
        { Revision = second.Revision }, CancellationToken.None)).Value);

        Assert.False(disabled.IsActive);
        Assert.False(disabled.IsDefault);
        Assert.False((await db.VpnInbounds.SingleAsync(x => x.Id == secondInbound.Id)).IsDefault);

        var defaultResult = await controller.SetDefaultInbound(secondInbound.Id, CancellationToken.None, disabled.Revision);

        Assert.IsType<BadRequestObjectResult>(defaultResult);
        var audits = await db.AuditLogs.ToListAsync();
        Assert.Equal(2, audits.Count(x => x.Action == "vpn_inbound.create"));
        Assert.Single(audits, x => x.Action == "vpn_inbound.update");
        Assert.DoesNotContain(audits, x => x.Action == "vpn_inbound.default.set");
    }

    [Fact]
    public async Task Client_Management_Should_Enable_Disable_Sync_Reset_And_Migrate()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());
        var controller = new AdminVpnPanelsController(service);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var targetInboundId = Guid.NewGuid();
        db.VpnInbounds.Add(new VpnInbound
        {
            Id = targetInboundId,
            VpnPanelId = ids.PanelId,
            ExternalInboundId = "2",
            Name = "backup-vless",
            Protocol = "vless",
            Port = 8443,
            StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}",
            SettingsJson = "{\"clients\":[]}",
            IsDefault = false,
            IsActive = true,
            Capacity = 100
        });
        db.AccessCredentials.Add(new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = ids.SubscriptionId,
            ProviderType = "x3ui",
            ProviderAccessId = "client-1",
            ServerId = Guid.NewGuid(),
            AccessUri = "vless://old",
            Status = AccessCredentialStatus.Active
        });
        await db.SaveChangesAsync();

        Assert.IsType<OkObjectResult>(await controller.GetClients(ids.PanelId, CancellationToken.None));
        var disabled = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.DisableClient(ids.ClientId, CancellationToken.None, revision: 0)).Value);
        var enabled = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.EnableClient(ids.ClientId, CancellationToken.None, disabled.Revision)).Value);
        var synced = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.SyncClient(ids.ClientId, CancellationToken.None, enabled.Revision)).Value);
        var reset = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.ResetClientTraffic(ids.ClientId, CancellationToken.None, synced.Revision)).Value);
        var migrated = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.MigrateClient(ids.ClientId, new MigrateVpnClientCommand(targetInboundId, Revision: reset.Revision), CancellationToken.None)).Value);

        Assert.False(disabled.Enable);
        Assert.True(enabled.Enable);
        Assert.Equal("synced", synced.SyncStatus);
        Assert.Equal("traffic-reset", reset.SyncStatus);
        Assert.Equal(targetInboundId, migrated.VpnInboundId);
        Assert.Contains(":8443", migrated.ConfigUri);
        Assert.Equal(2, remote.UpdateClientCalls);
        Assert.Equal(1, remote.GetTrafficCalls);
        Assert.Equal(1, remote.ResetTrafficCalls);
        Assert.Equal(1, remote.AddClientCalls);
        Assert.Equal(1, remote.DeleteClientCalls);
        var access = await db.AccessCredentials.SingleAsync();
        Assert.Equal(migrated.ConfigUri, access.AccessUri);
        Assert.Equal(AccessCredentialStatus.Active, access.Status);
        var audits = await db.AuditLogs.ToListAsync();
        Assert.Contains(audits, x => x.Action == "vpn_client.disable");
        Assert.Contains(audits, x => x.Action == "vpn_client.enable");
        Assert.Contains(audits, x => x.Action == "vpn_client.sync");
        Assert.Contains(audits, x => x.Action == "vpn_client.traffic.reset");
        Assert.Contains(audits, x => x.Action == "vpn_client.migrate");
        Assert.All(audits, x => Assert.DoesNotContain(migrated.ConfigUri, $"{x.BeforeJson}{x.AfterJson}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Admin_Client_Disable_Should_Restore_Remote_State_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        db.AccessCredentials.Add(new AccessCredential
        {
            SubscriptionId = ids.SubscriptionId,
            ProviderAccessId = "client-1",
            ServerId = Guid.NewGuid(),
            AccessUri = "vless://old",
            Status = AccessCredentialStatus.Active
        });
        await db.SaveChangesAsync();
        db.FailNextSave = true;
        var remote = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DisableClientAsync(ids.ClientId, CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal(2, remote.UpdateClientCalls);
        Assert.False(remote.UpdateRequests[0].Enable);
        Assert.True(remote.UpdateRequests[1].Enable);
        Assert.True((await db.VpnClients.SingleAsync()).Enable);
        Assert.Equal(AccessCredentialStatus.Active, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.disable.failed" && x.AfterJson.Contains("compensated"));
    }

    [Fact]
    public async Task Admin_Client_Enable_Should_Roll_Back_Ambiguous_Remote_Update()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var persisted = await db.VpnClients.SingleAsync();
        persisted.Enable = false;
        persisted.SyncStatus = "disabled";
        await db.SaveChangesAsync();
        var remote = new FakeX3UiClient(clock.UtcNow);
        remote.FailingUpdateClientCalls.Add(1);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<TimeoutException>(() => service.EnableClientAsync(ids.ClientId, CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal(2, remote.UpdateClientCalls);
        Assert.True(remote.UpdateRequests[0].Enable);
        Assert.False(remote.UpdateRequests[1].Enable);
        Assert.False((await db.VpnClients.SingleAsync()).Enable);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.enable.failed" && x.AfterJson.Contains("compensated"));
    }

    [Fact]
    public async Task Admin_Client_Disable_Should_Mark_Reconciliation_When_Remote_Rollback_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        db.AccessCredentials.Add(new AccessCredential
        {
            SubscriptionId = ids.SubscriptionId,
            ProviderAccessId = "client-1",
            ServerId = Guid.NewGuid(),
            AccessUri = "vless://old",
            Status = AccessCredentialStatus.Active
        });
        await db.SaveChangesAsync();
        db.FailNextSave = true;
        var remote = new FakeX3UiClient(clock.UtcNow);
        remote.FailingUpdateClientCalls.Add(2);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DisableClientAsync(ids.ClientId, CancellationToken.None));

        Assert.Contains("manual provider reconciliation", error.Message, StringComparison.OrdinalIgnoreCase);
        db.ChangeTracker.Clear();
        Assert.Equal("client-state-compensation-failed", (await db.VpnClients.SingleAsync()).SyncStatus);
        Assert.Equal(AccessCredentialStatus.SyncRequired, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.disable.compensation_failed");
    }

    [Fact]
    public async Task Admin_Client_Traffic_Reset_Should_Persist_Uncertainty_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        db.AccessCredentials.Add(new AccessCredential
        {
            SubscriptionId = ids.SubscriptionId,
            ProviderAccessId = "client-1",
            ServerId = Guid.NewGuid(),
            AccessUri = "vless://old",
            Status = AccessCredentialStatus.Active
        });
        await db.SaveChangesAsync();
        db.FailNextSave = true;
        var remote = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResetClientTrafficAsync(ids.ClientId, CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal(1, remote.ResetTrafficCalls);
        Assert.Equal("traffic-reset-uncertain", (await db.VpnClients.SingleAsync()).SyncStatus);
        Assert.Equal(AccessCredentialStatus.SyncRequired, (await db.AccessCredentials.SingleAsync()).Status);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.traffic.reset.uncertain");
    }

    [Fact]
    public async Task Admin_Client_Traffic_Reset_Should_Persist_Uncertainty_After_Ambiguous_Remote_Failure()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var remote = new FakeX3UiClient(clock.UtcNow) { FailResetTrafficAfterSideEffect = true };
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<TimeoutException>(() => service.ResetClientTrafficAsync(ids.ClientId, CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal("traffic-reset-uncertain", (await db.VpnClients.SingleAsync()).SyncStatus);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.traffic.reset.uncertain" && x.AfterJson.Contains("remote_operation_failed"));
    }

    [Fact]
    public async Task Admin_Client_Traffic_Reset_Cancellation_After_Remote_Side_Effect_Should_Persist_Uncertainty()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        using var cancellation = new CancellationTokenSource();
        var remote = new FakeX3UiClient(clock.UtcNow) { AfterResetTraffic = cancellation.Cancel };
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ResetClientTrafficAsync(ids.ClientId, cancellation.Token));

        db.ChangeTracker.Clear();
        Assert.Equal(1, remote.ResetTrafficCalls);
        Assert.Equal("traffic-reset-uncertain", (await db.VpnClients.SingleAsync()).SyncStatus);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.traffic.reset.uncertain" && x.AfterJson.Contains("remote_operation_failed"));
    }

    [Fact]
    public async Task Real_Vpn_Provider_Traffic_Reset_Should_Mark_Reconciliation_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        db.AccessCredentials.Add(new AccessCredential
        {
            SubscriptionId = ids.SubscriptionId,
            ProviderAccessId = "client-1",
            ServerId = Guid.NewGuid(),
            AccessUri = "vless://old",
            Status = AccessCredentialStatus.Active
        });
        await db.SaveChangesAsync();
        db.FailNextSave = true;
        var remote = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ResetTrafficAsync("client-1", CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal("traffic-reset-uncertain", (await db.VpnClients.SingleAsync()).SyncStatus);
        Assert.Equal(AccessCredentialStatus.SyncRequired, (await db.AccessCredentials.SingleAsync()).Status);
    }

    [Fact]
    public async Task Client_Migration_Should_Reject_Full_Target_Inbound_Before_Remote_Mutation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var sourcePanel = await db.VpnPanels.SingleAsync();
        var sourceInbound = await db.VpnInbounds.SingleAsync();
        sourcePanel.UsedCapacity = 1;
        sourceInbound.UsedCapacity = 1;
        var target = CreateMigrationInbound(ids.PanelId, "2", "full-target", capacity: 1, usedCapacity: 1);
        db.VpnInbounds.Add(target);
        await db.SaveChangesAsync();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.MigrateClientAsync(ids.ClientId, new MigrateVpnClientCommand(target.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("capacity", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, remote.AddClientCalls);
        Assert.Equal(ids.InboundId, (await db.VpnClients.SingleAsync()).VpnInboundId);
        Assert.Equal(1, (await db.VpnInbounds.SingleAsync(x => x.Id == target.Id)).UsedCapacity);
    }

    [Fact]
    public async Task Client_Migration_Should_Reject_Full_Target_Panel_Before_Remote_Mutation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var sourcePanel = await db.VpnPanels.SingleAsync();
        var sourceInbound = await db.VpnInbounds.SingleAsync();
        sourcePanel.UsedCapacity = 1;
        sourceInbound.UsedCapacity = 1;
        var targetPanel = new VpnPanel
        {
            Name = "full-panel",
            BaseUrl = "https://full-panel.example.test:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 1,
            UsedCapacity = 1
        };
        var target = CreateMigrationInbound(targetPanel.Id, "2", "target-on-full-panel", capacity: 10);
        db.VpnPanels.Add(targetPanel);
        db.VpnInbounds.Add(target);
        await db.SaveChangesAsync();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        var result = await service.MigrateClientAsync(ids.ClientId, new MigrateVpnClientCommand(target.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("capacity", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, remote.AddClientCalls);
        Assert.Equal(ids.InboundId, (await db.VpnClients.SingleAsync()).VpnInboundId);
        Assert.Equal(1, (await db.VpnPanels.SingleAsync(x => x.Id == targetPanel.Id)).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync(x => x.Id == target.Id)).UsedCapacity);
    }

    [Fact]
    public async Task Concurrent_Client_Migrations_Should_Reserve_Last_Target_Inbound_Once()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-migration-capacity-{Guid.NewGuid():N}.db");
        try
        {
            var options = SqliteOptions(databasePath);
            var clock = new FixedClock();
            var seed = await SeedRelationalMigrationAsync(options, clock.UtcNow);
            var addStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseAdd = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var remote = new FakeX3UiClient(clock.UtcNow) { AddClientStarted = addStarted, ReleaseAddClient = releaseAdd };
            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var firstService = new X3UiPanelService(firstDb, remote, new TestSecretProtector(), clock, ProductionConfiguration());
            var secondService = new X3UiPanelService(secondDb, remote, new TestSecretProtector(), clock, ProductionConfiguration());

            var first = Task.Run(() => CaptureMigrationAsync(() => firstService.MigrateClientAsync(seed.ClientIds[0], new MigrateVpnClientCommand(seed.TargetInboundId), CancellationToken.None)));
            await addStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var second = Task.Run(() => CaptureMigrationAsync(() => secondService.MigrateClientAsync(seed.ClientIds[1], new MigrateVpnClientCommand(seed.TargetInboundId), CancellationToken.None)));
            await Task.Delay(250);
            var secondCompletedBeforeRelease = second.IsCompleted;
            releaseAdd.TrySetResult(true);
            var attempts = await Task.WhenAll(first, second);

            Assert.True(secondCompletedBeforeRelease);
            Assert.Equal(1, attempts.Count(x => x.Result?.IsSuccess == true));
            Assert.Equal(1, attempts.Count(x => x.Result?.IsSuccess == false));
            Assert.DoesNotContain(attempts, x => x.Error is not null);
            Assert.Equal(1, remote.AddClientCalls);
            Assert.Equal(1, remote.DeleteClientCalls);
            await using var verify = new ApplicationDbContext(options);
            Assert.Equal(1, (await verify.VpnInbounds.SingleAsync(x => x.Id == seed.TargetInboundId)).UsedCapacity);
            Assert.Equal(2, (await verify.VpnPanels.SingleAsync()).UsedCapacity);
            Assert.Equal(1, await verify.VpnInbounds.CountAsync(x => x.Id != seed.TargetInboundId && x.UsedCapacity == 1));
            Assert.Equal(1, await verify.VpnInbounds.CountAsync(x => x.Id != seed.TargetInboundId && x.UsedCapacity == 0));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Client_Migration_Should_Restore_Remote_Source_And_Capacity_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var panel = await db.VpnPanels.SingleAsync();
        var sourceInbound = await db.VpnInbounds.SingleAsync();
        panel.UsedCapacity = 1;
        sourceInbound.UsedCapacity = 1;
        var target = CreateMigrationInbound(ids.PanelId, "2", "target-vless", capacity: 1);
        db.VpnInbounds.Add(target);
        await db.SaveChangesAsync();
        var remote = new FakeX3UiClient(clock.UtcNow)
        {
            AfterDeleteClient = inboundId =>
            {
                if (inboundId == "1") db.FailNextSave = true;
            }
        };
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateClientAsync(ids.ClientId, new MigrateVpnClientCommand(target.Id), CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal(ids.InboundId, (await db.VpnClients.SingleAsync()).VpnInboundId);
        Assert.Equal(1, (await db.VpnPanels.SingleAsync()).UsedCapacity);
        Assert.Equal(1, (await db.VpnInbounds.SingleAsync(x => x.Id == ids.InboundId)).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync(x => x.Id == target.Id)).UsedCapacity);
        Assert.Equal(2, remote.AddClientCalls);
        Assert.Equal(2, remote.DeleteClientCalls);
        Assert.Equal(new[] { "1", "2" }, remote.DeleteInboundIds);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.migrate.failed" && x.AfterJson.Contains("remote_rolled_back"));
    }

    [Fact]
    public async Task Client_Migration_Should_Remove_Target_Copy_When_Source_Delete_Fails()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var targetInboundId = Guid.NewGuid();
        db.VpnInbounds.Add(new VpnInbound
        {
            Id = targetInboundId,
            VpnPanelId = ids.PanelId,
            ExternalInboundId = "2",
            Name = "target-vless",
            Protocol = "vless",
            Port = 8443,
            StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}",
            SettingsJson = "{\"clients\":[]}",
            IsActive = true,
            Capacity = 100
        });
        await db.SaveChangesAsync();
        var remote = new FakeX3UiClient(clock.UtcNow);
        remote.FailingDeleteInboundIds.Add("1");
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateClientAsync(ids.ClientId, new MigrateVpnClientCommand(targetInboundId), CancellationToken.None));

        db.ChangeTracker.Clear();
        var client = await db.VpnClients.SingleAsync(x => x.Id == ids.ClientId);
        Assert.Equal(ids.InboundId, client.VpnInboundId);
        Assert.Equal(2, remote.AddClientCalls);
        Assert.Equal(new[] { "1", "2" }, remote.DeleteInboundIds);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "vpn_client.migrate.failed");
        Assert.Contains("compensated", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("true", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Client_Migration_Should_Record_Manual_Cleanup_When_Compensation_Fails()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var targetInboundId = Guid.NewGuid();
        db.VpnInbounds.Add(new VpnInbound
        {
            Id = targetInboundId,
            VpnPanelId = ids.PanelId,
            ExternalInboundId = "2",
            Name = "target-vless",
            Protocol = "vless",
            Port = 8443,
            StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}",
            SettingsJson = "{\"clients\":[]}",
            IsActive = true,
            Capacity = 100
        });
        await db.SaveChangesAsync();
        var remote = new FakeX3UiClient(clock.UtcNow);
        remote.FailingDeleteInboundIds.UnionWith(["1", "2"]);
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateClientAsync(ids.ClientId, new MigrateVpnClientCommand(targetInboundId), CancellationToken.None));

        Assert.Contains("manual provider cleanup", error.Message, StringComparison.OrdinalIgnoreCase);
        db.ChangeTracker.Clear();
        var client = await db.VpnClients.SingleAsync(x => x.Id == ids.ClientId);
        Assert.Equal(ids.InboundId, client.VpnInboundId);
        Assert.Equal("migration-compensation-failed", client.SyncStatus);
        Assert.Equal(new[] { "1", "2" }, remote.DeleteInboundIds);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "vpn_client.migrate.compensation_failed");
        Assert.Contains("false", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Client_Migration_Should_Remove_Ambiguous_Target_Copy_And_Release_Reservation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var panel = await db.VpnPanels.SingleAsync();
        var source = await db.VpnInbounds.SingleAsync();
        panel.UsedCapacity = 1;
        source.UsedCapacity = 1;
        var target = CreateMigrationInbound(ids.PanelId, "2", "ambiguous-target", capacity: 1);
        db.VpnInbounds.Add(target);
        await db.SaveChangesAsync();
        var remote = new FakeX3UiClient(clock.UtcNow) { FailAddClientAfterSideEffect = true };
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<TimeoutException>(() => service.MigrateClientAsync(ids.ClientId, new MigrateVpnClientCommand(target.Id), CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.Equal(ids.InboundId, (await db.VpnClients.SingleAsync()).VpnInboundId);
        Assert.Equal(1, (await db.VpnPanels.SingleAsync()).UsedCapacity);
        Assert.Equal(1, (await db.VpnInbounds.SingleAsync(x => x.Id == ids.InboundId)).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync(x => x.Id == target.Id)).UsedCapacity);
        Assert.Equal(1, remote.AddClientCalls);
        Assert.Equal(new[] { "2" }, remote.DeleteInboundIds);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.migrate.failed" && x.AfterJson.Contains("target_removed"));
    }

    [Fact]
    public async Task Client_Migration_Cancellation_After_Source_Delete_Should_Restore_Source_And_Release_Reservation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var panel = await db.VpnPanels.SingleAsync();
        var source = await db.VpnInbounds.SingleAsync();
        panel.UsedCapacity = 1;
        source.UsedCapacity = 1;
        var target = CreateMigrationInbound(ids.PanelId, "2", "cancel-target", capacity: 1);
        db.VpnInbounds.Add(target);
        await db.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        var remote = new FakeX3UiClient(clock.UtcNow)
        {
            AfterDeleteClient = inboundId =>
            {
                if (inboundId == "1") cancellation.Cancel();
            }
        };
        var service = new X3UiPanelService(db, remote, new TestSecretProtector(), clock, ProductionConfiguration());

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.MigrateClientAsync(ids.ClientId, new MigrateVpnClientCommand(target.Id), cancellation.Token));

        db.ChangeTracker.Clear();
        Assert.Equal(ids.InboundId, (await db.VpnClients.SingleAsync()).VpnInboundId);
        Assert.Equal(1, (await db.VpnPanels.SingleAsync()).UsedCapacity);
        Assert.Equal(1, (await db.VpnInbounds.SingleAsync(x => x.Id == ids.InboundId)).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync(x => x.Id == target.Id)).UsedCapacity);
        Assert.Equal(2, remote.AddClientCalls);
        Assert.Equal(new[] { "1", "2" }, remote.DeleteInboundIds);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_client.migrate.failed" && x.AfterJson.Contains("source_restored_and_target_removed"));
    }

    [Fact]
    public async Task Real_Vpn_Provider_Should_Auto_Create_Inbound_And_Client()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow) { ReturnNoInbounds = true };
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, client, new TestSecretProtector(), clock);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        db.TelegramAccounts.Add(new TelegramAccount { TelegramUserId = 555555, UserId = userId, LinkedAt = clock.UtcNow });
        db.VpnPanels.Add(new VpnPanel
        {
            Id = Guid.NewGuid(),
            Name = "prod-panel",
            BaseUrl = "https://vpn.example.com:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 100,
            AutoCreateInbound = true,
            DefaultInboundTemplateJson = "{\"remark\":\"auto-vless\",\"protocol\":\"vless\",\"port\":443,\"settings\":{\"clients\":[]},\"streamSettings\":{\"network\":\"tcp\",\"security\":\"tls\"},\"sniffing\":{}}"
        });
        await db.SaveChangesAsync();

        var access = await provider.CreateAccessAsync(new VpnProvisionRequest(subscriptionId, userId, tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None);

        Assert.Contains("vless://", access.AccessUri);
        Assert.Equal(1, await db.VpnInbounds.CountAsync());
        Assert.Equal(1, await db.VpnClients.CountAsync());
        Assert.Equal(1, await db.TelegramBotNotifications.CountAsync(x => x.Type == "vpn_access_ready"));
        Assert.Equal(1, client.CreateInboundCalls);
        Assert.Equal(1, client.AddClientCalls);
    }

    [Fact]
    public async Task Real_Vpn_Provider_Should_Delete_Auto_Created_Inbound_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow) { ReturnNoInbounds = true };
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
        var tariffId = Guid.NewGuid();
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly-auto-failure", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        db.VpnPanels.Add(new VpnPanel
        {
            Id = Guid.NewGuid(),
            Name = "prod-panel",
            BaseUrl = "https://vpn.example.com:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 100,
            AutoCreateInbound = true,
            DefaultInboundTemplateJson = "{\"remark\":\"auto-vless\",\"protocol\":\"vless\",\"port\":443,\"settings\":{\"clients\":[]},\"streamSettings\":{\"network\":\"tcp\",\"security\":\"tls\"},\"sniffing\":{}}"
        });
        await db.SaveChangesAsync();
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAccessAsync(new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None));

        Assert.Equal(new[] { "1" }, remote.DeletedInboundIds);
        Assert.Equal(0, remote.AddClientCalls);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.VpnInbounds.ToListAsync());
    }

    [Fact]
    public async Task Real_Vpn_Provider_Should_Delete_New_Remote_Client_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
        var panelId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly-client-failure", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        db.VpnPanels.Add(new VpnPanel { Id = panelId, Name = "prod-panel", BaseUrl = "https://vpn.example.com:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 100 });
        db.VpnInbounds.Add(new VpnInbound { Id = Guid.NewGuid(), VpnPanelId = panelId, ExternalInboundId = "1", Name = "vless", Protocol = "vless", Port = 443, SettingsJson = "{\"clients\":[]}", StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}", IsDefault = true, IsActive = true, Capacity = 100 });
        await db.SaveChangesAsync();
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAccessAsync(new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None));

        Assert.Equal(1, remote.AddClientCalls);
        Assert.Equal(new[] { "1" }, remote.DeleteInboundIds);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.VpnClients.ToListAsync());
        Assert.Equal(0, (await db.VpnPanels.SingleAsync()).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync()).UsedCapacity);
    }

    [Fact]
    public async Task Real_Vpn_Provider_Should_Require_Manual_Cleanup_When_Client_Compensation_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        remote.FailingDeleteInboundIds.Add("1");
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
        var panelId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly-client-compensation-failure", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        db.VpnPanels.Add(new VpnPanel { Id = panelId, Name = "prod-panel", BaseUrl = "https://vpn.example.com:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 100 });
        db.VpnInbounds.Add(new VpnInbound { Id = Guid.NewGuid(), VpnPanelId = panelId, ExternalInboundId = "1", Name = "vless", Protocol = "vless", Port = 443, SettingsJson = "{\"clients\":[]}", StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}", IsDefault = true, IsActive = true, Capacity = 100 });
        await db.SaveChangesAsync();
        db.FailNextSave = true;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAccessAsync(new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None));

        Assert.Contains("manual provider cleanup", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { "1" }, remote.DeleteInboundIds);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.VpnClients.ToListAsync());
        Assert.Equal(0, (await db.VpnPanels.SingleAsync()).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync()).UsedCapacity);
    }

    [Fact]
    public async Task Real_Vpn_Provider_Should_Cleanup_Ambiguous_Remote_Create_Failure()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow) { FailAddClientAfterSideEffect = true };
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
        var panelId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly-ambiguous-create", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        db.VpnPanels.Add(new VpnPanel { Id = panelId, Name = "prod-panel", BaseUrl = "https://vpn.example.com:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 100 });
        db.VpnInbounds.Add(new VpnInbound { Id = Guid.NewGuid(), VpnPanelId = panelId, ExternalInboundId = "1", Name = "vless", Protocol = "vless", Port = 443, SettingsJson = "{\"clients\":[]}", StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}", IsDefault = true, IsActive = true, Capacity = 100 });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<TimeoutException>(() => provider.CreateAccessAsync(new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None));

        Assert.Equal(1, remote.AddClientCalls);
        Assert.Equal(1, remote.DeleteClientCalls);
        Assert.Empty(await db.VpnClients.ToListAsync());
        Assert.Equal(0, (await db.VpnPanels.SingleAsync()).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync()).UsedCapacity);
    }

    [Fact]
    public async Task Renewal_Should_Roll_Back_Remote_Client_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var originalExpiry = (await db.VpnClients.SingleAsync()).ExpiryTime;
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.UpdateAccessAsync(new VpnProvisionRequest(ids.SubscriptionId, ids.UserId, ids.TariffId, Guid.NewGuid(), clock.UtcNow.AddDays(60), 5), CancellationToken.None));

        Assert.Equal(2, remote.UpdateClientCalls);
        var rollback = remote.UpdateRequests[1];
        Assert.Equal("1", rollback.InboundId);
        Assert.Equal("client-1", rollback.ClientId);
        Assert.Equal(originalExpiry, rollback.ExpiryTime);
        db.ChangeTracker.Clear();
        Assert.Equal(originalExpiry, (await db.VpnClients.SingleAsync()).ExpiryTime);
    }

    [Fact]
    public async Task Delete_Should_Restore_Remote_Client_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        (await db.VpnPanels.SingleAsync()).UsedCapacity = 1;
        (await db.VpnInbounds.SingleAsync()).UsedCapacity = 1;
        await db.SaveChangesAsync();
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.DeleteAccessAsync(ids.ClientId.ToString(), CancellationToken.None));

        Assert.Equal(1, remote.DeleteClientCalls);
        Assert.Equal(1, remote.AddClientCalls);
        db.ChangeTracker.Clear();
        Assert.Single(await db.VpnClients.ToListAsync());
        Assert.Equal(1, (await db.VpnPanels.SingleAsync()).UsedCapacity);
        Assert.Equal(1, (await db.VpnInbounds.SingleAsync()).UsedCapacity);
    }

    [Fact]
    public async Task Disable_Should_Reenable_Remote_Client_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.DisableAccessAsync(ids.ClientId.ToString(), CancellationToken.None));

        Assert.Equal(1, remote.DisableClientCalls);
        Assert.Equal(1, remote.EnableClientCalls);
        db.ChangeTracker.Clear();
        Assert.True((await db.VpnClients.SingleAsync()).Enable);
    }

    [Fact]
    public async Task Enable_Should_Redisable_Remote_Client_When_Local_Save_Fails()
    {
        await using var db = CreateFailingDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var existing = await db.VpnClients.SingleAsync();
        existing.Enable = false;
        existing.SyncStatus = "disabled";
        await db.SaveChangesAsync();
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.EnableAccessAsync(ids.ClientId.ToString(), CancellationToken.None));

        Assert.Equal(1, remote.EnableClientCalls);
        Assert.Equal(1, remote.DisableClientCalls);
        db.ChangeTracker.Clear();
        Assert.False((await db.VpnClients.SingleAsync()).Enable);
    }

    [Theory]
    [InlineData("trojan", "trojan://")]
    [InlineData("vmess", "vmess://")]
    public async Task Real_Vpn_Provider_Should_Create_Config_For_Scenario_Protocol(string protocol, string expectedPrefix)
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, client, new TestSecretProtector(), clock);
        var panelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = $"monthly-{protocol}", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        db.VpnPanels.Add(new VpnPanel { Id = panelId, Name = "prod-panel", BaseUrl = "https://vpn.example.com:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 100 });
        db.VpnInbounds.Add(new VpnInbound
        {
            Id = Guid.NewGuid(),
            VpnPanelId = panelId,
            ExternalInboundId = "1",
            Name = protocol,
            Protocol = protocol,
            Port = 443,
            SettingsJson = "{\"clients\":[]}",
            StreamSettingsJson = "{\"network\":\"ws\",\"security\":\"tls\",\"tlsSettings\":{\"serverName\":\"vpn.example.com\"},\"wsSettings\":{\"path\":\"/vpn\"}}",
            IsDefault = true,
            IsActive = true,
            Capacity = 100
        });
        await db.SaveChangesAsync();

        var access = await provider.CreateAccessAsync(new VpnProvisionRequest(subscriptionId, userId, tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3, protocol), CancellationToken.None);

        Assert.StartsWith(expectedPrefix, access.AccessUri);
        Assert.Equal(1, await db.VpnClients.CountAsync());
        Assert.Equal(1, client.AddClientCalls);
    }

    [Fact]
    public async Task Real_Vpn_Provider_Should_Fail_When_Inbound_Missing_And_AutoCreate_Disabled()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow) { ReturnNoInbounds = true };
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, client, new TestSecretProtector(), clock);
        var panelId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel
        {
            Id = panelId,
            Name = "prod-panel",
            BaseUrl = "https://vpn.example.com:2053",
            Login = "admin",
            EncryptedPassword = "secret",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 100,
            AutoCreateInbound = false
        });
        db.VpnInbounds.Add(new VpnInbound
        {
            VpnPanelId = panelId,
            ExternalInboundId = "disabled-inbound",
            Name = "disabled-vless",
            Protocol = "vless",
            Port = 443,
            IsDefault = true,
            IsActive = false,
            Capacity = 100
        });
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAccessAsync(new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None));

        Assert.Contains("no active inbound", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, client.CreateInboundCalls);
        Assert.Equal(0, client.AddClientCalls);
        Assert.Empty(await db.VpnClients.ToListAsync());
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync()).UsedCapacity);
    }

    [Fact]
    public async Task Renewal_Should_Update_Existing_Client_Instead_Of_Creating_Duplicate()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, client, new TestSecretProtector(), clock);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);

        var access = await provider.UpdateAccessAsync(new VpnProvisionRequest(ids.SubscriptionId, ids.UserId, ids.TariffId, Guid.NewGuid(), clock.UtcNow.AddDays(60), 3), CancellationToken.None);

        Assert.Contains("vless://", access.AccessUri);
        Assert.Equal(1, await db.VpnClients.CountAsync());
        Assert.Equal(1, client.UpdateClientCalls);
        Assert.Equal(clock.UtcNow.AddDays(60), (await db.VpnClients.SingleAsync()).ExpiryTime);
    }

    [Fact]
    public async Task Renewal_Should_Keep_Assigned_Panel_When_Its_Capacity_Is_Full()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, client, new TestSecretProtector(), clock);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        var assignedPanel = await db.VpnPanels.SingleAsync(x => x.Id == ids.PanelId);
        var assignedInbound = await db.VpnInbounds.SingleAsync(x => x.Id == ids.InboundId);
        assignedPanel.Capacity = 1;
        assignedPanel.UsedCapacity = 1;
        assignedInbound.Capacity = 1;
        assignedInbound.UsedCapacity = 1;
        var sparePanelId = Guid.NewGuid();
        db.VpnPanels.Add(new VpnPanel { Id = sparePanelId, Name = "spare", BaseUrl = "https://spare.example.com:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 100 });
        db.VpnInbounds.Add(new VpnInbound { Id = Guid.NewGuid(), VpnPanelId = sparePanelId, ExternalInboundId = "2", Name = "spare-vless", Protocol = "vless", Port = 8443, StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}", SettingsJson = "{\"clients\":[]}", IsDefault = true, IsActive = true, Capacity = 100 });
        await db.SaveChangesAsync();

        await provider.UpdateAccessAsync(new VpnProvisionRequest(ids.SubscriptionId, ids.UserId, ids.TariffId, Guid.NewGuid(), clock.UtcNow.AddDays(60), 3), CancellationToken.None);

        var renewed = await db.VpnClients.SingleAsync();
        Assert.Equal(ids.PanelId, renewed.VpnPanelId);
        Assert.Equal(ids.InboundId, renewed.VpnInboundId);
        Assert.Equal("1", Assert.Single(client.UpdateRequests).InboundId);
    }

    [Fact]
    public async Task Delete_Access_Should_Release_Panel_And_Inbound_Capacity()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, client, new TestSecretProtector(), clock);
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);
        (await db.VpnPanels.SingleAsync()).UsedCapacity = 1;
        (await db.VpnInbounds.SingleAsync()).UsedCapacity = 1;
        await db.SaveChangesAsync();

        await provider.DeleteAccessAsync(ids.ClientId.ToString(), CancellationToken.None);

        Assert.Empty(await db.VpnClients.ToListAsync());
        Assert.Equal(0, (await db.VpnPanels.SingleAsync()).UsedCapacity);
        Assert.Equal(0, (await db.VpnInbounds.SingleAsync()).UsedCapacity);
    }

    [Fact]
    public async Task Cancel_Subscription_Should_Atomically_Revoke_X3Ui_Client_And_Release_All_Capacity()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-cancel-{Guid.NewGuid():N}.db");
        try
        {
            var options = SqliteOptions(databasePath);
            var clock = new FixedClock();
            var remote = new FakeX3UiClient(clock.UtcNow);
            var seed = await SeedRelationalProvisioningAsync(options, clock.UtcNow, capacity: 1, subscriptionCount: 1);
            await using var db = new ApplicationDbContext(options);
            var panel = await db.VpnPanels.SingleAsync();
            var node = new VpnNode
            {
                Name = "cancel-node",
                Host = "vpn.example.test",
                IpAddress = "127.0.0.1",
                Provider = "x3ui",
                Region = "eu",
                Status = NodeStatus.Ready,
                Capacity = 1,
                UsedCapacity = 1,
                HealthStatus = HealthStatus.Healthy,
                IsAvailableForNewUsers = true,
                SupportedProtocolsCsv = "vless",
                PanelBaseUrl = panel.BaseUrl
            };
            db.VpnNodes.Add(node);
            await db.SaveChangesAsync();
            var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
            var provisioned = await provider.CreateAccessAsync(new VpnProvisionRequest(
                seed.SubscriptionIds[0],
                seed.UserId,
                seed.TariffId,
                node.Id,
                clock.UtcNow.AddDays(30),
                3), CancellationToken.None);
            var access = new AccessCredential
            {
                SubscriptionId = seed.SubscriptionIds[0],
                ProviderType = provider.Name,
                ProviderAccessId = provisioned.ProviderAccessId,
                ServerId = node.Id,
                AccessUri = provisioned.AccessUri,
                Status = AccessCredentialStatus.Active,
                IssuedAt = clock.UtcNow,
                Revision = 1
            };
            db.AccessCredentials.Add(access);
            var subscription = await db.Subscriptions.SingleAsync();
            subscription.CurrentAccessId = access.Id;
            subscription.CurrentServerId = node.Id;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            subscription = await db.Subscriptions.Include(x => x.CurrentAccess).SingleAsync();
            var lifecycle = new VpnAccessLifecycleService(db, new SingleVpnProviderFactory(provider), clock);

            var result = await lifecycle.CancelSubscriptionAsync(subscription, "terminal cancellation", null, CancellationToken.None);

            Assert.True(result.IsSuccess, result.Error);
            db.ChangeTracker.Clear();
            var persistedSubscription = await db.Subscriptions.SingleAsync();
            Assert.Equal(SubscriptionStatus.Cancelled, persistedSubscription.Status);
            Assert.Null(persistedSubscription.CurrentAccessId);
            Assert.Null(persistedSubscription.CurrentServerId);
            Assert.Equal(AccessCredentialStatus.Revoked, (await db.AccessCredentials.SingleAsync()).Status);
            Assert.Empty(await db.VpnClients.ToListAsync());
            Assert.Equal(0, (await db.VpnNodes.SingleAsync()).UsedCapacity);
            Assert.Equal(0, (await db.VpnPanels.SingleAsync()).UsedCapacity);
            Assert.Equal(0, (await db.VpnInbounds.SingleAsync()).UsedCapacity);
            Assert.Equal(1, remote.DeleteClientCalls);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Cancel_Subscription_Should_Roll_Back_All_Capacity_When_X3Ui_Local_Save_Fails()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-cancel-failure-{Guid.NewGuid():N}.db");
        try
        {
            var options = SqliteOptions(databasePath);
            var clock = new FixedClock();
            var remote = new FakeX3UiClient(clock.UtcNow);
            var seed = await SeedRelationalProvisioningAsync(options, clock.UtcNow, capacity: 1, subscriptionCount: 1);
            await using var db = new FailingSaveApplicationDbContext(options);
            var panel = await db.VpnPanels.SingleAsync();
            var node = new VpnNode
            {
                Name = "cancel-failure-node",
                Host = "vpn.example.test",
                IpAddress = "127.0.0.1",
                Provider = "x3ui",
                Region = "eu",
                Status = NodeStatus.Ready,
                Capacity = 1,
                UsedCapacity = 1,
                HealthStatus = HealthStatus.Healthy,
                IsAvailableForNewUsers = true,
                SupportedProtocolsCsv = "vless",
                PanelBaseUrl = panel.BaseUrl
            };
            db.VpnNodes.Add(node);
            await db.SaveChangesAsync();
            var provider = new X3UiVpnProvider(ProductionConfiguration(), db, remote, new TestSecretProtector(), clock);
            var provisioned = await provider.CreateAccessAsync(new VpnProvisionRequest(
                seed.SubscriptionIds[0],
                seed.UserId,
                seed.TariffId,
                node.Id,
                clock.UtcNow.AddDays(30),
                3), CancellationToken.None);
            var access = new AccessCredential
            {
                SubscriptionId = seed.SubscriptionIds[0],
                ProviderType = provider.Name,
                ProviderAccessId = provisioned.ProviderAccessId,
                ServerId = node.Id,
                AccessUri = provisioned.AccessUri,
                Status = AccessCredentialStatus.Active,
                IssuedAt = clock.UtcNow,
                Revision = 1
            };
            db.AccessCredentials.Add(access);
            var subscription = await db.Subscriptions.SingleAsync();
            subscription.CurrentAccessId = access.Id;
            subscription.CurrentServerId = node.Id;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            subscription = await db.Subscriptions.Include(x => x.CurrentAccess).SingleAsync();
            var lifecycle = new VpnAccessLifecycleService(db, new SingleVpnProviderFactory(provider), clock);
            db.FailNextSave = true;

            var result = await lifecycle.CancelSubscriptionAsync(subscription, "terminal cancellation", null, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.True(result.IsRetryable);
            db.ChangeTracker.Clear();
            var persistedSubscription = await db.Subscriptions.SingleAsync();
            Assert.Equal(SubscriptionStatus.Active, persistedSubscription.Status);
            Assert.Equal(access.Id, persistedSubscription.CurrentAccessId);
            Assert.Equal(node.Id, persistedSubscription.CurrentServerId);
            Assert.Equal(AccessCredentialStatus.SyncRequired, (await db.AccessCredentials.SingleAsync()).Status);
            Assert.Single(await db.VpnClients.ToListAsync());
            Assert.Equal(1, (await db.VpnNodes.SingleAsync()).UsedCapacity);
            Assert.Equal(1, (await db.VpnPanels.SingleAsync()).UsedCapacity);
            Assert.Equal(1, (await db.VpnInbounds.SingleAsync()).UsedCapacity);
            Assert.Equal(1, remote.DeleteClientCalls);
            Assert.Equal(2, remote.AddClientCalls);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Concurrent_Create_For_Same_Subscription_Should_Be_Idempotent()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-x3ui-{Guid.NewGuid():N}.db");
        try
        {
            var options = SqliteOptions(databasePath);
            var clock = new FixedClock();
            var addStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseAdd = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var remote = new FakeX3UiClient(clock.UtcNow) { AddClientStarted = addStarted, ReleaseAddClient = releaseAdd };
            var ids = await SeedRelationalProvisioningAsync(options, clock.UtcNow, capacity: 2, subscriptionCount: 1);
            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var firstProvider = new X3UiVpnProvider(ProductionConfiguration(), firstDb, remote, new TestSecretProtector(), clock);
            var secondProvider = new X3UiVpnProvider(ProductionConfiguration(), secondDb, remote, new TestSecretProtector(), clock);
            var request = new VpnProvisionRequest(ids.SubscriptionIds[0], ids.UserId, ids.TariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3);

            var first = Task.Run(() => firstProvider.CreateAccessAsync(request, CancellationToken.None));
            await addStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var secondStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var second = Task.Run(async () =>
            {
                secondStarted.SetResult(true);
                return await secondProvider.CreateAccessAsync(request, CancellationToken.None);
            });
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(100);
            Assert.Equal(1, remote.AddClientCalls);
            releaseAdd.SetResult(true);
            var results = await Task.WhenAll(first, second);

            await using var verify = new ApplicationDbContext(options);
            Assert.Equal(results[0].ProviderAccessId, results[1].ProviderAccessId);
            Assert.Equal(1, remote.AddClientCalls);
            Assert.Equal(1, remote.UpdateClientCalls);
            Assert.Equal(1, await verify.VpnClients.CountAsync());
            Assert.Equal(1, (await verify.VpnPanels.SingleAsync()).UsedCapacity);
            Assert.Equal(1, (await verify.VpnInbounds.SingleAsync()).UsedCapacity);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Concurrent_Create_Should_Not_Oversubscribe_Last_Inbound_Slot()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-x3ui-{Guid.NewGuid():N}.db");
        try
        {
            var options = SqliteOptions(databasePath);
            var clock = new FixedClock();
            using var addBarrier = new Barrier(2);
            var remote = new FakeX3UiClient(clock.UtcNow) { AddClientBarrier = addBarrier };
            var ids = await SeedRelationalProvisioningAsync(options, clock.UtcNow, capacity: 1, subscriptionCount: 2);
            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var firstProvider = new X3UiVpnProvider(ProductionConfiguration(), firstDb, remote, new TestSecretProtector(), clock);
            var secondProvider = new X3UiVpnProvider(ProductionConfiguration(), secondDb, remote, new TestSecretProtector(), clock);

            var attempts = await Task.WhenAll(
                Task.Run(() => CaptureAsync(() => firstProvider.CreateAccessAsync(new VpnProvisionRequest(ids.SubscriptionIds[0], ids.UserId, ids.TariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None))),
                Task.Run(() => CaptureAsync(() => secondProvider.CreateAccessAsync(new VpnProvisionRequest(ids.SubscriptionIds[1], ids.UserId, ids.TariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None))));

            await using var verify = new ApplicationDbContext(options);
            Assert.Equal(1, attempts.Count(x => x is null));
            Assert.Equal(1, attempts.Count(x => x is not null));
            Assert.Equal(2, remote.AddClientCalls);
            Assert.Equal(1, remote.DeleteClientCalls);
            Assert.Equal(1, await verify.VpnClients.CountAsync());
            Assert.Equal(1, (await verify.VpnPanels.SingleAsync()).UsedCapacity);
            Assert.Equal(1, (await verify.VpnInbounds.SingleAsync()).UsedCapacity);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }


    [Fact]
    public async Task Sandbox_Vpn_Provider_Should_Create_Deterministic_Client_Without_Network_Calls()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var client = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(SandboxConfiguration(), db, client, new TestSecretProtector(), clock);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        await db.SaveChangesAsync();

        var first = await provider.CreateAccessAsync(new VpnProvisionRequest(subscriptionId, userId, tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None);
        var second = await provider.UpdateAccessAsync(new VpnProvisionRequest(subscriptionId, userId, tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(60), 5), CancellationToken.None);

        Assert.Equal($"x3ui-sandbox-{subscriptionId:N}", first.ProviderAccessId);
        Assert.Equal(first.ProviderAccessId, second.ProviderAccessId);
        Assert.Equal(first.AccessUri, second.AccessUri);
        Assert.Contains("vless://", first.AccessUri);
        Assert.Equal(1, await db.VpnClients.CountAsync());
        Assert.Equal(0, client.AddClientCalls);
        Assert.Equal(0, client.UpdateClientCalls);
        Assert.Equal(clock.UtcNow.AddDays(60), (await db.VpnClients.SingleAsync()).ExpiryTime);
    }

    [Fact]
    public async Task Vpn_Provider_Should_Reject_Unsupported_Protocol_Before_Any_State_Or_Network_Mutation()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var remote = new FakeX3UiClient(clock.UtcNow);
        var provider = new X3UiVpnProvider(SandboxConfiguration(), db, remote, new TestSecretProtector(), clock);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAccessAsync(
            new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), clock.UtcNow.AddDays(30), 3, Protocol: "wireguard"),
            CancellationToken.None));

        Assert.Contains("unsupported VPN protocol", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.VpnClients.ToListAsync());
        Assert.Empty(await db.VpnPanels.ToListAsync());
        Assert.Empty(await db.VpnInbounds.ToListAsync());
        Assert.Equal(0, remote.AddClientCalls);
    }

    [Fact]
    public async Task Sandbox_Vpn_Provider_Should_Reject_Invalid_Public_Endpoint_Without_Local_State()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var provider = new X3UiVpnProvider(SandboxConfiguration("sandbox-node.local/path?token=leak", "65536"), db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock);
        var tariff = new Tariff { Name = "Invalid sandbox endpoint", Slug = "invalid-sandbox-endpoint", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAccessAsync(
            new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), tariff.Id, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3),
            CancellationToken.None));

        Assert.Contains("sandbox public endpoint", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.VpnClients.ToListAsync());
        Assert.Empty(await db.VpnPanels.ToListAsync());
        Assert.Empty(await db.VpnInbounds.ToListAsync());
    }

    [Fact]
    public async Task Sandbox_Vpn_Provider_Should_Bracket_Ipv6_Public_Endpoint()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var provider = new X3UiVpnProvider(SandboxConfiguration("2001:db8::20"), db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock);
        var tariff = new Tariff { Name = "IPv6 sandbox", Slug = "ipv6-sandbox", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();

        var access = await provider.CreateAccessAsync(
            new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), tariff.Id, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3),
            CancellationToken.None);

        Assert.Contains("@[2001:db8::20]:443", access.AccessUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sandbox_Vpn_Provider_Enable_Disable_Sync_And_Reset_Should_Update_Local_Client()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var provider = new X3UiVpnProvider(SandboxConfiguration(), db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        await db.SaveChangesAsync();

        var access = await provider.CreateAccessAsync(new VpnProvisionRequest(subscriptionId, userId, tariffId, Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None);
        await provider.DisableAccessAsync(access.ProviderAccessId, CancellationToken.None);
        Assert.False((await db.VpnClients.SingleAsync()).Enable);

        await provider.EnableAccessAsync(access.ProviderAccessId, CancellationToken.None);
        Assert.True((await db.VpnClients.SingleAsync()).Enable);

        var usage = await provider.SyncAccessAsync(access.ProviderAccessId, CancellationToken.None);
        await provider.ResetTrafficAsync(access.ProviderAccessId, CancellationToken.None);

        Assert.NotNull(usage.UsedTrafficBytes);
        Assert.NotNull((await db.VpnClients.SingleAsync()).LastSyncedAt);
    }

    private static AdminVpnPanelsController CreateAdminController(X3UiPanelService service, Guid adminId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString())
        }, "Test");

        return new AdminVpnPanelsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DbContextOptions<ApplicationDbContext> SqliteOptions(string databasePath)
        => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Default Timeout=10")
            .Options;

    private static async Task<(Guid UserId, Guid TariffId, Guid[] SubscriptionIds)> SeedRelationalProvisioningAsync(
        DbContextOptions<ApplicationDbContext> options,
        DateTimeOffset now,
        int capacity,
        int subscriptionCount)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var panelId = Guid.NewGuid();
        var subscriptions = Enumerable.Range(0, subscriptionCount)
            .Select(_ => new Subscription { UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = now, EndAt = now.AddDays(30) })
            .ToArray();
        db.Users.Add(new User { Id = userId, Email = $"x3ui-{userId:N}@example.test", DisplayName = "X3Ui test", ReferralCode = $"X3UI{userId:N}" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = $"monthly-{tariffId:N}", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        db.Subscriptions.AddRange(subscriptions);
        db.VpnPanels.Add(new VpnPanel { Id = panelId, Name = $"panel-{panelId:N}", BaseUrl = $"https://{panelId:N}.example.test:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = capacity });
        db.VpnInbounds.Add(new VpnInbound { Id = Guid.NewGuid(), VpnPanelId = panelId, ExternalInboundId = "1", Name = "vless", Protocol = "vless", Port = 443, SettingsJson = "{\"clients\":[]}", StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}", IsDefault = true, IsActive = true, Capacity = capacity });
        await db.SaveChangesAsync();
        return (userId, tariffId, subscriptions.Select(x => x.Id).ToArray());
    }

    private static async Task<(Guid[] ClientIds, Guid TargetInboundId)> SeedRelationalMigrationAsync(
        DbContextOptions<ApplicationDbContext> options,
        DateTimeOffset now)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var user = new User { Email = $"migration-{Guid.NewGuid():N}@example.test", DisplayName = "Migration test", ReferralCode = $"MIG{Guid.NewGuid():N}" };
        var tariff = new Tariff { Name = "Migration", Slug = $"migration-{Guid.NewGuid():N}", Description = "Migration", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var panel = new VpnPanel { Name = $"migration-panel-{Guid.NewGuid():N}", BaseUrl = $"https://migration-{Guid.NewGuid():N}.example.test:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 3, UsedCapacity = 2 };
        var sources = Enumerable.Range(1, 2).Select(index => CreateMigrationInbound(panel.Id, index.ToString(), $"source-{index}", capacity: 1, usedCapacity: 1, port: 443 + index)).ToArray();
        var target = CreateMigrationInbound(panel.Id, "3", "target", capacity: 1);
        var subscriptions = Enumerable.Range(0, 2).Select(_ => new Subscription { UserId = user.Id, TariffId = tariff.Id, Status = SubscriptionStatus.Active, StartAt = now, EndAt = now.AddDays(30) }).ToArray();
        var clients = subscriptions.Select((subscription, index) => new VpnClient
        {
            UserId = user.Id,
            SubscriptionId = subscription.Id,
            VpnPanelId = panel.Id,
            VpnInboundId = sources[index].Id,
            ExternalClientId = $"migration-client-{index + 1}",
            Email = $"migration-{index + 1}@example.test",
            Uuid = $"migration-client-{index + 1}",
            ExpiryTime = now.AddDays(30),
            Enable = true
        }).ToArray();
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.Subscriptions.AddRange(subscriptions);
        db.VpnPanels.Add(panel);
        db.VpnInbounds.AddRange(sources);
        db.VpnInbounds.Add(target);
        db.VpnClients.AddRange(clients);
        await db.SaveChangesAsync();
        return (clients.Select(x => x.Id).ToArray(), target.Id);
    }

    private static async Task<Exception?> CaptureAsync(Func<Task<VpnProvisionResult>> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static async Task<(Result<VpnClientDto>? Result, Exception? Error)> CaptureMigrationAsync(Func<Task<Result<VpnClientDto>>> action)
    {
        try
        {
            return (await action(), null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    private static FailingSaveApplicationDbContext CreateFailingDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new FailingSaveApplicationDbContext(options);
    }

    private static IConfiguration ProductionConfiguration()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Vpn:X3Ui:Mode"] = "Production" }).Build();

    private static IConfiguration SandboxConfiguration(string publicHost = "sandbox-node.local", string publicPort = "443")
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vpn:X3Ui:Mode"] = "Sandbox",
            ["Vpn:X3Ui:SandboxPublicHost"] = publicHost,
            ["Vpn:X3Ui:SandboxPublicPort"] = publicPort
        }).Build();

    private static CreateVpnInboundCommand NewInboundCommand(
        string name = "default-vless",
        string protocol = "vless",
        int port = 443,
        string settingsJson = "{\"clients\":[]}",
        string streamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}",
        string sniffingJson = "{}",
        bool isDefault = true,
        int capacity = 100,
        bool isActive = true)
        => new(name, protocol, port, string.Empty, settingsJson, streamSettingsJson, sniffingJson, isDefault, capacity, isActive);

    private static VpnInbound CreateMigrationInbound(Guid panelId, string externalId, string name, int capacity, int usedCapacity = 0, int port = 8443)
        => new()
        {
            VpnPanelId = panelId,
            ExternalInboundId = externalId,
            Name = name,
            Protocol = "vless",
            Port = port,
            StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}",
            SettingsJson = "{\"clients\":[]}",
            IsActive = true,
            Capacity = capacity,
            UsedCapacity = usedCapacity
        };

    private static async Task<(Guid PanelId, Guid InboundId, Guid ClientId, Guid SubscriptionId, Guid UserId, Guid TariffId)> SeedPanelWithLocalClientAsync(ApplicationDbContext db, DateTimeOffset now)
    {
        var panelId = Guid.NewGuid();
        var inboundId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true });
        db.VpnPanels.Add(new VpnPanel { Id = panelId, Name = "panel", BaseUrl = "https://vpn.example.com:2053", Login = "admin", EncryptedPassword = "secret", Region = "eu", Status = VpnPanelStatus.Active, HealthStatus = HealthStatus.Healthy, Capacity = 100 });
        db.VpnInbounds.Add(new VpnInbound { Id = inboundId, VpnPanelId = panelId, ExternalInboundId = "1", Name = "vless", Protocol = "vless", Port = 443, StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"tls\"}", SettingsJson = "{\"clients\":[]}", IsDefault = true, IsActive = true, Capacity = 100 });
        db.VpnClients.Add(new VpnClient { Id = clientId, UserId = userId, SubscriptionId = subscriptionId, VpnPanelId = panelId, VpnInboundId = inboundId, ExternalClientId = "client-1", Email = "user@example.test", Uuid = "client-1", ExpiryTime = now.AddDays(1), Enable = true });
        await db.SaveChangesAsync();
        return (panelId, inboundId, clientId, subscriptionId, userId, tariffId);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 4, 30, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FailingSaveApplicationDbContext : ApplicationDbContext
    {
        public FailingSaveApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public bool FailNextSave { get; set; }
        public bool FailNextSaveAfterCommit { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new InvalidOperationException("simulated local save failure");
            }

            var result = await base.SaveChangesAsync(cancellationToken);
            if (FailNextSaveAfterCommit)
            {
                FailNextSaveAfterCommit = false;
                throw new InvalidOperationException("simulated ambiguous local save outcome");
            }

            return result;
        }
    }

    private sealed class RevisionChangingSaveApplicationDbContext : ApplicationDbContext
    {
        public RevisionChangingSaveApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public Guid? ChangeClientRevisionOnNextSave { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            if (!ChangeClientRevisionOnNextSave.HasValue) return result;

            var clientId = ChangeClientRevisionOnNextSave.Value;
            ChangeClientRevisionOnNextSave = null;
            var client = await VpnClients.SingleAsync(x => x.Id == clientId, cancellationToken);
            client.Revision = checked(client.Revision + 1);
            await base.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(0, value.Length - visibleTail)) + value[^Math.Min(visibleTail, value.Length)..];
    }

    private sealed class QueryCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class SingleVpnProviderFactory(IVpnProvider provider) : IVpnProviderFactory
    {
        public IVpnProvider Get(string providerName) => provider;
    }

    private sealed class FakeX3UiClient : IX3UiClient
    {
        private readonly DateTimeOffset _now;
        private int _addClientCalls;
        public FakeX3UiClient(DateTimeOffset now) => _now = now;
        public bool ReturnNoInbounds { get; set; }
        public IReadOnlyCollection<X3UiInboundDto>? Inbounds { get; set; }
        public CancellationTokenSource? CancelGetInboundsWith { get; set; }
        public HashSet<string> FailingDeleteInboundIds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> FailingInboundDeleteIds { get; } = new(StringComparer.Ordinal);
        public HashSet<int> FailingUpdateInboundCalls { get; } = [];
        public List<string> DeleteInboundIds { get; } = [];
        public List<string> DeletedInboundIds { get; } = [];
        public List<X3UiUpdateInboundRequest> InboundUpdateRequests { get; } = [];
        public int CreateInboundCalls { get; private set; }
        public int UpdateInboundCalls { get; private set; }
        public int HealthCheckCalls { get; private set; }
        public int AddClientCalls => _addClientCalls;
        public int UpdateClientCalls { get; private set; }
        public int DeleteClientCalls { get; private set; }
        public int ResetTrafficCalls { get; private set; }
        public int GetTrafficCalls { get; private set; }
        public int EnableClientCalls { get; private set; }
        public int DisableClientCalls { get; private set; }
        public Barrier? AddClientBarrier { get; init; }
        public bool FailAddClientAfterSideEffect { get; init; }
        public TaskCompletionSource<bool>? AddClientStarted { get; init; }
        public TaskCompletionSource<bool>? ReleaseAddClient { get; init; }
        public TaskCompletionSource<bool>? UpdateInboundStarted { get; init; }
        public TaskCompletionSource<bool>? ReleaseUpdateInbound { get; init; }
        public TaskCompletionSource<bool>? HealthCheckStarted { get; init; }
        public TaskCompletionSource<bool>? ReleaseHealthCheck { get; init; }
        public Action<string>? AfterDeleteClient { get; init; }
        public HashSet<int> FailingUpdateClientCalls { get; } = [];
        public bool FailResetTrafficAfterSideEffect { get; init; }
        public Action? AfterResetTraffic { get; init; }
        public List<X3UiUpdateClientRequest> UpdateRequests { get; } = [];

        public Task<X3UiSession> LoginAsync(VpnPanel panel, string password, CancellationToken cancellationToken) => Task.FromResult(new X3UiSession("session=test", _now));
        public async Task<X3UiHealthResult> CheckHealthAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
        {
            HealthCheckCalls += 1;
            HealthCheckStarted?.TrySetResult(true);
            if (ReleaseHealthCheck is not null)
            {
                await ReleaseHealthCheck.Task.WaitAsync(cancellationToken);
            }
            return new X3UiHealthResult(true, "2.4.12", 12);
        }
        public Task<X3UiPanelVersionResult> GetPanelVersionAsync(VpnPanel panel, string password, CancellationToken cancellationToken) => Task.FromResult(new X3UiPanelVersionResult("2.4.12", "{}"));
        public Task<X3UiInboundDto?> GetInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken) => Task.FromResult<X3UiInboundDto?>(DefaultInbound());
        public Task<IReadOnlyCollection<X3UiInboundDto>> GetInboundsAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
        {
            if (CancelGetInboundsWith is not null)
            {
                CancelGetInboundsWith.Cancel();
                return Task.FromCanceled<IReadOnlyCollection<X3UiInboundDto>>(CancelGetInboundsWith.Token);
            }

            return Task.FromResult(Inbounds ?? (ReturnNoInbounds ? Array.Empty<X3UiInboundDto>() : new[] { DefaultInbound() }));
        }

        public Task<X3UiInboundDto> CreateInboundAsync(VpnPanel panel, string password, X3UiCreateInboundRequest request, CancellationToken cancellationToken)
        {
            CreateInboundCalls += 1;
            return Task.FromResult(new X3UiInboundDto("1", request.Remark, request.Protocol, request.Port, request.Listen, request.SettingsJson, request.StreamSettingsJson, request.SniffingJson, request.Enable));
        }

        public Task DeleteInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken)
        {
            DeletedInboundIds.Add(inboundId);
            if (FailingInboundDeleteIds.Contains(inboundId))
            {
                throw new InvalidOperationException($"inbound delete failed for {inboundId}");
            }
            return Task.CompletedTask;
        }

        public async Task<X3UiInboundDto> UpdateInboundAsync(VpnPanel panel, string password, X3UiUpdateInboundRequest request, CancellationToken cancellationToken)
        {
            UpdateInboundCalls += 1;
            var call = UpdateInboundCalls;
            InboundUpdateRequests.Add(request);
            if (call == 1 && UpdateInboundStarted is not null)
            {
                UpdateInboundStarted.TrySetResult(true);
                if (ReleaseUpdateInbound is not null)
                {
                    await ReleaseUpdateInbound.Task.WaitAsync(cancellationToken);
                }
            }
            if (FailingUpdateInboundCalls.Contains(call))
            {
                throw new TimeoutException("simulated ambiguous inbound update timeout");
            }
            return new X3UiInboundDto(request.Id, request.Remark, request.Protocol, request.Port, request.Listen, request.SettingsJson, request.StreamSettingsJson, request.SniffingJson, request.Enable);
        }

        public async Task<X3UiClientDto> AddClientAsync(VpnPanel panel, string password, X3UiAddClientRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _addClientCalls);
            AddClientStarted?.TrySetResult(true);
            AddClientBarrier?.SignalAndWait(TimeSpan.FromSeconds(10));
            if (ReleaseAddClient is not null)
            {
                await ReleaseAddClient.Task.WaitAsync(cancellationToken);
            }
            if (FailAddClientAfterSideEffect)
            {
                throw new TimeoutException("simulated ambiguous add timeout");
            }
            return new X3UiClientDto(request.Uuid, request.Email, request.Uuid, request.Flow, request.LimitIp, request.TotalGb, request.ExpiryTime, request.Enable, null, null);
        }

        public Task<X3UiClientDto> UpdateClientAsync(VpnPanel panel, string password, X3UiUpdateClientRequest request, CancellationToken cancellationToken)
        {
            UpdateClientCalls += 1;
            UpdateRequests.Add(request);
            if (FailingUpdateClientCalls.Contains(UpdateClientCalls))
            {
                throw new TimeoutException("simulated ambiguous update timeout");
            }
            return Task.FromResult(new X3UiClientDto(request.ClientId, request.Email, request.Uuid, request.Flow, request.LimitIp, request.TotalGb, request.ExpiryTime, request.Enable, null, null));
        }

        public Task DeleteClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
        {
            DeleteClientCalls += 1;
            DeleteInboundIds.Add(inboundId);
            AfterDeleteClient?.Invoke(inboundId);
            cancellationToken.ThrowIfCancellationRequested();
            if (FailingDeleteInboundIds.Contains(inboundId))
            {
                throw new InvalidOperationException($"delete failed for inbound {inboundId}");
            }
            return Task.CompletedTask;
        }
        public Task EnableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
        {
            EnableClientCalls += 1;
            return Task.CompletedTask;
        }
        public Task DisableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
        {
            DisableClientCalls += 1;
            return Task.CompletedTask;
        }
        public Task ResetClientTrafficAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
        {
            ResetTrafficCalls += 1;
            AfterResetTraffic?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (FailResetTrafficAfterSideEffect)
            {
                throw new TimeoutException("simulated ambiguous traffic reset timeout");
            }
            return Task.CompletedTask;
        }
        public Task<X3UiTrafficSnapshot> GetClientTrafficAsync(VpnPanel panel, string password, string clientId, CancellationToken cancellationToken)
        {
            GetTrafficCalls += 1;
            return Task.FromResult(new X3UiTrafficSnapshot(clientId, 0, 0, _now));
        }

        private X3UiInboundDto DefaultInbound()
            => new("1", "default-vless", "vless", 443, string.Empty,
                $"{{\"clients\":[{{\"id\":\"client-1\",\"email\":\"user@example.test\",\"expiryTime\":{_now.AddDays(30).ToUnixTimeMilliseconds()},\"enable\":true}},{{\"id\":\"orphan-1\",\"email\":\"orphan@example.test\",\"expiryTime\":{_now.AddDays(30).ToUnixTimeMilliseconds()},\"enable\":true}}]}}",
                "{\"network\":\"tcp\",\"security\":\"tls\"}",
                "{}",
                true);
    }

    private static X3UiInboundDto ChangedInbound()
        => new("1", "mutated-vless", "vless", 9443, string.Empty, "{\"clients\":[]}", "{\"network\":\"tcp\",\"security\":\"tls\"}", "{}", true);

    private static X3UiInboundDto AdditionalInbound()
        => new("2", "additional-vless", "vless", 8443, string.Empty, "{\"clients\":[]}", "{\"network\":\"tcp\",\"security\":\"tls\"}", "{}", true);

    private sealed class FailAfterFirstOnSecondEnumerationCollection : IReadOnlyCollection<X3UiInboundDto>
    {
        private readonly IReadOnlyList<X3UiInboundDto> _items;
        private readonly Action _fail;
        private int _enumerationCount;

        public FailAfterFirstOnSecondEnumerationCollection(IReadOnlyList<X3UiInboundDto> items, Action fail)
        {
            _items = items;
            _fail = fail;
        }

        public int Count => _items.Count;

        public IEnumerator<X3UiInboundDto> GetEnumerator()
        {
            _enumerationCount += 1;
            return _enumerationCount == 1 ? _items.GetEnumerator() : EnumerateUntilFailure().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<X3UiInboundDto> EnumerateUntilFailure()
        {
            yield return _items[0];
            _fail();
        }
    }
}
