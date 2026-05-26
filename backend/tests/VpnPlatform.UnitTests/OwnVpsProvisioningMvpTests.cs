using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        Assert.DoesNotContain("ssh-password-must-not-leak", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SshPrivateKeyPath", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("v1:", json, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("Validation precheck succeeded for vps.example.test", result.SummaryLog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation/mock", result.SummaryLog, StringComparison.OrdinalIgnoreCase);
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
