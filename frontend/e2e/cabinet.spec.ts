import { expect, test, type Page, type Route } from '@playwright/test'

const corsHeaders = {
  'access-control-allow-origin': '*',
  'access-control-allow-headers': 'content-type, authorization',
  'access-control-allow-methods': 'GET,POST,PATCH,DELETE,OPTIONS',
  'content-type': 'application/json; charset=utf-8'
}

const now = '2026-06-13T07:00:00Z'
const user = {
  id: 'user-cabinet-e2e',
  email: 'cabinet-e2e@example.test',
  displayName: 'Cabinet E2E',
  preferredLanguage: 'ru',
  referralCode: 'CABINET-E2E',
  status: 'Active'
}

const privateCabinetStateLabels = [
  'Активных подписок',
  'Подписок пока нет',
  'VPN-ключей пока нет',
  'Заказов нет',
  'Платежей нет',
  'Доступы не выдавались',
  'Реферальных начислений нет'
]

const appVersionRelease = {
  id: 'release-cabinet-e2e',
  releaseId: '2026-08-10-cabinet-modal-focus',
  version: '0.573.0',
  releasedAt: now,
  title: 'Проверка модального окна',
  summary: 'Клавиатурный фокус должен оставаться внутри окна обновления.',
  isActive: true,
  source: 'agent',
  items: [
    { id: 'release-item-cabinet-e2e', type: 'fixed', text: 'Проверен полный клавиатурный lifecycle.', sortOrder: 10 }
  ],
  createdByUserId: null,
  createdByUserName: 'Codex',
  updatedByUserId: null,
  updatedByUserName: 'Codex',
  createdAt: now,
  updatedAt: now
}

const staleAppVersionRelease = {
  ...appVersionRelease,
  id: 'release-cabinet-stale',
  releaseId: '2026-08-09-cabinet-stale-history',
  version: '0.572.0',
  title: 'Устаревшая история обновлений',
  summary: 'Этот ответ принадлежит завершённой сессии.',
  items: [
    { id: 'release-item-cabinet-stale', type: 'fixed', text: 'Старый ответ не должен попасть в новую сессию.', sortOrder: 10 }
  ]
}

const subscription = {
  id: 'sub-active',
  userId: user.id,
  tariffId: 'tariff-pro',
  tariffName: 'Pro 30 дней',
  status: 'Active',
  startAt: '2026-06-01T00:00:00Z',
  endAt: '2026-07-01T00:00:00Z',
  gracePeriodEndAt: null,
  autoRenewFlag: false,
  sourceChannel: 'Web',
  currentServerId: 'server-eu',
  currentAccessId: 'access-active',
  lastPaymentId: 'payment-paid',
  renewalCount: 0,
  blockReason: null,
  suspendedAt: null,
  cancelledAt: null,
  accessUri: 'vless://cabinet-e2e@example.com:443?security=reality#cabinet-e2e',
  qrCodePath: 'qr://cabinet-e2e',
  configPath: 'config://cabinet-e2e',
  nodeName: 'EU Sandbox',
  createdAt: now,
  updatedAt: now
}

const blockedSubscription = {
  ...subscription,
  id: 'sub-blocked',
  tariffName: 'Заблокированный тариф',
  status: 'Blocked',
  currentAccessId: null,
  lastPaymentId: null,
  accessUri: null,
  qrCodePath: null,
  configPath: null,
  blockReason: 'support review'
}

const cancelledSubscription = {
  ...subscription,
  id: 'sub-cancelled',
  tariffName: 'Отменённый тариф',
  status: 'Cancelled',
  currentAccessId: 'access-revoked',
  lastPaymentId: null,
  accessUri: null,
  qrCodePath: null,
  configPath: null,
  cancelledAt: now
}

const cancelledStaleSubscription = {
  ...subscription,
  id: 'sub-cancelled-stale',
  tariffName: 'Отменённый stale тариф',
  status: 'Cancelled',
  currentAccessId: 'access-cancelled-stale',
  lastPaymentId: null,
  accessUri: 'vless://cancelled-cabinet-stale-secret@example.test',
  qrCodePath: 'qr://cancelled-cabinet-stale-secret',
  configPath: 'config://cancelled-cabinet-stale-secret',
  cancelledAt: now
}

const paidOrder = {
  id: 'order-paid',
  userId: user.id,
  userDisplayName: user.displayName,
  userEmail: user.email,
  tariffId: subscription.tariffId,
  tariffName: subscription.tariffName,
  amount: 490,
  currency: 'RUB',
  status: 'Completed',
  type: 'NewSubscription',
  channel: 'Web',
  paymentProvider: 'YooKassa',
  checkoutSessionId: null,
  expiresAt: '2026-06-14T00:00:00Z',
  paidAt: '2026-06-13T06:00:00Z',
  isFirstPurchase: true,
  paymentAttemptsCount: 1,
  lastPaymentId: 'payment-paid',
  lastPaymentStatus: 'Succeeded',
  lastPaymentProvider: 'YooKassa',
  linkedSubscriptionId: subscription.id,
  createdAt: now,
  updatedAt: now
}

const stalePendingOrder = {
  ...paidOrder,
  id: 'order-stale-pending',
  tariffName: 'Истёкший заказ',
  status: 'PendingPayment',
  expiresAt: '2026-06-12T00:00:00Z',
  paidAt: null,
  isFirstPurchase: false,
  paymentAttemptsCount: 0,
  lastPaymentId: null,
  lastPaymentStatus: null,
  linkedSubscriptionId: null
}

const retryablePendingOrder = {
  ...stalePendingOrder,
  id: 'order-retryable',
  tariffName: 'Повторная оплата',
  expiresAt: '2099-06-14T00:00:00Z'
}

const paidPayment = {
  id: 'payment-paid',
  orderId: paidOrder.id,
  userId: user.id,
  userDisplayName: user.displayName,
  provider: 'YooKassa',
  paymentProviderAccountId: 'provider-yookassa',
  providerMode: 'Sandbox',
  providerPaymentId: 'yk-paid-1',
  externalEventId: 'evt-paid-1',
  idempotencyKey: 'idem-paid',
  confirmationUrl: 'https://pay.example.test/paid',
  returnUrl: 'http://127.0.0.1:5294',
  amount: 490,
  currency: 'RUB',
  status: 'Succeeded',
  signatureValidated: true,
  isActivationProcessed: true,
  activationProcessedAt: '2026-06-13T06:01:00Z',
  paidAt: '2026-06-13T06:00:00Z',
  failedAt: null,
  refundedAt: null,
  refundedAmount: 0,
  statusReason: null,
  webhookEventsCount: 1,
  refundsCount: 0,
  refundSupported: true,
  canRefund: false,
  refundableAmount: 0,
  refundBlockers: [],
  createdAt: now,
  updatedAt: now
}

const unsafeLinkPayment = {
  ...paidPayment,
  id: 'payment-unsafe-link',
  orderId: 'order-unsafe-link',
  providerPaymentId: 'unsafe-link-attempt',
  confirmationUrl: 'javascript:alert(1)'
}

const access = {
  id: 'access-active',
  subscriptionId: subscription.id,
  subscriptionStatus: 'Active',
  isTerminal: false,
  userId: user.id,
  providerType: 'X3UI',
  providerAccessId: 'x3ui-client-1',
  serverId: 'server-eu',
  serverName: 'EU Sandbox',
  accessUri: subscription.accessUri,
  qrCodePayload: subscription.accessUri,
  qrCodePath: 'qr://cabinet-e2e',
  configPath: 'config://cabinet-e2e',
  status: 'Active',
  issuedAt: '2026-06-13T06:00:00Z',
  expiryDate: subscription.endAt,
  disabledAt: null,
  lastSyncedAt: '2026-06-13T06:05:00Z',
  revision: 3,
  history: [],
  createdAt: now,
  updatedAt: now
}

const pendingAccess = {
  ...access,
  id: 'access-pending',
  subscriptionId: blockedSubscription.id,
  subscriptionStatus: 'Blocked',
  providerAccessId: 'x3ui-client-pending',
  serverName: 'Ожидает выдачи',
  accessUri: '',
  qrCodePayload: null,
  qrCodePath: '',
  configPath: '',
  status: 'Provisioning',
  revision: 1
}

const revokedAccess = {
  ...access,
  id: 'access-revoked',
  subscriptionId: cancelledSubscription.id,
  subscriptionStatus: 'Cancelled',
  isTerminal: true,
  providerAccessId: 'x3ui-client-revoked',
  serverName: 'Отозванный доступ',
  accessUri: 'vless://revoked-cabinet-secret@example.test',
  qrCodePayload: 'vless://revoked-cabinet-secret@example.test',
  qrCodePath: 'qr://revoked-cabinet-secret',
  configPath: 'config://revoked-cabinet-secret',
  status: 'Revoked',
  revision: 2
}

const cancelledStaleAccess = {
  ...access,
  id: 'access-cancelled-stale',
  subscriptionId: cancelledStaleSubscription.id,
  subscriptionStatus: 'Cancelled',
  isTerminal: true,
  providerAccessId: 'x3ui-client-cancelled-stale-secret',
  serverName: 'Отменённый stale доступ',
  accessUri: 'vless://cancelled-cabinet-stale-secret@example.test',
  qrCodePayload: 'vless://cancelled-cabinet-stale-secret@example.test',
  qrCodePath: 'qr://cancelled-cabinet-stale-secret',
  configPath: 'config://cancelled-cabinet-stale-secret',
  status: 'Active',
  revision: 4
}

const provider = {
  provider: 'YooKassa',
  publicName: 'YooKassa sandbox',
  mode: 'Sandbox',
  healthStatus: 'Healthy'
}

const alternateProvider = {
  provider: 'Stripe',
  publicName: 'Stripe sandbox',
  mode: 'Sandbox',
  healthStatus: 'Healthy'
}

