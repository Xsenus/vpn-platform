import { AdminUserOverviewDto } from '@vpn-platform/api-client'

export type AdminUserOverviewStats = {
  ordersCount: number
  paymentsCount: number
  subscriptionsCount: number
  activeSubscriptionsCount: number
  accessCredentialsCount: number
  activeAccessesCount: number
  telegramAccountsCount: number
  blockedTelegramAccountsCount: number
  supportConversationsCount: number
  openSupportConversationsCount: number
  totalPaidAmount: number
  currency: string
  needsAttention: boolean
  attentionReasons: string[]
}

function status(value: string | null | undefined) {
  return (value ?? '').trim().toLowerCase()
}

function amount(value: unknown) {
  return typeof value === 'number' && Number.isFinite(value) ? value : 0
}

function stringValue(value: unknown, fallback = '') {
  return typeof value === 'string' && value.trim() ? value : fallback
}

export function buildAdminUserOverviewStats(overview: AdminUserOverviewDto | null): AdminUserOverviewStats {
  if (!overview) {
    return {
      ordersCount: 0,
      paymentsCount: 0,
      subscriptionsCount: 0,
      activeSubscriptionsCount: 0,
      accessCredentialsCount: 0,
      activeAccessesCount: 0,
      telegramAccountsCount: 0,
      blockedTelegramAccountsCount: 0,
      supportConversationsCount: 0,
      openSupportConversationsCount: 0,
      totalPaidAmount: 0,
      currency: 'RUB',
      needsAttention: false,
      attentionReasons: []
    }
  }

  const activeSubscriptionsCount = overview.subscriptions.filter((item) => ['active', 'graceperiod'].includes(status(item.status))).length
  const activeAccessesCount = overview.accessCredentials.filter((item) => ['active', 'syncrequired'].includes(status(item.status))).length
  const blockedTelegramAccountsCount = overview.telegramAccounts.filter((item) => item.isBlocked).length
  const openSupportConversationsCount = overview.supportConversations.filter((item) => status(item.status) !== 'closed').length
  const failedPaymentsCount = overview.payments.filter((item) => ['failed', 'cancelled', 'unknown'].includes(status(item.status))).length
  const blockedSubscriptionsCount = overview.subscriptions.filter((item) => ['blocked', 'suspended'].includes(status(item.status))).length
  const totalPaidAmount = overview.payments
    .filter((item) => ['succeeded', 'refunded', 'partiallyrefunded'].includes(status(item.status)))
    .reduce((sum, item) => sum + amount(item.amount), 0)
  const currency = stringValue(overview.payments.find((item) => amount(item.amount) > 0)?.currency, 'RUB')
  const attentionReasons: string[] = []

  if (failedPaymentsCount > 0) attentionReasons.push(`проблемные платежи: ${failedPaymentsCount}`)
  if (blockedSubscriptionsCount > 0) attentionReasons.push(`ограниченные подписки: ${blockedSubscriptionsCount}`)
  if (blockedTelegramAccountsCount > 0) attentionReasons.push(`заблокированные Telegram: ${blockedTelegramAccountsCount}`)
  if (openSupportConversationsCount > 0) attentionReasons.push(`открытая поддержка: ${openSupportConversationsCount}`)

  return {
    ordersCount: overview.orders.length,
    paymentsCount: overview.payments.length,
    subscriptionsCount: overview.subscriptions.length,
    activeSubscriptionsCount,
    accessCredentialsCount: overview.accessCredentials.length,
    activeAccessesCount,
    telegramAccountsCount: overview.telegramAccounts.length,
    blockedTelegramAccountsCount,
    supportConversationsCount: overview.supportConversations.length,
    openSupportConversationsCount,
    totalPaidAmount,
    currency,
    needsAttention: attentionReasons.length > 0,
    attentionReasons
  }
}

export function formatAdminMoney(value: number, currency: string) {
  const normalizedCurrency = /^[A-Z]{3}$/.test(currency) ? currency : 'RUB'
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: normalizedCurrency,
    maximumFractionDigits: Number.isInteger(value) ? 0 : 2
  }).format(value)
}

export function telegramDisplayName(account: AdminUserOverviewDto['telegramAccounts'][number]) {
  const username = stringValue(account.username)
  const fullName = [account.firstName, account.lastName].map((item) => stringValue(item)).filter(Boolean).join(' ')
  const userId = account.telegramUserId ? `tg:${account.telegramUserId}` : 'Telegram'

  if (username) return `@${username}`
  if (fullName) return fullName
  return userId
}
