using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QRCoder;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Infrastructure.Vpn;

public class X3UiVpnProvider : IVpnProvider
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _db;
    private readonly IX3UiClient _client;
    private readonly ISecretProtector _secretProtector;
    private readonly IClock _clock;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public X3UiVpnProvider(IConfiguration configuration, IApplicationDbContext db, IX3UiClient client, ISecretProtector secretProtector, IClock clock, IQrCodeGenerator? qrCodeGenerator = null)
    {
        _configuration = configuration;
        _db = db;
        _client = client;
        _secretProtector = secretProtector;
        _clock = clock;
        _qrCodeGenerator = qrCodeGenerator ?? new SvgQrCodeGenerator(clock);
    }

    public string Name => "x3ui";

    public async Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
    {
        if (IsSandboxProvisioning(request))
        {
            return await CreateOrUpdateSandboxAccessAsync(request, cancellationToken);
        }

        return await CreateOrUpdateRealAccessAsync(request, false, cancellationToken);
    }

    public async Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
    {
        if (IsSandboxProvisioning(request))
        {
            return await CreateOrUpdateSandboxAccessAsync(request, cancellationToken);
        }

        return await CreateOrUpdateRealAccessAsync(request, true, cancellationToken);
    }

    public async Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
    {
        var vpnClient = await _db.VpnClients.Include(x => x.VpnPanel).Include(x => x.VpnInbound).FirstOrDefaultAsync(x => x.ExternalClientId == providerAccessId || x.Id.ToString() == providerAccessId, cancellationToken);
        if (vpnClient is null) return;

        var isSandboxClient = IsSandboxClient(vpnClient);
        if (!isSandboxClient && vpnClient.VpnPanel is not null && vpnClient.VpnInbound is not null)
        {
            var password = _secretProtector.Unprotect(vpnClient.VpnPanel.EncryptedPassword);
            await _client.DisableClientAsync(vpnClient.VpnPanel, password, vpnClient.VpnInbound.ExternalInboundId, vpnClient.Uuid, cancellationToken);
        }

        vpnClient.Enable = false;
        vpnClient.SyncStatus = isSandboxClient ? "sandbox-disabled" : "disabled";
        vpnClient.LastSyncedAt = _clock.UtcNow;
        vpnClient.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task EnableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
    {
        var vpnClient = await _db.VpnClients.Include(x => x.VpnPanel).Include(x => x.VpnInbound).FirstOrDefaultAsync(x => x.ExternalClientId == providerAccessId || x.Id.ToString() == providerAccessId, cancellationToken);
        if (vpnClient is null) return;

        var isSandboxClient = IsSandboxClient(vpnClient);
        if (!isSandboxClient && vpnClient.VpnPanel is not null && vpnClient.VpnInbound is not null)
        {
            var password = _secretProtector.Unprotect(vpnClient.VpnPanel.EncryptedPassword);
            await _client.EnableClientAsync(vpnClient.VpnPanel, password, vpnClient.VpnInbound.ExternalInboundId, vpnClient.Uuid, cancellationToken);
        }

        vpnClient.Enable = true;
        vpnClient.SyncStatus = isSandboxClient ? "sandbox-enabled" : "enabled";
        vpnClient.LastSyncedAt = _clock.UtcNow;
        vpnClient.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken)
    {
        var vpnClient = await _db.VpnClients.Include(x => x.VpnPanel).Include(x => x.VpnInbound).FirstOrDefaultAsync(x => x.ExternalClientId == providerAccessId || x.Id.ToString() == providerAccessId, cancellationToken);
        if (vpnClient is null) return;

        if (!IsSandboxClient(vpnClient) && vpnClient.VpnPanel is not null && vpnClient.VpnInbound is not null)
        {
            var password = _secretProtector.Unprotect(vpnClient.VpnPanel.EncryptedPassword);
            await _client.DeleteClientAsync(vpnClient.VpnPanel, password, vpnClient.VpnInbound.ExternalInboundId, vpnClient.Uuid, cancellationToken);
        }

        _db.VpnClients.Remove(vpnClient);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<VpnUsageSnapshot> SyncAccessAsync(string providerAccessId, CancellationToken cancellationToken)
    {
        var usage = await GetUsageAsync(providerAccessId, cancellationToken);
        var vpnClient = await _db.VpnClients.FirstOrDefaultAsync(x => x.ExternalClientId == providerAccessId || x.Id.ToString() == providerAccessId, cancellationToken);
        if (vpnClient is not null)
        {
            vpnClient.LastSyncedAt = usage.SyncedAt;
            vpnClient.SyncStatus = IsSandboxClient(vpnClient) ? "sandbox-synced" : "synced";
            vpnClient.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        return usage;
    }

    public async Task ResetTrafficAsync(string providerAccessId, CancellationToken cancellationToken)
    {
        var vpnClient = await _db.VpnClients.Include(x => x.VpnPanel).Include(x => x.VpnInbound).FirstOrDefaultAsync(x => x.ExternalClientId == providerAccessId || x.Id.ToString() == providerAccessId, cancellationToken);
        if (vpnClient is null) return;

        var isSandboxClient = IsSandboxClient(vpnClient);
        if (!isSandboxClient && vpnClient.VpnPanel is not null && vpnClient.VpnInbound is not null)
        {
            var password = _secretProtector.Unprotect(vpnClient.VpnPanel.EncryptedPassword);
            await _client.ResetClientTrafficAsync(vpnClient.VpnPanel, password, vpnClient.VpnInbound.ExternalInboundId, vpnClient.Uuid, cancellationToken);
        }

        vpnClient.SyncStatus = isSandboxClient ? "sandbox-traffic-reset" : "traffic-reset";
        vpnClient.LastSyncedAt = _clock.UtcNow;
        vpnClient.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken)
    {
        if (IsSandboxMode() || IsSandboxProviderAccessId(providerAccessId))
        {
            var used = providerAccessId.Sum(c => (long)c) * 1024;
            return new VpnUsageSnapshot(providerAccessId, used, 1, _clock.UtcNow);
        }

        var vpnClient = await _db.VpnClients.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.ExternalClientId == providerAccessId || x.Id.ToString() == providerAccessId, cancellationToken);
        if (vpnClient?.VpnPanel is null)
        {
            return new VpnUsageSnapshot(providerAccessId, null, null, _clock.UtcNow);
        }

        var password = _secretProtector.Unprotect(vpnClient.VpnPanel.EncryptedPassword);
        var traffic = await _client.GetClientTrafficAsync(vpnClient.VpnPanel, password, vpnClient.Uuid, cancellationToken);
        return new VpnUsageSnapshot(providerAccessId, traffic.Up + traffic.Down, null, traffic.SyncedAt);
    }

    public async Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken)
    {
        if (IsSandboxMode())
        {
            return HealthStatus.Healthy;
        }

        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.BaseUrl == node.PanelBaseUrl || x.Name == node.Name, cancellationToken)
            ?? await SelectPanelAsync(cancellationToken);
        var password = _secretProtector.Unprotect(panel.EncryptedPassword);
        var health = await _client.CheckHealthAsync(panel, password, cancellationToken);
        return health.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
    }

    private async Task<VpnProvisionResult> CreateOrUpdateRealAccessAsync(VpnProvisionRequest request, bool updateExisting, CancellationToken cancellationToken)
    {
        var panel = await SelectPanelAsync(request, cancellationToken);
        var inbound = await SelectInboundAsync(panel, request, cancellationToken);
        var password = _secretProtector.Unprotect(panel.EncryptedPassword);
        var existing = await _db.VpnClients.FirstOrDefaultAsync(x => x.SubscriptionId == request.SubscriptionId, cancellationToken);
        var uuid = existing?.Uuid ?? Guid.NewGuid().ToString();
        var email = existing?.Email ?? $"u-{request.UserId:N}-{request.SubscriptionId:N}";
        var flow = existing?.Flow ?? ReadFlow(inbound);
        var totalGb = request.TrafficLimit ?? await ResolveTrafficLimitAsync(request.TariffId, cancellationToken);

        if (existing is null)
        {
            var created = await _client.AddClientAsync(panel, password, new X3UiAddClientRequest(inbound.ExternalInboundId, email, uuid, flow, request.MaxDevices, totalGb, request.EndsAt, true), cancellationToken);
            existing = new VpnClient
            {
                UserId = request.UserId,
                SubscriptionId = request.SubscriptionId,
                VpnPanelId = panel.Id,
                VpnInboundId = inbound.Id,
                ExternalClientId = string.IsNullOrWhiteSpace(created.Id) ? uuid : created.Id,
                Email = email,
                Uuid = uuid,
                Flow = flow,
                LimitIp = request.MaxDevices,
                TotalGb = totalGb,
                ExpiryTime = request.EndsAt,
                Enable = true,
                LastSyncedAt = _clock.UtcNow
            };
            _db.VpnClients.Add(existing);
            panel.UsedCapacity += 1;
            inbound.UsedCapacity += 1;
        }
        else
        {
            await _client.UpdateClientAsync(panel, password, new X3UiUpdateClientRequest(inbound.ExternalInboundId, existing.Uuid, existing.Email, existing.Uuid, flow, request.MaxDevices, totalGb, request.EndsAt, true), cancellationToken);
            existing.VpnPanelId = panel.Id;
            existing.VpnInboundId = inbound.Id;
            existing.Flow = flow;
            existing.LimitIp = request.MaxDevices;
            existing.TotalGb = totalGb;
            existing.ExpiryTime = request.EndsAt;
            existing.Enable = true;
            existing.LastSyncedAt = _clock.UtcNow;
            existing.UpdatedAt = _clock.UtcNow;
        }

        var uri = X3UiConfigUriGenerator.BuildUri(panel, inbound, existing);
        if (string.IsNullOrWhiteSpace(uri))
        {
            existing.SyncStatus = "RequiresAdminReview";
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException($"Inbound settings are insufficient to generate {inbound.Protocol} config URI. Access requires admin review.");
        }

        var qr = request.GenerateQrCode
            ? _qrCodeGenerator.GeneratePayload(uri, $"vpn-client:{existing.Id:N}")
            : new QrCodeGenerationResult(uri, null, false, _clock.UtcNow);
        existing.ConfigUri = uri;
        existing.QrCodePayload = request.GenerateQrCode ? qr.Payload : string.Empty;
        existing.SyncStatus = "synced";
        await QueueAccessReadyNotificationAsync(existing, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return new VpnProvisionResult(existing.ExternalClientId, uri, request.GenerateQrCode ? qr.Payload : string.Empty, qr.ImagePath ?? string.Empty);
    }

    private async Task<VpnProvisionResult> CreateOrUpdateSandboxAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
    {
        var protocol = string.IsNullOrWhiteSpace(request.Protocol) ? "vless" : request.Protocol.Trim().ToLowerInvariant();
        var (panel, inbound) = await EnsureSandboxPanelAndInboundAsync(protocol, cancellationToken);
        var accessId = $"x3ui-sandbox-{request.SubscriptionId:N}";
        var uuid = request.SubscriptionId.ToString("D");
        var email = $"sandbox-{request.UserId:N}-{request.SubscriptionId:N}";
        var publicHost = _configuration["Vpn:X3Ui:SandboxPublicHost"] ?? "sandbox-node.local";
        var publicPort = int.TryParse(_configuration["Vpn:X3Ui:SandboxPublicPort"], out var port) ? port : 443;
        var uri = $"{protocol}://{uuid}@{publicHost}:{publicPort}?security=reality&type=tcp#vpn-{request.SubscriptionId:N}";
        var qr = request.GenerateQrCode
            ? _qrCodeGenerator.GeneratePayload(uri, $"vpn-client:{request.SubscriptionId:N}")
            : new QrCodeGenerationResult(uri, null, false, _clock.UtcNow);

        var existing = await _db.VpnClients.FirstOrDefaultAsync(x => x.SubscriptionId == request.SubscriptionId, cancellationToken);
        if (existing is null)
        {
            existing = new VpnClient
            {
                UserId = request.UserId,
                SubscriptionId = request.SubscriptionId,
                VpnPanelId = panel.Id,
                VpnInboundId = inbound.Id,
                ExternalClientId = accessId,
                Email = email,
                Uuid = uuid,
                Flow = string.Empty,
                LimitIp = request.MaxDevices,
                TotalGb = request.TrafficLimit ?? await ResolveTrafficLimitAsync(request.TariffId, cancellationToken),
                ExpiryTime = request.EndsAt,
                Enable = true,
                ConfigUri = uri,
                QrCodePayload = request.GenerateQrCode ? qr.Payload : string.Empty,
                LastSyncedAt = _clock.UtcNow,
                SyncStatus = "sandbox-synced"
            };
            _db.VpnClients.Add(existing);
            panel.UsedCapacity += 1;
            inbound.UsedCapacity += 1;
        }
        else
        {
            existing.VpnPanelId = panel.Id;
            existing.VpnInboundId = inbound.Id;
            existing.ExternalClientId = accessId;
            existing.Email = email;
            existing.Uuid = uuid;
            existing.LimitIp = request.MaxDevices;
            existing.TotalGb = request.TrafficLimit ?? await ResolveTrafficLimitAsync(request.TariffId, cancellationToken);
            existing.ExpiryTime = request.EndsAt;
            existing.Enable = true;
            existing.ConfigUri = uri;
            existing.QrCodePayload = request.GenerateQrCode ? qr.Payload : string.Empty;
            existing.LastSyncedAt = _clock.UtcNow;
            existing.SyncStatus = "sandbox-synced";
            existing.UpdatedAt = _clock.UtcNow;
        }

        await QueueAccessReadyNotificationAsync(existing, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return new VpnProvisionResult(accessId, uri, request.GenerateQrCode ? qr.Payload : string.Empty, $"/artifacts/configs/{accessId}.json");
    }

    private async Task QueueAccessReadyNotificationAsync(VpnClient vpnClient, CancellationToken cancellationToken)
    {
        var accounts = await _db.TelegramAccounts.AsNoTracking()
            .Where(x => x.UserId == vpnClient.UserId && !x.IsBlocked)
            .ToListAsync(cancellationToken);
        if (accounts.Count == 0)
        {
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            text = $"VPN-доступ готов. Подписка {vpnClient.SubscriptionId} активна до {vpnClient.ExpiryTime:O}.\n\n{vpnClient.ConfigUri}",
            replyMarkupJson = "{\"inline_keyboard\":[[{\"text\":\"Мои ключи\",\"callback_data\":\"menu_keys\"},{\"text\":\"Продлить\",\"callback_data\":\"menu_renew\"}],[{\"text\":\"Поддержка\",\"callback_data\":\"support\"}]]}",
            subscriptionId = vpnClient.SubscriptionId,
            clientId = vpnClient.Id
        });

        foreach (var account in accounts)
        {
            var exists = await _db.TelegramBotNotifications.AsNoTracking()
                .AnyAsync(x => x.TelegramUserId == account.TelegramUserId && x.Type == "vpn_access_ready" && x.PayloadJson == payloadJson && x.Status != "failed" && x.Status != "cancelled", cancellationToken);
            if (!exists)
            {
                _db.TelegramBotNotifications.Add(new TelegramBotNotification
                {
                    TelegramUserId = account.TelegramUserId,
                    Type = "vpn_access_ready",
                    PayloadJson = payloadJson,
                    Status = "pending",
                    NextAttemptAt = _clock.UtcNow
                });
            }
        }
    }

    private async Task<VpnPanel> SelectPanelAsync(CancellationToken cancellationToken)
    {
        var panel = await _db.VpnPanels
            .Where(x => x.Status == VpnPanelStatus.Active && x.HealthStatus != HealthStatus.Unhealthy && x.UsedCapacity < x.Capacity)
            .OrderBy(x => x.UsedCapacity * 1.0m / Math.Max(1, x.Capacity))
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return panel ?? throw new InvalidOperationException("Panel not configured: no active 3x-ui VPN panel is available.");
    }

    private async Task<VpnPanel> SelectPanelAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
    {
        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.NodeId, cancellationToken);
        if (node is not null && !string.IsNullOrWhiteSpace(node.PanelBaseUrl))
        {
            var nodePanel = await _db.VpnPanels
                .Where(x => x.BaseUrl == node.PanelBaseUrl && x.Status == VpnPanelStatus.Active && x.HealthStatus != HealthStatus.Unhealthy && x.UsedCapacity < x.Capacity)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (nodePanel is not null)
            {
                return nodePanel;
            }
        }

        return await SelectPanelAsync(cancellationToken);
    }

    private async Task<VpnInbound> SelectInboundAsync(VpnPanel panel, VpnProvisionRequest request, CancellationToken cancellationToken)
    {
        var protocol = string.IsNullOrWhiteSpace(request.Protocol) ? "vless" : request.Protocol.Trim().ToLowerInvariant();
        var inboundSelectionRule = string.IsNullOrWhiteSpace(request.InboundSelectionRule) ? "default" : request.InboundSelectionRule.Trim().ToLowerInvariant();
        var inbound = await _db.VpnInbounds
            .Where(x => x.VpnPanelId == panel.Id && x.IsActive && x.UsedCapacity < x.Capacity && x.Protocol.ToLower() == protocol)
            .OrderByDescending(x => inboundSelectionRule == "default" && x.IsDefault)
            .ThenBy(x => x.UsedCapacity * 1.0m / Math.Max(1, x.Capacity))
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (inbound is not null)
        {
            return inbound;
        }

        if (!panel.AutoCreateInbound)
        {
            throw new InvalidOperationException("Invalid inbound: no active inbound is available and AutoCreateInbound is disabled.");
        }

        if (IsSandboxProvisioning(request))
        {
            var sandboxInbound = new VpnInbound
            {
                VpnPanelId = panel.Id,
                ExternalInboundId = $"sandbox-inbound-{Guid.NewGuid():N}",
                Name = $"Sandbox {protocol.ToUpperInvariant()}",
                Protocol = protocol,
                Port = 443,
                Listen = string.Empty,
                SettingsJson = "{\"clients\":[]}",
                StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"reality\"}",
                SniffingJson = "{}",
                IsDefault = true,
                IsActive = true,
                Capacity = panel.Capacity
            };
            _db.VpnInbounds.Add(sandboxInbound);
            await _db.SaveChangesAsync(cancellationToken);
            return sandboxInbound;
        }

        var password = _secretProtector.Unprotect(panel.EncryptedPassword);
        var template = ParseInboundTemplate(panel.DefaultInboundTemplateJson);
        var remote = await _client.CreateInboundAsync(panel, password, template, cancellationToken);
        inbound = new VpnInbound
        {
            VpnPanelId = panel.Id,
            ExternalInboundId = remote.Id,
            Name = remote.Remark,
            Protocol = remote.Protocol,
            Port = remote.Port,
            Listen = remote.Listen,
            SettingsJson = remote.SettingsJson,
            StreamSettingsJson = remote.StreamSettingsJson,
            SniffingJson = remote.SniffingJson,
            IsDefault = true,
            IsActive = remote.Enable,
            Capacity = 5000
        };
        _db.VpnInbounds.Add(inbound);
        await _db.SaveChangesAsync(cancellationToken);
        return inbound;
    }

    private async Task<(VpnPanel Panel, VpnInbound Inbound)> EnsureSandboxPanelAndInboundAsync(string protocol, CancellationToken cancellationToken)
    {
        protocol = string.IsNullOrWhiteSpace(protocol) ? "vless" : protocol.Trim().ToLowerInvariant();
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Name == "sandbox-x3ui-panel", cancellationToken);
        if (panel is null)
        {
            panel = new VpnPanel
            {
                Name = "sandbox-x3ui-panel",
                BaseUrl = "https://sandbox-node.local",
                Region = "sandbox",
                Status = VpnPanelStatus.Active,
                HealthStatus = HealthStatus.Healthy,
                Login = "sandbox",
                EncryptedPassword = string.Empty,
                Capacity = 100000,
                AutoCreateInbound = true,
                Version = "sandbox",
                LastHealthCheckAt = _clock.UtcNow,
                LastSyncAt = _clock.UtcNow
            };
            _db.VpnPanels.Add(panel);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var inboundId = $"sandbox-default-{protocol}";
        var inbound = await _db.VpnInbounds.FirstOrDefaultAsync(x => x.VpnPanelId == panel.Id && x.ExternalInboundId == inboundId, cancellationToken);
        if (inbound is null)
        {
            inbound = new VpnInbound
            {
                VpnPanelId = panel.Id,
                ExternalInboundId = inboundId,
                Name = $"Sandbox {protocol.ToUpperInvariant()}",
                Protocol = protocol,
                Port = 443,
                Listen = string.Empty,
                SettingsJson = "{\"clients\":[]}",
                StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"reality\"}",
                SniffingJson = "{}",
                IsDefault = true,
                IsActive = true,
                Capacity = panel.Capacity
            };
            _db.VpnInbounds.Add(inbound);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return (panel, inbound);
    }

    private async Task<long?> ResolveTrafficLimitAsync(Guid tariffId, CancellationToken cancellationToken)
    {
        var tariff = await _db.Tariffs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tariffId, cancellationToken);
        return tariff?.TrafficLimit;
    }

    private static X3UiCreateInboundRequest ParseInboundTemplate(string templateJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(templateJson) ? "{}" : templateJson);
            var root = doc.RootElement;
            return new X3UiCreateInboundRequest(
                ReadString(root, "remark", "VPN Platform VLESS"),
                ReadString(root, "protocol", "vless"),
                ReadInt(root, "port", 443),
                ReadString(root, "listen", string.Empty),
                ReadRaw(root, "settings", "{\"clients\":[]}"),
                ReadRaw(root, "streamSettings", "{\"network\":\"tcp\",\"security\":\"none\"}"),
                ReadRaw(root, "sniffing", "{}"),
                true);
        }
        catch
        {
            return new X3UiCreateInboundRequest("VPN Platform VLESS", "vless", 443, string.Empty, "{\"clients\":[]}", "{\"network\":\"tcp\",\"security\":\"none\"}", "{}", true);
        }
    }

    private static string ReadFlow(VpnInbound inbound)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(inbound.SettingsJson) ? "{}" : inbound.SettingsJson);
            if (doc.RootElement.TryGetProperty("clients", out var clients) && clients.ValueKind == JsonValueKind.Array)
            {
                var first = clients.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("flow", out var flow) && flow.ValueKind == JsonValueKind.String)
                {
                    return flow.GetString() ?? string.Empty;
                }
            }
        }
        catch
        {
        }
        return string.Empty;
    }

    private bool IsSandboxProvisioning(VpnProvisionRequest request) => request.UseSandboxProvisioning || IsSandboxMode();

    private bool IsSandboxClient(VpnClient vpnClient)
        => IsSandboxMode()
           || IsSandboxProviderAccessId(vpnClient.ExternalClientId)
           || string.Equals(vpnClient.VpnPanel?.Name, "sandbox-x3ui-panel", StringComparison.OrdinalIgnoreCase);

    private bool IsSandboxMode() => string.Equals(_configuration["Vpn:X3Ui:Mode"], "Sandbox", StringComparison.OrdinalIgnoreCase);

    private static bool IsSandboxProviderAccessId(string? providerAccessId)
        => !string.IsNullOrWhiteSpace(providerAccessId)
           && providerAccessId.StartsWith("x3ui-sandbox-", StringComparison.OrdinalIgnoreCase);

    private static string ReadString(JsonElement root, string propertyName, string fallback)
        => root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;

    private static int ReadInt(JsonElement root, string propertyName, int fallback)
        => root.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;

    private static string ReadRaw(JsonElement root, string propertyName, string fallback)
    {
        if (!root.TryGetProperty(propertyName, out var value)) return fallback;
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : value.GetRawText();
    }
}

