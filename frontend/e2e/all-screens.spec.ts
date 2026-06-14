import { expect, test, type Page, type Route } from '@playwright/test'

const corsHeaders = {
  'access-control-allow-origin': '*',
  'access-control-allow-headers': 'content-type, authorization',
  'access-control-allow-methods': 'GET,POST,PUT,PATCH,DELETE,OPTIONS',
  'content-type': 'application/json; charset=utf-8'
}

const now = '2026-06-14T08:00:00Z'

const publicRoutes = ['/', '/tariffs', '/faq', '/help', '/account']
const adminSections = [
  'dashboard',
  'users',
  'payments',
  'tariffs',
  'subscriptions',
  'vpn',
  'nodes',
  'panels',
  'support',
  'audit',
  'bot',
  'releases',
  'faq',
  'content',
  'scenarios',
  'provisioning'
]

const user = {
  id: 'all-screens-user',
  email: 'all-screens@example.test',
  displayName: 'All Screens User',
  preferredLanguage: 'ru',
  referralCode: 'ALLSCREENS',
  status: 'Active'
}

const tariff = {
  id: 'tariff-all-screens',
  name: 'All Screens 30',
  slug: 'all-screens-30',
  description: 'Tariff for all screens smoke.',
  fullDescription: 'Stable tariff for browser all screens smoke.',
  features: ['2 devices', 'auto provisioning'],
  featuresJson: '[]',
  badge: 'Smoke',
  durationDays: 30,
  price: 490,
  currency: 'RUB',
  maxDevices: 2,
  trafficLimit: null,
  isTrial: false,
  isActive: true,
  sortOrder: 1,
  visibleFrom: null,
  visibleTo: null,
  tariffType: 'Personal',
  category: 'default',
  allowedRegionsCsv: 'EU',
  allowedNodeGroupsCsv: 'default',
  isReferralEligible: true,
  provisioningScenario: 'auto',
  afterPaymentText: 'Access appears in cabinet after payment.',
  createdAt: now,
  updatedAt: now
}

const subscription = {
  id: 'sub-all-screens',
  userId: user.id,
  tariffId: tariff.id,
  tariffName: tariff.name,
  status: 'Active',
  startAt: '2026-06-01T00:00:00Z',
  endAt: '2026-07-01T00:00:00Z',
  gracePeriodEndAt: null,
  autoRenewFlag: false,
  sourceChannel: 'Web',
  currentServerId: 'server-all-screens',
  currentAccessId: 'access-all-screens',
  lastPaymentId: 'payment-all-screens',
  renewalCount: 0,
  accessUri: 'vless://all-screens@example.test:443#all-screens',
  qrCodePath: 'qr://all-screens',
  configPath: 'config://all-screens',
  nodeName: 'EU Smoke',
  createdAt: now,
  updatedAt: now
}

const order = {
  id: 'order-all-screens',
  userId: user.id,
  userDisplayName: user.displayName,
  userEmail: user.email,
  tariffId: tariff.id,
  tariffName: tariff.name,
  amount: 490,
  currency: 'RUB',
  status: 'Paid',
  type: 'NewSubscription',
  channel: 'Web',
  paymentProvider: 'YooKassa',
  checkoutSessionId: null,
  expiresAt: '2026-06-14T09:00:00Z',
  paidAt: '2026-06-14T08:00:00Z',
  isFirstPurchase: true,
  paymentAttemptsCount: 1,
  lastPaymentId: 'payment-all-screens',
  lastPaymentStatus: 'Succeeded',
  lastPaymentProvider: 'YooKassa',
  linkedSubscriptionId: subscription.id,
  createdAt: now,
  updatedAt: now
}

const payment = {
  id: 'payment-all-screens',
  orderId: order.id,
  userId: user.id,
  userDisplayName: user.displayName,
  provider: 'YooKassa',
  paymentProviderAccountId: 'provider-yookassa',
  providerMode: 'Sandbox',
  providerPaymentId: 'yk-all-screens',
  confirmationUrl: 'https://pay.example.test/all-screens',
  returnUrl: 'http://127.0.0.1:5294',
  amount: 490,
  currency: 'RUB',
  status: 'Succeeded',
  signatureValidated: true,
  isActivationProcessed: true,
  paidAt: '2026-06-14T08:00:00Z',
  refundSupported: true,
  canRefund: false,
  refundableAmount: 0,
  refundBlockers: [],
  createdAt: now,
  updatedAt: now
}

