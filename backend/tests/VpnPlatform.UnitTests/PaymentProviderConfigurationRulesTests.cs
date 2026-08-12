using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentProviderConfigurationRulesTests
{
    [Fact]
    public void TelegramStars_Should_Be_Bot_Only_And_Hidden_From_Web_Checkout()
    {
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.TelegramStars,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            Name = "telegram-stars",
            PublicName = "Telegram Stars",
            ShopId = "vpnplatform_bot",
            ExtraSettingsJson = """{"status":"bot-only"}"""
        };

        Assert.False(PaymentProviderConfigurationRules.SupportsWebCheckout(account.Provider));
        Assert.True(PaymentProviderConfigurationRules.SupportsTelegramCheckout(account.Provider));
        Assert.False(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
        Assert.False(PaymentProviderConfigurationRules.IsBotCheckoutConfigured(account));
        Assert.Contains("Telegram bot", PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("invoice-flow", PaymentProviderConfigurationRules.GetBotCheckoutConfigurationIssue(account) ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        account.ExtraSettingsJson = """{"status":"invoice-flow"}""";

        Assert.True(PaymentProviderConfigurationRules.IsBotCheckoutConfigured(account));
        Assert.Null(PaymentProviderConfigurationRules.GetBotCheckoutConfigurationIssue(account));
    }

    [Fact]
    public void TelegramStars_Should_Require_Bot_Username_For_Invoice_Flow()
    {
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.TelegramStars,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            Name = "telegram-stars",
            PublicName = "Telegram Stars",
            ExtraSettingsJson = """{"status":"invoice-flow"}"""
        };

        Assert.False(PaymentProviderConfigurationRules.IsBotCheckoutConfigured(account));
        Assert.Contains("bot username", PaymentProviderConfigurationRules.GetBotCheckoutConfigurationIssue(account) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Web_Provider_Should_Require_ShopId_And_Production_Secret()
    {
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.Stripe,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true,
            Name = "stripe-live",
            PublicName = "Stripe"
        };

        Assert.False(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
        Assert.Contains("ShopId", PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account) ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        account.ShopId = "acct_test";
        Assert.False(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
        Assert.Contains("secret", PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account) ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        account.SecretKeyProtected = "protected-secret";
        Assert.True(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
    }

    [Fact]
    public void CloudPayments_Should_Require_Hosted_Checkout_Url()
    {
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.CloudPayments,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            Name = "cloudpayments",
            PublicName = "CloudPayments",
            ShopId = "public-id",
            ExtraSettingsJson = "{}"
        };

        Assert.False(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
        Assert.Contains("hostedCheckoutUrl", PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account) ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        account.ExtraSettingsJson = """{"hostedCheckoutUrl":"https://pay.example.test/cloudpayments"}""";

        Assert.True(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
    }

    [Theory]
    [InlineData("https://operator:secret@api.example.test", "https://cabinet.example.test/payments", "https://api.example.test/webhook", "credentials")]
    [InlineData("ftp://api.example.test", "https://cabinet.example.test/payments", "https://api.example.test/webhook", "http")]
    [InlineData("https://api.example.test", "javascript:alert(1)", "https://api.example.test/webhook", "http")]
    public void Web_Provider_Should_Reject_Unsafe_Legacy_Urls(
        string apiBaseUrl,
        string returnUrl,
        string webhookUrl,
        string expectedIssue)
    {
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            ShopId = "shop",
            ApiBaseUrl = apiBaseUrl,
            ReturnUrl = returnUrl,
            WebhookUrl = webhookUrl
        };

        Assert.False(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
        Assert.Contains(expectedIssue, PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Capability_Rules_Should_Report_Unsupported_Features()
    {
        var robokassa = PaymentProviderConfigurationRules.GetCapabilityRules(PaymentProvider.RoboKassa);

        Assert.Contains(robokassa, x => x.Key == "createPayment" && x.Supported);
        Assert.Contains(robokassa, x => x.Key == "refund" && !x.Supported);
        Assert.Contains(robokassa, x => x.Key == "recheck" && !x.Supported);
        Assert.False(PaymentProviderConfigurationRules.SupportsManualRecheck(PaymentProvider.RoboKassa));
        Assert.True(PaymentProviderConfigurationRules.SupportsManualRecheck(PaymentProvider.YooKassa));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Local")]
    [InlineData("Test")]
    [InlineData("Testing")]
    [InlineData("Sandbox")]
    public void Credentialless_Sandbox_Should_Be_Local_Only_In_Allowed_Environments(string environmentName)
    {
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.Stripe,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            ShopId = "local-stripe-account"
        };

        Assert.True(PaymentProviderConfigurationRules.IsCredentiallessLocalSandbox(account, environmentName));
    }

    [Fact]
    public void Credentialless_LocalSandbox_Rule_Should_Fail_Closed_In_Production_Or_With_Secret()
    {
        var account = new PaymentProviderAccount
        {
            Provider = PaymentProvider.Stripe,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            ShopId = "local-stripe-account"
        };

        Assert.False(PaymentProviderConfigurationRules.IsCredentiallessLocalSandbox(account, "Production"));
        Assert.False(PaymentProviderConfigurationRules.IsCredentiallessLocalSandbox(account, null));

        account.SecretKeyProtected = "protected-secret";
        Assert.False(PaymentProviderConfigurationRules.IsCredentiallessLocalSandbox(account, "Local"));
    }

    [Theory]
    [InlineData(PaymentProvider.YooKassa, true)]
    [InlineData(PaymentProvider.TBankAcquiring, true)]
    [InlineData(PaymentProvider.Stripe, false)]
    [InlineData(PaymentProvider.PayPal, true)]
    public void Refund_Configuration_Should_Require_Provider_Credentials_Outside_LocalSandbox(
        PaymentProvider provider,
        bool requiresShopId)
    {
        var account = new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            Mode = PaymentProviderMode.Production,
            IsEnabled = true
        };
        var payment = new PaymentAttempt
        {
            PaymentProviderAccountId = account.Id,
            PaymentProviderAccount = account,
            Provider = provider,
            ProviderMode = PaymentProviderMode.Production,
            ProviderPaymentId = "provider-payment-id",
            Status = PaymentStatus.Succeeded,
            Amount = 100m,
            Currency = "RUB"
        };

        var issues = PaymentProviderConfigurationRules.GetRefundConfigurationIssues(payment, account, "Production");

        Assert.Contains(issues, x => x.Code == "secret_missing");
        Assert.Equal(requiresShopId, issues.Any(x => x.Code == "shop_id_missing"));
    }
}
