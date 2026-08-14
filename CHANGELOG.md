# Changelog

## 0.726.0 - 2026-08-14

Release entry: `2026-08-14-provider-response-redaction-timeout-boundary`.

### Исправлено

- Payment init/status/capture/refund/recheck results очищают structured provider `RawResponse` и `StatusReason` до application result и persistence; исходящий `RawRequest` очищается также при exception/cancellation.
- Telegram Stars invoice response и ambiguous transport exception больше не сохраняют token/secret values в `PaymentAttempt`.
- Provisioning runner после execution timeout ожидает остановку process tree не более 5 секунд вместо дополнительного 10-секундного окна, согласуя runtime с существующим `<10s` контрактом.

### Проверено

- Fail-first provider/Telegram regressions `0/3`; after-fix focused `3/3`, payment/refund/webhook/Telegram regression `173/173`, manual recheck persistence `1/1`; provisioning timeout первоначально воспроизвел full-suite failure `1554/1555`, после исправления стабильно `3/3`.
- Backend Debug/Release `1555/1555`, Release build `0 warnings / 0 errors`, docs/encoding `28/28`, fresh SQLite full flow с latest release, formatter, EF drift и secret scan `707/0` зелёные. Roadmap `747/767` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.725.0 - 2026-08-14

Release entry: `2026-08-14-structured-json-secret-isolation`.

### Исправлено

- `SensitiveDataRedactor` разбирает JSON и рекурсивно удаляет значения password/token/secret/credential полей, сохраняя валидную структуру и безопасные metadata-флаги `Configured`/`Rotated`.
- Универсальный CRUD site content больше не возвращает и не изменяет системную группу и ключи `telegram_bot`; защищенные token values доступны только специализированному Telegram settings boundary.
- Case-insensitive group/key variants также отклоняются, поэтому системные записи нельзя подменить через альтернативный регистр.

### Проверено

- Fail-first structured JSON/content isolation regressions `0/2`; after-fix focused `2/2`, security/content/Telegram regression `26/26`, backend Debug/Release `1553/1553`, Release build `0 warnings / 0 errors`, docs/encoding `28/28`, fresh SQLite full flow latest release, formatter, EF drift и secret scan `707/0` зелёные.
- Roadmap `745/765` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.724.0 - 2026-08-14

Release entry: `2026-08-14-redact-before-truncation`.

### Исправлено

- `SensitiveDataRedactor` теперь удаляет known secrets и PEM private keys из полного diagnostic text до применения `maxLength`, поэтому секрет, пересекающий границу truncation, не раскрывается частично.
- Перекрывающиеся known secrets дедуплицируются и обрабатываются от длинного к короткому, исключая утечку хвоста после замены короткого префикса.
- Truncation suffix входит в итоговый лимит; email password-reset retry не сохраняет начало reset-кода в `NotificationDelivery.ErrorText`.

### Проверено

- Fail-first redaction/email regressions `0/3`; after-fix focused `3/3`, redactor consumer regression `247/247`, backend Debug/Release `1551/1551`, Release build `0 warnings / 0 errors`, docs/encoding `62/62`, fresh SQLite full flow latest release, глобальный formatter, EF drift и secret scan `707/0` зелёные.
- Roadmap `744/764` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.723.0 - 2026-08-14

Release entry: `2026-08-14-payment-error-redaction`.

### Исправлено

- Payment provider exceptions, unknown status reasons, refund/recheck failures и webhook verifier diagnostics редактируются до возврата из application service и до записи в `PaymentAttempt.StatusReason`/`PaymentWebhookEvent.ErrorText`.
- `Authorization: Bearer ...` обрабатывается раньше общего key-value шаблона, поэтому redactor удаляет весь bearer token, а не только слово `Bearer`.
- Диагностические ошибки ограничены 500 символами; retryability, webhook idempotency, refund reservation и reconciliation contract не изменены.

### Проверено

- Fail-first payment regressions `0/6`; after-fix focused `7/7`, payment/security regression `148/148`, backend Debug/Release `1548/1548`, Release build `0 warnings / 0 errors`, docs/encoding `62/62`, fresh SQLite full flow latest release, глобальный formatter, EF drift и secret scan `707/0` зелёные.
- Roadmap `743/763` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.722.0 - 2026-08-14

Release entry: `2026-08-14-automatic-entity-application-clock`.

### Исправлено

- Доменная модель больше не захватывает process clock в audit и operational date defaults; отсутствующие audit timestamps назначаются на persistence-границе через внедренный `IClock`.
- Автоматически создаваемые VPN nodes, checkout sessions, orders, payment attempts, refunds, webhook events и referral rewards получают согласованные `CreatedAt`/`UpdatedAt` из одного operation snapshot.
- Явно заданные исторические audit timestamps сохраняются, а sync/async `SaveChanges` используют одинаковый контракт.

### Проверено

- Fail-first clock regressions `0/7`; after-fix focused `9/9`, domain/seed/payment/provisioning regression `49/49`, backend Debug/Release `1546/1546`, frontend `172/172`, typecheck/build, bundle budget, dependency audit `0 vulnerabilities`, Playwright `270/270`, fresh SQLite full flow, глобальный formatter, EF drift и secret scan `707/0` зелёные.
- Roadmap `742/762` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.721.0 - 2026-08-14

Release entry: `2026-08-14-service-enum-boundaries`.

### Исправлено

- Payment provider account service больше не сохраняет numeric undefined provider и mode: оба значения проверяются до provider gate и SQL.
- Order service отклоняет undefined order type, channel и payment provider до create/select/promo операций, предотвращая некорректные order snapshots.
- Concurrent X3Ui migration test ждёт завершения competing capacity reservation с bounded timeout вместо хрупкого предположения, что SQLite-процесс всегда завершится за 250 мс.

### Проверено

- Fail-first service regressions `0/2` сохранили undefined enum; after-fix focused `2/2`, payment/order/checkout/Telegram regression `146/146`, X3Ui concurrency isolated `1/1` и repeat `10/10`, backend Debug/Release `1543/1543`, fresh SQLite full flow с latest release, глобальный formatter, EF drift и secret scan `706/0` зелёные. Frontend не менялся; актуальные frontend `172/172`, production build/bundle, dependency audit `0 vulnerabilities` и Playwright `270/270` остаются применимыми.
- Roadmap `741/761` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.720.0 - 2026-08-14

Release entry: `2026-08-14-enum-input-boundaries`.

### Исправлено

- Admin notification delivery filter больше не превращает неизвестный status в запрос без фильтра: malformed и numeric undefined значения отклоняются до SQL.
- Order status и Telegram payment provider parsers требуют не только `Enum.TryParse`, но и `Enum.IsDefined`, исключая неопределенные числовые значения.

### Проверено

- Fail-first regressions `0/3`; after-fix focused `4/4`, admin/Telegram regression `68/68`, backend Debug/Release `1541/1541`, fresh SQLite full flow с latest release, глобальный formatter, EF drift и secret scan `706/0` зелёные. Frontend не менялся; актуальные frontend `172/172`, production build/bundle, dependency audit `0 vulnerabilities` и Playwright `270/270` остаются применимыми.
- Roadmap `740/760` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.719.0 - 2026-08-14

Release entry: `2026-08-14-sqlite-repair-application-clock`.

### Исправлено

- SQLite migration preparation и post-migration repair больше не используют process/DB clock для quarantine timestamps: outbox, provisioning, panel sync и Telegram lifecycle получают один обязательный `repairAt` snapshot.
- Hosted startup и CLI admin-bootstrap передают `IClock.UtcNow`; SQL updates параметризованы тем же значением без `CURRENT_TIMESTAMP`, а direct entity repair больше не читает `DateTimeOffset.UtcNow`.

### Проверено

- Fail-first SQLite clock regressions `0/4` получили системное/DB-время; after-fix targeted `4/4`, SQLite repair/startup/admin-bootstrap regression `52/52`, backend Debug/Release `1540/1540`, fresh SQLite full flow с latest release, глобальный formatter, EF drift и secret scan `706/0` зелёные. Актуальные frontend `172/172`, production build/bundle, dependency audit `0 vulnerabilities` и Playwright `270/270` остаются применимыми, frontend/API DTO не менялись.
- Roadmap `739/759` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.718.0 - 2026-08-14

Release entry: `2026-08-14-backend-source-encoding-guard`.

### Исправлено

- Generated EF migrations больше не исключаются из strict UTF-8 проверки: BOM удалён из 15 исторических migration-файлов, а локальный backend C# baseline нормализован с 65 CRLF-файлов до LF согласно корневому `.editorconfig` и `.gitattributes`.
- Encoding suite теперь отдельно запрещает CR во всех backend C# и проверяет migration/snapshot-файлы на UTF-8 without BOM, предотвращая повторное появление скрытого formatter-долга.

### Проверено

- Fail-first encoding regressions `0/2` подтвердили CRLF и BOM; after-fix tracked C# baseline `312/0/0`, targeted `2/2`, глобальный `dotnet format --verify-no-changes`, backend Debug/Release `1540/1540`, fresh SQLite full flow с latest release, EF pending-model-changes и secret scan `706/0` зелёные. Актуальные frontend `172/172`, production build/bundle, dependency audit `0 vulnerabilities` и Playwright `270/270` остаются применимыми, runtime-логика и frontend не менялись.
- Roadmap `738/758` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.717.0 - 2026-08-14

Release entry: `2026-08-14-release-content-contract`.

### Исправлено

- Admin API и startup seed истории релизов используют единый fail-closed контракт: release ID допускает только lowercase kebab-case, обязательные поля и items имеют явные границы, source/item type не получают silent fallback, отрицательный и дублирующий sort order отклоняется до изменения базы.
- Frontend-редактор повторяет серверные ограничения при обычном и программном submit, не удаляет частично пустые items перед валидацией и блокирует добавление сверх 100 элементов.

### Проверено

- Fail-first backend regressions `0/8` воспроизвели default date, null/blank items, неизвестные source/type и malformed ID; отдельные sort-order regressions `0/4` подтвердили silent fallback и неоднозначный порядок, frontend programmatic-submit regression `0/1` дошёл до API. После исправления release controller/seed `47/47`, frontend targeted `1/1`, backend Debug/Release `1539/1539`, frontend `172/172`, typecheck/build, admin bundle `561037` raw/`148981` gzip/max `253610`, fresh SQLite full flow, EF drift, changed-file formatter verify, secret scan `706/0` и dependency audit `0 vulnerabilities` зелёные.
- Полная browser-матрица пройдена раздельно по тем же семи Playwright projects: `270/270`, включая public `27/27`, cabinet `32/32`, admin `69/69`, all-screens `14/14`, mobile-public `27/27`, mobile-cabinet `32/32`, mobile-admin `69/69`. Монолитный wrapper превысил локальный лимит длительности без итогового отчёта и не учитывался в результате.
- Roadmap `737/757` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.716.0 - 2026-08-14

Release entry: `2026-08-14-release-seed-preflight-demo-clock`.

### Исправлено

- Startup release sync валидирует весь seed до чтения tracked releases и отклоняет пустой файл, отсутствующие обязательные поля, duplicate ID, пустые items и недопустимый manual ownership; ошибочный seed больше не удаляет agent history и не оставляет частично изменённый контекст.
- `releasedAt` обязателен и больше не получает системный default; demo payment providers и sandbox VPN infrastructure используют application clock для audit и health timestamps.

### Проверено

- Fail-first release-seed regressions `0/5` воспроизвели удаление существующей истории, системную дату, silent duplicate resolution и создание immutable manual release; demo-seed clock regression `0/1` получил системные timestamps у всех девяти provider accounts. После исправления seed regression `14/14`, documentation/release gate `63/63`, backend Debug/Release `1521/1521`, fresh SQLite full flow, EF drift, formatter verify, secret scan `705/0` и dependency audit `0 vulnerabilities` зелёные; актуальный неизменённый visual/operation gate: frontend `172/172`, typecheck/build, bundle budget и Playwright `268/268` на 25 viewport-конфигурациях.
- Roadmap `736/756` closed, readiness `97.4%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.715.0 - 2026-08-14

Release entry: `2026-08-14-x3ui-safe-client-toggle-runtime-clock`.

### Исправлено

- 3x-ui enable/disable сначала читает исходную конфигурацию remote-клиента и меняет только `enable`, сохраняя UUID, email, flow, IP/traffic limits, expiry и дополнительные provider fields.
- Login, GET и POST к 3x-ui работают fail-closed для malformed payload, `success:false`, пустого обязательного ответа и отсутствующего session cookie; HTTP request clones освобождаются после каждого retry.
- JWT expiry, Stripe webhook timestamp tolerance, 3x-ui session/traffic timestamps и auto-created sandbox node health timestamp используют application clock.

### Проверено

- Fail-first regressions воспроизвели повреждение remote client payload, принятие malformed/unsuccessful 3x-ui HTTP 200, фиктивный session cookie, три системных timestamp, отклонение поддерживаемого root array, строковый false marker и принятие array login (`0/12`); after-fix provider/auth regression `66/66`, `X3UiHttpClientTests` `21/21`, backend Debug/Release `1515/1515`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow и актуальный Playwright `268/268` на 25 viewport-конфигурациях зелёные.
- Roadmap `734/754` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.714.0 - 2026-08-14

Release entry: `2026-08-14-public-catalog-release-seed-clock`.

### Исправлено

- Public tariff catalog использует внедренный `IClock` для `VisibleFrom`/`VisibleTo`, поэтому витрина и checkout оценивают временные границы одинаково.
- Release seed sync назначает согласованные `CreatedAt`/`UpdatedAt` релизам и пунктам из одного application clock boundary, сохраняя исходный `CreatedAt` существующего релиза.
- `AppReleaseSeedServiceTests` удаляют каждый временный seed root после теста; накопленные `4120` каталогов из `%TEMP%` очищены.

### Проверено

- Fail-first regressions `0/2` воспроизвели пустой действующий каталог и системный seed timestamp вместо `2034-04-05T06:07:08Z`; after-fix catalog/seed regression `50/50`, cleanup fixture `6/6`, backend Debug/Release `1501/1501`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow и EF drift зеленые. Актуальный visual/operation gate: Playwright `268/268` на 25 viewport-конфигурациях.
- Roadmap `732/752` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.713.0 - 2026-08-14

Release entry: `2026-08-14-admin-crud-clock-and-capacity-gates`.

### Исправлено

- FAQ, site content, work scenarios и Telegram bot settings используют внедренный `IClock` для create/update/restore, `GeneratedAt`/`CheckedAt` и всех settings/template timestamps.
- X3Ui concurrency regression принимает оба безопасных пути последнего inbound slot: ранний отказ до remote create или позднюю компенсацию лишнего remote-клиента; в обоих случаях локальная capacity и единственный клиент обязательны.

### Проверено

- Fail-first clock regressions `0/4` получили системное время вместо `2033-03-04T05:06:07Z`; after-fix targeted `4/4`, CRUD/Telegram regression `72/72`, capacity concurrency `5/5`, backend Debug/Release `1499/1499`, frontend `172/172`, typecheck/build и bundle budget зеленые. Fresh SQLite full flow, EF drift, secret scan `704/0` и dependency audit `0 vulnerabilities` также пройдены. Актуальный visual/operation gate: Playwright `268/268` на 25 viewport-конфигурациях.
- Roadmap `730/750` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.712.0 - 2026-08-14

Release entry: `2026-08-14-app-release-clock-consistency`.

### Исправлено

- Кабинетная история релизов, отметка «прочитано» и админские фильтры published/upcoming теперь используют внедренный `IClock`, а не системное время.
- Создание и обновление релиза синхронно назначает `CreatedAt`/`UpdatedAt` релизу, его пунктам и отметке просмотра из одного временного boundary.
- Encoding guard больше не сканирует временный `tmp`, поэтому параллельная очистка SQLite smoke не вызывает случайный `DirectoryNotFoundException`.

### Проверено

- Fail-first regressions `0/2` воспроизвели пустую историю опубликованных релизов и системный timestamp вместо `2032-02-03T07:08:09Z`; after-fix app-version regression `18/18`, backend Debug/Release `1495/1495`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, secret scan `704/0` и dependency audit `0 vulnerabilities` зеленые. Актуальный неизмененный visual/operation gate: Playwright `268/268` на 25 viewport-конфигурациях.
- Roadmap `728/748` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.711.0 - 2026-08-14

Release entry: `2026-08-14-cabinet-support-clock-consistency`.

### Исправлено

- Создание обращения поддержки в кабинете, reply и close/reopen больше не обходят внедренный `IClock` через системный `DateTimeOffset.UtcNow`.
- Conversation и создаваемые support messages получают согласованные `CreatedAt`/`UpdatedAt`/`ClosedAt`, поэтому тестовые, скорректированные и production clocks не создают временных скачков в истории обращения.

### Проверено

- Fail-first regression `0/1` сохранил системное время вместо `2032-02-03T07:08:09Z`; after-fix clock regression `1/1`, support/controller regression `15/15`, backend Debug/Release `1493/1493`, frontend `172/172`, typecheck/build и bundle budget зеленые. Актуальный неизмененный visual/operation gate: Playwright `268/268` на 25 viewport-конфигурациях.
- Roadmap `726/746` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.710.0 - 2026-08-13

Release entry: `2026-08-13-sqlite-telegram-dedup-reconciliation`.

### Исправлено

- SQLite migration upgrade больше не оставляет несколько pending/sending Telegram notifications активными с `legacy:*` keys и не допускает повторную доставку того же semantic event после обновления.
- Более ранняя notification по фактическому моменту времени сохраняется как survivor, более поздние pending/sending дубли отменяются.
- Frontend lockfile обновляет транзитивный `nanoid` с `3.3.17` до исправленной `3.3.18` после high advisory `GHSA-2v37-7h3g-55p8`.

### Улучшено

- Оба SQLite startup entry point выполняют idempotent local repair после `MigrateAsync`; historical migrations и PostgreSQL path не изменены.
- Post-migration reconciliation заменяет migration-only `legacy:*` keys на canonical SHA-key для survivor и устойчивые `duplicate:*` markers для дублей.

### Проверено

- Fail-first migration/startup regressions `0/2` подтвердили отсутствие canonical key и post-migration repair; after-fix targeted `17/17`, backend Debug/Release `1492/1492`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, secret scan `704/0` и dependency audit `0 vulnerabilities` зеленые. Актуальный неизмененный visual/operation gate: Playwright `268/268` на 25 viewport-конфигурациях.
- Roadmap `725/745` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.709.0 - 2026-08-13

Release entry: `2026-08-13-sqlite-outbox-provisioning-temporal-preflight`.

### Исправлено

- SQLite upgrade больше не сохраняет более поздний outbox event или provisioning run из-за лексикографической сортировки одинаковых correlation/node записей с разными UTC offsets.
- Local schema repair выбирает oldest queued provisioning run по фактическому моменту времени через `julianday`.

### Улучшено

- Migration preflight канонизирует в UTC только `CreatedAt` конфликтующих outbox/provisioning групп перед immutable historical migrations; обычные строки и PostgreSQL path не изменены.
- Некорректный timestamp в конфликтующей группе останавливает upgrade fail-closed вместо недетерминированной дедупликации.

### Проверено

- Fail-first upgrade regressions `0/2` подтвердили отсутствие preflight; after-fix local repair/upgrade `15/15`, backend Debug/Release `1490/1490`, frontend `172/172`, полный Playwright `268/268`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, secret scan `704/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `723/743` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.708.0 - 2026-08-13

Release entry: `2026-08-13-sqlite-temporal-repair-preflight`.

### Исправлено

- Local SQLite schema repair больше не выбирает старую Telegram-связь, новый concurrent panel sync или старый default payment account из-за лексикографической сортировки offset timestamps.
- SQLite migration startup выполняет chronological preflight до immutable migration chain и не позволяет старым migration SQL переопределить правильного победителя.

### Улучшено

- Telegram links, panel sync runs и payment provider defaults дедуплицируются через `julianday` и стабильный `Id` tie-break; PostgreSQL migrations не изменены.
- Local repair восстанавливает отсутствующий unique default-payment index и остается idempotent на актуальной схеме.

### Проверено

- Fail-first mixed-offset regressions `3/3` воспроизвели неверный выбор; after-fix local repair/payment migration `23/23`, backend Debug/Release `1486/1486`, frontend `172/172`, docs/current-state/encoding `46/46`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, secret scan `704/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `722/742` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui остаются внешней проверкой; статус `staging-ready baseline`, не production-ready.

## 0.707.0 - 2026-08-13

Release entry: `2026-08-13-x3ui-sqlite-diagnostics-ordering`.

### Исправлено

- SQLite admin VPN diagnostics больше не сортирует offset timestamps лексикографически и не показывает старые записи раньше новых.
- Clients, sync runs, sync events и health checks используют корректный chronological order.

### Улучшено

- Все четыре SQLite query используют `julianday`, стабильный `Id` tie-break и прежние DB-side limits; другие providers сохраняют LINQ ordering.

### Проверено

- Fail-first mixed-offset regression вернул более старый sync run первым; after-fix chronological SQL `1/1`, X3Ui/admin VPN regression `100/100`, backend Debug/Release `1486/1486`, frontend `172/172`, docs/current-state/encoding `64/64`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, secret scan `704/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `721/741` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные 3x-ui панели, VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API и SMTP локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.706.0 - 2026-08-13

Release entry: `2026-08-13-provisioning-support-sqlite-latest-boundary`.

### Исправлено

- `MarkSupportNeededAsync` больше не падает на SQLite при поиске последней open/pending support conversation.
- System support conversations без user/Telegram identity переиспользуются по прежней null-семантике и не дублируются.

### Улучшено

- SQLite query использует parameterized identity/subject filters, `julianday(CreatedAt)`, стабильный `Id` tie-break и DB-side `LIMIT 1`; другие providers сохраняют LINQ ordering.

### Проверено

- Fail-first SQLite regression воспроизвел `NotSupportedException`; after-fix targeted `3/3`, provisioning/own-VPS/admin regression `150/150`, backend Debug/Release `1485/1485`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow с latest release, EF drift, docs/encoding `64/64`, secret scan `704/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `720/740` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API, SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.705.0 - 2026-08-13

Release entry: `2026-08-13-telegram-payment-sqlite-temporal-boundaries`.

### Исправлено

- Telegram payload после успешной оплаты больше не падает на SQLite при fallback-поиске последнего VPN-ключа.
- Payment link, списки заказов/подписок/ключей, продление и support conversation больше не используют неподдерживаемый SQLite `DateTimeOffset ORDER BY`.

### Улучшено

- Provider-aware latest-access query и Telegram SQLite branches используют parameterized `julianday`, стабильный `Id` tie-break и DB-side `LIMIT`; PostgreSQL/другие providers сохраняют LINQ ordering.

### Проверено

- Fail-first SQLite regression воспроизвел `NotSupportedException`; after-fix targeted SQLite E2E `3/3`, Telegram/payment/subscription regression `62/62`, backend Debug/Release `1483/1483`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow с latest release, EF drift, docs/encoding `64/64`, secret scan `704/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `719/739` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram Bot API/webhook, SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.704.0 - 2026-08-13

Release entry: `2026-08-13-admin-bootstrap-sqlite-runtime-boundaries`.

### Исправлено

- Admin bootstrap/reset больше не загружает все активные password-reset tokens и refresh-сессии в память и не обновляет их по одной.
- Пустой список provisioning runs и panel health/sync workers больше не падают на SQLite из-за неподдерживаемой сортировки `DateTimeOffset`.

### Улучшено

- Relational providers выполняют два ограниченных set-based UPDATE в общей транзакции с password, reset-state и session-version changes; service сохраняет результат до возврата, а CLI не делает дублирующий SaveChanges.
- SQLite workers выбирают ограниченные очереди через `julianday` и `LIMIT 10/5`; пустой provisioning list не запускает лишний precheck query.

### Проверено

- Fail-first: SELECT двух bootstrap-коллекций по `25` строк и отдельные UPDATE; три SQLite runtime regression `0/3`, browser wrapper дважды обнаружил provisioning 500/console error. After-fix boundary/bootstrap/server/panel `101/101`, local admin wrapper readiness `17`, preflight `10/10`, sections `17/17`, без JS errors/401/403 и API ERR/500; backend Debug/Release `1480/1480`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, docs/encoding `46/46`, secret scan `703/0`, dependency audit `0 vulnerabilities` и rollback fault-injection зелёные. `RoadmapCurrentStateTests` и документационные guards синхронизированы.
- Roadmap `718/738` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram/Bot API/SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.703.0 - 2026-08-13

Release entry: `2026-08-13-admin-user-session-revocation-write-boundary`.

### Исправлено

- Деактивация пользователя в админке больше не загружает все активные refresh-сессии и не обновляет их по одной.

### Улучшено

- Relational providers отзывают сессии одним set-based UPDATE в транзакции с user/audit changes; Patch использует внедрённый `IClock`, сохраняя block/unblock и concurrent rotation semantics.

### Проверено

- Fail-first: SELECT `25` сессий и `25` UPDATE; after-fix admin user boundary/controller `13/13`, admin/auth/security/session `90/90`, backend Debug/Release `1475/1475`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, docs/encoding `49/49`, secret scan `702/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `716/736` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram/Bot API/SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.702.0 - 2026-08-13

Release entry: `2026-08-13-session-revocation-write-boundary`.

### Исправлено

- Logout-all и password reset больше не загружают все активные refresh-сессии в память и не обновляют их по одной.

### Улучшено

- Relational providers отзывают сессии одним set-based UPDATE в общей транзакции с user/reset/audit changes; multi-device login и refresh-family lifecycle сохранены.

### Проверено

- Fail-first: SELECT `25` сессий и `25` отдельных UPDATE; after-fix session/reset/boundary `17/17`, auth/security/admin-session `77/77`, backend Debug/Release `1474/1474`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, docs/encoding `49/49`, secret scan `701/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `715/735` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram/Bot API/SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.701.0 - 2026-08-13

Release entry: `2026-08-13-refresh-family-query-boundary`.

### Исправлено

- Reuse/logout современной refresh family больше не выполняет отдельный SQL-запрос для каждого звена rotation chain.

### Улучшено

- Сессии с `FamilyId` отзывают active family одним query в общей optimistic-concurrency transaction; legacy linked-chain fallback сохранен для старых записей без family ID.

