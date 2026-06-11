# Master roadmap: доведение VPN Platform до production-ready

Документ нужен как единая рабочая карта проекта. По нему агент или разработчик должен идти сверху вниз, отмечать выполненные пункты и оставлять доказательства: тесты, скриншоты, ссылки на коммиты, результаты smoke-проверок и замечания.

Дата актуализации: 2026-06-10.

## Как вести этот roadmap

Статусы:

- `[ ]` - не начато.
- `[~]` - в работе. Используется временно, если задача начата, но не закрыта.
- `[x]` - выполнено и проверено.
- `[!]` - есть блокер или ошибка, которую нельзя закрыть без внешних данных.

Правила для агента:

1. Нельзя ставить `[x]`, если нет проверки из поля `Доказательство`.
2. Если задача закрыта, рядом нужно добавить дату, коммит или короткую ссылку на результат.
3. Если задача неактуальна, ее нельзя удалять молча: нужно заменить на `[x] Неактуально: причина`.
4. Если найдена новая ошибка, ее нужно добавить в подходящий раздел с новым ID.
5. После каждого крупного этапа нужно запускать обязательный validation gate:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release
npm test
npm run typecheck
npm run build
git diff --check
```

## Текущее резюме состояния

Что подтверждено на 2026-06-10:

- [x] `STATE-001` Backend test suite проходит: `279/279`.
- [x] `STATE-002` Frontend test suite проходит: `55/55`.
- [x] `STATE-003` TypeScript typecheck проходит для public-web, cabinet и admin-panel.
- [x] `STATE-004` Frontend build проходит для public-web, cabinet и admin-panel.
- [x] `STATE-005` GitHub Actions `validation`, `staging-validation`, `deploy-vps` прошли успешно.
- [x] `STATE-006` VPS отвечает: `/health/live`, `/health/ready`, `/`, `/cabinet/`, `/admin/`.
- [x] `STATE-007` Sandbox-покупка и sandbox-выдача VPN реализованы.
- [x] `STATE-008` Production и sandbox VPN-выдача разделены.
- [x] `STATE-009` Генерация VPN-ссылок поддерживает VLESS, VMess и Trojan.
- [ ] `STATE-010` Полный browser E2E всех экранов не завершен.
- [ ] `STATE-011` Live-платежи всех провайдеров не подтверждены.
- [ ] `STATE-012` Live-выдача через реальный 3x-ui не подтверждена.
- [ ] `STATE-013` Админка на VPS не проверена под рабочим admin-аккаунтом.
- [ ] `STATE-014` Roadmap и старая документация частично устарели и требуют синхронизации.

## P0. Блокеры production-запуска

### P0.1 Доступ администратора на VPS

Проблема: страница `/admin/` открывается, но рабочий вход в админку на VPS не подтвержден. Локальные demo-credentials возвращали `401 invalid_credentials`.

- [ ] `P0-ADMIN-001` Создать или восстановить production admin-аккаунт на VPS.
  - Что сделать: добавить безопасный способ seed/reset admin-пользователя через CLI, миграцию, one-shot команду или защищенный endpoint только для maintenance.
  - Критерий готовности: можно войти в админку на VPS, токены создаются, logout работает.
  - Доказательство: скриншот входа, HTTP 200 для admin API после логина, запись в changelog/коммит.

- [x] `P0-ADMIN-001A` Добавить безопасный CLI-механизм admin bootstrap/reset. 2026-06-10.
  - Что сделано: команда `admin-bootstrap` создает администратора или сбрасывает пароль существующего администратора без запуска HTTP-сервера.
  - Доказательство: backend unit tests `AdminBootstrapServiceTests`, локальная SQLite-проверка команды, HTTP-smoke login и admin dashboard.

- [ ] `P0-ADMIN-002` Проверить все разделы админки под реальным admin-аккаунтом.
  - Что сделать: открыть dashboard, users, payments, tariffs, subscriptions, vpn, nodes, panels, support, bot, releases, faq, content, scenarios, provisioning.
  - Критерий готовности: нет белого экрана, JS-ошибок, 401/403 после логина, сломанных таблиц и пустых обязательных состояний без объяснения.
  - Доказательство: browser smoke-отчет, список найденных ошибок или отметка "ошибок нет".

### P0.2 Реальная production-выдача VPN

Проблема: код production-выдачи есть, но live-проверка с настоящей 3x-ui панелью и inbound не завершена.

- [ ] `P0-VPN-001` Подключить реальную 3x-ui панель в админке.
  - Что сделать: добавить panel base URL, логин, пароль/секрет, проверить подключение, сохранить без утечки секрета в API.
  - Критерий готовности: кнопка проверки подключения возвращает success, секреты не видны в ответах API.
  - Доказательство: результат health-check панели, тест на отсутствие секрета в response.

- [ ] `P0-VPN-002` Синхронизировать реальные inbound-ы.
  - Что сделать: получить inbound-ы из 3x-ui, сохранить protocol, port, network, security, stream settings.
  - Критерий готовности: в админке виден хотя бы один активный inbound для VLESS или другого выбранного протокола.
  - Доказательство: скриншот админки и API response без секретов.

- [ ] `P0-VPN-003` Подключить реальный VPN-сервер к панели.
  - Что сделать: создать VPN node, указать hostname, регион, capacity, supported protocols, panel binding, режим production.
  - Критерий готовности: сервер `Ready`, не sandbox, не maintenance, принимает новых пользователей.
  - Доказательство: admin readiness показывает готовый VPN-контур.

- [ ] `P0-VPN-004` Провести live production order smoke.
  - Что сделать: создать тестовый заказ в production-режиме, провести оплату через выбранный live/sandbox merchant, дождаться webhook, проверить подписку и VPN-доступ.
  - Критерий готовности: подписка активна, создан клиент в 3x-ui, в кабинете видны URI и QR.
  - Доказательство: ID заказа, ID подписки, ID VPN credential, подтверждение клиента в 3x-ui.

- [ ] `P0-VPN-005` Проверить fail-closed поведение.
  - Что сделать: временно отключить inbound/ноду и убедиться, что production-выдача не создает fake-доступ.
  - Критерий готовности: подписка остается `PendingActivation` или заказ получает понятную ошибку, fake URI не создается.
  - Доказательство: backend test или smoke-лог с ожидаемой ошибкой.

### P0.3 Live-платежи

Проблема: провайдеры есть в коде и админке, но не каждый live-flow подтвержден реальной проверкой.

- [x] `P0-PAY-001` Составить матрицу готовности провайдеров в админке. 2026-06-10.
  - Что сделать: для каждого провайдера показывать checkout, webhook, refund, status recheck, sandbox, production, required fields.
  - Критерий готовности: админ видит, что именно поддержано, чего не хватает и почему способ оплаты скрыт от пользователя.
  - Доказательство: backend tests `PaymentProviderConfigurationRulesTests`, `AdminAutomationMvpTests`, local SQLite HTTP-smoke admin/public payment providers.

- [ ] `P0-PAY-002` YooKassa live/sandbox smoke.
  - Что сделать: checkout -> redirect -> webhook -> paid order -> subscription -> VPN issue.
  - Критерий готовности: пользователь получает VPN после успешной оплаты.
  - Доказательство: ID платежа, webhook event, subscription ID.

- [ ] `P0-PAY-003` RoboKassa smoke.
  - Что сделать: проверить required fields, checkout, callback signatures, success/fail URLs.
  - Критерий готовности: заказ корректно переходит в paid/failed.
  - Доказательство: webhook log и order status.

- [ ] `P0-PAY-004` YooMoney smoke.
  - Что сделать: проверить quickpay/payment flow, webhook/signature, ограничения manual recheck.
  - Критерий готовности: оплата отображается пользователю и создает подписку.
  - Доказательство: payment ID и subscription ID.

- [ ] `P0-PAY-005` CloudPayments smoke.
  - Что сделать: проверить hosted checkout/widget URL, webhook, required extra settings.
  - Критерий готовности: способ оплаты не показывается без обязательного hosted checkout/widget config.
  - Доказательство: readiness result, успешный тестовый платеж или зафиксированный блокер.

- [ ] `P0-PAY-006` TBank Acquiring smoke.
  - Что сделать: проверить terminal key/password, init payment, notification signature, success/fail.
  - Критерий готовности: заказ получает корректный статус после уведомления.
  - Доказательство: payment log и order transition.

- [ ] `P0-PAY-007` Prodamus smoke.
  - Что сделать: проверить payform, signature, callback, ограничения refund/recheck.
  - Критерий готовности: пользователь может оплатить, webhook активирует подписку.
  - Доказательство: callback log и subscription ID.

- [ ] `P0-PAY-008` Stripe smoke.
  - Что сделать: checkout session, webhook signature, payment_intent, refund при наличии capture/payment intent.
  - Критерий готовности: paid/refund flow работает или явно скрыт при неполной настройке.
  - Доказательство: Stripe test event ID.

- [ ] `P0-PAY-009` PayPal smoke.
  - Что сделать: order create/capture, webhook, capture ID для refund.
  - Критерий готовности: оплата активирует подписку, refund не вызывается без capture ID.
  - Доказательство: PayPal order/capture ID.

- [ ] `P0-PAY-010` Telegram Stars invoice flow.
  - Что сделать: реализовать или полностью проверить Telegram invoice, pre-checkout, successful payment update, выдачу подписки.
  - Критерий готовности: пользователь может купить тариф в Telegram Stars и получить VPN.
  - Доказательство: Telegram update log, order ID, subscription ID.

- [x] `P0-PAY-011` Скрыть неподтвержденные способы оплаты от публичного сайта. 2026-06-10.
  - Что сделать: публичный API должен отдавать только enabled + ready providers.
  - Критерий готовности: пользователь не видит способ оплаты, который не пройдет checkout.
  - Доказательство: `PaymentProvidersPublicControllerTests`, local SQLite HTTP-smoke `/api/public/payments/providers`.

## P1. Полные пользовательские сценарии

### P1.1 Публичный сайт

- [x] `P1-PUBLIC-001` Проверить главную страницу. 2026-06-10.
  - Ошибка/риск: CTA может вести не туда, контент может быть hardcoded, адаптив может ломаться.
  - Что сделать: проверить hero, преимущества, тарифы, FAQ, CTA, футер, мобильную версию.
  - Доказательство: local Browser smoke `http://127.0.0.1:5187/`, русские заголовки и CTA, console errors `0`.

