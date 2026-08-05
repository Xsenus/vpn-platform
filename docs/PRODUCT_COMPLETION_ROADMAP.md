# Master roadmap: доведение VPN Platform до production-ready

Документ нужен как единая рабочая карта проекта. По нему агент или разработчик должен идти сверху вниз, отмечать выполненные пункты и оставлять доказательства: тесты, скриншоты, ссылки на коммиты, результаты smoke-проверок и замечания.

Дата актуализации: 2026-08-05.

Дата последней сверки: 2026-08-05.

Временный статус работы с roadmap: активная локальная доработка синхронизирована до `2026-08-05-provisioning-owner-actor-boundary`, версия `0.512.0`. Roadmap остается staging-ready baseline, не production-ready: закрыто `525/545` проверяемых пунктов, готовность `96.3%`, осталось `20`, открыто `19`, в работе `1`, блокеров `[!]` нет. Дальше нельзя закрывать `STATE-011`, `STATE-012`, `STATE-013`, `P0-ADMIN-001`, `P0-ADMIN-002`, `P0-VPN-*`, `P0-PAY-*`, `P9-TST-007` и `P11-ACC-002` без реального VPS/staging/live evidence.

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

Что подтверждено на 2026-08-05:

- [x] `STATE-001` Backend test suite проходит: `1060/1060`.
- [x] `STATE-002` Frontend test suite проходит: `84/84`.
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
- [x] `STATE-014` Roadmap и текущие статусные документы синхронизированы с проверками 2026-08-05.
  - Что сделано: верхний статус roadmap, README, final runbook, release decision, changelog, TEST_RESULTS, product/admin UI roadmap и seed "Что нового" приведены к одному состоянию: backend `1060/1060`, frontend `84/84`, browser console/responsive smoke `16/16`, dependency audit `0 vulnerabilities`, latest release `2026-08-05-provisioning-owner-actor-boundary`, версия `0.512.0`.
  - Что осталось: live-платежи, реальная 3x-ui выдача, VPS admin/live smoke и production-ready решение остаются отдельными открытыми задачами `STATE-011`, `STATE-012`, `STATE-013`, `P11-ACC-002` и P0.
  - Доказательство: `RoadmapCurrentStateTests` 13/13, `BugRegisterConsistencyTests` 2/2, `ProvisioningSecretStatusConsistencyTests` 1/1, `ProductAdminUiRoadmapSyncTests` 3/3, `ReleaseDecisionTests` 4/4, `ProductionReadinessGateTests` 119/119, `VpsProductionSmokeTests` 12/12, `StagingSmokeChecklistTests` 13/13, `PaymentProviderSmokeReportTests` 11/11, `AdminVpsSmokeReportTests` 28/28, `AdminBootstrapCliScriptTests` 21/21, `VpnLiveSmokeReportTests` 9/9, `ChannelWebhooksControllerTests` 2/2, `ReadmeDocumentationTests`, `FinalDocsChangelogTests`, `DocumentationEncodingTests`, local SQLite VPS smoke dry-run, fresh local SQLite smoke, local admin browser smoke через end-to-end wrapper, local CLI bootstrap admin smoke, admin bootstrap wrapper cleanup, admin VPS bootstrap smoke evidence validator cleanup, admin VPS smoke flow wrapper cleanup, admin VPS smoke report validator cleanup, admin VPS smoke preflight validator cleanup, admin VPS smoke evidence validator cleanup, staging smoke latest release guard cleanup, staging smoke generator release guard cleanup, staging smoke report self-link, payment provider smoke report self-link, VPN live smoke report self-link, production readiness summary self-link, production readiness assertion result self-link, roadmap progress counter guard, roadmap progress percent guard, roadmap progress remaining guard, agent instructions guard, external evidence open guard, product UI external evidence open guard, release decision external evidence open guard, final runbook external evidence open guard, test results external evidence open guard, changelog external evidence open guard, README external evidence open guard, product/admin UI roadmap external evidence open guard, roadmap external evidence open-set guard, status docs latest release seed guard, changelog/test results top release guard, latest release seed order guard, release seed identity guard, release seed version order guard, release seed file order guard, release seed secret literal guard, status docs production-ready claim guard, latest release evidence caveat guard, docs strict UTF-8 guard, agent instructions readable UTF-8 guard, source mojibake guard, changelog mojibake guard, project files UTF-8 guard, dotfiles UTF-8 guard, env example UTF-8 guard, deploy/frontend text UTF-8 guard, all markdown UTF-8 guard, status docs progress consistency guard, what's new progress consistency guard, agent source/version reporting guard, agent unavailable checks risk guard, agent git delivery guard, agent image attachment guard, agent duplicate task guard, agent local DB scope guard, agent verification handoff guard, agent encoding verification guard, agent customer chat image comment guard, agent end-to-end completion guard, agent external evidence boundary guard, production evidence bundle generator release guard regression, production evidence bundle generator release guard cleanup, production evidence bundle latest release guard regression, production evidence bundle latest release guard cleanup, production evidence manifest release guard regression, production evidence manifest release guard cleanup, production evidence archive release guard regression, production evidence archive release guard cleanup, production evidence handoff receipt release guard regression, production evidence handoff receipt release cleanup, production evidence handoff checklist release guard regression, production evidence handoff checklist release guard cleanup, production evidence handoff package latest release guard cleanup, production evidence handoff receipt verified files guard regression, production evidence handoff receipt verified files cleanup, production evidence handoff receipt markdown verified files guard regression, production evidence handoff receipt markdown verified files guard cleanup, production evidence handoff checklist markdown gates guard regression, production evidence handoff checklist markdown gates guard cleanup, production evidence handoff package markdown files guard regression, production evidence handoff package markdown files guard cleanup, production evidence handoff package archive duplicate entry regression, production evidence handoff package archive duplicate entry cleanup, production evidence handoff package archive nested entry regression, production evidence handoff package archive nested entry cleanup, production evidence handoff package archive directory entry regression, production evidence handoff package archive directory entry cleanup, production evidence handoff package archive backslash entry regression, production evidence handoff package archive backslash entry cleanup, production evidence handoff package archive dotdot entry regression, production evidence handoff package archive dotdot entry cleanup, production evidence handoff package archive dot entry regression, production evidence handoff package archive dot entry cleanup, production evidence handoff package archive rooted entry regression, production evidence handoff package archive rooted entry cleanup, production evidence handoff package archive whitespace entry regression, production evidence handoff package archive entry case regression, production evidence handoff package archive entry case cleanup, admin VPS bootstrap readiness release guard regression, admin VPS smoke preflight release guard regression, admin VPS browser smoke direct release guard regression, admin VPS browser smoke direct release guard cleanup, production readiness CI step summary cleanup, VPS production smoke generator release guard regression, VPS production smoke generator release guard cleanup, VPS production smoke latest release guard cleanup, staging smoke generator release guard regression, payment provider smoke generator release guard regression, VPN live smoke generator release guard regression, VPN live smoke generator release guard cleanup, admin VPS smoke generator release guard regression, admin VPS bootstrap smoke evidence latest release guard regression, admin VPS smoke evidence latest release guard regression, admin VPS smoke preflight latest release guard regression, production handoff package archive latest release guard regression, production handoff package archive latest release guard cleanup, production readiness assertion result latest release guard regression, production readiness summary latest release guard regression, production handoff CI summary latest release guard regression, production handoff CI summary latest release guard cleanup, production handoff CI result latest release guard regression, production handoff CI result latest release guard cleanup, production handoff flow result latest release guard regression, production handoff flow result latest release guard cleanup, production handoff package latest release guard regression, production handoff checklist latest release guard regression, admin VPS bootstrap smoke latest release guard regression, admin VPS bootstrap release guard cleanup, deploy production env normalizer regression, admin VPS smoke navigation fallback regression, admin VPS smoke remote release preflight/diagnostics/console-summary/remote-message/report-id-console/failed-checks/failed-count/check-counts regression, admin VPS smoke latest release guard regression, admin VPS smoke release guard cleanup, staging smoke latest release guard regression, payment provider smoke latest release regression, VPN live smoke latest release regression, VPN live smoke latest release cleanup, VPS production smoke latest release regression, real VPS admin smoke negative evidence for stale release/missing `audit` section, fresh local smoke default artifact cleanup, local admin VPS browser smoke cleanup, local admin VPS bootstrap smoke cleanup, admin VPS bootstrap smoke readiness cleanup, admin VPS smoke sections contract cleanup, deploy production env normalizer cleanup, docker validation tmp cleanup, deploy VPS Docker tmp cleanup, CI Ansible syntax-check tmp cleanup, validate_repo Ansible tmp cleanup, provision-node wrapper cleanup, VPS production smoke report self-link, backend full suite `737/737`, frontend tests `66/66`, latest "Что нового" `2026-07-02-agent-external-evidence-boundary-guard`, версия `0.454.0`.

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

- [x] `P0-ADMIN-002BD` Add latest release guard to admin VPS smoke preflight acceptance. 2026-07-01.
  - Done: `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady` now rejects ready preflight reports whose `releaseId` does not match the latest active release in `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
  - Evidence: `AdminVpsSmokeReportTests` 17/17, `scripts/test-admin-vps-smoke-preflight-latest-release-guard.ps1`, admin VPS smoke preflight latest release guard regression, backend full suite `609/609`, latest "Что нового" `2026-07-01-admin-vps-smoke-preflight-latest-release-guard`, version `0.314.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin smoke evidence is captured.

- [x] `P0-ADMIN-002BE` Add latest release guard to admin VPS smoke evidence acceptance. 2026-07-01.
  - Done: `scripts/validate-admin-vps-smoke-evidence.ps1` now rejects paired preflight/browser smoke evidence whose `releaseId` does not match the latest active release in `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
  - Evidence: `AdminVpsSmokeReportTests` 18/18, `scripts/test-admin-vps-smoke-evidence-latest-release-guard.ps1`, admin VPS smoke evidence latest release guard regression, backend full suite `610/610`, latest "Что нового" `2026-07-01-admin-vps-smoke-evidence-latest-release-guard`, version `0.315.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin smoke evidence is captured.

- [x] `P0-ADMIN-002BF` Add known release guard to admin VPS smoke report generator. 2026-07-01.
  - Done: `scripts/new-admin-vps-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a draft report.
  - Evidence: `AdminVpsSmokeReportTests` 19/19, `scripts/test-admin-vps-smoke-report-generator-release-guard.ps1`, admin VPS smoke generator release guard regression, backend full suite `612/612`, latest "Что нового" `2026-07-01-admin-vps-smoke-generator-release-guard`, version `0.317.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin smoke evidence is captured.

- [x] `P0-ADMIN-002BG` Add known release guard to direct admin VPS browser smoke. 2026-07-01.
  - Done: `scripts/admin-vps-browser-smoke.ps1 -ReleaseId` now rejects unknown manual release ids before setting smoke environment variables, running Playwright or writing report artifacts.
  - Evidence: `AdminVpsSmokeReportTests` 20/20, `scripts/test-admin-vps-browser-smoke-direct-release-guard.ps1`, admin VPS browser smoke direct release guard regression, backend full suite `617/617`, latest "Что нового" `2026-07-01-admin-vps-browser-smoke-direct-release-guard`, version `0.322.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin smoke evidence is captured.

- [x] `P0-ADMIN-002BH` Add known release guard to direct admin VPS smoke preflight. 2026-07-01.
  - Done: `scripts/admin-vps-smoke-preflight.ps1 -ReleaseId` now rejects unknown manual release ids before remote release checks, validator execution or writing preflight artifacts.
  - Evidence: `AdminVpsSmokeReportTests` 21/21, `scripts/test-admin-vps-smoke-preflight-release-guard.ps1`, admin VPS smoke preflight release guard regression, backend full suite `618/618`, latest "Что нового" `2026-07-01-admin-vps-smoke-preflight-release-guard`, version `0.323.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin smoke evidence is captured.

- [x] `P0-ADMIN-002BI` Admin VPS smoke generator release guard default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke generator release guard regression не должен оставлять пустой `tmp` после локального запуска с unknown release id.
  - Что сделано: `scripts/test-admin-vps-smoke-report-generator-release-guard.ps1` удаляет пустой `tmp`, сохраняя fail-closed проверку unknown manual release id без созданного JSON report.
  - Доказательство: `AdminVpsSmokeReportTests` 22/22, admin VPS smoke release guard cleanup, backend full suite `678/678`, latest "Что нового" `2026-07-02-admin-vps-smoke-release-guard-cleanup`, версия `0.382.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BJ` Admin VPS smoke latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke latest release guard regression не должен оставлять stale-release JSON и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-admin-vps-smoke-report-latest-release-guard.ps1` удаляет stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку stale release id в `-RequireAllPassed` режиме.
  - Доказательство: `AdminVpsSmokeReportTests` 22/22, admin VPS smoke release guard cleanup, backend full suite `678/678`, latest "Что нового" `2026-07-02-admin-vps-smoke-release-guard-cleanup`, версия `0.382.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BK` Admin VPS smoke preflight latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke preflight latest release guard regression не должен оставлять stale-release JSON и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-admin-vps-smoke-preflight-latest-release-guard.ps1` удаляет stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку stale release id в `-RequireReady` режиме.
  - Доказательство: `AdminVpsSmokeReportTests` 22/22, admin VPS smoke release guard cleanup, backend full suite `678/678`, latest "Что нового" `2026-07-02-admin-vps-smoke-release-guard-cleanup`, версия `0.382.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BL` Admin VPS smoke preflight release guard default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke preflight release guard regression не должен оставлять пустой `tmp` после локального запуска с unknown release id.
  - Что сделано: `scripts/test-admin-vps-smoke-preflight-release-guard.ps1` удаляет пустой `tmp`, сохраняя fail-closed проверку unknown manual release id без preflight/smoke JSON artifacts и без утечки пароля.
  - Доказательство: `AdminVpsSmokeReportTests` 22/22, admin VPS smoke release guard cleanup, backend full suite `678/678`, latest "Что нового" `2026-07-02-admin-vps-smoke-release-guard-cleanup`, версия `0.382.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BM` Admin VPS smoke evidence latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke evidence latest release guard regression не должен оставлять paired stale-release JSON и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-admin-vps-smoke-evidence-latest-release-guard.ps1` удаляет paired preflight/smoke JSON и пустой `tmp`, сохраняя fail-closed проверку stale release id в evidence chain.
  - Доказательство: `AdminVpsSmokeReportTests` 22/22, admin VPS smoke release guard cleanup, backend full suite `678/678`, latest "Что нового" `2026-07-02-admin-vps-smoke-release-guard-cleanup`, версия `0.382.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BN` Admin VPS browser smoke direct release guard default artifact cleanup. 2026-07-02.
  - Что сделать: direct admin VPS browser smoke release guard regression не должен оставлять пустой `tmp` после локального запуска с unknown release id.
  - Что сделано: `scripts/test-admin-vps-browser-smoke-direct-release-guard.ps1` удаляет пустой `tmp`, сохраняя fail-closed проверку unknown manual release id без browser smoke JSON artifact, без запуска Playwright и без утечки пароля.
  - Доказательство: `AdminVpsSmokeReportTests` 22/22, admin VPS browser smoke direct release guard cleanup, backend full suite `679/679`, latest "Что нового" `2026-07-02-admin-vps-browser-smoke-direct-release-guard-cleanup`, версия `0.384.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BO` Admin VPS smoke report validator latest release and default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke report validator regression должен проходить после смены latest active release и не оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-vps-smoke-report-validator.ps1` берет latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, удаляет default output directory и пустой `tmp` без `-KeepArtifacts`, сохраняя artifacts для явного debug режима.
  - Доказательство: `AdminVpsSmokeReportTests` 23/23, admin VPS smoke report validator cleanup, backend full suite `682/682`, latest "Что нового" `2026-07-02-admin-vps-smoke-report-validator-cleanup`, версия `0.387.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002BP` Admin VPS smoke preflight validator default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke preflight validator regression не должен оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-vps-smoke-preflight-validator.ps1` удаляет default output directory и пустой `tmp` без `-KeepArtifacts`, сохраняя artifacts для явного debug режима.
  - Доказательство: `AdminVpsSmokeReportTests` 24/24, admin VPS smoke preflight validator cleanup, backend full suite `683/683`, latest "Что нового" `2026-07-02-admin-vps-smoke-preflight-validator-cleanup`, версия `0.388.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BQ` Admin VPS smoke evidence validator latest release and default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke evidence validator regression должен проходить после смены latest active release и не оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-vps-smoke-evidence-validator.ps1` берет latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, дополняет synthetic preflight required counters/remote release fields, удаляет default output directory и пустой `tmp` без `-KeepArtifacts`, сохраняя artifacts для явного debug режима.
  - Доказательство: `AdminVpsSmokeReportTests` 25/25, admin VPS smoke evidence validator cleanup, backend full suite `684/684`, latest "Что нового" `2026-07-02-admin-vps-smoke-evidence-validator-cleanup`, версия `0.389.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BR` Admin VPS smoke flow wrapper default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke flow wrapper regression не должен оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-vps-smoke-flow-wrapper.ps1` удаляет default output directory и пустой `tmp` без `-KeepArtifacts`, сохраняя artifacts для явного debug режима.
  - Доказательство: `AdminVpsSmokeReportTests` 26/26, admin VPS smoke flow wrapper cleanup, backend full suite `686/686`, latest "Что нового" `2026-07-02-admin-vps-smoke-flow-wrapper-cleanup`, версия `0.391.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-002BS` Local admin VPS browser smoke default artifact cleanup. 2026-07-02.
  - Что сделать: local admin VPS browser smoke не должен оставлять пустой `tmp` после обычного локального SQLite/browser запуска.
  - Что сделано: `scripts/local-admin-vps-browser-smoke.ps1` удаляет `tmp/local-admin-vps-browser-smoke` и пустой `tmp` без `-KeepArtifacts`, сохраняя SQLite DB, отчеты и logs для явного debug режима.
  - Доказательство: `AdminVpsSmokeReportTests` 27/27, local admin VPS browser smoke cleanup, backend full suite `690/690`, latest "Что нового" `2026-07-02-local-admin-vps-browser-smoke-cleanup`, версия `0.395.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-002BT` Admin VPS smoke sections contract default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS smoke sections contract regression не должен оставлять autogenerated fixtures и пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-vps-smoke-sections-contract.ps1` удаляет default fixture directory и пустой `tmp`; custom `-OutputDirectory` сохраняет fixtures для явной отладки.
  - Доказательство: `AdminVpsSmokeReportTests` 28/28, admin VPS smoke sections contract cleanup, backend full suite `693/693`, latest "Что нового" `2026-07-02-admin-vps-smoke-sections-contract-cleanup`, версия `0.398.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.
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

- [x] `P0-ADMIN-001BV` Add latest release guard to admin VPS bootstrap smoke evidence acceptance. 2026-07-01.
  - Done: `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` now rejects paired readiness/bootstrap smoke evidence whose `releaseId` does not match the latest active release in `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
  - Evidence: `AdminBootstrapCliScriptTests` 13/13, `scripts/test-admin-vps-bootstrap-smoke-evidence-latest-release-guard.ps1`, admin VPS bootstrap smoke evidence latest release guard regression, backend full suite `611/611`, latest "Что нового" `2026-07-01-admin-vps-bootstrap-smoke-evidence-latest-release-guard`, version `0.316.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin bootstrap/smoke evidence is captured.
- [x] `P0-ADMIN-001BW` Add known release guard to direct admin VPS bootstrap readiness. 2026-07-01.
  - Done: `scripts/admin-vps-bootstrap-smoke-readiness.ps1 -ReleaseId` now rejects unknown manual release ids before validator execution or writing readiness artifacts.
  - Evidence: `AdminBootstrapCliScriptTests` 14/14, `scripts/test-admin-vps-bootstrap-readiness-release-guard.ps1`, admin VPS bootstrap readiness release guard regression, backend full suite `619/619`, latest "Что нового" `2026-07-01-admin-vps-bootstrap-readiness-release-guard`, version `0.324.0`. `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002` remain open until real VPS admin bootstrap/smoke evidence is captured.

- [x] `P0-ADMIN-001BX` Admin VPS bootstrap smoke latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS bootstrap smoke latest release guard regression не должен оставлять readiness/bootstrap JSON и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-admin-vps-bootstrap-smoke-latest-release-guard.ps1` удаляет readiness/bootstrap stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку stale release id в `-RequireReady`/`-RequirePassed` режимах.
  - Доказательство: `AdminBootstrapCliScriptTests` 15/15, admin VPS bootstrap release guard cleanup, backend full suite `679/679`, latest "Что нового" `2026-07-02-admin-vps-bootstrap-release-guard-cleanup`, версия `0.383.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001BY` Admin VPS bootstrap smoke evidence latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS bootstrap smoke evidence latest release guard regression не должен оставлять paired stale-release JSON и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-admin-vps-bootstrap-smoke-evidence-latest-release-guard.ps1` удаляет paired readiness/bootstrap JSON и пустой `tmp`, сохраняя fail-closed проверку stale release id в evidence chain.
  - Доказательство: `AdminBootstrapCliScriptTests` 15/15, admin VPS bootstrap release guard cleanup, backend full suite `679/679`, latest "Что нового" `2026-07-02-admin-vps-bootstrap-release-guard-cleanup`, версия `0.383.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001BZ` Admin VPS bootstrap readiness release guard default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS bootstrap readiness release guard regression не должен оставлять пустой `tmp` после локального запуска с unknown release id.
  - Что сделано: `scripts/test-admin-vps-bootstrap-readiness-release-guard.ps1` удаляет пустой `tmp`, сохраняя fail-closed проверку unknown manual release id без readiness/bootstrap JSON artifacts и без утечки пароля.
  - Доказательство: `AdminBootstrapCliScriptTests` 15/15, admin VPS bootstrap release guard cleanup, backend full suite `679/679`, latest "Что нового" `2026-07-02-admin-vps-bootstrap-release-guard-cleanup`, версия `0.383.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001CA` Direct admin bootstrap wrapper default artifact cleanup. 2026-07-02.
  - Что сделать: direct admin bootstrap wrapper regression не должен оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-bootstrap-wrapper.ps1` удаляет default output directory и пустой `tmp` без `-KeepArtifacts`, сохраняя artifacts для явного debug режима.
  - Доказательство: `AdminBootstrapCliScriptTests` 16/16, admin bootstrap wrapper cleanup, backend full suite `681/681`, latest "Что нового" `2026-07-02-admin-bootstrap-wrapper-cleanup`, версия `0.386.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001CB` Admin VPS bootstrap smoke evidence validator latest release and default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS bootstrap smoke evidence validator regression должен проходить после смены latest active release, генерировать synthetic evidence по актуальному контракту и не оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` берет latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, дополняет synthetic readiness/preflight required checks/remote release fields, обновляет fail-closed expectations и удаляет default output directory и пустой `tmp` без `-KeepArtifacts`, сохраняя artifacts для явного debug режима.
  - Доказательство: `AdminBootstrapCliScriptTests` 17/17, admin VPS bootstrap smoke evidence validator cleanup, backend full suite `685/685`, latest "Что нового" `2026-07-02-admin-vps-bootstrap-smoke-evidence-validator-cleanup`, версия `0.390.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001CC` Admin VPS bootstrap smoke wrapper default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS bootstrap smoke wrapper regression не должен оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` удаляет default output directory и пустой `tmp` без `-KeepArtifacts`, сохраняя artifacts для явного debug режима.
  - Доказательство: `AdminBootstrapCliScriptTests` 18/18, admin VPS bootstrap smoke wrapper cleanup, backend full suite `687/687`, latest "Что нового" `2026-07-02-admin-vps-bootstrap-smoke-wrapper-cleanup`, версия `0.392.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001CD` Local admin VPS bootstrap smoke wrapper default artifact cleanup. 2026-07-02.
  - Что сделать: local admin VPS bootstrap smoke wrapper regression не должен оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1` удаляет default output directory, `tmp/local-admin-vps-bootstrap-smoke` и пустой `tmp` без `-KeepArtifacts`, сохраняя artifacts для явного debug режима.
  - Доказательство: `AdminBootstrapCliScriptTests` 19/19, local admin VPS bootstrap smoke wrapper cleanup, backend full suite `688/688`, latest "Что нового" `2026-07-02-local-admin-vps-bootstrap-smoke-wrapper-cleanup`, версия `0.393.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

