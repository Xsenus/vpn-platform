# Результаты проверок

Дата проверки: 2026-05-25.

## Проверка 2026-06-14: final docs and changelog

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен корневой `CHANGELOG.md`.
- Добавлен финальный runbook `docs/final-runbook.md`.
- README обновлен ссылками на changelog/runbook, командами `e2e:mobile` и `e2e:console`, статусом `467/467`.
- Индекс `docs/README.md` ссылается на changelog и финальный runbook.
- Secret scan дополнительно исключает runtime `tmp`, чтобы full backend suite не конфликтовал с fresh local smoke artifacts.
- Добавлен `backend/tests/VpnPlatform.UnitTests/FinalDocsChangelogTests.cs`.
- Добавлен release entry `2026-06-14-final-docs-changelog` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "FinalDocsChangelogTests|ReleaseDocumentationGuardTests|ReadmeDocumentationTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Final docs/changelog guard: 3/3.
- Documentation guard suite: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 6/6.
- Fresh local SQLite smoke: OK.
- Backend full suite: 467/467.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-final-docs-changelog`, версия `0.103.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: security final checklist

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен финальный checklist `docs/security-final-checklist.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/SecurityFinalChecklistTests.cs`.
- Guard отражением проверяет все admin-контроллеры на class-level `Authorize`, отсутствие `AllowAnonymous` и write/manage policy у write endpoints.
- Checklist связывает существующие gates: `SecretScanTests`, `SecurityHardeningMvpTests`, `AdminAuthorizationPolicyTests`, `RateLimitingSecurityTests`, `SecurityHeadersTests`, `GitHubSecretsAuditTests`, `ProvisioningSecretMaterializerTests`, `PaymentWebhookIdempotencyContractTests`.
- `scan-secrets.ps1` и `scan-secrets.sh` исключают generated Playwright artifacts (`test-results`, `.playwright-artifacts-*`), чтобы actual secret scan не падал на исчезающих временных файлах после E2E.
- Добавлен release entry `2026-06-14-security-final-checklist` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "SecurityFinalChecklistTests|SecretScanTests|SecurityHardeningMvpTests|AdminAuthorizationPolicyTests|RateLimitingSecurityTests|SecurityHeadersTests|GitHubSecretsAuditTests|ProvisioningSecretMaterializerTests|PaymentWebhookIdempotencyContractTests|DocumentationEncodingTests|ReleaseDocumentationGuardTests|ReadmeDocumentationTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
npm run e2e:console --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Security final checklist tests: 3/3.
- Targeted security suite: OK.
- Admin anonymous routes: 0.
- Admin write endpoints без write/manage policy: 0.
- Secret scan: OK.
- Actual PowerShell secret scan: OK.
- Browser console smoke: 6/6.
- Security headers: OK.
- Rate limits: OK.
- RBAC matrix: OK.
- GitHub secrets audit: OK.
- Webhook idempotency contract: OK.
- Fresh local SQLite smoke: OK.
- Backend full suite: 464/464.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-security-final-checklist`, версия `0.102.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: browser console smoke

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен npm-скрипт `e2e:console`.
- Проверка покрывает `public-web`, `cabinet`, `admin-panel`, `mobile-public`, `mobile-cabinet`, `mobile-admin`.
- Существующие Playwright E2E public/cabinet/admin падают при `console.error` и `pageerror`.
- Добавлена инструкция `docs/no-console-errors-smoke.md` и ссылка в `docs/README.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/NoConsoleErrorsSmokeTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-14-no-console-errors-smoke`.
- Добавлен release entry `2026-06-14-no-console-errors-smoke` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:console --prefix frontend
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "NoConsoleErrorsSmokeTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Browser console smoke: 6/6.
- Browser console report: `console.error=0`, `pageerror=0`.
- No console errors smoke tests: 1/1.
- README/release/encoding documentation guard: OK.
- Fresh local SQLite smoke: OK.
- Backend full suite: 461/461.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-no-console-errors-smoke`, версия `0.101.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-14: mobile smoke

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены Playwright-проекты `mobile-public`, `mobile-cabinet`, `mobile-admin`.
- Добавлен npm-скрипт `e2e:mobile`.
- Existing E2E public/cabinet/admin теперь сохраняют mobile-скриншоты при запуске mobile-проектов.
- Добавлена инструкция `docs/mobile-smoke.md` и ссылка в `docs/README.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/MobileSmokeDocumentationTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-14-mobile-smoke`.
- Добавлен release entry `2026-06-14-mobile-smoke` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:mobile --prefix frontend
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "MobileSmokeDocumentationTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
git diff --check
```

Итог:

- Mobile Playwright smoke: 3/3.
- Скриншоты созданы в `frontend/test-results`: `public-mobile.png`, `cabinet-mobile.png`, `admin-mobile.png`.
- Визуальный просмотр скриншотов: public/cabinet/admin не пустые, основные действия доступны; остаточный UX-риск - плотность кабинета и админки на 393px.
- Mobile smoke documentation tests: 1/1.
- README/release/encoding documentation guard: OK.
- Fresh local SQLite smoke: OK.
- Backend full suite: 460/460.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-14-mobile-smoke`, версия `0.100.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-13: fresh local setup

Что проверено:

- Закрыт roadmap-пункт `P11-ACC-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `scripts/fresh-local-smoke.ps1` для чистого локального запуска API на SQLite.
- Добавлена инструкция `docs/fresh-local-smoke.md` и ссылка в `docs/README.md`.
- Исправлен SQLite-баг `/api/me/orders`: сортировка `DateTimeOffset` перенесена после `ToListAsync`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/FreshLocalSetupSmokeTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-fresh-local-smoke`.
- Добавлен release entry `2026-06-13-fresh-local-smoke` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "FreshLocalSetupSmokeTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests|DocumentationEncodingTests"
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18101
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const latest=[...data].sort((a,b)=>new Date(b.releasedAt)-new Date(a.releasedAt))[0]; if (latest.releaseId!=='2026-06-13-fresh-local-smoke'||latest.version!=='0.99.0') throw new Error('unexpected latest'); console.log('latest ok', latest.releaseId, latest.version);"
git diff --check
```

Итог:

- Fresh local smoke tests: 1/1.
- README/release/encoding documentation guard: OK.
- Fresh local smoke script: OK; `tariffs=3`, `providers=8`, sandbox order/payment/subscription/access созданы.
- Backend full suite: 459/459.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-fresh-local-smoke`, версия `0.99.0`.
- Encoding guard: OK.
- `git diff --check`: OK.

## Проверка 2026-06-13: защита кодировки документации

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/DocumentationEncodingTests.cs`.
- Markdown-документация проверяется на `U+FFFD` и типовые mojibake-маркеры без хранения поврежденных строк в `.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-docs-encoding-guard`.
- Добавлен release entry `2026-06-13-docs-encoding-guard` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "DocumentationEncodingTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/developer-guide.md','docs/README.md','backend/tests/VpnPlatform.UnitTests/DocumentationEncodingTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; const markers=[0xfffd,0x00d0,0x00d1,0x00c3,0x00c2].map(x=>String.fromCharCode(x)); for (const file of files) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('encoding marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Documentation encoding tests: 1/1.
- README/release documentation guard: OK.
- Backend full suite: 458/458.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-docs-encoding-guard`, версия `0.98.0`.
- Encoding guard: OK, `U+FFFD` и типовые UTF-8/CP1251 mojibake-маркеры не найдены в markdown-документации.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-docs-encoding-guard`, версия `0.98.0`.

## Проверка 2026-06-13: руководство разработчика

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `docs/developer-guide.md` с описанием архитектуры, доменных сущностей, state machines, платежей, VPN, provisioning, frontend, БД, безопасности и validation gates.
- Добавлен `docs/README.md` как индекс документации проекта.
- Добавлен `backend/tests/VpnPlatform.UnitTests/DeveloperGuideDocumentationTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-developer-guide`.
- Добавлен release entry `2026-06-13-developer-guide` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "DeveloperGuideDocumentationTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/developer-guide.md','docs/README.md','backend/tests/VpnPlatform.UnitTests/DeveloperGuideDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const strict=['README.md','docs/developer-guide.md','docs/README.md','backend/tests/VpnPlatform.UnitTests/DeveloperGuideDocumentationTests.cs']; const markers=[[0x0421,0x0403],[0x0420,0x045f],[0x0420,0x0491],[0x0421,0x0453]].map(xs=>String.fromCharCode(...xs)); for (const file of strict) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('mojibake marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Developer guide documentation tests: 3/3.
- README/release documentation guard: OK.
- Backend full suite: 457/457.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-developer-guide`, версия `0.97.0`.
- Encoding guard: OK, `U+FFFD` не найден в измененных файлах; developer guide, docs index и новый guard-тест дополнительно проверены на mojibake-маркеры.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-developer-guide`, версия `0.97.0`.

