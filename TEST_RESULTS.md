# Результаты проверок

Дата проверки: 2026-05-25.

## Проверка 2026-06-19: admin VPS bootstrap smoke readiness

Что проверялось:

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` пишет sanitized readiness report до `admin-bootstrap.ps1`.
- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -RequireReady` проверяет URL, email, provider, readiness flags, `passwordEnvPresent`, `passwordLengthOk`, `confirmBootstrapReset`, `connectionStringPresent`, обязательные checks и absence of secret markers.
- `scripts/admin-vps-bootstrap-smoke.ps1` запускает readiness gate перед reset-ом и не пишет пароль/connection string в report.
- `scripts/local-admin-vps-bootstrap-smoke.ps1` проверяет readiness/bootstrap/smoke wrapper на временной SQLite-БД с `AdminBootstrap__Enabled=false` после начального CLI bootstrap.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-bootstrap-smoke-readiness`, версия `0.196.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-readiness.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminBootstrapCliScriptTests|AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-bootstrap-smoke.ps1 -KeepArtifacts
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -ReportPath tmp\local-admin-vps-bootstrap-smoke\admin-vps-bootstrap-smoke-readiness-report.json -RequireReady
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-report.ps1 -ReportPath tmp\local-admin-vps-bootstrap-smoke\admin-vps-bootstrap-smoke-report.json -RequirePassed
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-wrapper.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
changed/new files strict UTF-8 without BOM check
```

Результат:

- Admin VPS bootstrap smoke readiness regression: OK.
- `AdminBootstrapCliScriptTests`: `8/8`.
- Targeted release/docs suite: `38/38`.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness report valid, readiness/bootstrap reports UTF-8 without BOM, bootstrap smoke report valid, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: `588/588`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings, 555 files scanned.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, 23 files.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS bootstrap smoke report

Что проверялось:

- `scripts/admin-vps-bootstrap-smoke.ps1` после успешного smoke пишет sanitized `admin-vps-bootstrap-smoke-report.json`.
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` проверяет URL, даты, reset flags, `passwordEnvPresent`, absence of secret markers и связку preflight/smoke через `validate-admin-vps-smoke-evidence.ps1`.
- `scripts/local-admin-vps-bootstrap-smoke.ps1` проверяет сам bootstrap+smoke wrapper на временной SQLite-БД с `AdminBootstrap__Enabled=false` после начального CLI bootstrap.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-bootstrap-smoke-report`, версия `0.195.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminBootstrapCliScriptTests|AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-bootstrap-smoke.ps1 -KeepArtifacts
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-bootstrap-smoke-report.ps1 -ReportPath tmp\local-admin-vps-bootstrap-smoke\admin-vps-bootstrap-smoke-report.json -RequirePassed
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-wrapper.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
changed/new files strict UTF-8 without BOM check
```

Результат:

- `AdminBootstrapCliScriptTests`: `7/7`.
- Targeted release/docs suite: `37/37`.
- Local CLI bootstrap admin smoke на SQLite: OK, bootstrap smoke report valid, bootstrap report UTF-8 without BOM, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Admin VPS bootstrap smoke wrapper regression: OK, `missing-password`, `missing-confirm-bootstrap-reset`, `missing-connection-string`, `dry-run-no-smoke`.
- Backend full suite: `587/587`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings, 552 files scanned.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, 21 file.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS bootstrap smoke wrapper regression

Что проверялось:

- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` запускает `scripts/admin-vps-bootstrap-smoke.ps1` в fail-closed сценариях до browser smoke.
- Проверяются `missing-password`, `missing-confirm-bootstrap-reset`, `missing-connection-string` и `dry-run-no-smoke`.
- Проверяется, что smoke/preflight artifacts не создаются, browser smoke не стартует, пароль не попадает в stdout/stderr.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-bootstrap-smoke-wrapper-regression`, версия `0.194.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-bootstrap-smoke-wrapper.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminBootstrapCliScriptTests|AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-bootstrap-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- Admin VPS bootstrap smoke wrapper regression: OK, tested scenarios `4/4`.
- `AdminBootstrapCliScriptTests`: `6/6`.
- Targeted release/docs suite: `36/36`.
- Local CLI bootstrap admin smoke на SQLite: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: `586/586`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings, files scanned `551`.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, files checked `19`.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS bootstrap smoke wrapper

Что проверялось:

- `scripts/admin-vps-bootstrap-smoke.ps1` запускает `scripts/admin-bootstrap.ps1`, требует `-ConfirmBootstrapReset` для не-локальной БД и затем выполняет `scripts/admin-vps-smoke.ps1 -AccountBootstrapChecked`.
- Пароль берется из `ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD`, в выводе остается только `Password: [hidden]`, а smoke получает пароль через process env `ADMIN_VPS_SMOKE_ADMIN_PASSWORD`.
- `scripts/local-admin-vps-bootstrap-smoke.ps1` проверяет flow на временной SQLite-БД: CLI bootstrap создает admin, API стартует с `AdminBootstrap__Enabled=false`, затем admin smoke проходит под созданным аккаунтом.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-bootstrap-smoke-wrapper`, версия `0.193.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-bootstrap-smoke.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminBootstrapCliScriptTests|AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- Local CLI bootstrap admin smoke на SQLite: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- `AdminBootstrapCliScriptTests`: `5/5`.
- Targeted release/docs suite: `35/35`.
- Backend full suite: `585/585`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings, files scanned `550`.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, files checked `19`.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS smoke evidence validator

Что проверялось:

- `scripts/validate-admin-vps-smoke-evidence.ps1` запускает preflight validator с `-RequireReady`, smoke report validator с `-RequireAllPassed` и сверяет связь отчетов.
- Проверяются `apiBaseUrl`, `adminWebUrl`, `environmentName`, `operator`, `smokeReportPath`, непустой `releaseId` и порядок дат.
- `scripts/admin-vps-smoke.ps1` теперь запускает evidence validator после browser smoke.
- Regression покрывает `mismatched-api-url`, `mismatched-smoke-report-path`, `mismatched-release-id`, `preflight-after-smoke`, `failed-smoke-report`.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-smoke-evidence-validator`, версия `0.192.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-evidence-validator.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1 -KeepArtifacts
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-evidence.ps1 -PreflightReportPath tmp\local-admin-vps-browser-smoke\admin-vps-smoke-preflight-report.json -SmokeReportPath tmp\local-admin-vps-browser-smoke\admin-vps-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- Admin VPS smoke evidence validator regression: OK, tested failures `5/5`.
- `AdminVpsSmokeReportTests`: `14/14`.
- Targeted release/docs suite: `30/30`.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: `583/583`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS smoke flow wrapper regression

Что проверялось:

- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` запускает `scripts/admin-vps-smoke.ps1` в fail-closed сценариях до browser smoke.
- Проверяются `missing-password`, `bad-api-url`, `missing-frontend`, отсутствие запуска Playwright/browser smoke, отсутствие smoke report после failed preflight и отсутствие пароля в stdout/stderr.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-smoke-flow-wrapper-regression`, версия `0.191.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-flow-wrapper.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- Admin VPS smoke flow wrapper regression: OK, tested failures `3/3`.
- `AdminVpsSmokeReportTests`: `13/13`.
- Targeted release/docs suite: `29/29`.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: `582/582`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS smoke flow wrapper

Что проверялось:

- `scripts/admin-vps-smoke.ps1` запускает `admin-vps-smoke-preflight.ps1 -RequirePassword` перед `admin-vps-browser-smoke.ps1 -RequireAllPassed`.
- Пароль берется только из `ADMIN_VPS_SMOKE_ADMIN_PASSWORD`, не принимается параметром и печатается как `Password: [hidden]`.
- `scripts/local-admin-vps-browser-smoke.ps1` проверяет новый wrapper на временной SQLite-БД.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-smoke-flow-wrapper`, версия `0.190.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- `AdminVpsSmokeReportTests`: `12/12`.
- Targeted release/docs suite: `28/28`.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: `581/581`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS smoke report validator regression

Что проверялось:

- `scripts/test-admin-vps-smoke-report-validator.ps1` создает synthetic passed admin VPS smoke report, запускает validator happy path и проверяет fail-closed tamper-сценарии.
- Проверяются `bad-http-status`, `placeholder-evidence`, `failed-status`, `missing-section`, `false-gate`, `secret-marker`.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-smoke-report-validator-regression`, версия `0.189.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-report-validator.ps1
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- `AdminVpsSmokeReportTests`: `11/11`.
- Targeted release/docs suite: `27/27`.
- Admin VPS smoke report validator regression: OK, tested failures `6/6`.
- Local SQLite admin browser smoke: OK, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: `580/580`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS smoke preflight validator regression

Что проверялось:

- `scripts/test-admin-vps-smoke-preflight-validator.ps1` создает валидный preflight report, запускает validator happy path и проверяет fail-closed tamper-сценарии.
- Проверяются `bad-ready-flag`, `failed-check`, `missing-check`, `duplicate-check`, `secret-marker`.
- Harness контролирует, что тестовый `ADMIN_VPS_SMOKE_ADMIN_PASSWORD` не попадает в JSON artifacts.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-smoke-preflight-validator-regression`, версия `0.188.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\test-admin-vps-smoke-preflight-validator.ps1
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- `AdminVpsSmokeReportTests`: `10/10`.
- Targeted release/docs suite: `26/26`.
- Admin VPS smoke preflight validator regression: OK, tested failures `5/5`, JSON без секрета.
- Local SQLite admin browser smoke: OK, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: `579/579`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS smoke preflight validator

Что проверялось:

- `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady` fail-closed проверяет preflight evidence JSON: обязательные поля, URL, email, readiness flags, checks, дубли и forbidden secret markers.
- `scripts/admin-vps-smoke-preflight.ps1` запускает validator preflight-отчета перед разрешением live smoke.
- `docs/admin-vps-smoke.md` описывает отдельную проверку preflight report.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-smoke-preflight-validator`, версия `0.187.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
$env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD="LocalAdminPassword123!"
powershell -ExecutionPolicy Bypass -File scripts\admin-vps-smoke-preflight.ps1 -ApiBaseUrl http://127.0.0.1:18201 -AdminWebUrl http://127.0.0.1:18205/admin/ -AdminEmail fresh-admin@example.test -SmokeReportPath tmp\admin-vps-smoke-report.json -PreflightReportPath tmp\admin-vps-smoke-preflight-report.json -EnvironmentName Local -Operator local-test -RequirePassword
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-preflight-report.ps1 -ReportPath tmp\admin-vps-smoke-preflight-report.json -RequireReady
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- `AdminVpsSmokeReportTests`: `9/9`.
- Targeted release/docs suite: `25/25`.
- Admin VPS smoke preflight validator: OK, `readyForLiveSmoke=true`, password output hidden, JSON без секрета.
- Local SQLite admin browser smoke: OK, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: `578/578`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS smoke preflight

Что проверялось:

- `scripts/admin-vps-smoke-preflight.ps1` проверяет live URL, admin email, наличие `ADMIN_VPS_SMOKE_ADMIN_PASSWORD`, frontend runner, npm command и validator перед реальным VPS smoke.
- Preflight пишет sanitized JSON report с `readyForLiveSmoke` и `passwordEnvPresent`, но не принимает пароль параметром и не выводит секрет.
- `docs/admin-vps-smoke.md` описывает preflight перед `scripts/admin-vps-browser-smoke.ps1`.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-smoke-preflight`, версия `0.186.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
$env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD="LocalAdminPassword123!"
powershell -ExecutionPolicy Bypass -File scripts\admin-vps-smoke-preflight.ps1 -ApiBaseUrl http://127.0.0.1:18201 -AdminWebUrl http://127.0.0.1:18205/admin/ -AdminEmail fresh-admin@example.test -SmokeReportPath tmp\admin-vps-smoke-report.json -PreflightReportPath tmp\admin-vps-smoke-preflight-report.json -EnvironmentName Local -Operator local-test -RequirePassword
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- `AdminVpsSmokeReportTests`: `8/8`.
- Targeted release/docs suite: `24/24`.
- Admin VPS smoke preflight: OK, `readyForLiveSmoke=true`, password output hidden.
- Local SQLite admin browser smoke: OK, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: `577/577`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS smoke acceptance evidence

Что проверялось:

- `scripts/validate-admin-vps-smoke-report.ps1 -RequireAllPassed` требует успешный `httpStatus` для каждой секции админки.
- Acceptance mode отклоняет placeholder evidence: `TODO`, `Not checked yet`, `safe screenshot name`, `browser smoke note`.
- `docs/admin-vps-smoke.md` описывает требование real evidence для закрытия `P0-ADMIN-002`.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-smoke-acceptance-evidence`, версия `0.185.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
git diff --check
```

Результат:

- `AdminVpsSmokeReportTests`: `7/7`.
- Targeted release/docs suite: `23/23`.
- Local SQLite admin browser smoke: OK, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: `576/576`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: local admin VPS browser smoke runner

Что проверялось:

- `scripts/local-admin-vps-browser-smoke.ps1` поднимает временную SQLite-БД, API и admin-panel без Docker.
- Скрипт включает local admin bootstrap, запускает `scripts/admin-vps-browser-smoke.ps1 -RequireAllPassed` и валидирует sanitized `admin-vps-smoke-report.json`.
- Cleanup останавливает дерево процессов, поэтому дочерний Vite `node.exe` не оставляет порт занятым.
- Раздел "Что нового" получил релиз `2026-06-19-local-admin-vps-browser-smoke`, версия `0.184.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests"
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1 -KeepArtifacts
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-report.ps1 -ReportPath tmp\local-admin-vps-browser-smoke\admin-vps-smoke-report.json -RequireAllPassed
powershell -ExecutionPolicy Bypass -File scripts\local-admin-vps-browser-smoke.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npx playwright test --config=playwright.vps-smoke.config.ts --list
git diff --check
```

Результат:

- `AdminVpsSmokeReportTests`: `6/6`.
- Local SQLite admin browser smoke: OK, Playwright `1/1`, report validator `16 passed`.
- Local smoke cleanup: OK, временные artifacts удаляются без `-KeepArtifacts`.
- Targeted release/docs suite: `22/22`.
- Backend full suite: `575/575`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Playwright admin VPS smoke test discovery: OK, найден 1 test в проекте `admin-vps-smoke`.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin VPS browser smoke runner

Что проверялось:

- `frontend/e2e/admin-vps-smoke.spec.ts` готовит live-smoke входа в `/admin/` и обхода всех обязательных разделов админки.
- `frontend/playwright.vps-smoke.config.ts` запускает этот smoke отдельно от локальных E2E, без webServer, trace, video и screenshots.
- `scripts/admin-vps-browser-smoke.ps1` принимает URL и email, пароль берет только из `ADMIN_VPS_SMOKE_ADMIN_PASSWORD`, печатает `Password: [hidden]` и валидирует отчет.
- Раздел "Что нового" получил релиз `2026-06-19-admin-vps-browser-smoke`, версия `0.183.0`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` не закрывались: реальный VPS bootstrap/login smoke не выполнялся из-за отсутствия live URL/секретов в рабочей папке.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run e2e:console --prefix frontend
npx playwright test --config=playwright.vps-smoke.config.ts --list
git diff --check
```

Результат:

- `AdminVpsSmokeReportTests`: `5/5`.
- Targeted release/docs suite: `21/21`.
- Backend full suite: `574/574`.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- Frontend build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: `9/9`.
- Playwright admin VPS smoke test discovery: OK, найден 1 test в проекте `admin-vps-smoke`.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: admin bootstrap wrapper

Что проверялось:

- `scripts/admin-bootstrap.ps1` запускает backend-команду `admin-bootstrap` как one-shot maintenance flow без HTTP-сервера.
- Скрипт поддерживает локальный SQLite-режим и production/Postgres-режим с явной строкой подключения.
- Dry-run валидирует параметры, не меняет БД и печатает `Password: [hidden]` вместо пароля.
- `docs/admin-bootstrap.md` описывает локальный и production запуск на русском языке.
- Раздел "Что нового" получил релиз `2026-06-19-admin-bootstrap-wrapper`, версия `0.182.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 -LocalSqlite -EnvironmentName Local -Email admin@local.test -Password "LocalAdminPassword123!" -DryRun
powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 -LocalSqlite -EnvironmentName Local -Email admin@local.test -Password "LocalAdminPassword123!" -ProjectPath backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminBootstrapCliScriptTests|AdminBootstrapServiceTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
git diff --check
```

Результат:

- Admin bootstrap dry-run: OK, password hidden.
- Local SQLite admin bootstrap/reset на временной БД: OK.
- `AdminBootstrapCliScriptTests`: `3/3`.
- Targeted release/docs suite: `23/23`.
- Backend full suite: `573/573`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Fresh local SQLite smoke: OK, latest `2026-06-19-admin-bootstrap-wrapper`.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- `git diff --check`: OK.

## Проверка 2026-06-19: staging smoke report evidence placeholders

Что проверялось:

- `scripts/validate-staging-smoke-report.ps1` в режиме `-RequireAllPassed` теперь запрещает `TODO` в evidence.
- Staging smoke report нельзя принять с `status = passed`, если доказательства остались шаблонными placeholder-строками.
- `docs/staging-smoke-checklist.md` уточняет правило real evidence для acceptance mode.
- Раздел "Что нового" получил релиз `2026-06-19-staging-smoke-report-evidence-placeholders`, версия `0.181.0`.
- Связанный предыдущий payment provider baseline сохранен как `2026-06-19-payment-provider-smoke-report-acceptance-gates`.
- `scripts/validate-payment-provider-smoke-report.ps1` при `-RequireAllPassed` теперь требует `true` для всех provider gates.
- Приемочный payment provider smoke report больше нельзя закрыть одним `status = passed`, если не подтверждены account, checkout, provider confirmation, webhook, subscription и refund.
- `docs/payment-provider-smoke.md` уточняет gates `accountConfigured`, `checkoutCreated`, `providerConfirmation`, `webhookProcessed`, `subscriptionActivated`, `refundChecked`.
- Связанный предыдущий VPS smoke report baseline сохранен как `2026-06-19-vps-production-smoke-report-contract`.
- Связанный предыдущий aggregate baseline сохранен как `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards-regression`.
- Связанный предыдущий aggregate baseline сохранен как `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards`.
- Связанный предыдущий CI step regression baseline сохранен как `2026-06-19-production-ci-workflow-artifacts-guards-ci-step-regression`.
- Связанный предыдущий CI step guard baseline сохранен как `2026-06-19-production-ci-workflow-artifacts-guards-ci-step-guard`.
- Связанный предыдущий CI baseline сохранен как `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression-ci-step`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-staging-smoke-report.ps1 -OutputPath tmp\generated-staging-smoke-report.json -ApiBaseUrl http://127.0.0.1:18102 -PublicWebUrl http://127.0.0.1:5183 -CabinetWebUrl http://127.0.0.1:5184 -AdminWebUrl http://127.0.0.1:5185 -EnvironmentName staging -Operator local-test -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-staging-smoke-report.ps1 -ReportPath tmp\generated-staging-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\validate-staging-smoke-report.ps1 -ReportPath tmp\generated-staging-smoke-report.json -RequireAllPassed
powershell -ExecutionPolicy Bypass -File scripts\new-vps-production-smoke-report.ps1 -OutputPath tmp\vps-production-smoke-report.json -ApiBaseUrl http://127.0.0.1:18102 -PublicWebUrl http://127.0.0.1:5183 -CabinetWebUrl http://127.0.0.1:5184 -AdminWebUrl http://127.0.0.1:5185 -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-vps-production-smoke-report.ps1 -ReportPath tmp\vps-production-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\validate-vps-production-smoke-report.ps1 -ReportPath tmp\vps-production-smoke-report.json -RequireAllPassed
powershell -ExecutionPolicy Bypass -File scripts\new-payment-provider-smoke-report.ps1 -OutputPath tmp\generated-payment-provider-smoke-report.json -EnvironmentName staging -Operator local-test -Mode sandbox -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-payment-provider-smoke-report.ps1 -ReportPath tmp\generated-payment-provider-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\validate-payment-provider-smoke-report.ps1 -ReportPath tmp\generated-payment-provider-smoke-report.json -RequireAllPassed
powershell -ExecutionPolicy Bypass -File scripts\test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-ci-workflow-artifacts-guards.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "StagingSmokeChecklistTests|PaymentProviderSmokeReportTests|VpsProductionSmokeTests|ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Staging smoke report generator: OK.
- Staging smoke report validator: OK.
- Staging expected fail-closed `-RequireAllPassed`: OK.
- Staging TODO evidence fail-closed: OK.
- `StagingSmokeChecklistTests`: `8/8`.
- Payment provider smoke report generator: OK.
- Payment provider smoke report validator: OK.
- Payment provider expected fail-closed `-RequireAllPassed`: OK.
- `PaymentProviderSmokeReportTests`: `6/6`.
- VPS production smoke report generator: OK.
- VPS production smoke report validator: OK.
- Expected fail-closed `-RequireAllPassed`: OK.
- `VpsProductionSmokeTests`: `7/7`.
- Production CI workflow artifacts aggregate CI step guard: OK.
- Production CI workflow artifacts aggregate CI step guard validator: OK.
- Production CI workflow artifacts guards aggregate: OK, `guardsCount = 6`.
- Production CI workflow artifacts aggregate guard validator: OK, включая CI-step tamper cases.
- Production CI workflow artifacts aggregate guard validator CI-step tamper cases: OK.
- Targeted release/docs suite: `94/94`.
- Backend full suite: `570/570`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-staging-smoke-report-evidence-placeholders`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-staging-smoke-report-evidence-placeholders`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production CI workflow artifacts aggregate guard regression

Что проверялось:

