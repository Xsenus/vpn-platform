# Smoke-проверка админки на VPS

Этот документ описывает безопасную проверку production/staging админки под реальным admin-аккаунтом. Локальные Playwright-тесты проверяют интерфейс на моках и локальном API, но не доказывают, что `/admin/` на VPS доступен, авторизация работает, а каждый раздел открывается без белого экрана, JS-ошибок и 401/403 после входа.

## Черновик отчета

Создайте fail-closed отчет перед ручной или браузерной проверкой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-admin-vps-smoke-report.ps1 -OutputPath tmp\admin-vps-smoke-report.json -ApiBaseUrl https://api.example.test -AdminWebUrl https://example.test/admin/ -AdminEmail owner@example.com -EnvironmentName staging -Operator local-test
```

Скрипт берет `docs/admin-vps-smoke-report.template.json`, подставляет latest release из раздела "Что нового", URL окружения, оператора и `smokeReportPath`, выставляет все разделы в `blocked`, не перезаписывает существующий файл без `-Force` и сразу запускает валидатор.

## Что проверять

Перед отметкой `passed` нужно подтвердить:

- production admin-аккаунт создан или восстановлен безопасным bootstrap/reset механизмом;
- страница админки открывается по HTTPS;
- логин проходит под рабочим admin-аккаунтом;
- после логина нет 401/403 на admin API;
- browser console не содержит `console.error` и `pageerror`;
- каждый раздел открывается и показывает осмысленное состояние.

Обязательные разделы:

- `dashboard`
- `users`
- `payments`
- `tariffs`
- `subscriptions`
- `vpn`
- `nodes`
- `panels`
- `support`
- `audit`
- `bot`
- `releases`
- `faq`
- `content`
- `scenarios`
- `provisioning`

Список обязательных разделов хранится в `docs/admin-vps-smoke-sections.json`. Перед реальным VPS smoke или изменением админки проверьте, что manifest, template, validator и Playwright specs синхронизированы:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-sections-contract.ps1
```

Локальная regression-проверка section contract, включая tamper-сценарии `duplicate-section`, `bad-route`, `template-missing-section`, `browser-spec-no-manifest` и `all-screens-missing-section`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-sections-contract.ps1
```

## Валидатор

Обычная структурная проверка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-report.ps1 -ReportPath tmp\admin-vps-smoke-report.json
```

Production gate для заполненного отчета:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-report.ps1 -ReportPath tmp\admin-vps-smoke-report.json -RequireAllPassed
```

`-RequireAllPassed` должен падать на черновике, потому что все разделы изначально находятся в статусе `blocked`. Перед закрытием `P0-ADMIN-002` нужно заменить статусы на `passed`, выставить `loaded=true`, `httpStatus=200`, заполнить общие флаги и приложить безопасные real evidence. Acceptance mode также отклоняет placeholder evidence вроде `TODO`, `Not checked yet`, `safe screenshot name` и шаблонных browser smoke notes.

`scripts/validate-admin-vps-smoke-report.ps1` читает `docs/admin-vps-smoke-sections.json` и сверяет не только наличие обязательных разделов, но и route каждого раздела с manifest.

Smoke report содержит `smokeReportPath`; validator сверяет это поле с фактически проверяемым `-ReportPath`, чтобы standalone evidence archive не мог подменить browser smoke JSON.

Локальная regression-проверка validator для заполненного admin VPS smoke report, включая happy path и tamper-сценарии `mismatched-smoke-report-path`, `bad-http-status`, `bad-route`, `placeholder-evidence`, `failed-status`, `missing-section`, `false-gate`, `secret-marker`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-report-validator.ps1
```

## Preflight перед live-smoke

Перед реальным запуском можно проверить готовность параметров без подключения к админке и без вывода секрета:

```powershell
$env:ADMIN_VPS_SMOKE_API_BASE_URL="https://api.example.test"
$env:ADMIN_VPS_SMOKE_ADMIN_WEB_URL="https://example.test/admin/"
$env:ADMIN_VPS_SMOKE_ADMIN_EMAIL="owner@example.com"
$env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD="<temporary-admin-password>"

powershell -ExecutionPolicy Bypass -File scripts\admin-vps-smoke-preflight.ps1 `
  -SmokeReportPath tmp\admin-vps-smoke-report.json `
  -PreflightReportPath tmp\admin-vps-smoke-preflight-report.json `
  -EnvironmentName staging `
  -Operator operator-name
```

