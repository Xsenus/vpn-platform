using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Security;
using VpnPlatform.Infrastructure.Services;
using VpnPlatform.Infrastructure.Vpn;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminServerManagementTests
{
    [Fact]
    public void VpnNode_Revision_Should_Be_A_Concurrency_Token()
    {
        using var db = CreateDbContext();
        var entityType = db.Model.FindEntityType(typeof(VpnNode));
        var revision = entityType?.FindProperty("Revision");

        Assert.NotNull(revision);
        Assert.True(revision.IsConcurrencyToken);
    }

    [Fact]
    public async Task UpdateServer_Should_Reject_Normalized_NoOp_Without_Revision_Timestamp_Or_Audit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var node = NewNode("no-op-server");
        node.TagsCsv = "source:admin,owner:admin,ssh-auth:ssh_key,credentials:missing,validation-mode:true,autodeploy-after-precheck:false";
        node.UpdatedAt = new DateTimeOffset(2026, 8, 15, 8, 0, 0, TimeSpan.Zero);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var originalUpdatedAt = node.UpdatedAt;
        var request = UpdateRequest(node) with
        {
            SshAuthMethod = "ssh_key",
            ValidationMode = true,
            OwnerType = "admin"
        };

        var result = await CreateController(db).UpdateServer(node.Id, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        db.ChangeTracker.Clear();
        var persisted = await db.VpnNodes.SingleAsync(x => x.Id == node.Id);
        Assert.Equal(0, persisted.Revision);
        Assert.Equal(originalUpdatedAt, persisted.UpdatedAt);
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action is "server.update" or "server.secret.rotate");
    }

    [Theory]
    [InlineData("name", 201)]
    [InlineData("provider", 121)]
    [InlineData("region", 121)]
    [InlineData("tags", 2001)]
    [InlineData("panelUsername", 201)]
    public async Task Server_Write_Should_Reject_Overlong_Fields(string field, int length)
    {
        await using var db = CreateDbContext();
        var node = NewNode("bounded-server");
        var request = UpdateRequest(node) with
        {
            Name = field == "name" ? new string('n', length) : node.Name,
            Provider = field == "provider" ? new string('p', length) : node.Provider,
            Region = field == "region" ? new string('r', length) : node.Region,
            TagsCsv = field == "tags" ? new string('t', length) : node.TagsCsv,
            PanelUsername = field == "panelUsername" ? new string('u', length) : node.PanelUsername
        };

        var result = await CreateController(db).AddServer(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.VpnNodes.ToListAsync());
    }

    [Fact]
    public async Task GetServers_Should_Bound_Health_Diagnostics_Before_Materialization()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var node = NewNode("bounded-diagnostics");
        db.VpnNodes.Add(node);
        db.NodeHealthChecks.AddRange(Enumerable.Range(0, 25).Select(index => new NodeHealthCheck
        {
            NodeId = node.Id,
            Status = HealthStatus.Healthy,
            CheckedAt = DateTimeOffset.UtcNow.AddMinutes(-index),
            LatencyMs = index,
            MetadataJson = "{}"
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        Assert.IsType<OkObjectResult>(await CreateController(db).GetServers(CancellationToken.None));

        Assert.Contains(interceptor.Commands, command =>
            command.Contains("NodeHealthChecks", StringComparison.OrdinalIgnoreCase)
            && command.Contains("ROW_NUMBER", StringComparison.OrdinalIgnoreCase)
            && command.Contains("WHERE \"_LatestRank\" = 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Provisioning_Reads_Should_Bound_Runs_And_Steps_Before_Materialization()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var run = new ProvisioningRun
        {
            NodeId = Guid.NewGuid(),
            Status = ProvisioningRunStatus.Failed,
            DryRun = true
        };
        db.ProvisioningRuns.Add(run);
        db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            ProvisioningRunId = run.Id,
            StepName = "bounded-step",
            Status = ProvisioningRunStatus.Failed
        });
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var controller = CreateController(db);
        Assert.IsType<OkObjectResult>(await controller.GetProvisioningRuns(CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.GetProvisioningRun(run.Id, CancellationToken.None));

        Assert.Contains(interceptor.Commands, command =>
            command.Contains("ProvisioningRuns", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 200", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("ProvisioningStepRuns", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 500", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Empty_Provisioning_List_Should_Return_Ok_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var result = Assert.IsType<OkObjectResult>(
            await CreateController(db).GetProvisioningRuns(CancellationToken.None));

        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<object>>(result.Value));
    }

    [Fact]
    public async Task Provisioning_List_Should_Load_Only_The_Latest_Precheck_Report_Per_Run()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var run = new ProvisioningRun
        {
            NodeId = Guid.NewGuid(),
            Status = ProvisioningRunStatus.ReadyToDeploy,
            DryRun = true
        };
        var baseline = DateTimeOffset.UtcNow.AddDays(-2);
        db.ProvisioningRuns.Add(run);
        db.ProvisioningStepRuns.AddRange(Enumerable.Range(0, 1000).Select(index => new ProvisioningStepRun
        {
            ProvisioningRunId = run.Id,
            StepName = "Precheck report",
            Status = ProvisioningRunStatus.ReadyToDeploy,
            Output = $"stale-report-{index}",
            CreatedAt = baseline.AddSeconds(index)
        }));
        db.ProvisioningStepRuns.Add(new ProvisioningStepRun
        {
            ProvisioningRunId = run.Id,
            StepName = "Precheck report",
            Status = ProvisioningRunStatus.ReadyToDeploy,
            Output = "latest-precheck-report",
            CreatedAt = baseline.AddDays(1)
        });
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var result = Assert.IsType<OkObjectResult>(
            await CreateController(db).GetProvisioningRuns(CancellationToken.None));
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);

        Assert.Contains("latest-precheck-report", json, StringComparison.Ordinal);
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("ProvisioningStepRuns", StringComparison.OrdinalIgnoreCase)
            && command.Contains("ROW_NUMBER", StringComparison.OrdinalIgnoreCase)
            && command.Contains("WHERE \"_LatestRank\" = 1", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Server_Write_Should_Persist_The_Normalized_Host_On_Sqlite(bool update)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var node = NewNode($"normalized-host-{update}");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var request = UpdateRequest(node) with { Host = "normalized.example.test/", IpAddress = string.Empty };
        var controller = CreateController(db);

        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        db.ChangeTracker.Clear();
        var persisted = update
            ? await db.VpnNodes.SingleAsync(x => x.Id == node.Id)
            : await db.VpnNodes.SingleAsync(x => x.Id != node.Id);
        Assert.Equal("normalized.example.test", persisted.Host);
        Assert.Null(ProvisioningService.ValidateProvisioningTarget(persisted));
    }

    [Theory]
    [InlineData(false, "ip-address")]
    [InlineData(false, "ssh-user")]
    [InlineData(true, "ip-address")]
    [InlineData(true, "ssh-user")]
    public async Task Server_Write_Should_Reject_Provisioning_Inventory_Injection_On_Sqlite(bool update, string field)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var node = NewNode($"inventory-guard-{update}-{field}");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var request = UpdateRequest(node) with
        {
            Name = $"mutated-{node.Name}",
            IpAddress = field == "ip-address" ? "10.0.0.1\ninjected ansible_connection=local" : node.IpAddress,
            SshUser = field == "ssh-user" ? "root ansible_connection=local" : "root"
        };
        var controller = CreateController(db);

        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        var invalid = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(field == "ip-address" ? "IP address" : "SSH username", invalid.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        db.ChangeTracker.Clear();
        var persisted = await db.VpnNodes.SingleAsync();
        Assert.Equal(node.Id, persisted.Id);
        Assert.Equal(node.Name, persisted.Name);
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action is "server.create" or "server.update");
    }

    [Theory]
    [InlineData(false, "pem")]
    [InlineData(false, "protected")]
    [InlineData(false, "placeholder")]
    [InlineData(false, "relative")]
    [InlineData(false, "quoted")]
    [InlineData(false, "whitespace")]
    [InlineData(true, "pem")]
    [InlineData(true, "protected")]
    [InlineData(true, "placeholder")]
    [InlineData(true, "relative")]
    [InlineData(true, "quoted")]
    [InlineData(true, "whitespace")]
    public async Task Server_Write_Should_Reject_Secret_Material_In_Legacy_Ssh_Key_Path_On_Sqlite(bool update, string pathKind)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var node = NewNode($"legacy-path-guard-{update}-{pathKind}");
        node.ProtectedSshCredential = "v1:existing-ssh-secret";
        node.SshCredentialRef = "secretref:ssh:existing";
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var unsafePath = pathKind switch
        {
            "pem" => "-----BEGIN OPENSSH PRIVATE KEY-----\nraw-secret\n-----END OPENSSH PRIVATE KEY-----",
            "protected" => "v1:legacy-protected-value",
            "placeholder" => "validation-placeholder:legacy-value",
            "relative" => "secrets/id_ed25519",
            "quoted" => "/run/secrets/id_ed25519\" --check",
            _ => "/run/secrets/operator key"
        };
        var request = UpdateRequest(node) with
        {
            Name = $"mutated-{node.Name}",
            SshPrivateKeyPath = unsafePath,
            SshCredential = null
        };
        var controller = CreateController(db);

        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        var invalid = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("SSH private key path", invalid.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        db.ChangeTracker.Clear();
        var persisted = await db.VpnNodes.SingleAsync();
        Assert.Equal(node.Id, persisted.Id);
        Assert.Equal(node.Name, persisted.Name);
        Assert.Equal("v1:existing-ssh-secret", persisted.ProtectedSshCredential);
        Assert.Equal("secretref:ssh:existing", persisted.SshCredentialRef);
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action is "server.create" or "server.update" or "server.secret.rotate");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Server_Write_Should_Accept_Legacy_Ssh_Key_Filesystem_Path_On_Sqlite(bool update)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var node = NewNode($"legacy-path-valid-{update}");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var request = UpdateRequest(node) with { SshPrivateKeyPath = "/run/secrets/vpn-platform/id_ed25519" };
        var controller = CreateController(db);

        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        db.ChangeTracker.Clear();
        var persisted = update
            ? await db.VpnNodes.SingleAsync(x => x.Id == node.Id)
            : await db.VpnNodes.SingleAsync(x => x.Id != node.Id);
        Assert.Equal(request.SshPrivateKeyPath, persisted.SshPrivateKeyPath);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == (update ? "server.update" : "server.create"));
    }

    [Theory]
    [InlineData(false, "ssh")]
    [InlineData(false, "panel")]
    [InlineData(true, "ssh")]
    [InlineData(true, "panel")]
    public async Task Server_Secret_Write_Should_Fail_Closed_When_Protector_Is_Unavailable_On_Sqlite(bool update, string secretKind)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var node = NewNode($"secret-guard-{update}-{secretKind}");
        node.ProtectedSshCredential = "v1:existing-ssh-secret";
        node.SshCredentialRef = "secretref:ssh:existing";
        node.ProtectedPanelPassword = "v1:existing-panel-secret";
        node.PanelSecretRef = "secretref:panel:existing";
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var request = UpdateRequest(node) with
        {
            Name = $"mutated-{node.Name}",
            SshAuthMethod = "password",
            SshCredential = secretKind == "ssh" ? "new-ssh-secret" : null,
            PanelPassword = secretKind == "panel" ? "new-panel-secret" : null
        };
        var controller = CreateController(db, includeSecretProtector: false);

        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        db.ChangeTracker.Clear();
        var persisted = await db.VpnNodes.SingleAsync();
        Assert.Equal(node.Id, persisted.Id);
        Assert.Equal(node.Name, persisted.Name);
        Assert.Equal("v1:existing-ssh-secret", persisted.ProtectedSshCredential);
        Assert.Equal("secretref:ssh:existing", persisted.SshCredentialRef);
        Assert.Equal("v1:existing-panel-secret", persisted.ProtectedPanelPassword);
        Assert.Equal("secretref:panel:existing", persisted.PanelSecretRef);
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action is "server.create" or "server.update" or "server.secret.rotate");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Server_Metadata_Write_Should_Not_Require_Secret_Protector_On_Sqlite(bool update)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var node = NewNode($"metadata-guard-{update}");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var request = UpdateRequest(node) with { Name = $"updated-{node.Name}" };
        var controller = CreateController(db, includeSecretProtector: false);

        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        db.ChangeTracker.Clear();
        var persisted = update
            ? await db.VpnNodes.SingleAsync(x => x.Id == node.Id)
            : await db.VpnNodes.SingleAsync(x => x.Id != node.Id);
        Assert.Equal(request.Name, persisted.Name);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == (update ? "server.update" : "server.create"));
    }

    [Theory]
    [InlineData(false, "name")]
    [InlineData(false, "capacity")]
    [InlineData(false, "priority")]
    [InlineData(false, "public-port-low")]
    [InlineData(false, "public-port-high")]
    [InlineData(false, "panel-inbound")]
    [InlineData(false, "node-group")]
    [InlineData(false, "public-hostname")]
    [InlineData(false, "protocols")]
    [InlineData(true, "name")]
    [InlineData(true, "capacity")]
    [InlineData(true, "priority")]
    [InlineData(true, "public-port-low")]
    [InlineData(true, "public-port-high")]
    [InlineData(true, "panel-inbound")]
    [InlineData(true, "node-group")]
    [InlineData(true, "public-hostname")]
    [InlineData(true, "protocols")]
    public async Task Server_Write_Should_Reject_Invalid_Semantic_Payload_On_Sqlite(bool update, string field)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var node = NewNode($"semantic-{field}");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var request = UpdateRequest(node) with
        {
            Name = field == "name" ? "  " : node.Name,
            Capacity = field == "capacity" ? 0 : node.Capacity,
            Priority = field == "priority" ? 0 : node.Priority,
            PublicPort = field switch
            {
                "public-port-low" => 0,
                "public-port-high" => 65536,
                _ => node.PublicPort
            },
            PanelInboundId = field == "panel-inbound" ? 0 : node.PanelInboundId,
            NodeGroupId = field == "node-group" ? Guid.NewGuid() : node.NodeGroupId,
            PublicHostname = field == "public-hostname" ? "vpn.example.test/path?token=leak" : node.PublicHostname,
            SupportedProtocolsCsv = field == "protocols" ? "notvless,wireguard" : node.SupportedProtocolsCsv
        };
        var controller = CreateController(db);

        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        db.ChangeTracker.Clear();
        var persisted = await db.VpnNodes.SingleAsync();
        Assert.Equal(node.Id, persisted.Id);
        Assert.Equal(node.Name, persisted.Name);
        Assert.Equal(node.Capacity, persisted.Capacity);
        Assert.Equal(node.Priority, persisted.Priority);
        Assert.Equal(node.PublicPort, persisted.PublicPort);
        Assert.Equal(node.PanelInboundId, persisted.PanelInboundId);
        Assert.Equal(node.NodeGroupId, persisted.NodeGroupId);
        Assert.Equal(node.PublicHostname, persisted.PublicHostname);
        Assert.Equal(node.SupportedProtocolsCsv, persisted.SupportedProtocolsCsv);
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), x => x.Action == (update ? "server.update" : "server.create"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Server_Write_Should_Accept_Existing_Node_Group_And_Normalize_Payload_On_Sqlite(bool update)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var group = new NodeGroup { Name = $"semantic-group-{update}", Region = "eu" };
        var node = NewNode($"semantic-valid-{update}");
        db.NodeGroups.Add(group);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var request = UpdateRequest(node) with
        {
            Name = $"  {node.Name}  ",
            Provider = "  x3ui  ",
            SupportedProtocolsCsv = " TROJAN, VLESS, trojan ",
            NodeGroupId = group.Id
        };
        var controller = CreateController(db);

        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        var saved = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(saved.Value);
        db.ChangeTracker.Clear();
        var persisted = update
            ? await db.VpnNodes.SingleAsync(x => x.Id == node.Id)
            : await db.VpnNodes.SingleAsync(x => x.Id != node.Id);
        Assert.Equal(node.Name, persisted.Name);
        Assert.Equal("x3ui", persisted.Provider);
        Assert.Equal("vless,trojan", persisted.SupportedProtocolsCsv);
        Assert.Equal(group.Id, persisted.NodeGroupId);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == (update ? "server.update" : "server.create"));
    }

    [Fact]
    public async Task DisableServer_Should_Close_Server_For_New_Users_And_Write_Audit()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);
        var node = NewNode("disable-node");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var result = await controller.DisableServer(node.Id, new ServerStateActionHttpRequest(node.Revision), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(NodeStatus.Disabled, node.Status);
        Assert.False(node.IsAvailableForNewUsers);
        Assert.Equal(1, node.Revision);
        Assert.Contains(db.AuditLogs, x => x.Action == "server.disable" && x.EntityId == node.Id.ToString());
    }

    [Fact]
    public async Task DeleteServer_Should_Remove_Unused_Server_And_Write_Audit()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);
        var node = NewNode("unused-node");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var result = await controller.DeleteServer(node.Id, node.Revision, CancellationToken.None);

        var response = Assert.IsType<DeleteServerHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.True(response.Deleted);
        Assert.False(response.Archived);
        Assert.Equal(0, response.LinkedHealthChecks);
        Assert.Equal(0, response.LinkedMigrationJobs);
        Assert.False(await db.VpnNodes.AnyAsync(x => x.Id == node.Id));
        Assert.Contains(db.AuditLogs, x => x.Action == "server.delete" && x.EntityId == node.Id.ToString());
    }

    [Fact]
    public async Task DeleteServer_Should_Archive_Server_When_Operational_Data_Is_Linked()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);
        var node = NewNode("linked-node");
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TariffId = Guid.NewGuid(),
            CurrentServerId = node.Id,
            Status = SubscriptionStatus.Expired,
            StartAt = DateTimeOffset.UtcNow,
            EndAt = DateTimeOffset.UtcNow.AddDays(-30)
        });
        db.AccessCredentials.Add(new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "client-1",
            AccessUri = "vless://client@example.test",
            Status = AccessCredentialStatus.Revoked
        });
        db.ProvisioningRuns.Add(new ProvisioningRun
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            Status = ProvisioningRunStatus.Succeeded,
            DryRun = true
        });
        await db.SaveChangesAsync();

        var result = await controller.DeleteServer(node.Id, node.Revision, CancellationToken.None);
        var archivedHealth = await controller.CheckServerHealth(node.Id, CancellationToken.None);

        var response = Assert.IsType<DeleteServerHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        var archivedHealthError = Assert.IsType<BadRequestObjectResult>(archivedHealth);
        Assert.Contains("read-only", archivedHealthError.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(response.Deleted);
        Assert.True(response.Archived);
        Assert.Equal(1, response.LinkedSubscriptions);
        Assert.Equal(1, response.LinkedAccesses);
        Assert.Equal(1, response.LinkedProvisioningRuns);
        Assert.Equal(0, response.LinkedHealthChecks);
        Assert.Equal(0, response.LinkedMigrationJobs);
        Assert.Equal(NodeStatus.Archived, node.Status);
        Assert.False(node.IsAvailableForNewUsers);
        Assert.True(await db.VpnNodes.AnyAsync(x => x.Id == node.Id));
        Assert.Contains(db.AuditLogs, x => x.Action == "server.archive" && x.EntityId == node.Id.ToString());
    }

    [Fact]
    public async Task DeleteServer_Should_Reject_Server_With_Active_Provisioning_Run_Without_Mutation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var controller = CreateController(db);
        var node = NewNode("active-provisioning-node");
        node.Revision = 3;
        node.UpdatedAt = new DateTimeOffset(2026, 8, 15, 13, 0, 0, TimeSpan.Zero);
        var run = new ProvisioningRun
        {
            NodeId = node.Id,
            Status = ProvisioningRunStatus.PrecheckQueued,
            DryRun = true,
            Revision = 2
        };
        db.AddRange(node, run);
        await db.SaveChangesAsync();

        var result = await controller.DeleteServer(node.Id, node.Revision, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("active provisioning", conflict.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(NodeStatus.Ready, node.Status);
        Assert.Equal(3, node.Revision);
        Assert.Equal(new DateTimeOffset(2026, 8, 15, 13, 0, 0, TimeSpan.Zero), node.UpdatedAt);
        Assert.Equal(ProvisioningRunStatus.PrecheckQueued, run.Status);
        Assert.Equal(2, run.Revision);
        Assert.Empty(db.AuditLogs);
    }

    [Theory]
    [InlineData("reserved-capacity")]
    [InlineData("active-subscription")]
    [InlineData("active-access")]
    [InlineData("active-migration")]
    public async Task DeleteServer_Should_Reject_Active_Workload_Without_Mutation(string dependency)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var controller = CreateController(db);
        var updatedAt = new DateTimeOffset(2026, 8, 15, 16, 0, 0, TimeSpan.Zero);
        var node = NewNode($"active-workload-{dependency}");
        node.Revision = 4;
        node.UpdatedAt = updatedAt;
        if (dependency == "reserved-capacity") node.UsedCapacity = 1;
        db.VpnNodes.Add(node);

        if (dependency is "active-subscription" or "active-access")
        {
            var user = new User
            {
                Email = $"{dependency}@example.test",
                DisplayName = dependency,
                ReferralCode = $"REF-{dependency}"
            };
            var tariff = new Tariff
            {
                Name = dependency,
                Slug = dependency,
                DurationDays = 30,
                Price = 100
            };
            var subscription = new Subscription
            {
                UserId = user.Id,
                TariffId = tariff.Id,
                Status = dependency == "active-subscription" ? SubscriptionStatus.Active : SubscriptionStatus.Expired,
                CurrentServerId = dependency == "active-subscription" ? node.Id : null,
                StartAt = updatedAt.AddDays(-30),
                EndAt = updatedAt.AddDays(30)
            };
            db.Users.Add(user);
            db.Tariffs.Add(tariff);
            db.Subscriptions.Add(subscription);
            if (dependency == "active-access")
            {
                db.AccessCredentials.Add(new AccessCredential
                {
                    SubscriptionId = subscription.Id,
                    ServerId = node.Id,
                    ProviderType = "x3ui",
                    ProviderAccessId = "active-client",
                    AccessUri = "vless://active@example.test",
                    Status = AccessCredentialStatus.Active,
                    IssuedAt = updatedAt.AddDays(-1)
                });
            }
        }
        else if (dependency == "active-migration")
        {
            db.MigrationJobs.Add(new MigrationJob
            {
                SourceNodeId = node.Id,
                TargetNodeId = Guid.NewGuid(),
                Status = MigrationJobStatus.Running,
                Type = "single-subscription",
                RequestedAt = updatedAt
            });
        }
        await db.SaveChangesAsync();

        var result = await controller.DeleteServer(node.Id, node.Revision, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        db.ChangeTracker.Clear();
        var persisted = await db.VpnNodes.SingleAsync(x => x.Id == node.Id);
        Assert.Equal(NodeStatus.Ready, persisted.Status);
        Assert.Equal(4, persisted.Revision);
        Assert.Equal(updatedAt, persisted.UpdatedAt);
        Assert.Empty(db.AuditLogs);
    }

    [Theory]
    [InlineData("health-check", 1, 0)]
    [InlineData("migration-source", 0, 1)]
    [InlineData("migration-target", 0, 1)]
    public async Task DeleteServer_Should_Archive_Server_When_Historical_Operations_Are_Linked(
        string dependency,
        int expectedHealthChecks,
        int expectedMigrationJobs)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var controller = CreateController(db);
        var node = NewNode($"historical-{dependency}");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        if (dependency == "health-check")
        {
            db.NodeHealthChecks.Add(new NodeHealthCheck
            {
                NodeId = node.Id,
                CheckedAt = DateTimeOffset.UtcNow,
                Status = HealthStatus.Healthy
            });
        }
        else
        {
            db.MigrationJobs.Add(new MigrationJob
            {
                SourceNodeId = dependency == "migration-source" ? node.Id : Guid.NewGuid(),
                TargetNodeId = dependency == "migration-target" ? node.Id : null,
                Status = MigrationJobStatus.Completed,
                Type = "historical-test"
            });
        }
        await db.SaveChangesAsync();

        var result = await controller.DeleteServer(node.Id, node.Revision, CancellationToken.None);
        var archivedHealth = await controller.CheckServerHealth(node.Id, CancellationToken.None);

        var response = Assert.IsType<DeleteServerHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        var archivedHealthError = Assert.IsType<BadRequestObjectResult>(archivedHealth);
        Assert.Contains("read-only", archivedHealthError.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(response.Deleted);
        Assert.True(response.Archived);
        Assert.Equal(expectedHealthChecks, response.LinkedHealthChecks);
        Assert.Equal(expectedMigrationJobs, response.LinkedMigrationJobs);
        Assert.Equal(NodeStatus.Archived, node.Status);
        Assert.True(await db.VpnNodes.AnyAsync(x => x.Id == node.Id));
        Assert.Equal(expectedHealthChecks, await db.NodeHealthChecks.CountAsync(x => x.NodeId == node.Id));
        Assert.Equal(expectedMigrationJobs, await db.MigrationJobs.CountAsync(x => x.SourceNodeId == node.Id || x.TargetNodeId == node.Id));
    }

    [Fact]
    public async Task DeleteServer_Should_Reject_Already_Archived_Server_Without_Mutation()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);
        var node = NewNode("already-archived-node");
        node.Status = NodeStatus.Archived;
        node.IsAvailableForNewUsers = false;
        node.Revision = 4;
        node.UpdatedAt = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
        db.VpnNodes.Add(node);
        db.NodeHealthChecks.Add(new NodeHealthCheck
        {
            NodeId = node.Id,
            CheckedAt = node.UpdatedAt.AddDays(-1),
            Status = HealthStatus.Healthy
        });
        await db.SaveChangesAsync();

        var result = await controller.DeleteServer(node.Id, node.Revision, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(NodeStatus.Archived, node.Status);
        Assert.False(node.IsAvailableForNewUsers);
        Assert.Equal(4, node.Revision);
        Assert.Equal(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero), node.UpdatedAt);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task CheckServerHealth_Should_Reject_Archived_Server_Without_Local_Churn()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db, new TestVpnProvider(HealthStatus.Healthy));
        var node = NewNode("archived-health-node");
        node.Status = NodeStatus.Archived;
        node.IsAvailableForNewUsers = false;
        node.HealthStatus = HealthStatus.Healthy;
        node.Revision = 4;
        node.LastHealthCheckAt = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        node.UpdatedAt = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
        db.VpnNodes.Add(node);
        db.NodeHealthChecks.Add(new NodeHealthCheck
        {
            NodeId = node.Id,
            CheckedAt = node.LastHealthCheckAt.Value,
            Status = HealthStatus.Healthy
        });
        await db.SaveChangesAsync();

        var result = await controller.CheckServerHealth(node.Id, CancellationToken.None);

        var error = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("read-only", error.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(NodeStatus.Archived, node.Status);
        Assert.Equal(HealthStatus.Healthy, node.HealthStatus);
        Assert.Equal(4, node.Revision);
        Assert.Equal(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero), node.LastHealthCheckAt);
        Assert.Equal(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero), node.UpdatedAt);
        Assert.Equal(1, await db.NodeHealthChecks.CountAsync(x => x.NodeId == node.Id));
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task CheckServerHealth_Should_Save_Healthy_Result_And_Update_Node()
    {
        await using var db = CreateDbContext();
        var provider = new TestVpnProvider(HealthStatus.Healthy);
        var controller = CreateController(db, provider);
        var node = NewNode("healthy-node");
        node.Provider = provider.Name;
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var result = await controller.CheckServerHealth(node.Id, CancellationToken.None);

        var check = Assert.IsType<NodeHealthCheckDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Healthy", check.Status);
        Assert.Equal(node.Id, check.NodeId);
        Assert.Equal(HealthStatus.Healthy, node.HealthStatus);
        Assert.NotNull(node.LastHealthCheckAt);
        Assert.True(await db.NodeHealthChecks.AnyAsync(x => x.NodeId == node.Id && x.Status == HealthStatus.Healthy));
        Assert.Contains(db.AuditLogs, x => x.Action == "server.health-check" && x.EntityId == node.Id.ToString());
    }

    [Fact]
    public async Task CheckServerHealth_Should_Report_Maintenance_As_Degraded_With_Clear_Reason()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db, new TestVpnProvider(HealthStatus.Healthy));
        var node = NewNode("maintenance-node");
        node.Status = NodeStatus.Maintenance;
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var result = await controller.CheckServerHealth(node.Id, CancellationToken.None);

        var check = Assert.IsType<NodeHealthCheckDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Degraded", check.Status);
        Assert.Contains("Сервер в обслуживании", check.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HealthStatus.Degraded, node.HealthStatus);
    }

    [Fact]
    public async Task CheckServerHealth_Should_Save_Unhealthy_Result_When_Provider_Fails()
    {
        await using var db = CreateDbContext();
        var provider = new TestVpnProvider(HealthStatus.Healthy, new InvalidOperationException("panel password secret failed"));
        var controller = CreateController(db, provider);
        var node = NewNode("failed-node");
        node.Provider = provider.Name;
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var result = await controller.CheckServerHealth(node.Id, CancellationToken.None);

        var check = Assert.IsType<NodeHealthCheckDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Unhealthy", check.Status);
        Assert.Contains("InvalidOperationException", check.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("panel password", check.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", check.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HealthStatus.Unhealthy, node.HealthStatus);
    }

    [Fact]
    public async Task CheckServerHealth_Should_Propagate_Caller_Cancellation_Without_State_Change()
    {
        await using var db = CreateDbContext();
        var healthStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHealth = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new TestVpnProvider(HealthStatus.Healthy, healthStarted: healthStarted, releaseHealth: releaseHealth);
        var controller = CreateController(db, provider);
        var node = NewNode("cancelled-health-node");
        node.Provider = provider.Name;
        node.LastHealthCheckAt = null;
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();

        var healthTask = controller.CheckServerHealth(node.Id, cancellation.Token);
        await healthStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => healthTask);
        Assert.Equal(HealthStatus.Healthy, node.HealthStatus);
        Assert.Null(node.LastHealthCheckAt);
        Assert.Empty(db.NodeHealthChecks);
        Assert.DoesNotContain(db.AuditLogs, x => x.Action == "server.health-check" && x.EntityId == node.Id.ToString());
    }

    [Fact]
    public async Task UpdateServer_Should_Reject_Capacity_Below_Used_Slots()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);
        var node = NewNode("used-capacity-node");
        node.Capacity = 10;
        node.UsedCapacity = 4;
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var result = await controller.UpdateServer(node.Id, UpdateRequest(node, capacity: 3), CancellationToken.None);

        var error = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("used capacity", error.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(10, node.Capacity);
        Assert.DoesNotContain(db.AuditLogs, x => x.Action == "server.update" && x.EntityId == node.Id.ToString());
    }

    [Theory]
    [InlineData(false, "https://operator:secret@panel.example.test")]
    [InlineData(true, "https://operator:secret@panel.example.test")]
    [InlineData(false, "ftp://panel.example.test")]
    [InlineData(true, "ftp://panel.example.test")]
    public async Task Server_Write_Should_Reject_Unsafe_Panel_Base_Url(bool update, string panelBaseUrl)
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);
        var node = NewNode("unsafe-panel-url-node");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var request = UpdateRequest(node, panelBaseUrl: panelBaseUrl);
        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        var error = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Panel base URL", error.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, node.PanelBaseUrl);
        Assert.DoesNotContain(db.AuditLogs, x => x.Action is "server.create" or "server.update");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Server_Write_Should_Accept_Safe_Panel_Base_Url(bool update)
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);
        var node = NewNode("safe-panel-url-node");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        const string panelBaseUrl = "https://panel.example.test:2053/base/";

        var request = UpdateRequest(node, panelBaseUrl: panelBaseUrl);
        var result = update
            ? await controller.UpdateServer(node.Id, request, CancellationToken.None)
            : await controller.AddServer(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Contains(await db.VpnNodes.ToListAsync(), x => x.PanelBaseUrl == panelBaseUrl);
        Assert.Contains(db.AuditLogs, x => x.Action == (update ? "server.update" : "server.create"));
    }

    [Fact]
    public async Task UpdateServer_Should_Wait_For_Concurrent_Health_Check()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-node-health-{Guid.NewGuid():N}.db");
        var healthStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHealth = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            Guid nodeId;
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                var node = NewNode("concurrent-health-node");
                node.Provider = "x3ui";
                seed.VpnNodes.Add(node);
                await seed.SaveChangesAsync();
                nodeId = node.Id;
            }

            var updateCompletedBeforeHealth = false;
            var provider = new TestVpnProvider(HealthStatus.Healthy, healthStarted: healthStarted, releaseHealth: releaseHealth);
            await using (var healthDb = new ApplicationDbContext(options))
            await using (var updateDb = new ApplicationDbContext(options))
            {
                var healthController = CreateController(healthDb, provider);
                var updateController = CreateController(updateDb);

                var healthTask = healthController.CheckServerHealth(nodeId, CancellationToken.None);
                await healthStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                var observed = await updateDb.VpnNodes.AsNoTracking().SingleAsync(x => x.Id == nodeId);
                var updateTask = updateController.UpdateServer(nodeId, UpdateRequest(observed, name: "updated-after-health"), CancellationToken.None);

                await Task.Delay(100);
                updateCompletedBeforeHealth = updateTask.IsCompleted;
                releaseHealth.TrySetResult(true);
                Assert.IsType<OkObjectResult>(await healthTask);
                Assert.IsType<OkObjectResult>(await updateTask);
            }

            string savedName;
            HealthStatus savedHealth;
            int healthCheckCount;
            await using (var verify = new ApplicationDbContext(options))
            {
                var saved = await verify.VpnNodes.AsNoTracking().SingleAsync(x => x.Id == nodeId);
                savedName = saved.Name;
                savedHealth = saved.HealthStatus;
                healthCheckCount = await verify.NodeHealthChecks.CountAsync(x => x.NodeId == nodeId);
            }

            Assert.Equal("updated-after-health", savedName);
            Assert.Equal(HealthStatus.Healthy, savedHealth);
            Assert.Equal(1, healthCheckCount);
            Assert.False(updateCompletedBeforeHealth);
        }
        finally
        {
            releaseHealth.TrySetResult(true);
            foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Server_Update_And_Delete_Should_Require_Revision(bool delete)
    {
        await using var db = CreateDbContext();
        var node = NewNode("revision-required");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var result = delete
            ? await CreateController(db).DeleteServer(node.Id, null, CancellationToken.None)
            : await CreateController(db).UpdateServer(
                node.Id,
                UpdateRequest(node) with { Revision = null },
                CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("revision-required", (await db.VpnNodes.AsNoTracking().SingleAsync()).Name);
    }

    [Theory]
    [InlineData("maintenance")]
    [InlineData("disable-maintenance")]
    [InlineData("disable-allocation")]
    [InlineData("enable-allocation")]
    [InlineData("disable")]
    public async Task Server_Mode_Action_Should_Require_Revision(string action)
    {
        await using var db = CreateDbContext();
        var node = NewNode($"mode-revision-required-{action}");
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var controller = CreateController(db);
        var request = new ServerStateActionHttpRequest();

        var result = action switch
        {
            "maintenance" => await controller.Maintenance(node.Id, request, CancellationToken.None),
            "disable-maintenance" => await controller.DisableMaintenance(node.Id, request, CancellationToken.None),
            "disable-allocation" => await controller.DisableAllocation(node.Id, request, CancellationToken.None),
            "enable-allocation" => await controller.EnableAllocation(node.Id, request, CancellationToken.None),
            "disable" => await controller.DisableServer(node.Id, request, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(NodeStatus.Ready, node.Status);
        Assert.True(node.IsAvailableForNewUsers);
        Assert.Equal(0, node.Revision);
        Assert.Empty(db.AuditLogs);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stale_Server_Update_And_Delete_Should_Not_Overwrite_External_Changes(bool deleteSecond)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-node-concurrency-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            Guid nodeId;
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                var node = NewNode("concurrent-server");
                seed.VpnNodes.Add(node);
                await seed.SaveChangesAsync();
                nodeId = node.Id;
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var first = await firstDb.VpnNodes.SingleAsync(x => x.Id == nodeId);
            var second = await secondDb.VpnNodes.SingleAsync(x => x.Id == nodeId);

            var firstResult = await CreateController(firstDb).UpdateServer(
                nodeId,
                UpdateRequest(first, name: "first-admin-change"),
                CancellationToken.None);
            var secondResult = deleteSecond
                ? await CreateController(secondDb).DeleteServer(nodeId, second.Revision, CancellationToken.None)
                : await CreateController(secondDb).UpdateServer(
                    nodeId,
                    UpdateRequest(second, name: "stale-admin-change"),
                    CancellationToken.None);

            Assert.IsType<OkObjectResult>(firstResult);
            Assert.IsType<ConflictObjectResult>(secondResult);
            await using var verify = new ApplicationDbContext(options);
            var saved = await verify.VpnNodes.AsNoTracking().SingleAsync(x => x.Id == nodeId);
            Assert.Equal("first-admin-change", saved.Name);
            Assert.Equal(1, saved.Revision);
        }
        finally
        {
            foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    [Theory]
    [InlineData("maintenance")]
    [InlineData("disable-maintenance")]
    [InlineData("disable-allocation")]
    [InlineData("enable-allocation")]
    [InlineData("disable")]
    public async Task Stale_Server_Mode_Action_Should_Return_Conflict_Without_Overwriting_External_Changes(string action)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-platform-node-mode-concurrency-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            Guid nodeId;
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                var node = NewNode($"concurrent-mode-{action}");
                seed.VpnNodes.Add(node);
                await seed.SaveChangesAsync();
                nodeId = node.Id;
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var staleDb = new ApplicationDbContext(options);
            var first = await firstDb.VpnNodes.SingleAsync(x => x.Id == nodeId);
            _ = await staleDb.VpnNodes.SingleAsync(x => x.Id == nodeId);

            var firstResult = await CreateController(firstDb).UpdateServer(
                nodeId,
                UpdateRequest(first, name: "first-admin-mode-change"),
                CancellationToken.None);
            var staleController = CreateController(staleDb);
            var staleResult = action switch
            {
                "maintenance" => await staleController.Maintenance(nodeId, new ServerStateActionHttpRequest(0), CancellationToken.None),
                "disable-maintenance" => await staleController.DisableMaintenance(nodeId, new ServerStateActionHttpRequest(0), CancellationToken.None),
                "disable-allocation" => await staleController.DisableAllocation(nodeId, new ServerStateActionHttpRequest(0), CancellationToken.None),
                "enable-allocation" => await staleController.EnableAllocation(nodeId, new ServerStateActionHttpRequest(0), CancellationToken.None),
                "disable" => await staleController.DisableServer(nodeId, new ServerStateActionHttpRequest(0), CancellationToken.None),
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
            };

            Assert.IsType<OkObjectResult>(firstResult);
            Assert.IsType<ConflictObjectResult>(staleResult);
            await using var verify = new ApplicationDbContext(options);
            var saved = await verify.VpnNodes.AsNoTracking().SingleAsync(x => x.Id == nodeId);
            Assert.Equal("first-admin-mode-change", saved.Name);
            Assert.Equal(NodeStatus.Ready, saved.Status);
            Assert.True(saved.IsAvailableForNewUsers);
            Assert.Equal(1, saved.Revision);
        }
        finally
        {
            foreach (var path in new[] { databasePath, databasePath + "-shm", databasePath + "-wal" })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    private static VpnNode NewNode(string name)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Host = $"{name}.example.test",
            IpAddress = "203.0.113.10",
            Provider = "admin-vps",
            Region = "eu",
            Country = "NL",
            Datacenter = "ams",
            Status = NodeStatus.Ready,
            HealthStatus = HealthStatus.Healthy,
            Capacity = 100,
            IsAvailableForNewUsers = true
        };

    private static CreateServerHttpRequest UpdateRequest(VpnNode node, int? capacity = null, string? name = null, string? panelBaseUrl = null)
        => new(
            name ?? node.Name,
            node.Host,
            node.IpAddress,
            node.Provider,
            node.Region,
            node.Country,
            node.Datacenter,
            capacity ?? node.Capacity,
            node.SupportedProtocolsCsv,
            node.Priority,
            node.TagsCsv,
            node.SshUser,
            node.SshPort,
            node.SshPrivateKeyPath,
            node.SkipHostKeyChecking,
            panelBaseUrl ?? node.PanelBaseUrl,
            node.PanelUsername,
            PanelPassword: null,
            node.PanelInboundId,
            node.PublicHostname,
            node.PublicPort,
            node.NodeGroupId,
            Revision: node.Revision);

    private static AdminOperationsController CreateController(
        ApplicationDbContext db,
        IVpnProvider? vpnProvider = null,
        bool includeSecretProtector = true)
    {
        var protector = CreateSecretProtector();
        var provisioning = new ProvisioningService(db, new TestClock(), protector);
        var controller = new AdminOperationsController(
            db,
            provisioning,
            paymentOrchestrator: null!,
            paymentProviderAccounts: new PaymentProviderAccountService(db, protector, new TestClock()),
            vpnAccessLifecycleService: null,
            secretProtector: includeSecretProtector ? protector : null,
            qrCodeGenerator: new SvgQrCodeGenerator(new TestClock()),
            vpnProviderFactory: new TestVpnProviderFactory(vpnProvider ?? new TestVpnProvider(HealthStatus.Healthy)));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static ISecretProtector CreateSecretProtector()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:SecretEncryptionKey"] = "unit-test-secret-encryption-key-0000000000000000000000"
            })
            .Build();
        return new SecretProtector(configuration, new TestHostEnvironment());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider;

        public TestVpnProviderFactory(IVpnProvider provider) => _provider = provider;

        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TestVpnProvider : IVpnProvider
    {
        private readonly HealthStatus _healthStatus;
        private readonly Exception? _exception;
        private readonly TaskCompletionSource<bool>? _healthStarted;
        private readonly TaskCompletionSource<bool>? _releaseHealth;

        public TestVpnProvider(
            HealthStatus healthStatus,
            Exception? exception = null,
            TaskCompletionSource<bool>? healthStarted = null,
            TaskCompletionSource<bool>? releaseHealth = null)
        {
            _healthStatus = healthStatus;
            _exception = exception;
            _healthStarted = healthStarted;
            _releaseHealth = releaseHealth;
        }

        public string Name => "x3ui";
        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, null, null, DateTimeOffset.UtcNow));

        public async Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            _healthStarted?.TrySetResult(true);
            if (_releaseHealth is not null)
            {
                await _releaseHealth.Task.WaitAsync(cancellationToken);
            }

            return _healthStatus;
        }
    }
}
