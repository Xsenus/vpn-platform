using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VpnPlatform.Api.Security;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Public;

public sealed record CreateCheckoutSessionHttpRequest(Guid TariffId, string Type, string Channel, string PaymentProvider, string? PromoCode, bool IsFirstPurchase, string? EmailHint, string? ReturnUrl);

[ApiController]
[Route("api/public")]
public class OrdersController : ControllerBase
{
    private readonly CheckoutSessionService _checkoutSessionService;
    private readonly OrderService _orderService;

    public OrdersController(CheckoutSessionService checkoutSessionService, OrderService orderService)
    {
        _checkoutSessionService = checkoutSessionService;
        _orderService = orderService;
    }

    [HttpPost("checkout-sessions")]
    [EnableRateLimiting(ApiRateLimitPolicies.PublicCheckout)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await _checkoutSessionService.CreateAsync(
            new CreateCheckoutSessionCommand(
                request.TariffId,
                Enum.Parse<OrderType>(request.Type, true),
                Enum.Parse<ChannelType>(request.Channel, true),
                Enum.Parse<PaymentProvider>(request.PaymentProvider, true),
                request.PromoCode,
                request.IsFirstPurchase,
                request.EmailHint,
                request.ReturnUrl),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("checkout-sessions/{token}")]
    public async Task<IActionResult> GetCheckoutSession([FromRoute] string token, CancellationToken cancellationToken)
    {
        var result = await _checkoutSessionService.GetAsync(token, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpGet("orders/{id:guid}/status")]
    public async Task<IActionResult> GetStatus([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetOrderStatusAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("orders")]
    [EnableRateLimiting(ApiRateLimitPolicies.PublicCheckout)]
    public IActionResult CreateAnonymousOrder()
        => StatusCode(StatusCodes.Status410Gone, new
        {
            error = "anonymous_public_orders_disabled",
            message = "Use POST /api/public/checkout-sessions, authenticate the user, then claim the session with POST /api/me/checkout-sessions/{token}/claim."
        });
}
