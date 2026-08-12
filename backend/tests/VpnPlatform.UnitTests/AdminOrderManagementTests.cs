using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminOrderManagementTests
{
    [Fact]
    public async Task GetOrders_Should_Filter_By_Status_Search_And_Return_LastPayment_On_Sqlite()
    {
        await using var db = CreateDb();
        var user = new User { Id = Guid.NewGuid(), Email = "orders@test.local", DisplayName = "Orders User", PasswordHash = "hash" };
        var tariff = Tariff("premium-orders");
        var matchingOrder = Order(user.Id, tariff.Id, OrderStatus.PendingPayment, createdAt: DateTimeOffset.UtcNow);
        var completedOrder = Order(user.Id, tariff.Id, OrderStatus.Completed, createdAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        var lastPayment = Payment(matchingOrder.Id, PaymentStatus.WaitingConfirmation, createdAt: DateTimeOffset.UtcNow);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.Orders.AddRange(matchingOrder, completedOrder);
        db.Payments.AddRange(
            Payment(matchingOrder.Id, PaymentStatus.Pending, createdAt: DateTimeOffset.UtcNow.AddMinutes(-10)),
            lastPayment,
            Payment(completedOrder.Id, PaymentStatus.Succeeded, createdAt: DateTimeOffset.UtcNow.AddMinutes(-4)));
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetOrders("PendingPayment", "orders@test.local", CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var orders = json.RootElement.EnumerateArray().ToList();

        Assert.Single(orders);
        Assert.Equal(matchingOrder.Id, orders[0].GetProperty("Id").GetGuid());
        Assert.Equal(lastPayment.Id, orders[0].GetProperty("LastPaymentId").GetGuid());
        Assert.Equal("WaitingConfirmation", orders[0].GetProperty("LastPaymentStatus").GetString());
        Assert.True(orders[0].GetProperty("LastPaymentRecheckSupported").GetBoolean());

        Assert.IsType<BadRequestObjectResult>(await controller.GetOrders("NotAStatus", null, CancellationToken.None));
    }

    [Fact]
    public async Task RecheckOrderPayment_Should_Recheck_Latest_PaymentAttempt()
    {
        await using var db = CreateDb();
        var clock = new TestClock(new DateTimeOffset(2026, 6, 11, 1, 30, 0, TimeSpan.Zero));
        var user = new User { Id = Guid.NewGuid(), Email = "recheck@test.local", DisplayName = "Recheck User", PasswordHash = "hash" };
        var tariff = Tariff("recheck-orders");
        var order = Order(user.Id, tariff.Id, OrderStatus.PendingPayment, createdAt: clock.UtcNow.AddMinutes(-20));
        var account = new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            Name = "sandbox-yookassa",
            PublicName = "YooKassa Sandbox",
            IsEnabled = true,
            ShopId = "shop",
            SecretKeyProtected = "secret"
        };
        var olderPayment = Payment(order.Id, PaymentStatus.Pending, createdAt: clock.UtcNow.AddMinutes(-10), account.Id);
        var latestPayment = Payment(order.Id, PaymentStatus.Pending, createdAt: clock.UtcNow.AddMinutes(-1), account.Id);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.Orders.Add(order);
        db.PaymentProviderAccounts.Add(account);
        db.Payments.AddRange(olderPayment, latestPayment);
        await db.SaveChangesAsync();

        var provider = new TrackingPaymentProvider(PaymentStatus.WaitingConfirmation);
        var controller = CreateController(db, CreateOrchestrator(db, provider, clock));

        var ok = Assert.IsType<OkObjectResult>(await controller.RecheckOrderPayment(order.Id, CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.Equal(order.Id, json.RootElement.GetProperty("OrderId").GetGuid());
        Assert.Equal(latestPayment.Id, json.RootElement.GetProperty("PaymentId").GetGuid());
        Assert.Equal("WaitingConfirmation", json.RootElement.GetProperty("Status").GetString());
        Assert.False(json.RootElement.TryGetProperty("RawResponse", out _));
        Assert.Equal(latestPayment.Id, provider.LastStatusPaymentId);
        await db.Entry(latestPayment).ReloadAsync();
        Assert.Equal(PaymentStatus.WaitingConfirmation, latestPayment.Status);
        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().Where(x => x.Action == "order.payment.recheck").ToListAsync());
        Assert.Equal("admin", audit.ActorType);
        Assert.DoesNotContain("private-provider-marker", audit.BeforeJson, StringComparison.Ordinal);
        Assert.DoesNotContain("private-provider-marker", audit.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecheckOrderPayment_Should_Reject_Order_Without_Payments()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.RecheckOrderPayment(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RecheckOrderPayment_Should_Reject_Unsupported_Provider_Before_Adapter_Call()
    {
        await using var db = CreateDb();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero));
        var user = new User { Id = Guid.NewGuid(), Email = "unsupported-recheck@test.local", DisplayName = "Unsupported Recheck", PasswordHash = "hash" };
        var tariff = Tariff("unsupported-recheck");
        var order = Order(user.Id, tariff.Id, OrderStatus.PendingPayment, clock.UtcNow.AddMinutes(-10));
        order.PaymentProvider = PaymentProvider.RoboKassa;
        var account = new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.RoboKassa,
            Mode = PaymentProviderMode.Sandbox,
            Name = "robokassa",
            PublicName = "RoboKassa",
            IsEnabled = true,
            ShopId = "merchant"
        };
        var payment = Payment(order.Id, PaymentStatus.Pending, clock.UtcNow.AddMinutes(-1), account.Id, PaymentProvider.RoboKassa);
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.Orders.Add(order);
        db.PaymentProviderAccounts.Add(account);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        var provider = new TrackingPaymentProvider(PaymentStatus.Succeeded, PaymentProvider.RoboKassa);

        var result = await CreateController(db, CreateOrchestrator(db, provider, clock))
            .RecheckOrderPayment(order.Id, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(badRequest.Value));
        Assert.Equal("Сверка статуса платежа недоступна.", json.RootElement.GetProperty("error").GetString());
        Assert.False(json.RootElement.GetProperty("readiness").GetProperty("CanRecheck").GetBoolean());
        Assert.NotEmpty(json.RootElement.GetProperty("readiness").GetProperty("Blockers").EnumerateArray());
        Assert.Null(provider.LastStatusPaymentId);
        await db.Entry(payment).ReloadAsync();
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, IPaymentProvider provider, TestClock clock)
    {
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        return new PaymentOrchestrator(db, new TestPaymentProviderFactory(provider), Array.Empty<IPaymentWebhookVerifier>(), providerAccounts, null!, clock);
    }

    private static AdminOperationsController CreateController(ApplicationDbContext db, PaymentOrchestrator? orchestrator = null)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminOperationsController(db, null!, orchestrator!, null!)
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

    private static Tariff Tariff(string slug)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"Tariff {slug}",
            Slug = slug,
            Description = "Test tariff",
            DurationDays = 30,
            Price = 490m,
            Currency = "RUB",
            MaxDevices = 3,
            IsActive = true,
            SortOrder = 10,
            Category = "standard",
            ProvisioningScenario = "auto"
        };

    private static Order Order(Guid userId, Guid tariffId, OrderStatus status, DateTimeOffset createdAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariffId,
            Amount = 490m,
            Currency = "RUB",
            Status = status,
            ExpiresAt = createdAt.AddMinutes(15),
            CreatedAt = createdAt,
            PaymentProvider = PaymentProvider.YooKassa
        };

    private static PaymentAttempt Payment(Guid orderId, PaymentStatus status, DateTimeOffset createdAt, Guid? accountId = null, PaymentProvider provider = PaymentProvider.YooKassa)
        => new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PaymentProviderAccountId = accountId,
            Provider = provider,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = $"payment-{Guid.NewGuid():N}",
            IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            Amount = 490m,
            Currency = "RUB",
            Status = status,
            CreatedAt = createdAt
        };

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : "***";
    }

    private sealed class TestPaymentProviderFactory(IPaymentProvider provider) : IPaymentProviderFactory
    {
        public IPaymentProvider Get(PaymentProvider _) => provider;
    }

    private sealed class TrackingPaymentProvider(PaymentStatus status, PaymentProvider provider = PaymentProvider.YooKassa) : IPaymentProvider
    {
        public PaymentProvider Provider => provider;
        public Guid? LastStatusPaymentId { get; private set; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentInitResult("payment-id", "https://payment.test", "{}"));

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
        {
            LastStatusPaymentId = payment.Id;
            return Task.FromResult(new PaymentStatusResult(payment.ProviderPaymentId, status, """{"status":"ok","private":"private-provider-marker"}"""));
        }

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