- [x] `P1-PUBLIC-002` Проверить тарифы. 2026-06-10.
  - Ошибка/риск: цена/описание могут не совпадать с админкой.
  - Что сделать: изменить тариф в админке и убедиться, что публичный сайт и кабинет обновились.
  - Доказательство: `TariffManagementTests.PublicTariffsController_Should_Return_Only_Visible_Active_Tariffs_On_Sqlite`, frontend `public-page-state` tests, Browser smoke `/tariffs`.

- [x] `P1-PUBLIC-003` Проверить FAQ. 2026-06-10.
  - Ошибка/риск: FAQ может быть статическим или неуправляемым.
  - Что сделать: создать/отредактировать FAQ в админке, проверить отображение на сайте.
  - Доказательство: existing `FaqControllerTests`, `public-faq.test.ts`, Browser smoke `/faq` with search/category controls.

- [x] `P1-PUBLIC-004` Проверить ошибки API. 2026-06-10.
  - Ошибка/риск: при падении API пользователь видит пустой экран.
  - Что сделать: смоделировать 500/timeout/empty data.
  - Критерий готовности: есть понятный error/empty state.
  - Доказательство: `public-page-state.test.ts` loading/error/empty/ready, `npm test`, Browser smoke without console errors.

### P1.2 Регистрация, логин и кабинет

- [x] `P1-CAB-001` Полный сценарий регистрации. 2026-06-10.
  - Что сделать: регистрация, подтверждение успешного входа, ошибки duplicate email, слабого пароля, неверного email.
  - Доказательство: `AuthRegistrationControllerTests.Register_Should_Create_User_And_Reject_Duplicate_Weak_Password_And_Invalid_Email_On_Sqlite`, frontend auth validation tests.

- [x] `P1-CAB-002` Полный сценарий логина/logout. 2026-06-10.
  - Что сделать: валидный логин, неверный пароль, refresh token, logout, повторное открытие кабинета.
  - Доказательство: `AuthSessionControllerTests.Login_Refresh_Logout_Should_Work_With_Sqlite_And_Reject_Invalid_Sessions`, existing refresh-token hardening test.

- [x] `P1-CAB-003` Восстановление пароля. 2026-06-10.
  - Ошибка/риск: endpoint есть, но почтовый/реальный сценарий может быть не проверен.
  - Что сделать: forgot password -> reset token -> reset password -> login.
  - Доказательство: `AuthPasswordResetControllerTests.Forgot_And_Reset_Password_Should_Work_With_Sqlite_And_Revoke_Old_Sessions`, `AuthPasswordResetControllerTests.ResetPassword_Should_Reject_Token_When_User_Becomes_Inactive`.

- [x] `P1-CAB-004` Кабинет без подписки. 2026-06-10.
  - Что сделать: проверить empty state, CTA покупки, отсутствие QR/URI.
  - Доказательство: `MeCabinetControllerTests.Cabinet_Should_Return_Empty_Subscriptions_And_Accesses_For_User_Without_Subscription_On_Sqlite`, frontend dashboard empty/expired tests.

- [x] `P1-CAB-005` Кабинет с активной подпиской. 2026-06-10.
  - Что сделать: проверить статус, срок действия, тариф, VPN URI, QR, копирование ссылки.
  - Доказательство: `MeCabinetControllerTests.Cabinet_Should_Return_Active_Subscription_With_Tariff_Access_Qr_And_Server_Metadata_On_Sqlite`, frontend dashboard active/currentAccessId tests.

- [x] `P1-CAB-006` История заказов и платежей. 2026-06-10.
  - Что сделать: проверить paid, pending, failed, refunded статусы.
  - Доказательство: `MePaymentsControllerTests.GetPayments_Should_Return_Safe_User_History_With_Key_Statuses_On_Sqlite`, frontend payment helper tests.

