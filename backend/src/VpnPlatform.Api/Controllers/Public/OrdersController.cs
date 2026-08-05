using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VpnPlatform.Api.Security;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Public;

public sealed record CreateCheckoutSessionHttpRequest(Guid TariffId, string Type, string PaymentProvider, string? PromoCode, string? EmailHint, string? ReturnUrl);

[ApiController]
[Route("api/public")]
public class OrdersController : ControllerBase
{
    private readonly CheckoutSessionService _checkoutSessionService;

    public OrdersController(CheckoutSessionService checkoutSessionService)
    {
        _checkoutSessionService = checkoutSessionService;
    }

    [HttpPost("checkout-sessions")]
    [EnableRateLimiting(ApiRateLimitPolicies.PublicCheckout)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionHttpRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseDefined(request.Type, out OrderType orderType))
        {
            return BadRequest(new { error = "Invalid order type." });
        }

        if (orderType != OrderType.NewSubscription)
        {
            return BadRequest(new { error = "Public checkout supports only new subscriptions." });
        }

        if (!TryParseDefined(request.PaymentProvider, out PaymentProvider paymentProvider))
        {
            return BadRequest(new { error = "Invalid payment provider." });
        }

        var result = await _checkoutSessionService.CreateAsync(
            new CreateCheckoutSessionCommand(
                request.TariffId,
                orderType,
                ChannelType.Web,
                paymentProvider,
                request.PromoCode,
                false,
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
    public IActionResult GetStatus([FromRoute] Guid id)
        => StatusCode(StatusCodes.Status410Gone, new
        {
            id,
            error = "anonymous_order_status_disabled",
            message = "Authenticate and use GET /api/me/orders. Public order identifiers are not status credentials."
        });

    [HttpPost("orders")]
    [EnableRateLimiting(ApiRateLimitPolicies.PublicCheckout)]
    public IActionResult CreateAnonymousOrder()
        => StatusCode(StatusCodes.Status410Gone, new
        {
            error = "anonymous_public_orders_disabled",
            message = "Use POST /api/public/checkout-sessions, authenticate the user, then claim the session with POST /api/me/checkout-sessions/{token}/claim."
        });

    private static bool TryParseDefined<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
        => Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);
}
