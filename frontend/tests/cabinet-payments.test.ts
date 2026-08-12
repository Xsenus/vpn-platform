import test from 'node:test'
import assert from 'node:assert/strict'
import type { CabinetPaymentAttemptDto, OrderDto } from '../packages/api-client/src/index.ts'
import {
  buildOrderExportText,
  canOpenOrderPaymentConfirmation,
  canOpenPaymentConfirmation,
  formatPaymentMoney,
  getLatestPaymentForOrder,
  getNextOrderPaymentExpiryDelay,
  getOrderPaymentAvailability,
  getOrderPaymentProviderAvailability,
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
    paymentProvider: 'YooKassa',
    expiresAt: '2026-05-28T00:00:00Z',
    createdAt: '2026-05-27T10:00:00Z',
    updatedAt: '2026-05-27T10:00:00Z',
    ...overrides
  }
}

function payment(overrides: Partial<CabinetPaymentAttemptDto> = {}): CabinetPaymentAttemptDto {
  return {
    id: 'payment-1',
    orderId: 'order-1',
    provider: 'YooKassa',
    providerMode: 'Sandbox',
    providerPaymentId: 'provider-payment-1',
    amount: 499,
    currency: 'RUB',
    status: 'Pending',
    isActivationProcessed: false,
    refundedAmount: 0,
    statusMessage: 'Ожидаем подтверждение платежа.',
    createdAt: '2026-05-27T10:01:00Z',
    updatedAt: '2026-05-27T10:01:00Z',
    ...overrides
  }
}

test('cabinet payments allows retry only for backend-supported order statuses', () => {
  const now = new Date('2026-05-27T12:00:00Z')
  assert.equal(getOrderPaymentAvailability(order(), now).canRetry, true)
  assert.equal(getOrderPaymentAvailability(order({ status: 'Failed' }), now).canRetry, true)
  assert.equal(getOrderPaymentAvailability(order({ status: 'Expired' }), now).canRetry, false)
  assert.equal(getOrderPaymentAvailability(order({ status: 'Completed' }), now).canRetry, false)
  assert.equal(getOrderPaymentAvailability(order({ status: 'Paid' }), now).canRetry, false)
})

test('cabinet payments treats a stale pending order as expired and offers a new order', () => {
  const availability = getOrderPaymentAvailability(
    order({ status: 'PendingPayment', expiresAt: '2026-05-27T11:59:59Z' }),
    new Date('2026-05-27T12:00:00Z')
  )

  assert.deepEqual(availability, {
    canRetry: false,
    shouldCreateNewOrder: true,
    isExpired: true,
    reason: 'Срок оплаты заказа истёк. Создайте новый заказ с актуальным сроком оплаты.'
  })
})

test('cabinet payments initializes an existing order only with its provider snapshot', () => {
  assert.deepEqual(getOrderPaymentProviderAvailability(order(), [
    { provider: 'YooKassa' },
    { provider: 'Stripe' }
  ]), {
    canInitialize: true,
    provider: 'YooKassa',
    reason: null
  })

  const unavailable = getOrderPaymentProviderAvailability(order(), [{ provider: 'Stripe' }])
  assert.equal(unavailable.canInitialize, false)
  assert.equal(unavailable.provider, 'YooKassa')
  assert.match(unavailable.reason ?? '', /YooKassa.*недоступен/)
})

test('cabinet payments returns human messages and tones for important statuses', () => {
  assert.match(getOrderStatusMessage('PendingPayment'), /ожидает оплаты/)
  assert.match(getOrderStatusMessage('Failed'), /не прошла/)
  assert.match(getOrderStatusMessage('Paid'), /подтверждена/)
  assert.doesNotMatch(getOrderStatusMessage('Expired'), /повторить оплату/)

  assert.match(getPaymentStatusMessage('Pending'), /ожидает подтверждения/)
  assert.match(getPaymentStatusMessage('Failed'), /ошибкой/)
  assert.match(getPaymentStatusMessage('Succeeded'), /успешно/)
  assert.match(getPaymentStatusMessage('PartiallyRefunded'), /частичный возврат/)

  assert.equal(getPaymentStatusTone('Pending'), 'pending')
  assert.equal(getPaymentStatusTone('Succeeded'), 'success')
  assert.equal(getPaymentStatusTone('PartiallyRefunded'), 'success')
  assert.equal(getPaymentStatusTone('Failed'), 'failed')
})

test('cabinet payments exposes confirmation links only for open payment statuses', () => {
  for (const status of ['New', 'Pending', 'WaitingConfirmation']) {
    assert.equal(canOpenPaymentConfirmation(status), true, status)
  }

  for (const status of ['Succeeded', 'Failed', 'Cancelled', 'Refunded', 'PartiallyRefunded', 'Unknown']) {
    assert.equal(canOpenPaymentConfirmation(status), false, status)
  }
})

test('cabinet payments exposes confirmation links only while the parent order is payable', () => {
  const now = new Date('2026-05-27T12:00:00Z')

  assert.equal(canOpenOrderPaymentConfirmation(order(), 'Pending', now), true)
  assert.equal(canOpenOrderPaymentConfirmation(order(), null, now), true)
  assert.equal(canOpenOrderPaymentConfirmation(undefined, 'Pending', now), false)
  assert.equal(canOpenOrderPaymentConfirmation(order({ expiresAt: '2026-05-27T12:00:00Z' }), 'Pending', now), false)
  assert.equal(canOpenOrderPaymentConfirmation(order({ status: 'Completed' }), 'Pending', now), false)
  assert.equal(canOpenOrderPaymentConfirmation(order(), 'Succeeded', now), false)
})

test('cabinet payments schedules the nearest future payable-order expiry', () => {
  const now = new Date('2026-05-27T12:00:00Z')

  assert.equal(getNextOrderPaymentExpiryDelay([
    order({ id: 'later', expiresAt: '2026-05-27T12:00:05Z' }),
    order({ id: 'nearest', expiresAt: '2026-05-27T12:00:02Z' }),
    order({ id: 'terminal', status: 'Completed', expiresAt: '2026-05-27T12:00:01Z' }),
    order({ id: 'already-expired', expiresAt: '2026-05-27T11:59:59Z' })
  ], now), 2_000)
  assert.equal(getNextOrderPaymentExpiryDelay([order({ status: 'Completed' })], now), null)
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
      statusMessage: 'Платёж не завершён. Повторите оплату или обратитесь в поддержку.'
    })
  ])

  const parsed = JSON.parse(exported)

  assert.equal(parsed.order.id, 'order-1')
  assert.equal(parsed.order.status, 'Failed')
  assert.equal(parsed.payments[0].id, 'payment-failed')
  assert.equal(parsed.payments[0].rawRequest, undefined)
  assert.equal(parsed.payments[0].rawResponse, undefined)
  assert.equal(parsed.payments[0].webhookPayload, undefined)
  assert.equal(parsed.payments[0].statusReason, undefined)
  assert.equal(parsed.payments[0].statusMessage, 'Платёж не завершён. Повторите оплату или обратитесь в поддержку.')
  assert.equal(formatPaymentMoney(1499.5, 'RUB'), '1 499,5 RUB')
})
