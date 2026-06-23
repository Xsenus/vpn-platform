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
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-bootstrap-smoke.ps1 -MaxEvidenceChainMinutes 120
```

Локальный fail-fast regression wrapper-а покрывает нечисловой CLI/env лимит, неверный CLI-лимит, неверный `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES`, лимит выше 1440 минут, нечисловые/вне диапазона `ApiPort`/`AdminPort` и совпадающие API/Admin порты:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-local-admin-vps-bootstrap-smoke-wrapper.ps1
```

Fail-closed regression wrapper-а проверяет `format-api-port`, `too-low-api-port`, `too-high-admin-port`, `same-api-admin-port`, `format-max-evidence-chain-minutes`, `format-env-max-evidence-chain-minutes`, `bad-max-evidence-chain-minutes`, `bad-env-max-evidence-chain-minutes`, `bad-api-url`, `bad-admin-web-url`, `bad-admin-email`, `same-report-paths`, `dry-run-default-operator`, `dry-run-default-environment`, `missing-password`, `missing-confirm-bootstrap-reset`, `missing-connection-string` и `dry-run-no-smoke` без запуска browser smoke и без сохранения пароля. Неверный CLI/env `MaxEvidenceChainMinutes`, неверные локальные порты, невалидные `ApiBaseUrl`/`AdminWebUrl`, невалидный `AdminEmail` и совпадающие report paths останавливаются до readiness report, bootstrap reset и smoke artifacts; пустой `Operator` нормализуется в `manual-operator`, пустой `EnvironmentName` - в `Production`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-wrapper.ps1
```

Regression includes `too-high-env-max-evidence-chain-minutes`, `bad-api-url`, `bad-admin-web-url`, `bad-admin-email`, `same-report-paths`, `dry-run-default-operator` and `dry-run-default-environment`: these scenarios must fail before readiness, bootstrap reset, preflight and smoke artifacts.

Readiness gate перед reset-ом пишет sanitized `admin-vps-bootstrap-smoke-readiness-report.json` без пароля и connection string. Валидатор fail-closed требует, чтобы `readyForBootstrapSmoke` совпадал с фактическим массивом `checks`, а `localSqlite=true` всегда сопровождался `provider=Sqlite`, даже если standalone-проверка запущена без `-RequireReady`. Он должен пройти до `admin-bootstrap.ps1`:

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

Standalone bootstrap evidence validator fail-fast отклоняет нечисловой `MaxEvidenceChainMinutes`, `<= 0` и `> 1440` едиными сообщениями до чтения readiness/bootstrap/preflight/smoke evidence; regression покрывает `format-max-evidence-chain-minutes`, `bad-max-evidence-chain-minutes` и `too-high-max-evidence-chain-minutes`.

Regression harness также покрывает `mismatched-readiness-report-path`, `mismatched-readiness-smoke-report-path`, `mismatched-readiness-preflight-report-path`, `preflight-generated-before-readiness`, `bad-smoke-route`, `valid-expected-sha256`, `mismatched-expected-readiness-sha256` и `bootstrap-generated-before-smoke-completed`, чтобы bootstrap+smoke evidence не принимал чужие readiness/smoke/preflight report paths, preflight report, созданный раньше readiness gate, устаревшие admin routes вне `docs/admin-vps-smoke-sections.json`, неверные expected SHA256 fingerprints и итоговый bootstrap report, созданный раньше завершения linked smoke. Valid-сценарий дополнительно проверяет, что success summary содержит `readinessReportId`, `bootstrapSmokeReportId`, `preflightReportId`, `smokeReportId`, SHA256 fingerprints `readinessReportSha256`/`bootstrapSmokeReportSha256`/`preflightReportSha256`/`smokeReportSha256`, опциональные expected SHA256 параметры `ExpectedReadinessReportSha256`/`ExpectedBootstrapSmokeReportSha256`/`ExpectedPreflightReportSha256`/`ExpectedSmokeReportSha256`, `apiBaseUrl`, `adminWebUrl`, `adminEmail`, `operator`, `passwordEnvName`, `passwordEnvPresent`, `passwordLengthOk`, `connectionStringPresent`, `applyMigrations`, `confirmBootstrapReset`, `bootstrapResetConfirmed`, `readyForBootstrapSmoke`, `bootstrapStatus`, `readinessGeneratedAt`, `preflightGeneratedAt`, `smokeStartedAt`, `smokeCompletedAt`, `bootstrapGeneratedAt`, `bootstrapCompletedAt`, `preflightToSmokeSeconds`, `smokeDurationSeconds`, `bootstrapDurationSeconds`, `readinessToBootstrapSeconds`, `readinessToPreflightSeconds`, `smokeToBootstrapSeconds`, `evidenceChainDurationSeconds`, `evidenceChronology`, `preflightReportPath`, `sectionsContractPath` и счетчики sections/passed/failed/blocked/skipped.
Внутри этого flow `scripts/admin-vps-bootstrap-smoke.ps1` вычисляет latest release один раз, fail-fast отклоняет ручной `ReleaseId`, которого нет в `backend/src/VpnPlatform.Api/AppReleases/releases.json`, и неподдерживаемый non-local `Provider` до readiness/bootstrap/smoke artifacts; `-LocalSqlite` всегда передает канонический provider `Sqlite`. Wrapper передает `ApiBaseUrl`, `AdminWebUrl`, `AdminEmail` и `AdminPasswordEnvName` в readiness gate, `scripts/admin-vps-smoke.ps1` и итоговый bootstrap smoke report без окружающих пробелов, а один `releaseId` - в readiness gate, `scripts/admin-vps-smoke.ps1` и итоговый bootstrap smoke report. Standalone readiness дополнительно нормализует регистронезависимые `Postgres`/`Sqlite` значения перед записью sanitized report, поэтому `postgres` из CLI/env сохраняется как `Postgres`, а неподдерживаемые значения все еще падают на `provider-supported`; пустой или whitespace `EnvironmentName` сохраняется как `Production`, а `ApiBaseUrl`, `AdminWebUrl`, `AdminEmail` и `AdminPasswordEnvName` записываются без окружающих пробелов. Browser smoke report пишет `smokeReportPath`, readiness report пишет `readinessReportPath`, итоговый bootstrap smoke report пишет `readinessReportPath` и `bootstrapSmokeReportPath`, а evidence validators сверяют эти self-link поля с фактическими readiness/smoke/bootstrap JSON. Standalone validators readiness и итогового bootstrap reports также сверяют `readinessReportPath`/`bootstrapSmokeReportPath` с фактическим `-ReportPath` до paired evidence validation. Bootstrap report validator дополнительно валидирует связанный readiness report, сверяет readiness `smokeReportPath`/`preflightReportPath`, `apiBaseUrl`, `adminWebUrl`, `environmentName`, `operator`, `adminEmail`, `releaseId`, `provider`, `passwordEnvName`, `localSqlite` и `confirmBootstrapReset` с readiness/preflight/browser smoke reports и требует порядок readiness `generatedAt` -> preflight `generatedAt` -> smoke `completedAt` -> bootstrap report `generatedAt`, чтобы нельзя было смешать bootstrap одного окружения, admin-аккаунта, режима БД или времени выполнения со smoke другого. Evidence validator отклоняет readiness/bootstrap пару с разным `releaseId`, а preflight report не проходит validation с пустым `releaseId`.

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
