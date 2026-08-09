import { expect, test, type Page, type Route } from '@playwright/test'

const corsHeaders = {
  'access-control-allow-origin': '*',
  'access-control-allow-headers': 'content-type, authorization',
  'access-control-allow-methods': 'GET,POST,PUT,PATCH,DELETE,OPTIONS',
  'content-type': 'application/json; charset=utf-8'
}

const now = '2026-06-13T07:00:00Z'
const fullAdminCapabilities = {
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

function referralProgram(overrides: Record<string, unknown> = {}) {
  return {
    id: 'referral-program-e2e',
    name: 'Welcome E2E',
    status: 'active',
    startAt: null,
    endAt: null,
    ruleDefinition: '{"firstPurchaseOnly":true,"minimumOrderAmount":0,"allowedChannels":["Web"]}',
    rewardDefinition: '{"referrer":{"type":"bonus-days","value":7,"unit":"days","autoApprove":true}}',
    antiFraudSettings: '{}',
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
    capabilitiesJson: '["checkout","refund"]',
    capabilities: [
      { key: 'checkout', label: 'Checkout', supported: true, status: 'Ready' },
      { key: 'refund', label: 'Refund', supported: true, status: 'Ready' }
    ],
    requiredFields: [
      { key: 'shopId', label: 'Shop ID', required: true, configured: true, issue: null },
      { key: 'secretKey', label: 'Secret key', required: true, configured: true, issue: null }
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
    items: [{ id: 'release-item-admin-e2e', type: 'new', text: 'Раздел релизов открыт в E2E.', sortOrder: 1 }],
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

function vpnServer(overrides: Record<string, unknown> = {}) {
  return {
    id: 'server-eu',
    name: 'EU Sandbox',
    host: 'eu.example.test',
    ipAddress: '10.0.0.1',
    provider: 'Local',
    region: 'EU',
    country: 'DE',
    datacenter: 'FRA',
    status: 'Ready',
    capacity: 1000,
    usedCapacity: 12,
    supportedProtocolsCsv: 'vless',
    healthStatus: 'Healthy',
    lastHealthCheckAt: now,
    lastHealthLatencyMs: 15,
    lastHealthError: '',
    lastHealthMetadataJson: '{}',
    provisioningStatus: 'Succeeded',
    provisioningMode: 'validation-deploy',
    provisioningModeTitle: 'Validation deploy',
    provisioningRiskLevel: 'low',
    liveDeployAllowed: false,
    provisioningNextAction: 'Проверьте precheck перед deploy.',
    provisioningOperatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.',
    precheckMode: 'dry-run',
    precheckModeTitle: 'Dry-run precheck',
    installedVersion: '1.0.0',
    backupStatus: 'Ready',
    monitoringStatus: 'Ready',
    loggingStatus: 'Ready',
    tagsCsv: 'validation-mode:true',
    priority: 10,
    isAvailableForNewUsers: true,
    sshUser: 'root',
    sshPort: 22,
    sshAuthMethod: 'ssh_key',
    sshCredentialConfigured: true,
    skipHostKeyChecking: true,
    panelBaseUrl: 'https://panel-eu.example.test',
    panelUsername: 'admin',
    panelPasswordConfigured: true,
    panelInboundId: 1,
    publicHostname: 'vpn-eu.example.test',
    publicPort: 443,
    nodeGroupId: null,
    createdAt: now,
    updatedAt: now,
    ...overrides
  }
}

function provisioningRun(overrides: Record<string, unknown> = {}) {
  return {
    id: 'provisioning-e2e',
    nodeId: 'server-eu',
    nodeName: 'EU Sandbox',
    targetHost: 'eu.example.test',
    sshPort: 22,
    username: 'root',
    authMethod: 'ssh_key',
    credentialsConfigured: true,
    source: 'admin',
    owner: 'admin',
    validationMode: true,
    mode: 'dry-run',
    modeTitle: 'Dry-run precheck',
    riskLevel: 'safe',
    liveDeployAllowed: false,
    nextAction: 'Проверьте precheck.',
    operatorWarning: 'Dry-run не меняет VPS.',
    deployMode: 'validation-deploy',
    deployModeTitle: 'Validation deploy',
    deployRiskLevel: 'low',
    deployLiveDeployAllowed: false,
    deployNextAction: 'Запустите validation deploy.',
    deployOperatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.',
    status: 'ReadyToDeploy',
    currentStep: 'ready_to_deploy',
    requestedByUserId: null,
    dryRun: true,
    attemptCount: 0,
    processingStartedAt: null,
    leaseExpiresAt: null,
    lastError: '',
    startedAt: now,
    finishedAt: now,
    errorSummary: '',
    executionLog: 'precheck ok',
    executionLogPreview: 'precheck ok',
    precheckReportPreview: 'VPS precheck ready.',
    createdAt: now,
    updatedAt: now,
    ...overrides
  }
}

function telegramBotSettings(overrides: Record<string, unknown> = {}) {
  return {
    enabled: false,
    mode: 'LongPolling',
    publicBotUsername: 'vpnplatform_bot',
    hasBotToken: false,
    botTokenMasked: '',
    webhookUrl: '',
    hasSecretToken: false,
    adminChatId: '',
    webAppUrl: 'http://127.0.0.1:5294',
    welcomeText: 'Добро пожаловать',
    instructionText: 'Выберите тариф',
    supportText: 'Поддержка',
    afterPaymentTextTemplate: 'После оплаты',
    renewalTextTemplate: 'Продление',
    paymentFailedTextTemplate: 'Ошибка оплаты',
    subscriptionExpiredTextTemplate: 'Подписка истекла',
    generatedAt: now,
    ...overrides
  }
}

async function mockAdminApi(page: Page) {
  const requests: Array<{ method: string; path: string; body: unknown; authorization: string }> = []
  let logoutShouldFail = false
  let dashboardShouldDeny = false
  let invalidUsersResponse = false
  let invalidPaymentProviderAccountsResponse = false
  let invalidTariffsResponse = false
  let invalidVpnPanelsResponse = false
  let invalidServersResponse = false
  let invalidProvisioningRunsResponse = false
  let invalidBotSettingsResponse = false
  let delayNextVpnPanelInboundsResponse = false
  let delayNextBotSettingsCheckResponse = false
  const providers = [paymentProviderAccount()]
  const tariffs = [tariff()]
  const referralPrograms = [referralProgram()]
  const releases = [release()]
  const scenarios = [workScenario()]
  const panels = [vpnPanel()]
  const subscriptions: Array<Record<string, unknown>> = [
    { id: 'sub-e2e', userId: 'user-e2e', tariffId: 'tariff-admin-pro', tariffName: 'Admin Pro 30', status: 'Active', startAt: now, endAt: '2026-07-13T07:00:00Z', gracePeriodEndAt: null, autoRenewFlag: false, sourceChannel: 'Web', currentServerId: 'server-eu', currentAccessId: 'access-e2e', lastPaymentId: 'payment-e2e', renewalCount: 0, blockReason: null, suspendedAt: null, cancelledAt: null, lifecycleAttemptCount: 0, lifecycleProcessingStartedAt: null, lifecycleLeaseExpiresAt: null, lifecycleNextAttemptAt: null, lifecycleLastError: null, accessUri: 'vless://admin-e2e@example.test', qrCodePath: 'qr://admin', configPath: 'config://admin', nodeName: 'EU Sandbox', createdAt: now, updatedAt: now },
    { id: 'sub-cancelled', userId: 'user-e2e', tariffId: 'tariff-admin-pro', tariffName: 'Отменённая подписка', status: 'Cancelled', startAt: '2026-05-01T00:00:00Z', endAt: now, cancelledAt: now, gracePeriodEndAt: null, autoRenewFlag: false, sourceChannel: 'Web', currentServerId: 'server-eu', currentAccessId: 'access-revoked', lastPaymentId: 'payment-old', renewalCount: 0, blockReason: null, suspendedAt: null, lifecycleAttemptCount: 0, lifecycleProcessingStartedAt: null, lifecycleLeaseExpiresAt: null, lifecycleNextAttemptAt: null, lifecycleLastError: null, accessUri: 'vless://cancelled-stale-secret@example.test', qrCodePath: 'qr://cancelled-stale-secret', configPath: 'config://cancelled-stale-secret', nodeName: 'EU Sandbox', createdAt: now, updatedAt: now }
  ]
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
    requests.push({
      method,
      path,
      body,
      authorization: request.headers().authorization ?? ''
    })

    if (method === 'POST' && path === '/api/auth/login') {
      const loginEmail = String((body as { email?: string } | null)?.email ?? '')
      if (loginEmail === 'user-e2e@example.test') {
        await fulfillJson(route, {
          accessToken: 'user-e2e-token',
          refreshToken: 'user-e2e-refresh',
          email: loginEmail,
          displayName: 'User E2E'
        })
        return
      }
      if (loginEmail === 'finance-e2e@example.test') {
        await fulfillJson(route, {
          accessToken: 'finance-e2e-token',
          refreshToken: 'finance-e2e-refresh',
          email: loginEmail,
          displayName: 'Finance E2E'
        })
        return
      }
      if (loginEmail === 'support-e2e@example.test') {
        await fulfillJson(route, {
          accessToken: 'support-e2e-token',
          refreshToken: 'support-e2e-refresh',
          email: loginEmail,
          displayName: 'Support E2E'
        })
        return
      }
      await fulfillJson(route, {
        accessToken: 'admin-e2e-token',
        refreshToken: 'admin-e2e-refresh',
        email: 'admin-e2e@example.test',
        displayName: 'Admin E2E'
      })
      return
    }

    if (method === 'POST' && path === '/api/auth/refresh') {
      await fulfillJson(route, {
        accessToken: 'admin-e2e-token-rotated',
        refreshToken: 'admin-e2e-refresh-rotated',
        email: 'admin-e2e@example.test',
        displayName: 'Admin E2E'
      })
      return
    }

    if (method === 'POST' && path === '/api/auth/logout') {
      await fulfillJson(route, logoutShouldFail ? { error: 'logout unavailable' } : { status: 'ok' }, logoutShouldFail ? 503 : 200)
      return
    }

    if (method === 'GET' && path === '/api/admin/session') {
      if (request.headers().authorization === 'Bearer user-e2e-token' || dashboardShouldDeny) {
        dashboardShouldDeny = false
        await fulfillJson(route, { error: 'forbidden' }, 403)
        return
      }
      const financeSession = request.headers().authorization === 'Bearer finance-e2e-token'
      const supportSession = request.headers().authorization === 'Bearer support-e2e-token'
      await fulfillJson(route, {
        userId: financeSession ? 'finance-user' : supportSession ? 'support-user' : 'admin-user',
        email: financeSession ? 'finance-e2e@example.test' : supportSession ? 'support-e2e@example.test' : 'admin-e2e@example.test',
        displayName: financeSession ? 'Finance E2E' : supportSession ? 'Support E2E' : 'Admin E2E',
        roles: [financeSession ? 'FinanceManager' : supportSession ? 'SupportAgent' : 'Admin'],
        capabilities: financeSession
          ? { ...fullAdminCapabilities, adminWrite: false, supportRead: false, supportWrite: false, provisioningManage: false, vpnManage: false, botManage: false, settingsManage: false }
          : supportSession
            ? { ...fullAdminCapabilities, adminWrite: false, financeRead: false, financeWrite: false, provisioningManage: false, vpnManage: false, botManage: false, settingsManage: false }
          : fullAdminCapabilities
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/dashboard/summary') {
      const financeVisible = request.headers().authorization !== 'Bearer support-e2e-token'
      const supportVisible = request.headers().authorization !== 'Bearer finance-e2e-token'
      await fulfillJson(route, {
        totalUsers: 4,
        telegramUsers: 2,
        activeSubscriptions: 3,
        expiringSubscriptions: 1,
        paidOrders: financeVisible ? 5 : 0,
        pendingOrders: financeVisible ? 1 : 0,
        failedPayments: financeVisible ? 1 : 0,
        recentPayments: financeVisible ? 2 : 0,
        recentOrders: financeVisible ? 3 : 0,
        vpnAccessesCount: 3,
        vpnNodesCount: 1,
        healthyVpnNodes: 1,
        vpnPanelsCount: panels.length,
        healthyVpnPanels: panels.filter((item) => item.healthStatus === 'Healthy').length,
        supportConversationsCount: supportVisible ? 1 : 0,
        openSupportConversations: supportVisible ? 1 : 0,
        provisioningErrors: 0,
        productionReadiness: {
          isReady: true,
          status: 'Ready',
          checks: [
            ...(financeVisible ? [{ key: 'payments', label: 'Платежи', status: 'Ready', message: 'Sandbox провайдер готов.', category: 'Sales', severity: 'critical', actionHref: '#payments', actionLabel: 'Открыть оплаты' }] : []),
            { key: 'vpn', label: 'VPN', status: 'Ready', message: 'Панель и inbound доступны.', category: 'VPN', severity: 'critical', actionHref: '#panels', actionLabel: 'Открыть панели' },
            { key: 'unsafe-action', label: 'Некорректное действие', status: 'Blocked', message: 'Ссылка должна быть отклонена.', category: 'Security', severity: 'warning', actionHref: 'javascript:window.__adminReadinessLinkExecuted=true', actionLabel: 'Опасная команда' }
          ]
        },
        generatedAt: now
      })
      return
    }

    if (method === 'GET' && path === '/api/app-version/latest') {
      const latestRelease = releases[releases.length - 1] ?? null
      await fulfillJson(route, { currentVersion: latestRelease?.version ?? null, latestRelease, seenByCurrentUser: true })
      return
    }

    if (method === 'GET' && path === '/api/app-version/history') {
      await fulfillJson(route, releases)
      return
    }

    if (method === 'GET' && path === '/api/admin/audit-logs') {
      const financeVisible = request.headers().authorization !== 'Bearer support-e2e-token'
      const supportVisible = request.headers().authorization !== 'Bearer finance-e2e-token'
      const botVisible = request.headers().authorization !== 'Bearer finance-e2e-token' && request.headers().authorization !== 'Bearer support-e2e-token'
      await fulfillJson(route, [
        { id: 'audit-login', actorType: 'Admin', actorId: 'admin-user', action: 'auth.login', entityType: 'Auth', entityId: 'admin-user', beforeJson: '{}', afterJson: '{"scope":"common"}', ip: '127.0.0.1', userAgent: 'Playwright', createdAt: now },
        ...(financeVisible ? [{ id: 'audit-payment', actorType: 'System', actorId: 'payments', action: 'payment.status.changed', entityType: 'PaymentAttempt', entityId: 'payment-e2e', beforeJson: '{}', afterJson: '{"scope":"finance"}', ip: '', userAgent: '', createdAt: now }] : []),
        ...(supportVisible ? [{ id: 'audit-support', actorType: 'Admin', actorId: 'support', action: 'support.reply', entityType: 'SupportConversation', entityId: 'support-e2e', beforeJson: '{}', afterJson: '{"scope":"support"}', ip: '127.0.0.1', userAgent: 'Playwright', createdAt: now }] : []),
        ...(botVisible ? [{ id: 'audit-bot', actorType: 'Admin', actorId: 'bot', action: 'telegram_bot.settings.update', entityType: 'TelegramBotSettings', entityId: 'bot-settings', beforeJson: '{}', afterJson: '{"scope":"bot"}', ip: '127.0.0.1', userAgent: 'Playwright', createdAt: now }] : [])
      ])
      return
    }

    if (method === 'GET' && path === '/api/admin/notification-deliveries') {
      await fulfillJson(route, [{
        id: 'notification-e2e',
        userId: 'user-e2e',
        templateKey: 'password_reset_requested',
        channel: 'Email',
        maskedToAddress: 'cl***@example.test',
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

    if (method === 'GET' && path === '/api/admin/users') {
      await fulfillJson(route, invalidUsersResponse ? [{}] : [{ id: 'user-e2e', email: 'client@example.test', displayName: 'Client E2E', rolesCsv: 'User', status: 'Active', isBlocked: false, preferredLanguage: 'ru', referralCode: 'E2E', authSource: 'Local', emailConfirmed: true, lastLoginAt: now, telegramRegistrationCompletedAt: null, createdAt: now, updatedAt: now }])
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
      await fulfillJson(route, [
        { id: 'access-e2e', subscriptionId: 'sub-e2e', subscriptionStatus: 'Active', isTerminal: false, userId: 'user-e2e', providerType: 'X3UI', providerAccessId: 'client-e2e', serverId: 'server-eu', serverName: 'EU Sandbox', accessUri: 'vless://admin-e2e@example.test', qrCodePayload: 'vless://admin-e2e@example.test', qrCodePath: 'qr://admin', configPath: 'config://admin', status: 'Active', issuedAt: now, expiryDate: '2026-07-13T07:00:00Z', disabledAt: null, lastSyncedAt: now, revision: 1, history: [], createdAt: now, updatedAt: now },
        { id: 'access-revoked', subscriptionId: 'sub-revoked', subscriptionStatus: 'Expired', isTerminal: true, userId: 'user-e2e', providerType: 'X3UI', providerAccessId: 'client-revoked', serverId: 'server-eu', serverName: 'EU Sandbox', accessUri: 'vless://revoked-admin-secret@example.test', qrCodePayload: 'vless://revoked-admin-secret@example.test', qrCodePath: 'qr://revoked-admin-secret', configPath: 'config://revoked-admin-secret', status: 'Revoked', issuedAt: now, expiryDate: now, disabledAt: now, lastSyncedAt: now, revision: 2, history: [{ id: 'history-revoked', accessCredentialId: 'access-revoked', subscriptionId: 'sub-revoked', eventType: 'AccessRevoked', oldValueJson: '{}', newValueJson: '{}', createdAt: now }], createdAt: now, updatedAt: now },
        { id: 'access-cancelled-stale', subscriptionId: 'sub-cancelled', subscriptionStatus: 'Cancelled', isTerminal: true, userId: 'user-e2e', providerType: 'X3UI', providerAccessId: 'client-cancelled-stale', serverId: 'server-eu', serverName: 'EU Sandbox', accessUri: 'vless://cancelled-access-stale-secret@example.test', qrCodePayload: 'vless://cancelled-access-stale-secret@example.test', qrCodePath: 'qr://cancelled-access-stale-secret', configPath: 'config://cancelled-access-stale-secret', status: 'Active', issuedAt: now, expiryDate: now, disabledAt: null, lastSyncedAt: now, revision: 1, history: [], createdAt: now, updatedAt: now }
      ])
      return
    }

    if (method === 'GET' && path === '/api/admin/access-credentials/access-e2e/qr') {
      await route.fulfill({
        status: 200,
        headers: { ...corsHeaders, 'content-type': 'image/svg+xml; charset=utf-8' },
        body: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10" /></svg>'
      })
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
      await fulfillJson(route, invalidPaymentProviderAccountsResponse ? [{}] : providers)
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
      await fulfillJson(route, [{ id: 'support-e2e', userId: 'user-e2e', telegramUserId: null, channel: 'web', status: 'open', subject: 'Проверка доступа', assignedToUserId: null, internalNote: '', revision: 0, closedAt: null, createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/support/conversations/support-e2e/messages') {
      await fulfillJson(route, [{ id: 'support-message-e2e', supportConversationId: 'support-e2e', userId: 'user-e2e', telegramUserId: null, direction: 'inbound', text: 'Нужна проверка доступа', attachmentsJson: '[]', isInternalNote: false, createdAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/tariffs') {
      await fulfillJson(route, invalidTariffsResponse ? [{}] : tariffs)
      return
    }

    if (method === 'POST' && path === '/api/admin/tariffs') {
      const created = tariff({ ...(body as Record<string, unknown>), id: 'tariff-created-e2e', createdAt: now, updatedAt: now })
      tariffs.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    if (method === 'GET' && path === '/api/admin/referral-programs') {
      await fulfillJson(route, referralPrograms)
      return
    }

    if (method === 'GET' && path === '/api/admin/referrals') {
      await fulfillJson(route, [{ id: 'reward-e2e', userId: 'user-e2e', sourceUserId: 'source-e2e', referralProgramId: 'referral-program-e2e', type: 'bonus-days', status: 'Approved', value: 7, currencyOrUnit: 'days', processedAt: now, createdAt: now }])
      return
    }

    if (method === 'POST' && path === '/api/admin/referral-programs') {
      const created = referralProgram({ ...(body as Record<string, unknown>), id: 'referral-program-created-e2e', createdAt: now, updatedAt: now })
      referralPrograms.push(created)
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
      const releaseBody = body as Record<string, unknown>
      const created = release({
        ...releaseBody,
        id: 'release-created-e2e',
        items: Array.isArray(releaseBody.items)
          ? releaseBody.items.map((item, index) => ({ ...(item as Record<string, unknown>), id: `release-created-item-${index + 1}` }))
          : [],
        createdAt: now,
        updatedAt: now
      })
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
      await fulfillJson(route, invalidServersResponse ? [{}] : [vpnServer()])
      return
    }

    if (method === 'GET' && path === '/api/admin/provisioning-runs') {
      await fulfillJson(route, invalidProvisioningRunsResponse ? [{}] : [provisioningRun()])
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels') {
      await fulfillJson(route, invalidVpnPanelsResponse ? [{}] : panels)
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
      if (delayNextVpnPanelInboundsResponse) {
        delayNextVpnPanelInboundsResponse = false
        await new Promise((resolve) => setTimeout(resolve, 1200))
      }
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

    if (method === 'POST' && path === '/api/admin/telegram-bot/settings/test') {
      if (delayNextBotSettingsCheckResponse) {
        delayNextBotSettingsCheckResponse = false
        await new Promise((resolve) => setTimeout(resolve, 1200))
      }
      await fulfillJson(route, { isReady: true, status: 'ready', requiredActions: [], warnings: [], checkedAt: now })
      return
    }

    if (method === 'GET' && path === '/api/admin/telegram-bot/settings') {
      await fulfillJson(route, invalidBotSettingsResponse ? {} : telegramBotSettings())
      return
    }

    await fulfillJson(route, { error: `Unhandled ${method} ${path}` }, 404)
  })

  return {
    getLastRequest: (path: string, method = 'POST') =>
      requests.findLast((item) => item.method === method && item.path === path),
    denyNextDashboard: () => { dashboardShouldDeny = true },
    returnInvalidUsersResponse: () => { invalidUsersResponse = true },
    returnInvalidPaymentProviderAccountsResponse: () => { invalidPaymentProviderAccountsResponse = true },
    returnInvalidTariffsResponse: () => { invalidTariffsResponse = true },
    returnInvalidVpnPanelsResponse: () => { invalidVpnPanelsResponse = true },
    returnInvalidServersResponse: () => { invalidServersResponse = true },
    returnInvalidProvisioningRunsResponse: () => { invalidProvisioningRunsResponse = true },
    returnInvalidBotSettingsResponse: () => { invalidBotSettingsResponse = true },
    delayNextVpnPanelInbounds: () => { delayNextVpnPanelInboundsResponse = true },
    delayNextBotSettingsCheck: () => { delayNextBotSettingsCheckResponse = true },
    failLogout: () => { logoutShouldFail = true }
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
  test.setTimeout(90_000)
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
  await page.locator('.admin-login-form input[type="email"]').fill('user-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('UserPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()
  await expect(page.getByText('У этой учетной записи нет доступа к админ-панели. Войдите с административной ролью.')).toBeVisible()
  await expect(page.locator('.admin-shell')).toBeHidden()
  expect(api.getLastRequest('/api/auth/logout')?.body).toEqual({ refreshToken: 'user-e2e-refresh' })
  expect(api.getLastRequest('/api/auth/logout')?.authorization).toBe('Bearer user-e2e-token')
  expect(await page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  const expectedDeniedResponse = failedResponses.findIndex((item) => item.includes('403') && item.includes('/api/admin/session'))
  expect(expectedDeniedResponse).toBeGreaterThanOrEqual(0)
  failedResponses.splice(expectedDeniedResponse, 1)
  const expectedDeniedConsoleError = consoleErrors.findIndex((item) => item.includes('403'))
  if (expectedDeniedConsoleError >= 0) consoleErrors.splice(expectedDeniedConsoleError, 1)

  await page.locator('.admin-login-form input[type="email"]').fill('admin-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('AdminPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()

  await expect(page.getByRole('heading', { name: 'Дашборд' })).toBeVisible()
  await expect(page.getByText('Готовность к live-продажам')).toBeVisible()
  await expect(page.getByText('Sandbox провайдер готов.')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Открыть оплаты' })).toHaveAttribute('href', '#payments')
  const unsafeReadinessCheck = page.locator('.list-item').filter({ hasText: 'Некорректное действие' })
  await expect(unsafeReadinessCheck).toBeVisible()
  await expect(unsafeReadinessCheck.getByRole('link')).toHaveCount(0)
  await expect(page.locator('a[href^="javascript:"]')).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => (window as typeof window & { __adminReadinessLinkExecuted?: boolean }).__adminReadinessLinkExecuted ?? false)).toBe(false)

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

  await openAdminSection(page, 'Рефералы', 'referrals')
  const referralsPanel = page.locator('#referrals')
  await expect(referralsPanel.locator('.list-item').filter({ hasText: 'Бонусные дни' })).toBeVisible()
  await referralsPanel.getByLabel('Название').fill('Referral Playwright E2E')
  await referralsPanel.locator('form select').first().selectOption('active')
  const createReferralRequest = page.waitForRequest((request) => request.method() === 'POST' && new URL(request.url()).pathname === '/api/admin/referral-programs')
  await referralsPanel.getByRole('button', { name: 'Создать программу' }).click()
  const referralRequest = await createReferralRequest
  expect(referralRequest.postDataJSON()).toMatchObject({ name: 'Referral Playwright E2E', status: 'active' })
  await expect(referralsPanel.locator('strong').filter({ hasText: 'Referral Playwright E2E' })).toBeVisible()

  await openAdminSection(page, 'VPN-доступы', 'vpn')
  await expect(page.getByText('vless://admin-e2e@example.test')).toBeVisible()
  const activeAccessRow = page.locator('#vpn .list-item-vertical').filter({ hasText: 'EU Sandbox' })
  await activeAccessRow.getByRole('button', { name: 'Показать QR' }).click()
  await expect(activeAccessRow.getByRole('img', { name: 'QR-код доступа access-e2e' })).toBeVisible()
  await expect(activeAccessRow.locator('svg')).toHaveCount(0)
  const revokedAccessRow = page.locator('#vpn .list-item-vertical').filter({ hasText: 'Доступ отозван.' })
  await expect(revokedAccessRow.getByText('Доступ отозван. Ключ и provider-команды скрыты; доступна только история.')).toBeVisible()
  await expect(revokedAccessRow.getByRole('button')).toHaveCount(0)
  await expect(page.getByText('client-revoked')).toHaveCount(0)
  await expect(page.getByText('vless://revoked-admin-secret@example.test')).toHaveCount(0)
  const cancelledAccessRow = page.locator('#vpn .list-item-vertical').filter({ hasText: 'Родительская подписка отменена.' })
  await expect(cancelledAccessRow.getByText('Родительская подписка отменена. Ключ и provider-команды скрыты; доступна только история.')).toBeVisible()
  await expect(cancelledAccessRow.getByRole('button')).toHaveCount(0)
  await expect(page.getByText('client-cancelled-stale')).toHaveCount(0)
  await expect(page.getByText('vless://cancelled-access-stale-secret@example.test')).toHaveCount(0)

  await openAdminSection(page, 'Подписки', 'subscriptions')
  const subscriptionsPanel = page.locator('#subscriptions')
  const cancelledSubscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Отменённая подписка' })
  await expect(cancelledSubscriptionRow.getByText('Отменённая подписка является терминальной. Доступны только просмотр и история.')).toBeVisible()
  await expect(cancelledSubscriptionRow.getByRole('button')).toHaveCount(0)
  await expect(cancelledSubscriptionRow.getByRole('spinbutton')).toHaveCount(0)
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

  expect(await page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).toEqual({ access: 'admin-e2e-token', refresh: 'admin-e2e-refresh' })

  await page.getByRole('button', { name: 'Обновить сессию' }).click()
  await expect(page.getByText('Сессия администратора обновлена.')).toBeVisible()
  expect(api.getLastRequest('/api/auth/refresh')?.body).toEqual({ refreshToken: 'admin-e2e-refresh' })
  expect(await page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).toEqual({ access: 'admin-e2e-token-rotated', refresh: 'admin-e2e-refresh-rotated' })

  await page.getByRole('button', { name: 'Завершить сессию' }).click()
  await expect(page.getByRole('heading', { name: 'Вход администратора' })).toBeVisible()
  expect(api.getLastRequest('/api/auth/logout')?.body).toEqual({ refreshToken: 'admin-e2e-refresh-rotated' })
  expect(api.getLastRequest('/api/auth/logout')?.authorization).toBe('Bearer admin-e2e-token-rotated')
  expect(await page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  await page.locator('.admin-login-form input[type="password"]').fill('AdminPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()
  await expect(page.locator('.admin-shell')).toBeVisible()
  api.denyNextDashboard()
  await page.getByRole('button', { name: 'Обновить сессию' }).click()
  await expect(page.getByText('У этой учетной записи нет доступа к админ-панели. Войдите с административной ролью.')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Вход администратора' })).toBeVisible()
  expect(api.getLastRequest('/api/auth/logout')?.body).toEqual({ refreshToken: 'admin-e2e-refresh-rotated' })
  expect(await page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  const expectedRefreshDeniedResponse = failedResponses.findIndex((item) => item.includes('403') && item.includes('/api/admin/session'))
  expect(expectedRefreshDeniedResponse).toBeGreaterThanOrEqual(0)
  failedResponses.splice(expectedRefreshDeniedResponse, 1)
  const expectedRefreshDeniedConsoleError = consoleErrors.findIndex((item) => item.includes('403'))
  if (expectedRefreshDeniedConsoleError >= 0) consoleErrors.splice(expectedRefreshDeniedConsoleError, 1)

  await page.locator('.admin-login-form input[type="password"]').fill('AdminPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()
  await expect(page.locator('.admin-shell')).toBeVisible()

  await openAdminSection(page, 'Серверы', 'nodes')
  const staleNodesSection = page.locator('#nodes')
  await staleNodesSection.getByRole('button', { name: 'Редактировать' }).click()
  await expect(staleNodesSection.getByRole('heading', { name: 'Редактировать VPN-сервер' })).toBeVisible()

  await openAdminSection(page, 'Подготовка VPS', 'provisioning')
  const staleProvisioningSection = page.locator('#provisioning')
  await expect(staleProvisioningSection.getByText('VPS precheck ready.', { exact: true })).toBeVisible()

  await openAdminSection(page, 'Telegram-бот', 'bot')
  const staleBotSection = page.locator('#bot')
  await staleBotSection.getByRole('button', { name: 'Проверить подключение' }).click()
  await expect(staleBotSection.getByRole('status').filter({ hasText: 'Проверка подключения' })).toBeVisible()

  await openAdminSection(page, '3x-ui панели', 'panels')
  const stalePanelsSection = page.locator('#panels')
  const panelSelect = stalePanelsSection.getByRole('combobox', { name: 'Панель' })
  await panelSelect.selectOption('')
  api.delayNextVpnPanelInbounds()
  const delayedDetailsRequest = page.waitForRequest((request) => request.method() === 'GET' && request.url().endsWith('/api/admin/vpn-panels/panel-eu/inbounds'))
  await panelSelect.selectOption('panel-eu')
  await delayedDetailsRequest
  await openAdminSection(page, 'Telegram-бот', 'bot')
  api.delayNextBotSettingsCheck()
  const delayedBotCheckRequest = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith('/api/admin/telegram-bot/settings/test'))
  await staleBotSection.getByRole('button', { name: 'Проверить подключение' }).click()
  await delayedBotCheckRequest
  api.returnInvalidServersResponse()
  api.returnInvalidProvisioningRunsResponse()
  api.returnInvalidBotSettingsResponse()
  api.returnInvalidVpnPanelsResponse()
  await page.getByRole('button', { name: 'Обновить данные' }).click()
  await expect(page.locator('.code-block').filter({ hasText: 'servers:' })).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await expect(page.locator('.code-block').filter({ hasText: 'подготовка серверов:' })).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await expect(page.locator('.code-block').filter({ hasText: 'настройки Telegram-бота:' })).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await expect(page.locator('.code-block').filter({ hasText: 'VPN-панели:' })).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await openAdminSection(page, '3x-ui панели', 'panels')
  await expect(stalePanelsSection.getByText('3x-ui панели не добавлены')).toBeVisible()
  await page.waitForTimeout(1400)
  await expect(stalePanelsSection.getByText('EU 3x-ui Sandbox', { exact: true })).toHaveCount(0)
  await expect(stalePanelsSection.getByText('default-vless', { exact: true })).toHaveCount(0)
  await expect(stalePanelsSection.getByText('client@example.test', { exact: true })).toHaveCount(0)
  await expect(stalePanelsSection.getByText('Remote panel sync failed.', { exact: true })).toHaveCount(0)
  await expect(stalePanelsSection.getByRole('combobox', { name: 'Панель' })).toHaveValue('')

  await openAdminSection(page, 'Серверы', 'nodes')
  await expect(staleNodesSection.getByText('VPN-серверы не добавлены')).toBeVisible()
  await expect(staleNodesSection.getByText('EU Sandbox', { exact: true })).toHaveCount(0)
  await expect(staleNodesSection.getByRole('heading', { name: 'Добавить VPN-сервер' })).toBeVisible()
  await expect(staleNodesSection.getByLabel('Название')).toHaveValue('')

  await openAdminSection(page, 'Подготовка VPS', 'provisioning')
  await expect(staleProvisioningSection.getByText('Запусков подготовки нет')).toBeVisible()
  await expect(staleProvisioningSection.getByText('VPS precheck ready.', { exact: true })).toHaveCount(0)

  await openAdminSection(page, 'Telegram-бот', 'bot')
  await expect(staleBotSection.getByText('@не настроен', { exact: true })).toBeVisible()
  await expect(staleBotSection.getByRole('status').filter({ hasText: 'Проверка подключения' })).toHaveCount(0)
  await expect(staleBotSection.getByLabel('Username публичного бота')).toHaveValue('')

  api.returnInvalidTariffsResponse()
  await page.getByRole('button', { name: 'Обновить данные' }).click()
  await expect(page.locator('.code-block').filter({ hasText: 'tariffs:' })).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await openAdminSection(page, 'Тарифы', 'tariffs')
  await expect(page.getByText('Тарифов нет')).toBeVisible()
  await expect(page.locator('#tariffs').getByText('Admin Pro 30', { exact: true })).toHaveCount(0)
  await expect(page.locator('#tariffs').getByText('E2E Premium 45', { exact: true })).toHaveCount(0)

  api.returnInvalidPaymentProviderAccountsResponse()
  await page.getByRole('button', { name: 'Обновить данные' }).click()
  await expect(page.locator('.code-block').filter({ hasText: 'способы оплаты:' })).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await openAdminSection(page, 'Оплаты', 'payments')
  await expect(page.getByText('Способы оплаты не настроены')).toBeVisible()
  await expect(page.locator('#payments').getByText('YooKassa sandbox', { exact: true })).toHaveCount(0)

  api.returnInvalidUsersResponse()
  await page.getByRole('button', { name: 'Обновить данные' }).click()
  await expect(page.locator('.code-block').filter({ hasText: 'users:' })).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await openAdminSection(page, 'Пользователи', 'users')
  await expect(page.getByText('Пользователи не найдены')).toBeVisible()
  await expect(page.getByText('Выберите пользователя.')).toBeVisible()
  await expect(page.locator('#users').getByText('client@example.test', { exact: true })).toHaveCount(0)

  api.failLogout()
  await page.getByRole('button', { name: 'Завершить сессию' }).click()
  await expect(page.getByText('Локальная сессия завершена, но отзыв серверной сессии не подтверждён. На чужом устройстве измените пароль из доверенного браузера.')).toBeVisible()
  expect(await page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  const expectedLogoutFailure = failedResponses.findIndex((item) => item.includes('503') && item.includes('/api/auth/logout'))
  expect(expectedLogoutFailure).toBeGreaterThanOrEqual(0)
  failedResponses.splice(expectedLogoutFailure, 1)
  const expectedLogoutConsoleError = consoleErrors.findIndex((item) => item.includes('503'))
  if (expectedLogoutConsoleError >= 0) consoleErrors.splice(expectedLogoutConsoleError, 1)
  expect(failedResponses).toEqual([])
  if (testInfo.project.name.startsWith('mobile-')) {
    await page.screenshot({ path: testInfo.outputPath('admin-mobile.png'), fullPage: true })
  }

  expect(consoleErrors).toEqual([])
})

test('finance role loads only permitted data and keeps common sections read-only', async ({ page }) => {
  const failedResponses: string[] = []
  page.on('response', (response) => {
    if (response.status() >= 400) failedResponses.push(`${response.status()} ${response.url()}`)
  })
  const api = await mockAdminApi(page)

  await page.goto('http://127.0.0.1:5295/')
  await page.locator('.admin-login-form input[type="email"]').fill('finance-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('FinancePassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()

  await expect(page.locator('.admin-shell')).toBeVisible()
  await expect(page.locator('.admin-section-select option[value="payments"]')).toHaveCount(1)
  await expect(page.locator('.admin-section-select option[value="support"]')).toHaveCount(0)
  await expect(page.locator('.admin-section-select option[value="bot"]')).toHaveCount(0)
  expect(api.getLastRequest('/api/admin/payments', 'GET')).toBeTruthy()
  expect(api.getLastRequest('/api/admin/support/conversations', 'GET')).toBeUndefined()
  expect(api.getLastRequest('/api/admin/telegram-bot/settings', 'GET')).toBeUndefined()

  await openAdminSection(page, 'Аудит', 'audit')
  await expect(page.getByText('payment.status.changed', { exact: true })).toBeVisible()
  await expect(page.getByText('support.reply', { exact: true })).toHaveCount(0)
  await expect(page.getByText('telegram_bot.settings.update', { exact: true })).toHaveCount(0)
  await expect(page.getByText('Изменения платежных провайдеров и ротация секретов')).toBeVisible()
  await expect(page.getByText('Ответы, заметки и статусы обращений')).toHaveCount(0)

  await openAdminSection(page, 'Тарифы', 'tariffs')
  await expect(page.getByText('Только просмотр', { exact: true })).toBeVisible()
  await expect(page.locator('#tariffs form').first()).toBeHidden()
  await expect(page.locator('#tariffs').getByRole('button', { name: 'Редактировать' })).toHaveCount(0)

  await openAdminSection(page, 'Оплаты', 'payments')
  await expect(page.locator('#payments form').first()).toBeVisible()
  await expect(page.getByText('Только просмотр', { exact: true })).toHaveCount(0)
  expect(failedResponses).toEqual([])
})

test('support role dashboard hides finance data and keeps support queue visible', async ({ page }) => {
  const failedResponses: string[] = []
  page.on('response', (response) => {
    if (response.status() >= 400) failedResponses.push(`${response.status()} ${response.url()}`)
  })
  const api = await mockAdminApi(page)

  await page.goto('http://127.0.0.1:5295/')
  await page.locator('.admin-login-form input[type="email"]').fill('support-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('SupportPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()

  await expect(page.locator('.admin-shell')).toBeVisible()
  await expect(page.locator('.admin-section-select option[value="support"]')).toHaveCount(1)
  await expect(page.locator('.admin-section-select option[value="payments"]')).toHaveCount(0)
  await expect(page.getByText('Оплачено / ожидает')).toHaveCount(0)
  await expect(page.getByText('Неуспешные платежи')).toHaveCount(0)
  await expect(page.getByText('Свежие платежи / заказы')).toHaveCount(0)
  await expect(page.getByText('Последние заказы')).toHaveCount(0)
  await expect(page.getByText('Очередь поддержки')).toBeVisible()
  await expect(page.getByText('Готовность инфраструктуры')).toBeVisible()
  await expect(page.getByText('Проверка доступа · tg:—', { exact: true })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Открыть оплаты' })).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(api.getLastRequest('/api/admin/payments', 'GET')).toBeUndefined()
  expect(api.getLastRequest('/api/admin/orders', 'GET')).toBeUndefined()
  expect(api.getLastRequest('/api/admin/support/conversations', 'GET')).toBeTruthy()

  await openAdminSection(page, 'Аудит', 'audit')
  await expect(page.getByText('auth.login', { exact: true })).toBeVisible()
  await expect(page.getByText('support.reply', { exact: true })).toBeVisible()
  await expect(page.getByText('payment.status.changed', { exact: true })).toHaveCount(0)
  await expect(page.getByText('telegram_bot.settings.update', { exact: true })).toHaveCount(0)
  await expect(page.getByText('Ответы, заметки и статусы обращений')).toBeVisible()
  await expect(page.getByText('Изменения платежных провайдеров и ротация секретов')).toHaveCount(0)
  expect(failedResponses).toEqual([])
})