- [x] `P0-ADMIN-001CE` Local admin VPS bootstrap smoke default artifact cleanup. 2026-07-02.
  - Что сделать: local admin VPS bootstrap smoke не должен оставлять пустой `tmp` после обычного локального SQLite/bootstrap/browser запуска.
  - Что сделано: `scripts/local-admin-vps-bootstrap-smoke.ps1` удаляет `tmp/local-admin-vps-bootstrap-smoke` и пустой `tmp` без `-KeepArtifacts`, сохраняя SQLite DB, отчеты и logs для явного debug режима.
  - Доказательство: `AdminBootstrapCliScriptTests` 20/20, local admin VPS bootstrap smoke cleanup, backend full suite `691/691`, latest "Что нового" `2026-07-02-local-admin-vps-bootstrap-smoke-cleanup`, версия `0.396.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P0-ADMIN-001CF` Admin VPS bootstrap smoke readiness default artifact cleanup. 2026-07-02.
  - Что сделать: admin VPS bootstrap smoke readiness regression не должен оставлять autogenerated reports и пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` удаляет default output directory и пустой `tmp`; custom `-OutputDirectory` сохраняет evidence для явной отладки.
  - Доказательство: `AdminBootstrapCliScriptTests` 21/21, admin VPS bootstrap smoke readiness cleanup, backend full suite `692/692`, latest "Что нового" `2026-07-02-admin-vps-bootstrap-smoke-readiness-cleanup`, версия `0.397.0`. Реальный VPS bootstrap/login smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.

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

- [x] `P0-VPN-008` VPN live smoke generator release guard. 2026-07-01.
  - Что сделать: не создавать черновик VPN live smoke report с вручную указанным неизвестным `ReleaseId`.
  - Что сделано: `scripts/new-vpn-live-smoke-report.ps1 -ReleaseId` сверяет ручной release id с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до записи JSON-артефакта; добавлен regression harness `scripts/test-vpn-live-smoke-report-generator-release-guard.ps1`, который доказывает fail-fast поведение и отсутствие созданного отчета.
  - Доказательство: `VpnLiveSmokeReportTests` 6/6, VPN live smoke generator release guard regression, latest "Что нового" `2026-07-01-vpn-live-smoke-generator-release-guard`, версия `0.318.0`. Реальные smoke по `P0-VPN-001` ... `P0-VPN-005` остаются открытыми до внешнего evidence.
- [x] `P0-VPN-009` VPN live smoke generator release guard default artifact cleanup. 2026-07-02.
  - Что сделать: VPN live smoke generator release guard regression не должен оставлять пустой `tmp` после локального запуска с unknown release id.
  - Что сделано: `scripts/test-vpn-live-smoke-report-generator-release-guard.ps1` удаляет пустой `tmp`, сохраняя fail-closed проверку unknown manual release id без созданного JSON report.
  - Доказательство: `VpnLiveSmokeReportTests` 7/7, VPN live smoke generator release guard cleanup, backend full suite `674/674`, latest "Что нового" `2026-07-02-vpn-live-smoke-generator-release-guard-cleanup`, версия `0.379.0`. Реальные smoke по `P0-VPN-001` ... `P0-VPN-005` остаются открытыми до внешнего evidence.

- [x] `P0-VPN-010` VPN live smoke latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: VPN live smoke latest release guard regression не должен оставлять stale-release JSON и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-vpn-live-smoke-report-latest-release-guard.ps1` удаляет stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку stale release id в `-RequireAllPassed` режиме.
  - Доказательство: `VpnLiveSmokeReportTests` 8/8, VPN live smoke latest release guard cleanup, backend full suite `675/675`, latest "Что нового" `2026-07-02-vpn-live-smoke-latest-release-guard-cleanup`, версия `0.380.0`. Реальные smoke по `P0-VPN-001` ... `P0-VPN-005` остаются открытыми до внешнего evidence.

- [x] `P0-VPN-011` VPN live smoke report self-link guard. 2026-07-02.
  - Что сделать: standalone VPN live smoke JSON должен явно ссылаться на фактически проверяемый report path, чтобы evidence archive не мог незаметно подменить VPN smoke отчет.
  - Что сделано: `docs/vpn-live-smoke-report.template.json` получил `smokeReportPath`, `scripts/new-vpn-live-smoke-report.ps1` записывает туда resolved output path, `scripts/validate-vpn-live-smoke-report.ps1` сверяет self-link с фактическим `-ReportPath`, а `scripts/test-vpn-live-smoke-report-self-link-guard.ps1` доказывает fail-closed mismatch и убирает default `tmp`.
  - Доказательство: `VpnLiveSmokeReportTests` 9/9, `scripts/test-vpn-live-smoke-report-self-link-guard.ps1`, backend full suite `703/703`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-vpn-live-smoke-report-self-link`, версия `0.408.0`. Реальные smoke по `P0-VPN-001` ... `P0-VPN-005` остаются открытыми до внешнего evidence.

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

- [x] `P0-PAY-016` Payment provider smoke generator release guard. 2026-07-01.
  - Что сделать: не создавать черновик payment provider smoke report с вручную указанным неизвестным `ReleaseId`.
  - Что сделано: `scripts/new-payment-provider-smoke-report.ps1 -ReleaseId` сверяет ручной release id с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до записи JSON-артефакта; добавлен regression harness `scripts/test-payment-provider-smoke-report-generator-release-guard.ps1`, который доказывает fail-fast поведение и отсутствие созданного отчета.
  - Доказательство: `PaymentProviderSmokeReportTests` 8/8, payment provider smoke generator release guard regression, latest "Что нового" `2026-07-01-payment-smoke-generator-release-guard`, версия `0.319.0`. Реальные smoke по провайдерам `P0-PAY-002` ... `P0-PAY-009` остаются открытыми до внешнего evidence.


- [x] `P0-PAY-017` Payment provider smoke generator release guard default artifact cleanup. 2026-07-02.
  - Что сделать: payment provider smoke generator release guard regression не должен оставлять пустой `tmp` после локального запуска с unknown release id.
  - Что сделано: `scripts/test-payment-provider-smoke-report-generator-release-guard.ps1` удаляет пустой `tmp`, сохраняя fail-closed проверку unknown manual release id без созданного JSON report.
  - Доказательство: `PaymentProviderSmokeReportTests` 10/10, payment provider smoke generator release guard cleanup, backend full suite `677/677`, latest "Что нового" `2026-07-02-payment-smoke-release-guard-cleanup`, версия `0.381.0`. Реальные smoke по провайдерам `P0-PAY-002` ... `P0-PAY-009` остаются открытыми до внешнего evidence.

- [x] `P0-PAY-018` Payment provider smoke latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: payment provider smoke latest release guard regression не должен оставлять stale-release JSON и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-payment-provider-smoke-report-latest-release-guard.ps1` удаляет stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку stale release id в `-RequireAllPassed` режиме.
  - Доказательство: `PaymentProviderSmokeReportTests` 10/10, payment provider smoke latest release guard cleanup, backend full suite `677/677`, latest "Что нового" `2026-07-02-payment-smoke-release-guard-cleanup`, версия `0.381.0`. Реальные smoke по провайдерам `P0-PAY-002` ... `P0-PAY-009` остаются открытыми до внешнего evidence.

- [x] `P0-PAY-019` Payment provider smoke report self-link guard. 2026-07-02.
  - Что сделать: standalone payment provider smoke JSON должен явно ссылаться на фактически проверяемый report path, чтобы evidence archive не мог незаметно подменить provider smoke отчет.
  - Что сделано: `docs/payment-provider-smoke-report.template.json` получил `smokeReportPath`, `scripts/new-payment-provider-smoke-report.ps1` записывает туда resolved output path, `scripts/validate-payment-provider-smoke-report.ps1` сверяет self-link с фактическим `-ReportPath`, а `scripts/test-payment-provider-smoke-report-self-link-guard.ps1` доказывает fail-closed mismatch и убирает default `tmp`.
  - Доказательство: `PaymentProviderSmokeReportTests` 11/11, `scripts/test-payment-provider-smoke-report-self-link-guard.ps1`, backend full suite `702/702`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-payment-provider-smoke-report-self-link`, версия `0.407.0`. Реальные smoke по провайдерам `P0-PAY-002` ... `P0-PAY-009` остаются открытыми до внешнего evidence.

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
- [x] `P8-CI-007` Production env normalizer default artifact cleanup. 2026-07-02.
  - Что сделать: production env normalizer regression не должен оставлять autogenerated `production.env`/`normalized.env` fixtures и пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-normalize-production-env.ps1` удаляет default fixture directory и пустой `tmp`; custom `-OutputDirectory` сохраняет fixtures для явной отладки.
  - Доказательство: `DeployWorkflowGuardTests` 4/4, deploy production env normalizer cleanup, backend full suite `694/694`, latest "Что нового" `2026-07-02-production-env-normalizer-cleanup`, версия `0.399.0`. Реальный VPS admin smoke остается в `STATE-013`/`P0-ADMIN-001`/`P0-ADMIN-002`.
- [x] `P8-CI-008` Docker validation tmp artifact cleanup. 2026-07-02.
  - Что сделать: Docker validation gate не должен оставлять fixed `/tmp/vpnplatform-*` curl/config/log artifacts после локального запуска.
  - Что сделано: `scripts/validate-docker.sh` пишет curl output, compose config и runtime logs в уникальный `mktemp` directory и удаляет его в `cleanup`; `KEEP_STACK=1` сохраняет только compose stack для ручной диагностики.
  - Доказательство: `SandboxE2EScenariosMvpTests`, `bash -n scripts/validate-docker.sh`, backend full suite `695/695`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-docker-validation-tmp-cleanup`, версия `0.400.0`. Реальный staging/VPS smoke остается в `STATE-011`/`STATE-012`/`STATE-013`/`P11-ACC-002`.
- [x] `P8-CI-009` Deploy VPS Docker tmp artifact cleanup. 2026-07-02.
  - Что сделать: Docker deploy workflow не должен оставлять fixed `/tmp/vpnplatform-compose.yml` artifact на VPS после `docker compose config`.
  - Что сделано: `.github/workflows/deploy-vps.yml` в шаге `Start Docker production stack` пишет compose config в remote `mktemp` directory и удаляет его через `trap cleanup EXIT`.
  - Доказательство: `DeployWorkflowGuardTests`, backend full suite `696/696`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-deploy-vps-docker-tmp-cleanup`, версия `0.401.0`. Реальный VPS deploy/post-deploy smoke остается внешним evidence для `STATE-013`/`P11-ACC-002`.

- [x] `P8-CI-010` CI Ansible syntax-check tmp artifact cleanup. 2026-07-02.
  - Что сделать: CI provisioning job не должен оставлять fixed `/tmp/vpnplatform-ci` inventory directory после Ansible syntax-check.
  - Что сделано: `.github/workflows/ci.yml` пишет inventory в per-run `mktemp` directory и удаляет его через `trap cleanup EXIT`.
  - Доказательство: `DeployWorkflowGuardTests`, backend full suite `697/697`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-ci-ansible-tmp-cleanup`, версия `0.402.0`. Реальный VPS/staging/live smoke остается внешним evidence для production-ready blockers.

- [x] `P8-CI-011` validate_repo Ansible tmp artifact cleanup. 2026-07-02.
  - Что сделать: local repository validation не должен оставлять temporary Ansible inventory directory, если `ansible-playbook --syntax-check` падает.
  - Что сделано: `scripts/validate_repo.sh` регистрирует `trap cleanup_ansible_tmp EXIT` сразу после `mktemp -d` и удаляет temporary inventory directory при успешном и ошибочном выходе.
  - Доказательство: `DeployWorkflowGuardTests`, `bash -n scripts/validate_repo.sh`, backend full suite `698/698`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-validate-repo-ansible-tmp-cleanup`, версия `0.403.0`. Реальный VPS/staging/live smoke остается внешним evidence для production-ready blockers.
- [x] `P8-CI-012` provision-node wrapper default workdir cleanup. 2026-07-02.
  - Что сделать: manual provisioning wrapper не должен оставлять fixed `/tmp/vpnplatform-manual-*` default workdir и должен запускать runner/playbook через absolute paths независимо от текущего каталога.
  - Что сделано: `scripts/provision-node.sh` вычисляет `ROOT_DIR`, передает absolute runner/playbook paths, создает default workdir через per-run `mktemp` и удаляет его через `trap cleanup_workdir EXIT`; явный custom workdir сохраняется для диагностики оператора.
  - Доказательство: `DeployWorkflowGuardTests`, `bash -n scripts/provision-node.sh`, backend full suite `699/699`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-provision-node-wrapper-cleanup`, версия `0.404.0`. Реальный VPS/staging/live smoke остается внешним evidence для production-ready blockers.

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

- [x] `P9-TST-007G` Staging smoke report generator release guard. 2026-07-01.
  - Что сделать: не создавать черновик staging smoke report с вручную указанным неизвестным `ReleaseId`.
  - Что сделано: `scripts/new-staging-smoke-report.ps1 -ReleaseId` сверяет ручной release id с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до записи JSON-артефакта; добавлен regression harness `scripts/test-staging-smoke-report-generator-release-guard.ps1`, который доказывает fail-fast поведение и отсутствие созданного отчета.
  - Доказательство: `StagingSmokeChecklistTests` 10/10, staging smoke generator release guard regression, latest "Что нового" `2026-07-01-staging-smoke-generator-release-guard`, версия `0.320.0`. Реальный live/staging smoke report еще нужен, поэтому `P9-TST-007` остается `[~]`.

- [x] `P9-TST-007H` Staging smoke latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: stale-release regression для staging smoke report не должен оставлять autogenerated `tmp/staging-smoke-stale-release-guard.json` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-staging-smoke-report-latest-release-guard.ps1` удаляет staging smoke report JSON и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `StagingSmokeChecklistTests` 11/11, staging smoke latest release guard cleanup, backend full suite `660/660`, latest "Что нового" `2026-07-02-staging-smoke-latest-release-guard-cleanup`, версия `0.365.0`. Реальный live/staging smoke report еще нужен, поэтому `P9-TST-007` остается `[~]`.

- [x] `P9-TST-007I` Staging smoke generator release guard default artifact cleanup. 2026-07-02.
  - Что сделать: unknown-release regression для staging smoke generator не должен оставлять пустой autogenerated `tmp` после локального запуска, даже когда JSON-отчет не создается.
  - Что сделано: `scripts/test-staging-smoke-report-generator-release-guard.ps1` удаляет пустой `tmp` в `finally`, сохраняя fail-fast проверку неизвестного `ReleaseId` до записи отчета.
  - Доказательство: `StagingSmokeChecklistTests` 13/13, staging smoke generator release guard cleanup, backend full suite `661/661`, latest "Что нового" `2026-07-02-staging-smoke-generator-release-guard-cleanup`, версия `0.366.0`. Реальный live/staging smoke report еще нужен, поэтому `P9-TST-007` остается `[~]`.
- [x] `P9-TST-007J` Staging smoke report self-link guard. 2026-07-02.
  - Что сделать: standalone staging smoke JSON должен явно ссылаться на фактически проверяемый report path, чтобы evidence archive не мог незаметно подменить отчет.
  - Что сделано: `docs/staging-smoke-report.template.json` получил `smokeReportPath`, `scripts/new-staging-smoke-report.ps1` записывает туда resolved output path, `scripts/validate-staging-smoke-report.ps1` сверяет self-link с фактическим `-ReportPath`, а `scripts/test-staging-smoke-report-self-link-guard.ps1` доказывает fail-closed mismatch и убирает default `tmp`.
  - Доказательство: `StagingSmokeChecklistTests` 13/13, `scripts/test-staging-smoke-report-self-link-guard.ps1`, backend full suite `701/701`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-staging-smoke-report-self-link`, версия `0.406.0`. Реальный live/staging smoke report еще нужен, поэтому `P9-TST-007` остается `[~]`.

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

- [x] `P11-ACC-129` Fresh local smoke default artifact cleanup. 2026-07-02.
  - Что сделать: fresh local SQLite smoke не должен оставлять пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/fresh-local-smoke.ps1` удаляет `tmp/fresh-local-smoke` и пустой `tmp` без `-KeepArtifacts`, сохраняя SQLite DB/logs для явного debug режима.
  - Доказательство: `FreshLocalSetupSmokeTests` 2/2, fresh local smoke cleanup, backend full suite `689/689`, latest "Что нового" `2026-07-02-fresh-local-smoke-cleanup`, версия `0.394.0`. Реальный VPS/staging/live evidence остается в `STATE-011`/`STATE-012`/`STATE-013`/`P11-ACC-002`.

- [x] `P11-ACC-130` VPS production smoke report self-link guard. 2026-07-02.
  - Что сделать: standalone VPS production smoke evidence должен сам указывать проверяемый JSON path, чтобы validator не принимал отчет, скопированный или подмененный без явной привязки к `-ReportPath`.
  - Что сделано: `docs/vps-production-smoke-report.template.json` получил `smokeReportPath`, `scripts/new-vps-production-smoke-report.ps1` записывает туда resolved output path, `scripts/validate-vps-production-smoke-report.ps1` сверяет self-link с фактическим `-ReportPath`, а `scripts/test-vps-production-smoke-report-self-link-guard.ps1` доказывает fail-closed mismatch и убирает default `tmp`.
  - Доказательство: `VpsProductionSmokeTests` 12/12, `scripts/test-vps-production-smoke-report-self-link-guard.ps1`, backend full suite `700/700`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-vps-production-smoke-report-self-link`, версия `0.405.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-131` Production readiness summary self-link guard. 2026-07-02.
  - Что сделать: production readiness summary JSON должен явно ссылаться на фактически проверяемые Markdown и JSON файлы, чтобы evidence handoff не мог незаметно подменить summary.
  - Что сделано: `scripts/new-production-readiness-summary.ps1` записывает `summaryPath` и `jsonSummaryPath`, `scripts/validate-production-readiness-summary.ps1` сверяет оба self-link с фактическими `-SummaryPath`/`-JsonSummaryPath`, а `scripts/test-production-readiness-summary-self-link-guard.ps1` доказывает fail-closed mismatch и убирает default `tmp`.
  - Доказательство: `ProductionReadinessGateTests` 118/118, `scripts/test-production-readiness-summary-self-link-guard.ps1`, backend full suite `704/704`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-production-readiness-summary-self-link`, версия `0.409.0`. Реальный live VPS/staging evidence еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-132` Production readiness assertion result self-link guard. 2026-07-02.
  - Что сделать: production readiness assertion result JSON должен явно ссылаться на фактически проверяемый JSON и Markdown artifact, чтобы CI/handoff не мог принять подмененный result файл.
  - Что сделано: `scripts/validate-production-readiness-assertion-result.ps1` требует `resultJsonPath` и сверяет его с фактическим `-ResultJsonPath`, сверяет `resultMarkdownPath` с `-ResultMarkdownPath`, а `scripts/test-production-readiness-assertion-result-self-link-guard.ps1` доказывает fail-closed mismatch и убирает default `tmp`.
  - Доказательство: `ProductionReadinessGateTests` 119/119, `scripts/test-production-readiness-assertion-result-self-link-guard.ps1`, backend full suite `705/705`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-production-readiness-assertion-result-self-link`, версия `0.410.0`. Реальный live VPS/staging evidence еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-133` Roadmap progress counter guard. 2026-07-02.
  - Что сделать: верхний статус roadmap должен автоматически совпадать с фактическими checklist markers, чтобы выполнено/осталось/процент готовности нельзя было случайно указать неверно.
  - Что сделано: `RoadmapCurrentStateTests` парсит `PRODUCT_COMPLETION_ROADMAP.md`, считает `[x]`, `[ ]`, `[~]` и `[!]`, сверяет completed/total/open/in-progress/blocked с верхней строкой roadmap и оставляет live evidence пункты открытыми.
  - Доказательство: `RoadmapCurrentStateTests` 3/3, targeted docs/release suite, backend full suite `706/706`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-roadmap-progress-counter-guard`, версия `0.411.0`. Реальный live VPS/staging evidence еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-134` Roadmap progress percent guard. 2026-07-02.
  - Что сделать: верхний статус roadmap должен явно показывать процент готовности и этот процент должен считаться от фактических checklist markers.
  - Что сделано: верхний статус roadmap получил `готовность`, а `RoadmapCurrentStateTests` вычисляет процент как `completed / total * 100` с округлением до одного знака и сверяет его с header.
  - Доказательство: `RoadmapCurrentStateTests` 3/3, targeted docs/release suite, backend full suite `706/706`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-roadmap-progress-percent-guard`, версия `0.412.0`. Реальный live VPS/staging evidence еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-135` Roadmap progress remaining guard. 2026-07-02.
  - Что сделать: верхний статус roadmap должен явно показывать, сколько проверяемых пунктов осталось до закрытия.
  - Что сделано: верхний статус roadmap получил `осталось`, а `RoadmapCurrentStateTests` сверяет remaining count с `total - completed` и суммой `open + in-progress + blocked`.
  - Доказательство: `RoadmapCurrentStateTests` 3/3, targeted docs/release suite, backend full suite `706/706`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-roadmap-progress-remaining-guard`, версия `0.413.0`. Реальный live VPS/staging evidence еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-136` Agent instructions guard. 2026-07-02.
  - Что сделать: локальные агентские инструкции должны быть читаемым UTF-8 и тестом закреплять обязательные правила прогресса, очистки артефактов, тестирования и локальной БД.
  - Что сделано: `AGENTS.md` нормализован в читаемый UTF-8; `DocumentationEncodingTests` теперь проверяет `AGENTS.md` на mojibake и наличие обязательных clauses про выполнено/осталось/процент, cleanup, тесты и local DB.
  - Доказательство: `DocumentationEncodingTests`, targeted docs/release suite, backend full suite `707/707`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-instructions-guard`, версия `0.414.0`. Реальный live VPS/staging evidence еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-137` External evidence open guard. 2026-07-02.
  - Что сделать: roadmap должен тестом защищать все внешние live/VPS/payment/VPN/staging пункты от случайного закрытия без реального evidence.
  - Что сделано: `RoadmapCurrentStateTests` теперь парсит checklist markers и проверяет, что `STATE-011`, `STATE-012`, `STATE-013`, `P0-ADMIN-001`, `P0-ADMIN-002`, `P0-VPN-001`..`P0-VPN-005`, `P0-PAY-002`..`P0-PAY-009`, `P9-TST-007` и `P11-ACC-002` остаются `[ ]` или `[~]` до реального evidence.
  - Доказательство: `RoadmapCurrentStateTests` 4/4, targeted docs/release suite, backend full suite `708/708`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-external-evidence-open-guard`, версия `0.415.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-138` Product UI external evidence open guard. 2026-07-02.
  - Что сделать: product/admin UI roadmap должен тестом защищать внешние live/VPS/payment/VPN/staging строки от случайного закрытия без реального evidence.
  - Что сделано: `ProductAdminUiRoadmapSyncTests` теперь проверяет, что строки live payments, production-like 3x-ui/VPN, VPS admin, staging/VPS smoke и production-ready decision остаются `[ ]` или `[~]`.
  - Доказательство: `ProductAdminUiRoadmapSyncTests` 2/2, targeted docs/release suite, backend full suite `709/709`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-product-ui-external-evidence-open-guard`, версия `0.416.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-139` Release decision external evidence open guard. 2026-07-02.
  - Что сделать: release decision должен тестом оставаться связанным с открытыми external evidence пунктами roadmap и не принимать production-ready без реального evidence.
  - Что сделано: `ReleaseDecisionTests` теперь проверяет открытые roadmap markers для live payments, live 3x-ui/VPN, VPS admin, staging smoke и VPS production smoke, а также наличие соответствующих blockers в `docs/release-decision.md`.
  - Доказательство: `ReleaseDecisionTests` 4/4, targeted docs/release suite, backend full suite `710/710`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-release-decision-external-evidence-open-guard`, версия `0.417.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.

- [x] `P11-ACC-140` Final runbook external evidence open guard. 2026-07-02.
  - Что сделать: final runbook должен тестом закреплять текущий release/status и production limitations, пока external evidence открыт.
  - Что сделано: `FinalDocsChangelogTests` теперь проверяет latest release, backend count, `staging-ready baseline`, production limitations и открытые roadmap markers в final runbook context.
  - Доказательство: `FinalDocsChangelogTests` 4/4, targeted docs/release suite, backend full suite `711/711`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-final-runbook-external-evidence-open-guard`, версия `0.418.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-141` Test results external evidence open guard. 2026-07-02.
  - Что сделать: TEST_RESULTS должен тестом закреплять текущий release/status, локальную валидацию, cleanup артефактов и production limitations, пока external evidence открыт.
  - Что сделано: `FinalDocsChangelogTests` теперь проверяет latest release, roadmap progress, backend count, local SQLite smoke, secret scan, artifact cleanup и отсутствие pending validation в TEST_RESULTS.
  - Доказательство: `FinalDocsChangelogTests` 5/5, targeted docs/release suite, backend full suite `712/712`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-test-results-external-evidence-open-guard`, версия `0.419.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-142` Changelog external evidence open guard. 2026-07-02.
  - Что сделать: CHANGELOG должен тестом закреплять текущий roadmap progress, validation evidence и production limitations, пока external evidence открыт.
  - Что сделано: `FinalDocsChangelogTests` теперь проверяет latest release, roadmap progress, backend count, targeted suite, local SQLite smoke, secret scan и отсутствие pending validation в CHANGELOG.
  - Доказательство: `FinalDocsChangelogTests` 6/6, targeted docs/release suite, backend full suite `713/713`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-changelog-external-evidence-open-guard`, версия `0.420.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-143` README external evidence open guard. 2026-07-02.
  - Что сделать: README должен тестом закреплять текущий release/status и production limitations, пока external evidence открыт.
  - Что сделано: `ReadmeDocumentationTests` теперь проверяет latest release, backend count, `staging-ready baseline`, production limitations и открытые roadmap markers в README context.
  - Доказательство: `ReadmeDocumentationTests` 4/4, targeted docs/release suite, backend full suite `714/714`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-readme-external-evidence-open-guard`, версия `0.421.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-144` Product/admin UI roadmap external evidence open guard. 2026-07-02.
  - Что сделать: product/admin UI roadmap должен тестом закреплять текущий validation status и production limitations, пока external evidence открыт.
  - Что сделано: `ProductAdminUiRoadmapSyncTests` теперь проверяет latest release, backend count, local validation evidence, `staging-ready baseline`, production limitations и открытые roadmap markers в product/admin UI roadmap context.
  - Доказательство: `ProductAdminUiRoadmapSyncTests` 3/3, targeted docs/release suite, backend full suite `715/715`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-product-roadmap-external-evidence-open-guard`, версия `0.422.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-145` Roadmap external evidence open-set guard. 2026-07-02.
  - Что сделать: roadmap должен тестом закреплять точный набор незакрытых пунктов, чтобы нельзя было случайно добавить скрытый `[ ]`, `[~]` или `[!]` пункт вне external-evidence списка.
  - Что сделано: `RoadmapCurrentStateTests` теперь сверяет весь not-closed marker set с явным списком `STATE-011`, `STATE-012`, `STATE-013`, `P0-ADMIN-*`, `P0-VPN-*`, `P0-PAY-*`, `P9-TST-007` и `P11-ACC-002`.
  - Доказательство: `RoadmapCurrentStateTests` 4/4, targeted docs/release suite, backend full suite `715/715`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-roadmap-external-evidence-open-set-guard`, версия `0.423.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-146` Status docs latest release seed guard. 2026-07-02.
  - Что сделать: статусные документы должны тестом сверяться с latest active release seed, включая product/admin UI roadmap, чтобы раздел "Что нового" не расходился с roadmap.
  - Что сделано: `RoadmapCurrentStateTests` теперь проверяет latest release id/version в README, CHANGELOG, TEST_RESULTS, final runbook, release decision и product/admin UI roadmap, а также сверяет latest active release в `releases.json`.
  - Доказательство: `RoadmapCurrentStateTests` 4/4, targeted docs/release suite, backend full suite `715/715`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-status-docs-latest-release-seed-guard`, версия `0.424.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-147` Changelog and TEST_RESULTS top release guard. 2026-07-02.
  - Что сделать: верхняя запись CHANGELOG и верхний блок TEST_RESULTS должны тестом совпадать с latest active release seed, чтобы статусные журналы не отставали от раздела "Что нового".
  - Что сделано: `FinalDocsChangelogTests` теперь читает latest active release из `releases.json` и проверяет, что верхние блоки `CHANGELOG.md` и `TEST_RESULTS.md` содержат тот же release id и версию.
  - Доказательство: `FinalDocsChangelogTests` 7/7, targeted docs/release suite, backend full suite `716/716`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-changelog-test-results-top-release-guard`, версия `0.425.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.
