# Финальный пакет проверки: этап 10

This document is the final validation package for running the Stage 10 gate on a CI runner or server that has the required toolchain. It does not enable live Telegram, payment provider, 3x-ui, SSH/Ansible, or VPS calls.

## Required environment

Run the gate only on a machine with:

- .NET 9 SDK
- Node.js 22
- npm 10+
- Docker Engine
- Docker Compose plugin
- Linux/macOS, or Windows with WSL2 and Docker Desktop WSL integration

Verify the toolchain first:

```bash
dotnet --info
docker --version
docker compose version
node --version
npm --version
```

## Safety gate

Before backend, frontend, Docker, or sandbox E2E checks, run:

```bash
./scripts/check-validation-safety.sh
```

Expected safety state:

- `TelegramBot__Enabled=false`
- all payment providers `Mode=Disabled`
- `Vpn__X3Ui__Mode=Sandbox`
- `X3UI_BASE_URL=` empty
- `X3UI_USERNAME=` empty
- `X3UI_PASSWORD=` empty
- `Provisioning__LiveExecutionEnabled=false`
- `Provisioning__AllowLiveDeploy=false`
- `AdminBootstrap__Enabled=false`
- `Auth__PasswordReset__ReturnTokenForValidation=false`
- no private keys, real tokens, or real secrets in `.env.example`, `docker-compose.validation.yml`, or `.github/workflows/staging-validation.yml`

## Backend validation

```bash
./scripts/validate-backend.sh
```

If a more granular run is needed:

```bash
dotnet restore backend/VpnPlatform.sln
dotnet build backend/VpnPlatform.sln --configuration Release
dotnet test backend/VpnPlatform.sln --configuration Release

dotnet tool restore

dotnet ef migrations list \
  --project backend/src/VpnPlatform.Infrastructure \
  --startup-project backend/src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --no-connect

./scripts/check-ef-drift.sh
```

Backend tests that must be preserved and executed include:

- `AdminAutomationMvpTests`
- `TelegramBotPurchaseFlowTests`
- `PaymentE2EHarnessTests`
- `VpnAccessAutomationMvpTests`
- `OwnVpsProvisioningMvpTests`
- `SecurityHardeningMvpTests`
- `SandboxE2EScenariosMvpTests`
- `AdminAuthorizationPolicyTests`
- `AdminUsersControllerTests`

## Frontend validation

```bash
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=high
cd ..
```

## Docker validation

```bash
./scripts/validate-docker.sh
```

For diagnostics with the stack left running:

```bash
KEEP_STACK=1 ./scripts/validate-docker.sh

docker compose -f docker-compose.yml -f docker-compose.validation.yml ps

docker compose -f docker-compose.yml -f docker-compose.validation.yml logs backend-api --tail=300
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs backend-api --tail=300
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs telegram-bot --tail=300
```

Runtime endpoints that must pass:

```bash
curl -f http://localhost:8080/health/live
curl -f http://localhost:8080/health/ready
curl -f http://localhost:8080/metrics
curl -f http://localhost:8081/health/live
curl -f http://localhost:8081/health/ready
```

Cleanup:

```bash
docker compose -f docker-compose.yml -f docker-compose.validation.yml down --remove-orphans
```

## Sandbox E2E validation

After backend build/test succeeds, run the sandbox E2E tests explicitly:

```bash
dotnet test backend/VpnPlatform.sln \
  --configuration Release \
  --filter SandboxE2EScenariosMvpTests
```

The sandbox E2E suite must confirm:

1. Telegram purchase sandbox E2E.
2. Payment idempotency.
3. Renewal E2E.
4. Expiry disables access.
5. Own VPS dry-run success.
6. Own VPS failure/support/retry.
7. Admin visibility.
8. Validation safety.

## Security validation checklist

Confirm that no API/admin/frontend path returns or renders:

- `PasswordHash`
- raw Telegram bot token
- raw payment provider protected secrets
- raw SSH password/private key
- raw x3-ui password
- raw `VpnNode.PanelPassword`
- unredacted provisioning logs containing credential-like values

## Staging-ready criteria

The project can be called staging-ready only if all of the following are confirmed:

- validation safety passed
- backend restore passed
- backend build passed
- backend tests passed
- EF migrations/drift passed, or a non-blocking EF reason is documented
- frontend typecheck/build/tests passed
- high-severity npm audit gate passed
- Docker compose config passed
- Docker compose build passed
- Docker compose up smoke passed
- API `/health/live` passed
- API `/health/ready` passed
- API `/metrics` passed
- TelegramBot `/health/live` passed
- TelegramBot `/health/ready` passed
- sandbox E2E tests passed
- no known critical security leak
- no live integrations were triggered accidentally

If any item is not confirmed, do not mark the project staging-ready.

## Current environment limitation observed during Stage 10

In the current execution environment, `dotnet` and `docker` are not installed. Therefore backend compile/test, EF drift, Docker build/runtime smoke, runtime health endpoints, runtime logs, and sandbox E2E execution cannot be confirmed here. The available frontend/static/safety checks passed in this environment, but that is not enough for staging readiness.