- `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1` проверяет fail-closed поведение aggregate guard.
- Harness портит копию `.github/workflows/ci.yml` и ожидает ошибки для `missing-readiness-guard-step`, `missing-readiness-assertion-log-artifact`, `missing-production-evidence-result-artifact` и `missing-if-no-files-found-error`.
- Раздел "Что нового" получил релиз `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression`, версия `0.173.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-ci-workflow-artifacts-guards.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1 -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production CI workflow artifacts guards aggregate: OK.
- Production CI workflow artifacts aggregate guard validator: OK.
- Readiness assertion workflow artifacts guard and validator: OK.
- Production evidence workflow artifacts guard and validator: OK.
- Targeted release/docs suite: `68/68`.
- Backend full suite: `559/559`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression`.
- `git diff --check`: OK.
## Проверка 2026-06-19: production CI workflow artifacts guards aggregate

Что проверялось:

- `scripts/test-production-ci-workflow-artifacts-guards.ps1` запускает оба production workflow artifacts guards и оба fail-closed validators одной командой.
- GitHub Actions backend job содержит step `Guard production CI workflow artifacts contracts` сразу после checkout и до .NET setup/build/test.
- Раздел "Что нового" получил релиз `2026-06-19-production-ci-workflow-artifacts-guards-aggregate`, версия `0.172.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-ci-workflow-artifacts-guards.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1 -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production CI workflow artifacts guards aggregate: OK.
- Readiness assertion workflow artifacts guard and validator: OK.
- Production evidence workflow artifacts guard and validator: OK.
- Targeted release/docs suite: `67/67`.
- Backend full suite: `558/558`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-ci-workflow-artifacts-guards-aggregate`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-ci-workflow-artifacts-guards-aggregate`.
- `git diff --check`: OK.
## Проверка 2026-06-19: production readiness assertion CI workflow artifacts guard regression

Что проверялось:

- `scripts/test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1` проверяет fail-closed поведение readiness assertion workflow artifacts guard.
- Harness портит копию `.github/workflows/ci.yml` и ожидает ошибки для `missing-guard-step`, `missing-assertion-log-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-workflow-artifacts-guard-regression`, версия `0.171.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI workflow artifacts guard: OK.
- Production readiness assertion CI workflow artifacts guard validator: OK.
- Production readiness assertion CI regression wrapper smoke: OK.
- Targeted release/docs suite: `66/66`.
- Backend full suite: `557/557`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-workflow-artifacts-guard-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-workflow-artifacts-guard-regression`.
- `git diff --check`: OK.
## Проверка 2026-06-19: production evidence CI workflow artifacts guard regression

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1` проверяет fail-closed поведение workflow artifacts guard.
- Harness портит копию `.github/workflows/ci.yml` и ожидает ошибки для `missing-guard-step`, `missing-result-json-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.
- Раздел "Что нового" получил релиз `2026-06-19-production-evidence-ci-workflow-artifacts-guard-regression`, версия `0.170.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence CI workflow artifacts guard: OK.
- Production evidence CI workflow artifacts guard validator: OK.
- Production evidence handoff archive CI regression smoke: OK.
- Targeted release/docs suite: `65/65`.
- Backend full suite: `556/556`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-evidence-ci-workflow-artifacts-guard-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-evidence-ci-workflow-artifacts-guard-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production evidence CI workflow artifacts guard

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1` проверяет published artifacts contract для job `production-evidence`.
- GitHub Actions запускает `Guard production evidence workflow artifacts` до `Run production evidence handoff archive CI regression`.
- Раздел "Что нового" получил релиз `2026-06-19-production-evidence-ci-workflow-artifacts-guard`, версия `0.169.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence CI workflow artifacts guard: OK.
- Production evidence handoff archive CI regression smoke: OK.
- Targeted release/docs suite: `64/64`.
- Backend full suite: `555/555`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-evidence-ci-workflow-artifacts-guard`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-evidence-ci-workflow-artifacts-guard`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production readiness assertion CI workflow guard step

Что проверялось:

- GitHub Actions job `production-readiness-assertion` запускает workflow artifacts guard до readiness assertion wrapper.
- `ProductionReadinessGateTests` проверяет наличие step, команду `test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson` и порядок guard -> wrapper -> upload.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-workflow-guard-step`, версия `0.168.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI workflow artifacts guard: OK.
- Production readiness assertion CI wrapper smoke: OK.
- Targeted release/docs suite: `63/63`.
- Backend full suite: `554/554`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-workflow-guard-step`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-workflow-guard-step`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production readiness assertion CI workflow artifacts guard

Что проверялось:

- `scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1` проверяет published artifact-директорий readiness assertion CI workflow.
- Guard закрепляет job `production-readiness-assertion`, `needs: backend`, запуск wrapper, upload-artifact, `if-no-files-found: error` и пять обязательных files.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-workflow-artifacts`, версия `0.167.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI workflow artifacts guard: OK.
- Production readiness assertion CI wrapper smoke: OK.
- Targeted release/docs suite: `62/62`.
- Backend full suite: `553/553`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-workflow-artifacts`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-workflow-artifacts`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production readiness assertion CI summary artifacts regression

Что проверялось:

- `scripts/test-production-readiness-assertion-ci-summary-validator.ps1` проверяет `bad-ci-artifacts-validator-regression`.
- CI wrapper запускает artifacts validator regression до summary validator regression, чтобы summary harness проверял строку `CI artifacts validator regression`.
- `validate-production-readiness-assertion-ci-regression-result.ps1` требует `bad-ci-artifacts-validator-regression` внутри `ciSummaryValidatorRegression`.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-summary-artifacts-regression`, версия `0.166.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-summary-validator.ps1 -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json -SummaryPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.md -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-step-summary.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-step-summary-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI wrapper smoke: OK.
- CI summary artifacts regression: OK.
- CI step summary smoke: OK.
- Targeted release/docs suite: `61/61`.
- Backend full suite: `552/552`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-summary-artifacts-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-summary-artifacts-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production readiness assertion CI artifacts validator regression

Что проверялось:

- `scripts/test-production-readiness-assertion-ci-artifacts-validator.ps1` проверяет fail-closed поведение validator всего readiness assertion CI artifact-директория.
- CI wrapper запускает artifacts validator regression автоматически и пишет `ciArtifactsValidatorRegression` в итоговый JSON/Markdown.
- CI result и summary validators проверяют `ciArtifactsValidatorRegression`, если этот блок присутствует.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-artifacts-validator-regression`, версия `0.165.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-artifacts-validator.ps1 -ArtifactDirectory tmp\production-readiness-assertion-ci-regression-test -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-ci-artifacts.ps1 -ArtifactDirectory tmp\production-readiness-assertion-ci-regression-test -RequireBlockedAssertion -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-step-summary.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-step-summary-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI wrapper smoke: OK.
- Production readiness assertion CI artifacts validator regression: OK.
- Production readiness assertion CI artifacts validator: OK.
- CI step summary smoke: OK.
- Targeted release/docs suite: `60/60`.
- Backend full suite: `551/551`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-artifacts-validator-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-artifacts-validator-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production readiness assertion CI artifacts validator

Что проверялось:

- `scripts/validate-production-readiness-assertion-ci-artifacts.ps1` проверяет весь artifact-директорий readiness assertion CI одной командой.
- CI wrapper запускает artifact-directory validator перед выводом результата.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-artifacts-validator`, версия `0.164.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-ci-artifacts.ps1 -ArtifactDirectory tmp\production-readiness-assertion-ci-regression-test -RequireBlockedAssertion -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-step-summary.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-step-summary-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI wrapper smoke: OK.
- Production readiness assertion CI artifacts validator: OK.
- CI step summary smoke: OK.
- Targeted release/docs suite: `59/59`.
- Backend full suite: `550/550`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-artifacts-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-artifacts-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production readiness assertion CI step summary smoke

Что проверялось:

- `scripts/test-production-readiness-assertion-ci-step-summary.ps1` выставляет `GITHUB_STEP_SUMMARY` и проверяет, что readiness assertion CI wrapper реально пишет summary file.
- Summary валидируется через `scripts/validate-production-readiness-assertion-ci-summary.ps1`, сверяется с result Markdown и содержит строки `CI summary validator regression` и `CI result validator regression`.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-step-summary-smoke`, версия `0.163.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-step-summary.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-step-summary-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI step summary smoke: OK.
- Production readiness assertion CI wrapper smoke: OK.
- Targeted release/docs suite: `58/58`.
- Backend full suite: `549/549`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-step-summary-smoke`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-step-summary-smoke`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production readiness assertion CI summary validator

Что проверялось:

- `scripts/validate-production-readiness-assertion-ci-summary.ps1` проверяет Markdown summary readiness assertion CI wrapper.
- `scripts/test-production-readiness-assertion-ci-summary-validator.ps1` проверяет fail-closed поведение summary validator на испорченных JSON/Markdown копиях.
- CI wrapper запускает новый summary validator и regression harness автоматически, сохраняет `ciSummaryValidatorRegression` в result JSON/Markdown и проверяет `GITHUB_STEP_SUMMARY`, если он доступен.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-summary-validator`, версия `0.162.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-ci-summary.ps1 -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json -SummaryPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.md -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-summary-validator.ps1 -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json -SummaryPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.md -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-ci-regression-result.ps1 -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI wrapper smoke with summary validator: OK.
- Standalone production readiness assertion CI summary validator: OK.
- Standalone production readiness assertion CI summary validator regression: OK.
- Targeted release/docs suite: `57/57`.
- Backend full suite: `548/548`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-summary-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-summary-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-19: production readiness assertion CI result validator regression

Что проверялось:

- `scripts/test-production-readiness-assertion-ci-regression-result-validator.ps1` проверяет fail-closed поведение `validate-production-readiness-assertion-ci-regression-result.ps1` на испорченных копиях CI result JSON/Markdown.
- CI wrapper запускает новый harness автоматически, сохраняет `ciResultValidatorRegression` в result JSON/Markdown и повторно валидирует итоговый artifact.
- Раздел "Что нового" получил релиз `2026-06-19-production-readiness-assertion-ci-result-validator-regression`, версия `0.161.0`.
- Предыдущий релиз standalone validator сохранен в журнале: `2026-06-19-production-readiness-assertion-ci-result-validator`, версия `0.160.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression-result-validator.ps1 -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-ci-regression-result.ps1 -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI wrapper smoke with CI result validator regression: OK.
- Standalone production readiness assertion CI result validator regression: OK.
- Standalone production readiness assertion CI result validator: OK.
- Targeted release/docs suite: `56/56`.
- Backend full suite: `547/547`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-19-production-readiness-assertion-ci-result-validator-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-19-production-readiness-assertion-ci-result-validator-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production readiness assertion CI regression

Что проверялось:

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает `assert-production-readiness.ps1`, сохраняет assertion JSON/Markdown/log, валидирует result и прогоняет validator regression для blocked result.
- `.github/workflows/ci.yml` содержит job `production-readiness-assertion`, который запускается после backend job и публикует artifact `production-readiness-assertion-ci-regression`.
- Раздел "Что нового" получил релиз `2026-06-18-production-readiness-assertion-ci-regression`, версия `0.159.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 -OutputDirectory tmp\production-readiness-assertion-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion CI regression smoke: OK.
- Targeted release/docs suite: `54/54`.
- Backend full suite: `545/545`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-readiness-assertion-ci-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-readiness-assertion-ci-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production readiness assertion result validator regression

Что проверялось:

- `scripts/test-production-readiness-assertion-result-validator.ps1` валидирует корректный assertion result и проверяет fail-closed сценарии validator на испорченных JSON/Markdown copies.
- Regression harness ожидает ошибки для bad status, неправильного `failedEvidenceReportsCount`, отсутствующего `vpn-live` evidence report, сломанного Markdown и `-RequireProductionReady` на blocked result.
- Раздел "Что нового" получил релиз `2026-06-18-production-readiness-assertion-result-validator-regression`, версия `0.158.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json -OutputPath tmp\production-readiness-assertion.md -Force
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-result-validator.ps1 -ResultJsonPath tmp\production-readiness-assertion.json -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion expected blocked artifact smoke: OK.
- Production readiness assertion result validator regression smoke: OK.
- Targeted release/docs suite: `53/53`.
- Backend full suite: `544/544`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-readiness-assertion-result-validator-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-readiness-assertion-result-validator-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production readiness assertion result validator

Что проверялось:

- `scripts/validate-production-readiness-assertion-result.ps1` проверяет JSON/Markdown artifacts от `assert-production-readiness.ps1`.
- `assert-production-readiness.ps1` запускает validator после записи result artifacts и до expected fail-closed `blocked`.
- Раздел "Что нового" получил релиз `2026-06-18-production-readiness-assertion-result-validator`, версия `0.157.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json -OutputPath tmp\production-readiness-assertion.md -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-result.ps1 -ResultJsonPath tmp\production-readiness-assertion.json -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion expected blocked artifact smoke with standalone validator: OK.
- Standalone production readiness assertion result validator: OK.
- Targeted release/docs suite: `52/52`.
- Backend full suite: `543/543`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-readiness-assertion-result-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-readiness-assertion-result-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production readiness assertion result artifacts

Что проверялось:

- `scripts/assert-production-readiness.ps1` сохраняет JSON/Markdown result artifacts при expected fail-closed `blocked`.
- Result artifacts фиксируют failed evidence reports, roadmap/release blockers, пути reports и итоговый статус.
- Раздел "Что нового" получил релиз `2026-06-18-production-readiness-assertion-result-artifacts`, версия `0.156.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json -OutputPath tmp\production-readiness-assertion.md -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production readiness assertion expected blocked artifact smoke: OK.
- Targeted release/docs suite: `51/51`.
- Backend full suite: `542/542`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-readiness-assertion-result-artifacts`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-readiness-assertion-result-artifacts`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive CI result validator regression

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1` проверяет fail-closed поведение standalone CI result validator.
- Основной CI wrapper добавляет `ciResultValidatorRegression` в итоговые JSON/Markdown artifacts и повторно валидирует финальный result.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator-regression`, версия `0.155.0`.

Команды:

```powershell
$env:GITHUB_STEP_SUMMARY = "tmp\production-evidence-handoff-package-archive-ci-summary.md"; powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1 -ResultJsonPath tmp\production-evidence-handoff-package-archive-ci-regression-test\production-evidence-handoff-package-archive-ci-regression-result.json -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive CI wrapper smoke with result validator regression: OK.
- Standalone CI result validator regression: OK.
- Targeted release/docs suite: `50/50`.
- Backend full suite: `541/541`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive CI result validator

Что проверялось:

- `scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1` проверяет итоговый CI regression JSON/Markdown artifact.
- Основной CI wrapper запускает result validator после финальной записи artifacts.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator`, версия `0.154.0`.

Команды:

```powershell
$env:GITHUB_STEP_SUMMARY = "tmp\production-evidence-handoff-package-archive-ci-summary.md"; powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package-archive-ci-regression-result.ps1 -ResultJsonPath tmp\production-evidence-handoff-package-archive-ci-regression-test\production-evidence-handoff-package-archive-ci-regression-result.json -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive CI wrapper smoke with result validator: OK.
- Standalone CI result validator: OK.
- Targeted release/docs suite: `49/49`.
- Backend full suite: `540/540`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive CI summary validator regression

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-ci-summary-validator.ps1` проверяет fail-closed поведение CI summary validator.
- Основной CI wrapper добавляет `ciSummaryValidatorRegression` в JSON/Markdown result artifacts.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator-regression`, версия `0.153.0`.

Команды:

```powershell
$env:GITHUB_STEP_SUMMARY = "tmp\production-evidence-handoff-package-archive-ci-summary.md"; powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-summary-validator.ps1 -ResultJsonPath tmp\production-evidence-handoff-package-archive-ci-regression-test\production-evidence-handoff-package-archive-ci-regression-result.json -SummaryPath tmp\production-evidence-handoff-package-archive-ci-summary.md -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive CI wrapper smoke with summary validator regression: OK.
- Standalone CI summary validator regression: OK.
- Targeted release/docs suite: `48/48`.
- Backend full suite: `539/539`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, `18` файлов.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive CI summary validator

Что проверялось:

- `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1` сверяет CI summary Markdown с JSON result artifact.
- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` запускает validator для result Markdown и `GITHUB_STEP_SUMMARY`, если summary-файл доступен.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator`, версия `0.152.0`.

Команды:

```powershell
$env:GITHUB_STEP_SUMMARY = "tmp\production-evidence-handoff-package-archive-ci-summary.md"; powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package-archive-ci-summary.ps1 -ResultJsonPath tmp\production-evidence-handoff-package-archive-ci-regression-test\production-evidence-handoff-package-archive-ci-regression-result.json -SummaryPath tmp\production-evidence-handoff-package-archive-ci-summary.md -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive CI wrapper smoke with summary validator: OK.
- Standalone CI summary validator: OK.
- Targeted release/docs suite: `47/47`.
- Backend full suite: `538/538`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, `18` файлов.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive CI summary

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` пишет Markdown-результат в `GITHUB_STEP_SUMMARY`, если переменная доступна.
- CI summary содержит общий статус, release id, статусы основного flow, result validator regression и long-path regression.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-ci-summary`, версия `0.151.0`.

Команды:

```powershell
$env:GITHUB_STEP_SUMMARY = "tmp\production-evidence-handoff-package-archive-ci-summary.md"; powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive CI regression wrapper smoke with `GITHUB_STEP_SUMMARY`: OK.
- Targeted release/docs suite: `46/46`.
- Backend full suite: `537/537`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, `17` файлов.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-summary`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-summary`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive CI workflow

Что проверялось:

- `.github/workflows/ci.yml` получил отдельный job `production-evidence`.
- Job запускает `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` после backend-проверок.
- GitHub Actions публикует JSON/Markdown artifacts `production-evidence-handoff-package-archive-ci-regression-result.json` и `.md`.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-ci-workflow`, версия `0.150.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive CI regression wrapper smoke: OK.
- Targeted release/docs suite: `45/45`.
- Backend full suite: `536/536`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, `17` файлов.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-workflow`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-workflow`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive CI regression wrapper

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` запускает основной archive flow, result validator regression и long-path regression.
- Wrapper сохраняет `production-evidence-handoff-package-archive-ci-regression-result.json` и `.md`.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-ci-regression`, версия `0.149.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); $files = @(); $files += git diff --name-only; $files += git ls-files --others --exclude-standard; $files | ? { $_ -and (Test-Path $_) -and $_ -notlike 'tmp/*' -and $_ -notlike 'tmp\*' } | select -Unique | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive CI regression wrapper smoke: OK.
- Targeted release/docs suite: `44/44`.
- Backend full suite: `535/535`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных и новых файлов: strict UTF-8 without BOM, `17` файлов.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-ci-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive long path regression

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-long-path.ps1` запускает полный flow в длинной production-evidence директории.
- Harness проверяет, что имя handoff package ZIP не содержит полный release id, содержит 12-символьный hash release id и остается коротким.
- Result JSON сохраняет полный release id.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-long-path-regression`, версия `0.148.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-long-path.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-long-release-id-path-regression-test -Force -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); git diff --name-only | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive long path regression smoke: OK.
- Targeted release/docs suite: `43/43`.
- Backend full suite: `534/534`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: strict UTF-8 without BOM, `17` changed/untracked source files.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-long-path-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-long-path-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive flow result validator regression

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-flow-result-validator.ps1` проверяет fail-closed поведение validator результата полного flow.
- Regression harness ожидает ошибки для bad status, неверного SHA256 handoff archive, отсутствующего tamper-сценария и Markdown без блока `Tested failures`.
- Handoff package archive использует короткое hash-based default-имя ZIP, чтобы длинный release id не ломал Windows path-limit.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator-regression`, версия `0.147.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-flow.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-flow-result-validator-regression-test -Force
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-flow-result-validator.ps1 -ResultJsonPath tmp\production-evidence-handoff-package-archive-flow-result-validator-regression-test\production-evidence-handoff-package-archive-flow-result.json -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); git diff --name-only | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive flow result validator regression smoke: OK.
- Targeted release/docs suite: `42/42`.
- Backend full suite: `533/533`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: strict UTF-8 without BOM, `19` changed/untracked source files.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive flow result validator

Что проверялось:

