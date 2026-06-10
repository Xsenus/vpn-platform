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
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TariffManagementTests
{
    [Fact]
    public async Task PublicCatalog_Should_Return_Extended_Active_Tariff_Content()
    {
        await using var db = CreateDb();
        db.Tariffs.AddRange(
            Tariff("hidden", isActive: false, sortOrder: 1),
            Tariff("premium", isActive: true, sortOrder: 2));
        await db.SaveChangesAsync();

        var tariffs = await new CatalogService(db).GetPublicTariffsAsync(CancellationToken.None);

        var tariff = Assert.Single(tariffs);
        Assert.Equal("premium", tariff.Slug);
        Assert.Equal("Популярный", tariff.Badge);
        Assert.Contains("Автоматическая выдача", tariff.Features);
        Assert.Equal("auto", tariff.ProvisioningScenario);
        Assert.Contains("После оплаты", tariff.AfterPaymentText);
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
        var controller = new TariffsController(new CatalogService(db));

        var response = AssertOk<List<TariffDto>>(await controller.Get(CancellationToken.None));

        Assert.Equal(new[] { "start", "standard" }, response.Select(x => x.Slug).ToArray());
        Assert.All(response, x =>
        {
            Assert.True(x.IsActive);
            Assert.NotEmpty(x.Features);
            Assert.Equal("RUB", x.Currency);
        });
    }

    [Fact]
    public async Task AdminTariffs_Should_Create_And_Update_Extended_Content()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var created = AssertOk<TariffDto>(await controller.CreateTariff(Tariff("admin-monthly"), CancellationToken.None));
        using var patch = JsonDocument.Parse("""
        {
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
    public async Task AdminTariffs_Should_Reject_Delete_When_Linked_Order_Exists()
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

        var result = await controller.DeleteTariff(tariff.Id, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(await db.Tariffs.AnyAsync(x => x.Id == tariff.Id));
    }

    [Fact]
    public async Task AdminTariffs_Should_Delete_Unused_Tariff()
    {
        await using var db = CreateDb();
        var tariff = Tariff("unused");
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.DeleteTariff(tariff.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await db.Tariffs.AnyAsync(x => x.Id == tariff.Id));
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

    private static T AssertOk<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<T>(ok.Value);
    }
}
