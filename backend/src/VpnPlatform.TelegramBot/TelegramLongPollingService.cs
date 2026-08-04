using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VpnPlatform.TelegramBot;

public class TelegramLongPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramHttpClient _client;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramLongPollingService> _logger;

    public TelegramLongPollingService(IServiceScopeFactory scopeFactory, TelegramHttpClient client, IOptions<TelegramBotOptions> options, ILogger<TelegramLongPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !string.Equals(_options.Mode, "LongPolling", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Telegram long polling is disabled. Enabled={Enabled}, Mode={Mode}", _options.Enabled, _options.Mode);
            return;
        }

        if (!_client.IsEnabledAndConfigured)
        {
            _logger.LogWarning("Telegram long polling is enabled but BotToken is missing.");
            return;
        }

        var offset = await LoadInitialOffsetAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecoverPendingDeliveriesAsync(stoppingToken);
                var updates = await _client.GetUpdatesAsync(offset, stoppingToken);
                foreach (var raw in updates)
                {
                    var updateId = ExtractUpdateId(raw);
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
                    var delivery = scope.ServiceProvider.GetRequiredService<TelegramUpdateDeliveryService>();
                    var result = await processor.ProcessUpdateAsync(raw, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null, stoppingToken);
                    if (result.IsSuccess && result.Value is { UpdateId: { } processedUpdateId })
                    {
                        var deliveryResult = await delivery.DeliverAsync(processedUpdateId, stoppingToken);
                        if (!deliveryResult.IsSuccess)
                        {
                            _logger.LogWarning(
                                "Telegram update {UpdateId} response delivery deferred: {Error}",
                                processedUpdateId,
                                deliveryResult.Error);
                        }
                    }
                    else if (result.IsRetryable)
                    {
                        throw new InvalidOperationException($"Telegram update requires retry: {result.Error}");
                    }

                    if (updateId.HasValue)
                    {
                        offset = Math.Max(offset, updateId.Value + 1);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram long polling iteration failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RecoverPendingDeliveriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var delivery = scope.ServiceProvider.GetRequiredService<TelegramUpdateDeliveryService>();
        var updateIds = await delivery.GetPendingUpdateIdsAsync(20, cancellationToken);
        foreach (var updateId in updateIds)
        {
            var result = await delivery.DeliverAsync(updateId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Pending Telegram response {UpdateId} remains deferred: {Error}", updateId, result.Error);
            }
        }
    }

    private async Task<long> LoadInitialOffsetAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var last = await db.TelegramBotUpdates.AsNoTracking().OrderByDescending(x => x.UpdateId).Select(x => (long?)x.UpdateId).FirstOrDefaultAsync(cancellationToken);
        return last.HasValue ? last.Value + 1 : 0;
    }

    private static long? ExtractUpdateId(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty("update_id", out var value) && value.TryGetInt64(out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }
}
