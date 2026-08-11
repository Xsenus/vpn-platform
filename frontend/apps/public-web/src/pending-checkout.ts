import { ApiClientError, isPaymentProvider, type PaymentProvider } from '@vpn-platform/api-client'

export type PendingCheckout = {
  token: string
  tariffName: string
  provider: PaymentProvider
  expiresAt: string | null
}

const maxPersistedCheckoutLength = 4_096
const maxTariffNameLength = 200
const checkoutTokenPattern = /^[A-Za-z0-9_-]{32,512}$/

export function parsePendingCheckout(raw: string | null): PendingCheckout | null {
  if (!raw || raw.length > maxPersistedCheckoutLength) return null

  let value: unknown
  try {
    value = JSON.parse(raw)
  } catch {
    return null
  }

  if (!value || typeof value !== 'object' || Array.isArray(value)) return null

  const candidate = value as Record<string, unknown>
  const tariffName = typeof candidate.tariffName === 'string' ? candidate.tariffName.trim() : ''
  const expiresAt = typeof candidate.expiresAt === 'string' ? candidate.expiresAt.trim() : null
  if (
    typeof candidate.token !== 'string'
    || !checkoutTokenPattern.test(candidate.token)
    || tariffName.length === 0
    || tariffName.length > maxTariffNameLength
    || !isPaymentProvider(candidate.provider)
    || (candidate.expiresAt !== undefined && (!expiresAt || !Number.isFinite(Date.parse(expiresAt))))
  ) {
    return null
  }

  return {
    token: candidate.token,
    tariffName,
    provider: candidate.provider,
    expiresAt
  }
}

export function getPendingCheckoutSessionAvailability(pending: Pick<PendingCheckout, 'expiresAt'>, now = new Date()) {
  const expiresAt = pending.expiresAt ? Date.parse(pending.expiresAt) : Number.NaN
  const isExpired = Number.isFinite(expiresAt) && expiresAt <= now.getTime()

  return {
    canClaim: !isExpired,
    shouldCreateNewOrder: isExpired,
    shouldForgetPendingCheckout: isExpired,
    title: isExpired ? 'Срок оформления покупки истёк' : null,
    statusLabel: isExpired ? 'срок оформления истёк' : null,
    reason: isExpired ? 'Время оформления закончилось. Выберите тариф и создайте новый заказ.' : null
  }
}

export function getPendingCheckoutSessionExpiryDelay(
  pending: Pick<PendingCheckout, 'expiresAt'>,
  now = new Date()
) {
  if (!getPendingCheckoutSessionAvailability(pending, now).canClaim || !pending.expiresAt) return null

  const expiresAt = Date.parse(pending.expiresAt)
  const nowTime = now.getTime()
  if (!Number.isFinite(expiresAt) || !Number.isFinite(nowTime) || expiresAt <= nowTime) return null
  return expiresAt - nowTime
}

export function isCheckoutSessionExpiredError(error: unknown) {
  if (!(error instanceof ApiClientError)) return false
  const payload = error.payload
  const raw = typeof payload === 'string'
    ? payload
    : payload && typeof payload === 'object' && 'error' in payload && typeof (payload as Record<string, unknown>).error === 'string'
      ? String((payload as Record<string, unknown>).error)
      : ''
  return raw.trim().toLowerCase() === 'checkout session expired.'
}
