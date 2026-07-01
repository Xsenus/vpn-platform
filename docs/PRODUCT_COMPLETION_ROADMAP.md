# Master roadmap: доведение VPN Platform до production-ready

Документ нужен как единая рабочая карта проекта. По нему агент или разработчик должен идти сверху вниз, отмечать выполненные пункты и оставлять доказательства: тесты, скриншоты, ссылки на коммиты, результаты smoke-проверок и замечания.

Дата актуализации: 2026-06-14.

Дата последней сверки: 2026-06-24.

Временный статус работы с roadmap: активная локальная доработка возобновлена для локальных safety-guard задач и синхронизирована до `2026-07-01-production-readiness-assertion-latest-release-guard`, версия `0.311.0`. Roadmap остается staging-ready baseline, не production-ready: закрыто `316/336` проверяемых пунктов, открыто `19`, в работе `1`, блокеров `[!]` нет. Дальше нельзя закрывать `STATE-011`, `STATE-012`, `STATE-013`, `P0-ADMIN-001`, `P0-ADMIN-002`, `P0-VPN-*`, `P0-PAY-*`, `P9-TST-007` и `P11-ACC-002` без реального VPS/staging/live evidence.

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

Что подтверждено на 2026-06-14:

- [x] `STATE-001` Backend test suite проходит: `596/596`.
- [x] `STATE-002` Frontend test suite проходит: `66/66`.
- [x] `STATE-003` TypeScript typecheck проходит для public-web, cabinet и admin-panel.
- [x] `STATE-004` Frontend production build проходит для public-web, cabinet и admin-panel.
- [x] `STATE-005` GitHub Actions `validation`, `staging-validation`, `deploy-vps` настроены; live deploy все еще требует реального прогона после push.
- [x] `STATE-006` Локальный SQLite smoke и VPS smoke runner проверяют health, admin login, checkout, payment init, sandbox webhook, подписку и VPN access; live VPS report еще нужен.
- [x] `STATE-007` Sandbox-покупка и sandbox-выдача VPN реализованы.
- [x] `STATE-008` Production и sandbox VPN-выдача разделены.
- [x] `STATE-009` Генерация VPN-ссылок поддерживает VLESS, VMess и Trojan.
- [x] `STATE-010` Полный mock-based browser E2E основных экранов завершен. 2026-06-14.
- [ ] `STATE-011` Live-платежи всех провайдеров не подтверждены.
- [ ] `STATE-012` Live-выдача через реальный 3x-ui не подтверждена.
- [ ] `STATE-013` Админка на VPS не проверена под рабочим admin-аккаунтом.
- [x] `STATE-014` Roadmap и текущие статусные документы синхронизированы с проверками 2026-06-14.
  - Что сделано: верхний статус roadmap, README, final runbook, release decision, changelog, TEST_RESULTS, product/admin UI roadmap и seed "Что нового" приведены к одному состоянию: backend `606/606`, frontend `66/66`, browser console smoke `9/9`, latest release `2026-07-01-production-readiness-assertion-latest-release-guard`, версия `0.311.0`.
  - Что осталось: live-платежи, реальная 3x-ui выдача, VPS admin/live smoke и production-ready решение остаются отдельными открытыми задачами `STATE-011`, `STATE-012`, `STATE-013`, `P11-ACC-002` и P0.
  - Доказательство: `RoadmapCurrentStateTests` 2/2, `BugRegisterConsistencyTests` 2/2, `ProvisioningSecretStatusConsistencyTests` 1/1, `ProductAdminUiRoadmapSyncTests` 1/1, `ProductionReadinessGateTests` 64/64, `VpsProductionSmokeTests` 8/8, `StagingSmokeChecklistTests` 9/9, `PaymentProviderSmokeReportTests` 7/7, `AdminVpsSmokeReportTests` 16/16, `AdminBootstrapCliScriptTests` 12/12, `VpnLiveSmokeReportTests` 5/5, `ChannelWebhooksControllerTests` 2/2, `ReadmeDocumentationTests`, `FinalDocsChangelogTests`, `DocumentationEncodingTests`, local SQLite VPS smoke dry-run, fresh local SQLite smoke, local admin browser smoke через end-to-end wrapper, local CLI bootstrap admin smoke, production readiness assertion result latest release guard regression, production readiness summary latest release guard regression, production handoff CI summary latest release guard regression, production handoff CI result latest release guard regression, production handoff flow result latest release guard regression, production handoff package latest release guard regression, production handoff checklist latest release guard regression, admin VPS bootstrap smoke latest release guard regression, deploy production env normalizer regression, admin VPS smoke navigation fallback regression, admin VPS smoke remote release preflight/diagnostics/console-summary/remote-message/report-id-console/failed-checks/failed-count/check-counts regression, admin VPS smoke latest release guard regression, staging smoke latest release guard regression, payment provider smoke latest release guard regression, VPN live smoke latest release guard regression, VPS production smoke latest release guard regression, real VPS admin smoke negative evidence for stale release/missing `audit` section, backend full suite `606/606`, frontend tests `66/66`, latest "Что нового" `2026-07-01-production-readiness-assertion-latest-release-guard`.

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

- [x] `P0-ADMIN-001B` Добавить операторский wrapper для admin bootstrap/reset. 2026-06-19.
  - Что сделать: дать одну безопасную PowerShell-команду для локального SQLite и production/Postgres reset, чтобы оператор не собирал env-переменные вручную и не печатал пароль в лог.
  - Что сделано: добавлен `scripts/admin-bootstrap.ps1` с `-LocalSqlite`, `-ApplyMigrations`, `-DryRun`, скрытием пароля в выводе и запуском `admin-bootstrap` без HTTP-сервера; добавлена инструкция `docs/admin-bootstrap.md`.
  - Доказательство: `AdminBootstrapCliScriptTests`, dry-run без записи в БД, локальный SQLite admin bootstrap/reset, latest "Что нового" `2026-06-19-admin-bootstrap-wrapper`, версия `0.182.0`. Реальный VPS login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001C` Связать admin bootstrap/reset с admin smoke в безопасный wrapper. 2026-06-19.
  - Что сделано: добавлен `scripts/admin-vps-bootstrap-smoke.ps1`, который запускает `scripts/admin-bootstrap.ps1`, требует `-ConfirmBootstrapReset` для не-локальной БД, берет пароль только из `ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD`, передает его в smoke через process env и затем запускает `scripts/admin-vps-smoke.ps1 -AccountBootstrapChecked`; добавлен `scripts/local-admin-vps-bootstrap-smoke.ps1`, который доказывает flow на временной SQLite-БД с `AdminBootstrap__Enabled=false` после CLI bootstrap.
  - Доказательство: `AdminBootstrapCliScriptTests` 5/5, local CLI bootstrap admin smoke через SQLite `1/1`, latest "Что нового" `2026-06-19-admin-vps-bootstrap-smoke-wrapper`, версия `0.193.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001D` Добавить regression harness для admin bootstrap+smoke wrapper. 2026-06-19.
  - Что сделано: добавлен `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1`, который проверяет fail-closed сценарии `missing-password`, `missing-confirm-bootstrap-reset`, `missing-connection-string` и `dry-run-no-smoke`, убеждается, что browser smoke не стартует, smoke/preflight artifacts не создаются и пароль не попадает в stdout/stderr.
  - Доказательство: `AdminBootstrapCliScriptTests` 6/6, admin VPS bootstrap smoke wrapper regression, local CLI bootstrap admin smoke через SQLite `1/1`, latest "Что нового" `2026-06-19-admin-vps-bootstrap-smoke-wrapper-regression`, версия `0.194.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001E` Добавить sanitized report для admin bootstrap+smoke wrapper. 2026-06-19.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` после успешного smoke пишет `admin-vps-bootstrap-smoke-report.json` без пароля, cookie и auth headers; `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` проверяет reset flags, локальность/подтверждение, дату, URL, absence of secret markers и связку preflight/smoke через `validate-admin-vps-smoke-evidence.ps1`.
  - Доказательство: `AdminBootstrapCliScriptTests` 7/7, local CLI bootstrap admin smoke через SQLite `1/1` с bootstrap smoke report validator, latest "Что нового" `2026-06-19-admin-vps-bootstrap-smoke-report`, версия `0.195.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001F` Добавить readiness gate перед admin bootstrap+smoke wrapper. 2026-06-19.
  - Что сделано: добавлены `scripts/admin-vps-bootstrap-smoke-readiness.ps1`, `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` и `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1`; основной wrapper запускает readiness gate до `admin-bootstrap.ps1`, пишет sanitized `admin-vps-bootstrap-smoke-readiness-report.json` без пароля и connection string, требует `-ConfirmBootstrapReset` и connection string для non-local БД.
  - Доказательство: `AdminBootstrapCliScriptTests` 8/8, admin VPS bootstrap smoke readiness regression, local CLI bootstrap admin smoke через SQLite `1/1` с readiness/bootstrap/smoke validators, latest "Что нового" `2026-06-19-admin-vps-bootstrap-smoke-readiness`, версия `0.196.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001G` Связать readiness и итоговый bootstrap+smoke report validator-ом. 2026-06-19.
  - Что сделано: добавлены `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` и `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1`; основной wrapper после успешного bootstrap smoke валидирует пару readiness/bootstrap reports, сверяет URL, admin email, environment, operator, provider, report paths, readiness flags и порядок дат.
  - Доказательство: `AdminBootstrapCliScriptTests` 9/9, admin VPS bootstrap smoke evidence validator regression, local CLI bootstrap admin smoke через SQLite `1/1` с readiness/bootstrap/evidence validators, latest "Что нового" `2026-06-19-admin-vps-bootstrap-smoke-evidence`, версия `0.197.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001H` Закрепить route contract в bootstrap smoke evidence regression. 2026-06-19.
  - Что сделано: `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` исправлен под актуальный route contract `/admin/#<id>` и получил fail-closed tamper-сценарий `bad-smoke-route`, который проверяет, что bootstrap evidence chain не принимает smoke report с устаревшими маршрутами.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `bad-smoke-route`, `AdminBootstrapCliScriptTests|AdminVpsSmokeReportTests` 24/24, latest "Что нового" `2026-06-19-admin-vps-bootstrap-smoke-route-regression`, версия `0.200.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001I` Закрепить release id в bootstrap smoke evidence chain. 2026-06-19.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` вычисляет latest release один раз и передает общий `releaseValue` в readiness gate, admin VPS smoke и итоговый bootstrap smoke report; `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed сверяет `releaseId` readiness/bootstrap reports; regression harness покрывает `mismatched-release-id` и dry-run readiness release id.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-release-id`, admin VPS bootstrap smoke wrapper regression с непустым dry-run readiness release id, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-19-admin-vps-bootstrap-smoke-release-id-chain`, версия `0.203.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001J` Связать release id итогового bootstrap report с preflight/smoke evidence. 2026-06-19.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` после успешной проверки preflight/smoke evidence сверяет `releaseId` итогового bootstrap smoke report с `releaseId` preflight и smoke reports; regression harness покрывает `mismatched-smoke-release-id`, где preflight/smoke согласованы между собой, но не совпадают с bootstrap report.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-smoke-release-id`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-19-admin-vps-bootstrap-smoke-report-release-link`, версия `0.204.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001K` Связать readinessReportPath с фактическим readiness evidence. 2026-06-20.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь сверяет `readinessReportPath` внутри readiness report с фактическим readiness JSON, переданным в validator; regression harness покрывает `mismatched-readiness-report-path`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-readiness-report-path`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-20-admin-vps-bootstrap-smoke-readiness-path-link`, версия `0.205.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.


- [x] `P0-ADMIN-001L` Связать итоговый bootstrap smoke report с readiness evidence path. 2026-06-22.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` пишет `readinessReportPath` в итоговый bootstrap smoke report; `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` требует это поле, а `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` сверяет его с фактическим readiness JSON.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-bootstrap-readiness-report-path`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-readiness-report-link`, версия `0.207.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001M` Связать итоговый bootstrap smoke report с собственным evidence path. 2026-06-22.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` пишет `bootstrapSmokeReportPath` в итоговый bootstrap smoke report; `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` требует это поле, а `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` сверяет его с фактическим bootstrap JSON.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-bootstrap-smoke-report-path`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-smoke-report-self-link`, версия `0.208.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001N` Связать admin email итогового bootstrap report с smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` сверяет `adminEmail` итогового bootstrap smoke report с preflight и browser smoke reports; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` пишет `smokeReportPath` в synthetic smoke report и покрывает `mismatched-bootstrap-admin-email`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-bootstrap-admin-email`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-smoke-admin-email-link`, версия `0.211.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001O` Связать окружение итогового bootstrap report с smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` сверяет `apiBaseUrl`, `adminWebUrl`, `environmentName` и `operator` итогового bootstrap smoke report с preflight и browser smoke reports; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `mismatched-bootstrap-environment`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-bootstrap-environment`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-smoke-environment-link`, версия `0.212.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001P` Валидировать self-link итогового bootstrap report standalone validator-ом. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` сверяет `bootstrapSmokeReportPath` с фактическим `-ReportPath` до paired evidence validation; regression harness ожидает standalone failure для `mismatched-bootstrap-smoke-report-path`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-bootstrap-smoke-report-path`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-smoke-report-self-validate`, версия `0.213.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001Q` Валидировать self-link readiness report standalone validator-ом. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -RequireReady` сверяет `readinessReportPath` с фактическим `-ReportPath`; `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` покрывает `mismatched-readiness-report-self-link`.
  - Доказательство: admin VPS bootstrap smoke readiness regression с `mismatched-readiness-report-self-link`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-readiness-self-validate`, версия `0.214.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001R` Валидировать readiness chain standalone bootstrap report validator-ом. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` валидирует связанный readiness report и сверяет readiness evidence с итоговым bootstrap smoke report по URL, окружению, оператору, `adminEmail`, `releaseId` и `readiness.bootstrapSmokeReportPath`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `missing-bootstrap-readiness-report-link` и `mismatched-readiness-bootstrap-report-path`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-readiness-chain-validate`, версия `0.215.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001S` Связать readiness metadata с итоговым bootstrap smoke report. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` сверяет `provider`, `passwordEnvName`, `localSqlite` и `confirmBootstrapReset` итогового bootstrap smoke report с readiness evidence.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-readiness-provider`, `mismatched-readiness-password-env-name`, `mismatched-readiness-local-sqlite` и `mismatched-readiness-confirm-bootstrap-reset`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-readiness-metadata-link`, версия `0.216.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001T` Зафиксировать порядок smoke completion -> bootstrap report в bootstrap smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` требует, чтобы `generatedAt` итогового bootstrap smoke report не был раньше linked smoke `completedAt`; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `bootstrap-generated-before-smoke-completed`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `bootstrap-generated-before-smoke-completed`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-smoke-timing-link`, версия `0.218.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001U` Связать readiness smoke/preflight paths с итоговым bootstrap smoke report. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` сверяет readiness `smokeReportPath` и `preflightReportPath` с одноименными путями итогового bootstrap smoke report; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `mismatched-readiness-smoke-report-path` и `mismatched-readiness-preflight-report-path`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `mismatched-readiness-smoke-report-path` и `mismatched-readiness-preflight-report-path`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-readiness-smoke-path-link`, версия `0.219.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001V` Зафиксировать порядок readiness -> preflight в bootstrap smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` требует, чтобы linked preflight `generatedAt` не был раньше readiness `generatedAt`; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `preflight-generated-before-readiness`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `preflight-generated-before-readiness`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-readiness-preflight-timing-link`, версия `0.220.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001W` Добавить preflight path в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `preflightReportPath` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет это в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `preflightReportPath` в valid summary, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-preflight-summary`, версия `0.221.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001X` Добавить sections contract path в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `sectionsContractPath` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет это поле в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `sectionsContractPath` в valid summary, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-sections-summary`, версия `0.224.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001Y` Добавить admin identity в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `apiBaseUrl`, `adminWebUrl` и `adminEmail` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет эти поля в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `apiBaseUrl`, `adminWebUrl` и `adminEmail` в valid summary, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-identity-summary`, версия `0.225.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001Z` Добавить operator в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `operator` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет это поле в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `operator` в valid summary, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-operator-summary`, версия `0.226.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AA` Добавить status gates в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `readyForBootstrapSmoke` и `bootstrapStatus` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет эти поля в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `readyForBootstrapSmoke` и `bootstrapStatus` в valid summary, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-status-summary`, версия `0.227.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AB` Добавить reset flags в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `passwordEnvPresent`, `confirmBootstrapReset` и `bootstrapResetConfirmed` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет эти поля в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с reset flags в valid summary, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-reset-flags-summary`, версия `0.228.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AC` Добавить readiness inputs в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `passwordEnvName`, `passwordLengthOk`, `connectionStringPresent` и `applyMigrations` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет эти поля в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с readiness inputs в valid summary, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-readiness-inputs-summary`, версия `0.229.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AD` Добавить report timing в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `readinessReportId`, `bootstrapSmokeReportId`, `readinessGeneratedAt`, `bootstrapGeneratedAt` и `bootstrapCompletedAt` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет эти поля в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с report id и timing fields в valid summary, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-timing-summary`, версия `0.230.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AE` Добавить smoke details в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `preflightReportId`, `smokeReportId`, `preflightGeneratedAt`, `smokeStartedAt`, `smokeCompletedAt` и счетчики `sections`/`passed`/`failed`/`blocked`/`skipped` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет эти поля в valid-сценарии; smoke/report wrappers сортируют latest release через `DateTimeOffset.Parse`, чтобы timestamp с миллисекундами выбирался корректно.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression со smoke summary fields, local CLI bootstrap admin smoke на SQLite с latest release `2026-06-22-admin-vps-bootstrap-evidence-smoke-summary`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-smoke-summary`, версия `0.231.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AF` Добавить duration metrics в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `preflightToSmokeSeconds`, `smokeDurationSeconds`, `bootstrapDurationSeconds` и `readinessToBootstrapSeconds` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет duration metrics и ожидаемые значения synthetic valid-сценария.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с duration metrics, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-duration-summary`, версия `0.232.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AG` Добавить SHA256 fingerprints в bootstrap smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет `readinessReportSha256`, `bootstrapSmokeReportSha256`, `preflightReportSha256` и `smokeReportSha256` в sanitized success summary; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` и `AdminBootstrapCliScriptTests` проверяют наличие fingerprints в valid-сценарии.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с SHA256 fingerprints, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-fingerprint-summary`, версия `0.233.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AH` Добавить expected SHA256 guard для bootstrap smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` принимает `ExpectedReadinessReportSha256`, `ExpectedBootstrapSmokeReportSha256`, `ExpectedPreflightReportSha256` и `ExpectedSmokeReportSha256`, проверяет формат SHA256 и fail-closed отклоняет bundle при несовпадении.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с `valid-expected-sha256` и `mismatched-expected-readiness-sha256`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-expected-fingerprint`, версия `0.234.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AI` Закрепить report id prefixes в bootstrap smoke evidence chain. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed требует уникальные report ids readiness/bootstrap/preflight/smoke и ожидаемые префиксы `admin-vps-bootstrap-smoke-readiness-`, `admin-vps-bootstrap-smoke-`, `admin-vps-smoke-preflight-`, `admin-vps-smoke-`; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `duplicate-report-id`, `bad-readiness-report-id-prefix`, `bad-bootstrap-report-id-prefix`, `bad-preflight-report-id-prefix` и `bad-smoke-report-id-prefix`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с report id prefix checks, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-report-id-prefix`, версия `0.244.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AJ` Закрепить timestamp format в bootstrap smoke evidence report ids. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed требует формат `yyyyMMdd-HHmmss` после ожидаемых префиксов readiness/bootstrap/preflight/smoke report ids; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` переведен на timestamp ids в valid fixtures и покрывает `bad-readiness-report-id-timestamp`, `bad-bootstrap-report-id-timestamp`, `bad-preflight-report-id-timestamp` и `bad-smoke-report-id-timestamp`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с report id timestamp checks, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp`, версия `0.245.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AK` Связать report id timestamp с датами bootstrap smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed сверяет timestamp-суффиксы readiness/bootstrap/preflight/smoke report ids с `generatedAt`/`startedAt` соответствующих reports; `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `mismatched-readiness-report-id-timestamp`, `mismatched-bootstrap-report-id-timestamp`, `mismatched-preflight-report-id-timestamp` и `mismatched-smoke-report-id-timestamp`.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с report id timestamp link checks, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp-link`, версия `0.248.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001AL` Добавить chronology summary для bootstrap smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет в sanitized success summary `evidenceChronology`, `readinessToPreflightSeconds`, `smokeToBootstrapSeconds` и `evidenceChainDurationSeconds`, чтобы оператор видел полный порядок readiness/preflight/smoke/bootstrap evidence без секретов.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression с chronology summary fields, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-bootstrap-evidence-chronology-summary`, версия `0.249.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AM` Протянуть max duration guard через bootstrap smoke wrapper. 2026-06-22.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` принимает `MaxEvidenceChainMinutes`, показывает примененный лимит без секретов, передает его в `scripts/admin-vps-smoke.ps1` и в `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1`.
  - Доказательство: admin VPS bootstrap smoke wrapper regression со сценарием `bad-max-evidence-chain-minutes`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-admin-vps-smoke-wrapper-max-duration`, версия `0.252.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AN` Протянуть max duration guard через локальный bootstrap smoke. 2026-06-22.
  - Что сделано: `scripts/local-admin-vps-bootstrap-smoke.ps1` принимает `MaxEvidenceChainMinutes`, fail-fast отклоняет неположительный лимит до запуска API/admin web и передает его в `scripts/admin-vps-bootstrap-smoke.ps1`.
  - Доказательство: local CLI bootstrap admin smoke на SQLite с `MaxEvidenceChainMinutes=120`, `AdminBootstrapCliScriptTests` 9/9, latest "Что нового" `2026-06-22-local-admin-bootstrap-smoke-max-duration`, версия `0.253.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AO` Добавить regression harness для локального bootstrap smoke wrapper. 2026-06-22.
  - Что сделано: добавлен `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1`, который проверяет `bad-max-evidence-chain-minutes` и убеждается, что локальный wrapper не запускает API/admin web, browser smoke и не создает `tmp/local-admin-vps-bootstrap-smoke` artifacts.
  - Доказательство: local admin VPS bootstrap smoke wrapper regression, `AdminBootstrapCliScriptTests` 10/10, latest "Что нового" `2026-06-22-local-admin-bootstrap-smoke-wrapper-regression`, версия `0.254.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AP` Поддержать env max duration guard в локальном bootstrap smoke. 2026-06-22.
  - Что сделано: `scripts/local-admin-vps-bootstrap-smoke.ps1` берет default `MaxEvidenceChainMinutes` из `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES`, а regression harness проверяет `bad-env-max-evidence-chain-minutes`.
  - Доказательство: local admin VPS bootstrap smoke wrapper regression с CLI/env max duration scenarios, local CLI bootstrap admin smoke на SQLite, `AdminBootstrapCliScriptTests` 10/10, latest "Что нового" `2026-06-22-local-admin-bootstrap-smoke-env-max-duration`, версия `0.255.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AQ` Поддержать env max duration guard в production bootstrap smoke wrapper regression. 2026-06-22.
  - Что сделано: `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` проверяет `bad-env-max-evidence-chain-minutes` для `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES` и очищает env-лимит для остальных сценариев, чтобы CLI/env проверки не влияли друг на друга.
  - Доказательство: admin VPS bootstrap smoke wrapper regression с CLI/env max duration scenarios, latest "Что нового" `2026-06-22-admin-vps-bootstrap-smoke-env-max-duration`, версия `0.256.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [ ] `P0-ADMIN-002` Проверить все разделы админки под реальным admin-аккаунтом.
  - Что сделать: открыть dashboard, users, payments, tariffs, subscriptions, vpn, nodes, panels, support, bot, releases, faq, content, scenarios, provisioning.
  - Критерий готовности: нет белого экрана, JS-ошибок, 401/403 после логина, сломанных таблиц и пустых обязательных состояний без объяснения.
  - Доказательство: browser smoke-отчет, список найденных ошибок или отметка "ошибок нет".

