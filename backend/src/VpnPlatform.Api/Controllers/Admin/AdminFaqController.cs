using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
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
        IQueryable<FaqEntry> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            RegisterSqliteUnicodeLower(dbContext);
            var normalizedCategory = NormalizeOptionalCategory(category);
            var normalizedVisibility = NormalizeVisibility(visibility);
            var normalizedSearch = search?.Trim().ToLowerInvariant() ?? string.Empty;
            query = _db.FaqEntries.FromSqlInterpolated($"""
                SELECT f.*
                FROM "FaqEntries" AS f
                WHERE ({normalizedCategory} = '' OR unicode_lower(trim(f."Category")) = {normalizedCategory})
                  AND ({normalizedVisibility} = 'all'
                    OR ({normalizedVisibility} = 'active' AND f."IsActive" = 1)
                    OR ({normalizedVisibility} = 'hidden' AND f."IsActive" = 0)
                    OR ({normalizedVisibility} = 'home' AND f."IsActive" = 1 AND f."ShowOnHome" = 1)
                    OR ({normalizedVisibility} = 'faq' AND f."IsActive" = 1 AND f."ShowOnFaqPage" = 1))
                  AND ({normalizedSearch} = ''
                    OR instr(unicode_lower(f."Question"), {normalizedSearch}) > 0
                    OR instr(unicode_lower(f."Answer"), {normalizedSearch}) > 0
                    OR instr(unicode_lower(f."Category"), {normalizedSearch}) > 0)
                ORDER BY f."SortOrder", f."Category", f."Question", f."Id"
                LIMIT 200
                """);
        }
        else
        {
            query = ApplyFilters(_db.FaqEntries.AsNoTracking(), category, visibility, search)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Category)
                .ThenBy(x => x.Question)
                .ThenBy(x => x.Id)
                .Take(200);
        }

        var entries = await query.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(entries
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.Question)
            .Select(MapFaq)
            .ToList());
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var totalCount = await _db.FaqEntries.AsNoTracking().CountAsync(cancellationToken);
        var activeCount = await _db.FaqEntries.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
        var homeCount = await _db.FaqEntries.AsNoTracking().CountAsync(x => x.IsActive && x.ShowOnHome, cancellationToken);
        var faqPageCount = await _db.FaqEntries.AsNoTracking().CountAsync(x => x.IsActive && x.ShowOnFaqPage, cancellationToken);
        var categories = (await GetCategoryRepresentativesAsync(cancellationToken))
            .Select(x => NormalizeCategory(x.Category))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var duplicateQuestions = (await GetDuplicateRepresentativesAsync(cancellationToken))
            .Select(x => $"{NormalizeCategory(x.Category)}: {x.Question.Trim()}")
            .OrderBy(x => x)
            .ToArray();

        return Ok(new
        {
            TotalCount = totalCount,
            ActiveCount = activeCount,
            HiddenCount = totalCount - activeCount,
            HomeCount = homeCount,
            FaqPageCount = faqPageCount,
            PublicCount = faqPageCount,
            CategoryCount = categories.Length,
            Categories = categories,
            DuplicateQuestions = duplicateQuestions,
            HasPublicFaq = faqPageCount > 0,
            HasHomeFaq = homeCount > 0
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
        AdminAuditLogWriter.Add(_db, this, "faq.create", "FaqEntry", entry.Id, null, MapFaq(entry));
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
        if (!request.Revision.HasValue || request.Revision.Value < 0)
        {
            return BadRequest(new { error = "FAQ revision is required and must be a non-negative integer." });
        }
        if (request.Revision.Value != entry.Revision)
        {
            return Conflict(new { error = "FAQ entry changed. Reload it and retry.", revision = entry.Revision });
        }

        var candidate = new FaqEntry();
        Apply(candidate, request);
        if (await HasDuplicateQuestionAsync(candidate.Question, candidate.Category, id, cancellationToken))
        {
            return BadRequest(new { error = "FAQ question already exists in this category." });
        }

        var before = MapFaq(entry);
        Copy(candidate, entry);
        entry.Revision = checked(entry.Revision + 1);
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        AdminAuditLogWriter.Add(_db, this, "faq.update", "FaqEntry", entry.Id, before, MapFaq(entry));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "FAQ entry changed. Reload it and retry." });
        }

        return Ok(MapFaq(entry));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] int? revision, CancellationToken cancellationToken)
    {
        var entry = await _db.FaqEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }
        if (!revision.HasValue || revision.Value < 0)
        {
            return BadRequest(new { error = "FAQ revision is required and must be a non-negative integer." });
        }
        if (revision.Value != entry.Revision)
        {
            return Conflict(new { error = "FAQ entry changed. Reload it and retry.", revision = entry.Revision });
        }

        var before = MapFaq(entry);
        _db.FaqEntries.Remove(entry);
        AdminAuditLogWriter.Add(_db, this, "faq.delete", "FaqEntry", entry.Id, before, null);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "FAQ entry changed. Reload it and retry." });
        }
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

    private static void Copy(FaqEntry source, FaqEntry target)
    {
        target.Question = source.Question;
        target.Answer = source.Answer;
        target.Category = source.Category;
        target.IsActive = source.IsActive;
        target.ShowOnHome = source.ShowOnHome;
        target.ShowOnFaqPage = source.ShowOnFaqPage;
        target.SortOrder = source.SortOrder;
    }

    private static string? Validate(FaqEntryUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question)) return "Question is required.";
        if (request.Question.Trim().Length > 300) return "Question must be 300 characters or less.";
        if (string.IsNullOrWhiteSpace(request.Answer)) return "Answer is required.";
        if (request.Category?.Length > 120) return "Category must be 120 characters or less.";
        return null;
    }

    private static IQueryable<FaqEntry> ApplyFilters(IQueryable<FaqEntry> entries, string? category, string? visibility, string? search)
    {
        var filtered = entries;
        if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedCategory = NormalizeCategory(category).ToLower();
            filtered = filtered.Where(x => x.Category.Trim().ToLower() == normalizedCategory);
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
            var normalizedSearch = search.Trim().ToLower();
            filtered = filtered.Where(x =>
                x.Question.ToLower().Contains(normalizedSearch) ||
                x.Answer.ToLower().Contains(normalizedSearch) ||
                x.Category.ToLower().Contains(normalizedSearch));
        }

        return filtered;
    }

    private async Task<List<FaqEntry>> GetDuplicateRepresentativesAsync(CancellationToken cancellationToken)
    {
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            RegisterSqliteUnicodeLower(dbContext);
            return await _db.FaqEntries.FromSqlRaw("""
                    SELECT f.* FROM "FaqEntries" AS f
                    WHERE f."Id" IN (
                        SELECT min(candidate."Id")
                        FROM "FaqEntries" AS candidate
                        GROUP BY unicode_lower(trim(candidate."Category")), unicode_lower(trim(candidate."Question"))
                        HAVING count(*) > 1
                        ORDER BY unicode_lower(trim(candidate."Category")), unicode_lower(trim(candidate."Question"))
                        LIMIT 200)
                    ORDER BY f."Category", f."Question", f."Id"
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        if (_db is DbContext postgresContext && postgresContext.Database.IsNpgsql())
        {
            return await _db.FaqEntries.FromSqlRaw("""
                    SELECT DISTINCT ON (lower(trim(f."Category")), lower(trim(f."Question"))) f.*
                    FROM "FaqEntries" AS f
                    WHERE EXISTS (
                        SELECT 1 FROM "FaqEntries" AS other
                        WHERE other."Id" <> f."Id"
                          AND lower(trim(other."Category")) = lower(trim(f."Category"))
                          AND lower(trim(other."Question")) = lower(trim(f."Question")))
                    ORDER BY lower(trim(f."Category")), lower(trim(f."Question")), f."Id"
                    LIMIT 200
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        var entries = await _db.FaqEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return entries
            .GroupBy(x => $"{NormalizeCategory(x.Category).ToLowerInvariant()}::{x.Question.Trim().ToLowerInvariant()}")
            .Where(x => x.Count() > 1)
            .Select(x => x.OrderBy(item => item.Id).First())
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Question)
            .Take(200)
            .ToList();
    }

    private async Task<List<FaqEntry>> GetCategoryRepresentativesAsync(CancellationToken cancellationToken)
    {
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            RegisterSqliteUnicodeLower(dbContext);
            return await _db.FaqEntries.FromSqlRaw("""
                    SELECT f.* FROM "FaqEntries" AS f
                    WHERE f."Id" IN (
                        SELECT min(candidate."Id")
                        FROM "FaqEntries" AS candidate
                        GROUP BY unicode_lower(trim(candidate."Category"))
                        ORDER BY unicode_lower(trim(candidate."Category"))
                        LIMIT 200)
                    ORDER BY unicode_lower(trim(f."Category")), f."Id"
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        if (_db is DbContext postgresContext && postgresContext.Database.IsNpgsql())
        {
            return await _db.FaqEntries.FromSqlRaw("""
                    SELECT DISTINCT ON (lower(trim(f."Category"))) f.*
                    FROM "FaqEntries" AS f
                    ORDER BY lower(trim(f."Category")), f."Id"
                    LIMIT 200
                    """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        var entries = await _db.FaqEntries.AsNoTracking().ToListAsync(cancellationToken);
        return entries
            .GroupBy(x => NormalizeCategory(x.Category), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.OrderBy(item => item.Id).First())
            .OrderBy(x => x.Category)
            .Take(200)
            .ToList();
    }

    private static string NormalizeOptionalCategory(string? category)
        => string.IsNullOrWhiteSpace(category) || string.Equals(category.Trim(), "all", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : NormalizeCategory(category).ToLowerInvariant();

    private static string NormalizeVisibility(string? visibility)
        => visibility?.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "hidden" => "hidden",
            "home" => "home",
            "faq" => "faq",
            _ => "all"
        };

    private static void RegisterSqliteUnicodeLower(DbContext dbContext)
    {
        if (dbContext.Database.GetDbConnection() is SqliteConnection connection)
        {
            connection.CreateFunction<string?, string?>("unicode_lower", value => value?.ToLowerInvariant(), isDeterministic: true);
        }
    }

    private async Task<bool> HasDuplicateQuestionAsync(string question, string category, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalizedQuestion = question.Trim().ToLowerInvariant();
        var normalizedCategory = NormalizeCategory(category).ToLowerInvariant();
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            RegisterSqliteUnicodeLower(dbContext);
            var excludedId = exceptId ?? Guid.Empty;
            return await _db.FaqEntries.FromSqlInterpolated($"""
                    SELECT f.* FROM "FaqEntries" AS f
                    WHERE f."Id" <> {excludedId}
                      AND unicode_lower(trim(f."Question")) = {normalizedQuestion}
                      AND unicode_lower(trim(f."Category")) = {normalizedCategory}
                    LIMIT 1
                    """)
                .AsNoTracking()
                .AnyAsync(cancellationToken);
        }

        return await _db.FaqEntries
            .AsNoTracking()
            .AnyAsync(x =>
                (!exceptId.HasValue || x.Id != exceptId.Value)
                && x.Question.Trim().ToLower() == normalizedQuestion
                && x.Category.Trim().ToLower() == normalizedCategory,
                cancellationToken);
    }

    private static string NormalizeCategory(string? category)
        => string.IsNullOrWhiteSpace(category) ? "Общее" : category.Trim();

    private static FaqEntryDto MapFaq(FaqEntry entry)
        => new(entry.Id, entry.Revision, entry.Question, entry.Answer, entry.Category, entry.IsActive, entry.ShowOnHome, entry.ShowOnFaqPage, entry.SortOrder, entry.CreatedAt, entry.UpdatedAt);
}