const access = {
  id: 'access-all-screens',
  subscriptionId: subscription.id,
  userId: user.id,
  providerType: 'X3UI',
  providerAccessId: 'x3ui-all-screens',
  serverId: 'server-all-screens',
  serverName: 'EU Smoke',
  accessUri: subscription.accessUri,
  qrCodePayload: subscription.accessUri,
  qrCodePath: 'qr://all-screens',
  configPath: 'config://all-screens',
  status: 'Active',
  issuedAt: now,
  expiryDate: subscription.endAt,
  disabledAt: null,
  lastSyncedAt: now,
  revision: 1,
  history: [],
  createdAt: now,
  updatedAt: now
}

const providerAccount = {
  id: 'provider-yookassa',
  provider: 'YooKassa',
  mode: 'Sandbox',
  name: 'yookassa-smoke',
  publicName: 'YooKassa sandbox',
  isEnabled: true,
  isDefault: true,
  shopId: 'shop-smoke',
  apiBaseUrl: 'https://api.yookassa.ru',
  returnUrl: 'http://127.0.0.1:5295/payments/return',
  webhookUrl: 'http://127.0.0.1:19080/api/webhooks/payments/yookassa',
  hasSecretKey: true,
  hasWebhookSecret: true,
  useWebhookIpAllowList: false,
  allowedWebhookIpRangesCsv: '',
  extraSettingsJson: '{}',
  healthStatus: 'Healthy',
  isCheckoutConfigured: true,
  checkoutConfigurationIssue: null,
  capabilities: [{ key: 'checkout', label: 'Checkout', supported: true, status: 'Ready' }],
  requiredFields: [{ key: 'shopId', label: 'Shop ID', required: true, configured: true }],
  readinessBlockers: [],
  isPubliclyAvailable: true,
  createdAt: now,
  updatedAt: now
}

const release = {
  id: 'release-all-screens',
  releaseId: '2026-06-14-all-screens-browser-smoke',
  version: '0.107.0',
  releasedAt: now,
  title: 'All screens browser smoke',
  summary: 'Browser smoke covers public, cabinet and admin surfaces.',
  isActive: true,
  source: 'agent',
  items: [{ type: 'new', text: 'All screens smoke is available.', sortOrder: 1 }],
  createdAt: now,
  updatedAt: now
}

function jsonResponse(body: unknown, status = 200) {
  return { status, headers: corsHeaders, body: JSON.stringify(body) }
}

async function fulfillJson(route: Route, body: unknown, status = 200) {
  await route.fulfill(jsonResponse(body, status))
}

function authResponse(email = user.email) {
  return {
    accessToken: `access-token-${email}`,
    refreshToken: `refresh-token-${email}`,
    email,
    displayName: user.displayName
  }
}

