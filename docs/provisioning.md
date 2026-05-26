# Конвейер provisioning

## Что реализовано

В репозитории есть end-to-end skeleton для автоподготовки нод:

1. Админ создает ноду через `POST /api/admin/servers`.
2. Админ запускает precheck или provisioning через:
   - `POST /api/admin/servers/{id}/precheck`
   - `POST /api/admin/servers/{id}/provision`
3. Backend создает `ProvisioningRun` со статусом `Pending`.
4. `ProvisioningWorker`, запущенный как hosted service внутри API, подхватывает run из БД.
5. `AnsibleProvisioningExecutor` вызывает Python runner.
6. Python runner генерирует inventory и запускает `ansible-playbook`.
7. Результат, stdout/stderr и шаги сохраняются в `ProvisioningRun` / `ProvisioningStepRun`.

## Ключевые файлы

- `backend/src/VpnPlatform.Application/Services/ProvisioningService.cs`
- `backend/src/VpnPlatform.Infrastructure/HostedServices/ProvisioningWorker.cs`
- `backend/src/VpnPlatform.Infrastructure/Provisioning/AnsibleProvisioningExecutor.cs`
- `infra/ansible/runner/run_playbook.py`
- `infra/ansible/playbooks/precheck-node.yml`
- `infra/ansible/playbooks/provision-node.yml`

## Какие данные нужны на ноде

Минимум:

- `host`
- `ipAddress`
- `sshUser`
- `sshPort`
- `sshPrivateKeyPath`

Опционально для 3x-ui и выдачи доступов:

- `panelBaseUrl`
- `panelUsername`
- `panelPassword`
- `panelInboundId`
- `publicHostname`
- `publicPort`

## Важное ограничение

Секреты панели и SSH-ключи в production должны выноситься в vault/secret store. В текущем delivery-пакете сохранена dev/staging-friendly модель, чтобы можно было запустить flow без отдельной secret platform.
