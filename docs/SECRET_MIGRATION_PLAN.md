# План миграции секретов

This plan covers the Stage 7 protected-secret MVP. It is a migration/runbook for existing deployments that may already contain plaintext or path-style sensitive values.

## Scope

Secrets that must not be returned through API responses, UI, logs, audit entries or validation artifacts:

- `VpnNode.PanelPassword` legacy plaintext value;
- `VpnNode.SshPrivateKeyPath` when it contains a protected marker or credential-like value;
- `VpnNode.ProtectedPanelPassword` and `VpnNode.ProtectedSshCredential` encrypted values;
- `VpnPanel.EncryptedPassword`;
- payment provider protected secrets;
- Telegram bot token and webhook secret;
- x3-ui credentials;
- password-reset and refresh tokens.

## Current Stage 7 storage model

New writes should use protected fields:

- `VpnNode.ProtectedPanelPassword` for panel passwords;
- `VpnNode.PanelSecretRef` as the configured reference/marker;
- `VpnNode.ProtectedSshCredential` for SSH password/private-key material in validation/dry-run flows;
- `VpnNode.SshCredentialRef` as the configured reference/marker.

Legacy fields are retained for compatibility only:

- `VpnNode.PanelPassword`;
- `VpnNode.SshPrivateKeyPath`.

These legacy fields must not be projected to admin/public/cabinet/Telegram APIs.

## Required key configuration

Set a strong secret protection key outside git:

```bash
Security__SecretEncryptionKey=<minimum-32-chars-random-secret>
```

In non-development environments the application must fail closed if this key is missing or too short. Development can fall back to a local signing key only for local validation.

## Migration procedure

1. Back up the database.
2. Deploy the migration that adds protected fields and token tables.
3. Run a one-time migration job or SQL/script that:
   - reads legacy `VpnNode.PanelPassword` values;
   - protects them with `ISecretProtector`;
   - writes `ProtectedPanelPassword` and `PanelSecretRef`;
   - clears legacy `PanelPassword` after a verified backup.
4. For SSH credentials:
   - if `SshPrivateKeyPath` is an operator-approved filesystem path, keep it only for explicitly approved staging/live provisioning;
   - if it contains credential material or protected markers, move it to `ProtectedSshCredential` or a future external secret reference;
   - clear credential-like legacy values after verification.
5. Re-run API/admin masking checks.
6. Search application logs and audit rows for known test secrets.
7. Rotate the migrated secrets if there is any chance of exposure.

## Rotation procedure

1. Add/replace the secret using a write-only admin/API field.
2. Confirm API responses show only `configured=true` or a masked preview.
3. Test connection in sandbox/approved staging only.
4. Disable the old secret in the upstream provider/panel/server.
5. Write an audit event with safe metadata only.

## Verification checklist

```bash
./scripts/check-validation-safety.sh
./scripts/validate-backend.sh
./scripts/validate-docker.sh
```

Then inspect:

```bash
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs backend-api --tail=200
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs backend-api --tail=200
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs telegram-bot --tail=200
```

No raw password, token, private key, SSH credential, bot token, webhook secret, `X3UI_PASSWORD`, protected payload or reset token should appear.

## Future external secret manager

Stage 7 intentionally does not integrate Vault, 1Password, AWS KMS, GCP KMS, Azure Key Vault or SOPS. Production should move toward one of those options and store only references in application tables.
