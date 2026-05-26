# Платежи Telegram Stars

Status: foundation/live-ready handler draft; requires real BotToken staging validation.

Implemented in the Telegram Bot service:

- create invoice abstraction;
- `sendInvoice` call through `TelegramHttpClient`;
- `pre_checkout_query` validation;
- `answerPreCheckoutQuery`;
- `successful_payment` handling;
- idempotency by `telegram_payment_charge_id`;
- amount/currency/user ownership validation;
- activation through existing payment/subscription flow.

The generic `IPaymentProvider` for `TelegramStars` remains fail-closed because Stars are not redirect/browser payments. Do not enable fake success in production.
