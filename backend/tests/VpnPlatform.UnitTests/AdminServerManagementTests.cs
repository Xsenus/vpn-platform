using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
    [Theory]
    [InlineData(false, "pem")]
    [InlineData(false, "protected")]
    [InlineData(false, "placeholder")]
    [InlineData(false, "relative")]
    [InlineData(false, "quoted")]
    [InlineData(true, "pem")]
    [InlineData(true, "protected")]
    [InlineData(true, "placeholder")]
    [InlineData(true, "relative")]
    [InlineData(true, "quoted")]
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
            _ => "/run/secrets/id_ed25519\" --check"
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
    [InlineData(true, "name")]
    [InlineData(true, "capacity")]
    [InlineData(true, "priority")]
    [InlineData(true, "public-port-low")]
    [InlineData(true, "public-port-high")]
    [InlineData(true, "panel-inbound")]
    [InlineData(true, "node-group")]
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
            NodeGroupId = field == "node-group" ? Guid.NewGuid() : node.NodeGroupId
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

        var result = await controller.DisableServer(node.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(NodeStatus.Disabled, node.Status);
        Assert.False(node.IsAvailableForNewUsers);
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

        var result = await controller.DeleteServer(node.Id, CancellationToken.None);

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
            StartAt = DateTimeOffset.UtcNow,
            EndAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        db.AccessCredentials.Add(new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "client-1",
            AccessUri = "vless://client@example.test",
            Status = AccessCredentialStatus.Active
        });
        db.ProvisioningRuns.Add(new ProvisioningRun
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            Status = ProvisioningRunStatus.Succeeded,
            DryRun = true
        });
        await db.SaveChangesAsync();

        var result = await controller.DeleteServer(node.Id, CancellationToken.None);

        var response = Assert.IsType<DeleteServerHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
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

        var result = await controller.DeleteServer(node.Id, CancellationToken.None);

        var response = Assert.IsType<DeleteServerHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
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
            node.NodeGroupId);

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
