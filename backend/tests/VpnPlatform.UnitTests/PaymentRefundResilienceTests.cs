using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentRefundResilienceTests
{
    [Theory]
    [InlineData(InvalidRefundAccountState.ProviderMismatch)]
    [InlineData(InvalidRefundAccountState.AccountDisabled)]
    [InlineData(InvalidRefundAccountState.ModeDisabled)]
    [InlineData(InvalidRefundAccountState.ProviderModeMismatch)]
    [InlineData(InvalidRefundAccountState.MissingProviderPaymentId)]
    [InlineData(InvalidRefundAccountState.MissingShopId)]
    [InlineData(InvalidRefundAccountState.MissingCredentials)]
    public async Task Invalid_Refund_Account_State_Should_Fail_Before_Reservation_Or_Provider_Call(InvalidRefundAccountState invalidState)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 12, 5, 0, 0, TimeSpan.Zero);
        var paymentId = await SeedRefundablePaymentAsync(db, now);
        var payment = await db.Payments.Include(x => x.PaymentProviderAccount).SingleAsync(x => x.Id == paymentId);
        var account = Assert.IsType<PaymentProviderAccount>(payment.PaymentProviderAccount);
        switch (invalidState)
        {
            case InvalidRefundAccountState.ProviderMismatch:
                account.Provider = PaymentProvider.Stripe;
                break;
            case InvalidRefundAccountState.AccountDisabled:
                account.IsEnabled = false;
                break;
            case InvalidRefundAccountState.ModeDisabled:
                account.Mode = PaymentProviderMode.Disabled;
                break;
            case InvalidRefundAccountState.ProviderModeMismatch:
                account.Mode = PaymentProviderMode.Production;
                break;
            case InvalidRefundAccountState.MissingProviderPaymentId:
                payment.ProviderPaymentId = string.Empty;
                break;
            case InvalidRefundAccountState.MissingShopId:
                account.ShopId = string.Empty;
                break;
            case InvalidRefundAccountState.MissingCredentials:
                account.SecretKeyProtected = string.Empty;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidState), invalidState, null);
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var provider = new TrackingPaymentProvider(() => { });
        var factory = new TrackingPaymentProviderFactory(provider);
        var orchestrator = CreateOrchestrator(db, factory, now);

        var result = await orchestrator.RefundPaymentAsync(paymentId, 40m, "invalid-account-state", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, factory.GetCalls);
        Assert.Equal(0, provider.RefundCalls);
        Assert.Empty(await db.Refunds.AsNoTracking().ToListAsync());
        var unchanged = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == paymentId);
        Assert.Equal(PaymentStatus.Succeeded, unchanged.Status);
        Assert.Equal(0m, unchanged.RefundedAmount);
    }

    [Fact]
    public async Task Unsupported_Provider_Should_Fail_Before_Refund_Reservation_Or_Adapter_Resolution()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 12, 4, 0, 0, TimeSpan.Zero);
        var paymentId = await SeedRefundablePaymentAsync(db, now, PaymentProvider.RoboKassa);
        var provider = new TrackingPaymentProvider(() => { }, PaymentProvider.RoboKassa);
        var factory = new TrackingPaymentProviderFactory(provider);
        var orchestrator = CreateOrchestrator(db, factory, now);

        var result = await orchestrator.RefundPaymentAsync(paymentId, 40m, "unsupported-provider", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not support refunds", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, factory.GetCalls);
        Assert.Equal(0, provider.RefundCalls);
        Assert.Empty(await db.Refunds.AsNoTracking().ToListAsync());
        var payment = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == paymentId);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(0m, payment.RefundedAmount);
    }

    [Fact]
    public async Task Reservation_Save_Failure_Should_Not_Call_Provider()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new FailingSaveApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 4, 10, 15, 0, TimeSpan.Zero);
        var paymentId = await SeedRefundablePaymentAsync(db, now);
        var provider = new TrackingPaymentProvider(() => { });
        var orchestrator = CreateOrchestrator(db, provider, now);
        db.FailNextSave = true;

        var result = await orchestrator.RefundPaymentAsync(paymentId, 40m, "reservation-failure", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("provider was not called", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.RefundCalls);
        await using var verificationDb = new ApplicationDbContext(options);
        Assert.Empty(await verificationDb.Refunds.ToListAsync());
        var payment = await verificationDb.Payments.SingleAsync(x => x.Id == paymentId);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(0m, payment.RefundedAmount);
    }

    [Fact]
    public async Task Final_Save_Failure_Should_Persist_Unknown_Refund_And_Prevent_A_Second_Provider_Call()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new FailingSaveApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 4, 10, 30, 0, TimeSpan.Zero);
        var paymentId = await SeedRefundablePaymentAsync(db, now);
        var provider = new TrackingPaymentProvider(() => db.FailNextSave = true);
        var orchestrator = CreateOrchestrator(db, provider, now);

        var first = await orchestrator.RefundPaymentAsync(paymentId, 40m, "database-failure", CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.Equal(1, provider.RefundCalls);
        await using (var verificationDb = new ApplicationDbContext(options))
        {
            var refund = await verificationDb.Refunds.SingleAsync();
            var payment = await verificationDb.Payments.SingleAsync(x => x.Id == paymentId);
            Assert.Equal(RefundStatus.Unknown, refund.Status);
            Assert.NotEmpty(refund.ProviderRefundId);
            Assert.Equal(now, refund.CreatedAt);
            Assert.Equal(now, refund.UpdatedAt);
            Assert.Equal(PaymentStatus.Succeeded, payment.Status);
            Assert.Equal(0m, payment.RefundedAmount);
        }

        await using var retryDb = new ApplicationDbContext(options);
        var retryOrchestrator = CreateOrchestrator(retryDb, provider, now);
        var sameRequest = await retryOrchestrator.RefundPaymentAsync(paymentId, 40m, "database-failure", CancellationToken.None);
        var differentRequest = await retryOrchestrator.RefundPaymentAsync(paymentId, 30m, "different-request", CancellationToken.None);

        Assert.True(sameRequest.IsSuccess, sameRequest.Error);
        Assert.Equal("Unknown", sameRequest.Value!.Status);
        Assert.False(differentRequest.IsSuccess);
        Assert.Contains("unfinished refund", differentRequest.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, provider.RefundCalls);
    }

    [Fact]
    public async Task Provider_Cancellation_Should_Persist_Unknown_Refund_And_Propagate_Cancellation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 4, 10, 45, 0, TimeSpan.Zero);
        var paymentId = await SeedRefundablePaymentAsync(db, now);
        using var cancellation = new CancellationTokenSource();
        var provider = new CancellingPaymentProvider(cancellation);
        var orchestrator = CreateOrchestrator(db, provider, now);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.RefundPaymentAsync(paymentId, 25m, "cancelled-request", cancellation.Token));

        Assert.Equal(1, provider.RefundCalls);
        await using var verificationDb = new ApplicationDbContext(options);
        var refund = await verificationDb.Refunds.SingleAsync();
        var payment = await verificationDb.Payments.SingleAsync(x => x.Id == paymentId);
        Assert.Equal(RefundStatus.Unknown, refund.Status);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(0m, payment.RefundedAmount);
    }

    [Fact]
    public async Task Provider_Exception_Should_Be_Redacted_In_Refund_Result()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 4, 11, 0, 0, TimeSpan.Zero);
        var paymentId = await SeedRefundablePaymentAsync(db, now);
        var provider = new TrackingPaymentProvider(
            () => throw new InvalidOperationException("secret=refund-private-token"));
        var orchestrator = CreateOrchestrator(db, provider, now);

        var result = await orchestrator.RefundPaymentAsync(paymentId, 40m, "redaction", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.DoesNotContain("refund-private-token", result.Error, StringComparison.Ordinal);
        Assert.Contains("REDACTED", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RefundStatus.Unknown, (await db.Refunds.SingleAsync()).Status);
    }

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, IPaymentProvider provider, DateTimeOffset now)
        => CreateOrchestrator(db, new TestPaymentProviderFactory(provider), now);

    private static PaymentOrchestrator CreateOrchestrator(ApplicationDbContext db, IPaymentProviderFactory providerFactory, DateTimeOffset now)
    {
        var clock = new FixedClock(now);
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        return new PaymentOrchestrator(db, providerFactory, Array.Empty<IPaymentWebhookVerifier>(), accounts, null!, clock);
    }

    private static async Task<Guid> SeedRefundablePaymentAsync(ApplicationDbContext db, DateTimeOffset now, PaymentProvider provider = PaymentProvider.YooKassa)
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"refund-{Guid.NewGuid():N}@example.test", DisplayName = "Refund User", PasswordHash = "hash" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Refund tariff", Slug = $"refund-{Guid.NewGuid():N}", Description = "Refund", DurationDays = 30, Price = 100m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = provider, Mode = PaymentProviderMode.Sandbox, Name = $"refund-{Guid.NewGuid():N}", PublicName = provider.ToString(), IsEnabled = true, ShopId = "shop", SecretKeyProtected = "secret" };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Amount = 100m, Currency = "RUB", Status = OrderStatus.Completed, ExpiresAt = now.AddMinutes(15), PaymentProvider = provider };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentProviderAccountId = account.Id,
            Provider = provider,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = $"payment-{Guid.NewGuid():N}",
            IdempotencyKey = $"payment-{Guid.NewGuid():N}",
            Amount = 100m,
            Currency = "RUB",
            Status = PaymentStatus.Succeeded,
            PaidAt = now
        };

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return payment.Id;
    }

    private sealed class FailingSaveApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        public bool FailNextSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new DbUpdateException("Injected local save failure.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class TrackingPaymentProvider(Action beforeReturn, PaymentProvider provider = PaymentProvider.YooKassa) : IPaymentProvider
    {
        public PaymentProvider Provider => provider;
        public int RefundCalls { get; private set; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        {
            RefundCalls++;
            beforeReturn();
            return Task.FromResult(new PaymentRefundResult($"refund-{payment.Id:N}", RefundStatus.Succeeded, "{\"status\":\"succeeded\"}"));
        }
    }

    private sealed class CancellingPaymentProvider(CancellationTokenSource cancellation) : IPaymentProvider
    {
        public PaymentProvider Provider => PaymentProvider.YooKassa;
        public int RefundCalls { get; private set; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        {
            RefundCalls++;
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not propagated.");
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => "***";
    }

    private sealed class TestPaymentProviderFactory(IPaymentProvider provider) : IPaymentProviderFactory
    {
        public IPaymentProvider Get(PaymentProvider _) => provider;
    }

    private sealed class TrackingPaymentProviderFactory(IPaymentProvider provider) : IPaymentProviderFactory
    {
        public int GetCalls { get; private set; }

        public IPaymentProvider Get(PaymentProvider _)
        {
            GetCalls++;
            return provider;
        }
    }

    public enum InvalidRefundAccountState
    {
        ProviderMismatch,
        AccountDisabled,
        ModeDisabled,
        ProviderModeMismatch,
        MissingProviderPaymentId,
        MissingShopId,
        MissingCredentials
    }
}
