# Настройка Prodamus

Status: draft implementation; validate in CI and staging before production.

Required provider account fields:

- Provider: `Prodamus`
- ShopId: merchant/payform label
- ApiBaseUrl: payform URL, for example `https://demo.payform.ru/`
- SecretKey: payform secret key
- WebhookSecret: notification secret, usually the same secret unless separated operationally
- ReturnUrl: cabinet payment status URL

Optional `ExtraSettingsJson`:

```json
{
  "sys": "your-integration-code",
  "notificationUrl": "https://api.example.com/api/webhooks/payments/prodamus"
}
```

The adapter builds a payform URL with `order_id`, `products[0]...`, return URLs and signature. Webhook verification checks the `Sign` header. Recheck/refund remain fail-closed until merchant-specific REST API credentials are configured.
