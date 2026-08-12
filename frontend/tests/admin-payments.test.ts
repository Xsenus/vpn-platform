import test from 'node:test'
import assert from 'node:assert/strict'
import { getAdminOrderPaymentRecheckBlocker, getAdminPaymentRecheckBlocker } from '../apps/admin-panel/src/admin-payments.ts'

test('admin payment recheck blockers fail closed for unsupported or incomplete payments', () => {
  assert.equal(getAdminPaymentRecheckBlocker({ recheckSupported: true }), null)
  assert.match(getAdminPaymentRecheckBlocker({ recheckSupported: false }) ?? '', /не поддерживает ручную перепроверку/)
  assert.equal(getAdminOrderPaymentRecheckBlocker({ lastPaymentId: 'payment-1', lastPaymentRecheckSupported: true }), null)
  assert.match(getAdminOrderPaymentRecheckBlocker({ lastPaymentId: 'payment-1', lastPaymentRecheckSupported: false }) ?? '', /не поддерживает ручную перепроверку/)
  assert.match(getAdminOrderPaymentRecheckBlocker({ lastPaymentId: null, lastPaymentRecheckSupported: false }) ?? '', /платежная попытка/)
})
