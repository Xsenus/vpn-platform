# Результаты проверок

Дата проверки: 2026-05-25.

## Проверка 2026-06-11: доступность интерфейса

Что проверено:

- Закрыт roadmap-пункт `P3-UX-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- В `@vpn-platform/ui` улучшены доступные состояния общих компонентов: `StatusBadge`, `CopyButton`, `PasswordField`, `SecretField`, `ConfirmButton`.
- Для копирования добавлен скрытый live-region `sr-only`, для паролей и секретов - `aria-describedby`, для статусов - `role="status"`, для подтверждения - `role="dialog"` и `aria-haspopup="dialog"`.
- Усилен видимый focus ring и добавлено правило `prefers-reduced-motion: reduce`.
- Окно "Что нового" в кабинете получает фокус при открытии, закрывается по Escape, возвращает фокус на предыдущий элемент, имеет `aria-describedby` и отмечает выбранный релиз через `aria-current`.
- Обращения в поддержку получили выбранное состояние через `aria-pressed` и понятное `aria-label`.
- Добавлен release entry `2026-06-11-accessibility-polish` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const p='backend/src/VpnPlatform.Api/AppReleases/releases.json'; const data=JSON.parse(fs.readFileSync(p,'utf8')); console.log(data.length, data[data.length-1].releaseId, data[data.length-1].version);"
cd frontend
npm test
npm run typecheck
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- App releases JSON: валиден, последний релиз `2026-06-11-accessibility-polish`, версия `0.61.0`.
- Frontend tests: 60/60 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-11-accessibility-polish`, версия `0.61.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`, публичные провайдеры `8`, публичные тарифы `3`.
- Browser accessibility smoke: public-web, cabinet и admin-panel открыты на 390 px; у интерактивных элементов нет пустых доступных имен, есть skip link/main, правило reduced motion подключено, горизонтального переполнения нет.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах выполнена, совпадений нет.

## Проверка 2026-06-11: адаптивность интерфейса

Что проверено:

- Закрыт roadmap-пункт `P3-UX-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- В `@vpn-platform/ui` добавлены responsive-токены `--page-x`, `--page-y`, `--page-bottom` и явные CSS-переломы для 1280, 1024, 768 и 390 px.
- Админка получила tablet/mobile правила для login-экрана, боковой навигации, вкладок разделов, платежных провайдеров, редактора релизов и пользовательских карточек.
- Публичный сайт получил отдельные правила для hero, тарифов, FAQ, карты покрытия, CTA и футера на desktop/tablet/mobile.
- Личный кабинет получил адаптивные правила для текущего VPN-доступа, поддержки, платежных метаданных и окна "Что нового".
- Добавлен release entry `2026-06-11-responsive-breakpoints` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Frontend static guard проверяет обязательные breakpoint-правила и ключевые responsive CSS-блоки.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const p='backend/src/VpnPlatform.Api/AppReleases/releases.json'; const data=JSON.parse(fs.readFileSync(p,'utf8')); console.log(data.length, data[data.length-1].releaseId, data[data.length-1].version);"
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- App releases JSON: валиден, последний релиз `2026-06-11-responsive-breakpoints`, версия `0.60.0`.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-11-responsive-breakpoints`, версия `0.60.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`, публичные провайдеры `8`, публичные тарифы `3`.
- Browser responsive check: public-web, cabinet и admin-panel открыты через временные Vite-серверы на 1280 и 390 px; `scrollWidth` не превышает `clientWidth`, горизонтального переполнения нет, ключевые заголовки и карточки отрисованы.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах выполнена, совпадений нет.

## Проверка 2026-06-11: проверка форм админки

Что проверено:

- Закрыт roadmap-пункт `P3-UX-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- В `@vpn-platform/ui` добавлен общий `FormValidationSummary`.
- Формы платежных провайдеров, тарифов, VPN-серверов, 3x-ui панелей, inbound-правил и сценариев получили явные валидаторы и видимый summary ошибок.
- Submit-кнопки этих форм блокируются по тем же массивам ошибок, которые показываются пользователю.
- Добавлен release entry `2026-06-11-admin-form-validation` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Static guard проверяет валидаторы, `FormValidationSummary` и disabled-состояния по ошибкам.

Команды и результат:

```powershell
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-admin-form-validation`, версия `0.59.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: навигация админки по разделам

