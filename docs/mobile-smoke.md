# Mobile smoke public/cabinet/admin

Документ закрывает roadmap-пункт `P11-ACC-003` и описывает мобильную приемку трех frontend-приложений.

## Что проверяется

Команда `npm run e2e:mobile --prefix frontend` запускает три Playwright-проекта:

- `mobile-public` - публичный сайт на viewport Pixel 5;
- `mobile-cabinet` - личный кабинет на viewport Pixel 5;
- `mobile-admin` - админка на viewport Pixel 5.

Каждый проект использует существующий E2E-сценарий своего приложения, но запускает его в мобильном viewport. Проверяются:

- загрузка приложения;
- навигация по основным разделам;
- ключевые формы и кнопки;
- mocked API flow без live-платежей и реального 3x-ui;
- отсутствие `console.error`;
- отсутствие `pageerror`;
- сохранение PNG-скриншота в `frontend/test-results`.

## Запуск

```powershell
npm run e2e:mobile --prefix frontend
```

После успешного запуска Playwright сохраняет скриншоты:

- `public-mobile.png`;
- `cabinet-mobile.png`;
- `admin-mobile.png`.

Фактический путь содержит имя теста и проекта, например:

```text
frontend/test-results/<test-name>-mobile-public/public-mobile.png
frontend/test-results/<test-name>-mobile-cabinet/cabinet-mobile.png
frontend/test-results/<test-name>-mobile-admin/admin-mobile.png
```

## Результат проверки 2026-06-14

Команда `npm run e2e:mobile --prefix frontend` прошла `3/3`.

Проверенные экраны:

- public: FAQ/search после перехода из checkout flow;
- cabinet: основной кабинет с подпиской, VPN-доступом, платежами и поддержкой;
- admin: раздел "Что нового" с навигацией, формой создания релиза и историей.

Найденный остаточный UX-риск: на мобильном viewport часть интерфейсов остается плотной, особенно кабинет и админка. Это не ломает сценарий и не вызывает console/page errors, но финальную ручную UX-полировку стоит продолжить перед production-ready решением.

## Что считать успешным результатом

Проверка считается успешной, если:

- все три mobile-проекта Playwright зеленые;
- в `frontend/test-results` появились PNG-скриншоты;
- `consoleErrors` в каждом E2E равен пустому массиву;
- ключевые пользовательские действия доступны на 393px viewport;
- HTML-report создан в `frontend/playwright-report/e2e`.
