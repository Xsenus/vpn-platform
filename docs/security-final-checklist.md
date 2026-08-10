# Security final checklist

Документ закрывает roadmap-пункт `P11-ACC-005` и фиксирует финальную локальную проверку безопасности перед решением о staging/production. Это не заменяет внешний аудит и live smoke на VPS, но закрепляет автоматические проверки, которые уже должны проходить в репозитории.

## Что проверяется

### Секреты и чувствительные данные

- `SecretScanTests` сканирует репозиторий на реальные токены Telegram, GitHub/GitLab, AWS, Google, Slack, OpenAI/Stripe-style ключи и PEM private keys.
- `SecurityHardeningMvpTests` проверяет защиту и ротацию секретов серверов, 3x-ui, SSH и платежных провайдеров.
- `ProvisioningSecretMaterializerTests` проверяет временные SSH-ключи для provisioning: файл удаляется после использования, путь и plaintext редактируются в логах.
- `GitHubSecretsAuditTests` проверяет, что workflow и `.github/github-secrets.audit.json` согласованы по именам секретов и не раскрывают значения.
- Админские ответы по платежным аккаунтам и automation snapshot возвращают только write-only флаги `HasSecretKey`/`HasWebhookSecret`, без `SecretKeyProtected`, `WebhookSecretProtected` и raw extra secrets.

Если секрет когда-либо был отправлен в чат, лог, issue, commit или скриншот, оператор обязан ротировать его до любого production-запуска. Это относится и к root-паролям VPS, токенам Telegram, платежным ключам, webhook secret, SSH private key и JWT/DataProtection ключам.

### Auth, сессии и права

- `AdminAuthorizationPolicyTests` проверяет role-based policy matrix: read-only не получает write/manage доступ, finance/support/operator разделены, admin/superadmin получают критичные политики.
- `SecurityFinalChecklistTests.Admin_Surface_Should_Keep_Policy_Authorization_And_No_Anonymous_Routes` отражением проходит все `VpnPlatform.Api.Controllers.Admin` контроллеры и проверяет:
  - у admin controller есть class-level `Authorize`;
  - нет `AllowAnonymous`;
  - все policy существуют в `AdminPolicies.PolicyRoles`;
  - write endpoints не остаются только на read-only policy.
- JWT middleware подключен в `Program.cs` в порядке `UseRateLimiter` -> `UseAuthentication` -> `UseAuthorization`.
- JWT bearer повторно проверяет активное состояние пользователя в БД на каждом запросе; admin deactivation атомарно отзывает refresh-сессии, а cabinet/Telegram каналы прекращают доступ к чувствительным операциям.

### Headers, CORS и rate limits

- `SecurityHeadersTests` проверяет API security headers: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, CSP и production HSTS.
- `frontend/nginx.security.conf` используется всеми frontend Dockerfile и содержит CSP, HSTS, `X-Frame-Options` и SPA fallback.
- `RateLimitingSecurityTests` проверяет политики `auth-sensitive`, `public-checkout`, `webhook`, их лимиты и подключение `app.UseRateLimiter()`.
- Production CORS остается allow-list based через `Cors:AllowedOrigins`.

### Webhooks, платежи и idempotency

- `PaymentWebhookIdempotencyContractTests` проверяет повтор webhook и fallback payload hash для каждого платежного провайдера.
- Webhook controllers покрыты rate limit policy `webhook`.
- Webhook headers проходят редактирование чувствительных значений; production не принимает local sandbox headers для реальных аккаунтов.
- Проверка платежных провайдеров отделена от live-денег: локальный режим использует sandbox seed и управляемые webhook-сценарии.

### Validation gates

Обязательные локальные entry points должны включать secret scan:

- `scripts/validate-all.sh`;
- `scripts/validate-backend.sh`;
- `scripts/validate-backend.ps1`;
- `scripts/check-validation-safety.sh`.

`scan-secrets.ps1` и `scan-secrets.sh` исключают generated artifacts (`test-results`, `.playwright-artifacts-*`, `playwright-report`, `dist`, `coverage`), чтобы E2E-прогоны не ломали проверку исчезающими временными файлами.

Финальная проверка этого пункта использует:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "SecurityFinalChecklistTests|SecretScanTests|SecurityHardeningMvpTests|AdminAuthorizationPolicyTests|RateLimitingSecurityTests|SecurityHeadersTests|GitHubSecretsAuditTests|ProvisioningSecretMaterializerTests|PaymentWebhookIdempotencyContractTests|DocumentationEncodingTests|ReleaseDocumentationGuardTests|ReadmeDocumentationTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

## Результат 2026-08-04

