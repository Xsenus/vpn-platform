using System.Security.Claims;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Api.Controllers.Me;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ReferralRewardLifecycleTests
{
    [Fact]
    public async Task Referral_Outbox_Should_Materialize_Both_Rewards_Once_And_Redact_Cabinet_Response()
    {
        await using var connection = await OpenConnectionAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateDb(connection, interceptor);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 18, 0, 0, TimeSpan.Zero);
        var (order, message, referrer, referred) = await SeedEligibleRewardAsync(db, now);
        var clock = new FixedClock(now);
        var delivery = new OutboxMessageDeliveryService(
            db,
            clock,
            new LocalOutboxMessageSink(db, new ReferralRewardService(db, clock)));

        var first = await delivery.DeliverAsync(message.Id);
        var repeated = await delivery.DeliverAsync(message.Id);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(repeated.IsSuccess, repeated.Error);
        var ledgers = await db.RewardLedgers.AsNoTracking().OrderBy(x => x.UserId).ToListAsync();
        Assert.Equal(2, ledgers.Count);
        Assert.All(ledgers, ledger =>
        {
            Assert.Equal(now, ledger.CreatedAt);
            Assert.Equal(now, ledger.UpdatedAt);
        });
        var referrerReward = Assert.Single(ledgers, x => x.UserId == referrer.Id);
        Assert.Equal(referred.Id, referrerReward.SourceUserId);
        Assert.Equal(7m, referrerReward.Value);
        Assert.Equal(RewardStatus.Approved, referrerReward.Status);
        Assert.NotNull(referrerReward.ProcessedAt);
        var referredReward = Assert.Single(ledgers, x => x.UserId == referred.Id);
        Assert.Equal(2m, referredReward.Value);
        Assert.Equal(RewardStatus.Pending, referredReward.Status);
        Assert.Contains(order.Id.ToString(), referredReward.MetadataJson, StringComparison.OrdinalIgnoreCase);

        var controller = CreateMeController(db, referrer.Id);
        var response = Assert.IsType<OkObjectResult>(await controller.GetReferrals(CancellationToken.None));
        var json = JsonSerializer.Serialize(response.Value);
        Assert.Contains("bonus-days", json, StringComparison.Ordinal);
        Assert.DoesNotContain(referred.Id.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("metadataJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sourceUserId", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("FROM \"ReferralRelationships\"", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("FROM \"ReferralPrograms\"", StringComparison.OrdinalIgnoreCase)
            && command.Contains("julianday(\"StartAt\")", StringComparison.OrdinalIgnoreCase)
            && command.Contains("julianday(\"EndAt\")", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cabinet_Referral_History_Should_Return_Only_The_Latest_100_Rewards()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var user = User("reward-history@example.test", "REF-HISTORY");
        db.Users.Add(user);
        db.RewardLedgers.AddRange(Enumerable.Range(0, 105).Select(index => new RewardLedger
        {
            UserId = user.Id,
            Type = "bonus-days",
            Status = RewardStatus.Approved,
            Value = index,
            CurrencyOrUnit = "days",
            CreatedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(index),
            UpdatedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(index)
        }));
        await db.SaveChangesAsync();

        var response = Assert.IsType<OkObjectResult>(await CreateMeController(db, user.Id).GetReferrals(CancellationToken.None));
        var rewards = Assert.IsAssignableFrom<IEnumerable<object>>(response.Value).ToList();
        var values = rewards
            .Select(item => (decimal)item.GetType().GetProperty("Value")!.GetValue(item)!)
            .ToList();

        Assert.Equal(100, rewards.Count);
        Assert.Equal(104m, values[0]);
        Assert.Equal(5m, values[^1]);
    }

    [Fact]
    public async Task Referral_Rewards_Should_Respect_First_Purchase_And_Promo_Stacking()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 18, 15, 0, TimeSpan.Zero);
        var (order, _, _, referred) = await SeedEligibleRewardAsync(db, now);
        db.Orders.Add(new Order
        {
            UserId = referred.Id,
            TariffId = order.TariffId,
            Type = OrderType.NewSubscription,
            Status = OrderStatus.Completed,
            Amount = order.Amount,
            Currency = "RUB",
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = now.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var service = new ReferralRewardService(db, new FixedClock(now));

        var repeatedPurchase = await service.MaterializeForOrderAsync(order.Id, Guid.NewGuid());

        Assert.True(repeatedPurchase.IsSuccess, repeatedPurchase.Error);
        Assert.Equal(0, repeatedPurchase.Value);
        Assert.Empty(await db.RewardLedgers.ToListAsync());

        var previous = await db.Orders.FirstAsync(x => x.Id != order.Id);
        db.Orders.Remove(previous);
        var promo = new PromoCode { Code = "NO-STACK", DiscountType = "percent", DiscountValue = 5, IsActive = true, AllowStackWithReferral = false };
        db.PromoCodes.Add(promo);
        order.PromoCodeId = promo.Id;
        db.Orders.Update(order);
        await db.SaveChangesAsync();

        var promoResult = await service.MaterializeForOrderAsync(order.Id, Guid.NewGuid());

        Assert.True(promoResult.IsSuccess, promoResult.Error);
        Assert.Equal(0, promoResult.Value);
        Assert.Empty(await db.RewardLedgers.ToListAsync());
    }

    [Fact]
    public async Task Invalid_Active_Referral_Program_Should_Dead_Letter_Without_Partial_Ledger()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 18, 30, 0, TimeSpan.Zero);
        var (_, message, _, _) = await SeedEligibleRewardAsync(db, now);
        var program = await db.ReferralPrograms.SingleAsync();
        program.RewardDefinition = "{}";
        await db.SaveChangesAsync();
        var clock = new FixedClock(now);

        var result = await new OutboxMessageDeliveryService(
            db,
            clock,
            new LocalOutboxMessageSink(db, new ReferralRewardService(db, clock)))
            .DeliverAsync(message.Id);

        Assert.False(result.IsSuccess);
        Assert.NotNull((await db.OutboxMessages.AsNoTracking().SingleAsync()).FailedAt);
        Assert.Empty(await db.RewardLedgers.AsNoTracking().ToListAsync());
    }

    private static async Task<(Order Order, OutboxMessage Message, User Referrer, User Referred)> SeedEligibleRewardAsync(
        ApplicationDbContext db,
        DateTimeOffset now)
    {
        var referrer = User("reward-referrer@example.test", "REF-REWARD");
        var referred = User("reward-referred@example.test", "REF-REFERRED");
        var tariff = new Tariff
        {
            Name = "Referral eligible",
            Slug = $"referral-{Guid.NewGuid():N}",
            DurationDays = 30,
            Price = 500m,
            Currency = "RUB",
            MaxDevices = 2,
            IsActive = true,
            IsReferralEligible = true
        };
        var program = new ReferralProgram
        {
            Name = "First purchase",
            Status = "active",
            StartAt = now.AddDays(-1),
            EndAt = now.AddDays(1),
            RuleDefinition = "{\"firstPurchaseOnly\":true,\"minimumOrderAmount\":100,\"allowedChannels\":[\"Web\"]}",
            RewardDefinition = "{\"referrer\":{\"type\":\"bonus-days\",\"value\":7,\"unit\":\"days\",\"autoApprove\":true},\"referred\":{\"type\":\"bonus-days\",\"value\":2,\"unit\":\"days\",\"autoApprove\":false}}"
        };
        db.AddRange(referrer, referred, tariff, program);
        var relationship = new ReferralRelationship
        {
            ReferrerUserId = referrer.Id,
            ReferredUserId = referred.Id,
            SourceChannel = ChannelType.Web
        };
        var order = new Order
        {
            UserId = referred.Id,
            TariffId = tariff.Id,
            Type = OrderType.NewSubscription,
            Status = OrderStatus.Completed,
            Amount = 500m,
            Currency = "RUB",
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = now.AddMinutes(15)
        };
        var message = new OutboxMessage
        {
            Type = "ReferralRewardRequested",
            CorrelationId = order.Id.ToString("N"),
            PayloadJson = JsonSerializer.Serialize(new { orderId = order.Id }),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.AddRange(relationship, order, message);
        await db.SaveChangesAsync();
        return (order, message, referrer, referred);
    }

    private static MeController CreateMeController(ApplicationDbContext db, Guid userId)
    {
        var controller = new MeController(db, null!, null!, null!, null!, null!, null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                    "test"))
            }
        };
        return controller;
    }

    private static User User(string email, string code)
        => new() { Email = email, DisplayName = email, PasswordHash = "hash", ReferralCode = code, Status = UserStatus.Active };

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static ApplicationDbContext CreateDb(SqliteConnection connection, DbCommandInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection);
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }
        return new ApplicationDbContext(builder.Options);
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

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
}
