# Этап 3: основа Telegram Bot

Дата: 2026-04-29

## Scope

Этот срез добавляет foundation для отдельного Telegram Bot service без попытки закрыть весь purchase/support/stars end-to-end.

Добавлено:

- executable project `VpnPlatform.TelegramBot`;
- LongPolling mode;
- Webhook mode endpoint;
- Telegram update idempotency;
- raw update persistence with redaction;
- TelegramAccount persistence;
- account linking through one-time deep link token;
- basic command routing;
- callback routing foundation;
- support conversation foundation;
- Telegram notifications dispatcher;
- Telegram Stars placeholder entities/provider fail-closed through payment architecture.

## Configuration

```json
"TelegramBot": {
  "Enabled": false,
  "Mode": "LongPolling",
  "BotToken": "",
  "WebhookUrl": "",
  "SecretToken": "",
  "AllowedUpdates": ["message", "callback_query", "pre_checkout_query"],
  "AdminChatId": "",
  "PublicBotUsername": "vpnplatform_bot",
  "WebAppUrl": "http://localhost:5174"
}
```

Production rules:

- if `TelegramBot:Enabled=true`, `BotToken` is required;
- Webhook mode requires `WebhookUrl` and `SecretToken`;
- token/secret values must not be logged.

## User linking flow

1. Authenticated cabinet user calls:

```http
POST /api/me/telegram/link-token
```

2. Backend creates one-time deep link token and stores only `TokenHash`.
3. User opens returned `https://t.me/<bot>?start=link_<token>`.
4. Bot receives `/start link_<token>`.
5. Bot links TelegramAccount to User if token exists, is not expired and was not used.
6. Reuse and cross-user claims are rejected.

## Bot commands

Implemented commands:

- `/start`;
- `/start link_<token>`;
- `/help`;
- `/menu`;
- `/subscriptions`;
- `/orders`;
- `/support`.

Main menu inline keyboard foundation:

- Купить VPN;
- Мои подписки;
- Мой доступ;
- Продлить;
- Поддержка;
- Личный кабинет.

## Running LongPolling

```bash
cd backend
dotnet run --project src/VpnPlatform.TelegramBot --environment Development
```

Required env vars:

```bash
export TelegramBot__Enabled=true
export TelegramBot__Mode=LongPolling
export TelegramBot__BotToken='<bot-token>'
export TelegramBot__PublicBotUsername='<bot-username-without-at>'
```

## Running Webhook

```bash
cd backend
dotnet run --project src/VpnPlatform.TelegramBot --environment Production
```

Required env vars:

```bash
export TelegramBot__Enabled=true
export TelegramBot__Mode=Webhook
export TelegramBot__BotToken='<bot-token>'
export TelegramBot__WebhookUrl='https://api.example.com/telegram/webhook'
export TelegramBot__SecretToken='<random-secret>'
```

Service endpoint:

```http
POST /telegram/webhook
X-Telegram-Bot-Api-Secret-Token: <random-secret>
```

## Support foundation

- `/support` creates/opens a Telegram support conversation.
- Subsequent text while session state is `support` is stored as inbound support message.
- Admin panel lists conversations.
- Admin reply API stores outbound message and queues `TelegramBotNotification` for the bot service to send.

## Not implemented yet

- complete Telegram Stars payment end-to-end;
- tariff selection and order creation directly inside bot;
- payment provider selection UI inside bot;
- QR/config delivery;
- rich support inbox with attachments/operators/internal notes;
- Telegram WebApp initData verification.
