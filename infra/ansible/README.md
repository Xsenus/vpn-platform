# Provisioning через Ansible

Этот каталог содержит playbook'и для precheck и автоподготовки VPN-ноды.

## Что делает pipeline

- генерирует одноразовый inventory под конкретную ноду;
- выполняет precheck (`precheck-node.yml`) или provisioning (`provision-node.yml`);
- ставит базовые пакеты и подготавливает каталоги платформы;
- может запустить официальный install script 3x-ui;
- сохраняет metadata-файл ноды в `/opt/vpnplatform/provisioning/node-meta.json`.

## Запуск вручную

```bash
python3 infra/ansible/runner/run_playbook.py \
  --playbook infra/ansible/playbooks/precheck-node.yml \
  --host 203.0.113.10 \
  --ssh-user root \
  --ssh-port 22 \
  --private-key-path ~/.ssh/id_ed25519 \
  --workdir /tmp/vpnplatform-precheck \
  --skip-host-key-checking
```

Для полноценного provisioning замените playbook на `provision-node.yml` и передайте `--extra-vars-file` с параметрами панели.

## Требования

- Ansible
- Python 3
- SSH-доступ к целевой ноде
- Debian/Ubuntu на целевой ноде

## Важно

На production секреты узлов и панелей должны храниться вне БД, в vault/secret store. В этом репозитории используются поля и переменные, достаточные для dev/staging-потока и интеграционного запуска.
