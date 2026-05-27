import test from 'node:test'
import assert from 'node:assert/strict'
import { AccessCredentialDto, SubscriptionDto } from '../packages/api-client/src/index.ts'
import { buildCabinetSummary, daysUntil, findAccessForSubscription, selectCurrentSubscription } from '../apps/cabinet/src/cabinet-dashboard.ts'

function subscription(overrides: Partial<SubscriptionDto>): SubscriptionDto {
  return {
    id: 'sub-1',
    userId: 'user-1',
    tariffId: 'tariff-1',
    status: 'Active',
    startAt: '2026-05-01T00:00:00Z',
    endAt: '2026-06-01T00:00:00Z',
    ...overrides
  }
}

function access(overrides: Partial<AccessCredentialDto>): AccessCredentialDto {
  return {
    id: 'access-1',
    subscriptionId: 'sub-1',
    providerType: 'x3ui',
    providerAccessId: 'client-1',
    serverId: 'node-1',
    accessUri: 'vless://client',
    qrCodePath: 'vless://client',
    configPath: '',
    status: 'Active',
    issuedAt: '2026-05-01T00:00:00Z',
    revision: 1,
    ...overrides
  }
}

test('cabinet dashboard selects current subscription before old records', () => {
  const current = subscription({ id: 'current', status: 'GracePeriod', endAt: '2026-05-29T00:00:00Z' })
  const old = subscription({ id: 'old', status: 'Expired', endAt: '2026-05-10T00:00:00Z' })

  assert.equal(selectCurrentSubscription([old, current])?.id, 'current')
})

test('cabinet dashboard links access by currentAccessId and reports days left', () => {
  const current = subscription({ id: 'sub-2', currentAccessId: 'access-2', endAt: '2026-05-30T00:00:00Z' })
  const linkedAccess = access({ id: 'access-2', subscriptionId: 'sub-2', accessUri: 'vless://linked' })

  assert.equal(findAccessForSubscription(current, [linkedAccess])?.accessUri, 'vless://linked')
  assert.equal(daysUntil(current.endAt, new Date('2026-05-27T00:00:00Z')), 3)

  const summary = buildCabinetSummary([current], [linkedAccess], new Date('2026-05-27T00:00:00Z'))
  assert.equal(summary.currentSubscription?.id, 'sub-2')
  assert.equal(summary.currentAccess?.id, 'access-2')
  assert.equal(summary.hasActiveSubscription, true)
  assert.equal(summary.hasConnectionLink, true)
  assert.equal(summary.daysLeft, 3)
})

test('cabinet dashboard returns empty summary when user has no subscription', () => {
  const summary = buildCabinetSummary([], [], new Date('2026-05-27T00:00:00Z'))

  assert.equal(summary.currentSubscription, null)
  assert.equal(summary.currentAccess, null)
  assert.equal(summary.hasActiveSubscription, false)
  assert.equal(summary.hasConnectionLink, false)
})
