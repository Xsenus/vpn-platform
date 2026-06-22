# Release decision

Документ закрывает roadmap-пункт `P11-ACC-007` и фиксирует текущее решение по готовности проекта.

## Решение

Статус на 2026-06-14: **staging-ready baseline, не production-ready**.

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

- Backend full suite: `590/590`.
- API Release build: OK.
- Frontend unit tests: `66/66`.
- Frontend typecheck/build: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: `9/9`.
- Actual PowerShell secret scan: OK.
- Frontend audit: OK, `0 vulnerabilities`.
- UTF-8/encoding guard: OK.
- Release decision entry: `2026-06-14-release-decision`, версия `0.104.0`.
- Latest "Что нового": `2026-06-22-admin-vps-bootstrap-smoke-environment-link`, версия `0.212.0`; `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` сверяет `apiBaseUrl`, `adminWebUrl`, `environmentName`, `operator`, `adminEmail` и `releaseId` итогового bootstrap smoke report с preflight и browser smoke reports, а `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает regression-сценарии `mismatched-bootstrap-environment` и `mismatched-bootstrap-admin-email`; `frontend/e2e/admin-vps-smoke.spec.ts`, `docs/admin-vps-smoke-report.template.json` и `scripts/new-admin-vps-smoke-report.ps1` пишут `smokeReportPath` в browser smoke report, `scripts/validate-admin-vps-smoke-report.ps1` требует это поле и сверяет его с фактическим smoke JSON; `scripts/admin-vps-smoke-preflight.ps1` пишет `preflightReportPath`, а `scripts/validate-admin-vps-smoke-evidence.ps1` сверяет preflight/smoke evidence; `scripts/admin-vps-bootstrap-smoke.ps1` пишет `bootstrapSmokeReportPath` и `readinessReportPath` в итоговый bootstrap smoke report, validators сверяют их с фактическими bootstrap/readiness JSON; production readiness gate и live VPS/staging evidence все еще требуются.

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
