# Проверочный контур сборки

Проект считается готовым к передаче только после прохождения обязательных проверок backend, frontend и provisioning runner.

## Backend

```powershell
dotnet restore backend\VpnPlatform.sln
dotnet build backend\VpnPlatform.sln --no-restore
dotnet test backend\VpnPlatform.sln --no-restore
```

Дополнительно:

```powershell
dotnet tool restore
dotnet ef migrations list --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --no-connect
```

## Frontend

```powershell
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=moderate
```

## Provisioning

```powershell
python -m unittest discover -s infra\ansible\runner\tests -v
```

Если установлен Ansible:

```powershell
ansible-playbook --syntax-check infra\ansible\playbooks\precheck-node.yml
ansible-playbook --syntax-check infra\ansible\playbooks\provision-node.yml
```

## Локальный smoke без Docker

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1
Invoke-WebRequest http://127.0.0.1:8080/health/live -UseBasicParsing
powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
```

## Что считать блокером

- Backend не собирается.
- API не стартует в `ASPNETCORE_ENVIRONMENT=Local`.
- Frontend не проходит typecheck/build/tests.
- `npm audit --audit-level=moderate` возвращает уязвимости.
- EF tooling не видит миграции.
- Provisioning runner tests падают.

Падающие тесты, которые проверяют старые тексты Telegram или незавершённые sandbox E2E, должны быть отдельно заведены как стабилизационные задачи и не смешиваться с инфраструктурным запуском без Docker.