## Проверка 2026-06-13: пользовательская помощь

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `docs/user-guide.md` с полным руководством пользователя.
- В публичном сайте добавлена страница `/help` и ссылка "Помощь" в основной навигации.
- В личном кабинете добавлен блок "Как пользоваться сервисом" с шагами оплаты, подключения, продления и поддержки.
- Добавлены `backend/tests/VpnPlatform.UnitTests/UserGuideDocumentationTests.cs` и `frontend/tests/user-help.test.ts`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-user-help-pages`.
- Добавлен release entry `2026-06-13-user-help-pages` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "UserGuideDocumentationTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/user-guide.md','frontend/apps/public-web/src/App.tsx','frontend/apps/public-web/src/styles.css','frontend/apps/cabinet/src/App.tsx','frontend/apps/cabinet/src/styles.css','frontend/tests/user-help.test.ts','backend/tests/VpnPlatform.UnitTests/UserGuideDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const strict=['README.md','docs/user-guide.md','frontend/tests/user-help.test.ts','backend/tests/VpnPlatform.UnitTests/UserGuideDocumentationTests.cs']; const markers=[[0x0421,0x0403],[0x0420,0x045f],[0x0420,0x0491],[0x0421,0x0453]].map(xs=>String.fromCharCode(...xs)); for (const file of strict) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('mojibake marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- User guide documentation tests: 3/3.
- README/release documentation guard: OK.
- Backend full suite: 454/454.
- API build: OK.
- Frontend unit tests: 65/65.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-user-help-pages`, версия `0.96.0`.
- Encoding guard: OK, `U+FFFD` не найден в измененных файлах; новый пользовательский guide и guard-тесты дополнительно проверены на mojibake-маркеры.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-user-help-pages`, версия `0.96.0`.

## Проверка 2026-06-13: руководство администратора

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `docs/admin-guide.md` с операторским runbook по всем вкладкам админки.
- Документ покрывает вход и RBAC, платежи, тарифы, подписки, VPN-доступы, серверы, 3x-ui панели, Telegram-бот, FAQ, контент, сценарии, "Что нового" и подготовку VPS.
- Добавлен `backend/tests/VpnPlatform.UnitTests/AdminGuideDocumentationTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-admin-operator-guide`.
- Добавлен release entry `2026-06-13-admin-operator-guide` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "AdminGuideDocumentationTests|ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/admin-guide.md','backend/tests/VpnPlatform.UnitTests/AdminGuideDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const strict=['README.md','docs/admin-guide.md','backend/tests/VpnPlatform.UnitTests/AdminGuideDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs']; const markers=[[0x0421,0x0403],[0x0420,0x045f],[0x0420,0x0491],[0x0421,0x0453]].map(xs=>String.fromCharCode(...xs)); for (const file of strict) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('mojibake marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Admin guide documentation tests: 3/3.
- README/release documentation guard: OK.
- Backend full suite: 451/451.
- API build: OK.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-admin-operator-guide`, версия `0.95.0`.
- Encoding guard: OK, `U+FFFD` не найден в измененных файлах; README, admin guide и новые guard-тесты дополнительно проверены на mojibake-маркеры.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-admin-operator-guide`, версия `0.95.0`.

## Проверка 2026-06-13: README на русском языке

Что проверено:

- Закрыт roadmap-пункт `P10-DOC-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `README.md` переписан как основной русский входной документ проекта.
- README описывает назначение платформы, состав монорепозитория, запуск без Docker, ручной запуск, Docker/VPS контекст, платежи, VPN, окружения и актуальный статус.
- Добавлен `backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-readme-russian-local-runbook`.
- Добавлен release entry `2026-06-13-readme-russian-local-runbook` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "ReadmeDocumentationTests|ReleaseDocumentationGuardTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['README.md','TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const strict=['README.md','backend/tests/VpnPlatform.UnitTests/ReadmeDocumentationTests.cs']; const markers=[[0x0421,0x0403],[0x0420,0x045f],[0x0420,0x0491],[0x0421,0x0453]].map(xs=>String.fromCharCode(...xs)); for (const file of strict) { const text=fs.readFileSync(file,'utf8'); for (const marker of markers) if (text.includes(marker)) throw new Error('mojibake marker in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- README documentation tests: 3/3.
- Release documentation guard: OK.
- Backend full suite: 448/448.
- API build: OK.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-readme-russian-local-runbook`, версия `0.94.0`.
- Encoding guard: OK, `U+FFFD` не найден в измененных файлах; README и новый README guard дополнительно проверены на mojibake-маркеры.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-readme-russian-local-runbook`, версия `0.94.0`.

## Проверка 2026-06-13: payment provider contract tests

Что проверено:

- Закрыт roadmap-пункт `P9-TST-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `backend/tests/VpnPlatform.UnitTests/PaymentProviderContractTests.cs`.
- Contract gate проверяет реальные DI-регистрации Application/Infrastructure для всех `PaymentProvider`.
- Проверяется один `IPaymentProvider` на каждый enum-провайдер, webhook verifier/status mapper для всех web-провайдеров и bot-only/fail-closed контракт Telegram Stars.
- Local sandbox checkout для YooMoney, YooKassa, RoboKassa, CloudPayments, TBank, Prodamus, Stripe и PayPal проходит без внешних API и без реальных денег.
- Добавлена документация `docs/payment-provider-contract-tests.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-payment-provider-contract-tests`.
- Добавлен release entry `2026-06-13-payment-provider-contract-tests` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "PaymentProviderContractTests"
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/payment-provider-contract-tests.md','backend/tests/VpnPlatform.UnitTests/PaymentProviderContractTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Payment provider contract tests: 12/12.
- Backend full suite: 445/445.
- API build: OK.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK; остаются 2 moderate advisory по `react-router`.
- JSON релизов валиден: latest seed `2026-06-13-payment-provider-contract-tests`, версия `0.93.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-payment-provider-contract-tests`, версия `0.93.0`.

## Проверка 2026-06-13: Playwright E2E admin

Что проверено:

- Закрыт roadmap-пункт `P9-TST-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `frontend/e2e/admin.spec.ts` для проверки админки.
- `frontend/scripts/playwright-webservers.mjs` теперь поднимает public-web, cabinet и admin-panel.
- Добавлен npm-скрипт `e2e:admin`.
- CI и staging-validation теперь запускают `npm run e2e:public`, `npm run e2e:cabinet` и `npm run e2e:admin`.
- Добавлена документация `docs/playwright-admin-e2e.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-playwright-admin-e2e`.
- Добавлен release entry `2026-06-13-playwright-admin-e2e` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:admin --prefix frontend
npm run e2e:public --prefix frontend
npm run e2e:cabinet --prefix frontend
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/playwright-admin-e2e.md','frontend/playwright.config.ts','frontend/e2e/admin.spec.ts','frontend/scripts/playwright-webservers.mjs','frontend/package.json','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Admin Playwright E2E: 1/1.
- Public Playwright E2E: 1/1.
- Cabinet Playwright E2E: 1/1.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK.
- Backend full suite: 433/433.
- API build: OK.
- JSON релизов валиден: latest seed `2026-06-13-playwright-admin-e2e`, версия `0.92.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-playwright-admin-e2e`, версия `0.92.0`.

## Проверка 2026-06-13: Playwright E2E cabinet

Что проверено:

