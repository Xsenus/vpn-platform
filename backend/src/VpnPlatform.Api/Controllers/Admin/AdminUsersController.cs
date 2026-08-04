using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public AdminUsersController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? role, CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Email != null && x.Email.ToLower().Contains(normalized)
                || x.DisplayName.ToLower().Contains(normalized)
                || x.ReferralCode.ToLower().Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<UserStatus>(status, true, out var parsedStatus) || !Enum.IsDefined(parsedStatus))
            {
                return BadRequest(new { error = "Invalid user status." });
            }

            query = query.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim();
            query = query.Where(x => x.RolesCsv.Contains(normalizedRole));
        }

        var users = await query.ToListAsync(cancellationToken);
        users = users.OrderByDescending(x => x.CreatedAt).Take(300).ToList();
        return Ok(users.Select(MapUser).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(MapUser(user));
    }

    [HttpGet("{id:guid}/overview")]
    public async Task<IActionResult> GetOverview(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return NotFound();

        var telegramAccounts = await _db.TelegramAccounts.AsNoTracking()
            .Where(x => x.UserId == id)
            .Select(x => new
            {
                x.Id,
                x.TelegramUserId,
                x.Username,
                x.FirstName,
                x.LastName,
                x.LanguageCode,
                x.IsBlocked,
                x.LinkedAt,
                x.LastSeenAt,
                x.RegistrationCompletedAt
            })
            .ToListAsync(cancellationToken);
        telegramAccounts = telegramAccounts.OrderByDescending(x => x.LastSeenAt ?? x.LinkedAt).ToList();

        var orders = await _db.Orders.AsNoTracking()
            .Where(x => x.UserId == id)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.TariffId,
                TariffName = x.Tariff != null ? x.Tariff.Name : string.Empty,
                x.CheckoutSessionId,
                x.Amount,
                x.Currency,
                Status = x.Status.ToString(),
                Type = x.Type.ToString(),
                Channel = x.Channel.ToString(),
                PaymentProvider = x.PaymentProvider.ToString(),
                x.ExpiresAt,
                x.PaidAt,
                x.IsFirstPurchase,
                PaymentAttemptsCount = x.PaymentAttempts.Count,
                LinkedSubscriptionId = _db.Subscriptions
                    .Where(subscription => subscription.UserId == x.UserId && subscription.TariffId == x.TariffId && subscription.LastPaymentId.HasValue)
                    .Select(subscription => (Guid?)subscription.Id)
                    .FirstOrDefault(),
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        orders = orders.OrderByDescending(x => x.CreatedAt).Take(20).ToList();

        var payments = await _db.Payments.AsNoTracking()
            .Where(x => x.Order != null && x.Order.UserId == id)
            .Select(x => new
            {
                x.Id,
                x.OrderId,
                UserId = x.Order != null ? x.Order.UserId : (Guid?)null,
                UserDisplayName = x.Order != null && x.Order.User != null ? x.Order.User.DisplayName : string.Empty,
                Provider = x.Provider.ToString(),
                x.PaymentProviderAccountId,
                ProviderMode = x.ProviderMode.ToString(),
                Status = x.Status.ToString(),
                x.Amount,
                x.Currency,
                x.ProviderPaymentId,
                x.ExternalEventId,
                x.IdempotencyKey,
                x.ConfirmationUrl,
                x.ReturnUrl,
                x.SignatureValidated,
                x.IsActivationProcessed,
                x.ActivationProcessedAt,
                x.PaidAt,
                x.FailedAt,
                x.RefundedAt,
                x.RefundedAmount,
                x.StatusReason,
                WebhookEventsCount = _db.PaymentWebhookEvents.Count(eventItem => eventItem.PaymentAttemptId == x.Id),
                RefundsCount = x.Refunds.Count,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        payments = payments.OrderByDescending(x => x.CreatedAt).Take(20).ToList();

        var subscriptions = await _db.Subscriptions.AsNoTracking()
            .Where(x => x.UserId == id)
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
                x.CurrentAccessId,
                x.CurrentServerId,
                x.LastPaymentId,
                x.RenewalCount,
                x.BlockReason,
                x.SuspendedAt,
                x.CancelledAt,
                SourceChannel = x.SourceChannel.ToString(),
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        subscriptions = subscriptions.OrderByDescending(x => x.StartAt).Take(20).ToList();

        var accesses = await _db.AccessCredentials.AsNoTracking()
            .Where(x => x.Subscription != null && x.Subscription.UserId == id)
            .Select(x => new
            {
                x.Id,
                x.SubscriptionId,
                UserId = x.Subscription != null ? x.Subscription.UserId : (Guid?)null,
                x.ProviderType,
                x.ProviderAccessId,
                x.ServerId,
                ServerName = x.Server != null ? x.Server.Name : string.Empty,
                Status = x.Status.ToString(),
                x.AccessUri,
                x.QrCodePath,
                QrCodePayload = x.QrCodePath,
                x.ConfigPath,
                x.IssuedAt,
                x.DisabledAt,
                x.LastSyncedAt,
                x.Revision,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        accesses = accesses.OrderByDescending(x => x.IssuedAt).Take(20).ToList();

        var telegramUserIds = telegramAccounts.Select(t => t.TelegramUserId).ToList();

        var supportConversations = await _db.SupportConversations.AsNoTracking()
            .Where(x => x.UserId == id || (x.TelegramUserId.HasValue && telegramUserIds.Contains(x.TelegramUserId.Value)))
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.TelegramUserId,
                x.Channel,
                x.Status,
                x.Subject,
                x.AssignedToUserId,
                x.InternalNote,
                x.ClosedAt,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        supportConversations = supportConversations.OrderByDescending(x => x.UpdatedAt).Take(20).ToList();

        return Ok(new
        {
            User = MapUser(user),
            TelegramAccounts = telegramAccounts,
            Orders = orders,
            Payments = payments,
            Subscriptions = subscriptions,
            AccessCredentials = accesses,
            SupportConversations = supportConversations
        });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return NotFound();

        if (payload.ValueKind != JsonValueKind.Object)
        {
            return BadRequest(new { error = "User patch must be a JSON object." });
        }

        string? nextDisplayName = null;
        if (payload.TryGetProperty("displayName", out var displayName))
        {
            if (displayName.ValueKind != JsonValueKind.String)
            {
                return BadRequest(new { error = "Display name must be a string." });
            }

            nextDisplayName = displayName.GetString();
        }

        bool? nextIsBlocked = null;
        if (payload.TryGetProperty("isBlocked", out var isBlocked))
        {
            if (isBlocked.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return BadRequest(new { error = "isBlocked must be a boolean." });
            }

            nextIsBlocked = isBlocked.GetBoolean();
        }

        UserStatus? nextStatus = null;
        if (payload.TryGetProperty("status", out var status))
        {
            if (status.ValueKind != JsonValueKind.String
                || !Enum.TryParse<UserStatus>(status.GetString(), true, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                return BadRequest(new { error = "Invalid user status." });
            }

            nextStatus = parsed;
        }

        var before = MapUser(user);
        if (nextDisplayName is not null) user.DisplayName = nextDisplayName;
        if (nextIsBlocked.HasValue) user.IsBlocked = nextIsBlocked.Value;
        if (nextStatus.HasValue) user.Status = nextStatus.Value;

        user.UpdatedAt = DateTimeOffset.UtcNow;
        AdminAuditLogWriter.Add(_db, this, "user.update", "User", user.Id, before, MapUser(user));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapUser(user));
    }

    private static AdminUserDto MapUser(User user)
        => new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.RolesCsv,
            user.Status.ToString(),
            user.IsBlocked,
            user.PreferredLanguage,
            user.ReferralCode,
            user.AuthSource.ToString(),
            user.EmailConfirmed,
            user.LastLoginAt,
            user.TelegramRegistrationCompletedAt,
            user.CreatedAt,
            user.UpdatedAt);

    private sealed record AdminUserDto(
        Guid Id,
        string? Email,
        string DisplayName,
        string RolesCsv,
        string Status,
        bool IsBlocked,
        string PreferredLanguage,
        string ReferralCode,
        string AuthSource,
        bool EmailConfirmed,
        DateTimeOffset? LastLoginAt,
        DateTimeOffset? TelegramRegistrationCompletedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
