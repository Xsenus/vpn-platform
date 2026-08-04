using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Public;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentProvidersPublicControllerTests
{
    [Fact]
    public async Task GetAvailableProviders_Should_Return_Only_Enabled_Configured_Providers_Without_Secrets()
    {
        await using var db = CreateDbContext();
        db.PaymentProviderAccounts.AddRange(
            Account(PaymentProvider.YooKassa, PaymentProviderMode.Sandbox, isEnabled: true, publicName: "YooKassa sandbox", secret: "protected-secret"),
            Account(PaymentProvider.RoboKassa, PaymentProviderMode.Sandbox, isEnabled: false, publicName: "Robo disabled", secret: "protected-secret"),
            Account(PaymentProvider.YooMoney, PaymentProviderMode.Disabled, isEnabled: true, publicName: "YooMoney disabled-mode", secret: "protected-secret"),
            Account(PaymentProvider.Stripe, PaymentProviderMode.Sandbox, isEnabled: true, publicName: "Stripe without shop", secret: "sandbox-secret", shopId: ""),
            Account(PaymentProvider.CloudPayments, PaymentProviderMode.Sandbox, isEnabled: true, publicName: "CloudPayments without widget", secret: "sandbox-secret", extraSettingsJson: "{}"),
            Account(PaymentProvider.TelegramStars, PaymentProviderMode.Sandbox, isEnabled: true, publicName: "Telegram Stars", secret: ""),
            Account(PaymentProvider.TBankAcquiring, PaymentProviderMode.Production, isEnabled: true, publicName: "TBank live", secret: "live-secret"));
        await db.SaveChangesAsync();

        var result = await new PaymentsController(db).GetAvailableProviders(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var providers = Assert.IsAssignableFrom<IReadOnlyCollection<PublicPaymentProviderDto>>(ok.Value);
        Assert.Contains(providers, x => x.Provider == nameof(PaymentProvider.YooKassa) && x.PublicName == "YooKassa sandbox" && x.Mode == nameof(PaymentProviderMode.Sandbox));
        Assert.Contains(providers, x => x.Provider == nameof(PaymentProvider.TBankAcquiring) && x.PublicName == "TBank live" && x.Mode == nameof(PaymentProviderMode.Production));
        Assert.DoesNotContain(providers, x => x.Provider == nameof(PaymentProvider.RoboKassa));
        Assert.DoesNotContain(providers, x => x.Provider == nameof(PaymentProvider.YooMoney));
        Assert.DoesNotContain(providers, x => x.Provider == nameof(PaymentProvider.Stripe));
        Assert.DoesNotContain(providers, x => x.Provider == nameof(PaymentProvider.CloudPayments));
        Assert.DoesNotContain(providers, x => x.Provider == nameof(PaymentProvider.TelegramStars));
        var serialized = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("protected-secret", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("live-secret", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecretKeyProtected", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WebhookSecretProtected", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAvailableProviders_Should_Return_Empty_List_When_No_Provider_Is_Enabled()
    {
        await using var db = CreateDbContext();
        db.PaymentProviderAccounts.Add(Account(PaymentProvider.YooKassa, PaymentProviderMode.Sandbox, isEnabled: false, publicName: "YooKassa"));
        await db.SaveChangesAsync();

        var result = await new PaymentsController(db).GetAvailableProviders(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var providers = Assert.IsAssignableFrom<IReadOnlyCollection<PublicPaymentProviderDto>>(ok.Value);
        Assert.Empty(providers);
    }

    [Fact]
    public async Task Public_List_And_Web_Checkout_Should_Select_The_Same_Configured_Account()
    {
        await using var db = CreateDbContext();
        var unconfiguredDefault = Account(
            PaymentProvider.YooKassa,
            PaymentProviderMode.Production,
            isEnabled: true,
            publicName: "Unavailable default",
            secret: "");
        unconfiguredDefault.IsDefault = true;
        unconfiguredDefault.CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var configuredFallback = Account(
            PaymentProvider.YooKassa,
            PaymentProviderMode.Sandbox,
            isEnabled: true,
            publicName: "Configured checkout",
            secret: "sandbox-secret");
        configuredFallback.IsDefault = false;
        configuredFallback.CreatedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        db.PaymentProviderAccounts.AddRange(unconfiguredDefault, configuredFallback);
        await db.SaveChangesAsync();

        var publicResult = await new PaymentsController(db).GetAvailableProviders(CancellationToken.None);
        var accountResult = await new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock())
            .GetWebCheckoutAccountEntityAsync(PaymentProvider.YooKassa, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(publicResult);
        var provider = Assert.Single(Assert.IsAssignableFrom<IReadOnlyCollection<PublicPaymentProviderDto>>(ok.Value));
        Assert.Equal("Configured checkout", provider.PublicName);
        Assert.True(accountResult.IsSuccess);
        Assert.Equal(configuredFallback.Id, accountResult.Value?.Id);
    }

    [Fact]
    public async Task Public_List_And_Web_Checkout_Should_Use_Deterministic_Order_When_No_Default_Exists()
    {
        await using var db = CreateDbContext();
        var olderByCreation = Account(PaymentProvider.Stripe, PaymentProviderMode.Sandbox, true, "Zulu checkout", "sandbox-secret");
        olderByCreation.IsDefault = false;
        olderByCreation.CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstByName = Account(PaymentProvider.Stripe, PaymentProviderMode.Sandbox, true, "Alpha checkout", "sandbox-secret");
        firstByName.IsDefault = false;
        firstByName.CreatedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        db.PaymentProviderAccounts.AddRange(firstByName, olderByCreation);
        await db.SaveChangesAsync();

        var publicResult = await new PaymentsController(db).GetAvailableProviders(CancellationToken.None);
        var accountResult = await new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock())
            .GetWebCheckoutAccountEntityAsync(PaymentProvider.Stripe, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(publicResult);
        var provider = Assert.Single(Assert.IsAssignableFrom<IReadOnlyCollection<PublicPaymentProviderDto>>(ok.Value));
        Assert.Equal("Zulu checkout", provider.PublicName);
        Assert.True(accountResult.IsSuccess);
        Assert.Equal(olderByCreation.Id, accountResult.Value?.Id);
    }

    private static PaymentProviderAccount Account(PaymentProvider provider, PaymentProviderMode mode, bool isEnabled, string publicName, string secret = "", string? shopId = null, string extraSettingsJson = "{}")
        => new()
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            Mode = mode,
            Name = publicName,
            PublicName = publicName,
            IsEnabled = isEnabled,
            IsDefault = true,
            ShopId = shopId ?? $"shop-{provider}",
            ApiBaseUrl = "https://payment.test",
            ReturnUrl = "https://cabinet.test/payments",
            SecretKeyProtected = secret,
            WebhookSecretProtected = secret,
            ExtraSettingsJson = extraSettingsJson
        };

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? protectedValue, int visibleSuffix = 4) => protectedValue ?? string.Empty;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
    }
}
