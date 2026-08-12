import type { OrderDto, PaymentAttemptDto } from '@vpn-platform/api-client'

export function getAdminPaymentRecheckBlocker(payment: Pick<PaymentAttemptDto, 'recheckSupported' | 'canRecheck' | 'recheckBlockers'>) {
  if (!payment.recheckSupported) return payment.recheckBlockers?.[0] ?? 'Провайдер этого платежа не поддерживает ручную перепроверку статуса.'
  if (!payment.canRecheck) return payment.recheckBlockers?.[0] ?? 'Платеж не готов к ручной перепроверке статуса.'
  return null
}

export function getAdminOrderPaymentRecheckBlocker(order: Pick<OrderDto, 'lastPaymentId' | 'lastPaymentRecheckSupported' | 'lastPaymentCanRecheck' | 'lastPaymentRecheckBlockers'>) {
  if (!order.lastPaymentId) return 'Сначала нужна платежная попытка.'
  if (!order.lastPaymentRecheckSupported) return order.lastPaymentRecheckBlockers?.[0] ?? 'Провайдер последнего платежа не поддерживает ручную перепроверку статуса.'
  if (!order.lastPaymentCanRecheck) return order.lastPaymentRecheckBlockers?.[0] ?? 'Последний платеж не готов к ручной перепроверке статуса.'
  return null
}
