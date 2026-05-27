using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        var uri = X3UiConfigUriGenerator.BuildVlessUri(panel, inbound, client);

        Assert.StartsWith("vless://11111111-1111-1111-1111-111111111111@vpn.example.com:443", uri);
        Assert.Contains("type=ws", uri);
        Assert.Contains("security=tls", uri);
        Assert.Contains("path=%2Fray", uri);
    }

    [Fact]
    public void Vless_Config_Generator_Should_Fail_When_Inbound_Is_Insufficient()
    {
        var uri = X3UiConfigUriGenerator.BuildVlessUri(
            new VpnPanel { BaseUrl = "https://vpn.example.com" },
            new VpnInbound { Protocol = "trojan", Port = 0 },
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
        var ids = await SeedPanelWithLocalClientAsync(db, clock.UtcNow);

        var health = await service.CheckHealthAsync(ids.PanelId, CancellationToken.None);
        var sync = await service.SyncPanelAsync(ids.PanelId, CancellationToken.None);

        Assert.True(health.IsSuccess, health.Error);
        Assert.True(sync.IsSuccess, sync.Error);
        Assert.Equal(HealthStatus.Healthy, (await db.VpnPanels.SingleAsync()).HealthStatus);
        Assert.True(await db.PanelHealthChecks.AnyAsync(x => x.Status == HealthStatus.Healthy));
        Assert.True(await db.PanelSyncEvents.AnyAsync(x => x.EventType == "orphan_client"));
        Assert.True(await db.PanelSyncEvents.AnyAsync(x => x.EventType == "expiry_mismatch"));
    }

    [Fact]
    public async Task Panel_Update_Should_Edit_Settings_And_Preserve_Empty_Password()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var service = new X3UiPanelService(db, new FakeX3UiClient(clock.UtcNow), new TestSecretProtector(), clock);

        var created = await service.CreatePanelAsync(new CreateVpnPanelCommand(
            "main-panel",
            "https://panel.example.test:2053/",
            "admin",
            "initial-secret",
            "eu",
            100,
            "Strict",
            "X3UiOfficial",
            false,
            "{}"), CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error);
        var panel = await db.VpnPanels.SingleAsync();
        var originalPassword = panel.EncryptedPassword;

        var updated = await service.UpdatePanelAsync(panel.Id, new UpdateVpnPanelCommand(
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
            Status: "Active"), CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error);
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
        Assert.Equal("edited-panel", updated.Value!.Name);
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
    public async Task Real_Vpn_Provider_Should_Fail_When_Inbound_Missing_And_AutoCreate_Disabled()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var provider = new X3UiVpnProvider(ProductionConfiguration(), db, new FakeX3UiClient(clock.UtcNow) { ReturnNoInbounds = true }, new TestSecretProtector(), clock);
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
            AutoCreateInbound = false
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateAccessAsync(new VpnProvisionRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), clock.UtcNow.AddDays(30), 3), CancellationToken.None));
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

        public Task<X3UiSession> LoginAsync(VpnPanel panel, string password, CancellationToken cancellationToken) => Task.FromResult(new X3UiSession("session=test", _now));
        public Task<X3UiHealthResult> CheckHealthAsync(VpnPanel panel, string password, CancellationToken cancellationToken) => Task.FromResult(new X3UiHealthResult(true, "2.4.12", 12));
        public Task<X3UiPanelVersionResult> GetPanelVersionAsync(VpnPanel panel, string password, CancellationToken cancellationToken) => Task.FromResult(new X3UiPanelVersionResult("2.4.12", "{}"));
        public Task<X3UiInboundDto?> GetInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken) => Task.FromResult<X3UiInboundDto?>(DefaultInbound());
        public Task<IReadOnlyCollection<X3UiInboundDto>> GetInboundsAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<X3UiInboundDto>>(ReturnNoInbounds ? Array.Empty<X3UiInboundDto>() : new[] { DefaultInbound() });

        public Task<X3UiInboundDto> CreateInboundAsync(VpnPanel panel, string password, X3UiCreateInboundRequest request, CancellationToken cancellationToken)
        {
            CreateInboundCalls += 1;
            return Task.FromResult(new X3UiInboundDto("1", request.Remark, request.Protocol, request.Port, request.Listen, request.SettingsJson, request.StreamSettingsJson, request.SniffingJson, true));
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

        public Task DeleteClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task EnableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ResetClientTrafficAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<X3UiTrafficSnapshot> GetClientTrafficAsync(VpnPanel panel, string password, string clientId, CancellationToken cancellationToken) => Task.FromResult(new X3UiTrafficSnapshot(clientId, 0, 0, _now));

        private X3UiInboundDto DefaultInbound()
            => new("1", "default-vless", "vless", 443, string.Empty,
                $"{{\"clients\":[{{\"id\":\"client-1\",\"email\":\"user@example.test\",\"expiryTime\":{_now.AddDays(30).ToUnixTimeMilliseconds()},\"enable\":true}},{{\"id\":\"orphan-1\",\"email\":\"orphan@example.test\",\"expiryTime\":{_now.AddDays(30).ToUnixTimeMilliseconds()},\"enable\":true}}]}}",
                "{\"network\":\"tcp\",\"security\":\"tls\"}",
                "{}",
                true);
    }
}
