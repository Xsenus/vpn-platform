using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Infrastructure.Services;

namespace VpnPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/app-version")]
public sealed class AppVersionController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IClock _clock;

    public AppVersionController(IApplicationDbContext db, IClock? clock = null)
    {
        _db = db;
        _clock = clock ?? new SystemClock();
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var release = (await GetPublishedActiveReleasesAsync(cancellationToken)).FirstOrDefault();
        if (release is null)
        {
            return Ok(new AppVersionLatestResponse(null, null, true));
        }

        var seen = await _db.AppReleaseSeen
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.AppReleaseId == release.Id, cancellationToken);

        var dto = MapCabinetRelease(release);
        return Ok(new AppVersionLatestResponse(dto.Version, dto, seen));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var releases = await GetPublishedActiveReleasesAsync(cancellationToken);

        return Ok(releases.Select(MapCabinetRelease).ToList());
    }

    [HttpPost("mark-seen")]
    public async Task<IActionResult> MarkSeen([FromBody] AppReleaseMarkSeenRequest request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var releaseId = request.ReleaseId.Trim();
        if (string.IsNullOrWhiteSpace(releaseId))
        {
            return BadRequest(new { error = "releaseId is required." });
        }

        var userId = ResolveUserId();
        var release = await _db.AppReleases.FirstOrDefaultAsync(x => x.ReleaseId == releaseId, cancellationToken);
        if (release is null)
        {
            return NotFound(new { error = "Release not found." });
        }
        if (!release.IsActive || release.ReleasedAt > now)
        {
            return NotFound(new { error = "Release is not published." });
        }

        var exists = await _db.AppReleaseSeen.AnyAsync(x => x.UserId == userId && x.AppReleaseId == release.Id, cancellationToken);
        if (!exists)
        {
            _db.AppReleaseSeen.Add(new AppReleaseSeen
            {
                AppReleaseId = release.Id,
                UserId = userId,
                SeenAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { release.ReleaseId, seen = true });
    }

    [HttpGet("admin/releases")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetAdminReleases(
        [FromQuery] string? visibility = null,
        [FromQuery] string? source = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        IQueryable<AppRelease> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            var normalizedVisibility = NormalizeVisibility(visibility);
            var normalizedSource = NormalizeSourceFilter(source);
            var normalizedSearch = search?.Trim().ToLowerInvariant() ?? string.Empty;
            query = _db.AppReleases.FromSqlInterpolated($"""
                SELECT r.*
                FROM "AppReleases" AS r
                WHERE ({normalizedVisibility} = 'all'
                    OR ({normalizedVisibility} = 'published' AND r."IsActive" = 1 AND julianday(r."ReleasedAt") <= julianday({now}))
                    OR ({normalizedVisibility} = 'upcoming' AND r."IsActive" = 1 AND julianday(r."ReleasedAt") > julianday({now}))
                    OR ({normalizedVisibility} = 'hidden' AND r."IsActive" = 0))
                  AND ({normalizedSource} = 'all' OR lower(r."Source") = {normalizedSource})
                  AND ({normalizedSearch} = ''
                    OR instr(lower(r."ReleaseId"), {normalizedSearch}) > 0
                    OR instr(lower(r."Version"), {normalizedSearch}) > 0
                    OR instr(lower(r."Title"), {normalizedSearch}) > 0
                    OR instr(lower(r."Summary"), {normalizedSearch}) > 0
                    OR EXISTS (
                        SELECT 1 FROM "AppReleaseItems" AS i
                        WHERE i."AppReleaseId" = r."Id" AND instr(lower(i."Text"), {normalizedSearch}) > 0))
                ORDER BY julianday(r."ReleasedAt") DESC, julianday(r."CreatedAt") DESC, r."Id" DESC
                LIMIT 200
                """);
        }
        else
        {
            query = ApplyAdminFilters(_db.AppReleases.AsNoTracking(), visibility, source, search, now)
                .OrderByDescending(x => x.ReleasedAt)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(200);
        }

        var orderedReleases = (await query
                .AsNoTracking()
                .Include(x => x.Items)
                .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.ReleasedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(MapAdminRelease)
            .ToList();

        return Ok(orderedReleases);
    }

    [HttpGet("admin/releases/overview")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetAdminReleasesOverview(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        IQueryable<AppRelease> publishedQuery;
        IQueryable<AppRelease> upcomingQuery;
        IQueryable<AppRelease> emptyQuery;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            publishedQuery = _db.AppReleases.FromSqlInterpolated($"""
                SELECT r.* FROM "AppReleases" AS r
                WHERE r."IsActive" = 1 AND julianday(r."ReleasedAt") <= julianday({now})
                """);
            upcomingQuery = _db.AppReleases.FromSqlInterpolated($"""
                SELECT r.* FROM "AppReleases" AS r
                WHERE r."IsActive" = 1 AND julianday(r."ReleasedAt") > julianday({now})
                """);
            emptyQuery = _db.AppReleases.FromSqlRaw("""
                SELECT r.* FROM "AppReleases" AS r
                WHERE NOT EXISTS (
                    SELECT 1 FROM "AppReleaseItems" AS i
                    WHERE i."AppReleaseId" = r."Id" AND trim(i."Text") <> '')
                ORDER BY r."ReleaseId"
                LIMIT 200
                """);
        }
        else
        {
            publishedQuery = _db.AppReleases.Where(x => x.IsActive && x.ReleasedAt <= now);
            upcomingQuery = _db.AppReleases.Where(x => x.IsActive && x.ReleasedAt > now);
            emptyQuery = _db.AppReleases
                .Where(x => !x.Items.Any(item => item.Text.Trim() != string.Empty))
                .OrderBy(x => x.ReleaseId)
                .Take(200);
        }

        var totalCount = await _db.AppReleases.AsNoTracking().CountAsync(cancellationToken);
        var publishedCount = await publishedQuery.AsNoTracking().CountAsync(cancellationToken);
        var upcomingCount = await upcomingQuery.AsNoTracking().CountAsync(cancellationToken);
        var hiddenCount = await _db.AppReleases.AsNoTracking().CountAsync(x => !x.IsActive, cancellationToken);
        var agentCount = await _db.AppReleases.AsNoTracking().CountAsync(x => x.Source.ToLower() == "agent", cancellationToken);
        var latestPublished = await GetLatestPublishedReleaseSummaryAsync(now, cancellationToken);
        var emptyReleaseIds = await emptyQuery.AsNoTracking().Select(x => x.ReleaseId).ToArrayAsync(cancellationToken);

        return Ok(new
        {
            TotalCount = totalCount,
            PublishedCount = publishedCount,
            UpcomingCount = upcomingCount,
            HiddenCount = hiddenCount,
            AgentCount = agentCount,
            ManualCount = totalCount - agentCount,
            SeenCount = await _db.AppReleaseSeen.AsNoTracking().CountAsync(cancellationToken),
            LatestPublishedReleaseId = latestPublished?.ReleaseId,
            LatestPublishedVersion = latestPublished?.Version,
            EmptyReleaseIds = emptyReleaseIds
        });
    }

    [HttpPost("admin/releases")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> CreateAdminRelease([FromBody] AppReleaseUpsertRequest request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var validationError = ValidateReleaseRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var releaseId = request.ReleaseId.Trim();
        var exists = await _db.AppReleases.AnyAsync(x => x.ReleaseId == releaseId, cancellationToken);
        if (exists)
        {
            return BadRequest(new { error = "ReleaseId already exists." });
        }

        var actor = ResolveActor();
        var release = new AppRelease
        {
            ReleaseId = releaseId,
            Version = request.Version.Trim(),
            ReleasedAt = request.ReleasedAt,
            Title = request.Title.Trim(),
            Summary = request.Summary.Trim(),
            IsActive = request.IsActive,
            Source = NormalizeSource(request.Source),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = actor.UserId,
            CreatedByUserName = actor.UserName,
            UpdatedByUserId = actor.UserId,
            UpdatedByUserName = actor.UserName
        };
        foreach (var item in MapRequestItems(request.Items, now))
        {
            item.AppReleaseId = release.Id;
            release.Items.Add(item);
        }

        _db.AppReleases.Add(release);
        AdminAuditLogWriter.Add(_db, this, "app_release.create", "AppRelease", release.Id, null, MapAdminRelease(release));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapAdminRelease(release));
    }

    [HttpPut("admin/releases/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> UpdateAdminRelease(Guid id, [FromBody] AppReleaseUpsertRequest request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var validationError = ValidateReleaseRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var release = await _db.AppReleases
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (release is null)
        {
            return NotFound();
        }

        if (!request.Revision.HasValue || request.Revision.Value < 0)
        {
            return BadRequest(new { error = "App release revision is required and must be a non-negative integer." });
        }
        if (request.Revision.Value != release.Revision)
        {
            return Conflict(new { error = "App release changed. Reload it and retry.", revision = release.Revision });
        }

        var releaseId = request.ReleaseId.Trim();
        var duplicate = await _db.AppReleases.AnyAsync(x => x.Id != id && x.ReleaseId == releaseId, cancellationToken);
        if (duplicate)
        {
            return BadRequest(new { error = "ReleaseId already exists." });
        }

        var before = MapAdminRelease(release);
        var actor = ResolveActor();
        release.ReleaseId = releaseId;
        release.Version = request.Version.Trim();
        release.ReleasedAt = request.ReleasedAt;
        release.Title = request.Title.Trim();
        release.Summary = request.Summary.Trim();
        release.IsActive = request.IsActive;
        release.Source = NormalizeSource(request.Source);
        release.Revision = checked(release.Revision + 1);
        release.UpdatedAt = now;
        release.UpdatedByUserId = actor.UserId;
        release.UpdatedByUserName = actor.UserName;

        var nextItems = MapRequestItems(request.Items, now).ToList();
        _db.AppReleaseItems.RemoveRange(release.Items);
        foreach (var item in nextItems)
        {
            item.AppReleaseId = release.Id;
            _db.AppReleaseItems.Add(item);
        }

        AdminAuditLogWriter.Add(_db, this, "app_release.update", "AppRelease", release.Id, before, MapAdminRelease(release, nextItems));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "App release changed. Reload it and retry." });
        }
        var updated = await _db.AppReleases
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(MapAdminRelease(updated));
    }

    [HttpDelete("admin/releases/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> DeleteAdminRelease(Guid id, [FromQuery] int? revision, CancellationToken cancellationToken)
    {
        var release = await _db.AppReleases
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (release is null)
        {
            return NotFound();
        }
        if (!revision.HasValue || revision.Value < 0)
        {
            return BadRequest(new { error = "App release revision is required and must be a non-negative integer." });
        }
        if (revision.Value != release.Revision)
        {
            return Conflict(new { error = "App release changed. Reload it and retry.", revision = release.Revision });
        }

        var before = MapAdminRelease(release);
        _db.AppReleases.Remove(release);
        AdminAuditLogWriter.Add(_db, this, "app_release.delete", "AppRelease", release.Id, before, null);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "App release changed. Reload it and retry." });
        }
        return Ok(new { id, deleted = true });
    }

    private async Task<AppRelease?> GetLatestPublishedReleaseSummaryAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            return await _db.AppReleases.FromSqlInterpolated($"""
                    SELECT r.* FROM "AppReleases" AS r
                    WHERE r."IsActive" = 1 AND julianday(r."ReleasedAt") <= julianday({now})
                    ORDER BY julianday(r."ReleasedAt") DESC, julianday(r."CreatedAt") DESC, r."Id" DESC
                    LIMIT 1
                    """)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await _db.AppReleases
            .AsNoTracking()
            .Where(x => x.IsActive && x.ReleasedAt <= now)
            .OrderByDescending(x => x.ReleasedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<List<AppRelease>> GetPublishedActiveReleasesAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        IQueryable<AppRelease> query;
        if (_db is DbContext dbContext && dbContext.Database.IsSqlite())
        {
            query = _db.AppReleases.FromSqlInterpolated($"""
                SELECT r.*
                FROM "AppReleases" AS r
                WHERE r."IsActive" = 1 AND julianday(r."ReleasedAt") <= julianday({now})
                ORDER BY julianday(r."ReleasedAt") DESC, julianday(r."CreatedAt") DESC, r."Id" DESC
                LIMIT 50
                """);
        }
        else
        {
            query = _db.AppReleases
                .Where(x => x.IsActive && x.ReleasedAt <= now)
                .OrderByDescending(x => x.ReleasedAt)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(50);
        }

        return (await query
                .AsNoTracking()
                .Include(x => x.Items)
                .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.ReleasedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToList();
    }

    private static IQueryable<AppRelease> ApplyAdminFilters(
        IQueryable<AppRelease> releases,
        string? visibility,
        string? source,
        string? search,
        DateTimeOffset now)
    {
        var filtered = releases;
        if (!string.IsNullOrWhiteSpace(visibility) && !string.Equals(visibility, "all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = visibility.Trim().ToLowerInvariant() switch
            {
                "published" => filtered.Where(x => x.IsActive && x.ReleasedAt <= now),
                "upcoming" => filtered.Where(x => x.IsActive && x.ReleasedAt > now),
                "hidden" => filtered.Where(x => !x.IsActive),
                _ => filtered
            };
        }

        if (!string.IsNullOrWhiteSpace(source) && !string.Equals(source, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedSource = NormalizeSource(source);
            filtered = filtered.Where(x => x.Source.ToLower() == normalizedSource);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            filtered = filtered.Where(x =>
                x.ReleaseId.ToLower().Contains(normalizedSearch) ||
                x.Version.ToLower().Contains(normalizedSearch) ||
                x.Title.ToLower().Contains(normalizedSearch) ||
                x.Summary.ToLower().Contains(normalizedSearch) ||
                x.Items.Any(item => item.Text.ToLower().Contains(normalizedSearch)));
        }

        return filtered;
    }

    private static AppReleaseDto MapAdminRelease(AppRelease release)
        => MapAdminRelease(release, release.Items);

    private static AppReleaseDto MapAdminRelease(AppRelease release, IEnumerable<AppReleaseItem> items)
        => new(
            release.Id,
            release.Revision,
            release.ReleaseId,
            release.Version,
            release.ReleasedAt,
            release.Title,
            release.Summary,
            release.IsActive,
            release.Source,
            items
                .OrderBy(x => x.SortOrder)
                .Select(x => new AppReleaseItemDto(x.Id, x.Type, x.Text, x.SortOrder))
                .ToList(),
            release.CreatedByUserId,
            release.CreatedByUserName,
            release.UpdatedByUserId,
            release.UpdatedByUserName,
            release.CreatedAt,
            release.UpdatedAt);

    private static CabinetAppReleaseDto MapCabinetRelease(AppRelease release)
        => new(
            release.ReleaseId,
            release.Version,
            release.ReleasedAt,
            release.Title,
            release.Summary,
            release.Items
                .OrderBy(x => x.SortOrder)
                .Select(x => new CabinetAppReleaseItemDto(x.Type, x.Text))
                .ToList());

    private static IEnumerable<AppReleaseItem> MapRequestItems(IReadOnlyList<AppReleaseItemDto> items, DateTimeOffset now)
        => items
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Select((x, index) => new AppReleaseItem
            {
                Type = NormalizeItemType(x.Type),
                Text = x.Text.Trim(),
                SortOrder = x.SortOrder > 0 ? x.SortOrder : (index + 1) * 10,
                CreatedAt = now,
                UpdatedAt = now
            });

    private static string? ValidateReleaseRequest(AppReleaseUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReleaseId)) return "ReleaseId is required.";
        if (request.ReleaseId.Length > 160) return "ReleaseId must be 160 characters or less.";
        if (string.IsNullOrWhiteSpace(request.Version)) return "Version is required.";
        if (request.Version.Length > 40) return "Version must be 40 characters or less.";
        if (string.IsNullOrWhiteSpace(request.Title)) return "Title is required.";
        if (request.Title.Length > 200) return "Title must be 200 characters or less.";
        if (string.IsNullOrWhiteSpace(request.Summary)) return "Summary is required.";
        if (request.Items.Count == 0 || request.Items.All(x => string.IsNullOrWhiteSpace(x.Text))) return "At least one release item is required.";
        return null;
    }

    private static string NormalizeItemType(string? type)
        => (type ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "new" => "new",
            "improved" => "improved",
            "fixed" => "fixed",
            "important" => "important",
            _ => "new"
        };

    private static string NormalizeSource(string? source)
        => (source ?? string.Empty).Trim().ToLowerInvariant() == "agent" ? "agent" : "manual";

    private static string NormalizeVisibility(string? visibility)
        => (visibility ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "published" => "published",
            "upcoming" => "upcoming",
            "hidden" => "hidden",
            _ => "all"
        };

    private static string NormalizeSourceFilter(string? source)
        => string.IsNullOrWhiteSpace(source) || string.Equals(source, "all", StringComparison.OrdinalIgnoreCase)
            ? "all"
            : NormalizeSource(source);

    private Guid ResolveUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var value) ? value : Guid.Empty;
    }

    private (Guid? UserId, string UserName) ResolveActor()
    {
        var userId = ResolveUserId();
        var name = User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? User.Identity?.Name
            ?? "admin";
        return (userId == Guid.Empty ? null : userId, name);
    }
}
