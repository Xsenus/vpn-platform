# Шаблон результатов проверки

Use this template to record real CI/server validation results. Do not paste real secrets, tokens, payment credentials, SSH credentials, or raw private URLs.

## Validation context

| Field | Value |
|---|---|
| Date/time |  |
| Operator |  |
| Git commit SHA |  |
| Branch |  |
| Machine/runner |  |
| OS |  |
| Notes |  |

## Toolchain versions

Paste sanitized command output or concise version strings.

```bash
dotnet --info
docker --version
docker compose version
node --version
npm --version
```

| Tool | Result |
|---|---|
| .NET SDK |  |
| Docker Engine |  |
| Docker Compose plugin |  |
| Node.js |  |
| npm |  |

## Backend gate

Command:

```bash
./scripts/check-validation-safety.sh
./scripts/validate-backend.sh
```

| Step | Status | Notes |
|---|---:|---|
| dotnet restore |  |  |
| dotnet build |  |  |
| dotnet test |  |  |
| dotnet tool restore |  |  |
| EF migrations list --no-connect |  |  |
| EF drift check |  |  |

Backend logs/test artifacts:

```text

```

## Frontend gate

Commands:

```bash
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=high
cd ..
```

| Step | Status | Notes |
|---|---:|---|
| npm ci |  |  |
| typecheck |  |  |
| build |  |  |
| tests |  |  |
| audit high severity |  |  |

Frontend output summary:

```text

```

## Docker gate

Command:

```bash
./scripts/validate-docker.sh
```

| Step | Status | Notes |
|---|---:|---|
| docker compose config |  |  |
| docker compose build |  |  |
| docker compose up |  |  |
| backend-api /health/live |  |  |
| backend-api /health/ready |  |  |
| backend-api /metrics |  |  |
| telegram-bot /health/live |  |  |
| telegram-bot /health/ready |  |  |
| postgres health |  |  |
| redis health |  |  |
| rabbitmq health |  |  |
| fatal log scan |  |  |

Compose status:

```text

```

Runtime logs summary:

```text
backend-api:

backend-api hosted workers:

telegram-bot:

```

## GitHub Actions gate

Workflow run URL or ID:

```text

```

| Job | Status | Notes |
|---|---:|---|
| backend |  |  |
| frontend |  |  |
| docker-build |  |  |
| static-and-provisioning |  |  |
| docker-runtime-smoke, if run manually |  |  |

## Security checks

| Check | Status | Notes |
|---|---:|---|
| No live Telegram calls |  |  |
| No live payment calls |  |  |
| No live 3x-ui calls |  |  |
| No live VPS/provisioning calls |  |  |
| No secrets in logs |  |  |
| Admin bootstrap disabled or controlled |  |  |
| ReadOnly write denial covered by tests |  |  |
| Provider secrets not exposed by public endpoints |  |  |

## Final gate decision

| Decision | Value |
|---|---|
| Can Stage 3 start? | yes/no |
| Blockers |  |
| Follow-up owner |  |
| Follow-up date |  |

## Sandbox E2E gate

Command:

```bash
./scripts/check-validation-safety.sh

dotnet test backend/VpnPlatform.sln \
  --configuration Release \
  --no-build \
  --filter SandboxE2EScenariosMvpTests
```

| Scenario | Status | Notes |
|---|---:|---|
| Telegram purchase sandbox E2E |  |  |
| Duplicate callback idempotency |  |  |
| Duplicate webhook idempotency |  |  |
| Renewal sandbox E2E |  |  |
| Expiry disables access |  |  |
| Provider disable failure controlled |  |  |
| Own VPS dry-run success |  |  |
| Own VPS dry-run failure/support |  |  |
| Admin visibility after purchase |  |  |
| Admin visibility after own VPS |  |  |
| No secrets in admin DTOs |  |  |
| No secrets in provisioning logs/audit |  |  |
| Validation safety script/static checks |  |  |

Sandbox E2E output summary:

```text

```

Live-call safety observations:

```text
Telegram API calls: none/observed
Payment provider API calls: none/observed
3x-ui API calls: none/observed
SSH/Ansible/VPS calls: none/observed
```

## UI/UX MVP review

| Area | Status | Notes |
|---|---:|---|
| Admin dashboard/sidebar |  |  |
| Admin empty/loading/error states |  |  |
| Admin secret fields write-only |  |  |
| Admin dangerous actions confirmation |  |  |
| Admin VPN access copy/QR |  |  |
| Admin provisioning redacted logs |  |  |
| Cabinet subscriptions/access empty states |  |  |
| Cabinet VPN URI copy + QR SVG |  |  |
| Cabinet logout/refresh/password reset |  |  |
| Public tariff/provider loading/empty/error states |  |  |
| Public login/register/password reset |  |  |
| Telegram texts human-readable and no secret echo |  |  |

UI runtime observations:

```text

```
