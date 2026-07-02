# Этап 1: стабилизация

Дата: 2026-04-29

## Цель

Сделать текущий skeleton безопаснее для дальнейшей production-разработки: убрать false-positive production behavior, разделить sandbox/dev flow и production flow, подготовить контролируемые миграции и убрать критичные hardcoded bootstrap-секреты.

## Изменено

### Backend

- Добавлен `User.RolesCsv` и helper `UserRoles`, чтобы роли не зависели от hardcoded email.
- Удален hardcoded bootstrap dev-админа.
- Добавлен `AdminBootstrap` через конфигурацию, выключенный по умолчанию.
- `DbInitializer` больше не использует schema auto-create; миграции запускаются только при `Database:ApplyMigrationsOnStartup=true`.
- Добавлена первая EF migration `20260429000100_InitialCreate` и `ApplicationDbContextFactory` для `dotnet ef`.
- Operational workers теперь запускаются внутри `VpnPlatform.Api`; отдельный worker-процесс удалён.
- Добавлен startup safety validator:
  - проверяет JWT signing key;
  - запрещает wildcard/empty CORS в Production;
  - запрещает auto-migrations и demo seed в Production;
  - запрещает sandbox payment/VPN modes в Production;
  - проверяет temporary admin bootstrap credentials.
- CORS переведен на allow-list.
- Swagger выключен по умолчанию и включается только в Development или через `Swagger:Enabled=true`.
- Exception middleware перестал отдавать `ex.Message` клиенту в Production.
- Payment adapters и X3-UI provider явно работают только в sandbox mode; в production они не могут случайно выдать fake payment/fake VPN access.
- False-success endpoints для channel webhooks, auth refresh/logout/password reset, payment recheck/refund переведены в явные `501 Not Implemented`.
- Provisioning executor редактирует известные секреты из stdout/stderr и удаляет временный `extra-vars.json` после запуска runner-а.

### Infra / scripts

- Обновлен `.env.example` с корректными ASP.NET env keys: `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`, `Payments__...`, `Cors__...`.
- Добавлен `.config/dotnet-tools.json` для `dotnet-ef`.
- Добавлены scripts:
  - `scripts/backup-db.sh`
  - `scripts/apply-migrations.sh`
- `scripts/validate_repo.sh` теперь условно запускает backend checks, если доступен .NET SDK, и не падает там, где отсутствует `ansible-playbook`.
- `scripts/validate_repo.sh` removes its temporary Ansible inventory directory with a trap even when syntax-check fails.

### Frontend

- Admin panel больше не содержит prefilled dev credentials.

## Как проверить Phase 1

### Полная проверка в CI/dev окружении

```bash
./scripts/validate_repo.sh
```

### Backend вручную

```bash
cd backend
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

### Controlled migration with backup

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<password>'
export DATABASE_URL='postgres://vpnplatform:<password>@localhost:5432/vpnplatform'
./scripts/apply-migrations.sh
```

### Local docker-compose bootstrap

1. Скопировать `.env.example` в `.env`.
2. Для локального админа включить:

```env
AdminBootstrap__Enabled=true
AdminBootstrap__Email=owner@example.test
AdminBootstrap__Password=<strong-temporary-password>
AdminBootstrap__RolesCsv=SuperAdmin
```

3. Запустить:

```bash
docker compose --env-file .env up -d postgres redis rabbitmq
cd backend
dotnet run --project src/VpnPlatform.Api
```

## Что сознательно не реализовано в Phase 1

- production payment adapters;
- production IX3UiClient;
- refresh token/session store;
- password reset flow;
- Telegram Bot worker;
- full RBAC permissions matrix;
- secret vault/encryption at rest.

Эти пункты вынесены в следующие фазы и теперь не маскируются под работающий production flow.
