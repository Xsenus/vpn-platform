import test from 'node:test'
import assert from 'node:assert/strict'
import type { OrderDto, PaymentAttemptDto } from '../packages/api-client/src/index.ts'
import {
  buildOrderExportText,
  canRetryOrderPayment,
  formatPaymentMoney,
  getLatestPaymentForOrder,
  getOrderStatusMessage,
  getPaymentStatusMessage,
  getPaymentStatusTone,
  groupPaymentsByOrderId
} from '../apps/cabinet/src/cabinet-payments.ts'

function order(overrides: Partial<OrderDto> = {}): OrderDto {
  return {
    id: 'order-1',
    userId: 'user-1',
    tariffId: 'tariff-1',
    tariffName: 'Оптимальный',
    amount: 499,
    currency: 'RUB',
    status: 'PendingPayment',
    type: 'New',
    channel: 'Web',
    paymentProvider: 'yookassa',
    expiresAt: '2026-05-28T00:00:00Z',
    createdAt: '2026-05-27T10:00:00Z',
    updatedAt: '2026-05-27T10:00:00Z',
    ...overrides
  }
}

function payment(overrides: Partial<PaymentAttemptDto> = {}): PaymentAttemptDto {
  return {
    id: 'payment-1',
    orderId: 'order-1',
    provider: 'yookassa',
    providerPaymentId: 'provider-payment-1',
    externalEventId: 'event-1',
    amount: 499,
    currency: 'RUB',
    status: 'Pending',
    signatureValidated: true,
    createdAt: '2026-05-27T10:01:00Z',
    updatedAt: '2026-05-27T10:01:00Z',
    ...overrides
  }
}

test('cabinet payments allows retry only for recoverable order statuses', () => {
  assert.equal(canRetryOrderPayment('PendingPayment'), true)
  assert.equal(canRetryOrderPayment('Failed'), true)
  assert.equal(canRetryOrderPayment('Expired'), true)

  assert.equal(canRetryOrderPayment('Completed'), false)
  assert.equal(canRetryOrderPayment('Paid'), false)
})

test('cabinet payments returns human messages and tones for important statuses', () => {
  assert.match(getOrderStatusMessage('PendingPayment'), /ожидает оплаты/)
  assert.match(getOrderStatusMessage('Failed'), /не прошла/)
  assert.match(getOrderStatusMessage('Paid'), /подтверждена/)

  assert.match(getPaymentStatusMessage('Pending'), /ожидает подтверждения/)
  assert.match(getPaymentStatusMessage('Failed'), /ошибкой/)
  assert.match(getPaymentStatusMessage('Succeeded'), /успешно/)
  assert.match(getPaymentStatusMessage('PartiallyRefunded'), /частичный возврат/)

  assert.equal(getPaymentStatusTone('Pending'), 'pending')
  assert.equal(getPaymentStatusTone('Succeeded'), 'success')
  assert.equal(getPaymentStatusTone('PartiallyRefunded'), 'success')
  assert.equal(getPaymentStatusTone('Failed'), 'failed')
})

test('cabinet payments groups attempts by order and selects the newest attempt', () => {
  const first = payment({ id: 'payment-old', createdAt: '2026-05-27T10:01:00Z' })
  const second = payment({ id: 'payment-new', createdAt: '2026-05-27T10:02:00Z' })
  const other = payment({ id: 'payment-other', orderId: 'order-2', createdAt: '2026-05-27T10:03:00Z' })

  const grouped = groupPaymentsByOrderId([first, other, second])

  assert.deepEqual(grouped.get('order-1')?.map((item) => item.id), ['payment-new', 'payment-old'])
  assert.equal(getLatestPaymentForOrder(order(), [first, other, second])?.id, 'payment-new')
})

test('cabinet payments exports safe order details without raw provider payloads', () => {
  const exported = buildOrderExportText(order({ status: 'Failed' }), [
    payment({
      id: 'payment-failed',
      status: 'Failed',
      rawRequest: '{"secret":"request"}',
      rawResponse: '{"secret":"response"}',
      webhookPayload: '{"secret":"webhook"}'
    })
  ])

  const parsed = JSON.parse(exported)

  assert.equal(parsed.order.id, 'order-1')
  assert.equal(parsed.order.status, 'Failed')
  assert.equal(parsed.payments[0].id, 'payment-failed')
  assert.equal(parsed.payments[0].rawRequest, undefined)
  assert.equal(parsed.payments[0].rawResponse, undefined)
  assert.equal(parsed.payments[0].webhookPayload, undefined)
  assert.equal(formatPaymentMoney(1499.5, 'RUB'), '1 499,5 RUB')
})