### Проверено

- Fail-first: `8` token-table reads для семьи из шести токенов; after-fix boundary/session `10/10`, auth/security/admin-session `85/85`, backend Debug `1473/1473`, frontend `172/172`, typecheck/build, bundle budget и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `714/734` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram/Bot API/SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.700.0 - 2026-08-13

Release entry: `2026-08-13-own-vps-latest-read-boundary`.

### Исправлено

- Own-VPS provisioning worker больше не загружает все подходящие подписки и обращения поддержки перед выбором последней записи.

### Улучшено

- SQLite использует parameterized latest-record queries с `julianday(CreatedAt)`, `Id` и `LIMIT 1`; остальные providers применяют эквивалентный ordered `FirstOrDefaultAsync`.

### Проверено

- Fail-first SQL assertions `0/2`; after-fix targeted `2/2`, own-VPS/provisioning/sandbox regression `72/72`, backend Debug `1472/1472`, frontend `172/172`, typecheck/build, bundle budget и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `713/733` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram/Bot API/SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.699.0 - 2026-08-13

Release entry: `2026-08-13-admin-ready-state-visual-gate`.

### Исправлено

- Визуальный all-screens аудит больше не принимает admin error boundary или partial-load banner за успешно отрисованный раздел.
- E2E assertions учитывают русскую локализацию бонусных дней и обязательную `revision` миграции VPN-клиента.

### Улучшено

- Exact admin subscription/payment fixtures отделены от сокращенных cabinet DTO; dashboard и все 17 разделов проверяются в ready-state на 25 viewport-конфигурациях.

### Проверено

- Fail-first обнаружил скрытые dashboard/subscriptions и затем `4` устаревших desktop/mobile assertions; after-fix targeted ready-state `1/1`, visual all-screens `14/14`, operational desktop/mobile `4/4`, полный Playwright `268/268` за 15.1 min, frontend `172/172`, backend Debug `1470/1470`, typecheck/build и bundle budget зеленые.
- Roadmap `712/732` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты, live payment, Telegram/Bot API/SMTP, VPS/SSH/Ansible и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.698.0 - 2026-08-13

Release entry: `2026-08-13-delivery-dispatch-query-boundary`.

### Исправлено

- SQLite email, Telegram notification и outbox dispatch больше не загружают и не сканируют pending queues перед due/stale filtering.

### Улучшено

- Три очереди используют parameterized `julianday`, deterministic `CreatedAt/Id` order и `LIMIT <= 100`; provider-neutral LINQ paths и delivery lifecycle сохранены.

### Проверено

- Fail-first SQL assertions `0/3`; after-fix targeted `3/3`, delivery lifecycle `32/32`, backend Debug/Release `1470/1470`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, Release build `0` warnings/errors, secret scan `698/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `711/731` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные Telegram/Bot API/SMTP delivery, provider кабинеты, live payment, VPS/SSH/Ansible и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.697.0 - 2026-08-13

Release entry: `2026-08-13-promo-redemption-query-boundary`.

### Исправлено

- Проверка promo limits больше не загружает все нетерминальные orders и не истекает stale pending orders по одному.

### Улучшено

- SQLite освобождает просроченные promo slots одним parameterized `julianday` UPDATE; PostgreSQL использует `ExecuteUpdateAsync`, global/per-user limits считаются через `COUNT(*)`.

### Проверено

- Fail-first SQL был без promo-scoped temporal UPDATE/COUNT; after-fix promo lifecycle `14/14`, order/checkout/payment regression `52/52`, backend Debug/Release `1470/1470`, frontend `172/172`, typecheck/build, bundle budget, fresh SQLite full flow, EF drift, Release build `0` warnings/errors, secret scan `698/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `710/730` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты, live payment, Telegram/Bot API/SMTP, VPS/SSH/Ansible и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.696.0 - 2026-08-13

Release entry: `2026-08-13-referral-reward-read-boundary`.

### Исправлено

- Referral reward outbox больше не загружает все relationships пользователя и status-active programs перед first/date filtering.

### Улучшено

- SQLite применяет relationship top-1 и program `StartAt/EndAt` через `julianday`; PostgreSQL использует LINQ, все одновременно активные программы сохраняются.

### Проверено

- Fail-first SQL был без top-1/date window; after-fix targeted `1/1`, referral/outbox/auth regression `18/18`, backend `1470/1470`, frontend `172/172`, typecheck/build, fresh SQLite full flow и EF drift зеленые.
- Roadmap `709/729` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты, live payment, Telegram/Bot API/SMTP, VPS/SSH/Ansible и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.695.0 - 2026-08-13

Release entry: `2026-08-13-node-allocation-selection-boundary`.

### Исправлено

- Production и sandbox allocation больше не загружают все node/panel candidates перед protocol filtering и ordering.

### Улучшено

- SQLite выбирает node/panel top-1 параметризованным exact-token/ratio/`julianday` SQL; PostgreSQL использует LINQ top-1, прежние allocation rules сохранены.

### Проверено

- Fail-first SQL был без protocol predicate/limit; after-fix targeted SQLite `3/3`, allocation/capacity/activation regression `55/55`, backend `1470/1470`, frontend `172/172`, typecheck/build, fresh SQLite full flow и EF drift зеленые.
- Roadmap `708/728` closed, readiness `97.3%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/provider кабинеты, live payment, Telegram/Bot API/SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.694.0 - 2026-08-13

Release entry: `2026-08-13-lifecycle-worker-query-boundary`.

### Исправлено

- Пятиминутный lifecycle worker больше не загружает все orders и все active/grace subscriptions перед проверкой сроков.

### Улучшено

- Pending orders истекают atomic conditional update; subscription queues читаются due-only batches по 200 с сохранением retry, lease, gate и provider lifecycle.

### Проверено

- Fail-first SQL assertions `0/2`; after-fix targeted `2/2`, order/promo/subscription/worker regression `34/34`, backend `1468/1468`, frontend `172/172`, typecheck/build, fresh SQLite full flow и EF drift зеленые.
- Roadmap `707/727` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram/Bot API/SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.693.0 - 2026-08-13

Release entry: `2026-08-13-provisioning-precheck-selection-boundary`.

### Исправлено

- Список provisioning runs больше не обрезает precheck history неупорядоченным глобальным `Take(1000)`, который мог скрыть актуальный отчет.

### Улучшено

- SQLite выбирает latest report каждого запуска через partitioned `ROW_NUMBER`/`julianday`, PostgreSQL через `DISTINCT ON`; materialization ограничен одной строкой на run.

### Проверено

- Fail-first не находил DB-side per-run top-1; after-fix targeted `2/2`, server/provisioning regression `136/136`, backend `1468/1468`, frontend `172/172`, typecheck/build, Release build `0` warnings/errors, fresh SQLite full flow, EF drift, strict UTF-8, secret scan `698/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `706/726` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider кабинеты, live payment, Telegram/Bot API/SMTP и production-like 3x-ui локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.692.0 - 2026-08-13

Release entry: `2026-08-13-subscription-migration-selection-boundary`.

### Исправлено

- Auto-target миграция подписки больше не загружает все candidate nodes и не выполняет panel/inbound N+1.

### Улучшено

- Единственный ordered join выбирает node/panel/inbound с `LIMIT 1`, сохраняя capacity, protocol, explicit inbound и прежние ошибки явной цели.

### Проверено

- Fail-first показал отдельные node/panel/inbound queries; after-fix operation boundary `11/11`, backend `1467/1467`, frontend `172/172`, subscription lifecycle desktop/mobile `2/2`, fresh SQLite full flow, EF drift, encoding `18/18`, secret scan `698/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `705/725` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальная VPS/production-like 3x-ui миграция и другие внешние evidence не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.691.0 - 2026-08-13

Release entry: `2026-08-13-admin-dashboard-aggregate-boundary`.

### Исправлено

- Admin dashboard больше не загружает subscription/payment/order/account rows для count-метрик.

### Улучшено

- SQLite считает временные окна через `julianday`/`COUNT(*)`, PostgreSQL использует LINQ aggregates; protected payment account fields не materialize-ятся для readiness.

### Проверено

- Fail-first показал row-select вместо aggregate; after-fix dashboard boundary/RBAC `4/4`, backend `1467/1467`, frontend `172/172`, dashboard desktop/mobile `4/4`, fresh SQLite full flow, EF drift, encoding `18/18`, secret scan `698/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `704/724` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты, live payment, VPS/SSH/Ansible, Telegram/Bot API/SMTP и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.690.0 - 2026-08-13

Release entry: `2026-08-13-admin-notification-read-boundary`.

### Исправлено

- Admin notification deliveries больше не загружают всю таблицу до latest top-500 на SQLite.

### Улучшено

- Status/template/search и абсолютная сортировка выполняются одним параметризованным SQLite-запросом; PostgreSQL использует LINQ/`Take`.

### Проверено

- Fail-first показал SQL без limit на `505` строках; after-fix notification/audit `12/12`, backend `1466/1466`, frontend `172/172`, masking/retry desktop/mobile `2/2`, fresh SQLite full flow, EF drift, encoding `18/18`, secret scan `697/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `703/723` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты, live payment, VPS/SSH/Ansible, Telegram/Bot API/SMTP и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.689.0 - 2026-08-13

Release entry: `2026-08-13-admin-audit-read-boundary`.

### Исправлено

- Admin audit больше не загружает всю разрешенную историю до временного окна и top-500.

### Улучшено

- SQLite применяет RBAC, пользовательские фильтры и `julianday`/`LIMIT` одним параметризованным запросом; PostgreSQL использует LINQ/`Take`.

### Проверено

- Fail-first показал SQL без limit на `505` строках; after-fix audit/RBAC/redaction `12/12`, backend `1465/1465`, frontend `172/172`, audit RBAC desktop/mobile `4/4`, fresh SQLite full flow, EF drift, encoding `18/18`, secret scan `696/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `702/722` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты, live payment, VPS/SSH/Ansible, Telegram/Bot API/SMTP и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.688.0 - 2026-08-13

Release entry: `2026-08-13-admin-order-read-boundary`.

### Исправлено

- Admin orders больше не загружают все заказы и все payment attempts до search/top-300.
- PaymentAttemptsCount и latest-payment readiness строятся отдельными bounded-агрегатами без неограниченного collection `Include`.

### Улучшено

- SQLite выполняет полный cross-table search и UTC-ordering до `LIMIT 300`, latest payment выбирается `ROW_NUMBER`; PostgreSQL использует LINQ/`DISTINCT ON`.

### Проверено

- Fail-first имел корректный response, но SQL без limit/window; after-fix order/finance backend `70/70`, backend `1464/1464`, frontend `172/172`, order/recheck/finance RBAC desktop/mobile `8/8`, fresh SQLite full flow, EF drift, secret scan `695/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `701/721` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты, live payment, VPS/SSH/Ansible, Telegram/Bot API/SMTP и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.687.0 - 2026-08-13

Release entry: `2026-08-13-admin-finance-read-boundary`.

### Исправлено

- Admin payments и refunds больше не загружают полные таблицы до top-300; webhook counts считаются только для выбранных платежей.
- Order payment recheck выбирает последнюю попытку top-1 в БД вместо загрузки всей истории заказа.

### Улучшено

- SQLite использует `julianday`/`LIMIT`, PostgreSQL применяет `OrderBy/Take`; refund/recheck readiness, DTO и RBAC не изменились.

### Проверено

- Fail-first вернул `305` refunds и SQL без limits; after-fix finance boundary и смежный backend `69/69`, backend `1463/1463`, frontend `172/172`, finance/recheck/refund lifecycle desktop/mobile `8/8`, fresh SQLite full flow, EF drift, secret scan `694/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `700/720` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты, live payment, VPS/SSH/Ansible, Telegram/Bot API/SMTP и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.686.0 - 2026-08-13

Release entry: `2026-08-13-admin-support-read-boundary`.

### Исправлено

- Общий список обращений поддержки больше не загружает всю таблицу до top-200.
- Сообщения выбранного обращения ограничены последними 200 строками в БД и возвращаются UI в хронологическом порядке.

### Улучшено

- SQLite использует `julianday` и `LIMIT`, PostgreSQL применяет `OrderBy/Take`; support DTO, internal-note visibility и optimistic lifecycle не изменились.

### Проверено

- Fail-first вернул `205` сообщений и SQL без limits; after-fix support boundary и смежный backend `26/26`, backend `1462/1462`, frontend `172/172`, support lifecycle desktop/mobile `8/8`, fresh SQLite full flow, EF drift, secret scan `693/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `699/719` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные Telegram/provider delivery, Bot API, SMTP, VPS/SSH/Ansible, live payment и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.685.0 - 2026-08-13

Release entry: `2026-08-13-admin-user-overview-read-boundary`.

### Исправлено

- Список пользователей и заказы, платежи и обращения в user overview больше не загружаются целиком до top-300/top-20.
- Фильтр роли сопоставляет полный CSV-токен без ложного совпадения `NotAdmin` по запросу `Admin`.

### Улучшено

- SQLite использует параметризованные bounded-запросы и `julianday`, PostgreSQL применяет `OrderBy/Take`; support overview связывает Telegram через SQL `EXISTS`.

### Проверено

- Fail-first `0/2` зафиксировал отсутствие SQL limits и substring role match; after-fix user/overview boundary и смежный backend `15/15`, backend `1461/1461`, frontend `172/172`, user overview lifecycle desktop/mobile `6/6`, fresh SQLite full flow, EF drift, secret scan `692/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `698/718` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider/Telegram/SMTP кабинеты, live payment и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.684.0 - 2026-08-13

Release entry: `2026-08-13-admin-subscription-access-read-boundary`.

### Исправлено

- Глобальные admin subscription/access lists и подписки/доступы user overview больше не загружают полные таблицы до top-300/top-20.
- Access history ограничивается top-5 на каждый latest access в SQL вместо неограниченного `Include` с последующей обрезкой в памяти.

### Улучшено

- SQLite использует `julianday`, CTE и `ROW_NUMBER`; PostgreSQL сохраняет indexed `ORDER BY/LIMIT`. DTO, terminal secret masking и порядок не изменились.

### Проверено

- Fail-first показывал корректный response count без SQL limits; after-fix SQL/full-flow `11/11`, смежный backend `64/64`, backend `1459/1459`, frontend `172/172`, subscription/access lifecycle desktop/mobile `6/6`, EF drift, secret scan `691/0` и dependency audit `0 vulnerabilities` зеленые.
- Roadmap `697/717` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider/Telegram/SMTP кабинеты, live payment и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.683.0 - 2026-08-13

Release entry: `2026-08-13-server-mode-action-boundary`.

### Исправлено

- Disable, maintenance on/off и allocation on/off требуют актуальную revision сервера; stale request и EF race возвращают controlled `409` без изменения состояния или audit trail.
- Browser mock повторяет backend state machine для `Draining`/`Ready`, поэтому lifecycle-тест проверяет фактические переходы статуса и доступности.

### Улучшено

- Пять mode-actions используют единый command-handler с одинаковой проверкой revision, повышением версии и обработкой `DbUpdateConcurrencyException`.
- Typed API client отправляет revision, а админка после конфликта загружает актуальный список и просит повторить действие.

### Проверено

- Fail-first SQLite `0/5`; after-fix server/operation boundary `97/97`, backend `1458/1458`, frontend `172/172`, typecheck/build, bundle `560247` raw/`148650` gzip, EF drift, secret scan `690/0` и dependency audit `0 vulnerabilities` зеленые. Валидный lifecycle и stale conflict desktop/mobile Playwright `4/4`.
- Roadmap `696/716` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider/Telegram/SMTP кабинеты, live payment и production-like 3x-ui evidence локально не проверялись; статус остается staging-ready baseline, not production-ready.

## 0.682.0 - 2026-08-13

Release entry: `2026-08-13-provisioning-management-boundary`.

### Исправлено

- Queue/precheck/retry/deploy/cancel/support-needed больше не выполняются по устаревшему состоянию сервера или provisioning run: HTTP требует revision, backend возвращает controlled `409`, а UI обновляет данные.
- Worker и coordinator повышают ревизии запуска и сервера при переходах; старые локальные SQLite-схемы получают колонку `ProvisioningRuns.Revision` через repair и миграцию.

### Улучшено

- Очередь claimable runs, lease recovery, admin list и detail steps ограничиваются в БД до materialization с SQLite-совместимой сортировкой.
- Provisioning list/detail/step/action DTO получили точную fail-closed форму; неизвестные и секретные поля отклоняются API client.

### Проверено

- Backend `1448/1448`, frontend `172/172`, typecheck/build/bundle budget и targeted provisioning browser `2/2` зелёные; fail-first подтвердил отсутствие revision-контракта, SQLite regressions проверяют bounded SQL, schema repair и stale command без side effects.
- Roadmap `695/715` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные VPS/SSH/Ansible, provider/Telegram/SMTP кабинеты, live payment и production-like 3x-ui evidence локально не проверялись; статус остаётся staging-ready baseline, not production-ready.

## 0.681.0 - 2026-08-13

Release entry: `2026-08-13-vpn-panel-management-boundary`.

### Исправлено

- Устаревшие PATCH/DELETE/default/client action/migration больше не перезаписывают параллельно измененные 3x-ui панели, inbound-правила и клиентов: HTTP требует актуальную revision и возвращает controlled `409`.
- Миграция клиента повторно проверяет revision после capacity reservation, освобождает резерв при конфликте и не вызывает 3x-ui с устаревшими данными.

### Улучшено

- Panel/inbound/client/history выборки получили DB-side bounds и SQLite-совместимую сортировку diagnostics; текстовые границы согласованы между EF, API client и формой.
- Админка после конфликта обновляет список и детали, закрывает stale editor и требует повторить действие с актуальной версией.

### Проверено

- X3Ui `89/89`, frontend `171/171`, backend `1444/1444`, typecheck/build/bundle budget, PostgreSQL EF drift, fresh SQLite, strict UTF-8, secret scan `688/0` и dependency audit `0 vulnerabilities` зеленые. Managed VPN lifecycle/conflicts desktop/mobile `10/10`, focused panel editor responsive/WCAG `1/1` на 320/390/1280 px.
- Roadmap `694/714` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/production-like 3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.680.0 - 2026-08-13

Release entry: `2026-08-13-server-management-boundary`.

### Исправлено

- Устаревший редактор или диалог удаления больше не перезаписывает и не удаляет VPN-сервер после параллельного изменения: PUT/DELETE требуют актуальную revision и возвращают controlled `409`.
- Admin API и frontend отклоняют неожиданные или секретные поля ответа сервера; текстовые границы согласованы с БД и формой.

### Улучшено

- Списки серверов и health-диагностика ограничиваются до materialization; `VpnNode.Revision` защищен EF concurrency token и миграцией.
- Редактор получил согласованные `maxLength`, write-only секреты и восстановление после конфликта на desktop/mobile.

### Проверено

- Server-management `75/75`, frontend `171/171`, backend `1435/1435`, typecheck/build/bundle budget, EF drift, fresh SQLite, strict UTF-8, secret scan `686/0` и dependency audit `0 vulnerabilities` зеленые. Stateful lifecycle/conflicts desktop/mobile `4/4`, focused responsive/WCAG `1/1` на 320/390/1280 px.
- Roadmap `693/713` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.679.0 - 2026-08-13

Release entry: `2026-08-13-tariff-boundary`.

### Исправлено

- Публичный API тарифов больше не раскрывает publication rules, internal allocation CSV, provisioning key и audit timestamps.
- Устаревшая admin-форма не может перезаписать, выключить или удалить параллельно измененный тариф; invalid features JSON больше не стирается молча.

### Улучшено

- Public/admin DTO разделены exact allow-list validators, create использует явный request без client-owned ID/audit fields, а PATCH отклоняет unknown, duplicate и no-op payloads.
- Списки ограничены DB-side top-200, `Tariff.Revision` защищён EF concurrency token и migration, а редактор получил тип и расписание публикации с frontend/backend границами.

### Проверено

- Fail-first tariff suite `17/27`; after-fix tariff/SQLite `33/33`, frontend `169/169`, backend `1424/1424`, typecheck/build/bundle budget, EF drift, fresh SQLite, strict UTF-8, secret scan и dependency audit `0 vulnerabilities` зелёные. CRUD desktop/mobile `2/2`, stale PATCH/DELETE `4/4`, public checkout `2/2`, render `2/2`, focused responsive/WCAG `1/1`.
- Roadmap `692/712` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.678.0 - 2026-08-13

Release entry: `2026-08-13-work-scenario-boundary`.

### Исправлено

- Устаревшая admin-форма больше не перезаписывает и не удаляет сценарий после параллельного изменения: PUT/DELETE требуют revision и возвращают controlled `409`.
- Поля сценария проверяются до записи по фактическим ограничениям БД; admin-форма показывает те же границы и ограничивает ввод.

### Улучшено

- Admin list ограничен DB-side top-200, `WorkScenario.Revision` защищён EF concurrency token и migration.
- Frontend принимает точный allow-list контракт сценария, а при конфликте перезагружает актуальную форму или список без потери внешнего изменения.

### Проверено

- Fail-first backend `0/3`, frontend `165/166`; targeted backend/SQLite `28/28`, EF drift `2/2`, frontend `167/167`, backend `1409/1409`, typecheck/build и bundle budget зелёные. Stale PUT/DELETE desktop/mobile `4/4`, file-backed SQLite race, fresh SQLite full flow, strict UTF-8, secret scan и dependency audit `0 vulnerabilities` зелёные.
- Roadmap `691/711` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.677.0 - 2026-08-13

Release entry: `2026-08-13-site-content-boundary`.

### Исправлено

- Публичный content API больше не раскрывает внутренние ID, редакторские поля, publication flags и audit timestamps; frontend принимает только `key/value`.
- Устаревшая admin-форма не может перезаписать или удалить параллельно изменённый блок: PUT/DELETE требуют revision и восстанавливают актуальную версию после controlled conflict.

### Улучшено

- Public/admin списки ограничены DB-side top-200; readiness считает totals/required states в БД и ограничивает duplicate diagnostics.
- `SiteContentBlock.Revision` защищён EF concurrency token и migration, а public/admin TypeScript-контракты разделены exact allow-list validators.

### Проверено

- Fail-first backend `5/7`, frontend `163/165`; targeted backend/SQLite `9/9`, EF drift `2/2`, frontend `165/165`, backend `1392/1392`, typecheck/build, bundle budget, CRUD/conflict/public/render/responsive gates и audit `0 vulnerabilities` зелёные. Первый full backend run имел transient timeout overrun `1391/1392`; isolated test и последовательный full rerun прошли.
- Roadmap `690/710` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.676.0 - 2026-08-13

Release entry: `2026-08-13-faq-boundary`.

### Исправлено

- Публичный FAQ больше не раскрывает внутренние идентификаторы, publication flags, порядок и audit timestamps; frontend принимает только вопрос, ответ и категорию.
- Устаревшая admin-форма не может перезаписать или удалить параллельно изменённую запись: PUT/DELETE требуют revision и возвращают controlled conflict с актуализацией формы и списка.

### Улучшено

- Public/admin списки, overview aggregates, category и duplicate diagnostics ограничены в БД; SQLite сохраняет регистронезависимый кириллический поиск.
- `FaqEntry.Revision` защищён EF concurrency token и migration, а public/admin TypeScript-контракты разделены и проверяются exact allow-list validators.

### Проверено

- Fail-first backend `0/2`, frontend `161/162`; targeted backend/SQLite `14/14`, frontend `163/163`, backend `1388/1388`, typecheck/build, bundle budget, FAQ CRUD/conflict/public/render/responsive gates, SQLite concurrency, EF drift, UTF-8 и audit `0 vulnerabilities` зелёные. Объединённый полный desktop Playwright run превысил 5-minute timeout без итогового отчёта и не засчитывался.
- Roadmap `689/709` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.675.0 - 2026-08-13

Release entry: `2026-08-13-app-release-boundary`.

### Исправлено

- Cabinet `latest/history` больше не раскрывают внутренние GUID, publication/source и actor/audit metadata релизов; frontend принимает только точный пользовательский контракт.
- Параллельно изменённый релиз нельзя перезаписать или удалить из устаревшей формы: PUT/DELETE требуют revision и возвращают controlled conflict с перезагрузкой актуального списка.

### Улучшено

- Пользовательская история ограничена последними 50 релизами, административный список — 200 релизами до materialization на SQLite/PostgreSQL.
- Overview считает агрегаты в БД и ограничивает список релизов без пунктов; `AppRelease.Revision` защищён EF concurrency token и migration.

### Проверено

- Fail-first backend `3/11`, frontend `0/1`; targeted backend `16/16`, frontend `3/3`, backend `1381/1381`, frontend `161/161`, typecheck/build, bundle budget, CRUD/conflict/browser responsive gates, SQLite concurrency, EF drift, UTF-8, secret scan `674/0` и audit `0 vulnerabilities` зелёные.
- Roadmap `688/708` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.674.0 - 2026-08-13

Release entry: `2026-08-13-admin-referral-program-boundary`.

### Исправлено

- Редактирование реферальной программы больше не стирает скрытые расширения правил, наград и anti-fraud настройки.
- Неизвестные, дублированные и пустые PATCH-команды отклоняются без мутации и ложной audit-записи; stale версия получает controlled conflict вместо перезаписи чужих изменений.

### Улучшено

- Список программ ограничен последними 200 записями на стороне SQLite/PostgreSQL; admin DTO и frontend используют optimistic revision.
- Границы названия, суммы, типа и единицы награды согласованы с backend, а legacy reward type сохраняется при редактировании.

### Проверено

- Fail-first backend/frontend `0/2`; targeted backend `13/13`, frontend `159/159`, backend `1373/1373`, typecheck/build, bundle budget, stateful CRUD и focused responsive/WCAG зелёные. Полная 25-viewport all-admin матрица не завершилась в лимит процесса; render/overlap и targeted 320/390/1280 px прошли.
- Roadmap `687/707` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.673.0 - 2026-08-13

Release entry: `2026-08-13-cabinet-referral-boundary`.

### Исправлено