- Закрыт roadmap-пункт `P9-TST-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `frontend/e2e/cabinet.spec.ts` для проверки личного кабинета.
- Добавлен `frontend/scripts/playwright-webservers.mjs`, который поднимает public-web и cabinet на стабильных портах для Playwright.
- Добавлен npm-скрипт `e2e:cabinet`.
- CI и staging-validation теперь запускают `npm run e2e:public` и `npm run e2e:cabinet`, а HTML-report сохраняют из `frontend/playwright-report/e2e`.
- Добавлена документация `docs/playwright-cabinet-e2e.md`, обновлена `docs/playwright-public-e2e.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-playwright-cabinet-e2e`.
- Добавлен release entry `2026-06-13-playwright-cabinet-e2e` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:cabinet --prefix frontend
npm run e2e:public --prefix frontend
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/playwright-public-e2e.md','docs/playwright-cabinet-e2e.md','frontend/playwright.config.ts','frontend/e2e/cabinet.spec.ts','frontend/scripts/playwright-webservers.mjs','frontend/package.json','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Cabinet Playwright E2E: 1/1.
- Public Playwright E2E: 1/1.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK.
- Backend full suite: 433/433.
- API build: OK.
- JSON релизов валиден: latest seed `2026-06-13-playwright-cabinet-e2e`, версия `0.91.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-playwright-cabinet-e2e`, версия `0.91.0`.

## Проверка 2026-06-13: Playwright E2E public

Что проверено:

- Закрыт roadmap-пункт `P9-TST-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены `frontend/playwright.config.ts` и `frontend/e2e/public.spec.ts`.
- Добавлены npm-скрипты `e2e` и `e2e:public`.
- Public E2E проверяет главную, managed FAQ preview, тарифы, web-provider select, public checkout session, переход на `/account`, сохраненную покупку и FAQ search.
- CI и staging-validation теперь устанавливают Chromium, запускают `npm run e2e:public` и сохраняют HTML-report artifact.
- Добавлена документация `docs/playwright-public-e2e.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-playwright-public-e2e`.
- Добавлен release entry `2026-06-13-playwright-public-e2e` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
npm run e2e:public --prefix frontend
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/playwright-public-e2e.md','frontend/playwright.config.ts','frontend/e2e/public.spec.ts','frontend/package.json','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Public Playwright E2E: 1/1.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Backend full suite: 433/433.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-playwright-public-e2e`, версия `0.90.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-playwright-public-e2e`, версия `0.90.0`.

## Проверка 2026-06-13: frontend validation gate

Что проверено:

- Закрыт roadmap-пункт `P9-TST-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Текущий обязательный frontend unit suite обновлен до `64/64`.
- Добавлены `scripts/validate-frontend.ps1` и `scripts/validate-frontend.sh` для Windows/Linux: npm lock/config safety, `npm ci`, typecheck, production build, unit tests и high-severity audit.
- Добавлена документация `docs/frontend-validation-gate.md` с критериями готовности и локальными командами.
- Добавлен frontend guard-test `frontend-validation-gate.test.ts`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-frontend-validation-gate`.
- Добавлен release entry `2026-06-13-frontend-validation-gate` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
[System.Management.Automation.PSParser]::Tokenize((Get-Content scripts/validate-frontend.ps1 -Raw), [ref]$null) | Out-Null
powershell -ExecutionPolicy Bypass -File scripts/validate-frontend.ps1
npm test --prefix frontend
npm run typecheck --prefix frontend
npm run build --prefix frontend
npm audit --audit-level=high --prefix frontend
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
node -e "const fs=require('fs'); const files=['TEST_RESULTS.md','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/frontend-validation-gate.md','scripts/validate-frontend.ps1','scripts/validate-frontend.sh','frontend/tests/frontend-validation-gate.test.ts','backend/src/VpnPlatform.Api/AppReleases/releases.json']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- PowerShell syntax parse `scripts/validate-frontend.ps1`: OK.
- Frontend validation gate: OK.
- Frontend unit tests: 64/64.
- Frontend typecheck: OK.
- Frontend production build: OK.
- Frontend high-severity audit: OK, есть только moderate advisory по `react-router`.
- Release documentation guard: OK.
- JSON релизов валиден: latest seed `2026-06-13-frontend-validation-gate`, версия `0.89.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-frontend-validation-gate`, версия `0.89.0`.

## Проверка 2026-06-13: backend validation gate

Что проверено:

- Закрыт roadmap-пункт `P9-TST-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Текущий обязательный backend suite обновлен до `433/433`.
- Добавлен `scripts/validate-backend.ps1` для Windows/PowerShell: validation safety, secret scan, restore, build, full backend tests, dotnet tools, EF migrations list и EF model drift.
- `scripts/validate-backend.sh` остается Linux/Git Bash entrypoint с тем же обязательным набором.
- Добавлена документация `docs/backend-validation-gate.md` с safe defaults и командами доказательства.
- Добавлен `BackendValidationGateTests`.
- `SecretScanTests` расширен проверкой, что PowerShell backend gate запускает `scan-secrets.ps1`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-backend-validation-gate`.
- Добавлен release entry `2026-06-13-backend-validation-gate` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
[System.Management.Automation.PSParser]::Tokenize((Get-Content scripts/validate-backend.ps1 -Raw), [ref]$null) | Out-Null
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "BackendValidationGateTests|SecretScanTests|ReleaseDocumentationGuardTests"
powershell -ExecutionPolicy Bypass -File scripts/validate-backend.ps1
dotnet test backend/VpnPlatform.sln --configuration Release
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj --configuration Release
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/BackendValidationGateTests.cs','backend/tests/VpnPlatform.UnitTests/SecretScanTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/backend-validation-gate.md','scripts/validate-backend.ps1','scripts/validate-backend.sh','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- PowerShell syntax parse `scripts/validate-backend.ps1`: OK.
- Targeted backend validation/release/secret guard tests: 9/9.
- Backend full suite: 433/433.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-backend-validation-gate`, версия `0.88.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, bootstrap login `smoke-admin@example.test`, `/api/app-version/latest`; latest release `2026-06-13-backend-validation-gate`, версия `0.88.0`.

## Проверка 2026-06-13: post-deploy smoke

Что проверено:

- Закрыт roadmap-пункт `P8-CI-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `scripts/post-deploy-smoke.sh` с проверками API live/ready, `/metrics`, `/api/public/payments/providers`, public web, cabinet web и admin web.
- `.github/workflows/deploy-vps.yml` запускает шаг `Post-deploy smoke` после docker или systemd deploy.
- Workflow вычисляет URL по режиму: docker public `:5173`, systemd public `VITE_PUBLIC_WEB_URL` или `http://VPS_HOST`, cabinet `:5174`, admin `:5175`, API `:8080`.
- Добавлены optional secrets `POST_DEPLOY_API_URL`, `POST_DEPLOY_PUBLIC_WEB_URL`, `POST_DEPLOY_CABINET_WEB_URL`, `POST_DEPLOY_ADMIN_WEB_URL`.
- `.github/github-secrets.audit.json` и `docs/github-secrets-audit.md` обновлены под новые optional smoke secrets.
- Добавлена документация `docs/post-deploy-smoke.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-post-deploy-smoke`.
- Добавлен release entry `2026-06-13-post-deploy-smoke` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
& 'C:\Program Files\Git\bin\bash.exe' -n scripts/post-deploy-smoke.sh
powershell -ExecutionPolicy Bypass -File scripts/audit-github-secrets.ps1 -DryRun
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "PostDeploySmokeTests|GitHubSecretsAuditTests|ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['.github/workflows/deploy-vps.yml','.github/github-secrets.audit.json','backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/PostDeploySmokeTests.cs','backend/tests/VpnPlatform.UnitTests/GitHubSecretsAuditTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/post-deploy-smoke.md','docs/github-deployment.md','docs/github-secrets-audit.md','scripts/post-deploy-smoke.sh','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- `bash -n scripts/post-deploy-smoke.sh`: OK.
- GitHub secrets audit dry-run: OK, optional `POST_DEPLOY_*` names совпадают с workflow references.
- Local post-deploy smoke на чистой SQLite API и тестовом HTML-сервере: OK, проверены `/health/live`, `/health/ready`, `/metrics`, `/api/public/payments/providers`, public/cabinet/admin HTML.
- Targeted post-deploy/secrets/release guard tests: 9/9.
- Backend full suite: 430/430.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-post-deploy-smoke`, версия `0.87.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: latest release `2026-06-13-post-deploy-smoke`, версия `0.87.0`; серверов `1`, provisioning-запусков `0`.
- Live VPS post-deploy smoke будет выполнен GitHub Actions после следующего deploy; из локальной среды production deploy не запускался.

## Проверка 2026-06-13: безопасная очистка VPS

Что проверено:

- Закрыт roadmap-пункт `P8-CI-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `scripts/vps-maintenance.sh` с dry-run по умолчанию и реальной очисткой только через `--apply`.
- Скрипт печатает `df -h`, `free -h`, `du -sh` до и после maintenance.
- Старые release-директории удаляются только внутри `APP_DIR/releases`, только с именем git sha и с сохранением `KEEP_RELEASES`.
- Защищены `APP_DIR`, `APP_DIR/shared`, `APP_DIR/current`, `APP_DIR/releases`, текущий symlink release и любые пути вне `APP_DIR`.
- App logs очищаются только внутри `APP_DIR/logs`; production `.env`, database dumps и рабочие каталоги не трогаются.
- `journalctl --vacuum-time`, `apt-get clean/autoclean` включены как безопасная системная очистка.
- Docker prune включается только через `--docker-prune`, не выполняет `docker volume prune`.
- Добавлена документация `docs/vps-maintenance.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-vps-maintenance-safe-cleanup`.
- Добавлен release entry `2026-06-13-vps-maintenance-safe-cleanup` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
bash -n scripts/vps-maintenance.sh
bash scripts/vps-maintenance.sh --dry-run --app-dir /tmp/vpn-platform-maintenance-smoke
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "VpsMaintenanceScriptTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/VpsMaintenanceScriptTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/vps-maintenance.md','docs/github-deployment.md','scripts/vps-maintenance.sh','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- `bash -n scripts/vps-maintenance.sh`: OK.
- Local maintenance dry-run: OK, команды удаления напечатаны как `[dry-run]`, рабочие данные не удалялись.
- Targeted VPS maintenance tests: 4/4.
- Targeted release/docs guard tests: 3/3.
- Backend full suite: 427/427.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-vps-maintenance-safe-cleanup`, версия `0.86.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-13-vps-maintenance-safe-cleanup`, версия `0.86.0`; серверов `1`, provisioning-запусков `0`.
- Live VPS cleanup не запускался из этой среды: скрипт подготовлен, но реальная очистка требует явного запуска оператором с `--apply`.

