using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Me;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class MePaymentsControllerTests
{
    [Fact]
    public async Task GetPayments_Should_Return_Safe_User_History_With_Key_Statuses_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var foreignUserId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 6, 10, 9, 0, 0, TimeSpan.Zero);
        db.Users.AddRange(
            new User { Id = userId, Email = "payments-user@example.test", DisplayName = "Payments user", PasswordHash = "hash", ReferralCode = "payments-user" },
            new User { Id = foreignUserId, Email = "payments-foreign@example.test", DisplayName = "Foreign user", PasswordHash = "hash", ReferralCode = "payments-foreign" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Payments", Slug = "payments", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });

        var ownOrders = new[]
        {
            Order(userId, tariffId, OrderStatus.Completed, now.AddMinutes(1)),
            Order(userId, tariffId, OrderStatus.PendingPayment, now.AddMinutes(2)),
            Order(userId, tariffId, OrderStatus.Failed, now.AddMinutes(3)),
            Order(userId, tariffId, OrderStatus.Refunded, now.AddMinutes(4))
        };
        var foreignOrder = Order(foreignUserId, tariffId, OrderStatus.Completed, now.AddMinutes(5));
        db.Orders.AddRange(ownOrders);
        db.Orders.Add(foreignOrder);

        var succeeded = Payment(ownOrders[0].Id, PaymentStatus.Succeeded, now.AddMinutes(11), paidAt: now.AddMinutes(12), rawResponse: """{"secret":"paid"}""");
        var pending = Payment(ownOrders[1].Id, PaymentStatus.Pending, now.AddMinutes(10), rawResponse: """{"secret":"pending"}""");
        var failed = Payment(ownOrders[2].Id, PaymentStatus.Failed, now.AddMinutes(9), failedAt: now.AddMinutes(9), rawResponse: """{"secret":"failed"}""");
        var refunded = Payment(ownOrders[3].Id, PaymentStatus.Refunded, now.AddMinutes(8), paidAt: now.AddMinutes(7), refundedAt: now.AddMinutes(8), refundedAmount: 100, rawResponse: """{"secret":"refund"}""");
        var foreign = Payment(foreignOrder.Id, PaymentStatus.Succeeded, now.AddMinutes(13), rawResponse: """{"secret":"foreign"}""");
        db.Payments.AddRange(succeeded, pending, failed, refunded, foreign);
        db.PaymentWebhookEvents.Add(new PaymentWebhookEvent { PaymentAttemptId = succeeded.Id, Provider = PaymentProvider.YooKassa, ExternalEventId = "evt-paid", EventType = "payment.succeeded", Status = PaymentWebhookEventStatus.Processed, RawPayload = """{"secret":"webhook"}""" });
        db.Refunds.Add(new Refund { PaymentAttemptId = refunded.Id, ProviderRefundId = "refund-1", Amount = 100, Currency = "RUB", Status = RefundStatus.Succeeded, RawResponse = """{"secret":"refund-raw"}""" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var listResult = await controller.GetPayments(CancellationToken.None);

        var listOk = Assert.IsType<OkObjectResult>(listResult);
        var payments = Assert.IsAssignableFrom<IEnumerable<object>>(listOk.Value).ToList();
        Assert.Equal(4, payments.Count);
        Assert.Equal(new[] { "Succeeded", "Pending", "Failed", "Refunded" }, payments.Select(x => Read<string>(x, "Status")).OrderBy(StatusOrder));
        Assert.DoesNotContain(payments, x => Read<Guid>(x, "OrderId") == foreignOrder.Id);
        Assert.Equal(1, Read<int>(payments.Single(x => Read<Guid>(x, "Id") == succeeded.Id), "WebhookEventsCount"));
        Assert.Equal(1, Read<int>(payments.Single(x => Read<Guid>(x, "Id") == refunded.Id), "RefundsCount"));
        Assert.DoesNotContain("RawResponse", JsonSerializer.Serialize(listOk.Value));
        Assert.DoesNotContain("RawRequest", JsonSerializer.Serialize(listOk.Value));
        Assert.DoesNotContain("WebhookPayload", JsonSerializer.Serialize(listOk.Value));
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(listOk.Value));

        var singleResult = await controller.GetPayment(succeeded.Id, CancellationToken.None);
        var singleOk = Assert.IsType<OkObjectResult>(singleResult);
        Assert.Equal("Succeeded", Read<string>(singleOk.Value!, "Status"));
        Assert.Equal(1, Read<int>(singleOk.Value!, "WebhookEventsCount"));
        Assert.DoesNotContain("RawResponse", JsonSerializer.Serialize(singleOk.Value));

        Assert.IsType<NotFoundResult>(await controller.GetPayment(foreign.Id, CancellationToken.None));
    }

    private static Order Order(Guid userId, Guid tariffId, OrderStatus status, DateTimeOffset createdAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariffId,
            Type = OrderType.NewSubscription,
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            Status = status,
            Amount = 100,
            Currency = "RUB",
            ExpiresAt = createdAt.AddMinutes(15),
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static PaymentAttempt Payment(
        Guid orderId,
        PaymentStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? paidAt = null,
        DateTimeOffset? failedAt = null,
        DateTimeOffset? refundedAt = null,
        decimal refundedAmount = 0,
        string rawResponse = "{}")
        => new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = $"pay-{Guid.NewGuid():N}",
            ExternalEventId = $"evt-{Guid.NewGuid():N}",
            IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            Amount = 100,
            Currency = "RUB",
            Status = status,
            ConfirmationUrl = "https://payments.example.test/checkout",
            ReturnUrl = "https://cabinet.example.test/payments",
            RawRequest = """{"secret":"request"}""",
            RawResponse = rawResponse,
            WebhookPayload = """{"secret":"payload"}""",
            SignatureValidated = true,
            IsActivationProcessed = status == PaymentStatus.Succeeded,
            PaidAt = paidAt,
            FailedAt = failedAt,
            RefundedAt = refundedAt,
            RefundedAmount = refundedAmount,
            StatusReason = status.ToString(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static int StatusOrder(string status)
        => status switch
        {
            "Succeeded" => 0,
            "Pending" => 1,
            "Failed" => 2,
            "Refunded" => 3,
            _ => 99
        };

    private static T Read<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(value));
    }

    private static MeController CreateController(ApplicationDbContext db, Guid userId)
    {
        var configuration = new ConfigurationBuilder().Build();
        return new MeController(db, null!, null!, null!, null!, null!, configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "unit-test"))
                }
            }
        };
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }
}