- История реферальных начислений больше не materialize-ится целиком: кабинет получает последние 100 записей, административный экран — последние 200, с сортировкой и ограничением на стороне SQLite/PostgreSQL.
- Пользовательский API-клиент fail-closed отклоняет `userId`, `sourceUserId`, `referralProgramId`, `metadataJson` и другие служебные поля, при этом полный административный контракт сохранён.

### Улучшено

- Cabinet и admin используют общее русскоязычное форматирование типов и величин начисления: техническое `7 days` отображается как `7 дней`, валюты форматируются единообразно.

### Проверено

- Fail-first backend `0/1`, frontend `0/2`; targeted backend `11/11`, frontend `90/90`, backend `1367/1367`, frontend `157/157`, typecheck/build, cabinet desktop/mobile `2/2`, admin full flow `1/1`, all-screens/responsive `7/7`, fresh SQLite, EF drift и dependency audit `0 vulnerabilities` зелёные.
- Roadmap `686/706` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.672.0 - 2026-08-12

Release entry: `2026-08-12-cabinet-support-boundary`.

### Исправлено

- Cabinet support API больше не раскрывает user/Telegram/assignment/internal-note/attachment metadata в списке обращений, сообщениях, создании и ответе.
- История обращений и сообщений больше не materialize-ится целиком: БД возвращает последние 100 обращений и 200 видимых сообщений.

### Улучшено

- Отдельные `CabinetSupportConversationDto` и `CabinetSupportMessageDto` защищены fail-closed frontend validators; полный административный контракт и internal-note workflow сохранены.
- В списке обращений технический канал `web` отображается как «Личный кабинет», а последние сообщения возвращаются пользователю в хронологическом порядке.

### Проверено

- Fail-first backend `0/2`, frontend `0/1`; targeted backend/support/source guard `11/11`, backend `1365/1365`, frontend `156/156`, typecheck/build, support desktop/mobile Playwright `12/12`, cabinet all-screens desktop/responsive `2/2`, fresh SQLite, EF drift, `RoadmapCurrentStateTests` и dependency audit `0 vulnerabilities` зелёные.
- Roadmap `685/705` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/Telegram кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.671.0 - 2026-08-12

Release entry: `2026-08-12-cabinet-order-boundary`.

### Исправлено

- История и команды заказов кабинета больше не раскрывают `UserId`, checkout/provider metadata, служебные счетчики и административные признаки.
- Инициализация оплаты возвращает только идентификатор платежа и проверенный redirect URL; raw provider response и exception заменены безопасным пользовательским сообщением.
- Playwright ожидает готовность public, cabinet и admin SPA независимо, поэтому оставшийся Vite-процесс не маскирует недоступный экран.

### Улучшено

- Последние 100 заказов сортируются и ограничиваются на стороне БД; cabinet/admin DTO и read/command contracts разделены, frontend fail-closed отклоняет служебные поля.
- Каждый Playwright webServer запускает одну именованную SPA и fail-fast отклоняет неизвестное приложение.

### Проверено

- Fail-first backend `0/3` и provider exception `0/1`, frontend `0/2`, Playwright harness `0/1`; targeted backend `22/22`, backend `1362/1362`, frontend `154/154`, typecheck/build, public/cabinet desktop/mobile Playwright `118/118`, cabinet all-screens `2/2`, admin inventory `1/1`, admin responsive `1/1`, fresh SQLite, EF drift, `RoadmapCurrentStateTests` и dependency audit `0 vulnerabilities` зелёные.
- Roadmap `684/704` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались; статус остается staging-ready baseline, not production-ready.

## 0.670.0 - 2026-08-12

Release entry: `2026-08-12-cabinet-access-boundary`.

### Исправлено

- `/api/me/accesses` больше не раскрывает provider/client/server идентификаторы, служебные QR/config поля, revision и lifecycle timestamps.
- Дублирующий раздел «Выданные доступы» удалён; VPN-ключ и QR доступны в единственном пользовательском представлении.

### Улучшено

- Для кабинета выделен минимальный `CabinetAccessCredentialDto`, а frontend fail-closed отклоняет расширенный ответ со служебными полями.
- Последние 100 VPN-доступов сортируются и ограничиваются на стороне БД; административный DTO и защищённый QR endpoint сохранены.

### Проверено

- Fail-first backend `0/2`, frontend `0/1`; targeted backend `18/18`, frontend decoder/dashboard `11/11`, backend `1358/1358`, frontend `152/152`, typecheck/build, cabinet desktop/mobile Playwright `64/64`, targeted all-screens `2/2`, fresh SQLite, EF drift и dependency audit `0 vulnerabilities` зелёные.
- Roadmap `683/703` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались.

## 0.669.0 - 2026-08-12

Release entry: `2026-08-12-cabinet-subscription-boundary`.

### Исправлено

- `/api/me/subscriptions` больше не раскрывает `BlockReason`, внутренние server/payment идентификаторы, lifecycle diagnostics и пути QR/конфигурации.
- Карточка подписки использует защищённую QR-операцию и не отображает служебные пути; полный административный контракт не изменён.
- На компактных мобильных экранах header остаётся в потоке и не перекрывает кнопку полной инструкции после прокрутки.

### Улучшено

- Для кабинета выделен минимальный `CabinetSubscriptionDto`, а frontend fail-closed отклоняет расширенный ответ со служебными полями.
- Последние 100 подписок сортируются и ограничиваются на стороне БД; SQLite использует параметризованный UTC-совместимый запрос с `LIMIT 100`.

### Проверено

- Fail-first backend `0/2`, frontend `0/1`; targeted backend `17/17`, frontend decoder/dashboard `11/11`, backend `1357/1357`, frontend `151/151`, typecheck/build, cabinet desktop/mobile Playwright `64/64` и targeted all-screens `2/2` зелёные.
- Roadmap `682/702` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались.

## 0.668.0 - 2026-08-12

Release entry: `2026-08-12-cabinet-payment-boundary`.

### Исправлено

- История платежей кабинета больше не получает внутренний `StatusReason`, exception провайдера, webhook/event/idempotency данные и другие административные поля.
- JSON-экспорт заказа использует безопасное пользовательское сообщение о статусе и не содержит return URL или признаков проверки подписи.

### Улучшено

- Cabinet API возвращает отдельный минимальный DTO; frontend fail-closed отклоняет служебные поля. Последние 100 платежей сортируются и ограничиваются на стороне БД, включая SQLite.

### Проверено

- Fail-first backend/frontend `0/3`; targeted backend `2/2`, API/payment regression `75/75`, backend `1356/1356`, frontend `150/150`, typecheck/build, cabinet Playwright `32/32`, targeted desktop/mobile `2/2`, fresh SQLite purchase/VPN access, EF drift и dependency audit `0 vulnerabilities` зелёные.
- Roadmap `681/701` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались.

## 0.667.0 - 2026-08-12

Release entry: `2026-08-12-admin-webhook-event-boundary`.

### Исправлено

- Admin webhook event API больше не передаёт в браузер внутренний `ErrorText`, который мог содержать exception/diagnostics провайдера или verifier-а.
- Последние 200 webhook events сортируются и ограничиваются на стороне БД; SQLite использует эквивалентную UTC-сортировку, не загружая всю таблицу в память.

### Улучшено

- Единое application-правило определяет terminal/retryable/attention состояние с десятиминутным lease. Admin UI показывает время, подпись, безопасные идентификаторы и remediation-состояние; frontend fail-closed отклоняет неизвестные status и диагностические поля.

### Проверено

- Fail-first backend/frontend `0/2`; webhook/API regression `44/44`, backend Release `1355/1355`, frontend `149/149`, typecheck/build, full admin Playwright `54/54`, targeted desktop/mobile `2/2`, responsive admin matrix `1/1` на всех representative viewport, fresh SQLite, EF drift, dependency audit `0 vulnerabilities`, strict UTF-8 и secret scan `675/0` зелёные.
- Roadmap `680/700` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные webhook/provider кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались.

## 0.666.0 - 2026-08-12

Release entry: `2026-08-12-refund-create-recovery`.

### Исправлено

- Неопределённый исход создания возврата YooKassa, Stripe или PayPal больше не оставляет платёж навсегда заблокированным: администратор может безопасно повторить точную операцию с теми же суммой и причиной.
- Admin refund create/recheck не раскрывает provider payload, status reason или transport exception; операторский audit сохраняется до внешнего вызова и на failure path.

### Улучшено

- API возвращает durable `Unknown` refund как `202 Accepted`, UI автоматически обновляет список и показывает команду «Повторить возврат» только при подтверждённой provider idempotency. Причина нормализуется и ограничена 120 символами; Т-Банк остаётся fail-closed.

### Проверено

- Fail-first backend `0/3`, browser `0/2`; targeted refund/payment regression `86/86`, backend Release `1354/1354`, frontend `148/148`, typecheck/build, desktop/mobile refund Playwright `6/6`, fresh SQLite, EF drift, dependency audit `0 vulnerabilities`, strict UTF-8 и secret scan `673/0` зелёные.
- Roadmap `679/699` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные refund/provider кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались.

## 0.665.0 - 2026-08-12

Release entry: `2026-08-12-admin-payment-recheck-boundary`.

### Исправлено

- Admin payment/order recheck больше не возвращает raw provider response, status reason или transport exception в браузер.
- Попытка ручной сверки сохраняет actor-aware audit до внешнего provider call, поэтому отказ сети не стирает операторское действие.

### Улучшено

- Оба endpoint выполняют единый readiness preflight, возвращают минимальный `orderId/paymentId/status` DTO и безопасные русские ошибки; frontend validator fail-closed отклоняет расширенный ответ.

### Проверено

- Fail-first `0/3`; targeted payment/admin/concurrency regression `92/92`, backend Release `1345/1345`, frontend `145/145`, typecheck/build, desktop/mobile Playwright `2/2`, fresh SQLite, EF drift, dependency audit `0 vulnerabilities`, strict UTF-8 и secret scan `673/0` зелёные.
- Roadmap `678/698` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты и VPS/staging/payment/3x-ui evidence локально не закрывались.

## 0.664.0 - 2026-08-12

Release entry: `2026-08-12-refund-status-reconciliation`.

### Исправлено

- Незавершённые возвраты YooKassa, Stripe и PayPal теперь можно сверить по отдельному provider refund ID; terminal failure/cancellation снимает блокировку следующего возврата, а успешная сумма применяется идемпотентно.
- Т-Банк не использует агрегированный payment `GetState` как ложное доказательство конкретного частичного возврата и явно сообщает, что такая сверка не поддерживается.

### Улучшено

- Admin API/UI показывают readiness и причины блокировки, предоставляют команду «Сверить возврат» и записывают create/recheck в аудит без raw provider payload и причины клиента.

### Проверено

- Fail-first `0/5`; targeted SQLite/provider/controller `52/52`, production adapter GET matrix `3/3`, backend Release `1342/1342`, frontend `144/144`, typecheck/build, desktop/mobile Playwright `2/2`, fresh SQLite, EF drift, audit `0 vulnerabilities`, UTF-8 и secret scan `673/0` зелёные.
- Roadmap `677/697` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Live refund/provider cabinet и VPS/staging/payment/3x-ui evidence локально не закрывались.

## 0.663.0 - 2026-08-12

Release entry: `2026-08-12-refund-proof-boundary`.

### Исправлено

- YooKassa, Stripe, PayPal и Т-Банк больше не изменяют локальную сумму возврата по успешному ответу, который относится к другой provider-транзакции или содержит другую сумму/валюту/internal payment reference.
- Т-Банк использует отдельный детерминированный ID операции возврата, поэтому последовательные частичные возвраты одного платежа не конфликтуют с уникальным индексом БД.

### Улучшено

- PayPal refund запрашивает полное представление ответа; успешные production refund responses без обязательного provider proof переводятся в `Unknown` и требуют сверки.

### Проверено

- Fail-first `0/7`; после исправления production adapters + SQLite `14/14`, backend `1328/1328`, fresh SQLite и EF drift зелёные, secret scan `673/0`.
- Roadmap `676/696` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Live refund/provider cabinet и VPS/staging/payment/3x-ui evidence локально не закрывались.

## 0.662.0 - 2026-08-12

Release entry: `2026-08-12-payment-status-proof-boundary`.

### Исправлено

- Manual recheck Stripe, YooKassa и Т-Банка больше не маскирует чужой provider payment ID сохранённым локальным значением и не активирует заказ при несовпадении ID, суммы, валюты, merchant account или internal order reference.
- Stripe `checkout.session.completed` без подтверждённого paid status и Т-Банк `CONFIRMED` при `Success=false` остаются неопределёнными, а не успешными платежами.

### Улучшено

- Успешный production status response обязан содержать доступное провайдеру payment proof; YooKassa status recheck сверяет сумму и валюту webhook со своим API.

### Проверено

- Fail-first `0/4`; после исправления direct/SQLite `12/12`, payment regression `99/99`, backend Release `1316/1316`, frontend `144/144`, responsive `7/7` за `9.2 min`, полный Playwright `227/227` за `12.9 min`.
- Roadmap `675/695` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider кабинеты и VPS/staging/live payment/3x-ui evidence локально не закрывались.

## 0.661.0 - 2026-08-12

Release entry: `2026-08-12-paypal-approved-order-capture`.

### Исправлено

- Верифицированный `CHECKOUT.ORDER.APPROVED` теперь запускает server-side PayPal order capture вместо окончательного сохранения `WaitingConfirmation` без списания.
- Capture response проверяется по order/capture ID, payment/order reference, сумме и валюте; один `COMPLETED` order без capture proof больше не считается оплатой.

### Улучшено

- Повтор после неопределённого capture outcome использует стабильный `PayPal-Request-Id` и GET-reconciliation, а webhook остаётся retryable до подтверждённого результата.

### Проверено

- Fail-first SQLite `0/1`; после исправления direct/SQLite payment regression `87/87`, backend Release `1304/1304`, frontend `144/144`, Playwright `227/227` за `12.3 min`.
- Roadmap `674/694` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальный PayPal sandbox order/capture и provider/VPS/staging/3x-ui evidence локально не закрывались.

## 0.660.0 - 2026-08-12

Release entry: `2026-08-12-paypal-capture-id-refund`.

### Исправлено

- PayPal refund больше не подставляет ID completed order в endpoint, который принимает только capture ID.
- Resolver сначала читает `purchase_units[].payments.captures[].id`; прямой `resource.id` используется только как fallback для capture webhook.

### Проверено

- Fail-first direct + SQLite `2/4` зафиксировал `/captures/ORDER-1/refund`; после исправления `4/4`, payment/refund regression `75/75`, backend Release `1300/1300`.
- Roadmap `673/693` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальный PayPal refund и provider/VPS/staging/3x-ui evidence локальными проверками не закрывались.

## 0.659.0 - 2026-08-12

Release entry: `2026-08-12-provider-refund-payload-recovery`.

### Исправлено

- Stripe и PayPal refund больше не падают на повреждённом legacy `WebhookPayload`/`RawResponse` до обращения к провайдеру.
- Локальная порча истории не создаёт ложный `Unknown` refund reservation с сообщением о неопределённом внешнем результате.

### Улучшено

- Payment-intent Stripe и capture ID PayPal восстанавливаются через checkout session/order API, если сохранённый payload нельзя разобрать.
- Ошибки provider API и сетевые неопределённости по-прежнему остаются fail-closed и требуют ручной сверки.

### Проверено

- Direct fail-first `0/2`; после исправления direct adapters и SQLite orchestrator `4/4`, payment/refund regression `75/75`, backend Release `1300/1300`, frontend `144/144`, Playwright `227/227`.
- Roadmap `672/692` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные Stripe/PayPal refund, provider/VPS/staging/3x-ui evidence локальными проверками не закрывались.

## 0.658.0 - 2026-08-12

Release entry: `2026-08-12-responsive-visual-oracle`.

### Исправлено

- Admin VPN actions больше не перекрываются на 521 px; поля периода реферальной программы и редактор пунктов релиза не заходят на соседние controls на 1280 px.
- Cabinet payment-expiry E2E ждёт завершения post-retry reload перед переводом browser clock и не создаёт ложные API timeout.

### Улучшено

- All-screens gate проверяет clipping обычного контента, dialog bounds и перекрытия реально видимых controls с учётом scroll/clipping ancestors и modal layer.
- Public routes и admin sections берутся из typed production inventory, поэтому новый экран не может тихо выпасть из browser audit.

### Проверено

- Frontend `144/144`; backend Release `1296/1296`; полный Playwright `227/227` за `15.0 min`; typecheck/build, admin bundle budget, dependency audit `0 vulnerabilities` и fresh SQLite зелёные.
- Roadmap `671/691` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/VPS/staging/3x-ui evidence локальными проверками не закрывались.

## 0.657.0 - 2026-08-12

Release entry: `2026-08-12-telegram-stars-charge-lifecycle`.

### Исправлено

- Telegram Stars не отправляет второй invoice после повторного callback, успешной оплаты или неопределённого transport outcome.
- Pre-checkout отклоняет отменённые, истёкшие и оплаченные заказы; callback после successful_payment больше не создаёт конфликтующую payment attempt.
- `successful_payment` сохраняет unknown payload и terminal-order charge для ручной сверки; второе списание фиксируется отдельно без повторной выдачи VPN.

### Проверено

- Telegram purchase flow `48/48`; backend Release `1296/1296`; frontend `142/142`; полный Playwright `226/226` за `12.3 min`; typecheck/build, bundle budget, dependency audit `0 vulnerabilities`, fresh SQLite и EF drift зелёные.
- Roadmap `670/690` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные Telegram Stars/Bot API, provider/VPS/staging evidence локальными проверками не закрывались.

## 0.656.0 - 2026-08-12

Release entry: `2026-08-12-telegram-payment-recovery-provider-lock`.

### Исправлено

- Telegram recovery после первой payment attempt показывает только закреплённый способ оплаты; истёкший или закрытый заказ больше не получает ложные provider-кнопки.
- Ошибки внешнего провайдера и Telegram Stars не раскрывают пользователю exception text, служебный invoice payload или инструкции по настройке `BotToken`.
- Frontend source-contract test синхронизирован с уже действующим snapshot-aware blocker повторной оплаты продления.

### Проверено

- Telegram purchase flow `39/39`; backend Release `1287/1287`; frontend `142/142`; полный Playwright `226/226` за `12.4 min`; typecheck/build и dependency audit `0 vulnerabilities`.
- Exhaustive admin responsive inventory сначала упёрся в старый `600 s` budget после `225/226`, изолированно прошёл за `8.1 min`; timeout повышен до `900 s` без ослабления viewport/WCAG проверок, после чего единый gate прошёл полностью. Fresh SQLite checkout/webhook/subscription/VPN, EF drift и secret scan `673/0` зелёные.
- Roadmap `669/689` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные Telegram/provider/VPS/staging evidence локальными проверками не закрывались.

## 0.655.0 - 2026-08-12

Release entry: `2026-08-12-payment-init-order-provider-snapshot`.

### Исправлено

- Payment init теперь отклоняет провайдера, который не совпадает с неизменяемым снимком заказа, до account lookup, reservation и adapter call.
- Public checkout и кабинет повторяют оплату по `Order.PaymentProvider`, даже если форма или сохранённая checkout session содержит другой выбранный провайдер; недоступный snapshot-провайдер блокирует кнопку точной причиной.
- Telegram закрепляет выбранный способ оплаты за живым pending-заказом до первой попытки, после чего смена провайдера запрещена; пустой provider payment ID не переводит reservation в `Pending` и не раскрывает redirect URL.

### Проверено

- Fail-first init boundary `0/2`; после исправления targeted Order/Telegram/init `63/63`, payment/checkout browser regression `26/26`.
- Backend `1283/1283`; frontend `142/142`; полный Playwright `226/226` за `12.3 min`; Release build `0` warnings/errors, typecheck/build, EF drift, fresh SQLite и dependency audit зелёные.
- Secret scan `673/0`; Roadmap `668/688` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked. Реальные provider/VPS/staging evidence локальными проверками не закрывались.

## 0.654.0 - 2026-08-12

Release entry: `2026-08-12-payment-recheck-account-readiness-preflight`.

### Исправлено

- Manual recheck теперь проверяет provider/mode snapshot, enabled/Disabled state, provider payment ID и обязательные credentials до разрешения адаптера и внешнего вызова.
- Admin orders/payments DTO и UI различают capability и фактическую готовность, показывают точный blocker и не отправляют POST для поддерживаемого, но недоступного аккаунта.
- Ответ адаптера с несовпадающим provider payment ID отклоняется до применения статуса и любых audit/outbox изменений.

### Проверено

- Fail-first account/config `0/7` и provider-ID `0/1`; после исправления targeted backend `48/48`, payment regression `237/237`, Local sandbox adapters `3/3` без HTTP.
- Backend `1279/1279`; frontend `141/141`; полный Playwright `222/222` за `13.2 min`; typecheck/build, EF drift и dependency audit зелёные.
- Roadmap: `667/687` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; реальные provider/VPS/staging evidence локальными проверками не закрывались.

## 0.653.0 - 2026-08-12

Release entry: `2026-08-12-payment-refund-account-readiness-preflight`.

### Исправлено

- `PaymentOrchestrator` теперь повторяет account/config readiness на свежем snapshot до durable refund reservation и provider call: проверяет provider/mode snapshot, enabled/Disabled state, provider payment ID и обязательные merchant credentials.
- Admin readiness и service preflight используют один application rule; direct caller больше не может выполнить возврат через выключенный, несовпадающий или неполностью настроенный аккаунт и создать ложный успешный либо `Unknown` outcome.

### Проверено

- Fail-first `0/7`: все invalid account/config states завершали возврат успешно; после исправления service/API/DI boundary `13/13`, payment regression `153/153`.
- Backend `1268/1268`; Release build `0` warnings/errors. Актуальные frontend `141/141` и полный Playwright `220/220` за `13.6 min` остаются применимыми, UI/DTO не менялись.
- Roadmap: `666/686` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; реальные provider/VPS/staging evidence локальными проверками не закрывались.

## 0.652.0 - 2026-08-12

Release entry: `2026-08-12-payment-local-sandbox-refund-contract`.

### Исправлено

- Credentialless local sandbox аккаунты Stripe, PayPal и TBank теперь завершают refund детерминированно и без внешнего HTTP, как уже делали checkout и manual recheck.
- Admin refund readiness использует тот же environment-aware контракт и больше не требует реальные provider secrets для локальных seed-аккаунтов; в Production и для sandbox с credentials требования не ослаблены.

### Проверено

- Fail-first: `0/6` provider/readiness cases с ошибками обязательных credentials; после исправления targeted SQLite/provider/rules `21/21`, payment regression `92/92`.
- Backend `1255/1255`; Release build `0` warnings/errors. Актуальные frontend `141/141` и полный Playwright `220/220` за `13.6 min` остаются применимыми, UI/DTO не менялись.
- Roadmap: `665/685` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; реальные provider/VPS/staging evidence локальными sandbox-проверками не закрывались.

## 0.651.0 - 2026-08-12

Release entry: `2026-08-12-payment-refund-capability-preflight`.

### Исправлено

- `PaymentOrchestrator` теперь сам отклоняет возврат для провайдера без refund capability до order gate, idempotency lookup, durable reservation, adapter resolution и внешнего вызова.
- Прямой service caller больше не может обойти admin readiness и создать ложный успешный либо `Unknown` refund для RoboKassa, YooMoney, CloudPayments или Prodamus.

### Проверено

- Fail-first direct orchestrator regression воспроизвел успешный unsupported refund; после исправления refund/concurrency/webhook suite `22/22` подтверждает нулевые factory/provider calls, пустую таблицу refunds и неизменный платеж.
- Backend `1240/1240`; Release build `0` warnings/errors; актуальный frontend `141/141` и полный Playwright `220/220` за `13.6 min` остаются применимыми, UI/DTO не менялись.
- Roadmap: `664/684` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; реальные provider/VPS/staging evidence локальными проверками не закрывались.

## 0.650.0 - 2026-08-12

Release entry: `2026-08-12-payment-manual-recheck-capability-guard`.

### Исправлено

- Ручная перепроверка платежа теперь использует единый provider capability contract: неподдерживаемые RoboKassa, YooMoney, CloudPayments и Prodamus отклоняются до разрешения адаптера и внешнего вызова.
- Admin orders/payments DTO явно передают доступность перепроверки; строгий API client и UI fail-closed блокируют команду с точной причиной, а поддерживаемые YooKassa, TBank, Stripe и PayPal сохраняют прежний flow.
- Cabinet browser regression дожидается завершения reload после создания продления до снимка счетчика следующего ручного refresh, исключая ложный duplicate-GET результат.

### Проверено

- Payment/admin targeted backend `56/56`, frontend `141/141`, capability policy unit, admin desktop/mobile `2/2`; конкурентный cabinet refresh после уточнения синхронизации `3/3`.
- Backend `1239/1239`; Release build `0` warnings/errors; frontend typecheck/build зелёные; полный Playwright `220/220` за `13.6 min` без failed/flaky/skipped; EF drift отсутствует, fresh SQLite smoke зелёный, audit `0` уязвимостей.
- Roadmap: `663/683` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; реальные provider/VPS/staging evidence локальными проверками не закрывались.

## 0.649.0 - 2026-08-12

Release entry: `2026-08-12-payment-checkout-url-readiness-guard`.

### Исправлено

- Legacy payment account с credential-bearing или non-HTTP API/return/webhook/hosted URL больше не считается готовым, не публикуется пользователю и не позволяет создать тупиковую checkout session.
- Публичный checkout return URL проверяется как credential-free `http/https`, нормализуется до persistence и сохраняется через структурный `JsonSerializer` вместо ручной вставки в JSON.

### Проверено

- Fail-first readiness/public/checkout `5/30`; отдельный return URL/JSON fail-first `4/4`; после исправления payment regression `76/76`, targeted public/cabinet desktop/mobile `8/8`.
- Backend `1238/1238`; frontend `140/140`; typecheck/build всех приложений зелёные; полный Playwright `218/218` за `11.5 min` без failed/flaky/skipped; EF drift и fresh SQLite зелёные; audit `0` уязвимостей, secret scan `671/0`.
- Roadmap: `662/682` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; реальные provider/VPS/staging evidence локальными проверками не закрывались.