Что проверено:

- Закрыт roadmap-пункт `P3-UX-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Админская навигация переведена на grouped tablist с группами `Операции`, `Продажи`, `VPN`, `Контент`.
- Добавлены мобильный `admin-section-select`, переходы `Предыдущий` / `Следующий`, описания активных разделов и hash-переходы без прыжка страницы.
- Основные разделы админки получили `role="tabpanel"` и связь с tab через `aria-labelledby={adminSectionTabId(...)}`.
- Добавлен release entry `2026-06-11-admin-section-navigation` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Static guard проверяет grouped navigation, tab semantics, mobile select, prev/next и panel-связи.

Команды и результат:

```powershell
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-admin-section-navigation`, версия `0.58.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: современный login админки

Что проверено:

- Закрыт roadmap-пункт `P3-UX-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Экран входа админки получил валидацию `validateAdminLogin`, `role="alert"` для ошибок, remember email без хранения пароля и подсказки по sessionStorage.
- Добавлен release entry `2026-06-11-admin-login-polish` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Static guard проверяет, что пароль не записывается в `sessionStorage`, а email сохраняется отдельно через `ADMIN_EMAIL_STORAGE_KEY`.
- Local SQLite проверяет, что seed релизов загружается в БД, новый релиз становится latest и его можно отметить просмотренным.

Команды и результат:

```powershell
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-admin-login-polish`, версия `0.57.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: единая дизайн-система

Что проверено:

- Закрыт roadmap-пункт `P3-UX-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- В `@vpn-platform/ui` добавлены `designTokens`, `SegmentedTabs`, `StateBlock` и `DataTableLite`.
- Public-web и cabinet используют общий компонент вкладок для входа и регистрации вместо локальных копий обработчика клавиатуры.
- Добавлен release entry `2026-06-11-design-system-foundation` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Local SQLite проверяет, что seed релизов загружается в БД, новый релиз становится latest и его можно отметить просмотренным.

Команды и результат:

```powershell
cd frontend
npm run typecheck
npm test
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Результат:

- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 60/60 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-design-system-foundation`, версия `0.56.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: обязательные записи «Что нового» для этапов

Что проверено:

- Закрытый roadmap-пункт `P2-ADM-REL-002` отмечен в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен release entry `2026-06-11-release-note-guard` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Backend static guard проверяет, что закрытые пункты P2.7 имеют запись в `releases.json` и упоминание releaseId в `TEST_RESULTS.md`.
- Backend static guard проверяет, что seed-файл «Что нового» содержит пользовательские title/summary/items и допустимые типы пунктов.
- Local SQLite проверяет, что seed релизов загружается в БД и latest/history видят новый релиз.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend narrow tests: 14/14 пройдено.
- Backend full suite: 301/301 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`; latest release `2026-06-11-release-note-guard`, версия `0.55.0`, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление разделом «Что нового»

Что проверено:

- Backend `/api/app-version/latest` и `/api/app-version/history` показывают пользователю только активные опубликованные релизы.
- Backend `/api/app-version/mark-seen` фиксирует просмотр опубликованного релиза и отклоняет скрытые или будущие релизы.
- Backend `/api/app-version/admin/releases` принимает фильтры `visibility`, `source`, `search`.
- Backend `/api/app-version/admin/releases/overview` возвращает счетчики опубликованных, запланированных, скрытых релизов, просмотры и последний опубликованный релиз.
- Админка показывает сводку релизов, фильтры истории и статус «Запланировано» для будущих публикаций.
- Кабинет продолжает открывать окно «Что нового», загружать историю и вызывать `mark-seen`.
- Добавлена запись «Что нового» `2026-06-11-app-version-management`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend narrow tests: 7/7 пройдено.
- Backend full suite: 299/299 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `POST /api/app-version/admin/releases`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`, `/api/app-version/admin/releases?visibility=published&source=manual&search=smoke`; опубликованный релиз стал latest, `mark-seen=true`, повторный latest вернул `seenByCurrentUser=true`, фильтр вернул 1 запись, будущий релиз на `mark-seen` вернул HTTP 404.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление FAQ в админке

