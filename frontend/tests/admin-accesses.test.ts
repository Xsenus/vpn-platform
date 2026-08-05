import test from 'node:test'
import assert from 'node:assert/strict'
import type { AccessCredentialDto } from '../packages/api-client/src/index.ts'
import { getAdminAccessCommandBlocker, getAdminAccessTerminalReason } from '../apps/admin-panel/src/admin-accesses.ts'

function access(overrides: Partial<AccessCredentialDto> = {}): AccessCredentialDto {
  return {
    id: 'access-1',
    subscriptionId: 'subscription-1',
    subscriptionStatus: 'Active',
    providerType: 'x3ui',
    providerAccessId: 'client-1',
    serverId: 'server-1',
    accessUri: 'vless://client@example.test',
    qrCodePath: 'qr://client',
    configPath: '/configs/client.json',
    status: 'Active',
    issuedAt: '2026-08-05T00:00:00Z',
    revision: 1,
    ...overrides
  }
}

test('cancelled parent subscription makes a stale active access terminal', () => {
  const staleAccess = access({ subscriptionStatus: 'Cancelled', status: 'Active', isTerminal: true })
  const reason = 'Родительская подписка отменена. Ключ и provider-команды скрыты; доступна только история.'

  assert.equal(getAdminAccessTerminalReason(staleAccess), reason)
  for (const command of ['copy', 'qr', 'enable', 'disable', 'sync', 'reset'] as const) {
    assert.equal(getAdminAccessCommandBlocker(staleAccess, command), reason)
  }
})

test('active access permits commands but requires a URI for copy and QR', () => {
  assert.equal(getAdminAccessTerminalReason(access()), null)
  assert.equal(getAdminAccessCommandBlocker(access(), 'sync'), null)
  assert.equal(getAdminAccessCommandBlocker(access({ accessUri: '' }), 'qr'), 'VPN URI ещё не выдан.')
})