## 0.648.0 - 2026-08-12

Release entry: `2026-08-12-vpn-public-endpoint-protocol-guard`.

### Исправлено

- Server и work-scenario API принимают только `vless`, `vmess`, `trojan`, валидируют public hostname/port и нормализуют protocol CSV; admin UI использует ограниченные списки и показывает endpoint diagnostics до submit.
- Node allocator сопоставляет протокол по точному CSV-токену; provisioning queue и VPN provider повторяют preflight для legacy/internal callers, а sandbox URI fail-closed валидирует config endpoint и корректно оформляет IPv6 authority.
- VLESS/VMess/Trojan generator отклоняет port вне `1..65535`; Ansible node metadata экранирует строковые значения через `to_json` вместо прямой вставки в JSON.

### Проверено

- Fail-first: backend `8/25`, sandbox endpoint `0/2`; после исправления затронутый backend `161/161`, runner `5/5`, frontend helper `4/4`, valid desktop/mobile lifecycle `2/2`, negative semantic desktop/mobile `2/2`.
- Backend `1229/1229`; frontend `140/140`; typecheck/build всех приложений зелёные; targeted responsive admin matrix `1/1` за `6.9 min` на всех 25 viewport-конфигурациях.
- Полный Playwright `218/218` за `11.7 min`, без failed/flaky/skipped; EF drift отсутствует, fresh SQLite smoke зелёный, release/encoding gate `59/59`, dependency audit `0` уязвимостей, secret scan `671/0`.
- Roadmap: `661/681` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; real VPS/staging/3x-ui evidence не заменялся локальными проверками.

## 0.647.0 - 2026-08-12

Release entry: `2026-08-12-provisioning-inventory-argument-guard`.

### Исправлено

- Server API, own-VPS onboarding и provisioning queue отклоняют невалидные IP/SSH username до node/run/step/audit mutation; заполненный `IpAddress` больше не обходит проверку валидного `Host`.
- Executor повторяет target/credential preflight до создания workdir и запуска process, использует безопасный фиксированный inventory alias и передаёт Python каждый аргумент через `ProcessStartInfo.ArgumentList` без ручного quoting.
- Legacy SSH key path не допускает whitespace, а create/update сохраняет нормализованный host без завершающего `/`; admin-форма показывает IP/SSH diagnostics до submit на desktop/mobile.

### Проверено

- Fail-first: inventory/process boundary `0/9`, own-VPS invalid input `5/6`, host normalization `0/2`, credential matrix `22/25`; после исправления boundary `15/15`, normalization `2/2`, credential matrix `25/25`, затронутый server/provisioning contour `134/134`.
- Backend `1211/1211`; Release build `0` warnings/errors; EF/fresh SQLite `15/15`; frontend `139/139`; typecheck/build всех приложений и audit `0 vulnerabilities` зелёные.
- Targeted admin desktop/mobile `2/2`; полный browser inventory `218/218` за `11.2 min` (`52+62+98+6`) без failed/flaky/skipped.
- Fresh SQLite flow подтвердил latest release; seed содержит `646` записей, encoding/release guards `57/57`, secret scan `670` files/`0` findings.
- Roadmap: `660/680` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; real VPS/staging/3x-ui evidence не заменялся локальными проверками.

## 0.646.0 - 2026-08-12

Release entry: `2026-08-12-provisioning-credential-preflight`.

### Исправлено

- Direct create/update VPN-сервера принимает в legacy `SshPrivateKeyPath` только абсолютный Unix filesystem path без control/quote-символов; raw private key, protected marker и validation placeholder отклоняются до записи node/audit.
- Provisioning queue проверяет исполнимость SSH credential до создания run: orphan reference, non-validation placeholder, password credential и невалидный legacy payload завершаются controlled failure.
- `credentialsConfigured` больше не считается истинным только из-за credential reference без protected payload; validation node сохраняет безопасный mock-контракт.

### Проверено

- SQLite fail-first `0/11`; после исправления negative/positive credential matrix `22/22`, расширенный provisioning/API/materializer/executor/coordinator контур `119/119`.
- Backend `1196/1196`; Release build `0` warnings/errors; frontend `136/136`; typecheck/build всех приложений и audit `0 vulnerabilities` зелёные.
- UI не менялся; актуальный полный browser inventory остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- EF drift отсутствует; fresh SQLite flow подтвердил latest release; seed содержит `645` записей, encoding/release guards `57/57`, secret scan `668` files/`0` findings.
- Roadmap: `659/679` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; real VPS/staging/3x-ui evidence не заменялся локальными проверками.

## 0.645.0 - 2026-08-12

Release entry: `2026-08-12-validation-deploy-executor-guard`.

### Исправлено

- `validation-mode:true` теперь имеет приоритет над глобальными live-флагами непосредственно в executor: process, SSH и Ansible не запускаются.
- Non-validation deploy при `LiveExecutionEnabled=false` больше не завершается ложным mock success и возвращает controlled failure до создания workdir/process.
- Dry-run без live execution сохраняет безопасный mock-контракт; настоящий live deploy по-прежнему требует оба глобальных флага и explicit node tag.

### Проверено

- Runtime fail-first `0/2`: validation node запустил canary runner, а выключенный live execution вернул mock success; после исправления executor `3/3`, provisioning/worker/sandbox контур `49/49`.
- Backend `1174/1174`; Release build `0` warnings/errors; frontend `136/136`; typecheck/build всех приложений и audit `0 vulnerabilities` зелёные.
- UI не менялся; актуальный полный browser inventory остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- EF drift отсутствует; fresh SQLite flow подтвердил latest release; seed содержит `644` записи, encoding/release guards `57/57`, secret scan `668` files/`0` findings.
- Roadmap: `658/678` closed, readiness `97.1%`, `20` remaining, `19` open, `1` in progress, `0` blocked; real VPS/staging/3x-ui evidence не заменялся локальными проверками.

## 0.644.0 - 2026-08-12

Release entry: `2026-08-12-server-secret-protection-fail-closed`.

### Исправлено

- Direct create/update VPN-сервера больше не подтверждает сохранение нового SSH или panel secret, если обязательный `ISecretProtector` недоступен.
- Необратимый `validation-placeholder` больше не создаётся HTTP write-путями и не помечается как готовый protected credential.
- Secret write возвращает controlled `503` до node/audit mutation; metadata-only create/update остаётся доступен без protector.

### Проверено

- SQLite fail-first `0/4`: create/update для SSH и panel secret возвращали `200` и сохраняли placeholder; после исправления secret boundary `6/6`, полный server/security suite `49/49`.
- Backend `1172/1172`; Release build `0` warnings/errors; frontend `136/136`; typecheck/build всех приложений и audit `0 vulnerabilities` зелёные.
- UI не менялся; актуальный полный browser inventory остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- EF drift отсутствует; fresh SQLite flow подтвердил latest release; seed содержит `643` записи, encoding/release guards `57/57`, secret scan `668` files/`0` findings.
- Roadmap: `657/677` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; real VPS/staging/3x-ui evidence не заменялся локальными проверками.

## 0.643.0 - 2026-08-12

Release entry: `2026-08-12-server-payload-validation`.

### Исправлено

- Direct create/update VPN-сервера больше не обходит semantic validation административной формы и не подменяет невалидные capacity/priority/ports скрытыми defaults.
- Пустое имя и неположительный panel inbound ID отклоняются до node/audit mutation.
- Неизвестный `NodeGroupId` возвращает controlled `400` вместо FK exception/`500`; существующая группа принимается для create/update.

### Проверено

- SQLite fail-first `0/14`: двенадцать payload-вариантов вернули `200`, два unknown node-group завершились FK exception; после исправления negative/positive server matrix и весь server-management suite `34/34`.
- Backend `1166/1166`; Release build `0` warnings/errors; frontend `136/136`; typecheck/build всех приложений и audit `0 vulnerabilities` зелёные.
- UI не менялся; актуальный полный browser inventory остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- EF drift отсутствует; fresh SQLite flow подтвердил latest release; seed содержит `642` записи, encoding/release guards `57/57`, secret scan `668` files/`0` findings.
- Roadmap: `656/676` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; real VPS/staging/3x-ui evidence не заменялся локальными проверками.

## 0.642.0 - 2026-08-12

Release entry: `2026-08-12-access-lifecycle-fail-closed`.

### Исправлено

- Admin enable/disable больше не меняют только локальный credential и не возвращают `200`, если обязательный VPN lifecycle-сервис отсутствует.
- Enable, disable, sync и reset-traffic возвращают единый ранний `503 Service Unavailable` до чтений, lifecycle gate, provider-вызовов и записей.
- Terminal/expired access guards проверяются с настоящим lifecycle и fail-on-call provider factory, поэтому доменный отказ не маскируется отсутствующей зависимостью.

### Проверено

- SQLite fail-first `0/4`: enable/disable возвращали ложный `200`, sync/reset использовали `400`; после исправления lifecycle/admin targeted `58/58`, boundary `11/11`.
- Backend `1150/1150`; Release build `0` warnings/errors; frontend `136/136`; typecheck/build всех приложений и audit `0 vulnerabilities` зелёные.
- UI не менялся; актуальный полный browser inventory остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- EF drift отсутствует; fresh SQLite flow подтвердил latest release; seed содержит `641` запись, encoding/release guards `57/57`, secret scan `668` files/`0` findings.
- Roadmap: `655/675` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.641.0 - 2026-08-12

Release entry: `2026-08-12-access-revision-sequence`.

### Исправлено

- Успешные `sync` и `reset-traffic` теперь повышают access revision вместе с durable timestamp/provider mutation и возвращают актуальную версию в API.
- Provider error/cancellation, которые сохраняют `Error` или `SyncRequired`, больше не меняют status с прежней revision.
- Fallback admin enable/disable и subscription expiry worker используют ту же последовательность, включая retry `Error -> Disabled`.

### Проверено

- Fail-first: `8` lifecycle/API и `3` expiry assertions воспроизвели устаревшую revision; после исправления targeted `30/30`, expiry `6/6`, смежные access/admin/subscription/X3Ui `160/160`.
- Backend `1147/1147`; Release build `0` warnings/errors; fresh SQLite order/payment/subscription/access flow, EF drift и formatter по изменённым C# файлам зелёные.
- Frontend `136/136`, typecheck/build всех приложений и audit `0 vulnerabilities`; UI не менялся, актуальный полный browser inventory остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- Encoding/documentation/release guards и latest release SQLite verification выполнены после синхронизации seed (`640` entries); secret scan `668` files/`0` findings, временные smoke-артефакты очищены.
- Roadmap: `654/674` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.640.0 - 2026-08-12

Release entry: `2026-08-12-access-sync-read-failure`.

### Исправлено

- Read-only ошибка `SyncAccessAsync/GetUsageAsync` больше не переводит рабочий `Active` или намеренно `Disabled` доступ в `Error`.
- Provider read failure сохраняет `DisabledAt`, `LastSyncedAt` и revision, записывая только redacted `provider_read_failed` history/audit.
- Monitoring failure теперь отделён от local persistence failure после успешного чтения и от необратимого traffic reset.

### Проверено

- Валидный SQLite fail-first `0/2`; после исправления active/disabled `2/2`, смежные lifecycle/admin/expiry `59/59`.
- Backend `1146/1146`; Release build `0` warnings/errors; fresh SQLite, EF drift и formatter по изменённым C# файлам зелёные.
- Frontend `136/136`, typecheck/build всех приложений и audit `0 vulnerabilities`; UI не менялся, актуальный полный browser inventory остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- Encoding/documentation/release guards и latest release SQLite verification выполнены после синхронизации seed (`639` entries); secret scan `668` files/`0` findings, временные smoke-артефакты очищены.
- Roadmap: `653/673` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.639.0 - 2026-08-12

Release entry: `2026-08-12-access-sync-reset-persistence`.

### Исправлено

- Завершённый provider sync больше не сохраняет новый `LastSyncedAt`, ложный success audit и статус `Error`, если локальный commit завершился ошибкой или поздней отменой.
- Traffic reset после local persistence failure очищает staged success evidence и сохраняет один `SyncRequired` reconciliation record с новой revision.
- Cancellation после успешного возврата provider отличается от cancellation внутри provider: durable failure/cancel evidence сохраняется независимо, затем исходная отмена пробрасывается вызывающему коду.

### Проверено

- Валидный SQLite fail-first `0/4`; после исправления completed-action suite `4/4`, смежные lifecycle/admin/expiry `57/57`.
- Backend `1144/1144`; Release build `0` warnings/errors; fresh SQLite order/payment/subscription/access flow, EF drift и formatter по изменённым C# файлам зелёные.
- Frontend `136/136`, typecheck/build всех приложений и audit `0 vulnerabilities`; UI не менялся, актуальный полный browser inventory остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- Encoding/documentation/release guards и latest release SQLite verification выполнены после синхронизации seed (`638` entries); secret scan `668` files/`0` findings, временные smoke-артефакты очищены.
- Roadmap: `652/672` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.638.0 - 2026-08-12

Release entry: `2026-08-12-access-state-compensation`.

### Исправлено

- Lifecycle `enable/disable` теперь отличает ошибку провайдера от локального `SaveChanges` failure после уже завершённой внешней мутации.
- При локальном сбое сервис удаляет незакоммиченные success history/audit и выполняет обратный provider-вызов с независимым токеном; успешная компенсация сохраняет исходный локальный статус.
- Если rollback не удался, доступ получает `SyncRequired`, новую revision и отдельные redacted history/audit с `provider_state_unknown`; поздняя отмена запроса проходит через тот же компенсационный путь и затем пробрасывается вызывающему коду.

### Проверено

- Валидный SQLite fail-first: `0/6`; после исправления compensation/cancellation suite `6/6`, смежные lifecycle/admin/expiry `53/53`.
- Backend `1140/1140`; Release build `0` warnings/errors; fresh SQLite order/payment/subscription/access flow и EF drift check зелёные.
- Frontend `136/136`, typecheck/build всех приложений и audit `0 vulnerabilities`; UI не менялся, актуальный полный browser inventory этого прохода остаётся `218/218` (`52+62+98+6`) без failed/flaky/skipped.
- Encoding/documentation/release guards и latest release SQLite verification выполнены после синхронизации seed (`637` entries); secret scan `668` files/`0` findings, временные smoke-артефакты очищены.
- Roadmap: `651/671` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.637.0 - 2026-08-12

Release entry: `2026-08-12-admin-subscription-effective-expiry`.

### Исправлено

- Административные sync и migration подписки теперь отклоняются до provider/job side effects после `gracePeriodEndAt ?? endAt`, даже если сохранённый статус ещё `Active` или `GracePeriod`.
- Dashboard и user overview считают active/expiring subscriptions и VPN accesses по effective state, а не по устаревшему строковому статусу.
- Разблокировка после paid end, но до grace end, возвращает подписку в `GracePeriod` и включает доступ; на точной границе grace end остаётся `Expired` без provider enable.
- API client принимает законный `GracePeriod` unblock response вместо ложной ошибки malformed DTO; dashboard summary повторно запрашивается на subscription deadline.
- Открытая админка планирует ближайший срок подписки и без reload убирает migration, отключает sync и показывает точную причину истечения на desktop/mobile.

### Проверено

- До исправления frontend fail-first был `132/136`, backend compile fail-first подтвердил отсутствие инъецируемого clock boundary, desktop/mobile browser был `0/2`; после исправления targeted backend `35/35`, frontend `136/136`, expiry browser `2/2` и focused admin flow `4/4`.
- Полный browser inventory прошёл `218/218` как эквивалентные изолированные группы `52 public + 62 cabinet + 98 admin + 6 all-screens`, без failed/flaky/skipped; единая команда дважды достигала shell timeout, а один повтор переиспользовал завершавшийся webserver и был исключён как инфраструктурный результат.
- Typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `357.70/104.34 kB`, cabinet bundle `372.73/107.60 kB`, admin bundle `532896/142757/max 233210`.
- Backend `1134/1134`, build `0` warnings/errors, EF drift отсутствует и fresh SQLite flow зелёный; encoding/documentation/release guards и latest release SQLite verification выполнены после синхронизации seed, secret scan `668` files/`0` findings.
- Roadmap: `650/670` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.636.0 - 2026-08-12

Release entry: `2026-08-12-admin-access-grace-expiry`.

### Исправлено

- Административные access DTO и user overview скрывают URI, provider ID, config и QR точно по `gracePeriodEndAt ?? endAt`, даже если lifecycle worker ещё не обновил статус.
- QR, `enable`, `sync` и reset traffic отклоняются до вызова VPN-провайдера после effective expiry; remedial disable остаётся доступным для фактического отключения истёкшего доступа.
- Открытая админка планирует ближайший access deadline, очищает QR-кэш и без reload оставляет только безопасное отключение у провайдера и историю.

### Проверено

- До исправления backend boundary-suite был `29/35`, frontend helper не имел expiry-контракта, desktop/mobile browser был `0/2`; после исправления backend targeted `35/35`, frontend `135/135`, expiry browser `2/2` и полный admin-flow `2/2`.
- Первый полный browser regression выявил конфликт remedial-команды у отменённой подписки: `214/216`; после fail-closed уточнения targeted desktop/mobile прошёл `4/4`, финальный console-responsive Playwright — `216/216` за `13.7 min` без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `357.69/104.34 kB`, cabinet bundle `372.71/107.60 kB`, admin bundle `531616/142394/max 231944`.
- Backend `1132/1132`, build `0` warnings/errors, EF drift отсутствует и fresh SQLite flow с latest release зелёный; encoding/documentation/release guards `57/57`, release seed `635`, secret scan `668` files/`0` findings.
- Roadmap: `649/669` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.635.0 - 2026-08-12

Release entry: `2026-08-12-cabinet-access-grace-expiry`.

### Исправлено

- Кабинет скрывает VPN URI, provider ID, config и QR точно в момент окончания `gracePeriodEndAt ?? endAt`, не ожидая фонового lifecycle worker.
- Обе пользовательские QR-точки и API-проекции используют единое effective-access правило и закрываются fail-closed для истёкшего доступа.
- Открытая вкладка планирует ближайший срок подписки/доступа, очищает QR-кэш и обновляет все повторяющиеся access surfaces без reload.

### Проверено

- До исправления frontend fail-first был `9/10`, backend SQLite exact-boundary `0/2`, desktop/mobile browser `0/2`; после исправления targeted наборы прошли `10/10`, `16/16`, `24/24` и `2/2`.
- Полный cabinet desktop/mobile regression прошел `62/62`; финальный console-responsive Playwright — `214/214` за `11.8 min` без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `134/134`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `357.69/104.34 kB`, cabinet bundle `372.71/107.60 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1127/1127`, build `0` warnings/errors, EF drift отсутствует и fresh SQLite flow с latest release зеленый; encoding/documentation/release guards `57/57`, release seed `634`, secret scan `668` files/`0` findings.
- Roadmap: `648/668` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.634.0 - 2026-08-12

Release entry: `2026-08-12-public-pending-checkout-live-expiry`.

### Исправлено

- Анонимная карточка сохраненного checkout автоматически переходит в истекшее состояние на границе срока session без входа или refresh.
- Partial checkout после ошибки payment init автоматически убирает обе retry-кнопки, когда claimed order истекает в открытой вкладке.
- Единый current-time timer отслеживает session, partial order или последний checkout snapshot и показывает только допустимые действия восстановления.

### Проверено

- До исправления desktop/mobile fail-first был `0/4`: anonymous session сохраняла ложное обещание привязки, partial order оставлял две retry-кнопки. После исправления targeted unit `9/9`, public typecheck и browser regression `4/4` прошли без overflow.
- Полный public desktop/mobile regression прошел `52/52`; финальный console-responsive Playwright — `212/212` за `11.0 min` без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `133/133`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `357.69/104.34 kB`, cabinet bundle `371.43/107.26 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зеленые; encoding/documentation guard `57/57`, release seed `633`, secret scan `668` files/`0` findings.
- Roadmap: `647/667` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.633.0 - 2026-08-12

Release entry: `2026-08-12-public-payment-link-expiry`.

### Исправлено

- Public account больше не оставляет redirect URL после истечения заказа, созданного через checkout-session.
- Доступность ссылки оплаты проверяет retryable order status и `expiresAt`, а открытая вкладка автоматически обновляется на границе срока без ручного refresh.
- Просроченная карточка показывает статус `Expired`, точное объяснение и действие «Создать новый заказ» вместо недействительной ссылки.

### Проверено

- До исправления desktop/mobile fail-first был `0/2`: после fake-clock expiry ссылка оставалась доступна. После исправления targeted unit `6/6`, typecheck public-web и desktop/mobile regression `2/2` прошли без overflow.
- Полный public desktop/mobile regression прошёл `48/48`; финальный console-responsive Playwright — `208/208` за `11.0 min` без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `133/133`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `357.13/104.23 kB`, cabinet bundle `371.43/107.26 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `632`, secret scan `668` files/`0` findings.
- Roadmap: `646/666` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.632.0 - 2026-08-12

Release entry: `2026-08-12-cabinet-payment-link-expiry`.

### Исправлено

- Кабинет больше не показывает provider confirmation URL, когда родительский заказ истёк по локальному времени, даже если payment attempt всё ещё имеет открытый статус.
- Карточки последнего продления и повторной оплаты, история заказов и история платежей используют единый order-aware payment contract.
- Ближайший срок оплаты автоматически обновляет интерфейс без ручной перезагрузки; отсутствующий родительский заказ скрывает payment link fail-closed.

### Проверено

- До исправления unit fail-first был `6/8`, а основной и long-login desktop/mobile browser fail-first — `0/2`: ссылки оставались после истечения или планировались от старого render time. После исправления targeted unit/source suite прошёл `68/68`, renewal/retry/current-login desktop/mobile — `6/6` без refresh и overflow.
- Полный cabinet desktop/mobile regression прошёл `60/60`; all-screens — `6/6` за `8.2 min` на 25 viewport-конфигурациях. Первый обновлённый full gate дал `205/206`: mobile renewal успел корректно истечь внутри слишком узкого двухсекундного fake-clock fixture; после детерминированного 30-second test-window targeted `6/6`, финальный неизменённый console-responsive Playwright — `206/206` за `12.3 min` без failed/flaky/skipped.
- Frontend `132/132`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `356.22/103.94 kB`, cabinet bundle `371.44/107.26 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `631`, secret scan `668` files/`0` findings.
- Roadmap: `645/665` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.631.0 - 2026-08-11

Release entry: `2026-08-11-public-checkout-session-expiry`.

### Исправлено

- Публичный checkout сохраняет `expiresAt` checkout-session и не отправляет claim, если срок оформления уже истёк до входа пользователя.
- Истёкшая session показывает отдельный статус, объяснение и действие «Создать новый заказ» вместо заведомо недопустимой повторной привязки.
- Legacy sessionStorage-записи без `expiresAt` остаются совместимыми: точный backend-ответ `Checkout session expired` переводит их в тот же terminal recovery после одного claim и без payment init.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: истёкшая checkout-session теряла `expiresAt` и продолжала claim/payment flow. После исправления targeted contract прошёл `2/2`: новая schema даёт `claim=0`, legacy fallback — `claim=1`, в обоих случаях `paymentInit=0` и storage очищен.
- Полный public desktop/mobile regression прошёл `46/46`. Первый полный console-responsive запуск дал `199/200` из-за невоспроизведённого нагрузочного timeout существующего mobile-admin VPN lifecycle; isolated rerun прошёл `1/1`, финальный неизменённый полный gate — `200/200` за `12.8 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `130/130`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `356.22/103.94 kB`, cabinet bundle `370.75/107.00 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `630`, secret scan `668` files/`0` findings.
- Roadmap: `644/664` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.630.0 - 2026-08-11

Release entry: `2026-08-11-public-completed-checkout-recovery`.

### Исправлено

- Повторный claim уже завершённого заказа больше не показывает ложное состояние «оплата не подготовлена» и не оставляет public checkout в reload-петле.
- Публичная карточка различает `PaymentReceived`, `FulfillmentInProgress`, `Completed`, `PartiallyProcessed`, `Refunded`, `Cancelled` и `Expired`, показывая точный статус и допустимое следующее действие.
- Неретрайные результаты удаляются из `sessionStorage` до выхода из checkout handler, но остаются видимыми в текущей вкладке до действия «Закрыть».

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: завершённый claimed order показывался как неподготовленная оплата, а persisted checkout оставался для нового claim после reload. После исправления targeted contract прошёл `2/2`, после reload `claim=1`, `paymentInit=0`.
- Полный public desktop/mobile regression прошёл `44/44`; полный console-responsive Playwright — `198/198` за `11.6 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `130/130`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `355.14/103.59 kB`, cabinet bundle `370.75/107.00 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `629`, secret scan `668` files/`0` findings.
- Roadmap: `643/663` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.629.0 - 2026-08-11

Release entry: `2026-08-11-public-expired-partial-checkout`.

### Исправлено

- Публичный checkout больше не пытается повторно инициализировать оплату для уже истёкшего частичного заказа после входа в аккаунт.
- Допустимость повторной оплаты теперь определяется по сохранённым backend-статусу и `expiresAt`: только живые `PendingPayment`/`Failed` заказы отправляются в payment init.
- Для истёкшего заказа показываются однозначный статус и действие «Создать новый заказ»; устаревший partial checkout очищается при переходе к тарифам.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: просроченный claimed order отправлялся в payment init и зацикливал пользователя на недопустимом retry. После исправления targeted contract прошёл `2/2`, payment init не вызывается.
- Полный public desktop/mobile regression прошёл `42/42`; полный console-responsive Playwright — `196/196` за `12.5 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `130/130`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.94/103.23 kB`, cabinet bundle `370.75/107.00 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `628`, secret scan `668` files/`0` findings.
- Roadmap: `642/662` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.628.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-terminal-payment-links`.

### Исправлено

- Кабинет больше не показывает confirmation URL для завершённых, ошибочных, отменённых, возвращённых или неизвестных payment states.
- Ссылки оплаты теперь доступны только для backend-статусов `New`, `Pending` и `WaitingConfirmation`; terminal/unknown состояние закрывается fail-closed.
- Карточки последнего продления и повторной оплаты синхронизируются с актуальным order snapshot, показывают подтверждённый статус после reload и используют тот же link-availability contract, что история платежей.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: после перехода retry order/payment в `Completed/Succeeded` заметная карточка продолжала показывать старую payment URL. После исправления targeted renewal/retry contract прошёл `4/4`.
- Полный cabinet desktop/mobile regression прошёл `54/54`; полный console-responsive Playwright — `194/194` за `13.2 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `129/129`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `370.75/107.00 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `627`, secret scan `668` files/`0` findings.
- Roadmap: `641/661` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.627.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-renewal-order-refresh`.

