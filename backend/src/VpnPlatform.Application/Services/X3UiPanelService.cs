using System.Diagnostics;
using System.Net;
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
    private const int PanelListLimit = 500;
    private const int InboundListLimit = 2000;
    private const int ClientListLimit = 2000;
    private const int DiagnosticsListLimit = 50;
    private const int SyncEventListLimit = 1000;
    private const string PanelChangedError = "VPN panel changed. Reload it and retry.";
    private const string PanelArchivedError = "VPN panel is already archived.";
    private const string ArchivedPanelReadOnlyError = "Archived VPN panel is read-only.";
    private const string InboundChangedError = "VPN inbound changed. Reload it and retry.";
    private const string ClientChangedError = "VPN client changed. Reload it and retry.";

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
        var panels = await _db.VpnPanels.AsNoTracking().OrderBy(x => x.Region).ThenBy(x => x.Name).Take(PanelListLimit).ToListAsync(cancellationToken);
        return panels.Select(MapPanel).ToList();
    }

    public async Task<Result<VpnPanelDto>> GetPanelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var panel = await _db.VpnPanels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return panel is null ? Result<VpnPanelDto>.Failure("VPN panel not found.") : Result<VpnPanelDto>.Success(MapPanel(panel));
    }

    public async Task<Result<ReadyVpnNodeDto>> AdoptReadyNodeAsync(
        Guid panelId,
        AdoptVpnPanelNodeCommand command,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (IsSandboxMode())
        {
            return Result<ReadyVpnNodeDto>.Failure("A production VPN node cannot be adopted while 3x-ui sandbox mode is enabled.");
        }

        var publicHostname = command.PublicHostname?.Trim() ?? string.Empty;
        if (!IsValidPublicHostname(publicHostname))
        {
            return Result<ReadyVpnNodeDto>.Failure("Public hostname must be a valid DNS name, IPv4 or IPv6 address.");
        }
        if (command.PublicPort is < 1 or > 65535)
        {
            return Result<ReadyVpnNodeDto>.Failure("Public port must be between 1 and 65535.");
        }

        var panel = await _db.VpnPanels
            .Include(x => x.Inbounds)
            .FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null)
        {
            return Result<ReadyVpnNodeDto>.Failure("VPN panel not found.");
        }
        if (panel.Status != VpnPanelStatus.Active || panel.HealthStatus != HealthStatus.Healthy)
        {
            return Result<ReadyVpnNodeDto>.Failure("VPN panel must be active and healthy before adopting a production node.");
        }

        var freshnessBoundary = _clock.UtcNow.AddMinutes(-10);
        if (panel.LastHealthCheckAt is null || panel.LastHealthCheckAt < freshnessBoundary
            || panel.LastSyncAt is null || panel.LastSyncAt < freshnessBoundary)
        {
            return Result<ReadyVpnNodeDto>.Failure("VPN panel health-check and inbound sync must both be completed within the last 10 minutes.");
        }

        var activeInbounds = panel.Inbounds
            .Where(x => x.IsActive && VpnProtocolPolicy.IsSupported(x.Protocol))
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Port)
            .ThenBy(x => x.Id)
            .ToArray();
        if (activeInbounds.Length == 0)
        {
            return Result<ReadyVpnNodeDto>.Failure("At least one active supported inbound is required before adopting a production node.");
        }

        var protocols = string.Join(',', activeInbounds
            .Select(x => VpnProtocolPolicy.Normalize(x.Protocol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.Ordinal));
        var primaryInbound = activeInbounds[0];
        var panelInboundId = int.TryParse(primaryInbound.ExternalInboundId, out var parsedInboundId)
            ? parsedInboundId
            : (int?)null;
        var node = await _db.VpnNodes.FirstOrDefaultAsync(
            x => x.PanelBaseUrl == panel.BaseUrl || x.Name == panel.Name,
            cancellationToken);

        object? before = null;
        if (node is null)
        {
            node = new VpnNode
            {
                Name = panel.Name,
                Provider = "x3ui",
                Region = panel.Region,
                Capacity = panel.Capacity,
                UsedCapacity = panel.UsedCapacity,
                Priority = 100,
                SshPort = 22,
                SshUser = "root",
                SkipHostKeyChecking = false,
                CreatedAt = _clock.UtcNow
            };
            _db.VpnNodes.Add(node);
        }
        else
        {
            if (node.Status is NodeStatus.Archived or NodeStatus.Disabled or NodeStatus.Maintenance
                || (!node.IsAvailableForNewUsers && node.Status is NodeStatus.Ready or NodeStatus.Draining))
            {
                return Result<ReadyVpnNodeDto>.Failure("Existing VPN node is operator-disabled, draining, in maintenance or archived and cannot be reopened by panel adoption.");
            }
            if (node.UsedCapacity > panel.Capacity)
            {
                return Result<ReadyVpnNodeDto>.Failure("VPN node used capacity exceeds panel capacity.");
            }
            before = ReadyNodeAuditSnapshot(node, panel.Id);
            node.Revision = checked(node.Revision + 1);
        }

        node.Name = panel.Name;
        node.Host = publicHostname;
        node.IpAddress = IPAddress.TryParse(publicHostname, out _) ? publicHostname : string.Empty;
        node.Provider = "x3ui";
        node.Region = panel.Region;
        node.Country = command.Country?.Trim() ?? string.Empty;
        node.Datacenter = command.Datacenter?.Trim() ?? string.Empty;
        node.Status = NodeStatus.Ready;
        node.HealthStatus = HealthStatus.Healthy;
        node.LastHealthCheckAt = panel.LastHealthCheckAt;
        node.ProvisioningStatus = ProvisioningRunStatus.Succeeded;
        node.Capacity = panel.Capacity;
        node.UsedCapacity = panel.UsedCapacity;
        node.SupportedProtocolsCsv = protocols;
        node.IsAvailableForNewUsers = true;
        node.PanelBaseUrl = panel.BaseUrl;
        node.PanelUsername = panel.Login;
        node.PanelInboundId = panelInboundId;
        node.PublicHostname = publicHostname;
        node.PublicPort = command.PublicPort;
        node.TagsCsv = "production,panel-adopted";
        node.UpdatedAt = _clock.UtcNow;

        AddAudit("vpn_panel.node.adopt", "VpnNode", node.Id, actorUserId, before, ReadyNodeAuditSnapshot(node, panel.Id));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ReadyVpnNodeDto>.Success(MapReadyNode(node, panel.Id));
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
            command.Region,
            command.Capacity,
            command.SslVerificationMode,
            command.ApiVariant,
            command.AuthenticationMode,
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
        var authenticationMode = Enum.Parse<VpnPanelAuthenticationMode>(command.AuthenticationMode, true);
        var panel = new VpnPanel
        {
            Name = name,
            BaseUrl = baseUrl,
            Login = command.Login?.Trim() ?? string.Empty,
            EncryptedPassword = _secretProtector.Protect(command.Password),
            Region = string.IsNullOrWhiteSpace(command.Region) ? "default" : command.Region.Trim(),
            Status = VpnPanelStatus.New,
            HealthStatus = HealthStatus.Unknown,
            Capacity = command.Capacity,
            SslVerificationMode = sslMode,
            ApiVariant = apiVariant,
            AuthenticationMode = authenticationMode,
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
        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(id, cancellationToken);
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (panel is null)
        {
            return Result<VpnPanelDto>.Failure("VPN panel not found.");
        }
        if (command.Revision.HasValue && command.Revision.Value != panel.Revision)
        {
            return Result<VpnPanelDto>.Failure(PanelChangedError);
        }
        if (panel.Status == VpnPanelStatus.Archived)
        {
            return Result<VpnPanelDto>.Failure(PanelArchivedError);
        }

        var name = string.IsNullOrWhiteSpace(command.Name) ? panel.Name : command.Name.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(command.BaseUrl) ? panel.BaseUrl : NormalizeBaseUrl(command.BaseUrl);
        var login = string.IsNullOrWhiteSpace(command.Login) ? panel.Login : command.Login.Trim();
        var capacity = command.Capacity ?? panel.Capacity;
        var sslModeText = string.IsNullOrWhiteSpace(command.SslVerificationMode) ? panel.SslVerificationMode.ToString() : command.SslVerificationMode;
        var apiVariantText = string.IsNullOrWhiteSpace(command.ApiVariant) ? panel.ApiVariant.ToString() : command.ApiVariant;
        var authenticationModeText = string.IsNullOrWhiteSpace(command.AuthenticationMode) ? panel.AuthenticationMode.ToString() : command.AuthenticationMode;
        var templateJson = string.IsNullOrWhiteSpace(command.DefaultInboundTemplateJson) ? panel.DefaultInboundTemplateJson : command.DefaultInboundTemplateJson.Trim();
        var statusText = string.IsNullOrWhiteSpace(command.Status) ? panel.Status.ToString() : command.Status;
        var validationError = ValidatePanelCommand(
            name,
            baseUrl,
            login,
            command.Password,
            string.IsNullOrWhiteSpace(command.Region) ? panel.Region : command.Region,
            capacity,
            sslModeText,
            apiVariantText,
            authenticationModeText,
            templateJson,
            statusText,
            passwordRequired: false);
        if (validationError is not null)
        {
            return Result<VpnPanelDto>.Failure(validationError);
        }
        if (capacity < panel.UsedCapacity)
        {
            return Result<VpnPanelDto>.Failure("Panel capacity cannot be lower than used capacity.");
        }

        if (await _db.VpnPanels.AnyAsync(
                x => x.Id != id && (x.Name.ToLower() == name.ToLower() || x.BaseUrl.ToLower() == baseUrl.ToLower()),
                cancellationToken))
        {
            return Result<VpnPanelDto>.Failure("A VPN panel with the same name or base URL already exists.");
        }

        var region = string.IsNullOrWhiteSpace(command.Region) ? panel.Region : command.Region.Trim();
        var sslMode = Enum.Parse<VpnSslVerificationMode>(sslModeText, true);
        var apiVariant = Enum.Parse<X3UiApiVariant>(apiVariantText, true);
        var authenticationMode = Enum.Parse<VpnPanelAuthenticationMode>(authenticationModeText, true);
        var autoCreateInbound = command.AutoCreateInbound ?? panel.AutoCreateInbound;
        var status = Enum.Parse<VpnPanelStatus>(statusText, true);
        if (status == VpnPanelStatus.Archived)
        {
            return Result<VpnPanelDto>.Failure("VPN panel cannot be archived through an update.");
        }
        if (string.IsNullOrWhiteSpace(command.Password)
            && panel.Name == name
            && panel.BaseUrl == baseUrl
            && panel.Login == login
            && panel.Region == region
            && panel.Capacity == capacity
            && panel.SslVerificationMode == sslMode
            && panel.ApiVariant == apiVariant
            && panel.AuthenticationMode == authenticationMode
            && panel.AutoCreateInbound == autoCreateInbound
            && panel.DefaultInboundTemplateJson == templateJson
            && panel.Status == status)
        {
            return Result<VpnPanelDto>.Failure("VPN panel changes were not detected.");
        }

        var before = PanelAuditSnapshot(panel);
        panel.Name = name;
        panel.BaseUrl = baseUrl;
        panel.Login = login;
        if (!string.IsNullOrWhiteSpace(command.Password)) panel.EncryptedPassword = _secretProtector.Protect(command.Password);
        panel.Region = region;
        panel.Capacity = capacity;
        panel.SslVerificationMode = sslMode;
        panel.ApiVariant = apiVariant;
        panel.AuthenticationMode = authenticationMode;
        panel.AutoCreateInbound = autoCreateInbound;
        panel.DefaultInboundTemplateJson = templateJson;
        panel.Status = status;
        panel.UpdatedAt = _clock.UtcNow;
        panel.Revision = checked(panel.Revision + 1);
        AddAudit("vpn_panel.update", "VpnPanel", panel.Id, actorUserId, before, PanelAuditSnapshot(panel));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearTracker();
            return Result<VpnPanelDto>.Failure(PanelChangedError);
        }
        return Result<VpnPanelDto>.Success(MapPanel(panel));
    }

    public Task<Result<DeleteVpnPanelResultDto>> DeletePanelAsync(Guid id, CancellationToken cancellationToken = default)
        => DeletePanelAsync(id, expectedRevision: null, actorUserId: null, cancellationToken);

    public Task<Result<DeleteVpnPanelResultDto>> DeletePanelAsync(Guid id, Guid? actorUserId, CancellationToken cancellationToken = default)
        => DeletePanelAsync(id, expectedRevision: null, actorUserId, cancellationToken);

    public async Task<Result<DeleteVpnPanelResultDto>> DeletePanelAsync(Guid id, int? expectedRevision, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(id, cancellationToken);
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (panel is null)
        {
            return Result<DeleteVpnPanelResultDto>.Failure("VPN panel not found.");
        }
        if (expectedRevision.HasValue && expectedRevision.Value != panel.Revision)
        {
            return Result<DeleteVpnPanelResultDto>.Failure(PanelChangedError);
        }
        if (panel.Status == VpnPanelStatus.Archived)
        {
            return Result<DeleteVpnPanelResultDto>.Failure(PanelArchivedError);
        }

        var linkedInbounds = await _db.VpnInbounds.CountAsync(x => x.VpnPanelId == id, cancellationToken);
        var linkedClients = await _db.VpnClients.CountAsync(x => x.VpnPanelId == id, cancellationToken);
        var linkedSyncRuns = await _db.PanelSyncRuns.CountAsync(x => x.VpnPanelId == id, cancellationToken);
        var linkedHealthChecks = await _db.PanelHealthChecks.CountAsync(x => x.VpnPanelId == id, cancellationToken);
        var before = PanelAuditSnapshot(panel);

        if (linkedInbounds > 0 || linkedClients > 0 || linkedSyncRuns > 0 || linkedHealthChecks > 0)
        {
            panel.Status = VpnPanelStatus.Archived;
            panel.HealthStatus = HealthStatus.Unknown;
            panel.LastError = "Panel archived by admin delete action because operational history is linked.";
            panel.UpdatedAt = _clock.UtcNow;
            panel.Revision = checked(panel.Revision + 1);
            AddAudit("vpn_panel.archive", "VpnPanel", panel.Id, actorUserId, before, new { panel.Status, linkedInbounds, linkedClients, linkedSyncRuns, linkedHealthChecks });
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                ClearTracker();
                return Result<DeleteVpnPanelResultDto>.Failure(PanelChangedError);
            }
            return Result<DeleteVpnPanelResultDto>.Success(new DeleteVpnPanelResultDto(id, Deleted: false, Archived: true, linkedInbounds, linkedClients, linkedSyncRuns, linkedHealthChecks));
        }

        _db.VpnPanels.Remove(panel);
        AddAudit("vpn_panel.delete", "VpnPanel", panel.Id, actorUserId, before, null);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearTracker();
            return Result<DeleteVpnPanelResultDto>.Failure(PanelChangedError);
        }
        return Result<DeleteVpnPanelResultDto>.Success(new DeleteVpnPanelResultDto(id, Deleted: true, Archived: false, linkedInbounds, linkedClients, linkedSyncRuns, linkedHealthChecks));
    }

    public Task<Result<PanelHealthCheckDto>> CheckHealthAsync(Guid panelId, CancellationToken cancellationToken = default)
        => CheckHealthAsync(panelId, null, cancellationToken);

    public async Task<Result<PanelHealthCheckDto>> CheckHealthAsync(Guid panelId, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(panelId, cancellationToken);
        return await CheckHealthCoreAsync(panelId, actorUserId, enforceExpectedLastHealthCheckAt: false, expectedLastHealthCheckAt: null, requireOperationalStatus: false, cancellationToken);
    }

    public async Task<Result<PanelHealthCheckDto>> CheckHealthIfCurrentAsync(
        Guid panelId,
        DateTimeOffset? expectedLastHealthCheckAt,
        CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(panelId, cancellationToken);
        return await CheckHealthCoreAsync(panelId, actorUserId: null, enforceExpectedLastHealthCheckAt: true, expectedLastHealthCheckAt, requireOperationalStatus: true, cancellationToken);
    }

    private async Task<Result<PanelHealthCheckDto>> CheckHealthCoreAsync(
        Guid panelId,
        Guid? actorUserId,
        bool enforceExpectedLastHealthCheckAt,
        DateTimeOffset? expectedLastHealthCheckAt,
        bool requireOperationalStatus,
        CancellationToken cancellationToken)
    {
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null)
        {
            return Result<PanelHealthCheckDto>.Failure("VPN panel not found.");
        }
        if (enforceExpectedLastHealthCheckAt && panel.LastHealthCheckAt != expectedLastHealthCheckAt)
        {
            return Result<PanelHealthCheckDto>.Failure("Panel health observation is stale; a newer check already completed.");
        }
        if (panel.Status == VpnPanelStatus.Archived)
        {
            return Result<PanelHealthCheckDto>.Failure(ArchivedPanelReadOnlyError);
        }
        if (requireOperationalStatus && panel.Status is not (VpnPanelStatus.Active or VpnPanelStatus.New))
        {
            return Result<PanelHealthCheckDto>.Failure("Panel is no longer active for scheduled health checks.");
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
            panel.Revision = checked(panel.Revision + 1);
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

        X3UiHealthResult health;
        try
        {
            var password = _secretProtector.Unprotect(panel.EncryptedPassword);
            health = await _client.CheckHealthAsync(panel, password, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return await RecordHealthFailureAsync(panel, actorUserId, before, sw, ex.Message, CancellationToken.None);
        }

        sw.Stop();
        var safeError = string.IsNullOrWhiteSpace(health.ErrorMessage) ? string.Empty : SafeError(health.ErrorMessage);
        var healthCheck = new PanelHealthCheck
        {
            VpnPanelId = panel.Id,
            Status = health.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
            LatencyMs = health.LatencyMs == 0 ? sw.ElapsedMilliseconds : health.LatencyMs,
            Version = health.Version,
            ErrorMessage = safeError,
            CheckedAt = _clock.UtcNow
        };
        _db.PanelHealthChecks.Add(healthCheck);
        ApplyHealthResult(panel, healthCheck);
        AddAudit("vpn_panel.health_check", "VpnPanel", panel.Id, actorUserId, before, HealthAuditSnapshot(healthCheck));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelHealthCheckDto>.Success(MapHealth(healthCheck));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception persistenceError)
        {
            return await RecoverHealthPersistenceAsync(panel.Id, actorUserId, before, healthCheck, persistenceError);
        }
    }

    private async Task<Result<PanelHealthCheckDto>> RecoverHealthPersistenceAsync(
        Guid panelId,
        Guid? actorUserId,
        object before,
        PanelHealthCheck attempted,
        Exception persistenceError)
    {
        ClearTracker();
        var persistedAttempt = await _db.PanelHealthChecks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == attempted.Id, CancellationToken.None);
        if (persistedAttempt is not null)
        {
            return Result<PanelHealthCheckDto>.Success(MapHealth(persistedAttempt));
        }

        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == panelId, CancellationToken.None)
            ?? throw new InvalidOperationException("VPN panel disappeared while recovering health-check persistence.", persistenceError);
        var recovered = new PanelHealthCheck
        {
            Id = attempted.Id,
            VpnPanelId = panel.Id,
            Status = attempted.Status,
            LatencyMs = attempted.LatencyMs,
            Version = attempted.Version,
            ErrorMessage = attempted.ErrorMessage,
            CheckedAt = attempted.CheckedAt
        };
        _db.PanelHealthChecks.Add(recovered);
        ApplyHealthResult(panel, recovered);
        AddAudit("vpn_panel.health_check.persistence_recovered", "VpnPanel", panel.Id, actorUserId, before, new
        {
            health = HealthAuditSnapshot(recovered),
            recovered = true,
            persistenceError = SafeError(persistenceError.Message)
        });
        try
        {
            await _db.SaveChangesAsync(CancellationToken.None);
            return Result<PanelHealthCheckDto>.Success(MapHealth(recovered));
        }
        catch (Exception recoveryError)
        {
            throw new InvalidOperationException(
                "VPN panel health result could not be persisted after retry.",
                new AggregateException(persistenceError, recoveryError));
        }
    }

    private static void ApplyHealthResult(VpnPanel panel, PanelHealthCheck health)
    {
        panel.HealthStatus = health.Status;
        panel.LastHealthCheckAt = health.CheckedAt;
        panel.LastError = health.ErrorMessage;
        panel.Version = health.Version;
        panel.Status = health.Status == HealthStatus.Healthy && panel.Status == VpnPanelStatus.New
            ? VpnPanelStatus.Active
            : panel.Status;
        panel.Revision = checked(panel.Revision + 1);
    }

    public Task<Result<PanelSyncRunDto>> SyncPanelAsync(Guid panelId, CancellationToken cancellationToken = default)
        => SyncPanelAsync(panelId, null, cancellationToken);

    public async Task<Result<PanelSyncRunDto>> SyncPanelAsync(Guid panelId, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var observation = await _db.VpnPanels.AsNoTracking()
            .Where(x => x.Id == panelId)
            .Select(x => new { x.LastSyncAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (observation is null)
        {
            return Result<PanelSyncRunDto>.Failure("VPN panel not found.");
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(panelId, cancellationToken);
        return await SyncPanelCoreAsync(panelId, actorUserId, enforceExpectedLastSyncAt: true, observation.LastSyncAt, cancellationToken);
    }

    public async Task<Result<PanelSyncRunDto>> SyncPanelIfCurrentAsync(
        Guid panelId,
        DateTimeOffset? expectedLastSyncAt,
        CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(panelId, cancellationToken);
        return await SyncPanelCoreAsync(
            panelId,
            actorUserId: null,
            enforceExpectedLastSyncAt: true,
            expectedLastSyncAt: expectedLastSyncAt,
            cancellationToken: cancellationToken);
    }

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
        if (panel.Status == VpnPanelStatus.Archived)
        {
            return Result<PanelSyncRunDto>.Failure(ArchivedPanelReadOnlyError);
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
        var previousPanelRevision = panel.Revision;
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
                panel.Revision = checked(panel.Revision + 1);
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
                panel.Revision = checked(panel.Revision + 1);
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
                local.Revision = checked(local.Revision + 1);
            }

            var missing = await _db.VpnInbounds.Where(x => x.VpnPanelId == panel.Id && !remoteIds.Contains(x.ExternalInboundId)).ToListAsync(cancellationToken);
            foreach (var inbound in missing)
            {
                inbound.IsActive = false;
                inbound.UpdatedAt = _clock.UtcNow;
                inbound.Revision = checked(inbound.Revision + 1);
                AddSyncEvent(run, "inbound_missing", "VpnInbound", inbound.Id, inbound.ExternalInboundId, "Inbound exists in DB but is missing on panel.", "{}");
            }

            await DetectClientDiffsAsync(panel.Id, run, remoteInbounds, cancellationToken);

            run.Status = PanelSyncRunStatus.Succeeded;
            run.FinishedAt = _clock.UtcNow;
            run.SummaryJson = JsonSerializer.Serialize(new { created, updated, missing = missing.Count });
            panel.LastSyncAt = run.FinishedAt;
            panel.LastError = string.Empty;
            panel.Revision = checked(panel.Revision + 1);
            AddAudit("vpn_panel.sync", "VpnPanel", panel.Id, actorUserId, before, SyncAuditSnapshot(run));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PanelSyncRunDto>.Success(MapSyncRun(run));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RestorePendingSyncChanges(panel, run, inboundSnapshots, previousLastSyncAt, previousPanelRevision, initialSyncAuditIds);
            run.Status = PanelSyncRunStatus.Failed;
            run.FinishedAt = _clock.UtcNow;
            run.ErrorMessage = "Panel sync was cancelled.";
            panel.LastError = run.ErrorMessage;
            panel.Revision = checked(panel.Revision + 1);
            AddAudit("vpn_panel.sync.cancelled", "VpnPanel", panel.Id, actorUserId, before, SyncAuditSnapshot(run));
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            RestorePendingSyncChanges(panel, run, inboundSnapshots, previousLastSyncAt, previousPanelRevision, initialSyncAuditIds);
            run.Status = PanelSyncRunStatus.Failed;
            run.FinishedAt = _clock.UtcNow;
            run.ErrorMessage = safeError;
            panel.LastError = safeError;
            panel.Revision = checked(panel.Revision + 1);
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
        panel.Revision = checked(panel.Revision + 1);
        AddAudit("vpn_panel.health_check.failed", "VpnPanel", panel.Id, actorUserId, before, HealthAuditSnapshot(entity));
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PanelHealthCheckDto>.Failure(safeError);
    }

    private void RestorePendingSyncChanges(
        VpnPanel panel,
        PanelSyncRun run,
        IReadOnlyDictionary<Guid, VpnInboundSyncSnapshot> inboundSnapshots,
        DateTimeOffset? previousLastSyncAt,
        int previousPanelRevision,
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
        panel.Revision = previousPanelRevision;
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

        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(panelId, cancellationToken);
        var panel = await _db.VpnPanels.FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null)
        {
            return Result<VpnInboundDto>.Failure("VPN panel not found.");
        }
        if (panel.Status == VpnPanelStatus.Archived)
        {
            return Result<VpnInboundDto>.Failure(ArchivedPanelReadOnlyError);
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
            foreach (var item in previousDefaults)
            {
                item.IsDefault = false;
                item.UpdatedAt = _clock.UtcNow;
                item.Revision = checked(item.Revision + 1);
            }
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
            foreach (var item in previousDefaults)
            {
                item.IsDefault = true;
                item.Revision = Math.Max(0, item.Revision - 1);
            }
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
        var inbounds = await _db.VpnInbounds.AsNoTracking().Where(x => x.VpnPanelId == panelId).OrderByDescending(x => x.IsDefault).ThenBy(x => x.Port).Take(InboundListLimit).ToListAsync(cancellationToken);
        return inbounds.Select(MapInbound).ToList();
    }

    public async Task<IReadOnlyCollection<VpnInboundDto>> GetInboundsAsync(CancellationToken cancellationToken = default)
    {
        var inbounds = await _db.VpnInbounds.AsNoTracking()
            .OrderBy(x => x.VpnPanelId)
            .ThenByDescending(x => x.IsDefault)
            .ThenBy(x => x.Port)
            .ThenBy(x => x.Id)
            .Take(InboundListLimit)
            .ToListAsync(cancellationToken);
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

        var observedPanelId = await _db.VpnInbounds.AsNoTracking()
            .Where(x => x.Id == inboundId)
            .Select(x => (Guid?)x.VpnPanelId)
            .FirstOrDefaultAsync(cancellationToken);
        if (observedPanelId is null)
        {
            return Result<VpnInboundDto>.Failure("VPN inbound not found.");
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(observedPanelId.Value, cancellationToken);
        var inbound = await _db.VpnInbounds.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.Id == inboundId, cancellationToken);
        if (inbound?.VpnPanel is null)
        {
            return Result<VpnInboundDto>.Failure("VPN inbound not found.");
        }
        if (command.Revision.HasValue && command.Revision.Value != inbound.Revision)
        {
            return Result<VpnInboundDto>.Failure(InboundChangedError);
        }
        if (inbound.VpnPanel.Status == VpnPanelStatus.Archived)
        {
            return Result<VpnInboundDto>.Failure(ArchivedPanelReadOnlyError);
        }
        if (command.Capacity < inbound.UsedCapacity)
        {
            return Result<VpnInboundDto>.Failure("Inbound capacity cannot be lower than used capacity.");
        }

        var normalizedProtocol = NormalizeProtocol(command.Protocol);
        var isDefault = command.IsDefault && command.IsActive;
        if (inbound.Name == command.Name
            && inbound.Protocol == normalizedProtocol
            && inbound.Port == command.Port
            && inbound.Listen == command.Listen
            && inbound.SettingsJson == command.SettingsJson
            && inbound.StreamSettingsJson == command.StreamSettingsJson
            && inbound.SniffingJson == command.SniffingJson
            && inbound.IsActive == command.IsActive
            && inbound.IsDefault == isDefault
            && inbound.Capacity == command.Capacity)
        {
            return Result<VpnInboundDto>.Failure("VPN inbound changes were not detected.");
        }

        var before = InboundAuditSnapshot(inbound);
        var previousRemoteRequest = new X3UiUpdateInboundRequest(
            inbound.ExternalInboundId,
            inbound.Name,
            inbound.Protocol,
            inbound.Port,
            inbound.Listen,
            inbound.SettingsJson,
            inbound.StreamSettingsJson,
            inbound.SniffingJson,
            inbound.IsActive);
        var remoteMutationAttempted = false;
        string? password = null;
        X3UiInboundDto remote;
        if (IsSandboxMode())
        {
            remote = new X3UiInboundDto(inbound.ExternalInboundId, command.Name, normalizedProtocol, command.Port, command.Listen, command.SettingsJson, command.StreamSettingsJson, command.SniffingJson, command.IsActive);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(inbound.VpnPanel.BaseUrl) || string.IsNullOrWhiteSpace(inbound.VpnPanel.Login) || string.IsNullOrWhiteSpace(inbound.VpnPanel.EncryptedPassword))
            {
                return Result<VpnInboundDto>.Failure("Panel not configured: base URL, login and password are required.");
            }
            password = _secretProtector.Unprotect(inbound.VpnPanel.EncryptedPassword);
            try
            {
                remoteMutationAttempted = true;
                remote = await _client.UpdateInboundAsync(inbound.VpnPanel, password, new X3UiUpdateInboundRequest(inbound.ExternalInboundId, command.Name, normalizedProtocol, command.Port, command.Listen, command.SettingsJson, command.StreamSettingsJson, command.SniffingJson, command.IsActive), cancellationToken);
            }
            catch (Exception remoteError)
            {
                await CompensateInboundUpdateFailureAsync(inbound, password, previousRemoteRequest, actorUserId, before, remoteError, "remote_operation_failed");
                throw;
            }
        }

        try
        {
            inbound.Name = remote.Remark;
            inbound.Protocol = remote.Protocol;
            inbound.Port = remote.Port;
            inbound.Listen = remote.Listen;
            inbound.SettingsJson = remote.SettingsJson;
            inbound.StreamSettingsJson = remote.StreamSettingsJson;
            inbound.SniffingJson = remote.SniffingJson;
            inbound.IsActive = remote.Enable;
            inbound.IsDefault = isDefault;
            inbound.Capacity = command.Capacity > 0 ? command.Capacity : inbound.Capacity;
            inbound.UpdatedAt = _clock.UtcNow;
            inbound.Revision = checked(inbound.Revision + 1);
            if (inbound.IsDefault)
            {
                var defaults = await _db.VpnInbounds.Where(x => x.VpnPanelId == inbound.VpnPanelId && x.Id != inbound.Id && x.IsDefault).ToListAsync(cancellationToken);
                foreach (var item in defaults)
                {
                    item.IsDefault = false;
                    item.UpdatedAt = _clock.UtcNow;
                    item.Revision = checked(item.Revision + 1);
                }
            }

            AddAudit("vpn_inbound.update", "VpnInbound", inbound.Id, actorUserId, before, InboundAuditSnapshot(inbound));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VpnInboundDto>.Success(MapInbound(inbound));
        }
        catch (DbUpdateConcurrencyException concurrencyError) when (remoteMutationAttempted)
        {
            await CompensateInboundUpdateFailureAsync(inbound, password!, previousRemoteRequest, actorUserId, before, concurrencyError, "concurrency_conflict");
            return Result<VpnInboundDto>.Failure(InboundChangedError);
        }
        catch (Exception localError) when (remoteMutationAttempted)
        {
            await CompensateInboundUpdateFailureAsync(inbound, password!, previousRemoteRequest, actorUserId, before, localError, "local_persistence_failed");
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearTracker();
            return Result<VpnInboundDto>.Failure(InboundChangedError);
        }
    }

    private async Task CompensateInboundUpdateFailureAsync(
        VpnInbound inbound,
        string password,
        X3UiUpdateInboundRequest previousRemoteRequest,
        Guid? actorUserId,
        object before,
        Exception operationError,
        string outcome)
    {
        try
        {
            await _client.UpdateInboundAsync(inbound.VpnPanel!, password, previousRemoteRequest, CancellationToken.None);
        }
        catch (Exception compensationError)
        {
            ClearTracker();
            AddAudit("vpn_inbound.update.compensation_failed", "VpnInbound", inbound.Id, actorUserId, before, new
            {
                remoteInboundId = previousRemoteRequest.Id,
                compensated = false,
                reconciliationRequired = true,
                outcome,
                error = SafeError(operationError.Message),
                compensationError = SafeError(compensationError.Message)
            });
            await _db.SaveChangesAsync(CancellationToken.None);
            throw new InvalidOperationException(
                "VPN inbound state is uncertain and remote rollback failed; manual provider reconciliation is required.",
                new AggregateException(operationError, compensationError));
        }

        ClearTracker();
        AddAudit("vpn_inbound.update.failed", "VpnInbound", inbound.Id, actorUserId, before, new
        {
            remoteInboundId = previousRemoteRequest.Id,
            compensated = true,
            outcome,
            error = SafeError(operationError.Message)
        });
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    public Task<Result<VpnInboundDto>> SetDefaultInboundAsync(Guid inboundId, CancellationToken cancellationToken = default)
        => SetDefaultInboundAsync(inboundId, expectedRevision: null, actorUserId: null, cancellationToken);

    public Task<Result<VpnInboundDto>> SetDefaultInboundAsync(Guid inboundId, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SetDefaultInboundAsync(inboundId, expectedRevision: null, actorUserId, cancellationToken);

    public async Task<Result<VpnInboundDto>> SetDefaultInboundAsync(Guid inboundId, int? expectedRevision, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var observedPanelId = await _db.VpnInbounds.AsNoTracking()
            .Where(x => x.Id == inboundId)
            .Select(x => (Guid?)x.VpnPanelId)
            .FirstOrDefaultAsync(cancellationToken);
        if (observedPanelId is null)
        {
            return Result<VpnInboundDto>.Failure("VPN inbound not found.");
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnPanelStateAsync(observedPanelId.Value, cancellationToken);
        var inbound = await _db.VpnInbounds.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.Id == inboundId, cancellationToken);
        if (inbound?.VpnPanel is null)
        {
            return Result<VpnInboundDto>.Failure("VPN inbound not found.");
        }
        if (expectedRevision.HasValue && expectedRevision.Value != inbound.Revision)
        {
            return Result<VpnInboundDto>.Failure(InboundChangedError);
        }
        if (inbound.VpnPanel.Status == VpnPanelStatus.Archived)
        {
            return Result<VpnInboundDto>.Failure(ArchivedPanelReadOnlyError);
        }
        if (!inbound.IsActive)
        {
            return Result<VpnInboundDto>.Failure("Inactive inbound cannot be default.");
        }

        var before = InboundAuditSnapshot(inbound);
        var all = await _db.VpnInbounds.Where(x => x.VpnPanelId == inbound.VpnPanelId).ToListAsync(cancellationToken);
        foreach (var item in all)
        {
            var shouldBeDefault = item.Id == inbound.Id;
            if (item.IsDefault != shouldBeDefault)
            {
                item.IsDefault = shouldBeDefault;
                item.UpdatedAt = _clock.UtcNow;
                item.Revision = checked(item.Revision + 1);
            }
        }
        AddAudit("vpn_inbound.default.set", "VpnInbound", inbound.Id, actorUserId, before, InboundAuditSnapshot(inbound));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearTracker();
            return Result<VpnInboundDto>.Failure(InboundChangedError);
        }
        return Result<VpnInboundDto>.Success(MapInbound(inbound));
    }

    private static string? ValidateInboundCommand(CreateVpnInboundCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return "Inbound name is required.";
        }
        if (command.Name.Trim().Length > 200) return "Inbound name must not exceed 200 characters.";
        if (command.Protocol.Trim().Length > 32) return "Inbound protocol must not exceed 32 characters.";
        if (command.Listen.Trim().Length > 255) return "Inbound listen address must not exceed 255 characters.";
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
        if (json.Length > 32768)
        {
            return $"{fieldName} must not exceed 32768 characters.";
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
        string? region,
        int capacity,
        string sslVerificationMode,
        string apiVariant,
        string authenticationMode,
        string defaultInboundTemplateJson,
        string? status,
        bool passwordRequired)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Panel name is required.";
        }
        if (name.Trim().Length > 200) return "Panel name must not exceed 200 characters.";
        if (baseUrl.Trim().Length > 2048) return "Base URL must not exceed 2048 characters.";
        if (SafeHttpUrl.ContainsCredentials(baseUrl))
        {
            return "Base URL must not contain credentials (login or password).";
        }
        if (!SafeHttpUrl.TryNormalize(baseUrl, out _))
        {
            return "Base URL must be an absolute HTTP or HTTPS URL.";
        }
        if (!Enum.TryParse<VpnPanelAuthenticationMode>(authenticationMode, true, out var parsedAuthenticationMode)
            || !Enum.IsDefined(parsedAuthenticationMode))
        {
            return "Authentication mode is invalid.";
        }
        if (parsedAuthenticationMode == VpnPanelAuthenticationMode.PasswordSession && string.IsNullOrWhiteSpace(login))
        {
            return "Login is required for password session authentication.";
        }
        if (!string.IsNullOrWhiteSpace(login) && login.Trim().Length > 200) return "Login must not exceed 200 characters.";
        if (!string.IsNullOrWhiteSpace(password) && password.Length > 4096) return "Password must not exceed 4096 characters.";
        if (!string.IsNullOrWhiteSpace(region) && region.Trim().Length > 120) return "Region must not exceed 120 characters.";
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
        var clients = _db is DbContext dbContext && IsSqlite(dbContext)
            ? await dbContext.Set<VpnClient>().FromSqlInterpolated($"""
                SELECT * FROM "VpnClients"
                WHERE "VpnPanelId" = {panelId}
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT {ClientListLimit}
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await _db.VpnClients.AsNoTracking().Where(x => x.VpnPanelId == panelId).OrderByDescending(x => x.CreatedAt).Take(ClientListLimit).ToListAsync(cancellationToken);
        return clients.Select(MapClient).ToList();
    }

    public Task<Result<VpnClientDto>> EnableClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, true, expectedRevision: null, actorUserId: null, cancellationToken);

    public Task<Result<VpnClientDto>> EnableClientAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, true, expectedRevision: null, actorUserId, cancellationToken);

    public Task<Result<VpnClientDto>> EnableClientAsync(Guid clientId, int? expectedRevision, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, true, expectedRevision, actorUserId: null, cancellationToken);

    public Task<Result<VpnClientDto>> EnableClientAsync(Guid clientId, int? expectedRevision, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, true, expectedRevision, actorUserId, cancellationToken);

    public Task<Result<VpnClientDto>> DisableClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, false, expectedRevision: null, actorUserId: null, cancellationToken);

    public Task<Result<VpnClientDto>> DisableClientAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, false, expectedRevision: null, actorUserId, cancellationToken);

    public Task<Result<VpnClientDto>> DisableClientAsync(Guid clientId, int? expectedRevision, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SetClientEnabledAsync(clientId, false, expectedRevision, actorUserId, cancellationToken);

    public Task<Result<VpnClientDto>> SyncClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        => SyncClientAsync(clientId, expectedRevision: null, actorUserId: null, cancellationToken);

    public Task<Result<VpnClientDto>> SyncClientAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken = default)
        => SyncClientAsync(clientId, expectedRevision: null, actorUserId, cancellationToken);

    public async Task<Result<VpnClientDto>> SyncClientAsync(Guid clientId, int? expectedRevision, Guid? actorUserId, CancellationToken cancellationToken = default)
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
        if (expectedRevision.HasValue && expectedRevision.Value != client.Revision)
        {
            return Result<VpnClientDto>.Failure(ClientChangedError);
        }
        if (client.VpnPanel.Status == VpnPanelStatus.Archived)
        {
            return Result<VpnClientDto>.Failure(ArchivedPanelReadOnlyError);
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
            await _client.GetClientTrafficAsync(client.VpnPanel, password, client.Email, cancellationToken);
        }

        client.SyncStatus = IsSandboxMode() ? "sandbox-synced" : "synced";
        client.LastSyncedAt = _clock.UtcNow;
        client.UpdatedAt = _clock.UtcNow;
        client.Revision = checked(client.Revision + 1);
        await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
        AddAudit("vpn_client.sync", "VpnClient", client.Id, actorUserId, before, ClientAuditSnapshot(client));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearTracker();
            return Result<VpnClientDto>.Failure(ClientChangedError);
        }
        return Result<VpnClientDto>.Success(MapClient(client));
    }

    public Task<Result<VpnClientDto>> ResetClientTrafficAsync(Guid clientId, CancellationToken cancellationToken = default)
        => ResetClientTrafficAsync(clientId, expectedRevision: null, actorUserId: null, cancellationToken);

    public Task<Result<VpnClientDto>> ResetClientTrafficAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken = default)
        => ResetClientTrafficAsync(clientId, expectedRevision: null, actorUserId, cancellationToken);

    public async Task<Result<VpnClientDto>> ResetClientTrafficAsync(Guid clientId, int? expectedRevision, Guid? actorUserId, CancellationToken cancellationToken = default)
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
        if (expectedRevision.HasValue && expectedRevision.Value != client.Revision)
        {
            return Result<VpnClientDto>.Failure(ClientChangedError);
        }
        if (client.VpnPanel.Status == VpnPanelStatus.Archived)
        {
            return Result<VpnClientDto>.Failure(ArchivedPanelReadOnlyError);
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
                await _client.ResetClientTrafficAsync(client.VpnPanel, password, client.VpnInbound.ExternalInboundId, client.Email, cancellationToken);
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
            client.Revision = checked(client.Revision + 1);
            await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
            AddAudit("vpn_client.traffic.reset", "VpnClient", client.Id, actorUserId, before, ClientAuditSnapshot(client));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VpnClientDto>.Success(MapClient(client));
        }
        catch (Exception localError) when (remoteMutationAttempted && remoteMutationCompleted)
        {
            await PersistTrafficResetUncertaintyAsync(client.Id, actorUserId, before, localError, "local_persistence_failed");
            if (localError is DbUpdateConcurrencyException) return Result<VpnClientDto>.Failure(ClientChangedError);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearTracker();
            return Result<VpnClientDto>.Failure(ClientChangedError);
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
        if (command.Revision.HasValue && command.Revision.Value != client.Revision)
        {
            return Result<VpnClientDto>.Failure(ClientChangedError);
        }
        if (client.VpnPanel.Status == VpnPanelStatus.Archived)
        {
            return Result<VpnClientDto>.Failure(ArchivedPanelReadOnlyError);
        }

        var targetInbound = await _db.VpnInbounds.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.Id == command.TargetInboundId, cancellationToken);
        if (targetInbound?.VpnPanel is null)
        {
            return Result<VpnClientDto>.Failure("Target inbound not found.");
        }
        VpnNode? targetNode = null;
        if (command.TargetNodeId.HasValue)
        {
            targetNode = await _db.VpnNodes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.TargetNodeId.Value, cancellationToken);
            if (targetNode is null)
            {
                return Result<VpnClientDto>.Failure("Target VPN server not found.");
            }
            if (!string.Equals(targetNode.PanelBaseUrl, targetInbound.VpnPanel.BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return Result<VpnClientDto>.Failure("Target VPN server does not own the selected panel.");
            }
            if (targetNode.Status != NodeStatus.Ready
                || !targetNode.IsAvailableForNewUsers
                || targetNode.HealthStatus == HealthStatus.Unhealthy
                || targetNode.UsedCapacity >= targetNode.Capacity)
            {
                return Result<VpnClientDto>.Failure("Target VPN server is not ready for allocation.");
            }
        }
        else
        {
            targetNode = await _db.VpnNodes.AsNoTracking()
                .Where(x => x.PanelBaseUrl == targetInbound.VpnPanel.BaseUrl)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        var sourceNodeId = await _db.Subscriptions.AsNoTracking()
            .Where(x => x.Id == client.SubscriptionId)
            .Select(x => x.CurrentServerId)
            .FirstOrDefaultAsync(cancellationToken);
        var reservedTargetNodeId = targetNode is not null && targetNode.Id != sourceNodeId ? targetNode.Id : (Guid?)null;
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
            client.Revision = checked(client.Revision + 1);
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

        var capacityResult = await TryReserveMigrationTargetCapacityAsync(targetInbound, reservedTargetNodeId, cancellationToken);
        if (!capacityResult.IsSuccess)
        {
            return Result<VpnClientDto>.Failure(capacityResult.Error ?? "Target VPN capacity is unavailable.");
        }

        ClearTracker();
        client = await LoadClientForActionAsync(clientId, cancellationToken);
        targetInbound = await _db.VpnInbounds.Include(x => x.VpnPanel).FirstOrDefaultAsync(x => x.Id == command.TargetInboundId, cancellationToken);
        if (client?.VpnPanel is null || client.VpnInbound is null || targetInbound?.VpnPanel is null)
        {
            await ReleaseMigrationTargetCapacityAsync(command.TargetInboundId, reservedTargetNodeId, cancellationToken);
            return Result<VpnClientDto>.Failure("VPN client or target inbound disappeared during migration.");
        }
        if (command.Revision.HasValue && command.Revision.Value != client.Revision)
        {
            await ReleaseMigrationTargetCapacityAsync(command.TargetInboundId, reservedTargetNodeId, cancellationToken);
            return Result<VpnClientDto>.Failure(ClientChangedError);
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
                    client.Email,
                    actorUserId,
                    before,
                    targetCreateError,
                    reservedTargetNodeId);
                throw;
            }

            try
            {
                await _client.DeleteClientAsync(sourcePanel, sourcePassword!, sourceInbound.ExternalInboundId, client.Uuid, client.Email, cancellationToken);
            }
            catch (Exception sourceDeleteError)
            {
                try
                {
                    await _client.AddClientAsync(sourcePanel, sourcePassword!, sourceRequest, CancellationToken.None);
                    await _client.DeleteClientAsync(targetInbound.VpnPanel, targetPassword!, targetInbound.ExternalInboundId, client.Uuid, client.Email, CancellationToken.None);
                    ClearTracker();
                    await ReleaseMigrationTargetCapacityAsync(targetInbound.Id, reservedTargetNodeId, CancellationToken.None);
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
            client.Revision = checked(client.Revision + 1);
            await UpdateLinkedAccessCredentialsAsync(client, cancellationToken, targetNode?.Id);
            if (targetNode is not null)
            {
                var subscription = await _db.Subscriptions.FirstOrDefaultAsync(x => x.Id == client.SubscriptionId, cancellationToken);
                if (subscription is not null)
                {
                    subscription.CurrentServerId = targetNode.Id;
                    subscription.UpdatedAt = _clock.UtcNow;
                    if (sourceNodeId.HasValue && sourceNodeId.Value != targetNode.Id)
                    {
                        if (_db is DbContext sourceDbContext && sourceDbContext.Database.IsRelational())
                        {
                            await _db.VpnNodes
                                .Where(x => x.Id == sourceNodeId.Value && x.UsedCapacity > 0)
                                .ExecuteUpdateAsync(setters => setters
                                    .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity - 1)
                                    .SetProperty(x => x.Revision, x => x.Revision + 1)
                                    .SetProperty(x => x.UpdatedAt, _clock.UtcNow), cancellationToken);
                        }
                        else
                        {
                            var trackedSourceNode = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == sourceNodeId.Value, cancellationToken);
                            if (trackedSourceNode is not null && trackedSourceNode.UsedCapacity > 0)
                            {
                                trackedSourceNode.UsedCapacity -= 1;
                                trackedSourceNode.Revision = checked(trackedSourceNode.Revision + 1);
                                trackedSourceNode.UpdatedAt = _clock.UtcNow;
                            }
                        }
                    }
                }
            }
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
                    await _client.DeleteClientAsync(targetInbound.VpnPanel, targetPassword!, targetInbound.ExternalInboundId, client.Uuid, client.Email, CancellationToken.None);
                }
                await ReleaseMigrationTargetCapacityAsync(targetInbound.Id, reservedTargetNodeId, CancellationToken.None);
            }
            catch (Exception compensationError)
            {
                await PersistMigrationManualCleanupAsync(clientId, sourceInbound.Id, targetInbound.Id, actorUserId, before, localError, compensationError, "local_save_compensation_failed");
                throw new InvalidOperationException(
                    "VPN client migration local persistence failed and remote rollback is uncertain; manual provider reconciliation is required.",
                    new AggregateException(localError, compensationError));
            }

            await PersistMigrationFailureAsync(clientId, sourceInbound.Id, targetInbound.Id, actorUserId, before, localError, "remote_rolled_back", compensated: true);
            if (localError is DbUpdateConcurrencyException) return Result<VpnClientDto>.Failure(ClientChangedError);
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
        var runs = _db is DbContext dbContext && IsSqlite(dbContext)
            ? await dbContext.Set<PanelSyncRun>().FromSqlInterpolated($"""
                SELECT * FROM "PanelSyncRuns"
                WHERE "VpnPanelId" = {panelId}
                ORDER BY julianday("StartedAt") DESC, "Id" DESC
                LIMIT {DiagnosticsListLimit}
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await _db.PanelSyncRuns.AsNoTracking().Where(x => x.VpnPanelId == panelId).OrderByDescending(x => x.StartedAt).Take(DiagnosticsListLimit).ToListAsync(cancellationToken);
        return runs.Select(MapSyncRun).ToList();
    }

    public async Task<IReadOnlyCollection<PanelSyncEventDto>> GetSyncEventsAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var events = _db is DbContext dbContext && IsSqlite(dbContext)
            ? await dbContext.Set<PanelSyncEvent>().FromSqlInterpolated($"""
                SELECT * FROM "PanelSyncEvents"
                WHERE "PanelSyncRunId" = {runId}
                ORDER BY julianday("CreatedAt"), "Id"
                LIMIT {SyncEventListLimit}
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await _db.PanelSyncEvents.AsNoTracking().Where(x => x.PanelSyncRunId == runId).OrderBy(x => x.CreatedAt).Take(SyncEventListLimit).ToListAsync(cancellationToken);
        return events.Select(MapSyncEvent).ToList();
    }

    public async Task<IReadOnlyCollection<PanelHealthCheckDto>> GetHealthChecksAsync(Guid panelId, CancellationToken cancellationToken = default)
    {
        var checks = _db is DbContext dbContext && IsSqlite(dbContext)
            ? await dbContext.Set<PanelHealthCheck>().FromSqlInterpolated($"""
                SELECT * FROM "PanelHealthChecks"
                WHERE "VpnPanelId" = {panelId}
                ORDER BY julianday("CheckedAt") DESC, "Id" DESC
                LIMIT {DiagnosticsListLimit}
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await _db.PanelHealthChecks.AsNoTracking().Where(x => x.VpnPanelId == panelId).OrderByDescending(x => x.CheckedAt).Take(DiagnosticsListLimit).ToListAsync(cancellationToken);
        return checks.Select(MapHealth).ToList();
    }

    private async Task<Result<VpnClientDto>> SetClientEnabledAsync(Guid clientId, bool enabled, int? expectedRevision, Guid? actorUserId, CancellationToken cancellationToken)
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
        if (expectedRevision.HasValue && expectedRevision.Value != client.Revision)
        {
            return Result<VpnClientDto>.Failure(ClientChangedError);
        }
        if (client.VpnPanel.Status == VpnPanelStatus.Archived)
        {
            return Result<VpnClientDto>.Failure(ArchivedPanelReadOnlyError);
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
            client.Revision = checked(client.Revision + 1);
            await UpdateLinkedAccessCredentialsAsync(client, cancellationToken);
            AddAudit(action, "VpnClient", client.Id, actorUserId, before, ClientAuditSnapshot(client));
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VpnClientDto>.Success(MapClient(client));
        }
        catch (Exception localError) when (remoteMutationAttempted)
        {
            await CompensateClientEnabledFailureAsync(client, password!, previousRemoteRequest, actorUserId, before, action, localError, "local_persistence_failed");
            if (localError is DbUpdateConcurrencyException) return Result<VpnClientDto>.Failure(ClientChangedError);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearTracker();
            return Result<VpnClientDto>.Failure(ClientChangedError);
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

    private async Task<Result<bool>> TryReserveMigrationTargetCapacityAsync(VpnInbound targetInbound, Guid? targetNodeId, CancellationToken cancellationToken)
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
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity + 1)
                    .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);
            if (panelReserved != 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result<bool>.Failure("Target panel capacity is exhausted or unavailable.");
            }

            var inboundReserved = await _db.VpnInbounds
                .Where(x => x.Id == targetInbound.Id && x.IsActive && x.UsedCapacity < x.Capacity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity + 1)
                    .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);
            if (inboundReserved != 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result<bool>.Failure("Target inbound capacity is exhausted or unavailable.");
            }

            if (targetNodeId.HasValue)
            {
                var nodeReserved = await _db.VpnNodes
                    .Where(x => x.Id == targetNodeId.Value
                        && x.Status == NodeStatus.Ready
                        && x.IsAvailableForNewUsers
                        && x.HealthStatus != HealthStatus.Unhealthy
                        && x.UsedCapacity < x.Capacity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity + 1)
                        .SetProperty(x => x.Revision, x => x.Revision + 1)
                        .SetProperty(x => x.UpdatedAt, _clock.UtcNow), cancellationToken);
                if (nodeReserved != 1)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return Result<bool>.Failure("Target VPN server capacity is exhausted or unavailable.");
                }
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
        targetInbound.VpnPanel.Revision = checked(targetInbound.VpnPanel.Revision + 1);
        targetInbound.Revision = checked(targetInbound.Revision + 1);
        VpnNode? targetNode = null;
        if (targetNodeId.HasValue)
        {
            targetNode = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == targetNodeId.Value, cancellationToken);
            if (targetNode is null || targetNode.Status != NodeStatus.Ready || !targetNode.IsAvailableForNewUsers
                || targetNode.HealthStatus == HealthStatus.Unhealthy || targetNode.UsedCapacity >= targetNode.Capacity)
            {
                targetInbound.VpnPanel.UsedCapacity -= 1;
                targetInbound.UsedCapacity -= 1;
                targetInbound.VpnPanel.Revision = Math.Max(0, targetInbound.VpnPanel.Revision - 1);
                targetInbound.Revision = Math.Max(0, targetInbound.Revision - 1);
                return Result<bool>.Failure("Target VPN server capacity is exhausted or unavailable.");
            }
            targetNode.UsedCapacity += 1;
            targetNode.Revision = checked(targetNode.Revision + 1);
            targetNode.UpdatedAt = _clock.UtcNow;
        }
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
        catch
        {
            targetInbound.VpnPanel.UsedCapacity -= 1;
            targetInbound.UsedCapacity -= 1;
            targetInbound.VpnPanel.Revision = Math.Max(0, targetInbound.VpnPanel.Revision - 1);
            targetInbound.Revision = Math.Max(0, targetInbound.Revision - 1);
            if (targetNode is not null)
            {
                targetNode.UsedCapacity -= 1;
                targetNode.Revision = Math.Max(0, targetNode.Revision - 1);
            }
            throw;
        }
    }

    private async Task ReleaseMigrationTargetCapacityAsync(Guid targetInboundId, Guid? targetNodeId, CancellationToken cancellationToken)
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
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity - 1)
                    .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);
            var panelReleased = await _db.VpnPanels
                .Where(x => x.Id == targetPanelId && x.UsedCapacity > 0)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity - 1)
                    .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);
            var nodeReleased = !targetNodeId.HasValue || await _db.VpnNodes
                .Where(x => x.Id == targetNodeId.Value && x.UsedCapacity > 0)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity - 1)
                    .SetProperty(x => x.Revision, x => x.Revision + 1)
                    .SetProperty(x => x.UpdatedAt, _clock.UtcNow), cancellationToken) == 1;
            if (inboundReleased != 1 || panelReleased != 1 || !nodeReleased)
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
        inbound.Revision = checked(inbound.Revision + 1);
        inbound.VpnPanel.Revision = checked(inbound.VpnPanel.Revision + 1);
        if (targetNodeId.HasValue)
        {
            var targetNode = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == targetNodeId.Value, cancellationToken);
            if (targetNode is null || targetNode.UsedCapacity <= 0)
            {
                throw new InvalidOperationException("Reserved target VPN server capacity could not be released consistently.");
            }
            targetNode.UsedCapacity -= 1;
            targetNode.Revision = checked(targetNode.Revision + 1);
            targetNode.UpdatedAt = _clock.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleTargetOnlyMigrationFailureAsync(
        Guid clientId,
        Guid sourceInboundId,
        VpnInbound targetInbound,
        string targetPassword,
        string clientUuid,
        string clientEmail,
        Guid? actorUserId,
        object before,
        Exception migrationError,
        Guid? targetNodeId)
    {
        try
        {
            await _client.DeleteClientAsync(targetInbound.VpnPanel!, targetPassword, targetInbound.ExternalInboundId, clientUuid, clientEmail, CancellationToken.None);
            ClearTracker();
            await ReleaseMigrationTargetCapacityAsync(targetInbound.Id, targetNodeId, CancellationToken.None);
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
        => string.IsNullOrWhiteSpace(panel.BaseUrl)
           || string.IsNullOrWhiteSpace(panel.EncryptedPassword)
           || (panel.AuthenticationMode == VpnPanelAuthenticationMode.PasswordSession && string.IsNullOrWhiteSpace(panel.Login))
            ? "Panel not configured: base URL and credential are required; login is also required for password sessions."
            : null;

    private async Task UpdateLinkedAccessCredentialsAsync(VpnClient client, CancellationToken cancellationToken, Guid? targetNodeId = null)
    {
        var currentAccessId = await _db.Subscriptions.AsNoTracking()
            .Where(x => x.Id == client.SubscriptionId)
            .Select(x => x.CurrentAccessId)
            .FirstOrDefaultAsync(cancellationToken);
        var accesses = await _db.AccessCredentials
            .Where(x => x.SubscriptionId == client.SubscriptionId
                && (x.Id == currentAccessId || x.ProviderAccessId == client.ExternalClientId || x.ProviderAccessId == client.Id.ToString()))
            .ToListAsync(cancellationToken);

        foreach (var access in accesses)
        {
            access.ProviderAccessId = client.ExternalClientId;
            access.AccessUri = client.ConfigUri;
            access.QrCodePath = client.QrCodePayload;
            if (targetNodeId.HasValue)
            {
                access.ServerId = targetNodeId.Value;
            }
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
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(client.Uuid) || inbound.Port is < 1 or > 65535)
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
            panel.AuthenticationMode,
            panel.Capacity,
            panel.UsedCapacity,
            panel.AutoCreateInbound,
            passwordConfigured = !string.IsNullOrWhiteSpace(panel.EncryptedPassword),
            panel.LastHealthCheckAt,
            panel.LastSyncAt
        };

    private static object ReadyNodeAuditSnapshot(VpnNode node, Guid panelId)
        => new
        {
            node.Id,
            VpnPanelId = panelId,
            node.Name,
            node.Host,
            node.Provider,
            node.Region,
            node.Status,
            node.HealthStatus,
            node.Capacity,
            node.UsedCapacity,
            node.SupportedProtocolsCsv,
            node.IsAvailableForNewUsers,
            node.PanelBaseUrl,
            node.PanelInboundId,
            node.PublicHostname,
            node.PublicPort,
            node.Revision
        };

    private static ReadyVpnNodeDto MapReadyNode(VpnNode node, Guid panelId)
        => new(
            node.Id,
            panelId,
            node.Name,
            node.Host,
            node.Region,
            node.Status.ToString(),
            node.HealthStatus.ToString(),
            node.SupportedProtocolsCsv,
            node.Capacity,
            node.UsedCapacity,
            node.IsAvailableForNewUsers,
            node.PanelBaseUrl,
            node.PublicHostname,
            node.PublicPort,
            node.Revision);

    private static bool IsValidPublicHostname(string value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= 253
           && !value.Any(char.IsWhiteSpace)
           && (IPAddress.TryParse(value, out _)
               || Uri.CheckHostName(value) == UriHostNameType.Dns);

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
        int Revision,
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
                inbound.Revision,
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
            inbound.Revision = Revision;
            inbound.UpdatedAt = UpdatedAt;
        }
    }

    private bool IsSandboxMode() => string.Equals(_configuration?["Vpn:X3Ui:Mode"], "Sandbox", StringComparison.OrdinalIgnoreCase);

    private static bool IsSqlite(DbContext dbContext)
        => string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);

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
        => new(x.Id, x.Name, x.BaseUrl, x.Region, x.Status.ToString(), x.HealthStatus.ToString(), x.Login, x.SslVerificationMode.ToString(), x.ApiVariant.ToString(), x.AuthenticationMode.ToString(), x.Capacity, x.UsedCapacity, x.AutoCreateInbound, x.DefaultInboundTemplateJson, x.LastHealthCheckAt, x.LastSyncAt, x.Version, x.LastError, x.Revision, x.CreatedAt, x.UpdatedAt);

    private static VpnInboundDto MapInbound(VpnInbound x)
        => new(x.Id, x.VpnPanelId, x.ExternalInboundId, x.Name, x.Protocol, x.Port, x.Listen, x.SettingsJson, x.StreamSettingsJson, x.SniffingJson, x.IsDefault, x.IsActive, x.Capacity, x.UsedCapacity, x.Revision);

    private static VpnClientDto MapClient(VpnClient x)
        => new(x.Id, x.UserId, x.SubscriptionId, x.VpnPanelId, x.VpnInboundId, x.ExternalClientId, x.Email, x.Uuid, x.Flow, x.LimitIp, x.TotalGb, x.ExpiryTime, x.Enable, x.ConfigUri, x.QrCodePayload, x.SyncStatus, x.LastSyncedAt, x.Revision);

    private static PanelHealthCheckDto MapHealth(PanelHealthCheck x)
        => new(x.Id, x.VpnPanelId, x.Status.ToString(), x.LatencyMs, x.Version, x.ErrorMessage, x.CheckedAt);

    private static PanelSyncRunDto MapSyncRun(PanelSyncRun x)
        => new(x.Id, x.VpnPanelId, x.Status.ToString(), x.StartedAt, x.FinishedAt, x.SummaryJson, x.ErrorMessage);

    private static PanelSyncEventDto MapSyncEvent(PanelSyncEvent x)
        => new(x.Id, x.PanelSyncRunId, x.EventType, x.EntityType, x.EntityId, x.ExternalId, x.Message, x.PayloadJson);
}