function supportConversation(id: string, subject: string) {
  return {
    id,
    userId: user.id,
    telegramUserId: null,
    channel: 'web',
    status: 'open',
    subject,
    assignedToUserId: null,
    internalNote: '',
    revision: 0,
    closedAt: null,
    createdAt: now,
    updatedAt: now
  }
}

function supportMessage(id: string, conversationId: string, text: string) {
  return {
    id,
    supportConversationId: conversationId,
    userId: user.id,
    telegramUserId: null,
    direction: 'inbound',
    text,
    attachmentsJson: '[]',
    isInternalNote: false,
    createdAt: now
  }
}

function authResponse(email = user.email) {
  return {
    accessToken: `access-token-${email}`,
    refreshToken: `refresh-token-${email}`,
    email,
    displayName: user.displayName
  }
}

const restoredAuthResponse = {
  ...authResponse(),
  accessToken: 'access-token-restored-rotated',
  refreshToken: 'refresh-token-restored-rotated'
}

function jsonResponse(body: unknown, status = 200) {
  return {
    status,
    headers: corsHeaders,
    body: JSON.stringify(body)
  }
}

async function fulfillJson(route: Route, body: unknown, status = 200) {
  await route.fulfill(jsonResponse(body, status))
}

async function fulfillOptions(route: Route) {
  await route.fulfill({ status: 204, headers: corsHeaders })
}

