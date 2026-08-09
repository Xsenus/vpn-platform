# VPN Platform

VPN Platform - монорепозиторий платформы для продажи VPN-подписок через публичный сайт, личный кабинет, админку и Telegram-бота.

Платформа решает полный продуктовый сценарий:

- пользователь выбирает тариф, регистрируется, создает заказ и оплачивает подписку;
- backend принимает webhook платежного провайдера, активирует подписку и выдает VPN-доступ;
- личный кабинет показывает активную подписку, ссылку подключения, QR-код, историю заказов, платежей и обращений в поддержку;
- администратор управляет тарифами, пользователями, платежами, возвратами, FAQ, текстами сайта, сценариями работы, VPN-серверами, 3x-ui панелями, inbound-ами, клиентами и Telegram-ботом;
- фоновые worker-сценарии обрабатывают outbox, lifecycle подписок, health/sync VPN-панелей и provisioning.

Главный roadmap: [docs/PRODUCT_COMPLETION_ROADMAP.md](docs/PRODUCT_COMPLETION_ROADMAP.md).
Changelog: [CHANGELOG.md](CHANGELOG.md).
Финальный runbook запуска, проверки и deploy: [docs/final-runbook.md](docs/final-runbook.md).
Release decision: [docs/release-decision.md](docs/release-decision.md).
Production readiness gate: [docs/production-readiness-gate.md](docs/production-readiness-gate.md).
VPS production smoke: [docs/vps-production-smoke.md](docs/vps-production-smoke.md).
Staging smoke checklist: [docs/staging-smoke-checklist.md](docs/staging-smoke-checklist.md).
All screens browser smoke: [docs/all-screens-browser-smoke.md](docs/all-screens-browser-smoke.md).
Инструкция по GitHub Actions и деплою на VPS: [docs/github-deployment.md](docs/github-deployment.md).
Руководство администратора: [docs/admin-guide.md](docs/admin-guide.md).
Admin bootstrap/reset: [docs/admin-bootstrap.md](docs/admin-bootstrap.md).
Индекс документации: [docs/README.md](docs/README.md).

## Состав проекта

- `backend/src/VpnPlatform.Domain` - доменные сущности, enum-статусы и базовые правила.
- `backend/src/VpnPlatform.Application` - бизнес-сервисы, DTO, orchestration платежей, подписок и VPN-доступов.
- `backend/src/VpnPlatform.Infrastructure` - EF Core, PostgreSQL/SQLite, платежные адаптеры, 3x-ui, provisioning, секреты, hosted workers.
- `backend/src/VpnPlatform.Api` - ASP.NET Core API, auth, публичные endpoints, кабинет, админка, webhooks, health и metrics.
- `backend/src/VpnPlatform.TelegramBot` - отдельный процесс LongPolling и отправки Telegram-уведомлений; webhook принимает основной API.
- `frontend/apps/public-web` - публичный сайт с тарифами, FAQ и покупкой.
- `frontend/apps/cabinet` - личный кабинет пользователя.
- `frontend/apps/admin-panel` - административная панель.
- `frontend/packages/api-client` - общий typed API client.
- `frontend/packages/ui` - общие UI-примитивы.
- `infra/` - Ansible, Prometheus, Grafana, Loki.
- `scripts/` - локальный запуск, валидация, аудит секретов, deploy/smoke helpers.
- `docs/` - русская документация по запуску, платежам, Telegram, provisioning, CI/CD и проверкам.

## Быстрый запуск без Docker

Локальный режим предназначен для проверки на рабочей машине без Docker. Он использует SQLite-файл `data/vpnplatform-local.db`, поэтому PostgreSQL, Redis и RabbitMQ не нужны.

Требования:

- .NET SDK 9.
- Node.js 22.22+ и npm.
- PowerShell.

Первый запуск из корня репозитория:

```powershell
dotnet restore backend\VpnPlatform.sln
cd frontend
npm install
cd ..
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1
```

После запуска доступны:

- API и Swagger: `http://127.0.0.1:8080/swagger`
- публичный сайт: `http://127.0.0.1:5173`
- личный кабинет: `http://127.0.0.1:5174`
- админка: `http://127.0.0.1:5175`
- локальный администратор: `admin@local.test` / `LocalAdminPassword123!`

Если стандартные порты заняты:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 8081 -PublicPort 5373 -CabinetPort 5374 -AdminPort 5375
```

Остановка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
```

Логи локального запуска пишутся в `data/logs/`. SQLite-база, логи и runtime-файлы в `data/` не должны попадать в git.

## Восстановление локального администратора

Если локальная SQLite-база уже существовала и пароль администратора был изменен, сбросьте его без запуска HTTP-сервера:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Local"
powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 -LocalSqlite -EnvironmentName Local -Email admin@local.test -Password "LocalAdminPassword123!"
```

Скрипт запускает backend-команду `admin-bootstrap` без HTTP-сервера, создает администратора при отсутствии или сбрасывает пароль существующего администратора только при явном CLI-запуске. Пароль в вывод команды не печатается. Production/Postgres пример описан в [docs/admin-bootstrap.md](docs/admin-bootstrap.md).

## Ручной запуск без Docker

Backend:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Local"
$env:DataProtection__KeyPath="../../../tmp/dataprotection-keys"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --urls http://127.0.0.1:8080
```

`DataProtection__KeyPath` нужен, чтобы локальный API хранил ключи авторизации внутри рабочей папки проекта, а не в профиле Windows.

Frontend в отдельных терминалах:

```powershell
cd frontend
$env:VITE_API_BASE_URL="http://127.0.0.1:8080"
npm run dev:public
npm run dev:cabinet
npm run dev:admin
```

## Проверка, что все работает

