using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminTelegramBotSettingsControllerTests
{
    [Fact]
    public async Task Telegram_Bot_Settings_Should_Save_Validate_And_Check_Readiness_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var controller = new AdminTelegramBotSettingsController(db, EmptyConfiguration(), new TestSecretProtector());

        var saved = await controller.UpdateSettings(new UpdateTelegramBotSettingsCommand(
            Enabled: true,
            Mode: "Webhook",
            PublicBotUsername: "@vpn_ready_bot",
            BotToken: "123456789:telegram-secret",
            WebhookUrl: "https://api.example.test/api/channels/telegram/webhook",
            SecretToken: "webhook-secret",
            AdminChatId: "-100123456",
            WebAppUrl: "https://cabinet.example.test",
            WelcomeText: "Добро пожаловать",
            InstructionText: "Инструкция",
            SupportText: "Поддержка",
            AfterPaymentTextTemplate: "Оплата получена",
            RenewalTextTemplate: "Подписка продлена",
            PaymentFailedTextTemplate: "Оплата не прошла",
            SubscriptionExpiredTextTemplate: "Подписка закончилась"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(saved);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("vpn_ready_bot", json, StringComparison.Ordinal);
        Assert.Contains("1234***cret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook-secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(await db.SiteContentBlocks.ToListAsync(), x => x.Key == "telegram_bot.bot_token_protected" && x.Value == "protected:123456789:telegram-secret");
        Assert.Contains(await db.SiteContentBlocks.ToListAsync(), x => x.Key == "telegram_bot.secret_token_protected" && x.Value == "protected:webhook-secret");
        Assert.Contains(await db.NotificationTemplates.ToListAsync(), x => x.Key == "telegram.welcome" && x.Body == "Добро пожаловать");

        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "telegram_bot.secret.rotate");
        var auditJson = $"{audit.BeforeJson}\n{audit.AfterJson}";
        Assert.Contains("rotatedBotToken", auditJson, StringComparison.Ordinal);
        Assert.Contains("rotatedSecretToken", auditJson, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-secret", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook-secret", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("protected:", auditJson, StringComparison.OrdinalIgnoreCase);

        var check = await controller.TestSettings(CancellationToken.None);
        var checkDto = Assert.IsType<AdminTelegramBotConnectionCheckDto>(Assert.IsType<OkObjectResult>(check).Value);
        Assert.True(checkDto.IsReady);
        Assert.Equal("ready", checkDto.Status);
        Assert.Empty(checkDto.RequiredActions);
    }

    [Fact]
    public async Task Telegram_Bot_Settings_Should_Reject_Invalid_Enabled_Configuration_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var controller = new AdminTelegramBotSettingsController(db, EmptyConfiguration(), new TestSecretProtector());

        var withoutToken = await controller.UpdateSettings(new UpdateTelegramBotSettingsCommand(
            Enabled: true,
            Mode: "LongPolling",
            PublicBotUsername: "vpn_ready_bot",
            BotToken: null,
            WebhookUrl: null,
            SecretToken: null,
            AdminChatId: null,
            WebAppUrl: null,
            WelcomeText: null,
            InstructionText: null,
            SupportText: null,
            AfterPaymentTextTemplate: null,
            RenewalTextTemplate: null,
            PaymentFailedTextTemplate: null,
            SubscriptionExpiredTextTemplate: null), CancellationToken.None);
        Assert.Contains("Bot token", JsonSerializer.Serialize(Assert.IsType<BadRequestObjectResult>(withoutToken).Value), StringComparison.Ordinal);

        var withoutWebhook = await controller.UpdateSettings(new UpdateTelegramBotSettingsCommand(
            Enabled: true,
            Mode: "Webhook",
            PublicBotUsername: "vpn_ready_bot",
            BotToken: "123456789:telegram-secret",
            WebhookUrl: "",
            SecretToken: null,
            AdminChatId: null,
            WebAppUrl: null,
            WelcomeText: null,
            InstructionText: null,
            SupportText: null,
            AfterPaymentTextTemplate: null,
            RenewalTextTemplate: null,
            PaymentFailedTextTemplate: null,
            SubscriptionExpiredTextTemplate: null), CancellationToken.None);
        Assert.Contains("Webhook URL", JsonSerializer.Serialize(Assert.IsType<BadRequestObjectResult>(withoutWebhook).Value), StringComparison.Ordinal);

        var invalidWebApp = await controller.UpdateSettings(new UpdateTelegramBotSettingsCommand(
            Enabled: false,
            Mode: "LongPolling",
            PublicBotUsername: "vpn_ready_bot",
            BotToken: null,
            WebhookUrl: null,
            SecretToken: null,
            AdminChatId: null,
            WebAppUrl: "cabinet.example.test",
            WelcomeText: null,
            InstructionText: null,
            SupportText: null,
            AfterPaymentTextTemplate: null,
            RenewalTextTemplate: null,
            PaymentFailedTextTemplate: null,
            SubscriptionExpiredTextTemplate: null), CancellationToken.None);
        Assert.Contains("WebApp URL", JsonSerializer.Serialize(Assert.IsType<BadRequestObjectResult>(invalidWebApp).Value), StringComparison.Ordinal);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => $"protected:{plaintext}";
        public string Unprotect(string protectedValue) => protectedValue.StartsWith("protected:", StringComparison.Ordinal)
            ? protectedValue["protected:".Length..]
            : protectedValue;
        public string Mask(string? value, int visibleTail = 4)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= visibleTail ? "***" : $"***{value[^visibleTail..]}";
        }
    }
}
