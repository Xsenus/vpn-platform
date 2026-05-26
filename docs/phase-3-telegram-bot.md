# Этап 3: реализация Telegram Bot

Status: working implementation draft. Do not call Phase 3 complete until `scripts/validate-all.sh` passes with .NET SDK, EF tools and frontend tooling.

## Implemented draft flow

The bot is an independent executable project: `backend/src/VpnPlatform.TelegramBot`.

Implemented user flow:

1. `/start` creates or updates `TelegramAccount` idempotently.
2. `/start link_<token>` links an existing cabinet account through a one-time hashed token.
3. Unlinked users see registration/linking/catalog/support menu.
4. Linked users see purchase/subscription/access/orders/support/cabinet menu.
5. `register_tg` creates a Telegram-origin `User` once using placeholder email `tg_<telegramUserId>@telegram.local`, `AuthSource=Telegram`, `EmailConfirmed=false`.
6. `tariffs` lists active tariffs only.
7. `buy:<tariffId>` validates tariff availability and moves session to order confirmation.
8. `confirm_order:<tariffId>` creates or reuses a user-bound Telegram order and emits `CreatedFromTelegramBot` timeline event.
9. `pay:<orderId>:YooKassa`, `pay:<orderId>:RoboKassa`, `pay:<orderId>:YooMoney` create `PaymentAttempt` through `PaymentOrchestrator` and return a payment URL.
10. `pay:<orderId>:TelegramStars` prepares a Stars `PaymentAttempt` and, when the TelegramBot executable has a configured `ITelegramInvoiceProvider`, sends `sendInvoice`; otherwise it fails closed with no fake success.
11. `checkpay:<paymentAttemptId>` shows current status and uses YooKassa recheck when supported.
12. Successful external payment webhook queues one deduplicated Telegram notification for linked accounts.
13. `/subscriptions`, `/orders`, `/access` show DB state.
14. `/access` shows config URI and QR payload when an `AccessCredential` exists; otherwise it says access is still being prepared. It never fabricates a config.
15. `/support` opens support state; text/photo/document messages are saved with attachment metadata.
16. Admin reply creates a pending Telegram notification. Internal notes/status changes are available through admin API/UI.
17. 3x-ui access creation queues a deduplicated `vpn_access_ready` Telegram notification for linked accounts.
18. Notification dispatcher retries pending notifications, respects blocked Telegram accounts and does not resend already sent notifications.

## State machine

Current states:

- `idle`
- `waiting_for_registration`
- `waiting_for_link`
- `browsing_tariffs`
- `confirming_order`
- `choosing_payment_provider`
- `waiting_for_payment`
- `support`

Supported callbacks:

- `menu`
- `tariffs`
- `buy:<tariffId>`
- `confirm_order:<tariffId>`
- `pay:<orderId>:<provider>`
- `checkpay:<paymentAttemptId>`
- `subscriptions`
- `orders`
- `access`
- `support`
- `register_tg`
- `link_account`
- `cancel`

All state-changing callbacks verify `TelegramAccount`; user-bound actions require `TelegramAccount.UserId`.

## Telegram Stars

Implemented skeleton:

- `ITelegramInvoiceProvider`
- `TelegramHttpClient.CreateInvoiceAsync` using `sendInvoice`
- `answerPreCheckoutQuery`
- `pre_checkout_query` validation
- `successful_payment` idempotency by `telegram_payment_charge_id`
- amount/currency/ownership validation
- `TelegramBotPayment` persistence
- activation via `SubscriptionService` when configured

Limitations:

- Live Stars flow requires real BotToken and Telegram environment validation.
- No fake success exists in production path.
- If invoice provider is not configured, bot explains the limitation and asks user to choose an external provider.

## Support flow

- User sends `/support`.
- Bot opens or reuses an open Telegram `SupportConversation`.
- User text/photo/document is stored as `SupportMessage` with `AttachmentsJson` metadata.
- Admin-panel lists conversations, messages, status, internal notes.
- Admin reply stores outbound `SupportMessage` and queues `TelegramBotNotification`.
- Dispatcher sends notification with retry/backoff.

## Validation

Run:

```bash
./scripts/validate-all.sh
```

Focused backend tests:

```bash
cd backend
dotnet test --filter TelegramBot
```