- [x] `P1-CAB-007` Продление подписки. 2026-06-10.
  - Что сделать: купить продление активной подписки, проверить новую дату окончания и отсутствие дублей доступа.
  - Доказательство: `OrderServiceSqliteTests.CreateOrderAsync_Should_Reuse_Renewal_Order_Only_For_Same_Subscription`, `SubscriptionScenarioProvisioningTests.ActivateOrRenewFromOrderAsync_Should_Renew_Target_Subscription_From_Order_Context`, `TelegramBotPurchaseFlowTests.Renewal_Flow_Should_Create_Renewal_Order_And_Respect_Provider_Filtering`.

- [x] `P1-CAB-008` Окончание подписки. 2026-06-10.
  - Что сделать: смоделировать expired subscription, проверить отключение/disable VPN клиента.
  - Доказательство: `SubscriptionLifecycleExpiryTests`, sandbox E2E expiry, lifecycle notifications.

- [x] `P1-CAB-009` Поддержка в кабинете. 2026-06-10.
  - Что сделать: создать диалог, отправить сообщение, ответить из админки, закрыть обращение.
  - Доказательство: `MeSupportControllerTests.Cabinet_Support_Flow_Should_Work_With_Sqlite`, `MeSupportControllerTests.SupportEndpoints_Should_Reject_Too_Short_Text_And_Invalid_Status`, frontend support tests.

### P1.3 Telegram-бот

- [x] `P1-TG-001` Настройка бота из админки.
  - Что сделано: сохранение token/webhook/режима, write-only секреты, маска token, серверная валидация username/token/URL и кнопка проверки подключения в админке.
  - Доказательство: `AdminTelegramBotSettingsControllerTests`, `AdminAutomationMvpTests.Telegram_Bot_Settings_Should_Mask_Token_And_Update_Text_Templates`, frontend `ApiClient admin Telegram bot settings masks token at API boundary`.

- [x] `P1-TG-002` Привязка Telegram к пользователю.
  - Что сделано: link-token/status/unlink, одноразовый hash token, истечение token, запрет повторной привязки и SQLite-совместимые проверки активных Telegram-сессий.
  - Доказательство: `TelegramBotFoundationTests.Telegram_Link_Status_Unlink_Should_Work_End_To_End_On_Sqlite`, frontend `ApiClient cabinet Telegram link status and unlink endpoints are tokenized`.

- [x] `P1-TG-003` Покупка через Telegram.
  - Что сделано: выбор тарифа, создание/reuse Telegram-заказа, выбор платежного провайдера, Telegram Stars pre-checkout/successful_payment, активация подписки и выдача VPN-доступа.
  - Доказательство: `TelegramBotPurchaseFlowTests.Telegram_Stars_Purchase_Should_Create_Subscription_And_Vpn_Access_On_Sqlite`, Telegram update log, payment ID, subscription ID и access ID.

- [x] `P1-TG-004` Уведомления.
  - Что сделано: Telegram-очередь покрывает ожидание оплаты, успешную оплату, ошибку/отмену оплаты, активацию подписки, готовность VPN-доступа, ошибку выдачи доступа, приближение окончания и окончание подписки.
  - Доказательство: `PaymentWebhookProcessingTests.YooKassa_Failed_Webhook_Should_Queue_Telegram_Payment_Failed_Once_On_Sqlite`, `TelegramBotPurchaseFlowTests`, `SubscriptionLifecycleExpiryTests`, `X3UiIntegrationTests.Real_Vpn_Provider_Should_Auto_Create_Inbound_And_Client`.

## P2. Админка как полноценный центр управления

### P2.1 Dashboard

- [x] `P2-ADM-DASH-001` Довести dashboard readiness до единого центра диагностики.
  - Что сделано: dashboard показывает готовность платежей, webhook, тарифов, VPN, 3x-ui, Telegram, VPS provisioning и CI/CD workflow с категориями, severity и понятными сообщениями.
  - Доказательство: `AdminAutomationMvpTests.Dashboard_Summary_Should_Report_Production_Readiness_And_Ignore_Sandbox_Infrastructure`, frontend `ApiClient admin dashboard and user overview endpoints are tokenized`, Local SQLite API smoke.

- [x] `P2-ADM-DASH-002` Добавить быстрые переходы к проблемным разделам.
  - Что сделано: каждый readiness-блокер содержит `actionLabel`/`actionHref`, а админка показывает кнопку перехода на нужную вкладку.
  - Доказательство: backend readiness DTO assertions, frontend API contract test, admin-panel typecheck/build.

### P2.2 Тарифы

- [x] `P2-ADM-TAR-001` Проверить CRUD тарифов.
  - Что сделать: создать, изменить, отключить, удалить/архивировать тариф.
  - Критерий готовности: публичный сайт и кабинет получают актуальные данные.
  - Что сделано: создание и обновление нормализуют данные, отключение скрывает тариф с витрины, удаление несвязанного тарифа удаляет запись, связанный тариф архивируется без потери заказов и подписок.
  - Доказательство: `TariffManagementTests.AdminTariffs_Should_Create_And_Update_Extended_Content`, `AdminTariffs_Should_Delete_Unused_Tariff`, `AdminTariffs_Should_Archive_Linked_Tariff_Instead_Of_Delete`, Local SQLite API smoke.

- [x] `P2-ADM-TAR-002` Управление описанием тарифов.
  - Что сделать: features, subtitle, badge, лимиты, порядок сортировки, видимость.
  - Что сделано: админка управляет кратким и полным описанием, преимуществами, бейджем, лимитами устройств/трафика, sortOrder, категорией, видимостью, сценарием выдачи и текстом после оплаты; публичный каталог отдает эти поля в `TariffDto`.
  - Доказательство: `TariffManagementTests.PublicCatalog_Should_Return_Extended_Active_Tariff_Content`, `PublicTariffsController_Should_Return_Only_Visible_Active_Tariffs_On_Sqlite`, frontend contract test на `featuresTextToJson` и `tariff-preview`.

- [x] `P2-ADM-TAR-003` Валидация цен и валют.
  - Что сделать: запрет отрицательных цен, некорректных валют, пустого названия.
  - Что сделано: backend отклоняет пустое название, отрицательную цену, неположительный срок/число устройств, некорректный код валюты, конфликтующий slug и некорректное окно видимости; frontend показывает ошибки до отправки формы.
  - Доказательство: `TariffManagementTests.AdminTariffs_Should_Reject_Invalid_Price_And_Currency`, `AdminTariffs_Should_Reject_Duplicate_Slug_On_Create_And_Update`, frontend source contract test на `validateTariffForm`.

### P2.3 Сценарии работы

- [x] `P2-ADM-SCN-001` Проверить CRUD сценариев.
  - Что сделать: создать сценарий покупки/продления/ошибки/окончания.
  - Критерий готовности: сценарий реально влияет на выдачу VPN и уведомления.
  - Что сделано: CRUD сценариев защищен от дублей ключей, некорректного JSON и некорректных GUID тарифов; тексты сценария попадают в историю доступа, outbox и Telegram payload после оплаты.
  - Доказательство: `WorkScenarioControllerTests.AdminWorkScenarios_Should_Reject_Duplicate_Key_On_Create_And_Update`, `AdminWorkScenarios_Should_Reject_Invalid_Allowed_Tariff_Ids`, `SubscriptionScenarioProvisioningTests.ActivateOrRenewFromOrderAsync_Should_Apply_WorkScenario_To_Vpn_Provisioning`, локальный SQLite smoke.

