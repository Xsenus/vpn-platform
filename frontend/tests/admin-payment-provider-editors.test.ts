import test from 'node:test'
import assert from 'node:assert/strict'
import type { PaymentProviderAccountDto, UpsertPaymentProviderAccountPayload } from '@vpn-platform/api-client'
import { isPaymentProviderAccountFormChanged } from '../apps/admin-panel/src/admin-payment-provider-editors.ts'

const account: PaymentProviderAccountDto = {
  id: 'provider-1', provider: 'YooKassa', mode: 'Sandbox', name: 'yookassa', publicName: 'YooKassa', isEnabled: true, isDefault: true, shopId: 'shop-1', apiBaseUrl: 'https://api.yookassa.ru/v3', returnUrl: '', webhookUrl: 'https://api.example.test/webhook', hasSecretKey: true, hasWebhookSecret: true, useWebhookIpAllowList: false, allowedWebhookIpRangesCsv: '', extraSettingsJson: '{"safe":"value"}', healthStatus: 'Unknown', isCheckoutConfigured: true, checkoutConfigurationIssue: null, capabilitiesJson: '[]', capabilities: [], requiredFields: [], readinessBlockers: [], isPubliclyAvailable: true, revision: 3, createdAt: '2026-08-15T00:00:00Z', updatedAt: '2026-08-15T00:00:00Z'
}

const form: UpsertPaymentProviderAccountPayload = {
  provider: 'YooKassa', mode: 'Sandbox', name: ' yookassa ', publicName: ' YooKassa ', isEnabled: true, isDefault: true, shopId: ' shop-1 ', apiBaseUrl: 'https://api.yookassa.ru/v3/', returnUrl: '', webhookUrl: ' https://api.example.test/webhook ', secretKey: '', webhookSecret: '', useWebhookIpAllowList: false, allowedWebhookIpRangesCsv: '', extraSettingsJson: '', revision: 3
}

test('payment provider editor matches backend normalization and preserved protected fields', () => {
  assert.equal(isPaymentProviderAccountFormChanged(form, account, 'https://api.yookassa.ru/v3'), false)
  assert.equal(isPaymentProviderAccountFormChanged({ ...form, apiBaseUrl: '' }, account, 'https://api.yookassa.ru/v3'), false)
  assert.equal(isPaymentProviderAccountFormChanged({ ...form, revision: 99 }, account, 'https://api.yookassa.ru/v3'), false)
})

test('payment provider editor treats configuration and secret rotations as changes', () => {
  assert.equal(isPaymentProviderAccountFormChanged({ ...form, publicName: 'Cards' }, account, 'https://api.yookassa.ru/v3'), true)
  assert.equal(isPaymentProviderAccountFormChanged({ ...form, secretKey: 'rotated' }, account, 'https://api.yookassa.ru/v3'), true)
  assert.equal(isPaymentProviderAccountFormChanged({ ...form, extraSettingsJson: '{"safe":"next"}' }, account, 'https://api.yookassa.ru/v3'), true)
})
