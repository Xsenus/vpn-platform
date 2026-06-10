# Машины состояний

## OrderStatus
- Draft
- PendingPayment
- PaymentReceived
- FulfillmentInProgress
- Completed
- Failed
- Cancelled
- Expired
- Refunded
- PartiallyProcessed

## PaymentStatus
- New
- Pending
- WaitingConfirmation
- Succeeded
- Failed
- Cancelled
- Refunded
- PartiallyRefunded
- Unknown

## SubscriptionStatus
- PendingActivation
- Active
- GracePeriod
- Expired
- Suspended
- Cancelled
- Blocked

Основные автоматические переходы:

- `Active -> GracePeriod`, когда `EndAt <= now`. VPN-доступ еще остается активным, создаются `NotificationRequested` и Telegram-уведомление `subscription_expiring`.
- `GracePeriod -> Expired`, когда `GracePeriodEndAt <= now`. VPN-доступ отключается через провайдера, пишется история доступа, создаются `NotificationRequested` и Telegram-уведомление `subscription_expired`.
- Повторный запуск lifecycle worker не должен создавать дубли уведомлений для уже обработанной подписки.

## AccessCredentialStatus
- Provisioning
- Active
- Rotating
- Disabled
- Revoked
- Error
- SyncRequired

## NodeStatus
- New
- Provisioning
- Ready
- Degraded
- Full
- Draining
- Maintenance
- Disabled
- Error
- Archived
