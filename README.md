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
- Node.js 22+ и npm.
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

На 2026-06-14 локально подтверждено:

- backend на .NET 9: `706/706` unit tests;
- API Release build: без ошибок и предупреждений;
- frontend unit tests: `66/66`;
- frontend typecheck и production build: OK;
- frontend audit: OK, `0 vulnerabilities`;
- Playwright E2E: public, cabinet, admin, all-screens, mobile и console smoke проходят;
- local SQLite HTTP-smoke проходит: live/ready, admin login и latest release;
- VPS production smoke runner добавлен и локально проверяется через SQLite dry-run;
- production readiness gate добавлен и fail-closed блокирует production-ready без passed staging/VPS smoke report и закрытых P0/P11/STATE blockers;
- changelog, финальный runbook, release decision, roadmap, продуктовый UI-roadmap и журнал ошибок синхронизированы с разделом "Что нового": `2026-07-02-roadmap-progress-remaining-guard`, версия `0.413.0`;
- текущий release decision: `staging-ready baseline`, не production-ready;
- roadmap еще содержит live/staging задачи, которые нельзя считать production-ready без реальных секретов, платежных кабинетов, VPS smoke и 3x-ui проверки.
