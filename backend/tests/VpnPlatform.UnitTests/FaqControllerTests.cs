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

public class FaqControllerTests
{
    [Fact]
    public async Task PublicFaq_Should_Return_Only_Active_Published_Items_In_Sort_Order()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(
            Entry("Второй", "Ответ", "Оплата", sortOrder: 20),
            Entry("Первый", "Ответ", "Подключение", sortOrder: 10),
            Entry("Скрытый", "Ответ", "Скрыто", isActive: false, sortOrder: 1),
            Entry("Не на FAQ", "Ответ", "Скрыто", showOnFaqPage: false, sortOrder: 2));
        await db.SaveChangesAsync();
        var controller = new ContentController(db);

        var response = AssertOk<List<FaqEntryDto>>(await controller.GetFaq(cancellationToken: CancellationToken.None));

        Assert.Equal(new[] { "Первый", "Второй" }, response.Select(x => x.Question).ToArray());
    }

    [Fact]
    public async Task PublicFaq_Should_Filter_Home_Items_When_Requested()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(
            Entry("Главная", "Ответ", "Общее", showOnHome: true, sortOrder: 10),
            Entry("Только FAQ", "Ответ", "Общее", showOnHome: false, sortOrder: 20));
        await db.SaveChangesAsync();
        var controller = new ContentController(db);

        var response = AssertOk<List<FaqEntryDto>>(await controller.GetFaq(home: true, cancellationToken: CancellationToken.None));

        Assert.Single(response);
        Assert.Equal("Главная", response[0].Question);
    }

    [Fact]
    public async Task AdminFaq_Should_Create_Update_And_Delete_Item()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);
        var createRequest = new FaqEntryUpsertRequest("Как оплатить?", "Выберите тариф и способ оплаты.", "Оплата", true, true, true, 10);

        var created = AssertOk<FaqEntryDto>(await controller.Create(createRequest, CancellationToken.None));
        var list = AssertOk<List<FaqEntryDto>>(await controller.Get(cancellationToken: CancellationToken.None));
        var updateRequest = createRequest with { Answer = "Оплата доступна на странице тарифов.", SortOrder = 5 };
        var updated = AssertOk<FaqEntryDto>(await controller.Update(created.Id, updateRequest, CancellationToken.None));
        var deleted = await controller.Delete(created.Id, CancellationToken.None);

        Assert.Contains(list, x => x.Question == "Как оплатить?");
        Assert.Equal("Оплата доступна на странице тарифов.", updated.Answer);
        Assert.Equal(5, updated.SortOrder);
        Assert.IsType<OkObjectResult>(deleted);
        Assert.Empty(await db.FaqEntries.ToListAsync());
    }

    [Fact]
    public async Task AdminFaq_Should_Reject_Empty_Question_Or_Answer()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);

        var result = await controller.Create(new FaqEntryUpsertRequest("", "", "Общее", true, true, true, 10), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AdminFaq_Should_Filter_By_Category_Visibility_And_Search()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(
            Entry("Как оплатить?", "Картой или через СБП.", "Оплата", showOnHome: true, sortOrder: 20),
            Entry("Как получить QR?", "Откройте личный кабинет.", "Подключение", showOnHome: false, sortOrder: 10),
            Entry("Скрытый платеж", "Черновик ответа.", "Оплата", isActive: false, sortOrder: 30));
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        var paymentItems = AssertOk<List<FaqEntryDto>>(await controller.Get(category: "Оплата", cancellationToken: CancellationToken.None));
        var homeItems = AssertOk<List<FaqEntryDto>>(await controller.Get(visibility: "home", cancellationToken: CancellationToken.None));
        var hiddenItems = AssertOk<List<FaqEntryDto>>(await controller.Get(visibility: "hidden", cancellationToken: CancellationToken.None));
        var searchItems = AssertOk<List<FaqEntryDto>>(await controller.Get(search: "qr", cancellationToken: CancellationToken.None));

        Assert.Equal(new[] { "Как оплатить?", "Скрытый платеж" }, paymentItems.Select(x => x.Question).ToArray());
        Assert.Equal("Как оплатить?", Assert.Single(homeItems).Question);
        Assert.Equal("Скрытый платеж", Assert.Single(hiddenItems).Question);
        Assert.Equal("Как получить QR?", Assert.Single(searchItems).Question);
    }

    [Fact]
    public async Task AdminFaq_Should_Report_Overview()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(
            Entry("Как оплатить?", "Картой.", "Оплата", showOnHome: true, sortOrder: 20),
            Entry("Как подключиться?", "Через кабинет.", "Подключение", showOnHome: false, sortOrder: 10),
            Entry("Черновик", "Ответ.", "Общее", isActive: false, sortOrder: 30));
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        using var overview = ToJson(await controller.GetOverview(CancellationToken.None));
        var root = overview.RootElement;

        Assert.Equal(3, root.GetProperty("TotalCount").GetInt32());
        Assert.Equal(2, root.GetProperty("ActiveCount").GetInt32());
        Assert.Equal(1, root.GetProperty("HiddenCount").GetInt32());
        Assert.Equal(1, root.GetProperty("HomeCount").GetInt32());
        Assert.Equal(2, root.GetProperty("FaqPageCount").GetInt32());
        Assert.True(root.GetProperty("HasPublicFaq").GetBoolean());
        Assert.True(root.GetProperty("HasHomeFaq").GetBoolean());
        Assert.Contains("Оплата", ReadStringArray(root.GetProperty("Categories")));
    }

    [Fact]
    public async Task AdminFaq_Should_Reject_Duplicate_Question_In_Category()
    {
        await using var db = CreateDb();
        db.FaqEntries.Add(Entry("Как оплатить?", "Ответ.", "Оплата"));
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        var duplicateCreate = await controller.Create(new FaqEntryUpsertRequest(" как оплатить? ", "Другой ответ.", " оплата ", true, true, true, 20), CancellationToken.None);
        var created = AssertOk<FaqEntryDto>(await controller.Create(new FaqEntryUpsertRequest("Как подключиться?", "Ответ.", "Подключение", true, true, true, 30), CancellationToken.None));
        var duplicateUpdate = await controller.Update(created.Id, new FaqEntryUpsertRequest("Как оплатить?", "Ответ.", "Оплата", true, true, true, 40), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(duplicateCreate);
        Assert.IsType<BadRequestObjectResult>(duplicateUpdate);
    }

    private static FaqEntry Entry(
        string question,
        string answer,
        string category,
        bool isActive = true,
        bool showOnHome = true,
        bool showOnFaqPage = true,
        int sortOrder = 100)
        => new()
        {
            Question = question,
            Answer = answer,
            Category = category,
            IsActive = isActive,
            ShowOnHome = showOnHome,
            ShowOnFaqPage = showOnFaqPage,
            SortOrder = sortOrder
        };

    private static AdminFaqController CreateAdminController(ApplicationDbContext db)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminFaqController(db)
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