- [x] `P0-ADMIN-002A` Добавить безопасный browser runner для admin VPS smoke. 2026-06-19.
  - Что сделано: добавлен `frontend/e2e/admin-vps-smoke.spec.ts`, отдельный `frontend/playwright.vps-smoke.config.ts` без trace/video/screenshot artifacts и wrapper `scripts/admin-vps-browser-smoke.ps1`, который берет пароль только из `ADMIN_VPS_SMOKE_ADMIN_PASSWORD` и пишет sanitized JSON report.
  - Доказательство: `AdminVpsSmokeReportTests` 5/5, Playwright test discovery для проекта `admin-vps-smoke`, latest "Что нового" `2026-06-19-admin-vps-browser-smoke`, версия `0.183.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002B` Проверить admin browser smoke runner на локальной SQLite-БД. 2026-06-19.
  - Что сделано: добавлен `scripts/local-admin-vps-browser-smoke.ps1`, который поднимает временную SQLite-БД, API и admin-panel, выполняет `admin-vps-browser-smoke.ps1 -RequireAllPassed`, валидирует report и останавливает дерево процессов.
  - Доказательство: `AdminVpsSmokeReportTests` 6/6, local SQLite admin browser smoke `1/1`, report validator `16 passed`, latest "Что нового" `2026-06-19-local-admin-vps-browser-smoke`, версия `0.184.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002C` Усилить acceptance gate для admin VPS smoke evidence. 2026-06-19.
  - Что сделано: `validate-admin-vps-smoke-report.ps1 -RequireAllPassed` требует успешный `httpStatus` по каждой секции и отклоняет placeholder evidence вроде `TODO`, `Not checked yet`, `safe screenshot name` и шаблонных browser smoke notes.
  - Доказательство: `AdminVpsSmokeReportTests` 7/7, local SQLite admin browser smoke `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-acceptance-evidence`, версия `0.185.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002D` Добавить preflight перед admin VPS smoke. 2026-06-19.
  - Что сделано: `scripts/admin-vps-smoke-preflight.ps1` проверяет live URL, admin email, наличие `ADMIN_VPS_SMOKE_ADMIN_PASSWORD` без вывода секрета, frontend runner, npm command и validator, затем пишет sanitized preflight report с `readyForLiveSmoke`.
  - Доказательство: `AdminVpsSmokeReportTests` 8/8, локальный preflight на тестовых URL и env-пароле, local SQLite admin browser smoke `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-preflight`, версия `0.186.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002E` Добавить validator для admin VPS smoke preflight report. 2026-06-19.
  - Что сделано: `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady` проверяет sanitized preflight JSON, обязательные checks, URL, email, readiness flags и forbidden secret markers; `admin-vps-smoke-preflight.ps1` запускает validator перед разрешением live smoke.
  - Доказательство: `AdminVpsSmokeReportTests` 9/9, локальный preflight validator на тестовых URL и env-пароле, local SQLite admin browser smoke `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-preflight-validator`, версия `0.187.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002F` Добавить regression harness для admin VPS smoke preflight validator. 2026-06-19.
  - Что сделано: `scripts/test-admin-vps-smoke-preflight-validator.ps1` создает валидный preflight report, проверяет validator happy path и ожидаемые ошибки для `bad-ready-flag`, `failed-check`, `missing-check`, `duplicate-check` и `secret-marker`, не сохраняя пароль в artifacts.
  - Доказательство: `AdminVpsSmokeReportTests` 10/10, локальный preflight validator regression smoke, local SQLite admin browser smoke `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-preflight-validator-regression`, версия `0.188.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002G` Добавить regression harness для admin VPS smoke report validator. 2026-06-19.
  - Что сделано: `scripts/test-admin-vps-smoke-report-validator.ps1` строит synthetic passed admin VPS smoke report и проверяет fail-closed ошибки для `bad-http-status`, `placeholder-evidence`, `failed-status`, `missing-section`, `false-gate` и `secret-marker`.
  - Доказательство: `AdminVpsSmokeReportTests` 11/11, локальный admin VPS smoke report validator regression smoke, local SQLite admin browser smoke `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-report-validator-regression`, версия `0.189.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002H` Добавить единый end-to-end wrapper для admin VPS smoke. 2026-06-19.
  - Что сделано: `scripts/admin-vps-smoke.ps1` запускает fail-closed preflight с `-RequirePassword`, валидирует preflight report, затем выполняет `scripts/admin-vps-browser-smoke.ps1 -RequireAllPassed`; `scripts/local-admin-vps-browser-smoke.ps1` проверяет этот flow на временной SQLite-БД.
  - Доказательство: `AdminVpsSmokeReportTests` 12/12, local SQLite admin browser smoke через `admin-vps-smoke.ps1` `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-flow-wrapper`, версия `0.190.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002I` Добавить regression harness для admin VPS smoke flow wrapper. 2026-06-19.
  - Что сделано: `scripts/test-admin-vps-smoke-flow-wrapper.ps1` проверяет fail-closed сценарии `missing-password`, `bad-api-url` и `missing-frontend`, убеждается, что browser smoke не стартует до valid preflight, smoke report не создается, а пароль не попадает в stdout/stderr.
  - Доказательство: `AdminVpsSmokeReportTests` 13/13, admin VPS smoke flow wrapper regression, local SQLite admin browser smoke через `admin-vps-smoke.ps1` `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-flow-wrapper-regression`, версия `0.191.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002J` Добавить парный evidence validator для admin VPS smoke. 2026-06-19.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` валидирует `preflight` с `-RequireReady`, smoke report с `-RequireAllPassed` и сверяет `apiBaseUrl`, `adminWebUrl`, `environmentName`, `operator`, `smokeReportPath`, непустой `releaseId` и порядок дат; `scripts/admin-vps-smoke.ps1` запускает этот validator после browser smoke.
  - Доказательство: `AdminVpsSmokeReportTests` 14/14, admin VPS smoke evidence validator regression, local SQLite admin browser smoke через `admin-vps-smoke.ps1` `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-evidence-validator`, версия `0.192.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002K` Зафиксировать единый contract обязательных admin sections для VPS smoke. 2026-06-19.
  - Что сделано: добавлен `docs/admin-vps-smoke-sections.json`; `frontend/e2e/admin-vps-smoke.spec.ts` берет id/route разделов из manifest; `scripts/validate-admin-vps-smoke-sections-contract.ps1` сверяет manifest, report template, report validator, VPS Playwright smoke и all-screens smoke; `scripts/test-admin-vps-smoke-sections-contract.ps1` покрывает fail-closed tamper-сценарии.
  - Доказательство: `AdminVpsSmokeReportTests` 15/15, admin VPS smoke sections contract regression, local CLI bootstrap admin smoke через SQLite `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-sections-contract`, версия `0.198.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002L` Связать admin VPS smoke report validator с route contract разделов. 2026-06-19.
  - Что сделано: `scripts/validate-admin-vps-smoke-report.ps1` читает `docs/admin-vps-smoke-sections.json`, берет обязательные section id/route из manifest и отклоняет report, если route раздела расходится с contract; `scripts/test-admin-vps-smoke-report-validator.ps1` покрывает tamper-сценарий `bad-route`.
  - Доказательство: `AdminVpsSmokeReportTests` 15/15, admin VPS smoke report validator regression с `bad-route`, sections contract validator/regression, local CLI bootstrap admin smoke через SQLite `1/1`, latest "Что нового" `2026-06-19-admin-vps-smoke-report-route-contract`, версия `0.199.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002M` Закрепить release id в admin VPS smoke preflight/evidence. 2026-06-19.
  - Что сделано: `scripts/admin-vps-smoke-preflight.ps1` подставляет latest release, если `-ReleaseId` не передан; `scripts/validate-admin-vps-smoke-preflight-report.ps1` отклоняет пустой `releaseId`; `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed проверяет release id пары preflight/smoke.
  - Доказательство: admin VPS smoke preflight validator regression с `empty-release-id`, admin VPS smoke evidence validator regression с `missing-preflight-release-id`, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-19-admin-vps-smoke-preflight-release-id`, версия `0.201.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002N` Зафиксировать единый release id для admin VPS smoke flow. 2026-06-19.
  - Что сделано: `scripts/admin-vps-smoke.ps1` вычисляет latest release один раз и передает общий `releaseValue` в preflight и browser smoke; `scripts/admin-vps-browser-smoke.ps1` имеет PowerShell fallback на latest release; `scripts/test-admin-vps-smoke-flow-wrapper.ps1` проверяет непустой release id в fail-closed preflight reports.
  - Доказательство: admin VPS smoke flow wrapper regression с `missing-password`, `bad-api-url`, `missing-frontend` и непустым release id, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-19-admin-vps-smoke-unified-release-id`, версия `0.202.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002O` Связать admin email в preflight и browser smoke evidence. 2026-06-20.
  - Что сделано: `docs/admin-vps-smoke-report.template.json`, `frontend/e2e/admin-vps-smoke.spec.ts` и `scripts/new-admin-vps-smoke-report.ps1` пишут sanitized `adminEmail` в smoke report; `scripts/validate-admin-vps-smoke-report.ps1` требует email-поле, а `scripts/validate-admin-vps-smoke-evidence.ps1` сверяет его с preflight report.
  - Доказательство: admin VPS smoke evidence validator regression с `mismatched-admin-email`, admin VPS smoke report validator regression, `AdminVpsSmokeReportTests|AdminBootstrapCliScriptTests` 24/24, latest "Что нового" `2026-06-20-admin-vps-smoke-admin-email-evidence`, версия `0.206.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002P` Связать preflight report с собственным evidence path. 2026-06-22.
  - Что сделано: `scripts/admin-vps-smoke-preflight.ps1` пишет `preflightReportPath` в preflight report; `scripts/validate-admin-vps-smoke-preflight-report.ps1` требует это поле, а `scripts/validate-admin-vps-smoke-evidence.ps1` сверяет его с фактическим preflight JSON.
  - Доказательство: admin VPS smoke evidence validator regression с `mismatched-preflight-report-path`, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-preflight-self-link`, версия `0.209.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002Q` Связать browser smoke report с собственным evidence path. 2026-06-22.
  - Что сделано: `frontend/e2e/admin-vps-smoke.spec.ts`, `docs/admin-vps-smoke-report.template.json` и `scripts/new-admin-vps-smoke-report.ps1` пишут `smokeReportPath` в browser smoke report; `scripts/validate-admin-vps-smoke-report.ps1` требует это поле и сверяет его с фактическим smoke JSON.
  - Доказательство: admin VPS smoke report validator regression с `mismatched-smoke-report-path`, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-report-self-link`, версия `0.210.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002R` Зафиксировать порядок preflight -> smoke start в admin VPS smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` требует, чтобы smoke `startedAt` не был раньше preflight `generatedAt`; `scripts/test-admin-vps-smoke-evidence-validator.ps1` пишет `smokeReportPath` в synthetic smoke fixture и покрывает `smoke-started-before-preflight`.
  - Доказательство: admin VPS smoke evidence validator regression с `smoke-started-before-preflight`, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-timing-link`, версия `0.217.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002S` Добавить preflight path в admin VPS smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `preflightReportPath` в sanitized success summary; `scripts/test-admin-vps-smoke-evidence-validator.ps1` захватывает PowerShell information stream и проверяет это поле в valid output.
  - Доказательство: admin VPS smoke evidence validator regression с `preflightReportPath` в valid summary, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-preflight-summary`, версия `0.222.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002T` Добавить sections contract path в admin VPS smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `sectionsContractPath` в sanitized success summary; `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет это поле в captured valid output.
  - Доказательство: admin VPS smoke evidence validator regression с `sectionsContractPath` в valid summary, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-sections-summary`, версия `0.223.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002U` Добавить expected SHA256 guard для admin VPS smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `preflightReportSha256` и `smokeReportSha256` в sanitized success summary, принимает `ExpectedPreflightReportSha256`/`ExpectedSmokeReportSha256`, проверяет формат SHA256 и fail-closed отклоняет bundle при несовпадении.
  - Доказательство: admin VPS smoke evidence validator regression с valid expected SHA256 и `mismatched-expected-preflight-sha256`, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-expected-fingerprint`, версия `0.235.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002V` Добавить duration metrics в admin VPS smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `preflightGeneratedAt`, `smokeStartedAt`, `smokeCompletedAt`, `preflightToSmokeSeconds` и `smokeDurationSeconds` в sanitized success summary; `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет duration metrics и ожидаемые значения synthetic valid-сценария.
  - Доказательство: admin VPS smoke evidence validator regression с duration metrics, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-duration-summary`, версия `0.236.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002W` Запретить отрицательную smoke duration в admin VPS smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` дополнительно проверяет `smoke.completedAt >= smoke.startedAt`; `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарий `smoke-completed-before-started`, который fail-closed отклоняется до acceptance.
  - Доказательство: admin VPS smoke evidence validator regression с `smoke-completed-before-started`, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-duration-order`, версия `0.237.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002X` Добавить admin identity в admin VPS smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `adminEmail` и `operator` в sanitized success summary; `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет эти поля и значения synthetic valid-сценария.
  - Доказательство: admin VPS smoke evidence validator regression с identity summary, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-identity-summary`, версия `0.238.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002Y` Добавить report ids в admin VPS smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `preflightReportId` и `smokeReportId` в sanitized success summary; `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет эти поля и значения synthetic valid-сценария.
  - Доказательство: admin VPS smoke evidence validator regression с report id summary, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-report-id-summary`, версия `0.239.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002Z` Добавить status counters в admin VPS smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `passed`, `failed`, `blocked` и `skipped` в sanitized success summary рядом с `sections`; `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет счетчики synthetic valid-сценария.
  - Доказательство: admin VPS smoke evidence validator regression со status counts summary, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-status-counts-summary`, версия `0.240.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AA` Добавить gate flags в admin VPS smoke evidence summary. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `accountBootstrapChecked`, `adminLoginPassed`, `noJsErrors` и `noUnauthorizedAfterLogin` в sanitized success summary; `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет эти флаги в synthetic valid-сценарии.
  - Доказательство: admin VPS smoke evidence validator regression с gate flags summary, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-gate-flags-summary`, версия `0.241.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AB` Запретить одинаковые report ids в admin VPS smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed требует непустые и разные `preflight.reportId`/`smoke.reportId`; `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарий `duplicate-report-id`.
  - Доказательство: admin VPS smoke evidence validator regression с `duplicate-report-id`, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-report-id-uniqueness`, версия `0.242.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AC` Закрепить префиксы report ids в admin VPS smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed требует `preflight.reportId` с префиксом `admin-vps-smoke-preflight-` и `smoke.reportId` с префиксом `admin-vps-smoke-` без preflight-префикса; `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает `bad-preflight-report-id-prefix` и `bad-smoke-report-id-prefix`.
  - Доказательство: admin VPS smoke evidence validator regression с report id prefix checks, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-report-id-prefix`, версия `0.243.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AD` Закрепить timestamp format в admin VPS smoke evidence report ids. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed требует формат `yyyyMMdd-HHmmss` после ожидаемых префиксов `preflight.reportId`/`smoke.reportId`; `scripts/test-admin-vps-smoke-evidence-validator.ps1` переведен на timestamp ids в valid fixtures и покрывает `bad-preflight-report-id-timestamp` и `bad-smoke-report-id-timestamp`.
  - Доказательство: admin VPS smoke evidence validator regression с report id timestamp checks, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp`, версия `0.246.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AE` Связать report id timestamp с датами admin VPS smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed сверяет timestamp-суффикс `preflight.reportId` с `preflight.generatedAt`, а `smoke.reportId` с `smoke.startedAt`; `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает `mismatched-preflight-report-id-timestamp` и `mismatched-smoke-report-id-timestamp`.
  - Доказательство: admin VPS smoke evidence validator regression с report id timestamp link checks, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp-link`, версия `0.247.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AF` Добавить chronology summary для admin VPS smoke evidence. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет в sanitized success summary `evidenceChronology` и `evidenceChainDurationSeconds`, чтобы оператор видел порядок preflight/smoke evidence и полную длительность проверки без секретов.
  - Доказательство: admin VPS smoke evidence validator regression с chronology summary fields, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-chronology-summary`, версия `0.250.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AG` Ограничить длительность admin VPS smoke evidence chain. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` и `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` получили fail-closed параметр `MaxEvidenceChainMinutes` и выводят `maxEvidenceChainMinutes` в sanitized success summary.
  - Доказательство: admin VPS smoke/bootstrap evidence validator regression со сценарием `evidence-chain-duration-exceeds-max`, `AdminVpsSmokeReportTests|AdminBootstrapCliScriptTests` 24/24, latest "Что нового" `2026-06-22-admin-vps-smoke-evidence-chain-max-duration`, версия `0.251.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AH` Протянуть max duration guard через admin VPS smoke wrapper. 2026-06-22.
  - Что сделано: `scripts/admin-vps-smoke.ps1` принимает `MaxEvidenceChainMinutes`, показывает примененный лимит без секретов и передает его в `scripts/validate-admin-vps-smoke-evidence.ps1`.
  - Доказательство: admin VPS smoke flow wrapper regression со сценарием `bad-max-evidence-chain-minutes`, `AdminVpsSmokeReportTests` 15/15, latest "Что нового" `2026-06-22-admin-vps-smoke-wrapper-max-duration`, версия `0.252.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AI` Поддержать env max duration guard в admin VPS smoke wrapper regression. 2026-06-22.
  - Что сделано: `scripts/admin-vps-smoke.ps1` явно fail-fast отклоняет неположительный `MaxEvidenceChainMinutes`, включая default из `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES`, до preflight/browser smoke; `scripts/test-admin-vps-smoke-flow-wrapper.ps1` проверяет `bad-env-max-evidence-chain-minutes`.
  - Доказательство: admin VPS smoke flow wrapper regression с CLI/env max duration scenarios, latest "Что нового" `2026-06-22-admin-vps-smoke-env-max-duration`, версия `0.257.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AJ` Закрепить явный max duration guard в admin VPS smoke wrapper. 2026-06-22.
  - Что сделано: `scripts/admin-vps-smoke.ps1` явно fail-fast отклоняет CLI/env `MaxEvidenceChainMinutes <= 0` и `> 1440` до preflight/browser smoke artifacts; regression проверяет точные сообщения и `too-high-max-evidence-chain-minutes`.
  - Доказательство: admin VPS smoke flow wrapper regression с CLI/env/upper-bound max duration guard scenarios, latest "Что нового" `2026-06-22-admin-vps-smoke-explicit-max-duration-guard`, версия `0.259.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AK` Закрепить явный max duration guard в admin VPS smoke evidence validator. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-smoke-evidence.ps1` больше не зависит от PowerShell `ValidateRange` и сам отклоняет `MaxEvidenceChainMinutes <= 0` и `> 1440` до чтения preflight/smoke evidence.
  - Доказательство: admin VPS smoke evidence validator regression со сценариями `bad-max-evidence-chain-minutes` и `too-high-max-evidence-chain-minutes`, latest "Что нового" `2026-06-22-admin-vps-evidence-explicit-max-duration-guard`, версия `0.262.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AL` Закрепить format guard для admin VPS smoke max duration. 2026-06-23.
  - Что сделано: `scripts/admin-vps-smoke.ps1` и `scripts/validate-admin-vps-smoke-evidence.ps1` явно отклоняют нечисловой `MaxEvidenceChainMinutes` с сообщением `MaxEvidenceChainMinutes must be an integer.` до preflight/browser smoke и чтения evidence reports.
  - Доказательство: admin VPS smoke flow/evidence validator regressions со сценариями `format-max-evidence-chain-minutes` и `format-env-max-evidence-chain-minutes`, latest "Что нового" `2026-06-23-admin-vps-max-duration-format-guard`, версия `0.263.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AM` Закрепить URL guard для admin VPS smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-smoke.ps1` fail-fast валидирует `ApiBaseUrl` и `AdminWebUrl` как absolute http/https URL до preflight/browser smoke и smoke artifacts.
  - Доказательство: admin VPS smoke flow wrapper regression со сценариями `bad-api-url` и `bad-admin-web-url`, latest "Что нового" `2026-06-23-admin-vps-url-guard`, версия `0.265.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AN` Закрепить email guard для admin VPS smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-smoke.ps1` fail-fast валидирует `AdminEmail` до preflight/browser smoke и не создает preflight report для невалидного email.
  - Доказательство: admin VPS smoke flow wrapper regression со сценарием `bad-admin-email`, latest "Что нового" `2026-06-23-admin-vps-email-guard`, версия `0.266.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AO` Закрепить report path guard для admin VPS smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-smoke.ps1` fail-fast требует разные `SmokeReportPath` и `PreflightReportPath` до preflight/browser smoke и smoke artifacts.
  - Доказательство: admin VPS smoke flow wrapper regression со сценарием `same-report-paths`, latest "Что нового" `2026-06-23-admin-vps-report-path-guard`, версия `0.267.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AP` Закрепить operator default для admin VPS smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-smoke.ps1` вычисляет единый `operatorValue`, использует `manual-operator` при пустом `Operator` и передает то же значение в preflight/browser smoke reports.
  - Доказательство: admin VPS smoke flow wrapper regression со сценарием `default-operator-missing-password`, latest "Что нового" `2026-06-23-admin-vps-operator-default`, версия `0.268.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AQ` Закрепить environment default для admin VPS smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-smoke.ps1` вычисляет единый `environmentNameValue`, использует `staging` при пустом `EnvironmentName` и передает то же значение в preflight/browser smoke reports.
  - Доказательство: admin VPS smoke flow wrapper regression со сценарием `default-environment-missing-password`, latest "Что нового" `2026-06-23-admin-vps-environment-default`, версия `0.269.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AR` Закрепить known release guard для admin VPS smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-smoke.ps1` fail-fast отклоняет ручной `ReleaseId`, которого нет в `backend/src/VpnPlatform.Api/AppReleases/releases.json`, до preflight/browser smoke artifacts.
  - Доказательство: admin VPS smoke flow wrapper regression со сценарием `unknown-release-id`, latest "Что нового" `2026-06-23-admin-vps-release-id-known-guard`, версия `0.270.0`. Реальный VPS smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AS` Normalize identity values in admin VPS smoke wrapper. 2026-06-23.
  - Done: `scripts/admin-vps-smoke.ps1` now trims `ApiBaseUrl`, `AdminWebUrl` and `AdminEmail` before console output, preflight args and browser smoke args.
  - Evidence: admin VPS smoke flow wrapper regression with `preflight-identity-values-normalized`, latest "What's New" `2026-06-23-admin-vps-smoke-wrapper-identity-normalization`, version `0.280.0`. Real VPS smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002AT` Support legacy admin navigation in VPS smoke. 2026-06-24.
  - Done: `frontend/e2e/admin-vps-smoke.spec.ts` opens required admin sections through `role=tab`, `role=link` or direct hash fallback from `docs/admin-vps-smoke-sections.json`, so old deployed admin UIs no longer mask missing sections as a generic timeout.
  - Evidence: `AdminVpsSmokeReportTests`, real VPS admin smoke attempt authenticated and passed sections through `support`, then failed explicitly on missing required `audit` section; latest "Что нового" `2026-06-24-admin-vps-smoke-navigation-fallback`, версия `0.290.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until a full passed VPS smoke report is captured after deploy.
