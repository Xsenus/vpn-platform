using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin/site-content")]
public class AdminSiteContentController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public AdminSiteContentController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? group = null, CancellationToken cancellationToken = default)
    {
        var query = _db.SiteContentBlocks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(group))
        {
            var normalizedGroup = group.Trim();
            query = query.Where(x => x.Group == normalizedGroup);
        }

        var blocks = await query
            .OrderBy(x => x.Group)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Key)
            .ToListAsync(cancellationToken);

        return Ok(blocks.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Create([FromBody] SiteContentBlockUpsertRequest request, CancellationToken cancellationToken)
    {
        var block = new SiteContentBlock();
        var error = Apply(block, request);
        if (error is not null) return BadRequest(new { error });

        _db.SiteContentBlocks.Add(block);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Map(block));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SiteContentBlockUpsertRequest request, CancellationToken cancellationToken)
    {
        var block = await _db.SiteContentBlocks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (block is null) return NotFound();

        var error = Apply(block, request);
        if (error is not null) return BadRequest(new { error });

        block.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Map(block));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var block = await _db.SiteContentBlocks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (block is null) return NotFound();

        _db.SiteContentBlocks.Remove(block);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { id, deleted = true });
    }

    private static string? Apply(SiteContentBlock block, SiteContentBlockUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key)) return "Content key is required.";
        if (string.IsNullOrWhiteSpace(request.Label)) return "Content label is required.";

        block.Key = request.Key.Trim();
        block.Value = request.Value ?? string.Empty;
        block.Group = string.IsNullOrWhiteSpace(request.Group) ? "home" : request.Group.Trim();
        block.Label = request.Label.Trim();
        block.Description = request.Description?.Trim() ?? string.Empty;
        block.InputType = string.IsNullOrWhiteSpace(request.InputType) ? "text" : request.InputType.Trim();
        block.IsActive = request.IsActive;
        block.SortOrder = request.SortOrder;
        return null;
    }

    private static SiteContentBlockDto Map(SiteContentBlock block)
        => new(block.Id, block.Key, block.Value, block.Group, block.Label, block.Description, block.InputType, block.IsActive, block.SortOrder, block.CreatedAt, block.UpdatedAt);
}
