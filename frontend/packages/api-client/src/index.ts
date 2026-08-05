export type ChannelType = 'Web' | 'Telegram' | 'Discord' | 'Vk' | 'WhatsApp' | 'Email'
export type OrderType = 'NewSubscription' | 'Renewal' | 'Upgrade' | 'Compensation'
export type PaymentProvider =
  | 'YooMoney'
  | 'YooKassa'
  | 'RoboKassa'
  | 'TelegramStars'
  | 'CloudPayments'
  | 'TBankAcquiring'
  | 'Prodamus'
  | 'Stripe'
  | 'PayPal'
export type PaymentProviderMode = 'Disabled' | 'Sandbox' | 'Production'

export type PublicPaymentProviderDto = {
  provider: PaymentProvider
  publicName: string
  mode: PaymentProviderMode
  healthStatus: string
}

export type TariffDto = {
  id: string
  name: string
  slug: string
  description: string
  fullDescription?: string
  features?: string[]
  featuresJson?: string
  badge?: string
  durationDays: number
  price: number
  currency: string
  maxDevices: number
  trafficLimit?: number | null
  isTrial?: boolean
  isActive?: boolean
  sortOrder?: number
  visibleFrom?: string | null
  visibleTo?: string | null
  tariffType?: string
  category: string
  allowedRegionsCsv?: string
  allowedNodeGroupsCsv?: string
  isReferralEligible?: boolean
  provisioningScenario?: string
  afterPaymentText?: string
  createdAt?: string
  updatedAt?: string
}

export type AppReleaseItemType = 'new' | 'improved' | 'fixed' | 'important'

export type AppReleaseItemDto = {
  id?: string | null
  type: AppReleaseItemType | string
  text: string
  sortOrder: number
}

export type AppReleaseDto = {
  id: string
  releaseId: string
  version: string
  releasedAt: string
  title: string
  summary: string
  isActive: boolean
  source: string
  items: AppReleaseItemDto[]
  createdByUserId?: string | null
  createdByUserName?: string | null
  updatedByUserId?: string | null
  updatedByUserName?: string | null
  createdAt: string
  updatedAt: string
}

export type AppVersionLatestResponse = {
  currentVersion?: string | null
  latestRelease?: AppReleaseDto | null
  seenByCurrentUser: boolean
}

export type AppReleaseUpsertPayload = {
  releaseId: string
  version: string
  releasedAt: string
  title: string
  summary: string
  isActive: boolean
  source?: string | null
  items: AppReleaseItemDto[]
}

export type AppReleaseOverviewDto = {
  totalCount: number
  publishedCount: number
  upcomingCount: number
  hiddenCount: number
  agentCount: number
  manualCount: number
  seenCount: number
  latestPublishedReleaseId?: string | null
  latestPublishedVersion?: string | null
  emptyReleaseIds: string[]
}

export type AppReleaseFilters = {
  visibility?: string
  source?: string
  search?: string
}

export type AdminAuditLogDto = {
  id: string
  actorType: string
  actorId: string
  action: string
  entityType: string
  entityId: string
  beforeJson: string
  afterJson: string
  ip: string
  userAgent: string
  createdAt: string
}

export type AdminAuditLogFilters = {
  action?: string
  entityType?: string
  actorType?: string
  search?: string
  from?: string
  to?: string
  limit?: number
}

export type UserProfileDto = {
  id: string
  email?: string | null
  displayName: string
  preferredLanguage: string
  referralCode: string
  status: string
}

export type AuthResponse = {
  accessToken: string
  refreshToken: string
  email: string
  displayName: string
}

export type AuthFormMode = 'login' | 'register'

export type ForgotPasswordResponse = {
  accepted: boolean
  message: string
  validationResetToken?: string | null
}

export type CheckoutSessionDto = {
  id: string
  token: string
  tariffId: string
  userId?: string | null
  orderId?: string | null
  status: string
  expiresAt: string
  emailHint?: string | null
}

export type OrderDto = {
  id: string
  userId: string
  userDisplayName?: string | null
  userEmail?: string | null
  tariffId: string
  tariffName?: string | null
  amount: number
  currency: string
  status: string
  type?: string
  channel?: string
  paymentProvider?: string
  checkoutSessionId?: string | null
  expiresAt: string
  paidAt?: string | null
  isFirstPurchase?: boolean
  paymentAttemptsCount?: number
  lastPaymentId?: string | null
  lastPaymentStatus?: string | null
  lastPaymentProvider?: string | null
  linkedSubscriptionId?: string | null
  createdAt?: string
  updatedAt?: string
}

export type AdminOrderFilters = {
  status?: string
  search?: string
}

export type PaymentStatusResultDto = {
  orderId?: string
  paymentId: string
  status: string
  rawResponse: string
  statusReason?: string | null
}

export type PaymentInitResult = {
  paymentId: string
  redirectUrl: string
  rawResponse: string
}

export type SubscriptionDto = {
  id: string
  userId: string
  tariffId: string
  tariffName?: string | null
  status: string
  startAt: string
  endAt: string
  gracePeriodEndAt?: string | null
  autoRenewFlag?: boolean
  sourceChannel?: string
  currentServerId?: string | null
  currentAccessId?: string | null
  lastPaymentId?: string | null
  renewalCount?: number
  blockReason?: string | null
  suspendedAt?: string | null
  cancelledAt?: string | null
  lifecycleAttemptCount?: number
  lifecycleProcessingStartedAt?: string | null
  lifecycleLeaseExpiresAt?: string | null
  lifecycleNextAttemptAt?: string | null
  lifecycleLastError?: string | null
  accessUri?: string | null
  qrCodePath?: string | null
  configPath?: string | null
  nodeName?: string | null
  createdAt?: string
  updatedAt?: string
}

export type PaymentAttemptDto = {
  id: string
  orderId: string
  userId?: string | null
  userDisplayName?: string | null
  provider: string
  paymentProviderAccountId?: string | null
  providerMode?: string | null
  providerPaymentId: string
  externalEventId: string
  idempotencyKey?: string | null
  confirmationUrl?: string | null
  returnUrl?: string | null
  amount: number
  currency: string
  status: string
  signatureValidated: boolean
  isActivationProcessed?: boolean
  activationProcessedAt?: string | null
  paidAt?: string | null
  failedAt?: string | null
  refundedAt?: string | null
  refundedAmount?: number
  statusReason?: string | null
  webhookEventsCount?: number
  refundsCount?: number
  refundSupported?: boolean
  canRefund?: boolean
  refundableAmount?: number
  refundBlockers?: string[]
  rawRequest?: string
  rawResponse?: string
  webhookPayload?: string
  createdAt: string
  updatedAt: string
}

export type PaymentProviderAccountDto = {
  id: string
  provider: PaymentProvider
  mode: PaymentProviderMode
  name: string
  publicName: string
  isEnabled: boolean
  isDefault: boolean
  shopId: string
  apiBaseUrl: string
  returnUrl: string
  webhookUrl: string
  hasSecretKey: boolean
  hasWebhookSecret: boolean
  useWebhookIpAllowList: boolean
  allowedWebhookIpRangesCsv: string
  extraSettingsJson: string
  healthStatus: string
  isCheckoutConfigured?: boolean
  checkoutConfigurationIssue?: string | null
  capabilitiesJson?: string
  capabilities?: PaymentProviderCapabilityDto[]
  requiredFields?: PaymentProviderRequiredFieldDto[]
  readinessBlockers?: string[]
  isPubliclyAvailable?: boolean
  createdAt: string
  updatedAt: string
}

