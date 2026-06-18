# Changelog

Все заметные изменения проекта фиксируются в этом файле и в разделе "Что нового" внутри приложения. Подробный рабочий roadmap находится в `docs/PRODUCT_COMPLETION_ROADMAP.md`.

## 0.156.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-result-artifacts`.

### Added

- `scripts/assert-production-readiness.ps1` получил `-OutputPath`, `-JsonOutputPath` и `-Force` для сохранения JSON/Markdown result artifacts.

### Changed

- Production readiness gate теперь пишет result artifacts даже при ожидаемом `blocked`, а затем продолжает fail-closed падать.
- Result JSON/Markdown фиксирует `failedEvidenceReportsCount`, `blockersCount`, пути всех evidence reports, `evidenceReports`, `blockers`, `resultJsonPath` и `resultMarkdownPath`.
- Roadmap и release docs синхронизированы с backend suite `542/542` и latest release `0.156.0`.

### Verified

- `ProductionReadinessGateTests`: 35/35.
- Blocked production readiness assertion artifact smoke: OK.
- Targeted release/docs suite: 51/51.
- Backend full suite: 542/542.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Artifacts не делают проект production-ready: live/VPS/payment/3x-ui blockers остаются открытыми до реальных passed evidence reports.

## 0.155.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1` проверяет fail-closed поведение standalone CI result validator на испорченных JSON/Markdown artifacts.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает CI result validator regression, сохраняет `ciResultValidatorRegression` в итоговом JSON/Markdown и повторно валидирует финальный result artifact.
- Production readiness gate документация описывает отдельный regression harness для CI result validator.
- Roadmap и release docs синхронизированы с backend suite `541/541` и latest release `0.155.0`.

### Verified

- `ProductionReadinessGateTests`: 34/34.
- Production evidence handoff package archive CI wrapper smoke with result validator regression: OK.
- Standalone CI result validator regression: OK.
- Backend full suite: 541/541.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет ошибки для неверного общего статуса, пустого `releaseId`, отсутствующего failure-сценария summary validator и сломанного Markdown.

## 0.154.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1` проверяет итоговый CI regression JSON/Markdown artifact.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает result validator после финальной записи artifacts.
- Production readiness gate документация описывает отдельную проверку скачанного CI result artifact.
- Roadmap и release docs синхронизированы с backend suite `540/540` и latest release `0.154.0`.

### Verified

- `ProductionReadinessGateTests`: 33/33.
- Production evidence handoff package archive CI wrapper smoke with result validator: OK.
- Backend full suite: 540/540.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет все вложенные statuses, `ciSummaryValidatorRegression`, Markdown-пару и обязательные artifact paths.

## 0.153.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-summary-validator.ps1` проверяет fail-closed поведение CI summary validator на испорченных JSON/Markdown artifacts.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает summary validator regression и сохраняет `ciSummaryValidatorRegression` в result artifacts.
- Production readiness gate документация описывает regression harness для CI summary validator.
- Roadmap и release docs синхронизированы с backend suite `539/539` и latest release `0.153.0`.

### Verified

- `ProductionReadinessGateTests`: 32/32.
- Production evidence handoff package archive CI wrapper smoke with summary validator regression: OK.
- Backend full suite: 539/539.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет неверный main flow status, чужой release id в summary, отсутствующий artifact path и неверный long-path status.

## 0.152.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1` проверяет CI summary Markdown против JSON result artifact.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает validator для result Markdown и для `GITHUB_STEP_SUMMARY`, если summary-файл доступен.
- Production readiness gate документация описывает fail-closed проверку summary.
- Roadmap и release docs синхронизированы с backend suite `538/538` и latest release `0.152.0`.

### Verified

- `ProductionReadinessGateTests`: 31/31.
- Production evidence handoff package archive CI wrapper smoke with summary validator: OK.
- Backend full suite: 538/538.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Summary validator проверяет статусы, release id и пути artifacts; live/VPS/payment blockers остаются внешними smoke-задачами.

## 0.151.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary`.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь дописывает Markdown-результат в `GITHUB_STEP_SUMMARY`, если wrapper запускается в GitHub Actions.
- Production readiness gate документация описывает GitHub Actions job summary и локальную проверку через временный summary-файл.
- Roadmap и release docs синхронизированы с backend suite `537/537` и latest release `0.151.0`.

### Verified

- `ProductionReadinessGateTests`: 30/30.
- Production evidence handoff package archive CI regression wrapper smoke with `GITHUB_STEP_SUMMARY`: OK.
- Backend full suite: 537/537.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Job summary ускоряет диагностику CI evidence gate, но финальные JSON/Markdown artifacts остаются основным handoff-доказательством.

## 0.150.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-workflow`.

### Added

- В `.github/workflows/ci.yml` добавлен job `production-evidence`, который запускает production evidence handoff package archive CI regression после backend-проверок.
- GitHub Actions публикует artifact `production-evidence-handoff-package-archive-ci-regression` с JSON/Markdown результатами wrapper.

### Changed

- Production readiness gate документация теперь описывает, где в GitHub Actions брать CI evidence artifacts.
- Roadmap и release docs синхронизированы с backend suite `536/536` и latest release `0.150.0`.

### Verified

- `ProductionReadinessGateTests`: 29/29.
- Production evidence handoff package archive CI regression wrapper smoke: OK.
- Backend full suite: 536/536.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI job проверяет локальные evidence regressions в validation pipeline, но live/VPS/payment blockers все еще закрываются только реальными smoke reports.

## 0.149.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` объединяет локальные archive flow regression harnesses в один CI-friendly запуск.
- Wrapper сохраняет `production-evidence-handoff-package-archive-ci-regression-result.json` и `.md`.

### Changed

- Production readiness gate документация теперь описывает единый CI wrapper для основного flow, result validator regression и long-path regression.
- Roadmap и release docs синхронизированы с backend suite `535/535` и latest release `0.149.0`.

