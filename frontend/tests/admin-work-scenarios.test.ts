import test from 'node:test'
import assert from 'node:assert/strict'
import type { WorkScenarioUpsertPayload } from '@vpn-platform/api-client'
import { validateWorkScenarioForm } from '../apps/admin-panel/src/admin-work-scenarios.ts'

function validScenario(): WorkScenarioUpsertPayload {
  return {
    name: 'Автоматическая выдача',
    key: 'auto',
    isActive: true,
    allowedTariffIdsJson: '[]',
    vpnProtocol: 'vless',
    serverSelectionRule: 'least-loaded',
    inboundSelectionRule: 'default',
    provisioningMode: 'auto',
    onPaymentSucceeded: 'create_subscription_and_access',
    onPaymentFailed: 'keep_order_pending',
    onRefund: 'disable_access',
    onSubscriptionExpired: 'disable_access_after_grace',
    onRenewal: 'extend_subscription',
    cabinetText: 'Доступ готов.',
    telegramText: 'Доступ готов.',
    generateQrCode: true,
    maxDevices: 3,
    trafficLimit: null,
    sortOrder: 10
  }
}

test('work scenario form matches backend storage boundaries', () => {
  const valid = validScenario()

  assert.deepEqual(validateWorkScenarioForm(valid), [])
  assert.ok(validateWorkScenarioForm({ ...valid, name: 'n'.repeat(201) }).some((error) => error.includes('200')))
  assert.ok(validateWorkScenarioForm({ ...valid, key: 'k'.repeat(121) }).some((error) => error.includes('120')))
  assert.ok(validateWorkScenarioForm({ ...valid, allowedTariffIdsJson: 'x'.repeat(4001) }).some((error) => error.includes('тарифов')))
  assert.ok(validateWorkScenarioForm({ ...valid, serverSelectionRule: 's'.repeat(121) }).some((error) => error.includes('сервера')))
  assert.ok(validateWorkScenarioForm({ ...valid, inboundSelectionRule: 'i'.repeat(121) }).some((error) => error.includes('inbound')))
  assert.ok(validateWorkScenarioForm({ ...valid, cabinetText: 't'.repeat(4001) }).some((error) => error.includes('4000')))
})
