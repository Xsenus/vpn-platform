using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;

namespace VpnPlatform.Infrastructure.Services;

public sealed class DisabledTelegramInvoiceProvider : ITelegramInvoiceProvider
{
    public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Telegram invoice provider is not configured. Set Telegram BotToken in admin settings or app configuration to send invoices.");

    public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
