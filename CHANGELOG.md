# Changelog

## 0.534.0 - 2026-08-09

Release entry: `2026-08-09-vpn-panel-api-dto-validation`.

### Исправлено

- API-клиент больше не принимает неполные DTO 3x-ui panel, inbound, client, sync run/event и health check как доверенные данные.
- Все 19 read и mutation маршрутов проверяют обязательные поля, даты, enum, JSON-объекты, nullable-значения, capacity/status связи, уникальные `id` и соответствие parent id фактическому backend-контракту.
- Поврежденный список панелей очищает выбранную панель и связанные inbound/client/sync/health данные; поколение запроса не позволяет позднему ответу восстановить stale состояние.

### Проверено

- Frontend `101/101`, включая malformed panel/inbound/client/sync/health DTO; typecheck и production build всех трех приложений.
- Admin desktop/mobile `6/6` с delayed stale-details regression; все admin-разделы и representative responsive widths; полный console/responsive suite `18/18`.
- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует; fresh local SQLite checkout/payment/subscription/VPN smoke пройден.
- Dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings; release/documentation/UTF-8 guards подтверждают latest `2026-08-09-vpn-panel-api-dto-validation`, версию `0.534.0` и roadmap `547/567`.
- Roadmap progress: `547/567` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.533.0 - 2026-08-09

Release entry: `2026-08-09-admin-content-api-dto-validation`.

### Исправлено

- API-клиент больше не принимает неполные DTO тарифов, реферальных программ и начислений, релизов приложения, FAQ, контента сайта и сценариев работы как доверенные данные.
- Runtime-контракты проверяют обязательные поля, даты, enum, nullable-значения, JSON-объекты и массивы, уникальные `id`, счетчики overview/readiness и согласованность latest release с текущей версией.
- Read и mutation ответы всех перечисленных разделов завершаются controlled `ApiClientError` `502` до TypeScript cast при нарушении фактического backend-контракта.
- Поврежденный список тарифов очищает ранее загруженные карточки вместо показа устаревших коммерческих данных.

### Проверено

- Frontend `100/100`, включая malformed content/reference/app-release read и mutation DTO; typecheck и production build всех трех приложений.
- Admin desktop/mobile `6/6`, malformed tariffs regression `2/2`, все admin-разделы и representative responsive widths `2/2`; полный console/responsive suite `18/18`.
- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует; fresh local SQLite checkout/payment/subscription/VPN smoke пройден.
- Dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings; release/documentation/UTF-8 guards подтверждают latest `2026-08-09-admin-content-api-dto-validation`, версию `0.533.0` и roadmap `546/566`.
- Roadmap progress: `546/566` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.532.0 - 2026-08-09

Release entry: `2026-08-09-admin-finance-support-api-dto-validation`.

### Исправлено

- Админка больше не принимает неполные DTO журнала аудита, email-уведомлений, recheck/refund, платежных аккаунтов, webhook-событий, возвратов и поддержки как доверенные данные.
- Runtime-контракты проверяют вложенные capabilities/required fields платежного аккаунта, даты, enum, суммы, nullable-поля, уникальные `id` и согласованность account check с исходным аккаунтом.
- Для admin support введен отдельный контракт сообщений: обычные сообщения и внутренние заметки проверяются по фактическим backend direction/isInternalNote, не ослабляя кабинетный запрет на внутренние заметки.
- Поврежденный список платежных аккаунтов теперь дает controlled `ApiClientError` `502` и очищает ранее загруженные способы оплаты вместо показа устаревшей готовности.

### Проверено

- Frontend `99/99`, включая malformed finance/audit/notification/support read и mutation DTO; typecheck и production build всех трех приложений.
- Admin desktop/mobile `6/6`, malformed payment providers regression `2/2`, все admin-разделы и representative responsive widths `2/2`; полный console/responsive suite `18/18`.
- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует; fresh local SQLite checkout/payment/subscription/VPN smoke пройден.
- Dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings; release/documentation/UTF-8 guards подтверждают latest `2026-08-09-admin-finance-support-api-dto-validation`, версию `0.532.0` и roadmap `545/565`.
- Roadmap progress: `545/565` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.531.0 - 2026-08-09

Release entry: `2026-08-09-admin-core-api-dto-validation`.

### Исправлено

- Админка больше не принимает неполные DTO административной сессии, dashboard readiness, пользователей, user overview, подписок, VPN-доступов, заказов и платежей как доверенные данные.
- Runtime-контракты проверяют capabilities, счетчики, вложенные readiness checks, даты, backend enum, обязательные поля и уникальные `id`; несовместимый ответ завершается controlled `ApiClientError` `502` без raw payload.
- При ошибке или пустом результате загрузки пользователей очищаются выбранный пользователь и его ранее загруженная карточка, поэтому персональные данные не остаются в активном разделе.
- Backend user overview снова соответствует `SupportConversationDto` и возвращает обязательный optimistic-concurrency `revision`.

### Проверено

- Frontend `98/98`, включая malformed admin core DTO; typecheck и production build всех трех приложений.
- Admin desktop/mobile `6/6`, malformed users regression `2/2`, все admin-разделы и representative responsive widths `2/2`; полный console/responsive suite `18/18`.
- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует; SQLite overview regression и fresh local SQLite checkout/payment/subscription/VPN smoke пройдены.
- Dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings; `RoadmapCurrentStateTests` и release/documentation/UTF-8 guards подтверждают latest `2026-08-09-admin-core-api-dto-validation`, версию `0.531.0` и roadmap `544/564`.
- Roadmap progress: `544/564` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.530.0 - 2026-08-09

Release entry: `2026-08-09-cabinet-api-dto-validation`.

### Исправлено

- Личный кабинет больше не принимает синтаксически корректные, но неполные DTO профиля, подписок, заказов, платежей, VPN-доступов, рефералов, поддержки и Telegram как доверенные данные.
- Runtime-контракты проверяют обязательные поля, даты, числовые значения, backend enum и уникальные `id`; несовместимый ответ завершается controlled `ApiClientError` `502` без raw payload до передачи данных в React.
- Та же проверка применяется к ответам создания заказа и обращения, загрузки отдельного платежа, ответа в поддержку и отвязки Telegram.
- Независимые cabinet/all-screens browser fixtures приведены к реальным backend-контрактам, включая `Completed`, lower-case support channel/status и полные payment/access projections.

### Проверено

- Frontend `98/98`, включая malformed DTO всех bootstrap-коллекций и связанных операций; typecheck и production build всех трех приложений.
- Malformed cabinet bootstrap regression desktop/mobile `2/2`: видимый error-state, без старого email/VPN URI и без `pageerror`.
- Полный public/cabinet/admin/all-screens/mobile/console responsive suite `18/18`; dependency audit `0 vulnerabilities`.
- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует; fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; secret scan `649` files/`0` findings.
- `RoadmapCurrentStateTests` и release/documentation/UTF-8 guards подтверждают latest `2026-08-09-cabinet-api-dto-validation`, версию `0.530.0` и roadmap `543/563`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.529.0 - 2026-08-09

Release entry: `2026-08-09-public-api-dto-validation`.

### Исправлено

- Публичные тарифы, FAQ, контент главной и способы оплаты больше не передают в React синтаксически корректные, но неполные или несовместимые DTO.
- Runtime-контракты проверяют обязательные строки, числа и флаги, вложенный список `features`, активные payment provider/mode и уникальные `id/slug/key/provider`; нарушение возвращает controlled `ApiClientError` `502` без raw payload.
- Страница тарифов показывает штатное error-state для ответа `[{}]` вместо пустой карточки, неверного checkout или render exception.

### Проверено

- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует.
- Frontend `98/98`, включая malformed DTO всех четырех публичных коллекций, typecheck и production build всех трёх приложений.
- Invalid-item public regression desktop/mobile `2/2`; полный public/cabinet/admin/all-screens/mobile/console responsive suite `18/18` без `pageerror`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings.
- `RoadmapCurrentStateTests` и release/documentation/UTF-8 guards подтверждают latest `2026-08-09-public-api-dto-validation`, версию `0.529.0` и roadmap `542/562`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.528.0 - 2026-08-05

Release entry: `2026-08-05-api-response-shape-boundary`.

### Исправлено

- Typed API transport теперь проверяет не только JSON/MIME, но и ожидаемую top-level форму: list endpoints принимают только array, DTO endpoints только non-array object.
- Все `41` list-метода переведены на явный `requestArray<T>`; object/array mismatch завершается controlled `ApiClientError` `502` до TypeScript cast и UI.
- Test fixtures FAQ, referral, VPN panel и app releases приведены к реальным read/write контрактам, которые прежний permissive transport скрывал.

### Проверено

- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует.
- Frontend `98/98`, включая `{}` вместо tariffs и `[]` вместо dashboard DTO, inventory list methods `41/41`, typecheck и production build всех трёх приложений.
- Wrong-shape public regression desktop/mobile `2/2`; полный public/cabinet/admin/all-screens/mobile/console responsive suite `18/18` без `pageerror`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings.
- `RoadmapCurrentStateTests` и release/documentation/UTF-8 guards подтверждают latest `2026-08-05-api-response-shape-boundary`, версию `0.528.0` и roadmap `541/561`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.527.0 - 2026-08-05

Release entry: `2026-08-05-api-response-size-boundary`.

### Исправлено

- API client больше не использует безусловный `response.text()` для JSON, error и QR body: общий streaming reader ограничивает объём до полной буферизации.
- Typed JSON ограничен `10 MB`, server error payload `64 KB`, QR SVG `1 MB`; declared `Content-Length` проверяется до чтения, фактический размер считается по bytes.
- При превышении transport вызывает `ReadableStream.cancel()` и возвращает controlled `ApiClientError` `502`; неверный успешный MIME также отклоняется до чтения потенциально чрезмерного body.

### Проверено

- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует.
- Frontend `98/98`, включая ранний declared-length отказ, bounded error response и фактическую отмену streamed QR, typecheck и production build всех трёх приложений.
- Реальный oversized JSON public regression desktop/mobile `2/2`; полный public/cabinet/admin/all-screens/mobile/console responsive suite `18/18` без `pageerror`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings.
- `RoadmapCurrentStateTests` и release/documentation/UTF-8 guards подтверждают latest `2026-08-05-api-response-size-boundary`, версию `0.527.0` и roadmap `540/560`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.526.0 - 2026-08-05

Release entry: `2026-08-05-api-json-response-boundary`.

### Исправлено

- Typed API client больше не передаёт UI успешный HTTP-ответ как DTO без runtime-проверки: принимается только непустой JSON object/array с `application/json` или `application/*+json`.
- HTML proxy/login page, отсутствующий или неподдерживаемый MIME, пустой body, повреждённый JSON и JSON-примитив завершаются controlled `ApiClientError` `502` до `.map/.filter` и рендера страницы.
- Ошибочные HTTP-ответы сохраняют прежнюю поддержку JSON и text payload, поэтому серверные сообщения и нормализация ошибок не изменились.

### Проверено

- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует.
- Frontend `98/98`, включая MIME/empty/malformed/primitive/structured-suffix response cases, typecheck и production build всех трёх приложений.
- Malformed `200 text/html` public regression на desktop/mobile `2/2`; полный public/cabinet/admin/all-screens/mobile/console responsive suite `18/18` без `pageerror`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings.
- `RoadmapCurrentStateTests` и release/documentation/UTF-8 guards подтверждают latest `2026-08-05-api-json-response-boundary`, версию `0.526.0` и roadmap `539/559`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.525.0 - 2026-08-05

Release entry: `2026-08-05-api-request-timeout-boundary`.

### Исправлено

- Все frontend API-операции получили единый 30-секундный deadline, поэтому зависший backend или proxy больше не удерживает экраны public, cabinet и admin в бесконечном loading/busy-state.
- Timeout охватывает не только ожидание headers, но и полное чтение JSON/SVG body; transport прерывается через `AbortController`, а UI получает controlled `408` ошибку с понятным текстом.
- Timer и внешний abort listener всегда очищаются в `finally`, включая успешный ответ и ошибку, поэтому завершенный запрос не получает поздний abort.

### Проверено

- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует.
- Frontend `97/97`, включая stalled fetch/body и timer cleanup `2/2`, typecheck и production build всех трех приложений.
- Полный public/cabinet/admin/all-screens/mobile/console responsive suite `16/16`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings.
- Release/documentation/UTF-8 guards подтверждают latest `2026-08-05-api-request-timeout-boundary`, версию `0.525.0` и roadmap `538/558`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.524.0 - 2026-08-05

Release entry: `2026-08-05-admin-readiness-link-boundary`.

### Исправлено

- Production-readiness dashboard больше не считает неизвестный server-driven `actionHref` допустимым: переход разрешен только для точного fragment существующего admin-раздела.
- Внешний, исполняемый, измененный или недоступный текущей роли адрес не создает активную ссылку; допустимый href канонизируется перед передачей браузеру.
- Общий parser также используется при чтении hash на старте админки, поэтому неизвестный fragment fail-closed возвращает пользователя на dashboard.

### Проверено

- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует.
- Frontend `95/95`, typecheck и production build всех трех приложений.
- Измененные admin desktop/mobile сценарии `6/6`; полный public/cabinet/admin/all-screens/mobile/console responsive suite `16/16`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings.
- Release/documentation/UTF-8 guards подтверждают latest `2026-08-05-admin-readiness-link-boundary`, версию `0.524.0` и roadmap `537/557`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.523.0 - 2026-08-05

Release entry: `2026-08-05-qr-svg-render-boundary`.

### Исправлено

- QR-ответы кабинета и админки принимаются только как непустой `image/svg+xml` в пределах лимита; неподдерживаемый MIME и чрезмерный payload завершаются controlled API-client ошибкой.
- Все пять QR-preview больше не вставляют серверный текст через `dangerouslySetInnerHTML`: shared validator отклоняет script, event handlers, ссылки и активные SVG-элементы, а допустимое изображение отображается изолированно через `<img>`.
- Поврежденный или активный SVG дает видимое сообщение об отказе без исполнения разметки; фиксированные размеры preview сохраняют стабильную геометрию на desktop и mobile.

### Проверено

- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует.
- Frontend `94/94`, typecheck и production build всех трех приложений.
- Измененные cabinet/admin desktop/mobile QR-сценарии `8/8`; полный public/cabinet/admin/all-screens/mobile/console responsive suite `16/16`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings.
- Release/documentation/UTF-8 guards подтверждают latest `2026-08-05-qr-svg-render-boundary`, версию `0.523.0` и roadmap `536/556`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.522.0 - 2026-08-05

Release entry: `2026-08-05-payment-link-safety-boundary`.

### Исправлено

- `PaymentOrchestrator` принимает только абсолютный `http/https` return URL без встроенных учетных данных и прекращает операцию до вызова провайдера при нарушении контракта.
- Относительный, исполняемый, `data:` или credential-bearing redirect провайдера не сохраняется и не возвращается клиенту; небезопасный legacy `confirmationUrl` также не переиспользуется.
- Public и cabinet открывают, показывают и копируют payment/Telegram URL только после общей frontend-проверки; при отказе выводится понятное предупреждение без активной ссылки.

### Проверено

- Backend full suite `1112/1112`, включая targeted payment initialization SQLite suite `17/17` и негативные return/provider/stored URL cases.
- Frontend `92/92`, typecheck и production build всех трех приложений.
- Измененные public/cabinet desktop/mobile сценарии `4/4`; полный public/cabinet/admin/all-screens/mobile/console responsive suite `16/16`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access.
- `RoadmapCurrentStateTests`, release/documentation guards, dependency/secret/UTF-8 проверки подтверждают latest `2026-08-05-payment-link-safety-boundary`, версию `0.522.0` и roadmap `535/555`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.521.0 - 2026-08-05

Release entry: `2026-08-05-clipboard-feedback-boundary`.

### Исправлено

- Shared `CopyButton` больше не сообщает об успехе, если Clipboard API отсутствует или браузер отклонил разрешение; Promise rejection обрабатывается внутри компонента.
- Быстрые повторные клики сериализованы синхронным guard, таймер feedback очищается при размонтировании, а постоянная строка статуса сохраняет стабильную геометрию панели.
- Удалён неиспользуемый админский clipboard helper с прежним ложным success-поведением.

### Проверено

- Frontend `91/91`, typecheck и production build всех трех приложений.
- Cabinet Clipboard API success/NotAllowedError на desktop/mobile `2/2`; полный public/cabinet/admin/all-screens/mobile/console responsive suite `16/16`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access.
- Backend full suite `1103/1103`; release/documentation guards подтверждают latest `2026-08-05-clipboard-feedback-boundary`, версию `0.521.0` и roadmap `534/554`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.520.0 - 2026-08-05

Release entry: `2026-08-05-checkout-operation-guard`.

### Исправлено

- Публичная страница блокирует все тарифы, способ оплаты и промокод на время checkout; синхронный guard не допускает второй запрос до React-перерендера.
- Повторная оплата в кабинете больше не зависит от popup после асинхронного API-вызова: результат отображается отдельной карточкой с явной ссылкой и копированием.

### Проверено

- Frontend `90/90`, typecheck и production build всех трех приложений.
- Playwright desktop/mobile для измененных сценариев `4/4`; полный public/cabinet/admin/all-screens/mobile/console suite `16/16` без неожиданных console errors и responsive overflow.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access и подтвердил предыдущий опубликованный release перед синхронизацией seed.
- Backend full suite `1103/1103`; release/documentation guards подтверждают latest `2026-08-05-checkout-operation-guard`, версию `0.520.0` и roadmap `533/553`.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами.

## 0.519.0 - 2026-08-05

Release entry: `2026-08-05-server-owned-checkout-context`.

### Исправлено

- Public checkout принимает только новую web-подписку и не позволяет подменить канал для channel-limited промокода или реферальной программы.
- `IsFirstPurchase` вычисляется backend по предыдущим завершенным покупкам и больше не доверяет public/cabinet payload.
- Checkout-сессия не создается для отключенного, неготового или bot-only платежного провайдера.
- Анонимный `GET /api/public/orders/{id}/status` закрыт ответом `410 Gone` и больше не раскрывает `UserId`, тариф и сумму по одному GUID.
- Время email-релиза исправлено на фактическое время коммита; новый guard не допускает, чтобы статусные документы объявляли upcoming-релиз опубликованным.

### Проверено

- Backend full suite `1103/1103`; targeted checkout/order/payment/referral SQLite suite `55/55`.
- Frontend `90/90`, typecheck/build; Playwright desktop/mobile/all-screens responsive suite `16/16` без console errors и overflow на ширинах `305..1920`.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access и подтвердил latest release `2026-08-05-server-owned-checkout-context`.
- Roadmap guards фиксируют `532/552` closed, readiness `96.4%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными моками.

## 0.518.0 - 2026-08-05

Release entry: `2026-08-05-email-delivery-lifecycle`.

### Исправлено

- Pending email deliveries теперь обрабатываются отдельным worker-ом с conditional claim, stale lease recovery, exponential backoff и terminal failure после пяти попыток.
- Password reset outbox содержит одноразовый код только в формате `ISecretProtector`; SMTP-адаптер расшифровывает его непосредственно перед отправкой.
- Production startup fail-closed требует `Email:Mode=Smtp`, SMTP host/port/from и пароль при настроенном username.
- Admin «Аудит» показывает маскированную очередь без payload и позволяет `adminWrite` безопасно повторить только failed delivery.

### Проверено

- Backend full suite `1098/1098`; targeted email/auth/outbox/audit/startup suite `40/40` на SQLite.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; frontend `90/90`, typecheck/build OK.
- Playwright desktop/mobile/all-screens responsive suite `16/16` прошел без неожиданных console errors/overflow.
- EF migration `20260805092911_EmailNotificationDeliveryLifecycle` согласована с моделью; pending model changes отсутствуют.
- API/TelegramBot Release builds: `0` warnings, `0` errors; validation safety guard учитывает disabled email mode для Local/CI.
- Dependency audit `0 vulnerabilities`; secret scan `649` files/`0` findings; strict UTF-8 guard пройден.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `531/551` closed, readiness `96.4%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальный SMTP/VPS/staging smoke недоступен; локальные fake sender и browser mocks не считаются доказательством внешней доставки.

## 0.517.0 - 2026-08-05

Release entry: `2026-08-05-referral-reward-lifecycle`.

### Исправлено

- Регистрация принимает необязательный реферальный код, проверяет активного владельца и сохраняет связь пользователей атомарно с новым аккаунтом.
- Завершение новой подписки публикует durable `ReferralRewardRequested`; обработчик идемпотентно создаёт начисления с учетом тарифа, первой покупки, промокода, периода, суммы и канала.
- Пользовательский API больше не раскрывает source user, program id и metadata реферального начисления; SQLite-сортировка выполняется совместимо с `DateTimeOffset`.
- Admin API валидирует полную конфигурацию программы, а панель предоставляет отдельный адаптивный раздел правил и журнала начислений без ручного JSON.

### Проверено

- Backend full suite `1087/1087`; targeted auth/referral/outbox/subscription suite `34/34`, включая SQLite attribution, idempotency, promo/first-purchase restrictions, dead-letter и cabinet redaction.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; frontend `89/89`, typecheck/build OK.
- Playwright desktop/mobile/all-screens responsive suite `16/16` проверяет referral registration payload, cabinet reward и admin create flow без неожиданных console errors/overflow.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`.
- Secret scan `642` files/`0` findings и strict UTF-8 guard пройдены; временные SQLite/browser artifacts очищены.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `530/550` closed, readiness `96.4%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.516.0 - 2026-08-05

Release entry: `2026-08-05-promo-lifecycle-integrity`.

### Исправлено

- Checkout теперь отклоняет неизвестные, неактивные, преждевременные, истекшие и не разрешенные для тарифа или канала промокоды вместо молчаливого заказа без скидки.
- Лимиты общего и пользовательского использования сериализованы на relational БД; устаревшие pending-заказы освобождают слот, повтор того же intent остается идемпотентным, а конкурентный последний слот возвращает одному запросу HTTP `409`.
- Заказ сохраняет `PromoCodeId` и snapshot бесплатных дней; активация и продление используют оплаченный snapshot, а выход срока за диапазон даты завершается контролируемой ошибкой.
- Публичный сайт показывает русские сообщения ошибок промокода и позволяет исправить ввод без ухода со страницы тарифов.

### Проверено

- Backend full suite `1081/1081`; targeted promo/checkout/order/subscription suite `40/40`, включая deterministic file-backed SQLite race последнего redemption slot и fail-closed stale-expiration conflict.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; frontend `85/85`, typecheck/build OK.
- Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`.
- Secret scan `640` files/`0` findings и strict UTF-8 guard пройдены; временные SQLite/browser artifacts очищены.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `529/549` closed, readiness `96.4%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.515.0 - 2026-08-05

Release entry: `2026-08-05-checkout-claim-atomicity`.

### Исправлено

- Claim checkout-сессии резервирует владельца условным update, создаёт заказ и публикует связь `session -> order` в одной relational transaction.
- Конкурентный запрос того же пользователя возвращает winning order, а другой пользователь получает отказ без второго или orphan-order.
- Повторный claim уже связанной `completed`-сессии остаётся идемпотентным после истечения исходного токена и не переписывает статус на `expired`.

### Проверено

- Backend full suite `1065/1065`; targeted checkout/order/payment suite `21/21`, включая два deterministic file-backed SQLite claim races и rollback reservation при order failure.
- Fresh local SQLite smoke завершил checkout, sandbox payment, subscription и VPN access; frontend `84/84`, typecheck/build OK.
- Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`.
- Secret scan `639` files/`0` findings и strict UTF-8 guard пройдены; временные SQLite/browser artifacts очищены.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `528/548` closed, readiness `96.4%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остаётся `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.514.0 - 2026-08-05

Release entry: `2026-08-05-provisioning-cancel-claim-boundary`.

### Исправлено

- Cancel queued provisioning больше не может перезаписать `Prechecking`/`Deploying`, если другой worker claim-нул run после чтения API.
- Relational cancel использует conditional `status + UpdatedAt` update внутри transaction; node и audit фиксируются в том же commit, а проигранная гонка возвращает HTTP `409` без partial mutations и обновляет runs в admin UI.
- Concurrency version повышается монотонно при clock skew между инстансами; изменение схемы и миграция не потребовались.

### Проверено

- Backend full suite `1061/1061`; targeted provisioning/coordinator suite `40/40`, включая deterministic file-backed SQLite claim-before-cancel fault injection.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`.
- Secret scan `639` files, `0` findings; artifact cleanup и strict UTF-8 guard пройдены.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `527/547` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.513.0 - 2026-08-05

Release entry: `2026-08-05-provisioning-retry-atomic-queue`.

### Исправлено

- Provisioning retry больше не публикует промежуточный claimable `PrecheckQueued`/`DeployQueued` перед отдельным переходом в `Retrying`.
- Run, node, queue step, execution log и audit сохраняются согласованно одним commit сразу в статусе `Retrying`; worker другого инстанса не может обработать run между двумя API-коммитами.
- Coordinator продолжает атомарно claim-ить `Retrying` в `Prechecking` или `Deploying`; изменение схемы и миграция не потребовались.

### Проверено

- Backend full suite `1060/1060`; targeted provisioning/coordinator suite `39/39`, включая SQLite commit-count regression и sandbox worker retry E2E.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`.
- Secret scan `639` files, `0` findings; artifact cleanup и strict UTF-8 guard пройдены.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `526/546` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.512.0 - 2026-08-05

Release entry: `2026-08-05-provisioning-owner-actor-boundary`.

### Исправлено

- Admin queue, deploy и retry customer VPS больше не заменяют владельца run идентификатором оператора; `ProvisioningRun.RequestedByUserId` наследуется как customer owner.
- Queue/deploy/retry customer-owned node восстанавливают owner из канонического `requested-user-id` с fallback на раннюю owner-history, поэтому исправляется и ранее загрязненная run, а worker создает support context, subscription и VPN access для клиента.
- `provisioning.queue` сохраняет фактического администратора в `AuditLog.ActorId`, а owner отдельно в безопасном audit payload; изменение схемы и миграция не потребовались.

### Проверено

- Backend full suite `1060/1060`; targeted provisioning/worker suite `33/33`, включая SQLite queue/deploy/retry и sandbox worker ownership E2E.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`.
- Secret scan `639` files, `0` findings; artifact cleanup и strict UTF-8 guard пройдены.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `525/545` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.511.0 - 2026-08-05

Release entry: `2026-08-05-support-conversation-concurrency`.

### Исправлено

- `SupportConversation.Revision` стал optimistic concurrency token; cabinet/admin reply, status и note требуют ожидаемую ревизию и возвращают `409 Conflict` вместо перезаписи более нового изменения.
- Telegram и provisioning повышают ревизию существующего обращения; входящий ответ повторно открывает `pending`-диалог, не оставляя новое сообщение вне активной очереди.
- Назначить ответственным теперь можно только активного незаблокированного пользователя с `SupportWrite`; кабинет fail-closed скрывает сообщения с `Direction=internal`, даже если legacy-флаг `IsInternalNote` ошибочно не выставлен.
- API client и оба интерфейса передают ревизию, обновляют очередь после конфликта; PostgreSQL migration и idempotent local SQLite repair добавляют rollout-совместимую колонку.

### Проверено

- Backend full suite `1059/1059`; targeted support/Telegram/provisioning/SQLite suite `56/56`, включая stale status, assignment capability и pending reopen во всех provisioning writers.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Локальная SQLite база обновлена аддитивно; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `639` files, `0` findings.
- Roadmap status: `524/544` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.510.0 - 2026-08-05

Release entry: `2026-08-05-pending-order-intent-concurrency`.

### Исправлено

- `OrderService` теперь сам проверяет renewal subscription: обязательный ID, владелец, допустимый статус и совпадающий тариф нельзя обойти внутренним вызовом.
- Активный pending-заказ получает детерминированный intent key; частичный unique index не позволяет двум API-инстансам создать дубли одного оформления.
- Проигравший конкурентный запрос возвращает уже сохраненный заказ, а просроченный intent сначала переводится в `Expired` и не блокирует новое оформление.
- Legacy `POST /api/me/subscriptions/{id}/renew` больше не подтверждает случайный ID ложным `200`, а явно отвечает `410 Gone` и направляет на order flow.
- PostgreSQL migration и idempotent local SQLite repair добавляют nullable intent column и filtered unique index без переписывания исторических заказов.

### Проверено

- Backend full suite `1053/1053`; targeted order/cabinet/Telegram/SQLite suite `74/74`, включая service-level ownership/status/tariff matrix, stale replacement и deterministic concurrent winner.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `637` files, `0` findings.
- Roadmap status: `523/543` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.509.0 - 2026-08-05

Release entry: `2026-08-05-telegram-link-lifecycle-concurrency`.

### Исправлено

- Повторная выдача Telegram deep link теперь немедленно инвалидирует ранее выданную неиспользованную ссылку; действителен только token последней generation.
- Отдельный `TelegramLinkState` с optimistic `Revision` сериализует reissue, consume и unlink между API/TelegramBot-инстансами, не добавляя concurrency-конфликты в обычные операции профиля.
- `TelegramBotDeepLink.Revision` отклоняет stale consume, а filtered unique index `TelegramAccounts.UserId` не позволяет привязать к одному пользователю два Telegram ID.
- Unlink отзывает outstanding links даже для еще не завершенной привязки; контролируемый retry обрабатывает state insert/revision conflict без частичных строк.
- PostgreSQL migration отзывает legacy outstanding links и отвязывает исторические дубли с сохранением самой свежей записи; local SQLite schema repair выполняет тот же idempotent переход.

### Проверено

- Backend full suite `1048/1048`; targeted Telegram/cabinet/admin/SQLite suite `131/131`, lifecycle/schema-repair `22/22`, включая file-backed concurrent reissue и cross-context consume.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `635` files, `0` findings.
- Roadmap status: `522/542` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.508.0 - 2026-08-05

Release entry: `2026-08-05-refresh-token-rotation-concurrency`.

### Исправлено

- Один refresh token больше не может одновременно создать две активные дочерние сессии на разных API-инстансах.
- `UserRefreshToken.Revision` стал optimistic concurrency boundary; проигравшая ротация откатывает child token и обрабатывается как reuse без HTTP 500.
- Обнаруженный concurrent reuse отзывает всю выигравшую семью, поэтому ни одна из конкурирующих ветвей не остается пригодной для восстановления доступа.
- Logout повторно читает изменившуюся семью и закрывает child, созданный конкурентной ротацией; logout-all повышает `SessionVersion`.
- Admin deactivation повторяет локальную транзакцию после session conflict, а bootstrap и все пути отзыва повышают revision.
- Добавлены PostgreSQL migration и idempotent local SQLite repair для refresh-token revision.

### Проверено

- Backend full suite `1043/1043`; targeted auth/admin/bootstrap/SQLite/PostgreSQL suite `43/43`, включая три file-backed fault-injection regression и SQLite logout-all.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `633` files, `0` findings.
- Roadmap status: `521/541` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.507.0 - 2026-08-05

Release entry: `2026-08-05-password-reset-generation-boundary`.

### Исправлено

- Повторный запрос сброса пароля немедленно закрывает ранее выданный код с причиной `password_reset_reissued`; действителен только код последней generation.
- Отдельный `PasswordResetState` с optimistic `Revision` сериализует конкурентные выдачи между API-инстансами без добавления concurrency-конфликтов в обычные login/profile операции.
- Гонка stale reset против нового `forgot-password` завершается fail-closed: новый запрос сохраняется, старый код не меняет пароль и не помечается использованным.
- Явный admin bootstrap password reset повышает generation и инвалидирует outstanding reset tokens; изменение ролей/разблокировка без смены пароля их не закрывает.
- Добавлены PostgreSQL migration и idempotent local SQLite repair для state table и token generation.

### Проверено

- Backend full suite `1039/1039`; targeted auth/bootstrap/SQLite schema suite `31/31`, включая две file-backed cross-context гонки.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `631` files, `0` findings.
- Roadmap status: `520/540` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.506.0 - 2026-08-05

Release entry: `2026-08-05-registration-email-race-boundary`.

### Исправлено

- Два конкурентных запроса регистрации одного email больше не превращают проигравший запрос в HTTP 500 после успешного `AnyAsync`.
- Точный конфликт `IX_Users_Email` возвращает существующий контракт `email_exists`; атомарный SaveChanges не оставляет refresh session или audit проигравшего запроса.
- Другие `DbUpdateException` не маскируются как duplicate email и продолжают пробрасываться для корректной диагностики/HTTP 500.
- Новые referral codes увеличены с 24 до 64 бит случайной части без изменения существующих кодов.

### Проверено

- Backend full suite `1036/1036`; targeted auth/session/SQLite schema suite `24/24`, включая file-backed SQLite race и unrelated persistence failure.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `629` files, `0` findings.
- Roadmap status: `519/539` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.505.0 - 2026-08-05

Release entry: `2026-08-05-password-reset-token-lifecycle`.

### Исправлено

- Успешный password reset атомарно инвалидирует остальные outstanding tokens пользователя, поэтому старый код больше не может повторно заменить пароль.
- Consumed и invalidated tokens повышают `Revision`; конкурентный sibling commit получает controlled `invalid_or_expired_reset_token` вместо второго успешного reset.
- Lifecycle хранит `InvalidatedAt` и `InvalidationReason=password_reset_completed`, а security audit фиксирует число закрытых sibling tokens.
- Добавлены PostgreSQL migration и idempotent local SQLite schema repair для новых полей.
- Кабинет больше не запускает повторную загрузку восьми защищенных ресурсов после login/register/refresh; восстановленная при старте сессия и новая сессия имеют раздельные пути гидратации без замены интерактивных кнопок во время клика.

### Проверено

- Backend full suite `1034/1034`; targeted auth/session/SQLite schema suite `21/21`, включая cross-context concurrency regression.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `629` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `518/538` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.504.0 - 2026-08-05

Release entry: `2026-08-05-refresh-token-family-boundary`.

### Исправлено

- Replay revoked refresh-токена больше не отзывает все активные сессии пользователя: password-reset/logout token не может завершать новые независимые входы.
- Каждый login создаёт собственный `FamilyId`, rotation сохраняет его, а reuse detection отзывает только active descendants той же семьи и `session_version`.
- Legacy rotation rows с `FamilyId = NULL` безопасно связываются по `ReplacedByTokenHash`, получают общий family ID и защищены от циклической цепочки.
- Добавлены PostgreSQL migration, составной family index и idempotent local SQLite schema repair.

### Проверено

- Backend full suite `1032/1032`; targeted auth/password-reset/SQLite schema suite `19/19`.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `627` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `516/536` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.503.0 - 2026-08-05

Release entry: `2026-08-05-versioned-auth-sessions`.

### Исправлено

- JWT содержит `session_version`, а refresh row сохраняет поколение пользовательской сессии; active-user validator и rotation отклоняют отсутствующую, устаревшую или несовпадающую версию.
- Password reset, переход active-to-inactive, изменение ролей/пароля и восстановление администратора через bootstrap увеличивают версию и отзывают активные refresh-сессии, поэтому block/unblock и гонка rotation не возвращают старые полномочия.
- Public и cabinet немедленно очищают access/refresh storage и загруженные приватные данные после успешной смены пароля.
- Добавлены EF migration для PostgreSQL и idempotent repair колонок `SessionVersion` в локальной SQLite.

### Проверено

- Backend full suite `1030/1030`; targeted auth/admin/bootstrap/SQLite schema suite `32/32`.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `625` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `515/535` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.502.0 - 2026-08-05

Release entry: `2026-08-05-active-user-session-boundary`.

### Исправлено

- JWT bearer повторно проверяет `User.Status` и `IsBlocked` на каждом авторизованном запросе; admin user patch атомарно отзывает все активные refresh-сессии при блокировке, suspension или deletion.
- Cabinet централизованно обрабатывает 401/403: очищает access/refresh tokens, профиль, подписки, платежи, VPN URI/QR и support state и возвращает пользователя к форме входа.
- Linked Telegram account не читает ключи, не создаёт заказы/поддержку/provisioning и не проходит Telegram Stars pre-checkout после деактивации; уже полученный `successful_payment` продолжает безопасный settlement.

### Проверено

