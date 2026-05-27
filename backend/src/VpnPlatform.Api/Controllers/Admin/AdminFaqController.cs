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
[Route("api/admin/faq")]
public sealed class AdminFaqController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public AdminFaqController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var entries = await _db.FaqEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Ok(entries
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.Question)
            .Select(MapFaq)
            .ToList());
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Create([FromBody] FaqEntryUpsertRequest request, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var entry = new FaqEntry();
        Apply(entry, request);
        _db.FaqEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(MapFaq(entry));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] FaqEntryUpsertRequest request, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var entry = await _db.FaqEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        Apply(entry, request);
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(MapFaq(entry));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _db.FaqEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        _db.FaqEntries.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { id, deleted = true });
    }

    private static void Apply(FaqEntry entry, FaqEntryUpsertRequest request)
    {
        entry.Question = request.Question.Trim();
        entry.Answer = request.Answer.Trim();
        entry.Category = string.IsNullOrWhiteSpace(request.Category) ? "Общее" : request.Category.Trim();
        entry.IsActive = request.IsActive;
        entry.ShowOnHome = request.ShowOnHome;
        entry.ShowOnFaqPage = request.ShowOnFaqPage;
        entry.SortOrder = request.SortOrder;
    }

    private static string? Validate(FaqEntryUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question)) return "Question is required.";
        if (request.Question.Trim().Length > 300) return "Question must be 300 characters or less.";
        if (string.IsNullOrWhiteSpace(request.Answer)) return "Answer is required.";
        if (request.Category?.Length > 120) return "Category must be 120 characters or less.";
        return null;
    }

    private static FaqEntryDto MapFaq(FaqEntry entry)
        => new(entry.Id, entry.Question, entry.Answer, entry.Category, entry.IsActive, entry.ShowOnHome, entry.ShowOnFaqPage, entry.SortOrder, entry.CreatedAt, entry.UpdatedAt);
}
