# Аудит PostgreSQL schema

Этот runbook закрывает проверку таблиц, индексов, внешних ключей, nullable-полей и миграций для production PostgreSQL.

## Что формируется

- `artifacts/postgres-schema-audit/ef-migrations.txt` - список EF migrations без подключения к базе.
- `artifacts/postgres-schema-audit/postgres-migrations-idempotent.sql` - idempotent SQL-скрипт миграций PostgreSQL.
- `artifacts/postgres-schema-audit/postgres-schema-snapshot.txt` - snapshot таблиц, колонок, nullable-полей, индексов и FK из PostgreSQL, если задан `DATABASE_URL`.
- `artifacts/postgres-schema-audit/audit-metadata.env` - время генерации, короткий git commit, версия .NET и режим аудита.

Файлы в `artifacts/` не коммитятся. Snapshot не читает данные таблиц и не выводит значения секретов: используются только `information_schema` и `pg_indexes`.

## Локальная проверка без PostgreSQL

На Windows:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\audit-postgres-schema.ps1
```

На Linux/VPS/GitHub Actions:

```bash
./scripts/audit-postgres-schema.sh
```

В этом режиме формируются EF migrations list и idempotent migration SQL. `postgres-schema-snapshot.txt` будет содержать явную пометку, что PostgreSQL snapshot пропущен, потому что `DATABASE_URL` не задан.

## Проверка реальной PostgreSQL-БД

На VPS или staging-машине с установленным `psql`:

```bash
export DATABASE_URL='Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=***'
./scripts/audit-postgres-schema.sh
```

На Windows:

```powershell
$env:DATABASE_URL = 'Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=***'
powershell -ExecutionPolicy Bypass -File scripts\audit-postgres-schema.ps1
```

Перед публикацией результата проверьте, что в snapshot нет секретов. Скрипты не выбирают данные из пользовательских таблиц, но сам файл все равно должен храниться как deployment artifact, а не как исходный код.

## Что проверять в результате

- Все ожидаемые таблицы присутствуют после применения миграций.
- Для таблиц заказов, оплат, пользователей, Telegram, VPN и outbox есть PK и нужные индексы.
- Внешние ключи соответствуют доменной модели и не допускают случайного каскадного удаления там, где нужен `Restrict` или `SetNull`.
- Nullable-поля осознанны: необязательные внешние связи, provider metadata и технические поля допускаются, критичные идентификаторы и статусы не должны быть nullable.
- `ef-migrations.txt` совпадает с последним ожидаемым migration chain, а `postgres-migrations-idempotent.sql` генерируется без ошибок.

## Связанные проверки

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter PostgresSchemaAuditTests
powershell -ExecutionPolicy Bypass -File scripts\check-ef-drift.ps1
```

Для полной локальной проверки после аудита:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```
