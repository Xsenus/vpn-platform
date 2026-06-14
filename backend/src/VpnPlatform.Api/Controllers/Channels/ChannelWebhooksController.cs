using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VpnPlatform.Api.Security;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;

namespace VpnPlatform.Api.Controllers.Channels;

[ApiController]
[Route("api/channels")]
[EnableRateLimiting(ApiRateLimitPolicies.Webhook)]
public class ChannelWebhooksController : ControllerBase
{
    private readonly TelegramBotService _telegramBotService;
    private readonly TelegramBotRuntimeSettingsService _telegramSettings;
    private readonly ITelegramInvoiceProvider _telegramProvider;

    public ChannelWebhooksController(
        TelegramBotService telegramBotService,
        TelegramBotRuntimeSettingsService telegramSettings,
        ITelegramInvoiceProvider telegramProvider)
    {
        _telegramBotService = telegramBotService;
        _telegramSettings = telegramSettings;
        _telegramProvider = telegramProvider;
    }

    [HttpPost("telegram/webhook")]
    public async Task<IActionResult> Telegram(CancellationToken cancellationToken)
    {
        var settings = await _telegramSettings.LoadAsync(cancellationToken);
        if (!settings.Enabled)
        {
            return NotFound(new { error = "telegram_bot_disabled" });
        }

        if (!string.Equals(settings.Mode, "Webhook", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "telegram_bot_not_in_webhook_mode" });
        }

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var headers = Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var result = await _telegramBotService.ProcessUpdateAsync(rawBody, headers, settings.SecretToken, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { error = result.Error });
        }

        if (!string.IsNullOrWhiteSpace(result.Value.PreCheckoutQueryId) && result.Value.PreCheckoutOk.HasValue)
        {
            await _telegramProvider.AnswerPreCheckoutQueryAsync(
                result.Value.PreCheckoutQueryId,
                result.Value.PreCheckoutOk.Value,
                result.Value.PreCheckoutError,
                cancellationToken);
        }

        if (result.Value.Processed && result.Value.ChatId.HasValue && !string.IsNullOrWhiteSpace(result.Value.ResponseText))
        {
            await _telegramProvider.SendMessageAsync(
                result.Value.ChatId.Value,
                result.Value.ResponseText,
                result.Value.ReplyMarkupJson,
                cancellationToken);
        }

        return Ok(new { status = result.Value.Processed ? "processed" : "duplicate" });
    }

    [HttpPost("discord/webhook")]
    public IActionResult Discord([FromBody] object payload) => StatusCode(StatusCodes.Status501NotImplemented, new { error = "discord_channel_not_configured" });

    [HttpPost("vk/webhook")]
    public IActionResult Vk([FromBody] object payload) => StatusCode(StatusCodes.Status501NotImplemented, new { error = "vk_channel_not_configured" });

    [HttpPost("whatsapp/webhook")]
    public IActionResult WhatsApp([FromBody] object payload) => StatusCode(StatusCodes.Status501NotImplemented, new { error = "whatsapp_channel_not_configured" });
}
