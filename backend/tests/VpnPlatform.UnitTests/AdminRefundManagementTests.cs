using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminRefundManagementTests
{
    [Fact]
    public async Task GetPayments_Should_Return_Provider_Specific_Refund_Readiness_On_Sqlite()
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var unsupportedAccount = Account(PaymentProvider.RoboKassa, secret: "secret");
        var refundable = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m, refundedAmount: 30m);
        var unsupported = Payment(user.Id, tariff.Id, unsupportedAccount, PaymentStatus.Succeeded, amount: 100m);
        var pending = Payment(user.Id, tariff.Id, account, PaymentStatus.Pending, amount: 100m);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.AddRange(account, unsupportedAccount);
        db.Orders.AddRange(refundable.Order!, unsupported.Order!, pending.Order!);
        db.Payments.AddRange(refundable, unsupported, pending);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetPayments(CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var payments = json.RootElement.EnumerateArray().ToList();

        var readyJson = payments.Single(x => x.GetProperty("Id").GetGuid() == refundable.Id);
        Assert.True(readyJson.GetProperty("CanRefund").GetBoolean());
        Assert.True(readyJson.GetProperty("RefundSupported").GetBoolean());
        Assert.True(readyJson.GetProperty("RecheckSupported").GetBoolean());
        Assert.True(readyJson.GetProperty("CanRecheck").GetBoolean());
        Assert.Empty(readyJson.GetProperty("RecheckBlockers").EnumerateArray());
        Assert.Equal(70m, readyJson.GetProperty("RefundableAmount").GetDecimal());
        Assert.Empty(readyJson.GetProperty("RefundBlockers").EnumerateArray());

        var unsupportedJson = payments.Single(x => x.GetProperty("Id").GetGuid() == unsupported.Id);
        Assert.False(unsupportedJson.GetProperty("CanRefund").GetBoolean());
        Assert.False(unsupportedJson.GetProperty("RefundSupported").GetBoolean());
        Assert.False(unsupportedJson.GetProperty("RecheckSupported").GetBoolean());
        Assert.False(unsupportedJson.GetProperty("CanRecheck").GetBoolean());
        Assert.Contains("не поддерживает", unsupportedJson.GetProperty("RecheckBlockers").EnumerateArray().First().GetString());
        Assert.Contains("не поддерживает", unsupportedJson.GetProperty("RefundBlockers").EnumerateArray().First().GetString());

        var pendingJson = payments.Single(x => x.GetProperty("Id").GetGuid() == pending.Id);
        Assert.False(pendingJson.GetProperty("CanRefund").GetBoolean());
        Assert.Contains(pendingJson.GetProperty("RefundBlockers").EnumerateArray(), x => x.GetString()?.Contains("успешных", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task RefundPayment_Should_Reject_Unsupported_Provider_Before_Provider_Call()
    {
        await using var db = CreateDb();
        var provider = new TrackingPaymentProvider(PaymentProvider.RoboKassa);
        var account = Account(PaymentProvider.RoboKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db, CreateOrchestrator(db, provider));
        var result = await controller.RefundPayment(payment.Id, new RefundPaymentHttpRequest(50m, "unsupported"), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cannot be refunded", JsonSerializer.Serialize(badRequest.Value));
        Assert.Equal(0, provider.RefundCalls);
        Assert.Empty(await db.Refunds.ToListAsync());
    }

    [Fact]
    public async Task RefundPayment_Should_Process_Supported_Refund_And_Update_Amounts()
    {
        await using var db = CreateDb();
        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa);
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db, CreateOrchestrator(db, provider));
        var ok = Assert.IsType<OkObjectResult>(await controller.RefundPayment(payment.Id, new RefundPaymentHttpRequest(40m, "partial-test"), CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.Equal("Succeeded", json.RootElement.GetProperty("Status").GetString());
        Assert.Equal(1, provider.RefundCalls);
        Assert.Equal(40m, provider.LastRefundAmount);
        await db.Entry(payment).ReloadAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(40m, payment.RefundedAmount);
        Assert.Single(await db.Refunds.ToListAsync());
        Assert.Contains(await db.AuditLogs.AsNoTracking().ToListAsync(), x => x.Action == "refund.create");
    }

    [Theory]
    [InlineData(PaymentProvider.Stripe)]
    [InlineData(PaymentProvider.PayPal)]
    [InlineData(PaymentProvider.TBankAcquiring)]
    public async Task GetPayments_Should_Allow_Credentialless_LocalSandbox_Refund(PaymentProvider provider)
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(provider, secret: string.Empty);
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db, environmentName: "Local");
        var ok = Assert.IsType<OkObjectResult>(await controller.GetPayments(CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var paymentJson = json.RootElement.EnumerateArray().Single();

        Assert.True(paymentJson.GetProperty("CanRefund").GetBoolean());
        Assert.Empty(paymentJson.GetProperty("RefundBlockers").EnumerateArray());
    }

    [Fact]
    public async Task GetPayments_Should_Expose_Provider_Mode_Snapshot_Mismatch_Blocker()
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        account.Mode = PaymentProviderMode.Production;

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetPayments(CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var paymentJson = json.RootElement.EnumerateArray().Single();

        Assert.False(paymentJson.GetProperty("CanRefund").GetBoolean());
        Assert.False(paymentJson.GetProperty("CanRecheck").GetBoolean());
        Assert.Contains(
            paymentJson.GetProperty("RefundBlockers").EnumerateArray(),
            item => item.GetString()?.Contains("режим", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(
            paymentJson.GetProperty("RecheckBlockers").EnumerateArray(),
            item => item.GetString()?.Contains("режим", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Theory]
    [InlineData("provider_mismatch")]
    [InlineData("provider_mode_mismatch")]
    [InlineData("account_disabled")]
    [InlineData("mode_disabled")]
    [InlineData("provider_payment_id_missing")]
    [InlineData("shop_id_missing")]
    [InlineData("secret_missing")]
    public async Task RecheckPayment_Should_Reject_Invalid_Account_Snapshot_Before_Provider_Call(string invalidState)
    {
        await using var db = CreateDb();
        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa);
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Pending, amount: 100m);
        payment.Order!.Status = OrderStatus.PendingPayment;

        switch (invalidState)
        {
            case "provider_mismatch":
                account.Provider = PaymentProvider.Stripe;
                break;
            case "provider_mode_mismatch":
                account.Mode = PaymentProviderMode.Production;
                break;
            case "account_disabled":
                account.IsEnabled = false;
                break;
            case "mode_disabled":
                account.Mode = PaymentProviderMode.Disabled;
                payment.ProviderMode = PaymentProviderMode.Disabled;
                break;
            case "provider_payment_id_missing":
                payment.ProviderPaymentId = string.Empty;
                break;
            case "shop_id_missing":
                account.ShopId = string.Empty;
                break;
            case "secret_missing":
                account.SecretKeyProtected = string.Empty;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidState), invalidState, null);
        }

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var result = await CreateOrchestrator(db, provider, "Production")
            .RecheckPaymentAsync(payment.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, provider.StatusCalls);
        await db.Entry(payment).ReloadAsync();
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task RecheckPayment_Should_Reject_Mismatched_Provider_Payment_Id_Without_State_Changes()
    {
        await using var db = CreateDb();
        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa, "different-provider-payment-id", PaymentStatus.Succeeded);
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Pending, amount: 100m);
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var result = await CreateOrchestrator(db, provider)
            .RecheckPaymentAsync(payment.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("identifier", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, provider.StatusCalls);
        await db.Entry(payment).ReloadAsync();
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task RecheckPayment_Controller_Should_Write_Admin_Audit_Without_Provider_Raw_Response()
    {
        await using var db = CreateDb();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Pending, amount: 100m);
        payment.Order!.Status = OrderStatus.PendingPayment;
        db.AddRange(user, tariff, account, payment.Order!, payment);
        await db.SaveChangesAsync();

        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa, status: PaymentStatus.WaitingConfirmation);
        var controller = CreateController(db, CreateOrchestrator(db, provider));
        var ok = Assert.IsType<OkObjectResult>(await controller.RecheckPayment(payment.Id, CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.False(json.RootElement.TryGetProperty("RawResponse", out _));
        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().Where(x => x.Action == "payment.recheck").ToListAsync());
        Assert.Equal("admin", audit.ActorType);
        Assert.DoesNotContain("private-provider-marker", audit.BeforeJson, StringComparison.Ordinal);
        Assert.DoesNotContain("private-provider-marker", audit.AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecheckPayment_Controller_Should_Keep_Request_Audit_When_Provider_Fails()
    {
        await using var db = CreateDb();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Pending, amount: 100m);
        payment.Order!.Status = OrderStatus.PendingPayment;
        db.AddRange(user, tariff, account, payment.Order, payment);
        await db.SaveChangesAsync();

        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa, statusError: "provider-status-private-marker");
        var controller = CreateController(db, CreateOrchestrator(db, provider));
        var badRequest = Assert.IsType<BadRequestObjectResult>(await controller.RecheckPayment(payment.Id, CancellationToken.None));

        Assert.DoesNotContain("provider-status-private-marker", JsonSerializer.Serialize(badRequest.Value), StringComparison.Ordinal);
        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().Where(x => x.Action == "payment.recheck").ToListAsync());
        Assert.DoesNotContain("provider-status-private-marker", audit.BeforeJson, StringComparison.Ordinal);
        Assert.DoesNotContain("provider-status-private-marker", audit.AfterJson, StringComparison.Ordinal);
        await db.Entry(payment).ReloadAsync();
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public async Task RecheckPayment_Controller_Should_Not_Expose_Unknown_Provider_Status_Reason()
    {
        await using var db = CreateDb();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var user = User();
        var tariff = Tariff();
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Pending, amount: 100m);
        payment.Order!.Status = OrderStatus.PendingPayment;
        db.AddRange(user, tariff, account, payment.Order, payment);
        await db.SaveChangesAsync();

        var provider = new TrackingPaymentProvider(
            PaymentProvider.YooKassa,
            status: PaymentStatus.Unknown,
            statusReason: "provider-unknown-private-marker");
        var controller = CreateController(db, CreateOrchestrator(db, provider));
        var badRequest = Assert.IsType<BadRequestObjectResult>(await controller.RecheckPayment(payment.Id, CancellationToken.None));
        var response = JsonSerializer.Serialize(badRequest.Value);
        using var responseJson = JsonDocument.Parse(response);

        Assert.DoesNotContain("provider-unknown-private-marker", response, StringComparison.Ordinal);
        Assert.Contains(
            "Повторите попытку позже",
            responseJson.RootElement.GetProperty("error").GetString(),
            StringComparison.Ordinal);
        Assert.Single(await db.AuditLogs.AsNoTracking().Where(x => x.Action == "payment.recheck").ToListAsync());
    }

    [Theory]
    [InlineData(PaymentProvider.Stripe)]
    [InlineData(PaymentProvider.PayPal)]
    [InlineData(PaymentProvider.TBankAcquiring)]
    public async Task RecheckPayment_Should_Complete_Credentialless_LocalSandbox_Flow_On_Sqlite(PaymentProvider providerType)
    {
        await using var db = CreateDb();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 12, 5, 0, 0, TimeSpan.Zero));
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var factory = new StaticHttpClientFactory(new HttpClient(new RejectHttpHandler()));
        IPaymentProvider provider = providerType switch
        {
            PaymentProvider.Stripe => new StripePaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            PaymentProvider.PayPal => new PayPalPaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            PaymentProvider.TBankAcquiring => new TBankAcquiringPaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        var orchestrator = new PaymentOrchestrator(
            db,
            new TestPaymentProviderFactory(provider),
            Array.Empty<IPaymentWebhookVerifier>(),
            accounts,
            null!,
            clock,
            new TestRuntimeEnvironment("Local"));
        var user = User();
        var tariff = Tariff();
        var account = Account(providerType, secret: string.Empty);
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Pending, amount: 100m);
        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var result = await orchestrator.RecheckPaymentAsync(payment.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(payment.ProviderPaymentId, result.Value!.PaymentId);
        Assert.Equal(PaymentStatus.Pending, result.Value.Status);
    }

    [Theory]
    [InlineData(PaymentProvider.Stripe)]
    [InlineData(PaymentProvider.PayPal)]
    [InlineData(PaymentProvider.TBankAcquiring)]
    public async Task RefundPayment_Should_Complete_Credentialless_LocalSandbox_Flow_On_Sqlite(PaymentProvider providerType)
    {
        await using var db = CreateDb();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 12, 4, 0, 0, TimeSpan.Zero));
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var factory = new StaticHttpClientFactory(new HttpClient(new RejectHttpHandler()));
        IPaymentProvider provider = providerType switch
        {
            PaymentProvider.Stripe => new StripePaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            PaymentProvider.PayPal => new PayPalPaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            PaymentProvider.TBankAcquiring => new TBankAcquiringPaymentProvider(factory, accounts, new TestHostEnvironment("Local")),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        var orchestrator = new PaymentOrchestrator(
            db,
            new TestPaymentProviderFactory(provider),
            Array.Empty<IPaymentWebhookVerifier>(),
            accounts,
            null!,
            clock,
            new TestRuntimeEnvironment("Local"));
        var user = User();
        var tariff = Tariff();
        var account = Account(providerType, secret: string.Empty);
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db, orchestrator, "Local");
        var result = await controller.RefundPayment(
            payment.Id,
            new RefundPaymentHttpRequest(40m, "local sandbox integration"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        await db.Entry(payment).ReloadAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(40m, payment.RefundedAmount);
        var refund = Assert.Single(await db.Refunds.AsNoTracking().ToListAsync());
        Assert.Equal(RefundStatus.Succeeded, refund.Status);
        Assert.Contains("sandbox", refund.ProviderRefundId, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(PaymentProvider.Stripe)]
    [InlineData(PaymentProvider.PayPal)]
    public async Task RefundPayment_Should_Recover_Malformed_Stored_Payload_On_Sqlite(PaymentProvider providerType)
    {
        await using var db = CreateDb();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 12, 8, 0, 0, TimeSpan.Zero));
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var handler = new ProviderRefundRecoveryHandler(providerType);
        var factory = new StaticHttpClientFactory(new HttpClient(handler));
        IPaymentProvider provider = providerType switch
        {
            PaymentProvider.Stripe => new StripePaymentProvider(factory, accounts, new TestHostEnvironment("Production")),
            PaymentProvider.PayPal => new PayPalPaymentProvider(factory, accounts, new TestHostEnvironment("Production")),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        var orchestrator = new PaymentOrchestrator(
            db,
            new TestPaymentProviderFactory(provider),
            Array.Empty<IPaymentWebhookVerifier>(),
            accounts,
            null!,
            clock,
            new TestRuntimeEnvironment("Production"));
        var user = User();
        var tariff = Tariff();
        var account = Account(providerType, secret: "protected-secret");
        account.Mode = PaymentProviderMode.Production;
        account.ShopId = providerType == PaymentProvider.PayPal ? "client-id" : "stripe-account";
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        payment.ProviderPaymentId = providerType == PaymentProvider.Stripe ? "cs_test_1" : "ORDER-1";
        payment.WebhookPayload = "{malformed";
        payment.RawResponse = """{"payment_intent":123,"purchase_units":"invalid"}""";

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var result = await orchestrator.RefundPaymentAsync(payment.Id, 40m, "operator refund", CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(nameof(RefundStatus.Succeeded), result.Value!.Status);
        await db.Entry(payment).ReloadAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(40m, payment.RefundedAmount);
        var refund = Assert.Single(await db.Refunds.AsNoTracking().ToListAsync());
        Assert.Equal(RefundStatus.Succeeded, refund.Status);
        Assert.Equal(providerType == PaymentProvider.Stripe ? "re_test_1" : "REFUND-1", refund.ProviderRefundId);
        Assert.Equal(2 + (providerType == PaymentProvider.PayPal ? 1 : 0), handler.Requests.Count);
        if (providerType == PaymentProvider.PayPal)
        {
            Assert.Contains(handler.Requests, request => request.Method == HttpMethod.Post && request.Path == "/v2/payments/captures/CAPTURE-1/refund");
        }
    }

    [Theory]
    [InlineData(PaymentProvider.YooKassa, "foreign-reference", false)]
    [InlineData(PaymentProvider.Stripe, "foreign-reference", false)]
    [InlineData(PaymentProvider.PayPal, "foreign-reference", false)]
    [InlineData(PaymentProvider.TBankAcquiring, "foreign-reference", false)]
    [InlineData(PaymentProvider.YooKassa, "wrong-amount", false)]
    [InlineData(PaymentProvider.Stripe, "wrong-amount", false)]
    [InlineData(PaymentProvider.PayPal, "wrong-amount", false)]
    [InlineData(PaymentProvider.YooKassa, "valid", true)]
    [InlineData(PaymentProvider.Stripe, "valid", true)]
    [InlineData(PaymentProvider.PayPal, "valid", true)]
    [InlineData(PaymentProvider.TBankAcquiring, "valid", true)]
    public async Task RefundPayment_Should_Validate_Provider_Proof_On_Sqlite(PaymentProvider providerType, string proof, bool expectedSuccess)
    {
        await using var db = CreateDb();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 12, 18, 30, 0, TimeSpan.Zero));
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var user = User();
        var tariff = Tariff();
        var account = Account(providerType, secret: "provider-secret");
        account.Mode = PaymentProviderMode.Production;
        account.ShopId = providerType == PaymentProvider.PayPal ? "paypal-client-id" : "merchant-account";
        account.ApiBaseUrl = "https://provider.test";
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        payment.ProviderMode = PaymentProviderMode.Production;
        payment.ProviderPaymentId = "PAYMENT-LOCAL";
        if (providerType == PaymentProvider.Stripe)
        {
            payment.RawResponse = "{\"payment_intent\":\"PI-LOCAL\"}";
        }
        else if (providerType == PaymentProvider.PayPal)
        {
            payment.WebhookPayload = "{\"resource\":{\"id\":\"CAPTURE-LOCAL\",\"status\":\"COMPLETED\"}}";
        }

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var handler = new RefundProofHandler(providerType, proof, payment.Id);
        var httpFactory = new StaticHttpClientFactory(new HttpClient(handler));
        var environment = new TestHostEnvironment("Production");
        IPaymentProvider provider = providerType switch
        {
            PaymentProvider.YooKassa => new YooKassaPaymentProvider(httpFactory, accounts, Microsoft.Extensions.Logging.Abstractions.NullLogger<YooKassaPaymentProvider>.Instance, environment),
            PaymentProvider.Stripe => new StripePaymentProvider(httpFactory, accounts, environment),
            PaymentProvider.PayPal => new PayPalPaymentProvider(httpFactory, accounts, environment),
            PaymentProvider.TBankAcquiring => new TBankAcquiringPaymentProvider(httpFactory, accounts, environment),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        var orchestrator = new PaymentOrchestrator(
            db,
            new TestPaymentProviderFactory(provider),
            Array.Empty<IPaymentWebhookVerifier>(),
            accounts,
            null!,
            clock,
            new TestRuntimeEnvironment("Production"));

        var result = await orchestrator.RefundPaymentAsync(payment.Id, 40m, "provider proof mismatch", CancellationToken.None);

        Assert.Equal(expectedSuccess, result.IsSuccess);
        db.ChangeTracker.Clear();
        var persistedPayment = await db.Payments.SingleAsync();
        var refund = await db.Refunds.SingleAsync();
        if (expectedSuccess)
        {
            Assert.Equal(PaymentStatus.PartiallyRefunded, persistedPayment.Status);
            Assert.Equal(40m, persistedPayment.RefundedAmount);
            Assert.Equal(RefundStatus.Succeeded, refund.Status);
            Assert.NotNull(refund.RefundedAt);
        }
        else
        {
            Assert.Contains("reconciliation", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(PaymentStatus.Succeeded, persistedPayment.Status);
            Assert.Equal(0m, persistedPayment.RefundedAmount);
            Assert.Equal(RefundStatus.Unknown, refund.Status);
            Assert.Null(refund.RefundedAt);
        }
    }

    [Fact]
    public async Task RefundPayment_Should_Allow_Sequential_TBank_Partial_Refunds_On_Sqlite()
    {
        await using var db = CreateDb();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 12, 18, 45, 0, TimeSpan.Zero));
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var user = User();
        var tariff = Tariff();
        var account = Account(PaymentProvider.TBankAcquiring, secret: "provider-secret");
        account.Mode = PaymentProviderMode.Production;
        account.ShopId = "terminal";
        account.ApiBaseUrl = "https://provider.test";
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        payment.ProviderMode = PaymentProviderMode.Production;
        payment.ProviderPaymentId = "PAYMENT-LOCAL";
        db.AddRange(user, tariff, account, payment.Order!, payment);
        await db.SaveChangesAsync();

        var handler = new RefundProofHandler(PaymentProvider.TBankAcquiring, "valid", payment.Id);
        var provider = new TBankAcquiringPaymentProvider(
            new StaticHttpClientFactory(new HttpClient(handler)),
            accounts,
            new TestHostEnvironment("Production"));
        var orchestrator = new PaymentOrchestrator(
            db,
            new TestPaymentProviderFactory(provider),
            Array.Empty<IPaymentWebhookVerifier>(),
            accounts,
            null!,
            clock,
            new TestRuntimeEnvironment("Production"));

        var first = await orchestrator.RefundPaymentAsync(payment.Id, 40m, "first partial refund", CancellationToken.None);
        var second = await orchestrator.RefundPaymentAsync(payment.Id, 30m, "second partial refund", CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        db.ChangeTracker.Clear();
        var persistedPayment = await db.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, persistedPayment.Status);
        Assert.Equal(70m, persistedPayment.RefundedAmount);
        var refunds = await db.Refunds.ToListAsync();
        Assert.Equal(2, refunds.Count);
        Assert.All(refunds, refund => Assert.Equal(RefundStatus.Succeeded, refund.Status));
        Assert.NotEqual(refunds[0].ProviderRefundId, refunds[1].ProviderRefundId);
    }

    [Theory]
    [InlineData(RefundStatus.New)]
    [InlineData(RefundStatus.Pending)]
    [InlineData(RefundStatus.Unknown)]
    public async Task GetPayments_Should_Block_New_Refund_When_Previous_Refund_Is_Unresolved(RefundStatus unresolvedStatus)
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        payment.Refunds.Add(new Refund
        {
            PaymentAttemptId = payment.Id,
            Provider = payment.Provider,
            ProviderRefundId = $"unresolved-{Guid.NewGuid():N}",
            IdempotencyKey = $"unresolved-{Guid.NewGuid():N}",
            Status = unresolvedStatus,
            Amount = 40m,
            Currency = payment.Currency,
            Reason = "awaiting reconciliation"
        });

        db.Users.Add(user);
        db.Tariffs.Add(tariff);
        db.PaymentProviderAccounts.Add(account);
        db.Orders.Add(payment.Order!);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetPayments(CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var paymentJson = json.RootElement.EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == payment.Id);

        Assert.False(paymentJson.GetProperty("CanRefund").GetBoolean());
        Assert.Contains(
            paymentJson.GetProperty("RefundBlockers").EnumerateArray(),
            item => item.GetString()?.Contains("заверш", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Theory]
    [InlineData(PaymentProvider.YooKassa, true)]
    [InlineData(PaymentProvider.Stripe, true)]
    [InlineData(PaymentProvider.PayPal, true)]
    [InlineData(PaymentProvider.TBankAcquiring, false)]
    public async Task GetRefunds_Should_Expose_Provider_Specific_Recheck_Readiness(
        PaymentProvider provider,
        bool expectedSupported)
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(provider, secret: "secret");
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        payment.Refunds.Add(new Refund
        {
            PaymentAttemptId = payment.Id,
            Provider = provider,
            ProviderRefundId = $"provider-refund-{Guid.NewGuid():N}",
            IdempotencyKey = $"refund-{Guid.NewGuid():N}",
            Status = RefundStatus.Pending,
            Amount = 40m,
            Currency = payment.Currency,
            Reason = "awaiting reconciliation"
        });

        db.AddRange(user, tariff, account, payment.Order!, payment);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetRefunds(CancellationToken.None));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var refundJson = json.RootElement.EnumerateArray().Single();

        Assert.Equal(expectedSupported, refundJson.GetProperty("RecheckSupported").GetBoolean());
        Assert.Equal(expectedSupported, refundJson.GetProperty("CanRecheck").GetBoolean());
        Assert.Equal(!expectedSupported, refundJson.GetProperty("RecheckBlockers").GetArrayLength() > 0);
    }

    [Theory]
    [InlineData(RefundStatus.Succeeded, PaymentStatus.PartiallyRefunded, 40, true)]
    [InlineData(RefundStatus.Pending, PaymentStatus.Succeeded, 0, true)]
    [InlineData(RefundStatus.Failed, PaymentStatus.Succeeded, 0, true)]
    [InlineData(RefundStatus.Cancelled, PaymentStatus.Succeeded, 0, true)]
    [InlineData(RefundStatus.Unknown, PaymentStatus.Succeeded, 0, false)]
    public async Task RecheckRefund_Should_Apply_Only_Validated_Succeeded_Amount(
        RefundStatus providerStatus,
        PaymentStatus expectedPaymentStatus,
        decimal expectedRefundedAmount,
        bool expectedSuccess)
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        var refund = new Refund
        {
            PaymentAttemptId = payment.Id,
            Provider = payment.Provider,
            ProviderRefundId = "provider-refund-1",
            IdempotencyKey = "refund-1",
            Status = RefundStatus.Pending,
            Amount = 40m,
            Currency = payment.Currency,
            Reason = "recheck"
        };
        payment.Refunds.Add(refund);
        db.AddRange(user, tariff, account, payment.Order!, payment);
        await db.SaveChangesAsync();

        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa, refundStatus: providerStatus);
        var orchestrator = CreateOrchestrator(db, provider);
        var result = await orchestrator.RecheckRefundAsync(refund.Id, CancellationToken.None);

        Assert.Equal(expectedSuccess, result.IsSuccess);
        db.ChangeTracker.Clear();
        var persistedPayment = await db.Payments.SingleAsync();
        var persistedRefund = await db.Refunds.SingleAsync();
        Assert.Equal(expectedPaymentStatus, persistedPayment.Status);
        Assert.Equal(expectedRefundedAmount, persistedPayment.RefundedAmount);
        Assert.Equal(providerStatus, persistedRefund.Status);
        Assert.Equal(1, provider.RefundStatusCalls);
    }

    [Fact]
    public async Task RecheckRefund_Should_Be_Idempotent_After_Terminal_Status()
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        var refund = new Refund
        {
            PaymentAttemptId = payment.Id,
            Provider = payment.Provider,
            ProviderRefundId = "provider-refund-1",
            IdempotencyKey = "refund-1",
            Status = RefundStatus.Pending,
            Amount = 40m,
            Currency = payment.Currency,
            Reason = "recheck"
        };
        payment.Refunds.Add(refund);
        db.AddRange(user, tariff, account, payment.Order!, payment);
        await db.SaveChangesAsync();

        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa, refundStatus: RefundStatus.Succeeded);
        var orchestrator = CreateOrchestrator(db, provider);
        Assert.True((await orchestrator.RecheckRefundAsync(refund.Id)).IsSuccess);
        Assert.True((await orchestrator.RecheckRefundAsync(refund.Id)).IsSuccess);

        db.ChangeTracker.Clear();
        Assert.Equal(40m, (await db.Payments.SingleAsync()).RefundedAmount);
        Assert.Equal(1, provider.RefundStatusCalls);
    }

    [Theory]
    [InlineData(PaymentProvider.YooKassa)]
    [InlineData(PaymentProvider.Stripe)]
    [InlineData(PaymentProvider.PayPal)]
    public async Task RecheckRefund_Should_Use_Production_Provider_Refund_Endpoint_On_Sqlite(PaymentProvider providerType)
    {
        await using var db = CreateDb();
        var clock = new TestClock(new DateTimeOffset(2026, 8, 12, 20, 0, 0, TimeSpan.Zero));
        var accounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var user = User();
        var tariff = Tariff();
        var account = Account(providerType, secret: "provider-secret");
        account.Mode = PaymentProviderMode.Production;
        account.ShopId = providerType == PaymentProvider.PayPal ? "paypal-client-id" : "merchant-account";
        account.ApiBaseUrl = "https://provider.test";
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        payment.ProviderMode = PaymentProviderMode.Production;
        payment.ProviderPaymentId = "PAYMENT-LOCAL";
        payment.RawResponse = providerType == PaymentProvider.Stripe ? "{\"payment_intent\":\"PI-LOCAL\"}" : "{}";
        payment.WebhookPayload = providerType == PaymentProvider.PayPal
            ? "{\"resource\":{\"id\":\"CAPTURE-LOCAL\",\"status\":\"COMPLETED\"}}"
            : "{}";
        var refund = new Refund
        {
            PaymentAttemptId = payment.Id,
            Provider = providerType,
            ProviderRefundId = "REFUND-1",
            IdempotencyKey = "refund-production-recheck",
            Status = RefundStatus.Pending,
            Amount = 40m,
            Currency = payment.Currency,
            Reason = "production recheck"
        };
        payment.Refunds.Add(refund);
        db.AddRange(user, tariff, account, payment.Order!, payment);
        await db.SaveChangesAsync();

        var handler = new RefundProofHandler(providerType, "valid", payment.Id);
        var httpFactory = new StaticHttpClientFactory(new HttpClient(handler));
        var environment = new TestHostEnvironment("Production");
        IPaymentProvider provider = providerType switch
        {
            PaymentProvider.YooKassa => new YooKassaPaymentProvider(httpFactory, accounts, Microsoft.Extensions.Logging.Abstractions.NullLogger<YooKassaPaymentProvider>.Instance, environment),
            PaymentProvider.Stripe => new StripePaymentProvider(httpFactory, accounts, environment),
            PaymentProvider.PayPal => new PayPalPaymentProvider(httpFactory, accounts, environment),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        var orchestrator = new PaymentOrchestrator(
            db,
            new TestPaymentProviderFactory(provider),
            Array.Empty<IPaymentWebhookVerifier>(),
            accounts,
            null!,
            clock,
            new TestRuntimeEnvironment("Production"));

        var result = await orchestrator.RecheckRefundAsync(refund.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(nameof(RefundStatus.Succeeded), result.Value!.Status);
        db.ChangeTracker.Clear();
        Assert.Equal(40m, (await db.Payments.SingleAsync()).RefundedAmount);
        Assert.Equal(RefundStatus.Succeeded, (await db.Refunds.SingleAsync()).Status);
        var expectedPath = providerType switch
        {
            PaymentProvider.YooKassa => "/refunds/REFUND-1",
            PaymentProvider.Stripe => "/v1/refunds/REFUND-1",
            PaymentProvider.PayPal => "/v2/payments/refunds/REFUND-1",
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
        Assert.Contains(handler.Requests, request => request.Method == HttpMethod.Get && request.Path == expectedPath);
    }

    [Fact]
    public async Task RecheckRefund_Controller_Should_Write_Redacted_Audit_Record()
    {
        await using var db = CreateDb();
        var user = User();
        var tariff = Tariff();
        var account = Account(PaymentProvider.YooKassa, secret: "secret");
        var payment = Payment(user.Id, tariff.Id, account, PaymentStatus.Succeeded, amount: 100m);
        var refund = new Refund
        {
            PaymentAttemptId = payment.Id,
            Provider = payment.Provider,
            ProviderRefundId = "provider-refund-audit",
            IdempotencyKey = "refund-audit",
            Status = RefundStatus.Pending,
            Amount = 40m,
            Currency = payment.Currency,
            Reason = "secret customer note"
        };
        payment.Refunds.Add(refund);
        db.AddRange(user, tariff, account, payment.Order!, payment);
        await db.SaveChangesAsync();

        var provider = new TrackingPaymentProvider(PaymentProvider.YooKassa, refundStatus: RefundStatus.Succeeded);
        var controller = CreateController(db, CreateOrchestrator(db, provider));
        Assert.IsType<OkObjectResult>(await controller.RecheckRefund(refund.Id, CancellationToken.None));

        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().Where(x => x.Action == "refund.recheck").ToListAsync());
        Assert.DoesNotContain("secret customer note", audit.BeforeJson, StringComparison.Ordinal);
        Assert.DoesNotContain("secret customer note", audit.AfterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("RawResponse", audit.AfterJson, StringComparison.Ordinal);
    }

    private static PaymentOrchestrator CreateOrchestrator(
        ApplicationDbContext db,
        IPaymentProvider provider,
        string environmentName = "Production")
    {
        var clock = new TestClock(new DateTimeOffset(2026, 6, 11, 2, 15, 0, TimeSpan.Zero));
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        return new PaymentOrchestrator(
            db,
            new TestPaymentProviderFactory(provider),
            Array.Empty<IPaymentWebhookVerifier>(),
            providerAccounts,
            null!,
            clock,
            new TestRuntimeEnvironment(environmentName));
    }

    private static AdminOperationsController CreateController(
        ApplicationDbContext db,
        PaymentOrchestrator? orchestrator = null,
        string environmentName = "Production")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Admin)
        }, "Test");

        return new AdminOperationsController(db, null!, orchestrator!, null!, hostEnvironment: new TestHostEnvironment(environmentName))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static ApplicationDbContext CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static User User()
        => new() { Id = Guid.NewGuid(), Email = "refund@test.local", DisplayName = "Refund User", PasswordHash = "hash" };

    private static Tariff Tariff()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Refund tariff",
            Slug = $"refund-{Guid.NewGuid():N}",
            Description = "Refund tariff",
            DurationDays = 30,
            Price = 100m,
            Currency = "RUB",
            MaxDevices = 3,
            IsActive = true,
            SortOrder = 10,
            Category = "standard",
            ProvisioningScenario = "auto"
        };

    private static PaymentProviderAccount Account(PaymentProvider provider, string secret)
        => new()
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            Mode = PaymentProviderMode.Sandbox,
            Name = $"{provider}-sandbox",
            PublicName = $"{provider} Sandbox",
            IsEnabled = true,
            ShopId = "shop",
            SecretKeyProtected = secret
        };

    private static PaymentAttempt Payment(Guid userId, Guid tariffId, PaymentProviderAccount account, PaymentStatus status, decimal amount, decimal refundedAmount = 0)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TariffId = tariffId,
            Amount = amount,
            Currency = "RUB",
            Status = OrderStatus.Completed,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            PaymentProvider = account.Provider
        };

        return new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PaymentProviderAccountId = account.Id,
            PaymentProviderAccount = account,
            Provider = account.Provider,
            ProviderMode = account.Mode,
            ProviderPaymentId = $"payment-{Guid.NewGuid():N}",
            IdempotencyKey = $"idem-{Guid.NewGuid():N}",
            Amount = amount,
            Currency = "RUB",
            Status = status,
            RefundedAmount = refundedAmount,
            PaidAt = status == PaymentStatus.Succeeded ? DateTimeOffset.UtcNow : null
        };
    }

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "VpnPlatform.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestRuntimeEnvironment(string environmentName) : IRuntimeEnvironment
    {
        public string EnvironmentName { get; } = environmentName;
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RejectHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new Xunit.Sdk.XunitException($"Local sandbox refund attempted HTTP: {request.Method} {request.RequestUri}");
    }

    private sealed class ProviderRefundRecoveryHandler(PaymentProvider provider) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            Requests.Add((request.Method, path));
            var isUnexpectedPayPalRefund = provider == PaymentProvider.PayPal
                && request.Method == HttpMethod.Post
                && path.Contains("/v2/payments/captures/", StringComparison.OrdinalIgnoreCase)
                && path != "/v2/payments/captures/CAPTURE-1/refund";
            var json = provider switch
            {
                PaymentProvider.Stripe when request.Method == HttpMethod.Get => """{"id":"cs_test_1","payment_status":"paid","payment_intent":"pi_test_1"}""",
                PaymentProvider.Stripe => """{"id":"re_test_1","status":"succeeded","payment_intent":"pi_test_1","amount":4000,"currency":"rub","metadata":{"paymentAttemptId":"PAYMENT_ATTEMPT_ID"}}""",
                PaymentProvider.PayPal when path.Contains("oauth2/token", StringComparison.OrdinalIgnoreCase) => """{"access_token":"access-token","token_type":"Bearer"}""",
                PaymentProvider.PayPal when request.Method == HttpMethod.Get => """{"id":"ORDER-1","status":"COMPLETED","purchase_units":[{"payments":{"captures":[{"id":"CAPTURE-1","status":"COMPLETED"}]}}]}""",
                PaymentProvider.PayPal => """{"id":"REFUND-1","status":"COMPLETED","amount":{"value":"40.00","currency_code":"RUB"},"links":[{"rel":"up","href":"https://api-m.paypal.com/v2/payments/captures/CAPTURE-1","method":"GET"}]}""",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            };
            json = json.Replace("PAYMENT_ATTEMPT_ID", ExtractPaymentAttemptId(request), StringComparison.Ordinal);
            return Task.FromResult(new HttpResponseMessage(isUnexpectedPayPalRefund ? System.Net.HttpStatusCode.NotFound : System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }

        private static string ExtractPaymentAttemptId(HttpRequestMessage request)
        {
            var idempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var values)
                ? values.Single()
                : string.Empty;
            var segments = idempotencyKey.Split('-', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length >= 2 ? segments[1] : string.Empty;
        }
    }

    private sealed class RefundProofHandler(PaymentProvider provider, string proof, Guid paymentAttemptId) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            Requests.Add((request.Method, path));
            var foreign = proof == "foreign-reference";
            var amount = proof == "wrong-amount" ? "39.00" : "40.00";
            var amountMinor = proof == "wrong-amount" ? 3900 : 4000;
            if (provider == PaymentProvider.PayPal
                && request.Method == HttpMethod.Post
                && !path.Contains("oauth2/token", StringComparison.OrdinalIgnoreCase)
                && (!request.Headers.TryGetValues("Prefer", out var preferences)
                    || !preferences.Contains("return=representation", StringComparer.OrdinalIgnoreCase)))
            {
                throw new Xunit.Sdk.XunitException("PayPal refund must request a complete provider representation.");
            }
            var json = provider switch
            {
                PaymentProvider.YooKassa => JsonSerializer.Serialize(new
                {
                    id = "REFUND-1",
                    status = "succeeded",
                    payment_id = foreign ? "PAYMENT-FOREIGN" : "PAYMENT-LOCAL",
                    amount = new { value = amount, currency = "RUB" }
                }),
                PaymentProvider.Stripe => JsonSerializer.Serialize(new
                {
                    id = "REFUND-1",
                    status = "succeeded",
                    payment_intent = foreign ? "PI-FOREIGN" : "PI-LOCAL",
                    amount = amountMinor,
                    currency = "rub",
                    metadata = new { paymentAttemptId = paymentAttemptId.ToString("N") }
                }),
                PaymentProvider.PayPal when path.Contains("oauth2/token", StringComparison.OrdinalIgnoreCase) => "{\"access_token\":\"access-token\",\"token_type\":\"Bearer\"}",
                PaymentProvider.PayPal => JsonSerializer.Serialize(new
                {
                    id = "REFUND-1",
                    status = "COMPLETED",
                    amount = new { value = amount, currency_code = "RUB" },
                    links = new[] { new { rel = "up", href = $"https://api-m.paypal.test/v2/payments/captures/{(foreign ? "CAPTURE-FOREIGN" : "CAPTURE-LOCAL")}", method = "GET" } }
                }),
                PaymentProvider.TBankAcquiring => JsonSerializer.Serialize(new
                {
                    Success = true,
                    Status = "PARTIAL_REFUNDED",
                    PaymentId = foreign ? "PAYMENT-FOREIGN" : "PAYMENT-LOCAL"
                }),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected refund proof request: {request.Method} {path}")
            };
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : "***";
    }

    private sealed class TestPaymentProviderFactory(IPaymentProvider provider) : IPaymentProviderFactory
    {
        public IPaymentProvider Get(PaymentProvider _) => provider;
    }

    private sealed class TrackingPaymentProvider(
        PaymentProvider provider,
        string? statusPaymentId = null,
        PaymentStatus? status = null,
        RefundStatus? refundStatus = null,
        string? statusError = null,
        string? statusReason = null) : IPaymentProvider, IPaymentRefundStatusProvider
    {
        public PaymentProvider Provider { get; } = provider;
        public int StatusCalls { get; private set; }
        public int RefundCalls { get; private set; }
        public int RefundStatusCalls { get; private set; }
        public decimal LastRefundAmount { get; private set; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentInitResult("payment-id", "https://payment.test", "{}"));

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
        {
            StatusCalls++;
            if (!string.IsNullOrWhiteSpace(statusError))
            {
                throw new InvalidOperationException(statusError);
            }
            return Task.FromResult(new PaymentStatusResult(statusPaymentId ?? payment.ProviderPaymentId, status ?? payment.Status, "{\"private\":\"private-provider-marker\"}", statusReason));
        }

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        {
            RefundCalls++;
            LastRefundAmount = amount;
            return Task.FromResult(new PaymentRefundResult($"refund-{payment.Id:N}", RefundStatus.Succeeded, "{}"));
        }

        public Task<PaymentRefundResult> GetRefundStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, Refund refund, CancellationToken cancellationToken)
        {
            RefundStatusCalls++;
            var nextStatus = refundStatus ?? refund.Status;
            return Task.FromResult(new PaymentRefundResult(
                refund.ProviderRefundId,
                nextStatus,
                "{}",
                payment.ProviderPaymentId,
                refund.Amount,
                refund.Currency,
                payment.Id.ToString("N"),
                nextStatus == RefundStatus.Unknown ? "provider_status_unknown" : null));
        }
    }
}
