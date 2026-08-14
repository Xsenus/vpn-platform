using System.Security.Claims;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Services;

namespace VpnPlatform.Api.Controllers.Admin;

public sealed record CreateServerHttpRequest(
    string Name,
    string Host,
    string IpAddress,
    string Provider,
    string Region,
    string Country,
    string Datacenter,
    int Capacity,
    string? SupportedProtocolsCsv,
    int Priority,
    string? TagsCsv,
    string? SshUser,
    int SshPort,
    string? SshPrivateKeyPath,
    bool SkipHostKeyChecking,
    string? PanelBaseUrl,
    string? PanelUsername,
    string? PanelPassword,
    int? PanelInboundId,
    string? PublicHostname,
    int PublicPort,
    Guid? NodeGroupId,
    string? SshAuthMethod = null,
    string? SshCredential = null,
    bool ValidationMode = true,
    string? OwnerType = null,
    int? Revision = null);

public sealed record QueueProvisionHttpRequest(bool DryRun = false, int? Revision = null);
public sealed record ProvisioningRunActionHttpRequest(int? Revision = null);
public sealed record ServerStateActionHttpRequest(int? Revision = null);
public sealed record RefundPaymentHttpRequest(decimal Amount, string? Reason);
public sealed record RefundReadinessDto(bool IsSupported, bool CanRefund, decimal RefundableAmount, IReadOnlyList<string> Blockers);
public sealed record RecheckReadinessDto(bool IsSupported, bool CanRecheck, IReadOnlyList<string> Blockers);
public sealed record RefundRecheckReadinessDto(bool IsSupported, bool CanRecheck, IReadOnlyList<string> Blockers);
public sealed record RefundRetryReadinessDto(bool IsSupported, bool CanRetry, IReadOnlyList<string> Blockers);
public sealed record AdminPaymentRecheckDto(Guid OrderId, Guid PaymentId, string Status);
public sealed record SetProviderEnabledHttpRequest(bool Enabled);
public sealed record AdminSupportReplyHttpRequest(string Text, int? Revision = null);
public sealed record AdminSupportStatusHttpRequest(string Status, Guid? AssignedToUserId = null, int? Revision = null);
public sealed record AdminSupportNoteHttpRequest(string Text, int? Revision = null);
public sealed record AdminSubscriptionExtendHttpRequest(int Days, string? Reason = null);
public sealed record AdminAccessActionHttpRequest(string? Reason = null);
public sealed record SetNodeAllocationHttpRequest(bool Available);
public sealed record DeleteServerHttpResponse(
    Guid Id,
    bool Deleted,
    bool Archived,
    int LinkedSubscriptions,
    int LinkedAccesses,
    int LinkedProvisioningRuns,
    int LinkedHealthChecks,
    int LinkedMigrationJobs);
public sealed record NodeHealthCheckDto(Guid Id, Guid NodeId, string Status, DateTimeOffset CheckedAt, long LatencyMs, string MetadataJson, string ErrorText);
public sealed record AdminAuditLogFilters(string? Action = null, string? EntityType = null, string? ActorType = null, string? Search = null, DateTimeOffset? From = null, DateTimeOffset? To = null, int Limit = 200);
public sealed record AdminNotificationDeliveryFilters(string? Status = null, string? TemplateKey = null, string? Search = null, int Limit = 100);
public sealed record ReferralProgramUpsertHttpRequest(
    string Name,
    string Status,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    string RuleDefinition,
    string RewardDefinition,
    string? AntiFraudSettings = null);
