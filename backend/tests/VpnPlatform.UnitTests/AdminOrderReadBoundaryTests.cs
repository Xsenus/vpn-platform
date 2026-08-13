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

public class AdminOrderReadBoundaryTests
{
    [Fact]
    public async Task Admin_Order_Search_Should_Bound_Parents_And_Latest_Payments_In_Sql()
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
        var user = new User
        {
            Email = "bounded-orders@example.test",
            DisplayName = "Bounded orders",
            PasswordHash = "hash",
            ReferralCode = "BOUNDEDORDERS"
        };
        var tariff = new Tariff
        {
            Name = "Bounded orders",
            Slug = "bounded-orders",
            DurationDays = 30,
            Price = 500m,
            Currency = "RUB",
            IsActive = true
        };
        db.AddRange(user, tariff);
        await db.SaveChangesAsync();

        var orders = Enumerable.Range(0, 305).Select(index => new Order
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            Status = OrderStatus.PendingPayment,
            Type = OrderType.NewSubscription,
            Amount = 500m,
            Currency = "RUB",
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = now.AddDays(1),
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();

        var payments = orders.Select((order, index) => new PaymentAttempt
        {
            OrderId = order.Id,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = $"bounded-order-payment-{index}",
            IdempotencyKey = $"bounded-order-payment-{index}",
            Amount = 500m,
            Currency = "RUB",
            Status = PaymentStatus.Pending,
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }).ToList();
        var newestOrder = orders[^1];
        var newestPayment = new PaymentAttempt
        {
            OrderId = newestOrder.Id,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = "bounded-order-latest",
            IdempotencyKey = "bounded-order-latest",
            Amount = 500m,
            Currency = "RUB",
            Status = PaymentStatus.WaitingConfirmation,
            CreatedAt = now.AddDays(1),
            UpdatedAt = now.AddDays(1)
        };
        db.Payments.AddRange(payments);
        db.Payments.Add(newestPayment);
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = new AdminOperationsController(db, null!, null!, null!);
        var result = Assert.IsType<OkObjectResult>(await controller.GetOrders(
            "PendingPayment",
            "bounded-orders@example.test",
            CancellationToken.None));

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal(300, json.RootElement.GetArrayLength());
        Assert.Equal(newestOrder.Id, json.RootElement[0].GetProperty("Id").GetGuid());
        Assert.Equal(2, json.RootElement[0].GetProperty("PaymentAttemptsCount").GetInt32());
        Assert.Equal(newestPayment.Id, json.RootElement[0].GetProperty("LastPaymentId").GetGuid());
        Assert.Equal("WaitingConfirmation", json.RootElement[0].GetProperty("LastPaymentStatus").GetString());

        Assert.Contains(interceptor.Commands, command =>
            command.Contains("Orders", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 300", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("Payments", StringComparison.OrdinalIgnoreCase)
            && command.Contains("ROW_NUMBER", StringComparison.OrdinalIgnoreCase));

        using var paymentStatusJson = ToJson(await controller.GetOrders(
            null,
            "waitingconfirmation",
            CancellationToken.None));
        Assert.Equal(1, paymentStatusJson.RootElement.GetArrayLength());
        Assert.Equal(newestOrder.Id, paymentStatusJson.RootElement[0].GetProperty("Id").GetGuid());

        using var providerPaymentJson = ToJson(await controller.GetOrders(
            null,
            "bounded-order-latest",
            CancellationToken.None));
        Assert.Equal(1, providerPaymentJson.RootElement.GetArrayLength());
        Assert.Equal(newestOrder.Id, providerPaymentJson.RootElement[0].GetProperty("Id").GetGuid());

        using var orderTypeJson = ToJson(await controller.GetOrders(
            null,
            "newsubscription",
            CancellationToken.None));
        Assert.Equal(300, orderTypeJson.RootElement.GetArrayLength());
    }

    private static JsonDocument ToJson(IActionResult result)
        => JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value));

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
