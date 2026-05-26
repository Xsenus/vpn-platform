# Настройка Stripe и PayPal

Status: draft implementation; validate in CI and staging before production.

## Stripe

Required `PaymentProviderAccount` fields:

- Provider: `Stripe`
- Mode: `Sandbox` or `Production`
- ShopId: Stripe account id or merchant label
- SecretKey: `sk_test_...` or `sk_live_...`
- WebhookSecret: endpoint signing secret `whsec_...`
- ApiBaseUrl: `https://api.stripe.com`
- ReturnUrl: cabinet payment status URL

Webhook URL:

```text
https://api.example.com/api/webhooks/payments/stripe
```

Events to enable:

- `checkout.session.completed`
- `checkout.session.expired`

The adapter validates the raw-body signature and timestamp from `Stripe-Signature`, rejects wrong amount/currency/order, and deduplicates by event id + payment id.

## PayPal

Required `PaymentProviderAccount` fields:

- Provider: `PayPal`
- Mode: `Sandbox` or `Production`
- ShopId: PayPal REST app client id
- SecretKey: PayPal REST app client secret
- WebhookSecret: PayPal Webhook ID
- ApiBaseUrl: `https://api-m.sandbox.paypal.com` for sandbox or `https://api-m.paypal.com` for production
- ReturnUrl: cabinet payment status URL

Webhook URL:

```text
https://api.example.com/api/webhooks/payments/paypal
```

Events to enable:

- `CHECKOUT.ORDER.APPROVED`
- `PAYMENT.CAPTURE.COMPLETED`
- refund/cancel events as needed later

The adapter verifies webhook authenticity through PayPal's verification API. It does not trust return URLs as payment proof.
