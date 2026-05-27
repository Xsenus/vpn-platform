using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Api.Controllers.Public;

[ApiController]
[Route("api/public/content")]
public class ContentController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public ContentController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("faq")]
    public async Task<IActionResult> GetFaq([FromQuery] bool home = false, CancellationToken cancellationToken = default)
    {
        var items = await _db.FaqEntries
            .AsNoTracking()
            .Where(x => x.IsActive && x.ShowOnFaqPage && (!home || x.ShowOnHome))
            .ToListAsync(cancellationToken);

        return Ok(items
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.Question)
            .Select(MapFaq)
            .ToList());
    }

    [HttpGet("home")]
    public async Task<IActionResult> GetHomeContent(CancellationToken cancellationToken = default)
    {
        var blocks = await _db.SiteContentBlocks
            .AsNoTracking()
            .Where(x => x.IsActive && x.Group == "home")
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Key)
            .ToListAsync(cancellationToken);

        return Ok(blocks.Select(MapSiteContent).ToList());
    }

    private static FaqEntryDto MapFaq(FaqEntry entry)
        => new(entry.Id, entry.Question, entry.Answer, entry.Category, entry.IsActive, entry.ShowOnHome, entry.ShowOnFaqPage, entry.SortOrder, entry.CreatedAt, entry.UpdatedAt);

    private static SiteContentBlockDto MapSiteContent(SiteContentBlock block)
        => new(block.Id, block.Key, block.Value, block.Group, block.Label, block.Description, block.InputType, block.IsActive, block.SortOrder, block.CreatedAt, block.UpdatedAt);
}
