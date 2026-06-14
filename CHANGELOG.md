# Changelog

Все заметные изменения проекта фиксируются в этом файле и в разделе "Что нового" внутри приложения. Подробный рабочий roadmap находится в `docs/PRODUCT_COMPLETION_ROADMAP.md`.

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