### Verified

- `ProductionReadinessGateTests`: 28/28.
- Production evidence handoff package archive CI regression wrapper smoke: OK.
- Backend full suite: 535/535.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI wrapper остается локальным evidence regression gate; live/VPS/payment blockers закрываются только реальными smoke reports.

## 0.148.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-long-path-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-long-path.ps1` запускает полный handoff flow в длинной production-evidence директории.
- Long-path harness проверяет hash-based имя handoff package ZIP и сохранение полного `releaseId` в result JSON.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку Windows path-limit regression.
- Roadmap и release docs синхронизированы с backend suite `534/534` и latest release `0.148.0`.

### Verified

- `ProductionReadinessGateTests`: 27/27.
- Production evidence handoff package archive long path regression smoke: OK.
- Backend full suite: 534/534.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Проверка не закрывает live/VPS/payment blockers; она защищает локальный и CI evidence flow от Windows path-limit на длинных release id.

## 0.147.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow-result-validator.ps1` проверяет fail-closed поведение validator результата полного handoff flow.
- Regression harness ожидает ошибки для испорченного `status`, неверного SHA256 handoff archive, отсутствующего tamper-сценария и Markdown без блока `Tested failures`.

### Changed

- Production readiness gate документация теперь описывает отдельный regression harness для result validator.
- Default-имя handoff package ZIP теперь использует короткий hash release id, чтобы длинные release id не ломали сборку на Windows path-limit.
- Roadmap и release docs синхронизированы с backend suite `533/533` и latest release `0.147.0`.

### Verified

- `ProductionReadinessGateTests`: 26/26.
- Production evidence handoff package archive flow result validator regression smoke: OK.
- Backend full suite: 533/533.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет только целостность локального evidence result validator; live/VPS/payment blockers остаются открытыми до реальных smoke reports.

## 0.146.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1` проверяет JSON/Markdown итог полного production evidence handoff flow.
- Flow автоматически запускает result validator после записи `production-evidence-handoff-package-archive-flow-result.json` и `.md`.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку result artifacts полного flow.
- Roadmap и release docs синхронизированы с backend suite `532/532` и latest release `0.146.0`.

### Verified

- `ProductionReadinessGateTests`: 25/25.
- Production evidence handoff package archive flow result validator smoke: OK.
- Backend full suite: 532/532.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Result validator не закрывает внешние live-блокеры; он проверяет локальный evidence flow outcome и целостность ссылок на handoff artifacts.

## 0.145.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` сохраняет `production-evidence-handoff-package-archive-flow-result.json` и `.md`.
- Result artifacts фиксируют release id, package status, SHA256 production evidence archive, SHA256 handoff package archive, пути artifacts и tamper-сценарии regression harness.

### Changed

- Production readiness gate документация теперь описывает result artifacts полного flow.
- Roadmap и release docs синхронизированы с backend suite `531/531` и latest release `0.145.0`.

### Verified

- `ProductionReadinessGateTests`: 24/24.
- Production evidence handoff package archive flow result artifacts smoke: OK.
- Backend full suite: 531/531.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Result artifacts не заменяют live/VPS/payment evidence reports; они фиксируют локальный flow outcome и SHA256 handoff artifacts.

## 0.144.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-safety`.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` теперь проверяет output directory перед рекурсивной очисткой.
- Flow запрещает корень файловой системы, корень репозитория и папку без явного `production-evidence` в имени.

### Added

- Production readiness gate документация описывает безопасный шаблон output directory для локальных и CI evidence-проверок.
- Roadmap получил пункт `P11-ACC-029` для защиты `-Force` в end-to-end flow.

### Verified

- `ProductionReadinessGateTests`: 23/23.
- Guarded production evidence handoff package archive flow smoke: OK.
- Backend full suite: 530/530.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Safety guard не меняет формат evidence artifacts; он защищает только выбор директории для перезаписи при `-Force`.

## 0.143.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` собирает весь локальный production evidence handoff flow одной командой.
- Flow создает evidence bundle, summary, manifest, production evidence ZIP, handoff receipt, checklist, package, финальный ZIP и запускает archive validator regression.

### Changed

- Production readiness gate документация теперь предлагает одну команду для полной локальной evidence handoff проверки.
- Roadmap и release docs синхронизированы с backend suite `529/529` и latest release `0.143.0`.

### Verified

- `ProductionReadinessGateTests`: 22/22.
- Production evidence handoff package archive end-to-end flow smoke: OK.
- Backend full suite: 529/529.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Flow harness проверяет локально собранный blocked/staging evidence; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.142.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-validator.ps1` запускает happy path и tamper-сценарии для финального ZIP-архива handoff package.
- Regression harness создает временные испорченные копии архива и проверяет ошибки для неверного SHA256, лишнего entry и отсутствующего `SHA256SUMS.txt`.

### Changed

- Production readiness gate документация теперь описывает regression-проверку archive validator.
- Roadmap и release docs синхронизированы с backend suite `528/528` и latest release `0.142.0`.

### Verified

- `ProductionReadinessGateTests`: 21/21.
- Production evidence handoff package archive validator regression smoke: OK.
- Backend full suite: 528/528.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет локально собранный ZIP; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.141.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive.ps1` проверяет финальный ZIP-архив handoff package.
- Валидатор сверяет SHA256 внешнего ZIP, запрещает неожиданные и вложенные entries, временно извлекает package и повторно запускает package validator.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку финального ZIP-архива handoff package.
- Roadmap и release docs синхронизированы с backend suite `527/527` и latest release `0.141.0`.

### Verified

