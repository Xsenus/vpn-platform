# Release decision

Документ закрывает roadmap-пункт `P11-ACC-007` и фиксирует текущее решение по готовности проекта.

## Решение

Статус на 2026-08-05: **staging-ready baseline, не production-ready**.

Проект можно использовать для локальной проверки, демонстрации продукта, подготовки staging и дальнейшего live smoke. Проект нельзя считать production-ready, пока не закрыт VPS production smoke и не проверены реальные внешние интеграции.

## Почему не production-ready

Production-ready решение заблокировано следующими пунктами:

- `P11-ACC-002 VPS production smoke` остается открытым: нет подтвержденного live deploy -> health -> admin login -> public order -> payment -> subscription -> VPN access на реальном VPS.
- Нужно ротировать любые секреты, которые могли быть раскрыты вне secret manager: root-пароли VPS, SSH keys, Telegram tokens, payment keys, webhook secrets, JWT/DataProtection keys.
- Нужен реальный домен и HTTPS, а не только локальные HTTP URLs.
- Нужна проверка PostgreSQL backup/restore на staging.
- Нужны реальные sandbox-кабинеты платежных провайдеров и provider-specific smoke.
- Нужна реальная 3x-ui/x-ui панель, inbound и проверка выдачи VPN-доступа на production-like сервере.
- Нужна отдельная проверка Telegram bot webhook/invoice flow, особенно для Telegram Stars.

## Что уже подтверждено