- [x] `P2-ADM-SCN-002` Привязка сценария к тарифу.
  - Что сделать: админ выбирает, какой сценарий используется для конкретного тарифа.
  - Что сделано: редактор сценария позволяет выбрать разрешенные тарифы чекбоксами, backend нормализует список GUID без дублей, а тариф продолжает выбирать основной сценарий по ключу.
  - Доказательство: заказ по тарифу применяет нужный `protocol`, `serverSelectionRule`, `inboundSelectionRule`, `maxDevices`, `trafficLimit`, тексты уведомлений и `scenarioKey`; frontend source test проверяет `scenario-tariff-picker`.

- [x] `P2-ADM-SCN-003` UX редактора сценариев.
  - Что сделать: заменить технические поля на понятные блоки: "после оплаты", "при продлении", "при ошибке", "при окончании".
  - Что сделано: редактор разделен на основные параметры, поведение системы и тексты для пользователя; ручное поле `Связанные тарифы JSON` заменено списком тарифов с чекбоксами и подсказкой; сохранение сценария валидирует форму до запроса.
  - Доказательство: frontend source contract test на `validateWorkScenarioForm`, `scenario-tariff-picker`, `updateWorkScenarioTariffLink`, отсутствие `Связанные тарифы JSON`, browser smoke админки.

### P2.4 Платежи

- [x] `P2-ADM-PAY-001` Провайдер-специфичные формы.
  - Что сделать: для каждого провайдера показывать только его обязательные поля, подсказки и webhook URL.
  - Критерий готовности: нет общего непонятного `ShopId / SecretKey / ExtraSettingsJson` без объяснения.
  - Что сделано: общие поля получили провайдерские названия и подсказки, ручное поле `Extra settings JSON` заменено блоком дополнительных параметров провайдера; CloudPayments настраивает `hostedCheckoutUrl`, RoboKassa - `hashAlgorithm`, Telegram Stars - служебный статус invoice-сценария.
  - Доказательство: frontend source contract test на `provider-extra-settings`, `Hosted checkout URL`, `Алгоритм подписи` и отсутствие `Extra settings JSON`; SQLite smoke сохранения provider-specific настроек; browser smoke формы платежей.

- [x] `P2-ADM-PAY-002` Проверка подключения. 2026-06-10.
  - Что сделать: кнопка "Проверить подключение" должна возвращать понятный результат и список проблем.
  - Что сделано: backend проверяет обязательные поля, URL, IP allow-list, ExtraSettingsJson и провайдерские особенности; админка показывает результат отдельным блоком со статусом, временем проверки и списком диагностических пунктов.
  - Доказательство: `AdminAutomationMvpTests.Provider_Account_Check_Should_Return_Clear_Readiness_For_All_Web_Providers`, `Provider_Account_Check_Should_Report_CloudPayments_Hosted_Checkout_Problem`, `Provider_Account_Check_Should_Show_TelegramStars_As_Bot_Only`, frontend source contract test, локальный SQLite smoke.

- [x] `P2-ADM-PAY-003` Секреты write-only. 2026-06-10.
  - Что сделать: убедиться, что API никогда не возвращает secret/webhook secret/private credentials.
  - Что сделано: платежные секреты в форме редактирования остаются пустыми и показывают только configured-статус; backend закреплен тестом, что create/list/update/check ответы не содержат secret, webhook secret, protected-поля и приватные extra settings.
  - Доказательство: `AdminAutomationMvpTests.Provider_Account_Secrets_Should_Be_Write_Only_In_Admin_Responses`, frontend source contract test на `configured={editingProviderAccount?.hasSecretKey}`, локальный SQLite smoke.

- [x] `P2-ADM-PAY-004` Sandbox seed для всех провайдеров. 2026-06-10.
  - Что сделать: локальный режим должен поднимать безопасные sandbox accounts без реальных денег.
  - Что сделано: локальный seed добавляет отсутствующие sandbox-аккаунты для всех web-провайдеров без дублей и без перезаписи уже настроенных аккаунтов; Telegram Stars остается bot-only/disabled до настройки Telegram invoice flow.
  - Доказательство: `PaymentProviderSandboxSeedTests.Demo_Seed_Should_Add_Missing_Sandbox_Providers_Without_Duplicating_Existing_Accounts`, `PaymentProvidersPublicControllerTests`, локальный SQLite HTTP-smoke `/api/admin/payment-providers/accounts` и `/api/public/payments/providers`.

### P2.5 VPN-серверы и 3x-ui панели

- [x] `P2-ADM-VPN-001` CRUD VPN-серверов. 2026-06-10.
  - Что сделать: создать, изменить, отключить, архивировать сервер.
  - Что сделано: API и админка поддерживают создание, редактирование, отключение, закрытие/открытие распределения, обслуживание и безопасное удаление VPN-сервера; сервер без связей удаляется, сервер с подписками, VPN-доступами или provisioning-запусками архивируется без потери истории.
  - Доказательство: `AdminServerManagementTests`, frontend typecheck/build, локальный SQLite HTTP-smoke `/api/admin/servers`.

- [x] `P2-ADM-VPN-002` Health-check серверов. 2026-06-10.
  - Что сделать: показывать online/offline/maintenance/draining и причину.
  - Что сделано: добавлен admin endpoint ручной проверки VPN-сервера, история `NodeHealthChecks`, обновление `HealthStatus/LastHealthCheckAt`, причины для disabled/archived/maintenance/draining/provider errors и вывод последней проверки в списке серверов.
  - Доказательство: `AdminServerManagementTests`, frontend tests/typecheck/build, локальный SQLite HTTP-smoke `/api/admin/servers/{id}/health-check`.

- [x] `P2-ADM-VPN-003` CRUD 3x-ui панелей. 2026-06-10.
  - Что сделать: создать, проверить подключение, синхронизировать inbound-ы, отключить.
  - Что сделано: создание/редактирование панели, health-check, sync, управление статусом Active/Disabled и безопасное удаление; панель без связей удаляется, панель с inbound-ами, клиентами или историей отключается и остается в базе.
  - Доказательство: `X3UiIntegrationTests`, frontend tests/typecheck/build, локальный SQLite HTTP-smoke `/api/admin/vpn-panels`.

- [x] `P2-ADM-VPN-004` Управление inbound-ами. 2026-06-10.
  - Что сделать: set default, protocol match, active/inactive, validation stream settings.
  - Что сделано: backend валидирует имя, протокол VLESS/VMess/Trojan, порт, емкость, JSON-объекты и обязательный `network` в `streamSettingsJson`; неактивный inbound нельзя назначить основным, а при выключении default-флаг снимается. В админке добавлены создание, редактирование, включение/выключение, назначение основным и управление JSON-полями inbound-а.
  - Доказательство: `X3UiIntegrationTests`, frontend API contract tests, typecheck/build, локальный SQLite HTTP-smoke `/api/admin/vpn-panels/{id}/inbounds` и `/api/admin/vpn-inbounds/{id}`.

