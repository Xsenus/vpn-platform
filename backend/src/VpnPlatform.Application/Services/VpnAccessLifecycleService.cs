using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    private readonly VpnNodeCapacityService _vpnNodeCapacityService;
    private readonly IClock _clock;

    public VpnAccessLifecycleService(IApplicationDbContext db, IVpnProviderFactory vpnProviderFactory, IClock clock)
    {
        _db = db;
        _vpnProviderFactory = vpnProviderFactory;
        _vpnNodeCapacityService = new VpnNodeCapacityService(db);
        _clock = clock;
    }

    public async Task<Result<string>> CancelSubscriptionAsync(
        Subscription subscription,
        string? reason,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        if (!StatusStateMachine.CanTransition(subscription.Status, SubscriptionStatus.Cancelled))
        {
            return Result<string>.Failure($"Subscription status transition {subscription.Status} -> {SubscriptionStatus.Cancelled} is not allowed.");
        }

        var access = subscription.CurrentAccess;
        if (access is not null && !StatusStateMachine.CanTransition(access.Status, AccessCredentialStatus.Revoked))
        {
            return Result<string>.Failure($"VPN access status transition {access.Status} -> {AccessCredentialStatus.Revoked} is not allowed.");
        }

        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "manual_subscription_cancel" : reason.Trim();
        var subscriptionBefore = new
        {
            subscription.Status,
            subscription.CurrentAccessId,
            subscription.CurrentServerId,
            subscription.CancelledAt,
            subscription.BlockReason
        };
        var accessBefore = access is null ? null : Snapshot(access);
        IDbContextTransaction? transaction = null;
        var providerDeletionAttempted = false;

        try
        {
            if (_db is DbContext dbContext && dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            var nodeCapacityReleased = await ReleaseNodeCapacityAsync(subscription.CurrentServerId, cancellationToken);

            if (access is not null)
            {
                StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Revoked, now);
                access.DisabledAt = now;
                access.Revision += 1;
                AddHistory(access, "AccessRevokedOnSubscriptionCancel", accessBefore!, new
                {
                    access.Status,
                    access.DisabledAt,
                    reason = normalizedReason
                });
                AddAudit("access.revoke", access, accessBefore!, new
                {
                    access.Status,
                    access.DisabledAt,
                    reason = normalizedReason
                }, actorUserId);
            }

            StatusStateMachine.SetSubscriptionStatus(subscription, SubscriptionStatus.Cancelled, now);
            ResetSubscriptionLifecycleState(subscription);
            subscription.CancelledAt ??= now;
            subscription.BlockReason = normalizedReason;
            subscription.CurrentAccessId = null;
            subscription.CurrentAccess = null;
            subscription.CurrentServerId = null;
            subscription.CurrentServer = null;
            AddSubscriptionAudit("subscription.cancel", subscription, subscriptionBefore, new
            {
                subscription.Status,
                subscription.CancelledAt,
                subscription.CurrentAccessId,
                subscription.CurrentServerId,
                nodeCapacityReleased,
                reason = normalizedReason
            }, actorUserId);

            if (access is not null)
            {
                var provider = _vpnProviderFactory.Get(access.ProviderType);
                providerDeletionAttempted = true;
                await provider.DeleteAccessAsync(access.ProviderAccessId, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return Result<string>.Success("Subscription cancelled and VPN access revoked.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RollbackAndDisposeAsync(transaction);
            transaction = null;
            await MarkCancellationUncertainAsync(
                subscription.Id,
                access?.Id,
                accessBefore,
                normalizedReason,
                "VPN access revocation was cancelled while provider state may be unknown.",
                "access.revoke.cancelled",
                actorUserId,
                providerDeletionAttempted);
            throw;
        }
        catch (Exception ex)
        {
            await RollbackAndDisposeAsync(transaction);
            transaction = null;
            var safeError = SafeError(ex.Message);
            await MarkCancellationUncertainAsync(
                subscription.Id,
                access?.Id,
                accessBefore,
                normalizedReason,
                safeError,
                "access.revoke.failed",
                actorUserId,
                providerDeletionAttempted);
            return Result<string>.Failure($"VPN access revocation failed: {safeError}", isRetryable: true);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkSyncRequired(access, now);
            AddHistory(access, $"{eventType ?? "AccessDisable"}Cancelled", before, new { access.Status, reason, outcome = "provider_state_unknown" });
            AddAudit("access.disable.cancelled", access, before, new { access.Status, reason, outcome = "provider_state_unknown" }, actorUserId);
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkSyncRequired(access, now);
            AddHistory(access, "AccessEnableCancelled", before, new { access.Status, reason, outcome = "provider_state_unknown" });
            AddAudit("access.enable.cancelled", access, before, new { access.Status, reason, outcome = "provider_state_unknown" }, actorUserId);
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AddHistory(access, "AccessSyncCancelled", before, new { access.Status, reason, outcome = "cancelled" });
            AddAudit("access.sync.cancelled", access, before, new { access.Status, reason, outcome = "cancelled" }, actorUserId);
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
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
        if (access.Status == AccessCredentialStatus.Revoked)
        {
            return Result<AdminAccessActionResult>.Failure("Revoked VPN access traffic cannot be reset.");
        }

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkSyncRequired(access, now);
            AddHistory(access, "AccessTrafficResetCancelled", before, new { access.Status, reason, outcome = "provider_state_unknown" });
            AddAudit("access.reset_traffic.cancelled", access, before, new { access.Status, reason, outcome = "provider_state_unknown" }, actorUserId);
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            MarkSyncRequired(access, now);
            AddHistory(access, "AccessTrafficResetFailed", before, new { access.Status, error = safeError, reason, outcome = "provider_state_unknown" });
            AddAudit("access.reset_traffic.failed", access, before, new { access.Status, error = safeError, reason, outcome = "provider_state_unknown" }, actorUserId);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<AdminAccessActionResult>.Failure($"VPN access traffic reset failed: {safeError}");
        }
    }

    private static AdminAccessActionResult ToResult(AccessCredential access, string? message = null, long? usedTrafficBytes = null)
        => new(access.Id, access.Status.ToString(), access.DisabledAt, access.LastSyncedAt, access.Revision, usedTrafficBytes, message);

    private static object Snapshot(AccessCredential access)
        => new { access.Status, access.ProviderAccessId, access.DisabledAt, access.LastSyncedAt, access.Revision };

    private static void MarkSyncRequired(AccessCredential access, DateTimeOffset now)
    {
        if (StatusStateMachine.CanTransition(access.Status, AccessCredentialStatus.SyncRequired))
        {
            StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.SyncRequired, now);
        }
    }

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

    private async Task<bool> ReleaseNodeCapacityAsync(Guid? nodeId, CancellationToken cancellationToken)
    {
        if (!nodeId.HasValue)
        {
            return false;
        }

        if (_db is DbContext dbContext && !dbContext.Database.IsRelational())
        {
            var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == nodeId.Value, cancellationToken);
            if (node is null || node.UsedCapacity <= 0)
            {
                return false;
            }

            node.UsedCapacity -= 1;
            return true;
        }

        return await _vpnNodeCapacityService.ReleaseAsync(nodeId.Value, cancellationToken);
    }

    private async Task MarkCancellationUncertainAsync(
        Guid subscriptionId,
        Guid? accessId,
        object? accessBefore,
        string reason,
        string error,
        string auditAction,
        Guid? actorUserId,
        bool providerStateUnknown)
    {
        if (_db is DbContext dbContext)
        {
            dbContext.ChangeTracker.Clear();
        }

        var now = _clock.UtcNow;
        var safeError = SafeError(error);
        if (providerStateUnknown && accessId.HasValue)
        {
            var persistedAccess = await _db.AccessCredentials.FirstOrDefaultAsync(x => x.Id == accessId.Value, CancellationToken.None);
            if (persistedAccess is not null && StatusStateMachine.CanTransition(persistedAccess.Status, AccessCredentialStatus.SyncRequired))
            {
                StatusStateMachine.SetAccessStatus(persistedAccess, AccessCredentialStatus.SyncRequired, now);
                persistedAccess.Revision += 1;
                AddHistory(persistedAccess, "AccessRevokeUncertainOnSubscriptionCancel", accessBefore ?? Snapshot(persistedAccess), new
                {
                    persistedAccess.Status,
                    reason,
                    error = safeError,
                    outcome = "provider_state_unknown"
                });
                AddAudit(auditAction, persistedAccess, accessBefore ?? Snapshot(persistedAccess), new
                {
                    persistedAccess.Status,
                    reason,
                    error = safeError,
                    outcome = "provider_state_unknown"
                }, actorUserId);
            }
        }

        var outcome = providerStateUnknown ? "provider_state_unknown" : "rolled_back_before_provider_mutation";
        var persistedSubscription = await _db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == subscriptionId, CancellationToken.None);
        if (persistedSubscription is not null && persistedSubscription.Status != SubscriptionStatus.Cancelled)
        {
            AddSubscriptionAudit("subscription.cancel.failed", persistedSubscription, new { persistedSubscription.Status }, new
            {
                persistedSubscription.Status,
                reason,
                error = safeError,
                outcome
            }, actorUserId);
        }

        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private void AddSubscriptionAudit(string action, Subscription subscription, object oldValue, object newValue, Guid? actorUserId)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = actorUserId.HasValue ? "admin" : "system",
            ActorId = actorUserId?.ToString() ?? "system",
            Action = action,
            EntityType = "Subscription",
            EntityId = subscription.Id.ToString(),
            BeforeJson = SensitiveDataRedactor.Redact(JsonSerializer.Serialize(oldValue)),
            AfterJson = SensitiveDataRedactor.Redact(JsonSerializer.Serialize(newValue)),
            CreatedAt = _clock.UtcNow
        });
    }

    private static void ResetSubscriptionLifecycleState(Subscription subscription)
    {
        subscription.LifecycleAttemptCount = 0;
        subscription.LifecycleProcessingStartedAt = null;
        subscription.LifecycleLeaseExpiresAt = null;
        subscription.LifecycleNextAttemptAt = null;
        subscription.LifecycleLastError = null;
    }

    private static async Task RollbackAndDisposeAsync(IDbContextTransaction? transaction)
    {
        if (transaction is null)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch
        {
            // The failure marker below records that provider and database state require reconciliation.
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    private static string SafeError(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "provider error"
            : SensitiveDataRedactor.Redact(value, maxLength: 500);
}
