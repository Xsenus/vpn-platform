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
  status: 'Paid',
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

function authResponse(email = user.email) {
  return {
    accessToken: `access-token-${email}`,
    refreshToken: `refresh-token-${email}`,
    email,
    displayName: user.displayName
  }
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
  let supportMessages: unknown[] = []
  let logoutShouldFail = false
  let authorizedRequestsRejected = false
  let rejectedAuthorizedPath: string | null = null
  let renewalOrder = {
    ...paidOrder,
    id: 'order-renewal',
    status: 'PendingPayment',
    type: 'Renewal',
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

    if (rejectedAuthorizedPath === path && request.headers().authorization) {
      rejectedAuthorizedPath = null
      await fulfillJson(route, { error: 'user_not_active' }, 401)
      return
    }

    if (method === 'GET' && path === '/api/me') {
      await fulfillJson(route, user)
      return
    }

    if (method === 'GET' && path === '/api/me/subscriptions') {
      await fulfillJson(route, [subscription, blockedSubscription, cancelledSubscription, cancelledStaleSubscription])
      return
    }

    if (method === 'GET' && path === '/api/me/orders') {
      await fulfillJson(route, [paidOrder, stalePendingOrder])
      return
    }

    if (method === 'GET' && path === '/api/me/payments') {
      await fulfillJson(route, [paidPayment])
      return
    }

    if (method === 'GET' && path === '/api/me/accesses') {
      await fulfillJson(route, [access, pendingAccess, revokedAccess, cancelledStaleAccess])
      return
    }

    if (method === 'GET' && path === '/api/me/referrals') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/me/support/conversations') {
      await fulfillJson(route, supportConversations)
      return
    }

    if (method === 'POST' && path === '/api/me/support/conversations') {
      const payload = request.postDataJSON()
      const conversation = {
        id: 'support-created',
        userId: user.id,
        telegramUserId: null,
        channel: 'Web',
        status: 'open',
        subject: payload.subject,
        assignedToUserId: null,
        internalNote: '',
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
      supportMessages = [firstMessage]
      await fulfillJson(route, conversation)
      return
    }

    if (method === 'GET' && path === '/api/me/support/conversations/support-created/messages') {
      await fulfillJson(route, supportMessages)
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
      supportMessages = [...supportMessages, message]
      await fulfillJson(route, message)
      return
    }

    if (method === 'GET' && path === '/api/me/telegram/status') {
      await fulfillJson(route, { isLinked: false, telegramUserId: null, username: null, linkedAt: null })
      return
    }

    if (method === 'GET' && path === '/api/public/payments/providers') {
      await fulfillJson(route, [provider])
      return
    }

    if (method === 'POST' && path === '/api/me/orders') {
      const payload = request.postDataJSON()
      renewalOrder = {
        ...renewalOrder,
        tariffId: payload.tariffId,
        paymentProvider: payload.paymentProvider
      }
      await fulfillJson(route, renewalOrder)
      return
    }

    if (method === 'POST' && path === '/api/me/orders/order-renewal/payments/YooKassa/init') {
      await fulfillJson(route, renewalPayment)
      return
    }

    if (method === 'GET' && path === '/api/cabinet/access/access-active/qr') {
      await route.fulfill({
        status: 200,
        headers: {
          ...corsHeaders,
          'content-type': 'image/svg+xml; charset=utf-8'
        },
        body: '<svg role="img" aria-label="qr-e2e"><text>qr-e2e</text></svg>'
      })
      return
    }

    if (method === 'GET' && path === '/api/app-version/latest') {
      await fulfillJson(route, { currentVersion: null, latestRelease: null, seenByCurrentUser: true })
      return
    }

    if (method === 'GET' && path === '/api/app-version/history') {
      await fulfillJson(route, [])
      return
    }

    await fulfillJson(route, { error: `Unhandled ${method} ${path}` }, 404)
  })

  return {
    requests,
    failLogout: () => { logoutShouldFail = true },
    rejectAuthorizedRequests: () => { authorizedRequestsRejected = true },
    allowAuthorizedRequests: () => { authorizedRequestsRejected = false },
    rejectNextAuthorizedPath: (path: string) => { rejectedAuthorizedPath = path },
    getLastRequest: (path: string, method = 'POST') =>
      requests.findLast((item) => item.method === method && new URL(item.url).pathname === path)
  }
}

