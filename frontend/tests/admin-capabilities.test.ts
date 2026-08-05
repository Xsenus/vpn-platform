import test from 'node:test'
import assert from 'node:assert/strict'
import type { AdminSessionCapabilitiesDto } from '../packages/api-client/src/index.ts'
import { canWriteAdminSection, filterAdminSectionIds, type AdminSectionId } from '../apps/admin-panel/src/admin-capabilities.ts'

const allSections: AdminSectionId[] = ['dashboard', 'users', 'support', 'audit', 'payments', 'tariffs', 'referrals', 'subscriptions', 'vpn', 'nodes', 'panels', 'provisioning', 'bot', 'releases', 'faq', 'content', 'scenarios']

function capabilities(overrides: Partial<AdminSessionCapabilitiesDto>): AdminSessionCapabilitiesDto {
  return {
    adminRead: true,
    adminWrite: false,
    financeRead: false,
    financeWrite: false,
    supportRead: false,
    supportWrite: false,
    provisioningManage: false,
    vpnManage: false,
    botManage: false,
    settingsManage: false,
    ...overrides
  }
}

test('finance role sees finance and common read sections but no support or bot', () => {
  const access = capabilities({ financeRead: true, financeWrite: true })
  const visible = filterAdminSectionIds(access, allSections)

  assert.ok(visible.includes('payments'))
  assert.ok(visible.includes('users'))
  assert.ok(!visible.includes('support'))
  assert.ok(!visible.includes('bot'))
  assert.equal(canWriteAdminSection(access, 'payments'), true)
  assert.equal(canWriteAdminSection(access, 'tariffs'), false)
  assert.equal(canWriteAdminSection(access, 'referrals'), false)
})

test('support role sees support and common read sections in read-only mode elsewhere', () => {
  const access = capabilities({ supportRead: true, supportWrite: true })
  const visible = filterAdminSectionIds(access, allSections)

  assert.ok(visible.includes('support'))
  assert.ok(!visible.includes('payments'))
  assert.ok(!visible.includes('bot'))
  assert.equal(canWriteAdminSection(access, 'support'), true)
  assert.equal(canWriteAdminSection(access, 'users'), false)
})

test('read-only role sees finance and support data without mutation rights', () => {
  const access = capabilities({ financeRead: true, supportRead: true })
  const visible = filterAdminSectionIds(access, allSections)

  assert.ok(visible.includes('payments'))
  assert.ok(visible.includes('support'))
  assert.ok(!visible.includes('bot'))
  assert.equal(visible.every((section) => canWriteAdminSection(access, section) === (section === 'dashboard')), true)
})

test('operator sees bot and operational sections without finance data', () => {
  const access = capabilities({ adminWrite: true, supportRead: true, supportWrite: true, provisioningManage: true, vpnManage: true, botManage: true })
  const visible = filterAdminSectionIds(access, allSections)

  assert.ok(visible.includes('bot'))
  assert.ok(visible.includes('provisioning'))
  assert.ok(!visible.includes('payments'))
  assert.equal(canWriteAdminSection(access, 'panels'), true)
})
