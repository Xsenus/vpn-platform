using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TelegramBotFoundationTests
{
    [Fact]
    public async Task Start_Should_Create_TelegramAccount_And_Duplicate_Update_Should_Be_Ignored()
    {
        await using var db = CreateDbContext();
        var service = new TelegramBotService(db, new FixedClock());
        var raw = Update(100, "/start");

        var first = await service.ProcessUpdateAsync(raw, new Dictionary<string, string>(), null, CancellationToken.None);
        var second = await service.ProcessUpdateAsync(raw, new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(first.Value!.Processed);
        Assert.True(second.IsSuccess, second.Error);
        Assert.False(second.Value!.Processed);
        Assert.Equal(1, await db.TelegramAccounts.CountAsync());
        Assert.Equal(1, await db.TelegramBotUpdates.CountAsync());
    }

    [Fact]
    public async Task Link_Token_Should_Link_TelegramAccount_Once()
    {
        await using var db = CreateDbContext();
        var clock = new FixedClock();
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.test", DisplayName = "User", PasswordHash = "hash", ReferralCode = "user" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new TelegramBotService(db, clock);
        var token = await service.CreateLinkTokenAsync(user.Id, "vpnplatform_bot", CancellationToken.None);
        Assert.True(token.IsSuccess, token.Error);

        var linked = await service.ProcessUpdateAsync(Update(101, $"/start link_{token.Value!.Token}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var reused = await service.ProcessUpdateAsync(Update(102, $"/start link_{token.Value!.Token}"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(linked.IsSuccess, linked.Error);
        Assert.True(linked.Value!.ResponseText.Contains("успешно", StringComparison.OrdinalIgnoreCase));
        Assert.True(reused.Value!.ResponseText.Contains("уже использован", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(user.Id, (await db.TelegramAccounts.SingleAsync()).UserId);
        var storedLink = await db.TelegramBotDeepLinks.SingleAsync();
        Assert.NotEqual(token.Value.Token, storedLink.TokenHash);
        Assert.Equal(64, storedLink.TokenHash.Length);
        Assert.NotNull(storedLink.UsedAt);
    }

    [Fact]
    public async Task Expired_Link_Token_Should_Be_Rejected()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.test", DisplayName = "User", PasswordHash = "hash", ReferralCode = "user" };
        db.Users.Add(user);
        db.TelegramBotDeepLinks.Add(new TelegramBotDeepLink
        {
            UserId = user.Id,
            TokenHash = TelegramBotService.HashToken("expired-token"),
            Purpose = "link_account",
            ExpiresAt = new DateTimeOffset(2026, 4, 29, 9, 0, 0, TimeSpan.Zero)
        });
        await db.SaveChangesAsync();

        var service = new TelegramBotService(db, new FixedClock());
        var result = await service.ProcessUpdateAsync(Update(103, "/start link_expired-token"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value!.ResponseText.Contains("истек", StringComparison.OrdinalIgnoreCase));
        Assert.Null((await db.TelegramAccounts.SingleAsync()).UserId);
    }

    [Fact]
    public async Task Webhook_Secret_Token_Should_Be_Validated()
    {
        await using var db = CreateDbContext();
        var service = new TelegramBotService(db, new FixedClock());
        var result = await service.ProcessUpdateAsync(Update(104, "/start"), new Dictionary<string, string>(), "expected-secret", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, await db.TelegramAccounts.CountAsync());
    }

    [Fact]
    public async Task Support_Command_Should_Create_Conversation()
    {
        await using var db = CreateDbContext();
        var service = new TelegramBotService(db, new FixedClock());

        var result = await service.ProcessUpdateAsync(Update(105, "/support"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1, await db.SupportConversations.CountAsync());
        Assert.Equal(1, await db.SupportMessages.CountAsync());
    }


    [Fact]
    public async Task Callback_Query_Routing_Should_Work()
    {
        await using var db = CreateDbContext();
        var service = new TelegramBotService(db, new FixedClock());

        var result = await service.ProcessUpdateAsync(CallbackUpdate(106, "buy"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.ResponseText));
        Assert.Equal(1, await db.TelegramBotCallbackQueries.CountAsync());
    }

    [Fact]
    public async Task Unknown_Command_Should_Return_Help()
    {
        await using var db = CreateDbContext();
        var service = new TelegramBotService(db, new FixedClock());

        var result = await service.ProcessUpdateAsync(Update(107, "/unknown"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value!.ResponseText.Contains("Доступные команды", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TelegramAccount_Should_Not_Link_To_Two_Users_And_User_Can_Unlink()
    {
        await using var db = CreateDbContext();
        var firstUser = new User { Id = Guid.NewGuid(), Email = "first@example.test", DisplayName = "First", PasswordHash = "hash", ReferralCode = "first" };
        var secondUser = new User { Id = Guid.NewGuid(), Email = "second@example.test", DisplayName = "Second", PasswordHash = "hash", ReferralCode = "second" };
        db.Users.AddRange(firstUser, secondUser);
        await db.SaveChangesAsync();
        var service = new TelegramBotService(db, new FixedClock());
        var firstToken = await service.CreateLinkTokenAsync(firstUser.Id, "vpnplatform_bot", CancellationToken.None);
        var secondToken = await service.CreateLinkTokenAsync(secondUser.Id, "vpnplatform_bot", CancellationToken.None);
        Assert.True(firstToken.IsSuccess, firstToken.Error);
        Assert.True(secondToken.IsSuccess, secondToken.Error);

        var firstLink = await service.ProcessUpdateAsync(Update(108, $"/start link_{firstToken.Value!.Token}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var secondLink = await service.ProcessUpdateAsync(Update(109, $"/start link_{secondToken.Value!.Token}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var unlink = await service.UnlinkAsync(firstUser.Id, CancellationToken.None);

        Assert.True(firstLink.IsSuccess, firstLink.Error);
        Assert.True(secondLink.Value!.ResponseText.Contains("другому аккаунту", StringComparison.OrdinalIgnoreCase));
        Assert.True(unlink.IsSuccess, unlink.Error);
        Assert.False(unlink.Value!.IsLinked);
    }

    [Fact]
    public async Task Telegram_Link_Status_Unlink_Should_Work_End_To_End_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock();
        var user = new User { Id = Guid.NewGuid(), Email = "sqlite@example.test", DisplayName = "SQLite User", PasswordHash = "hash", ReferralCode = "sqlite" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new TelegramBotService(db, clock);

        var before = await service.GetStatusAsync(user.Id, CancellationToken.None);
        var token = await service.CreateLinkTokenAsync(user.Id, "@vpnplatform_bot", CancellationToken.None);
        var staleToken = await service.CreateLinkTokenAsync(user.Id, "vpnplatform_bot", CancellationToken.None);
        var linked = await service.ProcessUpdateAsync(Update(110, $"/start link_{token.Value!.Token}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var duplicateToken = await service.CreateLinkTokenAsync(user.Id, "vpnplatform_bot", CancellationToken.None);
        var repeat = await service.ProcessUpdateAsync(Update(111, $"/start link_{staleToken.Value!.Token}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var linkedStatus = await service.GetStatusAsync(user.Id, CancellationToken.None);
        var unlink = await service.UnlinkAsync(user.Id, CancellationToken.None);
        var afterUnlink = await service.GetStatusAsync(user.Id, CancellationToken.None);

        Assert.False(before.IsLinked);
        Assert.True(token.IsSuccess, token.Error);
        Assert.True(staleToken.IsSuccess, staleToken.Error);
        Assert.Equal($"https://t.me/vpnplatform_bot?start=link_{token.Value.Token}", token.Value.DeepLinkUrl);
        Assert.True(linked.IsSuccess, linked.Error);
        Assert.True(linked.Value!.ResponseText.Contains("успешно", StringComparison.OrdinalIgnoreCase));
        Assert.False(duplicateToken.IsSuccess);
        Assert.Contains("already has a linked Telegram account", duplicateToken.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True(repeat.IsSuccess, repeat.Error);
        Assert.Contains("уже привязан", repeat.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.True(linkedStatus.IsLinked);
        Assert.Equal(777001, linkedStatus.TelegramUserId);
        Assert.Equal("ivan", linkedStatus.Username);
        Assert.Equal(clock.UtcNow, linkedStatus.LinkedAt);
        Assert.True(unlink.IsSuccess, unlink.Error);
        Assert.False(unlink.Value!.IsLinked);
        Assert.False(afterUnlink.IsLinked);
        Assert.Equal(1, await db.TelegramAccounts.CountAsync());
        Assert.Equal(2, await db.TelegramBotDeepLinks.CountAsync(x => x.UsedAt != null && x.UsedByTelegramUserId == 777001));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static string Update(long updateId, string text)
        => $$"""
        {
          "update_id": {{updateId}},
          "message": {
            "message_id": {{updateId + 1000}},
            "from": { "id": 777001, "is_bot": false, "first_name": "Ivan", "username": "ivan", "language_code": "ru" },
            "chat": { "id": 777001, "type": "private" },
            "date": 1777466400,
            "text": "{{text}}"
          }
        }
        """;


    private static string CallbackUpdate(long updateId, string data)
        => $$"""
        {
          "update_id": {{updateId}},
          "callback_query": {
            "id": "cb-{{updateId}}",
            "from": { "id": 777001, "is_bot": false, "first_name": "Ivan", "username": "ivan", "language_code": "ru" },
            "message": { "message_id": {{updateId + 1000}}, "chat": { "id": 777001, "type": "private" } },
            "data": "{{data}}"
          }
        }
        """;

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 4, 29, 10, 0, 0, TimeSpan.Zero);
    }
}
