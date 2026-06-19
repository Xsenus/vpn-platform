# Admin bootstrap/reset

Документ описывает безопасный one-shot запуск для создания или сброса администратора без открытия отдельного maintenance endpoint.

## Когда использовать

- Нужно восстановить доступ к админке на VPS.
- Нужно создать первого `SuperAdmin` после установки на чистую БД.
- Нужно сбросить пароль существующего администратора перед smoke-проверкой `/admin/`.

Команда не запускает HTTP-сервер и не печатает пароль в stdout. Backend-команда `admin-bootstrap` всегда сбрасывает пароль существующего пользователя только при явном CLI-запуске.

Wrapper сам выставляет `AdminBootstrap__Enabled=true`, `Database__Provider`, `Database__ApplyMigrationsOnStartup`, `Database__UseEnsureCreatedForLocalSqlite` и `ConnectionStrings__DefaultConnection`. В режиме `-LocalSqlite` используется `Data Source=data/vpnplatform-local.db`, если строка подключения не передана явно.

## Локальная SQLite-БД

```powershell
powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 `
  -LocalSqlite `
  -EnvironmentName Local `
  -Email admin@local.test `
  -Password "LocalAdminPassword123!" `
  -DisplayName "Local Admin" `
  -RolesCsv SuperAdmin
```

## Production/Postgres

Перед запуском подставьте реальную строку подключения через переменную окружения или параметр `-ConnectionString`.

```powershell
$env:ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<db-password>"

powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 `
  -EnvironmentName Production `
  -Provider Postgres `
  -ApplyMigrations `
  -Email owner@example.com `
  -Password "<temporary-password-at-least-16-chars>" `
  -DisplayName "Owner" `
  -RolesCsv SuperAdmin
```

## Bootstrap + smoke для VPS

Для одного операторского прохода можно сначала выполнить bootstrap/reset, а затем сразу прогнать admin VPS smoke под этим же аккаунтом. Пароль берется только из env, в выводе пишется `Password: [hidden]`, а для не-локальной БД нужен явный `-ConfirmBootstrapReset`.

```powershell
$env:ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<db-password>"
$env:ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD="<temporary-admin-password-at-least-16-chars>"

powershell -ExecutionPolicy Bypass -File scripts\admin-vps-bootstrap-smoke.ps1 `
  -ApiBaseUrl https://api.example.test `
  -AdminWebUrl https://example.test/admin/ `
  -AdminEmail owner@example.com `
  -BootstrapSmokeReportPath tmp\admin-vps-bootstrap-smoke-report.json `
  -EnvironmentName Production `
  -Operator operator-name `
  -Provider Postgres `
  -ApplyMigrations `
  -ConfirmBootstrapReset
```

Локальное доказательство этого flow без VPS и секретов:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-bootstrap-smoke.ps1
```

Fail-closed regression wrapper-а проверяет `missing-password`, `missing-confirm-bootstrap-reset`, `missing-connection-string` и `dry-run-no-smoke` без запуска browser smoke и без сохранения пароля:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-wrapper.ps1
```

После успешного bootstrap+smoke wrapper пишет sanitized `admin-vps-bootstrap-smoke-report.json`. Отдельная проверка report:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-report.ps1 -ReportPath tmp\admin-vps-bootstrap-smoke-report.json -RequirePassed
```

## Dry-run

Dry-run проверяет параметры и показывает только безопасные значения:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 `
  -LocalSqlite `
  -EnvironmentName Local `
  -Email admin@local.test `
  -Password "LocalAdminPassword123!" `
  -DryRun
```

Ожидаемый вывод содержит `Password: [hidden]` и `Dry-run mode: database was not changed.`

## Проверка после сброса

1. Запустите API и админку обычным способом.
2. Войдите в `/admin/` под указанным email и новым временным паролем.
3. Проверьте `GET /api/admin/dashboard/summary`.
4. Замените временный пароль на постоянный через штатный операционный процесс.
5. Зафиксируйте результат в `admin-vps-smoke-report.json` без паролей, cookie и raw headers.