- `ProductionReadinessGateTests`: 20/20.
- Production evidence handoff package archive validator smoke: OK.
- Backend full suite: 527/527.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Archive validator проверяет локально собранный ZIP; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.140.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive`.

### Added

- `scripts/new-production-evidence-handoff-package-archive.ps1` упаковывает проверенный handoff package в единый ZIP.
- Package archive generator повторно запускает package validator, добавляет только разрешенные package files и возвращает SHA256/размер архива.

### Changed

- Production readiness gate документация теперь описывает финальную упаковку handoff package в ZIP.
- Roadmap и release docs синхронизированы с backend suite `526/526` и latest release `0.140.0`.

### Verified

- `ProductionReadinessGateTests`: 19/19.
- Production evidence handoff package archive smoke: OK.
- Backend full suite: 526/526.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Archive generator упаковывает локальный проверенный package; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.139.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package.ps1` проверяет готовый каталог handoff package после сборки.
- Валидатор проверяет whitelist файлов, `production-evidence-handoff-package-index.json`, `SHA256SUMS.txt`, SHA256 каждого artifact и повторно запускает checklist validator.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку handoff package.
- Roadmap и release docs синхронизированы с backend suite `525/525` и latest release `0.139.0`.

### Verified

- `ProductionReadinessGateTests`: 18/18.
- Production evidence handoff package validator smoke: OK.
- Backend full suite: 525/525.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Package validator доказывает целостность локального handoff package; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.138.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package`.

### Added

- `scripts/new-production-evidence-handoff-package.ps1` собирает минимальный handoff package после проверки checklist.
- Package содержит production evidence ZIP, JSON/Markdown receipt, JSON/Markdown checklist, `production-evidence-handoff-package-index.json`, `.md` и `SHA256SUMS.txt`.

### Changed

- Production readiness gate документация теперь описывает финальный package step после checklist validator.
- Roadmap и release docs синхронизированы с backend suite `524/524` и latest release `0.138.0`.

### Verified

- `ProductionReadinessGateTests`: 17/17.
- Production evidence handoff package smoke: OK.
- Backend full suite: 524/524.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Package generator копирует только проверенные artifacts и не должен использоваться как замена реальному live/VPS/payment evidence.

## 0.137.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-checklist-validator`.

### Added

- `scripts/validate-production-evidence-handoff-checklist.ps1` проверяет JSON/Markdown checklist после генерации handoff artifact.
- Валидатор повторно запускает receipt validator и сверяет release id, SHA256 архива, SHA256 manifest, gates, operator actions и Markdown-пару checklist.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку checklist и строгий режим `-RequireProductionReady`.
- Roadmap и release docs синхронизированы с backend suite `523/523` и latest release `0.137.0`.

### Verified

- `ProductionReadinessGateTests`: 16/16.
- Production evidence handoff checklist validator smoke: OK.
- Backend full suite: 523/523.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет локальный handoff artifact. Production-ready по-прежнему требует реальные live payment/VPS/3x-ui evidence reports.

## 0.136.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-checklist`.

### Added

- `scripts/new-production-evidence-handoff-checklist.ps1` формирует JSON/Markdown checklist для передачи production evidence после проверки receipt.
- Checklist запускает `validate-production-evidence-handoff-receipt.ps1`, читает `production-readiness-summary.json` и фиксирует gates, статус handoff, release id, SHA256 архива и SHA256 manifest.

### Changed

- Production readiness gate документация теперь описывает финальный operator handoff step после ZIP, receipt и receipt validation.
- Roadmap и release docs синхронизированы с backend suite `522/522` и latest release `0.136.0`.

### Verified

- `ProductionReadinessGateTests`: 15/15.
- Production evidence handoff checklist smoke: OK.
- Backend full suite: 522/522.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Checklist не закрывает внешние production-блокеры сам по себе: для production-ready по-прежнему нужны live payment/VPS/3x-ui evidence reports.

## 0.135.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-receipt-validator`.

### Added

- `scripts/validate-production-evidence-handoff-receipt.ps1` проверяет JSON/Markdown receipt против ZIP-архива production evidence.
- Валидатор повторно запускает archive validator и сверяет release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files.
- Markdown-пара receipt проверяется на ключевые hash-данные, чтобы handoff artifact был проверяем без ручного сравнения.

### Verified

- `ProductionReadinessGateTests`: 14/14.
- Production evidence handoff receipt validator smoke: OK.
- Backend full suite: 521/521.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Receipt validation доказывает целостность локального handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.134.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-receipt`.

### Added

- `scripts/new-production-evidence-handoff-receipt.ps1` создает JSON/Markdown receipt для проверенного ZIP-архива production evidence.
- Receipt запускает archive validator, затем фиксирует release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files.
- Receipt не копирует содержимое evidence reports и подходит для передачи вместе с ZIP в CI или операторский handoff.

### Verified

- `ProductionReadinessGateTests`: 13/13.
- Production evidence handoff receipt smoke: OK, JSON/Markdown receipt создан после archive validation.
- Backend full suite: 520/520.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Receipt фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.133.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-archive-validator`.

### Added

- `scripts/validate-production-evidence-archive.ps1` проверяет ZIP-архив production evidence bundle перед публикацией или передачей оператору.
- Валидатор читает `production-evidence-manifest.json` из архива, запрещает лишние entries и сверяет обязательные файлы.
- Для каждого entry проверяются размер, `totalBytes`, SHA256 и безопасный relative path; для CI добавлен `-ExpectedArchiveSha256`.

### Verified

- `ProductionReadinessGateTests`: 12/12.
- Production evidence archive validator smoke: OK, ZIP проверен по manifest и expected archive SHA256.
- Backend full suite: 519/519.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Archive validation доказывает целостность локального ZIP artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.132.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-archive`.

### Added

- `scripts/new-production-evidence-archive.ps1` собирает ZIP-архив production evidence bundle после успешной проверки manifest.
- Архиватор добавляет в ZIP сам `production-evidence-manifest.json` и только файлы, перечисленные в manifest.
- Результат содержит SHA256 архива, SHA256 manifest, release id, размер архива и список entries.

### Verified

- `ProductionReadinessGateTests`: 11/11.
- Production evidence archive smoke: OK, ZIP создан после manifest validation.
- Backend full suite: 518/518.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Архив фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.131.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-manifest-validator`.

