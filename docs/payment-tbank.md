# Настройка T-Bank Acquiring

Status: draft implementation; validate in CI and staging before production.

Required provider account fields:

- Provider: `TBankAcquiring`
- ShopId: `TerminalKey`
- SecretKey: terminal password used to generate `Token`
- ApiBaseUrl: `https://securepay.tinkoff.ru`
- ReturnUrl: cabinet payment status URL

Optional `ExtraSettingsJson`:

```json
{
  "notificationUrl": "https://api.example.com/api/webhooks/payments/tbank"
}
```

Implemented operations:

- create payment via `/v2/Init`;
- payment URL from `PaymentURL`;
- webhook/token verification;
- manual recheck via `/v2/GetState`;
- refund/cancel via `/v2/Cancel`.

Token generation uses root request fields plus `Password`, sorted alphabetically by key, concatenated by values and hashed with SHA-256.