### Исправлено

- Успешное обновление данных кабинета теперь синхронизирует заметную карточку последнего продления с актуальным заказом из истории по его ID.
- Истёкший, завершённый или отменённый renewal order больше не остаётся визуально в `PendingPayment` и не показывает устаревшую retry-команду после ручного reload.
- Карточка продления использует тот же payment-availability контракт, что и история заказов: terminal/non-retry состояние объясняет причину и при необходимости предлагает создать новый заказ.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: после перехода сохранённого renewal order в `Expired` ручное обновление меняло историю, но карточка продолжала показывать retry. После исправления targeted contract прошёл `2/2`.
- Полный cabinet desktop/mobile regression прошёл `50/50`; полный console-responsive Playwright — `190/190` за `13.1 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `128/128`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `370.04/106.84 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `626`, secret scan `668` files/`0` findings.
- Roadmap: `640/660` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.626.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-renewal-provider-availability`.

### Исправлено

- Кнопка повторной подготовки оплаты продления больше не остаётся активной, если после обновления сессии доступные payment providers не загрузились.
- Заведомо no-op команда теперь блокируется тем же `busy || !provider` условием, что и остальные cabinet payment actions.
- Retry-кнопка передаёт точное `aria-busy` во время операции, а handler по-прежнему fail-closed отклоняет программный вызов без provider.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: после provider `503` сохранённый renewal показывал enabled retry-кнопку. После исправления targeted contract прошёл `2/2`, programmatic bypass не отправил второй payment init.
- Полный cabinet desktop/mobile regression прошёл `48/48`; полный console-responsive Playwright — `188/188` за `11.7 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `128/128`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.62/106.77 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `625`, secret scan `668` files/`0` findings.
- Roadmap: `639/659` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.625.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-payment-provider-lock`.

### Исправлено

- Выбранный в кабинете способ оплаты теперь блокируется на всё время создания продления или повторной оплаты.
- Видимый provider больше нельзя сменить, пока запрос использует ранее зафиксированный provider snapshot.
- Native `disabled` и `aria-busy` селектора следуют общему cabinet mutation lifecycle и снимаются только после завершения команды.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: при delayed retry-payment селектор оставался enabled. После исправления targeted contract прошёл `2/2`, значение оставалось `YooKassa` до ответа и control снова включался после завершения.
- Полный cabinet desktop/mobile regression прошёл `46/46`; первый полный console-responsive запуск дал `185/186` из-за невоспроизведённого нагрузочного timeout существующего mobile-admin VPN test, его isolated rerun прошёл `1/1`, финальный неизменённый full gate — `186/186` за `14.2 min` без failed/flaky/skipped.
- Frontend `128/128`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.60/106.77 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite order/payment/subscription/access flow зелёный; encoding/documentation guard `57/57`, release seed `624`, secret scan `668` files/`0` findings.
- Roadmap: `638/658` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.624.0 - 2026-08-11

Release entry: `2026-08-11-admin-concurrent-busy-resource-owner`.

### Исправлено

- Параллельная независимая admin-команда больше не сбрасывает индикатор и `disabled` незавершённой формы или строки.
- Однослотовый `actionBusyId` удалён; create/edit формы payment provider, VPN panel, server и inbound используют многозначный resource-owner set.
- Referral save и retry отдельных email-уведомлений получили явные resource keys и устойчивый shared busy-state.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: завершившийся provider check делал pending provider-create форму enabled и `aria-busy=false`. После исправления targeted contract прошёл `2/2`, duplicate programmatic submit не отправил второй POST.
- Notification/provider/VPN infrastructure/critical admin regression прошёл `12/12` за `4.2 min`; полный console-responsive Playwright — `184/184` за `13.1 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `128/128`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.59/106.77 kB`, admin bundle `530213/142009/max 230541`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `623`, secret scan `668` files/`0` findings.
- Roadmap: `637/657` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.623.0 - 2026-08-11

Release entry: `2026-08-11-admin-managed-config-resource-owner`.

### Исправлено

- Команды одного тарифа, релиза, FAQ-записи и рабочего сценария больше не выполняются параллельно через разные action IDs.
- Создание, редактирование, удаление и восстановление контента главной используют общий global boundary и не пересекают массовое восстановление defaults.
- Сохранение и проверка настроек Telegram-бота используют единый resource owner и shared busy-state.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: tariff toggle пропускал второй PATCH, home defaults restore — content create, bot settings save — connection test (`2/1/1` вместо `1/0/0`). После исправления targeted contract прошёл `2/2`, включая programmatic обход disabled.
- Managed configuration/Telegram/role regression прошёл `8/8` за `1.7 min`; полный console-responsive Playwright — `182/182` за `11.9 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `127/127`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.59/106.77 kB`, admin bundle `530419/142085/max 230747`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow и latest release verification зелёные; encoding/documentation guard `57/57`, release seed `622`, secret scan `668` files/`0` findings.
- Roadmap: `636/656` closed, readiness `97.0%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.622.0 - 2026-08-11

Release entry: `2026-08-11-admin-finance-resource-owner`.

### Исправлено

- Редактирование, включение/выключение и проверка настроек одного payment-provider account больше не выполняются параллельно через разные action IDs.
- Order-level recheck и direct payment recheck/refund одного платежа используют общий order/payment boundary и не запускают повторный provider-state transition.
- Shared busy-state блокирует sibling provider/payment controls, refund fields и edit submit; независимые аккаунты и заказы остаются доступными.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: при задержанном provider enabled mutation уходил provider check, а при задержанном order recheck уходил direct payment recheck (`1/1` вместо `0/0`). После исправления targeted contract прошёл `2/2`, включая programmatic обход disabled.
- Finance/provider/refund/role regression прошёл `12/12` за `54.9 s`, весь admin desktop/mobile — `90/90` за `8.4 min`; полный console-responsive Playwright — `180/180` за `10.9 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `126/126`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.59/106.77 kB`, admin bundle `530248/141999/max 230576`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `57/57`, release seed `621`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `635/655` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.621.0 - 2026-08-11

Release entry: `2026-08-11-admin-vpn-infrastructure-resource-owner`.

### Исправлено

- Команды одной 3x-ui панели, её inbound-правил и клиентов больше не выполняются параллельно через разные action IDs; panel mutation владеет общим parent resource до завершения reload/details.
- Команды одного VPN-клиента используют client/inbound/panel resource keys, а миграция атомарно резервирует source и target панели/inbound-правила.
- Lifecycle/health/precheck/provision/delete одного VPN-сервера и deploy/cancel/retry/support связанного provisioning run используют общий server boundary между разделами.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: при задержанном panel sync одновременно уходили panel test, inbound set-default и client sync (`1/1/1` вместо `0/0/0`). После исправления targeted contract прошёл `2/2`, включая programmatic обход disabled для client/run/server команд.
- Расширенный VPN infrastructure/provisioning/client/migration regression прошёл `10/10` за `3.2 min`, весь admin desktop/mobile — `88/88` за `9.4 min`; полный console-responsive Playwright — `178/178` за `11.3 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Shared busy-state блокирует sibling controls и формы редактирования между panel/inbound/client и server/provisioning разделами без изменения размеров/layout.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.59/106.77 kB`, admin bundle `529885/141838/max 230213`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `57/57`, release seed `620`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `634/654` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.620.0 - 2026-08-11

Release entry: `2026-08-11-admin-subscription-access-resource-owner`.

### Исправлено

- Lifecycle-команды одной подписки и связанного VPN-доступа больше не выполняются параллельно через разные action IDs.
- Admin `runAction` поддерживает несколько identity-safe resource keys; операция подписки владеет `subscription:<id>` и связанным `access:<id>`, а прямые VPN-команды используют тот же access boundary.
- Кэшированный QR очищается перед любой мутацией подписки или VPN-доступа, поэтому после изменения provider state интерфейс не показывает потенциально устаревшую ссылку.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: при задержанном продлении подписки sibling sync подписки уходил в API, а связанный VPN-доступ оставался вызываемым. После исправления targeted contract прошёл `2/2`; принудительное снятие `disabled` также не обошло synchronous owner.
- Subscription/VPN/support regression прошёл `8/8`, весь admin desktop/mobile — `86/86` за `7.8 min`; полный console-responsive Playwright — `176/176` за `12.3 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Общий busy-state работает между разделами подписок и VPN-доступов, освобождается только владельцем request identity и не меняет layout или размеры controls.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.59/106.77 kB`, admin bundle `529142/141520/max 229470`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `619`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `633/653` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не объявлялся production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.619.0 - 2026-08-11

Release entry: `2026-08-11-admin-support-mutation-resource-owner`.

### Исправлено

- Разные mutation-команды одного обращения поддержки больше не отправляются параллельно с одной и той же optimistic `revision`.
- Общий admin `runAction` получил identity-safe resource owner; status, reply и internal note одного обращения используют единый busy/ownership boundary, сохраняя независимость разных ресурсов.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: во время задержанного status PATCH админка отправляла reply POST вместо ожидаемых `0` дополнительных mutations. После исправления targeted contract прошёл `2/2`; черновики сохранились, а note/reply последовательно ушли с revisions `1` и `2`.
- Расширенный support/managed regression прошёл `24/24` за `4.9 min`, весь admin desktop/mobile — `84/84` за `7.4 min`; полный console-responsive Playwright — `174/174` за `10.6 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Общий busy-state блокирует status/reply/note одного обращения без изменения layout; focus, navigation, role boundaries, support lifecycle и responsive controls остались зелёными.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.59/106.77 kB`, admin bundle `528640/141319/max 228968`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `618`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `632/652` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.618.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-app-version-seen-single-flight`.

### Исправлено

- Два синхронных события закрытия cabinet «Что нового» больше не отправляют два `mark-seen` POST до React unmount.
- Session/release-scoped Promise owner возвращает duplicate caller текущий результат, сбрасывается на token/user boundary и разрешает новую server-sync попытку после transient failure.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: двойное закрытие отправляло `2` POST вместо `1`. После исправления targeted contract прошёл `2/2`, включая silent `503` и одну последующую retry-попытку.
- Полный app-version regression прошёл `12/12`, весь cabinet desktop/mobile — `44/44` за `58.6 s`; полный console-responsive Playwright — `172/172` за `12.1 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Разметка не менялась; modal loading/error/empty/current/history, focus/inert/scroll isolation, opener restore и logout/login lifecycle остались зелёными без overflow или clipped controls.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.59/106.77 kB`, admin bundle `528406/141215/max 228734`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `617`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `631/651` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.617.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-app-version-latest-single-flight`.

### Исправлено

- Раннее ручное открытие «Что нового» больше не запускает второй latest GET одновременно с session-загрузкой после завершения profile hydration.
- Auto-load, manual open и retry используют единый token/user/session-scoped loader с synchronous in-flight lock, mounted guard и request generation.

### Проверено

- До исправления fail-first desktop/mobile был `0/4`: раннее открытие давало `2` latest GET вместо `1`, а двойной retry увеличивал счётчик с `2` до `4` вместо `3`. После исправления targeted contract прошёл `4/4`.
- Полный app-version regression прошёл `12/12`, весь cabinet desktop/mobile — `44/44` за `1.0 min`; полный console-responsive Playwright — `172/172` без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Разметка модального окна не менялась; desktop/mobile regression подтвердил loading/error/empty/current/history, focus/inert/scroll isolation и logout/login lifecycle без overflow или clipped controls.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.27/106.67 kB`, admin bundle `528406/141215/max 228734`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `616`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `630/650` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.616.0 - 2026-08-11

Release entry: `2026-08-11-public-catalog-load-single-flight`.

### Исправлено

- Public `/tariffs` больше не отправляет по два initial GET тарифов и способов оплаты при React StrictMode effect replay.
- Обе области получили component-scoped initial claims, in-flight locks, mounted/request-generation guards и duplicate-safe retry.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: initial counters составляли `tariffs=2`, `paymentProviders=2` вместо `1/1`. После исправления targeted initial/retry contract прошёл `4/4`.
- Финальный public desktop/mobile regression прошёл `40/40` за `30.9 s`; полный console-responsive Playwright — `172/172` за `10.6 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Визуальная разметка не менялась; responsive gate подтвердил loading/error/empty/ready/checkout states без blank state, overflow, overlap или clipped controls.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `352.02/102.86 kB`, cabinet bundle `369.08/106.56 kB`, admin bundle `528406/141215/max 228734`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `615`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `629/649` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.615.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-recovery-load-single-flight`.

### Исправлено

- Синхронный двойной retry больше не отправляет два дополнительных запроса способов оплаты или сообщений выбранной переписки до React loading-rerender.
- Обе recovery-загрузки получили session/token/scope-scoped Promise owners с очисткой по identity, смене переписки и session boundary.

### Проверено

- До исправления fail-first desktop/mobile был `0/4`: обе recovery-точки получали `3` запроса вместо `2`, включая исходную неудачную попытку. После исправления targeted contract прошёл `4/4`.
- Финальный cabinet desktop/mobile regression прошёл `44/44` за `53.9 s`; полный console-responsive Playwright — `172/172` за `9.2 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Визуальная разметка recovery-панелей не менялась; responsive gate подтвердил отсутствие blank state, overflow, overlap или clipped controls.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `351.49/102.80 kB`, cabinet bundle `369.08/106.56 kB`, admin bundle `528406/141215/max 228734`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `614`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `628/648` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.614.0 - 2026-08-11

Release entry: `2026-08-11-admin-auth-command-single-flight`.

### Исправлено

- Синхронная повторная activation больше не отправляет два login, два refresh с одним rotation token или два backend logout до React busy-rerender.
- Login, manual refresh и logout получили отдельные operation-scoped Promise owners, snapshots формы/токенов и session-boundary cleanup.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: `login=2`, `refresh=2`, `logout=2` вместо `1/1/1`. После исправления targeted auth single-flight contract прошел `2/2`.
- Финальный admin desktop/mobile regression прошел `82/82` за `6.6 min`; полный console-responsive Playwright — `172/172` за `9.2 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Визуальная разметка не менялась; responsive gate подтвердил login/loading/dashboard transitions без blank state, overflow, overlap или clipped controls.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `351.49/102.80 kB`, cabinet bundle `368.29/106.39 kB`, admin bundle `528406/141215/max 228734`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зеленый; encoding guard `14/14`, release seed `613`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `627/647` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.613.0 - 2026-08-11

Release entry: `2026-08-11-admin-detail-retry-single-flight`.

### Исправлено

- Синхронный двойной retry больше не повторяет загрузку карточки пользователя, сообщений поддержки или пятизапросный набор деталей VPN-панели до React busy-rerender.
- User overview, support messages и VPN panel details получили operation/token/entity-scoped Promise owners с очисткой по request identity, смене выбора и session boundary.

### Проверено

- До исправления fail-first desktop/mobile был `0/6`: каждая контрольная detail-точка получала `3` вызова вместо `2`. После исправления targeted single-flight/recovery contract прошел `6/6`.
- Финальный admin desktop/mobile regression прошел `80/80` за `6.6 min`; полный console-responsive Playwright — `170/170` за `9.2 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Визуальная структура локальных recovery-панелей не менялась; responsive gate подтвердил отсутствие overflow/overlap и корректное отображение всех admin sections.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `351.49/102.80 kB`, cabinet bundle `368.29/106.39 kB`, admin bundle `527630/140994/max 227958`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зеленый; encoding guard `14/14`, release seed `612`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `626/646` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.612.0 - 2026-08-11

Release entry: `2026-08-11-admin-user-filter-load-recovery`.

### Исправлено

- Синхронная повторная отправка одинаковых фильтров пользователей больше не запускает второй GET до React busy-rerender.
- Ошибка фильтрованной загрузки больше не оставляет прежний список под новыми условиями и не создает необработанный `ApiClientError`; секция показывает локальную ошибку и явный retry.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: счетчик users GET достигал `3` вместо `2`, а HTTP 503 попадал в `pageerror`. После исправления targeted single-flight/recovery contract прошел `2/2`.
- Финальный admin desktop/mobile regression прошел `80/80`; полный console-responsive Playwright — `170/170` за `10.0 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Recovery UI просмотрен при 1280x1389 px: фильтры, локальная ошибка и retry читаемы без stale списка, horizontal overflow, overlap или смещения соседней карточки.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `351.49/102.80 kB`, cabinet bundle `368.29/106.39 kB`, admin bundle `526652/140842/max 226980`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зеленый; encoding guard `14/14`, release seed `611`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `625/645` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.611.0 - 2026-08-11

Release entry: `2026-08-11-admin-data-reload-single-flight`.

### Исправлено

- Синхронный двойной activation кнопки «Обновить данные» больше не запускает два полных набора admin GET до React busy-rerender.
- `loadAll` получил operation/token/session-scoped request owner: повторный вызов получает уже выполняющийся Promise, а новая admin session operation не блокируется старым запросом.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: после initial load каждый из десяти контрольных endpoint имел `3` запроса вместо `2`. После исправления targeted single-flight contract прошёл `2/2`.
- Финальный admin desktop/mobile regression прошёл `78/78`; полный console-responsive Playwright — `168/168` за `10.2 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Admin login UI просмотрен при 1280x720 px: экран читаем без blank state, horizontal overflow или clipped controls; authenticated mobile/desktop разделы покрыты Playwright.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `351.49/102.80 kB`, cabinet bundle `368.29/106.39 kB`, admin bundle `525576/140576/max 225904`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `610`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `624/644` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.610.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-data-reload-single-flight`.

### Исправлено

- Синхронный двойной activation кнопки «Обновить данные» больше не запускает два полных набора из восьми приватных cabinet GET до React busy-rerender.
- `loadAll` получил session/token-scoped request owner: повторный вызов получает уже выполняющийся Promise, а новая session operation после logout/login не блокируется старым запросом.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: после initial load каждый из восьми endpoint имел `3` запроса вместо `2`. После исправления targeted single-flight contract прошёл `2/2`.
- Финальный cabinet desktop/mobile regression прошёл `44/44`; полный console-responsive Playwright — `166/166` за `9.5 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Cabinet login/help/reset UI просмотрен при 1280x720 px: верх и низ страницы, sticky navigation и формы читаемы без blank screen, horizontal overflow или clipped controls; authenticated mobile/desktop dashboard покрыт Playwright.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `351.49/102.80 kB`, cabinet bundle `368.29/106.39 kB`, admin bundle `525267/140453/max 225595`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `609`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `623/643` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.609.0 - 2026-08-11

Release entry: `2026-08-11-public-managed-content-load-lifecycle`.

### Исправлено

- Страницы тарифов и аккаунта больше не отправляют по два одинаковых managed CMS GET при React StrictMode effect replay.
- Общий `useManagedHomeContent` владеет одной initial попыткой на mounted route и отклоняет completion после ухода со страницы; встроенный fallback-контент сохранён.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: уже `/tariffs` получал `2` initial CMS GET вместо `1`. После исправления targeted lifecycle contract прошёл `2/2` и подтвердил по одной попытке на `/tariffs` и `/account`.
- Финальный public desktop/mobile regression прошёл `40/40`; полный console-responsive Playwright — `164/164` за `9.7 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Tariffs/account просмотрены в браузере при 1280x720 px: обе части account-форм, retry-состояния тарифов и sticky navigation читаемы без blank screen, horizontal overflow или нецелевого overlap; mobile coverage подтверждён отдельным Playwright-проектом.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `351.49/102.80 kB`, cabinet bundle `368.01/106.29 kB`, admin bundle `525267/140453/max 225595`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `608`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `622/642` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.608.0 - 2026-08-11

Release entry: `2026-08-11-public-landing-load-lifecycle`.

### Исправлено

- Главная страница больше не отправляет по два initial GET-запроса FAQ-preview и managed CMS content при React StrictMode effect replay.
- Сбой FAQ-preview больше не маскируется успешным empty-state «FAQ скоро появится»: loading, error, empty и ready состояния взаимоисключены.
- Landing FAQ получил component ownership, in-flight/request generation guards и явный retry; managed CMS сохраняет безопасный встроенный fallback и отклоняет completion после ухода со страницы.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: оба проекта получали `faq=2/content=2` вместо одной initial попытки каждого endpoint. После исправления targeted boundary прошёл `2/2`.
- Финальный public desktop/mobile regression прошёл `38/38`; полный console-responsive Playwright — `162/162` за `9.6 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Landing recovery UI просмотрен на 1440x900 и 305x700 px: карточка занимает всю desktop-сетку, mobile alert/retry читаемы без false empty, overlap или horizontal overflow; временная browser-сессия закрыта.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `351.50/102.76 kB`, cabinet bundle `368.01/106.29 kB`, admin bundle `525267/140453/max 225595`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зеленый; encoding guard `14/14`, release seed `607`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `621/641` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.607.0 - 2026-08-11

Release entry: `2026-08-11-public-faq-load-lifecycle`.

### Исправлено

- Публичный FAQ больше не отправляет два initial GET-запроса при React StrictMode effect replay.
- Загрузка FAQ получила component ownership, single-flight claim и request generation guard: callback после ухода со страницы или устаревшей попытки не меняет UI.
- Ошибка FAQ остается отдельным recovery-state без ложного «FAQ пока пуст», не запускает фоновый request loop и восстанавливается ровно одним явным повтором.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: каждый initial render отправлял `2` FAQ-запроса вместо `1`. После исправления targeted boundary прошёл `2/2`.
- Финальный public desktop/mobile regression прошёл `36/36`; полный console-responsive Playwright — `160/160` за `10.6 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Recovery UI просмотрен на 1440x900 и 305x700 px: alert, retry, поиск и категория помещаются без false empty, overlap или horizontal overflow; временная browser-сессия закрыта.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `350.62/102.59 kB`, cabinet bundle `368.01/106.29 kB`, admin bundle `525267/140453/max 225595`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зеленый; encoding guard `14/14`, release seed `606`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `620/640` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.606.0 - 2026-08-11

Release entry: `2026-08-11-public-catalog-load-recovery`.

### Исправлено

- Публичная страница тарифов больше не объединяет ошибки каталога, способов оплаты и checkout mutation в один race-dependent state.
- Transient или malformed failure тарифов и payment providers остаётся в своей области, не создаёт ложный empty-state и не скрывает успешно загруженную соседнюю область.
- Обе загрузки получили явный локальный retry без reload; кнопки покупки при provider failure выключены с точной причиной, а CMS-тексты ошибок продолжают обновлять уже показанный recovery-state.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: provider `503` показывал одновременно ошибку и ложное «Нет доступных способов оплаты», а retry отсутствовал. После исправления targeted boundary прошёл `2/2`.
- Финальный public desktop/mobile regression прошёл `34/34`; полный console-responsive Playwright — `158/158` за `8.9 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Provider/tariff recovery UI просмотрен на 1280/393 px: здоровая область остаётся видимой, error/empty взаимоисключены, сообщения и full-width mobile retry помещаются без overlap или horizontal overflow; временные screenshots удалены.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `350.31/102.50 kB`, cabinet bundle `368.01/106.29 kB`, admin bundle `525267/140453/max 225595`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `605`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `619/639` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.605.0 - 2026-08-11

Release entry: `2026-08-11-public-checkout-session-boundary`.

### Исправлено

- Успешный результат публичного checkout больше не переживает завершение сессии в React state и не появляется после следующего входа в той же вкладке.
- Общая очистка public session удаляет прежние ID заказа, ID платежа, redirect URL и checkout-ошибку, которая могла содержать идентификатор уже созданного заказа.
- Сохранённый до авторизации выбор тарифа не изменён: исправление ограничено чувствительными данными подтверждённой покупки и всеми путями logout/session rejection/password reset.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: после logout и нового login карточка «Последняя покупка» со старой платежной ссылкой возвращалась. После исправления targeted boundary прошёл `2/2`.
- Финальный public desktop/mobile regression прошёл `32/32`; полный console-responsive Playwright — `156/156` за `8.8 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Account UI после повторного входа просмотрен на 1280/393 px: отображаются только профиль и пустой purchase summary новой сессии, без старых ID, ссылки, overlap или horizontal overflow; временные screenshots удалены.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; public bundle `349.33/102.34 kB`, cabinet bundle `368.01/106.29 kB`, admin bundle `525267/140453/max 225595`.
- Backend `1125/1125`, build `0` warnings/errors, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `604`, secret scan `668` files/`0` findings, временные browser/backend-артефакты удалены.
- Roadmap: `618/638` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.604.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-section-load-boundary`.

### Исправлено

