# Changelog

Все заметные изменения проекта фиксируются в этом файле и в разделе "Что нового" внутри приложения. Подробный рабочий roadmap находится в `docs/PRODUCT_COMPLETION_ROADMAP.md`.

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
