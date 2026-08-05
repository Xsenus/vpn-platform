# Runbook проверки staging

This runbook is the Stage 2.2 validation gate. It verifies that the current codebase builds, tests, has no EF migration drift, and can start through Docker without live Telegram, payment, 3x-ui, or VPS calls.

Do not use this runbook to claim production readiness. Production validation still requires a real staging server, a sandbox payment account, a real Telegram bot token used deliberately, a test 3x-ui panel, backup/restore, rollback, monitoring, and a security review.


## CI validation package

A dedicated GitHub Actions workflow is available at `.github/workflows/staging-validation.yml`. It can be triggered on push, pull request, or manually through `workflow_dispatch`. The normal jobs run backend, frontend, Docker config/build, static config parsing, and provisioning runner checks. The heavier Docker runtime smoke is manual-only through the `run_runtime_smoke` workflow input.

Docker validation uses `docker-compose.validation.yml` together with `docker-compose.yml` to force safe runtime overrides: Telegram bot disabled, payment provider modes disabled, 3x-ui sandbox mode, no live panel credentials, and admin bootstrap disabled.

## 0. Preconditions

Required tools:

```bash
dotnet --info        # .NET SDK 9.x
node --version       # Node 22.x
npm --version
docker --version
docker compose version
git --version
curl --version
```

Recommended local environment:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export TelegramBot__Enabled=false
export Email__Mode=Disabled
export Vpn__X3Ui__Mode=Sandbox
export Payments__YooMoney__Mode=Disabled
export Payments__YooKassa__Mode=Disabled
export Payments__RoboKassa__Mode=Disabled
export Payments__TelegramStars__Mode=Disabled
```

These defaults are intentionally safe: Telegram long polling/webhook traffic is disabled, payment providers are disabled unless explicitly configured, and 3x-ui stays in sandbox mode.

## 1. Backend compile/test/EF gate

From repository root:

```bash
./scripts/check-validation-safety.sh
./scripts/validate-backend.sh
```

This script runs:

```bash
dotnet --info
dotnet restore backend/VpnPlatform.sln
dotnet build backend/VpnPlatform.sln --configuration Release --no-restore
dotnet test backend/VpnPlatform.sln --configuration Release --no-build
dotnet tool restore
dotnet ef migrations list \
  --project backend/src/VpnPlatform.Infrastructure \
  --startup-project backend/src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --no-connect