- Cabinet больше не использует all-or-nothing `Promise.all` для восьми независимых пользовательских областей: transient или malformed ответ необязательного endpoint не блокирует вход при успешно подтверждённом профиле.
- Профиль, подписки, заказы, платежи, VPN-доступы, реферальные начисления, обращения поддержки и Telegram получили отдельные load-error boundaries; fallback-значения не показываются как фактические нули, empty states или доступные действия.
- `401/403` по-прежнему завершают сессию, profile failure сохраняет прежний recovery/refresh flow, stale selection обращений сбрасывается, а support остаётся доступен без неподтверждённой привязки к заказу или подписке.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: сбой subscriptions/orders/Telegram полностью блокировал кабинет. После исправления targeted boundary прошёл `2/2`.
- Финальный cabinet desktop/mobile regression прошёл `42/42`; полный console-responsive Playwright — `154/154` за `8.8 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Partial-error UI просмотрен на 1280/393 px: здоровые платежи, ключи, поддержка и начисления остаются доступны без overflow/overlap, ошибочные данные и действия скрыты; временные screenshots удалены.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; cabinet bundle `368.01/106.29 kB`, admin bundle `525267/140453/max 225595`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `603`, secret scan `668` files/`0` findings, временные browser-артефакты удалены.
- Roadmap: `617/637` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.603.0 - 2026-08-11

Release entry: `2026-08-11-admin-section-load-boundary`.

### Исправлено

- Частичный сбой общей загрузки admin-panel больше не превращает fallback-массивы в ложные нулевые метрики, empty states или доступные формы внутри затронутого раздела.
- Все разделы связаны с их API-областями: активный ошибочный раздел заменяется локальной recovery-панелью с диагностикой и явным повтором, а незатронутые разделы остаются доступны.
- Tab/tabpanel ARIA-связь сохраняется и для recovery-панели; в здоровом разделе вместо сырых деталей показывается краткое уведомление о количестве частичных ошибок.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: dashboard, users и payments показывали неподтверждённые данные после ответов `500`. После исправления targeted boundary прошёл `2/2`.
- Финальный admin desktop/mobile regression прошёл `76/76` за `6.3 min`; полный console-responsive Playwright — `152/152` за `8.7 min`, без failed/flaky/skipped, all-screens `6/6` на 25 viewport-конфигурациях.
- Recovery UI просмотрен на 1280/393 px без overflow, overlap и ложных данных; временные screenshots удалены.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; admin bundle `525267/140453/max 225595`, cabinet bundle `364.41/105.46 kB`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `602`, secret scan `668` files/`0` findings, временные browser-артефакты удалены.
- Roadmap: `616/636` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.602.0 - 2026-08-11

Release entry: `2026-08-11-admin-initial-data-boundary`.

### Исправлено

- После успешной проверки административной роли admin-panel больше не показывает нулевые operational метрики и ложные empty states, пока первая общая загрузка ещё ждёт API.
- До подтверждения первой выборки отображается отдельный focused loading-state; dashboard, навигация, очереди и формы монтируются только после получения данных, а последующие refresh сохраняют уже подтверждённый интерфейс.
- Переход фокуса теперь охватывает обе фазы: loading-контейнер получает фокус после login, а готовый `#admin-content` — после завершения загрузки; переходный экран не создаёт перекрывающий mobile skip-link.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: при задержанном dashboard response отображалась ложная метрика «Всего пользователей». После исправления targeted boundary прошёл `2/2`.
- Промежуточный полный admin run выявил focus regression `70/74`; после двухфазного focus transition targeted boundary/RBAC прошёл `6/6`, финальный admin desktop/mobile regression — `74/74` за `6.2 min`.
- Полный console-responsive Playwright прошёл `150/150` за `8.7 min`, без failed/flaky/skipped; all-screens `6/6` на 25 viewport-конфигурациях. Loading UI просмотрен на 1280/393 px без overflow, overlap и ложных operational данных.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; admin bundle `523325/140074/max 223653`, cabinet bundle `364.41/105.46 kB`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `601`, secret scan `668` files/`0` findings, временные browser-артефакты удалены.
- Roadmap: `615/635` closed, readiness `96.9%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.601.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-auth-data-boundary`.

### Исправлено

- Неавторизованный cabinet больше не показывает нулевые приватные метрики и длинный набор empty states о подписках, VPN-ключах, заказах, платежах, доступах и реферальных начислениях до идентификации пользователя.
- Экран входа теперь содержит только доступные до авторизации help/auth/password-reset поверхности; приватный dashboard монтируется после успешного login и подтверждённой загрузки профиля.
- Auth boundary проверяет обе стороны перехода: без token запрос `/api/me` не отправляется, после login появляются реальный профиль, метрики и подписка без reload.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: unauthenticated экран показывал «Активных подписок» как фактическую нулевую метрику. После исправления targeted auth transition прошёл `2/2`.
- Полный cabinet desktop/mobile regression прошёл `40/40`; login/register, refresh/logout, payment/renewal, QR, Telegram, support races и app-version lifecycle остались зелёными.
- Полный console-responsive Playwright прошёл `148/148` за `8.6 min`, без failed/flaky/skipped; all-screens `6/6` на 25 viewport-конфигурациях. Auth UI просмотрен на 1280/393 px без overflow и overlap.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; cabinet bundle `364.41/105.46 kB`, admin bundle budget `521075/139799/max 221403`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `600`, secret scan `668` files/`0` findings, временные browser-артефакты удалены.
- Roadmap: `614/634` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.600.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-restored-data-boundary`.

### Исправлено

- При transient failure первичной загрузки сохранённой cabinet-сессии интерфейс больше не показывает нулевые метрики и ложные empty states о подписках, VPN-ключах, заказах, платежах, доступах и реферальных начислениях.
- Recovery-карточка, русский alert, сохранённые access/refresh tokens и явный retry остаются доступны; приватные dashboard-поверхности появляются только после подтверждённой загрузки профиля и фактических данных.
- Существующие session/request generation guards и refresh policy не менялись: transient failure не завершает сессию и не вызывает refresh token, успешный retry восстанавливает исходный кабинет.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: при recovery одновременно отображалась ложная метрика «Активных подписок». После исправления targeted recovery прошёл `2/2`, токены сохраняются, retry возвращает профиль без refresh-запроса.
- Полный cabinet desktop/mobile regression прошёл `38/38`; login/refresh/logout, payment/renewal, QR, Telegram, support races и app-version lifecycle остались зелёными.
- Полный console-responsive Playwright прошёл `146/146` за `8.8 min`, без failed/flaky/skipped; all-screens `6/6` на 25 viewport-конфигурациях. Recovery UI просмотрен на 1280/393 px без overflow и overlap.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; cabinet bundle `364.41/105.46 kB`, admin bundle budget `521075/139799/max 221403`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `599`, secret scan `668` files/`0` findings, временные browser-артефакты удалены.
- Roadmap: `613/633` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.599.0 - 2026-08-11

Release entry: `2026-08-11-admin-detail-recovery`.

### Исправлено

- Ошибки загрузки карточки пользователя и деталей VPN-панели в admin-panel больше не уходят в общий banner и не маскируются ложными состояниями «Выберите пользователя» или «Клиентов нет».
- Обе detail-карточки получили собственные доступные alert-состояния и явные кнопки повторной загрузки без logout, reload или смены выбранной сущности.
- Детали VPN-панели показывают отдельное loading-состояние и скрывают формы inbound/клиентов до завершения актуального запроса; session/request generation guards отклоняют stale completion.

### Проверено

- До исправления fail-first desktop/mobile был `0/4`; после исправления targeted recovery прошёл `4/4`, включая единственную settled-попытку, retry, восстановление данных и loading-state VPN-панели.
- Полный admin desktop/mobile regression прошёл `72/72`; существующие support, CRUD, role/capability и VPN-операции остались зелёными.
- Полный console-responsive Playwright прошёл `146/146` за `8.6 min`, без failed/flaky/skipped; all-screens `6/6` на 25 viewport-конфигурациях. Error-карточки просмотрены на 1280/393 px без overflow и overlap.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; admin bundle budget `521075/139799/max 221403`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `598`, secret scan `668` files/`0` findings, временные browser-артефакты удалены.
- Roadmap: `612/632` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.598.0 - 2026-08-11

Release entry: `2026-08-11-admin-support-messages-retry`.

### Исправлено

- Transient error загрузки сообщений выбранного обращения в admin-panel больше не уходит в общий banner и не маскируется ложным состоянием «Сообщений нет».
- В карточке «Диалог поддержки» появился один доступный alert с явной кнопкой «Повторить загрузку сообщений» без logout, reload или повторного выбора обращения.
- Retry сохраняет текущую переписку и использует session/request generation guards, поэтому stale response другой переписки или сессии не заменяет актуальные сообщения.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: локальный alert и retry отсутствовали. После исправления targeted desktop/mobile прошёл `2/2`: одна failed attempt остаётся settled, второй запрос восстанавливает исходное сообщение.
- Полный admin desktop/mobile regression прошёл `68/68`; support lifecycle/race/logout, CRUD, role/capability и остальные admin операции остались зелёными.
- Полный console-responsive Playwright прошёл `142/142` за `8.5 min`, all-screens `6/6` на 25 viewport-конфигурациях; карточка отдельно просмотрена на 1280/393 px без overflow и overlap.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; admin bundle budget `519898/139555/max 220226`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `597`, secret scan `668` files/`0` findings, временные browser-артефакты удалены.
- Roadmap: `611/631` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.597.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-support-messages-retry`.

### Исправлено

- Ошибка загрузки сообщений выбранного обращения больше не уходит в общий banner и не маскируется ложным состоянием «Сообщений нет».
- В области переписки появился один доступный русский alert с явной кнопкой «Повторить загрузку переписки»; восстановление не требует logout или reload.
- Retry сохраняет выбранное обращение и использует существующие session/request generation guards, поэтому устаревший ответ другой переписки или сессии не может заменить актуальные сообщения.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: scoped alert и retry отсутствовали, одновременно показывалось ложное пустое состояние. После исправления targeted desktop/mobile прошёл `2/2`: одна failed attempt остаётся settled, второй запрос восстанавливает исходные сообщения.
- Полный cabinet desktop/mobile regression прошёл `38/38`; support race/logout/draft/status и остальные cabinet операции остались зелёными.
- Полный console-responsive Playwright прошёл `140/140` за `8.5 min`, all-screens `6/6` на 25 viewport-конфигурациях; error/retry область отдельно просмотрена на 1280/393 px без overflow и overlap.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; cabinet bundle `364.36 kB`, gzip `105.45 kB`, admin bundle budget `519433/139444/max 219849`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный; encoding guard `14/14`, release seed `596`, secret scan `668` files/`0` findings, временные browser-артефакты удалены.
- Roadmap: `610/630` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.596.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-payment-provider-retry`.

### Исправлено

- Cabinet больше не отправляет два автоматических запроса payment providers при React StrictMode effect replay: initial attempt дедуплицирована по текущему token.
- Ошибка загрузки способов оплаты теперь имеет один русский alert и явную кнопку «Повторить загрузку способов оплаты»; ложное сообщение об отсутствии включённых провайдеров при network failure скрыто.
- Retry использует request generation guard, восстанавливает select без logout/reload и не принимает stale completion старой попытки или сессии.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: ожидался один provider-запрос, фактически отправлялось два, recovery control отсутствовал. После исправления targeted desktop/mobile `2/2`: failure остаётся на одной попытке, ручной retry восстанавливает `YooKassa` вторым запросом.
- Полный cabinet desktop/mobile regression прошёл `36/36`; token rotation/logout, renewal/retry payment и остальные cabinet операции остались зелёными.
- Полный console-responsive Playwright прошёл `138/138` за `8.5 min`, all-screens `6/6` на 25 viewport-конфигурациях; карточка error/retry отдельно просмотрена на 1280/393 px без overflow и overlap.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный.
- Roadmap: `609/629` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.595.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-app-version-latest-recovery`.

### Исправлено

- Ручное открытие «Что нового» больше не исчезает при ошибке или пустом ответе latest endpoint: modal показывает loading, русский error/empty state и явный retry.
- History endpoint не вызывается, пока текущий релиз не загружен, поэтому скрытый запрос не стартует за невидимым modal.
- Latest retry ограничен текущими token/user и request generation; старый ответ не может заменить результат новой попытки или другой сессии.

### Проверено

- До исправления fail-first desktop/mobile был `0/2`: после клика dialog отсутствовал до timeout. После исправления targeted desktop/mobile `2/2`, failure и empty result не повторяются автоматически, recovery выполняется только явным retry.
- Полный cabinet desktop/mobile regression прошёл `34/34`; error/empty/recovery, отсутствие раннего history-запроса и horizontal overflow проверены в обоих viewport.
- Полный console-responsive Playwright прошёл `136/136` за `8.6 min`, all-screens `6/6` на 25 viewport-конфигурациях; отдельные viewport screenshots 1280/393 px просмотрены, повтор ошибки в header удалён.
- Frontend `125/125`, typecheck/build всех приложений и audit `0 vulnerabilities`; backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный.
- Roadmap: `608/628` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.594.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-app-version-history-retry`.

### Исправлено

- Cabinet history effect больше не повторяет запрос бесконечно после ошибки или успешного пустого ответа: session-scoped `historyAttempted` завершает одну попытку.
- Ошибка истории теперь отображается русским alert-состоянием с явной кнопкой «Повторить загрузку истории»; retry запускается только по действию пользователя.
- Error/retry блок стабильно помещается в desktop sidebar и mobile history drawer, длинная подпись кнопки переносится без overflow.

### Проверено

- До исправления fail-first desktop regression отправил `46` history-запросов за `300 ms` вместо одного; после исправления targeted desktop/mobile `2/2`, failure остаётся на одном запросе, empty-success — на втором после ручного retry.
- Полный cabinet desktop/mobile regression прошёл `32/32`; alert, retry, fallback текущего release и отсутствие horizontal overflow проверены в обоих viewport.
- Полный console-responsive Playwright прошёл `134/134` за `8.7 min`, all-screens `6/6` на 25 viewport-конфигурациях; frontend `125/125`, typecheck/build, audit `0 vulnerabilities`; backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный.
- Roadmap: `607/627` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.593.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-app-version-manual-intent`.

### Исправлено

- Ручное нажатие cabinet «Что нового» во время token-only hydration больше не теряется: open intent ждёт `profile.id` и выполняется после подтверждения пользователя.
- При полном logout signal по-прежнему завершается и не переносится в будущую сессию; ожидание действует только внутри текущего token lifecycle.
- Source guard закрепляет раздельную обработку отсутствующего token и временно отсутствующего user ID.

### Проверено

- До исправления fail-first desktop regression был `0/1`: после delayed profile latest release загрузился, но dialog не появился, потому что signal уже был сброшен.
- После исправления targeted desktop/mobile `2/2`, полный cabinet desktop/mobile regression `30/30`; до identity нет latest request/modal, после identity ручное окно открывается.
- Полный console-responsive Playwright прошёл `132/132` за `9.0 min`, all-screens `6/6` на 25 viewport-конфигурациях; frontend `125/125`, typecheck/build, audit `0 vulnerabilities`; backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный.
- Roadmap: `606/626` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.592.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-app-version-identity-boundary`.

### Исправлено

- Cabinet «Что нового» больше не загружает latest release до подтверждения `profile.id`: ранний token-only ответ не может открыть модальное окно под anonymous identity.
- Token/user transition всегда закрывает старое окно и очищает release state; персональный local dismissal применяется только к точному user ID.
- Anonymous dismissal key удалён, поэтому закрытие или показ release не смешивается между hydration-состоянием и реальным пользователем.

### Проверено

- До исправления fail-first desktop regression был `0/1`: ранний unseen response открывал окно, а после загрузки profile персональный dismissal не закрывал его (`expected 0`, `actual 1`).
- После исправления targeted desktop/mobile `2/2`, полный cabinet desktop/mobile regression `28/28`; до identity latest requests и modal отсутствуют, после identity dismissal применяется к текущему пользователю.
- Полный console-responsive Playwright прошёл `130/130` за `8.8 min`, all-screens `6/6` на 25 viewport-конфигурациях; frontend `125/125`, typecheck/build, audit `0 vulnerabilities`; backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный.
- Roadmap: `605/625` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.591.0 - 2026-08-11

Release entry: `2026-08-11-cabinet-app-version-session-boundary`.

### Исправлено

- Cabinet «Что нового» привязывает latest/history requests к текущим token/user generation: ответы старой сессии больше не могут заполнить модальное окно после logout/login.
- Logout и смена пользователя сбрасывают history, выбранный release и loading state; новая сессия немедленно запускает собственную загрузку истории вместо ожидания старого Promise.
- Manual refresh latest version и history completion получили отдельные request generations, поэтому более ранний ответ не перезаписывает более новый.

### Проверено

- До исправления fail-first desktop regression был `0/1`: после logout/login новая сессия не отправляла history request (`expected 2`, `actual 1`) из-за stale `loadingHistory`.
- После исправления targeted desktop/mobile `2/2`, полный cabinet desktop/mobile regression `26/26`; source guard подтверждает session/latest/history generations и отсутствие unguarded state completion.
- Полный console-responsive Playwright прошёл `128/128` за `9.6 min`, all-screens `6/6` на 25 viewport-конфигурациях; frontend `125/125`, typecheck/build, audit `0 vulnerabilities`; backend `1125/1125`, EF drift отсутствует, fresh SQLite flow зелёный.
- Roadmap: `604/624` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.590.0 - 2026-08-11

Release entry: `2026-08-11-public-managed-error-content`.

### Исправлено

- Public tariffs теперь хранит initial load failure как managed content key и повторно разрешает текст после загрузки CMS-контента; настроенные администратором `home.errors.tariffsLoad` и `home.errors.paymentProvidersLoad` больше не теряются из-за порядка ответов API.
- Стартовые content/tariff/provider requests получили lifetime guard и не меняют состояние после ухода со страницы.
- Exhaustive admin responsive test сохраняет все 25 viewport и все sections, но использует измеренный timeout `600 s`: прежние `480 s` обрывали зелёный layout audit под нагрузкой полного параллельного набора.

### Проверено

- До исправления fail-first browser regression получал встроенный текст вместо delayed managed message; после исправления targeted desktop/mobile `2/2`, полный public desktop/mobile regression `30/30`.
- Первый полный browser run прошёл `125/126`, а единственный exhaustive test был оборван ровно на `480 s`; изолированно он прошёл за `427.4 s`, после корректировки полный console-responsive Playwright прошёл `126/126` за `10.2 min`, all-screens `6/6` на 25 viewport-конфигурациях.
- Frontend `125/125`, typecheck/build, audit `0 vulnerabilities`; backend `1125/1125`, EF drift отсутствует, fresh SQLite order/payment/subscription/access flow зелёный.
- Roadmap: `603/623` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.589.0 - 2026-08-11

Release entry: `2026-08-11-runtime-error-ui-boundary`.

### Исправлено

- Public, cabinet и admin больше не выводят `Error.message` напрямую: runtime/handler exceptions проходят через общий русский fallback так же, как API и network failures.
- Удалены англоязычные fallback `Action failed` и `Failed to load`; session hydration, payment-provider loading, renewal details, admin actions и partial-load diagnostics получили контекстные русские сообщения.

### Проверено

- До исправления targeted набор падал `2/59`: public session возвращал `profile unavailable`, а source guard находил прямые exception consumers; после исправления targeted unit/source `59/59`, desktop/mobile browser `6/6`.
- Frontend `125/125`, typecheck/build, audit `0 vulnerabilities`; полный console-responsive Playwright `124/124`, all-screens `6/6` на 25 viewport-конфигурациях; admin bundle raw `519433`, gzip `139444`, largest `219849`.
- Backend `1125/1125`, EF drift отсутствует, fresh SQLite order/payment/subscription/access flow и secret scan `668/0` зелёные; финальные encoding, release-seed smoke и cleanup зафиксированы в `TEST_RESULTS.md`.
- Roadmap: `602/622` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence не переиспользовались.

## 0.588.0 - 2026-08-11

Release entry: `2026-08-11-russian-api-error-boundary`.

### Исправлено

- Англоязычные backend diagnostics и native network errors больше не выводятся напрямую в русской public/cabinet/admin UI; HTTP status и raw payload сохранены внутри `ApiClientError`, а caller-requested abort остаётся управляющей отменой.
- Десять известных promo error patterns переведены в общем API-клиенте, поэтому checkout сохраняет точные подсказки без отдельного расходящегося словаря.

### Проверено

- Инвентаризация controller literals: `162` error strings, `161` без русского текста; до исправления UI/tests показывали `boom`, `Failed to fetch`, `profile unavailable`, `payment provider unavailable` и English VPN phrase.
- Targeted API/public `60/60`, public browser `6/6`, admin desktop/mobile `2/2`; frontend `124/124`, typecheck/build, audit `0 vulnerabilities`; полный console-responsive Playwright `124/124`, all-screens `6/6` на 25 viewport-конфигурациях.
- Backend `1125/1125`, EF drift OK, fresh SQLite order/payment/subscription/access flow OK, secret scan `668/0`; финальный encoding и artifact cleanup зафиксированы в `TEST_RESULTS.md`.
- Roadmap: `601/621` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.587.0 - 2026-08-10

Release entry: `2026-08-10-api-error-payload-hardening`.

### Исправлено

- Общая нормализация теперь одинаково обрабатывает technical и whitespace-only значения в `error`, `message` и plain-text API payload.
- Неизвестный auth machine code больше не обходит caller fallback, при этом словарь известных auth-кодов и человеческие backend-сообщения сохранены.

### Проверено

- До исправления unit regression получал `provider_timeout` из `message` и `unknown_auth_failure` из auth payload; после исправления targeted API suite и admin desktop/mobile Playwright `2/2` проходят.
- Frontend `122/122`, typecheck/build, audit `0 vulnerabilities`; полный console-responsive Playwright `124/124`, включая cabinet `error`, admin `message` и all-screens `6/6` на 25 viewport-конфигурациях.
- Backend `1125/1125`, EF drift OK, fresh SQLite order/payment/subscription/access flow OK, secret scan `668/0`; финальный encoding и artifact cleanup зафиксированы в `TEST_RESULTS.md`.
- Roadmap: `600/620` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.586.0 - 2026-08-10

Release entry: `2026-08-10-api-error-code-fallback`.

### Исправлено

- Общий API-клиент больше не показывает машинные `snake_case` значения поля `error` как пользовательский текст и использует безопасный fallback.
- QR-запросы cabinet/admin выводят контекстное сообщение, а auth translation сохраняет специальные переводы через исходный payload code.

### Проверено

- До исправления confirmed QR `503` показывал `qr_temporarily_unavailable`; после исправления API unit regression и targeted cabinet/admin Playwright `4/4` на desktop/mobile проходят.
- Frontend `122/122`, typecheck/build, audit `0 vulnerabilities`; полный console-responsive Playwright `124/124`, включая all-screens `6/6` на 25 viewport-конфигурациях.
- Backend `1125/1125`, EF drift OK, fresh SQLite order/payment/subscription/access flow OK, secret scan `668/0`; финальный encoding и artifact cleanup зафиксированы в `TEST_RESULTS.md`.
- Roadmap: `599/619` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.585.0 - 2026-08-10

Release entry: `2026-08-10-stale-qr-cache-invalidation`.

### Исправлено

- Cabinet и admin теперь удаляют cached QR SVG до каждого повторного запроса; failed/blocked refresh не оставляет на экране устаревший ключ доступа.
- Очистка применяется ко всем повторным представлениям одного access ID в кабинете и к административной карточке VPN-доступа.

### Проверено

- До исправления после подтверждённого failed QR GET cabinet сохранял `4` preview, admin — `1`; после исправления targeted cabinet/admin `2/2` проходят.
- Frontend `122/122`, typecheck/build, audit `0 vulnerabilities`; полный console-responsive Playwright `124/124`, включая desktop/mobile stale-QR regressions и all-screens `6/6` на 25 viewport-конфигурациях.
- Backend `1125/1125`, EF drift OK, fresh SQLite order/payment/subscription/access flow OK, secret scan `668/0`; финальный encoding и artifact cleanup зафиксированы в `TEST_RESULTS.md`.
- Roadmap: `598/618` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.584.0 - 2026-08-10

Release entry: `2026-08-10-password-reset-form-separation`.

### Исправлено

- Public и cabinet password reset разделены на две семантически независимые формы: запрос кода и подтверждение нового пароля; Enter в поле email теперь запускает именно forgot-password.
- Ошибки отсутствующих кода и нового пароля больше не показываются сразу при вводе email, а каждая стадия имеет собственный validation summary и доступное имя формы.

### Проверено

- До исправления public Playwright получал `0` forgot-password запросов после Enter вместо `1`; после исправления targeted public `1/1` и полный cabinet lifecycle `1/1` проходят.
- Frontend `122/122`, typecheck/build, audit `0 vulnerabilities`; полный console-responsive Playwright `124/124`, включая desktop/mobile reset flow и all-screens `6/6` на 25 viewport-конфигурациях.
- Backend `1125/1125`, EF drift OK, fresh SQLite order/payment/subscription/access flow OK, secret scan `668/0`; финальный encoding и artifact cleanup зафиксированы в `TEST_RESULTS.md`.
- Roadmap: `597/617` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.583.0 - 2026-08-10

Release entry: `2026-08-10-responsive-breakpoint-pairs`.

### Улучшено

- Responsive all-screens matrix расширена с 19 до 25 конфигураций и теперь проверяет точные пары `N/N+1` для всех CSS-breakpoints `390`, `520`, `640`, `768`, `820`, `900`, `960`, `1024` и `1280` px.
- Новый source guard автоматически извлекает `max-width` из shared/public/cabinet/admin CSS и не допускает появление breakpoint без обеих сторон в browser matrix; backend contract дублирует критический инвариант.

### Проверено

- До исправления targeted guard падал на первой отсутствующей точной границе `1280`; после расширения targeted frontend `2/2`, all-screens `6/6` и полный console-responsive Playwright `124/124` проходят.
- Frontend `122/122`, typecheck/build, audit `0 vulnerabilities`; backend `1125/1125`, EF drift OK, fresh SQLite order/payment/subscription/access flow OK, secret scan `668/0`.
- RoadmapCurrentStateTests, release/docs guards, strict UTF-8 и artifact cleanup подтверждены после синхронизации статуса.
- Roadmap: `596/616` closed, readiness `96.8%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.582.0 - 2026-08-10

Release entry: `2026-08-10-cabinet-help-navigation`.

### Исправлено

- Пункт «Помощь» в шапке личного кабинета теперь открывает полноценную инструкцию `/help`, а не соседний FAQ `/faq`.
- Cabinet-to-public переход продолжает использовать проверенный credential-free `publicWebUrl`; destination одинаково корректен на desktop, mobile и узком viewport 305 px.

### Проверено

- До исправления source guard и desktop/mobile Playwright падали `0/1` и `0/2`, фактически получая `http://127.0.0.1:5293/faq`; после исправления targeted unit `1/1`, browser `2/2` и ручной переход в локальном браузере ведут на `/help`.
- Frontend `121/121`, typecheck/build, audit `0 vulnerabilities`; полный console-responsive Playwright `124/124`, включая all-screens `6/6` на 19 viewport-конфигурациях.
- Backend `1125/1125`, EF drift OK, fresh SQLite order/payment/subscription/access flow OK, secret scan `668/0`; финальный encoding и artifact cleanup зафиксированы в `TEST_RESULTS.md`.
- Roadmap: `595/615` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.581.0 - 2026-08-10

