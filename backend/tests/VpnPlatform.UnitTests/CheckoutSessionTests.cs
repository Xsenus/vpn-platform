using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class CheckoutSessionTests
{
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

    private static CheckoutSessionService CreateService(ApplicationDbContext db, IClock clock)
        => new(db, clock, new OrderService(db, clock));

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

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
}