### Added

- `scripts/validate-production-evidence-manifest.ps1` проверяет `production-evidence-manifest.json` перед handoff или CI-публикацией.
- Валидатор перечитывает manifest, проверяет schema, release id, обязательные файлы, relative paths, размеры, timestamps, total files и total bytes.
- Для каждого файла bundle пересчитывается SHA256, чтобы поймать изменение evidence artifact после генерации manifest.

### Verified

- `ProductionReadinessGateTests`: 10/10.
- Production evidence manifest validator smoke: OK, manifest проверен с `-RequireAllFiles`.
- Backend full suite: 517/517.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Manifest validation доказывает целостность локального handoff bundle; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.130.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-manifest`.

### Added

- `scripts/new-production-evidence-manifest.ps1` создает безопасный manifest для handoff production evidence bundle.
- Manifest валидирует bundle перед созданием, затем записывает `production-evidence-manifest.json` с release id, relative paths, SHA256, размерами файлов и UTC timestamps.
- Manifest фиксирует состав evidence bundle без копирования содержимого отчетов и без сохранения секретов.

### Verified

- `ProductionReadinessGateTests`: 9/9.
- Production evidence manifest smoke: OK, 6 файлов с SHA256.
- Backend full suite: 516/516.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Manifest фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытые live/VPS/payment blockers.

## 0.129.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-validator`.

### Added

- `scripts/validate-production-evidence-bundle.ps1` проверяет весь каталог production evidence bundle одной командой.
- Bundle validator запускает validators для staging/VPS, payment providers, admin VPS, VPN live и опционального production readiness summary.
- Добавлены режимы `-RequireSummary`, `-RequireReportFiles` через summary validator и `-RequireProductionReady` для строгого production handoff.

### Verified

- `ProductionReadinessGateTests`: 8/8.
- Production evidence bundle validator smoke: OK.
- Backend full suite: 515/515.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- `-RequireProductionReady` ожидаемо падает до реальных passed evidence reports и закрытия live/VPS/payment blockers.

## 0.128.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-summary-validator`.

### Added

- `scripts/validate-production-readiness-summary.ps1` проверяет Markdown/JSON summary перед handoff оператору или CI.
- Валидатор требует четыре reports (`staging-vps`, `payment-providers`, `admin-vps`, `vpn-live`), корректные статусы, счетчики checks, required flags, report paths и roadmap blockers.
- Добавлены режимы `-RequireReportFiles` и `-RequireProductionReady` для строгой проверки артефактов и финального production handoff.

### Verified

- `ProductionReadinessGateTests`: 7/7.
- Production readiness summary validator smoke: OK, status `blocked` для generated drafts.
- Backend full suite: 514/514.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- `-RequireProductionReady` пройдет только после реальных passed evidence reports и закрытия live/VPS/payment roadmap blockers.

## 0.127.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-summary`.

### Added

- `scripts/new-production-readiness-summary.ps1` создает Markdown и JSON summary по полному production evidence bundle.
- Summary показывает статус staging/VPS, payment providers, admin VPS и VPN live reports, количество passed/blocked/failed checks и required flags.
- Summary отдельно выводит все платежные провайдеры и открытые roadmap blockers, чтобы оператор видел, почему production-ready еще заблокирован.

### Verified

- `ProductionReadinessGateTests`: 6/6.
- Production readiness summary smoke: OK, status `blocked` для generated drafts.
- Backend full suite: 513/513.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Summary является локальным operator artifact. Production-ready по-прежнему требует реальные sanitized evidence после live/staging прогонов VPS, 3x-ui и платежных провайдеров.

## 0.126.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-generator`.

### Added

- `scripts/new-production-evidence-bundle.ps1` создает весь production evidence bundle одной командой: staging/VPS, payment provider, admin VPS и VPN live reports.
- Генератор вызывает существующие безопасные генераторы отчетов, прогоняет их validators и при `-RunProductionGate` возвращает статус агрегированного production gate.
- Документация `docs/production-readiness-gate.md` получила команду создания полного bundle без ручного копирования JSON.

### Verified

- `ProductionReadinessGateTests`: 5/5.
- Bundle generator smoke: OK, созданы 4 JSON-отчета.
- Expected aggregate gate status for generated drafts: `blocked`.
- Backend full suite: 512/512.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Generated reports остаются черновиками `blocked`, пока оператор не заменит TODO на реальные sanitized evidence после live/staging прогонов.

## 0.125.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-aggregate-gate`.

### Changed

- `scripts/assert-production-readiness.ps1` теперь запускает все четыре evidence validators независимо и не останавливается на первой ошибке.
- Fail-closed payload `Production readiness blocked` содержит массив `evidenceReports` с `name`, `status`, `reportPath`, `validatorPath` и `message` по staging/VPS, payment providers, admin VPS и VPN live reports.
- Roadmap/release blockers продолжают попадать в тот же payload, поэтому оператор видит одновременно недостающие отчеты и незакрытые production blockers.
- Current status обновлен до backend `511/511`, latest release `2026-06-18-production-evidence-aggregate-gate`.

### Verified

- `ProductionReadinessGateTests`: 4/4.
- `assert-production-readiness.ps1` на blocked templates возвращает агрегированный fail-closed payload с `evidenceReports`.
- Backend full suite: 511/511.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Production-ready все еще требует реальных passed reports по VPS/staging, платежам, админке VPS и live VPN/3x-ui.

## 0.124.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-gate`.

### Changed

