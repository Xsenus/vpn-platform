using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;

namespace VpnPlatform.Infrastructure.Services;

public sealed class TelegramBotHttpClient : ITelegramInvoiceProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly TelegramBotRuntimeSettingsService _settings;
    private readonly ILogger<TelegramBotHttpClient> _logger;

    public TelegramBotHttpClient(
        HttpClient httpClient,
        TelegramBotRuntimeSettingsService settings,
        ILogger<TelegramBotHttpClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken)
    {
        var settings = await _settings.LoadAsync(cancellationToken);
        if (!IsEnabledAndConfigured(settings))
        {
            throw new InvalidOperationException("Telegram BotToken is required to create Telegram Stars invoice.");
        }

        var body = new Dictionary<string, object?>
        {
            ["chat_id"] = request.TelegramUserId,
            ["title"] = request.Title,
            ["description"] = request.Description,
            ["payload"] = request.Payload,
            ["currency"] = request.Currency,
            ["prices"] = new[] { new { label = request.Title, amount = request.TotalAmountMinor } }
        };

        using var response = await PostJsonAsync(settings.BotToken, "sendInvoice", body, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram sendInvoice to chat {ChatId} failed with status {StatusCode}", request.TelegramUserId, response.StatusCode);
            throw new InvalidOperationException($"Telegram sendInvoice failed with HTTP {(int)response.StatusCode}.");
        }

        return new TelegramInvoiceResult(request.Payload, raw);
    }

    public async Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken)
    {
        var settings = await _settings.LoadAsync(cancellationToken);
        if (!IsEnabledAndConfigured(settings))
        {
            throw new InvalidOperationException("Telegram BotToken is required to answer pre-checkout query.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["pre_checkout_query_id"] = preCheckoutQueryId,
            ["ok"] = ok
        };
        if (!ok && !string.IsNullOrWhiteSpace(errorMessage))
        {
            payload["error_message"] = errorMessage;
        }

        using var response = await PostJsonAsync(settings.BotToken, "answerPreCheckoutQuery", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram answerPreCheckoutQuery failed with status {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"Telegram answerPreCheckoutQuery failed with HTTP {(int)response.StatusCode}.");
        }
    }

    public async Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken)
    {
        var settings = await _settings.LoadAsync(cancellationToken);
        if (!IsEnabledAndConfigured(settings))
        {
            throw new InvalidOperationException("Telegram BotToken is required to send message.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["disable_web_page_preview"] = true
        };

        if (!string.IsNullOrWhiteSpace(replyMarkupJson))
        {
            payload["reply_markup"] = JsonSerializer.Deserialize<JsonElement>(replyMarkupJson);
        }

        using var response = await PostJsonAsync(settings.BotToken, "sendMessage", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram sendMessage to chat {ChatId} failed with status {StatusCode}", chatId, response.StatusCode);
            throw new InvalidOperationException($"Telegram sendMessage failed with HTTP {(int)response.StatusCode}.");
        }
    }

    private async Task<HttpResponseMessage> PostJsonAsync(string botToken, string method, object payload, CancellationToken cancellationToken)
    {
        var url = $"https://api.telegram.org/bot{botToken}/{method}";
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(url, content, cancellationToken);
    }

    private static bool IsEnabledAndConfigured(TelegramBotRuntimeSettings settings)
        => settings.Enabled && !string.IsNullOrWhiteSpace(settings.BotToken);
}
