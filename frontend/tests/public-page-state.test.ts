import test from 'node:test'
import assert from 'node:assert/strict'
import type { PublicPaymentProviderDto, TariffDto } from '../packages/api-client/src/index.ts'
import { canStartCheckout, getCheckoutUnavailableReason, getPublicListState, getTariffFeatures } from '../apps/public-web/src/public-page-state.ts'

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
  assert.equal(canStartCheckout('', 'tariff-1', false, providers, 'YooKassa'), true)
  assert.equal(canStartCheckout('tariff-1', 'tariff-1', false, providers, 'YooKassa'), false)
  assert.equal(canStartCheckout('', 'tariff-1', false, [], 'YooKassa'), false)
})