- [x] `P0-ADMIN-002AU` Require remote release match before admin VPS smoke. 2026-06-24.
  - Done: `scripts/admin-vps-smoke.ps1` passes `-RequireRemoteReleaseMatch` to preflight; `scripts/admin-vps-smoke-preflight.ps1` logs in through `/api/auth/login`, keeps the bearer token in memory, checks `/api/app-version/latest`, writes sanitized `remoteReleaseId`/`remoteReleaseMatched` fields and fails `remote-latest-release` before browser smoke when VPS is stale.
  - Evidence: admin VPS smoke flow wrapper regression with remote-release-mismatch, preflight validator regression with remote-latest-release, local SQLite admin VPS browser smoke, latest release 2026-06-24-admin-vps-smoke-remote-release-preflight, version 0.291.0. STATE-013/P0-ADMIN-001/P0-ADMIN-002 remain open until the latest commits are deployed and a full passed VPS smoke report is captured.
- [x] `P0-ADMIN-002AV` Add sanitized remote release diagnostics to admin VPS smoke preflight. 2026-06-24.
  - Done: scripts/admin-vps-smoke-preflight.ps1 writes remoteReleaseStatus and remoteReleaseMessage for not-required, matched, mismatch and unavailable outcomes; scripts/validate-admin-vps-smoke-preflight-report.ps1 rejects tampered matched reports whose remoteReleaseId differs from local releaseId.
  - Evidence: preflight validator regression with valid stale release evidence and bad-remote-release-status, wrapper regression with remoteReleaseStatus=unavailable before browser smoke, local SQLite admin VPS browser smoke, latest release 2026-06-24-admin-vps-smoke-remote-release-diagnostics, version 0.292.0. STATE-013/P0-ADMIN-001/P0-ADMIN-002 remain open until the latest commits are deployed and a full passed VPS smoke report is captured.
- [x] `P0-ADMIN-002AW` Print remote release diagnostics in admin VPS smoke preflight console output. 2026-06-24.
  - Done: scripts/admin-vps-smoke-preflight.ps1 prints Remote release status, Remote release expected and Remote release actual without credentials, cookies or bearer tokens.
  - Evidence: AdminVpsSmokeReportTests static guard, admin VPS smoke flow wrapper regression asserting console status for remote-release-mismatch, local SQLite admin VPS browser smoke, latest release 2026-06-24-admin-vps-smoke-remote-release-console-summary, version 0.293.0. STATE-013/P0-ADMIN-001/P0-ADMIN-002 remain open until the latest commits are deployed and a full passed VPS smoke report is captured.
- [x] `P0-ADMIN-002AX` Add sanitized failed checks summary to admin VPS smoke preflight. 2026-06-24.
  - Done: scripts/admin-vps-smoke-preflight.ps1 writes failedChecks and prints Failed checks using only controlled check names; scripts/validate-admin-vps-smoke-preflight-report.ps1 verifies failedChecks exactly matches failed checks and keeps readyForLiveSmoke consistent.
  - Evidence: AdminVpsSmokeReportTests static guard, preflight validator regression with mismatched-failed-checks, admin VPS smoke flow wrapper regression asserting failedChecks/report/stdout, local SQLite admin VPS browser smoke, latest release 2026-06-24-admin-vps-smoke-preflight-failed-checks, version 0.294.0. STATE-013/P0-ADMIN-001/P0-ADMIN-002 remain open until the latest commits are deployed and a full passed VPS smoke report is captured.
- [x] `P0-ADMIN-002AY` Add sanitized failed check count to admin VPS smoke preflight. 2026-06-24.
  - Done: scripts/admin-vps-smoke-preflight.ps1 writes failedCheckCount and prints Failed check count next to failedChecks; scripts/validate-admin-vps-smoke-preflight-report.ps1 rejects reports where failedCheckCount does not match failed checks.
  - Evidence: AdminVpsSmokeReportTests static guard, preflight validator regression with mismatched-failed-check-count, admin VPS smoke flow wrapper regression asserting failedCheckCount/report/stdout, local SQLite admin VPS browser smoke, latest release 2026-06-24-admin-vps-smoke-preflight-failed-count, version 0.295.0. STATE-013/P0-ADMIN-001/P0-ADMIN-002 remain open until the latest commits are deployed and a full passed VPS smoke report is captured.
- [x] `P0-ADMIN-002AZ` Add sanitized total and passed check counts to admin VPS smoke preflight. 2026-06-24.
  - Done: scripts/admin-vps-smoke-preflight.ps1 writes checkCount, passedCheckCount and failedCheckCount and prints Check count, Passed checks and Failed check count before browser smoke; scripts/validate-admin-vps-smoke-preflight-report.ps1 rejects mismatched total/passed/failed counts.
  - Evidence: AdminVpsSmokeReportTests static guard, preflight validator regression with mismatched-check-count and mismatched-passed-check-count, admin VPS smoke flow wrapper regression asserting count summary in report/stdout, local SQLite admin VPS browser smoke, latest release 2026-06-24-admin-vps-smoke-preflight-check-counts, version 0.296.0. STATE-013/P0-ADMIN-001/P0-ADMIN-002 remain open until the latest commits are deployed and a full passed VPS smoke report is captured.
- [x] `P0-ADMIN-002BA` Print remote release guidance message in admin VPS smoke preflight console output. 2026-06-24.
  - Done: scripts/admin-vps-smoke-preflight.ps1 prints Remote release message next to the sanitized remote release status and check count summary.
  - Evidence: AdminVpsSmokeReportTests static guard, admin VPS smoke flow wrapper regression asserting unavailable remote release guidance before browser smoke, local SQLite admin VPS browser smoke, latest release 2026-06-24-admin-vps-smoke-remote-message-console, version 0.297.0. STATE-013/P0-ADMIN-001/P0-ADMIN-002 remain open until the latest commits are deployed and a full passed VPS smoke report is captured.
- [x] `P0-ADMIN-002BB` Print preflight report id in admin VPS smoke preflight console output. 2026-06-24.
  - Done: scripts/admin-vps-smoke-preflight.ps1 prints Preflight report id next to the sanitized preflight summary so stdout can be correlated with the JSON report.
  - Evidence: AdminVpsSmokeReportTests static guard, admin VPS smoke flow wrapper regression asserting stdout report id matches preflight JSON reportId, local SQLite admin VPS browser smoke, latest release 2026-06-24-admin-vps-smoke-preflight-report-id-console, version 0.298.0. STATE-013/P0-ADMIN-001/P0-ADMIN-002 remain open until the latest commits are deployed and a full passed VPS smoke report is captured.
- [x] `P0-ADMIN-002BC` Add latest release guard to admin VPS smoke acceptance. 2026-07-01.
  - Done: `scripts/validate-admin-vps-smoke-report.ps1 -RequireAllPassed` now rejects reports whose `releaseId` does not match the latest active release in `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
  - Evidence: `AdminVpsSmokeReportTests` 16/16, `scripts/test-admin-vps-smoke-report-latest-release-guard.ps1`, admin VPS smoke latest release guard regression, backend full suite `598/598`, latest "Что нового" `2026-07-01-admin-vps-smoke-latest-release-guard`, version `0.303.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin smoke evidence is captured.
- [x] `P0-ADMIN-001AR` Закрепить явный env max duration guard в admin VPS bootstrap smoke wrapper. 2026-06-22.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` явно fail-fast отклоняет неположительный CLI/env `MaxEvidenceChainMinutes` до readiness, bootstrap reset и smoke artifacts; regression запрещает readiness artifact для этих fail-fast сценариев.
  - Доказательство: admin VPS bootstrap smoke wrapper regression с CLI/env max duration guard scenarios, latest "Что нового" `2026-06-22-admin-vps-bootstrap-smoke-env-guard`, версия `0.258.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AS` Закрепить явный max duration guard в локальном bootstrap smoke wrapper. 2026-06-22.
  - Что сделано: `scripts/local-admin-vps-bootstrap-smoke.ps1` fail-fast отклоняет `MaxEvidenceChainMinutes <= 0` и `> 1440` до проверки портов, запуска API/admin web, создания SQLite DB и smoke artifacts.
  - Доказательство: local admin VPS bootstrap smoke wrapper regression с CLI/env/upper-bound max duration guard scenarios, latest "Что нового" `2026-06-22-local-admin-bootstrap-smoke-explicit-max-duration-guard`, версия `0.260.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AT` Закрепить env upper-bound max duration guard в admin VPS bootstrap smoke wrapper. 2026-06-22.
  - Что сделано: `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает `too-high-env-max-evidence-chain-minutes`, а admin smoke/local bootstrap harness'ы зеркально проверяют `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES=1441` до preflight, readiness, local DB и smoke artifacts.
  - Доказательство: admin VPS bootstrap/smoke/local wrapper regressions с env upper-bound max duration guard scenarios, latest "Что нового" `2026-06-22-admin-vps-bootstrap-smoke-env-upper-bound-guard`, версия `0.261.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AU` Закрепить явный max duration guard в bootstrap evidence validator. 2026-06-22.
  - Что сделано: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` больше не зависит от PowerShell `ValidateRange` и сам отклоняет `MaxEvidenceChainMinutes <= 0` и `> 1440` до чтения evidence reports.
  - Доказательство: admin VPS bootstrap smoke evidence validator regression со сценариями `bad-max-evidence-chain-minutes` и `too-high-max-evidence-chain-minutes`, latest "Что нового" `2026-06-22-admin-vps-evidence-explicit-max-duration-guard`, версия `0.262.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AV` Закрепить format guard для bootstrap/local max duration. 2026-06-23.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1`, `scripts/local-admin-vps-bootstrap-smoke.ps1` и `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` явно отклоняют нечисловой `MaxEvidenceChainMinutes` из CLI/env до readiness, bootstrap reset, local SQLite DB и smoke artifacts.
  - Доказательство: admin VPS bootstrap/local wrapper и bootstrap evidence validator regressions со сценариями `format-max-evidence-chain-minutes`/`format-env-max-evidence-chain-minutes`, latest "Что нового" `2026-06-23-admin-vps-max-duration-format-guard`, версия `0.263.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AW` Закрепить port guard для local bootstrap smoke. 2026-06-23.
  - Что сделано: `scripts/local-admin-vps-bootstrap-smoke.ps1` валидирует `ApiPort` и `AdminPort` как TCP-порты 1..65535, требует разные значения и останавливается до локальной SQLite DB, API/admin web и smoke artifacts.
  - Доказательство: local admin VPS bootstrap smoke wrapper regression со сценариями `format-api-port`, `too-low-api-port`, `too-high-admin-port` и `same-api-admin-port`, latest "Что нового" `2026-06-23-local-admin-bootstrap-port-guard`, версия `0.264.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001AX` Закрепить URL guard для admin VPS bootstrap smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` fail-fast валидирует `ApiBaseUrl` и `AdminWebUrl` как absolute http/https URL до readiness, bootstrap reset, передачи пароля и smoke artifacts.
  - Доказательство: admin VPS bootstrap smoke wrapper regression со сценариями `bad-api-url` и `bad-admin-web-url`, latest "Что нового" `2026-06-23-admin-vps-url-guard`, версия `0.265.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AY` Закрепить email guard для admin VPS bootstrap smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` fail-fast валидирует `AdminEmail` до readiness, bootstrap reset, передачи пароля и smoke artifacts.
  - Доказательство: admin VPS bootstrap smoke wrapper regression со сценарием `bad-admin-email`, latest "Что нового" `2026-06-23-admin-vps-email-guard`, версия `0.266.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001AZ` Закрепить report path guard для admin VPS bootstrap smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` fail-fast требует разные smoke/preflight/readiness/bootstrap report paths до readiness, bootstrap reset, передачи пароля и smoke artifacts.
  - Доказательство: admin VPS bootstrap smoke wrapper regression со сценарием `same-report-paths`, latest "Что нового" `2026-06-23-admin-vps-report-path-guard`, версия `0.267.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BA` Закрепить operator default для admin VPS bootstrap smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` вычисляет единый `operatorValue`, использует `manual-operator` при пустом `Operator` и передает то же значение в readiness, smoke и итоговый bootstrap smoke report.
  - Доказательство: admin VPS bootstrap smoke wrapper regression со сценарием `dry-run-default-operator`, latest "Что нового" `2026-06-23-admin-vps-operator-default`, версия `0.268.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BB` Закрепить environment default для admin VPS bootstrap smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` вычисляет единый `environmentNameValue`, использует `Production` при пустом `EnvironmentName` и передает то же значение в readiness, smoke и итоговый bootstrap smoke report.
  - Доказательство: admin VPS bootstrap smoke wrapper regression со сценарием `dry-run-default-environment`, latest "Что нового" `2026-06-23-admin-vps-environment-default`, версия `0.269.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BC` Закрепить known release guard для admin VPS bootstrap smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` fail-fast отклоняет ручной `ReleaseId`, которого нет в `backend/src/VpnPlatform.Api/AppReleases/releases.json`, до readiness, bootstrap reset, передачи пароля в smoke и smoke artifacts.
  - Доказательство: admin VPS bootstrap smoke wrapper regression со сценарием `unknown-release-id`, latest "Что нового" `2026-06-23-admin-vps-release-id-known-guard`, версия `0.270.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BD` Закрепить provider guard для admin VPS bootstrap smoke wrapper. 2026-06-23.
  - Что сделано: `scripts/admin-vps-bootstrap-smoke.ps1` нормализует `-LocalSqlite` в `Sqlite` и fail-fast отклоняет неподдерживаемый non-local `Provider` до readiness, bootstrap reset, передачи пароля в smoke и smoke artifacts.
  - Доказательство: admin VPS bootstrap smoke wrapper regression со сценарием `bad-provider`, latest "Что нового" `2026-06-23-admin-vps-bootstrap-provider-guard`, версия `0.271.0`. Реальный VPS bootstrap/login smoke остается в `P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001BE` Lock ready flag consistency for admin VPS bootstrap readiness report. 2026-06-23.
  - Done: `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` now fail-closed requires `readyForBootstrapSmoke` to match the actual `checks` array, even when the standalone validator runs without `-RequireReady`.
  - Evidence: admin VPS bootstrap smoke readiness regression with `mismatched-readiness-ready-flag`, latest "What's New" `2026-06-23-admin-vps-readiness-ready-flag`, version `0.272.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BF` Lock provider mode consistency for admin VPS bootstrap readiness report. 2026-06-23.
  - Done: `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` now fail-closed requires `provider=Sqlite` when `localSqlite=true`.
  - Evidence: admin VPS bootstrap smoke readiness regression with `mismatched-readiness-local-provider`, latest "What's New" `2026-06-23-admin-vps-readiness-provider-mode`, version `0.273.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BG` Normalize provider casing for standalone admin VPS bootstrap readiness. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now canonicalizes case-insensitive `Postgres`/`Sqlite` provider values before writing the sanitized readiness report, while unsupported values still fail `provider-supported`.
  - Evidence: admin VPS bootstrap smoke readiness regression with `provider-case-normalized`, latest "What's New" `2026-06-23-admin-vps-readiness-provider-normalization`, version `0.274.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BH` Normalize empty environment for standalone admin VPS bootstrap readiness. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now writes `Production` when `EnvironmentName` is empty or whitespace, keeping readiness evidence identity non-empty.
  - Evidence: admin VPS bootstrap smoke readiness regression with `environment-default-normalized`, latest "What's New" `2026-06-23-admin-vps-readiness-environment-default`, version `0.275.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BI` Normalize admin email for standalone admin VPS bootstrap readiness. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now trims `AdminEmail` before validation output and sanitized readiness evidence, keeping admin identity canonical.
  - Evidence: admin VPS bootstrap smoke readiness regression with `admin-email-normalized`, latest "What's New" `2026-06-23-admin-vps-readiness-admin-email-normalization`, version `0.276.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BJ` Normalize URLs for standalone admin VPS bootstrap readiness. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now trims `ApiBaseUrl` and `AdminWebUrl` before validation output and sanitized readiness evidence, keeping endpoint identity canonical.
  - Evidence: admin VPS bootstrap smoke readiness regression with `url-values-normalized`, latest "What's New" `2026-06-23-admin-vps-readiness-url-normalization`, version `0.277.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BK` Normalize URLs in admin VPS bootstrap+smoke wrapper. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke.ps1` now trims `ApiBaseUrl` and `AdminWebUrl` before console output, readiness args, smoke args and sanitized bootstrap smoke evidence.
  - Evidence: admin VPS bootstrap smoke wrapper regression with `dry-run-url-values-normalized`, latest "What's New" `2026-06-23-admin-vps-bootstrap-wrapper-url-normalization`, version `0.278.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BL` Normalize admin email in admin VPS bootstrap+smoke wrapper. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke.ps1` now trims `AdminEmail` before console output, readiness args, admin bootstrap args, smoke args and sanitized bootstrap smoke evidence.
  - Evidence: admin VPS bootstrap smoke wrapper regression with `dry-run-admin-email-normalized`, latest "What's New" `2026-06-23-admin-vps-bootstrap-wrapper-admin-email-normalization`, version `0.279.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BM` Normalize password env name for admin VPS bootstrap smoke. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke-readiness.ps1` and `scripts/admin-vps-bootstrap-smoke.ps1` now trim `AdminPasswordEnvName` before password lookup, readiness args and sanitized bootstrap smoke evidence; `scripts/local-admin-vps-bootstrap-smoke.ps1` now checks local ports with a loopback `TcpListener` and stops only its own launched process trees through `taskkill.exe`.
  - Evidence: admin VPS bootstrap smoke readiness regression with `password-env-name-normalized`, wrapper regression with `dry-run-password-env-name-normalized`, local SQLite bootstrap smoke completed with exit code 0, latest "What's New" `2026-06-23-admin-vps-bootstrap-password-env-normalization`, version `0.281.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BN` Normalize report paths for admin VPS smoke evidence. 2026-06-23.
  - Done: `scripts/admin-vps-smoke.ps1`, `scripts/admin-vps-bootstrap-smoke-readiness.ps1` and `scripts/admin-vps-bootstrap-smoke.ps1` now trim report path parameters before distinct-path checks, child script args, validators and sanitized evidence links.
  - Evidence: admin VPS bootstrap readiness regression with `report-paths-normalized`, bootstrap wrapper regression with `dry-run-report-paths-normalized`, smoke flow wrapper regression with `same-report-paths-normalized`, latest "What's New" `2026-06-23-admin-vps-report-path-normalization`, version `0.282.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BO` Normalize workspace paths for admin VPS smoke wrappers. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke-readiness.ps1`, `scripts/admin-vps-bootstrap-smoke.ps1`, `scripts/admin-vps-smoke.ps1`, `scripts/admin-vps-smoke-preflight.ps1`, `scripts/admin-vps-browser-smoke.ps1` and `scripts/admin-bootstrap.ps1` trim `ProjectPath`, `FrontendPath` and local `DataProtectionKeyPath` edge whitespace before local path checks and downstream script invocation.
  - Evidence: readiness regression with `workspace-paths-normalized`, bootstrap wrapper regression with `dry-run-workspace-paths-normalized`, smoke flow wrapper regression with `preflight-workspace-path-normalized`, latest "What's New" `2026-06-23-admin-vps-workspace-path-normalization`, version `0.283.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BP` Normalize admin bootstrap profile fields in wrappers. 2026-06-23.
  - Done: `scripts/admin-bootstrap.ps1` trims `EnvironmentName`, `Email`, `DisplayName`, `RolesCsv` and `Provider` before process env setup and safe console output; `scripts/admin-vps-bootstrap-smoke.ps1` passes trimmed `DisplayName` and `RolesCsv` to bootstrap while leaving the password and connection string untouched.
  - Evidence: bootstrap wrapper regression with `dry-run-admin-bootstrap-profile-normalized`, latest "What's New" `2026-06-23-admin-bootstrap-profile-normalization`, version `0.284.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BQ` Normalize direct admin bootstrap provider values. 2026-06-23.
  - Done: `scripts/admin-bootstrap.ps1` canonicalizes case-insensitive `Postgres`/`Sqlite`, rejects unsupported provider values before process env setup and keeps `-LocalSqlite` forced to `Sqlite`.
  - Evidence: direct bootstrap wrapper regression with `provider-case-normalized`, `local-sqlite-overrides-provider` and `bad-provider`, latest "What's New" `2026-06-23-admin-bootstrap-provider-normalization`, version `0.285.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BR` Guard direct admin bootstrap non-local resets. 2026-06-23.
  - Done: `scripts/admin-bootstrap.ps1` requires `-ConfirmBootstrapReset` and a non-empty `ConnectionString` before direct non-local bootstrap/reset, while local SQLite remains unchanged.
  - Evidence: direct bootstrap wrapper regression with `missing-confirm-bootstrap-reset` and `missing-connection-string`, latest "What's New" `2026-06-23-admin-bootstrap-nonlocal-reset-guard`, version `0.286.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BS` Guard admin bootstrap password env names. 2026-06-23.
  - Done: `scripts/admin-vps-bootstrap-smoke.ps1` rejects unsafe `AdminPasswordEnvName` values before reading process env secrets, while standalone readiness records `password-env-name-safe` and does not read unsafe env names.
  - Evidence: admin VPS bootstrap smoke wrapper/readiness regressions with `bad-password-env-name`, latest "What's New" `2026-06-23-admin-bootstrap-password-env-name-guard`, version `0.287.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BT` Guard readiness report password env name validation. 2026-06-23.
  - Done: `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` rejects tampered reports whose `passwordEnvName` is not a safe env identifier containing `PASSWORD`, even if generated checks are manually left passed.
  - Evidence: admin VPS bootstrap smoke readiness regression with `mismatched-readiness-password-env-name-safe`, latest "What's New" `2026-06-23-admin-bootstrap-readiness-password-env-validator`, version `0.288.0`. Real VPS bootstrap/login smoke remains in `P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001BU` Add latest release guard to admin VPS bootstrap smoke acceptance. 2026-07-01.
  - Done: `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -RequireReady` and `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` now reject reports whose `releaseId` does not match the latest active release in `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
  - Evidence: `AdminBootstrapCliScriptTests` 12/12, `scripts/test-admin-vps-bootstrap-smoke-latest-release-guard.ps1`, admin VPS bootstrap smoke latest release guard regression, backend full suite `599/599`, latest "What's New" `2026-07-01-admin-vps-bootstrap-smoke-latest-release-guard`, version `0.304.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin bootstrap/smoke evidence is captured.