- Backend full suite `1027/1027`; targeted auth/admin/Telegram suite `48/48`, включая SQLite active-user validator и payment/access regressions.
- Frontend `84/84`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `622` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `514/534` closed, readiness `96.3%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.501.0 - 2026-08-05

Release entry: `2026-08-05-cancelled-subscription-cabinet-boundary`.

### Исправлено

- `/api/me/subscriptions` и `/api/me/accesses` теперь редактируют URI, provider ID, QR и config не только для `Revoked`, но и для stale credential с `Cancelled` parent; access DTO возвращает parent status и terminal marker.
- Оба пользовательских QR route сериализованы через subscription lifecycle gate и повторно читают статус после ожидания, поэтому конкурентная отмена не может выдать QR по stale URI.
- Cabinet helper и все повторяющиеся desktop/mobile представления подписок и доступов работают fail-closed, скрывают уже полученные secrets/QR и не отправляют terminal QR-запрос.

### Проверено

- Backend full suite `1021/1021`; targeted cabinet SQLite suite `13/13`, включая два lifecycle gate race regression.
- Frontend `83/83`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `619` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `513/533` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.500.0 - 2026-08-05

Release entry: `2026-08-05-cancelled-subscription-access-boundary`.

### Исправлено

- Все direct VPN access-команды теперь учитывают статус родительской подписки: stale `Active`/`Disabled` credential у `Cancelled` отклоняет enable, disable, sync и reset до provider call, history и audit.
- Direct endpoints сериализованы через subscription lifecycle gate; QR и migration повторно читают terminal-state после ожидания, поэтому одновременная отмена не обходится stale read.
- Admin access list и user overview редактируют URI, provider ID, QR и config, возвращают parent status/terminal marker; desktop/mobile UI показывает такую запись только как историю без ключей и команд.

### Проверено

- Backend full suite `1018/1018`; targeted access/admin/user SQLite suite `51/51`.
- Frontend `82/82`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `619` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `512/532` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.499.0 - 2026-08-05

Release entry: `2026-08-05-cancelled-subscription-terminal-guard`.

### Исправлено

- `Cancelled` подписка теперь является терминальной для административной синхронизации VPN-доступа: backend отклоняет команду до provider call даже при несогласованном legacy credential со статусом `Active`.
- Admin-panel использует единую fail-closed матрицу действий и показывает отменённую подписку только для просмотра и истории, без продления, активации, синхронизации, блокировки, отмены и поля количества дней.
- SQLite и frontend regressions фиксируют backend/UI контракт, а desktop/mobile Playwright проверяет отсутствие управляющих элементов у terminal record.

### Проверено

- Backend full suite `1012/1012`; targeted admin subscription SQLite suite `22/22`.
- Frontend `80/80`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `617` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `511/531` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.498.0 - 2026-08-05

Release entry: `2026-08-05-revoked-vpn-access-terminal-guard`.

### Исправлено

- `Revoked` VPN access теперь является терминальным во всех пользовательских API: URI, QR payload/path, config path и provider access ID редактируются, оба QR route возвращают controlled `400`.
- Admin QR и reset traffic отклоняют отозванный credential до генератора/provider call; кабинет и админка скрывают URI, copy/QR и provider-команды, а stale QR/current summary больше не восстанавливают отозванный секрет.
- Backend, frontend и browser regression используют SQLite и намеренно переданные revoked secrets, проверяя отсутствие утечки и сетевых команд.

### Проверено

- Backend full suite `1011/1011`; targeted lifecycle/cabinet/admin SQLite `23/23`.
- Frontend `77/77`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `615` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `510/530` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.497.0 - 2026-08-05

Release entry: `2026-08-05-vpn-access-cancellation-reconciliation`.

### Исправлено

- Enable, sync и reset traffic VPN-доступа больше не поглощают caller cancellation общим обработчиком ошибок: операция сохраняет безопасный diagnostic-state и повторно выбрасывает `OperationCanceledException`.
- Отмена enable/reset переводит `AccessCredential` в `SyncRequired`, а отмена read-only sync сохраняет исходный статус; все три пути создают отдельные history/audit события через независимый persistence token.
- Неопределенный результат reset traffic теперь действительно переводит доступ в `SyncRequired`, как обещает административный UI, и оставляет явный `provider_state_unknown` для ручной сверки.

### Проверено

- Backend full suite `1008/1008`; targeted access lifecycle SQLite `8/8`, включая cancellation enable/sync/reset и reset failure; расширенный X3Ui/admin/subscription suite `117/117`.
- Frontend `77/77`, typecheck/build OK; Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют.
- Dependency audit `0 vulnerabilities`, secret scan `615` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `509/529` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.496.0 - 2026-08-05

Release entry: `2026-08-05-admin-audit-domain-scope`.

### Исправлено

- `GET /api/admin/audit-logs` теперь применяет доменную область видимости до пользовательских фильтров: finance, support и Telegram-записи доступны только при `FinanceRead`, `SupportRead` и `BotManage` соответственно.
- Поиск по action, entity type и содержимому payload больше не позволяет partial role обойти ограничение и получить чужие `BeforeJson`/`AfterJson`.
- Admin-panel показывает только доступные роли категории аудита и не рекламирует Support финансовые события, а Finance — support-события.

### Проверено

- Backend full suite `1004/1004`; targeted audit SQLite role matrix `9/9` для Support, Finance, Operator, ReadOnly и Admin, включая прямую попытку обхода фильтрами.
- Frontend `77/77`, typecheck/build OK; Finance и Support desktop/mobile подтверждают видимость разрешенных записей и отсутствие finance/support/Telegram payload соседних доменов.
- Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow; fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `615` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `508/528` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.495.0 - 2026-08-05

Release entry: `2026-08-05-admin-dashboard-domain-redaction`.

### Исправлено

- Dashboard summary теперь вычисляет finance/support aggregates только при `FinanceRead`/`SupportRead`, поэтому partial roles не получают данные соседнего домена.
- Production readiness не запрашивает и не возвращает payment checks без `FinanceRead` и Telegram check без `BotManage`.
- Admin-panel скрывает финансовые tiles, последние заказы, недоступные attention items и readiness actions; Support получает отдельное инфраструктурное описание dashboard.
- Readiness action дополнительно не отображается, если target section отсутствует в capability-filtered navigation.

### Проверено

- Backend full suite `999/999`; targeted dashboard/automation/sandbox `29/29`, включая SQLite role matrix Support/Finance/Admin `3/3`.
- Frontend `77/77`, typecheck/build OK; Support desktop/mobile `2/2` подтверждает отсутствие finance metrics/orders/payment actions/API calls, видимую support queue и отсутствие overflow.
- Playwright desktop/mobile/all-screens responsive suite `16/16` без неожиданных console errors/overflow; fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `615` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `507/527` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.494.0 - 2026-08-05

Release entry: `2026-08-05-admin-capability-aware-ui`.

### Исправлено

- Добавлен защищенный `/api/admin/session` с backend-owned capability matrix для всех административных ролей и policies.
- Admin-panel показывает только разрешенные разделы, не загружает недоступные доменные API, скрывает запрещенные write controls и повторно проверяет capability в command handlers до network call.
- Роль только для чтения получает явный режим просмотра, а переход по недоступному hash безопасно ведет в первый разрешенный раздел.
- User overview больше не раскрывает finance-данные ролям без `FinanceRead` и support-диалоги ролям без `SupportRead`.

### Проверено

- Backend full suite `996/996`; targeted admin policy/session/user overview `50/50`, включая SQLite redaction для Finance и Support.
- Frontend `77/77`, typecheck/build OK; admin desktop/mobile `4/4` покрывает full и Finance роли, фильтрацию навигации/данных, отсутствие запрещенных запросов и read-only controls.
- Playwright desktop/mobile/all-screens responsive suite `14/14` без неожиданных console errors/overflow; fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел.
- API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `614` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `506/526` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.493.0 - 2026-08-05

Release entry: `2026-08-05-admin-rbac-admission`.

### Исправлено

- Admin-panel подтверждает защищенный `AdminRead` до сохранения токенов и показа shell; обычная пользовательская учетная запись остается на login screen, а выданная refresh-сессия отзывается.
- API client сохраняет HTTP status и нормализованный payload в `ApiClientError`, поэтому `401/403` отличаются от сетевой ошибки без разбора строк.
- Восстановление сохраненной admin-сессии корректно переживает React StrictMode remount и не оставляет форму навечно в состоянии «Проверяем доступ...».
- All-screens fixture соответствует числовому dashboard DTO и больше не маскирует `[object Object]` в статистике свежих платежей/заказов.

### Проверено

- Backend full suite `989/989`; targeted RBAC/auth suite `34/34` подтверждает запрет `AdminRead` для роли `User`, разрешенные admin-роли, refresh и revoke.
- Frontend `72/72`, typecheck/build OK; admin desktop/mobile `2/2` покрывает non-admin `403`, немедленный revoke, пустой storage и последующий полный admin flow.
- Playwright desktop/mobile/all-screens responsive suite `12/12` без неожиданных console errors/overflow и без `[object Object]` на dashboard.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `505/525` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё еще требуется.

## 0.492.0 - 2026-08-05

Release entry: `2026-08-05-admin-session-lifecycle`.

### Исправлено

- Admin-panel теперь сохраняет access/refresh пару и не теряет серверную сессию после входа или перезагрузки вкладки.
- Кнопка обновления сессии ротирует refresh token, сохраняет новую пару и повторно загружает административные данные.
- Logout отправляет bearer и актуальный refresh token backend, а локальные токены и данные панели очищаются даже при controlled `503` revoke failure.

### Проверено

- Backend full suite `989/989`; targeted auth session SQLite `1/1` подтверждает rotation, revoke и запрет refresh после logout.
- Frontend `71/71`, typecheck/build OK; admin desktop/mobile `2/2` покрывает token storage, rotation, logout payload, success cleanup и controlled `503` cleanup, Playwright desktop/mobile/all-screens responsive suite `12/12` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `504/524` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остаётся `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.491.0 - 2026-08-05

Release entry: `2026-08-05-cabinet-logout-failure-cleanup`.

### Исправлено

- Cabinet logout теперь всегда очищает access/refresh токены, профиль, подписки, платежи, VPN-доступы, support и Telegram state.
- Backend по-прежнему получает bearer и текущий refresh token для отзыва серверной сессии.
- При недоступном logout API локальный выход завершается, а пользователь видит предупреждение о неподтверждённом revoke и безопасное следующее действие.

### Проверено

- Backend full suite `989/989`; targeted auth session SQLite `1/1` подтверждает revoke и запрет refresh после logout.
- Frontend `71/71`, typecheck/build OK; cabinet desktop/mobile `2/2` покрывает logout payload, success cleanup и controlled `503` cleanup, Playwright desktop/mobile/all-screens responsive suite `12/12` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `503/523` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остаётся `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.490.0 - 2026-08-05

Release entry: `2026-08-05-public-session-lifecycle`.

### Исправлено

- Public login/register больше не теряет refresh token: access/refresh пара сохраняется в session storage и ротируется после отклонения access token.
- Кнопка выхода вызывает backend `/api/auth/logout` с bearer и текущим refresh token, поэтому серверная сессия действительно отзывается.
- Локальные токены очищаются даже при недоступном logout API; пользователь получает явное предупреждение о неподтверждённом server revoke.

### Проверено

- Backend full suite `989/989`; targeted auth session SQLite `1/1` подтверждает revoke и запрет refresh после logout.
- Frontend `71/71`, typecheck/build OK; public desktop/mobile `2/2` покрывает `401` rotation, успешный logout и controlled `503` cleanup, Playwright desktop/mobile/all-screens responsive suite `12/12` без неожиданных console errors/overflow.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `502/522` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остаётся `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.489.0 - 2026-08-05

Release entry: `2026-08-05-cabinet-expired-order-payment-guard`.

### Исправлено

- Кабинет больше не предлагает повторную оплату `Expired` заказа, которую backend гарантированно отклоняет.
- `PendingPayment` и `Failed` дополнительно проверяются по `expiresAt`: stale карточка получает эффективный статус `Expired` и понятное объяснение.
- Вместо нерабочей команды показан переход к новому заказу; handler независимо проверяет срок и не вызывает payment API при программном обходе UI.

### Проверено

- Backend full suite `989/989`; targeted payment initialization SQLite suite `8/8`, включая явный `Expired` и просроченный `PendingPayment` без provider call и payment row.
- Frontend `71/71`, typecheck/build OK; cabinet desktop/mobile `2/2`, Playwright desktop/mobile/all-screens responsive suite `12/12` без overflow/console errors.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `501/521` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остаётся `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.488.0 - 2026-08-05

Release entry: `2026-08-05-cabinet-qr-availability`.

### Исправлено

- Кабинет больше не предлагает заведомо нерабочую генерацию QR-кода для VPN-доступа, у которого ещё не выдан `accessUri`.
- Все карточки текущего доступа, подписок и выданных ключей используют единое правило готовности QR; disabled-состояние объясняет, что ссылка подключения ещё формируется.
- Handler повторно проверяет доступ перед запросом API, поэтому программный обход кнопки также не вызывает гарантированный backend `400`.

### Проверено

- Backend full suite `987/987`; targeted cabinet SQLite suite `9/9`, включая provisioning-доступ без URI и ответ `400`.
- Frontend `70/70`, typecheck/build OK; cabinet desktop/mobile `2/2`, Playwright desktop/mobile/all-screens responsive suite `12/12` без overflow/console errors.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошёл; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `500/520` closed, readiness `96.2%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остаётся `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence всё ещё требуется.

## 0.487.0 - 2026-08-05

Release entry: `2026-08-05-cabinet-renewal-status-guard`.

### Исправлено

- Кабинет больше не показывает заведомо нерабочую кнопку «Продлить» для `Blocked` и `Cancelled` подписок.
- Для заблокированной подписки отображается переход к поддержке, для отменённой — предложение оформить новый тариф; handler повторно блокирует программный вызов.
- Правило UI согласовано с backend-контрактом, который возвращает `400` и не создаёт заказ для этих статусов.

### Проверено

- Backend full suite `986/986`; targeted cabinet SQLite suite `8/8`, включая `Blocked/Cancelled` без создания заказа.
- Frontend `69/69`, typecheck/build OK; cabinet desktop/mobile `2/2`, Playwright desktop/mobile/all-screens responsive suite `12/12` без overflow/console errors.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; dependency audit `0 vulnerabilities`, secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `499/519` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.486.0 - 2026-08-05

Release entry: `2026-08-05-payment-provider-configuration-check`.

### Исправлено

- Проверка payment provider account больше не выдает локальную валидацию полей за live-проверку внешнего кабинета.
- API разделяет `ConfigurationStatus` и реальный `HealthStatus`: configuration-only проверка возвращает `Ready`/`NeedsConfiguration`, сохраняет health как `Unknown` и очищает прежние синтетические health-маркеры.
- Админка показывает «Проверить настройки» и явно сообщает, что внешний кабинет провайдера не запрашивался; публичный API не раскрывает синтетический health.

### Проверено

- Backend full suite `984/984`; targeted payment provider/public API suite `20/20`.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright desktop/mobile/all-screens responsive suite `12/12`; secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `498/518` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment-provider/3x-ui evidence все еще требуется.

## 0.485.0 - 2026-08-05

Release entry: `2026-08-05-payment-recheck-cancellation`.

### Исправлено

- Manual payment recheck больше не поглощает отмену запроса внутри provider status call.
- `OperationCanceledException` с отмененным caller token пробрасывается вызывающему коду; business и `NotSupported` ошибки по-прежнему возвращаются как управляемый failure.
- При отмененном recheck статус платежа, audit и outbox остаются неизменными.

### Проверено

- Backend full suite `984/984`; targeted audit/payment/concurrency suite `14/14`, cancellation regression `1/1`.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют; frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright desktop/mobile/all-screens responsive suite `12/12`; secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `497/517` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment/3x-ui evidence все еще требуется.

## 0.484.0 - 2026-08-05

Release entry: `2026-08-05-payment-checkout-account-selection`.

### Исправлено

- Публичный список способов оплаты и фактический checkout теперь выбирают один и тот же настроенный web-аккаунт провайдера.
- Неготовый default-аккаунт больше не перехватывает платеж после того, как UI показал готовый fallback; Telegram Stars invoice-flow остается отдельным bot-only путем.
- При отсутствии default выбор детерминирован по `CreatedAt` и `Id`, поэтому имя и режим в UI соответствуют аккаунту, записанному в платеж.

### Проверено

- Backend full suite `983/983`; targeted public/account/payment suite `26/26`, регрессия public/checkout selection `4/4`.
- Fresh local SQLite checkout с webhook, подпиской и VPN-доступом прошел; frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright desktop/mobile/all-screens responsive suite `12/12`; secret scan `610` files, `0` findings, UTF-8 guard `14/14`.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `496/516` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/live payment и production-like 3x-ui evidence остаются открытыми.

## 0.483.0 - 2026-08-05

Release entry: `2026-08-05-payment-provider-default-uniqueness`.

### Исправлено

- Параллельный create/update payment provider account сериализован по provider; readiness и enable/disable сериализованы по account.
- Partial unique index гарантирует не более одного `IsDefault=true` на провайдера между API-инстансами; migration детерминированно снимает старые дубли до создания индекса.
- Переключение default выполняется двухфазно в одной транзакции, duplicate name/default возвращает управляемый conflict вместо HTTP 500, а неуникальные persistence failures не маскируются.
- Валидация и защита новых секретов выполняются на prospective entity до изменения tracked account; смена типа провайдера остается поддержанной.

### Проверено

- Backend full suite `981/981`; targeted payment/account suite `56/56`, default-account concurrency/migration regression `7/7`, включая два независимых SQLite-контекста, migration SQL и failure classification; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; EF pending model changes отсутствуют, fresh local SQLite smoke latest release OK; secret scan `610` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `495/515` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.482.0 - 2026-08-05

Release entry: `2026-08-05-vpn-node-state-consistency`.

### Исправлено

- Server update/delete, health-check, provisioning, capacity reservation/release и режимы maintenance/allocation используют единый node-scoped gate; устаревший provider result и slot reservation не пересекаются с изменением или архивацией узла.
- Capacity VPN-сервера нельзя уменьшить ниже `UsedCapacity`; проверка выполняется до ротации секретов, audit и сохранения.
- Caller cancellation во время provider health-check пробрасывается без ложной `Unhealthy` history или audit.

### Проверено

- Backend full suite `974/974`; targeted server management `12/12`, server/provisioning/capacity suite `86/86`, включая file-backed SQLite concurrency, shared capacity gate и cancellation regression; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; EF model drift отсутствует, fresh local SQLite smoke latest release OK; secret scan `607` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `494/514` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.481.0 - 2026-08-05

Release entry: `2026-08-05-x3ui-panel-health-consistency`.

### Исправлено

- Update/delete панели, health-check, sync и inbound management используют единый panel-scoped gate; изменение credentials/status не пересекается с активной provider-проверкой.
- Transient local save failure после успешного remote health-check очищает pending EF state и сохраняет ровно один подтверждённый health result с redacted recovery audit.
- Ambiguous commit определяется по стабильному ID health record и не создаёт повторную историю или audit.
- Panel и inbound capacity нельзя уменьшить ниже `UsedCapacity`; проверка выполняется до audit и remote mutation.
- Параллельные health workers после ожидания отклоняют stale observation без второго provider call.

### Проверено

- Backend full suite `970/970`; X3Ui integration suite `72/72`, targeted X3Ui/panel/SQLite suite `78/78`, targeted health/capacity regression `5/5`, включая pre-commit/ambiguous-commit fault injection и file-backed SQLite concurrency; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; EF model drift отсутствует, fresh local SQLite smoke latest release OK; secret scan `607` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `493/513` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.480.0 - 2026-08-05

Release entry: `2026-08-05-x3ui-inbound-update-reconciliation`.

### Исправлено

- Create/edit/default и panel sync используют единый panel-scoped gate; конкурентные edit больше не могут зафиксировать локальное состояние в порядке, отличном от 3x-ui.
- Edit inbound компенсирует ambiguous provider timeout и ошибку локального commit обратным remote update с исходной конфигурацией.
- Неудачная reverse update создаёт redacted `vpn_inbound.update.compensation_failed` audit с явным `reconciliationRequired`, не маскируя неопределённое состояние провайдера.
- Одновременный sync ожидает защищённую секцию и затем отклоняется как stale без повторного remote вызова.

### Проверено

- Backend full suite `965/965`; X3Ui integration suite `67/67`, targeted X3Ui/panel/SQLite suite `73/73`, inbound update regression `4/4`, включая file-backed SQLite concurrency, ambiguous timeout, local-save fault injection и compensation failure; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; EF model drift отсутствует, fresh local SQLite smoke latest release OK; secret scan `607` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `492/512` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.479.0 - 2026-08-05

Release entry: `2026-08-05-x3ui-client-state-reconciliation`.

### Исправлено

- Ручные enable/disable клиента 3x-ui сериализованы по подписке и компенсируют ambiguous remote update или сбой локального сохранения обратным provider update.
- Неудачная компенсация сохраняет `client-state-compensation-failed`, переводит связанный VPN-доступ в `SyncRequired` и создаёт redacted audit для ручной reconciliation.
- Необратимый reset traffic при timeout, cancellation или local save failure сохраняет `traffic-reset-uncertain`; admin и provider paths больше не теряют факт возможной remote-мутации.
- Админка перечитывает состояние после ошибки, показывает badge `SyncRequired` и явно предупреждает о необратимом reset traffic; desktop/mobile dialog проверен без overflow.

### Проверено

- Backend full suite `961/961`; X3Ui integration suite `63/63`, targeted client-state/reset regression `7/7`, включая ambiguous update, cancellation, compensation failure и local-save fault injection; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; EF model drift отсутствует, fresh local SQLite smoke latest release OK; secret scan `607` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `491/511` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.478.0 - 2026-08-04

Release entry: `2026-08-04-x3ui-client-migration-atomicity`.

### Исправлено

- Ручной перенос 3x-ui клиента атомарно резервирует временные slots target panel и inbound до remote add; заполненная или недоступная цель отклоняется без внешних изменений.
- Параллельные переносы через независимые SQLite-контексты не могут занять один последний slot: проигравший запрос завершается до remote call.
- Ошибка target add, source delete, cancellation или local save выполняет обратный remote move и освобождает reservation; неопределённая компенсация сохраняет capacity и `migration-compensation-failed` для ручной reconciliation.
- Админка скрывает заполненные цели, показывает occupancy в selector и подтверждает add-before-delete; desktop/mobile flow проверен без overflow.

### Проверено

- Backend full suite `954/954`; X3Ui integration suite `56/56`, targeted migration regression `9/9`, включая file-backed SQLite concurrency, ambiguous side effects и local-save fault injection; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; EF model drift отсутствует, fresh local SQLite smoke latest release OK; secret scan `607` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `490/510` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.477.0 - 2026-08-04

Release entry: `2026-08-04-terminal-subscription-cancellation`.

### Исправлено

- Ручная отмена подписки теперь завершает VPN lifecycle: доступ переводится в `Revoked`, provider-клиент удаляется, ссылки подписки очищаются, а ёмкость node/panel/inbound освобождается в одной локальной транзакции.
- Ошибка provider delete или локального commit откатывает подписку и все capacity counters; после фактической попытки удаления доступ получает `SyncRequired` и audit/history для reconciliation.
- Отмена запроса до обращения к провайдеру больше не создаёт ложный `SyncRequired`; повторная отмена не удаляет клиента и не освобождает slot второй раз.
- Админка явно предупреждает о необратимом отзыве и удалении доступа; destructive-flow проверен на desktop и mobile без overflow.

### Проверено

- Backend full suite `948/948`; targeted subscription cancellation/X3Ui/SQLite regression `23/23`, включая real provider adapter, local-save fault injection и cancellation boundaries; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; EF model drift отсутствует, fresh local SQLite smoke latest release OK; secret scan `607` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `489/509` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.476.0 - 2026-08-04

Release entry: `2026-08-04-vpn-node-capacity-reservation`.

### Исправлено

- Последний свободный слот `VpnNode` резервируется атомарным условным update до обращения к VPN-провайдеру; два параллельных заказа больше не могут одновременно занять одну ёмкость, а `Math.Min` не скрывает oversubscription.
- Ошибка или cancellation выдачи освобождает резерв ноды; ошибка компенсации возвращается как явное требование ручной reconciliation.
- Продление существующего доступа на `Maintenance/Draining` ноде сохраняет назначение и capacity. Отсутствующая или недопустимая sandbox-привязка не заменяется скрыто и требует явной миграции.

### Проверено

- Backend full suite `942/942`; целевой VPN/payment/SQLite regression `100/100`, включая два независимых file-backed SQLite контекста на последнем слоте и fault/cancellation cleanup; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; fresh local SQLite smoke latest release OK; secret scan `607` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `488/508` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.475.0 - 2026-08-04

Release entry: `2026-08-04-vpn-access-provisioning-consistency`.

### Исправлено

- Создание VPN-клиента сериализовано по подписке, а optimistic concurrency счётчиков panel/inbound не допускает локальный oversubscription последнего слота; проигравший remote create компенсируется delete.
- Продление обновляет клиента на уже назначенных panel/inbound даже при заполненной ёмкости и больше не создаёт скрытую копию на другой панели; смена протокола требует явной миграции.
- Удаление доступа освобождает ёмкость panel/inbound. При сбое локального commit после remote update/delete/enable/disable выполняется обратная операция с исходными параметрами; неоднозначный timeout после add также очищается.
- Выбор panel/inbound стал совместим с SQLite: SQL-сортировка больше не использует неподдерживаемые `decimal` и `DateTimeOffset` выражения.

### Проверено

- Backend full suite `939/939`; X3Ui suite `48/48`, включая concurrent SQLite, oversubscription и fault-injection; migration `20260804155901_VpnCapacityConcurrency`, PostgreSQL history SQL и EF model snapshot синхронизированы; API/TelegramBot Release builds без предупреждений.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`; fresh local SQLite smoke latest release OK; secret scan `605` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `487/507` closed, readiness `96.1%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.474.0 - 2026-08-04

Release entry: `2026-08-04-panel-sync-recovery`.

### Исправлено

- `PanelSyncRun` стал durable claim: частичный unique index не допускает два `Running` для одной 3x-ui панели в разных API-инстансах.
- Worker передаёт ожидаемый `LastSyncAt`, поэтому устаревший snapshot не запускает повтор сразу после завершения более свежей синхронизации; зависший run восстанавливается по пятиминутной lease.
- Ошибки расшифровки panel secret и preflight теперь создают `Unhealthy` health history, timestamp и audit вместо потери диагностики до `try`.
- Новые health/sync ошибки redacted и ограничены по длине; upgrade очищает потенциально чувствительные raw errors из старых panel, health и sync записей.
- Админка показывает время последней health-проверки и безопасную ошибку панели, а failed sync больше не скрывается пустым `summaryJson`.

### Проверено

- Backend full suite `930/930`; targeted X3Ui/panel/SQLite suite `52/52`; SQLite concurrent/stale/preflight fault-injection regression, migration `20260804150807_PanelSyncRecovery`, PostgreSQL SQL и EF model drift check OK.
- Frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console/responsive suite `12/12`, fresh local SQLite smoke latest release OK, secret scan: `603` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `486/506` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.473.0 - 2026-08-04

Release entry: `2026-08-04-subscription-lifecycle-recovery`.

### Исправлено

- Истечение подписки больше не подтверждается до успешного отключения VPN-доступа: ошибка провайдера оставляет `GracePeriod`, сохраняет redacted diagnostic, lease и backoff для восстановления после рестарта.
- Конкурентные lifecycle workers используют conditional claim и не отключают один доступ дважды; stale lease восстанавливается, а ручные admin-команды сериализованы с фоновым lifecycle.
- Cancellation отключения фиксирует `SyncRequired` и audit/history до повторного выброса исключения; успешные активация и продление очищают retry-state.
- Subscription lifecycle, order expiry, panel health и panel sync обрабатываются изолированно по batch/item, поэтому сбой одной записи не останавливает остальные.
- Админка показывает lifecycle error, номер попытки и время следующего повтора; локальная SQLite repair и EF migration добавляют durable lifecycle-поля.

### Проверено

- Backend full suite `926/926`; targeted lifecycle/panel/SQLite suite `36/36`; EF migration `20260804143027_SubscriptionLifecycleRecovery` без model drift; fresh local SQLite smoke OK; frontend `68/68`, typecheck/build OK; Playwright console suite `12/12`; ручной local SQLite admin smoke на desktop и `390x844` без overflow/console errors; secret scan: `600` files, `0` findings.
- `RoadmapCurrentStateTests` и release/documentation guards фиксируют `485/505` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Статус остается `staging-ready baseline`, not production-ready: live email, real VPS/staging/payment и production-like 3x-ui evidence остаются открытыми.

## 0.472.0 - 2026-08-04

Release entry: `2026-08-04-provisioning-worker-recovery`.

### Fixed
- `ProvisioningRunCoordinator` атомарно захватывает queued run, ведёт attempt/lease и не допускает двойной deploy несколькими worker instances.
- Просроченный `Prechecking`/`Deploying` переводится в redacted failure для явной проверки оператором без автоматического replay потенциально частичного внешнего deploy.
- Для одного node действует process gate и partial unique index; migration и local SQLite repair карантинируют исторические активные дубли до создания индекса.
- Ansible runner читает stdout/stderr параллельно, ограничен `ExecutionTimeoutSeconds`, завершает process tree при timeout/cancellation и всегда удаляет временный `extra-vars.json`.
- Админский API/UI показывает attempt/lease и разрешает retry/cancel только для безопасных статусов; исполняющийся run нельзя локально отменить поверх внешнего процесса.

### Notes
- Validation: backend full suite `918/918`, targeted provisioning/state/SQLite suite `98/98`, migration `20260804134818_ProvisioningWorkerRecovery` and PostgreSQL SQL OK, API and TelegramBot Release builds `0` warnings/`0` errors, fresh local SQLite smoke OK, frontend `68/68`, typecheck/build OK, dependency audit `0 vulnerabilities`, Playwright console suite `12/12`, responsive all-screens `6/6`, secret scan `588` files/`0` findings.
- RoadmapCurrentStateTests and release/documentation guards keep progress at `484/504` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked.
- The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.471.0 - 2026-08-04

Release entry: `2026-08-04-outbox-dispatch-recovery`.

### Fixed
- `OutboxDispatcherWorker` больше не подтверждает события без handler: application service выполняет atomic claim, минутную lease, retry с backoff, redacted error и terminal dead-letter после десяти попыток.
- Enqueue защищён unique identity `(Type, CorrelationId)` и транзакционным PostgreSQL/SQLite upsert; повтор failed-события переактивируется, а повторный password reset и смена payment status получают отдельные correlation ID.
- `NotificationRequested` и password reset материализуются в pending email delivery, malformed/unsupported payload завершается fail-closed; payment/order internal events проходят обязательную schema validation.
- EF migration и local SQLite repair добавляют lifecycle-поля, нормализуют исторические дубли до unique index; readiness health разделяет pending и failed outbox.

### Notes
- Validation: backend full suite `901/901`, targeted outbox/auth/payment/SQLite/observability suite `37/37`, EF model drift and PostgreSQL migration SQL OK, API and TelegramBot Release builds `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `581` files, `0` findings; encoding guard and artifact cleanup: OK.
- RoadmapCurrentStateTests and release/documentation guards keep progress at `483/503` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; live email and real VPS/staging/live payment/production-like 3x-ui evidence remain open.

## 0.470.0 - 2026-08-04

Release entry: `2026-08-04-telegram-notification-enqueue-deduplication`.

### Fixed
- Все producer-пути `TelegramBotNotification` получают стабильный `DeduplicationKey`; одинаковые user/type/payload больше не создают несколько активных записей при параллельном `AnyAsync` -> `Add`.
- `ApplicationDbContext` сохраняет notification через атомарный PostgreSQL/SQLite upsert в одной транзакции с бизнес-изменениями: существующие `sent/pending/sending` не дублируются, а `failed/cancelled` безопасно переактивируются.
- EF migration нормализует исторические записи до unique index и отменяет активные дубли; local SQLite repair выполняет тот же backfill для баз, созданных через `EnsureCreated`.
- При ошибке business save транзакция откатывает notification, а EF tracker сохраняет её в `Added` для корректного повторного commit.

### Notes
- Validation: backend full suite `881/881`, targeted Telegram persistence/delivery/SQLite repair suite `23/23`, PostgreSQL migration SQL OK, API and TelegramBot Release builds `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `575` files, `0` findings; encoding guard and artifact cleanup: OK.
- Roadmap progress is `482/502` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.469.0 - 2026-08-04

Release entry: `2026-08-04-telegram-notification-dispatch-recovery`.

### Fixed
- `TelegramBotNotification` теперь захватывается атомарным conditional DB update; два dispatcher-экземпляра не отправляют одно уведомление параллельно.
- Зависший `sending` восстанавливается после минутной lease, transient failure и cancellation сохраняют redacted retry state с backoff, а пятая ошибка переводит запись в terminal `failed`.
- Заблокированный Telegram account переводит notification в `cancelled` без внешнего вызова; malformed JSON/reply markup завершается fail-closed, legacy plain-text payload остается поддержан.
- Hosted dispatcher стал тонким worker над testable application service и больше не управляет статусами неатомарно внутри выбранного списка.

### Notes
- Validation: backend full suite `873/873`, targeted Telegram notification suite `17/17`, API and TelegramBot Release builds `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `571` files, `0` findings; encoding guard and artifact cleanup: OK.
- Roadmap progress is `481/501` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.468.0 - 2026-08-04

Release entry: `2026-08-04-telegram-response-delivery-recovery`.

### Fixed
- Ответ Telegram и pre-checkout acknowledgement сохраняются в `TelegramBotUpdate` вместе с результатом обработки, поэтому повторный webhook доставляет pending response без повторной маршрутизации команды.
- Отдельная delivery lease, conditional DB claim и exponential backoff исключают параллельную отправку; partial progress не отвечает на уже подтвержденный pre-checkout повторно после сбоя следующего сообщения.
- Long-polling восстанавливает due deliveries из БД перед чтением новых updates, а cancellation освобождает lease и независимо сохраняет retry state.
- Telegram HTTP clients теперь выбрасывают retryable ошибку при missing BotToken и non-2xx `sendMessage`/`answerPreCheckoutQuery` вместо ложной фиксации успешной доставки.

### Notes
- Validation: backend full suite `860/860`, targeted Telegram delivery suite `17/17`, EF migration list and generated PostgreSQL SQL OK, API and TelegramBot Release builds `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `569` files, `0` findings; encoding guard and artifact cleanup: OK.
- Roadmap progress is `480/500` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.467.0 - 2026-08-04

Release entry: `2026-08-04-telegram-update-recovery`.

### Fixed
- Telegram `update_id` резервируется в БД до маршрутизации: одинаковые concurrent delivery больше не вызывают invoice/provisioning или другие side effects дважды.
- Fresh незавершенный update защищен десятиминутным lease и возвращает retryable 503; failed и stale updates захватываются повторно условным DB update.
- Caller cancellation независимо сохраняет unprocessed retry marker с redacted error и пробрасывается вызывающему коду.
- Long-polling не сдвигает offset при retryable update result, поэтому временно занятый update не теряется.

### Notes
- Validation: backend full suite `850/850`, targeted Telegram suite `69/69`, API and TelegramBot Release builds `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `564` files, `0` findings; encoding guard and artifact cleanup: OK.
- Roadmap progress is `479/499` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.466.0 - 2026-08-04

Release entry: `2026-08-04-subscription-activation-compensation`.

### Fixed
- Если VPN-провайдер создал доступ, а локальное сохранение credential завершилось ошибкой, `SubscriptionService` удаляет remote access до фиксации retry-состояния.
- Если remote cleanup тоже завершился ошибкой, provider ID сохраняется как `SyncRequired`: повтор использует update существующего доступа и не создает второй remote client.
- Caller cancellation после remote create выполняет компенсацию, независимо сохраняет `PendingActivation`/`PartiallyProcessed` и audit, затем пробрасывается вызывающему коду.
- Provisioning failures возвращают retryable result; повтор renewal по тому же `LastPaymentId` не увеличивает срок и `RenewalCount` второй раз.

### Notes
- Validation: backend full suite `843/843`, targeted subscription/X3Ui suite `48/48`, API Release build `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `564` files, `0` findings; encoding guard and artifact cleanup: OK.
- Roadmap progress is `478/498` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.465.0 - 2026-08-04

Release entry: `2026-08-04-payment-webhook-recovery`.

### Fixed
- Payment webhook events теперь различают terminal `Processed/Rejected` и retryable `Failed`; зависшие `Received/Verified` атомарно перехватываются после десятиминутного lease.
- Не найденный пока payment attempt, сбой verifier, activation failure и незавершенный commit возвращают HTTP 503, а invalid signature/amount/account и запрещенный status transition остаются HTTP 400.
- Повторный malformed payload больше не падает на unique constraint, а параллельные одинаковые события сериализуются без двойной активации.
- Повтор после VPN provisioning failure продолжает подписку с тем же `LastPaymentId` без второй подписки или повторного продления; завершенный заказ восстанавливает потерянный activation marker.

### Notes
- Validation: backend full suite `839/839`, targeted payment webhook suite `63/63`, API Release build `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `51/51` keep current evidence synchronized.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `563` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `477/497` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.464.0 - 2026-08-04