- [x] `P11-ACC-148` Latest release seed order guard. 2026-07-02.
  - Что сделать: releases seed должен тестом гарантировать единственный latest active release и строгое самое позднее `releasedAt`, чтобы "Что нового" не выбирал старую запись.
  - Что сделано: `RoadmapCurrentStateTests` теперь проверяет active releases из `releases.json`, уникальность максимального `releasedAt`, совпадение latest release id/version с текущим roadmap status и отсутствие tie по latest timestamp.
  - Доказательство: `RoadmapCurrentStateTests` 5/5, targeted docs/release suite, backend full suite `717/717`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-latest-release-seed-order-guard`, версия `0.426.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.

- [x] `P11-ACC-149` Release seed identity guard. 2026-07-02.
  - Что сделать: releases seed должен тестом гарантировать уникальные `releaseId`, `version` и `releasedAt`, чтобы "Что нового", changelog и admin release history не получали конфликтующие записи.
  - Что сделано: `RoadmapCurrentStateTests` теперь парсит `releases.json` и проверяет отсутствие дублей по `releaseId`, `version` и `releasedAt` для всех release seed entries.
  - Доказательство: `RoadmapCurrentStateTests` 6/6, targeted docs/release suite, backend full suite `718/718`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-release-seed-identity-guard`, версия `0.427.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.

- [x] `P11-ACC-150` Release seed version order guard. 2026-07-02.
  - Что сделать: releases seed должен тестом гарантировать, что версии строго растут вместе с `releasedAt`, чтобы более новый release не получил старую или повторную semantic version.
  - Что сделано: `RoadmapCurrentStateTests` теперь сортирует release seed entries по `releasedAt` и проверяет строгий рост `version` для всей истории "Что нового".
  - Доказательство: `RoadmapCurrentStateTests` 7/7, targeted docs/release suite, backend full suite `719/719`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-release-seed-version-order-guard`, версия `0.428.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.

- [x] `P11-ACC-151` Release seed file order guard. 2026-07-02.
  - Что сделать: releases seed должен тестом гарантировать, что физический порядок записей в `releases.json` совпадает с ростом `releasedAt`, чтобы ручные вставки не ломали ревью и admin release history.
  - Что сделано: `RoadmapCurrentStateTests` теперь проверяет, что каждая следующая запись `releases.json` имеет более поздний `releasedAt`, чем предыдущая.
  - Доказательство: `RoadmapCurrentStateTests` 8/8, targeted docs/release suite, backend full suite `720/720`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-release-seed-file-order-guard`, версия `0.429.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.

- [x] `P11-ACC-152` Release seed secret literal guard. 2026-07-02.
  - Что сделать: releases seed должен тестом блокировать secret-like literals и raw provider payload markers, чтобы "Что нового" не стало источником утечек.
  - Что сделано: `RoadmapCurrentStateTests` теперь проверяет `releases.json` на PEM private key, bearer value, provider key patterns и raw provider payload marker.
  - Доказательство: `RoadmapCurrentStateTests` 9/9, targeted docs/release suite, backend full suite `721/721`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-release-seed-secret-literal-guard`, версия `0.430.0`. Реальный live VPS/staging/payment/3x-ui evidence еще нужен, поэтому production/live пункты остаются открытыми.

- [x] `P11-ACC-153` Status docs production-ready claim guard. 2026-07-02.
  - Что сделать: current status docs must fail closed if they claim production-ready acceptance before real VPS/staging/live evidence exists.
  - Что сделано: `RoadmapCurrentStateTests` now checks README, CHANGELOG, TEST_RESULTS, final runbook, release decision, product/admin UI roadmap and master roadmap for forbidden production-ready acceptance claims and accidental closure of `P11-ACC-002`.
  - Доказательство: `RoadmapCurrentStateTests` 10/10, targeted docs/release/encoding suite, backend full suite `722/722`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-status-docs-production-ready-claim-guard`, версия `0.431.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-154` Latest release evidence caveat guard. 2026-07-02.
  - Что сделать: latest active What's New release must keep an `important` caveat that real VPS/staging/payment/3x-ui evidence is still required.
  - Что сделано: `RoadmapCurrentStateTests` now checks the latest active release seed entry for an `important` item containing the required external evidence caveat.
  - Доказательство: `RoadmapCurrentStateTests` 11/11, targeted docs/release/encoding suite, backend full suite `723/723`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-latest-release-evidence-caveat-guard`, версия `0.432.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-155` Docs strict UTF-8 guard. 2026-07-02.
  - Что сделать: docs, current status files and the What's New seed must be strict UTF-8 without BOM so Russian text and roadmap evidence do not drift.
  - Что сделано: `DocumentationEncodingTests` now reads docs/status markdown and `releases.json` with throwing UTF-8 decoding and rejects UTF-8 BOM bytes.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-docs-strict-utf8-guard`, версия `0.433.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-156` Agent instructions readable UTF-8 guard. 2026-07-02.
  - Что сделать: `AGENTS.md` must be readable UTF-8 and tests must assert the actual Russian progress, cleanup, testing and local DB clauses instead of mojibake text.
  - Что сделано: `AGENTS.md` restored to readable UTF-8; `DocumentationEncodingTests` now rejects the `U+0420 U+00AD` mojibake marker and checks the real Russian clauses through Unicode literals.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-instructions-readable-utf8-guard`, версия `0.434.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-157` Source mojibake guard. 2026-07-02.
  - Что сделать: source-like tracked files must be checked for mojibake markers and strict UTF-8 so damaged Cyrillic literals cannot stay hidden in tests or scripts.
  - Что сделано: `DocumentationEncodingTests` now scans source-like tracked files, excludes generated EF migrations from the no-BOM rule, and payment provider tests keep damaged Cyrillic checks as Unicode escape literals.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-source-mojibake-guard`, версия `0.435.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-158` Changelog mojibake guard. 2026-07-02.
  - Что сделать: `CHANGELOG.md` must be covered by mojibake marker checks, not only strict UTF-8 checks, so release history cannot store damaged Cyrillic text.
  - Что сделано: `DocumentationEncodingTests` now includes `CHANGELOG.md` in the mojibake marker scan while keeping it in strict UTF-8 validation.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-changelog-mojibake-guard`, версия `0.436.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-159` Project files UTF-8 guard. 2026-07-02.
  - Что сделать: project/config files must be covered by mojibake and strict UTF-8 checks, and the solution file must not keep UTF-8 BOM drift.
  - Что сделано: `DocumentationEncodingTests` now covers `.sln`, `.csproj`, `.props`, `.targets`, `.http`, `.config` and `.xml`; `backend/VpnPlatform.sln` was normalized to UTF-8 without BOM.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-project-files-utf8-guard`, версия `0.437.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-160` Dotfiles UTF-8 guard. 2026-07-02.
  - Что сделать: tracked dotfiles must be covered by mojibake and strict UTF-8 checks so repository-level config files cannot keep damaged text or BOM drift.
  - Что сделано: `DocumentationEncodingTests` now covers `.dockerignore`, `.editorconfig`, `.gitattributes` and `.gitignore`; local `.serena` metadata stays excluded from repository assertions.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-dotfiles-utf8-guard`, версия `0.438.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-161` .env.example UTF-8 guard. 2026-07-02.
  - Что сделать: the tracked environment template must be covered by mojibake and strict UTF-8 checks so placeholder configuration cannot keep damaged text or BOM drift.
  - Что сделано: `DocumentationEncodingTests` now covers `.env.example` in the same strict UTF-8 without BOM and mojibake scan as source/config files.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-env-example-utf8-guard`, версия `0.439.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-162` Deploy/frontend text UTF-8 guard. 2026-07-02.
  - Что сделать: frontend HTML/CSS, Dockerfiles, nginx config, Ansible inventory/templates and Python helpers must be covered by mojibake and strict UTF-8 checks.
  - Что сделано: `DocumentationEncodingTests` now covers `.html`, `.css`, `.conf`, `.ini`, `.j2`, `.py` and `Dockerfile*` files in the same strict UTF-8 without BOM and mojibake scan as source/config files.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-deploy-frontend-text-utf8-guard`, версия `0.440.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-163` All markdown UTF-8 guard. 2026-07-02.
  - Что сделать: all tracked markdown files must be covered by mojibake and strict UTF-8 checks, including repository notes outside `docs`.
  - Что сделано: `DocumentationEncodingTests` now covers all `.md` files in the same strict UTF-8 without BOM and mojibake scan as source/config files, including `AUDIT_STAGE_1_2.md`, `delivery/PLAN_OF_RECORD.md` and `infra/ansible/README.md`.
  - Доказательство: `DocumentationEncodingTests` 3/3, targeted docs/release/encoding suite, backend full suite `724/724`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-all-markdown-utf8-guard`, версия `0.441.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-164` Status docs progress consistency guard. 2026-07-02.
  - Что сделать: README, CHANGELOG, TEST_RESULTS, final runbook, release decision and product/admin UI roadmap must report the same roadmap progress counters as the master roadmap markers.
  - Что сделано: `RoadmapCurrentStateTests` now calculates done/total, readiness percent, remaining, open, in-progress and blocked counts from `PRODUCT_COMPLETION_ROADMAP.md` and verifies the current status documents contain the same counters.
  - Доказательство: `RoadmapCurrentStateTests` 12/12, targeted docs/release/encoding suite, backend full suite `725/725`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-status-docs-progress-consistency-guard`, версия `0.442.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-165` What's New progress consistency guard. 2026-07-02.
  - Что сделать: latest active "Что нового" release must report the same done/total, readiness, remaining, open, in-progress and blocked counters as the master roadmap.
  - Что сделано: `RoadmapCurrentStateTests` now calculates roadmap counters and verifies the latest active release seed contains the same progress values in its summary/items.
  - Доказательство: `RoadmapCurrentStateTests` 13/13, targeted docs/release/encoding suite, backend full suite `726/726`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-whats-new-progress-consistency-guard`, версия `0.443.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-166` Agent source/version reporting guard. 2026-07-02.
  - Что сделать: agent instructions must require final roadmap/status answers to cite the source and status date/version when data comes from roadmap or markdown.
  - Что сделано: `DocumentationEncodingTests` now verifies `AGENTS.md` contains the source and date/version reporting requirements together with progress, cleanup, testing and local DB rules.
  - Доказательство: `DocumentationEncodingTests` 4/4, targeted docs/release/encoding suite, backend full suite `727/727`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-source-version-reporting-guard`, версия `0.444.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-167` Agent unavailable checks risk guard. 2026-07-02.
  - Что сделать: agent instructions must require final answers to state what was not checked, why and what residual risk remains when tests, local DB or external checks are unavailable.
  - Что сделано: `DocumentationEncodingTests` now verifies `AGENTS.md` contains the unavailable test/local DB/external check clause and the required final-answer risk disclosure fields.
  - Доказательство: `DocumentationEncodingTests` 5/5, targeted docs/release/encoding suite, backend full suite `728/728`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-unavailable-checks-risk-guard`, версия `0.445.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-168` Agent git delivery guard. 2026-07-02.
  - Что сделать: agent instructions must require Russian commit messages, task-scoped staging and no push unless the user explicitly asks for push.
  - Что сделано: `AGENTS.md` now has a Git Delivery section, and `DocumentationEncodingTests` verifies Russian commit, no-push and task-scoped staging requirements.
  - Доказательство: `DocumentationEncodingTests` 6/6, targeted docs/release/encoding suite, backend full suite `729/729`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-git-delivery-guard`, версия `0.446.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-169` Agent image attachment guard. 2026-07-02.
  - Что сделать: agent instructions must require checking attached screenshots/images, using customer notes when available, and explicitly disclosing when images are missing instead of inventing their text.
  - Что сделано: `AGENTS.md` now has an Image And Screenshot Inputs section, and `DocumentationEncodingTests` verifies attachment availability checks, customer-note handling and missing-image disclosure.
  - Доказательство: `DocumentationEncodingTests` 7/7, targeted docs/release/encoding suite, backend full suite `730/730`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-image-attachment-guard`, версия `0.447.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-170` Agent duplicate task guard. 2026-07-02.
  - Что сделать: agent instructions must require checking roadmap, changelog, TEST_RESULTS, What's New and code before reworking duplicate, completed or partial tasks.
  - Что сделано: `AGENTS.md` now has a Duplicate And Completed Tasks section, and `DocumentationEncodingTests` verifies completed-task skip and partial-task delta rules.
  - Доказательство: `DocumentationEncodingTests` 8/8, targeted docs/release/encoding suite, backend full suite `731/731`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-duplicate-task-guard`, версия `0.448.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-171` Agent local DB scenario scope guard. 2026-07-02.
  - Что сделать: agent instructions must explicitly pin local DB validation for new user, API, payment, VPN, admin, cabinet and provisioning scenarios.
  - Что сделано: `DocumentationEncodingTests` now verifies the full local DB scenario list from `AGENTS.md`, including user, API, payment, VPN, admin, cabinet and provisioning scopes.
  - Доказательство: `DocumentationEncodingTests` 9/9, targeted docs/release/encoding suite, backend full suite `732/732`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-local-db-scope-guard`, версия `0.449.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-172` Agent verification handoff guard. 2026-07-02.
  - Что сделать: agent instructions must require checks, local DB/SQLite validation, What's New updates, artifact cleanup and final `git status` before commit.
  - Что сделано: `AGENTS.md` now has a Verification And Release Handoff section, and `DocumentationEncodingTests` verifies the required verification-to-release handoff order.
  - Доказательство: `DocumentationEncodingTests` 10/10, targeted docs/release/encoding suite, backend full suite `733/733`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-verification-handoff-guard`, версия `0.450.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-173` Agent encoding verification guard. 2026-07-02.
  - Что сделать: agent instructions must require encoding checks for changed docs, source files and release seed changes.
  - Что сделано: `AGENTS.md` now has an Encoding Verification section, and `DocumentationEncodingTests` verifies strict UTF-8 without BOM, mojibake marker checks and encoding guard requirements for Russian docs/status text and the release seed.
  - Доказательство: `DocumentationEncodingTests` 11/11, targeted docs/release/encoding suite, backend full suite `734/734`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-encoding-verification-guard`, версия `0.451.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-174` Agent customer chat image comment guard. 2026-07-02.
  - Что сделать: agent instructions must require using customer chat comments above attached images together with OCR/visual text.
  - Что сделано: `AGENTS.md` now explicitly treats a customer chat comment above an image as part of the image task context and gives direct customer clarification priority over ambiguous image interpretation.
  - Доказательство: `DocumentationEncodingTests` 12/12, targeted docs/release/encoding suite, backend full suite `735/735`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-customer-chat-image-comment-guard`, версия `0.452.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-175` Agent end-to-end completion guard. 2026-07-02.
  - Что сделать: agent instructions must require completing implementation requests end-to-end instead of stopping at a plan or partial implementation.
  - Что сделано: `AGENTS.md` now has an End-To-End Task Completion section, and `DocumentationEncodingTests` verifies analysis, code, tests, local DB/SQLite, encoding, docs, What's New, cleanup, final `git status` and commit requirements.
  - Доказательство: `DocumentationEncodingTests` 13/13, targeted docs/release/encoding suite, backend full suite `736/736`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-end-to-end-completion-guard`, версия `0.453.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-176` Agent external evidence boundary guard. 2026-07-02.
  - Что сделать: agent instructions must forbid closing real VPS, staging, live payment, production-like VPN, 3x-ui/x-ui and provider-cabinet roadmap items with only local evidence.
  - Что сделано: `AGENTS.md` now has an External Evidence Boundaries section, and `DocumentationEncodingTests` verifies that local tests, mocks, dry-run and SQLite smoke are preparation only for external-evidence items.
  - Доказательство: `DocumentationEncodingTests` 14/14, targeted docs/release/encoding suite, backend full suite `737/737`, fresh local SQLite smoke, latest "Что нового" `2026-07-02-agent-external-evidence-boundary-guard`, версия `0.454.0`. Real live VPS/staging/payment/3x-ui evidence is still required, so production/live items remain open.
