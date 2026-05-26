using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace VpnPlatform.TelegramBot;

public class TelegramHttpClient : ITelegramInvoiceProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramHttpClient> _logger;

    public TelegramHttpClient(HttpClient httpClient, IOptions<TelegramBotOptions> options, ILogger<TelegramHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabledAndConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.BotToken);

    public async Task<IReadOnlyCollection<string>> GetUpdatesAsync(long offset, CancellationToken cancellationToken)
    {
        if (!IsEnabledAndConfigured)
        {
            return Array.Empty<string>();
        }

        var body = new Dictionary<string, object?>
        {
            ["offset"] = offset,
            ["timeout"] = 30,
            ["allowed_updates"] = _options.AllowedUpdates.Length == 0 ? new[] { "message", "callback_query", "pre_checkout_query" } : _options.AllowedUpdates
        };

        using var response = await PostJsonAsync("getUpdates", body, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram getUpdates failed with status {StatusCode}", response.StatusCode);
            return Array.Empty<string>();
        }

        using var doc = JsonDocument.Parse(responseBody);
        if (!doc.RootElement.TryGetProperty("ok", out var ok) || !ok.GetBoolean() || !doc.RootElement.TryGetProperty("result", out var result))
        {
            _logger.LogWarning("Telegram getUpdates returned unexpected response shape");
            return Array.Empty<string>();
        }

        return result.EnumerateArray().Select(x => x.GetRawText()).ToList();
    }

    public async Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken)
    {
        if (!IsEnabledAndConfigured)
        {
            return;
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

        using var response = await PostJsonAsync("sendMessage", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram sendMessage to chat {ChatId} failed with status {StatusCode}", chatId, response.StatusCode);
        }
    }


    public async Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken)
    {
        if (!IsEnabledAndConfigured)
        {
            return;
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

        using var response = await PostJsonAsync("answerPreCheckoutQuery", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram answerPreCheckoutQuery failed with status {StatusCode}", response.StatusCode);
        }
    }

    public async Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (!IsEnabledAndConfigured)
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

        using var response = await PostJsonAsync("sendInvoice", body, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram sendInvoice to chat {ChatId} failed with status {StatusCode}", request.TelegramUserId, response.StatusCode);
            throw new InvalidOperationException($"Telegram sendInvoice failed with HTTP {(int)response.StatusCode}.");
        }

        return new TelegramInvoiceResult(request.Payload, raw);
    }

    public async Task SendInvoiceAsync(long chatId, string title, string description, string payload, string currency, int totalAmountMinor, CancellationToken cancellationToken)
        => await CreateInvoiceAsync(new TelegramInvoiceRequest(Guid.Empty, Guid.Empty, chatId, title, description, payload, currency, totalAmountMinor), cancellationToken);

    private async Task<HttpResponseMessage> PostJsonAsync(string method, object payload, CancellationToken cancellationToken)
    {
        var url = $"https://api.telegram.org/bot{_options.BotToken}/{method}";
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(url, content, cancellationToken);
    }
}