async function mockCabinetApi(page: Page) {
  const requests: Array<{ method: string; url: string; body: unknown; authorization: string }> = []
  let supportConversations: unknown[] = []
  const supportMessagesByConversationId = new Map<string, unknown[]>()
  let telegramStatus = { isLinked: false, telegramUserId: null as number | null, username: null as string | null, linkedAt: null as string | null }
  let delayedSupportConversationId: string | null = null
  let supportMessagesFailureConversationId: string | null = null
  let supportMessagesFailureStatus = 503
  let delayedSupportMessagesReleased = false
  let releaseDelayedSupportMessages: (() => void) | null = null
  let logoutShouldFail = false
  let authorizedRequestsRejected = false
  let rejectedAuthorizedPath: string | null = null
  const expiredAccessTokens = new Set<string>()
  let failNextProfileStatus: number | null = null
  const cabinetLoadFailures = new Map<string, number>()
  let delayNextProfile = false
  let delayedProfileReleased = false
  let releaseDelayedProfile: (() => void) | null = null
  let refreshFailureStatus: number | null = null
  let delayNextRefresh = false
  let delayedRefreshReleased = false
  let releaseDelayedRefresh: (() => void) | null = null
  let delayNextSupportCreate = false
  let delayedSupportCreateReleased = false
  let releaseDelayedSupportCreate: (() => void) | null = null
  let delayNextQr = false
  let delayedQrReleased = false
  let releaseDelayedQr: (() => void) | null = null
  let failNextQrStatus: number | null = null
  let unsafeQrSvg = false
  let invalidSubscriptionsResponse = false
  let failNextRenewalPayment = false
  let paymentProvidersFailureStatus: number | null = null
  let multiplePaymentProviders = false
  let delayNextRetryPayment = false
  let delayedRetryPaymentReleased = false
  let releaseDelayedRetryPayment: (() => void) | null = null
  let appVersionAvailable = false
  let appVersionSeen = true
  let appVersionLatestFailureStatus: number | null = null
  let appVersionHistoryFailureStatus: number | null = null
  let appVersionMarkSeenFailureStatus: number | null = null
  let emptyAppVersionHistory = false
  let delayNextAppVersionHistory = false
  let delayedAppVersionHistoryReleased = false
  let releaseDelayedAppVersionHistory: (() => void) | null = null
  let renewalOrderCreated = false
  let renewalOrder = {
    ...paidOrder,
    id: 'order-renewal',
    status: 'PendingPayment',
    type: 'Renewal',
    expiresAt: '2099-06-14T00:00:00Z',
    isFirstPurchase: false,
    linkedSubscriptionId: subscription.id,
    paidAt: null,
    lastPaymentId: null,
    lastPaymentStatus: null
  }
  let renewalPayment = {
    paymentId: 'payment-renewal',
    redirectUrl: 'https://pay.example.test/renewal',
    rawResponse: '{"sandbox":true}'
  }

  const record = (route: Route) => {
    const request = route.request()
    if (request.method() !== 'OPTIONS') {
      requests.push({
        method: request.method(),
        url: request.url(),
        body: request.postData() ? request.postDataJSON() : null,
        authorization: request.headers().authorization ?? ''
      })
    }
  }

  await page.route('**/api/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname
    const method = request.method()

    if (method === 'OPTIONS') {
      await fulfillOptions(route)
      return
    }

    record(route)

    if (method === 'POST' && path === '/api/auth/register') {
      await fulfillJson(route, authResponse(request.postDataJSON().email))
      return
    }

    if (method === 'POST' && path === '/api/auth/login') {
      await fulfillJson(route, authResponse(request.postDataJSON().email))
      return
    }

    if (method === 'POST' && path === '/api/auth/refresh') {
      if (refreshFailureStatus !== null) {
        await fulfillJson(route, { error: 'invalid_refresh_token' }, refreshFailureStatus)
        return
      }
      if (delayNextRefresh) {
        delayNextRefresh = false
        if (!delayedRefreshReleased) {
          await new Promise<void>((resolve) => { releaseDelayedRefresh = resolve })
        }
        delayedRefreshReleased = false
        releaseDelayedRefresh = null
      }
      await fulfillJson(route, restoredAuthResponse)
      return
    }

    if (method === 'POST' && path === '/api/auth/forgot-password') {
      await fulfillJson(route, {
        accepted: true,
        message: 'If the account exists, a password reset instruction has been queued for the configured delivery channel.',
        validationResetToken: 'cabinet-reset-token'
      })
      return
    }

    if (method === 'POST' && path === '/api/auth/reset-password') {
      await fulfillJson(route, { status: 'password_changed' })
      return
    }

    if (method === 'POST' && path === '/api/auth/logout') {
      await fulfillJson(route, logoutShouldFail ? { error: 'logout unavailable' } : { status: 'ok' }, logoutShouldFail ? 503 : 200)
      return
    }

    if (authorizedRequestsRejected
      && request.headers().authorization
      && (path.startsWith('/api/me') || path.startsWith('/api/cabinet/'))) {
      await fulfillJson(route, { error: 'user_not_active' }, 401)
      return
    }

    if (expiredAccessTokens.has(request.headers().authorization ?? '')
      && (path.startsWith('/api/me') || path.startsWith('/api/cabinet/'))) {
      await fulfillJson(route, { error: 'access_token_expired' }, 401)
      return
    }

    if (rejectedAuthorizedPath === path && request.headers().authorization) {
      rejectedAuthorizedPath = null
      await fulfillJson(route, { error: 'user_not_active' }, 401)
      return
    }

    if (method === 'GET' && cabinetLoadFailures.has(path)) {
      await fulfillJson(route, { error: 'cabinet_load_temporarily_unavailable' }, cabinetLoadFailures.get(path)!)
      return
    }

    if (method === 'GET' && path === '/api/me') {
      if (failNextProfileStatus !== null) {
        const status = failNextProfileStatus
        failNextProfileStatus = null
        await fulfillJson(route, { error: 'profile_temporarily_unavailable' }, status)
        return
      }
      if (delayNextProfile) {
        delayNextProfile = false
        if (!delayedProfileReleased) {
          await new Promise<void>((resolve) => { releaseDelayedProfile = resolve })
        }
        delayedProfileReleased = false
        releaseDelayedProfile = null
      }
      await fulfillJson(route, user)
      return
    }

    if (method === 'GET' && path === '/api/me/subscriptions') {
      await fulfillJson(route, invalidSubscriptionsResponse ? [{}] : [subscription, blockedSubscription, cancelledSubscription, cancelledStaleSubscription])
      return
    }

    if (method === 'GET' && path === '/api/me/orders') {
      await fulfillJson(route, renewalOrderCreated
        ? [renewalOrder, paidOrder, stalePendingOrder, retryablePendingOrder]
        : [paidOrder, stalePendingOrder, retryablePendingOrder])
      return
    }

    if (method === 'GET' && path === '/api/me/payments') {
      await fulfillJson(route, [paidPayment, unsafeLinkPayment])
      return
    }

    if (method === 'GET' && path === '/api/me/accesses') {
      await fulfillJson(route, [access, pendingAccess, revokedAccess, cancelledStaleAccess])
      return
    }

    if (method === 'GET' && path === '/api/me/referrals') {
      await fulfillJson(route, [{ id: 'reward-e2e', type: 'bonus-days', status: 'Approved', value: 7, currencyOrUnit: 'days', processedAt: '2026-06-13T07:00:00Z', createdAt: '2026-06-13T07:00:00Z' }])
      return
    }

    if (method === 'GET' && path === '/api/me/support/conversations') {
      await fulfillJson(route, supportConversations)
      return
    }

    if (method === 'POST' && path === '/api/me/support/conversations') {
      const payload = request.postDataJSON()
      if (delayNextSupportCreate) {
        delayNextSupportCreate = false
        if (!delayedSupportCreateReleased) {
          await new Promise<void>((resolve) => { releaseDelayedSupportCreate = resolve })
        }
        delayedSupportCreateReleased = false
        releaseDelayedSupportCreate = null
      }
      const conversation = {
        id: 'support-created',
        userId: user.id,
        telegramUserId: null,
        channel: 'web',
        status: 'open',
        subject: payload.subject,
        assignedToUserId: null,
        internalNote: '',
        revision: 0,
        closedAt: null,
        createdAt: now,
        updatedAt: now
      }
      const firstMessage = {
        id: 'support-message-1',
        supportConversationId: conversation.id,
        userId: user.id,
        telegramUserId: null,
        direction: 'inbound',
        text: payload.text,
        attachmentsJson: '[]',
        isInternalNote: false,
        createdAt: now
      }
      supportConversations = [conversation]
      supportMessagesByConversationId.set(conversation.id, [firstMessage])
      await fulfillJson(route, conversation)
      return
    }

    const supportMessagesMatch = path.match(/^\/api\/me\/support\/conversations\/([^/]+)\/messages$/)
    if (method === 'GET' && supportMessagesMatch) {
      const conversationId = decodeURIComponent(supportMessagesMatch[1])
      if (supportMessagesFailureConversationId === conversationId) {
        await fulfillJson(route, { error: 'support_messages_temporarily_unavailable' }, supportMessagesFailureStatus)
        return
      }
      if (delayedSupportConversationId === conversationId) {
        if (!delayedSupportMessagesReleased) {
          await new Promise<void>((resolve) => { releaseDelayedSupportMessages = resolve })
        }
        delayedSupportConversationId = null
        delayedSupportMessagesReleased = false
        releaseDelayedSupportMessages = null
      }
      await fulfillJson(route, supportMessagesByConversationId.get(conversationId) ?? [])
      return
    }

    if (method === 'POST' && path === '/api/me/support/conversations/support-created/reply') {
      const payload = request.postDataJSON()
      const message = {
        id: 'support-message-2',
        supportConversationId: 'support-created',
        userId: user.id,
        telegramUserId: null,
        direction: 'inbound',
        text: payload.text,
        attachmentsJson: '[]',
        isInternalNote: false,
        createdAt: now
      }
      supportMessagesByConversationId.set('support-created', [
        ...(supportMessagesByConversationId.get('support-created') ?? []),
        message
      ])
      await fulfillJson(route, message)
      return
    }

    if (method === 'PATCH' && path === '/api/me/support/conversations/support-created/status') {
      const payload = request.postDataJSON() as { status: 'open' | 'closed'; revision: number }
      const revision = payload.revision + 1
      supportConversations = supportConversations.map((item) => {
        const conversation = item as ReturnType<typeof supportConversation>
        return conversation.id === 'support-created'
          ? { ...conversation, status: payload.status, revision, closedAt: payload.status === 'closed' ? now : null, updatedAt: now }
          : conversation
      })
      await fulfillJson(route, { conversationId: 'support-created', status: payload.status, revision })
      return
    }

    if (method === 'GET' && path === '/api/me/telegram/status') {
      await fulfillJson(route, telegramStatus)
      return
    }

    if (method === 'POST' && path === '/api/me/telegram/link-token') {
      await fulfillJson(route, {
        token: 'cabinet-e2e',
        deepLinkUrl: 'https://t.me/vpnplatform_bot?start=link_cabinet-e2e',
        expiresAt: '2026-06-13T07:15:00Z'
      })
      return
    }

    if (method === 'DELETE' && path === '/api/me/telegram/unlink') {
      telegramStatus = { isLinked: false, telegramUserId: null, username: null, linkedAt: null }
      await fulfillJson(route, telegramStatus)
      return
    }

    if (method === 'GET' && path === '/api/public/payments/providers') {
      if (paymentProvidersFailureStatus !== null) {
        await fulfillJson(route, { error: 'payment_providers_temporarily_unavailable' }, paymentProvidersFailureStatus)
        return
      }
      await fulfillJson(route, multiplePaymentProviders ? [provider, alternateProvider] : [provider])
      return
    }

    if (method === 'POST' && path === '/api/me/orders') {
      const payload = request.postDataJSON()
      renewalOrderCreated = true
      renewalOrder = {
        ...renewalOrder,
        tariffId: payload.tariffId,
        paymentProvider: payload.paymentProvider
      }
      await fulfillJson(route, {
        id: renewalOrder.id,
        userId: renewalOrder.userId,
        tariffId: renewalOrder.tariffId,
        amount: renewalOrder.amount,
        currency: renewalOrder.currency,
        status: renewalOrder.status,
        expiresAt: renewalOrder.expiresAt,
        linkedSubscriptionId: renewalOrder.linkedSubscriptionId
      })
      return
    }

    if (method === 'POST' && path === '/api/me/orders/order-renewal/payments/YooKassa/init') {
      if (failNextRenewalPayment) {
        failNextRenewalPayment = false
        await fulfillJson(route, { error: 'payment provider unavailable' }, 503)
        return
      }
      await fulfillJson(route, renewalPayment)
      return
    }

    if (method === 'POST' && path === '/api/me/orders/order-retryable/payments/YooKassa/init') {
      if (delayNextRetryPayment) {
        delayNextRetryPayment = false
        if (!delayedRetryPaymentReleased) {
          await new Promise<void>((resolve) => { releaseDelayedRetryPayment = resolve })
        }
        delayedRetryPaymentReleased = false
        releaseDelayedRetryPayment = null
      }
      await fulfillJson(route, {
        paymentId: 'payment-retry',
        redirectUrl: 'https://pay.example.test/retry',
        rawResponse: '{"sandbox":true}'
      })
      return
    }

    if (method === 'GET' && path === '/api/cabinet/access/access-active/qr') {
      if (failNextQrStatus !== null) {
        const status = failNextQrStatus
        failNextQrStatus = null
        await fulfillJson(route, { error: 'qr_temporarily_unavailable' }, status)
        return
      }
      if (delayNextQr) {
        delayNextQr = false
        if (!delayedQrReleased) {
          await new Promise<void>((resolve) => { releaseDelayedQr = resolve })
        }
        delayedQrReleased = false
        releaseDelayedQr = null
      }
      await route.fulfill({
        status: 200,
        headers: {
          ...corsHeaders,
          'content-type': 'image/svg+xml; charset=utf-8'
        },
        body: unsafeQrSvg
          ? '<svg xmlns="http://www.w3.org/2000/svg" onload="window.__cabinetQrExecuted=true"><rect width="10" height="10" /></svg>'
          : '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10" /></svg>'
      })
      return
    }

    if (method === 'GET' && path === '/api/app-version/latest') {
      if (appVersionLatestFailureStatus !== null) {
        await fulfillJson(route, { error: 'latest_temporarily_unavailable' }, appVersionLatestFailureStatus)
        return
      }
      await fulfillJson(route, appVersionAvailable
        ? { currentVersion: appVersionRelease.version, latestRelease: appVersionRelease, seenByCurrentUser: appVersionSeen }
        : { currentVersion: null, latestRelease: null, seenByCurrentUser: true })
      return
    }

    if (method === 'GET' && path === '/api/app-version/history') {
      if (appVersionHistoryFailureStatus !== null) {
        await fulfillJson(route, { error: 'history_temporarily_unavailable' }, appVersionHistoryFailureStatus)
        return
      }
      if (delayNextAppVersionHistory) {
        delayNextAppVersionHistory = false
        if (!delayedAppVersionHistoryReleased) {
          await new Promise<void>((resolve) => { releaseDelayedAppVersionHistory = resolve })
        }
        delayedAppVersionHistoryReleased = false
        releaseDelayedAppVersionHistory = null
        await fulfillJson(route, [staleAppVersionRelease])
        return
      }
      await fulfillJson(route, appVersionAvailable && !emptyAppVersionHistory ? [appVersionRelease] : [])
      return
    }

    if (method === 'POST' && path === '/api/app-version/mark-seen') {
      if (appVersionMarkSeenFailureStatus !== null) {
        await fulfillJson(route, { error: 'mark_seen_temporarily_unavailable' }, appVersionMarkSeenFailureStatus)
        return
      }
      await fulfillJson(route, { releaseId: appVersionRelease.releaseId, seen: true })
      return
    }

    await fulfillJson(route, { error: `Unhandled ${method} ${path}` }, 404)
  })

  return {
    requests,
    failLogout: () => { logoutShouldFail = true },
    rejectAuthorizedRequests: () => { authorizedRequestsRejected = true },
    allowAuthorizedRequests: () => { authorizedRequestsRejected = false },
    expireAccessToken: (accessToken: string) => { expiredAccessTokens.add(`Bearer ${accessToken}`) },
    failNextProfileRequest: (status = 503) => { failNextProfileStatus = status },
    failCabinetLoad: (path: string, status = 503) => { cabinetLoadFailures.set(path, status) },
    allowCabinetLoad: (path: string) => { cabinetLoadFailures.delete(path) },
    delayNextProfileRequest: () => { delayNextProfile = true },
    releaseProfileRequest: () => {
      delayedProfileReleased = true
      releaseDelayedProfile?.()
    },
    failRefreshRequest: (status = 401) => { refreshFailureStatus = status },
    delayNextRefreshRequest: () => { delayNextRefresh = true },
    releaseRefreshRequest: () => {
      delayedRefreshReleased = true
      releaseDelayedRefresh?.()
    },
    delayNextSupportCreateRequest: () => { delayNextSupportCreate = true },
    releaseSupportCreateRequest: () => {
      delayedSupportCreateReleased = true
      releaseDelayedSupportCreate?.()
    },
    delayNextQrRequest: () => { delayNextQr = true },
    releaseQrRequest: () => {
      delayedQrReleased = true
      releaseDelayedQr?.()
    },
    useSupportConversationRaceFixture: () => {
      supportConversations = [
        supportConversation('support-first', 'Первая переписка'),
        supportConversation('support-second', 'Вторая переписка')
      ]
      supportMessagesByConversationId.set('support-first', [supportMessage('message-first', 'support-first', 'Секрет первой переписки')])
      supportMessagesByConversationId.set('support-second', [supportMessage('message-second', 'support-second', 'Ответ второй переписки')])
    },
    delaySupportMessages: (conversationId: string) => {
      delayedSupportConversationId = conversationId
      delayedSupportMessagesReleased = false
    },
    failSupportMessages: (conversationId: string, status = 503) => {
      supportMessagesFailureConversationId = conversationId
      supportMessagesFailureStatus = status
    },
    allowSupportMessages: () => { supportMessagesFailureConversationId = null },
    releaseSupportMessages: () => {
      delayedSupportMessagesReleased = true
      releaseDelayedSupportMessages?.()
    },
    returnUnsafeQrSvg: () => { unsafeQrSvg = true },
    failNextQrRequest: (status = 503) => { failNextQrStatus = status },
    returnInvalidSubscriptionsResponse: () => { invalidSubscriptionsResponse = true },
    failNextRenewalPayment: () => { failNextRenewalPayment = true },
    failPaymentProviders: (status = 503) => { paymentProvidersFailureStatus = status },
    allowPaymentProviders: () => { paymentProvidersFailureStatus = null },
    useMultiplePaymentProviders: () => { multiplePaymentProviders = true },
    delayNextRetryPaymentRequest: () => { delayNextRetryPayment = true },
    releaseRetryPaymentRequest: () => {
      delayedRetryPaymentReleased = true
      releaseDelayedRetryPayment?.()
    },
    showAppVersionRelease: () => { appVersionAvailable = true },
    showUnseenAppVersionRelease: () => {
      appVersionAvailable = true
      appVersionSeen = false
    },
    failAppVersionLatest: (status = 503) => { appVersionLatestFailureStatus = status },
    allowAppVersionLatest: () => { appVersionLatestFailureStatus = null },
    failAppVersionHistory: (status = 503) => { appVersionHistoryFailureStatus = status },
    allowAppVersionHistory: () => { appVersionHistoryFailureStatus = null },
    failAppVersionMarkSeen: (status = 503) => { appVersionMarkSeenFailureStatus = status },
    allowAppVersionMarkSeen: () => { appVersionMarkSeenFailureStatus = null },
    returnEmptyAppVersionHistory: () => { emptyAppVersionHistory = true },
    delayNextAppVersionHistory: () => { delayNextAppVersionHistory = true },
    releaseAppVersionHistory: () => {
      delayedAppVersionHistoryReleased = true
      releaseDelayedAppVersionHistory?.()
    },
    markTelegramLinked: () => {
      telegramStatus = { isLinked: true, telegramUserId: 777001, username: 'cabinet_e2e', linkedAt: now }
    },
    rejectNextAuthorizedPath: (path: string) => { rejectedAuthorizedPath = path },
    getRequestCount: (path: string, method = 'GET') =>
      requests.filter((item) => item.method === method && new URL(item.url).pathname === path).length,
    getAuthorizedRequestCount: (path: string, authorization: string, method = 'GET') =>
      requests.filter((item) => item.method === method
        && item.authorization === authorization
        && new URL(item.url).pathname === path).length,
    getLastRequest: (path: string, method = 'POST') =>
      requests.findLast((item) => item.method === method && new URL(item.url).pathname === path)
  }
}

