# VPN Platform

Инструкция по публикации репозитория и автодеплою на VPS находится в [docs/github-deployment.md](docs/github-deployment.md).

Актуальный master-roadmap по доведению проекта до production-ready находится в [docs/PRODUCT_COMPLETION_ROADMAP.md](docs/PRODUCT_COMPLETION_ROADMAP.md).

Монорепозиторий платформы продажи VPN-подписок. Внутри есть backend на ASP.NET Core/.NET 9, три frontend-приложения на React/Vite, Telegram bot, background workers, платежные адаптеры, интеграция с 3x-ui/x-ui и provisioning через Ansible.

## Состав проекта

- `backend/src/VpnPlatform.Domain` - доменные сущности и статусы.
- `backend/src/VpnPlatform.Application` - бизнес-сервисы и сценарии.
- `backend/src/VpnPlatform.Infrastructure` - EF Core, PostgreSQL/SQLite, платежи, 3x-ui, JWT, секреты, hosted workers.
- `backend/src/VpnPlatform.Api` - HTTP API, auth, публичные endpoints, личный кабинет, админка, webhooks и фоновые задачи.
- `backend/src/VpnPlatform.TelegramBot` - Telegram bot process.
- `frontend/apps/public-web` - публичный сайт с тарифами и покупкой.
- `frontend/apps/cabinet` - личный кабинет пользователя.
- `frontend/apps/admin-panel` - административная панель.
- `frontend/packages/api-client` и `frontend/packages/ui` - общие frontend-пакеты.
- `infra/` - Ansible, Prometheus, Grafana, Loki.
- `scripts/` - локальные скрипты запуска и проверки.
- `docs/` - русская документация по архитектуре, запуску, оплатам, Telegram, provisioning и проверкам.

## Быстрый запуск без Docker

Этот режим предназначен для локальной проверки на Windows/PowerShell. Он использует SQLite-файл `data/vpnplatform-local.db`, поэтому PostgreSQL, Redis, RabbitMQ и Docker не нужны.

Требования:

- .NET SDK 9.
- Node.js 22+ и npm.
- PowerShell.

Первый запуск:

```powershell
cd C:\Users\User16\Desktop\vpn-platform
dotnet restore backend\VpnPlatform.sln
cd frontend
npm install
cd ..
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1
```

После запуска доступны:

- API и Swagger: `http://127.0.0.1:8080/swagger`
- Публичный сайт: `http://127.0.0.1:5173`
- Личный кабинет: `http://127.0.0.1:5174`
- Админка: `http://127.0.0.1:5175`
- Локальный администратор: `admin@local.test` / `LocalAdminPassword123!`

Если локальная SQLite-база уже существовала и пароль администратора был изменен, его можно восстановить без запуска HTTP-сервера:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Local"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj -- admin-bootstrap
```

Команда использует секцию `AdminBootstrap`, создает администратора при отсутствии или сбрасывает пароль существующего администратора только для явного CLI-запуска. Пароль в вывод команды не печатается.

Остановка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
```

Логи локального запуска пишутся в `data/logs/`. SQLite-база и логи игнорируются git через `data/`.

## Ручной запуск без Docker

Backend:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Local"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --urls http://127.0.0.1:8080
```

Frontend в отдельных терминалах:

```powershell
cd frontend
$env:VITE_API_BASE_URL="http://127.0.0.1:8080"
npm run dev:public
npm run dev:cabinet
npm run dev:admin
```

## Запуск через Docker

Docker-режим нужен для проверки PostgreSQL/Redis/RabbitMQ и production-like compose-сборки:

```powershell
docker compose up -d postgres redis rabbitmq prometheus grafana loki
docker compose up --build backend-api public-web cabinet admin-panel
```

Если Docker Desktop не запущен, compose-команды завершатся ошибкой подключения к `dockerDesktopLinuxEngine`.

## VPS/staging без Docker

Текущий staging на VPS запускается без Docker: backend опубликован как self-contained Linux x64 приложение, frontend отдаётся nginx, база данных - PostgreSQL 16.

Основные пути на сервере:

- API: `/opt/vpn-platform/api`
- frontend: `/opt/vpn-platform/web/public`, `/opt/vpn-platform/web/cabinet`, `/opt/vpn-platform/web/admin`
- env-файл API: `/etc/vpn-platform/api.env`
- systemd service: `vpn-platform-api`
- PostgreSQL database: `vpnplatform`, пользователь `vpnplatform`, подключение только через `127.0.0.1:5432`

Проверка на сервере:

```bash
systemctl status vpn-platform-api --no-pager
systemctl status postgresql --no-pager
curl -fsS http://127.0.0.1:8080/health/live
curl -fsS http://127.0.0.1:8080/api/public/tariffs
curl -fsS http://127.0.0.1:8080/api/public/payments/providers
```

Публичные адреса staging:

- `http://<staging-host>` или `http://<staging-host>:5173` - публичный сайт.
- `http://<staging-host>:5174` - личный кабинет.
- `http://<staging-host>:5175` - админка.
- `http://<staging-host>:8080/swagger` - Swagger API.

## Проверки

Backend:

```powershell
dotnet restore backend\VpnPlatform.sln
dotnet build backend\VpnPlatform.sln --no-restore
dotnet test backend\VpnPlatform.sln --no-restore
dotnet tool restore
dotnet ef migrations list --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --no-connect
```

Frontend:

```powershell
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=moderate
```

Provisioning runner:

```powershell
python -m unittest discover -s infra\ansible\runner\tests -v
```

## Что делает платформа

Пользователь выбирает тариф, регистрируется, создаёт заказ, выбирает платёжный провайдер и получает VPN-доступ после успешной оплаты. Backend создаёт подписку, выбирает VPN-узел, формирует доступ, отдаёт URI/QR и ведёт историю платежей, webhooks, outbox/inbox и audit-событий.

Администратор управляет пользователями, тарифами, заказами, платежами, возвратами, support-диалогами, VPN-серверами, 3x-ui панелями, inbound-контуром, клиентами, provisioning runs и настройками Telegram bot.

## Важные режимы

- `ASPNETCORE_ENVIRONMENT=Local` - локальный режим без Docker, SQLite, demo admin, sandbox payments/VPN.
- `ASPNETCORE_ENVIRONMENT=Development` - режим разработки с PostgreSQL из локальной инфраструктуры.
- `ASPNETCORE_ENVIRONMENT=Production` - строгий режим: PostgreSQL обязателен, SQLite, sandbox-платежи, demo seed и небезопасные placeholders запрещены startup validator-ом.

## Текущий статус

Локальный запуск без Docker проверен: API, публичный сайт, кабинет и админка отвечают HTTP 200. Frontend проходит typecheck/build/tests, npm audit сейчас без moderate+ уязвимостей. Backend на .NET 9 собирается без предупреждений, backend unit suite проходит 180/180. Staging на VPS переведён на PostgreSQL 16, API и фронтенды отвечают HTTP 200.
