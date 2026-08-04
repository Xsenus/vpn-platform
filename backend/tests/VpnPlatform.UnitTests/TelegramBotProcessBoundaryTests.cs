using Xunit;

namespace VpnPlatform.UnitTests;

public class TelegramBotProcessBoundaryTests
{
    [Fact]
    public void Standalone_TelegramBot_Process_Should_Not_Map_Webhook_Route()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.TelegramBot", "Program.cs"));

        Assert.DoesNotContain("MapPost(\"/telegram/webhook\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ProcessUpdateAsync(rawBody", program, StringComparison.Ordinal);
        Assert.Contains("TelegramLongPollingService", program, StringComparison.Ordinal);
        Assert.Contains("TelegramNotificationDispatcherService", program, StringComparison.Ordinal);
        Assert.Contains("/health/live", program, StringComparison.Ordinal);
        Assert.Contains("/health/ready", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Telegram_Documentation_Should_Point_Webhook_To_Main_Api()
    {
        var root = FindRepositoryRoot();
        var setup = File.ReadAllText(Path.Combine(root, "docs", "telegram-bot-setup.md"));
        var foundation = File.ReadAllText(Path.Combine(root, "docs", "phase-3-telegram-foundation.md"));
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var productionExample = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.TelegramBot", "appsettings.Production.example.json"));

        foreach (var document in new[] { setup, foundation, productionExample })
        {
            Assert.Contains("/api/channels/telegram/webhook", document, StringComparison.Ordinal);
        }

        Assert.Contains("main API", setup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Webhook mode is handled by the main API", foundation, StringComparison.Ordinal);
        Assert.Contains("LongPolling", readme, StringComparison.Ordinal);
        Assert.Contains("webhook", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Telegram_Long_Polling_Should_Not_Advance_Offset_For_Retryable_Update()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.TelegramBot", "TelegramLongPollingService.cs"));

        Assert.Contains("result.IsRetryable", program, StringComparison.Ordinal);
        Assert.Contains("Telegram update requires retry", program, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && File.Exists(Path.Combine(directory.FullName, "CHANGELOG.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for Telegram bot process boundary tests.");
    }
}