async function seedCabinetSession(page: Page, accessToken: string, refreshToken: string) {
  await page.addInitScript(({ accessToken: access, refreshToken: refresh }) => {
    sessionStorage.setItem('vpn-platform-cabinet-token', access)
    sessionStorage.setItem('vpn-platform-cabinet-refresh-token', refresh)
  }, { accessToken, refreshToken })
}

test('cabinet auth hides private dashboard until the profile is loaded', async ({ page }) => {
  const api = await mockCabinetApi(page)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Вход в личный кабинет' })).toBeVisible()
  for (const privateState of privateCabinetStateLabels) {
    await expect(page.getByText(privateState, { exact: true })).toHaveCount(0)
  }
  expect(api.getRequestCount('/api/me')).toBe(0)

  const authPanel = page.locator('#cabinet-auth-panel')
  await authPanel.getByLabel('Email').fill(user.email)
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await authPanel.getByRole('button', { name: 'Войти', exact: true }).click()

  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await expect(page.getByText('Активных подписок', { exact: true })).toBeVisible()
  await expect(page.getByText(subscription.tariffName, { exact: true }).first()).toBeVisible()
  expect(api.getRequestCount('/api/me')).toBe(1)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet partial load errors stay scoped and recover without false private data', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.failCabinetLoad('/api/me/subscriptions')
  api.failCabinetLoad('/api/me/orders')
  api.failCabinetLoad('/api/me/telegram/status')
  await seedCabinetSession(page, 'access-token-cabinet-partial-load', 'refresh-token-cabinet-partial-load')

  await page.goto('/')

  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await expect(page.getByText('Не удалось загрузить подписки и связанные VPN-доступы.', { exact: true })).toBeVisible()
  await expect(page.getByText('Подписок пока нет', { exact: true })).toHaveCount(0)
  await expect(page.getByText(subscription.tariffName, { exact: true })).toHaveCount(0)
  await expect(page.getByText('Не удалось загрузить историю заказов.', { exact: true })).toBeVisible()
  await expect(page.getByText('Заказов нет', { exact: true })).toHaveCount(0)
  await expect(page.getByText('Не удалось загрузить статус Telegram.', { exact: true })).toBeVisible()
  await expect(page.getByText('NotLinked', { exact: true })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Создать ссылку на бота' })).toHaveCount(0)
  await expect(page.getByText('YooKassa · 490 RUB', { exact: true }).first()).toBeVisible()
  await expect(page.getByText(/7 days/)).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)

  api.allowCabinetLoad('/api/me/subscriptions')
  api.allowCabinetLoad('/api/me/orders')
  api.allowCabinetLoad('/api/me/telegram/status')
  await page.getByRole('button', { name: 'Повторить загрузку данных' }).first().click()

  await expect(page.getByText(subscription.tariffName, { exact: true }).first()).toBeVisible()
  await expect(page.getByText('490 RUB', { exact: true }).first()).toBeVisible()
  await expect(page.getByRole('button', { name: 'Создать ссылку на бота' })).toBeVisible()
  await expect(page.getByText('Не удалось загрузить подписки и связанные VPN-доступы.', { exact: true })).toHaveCount(0)
  expect(api.getRequestCount('/api/me/subscriptions')).toBe(2)
  expect(api.getRequestCount('/api/me/orders')).toBe(2)
  expect(api.getRequestCount('/api/me/telegram/status')).toBe(2)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet app version modal traps focus and restores its opener', async ({ page }) => {
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))
  const api = await mockCabinetApi(page)
  api.showAppVersionRelease()
  await seedCabinetSession(page, 'access-token-cabinet-version', 'refresh-token-cabinet-version')

  await page.goto('/')
  await expect(page.getByRole('navigation', { name: 'Основная навигация' }).getByRole('link', { name: 'Помощь' }))
    .toHaveAttribute('href', 'http://127.0.0.1:5293/help')
  const opener = page.getByRole('button', { name: 'Что нового' })
  await expect(opener).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/app-version/latest')).toBeGreaterThan(0)
  await opener.click()
  const dialog = page.getByRole('dialog', { name: 'Проверка модального окна' })
  await expect(dialog).toBeVisible()
  await expect(dialog).toBeFocused()
  const dialogBox = await dialog.boundingBox()
  const viewport = page.viewportSize()
  expect(dialogBox).not.toBeNull()
  expect(viewport).not.toBeNull()
  expect(dialogBox!.x).toBeGreaterThanOrEqual(0)
  expect(dialogBox!.y).toBeGreaterThanOrEqual(0)
  expect(dialogBox!.x + dialogBox!.width).toBeLessThanOrEqual(viewport!.width)
  expect(dialogBox!.y + dialogBox!.height).toBeLessThanOrEqual(viewport!.height)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await expect.poll(() => page.evaluate(() => ({ inert: document.getElementById('root')?.inert, overflow: document.body.style.overflow })))
    .toEqual({ inert: true, overflow: 'hidden' })

  if (page.viewportSize()!.width <= 820) {
    const historyToggle = page.getByRole('button', { name: 'История' })
    await historyToggle.click()
    await expect(page.getByRole('button', { name: 'Скрыть' })).toBeFocused()
    await page.getByRole('button', { name: /Версия 0\.573\.0/ }).click()
    await expect(historyToggle).toBeFocused()
  }

  await dialog.focus()
  await page.keyboard.press('Shift+Tab')
  await expect(dialog.locator(':focus')).toHaveCount(1)
  await page.keyboard.press('Tab')
  await expect(page.getByRole('button', { name: 'Закрыть окно что нового' })).toBeFocused()

  await page.keyboard.press('Escape')
  await expect(dialog).toHaveCount(0)
  await expect(opener).toBeFocused()
  await expect.poll(() => page.evaluate(() => ({ inert: document.getElementById('root')?.inert, overflow: document.body.style.overflow })))
    .toEqual({ inert: false, overflow: '' })
  expect(browserErrors).toEqual([])
})

test('cabinet app version waits for the user identity before applying a local dismissal', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.showUnseenAppVersionRelease()
  api.delayNextProfileRequest()
  await seedCabinetSession(page, 'access-token-version-dismissed', 'refresh-token-version-dismissed')
  await page.addInitScript(({ userId, releaseId }) => {
    localStorage.setItem(`appVersion.dismissed.${userId}.${releaseId}`, '1')
  }, { userId: user.id, releaseId: appVersionRelease.releaseId })

  await page.goto('/')
  const dialog = page.getByRole('dialog', { name: appVersionRelease.title })
  await expect.poll(() => api.getRequestCount('/api/me')).toBe(1)
  expect(api.getRequestCount('/api/app-version/latest')).toBe(0)
  await expect(dialog).toHaveCount(0)

  api.releaseProfileRequest()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/app-version/latest')).toBeGreaterThan(0)
  await expect(dialog).toHaveCount(0)
})

