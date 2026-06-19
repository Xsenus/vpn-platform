# Admin bootstrap/reset

Документ описывает безопасный one-shot запуск для создания или сброса администратора без открытия отдельного maintenance endpoint.

## Когда использовать

- Нужно восстановить доступ к админке на VPS.
- Нужно создать первого `SuperAdmin` после установки на чистую БД.
- Нужно сбросить пароль существующего администратора перед smoke-проверкой `/admin/`.

Команда не запускает HTTP-сервер и не печатает пароль в stdout. Backend-команда `admin-bootstrap` всегда сбрасывает пароль существующего пользователя только при явном CLI-запуске.

Wrapper сам выставляет `AdminBootstrap__Enabled=true`, `Database__Provider`, `Database__ApplyMigrationsOnStartup`, `Database__UseEnsureCreatedForLocalSqlite` и `ConnectionStrings__DefaultConnection`. В режиме `-LocalSqlite` используется `Data Source=data/vpnplatform-local.db`, если строка подключения не передана явно.

## Локальная SQLite-БД

```powershell
powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 `
  -LocalSqlite `
  -EnvironmentName Local `
  -Email admin@local.test `
  -Password "LocalAdminPassword123!" `
  -DisplayName "Local Admin" `
  -RolesCsv SuperAdmin
```

## Production/Postgres

Перед запуском подставьте реальную строку подключения через переменную окружения или параметр `-ConnectionString`.

```powershell
$env:ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<db-password>"

powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 `
  -EnvironmentName Production `
  -Provider Postgres `
  -ApplyMigrations `
  -Email owner@example.com `
  -Password "<temporary-password-at-least-16-chars>" `
  -DisplayName "Owner" `
  -RolesCsv SuperAdmin
```

## Dry-run

Dry-run проверяет параметры и показывает только безопасные значения:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\admin-bootstrap.ps1 `
  -LocalSqlite `
  -EnvironmentName Local `
  -Email admin@local.test `
  -Password "LocalAdminPassword123!" `
  -DryRun
```

Ожидаемый вывод содержит `Password: [hidden]` и `Dry-run mode: database was not changed.`

## Проверка после сброса

1. Запустите API и админку обычным способом.
2. Войдите в `/admin/` под указанным email и новым временным паролем.
3. Проверьте `GET /api/admin/dashboard/summary`.
4. Замените временный пароль на постоянный через штатный операционный процесс.
5. Зафиксируйте результат в `admin-vps-smoke-report.json` без паролей, cookie и raw headers.
