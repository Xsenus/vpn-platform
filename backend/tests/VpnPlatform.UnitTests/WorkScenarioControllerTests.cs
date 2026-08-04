using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class WorkScenarioControllerTests
{
    [Fact]
    public async Task AdminWorkScenarios_Should_Create_Update_And_Delete_Scenario()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);
        var request = Request("auto-premium");

        var created = AssertOk<WorkScenarioDto>(await controller.Create(request, CancellationToken.None));
        var list = AssertOk<List<WorkScenarioDto>>(await controller.Get(CancellationToken.None));
        var updated = AssertOk<WorkScenarioDto>(await controller.Update(created.Id, request with { Name = "Premium auto", MaxDevices = 5, GenerateQrCode = false }, CancellationToken.None));
        var deleted = await controller.Delete(created.Id, CancellationToken.None);

        Assert.Contains(list, x => x.Key == "auto-premium");
        Assert.Equal("Premium auto", updated.Name);
        Assert.Equal(5, updated.MaxDevices);
        Assert.False(updated.GenerateQrCode);
        Assert.IsType<OkObjectResult>(deleted);
        Assert.Empty(await db.WorkScenarios.ToListAsync());
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Reject_Duplicate_Key_On_Create_And_Update()
    {
        await using var db = CreateDb();
        db.WorkScenarios.AddRange(Scenario("auto"), Scenario("premium-auto"));
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var duplicateCreate = await controller.Create(Request("auto"), CancellationToken.None);
        var premium = await db.WorkScenarios.SingleAsync(x => x.Key == "premium-auto");
        var duplicateUpdate = await controller.Update(premium.Id, Request("auto") with { Name = "Duplicate" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(duplicateCreate);
        Assert.IsType<BadRequestObjectResult>(duplicateUpdate);
        await db.Entry(premium).ReloadAsync();
        Assert.Equal("premium-auto", premium.Key);
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Reject_Delete_When_Tariff_Uses_Scenario()
    {
        await using var db = CreateDb();
        var scenario = Scenario("auto");
        db.WorkScenarios.Add(scenario);
        db.Tariffs.Add(new Tariff
        {
            Id = Guid.NewGuid(),
            Name = "Monthly",
            Slug = "monthly",
            DurationDays = 30,
            Price = 490m,
            Currency = "RUB",
            MaxDevices = 3,
            ProvisioningScenario = "auto"
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.Delete(scenario.Id, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(await db.WorkScenarios.AnyAsync(x => x.Id == scenario.Id));
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Reject_Linked_Key_Rename_Without_Mutating_Scenario()
    {
        await using var db = CreateDb();
        var scenario = Scenario("auto");
        db.WorkScenarios.Add(scenario);
        db.Tariffs.Add(new Tariff
        {
            Id = Guid.NewGuid(),
            Name = "Monthly",
            Slug = "monthly-linked-rename",
            DurationDays = 30,
            Price = 490m,
            Currency = "RUB",
            MaxDevices = 3,
            ProvisioningScenario = "auto"
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.Update(
            scenario.Id,
            Request("renamed-auto") with { Name = "Mutated name", MaxDevices = 9 },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("auto", scenario.Key);
        Assert.Equal("Автоматическая выдача", scenario.Name);
        Assert.Equal(3, scenario.MaxDevices);
        db.ChangeTracker.Clear();
        var persisted = await db.WorkScenarios.SingleAsync(x => x.Id == scenario.Id);
        Assert.Equal("auto", persisted.Key);
        Assert.Equal("Автоматическая выдача", persisted.Name);
        Assert.Equal(3, persisted.MaxDevices);
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Reject_Invalid_Request()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.Create(Request("") with { Name = "", MaxDevices = 0 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("{\"tariff\":\"bad\"}", "Allowed tariff ids must be a JSON array")]
    [InlineData("[\"not-a-guid\"]", "Allowed tariff ids must contain only tariff GUID strings")]
    [InlineData("[", "Allowed tariff ids must be valid JSON")]
    public async Task AdminWorkScenarios_Should_Reject_Invalid_Allowed_Tariff_Ids(string allowedTariffIdsJson, string expectedError)
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.Create(Request("json-check") with { AllowedTariffIdsJson = allowedTariffIdsJson }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(expectedError, System.Text.Json.JsonSerializer.Serialize(badRequest.Value));
        Assert.Empty(await db.WorkScenarios.ToListAsync());
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Normalize_Allowed_Tariff_Ids_And_Mode()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);
        var tariffId = Guid.NewGuid();

        var created = AssertOk<WorkScenarioDto>(await controller.Create(
            Request("Premium Auto") with
            {
                AllowedTariffIdsJson = $"[\"{tariffId}\",\"{tariffId}\"]",
                ProvisioningMode = " Auto "
            },
            CancellationToken.None));

        Assert.Equal("premium-auto", created.Key);
        Assert.Equal("auto", created.ProvisioningMode);
        Assert.Equal($"[\"{tariffId}\"]", created.AllowedTariffIdsJson);
    }

    private static WorkScenarioUpsertRequest Request(string key)
        => new(
            "Автоматическая выдача",
            key,
            true,
            "[]",
            "vless",
            "least-loaded",
            "default",
            "auto",
            "create_subscription_and_access",
            "keep_order_pending",
            "disable_access",
            "disable_access_after_grace",
            "extend_subscription",
            "Доступ появится в кабинете.",
            "Доступ готов.",
            true,
            3,
            null,
            10);

    private static WorkScenario Scenario(string key)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Автоматическая выдача",
            Key = key,
            IsActive = true,
            MaxDevices = 3
        };

    private static AdminWorkScenariosController CreateController(ApplicationDbContext db)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminWorkScenariosController(db)
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
