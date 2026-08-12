import test from 'node:test'
import assert from 'node:assert/strict'
import { getAdminOrderPaymentRecheckBlocker, getAdminPaymentRecheckBlocker } from '../apps/admin-panel/src/admin-payments.ts'

test('admin payment recheck blockers fail closed for unsupported or incomplete payments', () => {
  assert.equal(getAdminPaymentRecheckBlocker({ recheckSupported: true, canRecheck: true, recheckBlockers: [] }), null)
  assert.match(getAdminPaymentRecheckBlocker({ recheckSupported: false, canRecheck: false, recheckBlockers: ['Провайдер не поддерживает ручную перепроверку.'] }) ?? '', /не поддерживает ручную перепроверку/)
  assert.match(getAdminPaymentRecheckBlocker({ recheckSupported: true, canRecheck: false, recheckBlockers: ['Аккаунт платежного провайдера выключен.'] }) ?? '', /аккаунт платежного провайдера выключен/i)
  assert.equal(getAdminOrderPaymentRecheckBlocker({ lastPaymentId: 'payment-1', lastPaymentRecheckSupported: true, lastPaymentCanRecheck: true, lastPaymentRecheckBlockers: [] }), null)
  assert.match(getAdminOrderPaymentRecheckBlocker({ lastPaymentId: 'payment-1', lastPaymentRecheckSupported: false, lastPaymentCanRecheck: false, lastPaymentRecheckBlockers: ['Провайдер не поддерживает ручную перепроверку.'] }) ?? '', /не поддерживает ручную перепроверку/)
  assert.match(getAdminOrderPaymentRecheckBlocker({ lastPaymentId: 'payment-1', lastPaymentRecheckSupported: true, lastPaymentCanRecheck: false, lastPaymentRecheckBlockers: ['Не сохранен идентификатор платежа у провайдера.'] }) ?? '', /идентификатор платежа/i)
  assert.match(getAdminOrderPaymentRecheckBlocker({ lastPaymentId: null, lastPaymentRecheckSupported: false, lastPaymentCanRecheck: false, lastPaymentRecheckBlockers: [] }) ?? '', /платежная попытка/)
})
