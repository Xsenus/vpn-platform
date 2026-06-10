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
            PublicName = "Telegram Stars"
        };

        Assert.False(PaymentProviderConfigurationRules.SupportsWebCheckout(account.Provider));
        Assert.True(PaymentProviderConfigurationRules.SupportsTelegramCheckout(account.Provider));
        Assert.False(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
        Assert.True(PaymentProviderConfigurationRules.IsBotCheckoutConfigured(account));
        Assert.Contains("Telegram bot", PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void Capability_Rules_Should_Report_Unsupported_Features()
    {
        var robokassa = PaymentProviderConfigurationRules.GetCapabilityRules(PaymentProvider.RoboKassa);

        Assert.Contains(robokassa, x => x.Key == "createPayment" && x.Supported);
        Assert.Contains(robokassa, x => x.Key == "refund" && !x.Supported);
        Assert.Contains(robokassa, x => x.Key == "recheck" && !x.Supported);
    }
}
