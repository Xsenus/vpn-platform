# Product/UI roadmap: сайт, кабинет и админка VPN Platform

Дата актуализации: 2026-06-14.

Этот документ был исходным продуктовым планом по единому сайту, кабинету и админке. Актуальный источник правды по production-ready статусу находится в [PRODUCT_COMPLETION_ROADMAP.md](PRODUCT_COMPLETION_ROADMAP.md). Здесь оставлен компактный продуктовый срез, чтобы старые незакрытые чекбоксы не противоречили фактическим проверкам.

## Текущий статус

- [x] Публичный сайт, тарифы, FAQ, помощь и CTA приведены к единому пользовательскому пути.
- [x] Личный кабинет покрывает регистрацию, вход, заказы, платежи, подписку, VPN-доступ, QR, продление, Telegram-привязку, поддержку и окно "Что нового".
- [x] Админка разделена на отдельные вкладки: дашборд, пользователи, оплаты, тарифы, подписки, VPN-доступы, серверы, 3x-ui панели, поддержка, аудит, Telegram-бот, "Что нового", FAQ, контент сайта, сценарии и подготовка VPS.
- [x] Тарифы, FAQ, контент сайта, сценарии работы и релизы "Что нового" управляются из админки без правки кода.
- [x] Платежные провайдеры имеют provider-specific формы, readiness matrix, write-only секреты, проверку подключения и sandbox seed.
- [x] VPN-серверы, 3x-ui панели, inbound-ы и клиенты управляются из админки; sandbox-выдача VPN работает.
- [x] Общая UI-система, состояния loading/empty/error, мобильные viewport, доступность и русская локализация покрыты тестами и smoke-проверками.
- [~] Staging/VPS smoke checklist готов, но реальный заполненный отчет после deploy еще нужен.
- [x] Payment provider smoke report template, generator и validator готовы для всех web-провайдеров, но реальные кабинеты еще нужно пройти.
- [x] Admin VPS smoke report template, generator, validator, sections contract, парный evidence validator и regression harness, явный browser runner, единый preflight+browser smoke wrapper с regression harness, bootstrap+smoke wrapper с regression harness и sanitized report validator, preflight, preflight validator и regression harness, локальная SQLite-проверка runner, локальная CLI bootstrap smoke-проверка и strict acceptance evidence gate готовы для проверки всех разделов админки, но реальный VPS admin smoke еще нужно пройти.
- [x] VPN live smoke report template, generator и validator готовы для 3x-ui/inbound/node проверки, но реальную 3x-ui выдачу еще нужно пройти.
- [x] Production readiness gate требует полный пакет evidence reports: staging/VPS, платежи, админка VPS и live VPN/3x-ui.
- [ ] Live-платежи всех провайдеров не подтверждены реальными кабинетами.
- [ ] Реальная production-like выдача через 3x-ui/inbound/node не подтверждена.
- [ ] Админка на VPS не проверена под рабочим production admin-аккаунтом.
- [ ] Production-ready решение не принято: текущий статус `staging-ready baseline`.

## Что уже реализовано продуктово

### Публичный сайт

- [x] Главная страница с понятным оффером VPN-сервиса.
- [x] Страница тарифов получает цены, описания, features, badges и видимость из API.
- [x] FAQ хранится в базе, редактируется в админке и отображается публично.
- [x] Страница помощи описывает покупку, оплату, подключение, продление и поддержку.
- [x] CTA ведут к тарифам, checkout и аккаунту.
- [x] Loading, empty и error состояния не ломают экран.
- [x] Desktop, mobile и console smoke проходят.

### Личный кабинет

- [x] Регистрация, вход, logout, восстановление пароля и refresh/session flow.
- [x] Состояние без подписки ведет к покупке.
- [x] Активная подписка показывает срок, тариф, VPN URI, QR и действия копирования.
- [x] История заказов и платежей показывает paid, pending, failed и refunded состояния без raw provider payload.
- [x] Продление не создает дубли VPN-доступов.
- [x] Окончание подписки отключает доступ через lifecycle flow.
- [x] Поддержка позволяет создать диалог, ответить, закрыть и переоткрыть обращение.
- [x] Telegram-привязка, Telegram-покупка и уведомления реализованы для sandbox/бот-сценариев.
- [x] Окно "Что нового" доступно в кабинете и управляется админкой.

