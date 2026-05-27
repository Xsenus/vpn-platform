using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;

namespace VpnPlatform.Infrastructure.Services;

public sealed class AppReleaseSeedService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IHostEnvironment _environment;
    private readonly ILogger<AppReleaseSeedService> _logger;

    public AppReleaseSeedService(IHostEnvironment environment, ILogger<AppReleaseSeedService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<int> SyncAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
        => await SyncAsync(db, _environment.ContentRootPath, cancellationToken);

    public async Task<int> SyncAsync(ApplicationDbContext db, string contentRootPath, CancellationToken cancellationToken = default)
    {
        var releasesPath = Path.Combine(contentRootPath, "AppReleases", "releases.json");
        if (!File.Exists(releasesPath))
        {
            _logger.LogInformation("App releases seed file not found at {Path}", releasesPath);
            return 0;
        }

        await using var stream = File.OpenRead(releasesPath);
        var seedItems = await JsonSerializer.DeserializeAsync<List<AppReleaseSeedItem>>(stream, JsonOptions, cancellationToken) ?? new();
        var seedByReleaseId = seedItems
            .Where(x => !string.IsNullOrWhiteSpace(x.ReleaseId))
            .GroupBy(x => x.ReleaseId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Last())
            .ToDictionary(x => x.ReleaseId.Trim(), StringComparer.OrdinalIgnoreCase);

        var existing = await db.AppReleases
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var seed in seedByReleaseId.Values)
        {
            var releaseId = seed.ReleaseId.Trim();
            var release = existing.FirstOrDefault(x => string.Equals(x.ReleaseId, releaseId, StringComparison.OrdinalIgnoreCase));
            if (release is not null && string.Equals(release.Source, "manual", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (release is null)
            {
                release = new AppRelease
                {
                    ReleaseId = releaseId,
                    Source = "agent"
                };
                db.AppReleases.Add(release);
                existing.Add(release);
            }

            release.Version = seed.Version.Trim();
            release.ReleasedAt = seed.ReleasedAt.ToUniversalTime();
            release.Title = seed.Title.Trim();
            release.Summary = seed.Summary.Trim();
            release.IsActive = seed.IsActive;
            release.Source = string.IsNullOrWhiteSpace(seed.Source) ? "agent" : seed.Source.Trim();
            release.UpdatedAt = DateTimeOffset.UtcNow;

            var currentItems = release.Items.ToList();
            db.AppReleaseItems.RemoveRange(currentItems);
            release.Items.Clear();

            var seedItemsToAdd = seed.Items
                .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                .Select((x, index) => new AppReleaseItem
                {
                    AppReleaseId = release.Id,
                    Type = NormalizeItemType(x.Type),
                    Text = x.Text.Trim(),
                    SortOrder = x.SortOrder ?? (index + 1) * 10
                })
                .ToList();

            foreach (var item in seedItemsToAdd)
            {
                release.Items.Add(item);
                db.AppReleaseItems.Add(item);
            }

            changed++;
        }

        var seedReleaseIds = seedByReleaseId.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedAgentReleases = existing
            .Where(x => string.Equals(x.Source, "agent", StringComparison.OrdinalIgnoreCase) && !seedReleaseIds.Contains(x.ReleaseId))
            .ToList();
        if (removedAgentReleases.Count > 0)
        {
            db.AppReleases.RemoveRange(removedAgentReleases);
            changed += removedAgentReleases.Count;
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("App releases seed synchronized. Changed={Changed}, SeedItems={SeedItems}", changed, seedByReleaseId.Count);
        return changed;
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

    public sealed class AppReleaseSeedItem
    {
        [JsonPropertyName("releaseId")]
        public string ReleaseId { get; set; } = string.Empty;
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
        [JsonPropertyName("releasedAt")]
        public DateTimeOffset ReleasedAt { get; set; } = DateTimeOffset.UtcNow;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
        [JsonPropertyName("source")]
        public string? Source { get; set; }
        [JsonPropertyName("items")]
        public List<AppReleaseSeedItemEntry> Items { get; set; } = new();
    }

    public sealed class AppReleaseSeedItemEntry
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }
    }
}
