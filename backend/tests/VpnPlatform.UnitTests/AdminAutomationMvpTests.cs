using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminAutomationMvpTests
{
    [Fact]
    public async Task Dashboard_Summary_Should_Not_Expose_Secrets()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.test",
            DisplayName = "Admin",
            PasswordHash = "hash-must-not-leak",
            RolesCsv = "Admin",
            ReferralCode = "REF"
        });
        db.VpnNodes.Add(new VpnNode
        {
            Id = Guid.NewGuid(),
            Name = "Node 1",
            Host = "node-1",
            Region = "NL",
            PanelPassword = "panel-password-must-not-leak",
            HealthStatus = HealthStatus.Healthy
        });
        db.PaymentProviderAccounts.Add(PaymentAccount(
            PaymentProvider.YooKassa,
            PaymentProviderMode.Production,
            isEnabled: true,
            shopId: "shop-1",
            secret: "payment-secret-must-not-leak"));
        await db.SaveChangesAsync();

        var result = await new AdminDashboardController(db).GetSummary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("TotalUsers", json, StringComparison.Ordinal);
        Assert.Contains("ProductionReadiness", json, StringComparison.Ordinal);
        Assert.DoesNotContain("hash-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("panel-password-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment-secret-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_Summary_Should_Report_Production_Readiness_And_Ignore_Sandbox_Infrastructure()
    {
        await using var db = CreateDbContext();
        db.Tariffs.Add(new Tariff { Id = Guid.NewGuid(), Name = "Live", Slug = "live", IsActive = true, IsTrial = false, Price = 490m, Currency = "RUB", DurationDays = 30 });
        db.PaymentProviderAccounts.Add(PaymentAccount(PaymentProvider.YooKassa, PaymentProviderMode.Production, isEnabled: true, shopId: "shop-1", secret: "protected-secret"));
        AddTelegramSettings(db);
        db.VpnPanels.Add(new VpnPanel
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "sandbox-x3ui-panel",
            BaseUrl = "https://sandbox-node.local",
            Region = "sandbox",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 100000,
            UsedCapacity = 0
        });
        db.VpnNodes.Add(new VpnNode
        {
            Id = Guid.NewGuid(),
            Name = "sandbox-vpn-node",
            Host = "sandbox-node.local",
            Provider = "x3ui",
            Region = "sandbox",
            Status = NodeStatus.Ready,
            HealthStatus = HealthStatus.Healthy,
            IsAvailableForNewUsers = true,
            Capacity = 100000,
            UsedCapacity = 0,
            TagsCsv = "sandbox"
        });
        await db.SaveChangesAsync();

        var blocked = Assert.IsType<OkObjectResult>(await new AdminDashboardController(db).GetSummary(CancellationToken.None));
        var blockedSummary = Assert.IsType<AdminDashboardSummaryDto>(blocked.Value);
        Assert.False(blockedSummary.ProductionReadiness.IsReady);
        Assert.Contains(blockedSummary.ProductionReadiness.Checks, x => x.Key == "vpn-node" && x.Status == "Blocked");
        Assert.Contains(blockedSummary.ProductionReadiness.Checks, x => x.Key == "vpn-panel" && x.Status == "Blocked");
        Assert.Contains(blockedSummary.ProductionReadiness.Checks, x => x.Key == "payment-webhook" && x.Category == "Платежи" && x.ActionHref == "#payments");
        Assert.Contains(blockedSummary.ProductionReadiness.Checks, x => x.Key == "telegram-bot" && x.Status == "Ready" && x.ActionHref == "#bot");

        var panel = new VpnPanel
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "live-panel",
            BaseUrl = "https://panel.example.test",
            Region = "eu",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Healthy,
            Login = "admin",
            Capacity = 5000,
            UsedCapacity = 0
        };
        db.VpnPanels.Add(panel);
        db.VpnInbounds.Add(new VpnInbound
        {
            Id = Guid.NewGuid(),
            VpnPanelId = panel.Id,
            ExternalInboundId = "1",
            Name = "VLESS",
            Protocol = "vless",
            IsActive = true,
            Capacity = 5000,
            UsedCapacity = 0
        });
        db.VpnNodes.Add(new VpnNode
        {
            Id = Guid.NewGuid(),
            Name = "live-node",
            Host = "node.example.test",
            Provider = "x3ui",
            Region = "eu",
            Status = NodeStatus.Ready,
            HealthStatus = HealthStatus.Healthy,
            IsAvailableForNewUsers = true,
            Capacity = 5000,
            UsedCapacity = 0,
            PanelBaseUrl = panel.BaseUrl
        });
        await db.SaveChangesAsync();

        var ready = Assert.IsType<OkObjectResult>(await new AdminDashboardController(db).GetSummary(CancellationToken.None));
        var readySummary = Assert.IsType<AdminDashboardSummaryDto>(ready.Value);
        Assert.True(readySummary.ProductionReadiness.IsReady);
        Assert.All(readySummary.ProductionReadiness.Checks, check => Assert.Equal("Ready", check.Status));
        Assert.Contains(readySummary.ProductionReadiness.Checks, check => check.Key == "ci-cd" && check.Category == "CI/CD" && check.ActionHref == "#provisioning");
    }

    [Fact]
    public async Task Servers_List_Should_Mask_PanelPassword()
    {
        await using var db = CreateDbContext();
        db.VpnNodes.Add(new VpnNode
        {
            Id = Guid.NewGuid(),
            Name = "Node 1",
            Host = "node-1",
            Region = "NL",
            PanelBaseUrl = "https://panel.example.test",
            PanelUsername = "admin",
            PanelPassword = "super-secret-panel-password"
        });
        await db.SaveChangesAsync();

        var controller = CreateOperationsController(db);
        var result = await controller.GetServers(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("PanelPasswordConfigured", json, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-panel-password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PanelPassword\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Provider_Accounts_Should_Be_Visible_To_Admin_With_Masked_Settings_And_NotConfigured_Reason()
    {
        await using var db = CreateDbContext();
        db.PaymentProviderAccounts.AddRange(
            PaymentAccount(PaymentProvider.YooKassa, PaymentProviderMode.Sandbox, isEnabled: true, shopId: "shop-ok", secret: "protected-secret", extraSettingsJson: "{\"apiSecret\":\"must-not-leak\",\"region\":\"ru\"}"),
            PaymentAccount(PaymentProvider.RoboKassa, PaymentProviderMode.Disabled, isEnabled: true, shopId: "shop-disabled", secret: "protected-secret"));
        await db.SaveChangesAsync();

        var controller = CreateOperationsController(db);
        var result = await controller.GetPaymentProviderAccounts(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var accounts = Assert.IsAssignableFrom<IReadOnlyCollection<PaymentProviderAccountDto>>(ok.Value);
        var ready = Assert.Single(accounts, x => x.Provider == PaymentProvider.YooKassa);
        Assert.True(ready.IsCheckoutConfigured);
        Assert.True(ready.IsPubliclyAvailable);
        Assert.Null(ready.CheckoutConfigurationIssue);
        Assert.Contains("createPayment", ready.CapabilitiesJson, StringComparison.Ordinal);
        Assert.Contains(ready.Capabilities, x => x.Key == "createPayment" && x.Supported);
        Assert.Contains(ready.RequiredFields, x => x.Key == "shopId" && x.Required && x.Configured);
        Assert.Empty(ready.ReadinessBlockers);
        Assert.Contains("***", ready.ExtraSettingsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("must-not-leak", ready.ExtraSettingsJson, StringComparison.OrdinalIgnoreCase);

        var disabled = Assert.Single(accounts, x => x.Provider == PaymentProvider.RoboKassa);
        Assert.False(disabled.IsCheckoutConfigured);
        Assert.False(disabled.IsPubliclyAvailable);
        Assert.Contains("Disabled", disabled.CheckoutConfigurationIssue, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(disabled.ReadinessBlockers, x => x.Contains("Disabled", StringComparison.OrdinalIgnoreCase));

        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("SecretKeyProtected", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("protected-secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("must-not-leak", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Provider_Account_Update_Should_Preserve_Secrets_And_Extra_Settings_When_Left_Blank()
    {
        await using var db = CreateDbContext();
        var account = PaymentAccount(
            PaymentProvider.Stripe,
            PaymentProviderMode.Sandbox,
            isEnabled: true,
            shopId: "stripe-shop",
            secret: "protected-secret",
            extraSettingsJson: "{\"apiToken\":\"must-stay-secret\",\"region\":\"eu\"}");
        db.PaymentProviderAccounts.Add(account);
        await db.SaveChangesAsync();

        var controller = CreateOperationsController(db);
        var result = await controller.UpdatePaymentProviderAccount(account.Id, new UpsertPaymentProviderAccountCommand(
            PaymentProvider.Stripe,
            PaymentProviderMode.Sandbox,
            "stripe-main",
            "Stripe card",
            true,
            true,
            "stripe-shop-updated",
            "https://api.stripe.com",
            "https://cabinet.example.test/payment-return",
            "https://api.example.test/api/webhooks/payments/stripe",
            null,
            "",
            false,
            "",
            ""), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PaymentProviderAccountDto>(ok.Value);
        Assert.Equal("Stripe card", dto.PublicName);
        Assert.True(dto.HasSecretKey);
        Assert.True(dto.HasWebhookSecret);

        var saved = await db.PaymentProviderAccounts.SingleAsync(x => x.Id == account.Id);
        Assert.Equal("protected-secret", saved.SecretKeyProtected);
        Assert.Equal("protected-secret", saved.WebhookSecretProtected);
        Assert.Contains("must-stay-secret", saved.ExtraSettingsJson, StringComparison.Ordinal);
        Assert.Equal("stripe-shop-updated", saved.ShopId);
        Assert.False(saved.UseWebhookIpAllowList);
    }

    [Fact]
    public async Task Provider_Account_Create_Should_Reject_Invalid_Extra_Settings_Json()
    {
        await using var db = CreateDbContext();
        var controller = CreateOperationsController(db);

        var result = await controller.CreatePaymentProviderAccount(new UpsertPaymentProviderAccountCommand(
            PaymentProvider.CloudPayments,
            PaymentProviderMode.Sandbox,
            "cloudpayments-test",
            "CloudPayments",
            true,
            true,
            "public-id",
            "",
            "https://cabinet.example.test/payment-return",
            "https://api.example.test/api/webhooks/payments/cloudpayments",
            "",
            "",
            false,
            "",
            "[\"not-object\"]"), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);
        Assert.Contains("ExtraSettingsJson", json, StringComparison.Ordinal);
        Assert.Empty(await db.PaymentProviderAccounts.ToListAsync());
    }

    [Fact]
    public async Task Provider_Account_Check_Should_Update_Health_And_Return_Details()
    {
        await using var db = CreateDbContext();
        var account = PaymentAccount(PaymentProvider.YooKassa, PaymentProviderMode.Sandbox, isEnabled: true, shopId: "shop-ok", secret: "protected-secret");
        db.PaymentProviderAccounts.Add(account);
        await db.SaveChangesAsync();

        var controller = CreateOperationsController(db);
        var result = await controller.CheckPaymentProviderAccount(account.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var check = Assert.IsType<PaymentProviderAccountCheckResultDto>(ok.Value);
        Assert.True(check.IsReady);
        Assert.Equal("Healthy", check.HealthStatus);
        Assert.Contains(check.Details, x => x.Contains("ready", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("https://api.example.test/webhooks/payments", check.Account.WebhookUrl);

        var saved = await db.PaymentProviderAccounts.SingleAsync(x => x.Id == account.Id);
        Assert.Equal(HealthStatus.Healthy, saved.HealthStatus);
        Assert.NotNull(saved.LastHealthCheckAt);
    }

    [Fact]
    public async Task Subscription_Extend_Should_Audit_Manual_Action()
    {
        await using var db = CreateDbContext();
        var subscriptionId = Guid.NewGuid();
        db.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            UserId = Guid.NewGuid(),
            TariffId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            StartAt = DateTimeOffset.UtcNow.AddDays(-10),
            EndAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        var controller = CreateOperationsController(db);
        var result = await controller.ExtendSubscription(subscriptionId, new AdminSubscriptionExtendHttpRequest(30, "support compensation"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == subscriptionId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.True(subscription.EndAt > DateTimeOffset.UtcNow.AddDays(29));
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "subscription.extend" && x.EntityId == subscriptionId.ToString());
    }

    [Fact]
    public async Task Access_Disable_Enable_Should_Update_State_And_Write_History()
    {
        await using var db = CreateDbContext();
        var subscriptionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var accessId = Guid.NewGuid();
        db.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            UserId = Guid.NewGuid(),
            TariffId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            StartAt = DateTimeOffset.UtcNow.AddDays(-1),
            EndAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        db.VpnNodes.Add(new VpnNode { Id = nodeId, Name = "Node", Host = "node", Region = "NL" });
        db.AccessCredentials.Add(new AccessCredential
        {
            Id = accessId,
            SubscriptionId = subscriptionId,
            ServerId = nodeId,
            ProviderAccessId = "provider-client-1",
            AccessUri = "vless://example",
            Status = AccessCredentialStatus.Active
        });
        await db.SaveChangesAsync();

        var controller = CreateOperationsController(db);
        var disable = await controller.DisableAccessCredential(accessId, new AdminAccessActionHttpRequest("abuse"), CancellationToken.None);
        var enable = await controller.EnableAccessCredential(accessId, new AdminAccessActionHttpRequest("resolved"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(disable);
        Assert.IsType<OkObjectResult>(enable);
        var access = await db.AccessCredentials.SingleAsync(x => x.Id == accessId);
        Assert.Equal(AccessCredentialStatus.Active, access.Status);
        Assert.Null(access.DisabledAt);
        var history = await db.AccessCredentialHistories.Where(x => x.AccessCredentialId == accessId).ToListAsync();
        Assert.Contains(history, x => x.EventType == "manual_admin_disable");
        Assert.Contains(history, x => x.EventType == "manual_admin_enable");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "access.disable" && x.EntityId == accessId.ToString());
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "access.enable" && x.EntityId == accessId.ToString());
    }


    [Fact]
    public async Task Provisioning_Run_Views_Should_Redact_Secret_Like_Log_Values()
    {
        await using var db = CreateDbContext();
        var runId = Guid.NewGuid();
        db.ProvisioningRuns.Add(new ProvisioningRun
        {
            Id = runId,
            NodeId = Guid.NewGuid(),
            Status = ProvisioningRunStatus.Failed,
            DryRun = true,
            ExecutionLog = "precheck password=must-not-leak token:also-secret"
        });
        db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            Id = Guid.NewGuid(),
            ProvisioningRunId = runId,
            StepName = "precheck",
            Status = ProvisioningRunStatus.Failed,
            Output = "ssh_pass=step-secret",
            ErrorText = "api_key:another-secret"
        });
        await db.SaveChangesAsync();

        var controller = CreateOperationsController(db);
        var list = await controller.GetProvisioningRuns(CancellationToken.None);
        var detail = await controller.GetProvisioningRun(runId, CancellationToken.None);

        var listJson = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(list).Value);
        var detailJson = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(detail).Value);
        Assert.DoesNotContain("must-not-leak", listJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("also-secret", listJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("step-secret", detailJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("another-secret", detailJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("***", detailJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Telegram_Bot_Settings_Should_Mask_Token_And_Update_Text_Templates()
    {
        await using var db = CreateDbContext();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TelegramBot:Enabled"] = "false",
                ["TelegramBot:Mode"] = "Polling",
                ["TelegramBot:PublicBotUsername"] = "vpn_test_bot",
                ["TelegramBot:BotToken"] = "123456789:super-secret-token",
                ["TelegramBot:SecretToken"] = "webhook-secret",
                ["TelegramBot:WebAppUrl"] = "http://localhost:5174"
            })
            .Build();
        var controller = new AdminTelegramBotSettingsController(db, configuration, new TestSecretProtector());

        var before = await controller.GetSettings(CancellationToken.None);
        var okBefore = Assert.IsType<OkObjectResult>(before);
        var jsonBefore = JsonSerializer.Serialize(okBefore.Value);
        Assert.Contains("BotTokenMasked", jsonBefore, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-token", jsonBefore, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook-secret", jsonBefore, StringComparison.OrdinalIgnoreCase);

        var after = await controller.UpdateSettings(new UpdateTelegramBotSettingsCommand(
            Enabled: true,
            Mode: "Webhook",
            PublicBotUsername: "@managed_bot",
            BotToken: "987654321:new-secret-token",
            WebhookUrl: "https://api.example.test/api/channels/telegram/webhook",
            SecretToken: "new-webhook-secret",
            AdminChatId: "-100123456",
            WebAppUrl: "https://cabinet.example.test",
            WelcomeText: "Welcome",
            InstructionText: "Instruction",
            SupportText: "Support",
            AfterPaymentTextTemplate: "After payment",
            RenewalTextTemplate: "Renewal",
            PaymentFailedTextTemplate: "Payment failed",
            SubscriptionExpiredTextTemplate: "Subscription expired"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(after);
        Assert.Contains(await db.NotificationTemplates.ToListAsync(), x => x.Key == "telegram.welcome" && x.Body == "Welcome");
        Assert.Contains(await db.NotificationTemplates.ToListAsync(), x => x.Key == "telegram.instruction" && x.Body == "Instruction");
        Assert.Contains(await db.NotificationTemplates.ToListAsync(), x => x.Key == "telegram.renewal" && x.Body == "Renewal");
        Assert.Contains(await db.NotificationTemplates.ToListAsync(), x => x.Key == "telegram.payment_failed" && x.Body == "Payment failed");
        Assert.Contains(await db.NotificationTemplates.ToListAsync(), x => x.Key == "telegram.subscription_expired" && x.Body == "Subscription expired");
        Assert.Contains(await db.SiteContentBlocks.ToListAsync(), x => x.Key == "telegram_bot.public_bot_username" && x.Value == "managed_bot");
        Assert.Contains(await db.SiteContentBlocks.ToListAsync(), x => x.Key == "telegram_bot.mode" && x.Value == "Webhook");
        Assert.Contains(await db.SiteContentBlocks.ToListAsync(), x => x.Key == "telegram_bot.enabled" && x.Value == "true");
        var jsonAfter = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(after).Value);
        Assert.Contains("managed_bot", jsonAfter, StringComparison.Ordinal);
        Assert.DoesNotContain("new-secret-token", jsonAfter, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("new-webhook-secret", jsonAfter, StringComparison.OrdinalIgnoreCase);
    }

    private static AdminOperationsController CreateOperationsController(ApplicationDbContext db)
    {
        var controller = new AdminOperationsController(
            db,
            provisioningService: null!,
            paymentOrchestrator: null!,
            paymentProviderAccounts: new PaymentProviderAccountService(db, new TestSecretProtector(), new TestClock()));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static PaymentProviderAccount PaymentAccount(
        PaymentProvider provider,
        PaymentProviderMode mode,
        bool isEnabled,
        string shopId,
        string secret,
        string extraSettingsJson = "{}")
        => new()
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            Mode = mode,
            Name = provider.ToString(),
            PublicName = provider.ToString(),
            IsEnabled = isEnabled,
            IsDefault = true,
            ShopId = shopId,
            ApiBaseUrl = "https://payment.example.test",
            ReturnUrl = "https://cabinet.example.test/payments/return",
            WebhookUrl = "https://api.example.test/webhooks/payments",
            SecretKeyProtected = secret,
            WebhookSecretProtected = secret,
            ExtraSettingsJson = extraSettingsJson
        };

    private static void AddTelegramSettings(ApplicationDbContext db)
    {
        db.SiteContentBlocks.AddRange(
            new SiteContentBlock { Key = "telegram_bot.enabled", Group = "telegram_bot", Label = "Включен", Value = "true", InputType = "checkbox" },
            new SiteContentBlock { Key = "telegram_bot.mode", Group = "telegram_bot", Label = "Режим", Value = "LongPolling", InputType = "select" },
            new SiteContentBlock { Key = "telegram_bot.public_bot_username", Group = "telegram_bot", Label = "Public bot username", Value = "vpnplatform_bot", InputType = "text" },
            new SiteContentBlock { Key = "telegram_bot.bot_token_protected", Group = "telegram_bot", Label = "Bot token", Value = "protected-token", InputType = "secret" });
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= visibleTail ? "***" : $"***{value[^visibleTail..]}";
        }
    }
}
