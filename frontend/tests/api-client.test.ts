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

const adminFixtureTimestamp = '2026-08-09T00:00:00Z'

function adminTariffFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'tariff-1',
    name: 'Premium',
    slug: 'premium',
    description: 'Premium access',
    fullDescription: 'Premium VPN access',
    features: ['Автовыдача'],
    featuresJson: '["Автовыдача"]',
    badge: 'Популярный',
    durationDays: 30,
    price: 299,
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
    allowedRegionsCsv: '',
    allowedNodeGroupsCsv: '',
    isReferralEligible: true,
    provisioningScenario: 'auto',
    afterPaymentText: 'Доступ появится в кабинете.',
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminReferralProgramFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'program-1',
    name: 'Welcome',
    status: 'active',
    startAt: null,
    endAt: null,
    ruleDefinition: '{"firstPurchaseOnly":true}',
    rewardDefinition: '{"referrer":{"type":"bonus-days","value":7,"unit":"days","autoApprove":true}}',
    antiFraudSettings: '{}',
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function faqFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'faq-1',
    question: 'Как подключиться?',
    answer: 'Через кабинет',
    category: 'Подключение',
    isActive: true,
    showOnHome: true,
    showOnFaqPage: true,
    sortOrder: 10,
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function appReleaseFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'release-guid',
    releaseId: 'release-1',
    version: '0.2.0',
    releasedAt: adminFixtureTimestamp,
    title: 'Что нового',
    summary: 'Описание релиза',
    isActive: true,
    source: 'manual',
    items: [{ id: 'item-1', type: 'new', text: 'Пункт релиза', sortOrder: 10 }],
    createdByUserId: null,
    createdByUserName: 'Agent',
    updatedByUserId: null,
    updatedByUserName: 'Agent',
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function vpnPanelFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'panel-1',
    name: 'Panel',
    baseUrl: 'https://panel.example.test',
    region: 'eu',
    status: 'Active',
    healthStatus: 'Healthy',
    login: 'admin',
    sslVerificationMode: 'Strict',
    apiVariant: 'X3UiOfficial',
    capacity: 5000,
    usedCapacity: 1,
    autoCreateInbound: false,
    defaultInboundTemplateJson: '{}',
    lastHealthCheckAt: adminFixtureTimestamp,
    lastSyncAt: adminFixtureTimestamp,
    version: '2.4.12',
    lastError: '',
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function vpnInboundFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'inbound-1',
    vpnPanelId: 'panel-1',
    externalInboundId: '1',
    name: 'default-vless',
    protocol: 'vless',
    port: 443,
    listen: '',
    settingsJson: '{"clients":[]}',
    streamSettingsJson: '{"network":"tcp"}',
    sniffingJson: '{}',
    isDefault: true,
    isActive: true,
    capacity: 5000,
    usedCapacity: 1,
    ...overrides
  }
}

function vpnClientFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'client-1',
    userId: 'user-1',
    subscriptionId: 'sub-1',
    vpnPanelId: 'panel-1',
    vpnInboundId: 'inbound-1',
    externalClientId: 'client-1',
    email: 'user@example.test',
    uuid: '00000000-0000-4000-8000-000000000001',
    flow: '',
    limitIp: 3,
    totalGb: null,
    expiryTime: adminFixtureTimestamp,
    enable: true,
    configUri: 'vless://client',
    qrCodePayload: 'vless://client',
    syncStatus: 'synced',
    lastSyncedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function panelSyncRunFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'sync-1',
    vpnPanelId: 'panel-1',
    status: 'Succeeded',
    startedAt: adminFixtureTimestamp,
    finishedAt: adminFixtureTimestamp,
    summaryJson: '{}',
    errorMessage: '',
    ...overrides
  }
}

function panelSyncEventFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'sync-event-1',
    panelSyncRunId: 'sync-1',
    eventType: 'missing_client',
    entityType: 'VpnClient',
    entityId: null,
    externalId: 'client-1',
    message: 'Client is missing.',
    payloadJson: '{}',
    ...overrides
  }
}

function panelHealthCheckFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'health-1',
    vpnPanelId: 'panel-1',
    status: 'Healthy',
    latencyMs: 12,
    version: '2.4.12',
    errorMessage: '',
    checkedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function vpnNodeFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'node-1',
    name: 'nl-01',
    host: 'nl-01.example.com',
    ipAddress: '203.0.113.10',
    provider: 'hetzner',
    region: 'eu',
    country: 'NL',
    datacenter: 'fsn1',
    status: 'Ready',
    capacity: 5000,
    usedCapacity: 1,
    supportedProtocolsCsv: 'vless,vmess,trojan',
    healthStatus: 'Healthy',
    lastHealthCheckAt: adminFixtureTimestamp,
    lastHealthLatencyMs: 12,
    lastHealthError: '',
    lastHealthMetadataJson: '{}',
    provisioningStatus: 'Succeeded',
    provisioningMode: 'validation-deploy',
    provisioningModeTitle: 'Validation deploy',
    provisioningRiskLevel: 'low',
    liveDeployAllowed: false,
    provisioningNextAction: 'Проверьте precheck.',
    provisioningOperatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.',
    precheckMode: 'dry-run',
    precheckModeTitle: 'Dry-run precheck',
    installedVersion: '1.0.0',
    backupStatus: 'Ready',
    monitoringStatus: 'Ready',
    loggingStatus: 'Ready',
    tagsCsv: 'validation-mode:true',
    priority: 100,
    isAvailableForNewUsers: true,
    sshUser: 'root',
    sshPort: 22,
    sshAuthMethod: 'ssh_key',
    sshCredentialConfigured: true,
    skipHostKeyChecking: true,
    panelBaseUrl: 'https://panel.example.test',
    panelUsername: 'admin',
    panelPasswordConfigured: true,
    panelInboundId: 1,
    publicHostname: 'vpn.example.test',
    publicPort: 443,
    nodeGroupId: null,
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function nodeHealthCheckFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'node-check-1',
    nodeId: 'node-1',
    status: 'Healthy',
    checkedAt: adminFixtureTimestamp,
    latencyMs: 12,
    metadataJson: '{}',
    errorText: '',
    ...overrides
  }
}

function provisioningRunFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'run-1',
    nodeId: 'node-1',
    nodeName: 'nl-01',
    targetHost: 'nl-01.example.com',
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
    nextAction: 'Проверьте результат precheck.',
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
    startedAt: adminFixtureTimestamp,
    finishedAt: adminFixtureTimestamp,
    errorSummary: '',
    executionLog: 'precheck ok',
    executionLogPreview: 'precheck ok',
    precheckReportPreview: 'ready',
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function provisioningRunDetailsFixture(overrides: Record<string, unknown> = {}) {
  const { executionLogPreview: _executionLogPreview, precheckReportPreview: _precheckReportPreview, ...run } = provisioningRunFixture()
  return {
    run: { ...run, precheckReport: 'ready', linkedAccessId: null },
    steps: [{
      id: 'step-1',
      provisioningRunId: 'run-1',
      stepName: 'Validate input',
      status: 'Succeeded',
      startedAt: adminFixtureTimestamp,
      finishedAt: adminFixtureTimestamp,
      output: 'credentials=***',
      errorText: '',
      createdAt: adminFixtureTimestamp,
      updatedAt: adminFixtureTimestamp
    }],
    ...overrides
  }
}

function provisioningCommandFixture(overrides: Record<string, unknown> = {}) {
  return {
    serverId: 'node-1',
    runId: 'run-1',
    status: 'queued',
    dryRun: false,
    mode: 'validation-deploy',
    modeTitle: 'Validation deploy',
    riskLevel: 'low',
    liveDeployAllowed: false,
    nextAction: 'Проверьте результат.',
    operatorWarning: 'Validation deploy не меняет рабочую инфраструктуру.',
    ...overrides
  }
}

