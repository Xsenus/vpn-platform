# Финальный runbook запуска и проверки

Документ закрывает roadmap-пункт `P11-ACC-006` и собирает актуальный порядок запуска, проверки, deploy и обновления changelog. Детальные руководства остаются в README, `docs/github-deployment.md`, `docs/post-deploy-smoke.md`, `docs/security-final-checklist.md` и `docs/PRODUCT_COMPLETION_ROADMAP.md`.

## 1. Локальный запуск без Docker

Требования:

- .NET SDK 9;
- Node.js 22.22+ и npm;
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

На 2026-08-09 локально подтверждено:

- backend full suite: 1113/1113;
- frontend tests: 116/116;
- API build: OK;
- frontend typecheck/build: OK;
- fresh local SQLite smoke: OK;
- browser console smoke: 114/114; responsive all-screens: 6/6 на 18 viewport-конфигурациях `305x568..2560x1440`;
- visual assets: local same-origin WebP decode/dimensions OK; representative desktop/mobile screenshots reviewed;
- frontend dependency audit: `0 vulnerabilities`; React 19.2.8 и React Router 8.3.0 проверены на Node.js 22.22.0;
- API operation boundary regression: malformed enum/JSON returns 400 without partial database mutations; payment webhooks cover all 8 provider routes; VPN provisioning remains fail-closed.
- page quality gate: public, cabinet and 17 admin screens pass landmark, duplicate ID, image alt and accessible-name checks plus axe WCAG A/AA and best-practice rules on desktop and 320 px.
- subscription migration and archived-node boundaries: source/target/duplicate validation, migration item/audit persistence and fail-closed archived mode-actions are covered by SQLite tests.
- subscription/VPN consistency: provider failures leave subscription status and dates unchanged; server deletion preserves health/migration history; scenario key renames are guarded.
- refund consistency: provider call выполняется после durable reservation; concurrent duplicate, unresolved state, final commit failure и cancellation покрыты SQLite/fault-injection regression.
- payment init consistency: order gate, pre-provider reservation, concurrent duplicate, paid intermediate states и remote outcome recovery покрыты SQLite/fault-injection regression.
- subscription activation consistency: remote create компенсируется после local credential save failure; cleanup uncertainty сохраняет `SyncRequired`, cancellation пробрасывается после durable retry-state.
- Telegram ingress consistency: update reservation предшествует side effects; fresh lease возвращает retryable 503, failed/stale update восстанавливается, long-polling не теряет offset.
- Outbox consistency: enqueue дедуплицируется по event identity, dispatcher использует conditional claim, stale lease, backoff/dead-letter и не подтверждает malformed/unsupported payload как успешный.
- Email delivery consistency: SMTP worker использует conditional claim/stale lease/backoff, reset-код хранится только protected, а admin API возвращает маскированную диагностику без payload.
- Provisioning consistency: worker использует conditional claim и lease, stale execution требует явного operator retry, runner ограничен timeout, active run нельзя отменить или повторить поверх внешнего deploy.
- Subscription lifecycle consistency: expiration сначала отключает VPN-доступ, затем меняет статус; provider failure сохраняет `GracePeriod`, lease/backoff и retry diagnostics, а batch workers изолируют ошибки отдельных записей.
- Panel sync consistency: частичный unique index сериализует `Running` между инстансами, stale lease и worker snapshot восстанавливаются, health/sync diagnostics сохраняются redacted.
- Admin RBAC consistency: защищенная session capability matrix ограничивает разделы и команды partial roles, а user overview редактирует finance/support данные по backend read-policy.
- Admin dashboard consistency: finance/support aggregates и payment/Telegram readiness checks вычисляются только при соответствующих capabilities; frontend скрывает недоступные метрики и действия.
- Admin audit scope: finance/support/Telegram записи и JSON payload фильтруются по capabilities до Action/EntityType/Search; frontend показывает только разрешенные категории.
- VPN access lifecycle: enable/sync/reset пробрасывают caller cancellation после durable history/audit; enable/reset uncertainty сохраняется как `SyncRequired` для ручной сверки.
- Auth session lifecycle: access JWT и refresh rows содержат `session_version`; password reset, деактивация и изменяющий полномочия admin bootstrap повышают версию и отзывают старые refresh-сессии. JWT без claim после обновления требует refresh/relogin.
- Refresh replay lifecycle: новый login получает отдельный `FamilyId`, rotation наследует family, а reuse detection отзывает только эту цепочку; legacy NULL-family rows связываются через `ReplacedByTokenHash`.
- Password reset lifecycle: winning token получает `UsedAt`, остальные outstanding tokens — `InvalidatedAt`/reason; concurrency `Revision` отклоняет stale sibling commit и сохраняет единственный результат.
- Cabinet auth hydration: login/register/refresh и восстановленная сессия выполняют ровно один `loadAll`, включая `React.StrictMode` reload.
- Registration concurrency: unique email race возвращает `email_exists` без partial rows, а unrelated persistence failure остается видимым.
- Password reset generation: reissue закрывает старый code, concurrent issue/reset сериализуются per-user state revision, bootstrap password reset также invalidates codes.
- Refresh rotation concurrency: один source token не выпускает две active branches; stale rotation откатывается, reuse/logout закрывают family, admin deactivation повторяется после conflict.
- Support conversation concurrency: stale reply/status/note возвращают controlled conflict, pending inbound message переоткрывает active thread, assignment ограничен active `SupportWrite` users.
- Checkout claim atomicity: conditional session reservation, order creation и final link выполняются одной transaction; same-user race возвращает winner, другой user не создаёт orphan-order, completed status остаётся terminal.
- Cabinet renewal partial success: созданный order остается видимым при ошибке payment init, а retry использует тот же `orderId` без повторного создания заказа.
- Public authenticated checkout: session создается один раз, claim/payment-init имеет одного владельца, а partial success повторяет только оплату и игнорирует late response после logout.
- Public persisted checkout: browser state проходит bounded shape/token/provider validation и удаляется до авторизованных запросов при любом нарушении контракта.
- Public session hydration: StrictMode выполняет одну refresh-token rotation, transient profile failure сохраняет токены для ручного retry, logout инвалидирует late response.
- Public/cabinet mutation ownership исключает duplicate auth/refresh/action requests, stale completion после logout/unmount и потерю более нового support/reset draft.
- Provisioning runner timeout задаётся `Provisioning__ExecutionTimeoutSeconds` (по умолчанию `3600`, допустимо `1..86400` секунд); worker lease равна timeout плюс пять минут на завершение и сохранение результата.
- admin production bundle budget: `5` JS chunks, largest `219849`, total raw `512438`, gzip `137533`.
- unknown public route: доступное `404` recovery, desktop/mobile и 18 responsive viewport-конфигураций: OK.
- public route title/meta/focus: direct load, SPA navigation и browser Back desktop/mobile: OK.
- admin section metadata: hydration/login, deep-link, 17 sections и logout desktop/mobile: OK.
- admin section history/focus: tabs, Back/Forward и order-links desktop/mobile: OK.
- admin invalid hash canonical fallback: direct/runtime recovery, focus и Back desktop/mobile: OK.
- latest "Что нового": `2026-08-10-admin-invalid-hash-canonical-fallback`, версия `0.571.0`.
- roadmap progress: `584/604` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked.
- release decision: `staging-ready baseline`, подробнее в `docs/release-decision.md`.

## 8. Ограничения перед production

Проект нельзя считать production-ready только по локальным проверкам. До production нужны:

- live VPS smoke с реальным доменом и HTTPS;
- closed `P11-ACC-002` with real VPS/staging smoke evidence;
- ротация всех секретов, которые могли быть раскрыты вне secret manager;
- проверка backup/restore на staging PostgreSQL;
- реальные sandbox-кабинеты платежных провайдеров;
- реальная 3x-ui панель, inbound и выдача VPN-доступа;
- отдельный fail-closed `P11-ACC-008 Production readiness gate` перед сменой статуса на production-ready.
