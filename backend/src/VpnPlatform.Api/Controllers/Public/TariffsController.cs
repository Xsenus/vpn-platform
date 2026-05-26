using Microsoft.AspNetCore.Mvc;
using VpnPlatform.Application.Services;

namespace VpnPlatform.Api.Controllers.Public;

[ApiController]
[Route("api/public/tariffs")]
public class TariffsController : ControllerBase
{
    private readonly CatalogService _catalogService;

    public TariffsController(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _catalogService.GetPublicTariffsAsync(cancellationToken));
}
