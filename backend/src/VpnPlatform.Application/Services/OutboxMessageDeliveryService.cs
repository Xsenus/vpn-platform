using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Application.Services;

public sealed class OutboxMessageDeliveryService
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(1);
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly IOutboxMessageSink _sink;

    public OutboxMessageDeliveryService(IApplicationDbContext db, IClock clock, IOutboxMessageSink sink)
    {
        _db = db;
        _clock = clock;
        _sink = sink;
    }

    public async Task<IReadOnlyList<Guid>> GetDispatchableIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var staleBefore = now.Subtract(ProcessingLease);
        var limit = Math.Clamp(take, 1, 100);
        var query = _db.OutboxMessages.AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.FailedAt == null && x.Attempts < MaxAttempts);
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

        var result = new List<Guid>(limit);
        var offset = 0;
        while (result.Count < limit)
        {
            var batch = await query
                .OrderBy(x => x.Id)
                .Skip(offset)
                .Take(100)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            result.AddRange(batch
                .Where(x => (!x.NextAttemptAt.HasValue || x.NextAttemptAt <= now)
                    && (!x.ProcessingStartedAt.HasValue || x.ProcessingStartedAt <= staleBefore))
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

    public async Task<Result<OutboxMessageDeliveryResult>> DeliverAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireOutboxMessageAsync(messageId, cancellationToken);
        var now = _clock.UtcNow;
        var message = await _db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        if (message is null)
        {
            return Result<OutboxMessageDeliveryResult>.Failure("Outbox message was not found.");
        }

        if (message.ProcessedAt.HasValue || message.FailedAt.HasValue)
        {
            var status = message.ProcessedAt.HasValue ? "processed" : "failed";
            return Result<OutboxMessageDeliveryResult>.Success(
                new OutboxMessageDeliveryResult(messageId, message.ProcessedAt.HasValue, status));
        }

        if (message.NextAttemptAt > now)
        {
            return Result<OutboxMessageDeliveryResult>.Failure("Outbox retry is scheduled.", isRetryable: true);
        }

        if (message.ProcessingStartedAt > now.Subtract(ProcessingLease))
        {
            return Result<OutboxMessageDeliveryResult>.Failure("Outbox message processing is already in progress.", isRetryable: true);
        }

        if (message.Attempts >= MaxAttempts)
        {
            await MarkFailedAsync(message, "Outbox message reached the maximum number of attempts.", cancellationToken);
            return Result<OutboxMessageDeliveryResult>.Failure("Outbox message reached the maximum number of attempts.");
        }

        var expectedAttempts = message.Attempts;
        var expectedVersion = message.UpdatedAt;
        var claimVersion = NextVersion(expectedVersion, now);
        if (_db is DbContext dbContext
            && !string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            var claimed = await _db.OutboxMessages
                .Where(x => x.Id == message.Id
                    && x.ProcessedAt == null
                    && x.FailedAt == null
                    && x.Attempts == expectedAttempts
                    && x.UpdatedAt == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                    .SetProperty(x => x.ProcessingStartedAt, now)
                    .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.LastError, (string?)null)
                    .SetProperty(x => x.UpdatedAt, claimVersion), cancellationToken);
            if (claimed == 0)
            {
                return Result<OutboxMessageDeliveryResult>.Failure("Outbox message was claimed concurrently.", isRetryable: true);
            }

            await dbContext.Entry(message).ReloadAsync(cancellationToken);
        }
        else
        {
            ApplyClaim(message, now, claimVersion);
            await _db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            await _sink.DispatchAsync(message.Id, message.Type, message.CorrelationId, message.PayloadJson, cancellationToken);
            message.ProcessedAt = _clock.UtcNow;
            message.ProcessingStartedAt = null;
            message.NextAttemptAt = null;
            message.LastError = null;
            message.UpdatedAt = NextVersion(message.UpdatedAt, message.ProcessedAt.Value);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<OutboxMessageDeliveryResult>.Success(
                new OutboxMessageDeliveryResult(messageId, true, "processed"));
        }
        catch (OutboxPermanentDispatchException ex)
        {
            await MarkFailedAsync(message, ex.Message, CancellationToken.None);
            return Result<OutboxMessageDeliveryResult>.Failure("Outbox message was rejected by its handler.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkRetryAsync(message, "Outbox message processing was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            await MarkRetryAsync(message, ex.Message);
            var retryable = message.FailedAt is null;
            return Result<OutboxMessageDeliveryResult>.Failure(
                retryable
                    ? "Outbox processing failed and was scheduled for retry."
                    : "Outbox processing failed after the maximum number of attempts.",
                isRetryable: retryable);
        }
    }

    private async Task MarkRetryAsync(OutboxMessage message, string error)
    {
        var now = _clock.UtcNow;
        var retryable = message.Attempts < MaxAttempts;
        message.ProcessingStartedAt = null;
        message.NextAttemptAt = retryable ? now.AddSeconds(BackoffSeconds(message.Attempts)) : null;
        message.FailedAt = retryable ? null : now;
        message.LastError = SensitiveDataRedactor.Redact(error, maxLength: 500);
        message.UpdatedAt = NextVersion(message.UpdatedAt, now);
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task MarkFailedAsync(OutboxMessage message, string error, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        message.ProcessingStartedAt = null;
        message.NextAttemptAt = null;
        message.FailedAt = now;
        message.LastError = SensitiveDataRedactor.Redact(error, maxLength: 500);
        message.UpdatedAt = NextVersion(message.UpdatedAt, now);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyClaim(OutboxMessage message, DateTimeOffset now, DateTimeOffset version)
    {
        message.Attempts += 1;
        message.ProcessingStartedAt = now;
        message.NextAttemptAt = null;
        message.LastError = null;
        message.UpdatedAt = version;
    }

    private bool IsSqlite()
        => _db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);

    private static double BackoffSeconds(int attempts)
        => Math.Min(300, Math.Pow(2, Math.Min(attempts, 10)) * 5);

    private static DateTimeOffset NextVersion(DateTimeOffset current, DateTimeOffset now)
        => now > current ? now : current.AddTicks(1);
}

public sealed class OutboxPermanentDispatchException : Exception
{
    public OutboxPermanentDispatchException(string message) : base(message)
    {
    }
}