- `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1` проверяет JSON/Markdown результат полного flow.
- Flow автоматически запускает validator после записи `production-evidence-handoff-package-archive-flow-result.json` и `.md`.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator`, версия `0.146.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-flow.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-flow-result-validator-test -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package-archive-flow-result.ps1 -ResultJsonPath tmp\production-evidence-handoff-package-archive-flow-result-validator-test\production-evidence-handoff-package-archive-flow-result.json -WriteJson
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); git diff --name-only | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive flow result validator smoke: OK.
- Targeted release/docs suite: `41/41`.
- Backend full suite: `532/532`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: strict UTF-8 without BOM, `17` tracked-файлов и новый validator-файл.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive flow result artifacts

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` сохраняет `production-evidence-handoff-package-archive-flow-result.json` и `.md`.
- Result artifacts фиксируют release id, package status, SHA256 архивов, пути artifacts и tamper-сценарии regression harness.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-flow-result`, версия `0.145.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-flow.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-flow-result-test -Force
Test-Path tmp\production-evidence-handoff-package-archive-flow-result-test\production-evidence-handoff-package-archive-flow-result.json
Test-Path tmp\production-evidence-handoff-package-archive-flow-result-test\production-evidence-handoff-package-archive-flow-result.md
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true); git diff --name-only | % { $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $_)); [void]$utf8Strict.GetString($bytes); if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw "UTF-8 BOM found: $_" } }
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive flow result artifacts smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `531/531`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`; проверка создания тарифа в админке привязана к реальному POST `/api/admin/tariffs` и карточке в списке.
- Secret scan: 0 findings.
- Кодировка измененных файлов: strict UTF-8 without BOM, `18` файлов.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow-result`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow-result`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive flow safety

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` проверяет output directory перед рекурсивной очисткой.
- Flow запрещает корень файловой системы, корень репозитория и папку без явного `production-evidence` в имени.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-flow-safety`, версия `0.144.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-flow.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-flow-safety-test -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Guarded production evidence handoff package archive flow smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `530/530`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow-safety`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow-safety`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive end-to-end flow

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` собирает весь локальный production evidence handoff flow одной командой.
- Flow создает evidence bundle, summary, manifest, production evidence ZIP, handoff receipt, checklist, package, финальный ZIP и запускает archive validator regression.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-flow`, версия `0.143.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-flow.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-flow-test -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive end-to-end flow smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `529/529`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-flow`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive validator regression

Что проверялось:

- `scripts/test-production-evidence-handoff-package-archive-validator.ps1` запускает happy path и tamper-сценарии для финального ZIP-архива handoff package.
- Harness проверяет fail-closed ошибки для неверного expected SHA256, лишнего `unexpected-entry.txt` и отсутствующего `SHA256SUMS.txt`.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-validator-regression`, версия `0.142.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-validator-regression-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-package-archive-validator-regression-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-package-archive-validator-regression-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-package-archive-validator-regression-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-package-archive-validator-regression-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-package-archive-validator-regression-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-package-archive-validator-regression-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-package-archive-validator-regression-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-package-archive-validator-regression-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-package-archive-validator-regression-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-package-archive-validator-regression-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-checklist.ps1 -ReceiptPath tmp\production-evidence-handoff-package-archive-validator-regression-test\production-evidence-handoff-receipt.json -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package.ps1 -ChecklistPath tmp\production-evidence-handoff-package-archive-validator-regression-test\production-evidence-handoff-checklist.json -ExpectedArchiveSha256 $archiveSha256 -Force
$packageArchive = powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package-archive.ps1 -PackageDirectory tmp\production-evidence-handoff-package-archive-validator-regression-test\production-evidence-handoff-package -ExpectedArchiveSha256 $archiveSha256 -Force -WriteJson | ConvertFrom-Json
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-validator.ps1 -ArchivePath $packageArchive.archivePath -ExpectedArchiveSha256 $packageArchive.archiveSha256
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive validator regression smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `528/528`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-validator-regression`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-validator-regression`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive validator

Что проверялось:

- `scripts/validate-production-evidence-handoff-package-archive.ps1` проверяет финальный ZIP-архив handoff package.
- Валидатор сверяет SHA256 внешнего ZIP, запрещает неожиданные и вложенные entries, временно извлекает package и повторно запускает package validator.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive-validator`, версия `0.141.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-validator-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-package-archive-validator-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-package-archive-validator-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-package-archive-validator-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-package-archive-validator-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-package-archive-validator-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-package-archive-validator-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-package-archive-validator-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-package-archive-validator-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-package-archive-validator-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-package-archive-validator-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-checklist.ps1 -ReceiptPath tmp\production-evidence-handoff-package-archive-validator-test\production-evidence-handoff-checklist.json -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package.ps1 -ChecklistPath tmp\production-evidence-handoff-package-archive-validator-test\production-evidence-handoff-checklist.json -ExpectedArchiveSha256 $archiveSha256 -Force
$packageArchive = powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package-archive.ps1 -PackageDirectory tmp\production-evidence-handoff-package-archive-validator-test\production-evidence-handoff-package -ExpectedArchiveSha256 $archiveSha256 -Force -WriteJson | ConvertFrom-Json
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package-archive.ps1 -ArchivePath $packageArchive.archivePath -ExpectedArchiveSha256 $packageArchive.archiveSha256
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive validator smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `527/527`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package archive

Что проверялось:

- `scripts/new-production-evidence-handoff-package-archive.ps1` упаковывает проверенный handoff package в один ZIP.
- Перед упаковкой повторно запускается package validator, а в архив попадают только разрешенные package files.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-archive`, версия `0.140.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-package-archive-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-package-archive-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-package-archive-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-package-archive-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-package-archive-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-package-archive-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-package-archive-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-package-archive-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-package-archive-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-package-archive-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-package-archive-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-checklist.ps1 -ReceiptPath tmp\production-evidence-handoff-package-archive-test\production-evidence-handoff-receipt.json -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package.ps1 -ChecklistPath tmp\production-evidence-handoff-package-archive-test\production-evidence-handoff-checklist.json -ExpectedArchiveSha256 $archiveSha256 -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package-archive.ps1 -PackageDirectory tmp\production-evidence-handoff-package-archive-test\production-evidence-handoff-package -ExpectedArchiveSha256 $archiveSha256 -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package archive smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `526/526`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-archive`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-archive`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package validator

Что проверялось:

- `scripts/validate-production-evidence-handoff-package.ps1` проверяет готовый handoff package.
- Валидатор запрещает лишние файлы, сверяет index JSON/Markdown, `SHA256SUMS.txt`, SHA256 каждого artifact и повторно запускает checklist validator.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package-validator`, версия `0.139.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-package-validator-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-package-validator-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-package-validator-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-package-validator-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-package-validator-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-package-validator-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-package-validator-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-package-validator-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-package-validator-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-package-validator-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-package-validator-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-checklist.ps1 -ReceiptPath tmp\production-evidence-handoff-package-validator-test\production-evidence-handoff-receipt.json -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package.ps1 -ChecklistPath tmp\production-evidence-handoff-package-validator-test\production-evidence-handoff-checklist.json -ExpectedArchiveSha256 $archiveSha256 -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package.ps1 -PackageDirectory tmp\production-evidence-handoff-package-validator-test\production-evidence-handoff-package -ExpectedArchiveSha256 $archiveSha256
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package validator smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `525/525`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff package

Что проверялось:

- `scripts/new-production-evidence-handoff-package.ps1` собирает минимальный package после проверки checklist.
- Package содержит только ZIP, receipt, checklist, index и `SHA256SUMS.txt`.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-package`, версия `0.138.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-package-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-package-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-package-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-package-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-package-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-package-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-package-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-package-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-package-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-package-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-package-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-checklist.ps1 -ReceiptPath tmp\production-evidence-handoff-package-test\production-evidence-handoff-receipt.json -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package.ps1 -ChecklistPath tmp\production-evidence-handoff-package-test\production-evidence-handoff-checklist.json -ExpectedArchiveSha256 $archiveSha256 -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff package smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `524/524`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-package`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-package`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff checklist validator

Что проверялось:

- `scripts/validate-production-evidence-handoff-checklist.ps1` проверяет JSON/Markdown checklist против receipt, Markdown и summary.
- Валидатор повторно запускает receipt validator, сверяет SHA256 архива, SHA256 manifest, gates и operator actions.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-checklist-validator`, версия `0.137.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-checklist-validator-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-checklist-validator-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-checklist-validator-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-checklist-validator-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-checklist-validator-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-checklist-validator-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-checklist-validator-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-checklist-validator-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-checklist-validator-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-checklist-validator-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-checklist-validator-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-checklist.ps1 -ReceiptPath tmp\production-evidence-handoff-checklist-validator-test\production-evidence-handoff-receipt.json -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-checklist.ps1 -ChecklistPath tmp\production-evidence-handoff-checklist-validator-test\production-evidence-handoff-checklist.json -ExpectedArchiveSha256 $archiveSha256
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff checklist validator smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `523/523`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-checklist-validator`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-checklist-validator`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff checklist

Что проверялось:

- `scripts/new-production-evidence-handoff-checklist.ps1` формирует JSON/Markdown checklist для передачи production evidence.
- Checklist запускает receipt validator, читает `production-readiness-summary.json`, фиксирует gates и в строгом режиме блокирует handoff без `production-ready`.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-checklist`, версия `0.136.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-checklist-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-checklist-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-checklist-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-checklist-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-checklist-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-checklist-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-checklist-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-checklist-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-checklist-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-checklist-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-checklist-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-checklist.ps1 -ReceiptPath tmp\production-evidence-handoff-checklist-test\production-evidence-handoff-receipt.json -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff checklist smoke: OK.
- Targeted release/docs suite: OK.
- Backend full suite: `522/522`.
- API build: OK.
- TelegramBot build: OK.
- Frontend tests: `66/66`.
- Frontend typecheck: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Frontend build: OK.
- Playwright console E2E: `9/9`.
- Secret scan: 0 findings.
- Кодировка измененных файлов: U+FFFD не найден.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-checklist`.
- Local VPS smoke dry-run: OK, latest `2026-06-18-production-evidence-handoff-checklist`.
- `git diff --check`: OK.

## Проверка 2026-06-18: production evidence handoff receipt validator

Что проверялось:

- `scripts/validate-production-evidence-handoff-receipt.ps1` проверяет JSON/Markdown receipt против ZIP-архива production evidence.
- Валидатор повторно запускает archive validator и сверяет release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-receipt-validator`, версия `0.135.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-receipt-validator-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-receipt-validator-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-receipt-validator-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-receipt-validator-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-receipt-validator-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-receipt-validator-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-receipt-validator-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-receipt-validator-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-receipt-validator-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-receipt-validator-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-receipt-validator-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-receipt.ps1 -ReceiptPath tmp\production-evidence-handoff-receipt-validator-test\production-evidence-handoff-receipt.json -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff receipt validator smoke: OK.
- `ProductionReadinessGateTests`: `14/14`.
- Backend full suite: `521/521`.
- API Release build: OK.
- TelegramBot Release build: OK.
- Frontend unit tests: `66/66`.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend audit: `0 vulnerabilities`.
- Playwright console smoke: `9/9`.
- Secret scan: OK.
- Encoding guard: OK.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-receipt-validator`.
- Local SQLite VPS smoke dry-run: OK.
- `git diff --check`: OK.
- `STATE-014` остается закрытым и синхронизированным; production-ready blockers остаются внешними.

## Проверка 2026-06-18: production evidence handoff receipt

Что проверялось:

- `scripts/new-production-evidence-handoff-receipt.ps1` создает JSON/Markdown receipt для проверенного ZIP-архива.
- Receipt запускает archive validator, затем фиксирует release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files без содержимого reports.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-handoff-receipt`, версия `0.134.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-handoff-receipt-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-handoff-receipt-test\production-readiness-summary.md -ReportPath tmp\production-evidence-handoff-receipt-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-handoff-receipt-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-handoff-receipt-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-handoff-receipt-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-handoff-receipt-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-handoff-receipt-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-handoff-receipt-test\production-evidence.zip -RequireAllFiles -Force
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-handoff-receipt-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 -ArchivePath tmp\production-evidence-handoff-receipt-test\production-evidence.zip -ExpectedArchiveSha256 $archiveSha256 -RequireAllFiles -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence handoff receipt smoke: OK.
- `ProductionReadinessGateTests`: `13/13`.
- Backend full suite: `520/520`.
- API Release build: OK.
- TelegramBot Release build: OK.
- Frontend unit tests: `66/66`.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend audit: `0 vulnerabilities`.
- Playwright console smoke: `9/9`.
- Secret scan: OK.
- Encoding guard: OK.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-handoff-receipt`.
- Local SQLite VPS smoke dry-run: OK.
- `git diff --check`: OK.
- `STATE-014` остается закрытым и синхронизированным; production-ready blockers остаются внешними.

## Проверка 2026-06-18: production evidence archive validator

Что проверялось:

- `scripts/validate-production-evidence-archive.ps1` проверяет ZIP-архив production evidence bundle по manifest внутри архива.
- Валидатор запрещает лишние entries, сверяет обязательные файлы, размеры, `totalBytes`, SHA256 каждого entry и опциональный expected archive SHA256.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-archive-validator`, версия `0.133.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-archive-validator-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-archive-validator-test\production-readiness-summary.md -ReportPath tmp\production-evidence-archive-validator-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-archive-validator-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-archive-validator-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-archive-validator-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-archive-validator-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-archive-validator-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-archive-validator-test\production-evidence.zip -RequireAllFiles -Force -WriteJson
$archiveSha256 = (Get-FileHash -LiteralPath tmp\production-evidence-archive-validator-test\production-evidence.zip -Algorithm SHA256).Hash.ToLowerInvariant()
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-archive.ps1 -ArchivePath tmp\production-evidence-archive-validator-test\production-evidence.zip -RequireAllFiles -ExpectedArchiveSha256 $archiveSha256
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence archive validator smoke: OK.
- `ProductionReadinessGateTests`: `12/12`.
- Backend full suite: `519/519`.
- API Release build: OK.
- TelegramBot Release build: OK.
- Frontend unit tests: `66/66`.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend audit: `0 vulnerabilities`.
- Playwright console smoke: `9/9`.
- Secret scan: OK.
- Encoding guard: OK.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-archive-validator`.
- Local SQLite VPS smoke dry-run: OK.
- `git diff --check`: OK.
- `STATE-014` остается закрытым и синхронизированным; production-ready blockers остаются внешними.

## Проверка 2026-06-18: production evidence archive

Что проверялось:

- `scripts/new-production-evidence-archive.ps1` собирает ZIP-архив production evidence bundle только после успешной проверки manifest.
- Архиватор добавляет в ZIP сам manifest и файлы, перечисленные в manifest, проверяет relative paths и возвращает SHA256 архива/manifest.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-archive`, версия `0.132.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-archive-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-archive-test\production-readiness-summary.md -ReportPath tmp\production-evidence-archive-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-archive-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-archive-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-archive-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-archive-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-manifest.ps1 -ManifestPath tmp\production-evidence-archive-test\production-evidence-manifest.json -RequireAllFiles
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 -ManifestPath tmp\production-evidence-archive-test\production-evidence-manifest.json -OutputPath tmp\production-evidence-archive-test\production-evidence.zip -RequireAllFiles -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence archive smoke: OK.
- `ProductionReadinessGateTests`: `11/11`.
- Backend full suite: `518/518`.
- API Release build: OK.
- TelegramBot Release build: OK.
- Frontend unit tests: `66/66`.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend audit: `0 vulnerabilities`.
- Playwright console smoke: `9/9`.
- Secret scan: OK.
- Encoding guard: OK.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-archive`.
- Local SQLite VPS smoke dry-run: OK.
- `git diff --check`: OK.
- `STATE-014` остается закрытым и синхронизированным; production-ready blockers остаются внешними.

## Проверка 2026-06-18: production evidence manifest validator

Что проверялось:

- `scripts/validate-production-evidence-manifest.ps1` проверяет `production-evidence-manifest.json` после генерации handoff bundle.
- Валидатор сверяет schema, release id, обязательные файлы, relative paths, размеры, timestamps, total files, total bytes и пересчитывает SHA256 каждого файла.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-manifest-validator`, версия `0.131.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-manifest-validator-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-manifest-validator-test\production-readiness-summary.md -ReportPath tmp\production-evidence-manifest-validator-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-manifest-validator-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-manifest-validator-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-manifest-validator-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-manifest-validator-test -RequireSummary -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-manifest.ps1 -ManifestPath tmp\production-evidence-manifest-validator-test\production-evidence-manifest.json -RequireAllFiles
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-restore
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release --no-restore
npm test --prefix frontend
npm run typecheck --prefix frontend
npm audit --audit-level=high --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 18102 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
git diff --check
```

Результат:

- Production evidence manifest validator smoke: OK.
- `ProductionReadinessGateTests`: `10/10`.
- Backend full suite: `517/517`.
- API Release build: OK.
- TelegramBot Release build: OK.
- Frontend unit tests: `66/66`.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend audit: `0 vulnerabilities`.
- Playwright console smoke: `9/9`.
- Secret scan: OK.
- Encoding guard: OK.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-manifest-validator`.
- Local SQLite VPS smoke dry-run: OK.
- `git diff --check`: OK.
- `STATE-014` остается закрытым и синхронизированным; production-ready blockers остаются внешними.

## Проверка 2026-06-18: production evidence manifest

Что проверялось:

- `scripts/new-production-evidence-manifest.ps1` создает manifest для handoff production evidence bundle.
- Manifest валидирует bundle перед записью, фиксирует relative paths, SHA256, размеры файлов, timestamps и release id.
- Manifest не копирует содержимое evidence reports и не требует секретов.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-manifest`, версия `0.130.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-manifest-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-manifest-test\production-readiness-summary.md -ReportPath tmp\production-evidence-manifest-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-manifest-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-manifest-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-manifest-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 -BundleDirectory tmp\production-evidence-manifest-test -RequireSummary -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Production evidence manifest: OK.
- Manifest содержит `6` файлов с SHA256.
- `ProductionReadinessGateTests`: `9/9`.
- Backend full suite: `516/516`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `66/66`.
- Frontend audit: `0 vulnerabilities`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-manifest`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-18: production evidence bundle validator

Что проверялось:

- `scripts/validate-production-evidence-bundle.ps1` проверяет весь каталог production evidence bundle.
- Bundle validator запускает validators для staging/VPS, payment providers, admin VPS, VPN live и production readiness summary.
- Bundle validator поддерживает `-RequireSummary` и строгий `-RequireProductionReady`.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-bundle-validator`, версия `0.129.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-bundle-validator-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-bundle-validator-test\production-readiness-summary.md -ReportPath tmp\production-evidence-bundle-validator-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-bundle-validator-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-bundle-validator-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-bundle-validator-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-bundle.ps1 -BundleDirectory tmp\production-evidence-bundle-validator-test -RequireSummary
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Production evidence bundle validator: OK.
- Bundle validation status: `valid` для generated drafts в non-production режиме.
- `ProductionReadinessGateTests`: `8/8`.
- Backend full suite: `515/515`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `66/66`.
- Frontend audit: `0 vulnerabilities`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-bundle-validator`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-18: production readiness summary validator

Что проверялось:

- `scripts/validate-production-readiness-summary.ps1` проверяет Markdown/JSON summary.
- Валидатор требует все четыре evidence reports, корректные status/count/flag поля, report paths и roadmap blockers.
- Валидатор запрещает очевидные secret markers в Markdown/JSON summary.
- Раздел "Что нового" получил релиз `2026-06-18-production-readiness-summary-validator`, версия `0.128.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-readiness-summary-validator-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-readiness-summary-validator-test\production-readiness-summary.md -ReportPath tmp\production-readiness-summary-validator-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-readiness-summary-validator-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-readiness-summary-validator-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-readiness-summary-validator-test\vpn-live-smoke-report.json -Force
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-summary.ps1 -SummaryPath tmp\production-readiness-summary-validator-test\production-readiness-summary.md -RequireReportFiles
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Production readiness summary validator: OK.
- Validator status: `blocked`, ожидаемо для generated drafts и открытых production blockers.
- `ProductionReadinessGateTests`: `7/7`.
- Backend full suite: `514/514`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `66/66`.
- Frontend audit: `0 vulnerabilities`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-readiness-summary-validator`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-18: production readiness summary

Что проверялось:

- `scripts/new-production-readiness-summary.ps1` создает Markdown/JSON summary по полному production evidence bundle.
- Summary показывает статусы `staging-vps`, `payment-providers`, `admin-vps`, `vpn-live`.
- Summary выводит платежных провайдеров и открытые roadmap blockers без секретов.
- Раздел "Что нового" получил релиз `2026-06-18-production-readiness-summary`, версия `0.127.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-summary-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 -OutputPath tmp\production-evidence-summary-test\production-readiness-summary.md -ReportPath tmp\production-evidence-summary-test\staging-smoke-report.json -PaymentProviderReportPath tmp\production-evidence-summary-test\payment-provider-smoke-report.json -AdminVpsReportPath tmp\production-evidence-summary-test\admin-vps-smoke-report.json -VpnLiveReportPath tmp\production-evidence-summary-test\vpn-live-smoke-report.json -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Production readiness summary: OK, Markdown/JSON созданы.
- Summary status: `blocked`, ожидаемо для generated drafts и открытых production blockers.
- `ProductionReadinessGateTests`: `6/6`.
- Backend full suite: `513/513`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `66/66`.
- Frontend audit: `0 vulnerabilities`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-readiness-summary`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-18: production evidence bundle generator

Что проверялось:

- `scripts/new-production-evidence-bundle.ps1` создает все четыре production evidence reports одной командой.
- Генератор вызывает `new-staging-smoke-report.ps1`, `new-payment-provider-smoke-report.ps1`, `new-admin-vps-smoke-report.ps1`, `new-vpn-live-smoke-report.ps1`.
- Каждый сгенерированный отчет проходит обычный validator.
- `-RunProductionGate` возвращает ожидаемый aggregate gate status `blocked` для черновиков.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-bundle-generator`, версия `0.126.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 -OutputDirectory tmp\production-evidence-test -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test -CabinetWebUrl https://example.test/cabinet -AdminWebUrl https://example.test/admin -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test -RunProductionGate -Force
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Production evidence bundle generator: OK, 4 отчета созданы и провалидированы.
- Generated aggregate production gate status: `blocked`, ожидаемо для TODO/blocked drafts.
- `ProductionReadinessGateTests`: `5/5`.
- Backend full suite: `512/512`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `66/66`.
- Frontend audit: `0 vulnerabilities`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-bundle-generator`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-18: production evidence aggregate gate

Что проверялось:

- `scripts/assert-production-readiness.ps1` запускает все четыре evidence validators и не останавливается на первой ошибке.
- Fail-closed payload содержит `evidenceReports` по `staging-vps`, `payment-providers`, `admin-vps`, `vpn-live`.
- Roadmap/release blockers остаются в том же payload вместе с evidence failures.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-aggregate-gate`, версия `0.125.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Production evidence aggregate gate на текущих blocked templates: expected failure, payload содержит все `evidenceReports`.
- `ProductionReadinessGateTests`: `4/4`.
- Backend full suite: `511/511`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `66/66`.
- Frontend audit: `0 vulnerabilities`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-aggregate-gate`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-18: production evidence bundle gate

Что проверялось:

