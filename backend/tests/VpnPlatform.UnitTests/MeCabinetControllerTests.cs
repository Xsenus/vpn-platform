using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Me;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Vpn;
using Xunit;

namespace VpnPlatform.UnitTests;

public class MeCabinetControllerTests
{
    [Theory]
    [InlineData("999", "Web", "YooKassa")]
    [InlineData("NewSubscription", "999", "YooKassa")]
    [InlineData("NewSubscription", "Web", "999")]
    public async Task Cabinet_Order_Actions_Should_Reject_Undefined_Enum_Values_Without_Creating_Data(string type, string channel, string provider)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = "monthly-invalid-enum", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        db.Users.Add(User(userId, "invalid-enum@example.test"));
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();
        var controller = CreateController(db, userId);

        var result = await controller.CreateOrder(new CreateMeOrderHttpRequest(tariff.Id, type, channel, provider, null, false, null), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task Cabinet_Payment_Init_Should_Reject_Invalid_Provider_Without_Calling_Adapter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariffId,
            Type = OrderType.NewSubscription,
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            Status = OrderStatus.PendingPayment,
            Amount = 490m,
            Currency = "RUB",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        };
        db.Users.Add(User(userId, "invalid-payment-provider@example.test"));
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Monthly", Slug = "monthly-payment-init", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true });
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await CreateController(db, userId).InitOrderPayment(order.Id, "999", null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Payments.ToListAsync());
    }

    [Theory]
    [InlineData(SubscriptionStatus.Blocked)]
    [InlineData(SubscriptionStatus.Cancelled)]
    public async Task Cabinet_Should_Reject_Renewal_For_Unsupported_Subscription_Status_On_Sqlite(SubscriptionStatus status)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = $"monthly-{status.ToString().ToLowerInvariant()}", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Status = status,
            StartAt = DateTimeOffset.UtcNow.AddDays(-30),
            EndAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(User(userId, $"{status.ToString().ToLowerInvariant()}-renewal@example.test"));
        db.Tariffs.Add(tariff);
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var result = await CreateController(db, userId).CreateOrder(
            new CreateMeOrderHttpRequest(tariff.Id, "Renewal", "Web", "YooKassa", null, false, subscription.Id),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Cabinet_Should_Reject_Qr_Request_Until_Access_Uri_Is_Issued_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Pending QR", Slug = "pending-qr", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.PendingActivation,
            StartAt = DateTimeOffset.UtcNow,
            EndAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var node = new VpnNode
        {
            Id = Guid.NewGuid(),
            Name = "pending-qr-node",
            Host = "pending-qr.example.test",
            IpAddress = "192.0.2.20",
            Provider = "x3ui",
            Region = "eu-west",
            Country = "NL",
            Status = NodeStatus.Ready,
            HealthStatus = HealthStatus.Healthy
        };
        var access = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "pending-client",
            AccessUri = string.Empty,
            Status = AccessCredentialStatus.Provisioning
        };
        db.Users.Add(User(userId, "pending-qr@example.test"));
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(subscription);
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();

        var result = await CreateController(db, userId).GetAccessQr(access.Id, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Cabinet_Should_Redact_Revoked_Access_And_Reject_All_Qr_Routes_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Revoked", Slug = "revoked-access", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "revoked-node", Host = "revoked.example.test", IpAddress = "192.0.2.30", Provider = "x3ui", Region = "eu", Country = "NL" };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Cancelled,
            StartAt = DateTimeOffset.UtcNow.AddDays(-30),
            EndAt = DateTimeOffset.UtcNow,
            CancelledAt = DateTimeOffset.UtcNow
        };
        var access = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "revoked-provider-secret",
            AccessUri = "vless://revoked-secret@example.test",
            QrCodePath = "vless://revoked-qr-secret@example.test",
            ConfigPath = "/configs/revoked-secret.json",
            Status = AccessCredentialStatus.Revoked
        };
        db.Users.Add(User(userId, "revoked-access@example.test"));
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(subscription);
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();
        subscription.CurrentAccessId = access.Id;
        await db.SaveChangesAsync();

        var meController = CreateController(db, userId);
        var subscriptions = AssertOkList(await meController.GetSubscriptions(CancellationToken.None));
        var accesses = AssertOkList(await meController.GetAccesses(CancellationToken.None));
        var subscriptionDto = Assert.Single(subscriptions);
        var accessDto = Assert.Single(accesses);

        Assert.Null(subscriptionDto.GetType().GetProperty("AccessUri")!.GetValue(subscriptionDto));
        Assert.Null(subscriptionDto.GetType().GetProperty("QrCodePath")!.GetValue(subscriptionDto));
        Assert.Null(subscriptionDto.GetType().GetProperty("ConfigPath")!.GetValue(subscriptionDto));
        Assert.Equal(string.Empty, Read<string>(accessDto, "ProviderAccessId"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "AccessUri"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "QrCodePayload"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "QrCodePath"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "ConfigPath"));
        Assert.IsType<BadRequestObjectResult>(await meController.GetAccessQr(access.Id, CancellationToken.None));

        var cabinetController = new CabinetAccessController(db, new SvgQrCodeGenerator(new TestClock()))
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextForUser(userId) }
        };
        Assert.IsType<BadRequestObjectResult>(await cabinetController.GetAccessQr(access.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Cabinet_Should_Redact_Stale_Active_Access_Of_Cancelled_Subscription_And_Reject_All_Qr_Routes_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Cancelled stale", Slug = "cancelled-stale-access", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "cancelled-node", Host = "cancelled.example.test", IpAddress = "192.0.2.31", Provider = "x3ui", Region = "eu", Country = "NL" };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Cancelled,
            StartAt = DateTimeOffset.UtcNow.AddDays(-30),
            EndAt = DateTimeOffset.UtcNow,
            CancelledAt = DateTimeOffset.UtcNow
        };
        var access = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "cancelled-provider-secret",
            AccessUri = "vless://cancelled-stale-secret@example.test",
            QrCodePath = "vless://cancelled-stale-qr-secret@example.test",
            ConfigPath = "/configs/cancelled-stale-secret.json",
            Status = AccessCredentialStatus.Active
        };
        db.Users.Add(User(userId, "cancelled-stale-access@example.test"));
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(subscription);
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();
        subscription.CurrentAccessId = access.Id;
        await db.SaveChangesAsync();

        var qrGenerator = new SvgQrCodeGenerator(new TestClock());
        var meController = CreateController(db, userId, qrGenerator);
        var subscriptions = AssertOkList(await meController.GetSubscriptions(CancellationToken.None));
        var accesses = AssertOkList(await meController.GetAccesses(CancellationToken.None));
        var subscriptionDto = Assert.Single(subscriptions);
        var accessDto = Assert.Single(accesses);

        Assert.Null(subscriptionDto.GetType().GetProperty("AccessUri")!.GetValue(subscriptionDto));
        Assert.Null(subscriptionDto.GetType().GetProperty("QrCodePath")!.GetValue(subscriptionDto));
        Assert.Null(subscriptionDto.GetType().GetProperty("ConfigPath")!.GetValue(subscriptionDto));
        Assert.Equal("Cancelled", Read<string>(accessDto, "SubscriptionStatus"));
        Assert.True(Read<bool>(accessDto, "IsTerminal"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "ProviderAccessId"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "AccessUri"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "QrCodePayload"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "QrCodePath"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "ConfigPath"));
        Assert.IsType<BadRequestObjectResult>(await meController.GetAccessQr(access.Id, CancellationToken.None));

        var cabinetController = new CabinetAccessController(db, qrGenerator)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextForUser(userId) }
        };
        Assert.IsType<BadRequestObjectResult>(await cabinetController.GetAccessQr(access.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData("me")]
    [InlineData("cabinet")]
    public async Task Cabinet_Qr_Routes_Should_Wait_For_Subscription_Gate_And_Recheck_Cancelled_Status(string route)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "QR gate", Slug = $"qr-gate-{route}", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "qr-gate-node", Host = "qr-gate.example.test", IpAddress = "192.0.2.32", Provider = "x3ui", Region = "eu", Country = "NL" };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Active,
            StartAt = DateTimeOffset.UtcNow.AddDays(-1),
            EndAt = DateTimeOffset.UtcNow.AddDays(29)
        };
        var access = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = $"qr-gate-{route}",
            AccessUri = $"vless://qr-gate-{route}@example.test",
            Status = AccessCredentialStatus.Active
        };
        db.Users.Add(User(userId, $"qr-gate-{route}@example.test"));
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(subscription);
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();

        var qrGenerator = new SvgQrCodeGenerator(new TestClock());
        var heldGate = await PaymentProcessingGate.AcquireSubscriptionLifecycleAsync(subscription.Id, CancellationToken.None);
        var qrTask = route == "me"
            ? CreateController(db, userId, qrGenerator).GetAccessQr(access.Id, CancellationToken.None)
            : new CabinetAccessController(db, qrGenerator)
            {
                ControllerContext = new ControllerContext { HttpContext = HttpContextForUser(userId) }
            }.GetAccessQr(access.Id, CancellationToken.None);

        await Task.Delay(100);
        var waitedForGate = !qrTask.IsCompleted;
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await heldGate.DisposeAsync();

        Assert.True(waitedForGate);
        Assert.IsType<BadRequestObjectResult>(await qrTask);
    }

    [Fact]
    public async Task Cabinet_Should_Return_Empty_Subscriptions_And_Accesses_For_User_Without_Subscription_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        db.Users.Add(User(userId, "empty-cabinet@example.test"));
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var subscriptionsResult = await controller.GetSubscriptions(CancellationToken.None);
        var accessesResult = await controller.GetAccesses(CancellationToken.None);

        var subscriptions = AssertOkList(subscriptionsResult);
        var accesses = AssertOkList(accessesResult);
        Assert.Empty(subscriptions);
        Assert.Empty(accesses);
    }

    [Fact]
    public async Task Cabinet_Should_Return_Active_Subscription_With_Tariff_Access_Qr_And_Server_Metadata_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var foreignUserId = Guid.NewGuid();
        var tariff = new Tariff
        {
            Id = Guid.NewGuid(),
            Name = "VPN Премиум",
            Slug = "premium",
            DurationDays = 30,
            Price = 299,
            Currency = "RUB",
            IsActive = true
        };
        var node = new VpnNode
        {
            Id = Guid.NewGuid(),
            Name = "nl-ams-1",
            Host = "nl-ams-1.example.test",
            IpAddress = "192.0.2.10",
            Provider = "x3ui",
            Region = "eu-west",
            Country = "NL",
            Status = NodeStatus.Ready,
            HealthStatus = HealthStatus.Healthy
        };
        db.Users.AddRange(User(userId, "active-cabinet@example.test"), User(foreignUserId, "foreign-cabinet@example.test"));
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var now = new DateTimeOffset(2026, 6, 10, 10, 0, 0, TimeSpan.Zero);
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Active,
            StartAt = now.AddDays(-1),
            EndAt = now.AddDays(29),
            SourceChannel = ChannelType.Web,
            CurrentServerId = node.Id,
            AutoRenewFlag = true,
            RenewalCount = 1,
            CreatedAt = now.AddMinutes(1),
            UpdatedAt = now.AddMinutes(2)
        };
        var foreignSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = foreignUserId,
            TariffId = tariff.Id,
            Status = SubscriptionStatus.Active,
            StartAt = now.AddDays(-1),
            EndAt = now.AddDays(29),
            SourceChannel = ChannelType.Web,
            CurrentServerId = node.Id,
            CreatedAt = now.AddMinutes(3),
            UpdatedAt = now.AddMinutes(3)
        };
        db.Subscriptions.AddRange(subscription, foreignSubscription);
        await db.SaveChangesAsync();

        var access = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "client-active",
            ServerId = node.Id,
            AccessUri = "vless://active-user@example.test",
            QrCodePath = "vless://active-user@example.test",
            ConfigPath = "/configs/active-user.json",
            Status = AccessCredentialStatus.Active,
            IssuedAt = now,
            Revision = 2,
            CreatedAt = now.AddMinutes(4),
            UpdatedAt = now.AddMinutes(4)
        };
        var foreignAccess = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = foreignSubscription.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "client-foreign",
            ServerId = node.Id,
            AccessUri = "vless://foreign@example.test",
            QrCodePath = "vless://foreign@example.test",
            ConfigPath = "/configs/foreign.json",
            Status = AccessCredentialStatus.Active,
            IssuedAt = now,
            Revision = 1,
            CreatedAt = now.AddMinutes(5),
            UpdatedAt = now.AddMinutes(5)
        };
        db.AccessCredentials.AddRange(access, foreignAccess);
        await db.SaveChangesAsync();

        subscription.CurrentAccessId = access.Id;
        foreignSubscription.CurrentAccessId = foreignAccess.Id;
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);

        var subscriptions = AssertOkList(await controller.GetSubscriptions(CancellationToken.None));
        var accesses = AssertOkList(await controller.GetAccesses(CancellationToken.None));

        var subscriptionDto = Assert.Single(subscriptions);
        Assert.Equal(subscription.Id, Read<Guid>(subscriptionDto, "Id"));
        Assert.Equal("Active", Read<string>(subscriptionDto, "Status"));
        Assert.Equal(tariff.Name, Read<string>(subscriptionDto, "TariffName"));
        Assert.Equal(access.Id, Read<Guid>(subscriptionDto, "CurrentAccessId"));
        Assert.Equal(node.Id, Read<Guid>(subscriptionDto, "CurrentServerId"));
        Assert.Equal(node.Name, Read<string>(subscriptionDto, "NodeName"));
        Assert.Equal(access.AccessUri, Read<string>(subscriptionDto, "AccessUri"));
        Assert.Equal(access.QrCodePath, Read<string>(subscriptionDto, "QrCodePath"));
        Assert.Equal(access.ConfigPath, Read<string>(subscriptionDto, "ConfigPath"));

        var accessDto = Assert.Single(accesses);
        Assert.Equal(access.Id, Read<Guid>(accessDto, "Id"));
        Assert.Equal(userId, Read<Guid>(accessDto, "UserId"));
        Assert.Equal(subscription.Id, Read<Guid>(accessDto, "SubscriptionId"));
        Assert.Equal(node.Name, Read<string>(accessDto, "ServerName"));
        Assert.Equal("Active", Read<string>(accessDto, "Status"));
        Assert.Equal(subscription.EndAt, Read<DateTimeOffset>(accessDto, "ExpiryDate"));
        Assert.Equal(access.AccessUri, Read<string>(accessDto, "AccessUri"));
        Assert.Equal(access.QrCodePath, Read<string>(accessDto, "QrCodePayload"));
        Assert.Equal(2, Read<int>(accessDto, "Revision"));
    }

    private static User User(Guid id, string email)
        => new()
        {
            Id = id,
            Email = email,
            DisplayName = email,
            PasswordHash = "hash",
            ReferralCode = id.ToString("N")[..12],
            Status = UserStatus.Active
        };

    private static List<object> AssertOkList(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value).ToList();
    }

    private static T Read<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(value));
    }

    private static MeController CreateController(ApplicationDbContext db, Guid userId, SvgQrCodeGenerator? qrCodeGenerator = null)
    {
        var configuration = new ConfigurationBuilder().Build();
        return new MeController(db, null!, null!, null!, null!, qrCodeGenerator!, configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "unit-test"))
                }
            }
        };
    }

    private static DefaultHttpContext HttpContextForUser(Guid userId)
        => new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                "unit-test"))
        };

    private sealed class TestClock : VpnPlatform.Application.Abstractions.IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 5, 6, 20, 0, TimeSpan.FromHours(7));
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }
}