- [x] `P11-ACC-177` Полный локальный аудит качества приложения. 2026-08-04.
  - Что сделать: проверить backend/frontend, локальную SQLite-логику, все public/cabinet/admin экраны, основные операции, browser console и адаптивность на representative viewport matrix.
  - Что сделано: исправлены системные часы административных операций, mobile horizontal overflow, счетчик разделов админки, high advisory `postcss` и race encoding guard с временными Playwright-артефактами. All-screens smoke расширен до 5 public routes, кабинета и 16 admin sections на 8 ширинах `305..1920` px.
  - Доказательство: backend `737/737`, frontend `66/66`, typecheck/build OK, Playwright console `12/12`, all-screens `6/6`, fresh local SQLite smoke OK, реальный browser/SQLite flow registration -> login -> sandbox checkout -> cabinet order OK, latest "Что нового" `2026-08-04-full-project-quality-audit`, версия `0.455.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-178` Аудит границ операций API и качества экранов. 2026-08-04.
  - Что сделать: проверить некорректные enum/JSON payload, отсутствие частичных изменений, маршрутизацию payment webhooks, fail-closed VPN provisioning и базовую доступность всех public/cabinet/admin экранов.
  - Что сделано: checkout, кабинет, пользователи, тарифы и referral program возвращают 400 на некорректные типы и undefined enum без частичной записи; напрямую проверены 8 payment webhook routes и admin VPN panel actions; inactive/missing inbound не создает локальных или удаленных клиентов; all-screens gate проверяет `main`, уникальные ID, `alt` и доступные имена контролов. Unsupported Discord/VK/WhatsApp channels явно возвращают 501. Диагностический coverage run показал 51.9% line, 51.8% branch и 79.9% line для controllers; это не заменяет интеграционное production evidence.
  - Доказательство: backend `775/775`, frontend `66/66`, targeted API/SQLite/controller regression OK, all-screens `6/6`, latest "Что нового" `2026-08-04-operation-boundary-quality-audit`, версия `0.456.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-179` Усиление миграций, архивных серверов и frontend security. 2026-08-04.
  - Что сделать: проверить создание migration job/item и его границы, запретить возврат архивных VPN-серверов через mode-actions, устранить мобильную навигационную перегрузку админки и закрыть известные frontend dependency advisories.
  - Что сделано: миграция подписки валидирует текущий и целевой серверы, запрещает одинаковый target и дубли Planned/Running, сохраняет `MigrationItem` и audit; архивные серверы fail-closed для precheck/maintenance/ready/allocation/disable; на ширине до 640 px длинный tablist заменен существующим селектором; порядок счетчика и previous/next синхронизирован с grouped menu; React/React DOM обновлены до 19.2.8, Router до 8.3.0, Node-контракт до 22.22+.
  - Доказательство: backend `778/778`, targeted SQLite boundary tests `3/3`, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, all-screens `6/6`, fresh local SQLite smoke OK, secret scan `559` files/`0` findings, latest "Что нового" `2026-08-04-migration-node-and-frontend-hardening`, версия `0.457.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-180` Целостность команд подписок, истории VPN-узлов и сценариев. 2026-08-04.
  - Что сделать: исключить частично примененные subscription/VPN операции при ошибке провайдера, сохранить исторические health/migration записи при удалении узла, выровнять target миграции с allocation-инвариантами и защитить ключи связанных сценариев.
  - Что сделано: `extend/activate/block/unblock/cancel` меняют подписку только после успешного VPN lifecycle и fail-closed без сервиса; `EndAt == now` считается истекшим; серверы с health-check или migration job архивируются с полными счетчиками в API/UI; migration target отклоняется при `Unhealthy` и заполненной capacity; linked work scenario key нельзя переименовать, а rejected update не мутирует tracked entity; admin E2E выполняет confirmation удаления на desktop/mobile без overflow.
  - Доказательство: backend `797/797`, targeted SQLite/controller suite `38/38`, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, targeted admin desktop/mobile `2/2`, fresh local SQLite smoke OK, secret scan `559` files/`0` findings, latest "Что нового" `2026-08-04-subscription-node-integrity-hardening`, версия `0.458.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-181` Целостность аудита административных операций. 2026-08-04.
  - Что сделать: сопоставить все admin write-endpoint с audit trail, исключить утечку секретов в snapshots, частичную замену release items, tracked mutation после rejected update и зависшие 3x-ui sync-runs.
  - Что сделано: общий redaction-aware audit writer покрывает user/content/release/referral/support/Telegram writes; 3x-ui service сохраняет admin/system actor для panel/inbound/client операций; release items заменяются одним EF save; FAQ/site-content используют candidate-copy; неполная конфигурация панели завершает sync-run как `Failed`.
  - Доказательство: backend `798/798`, targeted SQLite/controller/service suite `73/73`, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `560` files/`0` findings, latest "Что нового" `2026-08-04-admin-operation-audit-integrity`, версия `0.459.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-182` Компенсация отказов и отмена операций 3x-ui. 2026-08-04.
  - Что сделать: отклонять невалидную конфигурацию панели до мутации, завершать отмененный sync-run, сохранять отмену HTTP-вызова и компенсировать созданную target-копию клиента при неудаче удаления source.
  - Что сделано: create/update панели валидируют HTTP(S) URL, password, capacity, enum и JSON object до изменения EF entity; caller cancellation пробрасывается через health/retry и переводит sync-run в `Failed`; миграция удаляет созданную target-копию при source delete failure и пишет redacted audit, а неудачная компенсация помечает необходимость ручной очистки.
  - Доказательство: backend `809/809`, targeted 3x-ui suite `41/41`, API Release build `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `560` files/`0` findings, latest "Что нового" `2026-08-04-x3ui-failure-compensation-hardening`, версия `0.460.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-183` Атомарность локального состояния при синхронизации 3x-ui. 2026-08-04.
  - Что сделать: исключить сохранение частично добавленных/обновленных inbound-ов, sync events и success audit при отмене или исключении в середине panel sync.
  - Что сделано: sync фиксирует снимки исходных inbound-ов и `LastSyncAt`; failure/cancellation path восстанавливает измененные записи, удаляет только новые inbound/events/success audit текущего run, после чего сохраняет отдельный `Failed` audit с независимым cancellation token.
  - Доказательство: backend `811/811`, targeted 3x-ui suite `43/43`, API Release build `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `560` files/`0` findings, latest "Что нового" `2026-08-04-x3ui-sync-atomicity-hardening`, версия `0.461.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-184` Компенсация remote create при отказе локального commit. 2026-08-04.
  - Что сделать: исключить orphan inbound/client в 3x-ui, если remote create успешен, а локальное сохранение или подготовка notification завершается ошибкой.
  - Что сделано: `IX3UiClient` получил delete-inbound contract с реальным 3x-ui endpoint; admin create и provider auto-create удаляют remote inbound после save failure; новая выдача удаляет remote client и восстанавливает local capacity/notification state; двойной отказ возвращает явное требование ручной provider cleanup и audit для admin inbound.
  - Доказательство: backend `817/817`, targeted 3x-ui suite `49/49`, API Release build `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `560` files/`0` findings, latest "Что нового" `2026-08-04-x3ui-remote-create-compensation`, версия `0.462.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-185` Устойчивость refund при concurrency и отказе локального commit. 2026-08-04.
  - Что сделать: исключить двойной provider refund при параллельных запросах, запретить новый возврат при незавершённой операции и не терять remote outcome при ошибке финального локального сохранения или cancellation.
  - Что сделано: `PaymentOrchestrator` сохраняет durable `New` reservation до provider call, повторно читает payment state под освобождаемым order gate и дедуплицирует одинаковый idempotency key; `New/Pending/Unknown` блокируют новый refund; неоднозначный provider outcome сохраняется как `Unknown` независимым token без изменения подтверждённой refunded amount/status.
  - Доказательство: backend `824/824`, targeted payment/refund suite `147/147`, API Release build `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `561` files/`0` findings, latest "Что нового" `2026-08-04-payment-refund-commit-resilience`, версия `0.463.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-186` Устойчивость payment init при concurrency и отказе локального commit. 2026-08-04.
  - Что сделать: исключить повторный provider checkout при параллельном init, не вызывать провайдера без durable reservation, корректно восстановить remote outcome после final save failure и запретить checkout для уже оплаченных intermediate order states.
  - Что сделано: payment init захватывает освобождаемый order gate и перечитывает order; local `New` payment сохраняется до provider call; transient final save повторяется независимым token, двойной failure остается retriable по тому же idempotency key; `PaidAt`, `FulfillmentInProgress` и `PartiallyProcessed` отклоняются до внешнего вызова.
  - Доказательство: backend `831/831`, targeted payment/refund suite `154/154`, API Release build `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `562` files/`0` findings, latest "Что нового" `2026-08-04-payment-init-commit-resilience`, версия `0.464.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-187` Восстановление payment webhook после временных отказов. 2026-08-04.
  - Что сделать: не терять provider webhook после init race, verifier/activation/commit failure или зависшего processing lease, не активировать оплату дважды и возвращать корректный retriable HTTP status.
  - Что сделано: события получили atomic claim и десятиминутный lease; `Failed` и stale `Received/Verified` повторяются, permanent rejection остается terminal; API различает 503 и 400; повтор provisioning продолжает subscription по `LastPaymentId`, не создавая вторую подписку и не продлевая renewal повторно; completed order восстанавливает activation marker.
  - Доказательство: backend `839/839`, targeted payment webhook suite `63/63`, API Release build `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `563` files/`0` findings, latest "Что нового" `2026-08-04-payment-webhook-recovery`, версия `0.465.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-188` Компенсация выдачи VPN после отказа локального activation commit. 2026-08-04.
  - Что сделать: не оставлять orphan remote access после успешного provider create и отказа локального credential save, не терять provider ID при неудачной компенсации, пробрасывать caller cancellation и не продлевать renewal повторно.
  - Что сделано: `SubscriptionService` вызывает remote delete с независимым token после известного provider ID и неуспешного local save; при cleanup failure сохраняет credential как `SyncRequired` для update/reconciliation без второго create; cancellation независимо фиксирует `PendingActivation`, `PartiallyProcessed` и audit, затем пробрасывается; provisioning failure помечается retryable, а повтор renewal дедуплицируется по `LastPaymentId`.
  - Доказательство: backend `843/843`, targeted subscription/X3Ui suite `48/48`, API Release build `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `564` files/`0` findings, latest "Что нового" `2026-08-04-subscription-activation-compensation`, версия `0.466.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-189` Восстановление и дедупликация Telegram update processing. 2026-08-04.
  - Что сделать: исключить двойную маршрутизацию одинакового `update_id`, сохранить durable reservation до invoice/provisioning side effects, восстанавливать failed/stale updates и не терять long-polling offset при временном конфликте.
  - Что сделано: update получает process-local gate и сохраняется до `RouteAsync`; fresh unprocessed row защищен десятиминутным lease и возвращает retryable 503, failed/stale row захватывается условным DB update; cancellation независимо сохраняет retry marker; long-polling сдвигает offset только после non-retryable результата.
  - Доказательство: backend `850/850`, targeted Telegram suite `69/69`, API и TelegramBot Release builds `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `564` files/`0` findings, latest "Что нового" `2026-08-04-telegram-update-recovery`, версия `0.467.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-190` Восстановление доставки ответа Telegram после обработанного update. 2026-08-04.
  - Что сделать: не терять message или pre-checkout acknowledgement после успешной бизнес-обработки и transport/commit failure, не выполнять команду повторно, исключить параллельную доставку и восстанавливать pending response после рестарта long-polling.
  - Что сделано: response и pre-checkout payload сохраняются в `TelegramBotUpdate`; отдельные completion timestamps сохраняют partial progress; delivery получает process gate, conditional DB claim, минутную lease и exponential backoff; duplicate webhook доставляет pending payload без `RouteAsync`; long-polling сканирует due deliveries; оба Telegram HTTP client выбрасывают ошибку при missing BotToken и non-2xx.
  - Доказательство: backend `860/860`, targeted Telegram delivery suite `17/17`, EF migration list и PostgreSQL migration SQL OK, API и TelegramBot Release builds `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `569` files/`0` findings, latest "Что нового" `2026-08-04-telegram-response-delivery-recovery`, версия `0.468.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-191` Восстановление и дедупликация Telegram notification dispatcher. 2026-08-04.
  - Что сделать: исключить двойную отправку notification несколькими worker instances, восстанавливать зависший `sending`, корректно фиксировать retry/cancellation/max attempts и не отправлять invalid payload или уведомление заблокированному account.
  - Что сделано: `TelegramNotificationDeliveryService` атомарно захватывает запись по status/`UpdatedAt`, использует минутную lease и process gate; stale `sending` возвращается в delivery, transient failure/cancellation сохраняют redacted backoff, пятая ошибка становится `failed`; blocked account становится `cancelled`; payload и reply markup валидируются до transport call, legacy plain text поддержан.
  - Доказательство: backend `873/873`, targeted Telegram notification suite `17/17`, API и TelegramBot Release builds `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `571` files/`0` findings, latest "Что нового" `2026-08-04-telegram-notification-dispatch-recovery`, версия `0.469.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-192` Конкурентная дедупликация постановки Telegram notifications. 2026-08-04.
  - Что сделать: устранить check-then-add race во всех producer-путях, не создавать вторую активную запись для одинакового события, сохранить атомарность notification с платежной, subscription, support и provisioning транзакцией и безопасно обновить существующие PostgreSQL/SQLite базы.
  - Что сделано: `ApplicationDbContext` автоматически вычисляет стабильный `DeduplicationKey` и сохраняет notification через транзакционный `ON CONFLICT`; `sent/pending/sending` не дублируются, `failed/cancelled` переактивируются, rollback восстанавливает `Added` state; migration и local SQLite repair нормализуют историю, отменяют активные дубли и создают unique index.
  - Доказательство: backend `881/881`, targeted Telegram persistence/delivery/SQLite repair suite `23/23`, PostgreSQL migration SQL и fixed cross-provider hash vector OK, API и TelegramBot Release builds `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `575` files/`0` findings, latest "Что нового" `2026-08-04-telegram-notification-enqueue-deduplication`, версия `0.470.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается обязательным и этим пунктом не закрывается.
- [x] `P11-ACC-193` Восстановление, дедупликация и fail-closed dispatch outbox events. 2026-08-04.
  - Что сделать: перестать помечать outbox обработанным без handler, исключить двойную обработку несколькими worker instances, восстанавливать stale processing, сохранять retry/cancellation/dead-letter и безопасно обновить event identity в PostgreSQL/SQLite.
  - Что сделано: `OutboxMessageDeliveryService` выполняет conditional DB claim с минутной lease, exponential backoff, redacted error и terminal `FailedAt`; `ApplicationDbContext` атомарно дедуплицирует enqueue по unique type/correlation и переактивирует failed событие; local sink материализует `NotificationRequested`/password reset в pending email delivery и валидирует внутренние payment/order events fail-closed; health report разделяет pending и failed outbox.
  - Доказательство: backend `901/901`, targeted outbox/auth/payment/SQLite/observability suite `37/37`, EF model drift отсутствует, migration `20260804131342_OutboxDispatchRecovery` и PostgreSQL SQL проверены, API и TelegramBot Release builds `0` warnings/`0` errors, frontend `66/66`, typecheck/build и dependency audit `0 vulnerabilities` на Node.js 22.22.0, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `581` files/`0` findings, latest "Что нового" `2026-08-04-outbox-dispatch-recovery`, версия `0.471.0`. Реальная отправка email и real VPS/staging/live payment/production-like 3x-ui evidence остаются внешними проверками и этим пунктом не закрываются.
- [x] `P11-ACC-194` Атомарный lifecycle и восстановление provisioning worker. 2026-08-04.
  - Что сделать: исключить двойной deploy несколькими worker instances, восстанавливать зависшие запуски без опасного автоматического replay, ограничить время runner, запретить retry/cancel поверх активного внешнего deploy и безопасно обновить PostgreSQL/SQLite базы.
  - Что сделано: `ProvisioningRunCoordinator` выполняет conditional claim с lease и attempt count; expired claim переводится в redacted failure для явной проверки оператором; active run на node защищен process gate и partial unique index; Ansible runner читает stdout/stderr параллельно, имеет timeout и завершает process tree; API/UI показывают lease и разрешают retry/cancel только для безопасных статусов.
  - Доказательство: backend `918/918`, targeted provisioning/state/SQLite suite `98/98`, migration `20260804134818_ProvisioningWorkerRecovery`, PostgreSQL SQL, local SQLite repair и concurrent worker/queue tests OK, API и TelegramBot Release builds `0` warnings/`0` errors, frontend `68/68`, typecheck/build и dependency audit `0 vulnerabilities`, Playwright console `12/12`, responsive all-screens `6/6`, fresh local SQLite smoke OK, secret scan `588` files/`0` findings, latest "Что нового" `2026-08-04-provisioning-worker-recovery`, версия `0.472.0`. Реальный VPS/staging/live deploy и production-like 3x-ui evidence остаются внешними проверками и этим пунктом не закрываются.
- [x] `P11-ACC-195` Восстанавливаемое истечение подписки и изоляция lifecycle/panel workers. 2026-08-04.
  - Что сделано: `Expired` фиксируется только после успешного отключения VPN-доступа; provider failure и cancellation сохраняют durable lease/backoff, redacted error, audit/history и `SyncRequired` при неопределенном remote state. Conditional claim исключает двойную обработку подписки, stale lease восстанавливается, ручные admin-команды используют тот же gate, а lifecycle, order expiry, panel health и panel sync изолируют сбои отдельных записей. Админка показывает состояние повтора; EF migration и local SQLite repair добавляют lifecycle-поля.
  - Проверка: `SubscriptionLifecycleExpiryTests`, `SubscriptionLifecycleWorkerTests`, `PanelWorkerIsolationTests`, `VpnAccessAutomationMvpTests`, `LocalSqliteSchemaRepairTests`, `AdminSubscriptionManagementTests` и concurrent SQLite regression.
  - Доказательство: backend `926/926`, targeted lifecycle/panel/SQLite suite `36/36`, migration `20260804143027_SubscriptionLifecycleRecovery`, EF model drift отсутствует, local SQLite schema upgrade и smoke OK, frontend `68/68`, typecheck/build OK, Playwright console `12/12`, responsive all-screens `6/6`, ручной admin smoke desktop/`390x844` без overflow и console errors, secret scan `600` files/`0` findings, latest "Что нового" `2026-08-04-subscription-lifecycle-recovery`, версия `0.473.0`. Live email и real VPS/staging/payment/production-like 3x-ui evidence остаются внешними проверками и этим пунктом не закрываются.
- [x] `P11-ACC-196` Межинстансное восстановление panel sync и безопасная health-диагностика. 2026-08-04.
  - Что сделано: partial unique index разрешает только один `Running` sync на панель, пятиминутная lease закрывает зависший run, worker compare-with-observation отклоняет устаревший `LastSyncAt`. Ошибки preflight/secret сохраняют `Unhealthy` history и audit, новые diagnostics redacted/ограничены, migration и local repair очищают потенциально чувствительные legacy errors. Админка показывает health timestamp/error и корректно предпочитает failed run error пустому summary.
  - Проверка: `PanelSyncDurabilityTests`, `PanelWorkerIsolationTests`, `X3UiIntegrationTests`, `LocalSqliteSchemaRepairTests`, SQLite concurrent/fault-injection, admin desktop/mobile Playwright.
  - Доказательство: backend `930/930`, targeted X3Ui/panel/SQLite suite `52/52`, migration `20260804150807_PanelSyncRecovery`, PostgreSQL SQL и EF model drift check OK, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `603` files/`0` findings, latest "Что нового" `2026-08-04-panel-sync-recovery`, версия `0.474.0`. Real VPS/staging/payment/production-like 3x-ui evidence остаются внешними проверками и этим пунктом не закрываются.
