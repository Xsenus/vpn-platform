# Результаты проверок

Дата проверки: 2026-05-25.

## Проверка 2026-06-11: управление заказами в админке

Что проверено:

- Backend `/api/admin/orders` принимает фильтры `status/search`, сохраняет SQLite-safe сортировку и возвращает последний платеж заказа.
- Backend `/api/admin/orders/{id}/recheck-payment` проверяет последнюю платежную попытку заказа через общий payment orchestrator.
- Раздел админки «Заказы» получил фильтр статуса, поиск, расширенные карточки, переходы к пользователю/платежу/подписке и кнопку «Проверить оплату».
- Исправлен frontend-контракт `recheckAdminPayment`: теперь он типизирован как `PaymentStatusResult`, а не как платежная попытка.
- Добавлена запись «Что нового» `2026-06-11-admin-order-management`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminOrderManagementTests|AdminAuthorizationPolicyTests"
cd frontend
npm test -- --test-name-pattern "admin order"
```

Результат:

- Backend narrow tests: 25/25 пройдено.
- Backend full suite: 289/289 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 58/58 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/orders?status=PendingPayment&search=smoke`, `/api/admin/orders/{missingId}/recheck-payment`; recheck несуществующего заказа корректно вернул HTTP 400.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление подписками в админке

Что проверено:

- Backend умеет активировать подписку, снимать блокировку/отмену и включать текущий VPN-доступ через `VpnAccessLifecycleService`.
- Backend умеет синхронизировать текущий VPN-доступ подписки через endpoint `/api/admin/subscriptions/{id}/sync-access`.
- Раздел админки «Подписки» получил действия: активировать, продлить, синхронизировать доступ, заблокировать/разблокировать и отменить.
- Новые действия подписки покрыты authorization policy: синхронизация доступа требует `VpnManage`.
- Добавлена запись «Что нового» `2026-06-11-admin-subscription-management`.

Команды и результат:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend tests: 283/283 пройдено.
- Frontend typecheck: пройден.
- Frontend tests: 57/57 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/subscriptions`, `/api/admin/subscriptions/{missingId}/activate`, `/api/admin/subscriptions/{missingId}/sync-access` прошли; новые маршруты корректно вернули 404 для отсутствующей подписки.

## Проверка 2026-06-11: карточка пользователя в админке

Что проверено:

- Backend user overview для админки возвращает безопасный профиль пользователя, Telegram-аккаунты, заказы, платежи, подписки, VPN-доступы и обращения поддержки без `PasswordHash` и приватных metadata.
- Раздел админки «Пользователи» показывает структурированную карточку: профиль, быстрые метрики, причины внимания оператора, подписки, заказы, платежи, VPN-доступы, Telegram и поддержку.
- Локальный запуск API на временной SQLite-БД работает без Docker; DataProtection-ключи направлены в рабочую папку проекта, чтобы не зависеть от прав к Windows-профилю.
- Проверка кодировки: символов `U+FFFD` в README/docs/backend/frontend/.env.example не найдено.

Команды и результат:

```powershell
dotnet build backend\VpnPlatform.sln --configuration Release --no-restore
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
git diff --check
rg -n "<символ U+FFFD>" README.md docs backend\src frontend\apps frontend\packages .env.example
```

Результат:

- Backend build: 0 ошибок, 0 предупреждений.
- Backend tests: 280/280 пройдено.
- Frontend typecheck: пройден.
- Frontend tests: 57/57 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/users?search=admin&status=Active`, `/api/admin/users/{id}/overview` прошли успешно.
- `git diff --check`: замечаний нет.
- Поиск символа `U+FFFD`: совпадений нет.

## Что исправлено

