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

function adminUser(id: string, displayName: string, email: string) {
  return {
    id,
    email,
    displayName,
    rolesCsv: 'User',
    status: 'Active',
    isBlocked: false,
    preferredLanguage: 'ru',
    referralCode: `REF-${id}`,
    authSource: 'Local',
    emailConfirmed: true,
    lastLoginAt: now,
    telegramRegistrationCompletedAt: null,
    createdAt: now,
    updatedAt: now
  }
}

function adminUserOverview(user: ReturnType<typeof adminUser>) {
  return {
    user,
    telegramAccounts: [],
    orders: [],
    payments: [],
    subscriptions: [],
    accessCredentials: [],
    supportConversations: []
  }
}

function adminSupportConversation(id: string, userId: string, subject: string) {
  return {
    id,
    userId,
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

function adminSupportMessage(id: string, conversationId: string, userId: string, text: string) {
  return {
    id,
    supportConversationId: conversationId,
    userId,
    telegramUserId: null,
    direction: 'inbound',
    text,
    attachmentsJson: '[]',
    isInternalNote: false,
    createdAt: now
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

function faqItem(overrides: Record<string, unknown> = {}) {
  return {
    id: 'faq-created-e2e',
    question: 'Как проверить управляемый FAQ?',
    answer: 'Создать, изменить и удалить запись через админку.',
    category: 'E2E',
    isActive: true,
    showOnHome: true,
    showOnFaqPage: true,
    sortOrder: 10,
    createdAt: now,
    updatedAt: now,
    ...overrides
  }
}

function siteContentBlock(overrides: Record<string, unknown> = {}) {
  return {
    id: 'content-created-e2e',
    key: 'home.e2e.title',
    value: 'Управляемый блок E2E',
    group: 'home',
    label: 'Заголовок E2E',
    description: 'Проверка CRUD контента через браузер.',
    inputType: 'text',
    isActive: true,
    sortOrder: 90,
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

function provisioningCommand(serverId: string, runId: string, overrides: Record<string, unknown> = {}) {
  return {
    serverId,
    runId,
    status: 'queued',
    dryRun: true,
    mode: 'dry-run',
    modeTitle: 'Dry-run precheck',
    riskLevel: 'safe',
    liveDeployAllowed: false,
    nextAction: 'Проверьте precheck перед validation deploy.',
    operatorWarning: 'Dry-run не меняет VPS.',
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
  const expiredAdminAccessTokens = new Set<string>()
  let failNextAdminSessionStatus: number | null = null
  let refreshFailureStatus: number | null = null
  let delayNextRefresh = false
  let delayedRefreshReleased = false
  let releaseDelayedRefresh: (() => void) | null = null
  let invalidUsersResponse = false
  let emptyAuditLogsResponse = false
  let invalidPaymentProviderAccountsResponse = false
  let invalidTariffsResponse = false
  let invalidVpnPanelsResponse = false
  let invalidServersResponse = false
  let invalidProvisioningRunsResponse = false
  let invalidBotSettingsResponse = false
  let botSettings = telegramBotSettings()
  let delayNextVpnPanelInboundsResponse = false
  let delayNextBotSettingsCheckResponse = false
  let users = [adminUser('user-e2e', 'Client E2E', 'client@example.test')]
  let userOverviews = new Map(users.map((item) => [item.id, adminUserOverview(item)]))
  let supportConversations = [adminSupportConversation('support-e2e', 'user-e2e', 'Проверка доступа')]
  let supportMessages = new Map([
    ['support-e2e', [adminSupportMessage('support-message-e2e', 'support-e2e', 'user-e2e', 'Нужна проверка доступа')]]
  ])
  let delayedUserOverviewId: string | null = null
  let releaseDelayedUserOverview: (() => void) | null = null
  let delayedSupportMessagesId: string | null = null
  let releaseDelayedSupportMessages: (() => void) | null = null
  let delayNextTariffCreateResponse = false
  let releaseDelayedTariffCreate: (() => void) | null = null
  const providers = [paymentProviderAccount()]
  const tariffs = [tariff()]
  const referralPrograms = [referralProgram()]
  const releases = [release()]
  const scenarios = [workScenario()]
  const faqEntries: Array<Record<string, unknown>> = []
  const siteContentBlocks: Array<Record<string, unknown>> = []
  const servers = [vpnServer()]
  const provisioningRuns = [provisioningRun()]
  const panels = [
    vpnPanel(),
    vpnPanel({
      id: 'panel-us',
      name: 'US 3x-ui Sandbox',
      baseUrl: 'https://panel-us.example.test',
      region: 'US',
      usedCapacity: 4,
      version: '2.4.9',
      lastError: ''
    })
  ]
  const subscriptions: Array<Record<string, unknown>> = [
    { id: 'sub-e2e', userId: 'user-e2e', tariffId: 'tariff-admin-pro', tariffName: 'Admin Pro 30', status: 'Active', startAt: now, endAt: '2026-07-13T07:00:00Z', gracePeriodEndAt: null, autoRenewFlag: false, sourceChannel: 'Web', currentServerId: 'server-eu', currentAccessId: 'access-e2e', lastPaymentId: 'payment-e2e', renewalCount: 0, blockReason: null, suspendedAt: null, cancelledAt: null, lifecycleAttemptCount: 0, lifecycleProcessingStartedAt: null, lifecycleLeaseExpiresAt: null, lifecycleNextAttemptAt: null, lifecycleLastError: null, accessUri: 'vless://admin-e2e@example.test', qrCodePath: 'qr://admin', configPath: 'config://admin', nodeName: 'EU Sandbox', createdAt: now, updatedAt: now },
    { id: 'sub-cancelled', userId: 'user-e2e', tariffId: 'tariff-admin-pro', tariffName: 'Отменённая подписка', status: 'Cancelled', startAt: '2026-05-01T00:00:00Z', endAt: now, cancelledAt: now, gracePeriodEndAt: null, autoRenewFlag: false, sourceChannel: 'Web', currentServerId: 'server-eu', currentAccessId: 'access-revoked', lastPaymentId: 'payment-old', renewalCount: 0, blockReason: null, suspendedAt: null, lifecycleAttemptCount: 0, lifecycleProcessingStartedAt: null, lifecycleLeaseExpiresAt: null, lifecycleNextAttemptAt: null, lifecycleLastError: null, accessUri: 'vless://cancelled-stale-secret@example.test', qrCodePath: 'qr://cancelled-stale-secret', configPath: 'config://cancelled-stale-secret', nodeName: 'EU Sandbox', createdAt: now, updatedAt: now }
  ]
  const accessCredentials: Array<Record<string, unknown>> = [
    { id: 'access-e2e', subscriptionId: 'sub-e2e', subscriptionStatus: 'Active', isTerminal: false, userId: 'user-e2e', providerType: 'X3UI', providerAccessId: 'client-e2e', serverId: 'server-eu', serverName: 'EU Sandbox', accessUri: 'vless://admin-e2e@example.test', qrCodePayload: 'vless://admin-e2e@example.test', qrCodePath: 'qr://admin', configPath: 'config://admin', status: 'Active', issuedAt: now, expiryDate: '2026-07-13T07:00:00Z', disabledAt: null, lastSyncedAt: now, revision: 1, history: [], createdAt: now, updatedAt: now },
    { id: 'access-revoked', subscriptionId: 'sub-revoked', subscriptionStatus: 'Expired', isTerminal: true, userId: 'user-e2e', providerType: 'X3UI', providerAccessId: 'client-revoked', serverId: 'server-eu', serverName: 'EU Sandbox', accessUri: 'vless://revoked-admin-secret@example.test', qrCodePayload: 'vless://revoked-admin-secret@example.test', qrCodePath: 'qr://revoked-admin-secret', configPath: 'config://revoked-admin-secret', status: 'Revoked', issuedAt: now, expiryDate: now, disabledAt: now, lastSyncedAt: now, revision: 2, history: [{ id: 'history-revoked', accessCredentialId: 'access-revoked', subscriptionId: 'sub-revoked', eventType: 'AccessRevoked', oldValueJson: '{}', newValueJson: '{}', createdAt: now }], createdAt: now, updatedAt: now },
    { id: 'access-cancelled-stale', subscriptionId: 'sub-cancelled', subscriptionStatus: 'Cancelled', isTerminal: true, userId: 'user-e2e', providerType: 'X3UI', providerAccessId: 'client-cancelled-stale', serverId: 'server-eu', serverName: 'EU Sandbox', accessUri: 'vless://cancelled-access-stale-secret@example.test', qrCodePayload: 'vless://cancelled-access-stale-secret@example.test', qrCodePath: 'qr://cancelled-access-stale-secret', configPath: 'config://cancelled-access-stale-secret', status: 'Active', issuedAt: now, expiryDate: now, disabledAt: null, lastSyncedAt: now, revision: 1, history: [], createdAt: now, updatedAt: now }
  ]
  const inbounds: Array<Record<string, unknown>> = [
    { id: 'inbound-default', vpnPanelId: 'panel-eu', externalInboundId: '1', name: 'default-vless', protocol: 'vless', port: 443, listen: '0.0.0.0', settingsJson: '{"clients":[]}', streamSettingsJson: '{"network":"tcp","security":"reality"}', sniffingJson: '{}', isDefault: true, isActive: true, capacity: 1000, usedCapacity: 12 },
    { id: 'inbound-backup', vpnPanelId: 'panel-eu', externalInboundId: '2', name: 'backup-vless', protocol: 'vless', port: 8443, listen: '0.0.0.0', settingsJson: '{"clients":[]}', streamSettingsJson: '{"network":"tcp","security":"reality"}', sniffingJson: '{}', isDefault: false, isActive: true, capacity: 20, usedCapacity: 3 },
    { id: 'inbound-us', vpnPanelId: 'panel-us', externalInboundId: '1', name: 'us-vless', protocol: 'vless', port: 9443, listen: '0.0.0.0', settingsJson: '{"clients":[]}', streamSettingsJson: '{"network":"tcp","security":"reality"}', sniffingJson: '{}', isDefault: true, isActive: true, capacity: 100, usedCapacity: 4 }
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
      if (expiredAdminAccessTokens.has(request.headers().authorization ?? '')) {
        await fulfillJson(route, { error: 'access_token_expired' }, 401)
        return
      }
      if (failNextAdminSessionStatus !== null) {
        const status = failNextAdminSessionStatus
        failNextAdminSessionStatus = null
        await fulfillJson(route, { error: 'admin_session_temporarily_unavailable' }, status)
        return
      }
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
      if (emptyAuditLogsResponse) {
        await fulfillJson(route, [])
        return
      }
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

    if (method === 'POST' && path === '/api/admin/notification-deliveries/notification-e2e/retry') {
      await fulfillJson(route, { id: 'notification-e2e', status: 'Pending', nextAttemptAt: now })
      return
    }

    if (method === 'GET' && path === '/api/admin/users') {
      await fulfillJson(route, invalidUsersResponse ? [{}] : users)
      return
    }

    const userOverviewMatch = path.match(/^\/api\/admin\/users\/([^/]+)\/overview$/)
    if (method === 'GET' && userOverviewMatch) {
      const userId = decodeURIComponent(userOverviewMatch[1])
      const response = userOverviews.get(userId)
      if (!response) {
        await fulfillJson(route, { error: 'user_not_found' }, 404)
        return
      }
      if (delayedUserOverviewId === userId) {
        delayedUserOverviewId = null
        await new Promise<void>((resolve) => { releaseDelayedUserOverview = resolve })
        releaseDelayedUserOverview = null
      }
      await fulfillJson(route, response)
      return
    }

    if (method === 'GET' && path === '/api/admin/subscriptions') {
      await fulfillJson(route, subscriptions)
      return
    }

    if (method === 'POST' && path === '/api/admin/subscriptions/sub-e2e/migrate') {
      const sourceNodeId = String(subscriptions[0].currentServerId ?? 'server-eu')
      const targetNodeId = typeof body === 'string' && body ? body : 'server-us'
      subscriptions[0] = { ...subscriptions[0], currentServerId: targetNodeId, nodeName: targetNodeId === 'server-us' ? 'US Sandbox' : targetNodeId, updatedAt: now }
      accessCredentials[0] = {
        ...accessCredentials[0],
        serverId: targetNodeId,
        serverName: targetNodeId === 'server-us' ? 'US Sandbox' : targetNodeId,
        lastSyncedAt: now,
        revision: Number(accessCredentials[0].revision ?? 0) + 1,
        updatedAt: now
      }
      await fulfillJson(route, { migrationJobId: 'migration-sub-e2e', subscriptionId: 'sub-e2e', sourceNodeId, targetNodeId, status: 'completed' })
      return
    }

    if (method === 'POST' && path === '/api/admin/subscriptions/sub-e2e/extend') {
      const days = Number((body as Record<string, unknown>)?.days ?? 0)
      const endAt = '2027-02-27T07:00:00Z'
      const gracePeriodEndAt = '2027-03-02T07:00:00Z'
      subscriptions[0] = {
        ...subscriptions[0],
        status: 'Active',
        endAt,
        gracePeriodEndAt,
        renewalCount: Number(subscriptions[0].renewalCount ?? 0) + 1,
        updatedAt: now
      }
      await fulfillJson(route, { id: 'sub-e2e', status: 'Active', endAt, gracePeriodEndAt, days })
      return
    }

    if (method === 'POST' && path === '/api/admin/subscriptions/sub-e2e/activate') {
      accessCredentials[0] = {
        ...accessCredentials[0],
        subscriptionStatus: 'Active',
        isTerminal: false,
        status: 'Active',
        disabledAt: null,
        lastSyncedAt: now,
        revision: Number(accessCredentials[0].revision ?? 0) + 1,
        updatedAt: now
      }
      subscriptions[0] = {
        ...subscriptions[0],
        status: 'Active',
        currentAccessId: 'access-e2e',
        blockReason: null,
        updatedAt: now
      }
      await fulfillJson(route, {
        id: 'sub-e2e',
        status: 'Active',
        endAt: subscriptions[0].endAt,
        currentAccessId: 'access-e2e',
        access: {
          id: 'access-e2e',
          status: 'Active',
          disabledAt: null,
          lastSyncedAt: now,
          revision: accessCredentials[0].revision,
          usedTrafficBytes: 0,
          message: 'activated'
        }
      })
      return
    }

    if (method === 'POST' && path === '/api/admin/subscriptions/sub-e2e/sync-access') {
      accessCredentials[0] = {
        ...accessCredentials[0],
        lastSyncedAt: now,
        revision: Number(accessCredentials[0].revision ?? 0) + 1,
        updatedAt: now
      }
      await fulfillJson(route, {
        id: 'sub-e2e',
        currentAccessId: 'access-e2e',
        access: { id: 'access-e2e', status: accessCredentials[0].status, disabledAt: accessCredentials[0].disabledAt, lastSyncedAt: now, revision: accessCredentials[0].revision, usedTrafficBytes: 0, message: 'synced' }
      })
      return
    }

    if (method === 'POST' && path === '/api/admin/subscriptions/sub-e2e/block') {
      subscriptions[0] = { ...subscriptions[0], status: 'Blocked', blockReason: 'manual_admin_action', updatedAt: now }
      accessCredentials[0] = { ...accessCredentials[0], subscriptionStatus: 'Blocked', status: 'Disabled', disabledAt: now, revision: Number(accessCredentials[0].revision ?? 0) + 1, updatedAt: now }
      await fulfillJson(route, { id: 'sub-e2e', status: 'Blocked', blockReason: 'manual_admin_action' })
      return
    }

    if (method === 'POST' && path === '/api/admin/subscriptions/sub-e2e/unblock') {
      subscriptions[0] = { ...subscriptions[0], status: 'Active', blockReason: null, updatedAt: now }
      accessCredentials[0] = { ...accessCredentials[0], subscriptionStatus: 'Active', status: 'Active', disabledAt: null, revision: Number(accessCredentials[0].revision ?? 0) + 1, updatedAt: now }
      await fulfillJson(route, { id: 'sub-e2e', status: 'Active' })
      return
    }

    if (method === 'POST' && path === '/api/admin/subscriptions/sub-e2e/cancel') {
      subscriptions[0] = { ...subscriptions[0], status: 'Cancelled', currentServerId: null, currentAccessId: null, accessUri: '', qrCodePath: '', configPath: '', cancelledAt: now, updatedAt: now }
      accessCredentials[0] = {
        ...accessCredentials[0],
        subscriptionStatus: 'Cancelled',
        isTerminal: true,
        providerAccessId: '',
        accessUri: '',
        qrCodePayload: '',
        qrCodePath: '',
        configPath: '',
        status: 'Revoked',
        disabledAt: now,
        revision: Number(accessCredentials[0].revision ?? 0) + 1,
        updatedAt: now
      }
      await fulfillJson(route, { id: 'sub-e2e', status: 'Cancelled', cancelledAt: now })
      return
    }

    if (method === 'GET' && path === '/api/admin/access-credentials') {
      await fulfillJson(route, accessCredentials)
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

    const accessActionMatch = path.match(/^\/api\/admin\/access-credentials\/access-e2e\/(enable|disable|sync|reset-traffic)$/)
    if (method === 'POST' && accessActionMatch) {
      const action = accessActionMatch[1]
      const status = action === 'disable' ? 'Disabled' : 'Active'
      const current = accessCredentials[0]
      const revision = Number(current.revision ?? 0) + 1
      accessCredentials[0] = {
        ...current,
        status,
        disabledAt: status === 'Disabled' ? now : null,
        lastSyncedAt: now,
        revision,
        updatedAt: now
      }
      await fulfillJson(route, {
        id: 'access-e2e',
        status,
        disabledAt: status === 'Disabled' ? now : null,
        lastSyncedAt: now,
        revision,
        usedTrafficBytes: 0,
        message: action
      })
      return
    }

    if (method === 'GET' && path === '/api/admin/orders') {
      await fulfillJson(route, [{ id: 'order-e2e', userId: 'user-e2e', userDisplayName: 'Client E2E', userEmail: 'client@example.test', tariffId: 'tariff-admin-pro', tariffName: 'Admin Pro 30', amount: 590, currency: 'RUB', status: 'PaymentReceived', type: 'NewSubscription', channel: 'Web', paymentProvider: 'YooKassa', checkoutSessionId: null, expiresAt: '2026-06-14T07:00:00Z', paidAt: now, isFirstPurchase: true, paymentAttemptsCount: 1, lastPaymentId: 'payment-e2e', lastPaymentStatus: 'Succeeded', lastPaymentProvider: 'YooKassa', linkedSubscriptionId: 'sub-e2e', createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'GET' && path === '/api/admin/payments') {
      await fulfillJson(route, [{ id: 'payment-e2e', orderId: 'order-e2e', userId: 'user-e2e', userDisplayName: 'Client E2E', provider: 'YooKassa', paymentProviderAccountId: 'provider-yookassa', providerMode: 'Sandbox', providerPaymentId: 'yk-admin-e2e', externalEventId: 'evt-admin-e2e', idempotencyKey: 'idem-admin-e2e', confirmationUrl: 'http://127.0.0.1:5295/payments/return', returnUrl: 'http://127.0.0.1:5295', amount: 590, currency: 'RUB', status: 'Succeeded', signatureValidated: true, isActivationProcessed: true, activationProcessedAt: now, paidAt: now, failedAt: null, refundedAt: null, refundedAmount: 0, statusReason: null, webhookEventsCount: 1, refundsCount: 0, refundSupported: true, canRefund: true, refundableAmount: 590, refundBlockers: [], createdAt: now, updatedAt: now }])
      return
    }

    if (method === 'POST' && (path === '/api/admin/payments/payment-e2e/recheck' || path === '/api/admin/orders/order-e2e/recheck-payment')) {
      await fulfillJson(route, { orderId: 'order-e2e', paymentId: 'payment-e2e', status: 'Succeeded', rawResponse: '{}', statusReason: null })
      return
    }

    if (method === 'POST' && path === '/api/admin/payments/payment-e2e/refund') {
      await fulfillJson(route, { id: 'refund-e2e', paymentAttemptId: 'payment-e2e', provider: 'YooKassa', providerRefundId: 'rf-e2e', status: 'Succeeded', amount: Number((body as Record<string, unknown>)?.amount ?? 0), currency: 'RUB', reason: String((body as Record<string, unknown>)?.reason ?? ''), createdAt: now, refundedAt: now })
      return
    }

    if (method === 'GET' && path === '/api/admin/payment-providers/accounts') {
      await fulfillJson(route, invalidPaymentProviderAccountsResponse ? [{}] : providers)
      return
    }

    if (method === 'POST' && path === '/api/admin/payment-providers/accounts') {
      const payload = body as Record<string, unknown>
      const account = paymentProviderAccount({
        id: 'provider-created-e2e',
        provider: payload.provider,
        mode: payload.mode,
        name: payload.name,
        publicName: payload.publicName,
        isEnabled: payload.isEnabled,
        isDefault: payload.isDefault,
        shopId: payload.shopId,
        apiBaseUrl: payload.apiBaseUrl,
        returnUrl: payload.returnUrl,
        webhookUrl: payload.webhookUrl,
        hasSecretKey: Boolean(payload.secretKey),
        hasWebhookSecret: Boolean(payload.webhookSecret),
        useWebhookIpAllowList: payload.useWebhookIpAllowList,
        allowedWebhookIpRangesCsv: payload.allowedWebhookIpRangesCsv,
        extraSettingsJson: payload.extraSettingsJson,
        healthStatus: 'Unknown',
        isPubliclyAvailable: Boolean(payload.isEnabled) && payload.mode !== 'Disabled'
      })
      providers.push(account)
      await fulfillJson(route, account)
      return
    }

    const providerMutationMatch = path.match(/^\/api\/admin\/payment-providers\/accounts\/([^/]+)$/)
    if (method === 'PATCH' && providerMutationMatch) {
      const accountId = decodeURIComponent(providerMutationMatch[1])
      const index = providers.findIndex((account) => account.id === accountId)
      const current = providers[index]
      const payload = body as Record<string, unknown>
      const account = paymentProviderAccount({
        ...current,
        provider: payload.provider,
        mode: payload.mode,
        name: payload.name,
        publicName: payload.publicName,
        isEnabled: payload.isEnabled,
        isDefault: payload.isDefault,
        shopId: payload.shopId,
        apiBaseUrl: payload.apiBaseUrl,
        returnUrl: payload.returnUrl,
        webhookUrl: payload.webhookUrl,
        hasSecretKey: Boolean(payload.secretKey) || Boolean(current?.hasSecretKey),
        hasWebhookSecret: Boolean(payload.webhookSecret) || Boolean(current?.hasWebhookSecret),
        useWebhookIpAllowList: payload.useWebhookIpAllowList,
        allowedWebhookIpRangesCsv: payload.allowedWebhookIpRangesCsv,
        extraSettingsJson: payload.extraSettingsJson || current?.extraSettingsJson,
        isPubliclyAvailable: Boolean(payload.isEnabled) && payload.mode !== 'Disabled',
        updatedAt: now
      })
      providers[index] = account
      await fulfillJson(route, account)
      return
    }

    const providerEnabledMatch = path.match(/^\/api\/admin\/payment-providers\/accounts\/([^/]+)\/enabled$/)
    if (method === 'POST' && providerEnabledMatch) {
      const accountId = decodeURIComponent(providerEnabledMatch[1])
      const index = providers.findIndex((account) => account.id === accountId)
      const enabled = Boolean((body as Record<string, unknown>)?.enabled)
      const account = paymentProviderAccount({ ...providers[index], isEnabled: enabled, isPubliclyAvailable: enabled, updatedAt: now })
      providers[index] = account
      await fulfillJson(route, account)
      return
    }

    const providerCheckMatch = path.match(/^\/api\/admin\/payment-providers\/accounts\/([^/]+)\/check$/)
    if (method === 'POST' && providerCheckMatch) {
      const accountId = decodeURIComponent(providerCheckMatch[1])
      const index = providers.findIndex((item) => item.id === accountId)
      const account = paymentProviderAccount({ ...providers[index], healthStatus: 'Unknown', updatedAt: '2026-06-13T07:05:00Z' })
      providers[index] = account
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
      await fulfillJson(route, supportConversations)
      return
    }

    const supportMessagesMatch = path.match(/^\/api\/admin\/support\/conversations\/([^/]+)\/messages$/)
    if (method === 'GET' && supportMessagesMatch) {
      const conversationId = decodeURIComponent(supportMessagesMatch[1])
      const response = supportMessages.get(conversationId) ?? []
      if (delayedSupportMessagesId === conversationId) {
        delayedSupportMessagesId = null
        await new Promise<void>((resolve) => { releaseDelayedSupportMessages = resolve })
        releaseDelayedSupportMessages = null
      }
      await fulfillJson(route, response)
      return
    }

    const supportStatusMatch = path.match(/^\/api\/admin\/support\/conversations\/([^/]+)\/status$/)
    if (method === 'PATCH' && supportStatusMatch) {
      const conversationId = decodeURIComponent(supportStatusMatch[1])
      const nextStatus = String((body as Record<string, unknown>)?.status ?? 'open')
      supportConversations = supportConversations.map((item) => item.id === conversationId
        ? { ...item, status: nextStatus, revision: item.revision + 1, updatedAt: now }
        : item)
      const updated = supportConversations.find((item) => item.id === conversationId)
      await fulfillJson(route, { conversationId, status: nextStatus, revision: updated?.revision ?? 0 })
      return
    }

    const supportReplyMatch = path.match(/^\/api\/admin\/support\/conversations\/([^/]+)\/reply$/)
    if (method === 'POST' && supportReplyMatch) {
      const conversationId = decodeURIComponent(supportReplyMatch[1])
      const text = String((body as Record<string, unknown>)?.text ?? '')
      const currentMessages = supportMessages.get(conversationId) ?? []
      supportMessages.set(conversationId, [...currentMessages, {
        ...adminSupportMessage(`reply-${currentMessages.length + 1}`, conversationId, 'admin-user', text),
        direction: 'outbound'
      }])
      supportConversations = supportConversations.map((item) => item.id === conversationId
        ? { ...item, revision: item.revision + 1, updatedAt: now }
        : item)
      const updated = supportConversations.find((item) => item.id === conversationId)
      await fulfillJson(route, { conversationId, status: updated?.status ?? 'open', revision: updated?.revision ?? 0 })
      return
    }

    const supportNoteMatch = path.match(/^\/api\/admin\/support\/conversations\/([^/]+)\/notes$/)
    if (method === 'POST' && supportNoteMatch) {
      const conversationId = decodeURIComponent(supportNoteMatch[1])
      const text = String((body as Record<string, unknown>)?.text ?? '')
      const currentMessages = supportMessages.get(conversationId) ?? []
      const note = {
        ...adminSupportMessage(`note-${currentMessages.length + 1}`, conversationId, 'admin-user', text),
        direction: 'internal',
        isInternalNote: true
      }
      supportMessages.set(conversationId, [...currentMessages, note])
      supportConversations = supportConversations.map((item) => item.id === conversationId
        ? { ...item, revision: item.revision + 1, internalNote: text, updatedAt: now }
        : item)
      await fulfillJson(route, note)
      return
    }

    if (method === 'GET' && path === '/api/admin/tariffs') {
      await fulfillJson(route, invalidTariffsResponse ? [{}] : tariffs)
      return
    }

    if (method === 'POST' && path === '/api/admin/tariffs') {
      if (delayNextTariffCreateResponse) {
        delayNextTariffCreateResponse = false
        await new Promise<void>((resolve) => { releaseDelayedTariffCreate = resolve })
      }
      const created = tariff({ ...(body as Record<string, unknown>), id: 'tariff-created-e2e', createdAt: now, updatedAt: now })
      tariffs.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    const tariffMutationMatch = path.match(/^\/api\/admin\/tariffs\/([^/]+)$/)
    if (tariffMutationMatch && method === 'PATCH') {
      const index = tariffs.findIndex((item) => item.id === tariffMutationMatch[1])
      const updated = tariff({ ...tariffs[index], ...(body as Record<string, unknown>), id: tariffMutationMatch[1], updatedAt: now })
      if (index >= 0) tariffs[index] = updated
      await fulfillJson(route, updated)
      return
    }

    if (tariffMutationMatch && method === 'DELETE') {
      const index = tariffs.findIndex((item) => item.id === tariffMutationMatch[1])
      if (index >= 0) tariffs.splice(index, 1)
      await fulfillJson(route, { id: tariffMutationMatch[1], deleted: true, archived: false })
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

    const referralProgramMutationMatch = path.match(/^\/api\/admin\/referral-programs\/([^/]+)$/)
    if (referralProgramMutationMatch && method === 'PATCH') {
      const index = referralPrograms.findIndex((item) => item.id === referralProgramMutationMatch[1])
      const updated = referralProgram({ ...referralPrograms[index], ...(body as Record<string, unknown>), id: referralProgramMutationMatch[1], updatedAt: now })
      if (index >= 0) referralPrograms[index] = updated
      await fulfillJson(route, updated)
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

    const releaseMutationMatch = path.match(/^\/api\/app-version\/admin\/releases\/([^/]+)$/)
    if (releaseMutationMatch && method === 'PUT') {
      const index = releases.findIndex((item) => item.id === releaseMutationMatch[1])
      const releaseBody = body as Record<string, unknown>
      const updated = release({
        ...releases[index],
        ...releaseBody,
        id: releaseMutationMatch[1],
        items: Array.isArray(releaseBody.items)
          ? releaseBody.items.map((item, itemIndex) => ({ ...(item as Record<string, unknown>), id: `release-updated-item-${itemIndex + 1}` }))
          : [],
        updatedAt: now
      })
      if (index >= 0) releases[index] = updated
      await fulfillJson(route, updated)
      return
    }

    if (releaseMutationMatch && method === 'DELETE') {
      const index = releases.findIndex((item) => item.id === releaseMutationMatch[1])
      if (index >= 0) releases.splice(index, 1)
      await fulfillJson(route, { id: releaseMutationMatch[1], deleted: true })
      return
    }

    if (method === 'GET' && path === '/api/admin/faq') {
      await fulfillJson(route, faqEntries)
      return
    }

    if (method === 'GET' && path === '/api/admin/faq/overview') {
      const activeEntries = faqEntries.filter((item) => item.isActive !== false)
      const homeEntries = activeEntries.filter((item) => item.showOnHome === true)
      const pageEntries = activeEntries.filter((item) => item.showOnFaqPage === true)
      const categories = [...new Set(faqEntries.map((item) => String(item.category ?? '')).filter(Boolean))]
      await fulfillJson(route, {
        totalCount: faqEntries.length,
        activeCount: activeEntries.length,
        hiddenCount: faqEntries.length - activeEntries.length,
        homeCount: homeEntries.length,
        faqPageCount: pageEntries.length,
        publicCount: activeEntries.length,
        categoryCount: categories.length,
        categories,
        duplicateQuestions: [],
        hasPublicFaq: pageEntries.length > 0,
        hasHomeFaq: homeEntries.length > 0
      })
      return
    }

    if (method === 'POST' && path === '/api/admin/faq') {
      const created = faqItem(body as Record<string, unknown>)
      faqEntries.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    const faqMutationMatch = path.match(/^\/api\/admin\/faq\/([^/]+)$/)
    if (faqMutationMatch && method === 'PUT') {
      const index = faqEntries.findIndex((item) => item.id === faqMutationMatch[1])
      const updated = faqItem({ ...faqEntries[index], ...(body as Record<string, unknown>), id: faqMutationMatch[1], updatedAt: now })
      if (index >= 0) faqEntries[index] = updated
      await fulfillJson(route, updated)
      return
    }

    if (faqMutationMatch && method === 'DELETE') {
      const index = faqEntries.findIndex((item) => item.id === faqMutationMatch[1])
      if (index >= 0) faqEntries.splice(index, 1)
      await fulfillJson(route, { id: faqMutationMatch[1], deleted: true })
      return
    }

    if (method === 'GET' && path === '/api/admin/site-content') {
      await fulfillJson(route, siteContentBlocks)
      return
    }

    if (method === 'GET' && path === '/api/admin/site-content/home-readiness') {
      await fulfillJson(route, { isReady: true, requiredCount: 1, presentCount: 1, activeRequiredCount: 1, missingKeys: [], inactiveKeys: [], emptyKeys: [], duplicateKeys: [], publicBlocksCount: 1, requiredKeys: ['home.hero.title'] })
      return
    }

    if (method === 'POST' && path === '/api/admin/site-content/home-defaults') {
      await fulfillJson(route, {
        created: 1,
        restored: 0,
        readiness: { isReady: true, requiredCount: 1, presentCount: 1, activeRequiredCount: 1, missingKeys: [], inactiveKeys: [], emptyKeys: [], duplicateKeys: [], publicBlocksCount: Math.max(1, siteContentBlocks.length), requiredKeys: ['home.hero.title'] }
      })
      return
    }

    if (method === 'POST' && path === '/api/admin/site-content') {
      const created = siteContentBlock(body as Record<string, unknown>)
      siteContentBlocks.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    const siteContentMutationMatch = path.match(/^\/api\/admin\/site-content\/([^/]+)$/)
    if (siteContentMutationMatch && method === 'PUT') {
      const index = siteContentBlocks.findIndex((item) => item.id === siteContentMutationMatch[1])
      const updated = siteContentBlock({ ...siteContentBlocks[index], ...(body as Record<string, unknown>), id: siteContentMutationMatch[1], updatedAt: now })
      if (index >= 0) siteContentBlocks[index] = updated
      await fulfillJson(route, updated)
      return
    }

    if (siteContentMutationMatch && method === 'DELETE') {
      const index = siteContentBlocks.findIndex((item) => item.id === siteContentMutationMatch[1])
      if (index >= 0) siteContentBlocks.splice(index, 1)
      await fulfillJson(route, { id: siteContentMutationMatch[1], deleted: true })
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

    const workScenarioMutationMatch = path.match(/^\/api\/admin\/work-scenarios\/([^/]+)$/)
    if (workScenarioMutationMatch && method === 'PUT') {
      const index = scenarios.findIndex((item) => item.id === workScenarioMutationMatch[1])
      const updated = workScenario({ ...scenarios[index], ...(body as Record<string, unknown>), id: workScenarioMutationMatch[1], updatedAt: now })
      if (index >= 0) scenarios[index] = updated
      await fulfillJson(route, updated)
      return
    }

    if (workScenarioMutationMatch && method === 'DELETE') {
      const index = scenarios.findIndex((item) => item.id === workScenarioMutationMatch[1])
      if (index >= 0) scenarios.splice(index, 1)
      await fulfillJson(route, { id: workScenarioMutationMatch[1], deleted: true })
      return
    }

    if (method === 'POST' && path === '/api/admin/servers') {
      const payload = body as Record<string, unknown>
      const publicPayload = { ...payload }
      const sshCredential = publicPayload.sshCredential
      const panelPassword = publicPayload.panelPassword
      delete publicPayload.sshCredential
      delete publicPayload.sshPrivateKeyPath
      delete publicPayload.panelPassword
      const created = vpnServer({
        ...publicPayload,
        id: 'server-created-e2e',
        usedCapacity: 0,
        status: 'Ready',
        healthStatus: 'Unknown',
        sshCredentialConfigured: Boolean(sshCredential),
        panelPasswordConfigured: Boolean(panelPassword),
        createdAt: now,
        updatedAt: now
      })
      servers.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    const serverMutationMatch = path.match(/^\/api\/admin\/servers\/([^/]+)$/)
    if (serverMutationMatch && method === 'PUT') {
      const index = servers.findIndex((item) => item.id === serverMutationMatch[1])
      const payload = body as Record<string, unknown>
      const publicPayload = { ...payload }
      const sshCredential = publicPayload.sshCredential
      const panelPassword = publicPayload.panelPassword
      delete publicPayload.sshCredential
      delete publicPayload.sshPrivateKeyPath
      delete publicPayload.panelPassword
      const updated = vpnServer({
        ...servers[index],
        ...publicPayload,
        id: serverMutationMatch[1],
        sshCredentialConfigured: Boolean(sshCredential) || Boolean(servers[index]?.sshCredentialConfigured),
        panelPasswordConfigured: Boolean(panelPassword) || Boolean(servers[index]?.panelPasswordConfigured),
        updatedAt: now
      })
      if (index >= 0) servers[index] = updated
      await fulfillJson(route, updated)
      return
    }

    const serverModeMatch = path.match(/^\/api\/admin\/servers\/([^/]+)\/(maintenance|disable-maintenance|disable-allocation|enable-allocation|disable)$/)
    if (serverModeMatch && method === 'POST') {
      const index = servers.findIndex((item) => item.id === serverModeMatch[1])
      const server = servers[index]
      const action = serverModeMatch[2]
      const updated = vpnServer({
        ...server,
        status: action === 'maintenance' ? 'Maintenance' : action === 'disable' ? 'Disabled' : action === 'disable-maintenance' ? 'Ready' : server.status,
        isAvailableForNewUsers: action === 'enable-allocation' ? true : action === 'disable-allocation' || action === 'maintenance' || action === 'disable' ? false : server.isAvailableForNewUsers,
        updatedAt: now
      })
      if (index >= 0) servers[index] = updated
      await fulfillJson(route, updated)
      return
    }

    const serverHealthMatch = path.match(/^\/api\/admin\/servers\/([^/]+)\/health-check$/)
    if (serverHealthMatch && method === 'POST') {
      const index = servers.findIndex((item) => item.id === serverHealthMatch[1])
      if (index >= 0) {
        servers[index] = vpnServer({ ...servers[index], healthStatus: 'Healthy', lastHealthCheckAt: now, lastHealthLatencyMs: 18, lastHealthError: '', updatedAt: now })
      }
      await fulfillJson(route, { id: 'health-created-e2e', nodeId: serverHealthMatch[1], status: 'Healthy', checkedAt: now, latencyMs: 18, metadataJson: '{"source":"playwright"}', errorText: '' })
      return
    }

    const serverPrecheckMatch = path.match(/^\/api\/admin\/servers\/([^/]+)\/precheck$/)
    if (serverPrecheckMatch && method === 'POST') {
      const server = servers.find((item) => item.id === serverPrecheckMatch[1])
      const runId = 'provisioning-precheck-created-e2e'
      if (!provisioningRuns.some((item) => item.id === runId)) {
        provisioningRuns.unshift(provisioningRun({
          id: runId,
          nodeId: serverPrecheckMatch[1],
          nodeName: `${server?.name ?? 'VPS'} Precheck E2E`,
          targetHost: server?.host ?? 'unknown.example.test',
          status: 'ReadyToDeploy',
          currentStep: 'ready_to_deploy',
          precheckReportPreview: 'Stateful browser precheck ready.',
          executionLog: 'dry-run precheck completed',
          executionLogPreview: 'dry-run precheck completed'
        }))
      }
      await fulfillJson(route, provisioningCommand(serverPrecheckMatch[1], runId, { status: 'ReadyToDeploy' }))
      return
    }

    const serverProvisionMatch = path.match(/^\/api\/admin\/servers\/([^/]+)\/provision$/)
    if (serverProvisionMatch && method === 'POST') {
      const server = servers.find((item) => item.id === serverProvisionMatch[1])
      const runId = 'provisioning-direct-created-e2e'
      if (!provisioningRuns.some((item) => item.id === runId)) {
        provisioningRuns.unshift(provisioningRun({
          id: runId,
          nodeId: serverProvisionMatch[1],
          nodeName: `${server?.name ?? 'VPS'} Validation E2E`,
          targetHost: server?.host ?? 'unknown.example.test',
          status: 'DeployQueued',
          currentStep: 'deploy_queued',
          dryRun: false,
          mode: 'validation-deploy',
          modeTitle: 'Validation deploy',
          riskLevel: 'low',
          nextAction: 'Дождитесь validation result.',
          operatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.',
          finishedAt: null
        }))
      }
      await fulfillJson(route, provisioningCommand(serverProvisionMatch[1], runId, {
        status: 'DeployQueued',
        dryRun: false,
        mode: 'validation-deploy',
        modeTitle: 'Validation deploy',
        riskLevel: 'low',
        nextAction: 'Дождитесь validation result.',
        operatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.'
      }))
      return
    }

    if (method === 'DELETE' && path === '/api/admin/servers/server-eu') {
      await fulfillJson(route, { id: 'server-eu', deleted: false, archived: true, linkedSubscriptions: 0, linkedAccesses: 0, linkedProvisioningRuns: 0, linkedHealthChecks: 2, linkedMigrationJobs: 1 })
      return
    }

    if (serverMutationMatch && method === 'DELETE') {
      const index = servers.findIndex((item) => item.id === serverMutationMatch[1])
      if (index >= 0) servers.splice(index, 1)
      await fulfillJson(route, { id: serverMutationMatch[1], deleted: true, archived: false, linkedSubscriptions: 0, linkedAccesses: 0, linkedProvisioningRuns: 0, linkedHealthChecks: 0, linkedMigrationJobs: 0 })
      return
    }

    if (method === 'GET' && path === '/api/admin/servers') {
      await fulfillJson(route, invalidServersResponse ? [{}] : servers)
      return
    }

    if (method === 'GET' && path === '/api/admin/provisioning-runs') {
      await fulfillJson(route, invalidProvisioningRunsResponse ? [{}] : provisioningRuns)
      return
    }

    const provisioningActionMatch = path.match(/^\/api\/admin\/provisioning-runs\/([^/]+)\/(deploy|cancel|retry|support-needed)$/)
    if (provisioningActionMatch && method === 'POST') {
      const runId = provisioningActionMatch[1]
      const action = provisioningActionMatch[2]
      const index = provisioningRuns.findIndex((item) => item.id === runId)
      const run = provisioningRuns[index]
      if (action === 'cancel') {
        provisioningRuns[index] = provisioningRun({ ...run, status: 'Cancelled', currentStep: 'cancelled', finishedAt: now, updatedAt: now })
        await fulfillJson(route, { runId, status: 'cancelled' })
        return
      }
      if (action === 'support-needed') {
        await fulfillJson(route, { runId, supportConversationId: 'support-provisioning-e2e' })
        return
      }
      const retry = action === 'retry'
      provisioningRuns[index] = provisioningRun({
        ...run,
        status: retry ? 'Retrying' : 'DeployQueued',
        currentStep: retry ? 'retrying' : 'deploy_queued',
        dryRun: false,
        mode: 'validation-deploy',
        modeTitle: 'Validation deploy',
        riskLevel: 'low',
        nextAction: retry ? 'Дождитесь повторной validation-попытки.' : 'Дождитесь validation deploy.',
        operatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.',
        attemptCount: Number(run?.attemptCount ?? 0) + (retry ? 1 : 0),
        finishedAt: null,
        updatedAt: now
      })
      await fulfillJson(route, provisioningCommand(String(run?.nodeId ?? 'server-eu'), runId, {
        status: retry ? 'Retrying' : 'DeployQueued',
        dryRun: false,
        mode: 'validation-deploy',
        modeTitle: 'Validation deploy',
        riskLevel: 'low',
        nextAction: retry ? 'Дождитесь повторной validation-попытки.' : 'Дождитесь validation deploy.',
        operatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.'
      }))
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-panels') {
      await fulfillJson(route, invalidVpnPanelsResponse ? [{}] : panels)
      return
    }

    if (method === 'POST' && path === '/api/admin/vpn-panels') {
      const payload = body as Record<string, unknown>
      const publicPayload = { ...payload }
      delete publicPayload.password
      const created = vpnPanel({
        ...publicPayload,
        id: 'panel-created-e2e',
        status: 'Active',
        healthStatus: 'Unknown',
        usedCapacity: 0,
        version: '',
        lastError: '',
        createdAt: now,
        updatedAt: now
      })
      panels.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    const panelMutationMatch = path.match(/^\/api\/admin\/vpn-panels\/([^/]+)$/)
    if (panelMutationMatch && method === 'PATCH') {
      const index = panels.findIndex((item) => item.id === panelMutationMatch[1])
      const payload = body as Record<string, unknown>
      const publicPayload = { ...payload }
      delete publicPayload.password
      const updated = vpnPanel({ ...panels[index], ...publicPayload, id: panelMutationMatch[1], updatedAt: now })
      if (index >= 0) panels[index] = updated
      await fulfillJson(route, updated)
      return
    }

    if (panelMutationMatch && method === 'DELETE') {
      const panelId = panelMutationMatch[1]
      const index = panels.findIndex((item) => item.id === panelId)
      const linkedInbounds = inbounds.filter((item) => item.vpnPanelId === panelId).length
      if (linkedInbounds > 0) {
        panels[index] = vpnPanel({ ...panels[index], status: 'Disabled', updatedAt: now })
      } else if (index >= 0) {
        panels.splice(index, 1)
      }
      await fulfillJson(route, { id: panelId, deleted: linkedInbounds === 0, archived: linkedInbounds > 0, linkedInbounds, linkedClients: 0, linkedSyncRuns: 0, linkedHealthChecks: 0 })
      return
    }

    const panelTestMatch = path.match(/^\/api\/admin\/vpn-panels\/([^/]+)\/test-connection$/)
    if (method === 'POST' && panelTestMatch) {
      await fulfillJson(route, { id: `panel-health-${panelTestMatch[1]}`, vpnPanelId: panelTestMatch[1], status: 'Healthy', latencyMs: 22, version: '2.4.9', errorMessage: '', checkedAt: '2026-06-13T07:06:00Z' })
      return
    }

    const panelSyncMatch = path.match(/^\/api\/admin\/vpn-panels\/([^/]+)\/sync$/)
    if (method === 'POST' && panelSyncMatch) {
      await fulfillJson(route, { id: `panel-sync-${panelSyncMatch[1]}`, vpnPanelId: panelSyncMatch[1], status: 'Succeeded', startedAt: '2026-06-13T07:07:00Z', finishedAt: '2026-06-13T07:07:10Z', summaryJson: '{"clients":1}', errorMessage: '' })
      return
    }

    if (method === 'GET' && path === '/api/admin/vpn-inbounds') {
      await fulfillJson(route, inbounds)
      return
    }

    const panelInboundsMatch = path.match(/^\/api\/admin\/vpn-panels\/([^/]+)\/inbounds$/)
    if (method === 'POST' && panelInboundsMatch) {
      const payload = body as Record<string, unknown>
      if (payload.isDefault) {
        inbounds.forEach((item) => {
          if (item.vpnPanelId === panelInboundsMatch[1]) item.isDefault = false
        })
      }
      const created = {
        id: 'inbound-created-e2e',
        vpnPanelId: panelInboundsMatch[1],
        externalInboundId: '99',
        usedCapacity: 0,
        ...payload
      }
      inbounds.push(created)
      await fulfillJson(route, created, 201)
      return
    }

    if (method === 'GET' && panelInboundsMatch) {
      if (panelInboundsMatch[1] === 'panel-eu' && delayNextVpnPanelInboundsResponse) {
        delayNextVpnPanelInboundsResponse = false
        await new Promise((resolve) => setTimeout(resolve, 1200))
      }
      await fulfillJson(route, inbounds.filter((item) => item.vpnPanelId === panelInboundsMatch[1]))
      return
    }

    const inboundMutationMatch = path.match(/^\/api\/admin\/vpn-inbounds\/([^/]+)$/)
    if (method === 'PATCH' && inboundMutationMatch) {
      const index = inbounds.findIndex((item) => item.id === inboundMutationMatch[1])
      const payload = body as Record<string, unknown>
      if (payload.isDefault) {
        inbounds.forEach((item) => {
          if (item.vpnPanelId === inbounds[index]?.vpnPanelId) item.isDefault = false
        })
      }
      const updated = { ...inbounds[index], ...payload, id: inboundMutationMatch[1] }
      if (index >= 0) inbounds[index] = updated
      await fulfillJson(route, updated)
      return
    }

    const inboundDefaultMatch = path.match(/^\/api\/admin\/vpn-inbounds\/([^/]+)\/set-default$/)
    if (method === 'POST' && inboundDefaultMatch) {
      const index = inbounds.findIndex((item) => item.id === inboundDefaultMatch[1])
      const panelId = inbounds[index]?.vpnPanelId
      inbounds.forEach((item) => {
        if (item.vpnPanelId === panelId) item.isDefault = item.id === inboundDefaultMatch[1]
      })
      await fulfillJson(route, inbounds[index])
      return
    }

    const panelClientsMatch = path.match(/^\/api\/admin\/vpn-panels\/([^/]+)\/clients$/)
    if (method === 'GET' && panelClientsMatch) {
      await fulfillJson(route, clients.filter((item) => item.vpnPanelId === panelClientsMatch[1]))
      return
    }

    const vpnClientActionMatch = path.match(/^\/api\/admin\/vpn-clients\/([^/]+)\/(enable|disable|sync|reset-traffic)$/)
    if (method === 'POST' && vpnClientActionMatch) {
      const index = clients.findIndex((item) => item.id === vpnClientActionMatch[1])
      const action = vpnClientActionMatch[2]
      const updated = {
        ...clients[index],
        enable: action === 'enable' ? true : action === 'disable' ? false : clients[index]?.enable,
        syncStatus: action === 'reset-traffic' ? 'traffic-reset' : action === 'sync' ? 'synced' : action,
        lastSyncedAt: now
      }
      if (index >= 0) clients[index] = updated
      await fulfillJson(route, updated)
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
      const sourcePanelId = clients[0].vpnPanelId
      if (source) source.usedCapacity = Number(source.usedCapacity) - 1
      target.usedCapacity = Number(target.usedCapacity) + 1
      if (sourcePanelId !== target.vpnPanelId) {
        const sourcePanel = panels.find((item) => item.id === sourcePanelId)
        const targetPanel = panels.find((item) => item.id === target.vpnPanelId)
        if (sourcePanel) sourcePanel.usedCapacity = Math.max(0, Number(sourcePanel.usedCapacity) - 1)
        if (targetPanel) targetPanel.usedCapacity = Number(targetPanel.usedCapacity) + 1
      }
      clients[0] = { ...clients[0], vpnPanelId: target.vpnPanelId, vpnInboundId: target.id, syncStatus: 'migrated', configUri: 'vless://client@us.example.test:9443', qrCodePayload: 'vless://client@us.example.test:9443', lastSyncedAt: now }
      await fulfillJson(route, clients[0])
      return
    }

    const panelHealthChecksMatch = path.match(/^\/api\/admin\/vpn-panels\/([^/]+)\/health-checks$/)
    if (method === 'GET' && panelHealthChecksMatch) {
      await fulfillJson(route, panelHealthChecksMatch[1] === 'panel-eu' ? [{ id: 'panel-health-e2e', vpnPanelId: 'panel-eu', status: 'Healthy', latencyMs: 22, version: '2.4.9', errorMessage: '', checkedAt: now }] : [])
      return
    }

    const panelSyncRunsMatch = path.match(/^\/api\/admin\/vpn-panels\/([^/]+)\/sync-runs$/)
    if (method === 'GET' && panelSyncRunsMatch) {
      await fulfillJson(route, panelSyncRunsMatch[1] === 'panel-eu' ? [{ id: 'panel-sync-e2e', vpnPanelId: 'panel-eu', status: 'Failed', startedAt: now, finishedAt: now, summaryJson: '{}', errorMessage: 'Remote panel sync failed.' }] : [])
      return
    }

    if (method === 'PATCH' && path === '/api/admin/telegram-bot/settings') {
      const payload = body as Record<string, unknown>
      botSettings = telegramBotSettings({
        ...botSettings,
        enabled: payload.enabled,
        mode: payload.mode,
        publicBotUsername: payload.publicBotUsername,
        webhookUrl: payload.webhookUrl,
        adminChatId: payload.adminChatId,
        webAppUrl: payload.webAppUrl,
        welcomeText: payload.welcomeText,
        instructionText: payload.instructionText,
        supportText: payload.supportText,
        afterPaymentTextTemplate: payload.afterPaymentTextTemplate,
        renewalTextTemplate: payload.renewalTextTemplate,
        paymentFailedTextTemplate: payload.paymentFailedTextTemplate,
        subscriptionExpiredTextTemplate: payload.subscriptionExpiredTextTemplate,
        hasBotToken: Boolean(payload.botToken) || Boolean(botSettings.hasBotToken),
        botTokenMasked: Boolean(payload.botToken) ? '***configured***' : botSettings.botTokenMasked,
        hasSecretToken: Boolean(payload.secretToken) || Boolean(botSettings.hasSecretToken),
        generatedAt: now
      })
      await fulfillJson(route, botSettings)
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
      await fulfillJson(route, invalidBotSettingsResponse ? {} : botSettings)
      return
    }

    await fulfillJson(route, { error: `Unhandled ${method} ${path}` }, 404)
  })

  return {
    getLastRequest: (path: string, method = 'POST') =>
      requests.findLast((item) => item.method === method && item.path === path),
    getRequestCount: (path: string, method = 'GET') =>
      requests.filter((item) => item.method === method && item.path === path).length,
    getAuthorizedRequestCount: (path: string, method: string, authorization: string) =>
      requests.filter((item) => item.method === method && item.path === path && item.authorization === authorization).length,
    denyNextDashboard: () => { dashboardShouldDeny = true },
    expireAccessToken: (accessToken: string) => { expiredAdminAccessTokens.add(`Bearer ${accessToken}`) },
    failNextAdminSessionRequest: (status = 503) => { failNextAdminSessionStatus = status },
    failRefreshRequest: (status = 401) => { refreshFailureStatus = status },
    delayNextRefreshRequest: () => { delayNextRefresh = true },
    releaseRefreshRequest: () => {
      delayedRefreshReleased = true
      releaseDelayedRefresh?.()
    },
    returnInvalidUsersResponse: () => { invalidUsersResponse = true },
    returnEmptyAuditLogsResponse: () => { emptyAuditLogsResponse = true },
    returnInvalidPaymentProviderAccountsResponse: () => { invalidPaymentProviderAccountsResponse = true },
    returnInvalidTariffsResponse: () => { invalidTariffsResponse = true },
    returnInvalidVpnPanelsResponse: () => { invalidVpnPanelsResponse = true },
    returnInvalidServersResponse: () => { invalidServersResponse = true },
    returnInvalidProvisioningRunsResponse: () => { invalidProvisioningRunsResponse = true },
    returnInvalidBotSettingsResponse: () => { invalidBotSettingsResponse = true },
    delayNextVpnPanelInbounds: () => { delayNextVpnPanelInboundsResponse = true },
    delayNextBotSettingsCheck: () => { delayNextBotSettingsCheckResponse = true },
    useDetailRequestRaceFixture: () => {
      users = [
        adminUser('user-first', 'Первый пользователь', 'first@example.test'),
        adminUser('user-second', 'Второй пользователь', 'second@example.test')
      ]
      userOverviews = new Map(users.map((item) => [item.id, adminUserOverview(item)]))
      supportConversations = [
        adminSupportConversation('support-first', 'user-first', 'Первое обращение'),
        adminSupportConversation('support-second', 'user-second', 'Второе обращение')
      ]
      supportMessages = new Map([
        ['support-first', [adminSupportMessage('message-first', 'support-first', 'user-first', 'Старое сообщение первого обращения')]],
        ['support-second', [adminSupportMessage('message-second', 'support-second', 'user-second', 'Текущее сообщение второго обращения')]]
      ])
    },
    delayUserOverview: (userId: string) => { delayedUserOverviewId = userId },
    releaseUserOverview: () => { releaseDelayedUserOverview?.() },
    delaySupportMessages: (conversationId: string) => { delayedSupportMessagesId = conversationId },
    releaseSupportMessages: () => { releaseDelayedSupportMessages?.() },
    delayNextTariffCreate: () => { delayNextTariffCreateResponse = true },
    releaseTariffCreate: () => { releaseDelayedTariffCreate?.() },
    prepareSubscriptionLifecycle: () => {
      subscriptions[0] = {
        ...subscriptions[0],
        status: 'PendingActivation',
        endAt: '2027-01-13T07:00:00Z',
        gracePeriodEndAt: null,
        currentServerId: 'server-eu',
        currentAccessId: null,
        renewalCount: 0,
        blockReason: null,
        cancelledAt: null,
        accessUri: '',
        qrCodePath: '',
        configPath: '',
        nodeName: 'EU Sandbox',
        updatedAt: now
      }
      accessCredentials[0] = {
        ...accessCredentials[0],
        subscriptionStatus: 'PendingActivation',
        isTerminal: false,
        serverId: 'server-eu',
        serverName: 'EU Sandbox',
        status: 'Disabled',
        disabledAt: now,
        revision: 1,
        updatedAt: now
      }
    },
    updateFirstDetailFixture: (userDisplayName: string, messageText: string) => {
      const nextUser = adminUser('user-first', userDisplayName, 'first@example.test')
      users = [nextUser, users.find((item) => item.id === 'user-second') ?? adminUser('user-second', 'Второй пользователь', 'second@example.test')]
      userOverviews.set('user-first', adminUserOverview(nextUser))
      supportMessages.set('support-first', [adminSupportMessage('message-first-current', 'support-first', 'user-first', messageText)])
    },
    failLogout: () => { logoutShouldFail = true }
  }
}

async function seedAdminSession(page: Page, accessToken: string, refreshToken: string) {
  await page.addInitScript(({ accessToken: access, refreshToken: refresh }) => {
    sessionStorage.setItem('vpn-platform-admin-token', access)
    sessionStorage.setItem('vpn-platform-admin-refresh-token', refresh)
  }, { accessToken, refreshToken })
}

test('admin detail views ignore older selections and keep support actions scoped', async ({ page }) => {
  const api = await mockAdminApi(page)
  api.useDetailRequestRaceFixture()
  api.delayUserOverview('user-first')
  api.delaySupportMessages('support-first')
  await seedAdminSession(page, 'admin-detail-race-token', 'admin-detail-race-refresh')

  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/admin/users/user-first/overview')).toBe(1)
  await expect.poll(() => api.getRequestCount('/api/admin/support/conversations/support-first/messages')).toBe(1)

  await openAdminSection(page, 'Пользователи', 'users')
  const usersSection = page.locator('#users')
  const secondUserRow = usersSection.locator('.list-item').filter({ hasText: 'Второй пользователь' })
  await secondUserRow.getByRole('button', { name: 'Открыть' }).click()
  await expect(usersSection.locator('.user-overview-card').getByText('Второй пользователь', { exact: true })).toBeVisible()
  const firstOverviewResponse = page.waitForResponse((response) => response.url().endsWith('/api/admin/users/user-first/overview'))
  api.releaseUserOverview()
  await firstOverviewResponse
  await expect(usersSection.locator('.user-overview-card').getByText('Второй пользователь', { exact: true })).toBeVisible()
  await expect(usersSection.locator('.user-overview-card').getByText('Первый пользователь', { exact: true })).toHaveCount(0)

  await openAdminSection(page, 'Поддержка', 'support')
  const supportSection = page.locator('#support')
  await supportSection.getByLabel('Ответ пользователю').fill('Черновик ответа для первого обращения')
  await supportSection.getByLabel('Внутренняя заметка').fill('Черновик заметки для первого обращения')
  const secondConversation = supportSection.locator('.list-item-vertical').filter({ hasText: 'Второе обращение' }).first()
  await secondConversation.getByRole('button', { name: 'Открыть' }).click()
  await expect(supportSection.getByText('Текущее сообщение второго обращения', { exact: true })).toBeVisible()
  await expect(supportSection.getByLabel('Ответ пользователю')).toHaveValue('')
  await expect(supportSection.getByLabel('Внутренняя заметка')).toHaveValue('')
  const firstMessagesResponse = page.waitForResponse((response) => response.url().endsWith('/api/admin/support/conversations/support-first/messages'))
  api.releaseSupportMessages()
  await firstMessagesResponse
  await expect(supportSection.getByText('Текущее сообщение второго обращения', { exact: true })).toBeVisible()
  await expect(supportSection.getByText('Старое сообщение первого обращения', { exact: true })).toHaveCount(0)

  const firstMessagesCount = api.getRequestCount('/api/admin/support/conversations/support-first/messages')
  const firstConversation = supportSection.locator('.list-item-vertical').filter({ hasText: 'Первое обращение' }).first()
  await firstConversation.getByRole('button', { name: 'Закрыть' }).click()
  await expect(page.getByText('Статус обращения обновлен: closed.')).toBeVisible()
  expect(api.getRequestCount('/api/admin/support/conversations/support-first/messages')).toBe(firstMessagesCount)
  await expect(supportSection.getByText('Текущее сообщение второго обращения', { exact: true })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('admin detail requests cannot restore old data after logout and new login', async ({ page }) => {
  const api = await mockAdminApi(page)
  api.useDetailRequestRaceFixture()
  api.delayUserOverview('user-first')
  api.delaySupportMessages('support-first')
  await seedAdminSession(page, 'admin-detail-logout-token', 'admin-detail-logout-refresh')

  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/admin/users/user-first/overview')).toBe(1)
  await expect.poll(() => api.getRequestCount('/api/admin/support/conversations/support-first/messages')).toBe(1)
  api.updateFirstDetailFixture('Профиль новой сессии', 'Сообщение новой сессии')

  await page.getByRole('button', { name: 'Завершить сессию', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Вход администратора' })).toBeVisible()
  await page.locator('.admin-login-form input[type="email"]').fill('admin-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('AdminPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()
  await expect(page.locator('.admin-shell')).toBeVisible()

  await openAdminSection(page, 'Пользователи', 'users')
  const usersSection = page.locator('#users')
  await expect(usersSection.locator('.user-overview-card').getByText('Профиль новой сессии', { exact: true })).toBeVisible()
  await openAdminSection(page, 'Поддержка', 'support')
  const supportSection = page.locator('#support')
  await expect(supportSection.getByText('Сообщение новой сессии', { exact: true })).toBeVisible()

  const oldOverviewResponse = page.waitForResponse((response) => response.url().endsWith('/api/admin/users/user-first/overview') && response.request().headers().authorization === 'Bearer admin-detail-logout-token')
  const oldMessagesResponse = page.waitForResponse((response) => response.url().endsWith('/api/admin/support/conversations/support-first/messages') && response.request().headers().authorization === 'Bearer admin-detail-logout-token')
  api.releaseUserOverview()
  api.releaseSupportMessages()
  await Promise.all([oldOverviewResponse, oldMessagesResponse])

  await openAdminSection(page, 'Пользователи', 'users')
  await expect(usersSection.locator('.user-overview-card').getByText('Профиль новой сессии', { exact: true })).toBeVisible()
  await expect(usersSection.locator('.user-overview-card').getByText('Первый пользователь', { exact: true })).toHaveCount(0)
  await openAdminSection(page, 'Поддержка', 'support')
  await expect(supportSection.getByText('Сообщение новой сессии', { exact: true })).toBeVisible()
  await expect(supportSection.getByText('Старое сообщение первого обращения', { exact: true })).toHaveCount(0)
})

test('admin mutations reject duplicate submits and ignore completion from an old session', async ({ page }) => {
  const api = await mockAdminApi(page)
  api.delayNextTariffCreate()
  await seedAdminSession(page, 'admin-mutation-old-token', 'admin-mutation-old-refresh')

  await page.goto('http://127.0.0.1:5295/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'Тарифы', 'tariffs')
  const tariffsSection = page.locator('#tariffs')
  await tariffsSection.getByLabel('Название').fill('Тариф старой сессии')
  await tariffsSection.getByLabel('Slug').fill('old-session-tariff')
  await tariffsSection.getByRole('spinbutton', { name: 'Цена' }).fill('690')
  await tariffsSection.getByLabel('Короткое описание').fill('Не должен менять UI новой сессии.')

  await tariffsSection.locator('form').evaluate((form) => {
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
  })
  await expect.poll(() => api.getRequestCount('/api/admin/tariffs', 'POST')).toBe(1)
  await expect(tariffsSection.getByRole('button', { name: 'Создать тариф' })).toHaveAttribute('aria-busy', 'true')

  await page.getByRole('button', { name: 'Завершить сессию' }).click()
  await expect(page.getByRole('heading', { name: 'Вход администратора' })).toBeVisible()
  await page.locator('.admin-login-form input[type="email"]').fill('admin-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('AdminPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()
  await expect(page.locator('.admin-shell')).toBeVisible()

  await openAdminSection(page, 'Тарифы', 'tariffs')
  await tariffsSection.getByLabel('Название').fill('Черновик новой сессии')
  await tariffsSection.getByLabel('Slug').fill('new-session-draft')
  const oldSessionTariffGets = api.getAuthorizedRequestCount('/api/admin/tariffs', 'GET', 'Bearer admin-mutation-old-token')
  const delayedResponse = page.waitForResponse((response) => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/admin/tariffs'
    && response.request().headers().authorization === 'Bearer admin-mutation-old-token')
  api.releaseTariffCreate()
  await delayedResponse

  await expect(tariffsSection.getByLabel('Название')).toHaveValue('Черновик новой сессии')
  await expect(tariffsSection.getByLabel('Slug')).toHaveValue('new-session-draft')
  await expect(page.getByText('Тариф создан.', { exact: true })).toHaveCount(0)
  await expect(tariffsSection.getByText('Тариф старой сессии', { exact: true })).toHaveCount(0)
  expect(api.getRequestCount('/api/admin/tariffs', 'POST')).toBe(1)
  expect(api.getAuthorizedRequestCount('/api/admin/tariffs', 'GET', 'Bearer admin-mutation-old-token')).toBe(oldSessionTariffGets)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('admin reloads preserve newer form drafts while an older save is pending', async ({ page }) => {
  const api = await mockAdminApi(page)
  api.delayNextTariffCreate()

  await page.goto('http://127.0.0.1:5295/')
  await page.locator('.admin-login-form input[type="email"]').fill('admin-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('AdminPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()
  await expect(page.locator('.admin-shell')).toBeVisible()

  await openAdminSection(page, 'Telegram-бот', 'bot')
  const botSection = page.locator('#bot')
  await botSection.getByLabel('Username публичного бота').fill('draft_bot_username')
  const botReload = page.waitForResponse((response) => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/admin/telegram-bot/settings')
  await page.getByRole('button', { name: 'Обновить данные' }).click()
  await botReload
  await expect(botSection.getByLabel('Username публичного бота')).toHaveValue('draft_bot_username')

  await openAdminSection(page, 'Тарифы', 'tariffs')
  const tariffsSection = page.locator('#tariffs')
  await tariffsSection.getByLabel('Название').fill('Отправленный тариф')
  await tariffsSection.getByLabel('Slug').fill('submitted-tariff')
  await tariffsSection.getByRole('spinbutton', { name: 'Цена' }).fill('590')
  await tariffsSection.getByLabel('Короткое описание').fill('Снимок формы для сохранения.')
  await tariffsSection.getByRole('button', { name: 'Создать тариф' }).click()
  await expect.poll(() => api.getRequestCount('/api/admin/tariffs', 'POST')).toBe(1)

  await tariffsSection.getByLabel('Название').fill('Новый несохранённый черновик')
  await tariffsSection.getByLabel('Slug').fill('new-unsaved-draft')
  const delayedResponse = page.waitForResponse((response) => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/admin/tariffs')
  api.releaseTariffCreate()
  await delayedResponse
  await expect(tariffsSection.getByText('Отправленный тариф', { exact: true })).toBeVisible()
  await expect(tariffsSection.getByLabel('Название')).toHaveValue('Новый несохранённый черновик')
  await expect(tariffsSection.getByLabel('Slug')).toHaveValue('new-unsaved-draft')

  await openAdminSection(page, 'Telegram-бот', 'bot')
  await expect(botSection.getByLabel('Username публичного бота')).toHaveValue('draft_bot_username')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
})

test('admin restores a valid session once under StrictMode', async ({ page }) => {
  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-e2e-token', 'admin-e2e-refresh')

  await page.goto('/')

  await expect(page.locator('.admin-shell')).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/admin/session')).toBe(1)
  expect(api.getRequestCount('/api/auth/refresh', 'POST')).toBe(0)
})

test('admin rotates an expired restored access token once', async ({ page }) => {
  const api = await mockAdminApi(page)
  api.expireAccessToken('admin-e2e-token-expired')
  await seedAdminSession(page, 'admin-e2e-token-expired', 'admin-e2e-refresh')

  await page.goto('/')

  await expect(page.locator('.admin-shell')).toBeVisible()
  await expect.poll(() => api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
  expect(api.getRequestCount('/api/admin/session')).toBe(2)
  await expect(page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).resolves.toEqual({
    access: 'admin-e2e-token-rotated',
    refresh: 'admin-e2e-refresh-rotated'
  })
})

test('admin preserves a restored session after a transient admission failure', async ({ page }) => {
  const api = await mockAdminApi(page)
  api.failNextAdminSessionRequest()
  await seedAdminSession(page, 'admin-e2e-token', 'admin-e2e-refresh')

  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Восстановление admin-сессии' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Повторить проверку' })).toBeEnabled()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await expect(page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).resolves.toEqual({ access: 'admin-e2e-token', refresh: 'admin-e2e-refresh' })

  await page.getByRole('button', { name: 'Повторить проверку' }).click()
  await expect(page.locator('.admin-shell')).toBeVisible()
  expect(api.getRequestCount('/api/admin/session')).toBe(2)
  expect(api.getRequestCount('/api/auth/refresh', 'POST')).toBe(0)
})

test('admin clears a restored session when refresh is rejected', async ({ page }) => {
  const api = await mockAdminApi(page)
  api.expireAccessToken('admin-e2e-token-expired')
  api.failRefreshRequest()
  await seedAdminSession(page, 'admin-e2e-token-expired', 'admin-e2e-refresh-invalid')

  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Вход администратора' })).toBeVisible()
  await expect(page.getByText('Сессия администратора завершена. Войдите заново.')).toBeVisible()
  await expect(page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).resolves.toEqual({ access: null, refresh: null })
  expect(api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
})

test('admin preserves rotated tokens when manual session verification is transient', async ({ page }) => {
  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-e2e-token', 'admin-e2e-refresh')

  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  api.failNextAdminSessionRequest()
  await page.getByRole('button', { name: 'Обновить сессию' }).click()

  await expect(page.getByRole('heading', { name: 'Восстановление admin-сессии' })).toBeVisible()
  await expect(page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).resolves.toEqual({
    access: 'admin-e2e-token-rotated',
    refresh: 'admin-e2e-refresh-rotated'
  })

  await page.getByRole('button', { name: 'Повторить проверку' }).click()
  await expect(page.locator('.admin-shell')).toBeVisible()
  expect(api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
})

test('admin ignores a delayed restored-session refresh after logout', async ({ page }) => {
  const api = await mockAdminApi(page)
  api.expireAccessToken('admin-e2e-token-expired')
  api.delayNextRefreshRequest()
  await seedAdminSession(page, 'admin-e2e-token-expired', 'admin-e2e-refresh')

  await page.goto('/')
  await expect.poll(() => api.getRequestCount('/api/auth/refresh', 'POST')).toBe(1)
  await page.getByRole('button', { name: 'Завершить сессию' }).click()
  await expect(page.getByRole('heading', { name: 'Вход администратора' })).toBeVisible()

  api.releaseRefreshRequest()
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-admin-token'),
    refresh: sessionStorage.getItem('vpn-platform-admin-refresh-token')
  }))).toEqual({ access: null, refresh: null })
  await expect(page.locator('.admin-shell')).toHaveCount(0)
})

test('admin clears private data and filters before a new login', async ({ page }) => {
  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-e2e-token', 'admin-e2e-refresh')

  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'Аудит', 'audit')
  await expect(page.getByText('auth.login', { exact: true })).toBeVisible()
  await page.locator('#audit').getByLabel('Действие').fill('stale-filter')

  await page.getByRole('button', { name: 'Завершить сессию' }).click()
  await expect(page.getByRole('heading', { name: 'Вход администратора' })).toBeVisible()
  api.returnEmptyAuditLogsResponse()
  await page.locator('.admin-login-form input[type="email"]').fill('admin-e2e@example.test')
  await page.locator('.admin-login-form input[type="password"]').fill('AdminPassword123!')
  await page.getByRole('button', { name: 'Войти в админку' }).click()

  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'Аудит', 'audit')
  await expect(page.getByText('Записей аудита нет')).toBeVisible()
  await expect(page.locator('#audit').getByLabel('Действие')).toHaveValue('')
  await expect(page.getByText('auth.login', { exact: true })).toHaveCount(0)
})

async function openAdminSection(page: Page, name: string, id: string) {
  const tab = page.getByRole('tab', { name })
  if (await tab.isVisible()) {
    await tab.click()
  } else {
    await page.getByRole('combobox', { name: 'Раздел' }).selectOption(id)
  }
  await expect(page.locator(`#${id}`)).toBeVisible()
}

test('admin payment provider accounts support secure lifecycle', async ({ page }) => {
  test.slow()
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))

  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-provider-token', 'admin-provider-refresh')
  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'Оплаты', 'payments')

  const paymentsPanel = page.locator('#payments')
  await paymentsPanel.getByLabel('Внутреннее имя').fill('yookassa-crud-e2e')
  await paymentsPanel.getByLabel('Название для пользователя').fill('YooKassa CRUD E2E')
  await paymentsPanel.getByLabel('Shop ID магазина').fill('shop-crud-e2e')
  await paymentsPanel.getByRole('textbox', { name: /^Секретный ключ/ }).fill('e2e-api-key-value')
  await paymentsPanel.getByRole('textbox', { name: /^Секрет webhook/ }).fill('e2e-webhook-key-value')
  await paymentsPanel.getByLabel('URL возврата после оплаты').fill('https://cabinet.example.test/payment-return')
  await paymentsPanel.getByLabel('URL webhook в YooKassa').fill('https://api.example.test/api/webhooks/payments/yookassa')
  await paymentsPanel.getByRole('button', { name: 'Сохранить способ оплаты' }).click()

  await expect(page.getByText('Способ оплаты yookassa-crud-e2e сохранен. Секреты не отображаются.')).toBeVisible()
  let providerRow = paymentsPanel.locator('.list-item-vertical').filter({ hasText: 'YooKassa CRUD E2E' })
  await expect(providerRow).toContainText('Секретный ключ: задан')
  await expect(providerRow).toContainText('Секрет webhook: задан')
  expect(api.getLastRequest('/api/admin/payment-providers/accounts')?.body).toMatchObject({
    name: 'yookassa-crud-e2e',
    publicName: 'YooKassa CRUD E2E',
    secretKey: 'e2e-api-key-value',
    webhookSecret: 'e2e-webhook-key-value'
  })
  await expect(page.getByText('e2e-api-key-value', { exact: true })).toHaveCount(0)
  await expect(page.getByText('e2e-webhook-key-value', { exact: true })).toHaveCount(0)

  await providerRow.getByRole('button', { name: 'Редактировать' }).click()
  await expect(paymentsPanel.getByRole('heading', { name: 'Редактирование способа оплаты' })).toBeVisible()
  await expect(paymentsPanel.getByRole('textbox', { name: /^Секретный ключ/ })).toHaveValue('')
  await expect(paymentsPanel.getByRole('textbox', { name: /^Секрет webhook/ })).toHaveValue('')
  await paymentsPanel.getByLabel('Название для пользователя').fill('YooKassa CRUD Updated')
  await paymentsPanel.getByRole('button', { name: 'Сохранить изменения' }).click()
  await expect(page.getByText('Способ оплаты yookassa-crud-e2e обновлен. Секреты не отображаются.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/payment-providers/accounts/provider-created-e2e', 'PATCH')?.body).toMatchObject({
    publicName: 'YooKassa CRUD Updated',
    secretKey: '',
    webhookSecret: ''
  })

  providerRow = paymentsPanel.locator('.list-item-vertical').filter({ hasText: 'YooKassa CRUD Updated' })
  await providerRow.getByRole('button', { name: 'Выключить' }).click()
  await paymentsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('yookassa-crud-e2e: выключен')).toBeVisible()
  await expect(providerRow.getByRole('button', { name: 'Включить' })).toBeVisible()
  expect(api.getLastRequest('/api/admin/payment-providers/accounts/provider-created-e2e/enabled')?.body).toEqual({ enabled: false })

  await page.getByRole('button', { name: 'Обновить данные' }).click()
  providerRow = paymentsPanel.locator('.list-item-vertical').filter({ hasText: 'YooKassa CRUD Updated' })
  await expect(providerRow.getByRole('button', { name: 'Включить' })).toBeVisible()
  await providerRow.getByRole('button', { name: 'Включить' }).click()
  await expect(page.getByText('yookassa-crud-e2e: включен')).toBeVisible()
  await expect(providerRow.getByRole('button', { name: 'Выключить' })).toBeVisible()

  await providerRow.getByRole('button', { name: 'Проверить настройки' }).click()
  await expect(providerRow.getByText('Настройки готовы')).toBeVisible()
  expect(api.getLastRequest('/api/admin/payment-providers/accounts/provider-created-e2e/check')).toBeTruthy()
  expect(api.getRequestCount('/api/admin/payment-providers/accounts/provider-created-e2e/enabled', 'POST')).toBe(2)
  expect(api.getAuthorizedRequestCount('/api/admin/payment-providers/accounts/provider-created-e2e', 'PATCH', 'Bearer admin-provider-token')).toBe(1)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('admin Telegram bot settings support secure save and reload lifecycle', async ({ page }) => {
  test.slow()
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))

  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-bot-token', 'admin-bot-refresh')
  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'Telegram-бот', 'bot')

  const botPanel = page.locator('#bot')
  await botPanel.getByLabel('Состояние').selectOption('true')
  await botPanel.getByLabel('Режим').selectOption('Webhook')
  await botPanel.getByLabel('Username публичного бота').fill('vpnplatform_e2e_bot')
  await botPanel.getByLabel('URL webhook').fill('https://api.example.test/api/channels/telegram/webhook')
  await botPanel.getByLabel('ID админского чата').fill('-100777001')
  await botPanel.getByLabel('URL WebApp').fill('https://cabinet.example.test')
  await botPanel.getByRole('textbox', { name: /^Токен бота/ }).fill('e2e-bot-token-value')
  await botPanel.getByRole('textbox', { name: /^Секрет webhook/ }).fill('e2e-bot-webhook-value')
  await botPanel.getByLabel('Приветствие').fill('Добро пожаловать в E2E-бота')
  await botPanel.getByLabel('Текст поддержки').fill('Поддержка E2E доступна в кабинете')
  await botPanel.getByRole('button', { name: 'Сохранить настройки бота' }).click()

  await expect(page.getByText('Настройки Telegram-бота сохранены. Токены остаются скрытыми и не возвращаются из API.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/telegram-bot/settings', 'PATCH')?.body).toMatchObject({
    enabled: true,
    mode: 'Webhook',
    publicBotUsername: 'vpnplatform_e2e_bot',
    botToken: 'e2e-bot-token-value',
    secretToken: 'e2e-bot-webhook-value',
    welcomeText: 'Добро пожаловать в E2E-бота',
    supportText: 'Поддержка E2E доступна в кабинете'
  })
  await expect(page.getByText('e2e-bot-token-value', { exact: true })).toHaveCount(0)
  await expect(page.getByText('e2e-bot-webhook-value', { exact: true })).toHaveCount(0)
  await expect(botPanel.getByRole('textbox', { name: /^Токен бота/ })).toHaveValue('')
  await expect(botPanel.getByRole('textbox', { name: /^Секрет webhook/ })).toHaveValue('')

  await botPanel.getByRole('button', { name: 'Проверить подключение' }).click()
  await expect(page.getByText('Telegram-бот готов к работе.')).toBeVisible()
  await expect(botPanel.getByText('Обязательные настройки заполнены. Можно проверять реальный диалог с ботом в Telegram.')).toBeVisible()

  await page.getByRole('button', { name: 'Обновить данные' }).click()
  await expect(botPanel.getByLabel('Состояние')).toHaveValue('true')
  await expect(botPanel.getByLabel('Режим')).toHaveValue('Webhook')
  await expect(botPanel.getByLabel('Username публичного бота')).toHaveValue('vpnplatform_e2e_bot')
  await expect(botPanel.getByLabel('Приветствие')).toHaveValue('Добро пожаловать в E2E-бота')
  await expect(botPanel.getByRole('textbox', { name: /^Токен бота/ })).toHaveValue('')

  await botPanel.getByLabel('Приветствие').fill('Обновленное приветствие E2E')
  await botPanel.getByRole('button', { name: 'Сохранить настройки бота' }).click()
  expect(api.getLastRequest('/api/admin/telegram-bot/settings', 'PATCH')?.body).toMatchObject({
    welcomeText: 'Обновленное приветствие E2E',
    botToken: '',
    secretToken: ''
  })
  expect(api.getRequestCount('/api/admin/telegram-bot/settings', 'PATCH')).toBe(2)
  expect(api.getAuthorizedRequestCount('/api/admin/telegram-bot/settings', 'PATCH', 'Bearer admin-bot-token')).toBe(2)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('admin VPN infrastructure supports secure managed lifecycle', async ({ page }) => {
  test.setTimeout(180_000)
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))

  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-infrastructure-token', 'admin-infrastructure-refresh')
  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()

  await openAdminSection(page, 'Серверы', 'nodes')
  const nodesPanel = page.locator('#nodes')
  const serverForm = nodesPanel.locator('form')
  await serverForm.getByLabel('Название').fill('E2E NL Node')
  await serverForm.getByLabel('Host или DNS').fill('nl-node.example.test')
  await serverForm.getByLabel('IP-адрес').fill('192.0.2.44')
  await serverForm.getByLabel('Провайдер').fill('E2E Provider')
  await serverForm.getByLabel('Регион').fill('EU')
  await serverForm.getByLabel('Страна').fill('NL')
  await serverForm.getByLabel('Дата-центр').fill('AMS-E2E')
  await serverForm.getByLabel('Емкость').fill('250')
  await serverForm.getByLabel('Приоритет').fill('25')
  await serverForm.getByLabel('Протоколы').fill('vless,trojan')
  await serverForm.getByRole('textbox', { name: /^SSH-доступ/ }).fill('playwright-ssh-write-only')
  await serverForm.getByRole('textbox', { name: /^Пароль панели/ }).fill('playwright-panel-write-only')
  await serverForm.getByRole('button', { name: 'Создать сервер' }).click()

  await expect(page.getByText('Сервер E2E NL Node создан. Секреты не возвращаются из API.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/servers', 'POST')?.body).toMatchObject({
    name: 'E2E NL Node',
    host: 'nl-node.example.test',
    sshCredential: 'playwright-ssh-write-only',
    panelPassword: 'playwright-panel-write-only'
  })
  await expect(page.getByText('playwright-ssh-write-only', { exact: true })).toHaveCount(0)
  await expect(page.getByText('playwright-panel-write-only', { exact: true })).toHaveCount(0)
  await expect(serverForm.getByRole('textbox', { name: /^SSH-доступ/ })).toHaveValue('')
  await expect(serverForm.getByRole('textbox', { name: /^Пароль панели/ })).toHaveValue('')

  await page.getByRole('button', { name: 'Обновить данные' }).click()
  let serverRow = nodesPanel.locator('.list-item-vertical').filter({ hasText: 'E2E NL Node' })
  await expect(serverRow).toContainText('EU/NL · E2E Provider · nl-node.example.test')
  await serverRow.getByRole('button', { name: 'Редактировать' }).click()
  await expect(serverForm.getByLabel('Название')).toHaveValue('E2E NL Node')
  await expect(serverForm.getByRole('textbox', { name: /^SSH-доступ/ })).toHaveValue('')
  await expect(serverForm.getByRole('textbox', { name: /^Пароль панели/ })).toHaveValue('')
  await serverForm.getByLabel('Название').fill('E2E NL Node Updated')
  await serverForm.getByLabel('Приоритет').fill('30')
  await serverForm.getByRole('button', { name: 'Сохранить сервер' }).click()

  await expect(page.getByText('Сервер E2E NL Node Updated обновлен. Секреты не возвращаются из API.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/servers/server-created-e2e', 'PUT')?.body).toMatchObject({
    name: 'E2E NL Node Updated',
    priority: 30,
    sshCredential: '',
    panelPassword: ''
  })
  serverRow = nodesPanel.locator('.list-item-vertical').filter({ hasText: 'E2E NL Node Updated' })
  await serverRow.getByRole('button', { name: 'Закрыть набор' }).click()
  await nodesPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(serverRow).toContainText('новые пользователи: закрыты')
  await serverRow.getByRole('button', { name: 'Открыть набор' }).click()
  await nodesPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(serverRow).toContainText('новые пользователи: разрешены')
  await serverRow.getByRole('button', { name: 'В обслуживание' }).click()
  await nodesPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Сервер E2E NL Node Updated: перевести в обслуживание.')).toBeVisible()
  await serverRow.getByRole('button', { name: 'Вернуть в работу' }).click()
  await expect(page.getByText('Сервер E2E NL Node Updated: вернуть в работу.')).toBeVisible()
  await serverRow.getByRole('button', { name: 'Отключить' }).click()
  await nodesPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Сервер E2E NL Node Updated: отключить сервер.')).toBeVisible()
  await serverRow.getByRole('button', { name: 'Удалить' }).click()
  await nodesPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Сервер E2E NL Node Updated удалён.')).toBeVisible()
  await expect(nodesPanel.getByText('E2E NL Node Updated', { exact: true })).toHaveCount(0)

  await openAdminSection(page, '3x-ui панели', 'panels')
  const panelsPanel = page.locator('#panels')
  const panelForm = panelsPanel.locator('form').first()
  await panelForm.getByLabel('Название панели').fill('E2E 3x-ui Panel')
  await panelForm.getByLabel('Адрес панели').fill('https://panel-created.example.test')
  await panelForm.getByLabel('Логин').fill('e2e-admin')
  await panelForm.getByRole('textbox', { name: /^Пароль панели/ }).fill('playwright-3xui-write-only')
  await panelForm.getByLabel('Регион').fill('NL')
  await panelForm.getByLabel('Емкость').fill('300')
  await panelForm.getByRole('button', { name: 'Добавить панель' }).click()

  await expect(page.getByText('VPN-панель E2E 3x-ui Panel сохранена. Пароль не возвращается из API.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/vpn-panels', 'POST')?.body).toMatchObject({
    name: 'E2E 3x-ui Panel',
    password: 'playwright-3xui-write-only',
    region: 'NL'
  })
  await expect(page.getByText('playwright-3xui-write-only', { exact: true })).toHaveCount(0)
  await expect(panelForm.getByRole('textbox', { name: /^Пароль панели/ })).toHaveValue('')

  await page.getByRole('button', { name: 'Обновить данные' }).click()
  let panelRow = panelsPanel.locator('.list-item-vertical').filter({ hasText: 'E2E 3x-ui Panel' })
  await expect(panelRow).toContainText('https://panel-created.example.test')
  await panelRow.getByRole('button', { name: 'Редактировать' }).click()
  await expect(panelForm.getByLabel('Название панели')).toHaveValue('E2E 3x-ui Panel')
  await expect(panelForm.getByRole('textbox', { name: /^Пароль панели/ })).toHaveValue('')
  await panelForm.getByLabel('Название панели').fill('E2E 3x-ui Panel Updated')
  await panelForm.getByRole('button', { name: 'Сохранить панель' }).click()
  await expect(page.getByText('VPN-панель E2E 3x-ui Panel Updated обновлена. Пароль не возвращается из API.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/vpn-panels/panel-created-e2e', 'PATCH')?.body).toMatchObject({
    name: 'E2E 3x-ui Panel Updated',
    password: ''
  })

  panelRow = panelsPanel.locator('.list-item-vertical').filter({ hasText: 'E2E 3x-ui Panel Updated' })
  await expect(panelsPanel.getByRole('combobox', { name: 'Панель' })).toHaveValue('panel-created-e2e')
  const inboundForm = panelsPanel.locator('form').nth(1)
  await inboundForm.getByLabel('Название inbound-правила').fill('e2e-vless')
  await inboundForm.getByLabel('Порт').fill('9443')
  await inboundForm.getByLabel('Емкость').fill('200')
  await inboundForm.getByLabel('Основной inbound для панели').uncheck()
  await inboundForm.getByRole('textbox', { name: 'settingsJson', exact: true }).fill('{"clients":[]}')
  await inboundForm.getByRole('textbox', { name: 'streamSettingsJson', exact: true }).fill('{"network":"tcp","security":"reality"}')
  await inboundForm.getByRole('textbox', { name: 'sniffingJson', exact: true }).fill('{}')
  await inboundForm.getByRole('button', { name: 'Создать inbound-правило' }).click()

  await expect(page.getByText('Inbound-правило e2e-vless создано.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/vpn-panels/panel-created-e2e/inbounds', 'POST')?.body).toMatchObject({ name: 'e2e-vless', port: 9443, capacity: 200 })
  let inboundRow = panelsPanel.locator('.list-item-vertical').filter({ hasText: 'e2e-vless' })
  await inboundRow.getByRole('button', { name: 'Редактировать' }).click()
  await inboundForm.getByLabel('Название inbound-правила').fill('e2e-vless-updated')
  await inboundForm.getByLabel('Порт').fill('10443')
  await inboundForm.getByRole('button', { name: 'Сохранить inbound-правило' }).click()
  await expect(page.getByText('Inbound-правило e2e-vless-updated обновлено.')).toBeVisible()
  inboundRow = panelsPanel.locator('.list-item-vertical').filter({ hasText: 'e2e-vless-updated' })
  await inboundRow.getByRole('button', { name: 'Сделать основным' }).click()
  await expect(inboundRow).toContainText('Основной')
  await inboundRow.getByRole('button', { name: 'Выключить' }).click()
  await panelsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(inboundRow.getByRole('button', { name: 'Включить' })).toBeVisible()
  await inboundRow.getByRole('button', { name: 'Включить' }).click()
  await expect(inboundRow.getByRole('button', { name: 'Выключить' })).toBeVisible()

  await panelRow.getByRole('button', { name: 'Отключить' }).click()
  await panelsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(panelRow.getByRole('button', { name: 'Включить' })).toBeVisible()
  await panelRow.getByRole('button', { name: 'Включить' }).click()
  await expect(panelRow.getByRole('button', { name: 'Отключить' })).toBeVisible()
  await panelRow.getByRole('button', { name: 'Удалить' }).click()
  await panelsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Панель E2E 3x-ui Panel Updated отключена и сохранена в истории: связей 1.')).toBeVisible()
  await expect(panelRow.getByRole('button', { name: 'Включить' })).toBeVisible()

  expect(api.getAuthorizedRequestCount('/api/admin/servers', 'POST', 'Bearer admin-infrastructure-token')).toBe(1)
  expect(api.getAuthorizedRequestCount('/api/admin/vpn-panels', 'POST', 'Bearer admin-infrastructure-token')).toBe(1)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('admin provisioning supports safe validation lifecycle', async ({ page }) => {
  test.setTimeout(150_000)
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))

  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-provisioning-token', 'admin-provisioning-refresh')
  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()

  await openAdminSection(page, 'Серверы', 'nodes')
  const nodesPanel = page.locator('#nodes')
  const serverRow = nodesPanel.locator('.list-item-vertical').filter({ hasText: 'EU Sandbox' }).first()
  await serverRow.getByRole('button', { name: 'Health-check' }).click()
  await expect(page.getByText('Health-check EU Sandbox: Healthy')).toBeVisible()
  expect(api.getLastRequest('/api/admin/servers/server-eu/health-check', 'POST')?.authorization).toBe('Bearer admin-provisioning-token')

  await serverRow.getByRole('button', { name: 'Precheck VPS' }).click()
  await expect(page.getByText('Проверка поставлена в очередь. Режим: Dry-run precheck. ID запуска: provisioning-precheck-created-e2e')).toBeVisible()
  expect(api.getLastRequest('/api/admin/servers/server-eu/precheck', 'POST')?.body).toEqual({})

  await serverRow.getByRole('button', { name: 'Подготовить' }).click()
  await expect(nodesPanel.getByRole('dialog')).toContainText('Validation deploy')
  await expect(nodesPanel.getByRole('dialog')).toContainText('не меняет рабочую инфраструктуру')
  await nodesPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Подготовка сервера поставлена в очередь. Режим: Validation deploy; риск: низкий риск. ID запуска: provisioning-direct-created-e2e')).toBeVisible()
  expect(api.getLastRequest('/api/admin/servers/server-eu/provision', 'POST')?.body).toEqual({ dryRun: false })

  await openAdminSection(page, 'Подготовка VPS', 'provisioning')
  const provisioningPanel = page.locator('#provisioning')
  let precheckRow = provisioningPanel.locator('.list-item-vertical').filter({ hasText: 'EU Sandbox Precheck E2E' })
  await expect(precheckRow).toContainText('Stateful browser precheck ready.')
  await expect(precheckRow.getByRole('button', { name: 'Повторить' })).toBeDisabled()
  await expect(precheckRow.getByRole('button', { name: 'Развернуть' })).toBeEnabled()
  await expect(precheckRow.getByRole('button', { name: 'Отменить' })).toBeEnabled()

  await precheckRow.getByRole('button', { name: 'Развернуть' }).click()
  await expect(provisioningPanel.getByRole('dialog')).toContainText('Validation deploy')
  await expect(provisioningPanel.getByRole('dialog')).toContainText('не меняет рабочую инфраструктуру')
  await provisioningPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Развертывание поставлено в очередь. Режим: Validation deploy; риск: низкий риск. ID запуска: provisioning-precheck-created-e2e')).toBeVisible()
  expect(api.getLastRequest('/api/admin/provisioning-runs/provisioning-precheck-created-e2e/deploy', 'POST')?.body).toEqual({})

  precheckRow = provisioningPanel.locator('.list-item-vertical').filter({ hasText: 'EU Sandbox Precheck E2E' })
  await expect(precheckRow.getByRole('button', { name: 'Развернуть' })).toBeDisabled()
  await precheckRow.getByRole('button', { name: 'Отменить' }).click()
  await provisioningPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Запуск подготовки сервера отменен.')).toBeVisible()
  await expect(precheckRow.getByRole('button', { name: 'Повторить' })).toBeEnabled()

  await precheckRow.getByRole('button', { name: 'Повторить' }).click()
  await expect(page.getByText('Повтор поставлен в очередь. Режим: Validation deploy. Новый ID запуска: provisioning-precheck-created-e2e')).toBeVisible()
  expect(api.getLastRequest('/api/admin/provisioning-runs/provisioning-precheck-created-e2e/retry', 'POST')?.authorization).toBe('Bearer admin-provisioning-token')
  await precheckRow.getByRole('button', { name: 'Нужна поддержка' }).click()
  await expect(page.getByText('Обращение в поддержку: support-provisioning-e2e')).toBeVisible()

  await page.getByRole('button', { name: 'Обновить данные' }).click()
  precheckRow = provisioningPanel.locator('.list-item-vertical').filter({ hasText: 'EU Sandbox Precheck E2E' })
  await expect(precheckRow).toContainText('Попытка 1')
  await expect(provisioningPanel.locator('.list-item-vertical').filter({ hasText: 'EU Sandbox Validation E2E' })).toBeVisible()
  expect(api.getAuthorizedRequestCount('/api/admin/provisioning-runs/provisioning-precheck-created-e2e/support-needed', 'POST', 'Bearer admin-provisioning-token')).toBe(1)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('admin 3x-ui client actions persist across reload', async ({ page }) => {
  test.setTimeout(120_000)
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))

  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-vpn-client-token', 'admin-vpn-client-refresh')
  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, '3x-ui панели', 'panels')

  const panelsPanel = page.locator('#panels')
  await expect(panelsPanel.getByRole('combobox', { name: 'Панель' })).toHaveValue('panel-eu')
  let clientRow = panelsPanel.locator('.list-item-vertical').filter({ hasText: 'client@example.test' })
  await expect(clientRow.getByRole('button', { name: 'Отключить' })).toBeVisible()

  await clientRow.getByRole('button', { name: 'Отключить' }).click()
  await panelsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('VPN-клиент client@example.test обновлен: disable.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/vpn-clients/client-e2e/disable', 'POST')?.body).toEqual({})
  await expect(clientRow.getByRole('button', { name: 'Включить' })).toBeVisible()

  await page.getByRole('button', { name: 'Обновить данные' }).click()
  clientRow = panelsPanel.locator('.list-item-vertical').filter({ hasText: 'client@example.test' })
  await expect(clientRow.getByRole('button', { name: 'Включить' })).toBeVisible()
  await expect(clientRow).toContainText('Синхронизация: disable')

  await clientRow.getByRole('button', { name: 'Включить' }).click()
  await expect(page.getByText('VPN-клиент client@example.test обновлен: enable.')).toBeVisible()
  await expect(clientRow.getByRole('button', { name: 'Отключить' })).toBeVisible()

  await clientRow.getByRole('button', { name: 'Синхронизировать' }).click()
  await expect(page.getByText('VPN-клиент client@example.test обновлен: synced.')).toBeVisible()
  await expect(clientRow).toContainText('Синхронизация: synced')

  await clientRow.getByRole('button', { name: 'Сбросить трафик' }).click()
  await expect(panelsPanel.getByRole('dialog')).toContainText('ручной сверки')
  await panelsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('VPN-клиент client@example.test обновлен: traffic-reset.')).toBeVisible()
  await expect(clientRow).toContainText('Синхронизация: traffic-reset')

  for (const action of ['disable', 'enable', 'sync', 'reset-traffic']) {
    expect(api.getAuthorizedRequestCount(`/api/admin/vpn-clients/client-e2e/${action}`, 'POST', 'Bearer admin-vpn-client-token')).toBe(1)
  }
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('admin subscription operations persist lifecycle across reload', async ({ page }) => {
  test.setTimeout(150_000)
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))

  const api = await mockAdminApi(page)
  api.prepareSubscriptionLifecycle()
  await seedAdminSession(page, 'admin-subscription-token', 'admin-subscription-refresh')
  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'Подписки', 'subscriptions')

  const subscriptionsPanel = page.locator('#subscriptions')
  let subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(subscriptionRow).toContainText('PendingActivation')
  await expect(subscriptionRow).toContainText('Доступа нет')
  await subscriptionRow.getByRole('button', { name: 'Активировать' }).click()
  await expect(page.getByText('Подписка активирована, текущий VPN-доступ включен при наличии.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/subscriptions/sub-e2e/activate')?.body).toEqual({ reason: 'manual_subscription_activate' })

  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(subscriptionRow).toContainText('Активно')
  await expect(subscriptionRow).toContainText('Доступ привязан')
  await subscriptionRow.getByRole('spinbutton').fill('45')
  await subscriptionRow.getByRole('button', { name: 'Продлить' }).click()
  await expect(page.getByText('Подписка продлена на 45 дней.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/subscriptions/sub-e2e/extend')?.body).toEqual({ days: 45, reason: 'manual_admin_extend' })
  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(subscriptionRow).toContainText('продлений: 1')

  await subscriptionRow.getByRole('button', { name: 'Синхронизировать доступ' }).click()
  await expect(page.getByText('Текущий VPN-доступ подписки синхронизирован.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/subscriptions/sub-e2e/sync-access')?.body).toEqual({ reason: 'manual_subscription_sync' })

  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await subscriptionRow.getByRole('button', { name: 'Заблокировать' }).click()
  await subscriptionsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(subscriptionRow).toContainText('Заблокировано')
  await expect(subscriptionRow).toContainText('manual_admin_action')

  await page.reload()
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'Подписки', 'subscriptions')
  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(subscriptionRow).toContainText('Заблокировано')
  await expect(subscriptionRow).toContainText('продлений: 1')
  await subscriptionRow.getByRole('button', { name: 'Разблокировать' }).click()
  await subscriptionsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(subscriptionRow).toContainText('Активно')

  await subscriptionRow.getByRole('combobox', { name: /Целевой сервер для миграции/ }).selectOption('auto')
  await subscriptionRow.getByRole('button', { name: 'Перенести', exact: true }).click()
  await subscriptionsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText(/Подписка перенесена на сервер server-u/)).toBeVisible()
  expect(api.getLastRequest('/api/admin/subscriptions/sub-e2e/migrate')?.body).toBeNull()
  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(subscriptionRow).toContainText('сервер: server-u')

  await page.reload()
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'VPN-доступы', 'vpn')
  let accessRow = page.locator('#vpn .list-item-vertical').filter({ hasText: 'client-e2e' })
  await expect(accessRow).toContainText('US Sandbox')
  await expect(accessRow).toContainText('версия: 6')

  await openAdminSection(page, 'Подписки', 'subscriptions')
  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await subscriptionRow.getByRole('button', { name: 'Отменить' }).click()
  await expect(subscriptionsPanel.getByRole('dialog')).toContainText('VPN-доступ будет отозван и удален с сервера')
  await subscriptionsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Подписка отменена, VPN-доступ отозван и удален с сервера.')).toBeVisible()

  await page.reload()
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'Подписки', 'subscriptions')
  subscriptionRow = subscriptionsPanel.locator('.list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(subscriptionRow).toContainText('Отменено')
  await expect(subscriptionRow).toContainText('Доступа нет')
  await expect(subscriptionRow.getByRole('button')).toHaveCount(0)

  await openAdminSection(page, 'VPN-доступы', 'vpn')
  accessRow = page.locator('#vpn .list-item-vertical').filter({ hasText: 'access-e' })
  await expect(accessRow).toContainText('Доступ отозван.')
  await expect(accessRow).toContainText('версия: 7')
  await expect(accessRow).not.toContainText('client-e2e')
  await expect(accessRow).not.toContainText('vless://admin-e2e@example.test')
  await expect(accessRow.getByRole('button')).toHaveCount(0)

  for (const action of ['activate', 'extend', 'sync-access', 'block', 'unblock', 'migrate', 'cancel']) {
    expect(api.getAuthorizedRequestCount(`/api/admin/subscriptions/sub-e2e/${action}`, 'POST', 'Bearer admin-subscription-token')).toBe(1)
  }
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('admin VPN access actions persist status and revision across reload', async ({ page }) => {
  test.setTimeout(120_000)
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))

  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-access-token', 'admin-access-refresh')
  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()
  await openAdminSection(page, 'VPN-доступы', 'vpn')

  const vpnPanel = page.locator('#vpn')
  let accessRow = vpnPanel.locator('.list-item-vertical').filter({ hasText: 'vless://admin-e2e@example.test' })
  await expect(accessRow).toContainText('версия: 1')
  await accessRow.getByRole('button', { name: 'Отключить' }).click()
  await vpnPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('VPN-доступ отключен.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/access-credentials/access-e2e/disable', 'POST')?.body).toEqual({ reason: 'manual_admin_action' })

  accessRow = vpnPanel.locator('.list-item-vertical').filter({ hasText: 'vless://admin-e2e@example.test' })
  await expect(accessRow.getByRole('button', { name: 'Включить' })).toBeVisible()
  await expect(accessRow).toContainText('версия: 2')
  await page.getByRole('button', { name: 'Обновить данные' }).click()
  accessRow = vpnPanel.locator('.list-item-vertical').filter({ hasText: 'vless://admin-e2e@example.test' })
  await expect(accessRow.getByRole('button', { name: 'Включить' })).toBeVisible()
  await expect(accessRow).toContainText('версия: 2')

  await accessRow.getByRole('button', { name: 'Включить' }).click()
  await expect(page.getByText('VPN-доступ включен.')).toBeVisible()
  await expect(accessRow.getByRole('button', { name: 'Отключить' })).toBeVisible()
  await expect(accessRow).toContainText('версия: 3')
  expect(api.getLastRequest('/api/admin/access-credentials/access-e2e/enable', 'POST')?.body).toEqual({ reason: 'manual_admin_action' })

  await accessRow.getByRole('button', { name: 'Синхронизировать' }).click()
  await expect(page.getByText('VPN-доступ синхронизирован.')).toBeVisible()
  await expect(accessRow).toContainText('версия: 4')
  expect(api.getLastRequest('/api/admin/access-credentials/access-e2e/sync', 'POST')?.body).toEqual({ reason: 'manual_admin_sync' })

  await accessRow.getByRole('button', { name: 'Сбросить трафик' }).click()
  await expect(vpnPanel.getByRole('dialog')).toContainText('SyncRequired')
  await vpnPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Трафик VPN-доступа сброшен.')).toBeVisible()
  await expect(accessRow).toContainText('версия: 5')
  expect(api.getLastRequest('/api/admin/access-credentials/access-e2e/reset-traffic', 'POST')?.body).toEqual({ reason: 'manual_admin_reset_traffic' })

  for (const action of ['disable', 'enable', 'sync', 'reset-traffic']) {
    expect(api.getAuthorizedRequestCount(`/api/admin/access-credentials/access-e2e/${action}`, 'POST', 'Bearer admin-access-token')).toBe(1)
  }
  await expect(vpnPanel.getByText('vless://revoked-admin-secret@example.test', { exact: true })).toHaveCount(0)
  await expect(vpnPanel.getByText('vless://cancelled-access-stale-secret@example.test', { exact: true })).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('admin managed configuration supports complete CRUD lifecycle', async ({ page }) => {
  test.setTimeout(180_000)
  const browserErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') browserErrors.push(message.text())
  })
  page.on('pageerror', (error) => browserErrors.push(error.message))

  const api = await mockAdminApi(page)
  await seedAdminSession(page, 'admin-content-token', 'admin-content-refresh')
  await page.goto('/')
  await expect(page.locator('.admin-shell')).toBeVisible()

  await openAdminSection(page, 'Тарифы', 'tariffs')
  const tariffsPanel = page.locator('#tariffs')
  await tariffsPanel.getByLabel('Название').fill('CRUD Tariff E2E')
  await tariffsPanel.getByLabel('Slug').fill('crud-tariff-e2e')
  await tariffsPanel.getByRole('spinbutton', { name: 'Цена' }).fill('690')
  await tariffsPanel.getByLabel('Короткое описание').fill('Тариф полного браузерного CRUD.')
  await tariffsPanel.getByLabel('Преимущества, по одному в строке').fill('CRUD\nDesktop и mobile')
  await tariffsPanel.getByRole('button', { name: 'Создать тариф' }).click()
  await expect(page.getByText('Тариф создан.')).toBeVisible()

  let tariffRow = tariffsPanel.locator('.list-item-vertical').filter({ hasText: 'CRUD Tariff E2E' })
  await expect(tariffRow).toContainText('690 RUB')
  await tariffRow.getByRole('button', { name: 'Редактировать' }).click()
  await tariffsPanel.getByLabel('Название').fill('CRUD Tariff Updated')
  await tariffsPanel.getByRole('button', { name: 'Сохранить тариф' }).click()
  await expect(page.getByText('Тариф обновлён.')).toBeVisible()
  tariffRow = tariffsPanel.locator('.list-item-vertical').filter({ hasText: 'CRUD Tariff Updated' })
  await tariffRow.getByRole('button', { name: 'Выключить' }).click()
  await tariffsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(tariffRow).toContainText('Выключено')
  await expect(tariffRow.getByRole('button', { name: 'Включить' })).toBeVisible()
  expect(api.getLastRequest('/api/admin/tariffs/tariff-created-e2e', 'PATCH')?.body).toMatchObject({ isActive: false })
  await tariffRow.getByRole('button', { name: 'Удалить' }).click()
  await tariffsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(tariffsPanel.getByText('CRUD Tariff Updated', { exact: true })).toHaveCount(0)
  expect(api.getLastRequest('/api/admin/tariffs/tariff-created-e2e', 'DELETE')).toBeTruthy()

  await openAdminSection(page, 'Рефералы', 'referrals')
  const referralsPanel = page.locator('#referrals')
  await referralsPanel.getByLabel('Название').fill('CRUD Referral E2E')
  await referralsPanel.locator('form select').first().selectOption('active')
  await referralsPanel.getByRole('button', { name: 'Создать программу' }).click()
  await expect(page.getByText('Реферальная программа создана.')).toBeVisible()
  let referralRow = referralsPanel.locator('.list-item-vertical').filter({ hasText: 'CRUD Referral E2E' })
  await referralRow.getByRole('button', { name: 'Редактировать' }).click()
  await referralsPanel.getByLabel('Название').fill('CRUD Referral Updated')
  await referralsPanel.getByRole('button', { name: 'Сохранить программу' }).click()
  await expect(page.getByText('Реферальная программа обновлена.')).toBeVisible()
  referralRow = referralsPanel.locator('.list-item-vertical').filter({ hasText: 'CRUD Referral Updated' })
  await expect(referralRow).toBeVisible()
  expect(api.getLastRequest('/api/admin/referral-programs/referral-program-created-e2e', 'PATCH')?.body).toMatchObject({ name: 'CRUD Referral Updated' })

  await openAdminSection(page, 'Сценарии', 'scenarios')
  const scenariosPanel = page.locator('#scenarios')
  await scenariosPanel.getByLabel('Название').fill('CRUD Scenario E2E')
  await scenariosPanel.getByLabel('Ключ').fill('crud-scenario-e2e')
  await scenariosPanel.getByLabel('VPN-протокол').fill('vless')
  await scenariosPanel.getByLabel('Текст для кабинета').fill('Сценарий полного браузерного CRUD.')
  await scenariosPanel.getByRole('button', { name: 'Создать сценарий' }).click()
  await expect(page.getByText('Сценарий работы создан.')).toBeVisible()
  let scenarioRow = scenariosPanel.locator('.list-item-vertical').filter({ hasText: 'CRUD Scenario E2E' })
  await scenarioRow.getByRole('button', { name: 'Редактировать' }).click()
  await scenariosPanel.getByLabel('Название').fill('CRUD Scenario Updated')
  await scenariosPanel.getByRole('button', { name: 'Сохранить сценарий' }).click()
  await expect(page.getByText('Сценарий работы обновлен.')).toBeVisible()
  scenarioRow = scenariosPanel.locator('.list-item-vertical').filter({ hasText: 'CRUD Scenario Updated' })
  await scenarioRow.getByRole('button', { name: 'Удалить' }).click()
  await scenariosPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(scenariosPanel.getByText('CRUD Scenario Updated', { exact: true })).toHaveCount(0)
  expect(api.getLastRequest('/api/admin/work-scenarios/scenario-created-e2e', 'DELETE')).toBeTruthy()

  await openAdminSection(page, 'Что нового', 'releases')
  const releasesPanel = page.locator('#releases')
  await releasesPanel.getByLabel('Release ID').fill('2026-08-10-admin-crud-e2e')
  await releasesPanel.getByLabel('Версия').fill('0.554.0-test')
  await releasesPanel.getByLabel('Дата публикации').fill('2026-08-10T07:00')
  await releasesPanel.getByLabel('Заголовок').fill('CRUD Release E2E')
  await releasesPanel.getByLabel('Короткое описание').fill('Релиз полного браузерного CRUD.')
  await releasesPanel.getByLabel('Текст').fill('Создание, обновление и удаление релиза.')
  await releasesPanel.getByRole('button', { name: 'Создать релиз' }).click()
  await expect(page.getByText('Релиз создан.')).toBeVisible()
  let releaseRow = releasesPanel.locator('.list-item-vertical').filter({ hasText: 'CRUD Release E2E' })
  await releaseRow.getByRole('button', { name: 'Редактировать' }).click()
  await releasesPanel.getByLabel('Заголовок').fill('CRUD Release Updated')
  await releasesPanel.getByRole('button', { name: 'Сохранить релиз' }).click()
  await expect(page.getByText('Релиз обновлен.')).toBeVisible()
  releaseRow = releasesPanel.locator('.list-item-vertical').filter({ hasText: 'CRUD Release Updated' })
  await releaseRow.getByRole('button', { name: 'Удалить' }).click()
  await releasesPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(releasesPanel.getByText('CRUD Release Updated', { exact: true })).toHaveCount(0)
  expect(api.getLastRequest('/api/app-version/admin/releases/release-created-e2e', 'DELETE')).toBeTruthy()

  await openAdminSection(page, 'FAQ', 'faq')
  const faqPanel = page.locator('#faq')
  const faqForm = faqPanel.locator('form').first()
  await faqForm.getByLabel('Вопрос').fill('Как проверить управляемый FAQ?')
  await faqForm.getByLabel('Ответ').fill('Создать, изменить и удалить запись через админку.')
  await faqForm.getByLabel('Категория').fill('E2E')
  await faqForm.getByRole('button', { name: 'Создать вопрос' }).click()
  await expect(page.getByText('Вопрос FAQ создан.')).toBeVisible()

  let faqRow = faqPanel.locator('.list-item-vertical').filter({ hasText: 'Как проверить управляемый FAQ?' })
  await expect(faqRow).toContainText('Создать, изменить и удалить запись через админку.')
  expect(api.getLastRequest('/api/admin/faq', 'POST')?.body).toMatchObject({ category: 'E2E', showOnFaqPage: true })

  await faqRow.getByRole('button', { name: 'Редактировать' }).click()
  await expect(faqPanel.getByRole('heading', { name: 'Редактировать вопрос' })).toBeVisible()
  await faqForm.getByLabel('Ответ').fill('Обновлённый ответ проверен браузерным E2E.')
  await faqForm.getByRole('button', { name: 'Сохранить вопрос' }).click()
  await expect(page.getByText('Вопрос FAQ обновлен.')).toBeVisible()
  faqRow = faqPanel.locator('.list-item-vertical').filter({ hasText: 'Как проверить управляемый FAQ?' })
  await expect(faqRow).toContainText('Обновлённый ответ проверен браузерным E2E.')
  expect(api.getLastRequest('/api/admin/faq/faq-created-e2e', 'PUT')?.body).toMatchObject({ answer: 'Обновлённый ответ проверен браузерным E2E.' })

  await faqRow.getByRole('button', { name: 'Удалить' }).click()
  await faqPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Вопрос FAQ удален.')).toBeVisible()
  await expect(faqPanel.getByText('FAQ пока пуст')).toBeVisible()
  expect(api.getLastRequest('/api/admin/faq/faq-created-e2e', 'DELETE')).toBeTruthy()

  await openAdminSection(page, 'Контент сайта', 'content')
  const contentPanel = page.locator('#content')
  const contentForm = contentPanel.locator('form')
  await contentForm.getByLabel('Ключ').fill('home.e2e.title')
  await contentForm.getByLabel('Название поля').fill('Заголовок E2E')
  await contentForm.getByLabel('Описание для администратора').fill('Проверка CRUD контента через браузер.')
  await contentForm.getByLabel('Значение').fill('Управляемый блок E2E')
  await contentForm.getByRole('button', { name: 'Создать блок' }).click()
  await expect(page.getByText('Блок контента создан.')).toBeVisible()

  let contentRow = contentPanel.locator('.list-item-vertical').filter({ hasText: 'Заголовок E2E' })
  await expect(contentRow).toContainText('Управляемый блок E2E')
  expect(api.getLastRequest('/api/admin/site-content', 'POST')?.body).toMatchObject({ key: 'home.e2e.title', value: 'Управляемый блок E2E' })

  await contentRow.getByRole('button', { name: 'Редактировать' }).click()
  await expect(contentPanel.getByRole('heading', { name: 'Редактировать блок контента' })).toBeVisible()
  await contentForm.getByLabel('Значение').fill('Обновлённый блок E2E')
  await contentForm.getByRole('button', { name: 'Сохранить блок' }).click()
  await expect(page.getByText('Блок контента обновлен.')).toBeVisible()
  contentRow = contentPanel.locator('.list-item-vertical').filter({ hasText: 'Заголовок E2E' })
  await expect(contentRow).toContainText('Обновлённый блок E2E')
  expect(api.getLastRequest('/api/admin/site-content/content-created-e2e', 'PUT')?.body).toMatchObject({ value: 'Обновлённый блок E2E' })

  await contentPanel.getByRole('button', { name: 'Восстановить главную' }).click()
  await contentPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Главная обновлена: создано 1, восстановлено 0.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/site-content/home-defaults', 'POST')).toBeTruthy()

  await contentRow.getByRole('button', { name: 'Удалить' }).click()
  await contentPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Блок контента удален.')).toBeVisible()
  await expect(contentPanel.getByText('Контент не настроен')).toBeVisible()
  expect(api.getLastRequest('/api/admin/site-content/content-created-e2e', 'DELETE')).toBeTruthy()

  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  expect(browserErrors).toEqual([])
})

test('admin panel covers login and critical operational mutations across all sections', async ({ page }, testInfo) => {
  test.setTimeout(150_000)
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

  await openAdminSection(page, 'Аудит', 'audit')
  const failedDelivery = page.locator('#audit .list-item').filter({ hasText: 'SMTP connection unavailable' })
  await failedDelivery.getByRole('button', { name: 'Повторить' }).click()
  await expect(page.getByText('Email-уведомление возвращено в очередь доставки.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/notification-deliveries/notification-e2e/retry')).toBeTruthy()

  await openAdminSection(page, 'Оплаты', 'payments')
  await expect(page.getByText('YooKassa sandbox')).toBeVisible()
  await page.locator('#payments').getByRole('button', { name: 'Проверить настройки' }).click()
  await expect(page.getByText('Настройки готовы', { exact: true })).toBeVisible()
  await expect(page.getByText('Конфигурация готова. Внешний кабинет провайдера не запрашивался.')).toBeVisible()
  expect(api.getLastRequest('/api/admin/payment-providers/accounts/provider-yookassa/check')).toBeTruthy()
  const orderRow = page.locator('#payments .list-item-vertical').filter({ hasText: 'Admin Pro 30' })
  await expect(orderRow.getByRole('button', { name: 'Проверить оплату' })).toBeVisible({ timeout: 5_000 })
  await orderRow.getByRole('button', { name: 'Проверить оплату' }).click()
  await expect(page.getByText(/последний платеж .* проверен, статус Succeeded/)).toBeVisible()
  const refundablePayment = page.locator('#payment-payment-e2e')
  await expect(refundablePayment.getByRole('button', { name: 'Проверить статус' })).toBeVisible({ timeout: 5_000 })
  await refundablePayment.getByRole('button', { name: 'Проверить статус' }).click()
  await expect(page.getByText(/Платеж .* проверен: Succeeded/)).toBeVisible()
  await refundablePayment.getByLabel('Причина').fill('playwright_refund')
  await refundablePayment.getByRole('button', { name: 'Вернуть платеж' }).click()
  await page.locator('#payments').getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Возврат rf-e2e: Succeeded')).toBeVisible()
  expect(api.getLastRequest('/api/admin/payments/payment-e2e/refund')?.body).toEqual({ amount: 590, reason: 'playwright_refund' })

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
  await activeAccessRow.getByRole('button', { name: 'Отключить' }).click()
  await page.locator('#vpn').getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('VPN-доступ отключен.')).toBeVisible()
  await activeAccessRow.getByRole('button', { name: 'Синхронизировать' }).click()
  await expect(page.getByText('VPN-доступ синхронизирован.')).toBeVisible()
  await activeAccessRow.getByRole('button', { name: 'Сбросить трафик' }).click()
  await page.locator('#vpn').getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('Трафик VPN-доступа сброшен.')).toBeVisible()
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
  await expect(cancelledSubscriptionRow.getByRole('combobox')).toHaveCount(0)
  await subscriptionsPanel.getByRole('button', { name: 'Продлить' }).click()
  await expect(page.getByText('Подписка продлена на 30 дней.')).toBeVisible()
  await subscriptionsPanel.getByRole('button', { name: 'Синхронизировать доступ' }).click()
  await expect(page.getByText('Текущий VPN-доступ подписки синхронизирован.')).toBeVisible()
  await subscriptionsPanel.getByRole('button', { name: 'Заблокировать' }).click()
  await subscriptionsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText(/Подписка обновлена:/)).toBeVisible()
  await subscriptionsPanel.getByRole('button', { name: 'Разблокировать' }).click()
  await subscriptionsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText(/Подписка обновлена:/)).toBeVisible()
  await subscriptionsPanel.getByRole('combobox', { name: /Целевой сервер для миграции/ }).selectOption('auto')
  await subscriptionsPanel.getByRole('button', { name: 'Перенести', exact: true }).click()
  await expect(subscriptionsPanel.getByRole('dialog')).toContainText('Клиент будет создан на целевой панели')
  await subscriptionsPanel.getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText(/Подписка перенесена на сервер/)).toBeVisible()
  expect(api.getLastRequest('/api/admin/subscriptions/sub-e2e/migrate')?.body).toBeNull()
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
  const migrationTarget = page.getByLabel('Целевой inbound для client@example.test')
  await expect(migrationTarget.locator('optgroup[label="US 3x-ui Sandbox · US"]')).toHaveCount(1)
  await migrationTarget.selectOption('inbound-us')
  await page.locator('#panels').getByRole('button', { name: 'Перенести' }).click()
  await expect(page.locator('#panels').getByRole('dialog')).toContainText('slot целевой панели, inbound и связанного VPN-сервера')
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await page.locator('#panels').getByRole('button', { name: 'Подтвердить' }).click()
  await expect(page.getByText('VPN-клиент client@example.test перенесен: US 3x-ui Sandbox · us-vless.')).toBeVisible()
  await expect(page.locator('#panels').getByRole('combobox', { name: 'Панель' })).toHaveValue('panel-us')
  await expect(page.locator('#panels').getByText('inbound us-vless')).toBeVisible()
  expect(api.getLastRequest('/api/admin/vpn-clients/client-e2e/migrate')?.body).toEqual({ targetInboundId: 'inbound-us' })
  const euPanelRow = page.locator('#panels .list-item-vertical').filter({ has: page.locator('strong', { hasText: 'EU 3x-ui Sandbox' }) })
  const usPanelRow = page.locator('#panels .list-item-vertical').filter({ has: page.locator('strong', { hasText: 'US 3x-ui Sandbox' }) })
  await expect(euPanelRow).toContainText('Емкость 11/1000')
  await expect(usPanelRow).toContainText('Емкость 5/1000')
  await usPanelRow.getByRole('button', { name: 'Проверить' }).click()
  await expect(page.getByText('Проверка панели: Healthy (2.4.9)')).toBeVisible()
  await expect(euPanelRow).toContainText('Емкость 11/1000')
  await expect(usPanelRow).toContainText('Емкость 5/1000')
  await usPanelRow.getByRole('button', { name: 'Синхронизировать' }).click()
  await expect(page.getByText('Синхронизация Succeeded: {"clients":1}')).toBeVisible()

  await openAdminSection(page, 'Поддержка', 'support')
  const supportPanel = page.locator('#support')
  await supportPanel.getByLabel('Ответ пользователю').fill('Ответ пользователю из операционного E2E')
  await supportPanel.getByRole('button', { name: 'Отправить через Telegram' }).click()
  await expect(supportPanel.getByText('Ответ пользователю из операционного E2E', { exact: true })).toBeVisible()
  await supportPanel.getByLabel('Внутренняя заметка').fill('Внутренняя заметка из операционного E2E')
  await supportPanel.getByRole('button', { name: 'Добавить заметку' }).click()
  await expect(supportPanel.getByText('Внутренняя заметка из операционного E2E', { exact: true })).toBeVisible()
  const supportConversationRow = supportPanel.locator('.list-item-vertical').filter({ hasText: 'Проверка доступа' }).first()
  await supportConversationRow.getByRole('button', { name: 'В ожидание' }).click()
  await expect(page.getByText('Статус обращения обновлен: pending.')).toBeVisible()

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

  await openAdminSection(page, 'Подписки', 'subscriptions')
  await expect(page.locator('#subscriptions').getByRole('combobox', { name: /Целевой сервер для миграции/ })).toHaveCount(0)
  await expect(page.locator('#subscriptions').getByRole('button', { name: 'Перенести', exact: true })).toHaveCount(0)

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