- [x] `P2-ADM-VPN-005` Управление клиентами. 2026-06-10.
  - Что сделать: enable, disable, reset traffic, migrate, sync.
  - Что сделано: добавлены admin endpoints `vpn-clients/{id}/enable`, `disable`, `sync`, `reset-traffic`, `migrate`; backend обновляет локальный `VpnClient`, связанные `AccessCredential`, sync status и timestamps, а в production вызывает 3x-ui client API. В админке список клиентов панели получил действия включения/выключения, синхронизации, сброса трафика и переноса на активный inbound того же протокола.
  - Доказательство: `X3UiIntegrationTests.Client_Management_Should_Enable_Disable_Sync_Reset_And_Migrate`, frontend API contract tests, typecheck/build, локальный SQLite HTTP-smoke `/api/admin/vpn-panels/{id}/clients` и guard `/api/admin/vpn-clients/{id}/disable`.

### P2.6 Пользователи, подписки, заказы

- [x] `P2-ADM-USR-001` Пользователи. 2026-06-11.
  - Что сделать: поиск, фильтры, карточка пользователя, подписки, заказы, платежи, Telegram.
  - Что сделано: список пользователей получил безопасную типизацию, поиск и фильтр по статусу; карточка пользователя разделена на профиль, метрики, подписки, заказы, платежи, VPN-доступы, Telegram-аккаунты и обращения поддержки. Backend overview теперь возвращает поля, совместимые с общими DTO, без `PasswordHash` и приватных metadata.
  - Доказательство: `AdminUsersControllerTests.GetOverview_Should_Return_Full_User_Profile_On_Sqlite`, frontend `admin user overview stats aggregates commercial and attention state`, typecheck/build, local SQLite API smoke `/api/admin/users` и `/api/admin/users/{id}/overview`.

- [x] `P2-ADM-SUB-001` Подписки. 2026-06-11.
  - Что сделать: активировать, отключить, продлить, синхронизировать VPN-доступ.
  - Что сделано: добавлены admin endpoints `subscriptions/{id}/activate` и `subscriptions/{id}/sync-access`; активация переводит подписку в `Active`, снимает блокировку/отмену и включает текущий VPN-доступ, синхронизация дергает lifecycle текущего доступа и пишет историю. В админке раздел подписок получил кнопки активации, продления, синхронизации доступа, блокировки/разблокировки и отмены с loading/disabled состояниями.
  - Доказательство: `AdminSubscriptionManagementTests`, `AdminAuthorizationPolicyTests`, frontend `ApiClient admin subscription and VPN access actions are confirmation-friendly POST calls`, typecheck/build, local SQLite HTTP-smoke.

- [x] `P2-ADM-ORD-001` Заказы. 2026-06-11.
  - Что сделать: фильтры по статусам, recheck payment, переход к пользователю/платежу.
  - Что сделано: backend `/api/admin/orders` принимает фильтры `status/search`, возвращает последний платеж заказа (`lastPaymentId/status/provider`) и сохраняет SQLite-safe сортировку в памяти; добавлен endpoint `orders/{id}/recheck-payment`, который проверяет последнюю платежную попытку заказа через общий payment orchestrator. В админке список заказов получил фильтр статуса, поиск, расширенную карточку, переходы к пользователю/платежу/подписке и кнопку проверки оплаты.
  - Доказательство: `AdminOrderManagementTests`, `AdminAuthorizationPolicyTests`, frontend `ApiClient admin order filters and recheck endpoints use finance-safe routes`, typecheck/build, local SQLite HTTP-smoke.

- [x] `P2-ADM-REF-001` Возвраты. 2026-06-11.
  - Что сделать: refund flow должен работать только для провайдеров, где есть нужные данные.
  - Что сделано: backend добавляет refund-readiness в `/api/admin/payments`: `refundSupported`, `canRefund`, `refundableAmount`, `refundBlockers`; endpoint `payments/{id}/refund` выполняет preflight и не вызывает провайдера, если адаптер не поддерживает возврат, платеж не успешный, сумма исчерпана или не хватает аккаунта/секретов. В админке платежи показывают остаток к возврату, причины блокировки, поле суммы и причину возврата перед подтверждением.
  - Доказательство: `AdminRefundManagementTests`, `AdminAuthorizationPolicyTests`, frontend `ApiClient admin payments expose refund readiness and send refund payload`, typecheck/build, local SQLite HTTP-smoke.

### P2.7 Контент, FAQ и "Что нового"

- [x] `P2-ADM-CNT-001` Управление контентом главной. 2026-06-11.
  - Что сделать: hero, преимущества, CTA, SEO/meta, порядок блоков.
  - Что сделано: backend получил проверку готовности `/api/admin/site-content/home-readiness` и восстановление обязательных блоков `/api/admin/site-content/home-defaults`; контролируются hero, SEO title/description, преимущества, тарифный заголовок, финальный CTA, footer и текст после оплаты. API запрещает дубли ключей при создании и редактировании. В админке раздел контента показывает, сколько обязательных блоков опубликовано, какие ключи отсутствуют, выключены, пустые или задублированы, и дает безопасную кнопку восстановления дефолтов.
  - Доказательство: изменение/восстановление в админке отражается в `/api/public/content/home`; `SiteContentControllerTests`, frontend `ApiClient site content endpoints cover public and admin CRUD`, typecheck/build, local SQLite HTTP-smoke.

- [x] `P2-ADM-FAQ-001` Управление FAQ. 2026-06-11.
  - Что сделать: категории, порядок, публикация/скрытие.
  - Что сделано: backend `/api/admin/faq` получил фильтры `category/visibility/search`, endpoint `/api/admin/faq/overview` возвращает счетчики публикации, категории, состояние главной/FAQ-страницы и дубли вопросов. Создание и редактирование блокируют одинаковый вопрос в одной категории с учетом регистра и кириллицы. В админке раздел FAQ показывает сводку, фильтры по категории/видимости/поиску, предупреждение о дублях и статусы публикации.
  - Доказательство: изменение FAQ в админке видно в `/api/public/content/faq` и `/api/public/content/faq?home=true`; `FaqControllerTests`, frontend `ApiClient FAQ endpoints cover public and admin CRUD`, typecheck/build, local SQLite HTTP-smoke.

- [x] `P2-ADM-REL-001` Раздел "Что нового". 2026-06-11.
  - Что сделать: создать релиз, отметить видимость, показать пользователю, mark as seen.
  - Что сделано: backend показывает только активные опубликованные релизы в `latest/history`, не дает отметить просмотренным скрытый или будущий релиз, а админские endpoints получили фильтры `visibility/source/search` и сводку `/api/app-version/admin/releases/overview`. В админке раздел "Что нового" показывает счетчики опубликованных, будущих, скрытых релизов, просмотры, последний опубликованный релиз, фильтры и статус "Запланировано" для будущих публикаций. Кабинет открывает новое обновление, показывает историю и фиксирует `mark-seen`.
  - Доказательство: `AppVersionControllerTests`, frontend `ApiClient app version endpoints are tokenized and mapped`, typecheck/build, local SQLite HTTP-smoke.