## Проверка 2026-06-13: аудит GitHub Secrets

Что проверено:

- Закрыт roadmap-пункт `P8-CI-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `.github/github-secrets.audit.json` со списком required/optional GitHub Actions secret names без значений.
- Required secrets совпадают с explicit gate в `.github/workflows/deploy-vps.yml`: `VPS_HOST`, `VPS_USER`, `VPS_PORT`, `VPS_APP_DIR`, `VPS_SSH_KEY`, `PRODUCTION_ENV_FILE`.
- Optional secrets покрывают остальные references workflow: `VPS_DEPLOY_MODE`, `VITE_API_BASE_URL`, `VITE_PUBLIC_WEB_URL`.
- Registry secrets явно отмечены как not required, потому что текущие workflows не пушат container images в registry.
- Добавлен `scripts/audit-github-secrets.ps1`: `-DryRun` проверяет локальный конфиг и workflow, live-режим получает только names через GitHub REST API и не выводит значения.
- Добавлена документация `docs/github-secrets-audit.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-13-github-secrets-audit`.
- Добавлен release entry `2026-06-13-github-secrets-audit` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/audit-github-secrets.ps1 -DryRun
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "GitHubSecretsAuditTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['.github/github-secrets.audit.json','backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/GitHubSecretsAuditTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/github-secrets-audit.md','docs/github-deployment.md','scripts/audit-github-secrets.ps1','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- GitHub secrets audit dry-run: OK, config и workflow references совпадают; GitHub API не вызывался.
- Targeted GitHub secrets audit tests: 3/3.
- Targeted release/docs guard tests: 3/3.
- Backend full suite: 423/423.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-13-github-secrets-audit`, версия `0.85.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-13-github-secrets-audit`, версия `0.85.0`; серверов `1`, provisioning-запусков `0`.
- Live GitHub secrets audit не выполнялся из этой среды: `GITHUB_TOKEN/GH_TOKEN` в env отсутствует. Скрипт готов к запуску с токеном, который может читать repository Actions secrets metadata; значения secrets GitHub API не возвращает.

## Проверка 2026-06-12: required checks для main

Что проверено:

- Закрыт roadmap-пункт `P8-CI-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен конфиг `.github/branch-protection.required-checks.json` для веток `main` и `master`.
- Required checks синхронизированы с job names workflow `validation`: backend, frontend, provisioning/Ansible и docker build.
- Добавлен `scripts/configure-branch-protection.ps1` с GitHub REST API, `-DryRun`, чтением token только из env и проверкой applied contexts после применения.
- Добавлена документация `docs/github-required-checks.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-12-required-checks-main`.
- Добавлен release entry `2026-06-12-required-checks-main` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/configure-branch-protection.ps1 -DryRun
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "BranchProtectionGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['.github/branch-protection.required-checks.json','backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/BranchProtectionGuardTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/github-required-checks.md','docs/github-deployment.md','scripts/configure-branch-protection.ps1','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Branch protection dry-run: OK, payload собран для `Xsenus/vpn-platform`, ветки `main/master`, 4 required checks.
- Targeted branch protection guard tests: 3/3.
- Targeted release/docs guard tests: 3/3.
- Backend full suite: 420/420.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-required-checks-main`, версия `0.84.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-12-required-checks-main`, версия `0.84.0`; серверов `1`, provisioning-запусков `0`.
- Live GitHub branch protection не применялся из этой среды: `gh` CLI не установлен, `GITHUB_TOKEN/GH_TOKEN` в env отсутствует. Для применения нужен запуск `scripts/configure-branch-protection.ps1` с токеном repository administration write.

## Проверка 2026-06-12: auto-detect deploy docker/systemd

Что проверено:

- Закрыт roadmap-пункт `P8-CI-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `.github/workflows/deploy-vps.yml` пишет requested/selected режим, `docker_detected` и причину выбора в `::notice`.
- Workflow добавляет блок `VPS deploy mode` в `$GITHUB_STEP_SUMMARY`.
- `auto` выбирает `docker` только при наличии `docker` и `docker compose version` на VPS, иначе выбирает `systemd`.
- Добавлена документация `docs/deploy-vps-auto-detect.md`.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-12-deploy-mode-auto-detect`.
- Добавлен release entry `2026-06-12-deploy-mode-auto-detect` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "DeployWorkflowGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['.github/workflows/deploy-vps.yml','backend/src/VpnPlatform.Api/AppReleases/releases.json','backend/tests/VpnPlatform.UnitTests/DeployWorkflowGuardTests.cs','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/deploy-vps-auto-detect.md','docs/github-deployment.md','TEST_RESULTS.md']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted deploy workflow guard tests: 2/2.
- Targeted release/docs guard tests: 3/3.
- Backend full suite: 417/417.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-deploy-mode-auto-detect`, версия `0.83.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-12-deploy-mode-auto-detect`, версия `0.83.0`; серверов `1`, provisioning-запусков `0`.

## Проверка 2026-06-12: runbook live provisioning

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `docs/live-provisioning-runbook.md` с preflight, Ansible syntax-check, SSH/known_hosts, live flags, тегами ноды, API-порядком precheck/deploy, ручным runner dry-run, rollback/failure path, smoke и fail-closed правилами.
- `docs/provisioning.md` ссылается на live provisioning runbook.
- `ReleaseDocumentationGuardTests` расширен ожиданием releaseId `2026-06-12-live-provisioning-runbook`.
- Добавлен guard `Live_Provisioning_Runbook_Should_Cover_Operator_Gates`.
- Добавлен release entry `2026-06-12-live-provisioning-runbook` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ReleaseDocumentationGuardTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/live-provisioning-runbook.md','docs/provisioning.md','TEST_RESULTS.md','backend/tests/VpnPlatform.UnitTests/ReleaseDocumentationGuardTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted release/docs guard tests: 3/3.
- Backend full suite: 415/415.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-live-provisioning-runbook`, версия `0.82.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-12-live-provisioning-runbook`, версия `0.82.0`; серверов `1`, provisioning-запусков `0`.

## Проверка 2026-06-12: rollback состояния VPS

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `ProvisioningWorker` снимает snapshot `VpnNode` перед deploy и при ошибке возвращает node status, health, availability и эксплуатационные поля к значениям до deploy.
- При failed deploy run остается `Failed`, а `VpnNode.ProvisioningStatus` становится `Failed`, чтобы оператор видел инцидент.
- В run добавляется шаг `Rollback node state`, а audit получает событие `provisioning.rollback_applied`.
- Support context и Telegram-уведомление получают redacted-контекст ошибки без SSH/password/token утечек.
- Добавлена документация `docs/vps-provisioning-rollback.md`.
- Добавлен release entry `2026-06-12-vps-provisioning-rollback` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "OwnVps_Deploy_Failure_Should_Roll_Back_Node_State_And_Surface_Admin_Context|OwnVps_DryRun_E2E_Should_Protect_Credential_Process_Mock_Deploy_Create_Access_And_Admin_Visibility|OwnVps_DryRun_Failure_Should_Create_Support_Context_And_Retry_Without_Duplicates"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "OwnVps_Deploy_Failure_Should_Roll_Back_Node_State_And_Surface_Admin_Context"
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/vps-provisioning-rollback.md','backend/src/VpnPlatform.Infrastructure/HostedServices/ProvisioningWorker.cs','backend/tests/VpnPlatform.UnitTests/SandboxE2EScenariosMvpTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted rollback/E2E tests: 3/3.
- Targeted rollback regression: 1/1.
- Backend full suite: 414/414.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-vps-provisioning-rollback`, версия `0.81.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`; latest release `2026-06-12-vps-provisioning-rollback`, версия `0.81.0`; серверов `1`, provisioning-запусков `0`.

## Проверка 2026-06-12: отчет precheck VPS

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `AnsibleProvisioningExecutor` формирует JSON `Precheck report` для dry-run/mock и live runner, сохраняет его отдельным шагом и добавляет в summary log.
- Admin API возвращает `precheckReportPreview` в списке provisioning runs и полный `precheckReport` в деталях запуска.
- Админка показывает отчет precheck в разделе «Подготовка VPS».
- `precheck-node.yml` инспектирует OS, ports, disk, RAM, firewall, Docker, systemd и 3x-ui availability.
- Добавлена документация `docs/vps-precheck-report.md`.
- Добавлен release entry `2026-06-12-vps-precheck-report` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "OwnVpsProvisioningMvpTests|ProvisioningSecretMaterializerTests"
npm test -- --test-name-pattern "provisioning"
npm run typecheck --workspace apps/admin-panel
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
npm run build --workspace apps/admin-panel
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/vps-precheck-report.md','TEST_RESULTS.md','backend/src/VpnPlatform.Infrastructure/Provisioning/AnsibleProvisioningExecutor.cs','backend/src/VpnPlatform.Api/Controllers/Admin/AdminOperationsController.cs','backend/tests/VpnPlatform.UnitTests/OwnVpsProvisioningMvpTests.cs','frontend/packages/api-client/src/index.ts','frontend/apps/admin-panel/src/App.tsx','infra/ansible/playbooks/precheck-node.yml']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted provisioning tests: 19/19.
- Frontend API tests: 61/61.
- Admin panel typecheck: OK.
- Admin panel production build: OK.
- Backend full suite: 413/413.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-vps-precheck-report`, версия `0.80.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `POST /api/admin/servers/{id}/precheck`, `/api/admin/provisioning-runs`, `/api/admin/provisioning-runs/{id}`; latest release `2026-06-12-vps-precheck-report`, версия `0.80.0`; серверов `1`; precheck run `ReadyToDeploy`; `precheckReportPreview` содержит `x3ui`; полный `precheckReport` содержит `firewall`.

## Проверка 2026-06-12: live Ansible credentials

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `AnsibleProvisioningExecutor` добавляет temporary SSH key path и temporary `secrets` directory path в список known secrets для redaction.
- Runner output/stderr/step logs больше не сохраняют raw private key, protected payload, legacy key path, temporary key path и panel password.
- Добавлена документация `docs/live-ansible-credentials.md`.
- Добавлен release entry `2026-06-12-live-ansible-credentials` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ProvisioningSecretMaterializerTests|OwnVpsProvisioningMvpTests"
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/live-ansible-credentials.md','backend/src/VpnPlatform.Infrastructure/Provisioning/AnsibleProvisioningExecutor.cs','backend/tests/VpnPlatform.UnitTests/ProvisioningSecretMaterializerTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- Targeted provisioning secret tests: 17/17.
- Backend full suite: 411/411.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-live-ansible-credentials`, версия `0.79.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`, `/api/public/payments/providers`; latest release `2026-06-12-live-ansible-credentials`, версия `0.79.0`; серверов `1`, provisioning-запусков `0`, публичных провайдеров `8`.

