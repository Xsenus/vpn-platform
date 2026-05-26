# Этап 2: усиление платёжного контура

Дата: 2026-04-29

## Scope

Этот срез доводит платежную архитектуру до проверяемого vertical slice:

- YooKassa — production-capable adapter with local sandbox mode;
- RoboKassa — redirect/payment URL + ResultURL signature verification;
- YooMoney — quickpay redirect/payment URL + HTTP notification HMAC verification;
- остальные providers присутствуют в enum/settings architecture, но fail-closed.

## Safety requirements implemented

- Anonymous public order/payment endpoints fail closed.
- Public checkout uses checkout session token; DB stores only token hash.
- Checkout session has TTL and can be claimed only once by a user-bound flow.
- Repeated claim by another user is rejected.
- Payment init is user-bound and reuses existing pending attempt for the same order/provider/account.
- Webhook raw payload is stored.
- Webhook headers are stored without Authorization/Cookie/Token secrets.
- Webhook processing validates payment id, provider, provider account, order, amount, currency, succeeded status and paid flag where provider exposes it.
- Duplicate webhook does not activate subscription twice.
- Duplicate refund with the same idempotency key returns existing refund.
- Unknown/wrong payment webhook is saved as rejected and does not activate anything.
- Local sandbox webhook headers are disabled in Production environment.

## YooKassa

Production-capable features:

- create payment via YooKassa API;
- confirmation URL redirect;
- status recheck;
- refund;
- webhook parse;
- webhook authenticity check by live status recheck and optional IP allow-list;
- local sandbox mode when account is Sandbox and credentials are empty, but only outside Production.

Required settings:

- `ShopId` — YooKassa shop id;
- `SecretKey` — YooKassa API secret;
- `ApiBaseUrl` — usually `https://api.yookassa.ru/v3`;
- `ReturnUrl` — cabinet/payment return URL;
- optional `UseWebhookIpAllowList` + `AllowedWebhookIpRangesCsv`.

Webhook URL:

```text
https://<api-domain>/api/webhooks/payments/yookassa
```

## RoboKassa

Implemented features:

- payment redirect URL generation;
- sandbox mode through `IsTest=1`;
- ResultURL parse;
- SignatureValue verification with Password #2;
- idempotent webhook processing;
- Robokassa-compatible `OK{InvId}` response.

Settings mapping:

- `ShopId` — `MerchantLogin`;
- `SecretKey` — Password #1;
- `WebhookSecret` — Password #2;
- `ApiBaseUrl` — default `https://auth.robokassa.ru/Merchant/Index.aspx`.

Manual recheck/refund are fail-closed in this adapter until XML/API credentials and exact operational policy are configured.

Webhook URL:

```text
https://<api-domain>/api/webhooks/payments/robokassa
```

## YooMoney

Implemented features:

- quickpay payment URL generation;
- notification parse;
- HMAC-SHA256 `sign` verification;
- idempotent webhook processing;
- local sandbox header only outside Production when account is Sandbox and webhook secret is empty.

Settings mapping:

- `ShopId` — receiver wallet/account;
- `SecretKey` — optional fallback secret for local/manual configuration;
- `WebhookSecret` — HTTP notification secret;
- `ApiBaseUrl` — default `https://yoomoney.ru/quickpay/confirm`.

Manual recheck/refund are fail-closed because they require a separate YooMoney Wallet/API OAuth flow and should not be simulated in production.

Webhook URL:

```text
https://<api-domain>/api/webhooks/payments/yoomoney
```

## Validation commands

```bash
cd backend
dotnet restore
dotnet build --no-restore
dotnet test --no-build

dotnet tool restore
dotnet ef migrations add __ModelDriftCheck \
  --project src/VpnPlatform.Infrastructure \
  --startup-project src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --output-dir Persistence/Migrations

git diff --exit-code -- src/VpnPlatform.Infrastructure/Persistence/Migrations
rm -f src/VpnPlatform.Infrastructure/Persistence/Migrations/*__ModelDriftCheck*.cs
```
