# VPS production smoke

Документ закрывает техническую часть roadmap-пункта `P11-ACC-002`: полный smoke-сценарий после deploy теперь воспроизводится отдельным PowerShell-скриптом.

## Что проверяет скрипт

Файл: `scripts/vps-production-smoke.ps1`.

Для фиксации результата live/staging smoke используется проверяемый отчет:

- шаблон: `docs/vps-production-smoke-report.template.json`;
- генератор безопасной заготовки: `scripts/new-vps-production-smoke-report.ps1`;
- validator: `scripts/validate-vps-production-smoke-report.ps1`.

Шаблон намеренно создается в состоянии `blocked`. Полный отчет можно считать приемочным доказательством только после ручного или CI-прогона на реальном VPS/staging, заполнения всех шагов безопасными evidence-строками и успешного запуска validator с `-RequireAllPassed`. В отчет нельзя добавлять пароли, cookies, authorization headers, provider secrets, raw webhook payloads и полные VPN-ссылки `vless://`, `vmess://`, `trojan://`.

Проверяемый путь:

1. API `/health/live`;
2. API `/health/ready`;
3. optional frontend checks для public/cabinet/admin web URLs;
4. optional admin login через `/api/auth/login`;
5. admin dashboard `/api/admin/dashboard/summary`;
6. публичные тарифы `/api/public/tariffs`;
7. публичные payment providers `/api/public/payments/providers`;
8. public checkout session `/api/public/checkout-sessions`;
9. регистрация smoke-пользователя `/api/auth/register`;
10. claim checkout session `/api/me/checkout-sessions/{token}/claim`;
11. payment init `/api/me/orders/{id}/payments/{provider}/init`;
12. при явном `-AllowSandboxWebhook` sandbox webhook `/api/webhooks/payments/yookassa`;
13. кабинетные `/api/me/orders`, `/api/me/payments`, `/api/me/subscriptions`, `/api/me/accesses`;
14. наличие active subscription;
15. наличие VPN access URI с `vless://`;
16. latest "Что нового" через `/api/app-version/latest`.

## Локальный dry-run на SQLite

Для локальной проверки скрипт запускается против API, поднятого на чистой SQLite-БД. Это не live production smoke, но проверяет тот же HTTP flow.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 `
  -ApiBaseUrl http://127.0.0.1:18101 `
  -AdminEmail fresh-admin@example.test `
  -AdminPassword LocalSmokePassword123! `
  -AllowSandboxWebhook
```

`-AllowSandboxWebhook` разрешен только для non-Production окружения. Если `/health/live` сообщает `Production`, скрипт останавливается до fake webhook.

## Запуск после deploy на VPS

Минимальный smoke без fake webhook:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vps-production-smoke.ps1 `
  -ApiBaseUrl http://83.147.222.145:8080 `
  -PublicWebUrl http://83.147.222.145 `
  -CabinetWebUrl http://83.147.222.145:5174 `
  -AdminWebUrl http://83.147.222.145:5175 `
  -AdminEmail <admin-email> `
  -AdminPassword <admin-password> `
  -RequireWebApps
```

В таком режиме скрипт проверяет health, web apps, admin login, public checkout, register, claim и payment init. Затем он останавливается со строкой `partial ok`, потому что реальная оплата должна завершаться через внешний платежный sandbox и настоящий webhook провайдера.

Полный production smoke считается успешным только когда после реального webhook появляются:

- `Succeeded` payment;
- active subscription;
- VPN access;
- `vless://` access URI;
- запись в кабинетной истории заказов и платежей.

## Безопасность

- Не передавайте реальные пароли и tokens в commit, issue, screenshots или `TEST_RESULTS.md`.
- Если root-пароль VPS, SSH key, Telegram token, payment key или webhook secret были раскрыты в чате/логах, их нужно ротировать до production.
- Не используйте `-AllowSandboxWebhook` на Production.
- Не сохраняйте cookies, bearer tokens, `.env` и private headers как артефакты.

## Результат 2026-06-14

Локальный dry-run против чистой SQLite-БД прошел полный путь до active subscription и VPN access:

- health: OK;
- admin login: OK;
- public checkout: OK;
- user register: OK;
- claim order: OK;
- YooKassa sandbox payment init: OK;
- sandbox webhook: OK;
- active subscription: OK;
- VPN access URI: `vless://`;
- latest "Что нового": `2026-06-14-vps-production-smoke-runner`, версия `0.105.0`.

Live VPS production smoke все равно должен быть выполнен отдельно после deploy и ротации раскрытых секретов.

## Отчет о live/staging прогоне

Создать заготовку:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-vps-production-smoke-report.ps1 `
  -OutputPath tmp\vps-production-smoke-report.json `
  -ApiBaseUrl https://api.example.test `
  -PublicWebUrl https://example.test `
  -CabinetWebUrl https://example.test/cabinet `
  -AdminWebUrl https://example.test/admin `
  -EnvironmentName staging
```

Проверить заполненный отчет:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-vps-production-smoke-report.ps1 `
  -ReportPath tmp\vps-production-smoke-report.json `
  -RequireAllPassed
```

Если хотя бы один шаг остался `blocked`, `failed`, не заполнено boolean-подтверждение или в evidence попал секретный маркер, validator завершится ошибкой.