test('cabinet covers register, login, payments, subscription access and support', async ({ page }, testInfo) => {
  const consoleErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') consoleErrors.push(message.text())
  })
  page.on('pageerror', (error) => consoleErrors.push(error.message))

  const api = await mockCabinetApi(page)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Личный кабинет', exact: true })).toBeVisible()
  await page.getByRole('tab', { name: 'Регистрация' }).click()

  const authPanel = page.locator('#cabinet-auth-panel')
  await authPanel.getByLabel('Имя').fill(user.displayName)
  await authPanel.getByLabel('Email').fill(user.email)
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await authPanel.getByRole('button', { name: 'Зарегистрироваться' }).click()

  await expect(page.getByText('Аккаунт создан.')).toBeVisible()
  await expect(page.getByText('Pro 30 дней').first()).toBeVisible()
  await expect(page.locator('.code-block').filter({ hasText: 'vless://cabinet-e2e@example.com:443' }).first()).toBeVisible()
  await expect(page.getByText('yk-paid-1')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'EU Sandbox' })).toBeVisible()

  const staleOrderCard = page.locator('.payment-record').filter({ hasText: 'Истёкший заказ' })
  await expect(staleOrderCard.getByText('Expired')).toBeVisible()
  await expect(staleOrderCard.getByText('Срок оплаты заказа истёк. Создайте новый заказ с актуальным сроком оплаты.')).toBeVisible()
  await expect(staleOrderCard.getByRole('button', { name: 'Повторить оплату' })).toHaveCount(0)
  await expect(staleOrderCard.getByRole('link', { name: 'Создать новый заказ' })).toHaveAttribute('href', /\/tariffs$/)

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
  await expect(page.locator('.qr-preview').first()).toContainText('qr-e2e')

  await page.getByRole('button', { name: 'Продлить' }).first().click()
  await expect(page.getByRole('heading', { name: 'Последнее продление' })).toBeVisible()
  await expect(page.getByText('payment-renewal')).toBeVisible()
  expect(api.getLastRequest('/api/me/orders')?.body).toMatchObject({
    tariffId: 'tariff-pro',
    type: 'Renewal',
    channel: 'Web',
    paymentProvider: 'YooKassa',
    subscriptionId: 'sub-active'
  })

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
  await loginPanel.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByText('Вход выполнен.')).toBeVisible()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()

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

  const resetCard = page.locator('.card').filter({ has: page.getByRole('heading', { name: 'Сброс пароля' }) })
  await resetCard.getByLabel('Email').fill(user.email)
  await resetCard.getByRole('button', { name: 'Запросить код' }).click()
  await resetCard.getByRole('textbox', { name: 'Новый пароль', exact: true }).fill('ChangedPassword123!')
  await resetCard.getByRole('button', { name: 'Сохранить пароль' }).click()
  await expect(page.getByText('Пароль изменён. Войдите с новым паролем.')).toBeVisible()
  await expect(page.getByRole('tabpanel', { name: 'Вход' })).toBeVisible()
  await expect(page.getByText('vless://cabinet-e2e@example.com:443', { exact: false })).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-cabinet-token'),
    refresh: sessionStorage.getItem('vpn-platform-cabinet-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  await reloginPanel.getByLabel('Email').fill(user.email)
  await reloginPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('ChangedPassword123!')
  await reloginPanel.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByText(user.email, { exact: true })).toBeVisible()

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
    displayName: user.displayName
  })
  expect(api.getLastRequest('/api/auth/login')?.body).toMatchObject({
    email: user.email
  })
  if (testInfo.project.name.startsWith('mobile-')) {
    await page.screenshot({ path: testInfo.outputPath('cabinet-mobile.png'), fullPage: true })
  }

  expect(consoleErrors).toEqual([])
})
