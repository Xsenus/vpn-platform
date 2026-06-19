# Smoke-проверка админки на VPS

Этот документ описывает безопасную проверку production/staging админки под реальным admin-аккаунтом. Локальные Playwright-тесты проверяют интерфейс на моках и локальном API, но не доказывают, что `/admin/` на VPS доступен, авторизация работает, а каждый раздел открывается без белого экрана, JS-ошибок и 401/403 после входа.

## Черновик отчета

Создайте fail-closed отчет перед ручной или браузерной проверкой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-admin-vps-smoke-report.ps1 -OutputPath tmp\admin-vps-smoke-report.json -ApiBaseUrl https://api.example.test -AdminWebUrl https://example.test/admin/ -EnvironmentName staging -Operator local-test
```

Скрипт берет `docs/admin-vps-smoke-report.template.json`, подставляет latest release из раздела "Что нового", URL окружения и оператора, выставляет все разделы в `blocked`, не перезаписывает существующий файл без `-Force` и сразу запускает валидатор.

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

Локальная regression-проверка validator для заполненного admin VPS smoke report, включая happy path и tamper-сценарии `bad-http-status`, `placeholder-evidence`, `failed-status`, `missing-section`, `false-gate`, `secret-marker`:

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

`scripts/admin-vps-smoke-preflight.ps1` проверяет `ADMIN_VPS_SMOKE_API_BASE_URL`, `ADMIN_VPS_SMOKE_ADMIN_WEB_URL`, `ADMIN_VPS_SMOKE_ADMIN_EMAIL`, наличие `ADMIN_VPS_SMOKE_ADMIN_PASSWORD` в process env, каталог `frontend`, команду `e2e:admin-vps-smoke`, browser runner, validator smoke-отчета и validator preflight-отчета. Пароль не принимается параметром и не записывается в отчет: в JSON сохраняется только `passwordEnvPresent`, а в консоль выводится `present [hidden]`. Если `readyForLiveSmoke=false`, реальный smoke запускать нельзя.

Preflight автоматически валидирует JSON через `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady`. Отдельная проверка уже созданного отчета:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-preflight-report.ps1 -ReportPath tmp\admin-vps-smoke-preflight-report.json -RequireReady
```

Локальная regression-проверка validator, включая happy path и tamper-сценарии `bad-ready-flag`, `failed-check`, `missing-check`, `duplicate-check`, `secret-marker`:

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

`scripts/admin-vps-smoke.ps1` не принимает пароль параметром, печатает только `Password: [hidden]`, запускает `scripts/admin-vps-smoke-preflight.ps1 -RequirePassword`, затем `scripts/admin-vps-browser-smoke.ps1 -RequireAllPassed`. Если preflight report не проходит `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady`, browser smoke не стартует. Для закрытия `P0-ADMIN-001`/`P0-ADMIN-002` нужен именно реальный VPS report без секретов, cookies, bearer-токенов и screenshots с приватными данными.

Локальная regression-проверка wrapper, включая fail-closed сценарии `missing-password`, `bad-api-url` и `missing-frontend`, доказывает, что browser smoke не стартует до valid preflight, smoke report не создается, а пароль не попадает в stdout/stderr:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-flow-wrapper.ps1
```

После успешного preflight+browser smoke парный evidence validator сверяет, что оба отчета относятся к одному запуску: URL, environment, operator, path smoke-отчета, непустой release id и порядок дат не расходятся. `scripts/admin-vps-smoke.ps1` запускает этот validator автоматически, но его можно выполнить отдельно:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-evidence.ps1 `
  -PreflightReportPath tmp\admin-vps-smoke-preflight-report.json `
  -SmokeReportPath tmp\admin-vps-smoke-report.json
```

Локальная regression-проверка evidence validator:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-evidence-validator.ps1
```

## Bootstrap + live-smoke

Если нужно в одном проходе восстановить production admin-аккаунт и сразу доказать вход в `/admin/`, используйте wrapper `scripts/admin-vps-bootstrap-smoke.ps1`. Он запускает `scripts/admin-bootstrap.ps1`, передает пароль в smoke только через process env `ADMIN_VPS_SMOKE_ADMIN_PASSWORD`, затем запускает `scripts/admin-vps-smoke.ps1 -AccountBootstrapChecked`.

```powershell
$env:ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<db-password>"
$env:ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD="<temporary-admin-password-at-least-16-chars>"

powershell -ExecutionPolicy Bypass -File scripts\admin-vps-bootstrap-smoke.ps1 `
  -ApiBaseUrl https://api.example.test `
  -AdminWebUrl https://example.test/admin/ `
  -AdminEmail owner@example.com `
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
