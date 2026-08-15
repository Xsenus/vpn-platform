using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class WorkScenarioControllerTests
{
    [Fact]
    public async Task AdminWorkScenario_Mutations_Should_Use_Injected_Clock()
    {
        await using var db = CreateDb();
        var initialTime = new DateTimeOffset(2033, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var clock = new MutableClock(initialTime);
        var controller = CreateController(db, clock);
        var request = Request("clock-scenario");

        var created = AssertOk<WorkScenarioDto>(await controller.Create(request, CancellationToken.None));

        Assert.Equal(initialTime, created.CreatedAt);
        Assert.Equal(initialTime, created.UpdatedAt);

        clock.UtcNow = initialTime.AddHours(1);
        var updated = AssertOk<WorkScenarioDto>(await controller.Update(
            created.Id,
            request with { Name = "Updated clock scenario", Revision = created.Revision },
            CancellationToken.None));

        Assert.Equal(initialTime, updated.CreatedAt);
        Assert.Equal(clock.UtcNow, updated.UpdatedAt);
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Limit_List()
    {
        await using var db = CreateDb();
        db.WorkScenarios.AddRange(Enumerable.Range(0, 205)
            .Select(index =>
            {
                var scenario = Scenario($"scenario-{index:D3}");
                scenario.SortOrder = index;
                return scenario;
            }));
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var list = AssertOk<List<WorkScenarioDto>>(await controller.Get(CancellationToken.None));

        Assert.Equal(200, list.Count);
        Assert.Equal("scenario-000", list[0].Key);
        Assert.Equal("scenario-199", list[^1].Key);
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Create_Update_And_Delete_Scenario()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);
        var request = Request("auto-premium");

        var created = AssertOk<WorkScenarioDto>(await controller.Create(request, CancellationToken.None));
        var list = AssertOk<List<WorkScenarioDto>>(await controller.Get(CancellationToken.None));
        var updated = AssertOk<WorkScenarioDto>(await controller.Update(created.Id, request with { Name = "Premium auto", MaxDevices = 5, GenerateQrCode = false, Revision = created.Revision }, CancellationToken.None));
        var deleted = await controller.Delete(created.Id, updated.Revision, CancellationToken.None);

        Assert.Contains(list, x => x.Key == "auto-premium");
        Assert.Equal(0, created.Revision);
        Assert.Equal("Premium auto", updated.Name);
        Assert.Equal(created.Revision + 1, updated.Revision);
        Assert.Equal(5, updated.MaxDevices);
        Assert.False(updated.GenerateQrCode);
        Assert.IsType<OkObjectResult>(deleted);
        Assert.Empty(await db.WorkScenarios.ToListAsync());
        var audits = await db.AuditLogs.ToListAsync();
        Assert.Equal(3, audits.Count);
        Assert.Contains(audits, x => x.Action == "work_scenario.create" && x.EntityId == created.Id.ToString() && x.BeforeJson == "{}");
        Assert.Contains(audits, x => x.Action == "work_scenario.update" && x.EntityId == created.Id.ToString() && x.BeforeJson != x.AfterJson);
        Assert.Contains(audits, x => x.Action == "work_scenario.delete" && x.EntityId == created.Id.ToString() && x.AfterJson == "{}");
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Reject_NoOp_Update_Without_Revision_Or_Audit_Churn()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);
        var request = Request("auto-premium");
        var created = AssertOk<WorkScenarioDto>(await controller.Create(request, CancellationToken.None));

        var result = await controller.Update(created.Id, request with { Revision = created.Revision }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        var persisted = await db.WorkScenarios.SingleAsync();
        Assert.Equal(created.Revision, persisted.Revision);
        Assert.Single(await db.AuditLogs.ToListAsync(), audit => audit.Action == "work_scenario.create");
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
        var duplicateUpdate = await controller.Update(premium.Id, Request("auto") with { Name = "Duplicate", Revision = premium.Revision }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(duplicateCreate);
        Assert.IsType<BadRequestObjectResult>(duplicateUpdate);
        await db.Entry(premium).ReloadAsync();
        Assert.Equal("premium-auto", premium.Key);
        Assert.Empty(await db.AuditLogs.ToListAsync());
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

        var result = await controller.Delete(scenario.Id, scenario.Revision, CancellationToken.None);

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
            Request("renamed-auto") with { Name = "Mutated name", MaxDevices = 9, Revision = scenario.Revision },
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
    [InlineData("wireguard")]
    [InlineData("vless://attacker.example")]
    public async Task AdminWorkScenarios_Should_Reject_Unsupported_Vpn_Protocol(string protocol)
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.Create(Request("protocol-check") with { VpnProtocol = protocol }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("vless, vmess or trojan", System.Text.Json.JsonSerializer.Serialize(badRequest.Value));
        Assert.Empty(await db.WorkScenarios.ToListAsync());
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

    [Fact]
    public async Task AdminWorkScenarios_Should_Require_Revision_For_Update_And_Delete()
    {
        await using var db = CreateDb();
        var scenario = Scenario("auto");
        db.WorkScenarios.Add(scenario);
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var update = await controller.Update(
            scenario.Id,
            Request(scenario.Key) with { Name = "Новое имя" },
            CancellationToken.None);
        var delete = await controller.Delete(scenario.Id, revision: null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(update);
        Assert.IsType<BadRequestObjectResult>(delete);
        Assert.Equal("Автоматическая выдача", (await db.WorkScenarios.SingleAsync()).Name);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData(201, 10)]
    [InlineData(10, 121)]
    public async Task AdminWorkScenarios_Should_Reject_Fields_That_Exceed_Database_Limits(int nameLength, int keyLength)
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.Create(
            Request(new string('a', keyLength)) with { Name = new string('n', nameLength) },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.WorkScenarios.ToListAsync());
    }

    [Theory]
    [InlineData("serverSelectionRule")]
    [InlineData("inboundSelectionRule")]
    [InlineData("provisioningMode")]
    [InlineData("onPaymentSucceeded")]
    [InlineData("onPaymentFailed")]
    [InlineData("onRefund")]
    [InlineData("onSubscriptionExpired")]
    [InlineData("onRenewal")]
    [InlineData("cabinetText")]
    [InlineData("telegramText")]
    public async Task AdminWorkScenarios_Should_Reject_Oversized_Rule_And_Text_Fields(string field)
    {
        await using var db = CreateDb();
        var controller = CreateController(db);
        var request = Request("oversized-field");
        request = field switch
        {
            "serverSelectionRule" => request with { ServerSelectionRule = new string('s', 121) },
            "inboundSelectionRule" => request with { InboundSelectionRule = new string('i', 121) },
            "provisioningMode" => request with { ProvisioningMode = new string('m', 41) },
            "onPaymentSucceeded" => request with { OnPaymentSucceeded = new string('a', 4001) },
            "onPaymentFailed" => request with { OnPaymentFailed = new string('a', 4001) },
            "onRefund" => request with { OnRefund = new string('a', 4001) },
            "onSubscriptionExpired" => request with { OnSubscriptionExpired = new string('a', 4001) },
            "onRenewal" => request with { OnRenewal = new string('a', 4001) },
            "cabinetText" => request with { CabinetText = new string('a', 4001) },
            "telegramText" => request with { TelegramText = new string('a', 4001) },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.WorkScenarios.ToListAsync());
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Reject_Oversized_Allowed_Tariff_List()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);
        var tariffIds = Enumerable.Range(0, 110).Select(_ => Guid.NewGuid());
        var request = Request("oversized-tariffs") with
        {
            AllowedTariffIdsJson = System.Text.Json.JsonSerializer.Serialize(tariffIds)
        };

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.WorkScenarios.ToListAsync());
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Reject_Cross_Context_Concurrent_Update()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-work-scenario-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.WorkScenarios.Add(Scenario("auto"));
                await setup.SaveChangesAsync();
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var first = await firstDb.WorkScenarios.SingleAsync();
            var second = await secondDb.WorkScenarios.SingleAsync();
            var firstController = CreateController(firstDb);
            var secondController = CreateController(secondDb);

            var firstResult = await firstController.Update(
                first.Id,
                Request(first.Key) with { Name = "Первое изменение", Revision = first.Revision },
                CancellationToken.None);
            var secondResult = await secondController.Update(
                second.Id,
                Request(second.Key) with { Name = "Второе изменение", Revision = second.Revision },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(firstResult);
            Assert.IsType<ConflictObjectResult>(secondResult);
            await using var verify = new ApplicationDbContext(options);
            Assert.Equal("Первое изменение", (await verify.WorkScenarios.SingleAsync()).Name);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task AdminWorkScenarios_Should_Reject_Cross_Context_Concurrent_Delete()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-work-scenario-delete-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.WorkScenarios.Add(Scenario("auto"));
                await setup.SaveChangesAsync();
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var first = await firstDb.WorkScenarios.SingleAsync();
            var second = await secondDb.WorkScenarios.SingleAsync();
            var firstController = CreateController(firstDb);
            var secondController = CreateController(secondDb);

            var firstResult = await firstController.Update(
                first.Id,
                Request(first.Key) with { Name = "Первое изменение", Revision = first.Revision },
                CancellationToken.None);
            var secondResult = await secondController.Delete(second.Id, second.Revision, CancellationToken.None);

            Assert.IsType<OkObjectResult>(firstResult);
            Assert.IsType<ConflictObjectResult>(secondResult);
            await using var verify = new ApplicationDbContext(options);
            Assert.Equal("Первое изменение", (await verify.WorkScenarios.SingleAsync()).Name);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
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

    private static AdminWorkScenariosController CreateController(ApplicationDbContext db, IClock? clock = null)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminWorkScenariosController(db, clock)
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
}