- `scripts/assert-production-readiness.ps1` теперь требует полный production evidence bundle: staging/VPS smoke report, payment provider smoke report, admin VPS smoke report и VPN live smoke report.
- Gate принимает `PaymentProviderReportPath`, `AdminVpsReportPath` и `VpnLiveReportPath`; если пути не переданы, используются стандартные шаблоны из `docs/`.
- Blocking/summary payload теперь показывает пути всех evidence reports, чтобы было понятно, какой отчет не готов.
- `frontend/package-lock.json` обновлен через `npm audit fix`, текущий frontend audit возвращает `0 vulnerabilities`.
- `frontend/scripts/playwright-webservers.mjs` больше не зависит от локального `apps/*/node_modules/vite` и корректно запускает E2E при hoisted workspace-зависимостях.
- Current status обновлен до backend `510/510`, latest release `2026-06-18-production-evidence-bundle-gate`.

### Verified

- `ProductionReadinessGateTests`: 3/3.
- `assert-production-readiness.ps1` остается fail-closed на текущих blocked templates.
- Backend full suite: 510/510.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Production-ready все еще требует реальных passed reports по VPS/staging, платежам, админке VPS и live VPN/3x-ui.

## 0.123.0 - 2026-06-14

Release entry: `2026-06-14-vpn-live-smoke-report`.

### Added

- `docs/vpn-live-smoke-report.template.json` фиксирует обязательный smoke-отчет для production-like VPN выдачи через реальную 3x-ui/x-ui панель.
- `scripts/new-vpn-live-smoke-report.ps1` создает безопасный blocked-черновик отчета с latest release, API URL, admin URL, 3x-ui URL и оператором.
- `scripts/validate-vpn-live-smoke-report.ps1` проверяет URL, даты, top-level VPN gates, обязательные checks и forbidden secret markers, включая полные VPN URI.
- `docs/vpn-live-smoke.md` описывает, как пройти 3x-ui/inbound/node/order/webhook/subscription/client/fail-closed smoke без сохранения секретов.

### Changed

- Current status обновлен до backend `509/509`, latest release `2026-06-14-vpn-live-smoke-report`.
- `P0-VPN-001` ... `P0-VPN-005` остаются открытыми до реальной 3x-ui проверки, но теперь у них есть обязательный формат safe evidence.

### Verified

- Generated VPN live smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `VpnLiveSmokeReportTests`: 4/4.
- Backend full suite: 509/509.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Нужно пройти реальную 3x-ui/x-ui панель, inbound, production VPN node и production-like order smoke, затем заполнить отчет safe evidence.

## 0.122.0 - 2026-06-14

Release entry: `2026-06-14-admin-vps-smoke-report`.

### Added

- `docs/admin-vps-smoke-report.template.json` фиксирует обязательный smoke-отчет для проверки всех разделов админки на VPS.
- `scripts/new-admin-vps-smoke-report.ps1` создает безопасный blocked-черновик отчета с latest release, API URL, admin URL и оператором.
- `scripts/validate-admin-vps-smoke-report.ps1` проверяет URL, даты, общие login/console/API gates, все admin sections и forbidden secret markers.
- `docs/admin-vps-smoke.md` описывает, как пройти VPS admin smoke без сохранения секретов.

### Changed

- Current status обновлен до backend `505/505`, latest release `2026-06-14-admin-vps-smoke-report`.
- `P0-ADMIN-002` остается открытым до реального VPS admin smoke, но теперь у него есть обязательный формат безопасного evidence.

### Verified

- Generated admin VPS smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `AdminVpsSmokeReportTests`: 4/4.
- Backend full suite: 505/505.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Нужно пройти `/admin/` на реальном VPS под production admin-аккаунтом и заполнить отчет safe evidence по всем разделам.

## 0.121.0 - 2026-06-14

Release entry: `2026-06-14-payment-provider-smoke-generator`.

### Added

- `scripts/new-payment-provider-smoke-report.ps1` создает безопасный черновик payment provider smoke report из `docs/payment-provider-smoke-report.template.json`.
- Генератор принимает `EnvironmentName`, `Operator`, `ReleaseId`, `Mode` (`sandbox` или `live`) и подставляет latest release из seed "Что нового", если `ReleaseId` не задан.
- Все провайдеры создаются со статусом `blocked`, пустыми gate-флагами и TODO evidence, поэтому real provider smoke остается fail-closed до внешней проверки.

### Changed

- `docs/payment-provider-smoke.md` теперь рекомендует начинать отчет через генератор, а не ручное копирование JSON.
- Current status обновлен до backend `501/501`, latest release `2026-06-14-payment-provider-smoke-generator`.

### Verified

- Generated payment provider smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `PaymentProviderSmokeReportTests`: 5/5.
- Backend full suite: 501/501.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальные provider smoke reports для YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe и PayPal еще нужно заполнить после внешних sandbox/live проверок.

## 0.120.0 - 2026-06-14

Release entry: `2026-06-14-payment-provider-smoke-report`.

### Added

- `docs/payment-provider-smoke-report.template.json` фиксирует обязательную smoke-матрицу для YooKassa, RoboKassa, YooMoney, CloudPayments, TBankAcquiring, Prodamus, Stripe и PayPal.
- `scripts/validate-payment-provider-smoke-report.ps1` проверяет структуру отчета, даты, дубли провайдеров, обязательные payment gates, безопасные evidence и forbidden secret markers.
- `docs/payment-provider-smoke.md` описывает, как заполнять provider smoke report и почему Telegram Stars проверяется отдельным Telegram invoice flow.
- `PaymentProviderSmokeReportTests` закрепляет fail-closed шаблон и связь отчета с roadmap.

### Changed

- Current status обновлен до backend `500/500`, latest release `2026-06-14-payment-provider-smoke-report`.
- `STATE-011` и `P0-PAY-002` ... `P0-PAY-009` остаются открытыми до реального sandbox/live отчета по внешним кабинетам.

### Verified

- Payment provider smoke report validator: OK.
- `-RequireAllPassed` для blocked шаблона: expected failure.
- `PaymentProviderSmokeReportTests`: 4/4.
- Backend full suite: 500/500.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальные YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe и PayPal кабинеты еще нужно пройти и приложить safe evidence без секретов.

## 0.119.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-generator`.

### Added