## Проверка 2026-06-12: границы режимов provisioning VPS

Что проверено:

- Закрыт roadmap-пункт `P7-PROV-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Backend разделяет `dry-run`, `validation-deploy`, `live-deploy-blocked` и `live-deploy` через `ProvisioningModeDescriptor`.
- Admin API отдаёт `mode`, `riskLevel`, `liveDeployAllowed`, `nextAction`, `operatorWarning` и отдельные `deployMode*` поля для следующего deploy после dry-run.
- Админка показывает режимы и риски в списке серверов и provisioning-запусков, блокирует запрещённый live deploy и оставляет `Precheck VPS` безопасным действием.
- Добавлена документация `docs/provisioning-modes.md`.
- Добавлен release entry `2026-06-12-provisioning-mode-boundaries` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "FullyQualifiedName~OwnVpsProvisioningMvpTests"
npm test -- --runInBand
npm run typecheck --workspace apps/admin-panel
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj
npm run build --workspace apps/admin-panel
dotnet build backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/provisioning-modes.md','backend/src/VpnPlatform.Application/Services/ProvisioningService.cs','backend/src/VpnPlatform.Api/Controllers/Admin/AdminOperationsController.cs','backend/tests/VpnPlatform.UnitTests/OwnVpsProvisioningMvpTests.cs','frontend/packages/api-client/src/index.ts','frontend/apps/admin-panel/src/App.tsx','frontend/tests/api-client.test.ts']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
git diff --check
```

Итог:

- `OwnVpsProvisioningMvpTests`: 12/12.
- Frontend tests: 61/61.
- Admin-panel typecheck/build: OK.
- Backend full suite: 410/410.
- API build: OK, предупреждений 0.
- JSON релизов валиден: latest seed `2026-06-12-provisioning-mode-boundaries`, версия `0.78.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `git diff --check`: OK.
- Local SQLite HTTP-smoke на чистой временной БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest`, `/api/admin/servers`, `/api/admin/provisioning-runs`, `/api/public/payments/providers`; latest release `2026-06-12-provisioning-mode-boundaries`, версия `0.78.0`; sandbox server вернул `provisioningMode=live-deploy-blocked`, `provisioningRiskLevel=blocked`, `liveDeployAllowed=false`; публичные провайдеры `8`.
- Browser smoke админки: `http://127.0.0.1:5175/` открыл login screen, title `VPN Platform — админ-панель`, React root найден, консольных `error` logs нет; dev server остановлен.

## Проверка 2026-06-12: secret scan gate

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены `scripts/scan-secrets.ps1` и `scripts/scan-secrets.sh`.
- Scanner проверяет Telegram, Stripe/OpenAI, GitHub, GitLab, AWS, Google, Slack tokens и PEM private keys.
- `validate-backend.sh` и `validate-all.sh` запускают secret scan до build/test шагов.
- `check-validation-safety.sh` проверяет наличие scanner и базовых token/private-key паттернов.
- Добавлен allowlist для тестовых fixture и локальных placeholders.
- Добавлена документация `docs/secret-scan.md`.
- Добавлен release entry `2026-06-12-secret-scan-gate` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/secret-scan.md','TEST_RESULTS.md','scripts/scan-secrets.ps1','scripts/scan-secrets.sh','scripts/validate-all.sh','scripts/validate-backend.sh','scripts/check-validation-safety.sh','backend/tests/VpnPlatform.UnitTests/SecretScanTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter SecretScanTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-secret-scan-gate`, версия `0.77.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- PowerShell secret scan: `Files scanned: 386. Findings: 0`.
- Bash scan локально не запущен: в текущей Windows-среде нет `/bin/bash`; bash-скрипт покрыт `SecretScanTests` и рассчитан на Linux CI.
- `SecretScanTests`: 3/3.
- Release docs tests: 14/14.
- Backend full suite: 408/408.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-secret-scan-gate`, версия `0.77.0`.

## Проверка 2026-06-12: security headers API и frontend

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Backend API получил `SecurityHeadersMiddleware`.
- API выставляет `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`.
- `Strict-Transport-Security` выставляется только в `Production`.
- Development Swagger UI не получает API CSP, чтобы не ломать локальную документацию.
- Frontend Dockerfiles `public-web`, `cabinet`, `admin-panel` копируют общий `frontend/nginx.security.conf`.
- nginx-конфиг frontend содержит CSP, HSTS, security headers и SPA fallback `try_files`.
- Production CORS остается allow-list based через `Cors:AllowedOrigins` и startup validator.
- Добавлена документация `docs/security-headers.md`.
- Добавлен release entry `2026-06-12-security-headers` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/security-headers.md','backend/src/VpnPlatform.Api/Middleware/SecurityHeadersMiddleware.cs','backend/src/VpnPlatform.Api/Program.cs','backend/tests/VpnPlatform.UnitTests/SecurityHeadersTests.cs','frontend/nginx.security.conf','frontend/Dockerfile.public-web','frontend/Dockerfile.cabinet','frontend/Dockerfile.admin-panel']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter SecurityHeadersTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
npm run typecheck
npm run build
npm test
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-security-headers`, версия `0.76.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `SecurityHeadersTests`: 5/5.
- Release docs tests: 14/14.
- Frontend typecheck: OK.
- Frontend build: OK.
- Frontend tests: 61/61.
- Backend full suite: 405/405.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-security-headers`, версия `0.76.0`; `/health/live` вернул `nosniff`, `DENY`, `no-referrer`, `Permissions-Policy`, API CSP, без HSTS в Local.

## Проверка 2026-06-12: rate limiting публичного API

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены `ApiRateLimitPolicies` с policy `auth-sensitive`, `public-checkout`, `webhook`.
- `Program.cs` подключает `builder.Services.AddRateLimiter(ApiRateLimitPolicies.Configure)` и `app.UseRateLimiter()`.
- Auth endpoints `register/login/refresh/forgot-password/reset-password` защищены policy `auth-sensitive`.
- Публичные checkout endpoints защищены policy `public-checkout`.
- Платежные webhook и channel webhook controllers защищены policy `webhook`.
- При превышении лимита API возвращает `429 Too Many Requests` с problem JSON.
- Добавлена документация `docs/rate-limiting.md`.
- Добавлен release entry `2026-06-12-api-rate-limiting` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/rate-limiting.md','backend/src/VpnPlatform.Api/Security/ApiRateLimitPolicies.cs','backend/src/VpnPlatform.Api/Program.cs','backend/src/VpnPlatform.Api/Controllers/Auth/AuthController.cs','backend/src/VpnPlatform.Api/Controllers/Public/OrdersController.cs','backend/src/VpnPlatform.Api/Controllers/Webhooks/PaymentWebhooksController.cs','backend/src/VpnPlatform.Api/Controllers/Channels/ChannelWebhooksController.cs','backend/tests/VpnPlatform.UnitTests/RateLimitingSecurityTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter RateLimitingSecurityTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-api-rate-limiting`, версия `0.75.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `RateLimitingSecurityTests`: 11/11.
- Release docs tests: 14/14.
- Backend full suite: 400/400.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-api-rate-limiting`, версия `0.75.0`; превышение `POST /api/auth/forgot-password` вернуло `429`.

## Проверка 2026-06-12: RBAC-матрица админки

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлена единая матрица `AdminPolicies.PolicyRoles` для всех admin-policy.
- `Program.cs` регистрирует authorization policies из матрицы, без ручного дублирования списка.
- Роль `User` исключена из всех admin-policy.
- Роли `ReadOnly`, `SupportAgent`, `FinanceManager`, `Operator`, `Admin`, `SuperAdmin` разведены по read/write/manage-доступам.
- Добавлены runtime authorization tests для разрешенных и запрещенных комбинаций ролей.
- Добавлена документация `docs/rbac-policy-matrix.md`.
- Добавлен release entry `2026-06-12-rbac-policy-matrix` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/rbac-policy-matrix.md','backend/src/VpnPlatform.Application/Common/AdminPolicies.cs','backend/src/VpnPlatform.Api/Program.cs','backend/tests/VpnPlatform.UnitTests/AdminAuthorizationPolicyTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter AdminAuthorizationPolicyTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-rbac-policy-matrix`, версия `0.74.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- `AdminAuthorizationPolicyTests`: 33/33.
- Release docs tests: 14/14.
- Backend full suite: 389/389.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-rbac-policy-matrix`, версия `0.74.0`.

