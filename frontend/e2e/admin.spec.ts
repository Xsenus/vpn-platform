import { expect, test, type Page, type Route } from '@playwright/test'

const corsHeaders = {
  'access-control-allow-origin': '*',
  'access-control-allow-headers': 'content-type, authorization',
  'access-control-allow-methods': 'GET,POST,PUT,PATCH,DELETE,OPTIONS',
  'content-type': 'application/json; charset=utf-8'
}

const now = '2026-06-13T07:00:00Z'

function jsonResponse(body: unknown, status = 200) {
  return { status, headers: corsHeaders, body: JSON.stringify(body) }
}

async function fulfillJson(route: Route, body: unknown, status = 200) {
  await route.fulfill(jsonResponse(body, status))
}

function tariff(overrides: Record<string, unknown> = {}) {
  return {
    id: 'tariff-admin-pro',
    name: 'Admin Pro 30',
    slug: 'admin-pro-30',
    description: 'Тариф для проверки админки',
    fullDescription: 'Полное описание тарифа для проверки админского E2E.',
    features: ['3 устройства', 'Автовыдача'],
    featuresJson: JSON.stringify(['3 устройства', 'Автовыдача']),
    badge: 'E2E',
    durationDays: 30,
    price: 590,
    currency: 'RUB',
    maxDevices: 3,
    trafficLimit: null,
    isTrial: false,
    isActive: true,
    sortOrder: 10,
    visibleFrom: null,
    visibleTo: null,
    tariffType: 'Personal',
    category: 'default',
    allowedRegionsCsv: 'EU',
    allowedNodeGroupsCsv: 'default',
    isReferralEligible: true,
    provisioningScenario: 'auto',
    afterPaymentText: 'Доступ появится в кабинете после оплаты.',
    createdAt: now,
    updatedAt: now,
    ...overrides
  }
}

function paymentProviderAccount(overrides: Record<string, unknown> = {}) {
  return {
    id: 'provider-yookassa',
    provider: 'YooKassa',
    mode: 'Sandbox',
    name: 'yookassa-sandbox',
    publicName: 'YooKassa sandbox',
    isEnabled: true,
    isDefault: true,
    shopId: 'shop-e2e',
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
    capabilities: [
      { key: 'checkout', label: 'Checkout', supported: true, status: 'Ready' },
      { key: 'refund', label: 'Refund', supported: true, status: 'Ready' }
    ],
    requiredFields: [
      { key: 'shopId', label: 'Shop ID', required: true, configured: true },
      { key: 'secretKey', label: 'Secret key', required: true, configured: true }
    ],
    readinessBlockers: [],
    isPubliclyAvailable: true,
    createdAt: now,
    updatedAt: now,
    ...overrides
  }
}

function release(overrides: Record<string, unknown> = {}) {
  return {
    id: 'release-admin-e2e',
    releaseId: '2026-06-13-admin-e2e-seed',
    version: '0.91.1',
    releasedAt: '2026-06-13T07:00:00Z',
    title: 'Админский E2E seed',
    summary: 'Релиз для проверки раздела Что нового.',
    isActive: true,
    source: 'agent',
    items: [{ type: 'new', text: 'Раздел релизов открыт в E2E.', sortOrder: 1 }],
    createdByUserId: 'admin-user',
    createdByUserName: 'Admin E2E',
    updatedByUserId: 'admin-user',
    updatedByUserName: 'Admin E2E',
    createdAt: now,
    updatedAt: now,
    ...overrides
  }
}

function workScenario(overrides: Record<string, unknown> = {}) {
  return {
    id: 'scenario-auto',
    name: 'Автовыдача E2E',
    key: 'auto',
    isActive: true,
    allowedTariffIdsJson: '[]',
    vpnProtocol: 'vless',
    serverSelectionRule: 'least-loaded',
    inboundSelectionRule: 'default',
    provisioningMode: 'auto',
    onPaymentSucceeded: 'create_subscription_and_access',
    onPaymentFailed: 'keep_order_pending',
    onRefund: 'disable_access',
    onSubscriptionExpired: 'disable_access',
    onRenewal: 'extend_subscription',
    cabinetText: 'Доступ выдан автоматически.',
    telegramText: 'VPN готов.',
    generateQrCode: true,
    maxDevices: 3,
    trafficLimit: null,
    sortOrder: 1,
    createdAt: now,
    updatedAt: now,
    ...overrides
  }
}

