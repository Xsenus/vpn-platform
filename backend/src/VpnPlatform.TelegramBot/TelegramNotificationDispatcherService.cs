using Microsoft.Extensions.Options;
using VpnPlatform.Application.Services;
using VpnPlatform.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VpnPlatform.TelegramBot;

public class TelegramNotificationDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramNotificationDispatcherService> _logger;

    public TelegramNotificationDispatcherService(IServiceScopeFactory scopeFactory, IOptions<TelegramBotOptions> options, ILogger<TelegramNotificationDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
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
                var delivery = scope.ServiceProvider.GetRequiredService<TelegramNotificationDeliveryService>();
                var notificationIds = await delivery.GetDispatchableIdsAsync(20, stoppingToken);
                foreach (var notificationId in notificationIds)
                {
                    var result = await delivery.DeliverAsync(notificationId, stoppingToken);
                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Telegram notification {NotificationId} delivery deferred or failed: {Error}",
                            notificationId,
                            result.Error);
                    }
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

}
