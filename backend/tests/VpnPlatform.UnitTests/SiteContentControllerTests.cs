using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Api.Controllers.Public;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SiteContentControllerTests
{
    [Fact]
    public async Task AdminSiteContent_Mutations_Should_Use_Injected_Clock()
    {
        await using var db = CreateDb();
        var initialTime = new DateTimeOffset(2033, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var clock = new MutableClock(initialTime);
        var controller = CreateAdminController(db, clock);

        await controller.RestoreHomeDefaults(CancellationToken.None);
        Assert.All(await db.SiteContentBlocks.ToListAsync(), block =>
        {
            Assert.Equal(initialTime, block.CreatedAt);
            Assert.Equal(initialTime, block.UpdatedAt);
        });

        var request = new SiteContentBlockUpsertRequest(
            "clock.custom",
            "Initial value",
            "custom",
            "Clock label",
            "Clock description",
            "text",
            true,
            10);
        var created = AssertOk<SiteContentBlockDto>(await controller.Create(request, CancellationToken.None));
        Assert.Equal(initialTime, created.CreatedAt);
        Assert.Equal(initialTime, created.UpdatedAt);

        clock.UtcNow = initialTime.AddHours(1);
        var updated = AssertOk<SiteContentBlockDto>(await controller.Update(
            created.Id,
            request with { Value = "Updated value", Revision = created.Revision },
            CancellationToken.None));

        Assert.Equal(initialTime, updated.CreatedAt);
        Assert.Equal(clock.UtcNow, updated.UpdatedAt);
    }

    [Fact]
    public void Site_Content_Read_Paths_Should_Keep_Production_Limits_Before_Materialization()
    {
        var repositoryRoot = FindRepositoryRoot();
        var publicSource = File.ReadAllText(Path.Combine(repositoryRoot, "backend", "src", "VpnPlatform.Api", "Controllers", "Public", "ContentController.cs"));
        var adminSource = File.ReadAllText(Path.Combine(repositoryRoot, "backend", "src", "VpnPlatform.Api", "Controllers", "Admin", "AdminSiteContentController.cs"));

        Assert.Contains(".Take(200)", publicSource, StringComparison.Ordinal);
        Assert.Contains(".Take(ListLimit)", adminSource, StringComparison.Ordinal);
        Assert.Contains("CountAsync", adminSource, StringComparison.Ordinal);
        Assert.Contains("GroupBy", adminSource, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildHomeReadiness(blocks)", adminSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicHomeContent_Should_Return_Only_Active_Home_Blocks()
    {
        await using var db = CreateDb();
        db.SiteContentBlocks.AddRange(
            Block("home.hero.title", "Главный заголовок", sortOrder: 20),
            Block("home.hero.subtitle", "Описание", sortOrder: 30),
            Block("home.seo.title", "SEO заголовок", sortOrder: 40),
            Block("home.features.item1", "Преимущество", sortOrder: 50),
            Block("home.footer.text", "Футер главной", sortOrder: 60),
            Block("home.errors.checkoutCreate", "Ошибка покупки", sortOrder: 70),
            Block("home.checkout.afterPaymentText", "После оплаты", sortOrder: 80),
            Block("hidden", "Скрыто", isActive: false),
            Block("footer.text", "Футер", group: "footer"));
        await db.SaveChangesAsync();
        var controller = new ContentController(db);

        var response = AssertOk<List<PublicSiteContentBlockDto>>(await controller.GetHomeContent(CancellationToken.None));

        Assert.Equal(new[] { "home.hero.title", "home.hero.subtitle", "home.seo.title", "home.features.item1", "home.footer.text", "home.errors.checkoutCreate", "home.checkout.afterPaymentText" }, response.Select(x => x.Key).ToArray());
    }

    [Fact]
    public async Task Public_And_Admin_Content_Lists_Should_Be_Bounded_And_Public_Payload_Should_Be_Minimal()
    {
        await using var db = CreateDb();
        db.SiteContentBlocks.AddRange(Enumerable.Range(0, 205)
            .Select(index => Block($"home.audit.{index:D3}", $"Значение {index}", sortOrder: index)));
        await db.SaveChangesAsync();

        var publicResult = await new ContentController(db).GetHomeContent(CancellationToken.None);
        var adminResult = await CreateAdminController(db).Get("home", CancellationToken.None);
        using var publicJson = ToJson(publicResult);
        using var adminJson = ToJson(adminResult);

        Assert.Equal(200, publicJson.RootElement.GetArrayLength());
        Assert.Equal(200, adminJson.RootElement.GetArrayLength());
        var publicItem = publicJson.RootElement[0];
        Assert.Equal(new[] { "Key", "Value" }, publicItem.EnumerateObject().Select(property => property.Name).Order().ToArray());
    }

    [Fact]
    public async Task AdminSiteContent_Should_Create_Update_And_Delete_Block()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);
        var createRequest = new SiteContentBlockUpsertRequest("home.hero.title", "Заголовок", "home", "Hero title", "Первый экран", "text", true, 10);

        var created = AssertOk<SiteContentBlockDto>(await controller.Create(createRequest, CancellationToken.None));
        var list = AssertOk<List<SiteContentBlockDto>>(await controller.Get("home", CancellationToken.None));
        var updated = AssertOk<SiteContentBlockDto>(await controller.Update(created.Id, createRequest with { Value = "Новый заголовок", InputType = "textarea", Revision = created.Revision }, CancellationToken.None));
        var deleted = await controller.Delete(created.Id, updated.Revision, CancellationToken.None);

        Assert.Contains(list, x => x.Key == "home.hero.title");
        Assert.Equal("Новый заголовок", updated.Value);
        Assert.Equal("textarea", updated.InputType);
        Assert.IsType<OkObjectResult>(deleted);
        Assert.Empty(await db.SiteContentBlocks.ToListAsync());
        var audits = await db.AuditLogs.ToListAsync();
        Assert.Equal(3, audits.Count);
        Assert.Contains(audits, x => x.Action == "site_content.create" && x.EntityId == created.Id.ToString() && x.BeforeJson == "{}");
        Assert.Contains(audits, x => x.Action == "site_content.update" && x.EntityId == created.Id.ToString() && x.BeforeJson != x.AfterJson);
        Assert.Contains(audits, x => x.Action == "site_content.delete" && x.EntityId == created.Id.ToString() && x.AfterJson == "{}");
    }

    [Fact]
    public async Task AdminSiteContent_Should_Reject_NoOp_Update_Without_Revision_Or_Audit_Churn()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);
        var request = new SiteContentBlockUpsertRequest("home.hero.title", "Заголовок", "home", "Hero title", "Первый экран", "text", true, 10);
        var created = AssertOk<SiteContentBlockDto>(await controller.Create(request, CancellationToken.None));

        var result = await controller.Update(created.Id, request with { Revision = created.Revision }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        var persisted = await db.SiteContentBlocks.SingleAsync();
        Assert.Equal(created.Revision, persisted.Revision);
        Assert.Single(await db.AuditLogs.ToListAsync(), audit => audit.Action == "site_content.create");
    }

    [Fact]
    public async Task AdminSiteContent_Should_Reject_Empty_Key()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);

        var result = await controller.Create(new SiteContentBlockUpsertRequest("", "value", "home", "Label", "", "text", true, 10), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AdminSiteContent_Should_Isolate_Telegram_Bot_System_Settings()
    {
        await using var db = CreateDb();
        var protectedToken = Block("telegram_bot.bot_token_protected", "v1:protected-token", "telegram_bot");
        var publicSetting = Block("telegram_bot.public_bot_username", "vpn_test_bot", "telegram_bot");
        var regularBlock = Block("home.hero.title", "Заголовок");
        db.SiteContentBlocks.AddRange(protectedToken, publicSetting, regularBlock);
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        var all = AssertOk<List<SiteContentBlockDto>>(await controller.Get(cancellationToken: CancellationToken.None));
        var systemGroup = AssertOk<List<SiteContentBlockDto>>(await controller.Get("telegram_bot", CancellationToken.None));
        var createByGroup = await controller.Create(
            new SiteContentBlockUpsertRequest("custom.key", "tampered", "Telegram_Bot", "System", "", "text", true, 1),
            CancellationToken.None);
        var createByKey = await controller.Create(
            new SiteContentBlockUpsertRequest("Telegram_Bot.mode", "Webhook", "custom", "System", "", "text", true, 1),
            CancellationToken.None);
        var update = await controller.Update(
            protectedToken.Id,
            new SiteContentBlockUpsertRequest(protectedToken.Key, "tampered", protectedToken.Group, protectedToken.Label, "", "text", true, 1, protectedToken.Revision),
            CancellationToken.None);
        var delete = await controller.Delete(publicSetting.Id, publicSetting.Revision, CancellationToken.None);

        Assert.Single(all, item => item.Id == regularBlock.Id);
        Assert.Empty(systemGroup);
        Assert.IsType<BadRequestObjectResult>(createByGroup);
        Assert.IsType<BadRequestObjectResult>(createByKey);
        Assert.IsType<NotFoundResult>(update);
        Assert.IsType<NotFoundResult>(delete);
        Assert.Equal("v1:protected-token", (await db.SiteContentBlocks.SingleAsync(item => item.Id == protectedToken.Id)).Value);
        Assert.Equal("vpn_test_bot", (await db.SiteContentBlocks.SingleAsync(item => item.Id == publicSetting.Id)).Value);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task AdminSiteContent_Should_Report_Readiness_And_Restore_Defaults()
    {
        await using var db = CreateDb();
        db.SiteContentBlocks.AddRange(
            Block("home.hero.title", "", isActive: false, sortOrder: 20),
            Block("home.hero.subtitle", "Описание", sortOrder: 30));
        await db.SaveChangesAsync();
        var admin = CreateAdminController(db);

        using var before = ToJson(await admin.GetHomeReadiness(CancellationToken.None));

        Assert.False(before.RootElement.GetProperty("IsReady").GetBoolean());
        Assert.Contains("home.features.item1", ReadStringArray(before.RootElement.GetProperty("MissingKeys")));
        Assert.Contains("home.hero.title", ReadStringArray(before.RootElement.GetProperty("InactiveKeys")));
        Assert.Contains("home.hero.title", ReadStringArray(before.RootElement.GetProperty("EmptyKeys")));
        Assert.Empty(ReadStringArray(before.RootElement.GetProperty("DuplicateKeys")));

        using var restored = ToJson(await admin.RestoreHomeDefaults(CancellationToken.None));
        var readiness = restored.RootElement.GetProperty("readiness");

        Assert.True(restored.RootElement.GetProperty("created").GetInt32() > 0);
        Assert.True(restored.RootElement.GetProperty("restored").GetInt32() > 0);
        Assert.True(readiness.GetProperty("IsReady").GetBoolean());
        Assert.Empty(ReadStringArray(readiness.GetProperty("DuplicateKeys")));
        Assert.Empty(ReadStringArray(readiness.GetProperty("MissingKeys")));
        Assert.Empty(ReadStringArray(readiness.GetProperty("InactiveKeys")));
        Assert.Empty(ReadStringArray(readiness.GetProperty("EmptyKeys")));

        var publicController = new ContentController(db);
        var publicBlocks = AssertOk<List<PublicSiteContentBlockDto>>(await publicController.GetHomeContent(CancellationToken.None));

        Assert.Contains(publicBlocks, x => x.Key == "home.seo.title");
        Assert.Contains(publicBlocks, x => x.Key == "home.features.item1");
        Assert.Contains(publicBlocks, x => x.Key == "home.finalCta.title");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "site_content.home_defaults.restore");
    }

    [Fact]
    public async Task AdminSiteContent_Should_Reject_Duplicate_Key_On_Create_And_Update()
    {
        await using var db = CreateDb();
        db.SiteContentBlocks.Add(Block("home.hero.title", "Заголовок", sortOrder: 20));
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        var duplicateCreate = await controller.Create(
            new SiteContentBlockUpsertRequest("home.hero.title", "Другой заголовок", "home", "Hero title", "", "text", true, 30),
            CancellationToken.None);
        var created = AssertOk<SiteContentBlockDto>(await controller.Create(
            new SiteContentBlockUpsertRequest("home.hero.subtitle", "Описание", "home", "Hero subtitle", "", "textarea", true, 40),
            CancellationToken.None));
        var duplicateUpdate = await controller.Update(
            created.Id,
            new SiteContentBlockUpsertRequest("home.hero.title", "Описание", "home", "Hero subtitle", "", "textarea", true, 40, created.Revision),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(duplicateCreate);
        Assert.IsType<BadRequestObjectResult>(duplicateUpdate);
        var persisted = await db.SiteContentBlocks.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(created.Key, persisted.Key);
        Assert.Equal(created.Value, persisted.Value);
        Assert.Single(await db.AuditLogs.ToListAsync(), x => x.Action == "site_content.create");
    }

    [Fact]
    public async Task AdminSiteContent_Should_Require_Revision_For_Update_And_Delete()
    {
        await using var db = CreateDb();
        db.SiteContentBlocks.Add(Block("home.hero.title", "Заголовок", sortOrder: 20));
        await db.SaveChangesAsync();
        var block = await db.SiteContentBlocks.SingleAsync();
        var controller = CreateAdminController(db);

        var update = await controller.Update(
            block.Id,
            new SiteContentBlockUpsertRequest(block.Key, "Новый заголовок", block.Group, block.Label, block.Description, block.InputType, true, block.SortOrder),
            CancellationToken.None);
        var delete = await controller.Delete(block.Id, revision: null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(update);
        Assert.IsType<BadRequestObjectResult>(delete);
        Assert.Equal("Заголовок", (await db.SiteContentBlocks.SingleAsync()).Value);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task AdminSiteContent_Should_Reject_Cross_Context_Concurrent_Update()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-site-content-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.SiteContentBlocks.Add(Block("home.hero.title", "Исходный заголовок", sortOrder: 10));
                await setup.SaveChangesAsync();
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var first = await firstDb.SiteContentBlocks.SingleAsync();
            var second = await secondDb.SiteContentBlocks.SingleAsync();
            var firstController = CreateAdminController(firstDb);
            var secondController = CreateAdminController(secondDb);

            var firstResult = await firstController.Update(
                first.Id,
                new SiteContentBlockUpsertRequest(first.Key, "Первое изменение", first.Group, first.Label, first.Description, first.InputType, true, first.SortOrder, first.Revision),
                CancellationToken.None);
            var secondResult = await secondController.Update(
                second.Id,
                new SiteContentBlockUpsertRequest(second.Key, "Второе изменение", second.Group, second.Label, second.Description, second.InputType, true, second.SortOrder, second.Revision),
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(firstResult);
            Assert.IsType<ConflictObjectResult>(secondResult);
            await using var verify = new ApplicationDbContext(options);
            Assert.Equal("Первое изменение", (await verify.SiteContentBlocks.SingleAsync()).Value);

            await using var updateDb = new ApplicationDbContext(options);
            await using var deleteDb = new ApplicationDbContext(options);
            var updateCandidate = await updateDb.SiteContentBlocks.SingleAsync();
            var deleteCandidate = await deleteDb.SiteContentBlocks.SingleAsync();
            var updateResult = await CreateAdminController(updateDb).Update(
                updateCandidate.Id,
                new SiteContentBlockUpsertRequest(updateCandidate.Key, "Изменение перед удалением", updateCandidate.Group, updateCandidate.Label, updateCandidate.Description, updateCandidate.InputType, true, updateCandidate.SortOrder, updateCandidate.Revision),
                CancellationToken.None);
            var staleDelete = await CreateAdminController(deleteDb).Delete(deleteCandidate.Id, deleteCandidate.Revision, CancellationToken.None);

            Assert.IsType<OkObjectResult>(updateResult);
            Assert.IsType<ConflictObjectResult>(staleDelete);
            await using var finalVerify = new ApplicationDbContext(options);
            Assert.Equal("Изменение перед удалением", (await finalVerify.SiteContentBlocks.SingleAsync()).Value);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static SiteContentBlock Block(string key, string value, string group = "home", bool isActive = true, int sortOrder = 100)
        => new()
        {
            Key = key,
            Value = value,
            Group = group,
            Label = key,
            InputType = "text",
            IsActive = isActive,
            SortOrder = sortOrder
        };

    private static AdminSiteContentController CreateAdminController(ApplicationDbContext db, IClock? clock = null)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminSiteContentController(db, clock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
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

    private static string[] ReadStringArray(JsonElement element)
        => element.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "frontend"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found for site content controller tests.");
    }
}
