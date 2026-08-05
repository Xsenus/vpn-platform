import test from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import {
  ApiClient,
  ApiClientError,
  buildAuthHeaders,
  isValidEmail,
  normalizeApiError,
  translateAuthError,
  translateAuthMessage,
  validateAuthInput,
  validatePasswordResetConfirm,
  validatePasswordResetRequest
} from '../packages/api-client/src/index.ts'

test('buildAuthHeaders returns bearer header when token exists', () => {
  assert.deepEqual(buildAuthHeaders('abc'), { Authorization: 'Bearer abc' })
  assert.deepEqual(buildAuthHeaders(''), {})
})

test('normalizeApiError prefers error field and message field', () => {
  assert.equal(normalizeApiError({ error: 'boom' }, 'fallback'), 'boom')
  assert.equal(normalizeApiError({ message: 'denied' }, 'fallback'), 'denied')
  assert.equal(normalizeApiError(null, 'fallback'), 'fallback')
})

test('ApiClient errors preserve HTTP status and normalized payload', async () => {
  globalThis.fetch = (async () => new Response(
    JSON.stringify({ error: 'forbidden' }),
    { status: 403, headers: { 'Content-Type': 'application/json' } }
  )) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await assert.rejects(
    () => client.getAdminDashboardSummary('user-token'),
    (error: unknown) => {
      assert.ok(error instanceof ApiClientError)
      assert.equal(error.status, 403)
      assert.equal(error.message, 'forbidden')
      assert.deepEqual(error.payload, { error: 'forbidden' })
      return true
    }
  )
})

test('ApiClient.getAdminSession loads the capability contract with bearer auth', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({
      userId: 'admin-1',
      email: 'admin@example.test',
      displayName: 'Admin',
      roles: ['FinanceManager'],
      capabilities: { adminRead: true, financeRead: true, financeWrite: true }
    }), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const session = await client.getAdminSession('admin-token')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/session')
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.deepEqual(session.roles, ['FinanceManager'])
  assert.equal(session.capabilities.financeWrite, true)
})

test('auth helpers validate forms and translate backend codes to Russian text', () => {
  assert.equal(isValidEmail('user@example.test'), true)
  assert.equal(isValidEmail('broken-email'), false)
  assert.deepEqual(validateAuthInput('login', 'user@example.test', 'Password123!'), [])
  assert.deepEqual(validateAuthInput('register', 'bad-email', 'short'), [
    'Введите корректный email.',
    'Пароль должен быть не короче 8 символов.'
  ])
  assert.deepEqual(validatePasswordResetRequest('user@example.test'), [])
  assert.deepEqual(validatePasswordResetConfirm('token', 'NewPassword123!'), [])
  assert.equal(translateAuthError(new Error('invalid_credentials')), 'Неверный email или пароль.')
  assert.equal(translateAuthError(new Error('email_exists')), 'Аккаунт с таким email уже зарегистрирован. Войдите или восстановите пароль.')
  assert.equal(translateAuthError(new Error('invalid_referral_code')), 'Реферальный код не найден или больше недоступен.')
  assert.equal(
    translateAuthMessage('If the account exists, a password reset instruction has been queued for the configured delivery channel.'),
    'Если аккаунт существует, инструкция по сбросу пароля поставлена в очередь отправки.'
  )
})

