# Production secret storage для provisioning

Этот этап закрывает безопасное использование защищенных SSH-секретов в Own VPS provisioning.

## Что реализовано

- SSH credential сохраняется в `VpnNode.ProtectedSshCredential` через `ISecretProtector`.
- API, админка, Telegram, audit и provisioning views продолжают показывать только признаки `configured=true` / `SshCredentialRef`.
- Live Ansible больше не получает protected payload напрямую.
- Для `ssh_key` credential добавлен `ProvisioningSecretMaterializer`: он расшифровывает ключ только на время provisioning-run, пишет его во временный файл внутри `WorkingDirectory/<runId>/secrets`, передает runner путь через `--private-key-path` и удаляет файл в `finally`.
- На Linux/Unix для директории `secrets` выставляется режим `700`, для key file - `600`. На Windows это пропускается, потому что Unix file modes недоступны.
- Password-based live SSH остается fail-closed: текущий runner не поддерживает безопасную передачу SSH password без записи секрета в inventory.
- `validation-placeholder:*` и legacy protected values в `SshPrivateKeyPath` не материализуются.
- Runner stdout/stderr и step output редактируются через `SecretRedactor` с учетом protected payload и расшифрованного plaintext.

## Когда материализация включается

Материализация происходит только в live-ветке `AnsibleProvisioningExecutor`, когда:

- `Provisioning:LiveExecutionEnabled=true`;
- для deploy-run дополнительно `Provisioning:AllowLiveDeploy=true`;
- у ноды заполнен `ProtectedSshCredential`;
- tag `ssh-auth:ssh_key`;
- protected payload имеет формат `v1:*`.

Если любое условие безопасности не выполнено, executor возвращает failed result без запуска `ansible-playbook`.

## Что остается запрещенным

- Нельзя хранить raw private key или password в `SshPrivateKeyPath`.
- Нельзя коммитить содержимое `WorkingDirectory`.
- Нельзя использовать password-based live SSH до отдельной реализации runner без утечки пароля в inventory/logs.
- Нельзя публиковать `ProtectedSshCredential`, `SshCredentialRef`, materialized path или plaintext в API/UI/logs.

## Проверка

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "ProvisioningSecretMaterializerTests|OwnVpsProvisioningMvpTests|SecurityHardeningMvpTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Ручная staging-проверка live-run должна выполняться только после backup БД и на тестовой ноде:

```bash
export Provisioning__LiveExecutionEnabled=true
export Provisioning__AllowLiveDeploy=true
export Provisioning__WorkingDirectory=/tmp/vpnplatform-provisioning
```

После run проверьте:

- файл `/tmp/vpnplatform-provisioning/<runId>/secrets/ssh-key-*` удален;
- в `ProvisioningRun.ExecutionLog`, `ProvisioningStepRuns`, API responses и application logs нет raw private key/password;
- `inventory.ini` содержит только path к удаленному key file, а не сам секрет;
- `/tmp/vpnplatform-provisioning` не попадает в backup исходного кода или git.
