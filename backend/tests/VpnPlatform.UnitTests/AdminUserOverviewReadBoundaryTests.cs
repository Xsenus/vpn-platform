using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminUserOverviewReadBoundaryTests
{
    [Fact]
    public async Task User_List_And_Overview_Collections_Should_Bound_Rows_In_Sql()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);
        var users = Enumerable.Range(0, 305).Select(index => new User
        {
            Email = $"bounded-user-{index}@example.test",
            DisplayName = $"Bounded user {index}",
            PasswordHash = "hash",
            RolesCsv = index == 304 ? UserRoles.Admin : UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = $"BOUND{index}",
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        var targetUser = users[^1];
        var tariff = new Tariff
        {
            Name = "Overview boundary",
            Slug = "overview-boundary",
            DurationDays = 30,
            Price = 500m,
            Currency = "RUB",
            IsActive = true
        };
        db.Users.AddRange(users);
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();

        var orders = Enumerable.Range(0, 25).Select(index => new Order
        {
            UserId = targetUser.Id,
            TariffId = tariff.Id,
            Type = OrderType.NewSubscription,
            Status = OrderStatus.Completed,
            Amount = 500m,
            Currency = "RUB",
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = now.AddDays(1),
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.Orders.AddRange(orders);
        db.TelegramAccounts.Add(new TelegramAccount
        {
            UserId = targetUser.Id,
            TelegramUserId = 8_130_000,
            Username = "bounded",
            LinkedAt = now,
            LastSeenAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SupportConversations.AddRange(Enumerable.Range(0, 25).Select(index => new SupportConversation
        {
            UserId = targetUser.Id,
            Channel = "web",
            Status = "open",
            Subject = $"Boundary {index}",
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }));
        await db.SaveChangesAsync();

        db.Payments.AddRange(orders.Select((order, index) => new PaymentAttempt
        {
            OrderId = order.Id,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = $"bounded-payment-{index}",
            IdempotencyKey = $"bounded-idempotency-{index}",
            Amount = order.Amount,
            Currency = order.Currency,
            Status = PaymentStatus.Succeeded,
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = CreateController(db);
        var listResult = Assert.IsType<OkObjectResult>(
            await controller.GetList(null, null, null, CancellationToken.None));
        var overviewResult = Assert.IsType<OkObjectResult>(
            await controller.GetOverview(targetUser.Id, CancellationToken.None));

        using var listJson = JsonDocument.Parse(JsonSerializer.Serialize(listResult.Value));
        using var overviewJson = JsonDocument.Parse(JsonSerializer.Serialize(overviewResult.Value));
        Assert.Equal(300, listJson.RootElement.GetArrayLength());
        Assert.Equal(1, overviewJson.RootElement.GetProperty("TelegramAccounts").GetArrayLength());
        Assert.Equal(20, overviewJson.RootElement.GetProperty("Orders").GetArrayLength());
        Assert.Equal(20, overviewJson.RootElement.GetProperty("Payments").GetArrayLength());
        Assert.Equal(20, overviewJson.RootElement.GetProperty("SupportConversations").GetArrayLength());

        AssertBoundedQuery(interceptor.Commands, "Users", 300);
        AssertBoundedQuery(interceptor.Commands, "Orders", 20);
        AssertBoundedQuery(interceptor.Commands, "Payments", 20);
        AssertBoundedQuery(interceptor.Commands, "SupportConversations", 20);
    }

    [Fact]
    public async Task User_List_Role_Filter_Should_Match_Whole_Csv_Token()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Users.AddRange(
            new User
            {
                Email = "exact-admin@example.test",
                DisplayName = "Exact admin",
                PasswordHash = "hash",
                RolesCsv = $"{UserRoles.User}, {UserRoles.Admin}",
                ReferralCode = "EXACTADMIN"
            },
            new User
            {
                Email = "substring-admin@example.test",
                DisplayName = "Substring admin",
                PasswordHash = "hash",
                RolesCsv = $"Not{UserRoles.Admin}",
                ReferralCode = "SUBADMIN"
            });
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(
            await CreateController(db).GetList(null, null, UserRoles.Admin, CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));

        Assert.Equal(1, json.RootElement.GetArrayLength());
        Assert.Equal("exact-admin@example.test", json.RootElement[0].GetProperty("Email").GetString());
    }

    private static AdminUsersController CreateController(ApplicationDbContext db)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, UserRoles.Admin)], "unit-test");
        return new AdminUsersController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static void AssertBoundedQuery(IEnumerable<string> commands, string table, int limit)
    {
        Assert.Contains(commands, command =>
            command.Contains(table, StringComparison.OrdinalIgnoreCase)
            && command.Contains($"LIMIT {limit}", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
