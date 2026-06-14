# All screens browser smoke

Документ закрывает локально проверяемую часть `STATE-010`: полный browser smoke основных экранов public site, личного кабинета и админки без live-платежей, Telegram, VPS и реального 3x-ui.

## Что проверяет команда

Команда `npm run e2e:all-screens --prefix frontend` запускает отдельный Playwright project `all-screens` и проверяет:

- public web routes: `/`, `/tariffs`, `/faq`, `/help`, `/account`;
- cabinet auth screen и авторизованный dashboard;
- все admin sections (`all admin sections`): `dashboard`, `users`, `payments`, `tariffs`, `subscriptions`, `vpn`, `nodes`, `panels`, `support`, `audit`, `bot`, `releases`, `faq`, `content`, `scenarios`, `provisioning`;
- отсутствие пустого `body`;
- отсутствие `console.error`;
- отсутствие `pageerror`.

Smoke использует mock API внутри Playwright. Он не подтверждает live-платежи, live 3x-ui и реальный VPS, но ловит сломанные маршруты, белые экраны, JavaScript exceptions и грубые ошибки интеграции UI с API DTO.

## Запуск

```powershell
npm run e2e:all-screens --prefix frontend
```

Полный browser console gate теперь включает этот project:

```powershell
npm run e2e:console --prefix frontend
```

## Что остается вне проверки

- Реальная оплата у провайдеров.
- Реальная выдача через 3x-ui.
- VPS admin login под production admin account.
- Скриншотная ручная UX-проверка после deploy.

Эти пункты остаются в live/staging задачах roadmap и не закрываются mock-based E2E.

## Результат 2026-06-14

- Добавлен `frontend/e2e/all-screens.spec.ts`.
- Добавлен npm-скрипт `e2e:all-screens`.
- `e2e:console` расширен project-ом `all-screens`.
- Добавлен guard `AllScreensBrowserSmokeTests`.
- Добавлена запись "Что нового" `2026-06-14-all-screens-browser-smoke`, версия `0.107.0`.
