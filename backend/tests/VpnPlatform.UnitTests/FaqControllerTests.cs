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

public class FaqControllerTests
{
    [Fact]
    public async Task AdminFaq_Mutations_Should_Use_Injected_Clock()
    {
        await using var db = CreateDb();
        var initialTime = new DateTimeOffset(2033, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var clock = new MutableClock(initialTime);
        var controller = CreateAdminController(db, clock);
        var request = new FaqEntryUpsertRequest("Clock question", "Clock answer", "Clock", true, false, true, 10);

        var created = AssertOk<FaqEntryDto>(await controller.Create(request, CancellationToken.None));

        Assert.Equal(initialTime, created.CreatedAt);
        Assert.Equal(initialTime, created.UpdatedAt);

        clock.UtcNow = initialTime.AddHours(1);
        var updated = AssertOk<FaqEntryDto>(await controller.Update(
            created.Id,
            request with { Answer = "Updated answer", Revision = created.Revision },
            CancellationToken.None));

        Assert.Equal(initialTime, updated.CreatedAt);
        Assert.Equal(clock.UtcNow, updated.UpdatedAt);
    }

    [Fact]
    public void Faq_Read_Paths_Should_Keep_Production_Limits_Before_Materialization()
    {
        var repositoryRoot = FindRepositoryRoot();
        var publicSource = File.ReadAllText(Path.Combine(repositoryRoot, "backend", "src", "VpnPlatform.Api", "Controllers", "Public", "ContentController.cs"));
        var adminSource = File.ReadAllText(Path.Combine(repositoryRoot, "backend", "src", "VpnPlatform.Api", "Controllers", "Admin", "AdminFaqController.cs"));

        Assert.Contains(".Take(200)", publicSource, StringComparison.Ordinal);
        Assert.Contains("LIMIT 200", adminSource, StringComparison.Ordinal);
        Assert.Contains(".Take(200)", adminSource, StringComparison.Ordinal);
        Assert.Contains("unicode_lower", adminSource, StringComparison.Ordinal);
        Assert.Contains("DISTINCT ON", adminSource, StringComparison.Ordinal);
    }

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

        var response = AssertOk<List<PublicFaqEntryDto>>(await controller.GetFaq(cancellationToken: CancellationToken.None));

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

        var response = AssertOk<List<PublicFaqEntryDto>>(await controller.GetFaq(home: true, cancellationToken: CancellationToken.None));

        Assert.Single(response);
        Assert.Equal("Главная", response[0].Question);
    }

    [Fact]
    public async Task PublicFaq_Should_Return_Minimal_Contract_And_Limit_Result_Set()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(Enumerable.Range(1, 205).Select(index =>
            Entry($"Вопрос {index:D3}", "Ответ", "Общее", sortOrder: index)));
        await db.SaveChangesAsync();
        var controller = new ContentController(db);

        using var response = ToJson(await controller.GetFaq(cancellationToken: CancellationToken.None));
        var items = response.RootElement.EnumerateArray().ToArray();