## Проверка 2026-06-12: безопасная ротация секретов

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Платежная ротация продолжает писать `payment_provider.secret.rotate`.
- Добавлен `server.secret.rotate` для SSH credential и panel password.
- Добавлен `telegram_bot.secret.rotate` для BotToken и SecretToken.
- При ротации server secrets создаются новые `secretref:ssh:*` и `secretref:panel:*`, но API/audit не раскрывают raw secret, protected payload или `secretref:*`.
- Telegram settings продолжают сохранять BotToken/SecretToken через write-only поля и audit содержит только флаги `rotatedBotToken` / `rotatedSecretToken`.
- Добавлена документация `docs/secret-rotation.md`.
- Добавлен release entry `2026-06-12-secret-rotation-audit` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "SecurityHardeningMvpTests|AdminTelegramBotSettingsControllerTests|AuditLogMvpTests" --logger "console;verbosity=minimal"
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.releasedAt,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/secret-rotation.md','backend/src/VpnPlatform.Api/Controllers/Admin/AdminOperationsController.cs','backend/src/VpnPlatform.Api/Controllers/Admin/AdminTelegramBotSettingsController.cs','backend/tests/VpnPlatform.UnitTests/SecurityHardeningMvpTests.cs','backend/tests/VpnPlatform.UnitTests/AdminTelegramBotSettingsControllerTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- `SecurityHardeningMvpTests|AdminTelegramBotSettingsControllerTests|AuditLogMvpTests`: 14/14.
- JSON релизов валиден: latest `2026-06-12-secret-rotation-audit`, версия `0.73.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- Release docs tests: 14/14.
- Backend full suite: 378/378.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-secret-rotation-audit`, версия `0.73.0`.

## Проверка 2026-06-12: production secret storage для provisioning

Что проверено:

- Закрыт roadmap-пункт `P6-SEC-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `ProvisioningSecretMaterializer` для временной материализации protected SSH private key.
- `AnsibleProvisioningExecutor` теперь умеет передавать runner только path к временно созданному key file и удаляет materialized secret в `finally`.
- Password-based live SSH, `validation-placeholder:*`, legacy protected values в `SshPrivateKeyPath` и missing protected payload при наличии `SshCredentialRef` остаются fail-closed.
- Runner stdout/stderr и step output редактируются с учетом protected payload и расшифрованного plaintext.
- Добавлены docs: `docs/production-secret-storage.md`; обновлены `docs/TODO_SECURE_PROVISIONING_SECRETS.md` и `docs/SECRET_MIGRATION_PLAN.md`.
- Добавлен release entry `2026-06-12-production-provisioning-secret-storage` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProvisioningSecretMaterializerTests|OwnVpsProvisioningMvpTests|SecurityHardeningMvpTests" --logger "console;verbosity=minimal"
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','docs/production-secret-storage.md','docs/TODO_SECURE_PROVISIONING_SECRETS.md','docs/SECRET_MIGRATION_PLAN.md','backend/src/VpnPlatform.Infrastructure/Provisioning/ProvisioningSecretMaterializer.cs','backend/src/VpnPlatform.Infrastructure/Provisioning/AnsibleProvisioningExecutor.cs','backend/tests/VpnPlatform.UnitTests/ProvisioningSecretMaterializerTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); console.log('encoding guard ok', data.at(-1).releaseId, data.at(-1).version);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- `ProvisioningSecretMaterializerTests|OwnVpsProvisioningMvpTests|SecurityHardeningMvpTests`: 22/22.
- JSON релизов валиден: latest `2026-06-12-production-provisioning-secret-storage`, версия `0.72.0`.
- Encoding guard: OK, `U+FFFD` не найден.
- Release docs tests: 14/14.
- Backend full suite: 377/377.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-production-provisioning-secret-storage`, версия `0.72.0`.
- Live Ansible на реальном VPS не запускался: проверена локальная безопасная materialization/cleanup логика и fail-closed ветки.

## Проверка 2026-06-12: аудит PostgreSQL schema

Что проверено:

- Закрыт roadmap-пункт `P5-DB-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлены `scripts/audit-postgres-schema.sh` и `scripts/audit-postgres-schema.ps1`.
- EF-only режим без PostgreSQL формирует `ef-migrations.txt`, `postgres-migrations-idempotent.sql`, `audit-metadata.env` и явный `postgres-schema-snapshot.txt` с пометкой, что `DATABASE_URL` не задан.
- PostgreSQL режим при наличии `DATABASE_URL` и `psql` снимает sanitized snapshot таблиц, колонок, nullable-полей, индексов и FK через `information_schema`/`pg_indexes`, без чтения пользовательских данных.
- Добавлен runbook `docs/postgres-schema-audit.md`.
- Добавлен `PostgresSchemaAuditTests`: проверяет PostgreSQL EF metadata, PK у mapped entities, индексы, FK, nullable metadata, migration chain, audit-скрипты и документацию.
- Добавлен release entry `2026-06-12-postgres-schema-audit` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
$null = [scriptblock]::Create((Get-Content -Path scripts\audit-postgres-schema.ps1 -Raw))
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','docs/postgres-schema-audit.md','scripts/audit-postgres-schema.sh','scripts/audit-postgres-schema.ps1','backend/tests/VpnPlatform.UnitTests/PostgresSchemaAuditTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter PostgresSchemaAuditTests --logger "console;verbosity=minimal"
$env:SCHEMA_AUDIT_DIR = Join-Path $PWD 'artifacts\postgres-schema-audit-local'; powershell -ExecutionPolicy Bypass -File scripts\audit-postgres-schema.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests" --logger "console;verbosity=minimal"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
git diff --check
```

Итог:

- JSON релизов валиден: latest `2026-06-12-postgres-schema-audit`, версия `0.71.0`.
- PowerShell syntax check: OK.
- Encoding guard: OK, `U+FFFD` не найден.
- `PostgresSchemaAuditTests`: 3/3.
- `scripts\audit-postgres-schema.ps1`: OK, EF-only artifacts созданы в `artifacts/postgres-schema-audit-local`.
- Реальный `psql` snapshot не запускался локально, потому что `DATABASE_URL` для отдельной PostgreSQL-БД не задан; runbook описывает staging/VPS запуск.
- Bash syntax check не запускался: доступный `bash` указывает на WSL без `/bin/bash`; Linux-скрипт покрыт static guard в `PostgresSchemaAuditTests`.
- Release docs tests: 14/14.
- Backend full suite: 373/373.
- Local SQLite HTTP-smoke на чистой БД: `/health/live`, `/health/ready`, `/metrics`, login `admin@local.test`, `/api/app-version/latest` с Bearer-токеном; latest release `2026-06-12-postgres-schema-audit`, версия `0.71.0`.

## Проверка 2026-06-12: backup/restore PostgreSQL для VPS

Что проверено:

