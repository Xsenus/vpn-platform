import test from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import {
  ApiClient,
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
  assert.equal(
    translateAuthMessage('If the account exists, a password reset instruction has been queued for the configured delivery channel.'),
    'Если аккаунт существует, инструкция по сбросу пароля поставлена в очередь отправки.'
  )
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
    if (String(url).includes('/api/public/content/faq') || String(url).endsWith('/api/admin/faq')) {
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
  await client.createAdminFaq('admin-token', { question: 'Как?', answer: 'Так', category: 'Общее', isActive: true, showOnHome: true, showOnFaqPage: true, sortOrder: 10 })
  await client.updateAdminFaq('admin-token', 'faq-1', { question: 'Как?', answer: 'Так', category: 'Общее', isActive: true, showOnHome: false, showOnFaqPage: true, sortOrder: 20 })
  await client.deleteAdminFaq('admin-token', 'faq-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/public/content/faq')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/public/content/faq?home=true')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/faq')
  assert.equal(calls[3]?.init?.method, 'POST')
  assert.equal(calls[4]?.init?.method, 'PUT')
  assert.equal(calls[5]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[5]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient site content endpoints cover public and admin CRUD', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).includes('/api/public/content/home') || String(url).includes('/api/admin/site-content')) {
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
  await client.createAdminSiteContent('admin-token', { key: 'home.hero.title', value: 'VPN title', group: 'home', label: 'Hero title', inputType: 'text', isActive: true, sortOrder: 10 })
  await client.updateAdminSiteContent('admin-token', 'content-1', { key: 'home.hero.title', value: 'New title', group: 'home', label: 'Hero title', inputType: 'text', isActive: true, sortOrder: 10 })
  await client.deleteAdminSiteContent('admin-token', 'content-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/public/content/home')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/site-content?group=home')
  assert.equal(calls[2]?.init?.method, 'POST')
  assert.equal(calls[3]?.init?.method, 'PUT')
  assert.equal(calls[4]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[4]?.init?.headers).get('Authorization'), 'Bearer admin-token')
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
    channel: 'Web',
    paymentProvider: 'YooKassa',
    promoCode: 'WELCOME10',
    isFirstPurchase: false,
    subscriptionId: 'subscription-1'
  })

  const headers = new Headers(calls[0]?.init?.headers)
  assert.equal(calls[0]?.url, 'http://localhost:8080/api/me/orders')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(headers.get('Authorization'), 'Bearer token-123')
  assert.match(String(calls[0]?.init?.body), /WELCOME10/)
  assert.match(String(calls[0]?.init?.body), /subscription-1/)
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
  const conversation = { id: 'support-1', userId: 'user-1', channel: 'web', status: 'open', subject: 'Оплата', internalNote: 'Связано: заказ order-1.', createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
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
      return new Response(JSON.stringify({ conversationId: 'support-1', status: 'closed' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    throw new Error(`Unexpected URL ${path}`)
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getMySupportConversations('user-token')
  await client.createMySupportConversation('user-token', { subject: 'Оплата', text: 'Нужна помощь', orderId: 'order-1', subscriptionId: null })
  await client.getMySupportMessages('user-token', 'support-1')
  await client.replyMySupportConversation('user-token', 'support-1', 'Спасибо')
  await client.updateMySupportConversationStatus('user-token', 'support-1', 'closed')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/me/support/conversations')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.match(String(calls[1]?.init?.body), /order-1/)
  assert.equal(calls[3]?.init?.method, 'POST')
  assert.equal(calls[4]?.init?.method, 'PATCH')
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
      return new Response(JSON.stringify({ id: 'node-1', deleted: true, archived: false, linkedSubscriptions: 0, linkedAccesses: 0, linkedProvisioningRuns: 0 }), {
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
  await client.deleteAdminServer('admin-token', 'node-1')

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
})

test('ApiClient provisioning run details and actions are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/deploy')) {
      return new Response(JSON.stringify({ runId: 'run-1', status: 'DeployQueued', dryRun: false }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/cancel')) {
      return new Response(JSON.stringify({ runId: 'run-1', status: 'cancelled' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/support-needed')) {
      return new Response(JSON.stringify({ runId: 'run-1', supportConversationId: 'support-1' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    return new Response(JSON.stringify({
      run: { id: 'run-1', nodeId: 'node-1', nodeName: 'customer-vps', targetHost: 'vps.example.com', sshPort: 22, username: 'root', authMethod: 'ssh_key', credentialsConfigured: true, status: 'ReadyToDeploy', currentStep: 'ready_to_deploy', dryRun: true, startedAt: new Date().toISOString(), executionLog: 'password=***', linkedAccessId: null, createdAt: new Date().toISOString() },
      steps: [{ id: 'step-1', provisioningRunId: 'run-1', stepName: 'Validate input', status: 'Succeeded', output: 'credentials=***', errorText: '', createdAt: new Date().toISOString() }]
    }), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const details = await client.getAdminProvisioningRun('admin-token', 'run-1')
  await client.deployAdminProvisioningRun('admin-token', 'run-1')
  await client.cancelAdminProvisioningRun('admin-token', 'run-1')
  await client.markAdminProvisioningSupportNeeded('admin-token', 'run-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/provisioning-runs/run-1')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/provisioning-runs/run-1/deploy')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/provisioning-runs/run-1/cancel')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/provisioning-runs/run-1/support-needed')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.equal(new Headers(calls[3]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(details.run.credentialsConfigured, true)
  assert.equal(details.steps[0]?.output, 'credentials=***')
})

test('ApiClient.queueAdminProvision calls provisioning endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ serverId: 'node-1', runId: 'run-1', status: 'queued', dryRun: false }), {
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
    channel: 'Web',
    paymentProvider: 'YooKassa',
    promoCode: null,
    isFirstPurchase: false,
    emailHint: null,
    returnUrl: 'http://localhost:5173/account'
  })

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/public/checkout-sessions')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.match(String(calls[0]?.init?.body), /YooKassa/)
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
  await client.replyAdminSupportConversation('admin-token', 'conv-1', 'reply')
  await client.updateAdminSupportConversationStatus('admin-token', 'conv-1', 'pending')
  await client.addAdminSupportInternalNote('admin-token', 'conv-1', 'note')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/support/conversations/conv-1/reply')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/support/conversations/conv-1/status')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/support/conversations/conv-1/notes')
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
    if (String(url).includes('/sync')) {
      return new Response(JSON.stringify({ id: 'sync-1', vpnPanelId: 'panel-1', status: 'Succeeded', startedAt: new Date().toISOString(), summaryJson: '{}', errorMessage: '' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).includes('/inbounds')) {
      return new Response(JSON.stringify(String(init?.method ?? 'GET') === 'POST' ? { id: 'inbound-1', vpnPanelId: 'panel-1', externalInboundId: '1', name: 'default-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{}', streamSettingsJson: '{}', sniffingJson: '{}', isDefault: true, isActive: true, capacity: 5000, usedCapacity: 0 } : []), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).includes('/clients')) {
      return new Response(JSON.stringify([]), { status: 200, headers: { 'Content-Type': 'application/json' } })
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
  await client.createAdminVpnPanelInbound('admin-token', 'panel-1', { name: 'default-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{}', streamSettingsJson: '{}', sniffingJson: '{}', isDefault: true, capacity: 5000 })
  await client.getAdminVpnPanelClients('admin-token', 'panel-1')
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
  assert.equal(calls[7]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/clients')
  assert.equal(calls[8]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/health-checks')
  assert.equal(calls[9]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1')
  assert.equal(calls[9]?.init?.method, 'DELETE')
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
    return new Response(JSON.stringify(String(url).endsWith('/check') ? { accountId: 'account-1', provider: 'Stripe', mode: 'Sandbox', isReady: true, healthStatus: 'Healthy', message: 'Payment provider account check passed.', details: ['Checkout configuration is ready.'], checkedAt: new Date().toISOString(), account: body } : body), {
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

test('ApiClient admin subscription and VPN access actions are confirmation-friendly POST calls', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ id: 'ok', status: 'Active' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.extendAdminSubscription('admin-token', 'sub-1', 30, 'manual')
  await client.blockAdminSubscription('admin-token', 'sub-1', 'abuse')
  await client.unblockAdminSubscription('admin-token', 'sub-1', 'resolved')
  await client.cancelAdminSubscription('admin-token', 'sub-1', 'customer request')
  await client.disableAdminAccess('admin-token', 'access-1', 'expired')
  await client.enableAdminAccess('admin-token', 'access-1', 'paid')
  await client.syncAdminAccess('admin-token', 'access-1', 'manual sync')
  await client.resetAdminAccessTraffic('admin-token', 'access-1', 'reset')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/extend')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/block')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/unblock')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/cancel')
  assert.equal(calls[4]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/disable')
  assert.equal(calls[5]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/enable')
  assert.equal(calls[6]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/sync')
  assert.equal(calls[7]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/reset-traffic')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(new Headers(calls[7]?.init?.headers).get('Authorization'), 'Bearer admin-token')
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

test('ApiClient app version endpoints are tokenized and mapped', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/latest')) {
      return new Response(JSON.stringify({ currentVersion: '0.2.0', release: null, seen: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }
    if (String(url).endsWith('/history') || String(url).endsWith('/admin/releases')) {
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
  assert.equal(calls[4]?.init?.method, 'POST')
  assert.equal(calls[5]?.init?.method, 'PUT')
  assert.equal(calls[6]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[2]?.init?.headers).get('Authorization'), 'Bearer user-token')
  assert.equal(new Headers(calls[6]?.init?.headers).get('Authorization'), 'Bearer admin-token')
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
  assert.match(adminSource, /createAdminAppRelease/)
  assert.match(adminSource, /updateAdminAppRelease/)
  assert.match(adminSource, /deleteAdminAppRelease/)
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
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')
  const adminStylesSource = readFileSync(new URL('../apps/admin-panel/src/styles.css', import.meta.url), 'utf8')
  const uiSource = readFileSync(new URL('../packages/ui/src/index.tsx', import.meta.url), 'utf8')
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
  assert.match(cabinetSource, /VITE_PUBLIC_WEB_URL/)
  assert.match(publicSource + cabinetSource + adminSource, /readSessionStorageItem/)
  assert.doesNotMatch(publicSource + cabinetSource + adminSource, /useState\(sessionStorage/)
  assert.match(cabinetSource, /'5474': '5473'/)
  assert.match(cabinetSource, /aria-current="page"/)
  assert.match(cabinetSource, /Доступы не выдавались|Ключ ещё не готов/)
  assert.doesNotMatch(cabinetSource, /Перевыпуск ключа скоро/)
  assert.match(adminSource, /getAdminDashboardSummary/)
  assert.match(adminSource, /getAdminSiteContent/)
  assert.match(adminSource, /id="content"/)
  assert.match(adminSource, /Контент сайта/)
  assert.match(adminSource, /getAdminWorkScenarios/)
  assert.match(adminSource, /id="scenarios"/)
  assert.match(adminSource, /Сценарии/)
  assert.match(adminSource, /provisioningScenario/)
  assert.match(adminSource, /scenario-tariff-picker/)
  assert.match(adminSource, /updateWorkScenarioTariffLink/)
  assert.match(adminSource, /Тарифы, которым разрешен сценарий/)
  assert.doesNotMatch(adminSource, /Связанные тарифы JSON/)
  assert.match(adminSource, /getAdminUserOverview/)
  assert.match(adminSource, /getAdminAccessQrSvg/)
  assert.match(adminSource, /credentialsConfigured/i)
  assert.match(adminSource, /ConfirmButton/)
  assert.match(adminSource, /adminAuthRequiredMessage/)
  assert.match(adminSource, /Войдите как администратор/)
  assert.match(adminSource, /clearAdminSession/)
  assert.match(adminSource, /Завершить сессию/)
  assert.match(adminSource, /fieldset className="form-section"/)
  assert.match(adminSource, /editServer/)
  assert.match(adminSource, /handleSaveServer/)
  assert.match(adminSource, /Редактировать VPN-сервер/)
  assert.match(adminSource, /Datacenter/)
  assert.match(adminSource, /Приоритет/)
  assert.match(adminSource, /editVpnPanel/)
  assert.match(adminSource, /handleSaveVpnPanel/)
  assert.match(adminSource, /Редактировать 3x-ui панель/)
  assert.match(adminSource, /SSL verification/)
  assert.match(adminSource, /API variant/)
  assert.match(adminSource, /Идентификация сервера|Подключение и безопасность|Тексты сценариев/)
  assert.match(adminSource, /editTariff/)
  assert.match(adminSource, /handleDeleteTariff/)
  assert.match(adminSource, /featuresTextToJson/)
  assert.match(adminSource, /validateTariffForm/)
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
  assert.match(publicSource + cabinetSource, /role="tabpanel"/)
  assert.match(publicSource + cabinetSource, /aria-controls=\{authPanelId\}/)
  assert.match(publicSource + cabinetSource, /onKeyDown=\{handleAuthTabsKeyDown\}/)
  assert.match(publicSource + cabinetSource, /aria-orientation="horizontal"/)
  assert.match(publicSource + cabinetSource, /tabIndex=\{/)
  assert.match(publicSource, /<Link className="app-brand" to="\/"/)
  assert.match(publicSource + cabinetSource, /aria-label="Открыть оплату в новой вкладке"/)
  assert.match(cabinetSource, /aria-label="Открыть Telegram-бота в новой вкладке"/)
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
  assert.match(adminSource, /TerminalKey/)
  assert.match(adminSource, /Webhook endpoint secret/)
  assert.match(adminSource, /Только Telegram/)
  assert.match(adminSource, /hostedCheckoutUrl/)
  assert.match(publicSource + cabinetSource, /готовые web-провайдеры/)
  assert.match(adminSource, /updateAdminPaymentProviderAccount/)
  assert.match(adminSource, /checkAdminPaymentProviderAccount/)
  assert.match(adminSource, /providerCheckResultClass/)
  assert.match(adminSource, /provider-check-result/)
  assert.match(adminSource, /role="status"/)
  assert.match(adminSource, /Webhook URL/)
  assert.match(adminSource, /provider-extra-settings/)
  assert.match(adminStylesSource, /\.provider-check-result/)
  assert.match(adminStylesSource, /\.provider-check-result-ok/)
  assert.match(adminStylesSource, /\.provider-check-result-problem/)
  assert.match(adminSource, /extraSettingsFields/)
  assert.match(adminSource, /Hosted checkout URL/)
  assert.match(adminSource, /Алгоритм подписи/)
  assert.doesNotMatch(adminSource, /Extra settings JSON/)
  assert.match(adminSource, /пустые поля секретов и дополнительных параметров сохраняют текущие значения/)
  assert.match(adminSource, /botTokenMasked|hasBotToken/)
  assert.match(adminSource, /Public bot username/)
  assert.match(adminSource, /Bot token/)
  assert.match(adminSource, /Secret token/)
  assert.match(adminSource, /WebApp URL/)
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

  assert.match(publicSource, /Показываем только активные тарифы/)
  assert.match(publicSource, /Нет доступных способов оплаты/)
  assert.match(publicSource, /Сброс пароля/)
  assert.match(publicSource, /Покупка сохранена|Привязываем покупку/)
  assert.match(publicSource, /Повторить привязку|Отменить эту покупку/)
  assert.match(publicSource, /onClearPendingCheckout/)
  assert.match(publicSource, /checkoutUnavailableReason|Оплата временно недоступна/)
  assert.match(publicSource, /CopyButton/)
  assert.match(publicSource, /tariffsLoading/)
  assert.match(publicSource, /ErrorBlock/)

  assert.match(telegramSource, /Главное меню VPN Platform/)
  assert.match(telegramSource, /Не публикуйте свой ключ|Не пересылайте ключ/)
  assert.match(telegramSource, /Секреты и платежные данные бот не запрашивает/)
  assert.match(telegramSource, /Пароль\/ключ не будет показан повторно/)
  assert.doesNotMatch(telegramSource, /my-secret-password|BEGIN PRIVATE KEY|bot-token-must-not-leak/i)
})
