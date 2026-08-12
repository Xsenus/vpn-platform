using System.Data.Common;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Controllers.Me;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Vpn;
using Xunit;

namespace VpnPlatform.UnitTests;

public class MeCabinetControllerTests
{
    [Fact]
    public async Task Cabinet_Orders_Should_Return_Only_User_Facing_Fields()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var foreignUserId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Safe order", Slug = "safe-order", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Amount = 490m,
            Currency = "RUB",
            Status = OrderStatus.PendingPayment,
            Type = OrderType.Renewal,
            Channel = ChannelType.Web,
            PaymentProvider = PaymentProvider.YooKassa,
            IsFirstPurchase = true,
            ExpiresAt = new TestClock().UtcNow.AddMinutes(15),
            ReferralContext = "{\"private\":true}"
        };
        db.Users.AddRange(User(userId, "safe-order@example.test"), User(foreignUserId, "foreign-order@example.test"));
        db.Tariffs.Add(tariff);
        db.Orders.AddRange(order, new Order
        {
            Id = Guid.NewGuid(),
            UserId = foreignUserId,
            TariffId = tariff.Id,
            Amount = 990m,
            Currency = "RUB",
            Status = OrderStatus.PendingPayment,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = new TestClock().UtcNow.AddMinutes(15)
        });
        db.Payments.Add(new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = "private-provider-payment",
            IdempotencyKey = "private-idempotency",
            Amount = order.Amount,
            Currency = order.Currency,
            Status = PaymentStatus.Pending,
            RawResponse = "{\"private\":true}"
        });
        await db.SaveChangesAsync();

        var item = Assert.Single(AssertOkList(await CreateController(db, userId).GetOrders(CancellationToken.None)));

        Assert.Equal(order.Id, Read<Guid>(item, "Id"));
        Assert.Equal("Safe order", Read<string>(item, "TariffName"));
        AssertCabinetOrderInternalFieldsAbsent(item);
    }

    [Fact]
    public async Task Cabinet_Orders_Should_Apply_User_History_Limit_In_Sqlite_Query()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateSqliteDbContext(connection, interceptor);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Order limit", Slug = "order-limit", DurationDays = 30, Price = 100m, Currency = "RUB", IsActive = true };
        var now = new TestClock().UtcNow;
        db.Users.Add(User(userId, "order-limit@example.test"));
        db.Tariffs.Add(tariff);
        db.Orders.AddRange(Enumerable.Range(0, 105).Select(index => new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Amount = index,
            Currency = "RUB",
            Status = OrderStatus.Expired,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = now.AddMinutes(index),
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var items = AssertOkList(await CreateController(db, userId).GetOrders(CancellationToken.None));

        Assert.Equal(100, items.Count);
        Assert.Contains(interceptor.Commands, command => command.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cabinet_Payment_Init_Should_Not_Return_Raw_Provider_Response()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Payment init", Slug = "payment-init-safe", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Amount = 490m,
            Currency = "RUB",
            Status = OrderStatus.PendingPayment,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = new TestClock().UtcNow.AddMinutes(15)
        };
        var account = new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            Name = "safe-init",
            PublicName = "Safe init",
            IsEnabled = true,
            IsDefault = true,
            ShopId = "shop",
            SecretKeyProtected = "secret",
            ReturnUrl = "https://cabinet.example.test/payments"
        };
        db.Users.Add(User(userId, "payment-init-safe@example.test"));
        db.Tariffs.Add(tariff);
        db.Orders.Add(order);
        db.PaymentProviderAccounts.Add(account);
        db.Payments.Add(new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentProviderAccountId = account.Id,
            Provider = PaymentProvider.YooKassa,
            ProviderMode = PaymentProviderMode.Sandbox,
            ProviderPaymentId = "provider-payment-safe",
            IdempotencyKey = "payment-init-safe",
            Amount = order.Amount,
            Currency = order.Currency,
            Status = PaymentStatus.Pending,
            ConfirmationUrl = "https://pay.example.test/safe",
            RawResponse = "{\"private\":\"provider-secret\"}"
        });
        await db.SaveChangesAsync();
        var clock = new TestClock();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var orchestrator = new PaymentOrchestrator(db, new TestPaymentProviderFactory(), [], accounts, null!, clock);

        var ok = Assert.IsType<OkObjectResult>(await CreateController(db, userId, paymentOrchestrator: orchestrator)
            .InitOrderPayment(order.Id, "YooKassa", null, CancellationToken.None));
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);

        Assert.DoesNotContain("RawResponse", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider-secret", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cabinet_Payment_Init_Should_Not_Return_Provider_Exception()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Payment exception", Slug = "payment-init-exception", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Amount = 490m,
            Currency = "RUB",
            Status = OrderStatus.PendingPayment,
            PaymentProvider = PaymentProvider.YooKassa,
            ExpiresAt = new TestClock().UtcNow.AddMinutes(15)
        };
        db.Users.Add(User(userId, "payment-init-exception@example.test"));
        db.Tariffs.Add(tariff);
        db.Orders.Add(order);
        db.PaymentProviderAccounts.Add(new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.YooKassa,
            Mode = PaymentProviderMode.Sandbox,
            Name = "exception-init",
            PublicName = "Exception init",
            IsEnabled = true,
            IsDefault = true,
            ShopId = "shop",
            SecretKeyProtected = "secret",
            ReturnUrl = "https://cabinet.example.test/payments"
        });
        await db.SaveChangesAsync();
        var clock = new TestClock();
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var orchestrator = new PaymentOrchestrator(
            db,
            new TestPaymentProviderFactory("private-provider-token-and-stack"),
            [],
            accounts,
            null!,
            clock);

        var badRequest = Assert.IsType<BadRequestObjectResult>(await CreateController(db, userId, paymentOrchestrator: orchestrator)
            .InitOrderPayment(order.Id, "YooKassa", null, CancellationToken.None));
        var error = Read<string>(badRequest.Value!, "error");

        Assert.DoesNotContain("private-provider-token-and-stack", error, StringComparison.Ordinal);
        Assert.Contains("Не удалось подготовить оплату", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("999", "YooKassa")]
    [InlineData("NewSubscription", "999")]
    public async Task Cabinet_Order_Actions_Should_Reject_Undefined_Enum_Values_Without_Creating_Data(string type, string provider)
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

        var result = await controller.CreateOrder(new CreateMeOrderHttpRequest(tariff.Id, type, provider, null, null), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task Cabinet_Order_Should_Use_Server_Owned_Web_Channel_And_First_Purchase_State()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Owned context", Slug = "owned-context", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        db.Users.Add(User(userId, "owned-context@example.test"));
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();

        var result = await CreateController(db, userId).CreateOrder(
            new CreateMeOrderHttpRequest(tariff.Id, "NewSubscription", "YooKassa", null, null),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var order = await db.Orders.SingleAsync();
        Assert.Equal(ChannelType.Web, order.Channel);
        Assert.True(order.IsFirstPurchase);
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
            new CreateMeOrderHttpRequest(tariff.Id, "Renewal", "YooKassa", null, subscription.Id),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public void Cabinet_Legacy_Renew_Endpoint_Should_Return_Gone_Instead_Of_False_Success()
    {
        using var db = CreateSqliteDbContext(new SqliteConnection("Data Source=:memory:"));
        var result = Assert.IsType<ObjectResult>(CreateController(db, Guid.NewGuid()).Renew(Guid.NewGuid()));

        Assert.Equal(StatusCodes.Status410Gone, result.StatusCode);
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
        Assert.Null(subscriptionDto.GetType().GetProperty("QrCodePath"));
        Assert.Null(subscriptionDto.GetType().GetProperty("ConfigPath"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "AccessUri"));
        AssertCabinetAccessInternalFieldsAbsent(accessDto);
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
        Assert.Null(subscriptionDto.GetType().GetProperty("QrCodePath"));
        Assert.Null(subscriptionDto.GetType().GetProperty("ConfigPath"));
        Assert.Equal("Cancelled", Read<string>(accessDto, "SubscriptionStatus"));
        Assert.True(Read<bool>(accessDto, "IsTerminal"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "AccessUri"));
        AssertCabinetAccessInternalFieldsAbsent(accessDto);
        Assert.IsType<BadRequestObjectResult>(await meController.GetAccessQr(access.Id, CancellationToken.None));

        var cabinetController = new CabinetAccessController(db, qrGenerator)
        {
            ControllerContext = new ControllerContext { HttpContext = HttpContextForUser(userId) }
        };
        Assert.IsType<BadRequestObjectResult>(await cabinetController.GetAccessQr(access.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.GracePeriod)]
    public async Task Cabinet_Should_Redact_Access_After_Grace_Period_And_Reject_All_Qr_Routes_On_Sqlite(SubscriptionStatus status)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();

        var clock = new TestClock();
        var userId = Guid.NewGuid();
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Expired grace", Slug = $"expired-grace-{status}", DurationDays = 30, Price = 490m, Currency = "RUB", IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "expired-grace-node", Host = "expired-grace.example.test", IpAddress = "192.0.2.33", Provider = "x3ui", Region = "eu", Country = "NL" };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariff.Id,
            Status = status,
            StartAt = clock.UtcNow.AddDays(-33),
            EndAt = clock.UtcNow.AddDays(-3),
            GracePeriodEndAt = clock.UtcNow
        };
        var access = new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            ServerId = node.Id,
            ProviderType = "x3ui",
            ProviderAccessId = "expired-grace-provider-secret",
            AccessUri = "vless://expired-grace-secret@example.test",
            QrCodePath = "vless://expired-grace-qr-secret@example.test",
            ConfigPath = "/configs/expired-grace-secret.json",
            Status = AccessCredentialStatus.Active
        };
        db.Users.Add(User(userId, $"expired-grace-{status}@example.test"));
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        db.Subscriptions.Add(subscription);
        db.AccessCredentials.Add(access);
        await db.SaveChangesAsync();
        subscription.CurrentAccessId = access.Id;
        await db.SaveChangesAsync();

        var qrGenerator = new SvgQrCodeGenerator(clock);
        var meController = CreateController(db, userId, qrGenerator);
        var subscriptionDto = Assert.Single(AssertOkList(await meController.GetSubscriptions(CancellationToken.None)));
        var accessDto = Assert.Single(AssertOkList(await meController.GetAccesses(CancellationToken.None)));

        Assert.Null(subscriptionDto.GetType().GetProperty("AccessUri")!.GetValue(subscriptionDto));
        Assert.Null(subscriptionDto.GetType().GetProperty("QrCodePath"));
        Assert.Null(subscriptionDto.GetType().GetProperty("ConfigPath"));
        Assert.True(Read<bool>(accessDto, "IsTerminal"));
        Assert.Equal(string.Empty, Read<string>(accessDto, "AccessUri"));
        AssertCabinetAccessInternalFieldsAbsent(accessDto);
        Assert.Equal(clock.UtcNow, Read<DateTimeOffset>(accessDto, "ExpiryDate"));
        Assert.IsType<BadRequestObjectResult>(await meController.GetAccessQr(access.Id, CancellationToken.None));

        var cabinetController = new CabinetAccessController(db, qrGenerator, clock)
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

        var now = new TestClock().UtcNow;
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
            BlockReason = "private-x3ui-provider-exception",
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
        Assert.Equal(node.Name, Read<string>(subscriptionDto, "NodeName"));
        Assert.Equal(access.AccessUri, Read<string>(subscriptionDto, "AccessUri"));
        var subscriptionJson = System.Text.Json.JsonSerializer.Serialize(subscriptionDto);
        Assert.DoesNotContain("BlockReason", subscriptionJson);
        Assert.DoesNotContain("CurrentServerId", subscriptionJson);
        Assert.DoesNotContain("LastPaymentId", subscriptionJson);
        Assert.DoesNotContain("QrCodePath", subscriptionJson);
        Assert.DoesNotContain("ConfigPath", subscriptionJson);
        Assert.DoesNotContain("private-x3ui-provider-exception", subscriptionJson);

        var accessDto = Assert.Single(accesses);
        Assert.Equal(access.Id, Read<Guid>(accessDto, "Id"));
        Assert.Equal(subscription.Id, Read<Guid>(accessDto, "SubscriptionId"));
        Assert.Equal(node.Name, Read<string>(accessDto, "ServerName"));
        Assert.Equal("Active", Read<string>(accessDto, "Status"));
        Assert.Equal(subscription.EndAt, Read<DateTimeOffset>(accessDto, "ExpiryDate"));
        Assert.Equal(access.AccessUri, Read<string>(accessDto, "AccessUri"));
        AssertCabinetAccessInternalFieldsAbsent(accessDto);
        var accessJson = System.Text.Json.JsonSerializer.Serialize(accessDto);
        Assert.DoesNotContain("client-active", accessJson);
        Assert.DoesNotContain("/configs/active-user.json", accessJson);
    }

    [Fact]
    public async Task Cabinet_Subscriptions_Should_Apply_User_History_Limit_In_Sqlite_Query()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateSqliteDbContext(connection, interceptor);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var now = new TestClock().UtcNow;
        db.Users.Add(User(userId, "subscription-limit@example.test"));
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Limit", Slug = "subscription-limit", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        db.Subscriptions.AddRange(Enumerable.Range(0, 105).Select(index => new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariffId,
            Status = SubscriptionStatus.Expired,
            StartAt = now.AddDays(-60),
            EndAt = now.AddDays(-30),
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var result = await CreateController(db, userId).GetSubscriptions(CancellationToken.None);

        Assert.Equal(100, AssertOkList(result).Count);
        Assert.Contains(interceptor.Commands, command => command.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cabinet_Accesses_Should_Apply_User_History_Limit_In_Sqlite_Query()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateSqliteDbContext(connection, interceptor);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var now = new TestClock().UtcNow;
        db.Users.Add(User(userId, "access-limit@example.test"));
        db.Tariffs.Add(new Tariff { Id = tariffId, Name = "Limit", Slug = "access-limit", DurationDays = 30, Price = 100, Currency = "RUB", IsActive = true });
        db.VpnNodes.Add(new VpnNode { Id = serverId, Name = "limit-node", Host = "limit.example.test", IpAddress = "192.0.2.40", Provider = "x3ui", Region = "eu", Country = "NL" });
        db.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            UserId = userId,
            TariffId = tariffId,
            Status = SubscriptionStatus.Expired,
            StartAt = now.AddDays(-60),
            EndAt = now.AddDays(-30)
        });
        db.AccessCredentials.AddRange(Enumerable.Range(0, 105).Select(index => new AccessCredential
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            ServerId = serverId,
            ProviderType = "x3ui",
            ProviderAccessId = $"limit-client-{index}",
            AccessUri = $"vless://limit-{index}@example.test",
            Status = AccessCredentialStatus.Revoked,
            IssuedAt = now.AddMinutes(index),
            CreatedAt = now.AddMinutes(index),
            UpdatedAt = now.AddMinutes(index)
        }));
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var result = await CreateController(db, userId).GetAccesses(CancellationToken.None);

        Assert.Equal(100, AssertOkList(result).Count);
        Assert.Contains(interceptor.Commands, command => command.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
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

    private static void AssertCabinetAccessInternalFieldsAbsent(object value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        foreach (var field in new[]
                 {
                     "UserId", "ProviderType", "ProviderAccessId", "ServerId", "QrCodePayload", "QrCodePath",
                     "ConfigPath", "IssuedAt", "DisabledAt", "LastSyncedAt", "Revision", "History", "CreatedAt", "UpdatedAt"
                 })
        {
            Assert.DoesNotContain($"\"{field}\"", json, StringComparison.Ordinal);
        }
    }

    private static void AssertCabinetOrderInternalFieldsAbsent(object value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        foreach (var field in new[] { "UserId", "CheckoutSessionId", "Channel", "IsFirstPurchase", "PaymentAttemptsCount" })
        {
            Assert.DoesNotContain($"\"{field}\"", json, StringComparison.Ordinal);
        }
    }

    private static MeController CreateController(
        ApplicationDbContext db,
        Guid userId,
        SvgQrCodeGenerator? qrCodeGenerator = null,
        PaymentOrchestrator? paymentOrchestrator = null)
    {
        var configuration = new ConfigurationBuilder().Build();
        var clock = new TestClock();
        var orderService = new VpnPlatform.Application.Services.OrderService(db, clock);
        return new MeController(db, orderService, new CheckoutSessionService(db, clock, orderService), paymentOrchestrator!, null!, qrCodeGenerator!, configuration, clock)
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

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection, IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection);
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new ApplicationDbContext(builder.Options);
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => "***";
    }

    private sealed class TestPaymentProviderFactory(string? exceptionMessage = null) : IPaymentProviderFactory
    {
        public IPaymentProvider Get(PaymentProvider provider) => new TestPaymentProvider(provider, exceptionMessage);
    }

    private sealed class TestPaymentProvider(PaymentProvider provider, string? exceptionMessage) : IPaymentProvider
    {
        public PaymentProvider Provider { get; } = provider;
        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException(exceptionMessage ?? "Provider call was not expected.");
        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
