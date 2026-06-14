# Production readiness gate

Документ закрывает локальный roadmap-пункт `P11-ACC-008`: перед тем как называть проект production-ready, теперь есть отдельная fail-closed команда, которая проверяет staging/VPS smoke report и не пропускает релиз, если roadmap или release decision все еще содержат открытые production-блокеры.

## Что проверяет gate

Команда `scripts/assert-production-readiness.ps1` выполняет две группы проверок:

- запускает `scripts/validate-staging-smoke-report.ps1 -RequireAllPassed`, поэтому все обязательные smoke-пункты должны быть `passed`, а отчет не должен содержать секреты, cookies, auth headers, private keys или provider tokens;
- читает `docs/PRODUCT_COMPLETION_ROADMAP.md` и `docs/release-decision.md`, затем блокирует production-ready, если остаются открытые `STATE-011`, `STATE-012`, `STATE-013`, `P0-*`, `P11-ACC-002`, `BUG-001`, `BUG-002`, `BUG-003` или решение все еще равно `staging-ready baseline`.

Это не заменяет реальные live-проверки. Gate нужен, чтобы не забыть зафиксировать доказательства и не выдать локально зеленый проект за production-ready.

## Как запускать

Сначала заполните отчет по шаблону `docs/staging-smoke-report.template.json`: замените `blocked` на реальные статусы, добавьте ссылки на GitHub Actions deploy, health responses, admin login, checkout, payment webhook, subscription, VPN access и подтверждение отсутствия секретов.

Проверка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
```

На текущем состоянии проекта команда должна завершаться ошибкой: шаблон содержит `blocked`, а master roadmap честно держит открытыми live-платежи, реальный 3x-ui, VPS admin smoke и `P11-ACC-002`.

После реального staging/VPS smoke команда сможет пройти только если одновременно выполнены условия:

- smoke report валиден и все checks имеют статус `passed`;
- секреты, cookies, `.env`, auth headers и provider keys не попали в отчет;
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
