# Playwright E2E: личный кабинет

Этот набор закрывает roadmap-пункт `P9-TST-004` и проверяет основной пользовательский путь в кабинете без живого backend и без реальных платежей:

- регистрация пользователя;
- повторный вход после выхода;
- загрузка активной подписки;
- показ VPN-ключа и QR-кода;
- история заказов, платежей и выданных доступов;
- продление подписки через готовый web-провайдер;
- создание обращения в поддержку и отправка ответа;
- отсутствие ошибок в консоли браузера.

## Запуск

```powershell
cd frontend
npm run e2e:cabinet
```

Playwright поднимает три Vite-приложения через `frontend/scripts/playwright-webservers.mjs`:

- public web: `http://127.0.0.1:5293`;
- cabinet: `http://127.0.0.1:5294`;
- admin-panel: `http://127.0.0.1:5295`.

Оба приложения получают `VITE_API_BASE_URL=http://127.0.0.1:19080`, но сетевые запросы `/api/**` мокируются внутри теста. Поэтому сценарий безопасен для локального запуска и CI: он не обращается к реальной БД, платежным провайдерам, Telegram или VPN-серверам.

## Первый запуск на новой машине

Если браузер Playwright ещё не установлен:

```powershell
cd frontend
npx playwright install chromium
```

## Что считается зелёным

- `npm run e2e:cabinet` проходит в проекте `cabinet`;
- регистрация, logout и повторный login проходят через UI;
- продление создаёт заказ с `type: Renewal`, `channel: Web`, `paymentProvider: YooKassa` и `subscriptionId`;
- поддержка создаёт обращение и добавляет ответ;
- HTML-report создаётся в `frontend/playwright-report/e2e`;
- `.github/workflows/ci.yml` и `.github/workflows/staging-validation.yml` запускают `npm run e2e:cabinet`;
- release seed содержит `2026-06-13-playwright-cabinet-e2e`.