Release entry: `2026-08-04-payment-init-commit-resilience`.

### Fixed
- Payment initialization сериализуется по order id: одинаковые параллельные запросы используют один local payment и один provider checkout.
- Local payment reservation сохраняется до provider call; конфликт или отказ reservation возвращает управляемую fail-closed ошибку без внешнего запроса.
- Успешный remote checkout после transient final commit failure сохраняется независимым token и возвращается клиенту как success; двойной save failure оставляет retriable `New` reservation с тем же idempotency key.
- Заказы `FulfillmentInProgress`, `PartiallyProcessed` и любые заказы с `PaidAt` больше не создают повторный checkout.

### Notes
- Validation: backend full suite `831/831`, targeted payment/refund suite `154/154`, API Release build `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `51/51` keep current evidence synchronized.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `562` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `476/496` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.463.0 - 2026-08-04

Release entry: `2026-08-04-payment-refund-commit-resilience`.

### Fixed
- Refund создаёт durable `New` reservation до вызова платежного провайдера, поэтому параллельный одинаковый запрос не выполняет возврат дважды.
- Перед возвратом повторно читаются актуальные payment/refund state и доступная сумма под order gate; `New`, `Pending` и `Unknown` операции блокируют следующий возврат до сверки с провайдером.
- Если provider call отменён или финальный local commit не состоялся, reservation сохраняется как `Unknown` независимым cancellation token, а сумма и статус платежа возвращаются к подтверждённому состоянию.
- Payment processing gates освобождаются после последнего участника и больше не накапливаются по каждому обработанному заказу.

### Notes
- Validation: backend full suite `824/824`, targeted payment/refund suite `147/147`, API Release build `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `51/51` keep current evidence synchronized.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `561` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `475/495` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.462.0 - 2026-08-04

Release entry: `2026-08-04-x3ui-remote-create-compensation`.

### Fixed
- Admin create inbound удаляет уже созданный remote inbound, если локальный commit завершается ошибкой, и сохраняет redacted audit результата компенсации.
- Production provider компенсирует auto-created inbound при локальном save failure до создания клиента.
- Новая VPN-выдача удаляет remote client при ошибке локального сохранения или подготовки notification, очищает pending local entity и восстанавливает capacity.
- Двойной отказ local save и provider cleanup возвращает явную ошибку о необходимости ручной очистки вместо скрытого orphan state.

### Notes
- Validation: backend full suite `817/817`, targeted 3x-ui suite `49/49`, API Release build `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `51/51` keep current evidence synchronized.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `560` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `474/494` closed, readiness `96.0%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.461.0 - 2026-08-04

Release entry: `2026-08-04-x3ui-sync-atomicity-hardening`.

### Fixed
- Panel sync теперь восстанавливает измененные inbound-ы и удаляет добавленные inbound-ы, если операция отменена или завершилась исключением до финального сохранения.
- Частичные sync events и предварительный success audit текущего run не сохраняются вместе с `Failed`; прежний `LastSyncAt` остается неизменным.
- Failure/cancellation finalization использует независимый cancellation token и сохраняет только диагностический статус и redacted audit отказа.

### Notes
- Validation: backend full suite `811/811`, targeted 3x-ui suite `43/43`, API Release build `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `51/51` keep current evidence synchronized.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `560` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `473/493` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.460.0 - 2026-08-04

Release entry: `2026-08-04-x3ui-failure-compensation-hardening`.

### Fixed
- Создание и обновление 3x-ui панели теперь отклоняет невалидные HTTP(S) URL, пароль, capacity, SSL/API/status enum и JSON template до изменения EF-сущности или записи аудита.
- Отмена синхронизации панели завершает sync-run статусом `Failed`, фиксирует `FinishedAt` и redacted audit, после чего пробрасывает исходную отмену вызывающей стороне.
- При ошибке удаления клиента из source inbound миграция удаляет уже созданную target-копию и сохраняет локальную привязку; неудачная компенсация получает отдельный audit и явное требование ручной provider cleanup.
- HTTP health/retry слой больше не преобразует caller cancellation в обычный unhealthy result и освобождает промежуточные `5xx` responses перед повтором.

### Notes
- Validation: backend full suite `809/809`, targeted 3x-ui suite `41/41`, API Release build `0` warnings and `0` errors, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `51/51` keep current evidence synchronized.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `560` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `472/492` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.459.0 - 2026-08-04

Release entry: `2026-08-04-admin-operation-audit-integrity`.

### Fixed
- Administrative writes for users, FAQ, site content, work scenarios, app releases, referral programs, support and Telegram settings now create redacted audit records with actor and entity metadata.
- 3x-ui panel, inbound and client commands now preserve the initiating admin identity in the audit trail; background health/sync calls remain explicitly attributed to the system actor.
- App release item replacement now uses one EF Core save instead of a separate bulk delete followed by insert, preventing a failed update from leaving an empty release.
- Rejected FAQ and site-content duplicate updates no longer mutate tracked entities, and a misconfigured 3x-ui sync closes its run as `Failed` instead of leaving it `Running`.

### Security
- Audit snapshots exclude Telegram tokens, panel passwords, support message text, VPN config URIs and client UUIDs; all serialized snapshots pass through the shared sensitive-data redactor.

### Notes
- Validation: backend full suite `798/798`, targeted SQLite/controller/service suite `73/73`, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite keep current evidence synchronized.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `560` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `471/491` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.458.0 - 2026-08-04

Release entry: `2026-08-04-subscription-node-integrity-hardening`.

### Fixed
- Admin subscription `extend`, `activate`, `block`, `unblock` and `cancel` commands now apply subscription status/date changes only after the corresponding VPN enable/disable succeeds; provider failures preserve the original subscription and record the access failure.
- Unblocking at the exact subscription end time now results in `Expired` without re-enabling VPN access, and commands fail closed when the access lifecycle service is unavailable.
- Deleting a VPN server now archives nodes linked by health checks or source/target migration jobs, preventing orphaned operational history; API and admin UI expose both counters.
- Manual migration rejects unhealthy or capacity-exhausted target nodes, matching automatic allocation rules.
- A linked work scenario key cannot be renamed until tariffs are moved, and rejected updates no longer mutate the tracked entity.

### Improved
- Admin browser E2E performs server deletion through the confirmation panel on desktop and mobile, verifies all linked-history counters and checks horizontal overflow.

### Notes
- Validation: backend full suite `797/797`, targeted SQLite/controller suite `38/38`, frontend `66/66`, Playwright console suite `12/12`, targeted admin desktop/mobile `2/2`, responsive all-screens `6/6`, fresh local SQLite smoke OK, Release build/typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `51/51` keep current evidence synchronized.
- Frontend dependency audit: `0 vulnerabilities`; secret scan: `559` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `470/490` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.457.0 - 2026-08-04

Release entry: `2026-08-04-migration-node-and-frontend-hardening`.

### Fixed
- Subscription migration now validates the source subscription/server, target readiness and allocation, rejects the current server and duplicate planned/running jobs, and persists a complete migration item plus audit record.
- Archived VPN servers can no longer be returned to maintenance/ready/allocation states or disabled again through admin operations; matching UI actions are disabled.
- Mobile admin navigation no longer places the full 16-item tablist before page content; the compact section selector remains available and E2E uses it on narrow viewports.
- Admin section counters and previous/next navigation now follow the same grouped order shown in the sidebar.

### Security
- React and React DOM were upgraded to `19.2.8`, React Router to `8.3.0`, TypeScript uses bundler resolution, and the frontend enforces Node.js `>=22.22.0`.
- `npm audit --audit-level=moderate` reports `0 vulnerabilities`; the two previously documented React Router advisories are removed.

### Notes
- Validation: backend full suite `778/778`, targeted SQLite boundaries `3/3`, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK on Node.js 22.22.0.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `51/51` keep current evidence synchronized.
- Secret scan: `559` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `469/489` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.456.0 - 2026-08-04

Release entry: `2026-08-04-operation-boundary-quality-audit`.

### Fixed
- Public checkout, cabinet payment initialization and admin user/tariff/referral updates now return 400 for malformed or undefined enum/JSON values instead of throwing or partially mutating tracked entities.
- VPN provisioning remains fail-closed when a production node or active inbound is unavailable; no local client, remote client call or inbound capacity change is left behind.
- Payment webhook dispatch now depends on `IPaymentWebhookProcessor`, and controller tests cover routing and response contracts for all 8 providers.

### Improved
- All public, cabinet and 16 admin screens now pass a browser quality gate for `main` landmarks, duplicate IDs, image alt text and accessible control names.
- Direct controller coverage was added for checkout sessions, payment webhooks, VPN panels, release history, unsupported channels and administrative writes.
- A diagnostic coverage run measured 51.9% line, 51.8% branch and 79.9% controller line coverage; it is a local gap-finding metric, not production evidence.

### Notes
- Validation: backend full suite `775/775`, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK.
- `RoadmapCurrentStateTests`, `FinalDocsChangelogTests` and the targeted docs/release/encoding suite `48/48` keep current evidence synchronized.
- Secret scan: `558` files, `0` findings; artifact cleanup: OK.
- Roadmap progress is `468/488` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready; real VPS/staging/live payment/production-like 3x-ui evidence remains open.

## 0.455.0 - 2026-08-04

Release entry: `2026-08-04-full-project-quality-audit`.

### Fixed
- Admin operations now use the registered `IClock`, removing date-dependent subscription activation failures and keeping timestamps deterministic in SQLite tests.
- The public site no longer creates horizontal scrolling at a 320 px browser window, including the 305 px content width left by a classic vertical scrollbar.
- The admin login metric now derives its section count from `adminSections`; the UI correctly reports 16 sections.
- The UTF-8 guard ignores generated `data`, coverage and Playwright report directories, so concurrent artifacts cannot race source validation.
- `postcss` was updated to `8.5.25`, removing the high-severity audit finding.

### Improved
- All-screens Playwright coverage now checks 5 public routes, the authenticated cabinet and all 16 admin sections at 305, 320, 360, 390, 768, 1024, 1440 and 1920 px.
- The responsive gate checks document overflow, clipped controls, blank screens and browser errors.
- A real local browser/SQLite pass confirmed registration, public login, sandbox checkout, order visibility in the cabinet and all admin sections.

### Notes
- Validation: backend `737/737`, frontend `66/66`, Playwright console suite `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, typecheck/build OK.
- `npm audit --audit-level=high` passes; 2 moderate React Router advisories remain. The affected SSR/RSC paths and user-controlled navigation are not used by these SPA clients.
- Roadmap progress is `467/487` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.

## 0.454.0 - 2026-07-02

Release entry: `2026-07-02-agent-external-evidence-boundary-guard`.

### Fixed
- `AGENTS.md` now contains an External Evidence Boundaries section for real VPS, staging, live payment, production-like VPN, 3x-ui/x-ui and provider-cabinet roadmap items.
- `DocumentationEncodingTests` now verifies that local tests, mocks, dry-run and SQLite smoke cannot close external-evidence items by themselves.
- `P11-ACC-176` documents the external evidence boundary guard while production proof remains required.

### Notes
- Roadmap progress is now `466/486` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 48/48, backend full suite `737/737`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.453.0 - 2026-07-02

Release entry: `2026-07-02-agent-end-to-end-completion-guard`.

### Fixed
- `AGENTS.md` now contains an End-To-End Task Completion section for implementation requests.
- `DocumentationEncodingTests` now verifies that agent tasks must cover analysis, code, tests, local DB/SQLite, encoding, docs, What's New, cleanup, final `git status` and commit.
- `P11-ACC-175` documents the end-to-end completion guard while production proof remains required.

### Notes
- Roadmap progress is now `465/485` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 47/47, backend full suite `736/736`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.452.0 - 2026-07-02

Release entry: `2026-07-02-agent-customer-chat-image-comment-guard`.

### Fixed
- `AGENTS.md` now explicitly requires using customer chat comments above attached images together with OCR and visual text.
- `DocumentationEncodingTests` now verifies that direct customer clarification has priority over ambiguous image interpretation.
- `P11-ACC-174` documents the customer chat image comment guard while production proof remains required.

### Notes
- Roadmap progress is now `464/484` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 46/46, backend full suite `735/735`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.451.0 - 2026-07-02

Release entry: `2026-07-02-agent-encoding-verification-guard`.

### Fixed
- `AGENTS.md` now contains an Encoding Verification section for changed markdown, JSON, C#, TypeScript, JavaScript, CSS, HTML and config files.
- `DocumentationEncodingTests` now verifies strict UTF-8 without BOM, mojibake marker checks and encoding guard requirements for Russian docs/status text and the release seed.
- `P11-ACC-173` documents the agent encoding verification guard while production proof remains required.

### Notes
- Roadmap progress is now `463/483` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 45/45, backend full suite `734/734`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.450.0 - 2026-07-02

Release entry: `2026-07-02-agent-verification-handoff-guard`.

### Fixed
- `AGENTS.md` now contains a Verification And Release Handoff section that requires checks, local DB/SQLite validation, What's New updates, artifact cleanup and final `git status` before commit.
- `DocumentationEncodingTests` now verifies the verification-to-release handoff order.
- `P11-ACC-172` documents the agent verification handoff guard while production proof remains required.

### Notes
- Roadmap progress is now `462/482` closed, readiness `95.9%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 44/44, backend full suite `733/733`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.449.0 - 2026-07-02

Release entry: `2026-07-02-agent-local-db-scope-guard`.

### Fixed
- `DocumentationEncodingTests` now verifies that `AGENTS.md` requires local DB validation for new user, API, payment, VPN, admin, cabinet and provisioning scenarios.
- `P11-ACC-171` documents the agent local DB scenario scope guard while production proof remains required.

### Notes
- Roadmap progress is now `461/481` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 43/43, backend full suite `732/732`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.448.0 - 2026-07-02

Release entry: `2026-07-02-agent-duplicate-task-guard`.

### Fixed
- `AGENTS.md` now contains a Duplicate And Completed Tasks section for checking roadmap, changelog, TEST_RESULTS, What's New and code before reworking repeated tasks.
- `DocumentationEncodingTests` now verifies completed-task skip and partial-task delta rules.
- `P11-ACC-170` documents the agent duplicate task guard while production proof remains required.

### Notes
- Roadmap progress is now `460/480` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 42/42, backend full suite `731/731`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.447.0 - 2026-07-02

Release entry: `2026-07-02-agent-image-attachment-guard`.

### Fixed
- `AGENTS.md` now contains an Image And Screenshot Inputs section for attachment availability checks, customer-note handling and missing-image disclosure.
- `DocumentationEncodingTests` now verifies those image attachment rules so future instruction edits cannot silently remove them.
- `P11-ACC-169` documents the agent image attachment guard while production proof remains required.

### Notes
- Roadmap progress is now `459/479` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 41/41, backend full suite `730/730`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.446.0 - 2026-07-02

Release entry: `2026-07-02-agent-git-delivery-guard`.

### Fixed
- `AGENTS.md` now contains a Git Delivery section for Russian commit messages, task-scoped staging and no push without an explicit user request.
- `DocumentationEncodingTests` now verifies those git delivery rules so future instruction edits cannot silently remove them.
- `P11-ACC-168` documents the agent git delivery guard while production proof remains required.

### Notes
- Roadmap progress is now `458/478` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 40/40, backend full suite `729/729`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.445.0 - 2026-07-02

Release entry: `2026-07-02-agent-unavailable-checks-risk-guard`.

### Fixed
- `DocumentationEncodingTests` now verifies that `AGENTS.md` requires final-answer disclosure for unavailable tests, local DB checks and external checks.
- `P11-ACC-167` documents the agent unavailable checks risk guard while production proof remains required.

### Notes
- Roadmap progress is now `457/477` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 39/39, backend full suite `728/728`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.444.0 - 2026-07-02

Release entry: `2026-07-02-agent-source-version-reporting-guard`.

### Fixed
- `DocumentationEncodingTests` now verifies that `AGENTS.md` requires source and status date/version reporting when roadmap or markdown data is used in final answers.
- `P11-ACC-166` documents the agent source/version reporting guard while production proof remains required.

### Notes
- Roadmap progress is now `456/476` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 38/38, backend full suite `727/727`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.443.0 - 2026-07-02

Release entry: `2026-07-02-whats-new-progress-consistency-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that the latest active What's New release reports the same roadmap progress counters as the master roadmap markers.
- `P11-ACC-165` documents the What's New progress consistency guard while production proof remains required.

### Notes
- Roadmap progress is now `455/475` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 37/37, backend full suite `726/726`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.442.0 - 2026-07-02

Release entry: `2026-07-02-status-docs-progress-consistency-guard`.

### Fixed
- `RoadmapCurrentStateTests` now calculates roadmap progress from `PRODUCT_COMPLETION_ROADMAP.md` markers and verifies README, CHANGELOG, TEST_RESULTS, final runbook, release decision and product/admin UI roadmap show the same counters.
- `P11-ACC-164` documents the status docs progress consistency guard while production proof remains required.

### Notes
- Roadmap progress is now `454/474` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Validation includes targeted docs/release/encoding suite 36/36, backend full suite `725/725`, fresh local SQLite smoke OK and secret scan `556` files, `0` findings.

## 0.441.0 - 2026-07-02

Release entry: `2026-07-02-all-markdown-utf8-guard`.

### Fixed
- `DocumentationEncodingTests` now includes all tracked markdown files, including delivery and infra notes outside `docs`, in strict UTF-8 and mojibake checks.
- `P11-ACC-163` documents the all-markdown UTF-8 guard while production proof remains required.

### Notes
- Roadmap progress is now `453/473` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.

## 0.440.0 - 2026-07-02

Release entry: `2026-07-02-deploy-frontend-text-utf8-guard`.

### Fixed
- `DocumentationEncodingTests` now includes frontend HTML/CSS, Dockerfiles, nginx config, Ansible inventory/templates and Python helper files in strict UTF-8 and mojibake checks.
- `P11-ACC-162` documents the deploy/frontend text UTF-8 guard while production proof remains required.

### Notes
- Roadmap progress is now `452/472` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.

## 0.439.0 - 2026-07-02

Release entry: `2026-07-02-env-example-utf8-guard`.

### Fixed
- `DocumentationEncodingTests` now includes the tracked `.env.example` environment template in strict UTF-8 and mojibake checks.
- `P11-ACC-161` documents the `.env.example` UTF-8 guard while production proof remains required.

### Notes
- Roadmap progress is now `451/471` closed, readiness `95.8%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.

## 0.438.0 - 2026-07-02

Release entry: `2026-07-02-dotfiles-utf8-guard`.

### Fixed
- `DocumentationEncodingTests` now includes tracked dotfiles such as `.dockerignore`, `.editorconfig`, `.gitattributes` and `.gitignore` in strict UTF-8 and mojibake checks.
- `P11-ACC-160` documents the dotfiles UTF-8 guard while production proof remains required.

### Notes
- Roadmap progress is now `450/470` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.

## 0.437.0 - 2026-07-02

Release entry: `2026-07-02-project-files-utf8-guard`.

### Fixed
- `DocumentationEncodingTests` now covers project/config files including `.sln`, `.csproj`, `.props`, `.targets`, `.http`, `.config` and `.xml`.
- `backend/VpnPlatform.sln` is normalized to UTF-8 without BOM.
- `P11-ACC-159` documents the project files UTF-8 guard while production proof remains required.

### Notes
- Roadmap progress is now `449/469` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DocumentationEncodingTests` 3/3; targeted docs/release/encoding suite 35/35; backend full suite `724/724`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.
## 0.436.0 - 2026-07-02

Release entry: `2026-07-02-changelog-mojibake-guard`.

### Fixed
- `DocumentationEncodingTests` now scans `CHANGELOG.md` for mojibake markers as well as strict UTF-8.
- `P11-ACC-158` documents the changelog mojibake guard while production proof remains required.

### Notes
- Roadmap progress is now `448/468` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DocumentationEncodingTests` 3/3; targeted docs/release/encoding suite 35/35; backend full suite `724/724`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.
## 0.435.0 - 2026-07-02

Release entry: `2026-07-02-source-mojibake-guard`.

### Fixed
- `DocumentationEncodingTests` now scans source-like tracked files for mojibake markers and strict UTF-8.
- Payment provider tests keep mojibake checks as Unicode escape literals instead of damaged Cyrillic text.
- `P11-ACC-157` documents the source mojibake guard while production proof remains required.

### Notes
- Roadmap progress is now `447/467` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DocumentationEncodingTests` 3/3; targeted docs/release/encoding suite 35/35; backend full suite `724/724`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.
## 0.434.0 - 2026-07-02

Release entry: `2026-07-02-agent-instructions-readable-utf8-guard`.

### Fixed
- `AGENTS.md` is readable UTF-8 and no longer stores mojibake text for the mandatory agent rules.
- `DocumentationEncodingTests` now asserts the real Russian progress, cleanup, testing and local DB clauses.
- `P11-ACC-156` documents the readable agent instructions guard while production proof remains required.

### Notes
- Roadmap progress is now `446/466` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DocumentationEncodingTests` 3/3; targeted docs/release/encoding suite 35/35; backend full suite `724/724`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.433.0 - 2026-07-02

Release entry: `2026-07-02-docs-strict-utf8-guard`.

### Fixed
- `DocumentationEncodingTests` now verifies that docs, status files and the release seed are strict UTF-8 without BOM.
- `P11-ACC-155` documents the strict UTF-8 guard while production proof remains required.

### Notes
- Roadmap progress is now `445/465` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DocumentationEncodingTests` 3/3; targeted docs/release/encoding suite 35/35; backend full suite `724/724`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.
## 0.432.0 - 2026-07-02

Release entry: `2026-07-02-latest-release-evidence-caveat-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that the latest active release note contains an `important` caveat about required external evidence.
- `P11-ACC-154` documents the latest release evidence caveat guard while production proof remains required.

### Notes
- Roadmap progress is now `444/464` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 11/11; targeted docs/release/encoding suite 34/34; backend full suite `723/723`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.
## 0.431.0 - 2026-07-02

Release entry: `2026-07-02-status-docs-production-ready-claim-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that current status docs do not claim production-ready acceptance or close `P11-ACC-002` without external evidence.
- `P11-ACC-153` documents the status docs production-ready claim guard while production proof remains required.

### Notes
- Roadmap progress is now `443/463` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 10/10; targeted docs/release/encoding suite 33/33; backend full suite `722/722`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.
## 0.430.0 - 2026-07-02

Release entry: `2026-07-02-release-seed-secret-literal-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that `releases.json` does not contain PEM private keys, bearer values, provider keys or raw provider payload markers.
- `P11-ACC-152` documents the release seed secret literal guard while production proof remains required.

### Notes
- Roadmap progress is now `442/462` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 9/9; targeted docs/release/encoding suite 32/32; backend full suite `721/721`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.429.0 - 2026-07-02

Release entry: `2026-07-02-release-seed-file-order-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that `releases.json` entries are physically ordered by increasing `releasedAt` timestamps.
- `P11-ACC-151` documents the release seed file order guard while production proof remains required.

### Notes
- Roadmap progress is now `441/461` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 8/8; targeted docs/release/encoding suite 31/31; backend full suite `720/720`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.428.0 - 2026-07-02

Release entry: `2026-07-02-release-seed-version-order-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that release seed versions strictly increase with `releasedAt` timestamps.
- `P11-ACC-150` documents the release seed version order guard while production proof remains required.

### Notes
- Roadmap progress is now `440/460` closed, readiness `95.7%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 7/7; targeted docs/release/encoding suite 30/30; backend full suite `719/719`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.427.0 - 2026-07-02

Release entry: `2026-07-02-release-seed-identity-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that release seed `releaseId`, `version` and `releasedAt` values stay unique.
- `P11-ACC-149` documents the release seed identity guard while production proof remains required.

### Notes
- Roadmap progress is now `439/459` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 6/6; targeted docs/release/encoding suite 29/29; backend full suite `718/718`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.426.0 - 2026-07-02

Release entry: `2026-07-02-latest-release-seed-order-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that the active release seed has exactly one newest `releasedAt` entry and that it matches the current roadmap release.
- `P11-ACC-148` documents the latest release seed order guard while production proof remains required.

### Notes
- Roadmap progress is now `438/458` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 5/5; targeted docs/release/encoding suite 28/28; backend full suite `717/717`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.425.0 - 2026-07-02

Release entry: `2026-07-02-changelog-test-results-top-release-guard`.

### Fixed
- `FinalDocsChangelogTests` now verifies that the top `CHANGELOG.md` and `TEST_RESULTS.md` blocks match the latest active release seed.
- `P11-ACC-147` documents the changelog/test results top release guard while production proof remains required.

### Notes
- Roadmap progress is now `437/457` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `FinalDocsChangelogTests` 7/7; targeted docs/release/encoding suite 27/27; backend full suite `716/716`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.424.0 - 2026-07-02

Release entry: `2026-07-02-status-docs-latest-release-seed-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that status documents include the latest active release seed, including the product/admin UI roadmap.
- `P11-ACC-146` documents the status docs latest release seed guard while production proof remains required.

### Notes
- Roadmap progress is now `436/456` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 4/4; targeted docs/release/encoding suite 26/26; backend full suite `715/715`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.423.0 - 2026-07-02

Release entry: `2026-07-02-roadmap-external-evidence-open-set-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies that every not-closed roadmap marker belongs to the explicit external-evidence set.
- `P11-ACC-145` documents the roadmap external evidence open-set guard while production proof remains required.

### Notes
- Roadmap progress is now `435/455` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 4/4; targeted docs/release/encoding suite 26/26; backend full suite `715/715`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.422.0 - 2026-07-02

Release entry: `2026-07-02-product-roadmap-external-evidence-open-guard`.

### Fixed
- `ProductAdminUiRoadmapSyncTests` now verifies that `docs/product-admin-ui-roadmap.md` keeps current validation status and production limitations explicit.
- `P11-ACC-144` documents the product/admin UI roadmap external evidence open guard while production proof remains required.

### Notes
- Roadmap progress is now `434/454` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `ProductAdminUiRoadmapSyncTests` 3/3; targeted docs/release/encoding suite 26/26; backend full suite `715/715`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.421.0 - 2026-07-02

Release entry: `2026-07-02-readme-external-evidence-open-guard`.

### Fixed
- `ReadmeDocumentationTests` now verifies that `README.md` keeps current release status and production limitations explicit.
- `P11-ACC-143` documents the README external evidence open guard while production proof remains required.

### Notes
- Roadmap progress is now `433/453` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `ReadmeDocumentationTests` 4/4; targeted docs/release/encoding suite 25/25; backend full suite `714/714`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.420.0 - 2026-07-02

Release entry: `2026-07-02-changelog-external-evidence-open-guard`.

### Fixed
- `FinalDocsChangelogTests` now verifies that `CHANGELOG.md` keeps current roadmap progress, validation evidence and production limitations explicit.
- `P11-ACC-142` documents the changelog external evidence open guard while production proof remains required.

### Notes
- Roadmap progress is now `432/452` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `FinalDocsChangelogTests` 6/6; targeted docs/release/encoding suite 24/24; backend full suite `713/713`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.419.0 - 2026-07-02

Release entry: `2026-07-02-test-results-external-evidence-open-guard`.

### Fixed
- `FinalDocsChangelogTests` now verifies that `TEST_RESULTS.md` keeps current release status, local validation evidence and artifact cleanup explicit.
- `P11-ACC-141` documents the test results external evidence open guard while production proof remains required.

### Notes
- Roadmap progress is now `431/451` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `FinalDocsChangelogTests` 5/5; targeted docs/release/encoding suite 23/23; backend full suite `712/712`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.418.0 - 2026-07-02

Release entry: `2026-07-02-final-runbook-external-evidence-open-guard`.

### Fixed
- `FinalDocsChangelogTests` now verifies that the final runbook keeps current release status and production limitations explicit.
- `P11-ACC-140` documents the final runbook external evidence open guard while production proof remains required.

### Notes
- Roadmap progress is now `430/450` closed, readiness `95.6%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `FinalDocsChangelogTests` 4/4; targeted docs/release/encoding suite 22/22; backend full suite `711/711`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.417.0 - 2026-07-02

Release entry: `2026-07-02-release-decision-external-evidence-open-guard`.

### Fixed
- `ReleaseDecisionTests` now verifies that production readiness blockers remain tied to open roadmap evidence items.
- `P11-ACC-139` documents the release decision external evidence open guard while production proof remains required.

### Notes
- Roadmap progress is now `429/449` closed, readiness `95.5%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `ReleaseDecisionTests` 4/4; targeted docs/release/encoding suite 21/21; backend full suite `710/710`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.416.0 - 2026-07-02

Release entry: `2026-07-02-product-ui-external-evidence-open-guard`.

### Fixed
- `ProductAdminUiRoadmapSyncTests` now verifies that product/admin UI roadmap live VPS, payment, VPN and staging evidence rows stay open or in progress until real evidence exists.
- `P11-ACC-138` documents the product/admin UI external evidence open guard while production proof remains required.

### Notes
- Roadmap progress is now `428/448` closed, readiness `95.5%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `ProductAdminUiRoadmapSyncTests` 2/2; targeted docs/release/encoding suite 20/20; backend full suite `709/709`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.415.0 - 2026-07-02

Release entry: `2026-07-02-external-evidence-open-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies every external live VPS, admin, VPN, payment, staging and production smoke evidence marker stays open or in progress until real evidence exists.
- `P11-ACC-137` documents the external evidence open guard while live VPS/staging/payment/3x-ui proof remains required.

### Notes
- Roadmap progress is now `427/447` closed, readiness `95.5%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 4/4; targeted docs/release/encoding suite 19/19; backend full suite `708/708`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.414.0 - 2026-07-02

Release entry: `2026-07-02-agent-instructions-guard`.

### Fixed
- `AGENTS.md` is normalized to readable UTF-8 Russian text.
- `DocumentationEncodingTests` now covers `AGENTS.md` and required progress, cleanup, testing and local DB instructions.
- `P11-ACC-136` documents the agent instructions guard while real VPS/staging/live evidence remains open.

### Notes
- Roadmap progress is now `426/446` closed, readiness `95.5%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 3/3; targeted docs/release/encoding suite 18/18; backend full suite `707/707`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.413.0 - 2026-07-02

Release entry: `2026-07-02-roadmap-progress-remaining-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies the explicit remaining task count in the roadmap header.
- `P11-ACC-135` documents the roadmap remaining count guard while real VPS/staging/live evidence remains open.

### Notes
- Roadmap progress is now `425/445` closed, readiness `95.5%`, `20` remaining, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 3/3; targeted docs/release suite 17/17; backend full suite `706/706`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.412.0 - 2026-07-02

Release entry: `2026-07-02-roadmap-progress-percent-guard`.

### Fixed
- `RoadmapCurrentStateTests` now verifies the roadmap readiness percent against actual checklist markers using `completed / total * 100`.
- `P11-ACC-134` documents the roadmap percent guard while real VPS/staging/live evidence remains open.

### Notes
- Roadmap progress is now `424/444` closed, readiness `95.5%`, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 3/3; targeted docs/release suite 17/17; backend full suite `706/706`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.411.0 - 2026-07-02

Release entry: `2026-07-02-roadmap-progress-counter-guard`.

### Fixed
- `RoadmapCurrentStateTests` now parses `PRODUCT_COMPLETION_ROADMAP.md` checklist markers and verifies completed, total, open, in-progress and blocked counts against the roadmap header.
- `P11-ACC-133` documents the roadmap progress counter guard while real VPS/staging/live evidence remains open.

### Notes
- Roadmap progress is now `423/443` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests` 3/3; targeted docs/release suite 17/17; backend full suite `706/706`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.410.0 - 2026-07-02

Release entry: `2026-07-02-production-readiness-assertion-result-self-link`.

### Fixed
- Production readiness assertion result JSON now requires `resultJsonPath` to self-link to the validated JSON file.
- `validate-production-readiness-assertion-result.ps1` rejects mismatched result JSON/Markdown self-links before accepting assertion evidence.
- `P11-ACC-132` documents the standalone assertion result self-link guard while real VPS/staging/live evidence remains open.

### Notes
- Roadmap progress is now `422/442` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `ProductionReadinessGateTests`; targeted docs/release suite; backend full suite `705/705`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `556` files, `0` findings; markdown/code encoding check OK.

## 0.409.0 - 2026-07-02

Release entry: `2026-07-02-production-readiness-summary-self-link`.

### Fixed
- Production readiness summary JSON now includes `summaryPath` and `jsonSummaryPath`.
- `validate-production-readiness-summary.ps1` resolves both self-links and rejects summary JSON when either path does not match the actual validated Markdown/JSON file.
- `P11-ACC-131` documents the standalone production readiness summary self-link guard while real VPS/staging/live evidence remains open.

### Notes
- Roadmap progress is now `421/441` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `ProductionReadinessGateTests`; targeted docs/release suite; backend full suite `704/704`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `555` files, `0` findings; markdown/code encoding check OK.

## 0.408.0 - 2026-07-02

Release entry: `2026-07-02-vpn-live-smoke-report-self-link`.

### Fixed
- VPN live smoke reports now include `smokeReportPath` in the template and generated report.
- `validate-vpn-live-smoke-report.ps1` resolves `smokeReportPath` and rejects reports whose self-link does not match the actual `-ReportPath`.
- `P0-VPN-011` documents the standalone VPN live smoke evidence self-link guard while real 3x-ui/live VPN smoke remains open.

### Notes
- Roadmap progress is now `420/440` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `VpnLiveSmokeReportTests`; targeted docs/release suite; backend full suite `703/703`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `554` files, `0` findings; markdown/code encoding check OK.

## 0.407.0 - 2026-07-02

Release entry: `2026-07-02-payment-provider-smoke-report-self-link`.

### Fixed
- Payment provider smoke reports now include `smokeReportPath` in the template and generated report.
- `validate-payment-provider-smoke-report.ps1` resolves `smokeReportPath` and rejects reports whose self-link does not match the actual `-ReportPath`.
- `P0-PAY-019` documents the standalone payment provider smoke evidence self-link guard while real live provider smoke remains open.

### Notes
- Roadmap progress is now `419/439` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `PaymentProviderSmokeReportTests`; targeted docs/release suite; backend full suite `702/702`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `553` files, `0` findings; markdown/code encoding check OK.

## 0.406.0 - 2026-07-02

Release entry: `2026-07-02-staging-smoke-report-self-link`.

### Fixed
- Staging smoke reports now include `smokeReportPath` in the template and generated report.
- `validate-staging-smoke-report.ps1` resolves `smokeReportPath` and rejects reports whose self-link does not match the actual `-ReportPath`.
- `P9-TST-007J` documents the standalone staging smoke evidence self-link guard while real staging/VPS smoke remains open.

### Notes
- Roadmap progress is now `418/438` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `StagingSmokeChecklistTests`; targeted docs/release suite; backend full suite `701/701`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `552` files, `0` findings; markdown/code encoding check OK.

## 0.405.0 - 2026-07-02

Release entry: `2026-07-02-vps-production-smoke-report-self-link`.

### Fixed
- VPS production smoke reports now include `smokeReportPath` in the template and generated report.
- `validate-vps-production-smoke-report.ps1` resolves `smokeReportPath` and rejects reports whose self-link does not match the actual `-ReportPath`.
- `P11-ACC-130` documents the standalone VPS production smoke evidence self-link guard while real live VPS smoke remains open.

### Notes
- Roadmap progress is now `417/437` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `VpsProductionSmokeTests`; targeted docs/release suite; backend full suite `700/700`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `551` files, `0` findings; markdown/code encoding check OK.

## 0.404.0 - 2026-07-02

Release entry: `2026-07-02-provision-node-wrapper-cleanup`.

### Fixed
- `scripts/provision-node.sh` now resolves the Ansible runner and playbooks from the repository root instead of passing relative paths into a temp workdir.
- The wrapper now creates a unique per-run default workdir with `mktemp` and removes it with `trap cleanup_workdir EXIT`.
- `P8-CI-012` documents manual provisioning wrapper cleanup while explicit custom workdirs remain available for operator diagnostics.

