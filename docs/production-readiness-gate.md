# Production readiness gate

Документ закрывает локальный roadmap-пункт `P11-ACC-008`: перед тем как называть проект production-ready, теперь есть отдельная fail-closed команда, которая проверяет staging/VPS smoke report и не пропускает релиз, если roadmap или release decision все еще содержат открытые production-блокеры.

## Что проверяет gate

Команда `scripts/assert-production-readiness.ps1` выполняет две группы проверок:

- запускает `scripts/validate-staging-smoke-report.ps1 -RequireAllPassed`, поэтому все обязательные smoke-пункты должны быть `passed`, а отчет не должен содержать секреты, cookies, `.env`, auth headers, private headers, private keys, provider tokens, client secrets или API keys;
- запускает `scripts/validate-payment-provider-smoke-report.ps1 -RequireAllPassed`, поэтому YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe и PayPal должны иметь passed evidence;
- запускает `scripts/validate-admin-vps-smoke-report.ps1 -RequireAllPassed`, поэтому вход в админку на VPS, отсутствие JS/API ошибок и все разделы админки должны быть подтверждены;
- запускает `scripts/validate-vpn-live-smoke-report.ps1 -RequireAllPassed`, поэтому реальная 3x-ui/x-ui панель, inbound, VPN node, заказ, webhook, подписка, клиент, URI/QR и fail-closed поведение должны быть подтверждены;
- читает `docs/PRODUCT_COMPLETION_ROADMAP.md` и `docs/release-decision.md`, затем блокирует production-ready, если остаются открытые `STATE-011`, `STATE-012`, `STATE-013`, `P0-*`, `P11-ACC-002`, `BUG-001`, `BUG-002`, `BUG-003` или решение все еще равно `staging-ready baseline`.

Gate агрегирует результаты всех четырех evidence validators и только после этого падает. Если несколько отчетов остаются `blocked` или `failed`, payload `Production readiness blocked` содержит массив `evidenceReports` с `name`, `status`, `reportPath`, `validatorPath` и `message` по каждому отчету, а также roadmap/release `blockers`. Это нужно, чтобы оператор видел полный список недостающих доказательств, а не только первую ошибку staging/VPS-шаблона.

Для CI и handoff можно передать `-OutputPath`: gate запишет Markdown и соседний JSON result artifact даже при ожидаемом `blocked`, а затем продолжит fail-closed падать. Result содержит `failedEvidenceReportsCount`, `blockersCount`, пути всех reports, `evidenceReports`, `blockers`, `resultJsonPath` и `resultMarkdownPath`.

Скачанный result artifact можно проверить без повторного запуска gate через `scripts/validate-production-readiness-assertion-result.ps1`. Валидатор сверяет статус `blocked`/`production-ready`, четыре evidence report entries, счетчики, пути reports, roadmap/release decision и Markdown-пару.

Это не заменяет реальные live-проверки. Gate нужен, чтобы не забыть зафиксировать доказательства и не выдать локально зеленый проект за production-ready.

## Как запускать

Сначала заполните отчет по шаблону `docs/staging-smoke-report.template.json`: замените `blocked` на реальные статусы, добавьте ссылки на GitHub Actions deploy, health responses, admin login, checkout, payment webhook, subscription, VPN access и подтверждение отсутствия секретов.

Проверка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
```

Проверка с result artifacts:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 `
  -ReportPath docs\staging-smoke-report.template.json `
  -OutputPath tmp\production-readiness-assertion.md `
  -Force
```

Отдельная проверка result artifact:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-result.ps1 `
  -ResultJsonPath tmp\production-readiness-assertion.json
```

Regression-проверка validator на испорченных копиях result artifacts:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-result-validator.ps1 `
  -ResultJsonPath tmp\production-readiness-assertion.json `
  -WriteJson
```

CI-friendly wrapper для assertion artifacts:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression.ps1 `
  -OutputDirectory tmp\production-readiness-assertion-ci-regression-test `
  -Force
