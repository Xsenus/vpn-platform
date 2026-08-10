import { expect, test, type Page, type Route, type TestInfo } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

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
  'support',
  'audit',
  'payments',
  'tariffs',
  'referrals',
  'subscriptions',
  'vpn',
  'nodes',
  'panels',
  'provisioning',
  'bot',
  'releases',
  'faq',
  'content',
  'scenarios'
]

const responsiveViewports = [
  { name: 'compact-mobile-with-scrollbar', width: 305, height: 568 },
  { name: 'compact-mobile', width: 320, height: 568 },
  { name: 'mobile', width: 360, height: 800 },
  { name: 'large-mobile', width: 390, height: 844 },
  { name: 'above-large-mobile', width: 391, height: 844 },
  { name: 'above-compact-layout', width: 521, height: 800 },
  { name: 'mobile-landscape', width: 568, height: 320 },
  { name: 'above-mobile-layout', width: 641, height: 900 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'above-tablet-layout', width: 769, height: 1024 },
  { name: 'above-cabinet-layout', width: 821, height: 1180 },
  { name: 'above-shared-shell-layout', width: 901, height: 768 },
  { name: 'above-public-layout', width: 961, height: 800 },
  { name: 'small-desktop', width: 1024, height: 768 },
  { name: 'above-small-desktop', width: 1025, height: 800 },
  { name: 'above-wide-layout', width: 1281, height: 900 },
  { name: 'desktop', width: 1440, height: 900 },
  { name: 'wide-desktop', width: 1920, height: 1080 },
  { name: '2k-desktop', width: 2560, height: 1440 }
]

const captureVisualAudit = process.env.VPN_PLATFORM_VISUAL_AUDIT === '1'

const user = {
  id: 'all-screens-user',
  email: 'all-screens@example.test',
  displayName: 'All Screens User',
  preferredLanguage: 'ru',
  referralCode: 'ALLSCREENS',
  status: 'Active'
}

