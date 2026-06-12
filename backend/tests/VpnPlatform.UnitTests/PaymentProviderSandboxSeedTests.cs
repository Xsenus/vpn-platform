using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Public;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Configuration;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PaymentProviderSandboxSeedTests
{
    [Fact]
    public async Task Local_Sqlite_Seed_Should_Create_Admin_Tariffs_Payments_And_Sandbox_Vpn_Infrastructure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var bootstrap = new AdminBootstrapService(new PasswordService());
        await bootstrap.BootstrapAsync(db, new AdminBootstrapOptions
        {
            Enabled = true,
            Email = "admin@local.test",
            Password = "LocalAdminPassword123!",
            DisplayName = "Локальный администратор",
            RolesCsv = UserRoles.SuperAdmin,
            ResetExistingPassword = true
        }, CancellationToken.None);
        await DbInitializer.SeedDemoDataAsync(db, CancellationToken.None);
        await db.SaveChangesAsync();

        var admin = await db.Users.SingleAsync(x => x.Email == "admin@local.test");
        Assert.Equal(UserStatus.Active, admin.Status);
        Assert.Equal(UserRoles.SuperAdmin, admin.RolesCsv);
        Assert.True(new PasswordService().Verify("LocalAdminPassword123!", admin.PasswordHash));

        var tariffs = await db.Tariffs.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync();
        Assert.True(tariffs.Count >= 3);
        Assert.Contains(tariffs, x => x.Slug == "one-month" && x.Name == "1 месяц" && x.Price == 490m);
        Assert.Contains(tariffs, x => x.Slug == "three-months" && x.Name == "3 месяца");
        Assert.Contains(tariffs, x => x.Slug == "trial" && x.IsTrial && x.Price == 0m);

        var providers = await db.PaymentProviderAccounts.AsNoTracking().ToListAsync();
        Assert.Equal(Enum.GetValues<PaymentProvider>().Length, providers.Select(x => x.Provider).Distinct().Count());
        Assert.Equal(8, providers.Count(x => x.IsEnabled && x.Mode == PaymentProviderMode.Sandbox));
        Assert.Contains(providers, x => x.Provider == PaymentProvider.CloudPayments && x.ExtraSettingsJson.Contains("hostedCheckoutUrl", StringComparison.Ordinal));
        Assert.Contains(providers, x => x.Provider == PaymentProvider.TelegramStars && !x.IsEnabled && x.Mode == PaymentProviderMode.Disabled);

        var sandboxGroup = await db.NodeGroups.AsNoTracking().SingleAsync(x => x.Code == "sandbox");
        Assert.Equal("sandbox", sandboxGroup.Region);

        var sandboxPanel = await db.VpnPanels.AsNoTracking().SingleAsync(x => x.Name == "sandbox-x3ui-panel");
        Assert.Equal(VpnPanelStatus.Active, sandboxPanel.Status);
        Assert.Equal(HealthStatus.Healthy, sandboxPanel.HealthStatus);
        Assert.Equal("https://sandbox-node.local", sandboxPanel.BaseUrl);

        var sandboxInbound = await db.VpnInbounds.AsNoTracking().SingleAsync(x => x.VpnPanelId == sandboxPanel.Id && x.ExternalInboundId == "sandbox-default-vless");
        Assert.True(sandboxInbound.IsDefault);
        Assert.True(sandboxInbound.IsActive);
        Assert.Equal("vless", sandboxInbound.Protocol);

        var sandboxNode = await db.VpnNodes.Include(x => x.NodeGroup).SingleAsync(x => x.Name == "sandbox-vpn-node");
        Assert.Equal(NodeStatus.Ready, sandboxNode.Status);
        Assert.Equal(HealthStatus.Healthy, sandboxNode.HealthStatus);
        Assert.True(sandboxNode.IsAvailableForNewUsers);
        Assert.Equal(sandboxGroup.Id, sandboxNode.NodeGroupId);
        Assert.Contains("vless", sandboxNode.SupportedProtocolsCsv, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sandbox", sandboxNode.TagsCsv, StringComparison.OrdinalIgnoreCase);

        var faq = await db.FaqEntries.AsNoTracking().ToListAsync();
        Assert.Contains(faq, x => x.Question == "Как подключиться?");
        var content = await db.SiteContentBlocks.AsNoTracking().ToListAsync();
        Assert.Contains(content, x => x.Key == "home.hero.title" && x.Value.Contains("Быстрый VPN-доступ", StringComparison.Ordinal));

        var seededText = tariffs.SelectMany(x => new[] { x.Name, x.Description, x.FullDescription, x.FeaturesJson, x.AfterPaymentText })
            .Concat(faq.SelectMany(x => new[] { x.Question, x.Answer, x.Category }))
            .Concat(content.SelectMany(x => new[] { x.Value, x.Label, x.Description }))
            .ToArray();
        Assert.DoesNotContain(seededText, ContainsMojibakeMarker);

        await DbInitializer.SeedDemoDataAsync(db, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(tariffs.Count, await db.Tariffs.CountAsync());
        Assert.Equal(providers.Count, await db.PaymentProviderAccounts.CountAsync());
        Assert.Equal(1, await db.NodeGroups.CountAsync(x => x.Code == "sandbox"));
        Assert.Equal(1, await db.VpnPanels.CountAsync(x => x.Name == "sandbox-x3ui-panel"));
        Assert.Equal(1, await db.VpnInbounds.CountAsync(x => x.ExternalInboundId == "sandbox-default-vless"));
        Assert.Equal(1, await db.VpnNodes.CountAsync(x => x.Name == "sandbox-vpn-node"));
    }

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

    private static bool ContainsMojibakeMarker(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return value.Contains("Рџ", StringComparison.Ordinal)
               || value.Contains("Р‘", StringComparison.Ordinal)
               || value.Contains("Р ", StringComparison.Ordinal)
               || value.Contains("СЃ", StringComparison.Ordinal)
               || value.Contains("С‚", StringComparison.Ordinal)
               || value.Contains("вЂ", StringComparison.Ordinal)
               || value.Contains("Р", StringComparison.Ordinal);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }
}
