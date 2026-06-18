# Финальный runbook запуска и проверки

Документ закрывает roadmap-пункт `P11-ACC-006` и собирает актуальный порядок запуска, проверки, deploy и обновления changelog. Детальные руководства остаются в README, `docs/github-deployment.md`, `docs/post-deploy-smoke.md`, `docs/security-final-checklist.md` и `docs/PRODUCT_COMPLETION_ROADMAP.md`.

## 1. Локальный запуск без Docker

Требования:

- .NET SDK 9;
- Node.js 22+ и npm;
- PowerShell.

Первый запуск из корня репозитория:

```powershell
dotnet restore backend\VpnPlatform.sln
cd frontend
npm install
cd ..
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1
```

Открыть:

- API и Swagger: `http://127.0.0.1:8080/swagger`;
- публичный сайт: `http://127.0.0.1:5173`;
- личный кабинет: `http://127.0.0.1:5174`;
- админка: `http://127.0.0.1:5175`;
- локальный администратор: `admin@local.test` / `LocalAdminPassword123!`.

Остановка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
```

## 2. Полная локальная проверка

Минимальный gate перед коммитом:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Приемочный smoke на чистой локальной SQLite-БД:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
```

Browser smoke:

```powershell
npm run e2e:public --prefix frontend
npm run e2e:cabinet --prefix frontend
npm run e2e:admin --prefix frontend
npm run e2e:all-screens --prefix frontend
npm run e2e:mobile --prefix frontend
npm run e2e:console --prefix frontend
```

VPS/staging HTTP smoke runner:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18101 -AllowSandboxWebhook
```

Production readiness gate:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
```

На текущем baseline эта команда должна завершаться ошибкой: шаблон smoke-отчета содержит `blocked`, а live-блокеры еще открыты в roadmap и release decision.

Security gate:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "SecurityFinalChecklistTests|SecretScanTests|SecurityHardeningMvpTests|AdminAuthorizationPolicyTests|RateLimitingSecurityTests|SecurityHeadersTests|GitHubSecretsAuditTests|ProvisioningSecretMaterializerTests|PaymentWebhookIdempotencyContractTests"
```

Secret scan исключает generated/runtime artifacts: `tmp`, `test-results`, `.playwright-artifacts-*`, `playwright-report`, `dist`, `coverage`, `bin`, `obj`.

## 3. Docker и production-like проверка

Docker нужен для проверки PostgreSQL/Redis/RabbitMQ и production-like compose:

```powershell
docker compose up -d postgres redis rabbitmq prometheus grafana loki
docker compose up --build backend-api public-web cabinet admin-panel
```

Если Docker Desktop не запущен, используйте локальный SQLite-режим без Docker и не считайте compose-проверку выполненной.

## 4. Deploy на VPS

Основной workflow: `.github/workflows/deploy-vps.yml`.

Поддерживаются режимы:

- `auto` - GitHub Actions сам выбирает Docker или systemd по наличию Docker Compose на VPS;
- `docker` - принудительный Docker Compose deploy;
- `systemd` - deploy без Docker: self-contained API, nginx frontend и PostgreSQL на сервере.

Обязательные GitHub Actions secrets:

- `VPS_HOST`;
- `VPS_USER`;
- `VPS_SSH_KEY`;
- `PRODUCTION_ENV_FILE`.

Часто используемые optional secrets:

- `VPS_PORT`;
- `VPS_APP_DIR`;
- `VPS_DEPLOY_MODE`;
- `VITE_API_BASE_URL`;
- `VITE_PUBLIC_WEB_URL`;
- `POST_DEPLOY_API_URL`;
- `POST_DEPLOY_PUBLIC_WEB_URL`;
- `POST_DEPLOY_CABINET_WEB_URL`;
- `POST_DEPLOY_ADMIN_WEB_URL`.

Полная инструкция: `docs/github-deployment.md`.

## 5. Проверка после deploy

На VPS:

```bash
systemctl status vpn-platform-api --no-pager
systemctl status postgresql --no-pager
curl -fsS http://127.0.0.1:8080/health/live
curl -fsS http://127.0.0.1:8080/health/ready
curl -fsS http://127.0.0.1:8080/api/public/tariffs
curl -fsS http://127.0.0.1:8080/api/public/payments/providers
```

Post-deploy smoke:

```bash
API_BASE_URL=http://127.0.0.1:8080 \
PUBLIC_WEB_URL=http://127.0.0.1:5173 \
CABINET_WEB_URL=http://127.0.0.1:5174 \
ADMIN_WEB_URL=http://127.0.0.1:5175 \
scripts/post-deploy-smoke.sh
```

Для GitHub Actions smoke URLs можно переопределить через `POST_DEPLOY_*` secrets.

## 6. Документация и changelog

Если задача закрывает roadmap-пункт или меняет пользовательское поведение, обновляются:

- `docs/PRODUCT_COMPLETION_ROADMAP.md`;
- `TEST_RESULTS.md`;
- `CHANGELOG.md`;
- `backend/src/VpnPlatform.Api/AppReleases/releases.json`;
- профильное руководство из `docs/README.md`;
- README, если изменились команды запуска, проверки или текущий статус.

Guard:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FinalDocsChangelogTests|ReleaseDocumentationGuardTests|ReadmeDocumentationTests|DocumentationEncodingTests"
```

## 7. Текущий статус

На 2026-06-14 локально подтверждено:

- backend full suite: 511/511;
- frontend tests: 66/66;
- API build: OK;
- frontend typecheck/build: OK;
- fresh local SQLite smoke: OK;
- browser console smoke: 9/9;
- frontend audit: OK, `0 vulnerabilities`;
- latest "Что нового": `2026-06-18-production-evidence-aggregate-gate`, версия `0.125.0`.
- release decision: `staging-ready baseline`, подробнее в `docs/release-decision.md`.

## 8. Ограничения перед production

Проект нельзя считать production-ready только по локальным проверкам. До production нужны:

- live VPS smoke с реальным доменом и HTTPS;
- ротация всех секретов, которые могли быть раскрыты вне secret manager;
- проверка backup/restore на staging PostgreSQL;
- реальные sandbox-кабинеты платежных провайдеров;
- реальная 3x-ui панель, inbound и выдача VPN-доступа;
- отдельный fail-closed `P11-ACC-008 Production readiness gate` перед сменой статуса на production-ready.