Что проверено:

- Backend `/api/admin/faq` принимает фильтры `category`, `visibility`, `search` и возвращает отсортированные вопросы.
- Backend `/api/admin/faq/overview` возвращает счетчики активных/скрытых вопросов, публикации на главной и странице FAQ, категории и дубли.
- Backend блокирует одинаковый вопрос в одной категории при создании и редактировании, включая русские категории с разным регистром.
- Админка показывает сводку FAQ, фильтры по категории/видимости/поиску, статусы публикации и предупреждение о дублях.
- Public API `/api/public/content/faq` и `/api/public/content/faq?home=true` получает опубликованные вопросы после админского изменения.
- Добавлена запись «Что нового» `2026-06-11-admin-faq-management`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FaqControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend narrow tests: 7/7 пройдено.
- Backend full suite: 297/297 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `POST /api/admin/faq`, `/api/admin/faq/overview`, `/api/admin/faq?category=Подключение&visibility=home&search=qr`, `/api/public/content/faq`, `/api/public/content/faq?home=true`; создан 1 вопрос, фильтр вернул 1 запись, public/home вернули 1 запись, дубль с другим регистром вернул HTTP 400.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: готовность контента главной в админке

Что проверено:

- Backend `/api/admin/site-content/home-readiness` возвращает готовность обязательных блоков главной: hero, SEO, преимущества, тарифный заголовок, CTA, footer и текст после оплаты.
- Backend `/api/admin/site-content/home-defaults` создает недостающие обязательные блоки и восстанавливает пустые или выключенные значения безопасными дефолтами.
- Backend запрещает дубли ключей контента при создании и редактировании.
- Админка показывает карточку готовности главной, списки отсутствующих/выключенных/пустых/задублированных ключей и кнопку восстановления дефолтов.
- Public API `/api/public/content/home` получает восстановленные опубликованные блоки.
- Добавлена запись «Что нового» `2026-06-11-admin-home-content-readiness`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "SiteContentControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend narrow tests: 5/5 пройдено.
- Backend full suite: 294/294 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/site-content/home-readiness`, `/api/admin/site-content/home-defaults`, `/api/admin/site-content?group=home`, `/api/public/content/home`; до восстановления `isReady=false`, после восстановления создано 18 блоков, `isReady=true`, public API вернул 18 блоков с hero и SEO.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: безопасные возвраты в админке

Что проверено:

- Backend `/api/admin/payments` возвращает refund readiness: `refundSupported`, `canRefund`, `refundableAmount`, `refundBlockers`.
- Backend `POST /api/admin/payments/{id}/refund` выполняет preflight и не вызывает провайдера, если возврат недоступен.
- В админке платежи показывают доступную сумму возврата, причины блокировки, поле суммы и причину возврата.
- Неподдерживаемые провайдеры, неуспешные платежи, полностью возвращенные суммы и неполная настройка аккаунта блокируются до вызова provider API.
- Добавлена запись «Что нового» `2026-06-11-admin-refund-readiness`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminRefundManagementTests|AdminAuthorizationPolicyTests"
cd frontend
npm test -- --test-name-pattern "refund readiness"
```

Результат:

- Backend narrow tests: 25/25 пройдено.
- Backend full suite: 292/292 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 59/59 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/payments`, `/api/admin/refunds`, `/api/admin/payments/{missingId}/refund`; refund несуществующего платежа корректно вернул HTTP 400.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление заказами в админке

Что проверено:

- Backend `/api/admin/orders` принимает фильтры `status/search`, сохраняет SQLite-safe сортировку и возвращает последний платеж заказа.
- Backend `/api/admin/orders/{id}/recheck-payment` проверяет последнюю платежную попытку заказа через общий payment orchestrator.
- Раздел админки «Заказы» получил фильтр статуса, поиск, расширенные карточки, переходы к пользователю/платежу/подписке и кнопку «Проверить оплату».
- Исправлен frontend-контракт `recheckAdminPayment`: теперь он типизирован как `PaymentStatusResult`, а не как платежная попытка.
- Добавлена запись «Что нового» `2026-06-11-admin-order-management`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminOrderManagementTests|AdminAuthorizationPolicyTests"
cd frontend
npm test -- --test-name-pattern "admin order"
```