### Notes
- Roadmap progress is now `416/436` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DeployWorkflowGuardTests`; `bash -n scripts/provision-node.sh`; targeted deploy/docs/release suite; backend full suite `699/699`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.403.0 - 2026-07-02

Release entry: `2026-07-02-validate-repo-ansible-tmp-cleanup`.

### Fixed
- `scripts/validate_repo.sh` now registers cleanup immediately after creating the temporary Ansible syntax-check inventory directory.
- The local repository validation script removes that directory with `trap cleanup_ansible_tmp EXIT` even when `ansible-playbook --syntax-check` fails.
- `P8-CI-011` documents local validation temp cleanup while real staging/VPS/live evidence remains open.

### Notes
- Roadmap progress is now `415/435` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DeployWorkflowGuardTests`; `bash -n scripts/validate_repo.sh`; targeted deploy/docs/release suite; backend full suite `698/698`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.402.0 - 2026-07-02

Release entry: `2026-07-02-ci-ansible-tmp-cleanup`.

### Fixed
- `.github/workflows/ci.yml` now writes the CI Ansible syntax-check inventory into a per-run `mktemp` directory.
- The provisioning syntax-check job removes that temporary inventory directory with `trap cleanup EXIT` and no longer leaves `/tmp/vpnplatform-ci`.
- `P8-CI-010` documents CI Ansible temp cleanup while real staging/VPS/live evidence remains open.

### Notes
- Roadmap progress is now `414/434` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DeployWorkflowGuardTests`; targeted deploy/docs/release suite; backend full suite `697/697`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.401.0 - 2026-07-02

Release entry: `2026-07-02-deploy-vps-docker-tmp-cleanup`.

### Fixed
- `.github/workflows/deploy-vps.yml` now writes the remote Docker compose config check into a per-run `mktemp` directory during `Start Docker production stack`.
- The remote Docker deploy step removes that temporary compose config artifact with `trap cleanup EXIT` and no longer leaves `/tmp/vpnplatform-compose.yml` on the VPS.
- `P8-CI-009` documents Docker deploy temp cleanup while real VPS deploy and post-deploy smoke evidence remain open.

### Notes
- Roadmap progress is now `413/433` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `DeployWorkflowGuardTests`; targeted deploy/docs/release suite; backend full suite `696/696`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.400.0 - 2026-07-02

Release entry: `2026-07-02-docker-validation-tmp-cleanup`.

### Fixed
- `scripts/validate-docker.sh` now stores curl output, compose config and runtime logs in a unique per-run temp directory instead of fixed `/tmp/vpnplatform-*` files.
- The Docker validation gate removes its temp directory on exit even when `KEEP_STACK=1` keeps the compose stack for manual inspection.
- `P8-CI-008` documents Docker validation temp cleanup while real staging/VPS evidence remains open.

### Notes
- Roadmap progress is now `412/432` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `SandboxE2EScenariosMvpTests`; `bash -n scripts/validate-docker.sh`; early missing-Docker cleanup regression; targeted docker/docs/release suite; backend full suite `695/695`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.399.0 - 2026-07-02

Release entry: `2026-07-02-production-env-normalizer-cleanup`.

### Fixed
- `test-normalize-production-env.ps1` now removes its autogenerated production.env fixtures and empty parent `tmp` after ordinary local runs.
- Custom `-OutputDirectory` runs still keep normalized env fixtures for explicit debugging.
- `P8-CI-007` documents the production env normalizer regression cleanup while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `411/431` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests`; production env normalizer cleanup OK; targeted deploy/docs/release suite `20/20`; backend full suite `694/694`; frontend tests `66/66`; frontend typecheck/build/audit OK; local normalizer regression OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.398.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-smoke-sections-contract-cleanup`.

### Fixed
- `test-admin-vps-smoke-sections-contract.ps1` now removes its autogenerated fixtures and empty parent `tmp` after ordinary local runs.
- Custom `-OutputDirectory` runs still keep section contract fixtures for explicit debugging.
- `P0-ADMIN-002BT` documents the sections contract regression cleanup while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `410/430` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests`; admin VPS smoke sections contract cleanup OK; targeted admin/docs/release suite `44/44`; backend full suite `693/693`; frontend tests `66/66`; frontend typecheck/build/audit OK; local sections contract regression OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.397.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-bootstrap-smoke-readiness-cleanup`.

### Fixed
- `test-admin-vps-bootstrap-smoke-readiness.ps1` now removes its autogenerated readiness reports and empty parent `tmp` after ordinary local runs.
- Custom `-OutputDirectory` runs still keep regression evidence for explicit debugging.
- `P0-ADMIN-001CF` documents the readiness regression cleanup while real VPS admin bootstrap/smoke evidence remains open.

### Notes
- Roadmap progress is now `409/429` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: `RoadmapCurrentStateTests`; admin VPS bootstrap smoke readiness cleanup OK; targeted bootstrap/docs/release suite `37/37`; backend full suite `692/692`; frontend tests `66/66`; frontend typecheck/build/audit OK; local readiness regression OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.396.0 - 2026-07-02

Release entry: `2026-07-02-local-admin-vps-bootstrap-smoke-cleanup`.

### Fixed
- `local-admin-vps-bootstrap-smoke.ps1` now removes its `tmp/local-admin-vps-bootstrap-smoke` output and empty parent `tmp` after ordinary local runs.
- `-KeepArtifacts` still preserves the local bootstrap SQLite DB, reports and logs for explicit local debugging.
- `P0-ADMIN-001CE` documents the local cleanup behavior while real VPS admin bootstrap/smoke evidence remains open.

### Notes
- Roadmap progress is now `408/428` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: local admin VPS bootstrap smoke cleanup OK; targeted bootstrap/docs/release suite `36/36`; backend full suite `691/691`; frontend tests `66/66`; frontend typecheck/build/audit OK; local bootstrap SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.395.0 - 2026-07-02

Release entry: `2026-07-02-local-admin-vps-browser-smoke-cleanup`.

### Fixed
- `local-admin-vps-browser-smoke.ps1` now removes its `tmp/local-admin-vps-browser-smoke` output and empty parent `tmp` after ordinary local runs.
- `-KeepArtifacts` still preserves the local admin browser SQLite DB, reports and logs for explicit local debugging.
- `P0-ADMIN-002BS` documents the local cleanup behavior while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `407/427` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: local admin VPS browser smoke cleanup OK; targeted admin/docs/release suite `43/43`; backend full suite `690/690`; frontend tests `66/66`; frontend typecheck/build/audit OK; local admin browser SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.394.0 - 2026-07-02

Release entry: `2026-07-02-fresh-local-smoke-cleanup`.

### Fixed
- `fresh-local-smoke.ps1` now removes its `tmp/fresh-local-smoke` output and empty parent `tmp` after ordinary local runs.
- `-KeepArtifacts` still preserves the fresh local SQLite DB and logs for explicit local debugging.
- `P11-ACC-129` documents the local cleanup behavior while real VPS/staging/live evidence remains open.

### Notes
- Roadmap progress is now `406/426` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: fresh local smoke cleanup OK; targeted fresh-local/docs/release suite `17/17`; backend full suite `689/689`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.393.0 - 2026-07-02

Release entry: `2026-07-02-local-admin-vps-bootstrap-smoke-wrapper-cleanup`.

### Fixed
- `test-local-admin-vps-bootstrap-smoke-wrapper.ps1` now removes its default output directory and empty `tmp` after ordinary local runs.
- `-KeepArtifacts` still preserves local bootstrap wrapper regression evidence for explicit local debugging.
- `P0-ADMIN-001CD` documents the local cleanup behavior while real VPS admin bootstrap/smoke evidence remains open.

### Notes
- Roadmap progress is now `405/425` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: local admin VPS bootstrap smoke wrapper cleanup OK; targeted bootstrap/docs/release suite `34/34`; backend full suite `688/688`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.392.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-bootstrap-smoke-wrapper-cleanup`.

### Fixed
- `test-admin-vps-bootstrap-smoke-wrapper.ps1` now removes its default output directory and empty `tmp` after ordinary local runs.
- `-KeepArtifacts` still preserves bootstrap wrapper regression evidence for explicit local debugging.
- `P0-ADMIN-001CC` documents the local cleanup behavior while real VPS admin bootstrap/smoke evidence remains open.

### Notes
- Roadmap progress is now `404/424` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS bootstrap smoke wrapper cleanup OK; targeted bootstrap/docs/release suite `33/33`; backend full suite `687/687`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.391.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-smoke-flow-wrapper-cleanup`.

### Fixed
- `test-admin-vps-smoke-flow-wrapper.ps1` now removes its default output directory and empty `tmp` after ordinary local runs.
- `-KeepArtifacts` still preserves wrapper regression evidence for explicit local debugging.
- `P0-ADMIN-002BR` documents the local cleanup behavior while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `403/423` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke flow wrapper cleanup OK; targeted admin/docs/release suite `41/41`; backend full suite `686/686`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.390.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-bootstrap-smoke-evidence-validator-cleanup`.

### Fixed
- `test-admin-vps-bootstrap-smoke-evidence-validator.ps1` now uses the latest active release from `releases.json` across readiness, preflight, smoke and bootstrap synthetic evidence.
- The bootstrap smoke evidence validator regression now emits current readiness/preflight fields and removes its default output plus empty `tmp` after ordinary local runs.
- `P0-ADMIN-001CB` documents the local cleanup behavior while real VPS admin bootstrap/smoke evidence remains open.

### Notes
- Roadmap progress is now `402/422` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS bootstrap smoke evidence validator cleanup OK; targeted admin/docs/release suite `32/32`; backend full suite `685/685`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.389.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-smoke-evidence-validator-cleanup`.

### Fixed
- `test-admin-vps-smoke-evidence-validator.ps1` now uses the latest active release from `releases.json`, so synthetic preflight/smoke evidence keeps matching release rotation.
- The evidence validator regression now includes required preflight counters and remote release fields, and removes its default output plus empty `tmp` after ordinary local runs.
- `P0-ADMIN-002BQ` documents the local cleanup behavior while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `401/421` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke evidence validator cleanup OK; targeted admin/docs/release suite `40/40`; backend full suite `684/684`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.388.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-smoke-preflight-validator-cleanup`.

### Fixed
- `test-admin-vps-smoke-preflight-validator.ps1` now removes its default output directory and empty `tmp` after ordinary local runs.
- `AdminVpsSmokeReportTests` pins the preflight validator regression cleanup contract while preserving `-KeepArtifacts` debug evidence.
- `P0-ADMIN-002BP` documents the local cleanup behavior while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `400/420` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke preflight validator cleanup OK; targeted admin/docs/release suite `39/39`; backend full suite `683/683`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.387.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-smoke-report-validator-cleanup`.

### Fixed
- `test-admin-vps-smoke-report-validator.ps1` now uses the latest active release from `releases.json`, so the regression keeps passing after release rotation.
- The validator regression removes its default output directory and empty `tmp` after ordinary local runs.
- `P0-ADMIN-002BO` documents the local cleanup behavior while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `399/419` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke report validator cleanup OK; targeted admin/docs/release suite `38/38`; backend full suite `682/682`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.386.0 - 2026-07-02

Release entry: `2026-07-02-admin-bootstrap-wrapper-cleanup`.

### Fixed
- `test-admin-bootstrap-wrapper.ps1` now removes its default output directory and empty `tmp` after ordinary local runs.
- `AdminBootstrapCliScriptTests` pins the direct bootstrap wrapper cleanup contract while preserving `-KeepArtifacts` debug evidence.
- `P0-ADMIN-001CA` documents the local cleanup behavior while real VPS admin bootstrap/smoke evidence remains open.

### Notes
- Roadmap progress is now `398/418` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin bootstrap wrapper cleanup OK; targeted admin/docs/release suite `31/31`; backend full suite `681/681`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.385.0 - 2026-07-02

Release entry: `2026-07-02-production-readiness-ci-step-summary-cleanup`.

### Fixed
- `test-production-readiness-assertion-ci-step-summary.ps1` now removes its autogenerated default output directory and empty `tmp` after ordinary local runs.
- `ProductionReadinessGateTests` pins cleanup while preserving explicit `-OutputDirectory` and `-WriteJson` evidence/debug flows.
- `P11-ACC-128` documents the local cleanup behavior while real VPS production smoke evidence remains open.

### Notes
- Roadmap progress is now `397/417` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production readiness CI step summary cleanup OK; targeted production/docs/release suite `210/210`; backend full suite `680/680`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.384.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-browser-smoke-direct-release-guard-cleanup`.

### Fixed
- `test-admin-vps-browser-smoke-direct-release-guard.ps1` now removes the empty default `tmp` directory after a local unknown release id regression run.
- `AdminVpsSmokeReportTests` extends the release guard cleanup contract to cover the direct admin VPS browser smoke guard.
- `P0-ADMIN-002BN` documents the local cleanup behavior while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `396/416` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS browser smoke direct release guard cleanup OK; targeted admin/docs/release suite `198/198`; backend full suite `679/679`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.383.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-bootstrap-release-guard-cleanup`.

### Fixed
- Admin VPS bootstrap smoke latest release, bootstrap evidence latest release and bootstrap readiness known release guard harnesses now remove empty default `tmp` after local runs.
- `AdminBootstrapCliScriptTests` pins the cleanup contract across all admin VPS bootstrap release guard harnesses.
- `P0-ADMIN-001BX` ... `P0-ADMIN-001BZ` document the local cleanup behavior while real VPS admin bootstrap/smoke evidence remains open.

### Notes
- Roadmap progress is now `395/415` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS bootstrap release guard cleanup OK; targeted admin bootstrap/docs/release suite `198/198`; backend full suite `679/679`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.382.0 - 2026-07-02

Release entry: `2026-07-02-admin-vps-smoke-release-guard-cleanup`.

### Fixed
- Admin VPS smoke report generator, report latest release, preflight latest release, preflight known release and evidence latest release guard harnesses now remove empty default `tmp` after local runs.
- `AdminVpsSmokeReportTests` pins the cleanup contract across all admin VPS smoke release guard harnesses.
- `P0-ADMIN-002BI` ... `P0-ADMIN-002BM` document the local cleanup behavior while real VPS admin smoke evidence remains open.

### Notes
- Roadmap progress is now `392/412` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke release guard cleanup OK; targeted admin/docs/release suite `183/183`; backend full suite `678/678`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.381.0 - 2026-07-02

Release entry: `2026-07-02-payment-smoke-release-guard-cleanup`.

### Fixed
- `test-payment-provider-smoke-report-generator-release-guard.ps1` now removes the empty default `tmp` directory after a local unknown release id regression run.
- `test-payment-provider-smoke-report-latest-release-guard.ps1` now removes its stale-release JSON report and empty default `tmp` directory after a local regression run.
- `PaymentProviderSmokeReportTests` pins both payment provider smoke release guard cleanup contracts.
- `P0-PAY-017` and `P0-PAY-018` document the local cleanup behavior while real payment provider smoke items remain open until external provider evidence exists.

### Notes
- Roadmap progress is now `387/407` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: payment provider smoke release guard cleanup OK; targeted payment/docs/release suite `161/161`; backend full suite `677/677`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.380.0 - 2026-07-02

Release entry: `2026-07-02-vpn-live-smoke-latest-release-guard-cleanup`.

### Fixed
- `test-vpn-live-smoke-report-latest-release-guard.ps1` now removes its stale-release JSON report and empty default `tmp` directory after a local regression run.
- `VpnLiveSmokeReportTests` pins the VPN live smoke latest release guard cleanup.
- `P0-VPN-010` documents the local cleanup behavior while real VPN live smoke items remain open until external 3x-ui/VPN evidence exists.

### Notes
- Roadmap progress is now `385/405` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPN live smoke latest release guard cleanup OK; targeted VPN/docs/release suite `151/151`; backend full suite `675/675`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.379.0 - 2026-07-02

Release entry: `2026-07-02-vpn-live-smoke-generator-release-guard-cleanup`.

### Fixed
- `test-vpn-live-smoke-report-generator-release-guard.ps1` now removes the empty default `tmp` directory after a local unknown release id regression run.
- `VpnLiveSmokeReportTests` pins the VPN live smoke generator release guard cleanup.
- `P0-VPN-009` documents the local cleanup behavior while real VPN live smoke items remain open until external 3x-ui/VPN evidence exists.

### Notes
- Roadmap progress is now `384/404` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPN live smoke generator release guard cleanup OK; targeted VPN/docs/release suite `150/150`; backend full suite `674/674`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.378.0 - 2026-07-02

Release entry: `2026-07-02-vps-production-smoke-latest-release-guard-cleanup`.

### Fixed
- `test-vps-production-smoke-report-latest-release-guard.ps1` now removes its stale-release JSON report and empty default `tmp` directory after a local regression run.
- `VpsProductionSmokeTests` pins the VPS production smoke latest release guard cleanup.
- `P11-ACC-127` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `383/403` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPS production smoke latest release guard cleanup OK; targeted VPS/docs/release suite `154/154`; backend full suite `673/673`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.377.0 - 2026-07-02

Release entry: `2026-07-02-vps-production-smoke-generator-release-guard-cleanup`.

### Fixed
- `test-vps-production-smoke-report-generator-release-guard.ps1` now removes the empty default `tmp` directory after a local unknown release id regression run.
- `VpsProductionSmokeTests` pins the VPS production smoke generator release guard cleanup.
- `P11-ACC-126` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `382/402` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPS production smoke generator release guard cleanup OK; targeted VPS/docs/release suite `153/153`; backend full suite `672/672`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.376.0 - 2026-07-02

Release entry: `2026-07-02-production-evidence-bundle-generator-release-guard-cleanup`.

### Fixed
- `test-production-evidence-bundle-generator-release-guard.ps1` now removes the empty default `tmp` directory after a local unknown release id regression run.
- `ProductionReadinessGateTests` pins the production evidence bundle generator release guard cleanup.
- `P11-ACC-125` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `381/401` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence bundle generator release guard cleanup OK; targeted production/docs/release suite `143/143`; backend full suite `671/671`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.375.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-package-markdown-files-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-markdown-files-guard.ps1` now removes its default `tmp/production-evidence-handoff-package-markdown-files-guard` bundle directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff package Markdown files guard cleanup.
- `P11-ACC-124` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `380/400` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff package Markdown files guard cleanup OK; targeted production/docs/release suite `142/142`; backend full suite `670/670`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.374.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-checklist-markdown-gates-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-checklist-markdown-gates-guard.ps1` now removes its default `tmp/production-evidence-handoff-checklist-markdown-gates-guard` bundle directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff checklist Markdown gates guard cleanup.
- `P11-ACC-123` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `379/399` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff checklist Markdown gates guard cleanup OK; targeted production/docs/release suite `141/141`; backend full suite `669/669`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.373.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-receipt-markdown-verified-files-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-receipt-markdown-verified-files-guard.ps1` now removes its default `tmp/production-evidence-handoff-receipt-markdown-verified-files-guard` bundle directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff receipt Markdown verified files guard cleanup.
- `P11-ACC-122` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `378/398` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff receipt Markdown verified files guard cleanup OK; targeted production/docs/release suite `140/140`; backend full suite `668/668`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.372.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-receipt-verified-files-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-receipt-verified-files-guard.ps1` now removes its default `tmp/production-evidence-handoff-receipt-verified-files-guard` bundle directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff receipt verified files guard cleanup.
- `P11-ACC-121` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `377/397` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff receipt verified files guard cleanup OK; targeted production/docs/release suite `139/139`; backend full suite `667/667`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.371.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-package-latest-release-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-latest-release-guard.ps1` now removes its default `tmp/production-evidence-handoff-package-stale-release-guard` package directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff package latest release guard cleanup.
- `P11-ACC-120` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `376/396` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff package latest release guard cleanup OK; targeted production/docs/release suite `138/138`; backend full suite `666/666`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.370.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-checklist-release-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-checklist-release-guard.ps1` now removes its default `tmp/production-evidence-handoff-checklist-unknown-release-id` bundle directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff checklist release guard cleanup.
- `P11-ACC-119` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `375/395` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff checklist release guard cleanup OK; targeted production/docs/release suite `137/137`; backend full suite `665/665`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.369.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-receipt-release-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-receipt-release-guard.ps1` now removes its default `tmp/production-evidence-handoff-receipt-unknown-release-id` bundle directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff receipt release guard cleanup.
- `P11-ACC-118` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `374/394` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff receipt release guard cleanup OK; targeted production/docs/release suite `136/136`; backend full suite `664/664`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.368.0 - 2026-07-02

Release entry: `2026-07-02-production-evidence-archive-release-guard-cleanup`.

### Fixed
- `test-production-evidence-archive-release-guard.ps1` now removes its default `tmp/production-evidence-archive-unknown-release-id` bundle directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production evidence archive release guard cleanup.
- `P11-ACC-117` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `373/393` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence archive release guard cleanup OK; targeted production/docs/release suite `135/135`; backend full suite `663/663`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.367.0 - 2026-07-02

Release entry: `2026-07-02-production-evidence-manifest-release-guard-cleanup`.

### Fixed
- `test-production-evidence-manifest-release-guard.ps1` now removes its default `tmp/production-evidence-manifest-unknown-release-id` bundle directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production evidence manifest release guard cleanup.
- `P11-ACC-116` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `372/392` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence manifest release guard cleanup OK; targeted production/docs/release suite `134/134`; backend full suite `662/662`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.366.0 - 2026-07-02

Release entry: `2026-07-02-staging-smoke-generator-release-guard-cleanup`.

### Fixed
- `test-staging-smoke-report-generator-release-guard.ps1` now removes the empty default `tmp` directory after the unknown-release regression fails before writing a report.
- `StagingSmokeChecklistTests` pins the staging smoke generator release guard cleanup.
- `P9-TST-007I` documents the local cleanup behavior while parent `P9-TST-007` remains in progress until real staging or VPS smoke evidence exists.

### Notes
- Roadmap progress is now `371/391` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: staging smoke generator release guard cleanup OK; targeted staging/docs/release suite `133/133`; backend full suite `661/661`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.365.0 - 2026-07-02

Release entry: `2026-07-02-staging-smoke-latest-release-guard-cleanup`.

### Fixed
- `test-staging-smoke-report-latest-release-guard.ps1` now removes its default `tmp/staging-smoke-stale-release-guard.json` report and empty `tmp` directory after a local run.
- `StagingSmokeChecklistTests` pins the staging smoke latest-release guard cleanup.
- `P9-TST-007H` documents the local cleanup behavior while parent `P9-TST-007` remains in progress until real staging or VPS smoke evidence exists.

### Notes
- Roadmap progress is now `370/390` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: staging smoke latest release guard cleanup OK; targeted staging/docs/release suite `132/132`; backend full suite `660/660`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.364.0 - 2026-07-02

Release entry: `2026-07-02-production-evidence-bundle-latest-release-guard-cleanup`.

### Fixed
- `test-production-evidence-bundle-latest-release-guard.ps1` now removes its default `tmp/production-evidence-bundle-stale-release-guard` directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production evidence bundle latest-release guard cleanup.
- `P11-ACC-115` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `369/389` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence bundle latest release guard cleanup OK; targeted production/docs/release suite `121/121`; backend full suite `659/659`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.363.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-package-archive-latest-release-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-archive-latest-release-guard.ps1` now removes its default `tmp/production-evidence-handoff-package-archive-stale-release-guard.zip`, package directory and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff package archive latest-release guard cleanup.
- `P11-ACC-114` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `368/388` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff package archive latest release guard cleanup OK; targeted production/docs/release suite `120/120`; backend full suite `658/658`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.362.0 - 2026-07-02

Release entry: `2026-07-02-production-handoff-ci-summary-latest-release-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-archive-ci-summary-latest-release-guard.ps1` now removes its default `tmp/production-evidence-handoff-package-archive-ci-summary-stale-release-guard.json` and `.md` output plus the empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff CI summary latest-release guard cleanup.
- `P11-ACC-113` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `367/387` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff CI summary latest release guard cleanup OK; targeted production/docs/release suite `119/119`; backend full suite `657/657`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.361.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-ci-result-latest-release-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-archive-ci-regression-result-latest-release-guard.ps1` now removes its default `tmp/production-evidence-handoff-package-archive-ci-regression-result-stale-release-guard.json` output and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff CI result latest-release guard cleanup.
- `P11-ACC-112` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `366/386` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff CI result latest release guard cleanup OK; targeted production/docs/release suite `118/118`; backend full suite `656/656`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.360.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-flow-result-latest-release-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-archive-flow-result-latest-release-guard.ps1` now removes its default `tmp/production-evidence-handoff-package-archive-flow-result-stale-release-guard.json` output and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff flow result latest-release guard cleanup.
- `P11-ACC-111` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `365/385` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff flow result latest release guard cleanup OK; targeted production/docs/release suite `117/117`; backend full suite `655/655`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.359.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-checklist-latest-release-guard-cleanup`.

### Fixed
- `test-production-evidence-handoff-checklist-latest-release-guard.ps1` now removes its default `tmp/production-evidence-handoff-checklist-stale-release-guard.json` and `.md` output plus the empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production handoff checklist latest-release guard cleanup.
- `P11-ACC-110` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `364/384` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff checklist latest release guard cleanup OK; targeted production/docs/release suite `116/116`; backend full suite `654/654`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.358.0 - 2026-07-01

Release entry: `2026-07-01-production-readiness-summary-latest-release-guard-cleanup`.

### Fixed
- `test-production-readiness-summary-latest-release-guard.ps1` now removes its default `tmp/production-readiness-summary-stale-release-guard.md` and `.json` output plus the empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production readiness summary latest-release guard cleanup.
- `P11-ACC-109` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `363/383` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production readiness summary latest release guard cleanup OK; targeted production/docs/release suite `115/115`; backend full suite `653/653`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.357.0 - 2026-07-01

Release entry: `2026-07-01-production-readiness-assertion-result-latest-release-guard-cleanup`.

### Fixed
- `test-production-readiness-assertion-result-latest-release-guard.ps1` now removes its default `tmp/production-readiness-assertion-result-stale-release-guard.json` output and empty `tmp` directory after a local run.
- `ProductionReadinessGateTests` pins the production readiness assertion result latest-release guard cleanup.
- `P11-ACC-108` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `362/382` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production readiness assertion result latest release guard cleanup OK; targeted production/docs/release suite `114/114`; backend full suite `652/652`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.356.0 - 2026-07-01

Release entry: `2026-07-01-production-readiness-assertion-ci-regression-default-cleanup`.

### Fixed
- `test-production-readiness-assertion-ci-regression.ps1` now removes its default `tmp/production-readiness-assertion-ci-regression-test` output after a non-JSON local run.
- `ProductionReadinessGateTests` pins the production readiness assertion CI regression default-output cleanup while preserving explicit `-OutputDirectory` and `-WriteJson` evidence flows.
- `P11-ACC-107` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `361/381` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production readiness assertion CI regression default cleanup OK; targeted production/docs/release suite `113/113`; backend full suite `651/651`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.355.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-long-path-default-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-archive-long-path.ps1` now removes its default `tmp/production-evidence-handoff-package-archive-long-release-id-path-regression-test` output after a non-JSON local run.
- `ProductionReadinessGateTests` pins the long-path default-output cleanup while preserving explicit `-OutputDirectory` and `-WriteJson` evidence flows.
- `P11-ACC-106` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `360/380` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive long-path default cleanup OK; targeted production/docs/release suite `112/112`; backend full suite `650/650`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.354.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-ci-regression-default-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-archive-ci-regression.ps1` now removes its default `tmp/production-evidence-handoff-package-archive-ci-regression-test` output after a non-JSON local run.
- `ProductionReadinessGateTests` pins the CI regression default-output cleanup while preserving explicit `-OutputDirectory` and `-WriteJson` evidence flows.
- `P11-ACC-105` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `359/379` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive CI regression default cleanup OK; targeted production/docs/release suite `111/111`; backend full suite `649/649`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.353.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-flow-default-cleanup`.

### Fixed
- `test-production-evidence-handoff-package-archive-flow.ps1` now removes its default `tmp/production-evidence-handoff-package-archive-flow-test` output after a non-JSON local run.
- `ProductionReadinessGateTests` pins the default-output cleanup while preserving explicit `-OutputDirectory` and `-WriteJson` evidence flows.
- `P11-ACC-104` documents the local cleanup behavior while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `358/378` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive default flow cleanup OK; targeted production/docs/release suite `110/110`; backend full suite `648/648`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.352.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-entry-guard-cleanup-coverage`.

### Fixed
- `ProductionReadinessGateTests` now verifies every archive entry guard removes its generated ZIP and empty `tmp` directory.
- All `test-production-evidence-handoff-package-archive-*-entry-guard.ps1` scripts are pinned by one cleanup coverage test.
- `P11-ACC-103` documents the aggregate cleanup coverage while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `357/377` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive entry guard cleanup coverage OK; targeted production/docs/release suite `109/109`; backend full suite `647/647`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.351.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-whitespace-entry-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-whitespace-entry-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the whitespace-entry archive guard.
- `P11-ACC-102` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `356/376` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive whitespace entry guard regression cleanup OK; targeted production/docs/release suite `108/108`; backend full suite `646/646`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.350.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-rooted-entry-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-rooted-entry-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the rooted-entry archive guard.
- `P11-ACC-101` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `355/375` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive rooted entry guard regression cleanup OK; targeted production/docs/release suite `107/107`; backend full suite `645/645`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.349.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-dot-entry-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-dot-entry-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the dot-entry archive guard.
- `P11-ACC-100` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `354/374` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive dot entry guard regression cleanup OK; targeted production/docs/release suite `106/106`; backend full suite `644/644`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.348.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-dotdot-entry-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-dotdot-entry-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the dotdot-entry archive guard.
- `P11-ACC-099` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `353/373` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive dotdot entry guard regression cleanup OK; targeted production/docs/release suite `105/105`; backend full suite `643/643`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.347.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-backslash-entry-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-backslash-entry-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the backslash-entry archive guard.
- `P11-ACC-098` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `352/372` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive backslash entry guard regression cleanup OK; targeted production/docs/release suite `104/104`; backend full suite `642/642`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.346.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-directory-entry-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-directory-entry-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the directory-entry archive guard.
- `P11-ACC-097` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `351/371` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive directory entry guard regression cleanup OK; targeted production/docs/release suite `103/103`; backend full suite `641/641`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.345.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-nested-entry-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-nested-entry-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the nested-entry archive guard.
- `P11-ACC-096` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `350/370` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive nested entry guard regression cleanup OK; targeted production/docs/release suite `102/102`; backend full suite `640/640`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.344.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-duplicate-entry-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-duplicate-entry-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the duplicate-entry archive guard.
- `P11-ACC-095` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `349/369` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive duplicate entry guard regression cleanup OK; targeted production/docs/release suite `101/101`; backend full suite `639/639`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.343.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-entry-case-cleanup`.

### Fixed
- `scripts/test-production-evidence-handoff-package-archive-entry-case-guard.ps1` now removes its generated ZIP and the empty `tmp` directory after the regression run.
- `ProductionReadinessGateTests` pins artifact cleanup for the entry-case archive guard.
- `P11-ACC-094` documents the local cleanup guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `348/368` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive entry case guard regression cleanup OK; targeted production/docs/release suite `100/100`; backend full suite `638/638`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.342.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-entry-case-guard`.

### Fixed
- `validate-production-evidence-handoff-package-archive.ps1` now uses exact ordinal entry-name sets for allowed and seen ZIP entries.
- `scripts/test-production-evidence-handoff-package-archive-entry-case-guard.ps1` creates a ZIP with lowercase `sha256sums.txt` and proves the final handoff package archive validator rejects case mismatches fail-closed.
- `P11-ACC-093` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `347/367` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive entry case guard regression OK; targeted production/docs/release suite `99/99`; backend full suite `637/637`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `550` files, `0` findings; markdown/code encoding check OK.

## 0.341.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-whitespace-entry-guard`.

### Added
- `scripts/test-production-evidence-handoff-package-archive-whitespace-entry-guard.ps1` creates a ZIP with a whitespace-only entry and proves the final handoff package archive validator rejects blank entry names fail-closed.
- `ProductionReadinessGateTests` now pins the whitespace-entry archive guard, validator blank-name check and production readiness gate documentation.
- `P11-ACC-092` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `346/366` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive whitespace entry guard regression OK; targeted production/docs/release suite `98/98`; backend full suite `636/636`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `549` files, `0` findings; markdown/code encoding check OK.

## 0.340.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-rooted-entry-guard`.

### Added
- `scripts/test-production-evidence-handoff-package-archive-rooted-entry-guard.ps1` creates a ZIP with `C:\SHA256SUMS.txt` entry and proves the final handoff package archive validator rejects rooted entries fail-closed.
- `ProductionReadinessGateTests` now pins the rooted-entry archive guard, validator path check and production readiness gate documentation.
- `P11-ACC-091` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `345/365` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive rooted entry guard regression OK; targeted production/docs/release suite `98/98`; backend full suite `635/635`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `548` files, `0` findings; markdown/code encoding check OK.

## 0.339.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-dot-entry-guard`.

### Added
- `scripts/test-production-evidence-handoff-package-archive-dot-entry-guard.ps1` creates a ZIP with `.` entry and proves the final handoff package archive validator rejects dot entries fail-closed.
- `ProductionReadinessGateTests` now pins the dot-entry archive guard, validator file-name-only check and production readiness gate documentation.
- `P11-ACC-090` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `344/364` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive dot entry guard regression OK; targeted production/docs/release suite `97/97`; backend full suite `634/634`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `547` files, `0` findings; markdown/code encoding check OK.

## 0.338.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-dotdot-entry-guard`.

### Added
- `scripts/test-production-evidence-handoff-package-archive-dotdot-entry-guard.ps1` creates a ZIP with `..` entry and proves the final handoff package archive validator rejects dotdot entries fail-closed.
- `ProductionReadinessGateTests` now pins the dotdot-entry archive guard, validator file-name-only check and production readiness gate documentation.
- `P11-ACC-089` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `343/363` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive dotdot entry guard regression OK; targeted production/docs/release suite `96/96`; backend full suite `633/633`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `546` files, `0` findings; markdown/code encoding check OK.

## 0.337.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-backslash-entry-guard`.

### Added
- `scripts/test-production-evidence-handoff-package-archive-backslash-entry-guard.ps1` creates a ZIP with `nested\SHA256SUMS.txt` and proves the final handoff package archive validator rejects Windows-style backslash entries fail-closed.
- `ProductionReadinessGateTests` now pins the backslash-entry archive guard, validator path check and production readiness gate documentation.
- `P11-ACC-088` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `342/362` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive backslash entry guard regression OK; targeted production/docs/release suite `95/95`; backend full suite `632/632`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `545` files, `0` findings; markdown/code encoding check OK.

## 0.336.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-directory-entry-guard`.

### Added
- `scripts/test-production-evidence-handoff-package-archive-directory-entry-guard.ps1` creates a ZIP with `empty-folder/` and proves the final handoff package archive validator rejects directory entries fail-closed.
- `ProductionReadinessGateTests` now pins the directory-entry archive guard, validator file-only check and production readiness gate documentation.
- `P11-ACC-087` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `341/361` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive directory entry guard regression OK; targeted production/docs/release suite `94/94`; backend full suite `631/631`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `544` files, `0` findings; markdown/code encoding check OK.

## 0.335.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-nested-entry-guard`.

### Added
- `scripts/test-production-evidence-handoff-package-archive-nested-entry-guard.ps1` creates a ZIP with `nested/SHA256SUMS.txt` and proves the final handoff package archive validator rejects nested entries fail-closed.
- `ProductionReadinessGateTests` now pins the nested-entry archive guard, validator path check and production readiness gate documentation.
- `P11-ACC-086` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `340/360` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive nested entry guard regression OK; targeted production/docs/release suite `93/93`; backend full suite `630/630`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `543` files, `0` findings; markdown/code encoding check OK.

## 0.334.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-duplicate-entry-guard`.

### Added
- `scripts/test-production-evidence-handoff-package-archive-duplicate-entry-guard.ps1` creates a ZIP with duplicated `SHA256SUMS.txt` entries and proves the final handoff package archive validator fails closed before delegating to package validation.
- `ProductionReadinessGateTests` now pins the duplicate-entry archive guard, validator error text and production readiness gate documentation.
- `P11-ACC-085` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `339/359` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package archive duplicate entry guard regression OK; targeted production/docs/release suite `92/92`; backend full suite `629/629`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `542` files, `0` findings; markdown/code encoding check OK.

## 0.333.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-markdown-files-guard`.

### Fixed
- `scripts/validate-production-evidence-handoff-package.ps1` now rejects package index Markdown that omits artifact file names, byte lengths or SHA256 values from the JSON index.

