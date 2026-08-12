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
public sealed record CabinetSubscriptionDto(
    Guid Id,
    Guid TariffId,
    string Status,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? TariffName,
    DateTimeOffset? GracePeriodEndAt,
    Guid? CurrentAccessId,
    string? AccessUri,
    string? NodeName,
    DateTimeOffset? SuspendedAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
public sealed record CabinetAccessCredentialDto(
    Guid Id,
    Guid SubscriptionId,
    string SubscriptionStatus,
    bool IsTerminal,
    string? ServerName,
    string AccessUri,
    string Status,
    DateTimeOffset? ExpiryDate);
public sealed record CabinetPaymentAttemptDto(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    string Provider,
    string ProviderMode,
    string ProviderPaymentId,
    string? ConfirmationUrl,
    decimal Amount,
    string Currency,
    string Status,
    bool IsActivationProcessed,
    DateTimeOffset? ActivationProcessedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? FailedAt,
    DateTimeOffset? RefundedAt,
    decimal RefundedAmount,
    string StatusMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

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
        IQueryable<Subscription> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            query = _db.Subscriptions.FromSqlInterpolated($"""
                SELECT s.*
                FROM "Subscriptions" AS s
                WHERE s."UserId" = {userId}
                ORDER BY julianday(s."CreatedAt") DESC, julianday(s."UpdatedAt") DESC
                LIMIT 100
                """);
        }
        else
        {
            query = _db.Subscriptions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.UpdatedAt)
                .Take(100);
        }

        var subscriptions = await query
            .AsNoTracking()
            .Include(x => x.Tariff)
            .Include(x => x.CurrentAccess)
            .Include(x => x.CurrentServer)
            .ToListAsync(cancellationToken);

        var items = subscriptions.Select(subscription =>
        {
            var hasUsableAccess = (subscription.Status == SubscriptionStatus.Active || subscription.Status == SubscriptionStatus.GracePeriod)
                && (subscription.GracePeriodEndAt ?? subscription.EndAt) > now
                && subscription.CurrentAccess is { Status: not AccessCredentialStatus.Revoked };

            return new CabinetSubscriptionDto(
                subscription.Id,
                subscription.TariffId,
                subscription.Status.ToString(),
                subscription.StartAt,
                subscription.EndAt,
                subscription.Tariff?.Name,
                subscription.GracePeriodEndAt,
                subscription.CurrentAccessId,
                hasUsableAccess ? subscription.CurrentAccess?.AccessUri : null,
                subscription.CurrentServer?.Name,
                subscription.SuspendedAt,
                subscription.CancelledAt,
                subscription.CreatedAt,
                subscription.UpdatedAt);
        }).ToList();

        return Ok(items);
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
        List<PaymentAttempt> payments;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            payments = await _db.Payments
                .FromSqlInterpolated($"""
                    SELECT p.*
                    FROM "Payments" AS p
                    INNER JOIN "Orders" AS o ON o."Id" = p."OrderId"
                    WHERE o."UserId" = {userId}
                    ORDER BY julianday(p."CreatedAt") DESC, julianday(p."UpdatedAt") DESC
                    LIMIT 100
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        else
        {
            payments = await _db.Payments
                .AsNoTracking()
                .Where(x => x.Order != null && x.Order.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.UpdatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        return Ok(payments.Select(payment => ToCabinetPayment(payment, userId)).ToList());
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
            : Ok(ToCabinetPayment(payment, userId));
    }

    private static CabinetPaymentAttemptDto ToCabinetPayment(PaymentAttempt payment, Guid userId)
        => new(
            payment.Id,
            payment.OrderId,
            userId,
            payment.Provider.ToString(),
            payment.ProviderMode.ToString(),
            payment.ProviderPaymentId,
            payment.ConfirmationUrl,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.IsActivationProcessed,
            payment.ActivationProcessedAt,
            payment.PaidAt,
            payment.FailedAt,
            payment.RefundedAt,
            payment.RefundedAmount,
            GetCabinetPaymentStatusMessage(payment.Status),
            payment.CreatedAt,
            payment.UpdatedAt);

    private static string GetCabinetPaymentStatusMessage(PaymentStatus status)
        => status switch
        {
            PaymentStatus.New => "Платёж создан.",
            PaymentStatus.Pending or PaymentStatus.WaitingConfirmation => "Ожидаем подтверждение платежа.",
            PaymentStatus.Succeeded => "Платёж подтверждён.",
            PaymentStatus.Failed => "Платёж не завершён. Повторите оплату или обратитесь в поддержку.",
            PaymentStatus.Cancelled => "Платёж отменён.",
            PaymentStatus.Refunded => "Средства возвращены.",
            PaymentStatus.PartiallyRefunded => "Часть суммы возвращена.",
            _ => "Статус платежа уточняется. Повторите проверку позже."
        };

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
        IQueryable<AccessCredential> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            query = _db.AccessCredentials.FromSqlInterpolated($"""
                SELECT a.*
                FROM "AccessCredentials" AS a
                INNER JOIN "Subscriptions" AS s ON s."Id" = a."SubscriptionId"
                WHERE s."UserId" = {userId}
                ORDER BY julianday(a."CreatedAt") DESC, julianday(a."UpdatedAt") DESC
                LIMIT 100
                """);
        }
        else
        {
            query = _db.AccessCredentials
                .Where(x => x.Subscription != null && x.Subscription.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.UpdatedAt)
                .Take(100);
        }

        var accessEntities = await query
            .AsNoTracking()
            .Include(x => x.Subscription)
            .Include(x => x.Server)
            .ToListAsync(cancellationToken);
        var accesses = accessEntities.Select(x =>
        {
            var subscriptionAvailable = x.Subscription is not null
                && BusinessRules.IsSubscriptionAccessAvailable(x.Subscription.Status, x.Subscription.EndAt, x.Subscription.GracePeriodEndAt, now);
            var accessAvailable = x.Status != AccessCredentialStatus.Revoked && subscriptionAvailable;
            return new CabinetAccessCredentialDto(
                x.Id,
                x.SubscriptionId,
                x.Subscription?.Status.ToString() ?? string.Empty,
                !accessAvailable,
                x.Server?.Name,
                accessAvailable ? x.AccessUri : string.Empty,
                x.Status.ToString(),
                x.Subscription is not null ? BusinessRules.GetSubscriptionAccessEnd(x.Subscription.EndAt, x.Subscription.GracePeriodEndAt) : null);
        }).ToList();

        return Ok(accesses);
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