- [x] `P0-ADMIN-003` Добавить безопасный admin VPS smoke report. 2026-06-14.
  - Что сделать: зафиксировать шаблон, генератор и валидатор отчета для проверки `/admin/` на VPS под реальным admin-аккаунтом.
  - Критерий готовности: отчет содержит все обязательные разделы админки, URL API/admin валидируются как absolute http/https, `-RequireAllPassed` требует успешный логин, отсутствие JS/API ошибок и `passed` по каждому разделу.
  - Доказательство: `docs/admin-vps-smoke-report.template.json`, `scripts/new-admin-vps-smoke-report.ps1`, `scripts/validate-admin-vps-smoke-report.ps1`, `docs/admin-vps-smoke.md`, `AdminVpsSmokeReportTests` 4/4, generator smoke, expected fail-closed `-RequireAllPassed`.

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

- [x] `P0-VPN-006` Добавить безопасный VPN live smoke report. 2026-06-14.
  - Что сделать: зафиксировать шаблон, генератор и валидатор отчета для проверки реальной 3x-ui панели, inbound, VPN node, production order, webhook, подписки, VPN-клиента, URI/QR и fail-closed поведения.
  - Критерий готовности: отчет содержит все обязательные VPN checks, URL API/admin/3x-ui валидируются как absolute http/https, `-RequireAllPassed` требует все top-level gates и `passed` по каждому check.
  - Доказательство: `docs/vpn-live-smoke-report.template.json`, `scripts/new-vpn-live-smoke-report.ps1`, `scripts/validate-vpn-live-smoke-report.ps1`, `docs/vpn-live-smoke.md`, `VpnLiveSmokeReportTests` 4/4, generator smoke, expected fail-closed `-RequireAllPassed`.

- [x] `P0-VPN-007` VPN live smoke report latest release guard. 2026-07-01.
  - Что сделать: не принимать VPN live smoke report как финальный acceptance evidence, если отчет был заполнен для старого release.
  - Что сделано: `scripts/validate-vpn-live-smoke-report.ps1 -RequireAllPassed` сверяет `releaseId` отчета с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`; добавлен regression harness `scripts/test-vpn-live-smoke-report-latest-release-guard.ps1`, который доказывает fail-closed поведение на полностью passed отчете со stale `releaseId`.
  - Доказательство: `VpnLiveSmokeReportTests` 5/5, VPN live smoke latest release guard regression, latest "Что нового" `2026-07-01-vpn-live-smoke-latest-release-guard`, версия `0.301.0`. Реальные smoke по `P0-VPN-001` ... `P0-VPN-005` остаются открытыми до внешнего evidence.

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

- [x] `P0-PAY-010` Telegram Stars invoice flow. 2026-06-14.
  - Что сделать: реализовать или полностью проверить Telegram invoice, pre-checkout, successful payment update, выдачу подписки.
  - Критерий готовности: пользователь может купить тариф в Telegram Stars и получить VPN.
  - Что сделано: `TelegramStars` остается скрытым из web checkout, а в Telegram-боте становится доступен только после явного `ExtraSettingsJson.status = "invoice-flow"`. Режим `bot-only` fail-closed, admin check показывает `Unhealthy`; явный `invoice-flow` показывает `Healthy` и не требует web secret key. Telegram purchase flow продолжает проверять `sendInvoice`, `pre_checkout_query`, `successful_payment`, подписку и VPN access на SQLite.
  - Доказательство: `PaymentProviderConfigurationRulesTests`, `PaymentProviderContractTests`, `AdminAutomationMvpTests.Provider_Account_Check_Should_Enable_TelegramStars_When_Invoice_Flow_Is_Explicit`, `TelegramBotPurchaseFlowTests.Telegram_Stars_Purchase_Should_Create_Subscription_And_Vpn_Access_On_Sqlite`, targeted payment/Telegram suite `61/61`, backend full suite `495/495`, fresh local SQLite smoke, local SQLite VPS smoke dry-run, latest "Что нового" `2026-06-14-telegram-stars-invoice-gate`, версия `0.118.0`. Live BotFather/Telegram Stars smoke остается внешним production-блокером `STATE-011`.

- [x] `P0-PAY-011` Скрыть неподтвержденные способы оплаты от публичного сайта. 2026-06-10.
  - Что сделать: публичный API должен отдавать только enabled + ready providers.
  - Критерий готовности: пользователь не видит способ оплаты, который не пройдет checkout.
  - Доказательство: `PaymentProvidersPublicControllerTests`, local SQLite HTTP-smoke `/api/public/payments/providers`.

- [x] `P0-PAY-012` Добавить безопасную матрицу smoke-проверки платежных провайдеров. 2026-06-14.
  - Что сделать: зафиксировать единый отчет для YooKassa, RoboKassa, YooMoney, CloudPayments, TBankAcquiring, Prodamus, Stripe и PayPal; запретить секреты в evidence; оставить production gate fail-closed до реальных проверок.
  - Критерий готовности: шаблон содержит все web-провайдеры, валидатор проверяет обязательные поля и `-RequireAllPassed`, Telegram Stars вынесен в отдельный invoice flow.
  - Доказательство: `docs/payment-provider-smoke-report.template.json`, `scripts/validate-payment-provider-smoke-report.ps1`, `docs/payment-provider-smoke.md`, `PaymentProviderSmokeReportTests`, обычная валидация шаблона OK, `-RequireAllPassed` ожидаемо падает на blocked report.

- [x] `P0-PAY-013` Добавить генератор payment provider smoke report. 2026-06-14.
  - Что сделать: убрать ручное копирование JSON и дать оператору безопасный черновик отчета по всем web-провайдерам.
  - Критерий готовности: скрипт подставляет latest release, environment, operator и mode, выставляет все провайдеры в `blocked`, не перезаписывает файл без `-Force` и сразу запускает валидатор.
  - Доказательство: `scripts/new-payment-provider-smoke-report.ps1`, `PaymentProviderSmokeReportTests` 5/5, generator smoke на `tmp/generated-payment-provider-smoke-report.json`, expected fail-closed `-RequireAllPassed`.

- [x] `P0-PAY-014` Усилить приемку payment provider smoke report. 2026-06-19.
  - Что сделать: запретить закрывать live/sandbox smoke провайдера одним `status = passed`, если не подтверждены все обязательные этапы настройки, checkout, provider confirmation, webhook, subscription и refund.
  - Что сделано: `scripts/validate-payment-provider-smoke-report.ps1` при `-RequireAllPassed` теперь требует `true` для `accountConfigured`, `checkoutCreated`, `providerConfirmation`, `webhookProcessed`, `subscriptionActivated` и `refundChecked` у каждого web-провайдера; `docs/payment-provider-smoke.md` объясняет приемочные gates и внешний блокер refund.
  - Доказательство: `PaymentProviderSmokeReportTests` 6/6, expected fail-closed `-RequireAllPassed`, latest "Что нового" `2026-06-19-payment-provider-smoke-report-acceptance-gates`, версия `0.180.0`.

- [x] `P0-PAY-015` Payment provider smoke report latest release guard. 2026-07-01.
  - Что сделать: не принимать payment provider smoke report как финальный acceptance evidence, если отчет был заполнен для старого release.
  - Что сделано: `scripts/validate-payment-provider-smoke-report.ps1 -RequireAllPassed` сверяет `releaseId` отчета с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`; добавлен regression harness `scripts/test-payment-provider-smoke-report-latest-release-guard.ps1`, который доказывает fail-closed поведение на полностью passed отчете со stale `releaseId`.
  - Доказательство: `PaymentProviderSmokeReportTests` 7/7, payment provider smoke latest release guard regression, latest "Что нового" `2026-07-01-payment-smoke-latest-release-guard`, версия `0.300.0`. Реальные smoke по провайдерам `P0-PAY-002` ... `P0-PAY-009` остаются открытыми до внешнего evidence.

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

- [x] `P1-TG-005` Telegram webhook в основном API. 2026-06-14.
  - Что сделать: убрать `501` с `/api/channels/telegram/webhook`, обрабатывать Telegram update через основной backend, читать режим/секреты из настроек админки и не дублировать ответы при повторной доставке update.
  - Что сделано: `ChannelWebhooksController` читает raw body, проверяет enabled/mode/secret через `TelegramBotRuntimeSettingsService`, вызывает `TelegramBotService.ProcessUpdateAsync`, отвечает на `pre_checkout_query` и отправляет сообщение через `ITelegramInvoiceProvider`. `TelegramBotHttpClient` перенесен в Infrastructure и использует BotToken из БД-настроек админки или fallback из config.
  - Доказательство: `ChannelWebhooksControllerTests` 2/2, targeted Telegram/API suite 39/39, backend full suite `491/491`, fresh local SQLite smoke, local SQLite VPS smoke dry-run, latest "Что нового" `2026-06-14-api-telegram-webhook`, версия `0.116.0`.

- [x] `P1-TG-006` Граница ответственности Telegram webhook и standalone bot. 2026-06-14.
  - Что сделать: убрать дублирующий `/telegram/webhook` из standalone `VpnPlatform.TelegramBot`, чтобы Telegram webhook не жил в двух местах и не расходился с настройками админки.
  - Что сделано: `VpnPlatform.TelegramBot` оставлен для `TelegramLongPollingService`, `TelegramNotificationDispatcherService` и health endpoints; webhook закреплен за основным API `/api/channels/telegram/webhook`. Документация и production example указывают на основной API endpoint.
  - Доказательство: `TelegramBotProcessBoundaryTests` 2/2, targeted Telegram boundary/API suite 41/41, standalone TelegramBot build без предупреждений, backend full suite `493/493`, fresh local SQLite smoke, local SQLite VPS smoke dry-run, latest "Что нового" `2026-06-14-telegram-webhook-boundary`, версия `0.117.0`.

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

- [x] `P3-UX-005` Адаптивность. 2026-06-11.
  - Что сделать: проверить 1440, 1280, 1024, 768, 390 px.
  - Что сделано: общий UI-слой получил токены адаптивных отступов и явные правила для 1280, 1024, 768 и 390 px; админка, публичный сайт и кабинет получили отдельные mobile/tablet правила для навигации, карточек, тарифов, таблиц, поддержки и окна "Что нового".
  - Доказательство: frontend static guard проверяет обязательные breakpoint-правила и критичные responsive CSS-блоки; дополнительно пройдены frontend typecheck/tests/build, backend full suite и local SQLite HTTP-smoke latest release `2026-06-11-responsive-breakpoints`.

- [x] `P3-UX-006` Доступность. 2026-06-11.
  - Что сделать: keyboard navigation, focus states, contrast, aria-label для icon buttons.
  - Что сделано: общий UI-слой получил screen-reader-only live-region для копирования, `aria-describedby` для password/secret полей, `role=status` для бейджей, `role=dialog` для confirm-popover, усиленный focus ring и `prefers-reduced-motion`; окно "Что нового" в кабинете получает фокус при открытии, закрывается по Escape, возвращает фокус назад и описывает выбранный релиз; обращения в поддержку получили выбранное состояние через `aria-pressed`.
  - Доказательство: frontend UI/static tests, browser accessibility smoke на локальных public/cabinet/admin, frontend typecheck/build, backend full suite, local SQLite HTTP-smoke latest release `2026-06-11-accessibility-polish`.

- [x] `P3-UX-007` Проверка русской локализации. 2026-06-11.
  - Сделано: API-клиент переведен на русский fallback для пустых ошибок сервера; в админке локализованы пользовательские подписи платежных провайдеров, Telegram-бота, серверов, VPN-панелей, источников релизов и режимов выдачи; общие бейджи больше не показывают `agent/manual/auto/hybrid` как голые enum-значения.
  - Доказательство: frontend static guard на отсутствие `Failed to ...`, трех вопросительных знаков подряд и символа `U+FFFD` в пользовательских источниках, browser smoke на локальных public/cabinet/admin, frontend typecheck/build, backend full suite, local SQLite HTTP-smoke latest release `2026-06-11-russian-localization-check`.

## P4. Backend, доменная логика и надежность

- [x] `P4-BE-001` Финализировать state machines. 2026-06-11.
  - Что сделать: заказы, платежи, подписки, VPN-доступы, provisioning runs.
  - Что сделано: добавлен общий `StatusStateMachine` для `OrderStatus`, `PaymentStatus`, `SubscriptionStatus`, `AccessCredentialStatus` и `ProvisioningRunStatus`; правила подключены к `PaymentOrchestrator`, Telegram Stars successful payment flow, `SubscriptionService`, `VpnAccessLifecycleService`, X3-UI синхронизации access credentials, ручным админским действиям и `ProvisioningWorker`.
  - Критерий готовности: невозможные переходы запрещены, повторные webhook остаются идемпотентными, поздний cancelled-webhook после successful payment не откатывает платеж/заказ/подписку.
  - Доказательство: `StatusStateMachineTests`, `PaymentWebhookProcessingTests.YooKassa_Late_Cancelled_Webhook_Should_Not_Downgrade_Succeeded_Payment`, backend full suite 341/341, local SQLite HTTP-smoke latest release `2026-06-11-state-machine-guards`.

- [x] `P4-BE-002` Идемпотентность webhook. 2026-06-11.
  - Что сделать: повтор webhook не создает вторую подписку/второй VPN-доступ.
  - Что сделано: `PaymentOrchestrator` строит стабильный idempotency key для webhook без внешнего event id через `payload:<sha256>`; повторная доставка события определяется до изменения платежа/заказа/подписки.
  - Доказательство: `PaymentWebhookIdempotencyContractTests` проверяет повтор webhook и fallback payload hash для каждого `PaymentProvider`; targeted payment webhook tests 42/42; backend full suite 359/359; local SQLite HTTP-smoke latest release `2026-06-11-payment-webhook-idempotency`.

- [x] `P4-BE-003` Конкурентность оплаты. 2026-06-12.
  - Что сделать: два webhook/recheck одновременно не ломают order/subscription.
  - Что сделано: `PaymentOrchestrator` сериализует применение статуса платежа по заказу через `PaymentProcessingGate`, после входа в gate перечитывает свежий snapshot платежа из БД и безопасно выходит, если активация уже выполнена. Конкурентная вставка одного `PaymentWebhookEvent` теперь возвращает идемпотентный ответ вместо исключения уникального индекса. Sandbox-выбор VPN-ноды перенес сортировку по загрузке в память, чтобы локальный SQLite проходил активацию оплаты.
  - Доказательство: `PaymentConcurrencyTests` проверяет параллельный одинаковый webhook и параллельные webhook/recheck на SQLite; targeted payment tests 28/28; local SQLite HTTP-smoke latest release `2026-06-12-payment-concurrency-guard`.

- [x] `P4-BE-004` Renew/expire jobs. 2026-06-10.
  - Что сделать: продление, окончание, отключение клиента, уведомления.
  - Доказательство: `SubscriptionLifecycleExpiryTests`, `SandboxE2EScenariosMvpTests`, local SQLite API smoke.

- [x] `P4-BE-005` Audit log. 2026-06-12.
  - Что сделать: логировать admin actions, payment transitions, VPN provisioning, secret rotations.
  - Что сделано: добавлен `/api/admin/audit-logs` с фильтрами и раздел "Аудит" в админке; платежные provider account действия пишут безопасные audit-события, а ротация SecretKey/webhook secret фиксируется отдельным событием без раскрытия значений. `PaymentOrchestrator` пишет системный `payment.status.changed` при фактической смене статуса, существующие VPN provisioning/lifecycle audit-события доступны в общем журнале.
  - Доказательство: `AuditLogMvpTests` проверяет endpoint на SQLite, отсутствие утечек секретов и системный audit платежного recheck; frontend typecheck/tests/build; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-admin-audit-log`.

- [x] `P4-BE-006` Observability. 2026-06-12.
  - Что сделать: structured logs, correlation IDs, health details, metrics.
  - Что сделано: добавлен нормализованный `X-Correlation-Id` в ответах и logger scope, request observability middleware со структурным HTTP-логом, runtime-счетчики запросов, Prometheus endpoint `/metrics`, детальный `/health/live` и `/health/ready` с проверками БД, outbox, provisioning и VPN-нод.
  - Доказательство: `ObservabilityMvpTests` проверяет correlation header, Prometheus-метрики и readiness report на SQLite; backend full suite; local SQLite HTTP-smoke `/health/live`, `/health/ready`, `/metrics`; latest release `2026-06-12-observability-mvp`.

## P5. База данных и миграции

- [x] `P5-DB-001` Полный аудит PostgreSQL schema. 2026-06-12.
  - Что сделать: проверить таблицы, индексы, FK, nullable-поля, миграции.
  - Что сделано: добавлены кроссплатформенные `scripts/audit-postgres-schema.sh` и `scripts/audit-postgres-schema.ps1`; аудит формирует `ef-migrations.txt`, idempotent `postgres-migrations-idempotent.sql`, metadata-файл и, при наличии `DATABASE_URL`/`psql`, sanitized `postgres-schema-snapshot.txt` только из `information_schema` и `pg_indexes` без чтения пользовательских данных. Добавлен runbook `docs/postgres-schema-audit.md` с локальным EF-only режимом и production/staging режимом для реальной PostgreSQL-БД.
  - Доказательство: `PostgresSchemaAuditTests` проверяет PostgreSQL EF metadata, наличие PK у всех mapped entities, индексы, FK, nullable metadata, migration chain и безопасность audit-скриптов; PowerShell syntax check; локальный EF-only запуск `scripts\audit-postgres-schema.ps1`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-postgres-schema-audit`.

