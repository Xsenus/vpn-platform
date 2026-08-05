import type { PublicPaymentProviderDto, TariffDto } from '@vpn-platform/api-client'

export type PublicListState = 'loading' | 'error' | 'empty' | 'ready'

export type CheckoutUnavailableCopy = {
  loading: string
  noProviders: string
  chooseProvider: string
}

export function getTariffFeatures(tariff: TariffDto) {
  if (Array.isArray(tariff.features) && tariff.features.length > 0) {
    return tariff.features.map((item) => item.trim()).filter(Boolean)
  }

  if (!tariff.featuresJson) return []

  try {
    const parsed = JSON.parse(tariff.featuresJson)
    return Array.isArray(parsed)
      ? parsed.filter((item): item is string => typeof item === 'string' && item.trim().length > 0).map((item) => item.trim())
      : []
  } catch {
    return []
  }
}

export function getPublicListState(loading: boolean, error: string, itemCount: number): PublicListState {
  if (loading) return 'loading'
  if (error.trim().length > 0) return 'error'
  if (itemCount === 0) return 'empty'
  return 'ready'
}

export function getCheckoutUnavailableReason(
  paymentProvidersLoading: boolean,
  paymentProviders: PublicPaymentProviderDto[],
  provider: string,
  copy: CheckoutUnavailableCopy
) {
  if (paymentProvidersLoading) return copy.loading
  if (paymentProviders.length === 0) return copy.noProviders
  if (!provider) return copy.chooseProvider
  return ''
}

export function canStartCheckout(
  pendingTariffId: string,
  tariffId: string,
  paymentProvidersLoading: boolean,
  paymentProviders: PublicPaymentProviderDto[],
  provider: string
) {
  return pendingTariffId !== tariffId
    && !paymentProvidersLoading
    && paymentProviders.length > 0
    && provider.trim().length > 0
}

export function getCheckoutErrorMessage(error: unknown, fallback: string) {
  const message = error instanceof Error ? error.message.trim() : ''
  const normalized = message.toLowerCase()
  const promoMessages: Array<[string, string]> = [
    ['promo code not found', 'Промокод не найден. Проверьте написание.'],
    ['promo code is inactive', 'Промокод отключён.'],
    ['promo code is not active yet', 'Промокод ещё не начал действовать.'],
    ['promo code expired', 'Срок действия промокода истёк.'],
    ['not available for this tariff', 'Промокод не действует для выбранного тарифа.'],
    ['not available for this channel', 'Промокод нельзя использовать в этом канале продаж.'],
    ['promo code configuration is invalid', 'Промокод настроен некорректно. Обратитесь в поддержку.'],
    ['redemption limit has been reached', 'Лимит активаций промокода исчерпан.'],
    ['usage limit for this account has been reached', 'Вы уже использовали этот промокод максимально допустимое число раз.'],
    ['promo code changed while', 'Промокод обновился во время оформления. Повторите попытку.']
  ]
  const promoMessage = promoMessages.find(([fragment]) => normalized.includes(fragment))
  return promoMessage?.[1] ?? (message || fallback)
}
