using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Public;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class CheckoutSessionTests
{
    [Fact]
    public async Task Public_Checkout_Controller_Should_Cover_Create_Get_Claimed_Status_And_Legacy_Gone_Response()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var tariff = await SeedTariffAsync(db);
        var user = await SeedUserAsync(db, "controller-buyer@example.test");
        var orderService = new OrderService(db, clock);
        var checkoutService = new CheckoutSessionService(db, clock, orderService);
        var controller = new OrdersController(checkoutService, orderService);

        var createdResult = await controller.CreateCheckoutSession(
            new CreateCheckoutSessionHttpRequest(tariff.Id, "NewSubscription", "Web", "YooKassa", null, true, user.Email, "https://example.test/return"),
            CancellationToken.None);
        var created = Assert.IsType<CheckoutSessionDto>(Assert.IsType<OkObjectResult>(createdResult).Value);

        var getResult = await controller.GetCheckoutSession(created.Token, CancellationToken.None);
        Assert.IsType<CheckoutSessionDto>(Assert.IsType<OkObjectResult>(getResult).Value);

        var claimed = await checkoutService.ClaimAsync(new ClaimCheckoutSessionCommand(created.Token, user.Id));
        Assert.True(claimed.IsSuccess, claimed.Error);
        var statusResult = await controller.GetStatus(claimed.Value!.Id, CancellationToken.None);
        Assert.IsType<OkObjectResult>(statusResult);

        var legacyResult = Assert.IsType<ObjectResult>(controller.CreateAnonymousOrder());
        Assert.Equal(StatusCodes.Status410Gone, legacyResult.StatusCode);
    }

    [Theory]
    [InlineData("invalid", "Web", "YooKassa")]
    [InlineData("NewSubscription", "invalid", "YooKassa")]
    [InlineData("NewSubscription", "Web", "invalid")]
    [InlineData("999", "Web", "YooKassa")]
    public async Task Public_Checkout_Controller_Should_Return_BadRequest_For_Invalid_Enum_Values(string type, string channel, string provider)
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var tariff = await SeedTariffAsync(db);
        var orderService = new OrderService(db, clock);
        var controller = new OrdersController(new CheckoutSessionService(db, clock, orderService), orderService);

        var result = await controller.CreateCheckoutSession(
            new CreateCheckoutSessionHttpRequest(tariff.Id, type, channel, provider, null, false, null, null),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.CheckoutSessions.ToListAsync());
    }

    [Fact]
    public async Task CheckoutSession_Should_Store_Only_Token_Hash_And_Create_User_Bound_Order_On_Claim()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var tariff = await SeedTariffAsync(db);
        var user = await SeedUserAsync(db, "buyer@example.test");
        var service = CreateService(db, clock);

        var created = await service.CreateAsync(new(tariff.Id, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false, null, "https://example.test/return"));
        Assert.True(created.IsSuccess, created.Error);

        var session = await db.CheckoutSessions.SingleAsync();
        Assert.NotEqual(created.Value!.Token, session.TokenHash);
        Assert.Equal(64, session.TokenHash.Length);

        var claimed = await service.ClaimAsync(new(created.Value.Token, user.Id));
        Assert.True(claimed.IsSuccess, claimed.Error);
        Assert.Equal(user.Id, claimed.Value!.UserId);
        Assert.Equal(user.Id, (await db.CheckoutSessions.SingleAsync()).UserId);
        Assert.Equal(user.Id, (await db.Orders.SingleAsync()).UserId);
    }

    [Fact]
    public async Task CheckoutSession_Should_Not_Create_Duplicate_Order_When_Claimed_Again_By_Same_User()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var tariff = await SeedTariffAsync(db);
        var user = await SeedUserAsync(db, "buyer@example.test");
        var service = CreateService(db, clock);

        var created = await service.CreateAsync(new(tariff.Id, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false, null, null));
        Assert.True(created.IsSuccess, created.Error);

        var first = await service.ClaimAsync(new(created.Value!.Token, user.Id));
        var second = await service.ClaimAsync(new(created.Value.Token, user.Id));

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Equal(1, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task CheckoutSession_Should_Reject_Claim_By_Another_User()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var tariff = await SeedTariffAsync(db);
        var firstUser = await SeedUserAsync(db, "first@example.test");
        var secondUser = await SeedUserAsync(db, "second@example.test");
        var service = CreateService(db, clock);

        var created = await service.CreateAsync(new(tariff.Id, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false, null, null));
        Assert.True(created.IsSuccess, created.Error);
        var first = await service.ClaimAsync(new(created.Value!.Token, firstUser.Id));
        Assert.True(first.IsSuccess, first.Error);

        var second = await service.ClaimAsync(new(created.Value.Token, secondUser.Id));

        Assert.False(second.IsSuccess);
        Assert.Equal(1, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task CheckoutSession_Should_Reject_Expired_Token()
    {
        await using var db = CreateDbContext();
        var clock = new MutableClock(new DateTimeOffset(2026, 4, 29, 8, 0, 0, TimeSpan.Zero));
        var tariff = await SeedTariffAsync(db);
        var user = await SeedUserAsync(db, "buyer@example.test");
        var service = CreateService(db, clock);

        var created = await service.CreateAsync(new(tariff.Id, OrderType.NewSubscription, ChannelType.Web, PaymentProvider.YooKassa, null, false, null, null));
        Assert.True(created.IsSuccess, created.Error);
        clock.UtcNow = clock.UtcNow.AddMinutes(31);

        var claimed = await service.ClaimAsync(new(created.Value!.Token, user.Id));

        Assert.False(claimed.IsSuccess);
        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal("expired", (await db.CheckoutSessions.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CheckoutSession_Claim_Should_Be_Atomic_Across_Concurrent_Users(bool sameUser)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-checkout-{Guid.NewGuid():N}.db");
        try
        {
            var clock = new FixedClock(new DateTimeOffset(2026, 8, 5, 14, 30, 0, TimeSpan.Zero));
            Guid winnerUserId;
            Guid loserUserId;
            string token;
            await using (var seedDb = CreateSqliteDbContext(databasePath))
            {
                await seedDb.Database.EnsureCreatedAsync();
                var tariff = await SeedTariffAsync(seedDb);
                var winner = await SeedUserAsync(seedDb, "checkout-winner@example.test");
                var loser = sameUser
                    ? winner
                    : await SeedUserAsync(seedDb, "checkout-loser@example.test");
                var created = await CreateService(seedDb, clock).CreateAsync(new(
                    tariff.Id,
                    OrderType.NewSubscription,
                    ChannelType.Web,
                    PaymentProvider.YooKassa,
                    null,
                    false,
                    null,
                    null));
                Assert.True(created.IsSuccess, created.Error);
                winnerUserId = winner.Id;
                loserUserId = loser.Id;
                token = created.Value!.Token;
            }

            var winnerBeforeLoserTransaction = new ClaimBeforeTransactionInterceptor(databasePath, token, winnerUserId, clock);
            await using (var loserDb = CreateSqliteDbContext(databasePath, winnerBeforeLoserTransaction))
            {
                var loserResult = await CreateService(loserDb, clock).ClaimAsync(new(token, loserUserId));

                Assert.NotNull(winnerBeforeLoserTransaction.WinnerResult);
                Assert.True(winnerBeforeLoserTransaction.WinnerResult!.IsSuccess, winnerBeforeLoserTransaction.WinnerResult.Error);
                if (sameUser)
                {
                    Assert.True(loserResult.IsSuccess, loserResult.Error);
                    Assert.Equal(winnerBeforeLoserTransaction.WinnerResult.Value!.Id, loserResult.Value!.Id);
                }
                else
                {
                    Assert.False(loserResult.IsSuccess);
                    Assert.Contains("another user", loserResult.Error, StringComparison.OrdinalIgnoreCase);
                }
            }

            await using var assertDb = CreateSqliteDbContext(databasePath);
            var order = await assertDb.Orders.AsNoTracking().SingleAsync();
            var session = await assertDb.CheckoutSessions.AsNoTracking().SingleAsync();
            Assert.Equal(winnerUserId, order.UserId);
            Assert.Equal(winnerUserId, session.UserId);
            Assert.Equal(order.Id, session.OrderId);
            Assert.Equal("claimed", session.Status);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Completed_CheckoutSession_Should_Remain_Idempotent_After_Expiry()
    {
        await using var db = CreateDbContext();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 5, 15, 0, 0, TimeSpan.Zero));
        var tariff = await SeedTariffAsync(db);
        var user = await SeedUserAsync(db, "completed-checkout@example.test");
        var service = CreateService(db, clock);
        var created = await service.CreateAsync(new(
            tariff.Id,
            OrderType.NewSubscription,
            ChannelType.Web,
            PaymentProvider.YooKassa,
            null,
            false,
            null,
            null));
        var first = await service.ClaimAsync(new(created.Value!.Token, user.Id));
        Assert.True(first.IsSuccess, first.Error);
        var session = await db.CheckoutSessions.SingleAsync();
        session.Status = "completed";
        session.CompletedAt = clock.UtcNow;
        await db.SaveChangesAsync();
        clock.UtcNow = clock.UtcNow.AddMinutes(31);

        var repeated = await service.ClaimAsync(new(created.Value.Token, user.Id));

        Assert.True(repeated.IsSuccess, repeated.Error);
        Assert.Equal(first.Value!.Id, repeated.Value!.Id);
        Assert.Equal("completed", (await db.CheckoutSessions.SingleAsync()).Status);
        Assert.Equal(1, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task CheckoutSession_Claim_Should_Roll_Back_Reservation_When_Order_Creation_Fails()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-checkout-rollback-{Guid.NewGuid():N}.db");
        try
        {
            var clock = new FixedClock(new DateTimeOffset(2026, 8, 5, 15, 30, 0, TimeSpan.Zero));
            string token;
            Guid userId;
            await using (var db = CreateSqliteDbContext(databasePath))
            {
                await db.Database.EnsureCreatedAsync();
                var tariff = await SeedTariffAsync(db);
                var user = await SeedUserAsync(db, "checkout-rollback@example.test");
                var service = CreateService(db, clock);
                var created = await service.CreateAsync(new(
                    tariff.Id,
                    OrderType.NewSubscription,
                    ChannelType.Web,
                    PaymentProvider.YooKassa,
                    null,
                    false,
                    null,
                    null));
                Assert.True(created.IsSuccess, created.Error);
                tariff.IsActive = false;
                await db.SaveChangesAsync();
                token = created.Value!.Token;
                userId = user.Id;

                var claim = await service.ClaimAsync(new(token, userId));

                Assert.False(claim.IsSuccess);
                Assert.Contains("inactive", claim.Error, StringComparison.OrdinalIgnoreCase);
            }

            await using var assertDb = CreateSqliteDbContext(databasePath);
            var session = await assertDb.CheckoutSessions.AsNoTracking().SingleAsync();
            Assert.Equal("open", session.Status);
            Assert.Null(session.UserId);
            Assert.Null(session.OrderId);
            Assert.Null(session.ClaimedAt);
            Assert.Empty(await assertDb.Orders.AsNoTracking().ToListAsync());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static CheckoutSessionService CreateService(ApplicationDbContext db, IClock clock)
        => new(db, clock, new OrderService(db, clock));

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(string databasePath)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Default Timeout=10;Pooling=False")
            .Options);

    private static ApplicationDbContext CreateSqliteDbContext(string databasePath, IInterceptor interceptor)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Default Timeout=10;Pooling=False")
            .AddInterceptors(interceptor)
            .Options);

    private static async Task<Tariff> SeedTariffAsync(ApplicationDbContext db)
    {
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = "monthly", Description = "Monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        return tariff;
    }

    private static async Task<User> SeedUserAsync(ApplicationDbContext db, string email)
    {
        var user = new User { Id = Guid.NewGuid(), Email = email, DisplayName = email, PasswordHash = "hash", ReferralCode = Guid.NewGuid().ToString("N")[..8] };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class MutableClock : IClock
    {
        public MutableClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class ClaimBeforeTransactionInterceptor(
        string databasePath,
        string token,
        Guid winnerUserId,
        IClock clock) : DbTransactionInterceptor
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
                await using var winnerDb = CreateSqliteDbContext(databasePath);
                WinnerResult = await CreateService(winnerDb, clock).ClaimAsync(new(token, winnerUserId), cancellationToken);
            }

            return result;
        }
    }
}
