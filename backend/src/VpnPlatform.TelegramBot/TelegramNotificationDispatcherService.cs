using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VpnPlatform.TelegramBot;

public class TelegramNotificationDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramHttpClient _client;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramNotificationDispatcherService> _logger;

    public TelegramNotificationDispatcherService(IServiceScopeFactory scopeFactory, TelegramHttpClient client, IOptions<TelegramBotOptions> options, ILogger<TelegramNotificationDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var now = DateTimeOffset.UtcNow;
                var notifications = await db.TelegramBotNotifications
                    .Where(x => x.Status == "pending" && (!x.NextAttemptAt.HasValue || x.NextAttemptAt <= now))
                    .OrderBy(x => x.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var notification in notifications)
                {
                    var accountBlocked = await db.TelegramAccounts.AsNoTracking().AnyAsync(x => x.TelegramUserId == notification.TelegramUserId && x.IsBlocked, stoppingToken);
                    if (accountBlocked)
                    {
                        notification.Status = "cancelled";
                        notification.ErrorText = "Telegram account is blocked.";
                        continue;
                    }

                    var payload = ExtractPayload(notification.PayloadJson);
                    try
                    {
                        notification.Status = "sending";
                        notification.AttemptCount += 1;
                        await db.SaveChangesAsync(stoppingToken);

                        await _client.SendMessageAsync(notification.TelegramUserId, payload.Text, payload.ReplyMarkupJson, stoppingToken);
                        notification.Status = "sent";
                        notification.SentAt = DateTimeOffset.UtcNow;
                        notification.ErrorText = string.Empty;
                    }
                    catch (Exception ex)
                    {
                        notification.Status = notification.AttemptCount >= 5 ? "failed" : "pending";
                        notification.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, notification.AttemptCount) * 5));
                        notification.ErrorText = ex.Message;
                        _logger.LogWarning(ex, "Failed to dispatch Telegram notification {NotificationId}", notification.Id);
                    }
                }

                if (notifications.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram notification dispatcher failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private static (string Text, string? ReplyMarkupJson) ExtractPayload(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var text = doc.RootElement.TryGetProperty("text", out var textElement)
                ? textElement.GetString() ?? string.Empty
                : payloadJson;
            var replyMarkupJson = doc.RootElement.TryGetProperty("replyMarkupJson", out var replyMarkupElement)
                ? replyMarkupElement.GetString()
                : null;
            return (text, string.IsNullOrWhiteSpace(replyMarkupJson) ? null : replyMarkupJson);
        }
        catch
        {
            return (payloadJson, null);
        }
    }
}
