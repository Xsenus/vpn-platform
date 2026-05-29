using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SubscriptionScenarioProvisioningTests
{
    [Fact]
    public async Task ActivateOrRenewFromOrderAsync_Should_Apply_WorkScenario_To_Vpn_Provisioning()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
        var provider = new TrackingVpnProvider();
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var highPriorityNodeId = Guid.NewGuid();
        var lowPriorityNodeId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "buyer@example.test", DisplayName = "Buyer", PasswordHash = "hash", ReferralCode = "buyer" });
        db.Tariffs.Add(new Tariff
        {
            Id = tariffId,
            Name = "Premium",
            Slug = "premium",
            DurationDays = 30,
            Price = 990m,
            Currency = "RUB",
            MaxDevices = 5,
            AllowedRegionsCsv = "eu",
            ProvisioningScenario = "premium-auto"
        });
        db.WorkScenarios.Add(new WorkScenario
        {
            Name = "Premium auto",
            Key = "premium-auto",
            IsActive = true,
            VpnProtocol = "trojan",
            ServerSelectionRule = "priority-first",
            InboundSelectionRule = "least-loaded",
            ProvisioningMode = "auto",
            OnPaymentSucceeded = "create_subscription_and_access",
            GenerateQrCode = false,
            MaxDevices = 2,
            TrafficLimit = 50L * 1024 * 1024 * 1024
        });
        db.VpnNodes.AddRange(
            Node(lowPriorityNodeId, "low", "eu", priority: 10, protocol: "trojan"),
            Node(highPriorityNodeId, "high", "eu", priority: 100, protocol: "trojan"));
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PaymentReceived, Amount = 990m, Currency = "RUB", ExpiresAt = now.AddMinutes(15) };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, Provider = PaymentProvider.YooKassa, Status = PaymentStatus.Succeeded, Amount = 990m, Currency = "RUB", ProviderPaymentId = "pay-1", PaidAt = now };
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(provider));

        var result = await service.ActivateOrRenewFromOrderAsync(order, payment);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(provider.LastRequest);
        Assert.Equal(highPriorityNodeId, provider.LastRequest!.NodeId);
        Assert.Equal("trojan", provider.LastRequest.Protocol);
        Assert.Equal("premium-auto", provider.LastRequest.ScenarioKey);
        Assert.Equal("least-loaded", provider.LastRequest.InboundSelectionRule);
        Assert.Equal(5, provider.LastRequest.MaxDevices);
        Assert.Equal(50L * 1024 * 1024 * 1024, provider.LastRequest.TrafficLimit);
        Assert.False(provider.LastRequest.GenerateQrCode);

        var access = await db.AccessCredentials.SingleAsync();
        Assert.Equal(highPriorityNodeId, access.ServerId);
        Assert.Empty(access.QrCodePath);
        Assert.Contains("trojan://", access.AccessUri, StringComparison.OrdinalIgnoreCase);
        var history = await db.AccessCredentialHistories.SingleAsync();
        Assert.Contains("premium-auto", history.NewValueJson, StringComparison.Ordinal);
        Assert.Contains("trojan", history.NewValueJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActivateOrRenewFromOrderAsync_Should_Reject_Tariff_When_Custom_Scenario_Is_Not_Allowed()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "buyer@example.test", DisplayName = "Buyer", PasswordHash = "hash", ReferralCode = "buyer" });
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Premium", Slug = "premium", DurationDays = 30, Price = 990m, Currency = "RUB", MaxDevices = 5, ProvisioningScenario = "locked-scenario" });
        db.WorkScenarios.Add(new WorkScenario
        {
            Name = "Locked",
            Key = "locked-scenario",
            IsActive = true,
            AllowedTariffIdsJson = $"[\"{Guid.NewGuid()}\"]",
            ProvisioningMode = "auto",
            OnPaymentSucceeded = "create_subscription_and_access"
        });
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PaymentReceived, Amount = 990m, Currency = "RUB", ExpiresAt = now.AddMinutes(15) };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, Provider = PaymentProvider.YooKassa, Status = PaymentStatus.Succeeded, Amount = 990m, Currency = "RUB", ProviderPaymentId = "pay-1", PaidAt = now };
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(new TrackingVpnProvider()));

        var result = await service.ActivateOrRenewFromOrderAsync(order, payment);

        Assert.False(result.IsSuccess);
        Assert.Contains("not allowed", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.Subscriptions.ToListAsync());
        Assert.Empty(await db.AccessCredentials.ToListAsync());
    }

    [Fact]
    public async Task ActivateOrRenewFromOrderAsync_Should_Create_Sandbox_Node_For_Sandbox_Payment()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
        var provider = new TrackingVpnProvider();
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "sandbox-buyer@example.test", DisplayName = "Sandbox Buyer", PasswordHash = "hash", ReferralCode = "sandbox" });
        db.Tariffs.Add(new Tariff
        {
            Id = tariffId,
            Name = "Sandbox",
            Slug = "sandbox",
            DurationDays = 30,
            Price = 100m,
            Currency = "RUB",
            MaxDevices = 3
        });
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PaymentReceived, Amount = 100m, Currency = "RUB", ExpiresAt = now.AddMinutes(15) };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, Provider = PaymentProvider.YooKassa, ProviderMode = PaymentProviderMode.Sandbox, Status = PaymentStatus.Succeeded, Amount = 100m, Currency = "RUB", ProviderPaymentId = "sandbox-pay-1", PaidAt = now };
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(provider));

        var result = await service.ActivateOrRenewFromOrderAsync(order, payment);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(provider.LastRequest);
        Assert.True(provider.LastRequest!.UseSandboxProvisioning);
        Assert.Equal("vless", provider.LastRequest.Protocol);

        var node = await db.VpnNodes.SingleAsync();
        Assert.Equal("sandbox-vpn-node", node.Name);
        Assert.Equal("sandbox", node.Region);
        Assert.Equal(NodeStatus.Ready, node.Status);

        var access = await db.AccessCredentials.SingleAsync();
        Assert.Equal(node.Id, access.ServerId);
    }

    [Fact]
    public async Task ActivateOrRenewFromOrderAsync_Should_Not_Use_Sandbox_Node_For_Production_Payment()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
        var provider = new TrackingVpnProvider();
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Users.Add(new User { Id = userId, Email = "live-buyer@example.test", DisplayName = "Live Buyer", PasswordHash = "hash", ReferralCode = "live" });
        db.Tariffs.Add(new Tariff
        {
            Id = tariffId,
            Name = "Live",
            Slug = "live",
            DurationDays = 30,
            Price = 490m,
            Currency = "RUB",
            MaxDevices = 3
        });
        db.VpnNodes.Add(Node(Guid.NewGuid(), "sandbox-vpn-node", "sandbox", priority: 1000, protocol: "vless"));
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, TariffId = tariffId, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PaymentReceived, Amount = 490m, Currency = "RUB", ExpiresAt = now.AddMinutes(15) };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, Provider = PaymentProvider.YooKassa, ProviderMode = PaymentProviderMode.Production, Status = PaymentStatus.Succeeded, Amount = 490m, Currency = "RUB", ProviderPaymentId = "live-pay-1", PaidAt = now };
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(provider));

        var result = await service.ActivateOrRenewFromOrderAsync(order, payment);

        Assert.False(result.IsSuccess);
        Assert.Contains(NodeAllocationService.NoAvailableNodeError, result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(provider.LastRequest);
        Assert.Empty(await db.AccessCredentials.ToListAsync());
        var subscription = await db.Subscriptions.SingleAsync();
        Assert.Equal(SubscriptionStatus.PendingActivation, subscription.Status);
    }

    private static VpnNode Node(Guid id, string name, string region, int priority, string protocol)
        => new()
        {
            Id = id,
            Name = name,
            Host = $"{name}.example.test",
            IpAddress = "127.0.0.1",
            Provider = "x3ui",
            Region = region,
            Country = "DE",
            Datacenter = "test",
            Status = NodeStatus.Ready,
            Capacity = 100,
            UsedCapacity = 0,
            SupportedProtocolsCsv = protocol,
            HealthStatus = HealthStatus.Healthy,
            Priority = priority,
            IsAvailableForNewUsers = true
        };

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class SingleVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider;
        public SingleVpnProviderFactory(IVpnProvider provider) => _provider = provider;
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TrackingVpnProvider : IVpnProvider
    {
        public string Name => "x3ui";
        public VpnProvisionRequest? LastRequest { get; private set; }

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var protocol = string.IsNullOrWhiteSpace(request.Protocol) ? "vless" : request.Protocol;
            var qr = request.GenerateQrCode ? $"qr:{request.SubscriptionId:N}" : string.Empty;
            return Task.FromResult(new VpnProvisionResult($"client-{request.SubscriptionId:N}", $"{protocol}://client@example.test", qr, "/config/test.json"));
        }

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => CreateAccessAsync(request, cancellationToken);

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }
}
