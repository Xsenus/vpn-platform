# Платёжный сценарий Telegram

Status: draft. Must pass validation gate before it is treated as complete.

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

Supported draft providers:

- YooKassa
- Robokassa
- YooMoney
- Telegram Stars skeleton

Unsupported providers remain fail-closed.

## Manual check

`checkpay:<paymentAttemptId>`:

- YooKassa: uses provider recheck when configured.
- Robokassa/YooMoney: shows DB status and tells user webhook will update final status.

## Telegram Stars skeleton

`pay:<orderId>:TelegramStars` creates a pending `PaymentAttempt` with `Currency=XTR` and payload `tgstars:<paymentAttemptId>`.

If the bot service is configured with BotToken, it calls `sendInvoice` through `ITelegramInvoiceProvider`. Otherwise it fails closed with a clear message and does not mark payment successful.

Handlers:

- `pre_checkout_query`: validates payload, ownership, amount and currency, then answers through Telegram API.
- `successful_payment`: stores `TelegramBotPayment`, validates amount/currency/ownership, marks payment succeeded once, activates subscription once.

## Idempotency

- Telegram updates: `TelegramBotUpdates.UpdateId` unique.
- Stars payments: `TelegramBotPayments.TelegramPaymentChargeId` unique.
- External webhooks: payment webhook event uniqueness and `PaymentAttempt.IsActivationProcessed` prevent duplicate subscription activation.
- Notifications: type + payload dedupe prevents duplicate Telegram messages for the same business event.
