# Playwright E2E: public web

Этот набор закрывает roadmap-пункт `P9-TST-003` и проверяет публичный пользовательский путь без live-платежей:

- главная страница загружает управляемый контент и FAQ preview;
- страница тарифов показывает каталог и включенные web-способы оплаты;
- кнопка покупки создает public checkout session;
- пользователь без авторизации попадает на `/account`, где покупка сохранена до входа;
- FAQ открывается отдельной страницей, поиск фильтрует вопросы.

## Запуск

```powershell
cd frontend
npm run e2e:public
```

Playwright сам поднимает `frontend/apps/public-web` на `http://127.0.0.1:5293`.
Публичные API endpoints мокируются внутри теста, поэтому сценарий не ходит во внешние платежные системы и не требует живого backend.

## Первый запуск на новой машине

Если браузер Playwright еще не установлен:

```powershell
cd frontend
npx playwright install chromium
```

## Что считается зеленым

- `npm run e2e:public` проходит в проекте `public-web`;
- в консоли браузера нет ошибок;
- HTML-report создается в `frontend/playwright-report/e2e`;
- `.github/workflows/ci.yml` и `.github/workflows/staging-validation.yml` устанавливают Chromium, запускают `npm run e2e:public` и сохраняют report artifact;
- `TEST_RESULTS.md` содержит актуальный результат;
- release seed содержит `2026-06-13-playwright-public-e2e`.
