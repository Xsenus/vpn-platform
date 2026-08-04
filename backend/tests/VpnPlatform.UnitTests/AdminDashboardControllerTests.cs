using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminDashboardControllerTests
{
    [Theory]
    [InlineData(UserRoles.SupportAgent, 0, 1, false, false)]
    [InlineData(UserRoles.FinanceManager, 1, 0, true, false)]
    [InlineData(UserRoles.Admin, 1, 1, true, true)]
    public async Task Summary_Should_Redact_Domain_Data_And_Readiness_By_Role(
        string role,
        int expectedFinanceCount,
        int expectedSupportCount,
        bool expectPaymentChecks,
        bool expectBotCheck)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "dashboard-user@example.test",
            DisplayName = "Dashboard User",
            PasswordHash = "hash",
            RolesCsv = UserRoles.User,
            ReferralCode = "DASHBOARD"
        });
        db.Tariffs.Add(new Tariff
        {
            Id = tariffId,
            Name = "Dashboard tariff",
            Slug = "dashboard-tariff",
            Price = 490,
            Currency = "RUB",
            DurationDays = 30,
            IsActive = true
        });
        db.Orders.Add(new Order
        {
            Id = orderId,
            UserId = userId,
            TariffId = tariffId,
            Status = OrderStatus.Completed,
            Type = OrderType.NewSubscription,
            Amount = 490,
            Currency = "RUB",
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = now.AddDays(1),
            CreatedAt = now
        });
        db.Payments.Add(new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Amount = 490,
            Currency = "RUB",
            Status = PaymentStatus.Failed,
            CreatedAt = now
        });
        db.PaymentProviderAccounts.Add(new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Production,
            Name = "dashboard-production",
            PublicName = "Dashboard production",
            IsEnabled = true,
            ShopId = "shop-dashboard",
            SecretKeyProtected = "protected-secret",
            WebhookUrl = "https://api.example.test/webhook"
        });
        db.SupportConversations.Add(new SupportConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = "web",
            Status = "open",
            Subject = "Dashboard support",
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SiteContentBlocks.AddRange(
            new SiteContentBlock { Key = "telegram_bot.enabled", Group = "telegram_bot", Label = "Enabled", Value = "true", InputType = "checkbox" },
            new SiteContentBlock { Key = "telegram_bot.mode", Group = "telegram_bot", Label = "Mode", Value = "LongPolling", InputType = "select" },
            new SiteContentBlock { Key = "telegram_bot.public_bot_username", Group = "telegram_bot", Label = "Username", Value = "dashboard_bot", InputType = "text" },
            new SiteContentBlock { Key = "telegram_bot.bot_token_protected", Group = "telegram_bot", Label = "Token", Value = "protected-token", InputType = "secret" });
        await db.SaveChangesAsync();

        var controller = new AdminDashboardController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Role, role) },
                        "test"))
                }
            }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.GetSummary(CancellationToken.None));
        var summary = Assert.IsType<AdminDashboardSummaryDto>(result.Value);

        Assert.Equal(expectedFinanceCount, summary.PaidOrders);
        Assert.Equal(expectedFinanceCount, summary.FailedPayments);
        Assert.Equal(expectedFinanceCount, summary.RecentPayments);
        Assert.Equal(expectedFinanceCount, summary.RecentOrders);
        Assert.Equal(expectedSupportCount, summary.SupportConversationsCount);
        Assert.Equal(expectedSupportCount, summary.OpenSupportConversations);
        Assert.Equal(expectPaymentChecks, summary.ProductionReadiness.Checks.Any(x => x.Key == "payment-provider"));
        Assert.Equal(expectPaymentChecks, summary.ProductionReadiness.Checks.Any(x => x.Key == "payment-webhook"));
        Assert.Equal(expectBotCheck, summary.ProductionReadiness.Checks.Any(x => x.Key == "telegram-bot"));
    }
}