- [x] `P2-ADM-REL-002` Добавлять описание задач после крупных изменений. 2026-06-11.
  - Что сделать: после реализации этапа roadmap добавлять запись в "Что нового".
  - Что сделано: добавлен static guard-тест, который сверяет закрытые пункты P2.7 с `AppReleases/releases.json` и `TEST_RESULTS.md`, чтобы крупные изменения не проходили без записи в "Что нового" и результатов проверки. Для текущего этапа добавлена запись `2026-06-11-release-note-guard`.
  - Доказательство: `ReleaseDocumentationGuardTests`, release entry ID `2026-06-11-release-note-guard`, local SQLite HTTP-smoke latest/history.

## P3. UX/UI и единая стилистика

- [x] `P3-UX-001` Единая дизайн-система. 2026-06-11.
  - Что сделать: цвета, типографика, кнопки, поля, tabs, badges, tables, empty/loading/error states.
  - Что сделано: в `@vpn-platform/ui` добавлены общие `designTokens`, `SegmentedTabs`, `StateBlock` и `DataTableLite`; публичный сайт и кабинет используют общий компонент вкладок входа/регистрации; CSS получил единые radius/state/table стили для следующих экранов админки, кабинета и сайта.
  - Критерий готовности: базовые компоненты и состояния больше не дублируются между public-web и cabinet, а новые разделы могут собираться из одного UI-пакета.
  - Доказательство: frontend UI/static tests, frontend typecheck/build, backend full suite, local SQLite HTTP-smoke latest release `2026-06-11-design-system-foundation`.

- [x] `P3-UX-002` Современный login admin. 2026-06-11.
  - Что сделать: отдельный аккуратный экран логина, ошибки, loading, password visibility, remember/session hints.
  - Что сделано: экран входа админки получил явную клиентскую валидацию, remember email без сохранения пароля, подсказку по sessionStorage, checklist безопасности, `role="alert"` для ошибок и сохранение токена только после успешной авторизации.
  - Доказательство: frontend static guard tests, frontend typecheck/build, backend full suite, local SQLite HTTP-smoke latest release `2026-06-11-admin-login-polish`.

- [x] `P3-UX-003` Навигация админки по вкладкам/разделам. 2026-06-11.
  - Что сделать: каждый раздел открывается отдельно, настройки не идут одной длинной простыней.
  - Что сделано: админская навигация стала grouped tablist с группами "Операции", "Продажи", "VPN" и "Контент"; добавлен мобильный select, переходы "Предыдущий/Следующий", описания активных разделов, hash-синхронизация без прыжков страницы и `role=tabpanel` для основных секций.
  - Доказательство: frontend static guard tests, frontend typecheck/build, backend full suite, local SQLite HTTP-smoke latest release `2026-06-11-admin-section-navigation`.

- [x] `P3-UX-004` Проверка всех форм. 2026-06-11.
  - Что сделать: labels, placeholders, validation, disabled/loading states, submit/cancel positions.
  - Что сделано: добавлен общий `FormValidationSummary`; ключевые админ-формы платежных провайдеров, тарифов, VPN-серверов, 3x-ui панелей, inbound-правил и сценариев получили явные валидаторы, видимый summary ошибок и disabled submit по тем же правилам.
  - Доказательство: frontend UI/static tests, frontend typecheck/build, backend full suite, local SQLite HTTP-smoke latest release `2026-06-11-admin-form-validation`.

- [ ] `P3-UX-005` Адаптивность.
  - Что сделать: проверить 1440, 1280, 1024, 768, 390 px.
  - Доказательство: screenshots или Playwright report.

- [ ] `P3-UX-006` Доступность.
  - Что сделать: keyboard navigation, focus states, contrast, aria-label для icon buttons.
  - Доказательство: axe/lighthouse или ручной отчет.

- [ ] `P3-UX-007` Проверка русской локализации.
  - Что сделать: отсутствие mojibake, нормальные переносы, нет английских технических сообщений пользователю.
  - Доказательство: grep/check script + screenshots.

## P4. Backend, доменная логика и надежность

- [ ] `P4-BE-001` Финализировать state machines.
  - Что сделать: заказы, платежи, подписки, VPN-доступы, provisioning runs.
  - Критерий готовности: невозможные переходы запрещены, повторные webhook идемпотентны.
  - Доказательство: unit/integration tests.

- [ ] `P4-BE-002` Идемпотентность webhook.
  - Что сделать: повтор webhook не создает вторую подписку/второй VPN-доступ.
  - Доказательство: tests по каждому провайдеру.

- [ ] `P4-BE-003` Конкурентность оплаты.
  - Что сделать: два webhook/recheck одновременно не ломают order/subscription.
  - Доказательство: concurrency tests.

- [x] `P4-BE-004` Renew/expire jobs. 2026-06-10.
  - Что сделать: продление, окончание, отключение клиента, уведомления.
  - Доказательство: `SubscriptionLifecycleExpiryTests`, `SandboxE2EScenariosMvpTests`, local SQLite API smoke.

- [ ] `P4-BE-005` Audit log.
  - Что сделать: логировать admin actions, payment transitions, VPN provisioning, secret rotations.
  - Доказательство: tests + admin view.

- [ ] `P4-BE-006` Observability.
  - Что сделать: structured logs, correlation IDs, health details, metrics.
  - Доказательство: log examples и health output.

## P5. База данных и миграции

- [ ] `P5-DB-001` Полный аудит PostgreSQL schema.
  - Что сделать: проверить таблицы, индексы, FK, nullable-поля, миграции.
  - Доказательство: migration script/result, psql schema snapshot без секретов.

- [ ] `P5-DB-002` EF model drift check.
  - Что сделать: убедиться, что модель и миграции не расходятся.
  - Доказательство: test или отдельная команда drift-check.

- [ ] `P5-DB-003` Seed локальных данных.
  - Что сделать: локальный запуск должен иметь тарифы, sandbox payments, sandbox VPN node, admin user.
  - Доказательство: local smoke после чистой БД.

- [ ] `P5-DB-004` Backup/restore для VPS.
  - Что сделать: настроить backup PostgreSQL и инструкцию восстановления.
  - Доказательство: test restore на отдельную БД или runbook.

## P6. Безопасность и секреты

- [ ] `P6-SEC-001` Production secret storage.
  - Проблема: Own VPS provisioning пока не materializes protected SSH credentials для live Ansible.
  - Что сделать: secret manager или encrypted ProvisioningSecret table, temporary materialization с cleanup.
  - Доказательство: security tests, отсутствие секретов в logs/API/UI.

- [ ] `P6-SEC-002` Secret rotation.
  - Что сделать: ротация платежных, Telegram, 3x-ui, SSH секретов без показа старых значений.
  - Доказательство: admin flow + tests.

- [ ] `P6-SEC-003` RBAC.
  - Что сделать: роли admin/support/operator, запрет опасных действий без прав.
  - Доказательство: authorization tests.

- [ ] `P6-SEC-004` Rate limiting.
  - Что сделать: login, register, forgot password, webhook endpoints, public checkout.
  - Доказательство: tests/config.

- [ ] `P6-SEC-005` CORS/CSP/security headers.
  - Что сделать: проверить production headers для API и frontend.
  - Доказательство: curl/browser security report.

- [ ] `P6-SEC-006` Проверка утечек секретов.
  - Что сделать: scan repo, logs, docs, env examples на реальные ключи.
  - Доказательство: secret scan result.

