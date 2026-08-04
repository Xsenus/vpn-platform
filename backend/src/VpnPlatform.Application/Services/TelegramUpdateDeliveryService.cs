using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Application.Services;

public sealed class TelegramUpdateDeliveryService
{
    private static readonly TimeSpan DeliveryLease = TimeSpan.FromMinutes(1);
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly ITelegramInvoiceProvider _provider;

    public TelegramUpdateDeliveryService(IApplicationDbContext db, IClock clock, ITelegramInvoiceProvider provider)
    {
        _db = db;
        _clock = clock;
        _provider = provider;
    }

    public async Task<IReadOnlyList<long>> GetPendingUpdateIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var staleBefore = now.Subtract(DeliveryLease);
        var limit = Math.Clamp(take, 1, 100);
        var result = new List<long>(limit);
        long? cursor = null;
        while (result.Count < limit)
        {
            var query = _db.TelegramBotUpdates.AsNoTracking()
                .Where(x => x.IsProcessed
                    && ((x.PreCheckoutQueryId != string.Empty && x.PreCheckoutOk.HasValue && !x.PreCheckoutAnsweredAt.HasValue)
                        || (x.ResponseChatId.HasValue && x.ResponseText != string.Empty && !x.ResponseSentAt.HasValue)));
            if (cursor.HasValue)
            {
                query = query.Where(x => x.UpdateId > cursor.Value);
            }

            var batch = await query.OrderBy(x => x.UpdateId).Take(100).ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            result.AddRange(batch
                .Where(x => (!x.DeliveryNextAttemptAt.HasValue || x.DeliveryNextAttemptAt <= now)
                    && (!x.DeliveryClaimedAt.HasValue || x.DeliveryClaimedAt <= staleBefore))
                .Select(x => x.UpdateId)
                .Take(limit - result.Count));
            cursor = batch[^1].UpdateId;
            if (batch.Count < 100)
            {
                break;
            }
        }

        return result;
    }

    public async Task<Result<TelegramUpdateDeliveryResult>> DeliverAsync(long updateId, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireTelegramDeliveryAsync(updateId, cancellationToken);
        var now = _clock.UtcNow;
        var update = await _db.TelegramBotUpdates.FirstOrDefaultAsync(x => x.UpdateId == updateId, cancellationToken);
        if (update is null)
        {
            return Result<TelegramUpdateDeliveryResult>.Failure("Telegram update was not found.");
        }

        if (!update.IsProcessed)
        {
            return Result<TelegramUpdateDeliveryResult>.Failure("Telegram update processing is not complete.", isRetryable: true);
        }

        if (!HasPendingDelivery(update))
        {
            return Result<TelegramUpdateDeliveryResult>.Success(new TelegramUpdateDeliveryResult(updateId, false, false));
        }

        if (update.DeliveryNextAttemptAt > now)
        {
            return Result<TelegramUpdateDeliveryResult>.Failure("Telegram response delivery retry is scheduled.", isRetryable: true);
        }

        if (update.DeliveryClaimedAt > now.Subtract(DeliveryLease))
        {
            return Result<TelegramUpdateDeliveryResult>.Failure("Telegram response delivery is already in progress.", isRetryable: true);
        }

        var expectedVersion = update.UpdatedAt;
        var claimVersion = NextVersion(expectedVersion, now);
        if (_db is DbContext dbContext
            && !string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            var claimed = await _db.TelegramBotUpdates
                .Where(x => x.Id == update.Id && x.IsProcessed && x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DeliveryClaimedAt, now)
                    .SetProperty(x => x.DeliveryAttemptCount, x => x.DeliveryAttemptCount + 1)
                    .SetProperty(x => x.DeliveryErrorText, string.Empty)
                    .SetProperty(x => x.UpdatedAt, claimVersion), cancellationToken);
            if (claimed == 0)
            {
                return Result<TelegramUpdateDeliveryResult>.Failure("Telegram response delivery was claimed concurrently.", isRetryable: true);
            }

            await dbContext.Entry(update).ReloadAsync(cancellationToken);
        }
        else
        {
            ApplyClaim(update, now, claimVersion);
            await _db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            if (HasPendingPreCheckout(update))
            {
                await _provider.AnswerPreCheckoutQueryAsync(
                    update.PreCheckoutQueryId,
                    update.PreCheckoutOk!.Value,
                    string.IsNullOrWhiteSpace(update.PreCheckoutError) ? null : update.PreCheckoutError,
                    cancellationToken);
                update.PreCheckoutAnsweredAt = _clock.UtcNow;
                update.UpdatedAt = NextVersion(update.UpdatedAt, update.PreCheckoutAnsweredAt.Value);
                await _db.SaveChangesAsync(cancellationToken);
            }

            if (HasPendingMessage(update))
            {
                await _provider.SendMessageAsync(
                    update.ResponseChatId!.Value,
                    update.ResponseText,
                    string.IsNullOrWhiteSpace(update.ResponseReplyMarkupJson) ? null : update.ResponseReplyMarkupJson,
                    cancellationToken);
                update.ResponseSentAt = _clock.UtcNow;
                update.UpdatedAt = NextVersion(update.UpdatedAt, update.ResponseSentAt.Value);
                await _db.SaveChangesAsync(cancellationToken);
            }

            update.DeliveryClaimedAt = null;
            update.DeliveryNextAttemptAt = null;
            update.DeliveryErrorText = string.Empty;
            update.UpdatedAt = NextVersion(update.UpdatedAt, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<TelegramUpdateDeliveryResult>.Success(new TelegramUpdateDeliveryResult(updateId, true, true));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkFailedAsync(update, "Telegram response delivery was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            await MarkFailedAsync(update, ex.Message);
            return Result<TelegramUpdateDeliveryResult>.Failure("Telegram response delivery failed and was scheduled for retry.", isRetryable: true);
        }
    }

    private async Task MarkFailedAsync(TelegramBotUpdate update, string error)
    {
        var now = _clock.UtcNow;
        var delaySeconds = Math.Min(300, Math.Pow(2, Math.Min(update.DeliveryAttemptCount, 10)) * 5);
        update.DeliveryClaimedAt = null;
        update.DeliveryNextAttemptAt = now.AddSeconds(delaySeconds);
        update.DeliveryErrorText = SensitiveDataRedactor.Redact(error, maxLength: 500);
        update.UpdatedAt = NextVersion(update.UpdatedAt, now);
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private static void ApplyClaim(TelegramBotUpdate update, DateTimeOffset claimedAt, DateTimeOffset version)
    {
        update.DeliveryClaimedAt = claimedAt;
        update.DeliveryAttemptCount += 1;
        update.DeliveryErrorText = string.Empty;
        update.UpdatedAt = version;
    }

    private static bool HasPendingDelivery(TelegramBotUpdate update)
        => HasPendingPreCheckout(update) || HasPendingMessage(update);

    private static bool HasPendingPreCheckout(TelegramBotUpdate update)
        => !string.IsNullOrWhiteSpace(update.PreCheckoutQueryId)
            && update.PreCheckoutOk.HasValue
            && !update.PreCheckoutAnsweredAt.HasValue;

    private static bool HasPendingMessage(TelegramBotUpdate update)
        => update.ResponseChatId.HasValue
            && !string.IsNullOrWhiteSpace(update.ResponseText)
            && !update.ResponseSentAt.HasValue;

    private static DateTimeOffset NextVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);
}
