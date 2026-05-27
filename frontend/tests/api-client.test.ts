import test from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { ApiClient, buildAuthHeaders, normalizeApiError } from '../packages/api-client/src/index.ts'

test('buildAuthHeaders returns bearer header when token exists', () => {
  assert.deepEqual(buildAuthHeaders('abc'), { Authorization: 'Bearer abc' })
  assert.deepEqual(buildAuthHeaders(''), {})
})

test('normalizeApiError prefers error field and message field', () => {
  assert.equal(normalizeApiError({ error: 'boom' }, 'fallback'), 'boom')
  assert.equal(normalizeApiError({ message: 'denied' }, 'fallback'), 'denied')
  assert.equal(normalizeApiError(null, 'fallback'), 'fallback')
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
    isFirstPurchase: false
  })

  const headers = new Headers(calls[0]?.init?.headers)
  assert.equal(calls[0]?.url, 'http://localhost:8080/api/me/orders')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(headers.get('Authorization'), 'Bearer token-123')
  assert.match(String(calls[0]?.init?.body), /WELCOME10/)
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

test('ApiClient.createAdminServer posts server payload with auth token', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ id: 'node-1', name: 'nl-01', host: 'nl-01.example.com', ipAddress: '203.0.113.10', provider: 'hetzner', region: 'eu', country: 'NL', datacenter: 'fsn1', status: 'New', capacity: 5000, usedCapacity: 0, supportedProtocolsCsv: 'vless,vmess,trojan', healthStatus: 'Unknown', installedVersion: '', backupStatus: 'unknown', monitoringStatus: 'unknown', loggingStatus: 'unknown', tagsCsv: '', priority: 100, isAvailableForNewUsers: false }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.createAdminServer('admin-token', {
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
  })

  const headers = new Headers(calls[0]?.init?.headers)
  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/servers')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(headers.get('Authorization'), 'Bearer admin-token')
  assert.match(String(calls[0]?.init?.body), /nl-01/)
  assert.match(String(calls[0]?.init?.body), /sshCredential/)
  assert.match(String(calls[0]?.init?.body), /validationMode/)
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
    return new Response(JSON.stringify([]), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getAdminVpnPanels('admin-token')
  await client.getAdminVpnPanelClients('admin-token', 'panel-1')
  await client.getAdminVpnPanelHealthChecks('admin-token', 'panel-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/vpn-panels')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/clients')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/health-checks')
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

    return new Response(JSON.stringify({ totalUsers: 1, activeSubscriptions: 1, generatedAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getAdminDashboardSummary('admin-token')
  await client.getAdminUserOverview('admin-token', 'user-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/dashboard/summary')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/users/user-1/overview')
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient admin payment providers expose readiness fields without secrets', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify([{ id: 'account-1', provider: 'YooKassa', mode: 'Sandbox', name: 'Yoo', publicName: 'Yoo', isEnabled: true, isDefault: true, shopId: 'shop', apiBaseUrl: '', returnUrl: '', hasSecretKey: true, hasWebhookSecret: false, useWebhookIpAllowList: false, allowedWebhookIpRangesCsv: '', extraSettingsJson: '{"apiSecret":"***"}', healthStatus: 'Unknown', isCheckoutConfigured: true, checkoutConfigurationIssue: null, capabilitiesJson: '["createPayment"]' }]), {
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
    return new Response(JSON.stringify({ enabled: false, mode: 'Polling', publicBotUsername: 'vpn_bot', hasBotToken: true, botTokenMasked: '1234***7890', webhookUrl: '', hasSecretToken: false, welcomeText: 'Welcome', instructionText: 'Instruction', supportText: 'Support', afterPaymentTextTemplate: 'After', generatedAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const settings = await client.getAdminTelegramBotSettings('admin-token')
  await client.updateAdminTelegramBotSettings('admin-token', { welcomeText: 'Welcome' })

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/telegram-bot/settings')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/telegram-bot/settings')
  assert.equal(calls[1]?.init?.method, 'PATCH')
  assert.equal(settings.botTokenMasked, '1234***7890')
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
      return new Response(JSON.stringify([{ id: 'ppa-1', provider: 'YooKassa', mode: 'Sandbox', isCheckoutConfigured: true, extraSettingsJson: '{"apiSecret":"***"}' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/telegram-bot/settings')) {
      return new Response(JSON.stringify({ enabled: false, hasBotToken: true, botTokenMasked: '1234***7890', hasSecretToken: true, welcomeText: 'Welcome' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
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
  const uiSource = readFileSync(new URL('../packages/ui/src/index.tsx', import.meta.url), 'utf8')
  const stylesSource = readFileSync(new URL('../packages/ui/src/styles.css', import.meta.url), 'utf8')

  assert.match(publicSource, /getPublicPaymentProviders/)
  assert.match(publicSource, /paymentProvidersLoading/)
  assert.match(publicSource, /Нет доступных способов оплаты|paymentProvidersLoading/)
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
  assert.match(adminSource, /getAdminUserOverview/)
  assert.match(adminSource, /getAdminAccessQrSvg/)
  assert.match(adminSource, /credentialsConfigured/i)
  assert.match(adminSource, /ConfirmButton/)
  assert.match(adminSource, /adminAuthRequiredMessage/)
  assert.match(adminSource, /Войдите как администратор/)
  assert.match(adminSource, /clearAdminSession/)
  assert.match(adminSource, /Завершить сессию/)
  assert.match(adminSource, /fieldset className="form-section"/)
  assert.match(adminSource, /Идентификация сервера|Подключение и безопасность|Тексты сценариев/)
  assert.match(stylesSource, /form:has\(\.form-section\)/)
  assert.match(stylesSource, /form\.form-grid/)
  assert.match(stylesSource, /label:has\(textarea\)/)
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
  assert.match(adminSource, /botTokenMasked|hasBotToken/)
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
