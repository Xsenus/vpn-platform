using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Payments;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TelegramBotPurchaseFlowTests
{
    [Fact]
    public async Task Telegram_Registration_Should_Create_User_Once()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(200, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        var first = await service.ProcessUpdateAsync(CallbackUpdate(201, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        var second = await service.ProcessUpdateAsync(CallbackUpdate(202, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal(1, await db.Users.CountAsync());
        var user = await db.Users.SingleAsync();
        Assert.Equal(AuthSource.Telegram, user.AuthSource);
        Assert.False(user.EmailConfirmed);
        Assert.StartsWith("tg_", user.Email);
    }

    [Fact]
    public async Task Tariff_List_And_Buy_Should_Create_User_Bound_Order_And_Avoid_Duplicates()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(210, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        var list = await service.ProcessUpdateAsync(CallbackUpdate(211, "tariffs"), new Dictionary<string, string>(), null, CancellationToken.None);
        var beforeRegister = await service.ProcessUpdateAsync(CallbackUpdate(212, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var registerAndContinue = await service.ProcessUpdateAsync(CallbackUpdate(213, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        var duplicateBuy = await service.ProcessUpdateAsync(CallbackUpdate(214, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(list.Value!.ResponseText.Contains("Monthly", StringComparison.OrdinalIgnoreCase));
        Assert.True(beforeRegister.Value!.ResponseText.Contains("зарегистрироваться", StringComparison.OrdinalIgnoreCase));
        Assert.True(registerAndContinue.Value!.ResponseText.Contains("Заказ создан", StringComparison.OrdinalIgnoreCase));
        Assert.True(duplicateBuy.Value!.ResponseText.Contains("Подтвердите", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(ChannelType.Telegram, (await db.Orders.SingleAsync()).Channel);
        Assert.Equal(1, await db.OutboxMessages.CountAsync(x => x.Type == "OrderTimelineEvent"));
    }

    [Theory]
    [InlineData(PaymentProvider.YooKassa)]
    [InlineData(PaymentProvider.RoboKassa)]
    [InlineData(PaymentProvider.YooMoney)]
    public async Task Payment_Provider_Callback_Should_Create_PaymentAttempt_And_Link(PaymentProvider provider)
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(220, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(221, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(222, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(223, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();

        var payment = await service.ProcessUpdateAsync(CallbackUpdate(224 + (int)provider, $"pay:{order.Id}:{provider}"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(payment.IsSuccess, payment.Error);
        Assert.True(payment.Value!.ResponseText.Contains("Платеж создан", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("payment.test", payment.Value!.ReplyMarkupJson!);
        Assert.Equal(1, await db.Payments.CountAsync(x => x.Provider == provider));
        Assert.Equal(1, await db.TelegramBotNotifications.CountAsync(x => x.Type == "payment_pending"));
    }

    [Fact]
    public async Task Telegram_Stars_SuccessfulPayment_Should_Be_Idempotent()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        await EnableTelegramStarsAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(230, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(231, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(232, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(233, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();
        await service.ProcessUpdateAsync(CallbackUpdate(234, $"pay:TelegramStars:{order.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var payment = await db.Payments.SingleAsync(x => x.Provider == PaymentProvider.TelegramStars);

        var first = await service.ProcessUpdateAsync(SuccessfulPaymentUpdate(235, payment.Id, "tg-charge-1"), new Dictionary<string, string>(), null, CancellationToken.None);
        var second = await service.ProcessUpdateAsync(SuccessfulPaymentUpdate(236, payment.Id, "tg-charge-1"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal(1, await db.TelegramBotPayments.CountAsync());
        Assert.Equal(1, await db.Subscriptions.CountAsync());
        Assert.Equal(1, await db.AccessCredentials.CountAsync());
        Assert.Equal(1, await db.TelegramBotNotifications.CountAsync(x => x.Type == "subscription_activated"));
        var notification = await db.TelegramBotNotifications.SingleAsync(x => x.Type == "subscription_activated");
        Assert.Contains("vless://test@vpn.test:443", notification.PayloadJson);
        Assert.Contains("Мои ключи", notification.PayloadJson);
        Assert.Contains("Продлить", notification.PayloadJson);
        Assert.Equal(PaymentStatus.Succeeded, (await db.Payments.SingleAsync()).Status);
    }

    [Fact]
    public async Task Telegram_Stars_Purchase_Should_Create_Subscription_And_Vpn_Access_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateSqliteDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        await EnableTelegramStarsAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(390, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(391, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(392, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(393, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();
        await service.ProcessUpdateAsync(CallbackUpdate(394, $"pay:{order.Id}:TelegramStars"), new Dictionary<string, string>(), null, CancellationToken.None);
        var payment = await db.Payments.SingleAsync(x => x.Provider == PaymentProvider.TelegramStars);
        var preCheckout = await service.ProcessUpdateAsync(PreCheckoutUpdate(395, payment.Id, 490, "XTR"), new Dictionary<string, string>(), null, CancellationToken.None);
        var successful = await service.ProcessUpdateAsync(SuccessfulPaymentUpdate(396, payment.Id, "sqlite-tg-charge-1"), new Dictionary<string, string>(), null, CancellationToken.None);

        var activationPayment = await db.Payments.SingleAsync();
        Assert.True(await db.Subscriptions.AnyAsync(), $"{successful.Value?.ResponseText} {activationPayment.StatusReason}");
        var subscription = await db.Subscriptions.SingleAsync();
        var auditError = (await db.AuditLogs.ToListAsync()).OrderByDescending(x => x.CreatedAt).Select(x => x.AfterJson).FirstOrDefault();
        Assert.True(await db.AccessCredentials.AnyAsync(), $"{successful.Value?.ResponseText} {activationPayment.StatusReason} {subscription.BlockReason} {auditError}");
        var access = await db.AccessCredentials.SingleAsync();
        var updatedPayment = activationPayment;
        var updatedOrder = await db.Orders.SingleAsync();

        Assert.True(preCheckout.IsSuccess, preCheckout.Error);
        Assert.True(preCheckout.Value!.PreCheckoutOk);
        Assert.True(successful.IsSuccess, successful.Error);
        Assert.Contains("vless://test@vpn.test:443", successful.Value!.ResponseText, StringComparison.Ordinal);
        Assert.Equal(OrderStatus.Completed, updatedOrder.Status);
        Assert.Equal(PaymentStatus.Succeeded, updatedPayment.Status);
        Assert.True(updatedPayment.IsActivationProcessed);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(ChannelType.Telegram, subscription.SourceChannel);
        Assert.Equal(updatedPayment.Id, subscription.LastPaymentId);
        Assert.Equal(subscription.Id, access.SubscriptionId);
        Assert.Equal("vless://test@vpn.test:443", access.AccessUri);
        Assert.Equal(1, await db.TelegramBotPayments.CountAsync(x => x.TelegramPaymentChargeId == "sqlite-tg-charge-1"));
        Assert.Equal(1, await db.TelegramBotNotifications.CountAsync(x => x.Type == "subscription_activated" && x.TelegramUserId == 777777));
        Assert.Contains(await db.TelegramBotUpdates.ToListAsync(), x => x.UpdateId == 396 && x.IsProcessed);
    }

    [Fact]
    public async Task Telegram_Stars_PreCheckout_Should_Reject_Wrong_Currency()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        await EnableTelegramStarsAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(236, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(237, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(238, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(239, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();
        await service.ProcessUpdateAsync(CallbackUpdate(240, $"pay:TelegramStars:{order.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var payment = await db.Payments.SingleAsync(x => x.Provider == PaymentProvider.TelegramStars);

        var result = await service.ProcessUpdateAsync(PreCheckoutUpdate(241, payment.Id, 490, "USD"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value!.PreCheckoutOk);
        Assert.Contains("currency", result.Value.PreCheckoutError!.ToLowerInvariant());
    }

    [Fact]
    public async Task Telegram_Stars_SuccessfulPayment_With_Wrong_Amount_Should_Not_Activate()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        await EnableTelegramStarsAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(241, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(242, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(243, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(244, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();
        await service.ProcessUpdateAsync(CallbackUpdate(245, $"pay:TelegramStars:{order.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var payment = await db.Payments.SingleAsync(x => x.Provider == PaymentProvider.TelegramStars);

        var first = await service.ProcessUpdateAsync(SuccessfulPaymentUpdate(246, payment.Id, "tg-charge-wrong-amount", amount: 1), new Dictionary<string, string>(), null, CancellationToken.None);
        var duplicate = await service.ProcessUpdateAsync(SuccessfulPaymentUpdate(247, payment.Id, "tg-charge-wrong-amount", amount: 1), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(duplicate.IsSuccess, duplicate.Error);
        Assert.Equal(1, await db.TelegramBotPayments.CountAsync());
        Assert.Equal(0, await db.Subscriptions.CountAsync());
        Assert.Equal(0, await db.AccessCredentials.CountAsync());
        Assert.NotEqual(PaymentStatus.Succeeded, (await db.Payments.SingleAsync()).Status);
        Assert.Contains("amount", (await db.Payments.SingleAsync()).StatusReason.ToLowerInvariant());
    }


    [Fact]
    public async Task Orders_Subscriptions_And_Support_Should_Work_For_Linked_User()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(240, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(241, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(242, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(243, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var orders = await service.ProcessUpdateAsync(Update(244, "/orders"), new Dictionary<string, string>(), null, CancellationToken.None);
        var subscriptions = await service.ProcessUpdateAsync(Update(245, "/subscriptions"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(Update(246, "/support"), new Dictionary<string, string>(), null, CancellationToken.None);
        var support = await service.ProcessUpdateAsync(Update(247, "Нужна помощь с оплатой"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(orders.Value!.ResponseText.Contains("Заказ", StringComparison.OrdinalIgnoreCase));
        Assert.True(subscriptions.Value!.ResponseText.Contains("подписок", StringComparison.OrdinalIgnoreCase));
        Assert.True(support.Value!.ResponseText.Contains("поддержку", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, await db.SupportConversations.CountAsync());
        Assert.True(await db.SupportMessages.AnyAsync(x => x.Text.Contains("Нужна помощь")));
    }

    [Fact]
    public async Task Expired_Pending_Order_Should_Create_New_Order()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(250, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(251, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(252, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(253, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var first = await db.Orders.SingleAsync();
        first.ExpiresAt = new FixedClock().UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        await service.ProcessUpdateAsync(CallbackUpdate(254, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var second = await service.ProcessUpdateAsync(CallbackUpdate(255, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(second.Value!.ResponseText.Contains("Заказ создан", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, await db.Orders.CountAsync());
    }

    [Fact]
    public async Task Access_Command_Should_Show_Config_When_Ready_And_Pending_When_NotReady()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(260, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(261, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        var user = await db.Users.SingleAsync();
        var subscriptionReady = new Subscription { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Status = SubscriptionStatus.Active, StartAt = new FixedClock().UtcNow, EndAt = new FixedClock().UtcNow.AddDays(30) };
        var subscriptionPending = new Subscription { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Status = SubscriptionStatus.Active, StartAt = new FixedClock().UtcNow, EndAt = new FixedClock().UtcNow.AddDays(30) };
        db.Subscriptions.AddRange(subscriptionReady, subscriptionPending);
        db.AccessCredentials.Add(new AccessCredential
        {
            SubscriptionId = subscriptionReady.Id,
            ProviderAccessId = "client-ready",
            ServerId = db.VpnNodes.Single().Id,
            AccessUri = "vless://ready@vpn.test:443",
            QrCodePath = "vless://ready@vpn.test:443",
            ConfigPath = string.Empty,
            Status = AccessCredentialStatus.Active
        });
        await db.SaveChangesAsync();

        var access = await service.ProcessUpdateAsync(Update(262, "/access"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.Contains("vless://ready@vpn.test:443", access.Value!.ResponseText);
        Assert.Contains("доступ готовится", access.Value.ResponseText.ToLowerInvariant());
        Assert.DoesNotContain("node.example.com", access.Value.ResponseText.ToLowerInvariant());
    }

    [Fact]
    public async Task Support_Attachment_Metadata_Should_Be_Saved()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(270, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(271, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(Update(272, "/support"), new Dictionary<string, string>(), null, CancellationToken.None);
        var result = await service.ProcessUpdateAsync(DocumentUpdate(273), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        var message = await db.SupportMessages.OrderByDescending(x => x.CreatedAt).FirstAsync(x => x.AttachmentsJson != "[]");
        Assert.Contains("file-123", message.AttachmentsJson);
    }

    [Fact]
    public async Task Telegram_Stars_PreCheckout_Should_Accept_Valid_Payload()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        await EnableTelegramStarsAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(280, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(281, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(282, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(283, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();
        await service.ProcessUpdateAsync(CallbackUpdate(284, $"pay:{order.Id}:TelegramStars"), new Dictionary<string, string>(), null, CancellationToken.None);
        var payment = await db.Payments.SingleAsync(x => x.Provider == PaymentProvider.TelegramStars);

        var result = await service.ProcessUpdateAsync(PreCheckoutUpdate(285, payment.Id, 490, "XTR"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value!.PreCheckoutOk);
    }


    [Fact]
    public async Task Payment_Provider_Keyboard_Should_Show_Only_Enabled_Configured_Bot_Providers()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(290, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(291, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(292, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var result = await service.ProcessUpdateAsync(CallbackUpdate(293, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);

        var markup = result.Value!.ReplyMarkupJson!;
        Assert.Contains(nameof(PaymentProvider.YooKassa), markup, StringComparison.Ordinal);
        Assert.Contains(nameof(PaymentProvider.RoboKassa), markup, StringComparison.Ordinal);
        Assert.Contains(nameof(PaymentProvider.YooMoney), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.TelegramStars), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.TBankAcquiring), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.Prodamus), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.Stripe), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.PayPal), markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Payment_Provider_Keyboard_Should_Not_Show_Disabled_Provider()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var disabled = await db.PaymentProviderAccounts.SingleAsync(x => x.Provider == PaymentProvider.RoboKassa);
        disabled.IsEnabled = false;
        await db.SaveChangesAsync();
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(300, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(301, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(302, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var result = await service.ProcessUpdateAsync(CallbackUpdate(303, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);

        var markup = result.Value!.ReplyMarkupJson!;
        Assert.Contains(nameof(PaymentProvider.YooKassa), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.RoboKassa), markup, StringComparison.Ordinal);
        Assert.Contains(nameof(PaymentProvider.YooMoney), markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Payment_Provider_Keyboard_Should_Not_Show_Disabled_Mode_Provider()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var disabled = await db.PaymentProviderAccounts.SingleAsync(x => x.Provider == PaymentProvider.YooMoney);
        disabled.Mode = PaymentProviderMode.Disabled;
        await db.SaveChangesAsync();
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(310, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(311, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(312, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var result = await service.ProcessUpdateAsync(CallbackUpdate(313, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);

        var markup = result.Value!.ReplyMarkupJson!;
        Assert.Contains(nameof(PaymentProvider.YooKassa), markup, StringComparison.Ordinal);
        Assert.Contains(nameof(PaymentProvider.RoboKassa), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.YooMoney), markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Payment_Provider_Keyboard_Should_Not_Show_Provider_Without_Required_Settings()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var unconfigured = await db.PaymentProviderAccounts.SingleAsync(x => x.Provider == PaymentProvider.YooMoney);
        unconfigured.ShopId = string.Empty;
        await db.SaveChangesAsync();
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(314, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(315, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(316, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var result = await service.ProcessUpdateAsync(CallbackUpdate(317, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);

        var markup = result.Value!.ReplyMarkupJson!;
        Assert.Contains(nameof(PaymentProvider.YooKassa), markup, StringComparison.Ordinal);
        Assert.Contains(nameof(PaymentProvider.RoboKassa), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.YooMoney), markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Payment_Callback_For_Disabled_Provider_Should_Fail_Closed_And_Not_Create_Attempt()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var disabled = await db.PaymentProviderAccounts.SingleAsync(x => x.Provider == PaymentProvider.YooKassa);
        disabled.IsEnabled = false;
        await db.SaveChangesAsync();
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(320, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(321, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(322, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(323, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();

        var result = await service.ProcessUpdateAsync(CallbackUpdate(324, $"pay:{order.Id}:YooKassa"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.Contains("отключен", result.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await db.Payments.CountAsync());
        Assert.DoesNotContain(nameof(PaymentProvider.YooKassa), result.Value.ReplyMarkupJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Payment_Provider_Keyboard_Should_Show_Empty_State_When_No_Configured_Providers_Are_Available()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        db.PaymentProviderAccounts.RemoveRange(db.PaymentProviderAccounts);
        db.PaymentProviderAccounts.Add(new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.TBankAcquiring,
            Mode = PaymentProviderMode.Sandbox,
            Name = "TBank sandbox",
            PublicName = "TBank sandbox",
            IsEnabled = true,
            ShopId = string.Empty,
            ApiBaseUrl = "https://payment.test",
            ReturnUrl = "https://cabinet.test/payments"
        });
        await db.SaveChangesAsync();
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(330, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(331, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(332, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var result = await service.ProcessUpdateAsync(CallbackUpdate(333, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);

        var markup = result.Value!.ReplyMarkupJson!;
        Assert.Contains("Платежные методы временно недоступны", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(nameof(PaymentProvider.YooKassa), markup, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(PaymentProvider.TBankAcquiring), markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Main_Menu_Should_Expose_Mvp_Items()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        var menu = service.MainMenuText();

        Assert.Contains("Купить VPN", menu);
        Assert.Contains("Мои подписки", menu);
        Assert.Contains("Мои ключи", menu);
        Assert.Contains("Продлить доступ", menu);
        Assert.Contains("Инструкция", menu);
        Assert.Contains("Поддержка", menu);
        Assert.Contains("Профиль", menu);
        Assert.Contains("VPN на мой VPS", menu);
    }

    [Fact]
    public async Task Tariff_List_Should_Hide_Disabled_Tariffs()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAndProvidersAsync(db);
        db.Tariffs.Add(new Tariff { Id = Guid.NewGuid(), Name = "Disabled tariff", Slug = "disabled", Description = "hidden", DurationDays = 10, Price = 1m, Currency = "RUB", MaxDevices = 1, IsActive = false });
        await db.SaveChangesAsync();
        var service = CreateBot(db);

        var result = await service.ProcessUpdateAsync(CallbackUpdate(340, "tariffs"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains("Monthly", result.Value!.ResponseText);
        Assert.DoesNotContain("Disabled tariff", result.Value.ResponseText);
        Assert.DoesNotContain("Disabled tariff", result.Value.ReplyMarkupJson ?? string.Empty);
    }

    [Fact]
    public async Task Payment_Callback_For_Unknown_Provider_Should_Fail_Closed_And_Not_Create_Attempt()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(350, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(351, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(352, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(353, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();

        var result = await service.ProcessUpdateAsync(CallbackUpdate(354, $"pay:{order.Id}:UnknownProvider"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.Contains("Некорректный", result.Value!.ResponseText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await db.Payments.CountAsync());
    }

    [Fact]
    public async Task Duplicate_Payment_Callback_Should_ReUse_Pending_Attempt()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(360, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(361, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(362, $"buy:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(363, $"confirm_order:{tariff.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);
        var order = await db.Orders.SingleAsync();

        var first = await service.ProcessUpdateAsync(CallbackUpdate(364, $"pay:{order.Id}:YooKassa"), new Dictionary<string, string>(), null, CancellationToken.None);
        var second = await service.ProcessUpdateAsync(CallbackUpdate(365, $"pay:{order.Id}:YooKassa"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error);
        Assert.True(second.IsSuccess, second.Error);
        Assert.Equal(1, await db.Payments.CountAsync(x => x.Provider == PaymentProvider.YooKassa));
    }

    [Fact]
    public async Task Renewal_Flow_Should_Create_Renewal_Order_And_Respect_Provider_Filtering()
    {
        await using var db = CreateDbContext();
        var tariff = await SeedCatalogAndProvidersAsync(db);
        var disabled = await db.PaymentProviderAccounts.SingleAsync(x => x.Provider == PaymentProvider.RoboKassa);
        disabled.IsEnabled = false;
        await db.SaveChangesAsync();
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(370, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(371, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        var user = await db.Users.SingleAsync();
        var subscription = new Subscription { Id = Guid.NewGuid(), UserId = user.Id, TariffId = tariff.Id, Status = SubscriptionStatus.Active, StartAt = new FixedClock().UtcNow, EndAt = new FixedClock().UtcNow.AddDays(30) };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var list = await service.ProcessUpdateAsync(CallbackUpdate(372, "renew"), new Dictionary<string, string>(), null, CancellationToken.None);
        var result = await service.ProcessUpdateAsync(CallbackUpdate(373, $"renew:{subscription.Id}"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.Contains("Выберите подписку", list.Value!.ResponseText);
        Assert.Contains("Заказ на продление", result.Value!.ResponseText);
        Assert.Contains(nameof(PaymentProvider.YooKassa), result.Value.ReplyMarkupJson!);
        Assert.DoesNotContain(nameof(PaymentProvider.RoboKassa), result.Value.ReplyMarkupJson!);
        var order = await db.Orders.SingleAsync();
        Assert.Equal(OrderType.Renewal, order.Type);
        Assert.Equal(subscription.Id, OrderService.GetRenewalSubscriptionId(order));
    }

    [Fact]
    public async Task My_Keys_Should_Show_Empty_State_When_No_Access()
    {
        await using var db = CreateDbContext();
        await SeedCatalogAndProvidersAsync(db);
        var service = CreateBot(db);

        await service.ProcessUpdateAsync(Update(380, "/start"), new Dictionary<string, string>(), null, CancellationToken.None);
        await service.ProcessUpdateAsync(CallbackUpdate(381, "register_tg"), new Dictionary<string, string>(), null, CancellationToken.None);
        var result = await service.ProcessUpdateAsync(CallbackUpdate(382, "keys"), new Dictionary<string, string>(), null, CancellationToken.None);

        Assert.Contains("Активных подписок пока нет", result.Value!.ResponseText);
    }

    private static TelegramBotService CreateBot(ApplicationDbContext db)
    {
        var clock = new FixedClock();
        var orderService = new OrderService(db, clock);
        var providerAccounts = new PaymentProviderAccountService(db, new TestSecretProtector(), clock);
        var providers = new IPaymentProvider[]
        {
            new FakePaymentProvider(PaymentProvider.YooKassa),
            new FakePaymentProvider(PaymentProvider.RoboKassa),
            new FakePaymentProvider(PaymentProvider.YooMoney)
        };
        var paymentFactory = new PaymentProviderFactory(providers);
        var subscriptionService = new SubscriptionService(db, clock, new NodeAllocationService(db), new TestVpnProviderFactory());
        var orchestrator = new PaymentOrchestrator(db, paymentFactory, Array.Empty<IPaymentWebhookVerifier>(), providerAccounts, subscriptionService, clock);
        return new TelegramBotService(db, clock, orderService, orchestrator, subscriptionService);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<Tariff> SeedCatalogAndProvidersAsync(ApplicationDbContext db)
    {
        var tariff = new Tariff { Id = Guid.NewGuid(), Name = "Monthly", Slug = "monthly", Description = "VPN monthly", DurationDays = 30, Price = 490m, Currency = "RUB", MaxDevices = 3, IsActive = true };
        var node = new VpnNode { Id = Guid.NewGuid(), Name = "node", Host = "127.0.0.1", IpAddress = "127.0.0.1", Provider = "x3ui", Region = "test", Country = "RU", Datacenter = "local", Status = NodeStatus.Ready, HealthStatus = HealthStatus.Healthy, Capacity = 100, IsAvailableForNewUsers = true };
        db.Tariffs.Add(tariff);
        db.VpnNodes.Add(node);
        foreach (var provider in new[] { PaymentProvider.YooKassa, PaymentProvider.RoboKassa, PaymentProvider.YooMoney })
        {
            db.PaymentProviderAccounts.Add(new PaymentProviderAccount
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                Mode = PaymentProviderMode.Sandbox,
                Name = provider.ToString(),
                PublicName = provider.ToString(),
                IsEnabled = true,
                IsDefault = true,
                ShopId = $"shop-{provider}",
                ApiBaseUrl = "https://payment.test",
                ReturnUrl = "https://cabinet.test/payments",
                SecretKeyProtected = string.Empty,
                WebhookSecretProtected = string.Empty
            });
        }
        await db.SaveChangesAsync();
        return tariff;
    }

    private static async Task EnableTelegramStarsAsync(ApplicationDbContext db)
    {
        db.PaymentProviderAccounts.Add(new PaymentProviderAccount
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProvider.TelegramStars,
            Mode = PaymentProviderMode.Production,
            Name = "Telegram Stars",
            PublicName = "Telegram Stars",
            IsEnabled = true,
            IsDefault = false,
            ShopId = "telegram-stars",
            ApiBaseUrl = "https://api.telegram.org",
            ReturnUrl = "https://cabinet.test/payments",
            SecretKeyProtected = string.Empty,
            WebhookSecretProtected = string.Empty,
            ExtraSettingsJson = """{"status":"invoice-flow"}"""
        });
        await db.SaveChangesAsync();
    }

    private static string Update(long updateId, string text)
        => $$"""
        {
          "update_id": {{updateId}},
          "message": {
            "message_id": {{updateId + 1000}},
            "from": { "id": 777777, "is_bot": false, "first_name": "Ivan", "username": "ivan", "language_code": "ru" },
            "chat": { "id": 777777, "type": "private" },
            "date": 1777466400,
            "text": "{{text}}"
          }
        }
        """;

    private static string CallbackUpdate(long updateId, string data)
        => $$"""
        {
          "update_id": {{updateId}},
          "callback_query": {
            "id": "cb-{{updateId}}",
            "from": { "id": 777777, "is_bot": false, "first_name": "Ivan", "username": "ivan", "language_code": "ru" },
            "message": { "message_id": {{updateId + 1000}}, "chat": { "id": 777777, "type": "private" } },
            "data": "{{data}}"
          }
        }
        """;

    private static string SuccessfulPaymentUpdate(long updateId, Guid paymentId, string telegramChargeId, long amount = 490, string currency = "XTR")
        => $$"""
        {
          "update_id": {{updateId}},
          "message": {
            "message_id": {{updateId + 1000}},
            "from": { "id": 777777, "is_bot": false, "first_name": "Ivan", "username": "ivan", "language_code": "ru" },
            "chat": { "id": 777777, "type": "private" },
            "date": 1777466400,
            "successful_payment": {
              "currency": "{{currency}}",
              "total_amount": {{amount}},
              "invoice_payload": "tgstars:{{paymentId:N}}",
              "telegram_payment_charge_id": "{{telegramChargeId}}",
              "provider_payment_charge_id": "provider-charge-1"
            }
          }
        }
        """;

    private static string PreCheckoutUpdate(long updateId, Guid paymentId, long amount, string currency)
        => $$"""
        {
          "update_id": {{updateId}},
          "pre_checkout_query": {
            "id": "pre-{{updateId}}",
            "from": { "id": 777777, "is_bot": false, "first_name": "Ivan", "username": "ivan", "language_code": "ru" },
            "currency": "{{currency}}",
            "total_amount": {{amount}},
            "invoice_payload": "tgstars:{{paymentId:N}}"
          }
        }
        """;

    private static string DocumentUpdate(long updateId)
        => $$"""
        {
          "update_id": {{updateId}},
          "message": {
            "message_id": {{updateId + 1000}},
            "from": { "id": 777777, "is_bot": false, "first_name": "Ivan", "username": "ivan", "language_code": "ru" },
            "chat": { "id": 777777, "type": "private" },
            "date": 1777466400,
            "caption": "логи подключения",
            "document": {
              "file_id": "file-123",
              "file_unique_id": "unique-123",
              "file_name": "client-log.txt",
              "mime_type": "text/plain",
              "file_size": 42
            }
          }
        }
        """;

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 4, 30, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(0, value.Length - visibleTail)) + value[^Math.Min(visibleTail, value.Length)..];
    }

    private sealed class FakePaymentProvider : IPaymentProvider
    {
        public FakePaymentProvider(PaymentProvider provider) => Provider = provider;
        public PaymentProvider Provider { get; }

        public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentInitResult($"{Provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}", $"https://payment.test/{Provider}/{request.Order.Id:N}", "{}"));

        public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentWebhookParseResult(string.Empty, string.Empty, string.Empty, PaymentStatus.Unknown, rawBody, false));

        public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentStatusResult(payment.ProviderPaymentId, payment.Status, "{}"));

        public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
            => Task.FromResult(new PaymentRefundResult($"refund_{payment.Id:N}", RefundStatus.Pending, "{}"));
    }

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly IVpnProvider _provider = new TestVpnProvider();
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TestVpnProvider : IVpnProvider
    {
        public string Name => "x3ui";
        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult($"client-{request.SubscriptionId:N}", "vless://test@vpn.test:443", "vless://test@vpn.test:443", string.Empty));
        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken) => CreateAccessAsync(request, cancellationToken);
        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken) => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken) => Task.FromResult(HealthStatus.Healthy);
    }
}
