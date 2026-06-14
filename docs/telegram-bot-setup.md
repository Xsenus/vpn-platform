# Настройка Telegram Bot

Status: draft operational guide.

## Environment variables

```env
TelegramBot__Enabled=true
TelegramBot__Mode=LongPolling # or Webhook
TelegramBot__BotToken=<telegram-bot-token>
TelegramBot__WebhookUrl=https://api.example.com/api/channels/telegram/webhook
TelegramBot__SecretToken=<random-secret-for-webhook-mode>
TelegramBot__PublicBotUsername=vpnplatform_bot
TelegramBot__WebAppUrl=https://cabinet.example.com
TelegramBot__AllowedUpdates__0=message
TelegramBot__AllowedUpdates__1=callback_query
TelegramBot__AllowedUpdates__2=pre_checkout_query
```

Production rules:

- `BotToken` is required when `Enabled=true`.
- Webhook mode requires `WebhookUrl` and `SecretToken`.
- BotToken and webhook secret are never logged.

## LongPolling

```bash
cd backend
TelegramBot__Enabled=true \
TelegramBot__Mode=LongPolling \
TelegramBot__BotToken='<token>' \
dotnet run --project src/VpnPlatform.TelegramBot --environment Development
```

LongPolling uses persisted update idempotency; duplicate updates are ignored.

## Webhook

```bash
cd backend
TelegramBot__Enabled=true \
TelegramBot__Mode=Webhook \
TelegramBot__BotToken='<token>' \
TelegramBot__WebhookUrl='https://api.example.com/api/channels/telegram/webhook' \
TelegramBot__SecretToken='<secret>' \
dotnet run --project src/VpnPlatform.Api --environment Production
```

Telegram must send:

```http
X-Telegram-Bot-Api-Secret-Token: <secret>
```

The main API endpoint `POST /api/channels/telegram/webhook` reads Telegram bot settings from the admin panel database first and falls back to `appsettings`. The standalone `VpnPlatform.TelegramBot` process is still useful for LongPolling, but Webhook mode does not require a separate bot process.

## Account linking

1. User logs into cabinet.
2. Cabinet calls `POST /api/me/telegram/link-token`.
3. Backend stores only token hash with TTL.
4. User opens `https://t.me/<bot>?start=link_<token>`.
5. Bot links `TelegramAccount` to `User` and marks token as used.

## Registration from Telegram

`register_tg` creates one `User` per Telegram id:

- `AuthSource=Telegram`
- `EmailConfirmed=false`
- placeholder email `tg_<telegramUserId>@telegram.local`

The placeholder email is intended to be replaced in cabinet later.

## Troubleshooting

- `Invalid Telegram webhook secret token`: check `TelegramBot:SecretToken` and Telegram webhook configuration.
- Bot does not send messages: verify `TelegramBot:Enabled=true`, valid BotToken and network connectivity.
- Stars invoice not sent: configure BotToken in the admin panel or API app settings and verify API network access to `api.telegram.org`.
- Duplicate updates: safe by design; check `TelegramBotUpdates` table.