### Added
- `scripts/test-production-evidence-handoff-package-markdown-files-guard.ps1` proves that a tampered Markdown package index fails validation while JSON, receipt, checklist and ZIP remain valid.
- `P11-ACC-084` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `338/358` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff package markdown files guard regression OK; targeted production/docs/release suite `91/91`; backend full suite `628/628`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `541` files, `0` findings; markdown encoding check OK.

## 0.332.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-checklist-markdown-gates-guard`.

### Fixed
- `scripts/validate-production-evidence-handoff-checklist.ps1` now rejects checklist Markdown that omits gate details or operator actions from the JSON checklist.

### Added
- `scripts/test-production-evidence-handoff-checklist-markdown-gates-guard.ps1` proves that a tampered Markdown checklist fails validation while JSON, receipt and ZIP remain valid.
- `P11-ACC-083` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `337/357` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff checklist markdown gates guard regression OK; targeted production/docs/release suite `90/90`; backend full suite `627/627`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `540` files, `0` findings; markdown encoding check OK.

## 0.331.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-receipt-markdown-verified-files-guard`.

### Fixed
- `scripts/validate-production-evidence-handoff-receipt.ps1` now rejects receipt Markdown that omits verified file names, entry names or SHA256 values.

### Added
- `scripts/test-production-evidence-handoff-receipt-markdown-verified-files-guard.ps1` proves that a tampered Markdown receipt fails validation while JSON and ZIP remain valid.
- `P11-ACC-082` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `336/356` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff receipt markdown verified files guard regression OK; targeted production/docs/release suite `89/89`; backend full suite `626/626`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `539` files, `0` findings; markdown encoding check OK.

## 0.330.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-receipt-verified-files-guard`.

### Fixed
- `scripts/validate-production-evidence-handoff-receipt.ps1` now rejects receipt `verifiedFiles` metadata that does not match the validated archive entries.

### Added
- `scripts/test-production-evidence-handoff-receipt-verified-files-guard.ps1` proves that a tampered receipt `verifiedFiles` hash fails validation before downstream handoff steps.
- `P11-ACC-081` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `335/355` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff receipt verified files guard regression OK; targeted production/docs/release suite `88/88`; backend full suite `625/625`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `538` files, `0` findings; markdown encoding check OK.

## 0.329.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-checklist-release-guard`.

### Fixed
- `scripts/new-production-evidence-handoff-checklist.ps1` now rejects unknown receipt release ids before writing JSON or Markdown checklist artifacts.

### Added
- `scripts/test-production-evidence-handoff-checklist-release-guard.ps1` proves that a tampered archive/receipt pair with an unknown `releaseId` cannot produce checklist artifacts.
- `P11-ACC-080` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `334/354` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff checklist release guard regression OK; targeted production/docs/release suite `87/87`; backend full suite `624/624`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `537` files, `0` findings; markdown encoding check OK.

## 0.328.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-receipt-release-guard`.

### Fixed
- `scripts/new-production-evidence-handoff-receipt.ps1` now rejects unknown archive release ids before writing JSON or Markdown receipt artifacts.

### Added
- `scripts/test-production-evidence-handoff-receipt-release-guard.ps1` proves that a tampered archive manifest with an unknown `releaseId` cannot produce receipt artifacts.
- `P11-ACC-079` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `333/353` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence handoff receipt release guard regression OK; targeted production/docs/release suite `86/86`; backend full suite `623/623`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `536` files, `0` findings; markdown encoding check OK.

## 0.327.0 - 2026-07-01

Release entry: `2026-07-01-production-evidence-archive-release-guard`.

### Fixed
- `scripts/new-production-evidence-archive.ps1` now rejects unknown manifest release ids before writing a production evidence ZIP.

### Added
- `scripts/test-production-evidence-archive-release-guard.ps1` proves that a tampered manifest with an unknown `releaseId` cannot produce an archive.
- `P11-ACC-078` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `332/352` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence archive release guard regression OK; targeted production/docs/release suite `85/85`; backend full suite `622/622`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `535` files, `0` findings; markdown encoding check OK.

## 0.326.0 - 2026-07-01

Release entry: `2026-07-01-production-evidence-manifest-release-guard`.

### Fixed
- `scripts/new-production-evidence-manifest.ps1` now rejects unknown bundle release ids before writing `production-evidence-manifest.json`.

### Added
- `scripts/test-production-evidence-manifest-release-guard.ps1` proves that a tampered bundle with an unknown `releaseId` cannot produce a manifest.
- `P11-ACC-077` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `331/351` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence manifest release guard regression OK; targeted production/docs/release suite `84/84`; backend full suite `621/621`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `534` files, `0` findings; markdown encoding check OK.

## 0.325.0 - 2026-07-01

Release entry: `2026-07-01-production-evidence-bundle-generator-release-guard`.

### Fixed
- `scripts/new-production-evidence-bundle.ps1 -ReleaseId` now rejects unknown manual release ids before creating the output directory or evidence artifacts.

### Added
- `scripts/test-production-evidence-bundle-generator-release-guard.ps1` proves that bundle generation fails fast and leaves no output directory for an unknown `ReleaseId`.
- `P11-ACC-076` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `330/350` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence bundle generator release guard regression OK; targeted production/docs/release suite `83/83`; backend full suite `620/620`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `533` files, `0` findings; markdown encoding check OK.

## 0.324.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-bootstrap-readiness-release-guard`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke-readiness.ps1 -ReleaseId` now rejects unknown manual release ids before validator execution or writing readiness artifacts.

### Added
- `scripts/test-admin-vps-bootstrap-readiness-release-guard.ps1` proves that direct bootstrap readiness fails fast, leaves no readiness/bootstrap JSON artifacts and does not leak the bootstrap password for an unknown `ReleaseId`.
- `P0-ADMIN-001BW` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin bootstrap/smoke evidence exists.

### Notes
- Roadmap progress is now `329/349` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS bootstrap readiness release guard regression OK; targeted admin bootstrap/docs/release suite `29/29`; backend full suite `619/619`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `532` files, `0` findings; markdown encoding check OK.

## 0.323.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-preflight-release-guard`.

### Fixed
- `scripts/admin-vps-smoke-preflight.ps1 -ReleaseId` now rejects unknown manual release ids before remote release checks, validator execution or writing preflight artifacts.

### Added
- `scripts/test-admin-vps-smoke-preflight-release-guard.ps1` proves that direct preflight fails fast, leaves no preflight/smoke JSON artifacts and does not leak the smoke password for an unknown `ReleaseId`.
- `P0-ADMIN-002BH` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `328/348` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke preflight release guard regression OK; targeted admin/docs/release suite `36/36`; backend full suite `618/618`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `531` files, `0` findings; markdown encoding check OK.

## 0.322.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-browser-smoke-direct-release-guard`.

### Fixed
- `scripts/admin-vps-browser-smoke.ps1 -ReleaseId` now rejects unknown manual release ids before setting smoke environment variables, running Playwright or writing report artifacts.

### Added
- `scripts/test-admin-vps-browser-smoke-direct-release-guard.ps1` proves that the direct browser runner fails fast, leaves no JSON artifact and does not leak the smoke password for an unknown `ReleaseId`.
- `P0-ADMIN-002BG` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `327/347` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS browser smoke direct release guard regression OK; targeted admin/docs/release suite `35/35`; backend full suite `617/617`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `530` files, `0` findings; markdown encoding check OK.

## 0.321.0 - 2026-07-01

Release entry: `2026-07-01-vps-production-smoke-generator-release-guard`.

### Fixed
- `scripts/new-vps-production-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a VPS production smoke draft.

### Added
- `scripts/test-vps-production-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P11-ACC-075` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `326/346` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPS production smoke generator release guard regression OK; targeted VPS/docs/release suite `24/24`; backend full suite `616/616`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `529` files, `0` findings; markdown encoding check OK.

## 0.320.0 - 2026-07-01

Release entry: `2026-07-01-staging-smoke-generator-release-guard`.

### Fixed
- `scripts/new-staging-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a staging smoke draft.

### Added
- `scripts/test-staging-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P9-TST-007G` documents the local guard while `P9-TST-007` remains in progress until real staging/VPS smoke evidence exists.

### Notes
- Roadmap progress is now `325/345` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: staging smoke generator release guard regression OK; targeted staging/docs/release suite `25/25`; backend full suite `615/615`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `528` files, `0` findings; markdown encoding check OK.

## 0.319.0 - 2026-07-01

Release entry: `2026-07-01-payment-smoke-generator-release-guard`.

### Fixed
- `scripts/new-payment-provider-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a payment provider smoke draft.

### Added
- `scripts/test-payment-provider-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P0-PAY-016` documents the local guard while `STATE-011` and `P0-PAY-002` ... `P0-PAY-009` remain open until real payment provider smoke evidence exists.

### Notes
- Roadmap progress is now `324/344` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: payment provider smoke generator release guard regression OK; targeted payment/docs/release suite `23/23`; backend full suite `614/614`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `527` files, `0` findings; markdown encoding check OK.

## 0.318.0 - 2026-07-01

Release entry: `2026-07-01-vpn-live-smoke-generator-release-guard`.

### Fixed
- `scripts/new-vpn-live-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a VPN live smoke draft.

### Added
- `scripts/test-vpn-live-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P0-VPN-008` documents the local guard while `STATE-012` and `P0-VPN-001` ... `P0-VPN-005` remain open until real 3x-ui/VPN live smoke evidence exists.

### Notes
- Roadmap progress is now `323/343` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPN live smoke generator release guard regression OK; targeted VPN/docs/release suite `21/21`; backend full suite `613/613`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `526` files, `0` findings; markdown encoding check OK.

## 0.317.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-generator-release-guard`.

### Fixed
- `scripts/new-admin-vps-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a draft report.

### Added
- `scripts/test-admin-vps-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P0-ADMIN-002BF` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `322/342` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke generator release guard regression OK; targeted admin/docs/release suite `34/34`; backend full suite `612/612`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `525` files, `0` findings; markdown encoding check OK.

## 0.316.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-bootstrap-smoke-evidence-latest-release-guard`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` now rejects paired bootstrap readiness/smoke evidence whose `releaseId` does not match the latest active release before accepting strict reports.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-latest-release-guard.ps1` proves that a stale bootstrap evidence chain is rejected.
- `P0-ADMIN-001BV` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin bootstrap/smoke evidence exists.

### Notes
- Roadmap progress is now `321/341` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS bootstrap smoke evidence latest release guard regression OK; targeted admin/bootstrap/docs/release suite `46/46`; backend full suite `611/611`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `524` files, `0` findings; markdown encoding check OK.

## 0.315.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-evidence-latest-release-guard`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` now rejects paired admin VPS smoke evidence whose `releaseId` does not match the latest active release before accepting strict preflight/browser smoke reports.

### Added
- `scripts/test-admin-vps-smoke-evidence-latest-release-guard.ps1` proves that a stale paired evidence chain is rejected.
- `P0-ADMIN-002BE` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `320/340` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke evidence latest release guard regression OK; targeted admin/docs/release suite `33/33`; backend full suite `610/610`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `523` files, `0` findings; markdown encoding check OK.

## 0.314.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-preflight-latest-release-guard`.

### Fixed
- `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady` now rejects ready preflight reports whose `releaseId` does not match the latest active release.

### Added
- `scripts/test-admin-vps-smoke-preflight-latest-release-guard.ps1` proves that a ready preflight report with stale `releaseId` is rejected before browser smoke can start.
- `P0-ADMIN-002BD` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `319/339` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke preflight latest release guard regression OK; targeted admin/docs/release suite `32/32`; backend full suite `609/609`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `522` files, `0` findings; markdown encoding check OK.

## 0.313.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-latest-release-guard`.

### Fixed
- `scripts/validate-production-evidence-handoff-package-archive.ps1 -RequireProductionReady` now rejects handoff package ZIP artifacts whose package index `releaseId` is stale before archive acceptance can continue.

### Added
- `scripts/test-production-evidence-handoff-package-archive-latest-release-guard.ps1` proves that a handoff package archive with stale `releaseId` is rejected.
- `P11-ACC-074` documents the acceptance guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `318/338` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff package archive latest release guard regression OK; targeted production handoff/docs/release suite `81/81`; backend full suite `608/608`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `521` files, `0` findings; markdown encoding check OK.

## 0.312.0 - 2026-07-01

Release entry: `2026-07-01-production-evidence-bundle-latest-release-guard`.

### Fixed
- `scripts/validate-production-evidence-bundle.ps1 -RequireProductionReady` now rejects evidence bundles whose required reports carry stale `releaseId` values before final evidence packaging can continue.

### Added
- `scripts/test-production-evidence-bundle-latest-release-guard.ps1` proves that a production-ready bundle with stale report `releaseId` is rejected.
- `P11-ACC-073` documents the acceptance guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `317/337` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence bundle latest release guard regression OK; targeted production handoff/docs/release suite `80/80`; backend full suite `607/607`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `520` files, `0` findings; markdown encoding check OK.

## 0.311.0 - 2026-07-01

Release entry: `2026-07-01-production-readiness-assertion-latest-release-guard`.

### Fixed
- `scripts/validate-production-readiness-assertion-result.ps1 -RequireProductionReady` now rejects production-ready assertion results whose `releaseId` does not match the latest active release in `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Added
- `scripts/test-production-readiness-assertion-result-latest-release-guard.ps1` proves that a production-ready assertion result with stale `releaseId` is rejected before linked report and Markdown checks.
- `P11-ACC-072` documents the acceptance guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `316/336` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production readiness assertion result latest release guard regression OK; targeted production handoff/docs/release suite `79/79`; backend full suite `606/606`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `519` files, `0` findings; markdown encoding check OK.

## 0.310.0 - 2026-07-01

Release entry: `2026-07-01-production-readiness-summary-latest-release-guard`.

### Added

- `scripts/test-production-readiness-summary-latest-release-guard.ps1` proves that a production-ready summary with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-071` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-readiness-summary.ps1 -RequireProductionReady` now requires the summary `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `315/335` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production readiness summary latest release guard regression OK; targeted production handoff/docs/release suite `78/78`; backend full suite `605/605`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `518` files, `0` findings; markdown encoding check OK.

## 0.309.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-ci-summary-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-summary-latest-release-guard.ps1` proves that a production-ready handoff CI summary with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-070` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1 -RequireProductionReady` now requires the summary result `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `314/334` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff CI summary latest release guard regression OK; targeted production handoff/docs/release suite `77/77`; backend full suite `604/604`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `517` files, `0` findings; markdown encoding check OK.

## 0.308.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-ci-result-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-latest-release-guard.ps1` proves that a production-ready handoff CI result with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-069` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1 -RequireProductionReady` now requires the result `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `313/333` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff CI result latest release guard regression OK; targeted production handoff/docs/release suite `76/76`; backend full suite `603/603`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `516` files, `0` findings; markdown encoding check OK.

## 0.307.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-flow-result-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow-result-latest-release-guard.ps1` proves that a production-ready handoff archive flow result with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-068` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1 -RequireProductionReady` now requires the result `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `312/332` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff flow result latest release guard regression OK; targeted production handoff/docs/release suite `75/75`; backend full suite `602/602`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `515` files, `0` findings; markdown encoding check OK.

## 0.306.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-latest-release-guard.ps1` proves that a production-ready handoff package index with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-067` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-package.ps1 -RequireProductionReady` now requires the package index `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `311/331` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff package latest release guard regression OK; targeted production handoff/docs/release suite `74/74`; backend full suite `601/601`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `514` files, `0` findings; markdown encoding check OK.

## 0.305.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-checklist-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-checklist-latest-release-guard.ps1` proves that a production-ready handoff checklist with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-066` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-checklist.ps1 -RequireProductionReady` now requires the checklist `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `310/330` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff checklist latest release guard regression OK; targeted production handoff/docs/release suite `73/73`; backend full suite `600/600`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `513` files, `0` findings; markdown encoding check OK.

## 0.304.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-bootstrap-smoke-latest-release-guard`.

### Added

- `scripts/test-admin-vps-bootstrap-smoke-latest-release-guard.ps1` proves that ready/passed admin VPS bootstrap smoke reports with stale `releaseId` are rejected.
- Roadmap item `P0-ADMIN-001BU` documents the latest-release acceptance guard while keeping real VPS admin bootstrap/smoke items open for external evidence.

### Changed

- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -RequireReady` now requires the readiness `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` now requires the final bootstrap smoke `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `309/329` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS bootstrap smoke latest release guard regression OK; targeted admin bootstrap/docs/release suite `27/27`; backend full suite `599/599`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `512` files, `0` findings; markdown encoding check OK.

## 0.303.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-latest-release-guard`.

### Added

- `scripts/test-admin-vps-smoke-report-latest-release-guard.ps1` proves that a fully passed admin VPS smoke report with stale `releaseId` is rejected.
- Roadmap item `P0-ADMIN-002BC` documents the latest-release acceptance guard while keeping real VPS admin smoke items open for external evidence.

### Changed

- `scripts/validate-admin-vps-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `308/328` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke latest release guard regression OK; targeted admin VPS/docs/release suite `31/31`; backend full suite `598/598`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `511` files, `0` findings; markdown encoding check OK.

## 0.302.0 - 2026-07-01

Release entry: `2026-07-01-vps-production-smoke-latest-release-guard`.

### Added

- `scripts/test-vps-production-smoke-report-latest-release-guard.ps1` proves that a fully passed VPS production smoke report with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-065` documents the latest-release acceptance guard while keeping `P11-ACC-002` open for real VPS evidence.

### Changed

- `scripts/validate-vps-production-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `307/327` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPS production smoke latest release guard regression OK; targeted VPS production/docs/release suite `23/23`; backend full suite `597/597`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `510` files, `0` findings; markdown encoding check OK.

## 0.301.0 - 2026-07-01

Release entry: `2026-07-01-vpn-live-smoke-latest-release-guard`.

### Added

- `scripts/test-vpn-live-smoke-report-latest-release-guard.ps1` proves that a fully passed VPN live smoke report with stale `releaseId` is rejected.
- Roadmap item `P0-VPN-007` documents the latest-release acceptance guard while keeping real 3x-ui/VPN smoke items open for external evidence.

### Changed

- `scripts/validate-vpn-live-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `306/326` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPN live smoke latest release guard regression OK; targeted VPN live/docs/release suite `20/20`; backend full suite `596/596`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `509` files, `0` findings; markdown encoding check OK.

## 0.300.0 - 2026-07-01

Release entry: `2026-07-01-payment-smoke-latest-release-guard`.

### Added

- `scripts/test-payment-provider-smoke-report-latest-release-guard.ps1` proves that a fully passed payment provider smoke report with stale `releaseId` is rejected.
- Roadmap item `P0-PAY-015` documents the latest-release acceptance guard while keeping real provider smoke items open for external evidence.

### Changed

- `scripts/validate-payment-provider-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `305/325` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: payment provider smoke latest release guard regression OK; targeted payment provider/docs/release suite `22/22`; backend full suite `595/595`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `508` files, `0` findings; markdown encoding check OK.

## 0.299.0 - 2026-07-01

Release entry: `2026-07-01-staging-smoke-latest-release-guard`.

### Added

- `scripts/test-staging-smoke-report-latest-release-guard.ps1` proves that a fully passed staging smoke report with stale `releaseId` is rejected.
- Roadmap item `P9-TST-007F` documents the latest-release acceptance guard while keeping parent `P9-TST-007` open for real staging/VPS evidence.

### Changed

- `scripts/validate-staging-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `304/324` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: staging smoke latest release guard regression OK; targeted backend/docs/release suite `24/24`; backend full suite `594/594`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `507` files, `0` findings.

## Roadmap checkpoint - 2026-06-24

### Documentation

- `docs/PRODUCT_COMPLETION_ROADMAP.md` now records the temporary pause state, last verification date and remaining live/VPS blockers after release `2026-06-24-admin-vps-smoke-preflight-report-id-console`.

### Verification

- Roadmap/docs guard suite: OK, `15/15`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `3` changed/new files.

## 0.298.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-preflight-report-id-console`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now prints sanitized `Preflight report id` next to the preflight summary.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` asserts that stdout report id matches the JSON `reportId` before browser smoke starts.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Admin VPS smoke flow wrapper regression: OK; failed preflight scenarios print `Preflight report id` and match it with JSON `reportId`.
- Admin VPS preflight validator regression: OK; standalone preflight prints `Preflight report id`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-preflight-report-id-console`, console output includes `Preflight report id`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.297.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-remote-message-console`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now prints sanitized `Remote release message` next to remote release status, expected release and actual release.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` asserts the unavailable remote release guidance before browser smoke starts.

### Verification

- Admin VPS smoke flow wrapper regression: OK; `remote-release-mismatch` console output includes the unavailable remote release guidance.
- Admin VPS preflight validator regression: OK; standalone preflight prints `Remote release message`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-remote-message-console`, console output includes `Remote release message`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.296.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-preflight-check-counts`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now writes sanitized `checkCount`, `passedCheckCount` and `failedCheckCount` and prints total/passed/failed check counts before browser smoke.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` validates count consistency against the `checks` array before accepting preflight evidence.
- `scripts/test-admin-vps-smoke-preflight-validator.ps1` covers `mismatched-check-count` and `mismatched-passed-check-count`; wrapper regression asserts the count summary in both report and console output.

### Verification

- Admin VPS preflight validator regression: OK after fixture update; valid reports keep `checkCount=10`, `passedCheckCount=10`, `failedCheckCount=0`, and mismatched total/passed/failed counts are rejected.
- Admin VPS smoke flow wrapper regression: OK; failed preflight scenarios print `Check count`, `Passed checks` and `Failed check count`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-preflight-check-counts`, preflight counts `10/10/0`, remote release status `matched`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.295.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-preflight-failed-count`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now writes sanitized `failedCheckCount` and prints `Failed check count` before browser smoke.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` validates that `failedCheckCount` matches failed `checks` and `failedChecks`.
- `scripts/test-admin-vps-smoke-preflight-validator.ps1` covers `mismatched-failed-check-count`; wrapper regression asserts the count in both report and console output.

### Verification

- Admin VPS preflight validator regression: OK; valid reports keep `failedCheckCount=0`, stale release evidence keeps `failedCheckCount=1`, and `mismatched-failed-check-count` is rejected.
- Admin VPS smoke flow wrapper regression: OK; failed preflight scenarios print `Failed check count` and keep it consistent with `failedChecks`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-preflight-failed-count`, preflight `failedCheckCount=0`, `failedChecks=[]`, remote release status `matched`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.294.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-preflight-failed-checks`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now writes sanitized `failedChecks` and prints `Failed checks` in stdout before browser smoke.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` validates that `failedChecks` exactly matches failed `checks` entries and keeps `readyForLiveSmoke` consistent.
- `scripts/test-admin-vps-smoke-preflight-validator.ps1` covers `mismatched-failed-checks`; wrapper regression asserts the expected failed check in both report and console output.

### Verification

- Admin VPS preflight validator regression: OK; valid reports keep `failedChecks=[]`, stale release evidence keeps `failedChecks=["remote-latest-release"]`, and `mismatched-failed-checks` is rejected.
- Admin VPS smoke flow wrapper regression: OK; failed preflight scenarios print `Failed checks` and store the expected failed check in the preflight report.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-preflight-failed-checks`, preflight `failedChecks=[]`, remote release status `matched`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.293.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-remote-release-console-summary`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now prints safe remote release diagnostics in stdout: status, expected release and actual remote release.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` asserts the console diagnostic for fail-closed remote release scenarios before browser smoke starts.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Admin VPS smoke flow wrapper regression: OK; `remote-release-mismatch` console output includes `Remote release status: unavailable`.
- Local SQLite admin VPS browser smoke: OK; console summary showed `Remote release status: matched`, latest release `2026-06-24-admin-vps-smoke-remote-release-console-summary`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.292.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-remote-release-diagnostics`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now records sanitized `remoteReleaseStatus` and `remoteReleaseMessage` fields for `not-required`, `matched`, `mismatch` and `unavailable` outcomes.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` validates the remote release diagnostic fields and rejects tampered matched reports when `remoteReleaseId` differs from the local smoke `releaseId`.
- Admin VPS wrapper regression now asserts remote release diagnostics before browser smoke starts.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Admin VPS preflight validator regression: OK; valid stale release evidence and `bad-remote-release-status` passed.
- Admin VPS smoke flow wrapper regression: OK; `remote-release-mismatch` stops before browser smoke and records `remoteReleaseStatus=unavailable`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-remote-release-diagnostics`, preflight checks `10/10`, remote release status `matched`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.291.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-remote-release-preflight`.

### Fixed

- `scripts/admin-vps-smoke.ps1` now requires a remote release match before browser smoke.
- `scripts/admin-vps-smoke-preflight.ps1` authenticates through `/api/auth/login`, keeps the bearer token in memory and records only sanitized `remoteReleaseId`/`remoteReleaseMatched` evidence.
- Future-dated local release seeds no longer block the local SQLite admin VPS smoke; the `0.291.0` release timestamp is in the past relative to the 2026-06-24 verification run.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Admin VPS preflight validator regression: OK; `remote-latest-release` is required and secret markers such as `bearer ` are rejected.
- Admin VPS smoke flow wrapper regression: OK; `remote-release-mismatch` stops before browser smoke.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-remote-release-preflight`, preflight checks `10/10`, remote release matched, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- Real VPS read-only inventory: systemd/nginx deploy is live at `83.147.222.145`, API health OK on port `8080`, but installed `/opt/vpn-platform/api/AppReleases/releases.json` does not contain the `0.291.0` release; this is stale-deploy evidence, not a passed smoke report.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.290.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-navigation-fallback`.

### Fixed

- `frontend/e2e/admin-vps-smoke.spec.ts` now opens admin sections through `role=tab`, `role=link` or direct hash fallback from `docs/admin-vps-smoke-sections.json`.
- Legacy deployed admin UIs no longer turn missing navigation items into a generic 120-second Playwright timeout.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-navigation-fallback`, preflight checks `9/9`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- Real VPS admin smoke attempt: preflight OK and admin login passed; sections through `support` passed, then required section `audit` failed to load on the currently deployed VPS UI. `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until a full passed VPS smoke report is captured after deploy.

## 0.289.0 - 2026-06-24

Release entry: `2026-06-24-deploy-production-env-normalizer`.

### Fixed

- `deploy-vps` now normalizes `PRODUCTION_ENV_FILE` before uploading shared `.env` to the VPS.
- Stale production secrets can no longer re-enable `ASPNETCORE_ENVIRONMENT=Local`, auto migrations, demo seed, Swagger or persistent admin bootstrap during Docker/systemd deploy.
- `AdminBootstrap__Password` is cleared from the uploaded shared env file, so one-shot admin reset stays outside the long-lived service configuration.

### Verification

- Deploy production env normalizer regression: OK.
- Targeted deploy/docs/release .NET suite: OK, `18/18`.
- Local SQLite admin bootstrap smoke: OK; latest release `2026-06-24-deploy-production-env-normalizer`, readiness checks `17/17`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `19` changed/new files.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a full real VPS admin bootstrap/login smoke report is captured.

## 0.288.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-readiness-password-env-validator`.

### Fixed

- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` now validates the `passwordEnvName` field itself, not only the generated readiness check.
- Tampered readiness reports with values like `Path` now fail validation even if `password-env-name-safe` is manually left passed.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` now covers `mismatched-readiness-password-env-name-safe`.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK; `16` scenarios passed, including `mismatched-readiness-password-env-name-safe`.
- Targeted docs/release .NET suite: OK, `41/41`.
- Local SQLite admin bootstrap smoke: OK; latest release `2026-06-23-admin-bootstrap-readiness-password-env-validator`, readiness checks `17/17`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `592/592`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `562`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `19` changed/new files.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.287.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-password-env-name-guard`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke.ps1` now rejects password env names that are not safe environment variable identifiers or do not contain `PASSWORD`.
- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now records a failed `password-env-name-safe` check and does not read unsafe env names.
- Wrapper/readiness regressions now cover unsafe password env names without leaking the password or creating smoke artifacts.

### Verification

- Admin VPS bootstrap smoke wrapper regression: OK; `25` scenarios passed, including `bad-password-env-name` fail-fast before smoke artifacts.
- Admin VPS bootstrap smoke readiness regression: OK; `15` scenarios passed, including `bad-password-env-name` with failed `password-env-name-safe` readiness check.
- Targeted docs/release .NET suite: OK, `41/41`.
- Local SQLite admin bootstrap smoke: OK; latest release `2026-06-23-admin-bootstrap-password-env-name-guard`, readiness checks `17/17`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `592/592`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `562`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `22` changed/new files.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.286.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-nonlocal-reset-guard`.

### Fixed

- `scripts/admin-bootstrap.ps1` now requires `-ConfirmBootstrapReset` before direct bootstrap/reset against any non-local database.
- The direct wrapper now fails fast when a non-local run has no `ConnectionString`, before process env setup or bootstrap execution.
- `scripts/test-admin-bootstrap-wrapper.ps1` now covers `missing-confirm-bootstrap-reset` and `missing-connection-string` without leaking the password.

### Verification

- Direct admin bootstrap wrapper regression: OK; scenarios `provider-case-normalized`, `local-sqlite-overrides-provider`, `bad-provider`, `missing-confirm-bootstrap-reset` and `missing-connection-string` passed.
- Targeted docs/release .NET suite: OK, `41/41`.
- Local SQLite admin bootstrap smoke: OK; latest release `2026-06-23-admin-bootstrap-nonlocal-reset-guard`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `592/592`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `562`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.285.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-provider-normalization`.

### Fixed

- `scripts/admin-bootstrap.ps1` now canonicalizes case-insensitive `Postgres`/`Sqlite` provider values and rejects unsupported providers before setting process env values.
- `-LocalSqlite` still forces provider `Sqlite`, so local bootstrap dry-runs cannot inherit an invalid CLI/env provider.
- Added `scripts/test-admin-bootstrap-wrapper.ps1` for provider normalization, local SQLite override and bad-provider fail-fast coverage without password leakage.

### Verification

- Direct admin bootstrap wrapper regression: OK, `provider-case-normalized`, `local-sqlite-overrides-provider` and `bad-provider` passed without password leakage.
- Targeted docs/release unit suite: 41/41.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-bootstrap-provider-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: 592/592.
- Frontend tests: 66/66.
- Frontend typecheck/build/audit: OK, audit 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: OK, files scanned 562, findings 0.
- Changed files encoding: strict UTF-8 without BOM, 17 files checked.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.284.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-profile-normalization`.

### Fixed

- `scripts/admin-bootstrap.ps1` now trims `EnvironmentName`, `Email`, `DisplayName`, `RolesCsv` and `Provider` before process env setup and safe console output.
- `scripts/admin-vps-bootstrap-smoke.ps1` now forwards trimmed `DisplayName` and `RolesCsv` into the bootstrap step while leaving the password and connection string untouched.
- Regression coverage now includes `dry-run-admin-bootstrap-profile-normalized` and static guards for normalized bootstrap profile arguments.

### Verification

- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-admin-bootstrap-profile-normalized` prints `Roles: SuperAdmin` without surrounding whitespace and does not start browser smoke.
- Targeted docs/release unit suite: 40/40.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-bootstrap-profile-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: 591/591.
- Frontend tests: 66/66.
- Frontend typecheck/build/audit: OK, audit 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: OK, files scanned 561, findings 0.
- Changed files encoding: strict UTF-8 without BOM, 19 files checked.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.283.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-workspace-path-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1`, `scripts/admin-vps-smoke-preflight.ps1` and `scripts/admin-vps-browser-smoke.ps1` now trim `ProjectPath`/`FrontendPath` workspace path inputs before local file/directory checks.
- `scripts/admin-vps-bootstrap-smoke.ps1`, `scripts/admin-vps-smoke.ps1` and `scripts/admin-bootstrap.ps1` now pass trimmed workspace paths to downstream scripts while keeping internal path spaces intact.
- `DataProtectionKeyPath` is trimmed before being forwarded to the admin bootstrap process environment.
- Regression coverage now includes `workspace-paths-normalized`, `dry-run-workspace-paths-normalized` and `preflight-workspace-path-normalized`.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `workspace-paths-normalized` accepts padded `ProjectPath` and `FrontendPath`.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-workspace-paths-normalized` passes padded workspace paths through readiness without starting browser smoke.
- Admin VPS smoke flow wrapper regression: OK, `preflight-workspace-path-normalized` reaches the expected password guard instead of failing `frontend-directory`.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-workspace-path-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.282.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-report-path-normalization`.

### Fixed

- `scripts/admin-vps-smoke.ps1` now trims `SmokeReportPath` and `PreflightReportPath` before distinct-path checks, preflight/browser args and evidence validation.
- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` and `scripts/admin-vps-bootstrap-smoke.ps1` now trim readiness, smoke, preflight and bootstrap smoke report paths before writing sanitized evidence links.
- Regression coverage now includes `report-paths-normalized`, `dry-run-report-paths-normalized` and `same-report-paths-normalized`.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `report-paths-normalized` writes trimmed report paths into readiness evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-report-paths-normalized` passes trimmed report paths through the wrapper into readiness evidence.
- Admin VPS smoke flow wrapper regression: OK, `same-report-paths-normalized` rejects report paths that differ only by surrounding whitespace.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-report-path-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.281.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-bootstrap-password-env-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now trims `AdminPasswordEnvName` before checking the password environment variable and writing `passwordEnvName` to readiness evidence.
- `scripts/admin-vps-bootstrap-smoke.ps1` now uses the same trimmed `AdminPasswordEnvName` for password lookup, readiness args and sanitized bootstrap smoke evidence.
- Regression coverage now includes `password-env-name-normalized` and `dry-run-password-env-name-normalized`.
- `scripts/local-admin-vps-bootstrap-smoke.ps1` now checks local ports through a loopback `TcpListener` and stops only its own launched process trees through `taskkill.exe`, avoiding local smoke cleanup hangs on WMI/CIM cmdlets.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `password-env-name-normalized` writes a trimmed password env name into readiness evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-password-env-name-normalized` passes the trimmed password env name through the wrapper into readiness evidence.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-bootstrap-password-env-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.280.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-smoke-wrapper-identity-normalization`.

### Fixed

- `scripts/admin-vps-smoke.ps1` now uses trimmed `ApiBaseUrl`, `AdminWebUrl` and `AdminEmail` in console output, preflight args and browser smoke args.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` covers `preflight-identity-values-normalized`, so preflight evidence cannot keep accidental surrounding whitespace in admin identity fields.

### Verification

- Admin VPS smoke flow wrapper regression: OK, `preflight-identity-values-normalized` writes trimmed API/admin URLs and admin email into preflight evidence; fail-fast scenarios still cover max duration, unknown release id, URL/email, report path, operator/environment defaults, password and frontend guards.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-smoke-wrapper-identity-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.279.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-bootstrap-wrapper-admin-email-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke.ps1` now uses a trimmed `AdminEmail` in console output, readiness args, admin bootstrap args, smoke args and sanitized bootstrap smoke evidence.
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` covers `dry-run-admin-email-normalized`, so the wrapper cannot pass accidental surrounding whitespace into readiness evidence.

### Verification

- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-admin-email-normalized` writes a trimmed admin email into readiness evidence and existing fail-fast scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-bootstrap-wrapper-admin-email-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.278.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-bootstrap-wrapper-url-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke.ps1` now uses trimmed `ApiBaseUrl` and `AdminWebUrl` in console output, readiness args, smoke args and sanitized bootstrap smoke evidence.
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` covers `dry-run-url-values-normalized`, so the wrapper cannot pass accidental surrounding whitespace into readiness evidence.

### Verification

- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-url-values-normalized` writes trimmed API/admin URLs into readiness evidence and existing fail-fast scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-bootstrap-wrapper-url-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.277.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-url-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now trims `ApiBaseUrl` and `AdminWebUrl` before validation output and sanitized readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `url-values-normalized`, so evidence identity cannot keep accidental surrounding whitespace around API/admin URLs.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `url-values-normalized` writes trimmed API/admin URLs and existing fail-closed scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-url-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.276.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-admin-email-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now trims `AdminEmail` before validation output and sanitized readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `admin-email-normalized`, so evidence identity cannot keep accidental surrounding whitespace.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `admin-email-normalized` writes a trimmed admin email and existing fail-closed scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-admin-email-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.275.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-environment-default`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now normalizes empty or whitespace `EnvironmentName` to `Production` before writing sanitized readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `environment-default-normalized`, so standalone readiness reports keep a non-empty environment identity.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `environment-default-normalized` writes `Production` and existing fail-closed scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-environment-default`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.274.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-provider-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now canonicalizes case-insensitive `Postgres`/`Sqlite` provider values before writing sanitized readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `provider-case-normalized`, so standalone readiness accepts `postgres` while unsupported providers remain fail-closed.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `provider-case-normalized` writes canonical `Postgres` and fail-closed scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-provider-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.273.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-provider-mode`.

### Fixed

- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` now rejects readiness reports where `localSqlite=true` but `provider` is not `Sqlite`.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `mismatched-readiness-local-provider`, so tampered local SQLite readiness evidence cannot be accepted.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `mismatched-readiness-local-provider` fails with `provider must be Sqlite when localSqlite is true`.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-provider-mode`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.272.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-ready-flag`.