export type PaymentProviderCapabilityDto = {
  key: string
  label: string
  supported: boolean
  status: string
}

export type PaymentProviderRequiredFieldDto = {
  key: string
  label: string
  required: boolean
  configured: boolean
  issue?: string | null
}

export type UpsertPaymentProviderAccountPayload = {
  provider: PaymentProvider
  mode: PaymentProviderMode
  name: string
  publicName: string
  isEnabled: boolean
  isDefault: boolean
  shopId: string
  apiBaseUrl: string
  returnUrl: string
  webhookUrl: string
  secretKey?: string | null
  webhookSecret?: string | null
  useWebhookIpAllowList: boolean
  allowedWebhookIpRangesCsv: string
  extraSettingsJson: string
}

export type PaymentProviderAccountCheckResultDto = {
  accountId: string
  provider: PaymentProvider
  mode: PaymentProviderMode
  isReady: boolean
  checkScope: 'ConfigurationOnly'
  configurationStatus: 'Ready' | 'NeedsConfiguration'
  healthStatus: string
  message: string
  details: string[]
  checkedAt: string
  account: PaymentProviderAccountDto
}

export type PaymentWebhookEventDto = {
  id: string
  provider: PaymentProvider
  paymentAttemptId?: string | null
  paymentProviderAccountId?: string | null
  providerPaymentId: string
  externalEventId: string
  eventType: string
  status: string
  signatureValidated: boolean
  receivedAt: string
  processedAt?: string | null
  errorText: string
}

export type RefundDto = {
  id: string
  paymentAttemptId: string
  provider: PaymentProvider
  providerRefundId: string
  status: string
  amount: number
  currency: string
  reason: string
  createdAt: string
  refundedAt?: string | null
}


export type TelegramStatusDto = {
  isLinked: boolean
  telegramUserId?: number | null
  username?: string | null
  linkedAt?: string | null
}

export type TelegramLinkTokenDto = {
  token: string
  deepLinkUrl: string
  expiresAt: string
}

export type SupportConversationDto = {
  id: string
  userId?: string | null
  telegramUserId?: number | null
  channel: string
  status: string
  subject: string
  assignedToUserId?: string | null
  internalNote: string
  closedAt?: string | null
  createdAt: string
  updatedAt: string
}

export type SupportMessageDto = {
  id: string
  supportConversationId: string
  userId?: string | null
  telegramUserId?: number | null
  direction: string
  text: string
  attachmentsJson: string
  isInternalNote: boolean
  createdAt: string
}

export type CreateMySupportConversationPayload = {
  subject: string
  text: string
  orderId?: string | null
  subscriptionId?: string | null
}

export type AccessCredentialHistoryDto = {
  id: string
  accessCredentialId: string
  subscriptionId: string
  eventType: string
  oldValueJson: string
  newValueJson: string
  createdAt: string
}

export type AccessActionResultDto = {
  id: string
  status: string
  disabledAt?: string | null
  lastSyncedAt?: string | null
  revision: number
  usedTrafficBytes?: number | null
  message?: string | null
}

export type AccessCredentialDto = {
  id: string
  subscriptionId: string
  subscriptionStatus?: string | null
  isTerminal?: boolean
  userId?: string | null
  providerType: string
  providerAccessId: string
  serverId: string
  serverName?: string | null
  accessUri: string
  qrCodePayload?: string | null
  qrCodePath: string
  configPath: string
  status: string
  issuedAt: string
  expiryDate?: string | null
  disabledAt?: string | null
  lastSyncedAt?: string | null
  revision: number
  history?: AccessCredentialHistoryDto[]
  createdAt?: string
  updatedAt?: string
}

export type RewardLedgerDto = {
  id: string
  userId: string
  sourceUserId?: string | null
  referralProgramId?: string | null
  type: string
  status: string
  value: number
  currencyOrUnit: string
  processedAt?: string | null
  metadataJson: string
}

export type VpnNodeDto = {
  id: string
  name: string
  host: string
  ipAddress: string
  provider: string
  region: string
  country: string
  datacenter: string
  status: string
  capacity: number
  usedCapacity: number
  supportedProtocolsCsv: string
  healthStatus: string
  lastHealthCheckAt?: string | null
  lastHealthLatencyMs?: number | null
  lastHealthError?: string | null
  lastHealthMetadataJson?: string | null
  provisioningStatus?: string
  provisioningMode?: string | null
  provisioningModeTitle?: string | null
  provisioningRiskLevel?: string | null
  liveDeployAllowed?: boolean
  provisioningNextAction?: string | null
  provisioningOperatorWarning?: string | null
  precheckMode?: string | null
  precheckModeTitle?: string | null
  installedVersion: string
  backupStatus: string
  monitoringStatus: string
  loggingStatus: string
  tagsCsv: string
  priority: number
  isAvailableForNewUsers: boolean
  sshUser?: string | null
  sshPort?: number | null
  sshAuthMethod?: string | null
  sshCredentialConfigured?: boolean
  skipHostKeyChecking?: boolean | null
  panelBaseUrl?: string | null
  panelUsername?: string | null
  panelPasswordConfigured?: boolean
  panelInboundId?: number | null
  publicHostname?: string | null
  publicPort?: number | null
  nodeGroupId?: string | null
}

export type ProvisioningRunDto = {
  id: string
  nodeId: string
  status: string
  nodeName?: string | null
  targetHost?: string | null
  sshPort?: number | null
  username?: string | null
  authMethod?: string | null
  credentialsConfigured?: boolean
  source?: string | null
  owner?: string | null
  validationMode?: boolean
  mode?: string | null
  modeTitle?: string | null
  riskLevel?: string | null
  liveDeployAllowed?: boolean
  nextAction?: string | null
  operatorWarning?: string | null
  deployMode?: string | null
  deployModeTitle?: string | null
  deployRiskLevel?: string | null
  deployLiveDeployAllowed?: boolean
  deployNextAction?: string | null
  deployOperatorWarning?: string | null
  currentStep?: string | null
  requestedByUserId?: string | null
  dryRun: boolean
  attemptCount?: number
  processingStartedAt?: string | null
  leaseExpiresAt?: string | null
  lastError?: string | null
  startedAt: string
  finishedAt?: string | null
  errorSummary?: string | null
  executionLog: string
  executionLogPreview?: string | null
  precheckReport?: string | null
  precheckReportPreview?: string | null
  createdAt: string
}

export type ProvisioningCommandResponse = {
  serverId?: string | null
  runId: string
  status: string
  dryRun: boolean
  mode?: string | null
  modeTitle?: string | null
  riskLevel?: string | null
  liveDeployAllowed?: boolean
  nextAction?: string | null
  operatorWarning?: string | null
}

export type NodeHealthCheckDto = {
  id: string
  nodeId: string
  status: string
  checkedAt: string
  latencyMs: number
  metadataJson: string
  errorText: string
}


export type ProvisioningRunDetailsDto = {
  run: ProvisioningRunDto & { linkedAccessId?: string | null }
  steps: Array<{
    id: string
    provisioningRunId: string
    stepName: string
    status: string
    startedAt?: string | null
    finishedAt?: string | null
    output?: string | null
    errorText?: string | null
    createdAt?: string | null
    updatedAt?: string | null
  }>
}

