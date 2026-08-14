import { formatStatusLabel } from '@vpn-platform/ui'

const adminDisplayLabels: Record<string, string> = {
  synced: 'Синхронизирован',
  'sandbox-synced': 'Синхронизирован (проверка)',
  'traffic-reset': 'Счётчики сброшены',
  'sandbox-traffic-reset': 'Счётчики сброшены (проверка)',
  migrated: 'Перенесён',
  'sandbox-migrated': 'Перенесён (проверка)',
  'already-on-target': 'Уже на выбранной точке',
  'sandbox-enabled': 'Включён (проверка)',
  'sandbox-disabled': 'Выключен (проверка)',
  requiresadminreview: 'Нужна ручная проверка',
  'client-state-compensation-failed': 'Нужна ручная сверка состояния',
  'traffic-reset-uncertain': 'Нужна ручная сверка сброса трафика',
  'migration-compensation-failed': 'Нужна ручная сверка переноса',
  local: 'Локальная учётная запись',
  imported: 'Импортированная учётная запись',
  user: 'Пользователь',
  superadmin: 'Суперадминистратор',
  admin: 'Администратор',
  supportagent: 'Сотрудник поддержки',
  operator: 'Оператор',
  financemanager: 'Финансовый менеджер',
  readonly: 'Только чтение',
  emailconfirmed: 'Email подтверждён',
  emailnotconfirmed: 'Email не подтверждён'
}

export function formatAdminDisplayLabel(value: unknown) {
  const raw = String(value ?? 'Unknown').trim()
  return adminDisplayLabels[raw.toLowerCase()] ?? formatStatusLabel(raw)
}

export function formatAdminRoleLabels(value: unknown) {
  const roles = String(value ?? '')
    .split(',')
    .map((role) => role.trim())
    .filter(Boolean)

  return (roles.length > 0 ? roles : ['User']).map(formatAdminDisplayLabel).join(', ')
}
