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
        var list = AssertOk<List<FaqEntryDto>>(await controller.Get(CancellationToken.None));
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
}