- Backend full suite: `1357/1357`.
- API Release build: OK.
- Frontend unit tests: `151/151`.
- Frontend typecheck/build: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: `227/227`; responsive all-screens проверяет content/modal/control geometry на 25 viewport-конфигурациях `305x568..2560x1440`, включая точные пары `N/N+1` для всех CSS-breakpoints; production inventory маршрутов и admin sections, local WebP decode и representative screenshots проверены.
- Actual PowerShell secret scan: OK.
- Frontend dependency audit: `0 vulnerabilities`; React 19.2.8 и React Router 8.3.0 проверены на Node.js 22.22.0.
- UTF-8/encoding guard: OK.
- Release decision entry: `2026-06-14-release-decision`, версия `0.104.0`.
- Operation boundary regression подтверждает 400 для некорректных enum/JSON без частичной записи; восемь payment webhook routes и fail-closed VPN provisioning покрыты контроллерными/SQLite-тестами.
- Subscription migration, archived-node fail-closed actions and compact mobile admin navigation are covered by SQLite/frontend/Playwright regression.
- Subscription commands apply status/date changes only after successful VPN lifecycle; historical node operations and linked scenario keys are protected by SQLite/browser regression.
- Admin writes and 3x-ui panel/inbound/client commands create redacted actor-aware audit records; incomplete panel configuration closes sync-runs as failed.
- Refund flow reserves an operation before provider call, blocks unresolved retries and preserves ambiguous provider outcomes for manual reconciliation without changing confirmed payment totals.
- Payment initialization uses an order gate and durable reservation, rejects paid intermediate states and recovers successful provider outcomes after transient local commit failure.
- Subscription activation compensates remote create after local credential save failure, persists `SyncRequired` when cleanup is uncertain and propagates cancellation after durable retry-state.
- Telegram update processing reserves `update_id` before side effects, retries failed/stale leases and preserves long-polling offset for retryable outcomes.
- Outbox events use unique event identity, atomic conditional claim, stale lease recovery, redacted retry/dead-letter and fail-closed payload validation; local email queue materialization is covered by SQLite tests.
- Email deliveries use a separate SMTP worker with conditional claim, stale lease/backoff and terminal failure; reset codes remain protected at rest and admin monitoring is masked/redacted.
- Provisioning runs use atomic claim and bounded lease; stale execution is quarantined without automatic external replay, runner timeout kills the process tree, and unsafe active retry/cancel is blocked.
- Subscription expiration disables remote access before `Expired`, persists lease/backoff retry state on provider failure and isolates lifecycle/panel worker failures per item.
- Panel sync uses a cross-instance unique claim, stale lease/snapshot recovery and redacted persisted diagnostics; legacy raw panel errors are cleared by the upgrade path.
- Admin session returns a backend-owned capability matrix; partial roles only load permitted sections and user overview redacts finance/support domains without their read policies.
- Dashboard summary avoids finance/support queries without matching read policies, filters payment/Telegram readiness checks and keeps partial-role UI free of hidden-domain metrics and actions.
- Audit log applies finance/support/Telegram capability scope before Action/EntityType/Search and keeps cross-domain actions, entity types and JSON payload out of partial-role responses and UI categories.
- VPN access enable/sync/reset preserves caller cancellation semantics and durable history/audit; enable/reset uncertainty is represented by `SyncRequired` instead of a false confirmed state; cabinet/admin invalidate cached QR before refresh or local blocking and replace technical/English API diagnostics and native network failures with a Russian fallback.
- Terminal subscription cancellation атомарно отзывает доступ, удаляет provider-клиента и освобождает capacity; rollback и provider uncertainty проверены fault-injection тестами.
- X3Ui client migration резервирует target capacity до remote add и компенсирует remote/local failure; last-slot concurrency проверена на независимых SQLite-контекстах.
- Versioned access/refresh sessions закрывают старые полномочия после password reset, деактивации и изменяющего роли/password admin bootstrap; JWT старого формата требует refresh/relogin.
- Refresh reuse detection ограничен одной token family и поддерживает legacy NULL-family chains без отзыва независимых сессий.
- Password reset invalidates outstanding sibling tokens in one transaction; optimistic `Revision` rejects stale concurrent commit across API instances.
- Cabinet login/register/refresh and restored-session reload use one protected-data hydration cycle without duplicate API calls or DOM replacement race.
- Registration email race maps the exact unique conflict to `email_exists` without partial auth rows and preserves unrelated storage failures.
- Password reset reissue advances a per-user generation; concurrent issue/reset conflicts fail closed and explicit bootstrap password reset invalidates outstanding codes.
- Refresh rotation uses optimistic revision so a source token cannot create two active children; reuse/logout revoke the current family and admin deactivation retries session conflicts.
- Support conversation revision rejects stale reply/status/note, reopens pending inbound threads and validates assigned support agents without closing external evidence gates.
- Checkout claim reserves the session, creates the order and publishes the final link in one transaction; same-user retries resolve the winner and another user cannot persist a second order.
- Promo validation is fail-closed across checkout and order creation; relational redemption limits, paid free-days snapshot and activation/renewal duration are covered by deterministic SQLite and browser regression.
- Общий inbound-каталог и admin desktop/mobile regression подтверждают межпанельный перенос клиента, автоматическое открытие панели назначения и последующие panel health/sync операции.
- После межпанельного переноса UI атомарно отражает `UsedCapacity` source/target панелей; desktop/mobile regression подтверждает значения до и после следующего API-refresh.
- Кабинет сохраняет renewal order при недоступности payment init и повторяет оплату по тому же ID без дубликата; desktop/mobile regression подтверждает запросы и состояние UI.
- Public authenticated checkout выполняет единственный claim/payment-init, сохраняет partial order для retry и отбрасывает late response после logout; desktop/mobile regression подтверждает request counts и UI.
- Persisted public checkout проходит bounded shape/token/provider validation; malformed browser state удаляется без claim/payment-init и stale UI.
- Public session hydration single-flight исключает concurrent refresh-token reuse, сохраняет transient state для retry и инвалидирует late response после logout.
- Cabinet support request generation исключает out-of-order thread и late logout completion; private support/auth/reset drafts очищаются до следующего входа.
- Admin detail request generation исключает out-of-order user/support state и post-logout completion; status action не подменяет выбранный support thread.
- Admin mutation session ownership исключает duplicate submit, post-logout UI/reload и потерю более нового form draft при delayed save или background reload.
- Public/cabinet mutation ownership исключает duplicate auth/refresh/action requests, stale completion после logout/unmount и потерю более нового support/reset draft; reset-code request и password confirmation используют независимые формы с корректным Enter submit.
- Axe WCAG 2.0/2.1/2.2 A/AA и best-practice gate без allow-list проходит 6 public route-состояний, cabinet auth/dashboard и admin auth/17 sections на desktop и 320 px.
- Admin production bundle: `5` chunks, largest `219849`, total raw `517701`, gzip `138757`; fail-closed budget пройден.
- Unknown public route показывает доступное `404` recovery и проходит desktop/mobile, Axe, console и 18 responsive viewport-конфигураций без blank screen/overflow.
- Public route title/meta/focus lifecycle проходит direct load, SPA navigation и browser Back на desktop/mobile; каждый route имеет точную metadata.
- Admin hydration/login и 17 sections имеют точную metadata; deep-link, section switch и logout проходят desktop/mobile.
- Admin tabs, Back/Forward, role fallback и order-links синхронизируют history, tabpanel, metadata и focus на desktop/mobile.
- Admin unknown hash канонизируется в Dashboard; direct/runtime recovery, focus и Back проходят desktop/mobile.
- Confirmed admin actions сохраняют async lifecycle: busy dialog удерживается до ответа API, controls блокируются, duplicate destructive submit исключён desktop/mobile regression.
- Cabinet configured public URL проходит fail-closed allow-list, а payment retry передаёт провайдеру только origin без query/fragment.
- Admin payment provider, Telegram и 3x-ui URL отклоняют embedded credentials до submit и persistence.
- VPN server/inbound handlers повторно валидируют programmatic submit; server/panel/inbound semantics и server panel URL защищены в UI/API.
- Hidden admin forms не обходят capability checks: releases, FAQ, content, scenarios, support и Telegram handlers проверяют write-право до API.
- Admin action dispatcher всегда проверяет target-section capability; writable active tab не разрешает hidden mutation другого раздела.
- Latest "Что нового": `2026-08-12-cabinet-subscription-boundary`, версия `0.669.0`; cabinet subscription read-model ограничен последними 100 записями и не раскрывает internal block/provider fields или пути конфигурации. Real VPS/SSH/Ansible, provider/Telegram/SMTP delivery и полный staging/production-like 3x-ui evidence все еще требуются.
- Roadmap progress: `682/702` closed, readiness `97.2%`, `20` remaining, `19` open, `1` in progress and `0` blocked.

## Команды проверки

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDecisionTests|ReleaseDocumentationGuardTests|ReadmeDocumentationTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
powershell -ExecutionPolicy Bypass -File scripts\assert-production-readiness.ps1 -ReportPath docs\staging-smoke-report.template.json
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

## Следующий шаг

Следующий технический шаг перед production: закрыть `P11-ACC-002` на реальном VPS или staging-домене.

Перед изменением решения на production-ready дополнительно должен пройти `scripts/assert-production-readiness.ps1` с реальным smoke-отчетом, где все обязательные checks имеют статус `passed`, а roadmap и этот документ больше не содержат открытых production-блокеров.

Минимальное доказательство для повышения статуса до production-ready:

- successful GitHub Actions deploy или ручной deploy на VPS;
- `/health/live` и `/health/ready` отвечают успешно;
- админка доступна, admin login работает;
- публичный сайт показывает тарифы и включенные способы оплаты;
- тестовый заказ проходит payment sandbox webhook;
- подписка активируется;
- пользователь получает рабочий VPN access URI и QR;
- post-deploy smoke сохранен без cookies, tokens, `.env` и приватных headers.

До этого момента корректная формулировка статуса: **staging-ready baseline**.
