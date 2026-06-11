using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class VpnAccessLifecycleService
{
    private readonly IApplicationDbContext _db;
    private readonly IVpnProviderFactory _vpnProviderFactory;
    private readonly IClock _clock;

    public VpnAccessLifecycleService(IApplicationDbContext db, IVpnProviderFactory vpnProviderFactory, IClock clock)
    {
        _db = db;
        _vpnProviderFactory = vpnProviderFactory;
        _clock = clock;
    }

    public async Task<Result<AdminAccessActionResult>> DisableAccessAsync(Guid accessId, string? eventType, string? reason, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var access = await _db.AccessCredentials.Include(x => x.Subscription).FirstOrDefaultAsync(x => x.Id == accessId, cancellationToken);
        return access is null
            ? Result<AdminAccessActionResult>.Failure("VPN access not found.")
            : await DisableAccessAsync(access, eventType, reason, actorUserId, cancellationToken);
    }

    public async Task<Result<AdminAccessActionResult>> DisableAccessAsync(AccessCredential access, string? eventType, string? reason, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var before = Snapshot(access);
        if (access.Status == AccessCredentialStatus.Disabled && access.DisabledAt.HasValue)
        {
            AddHistory(access, eventType ?? "AccessDisableSkipped", before, new { access.Status, access.DisabledAt, reason = reason ?? "already_disabled" });
            AddAudit("access.disable.skipped", access, before, new { access.Status, access.DisabledAt, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Success(ToResult(access, "Access is already disabled."));
        }

        if (!StatusStateMachine.CanTransition(access.Status, AccessCredentialStatus.Disabled))
        {
            return Result<AdminAccessActionResult>.Failure($"VPN access status transition {access.Status} -> {AccessCredentialStatus.Disabled} is not allowed.");
        }

        try
        {
            var provider = _vpnProviderFactory.Get(access.ProviderType);
            await provider.DisableAccessAsync(access.ProviderAccessId, cancellationToken);
            StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Disabled, now);
            access.DisabledAt = now;
            access.Revision += 1;
            AddHistory(access, eventType ?? "AccessDisabled", before, new { access.Status, access.DisabledAt, reason });
            AddAudit("access.disable", access, before, new { access.Status, access.DisabledAt, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Success(ToResult(access, "Access disabled."));
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Error, now);
            AddHistory(access, $"{eventType ?? "AccessDisable"}Failed", before, new { access.Status, error = safeError, reason });
            AddAudit("access.disable.failed", access, before, new { access.Status, error = safeError, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Failure($"VPN access disable failed: {safeError}");
        }
    }

    public async Task<Result<AdminAccessActionResult>> EnableAccessAsync(Guid accessId, string? reason, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var access = await _db.AccessCredentials.Include(x => x.Subscription).FirstOrDefaultAsync(x => x.Id == accessId, cancellationToken);
        if (access is null) return Result<AdminAccessActionResult>.Failure("VPN access not found.");

        var now = _clock.UtcNow;
        var before = Snapshot(access);
        if (!StatusStateMachine.CanTransition(access.Status, AccessCredentialStatus.Active))
        {
            return Result<AdminAccessActionResult>.Failure($"VPN access status transition {access.Status} -> {AccessCredentialStatus.Active} is not allowed.");
        }

        try
        {
            var provider = _vpnProviderFactory.Get(access.ProviderType);
            await provider.EnableAccessAsync(access.ProviderAccessId, cancellationToken);
            StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Active, now);
            access.DisabledAt = null;
            access.LastSyncedAt = now;
            access.Revision += 1;
            AddHistory(access, "AccessEnabled", before, new { access.Status, access.DisabledAt, reason });
            AddAudit("access.enable", access, before, new { access.Status, access.DisabledAt, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Success(ToResult(access, "Access enabled."));
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Error, now);
            AddHistory(access, "AccessEnableFailed", before, new { access.Status, error = safeError, reason });
            AddAudit("access.enable.failed", access, before, new { access.Status, error = safeError, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Failure($"VPN access enable failed: {safeError}");
        }
    }

    public async Task<Result<AdminAccessActionResult>> SyncAccessAsync(Guid accessId, string? reason, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var access = await _db.AccessCredentials.Include(x => x.Subscription).FirstOrDefaultAsync(x => x.Id == accessId, cancellationToken);
        if (access is null) return Result<AdminAccessActionResult>.Failure("VPN access not found.");

        var now = _clock.UtcNow;
        var before = Snapshot(access);
        if (access.Status == AccessCredentialStatus.Revoked)
        {
            return Result<AdminAccessActionResult>.Failure("Revoked VPN access cannot be synced.");
        }

        try
        {
            var provider = _vpnProviderFactory.Get(access.ProviderType);
            var usage = await provider.SyncAccessAsync(access.ProviderAccessId, cancellationToken);
            access.LastSyncedAt = usage.SyncedAt;
            access.UpdatedAt = now;
            AddHistory(access, "AccessSynced", before, new { access.Status, access.LastSyncedAt, usage.UsedTrafficBytes, usage.ActiveConnections, reason });
            AddAudit("access.sync", access, before, new { access.Status, access.LastSyncedAt, usage.UsedTrafficBytes, usage.ActiveConnections, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Success(ToResult(access, "Access synced.", usage.UsedTrafficBytes));
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Error, now);
            AddHistory(access, "AccessSyncFailed", before, new { access.Status, error = safeError, reason });
            AddAudit("access.sync.failed", access, before, new { access.Status, error = safeError, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Failure($"VPN access sync failed: {safeError}");
        }
    }

    public async Task<Result<AdminAccessActionResult>> ResetTrafficAsync(Guid accessId, string? reason, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var access = await _db.AccessCredentials.Include(x => x.Subscription).FirstOrDefaultAsync(x => x.Id == accessId, cancellationToken);
        if (access is null) return Result<AdminAccessActionResult>.Failure("VPN access not found.");

        var now = _clock.UtcNow;
        var before = Snapshot(access);
        try
        {
            var provider = _vpnProviderFactory.Get(access.ProviderType);
            await provider.ResetTrafficAsync(access.ProviderAccessId, cancellationToken);
            access.LastSyncedAt = now;
            access.UpdatedAt = now;
            AddHistory(access, "AccessTrafficReset", before, new { access.Status, access.LastSyncedAt, reason });
            AddAudit("access.reset_traffic", access, before, new { access.Status, access.LastSyncedAt, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Success(ToResult(access, "Access traffic reset."));
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            AddHistory(access, "AccessTrafficResetFailed", before, new { access.Status, error = safeError, reason });
            AddAudit("access.reset_traffic.failed", access, before, new { access.Status, error = safeError, reason }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Failure($"VPN access traffic reset failed: {safeError}");
        }
    }

    private static AdminAccessActionResult ToResult(AccessCredential access, string? message = null, long? usedTrafficBytes = null)
        => new(access.Id, access.Status.ToString(), access.DisabledAt, access.LastSyncedAt, access.Revision, usedTrafficBytes, message);

    private static object Snapshot(AccessCredential access)
        => new { access.Status, access.ProviderAccessId, access.DisabledAt, access.LastSyncedAt, access.Revision };

    private void AddHistory(AccessCredential access, string eventType, object oldValue, object newValue)
    {
        _db.AccessCredentialHistories.Add(new AccessCredentialHistory
        {
            AccessCredentialId = access.Id,
            SubscriptionId = access.SubscriptionId,
            EventType = eventType,
            OldValueJson = JsonSerializer.Serialize(oldValue),
            NewValueJson = JsonSerializer.Serialize(newValue),
            CreatedAt = _clock.UtcNow
        });
    }

    private void AddAudit(string action, AccessCredential access, object oldValue, object newValue, Guid? actorUserId)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = actorUserId.HasValue ? "admin" : "system",
            ActorId = actorUserId?.ToString() ?? "system",
            Action = action,
            EntityType = "AccessCredential",
            EntityId = access.Id.ToString(),
            BeforeJson = SensitiveDataRedactor.Redact(JsonSerializer.Serialize(oldValue)),
            AfterJson = SensitiveDataRedactor.Redact(JsonSerializer.Serialize(newValue)),
            CreatedAt = _clock.UtcNow
        });
    }

    private static string SafeError(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "provider error"
            : SensitiveDataRedactor.Redact(value, maxLength: 500);
}
