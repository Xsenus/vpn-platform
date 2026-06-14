# Платёжный сценарий Telegram

Status: MVP complete for roadmap blocks `P1-TG-003` and `P1-TG-004`.

## External payment providers

Telegram bot uses the existing payment architecture. It does not create anonymous orders.

Flow:

1. Telegram user registers or links an account.
2. User selects a tariff.
3. Bot confirms tariff and creates/reuses a `Channel=Telegram` order.
4. User selects provider.
5. Bot calls `PaymentOrchestrator.InitPaymentAsync`.
6. Bot sends payment URL and `checkpay:<paymentAttemptId>` button.
7. Provider webhook updates `PaymentAttempt` idempotently.
8. Successful webhook activates/renews subscription once.
9. Telegram notification is queued once for linked Telegram account.
10. `/subscriptions` and `/access` show updated status.

Supported bot checkout providers:

- YooKassa
- Robokassa
- YooMoney
- Telegram Stars

Unsupported providers remain fail-closed.

## Manual check

`checkpay:<paymentAttemptId>`:

- YooKassa: uses provider recheck when configured.
- Robokassa/YooMoney: shows DB status and tells user webhook will update final status.

## Telegram notifications

Telegram notifications are queued in `TelegramBotNotifications` and deduplicated by `TelegramUserId`, `Type`, `PayloadJson` and non-terminal queue status.

Covered event types:

- `payment_pending` - bot created a payment URL and waits for provider confirmation.
- `payment_succeeded` - external webhook succeeded, subscription was activated/renewed and VPN access is included when available.
- `payment_failed` - provider webhook returned `Failed` or `Cancelled`; the user receives retry/support buttons.
- `subscription_activated` - Telegram Stars successful payment activated subscription and created VPN access.
- `vpn_access_ready` - VPN provider created access outside the immediate payment response.
- `vpn_access_failed` - subscription activation could not create VPN access.
- `subscription_expiring` - subscription moved to grace period.
- `subscription_expired` - grace period ended and access was disabled.

## Telegram Stars

`pay:<orderId>:TelegramStars` creates a pending `PaymentAttempt` with `Currency=XTR` and payload `tgstars:<paymentAttemptId>`.

If the main API or LongPolling bot process has an effective BotToken from the admin panel or app configuration, it calls `sendInvoice` through `ITelegramInvoiceProvider`. Otherwise it fails closed with a clear message and does not mark payment successful.

Handlers:

- `pre_checkout_query`: validates payload, ownership, amount and currency, then answers through Telegram API.
- `successful_payment`: stores `TelegramBotPayment`, validates amount/currency/ownership, marks payment succeeded once, activates subscription once.
- After a valid successful payment the subscription is activated and VPN access is provisioned through the same `SubscriptionService` path as web payments.

## Validation

Main proof for the full Telegram purchase flow:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "TelegramBotPurchaseFlowTests"
```

The SQLite E2E regression `Telegram_Stars_Purchase_Should_Create_Subscription_And_Vpn_Access_On_Sqlite` validates:

- Telegram update log is written and processed;
- tariff selection creates a Telegram order;
- Telegram Stars payment attempt is prepared;
- `pre_checkout_query` accepts the valid payload;
- `successful_payment` creates `TelegramBotPayment`;
- payment becomes `Succeeded`;
- order becomes `Completed`;
- subscription becomes `Active`;
- VPN access is created;
- Telegram activation notification is queued.

Additional notification regressions:

- `PaymentWebhookProcessingTests.YooKassa_Failed_Webhook_Should_Queue_Telegram_Payment_Failed_Once_On_Sqlite`.
- `SubscriptionLifecycleExpiryTests` for `subscription_expiring` and `subscription_expired`.
- `X3UiIntegrationTests.Real_Vpn_Provider_Should_Auto_Create_Inbound_And_Client` for `vpn_access_ready`.

## Idempotency

- Telegram updates: `TelegramBotUpdates.UpdateId` unique.
- Stars payments: `TelegramBotPayments.TelegramPaymentChargeId` unique.
- External webhooks: payment webhook event uniqueness and `PaymentAttempt.IsActivationProcessed` prevent duplicate subscription activation.
- Notifications: type + payload dedupe prevents duplicate Telegram messages for the same business event.
