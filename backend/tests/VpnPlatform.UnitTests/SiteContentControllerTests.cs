using System.Security.Claims;
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
            Block("hidden", "Скрыто", isActive: false),
            Block("footer.text", "Футер", group: "footer"));
        await db.SaveChangesAsync();
        var controller = new ContentController(db);

        var response = AssertOk<List<SiteContentBlockDto>>(await controller.GetHomeContent(CancellationToken.None));

        Assert.Equal(new[] { "home.hero.title", "home.hero.subtitle", "home.seo.title", "home.features.item1", "home.footer.text" }, response.Select(x => x.Key).ToArray());
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
    }

    [Fact]
    public async Task AdminSiteContent_Should_Reject_Empty_Key()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);

        var result = await controller.Create(new SiteContentBlockUpsertRequest("", "value", "home", "Label", "", "text", true, 10), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
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
}
