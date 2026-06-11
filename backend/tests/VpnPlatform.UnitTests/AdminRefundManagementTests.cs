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

public class AdminRefundManagementTests
{
    [Fact]
    public async Task GetPayments_Should_Return_Provider_Specific_Refund_Readiness_On_Sqlite()
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var unsupportedAccount = Account(PaymentProvider.RoboKassa, secret: "secret");
        var refundable = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m, refundedAmount: 30m);
        var unsupported = Payment(user.Id, tariff.Id, unsupportedAccount, PaymentStatus.Succeeded, amount: 100m);
        var pending = Payment(user.Id, tariff.Id, account, PaymentStatus.Pending, amount: 100m);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.AddRange(account, unsupportedAccount);
        db.Orders.AddRange(refundable.Order!, unsupported.Order!, pending.Order!);
        db.Payments.AddRange(refundable, unsupported, pending);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetPayments(CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var payments = json.RootElement.EnumerateArray().ToList();

        var readyJson = payments.Single(x => x.GetProperty("Id").GetGuid() == refundable.Id);
        Assert.True(readyJson.GetProperty("CanRefund").GetBoolean());
        Assert.True(readyJson.GetProperty("RefundSupported").GetBoolean());
        Assert.Equal(70m, readyJson.GetProperty("RefundableAmount").GetDecimal());
        Assert.Empty(readyJson.GetProperty("RefundBlockers").EnumerateArray());

        var unsupportedJson = payments.Single(x => x.GetProperty("Id").GetGuid() == unsupported.Id);
        Assert.False(unsupportedJson.GetProperty("CanRefund").GetBoolean());
        Assert.False(unsupportedJson.GetProperty("RefundSupported").GetBoolean());
        Assert.Contains("не поддерживает", unsupportedJson.GetProperty("RefundBlockers").EnumerateArray().First().GetString());

        var pendingJson = payments.Single(x => x.GetProperty("Id").GetGuid() == pending.Id);
        Assert.False(pendingJson.GetProperty("CanRefund").GetBoolean());
        Assert.Contains(pendingJson.GetProperty("RefundBlockers").EnumerateArray(), x => x.GetString()?.Contains("успешных", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task RefundPayment_Should_Reject_Unsupported_Provider_Before_Provider_Call()
    {
        await using var db = CreateDb();
        var provider = new TrackingPaymentProvider(PaymentProvider.RoboKassa);
        var account = Account(PaymentProvider.RoboKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db, CreateOrchestrator(db, provider));
        var result = await controller.RefundPayment(payment.Id, new RefundPaymentHttpRequest(50m, "unsupported"), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cannot be refunded", JsonSerializer.Serialize(badRequest.Value));
        Assert.Equal(0, provider.RefundCalls);
        Assert.Empty(await db.Refunds.ToListAsync());
    }

    [Fact]
    public async Task RefundPayment_Should_Process_Supported_Refund_And_Update_Amounts()
    {
        await using var db = CreateDb();
        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa);
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db, CreateOrchestrator(db, provider));
        var ok = Assert.IsType<OkObjectResult>(await controller.RefundPayment(payment.Id, new RefundPaymentHttpRequest(40m, "partial-test"), CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.Equal("Succeeded", json.RootElement.GetProperty("Status").GetString());
        Assert.Equal(1, provider.RefundCalls);
        Assert.Equal(40m, provider.LastRefundAmount);
        await db.Entry(payment).ReloadAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(40m, payment.RefundedAmount);
        Assert.Single(await db.Refunds.ToListAsync());
    }

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, IPaymentProvider provider)
    {
        var clock = new TestClock(new DateTimeOffset(2026, 6, 11, 2, 15, 0, TimeSpan.Zero));
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
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static User User()
        => new() { Id = Guid.NewGuid(), Email = "refund@test.local", DisplayName = "Refund User", PasswordHash = "hash" };

    private static Tariff Tariff()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Refund tariff",
            Slug = $"refund-{Guid.NewGuid():N}",
            Description = "Refund tariff",
            DurationDays = 30,
            Price = 100m,
            Currency = "RUB",
            MaxDevices = 3,
            IsActive = true,
            SortOrder = 10,
            Category = "standard",
            ProvisioningScenario = "auto"
        };

    private static PaymentProviderAccount Account(PaymentProvider provider, string secret)
        => new()
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            Mode = PaymentProviderMode.Sandbox,
            Name = $"{provider}-sandbox",
            PublicName = $"{provider} Sandbox",
            IsEnabled = true,
            ShopId = "shop",
            SecretKeyProtected = secret
        };

    private static PaymentAttempt Payment(Guid userId, Guid tariffId, PaymentProviderAccount account, PaymentStatus status, decimal amount, decimal refundedAmount = 0)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariffId,
            Amount = amount,
            Currency = "RUB",
            Status = OrderStatus.Completed,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            PaymentProvider = account.Provider
        };

        return new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PaymentProviderAccountId = account.Id,
            PaymentProviderAccount = account,
            Provider = account.Provider,
            ProviderMode = account.Mode,
            ProviderPaymentId = $"payment-{Guid.NewGuid():N}",
            IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            Amount = amount,
            Currency = "RUB",
            Status = status,
            RefundedAmount = refundedAmount,
            PaidAt = status == PaymentStatus.Succeeded ? DateTimeOffset.UtcNow : null
        };
    }

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

    private sealed class TrackingPaymentProvider(PaymentProvider provider) : IPaymentProvider
    {
        public PaymentProvider Provider { get; } = provider;
        public int RefundCalls { get; private set; }
        public decimal LastRefundAmount { get; private set; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentInitResult("payment-id", "https://payment.test", "{}"));

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentStatusResult(payment.ProviderPaymentId, payment.Status, "{}"));

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        {
            RefundCalls++;
            LastRefundAmount = amount;
            return Task.FromResult(new PaymentRefundResult($"refund-{payment.Id:N}", RefundStatus.Succeeded, "{}"));
        }
    }
}
