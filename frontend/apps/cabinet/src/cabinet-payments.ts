import type { OrderDto, PaymentAttemptDto } from '@vpn-platform/api-client'

export type PaymentStatusTone = 'pending' | 'success' | 'failed' | 'neutral'

const retryableOrderStatuses = new Set(['PendingPayment', 'Failed', 'Expired'])
const successfulStatuses = new Set(['Paid', 'Completed', 'Succeeded', 'Success', 'Activated', 'Refunded', 'PartiallyRefunded'])
const failedStatuses = new Set(['Failed', 'Canceled', 'Cancelled', 'Expired', 'Rejected'])
const pendingStatuses = new Set(['Pending', 'PendingPayment', 'Created', 'Processing', 'WaitingForCapture'])

export function formatPaymentMoney(amount: number, currency: string) {
  return `${amount.toLocaleString('ru-RU', { maximumFractionDigits: 2 })} ${currency}`
}

export function canRetryOrderPayment(status: string) {
  return retryableOrderStatuses.has(status)
}

export function getPaymentStatusTone(status: string): PaymentStatusTone {
  if (successfulStatuses.has(status)) return 'success'
  if (failedStatuses.has(status)) return 'failed'
  if (pendingStatuses.has(status)) return 'pending'
  return 'neutral'
}

export function getOrderStatusMessage(status: string) {
  switch (status) {
    case 'PendingPayment':
    case 'Pending':
    case 'Created':
      return 'Заказ ожидает оплаты. Если страница оплаты закрылась, откройте оплату повторно.'
    case 'Paid':
    case 'Completed':
    case 'Succeeded':
    case 'Success':
      return 'Оплата подтверждена. Подписка и VPN-доступ появятся в кабинете автоматически.'
    case 'Failed':
      return 'Оплата не прошла. Проверьте способ оплаты или повторите попытку.'
    case 'Expired':
      return 'Срок оплаты заказа истек. Можно повторить оплату или создать новый заказ.'
    case 'Canceled':
    case 'Cancelled':
      return 'Заказ отменен. Для покупки VPN выберите тариф заново.'
    default:
      return 'Статус заказа обновляется автоматически после ответа платежного провайдера.'
  }
}

export function getPaymentStatusMessage(status: string) {
  switch (status) {
    case 'Pending':
    case 'PendingPayment':
    case 'Created':
    case 'Processing':
    case 'WaitingForCapture':
      return 'Платеж создан и ожидает подтверждения от провайдера.'
    case 'Succeeded':
    case 'Success':
    case 'Paid':
    case 'Completed':
      return 'Платеж успешно подтвержден. Доступ должен активироваться автоматически.'
    case 'Failed':
      return 'Платеж завершился ошибкой. Попробуйте оплатить еще раз или выберите другой способ.'
    case 'Canceled':
    case 'Cancelled':
      return 'Платеж отменен. Деньги не списаны или операция отменена провайдером.'
    case 'Refunded':
      return 'Платеж возвращен. Сумма возврата отражается в деталях платежа.'
    case 'PartiallyRefunded':
      return 'По платежу выполнен частичный возврат. Сумма возврата отражается в деталях платежа.'
    default:
      return 'Статус платежа синхронизируется с платежным провайдером.'
  }
}

export function groupPaymentsByOrderId(payments: PaymentAttemptDto[]) {
  const grouped = new Map<string, PaymentAttemptDto[]>()

  for (const payment of payments) {
    const list = grouped.get(payment.orderId) ?? []
    list.push(payment)
    grouped.set(payment.orderId, list)
  }

  for (const list of grouped.values()) {
    list.sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
  }

  return grouped
}

export function getLatestPaymentForOrder(order: OrderDto, payments: PaymentAttemptDto[]) {
  const orderPayments = payments.filter((payment) => payment.orderId === order.id)
  orderPayments.sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))

  return orderPayments[0] ?? null
}

export function buildOrderExportText(order: OrderDto, payments: PaymentAttemptDto[]) {
  const safePayments = payments.map((payment) => ({
    id: payment.id,
    orderId: payment.orderId,
    provider: payment.provider,
    providerMode: payment.providerMode ?? null,
    providerPaymentId: payment.providerPaymentId,
    amount: payment.amount,
    currency: payment.currency,
    status: payment.status,
    statusReason: payment.statusReason ?? null,
    confirmationUrl: payment.confirmationUrl ?? null,
    returnUrl: payment.returnUrl ?? null,
    signatureValidated: payment.signatureValidated,
    isActivationProcessed: payment.isActivationProcessed ?? false,
    paidAt: payment.paidAt ?? null,
    failedAt: payment.failedAt ?? null,
    refundedAt: payment.refundedAt ?? null,
    createdAt: payment.createdAt,
    updatedAt: payment.updatedAt
  }))

  return JSON.stringify({
    order: {
      id: order.id,
      tariffId: order.tariffId,
      tariffName: order.tariffName ?? null,
      amount: order.amount,
      currency: order.currency,
      status: order.status,
      type: order.type ?? null,
      channel: order.channel ?? null,
      paymentProvider: order.paymentProvider ?? null,
      expiresAt: order.expiresAt,
      paidAt: order.paidAt ?? null,
      isFirstPurchase: order.isFirstPurchase ?? null,
      linkedSubscriptionId: order.linkedSubscriptionId ?? null,
      createdAt: order.createdAt ?? null,
      updatedAt: order.updatedAt ?? null
    },
    payments: safePayments
  }, null, 2)
}