function telegramBotSettingsFixture(overrides: Record<string, unknown> = {}) {
  return {
    enabled: false,
    mode: 'LongPolling',
    publicBotUsername: 'vpnplatform_bot',
    hasBotToken: true,
    botTokenMasked: '1234***7890',
    webhookUrl: '',
    hasSecretToken: true,
    adminChatId: '',
    webAppUrl: 'https://cabinet.example.test',
    welcomeText: 'Добро пожаловать',
    instructionText: 'Инструкция',
    supportText: 'Поддержка',
    afterPaymentTextTemplate: 'Оплата получена',
    renewalTextTemplate: 'Продление',
    paymentFailedTextTemplate: 'Ошибка оплаты',
    subscriptionExpiredTextTemplate: 'Подписка истекла',
    generatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function telegramBotConnectionCheckFixture(overrides: Record<string, unknown> = {}) {
  return {
    isReady: true,
    status: 'ready',
    requiredActions: [],
    warnings: [],
    checkedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminCapabilitiesFixture(overrides: Record<string, unknown> = {}) {
  return {
    adminRead: true,
    adminWrite: true,
    financeRead: true,
    financeWrite: true,
    supportRead: true,
    supportWrite: true,
    provisioningManage: true,
    vpnManage: true,
    botManage: true,
    settingsManage: true,
    ...overrides
  }
}

function adminSessionFixture(overrides: Record<string, unknown> = {}) {
  return {
    userId: 'admin-1',
    email: 'admin@example.test',
    displayName: 'Admin',
    roles: ['Admin'],
    capabilities: adminCapabilitiesFixture(),
    ...overrides
  }
}

function adminDashboardFixture(overrides: Record<string, unknown> = {}) {
  return {
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
    supportConversationsCount: 0,
    openSupportConversations: 0,
    provisioningErrors: 0,
    productionReadiness: {
      isReady: false,
      status: 'Blocked',
      checks: [{
        key: 'payment-webhook',
        label: 'Payment webhook',
        status: 'Blocked',
        message: 'Webhook URL is not configured.',
        category: 'Payments',
        severity: 'critical',
        actionLabel: 'Open payments',
        actionHref: '#payments'
      }]
    },
    generatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminUserFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'user-1',
    email: 'user@example.test',
    displayName: 'User',
    rolesCsv: 'User',
    status: 'Active',
    isBlocked: false,
    preferredLanguage: 'ru',
    referralCode: 'USER1',
    authSource: 'Local',
    emailConfirmed: true,
    lastLoginAt: null,
    telegramRegistrationCompletedAt: null,
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminOrderFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'order-1',
    userId: 'user-1',
    userDisplayName: 'User',
    userEmail: 'user@example.test',
    tariffId: 'tariff-1',
    tariffName: 'Monthly',
    amount: 100,
    currency: 'RUB',
    status: 'PendingPayment',
    type: 'NewSubscription',
    channel: 'Web',
    paymentProvider: 'YooKassa',
    checkoutSessionId: null,
    expiresAt: adminFixtureTimestamp,
    paidAt: null,
    isFirstPurchase: false,
    paymentAttemptsCount: 1,
    lastPaymentId: 'payment-1',
    lastPaymentStatus: 'Pending',
    lastPaymentProvider: 'YooKassa',
    lastPaymentRecheckSupported: true,
    lastPaymentCanRecheck: true,
    lastPaymentRecheckBlockers: [],
    linkedSubscriptionId: null,
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminPaymentFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'payment-1',
    orderId: 'order-1',
    userId: 'user-1',
    userDisplayName: 'User',
    provider: 'YooKassa',
    paymentProviderAccountId: 'provider-1',
    providerMode: 'Sandbox',
    providerPaymentId: 'provider-payment-1',
    externalEventId: '',
    idempotencyKey: null,
    confirmationUrl: null,
    returnUrl: null,
    amount: 100,
    currency: 'RUB',
    status: 'Succeeded',
    signatureValidated: true,
    isActivationProcessed: true,
    activationProcessedAt: adminFixtureTimestamp,
    paidAt: adminFixtureTimestamp,
    failedAt: null,
    refundedAt: null,
    refundedAmount: 0,
    statusReason: null,
    webhookEventsCount: 1,
    refundsCount: 0,
    recheckSupported: true,
    canRecheck: true,
    recheckBlockers: [],
    refundSupported: true,
    canRefund: false,
    refundableAmount: 0,
    refundBlockers: [],
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminOverviewPaymentFixture(overrides: Record<string, unknown> = {}) {
  const payment = adminPaymentFixture(overrides)
  delete payment.recheckSupported
  delete payment.canRecheck
  delete payment.recheckBlockers
  delete payment.refundSupported
  delete payment.canRefund
  delete payment.refundableAmount
  delete payment.refundBlockers
  return payment
}

function cabinetPaymentFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'payment-1',
    orderId: 'order-1',
    userId: 'user-1',
    provider: 'YooKassa',
    providerMode: 'Sandbox',
    providerPaymentId: 'provider-payment-1',
    confirmationUrl: null,
    amount: 100,
    currency: 'RUB',
    status: 'Failed',
    isActivationProcessed: false,
    activationProcessedAt: null,
    paidAt: null,
    failedAt: adminFixtureTimestamp,
    refundedAt: null,
    refundedAmount: 0,
    statusMessage: 'Платёж не завершён. Повторите оплату или обратитесь в поддержку.',
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function cabinetOrderFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'order-1',
    tariffId: 'tariff-1',
    tariffName: 'Monthly',
    amount: 490,
    currency: 'RUB',
    status: 'PendingPayment',
    type: 'Renewal',
    paymentProvider: 'YooKassa',
    expiresAt: '2026-09-09T00:00:00Z',
    paidAt: null,
    linkedSubscriptionId: 'sub-1',
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function cabinetSubscriptionFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'sub-1',
    tariffId: 'tariff-1',
    tariffName: 'Monthly',
    status: 'Active',
    startAt: adminFixtureTimestamp,
    endAt: '2026-09-09T00:00:00Z',
    gracePeriodEndAt: null,
    currentAccessId: 'access-1',
    accessUri: 'vless://cabinet-safe',
    nodeName: 'EU node',
    suspendedAt: null,
    cancelledAt: null,
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function cabinetAccessFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'access-1',
    subscriptionId: 'sub-1',
    subscriptionStatus: 'Active',
    isTerminal: false,
    serverName: 'NL Amsterdam',
    accessUri: 'vless://cabinet-safe',
    status: 'Active',
    expiryDate: '2026-09-09T00:00:00Z',
    ...overrides
  }
}

function cabinetReferralFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'reward-1',
    type: 'bonus-days',
    status: 'Approved',
    value: 7,
    currencyOrUnit: 'days',
    processedAt: adminFixtureTimestamp,
    createdAt: adminFixtureTimestamp,
    ...overrides
  }
}

function cabinetSupportConversationFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'support-1',
    channel: 'web',
    status: 'open',
    subject: 'Оплата',
    revision: 4,
    closedAt: null,
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function cabinetSupportMessageFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'message-1',
    supportConversationId: 'support-1',
    direction: 'inbound',
    text: 'Нужна помощь',
    createdAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminPaymentProviderAccountFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'provider-1',
    provider: 'YooKassa',
    mode: 'Sandbox',
    name: 'yookassa-sandbox',
    publicName: 'YooKassa sandbox',
    isEnabled: true,
    isDefault: true,
    shopId: 'shop-1',
    apiBaseUrl: 'https://api.yookassa.ru',
    returnUrl: 'https://cabinet.example.test/payment-return',
    webhookUrl: 'https://api.example.test/webhooks/payments/yookassa',
    hasSecretKey: true,
    hasWebhookSecret: false,
    useWebhookIpAllowList: false,
    allowedWebhookIpRangesCsv: '',
    extraSettingsJson: '{}',
    healthStatus: 'Unknown',
    isCheckoutConfigured: true,
    checkoutConfigurationIssue: null,
    capabilitiesJson: '["createPayment"]',
    capabilities: [{ key: 'createPayment', label: 'Создание платежа', supported: true, status: 'supported' }],
    requiredFields: [{ key: 'shopId', label: 'ShopId / merchant id', required: true, configured: true, issue: null }],
    readinessBlockers: [],
    isPubliclyAvailable: true,
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminSubscriptionFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'sub-1',
    userId: 'user-1',
    tariffId: 'tariff-1',
    tariffName: 'Monthly',
    status: 'Active',
    startAt: adminFixtureTimestamp,
    endAt: '2026-09-09T00:00:00Z',
    gracePeriodEndAt: null,
    autoRenewFlag: false,
    sourceChannel: 'Web',
    currentServerId: 'server-1',
    currentAccessId: 'access-1',
    lastPaymentId: 'payment-1',
    renewalCount: 0,
    blockReason: null,
    suspendedAt: null,
    cancelledAt: null,
    lifecycleAttemptCount: 0,
    lifecycleProcessingStartedAt: null,
    lifecycleLeaseExpiresAt: null,
    lifecycleNextAttemptAt: null,
    lifecycleLastError: null,
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminAccessFixture(overrides: Record<string, unknown> = {}) {
  return {
    id: 'access-1',
    subscriptionId: 'sub-1',
    subscriptionStatus: 'Active',
    isTerminal: false,
    userId: 'user-1',
    providerType: 'x3ui',
    providerAccessId: 'client-1',
    serverId: 'server-1',
    serverName: 'Sandbox node',
    accessUri: 'vless://test',
    qrCodePayload: 'vless://test',
    qrCodePath: 'vless://test',
    configPath: '',
    status: 'Active',
    issuedAt: adminFixtureTimestamp,
    expiryDate: '2026-09-09T00:00:00Z',
    disabledAt: null,
    lastSyncedAt: adminFixtureTimestamp,
    revision: 1,
    history: [],
    createdAt: adminFixtureTimestamp,
    updatedAt: adminFixtureTimestamp,
    ...overrides
  }
}

function adminUserOverviewFixture(overrides: Record<string, unknown> = {}) {
  return {
    user: adminUserFixture(),
    telegramAccounts: [],
    orders: [],
    payments: [],
    subscriptions: [],
    accessCredentials: [],
    supportConversations: [],
    ...overrides
  }
}

test('buildAuthHeaders returns bearer header when token exists', () => {
  assert.deepEqual(buildAuthHeaders('abc'), { Authorization: 'Bearer abc' })
  assert.deepEqual(buildAuthHeaders(''), {})
})

test('normalizeApiError prefers error field and message field', () => {
  assert.equal(normalizeApiError({ error: 'boom' }, 'fallback'), 'fallback')
  assert.equal(normalizeApiError({ message: 'VPN access not found.' }, 'fallback'), 'fallback')
  assert.equal(normalizeApiError({ error: 'Операция недоступна.' }, 'fallback'), 'Операция недоступна.')
  assert.equal(normalizeApiError({ message: 'Запрос отклонён.' }, 'fallback'), 'Запрос отклонён.')
  assert.equal(normalizeApiError({ error: 'Promo code not found.' }, 'fallback'), 'Промокод не найден. Проверьте написание.')
  assert.equal(normalizeApiError({ error: 'qr_temporarily_unavailable' }, 'Не удалось загрузить QR-код.'), 'Не удалось загрузить QR-код.')
  assert.equal(normalizeApiError('provider_timeout', 'Сервис временно недоступен.'), 'Сервис временно недоступен.')
  assert.equal(normalizeApiError({ message: 'provider_timeout' }, 'Сервис временно недоступен.'), 'Сервис временно недоступен.')
  assert.equal(normalizeApiError({ error: '   ' }, 'fallback'), 'fallback')
  assert.equal(normalizeApiError({ error: ' ', message: 'Запрос отклонён.' }, 'fallback'), 'Запрос отклонён.')
  assert.equal(normalizeApiError({ message: '\t' }, 'fallback'), 'fallback')
  assert.equal(normalizeApiError(new Error('profile unavailable'), 'fallback'), 'fallback')
  assert.equal(normalizeApiError(new Error('Операция временно недоступна.'), 'fallback'), 'Операция временно недоступна.')
  assert.equal(normalizeApiError('Action failed', 'fallback'), 'fallback')
  assert.equal(normalizeApiError(null, 'fallback'), 'fallback')
})

test('frontend error consumers normalize exceptions before rendering them', () => {
  const publicSessionSource = readFileSync(new URL('../apps/public-web/src/public-session.ts', import.meta.url), 'utf8')
  const publicPageStateSource = readFileSync(new URL('../apps/public-web/src/public-page-state.ts', import.meta.url), 'utf8')
  const cabinetSource = readFileSync(new URL('../apps/cabinet/src/App.tsx', import.meta.url), 'utf8')
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  for (const source of [publicSessionSource, publicPageStateSource, cabinetSource, adminSource]) {
    assert.match(source, /normalizeApiError/)
    assert.doesNotMatch(source, /instanceof Error\s*\?\s*[a-z]+\.message/)
  }
  assert.doesNotMatch(cabinetSource, /setPaymentProvidersError\([^)]*\.message\)/)
  assert.doesNotMatch(adminSource, /['"](?:Action failed|Failed to load)['"]/)
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
      assert.equal(error.message, 'Не удалось выполнить запрос. Попробуйте еще раз.')
      assert.deepEqual(error.payload, { error: 'forbidden' })
      return true
    }
  )
})

test('ApiClient.getAdminSession loads the capability contract with bearer auth', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify(adminSessionFixture({
      roles: ['FinanceManager'],
      capabilities: adminCapabilitiesFixture({
        adminWrite: false,
        supportRead: false,
        supportWrite: false,
        provisioningManage: false,
        vpnManage: false,
        botManage: false,
        settingsManage: false
      })
    })), { status: 200, headers: { 'Content-Type': 'application/json' } })
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
  assert.equal(translateAuthError(new ApiClientError('Запрос не выполнен.', 401, { error: 'invalid_credentials' })), 'Неверный email или пароль.')
  assert.equal(translateAuthError(new ApiClientError('Запрос не выполнен.', 401, { error: 'unknown_auth_failure' }), 'Не удалось войти'), 'Не удалось войти')
  assert.equal(translateAuthError(new Error('unknown_auth_failure'), 'Не удалось войти'), 'Не удалось войти')
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
    return new Response(JSON.stringify([{
      id: '1', name: '1 month', slug: 'one-month', description: '', fullDescription: '', features: [], featuresJson: '[]', badge: '',
      durationDays: 30, price: 299, currency: 'RUB', maxDevices: 2, trafficLimit: null, isTrial: false, isActive: true,
      sortOrder: 10, visibleFrom: null, visibleTo: null, tariffType: 'Personal', category: 'default', allowedRegionsCsv: '',
      allowedNodeGroupsCsv: '', isReferralEligible: true, provisioningScenario: 'auto', afterPaymentText: '',
      createdAt: '2026-08-05T00:00:00Z', updatedAt: '2026-08-05T00:00:00Z'
    }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
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
      return new Response(JSON.stringify([{ id: 'faq-1', question: 'Как подключиться?', answer: 'Через кабинет', category: 'Подключение', isActive: true, showOnHome: true, showOnFaqPage: true, sortOrder: 10, createdAt: '2026-08-05T00:00:00Z', updatedAt: '2026-08-05T00:00:00Z' }]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    if (init?.method === 'DELETE') {
      return new Response(JSON.stringify({ id: 'faq-1', deleted: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify(faqFixture()), {
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
        return new Response(JSON.stringify({ isReady: true, requiredCount: 1, presentCount: 1, activeRequiredCount: 1, missingKeys: [], inactiveKeys: [], emptyKeys: [], duplicateKeys: [], publicBlocksCount: 1, requiredKeys: ['home.hero.title'] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }

      if (String(url).includes('/home-defaults')) {
        return new Response(JSON.stringify({ created: 1, restored: 2, readiness: { isReady: true, requiredCount: 1, presentCount: 1, activeRequiredCount: 1, missingKeys: [], inactiveKeys: [], emptyKeys: [], duplicateKeys: [], publicBlocksCount: 1, requiredKeys: ['home.hero.title'] } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
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
      return new Response(JSON.stringify([adminTariffFixture()]), {
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

    return new Response(JSON.stringify(adminTariffFixture({ badge: 'Выгодно' })), {
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
    return new Response(JSON.stringify(String(url).endsWith('/referrals') ? [] : isProgramList ? [adminReferralProgramFixture()] : adminReferralProgramFixture()), {
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
    const timestamp = new Date().toISOString()
    return new Response(JSON.stringify({
      id: 'order-1',
      tariffId: 'tariff-1',
      amount: 490,
      currency: 'RUB',
      status: 'PendingPayment',
      paymentProvider: 'YooKassa',
      expiresAt: timestamp,
      linkedSubscriptionId: null
    }), {
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
    return new Response(JSON.stringify({ paymentId: 'pay-1', redirectUrl: 'https://example.test/pay-1' }), {
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
  const conversation = { id: 'support-1', channel: 'web', status: 'open', subject: 'Оплата', revision: 4, closedAt: null, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
  const message = { id: 'message-1', supportConversationId: 'support-1', direction: 'inbound', text: 'Нужна помощь', createdAt: new Date().toISOString() }
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

    if (String(url).endsWith('/api/admin/servers') && !init?.method) {
      return new Response(JSON.stringify([vpnNodeFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    if (String(url).endsWith('/health-check') && init?.method === 'POST') {
      return new Response(JSON.stringify(nodeHealthCheckFixture()), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    if (String(url).endsWith('/health-checks')) {
      return new Response(JSON.stringify([nodeHealthCheckFixture()]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify(vpnNodeFixture()), {
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

  await client.getAdminServers('admin-token')
  await client.createAdminServer('admin-token', payload)
  await client.updateAdminServer('admin-token', 'node-1', { ...payload, name: 'nl-01-edited', priority: 200, tagsCsv: 'tier:premium' })
  await client.disableAdminServer('admin-token', 'node-1')
  await client.checkAdminServerHealth('admin-token', 'node-1')
  await client.getAdminServerHealthChecks('admin-token', 'node-1')
  await client.enableAdminServerAllocation('admin-token', 'node-1')
  await client.disableAdminServerAllocation('admin-token', 'node-1')
  await client.enableAdminServerMaintenance('admin-token', 'node-1')
  await client.disableAdminServerMaintenance('admin-token', 'node-1')
  const deletion = await client.deleteAdminServer('admin-token', 'node-1')

  const headers = new Headers(calls[0]?.init?.headers)
  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/servers')
  assert.equal(headers.get('Authorization'), 'Bearer admin-token')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.match(String(calls[1]?.init?.body), /sshCredential/)
  assert.match(String(calls[1]?.init?.body), /validationMode/)
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/servers/node-1')
  assert.equal(calls[2]?.init?.method, 'PUT')
  assert.match(String(calls[2]?.init?.body), /nl-01-edited/)
  assert.match(String(calls[2]?.init?.body), /tier:premium/)
  assert.deepEqual(calls.slice(3, 10).map((call) => new URL(call.url).pathname), [
    '/api/admin/servers/node-1/disable',
    '/api/admin/servers/node-1/health-check',
    '/api/admin/servers/node-1/health-checks',
    '/api/admin/servers/node-1/enable-allocation',
    '/api/admin/servers/node-1/disable-allocation',
    '/api/admin/servers/node-1/maintenance',
    '/api/admin/servers/node-1/disable-maintenance'
  ])
  assert.equal(calls[10]?.url, 'http://localhost:8080/api/admin/servers/node-1')
  assert.equal(calls[10]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[10]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(deletion.archived, true)
  assert.equal(deletion.linkedHealthChecks, 2)
  assert.equal(deletion.linkedMigrationJobs, 1)
})

test('ApiClient provisioning run details and actions are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).endsWith('/api/admin/provisioning-runs') && !init?.method) {
      return new Response(JSON.stringify([provisioningRunFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/retry')) {
      return new Response(JSON.stringify(provisioningCommandFixture({ status: 'Retrying', dryRun: true, mode: 'dry-run', modeTitle: 'Dry-run precheck', riskLevel: 'safe', nextAction: 'Проверьте precheck.', operatorWarning: 'Dry-run не меняет VPS.' })), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/deploy')) {
      return new Response(JSON.stringify(provisioningCommandFixture({ status: 'DeployQueued' })), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/cancel')) {
      return new Response(JSON.stringify({ runId: 'run-1', status: 'cancelled' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (String(url).endsWith('/support-needed')) {
      return new Response(JSON.stringify({ runId: 'run-1', supportConversationId: 'support-1' }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    return new Response(JSON.stringify(provisioningRunDetailsFixture()), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getAdminProvisioningRuns('admin-token')
  const details = await client.getAdminProvisioningRun('admin-token', 'run-1')
  await client.retryAdminProvisioningRun('admin-token', 'run-1')
  const deploy = await client.deployAdminProvisioningRun('admin-token', 'run-1')
  await client.cancelAdminProvisioningRun('admin-token', 'run-1')
  await client.markAdminProvisioningSupportNeeded('admin-token', 'run-1')

  assert.deepEqual(calls.map((call) => new URL(call.url).pathname), [
    '/api/admin/provisioning-runs',
    '/api/admin/provisioning-runs/run-1',
    '/api/admin/provisioning-runs/run-1/retry',
    '/api/admin/provisioning-runs/run-1/deploy',
    '/api/admin/provisioning-runs/run-1/cancel',
    '/api/admin/provisioning-runs/run-1/support-needed'
  ])
  assert.equal(calls[3]?.init?.method, 'POST')
  assert.equal(new Headers(calls[5]?.init?.headers).get('Authorization'), 'Bearer admin-token')
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
    const precheck = String(url).endsWith('/precheck')
    return new Response(JSON.stringify(provisioningCommandFixture(precheck
      ? { dryRun: true, mode: 'dry-run', modeTitle: 'Dry-run precheck', riskLevel: 'safe', nextAction: 'Проверьте precheck.', operatorWarning: 'Dry-run не меняет VPS.' }
      : {})), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.precheckAdminServer('admin-token', 'node-1')
  const response = await client.queueAdminProvision('admin-token', 'node-1')
  const headers = new Headers(calls[0]?.init?.headers)

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/servers/node-1/precheck')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/servers/node-1/provision')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(headers.get('Authorization'), 'Bearer admin-token')
  assert.equal(response.runId, 'run-1')
  assert.equal(response.mode, 'validation-deploy')
  assert.equal(response.riskLevel, 'low')
})

test('ApiClient.createCheckoutSession calls public checkout-session endpoint', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    return new Response(JSON.stringify({ id: 'session-1', token: 'checkout-token', tariffId: 'tariff-1', userId: null, orderId: null, status: 'open', expiresAt: new Date().toISOString(), emailHint: null }), {
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
    return new Response(JSON.stringify({ id: 'order-1', tariffId: 'tariff-1', amount: 490, currency: 'RUB', status: 'PendingPayment', paymentProvider: 'YooKassa', expiresAt: new Date().toISOString(), linkedSubscriptionId: null }), {
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

test('ApiClient accepts only the safe cabinet payment contract', async () => {
  const responses = [
    [cabinetPaymentFixture()],
    [{ ...cabinetPaymentFixture(), statusReason: 'private-provider-exception' }],
    [{ ...cabinetPaymentFixture(), externalEventId: 'private-event-id' }],
    [{ ...cabinetPaymentFixture(), signatureValidated: true }],
    [{ ...cabinetPaymentFixture(), rawResponse: '{"private":true}' }]
  ]
  globalThis.fetch = (async () => new Response(JSON.stringify(responses.shift()), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const payments = await client.getMyPayments('user-token')

  assert.equal(payments[0]?.statusMessage, 'Платёж не завершён. Повторите оплату или обратитесь в поддержку.')
  await assert.rejects(
    () => client.getMyPayments('user-token'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502
  )
  await assert.rejects(() => client.getMyPayments('user-token'), (error: unknown) => error instanceof ApiClientError && error.status === 502)
  await assert.rejects(() => client.getMyPayments('user-token'), (error: unknown) => error instanceof ApiClientError && error.status === 502)
  await assert.rejects(() => client.getMyPayments('user-token'), (error: unknown) => error instanceof ApiClientError && error.status === 502)
})

test('ApiClient accepts only the safe cabinet order contract', async () => {
  const responses = [
    [cabinetOrderFixture()],
    [{ ...cabinetOrderFixture(), userId: 'private-user-id' }],
    [{ ...cabinetOrderFixture(), checkoutSessionId: 'private-checkout-id' }],
    [{ ...cabinetOrderFixture(), channel: 'Web' }],
    [{ ...cabinetOrderFixture(), isFirstPurchase: true }],
    [{ ...cabinetOrderFixture(), paymentAttemptsCount: 7 }]
  ]
  globalThis.fetch = (async () => new Response(JSON.stringify(responses.shift()), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const orders = await client.getMyOrders('user-token')

  assert.equal(orders[0]?.tariffName, 'Monthly')
  for (let index = 0; index < 5; index += 1) {
    await assert.rejects(
      () => client.getMyOrders('user-token'),
      (error: unknown) => error instanceof ApiClientError && error.status === 502
    )
  }
})

test('ApiClient payment init rejects raw provider responses', async () => {
  const responses = [
    { paymentId: 'payment-1', redirectUrl: 'https://pay.example.test/order-1' },
    { paymentId: 'payment-1', redirectUrl: 'https://pay.example.test/order-1', rawResponse: '{"private":true}' }
  ]
  globalThis.fetch = (async () => new Response(JSON.stringify(responses.shift()), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const payment = await client.initMyPayment('user-token', 'order-1', 'YooKassa')

  assert.equal(payment.paymentId, 'payment-1')
  await assert.rejects(
    () => client.initMyPayment('user-token', 'order-1', 'YooKassa'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502
  )
})

test('ApiClient accepts only the safe cabinet subscription contract', async () => {
  const responses = [
    [cabinetSubscriptionFixture()],
    [{ ...cabinetSubscriptionFixture(), blockReason: 'private-x3ui-provider-exception' }],
    [{ ...cabinetSubscriptionFixture(), currentServerId: 'private-node-id' }],
    [{ ...cabinetSubscriptionFixture(), configPath: '/private/config/path' }],
    [{ ...cabinetSubscriptionFixture(), lifecycleLastError: 'private-lifecycle-error' }]
  ]
  globalThis.fetch = (async () => new Response(JSON.stringify(responses.shift()), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const subscriptions = await client.getMySubscriptions('user-token')

  assert.equal(subscriptions[0]?.accessUri, 'vless://cabinet-safe')
  for (let index = 0; index < 4; index += 1) {
    await assert.rejects(() => client.getMySubscriptions('user-token'), (error: unknown) => error instanceof ApiClientError && error.status === 502)
  }
})

test('ApiClient accepts only the safe cabinet access contract', async () => {
  const responses = [
    [cabinetAccessFixture()],
    [{ ...cabinetAccessFixture(), providerAccessId: 'private-x3ui-client-id' }],
    [{ ...cabinetAccessFixture(), serverId: 'private-node-id' }],
    [{ ...cabinetAccessFixture(), qrCodePath: 'vless://private-qr-payload' }],
    [{ ...cabinetAccessFixture(), configPath: '/private/config/path' }],
    [{ ...cabinetAccessFixture(), lastSyncedAt: adminFixtureTimestamp }]
  ]
  globalThis.fetch = (async () => new Response(JSON.stringify(responses.shift()), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const accesses = await client.getMyAccesses('user-token')

  assert.equal(accesses[0]?.serverName, 'NL Amsterdam')
  assert.equal(accesses[0]?.accessUri, 'vless://cabinet-safe')
  for (let index = 0; index < 5; index += 1) {
    await assert.rejects(
      () => client.getMyAccesses('user-token'),
      (error: unknown) => error instanceof ApiClientError && error.status === 502
    )
  }
})

test('ApiClient accepts only the safe cabinet referral contract', async () => {
  const responses = [
    [cabinetReferralFixture()],
    [{ ...cabinetReferralFixture(), userId: 'private-user-id' }],
    [{ ...cabinetReferralFixture(), sourceUserId: 'private-source-user-id' }],
    [{ ...cabinetReferralFixture(), referralProgramId: 'private-program-id' }],
    [{ ...cabinetReferralFixture(), metadataJson: '{"private":true}' }]
  ]
  globalThis.fetch = (async () => new Response(JSON.stringify(responses.shift()), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const rewards = await client.getMyReferrals('user-token')

  assert.equal(rewards[0]?.value, 7)
  for (let index = 0; index < 4; index += 1) {
    await assert.rejects(
      () => client.getMyReferrals('user-token'),
      (error: unknown) => error instanceof ApiClientError && error.status === 502
    )
  }
})

test('ApiClient accepts only safe cabinet support contracts', async () => {
  const responses = [
    [cabinetSupportConversationFixture()],
    [cabinetSupportMessageFixture()],
    [{ ...cabinetSupportConversationFixture(), userId: 'private-user-id' }],
    [{ ...cabinetSupportConversationFixture(), telegramUserId: 777001 }],
    [{ ...cabinetSupportConversationFixture(), assignedToUserId: 'private-agent-id' }],
    [{ ...cabinetSupportConversationFixture(), internalNote: 'private-order-context' }],
    [{ ...cabinetSupportMessageFixture(), userId: 'private-user-id' }],
    [{ ...cabinetSupportMessageFixture(), telegramUserId: 777001 }],
    [{ ...cabinetSupportMessageFixture(), attachmentsJson: '[{\"private\":true}]' }],
    [{ ...cabinetSupportMessageFixture(), isInternalNote: false }]
  ]
  globalThis.fetch = (async () => new Response(JSON.stringify(responses.shift()), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  assert.equal((await client.getMySupportConversations('user-token'))[0]?.subject, 'Оплата')
  assert.equal((await client.getMySupportMessages('user-token', 'support-1'))[0]?.text, 'Нужна помощь')
  for (let index = 0; index < 4; index += 1) {
    await assert.rejects(() => client.getMySupportConversations('user-token'), (error: unknown) => error instanceof ApiClientError && error.status === 502)
  }
  for (let index = 0; index < 4; index += 1) {
    await assert.rejects(() => client.getMySupportMessages('user-token', 'support-1'), (error: unknown) => error instanceof ApiClientError && error.status === 502)
  }
})

test('ApiClient admin support reply and note endpoints are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    const body = String(init?.body ?? '')
    if (String(url).endsWith('/notes')) {
      return new Response(JSON.stringify({ id: 'msg-1', supportConversationId: 'conv-1', userId: 'admin-1', telegramUserId: null, direction: 'internal', text: 'note', attachmentsJson: '[]', isInternalNote: true, createdAt: new Date().toISOString() }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ conversationId: 'conv-1', status: body.includes('pending') ? 'pending' : 'queued', revision: body.includes('pending') ? 9 : 8 }), {
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
    return new Response(JSON.stringify([adminAccessFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
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
    const path = new URL(String(url)).pathname
    const method = init?.method ?? 'GET'
    if (init?.method === 'DELETE') {
      return new Response(JSON.stringify({ id: 'panel-1', deleted: true, archived: false, linkedInbounds: 0, linkedClients: 0, linkedSyncRuns: 0, linkedHealthChecks: 0 }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/test-connection')) {
      return new Response(JSON.stringify(panelHealthCheckFixture()), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/health-checks')) {
      return new Response(JSON.stringify([panelHealthCheckFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/sync-runs')) {
      return new Response(JSON.stringify([panelSyncRunFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/events')) {
      return new Response(JSON.stringify([panelSyncEventFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path === '/api/admin/vpn-panels/panel-1/sync') {
      return new Response(JSON.stringify(panelSyncRunFixture()), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.includes('/api/admin/vpn-clients/')) {
      return new Response(JSON.stringify(vpnClientFixture({ enable: !path.endsWith('/disable') })), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path === '/api/admin/vpn-panels/panel-1/inbounds') {
      return new Response(JSON.stringify(method === 'POST' ? vpnInboundFixture() : [vpnInboundFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path === '/api/admin/vpn-inbounds') {
      return new Response(JSON.stringify([
        vpnInboundFixture(),
        vpnInboundFixture({ id: 'inbound-2', vpnPanelId: 'panel-2', externalInboundId: '1' })
      ]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.includes('/api/admin/vpn-inbounds/')) {
      return new Response(JSON.stringify(vpnInboundFixture(path.endsWith('/set-default')
        ? {}
        : { isDefault: false, isActive: false })), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path === '/api/admin/vpn-panels/panel-1/clients') {
      return new Response(JSON.stringify([vpnClientFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path === '/api/admin/vpn-panels' && method === 'GET') {
      return new Response(JSON.stringify([vpnPanelFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    return new Response(JSON.stringify(vpnPanelFixture()), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.getAdminVpnPanels('admin-token')
  await client.createAdminVpnPanel('admin-token', { name: 'panel', baseUrl: 'https://panel.example.test', login: 'admin', password: 'secret', region: 'eu', capacity: 5000, sslVerificationMode: 'Strict', apiVariant: 'X3UiOfficial', autoCreateInbound: false, defaultInboundTemplateJson: '{}' })
  await client.updateAdminVpnPanel('admin-token', 'panel-1', { name: 'edited-panel', password: '', sslVerificationMode: 'AllowSelfSigned', apiVariant: 'ThreeXUi', autoCreateInbound: true })
  await client.testAdminVpnPanel('admin-token', 'panel-1')
  await client.syncAdminVpnPanel('admin-token', 'panel-1')
  await client.getAdminVpnPanelInbounds('admin-token', 'panel-1')
  await client.getAdminVpnInbounds('admin-token')
  await client.createAdminVpnPanelInbound('admin-token', 'panel-1', { name: 'default-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{}', streamSettingsJson: '{"network":"tcp"}', sniffingJson: '{}', isDefault: true, capacity: 5000, isActive: true })
  await client.updateAdminVpnInbound('admin-token', 'inbound-1', { name: 'default-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{}', streamSettingsJson: '{"network":"tcp"}', sniffingJson: '{}', isDefault: false, capacity: 5000, isActive: false })
  await client.setAdminVpnInboundDefault('admin-token', 'inbound-1')
  await client.getAdminVpnPanelClients('admin-token', 'panel-1')
  await client.disableAdminVpnClient('admin-token', 'client-1')
  await client.enableAdminVpnClient('admin-token', 'client-1')
  await client.syncAdminVpnClient('admin-token', 'client-1')
  await client.resetAdminVpnClientTraffic('admin-token', 'client-1')
  await client.migrateAdminVpnClient('admin-token', 'client-1', 'inbound-2')
  await client.getAdminVpnPanelSyncRuns('admin-token', 'panel-1')
  await client.getAdminVpnPanelSyncEvents('admin-token', 'sync-1')
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
  assert.equal(calls[6]?.url, 'http://localhost:8080/api/admin/vpn-inbounds')
  assert.equal(calls[7]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/inbounds')
  assert.match(String(calls[7]?.init?.body), /"isActive":true/)
  assert.equal(calls[8]?.url, 'http://localhost:8080/api/admin/vpn-inbounds/inbound-1')
  assert.equal(calls[8]?.init?.method, 'PATCH')
  assert.match(String(calls[8]?.init?.body), /"isActive":false/)
  assert.equal(calls[9]?.url, 'http://localhost:8080/api/admin/vpn-inbounds/inbound-1/set-default')
  assert.equal(calls[9]?.init?.method, 'POST')
  assert.equal(calls[10]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/clients')
  assert.equal(calls[11]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/disable')
  assert.equal(calls[11]?.init?.method, 'POST')
  assert.equal(calls[12]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/enable')
  assert.equal(calls[13]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/sync')
  assert.equal(calls[14]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/reset-traffic')
  assert.equal(calls[15]?.url, 'http://localhost:8080/api/admin/vpn-clients/client-1/migrate')
  assert.match(String(calls[15]?.init?.body), /inbound-2/)
  assert.equal(calls[16]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/sync-runs')
  assert.equal(calls[17]?.url, 'http://localhost:8080/api/admin/vpn-panel-sync-runs/sync-1/events')
  assert.equal(calls[18]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1/health-checks')
  assert.equal(calls[19]?.url, 'http://localhost:8080/api/admin/vpn-panels/panel-1')
  assert.equal(calls[19]?.init?.method, 'DELETE')
  assert.equal(new Headers(calls[0]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient admin dashboard and user overview endpoints are tokenized', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).includes('/overview')) {
      return new Response(JSON.stringify(adminUserOverviewFixture({ payments: [adminOverviewPaymentFixture()] })), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify(adminDashboardFixture({
      productionReadiness: {
        isReady: false,
        status: 'Blocked',
        checks: [{ key: 'payment-webhook', label: 'Webhook платежей', status: 'Blocked', message: 'Webhook URL не заполнен', category: 'Платежи', severity: 'critical', actionLabel: 'Открыть платежи', actionHref: '#payments' }]
      }
    })), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const dashboard = await client.getAdminDashboardSummary('admin-token')
  const overview = await client.getAdminUserOverview('admin-token', 'user-1')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/dashboard/summary')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/users/user-1/overview')
  assert.equal(new Headers(calls[1]?.init?.headers).get('Authorization'), 'Bearer admin-token')
  assert.equal(dashboard.productionReadiness?.checks[0]?.actionHref, '#payments')
  assert.equal(dashboard.productionReadiness?.checks[0]?.category, 'Платежи')
  assert.equal(overview.payments[0]?.providerPaymentId, 'provider-payment-1')
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
    return new Response(JSON.stringify([adminPaymentProviderAccountFixture({ id: 'account-1', name: 'Yoo', publicName: 'Yoo', shopId: 'shop', apiBaseUrl: '', extraSettingsJson: '{"apiSecret":"***"}' })]), {
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
    const body = adminPaymentProviderAccountFixture({ id: 'account-1', provider: 'Stripe', name: 'stripe-main', publicName: 'Stripe', shopId: 'shop', apiBaseUrl: 'https://api.stripe.com', returnUrl: '', webhookUrl: 'https://api.example.test/webhooks/payments/stripe', hasWebhookSecret: true })
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
      return new Response(JSON.stringify([adminPaymentFixture({
        refundedAmount: 25,
        canRefund: true,
        refundableAmount: 75,
      })]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    return new Response(JSON.stringify({ id: 'refund-1', paymentAttemptId: 'payment-1', provider: 'YooKassa', providerRefundId: 'rf-1', status: 'Succeeded', amount: 50, currency: 'RUB', reason: 'manual', createdAt: new Date().toISOString(), refundedAt: new Date().toISOString() }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const payments = await client.getAdminPayments('admin-token')
  const refund = await client.refundAdminPayment('admin-token', 'payment-1', 50, 'manual')
  const recheckedRefund = await client.recheckAdminRefund('admin-token', 'refund-1')

  assert.equal(payments[0]?.canRefund, true)
  assert.equal(payments[0]?.refundableAmount, 75)
  assert.deepEqual(payments[0]?.refundBlockers, [])
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/payments/payment-1/refund')
  assert.equal(calls[1]?.init?.method, 'POST')
  assert.match(String(calls[1]?.init?.body), /"amount":50/)
  assert.match(String(calls[1]?.init?.body), /manual/)
  assert.equal(refund.status, 'Succeeded')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/refunds/refund-1/recheck')
  assert.equal(calls[2]?.init?.method, 'POST')
  assert.equal(recheckedRefund.status, 'Succeeded')
})

test('ApiClient admin refunds require explicit retry readiness contract', async () => {
  globalThis.fetch = (async () => new Response(JSON.stringify([{
    id: 'refund-1',
    paymentAttemptId: 'payment-1',
    provider: 'YooKassa',
    providerRefundId: 'pending:refund-1',
    status: 'Unknown',
    amount: 50,
    currency: 'RUB',
    reason: 'manual',
    createdAt: new Date().toISOString(),
    refundedAt: null,
    recheckSupported: true,
    canRecheck: false,
    recheckBlockers: ['Не сохранён идентификатор возврата у провайдера.'],
    retrySupported: true,
    canRetry: true,
    retryBlockers: []
  }]), { status: 200, headers: { 'Content-Type': 'application/json' } })) as typeof fetch

  const refunds = await new ApiClient('http://localhost:8080').getAdminRefunds('admin-token')

  assert.equal(refunds[0]?.canRetry, true)
  assert.deepEqual(refunds[0]?.retryBlockers, [])
})

test('ApiClient rejects unknown admin refund status', async () => {
  globalThis.fetch = (async () => new Response(JSON.stringify([{
    id: 'refund-1', paymentAttemptId: 'payment-1', provider: 'YooKassa', providerRefundId: 'rf-1',
    status: 'ProviderCustomStatus', amount: 50, currency: 'RUB', reason: 'manual', createdAt: new Date().toISOString(), refundedAt: null,
    recheckSupported: true, canRecheck: false, recheckBlockers: ['blocked'], retrySupported: true, canRetry: false, retryBlockers: ['blocked']
  }]), { status: 200, headers: { 'Content-Type': 'application/json' } })) as typeof fetch

  await assert.rejects(
    () => new ApiClient('http://localhost:8080').getAdminRefunds('admin-token'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502)
})

test('ApiClient rejects provider diagnostics in admin refund response', async () => {
  globalThis.fetch = (async () => new Response(JSON.stringify({
    id: 'refund-1', paymentAttemptId: 'payment-1', provider: 'YooKassa', providerRefundId: 'rf-1',
    status: 'Unknown', amount: 50, currency: 'RUB', reason: 'manual', createdAt: new Date().toISOString(), refundedAt: null,
    rawResponse: '{"private":"provider-marker"}'
  }), { status: 202, headers: { 'Content-Type': 'application/json' } })) as typeof fetch

  await assert.rejects(
    () => new ApiClient('http://localhost:8080').refundAdminPayment('admin-token', 'payment-1', 50, 'manual'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502)
})

test('ApiClient admin payment webhook events require safe operational state', async () => {
  const validEvent = {
    id: 'webhook-1', provider: 'YooKassa', paymentAttemptId: 'payment-1', paymentProviderAccountId: 'account-1',
    providerPaymentId: 'provider-payment-1', externalEventId: 'event-1', eventType: 'payment.succeeded', status: 'Failed',
    signatureValidated: false, receivedAt: adminFixtureTimestamp, processedAt: adminFixtureTimestamp,
    isRetryable: true, isTerminal: false, requiresAttention: true
  }
  let responseBody: unknown = [validEvent]
  globalThis.fetch = (async () => new Response(JSON.stringify(responseBody), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch
  const client = new ApiClient('http://localhost:8080')

  assert.deepEqual(await client.getAdminPaymentWebhookEvents('admin-token'), [validEvent])

  for (const unsafeBody of [
    [{ ...validEvent, errorText: 'private-provider-exception' }],
    [{ ...validEvent, rawPayload: '{"secret":"provider"}' }],
    [{ ...validEvent, headersJson: '{"Authorization":"secret"}' }],
    [{ ...validEvent, status: 'ProviderPrivateFailure' }]
  ]) {
    responseBody = unsafeBody
    await assert.rejects(
      () => client.getAdminPaymentWebhookEvents('admin-token'),
      (error: unknown) => error instanceof ApiClientError && error.status === 502)
  }
})

test('admin source serializes finance commands by provider, order and payment resources', () => {
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.match(adminSource, /paymentProviderActionResourceKey\(editingId \|\| 'create'\)/)
  assert.match(adminSource, /paymentProviderActionResourceKey\(account\.id\)/)
  assert.match(adminSource, /paymentActionResourceKeys\(paymentId, payments\.find/)
  assert.match(adminSource, /orderActionResourceKey\(order\.id\)/)
  assert.match(adminSource, /paymentActionResourceKey\(order\.lastPaymentId\)/)
  assert.match(adminSource, /paymentActionResourceKeys\(payment\.id, payment\.orderId\)/)
  assert.match(adminSource, /maxLength=\{120\}/)
  assert.match(adminSource, /catch \(error\) \{\s*await action\.reloadAll\(\)\s*throw error\s*\}/)
  assert.match(adminSource, /const paymentActionBusy = isActionResourceBusy/)
  assert.match(adminSource, /aria-busy=\{providerFormActionBusy\}/)
})

test('admin source serializes managed configuration commands by entity and global resources', () => {
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.match(adminSource, /tariffActionResourceKey\(editingId \|\| 'create'\)/)
  assert.match(adminSource, /tariffActionResourceKey\(tariff\.id\)/)
  assert.match(adminSource, /appReleaseActionResourceKey\(editingId \|\| 'create'\)/)
  assert.match(adminSource, /appReleaseActionResourceKey\(release\.id\)/)
  assert.match(adminSource, /faqActionResourceKey\(editingId \|\| 'create'\)/)
  assert.match(adminSource, /faqActionResourceKey\(faqId\)/)
  assert.match(adminSource, /}, siteContentActionResourceKey\)/)
  assert.match(adminSource, /workScenarioActionResourceKey\(editingId \|\| 'create'\)/)
  assert.match(adminSource, /workScenarioActionResourceKey\(scenario\.id\)/)
  assert.match(adminSource, /}, botSettingsActionResourceKey\)/)
  assert.match(adminSource, /aria-busy=\{siteContentActionBusy\}/)
  assert.match(adminSource, /aria-busy=\{botSettingsActionBusy\}/)
})

test('admin source derives concurrent busy state only from resource owners', () => {
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.doesNotMatch(adminSource, /actionBusyId|setActionBusyId/)
  assert.match(adminSource, /paymentProviderActionResourceKey\(editingId \|\| 'create'\)/)
  assert.match(adminSource, /vpnPanelActionResourceKey\(editingId \|\| 'create'\)/)
  assert.match(adminSource, /serverActionResourceKey\(editingId \|\| 'create'\)/)
  assert.match(adminSource, /referralProgramActionResourceKey/)
  assert.match(adminSource, /notificationDeliveryActionResourceKey\(deliveryId\)/)
  assert.match(adminSource, /const providerFormActionBusy = isActionResourceBusy/)
  assert.match(adminSource, /const referralProgramFormActionBusy = isActionResourceBusy/)
})

test('ApiClient admin subscription and VPN access actions are confirmation-friendly POST calls', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    const path = new URL(String(url)).pathname
    const accessResult = (status: string) => ({ id: 'access-1', status, disabledAt: status === 'Disabled' ? '2026-08-09T08:00:00Z' : null, lastSyncedAt: '2026-08-09T08:00:00Z', revision: 2, usedTrafficBytes: 0, message: 'ok' })
    const body = path.endsWith('/extend')
      ? { id: 'sub-1', status: 'Active', endAt: '2026-09-08T08:00:00Z', gracePeriodEndAt: '2026-09-11T08:00:00Z' }
      : path.endsWith('/activate')
        ? { id: 'sub-1', status: 'Active', endAt: '2026-09-08T08:00:00Z', currentAccessId: null, access: null }
        : path.endsWith('/block')
          ? { id: 'sub-1', status: 'Blocked', blockReason: 'abuse' }
          : path.endsWith('/unblock')
            ? { id: 'sub-1', status: 'GracePeriod' }
            : path.endsWith('/cancel')
              ? { id: 'sub-1', status: 'Cancelled', cancelledAt: '2026-08-09T08:00:00Z' }
              : path.endsWith('/sync-access')
                ? { id: 'sub-1', currentAccessId: 'access-1', access: accessResult('Active') }
                : path.endsWith('/migrate')
                  ? { migrationJobId: 'migration-1', subscriptionId: 'sub-1', sourceNodeId: 'node-1', targetNodeId: 'node-2', status: 'completed' }
                  : path.endsWith('/disable')
                    ? accessResult('Disabled')
                    : accessResult('Active')
    return new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } })
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await client.extendAdminSubscription('admin-token', 'sub-1', 30, 'manual')
  await client.activateAdminSubscription('admin-token', 'sub-1', 'activate')
  await client.blockAdminSubscription('admin-token', 'sub-1', 'abuse')
  const unblockResult = await client.unblockAdminSubscription('admin-token', 'sub-1', 'resolved')
  await client.cancelAdminSubscription('admin-token', 'sub-1', 'customer request')
  await client.syncAdminSubscriptionAccess('admin-token', 'sub-1', 'manual subscription sync')
  await client.migrateAdminSubscription('admin-token', 'sub-1', 'node-2')
  await client.disableAdminAccess('admin-token', 'access-1', 'expired')
  await client.enableAdminAccess('admin-token', 'access-1', 'paid')
  await client.syncAdminAccess('admin-token', 'access-1', 'manual sync')
  await client.resetAdminAccessTraffic('admin-token', 'access-1', 'reset')

  assert.equal(calls[0]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/extend')
  assert.equal(calls[1]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/activate')
  assert.equal(calls[2]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/block')
  assert.equal(calls[3]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/unblock')
  assert.equal(unblockResult.status, 'GracePeriod')
  assert.equal(calls[4]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/cancel')
  assert.equal(calls[5]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/sync-access')
  assert.equal(calls[6]?.url, 'http://localhost:8080/api/admin/subscriptions/sub-1/migrate')
  assert.equal(calls[6]?.init?.body, '"node-2"')
  assert.equal(calls[7]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/disable')
  assert.equal(calls[8]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/enable')
  assert.equal(calls[9]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/sync')
  assert.equal(calls[10]?.url, 'http://localhost:8080/api/admin/access-credentials/access-1/reset-traffic')
  assert.equal(calls[0]?.init?.method, 'POST')
  assert.equal(new Headers(calls[10]?.init?.headers).get('Authorization'), 'Bearer admin-token')
})

test('ApiClient rejects malformed admin subscription and VPN access action results', async () => {
  globalThis.fetch = (async () => new Response(JSON.stringify({}), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const actions: Array<() => Promise<unknown>> = [
    () => client.extendAdminSubscription('admin-token', 'sub-1', 30),
    () => client.activateAdminSubscription('admin-token', 'sub-1'),
    () => client.blockAdminSubscription('admin-token', 'sub-1'),
    () => client.unblockAdminSubscription('admin-token', 'sub-1'),
    () => client.cancelAdminSubscription('admin-token', 'sub-1'),
    () => client.syncAdminSubscriptionAccess('admin-token', 'sub-1'),
    () => client.migrateAdminSubscription('admin-token', 'sub-1', null),
    () => client.disableAdminAccess('admin-token', 'access-1'),
    () => client.enableAdminAccess('admin-token', 'access-1'),
    () => client.syncAdminAccess('admin-token', 'access-1'),
    () => client.resetAdminAccessTraffic('admin-token', 'access-1')
  ]

  for (const action of actions) {
    await assert.rejects(action, (error: unknown) =>
      error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message))
  }
})

test('ApiClient admin order filters and recheck endpoints use finance-safe routes', async () => {
  const calls: Array<{ url: string; init?: RequestInit }> = []
  globalThis.fetch = (async (url: string | URL, init?: RequestInit) => {
    calls.push({ url: String(url), init })
    if (String(url).includes('/api/admin/orders?')) {
      return new Response(JSON.stringify([adminOrderFixture()]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify({ orderId: 'order-1', paymentId: 'payment-1', status: 'Succeeded' }), {
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

test('ApiClient rejects provider raw payload in admin payment recheck response', async () => {
  globalThis.fetch = (async () => new Response(JSON.stringify({
    orderId: 'order-1',
    paymentId: 'payment-1',
    status: 'Succeeded',
    rawResponse: '{"private":"provider-secret-marker"}'
  }), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await assert.rejects(
    () => client.recheckAdminPayment('admin-token', 'payment-1'),
    (error: unknown) => error instanceof ApiClientError && error.status === 502)
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
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{"provider":"Unknown","publicName":"Unknown","mode":"Sandbox","healthStatus":"Healthy"}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{"provider":"YooKassa","publicName":"YooKassa","mode":"Disabled","healthStatus":"Healthy"}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{"provider":"YooKassa","publicName":"First","mode":"Sandbox","healthStatus":"Healthy"},{"provider":"YooKassa","publicName":"Second","mode":"Production","healthStatus":"Healthy"}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json', 'Content-Length': '10000001' } }),
    new Response('provider failure', { status: 503, headers: { 'Content-Type': 'text/plain', 'Content-Length': '64001' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
    new Response('[{}]', { status: 200, headers: { 'Content-Type': 'application/json' } }),
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
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)
  )
  await assert.rejects(
    () => client.getFaq(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)
  )
  await assert.rejects(
    () => client.getHomeContent(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)
  )
  await assert.rejects(
    () => client.getPublicPaymentProviders(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)
  )
  await assert.rejects(
    () => client.getPublicPaymentProviders(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)
  )
  await assert.rejects(
    () => client.getPublicPaymentProviders(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)
  )
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /размер/i.test(error.message)
  )
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError && error.status === 502 && /размер/i.test(error.message)
  )
  const isInvalidResponseDataError = (error: unknown) =>
    error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)
  await assert.rejects(() => client.getMe('user-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getMySubscriptions('user-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getMyOrders('user-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getMyPayments('user-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getMyAccesses('user-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getMyReferrals('user-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getMySupportConversations('user-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getMySupportMessages('user-token', 'support-1'), isInvalidResponseDataError)
  await assert.rejects(() => client.getTelegramStatus('user-token'), isInvalidResponseDataError)
  await assert.rejects(
    () => client.createMyOrder('user-token', { tariffId: 'tariff-1', type: 'NewSubscription', paymentProvider: 'YooKassa' }),
    isInvalidResponseDataError
  )
  await assert.rejects(() => client.getMyPayment('user-token', 'payment-1'), isInvalidResponseDataError)
  await assert.rejects(
    () => client.createMySupportConversation('user-token', { subject: 'Subject', text: 'Message' }),
    isInvalidResponseDataError
  )
  await assert.rejects(
    () => client.replyMySupportConversation('user-token', 'support-1', 'Message', 0),
    isInvalidResponseDataError
  )
  await assert.rejects(() => client.unlinkTelegram('user-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getAdminSession('admin-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getAdminDashboardSummary('admin-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getAdminUsers('admin-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getAdminUserOverview('admin-token', 'user-1'), isInvalidResponseDataError)
  await assert.rejects(() => client.getAdminSubscriptions('admin-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getAdminAccesses('admin-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getAdminOrders('admin-token'), isInvalidResponseDataError)
  await assert.rejects(() => client.getAdminPayments('admin-token'), isInvalidResponseDataError)
  assert.deepEqual(await client.getTariffs(), [])
})

test('ApiClient rejects malformed admin finance, audit, notification and support DTOs', async () => {
  const responses = [
    '[{}]',
    '[{}]',
    '{}',
    '{}',
    '{}',
    '{}',
    '{}',
    '[{}]',
    '{}',
    '{}',
    '{}',
    '{}',
    '[{}]',
    '[{}]',
    '[{}]',
    '[{}]',
    '{}',
    '{}',
    '{}'
  ]
  globalThis.fetch = (async () => new Response(responses.shift(), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const providerPayload = {
    provider: 'YooKassa' as const,
    mode: 'Sandbox' as const,
    name: 'sandbox',
    publicName: 'Sandbox',
    isEnabled: true,
    isDefault: true,
    shopId: 'shop',
    apiBaseUrl: '',
    returnUrl: '',
    webhookUrl: '',
    secretKey: '',
    webhookSecret: '',
    useWebhookIpAllowList: false,
    allowedWebhookIpRangesCsv: '',
    extraSettingsJson: '{}'
  }
  const isInvalidResponseDataError = (error: unknown) =>
    error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)
  const operations = [
    () => client.getAdminAuditLogs('admin-token'),
    () => client.getAdminNotificationDeliveries('admin-token'),
    () => client.retryAdminNotificationDelivery('admin-token', 'delivery-1'),
    () => client.recheckAdminPayment('admin-token', 'payment-1'),
    () => client.recheckAdminOrderPayment('admin-token', 'order-1'),
    () => client.refundAdminPayment('admin-token', 'payment-1', 50),
    () => client.recheckAdminRefund('admin-token', 'refund-1'),
    () => client.getAdminPaymentProviderAccounts('admin-token'),
    () => client.createAdminPaymentProviderAccount('admin-token', providerPayload),
    () => client.updateAdminPaymentProviderAccount('admin-token', 'provider-1', providerPayload),
    () => client.setAdminPaymentProviderAccountEnabled('admin-token', 'provider-1', false),
    () => client.checkAdminPaymentProviderAccount('admin-token', 'provider-1'),
    () => client.getAdminPaymentWebhookEvents('admin-token'),
    () => client.getAdminRefunds('admin-token'),
    () => client.getAdminSupportConversations('admin-token'),
    () => client.getAdminSupportMessages('admin-token', 'conversation-1'),
    () => client.replyAdminSupportConversation('admin-token', 'conversation-1', 'Reply', 1),
    () => client.updateAdminSupportConversationStatus('admin-token', 'conversation-1', 'pending', 1),
    () => client.addAdminSupportInternalNote('admin-token', 'conversation-1', 'Note', 1)
  ]

  for (const operation of operations) {
    await assert.rejects(operation, isInvalidResponseDataError)
  }
  assert.equal(responses.length, 0)
})

test('ApiClient rejects malformed admin content and app release DTOs', async () => {
  const responses = [
    '{}', '[{}]', '{}',
    '[{}]', '{}', '{}', '{}',
    '[{}]', '[{}]', '{}', '{}',
    '[{}]', '{}', '{}', '{}', '{}',
    '[{}]', '{}', '{}', '{}', '{}',
    '[{}]', '{}', '{}', '{}', '{}', '{}',
    '[{}]', '{}', '{}', '{}'
  ]
  globalThis.fetch = (async () => new Response(responses.shift(), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const tariffPayload = { name: 'Premium', featuresJson: '[]' }
  const referralPayload = {
    name: 'Welcome',
    status: 'active',
    startAt: null,
    endAt: null,
    ruleDefinition: '{}',
    rewardDefinition: '{}',
    antiFraudSettings: '{}'
  }
  const releasePayload = {
    releaseId: 'release-1',
    version: '0.2.0',
    releasedAt: adminFixtureTimestamp,
    title: 'Что нового',
    summary: 'Описание',
    isActive: true,
    source: 'manual' as const,
    items: [{ type: 'new' as const, text: 'Пункт', sortOrder: 10 }]
  }
  const faqPayload = {
    question: 'Как?',
    answer: 'Так',
    category: 'Общее',
    isActive: true,
    showOnHome: true,
    showOnFaqPage: true,
    sortOrder: 10
  }
  const siteContentPayload = {
    key: 'home.hero.title',
    value: 'VPN',
    group: 'home',
    label: 'Hero title',
    inputType: 'text',
    isActive: true,
    sortOrder: 10
  }
  const scenarioPayload = {
    name: 'Auto',
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
    onSubscriptionExpired: 'disable_access_after_grace',
    onRenewal: 'extend_subscription',
    cabinetText: 'Ready',
    telegramText: 'Ready',
    generateQrCode: true,
    maxDevices: 3,
    trafficLimit: null,
    sortOrder: 10
  }
  const operations = [
    () => client.getLatestAppVersion('user-token'),
    () => client.getAppVersionHistory('user-token'),
    () => client.markAppVersionSeen('user-token', 'release-1'),
    () => client.getAdminTariffs('admin-token'),
    () => client.createAdminTariff('admin-token', tariffPayload),
    () => client.updateAdminTariff('admin-token', 'tariff-1', tariffPayload),
    () => client.deleteAdminTariff('admin-token', 'tariff-1'),
    () => client.getAdminReferralPrograms('admin-token'),
    () => client.getAdminReferralRewards('admin-token'),
    () => client.createAdminReferralProgram('admin-token', referralPayload),
    () => client.updateAdminReferralProgram('admin-token', 'program-1', referralPayload),
    () => client.getAdminAppReleases('admin-token'),
    () => client.getAdminAppReleaseOverview('admin-token'),
    () => client.createAdminAppRelease('admin-token', releasePayload),
    () => client.updateAdminAppRelease('admin-token', 'release-guid', releasePayload),
    () => client.deleteAdminAppRelease('admin-token', 'release-guid'),
    () => client.getAdminFaq('admin-token'),
    () => client.getAdminFaqOverview('admin-token'),
    () => client.createAdminFaq('admin-token', faqPayload),
    () => client.updateAdminFaq('admin-token', 'faq-1', faqPayload),
    () => client.deleteAdminFaq('admin-token', 'faq-1'),
    () => client.getAdminSiteContent('admin-token'),
    () => client.getAdminHomeContentReadiness('admin-token'),
    () => client.restoreAdminHomeContentDefaults('admin-token'),
    () => client.createAdminSiteContent('admin-token', siteContentPayload),
    () => client.updateAdminSiteContent('admin-token', 'content-1', siteContentPayload),
    () => client.deleteAdminSiteContent('admin-token', 'content-1'),
    () => client.getAdminWorkScenarios('admin-token'),
    () => client.createAdminWorkScenario('admin-token', scenarioPayload),
    () => client.updateAdminWorkScenario('admin-token', 'scenario-1', scenarioPayload),
    () => client.deleteAdminWorkScenario('admin-token', 'scenario-1')
  ]
  const isInvalidResponseDataError = (error: unknown) =>
    error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)

  for (const operation of operations) {
    await assert.rejects(operation, isInvalidResponseDataError)
  }
  assert.equal(responses.length, 0)
})

test('ApiClient rejects malformed VPN panel, inbound, client and observation DTOs', async () => {
  const responses = [
    '[{}]', '{}', JSON.stringify(vpnPanelFixture({ id: 'panel-other' })),
    JSON.stringify({ id: 'panel-other', deleted: true, archived: false, linkedInbounds: 0, linkedClients: 0, linkedSyncRuns: 0, linkedHealthChecks: 0 }),
    JSON.stringify(panelHealthCheckFixture({ vpnPanelId: 'panel-other' })),
    JSON.stringify(panelSyncRunFixture({ vpnPanelId: 'panel-other' })),
    JSON.stringify([vpnInboundFixture({ vpnPanelId: 'panel-other' })]),
    JSON.stringify([vpnInboundFixture(), vpnInboundFixture({ id: 'inbound-2' })]),
    JSON.stringify(vpnInboundFixture({ vpnPanelId: 'panel-other' })),
    JSON.stringify(vpnInboundFixture({ id: 'inbound-other' })),
    JSON.stringify(vpnInboundFixture({ id: 'inbound-other' })),
    JSON.stringify([vpnClientFixture({ vpnPanelId: 'panel-other' })]),
    JSON.stringify(vpnClientFixture({ id: 'client-other' })),
    JSON.stringify(vpnClientFixture({ id: 'client-other' })),
    JSON.stringify(vpnClientFixture({ id: 'client-other' })),
    JSON.stringify(vpnClientFixture({ id: 'client-other' })),
    JSON.stringify(vpnClientFixture({ id: 'client-other' })),
    JSON.stringify([panelSyncRunFixture({ vpnPanelId: 'panel-other' })]),
    JSON.stringify([panelSyncEventFixture({ panelSyncRunId: 'sync-other' })]),
    JSON.stringify([panelHealthCheckFixture({ vpnPanelId: 'panel-other' })])
  ]
  globalThis.fetch = (async () => new Response(responses.shift(), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const panelPayload = {
    name: 'Panel',
    baseUrl: 'https://panel.example.test',
    login: 'admin',
    password: 'secret',
    region: 'eu',
    capacity: 100,
    sslVerificationMode: 'Strict',
    apiVariant: 'X3UiOfficial',
    autoCreateInbound: false,
    defaultInboundTemplateJson: '{}'
  }
  const inboundPayload = {
    name: 'default-vless',
    protocol: 'vless',
    port: 443,
    listen: '',
    settingsJson: '{"clients":[]}',
    streamSettingsJson: '{"network":"tcp"}',
    sniffingJson: '{}',
    isDefault: true,
    capacity: 100,
    isActive: true
  }
  const operations = [
    () => client.getAdminVpnPanels('admin-token'),
    () => client.createAdminVpnPanel('admin-token', panelPayload),
    () => client.updateAdminVpnPanel('admin-token', 'panel-1', panelPayload),
    () => client.deleteAdminVpnPanel('admin-token', 'panel-1'),
    () => client.testAdminVpnPanel('admin-token', 'panel-1'),
    () => client.syncAdminVpnPanel('admin-token', 'panel-1'),
    () => client.getAdminVpnPanelInbounds('admin-token', 'panel-1'),
    () => client.getAdminVpnInbounds('admin-token'),
    () => client.createAdminVpnPanelInbound('admin-token', 'panel-1', inboundPayload),
    () => client.setAdminVpnInboundDefault('admin-token', 'inbound-1'),
    () => client.updateAdminVpnInbound('admin-token', 'inbound-1', inboundPayload),
    () => client.getAdminVpnPanelClients('admin-token', 'panel-1'),
    () => client.enableAdminVpnClient('admin-token', 'client-1'),
    () => client.disableAdminVpnClient('admin-token', 'client-1'),
    () => client.syncAdminVpnClient('admin-token', 'client-1'),
    () => client.resetAdminVpnClientTraffic('admin-token', 'client-1'),
    () => client.migrateAdminVpnClient('admin-token', 'client-1', 'inbound-2'),
    () => client.getAdminVpnPanelSyncRuns('admin-token', 'panel-1'),
    () => client.getAdminVpnPanelSyncEvents('admin-token', 'sync-1'),
    () => client.getAdminVpnPanelHealthChecks('admin-token', 'panel-1')
  ]
  const isInvalidResponseDataError = (error: unknown) =>
    error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)

  for (const operation of operations) {
    await assert.rejects(operation, isInvalidResponseDataError)
  }
  assert.equal(responses.length, 0)
})

test('ApiClient rejects malformed server, provisioning and Telegram bot DTOs', async () => {
  const responses = [
    '[{}]', '{}', JSON.stringify(vpnNodeFixture({ id: 'node-other' })),
    JSON.stringify({ id: 'node-other', deleted: true, archived: false, linkedSubscriptions: 0, linkedAccesses: 0, linkedProvisioningRuns: 0, linkedHealthChecks: 0, linkedMigrationJobs: 0 }),
    JSON.stringify(vpnNodeFixture({ id: 'node-other' })),
    JSON.stringify(nodeHealthCheckFixture({ nodeId: 'node-other' })),
    JSON.stringify([nodeHealthCheckFixture({ nodeId: 'node-other' })]),
    JSON.stringify(vpnNodeFixture({ id: 'node-other' })),
    JSON.stringify(vpnNodeFixture({ id: 'node-other' })),
    JSON.stringify(vpnNodeFixture({ id: 'node-other' })),
    JSON.stringify(vpnNodeFixture({ id: 'node-other' })),
    JSON.stringify(provisioningCommandFixture({ serverId: 'node-other', dryRun: true, mode: 'dry-run', modeTitle: 'Dry-run precheck', riskLevel: 'safe', nextAction: 'Проверьте precheck.', operatorWarning: 'Dry-run не меняет VPS.' })),
    JSON.stringify(provisioningCommandFixture({ serverId: 'node-other' })),
    '[{}]',
    JSON.stringify(provisioningRunDetailsFixture({ run: { ...provisioningRunDetailsFixture().run, id: 'run-other' } })),
    JSON.stringify(provisioningCommandFixture({ runId: 'run-other' })),
    JSON.stringify(provisioningCommandFixture({ dryRun: true, mode: 'dry-run', modeTitle: 'Dry-run precheck', riskLevel: 'safe', nextAction: 'Проверьте precheck.', operatorWarning: 'Dry-run не меняет VPS.' })),
    JSON.stringify({ runId: 'run-other', status: 'cancelled' }),
    JSON.stringify({ runId: 'run-other', supportConversationId: 'support-1' }),
    '{}',
    JSON.stringify(telegramBotConnectionCheckFixture({ isReady: true, status: 'needs_configuration', requiredActions: ['Укажите токен.'] })),
    '{}'
  ]
  globalThis.fetch = (async () => new Response(responses.shift(), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const serverPayload = {
    name: 'node', host: 'node.example.test', ipAddress: '', provider: 'local', region: 'eu', country: 'NL', datacenter: 'test',
    capacity: 100, priority: 10, sshPort: 22, skipHostKeyChecking: true, publicPort: 443
  }
  const operations = [
    () => client.getAdminServers('admin-token'),
    () => client.createAdminServer('admin-token', serverPayload),
    () => client.updateAdminServer('admin-token', 'node-1', serverPayload),
    () => client.deleteAdminServer('admin-token', 'node-1'),
    () => client.disableAdminServer('admin-token', 'node-1'),
    () => client.checkAdminServerHealth('admin-token', 'node-1'),
    () => client.getAdminServerHealthChecks('admin-token', 'node-1'),
    () => client.enableAdminServerAllocation('admin-token', 'node-1'),
    () => client.disableAdminServerAllocation('admin-token', 'node-1'),
    () => client.enableAdminServerMaintenance('admin-token', 'node-1'),
    () => client.disableAdminServerMaintenance('admin-token', 'node-1'),
    () => client.precheckAdminServer('admin-token', 'node-1'),
    () => client.queueAdminProvision('admin-token', 'node-1'),
    () => client.getAdminProvisioningRuns('admin-token'),
    () => client.getAdminProvisioningRun('admin-token', 'run-1'),
    () => client.retryAdminProvisioningRun('admin-token', 'run-1'),
    () => client.deployAdminProvisioningRun('admin-token', 'run-1'),
    () => client.cancelAdminProvisioningRun('admin-token', 'run-1'),
    () => client.markAdminProvisioningSupportNeeded('admin-token', 'run-1'),
    () => client.getAdminTelegramBotSettings('admin-token'),
    () => client.testAdminTelegramBotSettings('admin-token'),
    () => client.updateAdminTelegramBotSettings('admin-token', { enabled: false })
  ]
  const isInvalidResponseDataError = (error: unknown) =>
    error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)

  for (const operation of operations) {
    await assert.rejects(operation, isInvalidResponseDataError)
  }
  assert.equal(responses.length, 0)
})

test('ApiClient rejects malformed auth, checkout, payment and cabinet action DTOs', async () => {
  const now = new Date().toISOString()
  const checkout = {
    id: 'checkout-1',
    token: 'checkout-token',
    tariffId: 'tariff-1',
    userId: null,
    orderId: null,
    status: 'open',
    expiresAt: now,
    emailHint: null
  }
  const order = {
    id: 'order-1',
    userId: 'user-1',
    tariffId: 'tariff-1',
    amount: 490,
    currency: 'RUB',
    status: 'PendingPayment',
    expiresAt: now,
    linkedSubscriptionId: null
  }
  const responses = [
    { accessToken: 'same-token', refreshToken: 'same-token', email: 'user@example.test', displayName: 'User' },
    { accessToken: 'access-1', refreshToken: 'refresh-1', email: 'invalid-email', displayName: 'User' },
    {},
    { status: 'revoked' },
    { accepted: false, message: 'queued', validationResetToken: null },
    { status: 'ok' },
    { ...checkout, status: 'PendingAuth' },
    { ...checkout, token: 'checkout-other' },
    { ...order, linkedSubscriptionId: undefined },
    { ...order, status: 'UnknownOrderStatus' },
    { paymentId: 'payment-1', redirectUrl: 'https://user:password@pay.example.test/order-1', rawResponse: '{}' },
    { conversationId: 'support-other', status: 'closed', revision: 2 },
    { token: 'link-token', deepLinkUrl: 'https://t.me/vpn_bot?start=link_other-token', expiresAt: now }
  ]
  globalThis.fetch = (async () => new Response(JSON.stringify(responses.shift()), {
    status: 200,
    headers: { 'Content-Type': 'application/json' }
  })) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  const operations = [
    () => client.register('user@example.test', 'Password123!', 'User'),
    () => client.login('user@example.test', 'Password123!'),
    () => client.refresh('refresh-token'),
    () => client.logout('access-token', 'refresh-token'),
    () => client.forgotPassword('user@example.test'),
    () => client.resetPassword('reset-token', 'NewPassword123!'),
    () => client.createCheckoutSession({ tariffId: 'tariff-1', type: 'NewSubscription', paymentProvider: 'YooKassa' }),
    () => client.getCheckoutSession('checkout-token'),
    () => client.claimCheckoutSession('access-token', 'checkout-token'),
    () => client.createMyOrder('access-token', { tariffId: 'tariff-1', type: 'Renewal', paymentProvider: 'YooKassa', subscriptionId: 'subscription-1' }),
    () => client.initMyPayment('access-token', 'order-1', 'YooKassa'),
    () => client.updateMySupportConversationStatus('access-token', 'support-1', 'closed', 1),
    () => client.createTelegramLinkToken('access-token')
  ]
  const isInvalidResponseDataError = (error: unknown) =>
    error instanceof ApiClientError && error.status === 502 && /некорректными данными/i.test(error.message)

  for (const operation of operations) {
    await assert.rejects(operation, isInvalidResponseDataError)
  }
  assert.equal(responses.length, 0)
  assert.equal((client as unknown as Record<string, unknown>).createPublicOrder, undefined)
  assert.equal((client as unknown as Record<string, unknown>).initPayment, undefined)
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

test('ApiClient converts native network failures to a controlled Russian error', async () => {
  globalThis.fetch = (async () => {
    throw new TypeError('Failed to fetch')
  }) as typeof fetch

  const client = new ApiClient('http://localhost:8080')
  await assert.rejects(
    () => client.getTariffs(),
    (error: unknown) => error instanceof ApiClientError
      && error.status === 0
      && error.payload === null
      && error.message === 'Не удалось связаться с сервером. Проверьте подключение и повторите попытку.'
  )
})

test('ApiClient preserves caller-requested aborts as control flow', async () => {
  globalThis.fetch = ((_url: string | URL, init?: RequestInit) => new Promise((_resolve, reject) => {
    const signal = init?.signal
    const rejectAbort = () => reject(signal?.reason ?? new DOMException('Aborted', 'AbortError'))
    if (signal?.aborted) rejectAbort()
    else signal?.addEventListener('abort', rejectAbort, { once: true })
  })) as typeof fetch

  const controller = new AbortController()
  const client = new ApiClient('http://localhost:8080')
  const requestArray = (client as unknown as {
    requestArray<T>(path: string, init?: RequestInit): Promise<T[]>
  }).requestArray.bind(client)
  const pending = requestArray('/api/public/tariffs', { signal: controller.signal })
  controller.abort(new DOMException('Caller cancelled', 'AbortError'))

  await assert.rejects(pending, (error: unknown) => error instanceof DOMException && error.name === 'AbortError')
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
      return new Response(JSON.stringify({ currentVersion: '0.2.0', latestRelease: appReleaseFixture(), seenByCurrentUser: true }), {
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
      return new Response(JSON.stringify([appReleaseFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }

    if (String(url).endsWith('/mark-seen')) {
      return new Response(JSON.stringify({ releaseId: 'release-1', seen: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    if (init?.method === 'DELETE') {
      return new Response(JSON.stringify({ id: 'release-guid', deleted: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
      })
    }

    return new Response(JSON.stringify(appReleaseFixture()), {
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
  assert.match(cabinetSource, /sessionRequestIdRef/)
  assert.match(cabinetSource, /sessionKeyRef/)
  assert.match(cabinetSource, /latestRequestIdRef/)
  assert.match(cabinetSource, /latestLoadInFlightRef/)
  assert.match(cabinetSource, /latestManualFeedbackRequestedRef/)
  assert.match(cabinetSource, /markSeenInFlightRef/)
  assert.match(cabinetSource, /activeRequest\.releaseId === release\.releaseId/)
  assert.match(cabinetSource, /markSeenInFlightRef\.current === request/)
  assert.match(cabinetSource, /const loadLatest = useCallback/)
  assert.match(cabinetSource, /if \(latestLoadInFlightRef\.current\) return/)
  assert.match(cabinetSource, /loadLatest\(false\)/)
  assert.match(cabinetSource, /loadLatest\(true\)/)
  assert.match(cabinetSource, /historyRequestIdRef/)
  assert.match(cabinetSource, /historyAttempted/)
  assert.match(cabinetSource, /Повторить загрузку истории/)
  assert.match(cabinetSource, /Не удалось загрузить информацию об обновлениях/)
  assert.match(cabinetSource, /Повторить загрузку/)
  assert.match(cabinetAppSource, /Повторить загрузку способов оплаты/)
  assert.match(cabinetAppSource, /paymentProvidersEffectToken/)
  assert.match(cabinetAppSource, /const paymentProvidersLoadInFlight = useRef<CabinetScopedLoadRequest \| null>\(null\)/)
  assert.match(cabinetAppSource, /activeRequest\.scopeKey === scopeKey[\s\S]*return activeRequest\.promise/)
  assert.match(cabinetAppSource, /supportMessagesError/)
  assert.match(cabinetAppSource, /Повторить загрузку переписки/)
  assert.match(cabinetAppSource, /const supportMessagesLoadInFlight = useRef<CabinetScopedLoadRequest \| null>\(null\)/)
  assert.match(cabinetAppSource, /activeRequest\.scopeKey === conversationId[\s\S]*return activeRequest\.promise/)
  assert.match(adminSource, /supportMessagesError/)
  assert.match(adminSource, /Повторить загрузку сообщений/)
  assert.match(adminSource, /userOverviewError/)
  assert.match(adminSource, /Повторить загрузку карточки/)
  assert.match(adminSource, /vpnPanelDetailsError/)
  assert.match(adminSource, /Повторить загрузку деталей/)
  assert.match(adminSource, /const \[adminDataReady, setAdminDataReady\] = useState\(false\)/)
  assert.match(adminSource, /setAdminDataReady\(true\)/)
  assert.match(adminSource, /if \(!adminDataReady\)/)
  assert.match(adminSource, /id="admin-data-loading"/)
  assert.match(adminSource, /const adminSectionLoadAreas: Record<AdminSectionId, readonly string\[\]>/)
  assert.match(adminSource, /activeSectionLoadErrors/)
  assert.match(adminSource, /id="admin-section-load-error"/)
  assert.match(adminSource, /Повторить загрузку раздела/)
  assert.equal((adminSource.match(/hidden=\{activeSection !== '[^']+' \|\| activeSectionLoadFailed\}/g) ?? []).length, 19)
  assert.match(adminSource, /const loadAllInFlight = useRef<AdminLoadRequest \| null>\(null\)/)
  assert.match(adminSource, /activeRequest\?\.operationId === operationId[\s\S]*activeRequest\.token === currentToken[\s\S]*activeRequest\.session === currentSession/)
  assert.match(adminSource, /if \(loadAllInFlight\.current === request\) loadAllInFlight\.current = null/)
  assert.match(adminSource, /const usersLoadInFlight = useRef<AdminUsersLoadRequest \| null>\(null\)/)
  assert.match(adminSource, /activeRequest\.search === search[\s\S]*activeRequest\.status === status/)
  assert.match(adminSource, /Повторить загрузку пользователей/)
  assert.match(adminSource, /!usersLoading && !usersError/)
  assert.match(adminSource, /const userOverviewLoadInFlight = useRef<AdminDetailLoadRequest \| null>\(null\)/)
  assert.match(adminSource, /const supportMessagesLoadInFlight = useRef<AdminDetailLoadRequest \| null>\(null\)/)
  assert.match(adminSource, /const vpnPanelDetailsLoadInFlight = useRef<AdminDetailLoadRequest \| null>\(null\)/)
  assert.match(adminSource, /activeRequest\.entityId === userId[\s\S]*return activeRequest\.promise/)
  assert.match(adminSource, /activeRequest\.entityId === conversationId[\s\S]*return activeRequest\.promise/)
  assert.match(adminSource, /activeRequest\.entityId === panelId[\s\S]*return activeRequest\.promise/)
  assert.match(adminSource, /const loginRequestInFlight = useRef<AdminSessionCommandRequest \| null>\(null\)/)
  assert.match(adminSource, /const refreshSessionRequestInFlight = useRef<AdminSessionCommandRequest \| null>\(null\)/)
  assert.match(adminSource, /const logoutRequestInFlight = useRef<AdminSessionCommandRequest \| null>\(null\)/)
  assert.match(adminSource, /sessionOperationId\.current === activeRequest\.operationId[\s\S]*return activeRequest\.promise/)
  assert.match(adminSource, /activeRequest\.key === refreshToken[\s\S]*return activeRequest\.promise/)
  assert.match(adminSource, /api\.logout\(submittedAccessToken \|\| null, submittedRefreshToken \|\| null\)/)
  assert.match(cabinetSource, /if \(!token \|\| !userId\) return/)
  assert.match(cabinetSource, /if \(!token\) \{\s*onManualOpenHandled\(\)\s*return\s*\}\s*if \(!userId\) return/)
  assert.match(cabinetSource, /sessionRequestIdRef\.current === sessionRequestId/)
  assert.match(cabinetAppSource, /const cabinetDataReady = profile !== null/)
  assert.match(cabinetAppSource, /\{cabinetDataReady && \(/)
  assert.match(cabinetAppSource, /type CabinetLoadArea = 'profile' \| 'subscriptions' \| 'orders' \| 'payments' \| 'accesses' \| 'referrals' \| 'support' \| 'telegram'/)
  assert.equal((cabinetAppSource.match(/loadArea\('(profile|subscriptions|orders|payments|accesses|referrals|support|telegram)'/g) ?? []).length, 8)
  assert.match(cabinetAppSource, /setLoadErrors\(nextLoadErrors\)/)
  assert.match(cabinetAppSource, /renderCabinetLoadError/)
  assert.match(cabinetAppSource, /Повторить загрузку данных/)
  assert.match(cabinetSource, /historyRequestIdRef\.current === historyRequestId/)
  assert.doesNotMatch(cabinetSource, /\.then\(\(items\) => setHistory\(items\)\)/)
  assert.doesNotMatch(cabinetSource, /\.finally\(\(\) => setLoadingHistory\(false\)\)/)
  assert.doesNotMatch(cabinetSource, /userId \|\| ['"]anonymous['"]/)
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
      ? { id: 'delivery-1', status: 'Pending', nextAttemptAt: adminFixtureTimestamp }
      : [{ id: 'delivery-1', userId: 'user-1', templateKey: 'password_reset_requested', channel: 'Email', maskedToAddress: 'us***@example.test', status: 'Failed', attempts: 5, processingStartedAt: null, nextAttemptAt: null, sentAt: null, errorText: 'SMTP unavailable', createdAt: adminFixtureTimestamp, updatedAt: adminFixtureTimestamp }]), {
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

test('frontend sources include managed public content surfaces', () => {
  const publicSource = readFileSync(new URL('../apps/public-web/src/App.tsx', import.meta.url), 'utf8')
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.match(publicSource, /getFaq/)
  assert.match(publicSource, /getHomeFaq/)
  assert.match(publicSource, /faq-toolbar/)
  assert.match(publicSource, /category/)
  assert.match(publicSource, /faqInitialLoadStartedRef/)
  assert.match(publicSource, /faqLoadInFlightRef/)
  assert.match(publicSource, /requestId === faqRequestIdRef\.current/)
  assert.match(publicSource, /Повторить загрузку/)
  assert.match(publicSource, /landingInitialLoadStartedRef/)
  assert.match(publicSource, /homeFaqLoadInFlightRef/)
  assert.match(publicSource, /requestId === homeFaqRequestIdRef\.current/)
  assert.match(publicSource, /Повторить загрузку FAQ/)
  assert.match(publicSource, /function useManagedHomeContent\(\)/)
  assert.match(publicSource, /managedHomeContentInitialLoadStartedRef/)
  assert.match(publicSource, /managedHomeContentMountedRef\.current/)
  assert.equal((publicSource.match(/const pageContent = useManagedHomeContent\(\)/g) ?? []).length, 2)
  assert.match(publicSource, /tariffsInitialLoadStartedRef/)
  assert.match(publicSource, /tariffsLoadInFlightRef/)
  assert.match(publicSource, /requestId === tariffsRequestIdRef\.current/)
  assert.match(publicSource, /paymentProvidersInitialLoadStartedRef/)
  assert.match(publicSource, /paymentProvidersLoadInFlightRef/)
  assert.match(publicSource, /requestId !== paymentProvidersRequestIdRef\.current/)
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
  assert.match(source, /Сначала будет занято по одному временному slot целевой панели, inbound и связанного VPN-сервера/)
  assert.match(source, /migrationOptionGroupsForClient/)
  assert.match(source, /getAdminVpnInbounds/)
  assert.match(source, /client\.vpnPanelId !== saved\.vpnPanelId/)
  assert.match(source, /Math\.max\(0, panel\.usedCapacity - 1\)/)
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
      return new Response(JSON.stringify(adminDashboardFixture()), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.includes('/api/admin/users/user-1/overview')) {
      return new Response(JSON.stringify(adminUserOverviewFixture()), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/users?search=user')) {
      return new Response(JSON.stringify([adminUserFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/orders')) {
      return new Response(JSON.stringify([adminOrderFixture({ linkedSubscriptionId: 'sub-1' })]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/payments')) {
      return new Response(JSON.stringify([adminPaymentFixture({ providerPaymentId: 'sandbox-pay-1' })]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/subscriptions')) {
      return new Response(JSON.stringify([adminSubscriptionFixture()]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/access-credentials')) {
      return new Response(JSON.stringify([adminAccessFixture({ accessUri: 'vless://sandbox/client' })]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/provisioning-runs')) {
      return new Response(JSON.stringify([provisioningRunFixture({ targetHost: 'vps.example.test', executionLog: 'password=***', executionLogPreview: 'password=***' })]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/provisioning-runs/run-1')) {
      const details = provisioningRunDetailsFixture()
      details.run = { ...details.run, targetHost: 'vps.example.test', executionLog: 'credential=***', linkedAccessId: 'access-1' }
      details.steps = details.steps.map((step) => ({ ...step, output: 'secret=***' }))
      return new Response(JSON.stringify(details), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/payment-providers/accounts')) {
      return new Response(JSON.stringify([adminPaymentProviderAccountFixture({ id: 'ppa-1', webhookUrl: 'https://api.example.test/webhooks/payments/yookassa', extraSettingsJson: '{"apiSecret":"***"}' })]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/admin/telegram-bot/settings')) {
      return new Response(JSON.stringify(telegramBotSettingsFixture({ publicBotUsername: 'vpn_bot', webAppUrl: 'http://localhost:5174' })), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/public/payments/providers')) {
      return new Response(JSON.stringify([{ provider: 'YooKassa', publicName: 'YooKassa Sandbox', mode: 'Sandbox', healthStatus: 'Unknown' }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/public/tariffs')) {
      return new Response(JSON.stringify([{
        id: 'tariff-1', name: 'Monthly', slug: 'monthly', description: '', fullDescription: '', features: [], featuresJson: '[]', badge: '',
        durationDays: 30, price: 299, currency: 'RUB', maxDevices: 2, trafficLimit: null, isTrial: false, isActive: true,
        sortOrder: 10, visibleFrom: null, visibleTo: null, tariffType: 'Personal', category: 'default', allowedRegionsCsv: '',
        allowedNodeGroupsCsv: '', isReferralEligible: true, provisioningScenario: 'auto', afterPaymentText: '',
        createdAt: '2026-08-05T00:00:00Z', updatedAt: '2026-08-05T00:00:00Z'
      }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (path.endsWith('/api/me/accesses')) {
      return new Response(JSON.stringify([{
        id: 'access-1',
        subscriptionId: 'sub-1',
        subscriptionStatus: 'Active',
        isTerminal: false,
        serverName: 'Sandbox node',
        accessUri: 'vless://sandbox/client',
        status: 'Active',
        expiryDate: null
      }]), { status: 200, headers: { 'Content-Type': 'application/json' } })
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
  const cabinetPublicUrlSource = readFileSync(new URL('../apps/cabinet/src/cabinet-public-url.ts', import.meta.url), 'utf8')
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
  assert.match(cabinetSource, /<select value=\{provider\} disabled=\{busy \|\| paymentProvidersLoading \|\| paymentProviders\.length === 0\} aria-busy=\{busy\}/)
  assert.match(cabinetSource, /onClick=\{handleRetryRenewalPayment\}[\s\S]*?disabled=\{busy \|\| !renewalProviderAvailability\?\.canInitialize\}[\s\S]*?title=\{renewalProviderAvailability\?\.reason \?\? undefined\}[\s\S]*?aria-busy=\{busy\}/)
  assert.match(cabinetSource, /const refreshedOrder = nextOrders\.find\(\(item\) => item\.id === current\.order\.id\)/)
  assert.match(cabinetSource, /renewalPaymentAvailability\?\.canRetry/)
  assert.match(cabinetSource, /renewalPaymentAvailability\?\.shouldCreateNewOrder/)
  assert.match(cabinetSource, /payment\.confirmationUrl && canOpenOrderPaymentConfirmation\(ordersById\.get\(payment\.orderId\), payment\.status, paymentNow\)/)
  assert.match(cabinetSource, /canOpenOrderPaymentConfirmation\(retryPaymentState\.order, retryPaymentAttempt\?\.status \?\? null, paymentNow\)/)
  assert.match(cabinetSource, /const cabinetNow = new Date\(\)/)
  assert.match(cabinetSource, /const paymentNow = cabinetNow/)
  assert.match(cabinetSource, /getNextOrderPaymentExpiryDelay\(trackedOrders, paymentNow\)/)
  assert.match(cabinetSource, /setPaymentClockTick\(\(current\) => current \+ 1\)/)
  assert.match(cabinetSource, /getNextCabinetAccessExpiryDelay\(subscriptions, accesses, cabinetNow\)/)
  assert.match(cabinetSource, /setAccessClockTick\(\(current\) => current \+ 1\)/)
  assert.match(cabinetSource, /setRetryPaymentState\(\(current\) =>/)
  assert.match(cabinetSource, /loadAllInFlight/)
  assert.match(cabinetSource, /activeRequest\?\.operationId === operationId && activeRequest\.token === currentToken/)
  assert.match(cabinetSource, /loadAllInFlight\.current === request/)
  assert.match(cabinetSource, /getMyAccessQrSvg/)
  assert.match(cabinetSource + adminSource, /QrCodePreview/)
  assert.doesNotMatch(cabinetSource + adminSource, /dangerouslySetInnerHTML/)
  assert.match(uiSource, /getSafeSvgImageDataUrl/)
  assert.match(apiClientSource, /expectedContentType: 'image\/svg\+xml'/)
  assert.match(cabinetSource, /VITE_PUBLIC_WEB_URL/)
  assert.match(cabinetSource, /resolveCabinetPublicWebUrl/)
  assert.doesNotMatch(cabinetSource, /initMyPayment\([^\n]+window\.location\.href/)
  assert.match(publicSource + cabinetSource + adminSource, /readSessionStorageItem/)
  assert.doesNotMatch(publicSource + cabinetSource + adminSource, /useState\(sessionStorage/)
  assert.match(cabinetPublicUrlSource, /'5474': '5473'/)
  assert.match(cabinetSource, /aria-current="page"/)
  assert.match(cabinetSource, /aria-pressed=\{selectedSupportConversation\?\.id === conversation\.id\}/)
  assert.match(cabinetSource, /aria-label=\{`\$\{conversation\.subject/)
  assert.match(appVersionSource, /useRef/)
  assert.match(appVersionSource, /dialogRef\.current\?\.focus\(\)/)
  assert.match(appVersionSource, /previousActiveElement\?\.isConnected.*previousActiveElement\.focus\(\)/)
  assert.match(appVersionSource, /event\.key === 'Escape'/)
  assert.match(appVersionSource, /event\.key !== 'Tab'/)
  assert.match(appVersionSource, /dialogFocusableSelector/)
  assert.match(appVersionSource, /appRoot\.inert = true/)
  assert.match(appVersionSource, /document\.body\.style\.overflow = 'hidden'/)
  assert.match(appVersionSource, /historyCloseRef\.current\?\.focus\(\)/)
  assert.match(appVersionSource, /historyToggleRef\.current\?\.focus\(\)/)
  assert.match(appVersionSource, /aria-describedby="app-version-summary"/)
  assert.match(appVersionSource, /aria-current=\{release\.releaseId === selectedRelease\.releaseId/)
  assert.match(appVersionSource, /aria-expanded=\{historyOpen\}/)
  assert.match(cabinetSource, /VPN-ключи/)
  assert.doesNotMatch(cabinetSource, /Выданные доступы/)
  assert.doesNotMatch(cabinetSource, /Перевыпуск ключа скоро/)
  assert.match(cabinetSource, /Заказ на продление.*создан, но ссылку оплаты подготовить не удалось/)
  assert.match(cabinetSource, /Повторить подготовку оплаты/)
  assert.match(cabinetSource, /api\.initMyPayment\(token, renewal\.order\.id/)
  assert.match(cabinetSource, /runSessionAction\(\s*`renewal-payment-/)
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
  assert.match(adminSource, /id="payments" className="section" role="tabpanel"/)
  assert.match(adminSource, /aria-controls=\{activeSection === id && activeSectionLoadFailed \? 'admin-section-load-error' : id\}/)
  assert.match(adminSource, /aria-labelledby=\{adminSectionTabId/)
  assert.match(adminSource, /admin-section-select/)
  assert.match(adminSource, /Предыдущий/)
  assert.match(adminSource, /Следующий/)
  assert.match(adminSource, /import \{ adminSectionIds, adminSectionLabels,/)
  assert.match(adminSource, /adminSectionIds\.map\(\(id\) => \[id, adminSectionLabels\[id\]\] as const\)/)
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
  assert.match(adminSource, /server\.status === 'Archived' \|\| isActionResourceBusy\(serverActionResourceKey\(server\.id\)\)/)
  assert.match(adminSource, /server\.status === 'Disabled' \|\| server\.status === 'Archived' \|\| isActionResourceBusy/)
  assert.match(adminSource, /provisioningRunActionResourceKey\(run\.id\), serverActionResourceKey\(run\.nodeId\)/)
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
  assert.match(publicSource, /checkoutClaimInFlightRef/)
  assert.match(publicSource, /checkoutClaimAttemptKeyRef/)
  assert.match(publicSource, /checkoutClaimRequestIdRef/)
  assert.match(publicSource, /const clearSession = \(\) => \{[\s\S]*?setLastCheckout\(null\)[\s\S]*?setCheckoutError\(''\)[\s\S]*?\n  \}/)
  assert.match(publicSource, /pendingCheckoutOrder/)
  assert.match(publicSource, /const paymentAvailability = getPendingCheckoutOrderAvailability\(order\)/)
  assert.match(publicSource, /if \(!paymentAvailability\.canRetry\)/)
  assert.match(publicSource, /paymentAvailability\.shouldForgetPendingCheckout[\s\S]*?removeSessionStorageItem\(PENDING_CHECKOUT_STORAGE_KEY\)/)
  assert.match(publicSource, /pendingCheckoutOrderAvailability\?\.title/)
  assert.match(publicSource, /expiresAt: session\.expiresAt/)
  assert.match(publicSource, /getPendingCheckoutSessionAvailability\(pendingCheckout\)/)
  assert.match(publicSource, /isCheckoutSessionExpiredError\(e\)/)
  assert.match(publicSource, /Создать новый заказ/)
  assert.doesNotMatch(publicSource, /function TariffsPage\(\{ token/)
  assert.match(publicSource, /sessionHydrationInFlightRef/)
  assert.match(publicSource, /sessionHydrationAttemptKeyRef/)
  assert.match(publicSource, /sessionHydrationRequestIdRef/)
  assert.match(publicSource, /Повторить проверку/)
  assert.match(publicSource, /!token \|\| !profile \|\| !pendingCheckout/)
  assert.match(publicSource, /onClearPendingCheckout/)
  assert.match(publicSource, /checkoutUnavailableReason|Оплата временно недоступна/)
  assert.match(publicSource, /ExternalLinkActions/)
  assert.match(publicSource, /tariffsLoading/)
  assert.match(publicSource, /ErrorBlock/)
  assert.match(publicSource, /tariffsErrorContentKey\s*\?\s*content\(tariffsErrorContentKey\)\s*:\s*''/)
  assert.match(publicSource, /paymentProvidersErrorContentKey\s*\?\s*content\(paymentProvidersErrorContentKey\)\s*:\s*''/)
  assert.match(publicSource, /setTariffsErrorContentKey\('home\.errors\.tariffsLoad'\)/)
  assert.match(publicSource, /setPaymentProvidersErrorContentKey\('home\.errors\.paymentProvidersLoad'\)/)
  assert.match(publicSource, /Повторить загрузку способов оплаты/)
  assert.match(publicSource, /Повторить загрузку тарифов/)
  assert.doesNotMatch(publicSource, /setError\(content\('home\.errors\.(?:tariffsLoad|paymentProvidersLoad)'\)\)/)

  assert.match(telegramSource, /Главное меню VPN Platform/)
  assert.match(telegramSource, /Не публикуйте свой ключ|Не пересылайте ключ/)
  assert.match(telegramSource, /Секреты и платежные данные бот не запрашивает/)
  assert.match(telegramSource, /Пароль\/ключ не будет показан повторно/)
  assert.doesNotMatch(telegramSource, /my-secret-password|BEGIN PRIVATE KEY|bot-token-must-not-leak/i)
})
