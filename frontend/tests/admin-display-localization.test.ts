import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import { formatAdminDisplayLabel, formatAdminRoleLabels } from '../apps/admin-panel/src/admin-display-labels'

test('admin display formatter localizes roles, auth sources and VPN client states', () => {
  assert.equal(formatAdminDisplayLabel('synced'), 'Синхронизирован')
  assert.equal(formatAdminDisplayLabel('traffic-reset'), 'Счётчики сброшены')
  assert.equal(formatAdminDisplayLabel('migration-compensation-failed'), 'Нужна ручная сверка переноса')
  assert.equal(formatAdminDisplayLabel('Local'), 'Локальная учётная запись')
  assert.equal(formatAdminDisplayLabel('Imported'), 'Импортированная учётная запись')
  assert.equal(formatAdminDisplayLabel('EmailConfirmed'), 'Email подтверждён')
  assert.equal(formatAdminRoleLabels('SuperAdmin,SupportAgent'), 'Суперадминистратор, Сотрудник поддержки')
  assert.equal(formatAdminRoleLabels(''), 'Пользователь')
})

test('admin user-facing metadata does not interpolate technical enum values directly', () => {
  const source = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  for (const rawInterpolation of [
    /\{account\.mode\}/,
    /\{order\.channel \?\?/,
    /\{order\.type \?\?/,
    /\{conversation\.channel\}/,
    /\{message\.direction\}/,
    /\{item\.type\}:/,
    /\{subscription\.sourceChannel \|\|/,
    /\$\{saved\.syncStatus\}/,
    /\{client\.syncStatus \|\|/
  ]) {
    assert.doesNotMatch(source, rawInterpolation)
  }

  assert.doesNotMatch(source, /статус \$\{(?:payment|refund|result|saved|check)\.status\}/)
  assert.doesNotMatch(source, /\$\{userOverviewStats\.(?:activeSubscriptionsCount|paymentsCount|activeAccessesCount|telegramAccountsCount)\} (?:active|payments|accounts)/)
  assert.doesNotMatch(source, /\bsync \{formatDate\(access\.lastSyncedAt\)\}/)
  assert.doesNotMatch(source, /\{s\((?:user|userOverview\.user)\.authSource\)\}/)
  assert.doesNotMatch(source, /value=\{s\(user\.rolesCsv/)
})
