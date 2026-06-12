# Rollback при ошибке provisioning VPS

`P7-PROV-004` добавляет понятный откат состояния ноды при неудачном deploy. Откат выполняется внутри `ProvisioningWorker` без изменения схемы БД.

## Как работает

Перед запуском deploy worker снимает snapshot эксплуатационного состояния `VpnNode`:

- `Status`
- `HealthStatus`
- `LastHealthCheckAt`
- `IsAvailableForNewUsers`
- `InstalledVersion`
- `BackupStatus`
- `MonitoringStatus`
- `LoggingStatus`
- `Capacity` и `UsedCapacity`
- `TagsCsv`

Если executor возвращает ошибку, worker:

1. Оставляет `ProvisioningRun.Status = Failed`, чтобы оператор видел провал deploy.
2. Возвращает перечисленные поля ноды к значениям до deploy.
3. Ставит `VpnNode.ProvisioningStatus = Failed`, чтобы было видно, что последний provisioning завершился ошибкой.
4. Добавляет шаг `Rollback node state`.
5. Пишет audit-событие `provisioning.rollback_applied`.
6. Создает support context и Telegram-уведомление с отредактированным текстом ошибки.

## Что не откатывается

Откат не удаляет изменения, которые внешний Ansible уже мог успеть сделать на удаленном VPS. Это контролируемый rollback состояния платформы: API, админка и выдача доступов возвращаются в понятное состояние, а оператор получает контекст для ручной проверки сервера.

Для полного инфраструктурного rollback нужен отдельный idempotent Ansible playbook или backup/restore сценарий. Это следующий уровень hardening после MVP.

## Где смотреть результат

- `GET /api/admin/provisioning-runs/{id}`: шаг `Rollback node state`.
- `ExecutionLog`: строка `Rollback applied after deploy failure`.
- Audit logs: `provisioning.rollback_applied` и `provisioning.deploy_failed`.
- Support: обращение `Own VPS deploy failed`.

## Безопасность

Rollback использует общий redaction pipeline provisioning. Ошибки executor, step output, audit и support note не должны сохранять SSH credential, token, password или raw private key.
