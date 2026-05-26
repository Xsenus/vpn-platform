# Runbook sandbox E2E

This runbook describes the non-live end-to-end validation package for the VPN Platform. It is intended to prove that the main business flows are wired together before any real Telegram bot, payment provider, 3x-ui panel, SSH session, Ansible deploy, or VPS is used.

## 1. What sandbox E2E validates

The sandbox E2E suite covers these flows with deterministic in-memory data and fake providers:

1. **Telegram purchase**: Telegram update → `/start` → tariff selection → payment provider selection → order → payment attempt → sandbox webhook → active subscription → sandbox VPN access → queued Telegram notification → admin visibility.
2. **Payment idempotency**: duplicate provider callback reuses the pending payment attempt, and duplicate webhook does not create duplicate subscriptions, access credentials, or provider clients.
3. **Renewal**: active subscription → Telegram renewal → renewal order/payment → subscription expiry extended → existing access updated/re-enabled → queued Telegram notification.
4. **Expiry lifecycle**: expired subscription → lifecycle service → provider disable through mock provider → access history/audit → admin-visible disabled or error state.
5. **Own VPS dry-run**: Telegram own-VPS state machine → protected/redacted credential → provisioning run → mock precheck/deploy → mock node/panel/inbound/access → queued notification → redacted admin provisioning view.
6. **Admin visibility**: dashboard, user overview, orders, payments, subscriptions, access credentials, provisioning runs, payment provider accounts and Telegram bot settings expose linked entities without raw secrets.
7. **Validation safety**: validation compose/workflow/env keep live integrations off and reject obvious live secret/token/private-key patterns.

## 2. Live calls that are prohibited in sandbox E2E

Do not use or enable the following during sandbox E2E:

- live Telegram Bot API calls;
- live payment provider checkout/webhook/recheck/refund calls;
- live 3x-ui/x-ui panel calls;
- live SSH sessions;
- live Ansible provisioning;
- live VPS deploys;
- real provider, Telegram, SSH, or 3x-ui secrets.

All tests must use fake payment webhooks, mock Telegram updates, sandbox/mock VPN providers, and mock provisioning executors.

## 3. Required safe-mode environment values

For validation runs, the effective environment must keep these values:

```bash
TelegramBot__Enabled=false
AdminBootstrap__Enabled=false
Auth__PasswordReset__ReturnTokenForValidation=false
Provisioning__LiveExecutionEnabled=false
Provisioning__AllowLiveDeploy=false
Vpn__X3Ui__Mode=Sandbox
X3UI_BASE_URL=
X3UI_USERNAME=
X3UI_PASSWORD=
Payments__YooMoney__Mode=Disabled
Payments__YooKassa__Mode=Disabled
Payments__RoboKassa__Mode=Disabled
Payments__TelegramStars__Mode=Disabled
Payments__CloudPayments__Mode=Disabled
Payments__TBankAcquiring__Mode=Disabled
Payments__Prodamus__Mode=Disabled
Payments__Stripe__Mode=Disabled
Payments__PayPal__Mode=Disabled
```

Run the safety gate before backend or Docker validation:

```bash
./scripts/check-validation-safety.sh
```

## 4. Running backend sandbox E2E tests

На машине с .NET 9 SDK:

```bash
./scripts/check-validation-safety.sh

dotnet restore backend/VpnPlatform.sln
dotnet build backend/VpnPlatform.sln --configuration Release --no-restore
dotnet test backend/VpnPlatform.sln --configuration Release --no-build --filter SandboxE2EScenariosMvpTests
```

To run all backend tests:

```bash
dotnet test backend/VpnPlatform.sln --configuration Release --no-build
```

The sandbox E2E tests are expected to run without PostgreSQL, Redis, RabbitMQ, Telegram, payment, 3x-ui, SSH, or Ansible network access.

## 5. Running frontend checks

Use Node 22 and npm 10+:

```bash
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=high
cd ..
```

Frontend tests include API-client endpoint regression checks and source-level safety checks for provider selectors, QR/access rendering, admin provisioning redaction, write-only secret fields, and dangerous-action confirmations.

## 6. Running Docker validation

Docker validation uses the validation override file and must not require live secrets:

```bash
./scripts/validate-docker.sh
```

For manual inspection:

```bash
KEEP_STACK=1 ./scripts/validate-docker.sh

docker compose -f docker-compose.yml -f docker-compose.validation.yml ps
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs backend-api --tail=200
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs backend-api --tail=200
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs telegram-bot --tail=200
```

Health endpoints:

```bash
curl -f http://localhost:8080/health/live
curl -f http://localhost:8080/health/ready
curl -f http://localhost:8080/metrics
curl -f http://localhost:8081/health/live
curl -f http://localhost:8081/health/ready
```

## 7. Interpreting results

A sandbox E2E pass means:

- critical business flows are connected in code;
- idempotency guards prevent duplicate subscriptions/access credentials in mock flows;
- admin APIs expose linked operational data without raw secrets;
- validation mode is configured to avoid live side effects.

It does **not** mean the project is staging-ready or production-ready. Staging readiness still requires a green backend build/test gate, EF migration/drift validation, Docker build/runtime smoke, and a controlled sandbox/staging integration run.

## 8. Future staging live test requirements

Before any live/staging integration test:

1. Complete and document backend/Docker validation results.
2. Use dedicated test Telegram bot token, not a production bot.
3. Use sandbox payment provider credentials only.
4. Use a disposable test 3x-ui panel/VPS.
5. Keep production customer data and real provider credentials out of the run.
6. Confirm provisioning live flags explicitly and use an approved runbook.
7. Capture logs, health endpoints, rollback steps and validation results.
