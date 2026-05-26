# Аудит этапов 1-2 и заметки по стабилизации

Документ сохранён как историческая сводка аудита. Актуальные инструкции запуска и проверки находятся в `README.md`, `docs/local-validation.md` и `TEST_RESULTS.md`.

## Что проверялось

- Структура backend и frontend.
- Платёжный контур и webhook-обработка.
- Начальная безопасность: секреты, demo-данные, startup validation.
- Provisioning runner и Ansible playbooks.
- Возможность локальной проверки.

## Основные выводы

- Backend построен как modular monolith на ASP.NET Core.
- PostgreSQL остаётся основным production/staging хранилищем.
- Для локального запуска без Docker добавлен SQLite-режим `ASPNETCORE_ENVIRONMENT=Local`.
- Платёжные провайдеры должны быть явно включены и настроены через admin API/UI.
- Production-режим не должен запускаться с sandbox-платежами, demo seed, SQLite или placeholder-секретами.
- Реальный provisioning должен запускаться только при явном включении `Provisioning:LiveExecutionEnabled` и `Provisioning:AllowLiveDeploy`.

## Исторические ограничения старого аудита

В ранней среде проверки не было .NET SDK, Docker и полноценного Node/npm окружения. Сейчас .NET SDK, Node.js и npm доступны, поэтому актуальный статус нужно смотреть в `TEST_RESULTS.md`.

## Актуальные команды

Локальный запуск без Docker:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1
```

Остановка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
```

Проверка backend:

```powershell
dotnet restore backend\VpnPlatform.sln
dotnet build backend\VpnPlatform.sln --no-restore
dotnet test backend\VpnPlatform.sln --no-restore
```

Проверка frontend:

```powershell
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
```
