# Fresh local smoke без Docker

Документ закрывает roadmap-пункт `P11-ACC-001` и описывает приемочную проверку чистого локального запуска на SQLite.

## Что проверяет smoke

Скрипт `scripts/fresh-local-smoke.ps1` поднимает только backend API на отдельном локальном порту и создает временную SQLite-БД в `tmp/fresh-local-smoke`.

Проверяемый путь:

1. старт API в окружении `Local`;
2. `Database__UseEnsureCreatedForLocalSqlite=true`;
3. `Database__SeedDemoData=true`;
4. `/health/live` и `/health/ready`;
5. публичные тарифы `/api/public/tariffs`;
6. публичные sandbox-провайдеры `/api/public/payments/providers`;
7. создание public checkout session;
8. регистрация нового пользователя;
9. claim checkout session в личном кабинете;
10. инициализация YooKassa sandbox payment;
11. local sandbox webhook `payment.succeeded` с заголовком `X-YooKassa-Sandbox-Webhook=true`;
12. проверка истории заказов и платежей в кабинете;
13. проверка активной подписки;
14. проверка созданного VPN-доступа с `vless://` URI;
15. проверка `/api/app-version/latest`.

Скрипт не ходит во внешние платежные системы и не подключается к реальному 3x-ui. Вся выдача VPN выполняется через local sandbox-провайдер.

## Запуск

```powershell
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1
```

Если порт `18101` занят:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -ApiPort 18111
```

Для диагностики можно сохранить временную БД и логи:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\fresh-local-smoke.ps1 -KeepArtifacts
```

После успешного запуска скрипт выводит строку вида:

```text
fresh local smoke ok live=ok ready=Ready tariffs=3 providers=8 order=<id> payment=<id> subscription=<id> access=<id> latest=<releaseId>
```

## Безопасность

- Скрипт удаляет только папку `tmp/fresh-local-smoke` внутри текущего репозитория.
- Перед удалением путь проверяется на принадлежность workspace.
- Все переменные окружения задаются только для текущего процесса PowerShell и восстанавливаются в `finally`.
- API-процесс всегда останавливается в `finally`.
- Local sandbox webhook запрещен в Production startup safety и используется только в окружении `Local`.

## Что считать успешным результатом

Проверка считается успешной, если:

- API поднялся на чистой SQLite-БД;
- seed создал тарифы и sandbox-провайдеры;
- пользователь прошел checkout flow;
- webhook перевел платеж в `Succeeded`;
- платеж обработал активацию;
- появилась активная подписка;
- появился VPN-доступ;
- временные файлы удалены, если не указан `-KeepArtifacts`.
