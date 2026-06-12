# Статус: безопасное хранение provisioning-секретов

Stage 6 introduced an Own VPS provisioning MVP. It keeps validation mode safe and redacts SSH credentials from API responses, admin UI, logs, audit entries, Telegram messages and Telegram update payloads.

Stage P6-SEC-001 adds production-oriented temporary materialization for protected SSH private keys. The current supported live credential path is `ssh_key`; password-based live SSH remains fail-closed until the runner can pass passwords without writing them into inventory/logs.

## Current MVP behaviour

- Telegram credentials are never echoed back to the user.
- Telegram inbound message text is stored as `[redacted provisioning credential]` while the bot is waiting for SSH credentials.
- Telegram raw update payload is redacted before persistence when the current session state is credential input.
- Admin API returns only:
  - `sshAuthMethod`;
  - `sshCredentialConfigured`;
  - host/port/username.
- Admin API does not return `SshPrivateKeyPath`, raw SSH password or raw private key.
- Validation mode stores either an `ISecretProtector` protected value or a deterministic `validation-placeholder:*` marker.
- Live Ansible execution is disabled by default with:
  - `Provisioning__LiveExecutionEnabled=false`;
  - `Provisioning__AllowLiveDeploy=false`.
- Protected `ssh_key` credentials can be materialized to a temporary key file for live Ansible only inside `AnsibleProvisioningExecutor`, after the explicit live provisioning gates are enabled. The file is deleted in `finally`.
- Protected password credentials, validation placeholders and legacy protected values in `SshPrivateKeyPath` are not materialized.

## Production hardening plan

1. Move SSH credentials out of `VpnNode.SshPrivateKeyPath` into a dedicated encrypted `ProvisioningSecret` table or external secret manager.
2. Store only a secret reference on `VpnNode` / `ProvisioningRun`.
3. Add per-secret purpose, owner, expiry/TTL and rotation metadata.
4. Materialize private keys only into temporary files with strict permissions when live provisioning is explicitly enabled.
5. Delete temporary files immediately after Ansible execution.
6. Add envelope encryption with key rotation support and audit events for decrypt/read operations.
7. Add automated tests proving credentials never appear in:
   - API responses;
   - admin UI;
   - audit logs;
   - provisioning logs;
   - Telegram messages;
   - worker logs.
8. Add staging runbook for live precheck/deploy with explicit approval gates.
9. Add admin UI for credential replacement/rotation without ever revealing the old value.
10. Add a data migration that removes legacy plaintext/path-style values or marks them as operator-managed paths.

Until those items are complete, Own VPS provisioning must remain validation/dry-run by default and must not be treated as production-ready.

## Stage 7 update

Stage 7 adds backward-compatible protected fields on `VpnNode`:

- `ProtectedSshCredential`;
- `SshCredentialRef`;
- `ProtectedPanelPassword`;
- `PanelSecretRef`.

New admin/Telegram provisioning writes should use these protected fields instead of storing credentials in `SshPrivateKeyPath` or `PanelPassword`. Legacy fields remain in the database only for compatibility and approved operator-managed paths.

Live Ansible must not receive protected credentials directly. `ProvisioningSecretMaterializer` now decrypts only `ssh_key` payloads into `WorkingDirectory/<runId>/secrets`, sets best-effort restrictive permissions and deletes the file after the runner exits. A future stage should add an external secret manager and a safe password-based runner path if password SSH is required.
