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
    public async Task<IActionResult> Get(
        [FromQuery] string? category = null,
        [FromQuery] string? visibility = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await _db.FaqEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var filtered = ApplyFilters(entries, category, visibility, search);

        return Ok(filtered
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.Question)
            .Select(MapFaq)
            .ToList());
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var entries = await _db.FaqEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var categories = entries
            .Select(x => NormalizeCategory(x.Category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
        var duplicateQuestions = entries
            .GroupBy(x => $"{NormalizeCategory(x.Category).ToLowerInvariant()}::{x.Question.Trim().ToLowerInvariant()}")
            .Where(x => x.Count() > 1)
            .Select(x =>
            {
                var first = x.First();
                return $"{NormalizeCategory(first.Category)}: {first.Question.Trim()}";
            })
            .OrderBy(x => x)
            .ToArray();

        return Ok(new
        {
            TotalCount = entries.Count,
            ActiveCount = entries.Count(x => x.IsActive),
            HiddenCount = entries.Count(x => !x.IsActive),
            HomeCount = entries.Count(x => x.IsActive && x.ShowOnHome),
            FaqPageCount = entries.Count(x => x.IsActive && x.ShowOnFaqPage),
            PublicCount = entries.Count(x => x.IsActive && x.ShowOnFaqPage),
            CategoryCount = categories.Length,
            Categories = categories,
            DuplicateQuestions = duplicateQuestions,
            HasPublicFaq = entries.Any(x => x.IsActive && x.ShowOnFaqPage),
            HasHomeFaq = entries.Any(x => x.IsActive && x.ShowOnHome)
        });
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
        if (await HasDuplicateQuestionAsync(entry.Question, entry.Category, null, cancellationToken))
        {
            return BadRequest(new { error = "FAQ question already exists in this category." });
        }

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
        if (await HasDuplicateQuestionAsync(entry.Question, entry.Category, id, cancellationToken))
        {
            return BadRequest(new { error = "FAQ question already exists in this category." });
        }

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
        entry.Category = NormalizeCategory(request.Category);
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

    private static IReadOnlyList<FaqEntry> ApplyFilters(IReadOnlyList<FaqEntry> entries, string? category, string? visibility, string? search)
    {
        var filtered = entries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedCategory = NormalizeCategory(category);
            filtered = filtered.Where(x => string.Equals(NormalizeCategory(x.Category), normalizedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(visibility) && !string.Equals(visibility, "all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = visibility.Trim().ToLowerInvariant() switch
            {
                "active" => filtered.Where(x => x.IsActive),
                "hidden" => filtered.Where(x => !x.IsActive),
                "home" => filtered.Where(x => x.IsActive && x.ShowOnHome),
                "faq" => filtered.Where(x => x.IsActive && x.ShowOnFaqPage),
                _ => filtered
            };
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            filtered = filtered.Where(x =>
                x.Question.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                x.Answer.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                NormalizeCategory(x.Category).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    private async Task<bool> HasDuplicateQuestionAsync(string question, string category, Guid? exceptId, CancellationToken cancellationToken)
    {
        var entries = await _db.FaqEntries
            .AsNoTracking()
            .Where(x => !exceptId.HasValue || x.Id != exceptId.Value)
            .ToListAsync(cancellationToken);

        return entries.Any(x =>
            string.Equals(x.Question.Trim(), question.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(NormalizeCategory(x.Category), NormalizeCategory(category), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeCategory(string? category)
        => string.IsNullOrWhiteSpace(category) ? "Общее" : category.Trim();

    private static FaqEntryDto MapFaq(FaqEntry entry)
        => new(entry.Id, entry.Question, entry.Answer, entry.Category, entry.IsActive, entry.ShowOnHome, entry.ShowOnFaqPage, entry.SortOrder, entry.CreatedAt, entry.UpdatedAt);
}
