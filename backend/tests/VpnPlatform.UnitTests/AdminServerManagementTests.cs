using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
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
        Assert.Equal(NodeStatus.Archived, node.Status);
        Assert.False(node.IsAvailableForNewUsers);
        Assert.True(await db.VpnNodes.AnyAsync(x => x.Id == node.Id));
        Assert.Contains(db.AuditLogs, x => x.Action == "server.archive" && x.EntityId == node.Id.ToString());
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

    private static AdminOperationsController CreateController(ApplicationDbContext db)
    {
        var protector = CreateSecretProtector();
        var provisioning = new ProvisioningService(db, new TestClock(), protector);
        var controller = new AdminOperationsController(
            db,
            provisioning,
            paymentOrchestrator: null!,
            paymentProviderAccounts: new PaymentProviderAccountService(db, protector, new TestClock()),
            vpnAccessLifecycleService: null,
            secretProtector: protector,
            qrCodeGenerator: new SvgQrCodeGenerator(new TestClock()));
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
}
