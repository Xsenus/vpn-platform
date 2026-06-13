# Frontend validation gate

Этот gate закрывает roadmap-пункт `P9-TST-002` и фиксирует обязательный набор проверок для всех frontend-приложений:

- публичный сайт `frontend/apps/public-web`;
- личный кабинет `frontend/apps/cabinet`;
- админка `frontend/apps/admin-panel`;
- общий API-клиент `frontend/packages/api-client`;
- общие UI-компоненты `frontend/packages/ui`.

## Быстрый запуск

Windows / PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/validate-frontend.ps1
```

Linux / Git Bash:

```bash
./scripts/validate-frontend.sh
```

## Что проверяется

1. `node --version`, `npm --version`, `npm config get registry` - окружение и npm registry.
2. Безопасность `frontend/package-lock.json` и `frontend/.npmrc` - без внутренних registry и auth token.
3. `npm ci` - чистая установка зависимостей из lockfile.
4. `npm run typecheck` - TypeScript-проверка public web, cabinet и admin panel.
5. `npm run build` - production-сборка всех трех приложений.
6. `npm run test` - frontend unit tests.
7. `npm audit --audit-level=high` - блокировка high/critical уязвимостей.

На 2026-06-13 frontend unit suite проходит `64/64`.

## Локальный режим

Для ускоренной перепроверки после уже выполненного `npm ci` можно пропустить установку:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/validate-frontend.ps1 -SkipInstall
```

Audit можно пропустить только при диагностике локального npm registry:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/validate-frontend.ps1 -SkipAudit
```

Перед коммитом для roadmap-задач gate должен проходить полностью без `-SkipInstall` и `-SkipAudit`.

## Критерий готовности

`P9-TST-002` считается зеленым, если:

- `scripts/validate-frontend.ps1` проходит локально на Windows;
- `scripts/validate-frontend.sh` содержит тот же обязательный набор для Linux/CI;
- `npm test` проходит с текущим счетчиком `64/64`;
- `npm run typecheck` и `npm run build` проходят для всех трех приложений;
- `npm audit --audit-level=high` не возвращает ошибку;
- `TEST_RESULTS.md` содержит актуальный результат.
