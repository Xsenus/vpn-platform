# Product/UI roadmap: сайт, кабинет и админка VPN Platform

Дата актуализации: 2026-08-14.

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
- [ ] Production-ready решение не принято: текущий статус `staging-ready baseline`. `P11-ACC-002` remains open until real VPS/staging smoke.
- [x] Roadmap progress синхронизирован с master roadmap: `736/756` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

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
- [x] Capability matrix административной сессии ограничивает разделы, чтение и команды для Finance, Support, Operator и ReadOnly; user overview редактирует finance/support данные по backend policy.
- [x] Dashboard summary и readiness редактируют finance/support/Telegram данные по capabilities; partial-role UI не показывает скрытые tiles, карточки и переходы.
- [x] Audit log применяет finance/support/Telegram scope до Action/EntityType/Search и не раскрывает partial role чужие JSON payload; UI показывает только доступные категории.
- [x] VPN access enable/sync/reset сохраняют cancellation audit/history; enable/reset uncertainty переводит доступ в `SyncRequired` в соответствии с административным UI.

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

- [x] Backend full suite: `1521/1521`.
- [x] Frontend unit tests: `172/172`.
- [x] API Release build: OK.
- [x] Frontend typecheck: OK.
- [x] Frontend production build: OK.
- [x] Playwright public/cabinet/admin/all-screens/mobile/console smoke: `268/268`.
- [x] Responsive matrix: 25 viewport-конфигураций `305x568..2560x1440`, точные пары `N/N+1` для всех CSS-breakpoints, same-origin decode локальных WebP и representative screenshot review.
- [x] Fresh local SQLite smoke: OK.
- [x] Local SQLite VPS smoke dry-run: OK.
- [x] Encoding guard: OK.
- [x] Secret scan: OK.
- [x] Operation boundary regression: malformed enum/JSON returns 400 without partial mutation; all 8 payment webhook routes and fail-closed VPN provisioning are covered.
- [x] Page quality gate covers landmarks, duplicate IDs, image alt text and accessible control names plus axe WCAG A/AA and best-practice checks on public, cabinet and all 17 admin screens at desktop and 320 px.
- [x] Subscription migration and archived-node mode actions are fail-closed and covered by SQLite regression.
- [x] Mobile admin navigation uses the compact section selector; desktop counters and previous/next order match the grouped menu.
- [x] Subscription commands fail closed on VPN provider errors; node deletion preserves health/migration history and reports all linked records in the admin UI.
- [x] Refund actions use a durable reservation, deduplicate parallel retries and expose a fail-closed blocker while provider reconciliation is unfinished.
- [x] Payment init serializes concurrent checkout requests, blocks paid intermediate orders and preserves remote checkout data across transient local commit failures.
- [x] Subscription activation compensates remote access after local persistence failure, preserves `SyncRequired` on cleanup uncertainty and keeps renewal retries idempotent.
- [x] Telegram update ingress reserves `update_id` before side effects and safely retries failed/stale processing without duplicate invoice calls.
- [x] Frontend dependency audit: `0 vulnerabilities` on React 19.2.8, React Router 8.3.0 and Node.js 22.22.0.
- [x] Payment provider account check различает локальную готовность настроек и реальный health внешнего кабинета; configuration-only результат не создает ложный `Healthy`.
- [x] Cabinet renewal action скрыта для `Blocked/Cancelled`; вместо нерабочей команды показано допустимое следующее действие.
- [x] Cabinet QR action доступна только после выдачи VPN URI; provisioning-карточки объясняют ожидание и не вызывают заведомый `400`.
- [x] Cabinet payment retry учитывает `expiresAt`: истёкший заказ не вызывает заведомый backend отказ и ведёт к созданию нового заказа.
- [x] Public access/refresh session ротируется после `401`, а logout отзывает backend refresh session и очищает browser storage при success/failure.
- [x] Cabinet logout очищает токены и все пользовательские/VPN-данные даже при недоступном backend revoke, сохраняя явное предупреждение.
- [x] Admin capability matrix проверена unit и desktop/mobile E2E: partial roles не видят чужие разделы, не отправляют запрещенные запросы и не получают чужие finance/support данные из user overview.
- [x] Support dashboard проверен desktop/mobile E2E: finance tiles/orders/readiness actions отсутствуют, support queue видима, запрещенные API не вызываются, overflow отсутствует.
- [x] Audit domain scope проверен SQLite role matrix и Finance/Support desktop/mobile E2E: чужие actions, entity types, payload и категории отсутствуют.
- [x] VPN access cancellation/reconciliation проверен SQLite `8/8` и расширенным X3Ui/admin/subscription regression `117/117`.
- [x] Отозванный VPN-доступ является терминальным: кабинет и админка скрывают URI/QR, а provider-команды fail-closed до сетевого вызова.
- [x] Access/refresh сессии версионированы; password reset и административные изменения полномочий немедленно закрывают старое поколение, public/cabinet очищают browser state.
- [x] Refresh-token replay изолирован по login/rotation families и не завершает независимые входы; legacy цепочки поддерживаются при rollout.
- [x] Password reset atomically invalidates sibling tokens; stale concurrent confirmation получает controlled отказ.
- [x] Cabinet auth hydration выполняется один раз после login/register/refresh и после reload восстановленной сессии под `React.StrictMode`.
- [x] Concurrent registration email conflict возвращает `email_exists` без partial auth rows; unrelated persistence failures не маскируются.
- [x] Password reset reissue закрывает старый code; per-user generation/revision защищает concurrent issue/reset и admin bootstrap password change.
- [x] Refresh token optimistic revision закрывает double rotation; concurrent reuse/logout/admin deactivation fail closed без активной stale branch.
- [x] Support conversation revision закрывает stale reply/status/note; оба UI обновляют очередь после conflict, pending inbound thread снова становится active.
- [x] Checkout claim атомарно связывает session/user/order; concurrent same-user получает winner, другой user не создаёт второй заказ, completed status остаётся terminal.
- [x] Subscription/access action DTO проверяются до рендера; межсерверная миграция подписки доступна в админке с `vpnManage`, target/auto выбором, подтверждением и фактическим завершением 3x-ui переноса вместо вечного planned job.
- [x] VPN-клиент переносится не только внутри выбранной панели: цели сгруппированы по доступным панелям, а после успеха UI открывает destination panel и обновленные client/inbound данные.
- [x] После межпанельной миграции карточки панелей сразу показывают обновленные source/target capacity и не откатываются после следующего refresh.
- [x] Кабинет сохраняет созданный заказ продления при сбое payment init и повторяет подготовку ссылки по тому же ID без создания дубликата.
- [x] Успешный reload заказов синхронизирует заметную карточку сохранённого продления по ID; terminal/non-retry состояние использует общий payment-availability contract и не показывает устаревшую retry-команду.
- [x] Provider confirmation URL доступна только для открытых payment states `New/Pending/WaitingConfirmation`; renewal/retry карточки и история скрывают ссылку после terminal/unknown transition.
- [x] Public checkout имеет один owner для claim/payment-init, сохраняет partial order для retry и отбрасывает late response после logout.
- [x] Persisted public checkout проходит bounded token/provider/shape validation и очищается до API-запросов при повреждённом browser state.
- [x] Public session hydration выполняет одну refresh-token rotation под StrictMode, сохраняет transient state для retry и отбрасывает late response после logout.
- [x] Cabinet support отбрасывает out-of-order и post-logout message responses, а новый вход начинается без support/auth/reset drafts предыдущей сессии.
- [x] Admin user/support detail views отбрасывают out-of-order и post-logout responses, очищают drafts при смене обращения и не загружают thread невыбранной status action.
- [x] Admin mutations применяют state/reload только в исходной session operation, отклоняют duplicate submit и сохраняют новый form draft при delayed completion.
- [x] Public/cabinet mutations отклоняют duplicate events, late session/unmount completion и сохраняют более новый support/reset draft; reset request/confirmation разделены на независимые формы с корректным Enter submit.
- [x] Критические admin-операции уведомлений, оплат/возвратов, подписок, VPN-доступа и поддержки проходят stateful desktop/mobile E2E; вся вкладка оплат находится в одном `tabpanel`.
- [x] Управляемые тарифы, реферальные программы, сценарии, релизы, FAQ и контент сайта проходят stateful create/edit/delete lifecycle на desktop/mobile с reload persistence.
- [x] Cabinet Telegram deep-link/unlink и support close/reopen проходят stateful desktop/mobile lifecycle с optimistic revision и reload persistence.
- [x] Payment provider accounts проходят secure create/edit/disable/reload/enable/check lifecycle без возврата write-only secret values в DTO/DOM.
- [x] Telegram bot settings проходят secure save/check/reload/edit lifecycle без возврата raw bot/webhook tokens в DTO/DOM.
- [x] Secure managed lifecycle VPN-сервера, 3x-ui панели и inbound-правила проходит desktop/mobile с write-only SSH/panel credentials.
- [x] Safe provisioning validation lifecycle проходит health/precheck/deploy/cancel/retry/support без реального SSH/Ansible.
- [x] 3x-ui client actions проходят disable/reload/enable/sync/reset-traffic lifecycle на desktop/mobile; cabinet/admin QR cache очищается до повторного GET и после local blocker, technical/English API diagnostics и native network errors заменяются русским fallback.
- [x] VPN access lifecycle сохраняет status/disabledAt/revision после reload и скрывает terminal secrets на desktop/mobile.
- [x] Subscription actions проходят activate/extend/sync/block/reload/unblock/migrate/reload/cancel lifecycle с persisted access state и terminal masking на desktop/mobile.
- [x] Payment refund lifecycle сохраняет partial/full refund state, автоматически подставляет остаток и блокирует повторный полный возврат на desktop/mobile.
- [x] Notification retry сохраняет Pending state, attempts reset/error cleanup и masked recipient; finance/support роли остаются read-only.
- [x] Admin production bundle budget: `5` chunks, largest `252482`, total raw `559564`, gzip `148564`.
- [x] Public catch-all `404` recovery проходит desktop/mobile, Axe и 25 viewport-конфигураций без blank screen/overflow.
- [x] Public route metadata/focus lifecycle проходит direct load, navigation и browser Back на desktop/mobile.
- [x] Admin metadata lifecycle проходит hydration/login, deep-link, 17 sections и logout на desktop/mobile.
- [x] Admin section history/focus lifecycle проходит tabs, Back/Forward, role fallback и order-links на desktop/mobile.
- [x] Admin invalid hash canonical fallback проходит direct/runtime recovery, focus и Back на desktop/mobile.
- [x] Admin skip links сохраняют section hash при focus transfer, login и reload на desktop/mobile.
- [x] Cabinet app-version modal удерживает focus, изолирует background/scroll и проходит 25-viewport responsive/WCAG gate.
- [x] Общие status badges различают составные negative/neutral/warning/success состояния и локализуют API casing.
- [x] Shared/public/admin styles и browser CSSOM gate не допускают внешние runtime asset URL на любом экране.
- [x] Подтверждаемые admin-операции удерживают async busy dialog до завершения API, блокируют повторный submit и проходят delayed desktop/mobile regression.
- [x] Cabinet public navigation принимает только safe configured base URL, а payment retry не передаёт внешнему провайдеру query/fragment текущей страницы.
- [x] Admin payment provider, Telegram и 3x-ui URL не принимают embedded credentials на frontend и backend.
- [x] VPN server/inbound handlers повторно валидируют programmatic submit; server, panel и inbound формы проверяют ranges, credentials, JSON и safe URL по backend-контракту.
- [x] Hidden releases/FAQ/content/scenarios/support/Telegram forms не обходят write capabilities при programmatic submit.
- [x] Все admin action dispatcher callsites явно проверяют capability целевого section, а не active tab.
- [x] Latest "Что нового": `2026-08-14-release-seed-preflight-demo-clock`, версия `0.716.0`; release seed проходит schema/ownership preflight до изменения базы, demo payment/VPN timestamps используют application clock; backend `1521/1521`.

## Как вести дальше

1. Для локальных UX/API/тестовых задач обновлять этот документ, master roadmap, `TEST_RESULTS.md` и "Что нового".
2. Для live-задач не ставить `[x]`, пока нет отчета staging/VPS smoke, ID платежей/webhook/subscription/VPN credential и подтверждения без утечки секретов.
3. Если возникает расхождение между этим файлом и master roadmap, главным считается `docs/PRODUCT_COMPLETION_ROADMAP.md`.