```

Wrapper запускает `assert-production-readiness.ps1`, сохраняет `production-readiness-assertion.json`, `.md` и `.log`, проверяет result через `validate-production-readiness-assertion-result.ps1`, запускает regression harness для blocked-result и пишет `production-readiness-assertion-ci-regression-result.json` и `.md`. В GitHub Actions он запускается job `production-readiness-assertion` в `.github/workflows/ci.yml` после backend job и публикует artifact `production-readiness-assertion-ci-regression`. Если доступна `GITHUB_STEP_SUMMARY`, wrapper добавляет краткий Markdown-итог в summary job.

Скачанный CI regression result можно проверить отдельно:

Workflow guard для published artifact-директория:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson
```

Guard проверяет, что `.github/workflows/ci.yml` содержит job `production-readiness-assertion`, зависит от `backend`, запускает `test-production-readiness-assertion-ci-regression.ps1`, публикует artifact `production-readiness-assertion-ci-regression`, включает `if-no-files-found: error` и перечисляет пять обязательных файлов: CI result JSON/Markdown, assertion JSON/Markdown и assertion log.

GitHub Actions запускает этот guard отдельным step `Guard production readiness assertion workflow artifacts` до `Run production readiness assertion CI regression`, поэтому broken published artifacts contract должен падать до запуска wrapper и upload step.

Fail-closed regression для guard:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1 -WriteJson
```

Harness проверяет happy path, затем портит копию workflow и ожидает ошибки для `missing-guard-step`, `missing-assertion-log-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.
Aggregate guard для всех production CI workflow artifacts contracts:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-ci-workflow-artifacts-guards.ps1 -WriteJson
```

Aggregate запускает readiness assertion workflow guard, readiness assertion fail-closed validator, production evidence workflow guard и production evidence fail-closed validator. В GitHub Actions он выполняется step `Guard production CI workflow artifacts contracts` сразу после checkout в backend job, чтобы сломанный artifact contract падал до тяжелых сборок и тестов.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-ci-regression-result.ps1 `
  -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json
```

Regression-проверка CI result validator на испорченных копиях CI result artifacts:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-regression-result-validator.ps1 `
  -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json `
  -WriteJson
```

Wrapper запускает этот harness автоматически и записывает `ciResultValidatorRegression` в итоговые JSON/Markdown artifacts.

GitHub Step Summary readiness assertion CI wrapper проверяется отдельно:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-ci-summary.ps1 `
  -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json `
  -SummaryPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.md
```

Regression-проверка summary validator:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-summary-validator.ps1 `
  -ResultJsonPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.json `
  -SummaryPath tmp\production-readiness-assertion-ci-regression-test\production-readiness-assertion-ci-regression-result.md `
  -WriteJson
```

Wrapper запускает summary validator и regression harness автоматически, записывает `ciSummaryValidatorRegression` в итоговый result и дополнительно проверяет реальный `GITHUB_STEP_SUMMARY`, если переменная доступна в GitHub Actions job.

Локальная smoke-проверка реального `GITHUB_STEP_SUMMARY`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-step-summary.ps1 `
  -OutputDirectory tmp\production-readiness-assertion-ci-step-summary-test `
  -Force `
  -WriteJson
```

Smoke выставляет `GITHUB_STEP_SUMMARY`, запускает readiness assertion CI wrapper, валидирует созданный summary через `validate-production-readiness-assertion-ci-summary.ps1`, сверяет summary с result Markdown и проверяет строки `CI summary validator regression` и `CI result validator regression`.

Проверка всего artifact-директория одной командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-assertion-ci-artifacts.ps1 `
  -ArtifactDirectory tmp\production-readiness-assertion-ci-regression-test `
  -RequireBlockedAssertion `
  -WriteJson
```

Validator проверяет наличие `production-readiness-assertion-ci-regression-result.json`, `.md`, `production-readiness-assertion.json`, `.md`, `.log`, согласованность путей внутри result JSON, standalone result validator, summary validator и optional `-StepSummaryPath`.

Fail-closed regression для всего artifact-директория:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-readiness-assertion-ci-artifacts-validator.ps1 `
  -ArtifactDirectory tmp\production-readiness-assertion-ci-regression-test `
  -WriteJson
```