## P7. Provisioning VPS

- [ ] `P7-PROV-001` Разделить dry-run, validation и live deploy.
  - Что сделать: UI и backend должны явно показывать режим, риски и ограничения.
  - Доказательство: tests + screenshot.

- [ ] `P7-PROV-002` Live Ansible credentials.
  - Что сделать: безопасная временная передача SSH credentials в Ansible.
  - Доказательство: live staging deploy без записи секрета в БД/лог.

- [ ] `P7-PROV-003` Precheck сервера.
  - Что сделать: OS, ports, disk, RAM, firewall, Docker/systemd, 3x-ui availability.
  - Доказательство: precheck report.

- [ ] `P7-PROV-004` Rollback.
  - Что сделать: при неудачном provisioning вернуть run/node в понятное состояние.
  - Доказательство: failure scenario test.

- [ ] `P7-PROV-005` Документация live provisioning.
  - Что сделать: отдельный runbook с предупреждениями и командами.
  - Доказательство: docs review.

## P8. CI/CD, GitHub и VPS deploy

- [ ] `P8-CI-001` Проверить workflow auto-detect docker/systemd.
  - Что сделать: убедиться, что deploy выбирает корректный режим и пишет понятный лог.
  - Доказательство: GitHub Actions log.

- [ ] `P8-CI-002` Required checks для main.
  - Что сделать: включить обязательные checks перед merge/push.
  - Доказательство: GitHub branch protection screenshot/config.

- [ ] `P8-CI-003` Secrets audit в GitHub.
  - Что сделать: проверить наличие и названия secrets для VPS, DB, deploy, registry.
  - Доказательство: список имен без значений.

- [ ] `P8-CI-004` VPS disk/memory maintenance.
  - Что сделать: безопасная очистка old artifacts, logs rotation, apt cache, docker cache если используется.
  - Доказательство: df/free до/после, без удаления рабочих данных.

- [ ] `P8-CI-005` Post-deploy smoke.
  - Что сделать: после deploy автоматически проверять API health, public, cabinet, admin, public providers.
  - Доказательство: Actions step log.

## P9. Тестирование

- [ ] `P9-TST-001` Backend обязательный suite.
  - Текущее состояние: проходит `226/226`.
  - Что сделать: держать зеленым после каждого изменения.
  - Доказательство: test output.

- [ ] `P9-TST-002` Frontend unit tests.
  - Текущее состояние: проходит `49/49`.
  - Что сделать: добавить тесты для новых UI-сценариев.
  - Доказательство: npm test output.

- [ ] `P9-TST-003` Playwright E2E public.
  - Что сделать: главная, тарифы, FAQ, checkout start.
  - Доказательство: Playwright report.

- [ ] `P9-TST-004` Playwright E2E cabinet.
  - Что сделать: register/login/order/payment status/subscription/access/support.
  - Доказательство: Playwright report.

- [ ] `P9-TST-005` Playwright E2E admin.
  - Что сделать: login, payments, tariffs, VPN, panels, scenarios, releases.
  - Доказательство: Playwright report.

- [ ] `P9-TST-006` Payment provider contract tests.
  - Что сделать: signature verification, webhook payloads, idempotency для всех провайдеров.
  - Доказательство: backend test names/results.

- [ ] `P9-TST-007` Real staging smoke checklist.
  - Что сделать: вручную или полуавтоматически пройти покупку и выдачу VPN на staging.
  - Доказательство: заполненный smoke report.

## P10. Документация

- [ ] `P10-DOC-001` README на русском.
  - Что сделать: запуск без Docker, запуск с Docker, env, DB, tests, deploy.
  - Доказательство: fresh clone local run.

- [ ] `P10-DOC-002` Документация администратора.
  - Что сделать: как настроить тарифы, платежи, VPN, 3x-ui, Telegram, сценарии.
  - Доказательство: review по каждому разделу админки.

- [ ] `P10-DOC-003` Документация пользователя.
  - Что сделать: как купить, оплатить, подключить VPN, продлить, обратиться в поддержку.
  - Доказательство: public/cabinet help pages.

- [ ] `P10-DOC-004` Документация разработчика.
  - Что сделать: архитектура, доменные сущности, state machines, тесты, добавление провайдера.
  - Доказательство: docs index.

- [ ] `P10-DOC-005` Убрать mojibake в старых документах.
  - Проблема: часть документов в консоли отображается как `Рџ...`, нужно проверить реальные файлы и перекодировать поврежденные.
  - Что сделать: проверить encoding всех `.md`, исправить поврежденные тексты.
  - Доказательство: script/report + нормальное отображение русского текста.

## P11. Финальная приемка production-ready

- [ ] `P11-ACC-001` Fresh local setup.
  - Что сделать: с нуля поднять backend, frontend, локальную БД, seed, пройти sandbox purchase.
  - Доказательство: fresh setup report.

- [ ] `P11-ACC-002` VPS production smoke.
  - Что сделать: deploy -> health -> admin login -> public order -> payment -> subscription -> VPN access.
  - Доказательство: smoke report.

- [ ] `P11-ACC-003` Mobile smoke.
  - Что сделать: public/cabinet/admin на мобильном viewport.
  - Доказательство: screenshots.

- [ ] `P11-ACC-004` No console errors.
  - Что сделать: проверить основные экраны в браузере.
  - Доказательство: browser console report.

- [ ] `P11-ACC-005` Security final check.
  - Что сделать: secrets, auth, headers, rate limits, permissions.
  - Доказательство: security checklist.

- [ ] `P11-ACC-006` Final docs and changelog.
  - Что сделать: обновить README, roadmap, "Что нового", инструкции запуска и deploy.
  - Доказательство: docs commit.

- [ ] `P11-ACC-007` Release decision.
  - Что сделать: принять решение: sandbox-ready, staging-ready или production-ready.
  - Критерий production-ready: все P0 закрыты, P1 критические сценарии закрыты, validation gate зеленый, VPS smoke успешен.
  - Доказательство: tagged release или зафиксированная версия.

## Журнал проверок

Новые проверки добавлять сверху.

