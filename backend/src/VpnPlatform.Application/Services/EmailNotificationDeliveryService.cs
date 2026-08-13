using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public sealed record EmailNotificationDeliveryResult(Guid NotificationId, bool Delivered, string Status);

public sealed class EmailNotificationDeliveryService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(1);
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly IEmailSender _sender;
    private readonly ISecretProtector _secretProtector;

    public EmailNotificationDeliveryService(
        IApplicationDbContext db,
        IClock clock,
        IEmailSender sender,
        ISecretProtector secretProtector)
    {
        _db = db;
        _clock = clock;
        _sender = sender;
        _secretProtector = secretProtector;
    }

    public async Task<IReadOnlyList<Guid>> GetDispatchableIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var staleBefore = now.Subtract(ProcessingLease);
        var limit = Math.Clamp(take, 1, 100);
        var query = _db.NotificationDeliveries.AsNoTracking()
            .Where(x => x.Channel == NotificationChannelType.Email
                && x.Status == NotificationDeliveryStatus.Pending
                && x.Attempts < MaxAttempts);

        if (!IsSqlite())
        {
            return await query
                .Where(x => x.NextAttemptAt == null || x.NextAttemptAt <= now)
                .Where(x => x.ProcessingStartedAt == null || x.ProcessingStartedAt <= staleBefore)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => x.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        var rows = await _db.NotificationDeliveries
            .FromSqlInterpolated($"""
                SELECT *
                FROM "NotificationDeliveries"
                WHERE "Channel" = {(int)NotificationChannelType.Email}
                  AND "Status" = {(int)NotificationDeliveryStatus.Pending}
                  AND "Attempts" < {MaxAttempts}
                  AND ("NextAttemptAt" IS NULL OR julianday("NextAttemptAt") <= julianday({now}))
                  AND ("ProcessingStartedAt" IS NULL OR julianday("ProcessingStartedAt") <= julianday({staleBefore}))
                ORDER BY julianday("CreatedAt"), "Id"
                LIMIT {limit}
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return rows.Select(x => x.Id).ToList();
    }

    public async Task<Result<EmailNotificationDeliveryResult>> DeliverAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireEmailNotificationAsync(notificationId, cancellationToken);
        var now = _clock.UtcNow;
        var notification = await _db.NotificationDeliveries.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (notification is null)
        {
            return Result<EmailNotificationDeliveryResult>.Failure("Email notification was not found.");
        }

        if (notification.Status != NotificationDeliveryStatus.Pending)
        {
            return Result<EmailNotificationDeliveryResult>.Success(new EmailNotificationDeliveryResult(
                notification.Id,
                notification.Status == NotificationDeliveryStatus.Sent,
                notification.Status.ToString().ToLowerInvariant()));
        }

        if (notification.NextAttemptAt > now)
        {
            return Result<EmailNotificationDeliveryResult>.Failure("Email notification retry is scheduled.", isRetryable: true);
        }

        if (notification.ProcessingStartedAt > now.Subtract(ProcessingLease))
        {
            return Result<EmailNotificationDeliveryResult>.Failure("Email notification delivery is already in progress.", isRetryable: true);
        }

        if (notification.Attempts >= MaxAttempts)
        {
            await MarkTerminalAsync(notification, NotificationDeliveryStatus.Failed, "Email notification reached the maximum number of attempts.", cancellationToken);
            return Result<EmailNotificationDeliveryResult>.Failure("Email notification reached the maximum number of attempts.");
        }

        var expectedAttempts = notification.Attempts;
        var expectedVersion = notification.UpdatedAt;
        var claimVersion = NextVersion(expectedVersion, now);
        if (_db is DbContext dbContext
            && !string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            var claimed = await _db.NotificationDeliveries
                .Where(x => x.Id == notification.Id
                    && x.Status == NotificationDeliveryStatus.Pending
                    && x.Attempts == expectedAttempts
                    && x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                    .SetProperty(x => x.ProcessingStartedAt, now)
                    .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.ErrorText, (string?)null)
                    .SetProperty(x => x.UpdatedAt, claimVersion), cancellationToken);
            if (claimed == 0)
            {
                return Result<EmailNotificationDeliveryResult>.Failure("Email notification was claimed concurrently.", isRetryable: true);
            }

            await dbContext.Entry(notification).ReloadAsync(cancellationToken);
        }
        else
        {
            ApplyClaim(notification, now, claimVersion);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (notification.UserId.HasValue)
        {
            var recipientExists = await _db.Users.AsNoTracking().AnyAsync(
                x => x.Id == notification.UserId && x.Status != UserStatus.Deleted,
                cancellationToken);
            if (!recipientExists)
            {
                await MarkTerminalAsync(notification, NotificationDeliveryStatus.Cancelled, "Email notification recipient is unavailable.", cancellationToken);
                return Result<EmailNotificationDeliveryResult>.Success(new EmailNotificationDeliveryResult(notification.Id, false, "cancelled"));
            }
        }

        EmailMessage message;
        try
        {
            message = await BuildMessageAsync(notification, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            await MarkTerminalAsync(notification, NotificationDeliveryStatus.Failed, ex.Message, CancellationToken.None);
            return Result<EmailNotificationDeliveryResult>.Failure("Email notification payload or template is invalid.");
        }

        try
        {
            await _sender.SendAsync(message, cancellationToken);
            notification.Status = NotificationDeliveryStatus.Sent;
            notification.SentAt = _clock.UtcNow;
            notification.ProcessingStartedAt = null;
            notification.NextAttemptAt = null;
            notification.ErrorText = null;
            notification.UpdatedAt = NextVersion(notification.UpdatedAt, notification.SentAt.Value);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<EmailNotificationDeliveryResult>.Success(new EmailNotificationDeliveryResult(notification.Id, true, "sent"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkRetryAsync(notification, "Email notification delivery was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            await MarkRetryAsync(notification, ex.Message, GetKnownSecrets(notification));
            var retryable = notification.Status == NotificationDeliveryStatus.Pending;
            return Result<EmailNotificationDeliveryResult>.Failure(
                retryable
                    ? "Email notification delivery failed and was scheduled for retry."
                    : "Email notification delivery failed after the maximum number of attempts.",
                isRetryable: retryable);
        }
    }

    private async Task<EmailMessage> BuildMessageAsync(NotificationDelivery notification, CancellationToken cancellationToken)
    {
        if (notification.TemplateKey == "password_reset_requested")
        {
            using var payload = JsonDocument.Parse(notification.PayloadJson);
            if (!payload.RootElement.TryGetProperty("protectedResetToken", out var tokenElement)
                || tokenElement.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(tokenElement.GetString()))
            {
                throw new InvalidOperationException("Protected password reset token is missing.");
            }

            var token = _secretProtector.Unprotect(tokenElement.GetString()!);
            var expiryMinutes = payload.RootElement.TryGetProperty("expiryMinutes", out var expiryElement)
                && expiryElement.TryGetInt32(out var parsedExpiry)
                    ? Math.Clamp(parsedExpiry, 1, 1440)
                    : 30;
            return new EmailMessage(
                notification.ToAddress,
                "Код восстановления пароля VPN Platform",
                $"Код восстановления пароля: {token}\n\nКод действует {expiryMinutes} мин. Если вы не запрашивали сброс пароля, проигнорируйте это письмо.");
        }

        var template = await _db.NotificationTemplates.AsNoTracking()
            .Where(x => x.Key == notification.TemplateKey
                && x.Channel == NotificationChannelType.Email
                && x.IsActive)
            .OrderByDescending(x => x.Language == "ru")
            .FirstOrDefaultAsync(cancellationToken);
        if (template is not null)
        {
            return new EmailMessage(notification.ToAddress, template.Subject, template.Body);
        }

        return notification.TemplateKey switch
        {
            "subscription_activated" => new EmailMessage(notification.ToAddress, "Ваш VPN-доступ готов", "Подписка активирована. Данные подключения доступны в личном кабинете."),
            "subscription_expiring" => new EmailMessage(notification.ToAddress, "Срок VPN-подписки заканчивается", "Срок подписки скоро закончится. Продлите ее в личном кабинете, чтобы сохранить доступ."),
            "subscription_expired" => new EmailMessage(notification.ToAddress, "VPN-подписка завершена", "Срок подписки закончился. Возобновить доступ можно в личном кабинете."),
            _ => throw new InvalidOperationException($"Active email template '{notification.TemplateKey}' was not found.")
        };
    }

    private async Task MarkRetryAsync(NotificationDelivery notification, string error, IEnumerable<string?>? knownSecrets = null)
    {
        var now = _clock.UtcNow;
        var retryable = notification.Attempts < MaxAttempts;
        notification.Status = retryable ? NotificationDeliveryStatus.Pending : NotificationDeliveryStatus.Failed;
        notification.ProcessingStartedAt = null;
        notification.NextAttemptAt = retryable ? now.AddSeconds(BackoffSeconds(notification.Attempts)) : null;
        notification.ErrorText = SensitiveDataRedactor.Redact(error, knownSecrets, maxLength: 500);
        notification.UpdatedAt = NextVersion(notification.UpdatedAt, now);
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task MarkTerminalAsync(NotificationDelivery notification, NotificationDeliveryStatus status, string error, CancellationToken cancellationToken)
    {
        notification.Status = status;
        notification.ProcessingStartedAt = null;
        notification.NextAttemptAt = null;
        notification.ErrorText = SensitiveDataRedactor.Redact(error, maxLength: 500);
        notification.UpdatedAt = NextVersion(notification.UpdatedAt, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyClaim(NotificationDelivery notification, DateTimeOffset now, DateTimeOffset version)
    {
        notification.Attempts += 1;
        notification.ProcessingStartedAt = now;
        notification.NextAttemptAt = null;
        notification.ErrorText = null;
        notification.UpdatedAt = version;
    }

    private IEnumerable<string?> GetKnownSecrets(NotificationDelivery notification)
    {
        var secrets = new List<string?> { notification.ToAddress };
        if (notification.TemplateKey != "password_reset_requested")
        {
            return secrets;
        }

        try
        {
            using var payload = JsonDocument.Parse(notification.PayloadJson);
            if (payload.RootElement.TryGetProperty("protectedResetToken", out var tokenElement)
                && tokenElement.ValueKind == JsonValueKind.String)
            {
                secrets.Add(_secretProtector.Unprotect(tokenElement.GetString() ?? string.Empty));
            }
        }
        catch
        {
        }

        return secrets;
    }

    private bool IsSqlite()
        => _db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);

    private static double BackoffSeconds(int attempts)
        => Math.Min(300, Math.Pow(2, Math.Min(attempts, 10)) * 5);

    private static DateTimeOffset NextVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);
}