- [x] `P5-DB-002` EF model drift check. 2026-06-12.
  - Что сделать: убедиться, что модель и миграции не расходятся.
  - Что сделано: базовый `EfModelDriftTests` сравнивает runtime-модель `ApplicationDbContext` с `ApplicationDbContextModelSnapshot`; добавлен кроссплатформенный `scripts/check-ef-drift.ps1` для Windows рядом с Linux `scripts/check-ef-drift.sh`; acceptance-test проверяет, что оба скрипта используют `has-pending-model-changes`, временную `__ModelDriftCheck`, безопасные env-переменные и документацию.
  - Доказательство: `EfModelDriftTests` 2/2; `powershell -ExecutionPolicy Bypass -File scripts\check-ef-drift.ps1` завершился `[OK] EF model has no pending migration changes`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-ef-drift-powershell-gate`.

- [x] `P5-DB-003` Seed локальных данных. 2026-06-12.
  - Что сделать: локальный запуск должен иметь тарифы, sandbox payments, sandbox VPN node, admin user.
  - Что сделано: demo seed дополняет чистую локальную SQLite-БД тарифами, FAQ/content/work scenario, sandbox-аккаунтами всех платежных провайдеров, disabled Telegram Stars, sandbox node group, `sandbox-x3ui-panel`, default VLESS inbound и `sandbox-vpn-node` Ready/Healthy. Admin bootstrap создает `admin@local.test`, а повторный seed не дублирует данные.
  - Доказательство: `PaymentProviderSandboxSeedTests` 2/2 на SQLite/InMemory; backend full suite; local SQLite HTTP-smoke после чистой БД показывает latest release `2026-06-12-local-seed-vpn-infrastructure`, публичные тарифы, платежные провайдеры, admin login и health/metrics.

- [x] `P5-DB-004` Backup/restore для VPS. 2026-06-12.
  - Что сделать: настроить backup PostgreSQL и инструкцию восстановления.
  - Что сделано: усилен `scripts/backup-db.sh`, добавлены `scripts/backup-db.ps1`, `scripts/restore-db.sh`, `scripts/restore-db.ps1`, игнор `backups/`, retention через `BACKUP_RETENTION_DAYS`, `.dump.list` через `pg_restore --list`, restore только через отдельный `RESTORE_DATABASE_URL` с защитой от совпадения с `DATABASE_URL`; `apply-migrations.sh` использует backup retention.
  - Доказательство: `DatabaseBackupRestoreScriptsTests`; PowerShell syntax check; runbook `docs/postgres-backup-restore.md` с test restore в `vpnplatform_restore_check`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-postgres-backup-restore-runbook`.

## P6. Безопасность и секреты

- [x] `P6-SEC-001` Production secret storage. 2026-06-12.
  - Проблема: Own VPS provisioning пока не materializes protected SSH credentials для live Ansible.
  - Что сделать: secret manager или encrypted ProvisioningSecret table, temporary materialization с cleanup.
  - Что сделано: добавлен `ProvisioningSecretMaterializer`, который использует `ISecretProtector`, расшифровывает только protected `ssh_key` payload в временный файл `WorkingDirectory/<runId>/secrets/ssh-key-*`, выставляет best-effort права `700/600` на Unix, передает runner только path и удаляет файл в `finally`. `AnsibleProvisioningExecutor` больше не fail-closed для supported protected SSH key, но продолжает блокировать password-based live SSH, `validation-placeholder:*`, legacy protected values в `SshPrivateKeyPath` и missing protected payload при наличии `SshCredentialRef`.
  - Доказательство: `ProvisioningSecretMaterializerTests`, `OwnVpsProvisioningMvpTests`, `SecurityHardeningMvpTests`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-production-provisioning-secret-storage`; документация `docs/production-secret-storage.md`.

- [x] `P6-SEC-002` Secret rotation. 2026-06-12.
  - Что сделать: ротация платежных, Telegram, 3x-ui, SSH секретов без показа старых значений.
  - Что сделано: платежная ротация уже фиксировалась через `payment_provider.secret.rotate`; добавлены `server.secret.rotate` для SSH credential/panel password и `telegram_bot.secret.rotate` для BotToken/SecretToken. При ротации server secrets создается новый `secretref:ssh:*`/`secretref:panel:*`, старые значения не раскрываются, API продолжает возвращать только configured-флаги. Audit-события содержат только безопасные флаги `rotated*` и metadata без raw secret/protected payload/secretref.
  - Доказательство: `SecurityHardeningMvpTests`, `AdminTelegramBotSettingsControllerTests`, `AuditLogMvpTests`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-secret-rotation-audit`; документация `docs/secret-rotation.md`.

- [x] `P6-SEC-003` RBAC. 2026-06-12.
  - Что сделать: роли admin/support/operator, запрет опасных действий без прав.
  - Что сделано: добавлена единая `AdminPolicies.PolicyRoles`, а `Program.cs` регистрирует все admin-policies из этой матрицы. Роли `ReadOnly`, `SupportAgent`, `FinanceManager`, `Operator`, `Admin`, `SuperAdmin` разведены по read/write/manage-доступам; `User` исключен из всех admin-policy. Для финансов, поддержки, VPN, provisioning, Telegram-бота и системных настроек сохранены отдельные политики.
  - Доказательство: `AdminAuthorizationPolicyTests`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-rbac-policy-matrix`; документация `docs/rbac-policy-matrix.md`.

- [x] `P6-SEC-004` Rate limiting. 2026-06-12.
  - Что сделать: login, register, forgot password, webhook endpoints, public checkout.
  - Что сделано: добавлены `ApiRateLimitPolicies` и middleware `AddRateLimiter/UseRateLimiter`; auth endpoints `register/login/refresh/forgot-password/reset-password` используют `auth-sensitive`, публичный checkout использует `public-checkout`, платежные и channel webhook controllers используют `webhook`. При превышении лимита API возвращает `429 Too Many Requests` с problem JSON.
  - Доказательство: `RateLimitingSecurityTests`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-api-rate-limiting`; документация `docs/rate-limiting.md`.

- [x] `P6-SEC-005` CORS/CSP/security headers. 2026-06-12.
  - Что сделать: проверить production headers для API и frontend.
  - Что сделано: backend API получил `SecurityHeadersMiddleware` с `nosniff`, `DENY`, `no-referrer`, `Permissions-Policy`, API CSP и production HSTS; frontend Docker images `public-web`, `cabinet`, `admin-panel` используют общий `nginx.security.conf` с CSP/HSTS/security headers и SPA fallback. Production CORS остается allow-list based через `Cors:AllowedOrigins` и startup validator.
  - Доказательство: `SecurityHeadersTests`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-security-headers`; документация `docs/security-headers.md`.

- [x] `P6-SEC-006` Проверка утечек секретов. 2026-06-12.
  - Что сделать: scan repo, logs, docs, env examples на реальные ключи.
  - Что сделано: добавлены `scripts/scan-secrets.ps1` и `scripts/scan-secrets.sh` для поиска Telegram, Stripe/OpenAI, GitHub, GitLab, AWS, Google, Slack tokens и PEM private keys; `validate-backend.sh` и `validate-all.sh` запускают secret scan до build/test; `check-validation-safety.sh` проверяет наличие scanner и базовых паттернов. Для тестовых fixture и локальных placeholders добавлен явный allowlist.
  - Доказательство: `SecretScanTests`; PowerShell secret scan result `Files scanned: 385. Findings: 0`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-secret-scan-gate`; документация `docs/secret-scan.md`.

## P7. Provisioning VPS

- [x] `P7-PROV-001` Разделить dry-run, validation и live deploy. 2026-06-12.
  - Что сделать: UI и backend должны явно показывать режим, риски и ограничения.
  - Что сделано: `ProvisioningService` получил единый `ProvisioningModeDescriptor` для `dry-run`, `validation-deploy`, `live-deploy-blocked` и `live-deploy`; backend возвращает mode/risk/liveDeployAllowed/nextAction/operatorWarning в admin API серверов и provisioning runs, а для dry-run отдельно отдаёт будущий `deployMode*`. Админка показывает режимы и риски в списке серверов и запусков, оставляет безопасный precheck доступным и блокирует deploy, если live deploy не разрешён явно.
  - Доказательство: `OwnVpsProvisioningMvpTests`, frontend source/API contract tests, `npm run typecheck --workspace apps/admin-panel`, документация `docs/provisioning-modes.md`, releaseId `2026-06-12-provisioning-mode-boundaries`.

- [x] `P7-PROV-002` Live Ansible credentials. 2026-06-12.
  - Что сделать: безопасная временная передача SSH credentials в Ansible.
  - Что сделано: live Ansible получает protected `ssh_key` только через временный файл `WorkingDirectory/<runId>/secrets/ssh-key-*`, а executor удаляет файл в `finally`. Redaction теперь покрывает raw/private key, protected payload, legacy key path, temporary key path и temporary `secrets` directory path, поэтому runner output/stderr/step logs не сохраняют путь или секрет даже при случайном выводе аргументов.
  - Доказательство: `ProvisioningSecretMaterializerTests`, `OwnVpsProvisioningMvpTests`; тест `Ansible_Runner_Redaction_Should_Cover_Temporary_Key_Path_And_Plaintext`; backend full suite; local SQLite HTTP-smoke latest release `2026-06-12-live-ansible-credentials`; документация `docs/live-ansible-credentials.md`.

- [x] `P7-PROV-003` Precheck сервера. 2026-06-12.
  - Что сделать: OS, ports, disk, RAM, firewall, Docker/systemd, 3x-ui availability.
  - Что сделано: `precheck-node.yml` проверяет Debian-family OS, свободное место на root, RAM, listening ports, firewall/UFW, systemd, Docker runtime и доступность/установленность 3x-ui. `AnsibleProvisioningExecutor` формирует единый JSON `Precheck report` для mock и live runner, сохраняет его отдельным `ProvisioningStepRun`, добавляет в summary log и отдает через admin API как `precheckReport`/`precheckReportPreview`. Админка показывает отчет в разделе «Подготовка VPS».
  - Доказательство: `OwnVpsProvisioningMvpTests`, frontend API/typecheck tests, backend full suite, local SQLite HTTP-smoke latest release `2026-06-12-vps-precheck-report`, документация `docs/vps-precheck-report.md`.

- [x] `P7-PROV-004` Rollback. 2026-06-12.
  - Что сделать: при неудачном provisioning вернуть run/node в понятное состояние.
  - Что сделано: `ProvisioningWorker` снимает snapshot состояния `VpnNode` перед deploy и при ошибке возвращает эксплуатационные поля ноды к прежним значениям, оставляя `ProvisioningRun.Status=Failed` и `VpnNode.ProvisioningStatus=Failed` для видимости инцидента. В run добавляется шаг `Rollback node state`, в audit пишется `provisioning.rollback_applied`, а support/Telegram получают redacted-контекст ошибки.
  - Доказательство: failure scenario test `OwnVps_Deploy_Failure_Should_Roll_Back_Node_State_And_Surface_Admin_Context`, backend full suite, local SQLite HTTP-smoke latest release `2026-06-12-vps-provisioning-rollback`, документация `docs/vps-provisioning-rollback.md`.

- [x] `P7-PROV-005` Документация live provisioning. 2026-06-12.
  - Что сделать: отдельный runbook с предупреждениями и командами.
  - Что сделано: добавлен `docs/live-provisioning-runbook.md` с preflight, Ansible syntax-check, SSH/known_hosts, live flags `Provisioning__LiveExecutionEnabled` и `Provisioning__AllowLiveDeploy`, тегами `validation-mode:false` и `explicit-live-provisioning:true`, API-порядком precheck/deploy, ручным runner dry-run, rollback/failure path, smoke и fail-closed правилами. Общий `docs/provisioning.md` теперь ссылается на live runbook.
  - Доказательство: `ReleaseDocumentationGuardTests.Live_Provisioning_Runbook_Should_Cover_Operator_Gates`, backend full suite, local SQLite HTTP-smoke latest release `2026-06-12-live-provisioning-runbook`, releaseId `2026-06-12-live-provisioning-runbook`.

## P8. CI/CD, GitHub и VPS deploy

- [x] `P8-CI-001` Проверить workflow auto-detect docker/systemd. 2026-06-12.
  - Что сделать: убедиться, что deploy выбирает корректный режим и пишет понятный лог.
  - Что сделано: шаг `Detect deployment mode` в `.github/workflows/deploy-vps.yml` теперь пишет requested/selected режим, результат Docker Compose detection, причину выбора в `::notice` и блок `$GITHUB_STEP_SUMMARY`. Для `auto` workflow выбирает `docker` только если на VPS доступны `docker` и `docker compose version`, иначе уходит в `systemd`; явные `docker`/`systemd` режимы сохраняются как manual override.
  - Доказательство: `DeployWorkflowGuardTests`, документация `docs/deploy-vps-auto-detect.md`, backend full suite, local SQLite HTTP-smoke latest release `2026-06-12-deploy-mode-auto-detect`, GitHub Actions log должен содержать `Deploy mode: requested=... selected=... docker_detected=... reason=...`.

- [x] `P8-CI-002` Required checks для main. 2026-06-12.
  - Что сделать: включить обязательные checks перед merge/push.
  - Что сделано: добавлен конфиг `.github/branch-protection.required-checks.json` с обязательными checks из workflow `validation`, включены strict up-to-date checks, один approving review, dismiss stale approvals, conversation resolution, запрет force push/delete и enforcement для администраторов. Добавлен `scripts/configure-branch-protection.ps1`, который применяет настройки через GitHub REST API или показывает payload в `-DryRun`. `deploy-vps` не добавлен в required checks, потому что это production deploy на push, а не PR validation.
  - Доказательство: `BranchProtectionGuardTests`, `powershell -ExecutionPolicy Bypass -File scripts/configure-branch-protection.ps1 -DryRun`, документация `docs/github-required-checks.md`, backend full suite, local SQLite HTTP-smoke latest release `2026-06-12-required-checks-main`, config `.github/branch-protection.required-checks.json`.

- [x] `P8-CI-003` Secrets audit в GitHub. 2026-06-13.
  - Что сделать: проверить наличие и названия secrets для VPS, DB, deploy, registry.
  - Что сделано: добавлен `.github/github-secrets.audit.json` со списком required/optional secret names для `deploy-vps`, включая VPS/deploy/frontend категории и явную пометку, что registry secrets сейчас не нужны. Добавлен `scripts/audit-github-secrets.ps1`: в `-DryRun` он сверяет конфиг с workflow локально, а в live-режиме через GitHub REST API получает только имена repository secrets и проверяет missing required без вывода значений.
  - Доказательство: `GitHubSecretsAuditTests`, `powershell -ExecutionPolicy Bypass -File scripts/audit-github-secrets.ps1 -DryRun`, документация `docs/github-secrets-audit.md`, backend full suite, local SQLite HTTP-smoke latest release `2026-06-13-github-secrets-audit`; список имен без значений хранится в `.github/github-secrets.audit.json`.

- [x] `P8-CI-004` VPS disk/memory maintenance. 2026-06-13.
  - Что сделать: безопасная очистка old artifacts, logs rotation, apt cache, docker cache если используется.
  - Что сделано: добавлен `scripts/vps-maintenance.sh` с dry-run по умолчанию и явным `--apply`, отчетом `df -h/free -h/du -sh` до и после, защитой `APP_DIR/shared`, `APP_DIR/current`, текущего release и путей вне `APP_DIR`. Скрипт чистит только старые release-директории с именем git sha, старые release archives, app logs, systemd journal и apt cache; Docker prune включается только отдельным `--docker-prune` и не трогает volumes. Добавлена инструкция `docs/vps-maintenance.md`.
  - Доказательство: `VpsMaintenanceScriptTests`, `bash -n scripts/vps-maintenance.sh`, local dry-run на временном APP_DIR, backend full suite, local SQLite HTTP-smoke latest release `2026-06-13-vps-maintenance-safe-cleanup`; live VPS cleanup не запускался без отдельной команды оператора.

- [x] `P8-CI-005` Post-deploy smoke. 2026-06-13.
  - Что сделать: после deploy автоматически проверять API health, public, cabinet, admin, public providers.
  - Что сделано: добавлен `scripts/post-deploy-smoke.sh`, который проверяет `/health/live`, `/health/ready`, `/metrics`, `/api/public/payments/providers`, public web, cabinet web и admin web. Workflow `.github/workflows/deploy-vps.yml` запускает шаг `Post-deploy smoke` после docker или systemd deploy, вычисляет URL по режиму деплоя и optional secrets `POST_DEPLOY_*`, требует непустой список публичных платежных провайдеров для production и пишет результат в `$GITHUB_STEP_SUMMARY`.
  - Доказательство: `PostDeploySmokeTests`, `GitHubSecretsAuditTests`, локальный post-deploy smoke на чистой SQLite API и тестовом HTML-сервере, backend full suite, local SQLite HTTP-smoke latest release `2026-06-13-post-deploy-smoke`; Actions log должен содержать `[ok] API live health`, `[ok] Public payment providers`, `[ok] Public web`, `[ok] Cabinet web`, `[ok] Admin web`.
- [x] `P8-CI-006` Normalize production env before VPS deploy upload. 2026-06-24.
  - Что сделать: не давать stale `PRODUCTION_ENV_FILE` повторно включать `Local`, auto migrations, demo seed, Swagger или постоянный admin bootstrap при GitHub Actions deploy.
  - Что сделано: добавлен `scripts/normalize-production-env.ps1`, workflow `deploy-vps` запускает его один раз перед Docker/systemd upload и отправляет на VPS уже нормализованный `production.env`.
  - Доказательство: `DeployWorkflowGuardTests`, `scripts/test-normalize-production-env.ps1`, latest "Что нового" `2026-06-24-deploy-production-env-normalizer`, версия `0.289.0`. Реальный VPS admin login был восстановлен вручную без публикации секретов, но `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` остаются открытыми до полного smoke report.

## P9. Тестирование

- [x] `P9-TST-001` Backend обязательный suite. 2026-06-13.
  - Текущее состояние: проходит `433/433`.
  - Что сделать: держать зеленым после каждого изменения.
  - Что сделано: добавлен Windows PowerShell entrypoint `scripts/validate-backend.ps1`, который повторяет обязательный backend gate: validation safety, secret scan, restore, build, full backend tests, dotnet tools, EF migrations list и EF model drift. Документация `docs/backend-validation-gate.md` фиксирует текущий зеленый счетчик `433/433`, safe defaults и обязательные команды для PR/roadmap задач. Guard-тесты проверяют, что bash и PowerShell gates не теряют full test suite, EF checks, secret scan и безопасные env defaults.
  - Доказательство: `BackendValidationGateTests`, `SecretScanTests.Validation_Entry_Points_Should_Run_Secret_Scan`, PowerShell syntax parse `scripts/validate-backend.ps1`, backend full suite 433/433, local SQLite HTTP-smoke latest release `2026-06-13-backend-validation-gate`.

- [x] `P9-TST-002` Frontend unit tests. 2026-06-13.
  - Текущее состояние: проходит `64/64`.
  - Что сделать: держать зеленым после каждого UI/API-client изменения.
  - Что сделано: добавлены отдельные frontend validation entrypoints `scripts/validate-frontend.ps1` и `scripts/validate-frontend.sh`: npm lock/config safety, `npm ci`, typecheck, production build, unit tests и high-severity audit. Документация `docs/frontend-validation-gate.md` фиксирует обязательные команды, текущий счетчик `64/64` и критерий готовности. Frontend guard-тесты проверяют, что локальные gates, `package.json`, CI workflow, roadmap, TEST_RESULTS и release seed не расходятся.
  - Доказательство: `frontend-validation-gate.test.ts`, `npm test` 64/64, `npm run typecheck`, `npm run build`, `npm audit --audit-level=high`, PowerShell syntax parse `scripts/validate-frontend.ps1`, local SQLite HTTP-smoke latest release `2026-06-13-frontend-validation-gate`.

- [x] `P9-TST-003` Playwright E2E public. 2026-06-13.
  - Что сделать: держать зеленым пользовательский путь главная -> тарифы -> checkout start -> аккаунт -> FAQ.
  - Что сделано: добавлен Playwright config `frontend/playwright.config.ts`, E2E spec `frontend/e2e/public.spec.ts`, npm-скрипты `e2e` и `e2e:public`, документация `docs/playwright-public-e2e.md`. Тест поднимает public-web на выделенном порту, мокирует публичные API endpoints без live-платежей, проверяет главную, managed FAQ preview, тарифы, web-provider select, создание public checkout session, сохраненную покупку на `/account`, FAQ search и отсутствие console/page errors. CI и staging-validation теперь устанавливают Chromium, запускают `npm run e2e:public` и сохраняют HTML-report.
  - Доказательство: `npm run e2e:public` 1/1, frontend unit tests 64/64, frontend typecheck/build, backend full suite 433/433, local SQLite HTTP-smoke latest release `2026-06-13-playwright-public-e2e`.

- [x] `P9-TST-004` Playwright E2E cabinet. 2026-06-13.
  - Что сделать: держать зеленым пользовательский путь register/login/order/payment status/subscription/access/support.
  - Что сделано: добавлен Playwright spec `frontend/e2e/cabinet.spec.ts`, общий Vite helper `frontend/scripts/playwright-webservers.mjs`, npm-скрипт `e2e:cabinet`, документация `docs/playwright-cabinet-e2e.md`. Тест мокирует cabinet API без live-платежей и проверяет регистрацию, выход, повторный вход, активную подписку, VPN-ключ, QR-код, историю заказов/платежей/доступов, продление через web-провайдер и поддержку. CI и staging-validation запускают `npm run e2e:cabinet` после public E2E и сохраняют общий HTML-report.
  - Доказательство: `npm run e2e:cabinet` 1/1, `npm run e2e:public` 1/1, frontend unit tests 64/64, frontend typecheck/build, backend full suite 433/433, local SQLite HTTP-smoke latest release `2026-06-13-playwright-cabinet-e2e`.

- [x] `P9-TST-005` Playwright E2E admin. 2026-06-13.
  - Что сделать: держать зеленым админский путь login/payments/tariffs/VPN/panels/scenarios/releases.
  - Что сделано: добавлен Playwright spec `frontend/e2e/admin.spec.ts`, admin-panel подключен к общему helper `frontend/scripts/playwright-webservers.mjs`, добавлен npm-скрипт `e2e:admin`, документация `docs/playwright-admin-e2e.md`. Тест мокирует admin API без live-платежей, Telegram, VPS и 3x-ui, проверяет вход администратора, дашборд, оплату, создание тарифа, VPN-доступы, panel test/sync, создание сценария работы и релиза «Что нового». CI и staging-validation запускают `npm run e2e:admin` после public/cabinet E2E и сохраняют общий HTML-report.
  - Доказательство: `npm run e2e:admin` 1/1, `npm run e2e:public` 1/1, `npm run e2e:cabinet` 1/1, frontend unit tests 64/64, frontend typecheck/build, backend full suite 433/433, local SQLite HTTP-smoke latest release `2026-06-13-playwright-admin-e2e`.