- `scripts/assert-production-readiness.ps1` требует полный пакет evidence reports.
- Gate запускает `validate-staging-smoke-report.ps1`, `validate-payment-provider-smoke-report.ps1`, `validate-admin-vps-smoke-report.ps1` и `validate-vpn-live-smoke-report.ps1` с `-RequireAllPassed`.
- Текущие blocked templates ожидаемо не проходят production-ready gate.
- Playwright webServer helper проверен на hoisted Vite workspace-зависимости после `npm audit fix`.
- Раздел "Что нового" получил релиз `2026-06-18-production-evidence-bundle-gate`, версия `0.124.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|VpnLiveSmokeReportTests|AdminVpsSmokeReportTests|PaymentProviderSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Production evidence bundle gate на текущих blocked templates: expected failure.
- `ProductionReadinessGateTests`: `3/3`.
- Backend full suite: `510/510`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `66/66`.
- Frontend audit: `0 vulnerabilities`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-18-production-evidence-bundle-gate`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-14: VPN live smoke report

Что проверялось:

- `docs/vpn-live-smoke-report.template.json` содержит все обязательные VPN/3x-ui checks.
- `scripts/new-vpn-live-smoke-report.ps1` создает безопасный черновик VPN live smoke report.
- `scripts/validate-vpn-live-smoke-report.ps1` проверяет URL, даты, top-level gates, checks и forbidden secret markers, включая полные VPN URI.
- `-RequireAllPassed` остается fail-closed и отклоняет сгенерированный blocked report.
- Раздел "Что нового" получил релиз `2026-06-14-vpn-live-smoke-report`, версия `0.123.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-vpn-live-smoke-report.ps1 -OutputPath tmp\generated-vpn-live-smoke-report.json -ApiBaseUrl https://api.example.test -AdminWebUrl https://example.test/admin/ -X3uiPanelUrl https://x3ui.example.test -EnvironmentName staging -Operator local-test
powershell -ExecutionPolicy Bypass -File scripts\validate-vpn-live-smoke-report.ps1 -ReportPath tmp\generated-vpn-live-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\validate-vpn-live-smoke-report.ps1 -ReportPath tmp\generated-vpn-live-smoke-report.json -RequireAllPassed
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "VpnLiveSmokeReportTests|AdminVpsSmokeReportTests|PaymentProviderSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Generator smoke: OK.
- `-RequireAllPassed` для generated blocked report: expected failure.
- `VpnLiveSmokeReportTests`: `4/4`.
- Backend full suite: `509/509`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `65/65`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-14-vpn-live-smoke-report`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-14: admin VPS smoke report

Что проверялось:

- `docs/admin-vps-smoke-report.template.json` содержит все обязательные разделы админки.
- `scripts/new-admin-vps-smoke-report.ps1` создает безопасный черновик admin VPS smoke report.
- `scripts/validate-admin-vps-smoke-report.ps1` проверяет URL, даты, login/console/API gates, разделы и forbidden secret markers.
- `-RequireAllPassed` остается fail-closed и отклоняет сгенерированный blocked report.
- Раздел "Что нового" получил релиз `2026-06-14-admin-vps-smoke-report`, версия `0.122.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-admin-vps-smoke-report.ps1 -OutputPath tmp\generated-admin-vps-smoke-report.json -ApiBaseUrl https://api.example.test -AdminWebUrl https://example.test/admin/ -EnvironmentName staging -Operator local-test
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-report.ps1 -ReportPath tmp\generated-admin-vps-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-report.ps1 -ReportPath tmp\generated-admin-vps-smoke-report.json -RequireAllPassed
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminVpsSmokeReportTests|PaymentProviderSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Generator smoke: OK.
- `-RequireAllPassed` для generated blocked report: expected failure.
- `AdminVpsSmokeReportTests`: `4/4`.
- Backend full suite: `505/505`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `65/65`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-14-admin-vps-smoke-report`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-14: payment provider smoke report generator

Что проверялось:

- `scripts/new-payment-provider-smoke-report.ps1` создает безопасный черновик payment provider smoke report.
- Сгенерированный отчет содержит все 8 web-провайдеров в статусе `blocked`.
- Обычный валидатор принимает структуру отчета.
- `-RequireAllPassed` остается fail-closed и отклоняет сгенерированный blocked report.
- Раздел "Что нового" получил релиз `2026-06-14-payment-provider-smoke-generator`, версия `0.121.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-payment-provider-smoke-report.ps1 -OutputPath tmp\generated-payment-provider-smoke-report.json -EnvironmentName staging -Operator local-test -Mode sandbox
powershell -ExecutionPolicy Bypass -File scripts\validate-payment-provider-smoke-report.ps1 -ReportPath tmp\generated-payment-provider-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\validate-payment-provider-smoke-report.ps1 -ReportPath tmp\generated-payment-provider-smoke-report.json -RequireAllPassed
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "PaymentProviderSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail admin@local.test -AdminPassword LocalAdminPassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Generator smoke: OK.
- `-RequireAllPassed` для generated blocked report: expected failure.
- `PaymentProviderSmokeReportTests`: `5/5`.
- Backend full suite: `501/501`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `65/65`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-14-payment-provider-smoke-generator`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-14: payment provider smoke report

Что проверялось:

- `docs/payment-provider-smoke-report.template.json` содержит все обязательные web-провайдеры.
- `scripts/validate-payment-provider-smoke-report.ps1` проверяет структуру отчета, статусы, даты, boolean gates, evidence и forbidden secret markers.
- `-RequireAllPassed` остается fail-closed и отклоняет blocked template.
- Telegram Stars не входит в web provider smoke report, потому что проверяется отдельным Telegram invoice flow.
- Раздел "Что нового" получил релиз `2026-06-14-payment-provider-smoke-report`, версия `0.120.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-payment-provider-smoke-report.ps1 -ReportPath docs\payment-provider-smoke-report.template.json
powershell -ExecutionPolicy Bypass -File scripts\validate-payment-provider-smoke-report.ps1 -ReportPath docs\payment-provider-smoke-report.template.json -RequireAllPassed
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "PaymentProviderSmokeReportTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
dotnet build backend\src\VpnPlatform.TelegramBot\VpnPlatform.TelegramBot.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
git diff --check
```

Результат:

- Payment provider smoke report validator: OK.
- `-RequireAllPassed` для blocked template: expected failure.
- `PaymentProviderSmokeReportTests`: `4/4`.
- Backend full suite: `500/500`.
- API/TBot Release build: OK, предупреждений 0.
- Frontend unit tests: `65/65`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-14-payment-provider-smoke-report`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-14: staging smoke report generator

Что проверялось:

- `scripts/new-staging-smoke-report.ps1` создает безопасный черновик staging/VPS smoke report.
- Сгенерированный отчет содержит все 18 обязательных checks в статусе `blocked`.
- Обычный валидатор принимает структуру отчета.
- `-RequireAllPassed` остается fail-closed и отклоняет сгенерированный blocked report.
- Раздел "Что нового" получил релиз `2026-06-14-staging-smoke-report-generator`, версия `0.119.0`.

Команды:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-staging-smoke-report.ps1 -OutputPath tmp\generated-staging-smoke-report.json -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test/ -CabinetWebUrl https://example.test/cabinet/ -AdminWebUrl https://example.test/admin/ -EnvironmentName staging -Operator local-test
powershell -ExecutionPolicy Bypass -File scripts\validate-staging-smoke-report.ps1 -ReportPath tmp\generated-staging-smoke-report.json
powershell -ExecutionPolicy Bypass -File scripts\validate-staging-smoke-report.ps1 -ReportPath tmp\generated-staging-smoke-report.json -RequireAllPassed
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "StagingSmokeChecklistTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|ReleaseDocumentationGuardTests|ProductAdminUiRoadmapSyncTests|DocumentationEncodingTests"
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
git diff --check
```

Результат:

- Generator smoke: OK.
- `-RequireAllPassed` для blocked report: expected failure.
- `StagingSmokeChecklistTests`: `7/7`.
- Backend full suite: `496/496`.
- Frontend unit tests: `65/65`.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-14-staging-smoke-report-generator`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- Push не выполнялся.

## Проверка 2026-06-14: Telegram Stars invoice gate

Что проверялось:

- `TelegramStars` не считается готовым для продаж, если платежный аккаунт просто включен в режиме `bot-only`.
- Telegram bot checkout включает Stars только при явном `ExtraSettingsJson.status = "invoice-flow"`.
- Админская кнопка "Проверить подключение" показывает `Unhealthy` для `bot-only` и `Healthy` для `invoice-flow`.
- Production-настройка Stars не требует web secret key, потому что оплата идет через Telegram invoice update flow.
- Раздел "Что нового" получил релиз `2026-06-14-telegram-stars-invoice-gate`, версия `0.118.0`.

Команды:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "PaymentProviderConfigurationRulesTests|PaymentProviderContractTests|AdminAutomationMvpTests|TelegramBotPurchaseFlowTests|PaymentProviderSandboxSeedTests"
dotnet test backend\VpnPlatform.sln --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm run e2e:console --prefix frontend
npm audit --audit-level=high --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
git diff --check
```

Результат:

- Targeted payment/Telegram suite: `61/61`.
- Backend full suite: `495/495`.
- Frontend unit tests: `65/65`.
- Typecheck/build: OK.
- Browser console E2E: `9/9`.
- Fresh local SQLite smoke: OK, latest `2026-06-14-telegram-stars-invoice-gate`.
- Local SQLite VPS smoke dry-run: OK.
- Secret scan: 0 findings.
- Encoding guard: OK.
- `npm audit --audit-level=high`: OK; остаются 2 moderate advisory по `react-router`.
- Push не выполнялся.

## Проверка 2026-06-14: Telegram webhook boundary

Что проверено:

- Standalone `VpnPlatform.TelegramBot` больше не мапит `/telegram/webhook`.
- Основной Telegram webhook остается в API: `/api/channels/telegram/webhook`.
- Отдельный Telegram bot process отвечает только за LongPolling, очередь Telegram-уведомлений и health endpoints.
- Документация и production example не уводят webhook на старый standalone endpoint.
- Добавлен roadmap-пункт `P1-TG-006` и запись "Что нового" `2026-06-14-telegram-webhook-boundary`, версия `0.117.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "TelegramBotProcessBoundaryTests|ChannelWebhooksControllerTests|TelegramBotFoundationTests|TelegramBotPurchaseFlowTests|AdminTelegramBotSettingsControllerTests"
dotnet build backend/src/VpnPlatform.TelegramBot/VpnPlatform.TelegramBot.csproj --configuration Release
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests|ProductAdminUiRoadmapSyncTests"
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Telegram boundary/API suite: 41/41.
- Standalone TelegramBot build: OK, предупреждений 0.
- Targeted documentation/release/encoding guard suite: OK.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо завершился fail-closed из-за `blocked` checks.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 493/493.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-telegram-webhook-boundary`, версия `0.117.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: API Telegram webhook

Что проверено:

- `/api/channels/telegram/webhook` больше не возвращает `501`, а обрабатывает Telegram update через основной API.
- Runtime-настройки Telegram-бота читаются из админки/БД: `telegram_bot.enabled`, `telegram_bot.mode`, protected BotToken и protected webhook secret.
- Повторная доставка одного `update_id` остается идемпотентной: повторный webhook получает `duplicate` и не отправляет второе сообщение.
- Добавлен roadmap-пункт `P1-TG-005` и запись "Что нового" `2026-06-14-api-telegram-webhook`, версия `0.116.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ChannelWebhooksControllerTests|TelegramBotFoundationTests|TelegramBotPurchaseFlowTests|AdminTelegramBotSettingsControllerTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests|ProductAdminUiRoadmapSyncTests"
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Channel webhook controller tests: 2/2.
- Targeted Telegram/API suite: 39/39.
- Targeted documentation/release/encoding guard suite: OK.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо завершился fail-closed из-за `blocked` checks.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 491/491.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-api-telegram-webhook`, версия `0.116.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: URL validation staging smoke report

Что проверено:

- `scripts/validate-staging-smoke-report.ps1` теперь требует absolute http/https URL для `apiBaseUrl`.
- `publicWebUrl`, `cabinetWebUrl` и `adminWebUrl` остаются опциональными, но если заполнены, тоже должны быть absolute http/https URL.
- Добавлен roadmap-подпункт `P9-TST-007C` и запись "Что нового" `2026-06-14-staging-smoke-report-url-validation`, версия `0.115.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "StagingSmokeChecklistTests|ReleaseDocumentationGuardTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests|ProductAdminUiRoadmapSyncTests"
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Staging smoke checklist guard: 6/6.
- Targeted documentation/release/encoding guard suite: OK.
- Runtime URL check: unsafe report with non-http `apiBaseUrl` rejected.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо завершился fail-closed из-за `blocked` checks.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 489/489.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-staging-smoke-report-url-validation`, версия `0.115.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: consistency staging smoke report

Что проверено:

- `scripts/validate-staging-smoke-report.ps1` теперь отклоняет отчет, где `completedAt` раньше `startedAt`.
- Валидатор отклоняет duplicate check id, чтобы один и тот же smoke-пункт нельзя было скрыть вторым `passed`-дублем.
- Добавлен roadmap-подпункт `P9-TST-007B` и запись "Что нового" `2026-06-14-staging-smoke-report-consistency`, версия `0.114.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "StagingSmokeChecklistTests|ReleaseDocumentationGuardTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests|ProductAdminUiRoadmapSyncTests"
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Staging smoke checklist guard: 5/5.
- Targeted documentation/release/encoding guard suite: OK.
- Runtime consistency check: unsafe report with `completedAt < startedAt` rejected.
- Runtime duplicate check: unsafe report with duplicate check id rejected.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо завершился fail-closed из-за `blocked` checks.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 488/488.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-staging-smoke-report-consistency`, версия `0.114.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: sanitizer staging smoke report

Что проверено:

- `scripts/validate-staging-smoke-report.ps1` дополнительно блокирует типовые утечки из smoke-отчетов: `Cookie:`, `Set-Cookie:`, `.env`, `client_secret`, `api_key`, `private header`, `X-Telegram-Bot-Api-Secret-Token`, `PRODUCTION_ENV_FILE` и `VPS_SSH_KEY`.
- `docs/staging-smoke-checklist.md` и `docs/production-readiness-gate.md` описывают запрет на cookies, `.env`, auth/private headers, client secrets и API keys в evidence.
- Добавлен roadmap-подпункт `P9-TST-007A` и запись "Что нового" `2026-06-14-staging-smoke-secret-sanitizer`, версия `0.113.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "StagingSmokeChecklistTests|ReleaseDocumentationGuardTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests|ProductAdminUiRoadmapSyncTests"
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Staging smoke checklist guard: 4/4.
- Targeted documentation/release/encoding guard suite: OK.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо завершился fail-closed из-за `blocked` checks.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 487/487.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-staging-smoke-secret-sanitizer`, версия `0.113.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: production readiness gate

Что проверено:

- Добавлен fail-closed gate `scripts/assert-production-readiness.ps1` поверх staging/VPS smoke report.
- Gate запускает `validate-staging-smoke-report.ps1 -RequireAllPassed`, поэтому `blocked`, `failed` и `skipped` не могут пройти production-ready проверку.
- Gate дополнительно читает `docs/PRODUCT_COMPLETION_ROADMAP.md` и `docs/release-decision.md` и блокирует production-ready, если открыты `STATE-011`, `STATE-012`, `STATE-013`, P0 live-блокеры, `P11-ACC-002`, `BUG-001`, `BUG-002`, `BUG-003` или решение все еще `staging-ready baseline`.
- Добавлены документ `docs/production-readiness-gate.md`, guard `ProductionReadinessGateTests` и запись "Что нового" `2026-06-14-production-readiness-gate`, версия `0.112.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductionReadinessGateTests|ReleaseDocumentationGuardTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Production readiness guard: 2/2.
- Targeted documentation/release/encoding guard suite: OK.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо завершился ошибкой `Check 'deploy' must be passed when -RequireAllPassed is set.` Это корректный результат до реального staging/VPS smoke report.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 486/486.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-production-readiness-gate`, версия `0.112.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: синхронизация product/admin UI roadmap

Что проверено:

- `docs/product-admin-ui-roadmap.md` больше не содержит старый список незакрытых локальных UX/API/E2E задач, которые уже закрыты в master roadmap и тестах.
- Файл теперь явно разделяет закрытый локальный продуктовый слой и открытые live-блокеры: платежные кабинеты, реальный 3x-ui, VPS admin/live smoke и production-ready решение.
- Добавлен guard `ProductAdminUiRoadmapSyncTests`, который проверяет актуальный latest release, счетчики, отсутствие старых unchecked пунктов и сохранение live-блокеров.
- Добавлена запись "Что нового" `2026-06-14-product-admin-roadmap-sync`, версия `0.111.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ProductAdminUiRoadmapSyncTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Product/admin UI roadmap sync guard: 1/1.
- Targeted documentation/release/encoding guard suite: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 484/484.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-product-admin-roadmap-sync`, версия `0.111.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: синхронизация provisioning secret materialization

Что проверено:

- `BUG-006` переведен из `open` в `Исправлено` в части secret materialization.
- `docs/SECURITY_HARDENING_MVP.md` синхронизирован с фактическим поведением `ProvisioningSecretMaterializer`.
- Добавлен guard `ProvisioningSecretStatusConsistencyTests`, который проверяет roadmap, security docs, production secret storage docs, live Ansible credentials docs и код provisioning materializer/executor.
- Live-блокеры не закрывались: live VPS/provisioning smoke, реальные 3x-ui, платежные кабинеты и `P11-ACC-002` остаются открытыми.
- Добавлена запись "Что нового" `2026-06-14-provisioning-secret-bug-sync`, версия `0.110.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ProvisioningSecretStatusConsistencyTests|BugRegisterConsistencyTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests|ProvisioningSecretMaterializerTests"
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Provisioning secret status consistency guard: 1/1.
- Targeted provisioning/docs/encoding guard suite: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 483/483.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-provisioning-secret-bug-sync`, версия `0.110.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: синхронизация журнала ошибок roadmap

Что проверено:

- `BUG-004` переведен из `open` в `Исправлено`, потому что all-screens browser smoke и console smoke уже закрывают public/cabinet/admin E2E.
- `BUG-005` переведен из `open` в `Исправлено`, потому что документация синхронизирована, а кодировка проверяется `DocumentationEncodingTests`.
- Добавлен guard `BugRegisterConsistencyTests`, который не дает снова оставить локально закрытые баги в статусе `open`.
- Live-блокеры не закрывались: `BUG-001`, `BUG-002`, `BUG-003`, `BUG-006`, P0-платежи, P0-VPN и VPS production smoke остаются открытыми.
- Добавлена запись "Что нового" `2026-06-14-roadmap-bug-register-sync`, версия `0.109.0`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "BugRegisterConsistencyTests|RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Bug register consistency guard: 2/2.
- Targeted documentation/release/encoding guard suite: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 482/482.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-roadmap-bug-register-sync`, версия `0.109.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: синхронизация текущего состояния roadmap

Что проверено:

- Верхний блок `docs/PRODUCT_COMPLETION_ROADMAP.md` обновлен до состояния на 2026-06-14.
- `STATE-014` закрыт как локально выполненная синхронизация текущего статуса документации.
- README, `docs/final-runbook.md`, `docs/release-decision.md`, `CHANGELOG.md`, TEST_RESULTS и seed "Что нового" приведены к latest release `2026-06-14-roadmap-current-state-sync`.
- Добавлен guard `RoadmapCurrentStateTests`, который не дает снова оставить верхний статус roadmap устаревшим.
- Live-блокеры не закрывались: `STATE-011`, `STATE-012`, `STATE-013`, `P11-ACC-002`, live payments, 3x-ui и VPS остаются отдельными задачами.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "RoadmapCurrentStateTests|ReadmeDocumentationTests|FinalDocsChangelogTests|ReleaseDecisionTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Roadmap current state guard: 2/2.
- Targeted documentation/release guard suite: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 480/480.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-roadmap-current-state-sync`, версия `0.108.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: all screens browser smoke

Что проверено:

- Добавлен `frontend/e2e/all-screens.spec.ts`.
- Добавлен Playwright project `all-screens`.
- Добавлен npm-скрипт `e2e:all-screens`.
- `e2e:console` расширен project-ом `all-screens`.
- Проверяются public routes `/`, `/tariffs`, `/faq`, `/help`, `/account`.
- Проверяются cabinet auth screen и авторизованный dashboard.
- Проверяются все admin sections: `dashboard`, `users`, `payments`, `tariffs`, `subscriptions`, `vpn`, `nodes`, `panels`, `support`, `audit`, `bot`, `releases`, `faq`, `content`, `scenarios`, `provisioning`.
- Проверяется отсутствие пустого `body`, `console.error` и `pageerror`.
- Добавлена запись "Что нового" `2026-06-14-all-screens-browser-smoke`, версия `0.107.0`.
- `STATE-010` закрыт для локального mock-based browser smoke; live/staging проверки внешних интеграций остаются отдельными roadmap-задачами.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "AllScreensBrowserSmokeTests|ReadmeDocumentationTests|DocumentationEncodingTests|ReleaseDecisionTests"
npm run e2e:all-screens --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- All screens browser smoke guard: 2/2.
- Targeted documentation/release guard suite: OK.
- `npm run e2e:all-screens --prefix frontend`: 3/3.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 9/9.
- Backend full suite: 478/478.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-all-screens-browser-smoke`, версия `0.107.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: staging smoke checklist

Что проверено:

- Добавлен `docs/staging-smoke-checklist.md`.
- Добавлен безопасный JSON-шаблон `docs/staging-smoke-report.template.json`.
- Добавлен валидатор `scripts/validate-staging-smoke-report.ps1`.
- Валидатор проверяет обязательные пункты staging smoke и запрещает секретные маркеры в отчете.
- Режим `-RequireAllPassed` работает fail-closed: `blocked`, `failed` и `skipped` не могут пройти release gate.
- Добавлена запись "Что нового" `2026-06-14-staging-smoke-checklist`, версия `0.106.0`.
- `P9-TST-007` переведен в состояние `[~]`: чеклист и валидатор готовы, но реальный staging smoke report еще не заполнен.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "StagingSmokeChecklistTests|ReadmeDocumentationTests|DocumentationEncodingTests|ReleaseDecisionTests|VpsProductionSmokeTests"
powershell -ExecutionPolicy Bypass -File scripts\validate-staging-smoke-report.ps1 -ReportPath docs\staging-smoke-report.template.json
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Staging smoke checklist guard: 3/3.
- Targeted documentation/release guard suite: OK.
- Staging smoke report validator: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 6/6.
- Backend full suite: 476/476.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-staging-smoke-checklist`, версия `0.106.0`.
- Encoding guard: OK.
- `git diff --check`: OK.
- Реальный staging/VPS smoke report не выполнялся в этом коммите; для production-ready нужен заполненный отчет с внешнего окружения.

## Проверка 2026-06-14: VPS production smoke runner

Что проверено:

- Добавлен воспроизводимый runner `scripts/vps-production-smoke.ps1` для проверки VPS/staging API.
- Runner покрывает health live/ready, опциональные public/cabinet/admin SPA, admin login/dashboard, публичные тарифы и payment providers, checkout session, регистрацию, claim заказа, payment init, sandbox webhook, историю заказов/платежей, активную подписку, VPN access и latest "Что нового".
- Sandbox webhook работает только при явном `-AllowSandboxWebhook` и запрещен, если `/health/live` сообщает `Production`.
- Добавлена инструкция `docs/vps-production-smoke.md` и ссылка из `docs/README.md`.
- Добавлена запись "Что нового" `2026-06-14-vps-production-smoke-runner`, версия `0.105.0`.
- `P11-ACC-002` дополнен технической частью, но live VPS smoke report остается обязательным для полного закрытия пункта.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "VpsProductionSmokeTests|ReleaseDecisionTests|ReadmeDocumentationTests|DocumentationEncodingTests|ReleaseDocumentationGuardTests"
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl http://127.0.0.1:18102 -AdminEmail fresh-admin@example.test -AdminPassword LocalSmokePassword123! -AllowSandboxWebhook
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- VPS production smoke guard: 3/3.
- Targeted documentation/release guard suite: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 6/6.
- Backend full suite: 473/473.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-vps-production-smoke-runner`, версия `0.105.0`.
- Encoding guard: OK.
- `git diff --check`: OK.
- Live VPS smoke на реальном сервере не выполнялся в этом коммите; для production-ready нужен отдельный smoke report после deploy и ротации секретов.

## Проверка 2026-06-14: release decision

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-007` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен документ `docs/release-decision.md`.
- Release decision зафиксирован как `staging-ready baseline`, не production-ready.
- Production-ready явно заблокирован до закрытия `P11-ACC-002 VPS production smoke`, ротации раскрытых секретов, домена/HTTPS, staging PostgreSQL backup/restore, provider-specific sandbox smoke, 3x-ui smoke и Telegram webhook/invoice smoke.
- README, changelog, final runbook и docs index ссылаются на release decision.
- Добавлен `backend/tests/VpnPlatform.UnitTests/ReleaseDecisionTests.cs`.
- Добавлен release entry `2026-06-14-release-decision` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDecisionTests|ReleaseDocumentationGuardTests|ReadmeDocumentationTests|DocumentationEncodingTests|FinalDocsChangelogTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Release decision guard: 3/3.
- Documentation guard suite: OK.
- Fresh local SQLite smoke: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 6/6.
- Backend full suite: 470/470.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-release-decision`, версия `0.104.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: final docs and changelog

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен корневой `CHANGELOG.md`.
- Добавлен финальный runbook `docs/final-runbook.md`.
- README обновлен ссылками на changelog/runbook, командами `e2e:mobile` и `e2e:console`, статусом `467/467`.
- Индекс `docs/README.md` ссылается на changelog и финальный runbook.
- Secret scan дополнительно исключает runtime `tmp`, чтобы full backend suite не конфликтовал с fresh local smoke artifacts.
- Добавлен `backend/tests/VpnPlatform.UnitTests/FinalDocsChangelogTests.cs`.
- Добавлен release entry `2026-06-14-final-docs-changelog` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "FinalDocsChangelogTests|ReleaseDocumentationGuardTests|ReadmeDocumentationTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Final docs/changelog guard: 3/3.
- Documentation guard suite: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 6/6.
- Fresh local SQLite smoke: OK.
- Backend full suite: 467/467.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-final-docs-changelog`, версия `0.103.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: security final checklist

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен финальный checklist `docs/security-final-checklist.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/SecurityFinalChecklistTests.cs`.
- Guard отражением проверяет все admin-контроллеры на class-level `Authorize`, отсутствие `AllowAnonymous` и write/manage policy у write endpoints.
- Checklist связывает существующие gates: `SecretScanTests`, `SecurityHardeningMvpTests`, `AdminAuthorizationPolicyTests`, `RateLimitingSecurityTests`, `SecurityHeadersTests`, `GitHubSecretsAuditTests`, `ProvisioningSecretMaterializerTests`, `PaymentWebhookIdempotencyContractTests`.
- `scan-secrets.ps1` и `scan-secrets.sh` исключают generated Playwright artifacts (`test-results`, `.playwright-artifacts-*`), чтобы actual secret scan не падал на исчезающих временных файлах после E2E.
- Добавлен release entry `2026-06-14-security-final-checklist` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "SecurityFinalChecklistTests|SecretScanTests|SecurityHardeningMvpTests|AdminAuthorizationPolicyTests|RateLimitingSecurityTests|SecurityHeadersTests|GitHubSecretsAuditTests|ProvisioningSecretMaterializerTests|PaymentWebhookIdempotencyContractTests|DocumentationEncodingTests|ReleaseDocumentationGuardTests|ReadmeDocumentationTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Security final checklist tests: 3/3.
- Targeted security suite: OK.
- Admin anonymous routes: 0.
- Admin write endpoints без write/manage policy: 0.
- Secret scan: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 6/6.
- Security headers: OK.
- Rate limits: OK.
- RBAC matrix: OK.
- GitHub secrets audit: OK.
- Webhook idempotency contract: OK.
- Fresh local SQLite smoke: OK.
- Backend full suite: 464/464.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-security-final-checklist`, версия `0.102.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: browser console smoke

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен npm-скрипт `e2e:console`.
- Проверка покрывает `public-web`, `cabinet`, `admin-panel`, `mobile-public`, `mobile-cabinet`, `mobile-admin`.
- Существующие Playwright E2E public/cabinet/admin падают при `console.error` и `pageerror`.
- Добавлена инструкция `docs/no-console-errors-smoke.md` и ссылка в `docs/README.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/NoConsoleErrorsSmokeTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-14-no-console-errors-smoke`.
- Добавлен release entry `2026-06-14-no-console-errors-smoke` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:console --prefix frontend
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "NoConsoleErrorsSmokeTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Browser console smoke: 6/6.
- Browser console report: `console.error=0`, `pageerror=0`.
- No console errors smoke tests: 1/1.
- README/release/encoding documentation guard: OK.
- Fresh local SQLite smoke: OK.
- Backend full suite: 461/461.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-no-console-errors-smoke`, версия `0.101.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: mobile smoke

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены Playwright-проекты `mobile-public`, `mobile-cabinet`, `mobile-admin`.
- Добавлен npm-скрипт `e2e:mobile`.
- Existing E2E public/cabinet/admin теперь сохраняют mobile-скриншоты при запуске mobile-проектов.
- Добавлена инструкция `docs/mobile-smoke.md` и ссылка в `docs/README.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/MobileSmokeDocumentationTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-14-mobile-smoke`.
- Добавлен release entry `2026-06-14-mobile-smoke` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:mobile --prefix frontend
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "MobileSmokeDocumentationTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Mobile Playwright smoke: 3/3.
- Скриншоты созданы в `frontend/test-results`: `public-mobile.png`, `cabinet-mobile.png`, `admin-mobile.png`.
- Визуальный просмотр скриншотов: public/cabinet/admin не пустые, основные действия доступны; остаточный UX-риск - плотность кабинета и админки на 393px.
- Mobile smoke documentation tests: 1/1.
- README/release/encoding documentation guard: OK.
- Fresh local SQLite smoke: OK.
- Backend full suite: 460/460.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-mobile-smoke`, версия `0.100.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-13: fresh local setup

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `scripts/fresh-local-smoke.ps1` для чистого локального запуска API на SQLite.
- Добавлена инструкция `docs/fresh-local-smoke.md` и ссылка в `docs/README.md`.
- Исправлен SQLite-баг `/api/me/orders`: сортировка `DateTimeOffset` перенесена после `ToListAsync`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/FreshLocalSetupSmokeTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-fresh-local-smoke`.
- Добавлен release entry `2026-06-13-fresh-local-smoke` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "FreshLocalSetupSmokeTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const latest=[...data].sort((a,b)=>new Date(b.releasedAt)-new Date(a.releasedAt))[0]; if (latest.releaseId!=='2026-06-13-fresh-local-smoke'||latest.version!=='0.99.0') throw new Error('unexpected latest'); console.log('latest ok', latest.releaseId, latest.version);"
git diff --check
```

Итог:

- Fresh local smoke tests: 1/1.
- README/release/encoding documentation guard: OK.
- Fresh local smoke script: OK; `tariffs=3`, `providers=8`, sandbox order/payment/subscription/access созданы.
- Backend full suite: 459/459.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-fresh-local-smoke`, версия `0.99.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-13: защита кодировки документации

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/DocumentationEncodingTests.cs`.
- Markdown-документация проверяется на `U+FFFD` и типовые mojibake-маркеры без хранения поврежденных строк в `.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-docs-encoding-guard`.
- Добавлен release entry `2026-06-13-docs-encoding-guard` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "DocumentationEncodingTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/developer-guide.md','docs/README.md','backend/tests/VpnPlatform.UnitTests/DocumentationEncodingTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; const markers=[0xfffd,0x00d0,0x00d1,0x00c3,0x00c2].map(x=>String.fromCharCode(x)); for (const file of files) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('encoding marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Documentation encoding tests: 1/1.
- README/release documentation guard: OK.
- Backend full suite: 458/458.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-docs-encoding-guard`, версия `0.98.0`.
- Encoding guard: OK, `U+FFFD` и типовые UTF-8/CP1251 mojibake-маркеры не найдены в markdown-документации.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-docs-encoding-guard`, версия `0.98.0`.

## Проверка 2026-06-13: руководство разработчика

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `docs/developer-guide.md` с описанием архитектуры, доменных сущностей, state machines, платежей, VPN, provisioning, frontend, БД, безопасности и validation gates.
- Добавлен `docs/README.md` как индекс документации проекта.
- Добавлен `backend/tests/VpnPlatform.UnitTests/DeveloperGuideDocumentationTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-developer-guide`.
- Добавлен release entry `2026-06-13-developer-guide` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "DeveloperGuideDocumentationTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/developer-guide.md','docs/README.md','backend/tests/VpnPlatform.UnitTests/DeveloperGuideDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const strict=['README.md','docs/developer-guide.md','docs/README.md','backend/tests/VpnPlatform.UnitTests/DeveloperGuideDocumentationTests.cs']; const markers=[[0x0421,0x0403],[0x0420,0x045f],[0x0420,0x0491],[0x0421,0x0453]].map(xs=>String.fromCharCode(...xs)); for (const file of strict) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('mojibake marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Developer guide documentation tests: 3/3.
- README/release documentation guard: OK.
- Backend full suite: 457/457.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-developer-guide`, версия `0.97.0`.
- Encoding guard: OK, `U+FFFD` не найден в измененных файлах; developer guide, docs index и новый guard-тест дополнительно проверены на mojibake-маркеры.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-developer-guide`, версия `0.97.0`.

## Проверка 2026-06-13: пользовательская помощь

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `docs/user-guide.md` с полным руководством пользователя.
- В публичном сайте добавлена страница `/help` и ссылка "Помощь" в основной навигации.
- В личном кабинете добавлен блок "Как пользоваться сервисом" с шагами оплаты, подключения, продления и поддержки.
- Добавлены `backend/tests/VpnPlatform.UnitTests/UserGuideDocumentationTests.cs` и `frontend/tests/user-help.test.ts`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-user-help-pages`.
- Добавлен release entry `2026-06-13-user-help-pages` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "UserGuideDocumentationTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/user-guide.md','frontend/apps/public-web/src/App.tsx','frontend/apps/public-web/src/styles.css','frontend/apps/cabinet/src/App.tsx','frontend/apps/cabinet/src/styles.css','frontend/tests/user-help.test.ts','backend/tests/VpnPlatform.UnitTests/UserGuideDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const strict=['README.md','docs/user-guide.md','frontend/tests/user-help.test.ts','backend/tests/VpnPlatform.UnitTests/UserGuideDocumentationTests.cs']; const markers=[[0x0421,0x0403],[0x0420,0x045f],[0x0420,0x0491],[0x0421,0x0453]].map(xs=>String.fromCharCode(...xs)); for (const file of strict) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('mojibake marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- User guide documentation tests: 3/3.
- README/release documentation guard: OK.
- Backend full suite: 454/454.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-user-help-pages`, версия `0.96.0`.
- Encoding guard: OK, `U+FFFD` не найден в измененных файлах; новый пользовательский guide и guard-тесты дополнительно проверены на mojibake-маркеры.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-user-help-pages`, версия `0.96.0`.

## Проверка 2026-06-13: руководство администратора

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `docs/admin-guide.md` с операторским runbook по всем вкладкам админки.
- Документ покрывает вход и RBAC, платежи, тарифы, подписки, VPN-доступы, серверы, 3x-ui панели, Telegram-бот, FAQ, контент, сценарии, "Что нового" и подготовку VPS.
- Добавлен `backend/tests/VpnPlatform.UnitTests/AdminGuideDocumentationTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-admin-operator-guide`.
- Добавлен release entry `2026-06-13-admin-operator-guide` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminGuideDocumentationTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/admin-guide.md','backend/tests/VpnPlatform.UnitTests/AdminGuideDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const strict=['README.md','docs/admin-guide.md','backend/tests/VpnPlatform.UnitTests/AdminGuideDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs']; const markers=[[0x0421,0x0403],[0x0420,0x045f],[0x0420,0x0491],[0x0421,0x0453]].map(xs=>String.fromCharCode(...xs)); for (const file of strict) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('mojibake marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Admin guide documentation tests: 3/3.
- README/release documentation guard: OK.
- Backend full suite: 451/451.
- API build: OK.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-admin-operator-guide`, версия `0.95.0`.
- Encoding guard: OK, `U+FFFD` не найден в измененных файлах; README, admin guide и новые guard-тесты дополнительно проверены на mojibake-маркеры.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-admin-operator-guide`, версия `0.95.0`.

## Проверка 2026-06-13: README на русском языке

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `README.md` переписан как основной русский входной документ проекта.
- README описывает назначение платформы, состав монорепозитория, запуск без Docker, ручной запуск, Docker/VPS контекст, платежи, VPN, окружения и актуальный статус.
- Добавлен `backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-readme-russian-local-runbook`.
- Добавлен release entry `2026-06-13-readme-russian-local-runbook` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const strict=['README.md','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs']; const markers=[[0x0421,0x0403],[0x0420,0x045f],[0x0420,0x0491],[0x0421,0x0453]].map(xs=>String.fromCharCode(...xs)); for (const file of strict) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('mojibake marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- README documentation tests: 3/3.
- Release documentation guard: OK.
- Backend full suite: 448/448.
- API build: OK.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-readme-russian-local-runbook`, версия `0.94.0`.
- Encoding guard: OK, `U+FFFD` не найден в измененных файлах; README и новый README guard дополнительно проверены на mojibake-маркеры.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-readme-russian-local-runbook`, версия `0.94.0`.

## Проверка 2026-06-13: payment provider contract tests

Что проверено:

- Закрыт roadmap-пункт `P9-TST-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/PaymentProviderContractTests.cs`.
- Contract gate проверяет реальные DI-регистрации Application/Infrastructure для всех `PaymentProvider`.
- Проверяется один `IPaymentProvider` на каждый enum-провайдер, webhook verifier/status mapper для всех web-провайдеров и bot-only/fail-closed контракт Telegram Stars.
- Local sandbox checkout для YooMoney, YooKassa, RoboKassa, CloudPayments, TBank, Prodamus, Stripe и PayPal проходит без внешних API и без реальных денег.
- Добавлена документация `docs/payment-provider-contract-tests.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-payment-provider-contract-tests`.
- Добавлен release entry `2026-06-13-payment-provider-contract-tests` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "PaymentProviderContractTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/payment-provider-contract-tests.md','backend/tests/VpnPlatform.UnitTests/PaymentProviderContractTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Payment provider contract tests: 12/12.
- Backend full suite: 445/445.
- API build: OK.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-payment-provider-contract-tests`, версия `0.93.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-payment-provider-contract-tests`, версия `0.93.0`.

## Проверка 2026-06-13: Playwright E2E admin

Что проверено:

- Закрыт roadmap-пункт `P9-TST-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `frontend/e2e/admin.spec.ts` для проверки админки.
- `frontend/scripts/playwright-webservers.mjs` теперь поднимает public-web, cabinet и admin-panel.
- Добавлен npm-скрипт `e2e:admin`.
- CI и staging-validation теперь запускают `npm run e2e:public`, `npm run e2e:cabinet` и `npm run e2e:admin`.
- Добавлена документация `docs/playwright-admin-e2e.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-playwright-admin-e2e`.
- Добавлен release entry `2026-06-13-playwright-admin-e2e` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:admin --prefix frontend
npm run e2e:public --prefix frontend
npm run e2e:cabinet --prefix frontend
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/playwright-admin-e2e.md','frontend/playwright.config.ts','frontend/e2e/admin.spec.ts','frontend/scripts/playwright-webservers.mjs','frontend/package.json','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Admin Playwright E2E: 1/1.
- Public Playwright E2E: 1/1.
- Cabinet Playwright E2E: 1/1.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK.
- Backend full suite: 433/433.
- API build: OK.
- JSON релизов валиден: latest seed `2026-06-13-playwright-admin-e2e`, версия `0.92.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-playwright-admin-e2e`, версия `0.92.0`.

## Проверка 2026-06-13: Playwright E2E cabinet

Что проверено:

- Закрыт roadmap-пункт `P9-TST-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `frontend/e2e/cabinet.spec.ts` для проверки личного кабинета.
- Добавлен `frontend/scripts/playwright-webservers.mjs`, который поднимает public-web и cabinet на стабильных портах для Playwright.
- Добавлен npm-скрипт `e2e:cabinet`.
- CI и staging-validation теперь запускают `npm run e2e:public` и `npm run e2e:cabinet`, а HTML-report сохраняют из `frontend/playwright-report/e2e`.
- Добавлена документация `docs/playwright-cabinet-e2e.md`, обновлена `docs/playwright-public-e2e.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-playwright-cabinet-e2e`.
- Добавлен release entry `2026-06-13-playwright-cabinet-e2e` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:cabinet --prefix frontend
npm run e2e:public --prefix frontend
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/playwright-public-e2e.md','docs/playwright-cabinet-e2e.md','frontend/playwright.config.ts','frontend/e2e/cabinet.spec.ts','frontend/scripts/playwright-webservers.mjs','frontend/package.json','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Cabinet Playwright E2E: 1/1.
- Public Playwright E2E: 1/1.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK.
- Backend full suite: 433/433.
- API build: OK.
- JSON релизов валиден: latest seed `2026-06-13-playwright-cabinet-e2e`, версия `0.91.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-playwright-cabinet-e2e`, версия `0.91.0`.

## Проверка 2026-06-13: Playwright E2E public

Что проверено:

- Закрыт roadmap-пункт `P9-TST-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены `frontend/playwright.config.ts` и `frontend/e2e/public.spec.ts`.
- Добавлены npm-скрипты `e2e` и `e2e:public`.
- Public E2E проверяет главную, managed FAQ preview, тарифы, web-provider select, public checkout session, переход на `/account`, сохраненную покупку и FAQ search.
- CI и staging-validation теперь устанавливают Chromium, запускают `npm run e2e:public` и сохраняют HTML-report artifact.
- Добавлена документация `docs/playwright-public-e2e.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-playwright-public-e2e`.
- Добавлен release entry `2026-06-13-playwright-public-e2e` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:public --prefix frontend
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/playwright-public-e2e.md','frontend/playwright.config.ts','frontend/e2e/public.spec.ts','frontend/package.json','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Public Playwright E2E: 1/1.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Backend full suite: 433/433.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-playwright-public-e2e`, версия `0.90.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-playwright-public-e2e`, версия `0.90.0`.

## Проверка 2026-06-13: frontend validation gate

Что проверено:

- Закрыт roadmap-пункт `P9-TST-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Текущий обязательный frontend unit suite обновлен до `64/64`.
- Добавлены `scripts/validate-frontend.ps1` и `scripts/validate-frontend.sh` для Windows/Linux: npm lock/config safety, `npm ci`, typecheck, production build, unit tests и high-severity audit.
- Добавлена документация `docs/frontend-validation-gate.md` с критериями готовности и локальными командами.
- Добавлен frontend guard-test `frontend-validation-gate.test.ts`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-frontend-validation-gate`.
- Добавлен release entry `2026-06-13-frontend-validation-gate` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
[System.Management.Automation.PSParser]::Tokenize((Get-Content scripts/validate-frontend.ps1 -Raw), [ref]$null) | Out-Null
powershell -ExecutionPolicy Bypass -File scripts/validate-frontend.ps1
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/frontend-validation-gate.md','scripts/validate-frontend.ps1','scripts/validate-frontend.sh','frontend/tests/frontend-validation-gate.test.ts','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- PowerShell syntax parse `scripts/validate-frontend.ps1`: OK.
- Frontend validation gate: OK.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK, есть только moderate advisory по `react-router`.
- Release documentation guard: OK.
- JSON релизов валиден: latest seed `2026-06-13-frontend-validation-gate`, версия `0.89.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-frontend-validation-gate`, версия `0.89.0`.

## Проверка 2026-06-13: backend validation gate

Что проверено:

- Закрыт roadmap-пункт `P9-TST-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Текущий обязательный backend suite обновлен до `433/433`.
- Добавлен `scripts/validate-backend.ps1` для Windows/PowerShell: validation safety, secret scan, restore, build, full backend tests, dotnet tools, EF migrations list и EF model drift.
- `scripts/validate-backend.sh` остается Linux/Git Bash entrypoint с тем же обязательным набором.
- Добавлена документация `docs/backend-validation-gate.md` с safe defaults и командами доказательства.
- Добавлен `BackendValidationGateTests`.
- `SecretScanTests` расширен проверкой, что PowerShell backend gate запускает `scan-secrets.ps1`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-backend-validation-gate`.
- Добавлен release entry `2026-06-13-backend-validation-gate` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
[System.Management.Automation.PSParser]::Tokenize((Get-Content scripts/validate-backend.ps1 -Raw), [ref]$null) | Out-Null
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "BackendValidationGateTests|SecretScanTests|ReleaseDocumentationGuardTests"
powershell -ExecutionPolicy Bypass -File scripts/validate-backend.ps1
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/BackendValidationGateTests.cs','backend/tests/VpnPlatform.UnitTests/SecretScanTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/backend-validation-gate.md','scripts/validate-backend.ps1','scripts/validate-backend.sh','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- PowerShell syntax parse `scripts/validate-backend.ps1`: OK.
- Targeted backend validation/release/secret guard tests: 9/9.
- Backend full suite: 433/433.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-backend-validation-gate`, версия `0.88.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-backend-validation-gate`, версия `0.88.0`.

## Проверка 2026-06-13: post-deploy smoke

Что проверено:

- Закрыт roadmap-пункт `P8-CI-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `scripts/post-deploy-smoke.sh` с проверками API live/ready, `/metrics`, `/api/public/payments/providers`, public web, cabinet web и admin web.
- `.github/workflows/deploy-vps.yml` запускает шаг `Post-deploy smoke` после docker или systemd deploy.
- Workflow вычисляет URL по режиму: docker public `:5173`, systemd public `VITE_PUBLIC_WEB_URL` или `http://VPS_HOST`, cabinet `:5174`, admin `:5175`, API `:8080`.
- Добавлены optional secrets `POST_DEPLOY_API_URL`, `POST_DEPLOY_PUBLIC_WEB_URL`, `POST_DEPLOY_CABINET_WEB_URL`, `POST_DEPLOY_ADMIN_WEB_URL`.
- `.github/github-secrets.audit.json` и `docs/github-secrets-audit.md` обновлены под новые optional smoke secrets.
- Добавлена документация `docs/post-deploy-smoke.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-post-deploy-smoke`.
- Добавлен release entry `2026-06-13-post-deploy-smoke` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
& 'C:\Program Files\Git\bin\bash.exe' -n scripts/post-deploy-smoke.sh
powershell -ExecutionPolicy Bypass -File scripts/audit-github-secrets.ps1 -DryRun
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "PostDeploySmokeTests|GitHubSecretsAuditTests|ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['.github/workflows/deploy-vps.yml','.github/github-secrets.audit.json','backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/PostDeploySmokeTests.cs','backend/tests/VpnPlatform.UnitTests/GitHubSecretsAuditTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/post-deploy-smoke.md','docs/github-deployment.md','docs/github-secrets-audit.md','scripts/post-deploy-smoke.sh','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- `bash -n scripts/post-deploy-smoke.sh`: OK.
- GitHub secrets audit dry-run: OK, optional `POST_DEPLOY_*` names совпадают с workflow references.
- Local post-deploy smoke на чистой SQLite API и тестовом HTML-сервере: OK, проверены `/health/live`, `/health/ready`, `/metrics`, `/api/public/payments/providers`, public/cabinet/admin HTML.
- Targeted post-deploy/secrets/release guard tests: 9/9.
- Backend full suite: 430/430.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-post-deploy-smoke`, версия `0.87.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: latest release `2026-06-13-post-deploy-smoke`, версия `0.87.0`; серверов `1`, provisioning-запусков `0`.
- Live VPS post-deploy smoke будет выполнен GitHub Actions после следующего deploy; из локальной среды production deploy не запускался.

## Проверка 2026-06-13: безопасная очистка VPS

Что проверено:

- Закрыт roadmap-пункт `P8-CI-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `scripts/vps-maintenance.sh` с dry-run по умолчанию и реальной очисткой только через `--apply`.
- Скрипт печатает `df -h`, `free -h`, `du -sh` до и после maintenance.
- Старые release-директории удаляются только внутри `APP_DIR/releases`, только с именем git sha и с сохранением `KEEP_RELEASES`.
- Защищены `APP_DIR`, `APP_DIR/shared`, `APP_DIR/current`, `APP_DIR/releases`, текущий symlink release и любые пути вне `APP_DIR`.
- App logs очищаются только внутри `APP_DIR/logs`; production `.env`, database dumps и рабочие каталоги не трогаются.
- `journalctl --vacuum-time`, `apt-get clean/autoclean` включены как безопасная системная очистка.
- Docker prune включается только через `--docker-prune`, не выполняет `docker volume prune`.
- Добавлена документация `docs/vps-maintenance.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-vps-maintenance-safe-cleanup`.
- Добавлен release entry `2026-06-13-vps-maintenance-safe-cleanup` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
bash -n scripts/vps-maintenance.sh
bash scripts/vps-maintenance.sh --dry-run --app-dir /tmp/vpn-platform-maintenance-smoke
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "VpsMaintenanceScriptTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/VpsMaintenanceScriptTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/vps-maintenance.md','docs/github-deployment.md','scripts/vps-maintenance.sh','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- `bash -n scripts/vps-maintenance.sh`: OK.
- Local maintenance dry-run: OK, команды удаления напечатаны как `[dry-run]`, рабочие данные не удалялись.
- Targeted VPS maintenance tests: 4/4.
- Targeted release/docs guard tests: 3/3.
- Backend full suite: 427/427.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-vps-maintenance-safe-cleanup`, версия `0.86.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-13-vps-maintenance-safe-cleanup`, версия `0.86.0`; серверов `1`, provisioning-запусков `0`.
- Live VPS cleanup не запускался из этой среды: скрипт подготовлен, но реальная очистка требует явного запуска оператором с `--apply`.

## Проверка 2026-06-13: аудит GitHub Secrets

Что проверено:

- Закрыт roadmap-пункт `P8-CI-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `.github/github-secrets.audit.json` со списком required/optional GitHub Actions secret names без значений.
- Required secrets совпадают с explicit gate в `.github/workflows/deploy-vps.yml`: `VPS_HOST`, `VPS_USER`, `VPS_PORT`, `VPS_APP_DIR`, `VPS_SSH_KEY`, `PRODUCTION_ENV_FILE`.
- Optional secrets покрывают остальные references workflow: `VPS_DEPLOY_MODE`, `VITE_API_BASE_URL`, `VITE_PUBLIC_WEB_URL`.
- Registry secrets явно отмечены как not required, потому что текущие workflows не пушат container images в registry.
- Добавлен `scripts/audit-github-secrets.ps1`: `-DryRun` проверяет локальный конфиг и workflow, live-режим получает только names через GitHub REST API и не выводит значения.
- Добавлена документация `docs/github-secrets-audit.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-github-secrets-audit`.
- Добавлен release entry `2026-06-13-github-secrets-audit` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/audit-github-secrets.ps1 -DryRun
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "GitHubSecretsAuditTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['.github/github-secrets.audit.json','backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/GitHubSecretsAuditTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/github-secrets-audit.md','docs/github-deployment.md','scripts/audit-github-secrets.ps1','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- GitHub secrets audit dry-run: OK, config и workflow references совпадают; GitHub API не вызывался.
- Targeted GitHub secrets audit tests: 3/3.
- Targeted release/docs guard tests: 3/3.
- Backend full suite: 423/423.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-github-secrets-audit`, версия `0.85.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-13-github-secrets-audit`, версия `0.85.0`; серверов `1`, provisioning-запусков `0`.
- Live GitHub secrets audit не выполнялся из этой среды: `GITHUB_TOKEN/GH_TOKEN` в env отсутствует. Скрипт готов к запуску с токеном, который может читать repository Actions secrets metadata; значения secrets GitHub API не возвращает.

## Проверка 2026-06-12: required checks для main

Что проверено:

- Закрыт roadmap-пункт `P8-CI-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен конфиг `.github/branch-protection.required-checks.json` для веток `main` и `master`.
- Required checks синхронизированы с job names workflow `validation`: backend, frontend, provisioning/Ansible и docker build.
- Добавлен `scripts/configure-branch-protection.ps1` с GitHub REST API, `-DryRun`, чтением token только из env и проверкой applied contexts после применения.
- Добавлена документация `docs/github-required-checks.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-12-required-checks-main`.
- Добавлен release entry `2026-06-12-required-checks-main` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/configure-branch-protection.ps1 -DryRun
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "BranchProtectionGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['.github/branch-protection.required-checks.json','backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/BranchProtectionGuardTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/github-required-checks.md','docs/github-deployment.md','scripts/configure-branch-protection.ps1','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Branch protection dry-run: OK, payload собран для `Xsenus/vpn-platform`, ветки `main/master`, 4 required checks.
- Targeted branch protection guard tests: 3/3.
- Targeted release/docs guard tests: 3/3.
- Backend full suite: 420/420.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-required-checks-main`, версия `0.84.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-12-required-checks-main`, версия `0.84.0`; серверов `1`, provisioning-запусков `0`.
- Live GitHub branch protection не применялся из этой среды: `gh` CLI не установлен, `GITHUB_TOKEN/GH_TOKEN` в env отсутствует. Для применения нужен запуск `scripts/configure-branch-protection.ps1` с токеном repository administration write.

## Проверка 2026-06-12: auto-detect deploy docker/systemd

Что проверено:

- Закрыт roadmap-пункт `P8-CI-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `.github/workflows/deploy-vps.yml` пишет requested/selected режим, `docker_detected` и причину выбора в `::notice`.
- Workflow добавляет блок `VPS deploy mode` в `$GITHUB_STEP_SUMMARY`.
- `auto` выбирает `docker` только при наличии `docker` и `docker compose version` на VPS, иначе выбирает `systemd`.
- Добавлена документация `docs/deploy-vps-auto-detect.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-12-deploy-mode-auto-detect`.
- Добавлен release entry `2026-06-12-deploy-mode-auto-detect` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "DeployWorkflowGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['.github/workflows/deploy-vps.yml','backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/DeployWorkflowGuardTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/deploy-vps-auto-detect.md','docs/github-deployment.md','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted deploy workflow guard tests: 2/2.
- Targeted release/docs guard tests: 3/3.
- Backend full suite: 417/417.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-deploy-mode-auto-detect`, версия `0.83.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-12-deploy-mode-auto-detect`, версия `0.83.0`; серверов `1`, provisioning-запусков `0`.

## Проверка 2026-06-12: runbook live provisioning

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `docs/live-provisioning-runbook.md` с preflight, Ansible syntax-check, SSH/known_hosts, live flags, тегами ноды, API-порядком precheck/deploy, ручным runner dry-run, rollback/failure path, smoke и fail-closed правилами.
- `docs/provisioning.md` ссылается на live provisioning runbook.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-12-live-provisioning-runbook`.
- Добавлен guard `Live_Provisioning_Runbook_Should_Cover_Operator_Gates`.
- Добавлен release entry `2026-06-12-live-provisioning-runbook` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/live-provisioning-runbook.md','docs/provisioning.md','TEST_RESULTS.md','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted release/docs guard tests: 3/3.
- Backend full suite: 415/415.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-live-provisioning-runbook`, версия `0.82.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-12-live-provisioning-runbook`, версия `0.82.0`; серверов `1`, provisioning-запусков `0`.

## Проверка 2026-06-12: rollback состояния VPS

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `ProvisioningWorker` снимает snapshot `VpnNode` перед deploy и при ошибке возвращает node status, health, availability и эксплуатационные поля к значениям до deploy.
- При failed deploy run остается `Failed`, а `VpnNode.ProvisioningStatus` становится `Failed`, чтобы оператор видел инцидент.
- В run добавляется шаг `Rollback node state`, а audit получает событие `provisioning.rollback_applied`.
- Support context и Telegram-уведомление получают redacted-контекст ошибки без SSH/password/token утечек.
- Добавлена документация `docs/vps-provisioning-rollback.md`.
- Добавлен release entry `2026-06-12-vps-provisioning-rollback` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "OwnVps_Deploy_Failure_Should_Roll_Back_Node_State_And_Surface_Admin_Context|OwnVps_DryRun_E2E_Should_Protect_Credential_Process_Mock_Deploy_Create_Access_And_Admin_Visibility|OwnVps_DryRun_Failure_Should_Create_Support_Context_And_Retry_Without_Duplicates"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "OwnVps_Deploy_Failure_Should_Roll_Back_Node_State_And_Surface_Admin_Context"
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/vps-provisioning-rollback.md','backend/src/VpnPlatform.Infrastructure/HostedServices/ProvisioningWorker.cs','backend/tests/VpnPlatform.UnitTests/SandboxE2EScenariosMvpTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted rollback/E2E tests: 3/3.
- Targeted rollback regression: 1/1.
- Backend full suite: 414/414.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-vps-provisioning-rollback`, версия `0.81.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-12-vps-provisioning-rollback`, версия `0.81.0`; серверов `1`, provisioning-запусков `0`.

## Проверка 2026-06-12: отчет precheck VPS

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `AnsibleProvisioningExecutor` формирует JSON `Precheck report` для dry-run/mock и live runner, сохраняет его отдельным шагом и добавляет в summary log.
- Admin API возвращает `precheckReportPreview` в списке provisioning runs и полный `precheckReport` в деталях запуска.
- Админка показывает отчет precheck в разделе «Подготовка VPS».
- `precheck-node.yml` инспектирует OS, ports, disk, RAM, firewall, Docker, systemd и 3x-ui availability.
- Добавлена документация `docs/vps-precheck-report.md`.
- Добавлен release entry `2026-06-12-vps-precheck-report` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "OwnVpsProvisioningMvpTests|ProvisioningSecretMaterializerTests"
npm test -- --test-name-pattern "provisioning"
npm run typecheck --workspace apps/admin-panel
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
npm run build --workspace apps/admin-panel
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/vps-precheck-report.md','TEST_RESULTS.md','backend/src/VpnPlatform.Infrastructure/Provisioning/AnsibleProvisioningExecutor.cs','backend/src/VpnPlatform.Api/Controllers/Admin/AdminOperationsController.cs','backend/tests/VpnPlatform.UnitTests/OwnVpsProvisioningMvpTests.cs','frontend/packages/api-client/src/index.ts','frontend/apps/admin-panel/src/App.tsx','infra/ansible/playbooks/precheck-node.yml']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted provisioning tests: 19/19.
- Frontend API tests: 61/61.
- Admin panel typecheck: OK.
- Admin panel production build: OK.
- Backend full suite: 413/413.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-vps-precheck-report`, версия `0.80.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `POST /api/admin/servers/{id}/precheck`, `/api/admin/provisioning-runs`, `/api/admin/provisioning-runs/{id}`; latest release `2026-06-12-vps-precheck-report`, версия `0.80.0`; серверов `1`; precheck run `ReadyToDeploy`; `precheckReportPreview` содержит `x3ui`; полный `precheckReport` содержит `firewall`.

## Проверка 2026-06-12: live Ansible credentials

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `AnsibleProvisioningExecutor` добавляет temporary SSH key path и temporary `secrets` directory path в список known secrets для redaction.
- Runner output/stderr/step logs больше не сохраняют raw private key, protected payload, legacy key path, temporary key path и panel password.
- Добавлена документация `docs/live-ansible-credentials.md`.
- Добавлен release entry `2026-06-12-live-ansible-credentials` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ProvisioningSecretMaterializerTests|OwnVpsProvisioningMvpTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/live-ansible-credentials.md','backend/src/VpnPlatform.Infrastructure/Provisioning/AnsibleProvisioningExecutor.cs','backend/tests/VpnPlatform.UnitTests/ProvisioningSecretMaterializerTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted provisioning secret tests: 17/17.
- Backend full suite: 411/411.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-live-ansible-credentials`, версия `0.79.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`, `/api/public/payments/providers`; latest release `2026-06-12-live-ansible-credentials`, версия `0.79.0`; серверов `1`, provisioning-запусков `0`, публичных провайдеров `8`.

## Проверка 2026-06-12: границы режимов provisioning VPS

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Backend разделяет `dry-run`, `validation-deploy`, `live-deploy-blocked` и `live-deploy` через `ProvisioningModeDescriptor`.
- Admin API отдаёт `mode`, `riskLevel`, `liveDeployAllowed`, `nextAction`, `operatorWarning` и отдельные `deployMode*` поля для следующего deploy после dry-run.
- Админка показывает режимы и риски в списке серверов и provisioning-запусков, блокирует запрещённый live deploy и оставляет `Precheck VPS` безопасным действием.
- Добавлена документация `docs/provisioning-modes.md`.
- Добавлен release entry `2026-06-12-provisioning-mode-boundaries` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "FullyQualifiedName~OwnVpsProvisioningMvpTests"
npm test -- --runInBand
npm run typecheck --workspace apps/admin-panel
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
npm run build --workspace apps/admin-panel
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/provisioning-modes.md','backend/src/VpnPlatform.Application/Services/ProvisioningService.cs','backend/src/VpnPlatform.Api/Controllers/Admin/AdminOperationsController.cs','backend/tests/VpnPlatform.UnitTests/OwnVpsProvisioningMvpTests.cs','frontend/packages/api-client/src/index.ts','frontend/apps/admin-panel/src/App.tsx','frontend/tests/api-client.test.ts']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- `OwnVpsProvisioningMvpTests`: 12/12.
- Frontend tests: 61/61.
- Admin-panel typecheck/build: OK.
- Backend full suite: 410/410.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-provisioning-mode-boundaries`, версия `0.78.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`, `/api/public/payments/providers`; latest release `2026-06-12-provisioning-mode-boundaries`, версия `0.78.0`; sandbox server вернул `provisioningMode=live-deploy-blocked`, `provisioningRiskLevel=blocked`, `liveDeployAllowed=false`; публичные провайдеры `8`.
- Browser smoke админки: `http://127.0.0.1:5175/` открыл login screen, title `VPN Platform — админ-панель`, React root найден, консольных `error` logs нет; dev server остановлен.

## Проверка 2026-06-12: secret scan gate

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены `scripts/scan-secrets.ps1` и `scripts/scan-secrets.sh`.
- Scanner проверяет Telegram, Stripe/OpenAI, GitHub, GitLab, AWS, Google, Slack tokens и PEM private keys.
- `validate-backend.sh` и `validate-all.sh` запускают secret scan до build/test шагов.
- `check-validation-safety.sh` проверяет наличие scanner и базовых token/private-key паттернов.
- Добавлен allowlist для тестовых fixture и локальных placeholders.
- Добавлена документация `docs/secret-scan.md`.
- Добавлен release entry `2026-06-12-secret-scan-gate` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/secret-scan.md','TEST_RESULTS.md','scripts/scan-secrets.ps1','scripts/scan-secrets.sh','scripts/validate-all.sh','scripts/validate-backend.sh','scripts/check-validation-safety.sh','backend/tests/VpnPlatform.UnitTests/SecretScanTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter SecretScanTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-secret-scan-gate`, версия `0.77.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- PowerShell secret scan: `Files scanned: 386. Findings: 0`.
- Bash scan локально не запущен: в текущей Windows-среде нет `/bin/bash`; bash-скрипт покрыт `SecretScanTests` и рассчитан на Linux CI.
- `SecretScanTests`: 3/3.
- Release docs tests: 14/14.
- Backend full suite: 408/408.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-secret-scan-gate`, версия `0.77.0`.

## Проверка 2026-06-12: security headers API и frontend

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Backend API получил `SecurityHeadersMiddleware`.
- API выставляет `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`.
- `Strict-Transport-Security` выставляется только в `Production`.
- Development Swagger UI не получает API CSP, чтобы не ломать локальную документацию.
- Frontend Dockerfiles `public-web`, `cabinet`, `admin-panel` копируют общий `frontend/nginx.security.conf`.
- nginx-конфиг frontend содержит CSP, HSTS, security headers и SPA fallback `try_files`.
- Production CORS остается allow-list based через `Cors:AllowedOrigins` и startup validator.
- Добавлена документация `docs/security-headers.md`.
- Добавлен release entry `2026-06-12-security-headers` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/security-headers.md','backend/src/VpnPlatform.Api/Middleware/SecurityHeadersMiddleware.cs','backend/src/VpnPlatform.Api/Program.cs','backend/tests/VpnPlatform.UnitTests/SecurityHeadersTests.cs','frontend/nginx.security.conf','frontend/Dockerfile.public-web','frontend/Dockerfile.cabinet','frontend/Dockerfile.admin-panel']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter SecurityHeadersTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
npm run typecheck
npm run build
npm test
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-security-headers`, версия `0.76.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `SecurityHeadersTests`: 5/5.
- Release docs tests: 14/14.
- Frontend typecheck: OK.
- Frontend build: OK.
- Frontend tests: 61/61.
- Backend full suite: 405/405.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-security-headers`, версия `0.76.0`; `/health/live` вернул `nosniff`, `DENY`, `no-referrer`, `Permissions-Policy`, API CSP, без HSTS в Local.