Release entry: `2026-08-10-admin-action-capability-boundary`.

### Исправлено

- Общий admin action dispatcher теперь проверяет capability целевого раздела, а не активного tab; finance-role больше не может вызвать hidden tariff/VPN/provisioning mutation из writable Dashboard или Payments.
- Все `54` action callsites типизированно привязаны к section capability; subscription migration требует одновременно subscription и VPN write-права, read-only QR явно исключен из mutation gate.

### Проверено

- До исправления desktop/mobile отправляли hidden tariff PATCH из finance-session; после исправления denied targeted `2/2` и разрешенный admin desktop/mobile lifecycle `66/66` зеленые.
- Frontend `121/121`, typecheck/build, audit `0 vulnerabilities`; admin bundle raw `517701`, gzip `138757`, largest `219849`.
- Полный console-responsive Playwright `124/124`, backend `1125/1125`, EF drift OK, fresh SQLite latest release OK, encoding `14/14`, secret scan `668/0`, artifact cleanup OK.
- Roadmap: `594/614` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.580.0 - 2026-08-10

Release entry: `2026-08-10-admin-hidden-form-capability-boundary`.

### Исправлено

- Admin mutation handlers для releases, FAQ, content, scenarios, support и Telegram settings повторно проверяют capability до validation и API; `hidden` форма больше не считается границей доступа.
- Finance/read-only сессии не могут отправить programmatic submit, support reply/note и Telegram connection test в обход скрытых controls; backend RBAC остается второй fail-closed границей.

### Проверено

- До исправления finance-role отправлял PATCH Telegram settings на desktop/mobile; после исправления targeted browser `2/2` и полный console-responsive Playwright `124/124` зеленые.
- Frontend `121/121`, typecheck/build, audit `0 vulnerabilities`; admin bundle raw `517096`, gzip `138654`, largest `219849`.
- Backend `1125/1125`, EF drift отсутствует, secret scan `668/0`; fresh SQLite latest release подтверждается финальным gate.
- Roadmap: `593/613` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; staging-ready baseline не production-ready, внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.579.0 - 2026-08-10

Release entry: `2026-08-10-admin-vpn-form-validation-boundary`.

### Исправлено

- Admin server и inbound handlers теперь повторно валидируют payload при любом submit и не полагаются на состояние disabled-кнопки; programmatic submit больше не отправляет пустой сервер или inbound с недопустимым портом.
- Формы VPN-сервера, 3x-ui панели и inbound проверяют целые диапазоны, обязательные panel credentials при создании, JSON-object поля, `network` в stream settings и согласованность default/active; server API отклоняет credential-bearing и non-HTTP panel base URL при create/update.

### Проверено

- До исправления desktop/mobile отправляли по одному некорректному server и inbound POST, а semantic form gate считал unsafe server URL и `priority=0` допустимыми; после исправления targeted browser `6/6`, server-management backend `18/18` и полный console-responsive Playwright `122/122` зелёные.
- Frontend `121/121`, typecheck/build, audit `0 vulnerabilities`; cabinet JS `359.27 kB` raw/`104.22 kB` gzip, admin bundle raw `516939`, gzip `138621`, largest `219849`.
- Backend `1125/1125`, fresh SQLite latest release и secret scan `668/0` подтверждены финальным gate.
- Roadmap: `592/612` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.578.0 - 2026-08-10

Release entry: `2026-08-10-admin-config-url-credentials-boundary`.

### Исправлено

- Admin-формы payment provider, Telegram Webhook/WebApp и 3x-ui panel теперь отклоняют `http/https` URL со встроенными логином или паролем, показывают понятную ошибку и блокируют submit до исправления.
- Backend применяет ту же fail-closed границу при записи, включении и выборе checkout account: защищены API/return/webhook URL провайдеров, `hostedCheckoutUrl`, Telegram URL и base URL 3x-ui; legacy unsafe default не мешает выбрать корректный fallback.

### Проверено

- До исправления backend воспроизводил шесть ошибочно принятых credential-bearing create/enable/checkout cases, а targeted admin desktop/mobile падал `6/6`; после исправления URL unit `2/2`, backend targeted `20/20`, browser targeted `6/6` и полный console-responsive Playwright `118/118` зелёные.
- Frontend `121/121`, typecheck/build, audit `0 vulnerabilities`; cabinet JS `359.27 kB` raw/`104.22 kB` gzip, admin bundle raw `515206`, gzip `138227`, largest `219849`.
- Backend `1119/1119`, fresh SQLite latest release и secret scan `668/0` подтверждены финальным gate.
- Roadmap: `591/611` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.577.0 - 2026-08-10

Release entry: `2026-08-10-cabinet-url-boundary`.

### Исправлено

- Cabinet-to-public ссылки теперь строятся только из credential-free `http/https` base URL без query/fragment; некорректный `VITE_PUBLIC_WEB_URL` безопасно откатывается к локальному origin/dev port mapping.
- Повторная оплата передаёт провайдеру канонический `window.location.origin`, а не полный browser URL с query/fragment, поэтому приватный навигационный контекст не попадает во внешний payment request.

### Проверено

- До исправления targeted cabinet desktop/mobile воспроизводил утечку query/fragment `2/2`; после исправления URL resolver unit `2/2`, targeted browser `2/2` и полный console-responsive Playwright `118/118` зелёные.
- Frontend `119/119`, typecheck/build, audit `0 vulnerabilities`; cabinet JS `359.27 kB` raw/`104.22 kB` gzip, admin bundle raw `514172`, gzip `138052`, largest `219849`.
- Backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate.
- Roadmap: `590/610` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.576.0 - 2026-08-10

Release entry: `2026-08-10-confirm-action-async-lifecycle`.

### Исправлено

- Все подтверждаемые административные операции теперь возвращают свой Promise в общий `ConfirmButton`: диалог остаётся открытым и блокирует подтверждение/отмену до фактического завершения API-запроса.
- Browser regression задерживает смену статуса платёжного провайдера и проверяет busy-состояние, единственный запрос и закрытие диалога только после ответа на desktop/mobile; source guard запрещает повторное отбрасывание Promise в `onConfirm`.

### Проверено

- До исправления targeted provider lifecycle воспроизводил раннее закрытие `2/2`; после исправления targeted desktop/mobile `2/2` и полный console-responsive Playwright `118/118` зелёные.
- Frontend `117/117`, typecheck/build, audit `0 vulnerabilities`; admin bundle raw `514172`, gzip `138052`, largest `219849`.
- Backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate.
- Roadmap: `589/609` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.575.0 - 2026-08-10

Release entry: `2026-08-10-shared-css-external-asset-guard`.

### Исправлено

- Из общего stylesheet удален недостижимый legacy `.hero` с runtime URL Unsplash; связанный неиспользуемый `HomePage` удален из public bundle source.
- Visual-assets guard теперь проверяет shared CSS вместе с app-specific стилями, а browser page-quality gate отклоняет cross-origin asset URL во всех загруженных stylesheet rules, даже если правило не совпало с текущим DOM.

### Проверено

- Targeted visual source `1/1` и public all-screens browser `1/1`; полный console-responsive Playwright `118/118` на 19 viewport-конфигурациях.
- Frontend `117/117`, typecheck/build, audit `0 vulnerabilities`; public/cabinet/admin CSS уменьшены до `28.00/24.25/26.44 kB` raw без изменения активных локальных WebP.
- Admin bundle raw `514307`, gzip `138055`, largest `219849`; backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate.
- Roadmap: `588/608` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.574.0 - 2026-08-10

Release entry: `2026-08-10-status-badge-semantic-tones`.

### Исправлено

- Общая статусная метка больше не считает `Unhealthy`, `Inactive` и `NotLinked` успешными из-за совпадений с подстроками `healthy`, `active` и `linked`; отрицательные составные статусы теперь распознаются до положительных fallback-правил.
- `Succeeded`, `Degraded`, `SyncRequired`, `Expired` и lowercase-статусы поддержки получили согласованные тона и русские подписи во всех приложениях.

### Проверено

- Компонентный regression покрывает danger/neutral/warning/success и регистронезависимую локализацию; frontend unit `117/117`, typecheck/build, audit `0 vulnerabilities`.
- Cabinet dashboard проверяет нейтральный `NotLinked` и успешный `Succeeded`; stale `Expired` order проверяется на desktop/mobile, полный console-responsive Playwright `118/118`.
- Cabinet JS `359.18/104.18 kB`; admin bundle raw `514307`, gzip `138055`, largest `219849`; backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate.
- Roadmap: `587/607` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.573.0 - 2026-08-10

Release entry: `2026-08-10-cabinet-app-version-modal-focus-trap`.

### Исправлено

- Модальное окно «Что нового» больше не выпускает клавиатурный focus на скрытый под overlay кабинет: Tab/Shift+Tab замыкаются на видимых controls, фон получает `inert`, а body scroll блокируется.
- Escape закрывает окно и возвращает focus к живой кнопке открытия; на mobile история переводит focus в кнопку закрытия и возвращает его после выбора версии, а исходные `inert`/`overflow` значения восстанавливаются.

### Проверено

- Targeted desktop/mobile modal lifecycle: `2/2`; focus trap, opener restore, background isolation, scroll lock, viewport bounds и отсутствие horizontal overflow подтверждены.
- Cabinet modal проходит все `19` viewport-конфигураций `305x568..2560x1440`, включая стороны breakpoints, compact Axe и responsive control clipping gate.
- Cabinet desktop/mobile suite `24/24`; полный console-responsive Playwright `118/118`; frontend unit `116/116`, typecheck/build, audit `0 vulnerabilities`.
- Admin bundle raw `512499`, gzip `137570`, largest `219849`; backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate.
- Roadmap: `586/606` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.

## 0.572.0 - 2026-08-10

Release entry: `2026-08-10-admin-skip-link-route-preservation`.

### Исправлено

- Skip-ссылки login, session recovery и authenticated admin screen больше не заменяют section hash служебными `#admin-login`, `#admin-session-recovery` или `#admin-content`.
- Клавиатурный переход сохраняет deep-linked раздел до входа, после входа и после reload, одновременно переводя focus в нужную область страницы.

### Проверено

- Targeted desktop/mobile skip-link lifecycle: `2/2`; текущий `#support`, видимый tabpanel, title и focus сохраняются через login и reload.
- Полный admin desktop/mobile suite `60/60`; полный console-responsive Playwright `116/116`, включая все 17 sections на 18 viewport-конфигурациях и Axe gate.
- Frontend unit `116/116`, typecheck/build всех приложений, audit `0 vulnerabilities`; admin bundle raw `512499`, gzip `137570`, largest `219849`.
- Backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.
- Roadmap: `585/605` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.571.0 - 2026-08-10

Release entry: `2026-08-10-admin-invalid-hash-canonical-fallback`.

### Исправлено

- Неизвестный admin hash больше не оставляет URL и видимый экран в разных состояниях: непустой invalid fragment канонизируется в `#dashboard`, а обычный корневой `/` не переписывается.
- Runtime invalid hash восстанавливает Dashboard, title и `admin-content` focus; Back возвращает предыдущий валидный раздел без сохранения несуществующего fragment.

### Проверено

- Direct `#unknown` и runtime `#not-a-section -> #dashboard -> Back -> #payments` проходят Desktop Chrome и Pixel 5: `2/2`.
- Полный admin desktop/mobile suite `58/58`; полный console-responsive Playwright `114/114`, включая все 17 sections на 18 viewport-конфигурациях и Axe gate.
- Frontend unit `116/116`, typecheck/build всех приложений, audit `0 vulnerabilities`; admin bundle raw `512438`, gzip `137533`, largest `219849`.
- Backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.
- Roadmap: `584/604` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.570.0 - 2026-08-10

Release entry: `2026-08-10-admin-section-history-focus-fix`.

### Исправлено

- Переключение admin sections больше не перезаписывает одну history-entry: browser Back/Forward восстанавливают предыдущий hash-раздел вместо выхода на предшествующую админке страницу.
- После Back/Forward и ролевого fallback видимый tabpanel, title и клавиатурный focus синхронизируются с основным admin content; внутренние ссылки заказа не оставляют focus в скрытом разделе.

### Проверено

- Stateful Dashboard -> Payments -> Support -> Back/Forward и order-links к пользователю/платежу/подписке проходят на Desktop Chrome и Pixel 5: `4/4`.
- Полный admin desktop/mobile suite `56/56`; полный console-responsive Playwright `112/112`, включая все 17 sections на 18 viewport-конфигурациях и Axe gate.
- Frontend unit `116/116`, typecheck/build всех приложений, audit `0 vulnerabilities`; admin bundle raw `512330`, gzip `137506`, largest `219849`.
- Backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.
- Roadmap: `583/603` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.569.0 - 2026-08-10

Release entry: `2026-08-10-admin-section-metadata-lifecycle-fix`.

### Исправлено

- Login, восстановление сессии и 17 рабочих разделов админки больше не делят статический browser title: metadata следует текущему session/section state, включая direct hash и logout.
- Для каждого authenticated section meta description синхронизирован с его рабочим назначением; login/hydration получают отдельные безопасные описания без данных пользователя.

### Проверено

- Pure metadata contract покрывает hydration/login/authenticated state; frontend unit `116/116`, typecheck/build всех приложений и audit `0 vulnerabilities`.
- Stateful `#payments -> login -> support -> logout` проходит на Desktop Chrome и Pixel 5: `2/2`; all-screens проверяет точный title login и всех 17 admin sections.
- Полный console-responsive Playwright `108/108`; admin bundle остается в бюджете: raw `512423`, gzip `137489`, largest `219849`.
- Backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate; внешние VPS/staging/payment/3x-ui/Telegram/SMTP evidence остаются открытыми.
- Roadmap: `582/602` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.568.0 - 2026-08-10

Release entry: `2026-08-10-public-route-metadata-focus-fix`.

### Исправлено

- SPA-переходы с главной больше не оставляют stale SEO title/description на тарифах, FAQ, помощи, аккаунте и `404`: каждый route получает собственные metadata, включая direct deep-link и trailing slash.
- После внутреннего перехода и browser Back/Forward scroll возвращается к началу страницы, а клавиатурный фокус переносится на новый `main-content` вместо общей шапки.

### Проверено

- Pure route mapping покрывает все известные/неизвестные пути; frontend unit `115/115`, typecheck/build всех приложений, admin bundle budget и audit `0 vulnerabilities`.
- Stateful title/meta/focus/Back regression проходит на Desktop Chrome и Pixel 5: `2/2`; шесть direct public routes проходят title, render, Axe и 18 responsive viewport-конфигураций.
- Полный console-responsive Playwright: `106/106`; backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate.
- Реальные VPS/staging/live payment/production-like 3x-ui, Telegram Bot API/webhook и SMTP evidence остаются внешними.
- Roadmap: `581/601` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.567.0 - 2026-08-10

Release entry: `2026-08-10-public-not-found-page-fix`.

### Исправлено

- Неизвестный публичный URL больше не оставляет после header пустой экран и не вызывает React Router warning: catch-all route показывает доступное `404` состояние с переходами на главную и в помощь.
- Неизвестный маршрут добавлен в all-screens матрицу non-blank, page quality, Axe WCAG A/AA и responsive overflow проверок.

### Проверено

- Целевой `404` lifecycle проходит на Desktop Chrome и Pixel 5: `2/2`; возврат на главную, main landmark, console/page errors и overflow проверены.
- Все шесть public route-состояний проходят render/WCAG и 18 viewport-конфигураций `305x568..2560x1440`; desktop/mobile screenshots просмотрены вручную.
- Frontend unit `114/114`, typecheck/build всех приложений, admin bundle budget и dependency audit `0 vulnerabilities`; полный console-responsive Playwright `104/104`.
- Backend `1113/1113`, fresh SQLite latest release и secret scan подтверждены финальным gate; реальные VPS/staging/live payment/3x-ui/Telegram/SMTP evidence остаются внешними.
- Roadmap: `580/600` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.566.0 - 2026-08-10

Release entry: `2026-08-10-admin-bundle-budget`.

### Улучшено

- Admin production bundle разделен на стабильные `vendor-react`, `platform-api`, `platform-ui`, runtime и изменяемый app chunk; largest chunk уменьшен с `512060` до `219849` bytes без повышения Vite warning limit.
- Обязательный post-build budget ограничивает число JS assets `5`, largest chunk `360 KiB`, общий raw `540 KiB` и общий gzip `145 KiB`; превышение завершает `npm run build` с ошибкой.

### Проверено

- Фактический admin build: `5` JS chunks, raw `511564`, gzip `137127`, largest `219849`; frontend unit `114/114`, typecheck/build всех приложений, audit `0 vulnerabilities`.
- Production preview Chromium загрузил все chunks без HTTP/browser errors и overflow на `1440x900`/`320x720`; admin all-screens render/responsive `2/2`.
- Backend `1113/1113`, полный console-responsive Playwright baseline `102/102`, fresh SQLite smoke latest release OK; secret scan `659` файлов, `0` находок.
- Реальные VPS/staging/live payment/production-like 3x-ui, provider, Telegram Bot API/webhook и SMTP evidence не переиспользовались.
- Roadmap: `579/599` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.565.0 - 2026-08-10

Release entry: `2026-08-10-admin-support-channel-lifecycle-fix`.

### Исправлено

- Web-обращение без `telegramUserId` больше не обещает Telegram-доставку: backend возвращает `saved`, а admin UI показывает «Сохранить ответ» и точное уведомление о сохранении.
- Telegram-обращение сохраняет отдельную команду «Отправить через Telegram» и статус очереди; кнопки смены статуса блокируются на время запроса и не допускают повторную операцию.

### Проверено

- Stateful SupportAgent flow выполняет reply, internal note, pending, close, reload, reopen и повторный reload с revision `0 -> 5`, authorization и сохранением сообщений на desktop/mobile.
- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`; targeted/adjacent admin Playwright `6/6`.
- Backend support contract `4/4`; backend full suite `1113/1113`, console-responsive Playwright `102/102`, fresh SQLite smoke и secret scan подтверждаются финальным gate этого release.
- `RoadmapCurrentStateTests` синхронизирует release seed, docs и счетчики, не закрывая внешние acceptance-пункты локальными mock/smoke.
- Fresh local SQLite smoke подтвердил latest release `2026-08-10-admin-support-channel-lifecycle-fix`; secret scan `657` файлов, `0` находок.
- Реальные VPS/staging/live payment/production-like 3x-ui, provider, Telegram Bot API/webhook и SMTP delivery evidence не переиспользовались.
- Roadmap: `578/598` closed, readiness `96.7%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.564.0 - 2026-08-10

Release entry: `2026-08-10-admin-notification-retry-lifecycle-e2e`.

### Улучшено

- Admin browser regression выполняет failed email notification retry и подтверждает persisted `Pending` state после reload на desktop/mobile.
- Stateful mock сбрасывает attempts `5 -> 0`, очищает error, назначает next attempt и исключает повторный retry вне Failed state.
- FinanceManager и SupportAgent видят только masked recipient и не получают write-control для retry.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `100/100`; notification retry lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальные VPS/staging/live payment/production-like 3x-ui, provider, Telegram и SMTP delivery evidence не переиспользовались.
- Roadmap: `577/597` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.563.0 - 2026-08-10

Release entry: `2026-08-10-admin-payment-refund-lifecycle-fix`.

### Исправлено

- После частичного refund admin UI больше не сохраняет старую сумму выше нового refundable max: следующий возврат автоматически получает остаток.
- Полностью возвращённый платёж показывает точный blocker «Сумма уже возвращена», а не сообщение о неуспешной оплате.
- Stateful order/payment/refund mock сохраняет recheck, partial/full refund и журнал возвратов после reload.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `98/98`; payment refund lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальные VPS/staging/live payment/production-like 3x-ui, provider, Telegram и SMTP evidence не переиспользовались.
- Roadmap: `576/596` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.562.0 - 2026-08-10

Release entry: `2026-08-10-admin-subscription-actions-lifecycle-e2e`.

### Улучшено

- Admin browser regression выполняет activate/extend/sync/block/reload/unblock/migrate/reload/cancel lifecycle подписки на desktop/mobile.
- Stateful mock API сохраняет status, renewal count, block reason, target server и связанную access revision, проверяет payload и authorization всех action routes.
- После отмены подписка остаётся terminal после reload, VPN-доступ отозван, provider ID/URI/команды скрыты, horizontal overflow и console errors отсутствуют.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `96/96`; subscription lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальные VPS/staging/live payment/production-like 3x-ui, provider, Telegram и SMTP evidence не переиспользовались.
- Roadmap: `575/595` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.561.0 - 2026-08-10

Release entry: `2026-08-10-admin-vpn-access-actions-lifecycle-e2e`.

### Улучшено

- Admin browser regression выполняет disable/reload/enable/sync/reset-traffic lifecycle VPN-доступа на desktop/mobile.
- Stateful mock API сохраняет status, disabledAt и revision `1 -> 5`, проверяет reason payload и authorization всех action routes.
- Terminal revoked/cancelled secrets остаются скрытыми; confirm-dialog, console errors и horizontal overflow проверяются после операций.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `94/94`; VPN access lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальные VPS/staging/live payment/production-like 3x-ui, provider, Telegram и SMTP evidence не переиспользовались.
- Roadmap: `574/594` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.560.0 - 2026-08-10

Release entry: `2026-08-10-admin-vpn-client-actions-lifecycle-e2e`.

### Улучшено

- Admin browser regression выполняет disable/reload/enable/sync/reset-traffic lifecycle 3x-ui клиента на desktop/mobile.
- Stateful mock API сохраняет client state и проверяет фактические POST routes, authorization, confirm-dialog и reload persistence.
- UI подтверждает каждый sync status, отсутствие console errors и горизонтального overflow после операций.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `92/92`; 3x-ui client lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальные VPS/staging/live payment/production-like 3x-ui, provider, Telegram и SMTP evidence не переиспользовались.
- Roadmap: `573/593` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.559.0 - 2026-08-10

Release entry: `2026-08-10-admin-provisioning-validation-lifecycle-e2e`.

### Улучшено

- Admin browser regression выполняет health-check, dry-run precheck, прямую validation preparation и deploy/cancel/retry/support lifecycle подготовки VPS на desktop/mobile.
- Stateful mock API сохраняет provisioning runs и проверяет допустимые переходы `ReadyToDeploy -> DeployQueued -> Cancelled -> Retrying`, payload и authorization.
- Confirm-dialog показывает validation mode и operator warning; reload сохраняет run state без вызова реального SSH/Ansible.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `90/90`; safe provisioning lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальные VPS/SSH/Ansible, staging/live payment/production-like 3x-ui, provider, Telegram и SMTP evidence не переиспользовались.
- Roadmap: `572/592` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.558.0 - 2026-08-10

Release entry: `2026-08-10-admin-vpn-infrastructure-secure-lifecycle-e2e`.

### Улучшено

- Admin browser regression выполняет create/reload/edit/status/delete lifecycle VPN-сервера и create/reload/edit/status/archive lifecycle 3x-ui панели на desktop/mobile.
- SSH credential и пароли панелей остаются write-only: отправляются только в mutation payload, очищаются после сохранения и не возвращаются в DTO или DOM.
- Stateful mock API поддерживает CRUD серверов, панелей и inbound-правил, включая default/disable/enable, authorization, confirm-dialog, console и overflow assertions.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `88/88`; secure infrastructure lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальные VPS/staging/live payment/production-like 3x-ui, provider, Telegram и SMTP evidence не переиспользовались.
- Roadmap: `571/591` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.557.0 - 2026-08-10

Release entry: `2026-08-10-admin-telegram-bot-settings-secure-lifecycle-e2e`.

### Улучшено

- Admin browser regression сохраняет Telegram bot mode, URLs, тексты и write-only bot/webhook tokens через фактическую форму на desktop/mobile.
- После PATCH и общего reload значения формы сохраняются, secret inputs очищаются, а DTO/DOM содержит только безопасные configured-признаки без raw tokens.
- Stateful mock API поддерживает save/check lifecycle, повторное редактирование без замены токенов, authorization, console и overflow assertions.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `86/86`; secure Telegram settings lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальный Telegram Bot API/webhook, VPS/staging/live payment/production-like 3x-ui и SMTP evidence не переиспользовались.
- Roadmap: `570/590` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.556.0 - 2026-08-10

Release entry: `2026-08-10-admin-payment-provider-secure-lifecycle-e2e`.

### Улучшено

- Admin browser regression выполняет create/edit/disable/reload/enable/check lifecycle платежного аккаунта через фактическую форму на desktop/mobile.
- Write-only secret-поля отправляются при создании, остаются пустыми при редактировании и никогда не появляются в DTO/DOM; UI показывает только безопасные `hasSecretKey`/`hasWebhookSecret` признаки.
- Stateful mock API хранит provider account state и проверяет mutation payload, authorization, readiness result, console errors и горизонтальный overflow.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `84/84`; secure provider lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Реальный provider sandbox/live кабинет, VPS/staging/production-like 3x-ui и SMTP evidence не переиспользовались.
- Roadmap: `569/589` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.555.0 - 2026-08-10

Release entry: `2026-08-10-cabinet-telegram-support-status-e2e`.

### Улучшено

- Cabinet browser regression выполняет создание Telegram deep-link, подтверждает внешний linked-state и отвязывает аккаунт через фактические UI-кнопки на desktop/mobile.
- Обращение поддержки закрывается и переоткрывается с проверкой optimistic revision `0 -> 1 -> 2`, mutation payload и сохранения статуса после общего reload.
- Stateful mock API хранит Telegram/support status и проверяет авторизацию, отсутствие console errors и горизонтального overflow.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `82/82`; cabinet Telegram/support lifecycle проходит `2/2`, all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- External VPS/staging/live payment/production-like 3x-ui и SMTP evidence не переиспользовалось.
- Roadmap: `568/588` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.554.0 - 2026-08-10

Release entry: `2026-08-10-admin-managed-configuration-crud-e2e`.

### Улучшено