Preflight report сохраняет `preflightReportPath` вместе с `smokeReportPath`, чтобы evidence validator мог standalone проверить, что архив содержит именно тот preflight JSON, который был связан с browser smoke.

`scripts/admin-vps-smoke-preflight.ps1` проверяет `ADMIN_VPS_SMOKE_API_BASE_URL`, `ADMIN_VPS_SMOKE_ADMIN_WEB_URL`, `ADMIN_VPS_SMOKE_ADMIN_EMAIL`, наличие `ADMIN_VPS_SMOKE_ADMIN_PASSWORD` в process env, каталог `frontend`, команду `e2e:admin-vps-smoke`, browser runner, validator smoke-отчета и validator preflight-отчета. Если `-ReleaseId` не передан, preflight report получает latest release из раздела "Что нового". Пароль не принимается параметром и не записывается в отчет: в JSON сохраняется только `passwordEnvPresent`, а в консоль выводится `present [hidden]`. Если `readyForLiveSmoke=false`, реальный smoke запускать нельзя.

Preflight автоматически валидирует JSON через `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady`. Отдельная проверка уже созданного отчета:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-preflight-report.ps1 -ReportPath tmp\admin-vps-smoke-preflight-report.json -RequireReady
```

Локальная regression-проверка validator, включая happy path и tamper-сценарии `empty-release-id`, `bad-ready-flag`, `failed-check`, `missing-check`, `duplicate-check`, `secret-marker`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-preflight-validator.ps1
```

## Единая команда live-smoke

Для реального VPS-прогона используйте fail-closed wrapper, который сначала запускает preflight и валидирует sanitized preflight report, а затем выполняет browser smoke только при готовых параметрах:

```powershell
$env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD="<temporary-admin-password>"

powershell -ExecutionPolicy Bypass -File scripts\admin-vps-smoke.ps1 `
  -ApiBaseUrl https://api.example.test `
  -AdminWebUrl https://example.test/admin/ `
  -AdminEmail owner@example.com `
  -SmokeReportPath tmp\admin-vps-smoke-report.json `
  -PreflightReportPath tmp\admin-vps-smoke-preflight-report.json `
  -EnvironmentName staging `
  -Operator operator-name `
  -AccountBootstrapChecked
```

`scripts/admin-vps-smoke.ps1` не принимает пароль параметром, печатает только `Password: [hidden]`, вычисляет release id один раз, запускает `scripts/admin-vps-smoke-preflight.ps1 -RequirePassword` с этим же release id, затем `scripts/admin-vps-browser-smoke.ps1 -RequireAllPassed` с тем же значением. Browser smoke report пишет sanitized `adminEmail`, а парный evidence validator сверяет его с preflight report, чтобы нельзя было смешать preflight и smoke от разных admin-аккаунтов. Если preflight report не проходит `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady`, browser smoke не стартует. Для закрытия `P0-ADMIN-001`/`P0-ADMIN-002` нужен именно реальный VPS report без секретов, cookies, bearer-токенов и screenshots с приватными данными.

Локальная regression-проверка wrapper, включая fail-closed сценарии `missing-password`, `bad-api-url` и `missing-frontend`, доказывает, что browser smoke не стартует до valid preflight, smoke report не создается, пароль не попадает в stdout/stderr, а preflight report получает непустой release id еще до отказа browser smoke:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-flow-wrapper.ps1
```

После успешного preflight+browser smoke парный evidence validator сверяет, что оба отчета относятся к одному запуску: URL, environment, operator, path smoke-отчета, непустой release id и порядок дат не расходятся. Порядок времени должен быть preflight `generatedAt` -> smoke `startedAt` -> smoke `completedAt`; smoke не может стартовать раньше preflight. `scripts/admin-vps-smoke.ps1` запускает этот validator автоматически, но его можно выполнить отдельно:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-evidence.ps1 `
  -PreflightReportPath tmp\admin-vps-smoke-preflight-report.json `
  -SmokeReportPath tmp\admin-vps-smoke-report.json
```