## Проверка 2026-06-12: rate limiting публичного API

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены `ApiRateLimitPolicies` с policy `auth-sensitive`, `public-checkout`, `webhook`.
- `Program.cs` подключает `builder.Services.AddRateLimiter(ApiRateLimitPolicies.Configure)` и `app.UseRateLimiter()`.
- Auth endpoints `register/login/refresh/forgot-password/reset-password` защищены policy `auth-sensitive`.
- Публичные checkout endpoints защищены policy `public-checkout`.
- Платежные webhook и channel webhook controllers защищены policy `webhook`.
- При превышении лимита API возвращает `429 Too Many Requests` с problem JSON.
- Добавлена документация `docs/rate-limiting.md`.
- Добавлен release entry `2026-06-12-api-rate-limiting` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/rate-limiting.md','backend/src/VpnPlatform.Api/Security/ApiRateLimitPolicies.cs','backend/src/VpnPlatform.Api/Program.cs','backend/src/VpnPlatform.Api/Controllers/Auth/AuthController.cs','backend/src/VpnPlatform.Api/Controllers/Public/OrdersController.cs','backend/src/VpnPlatform.Api/Controllers/Webhooks/PaymentWebhooksController.cs','backend/src/VpnPlatform.Api/Controllers/Channels/ChannelWebhooksController.cs','backend/tests/VpnPlatform.UnitTests/RateLimitingSecurityTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter RateLimitingSecurityTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-api-rate-limiting`, версия `0.75.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `RateLimitingSecurityTests`: 11/11.
- Release docs tests: 14/14.
- Backend full suite: 400/400.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-api-rate-limiting`, версия `0.75.0`; превышение `POST /api/auth/forgot-password` вернуло `429`.

## Проверка 2026-06-12: RBAC-матрица админки

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлена единая матрица `AdminPolicies.PolicyRoles` для всех admin-policy.
- `Program.cs` регистрирует authorization policies из матрицы, без ручного дублирования списка.
- Роль `User` исключена из всех admin-policy.
- Роли `ReadOnly`, `SupportAgent`, `FinanceManager`, `Operator`, `Admin`, `SuperAdmin` разведены по read/write/manage-доступам.
- Добавлены runtime authorization tests для разрешенных и запрещенных комбинаций ролей.
- Добавлена документация `docs/rbac-policy-matrix.md`.
- Добавлен release entry `2026-06-12-rbac-policy-matrix` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/rbac-policy-matrix.md','backend/src/VpnPlatform.Application/Common/AdminPolicies.cs','backend/src/VpnPlatform.Api/Program.cs','backend/tests/VpnPlatform.UnitTests/AdminAuthorizationPolicyTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter AdminAuthorizationPolicyTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-rbac-policy-matrix`, версия `0.74.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `AdminAuthorizationPolicyTests`: 33/33.
- Release docs tests: 14/14.
- Backend full suite: 389/389.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-rbac-policy-matrix`, версия `0.74.0`.

## Проверка 2026-06-12: безопасная ротация секретов

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Платежная ротация продолжает писать `payment_provider.secret.rotate`.
- Добавлен `server.secret.rotate` для SSH credential и panel password.
- Добавлен `telegram_bot.secret.rotate` для BotToken и SecretToken.
- При ротации server secrets создаются новые `secretref:ssh:*` и `secretref:panel:*`, но API/audit не раскрывают raw secret, protected payload или `secretref:*`.
- Telegram settings продолжают сохранять BotToken/SecretToken через write-only поля и audit содержит только флаги `rotatedBotToken` / `rotatedSecretToken`.
- Добавлена документация `docs/secret-rotation.md`.
- Добавлен release entry `2026-06-12-secret-rotation-audit` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "SecurityHardeningMvpTests|AdminTelegramBotSettingsControllerTests|AuditLogMvpTests" --logger "console;verbosity=minimal"
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.releasedAt,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/secret-rotation.md','backend/src/VpnPlatform.Api/Controllers/Admin/AdminOperationsController.cs','backend/src/VpnPlatform.Api/Controllers/Admin/AdminTelegramBotSettingsController.cs','backend/tests/VpnPlatform.UnitTests/SecurityHardeningMvpTests.cs','backend/tests/VpnPlatform.UnitTests/AdminTelegramBotSettingsControllerTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- `SecurityHardeningMvpTests|AdminTelegramBotSettingsControllerTests|AuditLogMvpTests`: 14/14.
- JSON релизов валиден: latest `2026-06-12-secret-rotation-audit`, версия `0.73.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- Release docs tests: 14/14.
- Backend full suite: 378/378.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-secret-rotation-audit`, версия `0.73.0`.

## Проверка 2026-06-12: production secret storage для provisioning

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `ProvisioningSecretMaterializer` для временной материализации protected SSH private key.
- `AnsibleProvisioningExecutor` теперь умеет передавать runner только path к временно созданному key file и удаляет materialized secret в `finally`.
- Password-based live SSH, `validation-placeholder:*`, legacy protected values в `SshPrivateKeyPath` и missing protected payload при наличии `SshCredentialRef` остаются fail-closed.
- Runner stdout/stderr и step output редактируются с учетом protected payload и расшифрованного plaintext.
- Добавлены docs: `docs/production-secret-storage.md`; обновлены `docs/TODO_SECURE_PROVISIONING_SECRETS.md` и `docs/SECRET_MIGRATION_PLAN.md`.
- Добавлен release entry `2026-06-12-production-provisioning-secret-storage` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProvisioningSecretMaterializerTests|OwnVpsProvisioningMvpTests|SecurityHardeningMvpTests" --logger "console;verbosity=minimal"
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','docs/production-secret-storage.md','docs/TODO_SECURE_PROVISIONING_SECRETS.md','docs/SECRET_MIGRATION_PLAN.md','backend/src/VpnPlatform.Infrastructure/Provisioning/ProvisioningSecretMaterializer.cs','backend/src/VpnPlatform.Infrastructure/Provisioning/AnsibleProvisioningExecutor.cs','backend/tests/VpnPlatform.UnitTests/ProvisioningSecretMaterializerTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- `ProvisioningSecretMaterializerTests|OwnVpsProvisioningMvpTests|SecurityHardeningMvpTests`: 22/22.
- JSON релизов валиден: latest `2026-06-12-production-provisioning-secret-storage`, версия `0.72.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- Release docs tests: 14/14.
- Backend full suite: 377/377.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-production-provisioning-secret-storage`, версия `0.72.0`.
- Live Ansible на реальном VPS не запускался: проверена локальная безопасная materialization/cleanup логика и fail-closed ветки.

## Проверка 2026-06-12: аудит PostgreSQL schema

Что проверено:

- Закрыт roadmap-пункт `P5-DB-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены `scripts/audit-postgres-schema.sh` и `scripts/audit-postgres-schema.ps1`.
- EF-only режим без PostgreSQL формирует `ef-migrations.txt`, `postgres-migrations-idempotent.sql`, `audit-metadata.env` и явный `postgres-schema-snapshot.txt` с пометкой, что `DATABASE_URL` не задан.
- PostgreSQL режим при наличии `DATABASE_URL` и `psql` снимает sanitized snapshot таблиц, колонок, nullable-полей, индексов и FK через `information_schema`/`pg_indexes`, без чтения пользовательских данных.
- Добавлен runbook `docs/postgres-schema-audit.md`.
- Добавлен `PostgresSchemaAuditTests`: проверяет PostgreSQL EF metadata, PK у mapped entities, индексы, FK, nullable metadata, migration chain, audit-скрипты и документацию.
- Добавлен release entry `2026-06-12-postgres-schema-audit` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
$null = [scriptblock]::Create((Get-Content -Path scripts\audit-postgres-schema.ps1 -Raw))
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/postgres-schema-audit.md','scripts/audit-postgres-schema.sh','scripts/audit-postgres-schema.ps1','backend/tests/VpnPlatform.UnitTests/PostgresSchemaAuditTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter PostgresSchemaAuditTests --logger "console;verbosity=minimal"
$env:SCHEMA_AUDIT_DIR = Join-Path $PWD 'artifacts\postgres-schema-audit-local'; powershell -ExecutionPolicy Bypass -File scripts\audit-postgres-schema.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-postgres-schema-audit`, версия `0.71.0`.
- PowerShell syntax check: OK.
- Encoding guard: OK, `U+FFFD` не найден.
- `PostgresSchemaAuditTests`: 3/3.
- `scripts\audit-postgres-schema.ps1`: OK, EF-only artifacts созданы в `artifacts/postgres-schema-audit-local`.
- Реальный `psql` snapshot не запускался локально, потому что `DATABASE_URL` для отдельной PostgreSQL-БД не задан; runbook описывает staging/VPS запуск.
- Bash syntax check не запускался: доступный `bash` указывает на WSL без `/bin/bash`; Linux-скрипт покрыт static guard в `PostgresSchemaAuditTests`.
- Release docs tests: 14/14.
- Backend full suite: 373/373.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-postgres-schema-audit`, версия `0.71.0`.

## Проверка 2026-06-12: backup/restore PostgreSQL для VPS

Что проверено:

- Закрыт roadmap-пункт `P5-DB-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `scripts/backup-db.sh` усилен проверкой `pg_dump`, custom dump, `.dump.list` и retention через `BACKUP_RETENTION_DAYS`.
- Добавлены PowerShell и Linux restore-скрипты: `scripts/restore-db.sh`, `scripts/restore-db.ps1`.
- Добавлен PowerShell backup-скрипт `scripts/backup-db.ps1`.
- Restore требует `BACKUP_FILE` и отдельный `RESTORE_DATABASE_URL`; совпадение с `DATABASE_URL` блокируется без `RESTORE_ALLOW_DATABASE_URL_MATCH=true`.
- `scripts/apply-migrations.sh` передает `BACKUP_RETENTION_DAYS` в pre-migration backup.
- `backups/` добавлен в `.gitignore`, чтобы dump-файлы не попадали в репозиторий.
- Добавлен runbook `docs/postgres-backup-restore.md` с test restore в отдельную БД `vpnplatform_restore_check`.
- Добавлен release entry `2026-06-12-postgres-backup-restore-runbook` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter DatabaseBackupRestoreScriptsTests --logger "console;verbosity=minimal"
$null = [scriptblock]::Create((Get-Content -Path scripts\backup-db.ps1 -Raw)); $null = [scriptblock]::Create((Get-Content -Path scripts\restore-db.ps1 -Raw))
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','docs/postgres-backup-restore.md','scripts/backup-db.sh','scripts/restore-db.sh','scripts/backup-db.ps1','scripts/restore-db.ps1','scripts/apply-migrations.sh','backend/tests/VpnPlatform.UnitTests/DatabaseBackupRestoreScriptsTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `DatabaseBackupRestoreScriptsTests`: 1/1 пройдено.
- PowerShell syntax check: `backup-db.ps1` и `restore-db.ps1` валидны.
- Реальный `pg_dump/pg_restore` не запускался в этой Windows-среде, потому что локальный PostgreSQL test restore не поднят; runbook и скрипты готовы для VPS/Linux или Windows с установленными PostgreSQL client tools.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 370/370 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-postgres-backup-restore-runbook`, версия `0.70.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: чистая БД, `/health/live`, `/health/ready`, `/metrics`, `/api/auth/login`, `/api/app-version/latest`; latest release `2026-06-12-postgres-backup-restore-runbook`, версия `0.70.0`, `readyChecks=2`, metrics содержат `vpnplatform_http_requests_total` и `vpnplatform_api_uptime_seconds`.

## Проверка 2026-06-12: локальный seed данных и VPN sandbox

Что проверено:

- Закрыт roadmap-пункт `P5-DB-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `DbInitializer.SeedDemoDataAsync` теперь создает sandbox VPN-инфраструктуру для чистой локальной БД: node group `sandbox`, panel `sandbox-x3ui-panel`, default inbound `sandbox-default-vless`, node `sandbox-vpn-node`.
- Sandbox-нода создается в статусах `Ready` и `Healthy`, доступна для новых пользователей и содержит протоколы `vless,vmess,trojan`.
- Добавлен SQLite acceptance-тест локального seed: admin user, тарифы, sandbox payments, Telegram Stars disabled, VPN panel/inbound/node, читаемый русский контент и идемпотентность повторного seed.
- Добавлен release entry `2026-06-12-local-seed-vpn-infrastructure` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter PaymentProviderSandboxSeedTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','backend/src/VpnPlatform.Infrastructure/Services/SystemServices.cs','backend/tests/VpnPlatform.UnitTests/PaymentProviderSandboxSeedTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `PaymentProviderSandboxSeedTests`: 2/2 пройдено.
- API Release build: пройдено без предупреждений и ошибок.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 369/369 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-local-seed-vpn-infrastructure`, версия `0.69.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: чистая БД, `/health/live`, `/health/ready`, `/metrics`, `/api/auth/login`, `/api/app-version/latest`, `/api/public/tariffs`, `/api/public/payments/providers`, `/api/admin/servers`, `/api/admin/vpn-panels`; latest release `2026-06-12-local-seed-vpn-infrastructure`, версия `0.69.0`, тарифы `3`, провайдеры `8`, серверы `1`, панели `1`, `sandbox-vpn-node=true`, `sandbox-x3ui-panel=true`.

## Проверка 2026-06-12: кроссплатформенный EF model drift gate

Что проверено:

- Закрыт roadmap-пункт `P5-DB-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `scripts/check-ef-drift.ps1` для локальной проверки EF model drift на Windows/PowerShell.
- PowerShell drift-check использует `dotnet ef migrations has-pending-model-changes`, безопасные env-переменные, временную диагностическую миграцию `__ModelDriftCheck` и cleanup без изменения snapshot.
- `EfModelDriftTests` расширен acceptance-проверкой Linux и PowerShell drift-скриптов, env safety и документации.
- Обновлена инструкция `docs/ef-model-drift-check.md`.
- Добавлен release entry `2026-06-12-ef-drift-powershell-gate` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter EfModelDriftTests --logger "console;verbosity=minimal"
powershell -ExecutionPolicy Bypass -File scripts\check-ef-drift.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','docs/ef-model-drift-check.md','scripts/check-ef-drift.ps1','scripts/check-validation-safety.sh','backend/tests/VpnPlatform.UnitTests/EfModelDriftTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `EfModelDriftTests`: 2/2 пройдено.
- PowerShell EF drift-check: `[OK] EF model has no pending migration changes.`
- Bash safety-check: не запускался в текущей Windows-среде, потому что доступный `bash` пытается стартовать WSL без `/bin/bash`; покрытие Linux-скрипта проверено статическим .NET acceptance-тестом и остается для GitHub/Linux среды.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 368/368 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-ef-drift-powershell-gate`, версия `0.68.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: `/health/live`, `/health/ready`, `/metrics`, `/api/auth/login`, `/api/app-version/latest`; `live=ok`, `ready=Ready`, `readyChecks=2`, latest release `2026-06-12-ef-drift-powershell-gate`, версия `0.68.0`, metrics содержат `vpnplatform_http_requests_total` и `vpnplatform_api_uptime_seconds`.

## Проверка 2026-06-12: observability API

Что проверено:

