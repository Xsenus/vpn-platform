using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Public;

public sealed record PublicPaymentProviderDto(
    string Provider,
    string PublicName,
    string Mode,
    string HealthStatus);

[ApiController]
[Route("api/public/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public PaymentsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("providers")]
    public async Task<IActionResult> GetAvailableProviders(CancellationToken cancellationToken)
    {
        var enabledAccounts = await _db.PaymentProviderAccounts
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.Mode != PaymentProviderMode.Disabled)
            .OrderBy(x => x.Provider)
            .ThenByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var providers = enabledAccounts
            .Where(PaymentProviderConfigurationRules.IsWebCheckoutConfigured)
            .GroupBy(x => x.Provider)
            .Select(group => MapProvider(group.First()))
            .OrderBy(x => x.Provider, StringComparer.Ordinal)
            .ToList();

        return Ok(providers);
    }

    [HttpPost("{provider}/init")]
    public IActionResult InitAnonymousPayment([FromRoute] string provider)
        => StatusCode(StatusCodes.Status410Gone, new
        {
            provider,
            error = "anonymous_payment_init_disabled",
            message = "Create a checkout session, authenticate the user, claim the session, then initialize payment through /api/me/orders/{id}/payments/{provider}/init."
        });

    private static PublicPaymentProviderDto MapProvider(PaymentProviderAccount account)
        => new(
            account.Provider.ToString(),
            string.IsNullOrWhiteSpace(account.PublicName) ? account.Provider.ToString() : account.PublicName,
            account.Mode.ToString(),
            account.HealthStatus.ToString());
}
