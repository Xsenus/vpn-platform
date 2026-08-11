import test from 'node:test'
import assert from 'node:assert/strict'
import type { OrderDto, PublicPaymentProviderDto, TariffDto } from '../packages/api-client/src/index.ts'
import { canOpenCheckoutPayment, canStartCheckout, getCheckoutErrorMessage, getCheckoutPaymentExpiryDelay, getCheckoutUnavailableReason, getNextPublicCheckoutExpiryDelay, getPendingCheckoutOrderAvailability, getPublicListState, getTariffFeatures } from '../apps/public-web/src/public-page-state.ts'

function tariff(overrides: Partial<TariffDto>): TariffDto {
  return {
    id: 'tariff-1',
    name: 'Оптимальный',
    slug: 'optimal',
    description: 'VPN на месяц',
    durationDays: 30,
    price: 490,
    currency: 'RUB',
    maxDevices: 3,
    isActive: true,
    sortOrder: 10,
    ...overrides
  }
}

function provider(overrides: Partial<PublicPaymentProviderDto> = {}): PublicPaymentProviderDto {
  return {
    provider: 'YooKassa',
    publicName: 'ЮKassa',
    mode: 'Sandbox',
    isEnabled: true,
    isReady: true,
    supportsRefunds: true,
    supportsWebhooks: true,
    supportsStatusRecheck: true,
    readiness: {
      isReady: true,
      missingRequiredFields: [],
      warnings: [],
      publicMessage: 'Готово',
      capabilities: {
        checkout: true,
        webhooks: true,
        refunds: true,
        statusRecheck: true,
        sandbox: true,
        production: false
      }
    },
    ...overrides
  }
}

function order(overrides: Partial<OrderDto> = {}): OrderDto {
  return {
    id: 'order-1',
    userId: 'user-1',
    tariffId: 'tariff-1',
    amount: 490,
    currency: 'RUB',
    status: 'PendingPayment',
    expiresAt: '2026-05-27T12:15:00Z',
    ...overrides
  }
}

test('public page state parses tariff features from array and JSON safely', () => {
  assert.deepEqual(getTariffFeatures(tariff({ features: [' Автовыдача ', '', 'QR-код'] })), ['Автовыдача', 'QR-код'])
  assert.deepEqual(getTariffFeatures(tariff({ featuresJson: '["До 3 устройств","Поддержка"]' })), ['До 3 устройств', 'Поддержка'])
  assert.deepEqual(getTariffFeatures(tariff({ featuresJson: '{bad json}' })), [])
})

test('public page state separates loading, error, empty and ready states', () => {
  assert.equal(getPublicListState(true, '', 0), 'loading')
  assert.equal(getPublicListState(false, 'API недоступен', 0), 'error')
  assert.equal(getPublicListState(false, '', 0), 'empty')
  assert.equal(getPublicListState(false, '', 2), 'ready')
})

test('public page state explains checkout availability and button state', () => {
  const copy = {
    loading: 'Загружаем способы оплаты...',
    noProviders: 'Нет способов оплаты',
    chooseProvider: 'Выберите способ оплаты'
  }
  const providers = [provider()]

  assert.equal(getCheckoutUnavailableReason(true, [], '', copy), copy.loading)
  assert.equal(getCheckoutUnavailableReason(false, [], '', copy), copy.noProviders)
  assert.equal(getCheckoutUnavailableReason(false, providers, '', copy), copy.chooseProvider)
  assert.equal(getCheckoutUnavailableReason(false, providers, 'YooKassa', copy), '')
  assert.equal(canStartCheckout('', false, providers, 'YooKassa'), true)
  assert.equal(canStartCheckout('tariff-1', false, providers, 'YooKassa'), false)
  assert.equal(canStartCheckout('', false, [], 'YooKassa'), false)
})

test('public checkout translates promo failures and hides unrelated English diagnostics', () => {
  assert.equal(getCheckoutErrorMessage(new Error('Promo code not found.'), 'Ошибка'), 'Промокод не найден. Проверьте написание.')
  assert.equal(getCheckoutErrorMessage(new Error('Promo code usage limit for this account has been reached.'), 'Ошибка'), 'Вы уже использовали этот промокод максимально допустимое число раз.')
  assert.equal(getCheckoutErrorMessage(new Error('Payment provider is unavailable.'), 'Ошибка'), 'Ошибка')
  assert.equal(getCheckoutErrorMessage(null, 'Не удалось оформить покупку.'), 'Не удалось оформить покупку.')
})

test('public partial checkout retries only a live backend-supported order', () => {
  const now = new Date('2026-05-27T12:00:00Z')

  assert.equal(getPendingCheckoutOrderAvailability(order(), now).canRetry, true)
  assert.equal(getPendingCheckoutOrderAvailability(order({ status: 'Failed' }), now).canRetry, true)

  const expired = getPendingCheckoutOrderAvailability(order({ expiresAt: '2026-05-27T11:59:59Z' }), now)
  assert.equal(expired.canRetry, false)
  assert.equal(expired.shouldCreateNewOrder, true)
  assert.equal(expired.isExpired, true)
  assert.match(expired.reason ?? '', /Срок оплаты заказа истёк/)

  assert.equal(getPendingCheckoutOrderAvailability(order({ status: 'Completed' }), now).canRetry, false)

  const completed = getPendingCheckoutOrderAvailability(order({ status: 'Completed' }), now)
  assert.equal(completed.shouldForgetPendingCheckout, true)
  assert.equal(completed.shouldCreateNewOrder, false)
  assert.equal(completed.title, 'Покупка завершена')
  assert.equal(completed.statusLabel, 'покупка завершена')
  assert.equal(completed.reason, 'Оплата подтверждена. Подписка и VPN-доступ появятся в личном кабинете.')

  const cancelled = getPendingCheckoutOrderAvailability(order({ status: 'Cancelled' }), now)
  assert.equal(cancelled.shouldForgetPendingCheckout, true)
  assert.equal(cancelled.shouldCreateNewOrder, true)
  assert.equal(cancelled.title, 'Заказ отменён')

  const processing = getPendingCheckoutOrderAvailability(order({ status: 'FulfillmentInProgress' }), now)
  assert.equal(processing.shouldForgetPendingCheckout, true)
  assert.equal(processing.shouldCreateNewOrder, false)
  assert.equal(processing.title, 'Подключаем VPN-доступ')
})

test('public checkout exposes a payment link only until the retryable order expires', () => {
  const now = new Date('2026-05-27T12:00:00Z')

  assert.equal(canOpenCheckoutPayment(order(), now), true)
  assert.equal(canOpenCheckoutPayment(order({ status: 'Failed' }), now), true)
  assert.equal(getCheckoutPaymentExpiryDelay(order(), now), 15 * 60 * 1000)

  assert.equal(canOpenCheckoutPayment(order({ expiresAt: '2026-05-27T12:00:00Z' }), now), false)
  assert.equal(canOpenCheckoutPayment(order({ status: 'Completed' }), now), false)
  assert.equal(getCheckoutPaymentExpiryDelay(order({ expiresAt: '2026-05-27T12:00:00Z' }), now), null)
  assert.equal(getCheckoutPaymentExpiryDelay(order({ status: 'Completed' }), now), null)
  assert.equal(getNextPublicCheckoutExpiryDelay([
    order({ expiresAt: '2026-05-27T12:15:00Z' }),
    order({ expiresAt: '2026-05-27T12:05:00Z' })
  ], { expiresAt: '2026-05-27T12:10:00Z' }, now), 5 * 60 * 1000)
  assert.equal(getNextPublicCheckoutExpiryDelay([], { expiresAt: null }, now), null)
})
