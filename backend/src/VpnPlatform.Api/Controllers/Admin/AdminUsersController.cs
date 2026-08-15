using System.Text.Json;
using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Services;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;

    public AdminUsersController(IApplicationDbContext db, IClock? clock = null)
    {
        _db = db;
        _clock = clock ?? new SystemClock();
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? role, CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim().ToLowerInvariant();
        UserStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<UserStatus>(status, true, out var value) || !Enum.IsDefined(value))
            {
                return BadRequest(new { error = "Invalid user status." });
            }

            parsedStatus = value;
        }

        var normalizedRole = string.IsNullOrWhiteSpace(role)
            ? null
            : role.Trim().ToLowerInvariant();

        IQueryable<User> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            var statusValue = parsedStatus.HasValue ? (int?)parsedStatus.Value : null;
            query = _db.Users.FromSqlInterpolated($"""
                SELECT *
                FROM "Users"
                WHERE ({normalizedSearch} IS NULL
                        OR instr(lower(COALESCE("Email", '')), {normalizedSearch}) > 0
                        OR instr(lower("DisplayName"), {normalizedSearch}) > 0
                        OR instr(lower("ReferralCode"), {normalizedSearch}) > 0)
                  AND ({statusValue} IS NULL OR "Status" = {statusValue})
                  AND ({normalizedRole} IS NULL
                       OR instr(',' || lower(replace("RolesCsv", ' ', '')) || ',', ',' || {normalizedRole} || ',') > 0)
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT 300
                """).AsNoTracking();
        }
        else
        {
            query = _db.Users.AsNoTracking();

            if (normalizedSearch is not null)
            {
                query = query.Where(x =>
                    x.Email != null && x.Email.ToLower().Contains(normalizedSearch)
                    || x.DisplayName.ToLower().Contains(normalizedSearch)
                    || x.ReferralCode.ToLower().Contains(normalizedSearch));
            }

            if (parsedStatus.HasValue)
            {
                query = query.Where(x => x.Status == parsedStatus.Value);
            }

            if (normalizedRole is not null)
            {
                var roleToken = $",{normalizedRole},";
                query = query.Where(x =>
                    ("," + x.RolesCsv.Replace(" ", string.Empty).ToLower() + ",").Contains(roleToken));
            }

            query = query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(300);
        }

        var users = await query.ToListAsync(cancellationToken);
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

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var canReadFinance = AdminPolicies.HasAccess(roles, AdminPolicies.FinanceRead);
        var canReadSupport = AdminPolicies.HasAccess(roles, AdminPolicies.SupportRead);

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

        var orderQuery = _db is DbContext orderDbContext && orderDbContext.Database.IsSqlite()
            ? _db.Orders.FromSqlInterpolated($"""
                SELECT *
                FROM "Orders"
                WHERE "UserId" = {id}
                ORDER BY julianday("CreatedAt") DESC, "Id" DESC
                LIMIT 20
                """).AsNoTracking()
            : _db.Orders.AsNoTracking()
                .Where(x => x.UserId == id)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(20);
        var orders = canReadFinance
            ? await orderQuery
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
            .ToListAsync(cancellationToken)
            : [];
        orders = orders.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList();

        var paymentQuery = _db is DbContext paymentDbContext && paymentDbContext.Database.IsSqlite()
            ? _db.Payments.FromSqlInterpolated($"""
                SELECT payment.*
                FROM "Payments" AS payment
                INNER JOIN "Orders" AS customer_order ON customer_order."Id" = payment."OrderId"
                WHERE customer_order."UserId" = {id}
                ORDER BY julianday(payment."CreatedAt") DESC, payment."Id" DESC
                LIMIT 20
                """).AsNoTracking()
            : _db.Payments.AsNoTracking()
                .Where(x => x.Order != null && x.Order.UserId == id)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(20);
        var payments = canReadFinance
            ? await paymentQuery
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
            .ToListAsync(cancellationToken)
            : [];
        payments = payments.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList();

        var subscriptionEntities = _db is DbContext subscriptionDbContext && subscriptionDbContext.Database.IsSqlite()
            ? await _db.Subscriptions
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "Subscriptions"
                    WHERE "UserId" = {id}
                    ORDER BY julianday("StartAt") DESC, "Id" DESC
                    LIMIT 20
                    """)
                .AsNoTracking()
                .Include(x => x.Tariff)
                .ToListAsync(cancellationToken)
            : await _db.Subscriptions.AsNoTracking()
                .Include(x => x.Tariff)
                .Where(x => x.UserId == id)
                .OrderByDescending(x => x.StartAt)
                .ThenByDescending(x => x.Id)
                .Take(20)
                .ToListAsync(cancellationToken);
        var subscriptions = subscriptionEntities
            .OrderByDescending(x => x.StartAt)
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
            .ToList();

        var now = _clock.UtcNow;
        var accessEntities = _db is DbContext accessDbContext && accessDbContext.Database.IsSqlite()
            ? await _db.AccessCredentials
                .FromSqlInterpolated($"""
                    SELECT access.*
                    FROM "AccessCredentials" AS access
                    INNER JOIN "Subscriptions" AS subscription ON subscription."Id" = access."SubscriptionId"
                    WHERE subscription."UserId" = {id}
                    ORDER BY julianday(access."IssuedAt") DESC, access."Id" DESC
                    LIMIT 20
                    """)
                .AsNoTracking()
                .Include(x => x.Subscription)
                .Include(x => x.Server)
                .ToListAsync(cancellationToken)
            : await _db.AccessCredentials.AsNoTracking()
                .Include(x => x.Subscription)
                .Include(x => x.Server)
                .Where(x => x.Subscription != null && x.Subscription.UserId == id)
                .OrderByDescending(x => x.IssuedAt)
                .ThenByDescending(x => x.Id)
                .Take(20)
                .ToListAsync(cancellationToken);
        var accesses = accessEntities.Select(x =>
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
                x.Id,
                x.SubscriptionId,
                UserId = x.Subscription != null ? x.Subscription.UserId : (Guid?)null,
                SubscriptionStatus = x.Subscription != null ? x.Subscription.Status.ToString() : string.Empty,
                IsTerminal = !accessAvailable,
                x.ProviderType,
                ProviderAccessId = accessAvailable ? x.ProviderAccessId : string.Empty,
                x.ServerId,
                ServerName = x.Server != null ? x.Server.Name : string.Empty,
                Status = x.Status.ToString(),
                AccessUri = accessAvailable ? x.AccessUri : string.Empty,
                QrCodePath = accessAvailable ? x.QrCodePath : string.Empty,
                QrCodePayload = accessAvailable ? x.QrCodePath : string.Empty,
                ConfigPath = accessAvailable ? x.ConfigPath : string.Empty,
                x.IssuedAt,
                ExpiryDate = x.Subscription is not null
                    ? BusinessRules.GetSubscriptionAccessEnd(x.Subscription.EndAt, x.Subscription.GracePeriodEndAt)
                    : (DateTimeOffset?)null,
                x.DisabledAt,
                x.LastSyncedAt,
                x.Revision,
                x.CreatedAt,
                x.UpdatedAt
            };
        }).ToList();
        accesses = accesses.OrderByDescending(x => x.IssuedAt).ThenByDescending(x => x.Id).ToList();

        var supportQuery = _db is DbContext supportDbContext && supportDbContext.Database.IsSqlite()
            ? _db.SupportConversations.FromSqlInterpolated($"""
                SELECT conversation.*
                FROM "SupportConversations" AS conversation
                WHERE conversation."UserId" = {id}
                   OR EXISTS (
                       SELECT 1
                       FROM "TelegramAccounts" AS telegram
                       WHERE telegram."UserId" = {id}
                         AND telegram."TelegramUserId" = conversation."TelegramUserId")
                ORDER BY julianday(conversation."UpdatedAt") DESC, conversation."Id" DESC
                LIMIT 20
                """).AsNoTracking()
            : _db.SupportConversations.AsNoTracking()
                .Where(x => x.UserId == id
                    || x.TelegramUserId.HasValue && _db.TelegramAccounts.Any(telegram =>
                        telegram.UserId == id && telegram.TelegramUserId == x.TelegramUserId.Value))
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .Take(20);
        var supportConversations = canReadSupport
            ? await supportQuery
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
                x.Revision,
                x.ClosedAt,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken)
            : [];
        supportConversations = supportConversations
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .ToList();

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

        var propertyNames = payload.EnumerateObject().Select(property => property.Name).ToList();
        if (propertyNames.Count != propertyNames.Distinct(StringComparer.Ordinal).Count())
        {
            return BadRequest(new { error = "User patch contains duplicate fields." });
        }

        var allowedFields = new HashSet<string>(["displayName", "isBlocked", "status", "updatedAt"], StringComparer.Ordinal);
        var unknownField = propertyNames.FirstOrDefault(propertyName => !allowedFields.Contains(propertyName));
        if (unknownField is not null)
        {
            return BadRequest(new { error = $"Unknown user patch field: {unknownField}." });
        }

        if (!payload.TryGetProperty("updatedAt", out var updatedAt)
            || updatedAt.ValueKind != JsonValueKind.String
            || !DateTimeOffset.TryParse(updatedAt.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expectedUpdatedAt))
        {
            return BadRequest(new { error = "User updatedAt version is required." });
        }

        if (expectedUpdatedAt != user.UpdatedAt)
        {
            return Conflict(new { error = "Профиль пользователя уже изменён. Обновите карточку и повторите действие." });
        }

        if (!propertyNames.Any(propertyName => propertyName is "displayName" or "isBlocked" or "status"))
        {
            return BadRequest(new { error = "User patch must contain at least one mutable field." });
        }

        string? nextDisplayName = null;
        if (payload.TryGetProperty("displayName", out var displayName))
        {
            if (displayName.ValueKind != JsonValueKind.String)
            {
                return BadRequest(new { error = "Display name must be a string." });
            }

            nextDisplayName = displayName.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(nextDisplayName) || nextDisplayName.Length > 80)
            {
                return BadRequest(new { error = "Display name must contain from 1 to 80 characters." });
            }
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
        var wasActive = !user.IsBlocked && user.Status == UserStatus.Active;
        var now = _clock.UtcNow;
        if (nextDisplayName is not null) user.DisplayName = nextDisplayName;
        if (nextIsBlocked.HasValue) user.IsBlocked = nextIsBlocked.Value;
        if (nextStatus.HasValue) user.Status = nextStatus.Value;
        var isActive = !user.IsBlocked && user.Status == UserStatus.Active;

        if (wasActive && !isActive)
        {
            user.SessionVersion = checked(user.SessionVersion + 1);
        }

        if (before.DisplayName == user.DisplayName
            && before.IsBlocked == user.IsBlocked
            && before.Status == user.Status.ToString())
        {
            return BadRequest(new { error = "Изменения профиля пользователя не обнаружены." });
        }

        user.UpdatedAt = now;
        if (_db is DbContext concurrencyContext)
        {
            concurrencyContext.Entry(user).Property(x => x.UpdatedAt).OriginalValue = expectedUpdatedAt;
        }
        AdminAuditLogWriter.Add(_db, this, "user.update", "User", user.Id, before, MapUser(user));
        try
        {
            await SaveUserPatchAsync(user.Id, !isActive, now, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (_db is DbContext dbContext)
            {
                dbContext.ChangeTracker.Clear();
            }
            return Conflict(new { error = "Профиль пользователя уже изменён. Обновите карточку и повторите действие." });
        }
        return Ok(MapUser(user));
    }

    private async Task SaveUserPatchAsync(
        Guid userId,
        bool revokeSessions,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!revokeSessions)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var revokedByIp = ControllerContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        if (_db is not DbContext dbContext || !dbContext.Database.IsRelational())
        {
            var sessions = await _db.UserRefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                RevokeSession(session, revokedByIp, now);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            await _db.UserRefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedByIp, revokedByIp)
                    .SetProperty(x => x.RevocationReason, "admin_user_deactivated")
                    .SetProperty(x => x.Revision, x => x.Revision + 1)
                    .SetProperty(x => x.UpdatedAt, now), cancellationToken);
            SynchronizeTrackedSessionRevocations(dbContext, userId, revokedByIp, now);
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            throw;
        }
    }

    private static void SynchronizeTrackedSessionRevocations(
        DbContext dbContext,
        Guid userId,
        string revokedByIp,
        DateTimeOffset now)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<UserRefreshToken>()
                     .Where(x => x.Entity.UserId == userId && x.Entity.RevokedAt is null))
        {
            RevokeSession(entry.Entity, revokedByIp, now);
            entry.OriginalValues.SetValues(entry.CurrentValues);
            entry.State = EntityState.Unchanged;
        }
    }

    private static void RevokeSession(UserRefreshToken session, string revokedByIp, DateTimeOffset now)
    {
        session.RevokedAt = now;
        session.RevokedByIp = revokedByIp;
        session.RevocationReason = "admin_user_deactivated";
        session.Revision = checked(session.Revision + 1);
        session.UpdatedAt = now;
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
