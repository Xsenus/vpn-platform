# Ротация секретов

Этот runbook описывает безопасную замену секретов без показа старых значений.

## Что поддерживается

- Платежные провайдеры: `SecretKey` и `WebhookSecret` заменяются через write-only поля в админке/API.
- Telegram bot: `BotToken` и `SecretToken` заменяются через write-only поля раздела бота.
- VPN/provisioning server: SSH credential и пароль панели заменяются через write-only поля сервера.

Во всех случаях read API возвращает только признаки `hasSecretKey`, `hasWebhookSecret`, `SshCredentialConfigured`, `PanelPasswordConfigured` или masked token preview. Raw secrets, protected payloads и `secretref:*` не должны возвращаться в DTO.

## Audit-события

- `payment_provider.secret.rotate`
- `telegram_bot.secret.rotate`
- `server.secret.rotate`

Audit содержит только безопасные флаги:

- какой тип секрета был заменен;
- provider/mode/name или node metadata;
- признаки configured/readiness.

Audit не содержит старый секрет, новый секрет, protected payload или `secretref:*`.

## Правила эксплуатации

1. Создать новый секрет у внешнего провайдера или на сервере.
2. Внести новый секрет в админке через write-only поле.
3. Проверить readiness/test connection.
4. Отключить старый секрет во внешней системе.
5. Проверить audit log на наличие события ротации.
6. Проверить, что API/UI/logs не содержат raw secret.

## Проверка

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "SecurityHardeningMvpTests|AdminTelegramBotSettingsControllerTests|AuditLogMvpTests"
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore
```

Локально также проверяется SQLite-smoke: health, ready, metrics, admin login и latest release.
