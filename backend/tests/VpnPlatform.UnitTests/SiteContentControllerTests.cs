using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Api.Controllers.Public;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SiteContentControllerTests
{
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

        var response = AssertOk<List<SiteContentBlockDto>>(await controller.GetHomeContent(CancellationToken.None));

        Assert.Equal(new[] { "home.hero.title", "home.hero.subtitle", "home.seo.title", "home.features.item1", "home.footer.text", "home.errors.checkoutCreate", "home.checkout.afterPaymentText" }, response.Select(x => x.Key).ToArray());
    }

    [Fact]
    public async Task AdminSiteContent_Should_Create_Update_And_Delete_Block()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);
        var createRequest = new SiteContentBlockUpsertRequest("home.hero.title", "Заголовок", "home", "Hero title", "Первый экран", "text", true, 10);

        var created = AssertOk<SiteContentBlockDto>(await controller.Create(createRequest, CancellationToken.None));
        var list = AssertOk<List<SiteContentBlockDto>>(await controller.Get("home", CancellationToken.None));
        var updated = AssertOk<SiteContentBlockDto>(await controller.Update(created.Id, createRequest with { Value = "Новый заголовок", InputType = "textarea" }, CancellationToken.None));
        var deleted = await controller.Delete(created.Id, CancellationToken.None);

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
    public async Task AdminSiteContent_Should_Reject_Empty_Key()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);

        var result = await controller.Create(new SiteContentBlockUpsertRequest("", "value", "home", "Label", "", "text", true, 10), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
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
        var publicBlocks = AssertOk<List<SiteContentBlockDto>>(await publicController.GetHomeContent(CancellationToken.None));

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
            new SiteContentBlockUpsertRequest("home.hero.title", "Описание", "home", "Hero subtitle", "", "textarea", true, 40),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(duplicateCreate);
        Assert.IsType<BadRequestObjectResult>(duplicateUpdate);
        var persisted = await db.SiteContentBlocks.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(created.Key, persisted.Key);
        Assert.Equal(created.Value, persisted.Value);
        Assert.Single(await db.AuditLogs.ToListAsync(), x => x.Action == "site_content.create");
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

    private static AdminSiteContentController CreateAdminController(ApplicationDbContext db)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminSiteContentController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
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
}
