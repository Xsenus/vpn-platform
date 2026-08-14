using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AppReleaseSeedServiceTests : IDisposable
{
    private readonly List<string> _seedRoots = [];

    [Theory]
    [InlineData("Invalid Release ID", "fixed")]
    [InlineData("invalid-item-type", "typo")]
    public async Task SyncAsync_Should_Reject_Invalid_Release_Id_And_Item_Type(string releaseId, string itemType)
    {
        await using var db = CreateDb();
        db.AppReleases.Add(new AppRelease
        {
            ReleaseId = "existing-agent",
            Version = "1.0.0",
            ReleasedAt = new DateTimeOffset(2034, 4, 5, 5, 0, 0, TimeSpan.Zero),
            Title = "Existing release",
            Summary = "Must remain",
            Source = "agent"
        });
        await db.SaveChangesAsync();
        var root = CreateSeedRoot($$"""
        [
          {
            "releaseId": "{{releaseId}}",
            "version": "2.0.0",
            "releasedAt": "2034-04-05T06:00:00Z",
            "title": "Invalid release",
            "summary": "Must be rejected",
            "source": "agent",
            "items": [{ "type": "{{itemType}}", "text": "Invalid item" }]
          }
        ]
        """);

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(root).SyncAsync(db, root));

        Assert.Equal("existing-agent", (await db.AppReleases.SingleAsync()).ReleaseId);
    }

    [Theory]
    [InlineData(-1, 20)]
    [InlineData(10, 10)]
    public async Task SyncAsync_Should_Reject_Negative_And_Duplicate_Item_Order(int firstSortOrder, int secondSortOrder)
    {
        await using var db = CreateDb();
        var root = CreateSeedRoot($$"""
        [
          {
            "releaseId": "invalid-item-order",
            "version": "1.0.0",
            "releasedAt": "2034-04-05T06:00:00Z",
            "title": "Invalid order",
            "summary": "Must be rejected",
            "source": "agent",
            "items": [
              { "type": "new", "text": "First item", "sortOrder": {{firstSortOrder}} },
              { "type": "fixed", "text": "Second item", "sortOrder": {{secondSortOrder}} }
            ]
          }
        ]
        """);

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(root).SyncAsync(db, root));

        Assert.Empty(await db.AppReleases.ToListAsync());
    }

    [Fact]
    public async Task SyncAsync_Should_Reject_Invalid_Seed_Before_Deleting_Existing_Releases()
    {
        await using var db = CreateDb();
        db.AppReleases.Add(new AppRelease
        {
            ReleaseId = "existing-agent",
            Version = "1.0.0",
            ReleasedAt = new DateTimeOffset(2034, 4, 5, 5, 0, 0, TimeSpan.Zero),
            Title = "Existing release",
            Summary = "Must remain",
            Source = "agent"
        });
        await db.SaveChangesAsync();
        var root = CreateSeedRoot("""
        [
          {
            "version": "2.0.0",
            "releasedAt": "2034-04-05T06:00:00Z",
            "title": "Invalid release",
            "summary": "Missing release id",
            "items": [{ "type": "fixed", "text": "Invalid item" }]
          }
        ]
        """);

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(root).SyncAsync(db, root));

        Assert.Equal("existing-agent", (await db.AppReleases.SingleAsync()).ReleaseId);
    }

    [Fact]
    public async Task SyncAsync_Should_Reject_Empty_Seed_Before_Deleting_Existing_Releases()
    {
        await using var db = CreateDb();
        db.AppReleases.Add(new AppRelease
        {
            ReleaseId = "existing-agent",
            Version = "1.0.0",
            ReleasedAt = new DateTimeOffset(2034, 4, 5, 5, 0, 0, TimeSpan.Zero),
            Title = "Existing release",
            Summary = "Must remain",
            Source = "agent"
        });
        await db.SaveChangesAsync();
        var root = CreateSeedRoot("[]");

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(root).SyncAsync(db, root));

        Assert.Equal("existing-agent", (await db.AppReleases.SingleAsync()).ReleaseId);
    }

    [Fact]
    public async Task SyncAsync_Should_Reject_Missing_Or_Default_ReleasedAt()
    {
        foreach (var releasedAtProperty in new[] { string.Empty, "\"releasedAt\": \"0001-01-01T00:00:00Z\"," })
        {
            await using var db = CreateDb();
            var root = CreateSeedRoot($$"""
            [
              {
                "releaseId": "invalid-date",
                "version": "1.0.0",
                {{releasedAtProperty}}
                "title": "Invalid date",
                "summary": "Must be rejected",
                "items": [{ "type": "fixed", "text": "Invalid item" }]
              }
            ]
            """);

            await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(root).SyncAsync(db, root));

            Assert.Empty(await db.AppReleases.ToListAsync());
        }
    }

    [Fact]
    public async Task SyncAsync_Should_Reject_Duplicate_ReleaseIds()
    {
        await using var db = CreateDb();
        var root = CreateSeedRoot("""
        [
          {
            "releaseId": "duplicate-release",
            "version": "1.0.0",
            "releasedAt": "2034-04-05T06:00:00Z",
            "title": "First",
            "summary": "First copy",
            "items": [{ "type": "new", "text": "First item" }]
          },
          {
            "releaseId": "DUPLICATE-RELEASE",
            "version": "2.0.0",
            "releasedAt": "2034-04-05T07:00:00Z",
            "title": "Second",
            "summary": "Second copy",
            "items": [{ "type": "fixed", "text": "Second item" }]
          }
        ]
        """);

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(root).SyncAsync(db, root));

        Assert.Empty(await db.AppReleases.ToListAsync());
    }

    [Fact]
    public async Task SyncAsync_Should_Reject_Manual_Source_In_Agent_Seed()
    {
        await using var db = CreateDb();
        var root = CreateSeedRoot("""
        [
          {
            "releaseId": "manual-from-seed",
            "version": "1.0.0",
            "releasedAt": "2034-04-05T06:00:00Z",
            "title": "Wrong ownership",
            "summary": "Agent seed must not create manual releases",
            "source": "manual",
            "items": [{ "type": "fixed", "text": "Invalid ownership" }]
          }
        ]
        """);

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateService(root).SyncAsync(db, root));

        Assert.Empty(await db.AppReleases.ToListAsync());
    }

    [Fact]
    public async Task SyncAsync_Should_Use_Injected_Clock_For_Release_And_Items()
    {
        await using var db = CreateDb();
        var initialTime = new DateTimeOffset(2034, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var clock = new MutableClock(initialTime);
        var root = CreateSeedRoot("""
        [
          {
            "releaseId": "clock-release",
            "version": "1.0.0",
            "releasedAt": "2034-04-05T05:00:00Z",
            "title": "Clock release",
            "summary": "Clock summary",
            "isActive": true,
            "source": "agent",
            "items": [{ "type": "fixed", "text": "Clock item" }]
          }
        ]
        """);
        var service = CreateService(root, clock);

        await service.SyncAsync(db, root);
        var created = await db.AppReleases.Include(x => x.Items).SingleAsync();

        Assert.Equal(initialTime, created.CreatedAt);
        Assert.Equal(initialTime, created.UpdatedAt);
        Assert.Equal(initialTime, created.Items.Single().CreatedAt);
        Assert.Equal(initialTime, created.Items.Single().UpdatedAt);

        clock.UtcNow = initialTime.AddHours(1);
        await service.SyncAsync(db, root);
        var updated = await db.AppReleases.Include(x => x.Items).SingleAsync();

        Assert.Equal(initialTime, updated.CreatedAt);
        Assert.Equal(clock.UtcNow, updated.UpdatedAt);
        Assert.Equal(clock.UtcNow, updated.Items.Single().CreatedAt);
        Assert.Equal(clock.UtcNow, updated.Items.Single().UpdatedAt);
    }

    [Fact]
    public async Task SyncAsync_Should_Create_Agent_Release_From_Json()
    {
        await using var db = CreateDb();
        var root = CreateSeedRoot("""
        [
          {
            "releaseId": "json-release",
            "version": "1.0.0",
            "releasedAt": "2026-05-27T09:00:00Z",
            "title": "Что нового",
            "summary": "Описание релиза",
            "isActive": true,
            "source": " AGENT ",
            "items": [
              { "type": " IMPROVED ", "text": "Улучшена админка", "sortOrder": 20 }
            ]
          }
        ]
        """);

        var changed = await CreateService(root).SyncAsync(db, root);

        Assert.Equal(1, changed);
        var release = await db.AppReleases.Include(x => x.Items).SingleAsync();
        Assert.Equal("json-release", release.ReleaseId);
        Assert.Equal("Что нового", release.Title);
        Assert.Equal("agent", release.Source);
        Assert.Equal("improved", release.Items.Single().Type);
    }

    [Fact]
    public async Task SyncAsync_Should_Normalize_Release_Date_To_Utc()
    {
        await using var db = CreateDb();
        var root = CreateSeedRoot("""
        [
          {
            "releaseId": "timezone-release",
            "version": "1.0.0",
            "releasedAt": "2026-05-27T22:08:00+07:00",
            "title": "Релиз с часовым поясом",
            "summary": "Дата должна сохраниться в UTC для PostgreSQL",
            "isActive": true,
            "source": "agent",
            "items": [{ "type": "new", "text": "Пункт" }]
          }
        ]
        """);

        await CreateService(root).SyncAsync(db, root);

        var release = await db.AppReleases.SingleAsync();
        Assert.Equal(TimeSpan.Zero, release.ReleasedAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 5, 27, 15, 8, 0, TimeSpan.Zero), release.ReleasedAt);
    }

    [Fact]
    public async Task SyncAsync_Should_Not_Overwrite_Manual_Release_With_Same_ReleaseId()
    {
        await using var db = CreateDb();
        db.AppReleases.Add(new AppRelease
        {
            ReleaseId = "manual-release",
            Version = "manual",
            ReleasedAt = DateTimeOffset.UtcNow,
            Title = "Ручной релиз",
            Summary = "Не перезаписывать",
            Source = "manual",
            Items = new List<AppReleaseItem> { new() { Type = "new", Text = "Ручной пункт", SortOrder = 10 } }
        });
        await db.SaveChangesAsync();
        var root = CreateSeedRoot("""
        [
          {
            "releaseId": "manual-release",
            "version": "agent",
            "releasedAt": "2026-05-27T09:00:00Z",
            "title": "Агентский релиз",
            "summary": "Должен быть пропущен",
            "isActive": true,
            "source": "agent",
            "items": [{ "type": "fixed", "text": "Новый пункт" }]
          }
        ]
        """);

        await CreateService(root).SyncAsync(db, root);

        var release = await db.AppReleases.Include(x => x.Items).SingleAsync();
        Assert.Equal("manual", release.Version);
        Assert.Equal("Ручной релиз", release.Title);
        Assert.Equal("new", release.Items.Single().Type);
    }

    [Fact]
    public async Task SyncAsync_Should_Remove_Only_Agent_Releases_Missing_From_Json()
    {
        await using var db = CreateDb();
        db.AppReleases.AddRange(
            new AppRelease
            {
                ReleaseId = "old-agent",
                Version = "1.0.0",
                ReleasedAt = DateTimeOffset.UtcNow.AddDays(-1),
                Title = "Старый агентский",
                Summary = "Будет удален",
                Source = "agent"
            },
            new AppRelease
            {
                ReleaseId = "manual-release",
                Version = "1.0.0",
                ReleasedAt = DateTimeOffset.UtcNow.AddDays(-1),
                Title = "Ручной",
                Summary = "Останется",
                Source = "manual"
            });
        await db.SaveChangesAsync();
        var root = CreateSeedRoot("""
        [
          {
            "releaseId": "new-agent",
            "version": "2.0.0",
            "releasedAt": "2026-05-27T09:00:00Z",
            "title": "Новый агентский",
            "summary": "Будет создан",
            "isActive": true,
            "source": "agent",
            "items": [{ "type": "new", "text": "Новый пункт" }]
          }
        ]
        """);

        await CreateService(root).SyncAsync(db, root);

        var releaseIds = await db.AppReleases.OrderBy(x => x.ReleaseId).Select(x => x.ReleaseId).ToListAsync();
        Assert.Equal(new[] { "manual-release", "new-agent" }, releaseIds);
    }

    [Fact]
    public async Task SyncAsync_Should_Rewrite_Items_On_Sqlite_Without_Concurrency_Error()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var firstRoot = CreateSeedRoot("""
        [
          {
            "releaseId": "sqlite-release",
            "version": "1.0.0",
            "releasedAt": "2026-05-27T09:00:00Z",
            "title": "Первый релиз",
            "summary": "Первое описание",
            "isActive": true,
            "source": "agent",
            "items": [
              { "type": "new", "text": "Первый пункт", "sortOrder": 10 },
              { "type": "fixed", "text": "Второй пункт", "sortOrder": 20 }
            ]
          }
        ]
        """);
        await CreateService(firstRoot).SyncAsync(db, firstRoot);

        var secondRoot = CreateSeedRoot("""
        [
          {
            "releaseId": "sqlite-release",
            "version": "1.1.0",
            "releasedAt": "2026-05-27T10:00:00Z",
            "title": "Обновленный релиз",
            "summary": "Новое описание",
            "isActive": true,
            "source": "agent",
            "items": [
              { "type": "improved", "text": "Новый единственный пункт", "sortOrder": 10 }
            ]
          }
        ]
        """);

        await CreateService(secondRoot).SyncAsync(db, secondRoot);

        var release = await db.AppReleases.Include(x => x.Items).SingleAsync();
        Assert.Equal("1.1.0", release.Version);
        var item = Assert.Single(release.Items);
        Assert.Equal("improved", item.Type);
        Assert.Equal("Новый единственный пункт", item.Text);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AppReleaseSeedService CreateService(string contentRootPath, IClock? clock = null)
        => new(new TestHostEnvironment(contentRootPath), NullLogger<AppReleaseSeedService>.Instance, clock);

    private string CreateSeedRoot(string json)
    {
        var root = Path.Combine(Path.GetTempPath(), "vpn-platform-app-releases", Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(root, "AppReleases");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "releases.json"), json);
        _seedRoots.Add(root);
        return root;
    }

    public void Dispose()
    {
        foreach (var root in _seedRoots)
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