const adminUser = {
  ...user,
  rolesCsv: 'User',
  isBlocked: false,
  authSource: 'Local',
  emailConfirmed: true,
  lastLoginAt: now,
  telegramRegistrationCompletedAt: null,
  createdAt: now,
  updatedAt: now
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
  blockReason: null,
  suspendedAt: null,
  cancelledAt: null,
  lifecycleAttemptCount: 0,
  lifecycleProcessingStartedAt: null,
  lifecycleLeaseExpiresAt: null,
  lifecycleNextAttemptAt: null,
  lifecycleLastError: null,
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
  status: 'Completed',
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
  externalEventId: '',
  idempotencyKey: null,
  confirmationUrl: 'https://pay.example.test/all-screens',
  returnUrl: 'http://127.0.0.1:5294',
  amount: 490,
  currency: 'RUB',
  status: 'Succeeded',
  signatureValidated: true,
  isActivationProcessed: true,
  activationProcessedAt: '2026-06-14T08:00:00Z',
  paidAt: '2026-06-14T08:00:00Z',
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
  id: 'access-all-screens',
  subscriptionId: subscription.id,
  subscriptionStatus: 'Active',
  isTerminal: false,
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
  capabilitiesJson: '["checkout"]',
  capabilities: [{ key: 'checkout', label: 'Checkout', supported: true, status: 'Ready' }],
  requiredFields: [{ key: 'shopId', label: 'Shop ID', required: true, configured: true, issue: null }],
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
  items: [{ id: 'release-item-all-screens', type: 'new', text: 'All screens smoke is available.', sortOrder: 1 }],
  createdByUserId: null,
  createdByUserName: 'Agent',
  updatedByUserId: null,
  updatedByUserName: 'Agent',
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
        ['home.hero.title', 'All screens VPN'],
        ['home.hero.subtitle', 'Browser smoke content.'],
        ['home.hero.primaryCta', 'Choose tariff'],
        ['home.pricing.title', 'Tariffs'],
        ['home.finalCta.title', 'Start VPN']
      ].map(([key, value], index) => ({
        id: `public-content-${index + 1}`,
        key,
        value,
        group: 'home',
        label: key,
        description: '',
        inputType: 'text',
        isActive: true,
        sortOrder: index + 1,
        createdAt: now,
        updatedAt: now
      })))
      return
    }

    if (method === 'GET' && path.startsWith('/api/public/content/faq')) {
      await fulfillJson(route, [
        { id: 'faq-all-screens', question: 'How to pay?', answer: 'Use sandbox provider.', category: 'Payment', sortOrder: 1, isActive: true, showOnHome: true, showOnFaqPage: true, createdAt: now, updatedAt: now }
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
      await fulfillJson(route, {
        id: 'checkout-all-screens', token: 'checkout-all-screens-token', tariffId: tariff.id,
        userId: null, orderId: null, status: 'open', expiresAt: now, emailHint: null
      })
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
      await fulfillJson(route, { currentVersion: release.version, latestRelease: release, seenByCurrentUser: true })
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
      await fulfillJson(route, path.endsWith('/messages') ? [] : [{
        id: 'support-all-screens',
        userId: user.id,
        telegramUserId: null,
        channel: 'web',
        status: 'open',
        subject: 'Smoke support',
        assignedToUserId: null,
        internalNote: '',
        revision: 0,
        closedAt: null,
        createdAt: now,
        updatedAt: now
      }])
      return
    }

    if (method === 'GET' && path === '/api/me/telegram/status') {
      await fulfillJson(route, { isLinked: false, telegramUserId: null, username: null, linkedAt: null })
      return
    }

    if (method === 'GET' && path.includes('/qr')) {
      await route.fulfill({ status: 200, headers: { ...corsHeaders, 'content-type': 'image/svg+xml' }, body: '<svg xmlns="http://www.w3.org/2000/svg"><text>qr</text></svg>' })
      return
    }

    if (method === 'GET' && path === '/api/admin/session') {
      await fulfillJson(route, {
        userId: 'admin-all-screens',
        email: 'admin@example.test',
        displayName: 'Admin Smoke',
        roles: ['Admin'],
        capabilities: {
          adminRead: true,
          adminWrite: true,
          financeRead: true,
          financeWrite: true,
          supportRead: true,
          supportWrite: true,
          provisioningManage: true,
          vpnManage: true,
          botManage: true,
          settingsManage: true
        }
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/dashboard/summary') {
      await fulfillJson(route, {
        totalUsers: 1,
        telegramUsers: 0,
        activeSubscriptions: 1,
        expiringSubscriptions: 0,
        paidOrders: 1,
        pendingOrders: 0,
        failedPayments: 0,
        recentPayments: 1,
        recentOrders: 1,
        vpnAccessesCount: 1,
        vpnNodesCount: 1,
        healthyVpnNodes: 1,
        vpnPanelsCount: 1,
        healthyVpnPanels: 1,
        supportConversationsCount: 1,
        openSupportConversations: 1,
        provisioningErrors: 0,
        productionReadiness: {
          isReady: false,
          status: 'Blocked',
          checks: [{ key: 'vpn-panel', label: 'VPN panel', status: 'Blocked', message: 'Live panel evidence is required.', category: 'VPN', severity: 'critical', actionLabel: 'Open panels', actionHref: '#panels' }]
        },
        generatedAt: now
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/users') {
      await fulfillJson(route, [adminUser])
      return
    }

    if (method === 'GET' && path.startsWith('/api/admin/users/') && path.endsWith('/overview')) {
      await fulfillJson(route, {
        user: adminUser,
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

    if (method === 'GET' && path === '/api/admin/referral-programs') {
      await fulfillJson(route, [{ id: 'referral-program-all-screens', name: 'Welcome', status: 'active', startAt: null, endAt: null, ruleDefinition: '{"firstPurchaseOnly":true}', rewardDefinition: '{"referrer":{"type":"bonus-days","value":7,"unit":"days","autoApprove":true}}', antiFraudSettings: '{}', createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/referrals') {
      await fulfillJson(route, [{ id: 'reward-all-screens', userId: user.id, sourceUserId: 'source-user', referralProgramId: 'referral-program-all-screens', type: 'bonus-days', status: 'Approved', value: 7, currencyOrUnit: 'days', processedAt: now, createdAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/support/conversations') {
      await fulfillJson(route, [{ id: 'support-all-screens', userId: user.id, telegramUserId: null, channel: 'web', subject: 'Smoke support', status: 'open', assignedToUserId: null, internalNote: '', revision: 0, closedAt: null, createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path.endsWith('/messages')) {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/faq') {
      await fulfillJson(route, [{ id: 'faq-all-screens', question: 'How to pay?', answer: 'Use sandbox provider.', category: 'Payment', sortOrder: 1, isActive: true, showOnHome: true, showOnFaqPage: true, createdAt: now, updatedAt: now }])
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
      await fulfillJson(route, [{ id: 'content-hero', key: 'home.hero.title', value: 'All screens VPN', group: 'home', label: 'Hero title', description: '', inputType: 'text', isActive: true, sortOrder: 1, createdAt: now, updatedAt: now }])
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
      await fulfillJson(route, [{
        id: 'scenario-auto',
        key: 'auto',
        name: 'Auto provisioning',
        isActive: true,
        allowedTariffIdsJson: '[]',
        vpnProtocol: 'vless',
        serverSelectionRule: 'least-loaded',
        inboundSelectionRule: 'default',
        provisioningMode: 'auto',
        onPaymentSucceeded: 'create_subscription_and_access',
        onPaymentFailed: 'keep_order_pending',
        onRefund: 'disable_access',
        onSubscriptionExpired: 'disable_access_after_grace',
        onRenewal: 'extend_subscription',
        cabinetText: 'Access is ready.',
        telegramText: 'VPN is ready.',
        generateQrCode: true,
        maxDevices: 3,
        trafficLimit: null,
        sortOrder: 1,
        createdAt: now,
        updatedAt: now
      }])
      return
    }

    if (method === 'GET' && path === '/api/admin/servers') {
      await fulfillJson(route, [{
        id: 'server-all-screens', name: 'EU Smoke', host: 'vpn.example.test', ipAddress: '203.0.113.10', provider: 'hetzner', region: 'eu', country: 'NL', datacenter: 'fsn1',
        status: 'Ready', capacity: 100, usedCapacity: 1, supportedProtocolsCsv: 'vless', healthStatus: 'Healthy', lastHealthCheckAt: now, lastHealthLatencyMs: 12,
        lastHealthError: '', lastHealthMetadataJson: '{}', provisioningStatus: 'Succeeded', provisioningMode: 'validation-deploy', provisioningModeTitle: 'Validation deploy',
        provisioningRiskLevel: 'low', liveDeployAllowed: false, provisioningNextAction: 'Проверьте precheck.', provisioningOperatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.',
        precheckMode: 'dry-run', precheckModeTitle: 'Dry-run precheck', installedVersion: '1.0.0', backupStatus: 'Ready', monitoringStatus: 'Ready', loggingStatus: 'Ready',
        tagsCsv: 'validation-mode:true', priority: 10, isAvailableForNewUsers: true, sshUser: 'root', sshPort: 22, sshAuthMethod: 'ssh_key', sshCredentialConfigured: true,
        skipHostKeyChecking: true, panelBaseUrl: 'https://panel.example.test', panelUsername: 'admin', panelPasswordConfigured: true, panelInboundId: 1,
        publicHostname: 'vpn.example.test', publicPort: 443, nodeGroupId: null, createdAt: now, updatedAt: now
      }])
      return
    }

    if (method === 'GET' && path === '/api/admin/provisioning-runs') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels') {
      await fulfillJson(route, [{
        id: 'panel-all-screens',
        name: '3x-ui Smoke',
        baseUrl: 'https://panel.example.test',
        region: 'eu',
        status: 'Active',
        healthStatus: 'Healthy',
        login: 'admin',
        sslVerificationMode: 'Strict',
        apiVariant: 'X3UiOfficial',
        capacity: 100,
        usedCapacity: 1,
        autoCreateInbound: false,
        defaultInboundTemplateJson: '{}',
        lastHealthCheckAt: now,
        lastSyncAt: now,
        version: '1.8.0',
        lastError: '',
        createdAt: now,
        updatedAt: now
      }])
      return
    }

    if (method === 'GET' && path.includes('/vpn-panels/')) {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/telegram-bot/settings') {
      await fulfillJson(route, {
        enabled: false, mode: 'LongPolling', publicBotUsername: 'vpn_smoke_bot', hasBotToken: false, botTokenMasked: '', webhookUrl: '', hasSecretToken: false,
        adminChatId: '', webAppUrl: 'https://cabinet.example.test', welcomeText: 'Welcome', instructionText: 'Instruction', supportText: 'Support',
        afterPaymentTextTemplate: 'After payment', renewalTextTemplate: 'Renewal', paymentFailedTextTemplate: 'Payment failed', subscriptionExpiredTextTemplate: 'Expired', generatedAt: now
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/audit-logs') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/notification-deliveries') {
      await fulfillJson(route, [{
        id: 'notification-all-screens',
        userId: user.id,
        templateKey: 'password_reset_requested',
        channel: 'Email',
        maskedToAddress: 'al***@example.test',
        status: 'Failed',
        attempts: 5,
        processingStartedAt: null,
        nextAttemptAt: null,
        sentAt: null,
        errorText: 'SMTP connection unavailable',
        createdAt: now,
        updatedAt: now
      }])
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

async function captureAuditScreenshot(page: Page, testInfo: TestInfo, name: string) {
  if (!captureVisualAudit) return
  await page.screenshot({
    path: testInfo.outputPath(`${name}.png`),
    fullPage: true,
    style: '.skip-link { visibility: hidden !important; }'
  })
}

async function expectNonBlankPage(page: Page) {
  await expect(page.locator('body')).toBeVisible()
  await expect.poll(async () => page.locator('body').innerText().then((text) => text.trim().length)).toBeGreaterThan(30)
}

async function expectPageQuality(page: Page, screenName: string) {
  const issues = await page.evaluate(() => {
    const problems: string[] = []
    const isVisible = (element: HTMLElement) => {
      const style = getComputedStyle(element)
      const rect = element.getBoundingClientRect()
      return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0
    }
    const labelledByText = (element: HTMLElement) => (element.getAttribute('aria-labelledby') ?? '')
      .split(/\s+/)
      .filter(Boolean)
      .map((id) => document.getElementById(id)?.textContent?.trim() ?? '')
      .join(' ')
      .trim()
    const hasAccessibleName = (element: HTMLElement) => {
      if (element.getAttribute('aria-label')?.trim() || labelledByText(element) || element.title.trim()) return true
      if (element instanceof HTMLInputElement && ['button', 'submit', 'reset'].includes(element.type) && element.value.trim()) return true
      if (element instanceof HTMLInputElement || element instanceof HTMLSelectElement || element instanceof HTMLTextAreaElement) {
        return Array.from(element.labels ?? []).some((label) => Boolean(label.textContent?.trim()))
      }
      if (element.textContent?.trim()) return true
      return Boolean(element.querySelector('img[alt]:not([alt=""])'))
    }

    if (!document.documentElement.lang.trim()) problems.push('html element has no lang attribute')
    if (!document.querySelector('main, [role="main"]')) problems.push('page has no main landmark')

    const ids = new Map<string, number>()
    for (const element of Array.from(document.querySelectorAll<HTMLElement>('[id]'))) {
      if (!element.id) continue
      ids.set(element.id, (ids.get(element.id) ?? 0) + 1)
    }
    for (const [id, count] of ids) {
      if (count > 1) problems.push(`duplicate id: ${id} (${count})`)
    }

    for (const image of Array.from(document.querySelectorAll<HTMLImageElement>('img'))) {
      if (isVisible(image) && !image.hasAttribute('alt')) problems.push(`image has no alt: ${image.src}`)
    }

    for (const control of Array.from(document.querySelectorAll<HTMLElement>('input:not([type="hidden"]), select, textarea'))) {
      if (isVisible(control) && !hasAccessibleName(control)) {
        problems.push(`form control has no accessible name: ${control.id || control.tagName.toLowerCase()}`)
      }
    }

    for (const action of Array.from(document.querySelectorAll<HTMLElement>('a[href], button, [role="tab"]'))) {
      if (isVisible(action) && !hasAccessibleName(action)) {
        problems.push(`action has no accessible name: ${action.id || action.tagName.toLowerCase()}`)
      }
    }

    return problems
  })

  expect(issues, `${screenName} must meet the page quality baseline`).toEqual([])
}

async function expectWcagQuality(page: Page, screenName: string) {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'])
    .analyze()
  const violations = results.violations.map((violation) => ({
    id: violation.id,
    impact: violation.impact,
    help: violation.help,
    nodes: violation.nodes.map((node) => ({ target: node.target, summary: node.failureSummary }))
  }))

  expect(violations, `${screenName} must pass automated WCAG A/AA and best-practice checks`).toEqual([])
}

async function expectLocalBackgroundAssets(page: Page, selectors: string[]) {
  const issues = await page.evaluate(async (targetSelectors) => {
    const problems: string[] = []
    const assetUrls = new Set<string>()

    for (const selector of targetSelectors) {
      const element = document.querySelector<HTMLElement>(selector)
      if (!element) {
        problems.push(`background owner is missing: ${selector}`)
        continue
      }

      const imageValue = getComputedStyle(element).backgroundImage
      const urls = Array.from(imageValue.matchAll(/url\(["']?(.*?)["']?\)/g), (match) => match[1])
      if (urls.length === 0) problems.push(`background image is missing: ${selector}`)
      for (const url of urls) assetUrls.add(new URL(url, window.location.href).href)
    }

    for (const url of assetUrls) {
      if (new URL(url).origin !== window.location.origin) {
        problems.push(`background image is external: ${url}`)
        continue
      }

      try {
        const image = new Image()
        image.src = url
        await image.decode()
        if (image.naturalWidth < 1200 || image.naturalHeight < 800) {
          problems.push(`background image is undersized: ${url} (${image.naturalWidth}x${image.naturalHeight})`)
        }
      } catch {
        problems.push(`background image did not load: ${url}`)
      }
    }

    return problems
  }, selectors)

  expect(issues, 'visual backgrounds must be local, loaded and large enough for responsive crops').toEqual([])
}

async function expectResponsiveLayout(page: Page, screenName: string) {
  await expectNonBlankPage(page)
  const issues = await page.evaluate(() => {
    const viewportWidth = document.documentElement.clientWidth
    const problems: string[] = []
    if (document.documentElement.scrollWidth > viewportWidth + 1) {
      problems.push(`document overflow: ${document.documentElement.scrollWidth}px > ${viewportWidth}px`)
    }

    const interactiveElements = Array.from(document.querySelectorAll<HTMLElement>(
      'a[href], button, input, select, textarea, [role="tab"]'
    ))
    for (const element of interactiveElements) {
      const style = getComputedStyle(element)
      const rect = element.getBoundingClientRect()
      if (style.display === 'none' || style.visibility === 'hidden' || rect.width === 0 || rect.height === 0) continue
      if (rect.left < -1 || rect.right > viewportWidth + 1) {
        const identity = element.getAttribute('aria-label') || element.textContent?.trim() || element.tagName.toLowerCase()
        problems.push(`interactive element clipped: ${identity.slice(0, 80)} (${Math.round(rect.left)}..${Math.round(rect.right)})`)
      }
    }

    return problems
  })

  expect(issues, `${screenName} must fit the viewport`).toEqual([])
}

test('all public routes render without blank screens or browser errors', async ({ page }, testInfo) => {
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  for (const route of publicRoutes) {
    await page.goto(route)
    await expectNonBlankPage(page)
    await expectPageQuality(page, route)
    await expectWcagQuality(page, route)
    if (route === '/') {
      await expectLocalBackgroundAssets(page, ['.landing-hero', '.landing-illustration', '.coverage-map'])
      await captureAuditScreenshot(page, testInfo, 'public-home-desktop')
    }
    if (route === '/account') await captureAuditScreenshot(page, testInfo, 'public-account-desktop')
  }

  expect(browserErrors).toEqual([])
})

test('cabinet auth and dashboard surfaces render without blank screens or browser errors', async ({ page }, testInfo) => {
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  await page.goto('http://127.0.0.1:5294/')
  await expectNonBlankPage(page)
  await expectPageQuality(page, 'cabinet auth')
  await expectWcagQuality(page, 'cabinet auth')
  await captureAuditScreenshot(page, testInfo, 'cabinet-auth-desktop')

  const authForm = page.locator('#cabinet-auth-panel')
  await authForm.locator('input[type="email"]').fill(user.email)
  await authForm.locator('input[type="password"]').fill('Password123!')
  await authForm.locator('button[type="submit"]').click()
  await expect(page.locator('.code-block').filter({ hasText: 'vless://' }).first()).toBeVisible()
  await expectNonBlankPage(page)
  await expectPageQuality(page, 'cabinet dashboard')
  await expectWcagQuality(page, 'cabinet dashboard')
  await captureAuditScreenshot(page, testInfo, 'cabinet-dashboard-desktop')

  expect(browserErrors).toEqual([])
})

test('every admin section renders without blank screens or browser errors', async ({ page }, testInfo) => {
  test.setTimeout(120_000)
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  await page.goto('http://127.0.0.1:5295/')
  await expectPageQuality(page, 'admin auth')
  await expectWcagQuality(page, 'admin auth')
  await expectLocalBackgroundAssets(page, ['.admin-login-intro'])
  await captureAuditScreenshot(page, testInfo, 'admin-auth-desktop')
  await page.locator('input[type="email"]').fill('admin@example.test')
  await page.locator('input[type="password"]').fill('Password123!')
  await page.locator('form').locator('button[type="submit"]').click()
  await expect(page.locator('.admin-shell')).toBeVisible()

  for (const section of adminSections) {
    await page.goto(`http://127.0.0.1:5295/#${section}`)
    await expect(page.locator('.admin-shell')).toBeVisible()
    await expectNonBlankPage(page)
    await expectPageQuality(page, `admin ${section}`)
    await expectWcagQuality(page, `admin ${section}`)
    if (section === 'dashboard' || section === 'panels') {
      await captureAuditScreenshot(page, testInfo, `admin-${section}-desktop`)
    }
  }

  expect(browserErrors).toEqual([])
})

test('all public routes fit representative responsive viewports', async ({ page }, testInfo) => {
  test.setTimeout(240_000)
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  for (const viewport of responsiveViewports) {
    await page.setViewportSize(viewport)
    for (const route of publicRoutes) {
      await page.goto(route)
      await expectResponsiveLayout(page, `${route} at ${viewport.name}`)
      if (viewport.name === 'compact-mobile') await expectWcagQuality(page, `${route} at ${viewport.name}`)
      if (viewport.name === 'compact-mobile' && (route === '/' || route === '/account')) {
        await captureAuditScreenshot(page, testInfo, `public-${route === '/' ? 'home' : 'account'}-mobile`)
      }
    }
  }

  expect(browserErrors).toEqual([])
})

test('cabinet fits representative responsive viewports after authentication', async ({ page }, testInfo) => {
  test.setTimeout(240_000)
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  for (const viewport of responsiveViewports) {
    await page.setViewportSize(viewport)
    await page.goto('http://127.0.0.1:5294/')
    const authForm = page.locator('#cabinet-auth-panel')
    if (await authForm.isVisible()) {
      await authForm.locator('input[type="email"]').fill(user.email)
      await authForm.locator('input[type="password"]').fill('Password123!')
      await authForm.locator('button[type="submit"]').click()
      await expect(page.locator('.code-block').filter({ hasText: 'vless://' }).first()).toBeVisible()
    }
    await expectResponsiveLayout(page, `cabinet at ${viewport.name}`)
    if (viewport.name === 'compact-mobile') await expectWcagQuality(page, `cabinet at ${viewport.name}`)
    if (viewport.name === 'compact-mobile') await captureAuditScreenshot(page, testInfo, 'cabinet-dashboard-mobile')
  }

  expect(browserErrors).toEqual([])
})

test('every admin section fits representative responsive viewports', async ({ page }, testInfo) => {
  test.setTimeout(480_000)
  const browserErrors = collectBrowserErrors(page)
  await installApiMock(page)

  for (const viewport of responsiveViewports) {
    await page.setViewportSize(viewport)
    await page.goto('http://127.0.0.1:5295/')
    const hasStoredAdminSession = await page.evaluate(() => Boolean(sessionStorage.getItem('vpn-platform-admin-token')))
    if (hasStoredAdminSession) {
      await expect(page.locator('.admin-shell')).toBeVisible()
    } else {
      await page.locator('input[type="email"]').fill('admin@example.test')
      await page.locator('input[type="password"]').fill('Password123!')
      await page.locator('form').locator('button[type="submit"]').click()
      await expect(page.locator('.admin-shell')).toBeVisible()
    }
    await expect(page.getByText('[object Object]', { exact: false })).toHaveCount(0)

    for (const section of adminSections) {
      await page.goto(`http://127.0.0.1:5295/#${section}`)
      await expectResponsiveLayout(page, `admin ${section} at ${viewport.name}`)
      if (viewport.name === 'compact-mobile') await expectWcagQuality(page, `admin ${section} at ${viewport.name}`)
      if (viewport.name === 'compact-mobile' && (section === 'dashboard' || section === 'panels')) {
        await captureAuditScreenshot(page, testInfo, `admin-${section}-mobile`)
      }
    }
  }

  expect(browserErrors).toEqual([])
})
