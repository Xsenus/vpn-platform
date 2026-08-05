using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Me;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PromoCodeLifecycleTests
{
    [Fact]
    public async Task Valid_Promo_Should_Apply_Discount_Persist_Identity_And_Respect_Global_Limit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 16, 0, 0, TimeSpan.Zero);
        var (tariff, firstUser) = await SeedBaseAsync(db, now);
        var secondUser = User("promo-second@example.test");
        var promo = Promo(now, "Summer10");
        promo.DiscountValue = 10;
        promo.FreeDays = 7;
        promo.MaxRedemptions = 1;
        promo.MaxPerUser = 1;
        promo.AllowedTariffIdsJson = JsonSerializer.Serialize(new[] { tariff.Id });
        promo.AllowedChannelsJson = JsonSerializer.Serialize(new[] { "Web" });
        db.Users.Add(secondUser);
        db.PromoCodes.Add(promo);
        await db.SaveChangesAsync();
        var service = new OrderService(db, new FixedClock(now));
        var command = new CreateOrderCommand(
            firstUser.Id,
            tariff.Id,
            OrderType.NewSubscription,
            ChannelType.Web,
            PaymentProvider.YooKassa,
            "  summer10  ",
            false);

        var first = await service.CreateOrderAsync(command);
        var repeated = await service.CreateOrderAsync(command);
        var exhausted = await service.CreateOrderAsync(command with { UserId = secondUser.Id });

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(repeated.IsSuccess, repeated.Error);
        Assert.Equal(first.Value!.Id, repeated.Value!.Id);
        Assert.Equal(90m, first.Value.Amount);
        Assert.False(exhausted.IsSuccess);
        Assert.Contains("limit", exhausted.Error, StringComparison.OrdinalIgnoreCase);
        var order = await db.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(promo.Id, order.PromoCodeId);
        Assert.Equal(7, OrderService.GetPromoFreeDays(order));
        Assert.NotNull(order.PendingIntentKey);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("inactive")]
    [InlineData("future")]
    [InlineData("expired")]
    [InlineData("tariff")]
    [InlineData("channel")]
    [InlineData("malformed-json")]
    [InlineData("malformed-after-match")]
    [InlineData("invalid-discount")]
    public async Task Checkout_Should_Reject_Invalid_Promo_Before_Persisting_Session(string scenario)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 16, 15, 0, TimeSpan.Zero);
        var (tariff, _) = await SeedBaseAsync(db, now);
        var promo = Promo(now, "Guarded");
        var requestedCode = promo.Code;
        switch (scenario)
        {
            case "unknown":
                requestedCode = "missing";
                break;
            case "inactive":
                promo.IsActive = false;
                break;
            case "future":
                promo.StartsAt = now.AddMinutes(1);
                break;
            case "expired":
                promo.EndsAt = now;
                break;
            case "tariff":
                promo.AllowedTariffIdsJson = JsonSerializer.Serialize(new[] { Guid.NewGuid() });
                break;
            case "channel":
                promo.AllowedChannelsJson = JsonSerializer.Serialize(new[] { "Telegram" });
                break;
            case "malformed-json":
                promo.AllowedTariffIdsJson = "{";
                break;
            case "malformed-after-match":
                promo.AllowedTariffIdsJson = $"[\"{tariff.Id:D}\",\"invalid\"]";
                break;
            case "invalid-discount":
                promo.DiscountType = "mystery";
                break;
        }

        db.PromoCodes.Add(promo);
        await db.SaveChangesAsync();
        var orderService = new OrderService(db, new FixedClock(now));
        var checkout = new CheckoutSessionService(db, new FixedClock(now), orderService);

        var result = await checkout.CreateAsync(new(
            tariff.Id,
            OrderType.NewSubscription,
            ChannelType.Web,
            PaymentProvider.YooKassa,
            requestedCode,
            false,
            null,
            null));

        Assert.False(result.IsSuccess);
        Assert.Empty(await db.CheckoutSessions.AsNoTracking().ToListAsync());
        Assert.Empty(await db.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Promo_Should_Enforce_Per_User_Limit_Across_Tariffs()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 16, 30, 0, TimeSpan.Zero);
        var (firstTariff, firstUser) = await SeedBaseAsync(db, now);
        var secondTariff = Tariff("promo-second-tariff", 200m);
        var secondUser = User("promo-other@example.test");
        var promo = Promo(now, "OnceEach");
        promo.MaxRedemptions = 10;
        promo.MaxPerUser = 1;
        db.AddRange(secondTariff, secondUser, promo);
        await db.SaveChangesAsync();
        var service = new OrderService(db, new FixedClock(now));

        var first = await service.CreateOrderAsync(Command(firstUser.Id, firstTariff.Id, promo.Code));
        var repeatedUser = await service.CreateOrderAsync(Command(firstUser.Id, secondTariff.Id, promo.Code));
        var otherUser = await service.CreateOrderAsync(Command(secondUser.Id, secondTariff.Id, promo.Code));

        Assert.True(first.IsSuccess, first.Error);
        Assert.False(repeatedUser.IsSuccess);
        Assert.Contains("account", repeatedUser.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True(otherUser.IsSuccess, otherUser.Error);
        Assert.Equal(2, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task Stale_Pending_Order_Should_Not_Consume_Promo_Limit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 16, 40, 0, TimeSpan.Zero);
        var (tariff, currentUser) = await SeedBaseAsync(db, now);
        var staleUser = User("promo-stale@example.test");
        var promo = Promo(now, "ReleasedSlot");
        promo.MaxRedemptions = 1;
        db.AddRange(staleUser, promo);
        var staleOrder = new Order
        {
            UserId = staleUser.Id,
            TariffId = tariff.Id,
            Type = OrderType.NewSubscription,
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            PromoCodeId = promo.Id,
            Status = OrderStatus.PendingPayment,
            Amount = 95m,
            Currency = "RUB",
            ExpiresAt = now.AddMinutes(-1),
            PendingIntentKey = Guid.NewGuid().ToString("N")
        };
        db.Orders.Add(staleOrder);
        await db.SaveChangesAsync();

        var result = await new OrderService(db, new FixedClock(now)).CreateOrderAsync(
            Command(currentUser.Id, tariff.Id, promo.Code));

        Assert.True(result.IsSuccess, result.Error);
        db.ChangeTracker.Clear();
        Assert.Equal(OrderStatus.Expired, (await db.Orders.SingleAsync(x => x.Id == staleOrder.Id)).Status);
        Assert.Equal(1, await db.Orders.CountAsync(x => x.Status == OrderStatus.PendingPayment));
    }

    [Fact]
    public async Task Failed_Stale_Expiration_Should_Not_Release_Promo_Limit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var now = new DateTimeOffset(2026, 8, 5, 16, 42, 0, TimeSpan.Zero);
        Guid tariffId;
        Guid currentUserId;
        Guid staleOrderId;
        await using (var seedDb = CreateDb(connection))
        {
            await seedDb.Database.EnsureCreatedAsync();
            var (tariff, currentUser) = await SeedBaseAsync(seedDb, now);
            var staleUser = User("promo-stale-race@example.test");
            var promo = Promo(now, "ContendedSlot");
            promo.MaxRedemptions = 1;
            var staleOrder = new Order
            {
                UserId = staleUser.Id,
                TariffId = tariff.Id,
                Type = OrderType.NewSubscription,
                Channel = ChannelType.Web,
                PaymentProvider = PaymentProvider.YooKassa,
                PromoCodeId = promo.Id,
                Status = OrderStatus.PendingPayment,
                Amount = 95m,
                Currency = "RUB",
                ExpiresAt = now.AddMinutes(-1),
                PendingIntentKey = Guid.NewGuid().ToString("N")
            };
            seedDb.AddRange(staleUser, promo, staleOrder);
            await seedDb.SaveChangesAsync();
            tariffId = tariff.Id;
            currentUserId = currentUser.Id;
            staleOrderId = staleOrder.Id;
        }

        await using var db = CreateDb(connection, new SuppressOrderExpirationInterceptor());
        var result = await new OrderService(db, new FixedClock(now)).CreateOrderAsync(
            Command(currentUserId, tariffId, "ContendedSlot"));

        Assert.False(result.IsSuccess);
        Assert.Contains("limit", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(OrderStatus.PendingPayment, (await db.Orders.AsNoTracking().SingleAsync(x => x.Id == staleOrderId)).Status);
        Assert.Single(await db.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Concurrent_Final_Promo_Redemption_Should_Return_Conflict_Without_Second_Order()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-promo-{Guid.NewGuid():N}.db");
        try
        {
            var now = new DateTimeOffset(2026, 8, 5, 16, 45, 0, TimeSpan.Zero);
            Guid tariffId;
            Guid promoId;
            Guid winnerUserId;
            Guid loserUserId;
            await using (var seedDb = CreateDb(databasePath))
            {
                await seedDb.Database.EnsureCreatedAsync();
                var (tariff, winner) = await SeedBaseAsync(seedDb, now);
                var loser = User("promo-race-loser@example.test");
                var promo = Promo(now, "LastOne");
                promo.MaxRedemptions = 1;
                promo.MaxPerUser = 1;
                seedDb.AddRange(loser, promo);
                await seedDb.SaveChangesAsync();
                tariffId = tariff.Id;
                promoId = promo.Id;
                winnerUserId = winner.Id;
                loserUserId = loser.Id;
            }

            var interceptor = new RedeemBeforeTransactionInterceptor(databasePath, tariffId, "LastOne", winnerUserId, now);
            await using (var loserDb = CreateDb(databasePath, interceptor))
            {
                var clock = new FixedClock(now);
                var orderService = new OrderService(loserDb, clock);
                var controller = new MeController(
                    loserDb,
                    orderService,
                    new CheckoutSessionService(loserDb, clock, orderService),
                    paymentOrchestrator: null!,
                    telegramBotService: null!,
                    qrCodeGenerator: null!,
                    configuration: null!);
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, loserUserId.ToString()) },
                            "test"))
                    }
                };

                var result = await controller.CreateOrder(new CreateMeOrderHttpRequest(
                    tariffId,
                    "NewSubscription",
                    "YooKassa",
                    "LastOne",
                    null), CancellationToken.None);

                Assert.True(interceptor.WinnerResult?.IsSuccess, interceptor.WinnerResult?.Error);
                Assert.IsType<ConflictObjectResult>(result);
            }

            await using var assertDb = CreateDb(databasePath);
            var order = await assertDb.Orders.AsNoTracking().SingleAsync();
            Assert.Equal(winnerUserId, order.UserId);
            Assert.Equal(promoId, order.PromoCodeId);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static CreateOrderCommand Command(Guid userId, Guid tariffId, string promoCode)
        => new(userId, tariffId, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, promoCode, false);

    private static async Task<(Tariff Tariff, User User)> SeedBaseAsync(ApplicationDbContext db, DateTimeOffset now)
    {
        var tariff = Tariff($"promo-{Guid.NewGuid():N}", 100m);
        var user = User($"promo-{Guid.NewGuid():N}@example.test");
        tariff.CreatedAt = tariff.UpdatedAt = now;
        user.CreatedAt = user.UpdatedAt = now;
        db.AddRange(tariff, user);
        await db.SaveChangesAsync();
        return (tariff, user);
    }

    private static Tariff Tariff(string slug, decimal price)
        => new()
        {
            Name = slug,
            Slug = slug,
            Description = slug,
            DurationDays = 30,
            Price = price,
            Currency = "RUB",
            MaxDevices = 3,
            IsActive = true
        };

    private static User User(string email)
        => new()
        {
            Email = email,
            DisplayName = email,
            PasswordHash = "hash",
            ReferralCode = Guid.NewGuid().ToString("N")[..8]
        };

    private static PromoCode Promo(DateTimeOffset now, string code)
        => new()
        {
            Code = code,
            DiscountType = "percent",
            DiscountValue = 5,
            FreeDays = 0,
            IsActive = true,
            StartsAt = now.AddDays(-1),
            EndsAt = now.AddDays(1),
            AllowedTariffIdsJson = "[]",
            AllowedChannelsJson = "[]",
            CreatedAt = now,
            UpdatedAt = now
        };

    private static ApplicationDbContext CreateDb(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private static ApplicationDbContext CreateDb(SqliteConnection connection, IInterceptor interceptor)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options);

    private static ApplicationDbContext CreateDb(string databasePath)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Default Timeout=10;Pooling=False")
            .Options);

    private static ApplicationDbContext CreateDb(string databasePath, IInterceptor interceptor)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Default Timeout=10;Pooling=False")
            .AddInterceptors(interceptor)
            .Options);

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class RedeemBeforeTransactionInterceptor(
        string databasePath,
        Guid tariffId,
        string promoCode,
        Guid winnerUserId,
        DateTimeOffset now) : DbTransactionInterceptor
    {
        private int _intercepted;
        public Result<OrderDto>? WinnerResult { get; private set; }

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _intercepted, 1) == 0)
            {
                await using var winnerDb = CreateDb(databasePath);
                WinnerResult = await new OrderService(winnerDb, new FixedClock(now)).CreateOrderAsync(
                    Command(winnerUserId, tariffId, promoCode),
                    cancellationToken);
            }

            return result;
        }
    }

    private sealed class SuppressOrderExpirationInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => command.CommandText.Contains("UPDATE \"Orders\"", StringComparison.Ordinal)
                ? ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0))
                : base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }
}
