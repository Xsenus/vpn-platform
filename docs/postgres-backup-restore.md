# Backup и restore PostgreSQL на VPS

Этот runbook описывает безопасный backup/restore базы `vpnplatform` перед миграциями, деплоем и ручными операциями на VPS.

## Что уже настроено

- `scripts/backup-db.sh` и `scripts/backup-db.ps1` создают custom dump через `pg_dump --format=custom`.
- `scripts/restore-db.sh` и `scripts/restore-db.ps1` восстанавливают dump через `pg_restore`.
- Backup создается без владельцев и прав: `--no-owner --no-privileges`.
- Restore требует отдельную переменную `RESTORE_DATABASE_URL`, чтобы не восстановить dump поверх production случайно.
- Если `RESTORE_DATABASE_URL` совпадает с `DATABASE_URL`, restore останавливается. Override возможен только через `RESTORE_ALLOW_DATABASE_URL_MATCH=true`.
- Для каждого dump создается `.dump.list` через `pg_restore --list`, если `pg_restore` установлен.
- Старые dump-файлы удаляются по `BACKUP_RETENTION_DAYS`, по умолчанию 14 дней.

## Backup на Linux/VPS

```bash
export DATABASE_URL='postgres://vpnplatform:<password>@127.0.0.1:5432/vpnplatform'
export BACKUP_DIR='/var/backups/vpn-platform/db'
export BACKUP_RETENTION_DAYS=14

./scripts/backup-db.sh
```

Скрипт печатает путь к созданному `.dump` файлу. Сохраните этот путь в журнале деплоя.

## Backup на Windows/PowerShell

```powershell
$env:DATABASE_URL = 'postgres://vpnplatform:<password>@127.0.0.1:5432/vpnplatform'
$env:BACKUP_DIR = '.\backups\db'
$env:BACKUP_RETENTION_DAYS = '14'

powershell -ExecutionPolicy Bypass -File scripts\backup-db.ps1
```

## Test restore в отдельную БД

Перед тем как считать backup пригодным, восстановите его в отдельную тестовую БД. Не используйте production database name.

```bash
export BACKUP_FILE='/var/backups/vpn-platform/db/vpnplatform-20260612T103000Z.dump'
export RESTORE_DATABASE_URL='postgres://vpnplatform:<password>@127.0.0.1:5432/vpnplatform_restore_check'

createdb vpnplatform_restore_check
./scripts/restore-db.sh
psql "$RESTORE_DATABASE_URL" -c 'select count(*) from "Users";'
dropdb vpnplatform_restore_check
```

PowerShell-вариант:

```powershell
$env:BACKUP_FILE = '.\backups\db\vpnplatform-20260612T103000Z.dump'
$env:RESTORE_DATABASE_URL = 'postgres://vpnplatform:<password>@127.0.0.1:5432/vpnplatform_restore_check'

createdb vpnplatform_restore_check
powershell -ExecutionPolicy Bypass -File scripts\restore-db.ps1
psql $env:RESTORE_DATABASE_URL -c 'select count(*) from "Users";'
dropdb vpnplatform_restore_check
```

## Restore production

Production restore выполняется только после остановки приложения и явного подтверждения владельцем проекта.

1. Остановить API и воркеры.
2. Создать новый backup текущего состояния.
3. Проверить, что выбран правильный dump.
4. Восстановить в отдельную БД и проверить smoke-запросы.
5. Переключить приложение на восстановленную БД или выполнить restore поверх production только после отдельного подтверждения.
6. Запустить API.
7. Проверить `/health/ready`, `/metrics`, вход администратора и ключевые admin-разделы.

Если restore выполняется поверх той же строки подключения, нужно явно указать:

```bash
export RESTORE_ALLOW_DATABASE_URL_MATCH=true
```

Без этого скрипт остановится.

## Pre-migration backup

`scripts/apply-migrations.sh` уже вызывает `scripts/backup-db.sh` перед `dotnet ef database update`.

```bash
export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<password>'
export DATABASE_URL='postgres://vpnplatform:<password>@127.0.0.1:5432/vpnplatform'
./scripts/apply-migrations.sh
```

Перед production-миграцией отдельно выполните test restore созданного dump в `vpnplatform_restore_check`.
