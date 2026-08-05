import { SubscriptionDto } from '@vpn-platform/api-client'

const mutableStatuses = new Set([
  'PendingActivation',
  'Active',
  'GracePeriod',
  'Expired',
  'Suspended',
  'Blocked'
])

export type AdminSubscriptionAction = 'activate' | 'extend' | 'block' | 'unblock' | 'cancel' | 'sync'

export function getAdminSubscriptionActionAvailability(subscription: SubscriptionDto, now = new Date()) {
  const isTerminal = subscription.status === 'Cancelled'
  const canManage = mutableStatuses.has(subscription.status) && !isTerminal
  const endAt = new Date(subscription.endAt).getTime()
  const periodIsActive = Number.isFinite(endAt) && endAt > now.getTime()

  return {
    isTerminal,
    canManage,
    canActivate: canManage && subscription.status !== 'Active' && periodIsActive,
    canExtend: canManage,
    canSync: canManage && Boolean(subscription.currentAccessId),
    canToggleBlock: canManage,
    canCancel: canManage,
    reason: isTerminal
      ? 'Отменённая подписка является терминальной. Доступны только просмотр и история.'
      : canManage ? null : 'Для неизвестного статуса подписки административные команды недоступны.'
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
        : action === 'cancel'
          ? availability.canCancel
          : availability.canToggleBlock

  if (allowed) return null
  if (availability.reason) return availability.reason
  if (action === 'activate') return 'Период подписки завершён или подписка уже активна.'
  if (action === 'sync') return 'У подписки нет текущего VPN-доступа для синхронизации.'
  return 'Команда недоступна для текущего статуса подписки.'
}
