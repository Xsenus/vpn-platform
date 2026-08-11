import { normalizeApiError, type OrderDto, type PublicPaymentProviderDto, type TariffDto } from '@vpn-platform/api-client'
import { getPendingCheckoutSessionExpiryDelay, type PendingCheckout } from './pending-checkout'

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
      shouldForgetPendingCheckout: true,
      isExpired: true,
      title: 'Срок оплаты заказа истёк',
      statusLabel: 'срок оплаты истёк',
      reason: 'Срок оплаты заказа истёк. Создайте новый заказ с актуальным сроком оплаты.'
    }
  }

  const resolvedCopy: Record<string, { title: string; statusLabel: string; reason: string; shouldCreateNewOrder?: boolean }> = {
    PaymentReceived: {
      title: 'Оплата получена',
      statusLabel: 'оплата получена',
      reason: 'Платёж подтверждён. Заказ передан на подключение VPN-доступа.'
    },
    FulfillmentInProgress: {
      title: 'Подключаем VPN-доступ',
      statusLabel: 'доступ подключается',
      reason: 'Оплата подтверждена. VPN-доступ подготавливается автоматически.'
    },
    Completed: {
      title: 'Покупка завершена',
      statusLabel: 'покупка завершена',
      reason: 'Оплата подтверждена. Подписка и VPN-доступ появятся в личном кабинете.'
    },
    PartiallyProcessed: {
      title: 'Завершаем выдачу доступа',
      statusLabel: 'требует завершения',
      reason: 'Оплата подтверждена, но выдача VPN-доступа ещё завершается. Проверьте личный кабинет или обратитесь в поддержку.'
    },
    Refunded: {
      title: 'Платёж возвращён',
      statusLabel: 'платёж возвращён',
      reason: 'Возврат оформлен. Для новой покупки выберите тариф заново.',
      shouldCreateNewOrder: true
    },
    Cancelled: {
      title: 'Заказ отменён',
      statusLabel: 'заказ отменён',
      reason: 'Заказ отменён. Для новой покупки выберите тариф заново.',
      shouldCreateNewOrder: true
    },
    Canceled: {
      title: 'Заказ отменён',
      statusLabel: 'заказ отменён',
      reason: 'Заказ отменён. Для новой покупки выберите тариф заново.',
      shouldCreateNewOrder: true
    }
  }

  const resolved = resolvedCopy[order.status]
  if (resolved) {
    return {
      canRetry: false,
      shouldCreateNewOrder: resolved.shouldCreateNewOrder ?? false,
      shouldForgetPendingCheckout: true,
      isExpired: false,
      title: resolved.title,
      statusLabel: resolved.statusLabel,
      reason: resolved.reason
    }
  }

  return {
    canRetry: hasRetryableStatus,
    shouldCreateNewOrder: false,
    shouldForgetPendingCheckout: !hasRetryableStatus,
    isExpired: false,
    title: hasRetryableStatus ? 'Заказ создан, оплата не подготовлена' : 'Заказ пока нельзя оплатить',
    statusLabel: hasRetryableStatus ? 'заказ создан' : 'требует проверки',
    reason: hasRetryableStatus ? null : 'Этот заказ больше нельзя отправить на повторную оплату.'
  }
}

export function canOpenCheckoutPayment(
  order: Pick<OrderDto, 'status' | 'expiresAt'>,
  now = new Date()
) {
  return getPendingCheckoutOrderAvailability(order, now).canRetry
}

export function getCheckoutPaymentExpiryDelay(
  order: Pick<OrderDto, 'status' | 'expiresAt'>,
  now = new Date()
) {
  if (!canOpenCheckoutPayment(order, now)) return null

  const expiresAt = Date.parse(order.expiresAt)
  const nowTime = now.getTime()
  if (!Number.isFinite(expiresAt) || !Number.isFinite(nowTime) || expiresAt <= nowTime) return null
  return expiresAt - nowTime
}

export function getNextPublicCheckoutExpiryDelay(
  orders: ReadonlyArray<Pick<OrderDto, 'status' | 'expiresAt'>>,
  pending: Pick<PendingCheckout, 'expiresAt'> | null,
  now = new Date()
) {
  const delays = [
    ...orders.map((order) => getCheckoutPaymentExpiryDelay(order, now)),
    pending ? getPendingCheckoutSessionExpiryDelay(pending, now) : null
  ].filter((delay): delay is number => delay !== null)

  return delays.length > 0 ? Math.min(...delays) : null
}
