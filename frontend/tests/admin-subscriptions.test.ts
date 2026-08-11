import test from 'node:test'
import assert from 'node:assert/strict'
import { SubscriptionDto } from '../packages/api-client/src/index.ts'
import { getAdminSubscriptionActionAvailability, getAdminSubscriptionActionBlocker, getNextAdminSubscriptionExpiryDelay } from '../apps/admin-panel/src/admin-subscriptions.ts'

function subscription(overrides: Partial<SubscriptionDto> = {}): SubscriptionDto {
  return {
    id: 'sub-1',
    userId: 'user-1',
    tariffId: 'tariff-1',
    status: 'Active',
    startAt: '2026-08-01T00:00:00Z',
    endAt: '2026-09-01T00:00:00Z',
    currentAccessId: 'access-1',
    currentServerId: 'server-1',
    ...overrides
  }
}

test('admin subscription actions follow the backend status contract', () => {
  const now = new Date('2026-08-05T00:00:00Z')

  assert.deepEqual(getAdminSubscriptionActionAvailability(subscription(), now), {
    isTerminal: false,
    canManage: true,
    isAccessAvailable: true,
    isEffectivelyExpired: false,
    canActivate: false,
    canExtend: true,
    canSync: true,
    canMigrate: true,
    canToggleBlock: true,
    canCancel: true,
    reason: null
  })
  assert.equal(getAdminSubscriptionActionAvailability(subscription({ status: 'Expired', endAt: '2026-08-01T00:00:00Z' }), now).canActivate, false)
  assert.equal(getAdminSubscriptionActionAvailability(subscription({ status: 'Expired', endAt: '2026-09-01T00:00:00Z' }), now).canActivate, true)
  assert.equal(getAdminSubscriptionActionAvailability(subscription({ currentAccessId: null }), now).canSync, false)
})

test('admin subscription provider commands follow the effective grace deadline', () => {
  const now = new Date('2026-08-05T00:00:00Z')
  const graceSubscription = subscription({
    status: 'GracePeriod',
    endAt: '2026-08-02T00:00:00Z',
    gracePeriodEndAt: '2026-08-06T00:00:00Z'
  })
  const expiredSubscription = subscription({
    status: 'GracePeriod',
    endAt: '2026-08-02T00:00:00Z',
    gracePeriodEndAt: now.toISOString()
  })

  assert.equal(getAdminSubscriptionActionAvailability(graceSubscription, now).isAccessAvailable, true)
  assert.equal(getAdminSubscriptionActionAvailability(graceSubscription, now).canMigrate, true)
  assert.equal(getAdminSubscriptionActionAvailability(expiredSubscription, now).isEffectivelyExpired, true)
  assert.equal(getAdminSubscriptionActionAvailability(expiredSubscription, now).canSync, false)
  assert.equal(getAdminSubscriptionActionAvailability(expiredSubscription, now).canMigrate, false)
  assert.equal(
    getAdminSubscriptionActionBlocker(expiredSubscription, 'sync', now),
    'Срок VPN-доступа подписки истёк. Синхронизация и миграция недоступны; можно продлить, ограничить или отменить подписку.'
  )
  assert.equal(
    getAdminSubscriptionActionBlocker(expiredSubscription, 'migrate', now),
    'Срок VPN-доступа подписки истёк. Синхронизация и миграция недоступны; можно продлить, ограничить или отменить подписку.'
  )
  assert.equal(getNextAdminSubscriptionExpiryDelay([graceSubscription], now), 24 * 60 * 60 * 1000)
})

test('cancelled subscription exposes no mutation or provider command', () => {
  assert.deepEqual(
    getAdminSubscriptionActionAvailability(subscription({ status: 'Cancelled' })),
    {
      isTerminal: true,
      canManage: false,
      isAccessAvailable: false,
      isEffectivelyExpired: false,
      canActivate: false,
      canExtend: false,
      canSync: false,
      canMigrate: false,
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
