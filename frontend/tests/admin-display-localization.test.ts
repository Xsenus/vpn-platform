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

test('admin display formatter localizes bounded operational metadata', () => {
  assert.equal(formatAdminDisplayLabel('admin'), 'Администратор')
  assert.equal(formatAdminDisplayLabel('system'), 'Система')
  assert.equal(formatAdminDisplayLabel('finance'), 'Финансы')
  assert.equal(formatAdminDisplayLabel('Strict'), 'Строгая проверка')
  assert.equal(formatAdminDisplayLabel('AllowSelfSigned'), 'Разрешён самоподписанный сертификат')
  assert.equal(formatAdminDisplayLabel('AccessRevoked'), 'Доступ отозван')
  assert.equal(formatAdminDisplayLabel('AccessTrafficResetFailed'), 'Сброс трафика не выполнен')
  assert.equal(formatAdminDisplayLabel('QrDisabled'), 'QR не создаётся')
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
  assert.doesNotMatch(source, /<option value="(?:Active|Suspended|Deleted|New)">(?:Active|Suspended|Deleted|New)<\/option>/)
  assert.doesNotMatch(source, /<span>Actor<\/span>|>admin<\/option>|>system<\/option>|>user<\/option>/)
  assert.doesNotMatch(source, /actor: \{entry\.actorType\}|entry\.(?:actorId|actorType) \|\| 'unknown'/)
  assert.doesNotMatch(source, /SSL \{panel\.sslVerificationMode\}/)
  assert.doesNotMatch(source, /\$\{h\.eventType\}/)
  assert.doesNotMatch(source, /'No QR'/)
  assert.doesNotMatch(source, /\|\| 'Support'|\|\| 'ok'/)
  assert.doesNotMatch(source, /<StatusBadge value="(?:finance|system|support|bot|vpn)"/)
})
