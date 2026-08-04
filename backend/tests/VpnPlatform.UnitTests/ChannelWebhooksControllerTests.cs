using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Channels;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ChannelWebhooksControllerTests
{
    [Fact]
    public async Task Unsupported_Channel_Webhooks_Should_Return_Explicit_NotImplemented_Responses()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db, new RecordingTelegramProvider(), "{}");

        var results = new[]
        {
            (Result: controller.Discord(new { }), Error: "discord_channel_not_configured"),
            (Result: controller.Vk(new { }), Error: "vk_channel_not_configured"),
            (Result: controller.WhatsApp(new { }), Error: "whatsapp_channel_not_configured")
        };

        foreach (var (result, error) in results)
        {
            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status501NotImplemented, response.StatusCode);
            Assert.Contains(error, JsonSerializer.Serialize(response.Value), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Telegram_Webhook_Should_Process_Update_From_Db_Settings_And_Send_Response()
    {
        await using var db = CreateDbContext();
        db.SiteContentBlocks.AddRange(
            Setting(TelegramBotRuntimeSettingsService.EnabledKey, "true"),
            Setting(TelegramBotRuntimeSettingsService.ModeKey, "Webhook"),
            Setting(TelegramBotRuntimeSettingsService.SecretTokenProtectedKey, "protected:webhook-secret"),
            Setting(TelegramBotRuntimeSettingsService.BotTokenProtectedKey, "protected:bot-token"));
        await db.SaveChangesAsync();

        var telegramProvider = new RecordingTelegramProvider();
        var controller = CreateController(db, telegramProvider, Update(9001, "/start"));
        controller.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = "webhook-secret";

        var first = await controller.Telegram(CancellationToken.None);
        controller = CreateController(db, telegramProvider, Update(9001, "/start"));
        controller.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = "webhook-secret";
        var duplicate = await controller.Telegram(CancellationToken.None);

        Assert.Equal("processed", ReadStatus(first));
        Assert.Equal("duplicate", ReadStatus(duplicate));
        Assert.Equal(1, await db.TelegramBotUpdates.CountAsync());
        var sentMessage = Assert.Single(telegramProvider.SentMessages);
        Assert.Equal(777001, sentMessage.ChatId);
        Assert.NotEmpty(sentMessage.Text);
    }

    [Fact]
    public async Task Telegram_Webhook_Should_Return_NotFound_When_Bot_Is_Disabled()
    {
        await using var db = CreateDbContext();
        db.SiteContentBlocks.Add(Setting(TelegramBotRuntimeSettingsService.EnabledKey, "false"));
        await db.SaveChangesAsync();

        var controller = CreateController(db, new RecordingTelegramProvider(), Update(9002, "/start"));

        var result = await controller.Telegram(CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var json = JsonSerializer.Serialize(notFound.Value);
        Assert.Contains("telegram_bot_disabled", json, StringComparison.Ordinal);
        Assert.Equal(0, await db.TelegramBotUpdates.CountAsync());
    }

    [Fact]
    public async Task Telegram_Webhook_Should_Return_ServiceUnavailable_For_Fresh_Update_Lease()
    {
        await using var db = CreateDbContext();
        db.SiteContentBlocks.AddRange(
            Setting(TelegramBotRuntimeSettingsService.EnabledKey, "true"),
            Setting(TelegramBotRuntimeSettingsService.ModeKey, "Webhook"),
            Setting(TelegramBotRuntimeSettingsService.SecretTokenProtectedKey, "protected:webhook-secret"),
            Setting(TelegramBotRuntimeSettingsService.BotTokenProtectedKey, "protected:bot-token"));
        db.TelegramBotUpdates.Add(new TelegramBotUpdate
        {
            UpdateId = 9003,
            TelegramUserId = 777001,
            UpdateType = "message",
            RawPayload = "{}",
            PayloadSha256 = "reserved",
            IsProcessed = false,
            ErrorText = string.Empty,
            CreatedAt = new FixedClock().UtcNow,
            UpdatedAt = new FixedClock().UtcNow
        });
        await db.SaveChangesAsync();
        var controller = CreateController(db, new RecordingTelegramProvider(), Update(9003, "/start"));
        controller.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = "webhook-secret";

        var result = await controller.Telegram(CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        Assert.Contains("in progress", JsonSerializer.Serialize(unavailable.Value), StringComparison.OrdinalIgnoreCase);
    }

    private static ChannelWebhooksController CreateController(ApplicationDbContext db, RecordingTelegramProvider telegramProvider, string rawBody)
    {
        var configuration = new ConfigurationBuilder().Build();
        var secretProtector = new TestSecretProtector();
        var settings = new TelegramBotRuntimeSettingsService(db, configuration, secretProtector);
        var bot = new TelegramBotService(db, new FixedClock());
        var controller = new ChannelWebhooksController(bot, settings, telegramProvider);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(rawBody));
        controller.Request.ContentType = "application/json";
        return controller;
    }

    private static string ReadStatus(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        return json.RootElement.GetProperty("status").GetString() ?? string.Empty;
    }

    private static SiteContentBlock Setting(string key, string value)
        => new()
        {
            Key = key,
            Group = TelegramBotRuntimeSettingsService.SettingsGroup,
            Label = key,
            Value = value,
            InputType = "text",
            IsActive = true
        };

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
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

    private sealed class RecordingTelegramProvider : ITelegramInvoiceProvider
    {
        public List<(long ChatId, string Text, string? ReplyMarkupJson)> SentMessages { get; } = new();
        public List<(string QueryId, bool Ok, string? ErrorMessage)> PreCheckoutAnswers { get; } = new();

        public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new TelegramInvoiceResult(request.Payload, "{}"));

        public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken)
        {
            PreCheckoutAnswers.Add((preCheckoutQueryId, ok, errorMessage));
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken)
        {
            SentMessages.Add((chatId, text, replyMarkupJson));
            return Task.CompletedTask;
        }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => "protected:" + plaintext;

        public string Unprotect(string protectedValue)
            => protectedValue.StartsWith("protected:", StringComparison.Ordinal)
                ? protectedValue["protected:".Length..]
                : protectedValue;

        public string Mask(string? value, int visibleTail = 4) => value ?? string.Empty;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 14, 17, 30, 0, TimeSpan.Zero);
    }
}
