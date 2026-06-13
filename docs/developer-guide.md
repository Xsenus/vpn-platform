# Руководство разработчика

Документ закрывает roadmap-пункт `P10-DOC-004` и описывает, как безопасно развивать VPN Platform: где лежит код, как устроены доменные сущности, какие state machines нельзя обходить, как запускать тесты и как добавлять нового платежного или VPN-провайдера.

## Архитектура

Проект организован как монорепозиторий:

- `backend/src/VpnPlatform.Domain` - доменные сущности, enum-статусы и базовые правила.
- `backend/src/VpnPlatform.Application` - бизнес-сервисы, DTO, orchestration заказов, платежей, подписок, VPN-доступов и provisioning.
- `backend/src/VpnPlatform.Infrastructure` - EF Core, PostgreSQL/SQLite, платежные адаптеры, 3x-ui, Ansible provisioning, секреты и hosted workers.
- `backend/src/VpnPlatform.Api` - ASP.NET Core API, auth, публичные endpoints, кабинет, админка, webhooks, health, metrics и release seed.
- `backend/src/VpnPlatform.TelegramBot` - отдельный процесс Telegram-бота.
- `frontend/apps/public-web` - публичный сайт, тарифы, FAQ, checkout и помощь пользователя.
- `frontend/apps/cabinet` - личный кабинет, подписки, VPN-ключи, платежи, поддержка, Telegram и "Что нового".
- `frontend/apps/admin-panel` - административная панель.
- `frontend/packages/api-client` - typed API client.
- `frontend/packages/ui` - общие UI-компоненты.
- `infra/ansible` - playbooks и runner для подготовки VPS.
- `scripts` - локальный запуск, validation gates, backup/restore, deploy/smoke helpers.

Backend следует зависимостям `Domain -> Application -> Infrastructure/API`. Доменные сущности не должны зависеть от EF, HTTP, Telegram, платежных SDK или frontend.

## Доменные сущности

Основные агрегаты:

- `User` - аккаунт пользователя, роли, статус, Telegram-связи и referral-код.
- `Tariff` - продаваемый тариф, цена, срок, описание, сценарий выдачи.
- `Order` - намерение покупки или продления.
- `PaymentAttempt` - попытка оплаты через конкретного провайдера.
- `PaymentWebhookEvent` - идемпотентный след входящего webhook.
- `Subscription` - право пользователя на VPN-доступ в период времени.
- `AccessCredential` - выданная VPN-ссылка, provider id, статус, сервер и история.
- `VpnNode`, `VpnPanel`, `VpnInbound`, `VpnClient` - инфраструктура VPN и 3x-ui.
- `SupportConversation`, `SupportMessage` - обращения пользователя и переписка.
- `AppRelease`, `AppReleaseItem`, `AppReleaseSeen` - модуль "Что нового".
- `ProvisioningRun`, `ProvisioningStepRun` - подготовка VPS, precheck, deploy и rollback.

Изменение сущностей почти всегда требует:

1. обновить EF-модель и миграции;
2. проверить PostgreSQL и SQLite режимы;
3. обновить DTO/API client;
4. добавить тест на поведение, а не только на структуру;
5. обновить документацию и release entry, если изменение видно пользователю или оператору.

## State Machines

Статусы заказов, платежей, подписок, VPN-доступов и provisioning нельзя менять напрямую без правил из `StatusStateMachine`.

Ключевые переходы:

- `Order`: Draft/Pending -> Paid/Cancelled/Failed/Refunded.
- `PaymentAttempt`: Pending -> Succeeded/Failed/Cancelled/Refunded.
- `Subscription`: Pending/Active/GracePeriod -> Expired/Cancelled/Blocked.
- `AccessCredential`: Pending/Active/Disabled/Expired/Revoked/Error.
- `ProvisioningRun`: Queued/Running -> Succeeded/Failed/Cancelled.

Если нужен новый переход:

1. добавьте правило в state machine;
2. обновите сервис, который инициирует переход;
3. проверьте идемпотентность повторного события;
4. добавьте unit-тест на разрешенный и запрещенный переход;
5. проверьте audit log и пользовательский текст ошибки.

## Платежный поток

Основной flow:

1. пользователь выбирает тариф;
2. создается `Order`;
3. `CheckoutSession` или кабинет вызывает создание `PaymentAttempt`;
4. платежный адаптер возвращает redirect URL;
5. провайдер присылает webhook;
6. `PaymentOrchestrator` валидирует подпись, идемпотентно применяет статус и активирует подписку;
7. `SubscriptionService` и `VpnAccessLifecycleService` создают или продлевают VPN-доступ;
8. результат виден в кабинете и аудите.

Важно:

- webhook должен быть идемпотентным;
- sandbox-header нельзя принимать в production;
- raw provider payload не должен попадать пользователю;
- секреты провайдера должны быть write-only;
- Telegram Stars отделен от web checkout и работает как bot-only/fail-closed сценарий до полноценного invoice flow.

## Как добавить платежного провайдера