Результат:

- Backend narrow tests: 25/25 пройдено.
- Backend full suite: 289/289 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend tests: 58/58 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/orders?status=PendingPayment&search=smoke`, `/api/admin/orders/{missingId}/recheck-payment`; recheck несуществующего заказа корректно вернул HTTP 400.
- Кодировка: проверка на символ замены Unicode U+FFFD в ключевых файлах без совпадений.

## Проверка 2026-06-11: управление подписками в админке

Что проверено:

- Backend умеет активировать подписку, снимать блокировку/отмену и включать текущий VPN-доступ через `VpnAccessLifecycleService`.
- Backend умеет синхронизировать текущий VPN-доступ подписки через endpoint `/api/admin/subscriptions/{id}/sync-access`.
- Раздел админки «Подписки» получил действия: активировать, продлить, синхронизировать доступ, заблокировать/разблокировать и отменить.
- Новые действия подписки покрыты authorization policy: синхронизация доступа требует `VpnManage`.
- Добавлена запись «Что нового» `2026-06-11-admin-subscription-management`.

Команды и результат:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
```

Результат:

- Backend tests: 283/283 пройдено.
- Frontend typecheck: пройден.
- Frontend tests: 57/57 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/subscriptions`, `/api/admin/subscriptions/{missingId}/activate`, `/api/admin/subscriptions/{missingId}/sync-access` прошли; новые маршруты корректно вернули 404 для отсутствующей подписки.

## Проверка 2026-06-11: карточка пользователя в админке

Что проверено:

- Backend user overview для админки возвращает безопасный профиль пользователя, Telegram-аккаунты, заказы, платежи, подписки, VPN-доступы и обращения поддержки без `PasswordHash` и приватных metadata.
- Раздел админки «Пользователи» показывает структурированную карточку: профиль, быстрые метрики, причины внимания оператора, подписки, заказы, платежи, VPN-доступы, Telegram и поддержку.
- Локальный запуск API на временной SQLite-БД работает без Docker; DataProtection-ключи направлены в рабочую папку проекта, чтобы не зависеть от прав к Windows-профилю.
- Проверка кодировки: символов `U+FFFD` в README/docs/backend/frontend/.env.example не найдено.

Команды и результат:

```powershell
dotnet build backend\VpnPlatform.sln --configuration Release --no-restore
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
cd frontend
npm run typecheck
npm test
npm run build
git diff --check
rg -n "<символ U+FFFD>" README.md docs backend\src frontend\apps frontend\packages .env.example
```

Результат:

- Backend build: 0 ошибок, 0 предупреждений.
- Backend tests: 280/280 пройдено.
- Frontend typecheck: пройден.
- Frontend tests: 57/57 пройдено.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/admin/users?search=admin&status=Active`, `/api/admin/users/{id}/overview` прошли успешно.
- `git diff --check`: замечаний нет.
- Поиск символа `U+FFFD`: совпадений нет.

## Что исправлено

- Backend переведен на `.NET 9` (`net9.0`), `global.json` переключен на SDK 9, `dotnet-ef` обновлен до 9.0.16.
- EF Core, ASP.NET Core, Microsoft.Extensions и Npgsql EF Core обновлены до последних patch-версий ветки 9.x.
- Исправлены оставшиеся падения backend suite: Telegram payment flow, sandbox E2E, сериализация EF-графов, sync-события 3x-ui и проверка provisioning precheck.
- JSON payload для Telegram-уведомлений теперь сохраняет русский текст читаемо, без `\uXXXX`.
- Отдельный worker-проект удалён: operational hosted workers запускаются внутри `VpnPlatform.Api`.
- Исправлена совместимость lifecycle/outbox workers с SQLite в Local-режиме.
- Локальный no-Docker запуск продолжает работать через SQLite.
- Staging на VPS переведён с временного SQLite на PostgreSQL 16.
- Исправлено падение кабинета `TypeError: e.toLowerCase is not a function`: общий `StatusBadge` теперь безопасно обрабатывает enum/number/null значения.
- Добавлена no-op EF migration `AlignEfModelSnapshotNet9`, которая синхронизирует snapshot модели с EF Core 9 без DDL-изменений.
- Local sandbox режим платежей расширен на `ASPNETCORE_ENVIRONMENT=Local` для YooKassa/YooMoney/RoboKassa/CloudPayments/TBank/Prodamus/Stripe/PayPal.