test('cabinet app version preserves a manual open while the user identity is loading', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.showAppVersionRelease()
  api.failAppVersionMarkSeen()
  api.delayNextProfileRequest()
  await seedCabinetSession(page, 'access-token-version-manual', 'refresh-token-version-manual')

  await page.goto('/')
  await expect.poll(() => api.getRequestCount('/api/me')).toBe(1)
  const opener = page.getByRole('button', { name: 'Что нового' })
  await expect(opener).toBeVisible()
  await opener.click()
  expect(api.getRequestCount('/api/app-version/latest')).toBe(0)
  const dialog = page.getByRole('dialog', { name: appVersionRelease.title })
  await expect(dialog).toHaveCount(0)

  api.releaseProfileRequest()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/app-version/latest')).toBe(1)
  await expect(dialog).toBeVisible()
  await page.waitForTimeout(300)
  expect(api.getRequestCount('/api/app-version/latest')).toBe(1)

  await dialog.getByRole('button', { name: 'Закрыть окно что нового' }).evaluate((button) => {
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  })
  await expect(dialog).toHaveCount(0)
  await expect.poll(() => api.getRequestCount('/api/app-version/mark-seen', 'POST')).toBe(1)
  await page.waitForTimeout(300)
  expect(api.getRequestCount('/api/app-version/mark-seen', 'POST')).toBe(1)

  api.allowAppVersionMarkSeen()
  await opener.click()
  await expect(dialog).toBeVisible()
  await dialog.getByRole('button', { name: 'Закрыть окно что нового' }).click()
  await expect(dialog).toHaveCount(0)
  await expect.poll(() => api.getRequestCount('/api/app-version/mark-seen', 'POST')).toBe(2)
})

test('cabinet app version manual failure and empty result retry only on demand', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.failAppVersionLatest()
  await seedCabinetSession(page, 'access-token-version-latest-error', 'refresh-token-version-latest-error')

  await page.goto('/')
  const opener = page.getByRole('button', { name: 'Что нового' })
  await expect(opener).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/app-version/latest')).toBe(1)
  await opener.click()

  const dialog = page.getByRole('dialog', { name: 'Что нового' })
  await expect(dialog).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/app-version/latest')).toBe(2)
  await page.waitForTimeout(300)
  expect(api.getRequestCount('/api/app-version/latest')).toBe(2)
  expect(api.getRequestCount('/api/app-version/history')).toBe(0)
  await expect(dialog.getByRole('alert')).toContainText('Не удалось загрузить информацию об обновлениях')

  api.allowAppVersionLatest()
  await dialog.getByRole('button', { name: 'Повторить загрузку' }).evaluate((button) => {
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  })
  await expect.poll(() => api.getRequestCount('/api/app-version/latest')).toBe(3)
  await expect(dialog.getByRole('status')).toContainText('Опубликованные обновления пока отсутствуют')
  await page.waitForTimeout(300)
  expect(api.getRequestCount('/api/app-version/latest')).toBe(3)
  expect(api.getRequestCount('/api/app-version/history')).toBe(0)

  api.showAppVersionRelease()
  await dialog.getByRole('button', { name: 'Повторить загрузку' }).evaluate((button) => {
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  })
  await expect.poll(() => api.getRequestCount('/api/app-version/latest')).toBe(4)
  await expect(page.getByRole('dialog', { name: appVersionRelease.title })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/app-version/history')).toBe(1)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet app version history fails once and retries only on demand', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.showAppVersionRelease()
  api.failAppVersionHistory()
  await seedCabinetSession(page, 'access-token-version-history-error', 'refresh-token-version-history-error')

  await page.goto('/')
  await expect(page.getByRole('button', { name: 'Что нового' })).toBeVisible()
  await page.getByRole('button', { name: 'Что нового' }).click()
  const dialog = page.getByRole('dialog', { name: appVersionRelease.title })
  await expect(dialog).toBeVisible()
  if (page.viewportSize()!.width <= 820) {
    await dialog.getByRole('button', { name: 'История' }).click()
  }

  await expect.poll(() => api.getRequestCount('/api/app-version/history')).toBeGreaterThan(0)
  await page.waitForTimeout(300)
  expect(api.getRequestCount('/api/app-version/history')).toBe(1)
  await expect(dialog.getByRole('alert')).toContainText('Не удалось загрузить историю обновлений')

  api.allowAppVersionHistory()
  api.returnEmptyAppVersionHistory()
  await dialog.getByRole('button', { name: 'Повторить загрузку истории' }).click()
  await expect.poll(() => api.getRequestCount('/api/app-version/history')).toBe(2)
  await page.waitForTimeout(300)
  expect(api.getRequestCount('/api/app-version/history')).toBe(2)
  await expect(dialog.getByRole('button', { name: /Версия 0\.573\.0/ })).toBeVisible()
  await expect(dialog.getByRole('alert')).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet app version rejects history completed by a logged-out session', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.showAppVersionRelease()
  api.delayNextAppVersionHistory()
  await seedCabinetSession(page, 'access-token-version-old', 'refresh-token-version-old')

  await page.goto('/')
  const opener = page.getByRole('button', { name: 'Что нового' })
  await expect.poll(() => api.getRequestCount('/api/app-version/latest')).toBeGreaterThan(0)
  await opener.click()
  await expect(page.getByRole('dialog', { name: appVersionRelease.title })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/app-version/history')).toBe(1)
  await page.keyboard.press('Escape')

  await page.getByRole('button', { name: 'Выйти', exact: true }).click()
  const authPanel = page.locator('#cabinet-auth-panel')
  await authPanel.getByLabel('Email').fill(user.email)
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await authPanel.getByRole('button', { name: 'Войти' }).click()
  await expect.poll(() => api.getAuthorizedRequestCount(
    '/api/app-version/latest',
    `Bearer access-token-${user.email}`
  )).toBeGreaterThan(0)

  await page.getByRole('button', { name: 'Что нового' }).click()
  const currentDialog = page.getByRole('dialog', { name: appVersionRelease.title })
  await expect(currentDialog).toBeVisible()
  if (page.viewportSize()!.width <= 820) {
    await currentDialog.getByRole('button', { name: 'История' }).click()
  }
  await expect.poll(() => api.getRequestCount('/api/app-version/history')).toBe(2)
  api.releaseAppVersionHistory()

  await expect(page.getByRole('button', { name: /Версия 0\.573\.0/ })).toBeVisible()
  await expect(page.getByRole('button', { name: /Версия 0\.572\.0/ })).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet payment providers fail once and recover only on explicit retry', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.failPaymentProviders()
  await seedCabinetSession(page, 'access-token-provider-retry', 'refresh-token-provider-retry')

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Сессия и оплата' })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/public/payments/providers')).toBe(1)
  await page.waitForTimeout(300)
  expect(api.getRequestCount('/api/public/payments/providers')).toBe(1)
  await expect(page.getByRole('alert').filter({ hasText: 'Не удалось загрузить способы оплаты' })).toHaveCount(1)
  await expect(page.getByText('Нет включенных способов оплаты для оплат из кабинета.')).toHaveCount(0)

  api.allowPaymentProviders()
  await page.getByRole('button', { name: 'Повторить загрузку способов оплаты' }).evaluate((button) => {
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  })
  await expect.poll(() => api.getRequestCount('/api/public/payments/providers')).toBe(2)
  const providerSelect = page.getByLabel('Способ оплаты для продления')
  await expect(providerSelect).toBeEnabled()
  await expect(providerSelect).toHaveValue('YooKassa')
  await expect(page.getByText('Доступно способов оплаты: 1.')).toBeVisible()
  await expect(page.getByRole('alert').filter({ hasText: 'Не удалось загрузить способы оплаты' })).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet locks the selected payment provider while retry payment is pending', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.useMultiplePaymentProviders()
  api.delayNextRetryPaymentRequest()
  await seedCabinetSession(page, 'access-token-provider-lock', 'refresh-token-provider-lock')

  await page.goto('/')
  const providerSelect = page.getByLabel('Способ оплаты для продления')
  await expect(providerSelect).toHaveValue('YooKassa')

  const retryableOrderCard = page.locator('.payment-record').filter({ hasText: 'Повторная оплата' })
  await retryableOrderCard.getByRole('button', { name: 'Повторить оплату' }).click()
  await expect.poll(() => api.getRequestCount('/api/me/orders/order-retryable/payments/YooKassa/init', 'POST')).toBe(1)

  await expect(providerSelect).toBeDisabled()
  await expect(providerSelect).toHaveValue('YooKassa')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)

  api.releaseRetryPaymentRequest()
  await expect(page.getByRole('heading', { name: 'Последняя повторная оплата' })).toBeVisible()
  await expect(providerSelect).toBeEnabled()
})