- `scripts/new-staging-smoke-report.ps1` создает безопасный черновик staging/VPS smoke report из `docs/staging-smoke-report.template.json`.
- Генератор принимает `ApiBaseUrl`, web URL-ы, `EnvironmentName`, `Operator`, `ReleaseId` и подставляет latest release из seed "Что нового", если `ReleaseId` не задан.
- Все обязательные checks создаются со статусом `blocked` и TODO evidence, поэтому production readiness gate остается fail-closed до реального прогона.

### Changed

- `docs/staging-smoke-checklist.md` теперь рекомендует начинать заполнение отчета через генератор, а не ручное копирование JSON.
- Current status обновлен до backend `496/496`, latest release `2026-06-14-staging-smoke-report-generator`.

### Verified

- `StagingSmokeChecklistTests`: 7/7.
- Generated staging smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- Backend full suite: 496/496.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальный staging/VPS smoke report пока не заполнен; live-платежи, 3x-ui и production-ready решение остаются внешними блокерами.

## 0.118.0 - 2026-06-14

Release entry: `2026-06-14-telegram-stars-invoice-gate`.

### Changed

- Telegram Stars теперь считается готовым для bot checkout только при явном `ExtraSettingsJson.status = "invoice-flow"`.
- Режим `bot-only` остается безопасным состоянием: Stars скрыт из web checkout и не появляется в Telegram-клавиатуре оплаты как готовый способ.
- Проверка подключения платежного провайдера в админке показывает Stars как `Unhealthy` для `bot-only` и `Healthy` для явного `invoice-flow`.
- Production-настройка Telegram Stars больше не требует web secret key, потому что Stars работает через Telegram invoice update flow.
- Current status обновлен до backend `495/495`, latest release `2026-06-14-telegram-stars-invoice-gate`.

### Verified

- Targeted payment/Telegram suite: 61/61.
- Backend full suite: 495/495.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Live BotFather/Telegram Stars smoke с реальным BotToken и Telegram окружением остается внешним production-блокером вместе с live-платежами и VPS/3x-ui проверками.

## 0.117.0 - 2026-06-14

Release entry: `2026-06-14-telegram-webhook-boundary`.

### Добавлено

- Guard-тесты `TelegramBotProcessBoundaryTests`, которые запрещают возвращать `/telegram/webhook` в standalone bot-процесс и проверяют документацию по основному API webhook.
- Roadmap-пункт `P1-TG-006` для явной границы ответственности между основным API и standalone Telegram bot process.

### Обновлено

- `VpnPlatform.TelegramBot` больше не мапит webhook route и остается для LongPolling, очереди Telegram-уведомлений и health endpoints.
- `docs/phase-3-telegram-foundation.md`, `docs/telegram-bot-setup.md`, README и production example указывают webhook на `/api/channels/telegram/webhook` основного API.
- Current status обновлен до backend `493/493`, latest release `2026-06-14-telegram-webhook-boundary`.

### Проверено

- Targeted Telegram boundary/API suite: 41/41.
- Standalone TelegramBot build: OK, предупреждений 0.
- Backend full suite: 493/493.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Реальный Telegram/BotFather webhook и Telegram Stars live/sandbox smoke остаются внешними production-блокерами `STATE-011`, `P11-ACC-002` и `P9-TST-007`.

## 0.116.0 - 2026-06-14

Release entry: `2026-06-14-api-telegram-webhook`.

### Добавлено

- API endpoint `/api/channels/telegram/webhook` теперь обрабатывает Telegram updates в основном backend вместо `501 NotImplemented`.
- Runtime-настройки Telegram-бота читаются из админки/БД с fallback на `appsettings`, включая protected BotToken и webhook secret.
- Infrastructure получил `TelegramBotHttpClient`, который отправляет Telegram Stars invoice, отвечает на `pre_checkout_query` и отправляет сообщения через общий `ITelegramInvoiceProvider`.
- Guard-тесты `ChannelWebhooksControllerTests` проверяют успешную обработку webhook, duplicate update и выключенный Telegram-бот.

### Обновлено

- Current status обновлен до backend `491/491`, latest release `2026-06-14-api-telegram-webhook`.
- Roadmap получил закрытый пункт `P1-TG-005` для Telegram webhook в основном API.

### Проверено

- Targeted Telegram/API suite: 39/39.
- Backend full suite: 491/491.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Live Telegram webhook с реальным BotFather/Bot API и реальные Telegram Stars платежи остаются частью production/staging smoke: `STATE-011`, `P11-ACC-002` и `P9-TST-007`; live smoke не закрывался.

## 0.115.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-url-validation`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет обязательные absolute http/https URL для `apiBaseUrl`, `publicWebUrl`, `cabinetWebUrl` и `adminWebUrl`.
- Roadmap-подпункт `P9-TST-007C` для локально закрытого URL validation слоя.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь отклоняет пустой или невалидный `apiBaseUrl`, а также непустые web URL без абсолютной `http`/`https` схемы.
- `docs/staging-smoke-checklist.md` описывает URL-правила для staging smoke report.
- Current status обновлен до backend `489/489`, latest release `2026-06-14-staging-smoke-report-url-validation`.

### Проверено

- Backend full suite: 489/489.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: URL validation закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.114.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-consistency`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет запрет на `completedAt` раньше `startedAt` и duplicate check id в staging smoke report.
- Roadmap-подпункт `P9-TST-007B` для локально закрытого consistency guard.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь проверяет хронологию `startedAt`/`completedAt` и не принимает повторяющиеся check id.
- `docs/staging-smoke-checklist.md` описывает эти правила как обязательную часть report validation.
- Current status обновлен до backend `488/488`, latest release `2026-06-14-staging-smoke-report-consistency`.

### Проверено

- Backend full suite: 488/488.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: consistency guard закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.113.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-secret-sanitizer`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет forbidden-маркеры для cookies, `.env`, client secrets, API keys, private headers, Telegram secret header и GitHub/VPS secret names.
- Roadmap-подпункт `P9-TST-007A` для локально закрытой части staging smoke report sanitizer.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь дополнительно блокирует `Cookie:`, `Set-Cookie:`, `.env`, `client_secret`, `api_key`, `private header`, `X-Telegram-Bot-Api-Secret-Token`, `PRODUCTION_ENV_FILE` и `VPS_SSH_KEY`.
- `docs/staging-smoke-checklist.md` и `docs/production-readiness-gate.md` уточняют, какие данные нельзя сохранять в smoke-отчет.
- Current status обновлен до backend `487/487`, latest release `2026-06-14-staging-smoke-secret-sanitizer`.