- Backend переведен на `.NET 9` (`net9.0`), `global.json` переключен на SDK 9, `dotnet-ef` обновлен до 9.0.16.
- EF Core, ASP.NET Core, Microsoft.Extensions и Npgsql EF Core обновлены до последних patch-версий ветки 9.x.
- Исправлены оставшиеся падения backend suite: Telegram payment flow, sandbox E2E, сериализация EF-графов, sync-события 3x-ui и проверка provisioning precheck.
- JSON payload для Telegram-уведомлений теперь сохраняет русский текст читаемо, без `\uXXXX`.
- Отдельный worker-проект удалён: operational hosted workers запускаются внутри `VpnPlatform.Api`.
- Исправлена совместимость lifecycle/outbox workers с SQLite в Local-режиме.
- Локальный no-Docker запуск продолжает работать через SQLite.
- Staging на VPS переведён с временного SQLite на PostgreSQL 16.
- Исправлено падение кабинета `TypeError: e.toLowerCase is not a function`: общий `StatusBadge` теперь безопасно обрабатывает enum/number/null значения.
- Добавлена no-op EF migration `AlignEfModelSnapshotNet9`, которая синхронизирует snapshot модели с EF Core 9 без DDL-изменений.
- Local sandbox режим платежей расширен на `ASPNETCORE_ENVIRONMENT=Local` для YooKassa/YooMoney/RoboKassa/CloudPayments/TBank/Prodamus/Stripe/PayPal.

## Backend

```powershell
dotnet restore backend\VpnPlatform.sln
dotnet build backend\VpnPlatform.sln --no-restore
dotnet test backend\VpnPlatform.sln --no-build --logger "trx;LogFileName=backend-tests-all-payments-local-sandbox.trx" --verbosity quiet
dotnet ef migrations has-pending-model-changes --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --context ApplicationDbContext
dotnet list backend\VpnPlatform.sln package --outdated --highest-patch
dotnet list backend\VpnPlatform.sln package --vulnerable --include-transitive
dotnet ef migrations list --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --no-connect
```

Результат:

- Build: 0 ошибок, 0 предупреждений.
- Tests: 180/180 пройдено.
- TRX: `backend/tests/VpnPlatform.UnitTests/TestResults/backend-tests-all-payments-local-sandbox.trx`.
- EF pending model changes: отсутствуют.
- Patch-обновлений внутри текущих major/minor веток нет.
- Уязвимых NuGet-пакетов не найдено.
- EF tooling на `dotnet-ef` 9.0.16 успешно видит миграции проекта.

## Local smoke без Docker

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 8088 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
```

Проверено:

- `http://127.0.0.1:8088/health/live` -> HTTP 200.
- `http://127.0.0.1:5183` -> HTTP 200.
- `http://127.0.0.1:5184` -> HTTP 200.
- `http://127.0.0.1:5185` -> HTTP 200.
- `scripts/stop-local.ps1` корректно остановил API, npm и дочерние Vite-процессы.

## Frontend

Ранее в рамках локальной стабилизации проверено:

```powershell
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=moderate
```

Результат: typecheck/build/test успешны, frontend tests 27/27, audit без moderate+ уязвимостей.

## VPS staging

Проверено после деплоя на staging VPS:

- PostgreSQL 16 установлен и запущен, database `vpnplatform`, пользователь `vpnplatform`, порт `5432` слушает только `127.0.0.1`.
- API запущен через `vpn-platform-api.service`, `Database__Provider=Postgres`.
- EF migrations применены, demo seed и admin bootstrap выполнены.
- `http://<staging-host>:8080/health/live` -> HTTP 200.
- `http://<staging-host>` -> HTTP 200.
- `http://<staging-host>:5173` -> HTTP 200.
- `http://<staging-host>:5174` -> HTTP 200.
- `http://<staging-host>:5175` -> HTTP 200.
- `http://<staging-host>:8080/api/public/payments/providers` отдаёт 8 sandbox-провайдеров: YooMoney, YooKassa, RoboKassa, CloudPayments, TBankAcquiring, Prodamus, Stripe, PayPal.
- Для всех 8 провайдеров проверен API flow: register -> create order -> payment init.
- Кабинет открыт в браузере, console errors: 0.

## Provisioning runner

```powershell
python -m unittest discover -s infra\ansible\runner\tests -v
```

Результат: 4/4 пройдено.

## Ограничения среды

- Docker Desktop в текущей среде не был запущен, поэтому compose runtime не проверялся.
- `ansible-playbook` не установлен, поэтому Ansible syntax-check не выполнялся.
- Bash-скрипты `.sh` рассчитаны на Linux/WSL; для Windows добавлены PowerShell-скрипты локального запуска.
