# Live Ansible credentials

Документ описывает безопасную передачу SSH-ключа в live Ansible provisioning.

## Цель

Live provisioning не должен хранить raw SSH key в БД, API, UI, audit log, execution log или runner output. Ansible получает только временный путь к ключу, а сам файл удаляется после завершения runner.

## Поток выполнения

1. Админ или Telegram own VPS flow сохраняет SSH credential в `VpnNode.ProtectedSshCredential`.
2. `ProtectedSshCredential` шифруется через `ISecretProtector`.
3. `ProvisioningService` разрешает live deploy только после явного provisioning guard-а.
4. `AnsibleProvisioningExecutor` при live execution вызывает `ProvisioningSecretMaterializer`.
5. `ProvisioningSecretMaterializer` расшифровывает только `ssh-auth:ssh_key`.
6. Ключ пишется во временный файл `WorkingDirectory/<runId>/secrets/ssh-key-*`.
7. Runner получает только `--private-key-path <temporary-path>`.
8. После выполнения runner удаляются `extra-vars.json`, временный ключ и пустая директория `secrets`.

## Redaction

Перед сохранением runner output и stderr executor редактирует:

- raw panel password;
- protected panel password;
- legacy `SshPrivateKeyPath`;
- protected SSH payload;
- plaintext SSH private key;
- temporary SSH key path;
- temporary `secrets` directory path.

Это защищает execution log, step output, stderr warning logs и error text, даже если runner случайно напечатает аргументы запуска.

## Ограничения

- Password-based live SSH остаётся заблокированным.
- `validation-placeholder:*` не материализуется.
- Protected payload в legacy поле `SshPrivateKeyPath` не передаётся runner.
- Если задан `SshCredentialRef`, но отсутствует `ProtectedSshCredential`, запуск fail-closed.

## Проверка

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "ProvisioningSecretMaterializerTests|OwnVpsProvisioningMvpTests"
```

Ключевой тест: `Ansible_Runner_Redaction_Should_Cover_Temporary_Key_Path_And_Plaintext`.