- Закрыт roadmap-пункт `P4-BE-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `CorrelationIdMiddleware` нормализует входящий `X-Correlation-Id`, возвращает его в ответе, добавляет в `HttpContext.Items`, `Activity` tag и logger scope.
- Добавлен `RequestObservabilityMiddleware`: каждый HTTP-запрос пишет структурный лог с методом, путем, статусом, временем выполнения и correlation id.
- Endpoint `/health/live` возвращает service, environment, uptime и correlation id.
- Endpoint `/health/ready` проверяет локальную БД и возвращает счетчики пользователей, активных тарифов, включенных платежных провайдеров, pending outbox, failed provisioning и unhealthy VPN-нод.
- Endpoint `/metrics` возвращает Prometheus text format с `vpnplatform_api_info`, `vpnplatform_api_uptime_seconds`, `vpnplatform_http_requests_in_flight`, `vpnplatform_http_requests_total`, `vpnplatform_http_request_duration_ms_sum`.
- Добавлен release entry `2026-06-12-observability-mvp` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --no-restore
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --no-restore --filter ObservabilityMvpTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','backend/src/VpnPlatform.Api/Middleware/CorrelationIdMiddleware.cs','backend/src/VpnPlatform.Api/Middleware/RequestObservabilityMiddleware.cs','backend/src/VpnPlatform.Api/Observability/ApiObservabilityMetrics.cs','backend/src/VpnPlatform.Api/Observability/ObservabilityHealthService.cs','backend/tests/VpnPlatform.UnitTests/ObservabilityMvpTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- API build: пройдено без предупреждений и ошибок.
- `ObservabilityMvpTests`: 3/3 пройдено.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 367/367 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-observability-mvp`, версия `0.67.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: `/health/live`, `/health/ready`, `/metrics`, `/api/auth/login`, `/api/app-version/latest`; `live=ok`, `ready=Ready`, `readyChecks=2`, latest release `2026-06-12-observability-mvp`, версия `0.67.0`, metrics содержат `vpnplatform_http_requests_total` и `vpnplatform_api_uptime_seconds`.

## Проверка 2026-06-12: журнал аудита в админке

Что проверено:

- Закрыт roadmap-пункт `P4-BE-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен backend endpoint `/api/admin/audit-logs` с фильтрами `action`, `entityType`, `actorType`, `search`, `from`, `to`, `limit`.
- В админке добавлен раздел `Аудит` с фильтрами и просмотром `beforeJson/afterJson`.
- Действия с платежными провайдерами пишут audit-события `payment_provider.create`, `payment_provider.update`, `payment_provider.enabled.set`, `payment_provider.check`.
- Ротация SecretKey/webhook secret пишется отдельным событием `payment_provider.secret.rotate` без раскрытия секретных значений.
- Переходы статусов платежей из webhook/recheck пишутся как системные события `payment.status.changed`.
- Добавлен release entry `2026-06-12-admin-audit-log` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~AuditLogMvpTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
npm run typecheck
npm test
npm run build
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data[data.length-1];console.log(data.length,last.releaseId,last.version,last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `AuditLogMvpTests`: 3/3 пройдено.
- Backend full suite: 364/364 пройдено.
- Frontend typecheck: пройдено для public-web, cabinet и admin-panel.
- Frontend tests: 61/61 пройдено.
- Frontend build: public-web, cabinet и admin-panel собраны успешно.
- App releases JSON: валиден, последний релиз `2026-06-12-admin-audit-log`, версия `0.66.0`.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/admin/releases?search=2026-06-12-admin-audit-log`, `/api/admin/audit-logs?limit=20`, `/api/admin/audit-logs?action=auth.login&limit=20`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-12-admin-audit-log`, версия `0.66.0`, audit `1`, audit search `1`, публичные провайдеры `8`, публичные тарифы `3`.

## Проверка 2026-06-12: конкурентная обработка оплаты

Что проверено:

- Закрыт roadmap-пункт `P4-BE-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `PaymentOrchestrator` получил общий `PaymentProcessingGate` по заказу и свежий DB snapshot внутри gate, чтобы параллельные webhook/recheck не запускали повторную активацию.
- Конкурентная вставка одного `PaymentWebhookEvent` теперь возвращает идемпотентный ответ, если уникальное событие уже сохранено другим потоком.
- Sandbox-выбор VPN-ноды больше не сортирует `decimal` в SQL, поэтому локальная SQLite-БД проходит активацию оплаты.
- Добавлен release entry `2026-06-12-payment-concurrency-guard` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~PaymentConcurrencyTests"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~PaymentWebhookProcessingTests|FullyQualifiedName~PaymentWebhookIdempotencyContractTests|FullyQualifiedName~PaymentConcurrencyTests"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data[data.length-1];console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','backend/src/VpnPlatform.Application/Common/PaymentProcessingGate.cs','backend/src/VpnPlatform.Application/Services/PaymentOrchestrator.cs','backend/src/VpnPlatform.Application/Services/NodeAllocationService.cs','backend/tests/VpnPlatform.UnitTests/PaymentConcurrencyTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `PaymentConcurrencyTests`: 2/2 пройдено на SQLite.
- Targeted payment tests: 28/28 пройдено.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 361/361 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-payment-concurrency-guard`, версия `0.65.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/admin/releases?search=2026-06-12-payment-concurrency-guard`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-12-payment-concurrency-guard`, версия `0.65.0`, история `50`, поиск релиза `1`, публичные провайдеры `8`, публичные тарифы `3`.

## Проверка 2026-06-11: идемпотентность платежных webhook

Что проверено:

- Закрыт roadmap-пункт `P4-BE-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `PaymentOrchestrator` теперь нормализует webhook event id: если провайдер не прислал внешний event id, используется стабильный ключ `payload:<sha256>`.
- Добавлены contract-тесты идемпотентности для всех значений `PaymentProvider`: YooMoney, YooKassa, RoboKassa, TelegramStars, CloudPayments, TBankAcquiring, Prodamus, Stripe и PayPal.
- Повторный webhook не создает вторую подписку, второй VPN-доступ или второй `PaymentWebhookEvent`.
- Добавлен release entry `2026-06-11-payment-webhook-idempotency` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --no-restore --filter "PaymentWebhookIdempotencyContractTests|PaymentWebhookProcessingTests|YooMoneyWebhookProcessingTests|RoboKassaWebhookProcessingTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data[data.length-1];console.log(data.length,last.releaseId,last.version,last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- Targeted payment webhook tests: 42/42 пройдено.
- Backend full suite: 359/359 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-11-payment-webhook-idempotency`, версия `0.64.0`.
- Contract-тесты: повтор webhook с внешним event id и без него идемпотентен для каждого `PaymentProvider`.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/admin/releases?search=2026-06-11-payment-webhook-idempotency`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-11-payment-webhook-idempotency`, версия `0.64.0`.

## Проверка 2026-06-11: state machines доменных статусов

Что проверено:

- Закрыт roadmap-пункт `P4-BE-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен общий `StatusStateMachine` для заказов, платежей, подписок, VPN-доступов и provisioning runs.
- Guard подключен к платежному оркестратору, Telegram Stars successful payment flow, подпискам, lifecycle VPN-доступа, X3-UI синхронизации, админским действиям и provisioning worker.
- Добавлен release entry `2026-06-11-state-machine-guards` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Добавлены unit-тесты матрицы разрешенных/запрещенных переходов и интеграционный тест позднего cancelled-webhook после successful payment.

Команды и результат:

```powershell
dotnet build backend\VpnPlatform.sln --configuration Release --no-restore
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data[data.length-1];console.log(data.length,last.releaseId,last.version,last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- Backend build: успешно, 0 warnings, 0 errors.
- Backend full suite: 341/341 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-11-state-machine-guards`, версия `0.63.0`.
- State machine unit tests: разрешают рабочие переходы и запрещают невозможные откаты для `OrderStatus`, `PaymentStatus`, `SubscriptionStatus`, `AccessCredentialStatus`, `ProvisioningRunStatus`.
- Webhook integration test: поздний `payment.canceled` после `payment.succeeded` получает failed processing и не откатывает `PaymentStatus.Succeeded`, `OrderStatus.Completed` и созданную подписку.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/admin/releases?search=2026-06-11-state-machine-guards`, `/api/app-version/admin/releases/overview`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-11-state-machine-guards`, версия `0.63.0`, публичные провайдеры `8`, публичные тарифы `3`, поиск релиза `1`.

## Проверка 2026-06-11: русская локализация интерфейса

Что проверено:

- Закрыт roadmap-пункт `P3-UX-007` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- API-клиент больше не использует англоязычные `Failed to ...` fallback-сообщения для пользовательских ошибок.
- В админке локализованы подписи платежных провайдеров, Telegram-бота, серверов, VPN-панелей, источников релизов и режимов выдачи.
- В `@vpn-platform/ui` локализованы бейджи `agent`, `manual`, `auto`, `hybrid`, `LongPolling`.
- Добавлен release entry `2026-06-11-russian-localization-check` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const p='backend/src/VpnPlatform.Api/AppReleases/releases.json'; const data=JSON.parse(fs.readFileSync(p,'utf8')); console.log(data.length, data[data.length-1].releaseId, data[data.length-1].version);"
cd frontend
npm test
npm run typecheck
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs'); const files=['frontend/apps/admin-panel/src/App.tsx','frontend/apps/cabinet/src/App.tsx','frontend/apps/public-web/src/App.tsx','frontend/packages/ui/src/index.tsx','frontend/packages/api-client/src/index.ts','frontend/tests/api-client.test.ts']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd)) || /\?{3,}/.test(text)) throw new Error(file); } console.log('encoding guard ok');"
```

Результат:

- App releases JSON: валиден, последний релиз `2026-06-11-russian-localization-check`, версия `0.62.0`.
- Frontend tests: 60/60 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`, `/api/app-version/admin/releases?search=2026-06-11-russian-localization-check`, `/api/public/payments/providers`, `/api/public/tariffs`; релиз `2026-06-11-russian-localization-check`, версия `0.62.0`, `mark-seen=true`, публичные провайдеры `8`, публичные тарифы `3`.
- Browser smoke: public `http://127.0.0.1:19173`, cabinet `http://127.0.0.1:19174`, admin `http://127.0.0.1:19175/#payments` и `#bot`; runtime-ошибок в консоли нет, признаков битой кодировки и `Failed to ...` в видимом тексте нет.
- Encoding guard: в пользовательских frontend-источниках нет символа `U+FFFD` и трех вопросительных знаков подряд.

## Проверка 2026-06-11: доступность интерфейса

Что проверено:

- Закрыт roadmap-пункт `P3-UX-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- В `@vpn-platform/ui` улучшены доступные состояния общих компонентов: `StatusBadge`, `CopyButton`, `PasswordField`, `SecretField`, `ConfirmButton`.
- Для копирования добавлен скрытый live-region `sr-only`, для паролей и секретов - `aria-describedby`, для статусов - `role="status"`, для подтверждения - `role="dialog"` и `aria-haspopup="dialog"`.
- Усилен видимый focus ring и добавлено правило `prefers-reduced-motion: reduce`.
- Окно "Что нового" в кабинете получает фокус при открытии, закрывается по Escape, возвращает фокус на предыдущий элемент, имеет `aria-describedby` и отмечает выбранный релиз через `aria-current`.
- Обращения в поддержку получили выбранное состояние через `aria-pressed` и понятное `aria-label`.
- Добавлен release entry `2026-06-11-accessibility-polish` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const p='backend/src/VpnPlatform.Api/AppReleases/releases.json'; const data=JSON.parse(fs.readFileSync(p,'utf8')); console.log(data.length, data[data.length-1].releaseId, data[data.length-1].version);"
cd frontend
npm test
npm run typecheck
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- App releases JSON: валиден, последний релиз `2026-06-11-accessibility-polish`, версия `0.61.0`.
- Frontend tests: 60/60 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-11-accessibility-polish`, версия `0.61.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`, публичные провайдеры `8`, публичные тарифы `3`.
- Browser accessibility smoke: public-web, cabinet и admin-panel открыты на 390 px; у интерактивных элементов нет пустых доступных имен, есть skip link/main, правило reduced motion подключено, горизонтального переполнения нет.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах выполнена, совпадений нет.

## Проверка 2026-06-11: адаптивность интерфейса

Что проверено:

- Закрыт roadmap-пункт `P3-UX-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- В `@vpn-platform/ui` добавлены responsive-токены `--page-x`, `--page-y`, `--page-bottom` и явные CSS-переломы для 1280, 1024, 768 и 390 px.
- Админка получила tablet/mobile правила для login-экрана, боковой навигации, вкладок разделов, платежных провайдеров, редактора релизов и пользовательских карточек.
- Публичный сайт получил отдельные правила для hero, тарифов, FAQ, карты покрытия, CTA и футера на desktop/tablet/mobile.
- Личный кабинет получил адаптивные правила для текущего VPN-доступа, поддержки, платежных метаданных и окна "Что нового".
- Добавлен release entry `2026-06-11-responsive-breakpoints` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Frontend static guard проверяет обязательные breakpoint-правила и ключевые responsive CSS-блоки.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const p='backend/src/VpnPlatform.Api/AppReleases/releases.json'; const data=JSON.parse(fs.readFileSync(p,'utf8')); console.log(data.length, data[data.length-1].releaseId, data[data.length-1].version);"
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- App releases JSON: валиден, последний релиз `2026-06-11-responsive-breakpoints`, версия `0.60.0`.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-11-responsive-breakpoints`, версия `0.60.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`, публичные провайдеры `8`, публичные тарифы `3`.
- Browser responsive check: public-web, cabinet и admin-panel открыты через временные Vite-серверы на 1280 и 390 px; `scrollWidth` не превышает `clientWidth`, горизонтального переполнения нет, ключевые заголовки и карточки отрисованы.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах выполнена, совпадений нет.

## Проверка 2026-06-11: проверка форм админки

Что проверено:

- Закрыт roadmap-пункт `P3-UX-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- В `@vpn-platform/ui` добавлен общий `FormValidationSummary`.
- Формы платежных провайдеров, тарифов, VPN-серверов, 3x-ui панелей, inbound-правил и сценариев получили явные валидаторы и видимый summary ошибок.
- Submit-кнопки этих форм блокируются по тем же массивам ошибок, которые показываются пользователю.
- Добавлен release entry `2026-06-11-admin-form-validation` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Static guard проверяет валидаторы, `FormValidationSummary` и disabled-состояния по ошибкам.

Команды и результат:

```powershell
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-admin-form-validation`, версия `0.59.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: навигация админки по разделам

Что проверено:

- Закрыт roadmap-пункт `P3-UX-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Админская навигация переведена на grouped tablist с группами `Операции`, `Продажи`, `VPN`, `Контент`.
- Добавлены мобильный `admin-section-select`, переходы `Предыдущий` / `Следующий`, описания активных разделов и hash-переходы без прыжка страницы.
- Основные разделы админки получили `role="tabpanel"` и связь с tab через `aria-labelledby={adminSectionTabId(...)}`.
- Добавлен release entry `2026-06-11-admin-section-navigation` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Static guard проверяет grouped navigation, tab semantics, mobile select, prev/next и panel-связи.

Команды и результат:

```powershell
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-admin-section-navigation`, версия `0.58.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: современный login админки

Что проверено:

- Закрыт roadmap-пункт `P3-UX-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Экран входа админки получил валидацию `validateAdminLogin`, `role="alert"` для ошибок, remember email без хранения пароля и подсказки по sessionStorage.
- Добавлен release entry `2026-06-11-admin-login-polish` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Static guard проверяет, что пароль не записывается в `sessionStorage`, а email сохраняется отдельно через `ADMIN_EMAIL_STORAGE_KEY`.
- Local SQLite проверяет, что seed релизов загружается в БД, новый релиз становится latest и его можно отметить просмотренным.

Команды и результат:

```powershell
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-admin-login-polish`, версия `0.57.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: единая дизайн-система

Что проверено:

- Закрыт roadmap-пункт `P3-UX-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- В `@vpn-platform/ui` добавлены `designTokens`, `SegmentedTabs`, `StateBlock` и `DataTableLite`.
- Public-web и cabinet используют общий компонент вкладок для входа и регистрации вместо локальных копий обработчика клавиатуры.
- Добавлен release entry `2026-06-11-design-system-foundation` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Local SQLite проверяет, что seed релизов загружается в БД, новый релиз становится latest и его можно отметить просмотренным.

Команды и результат:

```powershell
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-design-system-foundation`, версия `0.56.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: обязательные записи «Что нового» для этапов

Что проверено:

- Закрытый roadmap-пункт `P2-ADM-REL-002` отмечен в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен release entry `2026-06-11-release-note-guard` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Backend static guard проверяет, что закрытые пункты P2.7 имеют запись в `releases.json` и упоминание releaseId в `TEST_RESULTS.md`.
- Backend static guard проверяет, что seed-файл «Что нового» содержит пользовательские title/summary/items и допустимые типы пунктов.
- Local SQLite проверяет, что seed релизов загружается в БД и latest/history видят новый релиз.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend narrow tests: 14/14 пройдено.
- Backend full suite: 301/301 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-release-note-guard`, версия `0.55.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление разделом «Что нового»

Что проверено:

- Backend `/api/app-version/latest` и `/api/app-version/history` показывают пользователю только активные опубликованные релизы.
- Backend `/api/app-version/mark-seen` фиксирует просмотр опубликованного релиза и отклоняет скрытые или будущие релизы.
- Backend `/api/app-version/admin/releases` принимает фильтры `visibility`, `source`, `search`.
- Backend `/api/app-version/admin/releases/overview` возвращает счетчики опубликованных, запланированных, скрытых релизов, просмотры и последний опубликованный релиз.
- Админка показывает сводку релизов, фильтры истории и статус «Запланировано» для будущих публикаций.
- Кабинет продолжает открывать окно «Что нового», загружать историю и вызывать `mark-seen`.
- Добавлена запись «Что нового» `2026-06-11-app-version-management`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend narrow tests: 7/7 пройдено.
- Backend full suite: 299/299 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `POST /api/app-version/admin/releases`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`, `/api/app-version/admin/releases?visibility=published&source=manual&search=smoke`; опубликованный релиз стал latest, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`, фильтр вернул 1 запись, будущий релиз на `mark-seen` вернул HTTP 404.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление FAQ в админке

Что проверено:

- Backend `/api/admin/faq` принимает фильтры `category`, `visibility`, `search` и возвращает отсортированные вопросы.
- Backend `/api/admin/faq/overview` возвращает счетчики активных/скрытых вопросов, публикации на главной и странице FAQ, категории и дубли.
- Backend блокирует одинаковый вопрос в одной категории при создании и редактировании, включая русские категории с разным регистром.
- Админка показывает сводку FAQ, фильтры по категории/видимости/поиску, статусы публикации и предупреждение о дублях.
- Public API `/api/public/content/faq` и `/api/public/content/faq?home=true` получает опубликованные вопросы после админского изменения.
- Добавлена запись «Что нового» `2026-06-11-admin-faq-management`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FaqControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend narrow tests: 7/7 пройдено.
- Backend full suite: 297/297 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `POST /api/admin/faq`, `/api/admin/faq/overview`, `/api/admin/faq?category=Подключение&visibility=home&search=qr`, `/api/public/content/faq`, `/api/public/content/faq?home=true`; создан 1 вопрос, фильтр вернул 1 запись, public/home вернули 1 запись, дубль с другим регистром вернул HTTP 400.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: готовность контента главной в админке

Что проверено:

- Backend `/api/admin/site-content/home-readiness` возвращает готовность обязательных блоков главной: hero, SEO, преимущества, тарифный заголовок, CTA, footer и текст после оплаты.
- Backend `/api/admin/site-content/home-defaults` создает недостающие обязательные блоки и восстанавливает пустые или выключенные значения безопасными дефолтами.
- Backend запрещает дубли ключей контента при создании и редактировании.
- Админка показывает карточку готовности главной, списки отсутствующих/выключенных/пустых/задублированных ключей и кнопку восстановления дефолтов.
- Public API `/api/public/content/home` получает восстановленные опубликованные блоки.
- Добавлена запись «Что нового» `2026-06-11-admin-home-content-readiness`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "SiteContentControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend narrow tests: 5/5 пройдено.
- Backend full suite: 294/294 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/site-content/home-readiness`, `/api/admin/site-content/home-defaults`, `/api/admin/site-content?group=home`, `/api/public/content/home`; до восстановления `isReady=false`, после восстановления создано 18 блоков, `isReady=true`, public API вернул 18 блоков с hero и SEO.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: безопасные возвраты в админке

Что проверено:

- Backend `/api/admin/payments` возвращает refund readiness: `refundSupported`, `canRefund`, `refundableAmount`, `refundBlockers`.
- Backend `POST /api/admin/payments/{id}/refund` выполняет preflight и не вызывает провайдера, если возврат недоступен.
- В админке платежи показывают доступную сумму возврата, причины блокировки, поле суммы и причину возврата.
- Неподдерживаемые провайдеры, неуспешные платежи, полностью возвращенные суммы и неполная настройка аккаунта блокируются до вызова provider API.
- Добавлена запись «Что нового» `2026-06-11-admin-refund-readiness`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminRefundManagementTests|AdminAuthorizationPolicyTests"
cd frontend
npm test -- --test-name-pattern "refund readiness"
```

Результат:

- Backend narrow tests: 25/25 пройдено.
- Backend full suite: 292/292 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/payments`, `/api/admin/refunds`, `/api/admin/payments/{missingId}/refund`; refund несуществующего платежа корректно вернул HTTP 400.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление заказами в админке

Что проверено:

- Backend `/api/admin/orders` принимает фильтры `status/search`, сохраняет SQLite-safe сортировку и возвращает последний платеж заказа.
- Backend `/api/admin/orders/{id}/recheck-payment` проверяет последнюю платежную попытку заказа через общий payment orchestrator.
- Раздел админки «Заказы» получил фильтр статуса, поиск, расширенные карточки, переходы к пользователю/платежу/подписке и кнопку «Проверить оплату».
- Исправлен frontend-контракт `recheckAdminPayment`: теперь он типизирован как `PaymentStatusResult`, а не как платежная попытка.
- Добавлена запись «Что нового» `2026-06-11-admin-order-management`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminOrderManagementTests|AdminAuthorizationPolicyTests"
cd frontend
npm test -- --test-name-pattern "admin order"
```

Результат:

- Backend narrow tests: 25/25 пройдено.
- Backend full suite: 289/289 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 58/58 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/orders?status=PendingPayment&search=smoke`, `/api/admin/orders/{missingId}/recheck-payment`; recheck несуществующего заказа корректно вернул HTTP 400.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление подписками в админке

Что проверено:

- Backend умеет активировать подписку, снимать блокировку/отмену и включать текущий VPN-доступ через `VpnAccessLifecycleService`.
- Backend умеет синхронизировать текущий VPN-доступ подписки через endpoint `/api/admin/subscriptions/{id}/sync-access`.
- Раздел админки «Подписки» получил действия: активировать, продлить, синхронизировать доступ, заблокировать/разблокировать и отменить.
- Новые действия подписки покрыты authorization policy: синхронизация доступа требует `VpnManage`.
- Добавлена запись «Что нового» `2026-06-11-admin-subscription-management`.

Команды и результат:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend tests: 283/283 пройдено.
- Frontend typecheck: пройден.
- Frontend tests: 57/57 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/subscriptions`, `/api/admin/subscriptions/{missingId}/activate`, `/api/admin/subscriptions/{missingId}/sync-access` прошли; новые маршруты корректно вернули 404 для отсутствующей подписки.

## Проверка 2026-06-11: карточка пользователя в админке

Что проверено:

- Backend user overview для админки возвращает безопасный профиль пользователя, Telegram-аккаунты, заказы, платежи, подписки, VPN-доступы и обращения поддержки без `PasswordHash` и приватных metadata.
- Раздел админки «Пользователи» показывает структурированную карточку: профиль, быстрые метрики, причины внимания оператора, подписки, заказы, платежи, VPN-доступы, Telegram и поддержку.
- Локальный запуск API на временной SQLite-БД работает без Docker; DataProtection-ключи направлены в рабочую папку проекта, чтобы не зависеть от прав к Windows-профилю.
- Проверка кодировки: символов `U+FFFD` в README/docs/backend/frontend/.env.example не найдено.

Команды и результат:

```powershell
dotnet build backend\VpnPlatform.sln --configuration Release --no-restore
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
git diff --check
rg -n "<символ U+FFFD>" README.md docs backend\src frontend\apps frontend\packages .env.example
```

Результат:

- Backend build: 0 ошибок, 0 предупреждений.
- Backend tests: 280/280 пройдено.
- Frontend typecheck: пройден.
- Frontend tests: 57/57 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/users?search=admin&status=Active`, `/api/admin/users/{id}/overview` прошли успешно.
- `git diff --check`: замечаний нет.
- Поиск символа `U+FFFD`: совпадений нет.

## Что исправлено

- Backend переведен на `.NET 9` (`net9.0`), `global.json` переключен на SDK 9, `dotnet-ef` обновлен до 9.0.16.
- EF Core, ASP.NET Core, Microsoft.Extensions и Npgsql EF Core обновлены до последних patch-версий ветки 9.x.
- Исправлены оставшиеся падения backend suite: Telegram payment flow, sandbox E2E, сериализация EF-графов, sync-события 3x-ui и проверка provisioning precheck.
- JSON payload для Telegram-уведомлений теперь сохраняет русский текст читаемо, без `\uXXXX`.
- Отдельный worker-проект удалён: operational hosted workers запускаются внутри `VpnPlatform.Api`.
- Исправлена совместимость lifecycle/outbox workers с SQLite в Local-режиме.
- Локальный no-Docker запуск продолжает работать через SQLite.
- Staging на VPS переведён с временного SQLite на PostgreSQL 16.
- Исправлено падение кабинета `TypeError: e.toLowerCase is not a function`: общий `StatusBadge` теперь безопасно обрабатывает enum/number/null значения.
- Добавлена no-op EF migration `AlignEfModelSnapshotNet9`, которая синхронизирует snapshot модели с EF Core 9 без DDL-изменений.
- Local sandbox режим платежей расширен на `ASPNETCORE_ENVIRONMENT=Local` для YooKassa/YooMoney/RoboKassa/CloudPayments/TBank/Prodamus/Stripe/PayPal.

## Backend

```powershell
dotnet restore backend\VpnPlatform.sln
dotnet build backend\VpnPlatform.sln --no-restore
dotnet test backend\VpnPlatform.sln --no-build --logger "trx;LogFileName=backend-tests-all-payments-local-sandbox.trx" --verbosity quiet
dotnet ef migrations has-pending-model-changes --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --context ApplicationDbContext
dotnet list backend\VpnPlatform.sln package --outdated --highest-patch
dotnet list backend\VpnPlatform.sln package --vulnerable --include-transitive
dotnet ef migrations list --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --no-connect
```

Результат:

- Build: 0 ошибок, 0 предупреждений.
- Tests: 180/180 пройдено.
- TRX: `backend/tests/VpnPlatform.UnitTests/TestResults/backend-tests-all-payments-local-sandbox.trx`.
- EF pending model changes: отсутствуют.
- Patch-обновлений внутри текущих major/minor веток нет.
- Уязвимых NuGet-пакетов не найдено.
- EF tooling на `dotnet-ef` 9.0.16 успешно видит миграции проекта.

## Local smoke без Docker

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 8088 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
```

Проверено:

- `http://127.0.0.1:8088/health/live` -> HTTP 200.
- `http://127.0.0.1:5183` -> HTTP 200.
- `http://127.0.0.1:5184` -> HTTP 200.
- `http://127.0.0.1:5185` -> HTTP 200.
- `scripts/stop-local.ps1` корректно остановил API, npm и дочерние Vite-процессы.

## Frontend

Ранее в рамках локальной стабилизации проверено:

```powershell
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=moderate
```

Результат: typecheck/build/test успешны, frontend tests 27/27, audit без moderate+ уязвимостей.

## VPS staging

Проверено после деплоя на staging VPS:

- PostgreSQL 16 установлен и запущен, database `vpnplatform`, пользователь `vpnplatform`, порт `5432` слушает только `127.0.0.1`.
- API запущен через `vpn-platform-api.service`, `Database__Provider=Postgres`.
- EF migrations применены, demo seed и admin bootstrap выполнены.
- `http://<staging-host>:8080/health/live` -> HTTP 200.
- `http://<staging-host>` -> HTTP 200.
- `http://<staging-host>:5173` -> HTTP 200.
- `http://<staging-host>:5174` -> HTTP 200.
- `http://<staging-host>:5175` -> HTTP 200.
- `http://<staging-host>:8080/api/public/payments/providers` отдаёт 8 sandbox-провайдеров: YooMoney, YooKassa, RoboKassa, CloudPayments, TBankAcquiring, Prodamus, Stripe, PayPal.
- Для всех 8 провайдеров проверен API flow: register -> create order -> payment init.
- Кабинет открыт в браузере, console errors: 0.

## Provisioning runner

```powershell
python -m unittest discover -s infra\ansible\runner\tests -v
```

Результат: 4/4 пройдено.

## Ограничения среды

- Docker Desktop в текущей среде не был запущен, поэтому compose runtime не проверялся.
- `ansible-playbook` не установлен, поэтому Ansible syntax-check не выполнялся.
- Bash-скрипты `.sh` рассчитаны на Linux/WSL; для Windows добавлены PowerShell-скрипты локального запуска.
