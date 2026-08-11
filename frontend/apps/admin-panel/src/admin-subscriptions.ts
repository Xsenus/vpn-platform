import { SubscriptionDto } from '@vpn-platform/api-client'

const mutableStatuses = new Set([
  'PendingActivation',
  'Active',
  'GracePeriod',
  'Expired',
  'Suspended',
  'Blocked'
])

export type AdminSubscriptionAction = 'activate' | 'extend' | 'block' | 'unblock' | 'cancel' | 'sync' | 'migrate'

export function getAdminSubscriptionEffectiveEndTime(subscription: SubscriptionDto) {
  return Date.parse(subscription.gracePeriodEndAt ?? subscription.endAt)
}

export function getNextAdminSubscriptionExpiryDelay(subscriptions: SubscriptionDto[], now = new Date()) {
  const nowTime = now.getTime()
  const delays = subscriptions
    .filter((subscription) => subscription.status === 'Active' || subscription.status === 'GracePeriod')
    .map(getAdminSubscriptionEffectiveEndTime)
    .filter((expiryTime) => Number.isFinite(expiryTime) && expiryTime > nowTime)
    .map((expiryTime) => expiryTime - nowTime)

  return delays.length > 0 ? Math.min(...delays) : null
}

export function getAdminSubscriptionActionAvailability(subscription: SubscriptionDto, now = new Date()) {
  const isTerminal = subscription.status === 'Cancelled'
  const canManage = mutableStatuses.has(subscription.status) && !isTerminal
  const endAt = new Date(subscription.endAt).getTime()
  const periodIsActive = Number.isFinite(endAt) && endAt > now.getTime()
  const effectiveEndAt = getAdminSubscriptionEffectiveEndTime(subscription)
  const isEffectivelyExpired = !isTerminal
    && (subscription.status === 'Expired' || !Number.isFinite(effectiveEndAt) || effectiveEndAt <= now.getTime())
  const isAccessAvailable = !isTerminal
    && !isEffectivelyExpired
    && (subscription.status === 'Active' || subscription.status === 'GracePeriod')

  return {
    isTerminal,
    canManage,
    isAccessAvailable,
    isEffectivelyExpired,
    canActivate: canManage && subscription.status !== 'Active' && periodIsActive,
    canExtend: canManage,
    canSync: canManage && isAccessAvailable && Boolean(subscription.currentAccessId),
    canMigrate: canManage && isAccessAvailable && Boolean(subscription.currentAccessId) && Boolean(subscription.currentServerId),
    canToggleBlock: canManage,
    canCancel: canManage,
    reason: isTerminal
      ? 'Отменённая подписка является терминальной. Доступны только просмотр и история.'
      : !canManage
        ? 'Для неизвестного статуса подписки административные команды недоступны.'
        : isEffectivelyExpired
          ? 'Срок VPN-доступа подписки истёк. Синхронизация и миграция недоступны; можно продлить, ограничить или отменить подписку.'
          : null
  }
}

export function getAdminSubscriptionActionBlocker(
  subscription: SubscriptionDto,
  action: AdminSubscriptionAction,
  now = new Date()
) {
  const availability = getAdminSubscriptionActionAvailability(subscription, now)
  const allowed = action === 'activate'
    ? availability.canActivate
    : action === 'extend'
      ? availability.canExtend
      : action === 'sync'
        ? availability.canSync
        : action === 'migrate'
          ? availability.canMigrate
        : action === 'cancel'
          ? availability.canCancel
          : availability.canToggleBlock

  if (allowed) return null
  if (availability.reason) return availability.reason
  if (action === 'activate') return 'Период подписки завершён или подписка уже активна.'
  if ((action === 'sync' || action === 'migrate') && !availability.isAccessAvailable) {
    return 'VPN-доступ подписки неактивен. Provider-команда недоступна.'
  }
  if (action === 'sync') return 'У подписки нет текущего VPN-доступа для синхронизации.'
  if (action === 'migrate') return 'У подписки нет текущего VPN-доступа или сервера для миграции.'
  return 'Команда недоступна для текущего статуса подписки.'
}