Backend:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
```

Frontend:

```powershell
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
```

Playwright E2E:

```powershell
npm run e2e:public --prefix frontend
npm run e2e:cabinet --prefix frontend
npm run e2e:admin --prefix frontend
npm run e2e:all-screens --prefix frontend
npm run e2e:mobile --prefix frontend
npm run e2e:console --prefix frontend
```

Проверка health локального API:

```powershell
Invoke-RestMethod http://127.0.0.1:8080/health/live
Invoke-RestMethod http://127.0.0.1:8080/health/ready
```

## Запуск через Docker

Docker-режим нужен для production-like проверки PostgreSQL, Redis, RabbitMQ и compose-сборки:

```powershell
docker compose up -d postgres redis rabbitmq prometheus grafana loki
docker compose up --build backend-api public-web cabinet admin-panel
```

Если Docker Desktop не запущен, compose-команды завершатся ошибкой подключения к Docker engine. Это не влияет на локальный SQLite-режим без Docker.

## VPS и staging

Поддерживаются два режима deploy:

- Docker deploy - если на сервере доступен Docker/Compose.
- Systemd deploy без Docker - self-contained Linux x64 API, nginx для frontend и PostgreSQL 16.

Основные пути systemd-режима:

- API: `/opt/vpn-platform/api`
- frontend: `/opt/vpn-platform/web/public`, `/opt/vpn-platform/web/cabinet`, `/opt/vpn-platform/web/admin`
- env-файл API: `/etc/vpn-platform/api.env`
- systemd service: `vpn-platform-api`
- PostgreSQL database: `vpnplatform`

Проверки на сервере:

```bash
systemctl status vpn-platform-api --no-pager
systemctl status postgresql --no-pager
curl -fsS http://127.0.0.1:8080/health/live
curl -fsS http://127.0.0.1:8080/health/ready
curl -fsS http://127.0.0.1:8080/api/public/tariffs
curl -fsS http://127.0.0.1:8080/api/public/payments/providers
```

Post-deploy smoke описан в [docs/post-deploy-smoke.md](docs/post-deploy-smoke.md).

## Платежи и VPN

В коде есть адаптеры YooMoney, YooKassa, RoboKassa, CloudPayments, TBank Acquiring, Prodamus, Stripe, PayPal и Telegram Stars.

Локальный sandbox безопасен: checkout создается без реальных денег и без внешних API. Live-провайдеры требуют реальные учетные данные, webhook-секреты и отдельный smoke по каждому кабинету провайдера.

Telegram Stars работает через отдельный Telegram invoice flow и не входит в web checkout провайдеров.

VPN-выдача поддерживает sandbox-режим и интеграцию с 3x-ui/x-ui. Реальная production-выдача требует подключенной панели, inbound-а, VPN-ноды и успешного live smoke.

## Важные режимы

- `ASPNETCORE_ENVIRONMENT=Local` - локальный режим без Docker: SQLite, demo admin, sandbox payments/VPN.
- `ASPNETCORE_ENVIRONMENT=Development` - режим разработки с локальной инфраструктурой.
- `ASPNETCORE_ENVIRONMENT=Production` - строгий режим: PostgreSQL обязателен, SQLite и небезопасные placeholders запрещены startup validator-ом.

## Текущий статус

На 2026-08-09 локально подтверждено:

- backend на .NET 9: `1112/1112` unit tests;
- API Release build: без ошибок и предупреждений;
- frontend unit tests: `112/112`;
- frontend typecheck и production build: OK;
- frontend dependency audit: `0 vulnerabilities`; React 19.2.8 и React Router 8.3.0 проверены на Node.js 22.22.0;
- Playwright E2E: public, cabinet, admin, all-screens, mobile и console smoke проходят `78/78`; responsive matrix проверяет 18 конфигураций `305x568..2560x1440`, включая mobile landscape и обе стороны CSS-breakpoints;
- local SQLite HTTP-smoke проходит: live/ready, admin login и latest release;
- VPS production smoke runner добавлен и локально проверяется через SQLite dry-run;
- production readiness gate добавлен и fail-closed блокирует production-ready без passed staging/VPS smoke report и закрытых P0/P11/STATE blockers;
- checkout, кабинет и административные PATCH/API-операции отклоняют некорректные enum/JSON-значения ответом 400 без частичного изменения данных;
- browser gate дополнительно проверяет landmark, уникальные ID, alt-тексты, доступные имена контролов и same-origin decode фоновых assets на public, cabinet и 17 admin экранах;
- миграция подписки валидирует источник, целевой сервер и дубли; архивные серверы нельзя вернуть в работу через административные mode-actions;
- команды `extend/activate/block/unblock/cancel` не изменяют подписку при ошибке VPN-провайдера; unhealthy/full migration target отклоняется;
- удаление VPN-сервера сохраняет health-check и migration history через архивирование, а связанный ключ рабочего сценария нельзя переименовать;
- административные user/content/release/referral/support/Telegram и 3x-ui операции создают redacted audit trail с admin/system actor; panel sync откатывает частичные изменения, а remote inbound/client create компенсируется при отказе локального commit;
- возвраты резервируются локально до provider call; параллельные повторы дедуплицируются, а `New/Pending/Unknown` refund блокирует новую операцию до ручной сверки;
- payment init сериализуется по order id, сохраняет reservation до provider call и восстанавливает remote checkout после transient final commit failure;
- публичный список payment providers и checkout используют единый детерминированный selector готового web-аккаунта; неготовый default не перехватывает показанный fallback;
- manual payment recheck пробрасывает caller cancellation и не записывает ложную business-ошибку, audit или outbox;
- activation компенсирует remote access после local credential save failure, сохраняет `SyncRequired` при неудачном cleanup и пробрасывает caller cancellation после durable retry-state;
- Telegram update резервируется до side effects, защищен lease и повторяется после failed/stale processing без двойного invoice или потери long-poll offset;
- Telegram response и pre-checkout acknowledgement сохраняются до отправки, доставляются через отдельную lease/backoff и восстанавливаются webhook/long-polling без повторной обработки update;
- Telegram notification dispatcher атомарно захватывает pending/stale sending записи, восстанавливает их по lease/backoff и завершает blocked/invalid/max-attempt случаи без двойной отправки;
- outbox dispatcher атомарно захватывает события, восстанавливает stale lease, применяет redacted retry/dead-letter и материализует локальную email-очередь без ложного `ProcessedAt` до handler;
- email worker доставляет pending-уведомления через явный SMTP mode, восстанавливает stale lease/backoff, хранит reset-код только в защищенном payload и предоставляет маскированный admin retry;
- provisioning worker атомарно захватывает запуск, ограничивает runner timeout, восстанавливает stale lease без автоматического replay и блокирует небезопасные retry/cancel;
- истечение подписки отключает VPN-доступ до перевода в `Expired`, сохраняет lease/backoff и диагностируемый retry-state; lifecycle, panel health и panel sync workers изолируют ошибки отдельных записей;
- синхронизация 3x-ui использует межинстансный claim, восстанавливает stale `Running`, отклоняет устаревший worker snapshot и сохраняет только redacted health/sync diagnostics;
- выдача 3x-ui сериализована по подписке, capacity защищена optimistic concurrency, продление сохраняет назначенный inbound, а remote create/update/delete/enable/disable компенсируются при локальном отказе;
- мобильная админка использует компактный селектор раздела, а счетчик и переходы следуют фактическому порядку меню;
- terminal cancel подписки атомарно отзывает VPN-доступ, удаляет provider-клиента и освобождает node/panel/inbound capacity; rollback и reconciliation покрыты SQLite fault-injection;
- ручной перенос 3x-ui клиента резервирует target panel/inbound capacity до remote add и полностью компенсирует source/target при failure/cancellation/local-save ошибке;
- кабинет не предлагает QR до выдачи VPN URI; все карточки и handler используют единое правило готовности доступа;
- кабинет не предлагает повторную оплату истёкшего заказа и ведёт пользователя к новому оформлению;
- public checkout блокирует все тарифы и параметры на время запроса, а кабинет показывает надежную ссылку повторной оплаты без зависимости от popup;
- shared copy-кнопки показывают проверенный результат Clipboard API, обрабатывают отказ разрешения и сохраняют стабильную геометрию на mobile/desktop;
- public web сохраняет и ротирует refresh session, вызывает backend logout и очищает browser tokens даже при недоступности revoke-запроса;
- cabinet logout гарантированно удаляет локальные токены и пользовательские/VPN-данные при success или failure backend revoke;
- admin deactivation атомарно отзывает refresh-сессии; каждый JWT-запрос повторно проверяет активность user, а кабинет и linked Telegram account fail-closed очищают или скрывают чувствительные данные;
- admin-panel получает capability matrix из защищенной административной сессии, скрывает недоступные разделы и команды, не вызывает запрещенные доменные API, а user overview не раскрывает finance/support данные без соответствующего read-policy;
- dashboard редактирует финансовые и support-агрегаты по backend policy, readiness не возвращает недоступные payment/Telegram checks, а UI не показывает скрытые метрики и переходы partial roles;
- журнал аудита применяет finance/support/Telegram scope до пользовательских фильтров и не возвращает partial role чужие actions, entity types или JSON payload;
- VPN access enable/sync/reset пробрасывают caller cancellation после durable audit/history; enable/reset и неопределенный reset failure переводят доступ в `SyncRequired` для ручной сверки;
- access JWT и refresh-токены привязаны к `session_version`; password reset, деактивация и admin bootstrap с изменением полномочий отзывают старые поколения, а public/cabinet очищают browser state после смены пароля;
- refresh-token replay ограничен одной login/rotation family и не может отзывать новые входы после logout/password reset; legacy NULL-family chains связываются безопасно при первом replay;
- успешный password reset инвалидирует остальные коды пользователя, а concurrency `Revision` не допускает второго параллельного подтверждения sibling token;
- кабинет выполняет один цикл загрузки защищенных данных для новой или восстановленной auth-сессии, включая запуск под `React.StrictMode`;
- конкурентная регистрация одного email возвращает `email_exists` без partial session/audit rows; unrelated DB errors не маскируются, а новые referral codes имеют 64 случайных бита;
- повторный password reset request немедленно закрывает старый код; generation/revision сериализует concurrent reissue и stale reset, включая admin bootstrap password change;
- refresh rotation защищена optimistic revision: один source token не создает две ветви, concurrent reuse отзывает winning family, а logout/admin deactivation повторяются без HTTP 500;
- обращения поддержки используют optimistic revision: stale reply/status/note получают controlled conflict, а pending Telegram/provisioning диалог возвращается в active queue после нового сообщения;
- checkout claim резервирует session, создаёт order и публикует связь одной transaction: same-user race возвращает winner, другой user не создаёт второй заказ, completed не деградирует в expired;
- реферальный код атомарно связывает пользователей, а завершение подходящей покупки через durable outbox создаёт идемпотентные начисления; кабинет получает redacted DTO, программы и журнал управляются в отдельном admin-разделе;
- ответы 3x-ui panel/inbound/client/sync/health API проходят runtime-проверку фактического backend-контракта; при поврежденном списке панелей UI очищает выбранную панель и защищен от позднего восстановления stale деталей;
- server, provisioning и Telegram bot API DTO проходят runtime-проверку фактического backend-контракта; malformed refresh очищает список серверов, запуски, форму редактирования и устаревшую bot readiness;
- auth, checkout, payment-init, support status и Telegram link-token ответы проходят runtime-проверку; mutation order проверяется по фактическому минимальному backend DTO, а устаревшие anonymous order/payment client-методы удалены;
- кабинет сохраняет уже созданный заказ продления при сбое payment init и повторяет подготовку оплаты по тому же `orderId`, не создавая дубликат;
- public checkout использует один single-flight claim/payment-init, сохраняет созданный order для payment-only retry и игнорирует поздний ответ после logout;
- public checkout восстанавливается из browser storage только после проверки структуры, лимитов, token format и payment-provider allow-list; повреждённое состояние удаляется до API-запросов;
- public session hydration защищён single-flight от StrictMode refresh-token reuse, сохраняет токены при transient profile failure, отбрасывает поздний refresh после logout и допускает checkout claim только после подтверждения профиля;
- cabinet support принимает сообщения только от последнего выбранного thread, отбрасывает delayed response после logout и очищает приватные drafts/reset state перед новым входом;
- admin user/support detail views принимают только актуальный selected/session response, показывают loading/empty states и не подменяют открытый диалог при status action другой записи;
- общий каталог inbound позволяет админке переносить VPN-клиента между совместимыми доступными 3x-ui панелями и после успеха открывать панель назначения;
- после межпанельного переноса карточки панелей сразу отражают освобождённую source capacity и занятую target capacity, а следующий API-refresh сохраняет фактические значения;
- admin mutation lifecycle привязывает CRUD/payment/subscription/VPN/provisioning/bot completion к исходной сессии, блокирует duplicate submit и сохраняет новые form drafts при delayed save/reload;
- public/cabinet mutation lifecycle блокирует duplicate auth/refresh/action requests, отбрасывает completion старой сессии и сохраняет более новые support/reset drafts;
- публичная витрина и admin login используют три локальных WebP из production bundle вместо runtime Unsplash; desktop/mobile screenshots и 18 viewport-конфигураций проверены без overflow и clipped controls;
- axe проверяет WCAG 2.0/2.1/2.2 A/AA и best practices без allow-list на 5 public routes, cabinet auth/dashboard и admin auth/17 sections на desktop и 320 px;
- changelog, финальный runbook, release decision, roadmap, продуктовый UI-roadmap и журнал ошибок синхронизированы с разделом "Что нового": `2026-08-09-automated-wcag-accessibility-gate`, версия `0.552.0`;
- roadmap progress: `565/585` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked;
- текущий release decision: `staging-ready baseline`, не production-ready;
- roadmap still keeps live/staging blockers, including `P11-ACC-002`, and cannot be treated as production-ready without real secrets, payment cabinets, VPS smoke and 3x-ui checks.
