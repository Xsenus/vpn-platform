import test from 'node:test'
import assert from 'node:assert/strict'
import { SubscriptionDto } from '../packages/api-client/src/index.ts'
import { getAdminSubscriptionActionAvailability, getAdminSubscriptionActionBlocker } from '../apps/admin-panel/src/admin-subscriptions.ts'

function subscription(overrides: Partial<SubscriptionDto> = {}): SubscriptionDto {
  return {
    id: 'sub-1',
    userId: 'user-1',
    tariffId: 'tariff-1',
    status: 'Active',
    startAt: '2026-08-01T00:00:00Z',
    endAt: '2026-09-01T00:00:00Z',
    currentAccessId: 'access-1',
    ...overrides
  }
}

test('admin subscription actions follow the backend status contract', () => {
  const now = new Date('2026-08-05T00:00:00Z')

  assert.deepEqual(getAdminSubscriptionActionAvailability(subscription(), now), {
    isTerminal: false,
    canManage: true,
    canActivate: false,
    canExtend: true,
    canSync: true,
    canToggleBlock: true,
    canCancel: true,
    reason: null
  })
  assert.equal(getAdminSubscriptionActionAvailability(subscription({ status: 'Expired', endAt: '2026-08-01T00:00:00Z' }), now).canActivate, false)
  assert.equal(getAdminSubscriptionActionAvailability(subscription({ status: 'Expired', endAt: '2026-09-01T00:00:00Z' }), now).canActivate, true)
  assert.equal(getAdminSubscriptionActionAvailability(subscription({ currentAccessId: null }), now).canSync, false)
})

test('cancelled subscription exposes no mutation or provider command', () => {
  assert.deepEqual(
    getAdminSubscriptionActionAvailability(subscription({ status: 'Cancelled' })),
    {
      isTerminal: true,
      canManage: false,
      canActivate: false,
      canExtend: false,
      canSync: false,
      canToggleBlock: false,
      canCancel: false,
      reason: 'Отменённая подписка является терминальной. Доступны только просмотр и история.'
    }
  )
})

test('subscription action blocker rejects invalid commands before an API call', () => {
  assert.equal(getAdminSubscriptionActionBlocker(subscription(), 'sync'), null)
  assert.equal(
    getAdminSubscriptionActionBlocker(subscription({ currentAccessId: null }), 'sync'),
    'У подписки нет текущего VPN-доступа для синхронизации.'
  )
  assert.equal(
    getAdminSubscriptionActionBlocker(subscription({ status: 'Cancelled' }), 'sync'),
    'Отменённая подписка является терминальной. Доступны только просмотр и история.'
  )
})