| Дата | Кто | Что проверено | Результат | Доказательство |
| --- | --- | --- | --- | --- |
| 2026-06-10 | Codex | Управление 3x-ui клиентами: enable/disable, sync, reset traffic, migrate, UI-действия и SQLite-safe списки панели | Зеленое | Backend tests `279/279`, frontend tests `55/55`, typecheck/build, Local SQLite HTTP-smoke `/api/admin/vpn-panels/{id}/clients` |
| 2026-06-10 | Codex | Управление inbound-ами 3x-ui: create/edit, set default, active/inactive, protocol match и validation stream settings | Зеленое | Backend tests `278/278`, frontend tests `55/55`, typecheck/build, Local SQLite HTTP-smoke `/api/admin/vpn-panels/{id}/inbounds` и `/api/admin/vpn-inbounds/{id}` |
| 2026-06-10 | Codex | CRUD 3x-ui панелей: создание, редактирование, проверка подключения, sync, отключение/включение и безопасное удаление с историей | Зеленое | Backend tests `273/273`, frontend tests `55/55`, typecheck/build, Local SQLite HTTP-smoke `/api/admin/vpn-panels` |
| 2026-06-10 | Codex | Health-check VPN-серверов: Healthy, Degraded для обслуживания, Unhealthy при ошибке провайдера, история проверок и SQLite endpoint | Зеленое | Backend tests `271/271`, frontend tests `55/55`, typecheck/build, Local SQLite HTTP-smoke `/api/admin/servers/{id}/health-check` |
| 2026-06-10 | Codex | CRUD VPN-серверов: создание, редактирование, отключение, удаление чистого сервера и архивирование сервера с историей | Зеленое | Backend tests `268/268`, frontend tests `55/55`, typecheck/build, Local SQLite HTTP-smoke `/api/admin/servers` delete/archive |
| 2026-06-10 | Codex | Admin dashboard readiness: платежи, webhook, тарифы, VPN, 3x-ui, Telegram, VPS, CI/CD и быстрые переходы | Зеленое | Targeted backend tests `11/11`, frontend tests `55/55`, typecheck/build, Local SQLite API smoke |
| 2026-06-10 | Codex | Telegram-уведомления: pending/succeeded/failed платежи, активация, выдача VPN-доступа и lifecycle подписок | Зеленое | Backend tests `250/250`, frontend tests `55/55`, typecheck/build, Local SQLite API smoke |
| 2026-06-10 | Codex | Покупка через Telegram: тариф, заказ, Telegram Stars payment, pre-checkout, successful_payment, подписка и VPN-доступ на SQLite | Зеленое | Backend tests `249/249`, frontend tests `55/55`, typecheck/build, Local SQLite API smoke |
| 2026-06-10 | Codex | Привязка Telegram к пользователю: deep link, status, unlink, повторная привязка и SQLite expiry-запросы | Зеленое | Backend tests `248/248`, frontend tests `55/55`, typecheck/build, Local SQLite API smoke |
| 2026-06-10 | Codex | Настройка Telegram-бота из админки: защищенные токены, Webhook/LongPolling, public username, WebApp URL, шаблоны и проверка готовности | Зеленое | Backend tests `247/247`, frontend tests `54/54`, typecheck/build, Local SQLite API smoke |
| 2026-06-10 | Codex | Публичный сайт: главная, тарифы, FAQ, состояния loading/error/empty/ready, CTA и отсутствие console errors | Зеленое | Backend tests `245/245`, frontend tests `54/54`, local Browser smoke public-web |
| 2026-06-10 | Codex | Кабинет без активной подписки и с активным VPN-доступом: empty state, CTA покупки, тариф, сервер, URI, QR-метаданные | Зеленое | Backend tests `244/244`, SQLite cabinet state tests, frontend tests/typecheck/build |
| 2026-06-10 | Codex | Восстановление пароля: forgot, одноразовый reset token, новый пароль, отзыв сессий, expired/inactive user | Зеленое | Backend tests `242/242`, SQLite password reset flow, frontend tests/typecheck/build |
| 2026-06-10 | Codex | Логин/logout кабинета: валидный логин, неверный пароль, inactive user, refresh rotation, reuse detection, logout | Зеленое | Backend tests `240/240`, SQLite session flow, frontend tests/typecheck/build |
| 2026-06-10 | Codex | Регистрация в кабинете: успешный аккаунт, duplicate email, слабый пароль, неверный email, fallback display name | Зеленое | Backend tests `239/239`, SQLite registration test, frontend tests/typecheck/build |
| 2026-06-10 | Codex | История заказов и платежей в кабинете: paid/pending/failed/refunded, счетчики webhook/refund, отсутствие raw provider payload | Зеленое | Backend tests `238/238`, SQLite payment history test, frontend tests/typecheck/build |
| 2026-06-10 | Codex | Поддержка в кабинете: создание обращения, ответ администратора, скрытые внутренние заметки, закрытие и переоткрытие | Зеленое | Backend tests `237/237`, SQLite support flow, frontend tests/typecheck/build |
| 2026-06-10 | Codex | Продление конкретной подписки из кабинета и Telegram без дублей доступа | Зеленое | Backend tests `235/235`, frontend tests/typecheck/build, SQLite renewal order test |
| 2026-06-10 | Codex | Lifecycle подписок: grace/expired, отключение VPN, outbox и Telegram-уведомления | Зеленое | Backend tests `233/233`, local SQLite API smoke |
| 2026-06-10 | Codex | Матрица готовности платежных провайдеров и публичная фильтрация | Зеленое | Backend tests, frontend tests/typecheck/build, local SQLite HTTP-smoke |
| 2026-06-10 | Codex | CLI admin bootstrap/reset для локальной SQLite-БД | Зеленое | `admin-bootstrap`, `AdminBootstrapServiceTests`, HTTP-smoke login/admin dashboard |
| 2026-06-10 | Codex | Backend tests, frontend tests, typecheck, build, GitHub Actions, VPS HTTP health | Зеленое, кроме live admin auth/E2E | Локальный аудит и ответы VPS 200 |

## Журнал найденных ошибок

Новые ошибки добавлять сверху.

| ID | Приоритет | Область | Ошибка/риск | Статус | Что нужно сделать |
| --- | --- | --- | --- | --- | --- |
| `BUG-2026-06-10-010` | P1 | Local SQLite startup | При `UseEnsureCreatedForLocalSqlite=true` и `ApplyMigrationsOnStartup=false` новая SQLite-БД не создавала таблицы до admin bootstrap, из-за чего API падал на `no such table: Users`. | Исправлено | `EnsureCreated` и локальный schema repair выполняются до bootstrap/seed всегда, когда включен local SQLite EnsureCreated. |
| `BUG-2026-06-10-009` | P1 | Payments/SQLite | `PaymentOrchestrator.InitPaymentAsync` сортировал pending-платежи по `DateTimeOffset` в SQL, из-за чего локальная SQLite-БД падала при создании платежа. | Исправлено | Выборка ограничивается order/provider/account/status, сортировка `CreatedAt` выполняется в памяти. |
| `BUG-2026-06-10-008` | P1 | Payments/SQLite | `PaymentProviderAccountService.GetEnabledAccountEntityAsync` сортировал аккаунты провайдера по `CreatedAt` в SQLite SQL, что ломало выбор активного платежного аккаунта. | Исправлено | Сначала выбираются включенные кандидаты провайдера, затем приоритет default/created применяется в памяти. |
| BUG-001 | P0 | VPS/Admin | Не подтвержден рабочий вход в админку на VPS | partial | CLI-механизм восстановления добавлен; дальше выполнить reset на VPS и пройти smoke |
| BUG-002 | P0 | VPN | Не подтверждена live-выдача через реальный 3x-ui | open | Подключить panel/inbound/node и провести production smoke |
| BUG-003 | P0 | Payments | Не все payment providers подтверждены live/sandbox smoke | open | Пройти матрицу провайдеров |
| BUG-004 | P1 | Frontend | Нет полного browser E2E по public/cabinet/admin | open | Добавить Playwright/smoke проверки |
| BUG-005 | P1 | Docs | Часть roadmap/docs устарела, возможен mojibake в старых `.md` | open | Синхронизировать и проверить кодировку |
| BUG-006 | P1 | Provisioning | Live Ansible provisioning не production-ready из-за secret materialization | open | Реализовать безопасную передачу секретов |
