using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Common;

public static class PaymentProviderConfigurationRules
{
    public static bool SupportsWebCheckout(PaymentProvider provider)
        => provider != PaymentProvider.TelegramStars;

    public static bool SupportsTelegramCheckout(PaymentProvider provider)
        => provider == PaymentProvider.TelegramStars;

    public static bool IsCheckoutConfigured(PaymentProviderAccount account)
        => GetCheckoutConfigurationIssue(account) is null;

    public static bool IsWebCheckoutConfigured(PaymentProviderAccount account)
        => SupportsWebCheckout(account.Provider) && IsCheckoutConfigured(account);

    public static string? GetCheckoutConfigurationIssue(PaymentProviderAccount account)
    {
        if (!account.IsEnabled)
        {
            return "Provider account is disabled.";
        }

        if (account.Mode == PaymentProviderMode.Disabled)
        {
            return "Provider mode is Disabled.";
        }

        if (!SupportsWebCheckout(account.Provider))
        {
            return "Telegram Stars is available only inside the Telegram bot checkout flow.";
        }

        if (string.IsNullOrWhiteSpace(account.ShopId))
        {
            return "ShopId / merchant identifier is required before checkout can use this provider.";
        }

        if (account.Mode == PaymentProviderMode.Production && string.IsNullOrWhiteSpace(account.SecretKeyProtected))
        {
            return "Production checkout requires a protected secret key.";
        }

        return null;
    }

    public static string? GetBotCheckoutConfigurationIssue(PaymentProviderAccount account)
    {
        if (account.Provider != PaymentProvider.TelegramStars)
        {
            return GetCheckoutConfigurationIssue(account);
        }

        if (!account.IsEnabled)
        {
            return "Provider account is disabled.";
        }

        if (account.Mode == PaymentProviderMode.Disabled)
        {
            return "Provider mode is Disabled.";
        }

        return null;
    }

    public static bool IsBotCheckoutConfigured(PaymentProviderAccount account)
        => GetBotCheckoutConfigurationIssue(account) is null;

    public static string GetCapabilitiesJson(PaymentProvider provider)
    {
        var capabilities = provider switch
        {
            PaymentProvider.YooKassa => new[] { "createPayment", "webhook", "signatureValidation", "refund", "recheck", "sandbox", "live" },
            PaymentProvider.TBankAcquiring => new[] { "createPayment", "webhook", "signatureValidation", "refund", "recheck", "sandbox", "live" },
            PaymentProvider.RoboKassa => new[] { "createPayment", "webhook", "signatureValidation", "sandbox", "live" },
            PaymentProvider.Prodamus => new[] { "createPayment", "webhook", "signatureValidation", "sandbox", "live" },
            PaymentProvider.YooMoney => new[] { "createPayment", "webhook", "signatureValidation", "sandbox", "live" },
            PaymentProvider.TelegramStars => new[] { "telegramNative", "webhook", "sandboxNotSupported", "live" },
            PaymentProvider.Stripe => new[] { "createPayment", "webhook", "signatureValidation", "refund", "recheck", "sandbox", "live" },
            PaymentProvider.PayPal => new[] { "createPayment", "webhook", "signatureValidation", "refund", "recheck", "sandbox", "live" },
            PaymentProvider.CloudPayments => new[] { "createPayment", "webhook", "signatureValidation", "sandbox", "live" },
            _ => Array.Empty<string>()
        };

        return System.Text.Json.JsonSerializer.Serialize(capabilities);
    }
}
