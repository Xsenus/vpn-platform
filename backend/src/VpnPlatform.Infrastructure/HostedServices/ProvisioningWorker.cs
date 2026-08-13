using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;

namespace VpnPlatform.Infrastructure.HostedServices;

public sealed class ProvisioningWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProvisioningWorker> _logger;

    public ProvisioningWorker(IServiceProvider serviceProvider, ILogger<ProvisioningWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var recoveryScope = _serviceProvider.CreateScope())
                {
                    var coordinator = recoveryScope.ServiceProvider.GetRequiredService<ProvisioningRunCoordinator>();
                    var recovered = await coordinator.RecoverExpiredClaimsAsync(stoppingToken);
                    if (recovered > 0)
                    {
                        _logger.LogWarning("Provisioning worker recovered {RecoveredCount} expired claims for operator review.", recovered);
                    }
                }

                if (!await ProcessNextRunAsync(stoppingToken))
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provisioning worker loop failed.");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextRunAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var coordinator = scope.ServiceProvider.GetRequiredService<ProvisioningRunCoordinator>();
        var runIds = await coordinator.GetClaimableIdsAsync(10, cancellationToken);
        foreach (var runId in runIds)
        {
            if (!await coordinator.TryClaimAsync(runId, cancellationToken))
            {
                continue;
            }

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var run = await db.ProvisioningRuns.FirstAsync(x => x.Id == runId, cancellationToken);
            try
            {
                await ProcessRunAsync(
                    db,
                    scope.ServiceProvider.GetRequiredService<IProvisioningExecutor>(),
                    scope.ServiceProvider.GetRequiredService<ISecretProtector>(),
                    scope.ServiceProvider.GetRequiredService<IVpnProviderFactory>(),
                    scope.ServiceProvider.GetRequiredService<IClock>(),
                    run,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await FailClaimedRunWithFreshScopeAsync(
                    runId,
                    "Provisioning worker was interrupted during execution. Automatic replay is blocked because external changes may have partially completed.");
                throw;
            }
            catch (Exception ex)
            {
                await FailClaimedRunWithFreshScopeAsync(runId, ex.Message);
                _logger.LogError(ex, "Provisioning run {RunId} failed outside the controlled executor result.", runId);
            }

            return true;
        }

        return false;
    }

    private async Task FailClaimedRunWithFreshScopeAsync(Guid runId, string error)
    {
        using var scope = _serviceProvider.CreateScope();
        var coordinator = scope.ServiceProvider.GetRequiredService<ProvisioningRunCoordinator>();
        await coordinator.FailClaimedRunAsync(runId, error, CancellationToken.None);
    }

    private async Task ProcessRunAsync(
        ApplicationDbContext db,
        IProvisioningExecutor executor,
        ISecretProtector secretProtector,
        IVpnProviderFactory vpnProviderFactory,
        IClock clock,
        ProvisioningRun run,
        CancellationToken cancellationToken)
    {
        var node = await db.VpnNodes.FirstOrDefaultAsync(x => x.Id == run.NodeId, cancellationToken);
        if (node is null)
        {
            StatusStateMachine.SetProvisioningRunStatus(run, ProvisioningRunStatus.Failed, clock.UtcNow);
            run.FinishedAt = clock.UtcNow;
            run.ProcessingStartedAt = null;
            run.LeaseExpiresAt = null;
            run.LastError = "Provisioning failed: node not found.";
            run.ExecutionLog = "Provisioning failed: node not found.";
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var deployRollbackSnapshot = run.DryRun ? null : NodeRollbackSnapshot.Capture(node);
        var now = clock.UtcNow;
        StatusStateMachine.SetProvisioningRunStatus(run, run.DryRun ? ProvisioningRunStatus.Prechecking : ProvisioningRunStatus.Deploying, now);
        run.StartedAt = now;
        run.ExecutionLog = ProvisioningService.AppendLog(run.ExecutionLog, run.DryRun ? $"Precheck started for node {node.Name}." : $"Deploy started for node {node.Name}.");
        node.ProvisioningStatus = run.Status;
        if (!run.DryRun)
        {
            node.Status = NodeStatus.Provisioning;
            node.IsAvailableForNewUsers = false;
        }

        AddStep(db, run.Id, run.DryRun ? "Precheck started" : "Deploy started", run.Status, "Worker accepted provisioning run.", string.Empty, now);
        await QueueTelegramNotificationAsync(db, node, run.DryRun ? "own_vps_precheck_started" : "own_vps_deploy_started", run.DryRun
            ? $"Precheck VPS начался для {node.Host}. Validation mode не делает live SSH без explicit flag."
            : $"Deploy VPS начался для {node.Host}. Validation mode использует mock deployment без live SSH.", clock, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        ProvisioningExecutionResult result;
        try
        {
            result = await executor.ExecuteAsync(node, run, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result = new ProvisioningExecutionResult(false, "Provisioning executor failed with a controlled error.", new[]
            {
                new ProvisioningStepResult("executor", false, string.Empty, ex.Message)
            }, null, ex.Message);
        }

        now = clock.UtcNow;
        foreach (var step in result.Steps)
        {
            AddStep(db, run.Id, step.StepName, step.Success ? ProvisioningRunStatus.Succeeded : ProvisioningRunStatus.Failed, step.Output, step.ErrorText ?? string.Empty, now);
        }

        if (run.DryRun)
        {
            await CompletePrecheckAsync(db, node, run, result, clock, cancellationToken);
        }
        else
        {
            await CompleteDeployAsync(db, secretProtector, vpnProviderFactory, node, run, result, deployRollbackSnapshot, clock, cancellationToken);
        }

        run.ProcessingStartedAt = null;
        run.LeaseExpiresAt = null;
        run.LastError = result.Success
            ? null
            : ProvisioningService.RedactSensitiveText(result.ErrorText ?? result.SummaryLog, 1000);

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Provisioning run {RunId} completed for node {NodeId} with status {Status}", run.Id, node.Id, run.Status);
    }

    private static async Task CompletePrecheckAsync(ApplicationDbContext db, VpnNode node, ProvisioningRun run, ProvisioningExecutionResult result, IClock clock, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        run.FinishedAt = now;
        run.ExecutionLog = ProvisioningService.RedactSensitiveText(string.IsNullOrWhiteSpace(result.SummaryLog)
            ? (result.Success ? "Precheck succeeded." : "Precheck failed.")
            : result.SummaryLog, 8000);

        if (!result.Success)
        {
            StatusStateMachine.SetProvisioningRunStatus(run, ProvisioningRunStatus.PrecheckFailed, now);
            node.ProvisioningStatus = ProvisioningRunStatus.PrecheckFailed;
            node.Status = NodeStatus.Error;
            node.IsAvailableForNewUsers = false;
            node.UpdatedAt = now;
            await EnsureSupportConversationAsync(db, node, run, "Own VPS precheck failed", result.ErrorText ?? result.SummaryLog, clock, cancellationToken);
            await QueueTelegramNotificationAsync(db, node, "own_vps_precheck_failed", $"Precheck VPS не прошёл: {ProvisioningService.RedactSensitiveText(result.ErrorText ?? result.SummaryLog, 1000)}\n\nМы создали обращение в поддержку. Нажмите «Поддержка», если хотите добавить детали.", clock, cancellationToken);
            AddAudit(db, "provisioning.precheck_failed", "ProvisioningRun", run.Id, run.RequestedByUserId, new { nodeId = node.Id, error = ProvisioningService.RedactSensitiveText(result.ErrorText ?? result.SummaryLog, 1000) });
            return;
        }

        StatusStateMachine.SetProvisioningRunStatus(run, ProvisioningRunStatus.ReadyToDeploy, now);
        node.ProvisioningStatus = ProvisioningRunStatus.ReadyToDeploy;
        node.Status = NodeStatus.New;
        node.UpdatedAt = now;
        AddAudit(db, "provisioning.precheck_succeeded", "ProvisioningRun", run.Id, run.RequestedByUserId, new { nodeId = node.Id, dryRun = true });

        if (ProvisioningService.ShouldAutoDeployAfterPrecheck(node))
        {
            var deployRun = new ProvisioningRun
            {
                NodeId = node.Id,
                Status = ProvisioningRunStatus.DeployQueued,
                RequestedByUserId = run.RequestedByUserId,
                DryRun = false,
                StartedAt = now,
                ExecutionLog = "Deploy queued automatically after successful own VPS precheck. Validation mode remains safe unless live flags are explicitly enabled."
            };
            db.ProvisioningRuns.Add(deployRun);
            AddStep(db, deployRun.Id, "Deploy queued", ProvisioningRunStatus.DeployQueued, "Deploy was queued after precheck success.", string.Empty, now);
            node.ProvisioningStatus = ProvisioningRunStatus.DeployQueued;
            await QueueTelegramNotificationAsync(db, node, "own_vps_deploy_queued", $"Precheck VPS успешен. Deploy поставлен в очередь для {node.Host}. В validation mode live SSH не выполняется.", clock, cancellationToken);
            AddAudit(db, "provisioning.deploy_queued_after_precheck", "ProvisioningRun", deployRun.Id, run.RequestedByUserId, new { nodeId = node.Id, precheckRunId = run.Id });
        }
        else
        {
            await QueueTelegramNotificationAsync(db, node, "own_vps_ready_to_deploy", $"Precheck VPS успешен. Сервер {node.Host} готов к deploy. Админ может запустить deploy из панели.", clock, cancellationToken);
        }
    }

    private static async Task CompleteDeployAsync(ApplicationDbContext db, ISecretProtector secretProtector, IVpnProviderFactory vpnProviderFactory, VpnNode node, ProvisioningRun run, ProvisioningExecutionResult result, NodeRollbackSnapshot? rollbackSnapshot, IClock clock, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        run.FinishedAt = now;
        run.ExecutionLog = ProvisioningService.RedactSensitiveText(string.IsNullOrWhiteSpace(result.SummaryLog)
            ? (result.Success ? "Deploy succeeded." : "Deploy failed.")
            : result.SummaryLog, 8000);

        if (!result.Success)
        {
            StatusStateMachine.SetProvisioningRunStatus(run, ProvisioningRunStatus.Failed, now);
            ApplyDeployFailureRollback(db, node, run, rollbackSnapshot, result, now);
            await EnsureSupportConversationAsync(db, node, run, "Own VPS deploy failed", result.ErrorText ?? result.SummaryLog, clock, cancellationToken);
            await QueueTelegramNotificationAsync(db, node, "own_vps_deploy_failed", $"Deploy VPS завершился ошибкой: {ProvisioningService.RedactSensitiveText(result.ErrorText ?? result.SummaryLog, 1000)}\n\nСостояние сервера откатили к последнему понятному состоянию. Мы создали обращение в поддержку. Админ может сделать retry.", clock, cancellationToken);
            AddAudit(db, "provisioning.deploy_failed", "ProvisioningRun", run.Id, run.RequestedByUserId, new { nodeId = node.Id, error = ProvisioningService.RedactSensitiveText(result.ErrorText ?? result.SummaryLog, 1000), rollback = rollbackSnapshot is not null ? "applied" : "snapshot-missing" });
            return;
        }

        StatusStateMachine.SetProvisioningRunStatus(run, ProvisioningRunStatus.Deployed, now);
        node.ProvisioningStatus = ProvisioningRunStatus.Deployed;
        node.Status = NodeStatus.Ready;
        node.HealthStatus = HealthStatus.Healthy;
        node.LastHealthCheckAt = now;
        node.IsAvailableForNewUsers = !ProvisioningService.IsOwnVpsNode(node);
        node.InstalledVersion = string.IsNullOrWhiteSpace(node.InstalledVersion) ? "x3-ui (validation/mock)" : node.InstalledVersion;
        node.BackupStatus = string.IsNullOrWhiteSpace(node.BackupStatus) || node.BackupStatus == "unknown" ? "validation-configured" : node.BackupStatus;
        node.LoggingStatus = string.IsNullOrWhiteSpace(node.LoggingStatus) || node.LoggingStatus == "unknown" ? "validation-configured" : node.LoggingStatus;
        node.MonitoringStatus = string.IsNullOrWhiteSpace(node.MonitoringStatus) || node.MonitoringStatus == "unknown" ? "validation-configured" : node.MonitoringStatus;
        node.UpdatedAt = now;

        await EnsurePanelAndInboundAsync(db, secretProtector, node, now, cancellationToken);
        var accessMessage = await EnsureOwnVpsAccessAsync(db, vpnProviderFactory, node, run, clock, cancellationToken);
        await QueueTelegramNotificationAsync(db, node, "own_vps_deployed", $"VPN на вашем VPS готов ✅\nСервер: {node.Name} ({node.Host})\n{accessMessage}\n\nИнструкция: импортируйте VPN URI в VLESS/Xray-compatible клиент. Если возникнут проблемы — нажмите «Поддержка».", clock, cancellationToken, BuildPostPaymentReplyMarkupJson());
        AddAudit(db, "provisioning.deploy_succeeded", "ProvisioningRun", run.Id, run.RequestedByUserId, new { nodeId = node.Id, accessMessage });
    }

    private static void ApplyDeployFailureRollback(ApplicationDbContext db, VpnNode node, ProvisioningRun run, NodeRollbackSnapshot? snapshot, ProvisioningExecutionResult result, DateTimeOffset now)
    {
        if (snapshot is null)
        {
            node.ProvisioningStatus = ProvisioningRunStatus.Failed;
            node.Status = NodeStatus.Error;
            node.IsAvailableForNewUsers = false;
            node.UpdatedAt = now;
            run.ExecutionLog = ProvisioningService.AppendLog(run.ExecutionLog, "Rollback skipped: node snapshot was not available. Node marked Error for operator review.");
            AddStep(db, run.Id, "Rollback node state", ProvisioningRunStatus.Failed, "Rollback skipped because node snapshot was not available. Node marked Error for operator review.", result.ErrorText ?? result.SummaryLog, now);
            AddAudit(db, "provisioning.rollback_missing_snapshot", "ProvisioningRun", run.Id, run.RequestedByUserId, new { nodeId = node.Id, error = ProvisioningService.RedactSensitiveText(result.ErrorText ?? result.SummaryLog, 1000) });
            return;
        }

        snapshot.ApplyTo(node, now);
        node.ProvisioningStatus = ProvisioningRunStatus.Failed;
        node.UpdatedAt = now;

        var rollbackSummary = $"Rollback applied after deploy failure. Node restored to status={node.Status}, availableForNewUsers={node.IsAvailableForNewUsers}, health={node.HealthStatus}.";
        run.ExecutionLog = ProvisioningService.AppendLog(run.ExecutionLog, rollbackSummary);
        AddStep(db, run.Id, "Rollback node state", ProvisioningRunStatus.Succeeded, rollbackSummary, string.Empty, now);
        AddAudit(db, "provisioning.rollback_applied", "ProvisioningRun", run.Id, run.RequestedByUserId, new
        {
            nodeId = node.Id,
            restored = new
            {
                node.Status,
                node.HealthStatus,
                node.IsAvailableForNewUsers,
                node.InstalledVersion,
                node.BackupStatus,
                node.MonitoringStatus,
                node.LoggingStatus
            }
        });
    }

    private static async Task EnsurePanelAndInboundAsync(ApplicationDbContext db, ISecretProtector secretProtector, VpnNode node, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var panelBaseUrl = string.IsNullOrWhiteSpace(node.PanelBaseUrl) ? $"https://{node.Host}:2053" : node.PanelBaseUrl;
        var panel = await db.VpnPanels.FirstOrDefaultAsync(x => x.BaseUrl == panelBaseUrl, cancellationToken);
        if (panel is null)
        {
            panel = new VpnPanel
            {
                Name = $"own-vps-{node.Id:N}",
                BaseUrl = panelBaseUrl,
                Region = string.IsNullOrWhiteSpace(node.Region) ? "customer" : node.Region,
                Status = VpnPanelStatus.Active,
                HealthStatus = HealthStatus.Healthy,
                Login = string.IsNullOrWhiteSpace(node.PanelUsername) ? "admin" : node.PanelUsername,
                EncryptedPassword = secretProtector.Protect($"validation-panel-{node.Id:N}"),
                SslVerificationMode = VpnSslVerificationMode.AllowSelfSigned,
                ApiVariant = X3UiApiVariant.ThreeXUi,
                Capacity = Math.Max(10, node.Capacity),
                UsedCapacity = 0,
                AutoCreateInbound = true,
                DefaultInboundTemplateJson = "{}",
                LastHealthCheckAt = now,
                LastSyncAt = now,
                Version = "validation-mock"
            };
            db.VpnPanels.Add(panel);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            panel.Status = VpnPanelStatus.Active;
            panel.HealthStatus = HealthStatus.Healthy;
            panel.LastHealthCheckAt = now;
            panel.LastSyncAt = now;
            panel.UpdatedAt = now;
            panel.Revision = checked(panel.Revision + 1);
        }

        var inbound = await db.VpnInbounds.FirstOrDefaultAsync(x => x.VpnPanelId == panel.Id && x.ExternalInboundId == $"own-vps-inbound-{node.Id:N}", cancellationToken);
        if (inbound is null)
        {
            db.VpnInbounds.Add(new VpnInbound
            {
                VpnPanelId = panel.Id,
                ExternalInboundId = $"own-vps-inbound-{node.Id:N}",
                Name = "Own VPS VLESS",
                Protocol = "vless",
                Port = node.PublicPort > 0 ? node.PublicPort : 443,
                Listen = string.Empty,
                SettingsJson = "{\"clients\":[]}",
                StreamSettingsJson = "{\"network\":\"tcp\",\"security\":\"reality\"}",
                SniffingJson = "{}",
                IsDefault = true,
                IsActive = true,
                Capacity = panel.Capacity
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<string> EnsureOwnVpsAccessAsync(ApplicationDbContext db, IVpnProviderFactory vpnProviderFactory, VpnNode node, ProvisioningRun run, IClock clock, CancellationToken cancellationToken)
    {
        if (!run.RequestedByUserId.HasValue)
        {
            return "Пользователь не привязан: access не создан автоматически. Админ может выдать доступ вручную.";
        }

        var tariff = await EnsureOwnVpsTariffAsync(db, cancellationToken);
        var subscriptionCandidates = await db.Subscriptions
            .Include(x => x.CurrentAccess)
            .Where(x => x.UserId == run.RequestedByUserId.Value && x.CurrentServerId == node.Id && x.Status != SubscriptionStatus.Cancelled && x.Status != SubscriptionStatus.Blocked)
            .ToListAsync(cancellationToken);
        var subscription = subscriptionCandidates
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (subscription is null)
        {
            subscription = new Subscription
            {
                UserId = run.RequestedByUserId.Value,
                TariffId = tariff.Id,
                Status = SubscriptionStatus.Active,
                StartAt = clock.UtcNow,
                EndAt = clock.UtcNow.AddDays(Math.Max(1, tariff.DurationDays)),
                SourceChannel = ChannelType.Telegram,
                CurrentServerId = node.Id,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            };
            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            StatusStateMachine.SetSubscriptionStatus(subscription, SubscriptionStatus.Active, clock.UtcNow);
            if (subscription.EndAt < clock.UtcNow.AddDays(1))
            {
                subscription.EndAt = clock.UtcNow.AddDays(Math.Max(1, tariff.DurationDays));
            }
            subscription.CurrentServerId = node.Id;
        }

        var provider = vpnProviderFactory.Get("x3ui");
        var result = await provider.CreateAccessAsync(new VpnProvisionRequest(subscription.Id, subscription.UserId, subscription.TariffId, node.Id, subscription.EndAt, Math.Max(1, tariff.MaxDevices)), cancellationToken);
        var access = await db.AccessCredentials.FirstOrDefaultAsync(x => x.SubscriptionId == subscription.Id, cancellationToken);
        if (access is null)
        {
            access = new AccessCredential
            {
                SubscriptionId = subscription.Id,
                ProviderType = provider.Name,
                ProviderAccessId = result.ProviderAccessId,
                ServerId = node.Id,
                AccessUri = result.AccessUri,
                QrCodePath = result.QrCodePath,
                ConfigPath = result.ConfigPath,
                Status = AccessCredentialStatus.Active,
                IssuedAt = clock.UtcNow,
                LastSyncedAt = clock.UtcNow
            };
            db.AccessCredentials.Add(access);
        }
        else
        {
            access.ProviderAccessId = result.ProviderAccessId;
            access.ServerId = node.Id;
            access.AccessUri = result.AccessUri;
            access.QrCodePath = result.QrCodePath;
            access.ConfigPath = result.ConfigPath;
            StatusStateMachine.SetAccessStatus(access, AccessCredentialStatus.Active, clock.UtcNow);
            access.LastSyncedAt = clock.UtcNow;
            access.Revision += 1;
        }

        await db.SaveChangesAsync(cancellationToken);
        subscription.CurrentAccessId = access.Id;
        subscription.CurrentServerId = node.Id;
        subscription.UpdatedAt = clock.UtcNow;
        db.AccessCredentialHistories.Add(new AccessCredentialHistory
        {
            AccessCredentialId = access.Id,
            SubscriptionId = subscription.Id,
            EventType = "OwnVpsProvisioningAccessReady",
            OldValueJson = "{}",
            NewValueJson = JsonSerializer.Serialize(new { access.ProviderAccessId, access.ServerId, access.Status, subscription.EndAt })
        });
        await db.SaveChangesAsync(cancellationToken);
        return $"Подписка: {subscription.Id}\nДействует до: {subscription.EndAt:yyyy-MM-dd HH:mm} UTC\nVPN URI:\n{access.AccessUri}";
    }

    private static async Task<Tariff> EnsureOwnVpsTariffAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var tariff = await db.Tariffs.FirstOrDefaultAsync(x => x.Slug == "own-vps-mvp", cancellationToken);
        if (tariff is not null)
        {
            return tariff;
        }

        tariff = new Tariff
        {
            Name = "VPN на своём VPS (MVP)",
            Slug = "own-vps-mvp",
            Description = "Internal validation tariff for own VPS provisioning MVP.",
            DurationDays = 30,
            Price = 0,
            Currency = "RUB",
            MaxDevices = 3,
            IsActive = false,
            SortOrder = 999,
            Category = "own-vps",
            TariffType = TariffType.Personal
        };
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync(cancellationToken);
        return tariff;
    }

    private static async Task EnsureSupportConversationAsync(ApplicationDbContext db, VpnNode node, ProvisioningRun run, string subject, string error, IClock clock, CancellationToken cancellationToken)
    {
        var telegramUserId = ProvisioningService.ExtractLongTag(node.TagsCsv, "telegram-user-id");
        var conversationCandidates = await db.SupportConversations
            .Where(x => x.UserId == run.RequestedByUserId && x.TelegramUserId == telegramUserId && (x.Status == "open" || x.Status == "pending") && x.Subject == subject)
            .ToListAsync(cancellationToken);
        var conversation = conversationCandidates
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        var isExistingConversation = conversation is not null;
        if (conversation is null)
        {
            conversation = new SupportConversation
            {
                UserId = run.RequestedByUserId,
                TelegramUserId = telegramUserId,
                Channel = "telegram",
                Status = "open",
                Subject = subject
            };
            db.SupportConversations.Add(conversation);
        }

        db.SupportMessages.Add(new SupportMessage
        {
            SupportConversation = conversation,
            UserId = run.RequestedByUserId,
            TelegramUserId = telegramUserId,
            Direction = "internal",
            Text = ProvisioningService.RedactSensitiveText($"Provisioning run {run.Id} for node {node.Name} failed. {error}"),
            IsInternalNote = true,
            AttachmentsJson = "[]"
        });
        if (isExistingConversation)
        {
            conversation.Status = "open";
            conversation.ClosedAt = null;
            conversation.Revision = checked(conversation.Revision + 1);
        }
        conversation.UpdatedAt = clock.UtcNow;
    }

    private static async Task QueueTelegramNotificationAsync(ApplicationDbContext db, VpnNode node, string type, string text, IClock clock, CancellationToken cancellationToken, string? replyMarkupJson = null)
    {
        var telegramUserId = ProvisioningService.ExtractLongTag(node.TagsCsv, "telegram-user-id");
        if (!telegramUserId.HasValue)
        {
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new { text = ProvisioningService.RedactSensitiveText(text, 3000), replyMarkupJson });
        var exists = await db.TelegramBotNotifications.AsNoTracking()
            .AnyAsync(x => x.TelegramUserId == telegramUserId.Value && x.Type == type && x.PayloadJson == payloadJson && x.Status != "failed" && x.Status != "cancelled", cancellationToken);
        if (!exists)
        {
            db.TelegramBotNotifications.Add(new TelegramBotNotification
            {
                TelegramUserId = telegramUserId.Value,
                Type = type,
                PayloadJson = payloadJson,
                Status = "pending",
                NextAttemptAt = clock.UtcNow
            });
        }
    }

    private static void AddStep(ApplicationDbContext db, Guid runId, string stepName, ProvisioningRunStatus status, string output, string error, DateTimeOffset now)
    {
        db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            ProvisioningRunId = runId,
            StepName = stepName,
            Status = status,
            StartedAt = now,
            FinishedAt = now,
            Output = ProvisioningService.RedactSensitiveText(output),
            ErrorText = ProvisioningService.RedactSensitiveText(error)
        });
    }

    private static void AddAudit(ApplicationDbContext db, string action, string entityType, Guid entityId, Guid? actorId, object payload)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorType = actorId.HasValue ? "user" : "system",
            ActorId = actorId?.ToString() ?? "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            BeforeJson = "{}",
            AfterJson = ProvisioningService.RedactSensitiveText(JsonSerializer.Serialize(payload)),
            Ip = string.Empty,
            UserAgent = string.Empty
        });
    }

    private sealed record NodeRollbackSnapshot(
        NodeStatus Status,
        HealthStatus HealthStatus,
        DateTimeOffset? LastHealthCheckAt,
        bool IsAvailableForNewUsers,
        string InstalledVersion,
        string BackupStatus,
        string MonitoringStatus,
        string LoggingStatus,
        int UsedCapacity,
        int Capacity,
        string TagsCsv)
    {
        public static NodeRollbackSnapshot Capture(VpnNode node)
            => new(
                node.Status,
                node.HealthStatus,
                node.LastHealthCheckAt,
                node.IsAvailableForNewUsers,
                node.InstalledVersion,
                node.BackupStatus,
                node.MonitoringStatus,
                node.LoggingStatus,
                node.UsedCapacity,
                node.Capacity,
                node.TagsCsv);

        public void ApplyTo(VpnNode node, DateTimeOffset now)
        {
            node.Status = Status;
            node.HealthStatus = HealthStatus;
            node.LastHealthCheckAt = LastHealthCheckAt;
            node.IsAvailableForNewUsers = IsAvailableForNewUsers;
            node.InstalledVersion = InstalledVersion;
            node.BackupStatus = BackupStatus;
            node.MonitoringStatus = MonitoringStatus;
            node.LoggingStatus = LoggingStatus;
            node.UsedCapacity = UsedCapacity;
            node.Capacity = Capacity;
            node.TagsCsv = TagsCsv;
            node.UpdatedAt = now;
        }
    }

    private static string BuildPostPaymentReplyMarkupJson()
        => "{\"inline_keyboard\":[[{\"text\":\"Мои ключи\",\"callback_data\":\"keys\"},{\"text\":\"Поддержка\",\"callback_data\":\"support\"}],[{\"text\":\"Продлить\",\"callback_data\":\"renew\"}]]}";
}