### Проверено

- Backend full suite: 487/487.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: sanitizer закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.112.0 - 2026-06-14

Release entry: `2026-06-14-production-readiness-gate`.

### Добавлено

- Fail-closed gate `scripts/assert-production-readiness.ps1`, который проверяет staging/VPS smoke report через `validate-staging-smoke-report.ps1 -RequireAllPassed` и дополнительно блокирует production-ready при открытых P0/P11/STATE blockers в roadmap или текущем решении `staging-ready baseline`.
- Документ `docs/production-readiness-gate.md` с инструкцией запуска и объяснением, почему текущий baseline должен падать до реального smoke-отчета.
- Guard-тест `ProductionReadinessGateTests`, который закрепляет наличие скрипта, документации, roadmap-пункта `P11-ACC-008`, release seed и TEST_RESULTS.

### Обновлено

- README, финальный runbook, release decision, docs index и master roadmap синхронизированы с latest release `0.112.0`.
- Current status обновлен до backend `486/486`, latest release `2026-06-14-production-readiness-gate`.

### Проверено

- Backend full suite: 486/486.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо падает fail-closed, потому что smoke checks еще `blocked`.

### Ограничения

- Gate не закрывает live-платежи, реальный 3x-ui и VPS admin/live smoke; он только запрещает пометить проект production-ready без их доказательств.

## 0.111.0 - 2026-06-14

Release entry: `2026-06-14-product-admin-roadmap-sync`.

### Добавлено

- Guard-тест `ProductAdminUiRoadmapSyncTests`, который проверяет актуальность продуктового UI-roadmap и отсутствие старых незакрытых чекбоксов по уже покрытым локальным UX/API/E2E задачам.

### Обновлено

- `docs/product-admin-ui-roadmap.md` переписан как компактный актуальный продуктовый срез: локальный сайт, кабинет, админка, UX, API и smoke-проверки закрыты, а live-платежи, реальный 3x-ui и VPS smoke оставлены открытыми.
- Current status обновлен до backend `484/484`, latest release `0.111.0`.

### Проверено

- Backend full suite: 484/484.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Product/UI roadmap не закрывает production-ready: P0/live-задачи по VPS, платежам и 3x-ui остаются в master roadmap.

## 0.110.0 - 2026-06-14

Release entry: `2026-06-14-provisioning-secret-bug-sync`.

### Добавлено

- Guard-тест `ProvisioningSecretStatusConsistencyTests`, который связывает `BUG-006`, security-документацию, `ProvisioningSecretMaterializer` и открытые live smoke блокеры.

### Исправлено

- `BUG-006` больше не числится открытым из-за secret materialization: protected `ssh_key` временно материализуется только через `ProvisioningSecretMaterializer`, runner получает path, а файл удаляется в `finally`.
- `docs/SECURITY_HARDENING_MVP.md` больше не содержит устаревшую формулировку, что protected SSH credentials невозможно передать в Ansible.

### Проверено

- Backend full suite: 483/483.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Полный live provisioning smoke, VPS production smoke, реальные 3x-ui и платежные кабинеты остаются открытыми P0/P11-блокерами.

## 0.109.0 - 2026-06-14

Release entry: `2026-06-14-roadmap-bug-register-sync`.

### Добавлено

- Guard-тест `BugRegisterConsistencyTests`, который проверяет, что локально закрытые баги в журнале ошибок не остаются в статусе `open`.

### Исправлено

- `BUG-004` в roadmap больше не числится открытым: полный browser E2E public/cabinet/admin уже закрыт через `P9-TST-008`, all-screens и console smoke.
- `BUG-005` в roadmap больше не числится открытым: синхронизация документации и проверка кодировки закрыты через `P10-DOC-005`, `STATE-014` и guard-тесты.

### Проверено

- Backend full suite: 482/482.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `BUG-001`, `BUG-002`, `BUG-003`, `BUG-006` и P0/live-задачи остаются открытыми до реального VPS, платежных кабинетов, 3x-ui и provisioning smoke.

## 0.108.0 - 2026-06-14

Release entry: `2026-06-14-roadmap-current-state-sync`.

### Добавлено

- Guard-тест `RoadmapCurrentStateTests`, который закрепляет актуальный верхний статус roadmap и связь с README, release decision, final runbook, TEST_RESULTS, changelog и seed "Что нового".
- Запись "Что нового" `2026-06-14-roadmap-current-state-sync`.

### Обновлено

- Верхний блок `docs/PRODUCT_COMPLETION_ROADMAP.md` синхронизирован с текущими проверками: backend `480/480`, frontend `65/65`, browser console smoke `9/9`, latest release `0.108.0`.
- README, `docs/final-runbook.md` и `docs/release-decision.md` теперь показывают один и тот же latest release.

### Проверено

- Backend full suite: 480/480.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Production-ready статус все еще заблокирован live-платежами, реальной 3x-ui выдачей, VPS admin/live smoke и заполненным staging/VPS smoke report.

## 0.107.0 - 2026-06-14

Release entry: `2026-06-14-all-screens-browser-smoke`.

### Добавлено

- `frontend/e2e/all-screens.spec.ts` с mock-based browser smoke для всех основных экранов public web, кабинета и админки.
- Playwright project `all-screens`.
- npm-скрипт `e2e:all-screens`.
- Документация `docs/all-screens-browser-smoke.md`.
- Guard-тест `AllScreensBrowserSmokeTests`.

