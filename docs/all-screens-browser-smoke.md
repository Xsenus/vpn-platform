# All screens browser smoke

Документ закрывает локально проверяемую часть `STATE-010`: полный browser smoke основных экранов public site, личного кабинета и админки без live-платежей, Telegram, VPS и реального 3x-ui.

## Что проверяет команда

Команда `npm run e2e:all-screens --prefix frontend` запускает отдельный Playwright project `all-screens` и проверяет:

- public web routes: `/`, `/tariffs`, `/faq`, `/help`, `/account`;
- cabinet auth screen и авторизованный dashboard;
- все admin sections (`all admin sections`): `dashboard`, `users`, `payments`, `tariffs`, `referrals`, `subscriptions`, `vpn`, `nodes`, `panels`, `support`, `audit`, `bot`, `releases`, `faq`, `content`, `scenarios`, `provisioning`;
- отсутствие пустого `body`;
- отсутствие `console.error`;
- отсутствие `pageerror`;
- landmark, уникальные DOM `id`, `alt` у изображений и доступные имена controls/actions;
- отсутствие document overflow и горизонтально обрезанных interactive controls;
- responsive layout на 18 конфигурациях от `305x568` и `568x320` mobile landscape до `2560x1440`;
- ширины сразу после CSS-breakpoints: `391`, `521`, `641`, `769`, `821`, `901`, `961`, `1025`, `1281` px;
- same-origin URL, browser decode и минимальный размер `1200x800` для ключевых background assets public/admin.

Smoke использует mock API внутри Playwright. Он не подтверждает live-платежи, live 3x-ui и реальный VPS, но ловит сломанные маршруты, белые экраны, JavaScript exceptions и грубые ошибки интеграции UI с API DTO.

## Запуск

```powershell
npm run e2e:all-screens --prefix frontend
```

Полный browser console gate теперь включает этот project:

```powershell
npm run e2e:console --prefix frontend
```

Для локальной ручной приёмки representative desktop/mobile screenshots:

```powershell
$env:VPN_PLATFORM_VISUAL_AUDIT='1'
npm run e2e:all-screens --prefix frontend
Remove-Item Env:VPN_PLATFORM_VISUAL_AUDIT
```

PNG создаются только при явном флаге внутри `frontend/test-results` и после просмотра должны быть удалены. Screenshot helper скрывает skip-link только на изображении, чтобы Playwright full-page stitching не дублировал fixed accessibility control; runtime UI не изменяется.

## Что остается вне проверки

- Реальная оплата у провайдеров.
- Реальная выдача через 3x-ui.
- VPS admin login под production admin account.
- Скриншотная ручная UX-проверка после deploy на реальном контенте и шрифтах.

Эти пункты остаются в live/staging задачах roadmap и не закрываются mock-based E2E.

## Результат 2026-06-14

- Добавлен `frontend/e2e/all-screens.spec.ts`.
- Добавлен npm-скрипт `e2e:all-screens`.
- `e2e:console` расширен project-ом `all-screens`.
- Добавлен guard `AllScreensBrowserSmokeTests`.
- Добавлена запись "Что нового" `2026-06-14-all-screens-browser-smoke`, версия `0.107.0`.

## Результат 2026-08-09

- Responsive matrix расширена с 8 до 18 viewport-конфигураций и покрывает обе стороны всех используемых CSS-breakpoints.
- Встроена проверка local/same-origin decode фоновых WebP и их размеров.
- Встроен `@axe-core/playwright`: WCAG 2.0/2.1/2.2 A/AA и best-practice правила без allow-list проверяют desktop и compact mobile состояния public, cabinet и всех 17 admin sections.
- Representative public, account, cabinet и admin screenshots проверены вручную на desktop/mobile; временные PNG очищены.
- `npm run e2e:all-screens --prefix frontend`: `6/6`; полный `npm run e2e:console --prefix frontend`: `78/78`.
- Latest "Что нового": `2026-08-09-automated-wcag-accessibility-gate`, версия `0.552.0`.

## Результат 2026-08-10

- Критический admin flow выполняет notification retry, payment/order recheck, refund, subscription/VPN mutations и support reply/note/status на desktop/mobile.
- Public account assertion использует точное accessible name, а последовательный axe-аудит 17 admin sections имеет достаточный timeout без исключения правил.
- Управляемая конфигурация admin-панели проходит полный create/edit/delete lifecycle с confirm-dialog, payload assertions и сохранением состояния после reload на desktop/mobile.
- Cabinet Telegram deep-link/unlink и support close/reopen проходят с проверкой authorization, optimistic revision и reload persistence на desktop/mobile.
- Payment provider account проходит secure create/edit/disable/reload/enable/check lifecycle без раскрытия write-only secrets на desktop/mobile.
- Telegram bot settings проходят secure save/check/reload/edit lifecycle без раскрытия write-only bot/webhook tokens на desktop/mobile.
- `npm run e2e:console --prefix frontend`: `86/86`; all-screens: `6/6`.
- Latest "Что нового": `2026-08-10-admin-telegram-bot-settings-secure-lifecycle-e2e`, версия `0.557.0`.