test('cabinet support messages failure stays scoped and recovers on explicit retry', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.useSupportConversationRaceFixture()
  api.failSupportMessages('support-first')
  await seedCabinetSession(page, 'access-token-support-retry', 'refresh-token-support-retry')

  await page.goto('/')
  await expect(page.getByRole('button', { name: /Первая переписка/ })).toBeVisible()
  const messagesPath = '/api/me/support/conversations/support-first/messages'
  await expect.poll(() => api.getRequestCount(messagesPath)).toBe(1)
  await page.waitForTimeout(300)
  expect(api.getRequestCount(messagesPath)).toBe(1)
  await expect(page.getByRole('alert').filter({ hasText: 'Не удалось загрузить переписку поддержки' })).toHaveCount(1)
  await expect(page.getByRole('heading', { name: 'Сообщений нет' })).toHaveCount(0)

  api.allowSupportMessages()
  await page.getByRole('button', { name: 'Повторить загрузку переписки' }).evaluate((button) => {
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  })
  await expect.poll(() => api.getRequestCount(messagesPath)).toBe(2)
  await expect(page.getByText('Секрет первой переписки', { exact: true })).toBeVisible()
  await expect(page.getByRole('alert').filter({ hasText: 'Не удалось загрузить переписку поддержки' })).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet keeps the selected support thread when an older request finishes late', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.useSupportConversationRaceFixture()
  api.delaySupportMessages('support-first')
  await seedCabinetSession(page, 'access-token-support-race', 'refresh-token-support-race')

  await page.goto('/')

  await expect(page.getByRole('button', { name: /Первая переписка/ })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/me/support/conversations/support-first/messages')).toBe(1)
  await page.getByLabel('Ответ').fill('Черновик первой переписки')
  await page.getByRole('button', { name: /Вторая переписка/ }).click()
  await expect(page.getByLabel('Ответ')).toHaveValue('')
  await expect(page.getByText('Ответ второй переписки', { exact: true })).toBeVisible()

  api.releaseSupportMessages()
  await expect.poll(() => api.getRequestCount('/api/me/support/conversations/support-second/messages')).toBe(1)
  await expect(page.getByText('Ответ второй переписки', { exact: true })).toBeVisible()
  await expect(page.getByText('Секрет первой переписки', { exact: true })).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet logout invalidates delayed support data and clears private drafts', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.useSupportConversationRaceFixture()
  await seedCabinetSession(page, 'access-token-support-cleanup', 'refresh-token-support-cleanup')

  await page.goto('/')
  await expect(page.getByText('Секрет первой переписки', { exact: true })).toBeVisible()
  await page.getByLabel('Тема').fill('Черновик старого пользователя')
  await page.getByLabel('Связанный заказ').selectOption(paidOrder.id)
  await page.getByLabel('Связанная подписка').selectOption(subscription.id)
  await page.getByLabel('Сообщение').fill('Приватный текст старого пользователя')
  await page.getByLabel('Ответ').fill('Приватный ответ старого пользователя')

  api.delaySupportMessages('support-second')
  await page.getByRole('button', { name: /Вторая переписка/ }).click()
  await expect.poll(() => api.getRequestCount('/api/me/support/conversations/support-second/messages')).toBe(1)
  await page.getByRole('button', { name: 'Выйти', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Вход в личный кабинет' })).toBeVisible()

  api.releaseSupportMessages()
  const authPanel = page.locator('#cabinet-auth-panel')
  await authPanel.getByLabel('Email').fill(user.email)
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await authPanel.getByRole('button', { name: 'Войти' }).click()

  await expect(page.getByText('Секрет первой переписки', { exact: true })).toBeVisible()
  await expect(page.getByText('Ответ второй переписки', { exact: true })).toHaveCount(0)
  await expect(page.getByLabel('Тема')).toHaveValue('')
  await expect(page.getByLabel('Связанный заказ')).toHaveValue('')
  await expect(page.getByLabel('Связанная подписка')).toHaveValue('')
  await expect(page.getByLabel('Сообщение')).toHaveValue('')
  await expect(page.getByLabel('Ответ')).toHaveValue('')
})

test('cabinet keeps a newer support draft and rejects duplicate creation', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.delayNextSupportCreateRequest()
  await seedCabinetSession(page, 'access-token-support-create', 'refresh-token-support-create')

  await page.goto('/')
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await page.getByLabel('Тема').fill('Исходная тема обращения')
  await page.getByLabel('Сообщение').fill('Исходный текст обращения')

  const createButton = page.getByRole('button', { name: 'Создать обращение' })
  await createButton.evaluate((button) => {
    const form = (button as HTMLButtonElement).form
    form?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
    form?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
  })
  await expect.poll(() => api.getRequestCount('/api/me/support/conversations', 'POST')).toBe(1)

  await page.getByLabel('Тема').fill('Новый черновик после отправки')
  await page.getByLabel('Сообщение').fill('Новый текст не должен исчезнуть')
  api.releaseSupportCreateRequest()

  await expect(page.getByRole('button', { name: /Исходная тема обращения/ })).toBeVisible()
  await expect(page.getByRole('textbox', { name: 'Тема' })).toHaveValue('Новый черновик после отправки')
  await expect(page.getByLabel('Сообщение')).toHaveValue('Новый текст не должен исчезнуть')
  expect(api.getRequestCount('/api/me/support/conversations', 'POST')).toBe(1)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet rotates an expired restored access token once', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.expireAccessToken('access-token-restored-expired')
  await seedCabinetSession(page, 'access-token-restored-expired', 'refresh-token-restored-valid')

  await page.goto('/')

  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
  await expect(page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).resolves.toEqual({
    access: restoredAuthResponse.accessToken,
    refresh: restoredAuthResponse.refreshToken
  })
})

test('cabinet preserves a restored session after a transient profile failure', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.failNextProfileRequest()
  await seedCabinetSession(page, 'access-token-restored-valid', 'refresh-token-restored-valid')

  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Восстановление сессии' })).toBeVisible()
  await expect(page.getByRole('alert')).toContainText('Не удалось выполнить запрос. Попробуйте еще раз.')
  await expect(page.getByText('profile_temporarily_unavailable')).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Повторить загрузку' })).toBeEnabled()
  for (const falseLoadedState of privateCabinetStateLabels) {
    await expect(page.getByText(falseLoadedState, { exact: true })).toHaveCount(0)
  }
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await expect(page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).resolves.toEqual({
    access: 'access-token-restored-valid',
    refresh: 'refresh-token-restored-valid'
  })

  await page.getByRole('button', { name: 'Повторить загрузку' }).click()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  expect(api.getRequestCount('/api/auth/refresh', 'POST')).toBe(0)
})

test('cabinet clears a restored session when refresh is rejected', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.expireAccessToken('access-token-restored-expired')
  api.failRefreshRequest()
  await seedCabinetSession(page, 'access-token-restored-expired', 'refresh-token-restored-invalid')

  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Вход в личный кабинет' })).toBeVisible()
  await expect(page.getByText('Сессия завершена или доступ к аккаунту ограничен. Войдите заново.')).toBeVisible()
  await expect(page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).resolves.toEqual({ access: null, refresh: null })
  expect(api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
})

test('cabinet ignores a delayed restored-session refresh after logout', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.expireAccessToken('access-token-restored-expired')
  api.delayNextRefreshRequest()
  await seedCabinetSession(page, 'access-token-restored-expired', 'refresh-token-restored-valid')

  await page.goto('/')
  await expect.poll(() => api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
  await page.getByRole('button', { name: 'Выйти' }).click()
  await expect(page.getByRole('heading', { name: 'Вход в личный кабинет' })).toBeVisible()

  api.releaseRefreshRequest()
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).toEqual({ access: null, refresh: null })
  await expect(page.getByText(user.email, { exact: true })).toHaveCount(0)
})