- Security final checklist: OK.
- Admin anonymous routes: 0.
- Admin write endpoints без manage/write policy: 0.
- Secret scan: OK.
- Security headers: OK.
- Rate limits: OK.
- RBAC matrix: OK.
- GitHub secrets audit: OK.
- Webhook idempotency contract: OK.
- Fresh local SQLite smoke: OK.
- Backend full suite: 1112/1112.
- Payment webhook controller routes for all 8 providers: OK.
- Malformed enum/JSON write payloads fail with 400 and do not partially mutate persisted entities: OK.
- Subscription/VPN lifecycle commands fail closed without partial subscription mutation; historical server operations and linked scenario keys remain protected: OK.
- Refund provider calls use durable reservations and fail closed on concurrent, cancelled or locally uncommitted outcomes: OK.
- Payment initialization uses a durable reservation and order gate; paid intermediate orders and uncommitted local reservations do not call the provider: OK.
- Subscription activation compensates remote access after local credential save failure and preserves a `SyncRequired` reconciliation marker when cleanup fails: OK.
- Telegram `update_id` is durably claimed before side effects; concurrent/fresh/stale/cancelled processing paths are fail-closed and retryable: OK.
- Outbox payload is validated fail-closed; delivery errors are redacted and terminal failures are separated from pending health metrics: OK.
- Admin partial-role capability matrix and finance/support user-overview redaction: OK.
- Admin dashboard finance/support aggregates and payment/Telegram readiness checks are capability-redacted: OK.
- Admin audit finance/support/Telegram actions, entity types and JSON payload are capability-scoped before user filters: OK.
- VPN access enable/sync/reset caller cancellation and reset uncertainty persist safe audit/reconciliation state: OK.
- Access/refresh `session_version`, password reset/admin invalidation and stale rotation rejection: OK.
- Refresh token family isolation, stale-generation replay and legacy rotation chain handling: OK.
- Password reset sibling invalidation, explicit lifecycle reason and optimistic concurrency: OK.
- Cabinet auth hydration is single-flight for new and restored sessions under `React.StrictMode`: OK.
- Registration email unique race returns a controlled duplicate response without partial auth state; unrelated DB failures are not masked: OK.
- Password reset newest-generation-only boundary covers sequential reissue, concurrent issue/reset and explicit bootstrap password changes: OK.
- Refresh token revision rejects double rotation, revokes the winning family on reuse and gives logout/admin deactivation controlled retry paths: OK.
- Support conversation revision rejects stale mutations; internal messages fail closed and assignment requires active support capability: OK.
- Checkout claim transaction prevents cross-user order duplication and preserves terminal completed state: OK.
- Promo lifecycle validation, relational redemption limits and paid free-days snapshot fail closed across checkout, order and subscription activation: OK.
- Межпанельный VPN client migration использует только валидированный inbound-каталог, исключает unhealthy/full панели и сохраняет production-like 3x-ui evidence открытым: OK.
- Post-success migration state обновляет только публичные capacity counters и не выполняет второй fallible запрос после подтверждённого provider side effect: OK.
- Cabinet renewal retry использует уже созданный order ID и не повторяет mutation создания заказа после сбоя payment init: OK.
- Public checkout single-flight не дублирует claim/payment init, а request generation не принимает late payment response после logout: OK.
- Persisted public checkout допускает только bounded base64url token, нормализованное имя тарифа и payment provider из runtime allow-list; invalid state удаляется до API-запросов: OK.
- Public session hydration выполняет только одну rotating refresh операцию, не ротирует токен на transient `5xx` и инвалидирует delayed completion после logout: OK.
- Cabinet support принимает только актуальный thread response, инвалидирует delayed completion после logout и очищает private drafts/reset state: OK.
- Admin user/support detail requests принимают только актуальный selected/session response и не возвращают старые данные после logout: OK.
- Admin mutation completion, reload и form reset ограничены исходной session operation и отправленным snapshot; duplicate submit блокируется синхронно: OK.
- Public/cabinet mutation completion ограничен исходной session/unmount operation; duplicate auth/refresh/action events блокируются синхронно, а более новые drafts сохраняются: OK.
- Latest local release: `2026-08-10-admin-managed-configuration-crud-e2e`, версия `0.554.0`; критические admin mutations и полный CRUD управляемой конфигурации проверены на desktop/mobile, axe WCAG A/AA и best-practice gate без allow-list проверен на desktop и 320 px.
- Frontend tests: 112/112.
- Frontend typecheck/build: OK.
- Frontend dependency audit: `0 vulnerabilities`; React 19.2.8 и React Router 8.3.0 проверены на Node.js 22.22.0.

## Ограничения

Этот checklist подтверждает локальную security baseline. Перед production все равно нужны:

- live VPS smoke с реальным доменом, HTTPS и production-like environment;
- ротация любых секретов, которые могли быть раскрыты вне secret manager;
- проверка backup/restore на staging PostgreSQL;
- проверка реальных sandbox-кабинетов платежных провайдеров;
- отдельное решение release decision в `P11-ACC-007`.
