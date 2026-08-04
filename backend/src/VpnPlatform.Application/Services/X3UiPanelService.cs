using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class X3UiPanelService
{
    private static readonly TimeSpan SyncLeaseDuration = TimeSpan.FromMinutes(5);

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

    public Task<Result<VpnPanelDto>> CreatePanelAsync(CreateVpnPanelCommand command, CancellationToken cancellationToken = default)
        => CreatePanelAsync(command, null, cancellationToken);

    public async Task<Result<VpnPanelDto>> CreatePanelAsync(CreateVpnPanelCommand command, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var validationError = ValidatePanelCommand(
            command.Name,
            command.BaseUrl,
            command.Login,
            command.Password,
            command.Capacity,
            command.SslVerificationMode,
            command.ApiVariant,
            command.DefaultInboundTemplateJson,
            status: null,
            passwordRequired: true);
        if (validationError is not null)
        {
            return Result<VpnPanelDto>.Failure(validationError);
        }

        var name = command.Name.Trim();
        var baseUrl = NormalizeBaseUrl(command.BaseUrl);
        if (await _db.VpnPanels.AnyAsync(
                x => x.Name.ToLower() == name.ToLower() || x.BaseUrl.ToLower() == baseUrl.ToLower(),
                cancellationToken))
        {
            return Result<VpnPanelDto>.Failure("A VPN panel with the same name or base URL already exists.");
        }

        var sslMode = Enum.Parse<VpnSslVerificationMode>(command.SslVerificationMode, true);
        var apiVariant = Enum.Parse<X3UiApiVariant>(command.ApiVariant, true);
        var panel = new VpnPanel
        {
            Name = name,
            BaseUrl = baseUrl,
            Login = command.Login.Trim(),
            EncryptedPassword = _secretProtector.Protect(command.Password),
            Region = string.IsNullOrWhiteSpace(command.Region) ? "default" : command.Region.Trim(),
            Status = VpnPanelStatus.New,
            HealthStatus = HealthStatus.Unknown,
            Capacity = command.Capacity,
            SslVerificationMode = sslMode,
            ApiVariant = apiVariant,
            AutoCreateInbound = command.AutoCreateInbound,
            DefaultInboundTemplateJson = string.IsNullOrWhiteSpace(command.DefaultInboundTemplateJson) ? "{}" : command.DefaultInboundTemplateJson.Trim()
        };

        _db.VpnPanels.Add(panel);
        AddAudit("vpn_panel.create", "VpnPanel", panel.Id, actorUserId, null, PanelAuditSnapshot(panel));
        await _db.SaveChangesAsync(cancellationToken);

        var health = await CheckHealthAsync(panel.Id, actorUserId, cancellationToken);
        if (health.IsSuccess)
        {
            await SyncPanelAsync(panel.Id, actorUserId, cancellationToken);
        }

        var saved = await _db.VpnPanels.AsNoTracking().FirstAsync(x => x.Id == panel.Id, cancellationToken);
        return Result<VpnPanelDto>.Success(MapPanel(saved));
    }

    public Task<Result<VpnPanelDto>> UpdatePanelAsync(Guid id, UpdateVpnPanelCommand command, CancellationToken cancellationToken = default)
        => UpdatePanelAsync(id, command, null, cancellationToken);

    public async Task<Result<VpnPanelDto>> UpdatePanelAsync(Guid id, UpdateVpnPanelCommand command, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (panel is null)
        {
            return Result<VpnPanelDto>.Failure("VPN panel not found.");
        }

        var name = string.IsNullOrWhiteSpace(command.Name) ? panel.Name : command.Name.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(command.BaseUrl) ? panel.BaseUrl : NormalizeBaseUrl(command.BaseUrl);
        var login = string.IsNullOrWhiteSpace(command.Login) ? panel.Login : command.Login.Trim();
        var capacity = command.Capacity ?? panel.Capacity;
        var sslModeText = string.IsNullOrWhiteSpace(command.SslVerificationMode) ? panel.SslVerificationMode.ToString() : command.SslVerificationMode;
        var apiVariantText = string.IsNullOrWhiteSpace(command.ApiVariant) ? panel.ApiVariant.ToString() : command.ApiVariant;
        var templateJson = string.IsNullOrWhiteSpace(command.DefaultInboundTemplateJson) ? panel.DefaultInboundTemplateJson : command.DefaultInboundTemplateJson.Trim();
        var statusText = string.IsNullOrWhiteSpace(command.Status) ? panel.Status.ToString() : command.Status;
        var validationError = ValidatePanelCommand(
            name,
            baseUrl,
            login,
            command.Password,
            capacity,
            sslModeText,
            apiVariantText,
            templateJson,
            statusText,
            passwordRequired: false);
        if (validationError is not null)
        {
            return Result<VpnPanelDto>.Failure(validationError);
        }

        if (await _db.VpnPanels.AnyAsync(
                x => x.Id != id && (x.Name.ToLower() == name.ToLower() || x.BaseUrl.ToLower() == baseUrl.ToLower()),
                cancellationToken))
        {
            return Result<VpnPanelDto>.Failure("A VPN panel with the same name or base URL already exists.");
        }

        var before = PanelAuditSnapshot(panel);
        panel.Name = name;
        panel.BaseUrl = baseUrl;
        panel.Login = login;
        if (!string.IsNullOrWhiteSpace(command.Password)) panel.EncryptedPassword = _secretProtector.Protect(command.Password);
        if (!string.IsNullOrWhiteSpace(command.Region)) panel.Region = command.Region.Trim();
        panel.Capacity = capacity;
        panel.SslVerificationMode = Enum.Parse<VpnSslVerificationMode>(sslModeText, true);
        panel.ApiVariant = Enum.Parse<X3UiApiVariant>(apiVariantText, true);
        if (command.AutoCreateInbound.HasValue) panel.AutoCreateInbound = command.AutoCreateInbound.Value;
        panel.DefaultInboundTemplateJson = templateJson;
        panel.Status = Enum.Parse<VpnPanelStatus>(statusText, true);
        panel.UpdatedAt = _clock.UtcNow;
        AddAudit("vpn_panel.update", "VpnPanel", panel.Id, actorUserId, before, PanelAuditSnapshot(panel));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnPanelDto>.Success(MapPanel(panel));
    }

    public Task<Result<DeleteVpnPanelResultDto>> DeletePanelAsync(Guid id, CancellationToken cancellationToken = default)
        => DeletePanelAsync(id, null, cancellationToken);

    public async Task<Result<DeleteVpnPanelResultDto>> DeletePanelAsync(Guid id, Guid? actorUserId, CancellationToken cancellationToken = default)
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
        var before = PanelAuditSnapshot(panel);

        if (linkedInbounds > 0 || linkedClients > 0 || linkedSyncRuns > 0 || linkedHealthChecks > 0)
        {
            panel.Status = VpnPanelStatus.Disabled;
            panel.HealthStatus = HealthStatus.Unknown;
            panel.LastError = "Panel disabled by admin delete action because operational history is linked.";
            panel.UpdatedAt = _clock.UtcNow;
            AddAudit("vpn_panel.archive", "VpnPanel", panel.Id, actorUserId, before, new { panel.Status, linkedInbounds, linkedClients, linkedSyncRuns, linkedHealthChecks });
            await _db.SaveChangesAsync(cancellationToken);
            return Result<DeleteVpnPanelResultDto>.Success(new DeleteVpnPanelResultDto(id, Deleted: false, Archived: true, linkedInbounds, linkedClients, linkedSyncRuns, linkedHealthChecks));
        }

        _db.VpnPanels.Remove(panel);
        AddAudit("vpn_panel.delete", "VpnPanel", panel.Id, actorUserId, before, null);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<DeleteVpnPanelResultDto>.Success(new DeleteVpnPanelResultDto(id, Deleted: true, Archived: false, linkedInbounds, linkedClients, linkedSyncRuns, linkedHealthChecks));
    }

    public Task<Result<PanelHealthCheckDto>> CheckHealthAsync(Guid panelId, CancellationToken cancellationToken = default)
        => CheckHealthAsync(panelId, null, cancellationToken);

    public async Task<Result<PanelHealthCheckDto>> CheckHealthAsync(Guid panelId, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null)
        {
            return Result<PanelHealthCheckDto>.Failure("VPN panel not found.");
        }

        var before = PanelAuditSnapshot(panel);
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
            AddAudit("vpn_panel.health_check", "VpnPanel", panel.Id, actorUserId, before, HealthAuditSnapshot(entity));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelHealthCheckDto>.Success(MapHealth(entity));
        }

        var sw = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(panel.BaseUrl) || string.IsNullOrWhiteSpace(panel.Login) || string.IsNullOrWhiteSpace(panel.EncryptedPassword))
        {
            return await RecordHealthFailureAsync(
                panel,
                actorUserId,
                before,
                sw,
                "Panel not configured: base URL, login and password are required.",
                cancellationToken);
        }

        try
        {
            var password = _secretProtector.Unprotect(panel.EncryptedPassword);
            var health = await _client.CheckHealthAsync(panel, password, cancellationToken);
            sw.Stop();
            var safeError = string.IsNullOrWhiteSpace(health.ErrorMessage) ? string.Empty : SafeError(health.ErrorMessage);
            var entity = new PanelHealthCheck
            {
                VpnPanelId = panel.Id,
                Status = health.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                LatencyMs = health.LatencyMs == 0 ? sw.ElapsedMilliseconds : health.LatencyMs,
                Version = health.Version,
                ErrorMessage = safeError,
                CheckedAt = _clock.UtcNow
            };
            _db.PanelHealthChecks.Add(entity);
            panel.HealthStatus = entity.Status;
            panel.LastHealthCheckAt = entity.CheckedAt;
            panel.LastError = entity.ErrorMessage;
            panel.Version = entity.Version;
            panel.Status = entity.Status == HealthStatus.Healthy && panel.Status == VpnPanelStatus.New ? VpnPanelStatus.Active : panel.Status;
            AddAudit("vpn_panel.health_check", "VpnPanel", panel.Id, actorUserId, before, HealthAuditSnapshot(entity));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelHealthCheckDto>.Success(MapHealth(entity));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return await RecordHealthFailureAsync(panel, actorUserId, before, sw, ex.Message, CancellationToken.None);
        }
    }

    public Task<Result<PanelSyncRunDto>> SyncPanelAsync(Guid panelId, CancellationToken cancellationToken = default)
        => SyncPanelAsync(panelId, null, cancellationToken);

    public Task<Result<PanelSyncRunDto>> SyncPanelAsync(Guid panelId, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SyncPanelCoreAsync(panelId, actorUserId, enforceExpectedLastSyncAt: false, expectedLastSyncAt: null, cancellationToken);

    public Task<Result<PanelSyncRunDto>> SyncPanelIfCurrentAsync(
        Guid panelId,
        DateTimeOffset? expectedLastSyncAt,
        CancellationToken cancellationToken = default)
        => SyncPanelCoreAsync(
            panelId,
            actorUserId: null,
            enforceExpectedLastSyncAt: true,
            expectedLastSyncAt: expectedLastSyncAt,
            cancellationToken: cancellationToken);

    private async Task<Result<PanelSyncRunDto>> SyncPanelCoreAsync(
        Guid panelId,
        Guid? actorUserId,
        bool enforceExpectedLastSyncAt,
        DateTimeOffset? expectedLastSyncAt,
        CancellationToken cancellationToken)
    {
        var panel = await _db.VpnPanels.Include(x => x.Inbounds).FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null)
        {
            return Result<PanelSyncRunDto>.Failure("VPN panel not found.");
        }
        if (enforceExpectedLastSyncAt && panel.LastSyncAt != expectedLastSyncAt)
        {
            return Result<PanelSyncRunDto>.Failure("Panel sync observation is stale; a newer sync already completed.");
        }

        var before = PanelAuditSnapshot(panel);
        var now = _clock.UtcNow;
        var staleRuns = (await _db.PanelSyncRuns
                .Where(x => x.VpnPanelId == panel.Id && x.Status == PanelSyncRunStatus.Running)
                .ToListAsync(cancellationToken))
            .Where(x => x.StartedAt < now - SyncLeaseDuration)
            .ToList();
        foreach (var staleRun in staleRuns)
        {
            staleRun.Status = PanelSyncRunStatus.Failed;
            staleRun.FinishedAt = now;
            staleRun.ErrorMessage = "Panel sync lease expired before completion; a new attempt may start.";
            staleRun.UpdatedAt = now;
            AddAudit("vpn_panel.sync.lease_expired", "VpnPanel", panel.Id, actorUserId, before, SyncAuditSnapshot(staleRun));
        }
        if (staleRuns.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        var inboundSnapshots = panel.Inbounds.ToDictionary(x => x.Id, VpnInboundSyncSnapshot.Create);
        var previousLastSyncAt = panel.LastSyncAt;
        var initialSyncAuditIds = _db.AuditLogs.Local
            .Where(x => x.EntityType == "VpnPanel" && x.EntityId == panel.Id.ToString() && x.Action.StartsWith("vpn_panel.sync", StringComparison.Ordinal))
            .Select(x => x.Id)
            .ToHashSet();
        var run = new PanelSyncRun
        {
            VpnPanelId = panel.Id,
            Status = PanelSyncRunStatus.Running,
            StartedAt = _clock.UtcNow
        };
        _db.PanelSyncRuns.Add(run);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.PanelSyncRuns.Remove(run);
            if (await _db.PanelSyncRuns.AsNoTracking().AnyAsync(
                    x => x.VpnPanelId == panel.Id && x.Status == PanelSyncRunStatus.Running,
                    CancellationToken.None))
            {
                return Result<PanelSyncRunDto>.Failure("Panel sync is already running for this panel.");
            }
            throw;
        }

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
                AddAudit("vpn_panel.sync", "VpnPanel", panel.Id, actorUserId, before, SyncAuditSnapshot(run));
                await _db.SaveChangesAsync(cancellationToken);
                return Result<PanelSyncRunDto>.Success(MapSyncRun(run));
            }

            if (string.IsNullOrWhiteSpace(panel.BaseUrl) || string.IsNullOrWhiteSpace(panel.Login) || string.IsNullOrWhiteSpace(panel.EncryptedPassword))
            {
                run.Status = PanelSyncRunStatus.Failed;
                run.FinishedAt = _clock.UtcNow;
                run.ErrorMessage = "Panel not configured: base URL, login and password are required.";
                panel.LastError = run.ErrorMessage;
                AddAudit("vpn_panel.sync.failed", "VpnPanel", panel.Id, actorUserId, before, SyncAuditSnapshot(run));
                await _db.SaveChangesAsync(cancellationToken);
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
            AddAudit("vpn_panel.sync", "VpnPanel", panel.Id, actorUserId, before, SyncAuditSnapshot(run));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelSyncRunDto>.Success(MapSyncRun(run));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RestorePendingSyncChanges(panel, run, inboundSnapshots, previousLastSyncAt, initialSyncAuditIds);
            run.Status = PanelSyncRunStatus.Failed;
            run.FinishedAt = _clock.UtcNow;
            run.ErrorMessage = "Panel sync was cancelled.";
            panel.LastError = run.ErrorMessage;
            AddAudit("vpn_panel.sync.cancelled", "VpnPanel", panel.Id, actorUserId, before, SyncAuditSnapshot(run));
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            RestorePendingSyncChanges(panel, run, inboundSnapshots, previousLastSyncAt, initialSyncAuditIds);
            run.Status = PanelSyncRunStatus.Failed;
            run.FinishedAt = _clock.UtcNow;
            run.ErrorMessage = safeError;
            panel.LastError = safeError;
            AddAudit("vpn_panel.sync.failed", "VpnPanel", panel.Id, actorUserId, before, SyncAuditSnapshot(run));
            await _db.SaveChangesAsync(CancellationToken.None);
            return Result<PanelSyncRunDto>.Failure(safeError);
        }
    }

    private async Task<Result<PanelHealthCheckDto>> RecordHealthFailureAsync(
        VpnPanel panel,
        Guid? actorUserId,
        object before,
        Stopwatch stopwatch,
        string? error,
        CancellationToken cancellationToken)
    {
        stopwatch.Stop();
        var safeError = SafeError(error);
        var entity = new PanelHealthCheck
        {
            VpnPanelId = panel.Id,
            Status = HealthStatus.Unhealthy,
            LatencyMs = stopwatch.ElapsedMilliseconds,
            ErrorMessage = safeError,
            CheckedAt = _clock.UtcNow
        };
        _db.PanelHealthChecks.Add(entity);
        panel.HealthStatus = HealthStatus.Unhealthy;
        panel.LastHealthCheckAt = entity.CheckedAt;
        panel.LastError = safeError;
        AddAudit("vpn_panel.health_check.failed", "VpnPanel", panel.Id, actorUserId, before, HealthAuditSnapshot(entity));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PanelHealthCheckDto>.Failure(safeError);
    }

    private void RestorePendingSyncChanges(
        VpnPanel panel,
        PanelSyncRun run,
        IReadOnlyDictionary<Guid, VpnInboundSyncSnapshot> inboundSnapshots,
        DateTimeOffset? previousLastSyncAt,
        IReadOnlySet<Guid> initialSyncAuditIds)
    {
        foreach (var inbound in _db.VpnInbounds.Local.Where(x => x.VpnPanelId == panel.Id).ToList())
        {
            if (inboundSnapshots.TryGetValue(inbound.Id, out var snapshot))
            {
                snapshot.Restore(inbound);
            }
            else
            {
                _db.VpnInbounds.Remove(inbound);
            }
        }

        var pendingEvents = _db.PanelSyncEvents.Local.Where(x => x.PanelSyncRunId == run.Id).ToList();
        _db.PanelSyncEvents.RemoveRange(pendingEvents);

        var pendingSuccessAudits = _db.AuditLogs.Local
            .Where(x => x.EntityType == "VpnPanel"
                && x.EntityId == panel.Id.ToString()
                && x.Action.StartsWith("vpn_panel.sync", StringComparison.Ordinal)
                && !initialSyncAuditIds.Contains(x.Id))
            .ToList();
        _db.AuditLogs.RemoveRange(pendingSuccessAudits);

        panel.LastSyncAt = previousLastSyncAt;
        run.SummaryJson = "{}";
    }

    public Task<Result<VpnInboundDto>> CreateInboundAsync(Guid panelId, CreateVpnInboundCommand command, CancellationToken cancellationToken = default)
        => CreateInboundAsync(panelId, command, null, cancellationToken);

    public async Task<Result<VpnInboundDto>> CreateInboundAsync(Guid panelId, CreateVpnInboundCommand command, Guid? actorUserId, CancellationToken cancellationToken = default)
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

        var sandboxMode = IsSandboxMode();
        X3UiInboundDto remote;
        if (sandboxMode)
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
        var previousDefaults = new List<VpnInbound>();
        if (command.IsDefault)
        {
            previousDefaults = await _db.VpnInbounds.Where(x => x.VpnPanelId == panel.Id && x.IsDefault).ToListAsync(cancellationToken);
            foreach (var item in previousDefaults) item.IsDefault = false;
        }
        _db.VpnInbounds.Add(inbound);
        AddAudit("vpn_inbound.create", "VpnInbound", inbound.Id, actorUserId, null, InboundAuditSnapshot(inbound));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception saveError) when (!sandboxMode)
        {
            _db.VpnInbounds.Remove(inbound);
            foreach (var item in previousDefaults) item.IsDefault = true;
            var pendingAudit = _db.AuditLogs.Local
                .Where(x => x.Action == "vpn_inbound.create" && x.EntityId == inbound.Id.ToString())
                .ToList();
            _db.AuditLogs.RemoveRange(pendingAudit);

            try
            {
                var password = _secretProtector.Unprotect(panel.EncryptedPassword);
                await _client.DeleteInboundAsync(panel, password, remote.Id, CancellationToken.None);
            }
            catch (Exception compensationError)
            {
                AddAudit("vpn_inbound.create.compensation_failed", "VpnInbound", inbound.Id, actorUserId, null, new
                {
                    panelId = panel.Id,
                    remoteInboundId = remote.Id,
                    compensated = false
                });
                await _db.SaveChangesAsync(CancellationToken.None);
                throw new InvalidOperationException(
                    "VPN inbound was created remotely but local persistence and remote cleanup failed; manual provider cleanup is required.",
                    new AggregateException(saveError, compensationError));
            }

            AddAudit("vpn_inbound.create.failed", "VpnInbound", inbound.Id, actorUserId, null, new
            {
                panelId = panel.Id,
                remoteInboundId = remote.Id,
                compensated = true
            });
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        return Result<VpnInboundDto>.Success(MapInbound(inbound));
    }

    public async Task<IReadOnlyCollection<VpnInboundDto>> GetInboundsAsync(Guid panelId, CancellationToken cancellationToken = default)
    {
        var inbounds = await _db.VpnInbounds.AsNoTracking().Where(x => x.VpnPanelId == panelId).OrderByDescending(x => x.IsDefault).ThenBy(x => x.Port).ToListAsync(cancellationToken);
        return inbounds.Select(MapInbound).ToList();
    }

    public Task<Result<VpnInboundDto>> PatchInboundAsync(Guid inboundId, CreateVpnInboundCommand command, CancellationToken cancellationToken = default)
        => PatchInboundAsync(inboundId, command, null, cancellationToken);

    public async Task<Result<VpnInboundDto>> PatchInboundAsync(Guid inboundId, CreateVpnInboundCommand command, Guid? actorUserId, CancellationToken cancellationToken = default)
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

        var before = InboundAuditSnapshot(inbound);
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

        AddAudit("vpn_inbound.update", "VpnInbound", inbound.Id, actorUserId, before, InboundAuditSnapshot(inbound));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnInboundDto>.Success(MapInbound(inbound));
    }

    public Task<Result<VpnInboundDto>> SetDefaultInboundAsync(Guid inboundId, CancellationToken cancellationToken = default)
        => SetDefaultInboundAsync(inboundId, null, cancellationToken);

    public async Task<Result<VpnInboundDto>> SetDefaultInboundAsync(Guid inboundId, Guid? actorUserId, CancellationToken cancellationToken = default)
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

        var before = InboundAuditSnapshot(inbound);
        var all = await _db.VpnInbounds.Where(x => x.VpnPanelId == inbound.VpnPanelId).ToListAsync(cancellationToken);
        foreach (var item in all)
        {
            item.IsDefault = item.Id == inbound.Id;
            item.UpdatedAt = _clock.UtcNow;
        }
        AddAudit("vpn_inbound.default.set", "VpnInbound", inbound.Id, actorUserId, before, InboundAuditSnapshot(inbound));
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

    private static string? ValidatePanelCommand(
        string name,
        string baseUrl,
        string login,
        string? password,
        int capacity,
        string sslVerificationMode,
        string apiVariant,
        string defaultInboundTemplateJson,
        string? status,
        bool passwordRequired)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Panel name is required.";
        }
        if (!Uri.TryCreate(baseUrl?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return "Base URL must be an absolute HTTP or HTTPS URL.";
        }
        if (string.IsNullOrWhiteSpace(login))
        {
            return "Login is required.";
        }
        if (passwordRequired && string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }
        if (capacity <= 0)
        {
            return "Capacity must be greater than zero.";
        }
        if (!Enum.TryParse<VpnSslVerificationMode>(sslVerificationMode, true, out var sslMode)
            || !Enum.IsDefined(sslMode))
        {
            return "SSL verification mode is invalid.";
        }
        if (!Enum.TryParse<X3UiApiVariant>(apiVariant, true, out var variant)
            || !Enum.IsDefined(variant))
        {
            return "API variant is invalid.";
        }
        if (!string.IsNullOrWhiteSpace(status)
            && (!Enum.TryParse<VpnPanelStatus>(status, true, out var panelStatus) || !Enum.IsDefined(panelStatus)))
        {
            return "Panel status is invalid.";
        }

        return ValidateJsonObject(defaultInboundTemplateJson, "Default inbound template");
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
        => SetClientEnabledAsync(clientId, true, null, cancellationToken);

    public Task<Result<VpnClientDto>> EnableClientAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, true, actorUserId, cancellationToken);

    public Task<Result<VpnClientDto>> DisableClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, false, null, cancellationToken);

    public Task<Result<VpnClientDto>> DisableClientAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, false, actorUserId, cancellationToken);

    public Task<Result<VpnClientDto>> SyncClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        => SyncClientAsync(clientId, null, cancellationToken);

    public async Task<Result<VpnClientDto>> SyncClientAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var observedClient = await LoadClientForActionAsync(clientId, cancellationToken);
        if (observedClient?.VpnPanel is null || observedClient.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnSubscriptionAsync(observedClient.SubscriptionId, cancellationToken);
        ClearTracker();
        var client = await LoadClientForActionAsync(clientId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        var before = ClientAuditSnapshot(client);
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
        AddAudit("vpn_client.sync", "VpnClient", client.Id, actorUserId, before, ClientAuditSnapshot(client));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<VpnClientDto>.Success(MapClient(client));
    }

    public Task<Result<VpnClientDto>> ResetClientTrafficAsync(Guid clientId, CancellationToken cancellationToken = default)
        => ResetClientTrafficAsync(clientId, null, cancellationToken);

    public async Task<Result<VpnClientDto>> ResetClientTrafficAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var observedClient = await LoadClientForActionAsync(clientId, cancellationToken);
        if (observedClient?.VpnPanel is null || observedClient.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnSubscriptionAsync(observedClient.SubscriptionId, cancellationToken);
        ClearTracker();
        var client = await LoadClientForActionAsync(clientId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        var before = ClientAuditSnapshot(client);
        var remoteMutationAttempted = false;
        var remoteMutationCompleted = false;
        if (!IsSandboxMode())
        {
            var configurationError = ValidatePanelCredentials(client.VpnPanel);
            if (configurationError is not null)
            {
                return Result<VpnClientDto>.Failure(configurationError);
            }

            var password = _secretProtector.Unprotect(client.VpnPanel.EncryptedPassword);
            try
            {
                remoteMutationAttempted = true;
                await _client.ResetClientTrafficAsync(client.VpnPanel, password, client.VpnInbound.ExternalInboundId, client.Uuid, cancellationToken);
                remoteMutationCompleted = true;
            }
            catch (Exception remoteError)
            {
                await PersistTrafficResetUncertaintyAsync(client.Id, actorUserId, before, remoteError, "remote_operation_failed");
                throw;
            }
        }

        try
        {
            client.SyncStatus = IsSandboxMode() ? "sandbox-traffic-reset" : "traffic-reset";
            client.LastSyncedAt = _clock.UtcNow;
            client.UpdatedAt = _clock.UtcNow;
            await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
            AddAudit("vpn_client.traffic.reset", "VpnClient", client.Id, actorUserId, before, ClientAuditSnapshot(client));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VpnClientDto>.Success(MapClient(client));
        }
        catch (Exception localError) when (remoteMutationAttempted && remoteMutationCompleted)
        {
            await PersistTrafficResetUncertaintyAsync(client.Id, actorUserId, before, localError, "local_persistence_failed");
            throw;
        }
    }

    public Task<Result<VpnClientDto>> MigrateClientAsync(Guid clientId, MigrateVpnClientCommand command, CancellationToken cancellationToken = default)
        => MigrateClientAsync(clientId, command, null, cancellationToken);

    public async Task<Result<VpnClientDto>> MigrateClientAsync(Guid clientId, MigrateVpnClientCommand command, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var observedClient = await LoadClientForActionAsync(clientId, cancellationToken);
        if (observedClient?.VpnPanel is null || observedClient.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnSubscriptionAsync(observedClient.SubscriptionId, cancellationToken);
        ClearTracker();
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
        if (targetInbound.VpnPanel.Status != VpnPanelStatus.Active || targetInbound.VpnPanel.HealthStatus == HealthStatus.Unhealthy)
        {
            return Result<VpnClientDto>.Failure("Target panel is not active and healthy.");
        }
        if (!string.Equals(targetInbound.Protocol, client.VpnInbound.Protocol, StringComparison.OrdinalIgnoreCase))
        {
            return Result<VpnClientDto>.Failure("Target inbound protocol must match the client protocol.");
        }
        if (targetInbound.Id == client.VpnInboundId)
        {
            var sameTargetBefore = ClientAuditSnapshot(client);
            client.SyncStatus = "already-on-target";
            client.LastSyncedAt = _clock.UtcNow;
            client.UpdatedAt = _clock.UtcNow;
            AddAudit("vpn_client.migrate", "VpnClient", client.Id, actorUserId, sameTargetBefore, ClientAuditSnapshot(client));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VpnClientDto>.Success(MapClient(client));
        }

        string? sourcePassword = null;
        string? targetPassword = null;
        if (!IsSandboxMode())
        {
            var sourceConfigurationError = ValidatePanelCredentials(client.VpnPanel);
            var targetConfigurationError = ValidatePanelCredentials(targetInbound.VpnPanel);
            if (sourceConfigurationError is not null) return Result<VpnClientDto>.Failure(sourceConfigurationError);
            if (targetConfigurationError is not null) return Result<VpnClientDto>.Failure(targetConfigurationError);
            sourcePassword = _secretProtector.Unprotect(client.VpnPanel.EncryptedPassword);
            targetPassword = _secretProtector.Unprotect(targetInbound.VpnPanel.EncryptedPassword);
        }

        var capacityResult = await TryReserveMigrationTargetCapacityAsync(targetInbound, cancellationToken);
        if (!capacityResult.IsSuccess)
        {
            return Result<VpnClientDto>.Failure(capacityResult.Error ?? "Target VPN capacity is unavailable.");
        }

        ClearTracker();
        client = await LoadClientForActionAsync(clientId, cancellationToken);
        targetInbound = await _db.VpnInbounds.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.Id == command.TargetInboundId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null || targetInbound?.VpnPanel is null)
        {
            await ReleaseMigrationTargetCapacityAsync(command.TargetInboundId, cancellationToken);
            return Result<VpnClientDto>.Failure("VPN client or target inbound disappeared during migration.");
        }

        var before = ClientAuditSnapshot(client);
        var sourceInbound = client.VpnInbound;
        var sourcePanel = client.VpnPanel;
        var sourceRequest = new X3UiAddClientRequest(
            sourceInbound.ExternalInboundId,
            client.Email,
            client.Uuid,
            client.Flow,
            client.LimitIp,
            client.TotalGb,
            client.ExpiryTime,
            client.Enable);
        var targetRequest = sourceRequest with { InboundId = targetInbound.ExternalInboundId };
        X3UiClientDto? remote = null;
        if (!IsSandboxMode())
        {
            try
            {
                remote = await _client.AddClientAsync(targetInbound.VpnPanel, targetPassword!, targetRequest, cancellationToken);
            }
            catch (Exception targetCreateError)
            {
                await HandleTargetOnlyMigrationFailureAsync(
                    client.Id,
                    sourceInbound.Id,
                    targetInbound,
                    targetPassword!,
                    client.Uuid,
                    actorUserId,
                    before,
                    targetCreateError);
                throw;
            }

            try
            {
                await _client.DeleteClientAsync(sourcePanel, sourcePassword!, sourceInbound.ExternalInboundId, client.Uuid, cancellationToken);
            }
            catch (Exception sourceDeleteError)
            {
                try
                {
                    await _client.AddClientAsync(sourcePanel, sourcePassword!, sourceRequest, CancellationToken.None);
                    await _client.DeleteClientAsync(targetInbound.VpnPanel, targetPassword!, targetInbound.ExternalInboundId, client.Uuid, CancellationToken.None);
                    ClearTracker();
                    await ReleaseMigrationTargetCapacityAsync(targetInbound.Id, CancellationToken.None);
                }
                catch (Exception compensationError)
                {
                    await PersistMigrationManualCleanupAsync(client.Id, sourceInbound.Id, targetInbound.Id, actorUserId, before, sourceDeleteError, compensationError, "target_cleanup_failed");
                    throw new InvalidOperationException(
                        "VPN client migration failed and the target copy could not be removed; manual provider cleanup is required.",
                        new AggregateException(sourceDeleteError, compensationError));
                }

                await PersistMigrationFailureAsync(client.Id, sourceInbound.Id, targetInbound.Id, actorUserId, before, sourceDeleteError, "source_restored_and_target_removed", compensated: true);
                throw;
            }
        }

        IDbContextTransaction? localTransaction = null;
        var commitAttempted = false;
        try
        {
            if (sourcePanel.UsedCapacity > 0) sourcePanel.UsedCapacity -= 1;
            if (sourceInbound.UsedCapacity > 0) sourceInbound.UsedCapacity -= 1;

            if (_db is DbContext dbContext && dbContext.Database.IsRelational())
            {
                localTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
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
            AddAudit("vpn_client.migrate", "VpnClient", client.Id, actorUserId, before, ClientAuditSnapshot(client));
            await _db.SaveChangesAsync(cancellationToken);
            if (localTransaction is not null)
            {
                commitAttempted = true;
                await localTransaction.CommitAsync(cancellationToken);
            }

            return Result<VpnClientDto>.Success(MapClient(client));
        }
        catch (Exception localError)
        {
            if (localTransaction is not null)
            {
                try { await localTransaction.RollbackAsync(CancellationToken.None); } catch { }
            }
            ClearTracker();

            if (commitAttempted)
            {
                await PersistMigrationManualCleanupAsync(clientId, sourceInbound.Id, targetInbound.Id, actorUserId, before, localError, null, "database_commit_unknown");
                throw new InvalidOperationException("VPN client migration database commit is uncertain; manual provider reconciliation is required.", localError);
            }

            try
            {
                if (!IsSandboxMode())
                {
                    await _client.AddClientAsync(sourcePanel, sourcePassword!, sourceRequest, CancellationToken.None);
                    await _client.DeleteClientAsync(targetInbound.VpnPanel, targetPassword!, targetInbound.ExternalInboundId, client.Uuid, CancellationToken.None);
                }
                await ReleaseMigrationTargetCapacityAsync(targetInbound.Id, CancellationToken.None);
            }
            catch (Exception compensationError)
            {
                await PersistMigrationManualCleanupAsync(clientId, sourceInbound.Id, targetInbound.Id, actorUserId, before, localError, compensationError, "local_save_compensation_failed");
                throw new InvalidOperationException(
                    "VPN client migration local persistence failed and remote rollback is uncertain; manual provider reconciliation is required.",
                    new AggregateException(localError, compensationError));
            }

            await PersistMigrationFailureAsync(clientId, sourceInbound.Id, targetInbound.Id, actorUserId, before, localError, "remote_rolled_back", compensated: true);
            throw;
        }
        finally
        {
            if (localTransaction is not null)
            {
                await localTransaction.DisposeAsync();
            }
        }
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

    private async Task<Result<VpnClientDto>> SetClientEnabledAsync(Guid clientId, bool enabled, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var observedClient = await LoadClientForActionAsync(clientId, cancellationToken);
        if (observedClient?.VpnPanel is null || observedClient.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnSubscriptionAsync(observedClient.SubscriptionId, cancellationToken);
        ClearTracker();
        var client = await LoadClientForActionAsync(clientId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null)
        {
            return Result<VpnClientDto>.Failure("VPN client not found.");
        }
        if (client.Enable == enabled)
        {
            return Result<VpnClientDto>.Success(MapClient(client));
        }

        var before = ClientAuditSnapshot(client);
        var action = enabled ? "vpn_client.enable" : "vpn_client.disable";
        var previousRemoteRequest = new X3UiUpdateClientRequest(
            client.VpnInbound.ExternalInboundId,
            client.Uuid,
            client.Email,
            client.Uuid,
            client.Flow,
            client.LimitIp,
            client.TotalGb,
            client.ExpiryTime,
            client.Enable);
        var remoteMutationAttempted = false;
        string? password = null;
        if (!IsSandboxMode())
        {
            var configurationError = ValidatePanelCredentials(client.VpnPanel);
            if (configurationError is not null)
            {
                return Result<VpnClientDto>.Failure(configurationError);
            }

            password = _secretProtector.Unprotect(client.VpnPanel.EncryptedPassword);
            try
            {
                remoteMutationAttempted = true;
                await _client.UpdateClientAsync(client.VpnPanel, password, previousRemoteRequest with { Enable = enabled }, cancellationToken);
            }
            catch (Exception remoteError)
            {
                await CompensateClientEnabledFailureAsync(client, password, previousRemoteRequest, actorUserId, before, action, remoteError, "remote_operation_failed");
                throw;
            }
        }

        try
        {
            client.Enable = enabled;
            client.SyncStatus = IsSandboxMode()
                ? enabled ? "sandbox-enabled" : "sandbox-disabled"
                : enabled ? "enabled" : "disabled";
            client.LastSyncedAt = _clock.UtcNow;
            client.UpdatedAt = _clock.UtcNow;
            await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
            AddAudit(action, "VpnClient", client.Id, actorUserId, before, ClientAuditSnapshot(client));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VpnClientDto>.Success(MapClient(client));
        }
        catch (Exception localError) when (remoteMutationAttempted)
        {
            await CompensateClientEnabledFailureAsync(client, password!, previousRemoteRequest, actorUserId, before, action, localError, "local_persistence_failed");
            throw;
        }
    }

    private async Task CompensateClientEnabledFailureAsync(
        VpnClient client,
        string password,
        X3UiUpdateClientRequest previousRemoteRequest,
        Guid? actorUserId,
        object before,
        string action,
        Exception operationError,
        string outcome)
    {
        try
        {
            await _client.UpdateClientAsync(client.VpnPanel!, password, previousRemoteRequest, CancellationToken.None);
        }
        catch (Exception compensationError)
        {
            await PersistClientStateCompensationFailureAsync(client.Id, actorUserId, before, action, operationError, compensationError, outcome);
            throw new InvalidOperationException(
                "VPN client state is uncertain and remote rollback failed; manual provider reconciliation is required.",
                new AggregateException(operationError, compensationError));
        }

        ClearTracker();
        AddAudit($"{action}.failed", "VpnClient", client.Id, actorUserId, before, new
        {
            compensated = true,
            outcome,
            error = SafeError(operationError.Message)
        });
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task PersistClientStateCompensationFailureAsync(
        Guid clientId,
        Guid? actorUserId,
        object before,
        string action,
        Exception operationError,
        Exception compensationError,
        string outcome)
    {
        ClearTracker();
        var persistedClient = await _db.VpnClients.FirstOrDefaultAsync(x => x.Id == clientId, CancellationToken.None);
        if (persistedClient is not null)
        {
            persistedClient.SyncStatus = "client-state-compensation-failed";
            persistedClient.LastSyncedAt = _clock.UtcNow;
            persistedClient.UpdatedAt = _clock.UtcNow;
            await MarkLinkedAccessSyncRequiredAsync(persistedClient, CancellationToken.None);
        }
        AddAudit($"{action}.compensation_failed", "VpnClient", clientId, actorUserId, before, new
        {
            compensated = false,
            outcome,
            error = SafeError(operationError.Message),
            compensationError = SafeError(compensationError.Message)
        });
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task PersistTrafficResetUncertaintyAsync(
        Guid clientId,
        Guid? actorUserId,
        object before,
        Exception error,
        string outcome)
    {
        ClearTracker();
        var persistedClient = await _db.VpnClients.FirstOrDefaultAsync(x => x.Id == clientId, CancellationToken.None);
        if (persistedClient is not null)
        {
            persistedClient.SyncStatus = "traffic-reset-uncertain";
            persistedClient.LastSyncedAt = _clock.UtcNow;
            persistedClient.UpdatedAt = _clock.UtcNow;
            await MarkLinkedAccessSyncRequiredAsync(persistedClient, CancellationToken.None);
        }
        AddAudit("vpn_client.traffic.reset.uncertain", "VpnClient", clientId, actorUserId, before, new
        {
            outcome,
            reconciliationRequired = true,
            error = SafeError(error.Message)
        });
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task MarkLinkedAccessSyncRequiredAsync(VpnClient client, CancellationToken cancellationToken)
    {
        var accesses = await _db.AccessCredentials
            .Where(x => x.SubscriptionId == client.SubscriptionId && (x.ProviderAccessId == client.ExternalClientId || x.ProviderAccessId == client.Id.ToString()))
            .ToListAsync(cancellationToken);
        foreach (var access in accesses)
        {
            if (StatusStateMachine.TrySetAccessStatus(access, AccessCredentialStatus.SyncRequired, _clock.UtcNow).IsSuccess)
            {
                access.LastSyncedAt = _clock.UtcNow;
                access.Revision += 1;
            }
        }
    }

    private async Task<Result<bool>> TryReserveMigrationTargetCapacityAsync(VpnInbound targetInbound, CancellationToken cancellationToken)
    {
        if (targetInbound.VpnPanel is null)
        {
            return Result<bool>.Failure("Target panel not found.");
        }

        if (_db is DbContext dbContext && dbContext.Database.IsRelational())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var panelReserved = await _db.VpnPanels
                .Where(x => x.Id == targetInbound.VpnPanelId
                    && x.Status == VpnPanelStatus.Active
                    && x.HealthStatus != HealthStatus.Unhealthy
                    && x.UsedCapacity < x.Capacity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedCapacity, x => x.UsedCapacity + 1), cancellationToken);
            if (panelReserved != 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result<bool>.Failure("Target panel capacity is exhausted or unavailable.");
            }

            var inboundReserved = await _db.VpnInbounds
                .Where(x => x.Id == targetInbound.Id && x.IsActive && x.UsedCapacity < x.Capacity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedCapacity, x => x.UsedCapacity + 1), cancellationToken);
            if (inboundReserved != 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result<bool>.Failure("Target inbound capacity is exhausted or unavailable.");
            }

            await transaction.CommitAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        if (targetInbound.VpnPanel.Status != VpnPanelStatus.Active
            || targetInbound.VpnPanel.HealthStatus == HealthStatus.Unhealthy
            || targetInbound.VpnPanel.UsedCapacity >= targetInbound.VpnPanel.Capacity)
        {
            return Result<bool>.Failure("Target panel capacity is exhausted or unavailable.");
        }
        if (!targetInbound.IsActive || targetInbound.UsedCapacity >= targetInbound.Capacity)
        {
            return Result<bool>.Failure("Target inbound capacity is exhausted or unavailable.");
        }

        targetInbound.VpnPanel.UsedCapacity += 1;
        targetInbound.UsedCapacity += 1;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
        catch
        {
            targetInbound.VpnPanel.UsedCapacity -= 1;
            targetInbound.UsedCapacity -= 1;
            throw;
        }
    }

    private async Task ReleaseMigrationTargetCapacityAsync(Guid targetInboundId, CancellationToken cancellationToken)
    {
        if (_db is DbContext dbContext && dbContext.Database.IsRelational())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var targetPanelId = await _db.VpnInbounds.AsNoTracking()
                .Where(x => x.Id == targetInboundId)
                .Select(x => (Guid?)x.VpnPanelId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Target inbound disappeared while releasing migration capacity.");
            var inboundReleased = await _db.VpnInbounds
                .Where(x => x.Id == targetInboundId && x.UsedCapacity > 0)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedCapacity, x => x.UsedCapacity - 1), cancellationToken);
            var panelReleased = await _db.VpnPanels
                .Where(x => x.Id == targetPanelId && x.UsedCapacity > 0)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedCapacity, x => x.UsedCapacity - 1), cancellationToken);
            if (inboundReleased != 1 || panelReleased != 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw new InvalidOperationException("Reserved migration capacity could not be released consistently.");
            }

            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var inbound = await _db.VpnInbounds.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.Id == targetInboundId, cancellationToken)
            ?? throw new InvalidOperationException("Target inbound disappeared while releasing migration capacity.");
        if (inbound.VpnPanel is null || inbound.UsedCapacity <= 0 || inbound.VpnPanel.UsedCapacity <= 0)
        {
            throw new InvalidOperationException("Reserved migration capacity could not be released consistently.");
        }

        inbound.UsedCapacity -= 1;
        inbound.VpnPanel.UsedCapacity -= 1;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleTargetOnlyMigrationFailureAsync(
        Guid clientId,
        Guid sourceInboundId,
        VpnInbound targetInbound,
        string targetPassword,
        string clientUuid,
        Guid? actorUserId,
        object before,
        Exception migrationError)
    {
        try
        {
            await _client.DeleteClientAsync(targetInbound.VpnPanel!, targetPassword, targetInbound.ExternalInboundId, clientUuid, CancellationToken.None);
            ClearTracker();
            await ReleaseMigrationTargetCapacityAsync(targetInbound.Id, CancellationToken.None);
        }
        catch (Exception compensationError)
        {
            await PersistMigrationManualCleanupAsync(clientId, sourceInboundId, targetInbound.Id, actorUserId, before, migrationError, compensationError, "target_create_cleanup_failed");
            throw new InvalidOperationException(
                "VPN client target creation is uncertain and cleanup failed; manual provider reconciliation is required.",
                new AggregateException(migrationError, compensationError));
        }

        await PersistMigrationFailureAsync(clientId, sourceInboundId, targetInbound.Id, actorUserId, before, migrationError, "target_removed", compensated: true);
    }

    private async Task PersistMigrationFailureAsync(
        Guid clientId,
        Guid sourceInboundId,
        Guid targetInboundId,
        Guid? actorUserId,
        object before,
        Exception error,
        string outcome,
        bool compensated)
    {
        ClearTracker();
        AddAudit("vpn_client.migrate.failed", "VpnClient", clientId, actorUserId, before, new
        {
            sourceInboundId,
            targetInboundId,
            compensated,
            outcome,
            error = SafeError(error.Message)
        });
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task PersistMigrationManualCleanupAsync(
        Guid clientId,
        Guid sourceInboundId,
        Guid targetInboundId,
        Guid? actorUserId,
        object before,
        Exception error,
        Exception? compensationError,
        string outcome)
    {
        ClearTracker();
        var persistedClient = await _db.VpnClients.FirstOrDefaultAsync(x => x.Id == clientId, CancellationToken.None);
        if (persistedClient is not null)
        {
            persistedClient.SyncStatus = "migration-compensation-failed";
            persistedClient.LastSyncedAt = _clock.UtcNow;
            persistedClient.UpdatedAt = _clock.UtcNow;
        }
        AddAudit("vpn_client.migrate.compensation_failed", "VpnClient", clientId, actorUserId, before, new
        {
            sourceInboundId,
            targetInboundId,
            compensated = false,
            outcome,
            error = SafeError(error.Message),
            compensationError = SafeError(compensationError?.Message)
        });
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private void ClearTracker()
    {
        if (_db is DbContext dbContext)
        {
            dbContext.ChangeTracker.Clear();
        }
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

    private void AddAudit(string action, string entityType, Guid entityId, Guid? actorUserId, object? before, object? after)
        => _db.AuditLogs.Add(new AuditLog
        {
            ActorType = actorUserId.HasValue ? "admin" : "system",
            ActorId = actorUserId?.ToString() ?? "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            BeforeJson = SensitiveDataRedactor.Redact(SerializeAuditSnapshot(before)),
            AfterJson = SensitiveDataRedactor.Redact(SerializeAuditSnapshot(after)),
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        });

    private static string SerializeAuditSnapshot(object? snapshot)
        => snapshot is null ? "{}" : JsonSerializer.Serialize(snapshot);

    private static object PanelAuditSnapshot(VpnPanel panel)
        => new
        {
            panel.Name,
            panel.BaseUrl,
            panel.Region,
            panel.Status,
            panel.HealthStatus,
            panel.SslVerificationMode,
            panel.ApiVariant,
            panel.Capacity,
            panel.UsedCapacity,
            panel.AutoCreateInbound,
            passwordConfigured = !string.IsNullOrWhiteSpace(panel.EncryptedPassword),
            panel.LastHealthCheckAt,
            panel.LastSyncAt
        };

    private static object InboundAuditSnapshot(VpnInbound inbound)
        => new
        {
            inbound.VpnPanelId,
            inbound.Name,
            inbound.Protocol,
            inbound.Port,
            inbound.Listen,
            inbound.IsDefault,
            inbound.IsActive,
            inbound.Capacity,
            inbound.UsedCapacity
        };

    private static object ClientAuditSnapshot(VpnClient client)
        => new
        {
            client.UserId,
            client.SubscriptionId,
            client.VpnPanelId,
            client.VpnInboundId,
            client.ExpiryTime,
            client.Enable,
            client.SyncStatus,
            client.LastSyncedAt
        };

    private static object HealthAuditSnapshot(PanelHealthCheck check)
        => new
        {
            check.Id,
            check.Status,
            check.LatencyMs,
            check.Version,
            hasError = !string.IsNullOrWhiteSpace(check.ErrorMessage),
            check.CheckedAt
        };

    private static object SyncAuditSnapshot(PanelSyncRun run)
        => new
        {
            run.Id,
            run.Status,
            run.StartedAt,
            run.FinishedAt,
            hasError = !string.IsNullOrWhiteSpace(run.ErrorMessage)
        };

    private sealed record VpnInboundSyncSnapshot(
        string ExternalInboundId,
        string Name,
        string Protocol,
        int Port,
        string Listen,
        string SettingsJson,
        string StreamSettingsJson,
        string SniffingJson,
        bool IsDefault,
        bool IsActive,
        int Capacity,
        int UsedCapacity,
        DateTimeOffset UpdatedAt)
    {
        public static VpnInboundSyncSnapshot Create(VpnInbound inbound)
            => new(
                inbound.ExternalInboundId,
                inbound.Name,
                inbound.Protocol,
                inbound.Port,
                inbound.Listen,
                inbound.SettingsJson,
                inbound.StreamSettingsJson,
                inbound.SniffingJson,
                inbound.IsDefault,
                inbound.IsActive,
                inbound.Capacity,
                inbound.UsedCapacity,
                inbound.UpdatedAt);

        public void Restore(VpnInbound inbound)
        {
            inbound.ExternalInboundId = ExternalInboundId;
            inbound.Name = Name;
            inbound.Protocol = Protocol;
            inbound.Port = Port;
            inbound.Listen = Listen;
            inbound.SettingsJson = SettingsJson;
            inbound.StreamSettingsJson = StreamSettingsJson;
            inbound.SniffingJson = SniffingJson;
            inbound.IsDefault = IsDefault;
            inbound.IsActive = IsActive;
            inbound.Capacity = Capacity;
            inbound.UsedCapacity = UsedCapacity;
            inbound.UpdatedAt = UpdatedAt;
        }
    }

    private bool IsSandboxMode() => string.Equals(_configuration?["Vpn:X3Ui:Mode"], "Sandbox", StringComparison.OrdinalIgnoreCase);

    private static string SafeError(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "Panel operation failed."
            : SensitiveDataRedactor.Redact(value, maxLength: 500);

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