test('cabinet keeps manual refresh single-flight', async ({ page }) => {
  const api = await mockCabinetApi(page)
  await seedCabinetSession(page, 'access-token-manual-refresh', 'refresh-token-manual-refresh')

  await page.goto('/')
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  api.delayNextRefreshRequest()

  const refreshButton = page.getByRole('button', { name: 'Обновить сессию' })
  await refreshButton.evaluate((button) => {
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  })
  await expect.poll(() => api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
  api.releaseRefreshRequest()

  await expect(page.getByText('Сессия обновлена.')).toBeVisible()
  expect(api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
})

test('cabinet keeps manual data reload single-flight across synchronous activation', async ({ page }) => {
  const api = await mockCabinetApi(page)
  await seedCabinetSession(page, 'access-token-data-reload', 'refresh-token-data-reload')
  const loadPaths = [
    '/api/me',
    '/api/me/subscriptions',
    '/api/me/orders',
    '/api/me/payments',
    '/api/me/accesses',
    '/api/me/referrals',
    '/api/me/support/conversations',
    '/api/me/telegram/status'
  ]

  await page.goto('/')
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  expect(loadPaths.map((path) => api.getRequestCount(path))).toEqual(Array(8).fill(1))

  const reloadButton = page.getByRole('button', { name: 'Обновить данные' })
  await reloadButton.evaluate((button) => {
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  })

  await expect.poll(() => loadPaths.map((path) => api.getRequestCount(path))).toEqual(Array(8).fill(2))
  await expect(page.getByText('Обновляем...')).toHaveCount(0)
  await page.waitForTimeout(300)
  expect(loadPaths.map((path) => api.getRequestCount(path))).toEqual(Array(8).fill(2))
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet invalidates stale QR after logout or a failed reload', async ({ page }) => {
  const api = await mockCabinetApi(page)
  api.delayNextQrRequest()
  await seedCabinetSession(page, 'access-token-old-action', 'refresh-token-old-action')

  await page.goto('/')
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()

  const qrButton = page.getByRole('button', { name: 'Показать QR-код' }).first()
  await qrButton.evaluate((button) => {
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  })
  await expect.poll(() => api.getRequestCount('/api/cabinet/access/access-active/qr')).toBe(1)

  await page.getByRole('button', { name: 'Выйти', exact: true }).click()
  const authPanel = page.locator('#cabinet-auth-panel')
  await authPanel.getByLabel('Email').fill(user.email)
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await authPanel.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()

  api.releaseQrRequest()
  await expect(page.locator('.qr-preview')).toHaveCount(0)
  await expect(page.getByText('QR-код загружен.')).toHaveCount(0)
  expect(api.getRequestCount('/api/cabinet/access/access-active/qr')).toBe(1)
  expect(api.getAuthorizedRequestCount('/api/cabinet/access/access-active/qr', 'Bearer access-token-old-action')).toBe(1)

  await qrButton.click()
  await expect.poll(() => page.locator('.qr-preview').count()).toBeGreaterThan(0)
  const qrRequestsBeforeFailure = api.getRequestCount('/api/cabinet/access/access-active/qr')
  api.failNextQrRequest()
  await qrButton.click()
  await expect.poll(() => api.getRequestCount('/api/cabinet/access/access-active/qr')).toBe(qrRequestsBeforeFailure + 1)
  await expect(page.getByText('Не удалось загрузить QR-код. Повторите попытку.')).toBeVisible()
  await expect(page.locator('.qr-preview')).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('cabinet Telegram and support status operations persist across reload', async ({ page }) => {
  test.slow()
  const consoleErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') consoleErrors.push(message.text())
  })
  page.on('pageerror', (error) => consoleErrors.push(error.message))

  const api = await mockCabinetApi(page)
  await seedCabinetSession(page, 'access-token-cabinet-operations', 'refresh-token-cabinet-operations')
  await page.goto('/')
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()

  const telegramCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Telegram' }) })
  await telegramCard.getByRole('button', { name: 'Создать ссылку на бота' }).click()
  await expect(page.getByText('Ссылка на Telegram-бота создана.')).toBeVisible()
  await expect(telegramCard.getByRole('link', { name: 'Открыть Telegram-бота в новой вкладке' }))
    .toHaveAttribute('href', 'https://t.me/vpnplatform_bot?start=link_cabinet-e2e')
  expect(api.getLastRequest('/api/me/telegram/link-token')?.body).toEqual({})

  api.markTelegramLinked()
  await page.getByRole('button', { name: 'Обновить данные' }).click()
  await expect(telegramCard.getByText('@cabinet_e2e')).toBeVisible()
  await telegramCard.getByRole('button', { name: 'Отвязать Telegram' }).click()
  await expect(page.getByText('Telegram отвязан от аккаунта.')).toBeVisible()
  await expect(telegramCard.getByRole('button', { name: 'Создать ссылку на бота' })).toBeVisible()
  expect(api.getLastRequest('/api/me/telegram/unlink', 'DELETE')).toBeDefined()

  await page.getByLabel('Тема').fill('Проверка статуса обращения')
  await page.getByLabel('Сообщение').fill('Закрытие и повторное открытие должны сохраняться после обновления данных.')
  await page.getByRole('button', { name: 'Создать обращение' }).click()
  await expect(page.getByRole('button', { name: /Проверка статуса обращения/ })).toBeVisible()

  await page.getByRole('button', { name: 'Закрыть' }).click()
  await expect(page.getByText('Обращение закрыто.', { exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Переоткрыть' })).toBeVisible()
  expect(api.getLastRequest('/api/me/support/conversations/support-created/status', 'PATCH')?.body)
    .toEqual({ status: 'closed', revision: 0 })

  await page.getByRole('button', { name: 'Переоткрыть' }).click()
  await expect(page.getByText('Обращение переоткрыто.')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Закрыть' })).toBeVisible()
  expect(api.getLastRequest('/api/me/support/conversations/support-created/status', 'PATCH')?.body)
    .toEqual({ status: 'open', revision: 1 })

  await page.getByRole('button', { name: 'Обновить данные' }).click()
  await expect(page.getByRole('button', { name: /Проверка статуса обращения, статус open/ })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Закрыть' })).toBeVisible()
  expect(api.getRequestCount('/api/me/support/conversations/support-created/status', 'PATCH')).toBe(2)
  expect(api.getAuthorizedRequestCount('/api/me/telegram/unlink', 'Bearer access-token-cabinet-operations', 'DELETE')).toBe(1)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(consoleErrors).toEqual([])
})

test('cabinet covers register, login, payments, subscription access and support', async ({ page }, testInfo) => {
  test.slow()
  const consoleErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') consoleErrors.push(message.text())
  })
  page.on('pageerror', (error) => consoleErrors.push(error.message))

  const api = await mockCabinetApi(page)

  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'clipboard', {
      configurable: true,
      value: {
        writeText: async (value: string) => sessionStorage.setItem('cabinet-e2e-copied-value', value)
      }
    })
  })

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Личный кабинет', exact: true })).toBeVisible()
  await page.getByRole('tab', { name: 'Регистрация' }).click()

  const authPanel = page.locator('#cabinet-auth-panel')
  await authPanel.getByLabel('Имя').fill(user.displayName)
  await authPanel.getByLabel('Email').fill(user.email)
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await authPanel.getByLabel('Реферальный код').fill('CABINET-REF')
  await authPanel.getByRole('button', { name: 'Зарегистрироваться' }).click()

  await expect(page.getByText('Аккаунт создан.')).toBeVisible()
  await expect(page.getByText('Pro 30 дней').first()).toBeVisible()
  await expect(page.locator('.code-block').filter({ hasText: 'vless://cabinet-e2e@example.com:443' }).first()).toBeVisible()
  await expect(page.getByText('yk-paid-1')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'EU Sandbox' })).toBeVisible()
  const unsafeLinkPaymentCard = page.locator('.payment-record').filter({ hasText: 'order-unsafe-link' })
  await expect(unsafeLinkPaymentCard.getByRole('alert')).toContainText('Сохраненная ссылка оплаты отклонена как некорректная')
  await expect(unsafeLinkPaymentCard.getByRole('link', { name: 'Открыть оплату' })).toHaveCount(0)
  await expect(unsafeLinkPaymentCard.getByRole('button', { name: /Скопировать ссылку/ })).toHaveCount(0)

  const firstCopyButton = page.getByRole('button', { name: /Скопировать ссылку: скопировать значение/ }).first()
  await firstCopyButton.click()
  await expect(page.getByText('Скопировано', { exact: true }).first()).toBeVisible()
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem('cabinet-e2e-copied-value'))).toBe(subscription.accessUri)
  await page.evaluate(() => {
    Object.defineProperty(navigator.clipboard, 'writeText', {
      configurable: true,
      value: async () => { throw new DOMException('Clipboard permission denied', 'NotAllowedError') }
    })
  })
  await firstCopyButton.click()
  await expect(page.getByText('Не удалось скопировать', { exact: true }).first()).toBeVisible()
  const referralRewardRow = page.locator('.list-item').filter({ hasText: 'Бонусные дни' })
  await expect(referralRewardRow.getByText('7 days', { exact: false })).toBeVisible()
  await expect(referralRewardRow.getByText('Подтверждено')).toBeVisible()

  const staleOrderCard = page.locator('.payment-record').filter({ hasText: 'Истёкший заказ' })
  await expect(staleOrderCard.getByText('Срок истек')).toBeVisible()
  await expect(staleOrderCard.getByText('Срок оплаты заказа истёк. Создайте новый заказ с актуальным сроком оплаты.')).toBeVisible()
  await expect(staleOrderCard.getByRole('button', { name: 'Повторить оплату' })).toHaveCount(0)
  await expect(staleOrderCard.getByRole('link', { name: 'Создать новый заказ' })).toHaveAttribute('href', /\/tariffs$/)

  const retryableOrderCard = page.locator('.payment-record').filter({ hasText: 'Повторная оплата' })
  await page.evaluate(() => window.history.replaceState({}, '', '/?source=private-campaign#payment-history'))
  await retryableOrderCard.getByRole('button', { name: 'Повторить оплату' }).click()
  await expect(page.getByRole('heading', { name: 'Последняя повторная оплата' })).toBeVisible()
  await expect(page.getByText('payment-retry')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Открыть повторную оплату в новой вкладке' })).toHaveAttribute('href', 'https://pay.example.test/retry')
  expect(api.getLastRequest('/api/me/orders/order-retryable/payments/YooKassa/init')?.body).toEqual({ returnUrl: 'http://127.0.0.1:5294' })

  const blockedCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Заблокированный тариф' }) })
  const cancelledCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Отменённый тариф' }) })
  const cancelledStaleSubscriptionCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Отменённый stale тариф' }) })
  await expect(blockedCard.getByText('Продление заблокировано. Обратитесь в поддержку.')).toBeVisible()
  await expect(cancelledCard.getByText('Отменённую подписку нельзя продлить. Оформите новый тариф.')).toBeVisible()
  await expect(blockedCard.getByRole('button', { name: 'Продлить' })).toHaveCount(0)
  await expect(cancelledCard.getByRole('button', { name: 'Продлить' })).toHaveCount(0)
  await expect(cancelledStaleSubscriptionCard.getByText('Родительская подписка отменена. Ключ и QR-код больше недоступны.')).toBeVisible()
  await expect(cancelledStaleSubscriptionCard.getByRole('button', { name: 'Показать QR-код' })).toHaveCount(0)
  await expect(cancelledStaleSubscriptionCard.getByRole('button', { name: 'Скопировать ссылку' })).toHaveCount(0)

  const pendingAccessCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Ожидает выдачи' }) })
  await expect(pendingAccessCard.getByText('Ссылка ещё не выдана')).toBeVisible()
  await expect(pendingAccessCard.getByRole('button', { name: 'Показать QR-код' })).toBeDisabled()

  const revokedAccessCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Отозванный доступ' }) })
  await expect(revokedAccessCard.getByText('Доступ отозван. Ключ и QR-код больше недоступны.')).toBeVisible()
  await expect(revokedAccessCard.getByRole('button', { name: 'Показать QR-код' })).toHaveCount(0)
  await expect(revokedAccessCard.getByRole('button', { name: 'Скопировать ссылку' })).toHaveCount(0)
  await expect(page.getByText('vless://revoked-cabinet-secret@example.test')).toHaveCount(0)
  expect(api.getLastRequest('/api/cabinet/access/access-revoked/qr', 'GET')).toBeUndefined()

  const cancelledStaleAccessCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Отменённый stale доступ' }) })
  await expect(cancelledStaleAccessCard.getByText('Родительская подписка отменена. Ключ и QR-код больше недоступны.')).toBeVisible()
  await expect(cancelledStaleAccessCard.getByRole('button')).toHaveCount(0)
  await expect(page.getByText('vless://cancelled-cabinet-stale-secret@example.test')).toHaveCount(0)
  await expect(page.getByText('qr://cancelled-cabinet-stale-secret')).toHaveCount(0)
  await expect(page.getByText('config://cancelled-cabinet-stale-secret')).toHaveCount(0)
  expect(api.getLastRequest('/api/cabinet/access/access-cancelled-stale/qr', 'GET')).toBeUndefined()

  await page.getByRole('button', { name: 'Показать QR-код' }).first().click()
  const qrPreview = page.locator('.qr-preview').first()
  await expect(qrPreview.getByRole('img', { name: 'QR-код VPN-доступа' })).toBeVisible()
  await expect(qrPreview.locator('svg')).toHaveCount(0)
  await expect(qrPreview.getByRole('img')).toHaveAttribute('src', /^data:image\/svg\+xml;charset=utf-8,/)
  api.returnUnsafeQrSvg()
  await page.getByRole('button', { name: 'Показать QR-код' }).first().click()
  await expect(page.getByRole('alert').filter({ hasText: 'QR-код отклонен' }).first()).toBeVisible()
  await expect.poll(() => page.evaluate(() => (window as typeof window & { __cabinetQrExecuted?: boolean }).__cabinetQrExecuted ?? false)).toBe(false)

  api.failNextRenewalPayment()
  await page.getByRole('button', { name: 'Продлить' }).first().click()
  const renewalCard = page.getByRole('heading', { name: 'Последнее продление' }).locator('..')
  await expect(renewalCard).toContainText('ID заказа: order-renewal')
  await expect(renewalCard).toContainText('Заказ сохранён, но ссылка оплаты ещё не подготовлена.')
  await expect(page.getByRole('alert').filter({ hasText: 'Заказ на продление order-renewal создан' })).toContainText('но ссылку оплаты подготовить не удалось.')
  await expect(page.getByText('payment provider unavailable')).toHaveCount(0)
  expect(api.getRequestCount('/api/me/orders', 'POST')).toBe(1)
  expect(api.getRequestCount('/api/me/orders/order-renewal/payments/YooKassa/init', 'POST')).toBe(1)
  await renewalCard.getByRole('button', { name: 'Повторить подготовку оплаты' }).click()
  await expect(renewalCard.getByText('payment-renewal')).toBeVisible()
  await expect(page.getByText('Ссылка оплаты для созданного продления подготовлена.')).toBeVisible()
  expect(api.getRequestCount('/api/me/orders', 'POST')).toBe(1)
  expect(api.getRequestCount('/api/me/orders/order-renewal/payments/YooKassa/init', 'POST')).toBe(2)
  expect(api.getLastRequest('/api/me/orders')?.body).toMatchObject({
    tariffId: 'tariff-pro',
    type: 'Renewal',
    paymentProvider: 'YooKassa',
    subscriptionId: 'sub-active'
  })
  expect(api.getLastRequest('/api/me/orders')?.body).not.toHaveProperty('channel')
  expect(api.getLastRequest('/api/me/orders')?.body).not.toHaveProperty('isFirstPurchase')

  await page.getByLabel('Тема').fill('Не вижу продление')
  await page.getByLabel('Сообщение').fill('Оплата создана, хочу проверить статус подписки и доступ.')
  await page.getByLabel('Связанный заказ').selectOption('order-paid')
  await page.getByLabel('Связанная подписка').selectOption('sub-active')
  await page.getByRole('button', { name: 'Создать обращение' }).click()

  await expect(page.getByText('Обращение в поддержку создано.')).toBeVisible()
  await expect(page.getByRole('button', { name: /Не вижу продление/ })).toBeVisible()
  await expect(page.getByText('Оплата создана, хочу проверить статус подписки и доступ.')).toBeVisible()

  await page.getByLabel('Ответ').fill('Дополняю обращение из E2E.')
  await page.getByRole('button', { name: 'Отправить' }).click()
  await expect(page.getByText('Сообщение отправлено в поддержку.')).toBeVisible()
  await expect(page.getByText('Дополняю обращение из E2E.')).toBeVisible()

  await page.getByRole('button', { name: 'Выйти' }).click()
  await page.getByRole('tab', { name: 'Вход' }).click()
  await expect(page.getByRole('tabpanel', { name: 'Вход' })).toBeVisible()
  expect(api.getLastRequest('/api/auth/logout')?.body).toEqual({ refreshToken: `refresh-token-${user.email}` })
  expect(api.getLastRequest('/api/auth/logout')?.authorization).toBe(`Bearer access-token-${user.email}`)
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  const loginPanel = page.locator('#cabinet-auth-panel')
  await loginPanel.getByLabel('Email').fill(user.email)
  await loginPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  const profileLoadsBeforeRelogin = api.getRequestCount('/api/me')
  await loginPanel.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByText('Вход выполнен.')).toBeVisible()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  expect(api.getRequestCount('/api/me')).toBe(profileLoadsBeforeRelogin + 1)

  api.rejectAuthorizedRequests()
  await page.getByRole('button', { name: 'Показать QR-код' }).first().click()
  await expect(page.getByText('Сессия завершена или доступ к аккаунту ограничен. Войдите заново.')).toBeVisible()
  await expect(page.getByRole('tabpanel', { name: 'Вход' })).toBeVisible()
  await expect(page.getByText('vless://cabinet-e2e@example.com:443', { exact: false })).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).toEqual({ access: null, refresh: null })
  for (const message of consoleErrors.filter((item) => item.includes('401 (Unauthorized)'))) consoleErrors.splice(consoleErrors.indexOf(message), 1)

  api.allowAuthorizedRequests()
  const reloginPanel = page.locator('#cabinet-auth-panel')
  await reloginPanel.getByLabel('Email').fill(user.email)
  await reloginPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await reloginPanel.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  const profileLoadsBeforeRestore = api.getRequestCount('/api/me')
  await page.reload()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/me')).toBe(profileLoadsBeforeRestore + 1)

  const resetCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Сброс пароля' }) })
  await resetCard.getByLabel('Email').fill(user.email)
  const forgotPasswordRequests = api.getRequestCount('/api/auth/forgot-password', 'POST')
  await resetCard.getByLabel('Email').press('Enter')
  await expect.poll(() => api.getRequestCount('/api/auth/forgot-password', 'POST')).toBe(forgotPasswordRequests + 1)
  await resetCard.getByRole('textbox', { name: 'Новый пароль', exact: true }).fill('ChangedPassword123!')
  await resetCard.getByRole('button', { name: 'Сохранить пароль' }).click()
  await expect(page.getByText('Пароль изменён. Войдите с новым паролем.')).toBeVisible()
  await expect(page.getByRole('tabpanel', { name: 'Вход' })).toBeVisible()
  await expect(page.getByText('vless://cabinet-e2e@example.com:443', { exact: false })).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  const supportMessageLoadsBeforePasswordRelogin = api.getRequestCount('/api/me/support/conversations/support-created/messages')
  await reloginPanel.getByLabel('Email').fill(user.email)
  await reloginPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('ChangedPassword123!')
  await reloginPanel.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/me/support/conversations/support-created/messages'))
    .toBeGreaterThan(supportMessageLoadsBeforePasswordRelogin)

  api.rejectNextAuthorizedPath('/api/me/support/conversations/support-created/messages')
  await page.getByLabel('Тема').fill('Проверка завершения сессии')
  await page.getByLabel('Сообщение').fill('Повторная загрузка переписки должна завершить локальную сессию.')
  await page.getByRole('button', { name: 'Создать обращение' }).click()
  await expect(page.getByText('Сессия завершена или доступ к аккаунту ограничен. Войдите заново.')).toBeVisible()
  await expect(page.getByText('Обращение в поддержку создано.')).toHaveCount(0)
  await expect(page.getByRole('tabpanel', { name: 'Вход' })).toBeVisible()
  for (const message of consoleErrors.filter((item) => item.includes('401 (Unauthorized)'))) consoleErrors.splice(consoleErrors.indexOf(message), 1)

  await reloginPanel.getByLabel('Email').fill(user.email)
  await reloginPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await reloginPanel.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()

  api.returnInvalidSubscriptionsResponse()
  await page.reload()
  await expect(page.getByRole('heading', { name: 'Личный кабинет', exact: true })).toBeVisible()
  const subscriptionsLoadError = page.locator('.cabinet-load-error').filter({ hasText: 'Не удалось загрузить подписки и связанные VPN-доступы.' })
  await expect(subscriptionsLoadError).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()
  await expect(page.getByText('Подписок пока нет', { exact: true })).toHaveCount(0)
  await expect(page.getByText(subscription.tariffName, { exact: true })).toHaveCount(0)
  await expect(page.getByText('vless://cabinet-e2e@example.com:443', { exact: false }).first()).toBeVisible()

  api.failLogout()
  await page.getByRole('button', { name: 'Выйти' }).click()
  await expect(page.getByText('Локальная сессия завершена, но отзыв серверной сессии не подтверждён. На чужом устройстве измените пароль из доверенного браузера.')).toBeVisible()
  await expect(page.getByRole('tabpanel', { name: 'Вход' })).toBeVisible()
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).toEqual({ access: null, refresh: null })
  const expectedLogoutFailureLogs = consoleErrors.filter((message) => message.includes('503 (Service Unavailable)'))
  expect(expectedLogoutFailureLogs.length).toBeGreaterThan(0)
  for (const message of expectedLogoutFailureLogs) consoleErrors.splice(consoleErrors.indexOf(message), 1)

  expect(api.getLastRequest('/api/auth/register')?.body).toMatchObject({
    email: user.email,
    displayName: user.displayName,
    referralCode: 'CABINET-REF'
  })
  expect(api.getLastRequest('/api/auth/login')?.body).toMatchObject({
    email: user.email
  })
  if (testInfo.project.name.startsWith('mobile-')) {
    await page.screenshot({ path: testInfo.outputPath('cabinet-mobile.png'), fullPage: true })
  }

  expect(consoleErrors).toEqual([])
})
