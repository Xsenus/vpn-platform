using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;

namespace VpnPlatform.Infrastructure.Services;

public sealed class DisabledTelegramInvoiceProvider : ITelegramInvoiceProvider
{
    public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Telegram invoice provider is not configured in this service. Run VpnPlatform.TelegramBot with TelegramBot:BotToken to send invoices.");

    public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