Локальная regression-проверка evidence validator:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-evidence-validator.ps1
```

Regression harness покрывает `missing-preflight-release-id` и `smoke-started-before-preflight`: preflight/smoke evidence не должно проходить без release id в preflight report или с smoke, стартовавшим раньше preflight.

Regression harness также покрывает `mismatched-preflight-report-path`, `mismatched-expected-preflight-sha256`, `smoke-completed-before-started` и valid expected SHA256: preflight/smoke evidence не должно принимать preflight report, если поле `preflightReportPath` указывает не на фактически проверяемый preflight JSON, не должно принимать smoke report с `completedAt` раньше `startedAt` и должно fail-closed отклонять bundle при неверном expected SHA256. Valid-сценарий проверяет, что success summary содержит `adminEmail`, `operator`, `preflightReportId`, `smokeReportId`, `preflightReportPath`, `sectionsContractPath`, `preflightReportSha256`, `smokeReportSha256`, `preflightGeneratedAt`, `smokeStartedAt`, `smokeCompletedAt`, `preflightToSmokeSeconds`, `smokeDurationSeconds`, `sections`, `passed`, `failed`, `blocked`, `skipped`, а валидатор принимает `ExpectedPreflightReportSha256`/`ExpectedSmokeReportSha256`.

## Bootstrap + live-smoke

Если нужно в одном проходе восстановить production admin-аккаунт и сразу доказать вход в `/admin/`, используйте wrapper `scripts/admin-vps-bootstrap-smoke.ps1`. Он вычисляет release id один раз для readiness/smoke/bootstrap evidence, запускает `scripts/admin-bootstrap.ps1`, передает пароль в smoke только через process env `ADMIN_VPS_SMOKE_ADMIN_PASSWORD`, затем запускает `scripts/admin-vps-smoke.ps1 -AccountBootstrapChecked`.

```powershell
$env:ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<db-password>"
$env:ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD="<temporary-admin-password-at-least-16-chars>"

powershell -ExecutionPolicy Bypass -File scripts\admin-vps-bootstrap-smoke.ps1 `
  -ApiBaseUrl https://api.example.test `
  -AdminWebUrl https://example.test/admin/ `
  -AdminEmail owner@example.com `
  -ReadinessReportPath tmp\admin-vps-bootstrap-smoke-readiness-report.json `
  -BootstrapSmokeReportPath tmp\admin-vps-bootstrap-smoke-report.json `
  -EnvironmentName Production `
  -Operator operator-name `
  -Provider Postgres `
  -ApplyMigrations `
  -ConfirmBootstrapReset
```

Локальная SQLite-проверка доказывает, что admin-учетка создана CLI bootstrap-ом, а затем API стартует с `AdminBootstrap__Enabled=false` и вход в админку все равно проходит:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-bootstrap-smoke.ps1
```

Regression wrapper-а проверяет fail-closed сценарии до запуска smoke: нет пароля, нет `-ConfirmBootstrapReset`, нет connection string для не-локальной БД и `-DryRun`, при котором smoke не стартует:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-wrapper.ps1
```

Перед reset-ом wrapper запускает readiness gate и пишет sanitized report без пароля и connection string:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -ReportPath tmp\admin-vps-bootstrap-smoke-readiness-report.json -RequireReady
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-readiness.ps1
```

Успешный bootstrap+smoke проход дополнительно пишет sanitized report и проверяет его через validator. Readiness report содержит `readinessReportPath`, а итоговый bootstrap report содержит `readinessReportPath` и `bootstrapSmokeReportPath`, чтобы архив evidence можно было проверить standalone. Readiness validator сверяет `readinessReportPath` с фактическим `-ReportPath`; bootstrap report validator сверяет `bootstrapSmokeReportPath` с фактическим `-ReportPath`, валидирует связанный readiness report и сверяет `apiBaseUrl`, `adminWebUrl`, `environmentName`, `operator`, `adminEmail`, `releaseId`, `provider`, `passwordEnvName`, `localSqlite` и `confirmBootstrapReset` итогового bootstrap report с readiness, preflight и smoke reports:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-report.ps1 -ReportPath tmp\admin-vps-bootstrap-smoke-report.json -RequirePassed
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-evidence.ps1 -ReadinessReportPath tmp\admin-vps-bootstrap-smoke-readiness-report.json -BootstrapSmokeReportPath tmp\admin-vps-bootstrap-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-evidence-validator.ps1
```

