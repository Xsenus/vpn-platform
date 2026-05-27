using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AppReleaseSeedServiceTests
{
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
            "source": "agent",
            "items": [
              { "type": "improved", "text": "Улучшена админка", "sortOrder": 20 }
            ]
          }
        ]
        """);

        var changed = await CreateService(root).SyncAsync(db, root);

        Assert.Equal(1, changed);
        var release = await db.AppReleases.Include(x => x.Items).SingleAsync();
        Assert.Equal("json-release", release.ReleaseId);
        Assert.Equal("Что нового", release.Title);
        Assert.Equal("improved", release.Items.Single().Type);
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

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AppReleaseSeedService CreateService(string contentRootPath)
        => new(new TestHostEnvironment(contentRootPath), NullLogger<AppReleaseSeedService>.Instance);

    private static string CreateSeedRoot(string json)
    {
        var root = Path.Combine(Path.GetTempPath(), "vpn-platform-app-releases", Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(root, "AppReleases");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "releases.json"), json);
        return root;
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
