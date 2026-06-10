using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Public;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentProviderSandboxSeedTests
{
    [Fact]
    public async Task Demo_Seed_Should_Add_Missing_Sandbox_Providers_Without_Duplicating_Existing_Accounts()
    {
        await using var db = CreateDbContext();
        db.PaymentProviderAccounts.Add(new PaymentProviderAccount
        {
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            Name = "custom-yookassa",
            PublicName = "Custom YooKassa",
            IsEnabled = true,
            IsDefault = true,
            ShopId = "custom-shop",
            ApiBaseUrl = "https://api.yookassa.ru/v3",
            ReturnUrl = "http://localhost:5174/payments",
            WebhookUrl = "http://localhost:8080/api/webhooks/payments/yookassa",
            SecretKeyProtected = string.Empty,
            WebhookSecretProtected = string.Empty,
            UseWebhookIpAllowList = false,
            ExtraSettingsJson = "{}"
        });
        await db.SaveChangesAsync();

        await DbInitializer.SeedDemoDataAsync(db, CancellationToken.None);
        await db.SaveChangesAsync();

        var accounts = await db.PaymentProviderAccounts.AsNoTracking().ToListAsync();
        Assert.Equal(Enum.GetValues<PaymentProvider>().Length, accounts.Select(x => x.Provider).Distinct().Count());
        Assert.Single(accounts, x => x.Provider == PaymentProvider.YooKassa);
        Assert.Contains(accounts, x => x.Provider == PaymentProvider.YooKassa && x.Name == "custom-yookassa");

        var webProviders = Enum.GetValues<PaymentProvider>()
            .Where(x => x != PaymentProvider.TelegramStars)
            .ToArray();
        foreach (var provider in webProviders)
        {
            var account = Assert.Single(accounts, x => x.Provider == provider);
            Assert.Equal(PaymentProviderMode.Sandbox, account.Mode);
            Assert.True(account.IsEnabled);
            Assert.True(PaymentProviderConfigurationRules.IsWebCheckoutConfigured(account), $"{provider}: {PaymentProviderConfigurationRules.GetCheckoutConfigurationIssue(account)}");
        }

        var cloudPayments = Assert.Single(accounts, x => x.Provider == PaymentProvider.CloudPayments);
        Assert.Empty(cloudPayments.ApiBaseUrl);
        Assert.Contains("hostedCheckoutUrl", cloudPayments.ExtraSettingsJson, StringComparison.Ordinal);

        var telegramStars = Assert.Single(accounts, x => x.Provider == PaymentProvider.TelegramStars);
        Assert.False(telegramStars.IsEnabled);
        Assert.Equal(PaymentProviderMode.Disabled, telegramStars.Mode);
        Assert.Contains("bot-only", telegramStars.ExtraSettingsJson, StringComparison.Ordinal);

        var publicResult = await new PaymentsController(db).GetAvailableProviders(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(publicResult);
        var publicProviders = Assert.IsAssignableFrom<IReadOnlyCollection<PublicPaymentProviderDto>>(ok.Value);
        Assert.Equal(webProviders.Length, publicProviders.Count);
        Assert.DoesNotContain(publicProviders, x => x.Provider == nameof(PaymentProvider.TelegramStars));
        Assert.Contains(publicProviders, x => x.Provider == nameof(PaymentProvider.CloudPayments));
        Assert.Contains(publicProviders, x => x.Provider == nameof(PaymentProvider.PayPal));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }
}
