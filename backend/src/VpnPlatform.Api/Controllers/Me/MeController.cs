using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Me;

public sealed record CreateMeOrderHttpRequest(Guid TariffId, string Type, string Channel, string PaymentProvider, string? PromoCode, bool IsFirstPurchase);
public sealed record InitMePaymentHttpRequest(string? ReturnUrl);

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

    public MeController(
        IApplicationDbContext db,
        OrderService orderService,
        CheckoutSessionService checkoutSessionService,
        PaymentOrchestrator paymentOrchestrator,
        TelegramBotService telegramBotService,
        IQrCodeGenerator qrCodeGenerator,
        IConfiguration configuration)
    {
        _db = db;
        _orderService = orderService;
        _checkoutSessionService = checkoutSessionService;
        _paymentOrchestrator = paymentOrchestrator;
        _telegramBotService = telegramBotService;
        _qrCodeGenerator = qrCodeGenerator;
        _configuration = configuration;
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
        var items = await _db.Subscriptions
            .AsNoTracking()
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
                x.CurrentAccess != null ? x.CurrentAccess.AccessUri : null,
                x.CurrentAccess != null ? x.CurrentAccess.QrCodePath : null,
                x.CurrentAccess != null ? x.CurrentAccess.ConfigPath : null,
                x.CurrentServer != null ? x.CurrentServer.Name : null))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var orders = await _db.Orders.AsNoTracking().Where(x => x.UserId == ResolveUserId()).ToListAsync(cancellationToken);
        return Ok(orders.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateMeOrderHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateOrderAsync(
            new CreateOrderCommand(
                ResolveUserId(),
                request.TariffId,
                Enum.Parse<OrderType>(request.Type, true),
                Enum.Parse<ChannelType>(request.Channel, true),
                Enum.Parse<PaymentProvider>(request.PaymentProvider, true),
                request.PromoCode,
                request.IsFirstPurchase),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("checkout-sessions/{token}/claim")]
    public async Task<IActionResult> ClaimCheckoutSession([FromRoute] string token, CancellationToken cancellationToken)
    {
        var result = await _checkoutSessionService.ClaimAsync(new ClaimCheckoutSessionCommand(token, ResolveUserId()), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
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

        var result = await _paymentOrchestrator.InitPaymentAsync(
            new PaymentInitCommand(id, Enum.Parse<PaymentProvider>(provider, true), request?.ReturnUrl),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var orderIds = await _db.Orders.Where(x => x.UserId == userId).Select(x => x.Id).ToListAsync(cancellationToken);
        var payments = await _db.Payments.AsNoTracking().Where(x => orderIds.Contains(x.OrderId)).ToListAsync(cancellationToken);
        return Ok(payments.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpGet("payments/{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var payment = await _db.Payments
            .AsNoTracking()
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id && x.Order != null && x.Order.UserId == userId, cancellationToken);
        return payment is null ? NotFound() : Ok(payment);
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
        var rewards = await _db.RewardLedgers.AsNoTracking().Where(x => x.UserId == ResolveUserId()).ToListAsync(cancellationToken);
        return Ok(rewards.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpGet("accesses")]
    public async Task<IActionResult> GetAccesses(CancellationToken cancellationToken)
    {
        var subscriptionIds = await _db.Subscriptions.Where(x => x.UserId == ResolveUserId()).Select(x => x.Id).ToListAsync(cancellationToken);
        var accesses = await _db.AccessCredentials.AsNoTracking().Where(x => subscriptionIds.Contains(x.SubscriptionId)).ToListAsync(cancellationToken);
        return Ok(accesses.OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpGet("accesses/{id:guid}/qr")]
    public async Task<IActionResult> GetAccessQr(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var access = await _db.AccessCredentials
            .AsNoTracking()
            .Include(x => x.Subscription)
            .FirstOrDefaultAsync(x => x.Id == id && x.Subscription != null && x.Subscription.UserId == userId, cancellationToken);
        if (access is null)
        {
            return NotFound(new { error = "VPN access not found." });
        }

        if (string.IsNullOrWhiteSpace(access.AccessUri))
        {
            return BadRequest(new { error = "VPN access URI is not available yet." });
        }

        var qr = _qrCodeGenerator.GenerateSvg(access.AccessUri, $"cabinet-access-{id:N}");
        return Content(qr.Content, qr.MediaType);
    }

    [HttpPost("subscriptions/{id:guid}/renew")]
    public IActionResult Renew([FromRoute] Guid id) => Ok(new { subscriptionId = id, message = "Use POST /api/me/orders + payment init to renew this subscription." });

    private Guid ResolveUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var value) ? value : Guid.Empty;
    }
}
