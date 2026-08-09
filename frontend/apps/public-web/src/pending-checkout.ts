import { isPaymentProvider, type PaymentProvider } from '@vpn-platform/api-client'

export type PendingCheckout = {
  token: string
  tariffName: string
  provider: PaymentProvider
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
  if (
    typeof candidate.token !== 'string'
    || !checkoutTokenPattern.test(candidate.token)
    || tariffName.length === 0
    || tariffName.length > maxTariffNameLength
    || !isPaymentProvider(candidate.provider)
  ) {
    return null
  }

  return {
    token: candidate.token,
    tariffName,
    provider: candidate.provider
  }
}
