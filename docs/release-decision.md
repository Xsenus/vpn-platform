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

- Backend full suite: `579/579`.
- API Release build: OK.
- Frontend unit tests: `66/66`.
- Frontend typecheck/build: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: `9/9`.
- Actual PowerShell secret scan: OK.
- Frontend audit: OK, `0 vulnerabilities`.
- UTF-8/encoding guard: OK.
- Release decision entry: `2026-06-14-release-decision`, версия `0.104.0`.
- Latest "Что нового": `2026-06-19-admin-vps-smoke-preflight-validator-regression`, версия `0.188.0`; `test-admin-vps-smoke-preflight-validator.ps1` проверяет fail-closed tamper-сценарии preflight validator, `scripts/validate-admin-vps-smoke-preflight-report.ps1` fail-closed проверяет sanitized preflight evidence, `scripts/admin-vps-smoke-preflight.ps1` проверяет live-параметры admin VPS smoke без вывода пароля, admin VPS smoke acceptance gate требует successful section HTTP statuses и real evidence без placeholders, `scripts/local-admin-vps-browser-smoke.ps1` проверяет admin browser smoke на временной SQLite-БД и убирает дерево процессов, `scripts/admin-vps-browser-smoke.ps1` запускает явный browser smoke админки VPS без trace/video/screenshots и валидирует sanitized report, `scripts/admin-bootstrap.ps1` дает one-shot reset администратора для Local SQLite и Production/Postgres без вывода пароля; production readiness gate агрегирует полный пакет evidence reports, staging smoke report теперь запрещает `TODO` evidence в acceptance mode, payment provider smoke report требует все boolean gates при `-RequireAllPassed`, VPS production smoke получил проверяемый report contract `vps-production-smoke-report.template.json` с generator/validator и fail-closed `-RequireAllPassed`, `test-production-ci-workflow-artifacts-guards.ps1` одной командой проверяет aggregate CI step guard, его fail-closed validator, readiness assertion и production evidence workflow artifacts guards вместе с fail-closed validators, `test-production-ci-workflow-artifacts-guards-validator.ps1` проверяет fail-closed поведение aggregate guard на tampered workflow, включая CI-step guard command/validator и readiness/evidence artifact contracts, `test-production-ci-workflow-artifacts-guards-ci-step.ps1` проверяет wiring aggregate steps в backend job, `test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1` проверяет fail-closed tamper-сценарии этого guard, GitHub Actions запускает aggregate CI step guard и его regression step сразу после checkout, а aggregate validator step `Guard production CI workflow artifacts contracts regression` до backend setup/build/test, `assert-production-readiness.ps1` может сохранять JSON/Markdown result artifacts через `-OutputPath` даже при ожидаемом `blocked`, а `validate-production-readiness-assertion-result.ps1` проверяет эти artifacts отдельно, `test-production-readiness-assertion-result-validator.ps1` проверяет fail-closed поведение validator на испорченных assertion artifacts, `test-production-readiness-assertion-ci-regression.ps1` запускает assertion/result-validator/regression цепочку в CI-friendly режиме, `validate-production-readiness-assertion-ci-regression-result.ps1` проверяет скачанный CI result artifact отдельно, `test-production-readiness-assertion-ci-regression-result-validator.ps1` проверяет fail-closed behavior этого validator, `validate-production-readiness-assertion-ci-summary.ps1` и `test-production-readiness-assertion-ci-summary-validator.ps1` проверяют GitHub Step Summary readiness assertion job, включая `bad-ci-artifacts-validator-regression`, `test-production-readiness-assertion-ci-step-summary.ps1` доказывает локальную запись real `GITHUB_STEP_SUMMARY`, `validate-production-readiness-assertion-ci-artifacts.ps1` проверяет весь readiness assertion CI artifact directory, `test-production-readiness-assertion-ci-artifacts-validator.ps1` проверяет fail-closed поведение artifact-directory validator, `test-production-readiness-assertion-ci-workflow-artifacts.ps1` закрепляет published artifacts в `.github/workflows/ci.yml`, `test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1` проверяет fail-closed поведение readiness assertion workflow guard на tamper-сценариях, GitHub Actions job `production-readiness-assertion` запускает этот workflow guard до wrapper и публикует JSON/Markdown/log artifacts; manifest/archive validators сверяют SHA256, receipt/checklist/package validators проверяют handoff, `new-production-evidence-handoff-package-archive.ps1` упаковывает проверенный handoff package в единый ZIP с коротким hash-based default-именем, `validate-production-evidence-handoff-package-archive.ps1` проверяет финальный ZIP, `test-production-evidence-handoff-package-archive-validator.ps1` покрывает tamper-сценарии, а `test-production-evidence-handoff-package-archive-flow.ps1` выполняет всю локальную evidence цепочку одной командой, защищает рекурсивную очистку output-папки, сохраняет JSON/Markdown result artifacts и запускает `validate-production-evidence-handoff-package-archive-flow-result.ps1` для отдельной проверки итогового результата; `test-production-evidence-handoff-package-archive-flow-result-validator.ps1` проверяет fail-closed поведение этого validator на испорченных result artifacts, `test-production-evidence-handoff-package-archive-long-path.ps1` закрывает regression для длинных release id на Windows, `test-production-evidence-handoff-package-archive-ci-regression.ps1` объединяет локальные harnesses в один CI-friendly запуск, `test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1` закрепляет published artifacts contract для `production-evidence`, `test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1` проверяет fail-closed поведение этого workflow guard на tamper-сценариях, GitHub Actions job `production-evidence` запускает этот workflow guard до wrapper, публикует JSON/Markdown artifacts, выводит краткий результат в `GITHUB_STEP_SUMMARY`, валидирует summary через `validate-production-evidence-handoff-package-archive-ci-summary.ps1`, проверяет сам summary validator через `test-production-evidence-handoff-package-archive-ci-summary-validator.ps1`, валидирует финальный CI result artifact через `validate-production-evidence-handoff-package-archive-ci-regression-result.ps1` и проверяет fail-closed поведение этого result validator через `test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1`. Live VPS/staging evidence все еще требуется.

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
