# Production-выдача VPN-доступа

Документ описывает границу между sandbox-проверкой продаж и реальной выдачей VPN-доступов.

## Что считается sandbox

Sandbox-контур используется для проверки покупки без реальных денег и без реального 3x-ui сервера.

Признаки sandbox-доступа:

- платежный аккаунт работает в режиме `Sandbox`;
- `VpnProvisionRequest.UseSandboxProvisioning = true`;
- создается или используется нода `sandbox-vpn-node`;
- доступ получает идентификатор вида `x3ui-sandbox-*`;
- URI строится на `sandbox-node.local`.

Такая нода нужна только для безопасного smoke-теста. Она не должна обслуживать production-платежи.

## Что считается production

Production-контур используется только для платежей с `PaymentProviderMode.Production`.

Для него нужны реальные сущности:

- активная `3x-ui` панель в разделе админки `3x-ui панели`;
- хотя бы один активный inbound нужного протокола;
- готовый VPN-сервер в разделе `Серверы`;
- сервер не должен быть `sandbox`, `maintenance`, `draining`, `disabled` или `archived`;
- у сервера должен быть открыт набор новых пользователей;
- health status сервера и панели не должен быть `Unhealthy`.

Если в БД есть только `sandbox-vpn-node`, production-выдача должна завершиться ошибкой `No available VPN node for provisioning access`. Это правильное fail-closed поведение.

## Поддерживаемые протоколы

Production-выдача формирует клиентские ссылки для протоколов:

- `vless` - URI вида `vless://...`;
- `vmess` - URI вида `vmess://...` с base64 JSON profile;
- `trojan` - URI вида `trojan://...`.

Сценарий работы выбирает протокол через поле `VpnProtocol`. Для успешной выдачи на выбранной 3x-ui панели должен быть активный inbound с тем же протоколом. Если inbound или его stream settings недостаточны для сборки клиентской ссылки, выдача завершается ошибкой и подписка остается в `PendingActivation`, чтобы администратор мог исправить настройки.

## Порядок включения live-выдачи

1. Добавить реальную 3x-ui панель в админке.
2. Нажать `Проверить подключение`.
3. Запустить синхронизацию inbound-ов.
4. Добавить или отредактировать VPN-сервер и связать его с `PanelBaseUrl`.
5. Указать реальные `SupportedProtocolsCsv`, регион, емкость, public hostname и порт.
6. Убедиться, что сервер в статусе `Ready`, открыт для новых пользователей и не помечен тегом `sandbox`.
7. Проверить сценарий выдачи: протокол, правило выбора сервера и inbound.
8. Только после этого включать платежный аккаунт в режим `Production`.

## Проверки

Локальные обязательные проверки:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release
npm test
npm run typecheck
npm run build
git diff --check
```

Smoke для sandbox должен создавать доступ `x3ui-sandbox-*`.

Smoke для production без реального сервера должен падать с `No available VPN node for provisioning access`, а не создавать фейковый доступ.
