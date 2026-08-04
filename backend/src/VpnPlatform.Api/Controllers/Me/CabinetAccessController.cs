using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Me;

[ApiController]
[Authorize]
[Route("api/cabinet/access")]
public class CabinetAccessController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public CabinetAccessController(IApplicationDbContext db, IQrCodeGenerator qrCodeGenerator)
    {
        _db = db;
        _qrCodeGenerator = qrCodeGenerator;
    }

    [HttpGet("{id:guid}/qr")]
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

        if (access.Status == AccessCredentialStatus.Revoked)
        {
            return BadRequest(new { error = "Revoked VPN access QR code is not available." });
        }

        if (string.IsNullOrWhiteSpace(access.AccessUri))
        {
            return BadRequest(new { error = "VPN access URI is not available yet." });
        }

        var qr = _qrCodeGenerator.GenerateSvg(access.AccessUri, $"cabinet-access-{id:N}");
        return Content(qr.Content, qr.MediaType);
    }

    private Guid ResolveUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var value) ? value : Guid.Empty;
    }
}