async function installApiMock(page: Page) {
  await page.route('**/api/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname
    const method = request.method()

    if (method === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: corsHeaders })
      return
    }

    if (method === 'POST' && (path === '/api/auth/login' || path === '/api/auth/register')) {
      await fulfillJson(route, authResponse(request.postDataJSON()?.email ?? user.email))
      return
    }

    if (method === 'POST' && path === '/api/auth/logout') {
      await fulfillJson(route, { status: 'ok' })
      return
    }

    if (method === 'GET' && path === '/api/me') {
      await fulfillJson(route, user)
      return
    }

    if (method === 'GET' && path === '/api/public/content/home') {
      await fulfillJson(route, [
        { key: 'home.hero.title', value: 'All screens VPN' },
        { key: 'home.hero.subtitle', value: 'Browser smoke content.' },
        { key: 'home.hero.primaryCta', value: 'Choose tariff' },
        { key: 'home.pricing.title', value: 'Tariffs' },
        { key: 'home.finalCta.title', value: 'Start VPN' }
      ])
      return
    }

    if (method === 'GET' && path.startsWith('/api/public/content/faq')) {
      await fulfillJson(route, [
        { id: 'faq-all-screens', question: 'How to pay?', answer: 'Use sandbox provider.', category: 'Payment', sortOrder: 1 }
      ])
      return
    }

    if (method === 'GET' && path === '/api/public/tariffs') {
      await fulfillJson(route, [tariff])
      return
    }

    if (method === 'GET' && path === '/api/public/payments/providers') {
      await fulfillJson(route, [{ provider: 'YooKassa', publicName: 'YooKassa sandbox', mode: 'Sandbox', healthStatus: 'Healthy' }])
      return
    }

    if (method === 'POST' && path === '/api/public/checkout-sessions') {
      await fulfillJson(route, { id: 'checkout-all-screens', token: 'checkout-all-screens-token', tariffId: tariff.id, status: 'PendingAuth' })
      return
    }

    if (method === 'GET' && path.startsWith('/api/app-version')) {
      if (path.includes('/overview')) {
        await fulfillJson(route, {
          totalCount: 1,
          publishedCount: 1,
          upcomingCount: 0,
          hiddenCount: 0,
          agentCount: 1,
          manualCount: 0,
          seenCount: 0,
          totalViews: 0,
          latestPublishedReleaseId: release.releaseId,
          latestPublishedVersion: release.version,
          latestPublishedRelease: release,
          emptyReleaseIds: []
        })
        return
      }
      if (path.includes('/history') || path.includes('/admin/releases')) {
        await fulfillJson(route, [release])
        return
      }
      await fulfillJson(route, { latestRelease: release, hasUnseenRelease: false, seenAt: now })
      return
    }

    if (method === 'GET' && path === '/api/me/subscriptions') {
      await fulfillJson(route, [subscription])
      return
    }

    if (method === 'GET' && path === '/api/me/orders') {
      await fulfillJson(route, [order])
      return
    }

    if (method === 'GET' && path === '/api/me/payments') {
      await fulfillJson(route, [payment])
      return
    }

    if (method === 'GET' && path === '/api/me/accesses') {
      await fulfillJson(route, [access])
      return
    }

    if (method === 'GET' && path === '/api/me/referrals') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path.startsWith('/api/me/support/conversations')) {
      await fulfillJson(route, path.endsWith('/messages') ? [] : [{ id: 'support-all-screens', subject: 'Smoke support', status: 'Open', createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/me/telegram/status') {
      await fulfillJson(route, { isLinked: false, telegramUserId: null, username: null })
      return
    }

    if (method === 'GET' && path.includes('/qr')) {
      await route.fulfill({ status: 200, headers: { ...corsHeaders, 'content-type': 'image/svg+xml' }, body: '<svg xmlns="http://www.w3.org/2000/svg"><text>qr</text></svg>' })
      return
    }

    if (method === 'GET' && path === '/api/admin/dashboard/summary') {
      await fulfillJson(route, {
        generatedAt: now,
        totalUsers: 1,
        activeSubscriptions: 1,
        monthlyRevenue: 490,
        pendingOrders: 0,
        openSupportTickets: 1,
        activeVpnAccesses: 1,
        paymentProviderIssues: 0,
        vpnPanelIssues: 0,
        recentOrders: [order],
        recentPayments: [payment],
        provisioningQueue: []
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/users') {
      await fulfillJson(route, [user])
      return
    }

    if (method === 'GET' && path.startsWith('/api/admin/users/') && path.endsWith('/overview')) {
      await fulfillJson(route, {
        user,
        orders: [order],
        subscriptions: [subscription],
        payments: [payment],
        accessCredentials: [access],
        telegramAccounts: [],
        supportConversations: [],
        referralRewards: []
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/subscriptions') {
      await fulfillJson(route, [subscription])
      return
    }

    if (method === 'GET' && path === '/api/admin/access-credentials') {
      await fulfillJson(route, [access])
      return
    }

    if (method === 'GET' && path === '/api/admin/orders') {
      await fulfillJson(route, [order])
      return
    }

    if (method === 'GET' && path === '/api/admin/payments') {
      await fulfillJson(route, [payment])
      return
    }

    if (method === 'GET' && path === '/api/admin/payment-providers/accounts') {
      await fulfillJson(route, [providerAccount])
      return
    }

    if (method === 'GET' && path === '/api/admin/tariffs') {
      await fulfillJson(route, [tariff])
      return
    }

    if (method === 'GET' && path === '/api/admin/support/conversations') {
      await fulfillJson(route, [{ id: 'support-all-screens', userId: user.id, userEmail: user.email, subject: 'Smoke support', status: 'Open', createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path.endsWith('/messages')) {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/faq') {
      await fulfillJson(route, [{ id: 'faq-all-screens', question: 'How to pay?', answer: 'Use sandbox provider.', category: 'Payment', sortOrder: 1, isActive: true }])
      return
    }

    if (method === 'GET' && path === '/api/admin/faq/overview') {
      await fulfillJson(route, {
        totalCount: 1,
        activeCount: 1,
        hiddenCount: 0,
        homeCount: 1,
        faqPageCount: 1,
        publicCount: 1,
        categoryCount: 1,
        categories: ['Payment'],
        duplicateQuestions: [],
        hasPublicFaq: true,
        hasHomeFaq: true
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/site-content') {
      await fulfillJson(route, [{ id: 'content-hero', key: 'home.hero.title', value: 'All screens VPN', isActive: true, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/site-content/home-readiness') {
      await fulfillJson(route, {
        isReady: true,
        requiredCount: 1,
        presentCount: 1,
        activeRequiredCount: 1,
        missingKeys: [],
        inactiveKeys: [],
        emptyKeys: [],
        duplicateKeys: [],
        publicBlocksCount: 1,
        requiredKeys: ['home.hero.title']
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/work-scenarios') {
      await fulfillJson(route, [{ id: 'scenario-auto', key: 'auto', name: 'Auto provisioning', isActive: true, provisioningMode: 'auto' }])
      return
    }

    if (method === 'GET' && path === '/api/admin/servers') {
      await fulfillJson(route, [{ id: 'server-all-screens', name: 'EU Smoke', host: 'vpn.example.test', status: 'Active', healthStatus: 'Healthy', provider: 'hetzner', region: 'eu', capacity: 100, activeUsers: 1, validationMode: true }])
      return
    }

    if (method === 'GET' && path === '/api/admin/provisioning-runs') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels') {
      await fulfillJson(route, [{ id: 'panel-all-screens', name: '3x-ui Smoke', baseUrl: 'https://panel.example.test', login: 'admin', apiVariant: 'x-ui', sslVerificationMode: 'strict', capacity: 100, usedCapacity: 1, status: 'Active', healthStatus: 'Healthy', autoCreateInbound: false, version: '1.8.0', lastSyncAt: now }])
      return
    }

    if (method === 'GET' && path.includes('/vpn-panels/')) {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/telegram-bot/settings') {
      await fulfillJson(route, { isEnabled: false, botUsername: 'vpn_smoke_bot', hasBotToken: false, webhookUrl: '', status: 'Disabled' })
      return
    }

    if (method === 'GET' && path === '/api/admin/audit-logs') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && ['/api/admin/payment-webhook-events', '/api/admin/refunds'].includes(path)) {
      await fulfillJson(route, [])
      return
    }

    await fulfillJson(route, method === 'GET' ? [] : { status: 'ok' })
  })
}

function collectBrowserErrors(page: Page) {
  const errors: string[] = []
  page.on('console', (message) => {
    // Treat console.error as a failed screen smoke.
    if (message.type() === 'error') errors.push(message.text())
  })
  page.on('pageerror', (error) => errors.push(error.message))
  return errors
}

async function expectNonBlankPage(page: Page) {
  await expect(page.locator('body')).toBeVisible()
  await expect.poll(async () => page.locator('body').innerText().then((text) => text.trim().length)).toBeGreaterThan(30)
}

test('all public routes render without blank screens or browser errors', async ({ page }) => {
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  for (const route of publicRoutes) {
    await page.goto(route)
    await expectNonBlankPage(page)
  }

  expect(browserErrors).toEqual([])
})

test('cabinet auth and dashboard surfaces render without blank screens or browser errors', async ({ page }) => {
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  await page.goto('http://127.0.0.1:5294/')
  await expectNonBlankPage(page)

  const authForm = page.locator('#cabinet-auth-panel')
  await authForm.locator('input[type="email"]').fill(user.email)
  await authForm.locator('input[type="password"]').fill('Password123!')
  await authForm.locator('button[type="submit"]').click()
  await expect(page.locator('.code-block').filter({ hasText: 'vless://' }).first()).toBeVisible()
  await expectNonBlankPage(page)

  expect(browserErrors).toEqual([])
})

test('every admin section renders without blank screens or browser errors', async ({ page }) => {
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  await page.goto('http://127.0.0.1:5295/')
  await page.locator('input[type="email"]').fill('admin@example.test')
  await page.locator('input[type="password"]').fill('Password123!')
  await page.locator('form').locator('button[type="submit"]').click()
  await expect(page.locator('.admin-shell')).toBeVisible()

  for (const section of adminSections) {
    await page.goto(`http://127.0.0.1:5295/#${section}`)
    await expect(page.locator('.admin-shell')).toBeVisible()
    await expectNonBlankPage(page)
  }

  expect(browserErrors).toEqual([])
})
