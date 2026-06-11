using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class X3UiPanelService
{
    private readonly IApplicationDbContext _db;
    private readonly IX3UiClient _client;
    private readonly ISecretProtector _secretProtector;
    private readonly IClock _clock;
    private readonly IConfiguration? _configuration;

    public X3UiPanelService(IApplicationDbContext db, IX3UiClient client, ISecretProtector secretProtector, IClock clock, IConfiguration? configuration = null)
    {
        _db = db;
        _client = client;
        _secretProtector = secretProtector;
        _clock = clock;
        _configuration = configuration;
    }

    public async Task<IReadOnlyCollection<VpnPanelDto>> GetPanelsAsync(CancellationToken cancellationToken = default)
    {
        var panels = await _db.VpnPanels.AsNoTracking().OrderBy(x => x.Region).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return panels.Select(MapPanel).ToList();
    }

    public async Task<Result<VpnPanelDto>> GetPanelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var panel = await _db.VpnPanels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return panel is null ? Result<VpnPanelDto>.Failure("VPN panel not found.") : Result<VpnPanelDto>.Success(MapPanel(panel));
    }

    public async Task<Result<VpnPanelDto>> CreatePanelAsync(CreateVpnPanelCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.BaseUrl) || string.IsNullOrWhiteSpace(command.Login))
        {
            return Result<VpnPanelDto>.Failure("Name, BaseUrl and Login are required.");
        }

        if (!Enum.TryParse<VpnSslVerificationMode>(command.SslVerificationMode, true, out var sslMode))
        {
            sslMode = VpnSslVerificationMode.Strict;
        }

        if (!Enum.TryParse<X3UiApiVariant>(command.ApiVariant, true, out var apiVariant))
        {
            apiVariant = X3UiApiVariant.X3UiOfficial;
        }

        var panel = new VpnPanel
        {
            Name = command.Name.Trim(),
            BaseUrl = NormalizeBaseUrl(command.BaseUrl),
            Login = command.Login.Trim(),
            EncryptedPassword = _secretProtector.Protect(command.Password ?? string.Empty),
            Region = string.IsNullOrWhiteSpace(command.Region) ? "default" : command.Region.Trim(),
            Status = VpnPanelStatus.New,
            HealthStatus = HealthStatus.Unknown,
            Capacity = command.Capacity > 0 ? command.Capacity : 5000,
            SslVerificationMode = sslMode,
            ApiVariant = apiVariant,
            AutoCreateInbound = command.AutoCreateInbound,
            DefaultInboundTemplateJson = string.IsNullOrWhiteSpace(command.DefaultInboundTemplateJson) ? "{}" : command.DefaultInboundTemplateJson.Trim()
        };

        _db.VpnPanels.Add(panel);
        await _db.SaveChangesAsync(cancellationToken);

        var health = await CheckHealthAsync(panel.Id, cancellationToken);
        if (health.IsSuccess)
        {
            await SyncPanelAsync(panel.Id, cancellationToken);
        }

        var saved = await _db.VpnPanels.AsNoTracking().FirstAsync(x => x.Id == panel.Id, cancellationToken);
        return Result<VpnPanelDto>.Success(MapPanel(saved));
    }

    public async Task<Result<VpnPanelDto>> UpdatePanelAsync(Guid id, UpdateVpnPanelCommand command, CancellationToken cancellationToken = default)
    {
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (panel is null)
        {
            return Result<VpnPanelDto>.Failure("VPN panel not found.");
        }

        if (!string.IsNullOrWhiteSpace(command.Name)) panel.Name = command.Name.Trim();
        if (!string.IsNullOrWhiteSpace(command.BaseUrl)) panel.BaseUrl = NormalizeBaseUrl(command.BaseUrl);
        if (!string.IsNullOrWhiteSpace(command.Login)) panel.Login = command.Login.Trim();
        if (!string.IsNullOrWhiteSpace(command.Password)) panel.EncryptedPassword = _secretProtector.Protect(command.Password);
        if (!string.IsNullOrWhiteSpace(command.Region)) panel.Region = command.Region.Trim();
        if (command.Capacity.HasValue && command.Capacity.Value > 0) panel.Capacity = command.Capacity.Value;
        if (!string.IsNullOrWhiteSpace(command.SslVerificationMode) && Enum.TryParse<VpnSslVerificationMode>(command.SslVerificationMode, true, out var ssl)) panel.SslVerificationMode = ssl;
        if (!string.IsNullOrWhiteSpace(command.ApiVariant) && Enum.TryParse<X3UiApiVariant>(command.ApiVariant, true, out var variant)) panel.ApiVariant = variant;
        if (command.AutoCreateInbound.HasValue) panel.AutoCreateInbound = command.AutoCreateInbound.Value;
        if (!string.IsNullOrWhiteSpace(command.DefaultInboundTemplateJson)) panel.DefaultInboundTemplateJson = command.DefaultInboundTemplateJson.Trim();
        if (!string.IsNullOrWhiteSpace(command.Status) && Enum.TryParse<VpnPanelStatus>(command.Status, true, out var status)) panel.Status = status;
        panel.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnPanelDto>.Success(MapPanel(panel));
    }

    public async Task<Result<DeleteVpnPanelResultDto>> DeletePanelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (panel is null)
        {
            return Result<DeleteVpnPanelResultDto>.Failure("VPN panel not found.");
        }

        var linkedInbounds = await _db.VpnInbounds.CountAsync(x => x.VpnPanelId == id, cancellationToken);
        var linkedClients = await _db.VpnClients.CountAsync(x => x.VpnPanelId == id, cancellationToken);
        var linkedSyncRuns = await _db.PanelSyncRuns.CountAsync(x => x.VpnPanelId == id, cancellationToken);
        var linkedHealthChecks = await _db.PanelHealthChecks.CountAsync(x => x.VpnPanelId == id, cancellationToken);

        if (linkedInbounds > 0 || linkedClients > 0 || linkedSyncRuns > 0 || linkedHealthChecks > 0)
        {
            panel.Status = VpnPanelStatus.Disabled;
            panel.HealthStatus = HealthStatus.Unknown;
            panel.LastError = "Panel disabled by admin delete action because operational history is linked.";
            panel.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<DeleteVpnPanelResultDto>.Success(new DeleteVpnPanelResultDto(id, Deleted: false, Archived: true, linkedInbounds, linkedClients, linkedSyncRuns, linkedHealthChecks));
        }

        _db.VpnPanels.Remove(panel);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<DeleteVpnPanelResultDto>.Success(new DeleteVpnPanelResultDto(id, Deleted: true, Archived: false, linkedInbounds, linkedClients, linkedSyncRuns, linkedHealthChecks));
    }

    public async Task<Result<PanelHealthCheckDto>> CheckHealthAsync(Guid panelId, CancellationToken cancellationToken = default)
    {
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null)
        {
            return Result<PanelHealthCheckDto>.Failure("VPN panel not found.");
        }

        if (IsSandboxMode())
        {
            var entity = new PanelHealthCheck
            {
                VpnPanelId = panel.Id,
                Status = HealthStatus.Healthy,
                LatencyMs = 0,
                Version = "sandbox",
                ErrorMessage = "sandbox mode active - no live panel call",
                CheckedAt = _clock.UtcNow
            };
            _db.PanelHealthChecks.Add(entity);
            panel.HealthStatus = HealthStatus.Healthy;
            panel.LastHealthCheckAt = entity.CheckedAt;
            panel.LastError = string.Empty;
            panel.Version = "sandbox";
            if (panel.Status == VpnPanelStatus.New || panel.Status == VpnPanelStatus.Error) panel.Status = VpnPanelStatus.Active;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelHealthCheckDto>.Success(MapHealth(entity));
        }

        if (string.IsNullOrWhiteSpace(panel.BaseUrl) || string.IsNullOrWhiteSpace(panel.Login) || string.IsNullOrWhiteSpace(panel.EncryptedPassword))
        {
            return Result<PanelHealthCheckDto>.Failure("Panel not configured: base URL, login and password are required.");
        }

        var password = _secretProtector.Unprotect(panel.EncryptedPassword);
        var sw = Stopwatch.StartNew();
        try
        {
            var health = await _client.CheckHealthAsync(panel, password, cancellationToken);
            sw.Stop();
            var entity = new PanelHealthCheck
            {
                VpnPanelId = panel.Id,
                Status = health.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                LatencyMs = health.LatencyMs == 0 ? sw.ElapsedMilliseconds : health.LatencyMs,
                Version = health.Version,
                ErrorMessage = health.ErrorMessage ?? string.Empty,
                CheckedAt = _clock.UtcNow
            };
            _db.PanelHealthChecks.Add(entity);
            panel.HealthStatus = entity.Status;
            panel.LastHealthCheckAt = entity.CheckedAt;
            panel.LastError = entity.ErrorMessage;
            panel.Version = entity.Version;
            panel.Status = entity.Status == HealthStatus.Healthy && panel.Status == VpnPanelStatus.New ? VpnPanelStatus.Active : panel.Status;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelHealthCheckDto>.Success(MapHealth(entity));
        }
        catch (Exception ex)
        {
            sw.Stop();
            var entity = new PanelHealthCheck
            {
                VpnPanelId = panel.Id,
                Status = HealthStatus.Unhealthy,
                LatencyMs = sw.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                CheckedAt = _clock.UtcNow
            };
            _db.PanelHealthChecks.Add(entity);
            panel.HealthStatus = HealthStatus.Unhealthy;
            panel.LastHealthCheckAt = entity.CheckedAt;
            panel.LastError = ex.Message;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelHealthCheckDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<PanelSyncRunDto>> SyncPanelAsync(Guid panelId, CancellationToken cancellationToken = default)
    {
        var panel = await _db.VpnPanels.Include(x => x.Inbounds).FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null)
        {
            return Result<PanelSyncRunDto>.Failure("VPN panel not found.");
        }

        var run = new PanelSyncRun
        {
            VpnPanelId = panel.Id,
            Status = PanelSyncRunStatus.Running,
            StartedAt = _clock.UtcNow
        };
        _db.PanelSyncRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            if (IsSandboxMode())
            {
                run.Status = PanelSyncRunStatus.Succeeded;
                run.FinishedAt = _clock.UtcNow;
                run.SummaryJson = JsonSerializer.Serialize(new
                {
                    mode = "sandbox",
                    networkCalls = 0,
                    inboundCount = await _db.VpnInbounds.CountAsync(x => x.VpnPanelId == panel.Id, cancellationToken),
                    clientCount = await _db.VpnClients.CountAsync(x => x.VpnPanelId == panel.Id, cancellationToken)
                });
                AddSyncEvent(run, "sandbox_sync", "VpnPanel", panel.Id, panel.Id.ToString(), "Sandbox sync completed without live 3x-ui network calls.", run.SummaryJson);
                panel.LastSyncAt = run.FinishedAt;
                panel.LastError = string.Empty;
                await _db.SaveChangesAsync(cancellationToken);
                return Result<PanelSyncRunDto>.Success(MapSyncRun(run));
            }

            if (string.IsNullOrWhiteSpace(panel.BaseUrl) || string.IsNullOrWhiteSpace(panel.Login) || string.IsNullOrWhiteSpace(panel.EncryptedPassword))
            {
                return Result<PanelSyncRunDto>.Failure("Panel not configured: base URL, login and password are required.");
            }

            var password = _secretProtector.Unprotect(panel.EncryptedPassword);
            var remoteInbounds = await _client.GetInboundsAsync(panel, password, cancellationToken);
            var remoteIds = remoteInbounds.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var created = 0;
            var updated = 0;

            foreach (var remote in remoteInbounds)
            {
                var local = await _db.VpnInbounds.FirstOrDefaultAsync(x => x.VpnPanelId == panel.Id && x.ExternalInboundId == remote.Id, cancellationToken);
                if (local is null)
                {
                    local = new VpnInbound
                    {
                        VpnPanelId = panel.Id,
                        ExternalInboundId = remote.Id,
                        IsDefault = !await _db.VpnInbounds.AnyAsync(x => x.VpnPanelId == panel.Id && x.IsDefault, cancellationToken)
                    };
                    _db.VpnInbounds.Add(local);
                    created += 1;
                    AddSyncEvent(run, "inbound_created", "VpnInbound", local.Id, remote.Id, $"Inbound {remote.Id} imported from panel.", JsonSerializer.Serialize(remote));
                }
                else
                {
                    updated += 1;
                    AddSyncEvent(run, "inbound_updated", "VpnInbound", local.Id, remote.Id, $"Inbound {remote.Id} updated from panel.", JsonSerializer.Serialize(remote));
                }

                local.Name = string.IsNullOrWhiteSpace(remote.Remark) ? $"Inbound {remote.Id}" : remote.Remark;
                local.Protocol = remote.Protocol;
                local.Port = remote.Port;
                local.Listen = remote.Listen;
                local.SettingsJson = remote.SettingsJson;
                local.StreamSettingsJson = remote.StreamSettingsJson;
                local.SniffingJson = remote.SniffingJson;
                local.IsActive = remote.Enable;
                local.UpdatedAt = _clock.UtcNow;
            }

            var missing = await _db.VpnInbounds.Where(x => x.VpnPanelId == panel.Id && !remoteIds.Contains(x.ExternalInboundId)).ToListAsync(cancellationToken);
            foreach (var inbound in missing)
            {
                inbound.IsActive = false;
                inbound.UpdatedAt = _clock.UtcNow;
                AddSyncEvent(run, "inbound_missing", "VpnInbound", inbound.Id, inbound.ExternalInboundId, "Inbound exists in DB but is missing on panel.", "{}");
            }

            await DetectClientDiffsAsync(panel.Id, run, remoteInbounds, cancellationToken);

            run.Status = PanelSyncRunStatus.Succeeded;
            run.FinishedAt = _clock.UtcNow;
            run.SummaryJson = JsonSerializer.Serialize(new { created, updated, missing = missing.Count });
            panel.LastSyncAt = run.FinishedAt;
            panel.LastError = string.Empty;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelSyncRunDto>.Success(MapSyncRun(run));
        }
        catch (Exception ex)
        {
            run.Status = PanelSyncRunStatus.Failed;
            run.FinishedAt = _clock.UtcNow;
            run.ErrorMessage = ex.Message;
            panel.LastError = ex.Message;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelSyncRunDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<VpnInboundDto>> CreateInboundAsync(Guid panelId, CreateVpnInboundCommand command, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateInboundCommand(command);
        if (validationError is not null)
        {
            return Result<VpnInboundDto>.Failure(validationError);
        }

        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null)
        {
            return Result<VpnInboundDto>.Failure("VPN panel not found.");
        }

        X3UiInboundDto remote;
        if (IsSandboxMode())
        {
            var nextNumber = await _db.VpnInbounds.CountAsync(x => x.VpnPanelId == panel.Id, cancellationToken) + 1;
            remote = new X3UiInboundDto($"sandbox-inbound-{nextNumber}", command.Name, NormalizeProtocol(command.Protocol), command.Port, command.Listen, command.SettingsJson, command.StreamSettingsJson, command.SniffingJson, command.IsActive);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(panel.BaseUrl) || string.IsNullOrWhiteSpace(panel.Login) || string.IsNullOrWhiteSpace(panel.EncryptedPassword))
            {
                return Result<VpnInboundDto>.Failure("Panel not configured: base URL, login and password are required.");
            }
            var password = _secretProtector.Unprotect(panel.EncryptedPassword);
            remote = await _client.CreateInboundAsync(panel, password, new X3UiCreateInboundRequest(command.Name, NormalizeProtocol(command.Protocol), command.Port, command.Listen, command.SettingsJson, command.StreamSettingsJson, command.SniffingJson, command.IsActive), cancellationToken);
        }

        var inbound = new VpnInbound
        {
            VpnPanelId = panel.Id,
            ExternalInboundId = remote.Id,
            Name = string.IsNullOrWhiteSpace(remote.Remark) ? command.Name : remote.Remark,
            Protocol = remote.Protocol,
            Port = remote.Port,
            Listen = remote.Listen,
            SettingsJson = remote.SettingsJson,
            StreamSettingsJson = remote.StreamSettingsJson,
            SniffingJson = remote.SniffingJson,
            IsDefault = command.IsDefault,
            IsActive = remote.Enable,
            Capacity = command.Capacity > 0 ? command.Capacity : 5000
        };
        if (command.IsDefault)
        {
            var defaults = await _db.VpnInbounds.Where(x => x.VpnPanelId == panel.Id && x.IsDefault).ToListAsync(cancellationToken);
            foreach (var item in defaults) item.IsDefault = false;
        }
        _db.VpnInbounds.Add(inbound);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnInboundDto>.Success(MapInbound(inbound));
    }

    public async Task<IReadOnlyCollection<VpnInboundDto>> GetInboundsAsync(Guid panelId, CancellationToken cancellationToken = default)
    {
        var inbounds = await _db.VpnInbounds.AsNoTracking().Where(x => x.VpnPanelId == panelId).OrderByDescending(x => x.IsDefault).ThenBy(x => x.Port).ToListAsync(cancellationToken);
        return inbounds.Select(MapInbound).ToList();
    }

    public async Task<Result<VpnInboundDto>> PatchInboundAsync(Guid inboundId, CreateVpnInboundCommand command, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateInboundCommand(command);
        if (validationError is not null)
        {
            return Result<VpnInboundDto>.Failure(validationError);
        }

        var inbound = await _db.VpnInbounds.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.Id == inboundId, cancellationToken);
        if (inbound?.VpnPanel is null)
        {
            return Result<VpnInboundDto>.Failure("VPN inbound not found.");
        }

        X3UiInboundDto remote;
        if (IsSandboxMode())
        {
            remote = new X3UiInboundDto(inbound.ExternalInboundId, command.Name, NormalizeProtocol(command.Protocol), command.Port, command.Listen, command.SettingsJson, command.StreamSettingsJson, command.SniffingJson, command.IsActive);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(inbound.VpnPanel.BaseUrl) || string.IsNullOrWhiteSpace(inbound.VpnPanel.Login) || string.IsNullOrWhiteSpace(inbound.VpnPanel.EncryptedPassword))
            {
                return Result<VpnInboundDto>.Failure("Panel not configured: base URL, login and password are required.");
            }
            var password = _secretProtector.Unprotect(inbound.VpnPanel.EncryptedPassword);
            remote = await _client.UpdateInboundAsync(inbound.VpnPanel, password, new X3UiUpdateInboundRequest(inbound.ExternalInboundId, command.Name, NormalizeProtocol(command.Protocol), command.Port, command.Listen, command.SettingsJson, command.StreamSettingsJson, command.SniffingJson, command.IsActive), cancellationToken);
        }

        inbound.Name = remote.Remark;
        inbound.Protocol = remote.Protocol;
        inbound.Port = remote.Port;
        inbound.Listen = remote.Listen;
        inbound.SettingsJson = remote.SettingsJson;
        inbound.StreamSettingsJson = remote.StreamSettingsJson;
        inbound.SniffingJson = remote.SniffingJson;
        inbound.IsActive = remote.Enable;
        inbound.IsDefault = command.IsDefault && inbound.IsActive;
        inbound.Capacity = command.Capacity > 0 ? command.Capacity : inbound.Capacity;
        inbound.UpdatedAt = _clock.UtcNow;
        if (inbound.IsDefault)
        {
            var defaults = await _db.VpnInbounds.Where(x => x.VpnPanelId == inbound.VpnPanelId && x.Id != inbound.Id && x.IsDefault).ToListAsync(cancellationToken);
            foreach (var item in defaults)
            {
                item.IsDefault = false;
                item.UpdatedAt = _clock.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnInboundDto>.Success(MapInbound(inbound));
    }

    public async Task<Result<VpnInboundDto>> SetDefaultInboundAsync(Guid inboundId, CancellationToken cancellationToken = default)
    {
        var inbound = await _db.VpnInbounds.FirstOrDefaultAsync(x => x.Id == inboundId, cancellationToken);
        if (inbound is null)
        {
            return Result<VpnInboundDto>.Failure("VPN inbound not found.");
        }
        if (!inbound.IsActive)
        {
            return Result<VpnInboundDto>.Failure("Inactive inbound cannot be default.");
        }

        var all = await _db.VpnInbounds.Where(x => x.VpnPanelId == inbound.VpnPanelId).ToListAsync(cancellationToken);
        foreach (var item in all)
        {
            item.IsDefault = item.Id == inbound.Id;
            item.UpdatedAt = _clock.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnInboundDto>.Success(MapInbound(inbound));
    }

    private static string? ValidateInboundCommand(CreateVpnInboundCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return "Inbound name is required.";
        }
        if (!IsSupportedInboundProtocol(command.Protocol))
        {
            return "Inbound protocol must be vless, vmess or trojan.";
        }
        if (command.Port is < 1 or > 65535)
        {
            return "Inbound port must be between 1 and 65535.";
        }
        if (command.Capacity < 1)
        {
            return "Inbound capacity must be greater than zero.";
        }
        if (command.IsDefault && !command.IsActive)
        {
            return "Inactive inbound cannot be default.";
        }

        var settingsError = ValidateJsonObject(command.SettingsJson, "settingsJson");
        if (settingsError is not null) return settingsError;
        var streamError = ValidateJsonObject(command.StreamSettingsJson, "streamSettingsJson");
        if (streamError is not null) return streamError;
        var sniffingError = ValidateJsonObject(command.SniffingJson, "sniffingJson");
        if (sniffingError is not null) return sniffingError;

        using var streamSettings = JsonDocument.Parse(command.StreamSettingsJson);
        if (!streamSettings.RootElement.TryGetProperty("network", out var network) || network.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(network.GetString()))
        {
            return "streamSettingsJson must contain a non-empty network value.";
        }

        return null;
    }

    private static string? ValidateJsonObject(string json, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return $"{fieldName} must be a JSON object.";
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object ? null : $"{fieldName} must be a JSON object.";
        }
        catch (JsonException)
        {
            return $"{fieldName} must be a valid JSON object.";
        }
    }

    private static bool IsSupportedInboundProtocol(string protocol)
        => NormalizeProtocol(protocol) is "vless" or "vmess" or "trojan";

    private static string NormalizeProtocol(string protocol)
        => string.IsNullOrWhiteSpace(protocol) ? string.Empty : protocol.Trim().ToLowerInvariant();

    public async Task<IReadOnlyCollection<VpnClientDto>> GetClientsAsync(Guid panelId, CancellationToken cancellationToken = default)
    {
        var clients = await _db.VpnClients.AsNoTracking().Where(x => x.VpnPanelId == panelId).ToListAsync(cancellationToken);
        return clients.OrderByDescending(x => x.CreatedAt).Select(MapClient).ToList();
    }

    public Task<Result<VpnClientDto>> EnableClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, true, cancellationToken);

    public Task<Result<VpnClientDto>> DisableClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, false, cancellationToken);

    public async Task<Result<VpnClientDto>> SyncClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await LoadClientForActionAsync(clientId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        if (!IsSandboxMode())
        {
            var configurationError = ValidatePanelCredentials(client.VpnPanel);
            if (configurationError is not null)
            {
                return Result<VpnClientDto>.Failure(configurationError);
            }

            var password = _secretProtector.Unprotect(client.VpnPanel.EncryptedPassword);
            await _client.GetClientTrafficAsync(client.VpnPanel, password, client.Uuid, cancellationToken);
        }

        client.SyncStatus = IsSandboxMode() ? "sandbox-synced" : "synced";
        client.LastSyncedAt = _clock.UtcNow;
        client.UpdatedAt = _clock.UtcNow;
        await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnClientDto>.Success(MapClient(client));
    }

    public async Task<Result<VpnClientDto>> ResetClientTrafficAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await LoadClientForActionAsync(clientId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        if (!IsSandboxMode())
        {
            var configurationError = ValidatePanelCredentials(client.VpnPanel);
            if (configurationError is not null)
            {
                return Result<VpnClientDto>.Failure(configurationError);
            }

            var password = _secretProtector.Unprotect(client.VpnPanel.EncryptedPassword);
            await _client.ResetClientTrafficAsync(client.VpnPanel, password, client.VpnInbound.ExternalInboundId, client.Uuid, cancellationToken);
        }

        client.SyncStatus = IsSandboxMode() ? "sandbox-traffic-reset" : "traffic-reset";
        client.LastSyncedAt = _clock.UtcNow;
        client.UpdatedAt = _clock.UtcNow;
        await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnClientDto>.Success(MapClient(client));
    }

    public async Task<Result<VpnClientDto>> MigrateClientAsync(Guid clientId, MigrateVpnClientCommand command, CancellationToken cancellationToken = default)
    {
        var client = await LoadClientForActionAsync(clientId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        var targetInbound = await _db.VpnInbounds.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.Id == command.TargetInboundId, cancellationToken);
        if (targetInbound?.VpnPanel is null)
        {
            return Result<VpnClientDto>.Failure("Target inbound not found.");
        }
        if (!targetInbound.IsActive)
        {
            return Result<VpnClientDto>.Failure("Target inbound is inactive.");
        }
        if (!string.Equals(targetInbound.Protocol, client.VpnInbound.Protocol, StringComparison.OrdinalIgnoreCase))
        {
            return Result<VpnClientDto>.Failure("Target inbound protocol must match the client protocol.");
        }
        if (targetInbound.Id == client.VpnInboundId)
        {
            client.SyncStatus = "already-on-target";
            client.LastSyncedAt = _clock.UtcNow;
            client.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VpnClientDto>.Success(MapClient(client));
        }

        var sourceInbound = client.VpnInbound;
        var sourcePanel = client.VpnPanel;
        X3UiClientDto? remote = null;
        if (!IsSandboxMode())
        {
            var sourceConfigurationError = ValidatePanelCredentials(sourcePanel);
            var targetConfigurationError = ValidatePanelCredentials(targetInbound.VpnPanel);
            if (sourceConfigurationError is not null) return Result<VpnClientDto>.Failure(sourceConfigurationError);
            if (targetConfigurationError is not null) return Result<VpnClientDto>.Failure(targetConfigurationError);

            var targetPassword = _secretProtector.Unprotect(targetInbound.VpnPanel.EncryptedPassword);
            remote = await _client.AddClientAsync(targetInbound.VpnPanel, targetPassword, new X3UiAddClientRequest(targetInbound.ExternalInboundId, client.Email, client.Uuid, client.Flow, client.LimitIp, client.TotalGb, client.ExpiryTime, client.Enable), cancellationToken);
            var sourcePassword = _secretProtector.Unprotect(sourcePanel.EncryptedPassword);
            await _client.DeleteClientAsync(sourcePanel, sourcePassword, sourceInbound.ExternalInboundId, client.Uuid, cancellationToken);
        }

        if (sourceInbound.UsedCapacity > 0) sourceInbound.UsedCapacity -= 1;
        targetInbound.UsedCapacity += 1;
        if (sourcePanel.Id != targetInbound.VpnPanelId)
        {
            if (sourcePanel.UsedCapacity > 0) sourcePanel.UsedCapacity -= 1;
            targetInbound.VpnPanel.UsedCapacity += 1;
        }

        client.VpnPanelId = targetInbound.VpnPanelId;
        client.VpnInboundId = targetInbound.Id;
        client.ExternalClientId = string.IsNullOrWhiteSpace(remote?.Id) ? client.ExternalClientId : remote.Id;
        client.ConfigUri = BuildClientConfigUri(targetInbound.VpnPanel, targetInbound, client);
        client.QrCodePayload = string.IsNullOrWhiteSpace(client.ConfigUri) ? string.Empty : client.ConfigUri;
        client.SyncStatus = IsSandboxMode() ? "sandbox-migrated" : "migrated";
        client.LastSyncedAt = _clock.UtcNow;
        client.UpdatedAt = _clock.UtcNow;
        await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnClientDto>.Success(MapClient(client));
    }

    public async Task<IReadOnlyCollection<PanelSyncRunDto>> GetSyncRunsAsync(Guid panelId, CancellationToken cancellationToken = default)
    {
        var runs = await _db.PanelSyncRuns.AsNoTracking().Where(x => x.VpnPanelId == panelId).ToListAsync(cancellationToken);
        return runs.OrderByDescending(x => x.StartedAt).Take(50).Select(MapSyncRun).ToList();
    }

    public async Task<IReadOnlyCollection<PanelSyncEventDto>> GetSyncEventsAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var events = await _db.PanelSyncEvents.AsNoTracking().Where(x => x.PanelSyncRunId == runId).ToListAsync(cancellationToken);
        return events.OrderBy(x => x.CreatedAt).Select(MapSyncEvent).ToList();
    }

    public async Task<IReadOnlyCollection<PanelHealthCheckDto>> GetHealthChecksAsync(Guid panelId, CancellationToken cancellationToken = default)
    {
        var checks = await _db.PanelHealthChecks.AsNoTracking().Where(x => x.VpnPanelId == panelId).ToListAsync(cancellationToken);
        return checks.OrderByDescending(x => x.CheckedAt).Take(50).Select(MapHealth).ToList();
    }

    private async Task<Result<VpnClientDto>> SetClientEnabledAsync(Guid clientId, bool enabled, CancellationToken cancellationToken)
    {
        var client = await LoadClientForActionAsync(clientId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        if (!IsSandboxMode())
        {
            var configurationError = ValidatePanelCredentials(client.VpnPanel);
            if (configurationError is not null)
            {
                return Result<VpnClientDto>.Failure(configurationError);
            }

            var password = _secretProtector.Unprotect(client.VpnPanel.EncryptedPassword);
            await _client.UpdateClientAsync(client.VpnPanel, password, new X3UiUpdateClientRequest(client.VpnInbound.ExternalInboundId, client.Uuid, client.Email, client.Uuid, client.Flow, client.LimitIp, client.TotalGb, client.ExpiryTime, enabled), cancellationToken);
        }

        client.Enable = enabled;
        client.SyncStatus = IsSandboxMode()
            ? enabled ? "sandbox-enabled" : "sandbox-disabled"
            : enabled ? "enabled" : "disabled";
        client.LastSyncedAt = _clock.UtcNow;
        client.UpdatedAt = _clock.UtcNow;
        await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnClientDto>.Success(MapClient(client));
    }

    private Task<VpnClient?> LoadClientForActionAsync(Guid clientId, CancellationToken cancellationToken)
        => _db.VpnClients.Include(x => x.VpnPanel).Include(x => x.VpnInbound).FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken);

    private static string? ValidatePanelCredentials(VpnPanel panel)
        => string.IsNullOrWhiteSpace(panel.BaseUrl) || string.IsNullOrWhiteSpace(panel.Login) || string.IsNullOrWhiteSpace(panel.EncryptedPassword)
            ? "Panel not configured: base URL, login and password are required."
            : null;

    private async Task UpdateLinkedAccessCredentialsAsync(VpnClient client, CancellationToken cancellationToken)
    {
        var accesses = await _db.AccessCredentials
            .Where(x => x.SubscriptionId == client.SubscriptionId && (x.ProviderAccessId == client.ExternalClientId || x.ProviderAccessId == client.Id.ToString()))
            .ToListAsync(cancellationToken);

        foreach (var access in accesses)
        {
            access.ProviderAccessId = client.ExternalClientId;
            access.AccessUri = client.ConfigUri;
            access.QrCodePath = client.QrCodePayload;
            var targetStatus = client.Enable ? AccessCredentialStatus.Active : AccessCredentialStatus.Disabled;
            var statusResult = StatusStateMachine.TrySetAccessStatus(access, targetStatus, _clock.UtcNow);
            if (!statusResult.IsSuccess)
            {
                continue;
            }

            access.DisabledAt = client.Enable ? null : _clock.UtcNow;
            access.LastSyncedAt = client.LastSyncedAt;
            access.Revision += 1;
        }
    }

    private static string BuildClientConfigUri(VpnPanel panel, VpnInbound inbound, VpnClient client)
    {
        var host = ExtractHost(panel.BaseUrl);
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(client.Uuid) || inbound.Port <= 0)
        {
            return client.ConfigUri;
        }

        var protocol = NormalizeProtocol(inbound.Protocol);
        var network = ReadJsonString(inbound.StreamSettingsJson, "network", "tcp");
        var security = ReadJsonString(inbound.StreamSettingsJson, "security", "none");
        var remark = Uri.EscapeDataString(string.IsNullOrWhiteSpace(client.Email) ? $"vpn-{client.SubscriptionId}" : client.Email);

        if (protocol is "vless")
        {
            var query = $"type={Uri.EscapeDataString(network)}&security={Uri.EscapeDataString(security)}";
            if (!string.IsNullOrWhiteSpace(client.Flow))
            {
                query += $"&flow={Uri.EscapeDataString(client.Flow)}";
            }
            return $"vless://{client.Uuid}@{host}:{inbound.Port}?{query}#{remark}";
        }

        if (protocol is "trojan")
        {
            var query = $"type={Uri.EscapeDataString(network)}&security={Uri.EscapeDataString(security)}";
            return $"trojan://{Uri.EscapeDataString(client.Uuid)}@{host}:{inbound.Port}?{query}#{remark}";
        }

        if (protocol is "vmess")
        {
            var vmess = new Dictionary<string, string>
            {
                ["v"] = "2",
                ["ps"] = string.IsNullOrWhiteSpace(client.Email) ? $"vpn-{client.SubscriptionId}" : client.Email,
                ["add"] = host,
                ["port"] = inbound.Port.ToString(),
                ["id"] = client.Uuid,
                ["aid"] = "0",
                ["scy"] = "auto",
                ["net"] = network,
                ["type"] = "none",
                ["host"] = string.Empty,
                ["path"] = string.Empty,
                ["tls"] = security is "tls" or "reality" ? security : string.Empty
            };
            return $"vmess://{Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(vmess)))}";
        }

        return client.ConfigUri;
    }

    private static string ExtractHost(string baseUrl)
        => Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;

    private static string ReadJsonString(string json, string propertyName, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            return doc.RootElement.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? fallback
                : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private async Task DetectClientDiffsAsync(Guid panelId, PanelSyncRun run, IReadOnlyCollection<X3UiInboundDto> remoteInbounds, CancellationToken cancellationToken)
    {
        var localClients = await _db.VpnClients.AsNoTracking().Where(x => x.VpnPanelId == panelId).ToListAsync(cancellationToken);
        var remoteClients = new List<X3UiClientDto>();
        foreach (var inbound in remoteInbounds)
        {
            remoteClients.AddRange(ParseClientsFromInbound(inbound));
        }

        foreach (var remote in remoteClients)
        {
            var local = localClients.FirstOrDefault(x => string.Equals(x.Uuid, remote.Uuid, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Email, remote.Email, StringComparison.OrdinalIgnoreCase));
            if (local is null)
            {
                AddSyncEvent(run, "orphan_client", "VpnClient", null, remote.Uuid, $"Client {remote.Email} exists on panel but not in DB.", JsonSerializer.Serialize(remote));
                continue;
            }

            if (Math.Abs((local.ExpiryTime - remote.ExpiryTime).TotalMinutes) > 5)
            {
                AddSyncEvent(run, "expiry_mismatch", "VpnClient", local.Id, remote.Uuid, $"Client {remote.Email} expiry differs.", JsonSerializer.Serialize(new { local = local.ExpiryTime, remote = remote.ExpiryTime }));
            }

            if (local.Enable != remote.Enable)
            {
                AddSyncEvent(run, "enabled_mismatch", "VpnClient", local.Id, remote.Uuid, $"Client {remote.Email} enabled flag differs.", JsonSerializer.Serialize(new { local = local.Enable, remote = remote.Enable }));
            }
        }

        foreach (var local in localClients)
        {
            var found = remoteClients.Any(x => string.Equals(x.Uuid, local.Uuid, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Email, local.Email, StringComparison.OrdinalIgnoreCase));
            if (!found)
            {
                AddSyncEvent(run, "missing_client", "VpnClient", local.Id, local.Uuid, $"Client {local.Email} exists in DB but is missing on panel.", JsonSerializer.Serialize(new
                {
                    local.Id,
                    local.UserId,
                    local.SubscriptionId,
                    local.VpnPanelId,
                    local.VpnInboundId,
                    local.ExternalClientId,
                    local.Email,
                    local.Uuid,
                    local.ExpiryTime,
                    local.Enable,
                    local.SyncStatus,
                    local.LastSyncedAt
                }));
            }
        }
    }

    public static IReadOnlyCollection<X3UiClientDto> ParseClientsFromInbound(X3UiInboundDto inbound)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(inbound.SettingsJson) ? "{}" : inbound.SettingsJson);
            if (!doc.RootElement.TryGetProperty("clients", out var clients) || clients.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<X3UiClientDto>();
            }

            return clients.EnumerateArray().Select(client =>
            {
                var expiry = client.TryGetProperty("expiryTime", out var expiryElement) && expiryElement.TryGetInt64(out var ms)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ms)
                    : DateTimeOffset.MinValue;
                return new X3UiClientDto(
                    ReadString(client, "id"),
                    ReadString(client, "email"),
                    ReadString(client, "id"),
                    ReadString(client, "flow"),
                    ReadInt(client, "limitIp"),
                    ReadLongNullable(client, "totalGB"),
                    expiry,
                    !client.TryGetProperty("enable", out var enableElement) || enableElement.ValueKind != JsonValueKind.False,
                    null,
                    null);
            }).ToList();
        }
        catch
        {
            return Array.Empty<X3UiClientDto>();
        }
    }

    private void AddSyncEvent(PanelSyncRun run, string eventType, string entityType, Guid? entityId, string externalId, string message, string payloadJson)
        => _db.PanelSyncEvents.Add(new PanelSyncEvent
        {
            PanelSyncRunId = run.Id,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            ExternalId = externalId,
            Message = message,
            PayloadJson = payloadJson
        });

    private bool IsSandboxMode() => string.Equals(_configuration?["Vpn:X3Ui:Mode"], "Sandbox", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeBaseUrl(string baseUrl) => baseUrl.Trim().TrimEnd('/');

    private static string ReadString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static int ReadInt(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private static long? ReadLongNullable(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var parsed) ? parsed : null;

    private static VpnPanelDto MapPanel(VpnPanel x)
        => new(x.Id, x.Name, x.BaseUrl, x.Region, x.Status.ToString(), x.HealthStatus.ToString(), x.Login, x.SslVerificationMode.ToString(), x.ApiVariant.ToString(), x.Capacity, x.UsedCapacity, x.AutoCreateInbound, x.DefaultInboundTemplateJson, x.LastHealthCheckAt, x.LastSyncAt, x.Version, x.LastError, x.CreatedAt, x.UpdatedAt);

    private static VpnInboundDto MapInbound(VpnInbound x)
        => new(x.Id, x.VpnPanelId, x.ExternalInboundId, x.Name, x.Protocol, x.Port, x.Listen, x.SettingsJson, x.StreamSettingsJson, x.SniffingJson, x.IsDefault, x.IsActive, x.Capacity, x.UsedCapacity);

    private static VpnClientDto MapClient(VpnClient x)
        => new(x.Id, x.UserId, x.SubscriptionId, x.VpnPanelId, x.VpnInboundId, x.ExternalClientId, x.Email, x.Uuid, x.Flow, x.LimitIp, x.TotalGb, x.ExpiryTime, x.Enable, x.ConfigUri, x.QrCodePayload, x.SyncStatus, x.LastSyncedAt);

    private static PanelHealthCheckDto MapHealth(PanelHealthCheck x)
        => new(x.Id, x.VpnPanelId, x.Status.ToString(), x.LatencyMs, x.Version, x.ErrorMessage, x.CheckedAt);

    private static PanelSyncRunDto MapSyncRun(PanelSyncRun x)
        => new(x.Id, x.VpnPanelId, x.Status.ToString(), x.StartedAt, x.FinishedAt, x.SummaryJson, x.ErrorMessage);

    private static PanelSyncEventDto MapSyncEvent(PanelSyncEvent x)
        => new(x.Id, x.PanelSyncRunId, x.EventType, x.EntityType, x.EntityId, x.ExternalId, x.Message, x.PayloadJson);
}