function vpnPanel(overrides: Record<string, unknown> = {}) {
  return {
    id: 'panel-eu',
    name: 'EU 3x-ui Sandbox',
    baseUrl: 'https://panel-eu.example.test',
    region: 'EU',
    status: 'Active',
    healthStatus: 'Healthy',
    login: 'admin',
    sslVerificationMode: 'Strict',
    apiVariant: 'X3UiOfficial',
    capacity: 1000,
    usedCapacity: 12,
    autoCreateInbound: true,
    defaultInboundTemplateJson: '{"remark":"default-vless","protocol":"vless","port":443}',
    lastHealthCheckAt: now,
    lastSyncAt: now,
    version: '2.4.8',
    lastError: 'Panel sync lease expired before completion.',
    createdAt: now,
    updatedAt: now,
    ...overrides
  }
}

async function mockAdminApi(page: Page) {
  const requests: Array<{ method: string; path: string; body: unknown }> = []
  const providers = [paymentProviderAccount()]
  const tariffs = [tariff()]
  const releases = [release()]
  const scenarios = [workScenario()]
  const panels = [vpnPanel()]
  const subscriptions: Array<Record<string, unknown>> = [{ id: 'sub-e2e', userId: 'user-e2e', tariffId: 'tariff-admin-pro', tariffName: 'Admin Pro 30', status: 'Active', startAt: now, endAt: '2026-07-13T07:00:00Z', gracePeriodEndAt: null, autoRenewFlag: false, sourceChannel: 'Web', currentServerId: 'server-eu', currentAccessId: 'access-e2e', lastPaymentId: 'payment-e2e', renewalCount: 0, accessUri: 'vless://admin-e2e@example.test', qrCodePath: 'qr://admin', configPath: 'config://admin', nodeName: 'EU Sandbox', createdAt: now, updatedAt: now }]
  const inbounds: Array<Record<string, unknown>> = [
    { id: 'inbound-default', vpnPanelId: 'panel-eu', externalInboundId: '1', name: 'default-vless', protocol: 'vless', port: 443, listen: '0.0.0.0', settingsJson: '{"clients":[]}', streamSettingsJson: '{"network":"tcp","security":"reality"}', sniffingJson: '{}', isDefault: true, isActive: true, capacity: 1000, usedCapacity: 12 },
    { id: 'inbound-backup', vpnPanelId: 'panel-eu', externalInboundId: '2', name: 'backup-vless', protocol: 'vless', port: 8443, listen: '0.0.0.0', settingsJson: '{"clients":[]}', streamSettingsJson: '{"network":"tcp","security":"reality"}', sniffingJson: '{}', isDefault: false, isActive: true, capacity: 20, usedCapacity: 3 }
  ]
  const clients: Array<Record<string, unknown>> = [{ id: 'client-e2e', userId: 'user-e2e', subscriptionId: 'sub-e2e', vpnPanelId: 'panel-eu', vpnInboundId: 'inbound-default', externalClientId: 'client-e2e', email: 'client@example.test', uuid: '00000000-0000-4000-8000-000000000001', flow: 'xtls-rprx-vision', limitIp: 3, totalGb: null, expiryTime: '2026-07-13T07:00:00Z', enable: true, configUri: 'vless://client@example.test', qrCodePayload: 'vless://client@example.test', syncStatus: 'Synced', lastSyncedAt: now }]

  await page.route('**/api/**', async (route) => {
    const request = route.request()
    const method = request.method()
    const url = new URL(request.url())
    const path = url.pathname

    if (method === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: corsHeaders })
      return
    }

    const body = method === 'GET' ? null : request.postDataJSON()
    requests.push({ method, path, body })

    if (method === 'POST' && path === '/api/auth/login') {
      await fulfillJson(route, {
        accessToken: 'admin-e2e-token',
        refreshToken: 'admin-e2e-refresh',
        email: 'admin-e2e@example.test',
        displayName: 'Admin E2E'
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/dashboard/summary') {
      await fulfillJson(route, {
        totalUsers: 4,
        telegramUsers: 2,
        activeSubscriptions: 3,
        expiringSubscriptions: 1,
        paidOrders: 5,
        pendingOrders: 1,
        failedPayments: 1,
        recentPayments: 2,
        recentOrders: 3,
        vpnAccessesCount: 3,
        vpnNodesCount: 1,
        healthyVpnNodes: 1,
        vpnPanelsCount: panels.length,
        healthyVpnPanels: panels.filter((item) => item.healthStatus === 'Healthy').length,
        supportConversationsCount: 1,
        openSupportConversations: 1,
        provisioningErrors: 0,
        productionReadiness: {
          isReady: true,
          status: 'Ready',
          checks: [
            { key: 'payments', label: 'Платежи', status: 'Ready', message: 'Sandbox провайдер готов.', category: 'Sales', actionHref: '#payments', actionLabel: 'Открыть оплаты' },
            { key: 'vpn', label: 'VPN', status: 'Ready', message: 'Панель и inbound доступны.', category: 'VPN', actionHref: '#panels', actionLabel: 'Открыть панели' }
          ]
        },
        generatedAt: now
      })
      return
    }

    if (method === 'GET' && path === '/api/app-version/latest') {
      await fulfillJson(route, { currentVersion: null, latestRelease: releases[releases.length - 1] ?? null, seenByCurrentUser: true })
      return
    }

    if (method === 'GET' && path === '/api/app-version/history') {
      await fulfillJson(route, releases)
      return
    }

    if (method === 'GET' && path === '/api/admin/audit-logs') {
      await fulfillJson(route, [{ id: 'audit-login', actorType: 'Admin', actorId: 'admin-user', action: 'auth.login', entityType: 'Auth', entityId: 'admin-user', beforeJson: '{}', afterJson: '{}', ip: '127.0.0.1', userAgent: 'Playwright', createdAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/users') {
      await fulfillJson(route, [{ id: 'user-e2e', email: 'client@example.test', displayName: 'Client E2E', rolesCsv: 'User', status: 'Active', isBlocked: false, preferredLanguage: 'ru', referralCode: 'E2E', authSource: 'Local', emailConfirmed: true, lastLoginAt: now, telegramRegistrationCompletedAt: null, createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/users/user-e2e/overview') {
      await fulfillJson(route, {
        user: { id: 'user-e2e', email: 'client@example.test', displayName: 'Client E2E', rolesCsv: 'User', status: 'Active', isBlocked: false, preferredLanguage: 'ru', referralCode: 'E2E', authSource: 'Local', emailConfirmed: true, lastLoginAt: now, telegramRegistrationCompletedAt: null, createdAt: now, updatedAt: now },
        telegramAccounts: [],
        orders: [],
        payments: [],
        subscriptions: [],
        accessCredentials: [],
        supportConversations: []
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/subscriptions') {
      await fulfillJson(route, subscriptions)
      return
    }

    if (method === 'POST' && path === '/api/admin/subscriptions/sub-e2e/cancel') {
      subscriptions[0] = { ...subscriptions[0], status: 'Cancelled', currentServerId: null, currentAccessId: null, updatedAt: now }
      await fulfillJson(route, { id: 'sub-e2e', status: 'Cancelled', cancelledAt: now })
      return
    }

    if (method === 'GET' && path === '/api/admin/access-credentials') {
      await fulfillJson(route, [{ id: 'access-e2e', subscriptionId: 'sub-e2e', userId: 'user-e2e', providerType: 'X3UI', providerAccessId: 'client-e2e', serverId: 'server-eu', serverName: 'EU Sandbox', accessUri: 'vless://admin-e2e@example.test', qrCodePayload: 'vless://admin-e2e@example.test', qrCodePath: 'qr://admin', configPath: 'config://admin', status: 'Active', issuedAt: now, expiryDate: '2026-07-13T07:00:00Z', disabledAt: null, lastSyncedAt: now, revision: 1, history: [], createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/orders') {
      await fulfillJson(route, [{ id: 'order-e2e', userId: 'user-e2e', userDisplayName: 'Client E2E', userEmail: 'client@example.test', tariffId: 'tariff-admin-pro', tariffName: 'Admin Pro 30', amount: 590, currency: 'RUB', status: 'PaymentReceived', type: 'NewSubscription', channel: 'Web', paymentProvider: 'YooKassa', checkoutSessionId: null, expiresAt: '2026-06-14T07:00:00Z', paidAt: now, isFirstPurchase: true, paymentAttemptsCount: 1, lastPaymentId: 'payment-e2e', lastPaymentStatus: 'Succeeded', lastPaymentProvider: 'YooKassa', linkedSubscriptionId: 'sub-e2e', createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/payments') {
      await fulfillJson(route, [{ id: 'payment-e2e', orderId: 'order-e2e', userId: 'user-e2e', userDisplayName: 'Client E2E', provider: 'YooKassa', paymentProviderAccountId: 'provider-yookassa', providerMode: 'Sandbox', providerPaymentId: 'yk-admin-e2e', externalEventId: 'evt-admin-e2e', idempotencyKey: 'idem-admin-e2e', confirmationUrl: 'http://127.0.0.1:5295/payments/return', returnUrl: 'http://127.0.0.1:5295', amount: 590, currency: 'RUB', status: 'Succeeded', signatureValidated: true, isActivationProcessed: true, activationProcessedAt: now, paidAt: now, failedAt: null, refundedAt: null, refundedAmount: 0, statusReason: null, webhookEventsCount: 1, refundsCount: 0, refundSupported: true, canRefund: false, refundableAmount: 0, refundBlockers: [], createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/payment-providers/accounts') {
      await fulfillJson(route, providers)
      return
    }

    if (method === 'POST' && path === '/api/admin/payment-providers/accounts/provider-yookassa/check') {
      const account = paymentProviderAccount({ healthStatus: 'Unknown', updatedAt: '2026-06-13T07:05:00Z' })
      providers[0] = account
      await fulfillJson(route, {
        accountId: account.id,
        provider: account.provider,
        mode: account.mode,
        isReady: true,
        checkScope: 'ConfigurationOnly',
        configurationStatus: 'Ready',
        healthStatus: 'Unknown',
        message: 'Конфигурация готова. Внешний кабинет провайдера не запрашивался.',
        details: ['Shop ID заполнен', 'Секреты заданы', 'Webhook URL задан'],
        checkedAt: '2026-06-13T07:05:00Z',
        account
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/payment-webhook-events') {
      await fulfillJson(route, [{ id: 'webhook-e2e', provider: 'YooKassa', paymentAttemptId: 'payment-e2e', paymentProviderAccountId: 'provider-yookassa', providerPaymentId: 'yk-admin-e2e', externalEventId: 'evt-admin-e2e', eventType: 'payment.succeeded', status: 'Processed', signatureValidated: true, receivedAt: now, processedAt: now, errorText: '' }])
      return
    }

    if (method === 'GET' && path === '/api/admin/refunds') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/support/conversations') {
      await fulfillJson(route, [{ id: 'support-e2e', userId: 'user-e2e', telegramUserId: null, channel: 'Web', status: 'open', subject: 'Проверка доступа', assignedToUserId: null, internalNote: '', closedAt: null, createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/support/conversations/support-e2e/messages') {
      await fulfillJson(route, [{ id: 'support-message-e2e', supportConversationId: 'support-e2e', userId: 'user-e2e', telegramUserId: null, direction: 'Inbound', text: 'Нужна проверка доступа', attachmentsJson: '[]', isInternalNote: false, createdAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/tariffs') {
      await fulfillJson(route, tariffs)
      return
    }

    if (method === 'POST' && path === '/api/admin/tariffs') {
      const created = tariff({ ...(body as Record<string, unknown>), id: 'tariff-created-e2e', createdAt: now, updatedAt: now })
      tariffs.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    if (method === 'GET' && path === '/api/app-version/admin/releases') {
      await fulfillJson(route, releases)
      return
    }

    if (method === 'GET' && path === '/api/app-version/admin/releases/overview') {
      await fulfillJson(route, {
        totalCount: releases.length,
        publishedCount: releases.filter((item) => item.isActive).length,
        upcomingCount: 0,
        hiddenCount: releases.filter((item) => !item.isActive).length,
        agentCount: releases.filter((item) => item.source === 'agent').length,
        manualCount: releases.filter((item) => item.source === 'manual').length,
        seenCount: 1,
        latestPublishedReleaseId: releases[releases.length - 1]?.releaseId ?? null,
        latestPublishedVersion: releases[releases.length - 1]?.version ?? null,
        emptyReleaseIds: []
      })
      return
    }

    if (method === 'POST' && path === '/api/app-version/admin/releases') {
      const created = release({ ...(body as Record<string, unknown>), id: 'release-created-e2e', createdAt: now, updatedAt: now })
      releases.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    if (method === 'GET' && path === '/api/admin/faq') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/faq/overview') {
      await fulfillJson(route, { totalCount: 0, activeCount: 0, hiddenCount: 0, homeCount: 0, faqPageCount: 0, publicCount: 0, categoryCount: 0, categories: [], duplicateQuestions: [], hasPublicFaq: false, hasHomeFaq: false })
      return
    }

    if (method === 'GET' && path === '/api/admin/site-content') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/site-content/home-readiness') {
      await fulfillJson(route, { isReady: true, requiredCount: 1, presentCount: 1, activeRequiredCount: 1, missingKeys: [], inactiveKeys: [], emptyKeys: [], duplicateKeys: [], publicBlocksCount: 1, requiredKeys: ['home.hero.title'] })
      return
    }

    if (method === 'GET' && path === '/api/admin/work-scenarios') {
      await fulfillJson(route, scenarios)
      return
    }

    if (method === 'POST' && path === '/api/admin/work-scenarios') {
      const created = workScenario({ ...(body as Record<string, unknown>), id: 'scenario-created-e2e', createdAt: now, updatedAt: now })
      scenarios.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    if (method === 'DELETE' && path === '/api/admin/servers/server-eu') {
      await fulfillJson(route, { id: 'server-eu', deleted: false, archived: true, linkedSubscriptions: 0, linkedAccesses: 0, linkedProvisioningRuns: 0, linkedHealthChecks: 2, linkedMigrationJobs: 1 })
      return
    }

    if (method === 'GET' && path === '/api/admin/servers') {
      await fulfillJson(route, [{ id: 'server-eu', name: 'EU Sandbox', host: 'eu.example.test', ipAddress: '10.0.0.1', provider: 'Local', region: 'EU', country: 'DE', datacenter: 'FRA', status: 'Active', capacity: 1000, usedCapacity: 12, supportedProtocolsCsv: 'vless', healthStatus: 'Healthy', lastHealthCheckAt: now, lastHealthLatencyMs: 15, lastHealthError: null, lastHealthMetadataJson: '{}', provisioningStatus: 'Ready', provisioningMode: 'auto', provisioningModeTitle: 'Автоматически', provisioningRiskLevel: 'safe', liveDeployAllowed: false, provisioningNextAction: null, provisioningOperatorWarning: null, precheckMode: 'validation', precheckModeTitle: 'Validation', installedVersion: '1.0.0', backupStatus: 'Ready', monitoringStatus: 'Ready', loggingStatus: 'Ready', tagsCsv: 'validation-mode:true', priority: 10, isAvailableForNewUsers: true, sshUser: 'root', sshPort: 22, sshAuthMethod: 'Password', sshCredentialConfigured: true, skipHostKeyChecking: true, panelBaseUrl: 'https://panel-eu.example.test', panelUsername: 'admin', panelPasswordConfigured: true, panelInboundId: 1, publicHostname: 'vpn-eu.example.test', publicPort: 443, nodeGroupId: 'default' }])
      return
    }

    if (method === 'GET' && path === '/api/admin/provisioning-runs') {
      await fulfillJson(route, [])
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels') {
      await fulfillJson(route, panels)
      return
    }

    if (method === 'POST' && path === '/api/admin/vpn-panels/panel-eu/test-connection') {
      await fulfillJson(route, { id: 'panel-health-e2e', vpnPanelId: 'panel-eu', status: 'Healthy', latencyMs: 22, version: '2.4.9', errorMessage: '', checkedAt: '2026-06-13T07:06:00Z' })
      return
    }

    if (method === 'POST' && path === '/api/admin/vpn-panels/panel-eu/sync') {
      await fulfillJson(route, { id: 'panel-sync-e2e', vpnPanelId: 'panel-eu', status: 'Succeeded', startedAt: '2026-06-13T07:07:00Z', finishedAt: '2026-06-13T07:07:10Z', summaryJson: '{"clients":1}', errorMessage: '' })
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels/panel-eu/inbounds') {
      await fulfillJson(route, inbounds)
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels/panel-eu/clients') {
      await fulfillJson(route, clients)
      return
    }

    if (method === 'POST' && path === '/api/admin/vpn-clients/client-e2e/migrate') {
      const body = request.postDataJSON() as { targetInboundId?: string }
      const target = inbounds.find((item) => item.id === body.targetInboundId)
      if (!target) {
        await fulfillJson(route, { error: 'Target inbound not found.' }, 400)
        return
      }
      const source = inbounds.find((item) => item.id === clients[0].vpnInboundId)
      if (source) source.usedCapacity = Number(source.usedCapacity) - 1
      target.usedCapacity = Number(target.usedCapacity) + 1
      clients[0] = { ...clients[0], vpnInboundId: target.id, syncStatus: 'migrated', configUri: 'vless://client@backup.example.test:8443', qrCodePayload: 'vless://client@backup.example.test:8443', lastSyncedAt: now }
      await fulfillJson(route, clients[0])
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels/panel-eu/health-checks') {
      await fulfillJson(route, [{ id: 'panel-health-e2e', vpnPanelId: 'panel-eu', status: 'Healthy', latencyMs: 22, version: '2.4.9', errorMessage: '', checkedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels/panel-eu/sync-runs') {
      await fulfillJson(route, [{ id: 'panel-sync-e2e', vpnPanelId: 'panel-eu', status: 'Failed', startedAt: now, finishedAt: now, summaryJson: '{}', errorMessage: 'Remote panel sync failed.' }])
      return
    }

    if (method === 'GET' && path === '/api/admin/telegram-bot/settings') {
      await fulfillJson(route, { enabled: false, mode: 'LongPolling', publicBotUsername: 'vpnplatform_bot', hasBotToken: false, botTokenMasked: '', webhookUrl: '', hasSecretToken: false, adminChatId: '', webAppUrl: 'http://127.0.0.1:5294', welcomeText: 'Добро пожаловать', instructionText: 'Выберите тариф', supportText: 'Поддержка', afterPaymentTextTemplate: 'После оплаты', renewalTextTemplate: 'Продление', paymentFailedTextTemplate: 'Ошибка оплаты', subscriptionExpiredTextTemplate: 'Подписка истекла', generatedAt: now })
      return
    }

    await fulfillJson(route, { error: `Unhandled ${method} ${path}` }, 404)
  })

  return {
    getLastRequest: (path: string, method = 'POST') =>
      requests.findLast((item) => item.method === method && item.path === path)
  }
}

async function openAdminSection(page: Page, name: string, id: string) {
  const tab = page.getByRole('tab', { name })
  if (await tab.isVisible()) {
    await tab.click()
  } else {
    await page.getByRole('combobox', { name: 'Раздел' }).selectOption(id)
  }
  await expect(page.locator(`#${id}`)).toBeVisible()
}

test('admin panel covers login, payments, tariffs, VPN panels, scenarios and releases', async ({ page }, testInfo) => {
  const consoleErrors: string[] = []
  const failedResponses: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') consoleErrors.push(message.text())
  })
  page.on('pageerror', (error) => consoleErrors.push(error.message))
  page.on('response', (response) => {
    if (response.status() >= 400) failedResponses.push(`${response.status()} ${response.url()}`)
  })

  const api = await mockAdminApi(page)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Вход администратора' })).toBeVisible()
  await page.locator('.admin-login-form input[type="email"]').fill('admin-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('AdminPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()

  await expect(page.getByRole('heading', { name: 'Дашборд' })).toBeVisible()
  await expect(page.getByText('Готовность к live-продажам')).toBeVisible()
  await expect(page.getByText('Sandbox провайдер готов.')).toBeVisible()

  await openAdminSection(page, 'Оплаты', 'payments')
  await expect(page.getByText('YooKassa sandbox')).toBeVisible()
  await page.locator('#payments').getByRole('button', { name: 'Проверить настройки' }).click()
  await expect(page.getByText('Настройки готовы', { exact: true })).toBeVisible()
  await expect(page.getByText('Конфигурация готова. Внешний кабинет провайдера не запрашивался.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/payment-providers/accounts/provider-yookassa/check')).toBeTruthy()

  await openAdminSection(page, 'Тарифы', 'tariffs')
  const tariffsPanel = page.locator('#tariffs')
  await tariffsPanel.getByLabel('Название').fill('E2E Premium 45')
  await tariffsPanel.getByLabel('Slug').fill('e2e-premium-45')
  await tariffsPanel.getByRole('spinbutton', { name: 'Цена' }).fill('790')
  await tariffsPanel.getByLabel('Короткое описание').fill('Тариф создан браузерным E2E.')
  await tariffsPanel.getByLabel('Преимущества, по одному в строке').fill('5 устройств\nПриоритетный сервер')
  const createTariffRequest = page.waitForRequest((request) => {
    const url = new URL(request.url())
    return request.method() === 'POST' && url.pathname === '/api/admin/tariffs'
  })
  await tariffsPanel.getByRole('button', { name: 'Создать тариф' }).click()
  await createTariffRequest
  await expect(tariffsPanel.locator('.list-item-vertical strong').filter({ hasText: 'E2E Premium 45' })).toBeVisible()
  expect(api.getLastRequest('/api/admin/tariffs')).toBeTruthy()

  await openAdminSection(page, 'VPN-доступы', 'vpn')
  await expect(page.getByText('vless://admin-e2e@example.test')).toBeVisible()

  await openAdminSection(page, 'Подписки', 'subscriptions')
  const subscriptionsPanel = page.locator('#subscriptions')
  await subscriptionsPanel.getByRole('button', { name: 'Отменить' }).click()
  await expect(subscriptionsPanel.getByRole('dialog')).toContainText('VPN-доступ будет отозван и удален с сервера')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await subscriptionsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Подписка отменена, VPN-доступ отозван и удален с сервера.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/subscriptions/sub-e2e/cancel')).toBeTruthy()

  await openAdminSection(page, 'Серверы', 'nodes')
  const nodesPanel = page.locator('#nodes')
  await nodesPanel.getByRole('button', { name: 'Удалить' }).click()
  await expect(nodesPanel.getByRole('dialog')).toContainText('health-check или миграций')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await nodesPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Сервер EU Sandbox архивирован: связей 3.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/servers/server-eu', 'DELETE')).toBeTruthy()

  await openAdminSection(page, '3x-ui панели', 'panels')
  await expect(page.locator('#panels strong').filter({ hasText: 'EU 3x-ui Sandbox' })).toBeVisible()
  await expect(page.locator('#panels strong').filter({ hasText: 'client@example.test' })).toBeVisible()
  await expect(page.getByText('Последняя ошибка: Panel sync lease expired before completion.')).toBeVisible()
  await expect(page.getByText('Remote panel sync failed.')).toBeVisible()
  await page.locator('#panels').getByRole('button', { name: 'Сбросить трафик' }).click()
  await expect(page.locator('#panels').getByRole('dialog')).toContainText('Необратимо обнулить счётчики трафика')
  await expect(page.locator('#panels').getByRole('dialog')).toContainText('ручной сверки')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await page.locator('#panels').getByRole('button', { name: 'Отмена' }).click()
  await page.getByLabel('Целевой inbound для client@example.test').selectOption('inbound-backup')
  await page.locator('#panels').getByRole('button', { name: 'Перенести' }).click()
  await expect(page.locator('#panels').getByRole('dialog')).toContainText('временному slot панели и target inbound')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await page.locator('#panels').getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('VPN-клиент client@example.test перенесен на выбранный inbound.')).toBeVisible()
  await expect(page.locator('#panels').getByText('inbound backup-vless')).toBeVisible()
  expect(api.getLastRequest('/api/admin/vpn-clients/client-e2e/migrate')).toBeTruthy()
  await page.locator('#panels').getByRole('button', { name: 'Проверить' }).click()
  await expect(page.getByText('Проверка панели: Healthy (2.4.9)')).toBeVisible()
  await page.locator('#panels').getByRole('button', { name: 'Синхронизировать' }).first().click()
  await expect(page.getByText('Синхронизация Succeeded: {"clients":1}')).toBeVisible()

  await openAdminSection(page, 'Сценарии', 'scenarios')
  const scenariosPanel = page.locator('#scenarios')
  await scenariosPanel.getByLabel('Название').fill('E2E ручная проверка')
  await scenariosPanel.getByLabel('Ключ').fill('e2e-manual')
  await scenariosPanel.getByLabel('VPN-протокол').fill('vless')
  await scenariosPanel.getByLabel('Текст для кабинета').fill('Сценарий создан из Playwright E2E.')
  await scenariosPanel.getByRole('button', { name: 'Создать сценарий' }).click()
  await expect(scenariosPanel.locator('strong').filter({ hasText: 'E2E ручная проверка' })).toBeVisible()

  await openAdminSection(page, 'Что нового', 'releases')
  const releasesPanel = page.locator('#releases')
  await releasesPanel.getByLabel('Release ID').fill('2026-06-13-admin-playwright-e2e')
  await releasesPanel.getByLabel('Версия').fill('0.92.0')
  await releasesPanel.getByLabel('Дата публикации').fill('2026-06-13T07:00')
  await releasesPanel.getByLabel('Заголовок').fill('Админка покрыта E2E')
  await releasesPanel.getByLabel('Короткое описание').fill('Playwright проверяет основные разделы админки.')
  await releasesPanel.getByLabel('Текст').fill('Добавлен E2E для админских разделов.')
  await releasesPanel.getByRole('button', { name: 'Создать релиз' }).click()
  await expect(page.getByText('Админка покрыта E2E').first()).toBeVisible()

  expect(api.getLastRequest('/api/admin/work-scenarios')).toBeTruthy()
  expect(api.getLastRequest('/api/app-version/admin/releases')).toBeTruthy()
  expect(failedResponses).toEqual([])
  if (testInfo.project.name.startsWith('mobile-')) {
    await page.screenshot({ path: testInfo.outputPath('admin-mobile.png'), fullPage: true })
  }

  expect(consoleErrors).toEqual([])
})
