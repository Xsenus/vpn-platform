# Staging smoke checklist

Документ закрывает техническую часть roadmap-пункта `P9-TST-007`: теперь staging smoke имеет единый чеклист, JSON-шаблон отчета и валидатор, который не дает принять пустой или небезопасный отчет.

Важно: сам факт наличия чеклиста не делает проект production-ready. Для production-ready нужен заполненный отчет с реального staging/VPS окружения, где все обязательные пункты имеют статус `passed`.

## Что проверяется

Обязательный staging smoke должен покрывать:

- deploy: успешный GitHub Actions deploy или ручной deploy с логом;
- health: `/health/live` со статусом `ok` и `/health/ready` со статусом `Ready`;
- public web, cabinet web и admin web без белых экранов;
- admin login и загрузку dashboard summary;
- публичные тарифы и включенные готовые способы оплаты;
- checkout session, payment init и подтверждение оплаты через sandbox или безопасный тест провайдера;
- активную подписку после подтверждения оплаты;
- VPN access с `vless://`, `vmess://` или `trojan://` и QR-кодом;
- обращение в поддержку от пользователя и ответ администратора;
- отсутствие `console.error` и `pageerror` в браузере;
- ротацию раскрытых секретов перед live/staging прогоном;
- отсутствие паролей, bearer-токенов, cookies, private keys и webhook secrets в отчете.

Ключевые security check id в JSON-отчете: `secret-rotation` и `no-secret-leak`.

## Как заполнить отчет

1. Скопируйте `docs/staging-smoke-report.template.json` в отдельный файл отчета, например `tmp/staging-smoke-report.json`.
2. Заполните адреса API, публичного сайта, кабинета и админки.
3. Пройдите каждый пункт smoke и замените `blocked` на `passed`, `failed` или `skipped`.
4. В поле `evidence` оставляйте только безопасные доказательства: URL GitHub Actions run, sanitized curl output, номер заказа, provider payment id, id подписки, id VPN access, скриншот без секретов.
5. Не вставляйте в отчет пароли, токены, cookies, private keys, webhook secrets и приватные headers.

## Валидация отчета

Структурная проверка, полезная во время заполнения:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-staging-smoke-report.ps1 -ReportPath tmp\staging-smoke-report.json
```

Release-gate проверка, которую нужно использовать перед тем, как считать staging smoke зеленым:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-staging-smoke-report.ps1 -ReportPath tmp\staging-smoke-report.json -RequireAllPassed
```

Если в отчете есть `blocked`, `failed` или `skipped`, команда с `-RequireAllPassed` завершится ошибкой. Это намеренное fail-closed поведение.

Валидатор также ищет типовые признаки утечки секретов в любом поле отчета: `Authorization:`, `Bearer`, `Cookie:`, `Set-Cookie:`, `.env`, `client_secret`, `api_key`, `private header`, `x-api-key`, `X-Telegram-Bot-Api-Secret-Token`, `PRODUCTION_ENV_FILE`, `VPS_SSH_KEY`, private keys и webhook secrets. Если такой маркер найден, отчет считается небезопасным и не проходит проверку.

## Связь с автоматическими smoke

Для API/VPS сценария используйте:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 -ApiBaseUrl https://api.example.test -PublicWebUrl https://example.test/ -CabinetWebUrl https://example.test/cabinet/ -AdminWebUrl https://example.test/admin/ -AdminEmail admin@example.test -AdminPassword "<from-secret-manager>" -RequireWebApps
```

На non-Production staging можно добавить `-AllowSandboxWebhook`, если используется локальный/sandbox webhook без реальных денег. На Production этот режим запрещен самим runner-ом.

## Результат 2026-06-14

- Добавлен `scripts/validate-staging-smoke-report.ps1`.
- Добавлен шаблон `docs/staging-smoke-report.template.json`.
- Добавлены guard-тесты `StagingSmokeChecklistTests`.
- Валидатор расширен sanitizer-маркерами для cookies, `.env`, client secrets, API keys, private headers, Telegram secret header и GitHub/VPS secret names.
- `P9-TST-007` получил воспроизводимый чеклист и валидатор.
- Реальный live/staging smoke report пока не заполнен, поэтому внешние блокеры `P0-*`, `P11-ACC-002` и production-ready статус остаются открытыми.