test('ApiClient.register sends normalized optional referral code', async () => {
  let body: unknown = null
  globalThis.fetch = (async (_url: string | URL, init?: RequestInit) => {
    body = JSON.parse(String(init?.body))
    return new Response(JSON.stringify({ accessToken: 'a', refreshToken: 'r', email: 'user@example.test', displayName: 'User' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  await new ApiClient('http://localhost:8080').register('user@example.test', 'Password123!', 'User', ' REF-CODE ')

  assert.deepEqual(body, { email: 'user@example.test', password: 'Password123!', displayName: 'User', referralCode: 'REF-CODE' })
})

test('ApiClient.getTariffs calls public endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify([{ id: '1', name: '1 month' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const response = await client.getTariffs()

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/public/tariffs')
  assert.equal(response[0]?.id, '1')
})

test('ApiClient.getPublicPaymentProviders calls public providers endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify([{ provider: 'YooKassa', publicName: 'YooKassa sandbox', mode: 'Sandbox', healthStatus: 'Healthy' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const response = await client.getPublicPaymentProviders()

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/public/payments/providers')
  assert.equal(response[0]?.provider, 'YooKassa')
  assert.equal(response[0]?.publicName, 'YooKassa sandbox')
})

test('ApiClient FAQ endpoints cover public and admin CRUD', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/api/admin/faq/overview')) {
      return new Response(JSON.stringify({ totalCount: 1, activeCount: 1, hiddenCount: 0, homeCount: 1, faqPageCount: 1, publicCount: 1, categoryCount: 1, categories: ['Подключение'], duplicateQuestions: [], hasPublicFaq: true, hasHomeFaq: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    if (String(url).includes('/api/public/content/faq') || String(url).includes('/api/admin/faq?') || (String(url).endsWith('/api/admin/faq') && (init?.method ?? 'GET') === 'GET')) {
      return new Response(JSON.stringify([{ id: 'faq-1', question: 'Как подключиться?', answer: 'Через кабинет', category: 'Подключение', isActive: true, showOnHome: true, showOnFaqPage: true, sortOrder: 10 }]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ id: 'faq-1', question: 'Как подключиться?', answer: 'Через кабинет', category: 'Подключение', isActive: true, showOnHome: true, showOnFaqPage: true, sortOrder: 10 }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getFaq()
  await client.getHomeFaq()
  await client.getAdminFaq('admin-token')
  const filteredFaq = await client.getAdminFaq('admin-token', { category: 'Подключение', visibility: 'home', search: 'qr' })
  const overview = await client.getAdminFaqOverview('admin-token')
  await client.createAdminFaq('admin-token', { question: 'Как?', answer: 'Так', category: 'Общее', isActive: true, showOnHome: true, showOnFaqPage: true, sortOrder: 10 })
  await client.updateAdminFaq('admin-token', 'faq-1', { question: 'Как?', answer: 'Так', category: 'Общее', isActive: true, showOnHome: false, showOnFaqPage: true, sortOrder: 20 })
  await client.deleteAdminFaq('admin-token', 'faq-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/public/content/faq')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/public/content/faq?home=true')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/faq')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/faq?category=%D0%9F%D0%BE%D0%B4%D0%BA%D0%BB%D1%8E%D1%87%D0%B5%D0%BD%D0%B8%D0%B5&visibility=home&search=qr')
  assert.equal(calls[4]?.url, 'http://localhost:8080/api/admin/faq/overview')
  assert.equal(calls[5]?.init?.method, 'POST')
  assert.equal(calls[6]?.init?.method, 'PUT')
  assert.equal(calls[7]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[7]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(filteredFaq[0]?.category, 'Подключение')
  assert.equal(overview.hasPublicFaq, true)
})

test('ApiClient site content endpoints cover public and admin CRUD', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).includes('/api/public/content/home') || String(url).includes('/api/admin/site-content')) {
      if (String(url).includes('/home-readiness')) {
        return new Response(JSON.stringify({ isReady: true, requiredCount: 18, presentCount: 18, activeRequiredCount: 18, missingKeys: [], inactiveKeys: [], emptyKeys: [], duplicateKeys: [], publicBlocksCount: 18, requiredKeys: ['home.hero.title'] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }

      if (String(url).includes('/home-defaults')) {
        return new Response(JSON.stringify({ created: 1, restored: 2, readiness: { isReady: true, requiredCount: 18, presentCount: 18, activeRequiredCount: 18, missingKeys: [], inactiveKeys: [], emptyKeys: [], duplicateKeys: [], publicBlocksCount: 18, requiredKeys: ['home.hero.title'] } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }

      if (init?.method === 'DELETE') {
        return new Response(JSON.stringify({ id: 'content-1', deleted: true }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }

      const payload = { id: 'content-1', key: 'home.hero.title', value: 'VPN title', group: 'home', label: 'Hero title', description: '', inputType: 'text', isActive: true, sortOrder: 10, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
      return new Response(JSON.stringify(init?.method ? payload : [payload]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    throw new Error(`Unexpected URL ${String(url)}`)
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getHomeContent()
  await client.getAdminSiteContent('admin-token', 'home')
  const readiness = await client.getAdminHomeContentReadiness('admin-token')
  const restored = await client.restoreAdminHomeContentDefaults('admin-token')
  await client.createAdminSiteContent('admin-token', { key: 'home.hero.title', value: 'VPN title', group: 'home', label: 'Hero title', inputType: 'text', isActive: true, sortOrder: 10 })
  await client.updateAdminSiteContent('admin-token', 'content-1', { key: 'home.hero.title', value: 'New title', group: 'home', label: 'Hero title', inputType: 'text', isActive: true, sortOrder: 10 })
  await client.deleteAdminSiteContent('admin-token', 'content-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/public/content/home')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/site-content?group=home')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/site-content/home-readiness')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/site-content/home-defaults')
  assert.equal(calls[3]?.init?.method, 'POST')
  assert.equal(calls[4]?.init?.method, 'POST')
  assert.equal(calls[5]?.init?.method, 'PUT')
  assert.equal(calls[6]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[6]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(readiness.isReady, true)
  assert.equal(restored.created, 1)
})

test('ApiClient work scenario endpoints cover admin CRUD', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  const scenario = { id: 'scenario-1', name: 'Auto', key: 'auto', isActive: true, allowedTariffIdsJson: '[]', vpnProtocol: 'vless', serverSelectionRule: 'least-loaded', inboundSelectionRule: 'default', provisioningMode: 'auto', onPaymentSucceeded: 'create_subscription_and_access', onPaymentFailed: 'keep_order_pending', onRefund: 'disable_access', onSubscriptionExpired: 'disable_access_after_grace', onRenewal: 'extend_subscription', cabinetText: 'ready', telegramText: 'ready', generateQrCode: true, maxDevices: 3, trafficLimit: null, sortOrder: 10, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (init?.method === 'DELETE') {
      return new Response(JSON.stringify({ id: 'scenario-1', deleted: true }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    return new Response(JSON.stringify(init?.method ? scenario : [scenario]), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getAdminWorkScenarios('admin-token')
  await client.createAdminWorkScenario('admin-token', scenario)
  await client.updateAdminWorkScenario('admin-token', 'scenario-1', { ...scenario, name: 'Auto updated' })
  await client.deleteAdminWorkScenario('admin-token', 'scenario-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/work-scenarios')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.equal(calls[2]?.init?.method, 'PUT')
  assert.equal(calls[3]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[3]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient admin tariff endpoints cover extended CRUD', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/api/admin/tariffs') && init?.method !== 'POST') {
      return new Response(JSON.stringify([{ id: 'tariff-1', name: 'Premium', features: ['Автовыдача'], badge: 'Популярный' }]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    if (init?.method === 'DELETE') {
      return new Response(JSON.stringify({ id: 'tariff-1', deleted: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ id: 'tariff-1', name: 'Premium', features: ['Автовыдача'], badge: 'Выгодно' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getAdminTariffs('admin-token')
  await client.createAdminTariff('admin-token', { name: 'Premium', featuresJson: '["Автовыдача"]', badge: 'Популярный', afterPaymentText: 'После оплаты доступ появится в кабинете.' })
  await client.updateAdminTariff('admin-token', 'tariff-1', { badge: 'Выгодно', provisioningScenario: 'premium-auto' })
  await client.deleteAdminTariff('admin-token', 'tariff-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/tariffs')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.equal(calls[2]?.init?.method, 'PATCH')
  assert.equal(calls[3]?.init?.method, 'DELETE')
  assert.match(String(calls[1]?.init?.body), /featuresJson|afterPaymentText/)
  assert.equal(new Headers(calls[3]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient admin referral endpoints cover programs and rewards', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    const isProgramList = String(url).endsWith('/referral-programs') && (init?.method ?? 'GET') === 'GET'
    return new Response(JSON.stringify(String(url).endsWith('/referrals') ? [] : isProgramList ? [{ id: 'program-1', name: 'Welcome', status: 'active' }] : { id: 'program-1', name: 'Welcome', status: 'active' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const payload = {
    name: 'Welcome',
    status: 'active',
    startAt: null,
    endAt: null,
    ruleDefinition: '{"firstPurchaseOnly":true}',
    rewardDefinition: '{"referrer":{"type":"bonus-days","value":7,"unit":"days","autoApprove":true}}',
    antiFraudSettings: '{}'
  }
  const client = new ApiClient('http://localhost:8080')
  await client.getAdminReferralPrograms('admin-token')
  await client.getAdminReferralRewards('admin-token')
  await client.createAdminReferralProgram('admin-token', payload)
  await client.updateAdminReferralProgram('admin-token', 'program-1', payload)

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/referral-programs')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/referrals')
  assert.equal(calls[2]?.init?.method, 'POST')
  assert.equal(calls[3]?.init?.method, 'PATCH')
  assert.match(String(calls[2]?.init?.body), /rewardDefinition/)
  assert.equal(new Headers(calls[3]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient.createMyOrder sends auth header and payload', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ id: 'order-1', userId: 'user-1', tariffId: 'tariff-1', amount: 490, currency: 'RUB', status: 'PendingPayment', expiresAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.createMyOrder('token-123', {
    tariffId: 'tariff-1',
    type: 'NewSubscription',
    paymentProvider: 'YooKassa',
    promoCode: 'WELCOME10',
    subscriptionId: 'subscription-1'
  })

  const headers = new Headers(calls[0]?.init?.headers)
  assert.equal(calls[0]?.url, 'http://localhost:8080/api/me/orders')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(headers.get('Authorization'), 'Bearer token-123')
  assert.match(String(calls[0]?.init?.body), /WELCOME10/)
  assert.match(String(calls[0]?.init?.body), /subscription-1/)
  assert.doesNotMatch(String(calls[0]?.init?.body), /channel|isFirstPurchase/)
})

test('ApiClient.initMyPayment calls tokenized endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ paymentId: 'pay-1', redirectUrl: 'https://example.test/pay-1', rawResponse: '{}' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const result = await client.initMyPayment('token-123', 'order-1', 'RoboKassa')

  const headers = new Headers(calls[0]?.init?.headers)
  assert.equal(calls[0]?.url, 'http://localhost:8080/api/me/orders/order-1/payments/RoboKassa/init')
  assert.equal(headers.get('Authorization'), 'Bearer token-123')
  assert.equal(result.redirectUrl, 'https://example.test/pay-1')
})

test('ApiClient cabinet support endpoints are tokenized and link order context', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  const conversation = { id: 'support-1', userId: 'user-1', channel: 'web', status: 'open', subject: 'Оплата', internalNote: 'Связано: заказ order-1.', revision: 4, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
  const message = { id: 'message-1', supportConversationId: 'support-1', userId: 'user-1', direction: 'inbound', text: 'Нужна помощь', attachmentsJson: '[]', isInternalNote: false, createdAt: new Date().toISOString() }
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    const path = String(url)
    if (path.endsWith('/api/me/support/conversations') && !init?.method) {
      return new Response(JSON.stringify([conversation]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/me/support/conversations') && init?.method === 'POST') {
      return new Response(JSON.stringify(conversation), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/me/support/conversations/support-1/messages') && !init?.method) {
      return new Response(JSON.stringify([message]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/me/support/conversations/support-1/reply') && init?.method === 'POST') {
      return new Response(JSON.stringify(message), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/me/support/conversations/support-1/status') && init?.method === 'PATCH') {
      return new Response(JSON.stringify({ conversationId: 'support-1', status: 'closed', revision: 5 }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    throw new Error(`Unexpected URL ${path}`)
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getMySupportConversations('user-token')
  await client.createMySupportConversation('user-token', { subject: 'Оплата', text: 'Нужна помощь', orderId: 'order-1', subscriptionId: null })
  await client.getMySupportMessages('user-token', 'support-1')
  await client.replyMySupportConversation('user-token', 'support-1', 'Спасибо', 4)
  await client.updateMySupportConversationStatus('user-token', 'support-1', 'closed', 4)

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/me/support/conversations')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.match(String(calls[1]?.init?.body), /order-1/)
  assert.equal(calls[3]?.init?.method, 'POST')
  assert.deepEqual(JSON.parse(String(calls[3]?.init?.body)), { text: 'Спасибо', revision: 4 })
  assert.equal(calls[4]?.init?.method, 'PATCH')
  assert.deepEqual(JSON.parse(String(calls[4]?.init?.body)), { status: 'closed', revision: 4 })
  assert.equal(new Headers(calls[4]?.init?.headers).get('Authorization'), 'Bearer user-token')
})

test('ApiClient cabinet Telegram link status and unlink endpoints are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    const path = String(url)
    if (path.endsWith('/api/me/telegram/link-token')) {
      return new Response(JSON.stringify({ token: 'link-token', deepLinkUrl: 'https://t.me/vpnplatform_bot?start=link_link-token', expiresAt: new Date().toISOString() }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ isLinked: true, telegramUserId: 777001, username: 'ivan', linkedAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const link = await client.createTelegramLinkToken('user-token')
  const status = await client.getTelegramStatus('user-token')
  await client.unlinkTelegram('user-token')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/me/telegram/link-token')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/me/telegram/status')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/me/telegram/unlink')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(calls[2]?.init?.method, 'DELETE')
  assert.match(link.deepLinkUrl, /start=link_/)
  assert.equal(status.username, 'ivan')
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer user-token')
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer user-token')
  assert.equal(new Headers(calls[2]?.init?.headers).get('Authorization'), 'Bearer user-token')
})

test('ApiClient admin server CRUD actions send safe payloads with auth token', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (init?.method === 'DELETE') {
      return new Response(JSON.stringify({ id: 'node-1', deleted: false, archived: true, linkedSubscriptions: 0, linkedAccesses: 0, linkedProvisioningRuns: 0, linkedHealthChecks: 2, linkedMigrationJobs: 1 }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    if (String(url).endsWith('/health-check') && init?.method === 'POST') {
      return new Response(JSON.stringify({ id: 'check-1', nodeId: 'node-1', status: 'Healthy', checkedAt: new Date().toISOString(), latencyMs: 12, metadataJson: '{}', errorText: '' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    if (String(url).endsWith('/health-checks')) {
      return new Response(JSON.stringify([{ id: 'check-1', nodeId: 'node-1', status: 'Healthy', checkedAt: new Date().toISOString(), latencyMs: 12, metadataJson: '{}', errorText: '' }]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ id: 'node-1', name: 'nl-01', host: 'nl-01.example.com', ipAddress: '203.0.113.10', provider: 'hetzner', region: 'eu', country: 'NL', datacenter: 'fsn1', status: 'New', capacity: 5000, usedCapacity: 0, supportedProtocolsCsv: 'vless,vmess,trojan', healthStatus: 'Unknown', installedVersion: '', backupStatus: 'unknown', monitoringStatus: 'unknown', loggingStatus: 'unknown', tagsCsv: '', priority: 100, isAvailableForNewUsers: false }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const payload = {
    name: 'nl-01',
    host: 'nl-01.example.com',
    ipAddress: '203.0.113.10',
    provider: 'hetzner',
    region: 'eu',
    country: 'NL',
    datacenter: 'fsn1',
    capacity: 5000,
    supportedProtocolsCsv: 'vless,vmess,trojan',
    priority: 100,
    tagsCsv: '',
    sshUser: 'root',
    sshPort: 22,
    sshPrivateKeyPath: null,
    sshAuthMethod: 'ssh_key',
    sshCredential: 'PRIVATE-KEY-VALUE',
    validationMode: true,
    ownerType: 'admin',
    skipHostKeyChecking: true,
    panelBaseUrl: 'https://vpn.example.com:2053',
    panelUsername: 'admin',
    panelPassword: 'secret',
    panelInboundId: 1,
    publicHostname: 'vpn.example.com',
    publicPort: 443,
    nodeGroupId: null
  }

  await client.createAdminServer('admin-token', payload)
  await client.updateAdminServer('admin-token', 'node-1', { ...payload, name: 'nl-01-edited', priority: 200, tagsCsv: 'tier:premium' })
  await client.disableAdminServer('admin-token', 'node-1')
  await client.checkAdminServerHealth('admin-token', 'node-1')
  await client.getAdminServerHealthChecks('admin-token', 'node-1')
  const deletion = await client.deleteAdminServer('admin-token', 'node-1')

  const headers = new Headers(calls[0]?.init?.headers)
  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/servers')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(headers.get('Authorization'), 'Bearer admin-token')
  assert.match(String(calls[0]?.init?.body), /nl-01/)
  assert.match(String(calls[0]?.init?.body), /sshCredential/)
  assert.match(String(calls[0]?.init?.body), /validationMode/)
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/servers/node-1')
  assert.equal(calls[1]?.init?.method, 'PUT')
  assert.match(String(calls[1]?.init?.body), /nl-01-edited/)
  assert.match(String(calls[1]?.init?.body), /tier:premium/)
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/servers/node-1/disable')
  assert.equal(calls[2]?.init?.method, 'POST')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/servers/node-1/health-check')
  assert.equal(calls[3]?.init?.method, 'POST')
  assert.equal(calls[4]?.url, 'http://localhost:8080/api/admin/servers/node-1/health-checks')
  assert.equal(calls[4]?.init?.method, undefined)
  assert.equal(calls[5]?.url, 'http://localhost:8080/api/admin/servers/node-1')
  assert.equal(calls[5]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[5]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(deletion.archived, true)
  assert.equal(deletion.linkedHealthChecks, 2)
  assert.equal(deletion.linkedMigrationJobs, 1)
})

test('ApiClient provisioning run details and actions are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/deploy')) {
      return new Response(JSON.stringify({ runId: 'run-1', status: 'DeployQueued', dryRun: false, mode: 'validation-deploy', modeTitle: 'Validation deploy', riskLevel: 'low', liveDeployAllowed: false }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/cancel')) {
      return new Response(JSON.stringify({ runId: 'run-1', status: 'cancelled' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/support-needed')) {
      return new Response(JSON.stringify({ runId: 'run-1', supportConversationId: 'support-1' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    return new Response(JSON.stringify({
      run: { id: 'run-1', nodeId: 'node-1', nodeName: 'customer-vps', targetHost: 'vps.example.com', sshPort: 22, username: 'root', authMethod: 'ssh_key', credentialsConfigured: true, status: 'ReadyToDeploy', currentStep: 'ready_to_deploy', dryRun: true, mode: 'dry-run', modeTitle: 'Dry-run precheck', riskLevel: 'safe', liveDeployAllowed: false, deployMode: 'validation-deploy', deployModeTitle: 'Validation deploy', deployRiskLevel: 'low', deployLiveDeployAllowed: false, startedAt: new Date().toISOString(), executionLog: 'password=***', linkedAccessId: null, createdAt: new Date().toISOString() },
      steps: [{ id: 'step-1', provisioningRunId: 'run-1', stepName: 'Validate input', status: 'Succeeded', output: 'credentials=***', errorText: '', createdAt: new Date().toISOString() }]
    }), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const details = await client.getAdminProvisioningRun('admin-token', 'run-1')
  const deploy = await client.deployAdminProvisioningRun('admin-token', 'run-1')
  await client.cancelAdminProvisioningRun('admin-token', 'run-1')
  await client.markAdminProvisioningSupportNeeded('admin-token', 'run-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/provisioning-runs/run-1')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/provisioning-runs/run-1/deploy')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/provisioning-runs/run-1/cancel')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/provisioning-runs/run-1/support-needed')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.equal(new Headers(calls[3]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(details.run.credentialsConfigured, true)
  assert.equal(details.run.mode, 'dry-run')
  assert.equal(details.run.deployMode, 'validation-deploy')
  assert.equal(deploy.mode, 'validation-deploy')
  assert.equal(details.steps[0]?.output, 'credentials=***')
})

test('ApiClient.queueAdminProvision calls provisioning endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ serverId: 'node-1', runId: 'run-1', status: 'queued', dryRun: false, mode: 'live-deploy', modeTitle: 'Live deploy', riskLevel: 'high', liveDeployAllowed: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const response = await client.queueAdminProvision('admin-token', 'node-1')
  const headers = new Headers(calls[0]?.init?.headers)

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/servers/node-1/provision')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(headers.get('Authorization'), 'Bearer admin-token')
  assert.equal(response.runId, 'run-1')
  assert.equal(response.mode, 'live-deploy')
  assert.equal(response.riskLevel, 'high')
})

test('ApiClient.createCheckoutSession calls public checkout-session endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ id: 'session-1', token: 'checkout-token', tariffId: 'tariff-1', status: 'open', expiresAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const response = await client.createCheckoutSession({
    tariffId: 'tariff-1',
    type: 'NewSubscription',
    paymentProvider: 'YooKassa',
    promoCode: null,
    emailHint: null,
    returnUrl: 'http://localhost:5173/account'
  })

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/public/checkout-sessions')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.match(String(calls[0]?.init?.body), /YooKassa/)
  assert.doesNotMatch(String(calls[0]?.init?.body), /channel|isFirstPurchase/)
  assert.equal(response.token, 'checkout-token')
})

test('ApiClient.claimCheckoutSession binds session through authenticated endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ id: 'order-1', userId: 'user-1', tariffId: 'tariff-1', amount: 490, currency: 'RUB', status: 'PendingPayment', expiresAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.claimCheckoutSession('jwt-token', 'checkout-token')
  const headers = new Headers(calls[0]?.init?.headers)

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/me/checkout-sessions/checkout-token/claim')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(headers.get('Authorization'), 'Bearer jwt-token')
})

test('ApiClient admin support reply and note endpoints are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    const body = String(init?.body ?? '')
    if (String(url).endsWith('/notes')) {
      return new Response(JSON.stringify({ id: 'msg-1', supportConversationId: 'conv-1', direction: 'internal', text: 'note', attachmentsJson: '[]', isInternalNote: true, createdAt: new Date().toISOString() }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ conversationId: 'conv-1', status: body.includes('pending') ? 'pending' : 'queued' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.replyAdminSupportConversation('admin-token', 'conv-1', 'reply', 7)
  await client.updateAdminSupportConversationStatus('admin-token', 'conv-1', 'pending', 8)
  await client.addAdminSupportInternalNote('admin-token', 'conv-1', 'note', 9)

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/support/conversations/conv-1/reply')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/support/conversations/conv-1/status')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/support/conversations/conv-1/notes')
  assert.deepEqual(JSON.parse(String(calls[0]?.init?.body)), { text: 'reply', revision: 7 })
  assert.deepEqual(JSON.parse(String(calls[1]?.init?.body)), { status: 'pending', assignedToUserId: null, revision: 8 })
  assert.deepEqual(JSON.parse(String(calls[2]?.init?.body)), { text: 'note', revision: 9 })
  assert.equal(new Headers(calls[2]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient.getAdminAccesses calls admin VPN access endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify([{ id: 'access-1', subscriptionId: 'sub-1', providerType: 'x3ui', providerAccessId: 'client-1', serverId: 'node-1', accessUri: 'vless://test', qrCodePath: 'vless://test', configPath: '', status: 'Active', issuedAt: new Date().toISOString(), revision: 1 }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const response = await client.getAdminAccesses('admin-token')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/access-credentials')
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(response[0]?.accessUri, 'vless://test')
})

test('ApiClient VPN panel endpoints are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (init?.method === 'DELETE') {
      return new Response(JSON.stringify({ id: 'panel-1', deleted: true, archived: false, linkedInbounds: 0, linkedClients: 0, linkedSyncRuns: 0, linkedHealthChecks: 0 }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).includes('/test-connection') || String(url).includes('/health-checks')) {
      return new Response(JSON.stringify(String(url).includes('/health-checks') ? [] : { id: 'health-1', vpnPanelId: 'panel-1', status: 'Healthy', version: '2.4.12', latencyMs: 12, checkedAt: new Date().toISOString(), errorMessage: '' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).includes('/vpn-clients/')) {
      return new Response(JSON.stringify({ id: 'client-1', userId: 'user-1', subscriptionId: 'sub-1', vpnPanelId: 'panel-1', vpnInboundId: 'inbound-1', externalClientId: 'client-1', email: 'user@example.test', uuid: 'client-uuid', flow: '', limitIp: 3, totalGb: null, expiryTime: new Date().toISOString(), enable: !String(url).includes('/disable'), configUri: 'vless://client', qrCodePayload: 'vless://client', syncStatus: 'synced', lastSyncedAt: new Date().toISOString() }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).includes('/sync')) {
      return new Response(JSON.stringify({ id: 'sync-1', vpnPanelId: 'panel-1', status: 'Succeeded', startedAt: new Date().toISOString(), summaryJson: '{}', errorMessage: '' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).includes('/inbounds')) {
      return new Response(JSON.stringify(String(init?.method ?? 'GET') === 'POST' ? { id: 'inbound-1', vpnPanelId: 'panel-1', externalInboundId: '1', name: 'default-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{}', streamSettingsJson: '{}', sniffingJson: '{}', isDefault: true, isActive: true, capacity: 5000, usedCapacity: 0 } : []), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).includes('/vpn-inbounds/')) {
      return new Response(JSON.stringify({ id: 'inbound-1', vpnPanelId: 'panel-1', externalInboundId: '1', name: 'default-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{}', streamSettingsJson: '{"network":"tcp"}', sniffingJson: '{}', isDefault: true, isActive: false, capacity: 5000, usedCapacity: 0 }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).includes('/clients')) {
      return new Response(JSON.stringify([]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (new URL(String(url)).pathname === '/api/admin/vpn-panels' && (init?.method ?? 'GET') === 'GET') {
      return new Response(JSON.stringify([{ id: 'panel-1', name: 'panel', baseUrl: 'https://panel.example.test', region: 'eu', status: 'Active', healthStatus: 'Healthy', login: 'admin', sslVerificationMode: 'Strict', apiVariant: 'X3UiOfficial', capacity: 5000, usedCapacity: 0, autoCreateInbound: false, defaultInboundTemplateJson: '{}', version: '2.4.12', lastError: '', createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    return new Response(JSON.stringify({ id: 'panel-1', name: 'panel', baseUrl: 'https://panel.example.test', region: 'eu', status: 'Active', healthStatus: 'Healthy', login: 'admin', sslVerificationMode: 'Strict', apiVariant: 'X3UiOfficial', capacity: 5000, usedCapacity: 0, autoCreateInbound: false, defaultInboundTemplateJson: '{}', version: '2.4.12', lastError: '', createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getAdminVpnPanels('admin-token')
  await client.createAdminVpnPanel('admin-token', { name: 'panel', baseUrl: 'https://panel.example.test', login: 'admin', password: 'secret', region: 'eu', capacity: 5000, sslVerificationMode: 'Strict', apiVariant: 'X3UiOfficial', autoCreateInbound: false, defaultInboundTemplateJson: '{}' })
  await client.updateAdminVpnPanel('admin-token', 'panel-1', { name: 'edited-panel', password: '', sslVerificationMode: 'AllowSelfSigned', apiVariant: 'ThreeXUi', autoCreateInbound: true })
  await client.testAdminVpnPanel('admin-token', 'panel-1')
  await client.syncAdminVpnPanel('admin-token', 'panel-1')
  await client.getAdminVpnPanelInbounds('admin-token', 'panel-1')
  await client.createAdminVpnPanelInbound('admin-token', 'panel-1', { name: 'default-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{}', streamSettingsJson: '{"network":"tcp"}', sniffingJson: '{}', isDefault: true, capacity: 5000, isActive: true })
  await client.updateAdminVpnInbound('admin-token', 'inbound-1', { name: 'default-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{}', streamSettingsJson: '{"network":"tcp"}', sniffingJson: '{}', isDefault: false, capacity: 5000, isActive: false })
  await client.setAdminVpnInboundDefault('admin-token', 'inbound-1')
  await client.getAdminVpnPanelClients('admin-token', 'panel-1')
  await client.disableAdminVpnClient('admin-token', 'client-1')
  await client.enableAdminVpnClient('admin-token', 'client-1')
  await client.syncAdminVpnClient('admin-token', 'client-1')
  await client.resetAdminVpnClientTraffic('admin-token', 'client-1')
  await client.migrateAdminVpnClient('admin-token', 'client-1', 'inbound-2')
  await client.getAdminVpnPanelHealthChecks('admin-token', 'panel-1')
  await client.deleteAdminVpnPanel('admin-token', 'panel-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/vpn-panels')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/vpn-panels')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1')
  assert.equal(calls[2]?.init?.method, 'PATCH')
  assert.match(String(calls[2]?.init?.body), /AllowSelfSigned/)
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/test-connection')
  assert.equal(calls[4]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/sync')
  assert.equal(calls[5]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/inbounds')
  assert.equal(calls[6]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/inbounds')
  assert.match(String(calls[6]?.init?.body), /"isActive":true/)
  assert.equal(calls[7]?.url, 'http://localhost:8080/api/admin/vpn-inbounds/inbound-1')
  assert.equal(calls[7]?.init?.method, 'PATCH')
  assert.match(String(calls[7]?.init?.body), /"isActive":false/)
  assert.equal(calls[8]?.url, 'http://localhost:8080/api/admin/vpn-inbounds/inbound-1/set-default')
  assert.equal(calls[8]?.init?.method, 'POST')
  assert.equal(calls[9]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/clients')
  assert.equal(calls[10]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/disable')
  assert.equal(calls[10]?.init?.method, 'POST')
  assert.equal(calls[11]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/enable')
  assert.equal(calls[12]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/sync')
  assert.equal(calls[13]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/reset-traffic')
  assert.equal(calls[14]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/migrate')
  assert.match(String(calls[14]?.init?.body), /inbound-2/)
  assert.equal(calls[15]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/health-checks')
  assert.equal(calls[16]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1')
  assert.equal(calls[16]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient admin dashboard and user overview endpoints are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).includes('/overview')) {
      return new Response(JSON.stringify({ user: { id: 'user-1', displayName: 'User' }, orders: [], payments: [], subscriptions: [], accessCredentials: [], supportConversations: [] }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({
      totalUsers: 1,
      activeSubscriptions: 1,
      productionReadiness: {
        isReady: false,
        status: 'Blocked',
        checks: [{ key: 'payment-webhook', label: 'Webhook платежей', status: 'Blocked', message: 'Webhook URL не заполнен', category: 'Платежи', severity: 'critical', actionLabel: 'Открыть платежи', actionHref: '#payments' }]
      },
      generatedAt: new Date().toISOString()
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const dashboard = await client.getAdminDashboardSummary('admin-token')
  await client.getAdminUserOverview('admin-token', 'user-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/dashboard/summary')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/users/user-1/overview')
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(dashboard.productionReadiness?.checks[0]?.actionHref, '#payments')
  assert.equal(dashboard.productionReadiness?.checks[0]?.category, 'Платежи')
})

test('ApiClient admin audit logs endpoint sends filters and auth token', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify([{ id: 'audit-1', actorType: 'admin', actorId: 'user-1', action: 'payment_provider.update', entityType: 'PaymentProviderAccount', entityId: 'account-1', beforeJson: '{}', afterJson: '{}', ip: '127.0.0.1', userAgent: 'test', createdAt: '2026-06-12T10:30:00Z' }]), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const response = await client.getAdminAuditLogs('admin-token', { action: 'payment_provider', entityType: 'PaymentProviderAccount', actorType: 'admin', search: 'account-1', limit: 50 })

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/audit-logs?action=payment_provider&entityType=PaymentProviderAccount&actorType=admin&search=account-1&limit=50')
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(response[0]?.action, 'payment_provider.update')
})

test('ApiClient admin payment providers expose readiness fields without secrets', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify([{ id: 'account-1', provider: 'YooKassa', mode: 'Sandbox', name: 'Yoo', publicName: 'Yoo', isEnabled: true, isDefault: true, shopId: 'shop', apiBaseUrl: '', returnUrl: '', webhookUrl: 'https://api.example.test/webhooks/payments/yookassa', hasSecretKey: true, hasWebhookSecret: false, useWebhookIpAllowList: false, allowedWebhookIpRangesCsv: '', extraSettingsJson: '{"apiSecret":"***"}', healthStatus: 'Unknown', isCheckoutConfigured: true, checkoutConfigurationIssue: null, capabilitiesJson: '["createPayment"]', capabilities: [{ key: 'createPayment', label: 'Создание платежа', supported: true, status: 'supported' }], requiredFields: [{ key: 'shopId', label: 'ShopId / merchant id', required: true, configured: true, issue: null }], readinessBlockers: [], isPubliclyAvailable: true }]), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const response = await client.getAdminPaymentProviderAccounts('admin-token')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/payment-providers/accounts')
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(response[0]?.isCheckoutConfigured, true)
  assert.equal(response[0]?.extraSettingsJson, '{"apiSecret":"***"}')
  assert.equal(response[0]?.webhookUrl, 'https://api.example.test/webhooks/payments/yookassa')
  assert.equal(response[0]?.isPubliclyAvailable, true)
  assert.equal(response[0]?.capabilities?.[0]?.label, 'Создание платежа')
  assert.equal(response[0]?.requiredFields?.[0]?.configured, true)
  })

test('ApiClient admin payment providers can create, update and toggle accounts', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    const body = { id: 'account-1', provider: 'Stripe', mode: 'Sandbox', name: 'stripe-main', publicName: 'Stripe', isEnabled: true, isDefault: true, shopId: 'shop', apiBaseUrl: 'https://api.stripe.com', returnUrl: '', webhookUrl: 'https://api.example.test/webhooks/payments/stripe', hasSecretKey: true, hasWebhookSecret: true, useWebhookIpAllowList: false, allowedWebhookIpRangesCsv: '', extraSettingsJson: '{}', healthStatus: 'Unknown', isCheckoutConfigured: true, checkoutConfigurationIssue: null, capabilitiesJson: '["createPayment"]' }
    return new Response(JSON.stringify(String(url).endsWith('/check') ? { accountId: 'account-1', provider: 'Stripe', mode: 'Sandbox', isReady: true, checkScope: 'ConfigurationOnly', configurationStatus: 'Ready', healthStatus: 'Unknown', message: 'Configuration is ready. External provider account was not requested.', details: ['Checkout configuration is ready.'], checkedAt: new Date().toISOString(), account: body } : body), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const payload = { provider: 'Stripe' as const, mode: 'Sandbox' as const, name: 'stripe-main', publicName: 'Stripe', isEnabled: true, isDefault: true, shopId: 'shop', apiBaseUrl: 'https://api.stripe.com', returnUrl: '', webhookUrl: 'https://api.example.test/webhooks/payments/stripe', secretKey: 'sk_test', webhookSecret: 'whsec_test', useWebhookIpAllowList: false, allowedWebhookIpRangesCsv: '', extraSettingsJson: '{}' }
  const client = new ApiClient('http://localhost:8080')
  await client.createAdminPaymentProviderAccount('admin-token', payload)
  await client.updateAdminPaymentProviderAccount('admin-token', 'account-1', { ...payload, publicName: 'Stripe cards', secretKey: '', webhookSecret: '', extraSettingsJson: '' })
  await client.setAdminPaymentProviderAccountEnabled('admin-token', 'account-1', false)
  const check = await client.checkAdminPaymentProviderAccount('admin-token', 'account-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/payment-providers/accounts')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/payment-providers/accounts/account-1')
  assert.equal(calls[1]?.init?.method, 'PATCH')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/payment-providers/accounts/account-1/enabled')
  assert.equal(calls[2]?.init?.method, 'POST')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/payment-providers/accounts/account-1/check')
  assert.equal(calls[3]?.init?.method, 'POST')
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(JSON.parse(String(calls[1]?.init?.body)).extraSettingsJson, '')
  assert.equal(check.isReady, true)
})

test('ApiClient admin payments expose refund readiness and send refund payload', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/api/admin/payments')) {
      return new Response(JSON.stringify([{
        id: 'payment-1',
        orderId: 'order-1',
        provider: 'YooKassa',
        status: 'Succeeded',
        amount: 100,
        currency: 'RUB',
        refundedAmount: 25,
        refundSupported: true,
        canRefund: true,
        refundableAmount: 75,
        refundBlockers: []
      }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    return new Response(JSON.stringify({ id: 'refund-1', paymentAttemptId: 'payment-1', provider: 'YooKassa', providerRefundId: 'rf-1', status: 'Succeeded', amount: 50, currency: 'RUB', reason: 'manual', createdAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const payments = await client.getAdminPayments('admin-token')
  const refund = await client.refundAdminPayment('admin-token', 'payment-1', 50, 'manual')

  assert.equal(payments[0]?.canRefund, true)
  assert.equal(payments[0]?.refundableAmount, 75)
  assert.deepEqual(payments[0]?.refundBlockers, [])
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/payments/payment-1/refund')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.match(String(calls[1]?.init?.body), /"amount":50/)
  assert.match(String(calls[1]?.init?.body), /manual/)
  assert.equal(refund.status, 'Succeeded')
})

test('ApiClient admin subscription and VPN access actions are confirmation-friendly POST calls', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ id: 'ok', status: 'Active' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.extendAdminSubscription('admin-token', 'sub-1', 30, 'manual')
  await client.activateAdminSubscription('admin-token', 'sub-1', 'activate')
  await client.blockAdminSubscription('admin-token', 'sub-1', 'abuse')
  await client.unblockAdminSubscription('admin-token', 'sub-1', 'resolved')
  await client.cancelAdminSubscription('admin-token', 'sub-1', 'customer request')
  await client.syncAdminSubscriptionAccess('admin-token', 'sub-1', 'manual subscription sync')
  await client.disableAdminAccess('admin-token', 'access-1', 'expired')
  await client.enableAdminAccess('admin-token', 'access-1', 'paid')
  await client.syncAdminAccess('admin-token', 'access-1', 'manual sync')
  await client.resetAdminAccessTraffic('admin-token', 'access-1', 'reset')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/extend')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/activate')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/block')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/unblock')
  assert.equal(calls[4]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/cancel')
  assert.equal(calls[5]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/sync-access')
  assert.equal(calls[6]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/disable')
  assert.equal(calls[7]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/enable')
  assert.equal(calls[8]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/sync')
  assert.equal(calls[9]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/reset-traffic')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(new Headers(calls[9]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient admin order filters and recheck endpoints use finance-safe routes', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).includes('/api/admin/orders?')) {
      return new Response(JSON.stringify([{ id: 'order-1', status: 'PendingPayment', lastPaymentId: 'payment-1', lastPaymentStatus: 'Pending' }]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ orderId: 'order-1', paymentId: 'payment-1', status: 'Succeeded', rawResponse: '{}' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const orders = await client.getAdminOrders('admin-token', { status: 'PendingPayment', search: 'user@example.test' })
  const orderRecheck = await client.recheckAdminOrderPayment('admin-token', 'order-1')
  const paymentRecheck = await client.recheckAdminPayment('admin-token', 'payment-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/orders?status=PendingPayment&search=user%40example.test')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/orders/order-1/recheck-payment')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/payments/payment-1/recheck')
  assert.equal(calls[2]?.init?.method, 'POST')
  assert.equal(new Headers(calls[2]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(orders[0]?.lastPaymentId, 'payment-1')
  assert.equal(orderRecheck.paymentId, 'payment-1')
  assert.equal(paymentRecheck.status, 'Succeeded')
})

test('ApiClient admin Telegram bot settings masks token at API boundary', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/api/admin/telegram-bot/settings/test')) {
      return new Response(JSON.stringify({ isReady: true, status: 'ready', requiredActions: [], warnings: [], checkedAt: new Date().toISOString() }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ enabled: false, mode: 'LongPolling', publicBotUsername: 'vpn_bot', hasBotToken: true, botTokenMasked: '1234***7890', webhookUrl: '', hasSecretToken: false, adminChatId: '-1001', webAppUrl: 'https://cabinet.example.test', welcomeText: 'Welcome', instructionText: 'Instruction', supportText: 'Support', afterPaymentTextTemplate: 'After', renewalTextTemplate: 'Renewal', paymentFailedTextTemplate: 'Payment failed', subscriptionExpiredTextTemplate: 'Expired', generatedAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const settings = await client.getAdminTelegramBotSettings('admin-token')
  await client.updateAdminTelegramBotSettings('admin-token', { enabled: true, mode: 'Webhook', publicBotUsername: '@managed_bot', botToken: 'new-token', webhookUrl: 'https://api.example.test/api/channels/telegram/webhook', secretToken: 'new-secret', adminChatId: '-1001', webAppUrl: 'https://cabinet.example.test', welcomeText: 'Welcome', renewalTextTemplate: 'Renewal', paymentFailedTextTemplate: 'Payment failed', subscriptionExpiredTextTemplate: 'Expired' })
  const check = await client.testAdminTelegramBotSettings('admin-token')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/telegram-bot/settings')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/telegram-bot/settings')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/telegram-bot/settings/test')
  assert.equal(calls[1]?.init?.method, 'PATCH')
  assert.equal(calls[2]?.init?.method, 'POST')
  assert.equal(settings.botTokenMasked, '1234***7890')
  assert.equal(check.isReady, true)
  assert.equal(settings.webAppUrl, 'https://cabinet.example.test')
  assert.equal(settings.renewalTextTemplate, 'Renewal')
  assert.match(String(calls[1]?.init?.body), /managed_bot/)
  assert.match(String(calls[1]?.init?.body), /botToken/)
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient auth lifecycle endpoints use hashed-token-safe payloads', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/forgot-password')) {
      return new Response(JSON.stringify({ accepted: true, message: 'queued', validationResetToken: null }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/reset-password')) {
      return new Response(JSON.stringify({ status: 'password_changed' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/logout')) {
      return new Response(JSON.stringify({ status: 'ok' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    return new Response(JSON.stringify({ accessToken: 'jwt-2', refreshToken: 'refresh-2', email: 'u@example.com', displayName: 'User' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.refresh('refresh-1')
  await client.logout('jwt-1', 'refresh-2')
  await client.forgotPassword('u@example.com')
  await client.resetPassword('reset-token', 'newPassword123')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/auth/refresh')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/auth/logout')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/auth/forgot-password')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/auth/reset-password')
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer jwt-1')
  assert.match(String(calls[0]?.init?.body), /refreshToken/)
})

test('ApiClient QR SVG endpoints return text with auth headers', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response('<svg data-testid="qr">vless://client</svg>', { status: 200, headers: { 'Content-Type': 'image/svg+xml' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const cabinetQr = await client.getMyAccessQrSvg('user-token', 'access-1')
  const adminQr = await client.getAdminAccessQrSvg('admin-token', 'access-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/cabinet/access/access-1/qr')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/qr')
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer user-token')
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.match(cabinetQr, /<svg/)
  assert.match(adminQr, /vless/)
})

test('ApiClient QR SVG endpoints reject wrong MIME, empty and oversized responses', async () => {
  let oversizedStreamCancelled = false
  let oversizedStreamPulls = 0
  const oversizedStream = new ReadableStream<Uint8Array>({
    pull(controller) {
      oversizedStreamPulls += 1
      controller.enqueue(new Uint8Array(600_000))
    },
    cancel() {
      oversizedStreamCancelled = true
    }
  })
  const responses = [
    new Response('<svg></svg>', { status: 200, headers: { 'Content-Type': 'text/html' } }),
    new Response('   ', { status: 200, headers: { 'Content-Type': 'image/svg+xml; charset=utf-8' } }),
    new Response(oversizedStream, { status: 200, headers: { 'Content-Type': 'image/svg+xml' } })
  ]
  globalThis.fetch = (async () => responses.shift()!) as typeof fetch
  const client = new ApiClient('http://localhost:8080')

  await assert.rejects(
    () => client.getMyAccessQrSvg('user-token', 'wrong-mime'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /неподдерживаемом формате/i.test(error.message)
  )
  await assert.rejects(
    () => client.getMyAccessQrSvg('user-token', 'empty'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /пустой/i.test(error.message)
  )
  await assert.rejects(
    () => client.getAdminAccessQrSvg('admin-token', 'oversized'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /размер/i.test(error.message)
  )
  assert.equal(oversizedStreamCancelled, true)
  assert.ok(oversizedStreamPulls >= 2)
})

test('ApiClient rejects invalid successful JSON responses and accepts structured JSON media types', async () => {
  const responses = [
    new Response('<html>proxy login</html>', { status: 200, headers: { 'Content-Type': 'text/html' } }),
    new Response('', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{"id":', { status: 200, headers: { 'Content-Type': 'application/json; charset=utf-8' } }),
    new Response('null', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json', 'Content-Length': '10000001' } }),
    new Response('provider failure', { status: 503, headers: { 'Content-Type': 'text/plain', 'Content-Length': '64001' } }),
    new Response('[]', { status: 200, headers: { 'Content-Type': 'application/vnd.vpn-platform+json' } })
  ]
  globalThis.fetch = (async () => responses.shift()!) as typeof fetch
  const client = new ApiClient('http://localhost:8080')

  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /неподдерживаемом формате/i.test(error.message)
  )
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /пустой ответ/i.test(error.message)
  )
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /некорректный JSON/i.test(error.message)
  )
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /некорректный JSON/i.test(error.message)
  )
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /неожиданной формы/i.test(error.message)
  )
  await assert.rejects(
    () => client.getAdminDashboardSummary('admin-token'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /неожиданной формы/i.test(error.message)
  )
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /размер/i.test(error.message)
  )
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /размер/i.test(error.message)
  )
  assert.deepEqual(await client.getTariffs(), [])
})

test('ApiClient times out stalled fetches and response bodies with controlled errors', async () => {
  let abortedFetches = 0
  globalThis.fetch = (async (_url: string | URL, init?: RequestInit) => new Promise<Response>((_resolve, reject) => {
    init?.signal?.addEventListener('abort', () => {
      abortedFetches += 1
      reject(new DOMException('Aborted', 'AbortError'))
    }, { once: true })
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080', 15)
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 408 && /не ответил вовремя/i.test(error.message)
  )
  assert.equal(abortedFetches, 1)

  let bodySignal: AbortSignal | null = null
  globalThis.fetch = (async (_url: string | URL, init?: RequestInit) => {
    bodySignal = init?.signal ?? null
    const body = new ReadableStream<Uint8Array>({
      start(controller) {
        init?.signal?.addEventListener('abort', () => controller.error(new DOMException('Aborted', 'AbortError')), { once: true })
      }
    })
    return new Response(body, { status: 200, headers: { 'Content-Type': 'image/svg+xml' } })
  }) as typeof fetch

  await assert.rejects(
    () => client.getAdminAccessQrSvg('admin-token', 'stalled-body'),
    (error: unknown) => error instanceof ApiClientError && error.status === 408 && /повторите запрос/i.test(error.message)
  )
  assert.equal(bodySignal?.aborted, true)
})

test('ApiClient clears the request deadline after a successful response', async () => {
  let requestSignal: AbortSignal | null = null
  globalThis.fetch = (async (_url: string | URL, init?: RequestInit) => {
    requestSignal = init?.signal ?? null
    return new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080', 15)
  assert.deepEqual(await client.getTariffs(), [])
  await new Promise((resolve) => setTimeout(resolve, 30))
  assert.equal(requestSignal?.aborted, false)
})

test('ApiClient app version endpoints are tokenized and mapped', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/latest')) {
      return new Response(JSON.stringify({ currentVersion: '0.2.0', latestRelease: null, seenByCurrentUser: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }
    if (String(url).endsWith('/admin/releases/overview')) {
      return new Response(JSON.stringify({ totalCount: 1, publishedCount: 1, upcomingCount: 0, hiddenCount: 0, agentCount: 0, manualCount: 1, seenCount: 2, latestPublishedReleaseId: 'release-1', latestPublishedVersion: '0.2.0', emptyReleaseIds: [] }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }
    if (String(url).endsWith('/history') || String(url).includes('/admin/releases?') || (String(url).endsWith('/admin/releases') && (init?.method ?? 'GET') === 'GET')) {
      return new Response(JSON.stringify([]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    return new Response(JSON.stringify({ id: 'release-1', releaseId: 'release-1', version: '0.2.0', items: [] }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getLatestAppVersion('user-token')
  await client.getAppVersionHistory('user-token')
  await client.markAppVersionSeen('user-token', 'release-1')
  await client.getAdminAppReleases('admin-token')
  await client.getAdminAppReleases('admin-token', { visibility: 'published', source: 'manual', search: '0.2' })
  const overview = await client.getAdminAppReleaseOverview('admin-token')
  await client.createAdminAppRelease('admin-token', {
    releaseId: 'release-1',
    version: '0.2.0',
    releasedAt: new Date().toISOString(),
    title: 'Что нового',
    summary: 'Описание',
    isActive: true,
    source: 'manual',
    items: [{ type: 'new', text: 'Пункт релиза', sortOrder: 10 }]
  })
  await client.updateAdminAppRelease('admin-token', 'release-guid', {
    releaseId: 'release-1',
    version: '0.2.1',
    releasedAt: new Date().toISOString(),
    title: 'Что нового',
    summary: 'Описание',
    isActive: true,
    source: 'manual',
    items: [{ type: 'fixed', text: 'Исправление', sortOrder: 10 }]
  })
  await client.deleteAdminAppRelease('admin-token', 'release-guid')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/app-version/latest')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/app-version/history')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/app-version/mark-seen')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/app-version/admin/releases')
  assert.equal(calls[4]?.url, 'http://localhost:8080/api/app-version/admin/releases?visibility=published&source=manual&search=0.2')
  assert.equal(calls[5]?.url, 'http://localhost:8080/api/app-version/admin/releases/overview')
  assert.equal(calls[6]?.init?.method, 'POST')
  assert.equal(calls[7]?.init?.method, 'PUT')
  assert.equal(calls[8]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[2]?.init?.headers).get('Authorization'), 'Bearer user-token')
  assert.equal(new Headers(calls[8]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(overview.latestPublishedVersion, '0.2.0')
})

test('frontend sources include app version gate and admin release editor', () => {
  const cabinetSource = readFileSync(new URL('../apps/cabinet/src/AppVersion.tsx', import.meta.url), 'utf8')
  const cabinetAppSource = readFileSync(new URL('../apps/cabinet/src/App.tsx', import.meta.url), 'utf8')
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.match(cabinetSource, /appVersion\.dismissed/)
  assert.match(cabinetSource, /markAppVersionSeen/)
  assert.match(cabinetSource, /getAppVersionHistory/)
  assert.match(cabinetSource, /Показать текущее/)
  assert.match(cabinetAppSource, /AppVersionGate/)
  assert.match(cabinetAppSource, /Что нового/)
  assert.match(adminSource, /id="releases"/)
  assert.match(adminSource, /getAdminAppReleases/)
  assert.match(adminSource, /getAdminAppReleaseOverview/)
  assert.match(adminSource, /Фильтры релизов/)
  assert.match(adminSource, /Запланированные/)
  assert.match(adminSource, /createAdminAppRelease/)
  assert.match(adminSource, /updateAdminAppRelease/)
  assert.match(adminSource, /deleteAdminAppRelease/)
})

test('admin source includes referral program operations', () => {
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.match(adminSource, /id="referrals"/)
  assert.match(adminSource, /getAdminReferralPrograms/)
  assert.match(adminSource, /getAdminReferralRewards/)
  assert.match(adminSource, /createAdminReferralProgram/)
  assert.match(adminSource, /updateAdminReferralProgram/)
  assert.match(adminSource, /Реферальная программа/)
})

test('ApiClient exposes safe notification delivery monitoring and retry', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify(String(url).endsWith('/retry')
      ? { id: 'delivery-1', status: 'Pending' }
      : [{ id: 'delivery-1', maskedToAddress: 'us***@example.test', status: 'Failed' }]), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch
  const client = new ApiClient('http://localhost:8080')

  const deliveries = await client.getAdminNotificationDeliveries('admin-token')
  const retried = await client.retryAdminNotificationDelivery('admin-token', 'delivery-1')

  assert.equal(deliveries[0]?.maskedToAddress, 'us***@example.test')
  assert.equal(retried.status, 'Pending')
  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/notification-deliveries?limit=100')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/notification-deliveries/delivery-1/retry')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('frontend sources include managed FAQ surfaces', () => {
  const publicSource = readFileSync(new URL('../apps/public-web/src/App.tsx', import.meta.url), 'utf8')
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.match(publicSource, /getFaq/)
  assert.match(publicSource, /getHomeFaq/)
  assert.match(publicSource, /faq-toolbar/)
  assert.match(publicSource, /category/)
  assert.match(adminSource, /id="faq"/)
  assert.match(adminSource, /getAdminFaq/)
  assert.match(adminSource, /createAdminFaq/)
  assert.match(adminSource, /updateAdminFaq/)
  assert.match(adminSource, /deleteAdminFaq/)
})


test('admin UI source keeps secret fields write-only and validation mode visible', () => {
  const source = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.match(source, /SecretField|сохраняются скрыто/i)
  assert.match(source, /секрет|Пароль панели/i)
  assert.match(source, /режим проверки|Проверочный режим/i)
  assert.doesNotMatch(source, /panel-password-must-not-leak/i)
  assert.doesNotMatch(source, /ssh-password-must-not-leak/i)
  assert.match(source, /VPN-доступ будет отозван и удален с сервера, а занятый слот освободится/)
  assert.match(source, /Подписка отменена, VPN-доступ отозван и удален с сервера/)
  assert.match(source, /inbound\.usedCapacity < inbound\.capacity/)
  assert.match(source, /Сначала будет занято по одному временному slot панели и target inbound/)
  assert.match(source, /syncStatus\.includes\('uncertain'\)/)
  assert.match(source, /syncStatus\.includes\('compensation-failed'\)/)
  assert.match(source, /Необратимо обнулить счётчики трафика VPN-клиента/)
  assert.match(source, /При сетевой неопределённости доступ получит статус SyncRequired/)
})


test('ApiClient covers sandbox E2E admin, cabinet and checkout endpoints', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    const path = String(url)
    calls.push({ url: path, init })

    if (path.endsWith('/api/admin/dashboard/summary')) {
      return new Response(JSON.stringify({ totalUsers: 1, activeSubscriptions: 1, vpnAccesses: 1, failedPayments: 0 }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.includes('/api/admin/users/user-1/overview')) {
      return new Response(JSON.stringify({ user: { id: 'user-1', email: 'user@example.test', roles: ['User'] }, orders: [{ id: 'order-1' }], payments: [{ id: 'pay-1' }], subscriptions: [{ id: 'sub-1' }], accesses: [{ id: 'access-1' }], supportConversations: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/users?search=user')) {
      return new Response(JSON.stringify([{ id: 'user-1', email: 'user@example.test', passwordHash: undefined }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/orders')) {
      return new Response(JSON.stringify([{ id: 'order-1', paymentProvider: 'YooKassa', linkedSubscriptionId: 'sub-1' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/payments')) {
      return new Response(JSON.stringify([{ id: 'pay-1', provider: 'YooKassa', status: 'Succeeded', providerPaymentId: 'sandbox-pay-1' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/subscriptions')) {
      return new Response(JSON.stringify([{ id: 'sub-1', status: 'Active', currentAccessId: 'access-1' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/access-credentials')) {
      return new Response(JSON.stringify([{ id: 'access-1', status: 'Active', accessUri: 'vless://sandbox/client', latestHistory: [{ eventType: 'AccessCreated' }] }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/provisioning-runs')) {
      return new Response(JSON.stringify([{ id: 'run-1', targetHost: 'vps.example.test', credentialsConfigured: true, executionLog: 'password=***' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/provisioning-runs/run-1')) {
      return new Response(JSON.stringify({ run: { id: 'run-1', targetHost: 'vps.example.test', credentialsConfigured: true, executionLog: 'credential=***', linkedAccessId: 'access-1' }, steps: [{ stepName: 'Validate input', output: 'secret=***' }] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/payment-providers/accounts')) {
      return new Response(JSON.stringify([{ id: 'ppa-1', provider: 'YooKassa', mode: 'Sandbox', webhookUrl: 'https://api.example.test/webhooks/payments/yookassa', isCheckoutConfigured: true, extraSettingsJson: '{"apiSecret":"***"}' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/telegram-bot/settings')) {
      return new Response(JSON.stringify({ enabled: false, mode: 'LongPolling', publicBotUsername: 'vpn_bot', hasBotToken: true, botTokenMasked: '1234***7890', webhookUrl: '', hasSecretToken: true, adminChatId: '', webAppUrl: 'http://localhost:5174', welcomeText: 'Welcome', instructionText: '', supportText: '', afterPaymentTextTemplate: '', renewalTextTemplate: '', paymentFailedTextTemplate: '', subscriptionExpiredTextTemplate: '', generatedAt: new Date().toISOString() }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/public/payments/providers')) {
      return new Response(JSON.stringify([{ provider: 'YooKassa', publicName: 'YooKassa Sandbox', mode: 'Sandbox' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/public/tariffs')) {
      return new Response(JSON.stringify([{ id: 'tariff-1', name: 'Monthly', isActive: true }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/me/accesses')) {
      return new Response(JSON.stringify([{ id: 'access-1', accessUri: 'vless://sandbox/client', status: 'Active' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/cabinet/access/access-1/qr')) {
      return new Response('<svg>vless://sandbox/client</svg>', { status: 200, headers: { 'Content-Type': 'image/svg+xml' } })
    }

    throw new Error(`Unexpected URL ${path}`)
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getAdminDashboardSummary('admin-token')
  await client.getAdminUsers('admin-token', { search: 'user' })
  await client.getAdminUserOverview('admin-token', 'user-1')
  await client.getAdminOrders('admin-token')
  await client.getAdminPayments('admin-token')
  await client.getAdminSubscriptions('admin-token')
  await client.getAdminAccesses('admin-token')
  await client.getAdminProvisioningRuns('admin-token')
  const runDetails = await client.getAdminProvisioningRun('admin-token', 'run-1')
  await client.getAdminPaymentProviderAccounts('admin-token')
  await client.getAdminTelegramBotSettings('admin-token')
  await client.getPublicPaymentProviders()
  await client.getTariffs()
  await client.getMyAccesses('user-token')
  const qr = await client.getMyAccessQrSvg('user-token', 'access-1')

  assert.equal(calls.length, 15)
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(new Headers(calls[13]?.init?.headers).get('Authorization'), 'Bearer user-token')
  assert.equal(runDetails.run.credentialsConfigured, true)
  assert.equal(runDetails.steps[0]?.output, 'secret=***')
  assert.match(qr, /<svg>/)
  assert.doesNotMatch(JSON.stringify(runDetails), /raw-password|PRIVATE KEY|bot-token|webhook-secret/i)
})

test('frontend sources keep sandbox E2E surfaces safe and user-friendly', () => {
  const publicSource = readFileSync(new URL('../apps/public-web/src/App.tsx', import.meta.url), 'utf8')
  const cabinetSource = readFileSync(new URL('../apps/cabinet/src/App.tsx', import.meta.url), 'utf8')
  const appVersionSource = readFileSync(new URL('../apps/cabinet/src/AppVersion.tsx', import.meta.url), 'utf8')
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')
  const adminStylesSource = readFileSync(new URL('../apps/admin-panel/src/styles.css', import.meta.url), 'utf8')
  const publicStylesSource = readFileSync(new URL('../apps/public-web/src/styles.css', import.meta.url), 'utf8')
  const cabinetStylesSource = readFileSync(new URL('../apps/cabinet/src/styles.css', import.meta.url), 'utf8')
  const uiSource = readFileSync(new URL('../packages/ui/src/index.tsx', import.meta.url), 'utf8')
  const apiClientSource = readFileSync(new URL('../packages/api-client/src/index.ts', import.meta.url), 'utf8')
  const stylesSource = readFileSync(new URL('../packages/ui/src/styles.css', import.meta.url), 'utf8')

  assert.match(publicSource, /getPublicPaymentProviders/)
  assert.match(publicSource, /paymentProvidersLoading/)
  assert.match(publicSource, /Нет доступных способов оплаты|paymentProvidersLoading/)
  assert.match(publicSource, /getHomeContent/)
  assert.match(publicSource, /defaultHomeContent/)
  assert.match(publicSource, /tariffFeatures/)
  assert.match(publicSource, /afterPaymentText/)
  assert.match(publicSource, /feature-list compact-list/)
  assert.match(cabinetSource, /getPublicPaymentProviders/)
  assert.match(cabinetSource, /paymentProvidersLoading/)
  assert.match(cabinetSource, /getMyAccessQrSvg/)
  assert.match(cabinetSource + adminSource, /QrCodePreview/)
  assert.doesNotMatch(cabinetSource + adminSource, /dangerouslySetInnerHTML/)
  assert.match(uiSource, /getSafeSvgImageDataUrl/)
  assert.match(apiClientSource, /expectedContentType: 'image\/svg\+xml'/)
  assert.match(cabinetSource, /VITE_PUBLIC_WEB_URL/)
  assert.match(publicSource + cabinetSource + adminSource, /readSessionStorageItem/)
  assert.doesNotMatch(publicSource + cabinetSource + adminSource, /useState\(sessionStorage/)
  assert.match(cabinetSource, /'5474': '5473'/)
  assert.match(cabinetSource, /aria-current="page"/)
  assert.match(cabinetSource, /aria-pressed=\{selectedSupportConversation\?\.id === conversation\.id\}/)
  assert.match(cabinetSource, /aria-label=\{`\$\{conversation\.subject/)
  assert.match(appVersionSource, /useRef/)
  assert.match(appVersionSource, /dialogRef\.current\?\.focus\(\)/)
  assert.match(appVersionSource, /previousActiveElement\?\.focus\(\)/)
  assert.match(appVersionSource, /event\.key !== 'Escape'/)
  assert.match(appVersionSource, /aria-describedby="app-version-summary"/)
  assert.match(appVersionSource, /aria-current=\{release\.releaseId === selectedRelease\.releaseId/)
  assert.match(appVersionSource, /aria-expanded=\{historyOpen\}/)
  assert.match(cabinetSource, /Доступы не выдавались|Ключ ещё не готов/)
  assert.doesNotMatch(cabinetSource, /Перевыпуск ключа скоро/)
  assert.match(adminSource, /getAdminDashboardSummary/)
  assert.match(adminSource, /<strong>\{adminSections\.length\}<\/strong> разделов/)
  assert.match(adminSource, /getAdminFaqOverview/)
  assert.match(adminSource, /Фильтры FAQ/)
  assert.match(adminSource, /Дубли вопросов в категориях/)
  assert.match(adminSource, /getAdminSiteContent/)
  assert.match(adminSource, /getAdminHomeContentReadiness/)
  assert.match(adminSource, /restoreAdminHomeContentDefaults/)
  assert.match(adminSource, /Готовность главной страницы/)
  assert.match(adminSource, /id="content"/)
  assert.match(adminSource, /Контент сайта/)
  assert.match(adminSource, /getAdminWorkScenarios/)
  assert.match(adminSource, /id="scenarios"/)
  assert.match(adminSource, /Сценарии/)
  assert.match(adminSource, /provisioningScenario/)
  assert.match(adminSource, /scenario-tariff-picker/)
  assert.match(adminSource, /updateWorkScenarioTariffLink/)
  assert.match(stylesSource, /body\s*\{[^}]*min-width:\s*0;/s)
  assert.match(publicStylesSource, /\.device-card-tertiary\s*\{\s*left:\s*8%;\s*\}/)
  assert.match(adminSource, /Тарифы, которым разрешен сценарий/)
  assert.doesNotMatch(adminSource, /Связанные тарифы JSON/)
  assert.match(adminSource, /getAdminUserOverview/)
  assert.match(adminSource, /getAdminAccessQrSvg/)
  assert.match(adminSource, /credentialsConfigured/i)
  assert.match(adminSource, /ConfirmButton/)
  assert.match(adminSource, /adminAuthRequiredMessage/)
  assert.match(adminSource, /Войдите как администратор/)
  assert.match(adminSource, /ADMIN_EMAIL_STORAGE_KEY/)
  assert.match(adminSource, /validateAdminLogin/)
  assert.match(adminSource, /rememberAdminEmail/)
  assert.match(adminSource, /admin-login-session/)
  assert.match(adminSource, /admin-login-checklist/)
  assert.match(adminSource, /role="alert"/)
  assert.match(adminSource, /autoComplete="username"/)
  assert.match(adminSource, /Пароль не сохраняется/)
  assert.doesNotMatch(adminSource, /writeSessionStorageItem\([^)]*password/i)
  assert.match(adminSource, /clearAdminSession/)
  assert.match(adminSource, /Завершить сессию/)
  assert.match(adminSource, /fieldset className="form-section"/)
  assert.match(adminSource, /editServer/)
  assert.match(adminSource, /handleSaveServer/)
  assert.match(adminSource, /Редактировать VPN-сервер/)
  assert.match(adminSource, /Дата-центр/)
  assert.match(adminSource, /Приоритет/)
  assert.match(adminSource, /editVpnPanel/)
  assert.match(adminSource, /handleSaveVpnPanel/)
  assert.match(adminSource, /Редактировать 3x-ui панель/)
  assert.match(adminSource, /Проверка SSL/)
  assert.match(adminSource, /Вариант API/)
  assert.match(adminSource, /Идентификация сервера|Подключение и безопасность|Тексты сценариев/)
  assert.match(adminSource, /editTariff/)
  assert.match(adminSource, /handleDeleteTariff/)
  assert.match(adminSource, /featuresTextToJson/)
  assert.match(adminSource, /validateTariffForm/)
  assert.match(adminSource, /validatePaymentProviderForm/)
  assert.match(adminSource, /validateServerForm/)
  assert.match(adminSource, /validateVpnPanelForm/)
  assert.match(adminSource, /validateInboundForm/)
  assert.match(adminSource, /providerFormErrors/)
  assert.match(adminSource, /tariffFormErrors/)
  assert.match(adminSource, /serverFormErrors/)
  assert.match(adminSource, /vpnPanelFormErrors/)
  assert.match(adminSource, /inboundFormErrors/)
  assert.match(adminSource, /workScenarioFormErrors/)
  assert.match(adminSource, /FormValidationSummary/)
  assert.match(adminSource, /providerFormErrors\.length > 0/)
  assert.match(adminSource, /tariffFormErrors\.length > 0/)
  assert.match(adminSource, /serverFormErrors\.length > 0/)
  assert.match(adminSource, /vpnPanelFormErrors\.length > 0/)
  assert.match(adminSource, /inboundFormErrors\.length > 0/)
  assert.match(adminSource, /workScenarioFormErrors\.length > 0/)
  assert.match(adminSource, /Валюта должна быть кодом из 3 латинских букв/)
  assert.match(adminSource, /архивирован и скрыт с витрины/)
  assert.match(adminSource, /Предпросмотр|tariff-preview/)
  assert.match(stylesSource, /form:has\(\.form-section\)/)
  assert.match(stylesSource, /form\.form-grid/)
  assert.match(stylesSource, /label:has\(textarea\)/)
  assert.match(adminStylesSource, /scenario-tariff-picker/)
  assert.match(adminStylesSource, /checkbox-list/)
  assert.match(adminStylesSource, /provider-extra-settings/)
  assert.match(stylesSource, /password-field-row/)
  assert.match(stylesSource, /\.form-field/)
  assert.match(publicSource + cabinetSource + adminSource, /PasswordField/)
  assert.match(adminSource, /aria-current/)
  assert.match(adminSource, /hidden=\{activeSection !==/)
  assert.match(adminSource, /adminSectionGroups/)
  assert.match(adminSource, /adminSectionDescriptions/)
  assert.match(adminSource, /goToAdminSection/)
  assert.match(adminSource, /handleAdminSectionKeyDown/)
  assert.match(adminSource, /className="admin-section-tabs"/)
  assert.match(adminSource, /role="tablist"/)
  assert.match(adminSource, /role="tab"/)
  assert.match(adminSource, /role="tabpanel"/)
  assert.match(adminSource, /aria-controls=\{id\}/)
  assert.match(adminSource, /aria-labelledby=\{adminSectionTabId/)
  assert.match(adminSource, /admin-section-select/)
  assert.match(adminSource, /Предыдущий/)
  assert.match(adminSource, /Следующий/)
  assert.match(adminSource, /\['users', 'Пользователи'\],\s*\['support', 'Поддержка'\],\s*\['audit', 'Аудит'\],\s*\['payments', 'Оплаты'\]/)
  assert.match(adminSource, /\['panels', '3x-ui панели'\],\s*\['provisioning', 'Подготовка VPS'\],\s*\['bot', 'Telegram-бот'\]/)
  assert.match(publicSource + cabinetSource, /role="tabpanel"/)
  assert.match(publicSource + cabinetSource, /panelId=\{authPanelId\}/)
  assert.match(uiSource, /aria-controls=\{panelId\}/)
  assert.match(publicSource + cabinetSource, /SegmentedTabs/)
  assert.doesNotMatch(publicSource + cabinetSource, /handleAuthTabsKeyDown/)
  assert.match(uiSource, /designTokens/)
  assert.match(uiSource, /function SegmentedTabs/)
  assert.match(uiSource, /function StateBlock/)
  assert.match(uiSource, /function DataTableLite/)
  assert.match(stylesSource, /--radius-md/)
  assert.match(stylesSource, /state-block-warning/)
  assert.match(stylesSource, /data-table-lite/)
  assert.match(publicSource, /<Link className="app-brand" to="\/"/)
  assert.match(publicSource + cabinetSource, /ariaLabel="Открыть оплату в новой вкладке"/)
  assert.match(publicSource + cabinetSource, /ExternalLinkActions/)
  assert.doesNotMatch(publicSource + cabinetSource, /href=\{[^}]*(?:redirectUrl|confirmationUrl|deepLinkUrl)/)
  assert.match(cabinetSource, /ariaLabel="Открыть Telegram-бота в новой вкладке"/)
  assert.match(publicSource + cabinetSource + adminSource, /SkipLink/)
  assert.match(adminSource, /id="admin-content"/)
  assert.match(adminSource, /<nav className="admin-sidebar"/)
  assert.doesNotMatch(adminSource + uiSource, /window\.confirm|window\.prompt/)
  assert.doesNotMatch(publicSource + cabinetSource, /<(?:Link|a)\b[^>]*>\s*<PrimaryButton/s)
  assert.match(adminSource, /сохраняются скрыто|SecretField/i)
  assert.match(adminSource, /editProviderAccount/)
  assert.match(adminSource, /handleSaveProviderAccount/)
  assert.match(adminSource, /handleCheckProviderAccount/)
  assert.match(adminSource, /editingProviderAccount/)
  assert.match(adminSource, /configured=\{editingProviderAccount\?\.hasSecretKey\}/)
  assert.match(adminSource, /configured=\{editingProviderAccount\?\.hasWebhookSecret\}/)
  assert.match(adminSource, /paymentProviderSetup/)
  assert.match(adminSource, /Ключ терминала \(TerminalKey\)/)
  assert.match(adminSource, /Секрет endpoint webhook/)
  assert.match(adminSource, /Только Telegram/)
  assert.match(adminSource, /hostedCheckoutUrl/)
  assert.match(publicSource + cabinetSource, /готовые web-провайдеры/)
  assert.match(adminSource, /updateAdminPaymentProviderAccount/)
  assert.match(adminSource, /checkAdminPaymentProviderAccount/)
  assert.match(adminSource, /providerCheckResultClass/)
  assert.match(adminSource, /provider-check-result/)
  assert.match(adminSource, /Проверить настройки/)
  assert.match(adminSource, /configurationStatus/)
  assert.match(adminSource, /role="status"/)
  assert.match(adminSource, /URL webhook/)
  assert.match(adminSource, /provider-extra-settings/)
  assert.match(adminStylesSource, /\.provider-check-result/)
  assert.match(adminStylesSource, /\.provider-check-result-ok/)
  assert.match(adminStylesSource, /\.provider-check-result-problem/)
  assert.match(adminSource, /extraSettingsFields/)
  assert.match(adminSource, /URL страницы оплаты \(hosted checkout\)/)
  assert.match(adminSource, /Алгоритм подписи/)
  assert.doesNotMatch(adminSource, /Extra settings JSON/)
  assert.match(adminSource, /пустые поля секретов и дополнительных параметров сохраняют текущие значения/)
  assert.match(adminSource, /botTokenMasked|hasBotToken/)
  assert.match(adminSource, /Username публичного бота/)
  assert.match(adminSource, /Токен бота/)
  assert.match(adminSource, /Секрет webhook/)
  assert.match(adminSource, /URL WebApp/)
  assert.match(adminSource, /testAdminTelegramBotSettings/)
  assert.match(adminSource, /botSettingsCheck/)
  assert.match(adminSource, /Шаблон продления/)
  assert.match(adminSource, /Шаблон ошибки оплаты/)
  assert.match(adminSource, /Шаблон окончания подписки/)
  assert.match(publicSource, /home\.seo\.title/)
  assert.match(publicSource, /home\.seo\.description/)
  assert.match(publicSource, /home\.features\.item1/)
  assert.match(publicSource, /home\.testimonials\.item1\.text/)
  assert.match(publicSource, /home\.footer\.text/)
  assert.match(publicSource, /home\.errors\.checkoutCreate/)
  assert.match(publicSource, /home\.checkout\.afterPaymentText/)
  assert.match(publicSource, /home\.checkout\.providersEmptyDescription/)
  assert.match(adminSource, /panelPasswordConfigured|Пароль панели/i)
  assert.doesNotMatch(publicSource + cabinetSource + adminSource, /sk_live_|ghp_|BEGIN PRIVATE KEY/i)
  assert.doesNotMatch(apiClientSource, /errorMessage: 'Failed to /)
  assert.doesNotMatch(apiClientSource, /errorMessage: '(Registration|Login|Refresh|Logout|Password reset).*failed'/)
  assert.match(apiClientSource, /apiFallbackErrorMessage = 'Не удалось выполнить запрос\. Попробуйте еще раз\.'/)
  assert.match(uiSource, /agent: 'Агент'/)
  assert.match(uiSource, /manual: 'Вручную'/)
  assert.match(adminSource, /releaseSourceLabel/)
  assert.match(adminSource, /provisioningModeLabel/)
  assert.match(adminSource, /provisioningDeployModeLabel/)
  assert.match(adminSource, /provisioningRiskBadge/)
  assert.match(adminSource, /serverProvisioningCanDeploy/)
  assert.match(adminSource, /disabled=\{server\.status === 'Archived'\}/)
  assert.match(adminSource, /server\.status === 'Disabled' \|\| server\.status === 'Archived'/)
  assert.match(adminSource, /live deploy/)
  assert.match(adminSource, /deployMode === 'live-deploy-blocked'/)
  assert.match(adminSource, /Опрос Telegram/)
  for (const source of [publicSource, cabinetSource, appVersionSource, adminSource, uiSource, apiClientSource]) {
    assert.doesNotMatch(source, /\uFFFD/)
    assert.doesNotMatch(source, /\?{3,}/)
  }
  const responsiveStylesSource = [
    stylesSource,
    adminStylesSource,
    publicStylesSource,
    cabinetStylesSource
  ].join('\n')
  for (const breakpoint of ['1280px', '1024px', '768px', '390px']) {
    assert.match(responsiveStylesSource, new RegExp(`max-width:\\s*${breakpoint}`))
  }

  assert.match(stylesSource, /--page-x/)
  assert.match(stylesSource, /\.sr-only/)
  assert.match(stylesSource, /prefers-reduced-motion: reduce/)
  assert.match(stylesSource, /\.button:focus-visible/)
  assert.match(stylesSource, /minmax\(min\(220px, 100%\), 1fr\)/)
  assert.match(stylesSource, /minmax\(min\(180px, 100%\), 1fr\)/)
  assert.match(stylesSource, /\.data-table-lite[\s\S]*min-width: 480px/)
  assert.match(adminStylesSource, /admin-section-tabs[\s\S]*max-height: 320px/)
  assert.match(adminStylesSource, /@media \(max-width: 640px\)[\s\S]*?\.admin-section-tabs \{\s*display: none;/)
  assert.match(adminStylesSource, /provider-setup-note[\s\S]*grid-template-columns: 1fr/)
  assert.match(publicStylesSource, /landing-hero-visual[\s\S]*width: min\(36vw, 360px\)/)
  assert.match(publicStylesSource, /landing-stats[\s\S]*grid-template-columns: 1fr/)
  assert.match(cabinetStylesSource, /app-version-modal[\s\S]*grid-template-columns: 280px/)
  assert.match(cabinetStylesSource, /support-ticket[\s\S]*flex-direction: column/)
})


test('Stage 9 UI source includes MVP polish surfaces and safety affordances', () => {
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')
  const cabinetSource = readFileSync(new URL('../apps/cabinet/src/App.tsx', import.meta.url), 'utf8')
  const publicSource = readFileSync(new URL('../apps/public-web/src/App.tsx', import.meta.url), 'utf8')
  const uiSource = readFileSync(new URL('../packages/ui/src/index.tsx', import.meta.url), 'utf8')
  const telegramSource = readFileSync(new URL('../../backend/src/VpnPlatform.Application/Services/TelegramBotService.cs', import.meta.url), 'utf8')

  assert.match(adminSource, /admin-sidebar/)
  assert.match(adminSource, /Последние заказы/)
  assert.match(adminSource, /Требует внимания/)
  assert.match(adminSource, /EmptyState/)
  assert.match(adminSource, /SecretField/)
  assert.match(adminSource, /Внешние Telegram, оплаты, 3x-ui и VPS отключены/)
  assert.match(adminSource, /ConfirmButton/)
  assert.doesNotMatch(adminSource + uiSource, /window\.confirm|window\.prompt/)
  assert.match(adminSource, /CopyButton/)

  assert.match(cabinetSource, /Мой VPN-доступ/)
  assert.match(cabinetSource, /VPN-ключи/)
  assert.match(cabinetSource, /CopyButton/)
  assert.match(cabinetSource, /Подписок пока нет/)
  assert.match(cabinetSource, /getMyAccessQrSvg/)
  assert.match(cabinetSource + adminSource, /QrCodePreview/)
  assert.doesNotMatch(cabinetSource + adminSource, /dangerouslySetInnerHTML/)

  assert.match(publicSource, /Показываем только активные тарифы/)
  assert.match(publicSource, /Нет доступных способов оплаты/)
  assert.match(publicSource, /Сброс пароля/)
  assert.match(publicSource, /Покупка сохранена|Привязываем покупку/)
  assert.match(publicSource, /Повторить привязку|Отменить эту покупку/)
  assert.match(publicSource, /onClearPendingCheckout/)
  assert.match(publicSource, /checkoutUnavailableReason|Оплата временно недоступна/)
  assert.match(publicSource, /ExternalLinkActions/)
  assert.match(publicSource, /tariffsLoading/)
  assert.match(publicSource, /ErrorBlock/)

  assert.match(telegramSource, /Главное меню VPN Platform/)
  assert.match(telegramSource, /Не публикуйте свой ключ|Не пересылайте ключ/)
  assert.match(telegramSource, /Секреты и платежные данные бот не запрашивает/)
  assert.match(telegramSource, /Пароль\/ключ не будет показан повторно/)
  assert.doesNotMatch(telegramSource, /my-secret-password|BEGIN PRIVATE KEY|bot-token-must-not-leak/i)
})
