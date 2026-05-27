using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/app-version")]
public sealed class AppVersionController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public AppVersionController(IApplicationDbContext db)
    {
        _db = db;
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

        var dto = MapRelease(release);
        return Ok(new AppVersionLatestResponse(dto.Version, dto, seen));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var releases = await GetPublishedActiveReleasesAsync(cancellationToken);

        return Ok(releases.Select(MapRelease).ToList());
    }

    [HttpPost("mark-seen")]
    public async Task<IActionResult> MarkSeen([FromBody] AppReleaseMarkSeenRequest request, CancellationToken cancellationToken)
    {
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

        var exists = await _db.AppReleaseSeen.AnyAsync(x => x.UserId == userId && x.AppReleaseId == release.Id, cancellationToken);
        if (!exists)
        {
            _db.AppReleaseSeen.Add(new AppReleaseSeen
            {
                AppReleaseId = release.Id,
                UserId = userId,
                SeenAt = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { release.ReleaseId, seen = true });
    }

    [HttpGet("admin/releases")]
    [Authorize(Policy = AdminPolicies.AdminRead)]
    public async Task<IActionResult> GetAdminReleases(CancellationToken cancellationToken)
    {
        var releases = await _db.AppReleases
            .AsNoTracking()
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        var orderedReleases = releases
            .OrderByDescending(x => x.ReleasedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(MapRelease)
            .ToList();

        return Ok(orderedReleases);
    }

    [HttpPost("admin/releases")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> CreateAdminRelease([FromBody] AppReleaseUpsertRequest request, CancellationToken cancellationToken)
    {
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
            CreatedByUserId = actor.UserId,
            CreatedByUserName = actor.UserName,
            UpdatedByUserId = actor.UserId,
            UpdatedByUserName = actor.UserName
        };
        foreach (var item in MapRequestItems(request.Items))
        {
            item.AppReleaseId = release.Id;
            release.Items.Add(item);
        }

        _db.AppReleases.Add(release);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapRelease(release));
    }

    [HttpPut("admin/releases/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> UpdateAdminRelease(Guid id, [FromBody] AppReleaseUpsertRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateReleaseRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var release = await _db.AppReleases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (release is null)
        {
            return NotFound();
        }

        var releaseId = request.ReleaseId.Trim();
        var duplicate = await _db.AppReleases.AnyAsync(x => x.Id != id && x.ReleaseId == releaseId, cancellationToken);
        if (duplicate)
        {
            return BadRequest(new { error = "ReleaseId already exists." });
        }

        var actor = ResolveActor();
        release.ReleaseId = releaseId;
        release.Version = request.Version.Trim();
        release.ReleasedAt = request.ReleasedAt;
        release.Title = request.Title.Trim();
        release.Summary = request.Summary.Trim();
        release.IsActive = request.IsActive;
        release.Source = NormalizeSource(request.Source);
        release.UpdatedAt = DateTimeOffset.UtcNow;
        release.UpdatedByUserId = actor.UserId;
        release.UpdatedByUserName = actor.UserName;

        await _db.AppReleaseItems
            .Where(x => x.AppReleaseId == release.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var item in MapRequestItems(request.Items))
        {
            item.AppReleaseId = release.Id;
            _db.AppReleaseItems.Add(item);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var updated = await _db.AppReleases
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(MapRelease(updated));
    }

    [HttpDelete("admin/releases/{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> DeleteAdminRelease(Guid id, CancellationToken cancellationToken)
    {
        var release = await _db.AppReleases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (release is null)
        {
            return NotFound();
        }

        _db.AppReleases.Remove(release);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { id, deleted = true });
    }

    private async Task<List<AppRelease>> GetPublishedActiveReleasesAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var releases = await _db.AppReleases
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        return releases
            .Where(x => x.ReleasedAt <= now)
            .OrderByDescending(x => x.ReleasedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Take(50)
            .ToList();
    }

    private static AppReleaseDto MapRelease(AppRelease release)
        => new(
            release.Id,
            release.ReleaseId,
            release.Version,
            release.ReleasedAt,
            release.Title,
            release.Summary,
            release.IsActive,
            release.Source,
            release.Items
                .OrderBy(x => x.SortOrder)
                .Select(x => new AppReleaseItemDto(x.Id, x.Type, x.Text, x.SortOrder))
                .ToList(),
            release.CreatedByUserId,
            release.CreatedByUserName,
            release.UpdatedByUserId,
            release.UpdatedByUserName,
            release.CreatedAt,
            release.UpdatedAt);

    private static IEnumerable<AppReleaseItem> MapRequestItems(IReadOnlyList<AppReleaseItemDto> items)
        => items
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Select((x, index) => new AppReleaseItem
            {
                Type = NormalizeItemType(x.Type),
                Text = x.Text.Trim(),
                SortOrder = x.SortOrder > 0 ? x.SortOrder : (index + 1) * 10
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