Regression harness копирует валидный bundle во временные директории и проверяет tamper-сценарии `missing-required-artifact`, `bad-output-directory`, `bad-assertion-log-path`, `bad-result-markdown` и `bad-step-summary`. Wrapper `test-production-readiness-assertion-ci-regression.ps1` запускает harness автоматически, пишет `ciArtifactsValidatorRegression` в итоговый JSON/Markdown, а summary validator regression дополнительно проверяет `bad-ci-artifacts-validator-regression`, чтобы строка `CI artifacts validator regression` не могла исчезнуть из GitHub Step Summary.

Validator сверяет статус wrapper, assertion exit code, linked assertion JSON/Markdown/log, result validator, validator regression, обязательные failure-сценарии и Markdown-пару.

Если отчеты лежат не в стандартных местах, передайте их явно:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 `
  -ReportPath tmp\staging-smoke-report.json `
  -PaymentProviderReportPath tmp\payment-provider-smoke-report.json `
  -AdminVpsReportPath tmp\admin-vps-smoke-report.json `
  -VpnLiveReportPath tmp\vpn-live-smoke-report.json
```

Чтобы создать весь безопасный пакет черновиков одной командой, используйте bundle-generator:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-bundle.ps1 `
  -OutputDirectory tmp\production-evidence `
  -ApiBaseUrl https://api.example.test `
  -PublicWebUrl https://example.test `
  -CabinetWebUrl https://example.test/cabinet `
  -AdminWebUrl https://example.test/admin `
  -X3uiPanelUrl https://x3ui.example.test `
  -EnvironmentName staging `
  -Operator operator-name `
  -RunProductionGate
```

Скрипт создает `staging-smoke-report.json`, `payment-provider-smoke-report.json`, `admin-vps-smoke-report.json` и `vpn-live-smoke-report.json`, прогоняет обычные validators каждого отчета и при `-RunProductionGate` сохраняет статус агрегированного gate в итоговом сообщении. Черновики остаются `blocked`, пока оператор не заменит TODO на реальные sanitized evidence.

После этого можно собрать человекочитаемый summary для оператора:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-readiness-summary.ps1 `
  -OutputPath tmp\production-evidence\production-readiness-summary.md `
  -ReportPath tmp\production-evidence\staging-smoke-report.json `
  -PaymentProviderReportPath tmp\production-evidence\payment-provider-smoke-report.json `
  -AdminVpsReportPath tmp\production-evidence\admin-vps-smoke-report.json `
  -VpnLiveReportPath tmp\production-evidence\vpn-live-smoke-report.json `
  -Force
```

Summary пишет Markdown и соседний JSON-файл: статус каждого evidence report, количество passed/blocked/failed checks, список платежных провайдеров и открытые roadmap blockers. В summary нельзя добавлять секреты, cookies, auth headers, private keys или полные VPN access URI.

Проверить summary можно отдельным валидатором:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-readiness-summary.ps1 `
  -SummaryPath tmp\production-evidence\production-readiness-summary.md `
  -RequireReportFiles
```

Для финального production handoff добавьте `-RequireProductionReady`: валидатор потребует `production-ready`, четыре `passed` evidence reports и отсутствие roadmap blockers.

Проверить весь каталог evidence bundle одной командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-bundle.ps1 `
  -BundleDirectory tmp\production-evidence `
  -RequireSummary
```

Для финального production handoff используйте `-RequireProductionReady`: bundle validator запустит строгие validators всех четырех reports и summary.

Зафиксировать состав bundle для handoff можно manifest-файлом:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-manifest.ps1 `
  -BundleDirectory tmp\production-evidence `
  -RequireSummary `
  -Force
```

Manifest пишет `production-evidence-manifest.json` с release id, relative paths, SHA256, размером файлов и UTC timestamp. Он не содержит секреты и не копирует содержимое evidence reports.

Проверить, что bundle не изменился после генерации manifest, можно отдельной fail-closed командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-manifest.ps1 `
  -ManifestPath tmp\production-evidence\production-evidence-manifest.json `
  -RequireAllFiles
```

Валидатор перечитывает manifest, проверяет обязательные файлы, относительные пути, размеры, timestamps, total files/bytes и пересчитывает SHA256 каждого файла bundle.

После успешной проверки manifest можно собрать единый ZIP-архив для handoff:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-archive.ps1 `
  -ManifestPath tmp\production-evidence\production-evidence-manifest.json `
  -RequireAllFiles `
  -Force
