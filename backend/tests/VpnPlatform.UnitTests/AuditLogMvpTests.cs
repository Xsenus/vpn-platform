using System.Security.Claims;
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
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AuditLogMvpTests
{
    [Fact]
    public async Task Admin_Audit_Logs_Should_Filter_Recent_Records_On_Sqlite()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);
        db.AuditLogs.AddRange(
            new AuditLog { ActorType = "admin", ActorId = "admin-1", Action = "payment_provider.update", EntityType = "PaymentProviderAccount", EntityId = Guid.NewGuid().ToString(), BeforeJson = "{}", AfterJson = "{}", CreatedAt = now },
            new AuditLog { ActorType = "system", ActorId = "payment-orchestrator", Action = "payment.status.changed", EntityType = "PaymentAttempt", EntityId = Guid.NewGuid().ToString(), BeforeJson = "{}", AfterJson = "{}", CreatedAt = now.AddMinutes(-1) });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db, new FixedClock(now));
        var response = await controller.GetAuditLogs(new AdminAuditLogFilters(Action: "payment_provider", ActorType: "admin"), CancellationToken.None);

        var logs = AssertOk<List<AdminAuditLogDto>>(response);
        var log = Assert.Single(logs);
        Assert.Equal("payment_provider.update", log.Action);
        Assert.Equal("admin", log.ActorType);
    }

    [Theory]
    [InlineData(UserRoles.SupportAgent, false, true, false)]
    [InlineData(UserRoles.FinanceManager, true, false, false)]
    [InlineData(UserRoles.Operator, false, true, true)]
    [InlineData(UserRoles.ReadOnly, true, true, false)]
    [InlineData(UserRoles.Admin, true, true, true)]
    public async Task Admin_Audit_Logs_Should_Apply_Domain_Scope_Before_User_Filters(
        string role,
        bool expectFinance,
        bool expectSupport,
        bool expectBot)
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 5, 50, 0, TimeSpan.FromHours(7));
        db.AuditLogs.AddRange(
            new AuditLog { ActorType = "admin", ActorId = "admin-1", Action = "auth.login", EntityType = "Auth", EntityId = "admin-1", BeforeJson = "{}", AfterJson = "{\"scope\":\"common\"}", CreatedAt = now },
            new AuditLog { ActorType = "system", ActorId = "payments", Action = "payment.status.changed", EntityType = nameof(PaymentAttempt), EntityId = Guid.NewGuid().ToString(), BeforeJson = "{}", AfterJson = "{\"scope\":\"finance-private\"}", CreatedAt = now.AddSeconds(-1) },
            new AuditLog { ActorType = "admin", ActorId = "support", Action = "support.reply", EntityType = nameof(SupportConversation), EntityId = Guid.NewGuid().ToString(), BeforeJson = "{}", AfterJson = "{\"scope\":\"support-private\"}", CreatedAt = now.AddSeconds(-2) },
            new AuditLog { ActorType = "admin", ActorId = "bot", Action = "telegram_bot.settings.update", EntityType = "TelegramBotSettings", EntityId = Guid.NewGuid().ToString(), BeforeJson = "{}", AfterJson = "{\"scope\":\"bot-private\"}", CreatedAt = now.AddSeconds(-3) });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db, new FixedClock(now), role);
        var response = await controller.GetAuditLogs(new AdminAuditLogFilters(), CancellationToken.None);
        var logs = AssertOk<List<AdminAuditLogDto>>(response);

        Assert.Contains(logs, x => x.Action == "auth.login");
        Assert.Equal(expectFinance, logs.Any(x => x.Action == "payment.status.changed"));
        Assert.Equal(expectSupport, logs.Any(x => x.Action == "support.reply"));
        Assert.Equal(expectBot, logs.Any(x => x.Action == "telegram_bot.settings.update"));

        var serialized = string.Join('\n', logs.Select(x => x.AfterJson));
        Assert.Equal(expectFinance, serialized.Contains("finance-private", StringComparison.Ordinal));
        Assert.Equal(expectSupport, serialized.Contains("support-private", StringComparison.Ordinal));
        Assert.Equal(expectBot, serialized.Contains("bot-private", StringComparison.Ordinal));

        if (!expectFinance)
        {
            var bypassAttempt = AssertOk<List<AdminAuditLogDto>>(await controller.GetAuditLogs(
                new AdminAuditLogFilters(Action: "payment", EntityType: nameof(PaymentAttempt), Search: "payment"),
                CancellationToken.None));
            Assert.Empty(bypassAttempt);
        }
    }

    [Fact]
    public async Task Payment_Provider_Secret_Rotation_Should_Write_Redacted_Audit()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 6, 12, 10, 5, 0, TimeSpan.Zero);
        var controller = CreateAdminController(db, new FixedClock(now));

        var request = new UpsertPaymentProviderAccountCommand(
            PaymentProvider.YooKassa,
            PaymentProviderMode.Sandbox,
            "sandbox-yookassa",
            "YooKassa Sandbox",
            true,
            true,
            "shop-1",
            "https://api.yookassa.ru/v3",
            "https://example.test/success",
            "https://example.test/webhook",
            "raw-secret-must-not-leak",
            "raw-webhook-secret-must-not-leak",
            false,
            string.Empty,
            "{}");

        var response = await controller.CreatePaymentProviderAccount(request, CancellationToken.None);

        var account = AssertOk<PaymentProviderAccountDto>(response);
        Assert.Equal(PaymentProvider.YooKassa, account.Provider);

        var logs = await db.AuditLogs.OrderBy(x => x.Action).ToListAsync();
        Assert.Contains(logs, x => x.Action == "payment_provider.create");
        Assert.Contains(logs, x => x.Action == "payment_provider.secret.rotate");
        var auditJson = string.Join('\n', logs.Select(x => $"{x.BeforeJson}\n{x.AfterJson}"));
        Assert.DoesNotContain("raw-secret-must-not-leak", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-webhook-secret-must-not-leak", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rotatedSecretKey", auditJson, StringComparison.Ordinal);
        Assert.Contains("rotatedWebhookSecret", auditJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Payment_Status_Recheck_Should_Write_System_Audit()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 6, 12, 10, 10, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        var paymentProvider = new TestPaymentProvider("pay-audit-1");
        var orchestrator = CreateOrchestrator(db, clock, paymentProvider);
        var paymentId = await SeedPaymentGraphAsync(db, now, paymentProvider.PaymentId);

        var result = await orchestrator.RecheckPaymentAsync(paymentId, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "payment.status.changed");
        Assert.Equal("system", audit.ActorType);
        Assert.Equal("payment-orchestrator", audit.ActorId);
        Assert.Equal("PaymentAttempt", audit.EntityType);
        Assert.Equal(paymentId.ToString(), audit.EntityId);
        Assert.Contains("Succeeded", audit.AfterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-secret", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Payment_Status_Recheck_Should_Propagate_Provider_Cancellation_Without_State_Changes()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 2, 30, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        using var cancellation = new CancellationTokenSource();
        var paymentProvider = new CancellingStatusPaymentProvider("pay-cancelled-recheck", cancellation);
        var orchestrator = CreateOrchestrator(db, clock, paymentProvider);
        var paymentId = await SeedPaymentGraphAsync(db, now, paymentProvider.PaymentId);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.RecheckPaymentAsync(paymentId, cancellation.Token));

        db.ChangeTracker.Clear();
        var payment = await db.Payments.SingleAsync(x => x.Id == paymentId);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());
    }

    private static AdminOperationsController CreateAdminController(ApplicationDbContext db, FixedClock clock, string role = UserRoles.Admin)
    {
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var controller = new AdminOperationsController(db, null!, null!, providerAccounts, secretProtector: new TestSecretProtector());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Role, role) },
                    "test"))
            }
        };
        return controller;
    }

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, FixedClock clock, IPaymentProvider paymentProvider)
    {
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var paymentProviderFactory = new PaymentProviderFactory(new IPaymentProvider[] { paymentProvider });
        var nodeAllocation = new NodeAllocationService(db);
        var subscriptionService = new SubscriptionService(db, clock, nodeAllocation, new TestVpnProviderFactory(new TestVpnProvider()));
        var webhookVerifiers = paymentProvider is IPaymentWebhookVerifier verifier
            ? new[] { verifier }
            : Array.Empty<IPaymentWebhookVerifier>();
        return new PaymentOrchestrator(db, paymentProviderFactory, webhookVerifiers, providerAccounts, subscriptionService, clock);
    }

    private static async Task<Guid> SeedPaymentGraphAsync(ApplicationDbContext db, DateTimeOffset now, string providerPaymentId)
    {
        var user = new User { Id = Guid.NewGuid(), Email = "audit-buyer@example.test", DisplayName = "Audit Buyer", PasswordHash = "hash", ReferralCode = $"audit-{Guid.NewGuid():N}" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = $"monthly-{Guid.NewGuid():N}", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.YooKassa, Mode = PaymentProviderMode.Sandbox, Name = "audit-yookassa", PublicName = "YooKassa", IsEnabled = true, IsDefault = true, ShopId = "shop-1", ApiBaseUrl = "https://api.yookassa.ru/v3", ReturnUrl = "https://example.test/success", SecretKeyProtected = "raw-secret", UseWebhookIpAllowList = false };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PendingPayment, Amount = tariff.Price, Currency = tariff.Currency, ExpiresAt = now.AddMinutes(15), IsFirstPurchase = true };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, PaymentProviderAccountId = account.Id, Provider = PaymentProvider.YooKassa, ProviderMode = PaymentProviderMode.Sandbox, ProviderPaymentId = providerPaymentId, IdempotencyKey = $"payment:{order.Id:N}:audit", Amount = order.Amount, Currency = order.Currency, Status = PaymentStatus.Pending, ConfirmationUrl = "https://example.test/pay", ReturnUrl = account.ReturnUrl, RawRequest = "{}", RawResponse = "{}" };

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return payment.Id;
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static T AssertOk<T>(IActionResult response)
    {
        var ok = Assert.IsType<OkObjectResult>(response);
        return Assert.IsType<T>(ok.Value);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(0, value.Length - visibleTail)) + value[^Math.Min(visibleTail, value.Length)..];
    }

    private sealed class TestPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier
    {
        public TestPaymentProvider(string paymentId) => PaymentId = paymentId;
        public PaymentProvider Provider => PaymentProvider.YooKassa;
        public string PaymentId { get; }
        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken) => Task.FromResult(new PaymentInitResult(PaymentId, "https://example.test/pay", "{}"));
        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken) => Task.FromResult(new PaymentWebhookParseResult("evt-audit", "payment.succeeded", PaymentId, PaymentStatus.Succeeded, rawBody, true, 490m, "RUB", true));
        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken) => Task.FromResult(new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Succeeded, "{}"));
        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken) => Task.FromResult(new PaymentRefundResult($"refund-{Guid.NewGuid():N}", RefundStatus.Succeeded, "{}"));
        public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken) => Task.FromResult(new PaymentWebhookVerificationResult(true, "test", null));
    }

    private sealed class CancellingStatusPaymentProvider(string paymentId, CancellationTokenSource cancellation) : IPaymentProvider
    {
        public PaymentProvider Provider => PaymentProvider.YooKassa;
        public string PaymentId { get; } = paymentId;

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not propagated.");
        }

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider;
        public TestVpnProviderFactory(IVpnProvider provider) => _provider = provider;
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TestVpnProvider : IVpnProvider
    {
        public string Name => "x3ui";
        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken) => Task.FromResult(new VpnProvisionResult($"client-{request.SubscriptionId:N}", "vless://test@example.test:443", "/qr/test.png", "/config/test.txt"));
        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken) => CreateAccessAsync(request, cancellationToken);
        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }
}
