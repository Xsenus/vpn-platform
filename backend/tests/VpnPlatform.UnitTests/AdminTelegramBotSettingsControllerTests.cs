using System.Text.Json;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    public async Task Telegram_Bot_Settings_Workflow_Should_Use_Injected_Clock()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var initialTime = new DateTimeOffset(2033, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var clock = new MutableClock(initialTime);
        var controller = new AdminTelegramBotSettingsController(
            db,
            EmptyConfiguration(),
            new TestSecretProtector(),
            clock);

        var initialSettings = Assert.IsType<AdminTelegramBotSettingsDto>(
            Assert.IsType<OkObjectResult>(await controller.GetSettings(CancellationToken.None)).Value);
        var initialCheck = Assert.IsType<AdminTelegramBotConnectionCheckDto>(
            Assert.IsType<OkObjectResult>(await controller.TestSettings(CancellationToken.None)).Value);
        Assert.Equal(initialTime, initialSettings.GeneratedAt);
        Assert.Equal(0, initialSettings.Revision);
        Assert.Equal(initialTime, initialCheck.CheckedAt);

        var request = DisabledSettings("Initial welcome", initialSettings.Revision);
        var firstUpdatedSettings = Assert.IsType<AdminTelegramBotSettingsDto>(
            Assert.IsType<OkObjectResult>(await controller.UpdateSettings(request, CancellationToken.None)).Value);
        Assert.All(await db.SiteContentBlocks.ToListAsync(), setting =>
        {
            Assert.Equal(initialTime, setting.CreatedAt);
            Assert.Equal(initialTime, setting.UpdatedAt);
        });
        var template = await db.NotificationTemplates.SingleAsync();
        Assert.Equal(initialTime, template.CreatedAt);
        Assert.Equal(initialTime, template.UpdatedAt);
        Assert.Equal(1, firstUpdatedSettings.Revision);

        clock.UtcNow = initialTime.AddHours(1);
        var updatedSettings = Assert.IsType<AdminTelegramBotSettingsDto>(
            Assert.IsType<OkObjectResult>(await controller.UpdateSettings(
                DisabledSettings("Updated welcome", firstUpdatedSettings.Revision),
                CancellationToken.None)).Value);
        Assert.All(await db.SiteContentBlocks.ToListAsync(), setting =>
            Assert.Equal(clock.UtcNow, setting.UpdatedAt));
        template = await db.NotificationTemplates.SingleAsync();
        Assert.Equal(initialTime, template.CreatedAt);
        Assert.Equal(clock.UtcNow, template.UpdatedAt);
        Assert.Equal(clock.UtcNow, updatedSettings.GeneratedAt);
        Assert.Equal(2, updatedSettings.Revision);

        var unchanged = await controller.UpdateSettings(
            DisabledSettings("Updated welcome", updatedSettings.Revision),
            CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(unchanged);
        Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.Action == "telegram_bot.settings.update"));
    }

    [Fact]
    public async Task Telegram_Bot_Settings_Should_Reject_Stale_Admin_Snapshot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2033, 3, 4, 5, 6, 7, TimeSpan.Zero));
        var controller = new AdminTelegramBotSettingsController(db, EmptyConfiguration(), new TestSecretProtector(), clock);

        var firstSnapshot = Assert.IsType<AdminTelegramBotSettingsDto>(
            Assert.IsType<OkObjectResult>(await controller.GetSettings(CancellationToken.None)).Value);
        var staleSnapshot = Assert.IsType<AdminTelegramBotSettingsDto>(
            Assert.IsType<OkObjectResult>(await controller.GetSettings(CancellationToken.None)).Value);
        Assert.Equal(firstSnapshot.GeneratedAt, staleSnapshot.GeneratedAt);

        Assert.IsType<OkObjectResult>(await controller.UpdateSettings(
            DisabledSettings("Победившее приветствие", firstSnapshot.Revision),
            CancellationToken.None));
        clock.UtcNow = clock.UtcNow.AddMinutes(1);

        var staleResult = await controller.UpdateSettings(
            DisabledSettings("Устаревшее приветствие", staleSnapshot.Revision),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(staleResult);
        Assert.Equal("Победившее приветствие", (await db.NotificationTemplates.SingleAsync()).Body);
        Assert.Single(await db.AuditLogs.Where(x => x.Action == "telegram_bot.settings.update").ToListAsync());
    }

    [Fact]
    public async Task Telegram_Bot_Settings_Should_Roll_Back_When_Revision_Changes_During_Save()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-telegram-settings-race-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"Data Source={databasePath}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                var initial = await new AdminTelegramBotSettingsController(seed, EmptyConfiguration(), new TestSecretProtector())
                    .UpdateSettings(DisabledSettings("Исходное приветствие", 0), CancellationToken.None);
                Assert.IsType<OkObjectResult>(initial);
            }

            var interceptor = new BeforeTransactionInterceptor(async () =>
            {
                await using var competitor = new ApplicationDbContext(options);
                var winner = await new AdminTelegramBotSettingsController(competitor, EmptyConfiguration(), new TestSecretProtector())
                    .UpdateSettings(DisabledSettings("Параллельный победитель", 1), CancellationToken.None);
                Assert.IsType<OkObjectResult>(winner);
            });
            var raceOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .AddInterceptors(interceptor)
                .Options;
            await using var db = new ApplicationDbContext(raceOptions);

            var staleResult = await new AdminTelegramBotSettingsController(db, EmptyConfiguration(), new TestSecretProtector())
                .UpdateSettings(DisabledSettings("Устаревшая запись", 1), CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(staleResult);
            await using var verify = new ApplicationDbContext(options);
            Assert.Equal("Параллельный победитель", (await verify.NotificationTemplates.AsNoTracking().SingleAsync()).Body);
            Assert.Equal(2, await verify.SiteContentBlocks.AsNoTracking()
                .Where(x => x.Key == "telegram_bot.revision")
                .Select(x => x.Revision)
                .SingleAsync());
            Assert.Equal(2, await verify.AuditLogs.AsNoTracking().CountAsync(x => x.Action == "telegram_bot.settings.update"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

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
            SubscriptionExpiredTextTemplate: "Подписка закончилась",
            Revision: 0), CancellationToken.None);

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
        var settingsAudit = await db.AuditLogs.SingleAsync(x => x.Action == "telegram_bot.settings.update");
        var settingsAuditJson = $"{settingsAudit.BeforeJson}\n{settingsAudit.AfterJson}";
        Assert.Contains("botTokenRotated", settingsAuditJson, StringComparison.Ordinal);
        Assert.DoesNotContain("telegram-secret", settingsAuditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook-secret", settingsAuditJson, StringComparison.OrdinalIgnoreCase);

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

        var withoutRevision = await controller.UpdateSettings(
            DisabledSettings("Без версии", 0) with { Revision = null },
            CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(withoutRevision);

        var withUnknownField = await controller.UpdateSettings(
            DisabledSettings("Неизвестное поле", 0) with
            {
                AdditionalFields = new Dictionary<string, JsonElement>
                {
                    ["botTokne"] = JsonSerializer.Deserialize<JsonElement>("true")
                }
            },
            CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(withUnknownField);

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
            SubscriptionExpiredTextTemplate: null,
            Revision: 0), CancellationToken.None);
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
            SubscriptionExpiredTextTemplate: null,
            Revision: 0), CancellationToken.None);
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
            SubscriptionExpiredTextTemplate: null,
            Revision: 0), CancellationToken.None);
        Assert.Contains("WebApp URL", JsonSerializer.Serialize(Assert.IsType<BadRequestObjectResult>(invalidWebApp).Value), StringComparison.Ordinal);

        var credentialBearingWebhook = await controller.UpdateSettings(new UpdateTelegramBotSettingsCommand(
            Enabled: false,
            Mode: "LongPolling",
            PublicBotUsername: "vpn_ready_bot",
            BotToken: null,
            WebhookUrl: "https://operator:secret@api.example.test/webhook",
            SecretToken: null,
            AdminChatId: null,
            WebAppUrl: null,
            WelcomeText: null,
            InstructionText: null,
            SupportText: null,
            AfterPaymentTextTemplate: null,
            RenewalTextTemplate: null,
            PaymentFailedTextTemplate: null,
            SubscriptionExpiredTextTemplate: null,
            Revision: 0), CancellationToken.None);
        Assert.Contains("логин", ReadError(credentialBearingWebhook), StringComparison.OrdinalIgnoreCase);

        var credentialBearingWebApp = await controller.UpdateSettings(new UpdateTelegramBotSettingsCommand(
            Enabled: false,
            Mode: "LongPolling",
            PublicBotUsername: "vpn_ready_bot",
            BotToken: null,
            WebhookUrl: null,
            SecretToken: null,
            AdminChatId: null,
            WebAppUrl: "https://operator:secret@cabinet.example.test",
            WelcomeText: null,
            InstructionText: null,
            SupportText: null,
            AfterPaymentTextTemplate: null,
            RenewalTextTemplate: null,
            PaymentFailedTextTemplate: null,
            SubscriptionExpiredTextTemplate: null,
            Revision: 0), CancellationToken.None);
        Assert.Contains("логин", ReadError(credentialBearingWebApp), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.SiteContentBlocks.ToListAsync());
    }

    private static string ReadError(IActionResult result)
    {
        var value = Assert.IsType<BadRequestObjectResult>(result).Value;
        Assert.NotNull(value);
        var property = value.GetType().GetProperty("error");
        Assert.NotNull(property);
        return Assert.IsType<string>(property.GetValue(value));
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    private static UpdateTelegramBotSettingsCommand DisabledSettings(string welcomeText, int revision)
        => new(
            Enabled: false,
            Mode: "LongPolling",
            PublicBotUsername: "vpn_clock_bot",
            BotToken: null,
            WebhookUrl: null,
            SecretToken: null,
            AdminChatId: null,
            WebAppUrl: null,
            WelcomeText: welcomeText,
            InstructionText: null,
            SupportText: null,
            AfterPaymentTextTemplate: null,
            RenewalTextTemplate: null,
            PaymentFailedTextTemplate: null,
            SubscriptionExpiredTextTemplate: null,
            Revision: revision);

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

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

    private sealed class BeforeTransactionInterceptor(Func<Task> beforeTransaction) : DbTransactionInterceptor
    {
        private int _injected;

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _injected, 1) == 0)
            {
                await beforeTransaction();
            }

            return result;
        }
    }
}