```

Архиватор сначала запускает manifest validator, затем добавляет в ZIP сам manifest и только файлы, перечисленные в manifest. В результате выводятся путь к архиву, SHA256 архива, SHA256 manifest и список entries.

Перед публикацией или передачей ZIP можно проверить отдельной командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-archive.ps1 `
  -ArchivePath tmp\production-evidence\production-evidence.zip `
  -RequireAllFiles
```

Валидатор архива читает manifest из ZIP, запрещает лишние entries, проверяет обязательные файлы, размеры, `totalBytes`, SHA256 каждого entry и опционально принимает ожидаемый SHA256 архива через `-ExpectedArchiveSha256`.

Для handoff удобно сформировать отдельный receipt без содержимого evidence reports:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-receipt.ps1 `
  -ArchivePath tmp\production-evidence\production-evidence.zip `
  -RequireAllFiles `
  -Force
```

Receipt сначала запускает archive validator, затем пишет `production-evidence-handoff-receipt.json` и `.md` с release id, SHA256 архива, SHA256 manifest, размером архива и списком проверенных entries.

Проверить receipt перед передачей можно отдельной командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-receipt.ps1 `
  -ReceiptPath tmp\production-evidence\production-evidence-handoff-receipt.json `
  -RequireAllFiles
```

Валидатор receipt сверяет JSON и Markdown receipt с ZIP-архивом, повторно запускает archive validator, проверяет SHA256 архива, SHA256 manifest, entries и verified files.

Для финальной передачи оператору сформируйте checklist:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-checklist.ps1 `
  -ReceiptPath tmp\production-evidence\production-evidence-handoff-receipt.json `
  -RequireAllFiles `
  -Force
```

Checklist сначала запускает receipt validator, затем пишет `production-evidence-handoff-checklist.json` и `.md` со статусом handoff, release id, SHA256 архива, SHA256 manifest, gates и действиями оператора. В строгом режиме `-RequireProductionReady` команда fail-closed завершится ошибкой, если `production-readiness-summary.json` не подтверждает `production-ready`.

Проверить checklist перед передачей можно отдельной командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-checklist.ps1 `
  -ChecklistPath tmp\production-evidence\production-evidence-handoff-checklist.json
```

Checklist validator заново запускает receipt validator, сверяет release id, SHA256 архива, SHA256 manifest, Markdown-пару, gates и operator actions. Для финального production handoff добавьте `-RequireProductionReady`: валидатор потребует `production-ready-handoff`, все gates `passed` и `production-readiness-summary.json` со статусом `production-ready`.

После проверки checklist можно собрать минимальный handoff package:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package.ps1 `
  -ChecklistPath tmp\production-evidence\production-evidence-handoff-checklist.json `
  -Force
```

Package generator повторно запускает checklist validator, копирует только ZIP, JSON/Markdown receipt, JSON/Markdown checklist и создает `production-evidence-handoff-package-index.json`, `.md` и `SHA256SUMS.txt`. Для финального production handoff добавьте `-RequireProductionReady`, чтобы package не собирался из blocked checklist.

Проверить готовый package можно отдельной командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package.ps1 `
  -PackageDirectory tmp\production-evidence\production-evidence-handoff-package
```

Package validator проверяет whitelist файлов, `production-evidence-handoff-package-index.json`, `SHA256SUMS.txt`, пересчитывает SHA256 каждого artifact и повторно запускает checklist validator. В режиме `-RequireProductionReady` package должен иметь `production-ready-handoff` и проходить строгую проверку checklist.

Проверенный package можно упаковать в один ZIP:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-production-evidence-handoff-package-archive.ps1 `
  -PackageDirectory tmp\production-evidence\production-evidence-handoff-package `
  -Force
```

Package archive generator повторно запускает package validator, добавляет в ZIP только разрешенные package files и возвращает `archiveSha256`, `archiveBytes`, `entries`, исходный SHA256 production evidence ZIP и SHA256 manifest. В режиме `-RequireProductionReady` архив не будет создан из blocked package.

Default-имя ZIP использует короткий hash `releaseId`, а полный `releaseId` остается в JSON result. Это защищает локальные и CI-запуски на Windows от path-limit при длинных release id и глубокой `OutputDirectory`.

Регрессию длинных путей можно проверить отдельной командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-long-path.ps1 `
  -OutputDirectory tmp\production-evidence-handoff-package-archive-long-release-id-path-regression-test `
  -Force
```

