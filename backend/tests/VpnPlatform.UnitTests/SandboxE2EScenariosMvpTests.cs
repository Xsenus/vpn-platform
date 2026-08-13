using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Api.Controllers.Public;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.HostedServices;
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SandboxE2EScenariosMvpTests
{
    [Fact]
    public async Task Telegram_Purchase_Sandbox_E2E_Should_Activate_Access_Notify_Admin_And_Be_Idempotent()
    {
        await using var harness = await SandboxHarness.CreateAsync();
        var tariff = await harness.SeedTariffNodeAndProviderAsync();

        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(1000, "/start"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(1001, "register_tg"), EmptyHeaders(), null, CancellationToken.None);
        var tariffList = await harness.Bot.ProcessUpdateAsync(TelegramCallback(1002, "tariffs"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(1003, $"buy:{tariff.Id}"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(1004, $"confirm_order:{tariff.Id}"), EmptyHeaders(), null, CancellationToken.None);
        var order = await harness.Db.Orders.SingleAsync();

        var paymentLink = await harness.Bot.ProcessUpdateAsync(TelegramCallback(1005, $"pay:{order.Id}:YooKassa"), EmptyHeaders(), null, CancellationToken.None);
        var duplicatePaymentLink = await harness.Bot.ProcessUpdateAsync(TelegramCallback(1006, $"pay:{order.Id}:YooKassa"), EmptyHeaders(), null, CancellationToken.None);
        var payment = await harness.Db.Payments.SingleAsync();

        Assert.True(tariffList.IsSuccess, tariffList.Error);
        Assert.Contains("Monthly", tariffList.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.True(paymentLink.Value!.ResponseText.Contains("Платеж создан", StringComparison.OrdinalIgnoreCase));
        Assert.True(duplicatePaymentLink.Value!.ResponseText.Contains("Платеж создан", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, await harness.Db.Payments.CountAsync());

        var webhook = SandboxWebhook(payment.ProviderPaymentId, "evt-purchase-1", payment.Amount, payment.Currency, payment.OrderId, shopId: "shop-yookassa");
        var firstWebhook = await harness.Orchestrator.ProcessAsync(PaymentProvider.YooKassa, webhook, EmptyHeaders(), CancellationToken.None);
        var duplicateWebhook = await harness.Orchestrator.ProcessAsync(PaymentProvider.YooKassa, webhook, EmptyHeaders(), CancellationToken.None);

        Assert.True(firstWebhook.IsSuccess, firstWebhook.Error);
        Assert.True(duplicateWebhook.IsSuccess, duplicateWebhook.Error);
        Assert.Equal("Webhook already processed.", duplicateWebhook.Value);
        Assert.Equal(1, await harness.Db.PaymentWebhookEvents.CountAsync());
        Assert.Equal(1, await harness.Db.Subscriptions.CountAsync());
        Assert.Equal(1, await harness.Db.AccessCredentials.CountAsync());
        Assert.Equal(1, harness.VpnProvider.CreateCalls);
        Assert.Equal(0, harness.VpnProvider.UpdateCalls);

        var activatedPayment = await harness.Db.Payments.SingleAsync();
        var activatedOrder = await harness.Db.Orders.SingleAsync();
        var subscription = await harness.Db.Subscriptions.SingleAsync();
        var access = await harness.Db.AccessCredentials.SingleAsync();
        Assert.Equal(PaymentStatus.Succeeded, activatedPayment.Status);
        Assert.True(activatedPayment.IsActivationProcessed);
        Assert.Equal(OrderStatus.Completed, activatedOrder.Status);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(AccessCredentialStatus.Active, access.Status);
        Assert.Contains("vless://sandbox/", access.AccessUri, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await harness.Db.TelegramBotNotifications.CountAsync(x => x.Type == "payment_succeeded"));
        var notification = await harness.Db.TelegramBotNotifications.SingleAsync(x => x.Type == "payment_succeeded");
        Assert.Contains(access.AccessUri, notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Мои ключи", notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Продлить", notification.PayloadJson, StringComparison.OrdinalIgnoreCase);

        var adminSnapshot = JsonSerializer.Serialize(new
        {
            Dashboard = Assert.IsType<OkObjectResult>(await harness.DashboardController.GetSummary(CancellationToken.None)).Value,
            Users = Assert.IsType<OkObjectResult>(await harness.UsersController.GetList(null, null, null, CancellationToken.None)).Value,
            UserOverview = Assert.IsType<OkObjectResult>(await harness.UsersController.GetOverview(subscription.UserId, CancellationToken.None)).Value,
            Orders = Assert.IsType<OkObjectResult>(await harness.AdminController.GetOrders(null, null, CancellationToken.None)).Value,
            Payments = Assert.IsType<OkObjectResult>(await harness.AdminController.GetPayments(CancellationToken.None)).Value,
            Subscriptions = Assert.IsType<OkObjectResult>(await harness.AdminController.GetSubscriptions(CancellationToken.None)).Value,
            Accesses = Assert.IsType<OkObjectResult>(await harness.AdminController.GetAccessCredentials(CancellationToken.None)).Value,
            ProviderAccounts = Assert.IsType<OkObjectResult>(await harness.AdminController.GetPaymentProviderAccounts(CancellationToken.None)).Value
        });

        Assert.Contains(order.Id.ToString(), adminSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(payment.ProviderPaymentId, adminSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(subscription.Id.ToString(), adminSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(access.Id.ToString(), adminSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecretKeyProtected", adminSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WebhookSecretProtected", adminSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PanelPassword", adminSnapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", adminSnapshot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Renewal_Sandbox_E2E_Should_Extend_Existing_Subscription_Update_Access_And_Notify()
    {
        await using var harness = await SandboxHarness.CreateAsync();
        var tariff = await harness.SeedTariffNodeAndProviderAsync();
        await harness.CompleteTelegramPurchaseAsync(tariff, updateBase: 2000, webhookEventId: "evt-renew-base");
        var subscription = await harness.Db.Subscriptions.SingleAsync();
        var access = await harness.Db.AccessCredentials.SingleAsync();
        var originalExpiry = subscription.EndAt;

        access.Status = AccessCredentialStatus.Disabled;
        access.DisabledAt = harness.Clock.UtcNow.AddMinutes(-10);
        await harness.Db.SaveChangesAsync();

        var renewList = await harness.Bot.ProcessUpdateAsync(TelegramCallback(2010, "renew"), EmptyHeaders(), null, CancellationToken.None);
        var renewOrderResponse = await harness.Bot.ProcessUpdateAsync(TelegramCallback(2011, $"renew:{subscription.Id}"), EmptyHeaders(), null, CancellationToken.None);
        var renewalOrder = await harness.Db.Orders.OrderByDescending(x => x.CreatedAt).FirstAsync(x => x.Type == OrderType.Renewal);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(2012, $"pay:{renewalOrder.Id}:YooKassa"), EmptyHeaders(), null, CancellationToken.None);
        var renewalPayment = await harness.Db.Payments.SingleAsync(x => x.OrderId == renewalOrder.Id);

        var renewalWebhook = SandboxWebhook(renewalPayment.ProviderPaymentId, "evt-renew-1", renewalPayment.Amount, renewalPayment.Currency, renewalPayment.OrderId, shopId: "shop-yookassa");
        var result = await harness.Orchestrator.ProcessAsync(PaymentProvider.YooKassa, renewalWebhook, EmptyHeaders(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains("Мои подписки", renewList.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Заказ на продление", renewOrderResponse.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await harness.Db.Subscriptions.CountAsync());
        Assert.Equal(1, await harness.Db.AccessCredentials.CountAsync());
        Assert.Equal(1, harness.VpnProvider.UpdateCalls);

        var renewedSubscription = await harness.Db.Subscriptions.SingleAsync();
        var renewedAccess = await harness.Db.AccessCredentials.SingleAsync();
        Assert.True(renewedSubscription.EndAt > originalExpiry);
        Assert.Equal(SubscriptionStatus.Active, renewedSubscription.Status);
        Assert.Equal(AccessCredentialStatus.Active, renewedAccess.Status);
        Assert.Null(renewedAccess.DisabledAt);
        Assert.Contains("renewed", renewedAccess.AccessUri, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(await harness.Db.AccessCredentialHistories.ToListAsync(), x => x.EventType == "AccessRenewedAndEnabled");
        Assert.Contains(await harness.Db.TelegramBotNotifications.ToListAsync(), x => x.Type == "payment_succeeded" && x.PayloadJson.Contains(renewedAccess.AccessUri, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Expiry_E2E_Should_Disable_Access_Write_History_And_Surface_Admin_Status()
    {
        await using var harness = await SandboxHarness.CreateAsync();
        var seeded = await harness.SeedActiveSubscriptionWithAccessAsync(graceExpired: true);

        var processed = await harness.SubscriptionService.ProcessLifecycleAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(1, harness.VpnProvider.DisableCalls);
        var subscription = await harness.Db.Subscriptions.SingleAsync(x => x.Id == seeded.SubscriptionId);
        var access = await harness.Db.AccessCredentials.SingleAsync(x => x.Id == seeded.AccessId);
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.Equal(AccessCredentialStatus.Disabled, access.Status);
        Assert.NotNull(access.DisabledAt);
        Assert.Contains(await harness.Db.AccessCredentialHistories.ToListAsync(), x => x.EventType == "AccessDisabledOnExpiry");
        Assert.Contains(await harness.Db.AuditLogs.ToListAsync(), x => x.Action == "access.disable");

        var adminAccessJson = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await harness.AdminController.GetAccessCredentials(CancellationToken.None)).Value);
        Assert.Contains("Disabled", adminAccessJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("panel-password", adminAccessJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Expiry_Provider_Failure_Should_Not_Crash_And_Should_Mark_Access_Error()
    {
        await using var harness = await SandboxHarness.CreateAsync(throwOnDisable: true);
        var seeded = await harness.SeedActiveSubscriptionWithAccessAsync(graceExpired: true);

        var processed = await harness.SubscriptionService.ProcessLifecycleAsync(CancellationToken.None);

        Assert.Equal(0, processed);
        Assert.Equal(1, harness.VpnProvider.DisableCalls);
        var subscription = await harness.Db.Subscriptions.SingleAsync(x => x.Id == seeded.SubscriptionId);
        var access = await harness.Db.AccessCredentials.SingleAsync(x => x.Id == seeded.AccessId);
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
        Assert.Equal(1, subscription.LifecycleAttemptCount);
        Assert.NotNull(subscription.LifecycleNextAttemptAt);
        Assert.False(string.IsNullOrWhiteSpace(subscription.LifecycleLastError));
        Assert.Equal(AccessCredentialStatus.Error, access.Status);
        Assert.False(await harness.Db.OutboxMessages.AnyAsync(x => x.CorrelationId.StartsWith("subscription_expired:")));
        var historyJson = JsonSerializer.Serialize(await harness.Db.AccessCredentialHistories.ToListAsync());
        var auditJson = JsonSerializer.Serialize(await harness.Db.AuditLogs.ToListAsync());
        Assert.Contains("AccessDisabledOnExpiryFailed", historyJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("access.disable.failed", auditJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-disable-secret", historyJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-disable-secret", auditJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OwnVps_DryRun_E2E_Should_Protect_Credential_Process_Mock_Deploy_Create_Access_And_Admin_Visibility()
    {
        await using var harness = await SandboxHarness.CreateAsync();
        await harness.SeedTariffNodeAndProviderAsync();

        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(3000, "/start"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(3001, "register_tg"), EmptyHeaders(), null, CancellationToken.None);
        var start = await harness.Bot.ProcessUpdateAsync(TelegramCallback(3002, "own_vps"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(3003, "vps.example.test"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(3004, "2222"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(3005, "root"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(3006, "own_vps_auth:password"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(3007, "ssh-password-must-not-leak"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(3008, "Amsterdam customer VPS"), EmptyHeaders(), null, CancellationToken.None);
        var confirmed = await harness.Bot.ProcessUpdateAsync(TelegramCallback(3009, "own_vps_confirm"), EmptyHeaders(), null, CancellationToken.None);

        Assert.Contains("Validation mode", start.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Заявка создана", confirmed.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await harness.Db.ProvisioningRuns.CountAsync(x => x.Status == ProvisioningRunStatus.PrecheckQueued));
        Assert.Equal(0, await harness.Db.SupportConversations.CountAsync());

        var messagesJson = JsonSerializer.Serialize(await harness.Db.TelegramBotMessages.ToListAsync());
        var node = await harness.Db.VpnNodes.SingleAsync(x => x.Provider == "customer-vps");
        Assert.StartsWith("v1:", node.ProtectedSshCredential, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(node.SshPrivateKeyPath);
        Assert.DoesNotContain("ssh-password-must-not-leak", messagesJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ssh-password-must-not-leak", JsonSerializer.Serialize(await harness.Db.AuditLogs.ToListAsync()), StringComparison.OrdinalIgnoreCase);

        await harness.ProcessNextProvisioningRunAsync(new FakeProvisioningExecutor(success: true, phase: "precheck"));
        await harness.ProcessNextProvisioningRunAsync(new FakeProvisioningExecutor(success: true, phase: "deploy"));

        Assert.Equal(ProvisioningRunStatus.Deployed, (await harness.Db.ProvisioningRuns.OrderByDescending(x => x.CreatedAt).FirstAsync(x => !x.DryRun)).Status);
        Assert.Equal(NodeStatus.Ready, (await harness.Db.VpnNodes.SingleAsync(x => x.Id == node.Id)).Status);
        Assert.Equal(1, await harness.Db.VpnPanels.CountAsync());
        Assert.Equal(1, await harness.Db.VpnInbounds.CountAsync());
        Assert.Equal(1, await harness.Db.AccessCredentials.CountAsync());
        Assert.Contains(await harness.Db.TelegramBotNotifications.ToListAsync(), x => x.Type == "own_vps_deployed" && x.PayloadJson.Contains("vless://sandbox/", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await harness.Db.SupportConversations.CountAsync());

        var adminDetails = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await harness.AdminController.GetProvisioningRun((await harness.Db.ProvisioningRuns.OrderByDescending(x => x.CreatedAt).FirstAsync(x => !x.DryRun)).Id, CancellationToken.None)).Value);
        Assert.Contains("CredentialsConfigured", adminDetails, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LinkedAccessId", adminDetails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ssh-password-must-not-leak", adminDetails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("v1:", adminDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OwnVps_Deploy_Failure_Should_Roll_Back_Node_State_And_Surface_Admin_Context()
    {
        await using var harness = await SandboxHarness.CreateAsync();
        await harness.SeedTariffNodeAndProviderAsync();
        var userId = Guid.NewGuid();
        harness.Db.Users.Add(new User { Id = userId, Email = "rollback@example.test", DisplayName = "Rollback User", PasswordHash = "hash", RolesCsv = "User", Status = UserStatus.Active, ReferralCode = "RBK" });
        await harness.Db.SaveChangesAsync();

        var created = await harness.ProvisioningService.CreateOwnVpsRequestAsync(new OwnVpsProvisioningCommand(
            userId,
            300500,
            "rollback-vps.example.test",
            22,
            "root",
            "password",
            "ssh-password-must-not-leak",
            "Rollback VPS",
            "customer",
            "telegram"));
        Assert.True(created.IsSuccess, created.Error);

        await harness.ProcessNextProvisioningRunAsync(new FakeProvisioningExecutor(success: true, phase: "precheck"));
        var node = await harness.Db.VpnNodes.SingleAsync(x => x.Provider == "customer-vps");
        Assert.Equal(NodeStatus.New, node.Status);
        Assert.Equal(ProvisioningRunStatus.DeployQueued, node.ProvisioningStatus);
        Assert.False(node.IsAvailableForNewUsers);

        await harness.ProcessNextProvisioningRunAsync(new FakeProvisioningExecutor(success: false, phase: "deploy", error: "deploy failed password=raw-deploy-secret token=raw-deploy-secret"));

        var failedDeployRun = await harness.Db.ProvisioningRuns.OrderByDescending(x => x.CreatedAt).FirstAsync(x => !x.DryRun);
        var rolledBackNode = await harness.Db.VpnNodes.SingleAsync(x => x.Id == node.Id);
        Assert.Equal(ProvisioningRunStatus.Failed, failedDeployRun.Status);
        Assert.Equal(ProvisioningRunStatus.Failed, rolledBackNode.ProvisioningStatus);
        Assert.Equal(NodeStatus.New, rolledBackNode.Status);
        Assert.Equal(HealthStatus.Unknown, rolledBackNode.HealthStatus);
        Assert.False(rolledBackNode.IsAvailableForNewUsers);
        Assert.Equal(0, await harness.Db.AccessCredentials.CountAsync());
        Assert.Equal(0, await harness.Db.VpnPanels.CountAsync());
        Assert.Equal(1, await harness.Db.SupportConversations.CountAsync());

        var steps = await harness.Db.ProvisioningStepRuns.Where(x => x.ProvisioningRunId == failedDeployRun.Id).ToListAsync();
        Assert.Contains(steps, x => x.StepName == "Rollback node state" && x.Status == ProvisioningRunStatus.Succeeded);
        Assert.Contains(await harness.Db.AuditLogs.ToListAsync(), x => x.Action == "provisioning.rollback_applied" && x.EntityId == failedDeployRun.Id.ToString());
        Assert.Contains(await harness.Db.AuditLogs.ToListAsync(), x => x.Action == "provisioning.deploy_failed" && x.AfterJson.Contains("rollback", StringComparison.OrdinalIgnoreCase));

        var adminDetails = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await harness.AdminController.GetProvisioningRun(failedDeployRun.Id, CancellationToken.None)).Value);
        Assert.Contains("Rollback node state", adminDetails, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rollback applied", adminDetails, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Failed", adminDetails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-deploy-secret", adminDetails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ssh-password-must-not-leak", adminDetails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OwnVps_DryRun_Failure_Should_Create_Support_Context_And_Retry_Without_Duplicates()
    {
        await using var harness = await SandboxHarness.CreateAsync();
        await harness.SeedTariffNodeAndProviderAsync();

        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(4000, "/start"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(4001, "register_tg"), EmptyHeaders(), null, CancellationToken.None);
        var invalidHost = await harness.Bot.ProcessUpdateAsync(TelegramCallback(4002, "own_vps"), EmptyHeaders(), null, CancellationToken.None);
        var invalidHostResponse = await harness.Bot.ProcessUpdateAsync(TelegramUpdate(4003, "https://bad.example/path"), EmptyHeaders(), null, CancellationToken.None);
        Assert.Contains("Некорректный host", invalidHostResponse.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Validation mode", invalidHost.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);

        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(4004, "vps-failure.example.test"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(4005, "22"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(4006, "root"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(4007, "own_vps_auth:ssh_key"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(4008, "-----BEGIN PRIVATE KEY-----\nraw-private-key-must-not-leak\n-----END PRIVATE KEY-----"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramUpdate(4009, "Failure VPS"), EmptyHeaders(), null, CancellationToken.None);
        await harness.Bot.ProcessUpdateAsync(TelegramCallback(4010, "own_vps_confirm"), EmptyHeaders(), null, CancellationToken.None);

        await harness.ProcessNextProvisioningRunAsync(new FakeProvisioningExecutor(success: false, phase: "precheck", error: "precheck password=raw-private-key-must-not-leak failed"));
        var failedRun = await harness.Db.ProvisioningRuns.SingleAsync(x => x.Status == ProvisioningRunStatus.PrecheckFailed);
        Assert.Equal(1, await harness.Db.SupportConversations.CountAsync());
        Assert.Equal(0, await harness.Db.AccessCredentials.CountAsync());
        Assert.Equal(1, await harness.Db.VpnNodes.CountAsync(x => x.Provider == "customer-vps"));

        var redactedDetails = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await harness.AdminController.GetProvisioningRun(failedRun.Id, CancellationToken.None)).Value);
        Assert.Contains("PrecheckFailed", redactedDetails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-private-key-must-not-leak", redactedDetails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", redactedDetails, StringComparison.OrdinalIgnoreCase);

        var ownerUserId = Assert.IsType<Guid>(failedRun.RequestedByUserId);
        var actorUserId = Guid.NewGuid();
        var retry = await harness.ProvisioningService.RetryAsync(failedRun.Id, actorUserId, CancellationToken.None);
        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(ownerUserId, retry.Value!.RequestedByUserId);
        await harness.ProcessNextProvisioningRunAsync(new FakeProvisioningExecutor(success: true, phase: "retry-precheck"));
        await harness.ProcessNextProvisioningRunAsync(new FakeProvisioningExecutor(success: true, phase: "retry-deploy"));

        Assert.Equal(1, await harness.Db.VpnNodes.CountAsync(x => x.Provider == "customer-vps"));
        Assert.Equal(1, await harness.Db.AccessCredentials.CountAsync());
        Assert.Equal(1, await harness.Db.VpnPanels.CountAsync());
        Assert.Single(await harness.Db.Subscriptions.Where(x => x.UserId == ownerUserId).ToListAsync());
        Assert.Empty(await harness.Db.Subscriptions.Where(x => x.UserId == actorUserId).ToListAsync());
        Assert.Contains(await harness.Db.AuditLogs.ToListAsync(), x => x.Action == "provisioning.queue" && x.ActorId == actorUserId.ToString() && x.AfterJson.Contains(ownerUserId.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(await harness.Db.TelegramBotNotifications.ToListAsync(), x => x.Type == "own_vps_deployed");
    }

    [Fact]
    public async Task Admin_Management_E2E_Should_Surface_Tariffs_Providers_And_Linked_Business_Entities_Safely()
    {
        await using var harness = await SandboxHarness.CreateAsync();
        var admin = harness.AdminController;

        var createTariff = await admin.CreateTariff(new TariffCreateRequest(
            Name: "Admin monthly",
            Slug: "admin-monthly",
            Description: "Created by admin E2E",
            DurationDays: 30,
            Price: 590m,
            Currency: "RUB",
            MaxDevices: 5,
            IsActive: true,
            SortOrder: 1), CancellationToken.None);
        Assert.IsType<OkObjectResult>(createTariff);

        var providerAccount = await admin.CreatePaymentProviderAccount(new UpsertPaymentProviderAccountCommand(
            PaymentProvider.YooKassa,
            PaymentProviderMode.Sandbox,
            "admin-yookassa",
            "YooKassa sandbox",
            true,
            true,
            "admin-shop",
            "https://payments.example.test",
            "https://cabinet.example.test/payments",
            "https://api.example.test/api/webhooks/payments/yookassa",
            "secret-key-must-not-leak",
            "webhook-secret-must-not-leak",
            false,
            string.Empty,
            "{\"apiSecret\":\"raw-extra-secret-must-not-leak\"}"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(providerAccount);

        var publicTariffs = await new CatalogService(harness.Db).GetPublicTariffsAsync(CancellationToken.None);
        var publicProviders = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await new PaymentsController(harness.Db).GetAvailableProviders(CancellationToken.None)).Value);
        var providerAdminJson = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await admin.GetPaymentProviderAccounts(CancellationToken.None)).Value);

        Assert.Contains(publicTariffs, x => x.Slug == "admin-monthly");
        Assert.Contains("YooKassa", publicProviders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IsCheckoutConfigured", providerAdminJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-key-must-not-leak", providerAdminJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook-secret-must-not-leak", providerAdminJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-extra-secret-must-not-leak", providerAdminJson, StringComparison.OrdinalIgnoreCase);

        var disabledAccount = await admin.CreatePaymentProviderAccount(new UpsertPaymentProviderAccountCommand(
            PaymentProvider.RoboKassa,
            PaymentProviderMode.Disabled,
            "disabled-robo",
            "Disabled Robo",
            false,
            false,
            "disabled-shop",
            "https://disabled.example.test",
            "https://cabinet.example.test/payments",
            "https://api.example.test/api/webhooks/payments/robokassa",
            null,
            null,
            false,
            string.Empty,
            "{}"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(disabledAccount);

        var publicProvidersAfterDisabled = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await new PaymentsController(harness.Db).GetAvailableProviders(CancellationToken.None)).Value);
        Assert.DoesNotContain("Disabled Robo", publicProvidersAfterDisabled, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validation_Safety_Static_Checks_Should_Keep_Live_Integrations_Disabled()
    {
        var root = FindRepositoryRoot();
        var envExample = File.ReadAllText(Path.Combine(root, ".env.example"));
        var validationCompose = File.ReadAllText(Path.Combine(root, "docker-compose.validation.yml"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "staging-validation.yml"));
        var validateBackend = File.ReadAllText(Path.Combine(root, "scripts", "validate-backend.sh"));
        var validateDocker = File.ReadAllText(Path.Combine(root, "scripts", "validate-docker.sh"));
        var checkEfDrift = File.ReadAllText(Path.Combine(root, "scripts", "check-ef-drift.sh"));

        Assert.Contains("TelegramBot__Enabled=false", envExample, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provisioning__LiveExecutionEnabled=false", envExample, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provisioning__AllowLiveDeploy=false", envExample, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Vpn__X3Ui__Mode=Sandbox", envExample, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("X3UI_BASE_URL=", envExample, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("TelegramBot__Enabled", validationCompose, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"false\"", validationCompose, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provisioning__LiveExecutionEnabled", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provisioning__AllowLiveDeploy", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("X3UI_PASSWORD", workflow, StringComparison.OrdinalIgnoreCase);

        foreach (var blob in new[] { envExample, validationCompose, workflow })
        {
            Assert.DoesNotContain("sk_live_", blob, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ghp_", blob, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BEGIN PRIVATE KEY", blob, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(":AA", blob, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("check-validation-safety.sh", validateBackend, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("check-validation-safety.sh", validateDocker, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("check-validation-safety.sh", checkEfDrift, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Docker_Validation_Gate_Should_Cleanup_Temporary_Artifacts()
    {
        var root = FindRepositoryRoot();
        var validateDocker = File.ReadAllText(Path.Combine(root, "scripts", "validate-docker.sh"));
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "STAGING_VALIDATION_RUNBOOK.md"));

        foreach (var expected in new[]
                 {
                     "TMP_DIR=\"$(mktemp -d",
                     "CURL_OUTPUT_FILE=\"$TMP_DIR/curl-output.txt\"",
                     "COMPOSE_CONFIG_FILE=\"$TMP_DIR/compose-config.yml\"",
                     "RUNTIME_LOG_FILE=\"$TMP_DIR/runtime-logs.txt\"",
                     "rm -rf \"$TMP_DIR\"",
                     "trap cleanup EXIT"
                 })
        {
            Assert.Contains(expected, validateDocker, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var forbidden in new[]
                 {
                     ">/tmp/vpnplatform-curl-output.txt",
                     "2>/tmp/vpnplatform-curl-error.txt",
                     ">/tmp/vpnplatform-compose-config.yml",
                     ">/tmp/vpnplatform-runtime-logs.txt"
                 })
        {
            Assert.DoesNotContain(forbidden, validateDocker, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-docker.sh", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("temporary curl/config/log artifacts", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            validateDocker.IndexOf("trap cleanup EXIT", StringComparison.OrdinalIgnoreCase)
            < validateDocker.IndexOf("./scripts/check-validation-safety.sh", StringComparison.OrdinalIgnoreCase));
        Assert.True(
            validateDocker.IndexOf("trap cleanup EXIT", StringComparison.OrdinalIgnoreCase)
            < validateDocker.IndexOf("require docker", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyDictionary<string, string> EmptyHeaders() => new Dictionary<string, string>();

    private static string SandboxWebhook(string paymentId, string eventId, decimal amount, string currency, Guid orderId, string shopId)
        => JsonSerializer.Serialize(new
        {
            eventId,
            eventType = "payment.succeeded",
            paymentId,
            status = PaymentStatus.Succeeded.ToString(),
            paid = true,
            amount,
            currency,
            shopId,
            orderId = orderId.ToString("N")
        });

    private static string TelegramUpdate(long updateId, string text)
        => $$"""
        {
          "update_id": {{updateId}},
          "message": {
            "message_id": {{updateId + 1000}},
            "from": { "id": 888777, "is_bot": false, "first_name": "E2E", "username": "e2e_user", "language_code": "ru" },
            "chat": { "id": 888777, "type": "private" },
            "date": 1777466400,
            "text": {{JsonSerializer.Serialize(text)}}
          }
        }
        """;

    private static string TelegramCallback(long updateId, string data)
        => $$"""
        {
          "update_id": {{updateId}},
          "callback_query": {
            "id": "cb-e2e-{{updateId}}",
            "from": { "id": 888777, "is_bot": false, "first_name": "E2E", "username": "e2e_user", "language_code": "ru" },
            "message": { "message_id": {{updateId + 1000}}, "chat": { "id": 888777, "type": "private" } },
            "data": {{JsonSerializer.Serialize(data)}}
          }
        }
        """;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env.example")) && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for validation-safety static test.");
    }

    private sealed class SandboxHarness : IAsyncDisposable
    {
        private SandboxHarness(ApplicationDbContext db, MutableClock clock, TrackingVpnProvider vpnProvider, TelegramBotService bot, PaymentOrchestrator orchestrator, SubscriptionService subscriptionService, ProvisioningService provisioningService, AdminOperationsController adminController, AdminDashboardController dashboardController, AdminUsersController usersController)
        {
            Db = db;
            Clock = clock;
            VpnProvider = vpnProvider;
            Bot = bot;
            Orchestrator = orchestrator;
            SubscriptionService = subscriptionService;
            ProvisioningService = provisioningService;
            AdminController = adminController;
            DashboardController = dashboardController;
            UsersController = usersController;
        }

        public ApplicationDbContext Db { get; }
        public MutableClock Clock { get; }
        public TrackingVpnProvider VpnProvider { get; }
        public TelegramBotService Bot { get; }
        public PaymentOrchestrator Orchestrator { get; }
        public SubscriptionService SubscriptionService { get; }
        public ProvisioningService ProvisioningService { get; }
        public AdminOperationsController AdminController { get; }
        public AdminDashboardController DashboardController { get; }
        public AdminUsersController UsersController { get; }

        public static Task<SandboxHarness> CreateAsync(bool throwOnDisable = false)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            var db = new ApplicationDbContext(options);
            var clock = new MutableClock(new DateTimeOffset(2026, 5, 20, 9, 0, 0, TimeSpan.Zero));
            var secretProtector = new TestSecretProtector();
            var vpnProvider = new TrackingVpnProvider(throwOnDisable);
            var vpnProviderFactory = new TrackingVpnProviderFactory(vpnProvider);
            var nodeAllocation = new NodeAllocationService(db);
            var lifecycle = new VpnAccessLifecycleService(db, vpnProviderFactory, clock);
            var subscriptionService = new SubscriptionService(db, clock, nodeAllocation, vpnProviderFactory, lifecycle);
            var providerAccountService = new PaymentProviderAccountService(db, secretProtector, clock);
            var paymentProvider = new SandboxPaymentProvider(PaymentProvider.YooKassa);
            var paymentFactory = new PaymentProviderFactory(new IPaymentProvider[] { paymentProvider });
            var orchestrator = new PaymentOrchestrator(db, paymentFactory, new IPaymentWebhookVerifier[] { paymentProvider }, providerAccountService, subscriptionService, clock);
            var orderService = new OrderService(db, clock);
            var provisioningService = new ProvisioningService(db, clock, secretProtector);
            var bot = new TelegramBotService(db, clock, orderService, orchestrator, subscriptionService, provisioningService: provisioningService, secretProtector: secretProtector);
            var admin = new AdminOperationsController(db, provisioningService, orchestrator, providerAccountService, lifecycle, secretProtector, qrCodeGenerator: null);
            admin.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var dashboard = new AdminDashboardController(db);
            dashboard.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, UserRoles.Admin)], "test"))
                }
            };
            var users = new AdminUsersController(db);
            users.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, UserRoles.Admin)], "test"))
                }
            };
            return Task.FromResult(new SandboxHarness(db, clock, vpnProvider, bot, orchestrator, subscriptionService, provisioningService, admin, dashboard, users));
        }

        public async Task<Tariff> SeedTariffNodeAndProviderAsync()
        {
            var tariff = new Tariff
            {
                Id = Guid.NewGuid(),
                Name = "Monthly",
                Slug = "monthly",
                Description = "Sandbox monthly tariff",
                DurationDays = 30,
                Price = 490m,
                Currency = "RUB",
                MaxDevices = 3,
                IsActive = true,
                SortOrder = 1,
                Category = "vpn"
            };
            var node = new VpnNode
            {
                Id = Guid.NewGuid(),
                Name = "sandbox-node",
                Host = "127.0.0.1",
                IpAddress = "127.0.0.1",
                Provider = "x3ui",
                Region = "test",
                Country = "RU",
                Datacenter = "local",
                Status = NodeStatus.Ready,
                HealthStatus = HealthStatus.Healthy,
                Capacity = 100,
                UsedCapacity = 0,
                IsAvailableForNewUsers = true,
                SupportedProtocolsCsv = "vless"
            };
            var account = new PaymentProviderAccount
            {
                Id = Guid.NewGuid(),
                Provider = PaymentProvider.YooKassa,
                Mode = PaymentProviderMode.Sandbox,
                Name = "yookassa-sandbox",
                PublicName = "YooKassa Sandbox",
                IsEnabled = true,
                IsDefault = true,
                ShopId = "shop-yookassa",
                ApiBaseUrl = "https://payments.example.test",
                ReturnUrl = "https://cabinet.example.test/payments",
                SecretKeyProtected = string.Empty,
                WebhookSecretProtected = string.Empty,
                UseWebhookIpAllowList = false,
                ExtraSettingsJson = "{}",
                HealthStatus = HealthStatus.Healthy,
                CreatedAt = Clock.UtcNow,
                UpdatedAt = Clock.UtcNow
            };
            Db.Tariffs.Add(tariff);
            Db.VpnNodes.Add(node);
            Db.PaymentProviderAccounts.Add(account);
            await Db.SaveChangesAsync();
            return tariff;
        }

        public async Task CompleteTelegramPurchaseAsync(Tariff tariff, long updateBase, string webhookEventId)
        {
            await Bot.ProcessUpdateAsync(TelegramUpdate(updateBase, "/start"), EmptyHeaders(), null, CancellationToken.None);
            await Bot.ProcessUpdateAsync(TelegramCallback(updateBase + 1, "register_tg"), EmptyHeaders(), null, CancellationToken.None);
            await Bot.ProcessUpdateAsync(TelegramCallback(updateBase + 2, $"buy:{tariff.Id}"), EmptyHeaders(), null, CancellationToken.None);
            await Bot.ProcessUpdateAsync(TelegramCallback(updateBase + 3, $"confirm_order:{tariff.Id}"), EmptyHeaders(), null, CancellationToken.None);
            var order = await Db.Orders.OrderByDescending(x => x.CreatedAt).FirstAsync(x => x.Type == OrderType.NewSubscription);
            await Bot.ProcessUpdateAsync(TelegramCallback(updateBase + 4, $"pay:{order.Id}:YooKassa"), EmptyHeaders(), null, CancellationToken.None);
            var payment = await Db.Payments.SingleAsync(x => x.OrderId == order.Id);
            var webhook = SandboxWebhook(payment.ProviderPaymentId, webhookEventId, payment.Amount, payment.Currency, payment.OrderId, shopId: "shop-yookassa");
            var result = await Orchestrator.ProcessAsync(PaymentProvider.YooKassa, webhook, EmptyHeaders(), CancellationToken.None);
            Assert.True(result.IsSuccess, result.Error);
        }

        public async Task<(Guid UserId, Guid SubscriptionId, Guid AccessId)> SeedActiveSubscriptionWithAccessAsync(bool graceExpired)
        {
            var user = new User { Id = Guid.NewGuid(), Email = "expiry@example.test", DisplayName = "Expiry User", PasswordHash = "hash", RolesCsv = "User", Status = UserStatus.Active, ReferralCode = "EXP" };
            var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Expiry tariff", Slug = "expiry", Description = "Expiry", DurationDays = 30, Price = 100, Currency = "RUB", MaxDevices = 1, IsActive = true };
            var node = new VpnNode { Id = Guid.NewGuid(), Name = "expiry-node", Host = "127.0.0.1", IpAddress = "127.0.0.1", Provider = "x3ui", Region = "test", Country = "RU", Datacenter = "local", Status = NodeStatus.Ready, HealthStatus = HealthStatus.Healthy, Capacity = 100, IsAvailableForNewUsers = true };
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TariffId = tariff.Id,
                Status = graceExpired ? SubscriptionStatus.GracePeriod : SubscriptionStatus.Active,
                StartAt = Clock.UtcNow.AddDays(-40),
                EndAt = Clock.UtcNow.AddDays(-5),
                GracePeriodEndAt = Clock.UtcNow.AddDays(-1),
                SourceChannel = ChannelType.Telegram,
                CurrentServerId = node.Id
            };
            var access = new AccessCredential
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                ServerId = node.Id,
                ProviderType = "x3ui",
                ProviderAccessId = $"client-{subscription.Id:N}",
                AccessUri = $"vless://sandbox/{subscription.Id:N}@vpn.test:443",
                QrCodePath = $"vless://sandbox/{subscription.Id:N}@vpn.test:443",
                ConfigPath = string.Empty,
                Status = AccessCredentialStatus.Active,
                IssuedAt = Clock.UtcNow.AddDays(-35),
                LastSyncedAt = Clock.UtcNow.AddDays(-1)
            };
            subscription.CurrentAccessId = access.Id;
            Db.Users.Add(user);
            Db.Tariffs.Add(tariff);
            Db.VpnNodes.Add(node);
            Db.Subscriptions.Add(subscription);
            Db.AccessCredentials.Add(access);
            await Db.SaveChangesAsync();
            return (user.Id, subscription.Id, access.Id);
        }

        public async Task ProcessNextProvisioningRunAsync(IProvisioningExecutor executor)
        {
            var run = await Db.ProvisioningRuns
                .OrderBy(x => x.CreatedAt)
                .FirstAsync(x => x.Status == ProvisioningRunStatus.Pending
                    || x.Status == ProvisioningRunStatus.PrecheckQueued
                    || x.Status == ProvisioningRunStatus.DeployQueued
                    || x.Status == ProvisioningRunStatus.Retrying);
            var worker = new ProvisioningWorker(new ServiceCollection().BuildServiceProvider(), NullLogger<ProvisioningWorker>.Instance);
            var method = typeof(ProvisioningWorker).GetMethod("ProcessRunAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(worker, new object[] { Db, executor, new TestSecretProtector(), new TrackingVpnProviderFactory(VpnProvider), Clock, run, CancellationToken.None })!;
            await task;
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class MutableClock : IClock
    {
        public MutableClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => "v1:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        public string Unprotect(string protectedValue) => Encoding.UTF8.GetString(Convert.FromBase64String(protectedValue[3..]));
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"***{value[^Math.Min(visibleTail, value.Length)..]}";
    }

    private sealed class SandboxPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier
    {
        public SandboxPaymentProvider(PaymentProvider provider) => Provider = provider;
        public PaymentProvider Provider { get; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentInitResult($"sandbox_{request.Order.Id:N}", $"https://payments.example.test/{request.Order.Id:N}", "{\"sandbox\":true}"));

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            var statusText = root.GetProperty("status").GetString() ?? PaymentStatus.Unknown.ToString();
            var status = Enum.TryParse<PaymentStatus>(statusText, true, out var parsed) ? parsed : PaymentStatus.Unknown;
            return Task.FromResult(new PaymentWebhookParseResult(
                root.GetProperty("eventId").GetString() ?? string.Empty,
                root.GetProperty("eventType").GetString() ?? "payment.succeeded",
                root.GetProperty("paymentId").GetString() ?? string.Empty,
                status,
                rawBody,
                SignatureValidated: true,
                Amount: root.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : null,
                Currency: root.TryGetProperty("currency", out var currency) ? currency.GetString() : null,
                Paid: root.TryGetProperty("paid", out var paid) ? paid.GetBoolean() : null,
                ProviderAccountExternalId: root.TryGetProperty("shopId", out var shopId) ? shopId.GetString() : null,
                InternalOrderId: root.TryGetProperty("orderId", out var orderId) ? orderId.GetString() : null));
        }

        public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentWebhookVerificationResult(true, "sandbox-hmac", null));

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentStatusResult(payment.ProviderPaymentId, payment.Status, "{\"sandbox\":true}"));

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentRefundResult($"refund_{payment.Id:N}", RefundStatus.Pending, "{\"sandbox\":true}"));
    }

    private sealed class TrackingVpnProviderFactory : IVpnProviderFactory
    {
        private readonly TrackingVpnProvider _provider;
        public TrackingVpnProviderFactory(TrackingVpnProvider provider) => _provider = provider;
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TrackingVpnProvider : IVpnProvider
    {
        private readonly bool _throwOnDisable;
        public TrackingVpnProvider(bool throwOnDisable = false) => _throwOnDisable = throwOnDisable;
        public string Name => "x3ui";
        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int DisableCalls { get; private set; }

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
        {
            CreateCalls += 1;
            var uri = $"vless://sandbox/{request.SubscriptionId:N}@vpn.test:443?node={request.NodeId:N}&until={request.EndsAt:yyyyMMdd}";
            return Task.FromResult(new VpnProvisionResult($"sandbox-client-{request.SubscriptionId:N}", uri, uri, "/config/sandbox.json"));
        }

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
        {
            UpdateCalls += 1;
            var uri = $"vless://sandbox/renewed-{request.SubscriptionId:N}@vpn.test:443?node={request.NodeId:N}&until={request.EndsAt:yyyyMMdd}";
            return Task.FromResult(new VpnProvisionResult($"sandbox-client-{request.SubscriptionId:N}", uri, uri, "/config/sandbox.json"));
        }

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken)
        {
            DisableCalls += 1;
            if (_throwOnDisable)
            {
                throw new InvalidOperationException("provider disable failed password=raw-disable-secret token=raw-disable-secret");
            }
            return Task.CompletedTask;
        }

        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 1024, 1, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }

    private sealed class FakeProvisioningExecutor : IProvisioningExecutor
    {
        private readonly bool _success;
        private readonly string _phase;
        private readonly string _error;

        public FakeProvisioningExecutor(bool success, string phase, string? error = null)
        {
            _success = success;
            _phase = phase;
            _error = error ?? string.Empty;
        }

        public Task<ProvisioningExecutionResult> ExecuteAsync(VpnNode node, ProvisioningRun run, CancellationToken cancellationToken)
        {
            var steps = new[]
            {
                new ProvisioningStepResult("Validate input", true, $"{_phase}: host={node.Host}; credentials=***"),
                new ProvisioningStepResult("Check SSH config", _success, _success ? "validation/mock ssh ok" : _error, _success ? null : _error),
                new ProvisioningStepResult(run.DryRun ? "Check OS" : "Deploy x3-ui", _success, _success ? "validation/mock completed" : _error, _success ? null : _error)
            };
            var summary = _success
                ? $"{_phase} succeeded in validation/mock mode. No live SSH or Ansible executed."
                : $"{_phase} failed: {_error}";
            return Task.FromResult(new ProvisioningExecutionResult(_success, summary, steps, ArtifactDirectory: null, ErrorText: _success ? null : _error));
        }
    }
}