public static class X3UiConfigUriGenerator
{
    public static string BuildUri(VpnPanel panel, VpnInbound inbound, VpnClient client)
        => NormalizeProtocol(inbound.Protocol) switch
        {
            "vless" => BuildVlessUri(panel, inbound, client),
            "vmess" => BuildVmessUri(panel, inbound, client),
            "trojan" => BuildTrojanUri(panel, inbound, client),
            _ => string.Empty
        };

    public static string BuildVlessUri(VpnPanel panel, VpnInbound inbound, VpnClient client)
    {
        if (NormalizeProtocol(inbound.Protocol) != "vless" || string.IsNullOrWhiteSpace(client.Uuid) || inbound.Port <= 0)
        {
            return string.Empty;
        }

        var host = ExtractHost(panel.BaseUrl);
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var query = new Dictionary<string, string>
        {
            ["type"] = ReadStreamValue(inbound.StreamSettingsJson, "network", "tcp"),
            ["security"] = ReadStreamValue(inbound.StreamSettingsJson, "security", "none")
        };

        if (!string.IsNullOrWhiteSpace(client.Flow)) query["flow"] = client.Flow;
        var sni = ReadNestedValue(inbound.StreamSettingsJson, "tlsSettings", "serverName")
            ?? ReadNestedValue(inbound.StreamSettingsJson, "realitySettings", "serverNames", firstArrayValue: true)
            ?? ReadNestedValue(inbound.StreamSettingsJson, "realitySettings", "serverName");
        if (!string.IsNullOrWhiteSpace(sni)) query["sni"] = sni;
        var path = ReadNestedValue(inbound.StreamSettingsJson, "wsSettings", "path");
        if (!string.IsNullOrWhiteSpace(path)) query["path"] = path;
        var serviceName = ReadNestedValue(inbound.StreamSettingsJson, "grpcSettings", "serviceName");
        if (!string.IsNullOrWhiteSpace(serviceName)) query["serviceName"] = serviceName;

        var queryString = string.Join('&', query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        var remark = Uri.EscapeDataString(string.IsNullOrWhiteSpace(client.Email) ? $"vpn-{client.SubscriptionId}" : client.Email);
        return $"vless://{client.Uuid}@{host}:{inbound.Port}?{queryString}#{remark}";
    }

    public static string BuildTrojanUri(VpnPanel panel, VpnInbound inbound, VpnClient client)
    {
        if (NormalizeProtocol(inbound.Protocol) != "trojan" || string.IsNullOrWhiteSpace(client.Uuid) || inbound.Port <= 0)
        {
            return string.Empty;
        }

        var host = ExtractHost(panel.BaseUrl);
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var query = BuildSharedTransportQuery(inbound, includeSecurity: true);
        var queryString = string.Join('&', query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        var remark = Uri.EscapeDataString(string.IsNullOrWhiteSpace(client.Email) ? $"vpn-{client.SubscriptionId}" : client.Email);
        return $"trojan://{Uri.EscapeDataString(client.Uuid)}@{host}:{inbound.Port}?{queryString}#{remark}";
    }

    public static string BuildVmessUri(VpnPanel panel, VpnInbound inbound, VpnClient client)
    {
        if (NormalizeProtocol(inbound.Protocol) != "vmess" || string.IsNullOrWhiteSpace(client.Uuid) || inbound.Port <= 0)
        {
            return string.Empty;
        }

        var host = ExtractHost(panel.BaseUrl);
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var network = ReadStreamValue(inbound.StreamSettingsJson, "network", "tcp");
        var security = ReadStreamValue(inbound.StreamSettingsJson, "security", "none");
        var remark = string.IsNullOrWhiteSpace(client.Email) ? $"vpn-{client.SubscriptionId}" : client.Email;
        var vmess = new Dictionary<string, string>
        {
            ["v"] = "2",
            ["ps"] = remark,
            ["add"] = host,
            ["port"] = inbound.Port.ToString(),
            ["id"] = client.Uuid,
            ["aid"] = "0",
            ["scy"] = ReadClientSecurity(inbound.SettingsJson, "auto"),
            ["net"] = network,
            ["type"] = "none",
            ["host"] = ReadNestedValue(inbound.StreamSettingsJson, "wsSettings", "headers", "Host") ?? string.Empty,
            ["path"] = ReadNestedValue(inbound.StreamSettingsJson, "wsSettings", "path") ?? string.Empty,
            ["tls"] = security is "tls" or "reality" ? security : string.Empty,
            ["sni"] = ReadServerName(inbound.StreamSettingsJson) ?? string.Empty
        };

        var json = JsonSerializer.Serialize(vmess);
        return $"vmess://{Convert.ToBase64String(Encoding.UTF8.GetBytes(json))}";
    }

    private static string ExtractHost(string baseUrl)
        => Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;

    private static string NormalizeProtocol(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static Dictionary<string, string> BuildSharedTransportQuery(VpnInbound inbound, bool includeSecurity)
    {
        var query = new Dictionary<string, string>
        {
            ["type"] = ReadStreamValue(inbound.StreamSettingsJson, "network", "tcp")
        };

        if (includeSecurity)
        {
            query["security"] = ReadStreamValue(inbound.StreamSettingsJson, "security", "none");
        }

        var sni = ReadServerName(inbound.StreamSettingsJson);
        if (!string.IsNullOrWhiteSpace(sni)) query["sni"] = sni;
        var path = ReadNestedValue(inbound.StreamSettingsJson, "wsSettings", "path");
        if (!string.IsNullOrWhiteSpace(path)) query["path"] = path;
        var serviceName = ReadNestedValue(inbound.StreamSettingsJson, "grpcSettings", "serviceName");
        if (!string.IsNullOrWhiteSpace(serviceName)) query["serviceName"] = serviceName;
        return query;
    }

    private static string? ReadServerName(string json)
        => ReadNestedValue(json, "tlsSettings", "serverName")
           ?? ReadNestedValue(json, "realitySettings", "serverNames", firstArrayValue: true)
           ?? ReadNestedValue(json, "realitySettings", "serverName");

    private static string ReadStreamValue(string json, string propertyName, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            return doc.RootElement.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static string? ReadNestedValue(string json, string objectName, string propertyName, bool firstArrayValue = false)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (!doc.RootElement.TryGetProperty(objectName, out var obj) || obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(propertyName, out var value)) return null;
            if (firstArrayValue && value.ValueKind == JsonValueKind.Array)
            {
                var first = value.EnumerateArray().FirstOrDefault();
                return first.ValueKind == JsonValueKind.String ? first.GetString() : null;
            }
            return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadNestedValue(string json, string objectName, string nestedObjectName, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (!doc.RootElement.TryGetProperty(objectName, out var obj) || obj.ValueKind != JsonValueKind.Object) return null;
            if (!obj.TryGetProperty(nestedObjectName, out var nested) || nested.ValueKind != JsonValueKind.Object) return null;
            return nested.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ReadClientSecurity(string settingsJson, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson);
            if (!doc.RootElement.TryGetProperty("clients", out var clients) || clients.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var first = clients.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Object)
            {
                return fallback;
            }

            return first.TryGetProperty("security", out var security) && security.ValueKind == JsonValueKind.String
                ? security.GetString() ?? fallback
                : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}

public class VpnProviderFactory : IVpnProviderFactory
{
    private readonly IReadOnlyDictionary<string, IVpnProvider> _providers;

    public VpnProviderFactory(IEnumerable<IVpnProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
    }

    public IVpnProvider Get(string providerName)
        => _providers.TryGetValue(providerName, out var provider)
            ? provider
            : throw new InvalidOperationException($"VPN provider '{providerName}' is not registered.");
}

public class SvgQrCodeGenerator : IQrCodeGenerator
{
    private readonly IClock _clock;

    public SvgQrCodeGenerator(IClock clock)
    {
        _clock = clock;
    }

    public QrCodeGenerationResult GeneratePayload(string configUri, string purpose)
    {
        EnsureConfigUri(configUri);
        return new QrCodeGenerationResult(configUri, null, true, _clock.UtcNow);
    }

    public QrCodeImageResult GenerateSvg(string configUri, string purpose)
    {
        EnsureConfigUri(configUri);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(configUri, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(4);
        return new QrCodeImageResult(configUri, "image/svg+xml", svg, _clock.UtcNow);
    }

    private static void EnsureConfigUri(string configUri)
    {
        if (string.IsNullOrWhiteSpace(configUri))
        {
            throw new InvalidOperationException("Config URI is required for QR generation.");
        }
    }
}

[Obsolete("Use SvgQrCodeGenerator. This alias remains for backward-compatible tests and DI overrides.")]
public sealed class PayloadOnlyQrCodeGenerator : SvgQrCodeGenerator
{
    public PayloadOnlyQrCodeGenerator(IClock clock) : base(clock)
    {
    }
}
