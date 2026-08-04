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

public class PaymentInitializationResilienceTests
{
    [Fact]
    public async Task Reservation_Save_Failure_Should_Not_Call_Payment_Provider()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new TrackingPaymentProvider();
        var orchestrator = fixture.CreateOrchestrator(provider);
        fixture.Db.FailNextSaveCount = 1;

        var result = await orchestrator.InitPaymentAsync(fixture.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("provider was not called", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.InitCalls);
        await using var verificationDb = fixture.CreateVerificationContext();
        Assert.Empty(await verificationDb.Payments.ToListAsync());
    }

    [Fact]
    public async Task Final_Save_Failure_Should_Recover_Remote_Outcome_And_Return_Success()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var provider = new TrackingPaymentProvider(() => fixture.Db.FailNextSaveCount = 1);
        var orchestrator = fixture.CreateOrchestrator(provider);

        var result = await orchestrator.InitPaymentAsync(fixture.Command, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1, provider.InitCalls);
        await using var verificationDb = fixture.CreateVerificationContext();
        var payment = await verificationDb.Payments.SingleAsync();
        var order = await verificationDb.Orders.SingleAsync(x => x.Id == fixture.OrderId);
        Assert.Equal(provider.PaymentId, payment.ProviderPaymentId);
        Assert.Equal(provider.RedirectUrl, payment.ConfirmationUrl);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public async Task Repeated_Final_Save_Failure_Should_Keep_One_Reservation_And_Allow_Idempotent_Retry()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var injectFailure = true;
        var provider = new TrackingPaymentProvider(() =>
        {
            if (injectFailure)
            {
                injectFailure = false;
                fixture.Db.FailNextSaveCount = 2;
            }
        });
        var orchestrator = fixture.CreateOrchestrator(provider);

        var first = await orchestrator.InitPaymentAsync(fixture.Command, CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.Contains("local outcome", first.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, provider.InitCalls);
        await using (var firstVerificationDb = fixture.CreateVerificationContext())
        {
            var reservation = await firstVerificationDb.Payments.SingleAsync();
            Assert.Equal(PaymentStatus.New, reservation.Status);
            Assert.StartsWith("local_", reservation.ProviderPaymentId, StringComparison.Ordinal);
            Assert.Empty(reservation.ConfirmationUrl);
        }

        await using var retryDb = fixture.CreateVerificationContext();
        var retryOrchestrator = fixture.CreateOrchestrator(provider, retryDb);
        var retry = await retryOrchestrator.InitPaymentAsync(fixture.Command, CancellationToken.None);

        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(provider.PaymentId, retry.Value!.PaymentId);
        Assert.Equal(2, provider.InitCalls);
        Assert.Equal(1, await retryDb.Payments.CountAsync());
        var payment = await retryDb.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(provider.PaymentId, payment.ProviderPaymentId);
    }

    [Theory]
    [InlineData(OrderStatus.FulfillmentInProgress)]
    [InlineData(OrderStatus.PartiallyProcessed)]
    public async Task Paid_Intermediate_Order_Should_Not_Create_Another_Checkout(OrderStatus status)
    {
        await using var fixture = await PaymentFixture.CreateAsync(status, paidAt: new DateTimeOffset(2026, 8, 4, 4, 0, 0, TimeSpan.Zero));
        var provider = new TrackingPaymentProvider();
        var orchestrator = fixture.CreateOrchestrator(provider);

        var result = await orchestrator.InitPaymentAsync(fixture.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already paid", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.InitCalls);
        await using var verificationDb = fixture.CreateVerificationContext();
        Assert.Empty(await verificationDb.Payments.ToListAsync());
    }

    [Fact]
    public async Task Provider_Cancellation_Should_Keep_Reservation_And_Propagate_Cancellation()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        var provider = new CancellingPaymentProvider(cancellation);
        var orchestrator = fixture.CreateOrchestrator(provider);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            orchestrator.InitPaymentAsync(fixture.Command, cancellation.Token));

