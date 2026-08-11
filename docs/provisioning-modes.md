# Режимы подготовки VPS

Документ фиксирует границы между безопасной проверкой, validation-сценарием и настоящим live deploy.

## Режимы

| Режим | Когда используется | Риск | Что разрешено |
| --- | --- | --- | --- |
| `dry-run` | Кнопка `Precheck VPS`, Telegram own VPS precheck, повтор dry-run запуска | `safe` | Проверка данных и безопасный precheck без изменений на сервере |
| `validation-deploy` | Deploy на сервере с тегом `validation-mode:true` | `low` | Проверка сценария без реальных SSH/Ansible-изменений рабочей инфраструктуры |
| `live-deploy-blocked` | Deploy на сервере без `validation-mode:true` и без `explicit-live-provisioning:true` | `blocked` | Запуск запрещён backend guard-ом |
| `live-deploy` | Deploy на сервере с тегом `explicit-live-provisioning:true` | `high` | Реальные SSH/Ansible-действия на одобренном staging/production VPS |

## Правила backend

- `ProvisioningService.QueueAsync(..., dryRun: true)` всегда создаёт безопасный precheck run, если сервер валиден.
- `ProvisioningService.QueueAsync(..., dryRun: false)` блокирует live deploy по умолчанию.
- Для validation-сервера (`validation-mode:true`) deploy остаётся deterministic mock даже при глобально включённых `LiveExecutionEnabled` и `AllowLiveDeploy`; executor не создаёт workdir и не запускает process/SSH/Ansible.
- Для настоящего live deploy нужен явный тег `explicit-live-provisioning:true`.
- Non-validation deploy при `LiveExecutionEnabled=false` завершается controlled failure и никогда не помечается успешным mock deploy.
- Admin API возвращает `mode`, `modeTitle`, `riskLevel`, `liveDeployAllowed`, `nextAction` и `operatorWarning` для запусков и серверов.
- Для dry-run запусков Admin API дополнительно возвращает `deployMode*`, чтобы UI понимал, можно ли запускать следующий deploy.

## Правила UI

- В списке серверов админка показывает текущий provisioning-режим, риск, доступность live deploy и следующее действие.
- Кнопка `Precheck VPS` остаётся доступной как безопасное действие.
- Кнопка `Подготовить` блокируется, если будущий deploy имеет режим `live-deploy-blocked`.
- В разделе `Подготовка VPS` каждый run показывает режим самого запуска и отдельный режим следующего deploy.
- Кнопка `Развернуть` блокируется, если `deployMode` равен `live-deploy-blocked`.

## Как включить настоящий live deploy

1. Убедиться, что VPS является одобренным staging/production target.
2. Выполнить `Precheck VPS` и проверить результат.
3. Добавить серверу тег `explicit-live-provisioning:true`.
4. Проверить, что админка показывает `Live deploy`, риск `высокий риск` и `live deploy разрешён`.
5. Запускать deploy только после подтверждения оператора.
