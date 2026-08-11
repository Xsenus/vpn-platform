using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Services;

namespace VpnPlatform.Api.Controllers.Me;

public sealed record CreateMeOrderHttpRequest(Guid TariffId, string Type, string PaymentProvider, string? PromoCode, Guid? SubscriptionId);
public sealed record InitMePaymentHttpRequest(string? ReturnUrl);
public sealed record CreateMeSupportConversationHttpRequest(string Subject, string Text, Guid? OrderId, Guid? SubscriptionId);
public sealed record MeSupportReplyHttpRequest(string Text, int? Revision = null);
public sealed record MeSupportStatusHttpRequest(string Status, int? Revision = null);

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly OrderService _orderService;
    private readonly CheckoutSessionService _checkoutSessionService;
    private readonly PaymentOrchestrator _paymentOrchestrator;
    private readonly TelegramBotService _telegramBotService;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IConfiguration _configuration;
    private readonly IClock _clock;

    public MeController(
        IApplicationDbContext db,
        OrderService orderService,
        CheckoutSessionService checkoutSessionService,
        PaymentOrchestrator paymentOrchestrator,
        TelegramBotService telegramBotService,
        IQrCodeGenerator qrCodeGenerator,
        IConfiguration configuration,
        IClock? clock = null)
    {
        _db = db;
        _orderService = orderService;
        _checkoutSessionService = checkoutSessionService;
        _paymentOrchestrator = paymentOrchestrator;
        _telegramBotService = telegramBotService;
        _qrCodeGenerator = qrCodeGenerator;
        _configuration = configuration;
        _clock = clock ?? new SystemClock();
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null
            ? NotFound()
            : Ok(new { user.Id, user.Email, user.DisplayName, user.PreferredLanguage, user.ReferralCode, user.Status });
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var now = _clock.UtcNow;
        var items = await _db.Subscriptions
            .AsNoTracking()
            .Include(x => x.Tariff)
            .Include(x => x.CurrentAccess)
            .Include(x => x.CurrentServer)
            .Where(x => x.UserId == userId)
            .Select(x => new SubscriptionDto(
                x.Id,
                x.UserId,
                x.TariffId,
                x.Status.ToString(),
                x.StartAt,
                x.EndAt,
                (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.GracePeriod) && (x.GracePeriodEndAt ?? x.EndAt) > now && x.CurrentAccess != null && x.CurrentAccess.Status != AccessCredentialStatus.Revoked ? x.CurrentAccess.AccessUri : null,
                (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.GracePeriod) && (x.GracePeriodEndAt ?? x.EndAt) > now && x.CurrentAccess != null && x.CurrentAccess.Status != AccessCredentialStatus.Revoked ? x.CurrentAccess.QrCodePath : null,
                (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.GracePeriod) && (x.GracePeriodEndAt ?? x.EndAt) > now && x.CurrentAccess != null && x.CurrentAccess.Status != AccessCredentialStatus.Revoked ? x.CurrentAccess.ConfigPath : null,
                x.CurrentServer != null ? x.CurrentServer.Name : null,
                x.Tariff != null ? x.Tariff.Name : null,
                x.GracePeriodEndAt,
                x.AutoRenewFlag,
                x.SourceChannel.ToString(),
                x.CurrentServerId,
                x.CurrentAccessId,
                x.LastPaymentId,
                x.RenewalCount,
                x.BlockReason,
                x.SuspendedAt,
                x.CancelledAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(items.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Tariff)
            .Include(x => x.PaymentAttempts)
            .Where(x => x.UserId == ResolveUserId())
            .ToListAsync(cancellationToken);

        return Ok(orders.OrderByDescending(x => x.CreatedAt).Select(x => new
        {
            x.Id,
            x.UserId,
            x.TariffId,
            TariffName = x.Tariff != null ? x.Tariff.Name : null,
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
            LinkedSubscriptionId = OrderService.GetRenewalSubscriptionId(x),
            x.CreatedAt,
            x.UpdatedAt
        }).ToList());
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateMeOrderHttpRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseDefined(request.Type, out OrderType orderType)
            || !TryParseDefined(request.PaymentProvider, out PaymentProvider paymentProvider))
        {
            return BadRequest(new { error = "Invalid order request." });
        }

        var userId = ResolveUserId();
        var tariffId = request.TariffId;
        Guid? renewalSubscriptionId = null;

        if (orderType == OrderType.Renewal)
        {
            if (!request.SubscriptionId.HasValue)
            {
                return BadRequest(new { error = "Subscription is required for renewal orders." });
            }

            var subscription = await _db.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.SubscriptionId.Value && x.UserId == userId, cancellationToken);

            if (subscription is null)
            {
                return NotFound(new { error = "Subscription not found." });
            }

            if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Blocked)
            {
                return BadRequest(new { error = "Subscription is not available for renewal." });
            }

            if (subscription.TariffId != request.TariffId)
            {
                return BadRequest(new { error = "Tariff does not match subscription." });
            }

            tariffId = subscription.TariffId;
            renewalSubscriptionId = subscription.Id;
        }

        var result = await _orderService.CreateOrderAsync(
            new CreateOrderCommand(
                userId,
                tariffId,
                orderType,
                ChannelType.Web,
                paymentProvider,
                request.PromoCode,
                false,
                RenewalSubscriptionId: renewalSubscriptionId),
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.IsRetryable
                ? Conflict(new { error = result.Error })
                : BadRequest(new { error = result.Error });
    }

    [HttpPost("checkout-sessions/{token}/claim")]
    public async Task<IActionResult> ClaimCheckoutSession([FromRoute] string token, CancellationToken cancellationToken)
    {
        var result = await _checkoutSessionService.ClaimAsync(new ClaimCheckoutSessionCommand(token, ResolveUserId()), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.IsRetryable
                ? Conflict(new { error = result.Error })
                : BadRequest(new { error = result.Error });
    }

    [HttpPost("orders/{id:guid}/payments/{provider}/init")]
    public async Task<IActionResult> InitOrderPayment([FromRoute] Guid id, [FromRoute] string provider, [FromBody] InitMePaymentHttpRequest? request, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var ownsOrder = await _db.Orders.AnyAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (!ownsOrder)
        {
            return NotFound(new { error = "Order not found." });
        }

        if (!TryParseDefined(provider, out PaymentProvider paymentProvider))
        {
            return BadRequest(new { error = "Invalid payment provider." });
        }

        var result = await _paymentOrchestrator.InitPaymentAsync(
            new PaymentInitCommand(id, paymentProvider, request?.ReturnUrl),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(x => x.Order != null && x.Order.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.OrderId,
                UserId = x.Order != null ? x.Order.UserId : (Guid?)null,
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
                WebhookEventsCount = _db.PaymentWebhookEvents.Count(evt => evt.PaymentAttemptId == x.Id),
                RefundsCount = _db.Refunds.Count(refund => refund.PaymentAttemptId == x.Id),
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        return Ok(payments.OrderByDescending(x => x.CreatedAt).Take(100).ToList());
    }

    [HttpGet("payments/{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var payment = await _db.Payments
            .AsNoTracking()
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id && x.Order != null && x.Order.UserId == userId, cancellationToken);
        return payment is null
            ? NotFound()
            : Ok(new
            {
                payment.Id,
                payment.OrderId,
                UserId = payment.Order != null ? payment.Order.UserId : (Guid?)null,
                Provider = payment.Provider.ToString(),
                payment.PaymentProviderAccountId,
                ProviderMode = payment.ProviderMode.ToString(),
                payment.ProviderPaymentId,
                payment.ExternalEventId,
                payment.IdempotencyKey,
                payment.ConfirmationUrl,
                payment.ReturnUrl,
                payment.Amount,
                payment.Currency,
                Status = payment.Status.ToString(),
                payment.SignatureValidated,
                payment.IsActivationProcessed,
                payment.ActivationProcessedAt,
                payment.PaidAt,
                payment.FailedAt,
                payment.RefundedAt,
                payment.RefundedAmount,
                payment.StatusReason,
                WebhookEventsCount = await _db.PaymentWebhookEvents.CountAsync(evt => evt.PaymentAttemptId == payment.Id, cancellationToken),
                RefundsCount = await _db.Refunds.CountAsync(refund => refund.PaymentAttemptId == payment.Id, cancellationToken),
                payment.CreatedAt,
                payment.UpdatedAt
            });
    }

    [HttpGet("support/conversations")]
    public async Task<IActionResult> GetSupportConversations(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var conversations = await _db.SupportConversations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new SupportConversationDto(x.Id, x.UserId, x.TelegramUserId, x.Channel, x.Status, x.Subject, x.AssignedToUserId, string.Empty, x.Revision, x.ClosedAt, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(conversations.OrderByDescending(x => x.UpdatedAt).Take(100).ToList());
    }

    [HttpGet("support/conversations/{id:guid}/messages")]
    public async Task<IActionResult> GetSupportMessages(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var ownsConversation = await _db.SupportConversations
            .AnyAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (!ownsConversation)
        {
            return NotFound(new { error = "Support conversation not found." });
        }

        var messages = await _db.SupportMessages
            .AsNoTracking()
            .Where(x => x.SupportConversationId == id && !x.IsInternalNote && x.Direction != "internal")
            .Select(x => new SupportMessageDto(x.Id, x.SupportConversationId, x.UserId, x.TelegramUserId, x.Direction, x.Text, x.AttachmentsJson, x.IsInternalNote, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(messages.OrderBy(x => x.CreatedAt).ToList());
    }

    [HttpPost("support/conversations")]
    public async Task<IActionResult> CreateSupportConversation([FromBody] CreateMeSupportConversationHttpRequest request, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var subject = NormalizeSupportText(request?.Subject, 160);
        var text = NormalizeSupportText(request?.Text, 4000);
        var orderId = request?.OrderId;
        var subscriptionId = request?.SubscriptionId;
        if (subject.Length < 4)
        {
            return BadRequest(new { error = "Subject must contain at least 4 characters." });
        }

        if (text.Length < 10)
        {
            return BadRequest(new { error = "Message text must contain at least 10 characters." });
        }

        if (orderId.HasValue && !await _db.Orders.AnyAsync(x => x.Id == orderId.Value && x.UserId == userId, cancellationToken))
        {
            return BadRequest(new { error = "Linked order was not found." });
        }

        if (subscriptionId.HasValue && !await _db.Subscriptions.AnyAsync(x => x.Id == subscriptionId.Value && x.UserId == userId, cancellationToken))
        {
            return BadRequest(new { error = "Linked subscription was not found." });
        }

        var contextJson = JsonSerializer.Serialize(new
        {
            source = "cabinet",
            OrderId = orderId,
            SubscriptionId = subscriptionId
        });
        var conversation = new SupportConversation
        {
            UserId = userId,
            Channel = "web",
            Status = "open",
            Subject = subject,
            InternalNote = BuildSupportContextNote(orderId, subscriptionId)
        };
        var message = new SupportMessage
        {
            SupportConversationId = conversation.Id,
            UserId = userId,
            Direction = "inbound",
            Text = text,
            RawPayload = contextJson,
            AttachmentsJson = "[]"
        };

        _db.SupportConversations.Add(conversation);
        _db.SupportMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new SupportConversationDto(conversation.Id, conversation.UserId, conversation.TelegramUserId, conversation.Channel, conversation.Status, conversation.Subject, conversation.AssignedToUserId, string.Empty, conversation.Revision, conversation.ClosedAt, conversation.CreatedAt, conversation.UpdatedAt));
    }

    [HttpPost("support/conversations/{id:guid}/reply")]
    public async Task<IActionResult> ReplySupportConversation(Guid id, [FromBody] MeSupportReplyHttpRequest request, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var text = NormalizeSupportText(request?.Text, 4000);
        if (text.Length < 2)
        {
            return BadRequest(new { error = "Message text must contain at least 2 characters." });
        }

        var conversation = await _db.SupportConversations.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
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

        var message = new SupportMessage
        {
            SupportConversationId = conversation.Id,
            UserId = userId,
            Direction = "inbound",
            Text = text,
            AttachmentsJson = "[]"
        };

        _db.SupportMessages.Add(message);
        conversation.Status = "open";
        conversation.ClosedAt = null;
        conversation.Revision = checked(conversation.Revision + 1);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Support conversation changed. Reload it and retry." });
        }

        return Ok(new SupportMessageDto(message.Id, message.SupportConversationId, message.UserId, message.TelegramUserId, message.Direction, message.Text, message.AttachmentsJson, message.IsInternalNote, message.CreatedAt));
    }

    [HttpPatch("support/conversations/{id:guid}/status")]
    public async Task<IActionResult> UpdateSupportConversationStatus(Guid id, [FromBody] MeSupportStatusHttpRequest request, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var conversation = await _db.SupportConversations.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
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

        var status = (request?.Status ?? string.Empty).Trim().ToLowerInvariant();
        if (status is not ("open" or "closed"))
        {
            return BadRequest(new { error = "Status must be open or closed." });
        }

        conversation.Status = status;
        conversation.ClosedAt = status == "closed" ? DateTimeOffset.UtcNow : null;
        conversation.Revision = checked(conversation.Revision + 1);
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Support conversation changed. Reload it and retry." });
        }

        return Ok(new { conversationId = conversation.Id, conversation.Status, conversation.Revision });
    }


    [HttpPost("telegram/link-token")]
    public async Task<IActionResult> CreateTelegramLinkToken(CancellationToken cancellationToken)
    {
        var username = await _db.SiteContentBlocks
            .AsNoTracking()
            .Where(x => x.Key == "telegram_bot.public_bot_username" && x.Group == "telegram_bot" && x.IsActive)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken)
            ?? _configuration["TelegramBot:PublicBotUsername"]
            ?? string.Empty;
        var result = await _telegramBotService.CreateLinkTokenAsync(ResolveUserId(), username, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("telegram/status")]
    public async Task<IActionResult> GetTelegramStatus(CancellationToken cancellationToken)
        => Ok(await _telegramBotService.GetStatusAsync(ResolveUserId(), cancellationToken));

    [HttpDelete("telegram/unlink")]
    public async Task<IActionResult> UnlinkTelegram(CancellationToken cancellationToken)
    {
        var result = await _telegramBotService.UnlinkAsync(ResolveUserId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("referrals")]
    public async Task<IActionResult> GetReferrals(CancellationToken cancellationToken)
    {
        var rewards = await _db.RewardLedgers.AsNoTracking()
            .Where(x => x.UserId == ResolveUserId())
            .Select(x => new
            {
                x.Id,
                x.Type,
                Status = x.Status.ToString(),
                x.Value,
                x.CurrencyOrUnit,
                x.ProcessedAt,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return Ok(rewards.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpGet("accesses")]
    public async Task<IActionResult> GetAccesses(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var now = _clock.UtcNow;
        var accessEntities = await _db.AccessCredentials
            .AsNoTracking()
            .Include(x => x.Subscription)
            .Include(x => x.Server)
            .Where(x => x.Subscription != null && x.Subscription.UserId == userId)
            .ToListAsync(cancellationToken);
        var accesses = accessEntities.Select(x =>
        {
            var subscriptionAvailable = x.Subscription is not null
                && BusinessRules.IsSubscriptionAccessAvailable(x.Subscription.Status, x.Subscription.EndAt, x.Subscription.GracePeriodEndAt, now);
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
                ServerName = x.Server != null ? x.Server.Name : null,
                AccessUri = accessAvailable ? x.AccessUri : string.Empty,
                QrCodePayload = accessAvailable ? x.QrCodePath : string.Empty,
                QrCodePath = accessAvailable ? x.QrCodePath : string.Empty,
                ConfigPath = accessAvailable ? x.ConfigPath : string.Empty,
                Status = x.Status.ToString(),
                x.IssuedAt,
                ExpiryDate = x.Subscription != null ? BusinessRules.GetSubscriptionAccessEnd(x.Subscription.EndAt, x.Subscription.GracePeriodEndAt) : (DateTimeOffset?)null,
                x.DisabledAt,
                x.LastSyncedAt,
                x.Revision,
                x.CreatedAt,
                x.UpdatedAt
            };
        }).ToList();

        return Ok(accesses.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpGet("accesses/{id:guid}/qr")]
    public async Task<IActionResult> GetAccessQr(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var subscriptionId = await _db.AccessCredentials
            .AsNoTracking()
            .Where(x => x.Id == id && x.Subscription != null && x.Subscription.UserId == userId)
            .Select(x => (Guid?)x.SubscriptionId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!subscriptionId.HasValue)
        {
            return NotFound(new { error = "VPN access not found." });
        }

        await using var gate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(subscriptionId.Value, cancellationToken);
        var access = await _db.AccessCredentials
            .AsNoTracking()
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.Id == id && x.Subscription != null && x.Subscription.UserId == userId, cancellationToken);
        if (access is null)
        {
            return NotFound(new { error = "VPN access not found." });
        }

        if (access.Status == AccessCredentialStatus.Revoked)
        {
            return BadRequest(new { error = "Revoked VPN access QR code is not available." });
        }

        if (access.Subscription?.Status == SubscriptionStatus.Cancelled)
        {
            return BadRequest(new { error = "Cancelled subscription VPN access QR code is not available." });
        }

        if (access.Subscription is null
            || !BusinessRules.IsSubscriptionAccessAvailable(access.Subscription.Status, access.Subscription.EndAt, access.Subscription.GracePeriodEndAt, _clock.UtcNow))
        {
            return BadRequest(new { error = "Expired or inactive subscription VPN access QR code is not available." });
        }

        if (string.IsNullOrWhiteSpace(access.AccessUri))
        {
            return BadRequest(new { error = "VPN access URI is not available yet." });
        }

        var qr = _qrCodeGenerator.GenerateSvg(access.AccessUri, $"cabinet-access-{id:N}");
        return Content(qr.Content, qr.MediaType);
    }

    [HttpPost("subscriptions/{id:guid}/renew")]
    public IActionResult Renew([FromRoute] Guid id) => StatusCode(
        StatusCodes.Status410Gone,
        new { subscriptionId = id, error = "This endpoint is no longer supported. Use POST /api/me/orders with type Renewal." });

    private static bool TryParseDefined<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
        => Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

    private Guid ResolveUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var value) ? value : Guid.Empty;
    }

    private static string NormalizeSupportText(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string BuildSupportContextNote(Guid? orderId, Guid? subscriptionId)
    {
        var parts = new List<string>();
        if (orderId.HasValue)
        {
            parts.Add($"заказ {orderId.Value}");
        }

        if (subscriptionId.HasValue)
        {
            parts.Add($"подписка {subscriptionId.Value}");
        }

        return parts.Count == 0 ? string.Empty : $"Связано: {string.Join(", ", parts)}.";
    }
}
