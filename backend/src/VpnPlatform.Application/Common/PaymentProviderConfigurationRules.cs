using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Common;

public sealed record PaymentProviderCapabilityRule(string Key, string Label, bool Supported, string Status);
public sealed record PaymentRefundConfigurationIssue(string Code, string Message);

public static class PaymentProviderConfigurationRules
{
    public const string TelegramStarsStatusKey = "status";
    public const string TelegramStarsBotOnlyStatus = "bot-only";
    public const string TelegramStarsInvoiceFlowStatus = "invoice-flow";

    public static bool SupportsWebCheckout(PaymentProvider provider)
        => provider != PaymentProvider.TelegramStars;

    public static bool SupportsTelegramCheckout(PaymentProvider provider)
        => provider == PaymentProvider.TelegramStars;

    public static bool SupportsManualRecheck(PaymentProvider provider)
        => provider is PaymentProvider.YooKassa or PaymentProvider.TBankAcquiring or PaymentProvider.Stripe or PaymentProvider.PayPal;

    public static bool SupportsRefund(PaymentProvider provider)
        => provider is PaymentProvider.YooKassa or PaymentProvider.TBankAcquiring or PaymentProvider.Stripe or PaymentProvider.PayPal;

    public static bool IsCredentiallessLocalSandbox(PaymentProviderAccount account, string? environmentName)
        => account.Mode == PaymentProviderMode.Sandbox
           && string.IsNullOrWhiteSpace(account.SecretKeyProtected)
           && environmentName is not null
           && (environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase)
               || environmentName.Equals("Local", StringComparison.OrdinalIgnoreCase)
               || environmentName.Equals("Test", StringComparison.OrdinalIgnoreCase)
               || environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase)
               || environmentName.Equals("Sandbox", StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<PaymentRefundConfigurationIssue> GetRefundConfigurationIssues(
        PaymentAttempt payment,
        PaymentProviderAccount? account,
        string? environmentName)
    {
        var issues = new List<PaymentRefundConfigurationIssue>();
        if (account is null || payment.PaymentProviderAccountId is null)
        {
            issues.Add(new("account_missing", "Платеж не связан с аккаунтом платежного провайдера."));
            return issues;
        }

        if (account.Provider != payment.Provider)
        {
            issues.Add(new("provider_mismatch", "Аккаунт провайдера не совпадает с провайдером платежа."));
        }

        if (account.Mode != payment.ProviderMode)
        {
            issues.Add(new("provider_mode_mismatch", "Режим аккаунта провайдера не совпадает с режимом исходного платежа."));
        }

        if (!account.IsEnabled)
        {
            issues.Add(new("account_disabled", "Аккаунт платежного провайдера выключен."));
        }

        if (account.Mode == PaymentProviderMode.Disabled)
        {
            issues.Add(new("mode_disabled", "Аккаунт платежного провайдера находится в режиме Disabled."));
        }

        if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
        {
            issues.Add(new("provider_payment_id_missing", "Не сохранен идентификатор платежа у провайдера."));
        }

        if (IsCredentiallessLocalSandbox(account, environmentName))
        {
            return issues;
        }

        var hasShopId = !string.IsNullOrWhiteSpace(account.ShopId);
        var hasSecret = !string.IsNullOrWhiteSpace(account.SecretKeyProtected);
        switch (payment.Provider)
        {
            case PaymentProvider.YooKassa:
                if (!hasShopId)
                {
                    issues.Add(new("shop_id_missing", "Для возврата YooKassa нужен ShopId."));
                }
                if (!hasSecret)
                {
                    issues.Add(new("secret_missing", "Для возврата YooKassa нужен SecretKey."));
                }
                break;
            case PaymentProvider.TBankAcquiring:
                if (!hasShopId)
                {
                    issues.Add(new("shop_id_missing", "Для возврата TBank нужен TerminalKey."));
                }
                if (!hasSecret)
                {
                    issues.Add(new("secret_missing", "Для возврата TBank нужен Password терминала."));
                }
                break;
            case PaymentProvider.Stripe:
                if (!hasSecret)
                {
                    issues.Add(new("secret_missing", "Для возврата Stripe нужен SecretKey."));
                }
                break;
            case PaymentProvider.PayPal:
                if (!hasShopId)
                {
                    issues.Add(new("shop_id_missing", "Для возврата PayPal нужен Client ID."));
                }
                if (!hasSecret)
                {
                    issues.Add(new("secret_missing", "Для возврата PayPal нужен Client secret."));
                }
                break;
        }

        return issues;
    }

    public static bool IsCheckoutConfigured(PaymentProviderAccount account)
        => GetCheckoutConfigurationIssue(account) is null;

    public static bool IsWebCheckoutConfigured(PaymentProviderAccount account)
        => SupportsWebCheckout(account.Provider) && IsCheckoutConfigured(account);

    public static PaymentProviderAccount? SelectWebCheckoutAccount(
        IEnumerable<PaymentProviderAccount> accounts,
        PaymentProvider provider)
        => accounts
            .Where(x => x.Provider == provider && IsWebCheckoutConfigured(x))
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

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

        foreach (var (value, fieldName) in new[]
                 {
                     (account.ApiBaseUrl, "API base URL"),
                     (account.ReturnUrl, "Return URL"),
                     (account.WebhookUrl, "Webhook URL"),
                     (ReadExtraSetting(account.ExtraSettingsJson, "hostedCheckoutUrl"), "hostedCheckoutUrl")
                 })
        {
            var issue = GetOptionalSafeHttpUrlIssue(value, fieldName);
            if (issue is not null)
            {
                return issue;
            }
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

    private static string? GetOptionalSafeHttpUrlIssue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (SafeHttpUrl.ContainsCredentials(value))
        {
            return $"{fieldName} must not contain credentials (login or password).";
        }

        return SafeHttpUrl.TryNormalize(value, out _)
            ? null
            : $"{fieldName} must be an absolute http/https URL.";
    }

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
        var supportsRefund = SupportsRefund(provider);
        var supportsRecheck = SupportsManualRecheck(provider);
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
