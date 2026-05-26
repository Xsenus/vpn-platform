# Настройка CloudPayments

Status: draft implementation; validate in CI and with a staging merchant account before production.

CloudPayments primarily uses a payment widget. The backend adapter does not create fake provider-hosted payment pages. Instead, configure a merchant-hosted widget URL and let the backend create a signed/traceable `PaymentAttempt` with a CloudPayments `invoiceId`.

Required provider account fields:

- Provider: `CloudPayments`
- ShopId: CloudPayments `publicId`
- SecretKey: CloudPayments API secret for notification HMAC
- ApiBaseUrl: optional
- ReturnUrl: cabinet payment status URL
- ExtraSettingsJson.hostedCheckoutUrl: your hosted widget page

Example:

```json
{
  "hostedCheckoutUrl": "https://pay.example.com/cloudpayments-widget"
}
```

Webhook URLs:

```text
https://api.example.com/api/webhooks/payments/cloudpayments/pay
https://api.example.com/api/webhooks/payments/cloudpayments/fail
https://api.example.com/api/webhooks/payments/cloudpayments/refund
```

The adapter verifies `Content-HMAC` or `X-Content-HMAC` using HMAC-SHA256 over the raw request body. Manual recheck/refund are fail-closed in this widget adapter until CloudPayments API methods are configured for the merchant.