export type CreateServerPayload = {
  name: string
  host: string
  ipAddress: string
  provider: string
  region: string
  country: string
  datacenter: string
  capacity: number
  supportedProtocolsCsv?: string | null
  priority: number
  tagsCsv?: string | null
  sshUser?: string | null
  sshPort: number
  sshPrivateKeyPath?: string | null
  sshAuthMethod?: string | null
  sshCredential?: string | null
  validationMode?: boolean
  ownerType?: string | null
  skipHostKeyChecking: boolean
  panelBaseUrl?: string | null
  panelUsername?: string | null
  panelPassword?: string | null
  panelInboundId?: number | null
  publicHostname?: string | null
  publicPort: number
  nodeGroupId?: string | null
}


export type VpnPanelDto = {
  id: string
  name: string
  baseUrl: string
  region: string
  status: string
  healthStatus: string
  login: string
  sslVerificationMode: string
  apiVariant: string
  capacity: number
  usedCapacity: number
  autoCreateInbound: boolean
  defaultInboundTemplateJson: string
  lastHealthCheckAt?: string | null
  lastSyncAt?: string | null
  version: string
  lastError: string
  createdAt: string
  updatedAt: string
}

export type CreateVpnPanelPayload = {
  name: string
  baseUrl: string
  login: string
  password?: string | null
  region: string
  capacity: number
  sslVerificationMode: string
  apiVariant: string
  autoCreateInbound: boolean
  defaultInboundTemplateJson: string
}

export type UpdateVpnPanelPayload = Partial<CreateVpnPanelPayload> & { status?: string | null }

export type DeleteVpnPanelResult = {
  id: string
  deleted: boolean
  archived: boolean
  linkedInbounds: number
  linkedClients: number
  linkedSyncRuns: number
  linkedHealthChecks: number
}

export type VpnInboundDto = {
  id: string
  vpnPanelId: string
  externalInboundId: string
  name: string
  protocol: string
  port: number
  listen: string
  settingsJson: string
  streamSettingsJson: string
  sniffingJson: string
  isDefault: boolean
  isActive: boolean
  capacity: number
  usedCapacity: number
}

export type CreateVpnInboundPayload = {
  name: string
  protocol: string
  port: number
  listen: string
  settingsJson: string
  streamSettingsJson: string
  sniffingJson: string
  isDefault: boolean
  capacity: number
  isActive: boolean
}

export type VpnClientDto = {
  id: string
  userId: string
  subscriptionId: string
  vpnPanelId: string
  vpnInboundId: string
  externalClientId: string
  email: string
  uuid: string
  flow: string
  limitIp: number
  totalGb?: number | null
  expiryTime: string
  enable: boolean
  configUri: string
  qrCodePayload: string
  syncStatus: string
  lastSyncedAt?: string | null
}

export type PanelSyncRunDto = {
  id: string
  vpnPanelId: string
  status: string
  startedAt: string
  finishedAt?: string | null
  summaryJson: string
  errorMessage: string
}

export type PanelSyncEventDto = {
  id: string
  panelSyncRunId: string
  eventType: string
  entityType: string
  entityId?: string | null
  externalId: string
  message: string
  payloadJson: string
}

export type PanelHealthCheckDto = {
  id: string
  vpnPanelId: string
  status: string
  latencyMs?: number | null
  version: string
  errorMessage: string
  checkedAt: string
}


export type AdminDashboardSummaryDto = {
  totalUsers: number
  telegramUsers: number
  activeSubscriptions: number
  expiringSubscriptions: number
  paidOrders: number
  pendingOrders: number
  failedPayments: number
  recentPayments: number
  recentOrders: number
  vpnAccessesCount: number
  vpnNodesCount: number
  healthyVpnNodes: number
  vpnPanelsCount: number
  healthyVpnPanels: number
  supportConversationsCount: number
  openSupportConversations: number
  provisioningErrors: number
  productionReadiness?: AdminProductionReadinessDto
  generatedAt: string
}

export type AdminSessionCapabilitiesDto = {
  adminRead: boolean
  adminWrite: boolean
  financeRead: boolean
  financeWrite: boolean
  supportRead: boolean
  supportWrite: boolean
  provisioningManage: boolean
  vpnManage: boolean
  botManage: boolean
  settingsManage: boolean
}

export type AdminSessionDto = {
  userId: string
  email: string
  displayName: string
  roles: string[]
  capabilities: AdminSessionCapabilitiesDto
}

export type AdminProductionReadinessDto = {
  isReady: boolean
  status: string
  checks: AdminProductionReadinessCheckDto[]
}

export type AdminProductionReadinessCheckDto = {
  key: string
  label: string
  status: string
  message: string
  category?: string
  severity?: string
  actionLabel?: string
  actionHref?: string
}

export type AdminTelegramBotSettingsDto = {
  enabled: boolean
  mode: string
  publicBotUsername: string
  hasBotToken: boolean
  botTokenMasked: string
  webhookUrl: string
  hasSecretToken: boolean
  adminChatId: string
  webAppUrl: string
  welcomeText: string
  instructionText: string
  supportText: string
  afterPaymentTextTemplate: string
  renewalTextTemplate: string
  paymentFailedTextTemplate: string
  subscriptionExpiredTextTemplate: string
  generatedAt: string
}

export type AdminTelegramBotConnectionCheckDto = {
  isReady: boolean
  status: string
  requiredActions: string[]
  warnings: string[]
  checkedAt: string
}

export type UpdateTelegramBotSettingsPayload = {
  enabled?: boolean | null
  mode?: string | null
  publicBotUsername?: string | null
  botToken?: string | null
  webhookUrl?: string | null
  secretToken?: string | null
  adminChatId?: string | null
  webAppUrl?: string | null
  welcomeText?: string | null
  instructionText?: string | null
  supportText?: string | null
  afterPaymentTextTemplate?: string | null
  renewalTextTemplate?: string | null
  paymentFailedTextTemplate?: string | null
  subscriptionExpiredTextTemplate?: string | null
}

export type AdminUserDto = {
  id: string
  email?: string | null
  displayName: string
  rolesCsv: string
  status: string
  isBlocked: boolean
  preferredLanguage: string
  referralCode: string
  authSource: string
  emailConfirmed: boolean
  lastLoginAt?: string | null
  telegramRegistrationCompletedAt?: string | null
  createdAt: string
  updatedAt: string
}

export type AdminTelegramAccountDto = {
  id: string
  telegramUserId: number
  username: string
  firstName: string
  lastName: string
  languageCode: string
  isBlocked: boolean
  linkedAt?: string | null
  lastSeenAt?: string | null
  registrationCompletedAt?: string | null
}

export type AdminUserOverviewDto = {
  user: AdminUserDto
  telegramAccounts: AdminTelegramAccountDto[]
  orders: OrderDto[]
  payments: PaymentAttemptDto[]
  subscriptions: SubscriptionDto[]
  accessCredentials: AccessCredentialDto[]
  supportConversations: SupportConversationDto[]
}

