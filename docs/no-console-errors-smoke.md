# No console errors smoke

Документ закрывает roadmap-пункт `P11-ACC-004` и описывает проверку основных экранов в браузере на отсутствие console/page errors.

## Что проверяется

Команда `npm run e2e:console --prefix frontend` запускает все основные Playwright-проекты:

- `public-web`;
- `cabinet`;
- `admin-panel`;
- `mobile-public`;
- `mobile-cabinet`;
- `mobile-admin`.

Каждый E2E-сценарий подписывается на:

```ts
page.on('console', ...)
page.on('pageerror', ...)
```

Тест падает, если браузер выбрасывает `console.error` или необработанный `pageerror`.

## Запуск

```powershell
npm run e2e:console --prefix frontend
```

Команда поднимает локальные Vite-приложения через `frontend/scripts/playwright-webservers.mjs`, мокирует API внутри Playwright и не использует live-платежи, Telegram, VPS или настоящий 3x-ui.

## Результат проверки 2026-06-14

Команда `npm run e2e:console --prefix frontend` прошла `6/6`.

Проверенные поверхности:

- public desktop;
- cabinet desktop;
- admin desktop;
- public mobile;
- cabinet mobile;
- admin mobile.

Результат browser console report: `console.error=0`, `pageerror=0`.

## Артефакты

- HTML-report: `frontend/playwright-report/e2e`;
- mobile screenshots: `frontend/test-results/**/public-mobile.png`, `frontend/test-results/**/cabinet-mobile.png`, `frontend/test-results/**/admin-mobile.png`;
- trace retain-on-failure включен в `frontend/playwright.config.ts`.

## Ограничения

Это не live smoke production и не проверка настоящих внешних провайдеров. Проверка фиксирует, что основные frontend-сценарии не ломают браузерную консоль на desktop/mobile при управляемых API-ответах.
