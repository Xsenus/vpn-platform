using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Provisioning;
using Xunit;

namespace VpnPlatform.UnitTests;

public class OwnVpsProvisioningMvpTests
{
    [Fact]
    public async Task OwnVps_Request_Should_Protect_Credentials_And_Queue_Precheck()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "own-vps@example.test", PasswordHash = "", RolesCsv = "User", ReferralCode = "OWNVPS" });
        await db.SaveChangesAsync();

        var service = new ProvisioningService(db, new TestClock(), new TestSecretProtector());
        var result = await service.CreateOwnVpsRequestAsync(new OwnVpsProvisioningCommand(
            userId,
            100500,
            "vps.example.test",
            2222,
            "root",
            "ssh_key",
            "-----BEGIN PRIVATE KEY-----\nsecret-key-material\n-----END PRIVATE KEY-----",
            "Amsterdam VPS",
            "NL",
            "telegram"));

        Assert.True(result.IsSuccess, result.Error);
        var node = await db.VpnNodes.SingleAsync();
        Assert.Equal("vps.example.test", node.Host);
        Assert.Equal("root", node.SshUser);
        Assert.Equal(2222, node.SshPort);
        Assert.Empty(node.SshPrivateKeyPath);
        Assert.StartsWith("v1:", node.ProtectedSshCredential, StringComparison.Ordinal);
        Assert.True(ProvisioningService.CredentialsConfigured(node));
        Assert.True(ProvisioningService.IsOwnVpsNode(node));
        Assert.True(ProvisioningService.IsValidationNode(node));
        Assert.Equal("ssh_key", ProvisioningService.GetSshAuthMethod(node));

        var run = await db.ProvisioningRuns.SingleAsync();
        Assert.Equal(ProvisioningRunStatus.PrecheckQueued, run.Status);
        Assert.True(run.DryRun);
        Assert.Equal(userId, run.RequestedByUserId);
        Assert.Contains(await db.ProvisioningStepRuns.ToListAsync(), x => x.StepName == "Validate input");
        Assert.DoesNotContain("secret-key-material", JsonSerializer.Serialize(await db.AuditLogs.ToListAsync()), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://bad.example/path", 22, "root", "password", "secret", "Invalid host")]
    [InlineData("vps.example.test", 0, "root", "password", "secret", "Invalid SSH port")]
    [InlineData("vps.example.test", 22, "", "password", "secret", "SSH username is required")]
    [InlineData("vps.example.test", 22, "root", "token", "secret", "Unsupported auth method")]
    [InlineData("vps.example.test", 22, "root", "password", "", "SSH password/private key is required")]
    public async Task OwnVps_Request_Should_Reject_Invalid_Input(string host, int port, string username, string authMethod, string credential, string expectedError)
    {
        await using var db = CreateDbContext();
        var service = new ProvisioningService(db, new TestClock(), new TestSecretProtector());
        var result = await service.CreateOwnVpsRequestAsync(new OwnVpsProvisioningCommand(Guid.NewGuid(), 42, host, port, username, authMethod, credential, null, null, "telegram"));

        Assert.False(result.IsSuccess);
        Assert.Contains(expectedError, result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.VpnNodes.ToListAsync());
        Assert.Empty(await db.ProvisioningRuns.ToListAsync());
    }

    [Fact]
    public async Task Admin_Server_And_Provisioning_Run_Views_Should_Not_Return_Ssh_Credentials()
    {
        await using var db = CreateDbContext();
        var service = new ProvisioningService(db, new TestClock(), new TestSecretProtector());
        var result = await service.CreateOwnVpsRequestAsync(new OwnVpsProvisioningCommand(Guid.NewGuid(), 77, "203.0.113.10", 22, "root", "password", "ssh-password-must-not-leak", "Customer VPS", "EU", "telegram"));
        Assert.True(result.IsSuccess, result.Error);
        var run = await db.ProvisioningRuns.SingleAsync();
        run.AttemptCount = 2;
        run.ProcessingStartedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        run.LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        run.LastError = "token:api-secret-must-not-leak";
        await db.SaveChangesAsync();

        var controller = CreateOperationsController(db, service);
        var servers = await controller.GetServers(CancellationToken.None);
        var runs = await controller.GetProvisioningRuns(CancellationToken.None);
        var details = await controller.GetProvisioningRun(result.Value!.Id, CancellationToken.None);

        var json = JsonSerializer.Serialize(new
        {
            Servers = Assert.IsType<OkObjectResult>(servers).Value,
            Runs = Assert.IsType<OkObjectResult>(runs).Value,
            Details = Assert.IsType<OkObjectResult>(details).Value
        });
        Assert.Contains("SshCredentialConfigured", json, StringComparison.Ordinal);
        Assert.Contains("AuthMethod", json, StringComparison.Ordinal);
        Assert.Contains("ProvisioningMode", json, StringComparison.Ordinal);
        Assert.Contains("Mode", json, StringComparison.Ordinal);
        Assert.Contains("DeployMode", json, StringComparison.Ordinal);
        Assert.Contains("RiskLevel", json, StringComparison.Ordinal);
        Assert.Contains("LiveDeployAllowed", json, StringComparison.Ordinal);
        Assert.Contains("AttemptCount", json, StringComparison.Ordinal);
        Assert.Contains("LeaseExpiresAt", json, StringComparison.Ordinal);
        Assert.Contains("dry-run", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation-deploy", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ssh-password-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api-secret-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SshPrivateKeyPath", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("v1:", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Provisioning_Mode_Descriptor_Should_Separate_DryRun_Validation_Blocked_And_Live()
    {
        var validationNode = new VpnNode { TagsCsv = "validation-mode:true" };
        var blockedLiveNode = new VpnNode { TagsCsv = "validation-mode:false" };
        var explicitLiveNode = new VpnNode { TagsCsv = "validation-mode:false,explicit-live-provisioning:true" };

        var dryRun = ProvisioningService.DescribeProvisioningMode(blockedLiveNode, dryRun: true);
        var validation = ProvisioningService.DescribeProvisioningMode(validationNode, dryRun: false);
        var blocked = ProvisioningService.DescribeProvisioningMode(blockedLiveNode, dryRun: false);
        var live = ProvisioningService.DescribeProvisioningMode(explicitLiveNode, dryRun: false);

        Assert.Equal("dry-run", dryRun.Mode);
        Assert.Equal("safe", dryRun.RiskLevel);
        Assert.False(dryRun.LiveDeployAllowed);
        Assert.Equal("validation-deploy", validation.Mode);
        Assert.Equal("low", validation.RiskLevel);
        Assert.False(validation.LiveDeployAllowed);
        Assert.Equal("live-deploy-blocked", blocked.Mode);
        Assert.Equal("blocked", blocked.RiskLevel);
        Assert.False(blocked.LiveDeployAllowed);
        Assert.Contains("explicit-live-provisioning", blocked.NextAction, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("live-deploy", live.Mode);
        Assert.Equal("high", live.RiskLevel);
        Assert.True(live.LiveDeployAllowed);
    }

    [Fact]
    public async Task QueueAsync_Should_Block_Live_Deploy_Without_Explicit_Tag_But_Allow_DryRun_And_Explicit_Live()
    {
        await using var db = CreateDbContext();
        var blockedNodeId = Guid.NewGuid();
        var liveNodeId = Guid.NewGuid();
        db.VpnNodes.AddRange(
            new VpnNode { Id = blockedNodeId, Name = "blocked-live", Host = "blocked.example.test", SshUser = "root", SshPort = 22, TagsCsv = "validation-mode:false" },
            new VpnNode { Id = liveNodeId, Name = "explicit-live", Host = "live.example.test", SshUser = "root", SshPort = 22, TagsCsv = "validation-mode:false,explicit-live-provisioning:true" });
        await db.SaveChangesAsync();

        var service = new ProvisioningService(db, new TestClock(), new TestSecretProtector());

        var blockedLive = await service.QueueAsync(blockedNodeId, dryRun: false, requestedByUserId: Guid.NewGuid());
        var dryRun = await service.QueueAsync(blockedNodeId, dryRun: true, requestedByUserId: Guid.NewGuid());
        var explicitLive = await service.QueueAsync(liveNodeId, dryRun: false, requestedByUserId: Guid.NewGuid());

        Assert.False(blockedLive.IsSuccess);
        Assert.Equal(ProvisioningService.LiveDeployDisabledError, blockedLive.Error);
        Assert.True(dryRun.IsSuccess, dryRun.Error);
        Assert.True(dryRun.Value!.DryRun);
        Assert.True(explicitLive.IsSuccess, explicitLive.Error);
        Assert.False(explicitLive.Value!.DryRun);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "provisioning.queue" && x.AfterJson.Contains("live-deploy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RedactSensitiveText_Should_Redact_Password_Token_PrivateKey_And_Credential()
    {
        var text = "password=plain token:tokensecret credential=abc api_key:key -----BEGIN OPENSSH PRIVATE KEY-----\nsecret-key\n-----END OPENSSH PRIVATE KEY-----";

        var redacted = ProvisioningService.RedactSensitiveText(text);

        Assert.Contains("***", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("plain", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokensecret", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-key", redacted, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Validation_Mode_Precheck_Should_Return_Deterministic_Mock_Result_Without_Network()
    {
        var executor = new AnsibleProvisioningExecutor(
            Options.Create(new ProvisioningOptions()),
            new ProvisioningSecretMaterializer(new TestSecretProtector()),
            NullLogger<AnsibleProvisioningExecutor>.Instance);
        var node = new VpnNode
        {
            Id = Guid.NewGuid(),
            Name = "validation-node",
            Host = "vps.example.test",
            SshUser = "root",
            SshPort = 22,
            TagsCsv = "validation-mode:true,ssh-auth:ssh_key,credentials:protected",
            ProtectedSshCredential = "validation-placeholder:abc"
        };
        var run = new ProvisioningRun
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            DryRun = true,
            Status = ProvisioningRunStatus.PrecheckQueued
        };

        var result = await executor.ExecuteAsync(node, run, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Steps, x => x.StepName == "Validate input");
        Assert.Contains(result.Steps, x => x.StepName == "Check SSH config");
        Assert.Contains(result.Steps, x => x.StepName == "Check OS");
        Assert.Contains(result.Steps, x => x.StepName == "Check ports");
        Assert.Contains(result.Steps, x => x.StepName == "Check disk");
        Assert.Contains(result.Steps, x => x.StepName == "Check RAM");
        Assert.Contains(result.Steps, x => x.StepName == "Check firewall");
        Assert.Contains(result.Steps, x => x.StepName == "Check Docker");
        Assert.Contains(result.Steps, x => x.StepName == "Check systemd");
        Assert.Contains(result.Steps, x => x.StepName == "Check 3x-ui availability");
        Assert.Contains("Validation precheck succeeded for vps.example.test", result.SummaryLog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation/mock", result.SummaryLog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Precheck report", result.SummaryLog, StringComparison.OrdinalIgnoreCase);

        var reportStep = Assert.Single(result.Steps, x => x.StepName == "Precheck report");
        using var report = JsonDocument.Parse(reportStep.Output);
        var root = report.RootElement;
        Assert.Equal("passed", root.GetProperty("status").GetString());
        var checkKeys = root.GetProperty("checks").EnumerateArray().Select(x => x.GetProperty("key").GetString()).ToArray();
        Assert.Contains("ssh", checkKeys);
        Assert.Contains("os", checkKeys);
        Assert.Contains("ports", checkKeys);
        Assert.Contains("disk", checkKeys);
        Assert.Contains("ram", checkKeys);
        Assert.Contains("firewall", checkKeys);
        Assert.Contains("docker", checkKeys);
        Assert.Contains("systemd", checkKeys);
        Assert.Contains("x3ui", checkKeys);
    }

    [Fact]
    public async Task Admin_Provisioning_Run_Views_Should_Return_Precheck_Report()
    {
        await using var db = CreateDbContext();
        var nodeId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        db.VpnNodes.Add(new VpnNode { Id = nodeId, Name = "Precheck VPS", Host = "precheck.example.test", SshUser = "root", SshPort = 22, TagsCsv = "validation-mode:true" });
        db.ProvisioningRuns.Add(new ProvisioningRun { Id = runId, NodeId = nodeId, Status = ProvisioningRunStatus.ReadyToDeploy, DryRun = true, ExecutionLog = "Precheck report stored." });
        db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            ProvisioningRunId = runId,
            StepName = "Precheck report",
            Status = ProvisioningRunStatus.Succeeded,
            Output = """{"status":"passed","checks":[{"key":"ssh","status":"passed"}]}"""
        });
        await db.SaveChangesAsync();

        var service = new ProvisioningService(db, new TestClock(), new TestSecretProtector());
        var controller = CreateOperationsController(db, service);

        var runs = Assert.IsType<OkObjectResult>(await controller.GetProvisioningRuns(CancellationToken.None)).Value;
        var details = Assert.IsType<OkObjectResult>(await controller.GetProvisioningRun(runId, CancellationToken.None)).Value;
        var json = JsonSerializer.Serialize(new { runs, details });

        Assert.Contains("PrecheckReportPreview", json, StringComparison.Ordinal);
        Assert.Contains("PrecheckReport", json, StringComparison.Ordinal);
        Assert.Contains("status", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("passed", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_Provisioning_Run_List_With_Precheck_Report_Should_Work_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var nodeId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        db.VpnNodes.Add(new VpnNode { Id = nodeId, Name = "SQLite Precheck VPS", Host = "sqlite-precheck.example.test", SshUser = "root", SshPort = 22, TagsCsv = "validation-mode:true" });
        db.ProvisioningRuns.Add(new ProvisioningRun { Id = runId, NodeId = nodeId, Status = ProvisioningRunStatus.ReadyToDeploy, DryRun = true, ExecutionLog = "Precheck report stored." });
        db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            ProvisioningRunId = runId,
            StepName = "Precheck report",
            Status = ProvisioningRunStatus.Succeeded,
            Output = """{"status":"passed","checks":[{"key":"firewall","status":"passed"}]}"""
        });
        await db.SaveChangesAsync();

        var service = new ProvisioningService(db, new TestClock(), new TestSecretProtector());
        var controller = CreateOperationsController(db, service);

        var runs = Assert.IsType<OkObjectResult>(await controller.GetProvisioningRuns(CancellationToken.None)).Value;
        var json = JsonSerializer.Serialize(runs);

        Assert.Contains("PrecheckReportPreview", json, StringComparison.Ordinal);
        Assert.Contains("firewall", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_Deploy_And_Retry_Should_Preserve_Owner_And_Audit_Actor_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var ownerUserId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var queueNode = new VpnNode
        {
            Name = "Customer queue VPS",
            Host = "queue-owner.example.test",
            Provider = "customer-vps",
            SshUser = "root",
            SshPort = 22,
            ProtectedSshCredential = "v1:test",
            TagsCsv = $"validation-mode:true,requested-user-id:{ownerUserId}"
        };
        var deployNode = new VpnNode
        {
            Name = "Customer deploy VPS",
            Host = "deploy-owner.example.test",
            Provider = "customer-vps",
            SshUser = "root",
            SshPort = 22,
            ProtectedSshCredential = "v1:test",
            TagsCsv = $"validation-mode:false,explicit-live-provisioning:true,requested-user-id:{ownerUserId}"
        };
        var retryNode = new VpnNode
        {
            Name = "Customer retry VPS",
            Host = "retry-owner.example.test",
            Provider = "customer-vps",
            SshUser = "root",
            SshPort = 22,
            ProtectedSshCredential = "v1:test",
            TagsCsv = $"validation-mode:true,requested-user-id:{ownerUserId}"
        };
        var readyRun = new ProvisioningRun
        {
            NodeId = deployNode.Id,
            Status = ProvisioningRunStatus.ReadyToDeploy,
            RequestedByUserId = ownerUserId,
            DryRun = true
        };
        var previousQueueRun = new ProvisioningRun
        {
            NodeId = queueNode.Id,
            Status = ProvisioningRunStatus.PrecheckFailed,
            RequestedByUserId = actorUserId,
            DryRun = true
        };
        var failedRun = new ProvisioningRun
        {
            NodeId = retryNode.Id,
            Status = ProvisioningRunStatus.PrecheckFailed,
            RequestedByUserId = actorUserId,
            DryRun = true
        };
        db.AddRange(queueNode, deployNode, retryNode, previousQueueRun, readyRun, failedRun);
        await db.SaveChangesAsync();

        var service = new ProvisioningService(db, new TestClock(), new TestSecretProtector());
        var queue = await service.QueueAsync(queueNode.Id, dryRun: true, actorUserId);
        var deploy = await service.QueueDeployAsync(readyRun.Id, actorUserId);
        var retry = await service.RetryAsync(failedRun.Id, actorUserId);

        Assert.True(queue.IsSuccess, queue.Error);
        Assert.True(deploy.IsSuccess, deploy.Error);
        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(ownerUserId, queue.Value!.RequestedByUserId);
        Assert.Equal(ownerUserId, deploy.Value!.RequestedByUserId);
        Assert.Equal(ownerUserId, retry.Value!.RequestedByUserId);
        Assert.Equal(ProvisioningRunStatus.Retrying, retry.Value.Status);

        var queueAudits = await db.AuditLogs
            .Where(x => x.Action == "provisioning.queue")
            .ToListAsync();
        Assert.Equal(3, queueAudits.Count);
        Assert.All(queueAudits, audit =>
        {
            Assert.Equal(actorUserId.ToString(), audit.ActorId);
            Assert.Contains(ownerUserId.ToString(), audit.AfterJson, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Cancel_Should_Stop_Pending_Provisioning_And_Audit_Action()
    {
        await using var db = CreateDbContext();
        var nodeId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        db.VpnNodes.Add(new VpnNode { Id = nodeId, Name = "Node", Host = "vps.example.test", SshUser = "root", SshPort = 22, Status = NodeStatus.New, TagsCsv = "validation-mode:true" });
        db.ProvisioningRuns.Add(new ProvisioningRun { Id = runId, NodeId = nodeId, Status = ProvisioningRunStatus.PrecheckQueued, DryRun = true });
        await db.SaveChangesAsync();

        var service = new ProvisioningService(db, new TestClock(), new TestSecretProtector());
        var result = await service.CancelAsync(runId, Guid.NewGuid());

        Assert.True(result.IsSuccess, result.Error);
        var run = await db.ProvisioningRuns.SingleAsync(x => x.Id == runId);
        Assert.Equal(ProvisioningRunStatus.Cancelled, run.Status);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "provisioning.cancel" && x.EntityId == runId.ToString());
    }

    [Fact]
    public async Task MarkSupportNeeded_Should_Reopen_Existing_Pending_Conversation()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var node = new VpnNode
        {
            Name = "Own VPS",
            Host = "vps.example.test",
            SshUser = "root",
            SshPort = 22,
            TagsCsv = "telegram-user-id:5050"
        };
        var run = new ProvisioningRun
        {
            NodeId = node.Id,
            RequestedByUserId = userId,
            Status = ProvisioningRunStatus.PrecheckFailed,
            ExecutionLog = "Precheck failed."
        };
        var conversation = new SupportConversation
        {
            UserId = userId,
            TelegramUserId = 5050,
            Channel = "telegram",
            Status = "pending",
            Subject = "Own VPS provisioning needs support",
            Revision = 4
        };
        db.AddRange(node, run, conversation);
        await db.SaveChangesAsync();

        var result = await new ProvisioningService(db, new TestClock(), new TestSecretProtector())
            .MarkSupportNeededAsync(run.Id, Guid.NewGuid());

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(conversation.Id.ToString(), result.Value);
        Assert.Equal(1, await db.SupportConversations.CountAsync());
        Assert.Equal("open", conversation.Status);
        Assert.Equal(5, conversation.Revision);
        Assert.Single(await db.SupportMessages.Where(x => x.SupportConversationId == conversation.Id).ToListAsync());
    }

    [Theory]
    [InlineData(ProvisioningRunStatus.Running)]
    [InlineData(ProvisioningRunStatus.Prechecking)]
    [InlineData(ProvisioningRunStatus.Deploying)]
    public async Task Cancel_Should_Reject_Actively_Executing_Provisioning(ProvisioningRunStatus status)
    {
        await using var db = CreateDbContext();
        var node = new VpnNode { Name = "Node", Host = "vps.example.test", SshUser = "root", SshPort = 22, Status = NodeStatus.Provisioning, TagsCsv = "validation-mode:true" };
        var run = new ProvisioningRun { NodeId = node.Id, Status = status, DryRun = status == ProvisioningRunStatus.Prechecking };
        db.AddRange(node, run);
        await db.SaveChangesAsync();

        var result = await new ProvisioningService(db, new TestClock(), new TestSecretProtector()).CancelAsync(run.Id, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be cancelled safely", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(status, (await db.ProvisioningRuns.SingleAsync()).Status);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData(ProvisioningRunStatus.Succeeded)]
    [InlineData(ProvisioningRunStatus.Deployed)]
    [InlineData(ProvisioningRunStatus.Prechecking)]
    [InlineData(ProvisioningRunStatus.Deploying)]
    public async Task Retry_Should_Reject_Successful_Or_Active_Provisioning(ProvisioningRunStatus status)
    {
        await using var db = CreateDbContext();
        var node = new VpnNode { Name = "Node", Host = "vps.example.test", SshUser = "root", SshPort = 22, TagsCsv = "validation-mode:true" };
        var run = new ProvisioningRun { NodeId = node.Id, Status = status, DryRun = true };
        db.AddRange(node, run);
        await db.SaveChangesAsync();

        var result = await new ProvisioningService(db, new TestClock(), new TestSecretProtector()).RetryAsync(run.Id, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains("Only failed or cancelled", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.ProvisioningRuns.CountAsync());
    }

    private static AdminOperationsController CreateOperationsController(ApplicationDbContext db, ProvisioningService provisioningService)
    {
        var controller = new AdminOperationsController(
            db,
            provisioningService,
            paymentOrchestrator: null!,
            paymentProviderAccounts: null!,
            vpnAccessLifecycleService: null,
            secretProtector: new TestSecretProtector());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
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
        public string Protect(string plaintext) => "v1:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        public string Unprotect(string protectedValue) => Encoding.UTF8.GetString(Convert.FromBase64String(protectedValue[3..]));
        public string Mask(string? value, int visibleTail = 4)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= visibleTail ? "***" : $"***{value[^visibleTail..]}";
        }
    }
}
