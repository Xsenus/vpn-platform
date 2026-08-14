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
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TariffManagementTests
{
    [Fact]
    public async Task PublicCatalog_Should_Use_Injected_Clock_For_Visibility_Window()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2034, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var visible = Tariff("clock-visible");
        visible.VisibleFrom = now.AddMinutes(-1);
        visible.VisibleTo = now.AddMinutes(1);
        var upcoming = Tariff("clock-upcoming");
        upcoming.VisibleFrom = now.AddMinutes(1);
        db.Tariffs.AddRange(visible, upcoming);
        await db.SaveChangesAsync();

        var tariffs = await Catalog(db, now).GetPublicTariffsAsync(CancellationToken.None);

        Assert.Equal("clock-visible", Assert.Single(tariffs).Slug);
    }

    [Fact]
    public async Task PublicCatalog_Should_Return_Extended_Active_Tariff_Content()
    {
        await using var db = CreateDb();
        db.Tariffs.AddRange(
            Tariff("hidden", isActive: false, sortOrder: 1),
            Tariff("premium", isActive: true, sortOrder: 2));
        await db.SaveChangesAsync();

        var tariffs = await Catalog(db).GetPublicTariffsAsync(CancellationToken.None);

        var tariff = Assert.Single(tariffs);
        Assert.Equal("premium", tariff.Slug);
        Assert.Equal("Популярный", tariff.Badge);
        Assert.Contains("Автоматическая выдача", tariff.Features);
        Assert.Contains("После оплаты", tariff.AfterPaymentText);

        using var publicJson = JsonDocument.Parse(JsonSerializer.Serialize(tariff, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var publicFields = publicJson.RootElement.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("featuresJson", publicFields);
        Assert.DoesNotContain("isActive", publicFields);
        Assert.DoesNotContain("sortOrder", publicFields);
        Assert.DoesNotContain("visibleFrom", publicFields);
        Assert.DoesNotContain("visibleTo", publicFields);
        Assert.DoesNotContain("tariffType", publicFields);
        Assert.DoesNotContain("allowedRegionsCsv", publicFields);
        Assert.DoesNotContain("allowedNodeGroupsCsv", publicFields);
        Assert.DoesNotContain("isReferralEligible", publicFields);
        Assert.DoesNotContain("provisioningScenario", publicFields);
        Assert.DoesNotContain("createdAt", publicFields);
        Assert.DoesNotContain("updatedAt", publicFields);
    }

    [Fact]
    public async Task PublicTariffsController_Should_Return_Only_Visible_Active_Tariffs_On_Sqlite()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var future = Tariff("future", isActive: true, sortOrder: 2);
        future.VisibleFrom = now.AddDays(1);
        var expired = Tariff("expired", isActive: true, sortOrder: 3);
        expired.VisibleTo = now.AddDays(-1);
        db.Tariffs.AddRange(
            Tariff("inactive", isActive: false, sortOrder: 1),
            future,
            expired,
            Tariff("standard", isActive: true, sortOrder: 20),
            Tariff("start", isActive: true, sortOrder: 10));
        await db.SaveChangesAsync();
        var controller = new TariffsController(Catalog(db));

        var response = AssertOk<List<PublicTariffDto>>(await controller.Get(CancellationToken.None));

        Assert.Equal(new[] { "start", "standard" }, response.Select(x => x.Slug).ToArray());
        Assert.All(response, x =>
        {
            Assert.NotEmpty(x.Features);
            Assert.Equal("RUB", x.Currency);
        });
    }

    [Fact]
    public async Task Tariff_Lists_Should_Be_Deterministic_And_Limited()
    {
        await using var db = CreateDb();
        db.Tariffs.AddRange(Enumerable.Range(0, 205).Select(index => Tariff($"limited-{index:D3}", sortOrder: 10)));
        await db.SaveChangesAsync();

        var publicTariffs = await Catalog(db).GetPublicTariffsAsync(CancellationToken.None);
        var adminResult = Assert.IsType<OkObjectResult>(await CreateController(db).GetTariffs(CancellationToken.None));
        using var adminJson = JsonDocument.Parse(JsonSerializer.Serialize(adminResult.Value));

        Assert.Equal(200, publicTariffs.Count);
        Assert.Equal(200, adminJson.RootElement.GetArrayLength());
        Assert.Equal(
            publicTariffs.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            publicTariffs.Select(x => x.Name).ToArray());
    }

    [Fact]
    public async Task AdminTariffs_Should_Create_And_Update_Extended_Content()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var created = AssertOk<TariffDto>(await controller.CreateTariff(TariffRequest("admin-monthly"), CancellationToken.None));
        using var patch = JsonDocument.Parse("""
        {
          "revision": 0,
          "price": 790,
          "badge": "Выгодно",
          "featuresJson": "[\"5 устройств\",\"Приоритетные серверы\"]",
          "afterPaymentText": "После оплаты доступ появится в кабинете.",
          "provisioningScenario": "premium-auto"
        }
        """);

        var updated = AssertOk<TariffDto>(await controller.PatchTariff(created.Id, patch.RootElement, CancellationToken.None));

        Assert.Equal(790m, updated.Price);
        Assert.Equal("Выгодно", updated.Badge);
        Assert.Equal(new[] { "5 устройств", "Приоритетные серверы" }, updated.Features);
        Assert.Equal("premium-auto", updated.ProvisioningScenario);
        Assert.Contains("После оплаты", updated.AfterPaymentText);
    }

    [Fact]
    public async Task AdminTariffs_Should_Not_Accept_Client_Owned_Identity_Or_Audit_Fields()
    {
        await using var db = CreateDb();
        var submittedId = Guid.NewGuid();
        var request = JsonSerializer.Deserialize<TariffCreateRequest>($$"""
        {
          "id": "{{submittedId}}",
          "revision": 99,
          "createdAt": "2000-01-01T00:00:00Z",
          "updatedAt": "2000-01-01T00:00:00Z",
          "name": "Safe create",
          "slug": "safe-create",
          "durationDays": 30,
          "price": 490,
          "currency": "RUB",
          "maxDevices": 3
        }
        """, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var created = AssertOk<TariffDto>(await CreateController(db).CreateTariff(request!, CancellationToken.None));

        Assert.NotEqual(submittedId, created.Id);
        Assert.Equal(0, created.Revision);
        Assert.NotEqual(DateTimeOffset.Parse("2000-01-01T00:00:00Z"), created.CreatedAt);

        var invalidTypeRequest = TariffRequest("invalid-enum") with { TariffType = (TariffType)999 };
        Assert.IsType<BadRequestObjectResult>(await CreateController(db).CreateTariff(invalidTypeRequest, CancellationToken.None));
    }

    [Fact]
    public async Task AdminTariffs_Should_Archive_Linked_Tariff_Instead_Of_Delete()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var tariff = Tariff("linked");
        db.Users.Add(new User { Id = userId, Email = "user@test.local", DisplayName = "User", PasswordHash = "hash" });
        db.Tariffs.Add(tariff);
        db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Amount = tariff.Price,
            Currency = tariff.Currency,
            Status = OrderStatus.PendingPayment,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.DeleteTariff(tariff.Id, tariff.Revision, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var archived = await db.Tariffs.SingleAsync(x => x.Id == tariff.Id);
        Assert.False(archived.IsActive);
        Assert.NotNull(archived.VisibleTo);
        Assert.DoesNotContain((await Catalog(db).GetPublicTariffsAsync(CancellationToken.None)), x => x.Id == tariff.Id);
    }

    [Fact]
    public async Task AdminOrders_Should_Load_On_Sqlite_And_Sort_In_Memory()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var tariff = Tariff("orders");
        var olderOrderId = Guid.NewGuid();
        var newerOrderId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "orders@test.local", DisplayName = "Orders User", PasswordHash = "hash" });
        db.Tariffs.Add(tariff);
        db.Orders.AddRange(
            new Order
            {
                Id = olderOrderId,
                UserId = userId,
                TariffId = tariff.Id,
                Amount = tariff.Price,
                Currency = tariff.Currency,
                Status = OrderStatus.PendingPayment,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
            new Order
            {
                Id = newerOrderId,
                UserId = userId,
                TariffId = tariff.Id,
                Amount = tariff.Price,
                Currency = tariff.Currency,
                Status = OrderStatus.Completed,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
                CreatedAt = DateTimeOffset.UtcNow
            });
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetOrders(null, null, CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var first = json.RootElement.EnumerateArray().First();

        Assert.Equal(newerOrderId, first.GetProperty("Id").GetGuid());
    }

    [Fact]
    public async Task AdminTariffs_Should_Delete_Unused_Tariff()
    {
        await using var db = CreateDb();
        var tariff = Tariff("unused");
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.DeleteTariff(tariff.Id, tariff.Revision, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await db.Tariffs.AnyAsync(x => x.Id == tariff.Id));
    }

    [Fact]
    public async Task AdminTariffs_Should_Keep_Tariff_When_Delete_Revision_Is_Stale()
    {
        await using var db = CreateDb();
        var tariff = Tariff("stale-delete");
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();

        var result = await CreateController(db).DeleteTariff(tariff.Id, tariff.Revision + 1, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.True(await db.Tariffs.AnyAsync(x => x.Id == tariff.Id));
    }

    [Theory]
    [InlineData(-1, "RUB", "Tariff price must be non-negative")]
    [InlineData(490, "RUBLE", "Tariff currency must use a three-letter code")]
    [InlineData(490, "12R", "Tariff currency must use a three-letter code")]
    public async Task AdminTariffs_Should_Reject_Invalid_Price_And_Currency(decimal price, string currency, string expectedError)
    {
        await using var db = CreateDb();
        var tariff = Tariff("invalid-money");
        tariff.Price = price;
        tariff.Currency = currency;
        var controller = CreateController(db);

        var result = await controller.CreateTariff(TariffRequest(tariff), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(expectedError, JsonSerializer.Serialize(badRequest.Value));
        Assert.False(await db.Tariffs.AnyAsync());
    }

    [Fact]
    public async Task AdminTariffs_Should_Reject_Duplicate_Slug_On_Create_And_Update()
    {
        await using var db = CreateDb();
        db.Tariffs.AddRange(Tariff("basic"), Tariff("premium"));
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var duplicateCreate = await controller.CreateTariff(TariffRequest("basic"), CancellationToken.None);
        using var patch = JsonDocument.Parse("""{"revision":0,"slug":"basic"}""");
        var premium = await db.Tariffs.SingleAsync(x => x.Slug == "premium");
        var duplicateUpdate = await controller.PatchTariff(premium.Id, patch.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(duplicateCreate);
        Assert.IsType<BadRequestObjectResult>(duplicateUpdate);
        await db.Entry(premium).ReloadAsync();
        Assert.Equal("premium", premium.Slug);
    }

    [Theory]
    [InlineData("{\"price\":\"490\"}")]
    [InlineData("{\"durationDays\":30.5}")]
    [InlineData("{\"isActive\":\"true\"}")]
    [InlineData("{\"visibleFrom\":\"not-a-date\"}")]
    [InlineData("{\"revision\":0,\"tariffType\":\"999\"}")]
    [InlineData("[]")]
    public async Task AdminTariff_Patch_Should_Reject_Invalid_Types_Without_Mutating_Tracked_Entity(string rawPayload)
    {
        await using var db = CreateDb();
        var tariff = Tariff("safe-patch");
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        using var patch = JsonDocument.Parse(rawPayload);

        var result = await CreateController(db).PatchTariff(tariff.Id, patch.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(490m, tariff.Price);
        Assert.Equal(30, tariff.DurationDays);
        Assert.True(tariff.IsActive);
        Assert.Null(tariff.VisibleFrom);
    }

    [Theory]
    [InlineData("{\"revision\":0,\"price\":-1}")]
    [InlineData("{\"revision\":0,\"currency\":\"RUBLE\"}")]
    [InlineData("{\"revision\":0,\"visibleFrom\":\"2026-08-05T00:00:00Z\",\"visibleTo\":\"2026-08-04T00:00:00Z\"}")]
    public async Task AdminTariff_Patch_Should_Reject_Invalid_Business_Values_Without_Mutating_Tracked_Entity(string rawPayload)
    {
        await using var db = CreateDb();
        var tariff = Tariff("safe-business-patch");
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        using var patch = JsonDocument.Parse(rawPayload);

        var result = await CreateController(db).PatchTariff(tariff.Id, patch.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(490m, tariff.Price);
        Assert.Equal("RUB", tariff.Currency);
        Assert.Null(tariff.VisibleFrom);
        Assert.Null(tariff.VisibleTo);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"revision\":0}")]
    [InlineData("{\"revision\":0,\"unknown\":true}")]
    [InlineData("{\"revision\":0,\"name\":\"First\",\"name\":\"Second\"}")]
    public async Task AdminTariff_Patch_Should_Reject_Empty_Unknown_And_Duplicate_Fields(string rawPayload)
    {
        await using var db = CreateDb();
        var tariff = Tariff("strict-patch");
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        using var patch = JsonDocument.Parse(rawPayload);

        var result = await CreateController(db).PatchTariff(tariff.Id, patch.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Тариф strict-patch", tariff.Name);
    }

    [Fact]
    public async Task AdminTariff_Patch_Should_Reject_Stale_Revision()
    {
        await using var db = CreateDb();
        var tariff = Tariff("stale-patch");
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        using var patch = JsonDocument.Parse("""{"revision":1,"price":990}""");

        var result = await CreateController(db).PatchTariff(tariff.Id, patch.RootElement, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(490m, tariff.Price);
    }

    [Theory]
    [InlineData("badge", 81)]
    [InlineData("provisioningScenario", 121)]
    [InlineData("name", 4001)]
    public async Task AdminTariff_Patch_Should_Reject_Overlong_Fields(string field, int length)
    {
        await using var db = CreateDb();
        var tariff = Tariff("bounded-patch");
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        using var patch = JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["revision"] = 0,
            [field] = new string('x', length)
        }));

        var result = await CreateController(db).PatchTariff(tariff.Id, patch.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AdminTariff_Patch_Should_Reject_Invalid_Features_Json_Without_Data_Loss()
    {
        await using var db = CreateDb();
        var tariff = Tariff("invalid-features");
        var originalFeatures = tariff.FeaturesJson;
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        using var patch = JsonDocument.Parse("""{"revision":0,"featuresJson":"{not-json}"}""");

        var result = await CreateController(db).PatchTariff(tariff.Id, patch.RootElement, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(originalFeatures, tariff.FeaturesJson);
        Assert.Equal(0, tariff.Revision);

        using var nullFeaturePatch = JsonDocument.Parse("""{"revision":0,"featuresJson":"[null]"}""");
        Assert.IsType<BadRequestObjectResult>(await CreateController(db).PatchTariff(tariff.Id, nullFeaturePatch.RootElement, CancellationToken.None));
        Assert.Equal(originalFeatures, tariff.FeaturesJson);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdminTariffs_Should_Reject_Cross_Context_Concurrent_Mutation(bool deleteSecond)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-tariff-concurrency-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.Tariffs.Add(Tariff("concurrent"));
                await setup.SaveChangesAsync();
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var first = await firstDb.Tariffs.SingleAsync();
            var second = await secondDb.Tariffs.SingleAsync();
            using var firstPatch = JsonDocument.Parse("""{"revision":0,"name":"Первое изменение"}""");
            using var secondPatch = JsonDocument.Parse("""{"revision":0,"name":"Второе изменение"}""");

            var firstResult = await CreateController(firstDb).PatchTariff(first.Id, firstPatch.RootElement, CancellationToken.None);
            var secondResult = deleteSecond
                ? await CreateController(secondDb).DeleteTariff(second.Id, second.Revision, CancellationToken.None)
                : await CreateController(secondDb).PatchTariff(second.Id, secondPatch.RootElement, CancellationToken.None);

            Assert.IsType<OkObjectResult>(firstResult);
            Assert.IsType<ConflictObjectResult>(secondResult);
            await using var verify = new ApplicationDbContext(options);
            var saved = await verify.Tariffs.SingleAsync();
            Assert.Equal("Первое изменение", saved.Name);
            Assert.Equal(1, saved.Revision);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static Tariff Tariff(string slug, bool isActive = true, int sortOrder = 10)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"Тариф {slug}",
            Slug = slug,
            Description = "Короткое описание",
            FullDescription = "Полное описание тарифа для публичной страницы.",
            FeaturesJson = "[\"Автоматическая выдача\",\"QR-код\",\"Личный кабинет\"]",
            Badge = "Популярный",
            DurationDays = 30,
            Price = 490m,
            Currency = "RUB",
            MaxDevices = 3,
            IsActive = isActive,
            SortOrder = sortOrder,
            Category = "standard",
            ProvisioningScenario = "auto",
            AfterPaymentText = "После оплаты доступ появится в личном кабинете."
        };

    private static TariffCreateRequest TariffRequest(string slug)
        => TariffRequest(Tariff(slug));

    private static TariffCreateRequest TariffRequest(Tariff tariff)
        => new(
            tariff.Name,
            tariff.Slug,
            tariff.Description,
            tariff.FullDescription,
            tariff.FeaturesJson,
            tariff.Badge,
            tariff.DurationDays,
            tariff.Price,
            tariff.Currency,
            tariff.MaxDevices,
            tariff.TrafficLimit,
            tariff.IsTrial,
            tariff.IsActive,
            tariff.SortOrder,
            tariff.VisibleFrom,
            tariff.VisibleTo,
            tariff.TariffType,
            tariff.Category,
            tariff.AllowedRegionsCsv,
            tariff.AllowedNodeGroupsCsv,
            tariff.IsReferralEligible,
            tariff.ProvisioningScenario,
            tariff.AfterPaymentText);

    private static AdminOperationsController CreateController(ApplicationDbContext db)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminOperationsController(db, null!, null!, null!)
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

    private static CatalogService Catalog(ApplicationDbContext db, DateTimeOffset? now = null)
        => new(db, new FixedClock(now ?? DateTimeOffset.UtcNow));

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private static T AssertOk<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<T>(ok.Value);
    }
}