### Fixed

- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` now rejects readiness reports where `readyForBootstrapSmoke` does not match the actual `checks` array.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `mismatched-readiness-ready-flag` without `-RequireReady`, so standalone sanitized readiness evidence cannot claim bootstrap readiness with failed checks.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `mismatched-readiness-ready-flag` fails with `readyForBootstrapSmoke must match checks`.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-ready-flag`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.271.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-bootstrap-provider-guard`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke.ps1` now fail-fast rejects unsupported non-local `Provider` values before readiness, bootstrap reset and smoke artifacts.
- `-LocalSqlite` now uses a canonical `Sqlite` provider value across readiness, bootstrap and final bootstrap smoke evidence.

### Verified
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-provider` stops before readiness evidence.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-bootstrap-provider-guard`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.270.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-release-id-known-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` now fail-fast rejects a manual `ReleaseId` that is absent from `backend/src/VpnPlatform.Api/AppReleases/releases.json` before preflight/browser smoke artifacts.
- `scripts/admin-vps-bootstrap-smoke.ps1` applies the same known release guard before readiness, bootstrap reset and smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `unknown-release-id` stops before preflight evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `unknown-release-id` stops before readiness evidence.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.269.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-environment-default`.

### Fixed
- `scripts/admin-vps-smoke.ps1` now resolves blank `EnvironmentName` to `staging` before preflight/browser smoke and passes the same value to both reports.
- `scripts/admin-vps-bootstrap-smoke.ps1` now resolves blank `EnvironmentName` to `Production` before readiness/bootstrap/smoke and reuses it in the final bootstrap smoke report.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `default-environment-missing-password` writes `staging` to preflight evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-default-environment` writes `Production` to readiness evidence.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.268.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-operator-default`.

### Fixed
- `scripts/admin-vps-smoke.ps1` now resolves missing `Operator` to `manual-operator` before preflight/browser smoke and passes the same value to both reports.
- `scripts/admin-vps-bootstrap-smoke.ps1` now resolves one operator value before readiness/bootstrap/smoke and reuses it in the final bootstrap smoke report.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `default-operator-missing-password` writes `manual-operator` to preflight evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-default-operator` writes `manual-operator` to readiness evidence.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.267.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-report-path-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` now fail-fast rejects duplicate `SmokeReportPath`/`PreflightReportPath` before preflight/browser smoke and smoke artifacts.
- `scripts/admin-vps-bootstrap-smoke.ps1` applies the same distinct report path guard to smoke, preflight, readiness and bootstrap smoke report paths before readiness, bootstrap reset and smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `same-report-paths` does not create preflight/smoke artifacts.
- Admin VPS bootstrap smoke wrapper regression: OK, `same-report-paths` does not create readiness/bootstrap/smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.266.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-email-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` теперь fail-fast валидирует `AdminEmail` до preflight/browser smoke и не создает preflight report для невалидного email.
- `scripts/admin-vps-bootstrap-smoke.ps1` применяет тот же email guard до readiness, bootstrap reset, передачи пароля и smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `bad-admin-email` не создает preflight/smoke artifacts.
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-admin-email` не создает readiness/bootstrap/smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.265.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-url-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` теперь fail-fast валидирует `ApiBaseUrl` и `AdminWebUrl` как absolute http/https URL до preflight/browser smoke.
- `scripts/admin-vps-bootstrap-smoke.ps1` применяет тот же URL guard до readiness, bootstrap reset, передачи пароля и smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `bad-api-url` и `bad-admin-web-url` не создают preflight/smoke artifacts.
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-api-url` и `bad-admin-web-url` не создают readiness/bootstrap/smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.264.0 - 2026-06-23

Release entry: `2026-06-23-local-admin-bootstrap-port-guard`.

### Fixed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь явно валидирует `ApiPort` и `AdminPort` как TCP-порты 1..65535 и требует разные значения.
- Локальный wrapper fail-fast останавливается до создания `tmp/local-admin-vps-bootstrap-smoke`, локальной SQLite DB, API/admin web и smoke artifacts при нечисловых, вне диапазона или совпадающих портах.

### Verified
- Local admin VPS bootstrap smoke wrapper regression: OK, `format-api-port`, `too-low-api-port`, `too-high-admin-port` и `same-api-admin-port` покрыты.
- `AdminBootstrapCliScriptTests`: 10/10.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.263.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-max-duration-format-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1`, `scripts/admin-vps-bootstrap-smoke.ps1` и `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь явно парсят `MaxEvidenceChainMinutes` и возвращают единое сообщение `MaxEvidenceChainMinutes must be an integer.` для нечисловых CLI/env значений.
- `scripts/validate-admin-vps-smoke-evidence.ps1` и `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` получили такой же standalone format guard без PowerShell binding error.

### Verified
- Admin VPS smoke/bootstrap/local wrapper regressions: OK, `format-max-evidence-chain-minutes` и `format-env-max-evidence-chain-minutes` не создают preflight/readiness/local smoke artifacts.
- Admin VPS smoke/bootstrap evidence validator regressions: OK, `format-max-evidence-chain-minutes` покрыт.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-23-admin-vps-max-duration-format-guard`, smoke sections `16/16`, `MaxEvidenceChainMinutes=120`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

Все заметные изменения проекта фиксируются в этом файле и в разделе "Что нового" внутри приложения. Подробный рабочий roadmap находится в `docs/PRODUCT_COMPLETION_ROADMAP.md`.

## 0.262.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-evidence-explicit-max-duration-guard`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` и `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` больше не зависят от PowerShell `ValidateRange` для `MaxEvidenceChainMinutes`.
- Standalone evidence validators теперь явно отклоняют `MaxEvidenceChainMinutes <= 0` и `> 1440` с едиными fail-fast сообщениями до чтения evidence reports.

### Verified
- Admin VPS smoke/bootstrap evidence validator regressions: OK, `bad-max-evidence-chain-minutes` и `too-high-max-evidence-chain-minutes` покрыты.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-evidence-explicit-max-duration-guard`, smoke sections `16/16`, `MaxEvidenceChainMinutes=120`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.261.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-env-upper-bound-guard`.

### Fixed
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает env upper-bound сценарий `too-high-env-max-evidence-chain-minutes` для `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES=1441`.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` и `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1` зеркально проверяют, что env upper-bound guard не создает preflight/local smoke artifacts.

### Verified
- Admin VPS bootstrap/smoke/local wrapper regressions: OK, CLI/env/upper-bound max duration guard scenarios покрыты.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-smoke-env-upper-bound-guard`, smoke sections `16/16`, `MaxEvidenceChainMinutes=120`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.260.0 - 2026-06-22

Release entry: `2026-06-22-local-admin-bootstrap-smoke-explicit-max-duration-guard`.

### Fixed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь fail-fast отклоняет `MaxEvidenceChainMinutes > 1440` до проверки портов, запуска API/admin web, создания локальной SQLite DB и smoke artifacts.
- `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает `too-high-max-evidence-chain-minutes` и проверяет, что local smoke artifacts не создаются.

### Verified
- Local admin VPS bootstrap smoke wrapper regression: OK, CLI/env/upper-bound max duration guard scenarios покрыты.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.259.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-explicit-max-duration-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` больше не зависит от PowerShell `ValidateRange` для `MaxEvidenceChainMinutes`: CLI/env ошибки теперь возвращают единые fail-fast сообщения до preflight/browser smoke artifacts.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` покрывает `too-high-max-evidence-chain-minutes` и проверяет, что `MaxEvidenceChainMinutes > 1440` не создает preflight/smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, CLI/env max duration guard и upper-bound scenario покрыты.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS admin smoke report.

## 0.258.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-env-guard`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь явно fail-fast отклоняет неположительный `MaxEvidenceChainMinutes` из CLI или `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES` до readiness, bootstrap reset и smoke artifacts.
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` проверяет точное сообщение `MaxEvidenceChainMinutes must be greater than 0` и отсутствие readiness/smoke artifacts для CLI/env fail-fast сценариев.

### Verified
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` и `bad-env-max-evidence-chain-minutes` покрыты до readiness/smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.257.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-env-max-duration`.

### Fixed
- `scripts/admin-vps-smoke.ps1` теперь явно fail-fast отклоняет `MaxEvidenceChainMinutes <= 0` до вывода "flow is ready", preflight report и browser smoke, включая default из `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES`.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` покрывает `bad-env-max-evidence-chain-minutes` и проверяет, что env-лимит не создает preflight/smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `bad-max-evidence-chain-minutes` и `bad-env-max-evidence-chain-minutes` покрыты.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS admin smoke report.

## 0.256.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-env-max-duration`.

### Fixed
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает `bad-env-max-evidence-chain-minutes`, чтобы неверный `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES` fail-fast останавливал production bootstrap smoke wrapper до readiness/smoke artifacts.
- Regression harness очищает env-лимит для остальных сценариев, чтобы CLI/env проверки `MaxEvidenceChainMinutes` не влияли друг на друга.

### Verified
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` и `bad-env-max-evidence-chain-minutes` покрыты без запуска smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.255.0 - 2026-06-22

Release entry: `2026-06-22-local-admin-bootstrap-smoke-env-max-duration`.

### Fixed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь использует `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES` как default для `MaxEvidenceChainMinutes`, если параметр не передан явно.
- `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает `bad-env-max-evidence-chain-minutes`, чтобы неверный env-лимит fail-fast останавливал локальный wrapper до smoke artifacts.

### Verified
- Local admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` и `bad-env-max-evidence-chain-minutes` покрыты.
- Local CLI bootstrap admin smoke на SQLite: OK с `MaxEvidenceChainMinutes=120`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.254.0 - 2026-06-22

Release entry: `2026-06-22-local-admin-bootstrap-smoke-wrapper-regression`.

### Fixed
- Добавлен `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1`, который проверяет fail-fast сценарий `bad-max-evidence-chain-minutes` для локального SQLite bootstrap smoke wrapper.
- Regression harness доказывает, что неверный `MaxEvidenceChainMinutes` останавливает локальный wrapper до запуска API/admin web, browser smoke и создания `tmp/local-admin-vps-bootstrap-smoke` artifacts.

### Verified
- Local admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` покрыт.
- `AdminBootstrapCliScriptTests`: покрывает новый regression harness.
- Backend full suite: 591/591.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.253.0 - 2026-06-22

Release entry: `2026-06-22-local-admin-bootstrap-smoke-max-duration`.

### Fixed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` принимает `MaxEvidenceChainMinutes`, заранее отклоняет неположительный лимит и передает его в общий `scripts/admin-vps-bootstrap-smoke.ps1`.
- Локальная SQLite-проверка admin bootstrap/login теперь явно доказывает тот же fail-closed срок годности evidence chain, который используется для operator/VPS wrappers.

### Verified
- Local CLI bootstrap admin smoke на SQLite: OK с `MaxEvidenceChainMinutes=120`, smoke sections `16/16`.
- `AdminBootstrapCliScriptTests`: покрывает локальный wrapper и передачу `-MaxEvidenceChainMinutes`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.252.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-wrapper-max-duration`.

### Fixed
- `scripts/admin-vps-smoke.ps1` и `scripts/admin-vps-bootstrap-smoke.ps1` принимают `MaxEvidenceChainMinutes`, печатают примененный лимит без секретов и передают его в evidence validators.
- Wrapper regression harness добавил fail-fast сценарии `bad-max-evidence-chain-minutes`, чтобы операторский запуск не стартовал preflight/smoke при неверном лимите.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `bad-max-evidence-chain-minutes` не создает preflight report.
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` не создает smoke artifacts.
- `AdminVpsSmokeReportTests|AdminBootstrapCliScriptTests`: 24/24.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.251.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-chain-max-duration`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` и `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` получили fail-closed лимит `MaxEvidenceChainMinutes`, чтобы standalone evidence не принимал слишком растянутую или устаревшую цепочку preflight/smoke/bootstrap.
- Success summary теперь показывает `maxEvidenceChainMinutes`, чтобы оператор видел примененный срок годности evidence bundle без секретов.

### Verified
- Admin VPS smoke evidence validator regression: OK, сценарий `evidence-chain-duration-exceeds-max` отклоняет цепочку длиннее лимита.
- Admin VPS bootstrap smoke evidence validator regression: OK, сценарий `evidence-chain-duration-exceeds-max` отклоняет readiness/preflight/smoke/bootstrap bundle длиннее лимита.
- `AdminVpsSmokeReportTests|AdminBootstrapCliScriptTests`: 24/24.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-chain-max-duration`, smoke sections `16/16`, smoke/bootstrap evidence validators with expected SHA256 and max duration guard OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.250.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-chronology-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет в sanitized summary standalone admin smoke evidence порядок `preflight|smoke`.
- Admin VPS smoke evidence summary теперь показывает `evidenceChainDurationSeconds`, чтобы оператор видел полную длительность от preflight до завершения smoke без секретов.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary содержит chronology fields и full-chain duration.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-chronology-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and chronology summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.249.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-chronology-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет в sanitized summary полный порядок evidence chain `readiness|preflight|smoke|bootstrap`.
- Bootstrap evidence summary теперь показывает `readinessToPreflightSeconds`, `smokeToBootstrapSeconds` и `evidenceChainDurationSeconds`, чтобы оператор видел порядок и длительность всей цепочки без секретов.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary содержит chronology fields и новые duration metrics.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-chronology-summary`, smoke sections `16/16`, bootstrap evidence validator with expected SHA256 and chronology summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.248.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed сверяет timestamp-суффиксы readiness/bootstrap/preflight/smoke report ids с `generatedAt`/`startedAt` соответствующих reports.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает tamper-сценарии `mismatched-readiness-report-id-timestamp`, `mismatched-bootstrap-report-id-timestamp`, `mismatched-preflight-report-id-timestamp` и `mismatched-smoke-report-id-timestamp`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, report id timestamp mismatches отклоняются fail-closed для readiness/bootstrap/preflight/smoke reports.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp-link`, smoke sections `16/16`, bootstrap evidence validator with expected SHA256 and report id timestamp link checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.247.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp-link`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed сверяет timestamp-суффикс `preflight.reportId` с `preflight.generatedAt`, а `smoke.reportId` с `smoke.startedAt`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарии `mismatched-preflight-report-id-timestamp` и `mismatched-smoke-report-id-timestamp`.

### Verified
- Admin VPS smoke evidence validator regression: OK, `mismatched-preflight-report-id-timestamp` и `mismatched-smoke-report-id-timestamp` отклоняются fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp-link`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and report id timestamp link checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.246.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed отклоняет linked preflight/smoke evidence, если report ids имеют правильный префикс, но не соответствуют timestamp-формату `yyyyMMdd-HHmmss`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` переведен на timestamp ids в valid fixtures и покрывает tamper-сценарии `bad-preflight-report-id-timestamp` и `bad-smoke-report-id-timestamp`.

### Verified
- Admin VPS smoke evidence validator regression: OK, bad report id timestamp suffixes отклоняются fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and report id timestamp checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.245.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed отклоняет bootstrap evidence chain, если readiness/bootstrap/preflight/smoke report ids имеют правильный префикс, но не соответствуют timestamp-формату `yyyyMMdd-HHmmss`.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` переведен на timestamp ids в valid fixtures и покрывает tamper-сценарии `bad-readiness-report-id-timestamp`, `bad-bootstrap-report-id-timestamp`, `bad-preflight-report-id-timestamp` и `bad-smoke-report-id-timestamp`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, bad report id timestamp suffixes отклоняются fail-closed.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp`, smoke sections `16/16`, bootstrap evidence validator with expected SHA256 and report id timestamp checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.244.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-report-id-prefix`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed отклоняет bootstrap evidence chain, если readiness/bootstrap/preflight/smoke report ids совпадают или не соответствуют ожидаемым префиксам.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает tamper-сценарии `duplicate-report-id`, `bad-readiness-report-id-prefix`, `bad-bootstrap-report-id-prefix`, `bad-preflight-report-id-prefix` и `bad-smoke-report-id-prefix`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, duplicate/bad report id prefixes отклоняются fail-closed.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-report-id-prefix`, smoke sections `16/16`, bootstrap evidence validator with expected SHA256 and report id prefix checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.243.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-prefix`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed отклоняет linked preflight/smoke evidence, если `preflight.reportId` не начинается с `admin-vps-smoke-preflight-` или `smoke.reportId` использует чужой prefix.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарии `bad-preflight-report-id-prefix` и `bad-smoke-report-id-prefix`.

### Verified
- Admin VPS smoke evidence validator regression: OK, bad report id prefixes отклоняются fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-prefix`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and report id prefix checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.242.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-uniqueness`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed отклоняет linked preflight/smoke evidence, если `preflight.reportId` и `smoke.reportId` пустые или совпадают.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарий `duplicate-report-id` и подтверждает, что paired evidence нельзя принять с одинаковыми report ids.

### Verified
- Admin VPS smoke evidence validator regression: OK, `duplicate-report-id` отклоняется fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-uniqueness`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and unique report ids OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.241.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-gate-flags-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `accountBootstrapChecked`, `adminLoginPassed`, `noJsErrors` и `noUnauthorizedAfterLogin` в sanitized success summary.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет gate flags synthetic valid-сценария и их `true` значения.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `accountBootstrapChecked`, `adminLoginPassed`, `noJsErrors` и `noUnauthorizedAfterLogin`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-gate-flags-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and gate flags summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.240.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-status-counts-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `passed`, `failed`, `blocked` и `skipped` в sanitized success summary рядом с `sections`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет status counters synthetic valid-сценария: `sections=16`, `passed=16`, `failed=0`, `blocked=0`, `skipped=0`.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `sections=16`, `passed=16`, `failed=0`, `blocked=0`, `skipped=0`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-status-counts-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and status counters summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.239.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `preflightReportId` и `smokeReportId` в sanitized success summary после сверки linked preflight/smoke reports.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет report id fields и значения synthetic valid-сценария.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `preflightReportId` и `smokeReportId`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and report id summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.238.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-identity-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `adminEmail` и `operator` в sanitized success summary после сверки preflight/smoke identity.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет identity fields и значения synthetic valid-сценария.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `adminEmail` и `operator`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-identity-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and identity summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.237.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-duration-order`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed проверяет, что linked smoke report не завершился раньше `startedAt`, чтобы sanitized duration summary не мог принять отрицательную длительность.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарий `smoke-completed-before-started`.

### Verified
- Admin VPS smoke evidence validator regression: OK, `smoke-completed-before-started` отклоняется fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-duration-order`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and positive duration order OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.236.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-duration-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь добавляет `preflightGeneratedAt`, `smokeStartedAt`, `smokeCompletedAt`, `preflightToSmokeSeconds` и `smokeDurationSeconds` в sanitized success summary.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет duration metrics и ожидаемые значения synthetic valid-сценария.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает smoke timing и duration metrics.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-duration-summary`, smoke sections `16/16`, smoke evidence validator with SHA256 and duration metrics OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.235.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-expected-fingerprint`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь добавляет `preflightReportSha256` и `smokeReportSha256` в sanitized success summary.
- `scripts/validate-admin-vps-smoke-evidence.ps1` принимает expected SHA256 fingerprints `ExpectedPreflightReportSha256` и `ExpectedSmokeReportSha256` и fail-closed отклоняет bundle при несовпадении.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет success-сценарий с корректными expected SHA256 и fail-сценарий при несовпадении preflight fingerprint.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid expected SHA256 accepted, mismatched expected preflight SHA256 rejected.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-expected-fingerprint`, smoke sections `16/16`, smoke evidence validator with expected SHA256 OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.234.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-expected-fingerprint`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь принимает expected SHA256 fingerprints `ExpectedReadinessReportSha256`, `ExpectedBootstrapSmokeReportSha256`, `ExpectedPreflightReportSha256` и `ExpectedSmokeReportSha256`.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет success-сценарий с корректными expected SHA256 и fail-closed сценарий при несовпадении readiness fingerprint.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid expected SHA256 accepted, mismatched expected readiness SHA256 rejected.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-expected-fingerprint`, smoke sections `16/16`, paired evidence validator with expected SHA256 OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.233.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-fingerprint-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет SHA256 fingerprints `readinessReportSha256`, `bootstrapSmokeReportSha256`, `preflightReportSha256` и `smokeReportSha256` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` и `AdminBootstrapCliScriptTests` закрепляют наличие SHA256 fingerprints в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает SHA256 fingerprints.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-fingerprint-summary`, smoke sections `16/16`, paired evidence validator OK, summary содержит SHA256 fingerprints.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.232.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-duration-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет duration metrics `preflightToSmokeSeconds`, `smokeDurationSeconds`, `bootstrapDurationSeconds` и `readinessToBootstrapSeconds` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет duration metrics и ожидаемые значения synthetic valid-сценария.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает duration metrics и счетчики sections.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-duration-summary`, smoke sections `16/16`, paired evidence validator OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.231.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-smoke-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет linked preflight/smoke ids, smoke timing и счетчики admin sections в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет smoke summary fields в valid-сценарии.
- Smoke/report wrappers сортируют latest release через `DateTimeOffset.Parse`, поэтому timestamp с миллисекундами в `releases.json` корректно выбирается как актуальный release.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает linked smoke/preflight ids, timing и счетчики sections.
- `AdminBootstrapCliScriptTests`: 9/9.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-smoke-summary`, smoke sections `16/16`, paired evidence validator OK.
- Targeted release/docs suite: 65/65.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.230.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-timing-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `readinessReportId`, `bootstrapSmokeReportId`, `readinessGeneratedAt`, `bootstrapGeneratedAt` и `bootstrapCompletedAt` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет report id и timing fields в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает report id и timing fields.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил report id и timing fields в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.229.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-readiness-inputs-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `passwordEnvName`, `passwordLengthOk`, `connectionStringPresent` и `applyMigrations` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет readiness input fields в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `passwordEnvName`, `passwordLengthOk`, `connectionStringPresent` и `applyMigrations`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил readiness input fields в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.228.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-reset-flags-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `passwordEnvPresent`, `confirmBootstrapReset` и `bootstrapResetConfirmed` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет reset flags в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `passwordEnvPresent`, `confirmBootstrapReset` и `bootstrapResetConfirmed`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил reset flags в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.227.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-status-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `readyForBootstrapSmoke` и `bootstrapStatus` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет status-поля в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `readyForBootstrapSmoke` и `bootstrapStatus`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил status-поля в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.226.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-operator-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `operator` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет `operator` в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `apiBaseUrl`, `adminWebUrl`, `adminEmail`, `operator`, `preflightReportPath` и `sectionsContractPath`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил `operator` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.225.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-identity-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `apiBaseUrl`, `adminWebUrl` и `adminEmail` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет identity-поля в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `apiBaseUrl`, `adminWebUrl`, `adminEmail`, `preflightReportPath` и `sectionsContractPath`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил identity-поля в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.224.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-sections-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `sectionsContractPath` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет `sectionsContractPath` в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `sectionsContractPath`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил `sectionsContractPath` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.223.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-sections-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь добавляет `sectionsContractPath` в sanitized success summary.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет `sectionsContractPath` в captured valid output.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `sectionsContractPath`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local admin VPS smoke на SQLite: OK; paired evidence validator подтвердил `sectionsContractPath` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.222.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-preflight-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь добавляет `preflightReportPath` в sanitized success summary.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` захватывает PowerShell information stream и проверяет `preflightReportPath` в valid output.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `preflightReportPath`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local admin VPS smoke на SQLite: OK; paired evidence validator подтвердил `preflightReportPath` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9 after rerun, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.221.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-preflight-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `preflightReportPath` в sanitized success summary рядом с readiness/bootstrap/smoke paths.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет `preflightReportPath` в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `preflightReportPath`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил `preflightReportPath` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.220.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-preflight-timing-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь fail-closed требует, чтобы linked preflight `generatedAt` не был раньше readiness `generatedAt`.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `preflight-generated-before-readiness`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `preflight-generated-before-readiness`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил readiness -> preflight timing link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.219.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-smoke-path-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь fail-closed сверяет readiness `smokeReportPath` и `preflightReportPath` с итоговым bootstrap smoke report.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `mismatched-readiness-smoke-report-path` и `mismatched-readiness-preflight-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-readiness-smoke-report-path` и `mismatched-readiness-preflight-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил readiness linked smoke/preflight paths.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.218.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-timing-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь fail-closed требует, чтобы `generatedAt` итогового bootstrap smoke report не был раньше linked smoke `completedAt`.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `bootstrap-generated-before-smoke-completed` и фиксирует новый timing gate в bootstrap evidence chain.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `bootstrap-generated-before-smoke-completed`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил timing link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.217.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-timing-link`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь fail-closed требует, чтобы smoke `startedAt` не был раньше preflight `generatedAt`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` пишет `smokeReportPath` в synthetic smoke fixture и покрывает `smoke-started-before-preflight`.

### Verified
- Admin VPS smoke evidence validator regression: OK, включая `smoke-started-before-preflight`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; admin smoke evidence validator подтвердил timing link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.216.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-metadata-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь сверяет `provider`, `passwordEnvName`, `localSqlite` и `confirmBootstrapReset` итогового bootstrap smoke report с readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-readiness-provider`, `mismatched-readiness-password-env-name`, `mismatched-readiness-local-sqlite` и `mismatched-readiness-confirm-bootstrap-reset`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая новые readiness metadata mismatch сценарии.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил readiness metadata link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.215.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-chain-validate`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь валидирует связанный readiness report перед paired evidence validation.
- Bootstrap smoke report validator сверяет `apiBaseUrl`, `adminWebUrl`, `environmentName`, `operator`, `adminEmail`, `releaseId` и `readiness.bootstrapSmokeReportPath` с readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `missing-bootstrap-readiness-report-link` и `mismatched-readiness-bootstrap-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `missing-bootstrap-readiness-report-link` и `mismatched-readiness-bootstrap-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил readiness-chain link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.214.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-self-validate`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -RequireReady` теперь сверяет `readinessReportPath` с фактическим `-ReportPath`.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` покрывает fail-closed `mismatched-readiness-report-self-link` для standalone readiness validation.

### Verified
- Admin VPS bootstrap smoke readiness regression: OK, включая `mismatched-readiness-report-self-link`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; standalone readiness validator подтвердил `readinessReportPath`.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.213.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-report-self-validate`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь сверяет `bootstrapSmokeReportPath` с фактическим `-ReportPath` до paired evidence validation.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` фиксирует, что `mismatched-bootstrap-smoke-report-path` падает на standalone bootstrap report self-link.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-smoke-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; standalone bootstrap report validator подтвердил `bootstrapSmokeReportPath`.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.212.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-environment-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` теперь сверяет `apiBaseUrl`, `adminWebUrl`, `environmentName` и `operator` итогового bootstrap smoke report с preflight и browser smoke reports.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-bootstrap-environment`, чтобы нельзя было смешать bootstrap evidence одного окружения со smoke evidence другого.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-environment`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; readiness/bootstrap/preflight/browser smoke reports связаны по окружению, URL, оператору, `adminEmail` и `releaseId`.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.211.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-admin-email-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` теперь сверяет `adminEmail` итогового bootstrap smoke report с preflight и browser smoke reports.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` пишет `smokeReportPath` в synthetic smoke report и покрывает fail-closed `mismatched-bootstrap-admin-email`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-admin-email`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; readiness/bootstrap/preflight/browser smoke reports связаны по `adminEmail`.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.210.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-report-self-link`.

### Fixed
- `frontend/e2e/admin-vps-smoke.spec.ts`, `docs/admin-vps-smoke-report.template.json` и `scripts/new-admin-vps-smoke-report.ps1` теперь пишут `smokeReportPath` в browser smoke report.
- `scripts/validate-admin-vps-smoke-report.ps1` требует `smokeReportPath` и сверяет его с фактически проверяемым smoke JSON.
- `frontend/e2e/admin.spec.ts` больше не использует внешний `pay.example.test` в sandbox payment fixture, чтобы console E2E не зависел от DNS.

### Added
- `scripts/test-admin-vps-smoke-report-validator.ps1` покрывает fail-closed `mismatched-smoke-report-path`.

### Verified
- Admin VPS smoke report validator regression: OK, включая `mismatched-smoke-report-path`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, `smokeReportPath` связан с фактическим smoke JSON.
- Backend full suite: 590/590; frontend tests: 66/66; console E2E: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.209.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-preflight-self-link`.

### Fixed
- `scripts/admin-vps-smoke-preflight.ps1` теперь пишет `preflightReportPath` в preflight report.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` требует `preflightReportPath`, а `scripts/validate-admin-vps-smoke-evidence.ps1` сверяет его с фактически проверяемым preflight JSON.

### Added
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-preflight-report-path`.

### Verified
- Admin VPS smoke evidence validator regression: OK, включая `mismatched-preflight-report-path`.
- `AdminVpsSmokeReportTests`: 15/15.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.208.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-report-self-link`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь пишет `bootstrapSmokeReportPath` в итоговый bootstrap smoke report.
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` требует `bootstrapSmokeReportPath`, а `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` сверяет его с фактически проверяемым bootstrap JSON.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-bootstrap-smoke-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-smoke-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.207.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-report-link`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь пишет `readinessReportPath` в итоговый bootstrap smoke report.
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` требует `readinessReportPath`, а `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` сверяет его с фактически проверяемым readiness JSON.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-bootstrap-readiness-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-readiness-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.206.0 - 2026-06-20

Release entry: `2026-06-20-admin-vps-smoke-admin-email-evidence`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь сверяет `adminEmail` preflight report с `adminEmail` browser smoke report.
- `scripts/validate-admin-vps-smoke-report.ps1` требует `adminEmail` в smoke report.

### Added
- `frontend/e2e/admin-vps-smoke.spec.ts`, `scripts/new-admin-vps-smoke-report.ps1` и `docs/admin-vps-smoke-report.template.json` пишут sanitized `adminEmail` в smoke report.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-admin-email`.

### Verified
- Admin VPS smoke report validator regression: OK.
- Admin VPS smoke evidence validator regression: OK, включая `mismatched-admin-email`.
- Admin VPS bootstrap smoke evidence validator regression: OK.
- `AdminVpsSmokeReportTests|AdminBootstrapCliScriptTests`: 24/24.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.205.0 - 2026-06-20

Release entry: `2026-06-20-admin-vps-bootstrap-smoke-readiness-path-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь сверяет `readinessReportPath` внутри readiness report с фактическим readiness JSON, переданным в validator.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-readiness-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-readiness-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.204.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-report-release-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` теперь сверяет `releaseId` итогового bootstrap smoke report с `releaseId` preflight и smoke reports после успешной проверки smoke evidence.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `mismatched-smoke-release-id`, где preflight/smoke согласованы между собой, но не совпадают с bootstrap report.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-release-id` и `mismatched-smoke-release-id`.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.203.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-release-id-chain`.

### Changed
- `scripts/admin-vps-bootstrap-smoke.ps1` вычисляет latest release один раз и передает общий `releaseValue` в readiness gate, admin VPS smoke и итоговый bootstrap smoke report.
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь сверяет `releaseId` readiness/bootstrap reports и отклоняет mismatched evidence.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-release-id`.
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` проверяет, что dry-run readiness report получает непустой release id без запуска smoke.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-release-id`.
- Admin VPS bootstrap smoke wrapper regression: OK, dry-run readiness содержит непустой release id.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.202.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-unified-release-id`.

### Changed
- `scripts/admin-vps-smoke.ps1` вычисляет latest release один раз и передает общий `releaseValue` в preflight и browser smoke.
- `scripts/admin-vps-browser-smoke.ps1` получил PowerShell fallback на latest release и печатает выбранный release id без секретов.

### Added
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` проверяет, что fail-closed preflight reports получают непустой release id до запуска browser smoke.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `missing-password`, `bad-api-url`, `missing-frontend`, все с непустым release id.
- `AdminVpsSmokeReportTests`: 15/15.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.201.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-preflight-release-id`.

### Changed
- `scripts/admin-vps-smoke-preflight.ps1` теперь подставляет latest release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, если `-ReleaseId` не передан.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` требует непустой `releaseId`.
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed отклоняет preflight/smoke evidence без release id.

### Added
- `scripts/test-admin-vps-smoke-preflight-validator.ps1` покрывает `empty-release-id`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает `missing-preflight-release-id`.

### Verified
- Admin VPS smoke preflight validator regression: OK, включая `empty-release-id`.
- Admin VPS smoke evidence validator regression: OK, включая `missing-preflight-release-id`.
- `AdminVpsSmokeReportTests`: 15/15.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.200.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-route-regression`.

### Fixed
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` снова проходит valid-сценарий после route contract: synthetic smoke report генерирует route в формате `/admin/#<id>`.

### Added
- Regression harness покрывает fail-closed tamper-сценарий `bad-smoke-route`, который доказывает, что bootstrap evidence chain не примет smoke report с route вне `docs/admin-vps-smoke-sections.json`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, `valid`, `mismatched-admin-url`, `readiness-not-ready`, `bad-timing`, `bad-smoke-route`.
- `AdminBootstrapCliScriptTests|AdminVpsSmokeReportTests`: 24/24.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.199.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-report-route-contract`.

### Changed
- `scripts/validate-admin-vps-smoke-report.ps1` теперь читает `docs/admin-vps-smoke-sections.json` и проверяет, что route каждого раздела smoke report совпадает с sections contract.
- `scripts/validate-admin-vps-smoke-sections-contract.ps1` обновлен под manifest-driven report validator.

### Added
- `scripts/test-admin-vps-smoke-report-validator.ps1` покрывает fail-closed tamper-сценарий `bad-route`.

### Verified
- Admin VPS smoke report validator regression: OK, включая `bad-route`.
- Admin VPS smoke sections contract validator/regression: OK.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: OK.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness/bootstrap/smoke/preflight reports UTF-8 without BOM, bootstrap smoke report valid, paired evidence validator OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 590/590.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.198.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-sections-contract`.

### Added
- Добавлен `docs/admin-vps-smoke-sections.json` как единый manifest обязательных admin sections для VPS smoke evidence.
- Добавлен `scripts/validate-admin-vps-smoke-sections-contract.ps1` для сверки manifest, report template, report validator, VPS Playwright smoke и all-screens smoke.
- Добавлен `scripts/test-admin-vps-smoke-sections-contract.ps1` с fail-closed tamper-сценариями `duplicate-section`, `bad-route`, `template-missing-section`, `browser-spec-no-manifest` и `all-screens-missing-section`.

### Changed
- `frontend/e2e/admin-vps-smoke.spec.ts` берет id/route разделов из `docs/admin-vps-smoke-sections.json`, чтобы browser report не расходился с contract.
- `docs/admin-vps-smoke.md` описывает section contract validator и regression harness.

### Verified
- Admin VPS smoke sections contract validator: OK.
- Admin VPS smoke sections contract regression: OK, `6/6` scenarios.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted docs/admin suite: OK.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness/bootstrap/smoke/preflight reports UTF-8 without BOM, bootstrap smoke report valid, paired evidence validator OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 590/590.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.197.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-evidence`.

### Added
- Добавлен `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` для парной проверки readiness report и итогового bootstrap+smoke report.
- Добавлен `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` для fail-closed regression сценариев `valid`, `mismatched-admin-url`, `readiness-not-ready` и `bad-timing`.

### Changed
- `scripts/admin-vps-bootstrap-smoke.ps1` после успешного smoke теперь валидирует, что readiness и bootstrap reports относятся к одному запуску.

### Verified
- `AdminBootstrapCliScriptTests`: 9/9.
- Admin VPS bootstrap smoke evidence validator regression: OK.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness report valid, bootstrap smoke report valid, paired evidence validator OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 589/589.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.196.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-readiness`.

### Added
- Добавлен `scripts/admin-vps-bootstrap-smoke-readiness.ps1` для fail-closed проверки параметров live bootstrap+smoke до reset-а.
- Добавлен `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` для проверки sanitized readiness report.
- Добавлен `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` с regression-сценариями `local-ready`, `missing-password`, `missing-confirm-bootstrap-reset` и `missing-connection-string`.