        Assert.Equal(200, items.Length);
        Assert.Equal("Вопрос 001", items[0].GetProperty("Question").GetString());
        Assert.Equal(new[] { "Answer", "Category", "Question" }, items[0].EnumerateObject().Select(x => x.Name).Order().ToArray());
    }

    [Fact]
    public async Task AdminFaq_Should_Create_Update_And_Delete_Item()
    {
        await using var db = CreateDb();
        var controller = CreateAdminController(db);
        var createRequest = new FaqEntryUpsertRequest("Как оплатить?", "Выберите тариф и способ оплаты.", "Оплата", true, true, true, 10);

        var created = AssertOk<FaqEntryDto>(await controller.Create(createRequest, CancellationToken.None));
        var list = AssertOk<List<FaqEntryDto>>(await controller.Get(cancellationToken: CancellationToken.None));
        var updateRequest = createRequest with { Answer = "Оплата доступна на странице тарифов.", SortOrder = 5, Revision = created.Revision };
        var updated = AssertOk<FaqEntryDto>(await controller.Update(created.Id, updateRequest, CancellationToken.None));
        var deleted = await controller.Delete(created.Id, updated.Revision, CancellationToken.None);

        Assert.Contains(list, x => x.Question == "Как оплатить?");
        Assert.Equal("Оплата доступна на странице тарифов.", updated.Answer);
        Assert.Equal(5, updated.SortOrder);
        Assert.IsType<OkObjectResult>(deleted);
        Assert.Empty(await db.FaqEntries.ToListAsync());
        var audits = await db.AuditLogs.ToListAsync();
        Assert.Equal(3, audits.Count);
        Assert.Contains(audits, x => x.Action == "faq.create" && x.EntityId == created.Id.ToString() && x.BeforeJson == "{}");
        Assert.Contains(audits, x => x.Action == "faq.update" && x.EntityId == created.Id.ToString() && x.BeforeJson != x.AfterJson);
        Assert.Contains(audits, x => x.Action == "faq.delete" && x.EntityId == created.Id.ToString() && x.AfterJson == "{}");
        Assert.All(audits, x => Assert.NotEqual("unknown", x.ActorId));
    }

    [Fact]
    public async Task AdminFaq_Should_Limit_List_And_Require_Revision_For_Mutations()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(Enumerable.Range(1, 205).Select(index =>
            Entry($"Вопрос {index:D3}", "Ответ", "Общее", sortOrder: index)));
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        var list = AssertOk<List<FaqEntryDto>>(await controller.Get(cancellationToken: CancellationToken.None));
        var first = list[0];
        var update = await controller.Update(
            first.Id,
            new FaqEntryUpsertRequest(first.Question, "Новый ответ", first.Category, true, true, true, first.SortOrder),
            CancellationToken.None);
        var delete = await controller.Delete(first.Id, revision: null, CancellationToken.None);

        Assert.Equal(200, list.Count);
        Assert.IsType<BadRequestObjectResult>(update);
        Assert.IsType<BadRequestObjectResult>(delete);
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
    public async Task AdminFaq_Should_Normalize_Category_Overview_Case_Insensitively()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(
            Entry("Первый", "Ответ", " Оплата "),
            Entry("Второй", "Ответ", "оплата"),
            Entry("Третий", "Ответ", "Подключение"));
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        using var overview = ToJson(await controller.GetOverview(CancellationToken.None));
        var categories = ReadStringArray(overview.RootElement.GetProperty("Categories"));

        Assert.Equal(2, overview.RootElement.GetProperty("CategoryCount").GetInt32());
        Assert.Contains(categories, category => string.Equals(category, "Оплата", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(categories, category => string.Equals(category, "Подключение", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AdminFaq_Should_Keep_Overview_Counts_Complete_And_Limit_Diagnostics()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(Enumerable.Range(1, 205).SelectMany(index => new[]
        {
            Entry($"Повтор {index:D3}", "Первый", $"Категория {index:D3}", sortOrder: index),
            Entry($" повтор {index:D3} ", "Второй", $" категория {index:D3} ", sortOrder: index + 300)
        }));
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        using var overview = ToJson(await controller.GetOverview(CancellationToken.None));
        var root = overview.RootElement;

        Assert.Equal(410, root.GetProperty("TotalCount").GetInt32());
        Assert.Equal(410, root.GetProperty("ActiveCount").GetInt32());
        Assert.InRange(root.GetProperty("Categories").GetArrayLength(), 1, 200);
        Assert.InRange(root.GetProperty("DuplicateQuestions").GetArrayLength(), 1, 200);
    }

    [Fact]
    public async Task AdminFaq_Should_Filter_Cyrillic_Search_In_Sqlite()
    {
        await using var db = CreateDb();
        db.FaqEntries.AddRange(
            Entry("Как получить QR?", "Откройте личный кабинет.", "Подключение"),
            Entry("Как оплатить?", "Банковской картой.", "Оплата"));
        await db.SaveChangesAsync();
        var controller = CreateAdminController(db);

        var response = AssertOk<List<FaqEntryDto>>(await controller.Get(search: "ПОДКЛЮЧ", cancellationToken: CancellationToken.None));

        Assert.Equal("Как получить QR?", Assert.Single(response).Question);
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
        var duplicateUpdate = await controller.Update(created.Id, new FaqEntryUpsertRequest("Как оплатить?", "Ответ.", "Оплата", true, true, true, 40, created.Revision), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(duplicateCreate);
        Assert.IsType<BadRequestObjectResult>(duplicateUpdate);
        var persisted = await db.FaqEntries.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(created.Question, persisted.Question);
        Assert.Equal(created.Category, persisted.Category);
        Assert.Single(await db.AuditLogs.ToListAsync(), x => x.Action == "faq.create");
    }

    [Fact]
    public async Task AdminFaq_Should_Reject_Cross_Context_Concurrent_Update()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-faq-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.FaqEntries.Add(Entry("Исходный вопрос", "Исходный ответ", "Общее"));
                await setup.SaveChangesAsync();
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var first = await firstDb.FaqEntries.SingleAsync();
            var second = await secondDb.FaqEntries.SingleAsync();
            var firstController = CreateAdminController(firstDb);
            var secondController = CreateAdminController(secondDb);

            var firstResult = AssertOk<FaqEntryDto>(await firstController.Update(
                first.Id,
                new FaqEntryUpsertRequest("Первое изменение", first.Answer, first.Category, true, true, true, 10, first.Revision),
                CancellationToken.None));
            var secondResult = await secondController.Update(
                second.Id,
                new FaqEntryUpsertRequest("Второе изменение", second.Answer, second.Category, true, true, true, 10, second.Revision),
                CancellationToken.None);

            Assert.Equal(1, firstResult.Revision);
            Assert.IsType<ConflictObjectResult>(secondResult);
            await using var verify = new ApplicationDbContext(options);
            Assert.Equal("Первое изменение", (await verify.FaqEntries.SingleAsync()).Question);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
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

    private static AdminFaqController CreateAdminController(ApplicationDbContext db, IClock? clock = null)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminFaqController(db, clock)
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

        throw new DirectoryNotFoundException("Repository root was not found for FAQ controller tests.");
    }
}
