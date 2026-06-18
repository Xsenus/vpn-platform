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

Это не заменяет реальные live-проверки. Gate нужен, чтобы не забыть зафиксировать доказательства и не выдать локально зеленый проект за production-ready.

## Как запускать

Сначала заполните отчет по шаблону `docs/staging-smoke-report.template.json`: замените `blocked` на реальные статусы, добавьте ссылки на GitHub Actions deploy, health responses, admin login, checkout, payment webhook, subscription, VPN access и подтверждение отсутствия секретов.

Проверка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
```

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
