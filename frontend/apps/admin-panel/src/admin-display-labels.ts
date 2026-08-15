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
  system: 'Система',
  finance: 'Финансы',
  support: 'Поддержка',
  bot: 'Telegram-бот',
  vpn: 'VPN',
  common: 'Общее',
  user: 'Пользователь',
  superadmin: 'Суперадминистратор',
  admin: 'Администратор',
  supportagent: 'Сотрудник поддержки',
  operator: 'Оператор',
  financemanager: 'Финансовый менеджер',
  readonly: 'Только чтение',
  emailconfirmed: 'Email подтверждён',
  emailnotconfirmed: 'Email не подтверждён',
  strict: 'Строгая проверка',
  allowselfsigned: 'Разрешён самоподписанный сертификат',
  qrenabled: 'QR создаётся',
  qrdisabled: 'QR не создаётся',
  accesscreated: 'Доступ создан',
  accesscreatecleanupfailed: 'Не удалось очистить выданный доступ',
  accessupdated: 'Доступ обновлён',
  accessdisabled: 'Доступ выключен',
  accessdisableskipped: 'Доступ уже выключен',
  accessdisablecancelled: 'Выключение доступа отменено',
  accessdisablefailed: 'Не удалось выключить доступ',
  accessdisabledonexpiry: 'Доступ выключен после окончания подписки',
  accessdisablefailedonexpiry: 'Не удалось выключить доступ после окончания подписки',
  accessdisabledonsubscriptionblock: 'Доступ выключен при блокировке подписки',
  accessenabled: 'Доступ включён',
  accessenablecancelled: 'Включение доступа отменено',
  accessenablefailed: 'Не удалось включить доступ',
  accessrenewedandenabled: 'Доступ продлён и включён',
  accessrevoked: 'Доступ отозван',
  accessrevokedonsubscriptioncancel: 'Доступ отозван при отмене подписки',
  accessrevokeuncertainonsubscriptioncancel: 'Отзыв доступа требует ручной сверки',
  accesssynced: 'Доступ синхронизирован',
  accesssynccancelled: 'Синхронизация доступа отменена',
  accesssyncfailed: 'Не удалось синхронизировать доступ',
  accesstrafficreset: 'Счётчики трафика сброшены',
  accesstrafficresetcancelled: 'Сброс трафика отменён',
  accesstrafficresetfailed: 'Сброс трафика не выполнен',
  'least-loaded': 'Наименее загруженный сервер',
  default: 'Основное inbound-правило',
  create_subscription_and_access: 'Создать подписку и VPN-доступ',
  keep_order_pending: 'Оставить заказ в ожидании',
  disable_access: 'Отключить VPN-доступ',
  disable_access_after_grace: 'Отключить доступ после льготного периода',
  extend_subscription: 'Продлить подписку'
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
