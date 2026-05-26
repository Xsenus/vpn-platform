# Платёжные провайдеры

Status: draft implementation; requires `./scripts/validate-all.sh` and staging checks before production enablement.

## Common architecture

All payment providers are configured as `PaymentProviderAccount` records in the database. Secrets are stored through `ISecretProtector` and are never returned by API DTOs; admin UI only receives `hasSecretKey` and `hasWebhookSecret` flags.

Supported provider enum values:

- YooKassa
- RoboKassa
- YooMoney
- TelegramStars
- CloudPayments
- TBankAcquiring
- Prodamus
- Stripe
- PayPal

Every provider adapter must:

- create a `PaymentAttempt` through `PaymentOrchestrator`;
- keep raw request/response sanitized;
- parse webhook raw body;
- verify signature/authenticity before status is applied;
- reject wrong amount/currency/order/provider account;
- handle duplicate webhook idempotently;
- fail closed for unsupported recheck/refund operations.

## Webhook endpoints

```text
POST /api/webhooks/payments/yookassa
POST /api/webhooks/payments/robokassa
POST /api/webhooks/payments/yoomoney
POST /api/webhooks/payments/cloudpayments
POST /api/webhooks/payments/cloudpayments/{eventType}
POST /api/webhooks/payments/tbank
POST /api/webhooks/payments/tbank-acquiring
POST /api/webhooks/payments/prodamus
POST /api/webhooks/payments/stripe
POST /api/webhooks/payments/paypal
```

## Local sandbox

Local sandbox headers are accepted only outside Production and only for sandbox accounts without credentials. Production rejects sandbox headers.

```text
X-YooKassa-Sandbox-Webhook: true
X-YooMoney-Sandbox-Webhook: true
X-Stripe-Sandbox-Webhook: true
X-PayPal-Sandbox-Webhook: true
X-TBank-Sandbox-Webhook: true
X-CloudPayments-Sandbox-Webhook: true
X-Prodamus-Sandbox-Webhook: true
```

## Provider notes

### YooKassa

Production adapter supports create payment, redirect confirmation URL, webhook verification by status recheck/IP allow-list, manual recheck and refund.

### RoboKassa

Production adapter supports redirect URL and ResultURL signature verification. Manual recheck/refund remain fail-closed until Partner API credentials and endpoint mapping are configured.

### YooMoney

Production quickpay adapter supports redirect URL and notification HMAC verification. Manual recheck/refund remain fail-closed because the quickpay notification setup does not provide a universal refund/recheck API without Wallet/API OAuth setup.

### CloudPayments

CloudPayments uses a payment widget. The adapter intentionally does not fabricate a provider-hosted payment page. Configure `ExtraSettingsJson.hostedCheckoutUrl` to point to your own hosted widget page. Webhook verification uses `Content-HMAC` / `X-Content-HMAC` with the API secret. Manual recheck/refund are fail-closed in the widget adapter until API method mapping is configured.

Example `ExtraSettingsJson`:

```json
{
  "hostedCheckoutUrl": "https://pay.example.com/cloudpayments-widget"
}
```

### T-Bank Acquiring

T-Bank adapter uses `/v2/Init`, body token generation from root fields plus password, `/v2/GetState` for manual recheck and `/v2/Cancel` for refund/cancel. `ShopId` is `TerminalKey`; `SecretKey` is terminal password.

Optional `ExtraSettingsJson`:

```json
{
  "notificationUrl": "https://api.example.com/api/webhooks/payments/tbank"
}
```

### Prodamus

Prodamus adapter builds a payform URL and verifies `Sign` header on notifications. `ApiBaseUrl` is the payform URL, `SecretKey` is the payform secret, `WebhookSecret` may repeat the same secret or contain a dedicated notification secret. Recheck/refund are fail-closed until merchant-specific REST API credentials are configured.

Optional `ExtraSettingsJson`:

```json
{
  "sys": "your-integration-code",
  "notificationUrl": "https://api.example.com/api/webhooks/payments/prodamus"
}
```

### Stripe

Stripe adapter creates Checkout Sessions and verifies webhooks via the `Stripe-Signature` header and endpoint secret. Manual recheck uses Checkout Session retrieval. Refund resolves `payment_intent` from stored payload or live session retrieval.

### PayPal

PayPal adapter creates Checkout Orders and verifies webhooks through PayPal's `verify-webhook-signature` endpoint. `ShopId` is client id, `SecretKey` is client secret, `WebhookSecret` is PayPal Webhook ID. Refund resolves capture id from stored payload or live order retrieval.

### Telegram Stars

Telegram Stars is handled in the Telegram Bot service via `sendInvoice`, `pre_checkout_query`, `answerPreCheckoutQuery`, and `successful_payment`. The generic `IPaymentProvider` remains fail-closed because Stars payments are update-driven through Telegram Bot API rather than browser redirect driven.

## Branch B stabilization notes

This branch keeps all newly added adapters fail-closed by default:

- CloudPayments cannot create a payment unless `ExtraSettingsJson.hostedCheckoutUrl` is configured. Unknown webhook events or missing amount/currency do not map to success.
- T-Bank rejects missing terminal credentials, invalid `Token`, unknown statuses and missing amounts. Webhook `OrderId` may be the internal payment attempt id and is validated by the orchestrator against both order id and payment id.
- Prodamus rejects missing notification secret and invalid signatures. Missing/unknown payment status does not map to success.
- Stripe requires `Stripe-Signature`, enforces timestamp tolerance and rejects completed events without amount/currency data.
- PayPal requires client credentials and PayPal Webhook ID. Verification API failure rejects the webhook; completed events without amount/currency data do not map to success.
- Telegram Stars remains fail-closed in the generic browser-redirect provider path; live handling stays in Telegram Bot update handlers.

Run `./scripts/validate-all.sh` before continuing to other branches. Branch B must not be considered production-ready until backend build/tests, EF drift check, frontend typecheck/build/tests and staging provider checks are green.
