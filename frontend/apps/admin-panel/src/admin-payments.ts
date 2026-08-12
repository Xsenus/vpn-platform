import type { OrderDto, PaymentAttemptDto } from '@vpn-platform/api-client'

export function getAdminPaymentRecheckBlocker(payment: Pick<PaymentAttemptDto, 'recheckSupported'>) {
  return payment.recheckSupported
    ? null
    : 'Провайдер этого платежа не поддерживает ручную перепроверку статуса.'
}

export function getAdminOrderPaymentRecheckBlocker(order: Pick<OrderDto, 'lastPaymentId' | 'lastPaymentRecheckSupported'>) {
  if (!order.lastPaymentId) return 'Сначала нужна платежная попытка.'
  return order.lastPaymentRecheckSupported
    ? null
    : 'Провайдер последнего платежа не поддерживает ручную перепроверку статуса.'
}
