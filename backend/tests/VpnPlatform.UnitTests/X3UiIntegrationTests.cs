using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
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
            Status: "Active"), CancellationToken.None)).Value);

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

        var result = Assert.IsType<DeleteVpnPanelResultDto>(Assert.IsType<OkObjectResult>(await controller.DeletePanel(panelId, CancellationToken.None)).Value);

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
        await db.SaveChangesAsync();

        var first = Assert.IsType<VpnInboundDto>(Assert.IsType<OkObjectResult>(await controller.CreateInbound(panelId, NewInboundCommand(name: "main-vless", isDefault: true), CancellationToken.None)).Value);
        var second = Assert.IsType<VpnInboundDto>(Assert.IsType<OkObjectResult>(await controller.CreateInbound(panelId, NewInboundCommand(name: "backup-vmess", protocol: "VMESS", port: 8443, isDefault: true), CancellationToken.None)).Value);

        Assert.Equal(2, client.CreateInboundCalls);
        Assert.False((await db.VpnInbounds.SingleAsync(x => x.Id == first.Id)).IsDefault);
        var secondInbound = await db.VpnInbounds.SingleAsync(x => x.Id == second.Id);
        Assert.True(secondInbound.IsDefault);
        Assert.Equal("vmess", secondInbound.Protocol);
        Assert.IsType<OkObjectResult>(await controller.GetInbounds(panelId, CancellationToken.None));

        var disabled = Assert.IsType<VpnInboundDto>(Assert.IsType<OkObjectResult>(await controller.PatchInbound(secondInbound.Id, NewInboundCommand(
            name: "backup-disabled",
            protocol: "vmess",
            port: 8443,
            isDefault: false,
            isActive: false), CancellationToken.None)).Value);

        Assert.False(disabled.IsActive);
        Assert.False(disabled.IsDefault);
        Assert.False((await db.VpnInbounds.SingleAsync(x => x.Id == secondInbound.Id)).IsDefault);

        var defaultResult = await controller.SetDefaultInbound(secondInbound.Id, CancellationToken.None);

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
        var disabled = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.DisableClient(ids.ClientId, CancellationToken.None)).Value);
        var enabled = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.EnableClient(ids.ClientId, CancellationToken.None)).Value);
        var synced = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.SyncClient(ids.ClientId, CancellationToken.None)).Value);
        var reset = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.ResetClientTraffic(ids.ClientId, CancellationToken.None)).Value);
        var migrated = Assert.IsType<VpnClientDto>(Assert.IsType<OkObjectResult>(await controller.MigrateClient(ids.ClientId, new MigrateVpnClientCommand(targetInboundId), CancellationToken.None)).Value);

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

    private static IConfiguration ProductionConfiguration()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Vpn:X3Ui:Mode"] = "Production" }).Build();

    private static IConfiguration SandboxConfiguration()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vpn:X3Ui:Mode"] = "Sandbox",
            ["Vpn:X3Ui:SandboxPublicHost"] = "sandbox-node.local",
            ["Vpn:X3Ui:SandboxPublicPort"] = "443"
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

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(0, value.Length - visibleTail)) + value[^Math.Min(visibleTail, value.Length)..];
    }

    private sealed class FakeX3UiClient : IX3UiClient
    {
        private readonly DateTimeOffset _now;
        public FakeX3UiClient(DateTimeOffset now) => _now = now;
        public bool ReturnNoInbounds { get; set; }
        public int CreateInboundCalls { get; private set; }
        public int AddClientCalls { get; private set; }
        public int UpdateClientCalls { get; private set; }
        public int DeleteClientCalls { get; private set; }
        public int ResetTrafficCalls { get; private set; }
        public int GetTrafficCalls { get; private set; }

        public Task<X3UiSession> LoginAsync(VpnPanel panel, string password, CancellationToken cancellationToken) => Task.FromResult(new X3UiSession("session=test", _now));
        public Task<X3UiHealthResult> CheckHealthAsync(VpnPanel panel, string password, CancellationToken cancellationToken) => Task.FromResult(new X3UiHealthResult(true, "2.4.12", 12));
        public Task<X3UiPanelVersionResult> GetPanelVersionAsync(VpnPanel panel, string password, CancellationToken cancellationToken) => Task.FromResult(new X3UiPanelVersionResult("2.4.12", "{}"));
        public Task<X3UiInboundDto?> GetInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken) => Task.FromResult<X3UiInboundDto?>(DefaultInbound());
        public Task<IReadOnlyCollection<X3UiInboundDto>> GetInboundsAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<X3UiInboundDto>>(ReturnNoInbounds ? Array.Empty<X3UiInboundDto>() : new[] { DefaultInbound() });

        public Task<X3UiInboundDto> CreateInboundAsync(VpnPanel panel, string password, X3UiCreateInboundRequest request, CancellationToken cancellationToken)
        {
            CreateInboundCalls += 1;
            return Task.FromResult(new X3UiInboundDto("1", request.Remark, request.Protocol, request.Port, request.Listen, request.SettingsJson, request.StreamSettingsJson, request.SniffingJson, request.Enable));
        }

        public Task<X3UiInboundDto> UpdateInboundAsync(VpnPanel panel, string password, X3UiUpdateInboundRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new X3UiInboundDto(request.Id, request.Remark, request.Protocol, request.Port, request.Listen, request.SettingsJson, request.StreamSettingsJson, request.SniffingJson, request.Enable));

        public Task<X3UiClientDto> AddClientAsync(VpnPanel panel, string password, X3UiAddClientRequest request, CancellationToken cancellationToken)
        {
            AddClientCalls += 1;
            return Task.FromResult(new X3UiClientDto(request.Uuid, request.Email, request.Uuid, request.Flow, request.LimitIp, request.TotalGb, request.ExpiryTime, request.Enable, null, null));
        }

        public Task<X3UiClientDto> UpdateClientAsync(VpnPanel panel, string password, X3UiUpdateClientRequest request, CancellationToken cancellationToken)
        {
            UpdateClientCalls += 1;
            return Task.FromResult(new X3UiClientDto(request.ClientId, request.Email, request.Uuid, request.Flow, request.LimitIp, request.TotalGb, request.ExpiryTime, request.Enable, null, null));
        }

        public Task DeleteClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
        {
            DeleteClientCalls += 1;
            return Task.CompletedTask;
        }
        public Task EnableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ResetClientTrafficAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
        {
            ResetTrafficCalls += 1;
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
}
