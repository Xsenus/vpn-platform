using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class OrderServiceSqliteTests
{
    [Fact]
    public async Task ExpirePendingOrders_Should_Work_With_Sqlite()
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

        var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "sqlite-order@test.local", DisplayName = "SQLite user", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "SQLite", Slug = "sqlite", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        db.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Amount = 100, Currency = "RUB", Status = OrderStatus.PendingPayment, ExpiresAt = now.AddMinutes(-1) },
            new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Amount = 100, Currency = "RUB", Status = OrderStatus.Completed, ExpiresAt = now.AddMinutes(-1) },
            new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Amount = 100, Currency = "RUB", Status = OrderStatus.PendingPayment, ExpiresAt = now.AddMinutes(5) });
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var service = new OrderService(db, new FixedClock(now));

        var expired = await service.ExpirePendingOrdersAsync();

        Assert.Equal(1, expired);
        Assert.Equal(1, await db.Orders.CountAsync(x => x.Status == OrderStatus.Expired));
        Assert.Equal(1, await db.Orders.CountAsync(x => x.Status == OrderStatus.Completed));
        Assert.Equal(1, await db.Orders.CountAsync(x => x.Status == OrderStatus.PendingPayment));
        Assert.Contains(interceptor.Commands, command =>
            command.StartsWith("UPDATE \"Orders\"", StringComparison.OrdinalIgnoreCase)
            && command.Contains("\"Status\"", StringComparison.OrdinalIgnoreCase)
            && command.Contains("julianday(\"ExpiresAt\")", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Reuse_Renewal_Order_Only_For_Same_Subscription()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var firstSubscriptionId = Guid.NewGuid();
        var secondSubscriptionId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "sqlite-renewal@test.local", DisplayName = "SQLite renewal user", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "SQLite Renewal", Slug = "sqlite-renewal", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        db.Subscriptions.AddRange(
            Subscription(firstSubscriptionId, userId, tariffId, now),
            Subscription(secondSubscriptionId, userId, tariffId, now));
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FixedClock(now));

        var first = await service.CreateOrderAsync(new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: firstSubscriptionId));
        var duplicateFirst = await service.CreateOrderAsync(new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: firstSubscriptionId));
        var second = await service.CreateOrderAsync(new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: secondSubscriptionId));

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(duplicateFirst.IsSuccess, duplicateFirst.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal(first.Value!.Id, duplicateFirst.Value!.Id);
        Assert.NotEqual(first.Value.Id, second.Value!.Id);
        Assert.Equal(firstSubscriptionId, first.Value.LinkedSubscriptionId);
        Assert.Equal(secondSubscriptionId, second.Value.LinkedSubscriptionId);
        Assert.Equal(2, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Validate_Renewal_Subscription_At_Service_Boundary()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var otherTariffId = Guid.NewGuid();
        var activeId = Guid.NewGuid();
        var foreignId = Guid.NewGuid();
        var cancelledId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var mismatchedId = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = userId, Email = "renewal-owner@test.local", DisplayName = "Owner", PasswordHash = "hash", ReferralCode = "owner-renewal" },
            new User { Id = otherUserId, Email = "renewal-other@test.local", DisplayName = "Other", PasswordHash = "hash", ReferralCode = "other-renewal" });
        db.Tariffs.AddRange(
            new Tariff { Id = tariffId, Name = "Primary", Slug = "renewal-primary", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true },
            new Tariff { Id = otherTariffId, Name = "Other", Slug = "renewal-other", DurationDays = 30, Price = 200, Currency = "RUB", IsActive = true });
        db.Subscriptions.AddRange(
            Subscription(activeId, userId, tariffId, now),
            Subscription(foreignId, otherUserId, tariffId, now),
            Subscription(cancelledId, userId, tariffId, now, SubscriptionStatus.Cancelled),
            Subscription(blockedId, userId, tariffId, now, SubscriptionStatus.Blocked),
            Subscription(mismatchedId, userId, otherTariffId, now));
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FixedClock(now));
        var commands = new[]
        {
            new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false),
            new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: Guid.NewGuid()),
            new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: foreignId),
            new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: cancelledId),
            new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: blockedId),
            new CreateOrderCommand(userId, tariffId, OrderType.Renewal, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: mismatchedId),
            new CreateOrderCommand(userId, tariffId, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false, RenewalSubscriptionId: activeId)
        };

        foreach (var command in commands)
        {
            var result = await service.CreateOrderAsync(command);
            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        Assert.Empty(await db.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Order_Service_Should_Reject_Undefined_Enum_Inputs_Without_Mutation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 14, 6, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "invalid-order-enum@test.local", DisplayName = "Invalid enum", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Enum", Slug = "invalid-order-enum", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        await db.SaveChangesAsync();
        var service = new OrderService(db, new FixedClock(now));
        var valid = new CreateOrderCommand(userId, tariffId, OrderType.NewSubscription, ChannelType.Telegram, PaymentProvider.YooKassa, null, false);

        var invalidType = await service.CreateOrderAsync(valid with { Type = (OrderType)999 });
        var invalidChannel = await service.CreateOrderAsync(valid with { Channel = (ChannelType)999 });
        var invalidProvider = await service.CreateOrderAsync(valid with { PaymentProvider = (PaymentProvider)999 });

        Assert.False(invalidType.IsSuccess);
        Assert.Contains("type", invalidType.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(invalidChannel.IsSuccess);
        Assert.Contains("channel", invalidChannel.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(invalidProvider.IsSuccess);
        Assert.Contains("provider", invalidProvider.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.Orders.AsNoTracking().ToListAsync());

        var created = await service.CreateOrderAsync(valid);
        Assert.True(created.IsSuccess, created.Error);
        var selection = await service.SelectPaymentProviderAsync(created.Value!.Id, userId, (PaymentProvider)999);
        var promo = await service.ValidatePromoForCheckoutAsync(null, tariffId, (ChannelType)999);

        Assert.False(selection.IsSuccess);
        Assert.Contains("provider", selection.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(promo.IsSuccess);
        Assert.Contains("channel", promo.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PaymentProvider.YooKassa, (await db.Orders.SingleAsync()).PaymentProvider);
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Expire_Previous_Intent_Before_Creating_Replacement()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var clock = new MutableClock(new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero));
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "expired-intent@test.local", DisplayName = "Expired intent", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Intent", Slug = "expired-intent", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        await db.SaveChangesAsync();

        var service = new OrderService(db, clock);
        var command = new CreateOrderCommand(userId, tariffId, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false);
        var first = await service.CreateOrderAsync(command);
        clock.UtcNow = clock.UtcNow.AddMinutes(16);
        var second = await service.CreateOrderAsync(command);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.NotEqual(first.Value!.Id, second.Value!.Id);
        db.ChangeTracker.Clear();
        Assert.Equal(OrderStatus.Expired, (await db.Orders.FindAsync(first.Value.Id))!.Status);
        Assert.Equal(1, await db.Orders.CountAsync(x => x.Status == OrderStatus.PendingPayment));
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Return_Concurrent_Pending_Order_Winner()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new CompetingOrderDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "concurrent-intent@test.local", DisplayName = "Concurrent intent", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Concurrent", Slug = "concurrent-intent", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        await db.SaveChangesAsync();

        Guid winnerId = Guid.Empty;
        db.BeforeOrderSave = async (candidate, cancellationToken) =>
        {
            await using var competitor = new ApplicationDbContext(options);
            var winner = new Order
            {
                UserId = candidate.UserId,
                TariffId = candidate.TariffId,
                Type = candidate.Type,
                Channel = candidate.Channel,
                PaymentProvider = candidate.PaymentProvider,
                Status = candidate.Status,
                Amount = candidate.Amount,
                Currency = candidate.Currency,
                ReferralContext = candidate.ReferralContext,
                ExpiresAt = candidate.ExpiresAt,
                PendingIntentKey = candidate.PendingIntentKey
            };
            competitor.Orders.Add(winner);
            await competitor.SaveChangesAsync(cancellationToken);
            winnerId = winner.Id;
        };

        var result = await new OrderService(db, new FixedClock(now)).CreateOrderAsync(
            new CreateOrderCommand(userId, tariffId, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(winnerId, result.Value!.Id);
        Assert.Equal(1, await db.Orders.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_Should_Derive_First_Purchase_Without_Trusting_Command()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 10, 30, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var firstTariffId = Guid.NewGuid();
        var secondTariffId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "first-purchase@test.local", DisplayName = "First purchase", PasswordHash = "hash" });
        db.Tariffs.AddRange(
            new Tariff { Id = firstTariffId, Name = "First", Slug = "first-purchase-one", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true },
            new Tariff { Id = secondTariffId, Name = "Second", Slug = "first-purchase-two", DurationDays = 30, Price = 200, Currency = "RUB", IsActive = true });
        await db.SaveChangesAsync();
        var service = new OrderService(db, new FixedClock(now));

        var first = await service.CreateOrderAsync(new CreateOrderCommand(
            userId, firstTariffId, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false));
        Assert.True(first.IsSuccess, first.Error);
        var firstOrder = await db.Orders.SingleAsync(x => x.Id == first.Value!.Id);
        Assert.True(firstOrder.IsFirstPurchase);
        firstOrder.Status = OrderStatus.Completed;
        await db.SaveChangesAsync();

        var second = await service.CreateOrderAsync(new CreateOrderCommand(
            userId, secondTariffId, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, true));

        Assert.True(second.IsSuccess, second.Error);
        Assert.False((await db.Orders.SingleAsync(x => x.Id == second.Value!.Id)).IsFirstPurchase);
    }

    [Fact]
    public async Task SelectPaymentProviderAsync_Should_Lock_Snapshot_After_First_Attempt()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 12, 5, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "provider-snapshot@test.local", DisplayName = "Provider snapshot", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Snapshot", Slug = "provider-snapshot", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        await db.SaveChangesAsync();
        var service = new OrderService(db, new FixedClock(now));
        var created = await service.CreateOrderAsync(new CreateOrderCommand(userId, tariffId, OrderType.NewSubscription, ChannelType.Telegram, PaymentProvider.YooKassa, null, false));
        Assert.True(created.IsSuccess, created.Error);

        var selected = await service.SelectPaymentProviderAsync(created.Value!.Id, userId, PaymentProvider.Stripe);
        Assert.True(selected.IsSuccess, selected.Error);
        Assert.Equal(PaymentProvider.Stripe, selected.Value!.PaymentProvider);

        db.Payments.Add(new PaymentAttempt
        {
            OrderId = created.Value.Id,
            Provider = PaymentProvider.Stripe,
            ProviderPaymentId = "provider-locked",
            IdempotencyKey = "provider-locked",
            Amount = 100,
            Currency = "RUB",
            Status = PaymentStatus.New,
            RawRequest = "{}",
            RawResponse = "{}"
        });
        await db.SaveChangesAsync();

        var changed = await service.SelectPaymentProviderAsync(created.Value.Id, userId, PaymentProvider.YooKassa);
        Assert.False(changed.IsSuccess);
        Assert.Contains("locked", changed.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PaymentProvider.Stripe, (await db.Orders.SingleAsync(x => x.Id == created.Value.Id)).PaymentProvider);
    }

    [Fact]
    public async Task SelectPaymentProviderAsync_Should_Not_Change_Web_Order_Snapshot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 12, 5, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "web-provider-snapshot@test.local", DisplayName = "Web snapshot", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Web snapshot", Slug = "web-provider-snapshot", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        await db.SaveChangesAsync();
        var service = new OrderService(db, new FixedClock(now));
        var created = await service.CreateOrderAsync(new CreateOrderCommand(userId, tariffId, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false));
        Assert.True(created.IsSuccess, created.Error);

        var changed = await service.SelectPaymentProviderAsync(created.Value!.Id, userId, PaymentProvider.Stripe);

        Assert.False(changed.IsSuccess);
        Assert.Contains("Telegram", changed.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PaymentProvider.YooKassa, (await db.Orders.SingleAsync(x => x.Id == created.Value.Id)).PaymentProvider);
    }

    [Fact]
    public async Task ProcessSubscriptionLifecycle_Should_Work_With_Sqlite()
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

        var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "sqlite-subscription@test.local", DisplayName = "SQLite user", PasswordHash = "hash" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "SQLite", Slug = "sqlite-subscription", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        db.Subscriptions.AddRange(
            new Subscription { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = now.AddDays(-31), EndAt = now.AddMinutes(-1), GracePeriodEndAt = now.AddDays(3), SourceChannel = ChannelType.Web },
            new Subscription { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.GracePeriod, StartAt = now.AddDays(-40), EndAt = now.AddDays(-4), GracePeriodEndAt = now.AddMinutes(-1), SourceChannel = ChannelType.Web },
            new Subscription { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = now.AddDays(-1), EndAt = now.AddDays(1), GracePeriodEndAt = now.AddDays(4), SourceChannel = ChannelType.Web });
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new TestVpnProviderFactory());

        var processed = await service.ProcessLifecycleAsync();

        Assert.Equal(2, processed);
        Assert.Equal(1, await db.Subscriptions.CountAsync(x => x.Status == SubscriptionStatus.GracePeriod));
        Assert.Equal(1, await db.Subscriptions.CountAsync(x => x.Status == SubscriptionStatus.Expired));
        Assert.Equal(1, await db.Subscriptions.CountAsync(x => x.Status == SubscriptionStatus.Active));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("FROM \"Subscriptions\"", StringComparison.OrdinalIgnoreCase)
            && command.Contains("julianday(\"EndAt\")", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 200", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("FROM \"Subscriptions\"", StringComparison.OrdinalIgnoreCase)
            && command.Contains("julianday(\"GracePeriodEndAt\")", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 200", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

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

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class MutableClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private sealed class CompetingOrderDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        private bool _competitorInvoked;
        public Func<Order, CancellationToken, Task>? BeforeOrderSave { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var addedOrder = ChangeTracker.Entries<Order>().FirstOrDefault(x => x.State == EntityState.Added)?.Entity;
            if (!_competitorInvoked && addedOrder is not null && BeforeOrderSave is not null)
            {
                _competitorInvoked = true;
                await BeforeOrderSave(addedOrder, cancellationToken);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }

    private static Subscription Subscription(
        Guid id,
        Guid userId,
        Guid tariffId,
        DateTimeOffset now,
        SubscriptionStatus status = SubscriptionStatus.Active)
        => new()
        {
            Id = id,
            UserId = userId,
            TariffId = tariffId,
            Status = status,
            StartAt = now.AddDays(-10),
            EndAt = now.AddDays(20),
            SourceChannel = ChannelType.Web
        };

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider = new TestVpnProvider();
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TestVpnProvider : IVpnProvider
    {
        public string Name => "test";
        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult("test-access", "vless://test", "qr", "config"));

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => CreateAccessAsync(request, cancellationToken);

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken)
            => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));

        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken)
            => Task.FromResult(HealthStatus.Healthy);
    }
}