### Админка

- [x] Отдельный современный экран логина.
- [x] Навигация по вкладкам, где каждый раздел открывается отдельно.
- [x] Дашборд readiness показывает платежи, webhook, тарифы, VPN, 3x-ui, Telegram, VPS и CI/CD.
- [x] Управление пользователями, подписками, заказами, платежами и возвратами.
- [x] Управление тарифами: цена, валюта, описание, features, badge, видимость, сортировка.
- [x] Управление платежными аккаунтами: provider-specific поля, readiness, проверка подключения, write-only secrets.
- [x] Управление VPN-серверами, health-check, maintenance/draining, 3x-ui panels, inbound-ами и клиентами.
- [x] Управление Telegram-ботом, текстами, webhook/long polling и WebApp URL.
- [x] Управление FAQ, контентом главной, сценариями работы и релизами "Что нового".
- [x] RBAC, audit log, rate limit, security headers и secret scan проверяются тестами.

## Что остается до production

Эти пункты нельзя закрыть локальными unit/E2E тестами. Нужны реальные внешние окружения и безопасные секреты.

- [ ] Создать или восстановить production admin-аккаунт на VPS и пройти вход в админку.
- [ ] Пройти все разделы админки на VPS под рабочим admin-аккаунтом.
- [ ] Подключить реальную 3x-ui панель, inbound и VPN node.
- [ ] Провести production-like order smoke: checkout, payment, webhook, subscription, VPN access.
- [ ] Проверить live/sandbox кабинеты YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe, PayPal.
- [x] Реализовать или подтвердить полноценный Telegram Stars invoice flow на локальном invoice gate; live BotFather smoke остается внешней проверкой.
- [x] Сгенерировать безопасный staging/VPS smoke report draft через `scripts/new-staging-smoke-report.ps1`.
- [x] Подготовить безопасный payment provider smoke report template и validator через `docs/payment-provider-smoke-report.template.json`.
- [x] Сгенерировать безопасный payment provider smoke report draft через `scripts/new-payment-provider-smoke-report.ps1`.
- [x] Подготовить безопасный admin VPS smoke report через `docs/admin-vps-smoke-report.template.json`, `docs/admin-vps-smoke-sections.json`, `scripts/new-admin-vps-smoke-report.ps1`, `scripts/admin-vps-browser-smoke.ps1`, `scripts/admin-vps-bootstrap-smoke.ps1`, `scripts/validate-admin-vps-bootstrap-smoke-report.ps1`, `scripts/validate-admin-vps-smoke-sections-contract.ps1`, `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1`, локальную проверку `scripts/local-admin-vps-browser-smoke.ps1` и локальный CLI bootstrap smoke `scripts/local-admin-vps-bootstrap-smoke.ps1`.
- [x] Подготовить безопасный VPN live smoke report через `docs/vpn-live-smoke-report.template.json` и `scripts/new-vpn-live-smoke-report.ps1`.
- [x] Усилить production readiness gate полным evidence bundle через `scripts/assert-production-readiness.ps1`.
- [ ] Заполнить staging/VPS smoke report без секретов, cookies и приватных headers.

## Проверки, которыми закрыт локальный продуктовый слой

- [x] Backend full suite: `591/591`.
- [x] Frontend unit tests: `66/66`.
- [x] API Release build: OK.
- [x] Frontend typecheck: OK.
- [x] Frontend production build: OK.
- [x] Playwright public/cabinet/admin/all-screens/mobile/console smoke: `9/9`.
- [x] Fresh local SQLite smoke: OK.
- [x] Local SQLite VPS smoke dry-run: OK.
- [x] Encoding guard: OK.
- [x] Secret scan: OK.
- [x] Latest "Что нового": `2026-06-23-admin-vps-readiness-environment-default`, версия `0.275.0`.

## Как вести дальше

1. Для локальных UX/API/тестовых задач обновлять этот документ, master roadmap, `TEST_RESULTS.md` и "Что нового".
2. Для live-задач не ставить `[x]`, пока нет отчета staging/VPS smoke, ID платежей/webhook/subscription/VPN credential и подтверждения без утечки секретов.
3. Если возникает расхождение между этим файлом и master roadmap, главным считается `docs/PRODUCT_COMPLETION_ROADMAP.md`.