Harness запускает полный flow, проверяет, что имя handoff package ZIP не содержит полный `releaseId`, содержит 12-символьный hash release id, остается коротким, а result JSON сохраняет полный release id.

Финальный ZIP-архив handoff package можно проверить отдельным валидатором:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package-archive.ps1 `
  -ArchivePath tmp\production-evidence\production-evidence-handoff-package-<release>.zip
```

Package archive validator сверяет SHA256 внешнего ZIP, запрещает вложенные и неожиданные entries, проверяет обязательные `production-evidence.zip`, receipt, checklist, package index и `SHA256SUMS.txt`, временно извлекает package и повторно запускает `validate-production-evidence-handoff-package.ps1`. В режиме `-RequireProductionReady` строгая проверка доходит до production-ready handoff на уровне package validator.

Для regression-проверки валидатора на tamper-сценариях используйте отдельный harness:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-validator.ps1 `
  -ArchivePath tmp\production-evidence\production-evidence-handoff-package-<release>.zip
```

Harness сначала проверяет исходный ZIP, затем создает временные испорченные копии и ожидает fail-closed ошибки для неверного expected SHA256, лишнего `unexpected-entry.txt` и отсутствующего `SHA256SUMS.txt`.

Чтобы не собирать всю локальную цепочку вручную, можно запустить end-to-end flow одной командой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-flow.ps1 `
  -OutputDirectory tmp\production-evidence-handoff-package-archive-flow-test `
  -Force
```

Flow harness создает evidence bundle, summary, manifest, production evidence ZIP, handoff receipt, checklist, package, финальный handoff package ZIP и сразу запускает archive validator regression. Это снижает риск ошибок в ручной последовательности команд при локальной проверке и CI.

При `-Force` flow harness удаляет и пересоздает `OutputDirectory`, поэтому команда защищена fail-closed проверкой: путь не может быть корнем файловой системы, корнем репозитория и должен быть явно назван под `production-evidence` artifacts. Для CI используйте отдельный каталог вида `tmp\production-evidence-...`.

После успешного запуска flow сохраняет итоговые artifacts `production-evidence-handoff-package-archive-flow-result.json` и `production-evidence-handoff-package-archive-flow-result.md` в `OutputDirectory`. В них фиксируются release id, package status, production evidence archive SHA256, handoff package archive SHA256, пути к artifacts и tamper-сценарии, которые прошел regression harness.

Flow сразу запускает `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1`, а оператор может повторить проверку отдельно:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package-archive-flow-result.ps1 `
  -ResultJsonPath tmp\production-evidence-handoff-package-archive-flow-test\production-evidence-handoff-package-archive-flow-result.json
```

Валидатор проверяет `status = passed`, `regressionStatus = passed`, SHA256 production evidence archive и handoff package archive, Markdown-пару, обязательные tamper-сценарии regression harness и повторно запускает `validate-production-evidence-handoff-package-archive.ps1` для финального ZIP.

Для проверки fail-closed поведения самого result validator используйте regression harness:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-flow-result-validator.ps1 `
  -ResultJsonPath tmp\production-evidence-handoff-package-archive-flow-test\production-evidence-handoff-package-archive-flow-result.json
```

Harness ожидает ошибки для испорченного `status`, неверного SHA256 handoff package archive, отсутствующего tamper-сценария и Markdown без обязательного блока `Tested failures`.

Для CI можно запускать единый локальный wrapper, который последовательно выполняет основной flow, result validator regression и long-path regression:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression.ps1 `
  -OutputDirectory tmp\production-evidence-handoff-package-archive-ci-regression-test `
  -Force
```

Wrapper сохраняет `production-evidence-handoff-package-archive-ci-regression-result.json` и `.md` с путями к основному result, финальному handoff package archive и результатам regression harnesses.

