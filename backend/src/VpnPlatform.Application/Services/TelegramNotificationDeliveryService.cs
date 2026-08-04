using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Application.Services;

public sealed class TelegramNotificationDeliveryService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan DeliveryLease = TimeSpan.FromMinutes(1);
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly ITelegramInvoiceProvider _provider;

    public TelegramNotificationDeliveryService(IApplicationDbContext db, IClock clock, ITelegramInvoiceProvider provider)
    {
        _db = db;
        _clock = clock;
        _provider = provider;
    }

    public async Task<IReadOnlyList<Guid>> GetDispatchableIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var staleBefore = now.Subtract(DeliveryLease);
        var limit = Math.Clamp(take, 1, 100);
        var result = new List<Guid>(limit);
        var offset = 0;
        while (result.Count < limit)
        {
            var query = _db.TelegramBotNotifications.AsNoTracking()
                .Where(x => x.Status == "pending" || x.Status == "sending");
            var ordered = IsSqlite()
                ? query.OrderBy(x => x.Id)
                : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id);
            var batch = await ordered
                .Skip(offset)
                .Take(100)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            result.AddRange(batch
                .Where(x => x.Status == "pending"
                    ? !x.NextAttemptAt.HasValue || x.NextAttemptAt <= now
                    : x.UpdatedAt <= staleBefore)
                .Select(x => x.Id)
                .Take(limit - result.Count));
            offset += batch.Count;
            if (batch.Count < 100)
            {
                break;
            }
        }

        return result;
    }

    public async Task<Result<TelegramNotificationDeliveryResult>> DeliverAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireTelegramNotificationAsync(notificationId, cancellationToken);
        var now = _clock.UtcNow;
        var notification = await _db.TelegramBotNotifications.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (notification is null)
        {
            return Result<TelegramNotificationDeliveryResult>.Failure("Telegram notification was not found.");
        }

        if (IsTerminal(notification.Status))
        {
            return Result<TelegramNotificationDeliveryResult>.Success(
                new TelegramNotificationDeliveryResult(notificationId, notification.Status == "sent", notification.Status));
        }

        if (notification.Status == "pending" && notification.NextAttemptAt > now)
        {
            return Result<TelegramNotificationDeliveryResult>.Failure("Telegram notification retry is scheduled.", isRetryable: true);
        }

        if (notification.Status == "sending" && notification.UpdatedAt > now.Subtract(DeliveryLease))
        {
            return Result<TelegramNotificationDeliveryResult>.Failure("Telegram notification delivery is already in progress.", isRetryable: true);
        }

        if (notification.Status is not ("pending" or "sending"))
        {
            return Result<TelegramNotificationDeliveryResult>.Failure("Telegram notification has an unsupported status.");
        }

        if (notification.AttemptCount >= MaxAttempts)
        {
            await MarkTerminalAsync(notification, "failed", "Telegram notification delivery lease expired after the maximum number of attempts.", cancellationToken);
            return Result<TelegramNotificationDeliveryResult>.Failure("Telegram notification reached the maximum number of delivery attempts.");
        }

        var expectedStatus = notification.Status;
        var expectedVersion = notification.UpdatedAt;
        var claimVersion = NextVersion(expectedVersion, now);
        if (_db is DbContext dbContext
            && !string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            var claimed = await _db.TelegramBotNotifications
                .Where(x => x.Id == notification.Id && x.Status == expectedStatus && x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "sending")
                    .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                    .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.ErrorText, string.Empty)
                    .SetProperty(x => x.UpdatedAt, claimVersion), cancellationToken);
            if (claimed == 0)
            {
                return Result<TelegramNotificationDeliveryResult>.Failure("Telegram notification was claimed concurrently.", isRetryable: true);
            }

            await dbContext.Entry(notification).ReloadAsync(cancellationToken);
        }
        else
        {
            ApplyClaim(notification, claimVersion);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var accountBlocked = await _db.TelegramAccounts.AsNoTracking()
            .AnyAsync(x => x.TelegramUserId == notification.TelegramUserId && x.IsBlocked, cancellationToken);
        if (accountBlocked)
        {
            await MarkTerminalAsync(notification, "cancelled", "Telegram account is blocked.", cancellationToken);
            return Result<TelegramNotificationDeliveryResult>.Success(
                new TelegramNotificationDeliveryResult(notificationId, false, "cancelled"));
        }

        if (!TryExtractPayload(notification.PayloadJson, out var text, out var replyMarkupJson))
        {
            await MarkTerminalAsync(notification, "failed", "Telegram notification payload is invalid.", cancellationToken);
            return Result<TelegramNotificationDeliveryResult>.Failure("Telegram notification payload is invalid.");
        }

        try
        {
            await _provider.SendMessageAsync(notification.TelegramUserId, text, replyMarkupJson, cancellationToken);
            notification.Status = "sent";
            notification.SentAt = _clock.UtcNow;
            notification.NextAttemptAt = null;
            notification.ErrorText = string.Empty;
            notification.UpdatedAt = NextVersion(notification.UpdatedAt, notification.SentAt.Value);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<TelegramNotificationDeliveryResult>.Success(
                new TelegramNotificationDeliveryResult(notificationId, true, "sent"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkFailedAttemptAsync(notification, "Telegram notification delivery was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            await MarkFailedAttemptAsync(notification, ex.Message);
            var retryable = notification.Status == "pending";
            return Result<TelegramNotificationDeliveryResult>.Failure(
                retryable
                    ? "Telegram notification delivery failed and was scheduled for retry."
                    : "Telegram notification delivery failed after the maximum number of attempts.",
                isRetryable: retryable);
        }
    }

    private async Task MarkFailedAttemptAsync(TelegramBotNotification notification, string error)
    {
        var now = _clock.UtcNow;
        var retryable = notification.AttemptCount < MaxAttempts;
        notification.Status = retryable ? "pending" : "failed";
        notification.NextAttemptAt = retryable ? now.AddSeconds(BackoffSeconds(notification.AttemptCount)) : null;
        notification.ErrorText = SensitiveDataRedactor.Redact(error, maxLength: 500);
        notification.UpdatedAt = NextVersion(notification.UpdatedAt, now);
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task MarkTerminalAsync(
        TelegramBotNotification notification,
        string status,
        string error,
        CancellationToken cancellationToken)
    {
        notification.Status = status;
        notification.NextAttemptAt = null;
        notification.ErrorText = SensitiveDataRedactor.Redact(error, maxLength: 500);
        notification.UpdatedAt = NextVersion(notification.UpdatedAt, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyClaim(TelegramBotNotification notification, DateTimeOffset claimVersion)
    {
        notification.Status = "sending";
        notification.AttemptCount += 1;
        notification.NextAttemptAt = null;
        notification.ErrorText = string.Empty;
        notification.UpdatedAt = claimVersion;
    }

    private static bool TryExtractPayload(string payloadJson, out string text, out string? replyMarkupJson)
    {
        text = string.Empty;
        replyMarkupJson = null;
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return false;
            }

            text = payloadJson;
            replyMarkupJson = null;
            return true;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("text", out var textElement)
                || textElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            text = textElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (!doc.RootElement.TryGetProperty("replyMarkupJson", out var replyMarkupElement))
            {
                return true;
            }

            if (replyMarkupElement.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (replyMarkupElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            replyMarkupJson = replyMarkupElement.GetString();
            if (string.IsNullOrWhiteSpace(replyMarkupJson))
            {
                return true;
            }

            try
            {
                using var replyMarkup = JsonDocument.Parse(replyMarkupJson);
                return replyMarkup.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    private static bool IsTerminal(string status)
        => status is "sent" or "failed" or "cancelled";

    private bool IsSqlite()
        => _db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);

    private static double BackoffSeconds(int attemptCount)
        => Math.Min(300, Math.Pow(2, Math.Min(attemptCount, 10)) * 5);

    private static DateTimeOffset NextVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);
}
