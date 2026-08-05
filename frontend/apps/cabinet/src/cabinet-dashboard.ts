import { AccessCredentialDto, SubscriptionDto } from '@vpn-platform/api-client'

export function isCurrentSubscription(subscription: SubscriptionDto) {
  return subscription.status === 'Active' || subscription.status === 'GracePeriod'
}

export function formatReferralRewardType(type: string) {
  const labels: Record<string, string> = {
    'bonus-days': 'Бонусные дни',
    cashback: 'Кэшбэк',
    discount: 'Скидка'
  }
  return labels[type.trim().toLowerCase()] ?? 'Реферальное начисление'
}

export function getSubscriptionRenewalAvailability(subscription: SubscriptionDto) {
  if (subscription.status === 'Blocked') {
    return {
      canRenew: false,
      reason: 'Продление заблокировано. Обратитесь в поддержку.'
    }
  }

  if (subscription.status === 'Cancelled') {
    return {
      canRenew: false,
      reason: 'Отменённую подписку нельзя продлить. Оформите новый тариф.'
    }
  }

  return { canRenew: true, reason: null }
}

export function getCabinetAccessTerminalReason(
  access: Pick<AccessCredentialDto, 'status' | 'subscriptionStatus' | 'isTerminal'> | null | undefined,
  subscriptionStatus?: string | null
) {
  if (access?.status === 'Revoked') {
    return 'Доступ отозван. Ключ и QR-код больше недоступны.'
  }

  if (subscriptionStatus === 'Cancelled' || access?.subscriptionStatus === 'Cancelled') {
    return 'Родительская подписка отменена. Ключ и QR-код больше недоступны.'
  }

  if (access?.isTerminal) {
    return 'VPN-доступ завершён. Ключ и QR-код больше недоступны.'
  }

  return null
}

export function getAccessQrAvailability(access: Pick<AccessCredentialDto, 'accessUri' | 'status' | 'subscriptionStatus' | 'isTerminal'> | null | undefined) {
  if (access?.status === 'Revoked') {
    return {
      canGenerate: false,
      reason: 'Доступ отозван. Ссылка подключения и QR-код больше недоступны.'
    }
  }

  const terminalReason = getCabinetAccessTerminalReason(access)
  if (terminalReason) return { canGenerate: false, reason: terminalReason }

  const canGenerate = Boolean(access?.accessUri?.trim())
  return {
    canGenerate,
    reason: canGenerate ? null : 'QR-код появится после выдачи ссылки подключения.'
  }
}

export function selectCurrentSubscription(subscriptions: SubscriptionDto[]) {
  const sorted = subscriptions.filter(isCurrentSubscription).sort((left, right) => {
    const leftEndAt = new Date(left.endAt).getTime()
    const rightEndAt = new Date(right.endAt).getTime()
    return rightEndAt - leftEndAt
  })

  return sorted[0] ?? null
}

export function findAccessForSubscription(subscription: SubscriptionDto | null, accesses: AccessCredentialDto[]) {
  if (!subscription) return null

  if (subscription.currentAccessId) {
    const linked = accesses.find((access) => access.id === subscription.currentAccessId)
    if (linked) return getCabinetAccessTerminalReason(linked, subscription.status) ? null : linked
  }

  return accesses.find((access) => access.subscriptionId === subscription.id && access.status === 'Active' && !getCabinetAccessTerminalReason(access, subscription.status))
    ?? accesses.find((access) => access.subscriptionId === subscription.id && !getCabinetAccessTerminalReason(access, subscription.status))
    ?? null
}

export function daysUntil(dateValue?: string | null, now = new Date()) {
  if (!dateValue) return null
  const target = new Date(dateValue).getTime()
  if (Number.isNaN(target)) return null
  return Math.ceil((target - now.getTime()) / 86_400_000)
}

export function buildCabinetSummary(subscriptions: SubscriptionDto[], accesses: AccessCredentialDto[], now = new Date()) {
  const currentSubscription = selectCurrentSubscription(subscriptions)
  const currentAccess = findAccessForSubscription(currentSubscription, accesses)
  const linkedCurrentAccess = currentSubscription?.currentAccessId
    ? accesses.find((access) => access.id === currentSubscription.currentAccessId)
    : null
  const daysLeft = daysUntil(currentSubscription?.endAt, now)

  return {
    currentSubscription,
    currentAccess,
    daysLeft,
    hasActiveSubscription: currentSubscription ? isCurrentSubscription(currentSubscription) : false,
    hasConnectionLink: !getCabinetAccessTerminalReason(linkedCurrentAccess, currentSubscription?.status) && Boolean(currentAccess?.accessUri || currentSubscription?.accessUri)
  }
}