### Changed
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь запускает readiness gate до `admin-bootstrap.ps1` и пишет `admin-vps-bootstrap-smoke-readiness-report.json` без пароля и connection string.
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь доказывает readiness/bootstrap/smoke цепочку на временной SQLite-БД.

### Verified
- `AdminBootstrapCliScriptTests`: 8/8.
- Admin VPS bootstrap smoke readiness regression: OK.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness report valid, bootstrap smoke report valid, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 588/588.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.195.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-report`.

### Added
- Добавлен `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` для проверки sanitized bootstrap+smoke report.
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь после успешного smoke пишет `admin-vps-bootstrap-smoke-report.json` без пароля, cookie и auth headers.

### Changed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь проверяет сам bootstrap+smoke wrapper и его sanitized report на временной SQLite-БД.

### Verified
- `AdminBootstrapCliScriptTests`: 7/7.
- Local CLI bootstrap admin smoke на SQLite: OK, bootstrap smoke report valid, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 587/587.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.194.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-wrapper-regression`.

### Added
- Добавлен `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` для fail-closed regression проверки bootstrap+smoke wrapper.
- Regression покрывает `missing-password`, `missing-confirm-bootstrap-reset`, `missing-connection-string` и `dry-run-no-smoke`.

### Changed
- `docs/admin-bootstrap.md` и `docs/admin-vps-smoke.md` теперь содержат команду regression-проверки bootstrap+smoke wrapper.

### Verified
- `AdminBootstrapCliScriptTests`: 6/6.
- Admin VPS bootstrap smoke wrapper regression: OK, tested scenarios `4/4`.
- Local CLI bootstrap admin smoke на SQLite: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 586/586.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.193.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-wrapper`.

### Added
- Добавлен `scripts/admin-vps-bootstrap-smoke.ps1`, который запускает admin bootstrap/reset и затем admin VPS smoke под тем же аккаунтом.
- Добавлен `scripts/local-admin-vps-bootstrap-smoke.ps1`, который доказывает flow на временной SQLite-БД: CLI bootstrap создает admin, API стартует с `AdminBootstrap__Enabled=false`, затем проходит admin smoke.

### Changed
- `docs/admin-bootstrap.md` и `docs/admin-vps-smoke.md` описывают единый bootstrap+smoke проход без вывода пароля.

### Verified
- `AdminBootstrapCliScriptTests`: 5/5.
- Local CLI bootstrap admin smoke на SQLite: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 585/585.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.192.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-evidence-validator`.

### Added
- Добавлен `scripts/validate-admin-vps-smoke-evidence.ps1`, который валидирует preflight report и smoke report вместе и сверяет их связь.
- Добавлен `scripts/test-admin-vps-smoke-evidence-validator.ps1` с fail-closed сценариями для mismatched URL/path/release/timing и failed smoke report.

### Changed
- `scripts/admin-vps-smoke.ps1` теперь после browser smoke запускает парный evidence validator.

### Verified
- `AdminVpsSmokeReportTests`: 14/14.
- Admin VPS smoke evidence validator regression: OK, tested failures `5/5`.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 583/583.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.191.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-flow-wrapper-regression`.

### Added
- Добавлен `scripts/test-admin-vps-smoke-flow-wrapper.ps1`, который проверяет fail-closed поведение `scripts/admin-vps-smoke.ps1` до запуска browser smoke.
- Regression harness покрывает `missing-password`, `bad-api-url` и `missing-frontend`, проверяет отсутствие smoke report после failed preflight и отсутствие пароля в stdout/stderr.

### Verified
- `AdminVpsSmokeReportTests`: 13/13.
- Admin VPS smoke flow wrapper regression: OK, tested failures `3/3`.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 582/582.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.190.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-flow-wrapper`.

### Added
- Добавлен `scripts/admin-vps-smoke.ps1`, единый fail-closed wrapper для admin VPS smoke: сначала preflight с `-RequirePassword`, затем browser smoke с `-RequireAllPassed`.
- `scripts/local-admin-vps-browser-smoke.ps1` теперь проверяет тот же preflight+browser flow на временной SQLite-БД.

### Verified
- `AdminVpsSmokeReportTests`: 12/12.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 581/581.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.189.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-report-validator-regression`.

### Added
- Добавлен `scripts/test-admin-vps-smoke-report-validator.ps1`, который проверяет happy path основного admin VPS smoke report validator и fail-closed tamper-сценарии.
- Regression harness проверяет `bad-http-status`, `placeholder-evidence`, `failed-status`, `missing-section`, `false-gate` и `secret-marker`.

### Verified
- `AdminVpsSmokeReportTests`: 11/11.
- Admin VPS smoke report validator regression: OK.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 580/580.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.188.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-preflight-validator-regression`.

### Added
- Добавлен `scripts/test-admin-vps-smoke-preflight-validator.ps1`, который проверяет happy path preflight validator и fail-closed tamper-сценарии.
- Regression harness проверяет `bad-ready-flag`, `failed-check`, `missing-check`, `duplicate-check` и `secret-marker`, а также контролирует, что тестовый пароль не попал в JSON.

### Verified
- `AdminVpsSmokeReportTests`: 10/10.
- Admin VPS smoke preflight validator regression: OK.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 579/579.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.187.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-preflight-validator`.

### Added
- Добавлен `scripts/validate-admin-vps-smoke-preflight-report.ps1` для fail-closed проверки sanitized preflight evidence перед реальным admin VPS smoke.
- `scripts/admin-vps-smoke-preflight.ps1` теперь запускает validator preflight-отчета с `-RequireReady` перед разрешением live smoke.

### Verified
- `AdminVpsSmokeReportTests`: 9/9.
- Admin VPS smoke preflight validator: OK на тестовых URL и process env password.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 578/578.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.186.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-preflight`.

### Added
- Добавлен `scripts/admin-vps-smoke-preflight.ps1` для проверки live URL, admin email, password env, frontend runner, npm command и validator перед реальным admin VPS smoke.
- Preflight пишет sanitized JSON `admin-vps-smoke-preflight-report.json` с `readyForLiveSmoke` и `passwordEnvPresent`, но не принимает пароль параметром и не выводит секрет.

### Verified
- `AdminVpsSmokeReportTests`: 8/8.
- Admin VPS smoke preflight: OK на тестовых URL и process env password.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 577/577.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.185.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-acceptance-evidence`.

### Changed
- `scripts/validate-admin-vps-smoke-report.ps1 -RequireAllPassed` теперь требует успешный `httpStatus` по каждой секции админки.
- Acceptance mode отклоняет placeholder evidence вроде `TODO`, `Not checked yet`, `safe screenshot name` и шаблонных browser smoke notes.

### Verified
- `AdminVpsSmokeReportTests`: 7/7.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 576/576.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.184.0 - 2026-06-19

Release entry: `2026-06-19-local-admin-vps-browser-smoke`.

### Added
- Добавлен `scripts/local-admin-vps-browser-smoke.ps1` для полной локальной проверки admin browser smoke на временной SQLite-БД.
- Локальный harness поднимает API с `AdminBootstrap__Enabled=true`, admin-panel через Vite с `VITE_API_BASE_URL` на временный API, запускает `scripts/admin-vps-browser-smoke.ps1 -RequireAllPassed` и удаляет временные файлы по умолчанию.

### Fixed
- Cleanup локального smoke останавливает дерево процессов, чтобы дочерний Vite `node.exe` не оставался слушать порт после проверки.

### Verified
- `AdminVpsSmokeReportTests`: 6/6.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 575/575.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.183.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-browser-smoke`.

### Added
- Добавлен `frontend/e2e/admin-vps-smoke.spec.ts` для явного live-smoke входа в админку и обхода всех обязательных разделов VPS admin UI.
- Добавлен `frontend/playwright.vps-smoke.config.ts` без локального webServer, trace, video и screenshots, чтобы live-прогон не сохранял пароль, cookie или токены в artifacts.
- Добавлен `scripts/admin-vps-browser-smoke.ps1`, который принимает URL/email, берет пароль только из `ADMIN_VPS_SMOKE_ADMIN_PASSWORD`, печатает `Password: [hidden]` и валидирует JSON через `validate-admin-vps-smoke-report.ps1`.

### Verified
- `AdminVpsSmokeReportTests`: 5/5.
- Playwright admin VPS smoke test discovery: OK.
- Backend full suite: 574/574.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.182.0 - 2026-06-19

Release entry: `2026-06-19-admin-bootstrap-wrapper`.

### Added
- Добавлен `scripts/admin-bootstrap.ps1` для one-shot создания или сброса администратора без запуска HTTP-сервера.
- Добавлен `docs/admin-bootstrap.md` с локальным SQLite и production/Postgres сценариями запуска.
- Добавлены `AdminBootstrapCliScriptTests`, которые закрепляют dry-run, скрытие пароля и связь roadmap/test results/"Что нового".

### Verified
- `AdminBootstrapCliScriptTests`: 3/3.
- Admin bootstrap dry-run: OK, password hidden.
- Local SQLite admin bootstrap/reset: OK.
- Targeted release/docs suite: 23/23.
- Backend full suite: 573/573.

## 0.181.0 - 2026-06-19

Release entry: `2026-06-19-staging-smoke-report-evidence-placeholders`.

### Changed
- `scripts/validate-staging-smoke-report.ps1` в режиме `-RequireAllPassed` теперь отклоняет checks с `TODO` в evidence.
- `docs/staging-smoke-checklist.md` уточняет, что `status = passed` с placeholder evidence не является приемочным staging smoke.
- Roadmap и release docs синхронизированы с backend suite `570/570` и latest release `0.181.0`.

### Verified
- `StagingSmokeChecklistTests`: 8/8.
- Staging smoke report generator/validator smoke: OK.
- Expected fail-closed `-RequireAllPassed` для passed checks с TODO evidence: OK.
- Targeted release/docs suite: 94/94.
- Backend full suite: 570/570.

## 0.180.0 - 2026-06-19

Release entry: `2026-06-19-payment-provider-smoke-report-acceptance-gates`.

### Changed
- `scripts/validate-payment-provider-smoke-report.ps1` при `-RequireAllPassed` теперь требует `true` для всех provider gates: account, checkout, provider confirmation, webhook, subscription и refund.
- `docs/payment-provider-smoke.md` уточняет, что `status = passed` без закрытых boolean gates не является приемочным evidence.
- Roadmap и release docs синхронизированы с backend suite `569/569` и latest release `0.180.0`.

### Verified
- `PaymentProviderSmokeReportTests`: 6/6.
- Payment provider smoke report generator/validator smoke: OK.
- Expected fail-closed `-RequireAllPassed`: OK.
- Targeted release/docs suite: 86/86.
- Backend full suite: 569/569.

## 0.179.0 - 2026-06-19

Release entry: `2026-06-19-vps-production-smoke-report-contract`.

### Added
- Добавлен `docs/vps-production-smoke-report.template.json` для безопасной фиксации live/staging VPS production smoke.
- Добавлены `scripts/new-vps-production-smoke-report.ps1` и `scripts/validate-vps-production-smoke-report.ps1`.

### Changed
- VPS production smoke теперь имеет fail-closed report contract: `-RequireAllPassed` требует полный deploy -> health -> admin login -> order -> payment -> subscription -> VPN access flow.
- Roadmap и release docs синхронизированы с backend suite `568/568` и latest release `0.179.0`.

### Verified
- `VpsProductionSmokeTests`: 7/7.
- VPS production smoke report generator/validator smoke: OK.
- Targeted release/docs suite: 80/80.
- Backend full suite: 568/568.

## 0.178.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards-regression`.

### Added
- Aggregate fail-closed validator теперь проверяет tamper-сценарии `missing-aggregate-ci-step-guard-command` и `missing-aggregate-ci-step-validator`.

### Changed
- `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1` покрывает CI-step guard command/regression step вместе с readiness/evidence artifact contracts.
- Roadmap и release docs синхронизированы с backend suite `564/564` и latest release `0.178.0`.

### Verified
- Production CI workflow artifacts aggregate guard validator: OK, включая CI-step tamper cases.
- `ProductionReadinessGateTests`: 57/57.
- Targeted release/docs suite: 73/73.
- Backend full suite: 564/564.

## 0.177.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards`.

### Added
- Aggregate production workflow artifacts guard теперь запускает `test-production-ci-workflow-artifacts-guards-ci-step.ps1` и `test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1`.

### Changed
- `scripts/test-production-ci-workflow-artifacts-guards.ps1` покрывает 6 guards вместо 4, включая CI wiring guard и его fail-closed regression.
- Roadmap и release docs синхронизированы с backend suite `563/563` и latest release `0.177.0`.

### Verified
- Production CI workflow artifacts guards aggregate: OK, `guardsCount = 6`.
- `ProductionReadinessGateTests`: 56/56.
- Targeted release/docs suite: 72/72.
- Backend full suite: 563/563.

## 0.176.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-ci-step-regression`.

### Added
- Добавлен `scripts/test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1` для fail-closed regression проверки aggregate CI step guard.

### Changed
- Backend CI запускает `Guard production CI workflow artifacts guard steps regression` после CI-step guard и до aggregate guard.
- Roadmap и release docs синхронизированы с backend suite `562/562` и latest release `0.176.0`.

### Verified
- Production CI workflow artifacts aggregate CI step guard validator: OK.
- `ProductionReadinessGateTests`: 55/55.
- Targeted release/docs suite: 71/71.
- Backend full suite: 562/562.

## 0.175.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-ci-step-guard`.

### Added
- Добавлен `scripts/test-production-ci-workflow-artifacts-guards-ci-step.ps1` для проверки GitHub Actions wiring aggregate production workflow artifacts steps.

### Changed
- Backend CI запускает `Guard production CI workflow artifacts guard steps` после checkout, до aggregate guard, aggregate validator и .NET setup.
- Roadmap и release docs синхронизированы с backend suite `561/561` и latest release `0.175.0`.

### Verified
- Production CI workflow artifacts aggregate CI step guard: OK.
- `ProductionReadinessGateTests`: 54/54.
- Targeted release/docs suite: 70/70.
- Backend full suite: 561/561.

## 0.174.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression-ci-step`.

### Added

- GitHub Actions backend job запускает `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson` отдельным step до backend setup/build/test.

### Changed

- Production readiness gate docs описывают порядок aggregate guard -> aggregate validator -> backend setup.
- Roadmap и release docs синхронизированы с backend suite `560/560` и latest release `0.174.0`.

### Verified

- `ProductionReadinessGateTests`: 53/53.
- Production CI workflow artifacts aggregate validator CI step guard: OK.
- Targeted release/docs suite: 69/69.
- Backend full suite: 560/560.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI теперь запускает fail-closed aggregate validator сразу после aggregate guard, поэтому broken workflow artifact contract должен падать до тяжелых backend-команд.

## 0.173.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression`.

### Added

- `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1` проверяет fail-closed поведение aggregate production CI workflow artifacts guard.

### Changed

- Production readiness gate docs описывают aggregate guard regression для tampered `.github/workflows/ci.yml`.
- Roadmap и release docs синхронизированы с backend suite `559/559` и latest release `0.173.0`.

### Verified

- `ProductionReadinessGateTests`: 52/52.
- Production CI workflow artifacts aggregate guard validator smoke: OK.
- Targeted release/docs suite: 68/68.
- Backend full suite: 559/559.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет `missing-readiness-guard-step`, `missing-readiness-assertion-log-artifact`, `missing-production-evidence-result-artifact` и `missing-if-no-files-found-error`.

## 0.172.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate`.

### Added

- `scripts/test-production-ci-workflow-artifacts-guards.ps1` запускает оба production workflow artifacts guards и оба fail-closed validators одной командой.

### Changed

- GitHub Actions backend job запускает aggregate step `Guard production CI workflow artifacts contracts` сразу после checkout.
- Roadmap и release docs синхронизированы с backend suite `558/558` и latest release `0.172.0`.

### Verified

- `ProductionReadinessGateTests`: 51/51.
- Production CI workflow artifacts guards aggregate smoke: OK.
- Targeted release/docs suite: 67/67.
- Backend full suite: 558/558.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Aggregate guard проверяет readiness assertion и production evidence published artifacts contracts вместе с tamper regression harnesses.

## 0.171.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-workflow-artifacts-guard-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1` проверяет fail-closed поведение readiness assertion workflow artifacts guard.

### Changed

- Production readiness gate docs описывают readiness workflow guard regression для tampered `.github/workflows/ci.yml`.
- Roadmap и release docs синхронизированы с backend suite `557/557` и latest release `0.171.0`.

### Verified

- `ProductionReadinessGateTests`: 50/50.
- Production readiness assertion CI workflow artifacts guard validator smoke: OK.
- Targeted release/docs suite: 66/66.
- Backend full suite: 557/557.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет `missing-guard-step`, `missing-assertion-log-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.

## 0.170.0 - 2026-06-19

Release entry: `2026-06-19-production-evidence-ci-workflow-artifacts-guard-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1` проверяет fail-closed поведение production evidence workflow artifacts guard.

### Changed

- Production readiness gate docs описывают workflow guard regression для tampered `.github/workflows/ci.yml`.
- Roadmap и release docs синхронизированы с backend suite `556/556` и latest release `0.170.0`.

### Verified

- `ProductionReadinessGateTests`: 49/49.
- Production evidence CI workflow artifacts guard validator smoke: OK.
- Targeted release/docs suite: 65/65.
- Backend full suite: 556/556.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет `missing-guard-step`, `missing-result-json-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.

## 0.169.0 - 2026-06-19

Release entry: `2026-06-19-production-evidence-ci-workflow-artifacts-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1` проверяет published artifacts contract для GitHub Actions job `production-evidence`.

### Changed

- GitHub Actions job `production-evidence` запускает workflow artifacts guard до archive regression wrapper.
- Roadmap и release docs синхронизированы с backend suite `555/555` и latest release `0.169.0`.

### Verified

- `ProductionReadinessGateTests`: 48/48.
- Production evidence CI workflow artifacts guard smoke: OK.
- Targeted release/docs suite: 64/64.
- Backend full suite: 555/555.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI теперь fail-closed проверяет, что `production-evidence` публикует JSON/Markdown result artifacts handoff archive regression.

## 0.168.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-workflow-guard-step`.

### Added

- GitHub Actions job `production-readiness-assertion` запускает `scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson` до readiness assertion wrapper.

### Changed

- `ProductionReadinessGateTests` закрепляет порядок CI шагов: workflow artifacts guard, readiness assertion wrapper, artifacts upload.
- Roadmap и release docs синхронизированы с backend suite `554/554` и latest release `0.168.0`.

### Verified

- `ProductionReadinessGateTests`: 47/47.
- Production readiness assertion CI workflow artifacts guard smoke: OK.
- Targeted release/docs suite: 63/63.
- Backend full suite: 554/554.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI теперь fail-closed проверяет contract published artifacts до запуска wrapper и upload step.

## 0.167.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-workflow-artifacts`.

### Added

- `scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1` проверяет, что CI workflow публикует полный artifact-директорий readiness assertion.

### Changed

- Production readiness gate docs описывают workflow guard для `.github/workflows/ci.yml`.
- Roadmap и release docs синхронизированы с backend suite `553/553` и latest release `0.167.0`.

### Verified

- `ProductionReadinessGateTests`: 46/46.
- Production readiness assertion CI workflow artifacts guard smoke: OK.
- Targeted release/docs suite: 62/62.
- Backend full suite: 553/553.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Guard проверяет job `production-readiness-assertion`, `needs: backend`, wrapper command, artifact name, `if-no-files-found: error` и пять обязательных published files.

## 0.166.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-summary-artifacts-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-summary-validator.ps1` теперь проверяет tamper-сценарий `bad-ci-artifacts-validator-regression`.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает artifacts validator regression до summary validator regression, чтобы summary harness проверял строку `CI artifacts validator regression`.
- `scripts/validate-production-readiness-assertion-ci-regression-result.ps1` требует failure-case `bad-ci-artifacts-validator-regression` внутри `ciSummaryValidatorRegression`.
- Roadmap и release docs синхронизированы с backend suite `552/552` и latest release `0.166.0`.

### Verified

- `ProductionReadinessGateTests`: 45/45.
- Production readiness assertion CI summary artifacts regression smoke: OK.
- Targeted release/docs suite: 61/61.
- Backend full suite: 552/552.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Новый regression-case защищает GitHub Step Summary от потери строки `CI artifacts validator regression`.

## 0.165.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-artifacts-validator-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-artifacts-validator.ps1` проверяет fail-closed поведение validator всего readiness assertion CI artifact-директория.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает artifacts validator regression автоматически и записывает `ciArtifactsValidatorRegression` в итоговый JSON/Markdown.
- CI result и summary validators теперь проверяют `ciArtifactsValidatorRegression`, если этот блок присутствует в result artifact.
- Roadmap и release docs синхронизированы с backend suite `551/551` и latest release `0.165.0`.

### Verified

- `ProductionReadinessGateTests`: 44/44.
- Production readiness assertion CI artifacts validator regression smoke: OK.
- Targeted release/docs suite: 60/60.
- Backend full suite: 551/551.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет `missing-required-artifact`, `bad-output-directory`, `bad-assertion-log-path`, `bad-result-markdown` и `bad-step-summary`.

## 0.164.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-artifacts-validator`.

### Added

- `scripts/validate-production-readiness-assertion-ci-artifacts.ps1` проверяет весь artifact-директорий readiness assertion CI одной командой.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает artifact-directory validator перед выводом результата.
- Roadmap и release docs синхронизированы с backend suite `550/550` и latest release `0.164.0`.

### Verified

- `ProductionReadinessGateTests`: 43/43.
- Production readiness assertion CI artifacts validator smoke: OK.
- Targeted release/docs suite: 59/59.
- Backend full suite: 550/550.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет пять обязательных файлов, согласованность путей в result JSON, result validator, summary validator и optional `StepSummaryPath`.

## 0.163.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-step-summary-smoke`.

### Added

- `scripts/test-production-readiness-assertion-ci-step-summary.ps1` проверяет реальный файл `GITHUB_STEP_SUMMARY` для readiness assertion CI wrapper.

### Changed

- Roadmap и release docs синхронизированы с backend suite `549/549` и latest release `0.163.0`.

### Verified

- `ProductionReadinessGateTests`: 42/42.
- Production readiness assertion CI step summary smoke: OK.
- Targeted release/docs suite: 58/58.
- Backend full suite: 549/549.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Smoke выставляет `GITHUB_STEP_SUMMARY`, запускает CI wrapper, валидирует summary, сверяет его с result Markdown и проверяет строки summary/result validator regression.

## 0.162.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-summary-validator`.

### Added

- `scripts/validate-production-readiness-assertion-ci-summary.ps1` проверяет GitHub Step Summary readiness assertion CI wrapper.
- `scripts/test-production-readiness-assertion-ci-summary-validator.ps1` проверяет fail-closed поведение summary validator.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` валидирует result Markdown как summary, запускает summary validator regression, записывает `ciSummaryValidatorRegression` и проверяет реальный `GITHUB_STEP_SUMMARY`, если он доступен.
- Roadmap и release docs синхронизированы с backend suite `548/548` и latest release `0.162.0`.

### Verified

- `ProductionReadinessGateTests`: 41/41.
- Production readiness assertion CI summary validator regression smoke: OK.
- Targeted release/docs suite: 57/57.
- Backend full suite: 548/548.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Summary validator сверяет status, assertion status, result validator, regression statuses и artifact paths, чтобы GitHub Actions summary не расходился с опубликованным CI artifact.

## 0.161.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-result-validator-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-regression-result-validator.ps1` проверяет fail-closed поведение validator итогового production readiness assertion CI result.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает новый harness автоматически, сохраняет `ciResultValidatorRegression` в JSON/Markdown result и повторно валидирует итоговый artifact.
- `scripts/validate-production-readiness-assertion-ci-regression-result.ps1` проверяет `ciResultValidatorRegression`, если секция уже присутствует в result.
- Roadmap и release docs синхронизированы с backend suite `547/547` и latest release `0.161.0`.

### Verified

- `ProductionReadinessGateTests`: 40/40.
- Production readiness assertion CI result validator regression smoke: OK.
- Targeted release/docs suite: 56/56.
- Backend full suite: 547/547.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Harness покрывает bad status, неверный assertion exit code, пропавший regression failure, сломанный Markdown и неверный `evidenceReportsCount`.

## 0.160.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-result-validator`.

### Added

- `scripts/validate-production-readiness-assertion-ci-regression-result.ps1` проверяет итоговый JSON/Markdown artifact production readiness assertion CI regression.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает validator после записи result JSON/Markdown.
- Production readiness gate документация описывает отдельную проверку скачанного CI regression result artifact.
- Roadmap и release docs синхронизированы с backend suite `546/546` и latest release `0.160.0`.

### Verified

- `ProductionReadinessGateTests`: 39/39.
- Production readiness assertion CI result validator smoke: OK.
- Targeted release/docs suite: 55/55.
- Backend full suite: 546/546.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator сверяет status, assertion exit code, linked assertion JSON/Markdown/log, result validator, validator regression failure-сценарии и Markdown-пару.

## 0.159.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-ci-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает production readiness assertion, result validator и validator regression в одном CI-friendly flow.
- `.github/workflows/ci.yml` получил job `production-readiness-assertion` после backend job.

### Changed

- GitHub Actions validation публикует artifact `production-readiness-assertion-ci-regression` с assertion JSON/Markdown/log и итоговым CI regression result.
- Production readiness gate документация описывает локальный запуск wrapper и artifact в CI.
- Roadmap и release docs синхронизированы с backend suite `545/545` и latest release `0.159.0`.

### Verified

- `ProductionReadinessGateTests`: 38/38.
- Production readiness assertion CI regression smoke: OK.
- Targeted release/docs suite: 54/54.
- Backend full suite: 545/545.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Wrapper добавляет Markdown-итог в `GITHUB_STEP_SUMMARY`, если переменная доступна, и не закрывает live-блокеры без реального VPS/payment/3x-ui evidence.

## 0.158.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-result-validator-regression`.

### Added

- `scripts/test-production-readiness-assertion-result-validator.ps1` проверяет fail-closed поведение standalone validator production readiness assertion result artifacts.

### Changed

- Production readiness gate документация получила команду regression-проверки assertion result validator.
- Roadmap и release docs синхронизированы с backend suite `544/544` и latest release `0.158.0`.

### Verified

- `ProductionReadinessGateTests`: 37/37.
- Production readiness assertion result validator regression smoke: OK.
- Targeted release/docs suite: 53/53.
- Backend full suite: 544/544.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Harness портит только временные копии JSON/Markdown artifacts и ожидает ошибки для bad status, неверного `failedEvidenceReportsCount`, missing `vpn-live`, сломанного Markdown и `-RequireProductionReady` на blocked result.

## 0.157.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-result-validator`.

### Added

- `scripts/validate-production-readiness-assertion-result.ps1` проверяет JSON/Markdown result artifacts production readiness assertion без повторного запуска gate.

### Changed

- `scripts/assert-production-readiness.ps1` теперь запускает validator сразу после записи result artifacts и до fail-closed ошибки.
- Production readiness gate документация описывает отдельную проверку скачанного assertion result artifact.
- Roadmap и release docs синхронизированы с backend suite `543/543` и latest release `0.157.0`.

### Verified

- `ProductionReadinessGateTests`: 36/36.
- Blocked production readiness assertion artifact smoke with standalone validator: OK.
- Targeted release/docs suite: 52/52.
- Backend full suite: 543/543.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет статус, четыре evidence report entries, счетчики, пути reports, roadmap/release decision и Markdown-пару.

## 0.156.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-result-artifacts`.

### Added

- `scripts/assert-production-readiness.ps1` получил `-OutputPath`, `-JsonOutputPath` и `-Force` для сохранения JSON/Markdown result artifacts.

### Changed

- Production readiness gate теперь пишет result artifacts даже при ожидаемом `blocked`, а затем продолжает fail-closed падать.
- Result JSON/Markdown фиксирует `failedEvidenceReportsCount`, `blockersCount`, пути всех evidence reports, `evidenceReports`, `blockers`, `resultJsonPath` и `resultMarkdownPath`.
- Roadmap и release docs синхронизированы с backend suite `542/542` и latest release `0.156.0`.

### Verified

- `ProductionReadinessGateTests`: 35/35.
- Blocked production readiness assertion artifact smoke: OK.
- Targeted release/docs suite: 51/51.
- Backend full suite: 542/542.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Artifacts не делают проект production-ready: live/VPS/payment/3x-ui blockers остаются открытыми до реальных passed evidence reports.

## 0.155.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1` проверяет fail-closed поведение standalone CI result validator на испорченных JSON/Markdown artifacts.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает CI result validator regression, сохраняет `ciResultValidatorRegression` в итоговом JSON/Markdown и повторно валидирует финальный result artifact.
- Production readiness gate документация описывает отдельный regression harness для CI result validator.
- Roadmap и release docs синхронизированы с backend suite `541/541` и latest release `0.155.0`.

### Verified

- `ProductionReadinessGateTests`: 34/34.
- Production evidence handoff package archive CI wrapper smoke with result validator regression: OK.
- Standalone CI result validator regression: OK.
- Backend full suite: 541/541.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет ошибки для неверного общего статуса, пустого `releaseId`, отсутствующего failure-сценария summary validator и сломанного Markdown.

## 0.154.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1` проверяет итоговый CI regression JSON/Markdown artifact.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает result validator после финальной записи artifacts.
- Production readiness gate документация описывает отдельную проверку скачанного CI result artifact.
- Roadmap и release docs синхронизированы с backend suite `540/540` и latest release `0.154.0`.

### Verified

- `ProductionReadinessGateTests`: 33/33.
- Production evidence handoff package archive CI wrapper smoke with result validator: OK.
- Backend full suite: 540/540.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет все вложенные statuses, `ciSummaryValidatorRegression`, Markdown-пару и обязательные artifact paths.

## 0.153.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-summary-validator.ps1` проверяет fail-closed поведение CI summary validator на испорченных JSON/Markdown artifacts.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает summary validator regression и сохраняет `ciSummaryValidatorRegression` в result artifacts.
- Production readiness gate документация описывает regression harness для CI summary validator.
- Roadmap и release docs синхронизированы с backend suite `539/539` и latest release `0.153.0`.

### Verified

- `ProductionReadinessGateTests`: 32/32.
- Production evidence handoff package archive CI wrapper smoke with summary validator regression: OK.
- Backend full suite: 539/539.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет неверный main flow status, чужой release id в summary, отсутствующий artifact path и неверный long-path status.

## 0.152.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1` проверяет CI summary Markdown против JSON result artifact.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает validator для result Markdown и для `GITHUB_STEP_SUMMARY`, если summary-файл доступен.
- Production readiness gate документация описывает fail-closed проверку summary.
- Roadmap и release docs синхронизированы с backend suite `538/538` и latest release `0.152.0`.

### Verified

- `ProductionReadinessGateTests`: 31/31.
- Production evidence handoff package archive CI wrapper smoke with summary validator: OK.
- Backend full suite: 538/538.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Summary validator проверяет статусы, release id и пути artifacts; live/VPS/payment blockers остаются внешними smoke-задачами.

## 0.151.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary`.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь дописывает Markdown-результат в `GITHUB_STEP_SUMMARY`, если wrapper запускается в GitHub Actions.
- Production readiness gate документация описывает GitHub Actions job summary и локальную проверку через временный summary-файл.
- Roadmap и release docs синхронизированы с backend suite `537/537` и latest release `0.151.0`.

### Verified

- `ProductionReadinessGateTests`: 30/30.
- Production evidence handoff package archive CI regression wrapper smoke with `GITHUB_STEP_SUMMARY`: OK.
- Backend full suite: 537/537.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Job summary ускоряет диагностику CI evidence gate, но финальные JSON/Markdown artifacts остаются основным handoff-доказательством.

## 0.150.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-workflow`.

### Added

- В `.github/workflows/ci.yml` добавлен job `production-evidence`, который запускает production evidence handoff package archive CI regression после backend-проверок.
- GitHub Actions публикует artifact `production-evidence-handoff-package-archive-ci-regression` с JSON/Markdown результатами wrapper.

### Changed

- Production readiness gate документация теперь описывает, где в GitHub Actions брать CI evidence artifacts.
- Roadmap и release docs синхронизированы с backend suite `536/536` и latest release `0.150.0`.

### Verified

- `ProductionReadinessGateTests`: 29/29.
- Production evidence handoff package archive CI regression wrapper smoke: OK.
- Backend full suite: 536/536.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI job проверяет локальные evidence regressions в validation pipeline, но live/VPS/payment blockers все еще закрываются только реальными smoke reports.

## 0.149.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` объединяет локальные archive flow regression harnesses в один CI-friendly запуск.
- Wrapper сохраняет `production-evidence-handoff-package-archive-ci-regression-result.json` и `.md`.

### Changed

- Production readiness gate документация теперь описывает единый CI wrapper для основного flow, result validator regression и long-path regression.
- Roadmap и release docs синхронизированы с backend suite `535/535` и latest release `0.149.0`.

### Verified

- `ProductionReadinessGateTests`: 28/28.
- Production evidence handoff package archive CI regression wrapper smoke: OK.
- Backend full suite: 535/535.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI wrapper остается локальным evidence regression gate; live/VPS/payment blockers закрываются только реальными smoke reports.

## 0.148.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-long-path-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-long-path.ps1` запускает полный handoff flow в длинной production-evidence директории.
- Long-path harness проверяет hash-based имя handoff package ZIP и сохранение полного `releaseId` в result JSON.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку Windows path-limit regression.
- Roadmap и release docs синхронизированы с backend suite `534/534` и latest release `0.148.0`.

### Verified

- `ProductionReadinessGateTests`: 27/27.
- Production evidence handoff package archive long path regression smoke: OK.
- Backend full suite: 534/534.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Проверка не закрывает live/VPS/payment blockers; она защищает локальный и CI evidence flow от Windows path-limit на длинных release id.

## 0.147.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow-result-validator.ps1` проверяет fail-closed поведение validator результата полного handoff flow.
- Regression harness ожидает ошибки для испорченного `status`, неверного SHA256 handoff archive, отсутствующего tamper-сценария и Markdown без блока `Tested failures`.

### Changed

- Production readiness gate документация теперь описывает отдельный regression harness для result validator.
- Default-имя handoff package ZIP теперь использует короткий hash release id, чтобы длинные release id не ломали сборку на Windows path-limit.
- Roadmap и release docs синхронизированы с backend suite `533/533` и latest release `0.147.0`.

### Verified

- `ProductionReadinessGateTests`: 26/26.
- Production evidence handoff package archive flow result validator regression smoke: OK.
- Backend full suite: 533/533.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет только целостность локального evidence result validator; live/VPS/payment blockers остаются открытыми до реальных smoke reports.

## 0.146.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1` проверяет JSON/Markdown итог полного production evidence handoff flow.
- Flow автоматически запускает result validator после записи `production-evidence-handoff-package-archive-flow-result.json` и `.md`.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку result artifacts полного flow.
- Roadmap и release docs синхронизированы с backend suite `532/532` и latest release `0.146.0`.

### Verified

- `ProductionReadinessGateTests`: 25/25.
- Production evidence handoff package archive flow result validator smoke: OK.
- Backend full suite: 532/532.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Result validator не закрывает внешние live-блокеры; он проверяет локальный evidence flow outcome и целостность ссылок на handoff artifacts.

## 0.145.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` сохраняет `production-evidence-handoff-package-archive-flow-result.json` и `.md`.
- Result artifacts фиксируют release id, package status, SHA256 production evidence archive, SHA256 handoff package archive, пути artifacts и tamper-сценарии regression harness.

### Changed

- Production readiness gate документация теперь описывает result artifacts полного flow.
- Roadmap и release docs синхронизированы с backend suite `531/531` и latest release `0.145.0`.

### Verified

- `ProductionReadinessGateTests`: 24/24.
- Production evidence handoff package archive flow result artifacts smoke: OK.
- Backend full suite: 531/531.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Result artifacts не заменяют live/VPS/payment evidence reports; они фиксируют локальный flow outcome и SHA256 handoff artifacts.

## 0.144.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-safety`.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` теперь проверяет output directory перед рекурсивной очисткой.
- Flow запрещает корень файловой системы, корень репозитория и папку без явного `production-evidence` в имени.

### Added

- Production readiness gate документация описывает безопасный шаблон output directory для локальных и CI evidence-проверок.
- Roadmap получил пункт `P11-ACC-029` для защиты `-Force` в end-to-end flow.

### Verified

