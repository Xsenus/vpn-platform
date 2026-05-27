import { AccessCredentialDto, SubscriptionDto } from '@vpn-platform/api-client'

export function isCurrentSubscription(subscription: SubscriptionDto) {
  return subscription.status === 'Active' || subscription.status === 'GracePeriod'
}

export function selectCurrentSubscription(subscriptions: SubscriptionDto[]) {
  const sorted = [...subscriptions].sort((left, right) => {
    const leftActive = isCurrentSubscription(left) ? 0 : 1
    const rightActive = isCurrentSubscription(right) ? 0 : 1
    if (leftActive !== rightActive) return leftActive - rightActive
    return new Date(left.endAt).getTime() - new Date(right.endAt).getTime()
  })

  return sorted[0] ?? null
}

export function findAccessForSubscription(subscription: SubscriptionDto | null, accesses: AccessCredentialDto[]) {
  if (!subscription) return null

  if (subscription.currentAccessId) {
    const linked = accesses.find((access) => access.id === subscription.currentAccessId)
    if (linked) return linked
  }

  return accesses.find((access) => access.subscriptionId === subscription.id && access.status === 'Active')
    ?? accesses.find((access) => access.subscriptionId === subscription.id)
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
  const daysLeft = daysUntil(currentSubscription?.endAt, now)

  return {
    currentSubscription,
    currentAccess,
    daysLeft,
    hasActiveSubscription: currentSubscription ? isCurrentSubscription(currentSubscription) : false,
    hasConnectionLink: Boolean(currentAccess?.accessUri || currentSubscription?.accessUri)
  }
}