public sealed record ReferralProgramDto(
    Guid Id,
    int Revision,
    string Name,
    string Status,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    string RuleDefinition,
    string RewardDefinition,
    string AntiFraudSettings,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
public sealed record AdminRewardLedgerDto(
    Guid Id,
    Guid UserId,
    Guid? SourceUserId,
    Guid? ReferralProgramId,
    string Type,
    string Status,
    decimal Value,
    string CurrencyOrUnit,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset CreatedAt);

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin")]
public class AdminOperationsController : ControllerBase
{
    private const string PrecheckReportStepName = "Precheck report";
    private const int ServerListLimit = 300;
    private const int ServerHealthDiagnosticsLimit = 6000;
    private const int TariffListLimit = 200;
    private const int SubscriptionListLimit = 300;
    private const int AccessCredentialListLimit = 300;
    private const int AccessCredentialHistoryLimit = 5;
    private const int SupportConversationListLimit = 200;
    private const int SupportMessageListLimit = 200;
    private const int PaymentListLimit = 300;
    private const int RefundListLimit = 300;
    private const int OrderListLimit = 300;

    private static readonly HashSet<string> TariffPatchFields = new(StringComparer.Ordinal)
    {
        "revision",
        "name",
        "slug",
        "description",
        "fullDescription",
        "featuresJson",
        "badge",
        "durationDays",
        "price",
        "currency",
        "maxDevices",
        "trafficLimit",
        "isTrial",
        "isActive",
        "sortOrder",
        "visibleFrom",
        "visibleTo",
        "tariffType",
        "category",
        "allowedRegionsCsv",
        "allowedNodeGroupsCsv",
        "isReferralEligible",
        "provisioningScenario",
        "afterPaymentText"
    };

    private static readonly string[] FinanceAuditEntityTypes =
    [
        nameof(CheckoutSession),
        nameof(Order),
        nameof(PaymentProviderAccount),
        nameof(PaymentProviderSetting),
        nameof(PaymentAttempt),
        nameof(PaymentWebhookEvent),
        nameof(Refund),
        nameof(PaymentReceipt)
    ];

    private static readonly string[] SupportAuditEntityTypes =
    [
        nameof(SupportConversation),
        nameof(SupportMessage)
    ];

    private static readonly HashSet<string> ReferralProgramPatchFields = new(StringComparer.Ordinal)
    {
        "revision",
        "name",
        "status",
        "startAt",
        "endAt",
        "ruleDefinition",
        "rewardDefinition",
        "antiFraudSettings"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static void ResetSubscriptionLifecycleState(Subscription subscription)
    {
        subscription.LifecycleAttemptCount = 0;
        subscription.LifecycleProcessingStartedAt = null;
        subscription.LifecycleLeaseExpiresAt = null;
        subscription.LifecycleNextAttemptAt = null;
        subscription.LifecycleLastError = null;
    }

    private readonly IApplicationDbContext _db;
    private readonly ProvisioningService _provisioningService;
    private readonly PaymentOrchestrator _paymentOrchestrator;
    private readonly PaymentProviderAccountService _paymentProviderAccounts;
    private readonly VpnAccessLifecycleService? _vpnAccessLifecycleService;
    private readonly ISecretProtector? _secretProtector;
    private readonly IQrCodeGenerator? _qrCodeGenerator;
    private readonly IVpnProviderFactory? _vpnProviderFactory;
    private readonly IClock _clock;
    private readonly X3UiPanelService? _x3UiPanelService;
    private readonly IHostEnvironment? _hostEnvironment;

    public AdminOperationsController(
        IApplicationDbContext db,
        ProvisioningService provisioningService,
        PaymentOrchestrator paymentOrchestrator,
        PaymentProviderAccountService paymentProviderAccounts,
        VpnAccessLifecycleService? vpnAccessLifecycleService = null,
        ISecretProtector? secretProtector = null,
        IQrCodeGenerator? qrCodeGenerator = null,
        IVpnProviderFactory? vpnProviderFactory = null,
        IClock? clock = null,
        X3UiPanelService? x3UiPanelService = null,
        IHostEnvironment? hostEnvironment = null)
    {
        _db = db;
        _provisioningService = provisioningService;
        _paymentOrchestrator = paymentOrchestrator;
        _paymentProviderAccounts = paymentProviderAccounts;
        _vpnAccessLifecycleService = vpnAccessLifecycleService;
        _secretProtector = secretProtector;
        _qrCodeGenerator = qrCodeGenerator;
        _vpnProviderFactory = vpnProviderFactory;
        _clock = clock ?? new SystemClock();
        _x3UiPanelService = x3UiPanelService;
        _hostEnvironment = hostEnvironment;
    }

    [HttpGet("audit-logs")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AdminAuditLogFilters filters, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(filters.Limit, 1, 500);
        var roles = User?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray() ?? [];
        var hasFinanceAccess = AdminPolicies.HasAccess(roles, AdminPolicies.FinanceRead);
        var hasSupportAccess = AdminPolicies.HasAccess(roles, AdminPolicies.SupportRead);
        var hasBotAccess = AdminPolicies.HasAccess(roles, AdminPolicies.BotManage);
        var action = filters.Action?.Trim() ?? string.Empty;
        var entityType = filters.EntityType?.Trim() ?? string.Empty;
        var actorType = filters.ActorType?.Trim() ?? string.Empty;
        var search = filters.Search?.Trim() ?? string.Empty;
        var from = filters.From;
        var to = filters.To;

        List<AuditLog> rows;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            rows = await _db.AuditLogs
                .FromSqlInterpolated($$"""
                    SELECT *
                    FROM "AuditLogs"
                    WHERE ({{(hasFinanceAccess ? 1 : 0)}} = 1 OR (
                        "Action" NOT LIKE 'payment%'
                        AND "Action" NOT LIKE 'checkout.%'
                        AND "Action" NOT LIKE 'refund.%'
                        AND "Action" NOT LIKE 'order.%'
                        AND "EntityType" NOT IN (
                            {{FinanceAuditEntityTypes[0]}}, {{FinanceAuditEntityTypes[1]}},
                            {{FinanceAuditEntityTypes[2]}}, {{FinanceAuditEntityTypes[3]}},
                            {{FinanceAuditEntityTypes[4]}}, {{FinanceAuditEntityTypes[5]}},
                            {{FinanceAuditEntityTypes[6]}}, {{FinanceAuditEntityTypes[7]}}
                        )
                    ))
                    AND ({{(hasSupportAccess ? 1 : 0)}} = 1 OR (
                        "Action" NOT LIKE 'support.%'
                        AND "EntityType" NOT IN ({{SupportAuditEntityTypes[0]}}, {{SupportAuditEntityTypes[1]}})
                    ))
                    AND ({{(hasBotAccess ? 1 : 0)}} = 1 OR (
                        "Action" NOT LIKE 'telegram_bot.%'
                        AND "EntityType" <> 'TelegramBotSettings'
                    ))
                    AND ({{(action.Length > 0 ? 1 : 0)}} = 0 OR instr("Action", {{action}}) > 0)
                    AND ({{(entityType.Length > 0 ? 1 : 0)}} = 0 OR "EntityType" = {{entityType}})
                    AND ({{(actorType.Length > 0 ? 1 : 0)}} = 0 OR "ActorType" = {{actorType}})
                    AND ({{(search.Length > 0 ? 1 : 0)}} = 0 OR (
                        instr("Action", {{search}}) > 0
                        OR instr("EntityType", {{search}}) > 0
                        OR instr("EntityId", {{search}}) > 0
                        OR instr("ActorId", {{search}}) > 0
                    ))
                    AND ({{(from.HasValue ? 1 : 0)}} = 0 OR julianday("CreatedAt") >= julianday({{from}}))
                    AND ({{(to.HasValue ? 1 : 0)}} = 0 OR julianday("CreatedAt") <= julianday({{to}}))
                    ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                    LIMIT {{limit}}
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        else
        {
            var query = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (!hasFinanceAccess)
            {
                query = query.Where(x =>
                    !x.Action.StartsWith("payment") &&
                    !x.Action.StartsWith("checkout.") &&
                    !x.Action.StartsWith("refund.") &&
                    !x.Action.StartsWith("order.") &&
                    !FinanceAuditEntityTypes.Contains(x.EntityType));
            }

            if (!hasSupportAccess)
            {
                query = query.Where(x =>
                    !x.Action.StartsWith("support.") &&
                    !SupportAuditEntityTypes.Contains(x.EntityType));
            }

            if (!hasBotAccess)
            {
                query = query.Where(x =>
                    !x.Action.StartsWith("telegram_bot.") &&
                    x.EntityType != "TelegramBotSettings");
            }

            if (action.Length > 0)
            {
                query = query.Where(x => x.Action.Contains(action));
            }

            if (entityType.Length > 0)
            {
                query = query.Where(x => x.EntityType == entityType);
            }

            if (actorType.Length > 0)
            {
                query = query.Where(x => x.ActorType == actorType);
            }

            if (search.Length > 0)
            {
                query = query.Where(x =>
                    x.Action.Contains(search) ||
                    x.EntityType.Contains(search) ||
                    x.EntityId.Contains(search) ||
                    x.ActorId.Contains(search));
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= to.Value);
            }

            rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        var logs = rows
            .Select(x => new AdminAuditLogDto(
                x.Id,
                x.ActorType,
                x.ActorId,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.BeforeJson,
                x.AfterJson,
                x.Ip,
                x.UserAgent,
                x.CreatedAt))
            .ToList();

        return Ok(logs);
    }

    [HttpGet("notification-deliveries")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetNotificationDeliveries([FromQuery] AdminNotificationDeliveryFilters filters, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(filters.Limit, 1, 500);
        var status = NotificationDeliveryStatus.Pending;
        var hasStatus = !string.IsNullOrWhiteSpace(filters.Status);
        if (hasStatus
            && (!Enum.TryParse(filters.Status!.Trim(), true, out status) || !Enum.IsDefined(status)))
        {
            return BadRequest(new { error = "Invalid notification delivery status." });
        }

        var templateKey = filters.TemplateKey?.Trim() ?? string.Empty;
        var search = filters.Search?.Trim() ?? string.Empty;

        List<NotificationDelivery> rows;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            rows = await _db.NotificationDeliveries
                .FromSqlInterpolated($$"""
                    SELECT *
                    FROM "NotificationDeliveries"
                    WHERE ({{(hasStatus ? 1 : 0)}} = 0 OR "Status" = {{(int)status}})
                      AND ({{(templateKey.Length > 0 ? 1 : 0)}} = 0 OR "TemplateKey" = {{templateKey}})
                      AND ({{(search.Length > 0 ? 1 : 0)}} = 0 OR (
                          instr("TemplateKey", {{search}}) > 0
                          OR instr("ToAddress", {{search}}) > 0
                      ))
                    ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                    LIMIT {{limit}}
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        else
        {
            var query = _db.NotificationDeliveries.AsNoTracking().AsQueryable();
            if (hasStatus)
            {
                query = query.Where(x => x.Status == status);
            }

            if (templateKey.Length > 0)
            {
                query = query.Where(x => x.TemplateKey == templateKey);
            }

            if (search.Length > 0)
            {
                query = query.Where(x => x.TemplateKey.Contains(search) || x.ToAddress.Contains(search));
            }

            rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        return Ok(rows.Select(x => new AdminNotificationDeliveryDto(
            x.Id,
            x.UserId,
            x.TemplateKey,
            x.Channel.ToString(),
            MaskEmail(x.ToAddress),
            x.Status.ToString(),
            x.Attempts,
            x.ProcessingStartedAt,
            x.NextAttemptAt,
            x.SentAt,
            SensitiveDataRedactor.Redact(x.ErrorText, [x.ToAddress], maxLength: 500),
            x.CreatedAt,
            x.UpdatedAt)).ToList());
    }

    [HttpPost("notification-deliveries/{id:guid}/retry")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> RetryNotificationDelivery(Guid id, CancellationToken cancellationToken)
    {
        var delivery = await _db.NotificationDeliveries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (delivery is null)
        {
            return NotFound(new { error = "notification_delivery_not_found" });
        }

        if (delivery.Status != NotificationDeliveryStatus.Failed)
        {
            return Conflict(new { error = "notification_delivery_not_failed" });
        }

        var before = JsonSerializer.Serialize(new
        {
            status = delivery.Status.ToString(),
            delivery.Attempts,
            errorText = SensitiveDataRedactor.Redact(delivery.ErrorText, [delivery.ToAddress], maxLength: 500)
        });
        delivery.Status = NotificationDeliveryStatus.Pending;
        delivery.Attempts = 0;
        delivery.ProcessingStartedAt = null;
        delivery.NextAttemptAt = _clock.UtcNow;
        delivery.ErrorText = null;
        delivery.SentAt = null;
        delivery.UpdatedAt = _clock.UtcNow;
        AddAuditLog(
            "notification_delivery.retry",
            nameof(NotificationDelivery),
            delivery.Id,
            before,
            JsonSerializer.Serialize(new { status = delivery.Status.ToString(), delivery.Attempts, maskedToAddress = MaskEmail(delivery.ToAddress) }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { delivery.Id, Status = delivery.Status.ToString(), delivery.NextAttemptAt });
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var subscriptions = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? await _db.Subscriptions
                .FromSqlRaw("""
                    SELECT *
                    FROM "Subscriptions"
                    ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                    LIMIT 300
                    """)
                .AsNoTracking()
                .Include(x => x.Tariff)
                .ToListAsync(cancellationToken)
            : await _db.Subscriptions.AsNoTracking()
                .Include(x => x.Tariff)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(SubscriptionListLimit)
                .ToListAsync(cancellationToken);

        return Ok(subscriptions
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.TariffId,
                TariffName = x.Tariff != null ? x.Tariff.Name : string.Empty,
                Status = x.Status.ToString(),
                x.StartAt,
                x.EndAt,
                x.GracePeriodEndAt,
                x.AutoRenewFlag,
                SourceChannel = x.SourceChannel.ToString(),
                x.CurrentServerId,
                x.CurrentAccessId,
                x.LastPaymentId,
                x.RenewalCount,
                x.BlockReason,
                x.SuspendedAt,
                x.CancelledAt,
                x.LifecycleAttemptCount,
                x.LifecycleProcessingStartedAt,
                x.LifecycleLeaseExpiresAt,
                x.LifecycleNextAttemptAt,
                x.LifecycleLastError,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToList());
    }

    [HttpGet("access-credentials")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetAccessCredentials(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var isSqlite = _db is DbContext dbContext && dbContext.Database.IsSqlite();
        var accessCredentials = isSqlite
            ? await _db.AccessCredentials
                .FromSqlRaw("""
                    SELECT *
                    FROM "AccessCredentials"
                    ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                    LIMIT 300
                    """)
                .AsNoTracking()
                .Include(x => x.Subscription)
                .Include(x => x.Server)
                .ToListAsync(cancellationToken)
            : await _db.AccessCredentials.AsNoTracking()
                .Include(x => x.Subscription)
                .Include(x => x.Server)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(AccessCredentialListLimit)
                .ToListAsync(cancellationToken);

        List<AccessCredentialHistory> boundedHistory;
        if (isSqlite)
        {
            boundedHistory = await _db.AccessCredentialHistories
                .FromSqlRaw("""
                    WITH "SelectedAccesses" AS (
                        SELECT "Id"
                        FROM "AccessCredentials"
                        ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                        LIMIT 300
                    ),
                    "RankedHistory" AS (
                        SELECT
                            h."Id",
                            h."AccessCredentialId",
                            h."SubscriptionId",
                            h."EventType",
                            h."OldValueJson",
                            h."NewValueJson",
                            h."CreatedAt",
                            h."UpdatedAt",
                            ROW_NUMBER() OVER (
                                PARTITION BY h."AccessCredentialId"
                                ORDER BY julianday(h."CreatedAt") DESC, h."Id" DESC
                            ) AS "_HistoryRank"
                        FROM "AccessCredentialHistories" AS h
                        INNER JOIN "SelectedAccesses" AS selected ON selected."Id" = h."AccessCredentialId"
                    )
                    SELECT
                        "Id",
                        "AccessCredentialId",
                        "SubscriptionId",
                        "EventType",
                        "OldValueJson",
                        "NewValueJson",
                        "CreatedAt",
                        "UpdatedAt"
                    FROM "RankedHistory"
                    WHERE "_HistoryRank" <= 5
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        else if (_db is DbContext relationalContext
                 && relationalContext.Database.IsRelational()
                 && relationalContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            boundedHistory = await _db.AccessCredentialHistories
                .FromSqlRaw("""
                    WITH "SelectedAccesses" AS (
                        SELECT "Id"
                        FROM "AccessCredentials"
                        ORDER BY "CreatedAt" DESC, "Id" DESC
                        LIMIT 300
                    ),
                    "RankedHistory" AS (
                        SELECT
                            h."Id",
                            h."AccessCredentialId",
                            h."SubscriptionId",
                            h."EventType",
                            h."OldValueJson",
                            h."NewValueJson",
                            h."CreatedAt",
                            h."UpdatedAt",
                            ROW_NUMBER() OVER (
                                PARTITION BY h."AccessCredentialId"
                                ORDER BY h."CreatedAt" DESC, h."Id" DESC
                            ) AS "_HistoryRank"
                        FROM "AccessCredentialHistories" AS h
                        INNER JOIN "SelectedAccesses" AS selected ON selected."Id" = h."AccessCredentialId"
                    )
                    SELECT
                        "Id",
                        "AccessCredentialId",
                        "SubscriptionId",
                        "EventType",
                        "OldValueJson",
                        "NewValueJson",
                        "CreatedAt",
                        "UpdatedAt"
                    FROM "RankedHistory"
                    WHERE "_HistoryRank" <= 5
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        else
        {
            var selectedAccessIds = accessCredentials.Select(x => x.Id).ToList();
            boundedHistory = (await _db.AccessCredentialHistories.AsNoTracking()
                    .Where(x => selectedAccessIds.Contains(x.AccessCredentialId))
                    .ToListAsync(cancellationToken))
                .GroupBy(x => x.AccessCredentialId)
                .SelectMany(group => group
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .Take(AccessCredentialHistoryLimit))
                .ToList();
        }

        var historiesByAccess = boundedHistory
            .GroupBy(x => x.AccessCredentialId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new AdminAccessCredentialHistoryDto(x.Id, x.AccessCredentialId, x.SubscriptionId, x.EventType, x.OldValueJson, x.NewValueJson, x.CreatedAt))
                    .ToList());

        return Ok(accessCredentials
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x =>
            {
                var subscriptionAvailable = x.Subscription is not null
                    && BusinessRules.IsSubscriptionAccessAvailable(
                        x.Subscription.Status,
                        x.Subscription.EndAt,
                        x.Subscription.GracePeriodEndAt,
                        now);
                var accessAvailable = x.Status != AccessCredentialStatus.Revoked && subscriptionAvailable;
                return new
                {
                    IsTerminal = !accessAvailable,
                    x.Id,
                    x.SubscriptionId,
                    UserId = x.Subscription?.UserId,
                    SubscriptionStatus = x.Subscription?.Status.ToString() ?? string.Empty,
                    x.ProviderType,
                    ProviderAccessId = accessAvailable ? x.ProviderAccessId : string.Empty,
                    x.ServerId,
                    ServerName = x.Server?.Name ?? string.Empty,
                    AccessUri = accessAvailable ? x.AccessUri : string.Empty,
                    QrCodePayload = accessAvailable ? x.QrCodePath : string.Empty,
                    QrCodePath = accessAvailable ? x.QrCodePath : string.Empty,
                    ConfigPath = accessAvailable ? x.ConfigPath : string.Empty,
                    Status = x.Status.ToString(),
                    x.IssuedAt,
                    ExpiryDate = x.Subscription is not null
                        ? BusinessRules.GetSubscriptionAccessEnd(x.Subscription.EndAt, x.Subscription.GracePeriodEndAt)
                        : (DateTimeOffset?)null,
                    x.DisabledAt,
                    x.LastSyncedAt,
                    x.Revision,
                    History = historiesByAccess.GetValueOrDefault(x.Id) ?? [],
                    x.CreatedAt,
                    x.UpdatedAt
                };
            }).ToList());
    }

    [HttpPost("subscriptions/{id:guid}/extend")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> ExtendSubscription(Guid id, [FromBody] AdminSubscriptionExtendHttpRequest request, CancellationToken cancellationToken)
    {
        if (request.Days <= 0 || request.Days > 3650)
        {
            return BadRequest(new { error = "Extension days must be between 1 and 3650." });
        }

        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(id, cancellationToken);
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        var now = _clock.UtcNow;
        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.EndAt, subscription.GracePeriodEndAt, subscription.BlockReason });
        if (!StatusStateMachine.CanTransition(subscription.Status, SubscriptionStatus.Active))
        {
            return BadRequest(new { error = $"Subscription status transition {subscription.Status} -> {SubscriptionStatus.Active} is not allowed." });
        }

        if (subscription.CurrentAccess is not null && subscription.CurrentAccess.Status != AccessCredentialStatus.Active)
        {
            if (_vpnAccessLifecycleService is null)
            {
                return BadRequest(new { error = "VPN access lifecycle service is not configured." });
            }

            var accessResult = await _vpnAccessLifecycleService.EnableAccessAsync(
                subscription.CurrentAccess.Id,
                request.Reason ?? "manual_subscription_extend",
                ResolveUserId(),
                cancellationToken,
                allowUnavailableSubscription: true);
            if (!accessResult.IsSuccess)
            {
                return BadRequest(new { error = accessResult.Error });
            }
        }

        var baseDate = subscription.EndAt > now ? subscription.EndAt : now;
        subscription.EndAt = baseDate.AddDays(request.Days);
        subscription.GracePeriodEndAt = subscription.EndAt.AddDays(3);
        StatusStateMachine.SetSubscriptionStatus(subscription, SubscriptionStatus.Active, now);
        ResetSubscriptionLifecycleState(subscription);
        subscription.BlockReason = null;
        AddAuditLog("subscription.extend", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, subscription.EndAt, request.Days, request.Reason }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { subscription.Id, Status = subscription.Status.ToString(), subscription.EndAt, subscription.GracePeriodEndAt });
    }

    [HttpPost("subscriptions/{id:guid}/activate")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> ActivateSubscription(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(id, cancellationToken);
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        if (subscription.EndAt <= _clock.UtcNow)
        {
            return BadRequest(new { error = "Subscription period has already ended. Extend the subscription before activation." });
        }

        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.BlockReason, subscription.SuspendedAt, subscription.CancelledAt });
        var now = _clock.UtcNow;
        if (!StatusStateMachine.CanTransition(subscription.Status, SubscriptionStatus.Active))
        {
            return BadRequest(new { error = $"Subscription status transition {subscription.Status} -> {SubscriptionStatus.Active} is not allowed." });
        }

        AdminAccessActionResult? accessResult = null;
        if (subscription.CurrentAccess is not null && subscription.CurrentAccess.Status != AccessCredentialStatus.Active)
        {
            if (_vpnAccessLifecycleService is null)
            {
                return BadRequest(new { error = "VPN access lifecycle service is not configured." });
            }

            var result = await _vpnAccessLifecycleService.EnableAccessAsync(
                subscription.CurrentAccess.Id,
                request?.Reason ?? "manual_subscription_activate",
                ResolveUserId(),
                cancellationToken,
                allowUnavailableSubscription: true);
            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            accessResult = result.Value;
        }

        StatusStateMachine.SetSubscriptionStatus(subscription, SubscriptionStatus.Active, now);
        ResetSubscriptionLifecycleState(subscription);
        subscription.BlockReason = null;
        subscription.SuspendedAt = null;
        subscription.CancelledAt = null;
        subscription.GracePeriodEndAt ??= subscription.EndAt.AddDays(3);
        AddAuditLog("subscription.activate", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, request?.Reason }));
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { subscription.Id, Status = subscription.Status.ToString(), subscription.EndAt, subscription.CurrentAccessId, Access = accessResult });
    }

    [HttpPost("subscriptions/{id:guid}/block")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> BlockSubscription(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(id, cancellationToken);
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        var now = _clock.UtcNow;
        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.BlockReason });
        if (!StatusStateMachine.CanTransition(subscription.Status, SubscriptionStatus.Blocked))
        {
            return BadRequest(new { error = $"Subscription status transition {subscription.Status} -> {SubscriptionStatus.Blocked} is not allowed." });
        }

        var blockReason = string.IsNullOrWhiteSpace(request?.Reason) ? "manual_admin_action" : request!.Reason!.Trim();
        if (subscription.CurrentAccess is not null)
        {
            if (_vpnAccessLifecycleService is null)
            {
                return BadRequest(new { error = "VPN access lifecycle service is not configured." });
            }

            var accessResult = await _vpnAccessLifecycleService.DisableAccessAsync(
                subscription.CurrentAccess.Id,
                "AccessDisabledOnSubscriptionBlock",
                blockReason,
                ResolveUserId(),
                cancellationToken);
            if (!accessResult.IsSuccess)
            {
                return BadRequest(new { error = accessResult.Error });
            }
        }

        StatusStateMachine.SetSubscriptionStatus(subscription, SubscriptionStatus.Blocked, now);
        ResetSubscriptionLifecycleState(subscription);
        subscription.BlockReason = blockReason;
        AddAuditLog("subscription.block", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, subscription.BlockReason }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { subscription.Id, Status = subscription.Status.ToString(), subscription.BlockReason });
    }

    [HttpPost("subscriptions/{id:guid}/unblock")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> UnblockSubscription(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(id, cancellationToken);
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        var now = _clock.UtcNow;
        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.BlockReason });
        var nextStatus = subscription.EndAt > now
            ? SubscriptionStatus.Active
            : BusinessRules.GetSubscriptionAccessEnd(subscription.EndAt, subscription.GracePeriodEndAt) > now
                ? SubscriptionStatus.GracePeriod
                : SubscriptionStatus.Expired;
        if (!StatusStateMachine.CanTransition(subscription.Status, nextStatus))
        {
            return BadRequest(new { error = $"Subscription status transition {subscription.Status} -> {nextStatus} is not allowed." });
        }

        if (nextStatus is SubscriptionStatus.Active or SubscriptionStatus.GracePeriod
            && subscription.CurrentAccess is not null
            && subscription.CurrentAccess.Status != AccessCredentialStatus.Active)
        {
            if (_vpnAccessLifecycleService is null)
            {
                return BadRequest(new { error = "VPN access lifecycle service is not configured." });
            }

            var accessResult = await _vpnAccessLifecycleService.EnableAccessAsync(
                subscription.CurrentAccess.Id,
                request?.Reason ?? "manual_subscription_unblock",
                ResolveUserId(),
                cancellationToken,
                allowUnavailableSubscription: true);
            if (!accessResult.IsSuccess)
            {
                return BadRequest(new { error = accessResult.Error });
            }
        }

        StatusStateMachine.SetSubscriptionStatus(subscription, nextStatus, now);
        ResetSubscriptionLifecycleState(subscription);
        subscription.BlockReason = null;
        AddAuditLog("subscription.unblock", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, request?.Reason }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { subscription.Id, Status = subscription.Status.ToString() });
    }

    [HttpPost("subscriptions/{id:guid}/cancel")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> CancelSubscription(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(id, cancellationToken);
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        if (!StatusStateMachine.CanTransition(subscription.Status, SubscriptionStatus.Cancelled))
        {
            return BadRequest(new { error = $"Subscription status transition {subscription.Status} -> {SubscriptionStatus.Cancelled} is not allowed." });
        }

        if (_vpnAccessLifecycleService is null)
        {
            return BadRequest(new { error = "VPN access lifecycle service is not configured." });
        }

        var result = await _vpnAccessLifecycleService.CancelSubscriptionAsync(
            subscription,
            request?.Reason,
            ResolveUserId(),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { subscription.Id, Status = subscription.Status.ToString(), subscription.CancelledAt });
    }

    [HttpPost("subscriptions/{id:guid}/sync-access")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> SyncSubscriptionAccess(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (_vpnAccessLifecycleService is null)
        {
            return BadRequest(new { error = "VPN access lifecycle service is not configured." });
        }

        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(id, cancellationToken);
        var subscription = await _db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();
        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            return BadRequest(new { error = "Cancelled subscription VPN access cannot be synchronized." });
        }

        if (!BusinessRules.IsSubscriptionAccessAvailable(
                subscription.Status,
                subscription.EndAt,
                subscription.GracePeriodEndAt,
                _clock.UtcNow))
        {
            return BadRequest(new { error = "Expired or inactive subscription VPN access cannot be synchronized." });
        }

        if (!subscription.CurrentAccessId.HasValue)
        {
            return BadRequest(new { error = "Subscription does not have a current VPN access." });
        }

        var result = await _vpnAccessLifecycleService.SyncAccessAsync(subscription.CurrentAccessId.Value, request?.Reason ?? "manual_subscription_sync", ResolveUserId(), cancellationToken);
        return result.IsSuccess
            ? Ok(new { subscription.Id, subscription.CurrentAccessId, Access = result.Value })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("access-credentials/{id:guid}/qr")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetAccessCredentialQr(Guid id, CancellationToken cancellationToken)
    {
        var subscriptionId = await _db.AccessCredentials.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (Guid?)x.SubscriptionId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!subscriptionId.HasValue)
        {
            return NotFound(new { error = "VPN access not found." });
        }

        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(subscriptionId.Value, cancellationToken);
        var access = await _db.AccessCredentials.AsNoTracking()
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (access is null)
        {
            return NotFound(new { error = "VPN access not found." });
        }

        if (access.Subscription?.Status == SubscriptionStatus.Cancelled)
        {
            return BadRequest(new { error = "Cancelled subscription VPN access QR code is not available." });
        }

        if (access.Status == AccessCredentialStatus.Revoked)
        {
            return BadRequest(new { error = "Revoked VPN access QR code is not available." });
        }

        if (access.Subscription is null
            || !BusinessRules.IsSubscriptionAccessAvailable(
                access.Subscription.Status,
                access.Subscription.EndAt,
                access.Subscription.GracePeriodEndAt,
                _clock.UtcNow))
        {
            return BadRequest(new { error = "Expired or inactive subscription VPN access QR code is not available." });
        }

        if (_qrCodeGenerator is null)
        {
            return BadRequest(new { error = "QR code generator is not configured." });
        }

        if (string.IsNullOrWhiteSpace(access.AccessUri))
        {
            return BadRequest(new { error = "VPN access URI is not available yet." });
        }

        var qr = _qrCodeGenerator.GenerateSvg(access.AccessUri, $"admin-access-{id:N}");
        return Content(qr.Content, qr.MediaType);
    }

    [HttpPost("access-credentials/{id:guid}/disable")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> DisableAccessCredential(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (_vpnAccessLifecycleService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "VPN access lifecycle service is not configured." });
        }

        var subscriptionId = await _db.AccessCredentials.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (Guid?)x.SubscriptionId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!subscriptionId.HasValue) return NotFound();
        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(subscriptionId.Value, cancellationToken);

        var result = await _vpnAccessLifecycleService.DisableAccessAsync(id, "manual_admin_disable", request?.Reason, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("access-credentials/{id:guid}/enable")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> EnableAccessCredential(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (_vpnAccessLifecycleService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "VPN access lifecycle service is not configured." });
        }

        var subscriptionId = await _db.AccessCredentials.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (Guid?)x.SubscriptionId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!subscriptionId.HasValue) return NotFound();
        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(subscriptionId.Value, cancellationToken);

        var result = await _vpnAccessLifecycleService.EnableAccessAsync(id, request?.Reason, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("access-credentials/{id:guid}/sync")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> SyncAccessCredential(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (_vpnAccessLifecycleService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "VPN access lifecycle service is not configured." });
        }

        var subscriptionId = await _db.AccessCredentials.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (Guid?)x.SubscriptionId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!subscriptionId.HasValue) return NotFound();
        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(subscriptionId.Value, cancellationToken);

        var result = await _vpnAccessLifecycleService.SyncAccessAsync(id, request?.Reason, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("access-credentials/{id:guid}/reset-traffic")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> ResetAccessTraffic(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (_vpnAccessLifecycleService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "VPN access lifecycle service is not configured." });
        }

        var subscriptionId = await _db.AccessCredentials.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (Guid?)x.SubscriptionId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!subscriptionId.HasValue) return NotFound();
        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(subscriptionId.Value, cancellationToken);

        var result = await _vpnAccessLifecycleService.ResetTrafficAsync(id, request?.Reason, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("subscriptions/{id:guid}/migrate")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> MigrateSubscription(Guid id, [FromBody] Guid? targetNodeId, CancellationToken cancellationToken)
    {
        Guid migrationJobId;
        Guid sourceNodeId;
        Guid resolvedTargetNodeId;
        Guid targetInboundId;
        var actorUserId = ResolveUserId();

        await using (var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(id, cancellationToken))
        {
            var subscription = await _db.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (subscription is null)
            {
                return NotFound(new { error = "Subscription not found." });
            }

            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return BadRequest(new { error = "Cancelled subscription cannot be migrated." });
            }

            if (!BusinessRules.IsSubscriptionAccessAvailable(
                    subscription.Status,
                    subscription.EndAt,
                    subscription.GracePeriodEndAt,
                    _clock.UtcNow))
            {
                return BadRequest(new { error = "Expired or inactive subscription cannot be migrated." });
            }

            if (!subscription.CurrentServerId.HasValue)
            {
                return BadRequest(new { error = "Subscription does not have a source VPN server." });
            }

            if (_x3UiPanelService is null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "VPN migration service is unavailable." });
            }

            sourceNodeId = subscription.CurrentServerId.Value;
            if (targetNodeId == sourceNodeId)
            {
                return BadRequest(new { error = "Target VPN server must differ from the source server." });
            }

            var vpnClient = await _db.VpnClients.AsNoTracking()
                .Include(x => x.VpnInbound)
                .FirstOrDefaultAsync(x => x.SubscriptionId == id, cancellationToken);
            if (vpnClient?.VpnInbound is null)
            {
                return BadRequest(new { error = "Subscription does not have a managed 3x-ui client." });
            }

            var hasActiveMigration = await _db.MigrationItems
                .AsNoTracking()
                .AnyAsync(item => item.SubscriptionId == id
                    && _db.MigrationJobs.Any(job => job.Id == item.MigrationJobId
                        && (job.Status == MigrationJobStatus.Planned || job.Status == MigrationJobStatus.Running)), cancellationToken);
            if (hasActiveMigration)
            {
                return Conflict(new { error = "Subscription already has an active migration." });
            }

            var sourceProtocol = vpnClient.VpnInbound.Protocol;
            var selectedTarget = await (
                from node in _db.VpnNodes.AsNoTracking()
                join panel in _db.VpnPanels.AsNoTracking() on node.PanelBaseUrl equals panel.BaseUrl
                join inbound in _db.VpnInbounds.AsNoTracking() on panel.Id equals inbound.VpnPanelId
                where (!targetNodeId.HasValue || node.Id == targetNodeId.Value)
                    && node.Id != sourceNodeId
                    && node.Status == NodeStatus.Ready
                    && node.IsAvailableForNewUsers
                    && node.HealthStatus != HealthStatus.Unhealthy
                    && node.UsedCapacity < node.Capacity
                    && node.PanelBaseUrl != string.Empty
                    && panel.Status == VpnPanelStatus.Active
                    && panel.HealthStatus != HealthStatus.Unhealthy
                    && panel.UsedCapacity < panel.Capacity
                    && inbound.IsActive
                    && inbound.UsedCapacity < inbound.Capacity
                    && inbound.Protocol == sourceProtocol
                    && (!node.PanelInboundId.HasValue || inbound.ExternalInboundId == node.PanelInboundId.Value.ToString())
                orderby node.UsedCapacity * 1.0 / Math.Max(1, node.Capacity),
                    node.Priority,
                    node.Id,
                    inbound.IsDefault descending,
                    inbound.UsedCapacity * 1.0 / Math.Max(1, inbound.Capacity),
                    inbound.Id
                select new { Node = node, Inbound = inbound })
                .FirstOrDefaultAsync(cancellationToken);

            if (selectedTarget is null && targetNodeId.HasValue)
            {
                var targetState = await _db.VpnNodes.AsNoTracking()
                    .Where(x => x.Id == targetNodeId.Value)
                    .Select(x => new
                    {
                        IsReady = x.Id != sourceNodeId
                            && x.Status == NodeStatus.Ready
                            && x.IsAvailableForNewUsers
                            && x.HealthStatus != HealthStatus.Unhealthy
                            && x.UsedCapacity < x.Capacity
                            && x.PanelBaseUrl != string.Empty
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                if (targetState is null)
                {
                    return NotFound(new { error = "Target VPN server not found." });
                }

                if (!targetState.IsReady)
                {
                    return BadRequest(new { error = "Target VPN server is not ready for allocation." });
                }
            }

            if (selectedTarget is null)
            {
                return BadRequest(new { error = "Target VPN server does not have a compatible active inbound." });
            }

            resolvedTargetNodeId = selectedTarget.Node.Id;
            targetInboundId = selectedTarget.Inbound.Id;
            var migrationJob = new MigrationJob
            {
                SourceNodeId = sourceNodeId,
                TargetNodeId = resolvedTargetNodeId,
                Status = MigrationJobStatus.Running,
                Type = "single-subscription",
                RequestedByUserId = actorUserId,
                RequestedAt = _clock.UtcNow,
                StartedAt = _clock.UtcNow,
                Notes = $"Migration requested for subscription {id}"
            };
            migrationJob.Items.Add(new MigrationItem
            {
                SubscriptionId = subscription.Id,
                OldAccessId = subscription.CurrentAccessId,
                Status = MigrationJobStatus.Running
            });

            _db.MigrationJobs.Add(migrationJob);
            AddAuditLog(
                "subscription.migration.start",
                "Subscription",
                subscription.Id,
                JsonSerializer.Serialize(new { subscription.CurrentServerId, subscription.CurrentAccessId }),
                JsonSerializer.Serialize(new { MigrationJobId = migrationJob.Id, migrationJob.TargetNodeId, TargetInboundId = targetInboundId, migrationJob.Status }));
            await _db.SaveChangesAsync(cancellationToken);
            migrationJobId = migrationJob.Id;
        }

        async Task MarkJobAsync(MigrationJobStatus status, string? error)
        {
            var job = await _db.MigrationJobs.Include(x => x.Items).FirstAsync(x => x.Id == migrationJobId, CancellationToken.None);
            job.Status = status;
            job.FinishedAt = _clock.UtcNow;
            job.Notes = string.IsNullOrWhiteSpace(error) ? job.Notes : $"{job.Notes}; {error[..Math.Min(error.Length, 1000)]}";
            foreach (var item in job.Items)
            {
                item.Status = status;
                item.ErrorText = error is null ? string.Empty : error[..Math.Min(error.Length, 2000)];
                if (status == MigrationJobStatus.Completed)
                {
                    item.NewAccessId = await _db.Subscriptions.AsNoTracking()
                        .Where(x => x.Id == item.SubscriptionId)
                        .Select(x => x.CurrentAccessId)
                        .FirstOrDefaultAsync(CancellationToken.None);
                }
            }
            AddAuditLog(
                status == MigrationJobStatus.Completed ? "subscription.migration.complete" : "subscription.migration.fail",
                "Subscription",
                id,
                JsonSerializer.Serialize(new { SourceNodeId = sourceNodeId }),
                JsonSerializer.Serialize(new { MigrationJobId = migrationJobId, TargetNodeId = resolvedTargetNodeId, Status = status, Error = error }));
            await _db.SaveChangesAsync(CancellationToken.None);
        }

        try
        {
            var result = await _x3UiPanelService.MigrateClientAsync(
                (await _db.VpnClients.AsNoTracking().SingleAsync(x => x.SubscriptionId == id, cancellationToken)).Id,
                new MigrateVpnClientCommand(targetInboundId, resolvedTargetNodeId),
                actorUserId,
                cancellationToken);
            if (!result.IsSuccess)
            {
                await MarkJobAsync(MigrationJobStatus.Failed, result.Error ?? "VPN client migration failed.");
                return BadRequest(new { error = result.Error });
            }

            await MarkJobAsync(MigrationJobStatus.Completed, null);
            return Ok(new { migrationJobId, subscriptionId = id, sourceNodeId, targetNodeId = resolvedTargetNodeId, status = "completed" });
        }
        catch (Exception ex)
        {
            await MarkJobAsync(MigrationJobStatus.Failed, $"VPN provider migration failed ({ex.GetType().Name}).");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "VPN provider migration failed; see the migration job and audit log for details." });
        }
    }

    [HttpGet("orders")]
    [Authorize(Policy = AdminPolicies.FinanceRead)]
    public async Task<IActionResult> GetOrders([FromQuery] string? status = null, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var parsedStatuses = ParseOrderStatuses(status);
        if (parsedStatuses is null)
        {
            return BadRequest(new { error = "Invalid order status filter." });
        }

        var searchText = NormalizeSearchText(search);
        var searchEnabled = string.IsNullOrWhiteSpace(searchText) ? 0 : 1;
        var matchingOrderStatuses = MatchingEnumValues<OrderStatus>(searchText);
        var matchingOrderTypes = MatchingEnumValues<OrderType>(searchText);
        var matchingChannels = MatchingEnumValues<ChannelType>(searchText);
        var matchingProviders = MatchingEnumValues<PaymentProvider>(searchText);
        var matchingPaymentStatuses = MatchingEnumValues<PaymentStatus>(searchText);

        IQueryable<Order> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            var statusPredicate = parsedStatuses.Count == 0
                ? "1 = 1"
                : $"customer_order.\"Status\" IN ({string.Join(", ", parsedStatuses.Select(value => (int)value))})";
            var sql = $$"""
                SELECT customer_order.*
                FROM "Orders" AS customer_order
                LEFT JOIN "Users" AS customer ON customer."Id" = customer_order."UserId"
                LEFT JOIN "Tariffs" AS tariff ON tariff."Id" = customer_order."TariffId"
                WHERE {{statusPredicate}}
                  AND ({0} = 0
                       OR instr(lower(CAST(customer_order."Id" AS TEXT)), {1}) > 0
                       OR instr(lower(CAST(customer_order."UserId" AS TEXT)), {1}) > 0
                       OR instr(lower(CAST(customer_order."TariffId" AS TEXT)), {1}) > 0
                       OR instr(lower(COALESCE(customer."Email", '')), {1}) > 0
                       OR instr(lower(COALESCE(customer."DisplayName", '')), {1}) > 0
                       OR instr(lower(COALESCE(tariff."Name", '')), {1}) > 0
                       OR customer_order."Status" IN ({{ToSqlEnumList(matchingOrderStatuses)}})
                       OR customer_order."Type" IN ({{ToSqlEnumList(matchingOrderTypes)}})
                       OR customer_order."Channel" IN ({{ToSqlEnumList(matchingChannels)}})
                       OR customer_order."PaymentProvider" IN ({{ToSqlEnumList(matchingProviders)}})
                       OR instr(lower(customer_order."Currency"), {1}) > 0
                       OR EXISTS (
                           SELECT 1
                           FROM "Payments" AS payment
                           WHERE payment."OrderId" = customer_order."Id"
                             AND (instr(lower(CAST(payment."Id" AS TEXT)), {1}) > 0
                                  OR instr(lower(payment."ProviderPaymentId"), {1}) > 0
                                  OR payment."Status" IN ({{ToSqlEnumList(matchingPaymentStatuses)}}))))
                ORDER BY julianday(customer_order."CreatedAt") DESC, customer_order."Id" DESC
                LIMIT 300
                """;
            query = _db.Orders.FromSqlRaw(sql, searchEnabled, searchText);
        }
        else
        {
            query = _db.Orders;
            if (parsedStatuses.Count > 0)
            {
                query = query.Where(x => parsedStatuses.Contains(x.Status));
            }

            if (searchEnabled != 0)
            {
                query = query.Where(x =>
                    x.Id.ToString().ToLower().Contains(searchText)
                    || x.UserId.ToString().ToLower().Contains(searchText)
                    || x.TariffId.ToString().ToLower().Contains(searchText)
                    || x.User != null && x.User.Email != null && x.User.Email.ToLower().Contains(searchText)
                    || x.User != null && x.User.DisplayName.ToLower().Contains(searchText)
                    || x.Tariff != null && x.Tariff.Name.ToLower().Contains(searchText)
                    || matchingOrderStatuses.Contains(x.Status)
                    || matchingOrderTypes.Contains(x.Type)
                    || matchingChannels.Contains(x.Channel)
                    || matchingProviders.Contains(x.PaymentProvider)
                    || x.Currency.ToLower().Contains(searchText)
                    || x.PaymentAttempts.Any(payment =>
                        payment.Id.ToString().ToLower().Contains(searchText)
                        || payment.ProviderPaymentId.ToLower().Contains(searchText)
                        || matchingPaymentStatuses.Contains(payment.Status)));
            }

            query = query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(OrderListLimit);
        }

        var orders = await query
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Tariff)
            .ToListAsync(cancellationToken);
        orders = orders
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToList();
        var orderIds = orders.Select(x => x.Id).ToList();
        var paymentCounts = await _db.Payments.AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .GroupBy(x => x.OrderId)
            .Select(group => new { OrderId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.OrderId, x => x.Count, cancellationToken);
        var latestPayments = orderIds.Count == 0
            ? []
            : _db is DbContext paymentDbContext && (paymentDbContext.Database.IsSqlite() || paymentDbContext.Database.IsNpgsql())
                ? await LoadLatestOrderPaymentsAsync(paymentDbContext, orderIds, cancellationToken)
                : (await _db.Payments.AsNoTracking()
                    .Include(x => x.PaymentProviderAccount)
                    .Where(x => orderIds.Contains(x.OrderId))
                    .ToListAsync(cancellationToken))
                    .GroupBy(x => x.OrderId)
                    .Select(group => group.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).First())
                    .ToList();
        var latestPaymentByOrder = latestPayments.ToDictionary(x => x.OrderId);

        return Ok(orders.Select(x =>
            {
                var lastPayment = latestPaymentByOrder.GetValueOrDefault(x.Id);
                var recheck = lastPayment is null
                    ? new RecheckReadinessDto(false, false, new[] { "У заказа нет платежной попытки." })
                    : BuildRecheckReadiness(lastPayment);

                return new
                {
                    x.Id,
                    x.UserId,
                    UserDisplayName = x.User != null ? x.User.DisplayName : string.Empty,
                    UserEmail = x.User != null ? x.User.Email : null,
                    x.TariffId,
                    TariffName = x.Tariff != null ? x.Tariff.Name : string.Empty,
                    x.Amount,
                    x.Currency,
                    Status = x.Status.ToString(),
                    Type = x.Type.ToString(),
                    Channel = x.Channel.ToString(),
                    PaymentProvider = x.PaymentProvider.ToString(),
                    x.CheckoutSessionId,
                    x.ExpiresAt,
                    x.PaidAt,
                    x.IsFirstPurchase,
                    PaymentAttemptsCount = paymentCounts.GetValueOrDefault(x.Id),
                    LastPaymentId = lastPayment?.Id,
                    LastPaymentStatus = lastPayment?.Status.ToString(),
                    LastPaymentProvider = lastPayment?.Provider.ToString(),
                    LastPaymentRecheckSupported = recheck.IsSupported,
                    LastPaymentCanRecheck = recheck.CanRecheck,
                    LastPaymentRecheckBlockers = recheck.Blockers,
                    LinkedSubscriptionId = OrderService.GetRenewalSubscriptionId(x),
                    x.CreatedAt,
                    x.UpdatedAt
                };
            })
            .ToList());
    }

    [HttpPost("orders/{id:guid}/recheck-payment")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> RecheckOrderPayment(Guid id, CancellationToken cancellationToken)
    {
        var paymentQuery = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? _db.Payments.FromSqlInterpolated($"""
                SELECT *
                FROM "Payments"
                WHERE "OrderId" = {id}
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT 1
                """)
            : _db.Payments
                .Where(x => x.OrderId == id)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(1);
        var payment = await paymentQuery.AsNoTracking()
            .Include(x => x.PaymentProviderAccount)
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is null)
        {
            return BadRequest(new { error = "У заказа нет платежных попыток для сверки." });
        }

        var readiness = BuildRecheckReadiness(payment);
        if (!readiness.CanRecheck)
        {
            return BadRequest(new { error = "Сверка статуса платежа недоступна.", readiness });
        }

        AddAuditLog(
            "order.payment.recheck",
            nameof(Order),
            id,
            JsonSerializer.Serialize(new
            {
                PaymentId = payment.Id,
                PaymentStatus = payment.Status.ToString(),
                payment.Provider,
                payment.ProviderMode,
                payment.ProviderPaymentId
            }),
            JsonSerializer.Serialize(new { Outcome = "requested", PaymentId = payment.Id }));
        await _db.SaveChangesAsync(cancellationToken);

        var result = await _paymentOrchestrator.RecheckPaymentAsync(payment.Id, cancellationToken);
        return result.IsSuccess
            ? Ok(new AdminPaymentRecheckDto(id, payment.Id, result.Value!.Status.ToString()))
            : BadRequest(BuildPaymentRecheckError(result.IsRetryable));
    }

    [HttpGet("payments")]
    [Authorize(Policy = AdminPolicies.FinanceRead)]
    public async Task<IActionResult> GetPayments(CancellationToken cancellationToken)
    {
        var query = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? _db.Payments.FromSqlRaw("""
                SELECT *
                FROM "Payments"
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT 300
                """)
            : _db.Payments
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(PaymentListLimit);
        var payments = await query.AsNoTracking()
            .Include(x => x.Order)
                .ThenInclude(x => x!.User)
            .Include(x => x.PaymentProviderAccount)
            .Include(x => x.Refunds)
            .ToListAsync(cancellationToken);
        var paymentIds = payments.Select(x => x.Id).ToArray();
        var webhookCounts = await _db.PaymentWebhookEvents.AsNoTracking()
            .Where(x => x.PaymentAttemptId.HasValue && paymentIds.Contains(x.PaymentAttemptId.Value))
            .GroupBy(x => x.PaymentAttemptId!.Value)
            .Select(x => new { PaymentAttemptId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.PaymentAttemptId, x => x.Count, cancellationToken);

        return Ok(payments
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x =>
            {
                var refund = BuildRefundReadiness(x);
                var recheck = BuildRecheckReadiness(x);
                return new
                {
                    x.Id,
                    x.OrderId,
                    UserId = x.Order != null ? x.Order.UserId : (Guid?)null,
                    UserDisplayName = x.Order?.User?.DisplayName ?? string.Empty,
                    Provider = x.Provider.ToString(),
                    x.PaymentProviderAccountId,
                    ProviderMode = x.ProviderMode.ToString(),
                    x.ProviderPaymentId,
                    x.ExternalEventId,
                    x.IdempotencyKey,
                    x.ConfirmationUrl,
                    x.ReturnUrl,
                    x.Amount,
                    x.Currency,
                    Status = x.Status.ToString(),
                    x.SignatureValidated,
                    x.IsActivationProcessed,
                    x.ActivationProcessedAt,
                    x.PaidAt,
                    x.FailedAt,
                    x.RefundedAt,
                    x.RefundedAmount,
                    x.StatusReason,
                    WebhookEventsCount = webhookCounts.GetValueOrDefault(x.Id),
                    RefundsCount = x.Refunds.Count,
                    RecheckSupported = recheck.IsSupported,
                    CanRecheck = recheck.CanRecheck,
                    RecheckBlockers = recheck.Blockers,
                    RefundSupported = refund.IsSupported,
                    CanRefund = refund.CanRefund,
                    RefundableAmount = refund.RefundableAmount,
                    RefundBlockers = refund.Blockers,
                    x.CreatedAt,
                    x.UpdatedAt
                };
            })
            .ToList());
    }

    [HttpPost("payments/{id:guid}/recheck")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> RecheckPayment(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking()
            .Include(x => x.PaymentProviderAccount)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payment is null)
        {
            return BadRequest(new { error = "Платёжная попытка не найдена." });
        }

        var readiness = BuildRecheckReadiness(payment);
        if (!readiness.CanRecheck)
        {
            return BadRequest(new { error = "Сверка статуса платежа недоступна.", readiness });
        }

        AddAuditLog(
            "payment.recheck",
            nameof(PaymentAttempt),
            id,
            JsonSerializer.Serialize(new
            {
                payment.OrderId,
                Status = payment.Status.ToString(),
                payment.Provider,
                payment.ProviderMode,
                payment.ProviderPaymentId
            }),
            JsonSerializer.Serialize(new { Outcome = "requested" }));
        await _db.SaveChangesAsync(cancellationToken);

        var result = await _paymentOrchestrator.RecheckPaymentAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(new AdminPaymentRecheckDto(payment.OrderId, id, result.Value!.Status.ToString()))
            : BadRequest(BuildPaymentRecheckError(result.IsRetryable));
    }

    [HttpPost("payments/{id:guid}/refund")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentHttpRequest request, CancellationToken cancellationToken)
    {
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length > 120)
        {
            return BadRequest(new { error = "Причина возврата не должна превышать 120 символов." });
        }

        var payment = await _db.Payments.AsNoTracking()
            .Include(x => x.PaymentProviderAccount)
            .Include(x => x.Refunds)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payment is null)
        {
            return BadRequest(new { error = "Платёжная попытка не найдена." });
        }

        var readiness = BuildRefundReadiness(payment);
        var isRecoverableRetry = payment.Refunds.Any(x =>
            x.Status is RefundStatus.New or RefundStatus.Unknown
            && x.ProviderRefundId.StartsWith("pending:", StringComparison.OrdinalIgnoreCase)
            && x.Amount == request.Amount
            && string.Equals(x.Reason, x.Reason.Trim(), StringComparison.Ordinal)
            && string.Equals(x.Reason.Trim(), reason, StringComparison.Ordinal)
            && PaymentProviderConfigurationRules.SupportsIdempotentRefundCreateRetry(x.Provider));
        if (!readiness.CanRefund && !isRecoverableRetry)
        {
            return BadRequest(new { error = "Возврат платежа недоступен.", readiness });
        }

        if (request.Amount <= 0 || request.Amount > readiness.RefundableAmount)
        {
            return BadRequest(new { error = "Сумма возврата недопустима.", readiness });
        }

        AddAuditLog(
            "refund.create",
            nameof(PaymentAttempt),
            id,
            JsonSerializer.Serialize(new
            {
                payment.OrderId,
                Status = payment.Status.ToString(),
                payment.Provider,
                payment.ProviderMode,
                payment.ProviderPaymentId,
                payment.RefundedAmount
            }),
            JsonSerializer.Serialize(new
            {
                Outcome = "requested",
                Amount = request.Amount,
                payment.Currency
            }));
        await _db.SaveChangesAsync(cancellationToken);

        var result = await _paymentOrchestrator.RefundPaymentAsync(id, request.Amount, reason, cancellationToken);
        if (!result.IsSuccess)
        {
            var unresolved = await _db.Refunds.AsNoTracking()
                .Where(x => x.PaymentAttemptId == id
                    && x.Amount == request.Amount
                    && x.Reason == reason
                    && (x.Status == RefundStatus.New || x.Status == RefundStatus.Pending || x.Status == RefundStatus.Unknown))
                .FirstOrDefaultAsync(cancellationToken);
            if (unresolved is not null)
            {
                return Accepted(MapRefundDto(unresolved));
            }

            return BadRequest(BuildRefundOperationError(result.IsRetryable));
        }
        return Ok(result.Value);
    }

    [HttpGet("payment-webhook-events")]
    [Authorize(Policy = AdminPolicies.FinanceRead)]
    public async Task<IActionResult> GetPaymentWebhookEvents(CancellationToken cancellationToken)
    {
        var query = _db.PaymentWebhookEvents.AsNoTracking();
        var events = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? await _db.PaymentWebhookEvents
                .FromSqlRaw("SELECT * FROM \"PaymentWebhookEvents\" ORDER BY julianday(\"ReceivedAt\") DESC, julianday(\"CreatedAt\") DESC LIMIT 200")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
            : await query
                .OrderByDescending(x => x.ReceivedAt)
                .ThenByDescending(x => x.CreatedAt)
                .Take(200)
                .ToListAsync(cancellationToken);
        var now = _clock.UtcNow;
        return Ok(events.Select(x => new PaymentWebhookEventDto(
            x.Id,
            x.Provider,
            x.PaymentAttemptId,
            x.PaymentProviderAccountId,
            x.ProviderPaymentId,
            x.ExternalEventId,
            x.EventType,
            x.Status.ToString(),
            x.SignatureValidated,
            x.ReceivedAt,
            x.ProcessedAt,
            PaymentWebhookEventRules.IsRetryable(x.Status, x.ReceivedAt, now),
            PaymentWebhookEventRules.IsTerminal(x.Status),
            PaymentWebhookEventRules.RequiresAttention(x.Status, x.ReceivedAt, now))).ToList());
    }

    [HttpGet("refunds")]
    [Authorize(Policy = AdminPolicies.FinanceRead)]
    public async Task<IActionResult> GetRefunds(CancellationToken cancellationToken)
    {
        var query = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? _db.Refunds.FromSqlRaw("""
                SELECT *
                FROM "Refunds"
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT 300
                """)
            : _db.Refunds
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(RefundListLimit);
        var refunds = await query
            .AsNoTracking()
            .Include(x => x.PaymentAttempt)
                .ThenInclude(x => x!.PaymentProviderAccount)
            .ToListAsync(cancellationToken);
        return Ok(refunds
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x =>
            {
                var readiness = BuildRefundRecheckReadiness(x);
                var retry = BuildRefundRetryReadiness(x);
                return new
                {
                    x.Id,
                    x.PaymentAttemptId,
                    Provider = x.Provider.ToString(),
                    x.ProviderRefundId,
                    Status = x.Status.ToString(),
                    x.Amount,
                    x.Currency,
                    x.Reason,
                    x.CreatedAt,
                    x.RefundedAt,
                    RecheckSupported = readiness.IsSupported,
                    readiness.CanRecheck,
                    RecheckBlockers = readiness.Blockers,
                    RetrySupported = retry.IsSupported,
                    retry.CanRetry,
                    RetryBlockers = retry.Blockers
                };
            })
            .ToList());
    }

    [HttpPost("refunds/{id:guid}/recheck")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> RecheckRefund(Guid id, CancellationToken cancellationToken)
    {
        var before = await _db.Refunds.AsNoTracking()
            .Include(x => x.PaymentAttempt)
                .ThenInclude(x => x!.PaymentProviderAccount)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (before is null)
        {
            return BadRequest(new { error = "Возврат не найден." });
        }

        var readiness = BuildRefundRecheckReadiness(before);
        if (!readiness.CanRecheck)
        {
            return BadRequest(new { error = "Сверка возврата недоступна.", readiness });
        }

        AddAuditLog(
            "refund.recheck",
            nameof(Refund),
            id,
            JsonSerializer.Serialize(new
            {
                Status = before.Status.ToString(),
                before.Provider,
                before.ProviderRefundId,
                before.RefundedAt,
                PaymentStatus = before.PaymentAttempt?.Status.ToString(),
                before.PaymentAttempt?.RefundedAmount
            }),
            JsonSerializer.Serialize(new { Outcome = "requested" }));
        await _db.SaveChangesAsync(cancellationToken);

        var result = await _paymentOrchestrator.RecheckRefundAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            var unresolved = await _db.Refunds.AsNoTracking().FirstAsync(x => x.Id == id, cancellationToken);
            return Accepted(MapRefundDto(unresolved));
        }
        return Ok(result.Value);
    }

    [HttpGet("payment-providers/accounts")]
    [Authorize(Policy = AdminPolicies.FinanceRead)]
    public async Task<IActionResult> GetPaymentProviderAccounts(CancellationToken cancellationToken)
        => Ok(await _paymentProviderAccounts.GetAccountsAsync(cancellationToken));

    [HttpPost("payment-providers/accounts")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> CreatePaymentProviderAccount([FromBody] UpsertPaymentProviderAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await _paymentProviderAccounts.UpsertAsync(null, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        AddAuditLog("payment_provider.create", "PaymentProviderAccount", result.Value!.Id, "{}", SerializeProviderAccountAudit(result.Value));
        AddPaymentProviderSecretRotationAudit(result.Value, request);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpPatch("payment-providers/accounts/{id:guid}")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> UpdatePaymentProviderAccount(Guid id, [FromBody] UpsertPaymentProviderAccountCommand request, CancellationToken cancellationToken)
    {
        var before = await _db.PaymentProviderAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var result = await _paymentProviderAccounts.UpsertAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        AddAuditLog("payment_provider.update", "PaymentProviderAccount", id, SerializeProviderAccountAudit(before), SerializeProviderAccountAudit(result.Value!));
        AddPaymentProviderSecretRotationAudit(result.Value!, request);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("payment-providers/accounts/{id:guid}/enabled")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> SetPaymentProviderAccountEnabled(Guid id, [FromBody] SetProviderEnabledHttpRequest request, CancellationToken cancellationToken)
    {
        var before = await _db.PaymentProviderAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var result = await _paymentProviderAccounts.SetEnabledAsync(id, request.Enabled, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        AddAuditLog("payment_provider.enabled.set", "PaymentProviderAccount", id, SerializeProviderAccountAudit(before), SerializeProviderAccountAudit(result.Value!));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("payment-providers/accounts/{id:guid}/check")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> CheckPaymentProviderAccount(Guid id, CancellationToken cancellationToken)
    {
        var result = await _paymentProviderAccounts.CheckAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        AddAuditLog("payment_provider.check", "PaymentProviderAccount", id, "{}", JsonSerializer.Serialize(new { result.Value!.Provider, result.Value.Mode, result.Value.IsReady, result.Value.CheckScope, result.Value.ConfigurationStatus, result.Value.HealthStatus, result.Value.Message }, JsonOptions));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(result.Value);
    }


    [HttpGet("support/conversations")]
    [Authorize(Policy = AdminPolicies.SupportRead)]
    public async Task<IActionResult> GetSupportConversations(CancellationToken cancellationToken)
    {
        var query = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? _db.SupportConversations.FromSqlRaw("""
                SELECT *
                FROM "SupportConversations"
                ORDER BY julianday("UpdatedAt") DESC, "Id" DESC
                LIMIT 200
                """)
            : _db.SupportConversations
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .Take(SupportConversationListLimit);
        var conversations = await query
            .AsNoTracking()
            .Select(x => new SupportConversationDto(x.Id, x.UserId, x.TelegramUserId, x.Channel, x.Status, x.Subject, x.AssignedToUserId, x.InternalNote, x.Revision, x.ClosedAt, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
        return Ok(conversations
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .ToList());
    }

    [HttpGet("support/conversations/{id:guid}/messages")]
    [Authorize(Policy = AdminPolicies.SupportRead)]
    public async Task<IActionResult> GetSupportMessages(Guid id, CancellationToken cancellationToken)
    {
        var query = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? _db.SupportMessages.FromSqlInterpolated($"""
                SELECT *
                FROM "SupportMessages"
                WHERE "SupportConversationId" = {id}
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT 200
                """)
            : _db.SupportMessages
                .Where(x => x.SupportConversationId == id)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(SupportMessageListLimit);
        var messages = await query
            .AsNoTracking()
            .Select(x => new SupportMessageDto(x.Id, x.SupportConversationId, x.UserId, x.TelegramUserId, x.Direction, x.Text, x.AttachmentsJson, x.IsInternalNote, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(messages
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToList());
    }

    [HttpPost("support/conversations/{id:guid}/reply")]
    [Authorize(Policy = AdminPolicies.SupportWrite)]
    public async Task<IActionResult> ReplySupportConversation(Guid id, [FromBody] AdminSupportReplyHttpRequest request, CancellationToken cancellationToken)
    {
        var conversation = await _db.SupportConversations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (conversation is null)
        {
            return NotFound(new { error = "Support conversation not found." });
        }

        var expectedRevision = request?.Revision;
        if (!expectedRevision.HasValue)
        {
            return BadRequest(new { error = "Support conversation revision is required." });
        }

        if (expectedRevision.Value != conversation.Revision)
        {
            return Conflict(new { error = "Support conversation changed. Reload it and retry.", revision = conversation.Revision });
        }

        var text = NormalizeSupportText(request?.Text, 4000);
        if (text.Length < 2)
        {
            return BadRequest(new { error = "Reply text must contain at least 2 characters." });
        }

        var before = SerializeSupportConversationAudit(conversation);
        var message = new SupportMessage
        {
            SupportConversationId = id,
            UserId = ResolveUserId(),
            TelegramUserId = conversation.TelegramUserId,
            Direction = "outbound",
            Text = text,
            AttachmentsJson = "[]"
        };
        _db.SupportMessages.Add(message);

        var notificationQueued = false;
        if (conversation.TelegramUserId.HasValue)
        {
            var payloadJson = JsonSerializer.Serialize(new { conversationId = id, text }, JsonOptions);
            var exists = await _db.TelegramBotNotifications.AsNoTracking()
                .AnyAsync(x => x.TelegramUserId == conversation.TelegramUserId.Value && x.Type == "support_reply" && x.PayloadJson == payloadJson && x.Status != "failed" && x.Status != "cancelled", cancellationToken);
            if (!exists)
            {
                _db.TelegramBotNotifications.Add(new TelegramBotNotification
                {
                    TelegramUserId = conversation.TelegramUserId.Value,
                    Type = "support_reply",
                    PayloadJson = payloadJson,
                    Status = "pending",
                    NextAttemptAt = _clock.UtcNow
                });
                notificationQueued = true;
            }
        }

        conversation.Status = conversation.Status == "closed" ? "open" : conversation.Status;
        conversation.ClosedAt = conversation.Status == "closed" ? conversation.ClosedAt : null;
        conversation.Revision = checked(conversation.Revision + 1);
        conversation.UpdatedAt = _clock.UtcNow;
        AddAuditLog(
            "support.reply",
            "SupportConversation",
            conversation.Id,
            before,
            JsonSerializer.Serialize(new { conversation.Status, messageId = message.Id, notificationQueued }));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Support conversation changed. Reload it and retry." });
        }
        return Ok(new
        {
            conversationId = id,
            status = conversation.TelegramUserId.HasValue
                ? notificationQueued ? "queued" : "already_queued"
                : "saved",
            conversation.Revision
        });
    }


    [HttpPatch("support/conversations/{id:guid}/status")]
    [Authorize(Policy = AdminPolicies.SupportWrite)]
    public async Task<IActionResult> UpdateSupportConversationStatus(Guid id, [FromBody] AdminSupportStatusHttpRequest request, CancellationToken cancellationToken)
    {
        var conversation = await _db.SupportConversations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (conversation is null)
        {
            return NotFound(new { error = "Support conversation not found." });
        }

        var expectedRevision = request?.Revision;
        if (!expectedRevision.HasValue)
        {
            return BadRequest(new { error = "Support conversation revision is required." });
        }

        if (expectedRevision.Value != conversation.Revision)
        {
            return Conflict(new { error = "Support conversation changed. Reload it and retry.", revision = conversation.Revision });
        }

        var status = string.IsNullOrWhiteSpace(request?.Status) ? conversation.Status : request.Status.Trim().ToLowerInvariant();
        if (status is not ("open" or "pending" or "closed"))
        {
            return BadRequest(new { error = "Status must be open, pending or closed." });
        }

        var assignedToUserId = request?.AssignedToUserId;
        if (assignedToUserId.HasValue)
        {
            var assignedUser = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == assignedToUserId.Value, cancellationToken);
            if (assignedUser is null
                || assignedUser.IsBlocked
                || assignedUser.Status != UserStatus.Active
                || !AdminPolicies.HasAccess(UserRoles.Parse(assignedUser.RolesCsv), AdminPolicies.SupportWrite))
            {
                return BadRequest(new { error = "Assigned support agent must be an active user with support write access." });
            }
        }

        var before = SerializeSupportConversationAudit(conversation);
        conversation.Status = status;
        conversation.AssignedToUserId = assignedToUserId ?? conversation.AssignedToUserId;
        conversation.ClosedAt = status == "closed" ? _clock.UtcNow : null;
        conversation.Revision = checked(conversation.Revision + 1);
        conversation.UpdatedAt = _clock.UtcNow;
        AddAuditLog("support.status.update", "SupportConversation", conversation.Id, before, SerializeSupportConversationAudit(conversation));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Support conversation changed. Reload it and retry." });
        }
        return Ok(new { conversationId = id, conversation.Status, conversation.AssignedToUserId, conversation.Revision });
    }

    [HttpPost("support/conversations/{id:guid}/notes")]
    [Authorize(Policy = AdminPolicies.SupportWrite)]
    public async Task<IActionResult> AddSupportInternalNote(Guid id, [FromBody] AdminSupportNoteHttpRequest request, CancellationToken cancellationToken)
    {
        var conversation = await _db.SupportConversations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (conversation is null)
        {
            return NotFound(new { error = "Support conversation not found." });
        }

        var expectedRevision = request?.Revision;
        if (!expectedRevision.HasValue)
        {
            return BadRequest(new { error = "Support conversation revision is required." });
        }

        if (expectedRevision.Value != conversation.Revision)
        {
            return Conflict(new { error = "Support conversation changed. Reload it and retry.", revision = conversation.Revision });
        }

        var text = NormalizeSupportText(request?.Text, 4000);
        if (text.Length < 2)
        {
            return BadRequest(new { error = "Note text must contain at least 2 characters." });
        }

        var before = SerializeSupportConversationAudit(conversation);
        var adminUserId = ResolveUserId();
        var note = new SupportMessage
        {
            SupportConversationId = id,
            UserId = adminUserId,
            Direction = "internal",
            Text = text,
            IsInternalNote = true,
            AttachmentsJson = "[]"
        };
        _db.SupportMessages.Add(note);
        conversation.InternalNote = text;
        conversation.Revision = checked(conversation.Revision + 1);
        conversation.UpdatedAt = _clock.UtcNow;
        AddAuditLog(
            "support.note.add",
            "SupportConversation",
            conversation.Id,
            before,
            JsonSerializer.Serialize(new { conversation.Status, noteId = note.Id, hasInternalNote = true }));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Support conversation changed. Reload it and retry." });
        }
        return Ok(new SupportMessageDto(note.Id, note.SupportConversationId, note.UserId, note.TelegramUserId, note.Direction, note.Text, note.AttachmentsJson, note.IsInternalNote, note.CreatedAt));
    }

    [HttpGet("servers")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetServers(CancellationToken cancellationToken)
    {
        var nodes = await _db.VpnNodes.AsNoTracking()
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Name)
            .Take(ServerListLimit)
            .ToListAsync(cancellationToken);
        var nodeIds = nodes.Select(x => x.Id).ToList();
        var latestChecks = _db is DbContext dbContext && nodeIds.Count > 0 && (dbContext.Database.IsSqlite() || dbContext.Database.IsNpgsql())
            ? await LoadLatestNodeHealthChecksAsync(dbContext, nodeIds, cancellationToken)
            : await _db.NodeHealthChecks.AsNoTracking()
                .Where(x => nodeIds.Contains(x.NodeId))
                .OrderByDescending(x => x.CheckedAt)
                .ThenByDescending(x => x.Id)
                .Take(ServerHealthDiagnosticsLimit)
                .ToListAsync(cancellationToken);
        var latestByNode = latestChecks
            .GroupBy(x => x.NodeId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(check => check.CheckedAt).First());

        return Ok(nodes.Select(node => MapVpnNode(node, latestByNode.GetValueOrDefault(node.Id))).ToList());
    }

    [HttpPost("servers")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> AddServer([FromBody] CreateServerHttpRequest request, CancellationToken cancellationToken)
    {
        var payloadError = ValidateServerPayload(request);
        if (payloadError is not null)
        {
            return BadRequest(new { error = payloadError });
        }

        if (RequiresServerSecretProtection(request) && _secretProtector is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Server secret protection service is unavailable." });
        }

        var host = ProvisioningService.NormalizeHost(string.IsNullOrWhiteSpace(request.Host) ? request.IpAddress : request.Host);
        if (string.IsNullOrWhiteSpace(host) || !ProvisioningService.IsValidHost(host))
        {
            return BadRequest(new { error = "Invalid server host/IP." });
        }

        if (request.SshPort <= 0 || request.SshPort > 65535)
        {
            return BadRequest(new { error = "SSH port must be between 1 and 65535." });
        }

        if (!TryNormalizeServerPanelBaseUrl(request.PanelBaseUrl, out var panelBaseUrl, out var panelBaseUrlError))
        {
            return BadRequest(new { error = panelBaseUrlError });
        }

        if (request.NodeGroupId.HasValue
            && !await _db.NodeGroups.AsNoTracking().AnyAsync(x => x.Id == request.NodeGroupId.Value, cancellationToken))
        {
            return BadRequest(new { error = "VPN node group does not exist." });
        }

        var authMethod = ProvisioningService.NormalizeAuthMethod(request.SshAuthMethod ?? (string.IsNullOrWhiteSpace(request.SshCredential) ? "ssh_key" : "ssh_key"));
        if (!string.IsNullOrWhiteSpace(request.SshCredential) && authMethod != "password" && authMethod != "ssh_key")
        {
            return BadRequest(new { error = "Unsupported SSH auth method." });
        }

        var owner = string.IsNullOrWhiteSpace(request.OwnerType) ? "admin" : request.OwnerType.Trim().ToLowerInvariant();
        var protectedCredential = string.Empty;
        var legacySshKeyPath = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.SshCredential))
        {
            protectedCredential = _secretProtector!.Protect(request.SshCredential.Trim());
        }
        else if (!string.IsNullOrWhiteSpace(request.SshPrivateKeyPath))
        {
            // Compatibility: key path for explicitly approved live operators. Never return this value from API responses.
            legacySshKeyPath = request.SshPrivateKeyPath.Trim();
        }

        var protectedPanelPassword = string.IsNullOrWhiteSpace(request.PanelPassword)
            ? string.Empty
            : _secretProtector!.Protect(request.PanelPassword.Trim());

        var tags = NormalizeServerTags(request.TagsCsv, owner, authMethod, string.IsNullOrWhiteSpace(protectedCredential) ? "missing" : "protected", request.ValidationMode);
        if (tags.Length > 2000)
        {
            return BadRequest(new { error = "Server tagsCsv must not exceed 2000 characters after normalization." });
        }

        var node = new VpnNode
        {
            Name = TrimOrEmpty(request.Name),
            Host = host,
            IpAddress = TrimOrEmpty(request.IpAddress),
            Provider = string.IsNullOrWhiteSpace(request.Provider) ? "admin-vps" : request.Provider.Trim(),
            Region = TrimOrEmpty(request.Region),
            Country = TrimOrEmpty(request.Country),
            Datacenter = TrimOrEmpty(request.Datacenter),
            Capacity = request.Capacity,
            SupportedProtocolsCsv = NormalizeServerProtocols(request.SupportedProtocolsCsv),
            Priority = request.Priority,
            TagsCsv = tags,
            SshUser = string.IsNullOrWhiteSpace(request.SshUser) ? "root" : request.SshUser.Trim(),
            SshPort = request.SshPort,
            SshPrivateKeyPath = legacySshKeyPath,
            ProtectedSshCredential = protectedCredential,
            SshCredentialRef = string.IsNullOrWhiteSpace(protectedCredential) ? string.Empty : $"secretref:ssh:{Guid.NewGuid():N}",
            SkipHostKeyChecking = request.SkipHostKeyChecking,
            PanelBaseUrl = panelBaseUrl,
            PanelUsername = string.IsNullOrWhiteSpace(request.PanelUsername) ? "admin" : request.PanelUsername.Trim(),
            PanelPassword = string.Empty,
            ProtectedPanelPassword = protectedPanelPassword,
            PanelSecretRef = string.IsNullOrWhiteSpace(protectedPanelPassword) ? string.Empty : $"secretref:panel:{Guid.NewGuid():N}",
            PanelInboundId = request.PanelInboundId,
            PublicHostname = NormalizePublicHostname(request.PublicHostname),
            PublicPort = request.PublicPort,
            NodeGroupId = request.NodeGroupId,
            Status = NodeStatus.New,
            HealthStatus = HealthStatus.Unknown,
            ProvisioningStatus = ProvisioningRunStatus.Requested,
            IsAvailableForNewUsers = false
        };

        _db.VpnNodes.Add(node);
        AddAuditLog("server.create", "VpnNode", node.Id, "{}", JsonSerializer.Serialize(new { node.Name, node.Host, node.PanelBaseUrl, PanelPasswordConfigured = ProvisioningService.PanelPasswordConfigured(node), SshCredentialConfigured = ProvisioningService.CredentialsConfigured(node), authMethod, owner, request.ValidationMode }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapVpnNode(node));
    }

    [HttpPut("servers/{id:guid}")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> UpdateServer(Guid id, [FromBody] CreateServerHttpRequest request, CancellationToken cancellationToken)
    {
        var payloadError = ValidateServerPayload(request);
        if (payloadError is not null)
        {
            return BadRequest(new { error = payloadError });
        }

        if (RequiresServerSecretProtection(request) && _secretProtector is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Server secret protection service is unavailable." });
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnNodeStateAsync(id, cancellationToken);
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null)
        {
            return NotFound(new { error = "Server not found." });
        }
        if (!request.Revision.HasValue || request.Revision.Value < 0)
        {
            return BadRequest(new { error = "Server revision is required and must be a non-negative integer." });
        }
        if (request.Revision.Value != node.Revision)
        {
            return Conflict(new { error = "Server changed. Reload it and retry.", revision = node.Revision });
        }

        var host = ProvisioningService.NormalizeHost(string.IsNullOrWhiteSpace(request.Host) ? request.IpAddress : request.Host);
        if (string.IsNullOrWhiteSpace(host) || !ProvisioningService.IsValidHost(host))
        {
            return BadRequest(new { error = "Invalid server host/IP." });
        }

        if (request.SshPort <= 0 || request.SshPort > 65535)
        {
            return BadRequest(new { error = "SSH port must be between 1 and 65535." });
        }

        if (!TryNormalizeServerPanelBaseUrl(request.PanelBaseUrl, out var panelBaseUrl, out var panelBaseUrlError))
        {
            return BadRequest(new { error = panelBaseUrlError });
        }

        if (request.NodeGroupId.HasValue
            && !await _db.NodeGroups.AsNoTracking().AnyAsync(x => x.Id == request.NodeGroupId.Value, cancellationToken))
        {
            return BadRequest(new { error = "VPN node group does not exist." });
        }

        var authMethod = ProvisioningService.NormalizeAuthMethod(request.SshAuthMethod ?? ProvisioningService.GetSshAuthMethod(node));
        if (!string.IsNullOrWhiteSpace(request.SshCredential) && authMethod != "password" && authMethod != "ssh_key")
        {
            return BadRequest(new { error = "Unsupported SSH auth method." });
        }

        var capacity = request.Capacity;
        if (capacity < node.UsedCapacity)
        {
            return BadRequest(new { error = "Server capacity cannot be lower than used capacity." });
        }

        var owner = string.IsNullOrWhiteSpace(request.OwnerType)
            ? ProvisioningService.ExtractTag(node.TagsCsv, "owner") ?? "admin"
            : request.OwnerType.Trim().ToLowerInvariant();

        var oldSnapshot = new
        {
            node.Name,
            node.Host,
            node.Provider,
            node.Region,
            node.Country,
            node.Datacenter,
            node.Capacity,
            node.Priority,
            node.TagsCsv,
            node.PanelBaseUrl,
            PanelPasswordConfigured = ProvisioningService.PanelPasswordConfigured(node),
            SshCredentialConfigured = ProvisioningService.CredentialsConfigured(node)
        };

        var rotatedSshCredential = !string.IsNullOrWhiteSpace(request.SshCredential);
        var rotatedPanelPassword = !string.IsNullOrWhiteSpace(request.PanelPassword);

        if (rotatedSshCredential)
        {
            var sshCredential = request.SshCredential!.Trim();
            node.ProtectedSshCredential = _secretProtector!.Protect(sshCredential);
            node.SshCredentialRef = $"secretref:ssh:{Guid.NewGuid():N}";
            node.SshPrivateKeyPath = string.Empty;
        }
        else if (!string.IsNullOrWhiteSpace(request.SshPrivateKeyPath))
        {
            node.SshPrivateKeyPath = request.SshPrivateKeyPath.Trim();
            node.ProtectedSshCredential = string.Empty;
            node.SshCredentialRef = string.Empty;
        }

        if (rotatedPanelPassword)
        {
            var panelPassword = request.PanelPassword!.Trim();
            node.ProtectedPanelPassword = _secretProtector!.Protect(panelPassword);
            node.PanelSecretRef = $"secretref:panel:{Guid.NewGuid():N}";
            node.PanelPassword = string.Empty;
        }

        node.Name = TrimOrEmpty(request.Name);
        node.Host = host;
        node.IpAddress = TrimOrEmpty(request.IpAddress);
        node.Provider = string.IsNullOrWhiteSpace(request.Provider) ? "admin-vps" : request.Provider.Trim();
        node.Region = TrimOrEmpty(request.Region);
        node.Country = TrimOrEmpty(request.Country);
        node.Datacenter = TrimOrEmpty(request.Datacenter);
        node.Capacity = capacity;
        node.SupportedProtocolsCsv = NormalizeServerProtocols(request.SupportedProtocolsCsv);
        node.Priority = request.Priority;
        node.SshUser = string.IsNullOrWhiteSpace(request.SshUser) ? "root" : request.SshUser.Trim();
        node.SshPort = request.SshPort;
        node.SkipHostKeyChecking = request.SkipHostKeyChecking;
        node.PanelBaseUrl = panelBaseUrl;
        node.PanelUsername = string.IsNullOrWhiteSpace(request.PanelUsername) ? "admin" : request.PanelUsername.Trim();
        node.PanelInboundId = request.PanelInboundId;
        node.PublicHostname = NormalizePublicHostname(request.PublicHostname);
        node.PublicPort = request.PublicPort;
        node.NodeGroupId = request.NodeGroupId;
        node.TagsCsv = NormalizeServerTags(request.TagsCsv, owner, authMethod, ProvisioningService.CredentialsConfigured(node) ? "protected" : "missing", request.ValidationMode);
        if (node.TagsCsv.Length > 2000)
        {
            return BadRequest(new { error = "Server tagsCsv must not exceed 2000 characters after normalization." });
        }
        node.Revision = checked(node.Revision + 1);
        node.UpdatedAt = _clock.UtcNow;

        AddAuditLog("server.update", "VpnNode", node.Id, JsonSerializer.Serialize(oldSnapshot), JsonSerializer.Serialize(new
        {
            node.Name,
            node.Host,
            node.Provider,
            node.Region,
            node.Country,
            node.Datacenter,
            node.Capacity,
            node.Priority,
            node.TagsCsv,
            node.PanelBaseUrl,
            PanelPasswordConfigured = ProvisioningService.PanelPasswordConfigured(node),
            SshCredentialConfigured = ProvisioningService.CredentialsConfigured(node),
            authMethod,
            owner,
            request.ValidationMode
        }));
        AddServerSecretRotationAudit(node, rotatedSshCredential, rotatedPanelPassword, authMethod);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Server changed. Reload it and retry." });
        }
        return Ok(MapVpnNode(node));
    }

    [HttpPost("servers/{id:guid}/provision")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> Provision(Guid id, [FromBody] QueueProvisionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (request?.Revision is null or < 0)
        {
            return BadRequest(new { error = "Server revision is required." });
        }

        var result = await _provisioningService.QueueAsync(id, request.DryRun, ResolveUserId(), cancellationToken, request.Revision);
        if (!result.IsSuccess)
        {
            return result.IsRetryable ? Conflict(new { error = result.Error }) : BadRequest(new { error = result.Error });
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return Ok(MapProvisioningCommandResult(id, result.Value!, node, "queued"));
    }

    [HttpPost("servers/{id:guid}/precheck")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> Precheck(
        Guid id,
        CancellationToken cancellationToken,
        [FromBody] QueueProvisionHttpRequest? request = null)
    {
        if (request?.Revision is null or < 0)
        {
            return BadRequest(new { error = "Server revision is required." });
        }

        var result = await _provisioningService.QueueAsync(id, true, ResolveUserId(), cancellationToken, request.Revision);
        if (!result.IsSuccess)
        {
            return result.IsRetryable ? Conflict(new { error = result.Error }) : BadRequest(new { error = result.Error });
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return Ok(MapProvisioningCommandResult(id, result.Value!, node, "queued"));
    }

    [HttpPost("servers/{id:guid}/disable")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public Task<IActionResult> DisableServer(
        Guid id,
        [FromBody] ServerStateActionHttpRequest? request,
        CancellationToken cancellationToken)
        => ChangeServerStateAsync(id, request, NodeStatus.Disabled, false, "server.disable", cancellationToken);

    [HttpDelete("servers/{id:guid}")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> DeleteServer(Guid id, [FromQuery] int? revision, CancellationToken cancellationToken)
    {
        await using var gate = await PaymentProcessingGate.AcquireVpnNodeStateAsync(id, cancellationToken);
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null)
        {
            return NotFound(new { error = "Server not found." });
        }
        if (!revision.HasValue || revision.Value < 0)
        {
            return BadRequest(new { error = "Server revision is required and must be a non-negative integer." });
        }
        if (revision.Value != node.Revision)
        {
            return Conflict(new { error = "Server changed. Reload it and retry.", revision = node.Revision });
        }

        var linkedSubscriptions = await _db.Subscriptions.CountAsync(x => x.CurrentServerId == id, cancellationToken);
        var linkedAccesses = await _db.AccessCredentials.CountAsync(x => x.ServerId == id, cancellationToken);
        var linkedRuns = await _db.ProvisioningRuns.CountAsync(x => x.NodeId == id, cancellationToken);
        var linkedHealthChecks = await _db.NodeHealthChecks.CountAsync(x => x.NodeId == id, cancellationToken);
        var linkedMigrationJobs = await _db.MigrationJobs.CountAsync(x => x.SourceNodeId == id || x.TargetNodeId == id, cancellationToken);
        var before = JsonSerializer.Serialize(new
        {
            node.Name,
            node.Host,
            node.Status,
            node.IsAvailableForNewUsers,
            linkedSubscriptions,
            linkedAccesses,
            linkedRuns,
            linkedHealthChecks,
            linkedMigrationJobs
        });

        if (linkedSubscriptions > 0 || linkedAccesses > 0 || linkedRuns > 0 || linkedHealthChecks > 0 || linkedMigrationJobs > 0)
        {
            node.Status = NodeStatus.Archived;
            node.IsAvailableForNewUsers = false;
            node.Revision = checked(node.Revision + 1);
            node.UpdatedAt = _clock.UtcNow;
            AddAuditLog("server.archive", "VpnNode", id, before, JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers, linkedSubscriptions, linkedAccesses, linkedRuns, linkedHealthChecks, linkedMigrationJobs }));
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { error = "Server changed. Reload it and retry." });
            }
            return Ok(new DeleteServerHttpResponse(id, Deleted: false, Archived: true, linkedSubscriptions, linkedAccesses, linkedRuns, linkedHealthChecks, linkedMigrationJobs));
        }

        _db.VpnNodes.Remove(node);
        AddAuditLog("server.delete", "VpnNode", id, before, "{}");
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Server changed. Reload it and retry." });
        }
        return Ok(new DeleteServerHttpResponse(id, Deleted: true, Archived: false, linkedSubscriptions, linkedAccesses, linkedRuns, linkedHealthChecks, linkedMigrationJobs));
    }

    [HttpPost("servers/{id:guid}/health-check")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> CheckServerHealth(Guid id, CancellationToken cancellationToken)
    {
        await using var gate = await PaymentProcessingGate.AcquireVpnNodeStateAsync(id, cancellationToken);
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null)
        {
            return NotFound(new { error = "Server not found." });
        }

        var stopwatch = Stopwatch.StartNew();
        var status = HealthStatus.Unknown;
        var errorText = string.Empty;
        var reason = "Проверка выполнена.";

        if (node.Status is NodeStatus.Archived or NodeStatus.Disabled)
        {
            status = HealthStatus.Unhealthy;
            reason = node.Status == NodeStatus.Archived ? "Сервер архивирован." : "Сервер отключен.";
            errorText = reason;
        }
        else if (node.Status is NodeStatus.Maintenance or NodeStatus.Draining)
        {
            status = HealthStatus.Degraded;
            reason = node.Status == NodeStatus.Maintenance ? "Сервер в обслуживании." : "Набор новых пользователей закрыт.";
            errorText = reason;
        }
        else
        {
            try
            {
                var providerName = string.IsNullOrWhiteSpace(node.Provider) ? "x3ui" : node.Provider.Trim();
                if (_vpnProviderFactory is null)
                {
                    throw new InvalidOperationException("VPN provider factory is not configured.");
                }

                status = await _vpnProviderFactory.Get(providerName).GetNodeHealthAsync(node, cancellationToken);
                reason = status == HealthStatus.Healthy ? "VPN-сервер отвечает." : "VPN-сервер не прошел проверку провайдера.";
                errorText = status == HealthStatus.Healthy ? string.Empty : reason;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                status = HealthStatus.Unhealthy;
                reason = "Проверка VPN-сервера завершилась ошибкой.";
                errorText = $"Проверка провайдера завершилась ошибкой: {ex.GetType().Name}.";
            }
        }

        stopwatch.Stop();
        var check = new NodeHealthCheck
        {
            NodeId = node.Id,
            CheckedAt = _clock.UtcNow,
            Status = status,
            LatencyMs = Math.Max(0, stopwatch.ElapsedMilliseconds),
            MetadataJson = JsonSerializer.Serialize(new
            {
                node.Name,
                node.Host,
                node.Provider,
                NodeStatus = node.Status.ToString(),
                Reason = reason
            }, JsonOptions),
            ErrorText = errorText
        };

        _db.NodeHealthChecks.Add(check);
        var before = JsonSerializer.Serialize(new { node.HealthStatus, node.LastHealthCheckAt }, JsonOptions);
        node.HealthStatus = status;
        node.LastHealthCheckAt = check.CheckedAt;
        node.UpdatedAt = _clock.UtcNow;
        AddAuditLog("server.health-check", "VpnNode", node.Id, before, JsonSerializer.Serialize(new { node.HealthStatus, node.LastHealthCheckAt, check.ErrorText }, JsonOptions));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapNodeHealthCheck(check));
    }

    [HttpGet("servers/{id:guid}/health-checks")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetServerHealthChecks(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _db.VpnNodes.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            return NotFound(new { error = "Server not found." });
        }

        var checks = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? await _db.NodeHealthChecks
                .FromSqlInterpolated($"SELECT * FROM \"NodeHealthChecks\" WHERE \"NodeId\" = {id} ORDER BY julianday(\"CheckedAt\") DESC, \"Id\" DESC LIMIT 20")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
            : await _db.NodeHealthChecks.AsNoTracking()
                .Where(x => x.NodeId == id)
                .OrderByDescending(x => x.CheckedAt)
                .ThenByDescending(x => x.Id)
                .Take(20)
                .ToListAsync(cancellationToken);
        return Ok(checks.Select(MapNodeHealthCheck).ToList());
    }

    [HttpGet("provisioning-runs")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetProvisioningRuns(CancellationToken cancellationToken)
    {
        var runs = _db is DbContext dbContext && dbContext.Database.IsSqlite()
            ? await _db.ProvisioningRuns.FromSqlRaw("""
                SELECT *
                FROM "ProvisioningRuns"
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT 200
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await _db.ProvisioningRuns.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(200)
                .ToListAsync(cancellationToken);
        var nodeIds = runs.Select(x => x.NodeId).Distinct().ToList();
        var runIds = runs.Select(x => x.Id).ToList();
        var nodes = await _db.VpnNodes.AsNoTracking().Where(x => nodeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        List<ProvisioningStepRun> precheckReports;
        if (runIds.Count == 0)
        {
            precheckReports = [];
        }
        else if (_db is DbContext precheckDbContext
                 && (precheckDbContext.Database.IsSqlite() || precheckDbContext.Database.IsNpgsql()))
        {
            precheckReports = await LoadLatestProvisioningPrecheckReportsAsync(precheckDbContext, runIds, cancellationToken);
        }
        else
        {
            precheckReports = await _db.ProvisioningStepRuns.AsNoTracking()
                .Where(x => runIds.Contains(x.ProvisioningRunId) && x.StepName == PrecheckReportStepName)
                .GroupBy(x => x.ProvisioningRunId)
                .Select(group => group
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .First())
                .ToListAsync(cancellationToken);
        }
        var precheckReportByRunId = precheckReports
            .ToDictionary(x => x.ProvisioningRunId, x => RedactSensitiveText(x.Output, 4000));
        return Ok(runs.Select(x =>
        {
            nodes.TryGetValue(x.NodeId, out var node);
            var mode = ProvisioningService.DescribeProvisioningMode(node, x.DryRun);
            var deployMode = ProvisioningService.DescribeProvisioningMode(node, dryRun: false);
            precheckReportByRunId.TryGetValue(x.Id, out var precheckReportPreview);
            return new
            {
                x.Id,
                x.NodeId,
                x.Revision,
                NodeName = node?.Name ?? string.Empty,
                TargetHost = node?.Host ?? node?.IpAddress ?? string.Empty,
                SshPort = node?.SshPort ?? 0,
                Username = node?.SshUser ?? string.Empty,
                AuthMethod = node is null ? string.Empty : ProvisioningService.GetSshAuthMethod(node),
                CredentialsConfigured = node is not null && ProvisioningService.CredentialsConfigured(node),
                Source = node is null ? string.Empty : ProvisioningService.ExtractTag(node.TagsCsv, "source") ?? string.Empty,
                Owner = node is null ? string.Empty : ProvisioningService.ExtractTag(node.TagsCsv, "owner") ?? string.Empty,
                ValidationMode = node is not null && ProvisioningService.IsValidationNode(node),
                Mode = mode.Mode,
                ModeTitle = mode.Title,
                RiskLevel = mode.RiskLevel,
                mode.LiveDeployAllowed,
                mode.NextAction,
                mode.OperatorWarning,
                DeployMode = deployMode.Mode,
                DeployModeTitle = deployMode.Title,
                DeployRiskLevel = deployMode.RiskLevel,
                DeployLiveDeployAllowed = deployMode.LiveDeployAllowed,
                DeployNextAction = deployMode.NextAction,
                DeployOperatorWarning = deployMode.OperatorWarning,
                Status = x.Status.ToString(),
                CurrentStep = ResolveCurrentProvisioningStep(x.Status),
                x.RequestedByUserId,
                x.DryRun,
                x.AttemptCount,
                x.ProcessingStartedAt,
                x.LeaseExpiresAt,
                LastError = RedactSensitiveText(x.LastError, 1000),
                x.StartedAt,
                x.FinishedAt,
                ErrorSummary = IsProvisioningFailure(x.Status) ? RedactSensitiveText(x.ExecutionLog, 1000) : string.Empty,
                ExecutionLogPreview = RedactSensitiveText(x.ExecutionLog, 2000),
                ExecutionLog = RedactSensitiveText(x.ExecutionLog, 2000),
                PrecheckReportPreview = precheckReportPreview ?? string.Empty,
                x.CreatedAt,
                x.UpdatedAt
            };
        }).ToList());
    }

    [HttpGet("provisioning-runs/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetProvisioningRun(Guid id, CancellationToken cancellationToken)
    {
        var run = await _db.ProvisioningRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (run is null) return NotFound();

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == run.NodeId, cancellationToken);
        var steps = _db is DbContext detailDbContext && detailDbContext.Database.IsSqlite()
            ? await _db.ProvisioningStepRuns.FromSqlInterpolated($$"""
                SELECT *
                FROM "ProvisioningStepRuns"
                WHERE "ProvisioningRunId" = {{id}}
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT 500
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await _db.ProvisioningStepRuns.AsNoTracking()
                .Where(x => x.ProvisioningRunId == id)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(500)
                .ToListAsync(cancellationToken);
        steps = steps.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToList();

        AccessCredential? access = null;
        if (node is not null)
        {
            access = _db is DbContext accessDbContext && accessDbContext.Database.IsSqlite()
                ? await _db.AccessCredentials.FromSqlInterpolated($$"""
                    SELECT *
                    FROM "AccessCredentials"
                    WHERE "ServerId" = {{node.Id}}
                    ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                    LIMIT 1
                    """).AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                : await _db.AccessCredentials.AsNoTracking()
                    .Where(x => x.ServerId == node.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
        }

        var mode = ProvisioningService.DescribeProvisioningMode(node, run.DryRun);
        var deployMode = ProvisioningService.DescribeProvisioningMode(node, dryRun: false);
        var precheckReport = steps.LastOrDefault(x => x.StepName == PrecheckReportStepName)?.Output;
        return Ok(new
        {
            Run = new
            {
                run.Id,
                run.NodeId,
                run.Revision,
                NodeName = node?.Name ?? string.Empty,
                TargetHost = node?.Host ?? node?.IpAddress ?? string.Empty,
                SshPort = node?.SshPort ?? 0,
                Username = node?.SshUser ?? string.Empty,
                AuthMethod = node is null ? string.Empty : ProvisioningService.GetSshAuthMethod(node),
                CredentialsConfigured = node is not null && ProvisioningService.CredentialsConfigured(node),
                Source = node is null ? string.Empty : ProvisioningService.ExtractTag(node.TagsCsv, "source") ?? string.Empty,
                Owner = node is null ? string.Empty : ProvisioningService.ExtractTag(node.TagsCsv, "owner") ?? string.Empty,
                ValidationMode = node is not null && ProvisioningService.IsValidationNode(node),
                Mode = mode.Mode,
                ModeTitle = mode.Title,
                RiskLevel = mode.RiskLevel,
                mode.LiveDeployAllowed,
                mode.NextAction,
                mode.OperatorWarning,
                DeployMode = deployMode.Mode,
                DeployModeTitle = deployMode.Title,
                DeployRiskLevel = deployMode.RiskLevel,
                DeployLiveDeployAllowed = deployMode.LiveDeployAllowed,
                DeployNextAction = deployMode.NextAction,
                DeployOperatorWarning = deployMode.OperatorWarning,
                Status = run.Status.ToString(),
                CurrentStep = ResolveCurrentProvisioningStep(run.Status),
                run.RequestedByUserId,
                run.DryRun,
                run.AttemptCount,
                run.ProcessingStartedAt,
                run.LeaseExpiresAt,
                LastError = RedactSensitiveText(run.LastError, 1000),
                run.StartedAt,
                run.FinishedAt,
                ErrorSummary = IsProvisioningFailure(run.Status) ? RedactSensitiveText(run.ExecutionLog, 1000) : string.Empty,
                ExecutionLog = RedactSensitiveText(run.ExecutionLog, 8000),
                PrecheckReport = RedactSensitiveText(precheckReport, 8000),
                LinkedAccessId = access?.Id,
                run.CreatedAt,
                run.UpdatedAt
            },
            Steps = steps.Select(step => new
            {
                step.Id,
                step.ProvisioningRunId,
                step.StepName,
                Status = step.Status.ToString(),
                step.StartedAt,
                step.FinishedAt,
                Output = RedactSensitiveText(step.Output, 4000),
                ErrorText = RedactSensitiveText(step.ErrorText, 4000),
                step.CreatedAt,
                step.UpdatedAt
            })
        });
    }

    [HttpPost("provisioning-runs/{id:guid}/retry")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> RetryProvisioningRun(
        Guid id,
        CancellationToken cancellationToken,
        [FromBody] ProvisioningRunActionHttpRequest? request = null)
    {
        if (request?.Revision is null or < 0) return BadRequest(new { error = "Provisioning run revision is required." });
        var result = await _provisioningService.RetryAsync(id, ResolveUserId(), cancellationToken, request.Revision);
        if (!result.IsSuccess)
        {
            return result.IsRetryable ? Conflict(new { error = result.Error }) : BadRequest(new { error = result.Error });
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == result.Value!.NodeId, cancellationToken);
        return Ok(MapProvisioningCommandResult(null, result.Value!, node, result.Value!.Status.ToString()));
    }

    [HttpPost("provisioning-runs/{id:guid}/deploy")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> DeployProvisioningRun(
        Guid id,
        CancellationToken cancellationToken,
        [FromBody] ProvisioningRunActionHttpRequest? request = null)
    {
        if (request?.Revision is null or < 0) return BadRequest(new { error = "Provisioning run revision is required." });
        var result = await _provisioningService.QueueDeployAsync(id, ResolveUserId(), cancellationToken, request.Revision);
        if (!result.IsSuccess)
        {
            return result.IsRetryable ? Conflict(new { error = result.Error }) : BadRequest(new { error = result.Error });
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == result.Value!.NodeId, cancellationToken);
        return Ok(MapProvisioningCommandResult(null, result.Value!, node, result.Value!.Status.ToString()));
    }

    [HttpPost("provisioning-runs/{id:guid}/cancel")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> CancelProvisioningRun(
        Guid id,
        CancellationToken cancellationToken,
        [FromBody] ProvisioningRunActionHttpRequest? request = null)
    {
        if (request?.Revision is null or < 0) return BadRequest(new { error = "Provisioning run revision is required." });
        var result = await _provisioningService.CancelAsync(id, ResolveUserId(), cancellationToken, request.Revision);
        if (result.IsSuccess)
        {
            return Ok(new { runId = id, status = result.Value });
        }

        return result.IsRetryable
            ? Conflict(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("provisioning-runs/{id:guid}/support-needed")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> MarkProvisioningSupportNeeded(
        Guid id,
        CancellationToken cancellationToken,
        [FromBody] ProvisioningRunActionHttpRequest? request = null)
    {
        if (request?.Revision is null or < 0) return BadRequest(new { error = "Provisioning run revision is required." });
        var result = await _provisioningService.MarkSupportNeededAsync(id, ResolveUserId(), cancellationToken, request.Revision);
        if (result.IsSuccess) return Ok(new { runId = id, supportConversationId = result.Value });
        return result.IsRetryable ? Conflict(new { error = result.Error }) : BadRequest(new { error = result.Error });
    }

    [HttpPost("servers/{id:guid}/maintenance")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public Task<IActionResult> Maintenance(
        Guid id,
        [FromBody] ServerStateActionHttpRequest? request,
        CancellationToken cancellationToken)
        => ChangeServerStateAsync(id, request, NodeStatus.Maintenance, false, "server.maintenance.enable", cancellationToken);

    [HttpPost("servers/{id:guid}/disable-maintenance")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public Task<IActionResult> DisableMaintenance(
        Guid id,
        [FromBody] ServerStateActionHttpRequest? request,
        CancellationToken cancellationToken)
        => ChangeServerStateAsync(id, request, NodeStatus.Ready, true, "server.maintenance.disable", cancellationToken);

    [HttpPost("servers/{id:guid}/disable-allocation")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public Task<IActionResult> DisableAllocation(
        Guid id,
        [FromBody] ServerStateActionHttpRequest? request,
        CancellationToken cancellationToken)
        => ChangeServerStateAsync(id, request, NodeStatus.Draining, false, "server.allocation.disable", cancellationToken);

    [HttpPost("servers/{id:guid}/enable-allocation")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public Task<IActionResult> EnableAllocation(
        Guid id,
        [FromBody] ServerStateActionHttpRequest? request,
        CancellationToken cancellationToken)
        => ChangeServerStateAsync(id, request, NodeStatus.Ready, true, "server.allocation.enable", cancellationToken);

    private async Task<IActionResult> ChangeServerStateAsync(
        Guid id,
        ServerStateActionHttpRequest? request,
        NodeStatus status,
        bool isAvailableForNewUsers,
        string auditAction,
        CancellationToken cancellationToken)
    {
        if (request?.Revision is null or < 0)
        {
            return BadRequest(new { error = "Server revision is required." });
        }

        await using var gate = await PaymentProcessingGate.AcquireVpnNodeStateAsync(id, cancellationToken);
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null) return NotFound(new { error = "Server not found." });
        if (request.Revision.Value != node.Revision)
        {
            return Conflict(new { error = "Server changed. Reload it and retry.", revision = node.Revision });
        }
        if (node.Status == NodeStatus.Archived) return Conflict(new { error = "Archived server state cannot be changed." });

        var before = JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers });
        node.Status = status;
        node.IsAvailableForNewUsers = isAvailableForNewUsers;
        node.Revision = checked(node.Revision + 1);
        node.UpdatedAt = _clock.UtcNow;
        AddAuditLog(auditAction, "VpnNode", id, before, JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers }));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Server changed. Reload it and retry." });
        }
        return Ok(MapVpnNode(node));
    }

    [HttpGet("tariffs")]
    public async Task<IActionResult> GetTariffs(CancellationToken cancellationToken)
    {
        var tariffs = await _db.Tariffs.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Take(TariffListLimit)
            .ToListAsync(cancellationToken);

        return Ok(tariffs.Select(MapTariffDto).ToList());
    }

    [HttpPost("tariffs")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> CreateTariff([FromBody] TariffCreateRequest request, CancellationToken cancellationToken)
    {
        var tariff = CreateTariffEntity(request);
        var validationError = NormalizeTariff(tariff);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        if (await _db.Tariffs.AnyAsync(x => x.Slug == tariff.Slug, cancellationToken))
        {
            return BadRequest(new { error = "Tariff slug already exists." });
        }

        _db.Tariffs.Add(tariff);
        AddAuditLog("tariff.create", "Tariff", tariff.Id, "{}", JsonSerializer.Serialize(new { tariff.Name, tariff.Price, tariff.Currency, tariff.DurationDays, tariff.IsActive }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapTariffDto(tariff));
    }

    [HttpPatch("tariffs/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> PatchTariff(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var tariff = await _db.Tariffs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tariff is null) return NotFound();

        var payloadError = ValidateTariffPatchPayload(payload);
        if (payloadError is not null)
        {
            return BadRequest(new { error = payloadError });
        }

        var expectedRevision = payload.GetProperty("revision").GetInt32();
        if (expectedRevision != tariff.Revision)
        {
            return Conflict(new { error = "Tariff changed. Reload it and retry.", revision = tariff.Revision });
        }

        var before = JsonSerializer.Serialize(MapTariffDto(tariff));
        var candidate = CloneTariff(tariff);
        ApplyTariffPatch(candidate, payload);

        var validationError = NormalizeTariff(candidate);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        if (await _db.Tariffs.AnyAsync(x => x.Id != id && x.Slug == candidate.Slug, cancellationToken))
        {
            return BadRequest(new { error = "Tariff slug already exists." });
        }

        CopyTariffFields(candidate, tariff);
        tariff.Revision = checked(tariff.Revision + 1);
        tariff.UpdatedAt = _clock.UtcNow;
        AddAuditLog("tariff.update", "Tariff", id, before, JsonSerializer.Serialize(MapTariffDto(tariff)));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Tariff changed. Reload it and retry." });
        }
        return Ok(MapTariffDto(tariff));
    }

    [HttpDelete("tariffs/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> DeleteTariff(Guid id, [FromQuery] int? revision, CancellationToken cancellationToken)
    {
        var tariff = await _db.Tariffs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tariff is null) return NotFound();
        if (!revision.HasValue || revision.Value < 0)
        {
            return BadRequest(new { error = "Tariff revision is required and must be a non-negative integer." });
        }
        if (revision.Value != tariff.Revision)
        {
            return Conflict(new { error = "Tariff changed. Reload it and retry.", revision = tariff.Revision });
        }

        var hasLinkedOrders = await _db.Orders.AnyAsync(x => x.TariffId == id, cancellationToken);
        var hasLinkedSubscriptions = await _db.Subscriptions.AnyAsync(x => x.TariffId == id, cancellationToken);
        if (hasLinkedOrders || hasLinkedSubscriptions)
        {
            var beforeArchive = JsonSerializer.Serialize(MapTariffDto(tariff));
            tariff.IsActive = false;
            tariff.VisibleTo = _clock.UtcNow;
            tariff.Revision = checked(tariff.Revision + 1);
            tariff.UpdatedAt = _clock.UtcNow;
            AddAuditLog("tariff.archive", "Tariff", id, beforeArchive, JsonSerializer.Serialize(MapTariffDto(tariff)));
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { error = "Tariff changed. Reload it and retry." });
            }
            return Ok(new { id, deleted = false, archived = true });
        }

        var before = JsonSerializer.Serialize(MapTariffDto(tariff));
        _db.Tariffs.Remove(tariff);
        AddAuditLog("tariff.delete", "Tariff", id, before, "{}");
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Tariff changed. Reload it and retry." });
        }
        return Ok(new { id, deleted = true });
    }

    [HttpGet("referrals")]
    public async Task<IActionResult> GetReferrals(CancellationToken cancellationToken)
    {
        IQueryable<RewardLedger> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            query = _db.RewardLedgers.FromSqlRaw("""
                SELECT r.*
                FROM "RewardLedgers" AS r
                ORDER BY julianday(r."CreatedAt") DESC, r."Id" DESC
                LIMIT 200
                """);
        }
        else
        {
            query = _db.RewardLedgers
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(200);
        }

        var rewards = await query.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(rewards.Select(MapAdminRewardLedgerDto).ToList());
    }

    [HttpGet("referral-programs")]
    public async Task<IActionResult> GetReferralPrograms(CancellationToken cancellationToken)
    {
        IQueryable<ReferralProgram> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            query = _db.ReferralPrograms.FromSqlRaw("""
                SELECT p.*
                FROM "ReferralPrograms" AS p
                ORDER BY julianday(p."CreatedAt") DESC, p."Id" DESC
                LIMIT 200
                """);
        }
        else
        {
            query = _db.ReferralPrograms
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(200);
        }

        var programs = await query.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(programs.Select(MapReferralProgramDto).ToList());
    }

    [HttpPost("referral-programs")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> CreateReferralProgram([FromBody] ReferralProgramUpsertHttpRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateReferralProgram(request);
        if (!validation.IsSuccess) return BadRequest(new { error = validation.Error });

        var program = new ReferralProgram();
        CopyReferralProgramFields(request, program);
        _db.ReferralPrograms.Add(program);
        AddAuditLog("referral_program.create", "ReferralProgram", program.Id, "{}", SerializeReferralProgramAudit(program));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapReferralProgramDto(program));
    }

    [HttpPatch("referral-programs/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> PatchReferralProgram(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var program = await _db.ReferralPrograms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (program is null) return NotFound();

        if (payload.ValueKind != JsonValueKind.Object)
        {
            return BadRequest(new { error = "Referral program patch must be a JSON object." });
        }

        string? invalidFieldError = null;
        var seenFields = new HashSet<string>(StringComparer.Ordinal);
        var hasMutationField = false;
        foreach (var property in payload.EnumerateObject())
        {
            if (!ReferralProgramPatchFields.Contains(property.Name))
            {
                invalidFieldError = $"Unknown referral program field '{property.Name}'.";
                break;
            }
            if (!seenFields.Add(property.Name))
            {
                invalidFieldError = $"Referral program field '{property.Name}' must not be duplicated.";
                break;
            }
            if (property.Name != "revision") hasMutationField = true;
        }
        if (invalidFieldError is not null)
        {
            return BadRequest(new { error = invalidFieldError });
        }
        if (!hasMutationField)
        {
            return BadRequest(new { error = "Referral program patch must include at least one mutable field." });
        }

        if (!payload.TryGetProperty("revision", out var revisionProperty)
            || revisionProperty.ValueKind != JsonValueKind.Number
            || !revisionProperty.TryGetInt32(out var expectedRevision)
            || expectedRevision < 0)
        {
            return BadRequest(new { error = "Referral program revision is required and must be a non-negative integer." });
        }

        if (expectedRevision != program.Revision)
        {
            return Conflict(new { error = "Referral program changed. Reload it and retry.", revision = program.Revision });
        }

        if (!TryBuildReferralProgramPatch(payload, program, out var request, out var patchError))
        {
            return BadRequest(new { error = patchError });
        }

        var validation = ValidateReferralProgram(request);
        if (!validation.IsSuccess) return BadRequest(new { error = validation.Error });

        var before = SerializeReferralProgramAudit(program);
        CopyReferralProgramFields(request, program);
        program.Revision = checked(program.Revision + 1);
        program.UpdatedAt = _clock.UtcNow;
        AddAuditLog("referral_program.update", "ReferralProgram", program.Id, before, SerializeReferralProgramAudit(program));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Referral program changed. Reload it and retry." });
        }
        return Ok(MapReferralProgramDto(program));
    }


    private static TariffDto MapTariffDto(Tariff tariff)
        => new(
            tariff.Id,
            tariff.Revision,
            tariff.Name,
            tariff.Slug,
            tariff.Description,
            tariff.FullDescription,
            ParseTariffFeatures(tariff.FeaturesJson),
            tariff.FeaturesJson,
            tariff.Badge,
            tariff.DurationDays,
            tariff.Price,
            tariff.Currency,
            tariff.MaxDevices,
            tariff.TrafficLimit,
            tariff.IsTrial,
            tariff.IsActive,
            tariff.SortOrder,
            tariff.VisibleFrom,
            tariff.VisibleTo,
            tariff.TariffType.ToString(),
            tariff.Category,
            tariff.AllowedRegionsCsv,
            tariff.AllowedNodeGroupsCsv,
            tariff.IsReferralEligible,
            tariff.ProvisioningScenario,
            tariff.AfterPaymentText,
            tariff.CreatedAt,
            tariff.UpdatedAt);

    private static Tariff CreateTariffEntity(TariffCreateRequest request)
        => new()
        {
            Name = request.Name ?? string.Empty,
            Slug = request.Slug ?? string.Empty,
            Description = request.Description ?? string.Empty,
            FullDescription = request.FullDescription ?? string.Empty,
            FeaturesJson = request.FeaturesJson ?? "[]",
            Badge = request.Badge ?? string.Empty,
            DurationDays = request.DurationDays,
            Price = request.Price,
            Currency = request.Currency ?? "RUB",
            MaxDevices = request.MaxDevices,
            TrafficLimit = request.TrafficLimit,
            IsTrial = request.IsTrial,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            VisibleFrom = request.VisibleFrom,
            VisibleTo = request.VisibleTo,
            TariffType = request.TariffType,
            Category = request.Category ?? "default",
            AllowedRegionsCsv = request.AllowedRegionsCsv ?? string.Empty,
            AllowedNodeGroupsCsv = request.AllowedNodeGroupsCsv ?? string.Empty,
            IsReferralEligible = request.IsReferralEligible,
            ProvisioningScenario = request.ProvisioningScenario ?? "auto",
            AfterPaymentText = request.AfterPaymentText ?? string.Empty
        };

    private static string? NormalizeTariff(Tariff tariff)
    {
        if (string.IsNullOrWhiteSpace(tariff.Name))
        {
            return "Tariff name is required.";
        }

        if (tariff.Price < 0 || tariff.DurationDays <= 0)
        {
            return "Tariff price must be non-negative and durationDays must be positive.";
        }

        if (tariff.MaxDevices <= 0)
        {
            return "Tariff maxDevices must be positive.";
        }

        if (!Enum.IsDefined(tariff.TariffType))
        {
            return "Tariff tariffType must be a supported tariff type.";
        }

        tariff.Name = tariff.Name.Trim();
        tariff.Slug = Slugify(string.IsNullOrWhiteSpace(tariff.Slug) ? tariff.Name : tariff.Slug);
        tariff.Description = tariff.Description.Trim();
        tariff.FullDescription = tariff.FullDescription.Trim();
        if (!TryNormalizeTariffFeaturesJson(tariff.FeaturesJson, out var normalizedFeaturesJson))
        {
            return "Tariff featuresJson must be a JSON array of strings.";
        }
        tariff.FeaturesJson = normalizedFeaturesJson;
        tariff.Badge = tariff.Badge.Trim();
        tariff.Currency = string.IsNullOrWhiteSpace(tariff.Currency) ? "RUB" : tariff.Currency.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(tariff.Currency, "^[A-Z]{3}$"))
        {
            return "Tariff currency must use a three-letter code, for example RUB, USD or XTR.";
        }

        tariff.Category = string.IsNullOrWhiteSpace(tariff.Category) ? "default" : tariff.Category.Trim();
        tariff.AllowedRegionsCsv = tariff.AllowedRegionsCsv.Trim();
        tariff.AllowedNodeGroupsCsv = tariff.AllowedNodeGroupsCsv.Trim();
        tariff.ProvisioningScenario = string.IsNullOrWhiteSpace(tariff.ProvisioningScenario) ? "auto" : tariff.ProvisioningScenario.Trim();
        tariff.AfterPaymentText = tariff.AfterPaymentText.Trim();

        if (tariff.VisibleFrom is not null && tariff.VisibleTo is not null && tariff.VisibleFrom > tariff.VisibleTo)
        {
            return "Tariff visibleFrom must be earlier than visibleTo.";
        }

        if (tariff.Name.Length > 200) return "Tariff name must not exceed 200 characters.";
        if (tariff.Slug.Length > 160) return "Tariff slug must not exceed 160 characters.";
        if (tariff.Description.Length > 500) return "Tariff description must not exceed 500 characters.";
        if (tariff.FullDescription.Length > 4000) return "Tariff fullDescription must not exceed 4000 characters.";
        if (tariff.FeaturesJson.Length > 4000) return "Tariff featuresJson must not exceed 4000 characters.";
        if (tariff.Badge.Length > 80) return "Tariff badge must not exceed 80 characters.";
        if (tariff.Category.Length > 120) return "Tariff category must not exceed 120 characters.";
        if (tariff.AllowedRegionsCsv.Length > 2000) return "Tariff allowedRegionsCsv must not exceed 2000 characters.";
        if (tariff.AllowedNodeGroupsCsv.Length > 2000) return "Tariff allowedNodeGroupsCsv must not exceed 2000 characters.";
        if (tariff.ProvisioningScenario.Length > 120) return "Tariff provisioningScenario must not exceed 120 characters.";
        if (tariff.AfterPaymentText.Length > 2000) return "Tariff afterPaymentText must not exceed 2000 characters.";

        return null;
    }

    private static string? ValidateTariffPatchPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return "Tariff patch must be a JSON object.";
        }

        var seenFields = new HashSet<string>(StringComparer.Ordinal);
        var hasRevision = false;
        var editableFieldCount = 0;
        foreach (var property in payload.EnumerateObject())
        {
            if (!TariffPatchFields.Contains(property.Name))
            {
                return $"Unknown tariff field '{property.Name}'.";
            }
            if (!seenFields.Add(property.Name))
            {
                return $"Tariff field '{property.Name}' must not be repeated.";
            }

            var value = property.Value;
            switch (property.Name)
            {
                case "revision":
                    if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var revision) || revision < 0)
                    {
                        return "Tariff field 'revision' must be a non-negative integer.";
                    }
                    hasRevision = true;
                    break;
                case "name" or "slug" or "description" or "fullDescription" or "featuresJson" or "badge"
                    or "currency" or "category" or "allowedRegionsCsv" or "allowedNodeGroupsCsv"
                    or "provisioningScenario" or "afterPaymentText":
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        return $"Tariff field '{property.Name}' must be a string.";
                    }
                    editableFieldCount++;
                    break;
                case "price":
                    if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out _))
                    {
                        return "Tariff field 'price' must be a decimal number.";
                    }
                    editableFieldCount++;
                    break;
                case "durationDays" or "maxDevices" or "sortOrder":
                    if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out _))
                    {
                        return $"Tariff field '{property.Name}' must be an integer.";
                    }
                    editableFieldCount++;
                    break;
                case "trafficLimit":
                    if (value.ValueKind != JsonValueKind.Null
                        && (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out _)))
                    {
                        return "Tariff field 'trafficLimit' must be an integer or null.";
                    }
                    editableFieldCount++;
                    break;
                case "isTrial" or "isActive" or "isReferralEligible":
                    if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    {
                        return $"Tariff field '{property.Name}' must be a boolean.";
                    }
                    editableFieldCount++;
                    break;
                case "visibleFrom" or "visibleTo":
                    if (value.ValueKind != JsonValueKind.Null
                        && (value.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(value.GetString(), out _)))
                    {
                        return $"Tariff field '{property.Name}' must be an ISO date-time string or null.";
                    }
                    editableFieldCount++;
                    break;
                case "tariffType":
                    if (value.ValueKind != JsonValueKind.String
                        || !Enum.TryParse<TariffType>(value.GetString(), ignoreCase: true, out var parsedTariffType)
                        || !Enum.IsDefined(parsedTariffType))
                    {
                        return "Tariff field 'tariffType' must be a supported tariff type.";
                    }
                    editableFieldCount++;
                    break;
            }
        }

        if (!hasRevision)
        {
            return "Tariff revision is required and must be a non-negative integer.";
        }
        if (editableFieldCount == 0)
        {
            return "Tariff patch must contain at least one editable field.";
        }

        return null;
    }

    private static Tariff CloneTariff(Tariff source)
        => new()
        {
            Id = source.Id,
            Revision = source.Revision,
            Name = source.Name,
            Slug = source.Slug,
            Description = source.Description,
            FullDescription = source.FullDescription,
            FeaturesJson = source.FeaturesJson,
            Badge = source.Badge,
            DurationDays = source.DurationDays,
            Price = source.Price,
            Currency = source.Currency,
            MaxDevices = source.MaxDevices,
            TrafficLimit = source.TrafficLimit,
            IsTrial = source.IsTrial,
            IsActive = source.IsActive,
            SortOrder = source.SortOrder,
            VisibleFrom = source.VisibleFrom,
            VisibleTo = source.VisibleTo,
            TariffType = source.TariffType,
            Category = source.Category,
            AllowedRegionsCsv = source.AllowedRegionsCsv,
            AllowedNodeGroupsCsv = source.AllowedNodeGroupsCsv,
            IsReferralEligible = source.IsReferralEligible,
            ProvisioningScenario = source.ProvisioningScenario,
            AfterPaymentText = source.AfterPaymentText
        };

    private static void ApplyTariffPatch(Tariff tariff, JsonElement payload)
    {
        if (payload.TryGetProperty("name", out var name)) tariff.Name = name.GetString()?.Trim() ?? tariff.Name;
        if (payload.TryGetProperty("slug", out var slug)) tariff.Slug = slug.GetString()?.Trim() ?? tariff.Slug;
        if (payload.TryGetProperty("description", out var description)) tariff.Description = description.GetString() ?? tariff.Description;
        if (payload.TryGetProperty("fullDescription", out var fullDescription)) tariff.FullDescription = fullDescription.GetString() ?? tariff.FullDescription;
        if (payload.TryGetProperty("featuresJson", out var featuresJson)) tariff.FeaturesJson = featuresJson.GetString() ?? tariff.FeaturesJson;
        if (payload.TryGetProperty("badge", out var badge)) tariff.Badge = badge.GetString() ?? tariff.Badge;
        if (payload.TryGetProperty("price", out var price)) tariff.Price = price.GetDecimal();
        if (payload.TryGetProperty("currency", out var currency)) tariff.Currency = (currency.GetString() ?? tariff.Currency).Trim().ToUpperInvariant();
        if (payload.TryGetProperty("durationDays", out var durationDays)) tariff.DurationDays = durationDays.GetInt32();
        if (payload.TryGetProperty("maxDevices", out var maxDevices)) tariff.MaxDevices = maxDevices.GetInt32();
        if (payload.TryGetProperty("trafficLimit", out var trafficLimit)) tariff.TrafficLimit = trafficLimit.ValueKind == JsonValueKind.Null ? null : trafficLimit.GetInt64();
        if (payload.TryGetProperty("isTrial", out var isTrial)) tariff.IsTrial = isTrial.GetBoolean();
        if (payload.TryGetProperty("isActive", out var isActive)) tariff.IsActive = isActive.GetBoolean();
        if (payload.TryGetProperty("sortOrder", out var sortOrder)) tariff.SortOrder = sortOrder.GetInt32();
        if (payload.TryGetProperty("category", out var category)) tariff.Category = category.GetString() ?? tariff.Category;
        if (payload.TryGetProperty("allowedRegionsCsv", out var regions)) tariff.AllowedRegionsCsv = regions.GetString() ?? tariff.AllowedRegionsCsv;
        if (payload.TryGetProperty("allowedNodeGroupsCsv", out var nodeGroups)) tariff.AllowedNodeGroupsCsv = nodeGroups.GetString() ?? tariff.AllowedNodeGroupsCsv;
        if (payload.TryGetProperty("isReferralEligible", out var referralEligible)) tariff.IsReferralEligible = referralEligible.GetBoolean();
        if (payload.TryGetProperty("provisioningScenario", out var provisioningScenario)) tariff.ProvisioningScenario = provisioningScenario.GetString() ?? tariff.ProvisioningScenario;
        if (payload.TryGetProperty("afterPaymentText", out var afterPaymentText)) tariff.AfterPaymentText = afterPaymentText.GetString() ?? tariff.AfterPaymentText;
        if (payload.TryGetProperty("visibleFrom", out var visibleFrom)) tariff.VisibleFrom = ReadNullableDate(visibleFrom);
        if (payload.TryGetProperty("visibleTo", out var visibleTo)) tariff.VisibleTo = ReadNullableDate(visibleTo);
        if (payload.TryGetProperty("tariffType", out var tariffType)) tariff.TariffType = Enum.Parse<TariffType>(tariffType.GetString()!, ignoreCase: true);
    }

    private static void CopyTariffFields(Tariff source, Tariff target)
    {
        target.Name = source.Name;
        target.Slug = source.Slug;
        target.Description = source.Description;
        target.FullDescription = source.FullDescription;
        target.FeaturesJson = source.FeaturesJson;
        target.Badge = source.Badge;
        target.DurationDays = source.DurationDays;
        target.Price = source.Price;
        target.Currency = source.Currency;
        target.MaxDevices = source.MaxDevices;
        target.TrafficLimit = source.TrafficLimit;
        target.IsTrial = source.IsTrial;
        target.IsActive = source.IsActive;
        target.SortOrder = source.SortOrder;
        target.VisibleFrom = source.VisibleFrom;
        target.VisibleTo = source.VisibleTo;
        target.TariffType = source.TariffType;
        target.Category = source.Category;
        target.AllowedRegionsCsv = source.AllowedRegionsCsv;
        target.AllowedNodeGroupsCsv = source.AllowedNodeGroupsCsv;
        target.IsReferralEligible = source.IsReferralEligible;
        target.ProvisioningScenario = source.ProvisioningScenario;
        target.AfterPaymentText = source.AfterPaymentText;
    }

    private static IReadOnlyList<string> ParseTariffFeatures(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(featuresJson)?
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool TryNormalizeTariffFeaturesJson(string? featuresJson, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            normalized = "[]";
            return true;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<string?>>(featuresJson) ?? [];
            if (items.Any(x => x is null))
            {
                normalized = "[]";
                return false;
            }
            normalized = JsonSerializer.Serialize(items
                .Select(x => x!.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(), JsonOptions);
            return true;
        }
        catch (JsonException)
        {
            normalized = "[]";
            return false;
        }
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"\s+", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9а-яё\-_]+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..12] : slug;
    }

    private static DateTimeOffset? ReadNullableDate(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        var raw = value.GetString();
        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
    }

    private static string ResolveCurrentProvisioningStep(ProvisioningRunStatus status)
        => status switch
        {
            ProvisioningRunStatus.Requested => "requested",
            ProvisioningRunStatus.AwaitingCredentials => "awaiting_credentials",
            ProvisioningRunStatus.AwaitingConfirmation => "awaiting_confirmation",
            ProvisioningRunStatus.PrecheckQueued => "precheck_queued",
            ProvisioningRunStatus.Prechecking => "prechecking",
            ProvisioningRunStatus.PrecheckFailed => "precheck_failed",
            ProvisioningRunStatus.ReadyToDeploy => "ready_to_deploy",
            ProvisioningRunStatus.DeployQueued => "deploy_queued",
            ProvisioningRunStatus.Deploying => "deploying",
            ProvisioningRunStatus.Deployed => "deployed",
            ProvisioningRunStatus.Retrying => "retrying",
            ProvisioningRunStatus.Cancelled => "cancelled",
            ProvisioningRunStatus.Failed => "failed",
            ProvisioningRunStatus.Succeeded => "succeeded",
            ProvisioningRunStatus.Running => "running",
            _ => "pending"
        };

    private static bool IsProvisioningFailure(ProvisioningRunStatus status)
        => status is ProvisioningRunStatus.Failed or ProvisioningRunStatus.PrecheckFailed;

    private static IReadOnlyCollection<OrderStatus>? ParseOrderStatuses(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<OrderStatus>();
        }

        var result = new List<OrderStatus>();
        foreach (var rawValue in status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<OrderStatus>(rawValue, ignoreCase: true, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                return null;
            }

            result.Add(parsed);
        }

        return result.Distinct().ToArray();
    }

    private static string NormalizeSearchText(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static TEnum[] MatchingEnumValues<TEnum>(string searchText)
        where TEnum : struct, Enum
        => Enum.GetValues<TEnum>()
            .Where(value => value.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static string ToSqlEnumList<TEnum>(IEnumerable<TEnum> values)
        where TEnum : struct, Enum
    {
        var list = values.Select(value => Convert.ToInt32(value)).ToArray();
        return list.Length == 0 ? "-1" : string.Join(", ", list);
    }

    private RefundReadinessDto BuildRefundReadiness(PaymentAttempt payment)
    {
        var blockers = new List<string>();
        var refundableAmount = Math.Max(0m, payment.Amount - payment.RefundedAmount);
        var isSupported = PaymentProviderConfigurationRules.SupportsRefund(payment.Provider);

        if (!isSupported)
        {
            blockers.Add("Провайдер не поддерживает возвраты в текущем адаптере.");
        }

        if (payment.Status is not (PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded))
        {
            blockers.Add("Возврат доступен только для успешных или частично возвращенных платежей.");
        }

        if (refundableAmount <= 0)
        {
            blockers.Add("Вся сумма платежа уже возвращена.");
        }

        if (payment.Refunds.Any(x => x.Status is RefundStatus.New or RefundStatus.Pending or RefundStatus.Unknown))
        {
            blockers.Add("Есть незавершенный возврат: сверьте его статус у провайдера перед новой операцией.");
        }

        var account = payment.PaymentProviderAccount;
        blockers.AddRange(PaymentProviderConfigurationRules
            .GetRefundConfigurationIssues(payment, account, _hostEnvironment?.EnvironmentName)
            .Select(x => x.Message));

        return new RefundReadinessDto(isSupported, blockers.Count == 0, refundableAmount, blockers);
    }

    private RecheckReadinessDto BuildRecheckReadiness(PaymentAttempt payment)
    {
        var blockers = new List<string>();
        var isSupported = PaymentProviderConfigurationRules.SupportsManualRecheck(payment.Provider);
        if (!isSupported)
        {
            blockers.Add("Провайдер не поддерживает ручную перепроверку статуса в текущем адаптере.");
        }

        blockers.AddRange(PaymentProviderConfigurationRules
            .GetManualRecheckConfigurationIssues(payment, payment.PaymentProviderAccount, _hostEnvironment?.EnvironmentName)
            .Select(x => x.Message));

        return new RecheckReadinessDto(isSupported, blockers.Count == 0, blockers);
    }

    private RefundRecheckReadinessDto BuildRefundRecheckReadiness(Refund refund)
    {
        var blockers = new List<string>();
        var isSupported = PaymentProviderConfigurationRules.SupportsRefundStatusRecheck(refund.Provider);
        if (!isSupported)
        {
            blockers.Add("Провайдер не поддерживает сверку статуса отдельного возврата.");
        }

        if (refund.Status is not (RefundStatus.New or RefundStatus.Pending or RefundStatus.Unknown))
        {
            blockers.Add("Возврат уже имеет окончательный статус.");
        }

        if (string.IsNullOrWhiteSpace(refund.ProviderRefundId)
            || refund.ProviderRefundId.StartsWith("pending:", StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add("Не сохранён идентификатор возврата у провайдера.");
        }

        if (refund.PaymentAttempt is null)
        {
            blockers.Add("Связанная платёжная попытка недоступна.");
        }
        else
        {
            blockers.AddRange(PaymentProviderConfigurationRules
                .GetRefundConfigurationIssues(refund.PaymentAttempt, refund.PaymentAttempt.PaymentProviderAccount, _hostEnvironment?.EnvironmentName)
                .Select(x => x.Message));
        }

        return new RefundRecheckReadinessDto(isSupported, blockers.Count == 0, blockers);
    }

    private RefundRetryReadinessDto BuildRefundRetryReadiness(Refund refund)
    {
        var blockers = new List<string>();
        var isSupported = PaymentProviderConfigurationRules.SupportsIdempotentRefundCreateRetry(refund.Provider);
        if (!isSupported)
        {
            blockers.Add("Провайдер не гарантирует идемпотентный повтор создания возврата.");
        }

        if (refund.Status is not (RefundStatus.New or RefundStatus.Unknown))
        {
            blockers.Add("Повтор доступен только для возврата с неопределённым результатом создания.");
        }

        if (string.IsNullOrWhiteSpace(refund.ProviderRefundId)
            || !refund.ProviderRefundId.StartsWith("pending:", StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add("Возврат уже имеет идентификатор провайдера и должен сверяться по статусу.");
        }

        if (!string.Equals(refund.Reason, refund.Reason.Trim(), StringComparison.Ordinal))
        {
            blockers.Add("Legacy-возврат с ненормализованной причиной нельзя безопасно повторить автоматически.");
        }

        if (refund.PaymentAttempt is null)
        {
            blockers.Add("Связанная платёжная попытка недоступна.");
        }
        else
        {
            if (refund.PaymentAttempt.Status is not (PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded))
            {
                blockers.Add("Исходный платёж больше не допускает возврат.");
            }

            if (refund.Amount <= 0 || refund.Amount > refund.PaymentAttempt.Amount - refund.PaymentAttempt.RefundedAmount)
            {
                blockers.Add("Сохранённая сумма возврата больше недоступного остатка платежа.");
            }

            blockers.AddRange(PaymentProviderConfigurationRules
                .GetRefundConfigurationIssues(refund.PaymentAttempt, refund.PaymentAttempt.PaymentProviderAccount, _hostEnvironment?.EnvironmentName)
                .Select(x => x.Message));
        }

        return new RefundRetryReadinessDto(isSupported, blockers.Count == 0, blockers);
    }

    private static object BuildPaymentRecheckError(bool retryable)
        => new
        {
            error = retryable
                ? "Не удалось подтвердить статус платежа у провайдера. Повторите попытку позже."
                : "Не удалось выполнить сверку статуса платежа.",
            retryable
        };

    private static object BuildRefundOperationError(bool retryable)
        => new
        {
            error = retryable
                ? "Не удалось подтвердить результат возврата у провайдера. Повторите ту же операцию позже."
                : "Не удалось выполнить возврат платежа.",
            retryable
        };

    private static RefundDto MapRefundDto(Refund refund)
        => new(
            refund.Id,
            refund.PaymentAttemptId,
            refund.Provider,
            refund.ProviderRefundId,
            refund.Status.ToString(),
            refund.Amount,
            refund.Currency,
            refund.Reason,
            refund.CreatedAt,
            refund.RefundedAt);

    private void AddAuditLog(string action, string entityType, Guid entityId, string beforeJson, string afterJson)
    {
        AdminAuditLogWriter.Add(_db, this, action, entityType, entityId, beforeJson, afterJson);
    }

    private static string SerializeReferralProgramAudit(ReferralProgram program)
        => JsonSerializer.Serialize(new
        {
            program.Name,
            program.Revision,
            program.Status,
            program.StartAt,
            program.EndAt,
            program.RuleDefinition,
            program.RewardDefinition,
            program.AntiFraudSettings
        });

    private static Result<bool> ValidateReferralProgram(ReferralProgramUpsertHttpRequest request)
        => ReferralRewardService.ValidateProgramConfiguration(
            request.Name,
            request.Status,
            request.StartAt,
            request.EndAt,
            request.RuleDefinition,
            request.RewardDefinition,
            request.AntiFraudSettings ?? "{}");

    private static void CopyReferralProgramFields(ReferralProgramUpsertHttpRequest request, ReferralProgram program)
    {
        program.Name = request.Name.Trim();
        program.Status = request.Status.Trim().ToLowerInvariant();
        program.StartAt = request.StartAt;
        program.EndAt = request.EndAt;
        program.RuleDefinition = request.RuleDefinition.Trim();
        program.RewardDefinition = request.RewardDefinition.Trim();
        program.AntiFraudSettings = (request.AntiFraudSettings ?? "{}").Trim();
    }

    private static bool TryBuildReferralProgramPatch(
        JsonElement payload,
        ReferralProgram current,
        out ReferralProgramUpsertHttpRequest request,
        out string error)
    {
        request = default!;
        error = string.Empty;
        if (!TryReadReferralString(payload, "name", current.Name, out var name, out error)
            || !TryReadReferralString(payload, "status", current.Status, out var status, out error)
            || !TryReadReferralDate(payload, "startAt", current.StartAt, out var startAt, out error)
            || !TryReadReferralDate(payload, "endAt", current.EndAt, out var endAt, out error)
            || !TryReadReferralJson(payload, "ruleDefinition", current.RuleDefinition, out var rules, out error)
            || !TryReadReferralJson(payload, "rewardDefinition", current.RewardDefinition, out var rewards, out error)
            || !TryReadReferralJson(payload, "antiFraudSettings", current.AntiFraudSettings, out var antiFraud, out error))
        {
            return false;
        }

        request = new ReferralProgramUpsertHttpRequest(name, status, startAt, endAt, rules, rewards, antiFraud);
        return true;
    }

    private static bool TryReadReferralString(
        JsonElement payload,
        string propertyName,
        string fallback,
        out string value,
        out string error)
    {
        value = fallback;
        error = string.Empty;
        if (!payload.TryGetProperty(propertyName, out var property)) return true;
        if (property.ValueKind != JsonValueKind.String)
        {
            error = $"Referral program field '{propertyName}' must be a string.";
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryReadReferralDate(
        JsonElement payload,
        string propertyName,
        DateTimeOffset? fallback,
        out DateTimeOffset? value,
        out string error)
    {
        value = fallback;
        error = string.Empty;
        if (!payload.TryGetProperty(propertyName, out var property)) return true;
        if (property.ValueKind == JsonValueKind.Null)
        {
            value = null;
            return true;
        }
        if (property.ValueKind != JsonValueKind.String || !property.TryGetDateTimeOffset(out var parsed))
        {
            error = $"Referral program field '{propertyName}' must be an ISO date or null.";
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryReadReferralJson(
        JsonElement payload,
        string propertyName,
        string fallback,
        out string value,
        out string error)
    {
        value = fallback;
        error = string.Empty;
        if (!payload.TryGetProperty(propertyName, out var property)) return true;
        if (property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return true;
        }
        if (property.ValueKind == JsonValueKind.Object)
        {
            value = property.GetRawText();
            return true;
        }

        error = $"Referral program field '{propertyName}' must be a JSON object or a serialized JSON object.";
        return false;
    }

    private static ReferralProgramDto MapReferralProgramDto(ReferralProgram program)
        => new(
            program.Id,
            program.Revision,
            program.Name,
            program.Status,
            program.StartAt,
            program.EndAt,
            program.RuleDefinition,
            program.RewardDefinition,
            program.AntiFraudSettings,
            program.CreatedAt,
            program.UpdatedAt);

    private static AdminRewardLedgerDto MapAdminRewardLedgerDto(RewardLedger reward)
        => new(
            reward.Id,
            reward.UserId,
            reward.SourceUserId,
            reward.ReferralProgramId,
            reward.Type,
            reward.Status.ToString(),
            reward.Value,
            reward.CurrencyOrUnit,
            reward.ProcessedAt,
            reward.CreatedAt);

    private static string SerializeSupportConversationAudit(SupportConversation conversation)
        => JsonSerializer.Serialize(new
        {
            conversation.Status,
            conversation.AssignedToUserId,
            hasInternalNote = !string.IsNullOrWhiteSpace(conversation.InternalNote),
            conversation.Revision,
            conversation.ClosedAt
        });

    private void AddPaymentProviderSecretRotationAudit(PaymentProviderAccountDto account, UpsertPaymentProviderAccountCommand request)
    {
        var rotatedSecretKey = !string.IsNullOrWhiteSpace(request.SecretKey);
        var rotatedWebhookSecret = !string.IsNullOrWhiteSpace(request.WebhookSecret);
        if (!rotatedSecretKey && !rotatedWebhookSecret)
        {
            return;
        }

        AddAuditLog(
            "payment_provider.secret.rotate",
            "PaymentProviderAccount",
            account.Id,
            "{}",
            JsonSerializer.Serialize(new
            {
                account.Provider,
                account.Mode,
                account.Name,
                rotatedSecretKey,
                rotatedWebhookSecret
            }, JsonOptions));
    }

    private void AddServerSecretRotationAudit(VpnNode node, bool rotatedSshCredential, bool rotatedPanelPassword, string authMethod)
    {
        if (!rotatedSshCredential && !rotatedPanelPassword)
        {
            return;
        }

        AddAuditLog(
            "server.secret.rotate",
            "VpnNode",
            node.Id,
            "{}",
            JsonSerializer.Serialize(new
            {
                node.Name,
                node.Host,
                rotatedSshCredential,
                rotatedPanelPassword,
                sshAuthMethod = authMethod,
                sshCredentialConfigured = ProvisioningService.CredentialsConfigured(node),
                panelPasswordConfigured = ProvisioningService.PanelPasswordConfigured(node)
            }, JsonOptions));
    }

    private static string SerializeProviderAccountAudit(PaymentProviderAccount? account)
        => account is null ? "{}" : SerializeProviderAccountAudit(PaymentProviderAccountService.MapToDto(account));

    private static string SerializeProviderAccountAudit(PaymentProviderAccountDto account)
        => JsonSerializer.Serialize(new
        {
            account.Id,
            account.Provider,
            account.Mode,
            account.Name,
            account.PublicName,
            account.IsEnabled,
            account.IsDefault,
            account.ShopId,
            account.ApiBaseUrl,
            account.ReturnUrl,
            account.WebhookUrl,
            account.HasSecretKey,
            account.HasWebhookSecret,
            account.UseWebhookIpAllowList,
            account.AllowedWebhookIpRangesCsv,
            account.HealthStatus,
            account.IsCheckoutConfigured,
            account.CheckoutConfigurationIssue
        }, JsonOptions);

    private static string RedactSensitiveText(string? value, int maxLength)
        => SensitiveDataRedactor.Redact(value, maxLength: maxLength);

    private static string NormalizeSupportText(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string NormalizeServerTags(string? tagsCsv, string owner, string authMethod, string credentialsStatus, bool validationMode)
    {
        var systemTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["source"] = "admin",
            ["owner"] = string.IsNullOrWhiteSpace(owner) ? "admin" : owner.Trim().ToLowerInvariant(),
            ["ssh-auth"] = authMethod,
            ["credentials"] = credentialsStatus,
            ["validation-mode"] = validationMode.ToString().ToLowerInvariant(),
            ["autodeploy-after-precheck"] = "false"
        };

        var userTags = (tagsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag =>
            {
                var separator = tag.IndexOf(':', StringComparison.Ordinal);
                var key = separator > 0 ? tag[..separator].Trim() : tag.Trim();
                return !systemTags.ContainsKey(key);
            });

        return string.Join(',', userTags.Concat(systemTags.Select(tag => $"{tag.Key}:{tag.Value}")));
    }

    private static string? ValidateServerPayload(CreateServerHttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Server name is required.";
        }

        if (request.Capacity <= 0)
        {
            return "Server capacity must be greater than zero.";
        }

        if (request.Priority <= 0)
        {
            return "Server priority must be greater than zero.";
        }

        if (request.PublicPort <= 0 || request.PublicPort > 65535)
        {
            return "Public port must be between 1 and 65535.";
        }

        if (request.PanelInboundId is <= 0)
        {
            return "Panel inbound ID must be greater than zero.";
        }

        if (!string.IsNullOrWhiteSpace(request.SupportedProtocolsCsv)
            && !VpnProtocolPolicy.TryNormalizeCsv(request.SupportedProtocolsCsv, out _))
        {
            return "Supported protocols must contain only vless, vmess or trojan CSV tokens.";
        }

        if (!string.IsNullOrWhiteSpace(request.PublicHostname)
            && !ProvisioningService.IsValidHost(NormalizePublicHostname(request.PublicHostname)))
        {
            return "Public hostname must be a valid DNS name, IPv4 or IPv6 address.";
        }

        if (!string.IsNullOrWhiteSpace(request.IpAddress)
            && !ProvisioningService.IsValidIpAddress(request.IpAddress))
        {
            return "IP address must be a valid IPv4 or IPv6 address without whitespace.";
        }

        if (!string.IsNullOrWhiteSpace(request.SshUser)
            && !ProvisioningService.IsValidSshUsername(request.SshUser))
        {
            return "SSH username is invalid. Use letters, digits, dot, underscore, @ or hyphen without whitespace.";
        }

        if (!string.IsNullOrWhiteSpace(request.SshPrivateKeyPath)
            && !ProvisioningService.IsSafeLegacySshPrivateKeyPath(request.SshPrivateKeyPath))
        {
            return "SSH private key path must be an absolute Unix filesystem path without whitespace, control or quote characters. Submit key material through the protected SSH credential field.";
        }

        if (TrimOrEmpty(request.Name).Length > 200) return "Server name must not exceed 200 characters.";
        if (TrimOrEmpty(request.Host).Length > 253) return "Server host must not exceed 253 characters.";
        if (TrimOrEmpty(request.IpAddress).Length > 64) return "Server ipAddress must not exceed 64 characters.";
        if (TrimOrEmpty(request.Provider).Length > 120) return "Server provider must not exceed 120 characters.";
        if (TrimOrEmpty(request.Region).Length > 120) return "Server region must not exceed 120 characters.";
        if (TrimOrEmpty(request.Country).Length > 80) return "Server country must not exceed 80 characters.";
        if (TrimOrEmpty(request.Datacenter).Length > 120) return "Server datacenter must not exceed 120 characters.";
        if (TrimOrEmpty(request.SupportedProtocolsCsv).Length > 80) return "Server supportedProtocolsCsv must not exceed 80 characters.";
        if (TrimOrEmpty(request.TagsCsv).Length > 2000) return "Server tagsCsv must not exceed 2000 characters.";
        if (TrimOrEmpty(request.SshUser).Length > 64) return "Server sshUser must not exceed 64 characters.";
        if (TrimOrEmpty(request.SshPrivateKeyPath).Length > 4000) return "Server sshPrivateKeyPath must not exceed 4000 characters.";
        if (TrimOrEmpty(request.SshAuthMethod).Length > 20) return "Server sshAuthMethod must not exceed 20 characters.";
        if (TrimOrEmpty(request.SshCredential).Length > 16000) return "Server sshCredential must not exceed 16000 characters.";
        if (TrimOrEmpty(request.OwnerType).Length > 40) return "Server ownerType must not exceed 40 characters.";
        if (TrimOrEmpty(request.PanelBaseUrl).Length > 2000) return "Server panelBaseUrl must not exceed 2000 characters.";
        if (TrimOrEmpty(request.PanelUsername).Length > 200) return "Server panelUsername must not exceed 200 characters.";
        if (TrimOrEmpty(request.PanelPassword).Length > 4096) return "Server panelPassword must not exceed 4096 characters.";
        if (TrimOrEmpty(request.PublicHostname).Length > 253) return "Server publicHostname must not exceed 253 characters.";

        return null;
    }

    private static string TrimOrEmpty(string? value) => value?.Trim() ?? string.Empty;

    private async Task<List<PaymentAttempt>> LoadLatestOrderPaymentsAsync(
        DbContext dbContext,
        IReadOnlyList<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        var placeholders = string.Join(", ", orderIds.Select((_, index) => $"{{{index}}}"));
        var parameters = orderIds.Cast<object>().ToArray();
        var sql = dbContext.Database.IsSqlite()
            ? $"""
               SELECT * FROM (
                   SELECT payments.*, ROW_NUMBER() OVER (
                       PARTITION BY "OrderId"
                       ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                   ) AS "_LatestRank"
                   FROM "Payments" AS payments
                   WHERE "OrderId" IN ({placeholders})
               ) AS latest
               WHERE "_LatestRank" = 1
               """
            : $"""
               SELECT DISTINCT ON ("OrderId") *
               FROM "Payments"
               WHERE "OrderId" IN ({placeholders})
               ORDER BY "OrderId", "CreatedAt" DESC, "Id" DESC
               """;

        return await _db.Payments
            .FromSqlRaw(sql, parameters)
            .AsNoTracking()
            .Include(x => x.PaymentProviderAccount)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<NodeHealthCheck>> LoadLatestNodeHealthChecksAsync(
        DbContext dbContext,
        IReadOnlyList<Guid> nodeIds,
        CancellationToken cancellationToken)
    {
        var placeholders = string.Join(", ", nodeIds.Select((_, index) => $"{{{index}}}"));
        var parameters = nodeIds.Cast<object>().ToArray();
        var sql = dbContext.Database.IsSqlite()
            ? $"""
               SELECT * FROM (
                   SELECT checks.*, ROW_NUMBER() OVER (
                       PARTITION BY "NodeId"
                       ORDER BY julianday("CheckedAt") DESC, "Id" DESC
                   ) AS "_LatestRank"
                   FROM "NodeHealthChecks" AS checks
                   WHERE "NodeId" IN ({placeholders})
               ) AS latest
               WHERE "_LatestRank" = 1
               """
            : $"""
               SELECT DISTINCT ON ("NodeId") *
               FROM "NodeHealthChecks"
               WHERE "NodeId" IN ({placeholders})
               ORDER BY "NodeId", "CheckedAt" DESC, "Id" DESC
               """;

        return await _db.NodeHealthChecks
            .FromSqlRaw(sql, parameters)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ProvisioningStepRun>> LoadLatestProvisioningPrecheckReportsAsync(
        DbContext dbContext,
        IReadOnlyList<Guid> runIds,
        CancellationToken cancellationToken)
    {
        var placeholders = string.Join(", ", runIds.Select((_, index) => $"{{{index}}}"));
        var stepNameParameter = $"{{{runIds.Count}}}";
        var parameters = runIds.Cast<object>().Append(PrecheckReportStepName).ToArray();
        var sql = dbContext.Database.IsSqlite()
            ? $"""
               SELECT * FROM (
                   SELECT steps.*, ROW_NUMBER() OVER (
                       PARTITION BY "ProvisioningRunId"
                       ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                   ) AS "_LatestRank"
                   FROM "ProvisioningStepRuns" AS steps
                   WHERE "ProvisioningRunId" IN ({placeholders})
                     AND "StepName" = {stepNameParameter}
               ) AS latest
               WHERE "_LatestRank" = 1
               """
            : $"""
               SELECT DISTINCT ON ("ProvisioningRunId") *
               FROM "ProvisioningStepRuns"
               WHERE "ProvisioningRunId" IN ({placeholders})
                 AND "StepName" = {stepNameParameter}
               ORDER BY "ProvisioningRunId", "CreatedAt" DESC, "Id" DESC
               """;

        return await _db.ProvisioningStepRuns
            .FromSqlRaw(sql, parameters)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeServerProtocols(string? value)
    {
        var candidate = string.IsNullOrWhiteSpace(value)
            ? VpnProtocolPolicy.DefaultSupportedProtocolsCsv
            : value;
        return VpnProtocolPolicy.TryNormalizeCsv(candidate, out var normalized)
            ? normalized
            : string.Empty;
    }

    private static string NormalizePublicHostname(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : ProvisioningService.NormalizeHost(value);

    private static bool RequiresServerSecretProtection(CreateServerHttpRequest request)
        => !string.IsNullOrWhiteSpace(request.SshCredential)
            || !string.IsNullOrWhiteSpace(request.PanelPassword);

    private static bool TryNormalizeServerPanelBaseUrl(string? value, out string normalized, out string error)
    {
        normalized = value?.Trim() ?? string.Empty;
        error = string.Empty;
        if (normalized.Length == 0)
        {
            return true;
        }
        if (SafeHttpUrl.ContainsCredentials(normalized))
        {
            error = "Panel base URL must not contain credentials (login or password).";
            return false;
        }
        if (!SafeHttpUrl.TryNormalize(normalized, out normalized))
        {
            error = "Panel base URL must be an absolute HTTP or HTTPS URL.";
            return false;
        }
        return true;
    }

    private static NodeHealthCheckDto MapNodeHealthCheck(NodeHealthCheck check)
        => new(
            check.Id,
            check.NodeId,
            check.Status.ToString(),
            check.CheckedAt,
            check.LatencyMs,
            check.MetadataJson,
            RedactSensitiveText(check.ErrorText, 1000));

    private static object MapProvisioningCommandResult(Guid? serverId, ProvisioningRun run, VpnNode? node, string status)
    {
        var mode = ProvisioningService.DescribeProvisioningMode(node, run.DryRun);
        return new
        {
            ServerId = serverId ?? run.NodeId,
            RunId = run.Id,
            Status = status,
            run.DryRun,
            Mode = mode.Mode,
            ModeTitle = mode.Title,
            RiskLevel = mode.RiskLevel,
            mode.LiveDeployAllowed,
            mode.NextAction,
            mode.OperatorWarning
        };
    }

    private static object MapVpnNode(VpnNode node, NodeHealthCheck? latestHealthCheck = null)
    {
        var dryRunMode = ProvisioningService.DescribeProvisioningMode(node, dryRun: true);
        var deployMode = ProvisioningService.DescribeProvisioningMode(node, dryRun: false);
        return new
        {
            node.Id,
            node.Revision,
            node.Name,
            node.Host,
            node.IpAddress,
            node.Provider,
            node.Region,
            node.Country,
            node.Datacenter,
            Status = node.Status.ToString(),
            node.Capacity,
            node.UsedCapacity,
            node.SupportedProtocolsCsv,
            HealthStatus = node.HealthStatus.ToString(),
            node.LastHealthCheckAt,
            LastHealthLatencyMs = latestHealthCheck?.LatencyMs,
            LastHealthError = latestHealthCheck is null ? string.Empty : RedactSensitiveText(latestHealthCheck.ErrorText, 1000),
            LastHealthMetadataJson = latestHealthCheck?.MetadataJson ?? string.Empty,
            ProvisioningStatus = node.ProvisioningStatus.ToString(),
            ProvisioningMode = deployMode.Mode,
            ProvisioningModeTitle = deployMode.Title,
            ProvisioningRiskLevel = deployMode.RiskLevel,
            deployMode.LiveDeployAllowed,
            ProvisioningNextAction = deployMode.NextAction,
            ProvisioningOperatorWarning = deployMode.OperatorWarning,
            PrecheckMode = dryRunMode.Mode,
            PrecheckModeTitle = dryRunMode.Title,
            node.InstalledVersion,
            node.BackupStatus,
            node.MonitoringStatus,
            node.LoggingStatus,
            node.TagsCsv,
            node.Priority,
            node.IsAvailableForNewUsers,
            node.SshPort,
            node.SshUser,
            SshAuthMethod = ProvisioningService.GetSshAuthMethod(node),
            SshCredentialConfigured = ProvisioningService.CredentialsConfigured(node),
            node.SkipHostKeyChecking,
            node.PanelBaseUrl,
            node.PanelUsername,
            PanelPasswordConfigured = ProvisioningService.PanelPasswordConfigured(node),
            node.PanelInboundId,
            node.PublicHostname,
            node.PublicPort,
            node.NodeGroupId,
            node.CreatedAt,
            node.UpdatedAt
        };
    }

    private Guid? ResolveUserId()
    {
        var principal = HttpContext?.User ?? User;
        if (principal is null)
        {
            return null;
        }

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(sub, out var value) ? value : null;
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var separator = email.IndexOf('@');
        if (separator <= 0 || separator == email.Length - 1)
        {
            return "***";
        }

        var local = email[..separator];
        var visible = local[..Math.Min(2, local.Length)];
        return $"{visible}***@{email[(separator + 1)..]}";
    }
}