В GitHub Actions этот wrapper запускается отдельным job `production-evidence` в `.github/workflows/ci.yml` после backend-проверок. Job публикует artifact `production-evidence-handoff-package-archive-ci-regression` с итоговыми `production-evidence-handoff-package-archive-ci-regression-result.json` и `.md`, поэтому CI evidence можно скачать без повторного ручного запуска.

Workflow guard для published artifact-директория `production-evidence`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1 -WriteJson
```

GitHub Actions запускает этот guard отдельным step `Guard production evidence workflow artifacts` до `Run production evidence handoff archive CI regression`, поэтому broken published artifacts contract должен падать до запуска wrapper и upload step.

Fail-closed regression для guard:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1 -WriteJson
```

Harness проверяет happy path, затем портит копию workflow и ожидает ошибки для `missing-guard-step`, `missing-result-json-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.

Если доступна переменная `GITHUB_STEP_SUMMARY`, wrapper дополнительно дописывает тот же Markdown-результат в GitHub Actions job summary: общий статус, release id, статус основного flow, result validator regression и long-path regression. Локально это можно проверить, задав `GITHUB_STEP_SUMMARY` на временный `.md` файл перед запуском wrapper.

Wrapper запускает `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1` для result Markdown и для `GITHUB_STEP_SUMMARY`, если summary-файл доступен. Валидатор fail-closed сверяет JSON result artifact с Markdown: `status = passed`, release id, статусы основного flow, result validator regression, long-path regression и пути к обязательным artifacts.

Дополнительно wrapper запускает `scripts/test-production-evidence-handoff-package-archive-ci-summary-validator.ps1`. Regression harness портит JSON/Markdown summary и ожидает fail-closed ошибки для неверного статуса, чужого release id, отсутствующего artifact path и неверного long-path статуса. Итог записывается в поле `ciSummaryValidatorRegression` result JSON/Markdown.

Финальный CI result artifact можно проверить отдельно:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-production-evidence-handoff-package-archive-ci-regression-result.ps1 `
  -ResultJsonPath tmp\production-evidence-handoff-package-archive-ci-regression-test\production-evidence-handoff-package-archive-ci-regression-result.json
```

Этот валидатор сверяет общий статус, статусы всех вложенных regression harnesses, наличие `ciSummaryValidatorRegression`, обязательные failure-сценарии summary validator regression, Markdown-пару и пути к artifacts.

Fail-closed поведение этого standalone validator проверяется отдельным regression harness:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1 `
  -ResultJsonPath tmp\production-evidence-handoff-package-archive-ci-regression-test\production-evidence-handoff-package-archive-ci-regression-result.json
```

Harness портит итоговый CI result JSON/Markdown и ожидает ошибки для неверного общего статуса, пустого `releaseId`, отсутствующего failure-сценария summary validator и сломанного Markdown. Основной CI wrapper запускает его автоматически, записывает результат в `ciResultValidatorRegression`, обновляет JSON/Markdown artifacts и повторно валидирует финальный result artifact.

На текущем состоянии проекта команда должна завершаться ошибкой: шаблон содержит `blocked`, а master roadmap честно держит открытыми live-платежи, реальный 3x-ui, VPS admin smoke и `P11-ACC-002`.

После реального staging/VPS smoke команда сможет пройти только если одновременно выполнены условия:

- staging/VPS smoke report валиден и все checks имеют статус `passed`;
- payment provider smoke report валиден и все провайдеры имеют статус `passed`;
- admin VPS smoke report валиден, все общие gates истинные и все разделы имеют статус `passed`;
- VPN live smoke report валиден, все top-level gates истинные и все checks имеют статус `passed`;
- секреты, cookies, `.env`, auth headers, private headers, provider keys, client secrets и API keys не попали в отчет;
- roadmap обновлен: live-блокеры закрыты с доказательствами;
- `docs/release-decision.md` больше не содержит решение `staging-ready baseline, не production-ready`.

## Что остается внешним

Локально этот gate подтверждает только контракт проверки. Он не закрывает:

- live-платежи всех провайдеров;
- реальную 3x-ui/x-ui выдачу;
- production admin smoke на VPS;
- ротацию ранее раскрытых секретов;
- домен, HTTPS и staging PostgreSQL backup/restore.

Эти пункты остаются открытыми до фактического прогона на внешнем окружении.
