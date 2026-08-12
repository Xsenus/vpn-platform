using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class SubscriptionService
{
    private static readonly TimeSpan LifecycleLeaseDuration = TimeSpan.FromMinutes(5);

    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly NodeAllocationService _nodeAllocationService;
    private readonly VpnNodeCapacityService _vpnNodeCapacityService;
    private readonly IVpnProviderFactory _vpnProviderFactory;
    private readonly VpnAccessLifecycleService? _vpnAccessLifecycleService;

    public SubscriptionService(
        IApplicationDbContext db,
        IClock clock,
        NodeAllocationService nodeAllocationService,
        IVpnProviderFactory vpnProviderFactory,
        VpnAccessLifecycleService? vpnAccessLifecycleService = null)
    {
        _db = db;
        _clock = clock;
        _nodeAllocationService = nodeAllocationService;
        _vpnNodeCapacityService = new VpnNodeCapacityService(db);
        _vpnProviderFactory = vpnProviderFactory;
        _vpnAccessLifecycleService = vpnAccessLifecycleService;
    }

    public async Task<Result<ActivationResult>> ActivateOrRenewFromOrderAsync(Order order, PaymentAttempt payment, CancellationToken cancellationToken = default)
    {
        var tariff = await _db.Tariffs.FirstAsync(x => x.Id == order.TariffId, cancellationToken);
        var promoFreeDays = OrderService.GetPromoFreeDays(order);
        if (!promoFreeDays.HasValue && order.PromoCodeId.HasValue)
        {
            var promo = await _db.PromoCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.PromoCodeId.Value, cancellationToken);
            if (promo is null || promo.FreeDays < 0)
            {
                return Result<ActivationResult>.Failure("Order promo code duration is unavailable or invalid.");
            }

            promoFreeDays = promo.FreeDays;
        }

        if (promoFreeDays is < 0)
        {
            return Result<ActivationResult>.Failure("Order promo code duration is invalid.");
        }

        var durationDays = (long)tariff.DurationDays + (promoFreeDays ?? 0);
        if (durationDays <= 0 || durationDays > int.MaxValue)
        {
            return Result<ActivationResult>.Failure("Subscription duration is invalid.");
        }

        var scenarioResult = await ResolveScenarioAsync(tariff, cancellationToken);
        if (!scenarioResult.IsSuccess)
        {
            return Result<ActivationResult>.Failure(scenarioResult.Error ?? "Provisioning scenario is not configured.");
        }

        var scenario = scenarioResult.Value;
        var scenarioKey = scenario?.Key ?? NormalizeScenarioKey(tariff.ProvisioningScenario);
        var maxDevices = ResolveMaxDevices(tariff, scenario);
        var trafficLimit = tariff.TrafficLimit ?? scenario?.TrafficLimit;
        var protocol = string.IsNullOrWhiteSpace(scenario?.VpnProtocol) ? "vless" : scenario.VpnProtocol.Trim().ToLowerInvariant();
        if (!VpnProtocolPolicy.IsSupported(protocol))
        {
            return Result<ActivationResult>.Failure("Provisioning scenario VPN protocol is unsupported.");
        }
        var inboundSelectionRule = string.IsNullOrWhiteSpace(scenario?.InboundSelectionRule) ? "default" : scenario.InboundSelectionRule.Trim().ToLowerInvariant();
        var generateQrCode = scenario?.GenerateQrCode ?? true;
        var useSandboxProvisioning = payment.ProviderMode == PaymentProviderMode.Sandbox;

        var renewalSubscriptionId = order.Type == OrderType.Renewal
            ? OrderService.GetRenewalSubscriptionId(order)
            : null;

        var existingQuery = _db.Subscriptions
            .Include(x => x.CurrentAccess)
            .Where(x => x.UserId == order.UserId && x.TariffId == order.TariffId && x.Status != SubscriptionStatus.Cancelled && x.Status != SubscriptionStatus.Blocked);

        var existingForPayment = await existingQuery
            .FirstOrDefaultAsync(x => x.LastPaymentId == payment.Id, cancellationToken);
        var existing = existingForPayment;
        if (existing is null)
        {
            existing = renewalSubscriptionId.HasValue
                ? await existingQuery.FirstOrDefaultAsync(x => x.Id == renewalSubscriptionId.Value, cancellationToken)
                : (await existingQuery.ToListAsync(cancellationToken))
                    .OrderByDescending(x => x.EndAt)
                    .FirstOrDefault();
        }

        if (order.Type == OrderType.Renewal && renewalSubscriptionId.HasValue && existing is null)
        {
            return Result<ActivationResult>.Failure("Subscription not found or unavailable for renewal.");
        }

        var now = _clock.UtcNow;
        Subscription subscription;
        if (existing is null || (order.Type == OrderType.NewSubscription && existingForPayment is null))
        {
            if (!TryAddDays(now, durationDays, out var endAt))
            {
                return Result<ActivationResult>.Failure("Subscription duration exceeds the supported date range.");
            }

            subscription = new Subscription
            {
                UserId = order.UserId,
                TariffId = order.TariffId,
                Status = SubscriptionStatus.PendingActivation,
                StartAt = now,
                EndAt = endAt,
                GracePeriodEndAt = BusinessRules.GetGracePeriodEnd(endAt),
                SourceChannel = order.Channel,
                LastPaymentId = payment.Id,
                RenewalCount = 0
            };
            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            subscription = existing;
            if (existingForPayment is null)
            {
                var baseDate = BusinessRules.GetRenewalBaseDate(now, subscription.EndAt);
                if (!TryAddDays(baseDate, durationDays, out var endAt))
                {
                    return Result<ActivationResult>.Failure("Subscription duration exceeds the supported date range.");
                }

                subscription.EndAt = endAt;
                subscription.GracePeriodEndAt = BusinessRules.GetGracePeriodEnd(subscription.EndAt);
                subscription.LastPaymentId = payment.Id;
                subscription.RenewalCount += 1;
                StatusStateMachine.SetSubscriptionStatus(subscription, SubscriptionStatus.Active, now);
                subscription.BlockReason = null;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        var previousServerId = subscription.CurrentServerId;
        Guid? reservedNodeId = null;
        string? provisioningFailureOverride = null;

        try
        {
            var access = subscription.CurrentAccess ?? await _db.AccessCredentials.FirstOrDefaultAsync(x => x.SubscriptionId == subscription.Id, cancellationToken);
            var node = subscription.CurrentServerId.HasValue
                ? await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == subscription.CurrentServerId.Value, cancellationToken)
                : null;

            if (access is not null && subscription.CurrentServerId.HasValue)
            {
                if (node is null || (!useSandboxProvisioning && NodeAllocationService.IsSandboxNode(node)))
                {
                    throw new InvalidOperationException("Assigned VPN node is unavailable. Explicit access migration is required.");
                }
            }
            else if (node is null
                || node.Status is NodeStatus.Maintenance or NodeStatus.Draining or NodeStatus.Disabled or NodeStatus.Archived
                || !node.IsAvailableForNewUsers
                || (!useSandboxProvisioning && NodeAllocationService.IsSandboxNode(node)))
            {
                node = useSandboxProvisioning
                    ? await _nodeAllocationService.SelectOrCreateSandboxNodeAsync(protocol, cancellationToken)
                    : await _nodeAllocationService.SelectNodeAsync(tariff, scenario, cancellationToken);
            }

            if ((previousServerId != node.Id || existing is null)
                && !await _vpnNodeCapacityService.TryReserveAsync(node.Id, cancellationToken))
            {
                throw new InvalidOperationException(NodeAllocationService.NoAvailableNodeError);
            }

            if (previousServerId != node.Id || existing is null)
            {
                reservedNodeId = node.Id;
            }

            var provider = _vpnProviderFactory.Get(string.IsNullOrWhiteSpace(access?.ProviderType) ? "x3ui" : access.ProviderType);
            var request = new VpnProvisionRequest(subscription.Id, subscription.UserId, tariff.Id, node.Id, subscription.EndAt, maxDevices, protocol, trafficLimit, generateQrCode, scenarioKey, inboundSelectionRule, useSandboxProvisioning);

            if (access is null)
            {
                VpnProvisionResult? provisionResult = null;
                AccessCredential? createdAccess = null;
                try
                {
                    provisionResult = await provider.CreateAccessAsync(request, cancellationToken);
                    createdAccess = new AccessCredential
                    {
                        SubscriptionId = subscription.Id,
                        ProviderType = provider.Name,
                        ProviderAccessId = provisionResult.ProviderAccessId,
                        ServerId = node.Id,
                        AccessUri = provisionResult.AccessUri,
                        QrCodePath = provisionResult.QrCodePath,
                        ConfigPath = provisionResult.ConfigPath,
                        Status = AccessCredentialStatus.Active,
                        IssuedAt = now,
                        LastSyncedAt = now,
                        Revision = 1
                    };

                    _db.AccessCredentials.Add(createdAccess);
                    cancellationToken.ThrowIfCancellationRequested();
                    await _db.SaveChangesAsync(cancellationToken);
                    access = createdAccess;
                }
                catch (Exception ex) when (provisionResult is not null)
                {
                    var cleanupError = await TryDeleteCreatedAccessAsync(provider, provisionResult.ProviderAccessId);
                    if (cleanupError is null)
                    {
                        if (createdAccess is not null)
                        {
                            _db.AccessCredentials.Remove(createdAccess);
                        }
                    }
                    else if (createdAccess is not null)
                    {
                        StatusStateMachine.SetAccessStatus(createdAccess, AccessCredentialStatus.SyncRequired, now);
                        _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                        {
                            AccessCredentialId = createdAccess.Id,
                            SubscriptionId = subscription.Id,
                            EventType = "AccessCreateCleanupFailed",
                            OldValueJson = "{}",
                            NewValueJson = JsonSerializer.Serialize(new
                            {
                                createdAccess.ProviderAccessId,
                                createdAccess.Status,
                                provisioningError = SafeError(ex.Message),
                                cleanupError
                            })
                        });
                        access = createdAccess;
                        provisioningFailureOverride = $"{SafeError(ex.Message)} Remote VPN access cleanup failed; manual reconciliation is required.";
                    }

                    throw;
                }

                _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                {
                    AccessCredentialId = access.Id,
                    SubscriptionId = subscription.Id,
                    EventType = "AccessCreated",
                    OldValueJson = "{}",
                    NewValueJson = JsonSerializer.Serialize(new { access.ProviderAccessId, access.AccessUri, access.Status, subscription.EndAt, scenarioKey, maxDevices, protocol, trafficLimit, scenarioCabinetText = scenario?.CabinetText ?? string.Empty, scenarioTelegramText = scenario?.TelegramText ?? string.Empty })
                });
            }
            else
            {
                var before = JsonSerializer.Serialize(new { access.Status, access.ProviderAccessId, access.AccessUri, access.DisabledAt, access.Revision });
                var wasDisabled = access.Status == AccessCredentialStatus.Disabled || access.DisabledAt.HasValue;
                var provisionResult = await provider.UpdateAccessAsync(request, cancellationToken);

                access.ProviderType = provider.Name;
                access.ProviderAccessId = provisionResult.ProviderAccessId;
                access.ServerId = node.Id;
                access.AccessUri = provisionResult.AccessUri;
                access.QrCodePath = provisionResult.QrCodePath;
                access.ConfigPath = provisionResult.ConfigPath;
                StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Active, now);
                access.DisabledAt = null;
                access.LastSyncedAt = now;
                access.Revision += 1;

                _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                {
                    AccessCredentialId = access.Id,
                    SubscriptionId = subscription.Id,
                    EventType = wasDisabled ? "AccessRenewedAndEnabled" : "AccessUpdated",
                    OldValueJson = before,
                    NewValueJson = JsonSerializer.Serialize(new { access.ProviderAccessId, access.AccessUri, access.Status, subscription.EndAt, access.Revision, scenarioKey, maxDevices, protocol, trafficLimit, scenarioCabinetText = scenario?.CabinetText ?? string.Empty, scenarioTelegramText = scenario?.TelegramText ?? string.Empty })
                });
            }

            StatusStateMachine.SetSubscriptionStatus(subscription, SubscriptionStatus.Active, now);
            ResetLifecycleState(subscription);
            subscription.CurrentServerId = node.Id;
            subscription.CurrentAccessId = access.Id;
            StatusStateMachine.SetOrderStatus(order, OrderStatus.Completed, now);
            order.PaidAt = payment.PaidAt ?? now;

            _db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "NotificationRequested",
                CorrelationId = payment.ProviderPaymentId,
                PayloadJson = $$"""
                {
                  "userId": "{{order.UserId}}",
                  "templateKey": "subscription_activated",
                  "subscriptionId": "{{subscription.Id}}",
                  "accessId": "{{access.Id}}",
                  "scenarioKey": "{{scenarioKey}}",
                  "scenarioCabinetText": {{JsonSerializer.Serialize(scenario?.CabinetText ?? string.Empty)}},
                  "scenarioTelegramText": {{JsonSerializer.Serialize(scenario?.TelegramText ?? string.Empty)}}
                }
                """
            });

            if (order.Type == OrderType.NewSubscription)
            {
                _db.OutboxMessages.Add(new OutboxMessage
                {
                    Type = "ReferralRewardRequested",
                    CorrelationId = order.Id.ToString("N"),
                    PayloadJson = JsonSerializer.Serialize(new { orderId = order.Id })
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            reservedNodeId = null;
            return Result<ActivationResult>.Success(new ActivationResult(subscription.Id, access.Id, scenarioKey, scenario?.CabinetText ?? string.Empty, scenario?.TelegramText ?? string.Empty));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var capacityCleanupError = await TryReleaseNodeCapacityAsync(reservedNodeId);
            var safeError = AppendCapacityCleanupError(provisioningFailureOverride ?? "VPN access provisioning was cancelled.", capacityCleanupError);
            await RecordProvisioningFailureAsync(subscription, order, tariff, scenarioKey, safeError, "vpn_access.provisioning_cancelled", now);
            throw;
        }
        catch (Exception ex)
        {
            var capacityCleanupError = await TryReleaseNodeCapacityAsync(reservedNodeId);
            var safeError = AppendCapacityCleanupError(provisioningFailureOverride ?? SafeError(ex.Message), capacityCleanupError);
            await RecordProvisioningFailureAsync(subscription, order, tariff, scenarioKey, safeError, "vpn_access.provisioning_failed", now);
            return Result<ActivationResult>.Failure(safeError, isRetryable: true);
        }
    }

    public async Task<int> ProcessLifecycleAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var activeCandidates = await _db.Subscriptions.AsNoTracking()
            .Where(x => x.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);
        var expirationCandidates = await _db.Subscriptions.AsNoTracking()
            .Where(x => x.Status == SubscriptionStatus.GracePeriod)
            .ToListAsync(cancellationToken);
        var processed = 0;

        foreach (var candidate in activeCandidates.Where(x => x.EndAt <= now).OrderBy(x => x.EndAt).ThenBy(x => x.Id))
        {
            await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(candidate.Id, cancellationToken);
            var item = await _db.Subscriptions
                .Include(x => x.Tariff)
                .FirstOrDefaultAsync(x => x.Id == candidate.Id && x.Status == SubscriptionStatus.Active, cancellationToken);
            if (item is null || item.EndAt > now)
            {
                continue;
            }

            StatusStateMachine.SetSubscriptionStatus(item, SubscriptionStatus.GracePeriod, now);
            ResetLifecycleState(item);
            await QueueLifecycleNotificationAsync(item, "subscription_expiring", "subscription_expiring", now, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            processed++;
        }

        foreach (var candidate in expirationCandidates
                     .Where(x => x.GracePeriodEndAt.HasValue
                         && x.GracePeriodEndAt <= now
                         && (!x.LifecycleNextAttemptAt.HasValue || x.LifecycleNextAttemptAt <= now)
                         && (!x.LifecycleLeaseExpiresAt.HasValue || x.LifecycleLeaseExpiresAt <= now))
                     .OrderBy(x => x.GracePeriodEndAt)
                     .ThenBy(x => x.Id))
        {
            await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(candidate.Id, cancellationToken);
            if (!await TryClaimLifecycleAsync(candidate.Id, now, cancellationToken))
            {
                continue;
            }

            var item = await _db.Subscriptions
                .Include(x => x.CurrentAccess)
                .Include(x => x.Tariff)
                .FirstAsync(x => x.Id == candidate.Id, cancellationToken);

            try
            {
                var disableResult = await DisableLifecycleAccessAsync(item, now, cancellationToken);
                if (!disableResult.IsSuccess)
                {
                    await ScheduleLifecycleRetryAsync(item, disableResult.Error ?? "VPN access disable failed.", now, cancellationToken);
                    continue;
                }

                StatusStateMachine.SetSubscriptionStatus(item, SubscriptionStatus.Expired, now);
                ClearLifecycleClaim(item);
                item.LifecycleNextAttemptAt = null;
                item.LifecycleLastError = null;
                await QueueLifecycleNotificationAsync(item, "subscription_expired", "subscription_expired", now, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await ScheduleLifecycleRetryAsync(item, "Subscription expiry was cancelled while provider state may be unknown.", now, CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                await ScheduleLifecycleRetryAsync(item, SafeError(ex.Message), now, CancellationToken.None);
            }
        }

        return processed;
    }

    private async Task<bool> TryClaimLifecycleAsync(Guid subscriptionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var candidate = await _db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == subscriptionId && x.Status == SubscriptionStatus.GracePeriod, cancellationToken);
        if (candidate is null
            || !candidate.GracePeriodEndAt.HasValue
            || candidate.GracePeriodEndAt > now
            || candidate.LifecycleNextAttemptAt > now
            || candidate.LifecycleLeaseExpiresAt > now)
        {
            return false;
        }

        var version = now > candidate.UpdatedAt ? now : candidate.UpdatedAt.AddTicks(1);
        if (IsInMemoryProvider())
        {
            var tracked = await _db.Subscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);
            if (tracked is null || tracked.Status != candidate.Status || tracked.UpdatedAt != candidate.UpdatedAt)
            {
                return false;
            }

            ApplyLifecycleClaim(tracked, now, version);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _db.Subscriptions
            .Where(x => x.Id == subscriptionId && x.Status == candidate.Status && x.UpdatedAt == candidate.UpdatedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LifecycleAttemptCount, x => x.LifecycleAttemptCount + 1)
                .SetProperty(x => x.LifecycleProcessingStartedAt, now)
                .SetProperty(x => x.LifecycleLeaseExpiresAt, now.Add(LifecycleLeaseDuration))
                .SetProperty(x => x.LifecycleLastError, (string?)null)
                .SetProperty(x => x.UpdatedAt, version), cancellationToken);
        return affected == 1;
    }

    private async Task<Result<string>> DisableLifecycleAccessAsync(Subscription subscription, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var access = subscription.CurrentAccess;
        if (access is null)
        {
            return Result<string>.Success("No current VPN access.");
        }

        if (_vpnAccessLifecycleService is not null)
        {
            var result = await _vpnAccessLifecycleService.DisableAccessAsync(access, "AccessDisabledOnExpiry", "subscription_expired", null, cancellationToken);
            return result.IsSuccess
                ? Result<string>.Success(result.Value?.Message ?? "VPN access disabled.")
                : Result<string>.Failure(result.Error ?? "VPN access disable failed.", isRetryable: true);
        }

        try
        {
            if (access.Status != AccessCredentialStatus.Disabled || !access.DisabledAt.HasValue)
            {
                var provider = _vpnProviderFactory.Get(access.ProviderType);
                await provider.DisableAccessAsync(access.ProviderAccessId, cancellationToken);
                StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Disabled, now);
                access.DisabledAt = now;
                access.Revision += 1;
                _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                {
                    AccessCredentialId = access.Id,
                    SubscriptionId = subscription.Id,
                    EventType = "AccessDisabledOnExpiry",
                    OldValueJson = "{}",
                    NewValueJson = JsonSerializer.Serialize(new { access.ProviderAccessId, access.Status, disabledAt = now })
                });
            }

            return Result<string>.Success("VPN access disabled.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (StatusStateMachine.CanTransition(access.Status, AccessCredentialStatus.SyncRequired))
            {
                StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.SyncRequired, now);
                access.Revision += 1;
            }
            throw;
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            if (StatusStateMachine.CanTransition(access.Status, AccessCredentialStatus.Error))
            {
                StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Error, now);
                access.Revision += 1;
            }
            _db.AccessCredentialHistories.Add(new AccessCredentialHistory
            {
                AccessCredentialId = access.Id,
                SubscriptionId = subscription.Id,
                EventType = "AccessDisableFailedOnExpiry",
                OldValueJson = "{}",
                NewValueJson = JsonSerializer.Serialize(new { access.ProviderAccessId, error = safeError })
            });
            return Result<string>.Failure(safeError, isRetryable: true);
        }
    }

    private async Task ScheduleLifecycleRetryAsync(Subscription subscription, string error, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var safeError = SensitiveDataRedactor.Redact(error, maxLength: 1000);
        ClearLifecycleClaim(subscription);
        subscription.LifecycleLastError = safeError;
        subscription.LifecycleNextAttemptAt = now.Add(LifecycleRetryDelay(subscription.LifecycleAttemptCount));
        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = "system",
            ActorId = "subscription-lifecycle-worker",
            Action = "subscription.lifecycle_retry_scheduled",
            EntityType = "Subscription",
            EntityId = subscription.Id.ToString(),
            BeforeJson = "{}",
            AfterJson = JsonSerializer.Serialize(new { attempt = subscription.LifecycleAttemptCount, nextAttemptAt = subscription.LifecycleNextAttemptAt, error = safeError }),
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool TryAddDays(DateTimeOffset baseDate, long durationDays, out DateTimeOffset result)
    {
        try
        {
            result = baseDate.AddDays(durationDays);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            result = default;
            return false;
        }
    }

    private static TimeSpan LifecycleRetryDelay(int attemptCount)
        => TimeSpan.FromMinutes(Math.Min(60, 5 * Math.Pow(2, Math.Clamp(attemptCount - 1, 0, 4))));

    private static void ApplyLifecycleClaim(Subscription subscription, DateTimeOffset now, DateTimeOffset version)
    {
        subscription.LifecycleAttemptCount++;
        subscription.LifecycleProcessingStartedAt = now;
        subscription.LifecycleLeaseExpiresAt = now.Add(LifecycleLeaseDuration);
        subscription.LifecycleLastError = null;
        subscription.UpdatedAt = version;
    }

    private static void ClearLifecycleClaim(Subscription subscription)
    {
        subscription.LifecycleProcessingStartedAt = null;
        subscription.LifecycleLeaseExpiresAt = null;
    }

    private static void ResetLifecycleState(Subscription subscription)
    {
        subscription.LifecycleAttemptCount = 0;
        ClearLifecycleClaim(subscription);
        subscription.LifecycleNextAttemptAt = null;
        subscription.LifecycleLastError = null;
    }

    private bool IsInMemoryProvider()
        => _db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);

    private async Task<string?> TryReleaseNodeCapacityAsync(Guid? nodeId)
    {
        if (!nodeId.HasValue)
        {
            return null;
        }

        try
        {
            return await _vpnNodeCapacityService.ReleaseAsync(nodeId.Value, CancellationToken.None)
                ? null
                : "reserved VPN node capacity could not be released";
        }
        catch (Exception ex)
        {
            return SafeError(ex.Message);
        }
    }

    private static string AppendCapacityCleanupError(string error, string? capacityCleanupError)
        => capacityCleanupError is null
            ? error
            : $"{error} VPN node capacity cleanup failed ({capacityCleanupError}); manual reconciliation is required.";

    private async Task QueueLifecycleNotificationAsync(Subscription subscription, string templateKey, string eventType, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var correlationId = $"{eventType}:{subscription.Id:N}";
        var outboxExists = await _db.OutboxMessages.AsNoTracking()
            .AnyAsync(x => x.Type == "NotificationRequested" && x.CorrelationId == correlationId, cancellationToken);
        if (!outboxExists)
        {
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "NotificationRequested",
                CorrelationId = correlationId,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    userId = subscription.UserId,
                    templateKey,
                    subscriptionId = subscription.Id,
                    tariffId = subscription.TariffId,
                    tariffName = subscription.Tariff?.Name ?? string.Empty,
                    status = subscription.Status.ToString(),
                    endAt = subscription.EndAt,
                    gracePeriodEndAt = subscription.GracePeriodEndAt
                })
            });
        }

        var telegramAccounts = await _db.TelegramAccounts.AsNoTracking()
            .Where(x => x.UserId == subscription.UserId && !x.IsBlocked)
            .ToListAsync(cancellationToken);
        if (telegramAccounts.Count == 0)
        {
            return;
        }

        var text = BuildLifecycleTelegramText(subscription, eventType);
        var payloadJson = JsonSerializer.Serialize(new
        {
            text,
            replyMarkupJson = "{\"inline_keyboard\":[[{\"text\":\"Продлить подписку\",\"callback_data\":\"renew\"}]]}"
        });

        foreach (var account in telegramAccounts)
        {
            var exists = await _db.TelegramBotNotifications.AsNoTracking()
                .AnyAsync(x => x.TelegramUserId == account.TelegramUserId && x.Type == eventType && x.PayloadJson == payloadJson && x.Status != "failed" && x.Status != "cancelled", cancellationToken);
            if (exists)
            {
                continue;
            }

            _db.TelegramBotNotifications.Add(new TelegramBotNotification
            {
                TelegramUserId = account.TelegramUserId,
                Type = eventType,
                PayloadJson = payloadJson,
                Status = "pending",
                NextAttemptAt = now
            });
        }
    }

    private static string BuildLifecycleTelegramText(Subscription subscription, string eventType)
    {
        var tariffName = subscription.Tariff?.Name ?? "VPN";
        return eventType == "subscription_expired"
            ? $"Срок подписки {tariffName} истек, VPN-доступ отключен. Продлите тариф, чтобы восстановить подключение."
            : $"Подписка {tariffName} перешла в льготный период до {subscription.GracePeriodEndAt:yyyy-MM-dd HH:mm} UTC. Продлите тариф, чтобы VPN-доступ не отключился.";
    }

    private async Task QueueVpnAccessFailedNotificationAsync(Subscription subscription, Tariff tariff, string error, CancellationToken cancellationToken)
    {
        var accounts = await _db.TelegramAccounts.AsNoTracking().Where(x => x.UserId == subscription.UserId && !x.IsBlocked).ToListAsync(cancellationToken);
        foreach (var account in accounts)
        {
            _db.TelegramBotNotifications.Add(new TelegramBotNotification
            {
                TelegramUserId = account.TelegramUserId,
                Type = "vpn_access_failed",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    text = $"Оплата получена, но VPN-доступ по тарифу {tariff.Name} пока не выдан. Мы уже видим ошибку в админке и свяжемся с вами. Ошибка: {error}",
                    replyMarkupJson = "{\"inline_keyboard\":[[{\"text\":\"Поддержка\",\"callback_data\":\"support\"}]]}"
                }),
                Status = "pending",
                NextAttemptAt = _clock.UtcNow
            });
        }
    }

    private async Task RecordProvisioningFailureAsync(
        Subscription subscription,
        Order order,
        Tariff tariff,
        string scenarioKey,
        string safeError,
        string auditAction,
        DateTimeOffset now)
    {
        StatusStateMachine.SetSubscriptionStatus(subscription, SubscriptionStatus.PendingActivation, now);
        subscription.BlockReason = safeError;
        StatusStateMachine.SetOrderStatus(order, OrderStatus.PartiallyProcessed, now);
        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = "system",
            ActorId = "system",
            Action = auditAction,
            EntityType = "Subscription",
            EntityId = subscription.Id.ToString(),
            BeforeJson = "{}",
            AfterJson = JsonSerializer.Serialize(new { error = safeError, orderId = order.Id, tariffId = tariff.Id, scenarioKey }),
            CreatedAt = now
        });
        await QueueVpnAccessFailedNotificationAsync(subscription, tariff, safeError, CancellationToken.None);
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<string?> TryDeleteCreatedAccessAsync(IVpnProvider provider, string providerAccessId)
    {
        try
        {
            await provider.DeleteAccessAsync(providerAccessId, CancellationToken.None);
            return null;
        }
        catch (Exception ex)
        {
            return SafeError(ex.Message);
        }
    }

    private static string SafeError(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "VPN access provisioning failed."
            : SensitiveDataRedactor.Redact(value, maxLength: 500);

    private async Task<Result<WorkScenario?>> ResolveScenarioAsync(Tariff tariff, CancellationToken cancellationToken)
    {
        var key = NormalizeScenarioKey(tariff.ProvisioningScenario);
        if (key == "auto")
        {
            return Result<WorkScenario?>.Success(await _db.WorkScenarios.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key && x.IsActive, cancellationToken));
        }

        var scenario = await _db.WorkScenarios.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (scenario is null)
        {
            return Result<WorkScenario?>.Failure($"Provisioning scenario '{key}' is not configured.");
        }

        if (!scenario.IsActive)
        {
            return Result<WorkScenario?>.Failure($"Provisioning scenario '{key}' is disabled.");
        }

        if (!IsTariffAllowedByScenario(tariff.Id, scenario.AllowedTariffIdsJson))
        {
            return Result<WorkScenario?>.Failure($"Provisioning scenario '{key}' is not allowed for tariff '{tariff.Slug}'.");
        }

        if (!string.Equals(NormalizeScenarioAction(scenario.OnPaymentSucceeded), "create_subscription_and_access", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(NormalizeScenarioAction(scenario.ProvisioningMode), "auto", StringComparison.OrdinalIgnoreCase))
        {
            return Result<WorkScenario?>.Failure($"Provisioning scenario '{key}' does not allow automatic VPN access creation.");
        }

        return Result<WorkScenario?>.Success(scenario);
    }

    private static int ResolveMaxDevices(Tariff tariff, WorkScenario? scenario)
        => Math.Max(1, tariff.MaxDevices > 0 ? tariff.MaxDevices : scenario?.MaxDevices ?? 1);

    private static string NormalizeScenarioKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? "auto" : value.Trim().ToLowerInvariant();

    private static string NormalizeScenarioAction(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static bool IsTariffAllowedByScenario(Guid tariffId, string? allowedTariffIdsJson)
    {
        if (string.IsNullOrWhiteSpace(allowedTariffIdsJson) || allowedTariffIdsJson.Trim() == "[]")
        {
            return true;
        }

        try
        {
            var values = JsonSerializer.Deserialize<List<Guid>>(allowedTariffIdsJson);
            return values is null || values.Count == 0 || values.Contains(tariffId);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
