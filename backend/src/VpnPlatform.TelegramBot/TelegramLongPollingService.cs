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
                var updates = await _client.GetUpdatesAsync(offset, stoppingToken);
                foreach (var raw in updates)
                {
                    var updateId = ExtractUpdateId(raw);
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
                    var result = await processor.ProcessUpdateAsync(raw, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null, stoppingToken);
                    if (result.IsSuccess && result.Value is { } processed)
                    {
                        if (!string.IsNullOrWhiteSpace(processed.PreCheckoutQueryId) && processed.PreCheckoutOk.HasValue)
                        {
                            await _client.AnswerPreCheckoutQueryAsync(processed.PreCheckoutQueryId, processed.PreCheckoutOk.Value, processed.PreCheckoutError, stoppingToken);
                        }

                        if (processed.Processed && processed.ChatId.HasValue && !string.IsNullOrWhiteSpace(processed.ResponseText))
                        {
                            await _client.SendMessageAsync(processed.ChatId.Value, processed.ResponseText, processed.ReplyMarkupJson, stoppingToken);
                        }
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
