# Smoke-проверка админки на VPS

Этот документ описывает безопасную проверку production/staging админки под реальным admin-аккаунтом. Локальные Playwright-тесты проверяют интерфейс на моках и локальном API, но не доказывают, что `/admin/` на VPS доступен, авторизация работает, а каждый раздел открывается без белого экрана, JS-ошибок и 401/403 после входа.

## Черновик отчета

Создайте fail-closed отчет перед ручной или браузерной проверкой:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\new-admin-vps-smoke-report.ps1 -OutputPath tmp\admin-vps-smoke-report.json -ApiBaseUrl https://api.example.test -AdminWebUrl https://example.test/admin/ -EnvironmentName staging -Operator local-test
```

Скрипт берет `docs/admin-vps-smoke-report.template.json`, подставляет latest release из раздела "Что нового", URL окружения и оператора, выставляет все разделы в `blocked`, не перезаписывает существующий файл без `-Force` и сразу запускает валидатор.

## Что проверять

Перед отметкой `passed` нужно подтвердить:

- production admin-аккаунт создан или восстановлен безопасным bootstrap/reset механизмом;
- страница админки открывается по HTTPS;
- логин проходит под рабочим admin-аккаунтом;
- после логина нет 401/403 на admin API;
- browser console не содержит `console.error` и `pageerror`;
- каждый раздел открывается и показывает осмысленное состояние.

Обязательные разделы:

- `dashboard`
- `users`
- `payments`
- `tariffs`
- `subscriptions`
- `vpn`
- `nodes`
- `panels`
- `support`
- `audit`
- `bot`
- `releases`
- `faq`
- `content`
- `scenarios`
- `provisioning`

## Валидатор

Обычная структурная проверка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-report.ps1 -ReportPath tmp\admin-vps-smoke-report.json
```

Production gate для заполненного отчета:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-admin-vps-smoke-report.ps1 -ReportPath tmp\admin-vps-smoke-report.json -RequireAllPassed
```

`-RequireAllPassed` должен падать на черновике, потому что все разделы изначально находятся в статусе `blocked`. Перед закрытием `P0-ADMIN-002` нужно заменить статусы на `passed`, выставить `loaded=true`, `httpStatus=200`, заполнить общие флаги и приложить безопасные evidence.

## Что нельзя хранить

В отчете нельзя сохранять пароли, cookies, bearer-токены, private headers, `.env`, SSH-ключи, webhook secrets, raw provider payloads и скриншоты, где видны секреты.
