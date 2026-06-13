# Backend validation gate

Backend validation gate - обязательная проверка backend после каждого изменения.

## Что входит

Оба entrypoint выполняют один и тот же обязательный набор:

- validation safety defaults;
- repository secret scan;
- `dotnet restore backend/VpnPlatform.sln`;
- `dotnet build backend/VpnPlatform.sln --configuration Release --no-restore`;
- `dotnet test backend/VpnPlatform.sln --configuration Release --no-build`;
- `dotnet tool restore`;
- `dotnet ef migrations list --no-connect`;
- EF model drift check.

Linux/macOS/Git Bash:

```bash
./scripts/validate-backend.sh
```

Windows PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/validate-backend.ps1
```

Если нужно быстро проверить сам gate script без тяжелого EF drift, можно временно использовать:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/validate-backend.ps1 -SkipEfDrift
```

Для закрытия задач roadmap `-SkipEfDrift` не считается полным доказательством.

## Safe defaults

Gate отключает live side effects:

- `Database__ApplyMigrationsOnStartup=false`;
- `Database__SeedDemoData=false`;
- `AdminBootstrap__Enabled=false`;
- `Provisioning__LiveExecutionEnabled=false`;
- `Provisioning__AllowLiveDeploy=false`;
- `TelegramBot__Enabled=false`;
- все платежные провайдеры в `Disabled`;
- `Vpn__X3Ui__Mode=Sandbox`.

## Текущее состояние

На 2026-06-13 обязательный backend suite проходит `433/433`.

Актуальный результат фиксируется в `TEST_RESULTS.md`. Если количество тестов изменилось, нужно обновить этот документ и roadmap-пункт `P9-TST-001`.

## Доказательство

Минимальный набор для задачи или PR:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
powershell -ExecutionPolicy Bypass -File scripts/check-ef-drift.ps1
```

Полный локальный gate:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/validate-backend.ps1
```

CI использует workflow `validation`, где backend job выполняет restore, build, test, EF migrations list и EF drift.