- [x] `P11-ACC-197` Согласованная выдача 3x-ui и атомарный учёт capacity. 2026-08-04.
  - Что сделано: операции одной подписки сериализованы; panel/inbound capacity использует optimistic concurrency; проигравший concurrent create удаляет remote-клиента; продление остаётся на назначенном inbound; delete освобождает capacity; remote update/delete/enable/disable и неоднозначный add компенсируются при локальном отказе. SQLite-выбор panel/inbound больше не использует неподдерживаемые типы сортировки.
  - Проверка: `X3UiIntegrationTests` с file-backed SQLite concurrency, last-slot oversubscription, renewal/full-capacity, delete counters и fault-injection remote compensation.
  - Доказательство: backend `939/939`, X3Ui suite `48/48`, migration `20260804155901_VpnCapacityConcurrency`, PostgreSQL history SQL и EF model drift OK, API/TelegramBot Release builds `0` warnings/`0` errors, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `605` files/`0` findings, latest "Что нового" `2026-08-04-vpn-access-provisioning-consistency`, версия `0.475.0`. Production-like 3x-ui/VPS/staging/payment evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-198` Атомарная резервация capacity VPN-ноды. 2026-08-04.
  - Что сделано: последний slot `VpnNode` резервируется conditional `ExecuteUpdate` до remote provisioning; failure/cancellation освобождает резерв и диагностирует сбой компенсации. Продление существующего доступа сохраняет назначенную maintenance/draining ноду, а отсутствующая или недопустимая sandbox-привязка требует явной миграции вместо скрытой смены `CurrentServerId`.
  - Проверка: `VpnNodeCapacityServiceTests`, `SubscriptionActivationResilienceTests` и payment/webhook/scenario regressions; file-backed SQLite с двумя независимыми контекстами, fault/cancellation cleanup и maintenance renewal.
  - Доказательство: backend `942/942`, targeted VPN/payment/SQLite `100/100`, API/TelegramBot Release builds `0` warnings/`0` errors, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `607` files/`0` findings, latest "Что нового" `2026-08-04-vpn-node-capacity-reservation`, версия `0.476.0`. Real VPS/staging/payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-199` Terminal cancellation отзывает VPN-доступ и освобождает всю capacity. 2026-08-04.
  - Что сделать: `Cancelled` не должен оставлять remote/local VPN-клиента и занятые node/panel/inbound slots; provider/local failure и cancellation не должны частично менять подписку или скрывать неопределённое внешнее состояние.
  - Что сделано: `VpnAccessLifecycleService.CancelSubscriptionAsync` выполняет revoke, provider delete, очистку связей и node capacity в relational transaction; X3Ui delete освобождает panel/inbound в той же транзакции, rollback восстанавливает все counters, а provider uncertainty сохраняет `SyncRequired` с redacted audit/history. Повторный cancel идемпотентен, pre-provider cancellation не создаёт ложный marker, admin UI явно подтверждает необратимое удаление.
  - Доказательство: backend `948/948`, targeted subscription cancellation/X3Ui/SQLite `23/23`, real X3Ui adapter и file-backed SQLite local-save fault injection, API/TelegramBot Release builds `0` warnings/`0` errors, EF model drift отсутствует, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `607` files/`0` findings, latest "Что нового" `2026-08-04-terminal-subscription-cancellation`, версия `0.477.0`. Real VPS/staging/payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-200` Атомарный перенос 3x-ui клиента с capacity reservation и rollback. 2026-08-04.
  - Что сделать: manual migration не должна переполнять target panel/inbound, создавать две неконтролируемые remote-копии при гонке или оставлять source/target и counters рассинхронизированными после ambiguous add, source delete, cancellation и local save failure.
  - Что сделано: `X3UiPanelService` сериализует перенос по подписке, атомарно резервирует временные panel/inbound slots до remote add, освобождает source после local commit и выполняет обратный remote move при сбое. Неопределённая компенсация сохраняет резерв и `migration-compensation-failed`; админка исключает заполненные цели, показывает occupancy и подтверждает add-before-delete.
  - Доказательство: backend `954/954`, X3Ui integration `56/56`, targeted migration `9/9`, file-backed SQLite last-slot concurrency и fault/cancellation tests, API/TelegramBot Release builds `0` warnings/`0` errors, EF model drift отсутствует, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `607` files/`0` findings, latest "Что нового" `2026-08-04-x3ui-client-migration-atomicity`, версия `0.478.0`. Real VPS/staging/payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-201` Согласованность ручных client state операций и reset traffic. 2026-08-05.
  - Что сделать: admin enable/disable не должны оставлять provider и БД в разных состояниях после ambiguous update, cancellation или local save failure; необратимый reset traffic не должен терять факт возможной remote-мутации.
  - Что сделано: client actions сериализованы по подписке; enable/disable выполняют reverse provider update и сохраняют failure audit, а неудачная компенсация помечает клиента `client-state-compensation-failed` и связанный доступ `SyncRequired`. Admin и provider reset paths сохраняют `traffic-reset-uncertain`; UI перечитывает состояние после ошибки, показывает reconciliation badge и подтверждает необратимость.
  - Доказательство: backend `961/961`, X3Ui integration `63/63`, targeted client-state/reset `7/7`, ambiguous update, cancellation, compensation failure и local-save fault injection, API/TelegramBot Release builds `0` warnings/`0` errors, EF model drift отсутствует, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `607` files/`0` findings, latest "Что нового" `2026-08-05-x3ui-client-state-reconciliation`, версия `0.479.0`. Real VPS/staging/payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-202` Атомарное редактирование inbound и сериализация panel state. 2026-08-05.
  - Что сделать: concurrent edit/default/sync не должны менять локальную БД и 3x-ui в разном порядке; ambiguous provider update и local commit failure должны восстанавливать исходную remote-конфигурацию или оставлять явное требование ручной reconciliation.
  - Что сделано: inbound create/edit/default и panel sync используют единый panel-scoped gate; edit выполняет reverse update с исходными полями после remote/local failure, а двойной отказ сохраняет redacted `vpn_inbound.update.compensation_failed` audit с `reconciliationRequired`. Concurrent sync после ожидания отклоняется как stale без второго remote call.
  - Доказательство: backend `965/965`, X3Ui integration `67/67`, targeted X3Ui/panel/SQLite `73/73`, inbound update `4/4`, file-backed SQLite concurrent edit, ambiguous timeout, local-save fault injection и compensation failure; API/TelegramBot Release builds `0` warnings/`0` errors, EF model drift отсутствует, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `607` files/`0` findings, latest "Что нового" `2026-08-05-x3ui-inbound-update-reconciliation`, версия `0.480.0`. Real VPS/staging/payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-203` Атомарная health-история панели и capacity invariants. 2026-08-05.
  - Что сделать: успешный remote health-check не должен создавать противоречивые success/failure records после local save failure; concurrent CRUD/health/workers не должны обходить panel gate; capacity нельзя уменьшать ниже занятых slots.
  - Что сделано: update/delete/health/sync/inbound используют единый panel gate; health persistence очищает pending tracker, повторяет запись со стабильным ID и распознаёт уже состоявшийся ambiguous commit. Worker compare-to-observation исключает повторный provider call; panel/inbound update отклоняет capacity ниже `UsedCapacity` до side effects.
  - Доказательство: backend `970/970`, X3Ui integration `72/72`, targeted X3Ui/panel/SQLite `78/78`, health/capacity `5/5`, pre-commit и ambiguous-commit fault injection, file-backed SQLite concurrency; API/TelegramBot Release builds `0` warnings/`0` errors, EF model drift отсутствует, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `607` files/`0` findings, latest "Что нового" `2026-08-05-x3ui-panel-health-consistency`, версия `0.481.0`. Real VPS/staging/payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-204` Согласованность состояния VPN-сервера и capacity invariant. 2026-08-05.
  - Что сделать: server CRUD, health-check, provisioning и mode actions не должны обходить общий node gate; capacity нельзя уменьшать ниже занятых slots; caller cancellation не должна создавать ложный health result.
  - Что сделано: update/delete/health/provisioning/capacity reserve-release/maintenance/allocation используют один node-scoped gate; update валидирует нормализованную capacity до secret rotation и audit, а health-check отдельно пробрасывает caller cancellation без записи `Unhealthy`.
  - Доказательство: backend `974/974`, targeted server management `12/12`, server/provisioning/capacity `86/86`, file-backed SQLite concurrent health/update, shared reservation gate и cancellation regression; API/TelegramBot Release builds `0` warnings/`0` errors, EF model drift отсутствует, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `607` files/`0` findings, latest "Что нового" `2026-08-05-vpn-node-state-consistency`, версия `0.482.0`. Real VPS/staging/payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-205` Единственный default payment provider account. 2026-08-05.
  - Что сделать: concurrent admin create/update не должны сохранять несколько `IsDefault` для одного provider; upgrade должен безопасно очистить существующие дубли; constraint conflict не должен превращаться в HTTP 500.
  - Что сделано: provider/account gates сериализуют config и state actions; partial unique index обеспечивает межпроцессный invariant, migration CTE оставляет последний default, а смена default выполняется двумя save в одной транзакции. Validation работает на prospective entity, unique conflicts возвращают controlled failure, остальные DB errors пробрасываются.
  - Доказательство: backend `981/981`, targeted payment/account `56/56`, default concurrency/migration `7/7`, два независимых file-backed SQLite-контекста, migration cleanup/index SQL, direct EF no-pending-model check; API/TelegramBot Release builds `0` warnings/`0` errors, fresh local SQLite smoke latest release OK, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright console/responsive `12/12`, secret scan `610` files/`0` findings, latest "Что нового" `2026-08-05-payment-provider-default-uniqueness`, версия `0.483.0`. Real VPS/staging/live payment evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-206` Единый выбор payment provider account для public UI и checkout. 2026-08-05.
  - Что сделать: public provider list и фактическая инициализация платежа должны выбирать один настроенный web-аккаунт; неготовый default и различие сортировок не должны приводить к ошибке после показа способа оплаты.
  - Что сделано: общий selector фильтрует web readiness и детерминированно выбирает по `IsDefault`, `CreatedAt`, `Id`; его используют public API и `PaymentOrchestrator`, а Telegram Stars остается отдельным invoice-flow.
  - Доказательство: backend `983/983`, targeted public/account/payment `26/26`, public/checkout regression `4/4`, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright desktop/mobile/all-screens responsive `12/12`, secret scan `610` files/`0` findings, UTF-8 guard `14/14`, latest "Что нового" `2026-08-05-payment-checkout-account-selection`, версия `0.484.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-207` Propagation caller cancellation для manual payment recheck. 2026-08-05.
  - Что сделать: отмена provider status call не должна превращаться в обычный payment failure или скрываться от HTTP/worker caller; отмененный recheck не должен менять платеж, audit или outbox.
  - Что сделано: `RecheckPaymentAsync` отдельно пробрасывает `OperationCanceledException`, когда отменен caller token; controlled handling `NotSupportedException` и business/provider errors сохранено.
  - Доказательство: backend `984/984`, targeted audit/payment/concurrency `14/14`, cancellation regression `1/1`, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright desktop/mobile/all-screens responsive `12/12`, secret scan `610` files/`0` findings, UTF-8 guard `14/14`, latest "Что нового" `2026-08-05-payment-recheck-cancellation`, версия `0.485.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-208` Разделить configuration readiness и live health payment provider account. 2026-08-05.
  - Что сделать: локальная проверка обязательных полей, URL и JSON не должна записывать `Healthy/Unhealthy` или создавать впечатление запроса внешнего кабинета; API и админка должны явно показывать scope проверки.
  - Что сделано: check contract получил `CheckScope=ConfigurationOnly` и отдельный `ConfigurationStatus`; реальный `HealthStatus` остается `Unknown`, прежние синтетические health-маркеры очищаются, public API не публикует их, а admin UI явно говорит о проверке настроек без внешнего запроса.
  - Доказательство: backend `984/984`, targeted payment provider/public API `20/20`, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, frontend `68/68`, typecheck/build, dependency audit `0 vulnerabilities`, Playwright desktop/mobile/all-screens responsive `12/12`, secret scan `610` files/`0` findings, UTF-8 guard `14/14`, latest "Что нового" `2026-08-05-payment-provider-configuration-check`, версия `0.486.0`. Real payment provider cabinet/VPS/staging/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-209` Согласовать доступность продления в кабинете со статусом подписки. 2026-08-05.
  - Что сделать: кабинет не должен предлагать продление, которое backend гарантированно отклоняет для `Blocked/Cancelled`; пользователь должен видеть допустимое следующее действие.
  - Что сделано: единый frontend helper и независимый handler guard блокируют renewal для неподдерживаемых статусов; карточки показывают обращение в поддержку или оформление нового тарифа, не создавая ложной кнопки.
  - Доказательство: backend `986/986`, targeted cabinet SQLite `8/8` с `400` и отсутствием renewal order для `Blocked/Cancelled`, frontend `69/69`, typecheck/build, cabinet desktop/mobile `2/2`, Playwright desktop/mobile/all-screens responsive `12/12` без overflow/console errors, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `610` files/`0` findings, UTF-8 guard `14/14`, latest "Что нового" `2026-08-05-cabinet-renewal-status-guard`, версия `0.487.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-210` Согласовать доступность QR-кода с готовностью VPN-доступа. 2026-08-05.
  - Что сделать: кабинет не должен предлагать QR для доступа без выданного `accessUri`, потому что backend гарантированно возвращает `400`; правило должно одинаково работать во всех карточках и в handler.
  - Что сделано: единый frontend helper проверяет непустой `accessUri`, отключает QR во всех представлениях и показывает причину; handler повторно блокирует программный вызов до API.
  - Доказательство: backend `987/987`, targeted cabinet SQLite `9/9` с `400` для provisioning-доступа без URI, frontend `70/70`, typecheck/build, cabinet desktop/mobile `2/2`, Playwright desktop/mobile/all-screens responsive `12/12` без overflow/console errors, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, latest "Что нового" `2026-08-05-cabinet-qr-availability`, версия `0.488.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-211` Согласовать повторную оплату заказа с backend-сроком действия. 2026-08-05.
  - Что сделать: кабинет не должен предлагать повторную оплату `Expired` заказа или stale `PendingPayment` с прошедшим `expiresAt`, потому что backend всегда отклоняет такой init; пользователю нужен переход к новому заказу.
  - Что сделано: time-aware frontend helper вычисляет эффективное истечение, карточка показывает `Expired` и действие создания нового заказа, а handler повторно блокирует недоступный payment init до API.
  - Доказательство: backend `989/989`, targeted payment initialization SQLite `8/8`, включая явный `Expired` и просроченный `PendingPayment` без provider call/payment row; frontend `71/71`, typecheck/build, cabinet desktop/mobile `2/2`, Playwright desktop/mobile/all-screens responsive `12/12` без overflow/console errors, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, latest "Что нового" `2026-08-05-cabinet-expired-order-payment-guard`, версия `0.489.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-212` Завершить lifecycle публичной access/refresh сессии. 2026-08-05.
  - Что сделать: public login/register не должен терять выданный refresh token; истёкший access должен ротироваться через backend, а logout обязан отзывать refresh session и очищать локальные токены даже при сетевой ошибке.
  - Что сделано: public web хранит оба токена в session storage, проверяет новый access после refresh rotation, отправляет refresh token и bearer в `/api/auth/logout`, всегда очищает локальную сессию и показывает предупреждение, если server revoke не подтверждён.
  - Доказательство: backend `989/989`, targeted auth session SQLite `1/1` с revoke и запретом refresh after logout, frontend `71/71`, typecheck/build, public desktop/mobile `2/2` с `401` rotation, успешным logout и controlled `503` cleanup, Playwright desktop/mobile/all-screens responsive `12/12` без неожиданных console errors/overflow, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, latest "Что нового" `2026-08-05-public-session-lifecycle`, версия `0.490.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-213` Гарантировать локальный выход из кабинета при недоступном revoke API. 2026-08-05.
  - Что сделать: cabinet logout должен отправлять backend bearer/refresh session, но сетевой или server failure не должен оставлять access/refresh токены, профиль, подписки, платежи и VPN-данные в browser state.
  - Что сделано: cleanup перенесён в `finally`; при неподтверждённом revoke кабинет очищает все локальные данные и показывает явное предупреждение с безопасным следующим действием.
  - Доказательство: backend `989/989`, targeted auth session SQLite `1/1`, frontend `71/71`, typecheck/build, cabinet desktop/mobile `2/2` подтверждает bearer/refresh logout payload, success cleanup и controlled `503` cleanup/warning, Playwright desktop/mobile/all-screens responsive `12/12` без неожиданных console errors/overflow, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, latest "Что нового" `2026-08-05-cabinet-logout-failure-cleanup`, версия `0.491.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-214` Завершить lifecycle административной access/refresh сессии. 2026-08-05.
  - Что сделать: admin-panel не должен терять refresh token после входа; оператору нужна rotation текущей сессии, а logout обязан отзывать backend session и очищать локальные токены/данные даже при server failure.
  - Что сделано: admin-panel хранит оба токена в session storage, ротирует пару через `/api/auth/refresh`, повторно загружает данные, отправляет bearer и актуальный refresh token в `/api/auth/logout`, а cleanup выполняет в `finally` с явным предупреждением при неподтверждённом revoke.
  - Доказательство: backend `989/989`, targeted auth session SQLite `1/1` с rotation/revoke и запретом refresh after logout, frontend `71/71`, typecheck/build, admin desktop/mobile `2/2` подтверждает token storage, rotation, logout payload, success cleanup и controlled `503` cleanup/warning, Playwright desktop/mobile/all-screens responsive `12/12` без неожиданных console errors/overflow, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, latest "Что нового" `2026-08-05-admin-session-lifecycle`, версия `0.492.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остаётся внешним и этим пунктом не закрывается.
- [x] `P11-ACC-215` Проверять административную роль до открытия admin-shell. 2026-08-05.
  - Что сделать: успешный общий login обычного пользователя не должен считаться входом в админку; токены нельзя сохранять и shell нельзя показывать до подтверждения защищенного backend policy.
  - Что сделано: admin-panel проверяет `AdminRead` через dashboard до записи access/refresh, при `401/403` выполняет best-effort revoke и остается на login screen; `ApiClientError` сохраняет HTTP status, восстановление сессии корректно переживает StrictMode remount, а all-screens fixture соответствует числовому dashboard DTO.
  - Доказательство: backend `989/989`, targeted RBAC/auth `34/34` с runtime-запретом роли `User`, frontend `72/72`, typecheck/build, admin desktop/mobile `2/2` подтверждает non-admin `403`, revoke, пустой storage и полный admin flow после разрешенного входа, Playwright desktop/mobile/all-screens responsive `12/12` без неожиданных console errors/overflow и `[object Object]`, dependency audit `0 vulnerabilities`, latest "Что нового" `2026-08-05-admin-rbac-admission`, версия `0.493.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-216` Ограничить admin-panel фактическими возможностями административной роли. 2026-08-05.
  - Что сделать: partial roles не должны безусловно загружать чужие API, видеть запрещенные разделы и команды или получать finance/support данные через общий user overview; frontend admission и доступность действий должны следовать единому backend-owned capability contract.
  - Что сделано: защищенный `/api/admin/session` возвращает роли и policy capabilities; admin-panel фильтрует навигацию, запросы и write controls, handlers повторно проверяют permission до network call, read-only роль получает явный режим просмотра; `AdminUsersController` редактирует finance и support домены по соответствующим read-policy.
  - Доказательство: backend `996/996`, targeted admin policy/session/user overview `50/50`, включая SQLite redaction для Finance и Support; frontend `77/77`, typecheck/build, admin desktop/mobile `4/4` подтверждает полную и Finance-сессию без запрещенных запросов/контролов, Playwright desktop/mobile/all-screens responsive `14/14` без неожиданных console errors/overflow, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, latest "Что нового" `2026-08-05-admin-capability-aware-ui`, версия `0.494.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-217` Редактировать dashboard по доменным правам административной роли. 2026-08-05.
  - Что сделать: Support/Operator без `FinanceRead` не должны получать финансовые агрегаты и payment readiness, Finance без `SupportRead` не должен получать размеры support queue, а роли без `BotManage` не должны видеть Telegram readiness или переходы в недоступный раздел.
  - Что сделано: dashboard определяет capabilities по серверной policy matrix, не выполняет недоступные finance/support/Telegram запросы, возвращает нулевые скрытые агрегаты и фильтрует readiness checks; admin-panel скрывает доменные tiles, последние заказы, attention items и недоступные readiness actions, сохраняя role-specific описание.
  - Доказательство: backend `999/999`, targeted dashboard/automation/sandbox `29/29`, SQLite matrix Support/Finance/Admin `3/3`; frontend `77/77`, typecheck/build, Support desktop/mobile `2/2` подтверждает отсутствие finance metrics/orders/payment actions/API requests, видимую support queue и отсутствие overflow, Playwright desktop/mobile/all-screens responsive `16/16` без неожиданных console errors, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `615` files/`0` findings, latest "Что нового" `2026-08-05-admin-dashboard-domain-redaction`, версия `0.495.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-218` Ограничить журнал аудита доменными правами административной роли. 2026-08-05.
  - Что сделать: partial roles не должны получать finance/support/Telegram audit actions, entity types и `BeforeJson`/`AfterJson` без соответствующих `FinanceRead`, `SupportRead` или `BotManage`; пользовательские Action/EntityType/Search фильтры не должны обходить scope.
  - Что сделано: backend применяет capability-derived domain exclusions до пользовательских фильтров и fail-closed обрабатывает отсутствие role claims; admin-panel показывает только доступные категории аудита, а Finance/Support browser regressions проверяют отсутствие чужих записей и payload.
  - Доказательство: backend `1004/1004`, targeted audit SQLite role matrix `9/9` для Support/Finance/Operator/ReadOnly/Admin, включая прямую попытку обхода Action/EntityType/Search; frontend `77/77`, typecheck/build, Finance/Support desktop/mobile, Playwright desktop/mobile/all-screens responsive `16/16` без неожиданных console errors/overflow, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `615` files/`0` findings, latest "Что нового" `2026-08-05-admin-audit-domain-scope`, версия `0.496.0`. Real VPS/staging/live payment/production-like 3x-ui evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-219` Сохранять cancellation и reconciliation state VPN access lifecycle. 2026-08-05.
  - Что сделать: enable/sync/reset traffic не должны поглощать caller cancellation или пытаться сохранить diagnostics отмененным token; после потенциально выполненного enable/reset локальный статус не должен оставаться заведомо достоверным, а UI-обещание `SyncRequired` для reset uncertainty должно выполняться backend.
  - Что сделано: enable/sync/reset имеют отдельные cancellation paths с durable history/audit через `CancellationToken.None` и повторным throw; enable/reset cancellation и любой reset failure маркируют доступ `SyncRequired`, read-only sync cancellation сохраняет исходный status.
  - Доказательство: backend `1008/1008`, targeted access lifecycle SQLite `8/8`, включая enable/sync/reset cancellation и reset failure после очистки EF tracker, расширенный X3Ui/admin/subscription suite `117/117`; frontend `77/77`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16` без неожиданных console errors/overflow, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `615` files/`0` findings, latest "Что нового" `2026-08-05-vpn-access-cancellation-reconciliation`, версия `0.497.0`. Production-like 3x-ui cancellation/reconciliation evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-220` Сделать `Revoked` терминальным состоянием VPN-доступа во всех API и UI. 2026-08-05.
  - Что сделать: отозванный credential не должен возвращать URI, QR payload/path, config path или provider access ID пользователю; пользовательские и административный QR routes, reset traffic и UI-команды должны fail-closed без provider/network вызова, включая stale-cache состояние.
  - Что сделано: `/api/me/subscriptions` и `/api/me/accesses` редактируют terminal secrets, оба пользовательских QR route и admin QR route отклоняют `Revoked`, lifecycle блокирует reset до provider call; кабинет и админка скрывают URI/QR/copy/provider actions и уже загруженный QR, а current summary не выбирает отозванный credential.
  - Доказательство: backend `1011/1011`, targeted lifecycle/cabinet/admin SQLite `23/23`, frontend `77/77`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16` с намеренно переданными revoked secrets без утечки/команд, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `615` files/`0` findings, latest "Что нового" `2026-08-05-revoked-vpn-access-terminal-guard`, версия `0.498.0`. Production-like 3x-ui evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-221` Сделать `Cancelled` терминальным состоянием подписки для административных команд. 2026-08-05.
  - Что сделать: отменённая подписка не должна принимать sync или другие mutation/provider-команды даже при несогласованном legacy current access; admin UI должен показывать такую запись только для просмотра и истории.
  - Что сделано: backend отклоняет sync до provider call, history и audit; admin-panel использует единую fail-closed матрицу для rendering и handler guard и скрывает все controls у `Cancelled` и неизвестных статусов.
  - Доказательство: backend `1012/1012`, targeted admin subscription SQLite `22/22`, frontend `80/80`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16` без terminal controls и неожиданных console errors/overflow, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `617` files/`0` findings, latest "Что нового" `2026-08-05-cancelled-subscription-terminal-guard`, версия `0.499.0`. Production-like 3x-ui evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-222` Закрыть stale VPN access отменённой подписки во всех admin API и UI. 2026-08-05.
  - Что сделать: `Cancelled` parent должен блокировать enable/disable/sync/reset/QR/migration независимо от credential status; direct endpoints должны сериализоваться с отменой, а admin list/user overview не должны раскрывать stale URI/provider/QR/config.
  - Что сделано: lifecycle service проверяет parent status до provider/history/audit; direct endpoints повторно читают state внутри subscription gate; admin projections редактируют secrets и возвращают terminal marker, UI использует единый fail-closed helper.
  - Доказательство: backend `1018/1018`, targeted access/admin/user SQLite `51/51`, frontend `82/82`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16` с adversarial stale secret без утечки/команд, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `619` files/`0` findings, latest "Что нового" `2026-08-05-cancelled-subscription-access-boundary`, версия `0.500.0`. Production-like 3x-ui evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-223` Закрыть stale VPN access отменённой подписки в кабинете. 2026-08-05.
  - Что сделать: user subscription/access projections и оба QR route должны учитывать `Cancelled` parent независимо от credential status; QR должен повторно проверять state после lifecycle gate, а все cabinet views должны скрывать stale secrets/actions.
  - Что сделано: `/api/me/subscriptions` и `/api/me/accesses` редактируют URI/provider/QR/config и возвращают terminal metadata; оба QR route сериализованы с cancel; единый cabinet helper закрывает current summary, subscription cards и оба access lists.
  - Доказательство: backend `1021/1021`, targeted cabinet SQLite `13/13` с двумя gate race regression, frontend `83/83`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16` с adversarial raw URI/QR/config без утечки/кнопок, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `619` files/`0` findings, latest "Что нового" `2026-08-05-cancelled-subscription-cabinet-boundary`, версия `0.501.0`. Production-like 3x-ui evidence остается внешним и этим пунктом не закрывается.
- [x] `P11-ACC-224` Немедленно закрывать сессии и каналы деактивированного пользователя. 2026-08-05.
  - Что сделать: блокировка, suspension или deletion должны отзывать refresh-сессии, останавливать уже выданный JWT на следующем запросе, очищать чувствительное состояние кабинета и запрещать операции linked Telegram account.
  - Что сделано: JWT bearer повторно проверяет `User.Status`/`IsBlocked`; admin patch атомарно отзывает все refresh tokens; кабинет очищает storage и загруженные VPN/платёжные/support данные после 401/403; Telegram guard закрывает команды, callback и pre-checkout, сохраняя обработку уже состоявшегося `successful_payment`.
  - Доказательство: backend `1027/1027`, targeted auth/admin/Telegram `48/48`, SQLite active-user validator и Telegram payment/access regressions, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, fresh local SQLite checkout/webhook/subscription/VPN access, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan без findings, latest "Что нового" `2026-08-05-active-user-session-boundary`, версия `0.502.0`. Real VPS/staging/live payment/3x-ui evidence остается открытым.
- [x] `P11-ACC-225` Версионировать access- и refresh-сессии пользователя. 2026-08-05.
  - Что сделать: смена пароля, блокировка с последующей разблокировкой, изменение ролей и admin bootstrap не должны оставлять старый JWT действующим или позволять старому refresh-токену восстановить доступ при гонке ротации; browser UI должен немедленно очищать локальную сессию после успешного reset password.
  - Что сделано: `User.SessionVersion` входит в JWT, `UserRefreshToken.SessionVersion` фиксирует поколение refresh-сессии, active-user validator и refresh endpoint работают fail-closed; password reset, active-to-inactive admin patch и изменяющий полномочия bootstrap повышают версию и отзывают refresh-токены; public/cabinet очищают storage и приватное состояние после смены пароля. Добавлены PostgreSQL migration и idempotent local SQLite repair.
  - Доказательство: backend `1030/1030`, targeted auth/admin/bootstrap/SQLite schema `32/32`, frontend `84/84`, typecheck/build, Playwright public/cabinet desktop/mobile и полный responsive/console matrix `16/16`, fresh local SQLite smoke, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan без findings, latest "Что нового" `2026-08-05-versioned-auth-sessions`, версия `0.503.0`. Real VPS/staging/live payment/3x-ui evidence остается открытым.
- [x] `P11-ACC-226` Изолировать refresh-token replay по семействам сессий. 2026-08-05.
  - Что сделать: replay токена, отозванного logout/password reset, не должен отзывать новые независимые входы; replay rotation ancestor должен закрывать только его потомков, включая цепочки, созданные до появления FamilyId.
  - Что сделано: каждый login создаёт отдельный `FamilyId`, rotation наследует его, replay проверяет `session_version` до отзыва и закрывает только active rows той же семьи; legacy NULL-family цепочка обходится через `ReplacedByTokenHash` с защитой от циклов и нормализуется при обнаружении. Добавлены PostgreSQL migration, составной индекс и idempotent local SQLite repair.
  - Доказательство: backend `1032/1032`, targeted auth/password-reset/SQLite schema `19/19`, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, fresh local SQLite smoke, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan без findings, latest "Что нового" `2026-08-05-refresh-token-family-boundary`, версия `0.504.0`. Real VPS/staging/live payment/3x-ui evidence остается открытым.
- [x] `P11-ACC-227` Сделать password reset tokens одноразовым атомарным lifecycle. 2026-08-05.
  - Что сделать: успешный reset должен инвалидировать все остальные outstanding tokens пользователя; два кода, загруженные конкурентными API-инстансами, не должны оба изменить пароль.
  - Что сделано: sibling tokens получают `InvalidatedAt`/`InvalidationReason`, consumed и invalidated rows повышают concurrency `Revision`, EF обновляет их в одном SaveChanges transaction и преобразует `DbUpdateConcurrencyException` в controlled `invalid_or_expired_reset_token`. Добавлены PostgreSQL migration и idempotent local SQLite repair.
  - Доказательство: backend `1034/1034`, targeted auth/session/SQLite schema `21/21`, последовательный takeover regression и cross-context optimistic concurrency, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, fresh local SQLite smoke, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan без findings, latest "Что нового" `2026-08-05-password-reset-token-lifecycle`, версия `0.505.0`. Real VPS/staging/live payment/3x-ui evidence остается открытым.
- [x] `P11-ACC-228` Убрать двойную загрузку кабинета после смены auth-сессии. 2026-08-05.
  - Что сделать: login/register/refresh не должны одновременно загружать защищенные ресурсы из handler и token effect, заменяя интерактивные DOM-узлы во время действий пользователя.
  - Что сделано: token effect гидратирует только сессию, восстановленную при первом mount; handlers новых и обновленных сессий выполняют один явный `loadAll`. Cabinet E2E считает запросы `/api/me` и требует ровно одну загрузку на relogin.
  - Доказательство: frontend `84/84`, cabinet typecheck/build, targeted desktop cabinet `1/1`, Playwright desktop/mobile/all-screens responsive `16/16` без detached-element race, dependency audit `0 vulnerabilities`, latest "Что нового" `2026-08-05-password-reset-token-lifecycle`, версия `0.505.0`. External production evidence этим локальным UI-пунктом не подменяется.
- [x] `P11-ACC-229` Сделать конкурентную регистрацию одного email идемпотентной на API boundary. 2026-08-05.
  - Что сделать: два запроса, прошедшие email pre-check до первого commit, не должны отдавать HTTP 500 или оставлять частичные user/session/audit rows; unrelated DB failures нельзя маскировать как duplicate email.
  - Что сделано: точный unique conflict `IX_Users_Email`/`Users.Email` преобразуется в `email_exists`, failed tracker очищается, а остальные `DbUpdateException` пробрасываются. Энтропия нового referral code увеличена с 24 до 64 случайных бит без изменения существующих кодов.
  - Доказательство: backend `1036/1036`, targeted auth/session/SQLite schema `24/24`, file-backed SQLite race после успешного `AnyAsync`, negative storage-failure regression, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, fresh local SQLite smoke, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `629` files/`0` findings, latest "Что нового" `2026-08-05-registration-email-race-boundary`, версия `0.506.0`. External production evidence остается открытым.
- [x] `P11-ACC-230` Сделать повторную выдачу password reset code newest-generation-only. 2026-08-05.
  - Что сделать: новый `forgot-password` должен немедленно закрывать старый код; конкурентные выдачи и stale reset не должны обе commit-иться, а admin bootstrap со сменой пароля не должен оставлять ранее выданный код действующим.
  - Что сделано: отдельный `PasswordResetState` хранит generation и optimistic `Revision`; reissue инвалидирует outstanding rows с `password_reset_reissued`, retry разрешает concurrent first-state insert, reset atomically повышает generation, а stale commit получает controlled отказ. Bootstrap повышает generation только при явной смене пароля. Добавлены PostgreSQL migration и idempotent local SQLite repair.
  - Доказательство: backend `1039/1039`, targeted auth/bootstrap/SQLite schema `31/31`, sequential reissue, две file-backed cross-context гонки и legacy generation 0, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, fresh local SQLite smoke, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `631` files/`0` findings, latest "Что нового" `2026-08-05-password-reset-generation-boundary`, версия `0.507.0`. External production evidence остается открытым.
- [x] `P11-ACC-231` Сделать rotation одного refresh token атомарной между API-инстансами. 2026-08-05.
  - Что сделать: два запроса с одним source token не должны оба выпускать active child; concurrent logout и admin deactivation не должны оставлять новую ветвь или возвращать необработанный DB conflict.
  - Что сделано: `UserRefreshToken.Revision` является optimistic concurrency token и повышается всеми mutation paths; stale rotation откатывает child/audit и выполняет reuse family revoke, logout перечитывает семью, logout-all повышает `SessionVersion`, admin patch повторяет транзакцию. Добавлены PostgreSQL migration и idempotent local SQLite repair.
  - Доказательство: backend `1043/1043`, targeted auth/admin/bootstrap/SQLite/PostgreSQL `43/43`, три file-backed cross-context fault-injection regression и SQLite logout-all, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, fresh local SQLite smoke, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `633` files/`0` findings, latest "Что нового" `2026-08-05-refresh-token-rotation-concurrency`, версия `0.508.0`. External production evidence остается открытым.
- [x] `P11-ACC-232` Сделать жизненный цикл Telegram link-token атомарным и newest-only. 2026-08-05.
  - Что сделать: перевыпуск ссылки должен немедленно закрывать старую, один token и один пользователь не должны одновременно привязываться к двум Telegram ID, а unlink должен отзывать ожидающие ссылки.
  - Что сделано: отдельный `TelegramLinkState` хранит generation и optimistic `Revision`; reissue инвалидирует outstanding links, consume повышает generation, `TelegramBotDeepLink.Revision` отклоняет stale commit, filtered unique index защищает `TelegramAccounts.UserId`. PostgreSQL migration отзывает legacy links и очищает дубли, idempotent local SQLite repair выполняет тот же переход.
  - Доказательство: backend `1048/1048`, targeted Telegram/cabinet/admin/SQLite `131/131`, lifecycle/schema-repair `22/22`, file-backed concurrent reissue и cross-context consumption regressions, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, fresh local SQLite smoke, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `635` files/`0` findings, latest "Что нового" `2026-08-05-telegram-link-lifecycle-concurrency`, версия `0.509.0`. External production evidence остается открытым.
- [x] `P11-ACC-233` Защитить renewal validation и создание pending-заказа на application/DB boundary. 2026-08-05.
  - Что сделать: внутренний вызов order service не должен создавать renewal для отсутствующей, чужой, terminal или tariff-mismatched подписки; два API-инстанса не должны сохранить два активных pending-заказа одного намерения, а legacy renew route не должен возвращать ложный успех.
  - Что сделано: `OrderService` централизованно проверяет renewal subscription; SHA-256 intent key и filtered unique index сериализуют active `PendingPayment`, проигравший unique conflict возвращает winning order, stale intent переводится в `Expired`. Legacy endpoint отвечает `410 Gone`; PostgreSQL migration и idempotent SQLite repair добавляют rollout-совместимую схему.
  - Доказательство: backend `1053/1053`, targeted order/cabinet/Telegram/SQLite `74/74`, deterministic concurrent-winner и stale-replacement regressions, PostgreSQL migration SQL, EF model drift отсутствует, local SQLite legacy repair idempotent, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, fresh local SQLite smoke, API/TelegramBot Release builds `0` warnings/`0` errors, dependency audit `0 vulnerabilities`, secret scan `637` files/`0` findings, latest "Что нового" `2026-08-05-pending-order-intent-concurrency`, версия `0.510.0`. External production evidence остается открытым.
- [x] `P11-ACC-234` Защитить жизненный цикл обращения поддержки от stale mutations. 2026-08-05.
  - Что сделать: устаревшие reply/status/note не должны перезаписывать новые сообщения; pending-диалог должен возвращаться в active queue после входящего ответа, а ответственным нельзя назначить пользователя без support-доступа.
  - Что сделано: `SupportConversation.Revision` является optimistic concurrency token и повышается cabinet/admin/Telegram/provisioning mutation paths; API требует expected revision и возвращает `409`, UI перечитывает очередь. Pending reopen выполняется для Telegram/provisioning, assignment валидирует active non-blocked `SupportWrite`, cabinet internal-message filter работает fail-closed. Добавлены PostgreSQL migration и idempotent local SQLite repair.
  - Доказательство: backend `1059/1059`, targeted support/Telegram/provisioning/SQLite `56/56`, stale status/assignment/pending reopen regressions для service и worker writers, PostgreSQL migration и EF model drift check, локальная SQLite schema upgrade, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, API/TelegramBot Release builds `0` warnings/`0` errors, dependency audit `0 vulnerabilities`, secret scan `639` files/`0` findings, latest "Что нового" `2026-08-05-support-conversation-concurrency`, версия `0.511.0`. External production evidence остается открытым.
- [x] `P11-ACC-235` Разделить владельца customer VPS и инициатора provisioning-команды. 2026-08-05.
  - Что сделать: admin queue/deploy/retry не должны заменять владельца customer VPS идентификатором оператора и выдавать оператору клиентскую подписку, VPN access или support context; фактический инициатор должен сохраняться в аудите.
  - Что сделано: `ProvisioningRun.RequestedByUserId` сохранен как owner boundary. Queue/deploy/retry для customer-owned node восстанавливают владельца из канонического `requested-user-id` с fallback на раннюю owner-history, поэтому исправляется и ранее загрязненная run; `provisioning.queue` пишет admin actor отдельно в `AuditLog.ActorId` и owner в payload. Схема БД не менялась.
  - Доказательство: backend `1060/1060`, targeted provisioning/worker `33/33`, SQLite queue/deploy/retry regression, sandbox worker E2E с клиентской subscription/access и отсутствием actor subscription, frontend `84/84`, typecheck/build, Playwright desktop/mobile/all-screens responsive `16/16`, API/TelegramBot Release builds `0` warnings/`0` errors, EF pending model changes отсутствуют, dependency audit `0 vulnerabilities`, secret scan `639` files/`0` findings, latest "Что нового" `2026-08-05-provisioning-owner-actor-boundary`, версия `0.512.0`. External production evidence остается открытым.
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

- [x] `P11-ACC-073` Production evidence bundle latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-evidence-bundle.ps1 -RequireProductionReady` сверяет `releaseId` каждого обязательного report с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому stale production-ready bundle падает до упаковки manifest/archive/handoff.
  - Доказательство: `ProductionReadinessGateTests` 65/65, `scripts/test-production-evidence-bundle-latest-release-guard.ps1`, production evidence bundle latest release guard regression, backend full suite `607/607`, latest "Что нового" `2026-07-01-production-evidence-bundle-latest-release-guard`, версия `0.312.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-074` Production handoff package archive latest release guard. 2026-07-01.
  - Что сделано: `scripts/validate-production-evidence-handoff-package-archive.ps1 -RequireProductionReady` сверяет `releaseId` package index внутри ZIP с latest active release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, поэтому stale handoff package archive падает до повторного package validator.
  - Доказательство: `ProductionReadinessGateTests` 66/66, `scripts/test-production-evidence-handoff-package-archive-latest-release-guard.ps1`, production handoff package archive latest release guard regression, backend full suite `608/608`, latest "Что нового" `2026-07-01-production-handoff-package-archive-latest-release-guard`, версия `0.313.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-075` VPS production smoke report generator release guard. 2026-07-01.
  - Что сделать: не создавать черновик VPS production smoke report с вручную указанным неизвестным `ReleaseId`.
  - Что сделано: `scripts/new-vps-production-smoke-report.ps1 -ReleaseId` сверяет ручной release id с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до записи JSON-артефакта; добавлен regression harness `scripts/test-vps-production-smoke-report-generator-release-guard.ps1`, который доказывает fail-fast поведение и отсутствие созданного отчета.
  - Доказательство: `VpsProductionSmokeTests` 9/9, VPS production smoke generator release guard regression, latest "Что нового" `2026-07-01-vps-production-smoke-generator-release-guard`, версия `0.321.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-076` Production evidence bundle generator release guard. 2026-07-01.
  - Что сделать: не создавать каталог production evidence bundle с вручную указанным неизвестным `ReleaseId`.
  - Что сделано: `scripts/new-production-evidence-bundle.ps1 -ReleaseId` сверяет ручной release id с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до создания output directory и запуска дочерних генераторов.
  - Доказательство: `ProductionReadinessGateTests` 67/67, production evidence bundle generator release guard regression, latest "Что нового" `2026-07-01-production-evidence-bundle-generator-release-guard`, версия `0.325.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-077` Production evidence manifest release guard. 2026-07-01.
  - Что сделать: не создавать production evidence manifest для bundle с неизвестным `releaseId`.
  - Что сделано: `scripts/new-production-evidence-manifest.ps1` сверяет release id из bundle с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до записи `production-evidence-manifest.json`.
  - Доказательство: `ProductionReadinessGateTests` 68/68, production evidence manifest release guard regression, backend full suite `621/621`, latest "Что нового" `2026-07-01-production-evidence-manifest-release-guard`, версия `0.326.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-078` Production evidence archive release guard. 2026-07-01.
  - Что сделать: не создавать production evidence ZIP для manifest с неизвестным `releaseId`.
  - Что сделано: `scripts/new-production-evidence-archive.ps1` сверяет release id из валидированного manifest с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до записи ZIP-архива.
  - Доказательство: `ProductionReadinessGateTests` 69/69, production evidence archive release guard regression, backend full suite `622/622`, latest "Что нового" `2026-07-01-production-evidence-archive-release-guard`, версия `0.327.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-079` Production evidence handoff receipt release guard. 2026-07-01.
  - Что сделать: не создавать production evidence handoff receipt для archive manifest с неизвестным `releaseId`.
  - Что сделано: `scripts/new-production-evidence-handoff-receipt.ps1` сверяет release id из валидированного ZIP-архива с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до записи JSON/Markdown receipt.
  - Доказательство: `ProductionReadinessGateTests` 70/70, production evidence handoff receipt release guard regression, backend full suite `623/623`, latest "Что нового" `2026-07-01-production-handoff-receipt-release-guard`, версия `0.328.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-080` Production evidence handoff checklist release guard. 2026-07-01.
  - Что сделать: не создавать production evidence handoff checklist для receipt с неизвестным `releaseId`.
  - Что сделано: `scripts/new-production-evidence-handoff-checklist.ps1` сверяет release id из валидированного receipt с `backend/src/VpnPlatform.Api/AppReleases/releases.json` до записи JSON/Markdown checklist.
  - Доказательство: `ProductionReadinessGateTests` 71/71, production evidence handoff checklist release guard regression, backend full suite `624/624`, latest "Что нового" `2026-07-01-production-handoff-checklist-release-guard`, версия `0.329.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-081` Production evidence handoff receipt verified files guard. 2026-07-01.
  - Что сделать: не принимать production evidence handoff receipt, если `verifiedFiles` не совпадают с фактическими файлами validated archive.
  - Что сделано: `scripts/validate-production-evidence-handoff-receipt.ps1` сверяет `verifiedFiles` receipt с archive validator по `entryName`, `name`, `lengthBytes` и `sha256`; `scripts/test-production-evidence-handoff-receipt-verified-files-guard.ps1` подменяет hash и доказывает fail-closed поведение.
  - Доказательство: `ProductionReadinessGateTests` 72/72, production evidence handoff receipt verified files guard regression, backend full suite `625/625`, latest "Что нового" `2026-07-01-production-handoff-receipt-verified-files-guard`, версия `0.330.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-082` Production evidence handoff receipt Markdown verified files guard. 2026-07-01.
  - Что сделать: не принимать production evidence handoff receipt, если Markdown-пара не содержит verified file details из JSON.
  - Что сделано: `scripts/validate-production-evidence-handoff-receipt.ps1` проверяет, что Markdown receipt содержит имя, archive entry и SHA256 каждого `verifiedFiles` элемента; `scripts/test-production-evidence-handoff-receipt-markdown-verified-files-guard.ps1` подменяет только Markdown SHA256 и доказывает fail-closed поведение.
  - Доказательство: `ProductionReadinessGateTests` 73/73, production evidence handoff receipt markdown verified files guard regression, backend full suite `626/626`, latest "Что нового" `2026-07-01-production-handoff-receipt-markdown-verified-files-guard`, версия `0.331.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-083` Production evidence handoff checklist Markdown gates guard. 2026-07-01.
  - Что сделать: не принимать production evidence handoff checklist, если Markdown-пара не содержит gate/operator details из JSON.
  - Что сделано: `scripts/validate-production-evidence-handoff-checklist.ps1` проверяет, что Markdown checklist содержит каждое имя/status/message gate и каждое operator action; `scripts/test-production-evidence-handoff-checklist-markdown-gates-guard.ps1` подменяет только Markdown gate message и доказывает fail-closed поведение.
  - Доказательство: `ProductionReadinessGateTests` 74/74, production evidence handoff checklist markdown gates guard regression, backend full suite `627/627`, latest "Что нового" `2026-07-01-production-handoff-checklist-markdown-gates-guard`, версия `0.332.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-084` Production evidence handoff package Markdown files guard. 2026-07-01.
  - Что сделать: не принимать production evidence handoff package, если Markdown package index не содержит artifact file details из JSON.
  - Что сделано: `scripts/validate-production-evidence-handoff-package.ps1` проверяет, что Markdown package index содержит имя, размер и SHA256 каждого artifact file; `scripts/test-production-evidence-handoff-package-markdown-files-guard.ps1` подменяет только Markdown SHA256 и доказывает fail-closed поведение.
  - Доказательство: `ProductionReadinessGateTests` 75/75, production evidence handoff package markdown files guard regression, backend full suite `628/628`, latest "Что нового" `2026-07-01-production-handoff-package-markdown-files-guard`, версия `0.333.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-085` Production evidence handoff package archive duplicate entry guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если внутри архива есть повторяющиеся имена файлов.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-duplicate-entry-guard.ps1` создает ZIP с двумя `SHA256SUMS.txt` entries и доказывает, что `scripts/validate-production-evidence-handoff-package-archive.ps1` падает fail-closed на `duplicated entry`.
  - Доказательство: `ProductionReadinessGateTests` 76/76, production evidence handoff package archive duplicate entry guard regression, backend full suite `629/629`, latest "Что нового" `2026-07-01-production-handoff-package-archive-duplicate-entry-guard`, версия `0.334.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-086` Production evidence handoff package archive nested entry guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если внутри архива есть вложенные пути файлов.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-nested-entry-guard.ps1` создает ZIP с `nested/SHA256SUMS.txt` и доказывает, что `scripts/validate-production-evidence-handoff-package-archive.ps1` падает fail-closed на `unexpected entry`.
  - Доказательство: `ProductionReadinessGateTests` 77/77, production evidence handoff package archive nested entry guard regression, backend full suite `630/630`, latest "Что нового" `2026-07-01-production-handoff-package-archive-nested-entry-guard`, версия `0.335.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-087` Production evidence handoff package archive directory entry guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если внутри архива есть directory entries.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-directory-entry-guard.ps1` создает ZIP с `empty-folder/` и доказывает, что `scripts/validate-production-evidence-handoff-package-archive.ps1` падает fail-closed на `unexpected entry`.
  - Доказательство: `ProductionReadinessGateTests` 78/78, production evidence handoff package archive directory entry guard regression, backend full suite `631/631`, latest "Что нового" `2026-07-01-production-handoff-package-archive-directory-entry-guard`, версия `0.336.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-088` Production evidence handoff package archive backslash entry guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если внутри архива есть entry с Windows-style `\` separator.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-backslash-entry-guard.ps1` создает ZIP с `nested\SHA256SUMS.txt` и доказывает, что `scripts/validate-production-evidence-handoff-package-archive.ps1` падает fail-closed на `unexpected entry`.
  - Доказательство: `ProductionReadinessGateTests` 79/79, production evidence handoff package archive backslash entry guard regression, backend full suite `632/632`, latest "Что нового" `2026-07-01-production-handoff-package-archive-backslash-entry-guard`, версия `0.337.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-089` Production evidence handoff package archive dotdot entry guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если внутри архива есть entry с именем `..`.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-dotdot-entry-guard.ps1` создает ZIP с `..` entry и доказывает, что `scripts/validate-production-evidence-handoff-package-archive.ps1` падает fail-closed на `file name only`.
  - Доказательство: `ProductionReadinessGateTests` 80/80, production evidence handoff package archive dotdot entry guard regression, backend full suite `633/633`, latest "Что нового" `2026-07-01-production-handoff-package-archive-dotdot-entry-guard`, версия `0.338.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-090` Production evidence handoff package archive dot entry guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если внутри архива есть entry с именем `.`.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-dot-entry-guard.ps1` создает ZIP с `.` entry и доказывает, что `scripts/validate-production-evidence-handoff-package-archive.ps1` падает fail-closed на `file name only`.
  - Доказательство: `ProductionReadinessGateTests` 81/81, production evidence handoff package archive dot entry guard regression, backend full suite `634/634`, latest "Что нового" `2026-07-01-production-handoff-package-archive-dot-entry-guard`, версия `0.339.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-091` Production evidence handoff package archive rooted entry guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если внутри архива есть rooted entry вроде `C:\SHA256SUMS.txt`.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-rooted-entry-guard.ps1` создает ZIP с `C:\SHA256SUMS.txt` entry и доказывает, что `scripts/validate-production-evidence-handoff-package-archive.ps1` падает fail-closed на `unexpected entry`.
  - Доказательство: `ProductionReadinessGateTests` 82/82, production evidence handoff package archive rooted entry guard regression, backend full suite `635/635`, latest "Что нового" `2026-07-01-production-handoff-package-archive-rooted-entry-guard`, версия `0.340.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-092` Production evidence handoff package archive whitespace entry guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если внутри архива есть entry с пустым или состоящим из пробелов именем.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-whitespace-entry-guard.ps1` создает ZIP с whitespace-only entry и доказывает, что `scripts/validate-production-evidence-handoff-package-archive.ps1` падает fail-closed на `unexpected entry`.
  - Доказательство: `ProductionReadinessGateTests` 83/83, production evidence handoff package archive whitespace entry guard regression, backend full suite `636/636`, latest "Что нового" `2026-07-01-production-handoff-package-archive-whitespace-entry-guard`, версия `0.341.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-093` Production evidence handoff package archive entry case guard. 2026-07-01.
  - Что сделать: не принимать финальный handoff package ZIP, если обязательное имя entry отличается только регистром, например `sha256sums.txt` вместо `SHA256SUMS.txt`.
  - Что сделано: `scripts/validate-production-evidence-handoff-package-archive.ps1` перешел на точные ordinal-наборы allowed/seen entries; `scripts/test-production-evidence-handoff-package-archive-entry-case-guard.ps1` доказывает fail-closed на case mismatch.
  - Доказательство: `ProductionReadinessGateTests` 84/84, production evidence handoff package archive entry case guard regression, backend full suite `637/637`, latest "Что нового" `2026-07-01-production-handoff-package-archive-entry-case-guard`, версия `0.342.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-094` Production evidence handoff package archive entry case guard artifact cleanup. 2026-07-01.
  - Что сделать: entry-case regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-entry-case-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 85/85, production evidence handoff package archive entry case cleanup regression, backend full suite `638/638`, latest "Что нового" `2026-07-01-production-handoff-package-archive-entry-case-cleanup`, версия `0.343.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-095` Production evidence handoff package archive duplicate entry guard artifact cleanup. 2026-07-01.
  - Что сделать: duplicate-entry regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-duplicate-entry-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 86/86, production evidence handoff package archive duplicate entry cleanup regression, backend full suite `639/639`, latest "Что нового" `2026-07-01-production-handoff-package-archive-duplicate-entry-cleanup`, версия `0.344.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-096` Production evidence handoff package archive nested entry guard artifact cleanup. 2026-07-01.
  - Что сделать: nested-entry regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-nested-entry-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 87/87, production evidence handoff package archive nested entry cleanup regression, backend full suite `640/640`, latest "Что нового" `2026-07-01-production-handoff-package-archive-nested-entry-cleanup`, версия `0.345.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-097` Production evidence handoff package archive directory entry guard artifact cleanup. 2026-07-01.
  - Что сделать: directory-entry regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-directory-entry-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 88/88, production evidence handoff package archive directory entry cleanup regression, backend full suite `641/641`, latest "Что нового" `2026-07-01-production-handoff-package-archive-directory-entry-cleanup`, версия `0.346.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-098` Production evidence handoff package archive backslash entry guard artifact cleanup. 2026-07-01.
  - Что сделать: backslash-entry regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-backslash-entry-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 89/89, production evidence handoff package archive backslash entry cleanup regression, backend full suite `642/642`, latest "Что нового" `2026-07-01-production-handoff-package-archive-backslash-entry-cleanup`, версия `0.347.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-099` Production evidence handoff package archive dotdot entry guard artifact cleanup. 2026-07-01.
  - Что сделать: dotdot-entry regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-dotdot-entry-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 90/90, production evidence handoff package archive dotdot entry cleanup regression, backend full suite `643/643`, latest "Что нового" `2026-07-01-production-handoff-package-archive-dotdot-entry-cleanup`, версия `0.348.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-100` Production evidence handoff package archive dot entry guard artifact cleanup. 2026-07-01.
  - Что сделать: dot-entry regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-dot-entry-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 91/91, production evidence handoff package archive dot entry cleanup regression, backend full suite `644/644`, latest "Что нового" `2026-07-01-production-handoff-package-archive-dot-entry-cleanup`, версия `0.349.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-101` Production evidence handoff package archive rooted entry guard artifact cleanup. 2026-07-01.
  - Что сделать: rooted-entry regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-rooted-entry-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 92/92, production evidence handoff package archive rooted entry cleanup regression, backend full suite `645/645`, latest "Что нового" `2026-07-01-production-handoff-package-archive-rooted-entry-cleanup`, версия `0.350.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-102` Production evidence handoff package archive whitespace entry guard artifact cleanup. 2026-07-01.
  - Что сделать: whitespace-entry regression harness не должен оставлять временный ZIP или пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-whitespace-entry-guard.ps1` удаляет generated ZIP и пустой `tmp`; `ProductionReadinessGateTests` закрепляет cleanup contract.
  - Доказательство: `ProductionReadinessGateTests` 93/93, production evidence handoff package archive whitespace entry cleanup regression, backend full suite `646/646`, latest "Что нового" `2026-07-01-production-handoff-package-archive-whitespace-entry-cleanup`, версия `0.351.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-103` Production evidence handoff package archive entry guard cleanup coverage. 2026-07-01.
  - Что сделать: все archive entry regression harness должны быть покрыты единым cleanup guard, чтобы новые варианты не оставляли временный ZIP или пустой `tmp`.
  - Что сделано: `ProductionReadinessGateTests` перечисляет все `test-production-evidence-handoff-package-archive-*-entry-guard.ps1` scripts и проверяет `Remove-EmptyDirectory`, удаление generated ZIP и очистку пустого `tmp`.
  - Доказательство: `ProductionReadinessGateTests` 94/94, production evidence handoff package archive entry guard cleanup coverage, backend full suite `647/647`, latest "Что нового" `2026-07-01-production-handoff-package-archive-entry-guard-cleanup-coverage`, версия `0.352.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-104` Production evidence handoff package archive flow default artifact cleanup. 2026-07-01.
  - Что сделать: default archive flow run не должен оставлять autogenerated `tmp/production-evidence-handoff-package-archive-flow-test`, если оператор не запросил явный output или JSON для дальнейшей валидации.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-flow.ps1` удаляет default output после успешного non-JSON запуска, но сохраняет artifacts для explicit `-OutputDirectory` и `-WriteJson` evidence flows.
  - Доказательство: `ProductionReadinessGateTests` 95/95, production evidence handoff package archive default flow cleanup, backend full suite `648/648`, latest "Что нового" `2026-07-01-production-handoff-package-archive-flow-default-cleanup`, версия `0.353.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-105` Production evidence handoff package archive CI regression default artifact cleanup. 2026-07-01.
  - Что сделать: default CI regression wrapper run не должен оставлять autogenerated `tmp/production-evidence-handoff-package-archive-ci-regression-test`, если оператор не запросил явный output или JSON для дальнейшей валидации.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` удаляет default output после успешного non-JSON запуска, но сохраняет artifacts для explicit `-OutputDirectory` и `-WriteJson` evidence flows.
  - Доказательство: `ProductionReadinessGateTests` 96/96, production evidence handoff package archive CI regression default cleanup, backend full suite `649/649`, latest "Что нового" `2026-07-01-production-handoff-package-archive-ci-regression-default-cleanup`, версия `0.354.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-106` Production evidence handoff package archive long-path default artifact cleanup. 2026-07-01.
  - Что сделать: default long-path regression run не должен оставлять autogenerated `tmp/production-evidence-handoff-package-archive-long-release-id-path-regression-test`, если оператор не запросил явный output или JSON для дальнейшей валидации.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-long-path.ps1` удаляет default output после успешного non-JSON запуска, но сохраняет artifacts для explicit `-OutputDirectory` и `-WriteJson` evidence flows.
  - Доказательство: `ProductionReadinessGateTests` 100/100, production evidence handoff package archive long-path default cleanup, backend full suite `650/650`, latest "Что нового" `2026-07-01-production-handoff-package-archive-long-path-default-cleanup`, версия `0.355.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-107` Production readiness assertion CI regression default artifact cleanup. 2026-07-01.
  - Что сделать: default production readiness assertion CI regression run не должен оставлять autogenerated `tmp/production-readiness-assertion-ci-regression-test`, если оператор не запросил явный output или JSON для дальнейшей валидации.
  - Что сделано: `scripts/test-production-readiness-assertion-ci-regression.ps1` удаляет default output после успешного non-JSON запуска, но сохраняет artifacts для explicit `-OutputDirectory` и `-WriteJson` evidence flows.
  - Доказательство: `ProductionReadinessGateTests` 98/98, production readiness assertion CI regression default cleanup, backend full suite `651/651`, latest "Что нового" `2026-07-01-production-readiness-assertion-ci-regression-default-cleanup`, версия `0.356.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-108` Production readiness assertion result latest release guard default artifact cleanup. 2026-07-01.
  - Что сделать: stale-release regression для assertion result не должен оставлять autogenerated `tmp/production-readiness-assertion-result-stale-release-guard.json` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-readiness-assertion-result-latest-release-guard.ps1` удаляет stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `ProductionReadinessGateTests` 99/99, production readiness assertion result latest release guard cleanup, backend full suite `652/652`, latest "Что нового" `2026-07-01-production-readiness-assertion-result-latest-release-guard-cleanup`, версия `0.357.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-109` Production readiness summary latest release guard default artifact cleanup. 2026-07-01.
  - Что сделать: stale-release regression для production readiness summary не должен оставлять autogenerated `tmp/production-readiness-summary-stale-release-guard.md`, `.json` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-readiness-summary-latest-release-guard.ps1` удаляет summary Markdown/JSON и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `ProductionReadinessGateTests` 100/100, production readiness summary latest release guard cleanup, backend full suite `653/653`, latest "Что нового" `2026-07-01-production-readiness-summary-latest-release-guard-cleanup`, версия `0.358.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-110` Production handoff checklist latest release guard default artifact cleanup. 2026-07-01.
  - Что сделать: stale-release regression для production evidence handoff checklist не должен оставлять autogenerated `tmp/production-evidence-handoff-checklist-stale-release-guard.json`, `.md` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-checklist-latest-release-guard.ps1` удаляет checklist JSON/Markdown и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `ProductionReadinessGateTests` 101/101, production handoff checklist latest release guard cleanup, backend full suite `654/654`, latest "Что нового" `2026-07-01-production-handoff-checklist-latest-release-guard-cleanup`, версия `0.359.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-111` Production handoff flow result latest release guard default artifact cleanup. 2026-07-01.
  - Что сделать: stale-release regression для production evidence handoff package archive flow result не должен оставлять autogenerated `tmp/production-evidence-handoff-package-archive-flow-result-stale-release-guard.json` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-flow-result-latest-release-guard.ps1` удаляет stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `ProductionReadinessGateTests` 102/102, production handoff flow result latest release guard cleanup, backend full suite `655/655`, latest "Что нового" `2026-07-01-production-handoff-flow-result-latest-release-guard-cleanup`, версия `0.360.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-112` Production handoff CI result latest release guard default artifact cleanup. 2026-07-01.
  - Что сделать: stale-release regression для production evidence handoff package archive CI result не должен оставлять autogenerated `tmp/production-evidence-handoff-package-archive-ci-regression-result-stale-release-guard.json` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-latest-release-guard.ps1` удаляет stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `ProductionReadinessGateTests` 103/103, production handoff CI result latest release guard cleanup, backend full suite `656/656`, latest "Что нового" `2026-07-01-production-handoff-ci-result-latest-release-guard-cleanup`, версия `0.361.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-113` Production handoff CI summary latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: stale-release regression для production evidence handoff package archive CI summary не должен оставлять autogenerated `tmp/production-evidence-handoff-package-archive-ci-summary-stale-release-guard.json`, `.md` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-ci-summary-latest-release-guard.ps1` удаляет summary JSON/Markdown и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `ProductionReadinessGateTests` 104/104, production handoff CI summary latest release guard cleanup, backend full suite `657/657`, latest "Что нового" `2026-07-02-production-handoff-ci-summary-latest-release-guard-cleanup`, версия `0.362.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-114` Production handoff package archive latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: stale-release regression для production evidence handoff package archive не должен оставлять autogenerated `tmp/production-evidence-handoff-package-archive-stale-release-guard.zip`, package directory и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-archive-latest-release-guard.ps1` удаляет ZIP, package directory и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `ProductionReadinessGateTests` 105/105, production handoff package archive latest release guard cleanup, backend full suite `658/658`, latest "Что нового" `2026-07-02-production-handoff-package-archive-latest-release-guard-cleanup`, версия `0.363.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-115` Production evidence bundle latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: stale-release regression для production evidence bundle не должен оставлять autogenerated `tmp/production-evidence-bundle-stale-release-guard` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-bundle-latest-release-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку latest release guard.
  - Доказательство: `ProductionReadinessGateTests` 106/106, production evidence bundle latest release guard cleanup, backend full suite `659/659`, latest "Что нового" `2026-07-02-production-evidence-bundle-latest-release-guard-cleanup`, версия `0.364.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-116` Production evidence manifest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: unknown-release regression для production evidence manifest не должен оставлять autogenerated `tmp/production-evidence-manifest-unknown-release-id` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-manifest-release-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку неизвестного `releaseId`.
  - Доказательство: `ProductionReadinessGateTests` 107/107, production evidence manifest release guard cleanup, backend full suite `662/662`, latest "Что нового" `2026-07-02-production-evidence-manifest-release-guard-cleanup`, версия `0.367.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-117` Production evidence archive release guard default artifact cleanup. 2026-07-02.
  - Что сделать: unknown-release regression для production evidence archive не должен оставлять autogenerated `tmp/production-evidence-archive-unknown-release-id` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-archive-release-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку неизвестного `releaseId`.
  - Доказательство: `ProductionReadinessGateTests` 108/108, production evidence archive release guard cleanup, backend full suite `663/663`, latest "Что нового" `2026-07-02-production-evidence-archive-release-guard-cleanup`, версия `0.368.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-118` Production handoff receipt release guard default artifact cleanup. 2026-07-02.
  - Что сделать: unknown-release regression для production evidence handoff receipt не должен оставлять autogenerated `tmp/production-evidence-handoff-receipt-unknown-release-id` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-receipt-release-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку неизвестного `releaseId`.
  - Доказательство: `ProductionReadinessGateTests` 109/109, production handoff receipt release guard cleanup, backend full suite `664/664`, latest "Что нового" `2026-07-02-production-handoff-receipt-release-guard-cleanup`, версия `0.369.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-119` Production handoff checklist release guard default artifact cleanup. 2026-07-02.
  - Что сделать: unknown-release regression для production evidence handoff checklist не должен оставлять autogenerated `tmp/production-evidence-handoff-checklist-unknown-release-id` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-checklist-release-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку неизвестного `releaseId`.
  - Доказательство: `ProductionReadinessGateTests` 110/110, production handoff checklist release guard cleanup, backend full suite `665/665`, latest "Что нового" `2026-07-02-production-handoff-checklist-release-guard-cleanup`, версия `0.370.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-120` Production handoff package latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: stale-release regression для production evidence handoff package не должен оставлять autogenerated `tmp/production-evidence-handoff-package-stale-release-guard` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-latest-release-guard.ps1` удаляет package directory и пустой `tmp`, сохраняя fail-closed проверку stale `releaseId`.
  - Доказательство: `ProductionReadinessGateTests` 111/111, production handoff package latest release guard cleanup, backend full suite `666/666`, latest "Что нового" `2026-07-02-production-handoff-package-latest-release-guard-cleanup`, версия `0.371.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-121` Production handoff receipt verified files guard default artifact cleanup. 2026-07-02.
  - Что сделать: verified-files regression для production evidence handoff receipt не должен оставлять autogenerated `tmp/production-evidence-handoff-receipt-verified-files-guard` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-receipt-verified-files-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку tampered `verifiedFiles`.
  - Доказательство: `ProductionReadinessGateTests` 112/112, production handoff receipt verified files guard cleanup, backend full suite `667/667`, latest "Что нового" `2026-07-02-production-handoff-receipt-verified-files-guard-cleanup`, версия `0.372.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-122` Production handoff receipt Markdown verified files guard default artifact cleanup. 2026-07-02.
  - Что сделать: markdown verified-files regression для production evidence handoff receipt не должен оставлять autogenerated `tmp/production-evidence-handoff-receipt-markdown-verified-files-guard` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-receipt-markdown-verified-files-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку tampered Markdown `verifiedFiles`.
  - Доказательство: `ProductionReadinessGateTests` 113/113, production handoff receipt Markdown verified files guard cleanup, backend full suite `668/668`, latest "Что нового" `2026-07-02-production-handoff-receipt-markdown-verified-files-guard-cleanup`, версия `0.373.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-123` Production handoff checklist Markdown gates guard default artifact cleanup. 2026-07-02.
  - Что сделать: checklist markdown gates regression не должен оставлять autogenerated `tmp/production-evidence-handoff-checklist-markdown-gates-guard` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-checklist-markdown-gates-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку tampered Markdown gates.
  - Доказательство: `ProductionReadinessGateTests` 114/114, production handoff checklist Markdown gates guard cleanup, backend full suite `669/669`, latest "Что нового" `2026-07-02-production-handoff-checklist-markdown-gates-guard-cleanup`, версия `0.374.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

- [x] `P11-ACC-124` Production handoff package Markdown files guard default artifact cleanup. 2026-07-02.
  - Что сделать: package markdown files regression не должен оставлять autogenerated `tmp/production-evidence-handoff-package-markdown-files-guard` и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-production-evidence-handoff-package-markdown-files-guard.ps1` удаляет bundle directory и пустой `tmp`, сохраняя fail-closed проверку tampered package index Markdown files.
  - Доказательство: `ProductionReadinessGateTests` 115/115, production handoff package Markdown files guard cleanup, backend full suite `670/670`, latest "Что нового" `2026-07-02-production-handoff-package-markdown-files-guard-cleanup`, версия `0.375.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-125` Production evidence bundle generator release guard default artifact cleanup. 2026-07-02.
  - Что сделать: bundle generator release guard regression не должен оставлять пустой `tmp` после локального запуска с unknown release id.
  - Что сделано: `scripts/test-production-evidence-bundle-generator-release-guard.ps1` удаляет пустой `tmp`, сохраняя fail-closed проверку unknown manual release id без созданного output directory.
  - Доказательство: `ProductionReadinessGateTests` 116/116, production evidence bundle generator release guard cleanup, backend full suite `671/671`, latest "Что нового" `2026-07-02-production-evidence-bundle-generator-release-guard-cleanup`, версия `0.376.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-126` VPS production smoke generator release guard default artifact cleanup. 2026-07-02.
  - Что сделать: VPS production smoke generator release guard regression не должен оставлять пустой `tmp` после локального запуска с unknown release id.
  - Что сделано: `scripts/test-vps-production-smoke-report-generator-release-guard.ps1` удаляет пустой `tmp`, сохраняя fail-closed проверку unknown manual release id без созданного JSON report.
  - Доказательство: `VpsProductionSmokeTests` 10/10, VPS production smoke generator release guard cleanup, backend full suite `672/672`, latest "Что нового" `2026-07-02-vps-production-smoke-generator-release-guard-cleanup`, версия `0.377.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-127` VPS production smoke latest release guard default artifact cleanup. 2026-07-02.
  - Что сделать: VPS production smoke latest release guard regression не должен оставлять stale-release JSON и пустой `tmp` после локального запуска.
  - Что сделано: `scripts/test-vps-production-smoke-report-latest-release-guard.ps1` удаляет stale-release JSON и пустой `tmp`, сохраняя fail-closed проверку stale release id в `-RequireAllPassed` режиме.
  - Доказательство: `VpsProductionSmokeTests` 12/12, VPS production smoke latest release guard cleanup, backend full suite `673/673`, latest "Что нового" `2026-07-02-vps-production-smoke-latest-release-guard-cleanup`, версия `0.378.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.
- [x] `P11-ACC-128` Production readiness assertion CI step summary default artifact cleanup. 2026-07-02.
  - Что сделать: production readiness assertion CI step summary smoke не должен оставлять default output directory и пустой `tmp` после обычного локального запуска.
  - Что сделано: `scripts/test-production-readiness-assertion-ci-step-summary.ps1` удаляет default output directory и пустой `tmp` для non-JSON запуска, но сохраняет artifacts для явного `-OutputDirectory` и `-WriteJson` evidence/debug flows.
  - Доказательство: `ProductionReadinessGateTests` 117/117, production readiness CI step summary cleanup, backend full suite `680/680`, latest "Что нового" `2026-07-02-production-readiness-ci-step-summary-cleanup`, версия `0.385.0`. Реальный live VPS smoke report еще нужен, поэтому `P11-ACC-002` остается `[ ]`.

## Журнал проверок

Новые проверки добавлять сверху.

| Дата | Кто | Что проверено | Результат | Доказательство |
| --- | --- | --- | --- | --- |
| 2026-08-05 | Codex | Refresh rotation concurrency, logout race и admin deactivation retry | Зеленое локально | Backend `1043/1043`, targeted `43/43`, frontend `84/84`, responsive Playwright `16/16`, fresh SQLite smoke; real VPS/staging/live evidence остается открытым |
| 2026-08-05 | Codex | Password reset generation boundary и concurrent reissue | Зеленое локально | Backend `1039/1039`, targeted `31/31`, frontend `84/84`, responsive Playwright `16/16`, fresh SQLite smoke; real VPS/staging/live evidence остается открытым |
| 2026-08-05 | Codex | Registration email race boundary и referral entropy | Зеленое локально | Backend `1036/1036`, targeted `24/24`, frontend `84/84`, responsive Playwright `16/16`, fresh SQLite smoke; real VPS/staging/live evidence остается открытым |
| 2026-08-05 | Codex | Password reset sibling invalidation, optimistic concurrency и single-hydration кабинета | Зеленое локально | Backend `1034/1034`, targeted `21/21`, frontend `84/84`, responsive Playwright `16/16`, fresh SQLite smoke; real VPS/staging/live evidence остается открытым |
| 2026-08-05 | Codex | Refresh replay family isolation, session-version boundary и legacy chain rollout | Зеленое локально | Backend `1032/1032`, targeted `19/19`, frontend `84/84`, responsive Playwright `16/16`, fresh SQLite smoke; real VPS/staging/live evidence остается открытым |
| 2026-08-05 | Codex | Versioned JWT/refresh sessions, password reset и admin bootstrap invalidation | Зеленое локально | Backend `1030/1030`, targeted `32/32`, frontend `84/84`, responsive Playwright `16/16`, fresh SQLite smoke; real VPS/staging/live evidence остается открытым |
| 2026-08-05 | Codex | Active-user JWT/refresh/cabinet/Telegram session boundary | Зеленое локально | Backend `1027/1027`, targeted auth/admin/Telegram `48/48`, frontend `84/84`, responsive Playwright `16/16`, fresh SQLite smoke; real VPS/staging/live evidence остается открытым |
| 2026-08-05 | Codex | Cancelled parent access в user DTO/QR/cabinet UI и lifecycle gate | Зеленое локально | Backend `1021/1021`, targeted cabinet SQLite `13/13`, frontend `83/83`, responsive Playwright `16/16`, fresh SQLite smoke; production-like 3x-ui evidence остается открытым |
| 2026-08-05 | Codex | Cancelled parent access API/UI boundary, lifecycle gate и redaction | Зеленое локально | Backend `1018/1018`, targeted SQLite `51/51`, frontend `82/82`, responsive Playwright `16/16`, fresh SQLite smoke; production-like 3x-ui evidence остается открытым |
| 2026-08-05 | Codex | Cancelled subscription terminal backend/UI action guard | Зеленое локально | Backend `1012/1012`, targeted admin subscription SQLite `22/22`, frontend `80/80`, responsive Playwright `16/16`, fresh SQLite smoke; production-like 3x-ui evidence остается открытым |
| 2026-08-05 | Codex | Revoked VPN access terminal API/UI contract, redaction, QR и provider-command guards | Зеленое локально | Backend `1011/1011`, targeted SQLite `23/23`, frontend `77/77`, responsive Playwright `16/16`, fresh SQLite smoke; production-like 3x-ui evidence остается открытым |
| 2026-08-05 | Codex | VPN access enable/sync/reset cancellation и reset reconciliation | Зеленое локально | Backend `1008/1008`, targeted SQLite `8/8`, X3Ui/admin/subscription `117/117`, frontend `77/77`, responsive Playwright `16/16`; production-like 3x-ui evidence остается открытым |
| 2026-08-05 | Codex | Admin audit finance/support/Telegram domain scope и защита фильтров | Зеленое локально | Backend `1004/1004`, targeted audit SQLite role matrix `9/9`, frontend `77/77`, Finance/Support desktop/mobile и responsive Playwright `16/16`; real VPS/staging evidence остается открытым |
| 2026-08-05 | Codex | Admin dashboard finance/support/Telegram redaction для partial roles | Зеленое локально | Backend `999/999`, targeted `29/29`, SQLite role matrix `3/3`, frontend `77/77`, Support desktop/mobile и responsive Playwright `16/16`; real VPS/staging evidence остается открытым |
| 2026-08-05 | Codex | Admin partial-role capability matrix, запрещенные запросы/команды и finance/support redaction | Зеленое локально | Backend `996/996`, targeted policy/session/user overview `50/50` с SQLite, frontend `77/77`, Playwright desktop/mobile/all-screens `14/14`; real VPS/staging evidence остается открытым |
| 2026-08-05 | Codex | Manual payment recheck caller cancellation | Зеленое локально | Backend `984/984`, targeted `14/14`, cancellation `1/1`, fresh SQLite checkout/VPN, Playwright `12/12`; live payment остаётся открытым evidence |
| 2026-08-05 | Codex | Public payment providers и фактический checkout account selection | Зеленое локально | Backend `983/983`, targeted `26/26`, selection regression `4/4`, fresh SQLite checkout/VPN, Playwright `12/12`; live payment остаётся открытым evidence |
| 2026-08-05 | Codex | Payment provider default concurrency, migration cleanup и unique conflict | Зеленое локально | Backend `981/981`, payment/account `56/56`, default regression `7/7`, file-backed SQLite и migration SQL, Playwright `12/12`; live payment остаётся открытым evidence |
| 2026-08-05 | Codex | VPN node update/delete/health/provisioning/reservation gate, capacity и cancellation | Зеленое локально | Backend `974/974`, server management `12/12`, server/provisioning/capacity `86/86`, file-backed SQLite concurrency, Playwright `12/12`; real VPS остаётся открытым evidence |
| 2026-08-05 | Codex | X3Ui panel health persistence, CRUD gate и capacity invariants | Зеленое локально | Backend `970/970`, X3Ui `72/72`, targeted panel/SQLite `78/78`, health/capacity `5/5`, Playwright `12/12`; production-like 3x-ui остаётся открытым evidence |
| 2026-08-05 | Codex | X3Ui inbound update: panel-state gate, reverse update и manual reconciliation | Зеленое локально | Backend `965/965`, X3Ui `67/67`, targeted panel/SQLite `73/73`, inbound `4/4`, Playwright `12/12`; production-like 3x-ui остаётся открытым evidence |
| 2026-08-05 | Codex | Admin/provider enable, disable и reset traffic: rollback, uncertainty и reconciliation UI | Зеленое локально | Backend `961/961`, X3Ui `63/63`, targeted `7/7`, Playwright `12/12`; production-like 3x-ui остаётся открытым evidence |
| 2026-08-04 | Codex | X3Ui client migration capacity, concurrency и remote rollback | Зеленое локально | Backend `954/954`, X3Ui `56/56`, targeted `9/9`, file-backed SQLite last-slot race, Playwright `12/12`; production-like 3x-ui остаётся открытым evidence |
| 2026-08-04 | Codex | Terminal subscription cancel, X3Ui revoke/delete и node/panel/inbound rollback | Зеленое локально | Backend `948/948`, targeted `23/23`, real adapter file-backed SQLite fault injection, Playwright `12/12`; production-like 3x-ui остаётся открытым evidence |
| 2026-08-04 | Codex | 3x-ui create/update/delete/enable/disable, concurrent capacity и SQLite selection | Зеленое локально | Backend `939/939`, X3Ui `48/48`, file-backed SQLite concurrency, Playwright `12/12`; production-like 3x-ui остаётся открытым evidence |
| 2026-08-04 | Codex | VPN node capacity reserve/release и renewal assignment | Зеленое локально | Backend `942/942`, targeted VPN/payment/SQLite `100/100`, file-backed SQLite last-slot concurrency, Playwright `12/12`; production-like 3x-ui остаётся открытым evidence |
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
| `BUG-2026-08-05-050` | P0 | Provisioning ownership | Admin queue/deploy/retry записывали operator ID в `ProvisioningRun.RequestedByUserId`; worker использовал его как владельца support/subscription/VPN access и мог выдать клиентский доступ администратору. | Исправлено | Customer owner наследуется через все queue boundaries, actor хранится в audit; SQLite и worker E2E проверяют subscription/access ownership и отсутствие выдачи actor. |
| `BUG-2026-08-05-049` | P0 | Support lifecycle concurrency | Reply/status/note использовали last-write-wins: устаревшее закрытие могло скрыть новое входящее сообщение, pending Telegram reply создавал параллельный диалог, а `AssignedToUserId` принимал произвольный GUID. | Исправлено | Optimistic revision и controlled `409`, active-thread reopen, support-role assignment validation и fail-closed internal filter покрыты SQLite/API/UI regressions. |
| `BUG-2026-08-05-048` | P0 | Renewal/order concurrency | `OrderService` принимал произвольный renewal subscription ID, а read-before-insert позволял двум API-инстансам сохранить дубли pending-заказа; legacy route возвращал `200` для любого GUID. | Исправлено | Service-level ownership/status/tariff validation, deterministic intent key, partial unique index, winning-order recovery, stale expiry и `410 Gone` legacy response покрыты SQLite и migration tests. |
| `BUG-2026-08-05-047` | P0 | Telegram account linking | Повторная выдача deep link оставляла предыдущие ссылки действующими, а неуникальный `TelegramAccounts.UserId` позволял конкурентно привязать к одному пользователю два Telegram ID. | Исправлено | Per-user generation/revision оставляет действительной только последнюю ссылку, unlink отзывает outstanding token, unique filtered index и token revision закрывают межконтекстные гонки; legacy данные безопасно мигрируются. |
| `BUG-2026-08-05-046` | P0 | Refresh rotation concurrency | Два API-контекста могли одновременно прочитать один active refresh token, оба вернуть `200` и сохранить две действующие дочерние сессии; logout/admin revoke не имели controlled conflict handling. | Исправлено | Optimistic revision откатывает stale child, replay закрывает winning family, logout и admin deactivation повторяют актуальное состояние; три file-backed гонки покрыты. |
| `BUG-2026-08-05-045` | P0 | Password reset reissue | Повторный запрос выдавал новый код, но предыдущий оставался действующим; stale reset мог конкурировать с reissue, а admin bootstrap password reset не закрывал outstanding codes. | Исправлено | Per-user generation/revision делает newest request единственным действительным, stale commit откатывается, bootstrap invalidates codes; sequential и две file-backed гонки покрыты. |
| `BUG-2026-08-05-044` | P1 | Registration concurrency | Два запроса одного email могли оба пройти `AnyAsync`; проигравший падал на `IX_Users_Email` как HTTP 500, а короткий referral code содержал только 24 случайных бита. | Исправлено | Точный email conflict возвращает `email_exists` без partial rows, unrelated DB failure не маскируется, referral entropy увеличена до 64 бит; file-backed SQLite race regression. |
| `BUG-2026-08-05-043` | P1 | Cabinet auth hydration | После login/register/refresh кабинет одновременно вызывал `loadAll` из handler и token effect, удваивал восемь защищенных запросов и мог заменить QR-кнопку между locator и click. | Исправлено | Effect оставлен только для восстановленной сессии, E2E проверяет одну загрузку `/api/me` на relogin; desktop/mobile/full responsive matrix `16/16`. |
| `BUG-2026-08-05-042` | P0 | Password reset security | После успешного reset другие ранее выданные коды оставались валидными до expiry и могли снова заменить пароль; параллельные sibling tokens не имели DB concurrency boundary. | Исправлено | Sibling invalidation, explicit reason/timestamp и optimistic Revision делают reset atomic; backend `1034/1034`, targeted `21/21`, SQLite cross-context regression. |
| `BUG-2026-08-05-041` | P0 | Refresh session security | Replay любого revoked refresh-токена отзывал все активные сессии пользователя, поэтому старый logout/password-reset token позволял бесконечно завершать новые входы; pre-FamilyId rotation chain требовала rollout-совместимости. | Исправлено | FamilyId изолирует login/rotation chains, stale generation не затрагивает current sessions, legacy descendants связываются через ReplacedByTokenHash; backend `1032/1032`, targeted `19/19`. |
| `BUG-2026-08-05-040` | P0 | Auth sessions | Password reset отзывал refresh rows, но старый JWT жил до expiry; block/unblock или bootstrap мог вернуть старым JWT полномочия, а гонка refresh rotation могла создать новую сессию после массового отзыва. | Исправлено | Версии пользователя и refresh-сессии проверяются на каждом запросе/rotation, mutating identity operations повышают версию, browser reset очищает токены и приватное состояние; backend `1030/1030`, targeted `32/32`, desktop/mobile regressions. |
| `BUG-2026-08-05-039` | P0 | Auth / cabinet / Telegram | Admin deactivation не отзывала refresh-сессии; уже выданный JWT оставался действителен до expiry, кабинет сохранял загруженные secrets после 401/403, linked Telegram user продолжал читать ключи и создавать операции. | Исправлено | Per-request active-user JWT validation, атомарный refresh revoke, cabinet sensitive-state cleanup и Telegram route/pre-checkout guard; backend `1027/1027`, targeted `48/48`, frontend `84/84`, desktop/mobile regressions. |
| `BUG-2026-08-05-038` | P1 | Cabinet access boundary | User subscription/access DTO и оба QR route проверяли только `Revoked`, поэтому stale `Active` credential с `Cancelled` parent раскрывал URI/provider/QR/config и QR мог обойти concurrent cancel. | Исправлено | Projections редактируют secrets по parent status, QR routes ждут lifecycle gate, а cabinet helper скрывает terminal secrets/actions; SQLite race и desktop/mobile regressions подтверждают контракт. |
| `BUG-2026-08-05-037` | P1 | Cancelled subscription access | Stale Active/Disabled credential отменённой подписки принимал direct provider-команды, QR и migration; direct sync не ждал cancel gate, а admin list/user overview раскрывали URI/provider/QR/config. | Исправлено | Parent guard блокирует lifecycle до provider/history/audit, endpoints сериализованы, projections/UI редактируют secrets; SQLite race и desktop/mobile regressions подтверждают контракт. |
| `BUG-2026-08-05-036` | P1 | Admin subscription lifecycle | Отменённая подписка с legacy active current access принимала sync до VPN-провайдера, а admin-panel показывал продление, sync, block и numeric input у terminal record. | Исправлено | Backend отклоняет sync до provider call/history/audit; UI action matrix и handler работают fail-closed, SQLite и desktop/mobile regressions подтверждают отсутствие команд. |
| `BUG-2026-08-05-035` | P1 | Revoked VPN access | Терминальный credential сохранял пользовательские URI/QR/config/provider ID, QR routes генерировали код, reset traffic вызывал провайдера, а кабинет и админка показывали секреты и команды, включая stale QR. | Исправлено | API редактирует пользовательский материал и отклоняет QR, lifecycle блокирует reset до provider call, UI скрывает terminal secrets/actions; SQLite и desktop/mobile regressions подтверждают fail-closed контракт. |
| `BUG-2026-08-05-034` | P1 | VPN access lifecycle | Enable/sync/reset traffic поглощали caller cancellation общим catch и сохраняли diagnostics отмененным token; reset failure оставлял `AccessCredential` активным вопреки UI-обещанию `SyncRequired`. | Исправлено | Отдельные cancellation paths сохраняют history/audit и повторно выбрасывают отмену; enable/reset uncertainty durable переводит доступ в `SyncRequired`, SQLite и X3Ui/admin regression подтверждают контракт. |
| `BUG-2026-08-05-033` | P1 | Admin audit RBAC | Любая роль с `AdminRead` получала все finance/support/Telegram audit actions и JSON payload; Action/EntityType/Search позволяли явно запросить запись чужого домена. | Исправлено | Capability scope применяется до пользовательских фильтров, UI показывает только доступные категории; SQLite role matrix и Finance/Support desktop/mobile regressions подтверждают отсутствие междоменного раскрытия. |
| `BUG-2026-08-05-032` | P1 | Admin dashboard RBAC | Support/Operator без FinanceRead получали агрегаты заказов/платежей и payment readiness, Finance без SupportRead видел размер support queue, а роли без BotManage получали Telegram readiness и переходы. | Исправлено | Backend пропускает недоступные доменные запросы и фильтрует summary/readiness, UI скрывает соответствующие tiles/cards/actions; SQLite Support/Finance/Admin и desktop/mobile responsive regressions подтверждают redaction. |
| `BUG-2026-08-05-031` | P1 | Admin partial-role RBAC | Finance, Support и ReadOnly попадали в admin-shell, который загружал все доменные API, показывал запрещенные команды и через user overview мог раскрыть данные соседнего домена. | Исправлено | Backend session capability matrix управляет разделами/командами, handlers fail-closed до network call, а user overview редактирует finance/support данные; policy, SQLite, unit и desktop/mobile regressions подтверждают контракт. |
| `BUG-2026-08-05-030` | P1 | Admin RBAC admission | Общий login обычного пользователя сохранял токены и открывал admin-shell; последующие `403` отображались только как многочисленные ошибки загрузки разделов. | Исправлено | `AdminRead` подтверждается до local storage/shell, denied refresh отзывается, HTTP status типизирован; desktop/mobile и StrictMode responsive regressions подтверждают fail-closed поведение. |
| `BUG-2026-08-05-029` | P1 | Admin auth session | Admin-panel выбрасывал refresh token после login, не мог ротировать access и завершал сессию только локально, оставляя backend refresh session активной. | Исправлено | Access/refresh lifecycle синхронизирован с API; desktop/mobile E2E подтверждает rotation, bearer/refresh logout и локальную очистку при success/controlled `503`. |
| `BUG-2026-08-05-028` | P1 | Cabinet auth session | При ошибке `/api/auth/logout` кабинет оставлял access/refresh токены и загруженные пользовательские/VPN-данные в памяти и session storage, фактически не выполняя локальный выход. | Исправлено | Cleanup выполняется в `finally`, server revoke failure явно сообщается; desktop/mobile E2E подтверждает success/failure logout и отсутствие локальных токенов. |
| `BUG-2026-08-05-027` | P1 | Public auth session | Public login/register выбрасывал refresh token, не использовал rotation и локальный logout не вызывал backend, оставляя refresh session активной до истечения. | Исправлено | Access/refresh lifecycle синхронизирован с API; logout отзывает backend-сессию и гарантированно очищает browser storage, desktop/mobile E2E покрывает success, `401` rotation и controlled `503`. |
| `BUG-2026-08-05-026` | P1 | Cabinet payment retry | Кабинет предлагал «Повторить оплату» для `Expired` и stale `PendingPayment`, хотя backend всегда переводил/оставлял заказ в `Expired` и не вызывал провайдера. | Исправлено | Time-aware UI и handler guard скрывают недоступный retry, показывают эффективный `Expired` и ведут к новому заказу; SQLite и desktop/mobile regressions подтверждают контракт. |
| `BUG-2026-08-05-025` | P1 | Cabinet VPN access | Карточки доступа показывали активную кнопку QR при пустом `accessUri`, хотя backend всегда возвращал `400` до завершения выдачи. | Исправлено | Все QR-команды и handler используют единое правило готовности URI; SQLite и desktop/mobile regressions подтверждают disabled-состояние и рабочий QR после выдачи. |
| `BUG-2026-08-05-024` | P1 | Cabinet renewal | Карточки `Blocked/Cancelled` подписок показывали кнопку «Продлить», хотя backend всегда возвращал `400`, создавая заведомо нерабочую пользовательскую операцию. | Исправлено | UI и handler используют единое правило статусов; запрещённые карточки показывают поддержку или новый тариф, SQLite и desktop/mobile regressions подтверждают отсутствие ложной команды. |
| `BUG-2026-08-05-023` | P1 | Payment provider accounts | Локальная проверка конфигурации записывала `Healthy/Unhealthy`, `LastHealthCheckAt` и показывала «подключение готово», хотя внешний кабинет провайдера не запрашивался. | Исправлено | Configuration readiness отделена от live health в API и админке; синтетические health-маркеры очищаются, реальный provider health остается внешней проверкой. |
| `BUG-2026-08-05-022` | P1 | Payment recheck | Caller cancellation внутри provider `GetStatusAsync` поглощалась общим catch и возвращалась как обычный failure, скрывая отмену HTTP/worker операции. | Исправлено | Отмена caller token явно пробрасывается; SQLite regression подтверждает неизменность payment status и отсутствие audit, live provider recheck остается внешней проверкой. |
| `BUG-2026-08-05-021` | P1 | Payment checkout | Public API мог показать настроенный fallback account, но checkout выбирал неготовый default; без default UI и orchestrator сортировали аккаунты по-разному. | Исправлено | Общий web-ready selector используется public API и orchestrator; regression покрывает неготовый default и конфликт `Name`/`CreatedAt`, live provider smoke остаётся внешней проверкой. |
| `BUG-2026-08-05-020` | P1 | Payment provider accounts | Параллельные create/update могли сохранить несколько default-аккаунтов провайдера; БД не защищала invariant, default switch конфликтовал бы с unique index, а duplicate name давал HTTP 500. | Исправлено | Provider/account gates, partial unique index, cleanup migration, transactional switch и controlled unique conflict покрыты SQLite/migration/payment tests; live provider smoke остаётся внешней проверкой. |
| `BUG-2026-08-05-019` | P1 | VPN node state | Server CRUD/health, provisioning и slot reservation обходили единый state boundary, update позволял capacity ниже usage, а caller cancellation могла обрабатываться как provider failure. | Исправлено | Единый node-state gate для мутаций и capacity reserve/release, validation до side effects и явная cancellation propagation покрыты file-backed SQLite/server/provisioning тестами; real VPS smoke остаётся внешней проверкой. |
| `BUG-2026-08-05-018` | P1 | VPN/3x-ui panel health | Local save failure после успешного health-check мог записать одновременно success и failure history; panel CRUD/health использовали разные gates, а capacity разрешалось уменьшать ниже usage. | Исправлено | Stable-ID health recovery, общий panel gate, stale worker guard и capacity validation покрыты fault-injection/SQLite; production-like 3x-ui smoke остаётся внешней проверкой. |
| `BUG-2026-08-05-017` | P1 | VPN/3x-ui inbound | Edit inbound выполнял remote update до local commit без сериализации и reverse update; concurrent edit/sync мог оставить БД и 3x-ui в разном порядке. | Исправлено | Единый panel-state gate, stale sync guard, reverse update и manual-reconciliation audit покрыты fault-injection и file-backed SQLite; production-like 3x-ui smoke остаётся внешней проверкой. |
| `BUG-2026-08-05-016` | P1 | VPN/3x-ui client state | Ручные enable/disable не компенсировали successful/ambiguous remote update при local failure, а reset traffic мог выполниться в 3x-ui без durable uncertainty marker. | Исправлено | Reverse update, `SyncRequired`, redacted audit, durable reset uncertainty и desktop/mobile dialog покрыты fault-injection/X3Ui/Playwright; production-like 3x-ui smoke остаётся внешней проверкой. |
| `BUG-2026-08-04-015` | P1 | VPN/3x-ui migration | Manual client migration не проверяла target capacity, обычным `+= 1` допускала oversubscription и не восстанавливала source/target после local commit failure или ambiguous remote side effect. | Исправлено | Durable target reservation, source restore/target cleanup, uncertainty marker и desktop/mobile flow покрыты SQLite/X3Ui/Playwright; production-like 3x-ui smoke остаётся внешней проверкой. |
| `BUG-2026-08-04-014` | P1 | Subscription/VPN lifecycle | Terminal `Cancelled` только отключал доступ и оставлял provider-клиента, ссылки подписки и node/panel/inbound capacity занятыми навсегда. | Исправлено | Transactional revoke/delete, симметричный capacity release, rollback/reconciliation и desktop/mobile destructive-flow покрыты SQLite/X3Ui/Playwright; production-like 3x-ui smoke остаётся внешней проверкой. |
| `BUG-2026-08-04-012` | P1 | VPN/3x-ui | Параллельная выдача могла переполнить последний inbound slot, продление переносило клиента с заполненной назначенной панели без удаления старой копии, delete не освобождал capacity, SQLite selection падал на сортировке. | Исправлено | Subscription gate, optimistic concurrency, assigned-inbound renewal, симметричные capacity counters, remote compensation и SQLite regression добавлены; production-like 3x-ui smoke остаётся внешней проверкой. |
| `BUG-2026-08-04-013` | P1 | VPN allocation | Два разных заказа могли одновременно пройти проверку последнего slot `VpnNode`; `Math.Min` скрывал oversubscription, а renewal на maintenance node менял локальное назначение без provider migration. | Исправлено | Conditional reserve/release до remote call, compensation при failure/cancellation и сохранение assigned node проверены на file-backed SQLite; production-like 3x-ui smoke остаётся внешней проверкой. |
| `BUG-2026-08-04-011` | P1 | Provisioning | Несколько worker могли выполнить один deploy, зависший run не восстанавливался, active run можно было локально отменить, а runner не имел timeout и мог заблокироваться на pipe. | Исправлено | Atomic claim/lease recovery, unique active-node index, safe retry/cancel boundaries и runner timeout/process-tree cleanup покрыты SQLite и worker tests; real VPS smoke остаётся внешним evidence. |
| `BUG-2026-06-10-010` | P1 | Local SQLite startup | При `UseEnsureCreatedForLocalSqlite=true` и `ApplyMigrationsOnStartup=false` новая SQLite-БД не создавала таблицы до admin bootstrap, из-за чего API падал на `no such table: Users`. | Исправлено | `EnsureCreated` и локальный schema repair выполняются до bootstrap/seed всегда, когда включен local SQLite EnsureCreated. |
| `BUG-2026-06-10-009` | P1 | Payments/SQLite | `PaymentOrchestrator.InitPaymentAsync` сортировал pending-платежи по `DateTimeOffset` в SQL, из-за чего локальная SQLite-БД падала при создании платежа. | Исправлено | Выборка ограничивается order/provider/account/status, сортировка `CreatedAt` выполняется в памяти. |
| `BUG-2026-06-10-008` | P1 | Payments/SQLite | `PaymentProviderAccountService.GetEnabledAccountEntityAsync` сортировал аккаунты провайдера по `CreatedAt` в SQLite SQL, что ломало выбор активного платежного аккаунта. | Исправлено | Сначала выбираются включенные кандидаты провайдера, затем приоритет default/created применяется в памяти. |
| BUG-001 | P0 | VPS/Admin | Не подтвержден рабочий вход в админку на VPS | partial | CLI-механизм восстановления добавлен; дальше выполнить reset на VPS и пройти smoke |
| BUG-002 | P0 | VPN | Не подтверждена live-выдача через реальный 3x-ui | open | Подключить panel/inbound/node и провести production smoke |
| BUG-003 | P0 | Payments | Не все payment providers подтверждены live/sandbox smoke | open | Пройти матрицу провайдеров |
| BUG-004 | P1 | Frontend | Нет полного browser E2E по public/cabinet/admin | Исправлено | Закрыто через `P9-TST-008`, `AllScreensBrowserSmokeTests`, `npm run e2e:all-screens --prefix frontend` и `npm run e2e:console --prefix frontend`. |
| BUG-005 | P1 | Docs | Часть roadmap/docs устарела, возможен mojibake в старых `.md` | Исправлено | Закрыто через `P10-DOC-005`, `STATE-014`, `DocumentationEncodingTests`, `RoadmapCurrentStateTests` и `BugRegisterConsistencyTests`. |
| BUG-006 | P1 | Provisioning | Live Ansible provisioning не production-ready из-за secret materialization | Исправлено | Безопасная временная материализация protected `ssh_key` реализована через `ProvisioningSecretMaterializer`, `AnsibleProvisioningExecutor` передает runner только временный path и удаляет файл в `finally`; live VPS/provisioning smoke остается отдельным P0/P11-блокером. |