- [x] `P9-TST-006` Payment provider contract tests. 2026-06-13.
  - Что сделать: signature verification, webhook payloads, idempotency и единый контракт регистрации для всех провайдеров.
  - Что сделано: добавлен `PaymentProviderContractTests`, который поднимает реальные Application/Infrastructure DI-регистрации, проверяет один `IPaymentProvider` на каждый `PaymentProvider`, фиксирует webhook verifier/status mapper для всех web-провайдеров, проверяет networkless local sandbox create для YooMoney, YooKassa, RoboKassa, CloudPayments, TBank, Prodamus, Stripe и PayPal, а также bot-only/fail-closed контракт Telegram Stars. Capability rules проверяются на полный набор ключей и читаемые labels без mojibake.
  - Доказательство: `PaymentProviderContractTests` 12/12, backend full suite 445/445, local SQLite HTTP-smoke latest release `2026-06-13-payment-provider-contract-tests`.

- [~] `P9-TST-007` Real staging smoke checklist.
  - Что сделать: вручную или полуавтоматически пройти покупку и выдачу VPN на staging.
  - Что сделано: добавлен `docs/staging-smoke-checklist.md`, безопасный шаблон `docs/staging-smoke-report.template.json` и валидатор `scripts/validate-staging-smoke-report.ps1`. Валидатор проверяет обязательные пункты deploy, health, public/cabinet/admin web, admin login, tariffs, payment providers, checkout, payment init, provider confirmation, subscription, VPN access, support, browser console, secret rotation и отсутствие секретов в отчете. Режим `-RequireAllPassed` fail-closed и не принимает `blocked`, `failed` или `skipped`.
  - Что осталось: заполнить реальный staging/VPS smoke report после deploy, ротации секретов, настройки provider sandbox и production-like 3x-ui окружения.
  - Доказательство: `StagingSmokeChecklistTests` 3/3, validator structural check, backend full suite 478/478, local SQLite HTTP-smoke latest release `2026-06-14-all-screens-browser-smoke`, версия `0.107.0`; заполненный live/staging smoke report еще нужен.

- [x] `P9-TST-007A` Staging smoke report secret sanitizer. 2026-06-14.
  - Что сделать: усилить валидатор staging smoke report, чтобы он не принимал отчеты с cookies, `.env`, client secrets, API keys, private headers, Telegram secret header и GitHub/VPS secret names.
  - Что сделано: `scripts/validate-staging-smoke-report.ps1` расширен forbidden-маркерами `Cookie:`, `Set-Cookie:`, `.env`, `client_secret`, `api_key`, `private header`, `X-Telegram-Bot-Api-Secret-Token`, `PRODUCTION_ENV_FILE` и `VPS_SSH_KEY`; документация staging smoke и production readiness gate обновлена.
  - Доказательство: `StagingSmokeChecklistTests` 4/4, backend full suite `487/487`, latest "Что нового" `2026-06-14-staging-smoke-secret-sanitizer`, версия `0.113.0`.

- [x] `P9-TST-007B` Staging smoke report consistency guard. 2026-06-14.
  - Что сделать: запретить неконсистентные staging smoke reports с обратным порядком времени и повторяющимися check id.
  - Что сделано: `scripts/validate-staging-smoke-report.ps1` проверяет, что `completedAt` не раньше `startedAt`, а каждый check id встречается только один раз.
  - Доказательство: `StagingSmokeChecklistTests` 5/5, backend full suite `488/488`, latest "Что нового" `2026-06-14-staging-smoke-report-consistency`, версия `0.114.0`.

- [x] `P9-TST-007C` Staging smoke report URL validation. 2026-06-14.
  - Что сделать: запретить отчеты с пустым или некорректным `apiBaseUrl` и произвольным текстом вместо web URL.
  - Что сделано: `scripts/validate-staging-smoke-report.ps1` проверяет `apiBaseUrl`, `publicWebUrl`, `cabinetWebUrl` и `adminWebUrl` как absolute http/https URL; web URL остаются опциональными, но при заполнении валидируются.
  - Доказательство: `StagingSmokeChecklistTests` 6/6, backend full suite `489/489`, latest "Что нового" `2026-06-14-staging-smoke-report-url-validation`, версия `0.115.0`.

- [x] `P9-TST-007D` Staging smoke report generator. 2026-06-14.
  - Что сделать: убрать ручное копирование JSON как первый шаг и дать оператору безопасный генератор черновика staging/VPS smoke report.
  - Что сделано: добавлен `scripts/new-staging-smoke-report.ps1`, который берет `docs/staging-smoke-report.template.json`, подставляет URL окружения, оператора и latest release из seed "Что нового", создает все обязательные checks в `blocked`, не перезаписывает существующий отчет без `-Force` и сразу запускает `validate-staging-smoke-report.ps1`.
  - Доказательство: `StagingSmokeChecklistTests` 7/7, generator smoke на `tmp/generated-staging-smoke-report.json`, expected fail-closed `-RequireAllPassed`, backend full suite `496/496`, fresh local SQLite smoke, local SQLite VPS smoke dry-run, latest "Что нового" `2026-06-14-staging-smoke-report-generator`, версия `0.119.0`.

- [x] `P9-TST-007E` Staging smoke report evidence placeholder guard. 2026-06-19.
  - Что сделать: запретить принимать staging smoke report, где все checks помечены `passed`, но evidence осталось шаблонным `TODO`.
  - Что сделано: `scripts/validate-staging-smoke-report.ps1` в режиме `-RequireAllPassed` теперь отклоняет evidence с `TODO`; `docs/staging-smoke-checklist.md` явно требует real evidence вместо placeholder-строк.
  - Доказательство: `StagingSmokeChecklistTests` 8/8, expected fail-closed `-RequireAllPassed` для passed checks с TODO evidence, "Что нового" `2026-06-19-staging-smoke-report-evidence-placeholders`, версия `0.181.0`.

- [x] `P9-TST-007F` Staging smoke report latest release guard. 2026-07-01.
  - Что сделать: не принимать staging smoke report как финальный acceptance evidence, если отчет был заполнен для старого release.
  - Что сделано: `scripts/validate-staging-smoke-report.ps1 -RequireAllPassed` сверяет `releaseId` отчета с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`; добавлен regression harness `scripts/test-staging-smoke-report-latest-release-guard.ps1`, который доказывает fail-closed поведение на полностью passed отчете со stale `releaseId`.
  - Доказательство: `StagingSmokeChecklistTests` 9/9, staging smoke latest release guard regression, latest "Что нового" `2026-07-01-staging-smoke-latest-release-guard`, версия `0.299.0`. Реальный live/staging smoke report еще нужен, поэтому `P9-TST-007` остается `[~]`.

- [x] `P9-TST-008` All screens browser smoke. 2026-06-14.
  - Что сделать: пройти все основные public/cabinet/admin экраны в браузере, проверить отсутствие белых экранов, `console.error` и `pageerror`.
  - Что сделано: добавлен Playwright spec `frontend/e2e/all-screens.spec.ts`, project `all-screens`, npm-скрипт `e2e:all-screens` и документация `docs/all-screens-browser-smoke.md`. Smoke открывает public routes `/`, `/tariffs`, `/faq`, `/help`, `/account`, проверяет cabinet auth screen и авторизованный dashboard, а также все admin sections: `dashboard`, `users`, `payments`, `tariffs`, `subscriptions`, `vpn`, `nodes`, `panels`, `support`, `audit`, `bot`, `releases`, `faq`, `content`, `scenarios`, `provisioning`. `e2e:console` теперь включает project `all-screens`.
  - Доказательство: `npm run e2e:all-screens --prefix frontend` 3/3, `npm run e2e:console --prefix frontend` 9/9, `AllScreensBrowserSmokeTests` 2/2, backend full suite 478/478, local SQLite HTTP-smoke latest release `2026-06-14-all-screens-browser-smoke`, версия `0.107.0`.

## P10. Документация

- [x] `P10-DOC-001` README на русском. 2026-06-13.
  - Что сделать: запуск без Docker, запуск с Docker, env, DB, tests, deploy.
  - Что сделано: README переписан как основной русский входной документ: описаны назначение платформы, структура монорепозитория, быстрый запуск без Docker на SQLite, ручной запуск API/frontend, восстановление локального администратора, команды проверки, Docker-режим, VPS/systemd контекст, платежи, VPN, режимы окружения и актуальный статус проекта.
  - Доказательство: `ReadmeDocumentationTests` 3/3, backend full suite 448/448, frontend tests/typecheck/build, local SQLite HTTP-smoke latest release `2026-06-13-readme-russian-local-runbook`.

- [x] `P10-DOC-002` Документация администратора. 2026-06-13.
  - Что сделать: как настроить тарифы, платежи, VPN, 3x-ui, Telegram, сценарии.
  - Что сделано: добавлено `docs/admin-guide.md` с полным операторским runbook по всем вкладкам админки: вход и RBAC, дашборд, пользователи, платежные провайдеры, тарифы, подписки, VPN-доступы, серверы, 3x-ui панели, поддержка, аудит, Telegram-бот, "Что нового", FAQ, контент сайта, сценарии и подготовка VPS. Документ описывает provider-specific поля, write-only секреты, кнопку "Проверить подключение", sandbox/live границы, Telegram Stars как bot-only/fail-closed flow, 3x-ui inbound/client flow и быстрый приемочный чеклист.
  - Доказательство: `AdminGuideDocumentationTests` 3/3, backend full suite 451/451, frontend tests/typecheck/build, local SQLite HTTP-smoke latest release `2026-06-13-admin-operator-guide`.

- [x] `P10-DOC-003` Документация пользователя. 2026-06-13.
  - Что сделать: как купить, оплатить, подключить VPN, продлить, обратиться в поддержку.
  - Что сделано: добавлено `docs/user-guide.md`, публичная страница `/help`, ссылка "Помощь" в навигации публичного сайта и блок "Как пользоваться сервисом" в личном кабинете. Пользовательский путь описывает выбор тарифа, оплату, возврат в кабинет, получение ссылки и QR-кода, подключение через VLESS/VMess/Trojan, продление, Telegram-привязку, поддержку и окно "Что нового".
  - Доказательство: `UserGuideDocumentationTests` 3/3, frontend `user-help.test.ts`, backend full suite 454/454, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-13-user-help-pages`.

- [x] `P10-DOC-004` Документация разработчика. 2026-06-13.
  - Что сделать: архитектура, доменные сущности, state machines, тесты, добавление провайдера.
  - Что сделано: добавлены `docs/developer-guide.md` и `docs/README.md`. Руководство разработчика описывает слои монорепозитория, доменные сущности, state machines, платежный поток, добавление `IPaymentProvider`/webhook verifier/status mapper, VPN/3x-ui flow, provisioning gates, frontend-правила, validation gates, PostgreSQL/SQLite, секреты и порядок обновления документации/"Что нового". Индекс документации связывает README, roadmap, руководства администратора/пользователя/разработчика, платежи, provisioning, безопасность и E2E.
  - Доказательство: `DeveloperGuideDocumentationTests` 3/3, backend full suite 457/457, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-13-developer-guide`.

- [x] `P10-DOC-005` Убрать mojibake в старых документах. 2026-06-13.
  - Проблема: часть документов в консоли отображалась как типовые последовательности UTF-8/CP1251 mojibake, нужно было проверить реальные файлы и перекодировать поврежденные.
  - Что сделать: проверить encoding всех `.md`, исправить поврежденные тексты.
  - Что сделано: добавлен `DocumentationEncodingTests`, который сканирует `docs/**/*.md`, `README.md` и `TEST_RESULTS.md` на replacement character и типовые mojibake-маркеры без хранения этих маркеров в markdown. В roadmap убран последний реальный маркер битой строки, а проверка кодировки вынесена в обязательный backend suite.
  - Доказательство: `DocumentationEncodingTests` 1/1, backend full suite 458/458, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-13-docs-encoding-guard`.

## P11. Финальная приемка production-ready

- [x] `P11-ACC-001` Fresh local setup. 2026-06-13.
  - Что сделать: с нуля поднять backend, frontend, локальную БД, seed, пройти sandbox purchase.
  - Что сделано: добавлен `scripts/fresh-local-smoke.ps1` и инструкция `docs/fresh-local-smoke.md`. Smoke поднимает API на чистой SQLite-БД, включает local seed, проверяет health, публичные тарифы и payment providers, создает checkout session, регистрирует пользователя, claim-ит заказ, инициализирует YooKassa sandbox payment, отправляет local sandbox webhook `payment.succeeded`, проверяет историю заказов/платежей, активную подписку и VPN-доступ с `vless://` URI. Во время проверки найден и исправлен SQLite-баг `/api/me/orders`: сортировка `DateTimeOffset` перенесена после `ToListAsync`.
  - Доказательство: `FreshLocalSetupSmokeTests` 1/1, `powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101`, backend full suite 459/459, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-13-fresh-local-smoke`.

- [ ] `P11-ACC-002` VPS production smoke.
  - Что сделать: deploy -> health -> admin login -> public order -> payment -> subscription -> VPN access.
  - Что сделано: добавлен `scripts/vps-production-smoke.ps1` и инструкция `docs/vps-production-smoke.md`. Runner проверяет `/health/live`, `/health/ready`, опционально public/cabinet/admin SPA, admin login/dashboard, публичные тарифы и способы оплаты, checkout session, регистрацию пользователя, claim заказа, payment init, sandbox webhook только в non-Production, историю заказов/платежей, активную подписку, VPN access и latest "Что нового". Для `YooKassa` добавлен безопасный sandbox webhook header. Скрипт fail-closed: без `-AllowSandboxWebhook` останавливается после payment init с `partial ok`, а с `-AllowSandboxWebhook` запрещает запуск, если API сообщает `Production`.
  - Что осталось: выполнить live/staging прогон после deploy, ротации раскрытых секретов, настройки домена/HTTPS, реальных provider sandbox кабинетов и production-like VPN/3x-ui окружения.
  - Доказательство: `VpsProductionSmokeTests` 3/3, local SQLite VPS smoke dry-run, backend full suite 478/478, local SQLite HTTP-smoke latest release `2026-06-14-all-screens-browser-smoke`, версия `0.107.0`; live VPS smoke report еще нужен для закрытия пункта.

- [x] `P11-ACC-003` Mobile smoke. 2026-06-14.
  - Что сделать: public/cabinet/admin на мобильном viewport.
  - Что сделано: добавлены Playwright-проекты `mobile-public`, `mobile-cabinet`, `mobile-admin`, npm-скрипт `e2e:mobile`, сохранение скриншотов `public-mobile.png`, `cabinet-mobile.png`, `admin-mobile.png` и инструкция `docs/mobile-smoke.md`. Mobile smoke прогоняет существующие E2E-сценарии public/cabinet/admin на viewport Pixel 5, проверяет отсутствие `console.error`/`pageerror` и сохраняет PNG-артефакты в `frontend/test-results`. Визуальный просмотр показал, что экраны не пустые и основные действия доступны; остаточный UX-риск: интерфейсы кабинета и админки на 393px остаются плотными и требуют финальной ручной полировки перед production-ready.
  - Доказательство: `npm run e2e:mobile --prefix frontend` 3/3, `MobileSmokeDocumentationTests` 1/1, backend full suite 460/460, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-14-mobile-smoke`.

- [x] `P11-ACC-004` No console errors. 2026-06-14.
  - Что сделать: проверить основные экраны в браузере.
  - Что сделано: добавлен npm-скрипт `e2e:console`, который прогоняет desktop, all-screens и mobile Playwright-проекты `public-web`, `cabinet`, `admin-panel`, `all-screens`, `mobile-public`, `mobile-cabinet`, `mobile-admin`. E2E-сценарии подписаны на `page.on('console')` и `page.on('pageerror')` и падают при `console.error` или необработанном browser exception. Добавлена инструкция `docs/no-console-errors-smoke.md`.
  - Доказательство: `npm run e2e:console --prefix frontend` 9/9, browser console report `console.error=0`, `pageerror=0`, `NoConsoleErrorsSmokeTests` 1/1, backend full suite 478/478, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-14-all-screens-browser-smoke`.

- [x] `P11-ACC-005` Security final check. 2026-06-14.
  - Что сделать: secrets, auth, headers, rate limits, permissions.
  - Что сделано: добавлен финальный checklist `docs/security-final-checklist.md` и guard `SecurityFinalChecklistTests`. Проверка связывает уже существующие security gates по секретам, auth/RBAC, headers, rate limits, webhook idempotency, GitHub secrets и provisioning secrets, а также отражением проверяет все admin-контроллеры на отсутствие `AllowAnonymous`, наличие class-level `Authorize` и write/manage policy у write endpoints. Actual secret scan дополнительно защищен от generated Playwright artifacts `test-results` и `.playwright-artifacts-*`, чтобы E2E-прогоны не ломали проверку исчезающими временными файлами.
  - Доказательство: `SecurityFinalChecklistTests` 3/3, targeted security suite, actual `scan-secrets.ps1`, `npm run e2e:console --prefix frontend` 6/6, backend full suite 464/464, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-14-security-final-checklist`; security checklist фиксирует ограничение: перед production нужна ротация любых раскрытых секретов и отдельный VPS smoke.