### Проверяется

- public routes `/`, `/tariffs`, `/faq`, `/help`, `/account`;
- cabinet auth screen и авторизованный dashboard;
- admin sections `dashboard`, `users`, `payments`, `tariffs`, `subscriptions`, `vpn`, `nodes`, `panels`, `support`, `audit`, `bot`, `releases`, `faq`, `content`, `scenarios`, `provisioning`;
- отсутствие пустого body;
- отсутствие `console.error` и `pageerror`.

### Проверено

- Backend full suite: 478/478.
- `npm run e2e:all-screens --prefix frontend`: 3/3.
- Browser console smoke: 9/9.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.

### Ограничения

- Smoke использует mock API и не подтверждает live-платежи, live 3x-ui или реальный VPS.

## 0.106.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-checklist`.

### Добавлено

- `docs/staging-smoke-checklist.md` с обязательным staging smoke checklist для покупки, оплаты, подписки, VPN-доступа, админки, поддержки и отсутствия browser console errors.
- `docs/staging-smoke-report.template.json` как безопасный шаблон отчета без секретов.
- `scripts/validate-staging-smoke-report.ps1` для структурной проверки отчета и fail-closed release gate через `-RequireAllPassed`.
- Guard-тест `StagingSmokeChecklistTests`.

### Проверяется

- обязательные check id для deploy, health, public/cabinet/admin web, admin login, tariffs, payment providers, checkout, payment init, provider confirmation, subscription, VPN access, support, console, secret rotation и no secret leak;
- запрет на пароли, bearer-токены, private keys и webhook secrets в отчете;
- связка docs index, changelog, TEST_RESULTS и seed "Что нового".

### Проверено

- Backend full suite: 476/476.
- Staging smoke report validator: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.

### Ограничения

- Реальный staging/VPS smoke report еще должен быть заполнен после deploy и настройки внешних sandbox-интеграций.
- Production-ready статус остается заблокированным до live evidence по платежам, 3x-ui и VPS.

## 0.105.0 - 2026-06-14

### Добавлено

- `scripts/vps-production-smoke.ps1` для полного HTTP-smoke против VPS или staging API.
- Документация `docs/vps-production-smoke.md`.
- Guard-тест `VpsProductionSmokeTests`.

### Проверяется

- health live/ready;
- optional public/cabinet/admin web URLs;
- optional admin login и dashboard;
- public checkout session;
- user registration;
- order claim;
- payment init;
- sandbox webhook только в non-Production;
- active subscription;
- VPN access URI.

### Ограничения

- Live VPS smoke должен запускаться отдельно после deploy и ротации раскрытых секретов.
- `-AllowSandboxWebhook` запрещен, если API сообщает `Production`.

### Проверено

- Backend full suite: 473/473.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.

## 0.104.0 - 2026-06-14

### Добавлено

- Release decision `docs/release-decision.md`.
- Guard-тест `ReleaseDecisionTests`, который закрепляет статус `staging-ready baseline` и блокеры production-ready.
- Release entry `2026-06-14-release-decision` для раздела "Что нового".

### Решение

- Текущий статус: **staging-ready baseline, не production-ready**.
- Причина: `P11-ACC-002 VPS production smoke` остается открытым, а production требует live VPS smoke, ротации раскрытых секретов, реального домена/HTTPS, provider-specific sandbox smoke и реальной 3x-ui проверки.

### Проверено

- Backend full suite: 470/470.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.
- Frontend unit tests: 65/65.
- Frontend typecheck/build: OK.
- High-severity frontend audit: OK; остаются 2 moderate advisory по `react-router`.

## 0.103.0 - 2026-06-14

### Добавлено

- Финальный runbook `docs/final-runbook.md`: локальный запуск без Docker, полный validation gate, browser smoke, security gate, deploy на VPS и post-deploy smoke.
- Guard-тест `FinalDocsChangelogTests`, который связывает README, docs index, roadmap, changelog, `TEST_RESULTS.md` и seed "Что нового".
- Release entry `2026-06-14-final-docs-changelog` для админского раздела "Что нового".

### Проверено

- Backend full suite: 467/467.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.
- Frontend unit tests: 65/65.
- Frontend typecheck/build: OK.
- API Release build: OK.
- High-severity frontend audit: OK; остаются 2 moderate advisory по `react-router`.
- UTF-8/encoding guard: OK.

### Ограничения

- Проект находится на уровне локально подтвержденного staging-ready baseline.
- Production-ready решение требует отдельного live VPS smoke, ротации раскрытых секретов, production домена/HTTPS, реальных sandbox-кабинетов платежных провайдеров и проверки 3x-ui панели.

## 0.102.0 - 2026-06-14

### Добавлено

- Финальный security checklist `docs/security-final-checklist.md`.
- Guard `SecurityFinalChecklistTests` для admin auth policies, headers, rate limits, secrets и webhook/security gates.

### Исправлено

- `scan-secrets.ps1` и `scan-secrets.sh` исключают generated Playwright artifacts, чтобы E2E-прогоны не ломали actual secret scan исчезающими временными файлами.

## 0.101.0 - 2026-06-14

### Добавлено

- Browser console smoke `npm run e2e:console --prefix frontend`.
- Проверка desktop/mobile public, cabinet и admin поверхностей на отсутствие `console.error` и `pageerror`.

## 0.100.0 - 2026-06-14

### Добавлено

- Mobile smoke для public, cabinet и admin.
- Mobile Playwright-проекты и PNG-артефакты для основных экранов.

## 0.99.0 - 2026-06-13

### Добавлено

- Fresh local smoke на чистой SQLite-БД: health, seed, sandbox payment, webhook, subscription и VPN access.

### Исправлено

- SQLite-сортировка `/api/me/orders` по `DateTimeOffset` перенесена после materialize.
