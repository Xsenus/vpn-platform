# Проверка EF model drift

Проверка EF model drift защищает `ApplicationDbContext` от ситуации, когда код модели изменился, а миграция или `ApplicationDbContextModelSnapshot` не обновлены.

## Что проверяется

- `EfModelDriftTests` строит текущую модель на PostgreSQL-провайдере и сравнивает ее с последним snapshot через EF migrations differ.
- `scripts/check-ef-drift.ps1` выполняет ту же проверку из PowerShell на Windows.
- `./scripts/check-ef-drift.sh` выполняет ту же проверку в Linux/GitHub Actions.
- Скрипты отключают live-интеграции, demo seed, admin bootstrap и auto migrations, чтобы drift-check не трогал внешние сервисы и реальные данные.
- Если EF сообщает о pending changes, скрипты создают временную диагностическую миграцию `__ModelDriftCheck`, проверяют, что она пустая, и очищают сгенерированные файлы.

## Как проверить на Windows

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release --filter EfModelDriftTests
powershell -ExecutionPolicy Bypass -File scripts\check-ef-drift.ps1
```

## Как проверить в Linux/GitHub Actions

```bash
dotnet test backend/VpnPlatform.sln --configuration Release --filter EfModelDriftTests
./scripts/check-ef-drift.sh
```

Проверки не подключаются к реальному Postgres и не требуют поднятой БД. Строка подключения нужна EF tooling только для построения PostgreSQL-модели.
