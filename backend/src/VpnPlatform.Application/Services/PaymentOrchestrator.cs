using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

public class PaymentOrchestrator : IPaymentWebhookProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly OrderStatus[] TerminalOrderStatuses =
    {
        OrderStatus.Completed,
        OrderStatus.Cancelled,
        OrderStatus.Expired,
        OrderStatus.Refunded
    };

    private static readonly RefundStatus[] UnresolvedRefundStatuses =
    {
        RefundStatus.New,
        RefundStatus.Pending,
        RefundStatus.Unknown
    };

    private static readonly TimeSpan WebhookClaimTimeout = TimeSpan.FromMinutes(10);

    private readonly IApplicationDbContext _db;
    private readonly IPaymentProviderFactory _paymentProviderFactory;
    private readonly IEnumerable<IPaymentWebhookVerifier> _webhookVerifiers;
    private readonly PaymentProviderAccountService _providerAccounts;
    private readonly SubscriptionService _subscriptionService;
    private readonly IClock _clock;

    public PaymentOrchestrator(
        IApplicationDbContext db,
        IPaymentProviderFactory paymentProviderFactory,
        IEnumerable<IPaymentWebhookVerifier> webhookVerifiers,
        PaymentProviderAccountService providerAccounts,
        SubscriptionService subscriptionService,
        IClock clock)
    {
        _db = db;
        _paymentProviderFactory = paymentProviderFactory;
        _webhookVerifiers = webhookVerifiers;
        _providerAccounts = providerAccounts;
        _subscriptionService = subscriptionService;
        _clock = clock;
    }

    public async Task<Result<PaymentInitResult>> InitPaymentAsync(PaymentInitCommand command, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result<PaymentInitResult>.Failure("Order not found.");
        }

        await using var processingGate = await PaymentProcessingGate.AcquireOrderAsync(order.Id, cancellationToken);
        var currentOrder = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.OrderId, cancellationToken);
        if (currentOrder is null)
        {
            return Result<PaymentInitResult>.Failure("Order not found.");
        }

        RestoreTrackedOrder(order, currentOrder);
        if (HasReceivedPayment(currentOrder))
        {
            return Result<PaymentInitResult>.Failure("Order is already paid.");
        }

        if (currentOrder.Status == OrderStatus.Cancelled)
        {
            return Result<PaymentInitResult>.Failure("Order is cancelled.");
        }

        if (currentOrder.Status == OrderStatus.Expired || currentOrder.ExpiresAt <= _clock.UtcNow)
        {
            StatusStateMachine.SetOrderStatus(order, OrderStatus.Expired, _clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PaymentInitResult>.Failure("Order expired.");
        }

        if (currentOrder.UserId == Guid.Empty)
        {
            return Result<PaymentInitResult>.Failure("Order must be bound to a user before payment initialization.");
        }

        var accountResult = await _providerAccounts.GetWebCheckoutAccountEntityAsync(command.Provider, cancellationToken);
        if (!accountResult.IsSuccess || accountResult.Value is null)
        {
            return Result<PaymentInitResult>.Failure(accountResult.Error ?? "Payment provider account is not configured.");
        }

        var account = accountResult.Value;
        var provider = _paymentProviderFactory.Get(command.Provider);
        var now = _clock.UtcNow;
        var returnUrl = !string.IsNullOrWhiteSpace(command.ReturnUrl)
            ? command.ReturnUrl.Trim()
            : !string.IsNullOrWhiteSpace(account.ReturnUrl)
                ? account.ReturnUrl
                : "http://localhost:5174/payments";

        var existingPendingCandidates = await _db.Payments
            .Where(x =>
                x.OrderId == currentOrder.Id &&
                x.Provider == command.Provider &&
                x.PaymentProviderAccountId == account.Id &&
                (x.Status == PaymentStatus.New || x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.WaitingConfirmation))
            .ToListAsync(cancellationToken);
        var existingPending = existingPendingCandidates
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (existingPending is not null && !string.IsNullOrWhiteSpace(existingPending.ConfirmationUrl))
        {
            return Result<PaymentInitResult>.Success(new PaymentInitResult(existingPending.ProviderPaymentId, existingPending.ConfirmationUrl, existingPending.RawResponse));
        }

        var payment = existingPending ?? new PaymentAttempt
        {
            OrderId = currentOrder.Id,
            PaymentProviderAccountId = account.Id,
            Provider = command.Provider,
            ProviderMode = account.Mode,
            Amount = currentOrder.Amount,
            Currency = currentOrder.Currency,
            Status = PaymentStatus.New,
            ProviderPaymentId = $"local_{Guid.NewGuid():N}",
            IdempotencyKey = BuildPaymentIdempotencyKey(currentOrder.Id, command.Provider, account.Id),
            ReturnUrl = returnUrl,
            RawRequest = "{}",
            RawResponse = "{}"
        };

        if (existingPending is null)
        {
            _db.Payments.Add(payment);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _db.Payments.Remove(payment);
                try
                {
                    var concurrentPayment = await _db.Payments.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.IdempotencyKey == payment.IdempotencyKey, cancellationToken);
                    if (concurrentPayment is not null && !string.IsNullOrWhiteSpace(concurrentPayment.ConfirmationUrl))
                    {
                        return Result<PaymentInitResult>.Success(new PaymentInitResult(concurrentPayment.ProviderPaymentId, concurrentPayment.ConfirmationUrl, concurrentPayment.RawResponse));
                    }

                    if (concurrentPayment is not null)
                    {
                        return Result<PaymentInitResult>.Failure("Payment initialization is already in progress; retry shortly.");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // No provider call has happened, so a failed conflict lookup remains fail-closed.
                }

                return Result<PaymentInitResult>.Failure("Payment reservation could not be saved; the payment provider was not called.");
            }
        }
        else
        {
            payment.ReturnUrl = returnUrl;
            payment.UpdatedAt = now;
        }

        PaymentInitResult init;
        try
        {
            init = await provider.CreatePaymentAsync(new PaymentCreateRequest(currentOrder, payment, account, returnUrl), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            payment.StatusReason = "Payment provider call was cancelled before its outcome could be confirmed.";
            payment.UpdatedAt = now;
            await TrySavePaymentStateAsync();
            throw;
        }
        catch (Exception ex)
        {
            payment.StatusReason = ex.Message;
            payment.UpdatedAt = now;
            await TrySavePaymentStateAsync();
            return Result<PaymentInitResult>.Failure(ex.Message);
        }

        payment.ProviderPaymentId = init.PaymentId;
        payment.RawResponse = init.RawResponse;
        payment.ConfirmationUrl = init.RedirectUrl;
        payment.StatusReason = string.Empty;
        StatusStateMachine.SetPaymentStatus(payment, PaymentStatus.Pending, now);
        payment.ProviderMode = account.Mode;
        payment.PaymentProviderAccountId = account.Id;
        RestoreTrackedOrder(order, currentOrder);
        StatusStateMachine.SetOrderStatus(order, OrderStatus.PendingPayment, now);
        order.PaymentProvider = command.Provider;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PaymentInitResult>.Success(init);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TrySavePaymentStateAsync();
            throw;
        }
        catch
        {
            if (await TrySavePaymentStateAsync())
            {
                return Result<PaymentInitResult>.Success(init);
            }

            return Result<PaymentInitResult>.Failure("Payment was created by the provider but its local outcome could not be finalized; retry with the same order before creating another payment.");
        }
    }

    private async Task<bool> TrySavePaymentStateAsync()
    {
        try
        {
            await _db.SaveChangesAsync(CancellationToken.None);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasReceivedPayment(Order order)
        => order.PaidAt.HasValue
            || order.Status is OrderStatus.PaymentReceived
                or OrderStatus.FulfillmentInProgress
                or OrderStatus.PartiallyProcessed
                or OrderStatus.Completed
                or OrderStatus.Refunded;

    private static void RestoreTrackedOrder(Order order, Order currentOrder)
    {
        order.Status = currentOrder.Status;
        order.PaymentProvider = currentOrder.PaymentProvider;
        order.PaidAt = currentOrder.PaidAt;
        order.UpdatedAt = currentOrder.UpdatedAt;
    }

    public Task<Result<string>> HandleWebhookAsync(PaymentProvider provider, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default)
        => ProcessAsync(provider, rawBody, headers, cancellationToken);

    public async Task<Result<string>> ProcessAsync(PaymentProvider providerType, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var provider = _paymentProviderFactory.Get(providerType);
        PaymentWebhookParseResult parsed;
        try
        {
            parsed = await provider.ParseWebhookAsync(rawBody, headers, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await SaveRejectedWebhookAsync(providerType, string.Empty, "parse_error", string.Empty, rawBody, headers, ex.Message, cancellationToken);
            return Result<string>.Failure("Webhook parse failed.");
        }

        var payloadSha = Sha256(rawBody);
        var webhookEventId = BuildWebhookEventId(parsed, payloadSha);
        await using var webhookGate = await PaymentProcessingGate.AcquireWebhookAsync(providerType.ToString(), webhookEventId, parsed.PaymentId, cancellationToken);
        var now = _clock.UtcNow;
        var existingEvent = await _db.PaymentWebhookEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == providerType && x.ExternalEventId == webhookEventId && x.ProviderPaymentId == parsed.PaymentId, cancellationToken);
        PaymentWebhookEvent webhookEvent;
        if (existingEvent is not null)
        {
            if (IsTerminalWebhookEvent(existingEvent.Status))
            {
                return Result<string>.Success("Webhook already processed.");
            }

            if (!await TryClaimWebhookEventAsync(existingEvent, now, cancellationToken))
            {
                return await ResolveUnclaimedWebhookAsync(existingEvent.Id, cancellationToken);
            }

            webhookEvent = await GetTrackedWebhookEventAsync(existingEvent.Id, cancellationToken);
        }
        else
        {
            webhookEvent = new PaymentWebhookEvent
            {
                Provider = providerType,
                ProviderPaymentId = parsed.PaymentId,
                ExternalEventId = webhookEventId,
                EventType = parsed.EventType,
                PayloadSha256 = payloadSha,
                RawPayload = rawBody,
                HeadersJson = SerializeSafeHeaders(headers),
                SignatureValidated = false,
                Status = PaymentWebhookEventStatus.Received,
                ReceivedAt = now
            };
            _db.PaymentWebhookEvents.Add(webhookEvent);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _db.PaymentWebhookEvents.Remove(webhookEvent);
                existingEvent = await _db.PaymentWebhookEvents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Provider == providerType && x.ExternalEventId == webhookEventId && x.ProviderPaymentId == parsed.PaymentId, cancellationToken);
                if (existingEvent is null)
                {
                    throw;
                }

                if (IsTerminalWebhookEvent(existingEvent.Status))
                {
                    return Result<string>.Success("Webhook already processed.");
                }

                if (!await TryClaimWebhookEventAsync(existingEvent, now, cancellationToken))
                {
                    return await ResolveUnclaimedWebhookAsync(existingEvent.Id, cancellationToken);
                }

                webhookEvent = await GetTrackedWebhookEventAsync(existingEvent.Id, cancellationToken);
            }
        }

        webhookEvent.EventType = parsed.EventType;
        webhookEvent.PayloadSha256 = payloadSha;
        webhookEvent.RawPayload = rawBody;
        webhookEvent.HeadersJson = SerializeSafeHeaders(headers);
        webhookEvent.SignatureValidated = false;
        webhookEvent.ProcessedAt = null;
        webhookEvent.ErrorText = string.Empty;

        var paymentQuery = _db.Payments
            .Include(x => x.Order)
            .Include(x => x.PaymentProviderAccount)
            .Where(x => x.Provider == providerType && x.ProviderPaymentId == parsed.PaymentId);
        if (!string.IsNullOrWhiteSpace(parsed.ProviderAccountExternalId))
        {
            paymentQuery = paymentQuery.Where(x => x.PaymentProviderAccount != null && x.PaymentProviderAccount.ShopId == parsed.ProviderAccountExternalId);
        }
        var payment = await paymentQuery.FirstOrDefaultAsync(cancellationToken);
        webhookEvent.PaymentAttemptId = payment?.Id;
        webhookEvent.PaymentProviderAccountId = payment?.PaymentProviderAccountId;

        if (payment is null || payment.Order is null || payment.PaymentProviderAccount is null)
        {
            var providerAccountMismatch = !string.IsNullOrWhiteSpace(parsed.ProviderAccountExternalId)
                && await _db.Payments.AsNoTracking()
                    .AnyAsync(x => x.Provider == providerType && x.ProviderPaymentId == parsed.PaymentId, cancellationToken);
            webhookEvent.Status = providerAccountMismatch
                ? PaymentWebhookEventStatus.Rejected
                : PaymentWebhookEventStatus.Failed;
            webhookEvent.ErrorText = providerAccountMismatch
                ? "Payment provider account does not match payment attempt."
                : "Payment attempt not found.";
            webhookEvent.ProcessedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<string>.Failure(webhookEvent.ErrorText, isRetryable: !providerAccountMismatch);
        }

        var validation = ValidateParsedWebhook(payment, parsed);
        if (!validation.IsSuccess)
        {
            webhookEvent.Status = PaymentWebhookEventStatus.Rejected;
            webhookEvent.ErrorText = validation.Error ?? "Webhook does not match payment attempt.";
            webhookEvent.ProcessedAt = _clock.UtcNow;
            payment.WebhookPayload = rawBody;
            payment.StatusReason = validation.Error ?? string.Empty;
            payment.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<string>.Failure(validation.Error ?? "Webhook rejected.");
        }

        var verifier = _webhookVerifiers.FirstOrDefault(x => x.Provider == providerType);
        if (verifier is null)
        {
            webhookEvent.Status = PaymentWebhookEventStatus.Failed;
            webhookEvent.ErrorText = "Webhook verifier is not registered.";
            webhookEvent.ProcessedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<string>.Failure("Webhook verifier is not registered.", isRetryable: true);
        }

        PaymentWebhookVerificationResult verification;
        try
        {
            verification = await verifier.VerifyAsync(payment.PaymentProviderAccount, parsed, rawBody, headers, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkWebhookFailed(webhookEvent, "Webhook verification was cancelled before its outcome could be confirmed.");
            await TrySavePaymentStateAsync();
            throw;
        }
        catch (Exception ex)
        {
            MarkWebhookFailed(webhookEvent, ex.Message);
            await TrySavePaymentStateAsync();
            return Result<string>.Failure("Webhook verification failed.", isRetryable: true);
        }

        webhookEvent.SignatureValidated = verification.IsValid;
        payment.SignatureValidated = verification.IsValid;

        if (!verification.IsValid)
        {
            webhookEvent.Status = PaymentWebhookEventStatus.Rejected;
            webhookEvent.ErrorText = verification.Error ?? "Invalid webhook authenticity.";
            webhookEvent.ProcessedAt = _clock.UtcNow;
            payment.WebhookPayload = rawBody;
            payment.StatusReason = verification.Error ?? string.Empty;
            payment.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<string>.Failure("Invalid webhook authenticity.");
        }

        webhookEvent.Status = PaymentWebhookEventStatus.Verified;
        Result<string> result;
        try
        {
            result = await ApplyPaymentStatusAsync(payment, parsed.Status, rawBody, webhookEventId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MarkWebhookFailed(webhookEvent, "Webhook status application was cancelled before its outcome could be confirmed.");
            await TrySavePaymentStateAsync();
            throw;
        }
        catch (Exception ex)
        {
            MarkWebhookFailed(webhookEvent, ex.Message);
            await TrySavePaymentStateAsync();
            return Result<string>.Failure("Webhook processing failed.", isRetryable: true);
        }

        webhookEvent.Status = result.IsSuccess
            ? PaymentWebhookEventStatus.Processed
            : result.IsRetryable
                ? PaymentWebhookEventStatus.Failed
                : PaymentWebhookEventStatus.Rejected;
        webhookEvent.ErrorText = result.Error ?? string.Empty;
        webhookEvent.ProcessedAt = _clock.UtcNow;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TrySavePaymentStateAsync();
            throw;
        }
        catch
        {
            if (!await TrySavePaymentStateAsync())
            {
                return Result<string>.Failure("Webhook outcome could not be finalized; retry the same event.", isRetryable: true);
            }
        }

        return result.IsSuccess
            ? Result<string>.Success("Webhook processed.")
            : Result<string>.Failure(result.Error ?? "Webhook processing failed.", result.IsRetryable);
    }

    private async Task<bool> TryClaimWebhookEventAsync(PaymentWebhookEvent existingEvent, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var staleBefore = now - WebhookClaimTimeout;
        IQueryable<PaymentWebhookEvent> claimable;
        if (existingEvent.Status == PaymentWebhookEventStatus.Failed)
        {
            claimable = _db.PaymentWebhookEvents.Where(x => x.Id == existingEvent.Id && x.Status == PaymentWebhookEventStatus.Failed);
        }
        else if (existingEvent.Status is PaymentWebhookEventStatus.Received or PaymentWebhookEventStatus.Verified
            && existingEvent.ReceivedAt <= staleBefore)
        {
            claimable = _db.PaymentWebhookEvents.Where(x => x.Id == existingEvent.Id
                && x.Status == existingEvent.Status
                && x.ReceivedAt == existingEvent.ReceivedAt);
        }
        else
        {
            return false;
        }

        var claimed = await claimable
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, PaymentWebhookEventStatus.Received)
                .SetProperty(x => x.SignatureValidated, false)
                .SetProperty(x => x.ReceivedAt, now)
                .SetProperty(x => x.ProcessedAt, (DateTimeOffset?)null)
                .SetProperty(x => x.ErrorText, string.Empty)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        return claimed == 1;
    }

    private async Task<PaymentWebhookEvent> GetTrackedWebhookEventAsync(Guid webhookEventId, CancellationToken cancellationToken)
    {
        var webhookEvent = _db.PaymentWebhookEvents.Local.FirstOrDefault(x => x.Id == webhookEventId)
            ?? await _db.PaymentWebhookEvents.FirstAsync(x => x.Id == webhookEventId, cancellationToken);
        webhookEvent.Status = PaymentWebhookEventStatus.Received;
        webhookEvent.SignatureValidated = false;
        webhookEvent.ReceivedAt = _clock.UtcNow;
        webhookEvent.ProcessedAt = null;
        webhookEvent.ErrorText = string.Empty;
        return webhookEvent;
    }

    private async Task<Result<string>> ResolveUnclaimedWebhookAsync(Guid webhookEventId, CancellationToken cancellationToken)
    {
        var status = await _db.PaymentWebhookEvents.AsNoTracking()
            .Where(x => x.Id == webhookEventId)
            .Select(x => (PaymentWebhookEventStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken);
        return status.HasValue && IsTerminalWebhookEvent(status.Value)
            ? Result<string>.Success("Webhook already processed.")
            : Result<string>.Failure("Webhook processing is already in progress; retry shortly.", isRetryable: true);
    }

    private static bool IsTerminalWebhookEvent(PaymentWebhookEventStatus status)
        => status is PaymentWebhookEventStatus.Processed or PaymentWebhookEventStatus.Rejected or PaymentWebhookEventStatus.Duplicate;

    private void MarkWebhookFailed(PaymentWebhookEvent webhookEvent, string error)
    {
        webhookEvent.Status = PaymentWebhookEventStatus.Failed;
        webhookEvent.ErrorText = error;
        webhookEvent.ProcessedAt = _clock.UtcNow;
    }

    public async Task<Result<PaymentStatusResult>> RecheckPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .Include(x => x.Order)
            .Include(x => x.PaymentProviderAccount)
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
        if (payment is null || payment.PaymentProviderAccount is null)
        {
            return Result<PaymentStatusResult>.Failure("Payment attempt not found.");
        }

        try
        {
            var provider = _paymentProviderFactory.Get(payment.Provider);
            var statusResult = await provider.GetStatusAsync(payment, payment.PaymentProviderAccount, cancellationToken);
            var apply = await ApplyPaymentStatusAsync(payment, statusResult.Status, statusResult.RawResponse, $"manual-recheck:{payment.Id}:{statusResult.Status}", cancellationToken);
            return apply.IsSuccess ? Result<PaymentStatusResult>.Success(statusResult) : Result<PaymentStatusResult>.Failure(apply.Error ?? "Status recheck failed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (NotSupportedException ex)
        {
            return Result<PaymentStatusResult>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<PaymentStatusResult>.Failure(ex.Message);
        }
    }

    public async Task<Result<RefundDto>> RefundPaymentAsync(Guid paymentId, decimal amount, string reason, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .Include(x => x.Order)
            .Include(x => x.PaymentProviderAccount)
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
        if (payment is null || payment.PaymentProviderAccount is null)
        {
            return Result<RefundDto>.Failure("Payment attempt not found.");
        }

        var refundIdempotencyKey = BuildRefundIdempotencyKey(payment.Id, amount, reason);
        await using var processingGate = await PaymentProcessingGate.AcquireOrderAsync(payment.OrderId, cancellationToken);

        var existing = await _db.Refunds
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PaymentAttemptId == payment.Id && x.IdempotencyKey == refundIdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Result<RefundDto>.Success(MapRefund(existing));
        }

        var currentPayment = await _db.Payments
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.PaymentProviderAccount)
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
        if (currentPayment is null || currentPayment.PaymentProviderAccount is null)
        {
            return Result<RefundDto>.Failure("Payment attempt not found.");
        }

        var hasUnresolvedRefund = await _db.Refunds.AsNoTracking()
            .AnyAsync(x => x.PaymentAttemptId == payment.Id && UnresolvedRefundStatuses.Contains(x.Status), cancellationToken);
        if (hasUnresolvedRefund)
        {
            return Result<RefundDto>.Failure("Payment has an unfinished refund that requires provider reconciliation before another refund.");
        }

        if (currentPayment.Status != PaymentStatus.Succeeded && currentPayment.Status != PaymentStatus.PartiallyRefunded)
        {
            return Result<RefundDto>.Failure("Only succeeded or partially refunded payments can be refunded.");
        }

        if (amount <= 0 || amount > currentPayment.Amount - currentPayment.RefundedAmount)
        {
            return Result<RefundDto>.Failure("Refund amount is invalid.");
        }

        var refund = new Refund
        {
            PaymentAttemptId = currentPayment.Id,
            Provider = currentPayment.Provider,
            ProviderRefundId = $"pending:{Guid.NewGuid():N}",
            IdempotencyKey = refundIdempotencyKey,
            Status = RefundStatus.New,
            Amount = amount,
            Currency = currentPayment.Currency,
            Reason = reason,
            RawRequest = JsonSerializer.Serialize(new { paymentId = currentPayment.Id, amount, currency = currentPayment.Currency, reason, idempotencyKey = refundIdempotencyKey }),
            RawResponse = "{}"
        };
        _db.Refunds.Add(refund);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.Refunds.Remove(refund);
            try
            {
                existing = await _db.Refunds
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PaymentAttemptId == payment.Id && x.IdempotencyKey == refundIdempotencyKey, cancellationToken);
                if (existing is not null)
                {
                    return Result<RefundDto>.Success(MapRefund(existing));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // The provider has not been called yet, so a database read failure is still fail-closed.
            }

            return Result<RefundDto>.Failure("Refund reservation could not be saved; the payment provider was not called.");
        }

        PaymentRefundResult refundResult;
        try
        {
            var provider = _paymentProviderFactory.Get(currentPayment.Provider);
            refundResult = await provider.RefundAsync(currentPayment, currentPayment.PaymentProviderAccount, amount, reason, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkRefundUnresolvedAsync(refund, payment, currentPayment);
            throw;
        }
        catch (Exception ex)
        {
            var persisted = await MarkRefundUnresolvedAsync(refund, payment, currentPayment);
            var suffix = persisted ? string.Empty : " Local reconciliation also failed.";
            return Result<RefundDto>.Failure($"Refund provider outcome is unknown and requires manual reconciliation. {ex.Message}{suffix}");
        }

        refund.ProviderRefundId = string.IsNullOrWhiteSpace(refundResult.RefundId) ? refund.ProviderRefundId : refundResult.RefundId;
        refund.Status = refundResult.Status;
        refund.RawResponse = refundResult.RawResponse;
        refund.RefundedAt = refundResult.Status == RefundStatus.Succeeded ? _clock.UtcNow : null;

        RestoreTrackedPayment(payment, currentPayment);
        if (refund.Status == RefundStatus.Succeeded)
        {
            payment.RefundedAmount = currentPayment.RefundedAmount + amount;
            payment.RefundedAt = _clock.UtcNow;
            var nextPaymentStatus = payment.RefundedAmount >= payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
            StatusStateMachine.SetPaymentStatus(payment, nextPaymentStatus, _clock.UtcNow);
            if (payment.Order is not null && payment.Status == PaymentStatus.Refunded)
            {
                StatusStateMachine.SetOrderStatus(payment.Order, OrderStatus.Refunded, _clock.UtcNow);
            }
        }

        payment.UpdatedAt = _clock.UtcNow;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return Result<RefundDto>.Success(MapRefund(refund));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkRefundUnresolvedAsync(refund, payment, currentPayment);
            throw;
        }
        catch (Exception ex)
        {
            var persisted = await MarkRefundUnresolvedAsync(refund, payment, currentPayment);
            var suffix = persisted ? string.Empty : " The durable reservation remains in New status because local reconciliation also failed.";
            return Result<RefundDto>.Failure($"Refund was accepted by the provider but local finalization failed; manual reconciliation is required. {ex.Message}{suffix}");
        }
    }

    private async Task<bool> MarkRefundUnresolvedAsync(Refund refund, PaymentAttempt payment, PaymentAttempt currentPayment)
    {
        refund.Status = RefundStatus.Unknown;
        refund.RefundedAt = null;
        RestoreTrackedPayment(payment, currentPayment);
        try
        {
            await _db.SaveChangesAsync(CancellationToken.None);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RestoreTrackedPayment(PaymentAttempt payment, PaymentAttempt currentPayment)
    {
        payment.Status = currentPayment.Status;
        payment.RefundedAmount = currentPayment.RefundedAmount;
        payment.RefundedAt = currentPayment.RefundedAt;
        payment.UpdatedAt = currentPayment.UpdatedAt;
        if (payment.Order is not null && currentPayment.Order is not null)
        {
            payment.Order.Status = currentPayment.Order.Status;
            payment.Order.UpdatedAt = currentPayment.Order.UpdatedAt;
        }
    }

    private async Task<Result<string>> ApplyPaymentStatusAsync(PaymentAttempt payment, PaymentStatus status, string rawPayload, string externalEventId, CancellationToken cancellationToken)
    {
        await using var processingGate = await PaymentProcessingGate.AcquireOrderAsync(payment.OrderId, cancellationToken);
        var now = _clock.UtcNow;
        var currentPayment = await _db.Payments
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.PaymentProviderAccount)
            .FirstOrDefaultAsync(x => x.Id == payment.Id, cancellationToken);
        if (currentPayment is not null)
        {
            if (status == PaymentStatus.Succeeded && currentPayment.IsActivationProcessed)
            {
                return Result<string>.Success("Payment already activated.");
            }

            payment.Status = currentPayment.Status;
            payment.IsActivationProcessed = currentPayment.IsActivationProcessed;
            payment.ActivationProcessedAt = currentPayment.ActivationProcessedAt;
            payment.PaidAt = currentPayment.PaidAt;
            payment.FailedAt = currentPayment.FailedAt;
            payment.RefundedAt = currentPayment.RefundedAt;
            payment.RefundedAmount = currentPayment.RefundedAmount;
            payment.StatusReason = currentPayment.StatusReason;

            if (payment.Order is not null && currentPayment.Order is not null)
            {
                payment.Order.Status = currentPayment.Order.Status;
                payment.Order.PaidAt = currentPayment.Order.PaidAt;
            }
        }

        if (status == PaymentStatus.Succeeded
            && currentPayment?.Status == PaymentStatus.Succeeded
            && currentPayment.Order?.Status == OrderStatus.Completed)
        {
            payment.IsActivationProcessed = true;
            payment.ActivationProcessedAt ??= now;
            payment.PaidAt ??= currentPayment.PaidAt ?? currentPayment.Order.PaidAt ?? now;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<string>.Success("Payment activation marker reconciled.");
        }

        var previousStatus = payment.Status;
        var paymentTransition = StatusStateMachine.TrySetPaymentStatus(payment, status, now);
        if (!paymentTransition.IsSuccess)
        {
            payment.StatusReason = paymentTransition.Error ?? string.Empty;
            payment.WebhookPayload = rawPayload;
            payment.ExternalEventId = externalEventId;
            payment.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<string>.Failure(paymentTransition.Error ?? "Payment status transition is not allowed.");
        }

        payment.ExternalEventId = externalEventId;
        payment.WebhookPayload = rawPayload;

        if (status == PaymentStatus.Succeeded)
        {
            payment.PaidAt ??= now;
            if (payment.Order is not null)
            {
                payment.Order.PaidAt ??= now;
                if (payment.IsActivationProcessed || payment.Order.Status == OrderStatus.Completed)
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    return Result<string>.Success("Payment already activated.");
                }

                if (TerminalOrderStatuses.Contains(payment.Order.Status))
                {
                    return Result<string>.Failure($"Order is in terminal status {payment.Order.Status}.");
                }

                var orderPaymentReceived = StatusStateMachine.TrySetOrderStatus(payment.Order, OrderStatus.PaymentReceived, now);
                if (!orderPaymentReceived.IsSuccess)
                {
                    return Result<string>.Failure(orderPaymentReceived.Error ?? "Order status transition is not allowed.");
                }

                var activation = await _subscriptionService.ActivateOrRenewFromOrderAsync(payment.Order, payment, cancellationToken);
                if (!activation.IsSuccess)
                {
                    StatusStateMachine.SetOrderStatus(payment.Order, OrderStatus.PartiallyProcessed, now);
                    payment.StatusReason = activation.Error ?? string.Empty;
                    await _db.SaveChangesAsync(cancellationToken);
                    return Result<string>.Failure(activation.Error ?? "Subscription activation failed.", isRetryable: true);
                }

                payment.IsActivationProcessed = true;
                payment.ActivationProcessedAt = now;

                var telegramAccounts = await _db.TelegramAccounts.AsNoTracking()
                    .Where(x => x.UserId == payment.Order.UserId && !x.IsBlocked)
                    .ToListAsync(cancellationToken);
                var payloadJson = await BuildPaymentSucceededTelegramPayloadAsync(payment.Order, activation.Value!, cancellationToken);
                foreach (var telegramAccount in telegramAccounts)
                {
                    var exists = await _db.TelegramBotNotifications.AsNoTracking()
                        .AnyAsync(x => x.TelegramUserId == telegramAccount.TelegramUserId && x.Type == "payment_succeeded" && x.PayloadJson == payloadJson && x.Status != "failed" && x.Status != "cancelled", cancellationToken);
                    if (!exists)
                    {
                        _db.TelegramBotNotifications.Add(new TelegramBotNotification
                        {
                            TelegramUserId = telegramAccount.TelegramUserId,
                            Type = "payment_succeeded",
                            PayloadJson = payloadJson,
                            Status = "pending",
                            NextAttemptAt = now
                        });
                    }
                }
            }
        }
        else if (status is PaymentStatus.Failed or PaymentStatus.Cancelled)
        {
            payment.FailedAt ??= now;
            if (payment.Order is not null && payment.Order.Status != OrderStatus.Completed)
            {
                StatusStateMachine.SetOrderStatus(payment.Order, OrderStatus.Failed, now);
                await QueuePaymentFailedTelegramNotificationsAsync(payment.Order, payment, status, now, cancellationToken);
            }
        }
        else if (status == PaymentStatus.Refunded)
        {
            payment.RefundedAt ??= now;
            if (payment.Order is not null)
            {
                StatusStateMachine.SetOrderStatus(payment.Order, OrderStatus.Refunded, now);
            }
        }
        else if (status == PaymentStatus.WaitingConfirmation && payment.Order is not null)
        {
            StatusStateMachine.SetOrderStatus(payment.Order, OrderStatus.PendingPayment, now);
        }

        if (previousStatus != status)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                ActorType = "system",
                ActorId = "payment-orchestrator",
                Action = "payment.status.changed",
                EntityType = nameof(PaymentAttempt),
                EntityId = payment.Id.ToString(),
                BeforeJson = JsonSerializer.Serialize(new { status = previousStatus.ToString(), payment.OrderId, payment.Provider, payment.ProviderPaymentId }, JsonOptions),
                AfterJson = JsonSerializer.Serialize(new { status = payment.Status.ToString(), orderStatus = payment.Order?.Status.ToString(), payment.IsActivationProcessed, payment.PaidAt, externalEventId }, JsonOptions),
                CreatedAt = now
            });
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "PaymentStatusChanged",
                CorrelationId = $"{payment.Id:N}:{payment.Status}",
                PayloadJson = $$"""
                {
                  "paymentId": "{{payment.Id}}",
                  "orderId": "{{payment.OrderId}}",
                  "provider": "{{payment.Provider}}",
                  "status": "{{payment.Status}}"
                }
                """
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<string>.Success("Payment status applied.");
    }

    private Result<string> ValidateParsedWebhook(PaymentAttempt payment, PaymentWebhookParseResult parsed)
    {
        if (parsed.Status == PaymentStatus.Unknown)
        {
            return Result<string>.Failure("Webhook payment status is unknown.");
        }

        if (payment.ProviderPaymentId != parsed.PaymentId)
        {
            return Result<string>.Failure("Webhook payment id does not match payment attempt.");
        }

        if (!string.IsNullOrWhiteSpace(parsed.ProviderAccountExternalId) && payment.PaymentProviderAccount?.ShopId != parsed.ProviderAccountExternalId)
        {
            return Result<string>.Failure("Webhook provider account does not match payment attempt.");
        }

        if (!string.IsNullOrWhiteSpace(parsed.InternalOrderId)
            && !string.Equals(parsed.InternalOrderId, payment.OrderId.ToString("N"), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parsed.InternalOrderId, payment.OrderId.ToString(), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parsed.InternalOrderId, payment.Id.ToString("N"), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parsed.InternalOrderId, payment.Id.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Failure("Webhook order id does not match payment attempt.");
        }

        if (parsed.Amount.HasValue && decimal.Round(parsed.Amount.Value, 2) != decimal.Round(payment.Amount, 2))
        {
            return Result<string>.Failure("Webhook amount does not match payment attempt.");
        }

        if (!string.IsNullOrWhiteSpace(parsed.Currency) && !string.Equals(NormalizeCurrency(parsed.Currency), NormalizeCurrency(payment.Currency), StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Failure("Webhook currency does not match payment attempt.");
        }

        if (parsed.Status == PaymentStatus.Succeeded)
        {
            if (parsed.Paid.HasValue && !parsed.Paid.Value)
            {
                return Result<string>.Failure("Webhook succeeded status is not marked as paid.");
            }

            if (payment.IsActivationProcessed)
            {
                return Result<string>.Success("Payment is already activation-processed.");
            }

            if (payment.Order is null)
            {
                return Result<string>.Failure("Payment order is missing.");
            }

            if (payment.Order.Status == OrderStatus.Completed
                && payment.Status == PaymentStatus.Succeeded
                && payment.PaidAt.HasValue)
            {
                return Result<string>.Success("Completed payment requires activation marker reconciliation.");
            }

            if (payment.Order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.Refunded)
            {
                return Result<string>.Failure($"Order is in terminal status {payment.Order.Status}.");
            }
        }

        return Result<string>.Success("Webhook matches payment attempt.");
    }

    private async Task SaveRejectedWebhookAsync(PaymentProvider provider, string providerPaymentId, string externalEventId, string eventType, string rawBody, IReadOnlyDictionary<string, string> headers, string error, CancellationToken cancellationToken)
    {
        var webhookEvent = new PaymentWebhookEvent
        {
            Provider = provider,
            ProviderPaymentId = providerPaymentId,
            ExternalEventId = string.IsNullOrWhiteSpace(externalEventId) ? $"parse-error:{Sha256(rawBody)}" : externalEventId,
            EventType = eventType,
            PayloadSha256 = Sha256(rawBody),
            RawPayload = rawBody,
            HeadersJson = SerializeSafeHeaders(headers),
            Status = PaymentWebhookEventStatus.Rejected,
            ErrorText = error,
            ReceivedAt = _clock.UtcNow,
            ProcessedAt = _clock.UtcNow
        };
        _db.PaymentWebhookEvents.Add(webhookEvent);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.PaymentWebhookEvents.Remove(webhookEvent);
            var duplicate = await _db.PaymentWebhookEvents.AsNoTracking()
                .AnyAsync(x => x.Provider == provider
                    && x.ExternalEventId == webhookEvent.ExternalEventId
                    && x.ProviderPaymentId == providerPaymentId, cancellationToken);
            if (!duplicate)
            {
                throw;
            }
        }
    }

    private async Task<string> BuildPaymentSucceededTelegramPayloadAsync(Order order, ActivationResult activation, CancellationToken cancellationToken)
    {
        var subscription = await _db.Subscriptions.AsNoTracking()
            .Include(x => x.Tariff)
            .FirstOrDefaultAsync(x => x.Id == activation.SubscriptionId, cancellationToken);

        AccessCredential? access = null;
        if (activation.AccessId.HasValue)
        {
            access = await _db.AccessCredentials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == activation.AccessId.Value, cancellationToken);
        }

        access ??= await _db.AccessCredentials.AsNoTracking()
            .Where(x => x.SubscriptionId == activation.SubscriptionId)
            .OrderByDescending(x => x.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var tariffName = subscription?.Tariff?.Name ?? order.TariffId.ToString("N")[..8];
        var expiresAt = subscription is null ? "—" : subscription.EndAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
        var accessUri = access is null || string.IsNullOrWhiteSpace(access.AccessUri)
            ? "Доступ готовится. Мы отправим ключ отдельным сообщением."
            : access.AccessUri;
        var qrPayload = access is null || string.IsNullOrWhiteSpace(access.QrCodePath)
            ? "QR payload пока не создан."
            : access.QrCodePath;
        var scenarioText = string.IsNullOrWhiteSpace(activation.TelegramText)
            ? activation.CabinetText
            : activation.TelegramText;
        var scenarioBlock = string.IsNullOrWhiteSpace(scenarioText)
            ? string.Empty
            : $"\n\n{scenarioText.Trim()}";
        var text = $"Оплата получена ✅\nЗаказ: {order.Id}\nТариф: {tariffName}\nПодписка действует до: {expiresAt}{scenarioBlock}\n\nVPN URI:\n{accessUri}\n\nQR payload:\n{qrPayload}\n\nИнструкция: импортируйте VPN URI в VLESS/Xray-compatible клиент. Если возникнут проблемы — нажмите «Поддержка».";
        return JsonSerializer.Serialize(new
        {
            text,
            replyMarkupJson = BuildPostPaymentReplyMarkupJson(),
            orderId = order.Id,
            activation.SubscriptionId,
            activation.AccessId,
            activation.ScenarioKey,
            scenarioText = scenarioText ?? string.Empty
        }, JsonOptions);
    }

    private async Task QueuePaymentFailedTelegramNotificationsAsync(Order order, PaymentAttempt payment, PaymentStatus status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var telegramAccounts = await _db.TelegramAccounts.AsNoTracking()
            .Where(x => x.UserId == order.UserId && !x.IsBlocked)
            .ToListAsync(cancellationToken);

        if (telegramAccounts.Count == 0)
        {
            return;
        }

        var payloadJson = BuildPaymentFailedTelegramPayload(order, payment, status);
        foreach (var telegramAccount in telegramAccounts)
        {
            var exists = await _db.TelegramBotNotifications.AsNoTracking()
                .AnyAsync(x => x.TelegramUserId == telegramAccount.TelegramUserId && x.Type == "payment_failed" && x.PayloadJson == payloadJson && x.Status != "failed" && x.Status != "cancelled", cancellationToken);
            if (exists)
            {
                continue;
            }

            _db.TelegramBotNotifications.Add(new TelegramBotNotification
            {
                TelegramUserId = telegramAccount.TelegramUserId,
                Type = "payment_failed",
                PayloadJson = payloadJson,
                Status = "pending",
                NextAttemptAt = now
            });
        }
    }

    private static string BuildPaymentFailedTelegramPayload(Order order, PaymentAttempt payment, PaymentStatus status)
    {
        var statusText = status == PaymentStatus.Cancelled ? "отменен" : "не прошел";
        var text = $"Платеж {statusText}.\nЗаказ: {order.Id}\nПровайдер: {payment.Provider}\nСумма: {payment.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {payment.Currency}\n\nВы можете выбрать другой способ оплаты или написать в поддержку.";
        return JsonSerializer.Serialize(new
        {
            text,
            replyMarkupJson = BuildPaymentFailedReplyMarkupJson(),
            orderId = order.Id,
            paymentId = payment.Id,
            provider = payment.Provider.ToString(),
            status = status.ToString()
        }, JsonOptions);
    }

    private static string BuildPaymentFailedReplyMarkupJson()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Купить VPN", callback_data = "tariffs" }, new { text = "Мои заказы", callback_data = "orders" } },
                new object[] { new { text = "Поддержка", callback_data = "support" } }
            }
        }, JsonOptions);

    private static string BuildPostPaymentReplyMarkupJson()
        => JsonSerializer.Serialize(new
        {
            inline_keyboard = new object[]
            {
                new object[] { new { text = "Мои ключи", callback_data = "keys" }, new { text = "Мои подписки", callback_data = "subscriptions" } },
                new object[] { new { text = "Продлить", callback_data = "renew" }, new { text = "Поддержка", callback_data = "support" } }
            }
        }, JsonOptions);

    private static RefundDto MapRefund(Refund refund)
        => new(refund.Id, refund.PaymentAttemptId, refund.Provider, refund.ProviderRefundId, refund.Status.ToString(), refund.Amount, refund.Currency, refund.Reason, refund.CreatedAt, refund.RefundedAt);

    private static string BuildPaymentIdempotencyKey(Guid orderId, PaymentProvider provider, Guid accountId)
        => Sha256($"payment:{orderId:N}:{provider}:{accountId:N}");

    private static string BuildRefundIdempotencyKey(Guid paymentId, decimal amount, string reason)
        => Sha256($"refund:{paymentId:N}:{amount.ToString("0.00", CultureInfo.InvariantCulture)}:{reason.Trim()}");

    private static string BuildWebhookEventId(PaymentWebhookParseResult parsed, string payloadSha256)
        => string.IsNullOrWhiteSpace(parsed.ExternalEventId)
            ? $"payload:{payloadSha256}"
            : parsed.ExternalEventId.Trim();

    private static string NormalizeCurrency(string currency)
        => currency == "643" ? "RUB" : currency.Trim().ToUpperInvariant();

    private static string Sha256(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SerializeSafeHeaders(IReadOnlyDictionary<string, string> headers)
    {
        var safe = headers
            .Where(x => !x.Key.Contains("Authorization", StringComparison.OrdinalIgnoreCase) && !x.Key.Contains("Cookie", StringComparison.OrdinalIgnoreCase) && !x.Key.Contains("Token", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => x.Value);
        return JsonSerializer.Serialize(safe);
    }
}
