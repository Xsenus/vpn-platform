# Усиление безопасности MVP

Stage 7 introduces the first production-hardening layer without enabling live integrations.

## Implemented MVP controls

- Protected secret storage via `ISecretProtector` / `SecretProtector`.
- Fail-closed secret key requirement outside Development.
- Protected fields for new VPN node panel and SSH credentials.
- Refresh-token storage with SHA-256 hashes, expiry, rotation and revocation.
- Password-reset token storage with SHA-256 hashes, expiry and one-time use.
- SVG QR generation for VPN access links.
- Centralized secret redaction helper for audit, provisioning, provider and lifecycle errors.
- Validation safety script for compose/workflow/env defaults.
- Write-only/masked secret UX for admin forms.

## Still not production-ready

- Legacy plaintext fields remain in the schema for backward compatibility until a verified data migration clears them.
- Own VPS live provisioning can materialize protected `ssh_key` credentials only through `ProvisioningSecretMaterializer`: the runner receives a temporary path, the plaintext and path are redacted, and the file is deleted in `finally`. Password-based live SSH and full production smoke remain fail-closed until a real staging/VPS run is approved.
- Password reset использует durable email queue и SMTP adapter; reset-код хранится в delivery payload только через `ISecretProtector`, а production startup fail-closed требует SMTP-конфигурацию. Реальная доставка остается внешней staging/VPS-проверкой.
- Backend/Docker validation must still pass on CI/server with `.NET 9 SDK` and Docker.
- External secret manager integration is not implemented.

## Safe validation defaults

The validation stack must keep:

```bash
TelegramBot__Enabled=false
AdminBootstrap__Enabled=false
Provisioning__LiveExecutionEnabled=false
Provisioning__AllowLiveDeploy=false
Vpn__X3Ui__Mode=Sandbox
X3UI_BASE_URL=
X3UI_USERNAME=
X3UI_PASSWORD=
```

All payment provider modes must remain `Disabled` during validation unless a specific sandbox E2E has explicit approval.