- `ProductionReadinessGateTests`: 23/23.
- Guarded production evidence handoff package archive flow smoke: OK.
- Backend full suite: 530/530.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Safety guard не меняет формат evidence artifacts; он защищает только выбор директории для перезаписи при `-Force`.

## 0.143.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` собирает весь локальный production evidence handoff flow одной командой.
- Flow создает evidence bundle, summary, manifest, production evidence ZIP, handoff receipt, checklist, package, финальный ZIP и запускает archive validator regression.

### Changed

- Production readiness gate документация теперь предлагает одну команду для полной локальной evidence handoff проверки.
- Roadmap и release docs синхронизированы с backend suite `529/529` и latest release `0.143.0`.

### Verified

- `ProductionReadinessGateTests`: 22/22.
- Production evidence handoff package archive end-to-end flow smoke: OK.
- Backend full suite: 529/529.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Flow harness проверяет локально собранный blocked/staging evidence; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.142.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-validator.ps1` запускает happy path и tamper-сценарии для финального ZIP-архива handoff package.
- Regression harness создает временные испорченные копии архива и проверяет ошибки для неверного SHA256, лишнего entry и отсутствующего `SHA256SUMS.txt`.

### Changed

- Production readiness gate документация теперь описывает regression-проверку archive validator.
- Roadmap и release docs синхронизированы с backend suite `528/528` и latest release `0.142.0`.

### Verified

- `ProductionReadinessGateTests`: 21/21.
- Production evidence handoff package archive validator regression smoke: OK.
- Backend full suite: 528/528.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет локально собранный ZIP; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.141.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive.ps1` проверяет финальный ZIP-архив handoff package.
- Валидатор сверяет SHA256 внешнего ZIP, запрещает неожиданные и вложенные entries, временно извлекает package и повторно запускает package validator.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку финального ZIP-архива handoff package.
- Roadmap и release docs синхронизированы с backend suite `527/527` и latest release `0.141.0`.

### Verified

- `ProductionReadinessGateTests`: 20/20.
- Production evidence handoff package archive validator smoke: OK.
- Backend full suite: 527/527.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Archive validator проверяет локально собранный ZIP; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.140.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive`.

### Added

- `scripts/new-production-evidence-handoff-package-archive.ps1` упаковывает проверенный handoff package в единый ZIP.
- Package archive generator повторно запускает package validator, добавляет только разрешенные package files и возвращает SHA256/размер архива.

### Changed

- Production readiness gate документация теперь описывает финальную упаковку handoff package в ZIP.
- Roadmap и release docs синхронизированы с backend suite `526/526` и latest release `0.140.0`.

### Verified

- `ProductionReadinessGateTests`: 19/19.
- Production evidence handoff package archive smoke: OK.
- Backend full suite: 526/526.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Archive generator упаковывает локальный проверенный package; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.139.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package.ps1` проверяет готовый каталог handoff package после сборки.
- Валидатор проверяет whitelist файлов, `production-evidence-handoff-package-index.json`, `SHA256SUMS.txt`, SHA256 каждого artifact и повторно запускает checklist validator.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку handoff package.
- Roadmap и release docs синхронизированы с backend suite `525/525` и latest release `0.139.0`.

### Verified

- `ProductionReadinessGateTests`: 18/18.
- Production evidence handoff package validator smoke: OK.
- Backend full suite: 525/525.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Package validator доказывает целостность локального handoff package; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.138.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package`.

### Added

- `scripts/new-production-evidence-handoff-package.ps1` собирает минимальный handoff package после проверки checklist.
- Package содержит production evidence ZIP, JSON/Markdown receipt, JSON/Markdown checklist, `production-evidence-handoff-package-index.json`, `.md` и `SHA256SUMS.txt`.

### Changed

- Production readiness gate документация теперь описывает финальный package step после checklist validator.
- Roadmap и release docs синхронизированы с backend suite `524/524` и latest release `0.138.0`.

### Verified

- `ProductionReadinessGateTests`: 17/17.
- Production evidence handoff package smoke: OK.
- Backend full suite: 524/524.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Package generator копирует только проверенные artifacts и не должен использоваться как замена реальному live/VPS/payment evidence.

## 0.137.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-checklist-validator`.

### Added

- `scripts/validate-production-evidence-handoff-checklist.ps1` проверяет JSON/Markdown checklist после генерации handoff artifact.
- Валидатор повторно запускает receipt validator и сверяет release id, SHA256 архива, SHA256 manifest, gates, operator actions и Markdown-пару checklist.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку checklist и строгий режим `-RequireProductionReady`.
- Roadmap и release docs синхронизированы с backend suite `523/523` и latest release `0.137.0`.

### Verified

- `ProductionReadinessGateTests`: 16/16.
- Production evidence handoff checklist validator smoke: OK.
- Backend full suite: 523/523.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет локальный handoff artifact. Production-ready по-прежнему требует реальные live payment/VPS/3x-ui evidence reports.

## 0.136.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-checklist`.

### Added

- `scripts/new-production-evidence-handoff-checklist.ps1` формирует JSON/Markdown checklist для передачи production evidence после проверки receipt.
- Checklist запускает `validate-production-evidence-handoff-receipt.ps1`, читает `production-readiness-summary.json` и фиксирует gates, статус handoff, release id, SHA256 архива и SHA256 manifest.

### Changed

- Production readiness gate документация теперь описывает финальный operator handoff step после ZIP, receipt и receipt validation.
- Roadmap и release docs синхронизированы с backend suite `522/522` и latest release `0.136.0`.

### Verified

- `ProductionReadinessGateTests`: 15/15.
- Production evidence handoff checklist smoke: OK.
- Backend full suite: 522/522.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Checklist не закрывает внешние production-блокеры сам по себе: для production-ready по-прежнему нужны live payment/VPS/3x-ui evidence reports.

## 0.135.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-receipt-validator`.

### Added

- `scripts/validate-production-evidence-handoff-receipt.ps1` проверяет JSON/Markdown receipt против ZIP-архива production evidence.
- Валидатор повторно запускает archive validator и сверяет release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files.
- Markdown-пара receipt проверяется на ключевые hash-данные, чтобы handoff artifact был проверяем без ручного сравнения.

### Verified

- `ProductionReadinessGateTests`: 14/14.
- Production evidence handoff receipt validator smoke: OK.
- Backend full suite: 521/521.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Receipt validation доказывает целостность локального handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.134.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-receipt`.

### Added

- `scripts/new-production-evidence-handoff-receipt.ps1` создает JSON/Markdown receipt для проверенного ZIP-архива production evidence.
- Receipt запускает archive validator, затем фиксирует release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files.
- Receipt не копирует содержимое evidence reports и подходит для передачи вместе с ZIP в CI или операторский handoff.

### Verified

- `ProductionReadinessGateTests`: 13/13.
- Production evidence handoff receipt smoke: OK, JSON/Markdown receipt создан после archive validation.
- Backend full suite: 520/520.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Receipt фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.133.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-archive-validator`.

### Added

- `scripts/validate-production-evidence-archive.ps1` проверяет ZIP-архив production evidence bundle перед публикацией или передачей оператору.
- Валидатор читает `production-evidence-manifest.json` из архива, запрещает лишние entries и сверяет обязательные файлы.
- Для каждого entry проверяются размер, `totalBytes`, SHA256 и безопасный relative path; для CI добавлен `-ExpectedArchiveSha256`.

### Verified

- `ProductionReadinessGateTests`: 12/12.
- Production evidence archive validator smoke: OK, ZIP проверен по manifest и expected archive SHA256.
- Backend full suite: 519/519.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Archive validation доказывает целостность локального ZIP artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.132.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-archive`.

### Added

- `scripts/new-production-evidence-archive.ps1` собирает ZIP-архив production evidence bundle после успешной проверки manifest.
- Архиватор добавляет в ZIP сам `production-evidence-manifest.json` и только файлы, перечисленные в manifest.
- Результат содержит SHA256 архива, SHA256 manifest, release id, размер архива и список entries.

### Verified

- `ProductionReadinessGateTests`: 11/11.
- Production evidence archive smoke: OK, ZIP создан после manifest validation.
- Backend full suite: 518/518.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Архив фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.131.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-manifest-validator`.

### Added

- `scripts/validate-production-evidence-manifest.ps1` проверяет `production-evidence-manifest.json` перед handoff или CI-публикацией.
- Валидатор перечитывает manifest, проверяет schema, release id, обязательные файлы, relative paths, размеры, timestamps, total files и total bytes.
- Для каждого файла bundle пересчитывается SHA256, чтобы поймать изменение evidence artifact после генерации manifest.

### Verified

- `ProductionReadinessGateTests`: 10/10.
- Production evidence manifest validator smoke: OK, manifest проверен с `-RequireAllFiles`.
- Backend full suite: 517/517.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Manifest validation доказывает целостность локального handoff bundle; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.130.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-manifest`.

### Added

- `scripts/new-production-evidence-manifest.ps1` создает безопасный manifest для handoff production evidence bundle.
- Manifest валидирует bundle перед созданием, затем записывает `production-evidence-manifest.json` с release id, relative paths, SHA256, размерами файлов и UTC timestamps.
- Manifest фиксирует состав evidence bundle без копирования содержимого отчетов и без сохранения секретов.

### Verified

- `ProductionReadinessGateTests`: 9/9.
- Production evidence manifest smoke: OK, 6 файлов с SHA256.
- Backend full suite: 516/516.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Manifest фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытые live/VPS/payment blockers.

## 0.129.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-validator`.

### Added

- `scripts/validate-production-evidence-bundle.ps1` проверяет весь каталог production evidence bundle одной командой.
- Bundle validator запускает validators для staging/VPS, payment providers, admin VPS, VPN live и опционального production readiness summary.
- Добавлены режимы `-RequireSummary`, `-RequireReportFiles` через summary validator и `-RequireProductionReady` для строгого production handoff.

### Verified

- `ProductionReadinessGateTests`: 8/8.
- Production evidence bundle validator smoke: OK.
- Backend full suite: 515/515.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- `-RequireProductionReady` ожидаемо падает до реальных passed evidence reports и закрытия live/VPS/payment blockers.

## 0.128.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-summary-validator`.

### Added

- `scripts/validate-production-readiness-summary.ps1` проверяет Markdown/JSON summary перед handoff оператору или CI.
- Валидатор требует четыре reports (`staging-vps`, `payment-providers`, `admin-vps`, `vpn-live`), корректные статусы, счетчики checks, required flags, report paths и roadmap blockers.
- Добавлены режимы `-RequireReportFiles` и `-RequireProductionReady` для строгой проверки артефактов и финального production handoff.

### Verified

- `ProductionReadinessGateTests`: 7/7.
- Production readiness summary validator smoke: OK, status `blocked` для generated drafts.
- Backend full suite: 514/514.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- `-RequireProductionReady` пройдет только после реальных passed evidence reports и закрытия live/VPS/payment roadmap blockers.

## 0.127.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-summary`.

### Added

- `scripts/new-production-readiness-summary.ps1` создает Markdown и JSON summary по полному production evidence bundle.
- Summary показывает статус staging/VPS, payment providers, admin VPS и VPN live reports, количество passed/blocked/failed checks и required flags.
- Summary отдельно выводит все платежные провайдеры и открытые roadmap blockers, чтобы оператор видел, почему production-ready еще заблокирован.

### Verified

- `ProductionReadinessGateTests`: 6/6.
- Production readiness summary smoke: OK, status `blocked` для generated drafts.
- Backend full suite: 513/513.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Summary является локальным operator artifact. Production-ready по-прежнему требует реальные sanitized evidence после live/staging прогонов VPS, 3x-ui и платежных провайдеров.

## 0.126.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-generator`.

### Added

- `scripts/new-production-evidence-bundle.ps1` создает весь production evidence bundle одной командой: staging/VPS, payment provider, admin VPS и VPN live reports.
- Генератор вызывает существующие безопасные генераторы отчетов, прогоняет их validators и при `-RunProductionGate` возвращает статус агрегированного production gate.
- Документация `docs/production-readiness-gate.md` получила команду создания полного bundle без ручного копирования JSON.

### Verified

- `ProductionReadinessGateTests`: 5/5.
- Bundle generator smoke: OK, созданы 4 JSON-отчета.
- Expected aggregate gate status for generated drafts: `blocked`.
- Backend full suite: 512/512.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Generated reports остаются черновиками `blocked`, пока оператор не заменит TODO на реальные sanitized evidence после live/staging прогонов.

## 0.125.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-aggregate-gate`.

### Changed

- `scripts/assert-production-readiness.ps1` теперь запускает все четыре evidence validators независимо и не останавливается на первой ошибке.
- Fail-closed payload `Production readiness blocked` содержит массив `evidenceReports` с `name`, `status`, `reportPath`, `validatorPath` и `message` по staging/VPS, payment providers, admin VPS и VPN live reports.
- Roadmap/release blockers продолжают попадать в тот же payload, поэтому оператор видит одновременно недостающие отчеты и незакрытые production blockers.
- Current status обновлен до backend `511/511`, latest release `2026-06-18-production-evidence-aggregate-gate`.

### Verified

- `ProductionReadinessGateTests`: 4/4.
- `assert-production-readiness.ps1` на blocked templates возвращает агрегированный fail-closed payload с `evidenceReports`.
- Backend full suite: 511/511.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Production-ready все еще требует реальных passed reports по VPS/staging, платежам, админке VPS и live VPN/3x-ui.

## 0.124.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-gate`.

### Changed

- `scripts/assert-production-readiness.ps1` теперь требует полный production evidence bundle: staging/VPS smoke report, payment provider smoke report, admin VPS smoke report и VPN live smoke report.
- Gate принимает `PaymentProviderReportPath`, `AdminVpsReportPath` и `VpnLiveReportPath`; если пути не переданы, используются стандартные шаблоны из `docs/`.
- Blocking/summary payload теперь показывает пути всех evidence reports, чтобы было понятно, какой отчет не готов.
- `frontend/package-lock.json` обновлен через `npm audit fix`, текущий frontend audit возвращает `0 vulnerabilities`.
- `frontend/scripts/playwright-webservers.mjs` больше не зависит от локального `apps/*/node_modules/vite` и корректно запускает E2E при hoisted workspace-зависимостях.
- Current status обновлен до backend `510/510`, latest release `2026-06-18-production-evidence-bundle-gate`.

### Verified

- `ProductionReadinessGateTests`: 3/3.
- `assert-production-readiness.ps1` остается fail-closed на текущих blocked templates.
- Backend full suite: 510/510.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Production-ready все еще требует реальных passed reports по VPS/staging, платежам, админке VPS и live VPN/3x-ui.

## 0.123.0 - 2026-06-14

Release entry: `2026-06-14-vpn-live-smoke-report`.

### Added

- `docs/vpn-live-smoke-report.template.json` фиксирует обязательный smoke-отчет для production-like VPN выдачи через реальную 3x-ui/x-ui панель.
- `scripts/new-vpn-live-smoke-report.ps1` создает безопасный blocked-черновик отчета с latest release, API URL, admin URL, 3x-ui URL и оператором.
- `scripts/validate-vpn-live-smoke-report.ps1` проверяет URL, даты, top-level VPN gates, обязательные checks и forbidden secret markers, включая полные VPN URI.
- `docs/vpn-live-smoke.md` описывает, как пройти 3x-ui/inbound/node/order/webhook/subscription/client/fail-closed smoke без сохранения секретов.

### Changed

- Current status обновлен до backend `509/509`, latest release `2026-06-14-vpn-live-smoke-report`.
- `P0-VPN-001` ... `P0-VPN-005` остаются открытыми до реальной 3x-ui проверки, но теперь у них есть обязательный формат safe evidence.

### Verified

- Generated VPN live smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `VpnLiveSmokeReportTests`: 4/4.
- Backend full suite: 509/509.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Нужно пройти реальную 3x-ui/x-ui панель, inbound, production VPN node и production-like order smoke, затем заполнить отчет safe evidence.

## 0.122.0 - 2026-06-14

Release entry: `2026-06-14-admin-vps-smoke-report`.

### Added

- `docs/admin-vps-smoke-report.template.json` фиксирует обязательный smoke-отчет для проверки всех разделов админки на VPS.
- `scripts/new-admin-vps-smoke-report.ps1` создает безопасный blocked-черновик отчета с latest release, API URL, admin URL и оператором.
- `scripts/validate-admin-vps-smoke-report.ps1` проверяет URL, даты, общие login/console/API gates, все admin sections и forbidden secret markers.
- `docs/admin-vps-smoke.md` описывает, как пройти VPS admin smoke без сохранения секретов.

### Changed

- Current status обновлен до backend `505/505`, latest release `2026-06-14-admin-vps-smoke-report`.
- `P0-ADMIN-002` остается открытым до реального VPS admin smoke, но теперь у него есть обязательный формат безопасного evidence.

### Verified

- Generated admin VPS smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `AdminVpsSmokeReportTests`: 4/4.
- Backend full suite: 505/505.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Нужно пройти `/admin/` на реальном VPS под production admin-аккаунтом и заполнить отчет safe evidence по всем разделам.

## 0.121.0 - 2026-06-14

Release entry: `2026-06-14-payment-provider-smoke-generator`.

### Added

- `scripts/new-payment-provider-smoke-report.ps1` создает безопасный черновик payment provider smoke report из `docs/payment-provider-smoke-report.template.json`.
- Генератор принимает `EnvironmentName`, `Operator`, `ReleaseId`, `Mode` (`sandbox` или `live`) и подставляет latest release из seed "Что нового", если `ReleaseId` не задан.
- Все провайдеры создаются со статусом `blocked`, пустыми gate-флагами и TODO evidence, поэтому real provider smoke остается fail-closed до внешней проверки.

### Changed

- `docs/payment-provider-smoke.md` теперь рекомендует начинать отчет через генератор, а не ручное копирование JSON.
- Current status обновлен до backend `501/501`, latest release `2026-06-14-payment-provider-smoke-generator`.

### Verified

- Generated payment provider smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `PaymentProviderSmokeReportTests`: 5/5.
- Backend full suite: 501/501.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальные provider smoke reports для YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe и PayPal еще нужно заполнить после внешних sandbox/live проверок.

## 0.120.0 - 2026-06-14

Release entry: `2026-06-14-payment-provider-smoke-report`.

### Added

- `docs/payment-provider-smoke-report.template.json` фиксирует обязательную smoke-матрицу для YooKassa, RoboKassa, YooMoney, CloudPayments, TBankAcquiring, Prodamus, Stripe и PayPal.
- `scripts/validate-payment-provider-smoke-report.ps1` проверяет структуру отчета, даты, дубли провайдеров, обязательные payment gates, безопасные evidence и forbidden secret markers.
- `docs/payment-provider-smoke.md` описывает, как заполнять provider smoke report и почему Telegram Stars проверяется отдельным Telegram invoice flow.
- `PaymentProviderSmokeReportTests` закрепляет fail-closed шаблон и связь отчета с roadmap.

### Changed

- Current status обновлен до backend `500/500`, latest release `2026-06-14-payment-provider-smoke-report`.
- `STATE-011` и `P0-PAY-002` ... `P0-PAY-009` остаются открытыми до реального sandbox/live отчета по внешним кабинетам.

### Verified

- Payment provider smoke report validator: OK.
- `-RequireAllPassed` для blocked шаблона: expected failure.
- `PaymentProviderSmokeReportTests`: 4/4.
- Backend full suite: 500/500.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальные YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe и PayPal кабинеты еще нужно пройти и приложить safe evidence без секретов.

## 0.119.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-generator`.

### Added

- `scripts/new-staging-smoke-report.ps1` создает безопасный черновик staging/VPS smoke report из `docs/staging-smoke-report.template.json`.
- Генератор принимает `ApiBaseUrl`, web URL-ы, `EnvironmentName`, `Operator`, `ReleaseId` и подставляет latest release из seed "Что нового", если `ReleaseId` не задан.
- Все обязательные checks создаются со статусом `blocked` и TODO evidence, поэтому production readiness gate остается fail-closed до реального прогона.

### Changed

- `docs/staging-smoke-checklist.md` теперь рекомендует начинать заполнение отчета через генератор, а не ручное копирование JSON.
- Current status обновлен до backend `496/496`, latest release `2026-06-14-staging-smoke-report-generator`.

### Verified

- `StagingSmokeChecklistTests`: 7/7.
- Generated staging smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- Backend full suite: 496/496.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальный staging/VPS smoke report пока не заполнен; live-платежи, 3x-ui и production-ready решение остаются внешними блокерами.

## 0.118.0 - 2026-06-14

Release entry: `2026-06-14-telegram-stars-invoice-gate`.

### Changed

- Telegram Stars теперь считается готовым для bot checkout только при явном `ExtraSettingsJson.status = "invoice-flow"`.
- Режим `bot-only` остается безопасным состоянием: Stars скрыт из web checkout и не появляется в Telegram-клавиатуре оплаты как готовый способ.
- Проверка подключения платежного провайдера в админке показывает Stars как `Unhealthy` для `bot-only` и `Healthy` для явного `invoice-flow`.
- Production-настройка Telegram Stars больше не требует web secret key, потому что Stars работает через Telegram invoice update flow.
- Current status обновлен до backend `495/495`, latest release `2026-06-14-telegram-stars-invoice-gate`.

### Verified

- Targeted payment/Telegram suite: 61/61.
- Backend full suite: 495/495.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Live BotFather/Telegram Stars smoke с реальным BotToken и Telegram окружением остается внешним production-блокером вместе с live-платежами и VPS/3x-ui проверками.

## 0.117.0 - 2026-06-14

Release entry: `2026-06-14-telegram-webhook-boundary`.

### Добавлено

- Guard-тесты `TelegramBotProcessBoundaryTests`, которые запрещают возвращать `/telegram/webhook` в standalone bot-процесс и проверяют документацию по основному API webhook.
- Roadmap-пункт `P1-TG-006` для явной границы ответственности между основным API и standalone Telegram bot process.

### Обновлено

- `VpnPlatform.TelegramBot` больше не мапит webhook route и остается для LongPolling, очереди Telegram-уведомлений и health endpoints.
- `docs/phase-3-telegram-foundation.md`, `docs/telegram-bot-setup.md`, README и production example указывают webhook на `/api/channels/telegram/webhook` основного API.
- Current status обновлен до backend `493/493`, latest release `2026-06-14-telegram-webhook-boundary`.

### Проверено

- Targeted Telegram boundary/API suite: 41/41.
- Standalone TelegramBot build: OK, предупреждений 0.
- Backend full suite: 493/493.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Реальный Telegram/BotFather webhook и Telegram Stars live/sandbox smoke остаются внешними production-блокерами `STATE-011`, `P11-ACC-002` и `P9-TST-007`.

## 0.116.0 - 2026-06-14

Release entry: `2026-06-14-api-telegram-webhook`.

### Добавлено

- API endpoint `/api/channels/telegram/webhook` теперь обрабатывает Telegram updates в основном backend вместо `501 NotImplemented`.
- Runtime-настройки Telegram-бота читаются из админки/БД с fallback на `appsettings`, включая protected BotToken и webhook secret.
- Infrastructure получил `TelegramBotHttpClient`, который отправляет Telegram Stars invoice, отвечает на `pre_checkout_query` и отправляет сообщения через общий `ITelegramInvoiceProvider`.
- Guard-тесты `ChannelWebhooksControllerTests` проверяют успешную обработку webhook, duplicate update и выключенный Telegram-бот.

### Обновлено

- Current status обновлен до backend `491/491`, latest release `2026-06-14-api-telegram-webhook`.
- Roadmap получил закрытый пункт `P1-TG-005` для Telegram webhook в основном API.

### Проверено

- Targeted Telegram/API suite: 39/39.
- Backend full suite: 491/491.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Live Telegram webhook с реальным BotFather/Bot API и реальные Telegram Stars платежи остаются частью production/staging smoke: `STATE-011`, `P11-ACC-002` и `P9-TST-007`; live smoke не закрывался.

## 0.115.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-url-validation`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет обязательные absolute http/https URL для `apiBaseUrl`, `publicWebUrl`, `cabinetWebUrl` и `adminWebUrl`.
- Roadmap-подпункт `P9-TST-007C` для локально закрытого URL validation слоя.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь отклоняет пустой или невалидный `apiBaseUrl`, а также непустые web URL без абсолютной `http`/`https` схемы.
- `docs/staging-smoke-checklist.md` описывает URL-правила для staging smoke report.
- Current status обновлен до backend `489/489`, latest release `2026-06-14-staging-smoke-report-url-validation`.

### Проверено

- Backend full suite: 489/489.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: URL validation закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.114.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-consistency`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет запрет на `completedAt` раньше `startedAt` и duplicate check id в staging smoke report.
- Roadmap-подпункт `P9-TST-007B` для локально закрытого consistency guard.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь проверяет хронологию `startedAt`/`completedAt` и не принимает повторяющиеся check id.
- `docs/staging-smoke-checklist.md` описывает эти правила как обязательную часть report validation.
- Current status обновлен до backend `488/488`, latest release `2026-06-14-staging-smoke-report-consistency`.

### Проверено

- Backend full suite: 488/488.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: consistency guard закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.113.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-secret-sanitizer`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет forbidden-маркеры для cookies, `.env`, client secrets, API keys, private headers, Telegram secret header и GitHub/VPS secret names.
- Roadmap-подпункт `P9-TST-007A` для локально закрытой части staging smoke report sanitizer.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь дополнительно блокирует `Cookie:`, `Set-Cookie:`, `.env`, `client_secret`, `api_key`, `private header`, `X-Telegram-Bot-Api-Secret-Token`, `PRODUCTION_ENV_FILE` и `VPS_SSH_KEY`.
- `docs/staging-smoke-checklist.md` и `docs/production-readiness-gate.md` уточняют, какие данные нельзя сохранять в smoke-отчет.
- Current status обновлен до backend `487/487`, latest release `2026-06-14-staging-smoke-secret-sanitizer`.

### Проверено

- Backend full suite: 487/487.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: sanitizer закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.112.0 - 2026-06-14

Release entry: `2026-06-14-production-readiness-gate`.

### Добавлено

- Fail-closed gate `scripts/assert-production-readiness.ps1`, который проверяет staging/VPS smoke report через `validate-staging-smoke-report.ps1 -RequireAllPassed` и дополнительно блокирует production-ready при открытых P0/P11/STATE blockers в roadmap или текущем решении `staging-ready baseline`.
- Документ `docs/production-readiness-gate.md` с инструкцией запуска и объяснением, почему текущий baseline должен падать до реального smoke-отчета.
- Guard-тест `ProductionReadinessGateTests`, который закрепляет наличие скрипта, документации, roadmap-пункта `P11-ACC-008`, release seed и TEST_RESULTS.

### Обновлено

- README, финальный runbook, release decision, docs index и master roadmap синхронизированы с latest release `0.112.0`.
- Current status обновлен до backend `486/486`, latest release `2026-06-14-production-readiness-gate`.

### Проверено

- Backend full suite: 486/486.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо падает fail-closed, потому что smoke checks еще `blocked`.

### Ограничения

- Gate не закрывает live-платежи, реальный 3x-ui и VPS admin/live smoke; он только запрещает пометить проект production-ready без их доказательств.

## 0.111.0 - 2026-06-14

Release entry: `2026-06-14-product-admin-roadmap-sync`.

### Добавлено

- Guard-тест `ProductAdminUiRoadmapSyncTests`, который проверяет актуальность продуктового UI-roadmap и отсутствие старых незакрытых чекбоксов по уже покрытым локальным UX/API/E2E задачам.

### Обновлено

- `docs/product-admin-ui-roadmap.md` переписан как компактный актуальный продуктовый срез: локальный сайт, кабинет, админка, UX, API и smoke-проверки закрыты, а live-платежи, реальный 3x-ui и VPS smoke оставлены открытыми.
- Current status обновлен до backend `484/484`, latest release `0.111.0`.

### Проверено

- Backend full suite: 484/484.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Product/UI roadmap не закрывает production-ready: P0/live-задачи по VPS, платежам и 3x-ui остаются в master roadmap.

## 0.110.0 - 2026-06-14

Release entry: `2026-06-14-provisioning-secret-bug-sync`.

### Добавлено

- Guard-тест `ProvisioningSecretStatusConsistencyTests`, который связывает `BUG-006`, security-документацию, `ProvisioningSecretMaterializer` и открытые live smoke блокеры.

### Исправлено

- `BUG-006` больше не числится открытым из-за secret materialization: protected `ssh_key` временно материализуется только через `ProvisioningSecretMaterializer`, runner получает path, а файл удаляется в `finally`.
- `docs/SECURITY_HARDENING_MVP.md` больше не содержит устаревшую формулировку, что protected SSH credentials невозможно передать в Ansible.

### Проверено

- Backend full suite: 483/483.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Полный live provisioning smoke, VPS production smoke, реальные 3x-ui и платежные кабинеты остаются открытыми P0/P11-блокерами.

## 0.109.0 - 2026-06-14

Release entry: `2026-06-14-roadmap-bug-register-sync`.

### Добавлено

- Guard-тест `BugRegisterConsistencyTests`, который проверяет, что локально закрытые баги в журнале ошибок не остаются в статусе `open`.

### Исправлено

- `BUG-004` в roadmap больше не числится открытым: полный browser E2E public/cabinet/admin уже закрыт через `P9-TST-008`, all-screens и console smoke.
- `BUG-005` в roadmap больше не числится открытым: синхронизация документации и проверка кодировки закрыты через `P10-DOC-005`, `STATE-014` и guard-тесты.

### Проверено

- Backend full suite: 482/482.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `BUG-001`, `BUG-002`, `BUG-003`, `BUG-006` и P0/live-задачи остаются открытыми до реального VPS, платежных кабинетов, 3x-ui и provisioning smoke.

## 0.108.0 - 2026-06-14

Release entry: `2026-06-14-roadmap-current-state-sync`.

### Добавлено

- Guard-тест `RoadmapCurrentStateTests`, который закрепляет актуальный верхний статус roadmap и связь с README, release decision, final runbook, TEST_RESULTS, changelog и seed "Что нового".
- Запись "Что нового" `2026-06-14-roadmap-current-state-sync`.

### Обновлено

- Верхний блок `docs/PRODUCT_COMPLETION_ROADMAP.md` синхронизирован с текущими проверками: backend `480/480`, frontend `65/65`, browser console smoke `9/9`, latest release `0.108.0`.
- README, `docs/final-runbook.md` и `docs/release-decision.md` теперь показывают один и тот же latest release.

### Проверено

- Backend full suite: 480/480.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Production-ready статус все еще заблокирован live-платежами, реальной 3x-ui выдачей, VPS admin/live smoke и заполненным staging/VPS smoke report.

## 0.107.0 - 2026-06-14

Release entry: `2026-06-14-all-screens-browser-smoke`.

### Добавлено

- `frontend/e2e/all-screens.spec.ts` с mock-based browser smoke для всех основных экранов public web, кабинета и админки.
- Playwright project `all-screens`.
- npm-скрипт `e2e:all-screens`.
- Документация `docs/all-screens-browser-smoke.md`.
- Guard-тест `AllScreensBrowserSmokeTests`.

### Проверяется

- public routes `/`, `/tariffs`, `/faq`, `/help`, `/account`;
- cabinet auth screen и авторизованный dashboard;
- admin sections `dashboard`, `users`, `payments`, `tariffs`, `subscriptions`, `vpn`, `nodes`, `panels`, `support`, `audit`, `bot`, `releases`, `faq`, `content`, `scenarios`, `provisioning`;
- отсутствие пустого body;
- отсутствие `console.error` и `pageerror`.

### Проверено

- Backend full suite: 478/478.
- `npm run e2e:all-screens --prefix frontend`: 3/3.
- Browser console smoke: 9/9.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.

### Ограничения

- Smoke использует mock API и не подтверждает live-платежи, live 3x-ui или реальный VPS.

## 0.106.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-checklist`.

### Добавлено

- `docs/staging-smoke-checklist.md` с обязательным staging smoke checklist для покупки, оплаты, подписки, VPN-доступа, админки, поддержки и отсутствия browser console errors.
- `docs/staging-smoke-report.template.json` как безопасный шаблон отчета без секретов.
- `scripts/validate-staging-smoke-report.ps1` для структурной проверки отчета и fail-closed release gate через `-RequireAllPassed`.
- Guard-тест `StagingSmokeChecklistTests`.

### Проверяется

- обязательные check id для deploy, health, public/cabinet/admin web, admin login, tariffs, payment providers, checkout, payment init, provider confirmation, subscription, VPN access, support, console, secret rotation и no secret leak;
- запрет на пароли, bearer-токены, private keys и webhook secrets в отчете;
- связка docs index, changelog, TEST_RESULTS и seed "Что нового".

### Проверено

- Backend full suite: 476/476.
- Staging smoke report validator: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.

### Ограничения

- Реальный staging/VPS smoke report еще должен быть заполнен после deploy и настройки внешних sandbox-интеграций.
- Production-ready статус остается заблокированным до live evidence по платежам, 3x-ui и VPS.

## 0.105.0 - 2026-06-14

### Добавлено

- `scripts/vps-production-smoke.ps1` для полного HTTP-smoke против VPS или staging API.
- Документация `docs/vps-production-smoke.md`.
- Guard-тест `VpsProductionSmokeTests`.

### Проверяется

- health live/ready;
- optional public/cabinet/admin web URLs;
- optional admin login и dashboard;
- public checkout session;
- user registration;
- order claim;
- payment init;
- sandbox webhook только в non-Production;
- active subscription;
- VPN access URI.

### Ограничения

- Live VPS smoke должен запускаться отдельно после deploy и ротации раскрытых секретов.
- `-AllowSandboxWebhook` запрещен, если API сообщает `Production`.

### Проверено

- Backend full suite: 473/473.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.

## 0.104.0 - 2026-06-14

### Добавлено

- Release decision `docs/release-decision.md`.
- Guard-тест `ReleaseDecisionTests`, который закрепляет статус `staging-ready baseline` и блокеры production-ready.
- Release entry `2026-06-14-release-decision` для раздела "Что нового".

### Решение

- Текущий статус: **staging-ready baseline, не production-ready**.
- Причина: `P11-ACC-002 VPS production smoke` остается открытым, а production требует live VPS smoke, ротации раскрытых секретов, реального домена/HTTPS, provider-specific sandbox smoke и реальной 3x-ui проверки.

### Проверено

- Backend full suite: 470/470.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.
- Frontend unit tests: 65/65.
- Frontend typecheck/build: OK.
- High-severity frontend audit: OK; остаются 2 moderate advisory по `react-router`.

## 0.103.0 - 2026-06-14

### Добавлено

- Финальный runbook `docs/final-runbook.md`: локальный запуск без Docker, полный validation gate, browser smoke, security gate, deploy на VPS и post-deploy smoke.
- Guard-тест `FinalDocsChangelogTests`, который связывает README, docs index, roadmap, changelog, `TEST_RESULTS.md` и seed "Что нового".
- Release entry `2026-06-14-final-docs-changelog` для админского раздела "Что нового".

### Проверено

- Backend full suite: 467/467.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.
- Frontend unit tests: 65/65.
- Frontend typecheck/build: OK.
- API Release build: OK.
- High-severity frontend audit: OK; остаются 2 moderate advisory по `react-router`.
- UTF-8/encoding guard: OK.

### Ограничения

- Проект находится на уровне локально подтвержденного staging-ready baseline.
- Production-ready решение требует отдельного live VPS smoke, ротации раскрытых секретов, production домена/HTTPS, реальных sandbox-кабинетов платежных провайдеров и проверки 3x-ui панели.

## 0.102.0 - 2026-06-14

### Добавлено

- Финальный security checklist `docs/security-final-checklist.md`.
- Guard `SecurityFinalChecklistTests` для admin auth policies, headers, rate limits, secrets и webhook/security gates.

### Исправлено

- `scan-secrets.ps1` и `scan-secrets.sh` исключают generated Playwright artifacts, чтобы E2E-прогоны не ломали actual secret scan исчезающими временными файлами.

## 0.101.0 - 2026-06-14

### Добавлено

- Browser console smoke `npm run e2e:console --prefix frontend`.
- Проверка desktop/mobile public, cabinet и admin поверхностей на отсутствие `console.error` и `pageerror`.

## 0.100.0 - 2026-06-14

### Добавлено

- Mobile smoke для public, cabinet и admin.
- Mobile Playwright-проекты и PNG-артефакты для основных экранов.

## 0.99.0 - 2026-06-13

### Добавлено

- Fresh local smoke на чистой SQLite-БД: health, seed, sandbox payment, webhook, subscription и VPN access.

### Исправлено

- SQLite-сортировка `/api/me/orders` по `DateTimeOffset` перенесена после materialize.
