using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Common;

public sealed record PaymentProviderCapabilityRule(string Key, string Label, bool Supported, string Status);

public static class PaymentProviderConfigurationRules
{
    public const string TelegramStarsStatusKey = "status";
    public const string TelegramStarsBotOnlyStatus = "bot-only";
    public const string TelegramStarsInvoiceFlowStatus = "invoice-flow";

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

        if (account.Provider == PaymentProvider.CloudPayments && string.IsNullOrWhiteSpace(ReadExtraSetting(account.ExtraSettingsJson, "hostedCheckoutUrl")))
        {
            return "CloudPayments checkout requires ExtraSettingsJson.hostedCheckoutUrl with a merchant-hosted widget page.";
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

        if (string.IsNullOrWhiteSpace(account.ShopId))
        {
            return "Telegram Stars bot username is required before invoice flow can be enabled.";
        }

        var status = ReadExtraSetting(account.ExtraSettingsJson, TelegramStarsStatusKey);
        if (!string.Equals(status, TelegramStarsInvoiceFlowStatus, StringComparison.OrdinalIgnoreCase))
        {
            return "Telegram Stars invoice flow must be explicitly enabled with ExtraSettingsJson.status = \"invoice-flow\".";
        }

        return null;
    }

    public static bool IsBotCheckoutConfigured(PaymentProviderAccount account)
        => GetBotCheckoutConfigurationIssue(account) is null;

    public static string GetCapabilitiesJson(PaymentProvider provider)
    {
        var capabilities = GetCapabilityRules(provider)
            .Where(x => x.Supported)
            .Select(x => x.Key)
            .ToArray();

        return System.Text.Json.JsonSerializer.Serialize(capabilities);
    }

    public static IReadOnlyCollection<PaymentProviderCapabilityRule> GetCapabilityRules(PaymentProvider provider)
    {
        var supportsWebCheckout = SupportsWebCheckout(provider);
        var supportsRefund = provider is PaymentProvider.YooKassa or PaymentProvider.TBankAcquiring or PaymentProvider.Stripe or PaymentProvider.PayPal;
        var supportsRecheck = provider is PaymentProvider.YooKassa or PaymentProvider.TBankAcquiring or PaymentProvider.Stripe or PaymentProvider.PayPal;
        var supportsSandbox = provider != PaymentProvider.TelegramStars;

        return new[]
        {
            new PaymentProviderCapabilityRule("createPayment", "Создание платежа", supportsWebCheckout, supportsWebCheckout ? "supported" : "bot_only"),
            new PaymentProviderCapabilityRule("telegramNative", "Telegram invoice", provider == PaymentProvider.TelegramStars, provider == PaymentProvider.TelegramStars ? "supported" : "not_applicable"),
            new PaymentProviderCapabilityRule("webhook", "Webhook / уведомления", true, "supported"),
            new PaymentProviderCapabilityRule("signatureValidation", "Проверка подписи", provider != PaymentProvider.TelegramStars, provider == PaymentProvider.TelegramStars ? "telegram_update" : "supported"),
            new PaymentProviderCapabilityRule("refund", "Возвраты", supportsRefund, supportsRefund ? "supported" : "not_supported"),
            new PaymentProviderCapabilityRule("recheck", "Ручная перепроверка", supportsRecheck, supportsRecheck ? "supported" : "not_supported"),
            new PaymentProviderCapabilityRule("sandbox", "Sandbox-режим", supportsSandbox, supportsSandbox ? "supported" : "not_supported"),
            new PaymentProviderCapabilityRule("live", "Production-режим", true, provider == PaymentProvider.TelegramStars ? "requires_telegram_bot" : "supported")
        };
    }

    public static string? ReadExtraSetting(string extraSettingsJson, string key)
    {
        if (string.IsNullOrWhiteSpace(extraSettingsJson))
        {
            return null;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(extraSettingsJson);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && document.RootElement.TryGetProperty(key, out var value)
                && value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? value.GetString()
                    : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