export type UpdateTariffPayload = Partial<Pick<TariffDto, 'name' | 'slug' | 'description' | 'fullDescription' | 'featuresJson' | 'badge' | 'price' | 'currency' | 'durationDays' | 'maxDevices' | 'trafficLimit' | 'isTrial' | 'isActive' | 'sortOrder' | 'category' | 'allowedRegionsCsv' | 'allowedNodeGroupsCsv' | 'isReferralEligible' | 'provisioningScenario' | 'afterPaymentText'>>

export type FaqItem = {
  id?: string
  question: string
  answer: string
  category?: string
  isActive?: boolean
  showOnHome?: boolean
  showOnFaqPage?: boolean
  sortOrder?: number
  createdAt?: string
  updatedAt?: string
}

export type FaqUpsertPayload = {
  question: string
  answer: string
  category?: string | null
  isActive: boolean
  showOnHome: boolean
  showOnFaqPage: boolean
  sortOrder: number
}

export type FaqOverviewDto = {
  totalCount: number
  activeCount: number
  hiddenCount: number
  homeCount: number
  faqPageCount: number
  publicCount: number
  categoryCount: number
  categories: string[]
  duplicateQuestions: string[]
  hasPublicFaq: boolean
  hasHomeFaq: boolean
}

export type AdminFaqFilters = {
  category?: string
  visibility?: string
  search?: string
}

export type SiteContentBlockDto = {
  id: string
  key: string
  value: string
  group: string
  label: string
  description: string
  inputType: string
  isActive: boolean
  sortOrder: number
  createdAt: string
  updatedAt: string
}

export type SiteContentBlockUpsertPayload = {
  key: string
  value: string
  group?: string | null
  label?: string | null
  description?: string | null
  inputType?: string | null
  isActive: boolean
  sortOrder: number
}

export type SiteContentReadinessDto = {
  isReady: boolean
  requiredCount: number
  presentCount: number
  activeRequiredCount: number
  missingKeys: string[]
  inactiveKeys: string[]
  emptyKeys: string[]
  duplicateKeys: string[]
  publicBlocksCount: number
  requiredKeys: string[]
}

export type SiteContentDefaultsResultDto = {
  created: number
  restored: number
  readiness: SiteContentReadinessDto
}

export type WorkScenarioDto = {
  id: string
  name: string
  key: string
  isActive: boolean
  allowedTariffIdsJson: string
  vpnProtocol: string
  serverSelectionRule: string
  inboundSelectionRule: string
  provisioningMode: string
  onPaymentSucceeded: string
  onPaymentFailed: string
  onRefund: string
  onSubscriptionExpired: string
  onRenewal: string
  cabinetText: string
  telegramText: string
  generateQrCode: boolean
  maxDevices: number
  trafficLimit?: number | null
  sortOrder: number
  createdAt: string
  updatedAt: string
}

export type WorkScenarioUpsertPayload = Omit<WorkScenarioDto, 'id' | 'createdAt' | 'updatedAt'>

export type CreateCheckoutSessionPayload = {
  tariffId: string
  type: OrderType
  channel: ChannelType
  paymentProvider: PaymentProvider
  promoCode?: string | null
  isFirstPurchase: boolean
  emailHint?: string | null
  returnUrl?: string | null
}

export type CreatePublicOrderPayload = {
  userId: string
  tariffId: string
  type: OrderType
  channel: ChannelType
  paymentProvider: PaymentProvider
  promoCode?: string | null
  isFirstPurchase: boolean
}

export type CreateMyOrderPayload = {
  tariffId: string
  type: OrderType
  channel: ChannelType
  paymentProvider: PaymentProvider
  promoCode?: string | null
  isFirstPurchase: boolean
  subscriptionId?: string | null
}

export function buildAuthHeaders(token?: string | null): HeadersInit {
  return token ? { Authorization: `Bearer ${token}` } : {}
}

const authErrorMessages: Record<string, string> = {
  invalid_credentials: 'Неверный email или пароль.',
  invalid_registration_request: 'Проверьте email и пароль: пароль должен быть не короче 8 символов.',
  email_exists: 'Аккаунт с таким email уже зарегистрирован. Войдите или восстановите пароль.',
  invalid_refresh_token: 'Сессия не найдена. Войдите заново.',
  refresh_token_reuse_detected: 'Сессия была отозвана из-за повторного использования старого токена. Войдите заново.',
  refresh_token_expired: 'Сессия истекла. Войдите заново.',
  user_not_active: 'Аккаунт недоступен. Обратитесь в поддержку.',
  invalid_reset_request: 'Проверьте код сброса и новый пароль: пароль должен быть не короче 8 символов.',
  invalid_or_expired_reset_token: 'Код сброса неверный или уже истек. Запросите новый код.'
}

const authErrorFallbacks: Record<string, string> = {
  'Password reset request failed': 'Не удалось запросить код сброса пароля.',
  'Password reset failed': 'Не удалось изменить пароль.',
  'Request failed': 'Запрос не выполнен. Попробуйте еще раз.'
}

export function isValidEmail(value: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim())
}

export function validateAuthInput(mode: AuthFormMode, email: string, password: string, displayName = '') {
  const errors: string[] = []
  if (!isValidEmail(email)) errors.push('Введите корректный email.')
  if (password.trim().length < 8) errors.push('Пароль должен быть не короче 8 символов.')
  if (mode === 'register' && displayName.trim().length > 80) errors.push('Имя должно быть короче 80 символов.')
  return errors
}

export function validatePasswordResetRequest(email: string) {
  return isValidEmail(email) ? [] : ['Введите корректный email для восстановления пароля.']
}

export function validatePasswordResetConfirm(resetToken: string, newPassword: string) {
  const errors: string[] = []
  if (!resetToken.trim()) errors.push('Введите код сброса пароля.')
  if (newPassword.trim().length < 8) errors.push('Новый пароль должен быть не короче 8 символов.')
  return errors
}

export function translateAuthError(error: unknown, fallback = 'Ошибка авторизации') {
  const raw = error instanceof Error ? error.message : String(error ?? '')
  return authErrorMessages[raw] ?? authErrorFallbacks[raw] ?? (raw || fallback)
}

export function translateAuthMessage(message: string) {
  if (message === 'If the account exists, a password reset instruction has been queued for the configured delivery channel.') {
    return 'Если аккаунт существует, инструкция по сбросу пароля поставлена в очередь отправки.'
  }

  return message
}

async function readJsonOrText(response: Response) {
  const text = await response.text()
  if (!text) return null

  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

export function normalizeApiError(payload: unknown, fallback: string): string {
  if (!payload) return fallback
  if (typeof payload === 'string') return payload
  if (typeof payload === 'object' && payload !== null) {
    if ('error' in payload && typeof (payload as Record<string, unknown>).error === 'string') {
      return String((payload as Record<string, unknown>).error)
    }
    if ('message' in payload && typeof (payload as Record<string, unknown>).message === 'string') {
      return String((payload as Record<string, unknown>).message)
    }
  }
  return fallback
}

const apiFallbackErrorMessage = 'Не удалось выполнить запрос. Попробуйте еще раз.'

export class ApiClientError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly payload: unknown
  ) {
    super(message)
    this.name = 'ApiClientError'
  }
}

export class ApiClient {
  constructor(private readonly baseUrl: string) {}

  private async request<T>(path: string, init?: RequestInit & { token?: string | null; errorMessage?: string }): Promise<T> {
    const { token, errorMessage, ...requestInit } = init ?? {}
    const headers = new Headers(requestInit.headers ?? {})

    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }

    if (requestInit.body && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }

    const response = await fetch(`${this.baseUrl}${path}`, {
      ...requestInit,
      headers
    })

    const payload = await readJsonOrText(response)
    if (!response.ok) {
      throw new ApiClientError(normalizeApiError(payload, errorMessage ?? apiFallbackErrorMessage), response.status, payload)
    }

    return payload as T
  }


  private async requestText(path: string, init?: RequestInit & { token?: string | null; errorMessage?: string }): Promise<string> {
    const { token, errorMessage, ...requestInit } = init ?? {}
    const headers = new Headers(requestInit.headers ?? {})

    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }

    const response = await fetch(`${this.baseUrl}${path}`, {
      ...requestInit,
      headers
    })

    const text = await response.text()
    if (!response.ok) {
      const payload = text ? (() => { try { return JSON.parse(text) } catch { return text } })() : null
      throw new ApiClientError(normalizeApiError(payload, errorMessage ?? apiFallbackErrorMessage), response.status, payload)
    }

    return text
  }

  getTariffs(): Promise<TariffDto[]> {
    return this.request<TariffDto[]>('/api/public/tariffs', { errorMessage: apiFallbackErrorMessage })
  }

  getFaq(): Promise<FaqItem[]> {
    return this.request<FaqItem[]>('/api/public/content/faq', { errorMessage: apiFallbackErrorMessage })
  }

  getHomeFaq(): Promise<FaqItem[]> {
    return this.request<FaqItem[]>('/api/public/content/faq?home=true', { errorMessage: apiFallbackErrorMessage })
  }

  getHomeContent(): Promise<SiteContentBlockDto[]> {
    return this.request<SiteContentBlockDto[]>('/api/public/content/home', { errorMessage: apiFallbackErrorMessage })
  }

  getPublicPaymentProviders(): Promise<PublicPaymentProviderDto[]> {
    return this.request<PublicPaymentProviderDto[]>('/api/public/payments/providers', { errorMessage: apiFallbackErrorMessage })
  }

  register(email: string, password: string, displayName: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, displayName }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  login(email: string, password: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  refresh(refreshToken: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  logout(token?: string | null, refreshToken?: string | null): Promise<{ status: string }> {
    return this.request<{ status: string }>('/api/auth/logout', {
      method: 'POST',
      token,
      body: JSON.stringify({ refreshToken: refreshToken ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  forgotPassword(email: string): Promise<ForgotPasswordResponse> {
    return this.request<ForgotPasswordResponse>('/api/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify({ email }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  resetPassword(resetToken: string, newPassword: string): Promise<{ status: string }> {
    return this.request<{ status: string }>('/api/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify({ token: resetToken, newPassword }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getMe(token: string): Promise<UserProfileDto> {
    return this.request<UserProfileDto>('/api/me', { token, errorMessage: apiFallbackErrorMessage })
  }

  createCheckoutSession(payload: CreateCheckoutSessionPayload): Promise<CheckoutSessionDto> {
    return this.request<CheckoutSessionDto>('/api/public/checkout-sessions', {
      method: 'POST',
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getCheckoutSession(token: string): Promise<CheckoutSessionDto> {
    return this.request<CheckoutSessionDto>(`/api/public/checkout-sessions/${encodeURIComponent(token)}`, {
      errorMessage: apiFallbackErrorMessage
    })
  }

  claimCheckoutSession(token: string, checkoutToken: string): Promise<OrderDto> {
    return this.request<OrderDto>(`/api/me/checkout-sessions/${encodeURIComponent(checkoutToken)}/claim`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  createPublicOrder(payload: CreatePublicOrderPayload): Promise<OrderDto> {
    return this.request<OrderDto>('/api/public/orders', {
      method: 'POST',
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  initPayment(orderId: string, provider: PaymentProvider): Promise<PaymentInitResult> {
    return this.request<PaymentInitResult>(`/api/public/payments/${provider}/init`, {
      method: 'POST',
      body: JSON.stringify({ orderId }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getOrderStatus(orderId: string): Promise<OrderDto> {
    return this.request<OrderDto>(`/api/public/orders/${orderId}/status`, { errorMessage: apiFallbackErrorMessage })
  }

  createMyOrder(token: string, payload: CreateMyOrderPayload): Promise<OrderDto> {
    return this.request<OrderDto>('/api/me/orders', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  initMyPayment(token: string, orderId: string, provider: PaymentProvider, returnUrl?: string | null): Promise<PaymentInitResult> {
    return this.request<PaymentInitResult>(`/api/me/orders/${orderId}/payments/${provider}/init`, {
      method: 'POST',
      token,
      body: JSON.stringify({ returnUrl: returnUrl ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getMySubscriptions(token: string): Promise<SubscriptionDto[]> {
    return this.request<SubscriptionDto[]>('/api/me/subscriptions', { token, errorMessage: apiFallbackErrorMessage })
  }

  getMyOrders(token: string): Promise<OrderDto[]> {
    return this.request<OrderDto[]>('/api/me/orders', { token, errorMessage: apiFallbackErrorMessage })
  }

  getMyPayments(token: string): Promise<PaymentAttemptDto[]> {
    return this.request<PaymentAttemptDto[]>('/api/me/payments', { token, errorMessage: apiFallbackErrorMessage })
  }

  getMyPayment(token: string, paymentId: string): Promise<PaymentAttemptDto> {
    return this.request<PaymentAttemptDto>(`/api/me/payments/${paymentId}`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getMySupportConversations(token: string): Promise<SupportConversationDto[]> {
    return this.request<SupportConversationDto[]>('/api/me/support/conversations', { token, errorMessage: apiFallbackErrorMessage })
  }

  getMySupportMessages(token: string, conversationId: string): Promise<SupportMessageDto[]> {
    return this.request<SupportMessageDto[]>(`/api/me/support/conversations/${conversationId}/messages`, { token, errorMessage: apiFallbackErrorMessage })
  }

  createMySupportConversation(token: string, payload: CreateMySupportConversationPayload): Promise<SupportConversationDto> {
    return this.request<SupportConversationDto>('/api/me/support/conversations', {
      method: 'POST',
      token,
      body: JSON.stringify({
        subject: payload.subject,
        text: payload.text,
        orderId: payload.orderId ?? null,
        subscriptionId: payload.subscriptionId ?? null
      }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  replyMySupportConversation(token: string, conversationId: string, text: string): Promise<SupportMessageDto> {
    return this.request<SupportMessageDto>(`/api/me/support/conversations/${conversationId}/reply`, {
      method: 'POST',
      token,
      body: JSON.stringify({ text }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateMySupportConversationStatus(token: string, conversationId: string, status: 'open' | 'closed'): Promise<{ conversationId: string; status: string }> {
    return this.request<{ conversationId: string; status: string }>(`/api/me/support/conversations/${conversationId}/status`, {
      method: 'PATCH',
      token,
      body: JSON.stringify({ status }),
      errorMessage: apiFallbackErrorMessage
    })
  }


  createTelegramLinkToken(token: string): Promise<TelegramLinkTokenDto> {
    return this.request<TelegramLinkTokenDto>('/api/me/telegram/link-token', {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getTelegramStatus(token: string): Promise<TelegramStatusDto> {
    return this.request<TelegramStatusDto>('/api/me/telegram/status', { token, errorMessage: apiFallbackErrorMessage })
  }

  unlinkTelegram(token: string): Promise<TelegramStatusDto> {
    return this.request<TelegramStatusDto>('/api/me/telegram/unlink', {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  getMyAccesses(token: string): Promise<AccessCredentialDto[]> {
    return this.request<AccessCredentialDto[]>('/api/me/accesses', { token, errorMessage: apiFallbackErrorMessage })
  }

  getMyAccessQrSvg(token: string, id: string): Promise<string> {
    return this.requestText(`/api/cabinet/access/${id}/qr`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getMyReferrals(token: string): Promise<RewardLedgerDto[]> {
    return this.request<RewardLedgerDto[]>('/api/me/referrals', { token, errorMessage: apiFallbackErrorMessage })
  }

  getLatestAppVersion(token: string): Promise<AppVersionLatestResponse> {
    return this.request<AppVersionLatestResponse>('/api/app-version/latest', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAppVersionHistory(token: string): Promise<AppReleaseDto[]> {
    return this.request<AppReleaseDto[]>('/api/app-version/history', { token, errorMessage: apiFallbackErrorMessage })
  }

  markAppVersionSeen(token: string, releaseId: string): Promise<{ releaseId: string; seen: boolean }> {
    return this.request<{ releaseId: string; seen: boolean }>('/api/app-version/mark-seen', {
      method: 'POST',
      token,
      body: JSON.stringify({ releaseId }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminDashboardSummary(token: string): Promise<AdminDashboardSummaryDto> {
    return this.request<AdminDashboardSummaryDto>('/api/admin/dashboard/summary', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminSession(token: string): Promise<AdminSessionDto> {
    return this.request<AdminSessionDto>('/api/admin/session', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminAuditLogs(token: string, filters: AdminAuditLogFilters = {}): Promise<AdminAuditLogDto[]> {
    const params = new URLSearchParams()
    if (filters.action) params.set('action', filters.action)
    if (filters.entityType) params.set('entityType', filters.entityType)
    if (filters.actorType) params.set('actorType', filters.actorType)
    if (filters.search) params.set('search', filters.search)
    if (filters.from) params.set('from', filters.from)
    if (filters.to) params.set('to', filters.to)
    if (filters.limit) params.set('limit', String(filters.limit))
    const query = params.toString()
    return this.request<AdminAuditLogDto[]>(`/api/admin/audit-logs${query ? `?${query}` : ''}`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminUsers(token: string, filters?: { search?: string; status?: string; role?: string }): Promise<AdminUserDto[]> {
    const params = new URLSearchParams()
    if (filters?.search) params.set('search', filters.search)
    if (filters?.status) params.set('status', filters.status)
    if (filters?.role) params.set('role', filters.role)
    const suffix = params.toString() ? `?${params.toString()}` : ''
    return this.request<AdminUserDto[]>(`/api/admin/users${suffix}`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminUserOverview(token: string, userId: string): Promise<AdminUserOverviewDto> {
    return this.request<AdminUserOverviewDto>(`/api/admin/users/${userId}/overview`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminSubscriptions(token: string): Promise<SubscriptionDto[]> {
    return this.request<SubscriptionDto[]>('/api/admin/subscriptions', { token, errorMessage: apiFallbackErrorMessage })
  }

  extendAdminSubscription(token: string, id: string, days: number, reason?: string | null): Promise<{ id: string; status: string; endAt: string }> {
    return this.request<{ id: string; status: string; endAt: string }>(`/api/admin/subscriptions/${id}/extend`, {
      method: 'POST',
      token,
      body: JSON.stringify({ days, reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  activateAdminSubscription(token: string, id: string, reason?: string | null): Promise<{ id: string; status: string; endAt: string; currentAccessId?: string | null; access?: AccessActionResultDto | null }> {
    return this.request<{ id: string; status: string; endAt: string; currentAccessId?: string | null; access?: AccessActionResultDto | null }>(`/api/admin/subscriptions/${id}/activate`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  blockAdminSubscription(token: string, id: string, reason?: string | null): Promise<{ id: string; status: string }> {
    return this.request<{ id: string; status: string }>(`/api/admin/subscriptions/${id}/block`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: apiFallbackErrorMessage })
  }

  unblockAdminSubscription(token: string, id: string, reason?: string | null): Promise<{ id: string; status: string }> {
    return this.request<{ id: string; status: string }>(`/api/admin/subscriptions/${id}/unblock`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: apiFallbackErrorMessage })
  }

  cancelAdminSubscription(token: string, id: string, reason?: string | null): Promise<{ id: string; status: string }> {
    return this.request<{ id: string; status: string }>(`/api/admin/subscriptions/${id}/cancel`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: apiFallbackErrorMessage })
  }

  syncAdminSubscriptionAccess(token: string, id: string, reason?: string | null): Promise<{ id: string; currentAccessId?: string | null; access: AccessActionResultDto }> {
    return this.request<{ id: string; currentAccessId?: string | null; access: AccessActionResultDto }>(`/api/admin/subscriptions/${id}/sync-access`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminAccesses(token: string): Promise<AccessCredentialDto[]> {
    return this.request<AccessCredentialDto[]>('/api/admin/access-credentials', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminAccessQrSvg(token: string, id: string): Promise<string> {
    return this.requestText(`/api/admin/access-credentials/${id}/qr`, { token, errorMessage: apiFallbackErrorMessage })
  }

  enableAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/enable`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  disableAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/disable`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }


  syncAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/sync`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  resetAdminAccessTraffic(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/reset-traffic`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminOrders(token: string, filters: AdminOrderFilters = {}): Promise<OrderDto[]> {
    const params = new URLSearchParams()
    if (filters.status) params.set('status', filters.status)
    if (filters.search) params.set('search', filters.search)
    const query = params.toString()
    return this.request<OrderDto[]>(`/api/admin/orders${query ? `?${query}` : ''}`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminPayments(token: string): Promise<PaymentAttemptDto[]> {
    return this.request<PaymentAttemptDto[]>('/api/admin/payments', { token, errorMessage: apiFallbackErrorMessage })
  }

  recheckAdminPayment(token: string, paymentId: string): Promise<PaymentStatusResultDto> {
    return this.request<PaymentStatusResultDto>(`/api/admin/payments/${paymentId}/recheck`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  recheckAdminOrderPayment(token: string, orderId: string): Promise<PaymentStatusResultDto> {
    return this.request<PaymentStatusResultDto>(`/api/admin/orders/${orderId}/recheck-payment`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  refundAdminPayment(token: string, paymentId: string, amount: number, reason?: string): Promise<RefundDto> {
    return this.request<RefundDto>(`/api/admin/payments/${paymentId}/refund`, {
      method: 'POST',
      token,
      body: JSON.stringify({ amount, reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminPaymentProviderAccounts(token: string): Promise<PaymentProviderAccountDto[]> {
    return this.request<PaymentProviderAccountDto[]>('/api/admin/payment-providers/accounts', { token, errorMessage: apiFallbackErrorMessage })
  }

  createAdminPaymentProviderAccount(token: string, payload: UpsertPaymentProviderAccountPayload): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>('/api/admin/payment-providers/accounts', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminPaymentProviderAccount(token: string, id: string, payload: UpsertPaymentProviderAccountPayload): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>(`/api/admin/payment-providers/accounts/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  setAdminPaymentProviderAccountEnabled(token: string, id: string, enabled: boolean): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>(`/api/admin/payment-providers/accounts/${id}/enabled`, {
      method: 'POST',
      token,
      body: JSON.stringify({ enabled }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  checkAdminPaymentProviderAccount(token: string, id: string): Promise<PaymentProviderAccountCheckResultDto> {
    return this.request<PaymentProviderAccountCheckResultDto>(`/api/admin/payment-providers/accounts/${id}/check`, {
      method: 'POST',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminPaymentWebhookEvents(token: string): Promise<PaymentWebhookEventDto[]> {
    return this.request<PaymentWebhookEventDto[]>('/api/admin/payment-webhook-events', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminRefunds(token: string): Promise<RefundDto[]> {
    return this.request<RefundDto[]>('/api/admin/refunds', { token, errorMessage: apiFallbackErrorMessage })
  }


  getAdminSupportConversations(token: string): Promise<SupportConversationDto[]> {
    return this.request<SupportConversationDto[]>('/api/admin/support/conversations', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminSupportMessages(token: string, conversationId: string): Promise<SupportMessageDto[]> {
    return this.request<SupportMessageDto[]>(`/api/admin/support/conversations/${conversationId}/messages`, { token, errorMessage: apiFallbackErrorMessage })
  }

  replyAdminSupportConversation(token: string, conversationId: string, text: string): Promise<{ conversationId: string; status: string }> {
    return this.request<{ conversationId: string; status: string }>(`/api/admin/support/conversations/${conversationId}/reply`, {
      method: 'POST',
      token,
      body: JSON.stringify({ text }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminSupportConversationStatus(token: string, conversationId: string, status: string, assignedToUserId?: string | null): Promise<{ conversationId: string; status: string }> {
    return this.request<{ conversationId: string; status: string }>(`/api/admin/support/conversations/${conversationId}/status`, {
      method: 'PATCH',
      token,
      body: JSON.stringify({ status, assignedToUserId: assignedToUserId ?? null }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  addAdminSupportInternalNote(token: string, conversationId: string, text: string): Promise<SupportMessageDto> {
    return this.request<SupportMessageDto>(`/api/admin/support/conversations/${conversationId}/notes`, {
      method: 'POST',
      token,
      body: JSON.stringify({ text }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminVpnPanels(token: string): Promise<VpnPanelDto[]> {
    return this.request<VpnPanelDto[]>('/api/admin/vpn-panels', { token, errorMessage: apiFallbackErrorMessage })
  }

  createAdminVpnPanel(token: string, payload: CreateVpnPanelPayload): Promise<VpnPanelDto> {
    return this.request<VpnPanelDto>('/api/admin/vpn-panels', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminVpnPanel(token: string, id: string, payload: UpdateVpnPanelPayload): Promise<VpnPanelDto> {
    return this.request<VpnPanelDto>(`/api/admin/vpn-panels/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  deleteAdminVpnPanel(token: string, id: string): Promise<DeleteVpnPanelResult> {
    return this.request<DeleteVpnPanelResult>(`/api/admin/vpn-panels/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  testAdminVpnPanel(token: string, id: string): Promise<PanelHealthCheckDto> {
    return this.request<PanelHealthCheckDto>(`/api/admin/vpn-panels/${id}/test-connection`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  syncAdminVpnPanel(token: string, id: string): Promise<PanelSyncRunDto> {
    return this.request<PanelSyncRunDto>(`/api/admin/vpn-panels/${id}/sync`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminVpnPanelInbounds(token: string, id: string): Promise<VpnInboundDto[]> {
    return this.request<VpnInboundDto[]>(`/api/admin/vpn-panels/${id}/inbounds`, { token, errorMessage: apiFallbackErrorMessage })
  }

  createAdminVpnPanelInbound(token: string, id: string, payload: CreateVpnInboundPayload): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-panels/${id}/inbounds`, {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  setAdminVpnInboundDefault(token: string, id: string): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-inbounds/${id}/set-default`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminVpnInbound(token: string, id: string, payload: CreateVpnInboundPayload): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-inbounds/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminVpnPanelClients(token: string, id: string): Promise<VpnClientDto[]> {
    return this.request<VpnClientDto[]>(`/api/admin/vpn-panels/${id}/clients`, { token, errorMessage: apiFallbackErrorMessage })
  }

  enableAdminVpnClient(token: string, id: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/enable`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  disableAdminVpnClient(token: string, id: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/disable`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  syncAdminVpnClient(token: string, id: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/sync`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  resetAdminVpnClientTraffic(token: string, id: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/reset-traffic`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  migrateAdminVpnClient(token: string, id: string, targetInboundId: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/migrate`, {
      method: 'POST',
      token,
      body: JSON.stringify({ targetInboundId }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminVpnPanelSyncRuns(token: string, id: string): Promise<PanelSyncRunDto[]> {
    return this.request<PanelSyncRunDto[]>(`/api/admin/vpn-panels/${id}/sync-runs`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminVpnPanelSyncEvents(token: string, runId: string): Promise<PanelSyncEventDto[]> {
    return this.request<PanelSyncEventDto[]>(`/api/admin/vpn-panel-sync-runs/${runId}/events`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminVpnPanelHealthChecks(token: string, id: string): Promise<PanelHealthCheckDto[]> {
    return this.request<PanelHealthCheckDto[]>(`/api/admin/vpn-panels/${id}/health-checks`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminTariffs(token: string): Promise<TariffDto[]> {
    return this.request<TariffDto[]>('/api/admin/tariffs', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminAppReleases(token: string, filters: AppReleaseFilters = {}): Promise<AppReleaseDto[]> {
    const params = new URLSearchParams()
    if (filters.visibility && filters.visibility !== 'all') params.set('visibility', filters.visibility)
    if (filters.source && filters.source !== 'all') params.set('source', filters.source)
    if (filters.search) params.set('search', filters.search)
    const query = params.toString()
    return this.request<AppReleaseDto[]>(`/api/app-version/admin/releases${query ? `?${query}` : ''}`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminAppReleaseOverview(token: string): Promise<AppReleaseOverviewDto> {
    return this.request<AppReleaseOverviewDto>('/api/app-version/admin/releases/overview', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminFaq(token: string, filters: AdminFaqFilters = {}): Promise<FaqItem[]> {
    const params = new URLSearchParams()
    if (filters.category && filters.category !== 'all') params.set('category', filters.category)
    if (filters.visibility && filters.visibility !== 'all') params.set('visibility', filters.visibility)
    if (filters.search) params.set('search', filters.search)
    const query = params.toString()
    return this.request<FaqItem[]>(`/api/admin/faq${query ? `?${query}` : ''}`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminFaqOverview(token: string): Promise<FaqOverviewDto> {
    return this.request<FaqOverviewDto>('/api/admin/faq/overview', { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminSiteContent(token: string, group = 'home'): Promise<SiteContentBlockDto[]> {
    const suffix = group ? `?group=${encodeURIComponent(group)}` : ''
    return this.request<SiteContentBlockDto[]>(`/api/admin/site-content${suffix}`, { token, errorMessage: apiFallbackErrorMessage })
  }

  getAdminHomeContentReadiness(token: string): Promise<SiteContentReadinessDto> {
    return this.request<SiteContentReadinessDto>('/api/admin/site-content/home-readiness', { token, errorMessage: apiFallbackErrorMessage })
  }

  restoreAdminHomeContentDefaults(token: string): Promise<SiteContentDefaultsResultDto> {
    return this.request<SiteContentDefaultsResultDto>('/api/admin/site-content/home-defaults', {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  createAdminSiteContent(token: string, payload: SiteContentBlockUpsertPayload): Promise<SiteContentBlockDto> {
    return this.request<SiteContentBlockDto>('/api/admin/site-content', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminSiteContent(token: string, id: string, payload: SiteContentBlockUpsertPayload): Promise<SiteContentBlockDto> {
    return this.request<SiteContentBlockDto>(`/api/admin/site-content/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  deleteAdminSiteContent(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/site-content/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminWorkScenarios(token: string): Promise<WorkScenarioDto[]> {
    return this.request<WorkScenarioDto[]>('/api/admin/work-scenarios', { token, errorMessage: apiFallbackErrorMessage })
  }

  createAdminWorkScenario(token: string, payload: WorkScenarioUpsertPayload): Promise<WorkScenarioDto> {
    return this.request<WorkScenarioDto>('/api/admin/work-scenarios', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminWorkScenario(token: string, id: string, payload: WorkScenarioUpsertPayload): Promise<WorkScenarioDto> {
    return this.request<WorkScenarioDto>(`/api/admin/work-scenarios/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  deleteAdminWorkScenario(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/work-scenarios/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  createAdminFaq(token: string, payload: FaqUpsertPayload): Promise<FaqItem> {
    return this.request<FaqItem>('/api/admin/faq', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminFaq(token: string, id: string, payload: FaqUpsertPayload): Promise<FaqItem> {
    return this.request<FaqItem>(`/api/admin/faq/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  deleteAdminFaq(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/faq/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  createAdminAppRelease(token: string, payload: AppReleaseUpsertPayload): Promise<AppReleaseDto> {
    return this.request<AppReleaseDto>('/api/app-version/admin/releases', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminAppRelease(token: string, id: string, payload: AppReleaseUpsertPayload): Promise<AppReleaseDto> {
    return this.request<AppReleaseDto>(`/api/app-version/admin/releases/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  deleteAdminAppRelease(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/app-version/admin/releases/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  createAdminTariff(token: string, payload: UpdateTariffPayload): Promise<TariffDto> {
    return this.request<TariffDto>('/api/admin/tariffs', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminTariff(token: string, id: string, payload: UpdateTariffPayload): Promise<TariffDto> {
    return this.request<TariffDto>(`/api/admin/tariffs/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  deleteAdminTariff(token: string, id: string): Promise<{ id: string; deleted: boolean; archived?: boolean }> {
    return this.request<{ id: string; deleted: boolean; archived?: boolean }>(`/api/admin/tariffs/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminServers(token: string): Promise<VpnNodeDto[]> {
    return this.request<VpnNodeDto[]>('/api/admin/servers', { token, errorMessage: apiFallbackErrorMessage })
  }

  createAdminServer(token: string, payload: CreateServerPayload): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>('/api/admin/servers', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminServer(token: string, serverId: string, payload: CreateServerPayload): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }

  deleteAdminServer(token: string, serverId: string): Promise<{ id: string; deleted: boolean; archived: boolean; linkedSubscriptions: number; linkedAccesses: number; linkedProvisioningRuns: number; linkedHealthChecks: number; linkedMigrationJobs: number }> {
    return this.request<{ id: string; deleted: boolean; archived: boolean; linkedSubscriptions: number; linkedAccesses: number; linkedProvisioningRuns: number; linkedHealthChecks: number; linkedMigrationJobs: number }>(`/api/admin/servers/${serverId}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    })
  }

  disableAdminServer(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/disable`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage })
  }

  checkAdminServerHealth(token: string, serverId: string): Promise<NodeHealthCheckDto> {
    return this.request<NodeHealthCheckDto>(`/api/admin/servers/${serverId}/health-check`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminServerHealthChecks(token: string, serverId: string): Promise<NodeHealthCheckDto[]> {
    return this.request<NodeHealthCheckDto[]>(`/api/admin/servers/${serverId}/health-checks`, { token, errorMessage: apiFallbackErrorMessage })
  }

  enableAdminServerAllocation(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/enable-allocation`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage })
  }

  disableAdminServerAllocation(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/disable-allocation`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage })
  }

  enableAdminServerMaintenance(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/maintenance`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage })
  }

  disableAdminServerMaintenance(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/disable-maintenance`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage })
  }

  precheckAdminServer(token: string, serverId: string): Promise<ProvisioningCommandResponse> {
    return this.request<ProvisioningCommandResponse>(`/api/admin/servers/${serverId}/precheck`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  queueAdminProvision(token: string, serverId: string, dryRun = false): Promise<ProvisioningCommandResponse> {
    return this.request<ProvisioningCommandResponse>(`/api/admin/servers/${serverId}/provision`, {
      method: 'POST',
      token,
      body: JSON.stringify({ dryRun }),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminProvisioningRuns(token: string): Promise<ProvisioningRunDto[]> {
    return this.request<ProvisioningRunDto[]>('/api/admin/provisioning-runs', { token, errorMessage: apiFallbackErrorMessage })
  }


  getAdminProvisioningRun(token: string, runId: string): Promise<ProvisioningRunDetailsDto> {
    return this.request<ProvisioningRunDetailsDto>(`/api/admin/provisioning-runs/${runId}`, { token, errorMessage: apiFallbackErrorMessage })
  }

  retryAdminProvisioningRun(token: string, runId: string): Promise<ProvisioningCommandResponse> {
    return this.request<ProvisioningCommandResponse>(`/api/admin/provisioning-runs/${runId}/retry`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  deployAdminProvisioningRun(token: string, runId: string): Promise<ProvisioningCommandResponse> {
    return this.request<ProvisioningCommandResponse>(`/api/admin/provisioning-runs/${runId}/deploy`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  cancelAdminProvisioningRun(token: string, runId: string): Promise<{ runId: string; status: string }> {
    return this.request<{ runId: string; status: string }>(`/api/admin/provisioning-runs/${runId}/cancel`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  markAdminProvisioningSupportNeeded(token: string, runId: string): Promise<{ runId: string; supportConversationId: string }> {
    return this.request<{ runId: string; supportConversationId: string }>(`/api/admin/provisioning-runs/${runId}/support-needed`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  getAdminTelegramBotSettings(token: string): Promise<AdminTelegramBotSettingsDto> {
    return this.request<AdminTelegramBotSettingsDto>('/api/admin/telegram-bot/settings', { token, errorMessage: apiFallbackErrorMessage })
  }

  testAdminTelegramBotSettings(token: string): Promise<AdminTelegramBotConnectionCheckDto> {
    return this.request<AdminTelegramBotConnectionCheckDto>('/api/admin/telegram-bot/settings/test', {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    })
  }

  updateAdminTelegramBotSettings(token: string, payload: UpdateTelegramBotSettingsPayload): Promise<AdminTelegramBotSettingsDto> {
    return this.request<AdminTelegramBotSettingsDto>('/api/admin/telegram-bot/settings', {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    })
  }
}
