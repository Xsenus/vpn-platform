import test from 'node:test'
import assert from 'node:assert/strict'
import { AccessCredentialDto, CabinetSubscriptionDto } from '../packages/api-client/src/index.ts'
import { buildCabinetSummary, daysUntil, findAccessForSubscription, formatReferralRewardType, getAccessQrAvailability, getCabinetAccessTerminalReason, getEffectiveSubscriptionStatus, getNextCabinetAccessExpiryDelay, getSubscriptionRenewalAvailability, selectCurrentSubscription } from '../apps/cabinet/src/cabinet-dashboard.ts'

function subscription(overrides: Partial<CabinetSubscriptionDto>): CabinetSubscriptionDto {
  return {
    id: 'sub-1',
    tariffId: 'tariff-1',
    status: 'Active',
    startAt: '2026-05-01T00:00:00Z',
    endAt: '2026-06-01T00:00:00Z',
    createdAt: '2026-05-01T00:00:00Z',
    updatedAt: '2026-05-01T00:00:00Z',
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
  const now = new Date('2026-05-27T00:00:00Z')
  const current = subscription({ id: 'current', status: 'GracePeriod', endAt: '2026-05-29T00:00:00Z' })
  const old = subscription({ id: 'old', status: 'Expired', endAt: '2026-05-10T00:00:00Z' })

  assert.equal(selectCurrentSubscription([old, current], now)?.id, 'current')
})

test('cabinet dashboard does not treat expired subscriptions as current access', () => {
  const expired = subscription({ id: 'expired', status: 'Expired', endAt: '2026-05-10T00:00:00Z' })
  const summary = buildCabinetSummary([expired], [access({ subscriptionId: 'expired' })], new Date('2026-05-27T00:00:00Z'))

  assert.equal(selectCurrentSubscription([expired]), null)
  assert.equal(summary.currentSubscription, null)
  assert.equal(summary.currentAccess, null)
  assert.equal(summary.hasActiveSubscription, false)
  assert.equal(summary.hasConnectionLink, false)
})

test('cabinet dashboard stops exposing stale access at the grace-period boundary', () => {
  const now = new Date('2026-05-27T12:00:00Z')
  const stale = subscription({
    status: 'GracePeriod',
    endAt: '2026-05-24T12:00:00Z',
    gracePeriodEndAt: '2026-05-27T12:00:00Z',
    currentAccessId: 'access-stale',
    accessUri: 'vless://stale-secret'
  })
  const staleAccess = access({
    id: 'access-stale',
    expiryDate: '2026-05-27T12:00:00Z',
    accessUri: 'vless://stale-secret'
  })

  const summary = buildCabinetSummary([stale], [staleAccess], now)

  assert.equal(summary.currentSubscription, null)
  assert.equal(summary.currentAccess, null)
  assert.equal(summary.hasActiveSubscription, false)
  assert.equal(summary.hasConnectionLink, false)
  assert.equal(getEffectiveSubscriptionStatus(stale, now), 'Expired')
  assert.equal(getCabinetAccessTerminalReason(staleAccess, 'Expired', now), 'Срок VPN-доступа истёк. Ключ и QR-код больше недоступны.')
  assert.deepEqual(getAccessQrAvailability(staleAccess, now), {
    canGenerate: false,
    reason: 'Срок VPN-доступа истёк. Ключ и QR-код больше недоступны.'
  })
  assert.equal(getNextCabinetAccessExpiryDelay([
    subscription({ endAt: '2026-05-27T12:05:00Z' }),
    subscription({ endAt: '2026-05-27T12:10:00Z' })
  ], [], now), 5 * 60 * 1000)
})

test('cabinet dashboard prefers the latest active subscription', () => {
  const now = new Date('2026-05-27T00:00:00Z')
  const expiringSoon = subscription({ id: 'soon', status: 'Active', endAt: '2026-05-28T00:00:00Z' })
  const latest = subscription({ id: 'latest', status: 'Active', endAt: '2026-06-30T00:00:00Z' })

  assert.equal(selectCurrentSubscription([expiringSoon, latest], now)?.id, 'latest')
})