./scripts/check-ef-drift.sh
```

Expected result: every step exits with code `0`. Test results are written to `backend/TestResults/test-results.trx`.

If `dotnet ef migrations list --no-connect` fails because EF tooling is unavailable, first run:

```bash
cd backend
dotnet tool restore
cd ..
```

If `check-ef-drift.sh` fails, review the generated migration diff. Do not delete or ignore a real model drift; create a proper migration or fix the model/mapping.

## 2. Frontend regression gate

From repository root:

```bash
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
cd ..
```

Expected result:

- public-web typecheck passes;
- cabinet typecheck passes;
- admin-panel typecheck passes;
- all three apps build;
- frontend tests pass.

The public-web and cabinet payment selectors must load `/api/public/payments/providers`, show a loading state, show an empty/error state when no providers are available, and prevent checkout/renewal without a selected provider.

## 3. Docker compose/build/runtime gate

From repository root:

```bash
./scripts/validate-docker.sh
```

This script runs safe local defaults and does not enable live Telegram, payment, 3x-ui, or VPS actions. It runs:

```bash
docker compose -f docker-compose.yml -f docker-compose.validation.yml config
docker compose -f docker-compose.yml -f docker-compose.validation.yml build backend-api telegram-bot public-web cabinet admin-panel
docker compose -f docker-compose.yml -f docker-compose.validation.yml up -d postgres redis rabbitmq backend-api telegram-bot
docker compose ps
curl -fsS http://localhost:8080/health/live
curl -fsS http://localhost:8080/health/ready
curl -fsS http://localhost:8080/metrics
curl -fsS http://localhost:8081/health/live
curl -fsS http://localhost:8081/health/ready
docker compose exec -T postgres pg_isready -U "${POSTGRES_USER:-vpnplatform}" -d "${POSTGRES_DB:-vpnplatform}"
docker compose exec -T redis redis-cli ping
docker compose exec -T rabbitmq rabbitmq-diagnostics -q ping
docker compose logs --tail=250 backend-api telegram-bot
```

`scripts/validate-docker.sh` stores temporary curl/config/log artifacts in a per-run `mktemp` directory and removes those temporary curl/config/log artifacts on exit. `KEEP_STACK=1` keeps the Docker stack running for manual inspection, but it still removes the temporary files created only by the validation script.

Expected result:

- Compose config is valid;
- backend-api, telegram-bot, public-web, cabinet, and admin-panel images build;
- postgres, redis, rabbitmq, backend-api, and telegram-bot start;
- API `/health/live`, `/health/ready`, and `/metrics` return HTTP 2xx;
- Telegram bot service `/health/live` and `/health/ready` return HTTP 2xx on port `8081`;
- dependency health checks pass;
- runtime logs do not contain fatal startup failures.

By default the script tears the stack down at the end. To inspect the running containers after the gate:

```bash
KEEP_STACK=1 ./scripts/validate-docker.sh
```

Then clean up manually:

```bash
docker compose -f docker-compose.yml -f docker-compose.validation.yml down --remove-orphans
```

## 4. Admin user bootstrap for local smoke

For a local smoke only, create an admin user through controlled bootstrap env vars. Do not commit the password.

```bash
export AdminBootstrap__Enabled=true
export AdminBootstrap__Email=admin.local@example.test
export AdminBootstrap__Password='replace-with-a-local-password-at-least-16-chars'
export AdminBootstrap__DisplayName='Local Admin'
export AdminBootstrap__RolesCsv=SuperAdmin
```

Then start the API with migrations enabled:

```bash
export Database__ApplyMigrationsOnStartup=true
dotnet run --project backend/src/VpnPlatform.Api
```

Or run the Docker gate with `KEEP_STACK=1` and the same environment variables exported before the script. The application hashes the password at startup; the plaintext password must only live in the local shell environment.

## 5. Runtime endpoint checklist

API:

```bash
curl -f http://localhost:8080/health/live
curl -f http://localhost:8080/health/ready
curl -f http://localhost:8080/metrics
```

Telegram bot service:

```bash
curl -f http://localhost:8081/health/live
curl -f http://localhost:8081/health/ready
```

The Telegram bot service currently has minimal HTTP health endpoints in addition to webhook/long-polling workers. With `TelegramBot__Enabled=false`, it should start without contacting Telegram.

## 6. Security regression checklist

Before moving to Stage 3, confirm:

- ReadOnly cannot call write endpoints;
- SupportAgent cannot change payment/provider settings;
- FinanceManager cannot manage VPN/provisioning;
- public payment providers endpoint does not expose `SecretKeyProtected`, `WebhookSecretProtected`, or raw account entities;
- admin users endpoint does not expose `PasswordHash` or metadata secrets;
- disabled/unconfigured providers are hidden from Telegram callback payment flow;
- subscription expiry does not crash when VPN provider `DisableAccessAsync` fails;
- logs do not print protected provider secrets or Telegram bot token values.

The focused backend tests added in Stage 2.1 cover most of these checks, but they must pass under `dotnet test` before this gate is considered complete.

## 7. Minimum pass condition for moving to Stage 3

Stage 3 Telegram sales-flow work may start only when all items below are true:

- `./scripts/validate-backend.sh` passes;
- frontend `npm ci`, `npm run typecheck`, `npm run build`, and `npm run test` pass;
- `./scripts/validate-docker.sh` passes, or runtime blockers are documented with exact failures;
- there is no known critical security regression;
- no live Telegram, payment, 3x-ui, or VPS actions were performed unintentionally.


## 8. Sandbox E2E scenarios gate

After the generic backend/frontend/Docker gates, run the sandbox E2E suite to verify that the product flows are connected without live integrations.

```bash
./scripts/check-validation-safety.sh

dotnet test backend/VpnPlatform.sln \
  --configuration Release \
  --no-build \
  --filter SandboxE2EScenariosMvpTests
```

The sandbox E2E tests validate:

- Telegram purchase flow through sandbox payment webhook and sandbox VPN provider;
- duplicate callback/webhook idempotency;
- renewal flow with access update/re-enable;
- expiry flow with access disable/history/audit;
- provider disable failure handling;
- own-VPS dry-run flow with protected credentials, mock precheck/deploy, node/panel/inbound/access creation and redacted admin visibility;
- admin management visibility for tariffs, provider accounts and linked business entities;
- no raw secrets in admin DTO snapshots, provisioning logs or queued notification payloads.

These tests must not require Telegram API, payment provider APIs, a live 3x-ui panel, SSH, Ansible or VPS network access. If any sandbox E2E test attempts a live call, treat that as a validation-mode safety regression.

## 9. Sandbox E2E result interpretation

A green sandbox E2E suite confirms that business flows are connected in code and protected by deterministic mock providers. It does not replace:

- `dotnet restore/build/test` for the whole solution;
- EF migration/drift validation;
- Docker build/runtime smoke;
- a future approved staging test with dedicated test Telegram/payment/3x-ui/VPS resources.

Do not mark the project staging-ready or production-ready until those gates are also passed and documented.

## 10. UI/UX MVP validation gate

Stage 9 adds UI polish only. After the standard backend/frontend/Docker gates, review:

```bash
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=high
cd ..
```

Manual runtime UI checks after Docker smoke:

- admin sidebar navigation and dashboard cards render;
- admin payment provider secrets are write-only/configured flags only;
- admin dangerous actions require confirmation;
- provisioning logs are redacted and validation-mode warning is visible;
- cabinet shows empty states, VPN access URI copy button and QR SVG preview;
- public checkout handles loading, errors and no payment providers;
- public password reset form works only through safe validation flow;
- Telegram bot texts do not echo secrets and use clear Russian instructions.

A green UI build/test does not make the system staging-ready without backend, EF, Docker runtime and sandbox E2E validation.