- Закрыт roadmap-пункт `P5-DB-004` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `scripts/backup-db.sh` усилен проверкой `pg_dump`, custom dump, `.dump.list` и retention через `BACKUP_RETENTION_DAYS`.
- Добавлены PowerShell и Linux restore-скрипты: `scripts/restore-db.sh`, `scripts/restore-db.ps1`.
- Добавлен PowerShell backup-скрипт `scripts/backup-db.ps1`.
- Restore требует `BACKUP_FILE` и отдельный `RESTORE_DATABASE_URL`; совпадение с `DATABASE_URL` блокируется без `RESTORE_ALLOW_DATABASE_URL_MATCH=true`.
- `scripts/apply-migrations.sh` передает `BACKUP_RETENTION_DAYS` в pre-migration backup.
- `backups/` добавлен в `.gitignore`, чтобы dump-файлы не попадали в репозиторий.
- Добавлен runbook `docs/postgres-backup-restore.md` с test restore в отдельную БД `vpnplatform_restore_check`.
- Добавлен release entry `2026-06-12-postgres-backup-restore-runbook` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter DatabaseBackupRestoreScriptsTests --logger "console;verbosity=minimal"
$null = [scriptblock]::Create((Get-Content -Path scripts\backup-db.ps1 -Raw)); $null = [scriptblock]::Create((Get-Content -Path scripts\restore-db.ps1 -Raw))
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','docs/postgres-backup-restore.md','scripts/backup-db.sh','scripts/restore-db.sh','scripts/backup-db.ps1','scripts/restore-db.ps1','scripts/apply-migrations.sh','backend/tests/VpnPlatform.UnitTests/DatabaseBackupRestoreScriptsTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `DatabaseBackupRestoreScriptsTests`: 1/1 пройдено.
- PowerShell syntax check: `backup-db.ps1` и `restore-db.ps1` валидны.
- Реальный `pg_dump/pg_restore` не запускался в этой Windows-среде, потому что локальный PostgreSQL test restore не поднят; runbook и скрипты готовы для VPS/Linux или Windows с установленными PostgreSQL client tools.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 370/370 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-postgres-backup-restore-runbook`, версия `0.70.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: чистая БД, `/health/live`, `/health/ready`, `/metrics`, `/api/auth/login`, `/api/app-version/latest`; latest release `2026-06-12-postgres-backup-restore-runbook`, версия `0.70.0`, `readyChecks=2`, metrics содержат `vpnplatform_http_requests_total` и `vpnplatform_api_uptime_seconds`.

## Проверка 2026-06-12: локальный seed данных и VPN sandbox

Что проверено:

- Закрыт roadmap-пункт `P5-DB-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `DbInitializer.SeedDemoDataAsync` теперь создает sandbox VPN-инфраструктуру для чистой локальной БД: node group `sandbox`, panel `sandbox-x3ui-panel`, default inbound `sandbox-default-vless`, node `sandbox-vpn-node`.
- Sandbox-нода создается в статусах `Ready` и `Healthy`, доступна для новых пользователей и содержит протоколы `vless,vmess,trojan`.
- Добавлен SQLite acceptance-тест локального seed: admin user, тарифы, sandbox payments, Telegram Stars disabled, VPN panel/inbound/node, читаемый русский контент и идемпотентность повторного seed.
- Добавлен release entry `2026-06-12-local-seed-vpn-infrastructure` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter PaymentProviderSandboxSeedTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','backend/src/VpnPlatform.Infrastructure/Services/SystemServices.cs','backend/tests/VpnPlatform.UnitTests/PaymentProviderSandboxSeedTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `PaymentProviderSandboxSeedTests`: 2/2 пройдено.
- API Release build: пройдено без предупреждений и ошибок.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 369/369 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-local-seed-vpn-infrastructure`, версия `0.69.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: чистая БД, `/health/live`, `/health/ready`, `/metrics`, `/api/auth/login`, `/api/app-version/latest`, `/api/public/tariffs`, `/api/public/payments/providers`, `/api/admin/servers`, `/api/admin/vpn-panels`; latest release `2026-06-12-local-seed-vpn-infrastructure`, версия `0.69.0`, тарифы `3`, провайдеры `8`, серверы `1`, панели `1`, `sandbox-vpn-node=true`, `sandbox-x3ui-panel=true`.

## Проверка 2026-06-12: кроссплатформенный EF model drift gate

Что проверено:

- Закрыт roadmap-пункт `P5-DB-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен `scripts/check-ef-drift.ps1` для локальной проверки EF model drift на Windows/PowerShell.
- PowerShell drift-check использует `dotnet ef migrations has-pending-model-changes`, безопасные env-переменные, временную диагностическую миграцию `__ModelDriftCheck` и cleanup без изменения snapshot.
- `EfModelDriftTests` расширен acceptance-проверкой Linux и PowerShell drift-скриптов, env safety и документации.
- Обновлена инструкция `docs/ef-model-drift-check.md`.
- Добавлен release entry `2026-06-12-ef-drift-powershell-gate` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter EfModelDriftTests --logger "console;verbosity=minimal"
powershell -ExecutionPolicy Bypass -File scripts\check-ef-drift.ps1
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','docs/ef-model-drift-check.md','scripts/check-ef-drift.ps1','scripts/check-validation-safety.sh','backend/tests/VpnPlatform.UnitTests/EfModelDriftTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `EfModelDriftTests`: 2/2 пройдено.
- PowerShell EF drift-check: `[OK] EF model has no pending migration changes.`
- Bash safety-check: не запускался в текущей Windows-среде, потому что доступный `bash` пытается стартовать WSL без `/bin/bash`; покрытие Linux-скрипта проверено статическим .NET acceptance-тестом и остается для GitHub/Linux среды.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 368/368 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-ef-drift-powershell-gate`, версия `0.68.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: `/health/live`, `/health/ready`, `/metrics`, `/api/auth/login`, `/api/app-version/latest`; `live=ok`, `ready=Ready`, `readyChecks=2`, latest release `2026-06-12-ef-drift-powershell-gate`, версия `0.68.0`, metrics содержат `vpnplatform_http_requests_total` и `vpnplatform_api_uptime_seconds`.

## Проверка 2026-06-12: observability API

Что проверено:

- Закрыт roadmap-пункт `P4-BE-006` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `CorrelationIdMiddleware` нормализует входящий `X-Correlation-Id`, возвращает его в ответе, добавляет в `HttpContext.Items`, `Activity` tag и logger scope.
- Добавлен `RequestObservabilityMiddleware`: каждый HTTP-запрос пишет структурный лог с методом, путем, статусом, временем выполнения и correlation id.
- Endpoint `/health/live` возвращает service, environment, uptime и correlation id.
- Endpoint `/health/ready` проверяет локальную БД и возвращает счетчики пользователей, активных тарифов, включенных платежных провайдеров, pending outbox, failed provisioning и unhealthy VPN-нод.
- Endpoint `/metrics` возвращает Prometheus text format с `vpnplatform_api_info`, `vpnplatform_api_uptime_seconds`, `vpnplatform_http_requests_in_flight`, `vpnplatform_http_requests_total`, `vpnplatform_http_request_duration_ms_sum`.
- Добавлен release entry `2026-06-12-observability-mvp` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet build backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --no-restore
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --no-restore --filter ObservabilityMvpTests --logger "console;verbosity=minimal"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data.at(-1);console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','backend/src/VpnPlatform.Api/Middleware/CorrelationIdMiddleware.cs','backend/src/VpnPlatform.Api/Middleware/RequestObservabilityMiddleware.cs','backend/src/VpnPlatform.Api/Observability/ApiObservabilityMetrics.cs','backend/src/VpnPlatform.Api/Observability/ObservabilityHealthService.cs','backend/tests/VpnPlatform.UnitTests/ObservabilityMvpTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- API build: пройдено без предупреждений и ошибок.
- `ObservabilityMvpTests`: 3/3 пройдено.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 367/367 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-observability-mvp`, версия `0.67.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: `/health/live`, `/health/ready`, `/metrics`, `/api/auth/login`, `/api/app-version/latest`; `live=ok`, `ready=Ready`, `readyChecks=2`, latest release `2026-06-12-observability-mvp`, версия `0.67.0`, metrics содержат `vpnplatform_http_requests_total` и `vpnplatform_api_uptime_seconds`.

## Проверка 2026-06-12: журнал аудита в админке

Что проверено:

- Закрыт roadmap-пункт `P4-BE-005` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен backend endpoint `/api/admin/audit-logs` с фильтрами `action`, `entityType`, `actorType`, `search`, `from`, `to`, `limit`.
- В админке добавлен раздел `Аудит` с фильтрами и просмотром `beforeJson/afterJson`.
- Действия с платежными провайдерами пишут audit-события `payment_provider.create`, `payment_provider.update`, `payment_provider.enabled.set`, `payment_provider.check`.
- Ротация SecretKey/webhook secret пишется отдельным событием `payment_provider.secret.rotate` без раскрытия секретных значений.
- Переходы статусов платежей из webhook/recheck пишутся как системные события `payment.status.changed`.
- Добавлен release entry `2026-06-12-admin-audit-log` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~AuditLogMvpTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
npm run typecheck
npm test
npm run build
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data[data.length-1];console.log(data.length,last.releaseId,last.version,last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `AuditLogMvpTests`: 3/3 пройдено.
- Backend full suite: 364/364 пройдено.
- Frontend typecheck: пройдено для public-web, cabinet и admin-panel.
- Frontend tests: 61/61 пройдено.
- Frontend build: public-web, cabinet и admin-panel собраны успешно.
- App releases JSON: валиден, последний релиз `2026-06-12-admin-audit-log`, версия `0.66.0`.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/admin/releases?search=2026-06-12-admin-audit-log`, `/api/admin/audit-logs?limit=20`, `/api/admin/audit-logs?action=auth.login&limit=20`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-12-admin-audit-log`, версия `0.66.0`, audit `1`, audit search `1`, публичные провайдеры `8`, публичные тарифы `3`.

## Проверка 2026-06-12: конкурентная обработка оплаты

Что проверено:

- Закрыт roadmap-пункт `P4-BE-003` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `PaymentOrchestrator` получил общий `PaymentProcessingGate` по заказу и свежий DB snapshot внутри gate, чтобы параллельные webhook/recheck не запускали повторную активацию.
- Конкурентная вставка одного `PaymentWebhookEvent` теперь возвращает идемпотентный ответ, если уникальное событие уже сохранено другим потоком.
- Sandbox-выбор VPN-ноды больше не сортирует `decimal` в SQL, поэтому локальная SQLite-БД проходит активацию оплаты.
- Добавлен release entry `2026-06-12-payment-concurrency-guard` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~PaymentConcurrencyTests"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~PaymentWebhookProcessingTests|FullyQualifiedName~PaymentWebhookIdempotencyContractTests|FullyQualifiedName~PaymentConcurrencyTests"
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ReleaseDocumentationGuardTests|AppReleaseSeedServiceTests|AppVersionControllerTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data[data.length-1];console.log(data.length,last.releaseId,last.version,last.title);"
node -e "const fs=require('fs'); const files=['backend/src/VpnPlatform.Api/AppReleases/releases.json','docs/PRODUCT_COMPLETION_ROADMAP.md','TEST_RESULTS.md','backend/src/VpnPlatform.Application/Common/PaymentProcessingGate.cs','backend/src/VpnPlatform.Application/Services/PaymentOrchestrator.cs','backend/src/VpnPlatform.Application/Services/NodeAllocationService.cs','backend/tests/VpnPlatform.UnitTests/PaymentConcurrencyTests.cs']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd))) throw new Error('U+FFFD in '+file); } const data=JSON.parse(fs.readFileSync('backend/src/VpnPlatform.Api/AppReleases/releases.json','utf8')); const last=data.at(-1); console.log('encoding guard ok', last.releaseId, last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- `PaymentConcurrencyTests`: 2/2 пройдено на SQLite.
- Targeted payment tests: 28/28 пройдено.
- Release documentation tests: 14/14 пройдено.
- Backend full suite: 361/361 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-12-payment-concurrency-guard`, версия `0.65.0`.
- Encoding guard: измененные файлы читаются как UTF-8, `U+FFFD` не найден.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/admin/releases?search=2026-06-12-payment-concurrency-guard`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-12-payment-concurrency-guard`, версия `0.65.0`, история `50`, поиск релиза `1`, публичные провайдеры `8`, публичные тарифы `3`.

## Проверка 2026-06-11: идемпотентность платежных webhook

Что проверено:

- Закрыт roadmap-пункт `P4-BE-002` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- `PaymentOrchestrator` теперь нормализует webhook event id: если провайдер не прислал внешний event id, используется стабильный ключ `payload:<sha256>`.
- Добавлены contract-тесты идемпотентности для всех значений `PaymentProvider`: YooMoney, YooKassa, RoboKassa, TelegramStars, CloudPayments, TBankAcquiring, Prodamus, Stripe и PayPal.
- Повторный webhook не создает вторую подписку, второй VPN-доступ или второй `PaymentWebhookEvent`.
- Добавлен release entry `2026-06-11-payment-webhook-idempotency` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --no-restore --filter "PaymentWebhookIdempotencyContractTests|PaymentWebhookProcessingTests|YooMoneyWebhookProcessingTests|RoboKassaWebhookProcessingTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data[data.length-1];console.log(data.length,last.releaseId,last.version,last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- Targeted payment webhook tests: 42/42 пройдено.
- Backend full suite: 359/359 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-11-payment-webhook-idempotency`, версия `0.64.0`.
- Contract-тесты: повтор webhook с внешним event id и без него идемпотентен для каждого `PaymentProvider`.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/admin/releases?search=2026-06-11-payment-webhook-idempotency`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-11-payment-webhook-idempotency`, версия `0.64.0`.

## Проверка 2026-06-11: state machines доменных статусов

Что проверено:

- Закрыт roadmap-пункт `P4-BE-001` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- Добавлен общий `StatusStateMachine` для заказов, платежей, подписок, VPN-доступов и provisioning runs.
- Guard подключен к платежному оркестратору, Telegram Stars successful payment flow, подпискам, lifecycle VPN-доступа, X3-UI синхронизации, админским действиям и provisioning worker.
- Добавлен release entry `2026-06-11-state-machine-guards` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- Добавлены unit-тесты матрицы разрешенных/запрещенных переходов и интеграционный тест позднего cancelled-webhook после successful payment.

Команды и результат:

```powershell
dotnet build backend\VpnPlatform.sln --configuration Release --no-restore
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs');const p='backend/src/VpnPlatform.Api/AppReleases/releases.json';const data=JSON.parse(fs.readFileSync(p,'utf8'));const last=data[data.length-1];console.log(data.length,last.releaseId,last.version,last.title);"
dotnet run --project backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj --configuration Release --no-build
```

Результат:

- Backend build: успешно, 0 warnings, 0 errors.
- Backend full suite: 341/341 пройдено.
- App releases JSON: валиден, последний релиз `2026-06-11-state-machine-guards`, версия `0.63.0`.
- State machine unit tests: разрешают рабочие переходы и запрещают невозможные откаты для `OrderStatus`, `PaymentStatus`, `SubscriptionStatus`, `AccessCredentialStatus`, `ProvisioningRunStatus`.
- Webhook integration test: поздний `payment.canceled` после `payment.succeeded` получает failed processing и не откатывает `PaymentStatus.Succeeded`, `OrderStatus.Completed` и созданную подписку.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/latest`, `/api/app-version/history`, `/api/app-version/admin/releases?search=2026-06-11-state-machine-guards`, `/api/app-version/admin/releases/overview`, `/api/public/payments/providers`, `/api/public/tariffs`; latest release `2026-06-11-state-machine-guards`, версия `0.63.0`, публичные провайдеры `8`, публичные тарифы `3`, поиск релиза `1`.

## Проверка 2026-06-11: русская локализация интерфейса

Что проверено:

- Закрыт roadmap-пункт `P3-UX-007` в `docs/PRODUCT_COMPLETION_ROADMAP.md`.
- API-клиент больше не использует англоязычные `Failed to ...` fallback-сообщения для пользовательских ошибок.
- В админке локализованы подписи платежных провайдеров, Telegram-бота, серверов, VPN-панелей, источников релизов и режимов выдачи.
- В `@vpn-platform/ui` локализованы бейджи `agent`, `manual`, `auto`, `hybrid`, `LongPolling`.
- Добавлен release entry `2026-06-11-russian-localization-check` в `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

Команды и результат:

```powershell
node -e "const fs=require('fs'); const p='backend/src/VpnPlatform.Api/AppReleases/releases.json'; const data=JSON.parse(fs.readFileSync(p,'utf8')); console.log(data.length, data[data.length-1].releaseId, data[data.length-1].version);"
cd frontend
npm test
npm run typecheck
npm run build
cd ..
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
node -e "const fs=require('fs'); const files=['frontend/apps/admin-panel/src/App.tsx','frontend/apps/cabinet/src/App.tsx','frontend/apps/public-web/src/App.tsx','frontend/packages/ui/src/index.tsx','frontend/packages/api-client/src/index.ts','frontend/tests/api-client.test.ts']; for (const file of files) { const text=fs.readFileSync(file,'utf8'); if (text.includes(String.fromCharCode(0xfffd)) || /\?{3,}/.test(text)) throw new Error(file); } console.log('encoding guard ok');"
```

Результат:

- App releases JSON: валиден, последний релиз `2026-06-11-russian-localization-check`, версия `0.62.0`.
- Frontend tests: 60/60 пройдено.
- Frontend typecheck: пройден для public-web, cabinet, admin-panel.
- Frontend build: public-web, cabinet, admin-panel собраны успешно.
- Backend full suite: 301/301 пройдено.
- Local SQLite HTTP-smoke: `/health/live`, `/api/auth/login`, `/api/app-version/mark-seen`, `/api/app-version/admin/releases/overview`, `/api/app-version/admin/releases?search=2026-06-11-russian-localization-check`, `/api/public/payments/providers`, `/api/public/tariffs`; релиз `2026-06-11-russian-localization-check`, версия `0.62.0`, `mark-seen=true`, публичные провайдеры `8`, публичные тарифы `3`.
- Browser smoke: public `http://127.0.0.1:19173`, cabinet `http://127.0.0.1:19174`, admin `http://127.0.0.1:19175/#payments` и `#bot`; runtime-ошибок в консоли нет, признаков битой кодировки и `Failed to ...` в видимом тексте нет.
- Encoding guard: в пользовательских frontend-источниках нет символа `U+FFFD` и трех вопросительных знаков подряд.

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