- [x] `P11-ACC-006` Final docs and changelog. 2026-06-14.
  - Что сделать: обновить README, roadmap, "Что нового", инструкции запуска и deploy.
  - Что сделано: добавлены `CHANGELOG.md` и `docs/final-runbook.md`, README получил прямые ссылки на changelog/runbook, команды `e2e:mobile` и `e2e:console`, актуальный статус проверок и связь с разделом "Что нового". Индекс документации ссылается на changelog и финальный runbook. Добавлен guard `FinalDocsChangelogTests`, который проверяет синхронизацию README, docs index, roadmap, changelog, `TEST_RESULTS.md` и release seed.
  - Доказательство: `FinalDocsChangelogTests` 3/3, documentation guard suite, backend full suite 467/467, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-14-final-docs-changelog`; production-ready решение вынесено в `P11-ACC-007`.

- [x] `P11-ACC-007` Release decision. 2026-06-14.
  - Что сделать: принять решение: sandbox-ready, staging-ready или production-ready.
  - Критерий production-ready: все P0 закрыты, P1 критические сценарии закрыты, validation gate зеленый, VPS smoke успешен.
  - Что сделано: добавлен документ `docs/release-decision.md` и guard `ReleaseDecisionTests`. Решение зафиксировано как `staging-ready baseline, не production-ready`, потому что `P11-ACC-002 VPS production smoke` остается открытым и нет live доказательства полного production сценария на реальном VPS. Документ перечисляет блокеры production-ready: ротация раскрытых секретов, домен/HTTPS, staging PostgreSQL backup/restore, реальные sandbox-кабинеты платежных провайдеров, 3x-ui panel/inbound/access smoke и Telegram bot webhook/invoice flow.
  - Доказательство: `ReleaseDecisionTests` 3/3, release decision documentation guard, backend full suite 478/478, frontend tests 65/65, frontend typecheck/build, local SQLite HTTP-smoke latest release `2026-06-14-all-screens-browser-smoke`, версия `0.107.0`.

- [x] `P11-ACC-008` Production readiness gate. 2026-06-14.
  - Что сделать: добавить fail-closed команду, которая не позволит считать проект production-ready без реального staging/VPS smoke report и закрытых P0/P11/STATE blockers.
  - Что сделано: добавлен `scripts/assert-production-readiness.ps1`, документ `docs/production-readiness-gate.md` и guard `ProductionReadinessGateTests`. Скрипт запускает `validate-staging-smoke-report.ps1 -RequireAllPassed`, затем проверяет `docs/PRODUCT_COMPLETION_ROADMAP.md` и `docs/release-decision.md`; если остаются открытые live-блокеры или решение `staging-ready baseline`, команда падает с `Production readiness blocked`.
  - Доказательство: `ProductionReadinessGateTests` 2/2, fail-closed запуск на текущем шаблоне staging smoke report, backend full suite `486/486`, latest "Что нового" `2026-06-14-production-readiness-gate`, версия `0.112.0`.

- [x] `P11-ACC-009` Production evidence bundle gate. 2026-06-18.
  - Что сделать: усилить production readiness gate так, чтобы он требовал полный пакет evidence: staging/VPS, платежные провайдеры, админка VPS и live VPN/3x-ui.
  - Что сделано: `scripts/assert-production-readiness.ps1` принимает `PaymentProviderReportPath`, `AdminVpsReportPath`, `VpnLiveReportPath`, запускает все профильные валидаторы с `-RequireAllPassed` и включает пути отчетов в summary/blocking payload. Дополнительно закрыт frontend audit до `0 vulnerabilities`, а Playwright webServer helper стал устойчив к hoisted Vite workspace-зависимостям.
  - Доказательство: `ProductionReadinessGateTests` 3/3, expected fail-closed `assert-production-readiness.ps1` на текущих blocked templates, latest "Что нового" `2026-06-18-production-evidence-bundle-gate`, версия `0.124.0`.
- [x] `P11-ACC-010` Production evidence aggregate gate. 2026-06-18.
  - Что сделать: не останавливать production readiness gate на первом blocked evidence report, а показывать оператору полный список недостающих отчетов и roadmap-блокеров.
  - Что сделано: `assert-production-readiness.ps1` запускает все четыре validators через `Invoke-EvidenceValidator`, собирает `evidenceReports` со статусом, путем отчета, путем валидатора и сообщением ошибки, а затем возвращает единый fail-closed payload вместе с roadmap/release blockers.
  - Доказательство: `ProductionReadinessGateTests` 4/4, expected fail-closed `assert-production-readiness.ps1` показывает `staging-vps`, `payment-providers`, `admin-vps`, `vpn-live` и `evidenceReports`, latest "Что нового" `2026-06-18-production-evidence-aggregate-gate`, версия `0.125.0`.
- [x] `P11-ACC-011` Production evidence bundle generator. 2026-06-18.
  - Что сделать: дать оператору одну безопасную команду для создания всех четырех черновиков evidence reports, чтобы не копировать JSON вручную и не забыть часть production gate.
  - Что сделано: добавлен `scripts/new-production-evidence-bundle.ps1`, который создает `staging-smoke-report.json`, `payment-provider-smoke-report.json`, `admin-vps-smoke-report.json` и `vpn-live-smoke-report.json`, запускает их обычные validators и при `-RunProductionGate` возвращает статус агрегированного gate без раскрытия секретов.
  - Доказательство: `ProductionReadinessGateTests` 5/5, generator smoke в `tmp/production-evidence-test`, expected aggregate gate status `blocked`, latest "Что нового" `2026-06-18-production-evidence-bundle-generator`, версия `0.126.0`.
- [x] `P11-ACC-012` Production readiness summary. 2026-06-18.
  - Что сделать: после генерации evidence bundle дать оператору один человекочитаемый Markdown/JSON summary с состоянием четырех отчетов, платежных провайдеров и roadmap-блокеров.
  - Что сделано: добавлен `scripts/new-production-readiness-summary.ps1`, который читает staging/VPS, payment provider, admin VPS и VPN live reports, считает passed/blocked/failed по checks и required flags, выводит платежных провайдеров и открытые production blockers без секретов.
  - Доказательство: `ProductionReadinessGateTests` 6/6, summary smoke в `tmp/production-evidence-summary-test`, status `blocked` для generated drafts, latest "Что нового" `2026-06-18-production-readiness-summary`, версия `0.127.0`.
- [x] `P11-ACC-013` Production readiness summary validator. 2026-06-18.
  - Что сделать: добавить fail-closed проверку Markdown/JSON summary, чтобы operator artifact можно было валидировать отдельно от генератора и использовать в CI или handoff.
  - Что сделано: добавлен `scripts/validate-production-readiness-summary.ps1`, который проверяет summary Markdown, соседний JSON, четыре evidence reports, status/count/flag consistency, roadmap blockers, ссылки на report paths и запрещенные secret markers; для финального запуска есть `-RequireProductionReady`.
  - Доказательство: `ProductionReadinessGateTests` 7/7, validator smoke в `tmp/production-readiness-summary-validator-test`, status `blocked` для generated drafts, latest "Что нового" `2026-06-18-production-readiness-summary-validator`, версия `0.128.0`.
- [x] `P11-ACC-014` Production evidence bundle validator. 2026-06-18.
  - Что сделать: добавить одну fail-closed проверку всего каталога production evidence bundle, чтобы оператор или CI не валидировал четыре отчета и summary вручную.
  - Что сделано: добавлен `scripts/validate-production-evidence-bundle.ps1`, который требует `staging-smoke-report.json`, `payment-provider-smoke-report.json`, `admin-vps-smoke-report.json`, `vpn-live-smoke-report.json`, опционально `production-readiness-summary.md/json`, запускает все validators и поддерживает `-RequireProductionReady`.
  - Доказательство: `ProductionReadinessGateTests` 8/8, bundle validator smoke в `tmp/production-evidence-bundle-validator-test`, latest "Что нового" `2026-06-18-production-evidence-bundle-validator`, версия `0.129.0`.
- [x] `P11-ACC-015` Production evidence manifest. 2026-06-18.
  - Что сделать: добавить безопасный manifest для handoff, чтобы фиксировать состав evidence bundle по SHA256 без копирования содержимого отчетов.
  - Что сделано: добавлен `scripts/new-production-evidence-manifest.ps1`, который валидирует bundle, читает release id, пишет `production-evidence-manifest.json` с relative paths, SHA256, size, timestamps, total files и total bytes.
  - Доказательство: `ProductionReadinessGateTests` 9/9, manifest smoke в `tmp/production-evidence-manifest-test`, generated manifest содержит 6 файлов с SHA256, latest "Что нового" `2026-06-18-production-evidence-manifest`, версия `0.130.0`.
- [x] `P11-ACC-016` Production evidence manifest validator. 2026-06-18.
  - Что сделать: добавить fail-closed проверку `production-evidence-manifest.json`, чтобы оператор, CI или VPS могли доказать, что handoff bundle не изменился после генерации manifest.
  - Что сделано: добавлен `scripts/validate-production-evidence-manifest.ps1`, который читает manifest, проверяет schema, release id, обязательные файлы, relative paths, размеры, timestamps, total files, total bytes и пересчитывает SHA256 каждого файла bundle.
  - Доказательство: `ProductionReadinessGateTests` 10/10, manifest validator smoke в `tmp/production-evidence-manifest-validator-test`, latest "Что нового" `2026-06-18-production-evidence-manifest-validator`, версия `0.131.0`.
- [x] `P11-ACC-017` Production evidence archive. 2026-06-18.
  - Что сделать: добавить безопасную упаковку проверенного production evidence bundle в ZIP, чтобы CI или оператор могли передать один artifact с SHA256 архива и manifest.
  - Что сделано: добавлен `scripts/new-production-evidence-archive.ps1`, который сначала запускает `validate-production-evidence-manifest.ps1`, затем добавляет в архив сам manifest и только перечисленные в manifest файлы, проверяя relative paths и запрет выхода за пределы bundle.
  - Доказательство: `ProductionReadinessGateTests` 11/11, archive smoke в `tmp/production-evidence-archive-test`, latest "Что нового" `2026-06-18-production-evidence-archive`, версия `0.132.0`.
- [x] `P11-ACC-018` Production evidence archive validator. 2026-06-18.
  - Что сделать: добавить fail-closed проверку ZIP-архива production evidence, чтобы опубликованный handoff artifact можно было сверить без распаковки и ручного сравнения.
  - Что сделано: добавлен `scripts/validate-production-evidence-archive.ps1`, который читает manifest из ZIP, запрещает лишние entries, проверяет обязательные файлы, размеры, `totalBytes`, SHA256 каждого entry и опциональный `-ExpectedArchiveSha256`.
  - Доказательство: `ProductionReadinessGateTests` 12/12, archive validator smoke в `tmp/production-evidence-archive-validator-test`, latest "Что нового" `2026-06-18-production-evidence-archive-validator`, версия `0.133.0`.
- [x] `P11-ACC-019` Production evidence handoff receipt. 2026-06-18.
  - Что сделать: добавить JSON/Markdown receipt для проверенного ZIP-архива, чтобы оператор или CI могли передать один ZIP и отдельный hash-чек без содержимого evidence reports.
  - Что сделано: добавлен `scripts/new-production-evidence-handoff-receipt.ps1`, который сначала запускает `validate-production-evidence-archive.ps1`, затем пишет receipt с release id, SHA256 архива, SHA256 manifest, размером, entries и verified files.
  - Доказательство: `ProductionReadinessGateTests` 13/13, handoff receipt smoke в `tmp/production-evidence-handoff-receipt-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-receipt`, версия `0.134.0`.
- [x] `P11-ACC-020` Production evidence handoff receipt validator. 2026-06-18.
  - Что сделать: добавить fail-closed проверку JSON/Markdown receipt против ZIP-архива, чтобы CI или оператор мог доказать, что receipt и архив относятся к одному handoff artifact.
  - Что сделано: добавлен `scripts/validate-production-evidence-handoff-receipt.ps1`, который читает receipt, проверяет Markdown-пару, повторно запускает archive validator и сверяет release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files.
  - Доказательство: `ProductionReadinessGateTests` 14/14, handoff receipt validator smoke в `tmp/production-evidence-handoff-receipt-validator-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-receipt-validator`, версия `0.135.0`.
- [x] `P11-ACC-021` Production evidence handoff checklist. 2026-06-18.
  - Что сделать: добавить операторский checklist поверх проверенного ZIP и receipt, чтобы финальный handoff был проверяемым и fail-closed отличал локальный artifact от production-ready evidence.
  - Что сделано: добавлен `scripts/new-production-evidence-handoff-checklist.ps1`, который запускает receipt validator, читает `production-readiness-summary.json`, пишет JSON/Markdown checklist, фиксирует gates и в строгом режиме `-RequireProductionReady` блокирует handoff без production-ready summary.
  - Доказательство: `ProductionReadinessGateTests` 15/15, handoff checklist smoke в `tmp/production-evidence-handoff-checklist-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-checklist`, версия `0.136.0`.
- [x] `P11-ACC-022` Production evidence handoff checklist validator. 2026-06-18.
  - Что сделать: добавить независимую fail-closed проверку JSON/Markdown checklist против receipt и summary, чтобы оператор или CI могли валидировать финальный handoff artifact после генерации.
  - Что сделано: добавлен `scripts/validate-production-evidence-handoff-checklist.ps1`, который проверяет schema, gates, operator actions, Markdown-пару, повторно запускает receipt validator и в строгом режиме требует production-ready summary.
  - Доказательство: `ProductionReadinessGateTests` 16/16, handoff checklist validator smoke в `tmp/production-evidence-handoff-checklist-validator-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-checklist-validator`, версия `0.137.0`.
- [x] `P11-ACC-023` Production evidence handoff package. 2026-06-18.
  - Что сделать: добавить сборку минимального handoff package из уже проверенных ZIP, receipt и checklist, чтобы оператору или CI не нужно было вручную выбирать файлы для передачи.
  - Что сделано: добавлен `scripts/new-production-evidence-handoff-package.ps1`, который повторно запускает checklist validator, копирует только ZIP/receipt/checklist artifacts, создает `production-evidence-handoff-package-index.json`, `.md` и `SHA256SUMS.txt`.
  - Доказательство: `ProductionReadinessGateTests` 17/17, handoff package smoke в `tmp/production-evidence-handoff-package-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package`, версия `0.138.0`.
- [x] `P11-ACC-024` Production evidence handoff package validator. 2026-06-18.
  - Что сделать: добавить независимую fail-closed проверку готового handoff package, чтобы оператор или CI могли доказать, что переданный каталог содержит только разрешенные artifacts и корректные hashes.
  - Что сделано: добавлен `scripts/validate-production-evidence-handoff-package.ps1`, который проверяет whitelist файлов, index JSON/Markdown, `SHA256SUMS.txt`, пересчитывает SHA256 и повторно запускает checklist validator.
  - Доказательство: `ProductionReadinessGateTests` 18/18, handoff package validator smoke в `tmp/production-evidence-handoff-package-validator-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-validator`, версия `0.139.0`.
- [x] `P11-ACC-025` Production evidence handoff package archive. 2026-06-18.
  - Что сделать: добавить упаковку проверенного handoff package в один ZIP, чтобы оператор или CI могли передать единый архив с SHA256.
  - Что сделано: добавлен `scripts/new-production-evidence-handoff-package-archive.ps1`, который повторно запускает package validator, добавляет в ZIP только разрешенные package files и возвращает SHA256/размер архива, entries, исходный SHA256 production evidence ZIP и SHA256 manifest.
  - Доказательство: `ProductionReadinessGateTests` 19/19, handoff package archive smoke в `tmp/production-evidence-handoff-package-archive-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive`, версия `0.140.0`.
- [x] `P11-ACC-026` Production evidence handoff package archive validator. 2026-06-18.
  - Что сделать: добавить независимую fail-closed проверку ZIP-архива handoff package, чтобы оператор или CI могли доказать, что переданный финальный архив не содержит лишних entries, совпадает по SHA256 и после извлечения проходит package validator.
  - Что сделано: добавлен `scripts/validate-production-evidence-handoff-package-archive.ps1`, который проверяет SHA256 внешнего ZIP, whitelist entries, отсутствие вложенных/опасных путей, временно извлекает package и повторно запускает `validate-production-evidence-handoff-package.ps1`.
  - Доказательство: `ProductionReadinessGateTests` 20/20, handoff package archive validator smoke в `tmp/production-evidence-handoff-package-archive-validator-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-validator`, версия `0.141.0`.
- [x] `P11-ACC-027` Production evidence handoff package archive validator regression harness. 2026-06-18.
  - Что сделать: добавить исполняемый regression harness, который проверяет не только happy path финального ZIP, но и fail-closed поведение при tamper-сценариях.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-validator.ps1`, который проверяет исходный ZIP, затем создает временные испорченные копии и ожидает ошибки для неверного expected SHA256, лишнего `unexpected-entry.txt` и отсутствующего `SHA256SUMS.txt`.
  - Доказательство: `ProductionReadinessGateTests` 21/21, archive validator regression smoke в `tmp/production-evidence-handoff-package-archive-validator-regression-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-validator-regression`, версия `0.142.0`.
- [x] `P11-ACC-028` Production evidence handoff package archive end-to-end flow harness. 2026-06-18.
  - Что сделать: заменить длинную ручную последовательность команд для локального evidence handoff одной исполняемой командой, чтобы не ошибаться в путях receipt/checklist и сразу проверять финальный ZIP.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-flow.ps1`, который собирает evidence bundle, summary, manifest, production evidence ZIP, handoff receipt, checklist, package, финальный ZIP и запускает archive validator regression.
  - Доказательство: `ProductionReadinessGateTests` 22/22, end-to-end flow smoke в `tmp/production-evidence-handoff-package-archive-flow-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-flow`, версия `0.143.0`.
- [x] `P11-ACC-029` Production evidence handoff package archive flow output safety. 2026-06-18.
  - Что сделать: защитить `-Force` в end-to-end flow от случайного рекурсивного удаления корня диска, корня репозитория или неподходящей папки.
  - Что сделано: в `scripts/test-production-evidence-handoff-package-archive-flow.ps1` добавлен `Assert-SafeOutputDirectory`, который требует безопасный output path и явное имя под `production-evidence` artifacts перед очисткой директории.
  - Доказательство: `ProductionReadinessGateTests` 23/23, guarded flow smoke в `tmp/production-evidence-handoff-package-archive-flow-safety-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-flow-safety`, версия `0.144.0`.
- [x] `P11-ACC-030` Production evidence handoff package archive flow result artifacts. 2026-06-18.
  - Что сделать: сохранять итог полного flow в JSON/Markdown artifacts, чтобы CI или оператор могли приложить короткий результат без ручного копирования console output.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-flow.ps1` теперь пишет `production-evidence-handoff-package-archive-flow-result.json` и `.md` с release id, package status, SHA256 архивов, путями artifacts и tamper-сценариями regression harness.
  - Доказательство: `ProductionReadinessGateTests` 24/24, flow result artifacts smoke в `tmp/production-evidence-handoff-package-archive-flow-result-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-flow-result`, версия `0.145.0`.

- [x] `P11-ACC-031` Production evidence handoff package archive flow result validator. 2026-06-18.
  - Что сделать: добавить standalone-валидатор для JSON/Markdown результата полного flow, чтобы итоговые artifacts можно было проверять отдельно от console output.
  - Что сделано: добавлен `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1`; flow запускает его после записи result artifacts. Валидатор сверяет `status`, `regressionStatus`, SHA256 production evidence archive, SHA256 handoff package archive, Markdown-пару, обязательные tamper-сценарии и повторно запускает archive validator.
  - Доказательство: `ProductionReadinessGateTests` 25/25, flow result validator smoke в `tmp/production-evidence-handoff-package-archive-flow-result-validator-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator`, версия `0.146.0`.

- [x] `P11-ACC-032` Production evidence handoff package archive flow result validator regression harness. 2026-06-18.
  - Что сделать: проверить fail-closed поведение validator результата полного flow на испорченных JSON/Markdown artifacts.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-flow-result-validator.ps1`, который сначала валидирует корректный result, затем ожидает ошибки для `bad-status`, неверного SHA256 handoff archive, отсутствующего tamper-сценария и Markdown без блока `Tested failures`. Default-имя handoff package ZIP сокращено через hash release id, чтобы длинные release id не ломали Windows path-limit.
  - Доказательство: `ProductionReadinessGateTests` 26/26, flow result validator regression smoke в `tmp/production-evidence-handoff-package-archive-flow-result-validator-regression-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator-regression`, версия `0.147.0`.

- [x] `P11-ACC-033` Production evidence handoff package archive long path regression. 2026-06-18.
  - Что сделать: закрепить отдельной проверкой Windows path-limit regression для длинных release id и глубокой `OutputDirectory`.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-long-path.ps1`. Harness запускает полный flow, проверяет hash-based имя handoff package ZIP, короткую длину имени, отсутствие полного release id в имени ZIP и сохранение полного release id в result JSON.
  - Доказательство: `ProductionReadinessGateTests` 27/27, long-path regression smoke в `tmp/production-evidence-handoff-package-archive-long-release-id-path-regression-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-long-path-regression`, версия `0.148.0`.

- [x] `P11-ACC-034` Production evidence handoff package archive CI regression wrapper. 2026-06-18.
  - Что сделать: объединить локальные regression harnesses archive flow в одну CI-friendly команду и единый result artifact.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1`. Wrapper запускает основной archive flow, result validator regression и long-path regression, затем сохраняет `production-evidence-handoff-package-archive-ci-regression-result.json` и `.md`.
  - Доказательство: `ProductionReadinessGateTests` 28/28, CI regression wrapper smoke в `tmp/production-evidence-handoff-package-archive-ci-regression-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-ci-regression`, версия `0.149.0`.

- [x] `P11-ACC-035` Production evidence handoff package archive CI workflow. 2026-06-18.
  - Что сделать: подключить CI-friendly wrapper handoff archive regression к GitHub Actions, чтобы regression запускалась в общей validation pipeline.
  - Что сделано: в `.github/workflows/ci.yml` добавлен job `production-evidence`, который зависит от backend, запускает `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` в `pwsh` и публикует JSON/Markdown result artifacts.
  - Доказательство: `ProductionReadinessGateTests` 29/29, workflow guard для `production-evidence`, CI regression wrapper smoke в `tmp/production-evidence-handoff-package-archive-ci-regression-test`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-ci-workflow`, версия `0.150.0`.

- [x] `P11-ACC-036` Production evidence handoff package archive CI summary. 2026-06-18.
  - Что сделать: выводить краткий результат production evidence handoff archive regression прямо в GitHub Actions job summary, чтобы оператор видел статус без скачивания artifacts.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` дописывает Markdown-результат в `GITHUB_STEP_SUMMARY`, если переменная доступна, и продолжает сохранять JSON/Markdown artifacts.
  - Доказательство: `ProductionReadinessGateTests` 30/30, локальный smoke wrapper с `GITHUB_STEP_SUMMARY`, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-ci-summary`, версия `0.151.0`.

- [x] `P11-ACC-037` Production evidence handoff package archive CI summary validator. 2026-06-18.
  - Что сделать: добавить fail-closed проверку CI summary, чтобы Markdown в `GITHUB_STEP_SUMMARY` не расходился с JSON result artifact.
  - Что сделано: добавлен `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1`; wrapper запускает validator для result Markdown и для `GITHUB_STEP_SUMMARY`, если summary-файл доступен.
  - Доказательство: `ProductionReadinessGateTests` 31/31, локальный smoke wrapper с summary validator, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator`, версия `0.152.0`.

- [x] `P11-ACC-038` Production evidence handoff package archive CI summary validator regression harness. 2026-06-18.
  - Что сделать: добавить regression harness для CI summary validator, чтобы проверять fail-closed поведение на испорченном JSON и Markdown.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-ci-summary-validator.ps1`; основной CI wrapper запускает harness, добавляет `ciSummaryValidatorRegression` в JSON/Markdown result artifacts и обновляет `GITHUB_STEP_SUMMARY`.
  - Доказательство: `ProductionReadinessGateTests` 32/32, локальный smoke wrapper с summary validator regression, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator-regression`, версия `0.153.0`.

- [x] `P11-ACC-039` Production evidence handoff package archive CI result validator. 2026-06-18.
  - Что сделать: добавить standalone validator для итогового CI regression result artifact, чтобы скачанный из GitHub Actions JSON/Markdown можно было проверить отдельно от wrapper.
  - Что сделано: добавлен `scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1`; wrapper запускает validator после финальной записи JSON/Markdown и `GITHUB_STEP_SUMMARY`.
  - Доказательство: `ProductionReadinessGateTests` 33/33, standalone CI result validator smoke, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator`, версия `0.154.0`.

- [x] `P11-ACC-040` Production evidence handoff package archive CI result validator regression harness. 2026-06-18.
  - Что сделать: добавить fail-closed regression harness для standalone CI result validator, чтобы итоговый CI artifact проверялся не только happy-path validator, но и испорченными JSON/Markdown сценариями.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1`; основной CI wrapper запускает harness, добавляет `ciResultValidatorRegression` в итоговый JSON/Markdown и повторно валидирует финальный result artifact.
  - Доказательство: `ProductionReadinessGateTests` 34/34, CI result validator regression smoke, latest "Что нового" `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator-regression`, версия `0.155.0`.

- [x] `P11-ACC-041` Production readiness assertion result artifacts. 2026-06-18.
  - Что сделать: добавить JSON/Markdown result artifacts для `assert-production-readiness.ps1`, чтобы CI и оператор получали структурированное доказательство даже при ожидаемом fail-closed `blocked`.
  - Что сделано: `assert-production-readiness.ps1` получил `-OutputPath`, `-JsonOutputPath` и `-Force`; скрипт пишет `production-readiness-assertion.md/json` со статусом, счетчиками failed evidence reports, roadmap blockers, путями отчетов и результатами всех validators, а затем продолжает падать для `blocked`.
  - Доказательство: `ProductionReadinessGateTests` 35/35, blocked assertion artifact smoke, latest "Что нового" `2026-06-18-production-readiness-assertion-result-artifacts`, версия `0.156.0`.

- [x] `P11-ACC-042` Production readiness assertion result validator. 2026-06-18.
  - Что сделать: добавить standalone validator для JSON/Markdown result artifacts `assert-production-readiness.ps1`, чтобы скачанный blocked/production-ready assertion result можно было проверить без повторного запуска gate.
  - Что сделано: добавлен `scripts/validate-production-readiness-assertion-result.ps1`; `assert-production-readiness.ps1` запускает validator сразу после записи artifacts и до fail-closed ошибки.
  - Доказательство: `ProductionReadinessGateTests` 36/36, standalone assertion result validator smoke, latest "Что нового" `2026-06-18-production-readiness-assertion-result-validator`, версия `0.157.0`.

- [x] `P11-ACC-043` Production readiness assertion result validator regression harness. 2026-06-18.
  - Что сделать: добавить исполняемый regression harness для standalone validator `assert-production-readiness.ps1` result artifacts, чтобы проверять fail-closed поведение на испорченных JSON/Markdown без ручной правки файлов.
  - Что сделано: добавлен `scripts/test-production-readiness-assertion-result-validator.ps1`, который валидирует корректный assertion result, затем ожидает ошибки для неверного status, неправильного `failedEvidenceReportsCount`, отсутствующего `vpn-live` evidence report, сломанного Markdown и `-RequireProductionReady` на blocked result.
  - Доказательство: `ProductionReadinessGateTests` 37/37, assertion result validator regression smoke, latest "Что нового" `2026-06-18-production-readiness-assertion-result-validator-regression`, версия `0.158.0`.

- [x] `P11-ACC-044` Production readiness assertion CI regression wrapper. 2026-06-18.
  - Что сделать: добавить CI-friendly wrapper для assertion result artifacts, чтобы GitHub Actions автоматически сохранял blocked/production-ready assertion evidence и проверял validator regression без ручных команд.
  - Что сделано: добавлен `scripts/test-production-readiness-assertion-ci-regression.ps1`; `.github/workflows/ci.yml` получил job `production-readiness-assertion`, который запускается после backend job и публикует assertion JSON/Markdown/log и итоговый CI regression result.
  - Доказательство: `ProductionReadinessGateTests` 38/38, production readiness assertion CI regression smoke, latest "Что нового" `2026-06-18-production-readiness-assertion-ci-regression`, версия `0.159.0`.

- [x] `P11-ACC-045` Production readiness assertion CI regression result validator. 2026-06-19.
  - Что сделать: добавить standalone validator для итогового JSON/Markdown artifact `test-production-readiness-assertion-ci-regression.ps1`, чтобы скачанный из GitHub Actions result можно было проверить отдельно от wrapper.
  - Что сделано: добавлен `scripts/validate-production-readiness-assertion-ci-regression-result.ps1`; CI wrapper запускает validator после записи result JSON/Markdown и до вывода результата.
  - Доказательство: `ProductionReadinessGateTests` 39/39, production readiness assertion CI result validator smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-result-validator`, версия `0.160.0`.