test('cabinet dashboard links access by currentAccessId and reports days left', () => {
  const current = subscription({ id: 'sub-2', currentAccessId: 'access-2', endAt: '2026-05-30T00:00:00Z' })
  const linkedAccess = access({ id: 'access-2', subscriptionId: 'sub-2', accessUri: 'vless://linked' })

  assert.equal(findAccessForSubscription(current, [linkedAccess], new Date('2026-05-27T00:00:00Z'))?.accessUri, 'vless://linked')
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

test('cabinet dashboard exposes renewal only for backend-supported subscription statuses', () => {
  assert.deepEqual(getSubscriptionRenewalAvailability(subscription({ status: 'Active' })), { canRenew: true, reason: null })
  assert.deepEqual(getSubscriptionRenewalAvailability(subscription({ status: 'Expired' })), { canRenew: true, reason: null })
  assert.deepEqual(getSubscriptionRenewalAvailability(subscription({ status: 'Blocked' })), {
    canRenew: false,
    reason: 'Продление заблокировано. Обратитесь в поддержку.'
  })
  assert.deepEqual(getSubscriptionRenewalAvailability(subscription({ status: 'Cancelled' })), {
    canRenew: false,
    reason: 'Отменённую подписку нельзя продлить. Оформите новый тариф.'
  })
})

test('cabinet dashboard enables QR only after the access URI is issued', () => {
  assert.deepEqual(getAccessQrAvailability(access({ accessUri: ' vless://issued ' })), { canGenerate: true, reason: null })
  assert.deepEqual(getAccessQrAvailability(access({ status: 'Revoked', accessUri: 'vless://revoked' })), {
    canGenerate: false,
    reason: 'Доступ отозван. Ссылка подключения и QR-код больше недоступны.'
  })
  assert.deepEqual(getAccessQrAvailability(access({ accessUri: '' })), {
    canGenerate: false,
    reason: 'QR-код появится после выдачи ссылки подключения.'
  })
  assert.deepEqual(getAccessQrAvailability(null), {
    canGenerate: false,
    reason: 'QR-код появится после выдачи ссылки подключения.'
  })

  const revoked = access({ id: 'access-revoked', status: 'Revoked', accessUri: 'vless://revoked-secret' })
  const staleSubscription = subscription({ currentAccessId: revoked.id, accessUri: 'vless://revoked-secret' })
  assert.equal(findAccessForSubscription(staleSubscription, [revoked]), null)
  assert.equal(buildCabinetSummary([staleSubscription], [revoked]).hasConnectionLink, false)
})

test('cabinet treats stale active access of a cancelled subscription as terminal', () => {
  const staleAccess = access({
    id: 'access-cancelled',
    subscriptionStatus: 'Cancelled',
    isTerminal: true,
    status: 'Active',
    accessUri: 'vless://cancelled-stale-secret'
  })
  const cancelled = subscription({
    status: 'Cancelled',
    currentAccessId: staleAccess.id,
    accessUri: 'vless://cancelled-stale-secret'
  })

  assert.equal(getCabinetAccessTerminalReason(staleAccess), 'Родительская подписка отменена. Ключ и QR-код больше недоступны.')
  assert.deepEqual(getAccessQrAvailability(staleAccess), {
    canGenerate: false,
    reason: 'Родительская подписка отменена. Ключ и QR-код больше недоступны.'
  })
  assert.equal(findAccessForSubscription(cancelled, [staleAccess]), null)
  assert.equal(buildCabinetSummary([cancelled], [staleAccess]).hasConnectionLink, false)
})

test('referral reward types remain user-facing', () => {
  assert.equal(formatReferralRewardType('bonus-days'), 'Бонусные дни')
  assert.equal(formatReferralRewardType('cashback'), 'Кэшбэк')
  assert.equal(formatReferralRewardType('custom-ledger-type'), 'Реферальное начисление')
})
