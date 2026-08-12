using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
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
    public async Task Node_Allocation_Should_Match_Protocol_As_Exact_Csv_Token()
    {
        await using var db = CreateDb();
        var rejected = Node(Guid.NewGuid(), "substring", "eu", priority: 100, protocol: "notvless");
        var selected = Node(Guid.NewGuid(), "exact", "eu", priority: 10, protocol: "trojan, VLESS");
        db.VpnNodes.AddRange(rejected, selected);
        await db.SaveChangesAsync();

        var result = await new NodeAllocationService(db).SelectNodeAsync(
            new Tariff { Name = "Protocol", Slug = "protocol", AllowedRegionsCsv = "eu" },
            new WorkScenario { VpnProtocol = "vless", ServerSelectionRule = "priority-first" });

        Assert.Equal(selected.Id, result.Id);
    }

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
            CabinetText = "Кабинет: доступ premium готов.",
            TelegramText = "Telegram: premium доступ готов.",
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
        Assert.Equal("premium-auto", result.Value!.ScenarioKey);
        Assert.Equal("Кабинет: доступ premium готов.", result.Value.CabinetText);
        Assert.Equal("Telegram: premium доступ готов.", result.Value.TelegramText);
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
        using var historyJson = System.Text.Json.JsonDocument.Parse(history.NewValueJson);
        Assert.Equal("premium-auto", historyJson.RootElement.GetProperty("scenarioKey").GetString());
        Assert.Equal("trojan", historyJson.RootElement.GetProperty("protocol").GetString());
        Assert.Equal("Кабинет: доступ premium готов.", historyJson.RootElement.GetProperty("scenarioCabinetText").GetString());
        Assert.Equal("Telegram: premium доступ готов.", historyJson.RootElement.GetProperty("scenarioTelegramText").GetString());
        var outbox = await db.OutboxMessages.SingleAsync(x => x.Type == "NotificationRequested");
        using var outboxJson = System.Text.Json.JsonDocument.Parse(outbox.PayloadJson);
        Assert.Equal("premium-auto", outboxJson.RootElement.GetProperty("scenarioKey").GetString());
        Assert.Equal("Кабинет: доступ premium готов.", outboxJson.RootElement.GetProperty("scenarioCabinetText").GetString());
        Assert.Equal("Telegram: premium доступ готов.", outboxJson.RootElement.GetProperty("scenarioTelegramText").GetString());

        var orchestrator = new PaymentOrchestrator(db, null!, Array.Empty<IPaymentWebhookVerifier>(), null!, null!, new FixedClock(now));
        var payloadMethod = typeof(PaymentOrchestrator).GetMethod("BuildPaymentSucceededTelegramPayloadAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(payloadMethod);
        var payloadTask = Assert.IsAssignableFrom<Task<string>>(payloadMethod.Invoke(orchestrator, new object[] { order, result.Value, CancellationToken.None }));
        var payload = await payloadTask;
        using var payloadJson = System.Text.Json.JsonDocument.Parse(payload);
        Assert.Contains("Telegram: premium доступ готов.", payloadJson.RootElement.GetProperty("text").GetString(), StringComparison.Ordinal);
        Assert.Equal("Telegram: premium доступ готов.", payloadJson.RootElement.GetProperty("scenarioText").GetString());
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
    public async Task ActivateOrRenewFromOrderAsync_Should_Reject_Legacy_Unsupported_Scenario_Protocol()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2026, 8, 12, 1, 0, 0, TimeSpan.Zero);
        var user = new User { Email = "legacy-protocol@example.test", DisplayName = "Legacy", PasswordHash = "hash", ReferralCode = "legacy-protocol" };
        var tariff = new Tariff { Name = "Legacy protocol", Slug = "legacy-protocol", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, ProvisioningScenario = "legacy-wireguard" };
        db.WorkScenarios.Add(new WorkScenario { Name = "Legacy", Key = "legacy-wireguard", IsActive = true, VpnProtocol = "wireguard", MaxDevices = 3 });
        var order = new Order { UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PaymentReceived, Amount = 490m, Currency = "RUB", ExpiresAt = now.AddMinutes(15) };
        var payment = new PaymentAttempt { OrderId = order.Id, Provider = PaymentProvider.YooKassa, Status = PaymentStatus.Succeeded, Amount = 490m, Currency = "RUB", ProviderPaymentId = "legacy-protocol", PaidAt = now };
        db.AddRange(user, tariff, order, payment);
        await db.SaveChangesAsync();
        var provider = new TrackingVpnProvider();

        var result = await new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(provider))
            .ActivateOrRenewFromOrderAsync(order, payment);

        Assert.False(result.IsSuccess);
        Assert.Contains("protocol", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(provider.LastRequest);
        Assert.Empty(await db.Subscriptions.ToListAsync());
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
        Assert.Empty(await db.VpnClients.ToListAsync());
        Assert.Equal(OrderStatus.PartiallyProcessed, order.Status);
        var subscription = await db.Subscriptions.SingleAsync();
        Assert.Equal(SubscriptionStatus.PendingActivation, subscription.Status);
        Assert.Contains(NodeAllocationService.NoAvailableNodeError, subscription.BlockReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "vpn_access.provisioning_failed" && x.EntityId == subscription.Id.ToString());
    }

    [Fact]
    public async Task ActivateOrRenewFromOrderAsync_Should_Renew_Target_Subscription_From_Order_Context()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
        var provider = new TrackingVpnProvider();
        var tariffId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var firstSubscriptionId = Guid.NewGuid();
        var targetSubscriptionId = Guid.NewGuid();
        var firstEndAt = now.AddDays(10);
        var targetEndAt = now.AddDays(20);

        db.Users.Add(new User { Id = userId, Email = "target-renewal@example.test", DisplayName = "Target renewal", PasswordHash = "hash", ReferralCode = "target" });
        db.Tariffs.Add(new Tariff
        {
            Id = tariffId,
            Name = "Target Renewal",
            Slug = "target-renewal",
            DurationDays = 30,
            Price = 990m,
            Currency = "RUB",
            MaxDevices = 2
        });
        db.VpnNodes.Add(Node(Guid.NewGuid(), "target-node", "eu", priority: 100, protocol: "vless"));
        db.Subscriptions.AddRange(
            new Subscription { Id = firstSubscriptionId, UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = now.AddDays(-5), EndAt = firstEndAt, GracePeriodEndAt = BusinessRules.GetGracePeriodEnd(firstEndAt), SourceChannel = ChannelType.Web },
            new Subscription { Id = targetSubscriptionId, UserId = userId, TariffId = tariffId, Status = SubscriptionStatus.Active, StartAt = now.AddDays(-3), EndAt = targetEndAt, GracePeriodEndAt = BusinessRules.GetGracePeriodEnd(targetEndAt), SourceChannel = ChannelType.Web });
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariffId,
            Type = OrderType.Renewal,
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            Status = OrderStatus.PaymentReceived,
            Amount = 990m,
            Currency = "RUB",
            ExpiresAt = now.AddMinutes(15),
            ReferralContext = "{\"renewalSubscriptionId\":\"" + targetSubscriptionId + "\"}"
        };
        var payment = new PaymentAttempt { Id = Guid.NewGuid(), OrderId = order.Id, Provider = PaymentProvider.YooKassa, Status = PaymentStatus.Succeeded, Amount = 990m, Currency = "RUB", ProviderPaymentId = "pay-target", PaidAt = now };
        db.Orders.Add(order);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(provider));

        var result = await service.ActivateOrRenewFromOrderAsync(order, payment);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(targetSubscriptionId, result.Value!.SubscriptionId);
        Assert.Equal(firstEndAt, (await db.Subscriptions.SingleAsync(x => x.Id == firstSubscriptionId)).EndAt);
        Assert.Equal(targetEndAt.AddDays(30), (await db.Subscriptions.SingleAsync(x => x.Id == targetSubscriptionId)).EndAt);
        Assert.Equal(targetSubscriptionId, provider.LastRequest!.SubscriptionId);
        Assert.Equal(targetSubscriptionId, (await db.AccessCredentials.SingleAsync()).SubscriptionId);
    }

    [Fact]
    public async Task ActivateOrRenewFromOrderAsync_Should_Apply_Promo_Free_Days_To_New_And_Renewal_Periods()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2026, 8, 5, 17, 0, 0, TimeSpan.Zero);
        var provider = new TrackingVpnProvider();
        var user = new User
        {
            Email = "promo-duration@example.test",
            DisplayName = "Promo duration",
            PasswordHash = "hash",
            ReferralCode = "promo-duration"
        };
        var tariff = new Tariff
        {
            Name = "Promo duration",
            Slug = "promo-duration",
            DurationDays = 30,
            Price = 500m,
            Currency = "RUB",
            MaxDevices = 3,
            IsActive = true
        };
        var promo = new PromoCode
        {
            Code = "WEEK",
            DiscountType = "percent",
            DiscountValue = 0,
            FreeDays = 7,
            IsActive = true
        };
        db.AddRange(user, tariff, promo, Node(Guid.NewGuid(), "promo-duration", "eu", 10, "vless"));
        var firstOrder = new Order
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            PromoCodeId = promo.Id,
            Type = OrderType.NewSubscription,
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            Status = OrderStatus.PaymentReceived,
            Amount = 500m,
            Currency = "RUB",
            ExpiresAt = now.AddMinutes(15),
            ReferralContext = "{\"promoFreeDays\":7}"
        };
        var firstPayment = new PaymentAttempt
        {
            OrderId = firstOrder.Id,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Production,
            Status = PaymentStatus.Succeeded,
            Amount = 500m,
            Currency = "RUB",
            ProviderPaymentId = "promo-duration-first",
            PaidAt = now
        };
        db.AddRange(firstOrder, firstPayment);
        await db.SaveChangesAsync();
        promo.FreeDays = 99;
        await db.SaveChangesAsync();
        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(provider));

        var first = await service.ActivateOrRenewFromOrderAsync(firstOrder, firstPayment);

        Assert.True(first.IsSuccess, first.Error);
        var subscription = await db.Subscriptions.SingleAsync(x => x.Id == first.Value!.SubscriptionId);
        Assert.Equal(now.AddDays(37), subscription.EndAt);

        var renewalOrder = new Order
        {
            UserId = user.Id,
            TariffId = tariff.Id,
            PromoCodeId = promo.Id,
            Type = OrderType.Renewal,
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            Status = OrderStatus.PaymentReceived,
            Amount = 500m,
            Currency = "RUB",
            ExpiresAt = now.AddMinutes(15),
            ReferralContext = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["renewalSubscriptionId"] = subscription.Id.ToString("D"),
                ["promoFreeDays"] = 7
            })
        };
        var renewalPayment = new PaymentAttempt
        {
            OrderId = renewalOrder.Id,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Production,
            Status = PaymentStatus.Succeeded,
            Amount = 500m,
            Currency = "RUB",
            ProviderPaymentId = "promo-duration-renewal",
            PaidAt = now
        };
        db.AddRange(renewalOrder, renewalPayment);
        await db.SaveChangesAsync();

        var renewal = await service.ActivateOrRenewFromOrderAsync(renewalOrder, renewalPayment);

        Assert.True(renewal.IsSuccess, renewal.Error);
        Assert.Equal(now.AddDays(74), (await db.Subscriptions.SingleAsync(x => x.Id == subscription.Id)).EndAt);
    }

    [Fact]
    public async Task ActivateOrRenewFromOrderAsync_Should_Reject_Duration_Outside_Date_Range()
    {
        await using var db = CreateDb();
        var now = new DateTimeOffset(2026, 8, 5, 17, 0, 0, TimeSpan.Zero);
        var user = new User { Email = "duration-limit@example.test", DisplayName = "Duration limit", PasswordHash = "hash", ReferralCode = "duration-limit" };
        var tariff = new Tariff { Name = "Duration limit", Slug = "duration-limit", DurationDays = int.MaxValue, Price = 500m, Currency = "RUB", MaxDevices = 1, IsActive = true };
        var order = new Order { UserId = user.Id, TariffId = tariff.Id, Type = OrderType.NewSubscription, Channel = ChannelType.Web, PaymentProvider = PaymentProvider.YooKassa, Status = OrderStatus.PaymentReceived, Amount = 500m, Currency = "RUB", ExpiresAt = now.AddMinutes(15) };
        var payment = new PaymentAttempt { OrderId = order.Id, Provider = PaymentProvider.YooKassa, ProviderMode = PaymentProviderMode.Production, Status = PaymentStatus.Succeeded, Amount = 500m, Currency = "RUB", ProviderPaymentId = "duration-limit", PaidAt = now };
        db.AddRange(user, tariff, order, payment);
        await db.SaveChangesAsync();
        var service = new SubscriptionService(db, new FixedClock(now), new NodeAllocationService(db), new SingleVpnProviderFactory(new TrackingVpnProvider()));

        var result = await service.ActivateOrRenewFromOrderAsync(order, payment);

        Assert.False(result.IsSuccess);
        Assert.Contains("date range", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.Subscriptions.ToListAsync());
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
