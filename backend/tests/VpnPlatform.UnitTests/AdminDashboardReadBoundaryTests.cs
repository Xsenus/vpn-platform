using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminDashboardReadBoundaryTests
{
    [Fact]
    public async Task Dashboard_Should_Calculate_Time_Boundaries_With_Aggregate_Queries()
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
        var recentSince = now.AddDays(-7);
        var user = new User
        {
            Email = "dashboard-boundary@example.test",
            DisplayName = "Dashboard boundary",
            PasswordHash = "hash",
            ReferralCode = "DASHBOUNDARY"
        };
        var tariff = new Tariff
        {
            Name = "Dashboard boundary",
            Slug = "dashboard-boundary",
            Price = 500,
            Currency = "RUB",
            DurationDays = 30,
            IsActive = true
        };
        db.AddRange(user, tariff);
        await db.SaveChangesAsync();

        db.Subscriptions.AddRange(
            CreateSubscription(user.Id, tariff.Id, SubscriptionStatus.Active, now, now),
            CreateSubscription(user.Id, tariff.Id, SubscriptionStatus.GracePeriod, now.AddDays(-1), now.AddDays(2)),
            CreateSubscription(user.Id, tariff.Id, SubscriptionStatus.Active, now.AddDays(8), null));
        var recentOrder = CreateOrder(user.Id, tariff.Id, recentSince);
        var oldOrder = CreateOrder(user.Id, tariff.Id, recentSince.AddSeconds(-1));
        db.Orders.AddRange(recentOrder, oldOrder);
        db.Payments.AddRange(
            CreatePayment(recentOrder.Id, recentSince, "recent"),
            CreatePayment(oldOrder.Id, recentSince.AddSeconds(-1), "old"));
        db.PaymentProviderAccounts.Add(new PaymentProviderAccount
        {
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Production,
            Name = "dashboard-boundary",
            PublicName = "Dashboard boundary",
            IsEnabled = true,
            ShopId = "shop-dashboard",
            SecretKeyProtected = "protected-secret",
            WebhookUrl = "https://api.example.test/webhook"
        });
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = new AdminDashboardController(db, clock: new FixedClock(now))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.Role, UserRoles.Admin)],
                        "test"))
                }
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.GetSummary(CancellationToken.None));
        var summary = Assert.IsType<AdminDashboardSummaryDto>(result.Value);
        Assert.Equal(2, summary.ActiveSubscriptions);
        Assert.Equal(1, summary.ExpiringSubscriptions);
        Assert.Equal(1, summary.RecentOrders);
        Assert.Equal(1, summary.RecentPayments);

        foreach (var table in new[] { "Subscriptions", "Orders", "Payments", "PaymentProviderAccounts" })
        {
            var commands = interceptor.Commands
                .Where(command => command.Contains(table, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.NotEmpty(commands);
            Assert.All(commands, command => Assert.Contains("COUNT(", command, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static Subscription CreateSubscription(
        Guid userId,
        Guid tariffId,
        SubscriptionStatus status,
        DateTimeOffset endAt,
        DateTimeOffset? gracePeriodEndAt)
        => new()
        {
            UserId = userId,
            TariffId = tariffId,
            Status = status,
            StartAt = endAt.AddDays(-30),
            EndAt = endAt,
            GracePeriodEndAt = gracePeriodEndAt
        };

    private static Order CreateOrder(Guid userId, Guid tariffId, DateTimeOffset createdAt)
        => new()
        {
            UserId = userId,
            TariffId = tariffId,
            Status = OrderStatus.Completed,
            Type = OrderType.NewSubscription,
            Amount = 500,
            Currency = "RUB",
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = createdAt.AddDays(1),
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static PaymentAttempt CreatePayment(Guid orderId, DateTimeOffset createdAt, string suffix)
        => new()
        {
            OrderId = orderId,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = $"dashboard-{suffix}",
            IdempotencyKey = $"dashboard-{suffix}",
            Amount = 500,
            Currency = "RUB",
            Status = PaymentStatus.Succeeded,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

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
