using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AppVersionControllerTests
{
    [Fact]
    public async Task GetHistory_Should_Return_Only_Published_Active_Releases_In_Descending_Order()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        db.AppReleases.AddRange(
            Release("older-release", "1.0.0", DateTimeOffset.UtcNow.AddDays(-2)),
            Release("newer-release", "1.1.0", DateTimeOffset.UtcNow.AddHours(-1)),
            Release("future-release", "2.0.0", DateTimeOffset.UtcNow.AddDays(1)),
            Release("inactive-release", "3.0.0", DateTimeOffset.UtcNow.AddMinutes(-30), isActive: false));
        await db.SaveChangesAsync();

        var history = AssertOk<List<CabinetAppReleaseDto>>(await CreateController(db, userId).GetHistory(CancellationToken.None));

        Assert.Equal(new[] { "newer-release", "older-release" }, history.Select(x => x.ReleaseId));
    }

    [Fact]
    public async Task Cabinet_App_Releases_Should_Not_Expose_Admin_Metadata()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        db.AppReleases.Add(Release("public-release", "1.0.0", DateTimeOffset.UtcNow.AddHours(-1)));
        await db.SaveChangesAsync();

        var result = await CreateController(db, userId).GetHistory(CancellationToken.None);
        var json = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value);

        foreach (var privateField in new[]
                 {
                     "\"id\"", "\"isActive\"", "\"source\"", "\"createdByUserId\"",
                     "\"createdByUserName\"", "\"updatedByUserId\"", "\"updatedByUserName\"",
                     "\"createdAt\"", "\"updatedAt\""
                 })
        {
            Assert.DoesNotContain(privateField, json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void App_Release_Lists_Should_Apply_Database_Limits_Before_Materialization()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "Controllers", "AppVersionController.cs"));

        Assert.Contains("LIMIT 50", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".Take(50)", source, StringComparison.Ordinal);
        Assert.Contains("LIMIT 200", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".Take(200)", source, StringComparison.Ordinal);
        var publishedMethodStart = source.IndexOf("private async Task<List<AppRelease>> GetPublishedActiveReleasesAsync", StringComparison.Ordinal);
        var adminFilterMethodStart = source.IndexOf("private static IQueryable<AppRelease> ApplyAdminFilters", StringComparison.Ordinal);
        Assert.True(publishedMethodStart >= 0 && adminFilterMethodStart > publishedMethodStart);
        var publishedMethod = source[publishedMethodStart..adminFilterMethodStart];
        Assert.True(
            publishedMethod.IndexOf(".Take(50)", StringComparison.Ordinal) < publishedMethod.LastIndexOf("ToListAsync(cancellationToken)", StringComparison.Ordinal),
            "Published app releases must be limited before materialization.");
    }

    [Fact]
    public void Admin_App_Release_Update_Should_Require_Revision_And_Handle_Conflicts()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "Controllers", "AppVersionController.cs"));

        Assert.Contains("request.Revision", source, StringComparison.Ordinal);
        Assert.Contains("DbUpdateConcurrencyException", source, StringComparison.Ordinal);
        Assert.Contains("return Conflict", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_Release_Update_Should_Reject_Stale_Revision_Without_Mutating_Release()
    {
        await using var db = CreateDb();
        var adminId = Guid.NewGuid();
        await SeedUserAsync(db, adminId, UserRoles.Admin);
        var release = Release("concurrent-release", "1.0.0", DateTimeOffset.UtcNow.AddMinutes(-10));
        release.Revision = 2;
        db.AppReleases.Add(release);
        await db.SaveChangesAsync();
        var request = ValidRequest(release.ReleaseId, release.Version) with { Title = "Overwritten", Revision = 1 };

        var result = await CreateController(db, adminId, UserRoles.Admin)
            .UpdateAdminRelease(release.Id, request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("concurrent-release", release.Title);
        Assert.Equal(2, release.Revision);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Published_App_Release_History_Should_Return_Only_The_Latest_50_Releases()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        var start = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        db.AppReleases.AddRange(Enumerable.Range(0, 55).Select(index => Release($"release-{index:D2}", $"1.0.{index}", start.AddMinutes(index))));
        await db.SaveChangesAsync();

        var history = AssertOk<List<CabinetAppReleaseDto>>(
            await CreateController(db, userId).GetHistory(CancellationToken.None));

        Assert.Equal(50, history.Count);
        Assert.Equal("release-54", history[0].ReleaseId);
        Assert.Equal("release-05", history[^1].ReleaseId);
    }

    [Fact]
    public async Task Admin_App_Release_List_Should_Return_Only_The_Latest_200_Releases()
    {
        await using var db = CreateDb();
        var adminId = Guid.NewGuid();
        await SeedUserAsync(db, adminId, UserRoles.Admin);
        var start = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        db.AppReleases.AddRange(Enumerable.Range(0, 205).Select(index => Release($"release-{index:D3}", $"1.0.{index}", start.AddMinutes(index))));
        await db.SaveChangesAsync();

        var releases = AssertOk<List<AppReleaseDto>>(
            await CreateController(db, adminId, UserRoles.Admin).GetAdminReleases(cancellationToken: CancellationToken.None));

        Assert.Equal(200, releases.Count);
        Assert.Equal("release-204", releases[0].ReleaseId);
        Assert.Equal("release-005", releases[^1].ReleaseId);
    }

    [Fact]
    public async Task Admin_Release_Delete_Should_Reject_Stale_Revision_Without_Deleting_Release()
    {
        await using var db = CreateDb();
        var adminId = Guid.NewGuid();
        await SeedUserAsync(db, adminId, UserRoles.Admin);
        var release = Release("delete-conflict", "1.0.0", DateTimeOffset.UtcNow.AddMinutes(-10));
        release.Revision = 3;
        db.AppReleases.Add(release);
        await db.SaveChangesAsync();

        var result = await CreateController(db, adminId, UserRoles.Admin)
            .DeleteAdminRelease(release.Id, 2, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.True(await db.AppReleases.AnyAsync(x => x.Id == release.Id));
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task App_Release_Revision_Should_Reject_Concurrent_Database_Update()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-app-release-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.AppReleases.Add(Release("database-race", "1.0.0", DateTimeOffset.UtcNow.AddMinutes(-5)));
                await setup.SaveChangesAsync();
            }

            await using (var firstDb = new ApplicationDbContext(options))
            await using (var secondDb = new ApplicationDbContext(options))
            {
                var first = await firstDb.AppReleases.SingleAsync();
                var second = await secondDb.AppReleases.SingleAsync();
                first.Title = "First update";
                first.Revision++;
                second.Title = "Second update";
                second.Revision++;

                await firstDb.SaveChangesAsync();

                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondDb.SaveChangesAsync());
            }
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task GetLatest_Should_Return_Unseen_Latest_Published_Release()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        db.AppReleases.AddRange(
            Release("old-release", "1.0.0", DateTimeOffset.UtcNow.AddDays(-2)),
            Release("latest-release", "1.1.0", DateTimeOffset.UtcNow.AddHours(-1)));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.GetLatest(CancellationToken.None);

        var response = AssertOk<AppVersionLatestResponse>(result);
        Assert.Equal("1.1.0", response.CurrentVersion);
        Assert.Equal("latest-release", response.LatestRelease?.ReleaseId);
        Assert.False(response.SeenByCurrentUser);
    }

    [Fact]
    public async Task MarkSeen_Should_Make_Current_Release_Seen_For_User()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        db.AppReleases.Add(Release("release-to-see", "1.2.0", DateTimeOffset.UtcNow.AddHours(-1)));
        await db.SaveChangesAsync();
        var controller = CreateController(db, userId);

        await controller.MarkSeen(new AppReleaseMarkSeenRequest("release-to-see"), CancellationToken.None);
        var result = await controller.GetLatest(CancellationToken.None);

        var response = AssertOk<AppVersionLatestResponse>(result);
        Assert.True(response.SeenByCurrentUser);
        Assert.Equal(1, await db.AppReleaseSeen.CountAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task MarkSeen_Should_Reject_Future_And_Inactive_Releases()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        db.AppReleases.AddRange(
            Release("future-release", "2.0.0", DateTimeOffset.UtcNow.AddDays(1)),
            Release("hidden-release", "3.0.0", DateTimeOffset.UtcNow.AddHours(-1), isActive: false));
        await db.SaveChangesAsync();
        var controller = CreateController(db, userId);

        var future = await controller.MarkSeen(new AppReleaseMarkSeenRequest("future-release"), CancellationToken.None);
        var hidden = await controller.MarkSeen(new AppReleaseMarkSeenRequest("hidden-release"), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(future);
        Assert.IsType<NotFoundObjectResult>(hidden);
        Assert.Empty(await db.AppReleaseSeen.ToListAsync());
    }

    [Fact]
    public async Task GetLatest_Should_Show_New_Release_When_User_Saw_Previous_One()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        var oldRelease = Release("seen-release", "1.0.0", DateTimeOffset.UtcNow.AddDays(-2));
        db.AppReleases.AddRange(oldRelease, Release("new-release", "1.1.0", DateTimeOffset.UtcNow.AddHours(-1)));
        await db.SaveChangesAsync();
        db.AppReleaseSeen.Add(new AppReleaseSeen { AppReleaseId = oldRelease.Id, UserId = userId });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.GetLatest(CancellationToken.None);

        var response = AssertOk<AppVersionLatestResponse>(result);
        Assert.Equal("new-release", response.LatestRelease?.ReleaseId);
        Assert.False(response.SeenByCurrentUser);
    }

    [Fact]
    public async Task GetLatest_Should_Ignore_Future_And_Inactive_Releases()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId);
        db.AppReleases.AddRange(
            Release("published", "1.0.0", DateTimeOffset.UtcNow.AddHours(-1)),
            Release("future", "2.0.0", DateTimeOffset.UtcNow.AddDays(1)),
            Release("inactive", "3.0.0", DateTimeOffset.UtcNow.AddMinutes(-10), isActive: false));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var result = await controller.GetLatest(CancellationToken.None);

        var response = AssertOk<AppVersionLatestResponse>(result);
        Assert.Equal("published", response.LatestRelease?.ReleaseId);
    }

    [Fact]
    public async Task AdminReleaseCrud_Should_Create_Update_And_Delete_Release()
    {
        await using var db = CreateDb();
        var adminId = Guid.NewGuid();
        await SeedUserAsync(db, adminId, UserRoles.Admin);
        var controller = CreateController(db, adminId, UserRoles.Admin);
        var createRequest = new AppReleaseUpsertRequest(
            "manual-release",
            "1.0.0",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            "Первый релиз",
            "Описание релиза",
            true,
            "manual",
            new[] { new AppReleaseItemDto(Guid.Empty, "new", "Добавлен раздел", 10) });

        var created = AssertOk<AppReleaseDto>(await controller.CreateAdminRelease(createRequest, CancellationToken.None));
        var list = AssertOk<List<AppReleaseDto>>(await controller.GetAdminReleases(cancellationToken: CancellationToken.None));
        Assert.Contains(list, x => x.ReleaseId == "manual-release");
        db.ChangeTracker.Clear();
        var updateRequest = createRequest with
        {
            Title = "Обновленный релиз",
            Revision = created.Revision,
            Items = new[] { new AppReleaseItemDto(Guid.Empty, "fixed", "Исправлено описание", 10) }
        };

        var updated = AssertOk<AppReleaseDto>(await controller.UpdateAdminRelease(created.Id, updateRequest, CancellationToken.None));
        db.ChangeTracker.Clear();
        var deleted = await controller.DeleteAdminRelease(created.Id, updated.Revision, CancellationToken.None);

        Assert.Equal("Обновленный релиз", updated.Title);
        Assert.Equal("fixed", updated.Items[0].Type);
        Assert.IsType<OkObjectResult>(deleted);
        Assert.Empty(await db.AppReleases.ToListAsync());
        var audits = await db.AuditLogs.ToListAsync();
        Assert.Equal(3, audits.Count);
        Assert.Contains(audits, x => x.Action == "app_release.create" && x.EntityId == created.Id.ToString() && x.ActorId == adminId.ToString());
        Assert.Contains(audits, x => x.Action == "app_release.update" && x.EntityId == created.Id.ToString() && x.BeforeJson != x.AfterJson);
        Assert.Contains(audits, x => x.Action == "app_release.delete" && x.EntityId == created.Id.ToString() && x.AfterJson == "{}");
        Assert.Empty(await db.AppReleaseItems.ToListAsync());
    }

    [Fact]
    public async Task AdminReleases_Should_Filter_And_Report_Overview()
    {
        await using var db = CreateDb();
        var adminId = Guid.NewGuid();
        await SeedUserAsync(db, adminId, UserRoles.Admin);
        var published = Release("published-agent", "1.0.0", DateTimeOffset.UtcNow.AddHours(-2));
        var upcoming = Release("upcoming-manual", "1.1.0", DateTimeOffset.UtcNow.AddDays(1));
        upcoming.Source = "manual";
        var hidden = Release("hidden-agent", "0.9.0", DateTimeOffset.UtcNow.AddDays(-3), isActive: false);
        db.AppReleases.AddRange(published, upcoming, hidden);
        db.AppReleaseSeen.Add(new AppReleaseSeen { AppReleaseId = published.Id, UserId = adminId });
        await db.SaveChangesAsync();
        var controller = CreateController(db, adminId, UserRoles.Admin);

        var publishedOnly = AssertOk<List<AppReleaseDto>>(await controller.GetAdminReleases(visibility: "published", cancellationToken: CancellationToken.None));
        var manualOnly = AssertOk<List<AppReleaseDto>>(await controller.GetAdminReleases(source: "manual", cancellationToken: CancellationToken.None));
        var searchOnly = AssertOk<List<AppReleaseDto>>(await controller.GetAdminReleases(search: "hidden", cancellationToken: CancellationToken.None));
        using var overview = ToJson(await controller.GetAdminReleasesOverview(CancellationToken.None));
        var root = overview.RootElement;

        Assert.Equal("published-agent", Assert.Single(publishedOnly).ReleaseId);
        Assert.Equal("upcoming-manual", Assert.Single(manualOnly).ReleaseId);
        Assert.Equal("hidden-agent", Assert.Single(searchOnly).ReleaseId);
        Assert.Equal(3, root.GetProperty("TotalCount").GetInt32());
        Assert.Equal(1, root.GetProperty("PublishedCount").GetInt32());
        Assert.Equal(1, root.GetProperty("UpcomingCount").GetInt32());
        Assert.Equal(1, root.GetProperty("HiddenCount").GetInt32());
        Assert.Equal(1, root.GetProperty("SeenCount").GetInt32());
        Assert.Equal("published-agent", root.GetProperty("LatestPublishedReleaseId").GetString());
    }

    private static ApplicationDbContext CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task SeedUserAsync(ApplicationDbContext db, Guid userId, string roles = UserRoles.User)
    {
        db.Users.Add(new User
        {
            Id = userId,
            Email = $"{userId:N}@test.local",
            DisplayName = "Test user",
            PasswordHash = "hash",
            RolesCsv = roles
        });
        await db.SaveChangesAsync();
    }

    private static AppRelease Release(string releaseId, string version, DateTimeOffset releasedAt, bool isActive = true)
        => new()
        {
            ReleaseId = releaseId,
            Version = version,
            ReleasedAt = releasedAt,
            Title = releaseId,
            Summary = $"Описание {releaseId}",
            IsActive = isActive,
            Source = "agent",
            Items = new List<AppReleaseItem>
            {
                new() { Type = "new", Text = $"Пункт {releaseId}", SortOrder = 10 }
            }
        };

    private static AppReleaseUpsertRequest ValidRequest(string releaseId, string version)
        => new(
            releaseId,
            version,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            releaseId,
            $"Описание {releaseId}",
            true,
            "manual",
            new[] { new AppReleaseItemDto(null, "new", $"Пункт {releaseId}", 10) });

    private static AppVersionController CreateController(ApplicationDbContext db, Guid userId, string role = UserRoles.User)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "Tester"),
            new Claim(ClaimTypes.Role, role)
        }, "Test");

        return new AppVersionController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static T AssertOk<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<T>(ok.Value);
    }

    private static JsonDocument ToJson(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "backend")) && Directory.Exists(Path.Combine(directory.FullName, "frontend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
