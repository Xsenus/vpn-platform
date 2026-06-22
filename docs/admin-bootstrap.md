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
  -ReadinessReportPath tmp\admin-vps-bootstrap-smoke-readiness-report.json `
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

Readiness gate перед reset-ом пишет sanitized `admin-vps-bootstrap-smoke-readiness-report.json` без пароля и connection string. Он должен пройти до `admin-bootstrap.ps1`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -ReportPath tmp\admin-vps-bootstrap-smoke-readiness-report.json -RequireReady
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-readiness.ps1
```

После успешного bootstrap+smoke wrapper пишет sanitized `admin-vps-bootstrap-smoke-report.json`. Отдельная проверка report дополнительно сверяет `releaseId` итогового bootstrap report с preflight и smoke reports:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-report.ps1 -ReportPath tmp\admin-vps-bootstrap-smoke-report.json -RequirePassed
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-evidence.ps1 -ReadinessReportPath tmp\admin-vps-bootstrap-smoke-readiness-report.json -BootstrapSmokeReportPath tmp\admin-vps-bootstrap-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-evidence-validator.ps1
```

Regression harness также покрывает `mismatched-readiness-report-path`, `mismatched-readiness-smoke-report-path`, `mismatched-readiness-preflight-report-path`, `preflight-generated-before-readiness`, `bad-smoke-route` и `bootstrap-generated-before-smoke-completed`, чтобы bootstrap+smoke evidence не принимал чужие readiness/smoke/preflight report paths, preflight report, созданный раньше readiness gate, устаревшие admin routes вне `docs/admin-vps-smoke-sections.json` и итоговый bootstrap report, созданный раньше завершения linked smoke. Valid-сценарий дополнительно проверяет, что success summary содержит `preflightReportPath` и `sectionsContractPath`.
Внутри этого flow `scripts/admin-vps-bootstrap-smoke.ps1` вычисляет latest release один раз и передает один `releaseId` в readiness gate, `scripts/admin-vps-smoke.ps1` и итоговый bootstrap smoke report. Browser smoke report пишет `smokeReportPath`, readiness report пишет `readinessReportPath`, итоговый bootstrap smoke report пишет `readinessReportPath` и `bootstrapSmokeReportPath`, а evidence validators сверяют эти self-link поля с фактическими readiness/smoke/bootstrap JSON. Standalone validators readiness и итогового bootstrap reports также сверяют `readinessReportPath`/`bootstrapSmokeReportPath` с фактическим `-ReportPath` до paired evidence validation. Bootstrap report validator дополнительно валидирует связанный readiness report, сверяет readiness `smokeReportPath`/`preflightReportPath`, `apiBaseUrl`, `adminWebUrl`, `environmentName`, `operator`, `adminEmail`, `releaseId`, `provider`, `passwordEnvName`, `localSqlite` и `confirmBootstrapReset` с readiness/preflight/browser smoke reports и требует порядок readiness `generatedAt` -> preflight `generatedAt` -> smoke `completedAt` -> bootstrap report `generatedAt`, чтобы нельзя было смешать bootstrap одного окружения, admin-аккаунта, режима БД или времени выполнения со smoke другого. Evidence validator отклоняет readiness/bootstrap пару с разным `releaseId`, а preflight report не проходит validation с пустым `releaseId`.

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
