import { normalizeApiError, type OrderDto, type PublicPaymentProviderDto, type TariffDto } from '@vpn-platform/api-client'

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
  paymentProvidersLoading: boolean,
  paymentProviders: PublicPaymentProviderDto[],
  provider: string
) {
  return pendingTariffId.length === 0
    && !paymentProvidersLoading
    && paymentProviders.length > 0
    && provider.trim().length > 0
}

export function getCheckoutErrorMessage(error: unknown, fallback: string) {
  return normalizeApiError(error, fallback)
}

export function getPendingCheckoutOrderAvailability(
  order: Pick<OrderDto, 'status' | 'expiresAt'>,
  now = new Date()
) {
  const hasRetryableStatus = order.status === 'PendingPayment' || order.status === 'Failed'
  const expiresAt = Date.parse(order.expiresAt)
  const isExpired = order.status === 'Expired'
    || (hasRetryableStatus && !Number.isNaN(expiresAt) && expiresAt <= now.getTime())

  if (isExpired) {
    return {
      canRetry: false,
      shouldCreateNewOrder: true,
      isExpired: true,
      reason: 'Срок оплаты заказа истёк. Создайте новый заказ с актуальным сроком оплаты.'
    }
  }

  return {
    canRetry: hasRetryableStatus,
    shouldCreateNewOrder: order.status === 'Cancelled' || order.status === 'Canceled',
    isExpired: false,
    reason: hasRetryableStatus ? null : 'Этот заказ больше нельзя отправить на повторную оплату.'
  }
}
