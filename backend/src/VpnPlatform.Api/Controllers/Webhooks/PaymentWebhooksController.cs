using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VpnPlatform.Api.Security;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Webhooks;

[ApiController]
[Route("api/webhooks/payments")]
[EnableRateLimiting(ApiRateLimitPolicies.Webhook)]
public class PaymentWebhooksController : ControllerBase
{
    private readonly PaymentOrchestrator _paymentOrchestrator;

    public PaymentWebhooksController(PaymentOrchestrator paymentOrchestrator)
    {
        _paymentOrchestrator = paymentOrchestrator;
    }

    [HttpPost("yoomoney")]
    public Task<IActionResult> YooMoney(CancellationToken cancellationToken) => Handle(PaymentProvider.YooMoney, cancellationToken);

    [HttpPost("yookassa")]
    public Task<IActionResult> YooKassa(CancellationToken cancellationToken) => Handle(PaymentProvider.YooKassa, cancellationToken);

    [HttpPost("robokassa")]
    public Task<IActionResult> RoboKassa(CancellationToken cancellationToken) => Handle(PaymentProvider.RoboKassa, cancellationToken);

    [HttpPost("cloudpayments")]
    [HttpPost("cloudpayments/{eventType}")]
    public Task<IActionResult> CloudPayments(string? eventType, CancellationToken cancellationToken) => Handle(PaymentProvider.CloudPayments, cancellationToken, eventType);

    [HttpPost("tbank")]
    [HttpPost("tbank-acquiring")]
    public Task<IActionResult> TBankAcquiring(CancellationToken cancellationToken) => Handle(PaymentProvider.TBankAcquiring, cancellationToken);

    [HttpPost("prodamus")]
    public Task<IActionResult> Prodamus(CancellationToken cancellationToken) => Handle(PaymentProvider.Prodamus, cancellationToken);

    [HttpPost("stripe")]
    public Task<IActionResult> Stripe(CancellationToken cancellationToken) => Handle(PaymentProvider.Stripe, cancellationToken);

    [HttpPost("paypal")]
    public Task<IActionResult> PayPal(CancellationToken cancellationToken) => Handle(PaymentProvider.PayPal, cancellationToken);

    private async Task<IActionResult> Handle(PaymentProvider providerType, CancellationToken cancellationToken, string? eventType = null)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var headers = Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        headers["X-Source-IP"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            headers["X-CloudPayments-Event"] = eventType;
        }
        var result = await _paymentOrchestrator.HandleWebhookAsync(providerType, rawBody, headers, cancellationToken);
        if (providerType == PaymentProvider.RoboKassa && result.IsSuccess)
        {
            return Content($"OK{ReadFormValue(rawBody, "InvId")}", "text/plain", Encoding.UTF8);
        }

        return result.IsSuccess ? Ok(new { status = result.Value }) : BadRequest(new { error = result.Error });
    }

    private static string ReadFormValue(string rawBody, string key)
    {
        foreach (var pair in rawBody.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            var name = idx >= 0 ? pair[..idx] : pair;
            var value = idx >= 0 ? pair[(idx + 1)..] : string.Empty;
            if (string.Equals(WebUtility.UrlDecode(name), key, StringComparison.OrdinalIgnoreCase))
            {
                return WebUtility.UrlDecode(value);
            }
        }

        return string.Empty;
    }
}
