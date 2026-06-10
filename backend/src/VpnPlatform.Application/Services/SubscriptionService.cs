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
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly NodeAllocationService _nodeAllocationService;
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
        _vpnProviderFactory = vpnProviderFactory;
        _vpnAccessLifecycleService = vpnAccessLifecycleService;
    }

    public async Task<Result<ActivationResult>> ActivateOrRenewFromOrderAsync(Order order, PaymentAttempt payment, CancellationToken cancellationToken = default)
    {
        var tariff = await _db.Tariffs.FirstAsync(x => x.Id == order.TariffId, cancellationToken);
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
        var inboundSelectionRule = string.IsNullOrWhiteSpace(scenario?.InboundSelectionRule) ? "default" : scenario.InboundSelectionRule.Trim().ToLowerInvariant();
        var generateQrCode = scenario?.GenerateQrCode ?? true;
        var useSandboxProvisioning = payment.ProviderMode == PaymentProviderMode.Sandbox;

        var existing = await _db.Subscriptions
            .Include(x => x.CurrentAccess)
            .FirstOrDefaultAsync(x => x.UserId == order.UserId && x.TariffId == order.TariffId && x.Status != SubscriptionStatus.Cancelled && x.Status != SubscriptionStatus.Blocked, cancellationToken);

        var now = _clock.UtcNow;
        Subscription subscription;
        if (existing is null || order.Type == OrderType.NewSubscription)
        {
            subscription = new Subscription
            {
                UserId = order.UserId,
                TariffId = order.TariffId,
                Status = SubscriptionStatus.PendingActivation,
                StartAt = now,
                EndAt = now.AddDays(tariff.DurationDays),
                GracePeriodEndAt = BusinessRules.GetGracePeriodEnd(now.AddDays(tariff.DurationDays)),
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
            var baseDate = BusinessRules.GetRenewalBaseDate(now, subscription.EndAt);
            subscription.EndAt = baseDate.AddDays(tariff.DurationDays);
            subscription.GracePeriodEndAt = BusinessRules.GetGracePeriodEnd(subscription.EndAt);
            subscription.LastPaymentId = payment.Id;
            subscription.RenewalCount += 1;
            subscription.Status = SubscriptionStatus.Active;
            subscription.BlockReason = null;
            subscription.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var previousServerId = subscription.CurrentServerId;

        try
        {
            var node = subscription.CurrentServerId.HasValue
                ? await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == subscription.CurrentServerId.Value, cancellationToken)
                : null;

            if (node is null
                || node.Status is NodeStatus.Maintenance or NodeStatus.Draining or NodeStatus.Disabled or NodeStatus.Archived
                || !node.IsAvailableForNewUsers
                || (!useSandboxProvisioning && NodeAllocationService.IsSandboxNode(node)))
            {
                node = useSandboxProvisioning
                    ? await _nodeAllocationService.SelectOrCreateSandboxNodeAsync(protocol, cancellationToken)
                    : await _nodeAllocationService.SelectNodeAsync(tariff, scenario, cancellationToken);
            }

            var access = subscription.CurrentAccess ?? await _db.AccessCredentials.FirstOrDefaultAsync(x => x.SubscriptionId == subscription.Id, cancellationToken);
            var provider = _vpnProviderFactory.Get(string.IsNullOrWhiteSpace(access?.ProviderType) ? "x3ui" : access.ProviderType);
            var request = new VpnProvisionRequest(subscription.Id, subscription.UserId, tariff.Id, node.Id, subscription.EndAt, maxDevices, protocol, trafficLimit, generateQrCode, scenarioKey, inboundSelectionRule, useSandboxProvisioning);

            if (access is null)
            {
                var provisionResult = await provider.CreateAccessAsync(request, cancellationToken);
                access = new AccessCredential
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

                _db.AccessCredentials.Add(access);
                await _db.SaveChangesAsync(cancellationToken);
                _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                {
                    AccessCredentialId = access.Id,
                    SubscriptionId = subscription.Id,
                    EventType = "AccessCreated",
                    OldValueJson = "{}",
                    NewValueJson = JsonSerializer.Serialize(new { access.ProviderAccessId, access.AccessUri, access.Status, subscription.EndAt, scenarioKey, maxDevices, protocol, trafficLimit })
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
                access.Status = AccessCredentialStatus.Active;
                access.DisabledAt = null;
                access.LastSyncedAt = now;
                access.Revision += 1;
                access.UpdatedAt = now;

                _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                {
                    AccessCredentialId = access.Id,
                    SubscriptionId = subscription.Id,
                    EventType = wasDisabled ? "AccessRenewedAndEnabled" : "AccessUpdated",
                    OldValueJson = before,
                    NewValueJson = JsonSerializer.Serialize(new { access.ProviderAccessId, access.AccessUri, access.Status, subscription.EndAt, access.Revision, scenarioKey, maxDevices, protocol, trafficLimit })
                });
            }

            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentServerId = node.Id;
            subscription.CurrentAccessId = access.Id;
            subscription.UpdatedAt = now;
            order.Status = OrderStatus.Completed;
            order.PaidAt = payment.PaidAt ?? now;

            if (previousServerId != node.Id || existing is null)
            {
                node.UsedCapacity = Math.Min(node.Capacity, node.UsedCapacity + 1);
            }

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
                  "scenarioKey": "{{scenarioKey}}"
                }
                """
            });

            await _db.SaveChangesAsync(cancellationToken);
            return Result<ActivationResult>.Success(new ActivationResult(subscription.Id, access.Id));
        }
        catch (Exception ex)
        {
            var safeError = SafeError(ex.Message);
            subscription.Status = SubscriptionStatus.PendingActivation;
            subscription.BlockReason = safeError;
            subscription.UpdatedAt = now;
            order.Status = OrderStatus.PartiallyProcessed;
            order.UpdatedAt = now;
            _db.AuditLogs.Add(new AuditLog
            {
                ActorType = "system",
                ActorId = "system",
                Action = "vpn_access.provisioning_failed",
                EntityType = "Subscription",
                EntityId = subscription.Id.ToString(),
                BeforeJson = "{}",
                AfterJson = JsonSerializer.Serialize(new { error = safeError, orderId = order.Id, tariffId = tariff.Id, scenarioKey }),
                CreatedAt = now
            });
            await QueueVpnAccessFailedNotificationAsync(subscription, tariff, safeError, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<ActivationResult>.Failure(safeError);
        }
    }

    public async Task<int> ProcessLifecycleAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var subscriptions = await _db.Subscriptions
            .Include(x => x.CurrentAccess)
            .Include(x => x.Tariff)
            .ToListAsync(cancellationToken);

        var moveToGrace = subscriptions
            .Where(x => x.Status == SubscriptionStatus.Active && x.EndAt <= now)
            .ToList();

        foreach (var item in moveToGrace)
        {
            item.Status = SubscriptionStatus.GracePeriod;
            item.UpdatedAt = now;
            await QueueLifecycleNotificationAsync(item, "subscription_expiring", "subscription_expiring", now, cancellationToken);
        }

        var expire = subscriptions
            .Where(x => x.Status == SubscriptionStatus.GracePeriod && x.GracePeriodEndAt.HasValue && x.GracePeriodEndAt <= now)
            .ToList();

        foreach (var item in expire)
        {
            item.Status = SubscriptionStatus.Expired;
            item.UpdatedAt = now;

            if (item.CurrentAccess is not null)
            {
                if (_vpnAccessLifecycleService is not null)
                {
                    await _vpnAccessLifecycleService.DisableAccessAsync(item.CurrentAccess, "AccessDisabledOnExpiry", "subscription_expired", null, cancellationToken);
                }
                else
                {
                    try
                    {
                        var provider = _vpnProviderFactory.Get(item.CurrentAccess.ProviderType);
                        await provider.DisableAccessAsync(item.CurrentAccess.ProviderAccessId, cancellationToken);
                        item.CurrentAccess.Status = AccessCredentialStatus.Disabled;
                        item.CurrentAccess.DisabledAt = now;
                        item.CurrentAccess.UpdatedAt = now;
                        _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                        {
                            AccessCredentialId = item.CurrentAccess.Id,
                            SubscriptionId = item.Id,
                            EventType = "AccessDisabledOnExpiry",
                            OldValueJson = "{}",
                            NewValueJson = JsonSerializer.Serialize(new { item.CurrentAccess.ProviderAccessId, item.CurrentAccess.Status, disabledAt = now })
                        });
                    }
                    catch (Exception ex)
                    {
                        var safeError = SafeError(ex.Message);
                        item.CurrentAccess.Status = AccessCredentialStatus.Error;
                        item.CurrentAccess.UpdatedAt = now;
                        _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                        {
                            AccessCredentialId = item.CurrentAccess.Id,
                            SubscriptionId = item.Id,
                            EventType = "AccessDisableFailedOnExpiry",
                            OldValueJson = "{}",
                            NewValueJson = JsonSerializer.Serialize(new { item.CurrentAccess.ProviderAccessId, error = safeError })
                        });
                    }
                }
            }

            await QueueLifecycleNotificationAsync(item, "subscription_expired", "subscription_expired", now, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return moveToGrace.Count + expire.Count;
    }

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
