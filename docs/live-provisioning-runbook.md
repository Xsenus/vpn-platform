# Live provisioning VPS

Этот runbook описывает безопасный порядок включения настоящего SSH/Ansible provisioning для staging или production VPS. По умолчанию проект работает в безопасном режиме: `Provisioning__LiveExecutionEnabled=false` и `Provisioning__AllowLiveDeploy=false`.

## Когда можно использовать

Live provisioning разрешается только для одобренного VPS, на котором уже прошел dry-run precheck и понятны последствия Ansible-действий.

Минимальные условия:

- VPS на Debian/Ubuntu с доступом по SSH.
- У оператора есть root или sudo-доступ.
- SSH credential сохранен в платформе как protected `ssh_key`.
- Password-based live SSH не используется: текущий runner материализует только private key.
- Legacy `SshPrivateKeyPath` может содержать только абсолютный Unix path к уже смонтированному key-файлу без control/quote-символов; raw key, protected marker и validation placeholder API отклоняет до записи.
- Сервер не является production-сервером клиента без отдельного согласования.
- В БД есть актуальный `VpnNode` с корректными `Host` или `IpAddress`, `SshUser`, `SshPort`, `PublicHostname`, `PublicPort`.
- Для live deploy у ноды есть тег `explicit-live-provisioning:true`.

## Preflight на сервере API

Проверить версии:

```bash
dotnet --version
python3 --version
ansible-playbook --version
ssh -V
```

Проверить доступность playbook:

```bash
test -f infra/ansible/playbooks/precheck-node.yml
test -f infra/ansible/playbooks/provision-node.yml
test -f infra/ansible/runner/run_playbook.py
```

Проверить syntax Ansible:

```bash
tmp_dir="$(mktemp -d)"
cat > "$tmp_dir/inventory.ini" <<'EOF'
[vpn_nodes]
syntax ansible_host=127.0.0.1 ansible_user=root ansible_port=22 ansible_connection=local ansible_python_interpreter=/usr/bin/python3
EOF
ansible-playbook --syntax-check -i "$tmp_dir/inventory.ini" infra/ansible/playbooks/precheck-node.yml
ansible-playbook --syntax-check -i "$tmp_dir/inventory.ini" infra/ansible/playbooks/provision-node.yml
rm -rf "$tmp_dir"
```

## SSH и known_hosts

Для staging/production предпочтительно не использовать `SkipHostKeyChecking=true`.

Подготовить known_hosts:

```bash
mkdir -p /var/lib/vpnplatform
ssh-keyscan -p 22 example-vps.example.com >> /var/lib/vpnplatform/known_hosts
chmod 0644 /var/lib/vpnplatform/known_hosts
```

В production env указать:

```bash
Provisioning__KnownHostsPath=/var/lib/vpnplatform/known_hosts
```

Если используется `SkipHostKeyChecking=true`, это должно быть временное решение для одноразового staging-run, а не production default.

## Включение live flags

Live execution состоит из двух независимых флагов:

```bash
Provisioning__LiveExecutionEnabled=true
Provisioning__AllowLiveDeploy=true
```

Роли флагов:

- `Provisioning__LiveExecutionEnabled=true` разрешает backend запускать Python/Ansible runner вместо mock executor.
- `Provisioning__AllowLiveDeploy=true` разрешает deploy-run, который реально меняет VPS.

Для одного dry-run precheck достаточно `Provisioning__LiveExecutionEnabled=true`; deploy все равно останется заблокированным, пока `Provisioning__AllowLiveDeploy=false`.

## Теги ноды

Перед настоящим deploy у ноды должны быть явные теги:

```text
validation-mode:false
explicit-live-provisioning:true
```

Назначение:

- `validation-mode:false` переводит ноду из validation deploy в live candidate.
- `explicit-live-provisioning:true` подтверждает, что оператор одобрил live deploy именно этой ноды.

Без `explicit-live-provisioning:true` backend вернет `live-deploy-blocked`.

## Порядок запуска через API

1. Создать или обновить сервер в админке.
2. Убедиться, что protected SSH key задан и не возвращается в API. Queue дополнительно проверит наличие payload и поддерживаемый `ssh_key` auth type до создания run.
3. Запустить precheck:

```http
POST /api/admin/servers/{id}/precheck
```

4. Дождаться `ReadyToDeploy`.
5. Открыть детали:

```http
GET /api/admin/provisioning-runs/{id}
```

6. Проверить шаг `Precheck report`: OS, ports, disk, RAM, firewall, Docker, systemd, 3x-ui.
7. Если отчет чистый, запустить deploy:

```http
POST /api/admin/provisioning-runs/{id}/deploy
```

8. Проверить финальный статус `Deployed`, созданную панель/inbound/access и audit.

## Ручной dry-run runner

Перед API-run можно выполнить runner вручную:

```bash
python3 infra/ansible/runner/run_playbook.py \
  --playbook infra/ansible/playbooks/precheck-node.yml \
  --host example-vps.example.com \
  --ssh-user root \
  --ssh-port 22 \
  --private-key-path /path/to/staging-key \
  --workdir /tmp/vpnplatform-precheck \
  --known-hosts-path /var/lib/vpnplatform/known_hosts \
  --check
```

Команда должна вернуть JSON с `success=true`.

## Rollback и failure path

Если deploy падает, платформа выполняет rollback состояния ноды внутри БД:

- `ProvisioningRun.Status` остается `Failed`.
- `VpnNode.ProvisioningStatus` становится `Failed`.
- эксплуатационные поля `VpnNode` возвращаются к snapshot до deploy.
- в деталях run появляется шаг `Rollback node state`.
- audit получает `provisioning.rollback_applied`.
- support context получает redacted-ошибку.

Важно: это rollback состояния платформы. Он не отменяет изменения, которые Ansible уже успел сделать на удаленном VPS. После failed deploy оператор должен вручную проверить VPS, `/opt/vpnplatform`, `ufw`, `systemctl` и состояние 3x-ui.

## Smoke после deploy

Минимальный smoke:

```bash
curl -fsS http://127.0.0.1:8080/health/live
curl -fsS http://127.0.0.1:8080/health/ready
curl -fsS http://127.0.0.1:8080/metrics | grep vpnplatform_http_requests_total
```

В админке проверить:

- сервер не в `Error`;
- provisioning run имеет `Deployed`;
- панель 3x-ui `Active` или `Healthy`;
- inbound активен;
- тестовая подписка получает реальный VPN URI;
- audit содержит `provisioning.deploy_succeeded`.

## Fail-closed правила

- `validation-mode:true` имеет приоритет над глобальными live-флагами: executor обязан вернуть mock result без запуска process/SSH/Ansible.
- Non-validation deploy при `Provisioning__LiveExecutionEnabled=false` обязан завершиться ошибкой, а не mock success.
- Не включать `Provisioning__AllowLiveDeploy=true` глобально без change window.
- Не запускать live deploy без `Precheck report`.
- Не хранить raw private key в `SshPrivateKeyPath`.
- Не использовать `validation-placeholder:*` для live Ansible.
- Не использовать production VPS для проверки платежных sandbox-сценариев.
- Не считать `Rollback node state` инфраструктурным откатом удаленного VPS.