- Admin browser regression выполняет полный create/edit/delete lifecycle тарифов, сценариев, релизов, FAQ и контента сайта, create/edit реферальной программы, disable тарифа и восстановление обязательных блоков главной.
- Mock API хранит состояние управляемых сущностей между mutation и общим reload, поэтому тест подтверждает не только отправку запроса, но и фактическое повторное отображение результата после загрузки данных.
- Один и тот же сценарий проходит desktop и mobile-admin через реальные формы, доступные кнопки и confirm-dialog, проверяя API methods/payload и отсутствие горизонтального overflow.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `80/80`; all-screens axe/responsive matrix `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- External VPS/staging/live payment/production-like 3x-ui и SMTP evidence не переиспользовалось.
- Roadmap: `567/587` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.553.0 - 2026-08-10

Release entry: `2026-08-10-admin-critical-operations-e2e`.

### Исправлено

- Вкладка «Оплаты» теперь объединяет настройки провайдеров, заказы, платежи и возвраты в одном корректном `tabpanel`; переходы и scoped browser-действия больше не теряют визуально связанные операции.
- Главный admin E2E выполняет повтор email-доставки, перепроверку заказа и платежа, возврат, продление/синхронизацию/блокировку подписки, disable/sync/reset VPN-доступа, ответ, внутреннюю заметку и смену статуса поддержки.
- Public checkout использует точное accessible name заголовка аккаунта, а последовательный axe-аудит 17 admin-разделов получил реалистичный общий timeout без ослабления проверок.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений, dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `78/78`; критический admin flow проходит на desktop и mobile-admin, all-screens axe/responsive matrix проходит `6/6`.
- Backend `1112/1112`, fresh local SQLite checkout/payment/subscription/VPN smoke и secret scan `657/0` пройдены.
- Roadmap/release/strict UTF-8 guards `49/49` пройдены; временные Playwright traces/reports и frontend build artifacts очищены.
- External VPS/staging/live payment/production-like 3x-ui и SMTP evidence не переиспользовалось.
- Roadmap: `566/586` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.552.0 - 2026-08-09

Release entry: `2026-08-09-automated-wcag-accessibility-gate`.

### Исправлено

- All-screens gate запускает axe с тегами WCAG 2.0/2.1/2.2 A/AA и best practices без allow-list для 5 public routes, cabinet auth/dashboard и admin auth/17 sections на desktop и 320 px.
- Публичный красный акцент затемнен до AA-контраста; auth tabs больше не назначают недопустимый `tabpanel` непосредственно форме.
- Структура `/account` получила последовательный heading level, а admin auth/recovery больше не вкладывает второй `main` landmark в общий `PageShell`.

### Проверено

- Frontend `112/112`, typecheck/build всех приложений; dependency audit сообщает `0 vulnerabilities`.
- All-screens `6/6` проходит axe и 18 viewport-конфигураций `305x568..2560x1440`; browser audit не использует исключения rules/selectors.
- Backend `1112/1112`, strict UTF-8 guards и fresh local SQLite smoke пройдены; автоматический axe-аудит не заменяет ручную проверку доступности.
- External VPS/staging/live payment/production-like 3x-ui и SMTP evidence не переиспользовалось.
- Roadmap: `565/585` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.551.0 - 2026-08-09

Release entry: `2026-08-09-local-visual-assets-responsive-boundaries`.

### Исправлено

- Публичная витрина и экран входа администратора больше не загружают четыре фоновых изображения с внешнего Unsplash: три оптимизированных WebP входят в frontend bundle и доступны независимо от сети, CSP и стороннего CDN.
- Рабочая и глобальная сцены получили проверенный overlay-контраст, при котором изображение остаётся различимым, а карточки и текст сохраняют читаемость.
- Hero и admin-login typography больше не масштабируется через viewport width; размеры фиксированы на явных responsive breakpoints.

### Проверено

- Frontend `111/111`, typecheck/build всех приложений; production bundle содержит локальные хешированные WebP assets, dependency audit сообщает `0 vulnerabilities`.
- Полный console-responsive Playwright suite `78/78`; all-screens `6/6` проходит `18` viewport-конфигураций от `305x568` и mobile landscape до `2560x1440`, включая обе стороны CSS-breakpoints.
- Browser test декодирует каждый runtime background, проверяет same-origin, минимальный размер `1200x800`, отсутствие overflow/clipped controls, console errors и blank screens на 5 public routes, cabinet и 17 admin sections.
- Desktop/mobile screenshots публичной витрины, account, cabinet и admin просмотрены вручную; временные screenshots и build artifacts очищаются перед коммитом.
- Backend `1112/1112`, solution Release build `0` warnings/`0` errors; external VPS/staging/live payment/production-like 3x-ui и SMTP evidence не переиспользовалось.
- Roadmap: `564/584` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress, `0` blocked.

## 0.550.0 - 2026-08-09

Release entry: `2026-08-09-public-cabinet-mutation-request-lifecycle`.

### Исправлено

- Cabinet QR, Telegram, support, payment и renewal actions применяют результат только в исходной session operation; delayed completion после logout/new login не возвращает старый UI.
- Cabinet auth/manual refresh и публичные auth/reset формы получили синхронный in-flight guard, поэтому два события до React-render создают один API request и не переиспользуют rotating refresh-token.
- Новый support/reset draft не стирается завершением старой отправки; смена support conversation очищает reply draft и исключает перенос текста между обращениями.
- Public checkout инвалидируется при уходе со страницы, session hydration допускает повторный вход с тем же access token, а checkout claim key учитывает текущую сессию.

### Проверено

- Frontend `110/110`, typecheck/build всех приложений и dependency audit `0 vulnerabilities`.
- Полный desktop/mobile console-responsive Playwright suite `78/78`; новые public/cabinet lifecycle regressions проходят на обоих viewport.
- Backend `1112/1112`, solution Release build `0` warnings/`0` errors, EF model drift отсутствует и fresh local SQLite checkout/payment/subscription/VPN smoke пройден.
- Strict UTF-8 guards, secret scan `655/0` и artifact cleanup пройдены.
- `RoadmapCurrentStateTests` фиксирует `563/583` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.549.0 - 2026-08-09

Release entry: `2026-08-09-admin-mutation-request-lifecycle`.

### Исправлено

- CRUD/payment/subscription/VPN/provisioning/bot actions применяют setter и reload только в исходной admin session operation; delayed completion после logout/new login не затрагивает новую сессию.
- `runAction` немедленно регистрирует in-flight operation, поэтому два синхронных submit события одного действия создают только один API request.
- Завершение старого save сбрасывает форму только при совпадении отправленного snapshot; новый draft и несохраненные настройки Telegram-бота сохраняются при mutation/background reload.
- Full admin reload, user filter и VPN panel details используют request/session generation и принимают только последний актуальный ответ.

### Проверено

- Frontend `110/110`, typecheck/build всех приложений и dependency audit `0 vulnerabilities`.
- Admin desktop/mobile `28/28`; полный desktop/mobile console-responsive Playwright suite `66/66` покрывает duplicate submit, post-logout completion, новый draft во время pending save и dirty bot form reload.
- Backend `1112/1112`, API/TelegramBot Release build `0` warnings/`0` errors и fresh local SQLite checkout/payment/subscription/VPN smoke пройдены.
- EF drift, secret scan `655/0`, strict UTF-8 guards и artifact cleanup пройдены.
- `RoadmapCurrentStateTests` фиксирует `562/582` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.548.0 - 2026-08-09

Release entry: `2026-08-09-admin-detail-request-lifecycle`.

### Исправлено

- User overview и support messages в админке принимают только последний запрос выбранной записи текущей session operation; поздние ответы не заменяют новый выбор и не возвращаются после logout.
- Support status action для невыбранного обращения больше не загружает его сообщения в открытый диалог; reply/note/status reload ограничен актуальным thread и отбрасывается после завершения сессии.
- Смена пользователя или обращения немедленно очищает старый detail state, reply/note drafts и показывает явные loading/empty состояния вместо данных предыдущего выбора.

### Проверено

- Frontend `110/110`, typecheck/build всех приложений и dependency audit `0 vulnerabilities`.
- Admin desktop/mobile `24/24`; полный desktop/mobile console-responsive Playwright suite `62/62` покрывает out-of-order user/support requests, status action scope, draft cleanup и post-logout completion.
- Backend `1112/1112`, Release build `0` warnings/`0` errors и fresh local SQLite checkout/payment/subscription/VPN smoke пройдены.
- EF drift, secret scan, strict UTF-8 guards и artifact cleanup пройдены.
- `RoadmapCurrentStateTests` фиксирует `561/581` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.547.0 - 2026-08-09

Release entry: `2026-08-09-cabinet-support-request-lifecycle`.

### Исправлено

- Загрузка сообщений кабинета принимает ответ только для актуальной session operation и последнего выбранного обращения; медленный старый thread больше не заменяет текущую переписку.
- Logout инвалидирует pending support/provider requests и очищает сообщения, support drafts, связанные order/subscription, auth fields, password reset token и payment selection до следующего входа.
- Создание обращения сохраняет await-контракт первой загрузки сообщений без дублирующего effect-запроса, поэтому terminal `401` по-прежнему завершает локальную сессию fail-closed.

### Проверено

- Frontend `110/110`, typecheck/build всех приложений и dependency audit `0 vulnerabilities`.
- Cabinet desktop/mobile `14/14`; полный desktop/mobile console-responsive Playwright suite `58/58` покрывает out-of-order thread, logout во время delayed response, повторный вход и private draft cleanup.
- Backend `1112/1112`, Release build `0` warnings/`0` errors и fresh local SQLite checkout/payment/subscription/VPN smoke пройдены.
- EF drift, secret scan, strict UTF-8 guards и artifact cleanup пройдены.
- `RoadmapCurrentStateTests` фиксирует `560/580` closed, readiness `96.6%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.546.0 - 2026-08-09

Release entry: `2026-08-09-admin-restored-session-refresh`.

### Исправлено

- Восстановленная admin-сессия выполняет одну admission-проверку под `React.StrictMode`; access-token `401` один раз ротирует сохранённый refresh-token вместо преждевременного logout.
- Новая пара токенов сохраняется до capability-проверки, поэтому transient `5xx` после автоматического или ручного refresh допускает retry без потери уже ротированной сессии.
- Session operation generation блокирует поздние admission/refresh/data ответы после logout или нового входа; rejected refresh и потеря административной роли завершают сессию fail-closed.
- Logout и transient cleanup удаляют audit rows, фильтры, support drafts, provider/inbound forms и VPN migration targets, исключая stale state после следующей авторизации.

### Улучшено

- До подтверждения административных полномочий показывается отдельный recovery-экран с loading, retry и logout; форма нового входа и приватные разделы в этот момент не отображаются.

### Проверено

- Frontend `110/110`, typecheck/build всех приложений и dependency audit `0 vulnerabilities`.
- Admin desktop/mobile `20/20`; полный desktop/mobile console-responsive Playwright suite `54/54` покрывает single admission/refresh, transient retry, rejected refresh, delayed logout completion и private-state cleanup.
- Backend `1112/1112`, Release build `0` warnings/`0` errors и fresh local SQLite checkout/payment/subscription/VPN smoke пройдены.
- Secret scan, strict UTF-8 guards и artifact cleanup пройдены.
- `RoadmapCurrentStateTests` фиксирует `559/579` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остаётся `staging-ready baseline`, not production-ready.

## 0.545.0 - 2026-08-09

Release entry: `2026-08-09-cabinet-restored-session-refresh`.

### Исправлено

- Кабинет при восстановлении сохранённой сессии сначала пробует загрузить данные, а после access-token `401` ровно один раз ротирует действующий refresh-token вместо немедленного удаления обоих токенов.
- Transient bootstrap-ошибка сохраняет текущую или уже ротированную пару токенов и оставляет явную повторную загрузку; rejected refresh `401/403` завершает сессию fail-closed.
- Session operation generation отбрасывает поздние refresh/profile ответы после logout или новой авторизации и не позволяет вернуть приватные данные или browser storage.

### Улучшено

- До подтверждения профиля кабинет показывает отдельное состояние восстановления с loading, retry и logout, не выводит пустой VPN-доступ и не сообщает ложный вход как «пользователь».

### Проверено

- Frontend `109/109`, typecheck/build всех приложений и dependency audit `0 vulnerabilities`.
- Cabinet desktop/mobile `10/10`; полный desktop/mobile console-responsive Playwright suite `40/40` покрывает single refresh, transient `503`, rejected refresh и delayed completion после logout.
- Backend `1112/1112`, Release build `0` warnings/`0` errors и fresh local SQLite checkout/payment/subscription/VPN smoke пройдены.
- Secret scan, strict UTF-8 guards и artifact cleanup пройдены.
- `RoadmapCurrentStateTests` фиксирует `558/578` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остаётся `staging-ready baseline`, not production-ready.

## 0.544.0 - 2026-08-09

Release entry: `2026-08-09-public-session-refresh-single-flight`.

### Исправлено

- Восстановление публичной сессии выполняет один `GET /api/me` и не запускает конкурентные refresh-token rotation под `React.StrictMode`.
- Только `401` от access-token запускает refresh; transient `5xx` сохраняет локальные токены и предлагает ручную повторную проверку без нового входа.
- Новая пара токенов сохраняется сразу после успешной ротации, а request generation отбрасывает поздний refresh/profile response после logout или новой авторизации.
- Pending checkout ждёт подтверждённый профиль и не отправляет claim по просроченному восстановленному access-token.

### Улучшено

- Пока восстановленная сессия не подтверждена, account-page показывает отдельные loading/error/retry/logout состояния и не отображает форму нового входа поверх сохранённых токенов.

### Проверено

- Public session unit regression `2/2`; frontend `109/109`, typecheck/build всех приложений и dependency audit `0 vulnerabilities`.
- Desktop/mobile E2E фиксирует один old-token profile request, один refresh и один refreshed-token profile request под StrictMode.
- Controlled `503` даёт `profile=1/refresh=0`, сохраняет оба browser token и после ручной команды загружает профиль без ротации; delayed refresh после logout не восстанавливает UI или storage.
- Полный desktop/mobile console-responsive Playwright suite `32/32`; backend `1112/1112`, Release build `0` warnings/`0` errors и fresh local SQLite smoke пройдены.
- Secret scan `653/0`, strict UTF-8 guards и artifact cleanup пройдены.
- `RoadmapCurrentStateTests` фиксирует `557/577` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.543.0 - 2026-08-09

Release entry: `2026-08-09-public-checkout-storage-validation`.

### Исправлено

- Восстановление pending checkout больше не доверяет произвольному JSON из `sessionStorage`: проверяются структура, размеры, формат checkout token и payment-provider allow-list.
- Повреждённое или подменённое состояние удаляется до запуска авторизованных claim/payment-init запросов и не отображается как сохранённая покупка.

### Проверено

- Новый parser unit suite проверяет valid restore, нормализацию tariff name, invalid JSON/object/array, пустые и oversized значения, malformed token и provider path injection.
- Desktop/mobile E2E подтверждает удаление подменённого checkout, отсутствие stale UI, `checkout=0`, `claim=0`, `payment-init=0` и отсутствие `pageerror`.
- Frontend `107/107`, typecheck/build всех приложений; полный desktop/mobile console-responsive Playwright suite `26/26`.
- Backend `1112/1112`; fresh local SQLite smoke прошёл checkout, payment, subscription и VPN access; Release build `0` warnings/`0` errors.
- Dependency audit `0 vulnerabilities`, secret scan `651/0` и strict UTF-8 guards пройдены.
- `RoadmapCurrentStateTests` фиксирует `556/576` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.542.0 - 2026-08-09

Release entry: `2026-08-09-public-checkout-single-flight`.

### Исправлено

- Авторизованная покупка больше не запускает checkout claim и payment init одновременно из страницы тарифов и родительского effect.
- Ошибка payment init не создает автоматический цикл повторов при сбросе busy или ротации access token; новая попытка выполняется только по команде пользователя.
- После успешного claim account-card сохраняет ID заказа и повторяет только payment init; поздний provider response после logout не восстанавливает ссылку старой сессии.

### Проверено

- Frontend `104/104`, typecheck/build всех приложений; полный desktop/mobile console-responsive Playwright suite `24/24`.
- E2E подтверждает authenticated checkout `checkout=1`, `claim=1`, `payment-init=1`; после контролируемого `503` ручной retry сохраняет `checkout=1`, `claim=1`, `payment-init=2`.
- Delayed payment-init после logout не восстанавливает order/payment UI или browser tokens; CMS-тексты результата и CTA сохранены в account-card.
- Backend `1112/1112`, Release build `0` warnings/`0` errors; backend order gate и payment idempotency остаются дополнительной защитой, но browser больше не отправляет конкурентные дубли.
- Fresh local SQLite smoke подтвердил latest release `2026-08-09-public-checkout-single-flight`; dependency audit `0 vulnerabilities`, secret scan `649/0` и strict UTF-8 guards пройдены.
- `RoadmapCurrentStateTests` фиксирует `555/575` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.541.0 - 2026-08-09

Release entry: `2026-08-09-cabinet-renewal-partial-success`.

### Исправлено

- Кабинет больше не теряет уже созданный заказ на продление, если платежный провайдер не смог подготовить ссылку оплаты.
- Частичный успех показывается явно с ID заказа и отдельной командой повторной подготовки оплаты.
- Повтор использует тот же `orderId` и не создает второй заказ на продление.

### Проверено

- Frontend `104/104`, typecheck/build всех приложений; полный desktop/mobile console-responsive Playwright suite `20/20`.
- E2E подтверждает один POST создания заказа, контролируемый `503` первой payment-init попытки и успешный второй payment-init по тому же заказу.
- Backend `1112/1112`, Release build `0` warnings/`0` errors; идемпотентный pending renewal intent подтвержден существующим service/SQLite контрактом.
- Fresh local SQLite smoke подтвердил latest release `2026-08-09-cabinet-renewal-partial-success`; dependency audit `0 vulnerabilities`, secret scan `649/0` и strict UTF-8 guards пройдены.
- `RoadmapCurrentStateTests` фиксирует `554/574` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.540.0 - 2026-08-09

Release entry: `2026-08-09-vpn-migration-capacity-ui-sync`.

### Исправлено

- Карточки 3x-ui панелей больше не показывают stale `UsedCapacity` после успешного межпанельного переноса VPN-клиента.
- Source capacity уменьшается, destination capacity увеличивается из подтверждённого migration result; same-panel migration сохраняет общий счётчик.
- Обновление не добавляет fallible fetch после выполненного side effect и поэтому не превращает успешный перенос в ложную ошибку UI.

### Проверено

- Frontend `104/104`, typecheck/build всех приложений; admin Playwright `3/3`, полный desktop/mobile console-responsive suite `20/20`.
- E2E подтверждает `EU 12 -> 11` и `US 4 -> 5` сразу после переноса и после последующего refresh через panel health action.
- Backend `1112/1112`, Release build `0` warnings/`0` errors, fresh local SQLite smoke, EF model drift, dependency audit `0 vulnerabilities`, secret scan `649/0` и strict UTF-8 guards пройдены.
- `RoadmapCurrentStateTests` фиксирует `553/573` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.539.0 - 2026-08-09

Release entry: `2026-08-09-cross-panel-client-migration-ui`.

### Исправлено

- Админка больше не ограничивает перенос VPN-клиента inbound-ами выбранной панели: новый `GET /api/admin/vpn-inbounds` возвращает общий read-only каталог.
- API-клиент fail-closed проверяет уникальные internal ID, составной `panel + externalInboundId` и единственный default inbound в границах каждой панели.
- Цели переноса сгруппированы по активным и не `Unhealthy` панелям с доступной capacity и совместимым протоколом; после успеха UI автоматически открывает destination panel.

### Проверено

- Backend full suite `1112/1112`; Release build `0` warnings/`0` errors; контроллерный SQLite test подтверждает агрегат нескольких панелей.
- Frontend `104/104`, typecheck/build всех трех приложений; admin Playwright `3/3`, полный desktop/mobile console-responsive suite `20/20`.
- E2E подтверждает выбор `inbound-us`, POST body, смену панели клиента, destination UI и последующие health/sync операции; fresh local SQLite smoke подтверждает latest release.
- Dependency audit `0 vulnerabilities`; secret scan `649` files/`0` findings; release/documentation/strict UTF-8 guards пройдены.
- `RoadmapCurrentStateTests` фиксирует `552/572` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Real VPS/staging/live payment/production-like 3x-ui и SMTP evidence остаются внешними; статус остается `staging-ready baseline`, not production-ready.

## 0.538.0 - 2026-08-09

Release entry: `2026-08-09-subscription-migration-execution`.

### Исправлено

- Межсерверная миграция подписки больше не создает задание, которое навсегда остается `Planned`: endpoint выбирает совместимые node/panel/inbound, запускает существующий компенсируемый перенос 3x-ui и завершает `MigrationJob`/`MigrationItem` как `Completed` либо `Failed` с audit trail.
- Общая миграция VPN-клиента согласованно обновляет `Subscription.CurrentServerId`, текущий `AccessCredential.ServerId`, provider ID, URI и счетчики source/target; целевая capacity node/panel/inbound резервируется транзакционно и освобождается во всех компенсируемых ветках.
- Admin UI выполняет перенос после подтверждения и показывает фактически выбранный сервер и завершенный job; typed API принимает auto-selected target только из проверенного completed-response.

### Проверено

- Backend `1112/1112`, targeted X3Ui/admin SQLite suite `77/77`; исполняемый boundary подтверждает client/inbound, subscription/access, job/audit и capacity `source=0`, `target=1`; Release build `0` warnings/`0` errors, EF model drift отсутствует.
- Frontend `104/104`, typecheck/build трех приложений, dependency audit `0 vulnerabilities`, полный Playwright console/responsive suite `20/20`; fresh local SQLite purchase/subscription/VPN smoke подтверждает latest release.
- Secret scan `649` files/`0` findings; release/documentation/strict UTF-8 guards `57/57`.
- Roadmap progress: `551/571` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.537.0 - 2026-08-09

Release entry: `2026-08-09-admin-subscription-access-action-api-dto-validation`.

### Исправлено

- Все десять административных команд lifecycle подписки и VPN-доступа проверяют route-specific success DTO до передачи результата в React: обязательные даты, статусы, nullable-поля, revision и соответствие route ID.
- Fallback-ветки enable/disable backend теперь возвращают тот же полный `AdminAccessActionResult`, что и основной lifecycle service, вместо сокращенного анонимного объекта без revision/sync-полей.
- Межсерверная миграция подписки больше не скрыта за backend API: оператор с `vpnManage` может выбрать готовый VPN-узел или автоматическое распределение, подтвердить команду и получить номер planned migration job.

### Проверено

- Frontend `104/104`, включая malformed DTO всех 11 subscription/access/migration операций; typecheck и production build всех трех приложений.
- Admin desktop/mobile `6/6`, включая планирование миграции, RBAC, confirmation и отсутствие horizontal overflow; полный console/responsive suite `20/20`.
- Backend full suite `1112/1112`, targeted subscription/access/admin boundary `44/44`, Release build `0` warnings/`0` errors, EF model drift отсутствует; fresh local SQLite checkout/payment/subscription/VPN smoke пройден.
- Dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings; release/documentation/UTF-8 guards подтверждают latest `2026-08-09-admin-subscription-access-action-api-dto-validation`, версию `0.537.0` и roadmap `550/570`.
- Roadmap progress: `550/570` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.536.0 - 2026-08-09

Release entry: `2026-08-09-auth-checkout-action-api-dto-validation`.

### Исправлено

- Auth, checkout, payment-init, cabinet support status и Telegram link-token success-ответы больше не проходят в UI без проверки обязательных полей и backend semantics.
- `createMyOrder` и checkout claim теперь принимают фактический минимальный `OrderDto` backend вместо ошибочно ожидаемой расширенной list-проекции; реальное продление больше не зависит от полей, которых mutation endpoint не возвращает.
- Удалены два неиспользуемых frontend client-метода anonymous order/payment к endpoint-ам, которые backend намеренно завершает `410 Gone`.
- Payment redirect проверяется как credential-free absolute `http/https`, checkout token/status/nullable-связи сверяются с маршрутом, Telegram deep link обязан вести на `https://t.me` с соответствующим одноразовым token.

### Проверено

- Frontend `103/103`, включая malformed DTO всех 13 активных critical-flow операций; typecheck и production build всех трех приложений.
- Public critical-flow regression desktop/mobile `2/2`, public/cabinet focused matrix `8/8`; полный console/responsive suite `20/20`.
- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует; fresh local SQLite checkout/payment/subscription/VPN smoke пройден.
- Dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings; release/documentation/UTF-8 guards подтверждают latest `2026-08-09-auth-checkout-action-api-dto-validation`, версию `0.536.0` и roadmap `549/569`.
- Roadmap progress: `549/569` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

## 0.535.0 - 2026-08-09

Release entry: `2026-08-09-admin-infrastructure-api-dto-validation`.

### Исправлено

- API-клиент больше не принимает неполные DTO VPN-серверов, node health, provisioning run/detail/command и Telegram bot settings/readiness как доверенные данные.
- Все 22 read и mutation маршрута проверяют обязательные поля, даты, enum, JSON, capacity, mode/risk/live-deploy связи, уникальные `id` и соответствие `nodeId/runId` фактическому backend-контракту.
- Поврежденная инфраструктурная загрузка очищает старые серверы, запуски provisioning, форму редактирования исчезнувшего сервера и устаревший результат Telegram connection check; поздний check отбрасывается по поколению запроса.

### Проверено

- Frontend `102/102`, включая malformed server/provisioning/Telegram DTO; typecheck и production build всех трех приложений.
- Admin/all-screens desktop/mobile `12/12` с malformed infrastructure regression; полный console/responsive suite `18/18`.
- Backend full suite `1112/1112`, Release build `0` warnings/`0` errors, EF model drift отсутствует; fresh local SQLite checkout/payment/subscription/VPN smoke пройден.
- Dependency audit `0 vulnerabilities`, secret scan `649` files/`0` findings; release/documentation/UTF-8 guards подтверждают latest `2026-08-09-admin-infrastructure-api-dto-validation`, версию `0.535.0` и roadmap `548/568`.
- Roadmap progress: `548/568` closed, readiness `96.5%`, `20` remaining, `19` open, `1` in progress и `0` blocked.
- Реальные VPS/staging/live payment/3x-ui и SMTP evidence остаются внешними и не закрывались локальными тестами; статус остается `staging-ready baseline`, не production-ready.

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
