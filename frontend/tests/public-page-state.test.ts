import test from 'node:test'
import assert from 'node:assert/strict'
import type { OrderDto, PublicPaymentProviderDto, TariffDto } from '../packages/api-client/src/index.ts'
import { canStartCheckout, getCheckoutErrorMessage, getCheckoutUnavailableReason, getPendingCheckoutOrderAvailability, getPublicListState, getTariffFeatures } from '../apps/public-web/src/public-page-state.ts'

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
})
