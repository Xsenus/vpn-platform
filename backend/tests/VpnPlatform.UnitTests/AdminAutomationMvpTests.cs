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
        Assert.DoesNotContain("hash-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("panel-password-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment-secret-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
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
        Assert.Null(ready.CheckoutConfigurationIssue);
        Assert.Contains("createPayment", ready.CapabilitiesJson, StringComparison.Ordinal);
        Assert.Contains("***", ready.ExtraSettingsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("must-not-leak", ready.ExtraSettingsJson, StringComparison.OrdinalIgnoreCase);

        var disabled = Assert.Single(accounts, x => x.Provider == PaymentProvider.RoboKassa);
        Assert.False(disabled.IsCheckoutConfigured);
        Assert.Contains("Disabled", disabled.CheckoutConfigurationIssue, StringComparison.OrdinalIgnoreCase);

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
                ["TelegramBot:SecretToken"] = "webhook-secret"
            })
            .Build();
        var controller = new AdminTelegramBotSettingsController(db, configuration);

        var before = await controller.GetSettings(CancellationToken.None);
        var okBefore = Assert.IsType<OkObjectResult>(before);
        var jsonBefore = JsonSerializer.Serialize(okBefore.Value);
        Assert.Contains("BotTokenMasked", jsonBefore, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-token", jsonBefore, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook-secret", jsonBefore, StringComparison.OrdinalIgnoreCase);

        var after = await controller.UpdateSettings(new UpdateTelegramBotSettingsCommand("Welcome", "Instruction", "Support", "After payment"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(after);
        Assert.Contains(await db.NotificationTemplates.ToListAsync(), x => x.Key == "telegram.welcome" && x.Body == "Welcome");
        Assert.Contains(await db.NotificationTemplates.ToListAsync(), x => x.Key == "telegram.instruction" && x.Body == "Instruction");
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
