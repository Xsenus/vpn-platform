using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Application;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentProviderContractTests
{
    private static readonly PaymentProvider[] WebCheckoutProviders =
    [
        PaymentProvider.YooMoney,
        PaymentProvider.YooKassa,
        PaymentProvider.RoboKassa,
        PaymentProvider.CloudPayments,
        PaymentProvider.TBankAcquiring,
        PaymentProvider.Prodamus,
        PaymentProvider.Stripe,
        PaymentProvider.PayPal
    ];

    public static IEnumerable<object[]> WebCheckoutProviderCases()
        => WebCheckoutProviders.Select(provider => new object[] { provider });

    [Fact]
    public void Infrastructure_Should_Register_Exactly_One_PaymentProvider_For_Each_Enum_Value()
    {
        using var serviceProvider = BuildInfrastructureProvider();
        using var scope = serviceProvider.CreateScope();

        var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<IPaymentProvider>>().ToList();
        var registered = providers.Select(x => x.Provider).OrderBy(x => x).ToArray();
        var expected = Enum.GetValues<PaymentProvider>().OrderBy(x => x).ToArray();

        Assert.Equal(expected, registered);
        Assert.DoesNotContain(providers.GroupBy(x => x.Provider), x => x.Count() > 1);

        var factory = scope.ServiceProvider.GetRequiredService<IPaymentProviderFactory>();
        foreach (var provider in expected)
        {
            Assert.Equal(provider, factory.Get(provider).Provider);
        }
    }

    [Fact]
    public void Infrastructure_Should_Register_Webhook_And_Status_Contracts_For_All_Web_Providers()
    {
        using var serviceProvider = BuildInfrastructureProvider();
        using var scope = serviceProvider.CreateScope();

        var verifiers = scope.ServiceProvider.GetRequiredService<IEnumerable<IPaymentWebhookVerifier>>()
            .Select(x => x.Provider)
            .OrderBy(x => x)
            .ToArray();
        var mappers = scope.ServiceProvider.GetRequiredService<IEnumerable<IPaymentStatusMapper>>()
            .Select(x => x.Provider)
            .OrderBy(x => x)
            .ToArray();
        var expected = WebCheckoutProviders.OrderBy(x => x).ToArray();

        Assert.Equal(expected, verifiers);
        Assert.Equal(expected, mappers);
        Assert.DoesNotContain(PaymentProvider.TelegramStars, verifiers);
        Assert.DoesNotContain(PaymentProvider.TelegramStars, mappers);
    }

    [Theory]
    [MemberData(nameof(WebCheckoutProviderCases))]
    public async Task Local_Sandbox_CreatePayment_Should_Be_Networkless_For_Web_Providers(PaymentProvider provider)
    {
        using var serviceProvider = BuildInfrastructureProvider();
        using var scope = serviceProvider.CreateScope();
        var paymentProvider = scope.ServiceProvider.GetRequiredService<IPaymentProviderFactory>().Get(provider);
        var account = CreateSandboxAccount(provider);
        var request = CreatePaymentCreateRequest(provider, account);

        var result = await paymentProvider.CreatePaymentAsync(request, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.PaymentId));
        Assert.StartsWith("https://merchant.example.test/payments/return", result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sandbox", result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(provider.ToString(), result.RedirectUrl, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(request.Payment.RawRequest));
        Assert.DoesNotContain("secret", request.Payment.RawRequest, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TelegramStars_Should_Be_Explicit_Bot_Only_Unsupported_Web_Contract()
    {
        using var serviceProvider = BuildInfrastructureProvider();
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IPaymentProviderFactory>().Get(PaymentProvider.TelegramStars);
        var account = CreateSandboxAccount(PaymentProvider.TelegramStars);
        var request = CreatePaymentCreateRequest(PaymentProvider.TelegramStars, account);

        await Assert.ThrowsAsync<NotSupportedException>(() => provider.CreatePaymentAsync(request, CancellationToken.None));

        Assert.False(PaymentProviderConfigurationRules.SupportsWebCheckout(PaymentProvider.TelegramStars));
        Assert.True(PaymentProviderConfigurationRules.SupportsTelegramCheckout(PaymentProvider.TelegramStars));
        Assert.True(PaymentProviderConfigurationRules.IsBotCheckoutConfigured(account));
        Assert.False(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account));
    }

    [Fact]
    public void Capability_Rules_Should_Cover_Every_Provider_With_Readable_Labels()
    {
        var requiredKeys = new[]
        {
            "createPayment",
            "telegramNative",
            "webhook",
            "signatureValidation",
            "refund",
            "recheck",
            "sandbox",
            "live"
        };

        foreach (var provider in Enum.GetValues<PaymentProvider>())
        {
            var rules = PaymentProviderConfigurationRules.GetCapabilityRules(provider);
            Assert.Equal(requiredKeys.OrderBy(x => x), rules.Select(x => x.Key).OrderBy(x => x));
            Assert.All(rules, rule =>
            {
                Assert.False(string.IsNullOrWhiteSpace(rule.Label));
                Assert.DoesNotContain("РЎ", rule.Label, StringComparison.Ordinal);
                Assert.DoesNotContain("Рџ", rule.Label, StringComparison.Ordinal);
                Assert.DoesNotContain("СЂ", rule.Label, StringComparison.Ordinal);
            });
        }
    }

    private static ServiceProvider BuildInfrastructureProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                ["Database:Provider"] = "Sqlite",
                ["Jwt:SigningKey"] = "payment-provider-contract-tests-signing-key-32-chars",
                ["Jwt:Issuer"] = "VpnPlatform.Tests",
                ["Jwt:Audience"] = "VpnPlatform.Tests",
                ["Security:SecretEncryptionKey"] = "payment-provider-contract-tests-secret-key-32-chars"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Local"));
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration, includeHostedServices: false, includeOperationalWorkers: false);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static PaymentProviderAccount CreateSandboxAccount(PaymentProvider provider)
        => new()
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            Mode = PaymentProviderMode.Sandbox,
            IsEnabled = true,
            Name = $"{provider}-contract-sandbox",
            PublicName = provider.ToString(),
            ReturnUrl = "https://merchant.example.test/payments/return",
            ExtraSettingsJson = "{}"
        };

    private static PaymentCreateRequest CreatePaymentCreateRequest(PaymentProvider provider, PaymentProviderAccount account)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TariffId = Guid.NewGuid(),
            Amount = 490m,
            Currency = "RUB",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            PaymentProvider = provider
        };
        var payment = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Provider = provider,
            ProviderMode = account.Mode,
            PaymentProviderAccountId = account.Id,
            Amount = order.Amount,
            Currency = order.Currency,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        return new PaymentCreateRequest(order, payment, account, "https://merchant.example.test/payments/return");
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
