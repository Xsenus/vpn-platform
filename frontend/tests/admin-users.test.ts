import test from 'node:test'
import assert from 'node:assert/strict'
import { AdminUserOverviewDto } from '../packages/api-client/src/index.ts'
import { buildAdminUserOverviewStats, formatAdminMoney, telegramDisplayName } from '../apps/admin-panel/src/admin-users.ts'

const now = '2026-06-10T12:00:00Z'

function overview(overrides: Partial<AdminUserOverviewDto> = {}): AdminUserOverviewDto {
  return {
    user: {
      id: 'user-1',
      email: 'client@example.test',
      displayName: 'Client',
      rolesCsv: 'User',
      status: 'Active',
      isBlocked: false,
      preferredLanguage: 'ru',
      referralCode: 'REF1',
      authSource: 'Telegram',
      emailConfirmed: true,
      createdAt: now,
      updatedAt: now
    },
    telegramAccounts: [],
    orders: [],
    payments: [],
    subscriptions: [],
    accessCredentials: [],
    supportConversations: [],
    ...overrides
  }
}

test('admin user overview stats aggregates commercial and effective access state', () => {
  const result = buildAdminUserOverviewStats(overview({
    telegramAccounts: [
      { id: 'tg-1', telegramUserId: 777001, username: 'client_tg', firstName: '', lastName: '', languageCode: 'ru', isBlocked: true, linkedAt: now, lastSeenAt: now }
    ],
    orders: [
      { id: 'order-1', userId: 'user-1', tariffId: 'tariff-1', tariffName: 'Premium', amount: 490, currency: 'RUB', status: 'Completed', expiresAt: now, createdAt: now }
    ],
    payments: [
      { id: 'pay-1', orderId: 'order-1', provider: 'YooKassa', providerPaymentId: 'pay_1', externalEventId: 'evt_1', amount: 490, currency: 'RUB', status: 'Succeeded', signatureValidated: true, createdAt: now, updatedAt: now },
      { id: 'pay-2', orderId: 'order-1', provider: 'YooKassa', providerPaymentId: 'pay_2', externalEventId: 'evt_2', amount: 490, currency: 'RUB', status: 'Failed', signatureValidated: false, createdAt: now, updatedAt: now }
    ],
    subscriptions: [
      { id: 'sub-1', userId: 'user-1', tariffId: 'tariff-1', tariffName: 'Premium', status: 'Active', startAt: now, endAt: '2026-07-10T12:00:00Z' },
      { id: 'sub-2', userId: 'user-1', tariffId: 'tariff-1', tariffName: 'Premium', status: 'Blocked', startAt: now, endAt: now },
      { id: 'sub-stale', userId: 'user-1', tariffId: 'tariff-1', tariffName: 'Premium', status: 'Active', startAt: now, endAt: now }
    ],
    accessCredentials: [
      { id: 'access-1', subscriptionId: 'sub-1', providerType: 'x3ui', providerAccessId: 'client-1', serverId: 'node-1', accessUri: 'vless://client', qrCodePath: 'vless://client', configPath: '', status: 'Active', issuedAt: now, expiryDate: '2026-07-10T12:00:00Z', revision: 1 },
      { id: 'access-stale', subscriptionId: 'sub-stale', subscriptionStatus: 'Active', isTerminal: false, providerType: 'x3ui', providerAccessId: 'client-stale', serverId: 'node-1', accessUri: 'vless://stale', qrCodePath: 'vless://stale', configPath: '', status: 'Active', issuedAt: now, expiryDate: now, revision: 1 }
    ],
    supportConversations: [
      { id: 'support-1', userId: 'user-1', telegramUserId: 777001, channel: 'telegram', status: 'open', subject: 'Help', assignedToUserId: null, internalNote: 'VIP', createdAt: now, updatedAt: now }
    ]
  }), new Date(now))

  assert.equal(result.ordersCount, 1)
  assert.equal(result.paymentsCount, 2)
  assert.equal(result.activeSubscriptionsCount, 1)
  assert.equal(result.activeAccessesCount, 1)
  assert.equal(result.totalPaidAmount, 490)
  assert.equal(result.needsAttention, true)
  assert.deepEqual(result.attentionReasons, [
    'проблемные платежи: 1',
    'ограниченные подписки: 1',
    'заблокированные Telegram: 1',
    'открытая поддержка: 1'
  ])
})

test('admin user helpers format money and Telegram display names', () => {
  assert.equal(formatAdminMoney(490, 'RUB'), '490 ₽')
  assert.equal(formatAdminMoney(12.5, 'broken'), '12,50 ₽')
  assert.equal(telegramDisplayName({ id: 'tg-1', telegramUserId: 777001, username: 'client_tg', firstName: '', lastName: '', languageCode: 'ru', isBlocked: false }), '@client_tg')
  assert.equal(telegramDisplayName({ id: 'tg-2', telegramUserId: 777002, username: '', firstName: 'Ivan', lastName: 'Petrov', languageCode: 'ru', isBlocked: false }), 'Ivan Petrov')
})