        Assert.Equal(1, provider.InitCalls);
        await using var verificationDb = fixture.CreateVerificationContext();
        var payment = await verificationDb.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.New, payment.Status);
        Assert.StartsWith("local_", payment.ProviderPaymentId, StringComparison.Ordinal);
        Assert.Empty(payment.ConfirmationUrl);
    }

    private sealed class PaymentFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly DateTimeOffset _now;

        private PaymentFixture(SqliteConnection connection, DbContextOptions<ApplicationDbContext> options, FailingSaveApplicationDbContext db, Guid orderId, DateTimeOffset now)
        {
            _connection = connection;
            _options = options;
            Db = db;
            OrderId = orderId;
            _now = now;
        }

        public FailingSaveApplicationDbContext Db { get; }
        public Guid OrderId { get; }
        public PaymentInitCommand Command => new(OrderId, PaymentProvider.YooKassa, "https://example.test/return");

        public static async Task<PaymentFixture> CreateAsync(OrderStatus status = OrderStatus.PendingPayment, DateTimeOffset? paidAt = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var db = new FailingSaveApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();
            var now = new DateTimeOffset(2026, 8, 4, 11, 30, 0, TimeSpan.Zero);
            var orderId = await SeedOrderAsync(db, now, status, paidAt);
            return new PaymentFixture(connection, options, db, orderId, now);
        }

        public PaymentOrchestrator CreateOrchestrator(IPaymentProvider provider, ApplicationDbContext? db = null)
        {
            db ??= Db;
            var clock = new FixedClock(_now);
            var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
            return new PaymentOrchestrator(db, new TestPaymentProviderFactory(provider), Array.Empty<IPaymentWebhookVerifier>(), accounts, null!, clock);
        }

        public ApplicationDbContext CreateVerificationContext() => new(_options);

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private static async Task<Guid> SeedOrderAsync(ApplicationDbContext db, DateTimeOffset now, OrderStatus status, DateTimeOffset? paidAt)
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"init-{Guid.NewGuid():N}@example.test", DisplayName = "Init User", PasswordHash = "hash", ReferralCode = $"init-{Guid.NewGuid():N}" };
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Init tariff", Slug = $"init-{Guid.NewGuid():N}", Description = "Init", DurationDays = 30, Price = 100m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var account = new PaymentProviderAccount { Id = Guid.NewGuid(), Provider = PaymentProvider.YooKassa, Mode = PaymentProviderMode.Sandbox, Name = $"init-{Guid.NewGuid():N}", PublicName = "YooKassa", IsEnabled = true, IsDefault = true, ShopId = "shop", SecretKeyProtected = "secret", ReturnUrl = "https://example.test/return" };
        var order = new Order { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Amount = 100m, Currency = "RUB", Status = status, PaidAt = paidAt, ExpiresAt = now.AddMinutes(15), PaymentProvider = PaymentProvider.YooKassa };

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    private sealed class FailingSaveApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        public int FailNextSaveCount { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailNextSaveCount > 0)
            {
                FailNextSaveCount--;
                throw new DbUpdateException("Injected payment initialization save failure.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class TrackingPaymentProvider(Action? beforeReturn = null) : IPaymentProvider
    {
        public PaymentProvider Provider => PaymentProvider.YooKassa;
        public int InitCalls { get; private set; }
        public string PaymentId { get; } = $"provider-{Guid.NewGuid():N}";
        public string RedirectUrl { get; } = "https://provider.example.test/checkout";

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
        {
            InitCalls++;
            beforeReturn?.Invoke();
            return Task.FromResult(new PaymentInitResult(PaymentId, RedirectUrl, "{\"status\":\"pending\"}"));
        }

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class CancellingPaymentProvider(CancellationTokenSource cancellation) : IPaymentProvider
    {
        public PaymentProvider Provider => PaymentProvider.YooKassa;
        public int InitCalls { get; private set; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
        {
            InitCalls++;
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not propagated.");
        }

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
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
}
