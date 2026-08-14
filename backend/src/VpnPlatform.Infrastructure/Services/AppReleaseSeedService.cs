using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
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
    private readonly IClock _clock;

    public AppReleaseSeedService(IHostEnvironment environment, ILogger<AppReleaseSeedService> logger, IClock? clock = null)
    {
        _environment = environment;
        _logger = logger;
        _clock = clock ?? new SystemClock();
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
        ValidateSeedItems(seedItems);
        var seedByReleaseId = seedItems.ToDictionary(x => x.ReleaseId.Trim(), StringComparer.OrdinalIgnoreCase);

        var existing = await db.AppReleases
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
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
                    Source = "agent",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.AppReleases.Add(release);
                existing.Add(release);
            }

            release.Version = seed.Version.Trim();
            release.ReleasedAt = seed.ReleasedAt!.Value.ToUniversalTime();
            release.Title = seed.Title.Trim();
            release.Summary = seed.Summary.Trim();
            release.IsActive = seed.IsActive;
            release.Source = "agent";
            release.UpdatedAt = now;

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
                    SortOrder = ResolveSortOrder(x.SortOrder, index),
                    CreatedAt = now,
                    UpdatedAt = now
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
        => AppReleaseContentPolicy.TryNormalizeItemType(type, out var normalized)
            ? normalized
            : throw new InvalidOperationException("Release item type was not validated.");

    private static int ResolveSortOrder(int? sortOrder, int index)
        => AppReleaseContentPolicy.TryResolveSortOrder(sortOrder, index, out var resolved)
            ? resolved
            : throw new InvalidOperationException("Release item sort order was not validated.");

    private static void ValidateSeedItems(IReadOnlyList<AppReleaseSeedItem> seedItems)
    {
        if (seedItems.Count == 0)
        {
            throw new InvalidDataException("App release seed must contain at least one release.");
        }

        var releaseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < seedItems.Count; index++)
        {
            var seed = seedItems[index]
                ?? throw new InvalidDataException($"App release seed item {index} must be an object.");
            var itemLabel = $"App release seed item {index}";
            if (string.IsNullOrWhiteSpace(seed.ReleaseId))
            {
                throw new InvalidDataException($"{itemLabel} requires releaseId.");
            }

            var releaseId = seed.ReleaseId.Trim();
            if (!AppReleaseContentPolicy.IsValidReleaseId(releaseId))
            {
                throw new InvalidDataException($"{itemLabel} releaseId must be lowercase kebab-case and 160 characters or less.");
            }
            if (!releaseIds.Add(releaseId))
            {
                throw new InvalidDataException($"App release seed contains duplicate releaseId '{releaseId}'.");
            }
            if (string.IsNullOrWhiteSpace(seed.Version))
            {
                throw new InvalidDataException($"{itemLabel} requires version.");
            }
            if (seed.Version.Trim().Length > AppReleaseContentPolicy.VersionMaxLength)
            {
                throw new InvalidDataException($"{itemLabel} version must be 40 characters or less.");
            }
            if (!seed.ReleasedAt.HasValue || seed.ReleasedAt.Value == default)
            {
                throw new InvalidDataException($"{itemLabel} requires releasedAt.");
            }
            if (string.IsNullOrWhiteSpace(seed.Title))
            {
                throw new InvalidDataException($"{itemLabel} requires title.");
            }
            if (seed.Title.Trim().Length > AppReleaseContentPolicy.TitleMaxLength)
            {
                throw new InvalidDataException($"{itemLabel} title must be 200 characters or less.");
            }
            if (string.IsNullOrWhiteSpace(seed.Summary))
            {
                throw new InvalidDataException($"{itemLabel} requires summary.");
            }
            if (seed.Summary.Trim().Length > AppReleaseContentPolicy.SummaryMaxLength)
            {
                throw new InvalidDataException($"{itemLabel} summary must be 4000 characters or less.");
            }
            if (!AppReleaseContentPolicy.TryNormalizeSource(seed.Source, "agent", out var source)
                || !string.Equals(source, "agent", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"{itemLabel} source must be 'agent'.");
            }
            if (seed.Items is null || seed.Items.Count == 0)
            {
                throw new InvalidDataException($"{itemLabel} requires at least one item.");
            }
            if (seed.Items.Count > AppReleaseContentPolicy.MaxItems)
            {
                throw new InvalidDataException($"{itemLabel} cannot contain more than 100 items.");
            }
            var sortOrders = new HashSet<int>();
            for (var itemIndex = 0; itemIndex < seed.Items.Count; itemIndex++)
            {
                var item = seed.Items[itemIndex];
                if (item is null || string.IsNullOrWhiteSpace(item.Text))
                {
                    throw new InvalidDataException($"{itemLabel} item {itemIndex} requires text.");
                }
                if (item.Text.Trim().Length > AppReleaseContentPolicy.ItemTextMaxLength)
                {
                    throw new InvalidDataException($"{itemLabel} item {itemIndex} text must be 4000 characters or less.");
                }
                if (!AppReleaseContentPolicy.TryNormalizeItemType(item.Type, out _))
                {
                    throw new InvalidDataException($"{itemLabel} item {itemIndex} has an unsupported type.");
                }
                if (!AppReleaseContentPolicy.TryResolveSortOrder(item.SortOrder, itemIndex, out var sortOrder))
                {
                    throw new InvalidDataException($"{itemLabel} item {itemIndex} sort order cannot be negative.");
                }
                if (!sortOrders.Add(sortOrder))
                {
                    throw new InvalidDataException($"{itemLabel} item {itemIndex} has a duplicate sort order.");
                }
            }
        }
    }

    public sealed class AppReleaseSeedItem
    {
        [JsonPropertyName("releaseId")]
        public string ReleaseId { get; set; } = string.Empty;
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
        [JsonPropertyName("releasedAt")]
        public DateTimeOffset? ReleasedAt { get; set; }
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