1. Добавьте значение в enum `PaymentProvider`.
2. Опишите required fields и capabilities в `PaymentProviderConfigurationRules`.
3. Реализуйте `IPaymentProvider`.
4. Если есть webhook, реализуйте `IPaymentWebhookVerifier` и `IPaymentStatusMapper`.
5. Зарегистрируйте реализацию в DI.
6. Добавьте provider-specific форму в админке.
7. Обновите публичный список способов оплаты: показывать только enabled + ready web-провайдеры.
8. Добавьте sandbox seed без реальных денег и внешних API.
9. Добавьте contract tests: create checkout, signature verification, status mapping, refund/recheck capabilities.
10. Обновите docs/payment-providers.md, docs/admin-guide.md и "Что нового".

Fail-closed правило: если подпись, секрет, API base URL или hosted checkout URL не настроены, provider не должен выглядеть готовым.

## VPN и 3x-ui

VPN-выдача проходит через `VpnAccessLifecycleService`, `NodeAllocationService`, `X3UiPanelService` и provider-адаптеры. Поддерживаемые протоколы: VLESS, VMess и Trojan.

Для реального 3x-ui flow нужны:

- активная `VpnNode`;
- подключенная `VpnPanel`;
- хотя бы один активный `VpnInbound`;
- успешная проверка панели;
- синхронизация клиентов;
- smoke, который подтверждает создание клиента и валидный access URI.

Правило безопасности: если 3x-ui недоступен или inbound не найден, выдача должна завершаться fail-closed, а не создавать фальшивый успешный доступ.

## Provisioning VPS

Provisioning поддерживает precheck, dry-run, validation-deploy, live-deploy-blocked и live-deploy. Live deploy требует явных operator gates:

- `Provisioning__LiveExecutionEnabled=true`;
- `Provisioning__AllowLiveDeploy=true`;
- tag `explicit-live-provisioning:true`;
- `validation-mode:false`;
- корректный `Provisioning__KnownHostsPath`.

Секреты SSH должны передаваться через protected payload и временный файл, который удаляется после выполнения. Password-based live SSH остается fail-closed.

## Frontend

Frontend состоит из трех приложений и двух пакетов. Общие правила:

- API вызывается через `@vpn-platform/api-client`;
- общие элементы берутся из `@vpn-platform/ui`;
- пользовательский текст должен быть на русском;
- страницы должны иметь loading, empty, error и ready состояния;
- секреты не выводятся в UI;
- новые пользовательские сценарии покрываются unit/static tests и при необходимости Playwright.

Для публичного сайта проверяйте путь главная -> тарифы -> checkout -> аккаунт -> помощь/FAQ. Для кабинета проверяйте login/register, подписки, платежи, VPN-ссылку, QR, поддержку и "Что нового". Для админки проверяйте вкладки, формы, write-only секреты и доступность действий без наложений.

## Тесты и validation gates

Минимальный набор перед коммитом:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Дополнительные проверки:

```powershell
npm run e2e:public --prefix frontend
npm run e2e:cabinet --prefix frontend
npm run e2e:admin --prefix frontend
powershell -ExecutionPolicy Bypass -File scripts\validate-backend.ps1
powershell -ExecutionPolicy Bypass -File scripts\validate-frontend.ps1
```

Локальный smoke должен поднимать API на чистой SQLite-БД, проверять `/health/live`, `/health/ready`, bootstrap login и `/api/app-version/latest`.

## Работа с БД

Production-режим использует PostgreSQL. Local-режим использует SQLite.

При изменении модели:

1. добавьте EF migration;
2. проверьте `ApplicationDbContextModelSnapshot`;
3. прогоните EF drift check;
4. проверьте локальный SQLite startup;
5. обновите seed, если новые поля обязательны;
6. не читайте пользовательские данные в audit/schema scripts.

## Секреты и безопасность

Нельзя коммитить реальные токены, пароли, приватные ключи, webhook secrets, Stripe/PayPal/YooKassa/TBank credentials или SSH material.

Правила:

- все секреты проходят через конфигурацию окружения или protected storage;
- UI показывает только masked/write-only состояние;
- logs, audit, support notes и provisioning errors проходят redaction;
- production startup validator блокирует placeholder values;
- CORS/CSP/security headers не ослабляются ради локальной отладки.

## Документация и "Что нового"

Если изменение влияет на пользователя, администратора, deploy, безопасность, платежи, VPN или тестовый gate:

1. обновите соответствующий документ в `docs/`;
2. обновите roadmap;
3. добавьте запись в `backend/src/VpnPlatform.Api/AppReleases/releases.json`;
4. добавьте releaseId в `ReleaseDocumentationGuardTests`, если пункт roadmap закрывается;
5. обновите `TEST_RESULTS.md`;
6. проверьте кодировку UTF-8 и отсутствие `U+FFFD`.

## Быстрый чеклист разработчика

- Код соответствует существующим слоям и не ломает зависимости.
- State machine обновлена вместе с тестами.
- Платежи и webhooks идемпотентны.
- VPN/live provisioning fail-closed при недонастройке.
- Секреты write-only и redacted.
- PostgreSQL и SQLite сценарии учтены.
- Backend и frontend validation gates проходят.
- Документация, roadmap и "Что нового" обновлены.
