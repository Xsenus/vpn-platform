using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminFinanceReadBoundaryTests
{
    [Fact]
    public async Task Admin_Payment_Refund_And_Order_Recheck_Reads_Should_Bound_Rows_In_Sql()
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
            Email = "bounded-finance@example.test",
            DisplayName = "Bounded finance",
            PasswordHash = "hash",
            ReferralCode = "BOUNDEDFINANCE"
        };
        var tariff = new Tariff
        {
            Name = "Bounded finance",
            Slug = "bounded-finance",
            DurationDays = 30,
            Price = 500m,
            Currency = "RUB",
            IsActive = true
        };
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.RoboKassa,
            Mode = PaymentProviderMode.Sandbox,
            Name = "bounded-finance",
            PublicName = "Bounded finance",
            IsEnabled = true,
            ShopId = "bounded-finance"
        };
        db.AddRange(user, tariff, account);
        await db.SaveChangesAsync();

        var orders = Enumerable.Range(0, 305).Select(index => new Order
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = OrderStatus.Completed,
            Type = OrderType.NewSubscription,
            Amount = 500m,
            Currency = "RUB",
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.RoboKassa,
            ExpiresAt = now.AddDays(1),
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();

        var payments = orders.Select((order, index) => new PaymentAttempt
        {
            OrderId = order.Id,
            PaymentProviderAccountId = account.Id,
            Provider = PaymentProvider.RoboKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = $"bounded-finance-{index}",
            IdempotencyKey = $"bounded-finance-{index}",
            Amount = 500m,
            Currency = "RUB",
            Status = PaymentStatus.Succeeded,
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.Payments.AddRange(payments);
        await db.SaveChangesAsync();

        var refunds = payments.Select((payment, index) => new Refund
        {
            PaymentAttemptId = payment.Id,
            Provider = PaymentProvider.RoboKassa,
            ProviderRefundId = $"bounded-refund-{index}",
            IdempotencyKey = $"bounded-refund-{index}",
            Status = RefundStatus.Succeeded,
            Amount = 100m,
            Currency = "RUB",
            Reason = "bounded",
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.Refunds.AddRange(refunds);
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = new AdminOperationsController(db, null!, null!, null!);
        var paymentsResult = Assert.IsType<OkObjectResult>(await controller.GetPayments(CancellationToken.None));
        var refundsResult = Assert.IsType<OkObjectResult>(await controller.GetRefunds(CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(
            await controller.RecheckOrderPayment(orders[^1].Id, CancellationToken.None));

        using var paymentsJson = JsonDocument.Parse(JsonSerializer.Serialize(paymentsResult.Value));
        using var refundsJson = JsonDocument.Parse(JsonSerializer.Serialize(refundsResult.Value));
        Assert.Equal(300, paymentsJson.RootElement.GetArrayLength());
        Assert.Equal(payments[^1].Id, paymentsJson.RootElement[0].GetProperty("Id").GetGuid());
        Assert.Equal(300, refundsJson.RootElement.GetArrayLength());
        Assert.Equal(refunds[^1].Id, refundsJson.RootElement[0].GetProperty("Id").GetGuid());

        Assert.Contains(interceptor.Commands, command =>
            command.Contains("Payments", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 300", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("Refunds", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 300", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("Payments", StringComparison.OrdinalIgnoreCase)
            && command.Contains("OrderId", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 1", StringComparison.OrdinalIgnoreCase));
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
