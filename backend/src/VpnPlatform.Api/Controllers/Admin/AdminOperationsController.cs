using System.Security.Claims;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

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
    string? OwnerType = null);

public sealed record QueueProvisionHttpRequest(bool DryRun = false);
public sealed record RefundPaymentHttpRequest(decimal Amount, string? Reason);
public sealed record RefundReadinessDto(bool IsSupported, bool CanRefund, decimal RefundableAmount, IReadOnlyList<string> Blockers);
public sealed record SetProviderEnabledHttpRequest(bool Enabled);
public sealed record AdminSupportReplyHttpRequest(string Text);
public sealed record AdminSupportStatusHttpRequest(string Status, Guid? AssignedToUserId = null);
public sealed record AdminSupportNoteHttpRequest(string Text);
public sealed record AdminSubscriptionExtendHttpRequest(int Days, string? Reason = null);
public sealed record AdminAccessActionHttpRequest(string? Reason = null);
public sealed record SetNodeAllocationHttpRequest(bool Available);
public sealed record DeleteServerHttpResponse(Guid Id, bool Deleted, bool Archived, int LinkedSubscriptions, int LinkedAccesses, int LinkedProvisioningRuns);
public sealed record NodeHealthCheckDto(Guid Id, Guid NodeId, string Status, DateTimeOffset CheckedAt, long LatencyMs, string MetadataJson, string ErrorText);
public sealed record AdminAuditLogFilters(string? Action = null, string? EntityType = null, string? ActorType = null, string? Search = null, DateTimeOffset? From = null, DateTimeOffset? To = null, int Limit = 200);

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin")]
public class AdminOperationsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IApplicationDbContext _db;
    private readonly ProvisioningService _provisioningService;
    private readonly PaymentOrchestrator _paymentOrchestrator;
    private readonly PaymentProviderAccountService _paymentProviderAccounts;
    private readonly VpnAccessLifecycleService? _vpnAccessLifecycleService;
    private readonly ISecretProtector? _secretProtector;
    private readonly IQrCodeGenerator? _qrCodeGenerator;
    private readonly IVpnProviderFactory? _vpnProviderFactory;

    public AdminOperationsController(
        IApplicationDbContext db,
        ProvisioningService provisioningService,
        PaymentOrchestrator paymentOrchestrator,
        PaymentProviderAccountService paymentProviderAccounts,
        VpnAccessLifecycleService? vpnAccessLifecycleService = null,
        ISecretProtector? secretProtector = null,
        IQrCodeGenerator? qrCodeGenerator = null,
        IVpnProviderFactory? vpnProviderFactory = null)
    {
        _db = db;
        _provisioningService = provisioningService;
        _paymentOrchestrator = paymentOrchestrator;
        _paymentProviderAccounts = paymentProviderAccounts;
        _vpnAccessLifecycleService = vpnAccessLifecycleService;
        _secretProtector = secretProtector;
        _qrCodeGenerator = qrCodeGenerator;
        _vpnProviderFactory = vpnProviderFactory;
    }

    [HttpGet("audit-logs")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AdminAuditLogFilters filters, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(filters.Limit, 1, 500);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Action))
        {
            var action = filters.Action.Trim();
            query = query.Where(x => x.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(filters.EntityType))
        {
            var entityType = filters.EntityType.Trim();
            query = query.Where(x => x.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(filters.ActorType))
        {
            var actorType = filters.ActorType.Trim();
            query = query.Where(x => x.ActorType == actorType);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(x =>
                x.Action.Contains(search) ||
                x.EntityType.Contains(search) ||
                x.EntityId.Contains(search) ||
                x.ActorId.Contains(search));
        }

        var rows = await query.ToListAsync(cancellationToken);
        if (filters.From.HasValue)
        {
            rows = rows.Where(x => x.CreatedAt >= filters.From.Value).ToList();
        }

        if (filters.To.HasValue)
        {
            rows = rows.Where(x => x.CreatedAt <= filters.To.Value).ToList();
        }

        var logs = rows
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
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

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var subscriptions = await _db.Subscriptions.AsNoTracking()
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
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        return Ok(subscriptions.OrderByDescending(x => x.CreatedAt).Take(300).ToList());
    }

    [HttpGet("access-credentials")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetAccessCredentials(CancellationToken cancellationToken)
    {
        var accessCredentials = await _db.AccessCredentials.AsNoTracking()
            .Include(x => x.Subscription)
            .Include(x => x.Server)
            .Include(x => x.History)
            .ToListAsync(cancellationToken);

        return Ok(accessCredentials
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .Select(x => new
            {
                x.Id,
                x.SubscriptionId,
                UserId = x.Subscription?.UserId,
                x.ProviderType,
                x.ProviderAccessId,
                x.ServerId,
                ServerName = x.Server?.Name ?? string.Empty,
                x.AccessUri,
                QrCodePayload = x.QrCodePath,
                x.QrCodePath,
                x.ConfigPath,
                Status = x.Status.ToString(),
                x.IssuedAt,
                ExpiryDate = x.Subscription?.EndAt,
                x.DisabledAt,
                x.LastSyncedAt,
                x.Revision,
                History = x.History
                .OrderByDescending(h => h.CreatedAt)
                .Take(5)
                .Select(h => new AdminAccessCredentialHistoryDto(h.Id, h.AccessCredentialId, h.SubscriptionId, h.EventType, h.OldValueJson, h.NewValueJson, h.CreatedAt))
                .ToList(),
                x.CreatedAt,
                x.UpdatedAt
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

        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        var now = DateTimeOffset.UtcNow;
        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.EndAt, subscription.GracePeriodEndAt, subscription.BlockReason });
        var baseDate = subscription.EndAt > now ? subscription.EndAt : now;
        subscription.EndAt = baseDate.AddDays(request.Days);
        subscription.GracePeriodEndAt = subscription.EndAt.AddDays(3);
        var statusResult = StatusStateMachine.TrySetSubscriptionStatus(subscription, SubscriptionStatus.Active, now);
        if (!statusResult.IsSuccess) return BadRequest(new { error = statusResult.Error });
        subscription.BlockReason = null;
        AddAuditLog("subscription.extend", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, subscription.EndAt, request.Days, request.Reason }));
        await _db.SaveChangesAsync(cancellationToken);
        if (subscription.CurrentAccess is not null && _vpnAccessLifecycleService is not null && subscription.CurrentAccess.Status != AccessCredentialStatus.Active)
        {
            await _vpnAccessLifecycleService.EnableAccessAsync(subscription.CurrentAccess.Id, request.Reason ?? "manual_subscription_extend", ResolveUserId(), cancellationToken);
        }
        return Ok(new { subscription.Id, Status = subscription.Status.ToString(), subscription.EndAt, subscription.GracePeriodEndAt });
    }

    [HttpPost("subscriptions/{id:guid}/activate")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> ActivateSubscription(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        if (subscription.EndAt <= DateTimeOffset.UtcNow)
        {
            return BadRequest(new { error = "Subscription period has already ended. Extend the subscription before activation." });
        }

        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.BlockReason, subscription.SuspendedAt, subscription.CancelledAt });
        var now = DateTimeOffset.UtcNow;
        var statusResult = StatusStateMachine.TrySetSubscriptionStatus(subscription, SubscriptionStatus.Active, now);
        if (!statusResult.IsSuccess) return BadRequest(new { error = statusResult.Error });
        subscription.BlockReason = null;
        subscription.SuspendedAt = null;
        subscription.CancelledAt = null;
        subscription.GracePeriodEndAt ??= subscription.EndAt.AddDays(3);
        AddAuditLog("subscription.activate", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, request?.Reason }));
        await _db.SaveChangesAsync(cancellationToken);

        AdminAccessActionResult? accessResult = null;
        if (subscription.CurrentAccess is not null)
        {
            if (_vpnAccessLifecycleService is not null)
            {
                var result = await _vpnAccessLifecycleService.EnableAccessAsync(subscription.CurrentAccess.Id, request?.Reason ?? "manual_subscription_activate", ResolveUserId(), cancellationToken);
                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Error });
                }

                accessResult = result.Value;
            }
            else if (subscription.CurrentAccess.Status != AccessCredentialStatus.Active || subscription.CurrentAccess.DisabledAt.HasValue)
            {
                var accessBefore = JsonSerializer.Serialize(new { subscription.CurrentAccess.Status, subscription.CurrentAccess.DisabledAt });
                var accessStatusResult = StatusStateMachine.TrySetAccessStatus(subscription.CurrentAccess, AccessCredentialStatus.Active, DateTimeOffset.UtcNow);
                if (!accessStatusResult.IsSuccess) return BadRequest(new { error = accessStatusResult.Error });
                subscription.CurrentAccess.DisabledAt = null;
                subscription.CurrentAccess.LastSyncedAt = DateTimeOffset.UtcNow;
                subscription.CurrentAccess.Revision += 1;
                _db.AccessCredentialHistories.Add(new AccessCredentialHistory
                {
                    AccessCredentialId = subscription.CurrentAccess.Id,
                    SubscriptionId = subscription.Id,
                    EventType = "manual_subscription_activate",
                    OldValueJson = accessBefore,
                    NewValueJson = JsonSerializer.Serialize(new { subscription.CurrentAccess.Status, request?.Reason })
                });
                AddAuditLog("access.enable", "AccessCredential", subscription.CurrentAccess.Id, accessBefore, JsonSerializer.Serialize(new { subscription.CurrentAccess.Status, request?.Reason }));
                await _db.SaveChangesAsync(cancellationToken);
                accessResult = new AdminAccessActionResult(subscription.CurrentAccess.Id, subscription.CurrentAccess.Status.ToString(), subscription.CurrentAccess.DisabledAt, subscription.CurrentAccess.LastSyncedAt, subscription.CurrentAccess.Revision, null, "Access enabled.");
            }
        }

        return Ok(new { subscription.Id, Status = subscription.Status.ToString(), subscription.EndAt, subscription.CurrentAccessId, Access = accessResult });
    }

    [HttpPost("subscriptions/{id:guid}/block")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> BlockSubscription(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        var now = DateTimeOffset.UtcNow;
        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.BlockReason });
        var statusResult = StatusStateMachine.TrySetSubscriptionStatus(subscription, SubscriptionStatus.Blocked, now);
        if (!statusResult.IsSuccess) return BadRequest(new { error = statusResult.Error });
        subscription.BlockReason = string.IsNullOrWhiteSpace(request?.Reason) ? "manual_admin_action" : request!.Reason!.Trim();
        AddAuditLog("subscription.block", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, subscription.BlockReason }));
        await _db.SaveChangesAsync(cancellationToken);
        if (subscription.CurrentAccess is not null && _vpnAccessLifecycleService is not null)
        {
            await _vpnAccessLifecycleService.DisableAccessAsync(subscription.CurrentAccess.Id, "AccessDisabledOnSubscriptionBlock", subscription.BlockReason, ResolveUserId(), cancellationToken);
        }
        return Ok(new { subscription.Id, Status = subscription.Status.ToString(), subscription.BlockReason });
    }

    [HttpPost("subscriptions/{id:guid}/unblock")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> UnblockSubscription(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        var now = DateTimeOffset.UtcNow;
        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.BlockReason });
        var nextStatus = subscription.EndAt >= now ? SubscriptionStatus.Active : SubscriptionStatus.Expired;
        var statusResult = StatusStateMachine.TrySetSubscriptionStatus(subscription, nextStatus, now);
        if (!statusResult.IsSuccess) return BadRequest(new { error = statusResult.Error });
        subscription.BlockReason = null;
        AddAuditLog("subscription.unblock", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, request?.Reason }));
        await _db.SaveChangesAsync(cancellationToken);
        if (subscription.Status == SubscriptionStatus.Active && subscription.CurrentAccess is not null && _vpnAccessLifecycleService is not null)
        {
            await _vpnAccessLifecycleService.EnableAccessAsync(subscription.CurrentAccess.Id, request?.Reason ?? "manual_subscription_unblock", ResolveUserId(), cancellationToken);
        }
        return Ok(new { subscription.Id, Status = subscription.Status.ToString() });
    }

    [HttpPost("subscriptions/{id:guid}/cancel")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> CancelSubscription(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        var subscription = await _db.Subscriptions.Include(x => x.CurrentAccess).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();

        var now = DateTimeOffset.UtcNow;
        var before = JsonSerializer.Serialize(new { subscription.Status, subscription.CancelledAt });
        var statusResult = StatusStateMachine.TrySetSubscriptionStatus(subscription, SubscriptionStatus.Cancelled, now);
        if (!statusResult.IsSuccess) return BadRequest(new { error = statusResult.Error });
        subscription.CancelledAt = now;
        subscription.BlockReason = string.IsNullOrWhiteSpace(request?.Reason) ? subscription.BlockReason : request!.Reason!.Trim();
        AddAuditLog("subscription.cancel", "Subscription", id, before, JsonSerializer.Serialize(new { subscription.Status, subscription.CancelledAt, request?.Reason }));
        await _db.SaveChangesAsync(cancellationToken);
        if (subscription.CurrentAccess is not null && _vpnAccessLifecycleService is not null)
        {
            await _vpnAccessLifecycleService.DisableAccessAsync(subscription.CurrentAccess.Id, "AccessDisabledOnSubscriptionCancel", request?.Reason ?? "manual_subscription_cancel", ResolveUserId(), cancellationToken);
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

        var subscription = await _db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (subscription is null) return NotFound();
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
        if (_qrCodeGenerator is null)
        {
            return BadRequest(new { error = "QR code generator is not configured." });
        }

        var access = await _db.AccessCredentials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (access is null)
        {
            return NotFound(new { error = "VPN access not found." });
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
        if (_vpnAccessLifecycleService is not null)
        {
            var result = await _vpnAccessLifecycleService.DisableAccessAsync(id, "manual_admin_disable", request?.Reason, ResolveUserId(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
        }

        var access = await _db.AccessCredentials.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (access is null) return NotFound();

        var now = DateTimeOffset.UtcNow;
        var before = JsonSerializer.Serialize(new { access.Status, access.DisabledAt });
        var statusResult = StatusStateMachine.TrySetAccessStatus(access, AccessCredentialStatus.Disabled, now);
        if (!statusResult.IsSuccess) return BadRequest(new { error = statusResult.Error });
        access.DisabledAt = now;
        _db.AccessCredentialHistories.Add(new AccessCredentialHistory
        {
            AccessCredentialId = access.Id,
            SubscriptionId = access.SubscriptionId,
            EventType = "manual_admin_disable",
            OldValueJson = before,
            NewValueJson = JsonSerializer.Serialize(new { access.Status, access.DisabledAt, request?.Reason })
        });
        AddAuditLog("access.disable", "AccessCredential", id, before, JsonSerializer.Serialize(new { access.Status, access.DisabledAt, request?.Reason }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { access.Id, Status = access.Status.ToString(), access.DisabledAt });
    }

    [HttpPost("access-credentials/{id:guid}/enable")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> EnableAccessCredential(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (_vpnAccessLifecycleService is not null)
        {
            var result = await _vpnAccessLifecycleService.EnableAccessAsync(id, request?.Reason, ResolveUserId(), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
        }

        var access = await _db.AccessCredentials.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (access is null) return NotFound();

        var before = JsonSerializer.Serialize(new { access.Status, access.DisabledAt });
        var statusResult = StatusStateMachine.TrySetAccessStatus(access, AccessCredentialStatus.Active, DateTimeOffset.UtcNow);
        if (!statusResult.IsSuccess) return BadRequest(new { error = statusResult.Error });
        access.DisabledAt = null;
        _db.AccessCredentialHistories.Add(new AccessCredentialHistory
        {
            AccessCredentialId = access.Id,
            SubscriptionId = access.SubscriptionId,
            EventType = "manual_admin_enable",
            OldValueJson = before,
            NewValueJson = JsonSerializer.Serialize(new { access.Status, request?.Reason })
        });
        AddAuditLog("access.enable", "AccessCredential", id, before, JsonSerializer.Serialize(new { access.Status, request?.Reason }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { access.Id, Status = access.Status.ToString() });
    }

    [HttpPost("access-credentials/{id:guid}/sync")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> SyncAccessCredential(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (_vpnAccessLifecycleService is null)
        {
            return BadRequest(new { error = "VPN access lifecycle service is not configured." });
        }

        var result = await _vpnAccessLifecycleService.SyncAccessAsync(id, request?.Reason, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("access-credentials/{id:guid}/reset-traffic")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> ResetAccessTraffic(Guid id, [FromBody] AdminAccessActionHttpRequest? request, CancellationToken cancellationToken)
    {
        if (_vpnAccessLifecycleService is null)
        {
            return BadRequest(new { error = "VPN access lifecycle service is not configured." });
        }

        var result = await _vpnAccessLifecycleService.ResetTrafficAsync(id, request?.Reason, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("subscriptions/{id:guid}/migrate")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> MigrateSubscription(Guid id, [FromBody] Guid? targetNodeId, CancellationToken cancellationToken)
    {
        _db.MigrationJobs.Add(new MigrationJob
        {
            SourceNodeId = Guid.Empty,
            TargetNodeId = targetNodeId,
            Status = MigrationJobStatus.Planned,
            Type = "single-subscription",
            Notes = $"Migration requested for subscription {id}"
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { subscriptionId = id, targetNodeId, status = "planned" });
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

        var query = _db.Orders.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Tariff)
            .Include(x => x.PaymentAttempts)
            .AsQueryable();

        if (parsedStatuses.Count > 0)
        {
            query = query.Where(x => parsedStatuses.Contains(x.Status));
        }

        var orders = await query.ToListAsync(cancellationToken);
        orders = orders
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var searchText = NormalizeSearchText(search);
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            orders = orders
                .Where(x => OrderMatchesSearch(x, searchText))
                .ToList();
        }

        return Ok(orders.Take(300).Select(x =>
            {
                var lastPayment = x.PaymentAttempts
                    .OrderByDescending(payment => payment.CreatedAt)
                    .FirstOrDefault();

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
                    PaymentAttemptsCount = x.PaymentAttempts.Count,
                    LastPaymentId = lastPayment?.Id,
                    LastPaymentStatus = lastPayment?.Status.ToString(),
                    LastPaymentProvider = lastPayment?.Provider.ToString(),
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
        var paymentCandidates = await _db.Payments.AsNoTracking()
            .Where(x => x.OrderId == id)
            .ToListAsync(cancellationToken);
        var payment = paymentCandidates
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (payment is null)
        {
            return BadRequest(new { error = "Order does not have payment attempts to recheck." });
        }

        var result = await _paymentOrchestrator.RecheckPaymentAsync(payment.Id, cancellationToken);
        return result.IsSuccess
            ? Ok(new
            {
                OrderId = id,
                PaymentId = payment.Id,
                Status = result.Value!.Status.ToString(),
                result.Value.RawResponse,
                result.Value.StatusReason
            })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("payments")]
    [Authorize(Policy = AdminPolicies.FinanceRead)]
    public async Task<IActionResult> GetPayments(CancellationToken cancellationToken)
    {
        var payments = await _db.Payments.AsNoTracking()
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
            .Take(300)
            .Select(x =>
            {
                var refund = BuildRefundReadiness(x);
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
        var result = await _paymentOrchestrator.RecheckPaymentAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("payments/{id:guid}/refund")]
    [Authorize(Policy = AdminPolicies.FinanceWrite)]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentHttpRequest request, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking()
            .Include(x => x.PaymentProviderAccount)
            .Include(x => x.Refunds)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payment is null)
        {
            return BadRequest(new { error = "Payment attempt not found." });
        }

        var readiness = BuildRefundReadiness(payment);
        if (!readiness.CanRefund)
        {
            return BadRequest(new { error = "Payment cannot be refunded.", readiness });
        }

        if (request.Amount <= 0 || request.Amount > readiness.RefundableAmount)
        {
            return BadRequest(new { error = "Refund amount is invalid.", readiness });
        }

        var result = await _paymentOrchestrator.RefundPaymentAsync(id, request.Amount, request.Reason ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("payment-webhook-events")]
    [Authorize(Policy = AdminPolicies.FinanceRead)]
    public async Task<IActionResult> GetPaymentWebhookEvents(CancellationToken cancellationToken)
    {
        var events = await _db.PaymentWebhookEvents
            .AsNoTracking()
            .Select(x => new PaymentWebhookEventDto(x.Id, x.Provider, x.PaymentAttemptId, x.PaymentProviderAccountId, x.ProviderPaymentId, x.ExternalEventId, x.EventType, x.Status.ToString(), x.SignatureValidated, x.ReceivedAt, x.ProcessedAt, x.ErrorText))
            .ToListAsync(cancellationToken);
        return Ok(events.OrderByDescending(x => x.ReceivedAt).Take(200).ToList());
    }

    [HttpGet("refunds")]
    [Authorize(Policy = AdminPolicies.FinanceRead)]
    public async Task<IActionResult> GetRefunds(CancellationToken cancellationToken)
    {
        var refunds = await _db.Refunds
            .AsNoTracking()
            .Select(x => new RefundDto(x.Id, x.PaymentAttemptId, x.Provider, x.ProviderRefundId, x.Status.ToString(), x.Amount, x.Currency, x.Reason, x.CreatedAt, x.RefundedAt))
            .ToListAsync(cancellationToken);
        return Ok(refunds.OrderByDescending(x => x.CreatedAt).ToList());
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

        AddAuditLog("payment_provider.check", "PaymentProviderAccount", id, "{}", JsonSerializer.Serialize(new { result.Value!.Provider, result.Value.Mode, result.Value.IsReady, result.Value.HealthStatus, result.Value.Message }, JsonOptions));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(result.Value);
    }


    [HttpGet("support/conversations")]
    [Authorize(Policy = AdminPolicies.SupportRead)]
    public async Task<IActionResult> GetSupportConversations(CancellationToken cancellationToken)
    {
        var conversations = await _db.SupportConversations
            .AsNoTracking()
            .Select(x => new SupportConversationDto(x.Id, x.UserId, x.TelegramUserId, x.Channel, x.Status, x.Subject, x.AssignedToUserId, x.InternalNote, x.ClosedAt, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
        return Ok(conversations.OrderByDescending(x => x.UpdatedAt).Take(200).ToList());
    }

    [HttpGet("support/conversations/{id:guid}/messages")]
    [Authorize(Policy = AdminPolicies.SupportRead)]
    public async Task<IActionResult> GetSupportMessages(Guid id, CancellationToken cancellationToken)
    {
        var messages = await _db.SupportMessages
            .AsNoTracking()
            .Where(x => x.SupportConversationId == id)
            .Select(x => new SupportMessageDto(x.Id, x.SupportConversationId, x.UserId, x.TelegramUserId, x.Direction, x.Text, x.AttachmentsJson, x.IsInternalNote, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(messages.OrderBy(x => x.CreatedAt).ToList());
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

        var text = NormalizeSupportText(request?.Text, 4000);
        if (text.Length < 2)
        {
            return BadRequest(new { error = "Reply text must contain at least 2 characters." });
        }

        _db.SupportMessages.Add(new SupportMessage
        {
            SupportConversationId = id,
            UserId = ResolveUserId(),
            TelegramUserId = conversation.TelegramUserId,
            Direction = "outbound",
            Text = text,
            AttachmentsJson = "[]"
        });

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
                    NextAttemptAt = DateTimeOffset.UtcNow
                });
            }
        }

        conversation.Status = conversation.Status == "closed" ? "open" : conversation.Status;
        conversation.ClosedAt = conversation.Status == "closed" ? conversation.ClosedAt : null;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { conversationId = id, status = "queued" });
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

        var status = string.IsNullOrWhiteSpace(request?.Status) ? conversation.Status : request.Status.Trim().ToLowerInvariant();
        if (status is not ("open" or "pending" or "closed"))
        {
            return BadRequest(new { error = "Status must be open, pending or closed." });
        }

        conversation.Status = status;
        conversation.AssignedToUserId = request?.AssignedToUserId ?? conversation.AssignedToUserId;
        conversation.ClosedAt = status == "closed" ? DateTimeOffset.UtcNow : null;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { conversationId = id, conversation.Status, conversation.AssignedToUserId });
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

        var text = NormalizeSupportText(request?.Text, 4000);
        if (text.Length < 2)
        {
            return BadRequest(new { error = "Note text must contain at least 2 characters." });
        }

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
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new SupportMessageDto(note.Id, note.SupportConversationId, note.UserId, note.TelegramUserId, note.Direction, note.Text, note.AttachmentsJson, note.IsInternalNote, note.CreatedAt));
    }

    [HttpGet("servers")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetServers(CancellationToken cancellationToken)
    {
        var nodes = await _db.VpnNodes.AsNoTracking()
            .OrderBy(x => x.Region)
            .ThenBy(x => x.Name)
            .Take(300)
            .ToListAsync(cancellationToken);
        var nodeIds = nodes.Select(x => x.Id).ToList();
        var latestChecks = await _db.NodeHealthChecks.AsNoTracking()
            .Where(x => nodeIds.Contains(x.NodeId))
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
        var host = ProvisioningService.NormalizeHost(string.IsNullOrWhiteSpace(request.Host) ? request.IpAddress : request.Host);
        if (string.IsNullOrWhiteSpace(host) || !ProvisioningService.IsValidHost(host))
        {
            return BadRequest(new { error = "Invalid server host/IP." });
        }

        if (request.SshPort <= 0 || request.SshPort > 65535)
        {
            return BadRequest(new { error = "SSH port must be between 1 and 65535." });
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
            protectedCredential = _secretProtector is not null
                ? _secretProtector.Protect(request.SshCredential.Trim())
                : "validation-placeholder:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.SshCredential.Trim()))).ToLowerInvariant();
        }
        else if (!string.IsNullOrWhiteSpace(request.SshPrivateKeyPath))
        {
            // Compatibility: key path for explicitly approved live operators. Never return this value from API responses.
            legacySshKeyPath = request.SshPrivateKeyPath.Trim();
        }

        var protectedPanelPassword = string.IsNullOrWhiteSpace(request.PanelPassword)
            ? string.Empty
            : _secretProtector is not null
                ? _secretProtector.Protect(request.PanelPassword.Trim())
                : "validation-placeholder:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.PanelPassword.Trim()))).ToLowerInvariant();

        var tags = NormalizeServerTags(request.TagsCsv, owner, authMethod, string.IsNullOrWhiteSpace(protectedCredential) ? "missing" : "protected", request.ValidationMode);

        var node = new VpnNode
        {
            Name = request.Name,
            Host = string.IsNullOrWhiteSpace(request.Host) ? host : request.Host.Trim(),
            IpAddress = request.IpAddress,
            Provider = string.IsNullOrWhiteSpace(request.Provider) ? "admin-vps" : request.Provider,
            Region = request.Region,
            Country = request.Country,
            Datacenter = request.Datacenter,
            Capacity = request.Capacity > 0 ? request.Capacity : 5000,
            SupportedProtocolsCsv = string.IsNullOrWhiteSpace(request.SupportedProtocolsCsv) ? "vless,vmess,trojan" : request.SupportedProtocolsCsv,
            Priority = request.Priority > 0 ? request.Priority : 100,
            TagsCsv = tags,
            SshUser = string.IsNullOrWhiteSpace(request.SshUser) ? "root" : request.SshUser,
            SshPort = request.SshPort > 0 ? request.SshPort : 22,
            SshPrivateKeyPath = legacySshKeyPath,
            ProtectedSshCredential = protectedCredential,
            SshCredentialRef = string.IsNullOrWhiteSpace(protectedCredential) ? string.Empty : $"secretref:ssh:{Guid.NewGuid():N}",
            SkipHostKeyChecking = request.SkipHostKeyChecking,
            PanelBaseUrl = request.PanelBaseUrl ?? string.Empty,
            PanelUsername = string.IsNullOrWhiteSpace(request.PanelUsername) ? "admin" : request.PanelUsername,
            PanelPassword = string.Empty,
            ProtectedPanelPassword = protectedPanelPassword,
            PanelSecretRef = string.IsNullOrWhiteSpace(protectedPanelPassword) ? string.Empty : $"secretref:panel:{Guid.NewGuid():N}",
            PanelInboundId = request.PanelInboundId,
            PublicHostname = request.PublicHostname ?? string.Empty,
            PublicPort = request.PublicPort > 0 ? request.PublicPort : 443,
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
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null)
        {
            return NotFound(new { error = "Server not found." });
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

        var authMethod = ProvisioningService.NormalizeAuthMethod(request.SshAuthMethod ?? ProvisioningService.GetSshAuthMethod(node));
        if (!string.IsNullOrWhiteSpace(request.SshCredential) && authMethod != "password" && authMethod != "ssh_key")
        {
            return BadRequest(new { error = "Unsupported SSH auth method." });
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
            node.ProtectedSshCredential = _secretProtector is not null
                ? _secretProtector.Protect(sshCredential)
                : "validation-placeholder:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sshCredential))).ToLowerInvariant();
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
            node.ProtectedPanelPassword = _secretProtector is not null
                ? _secretProtector.Protect(panelPassword)
                : "validation-placeholder:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(panelPassword))).ToLowerInvariant();
            node.PanelSecretRef = $"secretref:panel:{Guid.NewGuid():N}";
            node.PanelPassword = string.Empty;
        }

        node.Name = request.Name.Trim();
        node.Host = string.IsNullOrWhiteSpace(request.Host) ? host : request.Host.Trim();
        node.IpAddress = request.IpAddress.Trim();
        node.Provider = string.IsNullOrWhiteSpace(request.Provider) ? "admin-vps" : request.Provider.Trim();
        node.Region = request.Region.Trim();
        node.Country = request.Country.Trim();
        node.Datacenter = request.Datacenter.Trim();
        node.Capacity = request.Capacity > 0 ? request.Capacity : 5000;
        node.SupportedProtocolsCsv = string.IsNullOrWhiteSpace(request.SupportedProtocolsCsv) ? "vless,vmess,trojan" : request.SupportedProtocolsCsv.Trim();
        node.Priority = request.Priority > 0 ? request.Priority : 100;
        node.SshUser = string.IsNullOrWhiteSpace(request.SshUser) ? "root" : request.SshUser.Trim();
        node.SshPort = request.SshPort > 0 ? request.SshPort : 22;
        node.SkipHostKeyChecking = request.SkipHostKeyChecking;
        node.PanelBaseUrl = request.PanelBaseUrl?.Trim() ?? string.Empty;
        node.PanelUsername = string.IsNullOrWhiteSpace(request.PanelUsername) ? "admin" : request.PanelUsername.Trim();
        node.PanelInboundId = request.PanelInboundId;
        node.PublicHostname = request.PublicHostname?.Trim() ?? string.Empty;
        node.PublicPort = request.PublicPort > 0 ? request.PublicPort : 443;
        node.NodeGroupId = request.NodeGroupId;
        node.TagsCsv = NormalizeServerTags(request.TagsCsv, owner, authMethod, ProvisioningService.CredentialsConfigured(node) ? "protected" : "missing", request.ValidationMode);
        node.UpdatedAt = DateTimeOffset.UtcNow;

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
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapVpnNode(node));
    }

    [HttpPost("servers/{id:guid}/provision")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> Provision(Guid id, [FromBody] QueueProvisionHttpRequest? request, CancellationToken cancellationToken)
    {
        var result = await _provisioningService.QueueAsync(id, request?.DryRun ?? false, ResolveUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return Ok(MapProvisioningCommandResult(id, result.Value!, node, "queued"));
    }

    [HttpPost("servers/{id:guid}/precheck")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> Precheck(Guid id, CancellationToken cancellationToken)
    {
        var result = await _provisioningService.QueueAsync(id, true, ResolveUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return Ok(MapProvisioningCommandResult(id, result.Value!, node, "queued"));
    }

    [HttpPost("servers/{id:guid}/disable")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> DisableServer(Guid id, CancellationToken cancellationToken)
    {
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null) return NotFound();

        var before = JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers });
        node.Status = NodeStatus.Disabled;
        node.IsAvailableForNewUsers = false;
        node.UpdatedAt = DateTimeOffset.UtcNow;
        AddAuditLog("server.disable", "VpnNode", id, before, JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapVpnNode(node));
    }

    [HttpDelete("servers/{id:guid}")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> DeleteServer(Guid id, CancellationToken cancellationToken)
    {
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null)
        {
            return NotFound(new { error = "Server not found." });
        }

        var linkedSubscriptions = await _db.Subscriptions.CountAsync(x => x.CurrentServerId == id, cancellationToken);
        var linkedAccesses = await _db.AccessCredentials.CountAsync(x => x.ServerId == id, cancellationToken);
        var linkedRuns = await _db.ProvisioningRuns.CountAsync(x => x.NodeId == id, cancellationToken);
        var before = JsonSerializer.Serialize(new
        {
            node.Name,
            node.Host,
            node.Status,
            node.IsAvailableForNewUsers,
            linkedSubscriptions,
            linkedAccesses,
            linkedRuns
        });

        if (linkedSubscriptions > 0 || linkedAccesses > 0 || linkedRuns > 0)
        {
            node.Status = NodeStatus.Archived;
            node.IsAvailableForNewUsers = false;
            node.UpdatedAt = DateTimeOffset.UtcNow;
            AddAuditLog("server.archive", "VpnNode", id, before, JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers, linkedSubscriptions, linkedAccesses, linkedRuns }));
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new DeleteServerHttpResponse(id, Deleted: false, Archived: true, linkedSubscriptions, linkedAccesses, linkedRuns));
        }

        _db.VpnNodes.Remove(node);
        AddAuditLog("server.delete", "VpnNode", id, before, "{}");
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new DeleteServerHttpResponse(id, Deleted: true, Archived: false, linkedSubscriptions, linkedAccesses, linkedRuns));
    }

    [HttpPost("servers/{id:guid}/health-check")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> CheckServerHealth(Guid id, CancellationToken cancellationToken)
    {
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
            CheckedAt = DateTimeOffset.UtcNow,
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
        node.UpdatedAt = DateTimeOffset.UtcNow;
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

        var checks = await _db.NodeHealthChecks.AsNoTracking()
            .Where(x => x.NodeId == id)
            .ToListAsync(cancellationToken);
        return Ok(checks.OrderByDescending(x => x.CheckedAt).Take(20).Select(MapNodeHealthCheck).ToList());
    }

    [HttpGet("provisioning-runs")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetProvisioningRuns(CancellationToken cancellationToken)
    {
        var runs = await _db.ProvisioningRuns
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        runs = runs.OrderByDescending(x => x.CreatedAt).Take(200).ToList();
        var nodeIds = runs.Select(x => x.NodeId).Distinct().ToList();
        var nodes = await _db.VpnNodes.AsNoTracking().Where(x => nodeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        return Ok(runs.Select(x =>
        {
            nodes.TryGetValue(x.NodeId, out var node);
            var mode = ProvisioningService.DescribeProvisioningMode(node, x.DryRun);
            var deployMode = ProvisioningService.DescribeProvisioningMode(node, dryRun: false);
            return new
            {
                x.Id,
                x.NodeId,
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
                x.StartedAt,
                x.FinishedAt,
                ErrorSummary = IsProvisioningFailure(x.Status) ? RedactSensitiveText(x.ExecutionLog, 1000) : string.Empty,
                ExecutionLogPreview = RedactSensitiveText(x.ExecutionLog, 2000),
                ExecutionLog = RedactSensitiveText(x.ExecutionLog, 2000),
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
        var steps = await _db.ProvisioningStepRuns.AsNoTracking()
            .Where(x => x.ProvisioningRunId == id)
            .ToListAsync(cancellationToken);
        steps = steps.OrderBy(x => x.CreatedAt).ToList();

        var access = node is null
            ? null
            : await _db.AccessCredentials.AsNoTracking().FirstOrDefaultAsync(x => x.ServerId == node.Id, cancellationToken);

        var mode = ProvisioningService.DescribeProvisioningMode(node, run.DryRun);
        var deployMode = ProvisioningService.DescribeProvisioningMode(node, dryRun: false);
        return Ok(new
        {
            Run = new
            {
                run.Id,
                run.NodeId,
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
                run.StartedAt,
                run.FinishedAt,
                ErrorSummary = IsProvisioningFailure(run.Status) ? RedactSensitiveText(run.ExecutionLog, 1000) : string.Empty,
                ExecutionLog = RedactSensitiveText(run.ExecutionLog, 8000),
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
    public async Task<IActionResult> RetryProvisioningRun(Guid id, CancellationToken cancellationToken)
    {
        var result = await _provisioningService.RetryAsync(id, ResolveUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == result.Value!.NodeId, cancellationToken);
        return Ok(MapProvisioningCommandResult(null, result.Value!, node, result.Value!.Status.ToString()));
    }

    [HttpPost("provisioning-runs/{id:guid}/deploy")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> DeployProvisioningRun(Guid id, CancellationToken cancellationToken)
    {
        var result = await _provisioningService.QueueDeployAsync(id, ResolveUserId(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        var node = await _db.VpnNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == result.Value!.NodeId, cancellationToken);
        return Ok(MapProvisioningCommandResult(null, result.Value!, node, result.Value!.Status.ToString()));
    }

    [HttpPost("provisioning-runs/{id:guid}/cancel")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> CancelProvisioningRun(Guid id, CancellationToken cancellationToken)
    {
        var result = await _provisioningService.CancelAsync(id, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new { runId = id, status = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPost("provisioning-runs/{id:guid}/support-needed")]
    [Authorize(Policy = AdminPolicies.ProvisioningManage)]
    public async Task<IActionResult> MarkProvisioningSupportNeeded(Guid id, CancellationToken cancellationToken)
    {
        var result = await _provisioningService.MarkSupportNeededAsync(id, ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(new { runId = id, supportConversationId = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPost("servers/{id:guid}/maintenance")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> Maintenance(Guid id, CancellationToken cancellationToken)
    {
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null) return NotFound();

        var before = JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers });
        node.Status = NodeStatus.Maintenance;
        node.IsAvailableForNewUsers = false;
        node.UpdatedAt = DateTimeOffset.UtcNow;
        AddAuditLog("server.maintenance.enable", "VpnNode", id, before, JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapVpnNode(node));
    }

    [HttpPost("servers/{id:guid}/disable-maintenance")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> DisableMaintenance(Guid id, CancellationToken cancellationToken)
    {
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null) return NotFound();

        var before = JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers });
        node.Status = NodeStatus.Ready;
        node.IsAvailableForNewUsers = true;
        node.UpdatedAt = DateTimeOffset.UtcNow;
        AddAuditLog("server.maintenance.disable", "VpnNode", id, before, JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapVpnNode(node));
    }

    [HttpPost("servers/{id:guid}/disable-allocation")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> DisableAllocation(Guid id, CancellationToken cancellationToken)
    {
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null) return NotFound();

        var before = JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers });
        node.IsAvailableForNewUsers = false;
        node.Status = NodeStatus.Draining;
        node.UpdatedAt = DateTimeOffset.UtcNow;
        AddAuditLog("server.allocation.disable", "VpnNode", id, before, JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapVpnNode(node));
    }

    [HttpPost("servers/{id:guid}/enable-allocation")]
    [Authorize(Policy = AdminPolicies.VpnManage)]
    public async Task<IActionResult> EnableAllocation(Guid id, CancellationToken cancellationToken)
    {
        var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null) return NotFound();

        var before = JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers });
        node.IsAvailableForNewUsers = true;
        node.Status = NodeStatus.Ready;
        node.UpdatedAt = DateTimeOffset.UtcNow;
        AddAuditLog("server.allocation.enable", "VpnNode", id, before, JsonSerializer.Serialize(new { node.Status, node.IsAvailableForNewUsers }));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapVpnNode(node));
    }

    [HttpGet("tariffs")]
    public async Task<IActionResult> GetTariffs(CancellationToken cancellationToken)
    {
        var tariffs = await _db.Tariffs.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        return Ok(tariffs.Select(MapTariffDto).ToList());
    }

    [HttpPost("tariffs")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> CreateTariff([FromBody] Tariff tariff, CancellationToken cancellationToken)
    {
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

        var before = JsonSerializer.Serialize(MapTariffDto(tariff));
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

        var validationError = NormalizeTariff(tariff);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        if (await _db.Tariffs.AnyAsync(x => x.Id != id && x.Slug == tariff.Slug, cancellationToken))
        {
            return BadRequest(new { error = "Tariff slug already exists." });
        }

        tariff.UpdatedAt = DateTimeOffset.UtcNow;
        AddAuditLog("tariff.update", "Tariff", id, before, JsonSerializer.Serialize(MapTariffDto(tariff)));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapTariffDto(tariff));
    }

    [HttpDelete("tariffs/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> DeleteTariff(Guid id, CancellationToken cancellationToken)
    {
        var tariff = await _db.Tariffs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tariff is null) return NotFound();

        var hasLinkedOrders = await _db.Orders.AnyAsync(x => x.TariffId == id, cancellationToken);
        var hasLinkedSubscriptions = await _db.Subscriptions.AnyAsync(x => x.TariffId == id, cancellationToken);
        if (hasLinkedOrders || hasLinkedSubscriptions)
        {
            var beforeArchive = JsonSerializer.Serialize(MapTariffDto(tariff));
            tariff.IsActive = false;
            tariff.VisibleTo = DateTimeOffset.UtcNow;
            tariff.UpdatedAt = DateTimeOffset.UtcNow;
            AddAuditLog("tariff.archive", "Tariff", id, beforeArchive, JsonSerializer.Serialize(MapTariffDto(tariff)));
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { id, deleted = false, archived = true });
        }

        var before = JsonSerializer.Serialize(MapTariffDto(tariff));
        _db.Tariffs.Remove(tariff);
        AddAuditLog("tariff.delete", "Tariff", id, before, "{}");
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { id, deleted = true });
    }

    [HttpGet("referrals")]
    public async Task<IActionResult> GetReferrals(CancellationToken cancellationToken)
    {
        var rewards = await _db.RewardLedgers.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(rewards.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpPost("referral-programs")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> CreateReferralProgram([FromBody] ReferralProgram program, CancellationToken cancellationToken)
    {
        _db.ReferralPrograms.Add(program);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(program);
    }

    [HttpPatch("referral-programs/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> PatchReferralProgram(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var program = await _db.ReferralPrograms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (program is null) return NotFound();

        if (payload.TryGetProperty("name", out var name)) program.Name = name.GetString() ?? program.Name;
        if (payload.TryGetProperty("status", out var status)) program.Status = status.GetString() ?? program.Status;
        if (payload.TryGetProperty("ruleDefinition", out var ruleDefinition)) program.RuleDefinition = ruleDefinition.GetRawText();

        program.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(program);
    }


    private static TariffDto MapTariffDto(Tariff tariff)
        => new(
            tariff.Id,
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

        tariff.Name = tariff.Name.Trim();
        tariff.Slug = Slugify(string.IsNullOrWhiteSpace(tariff.Slug) ? tariff.Name : tariff.Slug);
        tariff.Description = tariff.Description.Trim();
        tariff.FullDescription = tariff.FullDescription.Trim();
        tariff.FeaturesJson = NormalizeTariffFeaturesJson(tariff.FeaturesJson);
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

        return null;
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

    private static string NormalizeTariffFeaturesJson(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson)) return "[]";

        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(featuresJson) ?? [];
            return JsonSerializer.Serialize(items
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(), JsonOptions);
        }
        catch (JsonException)
        {
            return "[]";
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
            if (!Enum.TryParse<OrderStatus>(rawValue, ignoreCase: true, out var parsed))
            {
                return null;
            }

            result.Add(parsed);
        }

        return result.Distinct().ToArray();
    }

    private static string NormalizeSearchText(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static bool OrderMatchesSearch(Order order, string searchText)
        => order.Id.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || order.UserId.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || order.TariffId.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || (order.User?.Email ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || (order.User?.DisplayName ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || (order.Tariff?.Name ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || order.Status.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || order.Type.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || order.Channel.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || order.PaymentProvider.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || order.Currency.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || order.PaymentAttempts.Any(payment =>
                payment.Id.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || payment.ProviderPaymentId.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || payment.Status.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase));

    private static RefundReadinessDto BuildRefundReadiness(PaymentAttempt payment)
    {
        var blockers = new List<string>();
        var refundableAmount = Math.Max(0m, payment.Amount - payment.RefundedAmount);
        var isSupported = PaymentProviderConfigurationRules.GetCapabilityRules(payment.Provider)
            .Any(x => x.Key == "refund" && x.Supported);

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

        if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
        {
            blockers.Add("Не сохранен идентификатор платежа у провайдера.");
        }

        var account = payment.PaymentProviderAccount;
        if (payment.PaymentProviderAccountId is null || account is null)
        {
            blockers.Add("Платеж не связан с аккаунтом платежного провайдера.");
        }
        else
        {
            if (account.Provider != payment.Provider)
            {
                blockers.Add("Аккаунт провайдера не совпадает с провайдером платежа.");
            }

            if (!account.IsEnabled)
            {
                blockers.Add("Аккаунт платежного провайдера выключен.");
            }

            if (account.Mode == PaymentProviderMode.Disabled)
            {
                blockers.Add("Аккаунт платежного провайдера находится в режиме Disabled.");
            }

            AddProviderSpecificRefundBlockers(payment, account, blockers);
        }

        return new RefundReadinessDto(isSupported, blockers.Count == 0, refundableAmount, blockers);
    }

    private static void AddProviderSpecificRefundBlockers(PaymentAttempt payment, PaymentProviderAccount account, List<string> blockers)
    {
        static bool Missing(string? value) => string.IsNullOrWhiteSpace(value);
        var hasSecret = !Missing(account.SecretKeyProtected);

        switch (payment.Provider)
        {
            case PaymentProvider.YooKassa:
                if (account.Mode == PaymentProviderMode.Production && Missing(account.ShopId))
                {
                    blockers.Add("Для production-возврата YooKassa нужен ShopId.");
                }

                if (account.Mode == PaymentProviderMode.Production && !hasSecret)
                {
                    blockers.Add("Для production-возврата YooKassa нужен SecretKey.");
                }
                break;

            case PaymentProvider.TBankAcquiring:
                if (Missing(account.ShopId))
                {
                    blockers.Add("Для возврата TBank нужен TerminalKey.");
                }

                if (!hasSecret)
                {
                    blockers.Add("Для возврата TBank нужен Password терминала.");
                }
                break;

            case PaymentProvider.Stripe:
                if (!hasSecret)
                {
                    blockers.Add("Для возврата Stripe нужен SecretKey.");
                }

                if (Missing(payment.ProviderPaymentId))
                {
                    blockers.Add("Для возврата Stripe нужен Checkout Session ID.");
                }
                break;

            case PaymentProvider.PayPal:
                if (Missing(account.ShopId))
                {
                    blockers.Add("Для возврата PayPal нужен Client ID.");
                }

                if (!hasSecret)
                {
                    blockers.Add("Для возврата PayPal нужен Client secret.");
                }

                if (Missing(payment.ProviderPaymentId))
                {
                    blockers.Add("Для возврата PayPal нужен Order ID, чтобы получить capture.");
                }
                break;
        }
    }

    private void AddAuditLog(string action, string entityType, Guid entityId, string beforeJson, string afterJson)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = "admin",
            ActorId = ResolveUserId()?.ToString() ?? "unknown",
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            BeforeJson = SensitiveDataRedactor.Redact(beforeJson),
            AfterJson = SensitiveDataRedactor.Redact(afterJson),
            Ip = HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty
        });
    }

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
}
