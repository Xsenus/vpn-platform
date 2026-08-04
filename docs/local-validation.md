# Локальная проверка

Документ описывает проверку проекта на рабочей машине. Для быстрой ручной проверки используйте режим без Docker, для production-like проверки используйте Docker/Compose и PostgreSQL.

## Проверка без Docker

Требования:

- .NET SDK 9.
- Node.js 22.22+ и npm.
- PowerShell.

Команды из корня репозитория:

```powershell
dotnet restore backend\VpnPlatform.sln
cd frontend
npm install
cd ..
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1
```

Если один из стандартных портов уже занят, передайте свободные порты явно:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 8081 -PublicPort 5373 -CabinetPort 5374 -AdminPort 5375
```

Скрипт поднимает:

- API: `http://127.0.0.1:8080/swagger`
- публичный сайт: `http://127.0.0.1:5173`
- кабинет: `http://127.0.0.1:5174`
- админку: `http://127.0.0.1:5175`

Локальная база: `data/vpnplatform-local.db`.

Локальный администратор:

```text
admin@local.test
LocalAdminPassword123!
```

Остановка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
```

## Ручная проверка backend

```powershell
dotnet restore backend\VpnPlatform.sln
dotnet build backend\VpnPlatform.sln --no-restore
dotnet test backend\VpnPlatform.sln --no-restore
```

Проверка health endpoint в Local-режиме:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Local"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --urls http://127.0.0.1:8080
Invoke-WebRequest http://127.0.0.1:8080/health/live -UseBasicParsing
```

## Ручная проверка frontend

```powershell
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=moderate
```

## Проверка EF migrations

```powershell
dotnet tool restore
dotnet ef migrations list --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --no-connect
```

PostgreSQL-миграции остаются источником истины для staging/production. SQLite используется только для локального запуска без Docker и создаёт схему через `EnsureCreated`.

## Проверка provisioning runner

```powershell
python -m unittest discover -s infra\ansible\runner\tests -v
```

`ansible-playbook` нужен только для синтаксической проверки playbook-ов и реального provisioning. Локальный режим без Docker его не требует.

## Docker-проверка

```powershell
docker compose config
docker compose up -d postgres redis rabbitmq
docker compose up --build backend-api public-web cabinet admin-panel
```

Если Docker Desktop не запущен, команда завершится ошибкой подключения к Docker engine. Это не влияет на Local-режим с SQLite.