Regression harness также проверяет `mismatched-readiness-report-self-link`, `bad-smoke-route`, `mismatched-release-id`, `mismatched-readiness-report-path`, `mismatched-readiness-smoke-report-path`, `mismatched-readiness-preflight-report-path`, `preflight-generated-before-readiness`, `valid-expected-sha256`, `mismatched-expected-readiness-sha256`, `missing-bootstrap-readiness-report-link`, `mismatched-readiness-bootstrap-report-path`, `mismatched-readiness-provider`, `mismatched-readiness-password-env-name`, `mismatched-readiness-local-sqlite`, `mismatched-readiness-confirm-bootstrap-reset`, `mismatched-bootstrap-smoke-report-path`, `mismatched-bootstrap-admin-email`, `mismatched-smoke-release-id` и `bootstrap-generated-before-smoke-completed`: bootstrap evidence chain должен отклонять smoke report, если route раздела расходится с `docs/admin-vps-smoke-sections.json`, не должен принимать отсутствующий или чужой readiness/bootstrap/smoke/preflight report path, не должен смешивать readiness/bootstrap/preflight/smoke reports от разных admin/release/DB-mode evidence, должен отклонять неверные expected SHA256 fingerprints и не должен принимать preflight report, созданный раньше readiness gate, или итоговый bootstrap report, созданный раньше завершения linked smoke. Valid-сценарий проверяет, что success summary содержит `readinessReportId`, `bootstrapSmokeReportId`, `preflightReportId`, `smokeReportId`, SHA256 fingerprints `readinessReportSha256`/`bootstrapSmokeReportSha256`/`preflightReportSha256`/`smokeReportSha256`, опциональные expected SHA256 параметры `ExpectedReadinessReportSha256`/`ExpectedBootstrapSmokeReportSha256`/`ExpectedPreflightReportSha256`/`ExpectedSmokeReportSha256`, `apiBaseUrl`, `adminWebUrl`, `adminEmail`, `operator`, `passwordEnvName`, `passwordEnvPresent`, `passwordLengthOk`, `connectionStringPresent`, `applyMigrations`, `confirmBootstrapReset`, `bootstrapResetConfirmed`, `readyForBootstrapSmoke`, `bootstrapStatus`, `readinessGeneratedAt`, `preflightGeneratedAt`, `smokeStartedAt`, `smokeCompletedAt`, `bootstrapGeneratedAt`, `bootstrapCompletedAt`, `preflightToSmokeSeconds`, `smokeDurationSeconds`, `bootstrapDurationSeconds`, `readinessToBootstrapSeconds`, `preflightReportPath`, `sectionsContractPath` и счетчики sections/passed/failed/blocked/skipped.

## Браузерный live-smoke

Низкоуровневый browser runner можно запускать отдельно для диагностики, если preflight уже пройден и нужно повторить только Playwright smoke без пересоздания preflight report:

```powershell
$env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD="<temporary-admin-password>"

powershell -ExecutionPolicy Bypass -File scripts\admin-vps-browser-smoke.ps1 `
  -ApiBaseUrl https://api.example.test `
  -AdminWebUrl https://example.test/admin/ `
  -AdminEmail owner@example.com `
  -OutputPath tmp\admin-vps-smoke-report.json `
  -EnvironmentName staging `
  -Operator operator-name `
  -AccountBootstrapChecked `
  -RequireAllPassed
```

Browser runner печатает `Password: [hidden]`, запускает `npm run e2e:admin-vps-smoke` из `frontend`, обходит все обязательные вкладки админки, проверяет отсутствие `console.error`, `pageerror` и 401/403 после логина, затем валидирует JSON через `scripts/validate-admin-vps-smoke-report.ps1`. Без `-AccountBootstrapChecked` отчет останется неприемочным для `-RequireAllPassed`, даже если логин и вкладки прошли.

## Локальная проверка runner на SQLite

Перед VPS-прогоном можно проверить сам browser runner на временной локальной SQLite-БД:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
```

Скрипт поднимает API в окружении `Local`, создает временную SQLite-БД в `tmp/local-admin-vps-browser-smoke`, включает demo seed и admin bootstrap, запускает admin-panel через Vite с `VITE_API_BASE_URL` на временный API, выполняет `scripts/admin-vps-smoke.ps1`, затем останавливает процессы и удаляет временные файлы. Для диагностики можно добавить `-KeepArtifacts`.

## Что нельзя хранить

В отчете нельзя сохранять пароли, cookies, bearer-токены, private headers, `.env`, SSH-ключи, webhook secrets, raw provider payloads и скриншоты, где видны секреты.
