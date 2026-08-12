import { AccessCredentialDto, CabinetSubscriptionDto } from '@vpn-platform/api-client'

export function getSubscriptionAccessExpiry(subscription: Pick<CabinetSubscriptionDto, 'endAt' | 'gracePeriodEndAt'>) {
  return subscription.gracePeriodEndAt ?? subscription.endAt
}

export function getEffectiveSubscriptionStatus(subscription: CabinetSubscriptionDto, now = new Date()) {
  if (subscription.status !== 'Active' && subscription.status !== 'GracePeriod') return subscription.status

  const endAt = Date.parse(subscription.endAt)
  const accessExpiry = Date.parse(getSubscriptionAccessExpiry(subscription))
  const nowTime = now.getTime()
  if (Number.isFinite(accessExpiry) && Number.isFinite(nowTime) && accessExpiry <= nowTime) return 'Expired'
  if (subscription.status === 'Active' && Number.isFinite(endAt) && Number.isFinite(nowTime) && endAt <= nowTime) return 'GracePeriod'
  return subscription.status
}

export function isCurrentSubscription(subscription: CabinetSubscriptionDto, now = new Date()) {
  const status = getEffectiveSubscriptionStatus(subscription, now)
  return status === 'Active' || status === 'GracePeriod'
}

export function formatReferralRewardType(type: string) {
  const labels: Record<string, string> = {
    'bonus-days': 'Бонусные дни',
    cashback: 'Кэшбэк',
    discount: 'Скидка'
  }
  return labels[type.trim().toLowerCase()] ?? 'Реферальное начисление'
}

export function getSubscriptionRenewalAvailability(subscription: CabinetSubscriptionDto) {
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
  access: Pick<AccessCredentialDto, 'status' | 'subscriptionStatus' | 'isTerminal' | 'expiryDate'> | null | undefined,
  subscriptionStatus?: string | null,
  now = new Date()
) {
  if (access?.status === 'Revoked') {
    return 'Доступ отозван. Ключ и QR-код больше недоступны.'
  }

  if (subscriptionStatus === 'Cancelled' || access?.subscriptionStatus === 'Cancelled') {
    return 'Родительская подписка отменена. Ключ и QR-код больше недоступны.'
  }

  const expiresAt = access?.expiryDate ? Date.parse(access.expiryDate) : Number.NaN
  if (subscriptionStatus === 'Expired'
    || access?.subscriptionStatus === 'Expired'
    || (Number.isFinite(expiresAt) && expiresAt <= now.getTime())) {
    return 'Срок VPN-доступа истёк. Ключ и QR-код больше недоступны.'
  }

  if (access?.isTerminal) {
    return 'VPN-доступ завершён. Ключ и QR-код больше недоступны.'
  }

  return null
}

export function getAccessQrAvailability(
  access: Pick<AccessCredentialDto, 'accessUri' | 'status' | 'subscriptionStatus' | 'isTerminal' | 'expiryDate'> | null | undefined,
  now = new Date()
) {
  if (access?.status === 'Revoked') {
    return {
      canGenerate: false,
      reason: 'Доступ отозван. Ссылка подключения и QR-код больше недоступны.'
    }
  }

  const terminalReason = getCabinetAccessTerminalReason(access, undefined, now)
  if (terminalReason) return { canGenerate: false, reason: terminalReason }

  const canGenerate = Boolean(access?.accessUri?.trim())
  return {
    canGenerate,
    reason: canGenerate ? null : 'QR-код появится после выдачи ссылки подключения.'
  }
}

export function selectCurrentSubscription(subscriptions: CabinetSubscriptionDto[], now = new Date()) {
  const sorted = subscriptions.filter((subscription) => isCurrentSubscription(subscription, now)).sort((left, right) => {
    const leftEndAt = Date.parse(getSubscriptionAccessExpiry(left))
    const rightEndAt = Date.parse(getSubscriptionAccessExpiry(right))
    return rightEndAt - leftEndAt
  })

  return sorted[0] ?? null
}

export function findAccessForSubscription(subscription: CabinetSubscriptionDto | null, accesses: AccessCredentialDto[], now = new Date()) {
  if (!subscription) return null

  if (subscription.currentAccessId) {
    const linked = accesses.find((access) => access.id === subscription.currentAccessId)
    if (linked) return getCabinetAccessTerminalReason(linked, getEffectiveSubscriptionStatus(subscription, now), now) ? null : linked
  }

  const effectiveStatus = getEffectiveSubscriptionStatus(subscription, now)
  return accesses.find((access) => access.subscriptionId === subscription.id && access.status === 'Active' && !getCabinetAccessTerminalReason(access, effectiveStatus, now))
    ?? accesses.find((access) => access.subscriptionId === subscription.id && !getCabinetAccessTerminalReason(access, effectiveStatus, now))
    ?? null
}

export function daysUntil(dateValue?: string | null, now = new Date()) {
  if (!dateValue) return null
  const target = new Date(dateValue).getTime()
  if (Number.isNaN(target)) return null
  return Math.ceil((target - now.getTime()) / 86_400_000)
}

export function buildCabinetSummary(subscriptions: CabinetSubscriptionDto[], accesses: AccessCredentialDto[], now = new Date()) {
  const currentSubscription = selectCurrentSubscription(subscriptions, now)
  const currentAccess = findAccessForSubscription(currentSubscription, accesses, now)
  const linkedCurrentAccess = currentSubscription?.currentAccessId
    ? accesses.find((access) => access.id === currentSubscription.currentAccessId)
    : null
  const daysLeft = daysUntil(currentSubscription ? getSubscriptionAccessExpiry(currentSubscription) : null, now)

  return {
    currentSubscription,
    currentAccess,
    daysLeft,
    hasActiveSubscription: currentSubscription ? isCurrentSubscription(currentSubscription, now) : false,
    hasConnectionLink: !getCabinetAccessTerminalReason(linkedCurrentAccess, currentSubscription ? getEffectiveSubscriptionStatus(currentSubscription, now) : null, now) && Boolean(currentAccess?.accessUri || currentSubscription?.accessUri)
  }
}

export function getNextCabinetAccessExpiryDelay(
  subscriptions: CabinetSubscriptionDto[],
  accesses: AccessCredentialDto[],
  now = new Date()
) {
  const nowTime = now.getTime()
  if (!Number.isFinite(nowTime)) return null

  const deadlines = [
    ...subscriptions
      .filter((subscription) => subscription.status === 'Active' || subscription.status === 'GracePeriod')
      .map((subscription) => Date.parse(getSubscriptionAccessExpiry(subscription))),
    ...accesses
      .filter((access) => access.status !== 'Revoked' && !access.isTerminal && access.expiryDate)
      .map((access) => Date.parse(access.expiryDate!))
  ].filter((deadline) => Number.isFinite(deadline) && deadline > nowTime)

  return deadlines.length > 0 ? Math.min(...deadlines) - nowTime : null
}
