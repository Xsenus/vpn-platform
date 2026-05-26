# Этап 2: платёжная архитектура и YooKassa

## Scope

This phase introduces a modular payment subsystem and implements YooKassa as the first production-capable provider.

Implemented now:

- `PaymentProviderAccount` and encrypted provider credentials.
- `PaymentProviderSetting` for future key/value provider settings.
- `PaymentWebhookEvent` with raw payload, redacted headers, payload hash, verification state and idempotency marker.
- `Refund` and `PaymentReceipt` tables.
- Public checkout sessions instead of anonymous public orders.
- YooKassa payment creation with redirect confirmation URL.
- YooKassa webhook parsing and authenticity verification by status recheck plus optional source IP allow-list.
- YooKassa manual status recheck.
- YooKassa refund API.
- Admin APIs and minimal admin UI for provider account settings, webhook events and refunds.
- Cabinet/public-web payment status and redirect flow.

Other providers are registered as disabled/unsupported adapters. They fail closed and must not be enabled until their provider-specific adapters are implemented.

## Secure checkout flow

Public-web no longer creates anonymous orders directly.

1. `POST /api/public/checkout-sessions` creates a short-lived checkout session.
2. User logs in or registers.
3. `POST /api/me/checkout-sessions/{token}/claim` binds the session to the user and creates the order.
4. `POST /api/me/orders/{orderId}/payments/YooKassa/init` creates the provider payment.
5. User follows the YooKassa confirmation URL.
6. YooKassa sends webhook to `/api/webhooks/payments/yookassa`.
7. Backend verifies webhook authenticity and applies status idempotently.
8. A succeeded payment activates or renews the subscription once.

## YooKassa configuration

Create a payment provider account in admin-panel or via API:

```json
{
  "provider": "YooKassa",
  "mode": "Sandbox",
  "name": "yookassa-sandbox",
  "publicName": "YooKassa",
  "isEnabled": true,
  "isDefault": true,
  "shopId": "<YooKassa shopId>",
  "apiBaseUrl": "https://api.yookassa.ru/v3",
  "returnUrl": "https://example.com/account",
  "secretKey": "<YooKassa secret key>",
  "webhookSecret": null,
  "useWebhookIpAllowList": true,
  "allowedWebhookIpRangesCsv": "185.71.76.0/27,185.71.77.0/27,77.75.153.0/25,77.75.156.11,77.75.156.35,77.75.154.128/25,2a02:5180::/32",
  "extraSettingsJson": "{}"
}
```

For local sandbox development without real YooKassa credentials, create a YooKassa account with `Mode=Sandbox`, empty `ShopId`, empty `SecretKey`, and `IsEnabled=true`. This enables only local simulated redirects and requires test webhooks to send `X-YooKassa-Sandbox-Webhook: true`.

Production mode requires `ShopId` and `SecretKey`. Secrets are encrypted at rest through `ISecretProtector` and must be supplied through admin UI/API only; they are not returned by read APIs.

## Manual webhook smoke test

After creating an order and initializing payment in local sandbox, send a test webhook:

```bash
curl -i -X POST http://localhost:8080/api/webhooks/payments/yookassa \
  -H 'Content-Type: application/json' \
  -H 'X-YooKassa-Sandbox-Webhook: true' \
  -d '{
    "type":"notification",
    "event":"payment.succeeded",
    "object":{
      "id":"yk_sandbox_<paymentAttemptGuidWithoutDashes>",
      "status":"succeeded",
      "paid":true,
      "amount":{"value":"490.00","currency":"RUB"},
      "metadata":{}
    }
  }'
```

The second identical webhook should be acknowledged as already processed and must not create a second subscription activation.

## Tests added

Backend tests added in `backend/tests/VpnPlatform.UnitTests`:

- `YooKassaPaymentTests` — provider status mapping, refund status mapping, webhook parsing, local sandbox webhook verification positive/negative cases.
- `PaymentWebhookProcessingTests` — local sandbox order → payment init → webhook → subscription activation, duplicate webhook idempotency, invalid webhook rejection.

Эти тесты требуют .NET 9 SDK и запускаются командой:

```bash
cd backend
dotnet test
```

## Migration drift check

Запускайте на машине с .NET 9 SDK:

```bash
cd backend
dotnet tool restore
dotnet ef migrations add __ModelDriftCheck \
  --project src/VpnPlatform.Infrastructure \
  --startup-project src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --output-dir Persistence/Migrations

git diff --exit-code -- src/VpnPlatform.Infrastructure/Persistence/Migrations
rm -f src/VpnPlatform.Infrastructure/Persistence/Migrations/*__ModelDriftCheck*.cs
```

If the generated migration has operations, the snapshot is out of sync with `ApplicationDbContext`.