## Backend

```powershell
dotnet restore backend\VpnPlatform.sln
dotnet build backend\VpnPlatform.sln --no-restore
dotnet test backend\VpnPlatform.sln --no-build --logger "trx;LogFileName=backend-tests-all-payments-local-sandbox.trx" --verbosity quiet
dotnet ef migrations has-pending-model-changes --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --context ApplicationDbContext
dotnet list backend\VpnPlatform.sln package --outdated --highest-patch
dotnet list backend\VpnPlatform.sln package --vulnerable --include-transitive
dotnet ef migrations list --project backend\src\VpnPlatform.Infrastructure\VpnPlatform.Infrastructure.csproj --startup-project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --no-connect
```

Результат:

- Build: 0 ошибок, 0 предупреждений.
- Tests: 180/180 пройдено.
- TRX: `backend/tests/VpnPlatform.UnitTests/TestResults/backend-tests-all-payments-local-sandbox.trx`.
- EF pending model changes: отсутствуют.
- Patch-обновлений внутри текущих major/minor веток нет.
- Уязвимых NuGet-пакетов не найдено.
- EF tooling на `dotnet-ef` 9.0.16 успешно видит миграции проекта.

## Local smoke без Docker

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1 -ApiPort 8088 -PublicPort 5183 -CabinetPort 5184 -AdminPort 5185
```

Проверено:

- `http://127.0.0.1:8088/health/live` -> HTTP 200.
- `http://127.0.0.1:5183` -> HTTP 200.
- `http://127.0.0.1:5184` -> HTTP 200.
- `http://127.0.0.1:5185` -> HTTP 200.
- `scripts/stop-local.ps1` корректно остановил API, npm и дочерние Vite-процессы.

## Frontend

Ранее в рамках локальной стабилизации проверено:

```powershell
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=moderate
```

Результат: typecheck/build/test успешны, frontend tests 27/27, audit без moderate+ уязвимостей.

## VPS staging

Проверено после деплоя на staging VPS:

- PostgreSQL 16 установлен и запущен, database `vpnplatform`, пользователь `vpnplatform`, порт `5432` слушает только `127.0.0.1`.
- API запущен через `vpn-platform-api.service`, `Database__Provider=Postgres`.
- EF migrations применены, demo seed и admin bootstrap выполнены.
- `http://<staging-host>:8080/health/live` -> HTTP 200.
- `http://<staging-host>` -> HTTP 200.
- `http://<staging-host>:5173` -> HTTP 200.
- `http://<staging-host>:5174` -> HTTP 200.
- `http://<staging-host>:5175` -> HTTP 200.
- `http://<staging-host>:8080/api/public/payments/providers` отдаёт 8 sandbox-провайдеров: YooMoney, YooKassa, RoboKassa, CloudPayments, TBankAcquiring, Prodamus, Stripe, PayPal.
- Для всех 8 провайдеров проверен API flow: register -> create order -> payment init.
- Кабинет открыт в браузере, console errors: 0.

## Provisioning runner

```powershell
python -m unittest discover -s infra\ansible\runner\tests -v
```

Результат: 4/4 пройдено.

## Ограничения среды

- Docker Desktop в текущей среде не был запущен, поэтому compose runtime не проверялся.
- `ansible-playbook` не установлен, поэтому Ansible syntax-check не выполнялся.
- Bash-скрипты `.sh` рассчитаны на Linux/WSL; для Windows добавлены PowerShell-скрипты локального запуска.
