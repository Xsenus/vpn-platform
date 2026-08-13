using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminSubscriptionReadBoundaryTests
{
    [Fact]
    public async Task Admin_Subscription_And_Access_Lists_Should_Bound_Rows_And_History_In_Sql()
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

        var now = new DateTimeOffset(2026, 8, 13, 7, 0, 0, TimeSpan.Zero);
        var user = new User
        {
            Email = "bounded-admin-read@example.test",
            DisplayName = "Bounded read",
            PasswordHash = "hash",
            ReferralCode = "BOUNDREAD"
        };
        var tariff = new Tariff
        {
            Name = "Bounded read",
            Slug = "bounded-admin-read",
            DurationDays = 30,
            Price = 500m,
            Currency = "RUB",
            IsActive = true
        };
        var node = new VpnNode
        {
            Name = "bounded-admin-read",
            Host = "bounded-admin-read.example.test",
            Provider = "x3ui",
            Region = "eu",
            Country = "NL",
            Status = NodeStatus.Ready,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 1000,
            IsAvailableForNewUsers = true
        };
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var subscriptions = Enumerable.Range(0, 305).Select(index => new Subscription
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Active,
            StartAt = now.AddDays(-1),
            EndAt = now.AddDays(29),
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.Subscriptions.AddRange(subscriptions);
        await db.SaveChangesAsync();

        var accesses = subscriptions.Select((subscription, index) => new AccessCredential
        {
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = $"bounded-client-{index}",
            AccessUri = $"vless://bounded-client-{index}@example.test",
            Status = AccessCredentialStatus.Active,
            IssuedAt = now.AddMinutes(index),
            Revision = 1,
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.AccessCredentials.AddRange(accesses);
        await db.SaveChangesAsync();

        var newest = accesses[^1];
        db.AccessCredentialHistories.AddRange(Enumerable.Range(0, 7).Select(index => new AccessCredentialHistory
        {
            AccessCredentialId = newest.Id,
            SubscriptionId = newest.SubscriptionId,
            EventType = $"BoundedHistory{index}",
            OldValueJson = "{}",
            NewValueJson = "{}",
            CreatedAt = now.AddHours(index),
            UpdatedAt = now.AddHours(index)
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = new AdminOperationsController(db, null!, null!, null!, clock: new FixedClock(now.AddDays(1)));
        var subscriptionResult = Assert.IsType<OkObjectResult>(await controller.GetSubscriptions(CancellationToken.None));
        var accessResult = Assert.IsType<OkObjectResult>(await controller.GetAccessCredentials(CancellationToken.None));
        var usersController = new AdminUsersController(db, new FixedClock(now.AddDays(1)))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.Role, UserRoles.Admin)],
                        "unit-test"))
                }
            }
        };
        var overviewResult = Assert.IsType<OkObjectResult>(await usersController.GetOverview(user.Id, CancellationToken.None));

        using var subscriptionJson = JsonDocument.Parse(JsonSerializer.Serialize(subscriptionResult.Value));
        using var accessJson = JsonDocument.Parse(JsonSerializer.Serialize(accessResult.Value));
        Assert.Equal(300, subscriptionJson.RootElement.GetArrayLength());
        Assert.Equal(300, accessJson.RootElement.GetArrayLength());
        Assert.Equal(newest.Id, accessJson.RootElement[0].GetProperty("Id").GetGuid());
        Assert.Equal(5, accessJson.RootElement[0].GetProperty("History").GetArrayLength());
        using var overviewJson = JsonDocument.Parse(JsonSerializer.Serialize(overviewResult.Value));
        Assert.Equal(20, overviewJson.RootElement.GetProperty("Subscriptions").GetArrayLength());
        Assert.Equal(20, overviewJson.RootElement.GetProperty("AccessCredentials").GetArrayLength());

        Assert.Contains(interceptor.Commands, command =>
            command.Contains("Subscriptions", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 300", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("AccessCredentials", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 300", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("AccessCredentialHistories", StringComparison.OrdinalIgnoreCase)
            && command.Contains("ROW_NUMBER", StringComparison.OrdinalIgnoreCase)
            && command.Contains("<= 5", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("Subscriptions", StringComparison.OrdinalIgnoreCase)
            && command.Contains("UserId", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 20", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("AccessCredentials", StringComparison.OrdinalIgnoreCase)
            && command.Contains("Subscriptions", StringComparison.OrdinalIgnoreCase)
            && command.Contains("UserId", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 20", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
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
