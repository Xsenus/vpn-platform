import test from 'node:test'
import assert from 'node:assert/strict'
import { ApiClientError, isPaymentProvider } from '../packages/api-client/src/index.ts'
import { getPendingCheckoutSessionAvailability, isCheckoutSessionExpiredError, parsePendingCheckout } from '../apps/public-web/src/pending-checkout.ts'

const validCheckout = {
  token: 'checkout_token_1234567890123456789012345678',
  tariffName: 'Оптимальный',
  provider: 'YooKassa',
  expiresAt: '2099-06-14T00:00:00Z'
}

test('payment provider guard accepts only the public API allow-list', () => {
  for (const provider of [
    'YooMoney',
    'YooKassa',
    'RoboKassa',
    'TelegramStars',
    'CloudPayments',
    'TBankAcquiring',
    'Prodamus',
    'Stripe',
    'PayPal'
  ]) {
    assert.equal(isPaymentProvider(provider), true)
  }

  assert.equal(isPaymentProvider('UnknownProvider'), false)
  assert.equal(isPaymentProvider('yookassa'), false)
  assert.equal(isPaymentProvider(null), false)
})

test('pending checkout parser restores only a bounded valid checkout', () => {
  assert.deepEqual(parsePendingCheckout(JSON.stringify(validCheckout)), validCheckout)
  assert.deepEqual(
    parsePendingCheckout(JSON.stringify({ ...validCheckout, tariffName: '  Оптимальный  ' })),
    validCheckout
  )
  assert.deepEqual(
    parsePendingCheckout(JSON.stringify({ token: validCheckout.token, tariffName: validCheckout.tariffName, provider: validCheckout.provider })),
    { ...validCheckout, expiresAt: null }
  )

  assert.equal(getPendingCheckoutSessionAvailability(validCheckout, new Date('2026-06-14T00:00:00Z')).canClaim, true)
  const expired = getPendingCheckoutSessionAvailability(
    { expiresAt: '2026-06-13T23:59:59Z' },
    new Date('2026-06-14T00:00:00Z')
  )
  assert.equal(expired.canClaim, false)
  assert.equal(expired.shouldCreateNewOrder, true)
  assert.equal(expired.shouldForgetPendingCheckout, true)
  assert.equal(expired.title, 'Срок оформления покупки истёк')

  assert.equal(isCheckoutSessionExpiredError(new ApiClientError('Ошибка', 400, { error: 'Checkout session expired.' })), true)
  assert.equal(isCheckoutSessionExpiredError(new ApiClientError('Ошибка', 400, { error: 'Checkout session not found.' })), false)
})

test('pending checkout parser rejects malformed and unsafe persisted values', () => {
  const invalidValues = [
    null,
    '',
    '{',
    'null',
    '[]',
    JSON.stringify({}),
    JSON.stringify({ ...validCheckout, token: '' }),
    JSON.stringify({ ...validCheckout, token: '../auth/logout' }),
    JSON.stringify({ ...validCheckout, token: 'x'.repeat(513) }),
    JSON.stringify({ ...validCheckout, tariffName: ' ' }),
    JSON.stringify({ ...validCheckout, tariffName: 'x'.repeat(201) }),
    JSON.stringify({ ...validCheckout, provider: '../../auth/logout' }),
    JSON.stringify({ ...validCheckout, provider: null }),
    JSON.stringify({ ...validCheckout, expiresAt: '' }),
    JSON.stringify({ ...validCheckout, expiresAt: 'not-a-date' }),
    'x'.repeat(4_097)
  ]

  for (const raw of invalidValues) {
    assert.equal(parsePendingCheckout(raw), null)
  }
})