- [x] `P11-ACC-046` Production readiness assertion CI regression result validator regression harness. 2026-06-19.
  - Что сделать: добавить regression harness для standalone validator итогового CI result artifact, чтобы проверять fail-closed поведение на испорченных JSON/Markdown копиях без повторного запуска полного wrapper.
  - Что сделано: добавлен `scripts/test-production-readiness-assertion-ci-regression-result-validator.ps1`; CI wrapper запускает harness, записывает `ciResultValidatorRegression` в result JSON/Markdown и повторно валидирует result artifact.
  - Доказательство: `ProductionReadinessGateTests` 40/40, CI result validator regression smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-result-validator-regression`, версия `0.161.0`.

- [x] `P11-ACC-047` Production readiness assertion CI summary validator. 2026-06-19.
  - Что сделать: добавить fail-closed validator для `GITHUB_STEP_SUMMARY` readiness assertion CI wrapper, чтобы summary job нельзя было сломать отдельно от JSON/Markdown artifacts.
  - Что сделано: добавлены `scripts/validate-production-readiness-assertion-ci-summary.ps1` и `scripts/test-production-readiness-assertion-ci-summary-validator.ps1`; CI wrapper валидирует result Markdown как summary, прогоняет regression harness, записывает `ciSummaryValidatorRegression` и проверяет итоговый GitHub summary, если доступен `GITHUB_STEP_SUMMARY`.
  - Доказательство: `ProductionReadinessGateTests` 41/41, CI summary validator regression smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-summary-validator`, версия `0.162.0`.

- [x] `P11-ACC-048` Production readiness assertion CI GitHub Step Summary smoke. 2026-06-19.
  - Что сделать: добавить локальный smoke для `GITHUB_STEP_SUMMARY`, чтобы доказать, что readiness assertion CI wrapper реально пишет и валидирует job summary, а не только result Markdown artifact.
  - Что сделано: добавлен `scripts/test-production-readiness-assertion-ci-step-summary.ps1`; скрипт выставляет `GITHUB_STEP_SUMMARY`, запускает CI wrapper, валидирует созданный summary через `validate-production-readiness-assertion-ci-summary.ps1`, сверяет его с result Markdown и проверяет строки `ciSummaryValidatorRegression`/`ciResultValidatorRegression`.
  - Доказательство: `ProductionReadinessGateTests` 42/42, CI step summary smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-step-summary-smoke`, версия `0.163.0`.

- [x] `P11-ACC-049` Production readiness assertion CI artifact directory validator. 2026-06-19.
  - Что сделать: добавить одну команду проверки всего artifact-директория readiness assertion CI перед публикацией или после локального запуска, чтобы оператор не валидировал JSON/Markdown/summary вручную по отдельности.
  - Что сделано: добавлен `scripts/validate-production-readiness-assertion-ci-artifacts.ps1`; CI wrapper запускает validator перед выводом результата. Validator проверяет наличие пяти обязательных файлов, согласованность путей в result JSON, result validator, summary validator и optional `StepSummaryPath`.
  - Доказательство: `ProductionReadinessGateTests` 43/43, CI artifacts validator smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-artifacts-validator`, версия `0.164.0`.

- [x] `P11-ACC-050` Production readiness assertion CI artifact directory validator regression. 2026-06-19.
  - Что сделать: добавить fail-closed harness для validator всего artifact-директория, чтобы поврежденный CI bundle нельзя было принять как валидный.
  - Что сделано: добавлен `scripts/test-production-readiness-assertion-ci-artifacts-validator.ps1`; CI wrapper запускает harness автоматически и пишет `ciArtifactsValidatorRegression` в итоговый JSON/Markdown. Harness проверяет `missing-required-artifact`, `bad-output-directory`, `bad-assertion-log-path`, `bad-result-markdown` и `bad-step-summary`; CI result/summary validators проверяют этот новый regression-блок.
  - Доказательство: `ProductionReadinessGateTests` 44/44, CI artifacts validator regression smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-artifacts-validator-regression`, версия `0.165.0`.

- [x] `P11-ACC-051` Production readiness assertion CI summary artifacts regression. 2026-06-19.
  - Что сделать: закрыть regression gap, при котором GitHub Step Summary мог потерять строку `CI artifacts validator regression`, а summary validator regression не проверял этот tamper-сценарий отдельно.
  - Что сделано: `scripts/test-production-readiness-assertion-ci-summary-validator.ps1` проверяет `bad-ci-artifacts-validator-regression`; CI wrapper запускает artifacts regression до summary regression, а `validate-production-readiness-assertion-ci-regression-result.ps1` требует этот failure-case внутри `ciSummaryValidatorRegression`.
  - Доказательство: `ProductionReadinessGateTests` 45/45, CI summary artifacts regression smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-summary-artifacts-regression`, версия `0.166.0`.

- [x] `P11-ACC-052` Production readiness assertion CI workflow artifacts guard. 2026-06-19.
  - Что сделать: закрепить, что GitHub Actions job публикует полный readiness assertion CI artifact-директорий, а не только часть файлов.
  - Что сделано: добавлен `scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1`; guard проверяет `.github/workflows/ci.yml`, job `production-readiness-assertion`, `needs: backend`, запуск wrapper, `actions/upload-artifact@v4`, `if-no-files-found: error` и пять обязательных файлов artifact-директория.
  - Доказательство: `ProductionReadinessGateTests` 46/46, CI workflow artifacts guard smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-workflow-artifacts`, версия `0.167.0`.

- [x] `P11-ACC-053` Production readiness assertion CI workflow guard step. 2026-06-19.
  - Что сделать: закрепить, что GitHub Actions не только описывает published artifacts, но и запускает workflow artifacts guard внутри job до readiness assertion wrapper.
  - Что сделано: в `.github/workflows/ci.yml` добавлен step `Guard production readiness assertion workflow artifacts`, который запускает `scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson` до `Run production readiness assertion CI regression`; `ProductionReadinessGateTests` проверяет наличие step, команду и порядок guard -> wrapper -> upload.
  - Доказательство: `ProductionReadinessGateTests` 47/47, CI workflow artifacts guard smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-workflow-guard-step`, версия `0.168.0`.

- [x] `P11-ACC-054` Production evidence CI workflow artifacts guard. 2026-06-19.
  - Что сделать: закрепить published artifacts contract для GitHub Actions job `production-evidence`, чтобы CI не мог молча перестать публиковать JSON/Markdown result artifacts handoff archive regression.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1`; в `.github/workflows/ci.yml` добавлен step `Guard production evidence workflow artifacts`, который запускает guard до `Run production evidence handoff archive CI regression`; `ProductionReadinessGateTests` проверяет guard-скрипт, workflow step, команду и порядок guard -> wrapper -> upload.
  - Доказательство: `ProductionReadinessGateTests` 48/48, production evidence workflow artifacts guard smoke, latest "Что нового" `2026-06-19-production-evidence-ci-workflow-artifacts-guard`, версия `0.169.0`.

- [x] `P11-ACC-055` Production evidence CI workflow artifacts guard regression. 2026-06-19.
  - Что сделать: добавить fail-closed regression harness для workflow artifacts guard, чтобы поврежденный `.github/workflows/ci.yml` не мог пройти локальную проверку.
  - Что сделано: добавлен `scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1`; harness копирует workflow во временную директорию, проверяет happy path, затем ожидает ошибки для `missing-guard-step`, `missing-result-json-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.
  - Доказательство: `ProductionReadinessGateTests` 49/49, production evidence workflow artifacts guard validator smoke, latest "Что нового" `2026-06-19-production-evidence-ci-workflow-artifacts-guard-regression`, версия `0.170.0`.

- [x] `P11-ACC-056` Production readiness assertion CI workflow artifacts guard regression. 2026-06-19.
  - Что сделать: добавить fail-closed regression harness для readiness assertion workflow artifacts guard, чтобы поврежденный `.github/workflows/ci.yml` не мог пройти локальную проверку.
  - Что сделано: добавлен `scripts/test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1`; harness копирует workflow во временную директорию, проверяет happy path, затем ожидает ошибки для `missing-guard-step`, `missing-assertion-log-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.
  - Доказательство: `ProductionReadinessGateTests` 50/50, readiness assertion workflow artifacts guard validator smoke, latest "Что нового" `2026-06-19-production-readiness-assertion-ci-workflow-artifacts-guard-regression`, версия `0.171.0`.

- [x] `P11-ACC-057` Production CI workflow artifacts guards aggregate. 2026-06-19.
  - Что сделать: добавить один локальный и CI-friendly entrypoint для проверки всех production workflow artifacts guards, чтобы release handoff не зависел от ручного запуска четырех отдельных команд.
  - Что сделано: добавлен `scripts/test-production-ci-workflow-artifacts-guards.ps1`; aggregate запускает readiness assertion guard, readiness assertion fail-closed validator, production evidence guard и production evidence fail-closed validator; GitHub Actions backend job запускает aggregate step `Guard production CI workflow artifacts contracts` сразу после checkout.
  - Доказательство: `ProductionReadinessGateTests` 51/51, production CI workflow artifacts guards aggregate smoke, latest "Что нового" `2026-06-19-production-ci-workflow-artifacts-guards-aggregate`, версия `0.172.0`.

- [x] `P11-ACC-058` Production CI workflow artifacts aggregate guard regression. 2026-06-19.
  - Что сделать: добавить fail-closed regression harness для aggregate guard, чтобы общий entrypoint не мог пройти при сломанном readiness assertion или production evidence published artifact contract.
  - Что сделано: добавлен `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1`; harness копирует workflow во временную директорию, проверяет happy path aggregate guard, затем ожидает ошибки для `missing-readiness-guard-step`, `missing-readiness-assertion-log-artifact`, `missing-production-evidence-result-artifact` и `missing-if-no-files-found-error`.
  - Доказательство: `ProductionReadinessGateTests` 52/52, production CI workflow artifacts aggregate guard validator smoke, latest "Что нового" `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression`, версия `0.173.0`.

- [x] `P11-ACC-059` Production CI workflow artifacts aggregate regression CI step. 2026-06-19.
  - Что сделать: запускать fail-closed aggregate validator в GitHub Actions до backend setup/build/test, чтобы CI проверял не только happy path aggregate guard, но и tamper regression.
  - Что сделано: в `.github/workflows/ci.yml` добавлен step `Guard production CI workflow artifacts contracts regression`, который запускает `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson` сразу после aggregate guard и до `Setup .NET SDK from global.json`; `ProductionReadinessGateTests` проверяет порядок aggregate guard -> aggregate validator -> backend setup.
  - Доказательство: `ProductionReadinessGateTests` 53/53, production CI workflow artifacts aggregate validator CI step guard, latest "Что нового" `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression-ci-step`, версия `0.174.0`.

- [x] `P11-ACC-060` Production CI workflow artifacts aggregate CI step guard. 2026-06-19.
  - Что сделать: добавить быстрый guard для CI wiring aggregate workflow artifacts steps, чтобы `.github/workflows/ci.yml` не мог потерять aggregate guard или aggregate validator перед backend setup.
  - Что сделано: добавлен `scripts/test-production-ci-workflow-artifacts-guards-ci-step.ps1`; guard проверяет backend job, aggregate guard step, aggregate validator step, команды `-WriteJson` и порядок guard -> validator -> `Setup .NET SDK from global.json`; GitHub Actions запускает этот guard step `Guard production CI workflow artifacts guard steps` сразу после checkout.
  - Доказательство: `ProductionReadinessGateTests` 54/54, production CI workflow artifacts aggregate CI step guard smoke, latest "Что нового" `2026-06-19-production-ci-workflow-artifacts-guards-ci-step-guard`, версия `0.175.0`.

- [x] `P11-ACC-061` Production CI workflow artifacts aggregate CI step guard regression. 2026-06-19.
  - Что сделать: добавить fail-closed regression harness для aggregate CI step guard, чтобы tampered `.github/workflows/ci.yml` не мог пройти локальную и CI-проверку при удалении guard step, команды validator или нарушении порядка.
  - Что сделано: добавлен `scripts/test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1`; harness запускает happy path guard и ожидает ошибки для `missing-ci-step-guard`, `missing-ci-step-guard-command`, `missing-ci-step-validator`, `ci-step-guard-after-aggregate-guard`; GitHub Actions запускает step `Guard production CI workflow artifacts guard steps regression` до aggregate guard.
  - Доказательство: `ProductionReadinessGateTests` 55/55, production CI workflow artifacts aggregate CI step guard validator smoke, latest "Что нового" `2026-06-19-production-ci-workflow-artifacts-guards-ci-step-regression`, версия `0.176.0`.

- [x] `P11-ACC-062` Production CI workflow artifacts aggregate includes CI step guards. 2026-06-19.
  - Что сделать: расширить общий `scripts/test-production-ci-workflow-artifacts-guards.ps1`, чтобы одна команда проверяла не только readiness/evidence workflow artifacts guards, но и aggregate CI step guard вместе с его fail-closed validator.
  - Что сделано: aggregate entrypoint теперь запускает `production-ci-workflow-artifacts-ci-step`, `production-ci-workflow-artifacts-ci-step-validator`, readiness assertion guard, readiness assertion validator, production evidence guard и production evidence validator; итоговый JSON возвращает `guardsCount = 6`.
  - Доказательство: `ProductionReadinessGateTests` 56/56, production CI workflow artifacts guards aggregate smoke с `guardsCount = 6`, latest "Что нового" `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards`, версия `0.177.0`.

- [x] `P11-ACC-063` Production CI workflow artifacts aggregate CI step guard regression coverage. 2026-06-19.
  - Что сделать: расширить aggregate fail-closed validator, чтобы общий tamper harness проверял не только readiness/evidence artifacts, но и новые aggregate CI step guard checks.
  - Что сделано: `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1` теперь ожидает ошибки для `missing-aggregate-ci-step-guard-command` и `missing-aggregate-ci-step-validator`; эти сценарии ломают CI-step guard command и regression step внутри `.github/workflows/ci.yml`.
  - Доказательство: `ProductionReadinessGateTests` 57/57, production CI workflow artifacts aggregate validator CI-step tamper smoke, latest "Что нового" `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards-regression`, версия `0.178.0`.

- [x] `P11-ACC-064` VPS production smoke report contract. 2026-06-19.
  - Что сделать: добавить проверяемый report contract для live/staging VPS production smoke, чтобы `P11-ACC-002` закрывался только безопасным отчетом полного deploy -> health -> admin login -> order -> payment -> subscription -> VPN access flow.
  - Что сделано: добавлены `docs/vps-production-smoke-report.template.json`, `scripts/new-vps-production-smoke-report.ps1` и `scripts/validate-vps-production-smoke-report.ps1`; validator fail-closed требует все обязательные шаги, boolean-подтверждения, валидные URL/даты, `-RequireAllPassed` для приемки и запрещает секретные маркеры, raw webhook payloads и полные VPN URI в evidence.
  - Доказательство: `VpsProductionSmokeTests` 7/7, generator/validator smoke, expected fail-closed `-RequireAllPassed`, latest "Что нового" `2026-06-19-vps-production-smoke-report-contract`, версия `0.179.0`.

- [x] `P11-ACC-065` VPS production smoke report latest release guard. 2026-07-01.
  - Что сделать: не принимать VPS production smoke report как финальный acceptance evidence, если отчет был заполнен для старого release.
  - Что сделано: `scripts/validate-vps-production-smoke-report.ps1 -RequireAllPassed` сверяет `releaseId` отчета с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`; добавлен regression harness `scripts/test-vps-production-smoke-report-latest-release-guard.ps1`, который доказывает fail-closed поведение на полностью passed отчете со stale `releaseId`.
  - Доказательство: `VpsProductionSmokeTests` 8/8, VPS production smoke latest release guard regression, latest "Что нового" `2026-07-01-vps-production-smoke-latest-release-guard`, версия `0.302.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-066` Production handoff checklist latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-evidence-handoff-checklist.ps1 -RequireProductionReady` сверяет `releaseId` checklist с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому production-ready handoff на старом release падает до acceptance.
  - Доказательство: `ProductionReadinessGateTests` 58/58, `scripts/test-production-evidence-handoff-checklist-latest-release-guard.ps1`, production handoff checklist latest release guard regression, backend full suite `600/600`, latest "Что нового" `2026-07-01-production-handoff-checklist-latest-release-guard`, версия `0.305.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-067` Production handoff package latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-evidence-handoff-package.ps1 -RequireProductionReady` сверяет `releaseId` package index с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому финальный handoff package на старом release падает до проверки checksums/checklist.
  - Доказательство: `ProductionReadinessGateTests` 59/59, `scripts/test-production-evidence-handoff-package-latest-release-guard.ps1`, production handoff package latest release guard regression, backend full suite `601/601`, latest "Что нового" `2026-07-01-production-handoff-package-latest-release-guard`, версия `0.306.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-068` Production handoff archive flow result latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1 -RequireProductionReady` сверяет `releaseId` flow result с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому stale CI/operator result падает до проверки архивов и Markdown.
  - Доказательство: `ProductionReadinessGateTests` 60/60, `scripts/test-production-evidence-handoff-package-archive-flow-result-latest-release-guard.ps1`, production handoff flow result latest release guard regression, backend full suite `602/602`, latest "Что нового" `2026-07-01-production-handoff-flow-result-latest-release-guard`, версия `0.307.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-069` Production handoff CI regression result latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1 -RequireProductionReady` сверяет `releaseId` CI result с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому stale downloaded CI artifact падает до проверки Markdown и artifact paths.
  - Доказательство: `ProductionReadinessGateTests` 61/61, `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-latest-release-guard.ps1`, production handoff CI result latest release guard regression, backend full suite `603/603`, latest "Что нового" `2026-07-01-production-handoff-ci-result-latest-release-guard`, версия `0.308.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-070` Production handoff CI summary latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1 -RequireProductionReady` сверяет `releaseId` CI summary result с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому stale GitHub Actions summary падает до проверки Markdown и artifact paths.
  - Доказательство: `ProductionReadinessGateTests` 62/62, `scripts/test-production-evidence-handoff-package-archive-ci-summary-latest-release-guard.ps1`, production handoff CI summary latest release guard regression, backend full suite `604/604`, latest "Что нового" `2026-07-01-production-handoff-ci-summary-latest-release-guard`, версия `0.309.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-071` Production readiness summary latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-readiness-summary.ps1 -RequireProductionReady` сверяет `releaseId` summary с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому stale production-ready summary падает до handoff checklist/package.
  - Доказательство: `ProductionReadinessGateTests` 63/63, `scripts/test-production-readiness-summary-latest-release-guard.ps1`, production readiness summary latest release guard regression, backend full suite `605/605`, latest "Что нового" `2026-07-01-production-readiness-summary-latest-release-guard`, версия `0.310.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-072` Production readiness assertion result latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-readiness-assertion-result.ps1 -RequireProductionReady` сверяет `releaseId` assertion result с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому stale production-ready assertion artifact падает до проверки linked reports и Markdown.
  - Доказательство: `ProductionReadinessGateTests` 64/64, `scripts/test-production-readiness-assertion-result-latest-release-guard.ps1`, production readiness assertion result latest release guard regression, backend full suite `606/606`, latest "Что нового" `2026-07-01-production-readiness-assertion-latest-release-guard`, версия `0.311.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

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
| BUG-004 | P1 | Frontend | Нет полного browser E2E по public/cabinet/admin | Исправлено | Закрыто через `P9-TST-008`, `AllScreensBrowserSmokeTests`, `npm run e2e:all-screens --prefix frontend` и `npm run e2e:console --prefix frontend`. |
| BUG-005 | P1 | Docs | Часть roadmap/docs устарела, возможен mojibake в старых `.md` | Исправлено | Закрыто через `P10-DOC-005`, `STATE-014`, `DocumentationEncodingTests`, `RoadmapCurrentStateTests` и `BugRegisterConsistencyTests`. |
| BUG-006 | P1 | Provisioning | Live Ansible provisioning не production-ready из-за secret materialization | Исправлено | Безопасная временная материализация protected `ssh_key` реализована через `ProvisioningSecretMaterializer`, `AnsibleProvisioningExecutor` передает runner только временный path и удаляет файл в `finally`; live VPS/provisioning smoke остается отдельным P0/P11-блокером. |
