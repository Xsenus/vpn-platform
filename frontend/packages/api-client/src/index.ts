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
  createdByUserId: string | null
  createdByUserName: string
  updatedByUserId: string | null
  updatedByUserName: string
  createdAt: string
  updatedAt: string
}

export type AppVersionLatestResponse = {
  currentVersion: string | null
  latestRelease: AppReleaseDto | null
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

export type AdminNotificationDeliveryDto = {
  id: string
  userId?: string | null
  templateKey: string
  channel: string
  maskedToAddress: string
  status: string
  attempts: number
  processingStartedAt?: string | null
  nextAttemptAt?: string | null
  sentAt?: string | null
  errorText: string
  createdAt: string
  updatedAt: string
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
  validationResetToken: string | null
}

export type CheckoutSessionDto = {
  id: string
  token: string
  tariffId: string
  userId: string | null
  orderId: string | null
  status: string
  expiresAt: string
  emailHint: string | null
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
  linkedSubscriptionId: string | null
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
  isCheckoutConfigured: boolean
  checkoutConfigurationIssue: string | null
  capabilitiesJson: string
  capabilities: PaymentProviderCapabilityDto[]
  requiredFields: PaymentProviderRequiredFieldDto[]
  readinessBlockers: string[]
  isPubliclyAvailable: boolean
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
  revision: number
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
  disabledAt: string | null
  lastSyncedAt: string | null
  revision: number
  usedTrafficBytes: number | null
  message: string | null
}

export type AdminSubscriptionExtendResultDto = {
  id: string
  status: 'Active'
  endAt: string
  gracePeriodEndAt: string
}

export type AdminSubscriptionActivateResultDto = {
  id: string
  status: 'Active'
  endAt: string
  currentAccessId: string | null
  access: AccessActionResultDto | null
}

export type AdminSubscriptionBlockResultDto = {
  id: string
  status: 'Blocked'
  blockReason: string
}

export type AdminSubscriptionStatusResultDto = {
  id: string
  status: string
}

export type AdminSubscriptionCancelResultDto = {
  id: string
  status: 'Cancelled'
  cancelledAt: string
}

export type AdminSubscriptionAccessSyncResultDto = {
  id: string
  currentAccessId: string
  access: AccessActionResultDto
}

export type AdminSubscriptionMigrationResultDto = {
  migrationJobId: string
  subscriptionId: string
  sourceNodeId: string
  targetNodeId: string | null
  status: 'completed'
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
  type: string
  status: string
  value: number
  currencyOrUnit: string
  processedAt?: string | null
  createdAt: string
}

export type AdminReferralProgramDto = {
  id: string
  name: string
  status: string
  startAt?: string | null
  endAt?: string | null
  ruleDefinition: string
  rewardDefinition: string
  antiFraudSettings: string
  createdAt: string
  updatedAt: string
}

export type ReferralProgramUpsertPayload = {
  name: string
  status: string
  startAt?: string | null
  endAt?: string | null
  ruleDefinition: string
  rewardDefinition: string
  antiFraudSettings?: string
}

export type AdminRewardLedgerDto = RewardLedgerDto & {
  userId: string
  sourceUserId?: string | null
  referralProgramId?: string | null
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
  lastHealthCheckAt: string | null
  lastHealthLatencyMs: number | null
  lastHealthError: string
  lastHealthMetadataJson: string
  provisioningStatus: string
  provisioningMode: string
  provisioningModeTitle: string
  provisioningRiskLevel: string
  liveDeployAllowed: boolean
  provisioningNextAction: string
  provisioningOperatorWarning: string
  precheckMode: string
  precheckModeTitle: string
  installedVersion: string
  backupStatus: string
  monitoringStatus: string
  loggingStatus: string
  tagsCsv: string
  priority: number
  isAvailableForNewUsers: boolean
  sshUser: string
  sshPort: number
  sshAuthMethod: string
  sshCredentialConfigured: boolean
  skipHostKeyChecking: boolean
  panelBaseUrl: string
  panelUsername: string
  panelPasswordConfigured: boolean
  panelInboundId: number | null
  publicHostname: string
  publicPort: number
  nodeGroupId: string | null
  createdAt: string
  updatedAt: string
}

export type ProvisioningRunDto = {
  id: string
  nodeId: string
  status: string
  nodeName: string
  targetHost: string
  sshPort: number
  username: string
  authMethod: string
  credentialsConfigured: boolean
  source: string
  owner: string
  validationMode: boolean
  mode: string
  modeTitle: string
  riskLevel: string
  liveDeployAllowed: boolean
  nextAction: string
  operatorWarning: string
  deployMode: string
  deployModeTitle: string
  deployRiskLevel: string
  deployLiveDeployAllowed: boolean
  deployNextAction: string
  deployOperatorWarning: string
  currentStep: string
  requestedByUserId: string | null
  dryRun: boolean
  attemptCount: number
  processingStartedAt: string | null
  leaseExpiresAt: string | null
  lastError: string
  startedAt: string
  finishedAt: string | null
  errorSummary: string
  executionLog: string
  executionLogPreview: string
  precheckReportPreview: string
  createdAt: string
  updatedAt: string
}

export type ProvisioningCommandResponse = {
  serverId: string
  runId: string
  status: string
  dryRun: boolean
  mode: string
  modeTitle: string
  riskLevel: string
  liveDeployAllowed: boolean
  nextAction: string
  operatorWarning: string
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
  run: Omit<ProvisioningRunDto, 'executionLogPreview' | 'precheckReportPreview'> & {
    precheckReport: string
    linkedAccessId: string | null
  }
  steps: Array<{
    id: string
    provisioningRunId: string
    stepName: string
    status: string
    startedAt: string
    finishedAt: string | null
    output: string
    errorText: string
    createdAt: string
    updatedAt: string
  }>
}

export type DeleteAdminServerResult = {
  id: string
  deleted: boolean
  archived: boolean
  linkedSubscriptions: number
  linkedAccesses: number
  linkedProvisioningRuns: number
  linkedHealthChecks: number
  linkedMigrationJobs: number
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
  lastHealthCheckAt: string | null
  lastSyncAt: string | null
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
  totalGb: number | null
  expiryTime: string
  enable: boolean
  configUri: string
  qrCodePayload: string
  syncStatus: string
  lastSyncedAt: string | null
}

export type PanelSyncRunDto = {
  id: string
  vpnPanelId: string
  status: string
  startedAt: string
  finishedAt: string | null
  summaryJson: string
  errorMessage: string
}

export type PanelSyncEventDto = {
  id: string
  panelSyncRunId: string
  eventType: string
  entityType: string
  entityId: string | null
  externalId: string
  message: string
  payloadJson: string
}

export type PanelHealthCheckDto = {
  id: string
  vpnPanelId: string
  status: string
  latencyMs: number | null
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
  paymentProvider: PaymentProvider
  promoCode?: string | null
  emailHint?: string | null
  returnUrl?: string | null
}

export type CreateMyOrderPayload = {
  tariffId: string
  type: OrderType
  paymentProvider: PaymentProvider
  promoCode?: string | null
  subscriptionId?: string | null
}

export function buildAuthHeaders(token?: string | null): HeadersInit {
  return token ? { Authorization: `Bearer ${token}` } : {}
}

const authErrorMessages: Record<string, string> = {
  invalid_credentials: 'Неверный email или пароль.',
  invalid_registration_request: 'Проверьте email и пароль: пароль должен быть не короче 8 символов.',
  email_exists: 'Аккаунт с таким email уже зарегистрирован. Войдите или восстановите пароль.',
  invalid_referral_code: 'Реферальный код не найден или больше недоступен.',
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
const apiRequestTimeoutMessage = 'Сервер не ответил вовремя. Проверьте подключение и повторите запрос.'
const apiEmptyResponseMessage = 'Сервер вернул пустой ответ. Повторите запрос позже.'
const apiInvalidJsonResponseMessage = 'Сервер вернул некорректный JSON-ответ. Повторите запрос позже.'
const apiUnsupportedResponseMessage = 'Сервер вернул ответ в неподдерживаемом формате. Повторите запрос позже.'
const apiOversizedResponseMessage = 'Ответ сервера превышает допустимый размер. Повторите запрос позже.'
const apiUnexpectedResponseShapeMessage = 'Сервер вернул JSON-ответ неожиданной формы. Повторите запрос позже.'
const apiInvalidResponseDataMessage = 'Сервер вернул JSON-ответ с некорректными данными. Повторите запрос позже.'
const defaultApiRequestTimeoutMs = 30_000
const defaultJsonResponseMaxBytes = 10_000_000
const defaultApiErrorResponseMaxBytes = 64_000

const paymentProviderValues = new Set<PaymentProvider>([
  'YooMoney',
  'YooKassa',
  'RoboKassa',
  'TelegramStars',
  'CloudPayments',
  'TBankAcquiring',
  'Prodamus',
  'Stripe',
  'PayPal'
])

export function isPaymentProvider(value: unknown): value is PaymentProvider {
  return typeof value === 'string' && paymentProviderValues.has(value as PaymentProvider)
}

const publicPaymentProviderModeValues = new Set<PaymentProviderMode>(['Sandbox', 'Production'])
const paymentProviderModeValues = new Set<PaymentProviderMode>(['Disabled', 'Sandbox', 'Production'])
const userStatusValues = new Set(['New', 'Active', 'Suspended', 'Deleted'])
const authSourceValues = new Set(['Local', 'Telegram', 'Imported'])
const channelTypeValues = new Set<ChannelType>(['Web', 'Telegram', 'Discord', 'Vk', 'WhatsApp', 'Email'])
const orderTypeValues = new Set<OrderType>(['NewSubscription', 'Renewal', 'Upgrade', 'Compensation'])
const orderStatusValues = new Set(['Draft', 'PendingPayment', 'PaymentReceived', 'FulfillmentInProgress', 'Completed', 'Failed', 'Cancelled', 'Expired', 'Refunded', 'PartiallyProcessed'])
const paymentStatusValues = new Set(['New', 'Pending', 'WaitingConfirmation', 'Succeeded', 'Failed', 'Cancelled', 'Refunded', 'PartiallyRefunded', 'Unknown'])
const subscriptionStatusValues = new Set(['PendingActivation', 'Active', 'GracePeriod', 'Expired', 'Suspended', 'Cancelled', 'Blocked'])
const accessCredentialStatusValues = new Set(['Provisioning', 'Active', 'Rotating', 'Disabled', 'Revoked', 'Error', 'SyncRequired'])
const rewardStatusValues = new Set(['Pending', 'Approved', 'Cancelled', 'Reverted'])
const supportChannelValues = new Set(['web', 'telegram'])
const supportStatusValues = new Set(['open', 'pending', 'closed'])
const supportDirectionValues = new Set(['inbound', 'outbound'])
const referralProgramStatusValues = new Set(['draft', 'active', 'paused', 'archived'])
const appReleaseItemTypeValues = new Set(['new', 'improved', 'fixed', 'important'])
const appReleaseSourceValues = new Set(['agent', 'manual'])
const vpnPanelStatusValues = new Set(['New', 'Active', 'Disabled', 'Maintenance', 'Error'])
const healthStatusValues = new Set(['Unknown', 'Healthy', 'Degraded', 'Unhealthy'])
const vpnSslVerificationModeValues = new Set(['Strict', 'AllowSelfSigned', 'Disabled'])
const x3UiApiVariantValues = new Set(['X3UiOfficial', 'ThreeXUi', 'LegacyXUi', 'Custom'])
const vpnInboundProtocolValues = new Set(['vless', 'vmess', 'trojan'])
const panelSyncRunStatusValues = new Set(['Pending', 'Running', 'Succeeded', 'Failed'])
const nodeStatusValues = new Set(['New', 'Provisioning', 'Ready', 'Degraded', 'Full', 'Draining', 'Maintenance', 'Disabled', 'Error', 'Archived'])
const provisioningRunStatusValues = new Set(['Pending', 'Running', 'Succeeded', 'Failed', 'Cancelled', 'Requested', 'AwaitingCredentials', 'AwaitingConfirmation', 'PrecheckQueued', 'Prechecking', 'PrecheckFailed', 'ReadyToDeploy', 'DeployQueued', 'Deploying', 'Deployed', 'Retrying'])
const provisioningCommandStatusValues = new Set(['queued', ...provisioningRunStatusValues])
const provisioningModeValues = new Map([
  ['dry-run', { risk: 'safe', live: false }],
  ['unknown', { risk: 'blocked', live: false }],
  ['validation-deploy', { risk: 'low', live: false }],
  ['live-deploy', { risk: 'high', live: true }],
  ['live-deploy-blocked', { risk: 'blocked', live: false }]
])
const telegramBotModeValues = new Set(['LongPolling', 'Webhook'])
const telegramBotCheckStatusValues = new Set(['ready', 'needs_configuration'])
const checkoutSessionStatusValues = new Set(['open', 'claiming', 'claimed', 'completed', 'expired'])

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function hasString(record: Record<string, unknown>, key: string, requireValue = false) {
  const value = record[key]
  return typeof value === 'string' && (!requireValue || value.trim().length > 0)
}

function hasFiniteNumber(record: Record<string, unknown>, key: string, minimum?: number) {
  const value = record[key]
  return typeof value === 'number' && Number.isFinite(value) && (minimum === undefined || value >= minimum)
}

function hasInteger(record: Record<string, unknown>, key: string, minimum?: number) {
  return hasFiniteNumber(record, key, minimum) && Number.isInteger(record[key])
}

function hasBoolean(record: Record<string, unknown>, key: string) {
  return typeof record[key] === 'boolean'
}

function hasNullableString(record: Record<string, unknown>, key: string) {
  return record[key] === null || typeof record[key] === 'string'
}

function hasNullableInteger(record: Record<string, unknown>, key: string, minimum?: number) {
  return record[key] === null || hasInteger(record, key, minimum)
}

function hasDateString(record: Record<string, unknown>, key: string) {
  const value = record[key]
  return typeof value === 'string' && value.trim().length > 0 && Number.isFinite(Date.parse(value))
}

function hasNullableDateString(record: Record<string, unknown>, key: string) {
  return record[key] === null || hasDateString(record, key)
}

function hasStringArray(record: Record<string, unknown>, key: string, requireNonEmptyItems = false) {
  const value = record[key]
  return Array.isArray(value)
    && value.every((item) => typeof item === 'string' && (!requireNonEmptyItems || item.trim().length > 0))
}

function hasJsonObjectString(record: Record<string, unknown>, key: string) {
  if (!hasString(record, key, true)) return false
  try {
    return isRecord(JSON.parse(record[key] as string))
  } catch {
    return false
  }
}

function hasEmptyOrJsonObjectString(record: Record<string, unknown>, key: string) {
  return hasString(record, key)
    && (record[key] === '' || hasJsonObjectString(record, key))
}

function hasJsonUniqueStringArray(record: Record<string, unknown>, key: string) {
  if (!hasString(record, key, true)) return false
  try {
    const parsed = JSON.parse(record[key] as string)
    return Array.isArray(parsed)
      && parsed.every((item) => typeof item === 'string' && item.trim().length > 0)
      && new Set(parsed).size === parsed.length
  } catch {
    return false
  }
}

function hasAbsoluteHttpUrl(record: Record<string, unknown>, key: string) {
  if (!hasString(record, key, true)) return false
  try {
    const url = new URL(record[key] as string)
    return (url.protocol === 'http:' || url.protocol === 'https:') && url.hostname.length > 0
  } catch {
    return false
  }
}

function hasSafeAbsoluteHttpUrl(record: Record<string, unknown>, key: string) {
  if (!hasAbsoluteHttpUrl(record, key)) return false
  const url = new URL(record[key] as string)
  return url.username.length === 0 && url.password.length === 0
}

function hasProvisioningModeDescriptor(
  record: Record<string, unknown>,
  modeKey: string,
  titleKey: string,
  riskKey: string,
  liveKey: string,
  nextActionKey: string,
  warningKey: string
) {
  if (!hasString(record, modeKey, true)
    || !hasString(record, titleKey, true)
    || !hasString(record, riskKey, true)
    || !hasBoolean(record, liveKey)
    || !hasString(record, nextActionKey, true)
    || !hasString(record, warningKey, true)) return false

  const expected = provisioningModeValues.get(record[modeKey] as string)
  return expected !== undefined
    && record[riskKey] === expected.risk
    && record[liveKey] === expected.live
}

function hasUniqueStringKey(items: unknown[], key: string) {
  const values = new Set<string>()
  for (const item of items) {
    if (!isRecord(item) || typeof item[key] !== 'string' || values.has(item[key])) return false
    values.add(item[key])
  }
  return true
}

function isTariffDto(value: unknown): value is TariffDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'name', true)
    && hasString(value, 'slug', true)
    && hasString(value, 'description')
    && hasString(value, 'fullDescription')
    && Array.isArray(value.features)
    && value.features.every((item) => typeof item === 'string')
    && hasString(value, 'featuresJson')
    && hasString(value, 'badge')
    && hasInteger(value, 'durationDays', 1)
    && hasFiniteNumber(value, 'price', 0)
    && hasString(value, 'currency', true)
    && hasInteger(value, 'maxDevices', 1)
    && (value.trafficLimit === null || (hasInteger(value, 'trafficLimit', 0)))
    && hasBoolean(value, 'isTrial')
    && hasBoolean(value, 'isActive')
    && hasInteger(value, 'sortOrder')
    && hasNullableString(value, 'visibleFrom')
    && hasNullableString(value, 'visibleTo')
    && hasString(value, 'tariffType', true)
    && hasString(value, 'category', true)
    && hasString(value, 'allowedRegionsCsv')
    && hasString(value, 'allowedNodeGroupsCsv')
    && hasBoolean(value, 'isReferralEligible')
    && hasString(value, 'provisioningScenario', true)
    && hasString(value, 'afterPaymentText')
    && hasString(value, 'createdAt', true)
    && hasString(value, 'updatedAt', true)
}

function isFaqItem(value: unknown): value is FaqItem {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'question', true)
    && hasString(value, 'answer', true)
    && hasString(value, 'category', true)
    && hasBoolean(value, 'isActive')
    && hasBoolean(value, 'showOnHome')
    && hasBoolean(value, 'showOnFaqPage')
    && hasInteger(value, 'sortOrder')
    && hasString(value, 'createdAt', true)
    && hasString(value, 'updatedAt', true)
}

function isSiteContentBlockDto(value: unknown): value is SiteContentBlockDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'key', true)
    && hasString(value, 'value')
    && hasString(value, 'group', true)
    && hasString(value, 'label')
    && hasString(value, 'description')
    && hasString(value, 'inputType', true)
    && hasBoolean(value, 'isActive')
    && hasInteger(value, 'sortOrder')
    && hasString(value, 'createdAt', true)
    && hasString(value, 'updatedAt', true)
}

function isPublicPaymentProviderDto(value: unknown): value is PublicPaymentProviderDto {
  if (!isRecord(value)) return false

  return hasString(value, 'provider', true)
    && paymentProviderValues.has(value.provider as PaymentProvider)
    && hasString(value, 'publicName', true)
    && hasString(value, 'mode', true)
    && publicPaymentProviderModeValues.has(value.mode as PaymentProviderMode)
    && hasString(value, 'healthStatus', true)
}

function isAdminSessionCapabilitiesDto(value: unknown): value is AdminSessionCapabilitiesDto {
  if (!isRecord(value)) return false

  const keys: Array<keyof AdminSessionCapabilitiesDto> = [
    'adminRead',
    'adminWrite',
    'financeRead',
    'financeWrite',
    'supportRead',
    'supportWrite',
    'provisioningManage',
    'vpnManage',
    'botManage',
    'settingsManage'
  ]
  if (!keys.every((key) => hasBoolean(value, key))) return false

  return value.adminRead === true
    && (value.adminWrite !== true || value.adminRead === true)
    && (value.financeWrite !== true || value.financeRead === true)
    && (value.supportWrite !== true || value.supportRead === true)
}

function isAdminSessionDto(value: unknown): value is AdminSessionDto {
  if (!isRecord(value)) return false

  return hasString(value, 'userId', true)
    && hasString(value, 'email', true)
    && hasString(value, 'displayName', true)
    && hasStringArray(value, 'roles', true)
    && (value.roles as string[]).length > 0
    && new Set(value.roles as string[]).size === (value.roles as string[]).length
    && isAdminSessionCapabilitiesDto(value.capabilities)
}

function isAdminProductionReadinessCheckDto(value: unknown): value is AdminProductionReadinessCheckDto {
  if (!isRecord(value)) return false

  return hasString(value, 'key', true)
    && hasString(value, 'label', true)
    && hasString(value, 'status', true)
    && (value.status === 'Ready' || value.status === 'Blocked')
    && hasString(value, 'message')
    && hasString(value, 'category', true)
    && hasString(value, 'severity', true)
    && hasString(value, 'actionLabel')
    && hasString(value, 'actionHref')
}

function isAdminDashboardSummaryDto(value: unknown): value is AdminDashboardSummaryDto {
  if (!isRecord(value)) return false

  const countKeys: Array<keyof AdminDashboardSummaryDto> = [
    'totalUsers',
    'telegramUsers',
    'activeSubscriptions',
    'expiringSubscriptions',
    'paidOrders',
    'pendingOrders',
    'failedPayments',
    'recentPayments',
    'recentOrders',
    'vpnAccessesCount',
    'vpnNodesCount',
    'healthyVpnNodes',
    'vpnPanelsCount',
    'healthyVpnPanels',
    'supportConversationsCount',
    'openSupportConversations',
    'provisioningErrors'
  ]
  if (!countKeys.every((key) => hasInteger(value, key, 0)) || !hasDateString(value, 'generatedAt')) return false
  if (!isRecord(value.productionReadiness)
    || !hasBoolean(value.productionReadiness, 'isReady')
    || !hasString(value.productionReadiness, 'status', true)
    || (value.productionReadiness.status !== 'Ready' && value.productionReadiness.status !== 'Blocked')
    || !Array.isArray(value.productionReadiness.checks)
    || !value.productionReadiness.checks.every(isAdminProductionReadinessCheckDto)) return false

  return hasUniqueStringKey(value.productionReadiness.checks, 'key')
}

function isAdminUserDto(value: unknown): value is AdminUserDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasNullableString(value, 'email')
    && hasString(value, 'displayName', true)
    && hasString(value, 'rolesCsv', true)
    && hasString(value, 'status', true)
    && userStatusValues.has(value.status as string)
    && hasBoolean(value, 'isBlocked')
    && hasString(value, 'preferredLanguage', true)
    && hasString(value, 'referralCode', true)
    && hasString(value, 'authSource', true)
    && authSourceValues.has(value.authSource as string)
    && hasBoolean(value, 'emailConfirmed')
    && hasNullableDateString(value, 'lastLoginAt')
    && hasNullableDateString(value, 'telegramRegistrationCompletedAt')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isAdminSubscriptionDto(value: unknown): value is SubscriptionDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'userId', true)
    && hasString(value, 'tariffId', true)
    && hasNullableString(value, 'tariffName')
    && hasString(value, 'status', true)
    && subscriptionStatusValues.has(value.status as string)
    && hasDateString(value, 'startAt')
    && hasDateString(value, 'endAt')
    && hasNullableDateString(value, 'gracePeriodEndAt')
    && hasBoolean(value, 'autoRenewFlag')
    && hasString(value, 'sourceChannel', true)
    && channelTypeValues.has(value.sourceChannel as ChannelType)
    && hasNullableString(value, 'currentServerId')
    && hasNullableString(value, 'currentAccessId')
    && hasNullableString(value, 'lastPaymentId')
    && hasInteger(value, 'renewalCount', 0)
    && hasNullableString(value, 'blockReason')
    && hasNullableDateString(value, 'suspendedAt')
    && hasNullableDateString(value, 'cancelledAt')
    && hasInteger(value, 'lifecycleAttemptCount', 0)
    && hasNullableDateString(value, 'lifecycleProcessingStartedAt')
    && hasNullableDateString(value, 'lifecycleLeaseExpiresAt')
    && hasNullableDateString(value, 'lifecycleNextAttemptAt')
    && hasNullableString(value, 'lifecycleLastError')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isAccessActionResultDto(value: unknown, expectedId?: string): value is AccessActionResultDto {
  return isRecord(value)
    && hasString(value, 'id', true)
    && (expectedId === undefined || value.id === expectedId)
    && hasString(value, 'status', true)
    && accessCredentialStatusValues.has(value.status as string)
    && hasNullableDateString(value, 'disabledAt')
    && hasNullableDateString(value, 'lastSyncedAt')
    && hasInteger(value, 'revision', 0)
    && hasNullableInteger(value, 'usedTrafficBytes', 0)
    && hasNullableString(value, 'message')
}

function isAdminSubscriptionExtendResultDto(value: unknown, expectedId: string): value is AdminSubscriptionExtendResultDto {
  return isRecord(value)
    && value.id === expectedId
    && value.status === 'Active'
    && hasDateString(value, 'endAt')
    && hasDateString(value, 'gracePeriodEndAt')
    && Date.parse(value.gracePeriodEndAt as string) >= Date.parse(value.endAt as string)
}

function isAdminSubscriptionActivateResultDto(value: unknown, expectedId: string): value is AdminSubscriptionActivateResultDto {
  if (!isRecord(value)
    || value.id !== expectedId
    || value.status !== 'Active'
    || !hasDateString(value, 'endAt')
    || !hasNullableString(value, 'currentAccessId')
    || (value.access !== null && !isAccessActionResultDto(value.access))) return false

  return value.access === null
    || (typeof value.currentAccessId === 'string' && value.currentAccessId === value.access.id)
}

function isAdminSubscriptionBlockResultDto(value: unknown, expectedId: string): value is AdminSubscriptionBlockResultDto {
  return isRecord(value)
    && value.id === expectedId
    && value.status === 'Blocked'
    && hasString(value, 'blockReason', true)
}

function isAdminSubscriptionStatusResultDto(
  value: unknown,
  expectedId: string,
  allowedStatuses: ReadonlySet<string>
): value is AdminSubscriptionStatusResultDto {
  return isRecord(value)
    && value.id === expectedId
    && hasString(value, 'status', true)
    && allowedStatuses.has(value.status as string)
}

function isAdminSubscriptionCancelResultDto(value: unknown, expectedId: string): value is AdminSubscriptionCancelResultDto {
  return isRecord(value)
    && value.id === expectedId
    && value.status === 'Cancelled'
    && hasDateString(value, 'cancelledAt')
}

function isAdminSubscriptionAccessSyncResultDto(value: unknown, expectedId: string): value is AdminSubscriptionAccessSyncResultDto {
  return isRecord(value)
    && value.id === expectedId
    && hasString(value, 'currentAccessId', true)
    && isAccessActionResultDto(value.access, value.currentAccessId as string)
}

function isAdminSubscriptionMigrationResultDto(
  value: unknown,
  expectedSubscriptionId: string,
  expectedTargetNodeId: string | null
): value is AdminSubscriptionMigrationResultDto {
  return isRecord(value)
    && hasString(value, 'migrationJobId', true)
    && value.subscriptionId === expectedSubscriptionId
    && hasString(value, 'sourceNodeId', true)
    && hasNullableString(value, 'targetNodeId')
    && (expectedTargetNodeId === null || value.targetNodeId === expectedTargetNodeId)
    && value.sourceNodeId !== value.targetNodeId
    && value.status === 'completed'
}

function isAdminAccessHistoryDto(value: unknown): value is AccessCredentialHistoryDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'accessCredentialId', true)
    && hasString(value, 'subscriptionId', true)
    && hasString(value, 'eventType', true)
    && hasString(value, 'oldValueJson')
    && hasString(value, 'newValueJson')
    && hasDateString(value, 'createdAt')
}

function isAdminAccessCredentialDto(value: unknown): value is AccessCredentialDto {
  if (!isCabinetAccessCredentialDto(value) || !isRecord(value)) return false

  return Array.isArray(value.history)
    && value.history.every(isAdminAccessHistoryDto)
    && hasUniqueStringKey(value.history, 'id')
}

function isAdminPaymentAttemptDto(value: unknown): value is PaymentAttemptDto {
  if (!isCabinetPaymentAttemptDto(value) || !isRecord(value)) return false

  return hasBoolean(value, 'refundSupported')
    && hasBoolean(value, 'canRefund')
    && hasFiniteNumber(value, 'refundableAmount', 0)
    && hasStringArray(value, 'refundBlockers')
}

function isAdminAuditLogDto(value: unknown): value is AdminAuditLogDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'actorType', true)
    && hasString(value, 'actorId')
    && hasString(value, 'action', true)
    && hasString(value, 'entityType', true)
    && hasString(value, 'entityId')
    && hasString(value, 'beforeJson')
    && hasString(value, 'afterJson')
    && hasString(value, 'ip')
    && hasString(value, 'userAgent')
    && hasDateString(value, 'createdAt')
}

function isAdminNotificationDeliveryDto(value: unknown): value is AdminNotificationDeliveryDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasNullableString(value, 'userId')
    && hasString(value, 'templateKey', true)
    && hasString(value, 'channel', true)
    && hasString(value, 'maskedToAddress')
    && hasString(value, 'status', true)
    && hasInteger(value, 'attempts', 0)
    && hasNullableDateString(value, 'processingStartedAt')
    && hasNullableDateString(value, 'nextAttemptAt')
    && hasNullableDateString(value, 'sentAt')
    && hasString(value, 'errorText')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isAdminNotificationRetryResult(value: unknown): value is { id: string; status: string; nextAttemptAt?: string | null } {
  return isRecord(value)
    && hasString(value, 'id', true)
    && hasString(value, 'status', true)
    && hasNullableDateString(value, 'nextAttemptAt')
}

function isPaymentStatusResultDto(value: unknown): value is PaymentStatusResultDto {
  if (!isRecord(value)) return false

  return (value.orderId === undefined || hasString(value, 'orderId', true))
    && hasString(value, 'paymentId', true)
    && hasString(value, 'status', true)
    && paymentStatusValues.has(value.status as string)
    && hasString(value, 'rawResponse')
    && (value.statusReason === undefined || hasNullableString(value, 'statusReason'))
}

function isPaymentProviderCapabilityDto(value: unknown): value is PaymentProviderCapabilityDto {
  return isRecord(value)
    && hasString(value, 'key', true)
    && hasString(value, 'label', true)
    && hasBoolean(value, 'supported')
    && hasString(value, 'status', true)
}

function isPaymentProviderRequiredFieldDto(value: unknown): value is PaymentProviderRequiredFieldDto {
  return isRecord(value)
    && hasString(value, 'key', true)
    && hasString(value, 'label', true)
    && hasBoolean(value, 'required')
    && hasBoolean(value, 'configured')
    && hasNullableString(value, 'issue')
}

function isPaymentProviderAccountDto(value: unknown): value is PaymentProviderAccountDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'provider', true)
    && paymentProviderValues.has(value.provider as PaymentProvider)
    && hasString(value, 'mode', true)
    && paymentProviderModeValues.has(value.mode as PaymentProviderMode)
    && hasString(value, 'name', true)
    && hasString(value, 'publicName', true)
    && hasBoolean(value, 'isEnabled')
    && hasBoolean(value, 'isDefault')
    && hasString(value, 'shopId')
    && hasString(value, 'apiBaseUrl')
    && hasString(value, 'returnUrl')
    && hasString(value, 'webhookUrl')
    && hasBoolean(value, 'hasSecretKey')
    && hasBoolean(value, 'hasWebhookSecret')
    && hasBoolean(value, 'useWebhookIpAllowList')
    && hasString(value, 'allowedWebhookIpRangesCsv')
    && hasString(value, 'extraSettingsJson')
    && hasString(value, 'healthStatus', true)
    && hasBoolean(value, 'isCheckoutConfigured')
    && hasNullableString(value, 'checkoutConfigurationIssue')
    && hasString(value, 'capabilitiesJson')
    && Array.isArray(value.capabilities)
    && value.capabilities.every(isPaymentProviderCapabilityDto)
    && hasUniqueStringKey(value.capabilities, 'key')
    && Array.isArray(value.requiredFields)
    && value.requiredFields.every(isPaymentProviderRequiredFieldDto)
    && hasUniqueStringKey(value.requiredFields, 'key')
    && hasStringArray(value, 'readinessBlockers', true)
    && hasBoolean(value, 'isPubliclyAvailable')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isPaymentProviderAccountCheckResultDto(value: unknown): value is PaymentProviderAccountCheckResultDto {
  return isRecord(value)
    && hasString(value, 'accountId', true)
    && hasString(value, 'provider', true)
    && paymentProviderValues.has(value.provider as PaymentProvider)
    && hasString(value, 'mode', true)
    && paymentProviderModeValues.has(value.mode as PaymentProviderMode)
    && hasBoolean(value, 'isReady')
    && value.checkScope === 'ConfigurationOnly'
    && (value.configurationStatus === 'Ready' || value.configurationStatus === 'NeedsConfiguration')
    && hasString(value, 'healthStatus', true)
    && hasString(value, 'message', true)
    && hasStringArray(value, 'details', true)
    && hasDateString(value, 'checkedAt')
    && isPaymentProviderAccountDto(value.account)
    && value.accountId === value.account.id
    && value.provider === value.account.provider
    && value.mode === value.account.mode
}

function isPaymentWebhookEventDto(value: unknown): value is PaymentWebhookEventDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'provider', true)
    && paymentProviderValues.has(value.provider as PaymentProvider)
    && hasNullableString(value, 'paymentAttemptId')
    && hasNullableString(value, 'paymentProviderAccountId')
    && hasString(value, 'providerPaymentId')
    && hasString(value, 'externalEventId')
    && hasString(value, 'eventType', true)
    && hasString(value, 'status', true)
    && hasBoolean(value, 'signatureValidated')
    && hasDateString(value, 'receivedAt')
    && hasNullableDateString(value, 'processedAt')
    && hasString(value, 'errorText')
}

function isRefundDto(value: unknown): value is RefundDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'paymentAttemptId', true)
    && hasString(value, 'provider', true)
    && paymentProviderValues.has(value.provider as PaymentProvider)
    && hasString(value, 'providerRefundId', true)
    && hasString(value, 'status', true)
    && hasFiniteNumber(value, 'amount', 0)
    && (value.amount as number) > 0
    && hasString(value, 'currency', true)
    && hasString(value, 'reason')
    && hasDateString(value, 'createdAt')
    && hasNullableDateString(value, 'refundedAt')
}

function isAdminSupportMessageDto(value: unknown): value is SupportMessageDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'supportConversationId', true)
    && hasNullableString(value, 'userId')
    && hasNullableInteger(value, 'telegramUserId', 1)
    && hasString(value, 'direction', true)
    && (supportDirectionValues.has(value.direction as string) || value.direction === 'internal')
    && hasString(value, 'text', true)
    && hasString(value, 'attachmentsJson')
    && hasBoolean(value, 'isInternalNote')
    && (value.isInternalNote === (value.direction === 'internal'))
    && hasDateString(value, 'createdAt')
}

function isAdminSupportMutationResult(value: unknown): value is { conversationId: string; status: string; revision: number } {
  return isRecord(value)
    && hasString(value, 'conversationId', true)
    && hasString(value, 'status', true)
    && hasInteger(value, 'revision', 1)
}

function isAdminReferralProgramDto(value: unknown): value is AdminReferralProgramDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'name', true)
    && hasString(value, 'status', true)
    && referralProgramStatusValues.has(value.status as string)
    && hasNullableDateString(value, 'startAt')
    && hasNullableDateString(value, 'endAt')
    && (value.startAt === null || value.endAt === null || Date.parse(value.endAt as string) > Date.parse(value.startAt as string))
    && hasJsonObjectString(value, 'ruleDefinition')
    && hasJsonObjectString(value, 'rewardDefinition')
    && hasJsonObjectString(value, 'antiFraudSettings')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isAdminRewardLedgerDto(value: unknown): value is AdminRewardLedgerDto {
  return isRewardLedgerDto(value)
    && isRecord(value)
    && hasString(value, 'userId', true)
    && hasNullableString(value, 'sourceUserId')
    && hasNullableString(value, 'referralProgramId')
}

function isAppReleaseItemDto(value: unknown): value is AppReleaseItemDto {
  return isRecord(value)
    && hasString(value, 'id', true)
    && hasString(value, 'type', true)
    && appReleaseItemTypeValues.has(value.type as string)
    && hasString(value, 'text', true)
    && hasInteger(value, 'sortOrder', 0)
}

function isAppReleaseDto(value: unknown): value is AppReleaseDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'releaseId', true)
    && hasString(value, 'version', true)
    && hasDateString(value, 'releasedAt')
    && hasString(value, 'title', true)
    && hasString(value, 'summary', true)
    && hasBoolean(value, 'isActive')
    && hasString(value, 'source', true)
    && appReleaseSourceValues.has(value.source as string)
    && Array.isArray(value.items)
    && value.items.every(isAppReleaseItemDto)
    && hasUniqueStringKey(value.items, 'id')
    && hasNullableString(value, 'createdByUserId')
    && hasString(value, 'createdByUserName')
    && hasNullableString(value, 'updatedByUserId')
    && hasString(value, 'updatedByUserName')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isAppVersionLatestResponse(value: unknown): value is AppVersionLatestResponse {
  if (!isRecord(value)
    || !hasNullableString(value, 'currentVersion')
    || !hasBoolean(value, 'seenByCurrentUser')
    || (value.latestRelease !== null && !isAppReleaseDto(value.latestRelease))) return false

  return value.latestRelease === null
    ? value.currentVersion === null && value.seenByCurrentUser === true
    : value.currentVersion === value.latestRelease.version
}

function isAppReleaseSeenResult(value: unknown): value is { releaseId: string; seen: boolean } {
  return isRecord(value)
    && hasString(value, 'releaseId', true)
    && value.seen === true
}

function isAppReleaseOverviewDto(value: unknown): value is AppReleaseOverviewDto {
  if (!isRecord(value)) return false

  const countKeys: Array<keyof AppReleaseOverviewDto> = [
    'totalCount',
    'publishedCount',
    'upcomingCount',
    'hiddenCount',
    'agentCount',
    'manualCount',
    'seenCount'
  ]
  if (!countKeys.every((key) => hasInteger(value, key, 0))
    || !hasNullableString(value, 'latestPublishedReleaseId')
    || !hasNullableString(value, 'latestPublishedVersion')
    || !hasStringArray(value, 'emptyReleaseIds', true)
    || new Set(value.emptyReleaseIds as string[]).size !== (value.emptyReleaseIds as string[]).length) return false

  return value.totalCount === (value.publishedCount as number) + (value.upcomingCount as number) + (value.hiddenCount as number)
    && value.totalCount === (value.agentCount as number) + (value.manualCount as number)
    && ((value.publishedCount as number) > 0
      ? value.latestPublishedReleaseId !== null && value.latestPublishedVersion !== null
      : value.latestPublishedReleaseId === null && value.latestPublishedVersion === null)
}

function isFaqOverviewDto(value: unknown): value is FaqOverviewDto {
  if (!isRecord(value)) return false

  const countKeys: Array<keyof FaqOverviewDto> = [
    'totalCount',
    'activeCount',
    'hiddenCount',
    'homeCount',
    'faqPageCount',
    'publicCount',
    'categoryCount'
  ]
  if (!countKeys.every((key) => hasInteger(value, key, 0))
    || !hasStringArray(value, 'categories', true)
    || !hasStringArray(value, 'duplicateQuestions', true)
    || !hasBoolean(value, 'hasPublicFaq')
    || !hasBoolean(value, 'hasHomeFaq')) return false

  return value.totalCount === (value.activeCount as number) + (value.hiddenCount as number)
    && value.categoryCount === (value.categories as string[]).length
    && new Set(value.categories as string[]).size === (value.categories as string[]).length
    && new Set(value.duplicateQuestions as string[]).size === (value.duplicateQuestions as string[]).length
    && (value.homeCount as number) <= (value.activeCount as number)
    && (value.faqPageCount as number) <= (value.activeCount as number)
    && value.publicCount === value.faqPageCount
    && value.hasPublicFaq === ((value.publicCount as number) > 0)
    && value.hasHomeFaq === ((value.homeCount as number) > 0)
}

function isSiteContentReadinessDto(value: unknown): value is SiteContentReadinessDto {
  if (!isRecord(value)
    || !hasBoolean(value, 'isReady')
    || !hasInteger(value, 'requiredCount', 0)
    || !hasInteger(value, 'presentCount', 0)
    || !hasInteger(value, 'activeRequiredCount', 0)
    || !hasInteger(value, 'publicBlocksCount', 0)) return false

  const arrayKeys: Array<keyof SiteContentReadinessDto> = [
    'missingKeys',
    'inactiveKeys',
    'emptyKeys',
    'duplicateKeys',
    'requiredKeys'
  ]
  if (!arrayKeys.every((key) => hasStringArray(value, key, true)
    && new Set(value[key] as string[]).size === (value[key] as string[]).length)) return false

  const noIssues = (value.missingKeys as string[]).length === 0
    && (value.inactiveKeys as string[]).length === 0
    && (value.emptyKeys as string[]).length === 0
    && (value.duplicateKeys as string[]).length === 0
  return value.requiredCount === (value.requiredKeys as string[]).length
    && (value.presentCount as number) <= (value.requiredCount as number)
    && (value.activeRequiredCount as number) <= (value.presentCount as number)
    && (value.missingKeys as string[]).length === (value.requiredCount as number) - (value.presentCount as number)
    && value.isReady === (noIssues
      && value.presentCount === value.requiredCount
      && value.activeRequiredCount === value.requiredCount)
}

function isSiteContentDefaultsResultDto(value: unknown): value is SiteContentDefaultsResultDto {
  return isRecord(value)
    && hasInteger(value, 'created', 0)
    && hasInteger(value, 'restored', 0)
    && isSiteContentReadinessDto(value.readiness)
}

function isWorkScenarioDto(value: unknown): value is WorkScenarioDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'name', true)
    && hasString(value, 'key', true)
    && hasBoolean(value, 'isActive')
    && hasJsonUniqueStringArray(value, 'allowedTariffIdsJson')
    && hasString(value, 'vpnProtocol', true)
    && hasString(value, 'serverSelectionRule', true)
    && hasString(value, 'inboundSelectionRule', true)
    && hasString(value, 'provisioningMode', true)
    && hasString(value, 'onPaymentSucceeded', true)
    && hasString(value, 'onPaymentFailed', true)
    && hasString(value, 'onRefund', true)
    && hasString(value, 'onSubscriptionExpired', true)
    && hasString(value, 'onRenewal', true)
    && hasString(value, 'cabinetText')
    && hasString(value, 'telegramText')
    && hasBoolean(value, 'generateQrCode')
    && hasInteger(value, 'maxDevices', 1)
    && hasNullableInteger(value, 'trafficLimit', 0)
    && hasInteger(value, 'sortOrder')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isDeleteResultDto(value: unknown): value is { id: string; deleted: boolean } {
  return isRecord(value)
    && hasString(value, 'id', true)
    && hasBoolean(value, 'deleted')
}

function isTariffDeleteResultDto(value: unknown): value is { id: string; deleted: boolean; archived?: boolean } {
  if (!isRecord(value)
    || !hasString(value, 'id', true)
    || !hasBoolean(value, 'deleted')) return false

  return (value.archived === undefined || typeof value.archived === 'boolean')
    && (value.deleted === true || value.archived === true)
}

function isVpnPanelDto(value: unknown): value is VpnPanelDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'name', true)
    && hasAbsoluteHttpUrl(value, 'baseUrl')
    && hasString(value, 'region')
    && hasString(value, 'status', true)
    && vpnPanelStatusValues.has(value.status as string)
    && hasString(value, 'healthStatus', true)
    && healthStatusValues.has(value.healthStatus as string)
    && hasString(value, 'login', true)
    && hasString(value, 'sslVerificationMode', true)
    && vpnSslVerificationModeValues.has(value.sslVerificationMode as string)
    && hasString(value, 'apiVariant', true)
    && x3UiApiVariantValues.has(value.apiVariant as string)
    && hasInteger(value, 'capacity', 1)
    && hasInteger(value, 'usedCapacity', 0)
    && (value.usedCapacity as number) <= (value.capacity as number)
    && hasBoolean(value, 'autoCreateInbound')
    && hasJsonObjectString(value, 'defaultInboundTemplateJson')
    && hasNullableDateString(value, 'lastHealthCheckAt')
    && hasNullableDateString(value, 'lastSyncAt')
    && hasString(value, 'version')
    && hasString(value, 'lastError')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
    && Date.parse(value.updatedAt as string) >= Date.parse(value.createdAt as string)
}

function isDeleteVpnPanelResult(value: unknown): value is DeleteVpnPanelResult {
  if (!isRecord(value)
    || !hasString(value, 'id', true)
    || !hasBoolean(value, 'deleted')
    || !hasBoolean(value, 'archived')) return false

  const countKeys: Array<keyof DeleteVpnPanelResult> = [
    'linkedInbounds',
    'linkedClients',
    'linkedSyncRuns',
    'linkedHealthChecks'
  ]
  if (!countKeys.every((key) => hasInteger(value, key, 0))) return false

  const linkedCount = countKeys.reduce((total, key) => total + (value[key] as number), 0)
  return value.deleted !== value.archived
    && value.archived === (linkedCount > 0)
}

function isVpnInboundDto(value: unknown): value is VpnInboundDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'vpnPanelId', true)
    && hasString(value, 'externalInboundId', true)
    && hasString(value, 'name', true)
    && hasString(value, 'protocol', true)
    && vpnInboundProtocolValues.has((value.protocol as string).toLowerCase())
    && hasInteger(value, 'port', 1)
    && (value.port as number) <= 65535
    && hasString(value, 'listen')
    && hasJsonObjectString(value, 'settingsJson')
    && hasJsonObjectString(value, 'streamSettingsJson')
    && hasJsonObjectString(value, 'sniffingJson')
    && hasBoolean(value, 'isDefault')
    && hasBoolean(value, 'isActive')
    && (value.isDefault !== true || value.isActive === true)
    && hasInteger(value, 'capacity', 1)
    && hasInteger(value, 'usedCapacity', 0)
    && (value.usedCapacity as number) <= (value.capacity as number)
}

function isVpnClientDto(value: unknown): value is VpnClientDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'userId', true)
    && hasString(value, 'subscriptionId', true)
    && hasString(value, 'vpnPanelId', true)
    && hasString(value, 'vpnInboundId', true)
    && hasString(value, 'externalClientId', true)
    && hasString(value, 'email', true)
    && hasString(value, 'uuid', true)
    && hasString(value, 'flow')
    && hasInteger(value, 'limitIp', 0)
    && hasNullableInteger(value, 'totalGb', 0)
    && hasDateString(value, 'expiryTime')
    && hasBoolean(value, 'enable')
    && hasString(value, 'configUri')
    && hasString(value, 'qrCodePayload')
    && hasString(value, 'syncStatus', true)
    && hasNullableDateString(value, 'lastSyncedAt')
}

function isPanelSyncRunDto(value: unknown): value is PanelSyncRunDto {
  if (!isRecord(value)
    || !hasString(value, 'id', true)
    || !hasString(value, 'vpnPanelId', true)
    || !hasString(value, 'status', true)
    || !panelSyncRunStatusValues.has(value.status as string)
    || !hasDateString(value, 'startedAt')
    || !hasNullableDateString(value, 'finishedAt')
    || !hasJsonObjectString(value, 'summaryJson')
    || !hasString(value, 'errorMessage')) return false

  const isFinished = value.status === 'Succeeded' || value.status === 'Failed'
  return isFinished
    ? value.finishedAt !== null && Date.parse(value.finishedAt as string) >= Date.parse(value.startedAt as string)
    : value.finishedAt === null
}

function isPanelSyncEventDto(value: unknown): value is PanelSyncEventDto {
  return isRecord(value)
    && hasString(value, 'id', true)
    && hasString(value, 'panelSyncRunId', true)
    && hasString(value, 'eventType', true)
    && hasString(value, 'entityType', true)
    && hasNullableString(value, 'entityId')
    && hasString(value, 'externalId')
    && hasString(value, 'message', true)
    && hasJsonObjectString(value, 'payloadJson')
}

function isPanelHealthCheckDto(value: unknown): value is PanelHealthCheckDto {
  return isRecord(value)
    && hasString(value, 'id', true)
    && hasString(value, 'vpnPanelId', true)
    && hasString(value, 'status', true)
    && healthStatusValues.has(value.status as string)
    && hasNullableInteger(value, 'latencyMs', 0)
    && hasString(value, 'version')
    && hasString(value, 'errorMessage')
    && hasDateString(value, 'checkedAt')
}

function isVpnNodeDto(value: unknown): value is VpnNodeDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'name', true)
    && hasString(value, 'host')
    && hasString(value, 'ipAddress')
    && ((value.host as string).trim().length > 0 || (value.ipAddress as string).trim().length > 0)
    && hasString(value, 'provider')
    && hasString(value, 'region')
    && hasString(value, 'country')
    && hasString(value, 'datacenter')
    && hasString(value, 'status', true)
    && nodeStatusValues.has(value.status as string)
    && hasInteger(value, 'capacity', 1)
    && hasInteger(value, 'usedCapacity', 0)
    && (value.usedCapacity as number) <= (value.capacity as number)
    && hasString(value, 'supportedProtocolsCsv', true)
    && hasString(value, 'healthStatus', true)
    && healthStatusValues.has(value.healthStatus as string)
    && hasNullableDateString(value, 'lastHealthCheckAt')
    && hasNullableInteger(value, 'lastHealthLatencyMs', 0)
    && hasString(value, 'lastHealthError')
    && hasEmptyOrJsonObjectString(value, 'lastHealthMetadataJson')
    && hasString(value, 'provisioningStatus', true)
    && provisioningRunStatusValues.has(value.provisioningStatus as string)
    && hasProvisioningModeDescriptor(value, 'provisioningMode', 'provisioningModeTitle', 'provisioningRiskLevel', 'liveDeployAllowed', 'provisioningNextAction', 'provisioningOperatorWarning')
    && value.provisioningMode !== 'dry-run'
    && value.precheckMode === 'dry-run'
    && hasString(value, 'precheckModeTitle', true)
    && hasString(value, 'installedVersion')
    && hasString(value, 'backupStatus', true)
    && hasString(value, 'monitoringStatus', true)
    && hasString(value, 'loggingStatus', true)
    && hasString(value, 'tagsCsv')
    && hasInteger(value, 'priority', 0)
    && hasBoolean(value, 'isAvailableForNewUsers')
    && hasString(value, 'sshUser', true)
    && hasInteger(value, 'sshPort', 1)
    && (value.sshPort as number) <= 65535
    && hasString(value, 'sshAuthMethod', true)
    && hasBoolean(value, 'sshCredentialConfigured')
    && hasBoolean(value, 'skipHostKeyChecking')
    && hasString(value, 'panelBaseUrl')
    && hasString(value, 'panelUsername')
    && hasBoolean(value, 'panelPasswordConfigured')
    && hasNullableInteger(value, 'panelInboundId', 1)
    && hasString(value, 'publicHostname')
    && hasInteger(value, 'publicPort', 1)
    && (value.publicPort as number) <= 65535
    && hasNullableString(value, 'nodeGroupId')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
    && Date.parse(value.updatedAt as string) >= Date.parse(value.createdAt as string)
}

function isDeleteAdminServerResult(value: unknown): value is DeleteAdminServerResult {
  if (!isRecord(value)
    || !hasString(value, 'id', true)
    || !hasBoolean(value, 'deleted')
    || !hasBoolean(value, 'archived')) return false

  const countKeys: Array<keyof DeleteAdminServerResult> = [
    'linkedSubscriptions',
    'linkedAccesses',
    'linkedProvisioningRuns',
    'linkedHealthChecks',
    'linkedMigrationJobs'
  ]
  if (!countKeys.every((key) => hasInteger(value, key, 0))) return false

  const linkedCount = countKeys.reduce((total, key) => total + (value[key] as number), 0)
  return value.deleted !== value.archived
    && value.archived === (linkedCount > 0)
}

function isNodeHealthCheckDto(value: unknown): value is NodeHealthCheckDto {
  return isRecord(value)
    && hasString(value, 'id', true)
    && hasString(value, 'nodeId', true)
    && hasString(value, 'status', true)
    && healthStatusValues.has(value.status as string)
    && hasDateString(value, 'checkedAt')
    && hasInteger(value, 'latencyMs', 0)
    && hasJsonObjectString(value, 'metadataJson')
    && hasString(value, 'errorText')
}

function isProvisioningRunBase(value: unknown): value is Record<string, unknown> {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'nodeId', true)
    && hasString(value, 'nodeName')
    && hasString(value, 'targetHost')
    && hasInteger(value, 'sshPort', 0)
    && (value.sshPort as number) <= 65535
    && hasString(value, 'username')
    && hasString(value, 'authMethod')
    && hasBoolean(value, 'credentialsConfigured')
    && hasString(value, 'source')
    && hasString(value, 'owner')
    && hasBoolean(value, 'validationMode')
    && hasProvisioningModeDescriptor(value, 'mode', 'modeTitle', 'riskLevel', 'liveDeployAllowed', 'nextAction', 'operatorWarning')
    && hasProvisioningModeDescriptor(value, 'deployMode', 'deployModeTitle', 'deployRiskLevel', 'deployLiveDeployAllowed', 'deployNextAction', 'deployOperatorWarning')
    && hasString(value, 'status', true)
    && provisioningRunStatusValues.has(value.status as string)
    && hasString(value, 'currentStep', true)
    && hasNullableString(value, 'requestedByUserId')
    && hasBoolean(value, 'dryRun')
    && ((value.dryRun === true && value.mode === 'dry-run') || (value.dryRun === false && value.mode !== 'dry-run'))
    && hasInteger(value, 'attemptCount', 0)
    && hasNullableDateString(value, 'processingStartedAt')
    && hasNullableDateString(value, 'leaseExpiresAt')
    && hasString(value, 'lastError')
    && hasDateString(value, 'startedAt')
    && hasNullableDateString(value, 'finishedAt')
    && (value.finishedAt === null || Date.parse(value.finishedAt as string) >= Date.parse(value.startedAt as string))
    && hasString(value, 'errorSummary')
    && hasString(value, 'executionLog')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
    && Date.parse(value.updatedAt as string) >= Date.parse(value.createdAt as string)
}

function isProvisioningRunDto(value: unknown): value is ProvisioningRunDto {
  return isProvisioningRunBase(value)
    && hasString(value, 'executionLogPreview')
    && hasString(value, 'precheckReportPreview')
}

function isProvisioningStepDto(value: unknown) {
  return isRecord(value)
    && hasString(value, 'id', true)
    && hasString(value, 'provisioningRunId', true)
    && hasString(value, 'stepName', true)
    && hasString(value, 'status', true)
    && provisioningRunStatusValues.has(value.status as string)
    && hasDateString(value, 'startedAt')
    && hasNullableDateString(value, 'finishedAt')
    && (value.finishedAt === null || Date.parse(value.finishedAt as string) >= Date.parse(value.startedAt as string))
    && hasString(value, 'output')
    && hasString(value, 'errorText')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
    && Date.parse(value.updatedAt as string) >= Date.parse(value.createdAt as string)
}

function isProvisioningRunDetailsDto(value: unknown): value is ProvisioningRunDetailsDto {
  if (!isRecord(value)) return false
  const run = value.run
  if (!isProvisioningRunBase(run)
    || !hasString(run, 'precheckReport')
    || !hasNullableString(run, 'linkedAccessId')
    || !Array.isArray(value.steps)
    || !value.steps.every(isProvisioningStepDto)
    || !hasUniqueStringKey(value.steps, 'id')) return false

  return value.steps.every((step) => step.provisioningRunId === run.id)
}

function isProvisioningCommandResponse(value: unknown): value is ProvisioningCommandResponse {
  return isRecord(value)
    && hasString(value, 'serverId', true)
    && hasString(value, 'runId', true)
    && hasString(value, 'status', true)
    && provisioningCommandStatusValues.has(value.status as string)
    && hasBoolean(value, 'dryRun')
    && hasProvisioningModeDescriptor(value, 'mode', 'modeTitle', 'riskLevel', 'liveDeployAllowed', 'nextAction', 'operatorWarning')
    && ((value.dryRun === true && value.mode === 'dry-run') || (value.dryRun === false && value.mode !== 'dry-run'))
}

function isProvisioningCancelResult(value: unknown): value is { runId: string; status: string } {
  return isRecord(value)
    && hasString(value, 'runId', true)
    && value.status === 'cancelled'
}

function isProvisioningSupportResult(value: unknown): value is { runId: string; supportConversationId: string } {
  return isRecord(value)
    && hasString(value, 'runId', true)
    && hasString(value, 'supportConversationId', true)
}

function isAdminTelegramBotSettingsDto(value: unknown): value is AdminTelegramBotSettingsDto {
  if (!isRecord(value)) return false

  return hasBoolean(value, 'enabled')
    && hasString(value, 'mode', true)
    && telegramBotModeValues.has(value.mode as string)
    && hasString(value, 'publicBotUsername')
    && hasBoolean(value, 'hasBotToken')
    && hasString(value, 'botTokenMasked')
    && ((value.hasBotToken === true && (value.botTokenMasked as string).length > 0) || (value.hasBotToken === false && value.botTokenMasked === ''))
    && hasString(value, 'webhookUrl')
    && hasBoolean(value, 'hasSecretToken')
    && hasString(value, 'adminChatId')
    && hasString(value, 'webAppUrl')
    && hasString(value, 'welcomeText')
    && hasString(value, 'instructionText')
    && hasString(value, 'supportText')
    && hasString(value, 'afterPaymentTextTemplate')
    && hasString(value, 'renewalTextTemplate')
    && hasString(value, 'paymentFailedTextTemplate')
    && hasString(value, 'subscriptionExpiredTextTemplate')
    && hasDateString(value, 'generatedAt')
}

function isAdminTelegramBotConnectionCheckDto(value: unknown): value is AdminTelegramBotConnectionCheckDto {
  if (!isRecord(value)
    || !hasBoolean(value, 'isReady')
    || !hasString(value, 'status', true)
    || !telegramBotCheckStatusValues.has(value.status as string)
    || !hasStringArray(value, 'requiredActions', true)
    || !hasStringArray(value, 'warnings', true)
    || !hasDateString(value, 'checkedAt')) return false

  const requiredActions = value.requiredActions as string[]
  const warnings = value.warnings as string[]
  return new Set(requiredActions).size === requiredActions.length
    && new Set(warnings).size === warnings.length
    && value.isReady === (value.status === 'ready')
    && value.isReady === (requiredActions.length === 0)
}

function isAdminTelegramAccountDto(value: unknown): value is AdminTelegramAccountDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasInteger(value, 'telegramUserId', 1)
    && hasString(value, 'username')
    && hasString(value, 'firstName')
    && hasString(value, 'lastName')
    && hasString(value, 'languageCode')
    && hasBoolean(value, 'isBlocked')
    && hasNullableDateString(value, 'linkedAt')
    && hasNullableDateString(value, 'lastSeenAt')
    && hasNullableDateString(value, 'registrationCompletedAt')
}

function isAdminOverviewSubscriptionDto(value: unknown): value is SubscriptionDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'userId', true)
    && hasString(value, 'tariffId', true)
    && hasNullableString(value, 'tariffName')
    && hasString(value, 'status', true)
    && subscriptionStatusValues.has(value.status as string)
    && hasDateString(value, 'startAt')
    && hasDateString(value, 'endAt')
    && hasNullableDateString(value, 'gracePeriodEndAt')
    && hasBoolean(value, 'autoRenewFlag')
    && hasString(value, 'sourceChannel', true)
    && channelTypeValues.has(value.sourceChannel as ChannelType)
    && hasNullableString(value, 'currentServerId')
    && hasNullableString(value, 'currentAccessId')
    && hasNullableString(value, 'lastPaymentId')
    && hasInteger(value, 'renewalCount', 0)
    && hasNullableString(value, 'blockReason')
    && hasNullableDateString(value, 'suspendedAt')
    && hasNullableDateString(value, 'cancelledAt')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isAdminOverviewAccessDto(value: unknown): value is AccessCredentialDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'subscriptionId', true)
    && hasString(value, 'subscriptionStatus', true)
    && subscriptionStatusValues.has(value.subscriptionStatus as string)
    && hasBoolean(value, 'isTerminal')
    && hasNullableString(value, 'userId')
    && hasString(value, 'providerType', true)
    && hasString(value, 'providerAccessId')
    && hasString(value, 'serverId', true)
    && hasNullableString(value, 'serverName')
    && hasString(value, 'accessUri')
    && hasNullableString(value, 'qrCodePayload')
    && hasString(value, 'qrCodePath')
    && hasString(value, 'configPath')
    && hasString(value, 'status', true)
    && accessCredentialStatusValues.has(value.status as string)
    && hasDateString(value, 'issuedAt')
    && hasNullableDateString(value, 'disabledAt')
    && hasNullableDateString(value, 'lastSyncedAt')
    && hasInteger(value, 'revision', 0)
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isAdminUserOverviewDto(value: unknown): value is AdminUserOverviewDto {
  if (!isRecord(value)
    || !isAdminUserDto(value.user)
    || !Array.isArray(value.telegramAccounts)
    || !Array.isArray(value.orders)
    || !Array.isArray(value.payments)
    || !Array.isArray(value.subscriptions)
    || !Array.isArray(value.accessCredentials)
    || !Array.isArray(value.supportConversations)) return false

  return value.telegramAccounts.every(isAdminTelegramAccountDto)
    && value.orders.every(isCabinetOrderDto)
    && value.payments.every(isCabinetPaymentAttemptDto)
    && value.subscriptions.every(isAdminOverviewSubscriptionDto)
    && value.accessCredentials.every(isAdminOverviewAccessDto)
    && value.supportConversations.every(isSupportConversationDto)
    && hasUniqueStringKey(value.telegramAccounts, 'id')
    && hasUniqueStringKey(value.orders, 'id')
    && hasUniqueStringKey(value.payments, 'id')
    && hasUniqueStringKey(value.subscriptions, 'id')
    && hasUniqueStringKey(value.accessCredentials, 'id')
    && hasUniqueStringKey(value.supportConversations, 'id')
}

function isUserProfileDto(value: unknown): value is UserProfileDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasNullableString(value, 'email')
    && hasString(value, 'displayName', true)
    && hasString(value, 'preferredLanguage', true)
    && hasString(value, 'referralCode', true)
    && hasString(value, 'status', true)
    && userStatusValues.has(value.status as string)
}

function isAuthResponse(value: unknown): value is AuthResponse {
  return isRecord(value)
    && hasString(value, 'accessToken', true)
    && hasString(value, 'refreshToken', true)
    && value.accessToken !== value.refreshToken
    && hasString(value, 'email', true)
    && isValidEmail(value.email as string)
    && hasString(value, 'displayName')
}

function isForgotPasswordResponse(value: unknown): value is ForgotPasswordResponse {
  return isRecord(value)
    && value.accepted === true
    && hasString(value, 'message', true)
    && hasNullableString(value, 'validationResetToken')
    && (value.validationResetToken === null || (value.validationResetToken as string).trim().length > 0)
}

function isCheckoutSessionDto(value: unknown): value is CheckoutSessionDto {
  if (!isRecord(value)
    || !hasString(value, 'id', true)
    || !hasString(value, 'token', true)
    || !hasString(value, 'tariffId', true)
    || !hasNullableString(value, 'userId')
    || !hasNullableString(value, 'orderId')
    || !hasString(value, 'status', true)
    || !checkoutSessionStatusValues.has(value.status as string)
    || !hasDateString(value, 'expiresAt')
    || !hasNullableString(value, 'emailHint')) return false

  if (value.status === 'open' || value.status === 'expired') {
    return value.userId === null && value.orderId === null
  }
  if (value.status === 'claiming') {
    return typeof value.userId === 'string' && value.userId.trim().length > 0 && value.orderId === null
  }
  return typeof value.userId === 'string'
    && value.userId.trim().length > 0
    && typeof value.orderId === 'string'
    && value.orderId.trim().length > 0
}

function isOrderCommandDto(value: unknown): value is OrderDto {
  return isRecord(value)
    && hasString(value, 'id', true)
    && hasString(value, 'userId', true)
    && hasString(value, 'tariffId', true)
    && hasFiniteNumber(value, 'amount', 0)
    && hasString(value, 'currency', true)
    && hasString(value, 'status', true)
    && orderStatusValues.has(value.status as string)
    && hasDateString(value, 'expiresAt')
    && hasNullableString(value, 'linkedSubscriptionId')
}

function isPaymentInitResult(value: unknown): value is PaymentInitResult {
  return isRecord(value)
    && hasString(value, 'paymentId', true)
    && hasSafeAbsoluteHttpUrl(value, 'redirectUrl')
    && hasString(value, 'rawResponse')
}

function isSupportMutationResult(value: unknown): value is { conversationId: string; status: string; revision: number } {
  return isRecord(value)
    && hasString(value, 'conversationId', true)
    && hasString(value, 'status', true)
    && supportStatusValues.has(value.status as string)
    && hasInteger(value, 'revision', 1)
}

function isTelegramLinkTokenDto(value: unknown): value is TelegramLinkTokenDto {
  if (!isRecord(value)
    || !hasString(value, 'token', true)
    || !hasSafeAbsoluteHttpUrl(value, 'deepLinkUrl')
    || !hasDateString(value, 'expiresAt')) return false

  const url = new URL(value.deepLinkUrl as string)
  return url.protocol === 'https:'
    && url.hostname.toLowerCase() === 't.me'
    && url.searchParams.get('start') === `link_${value.token}`
}

function isCabinetSubscriptionDto(value: unknown): value is SubscriptionDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'userId', true)
    && hasString(value, 'tariffId', true)
    && hasString(value, 'status', true)
    && subscriptionStatusValues.has(value.status as string)
    && hasDateString(value, 'startAt')
    && hasDateString(value, 'endAt')
    && hasNullableString(value, 'tariffName')
    && hasNullableDateString(value, 'gracePeriodEndAt')
    && hasBoolean(value, 'autoRenewFlag')
    && hasString(value, 'sourceChannel', true)
    && channelTypeValues.has(value.sourceChannel as ChannelType)
    && hasNullableString(value, 'currentServerId')
    && hasNullableString(value, 'currentAccessId')
    && hasNullableString(value, 'lastPaymentId')
    && hasInteger(value, 'renewalCount', 0)
    && hasNullableString(value, 'blockReason')
    && hasNullableDateString(value, 'suspendedAt')
    && hasNullableDateString(value, 'cancelledAt')
    && hasNullableString(value, 'accessUri')
    && hasNullableString(value, 'qrCodePath')
    && hasNullableString(value, 'configPath')
    && hasNullableString(value, 'nodeName')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isCabinetOrderDto(value: unknown): value is OrderDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'userId', true)
    && hasString(value, 'tariffId', true)
    && hasNullableString(value, 'tariffName')
    && hasFiniteNumber(value, 'amount', 0)
    && hasString(value, 'currency', true)
    && hasString(value, 'status', true)
    && orderStatusValues.has(value.status as string)
    && hasString(value, 'type', true)
    && orderTypeValues.has(value.type as OrderType)
    && hasString(value, 'channel', true)
    && channelTypeValues.has(value.channel as ChannelType)
    && hasString(value, 'paymentProvider', true)
    && paymentProviderValues.has(value.paymentProvider as PaymentProvider)
    && hasNullableString(value, 'checkoutSessionId')
    && hasDateString(value, 'expiresAt')
    && hasNullableDateString(value, 'paidAt')
    && hasBoolean(value, 'isFirstPurchase')
    && hasInteger(value, 'paymentAttemptsCount', 0)
    && hasNullableString(value, 'linkedSubscriptionId')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isCabinetPaymentAttemptDto(value: unknown): value is PaymentAttemptDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'orderId', true)
    && hasNullableString(value, 'userId')
    && hasString(value, 'provider', true)
    && paymentProviderValues.has(value.provider as PaymentProvider)
    && hasNullableString(value, 'paymentProviderAccountId')
    && hasString(value, 'providerMode', true)
    && paymentProviderModeValues.has(value.providerMode as PaymentProviderMode)
    && hasString(value, 'providerPaymentId')
    && hasString(value, 'externalEventId')
    && hasNullableString(value, 'idempotencyKey')
    && hasNullableString(value, 'confirmationUrl')
    && hasNullableString(value, 'returnUrl')
    && hasFiniteNumber(value, 'amount', 0)
    && hasString(value, 'currency', true)
    && hasString(value, 'status', true)
    && paymentStatusValues.has(value.status as string)
    && hasBoolean(value, 'signatureValidated')
    && hasBoolean(value, 'isActivationProcessed')
    && hasNullableDateString(value, 'activationProcessedAt')
    && hasNullableDateString(value, 'paidAt')
    && hasNullableDateString(value, 'failedAt')
    && hasNullableDateString(value, 'refundedAt')
    && hasFiniteNumber(value, 'refundedAmount', 0)
    && hasNullableString(value, 'statusReason')
    && hasInteger(value, 'webhookEventsCount', 0)
    && hasInteger(value, 'refundsCount', 0)
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isCabinetAccessCredentialDto(value: unknown): value is AccessCredentialDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'subscriptionId', true)
    && hasString(value, 'subscriptionStatus', true)
    && subscriptionStatusValues.has(value.subscriptionStatus as string)
    && hasBoolean(value, 'isTerminal')
    && hasNullableString(value, 'userId')
    && hasString(value, 'providerType', true)
    && hasString(value, 'providerAccessId')
    && hasString(value, 'serverId', true)
    && hasNullableString(value, 'serverName')
    && hasString(value, 'accessUri')
    && hasNullableString(value, 'qrCodePayload')
    && hasString(value, 'qrCodePath')
    && hasString(value, 'configPath')
    && hasString(value, 'status', true)
    && accessCredentialStatusValues.has(value.status as string)
    && hasDateString(value, 'issuedAt')
    && hasNullableDateString(value, 'expiryDate')
    && hasNullableDateString(value, 'disabledAt')
    && hasNullableDateString(value, 'lastSyncedAt')
    && hasInteger(value, 'revision', 0)
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isRewardLedgerDto(value: unknown): value is RewardLedgerDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'type', true)
    && hasString(value, 'status', true)
    && rewardStatusValues.has(value.status as string)
    && hasFiniteNumber(value, 'value')
    && hasString(value, 'currencyOrUnit', true)
    && hasNullableDateString(value, 'processedAt')
    && hasDateString(value, 'createdAt')
}

function isSupportConversationDto(value: unknown): value is SupportConversationDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasNullableString(value, 'userId')
    && hasNullableInteger(value, 'telegramUserId', 1)
    && hasString(value, 'channel', true)
    && supportChannelValues.has(value.channel as string)
    && hasString(value, 'status', true)
    && supportStatusValues.has(value.status as string)
    && hasString(value, 'subject', true)
    && hasNullableString(value, 'assignedToUserId')
    && hasString(value, 'internalNote')
    && hasInteger(value, 'revision', 0)
    && hasNullableDateString(value, 'closedAt')
    && hasDateString(value, 'createdAt')
    && hasDateString(value, 'updatedAt')
}

function isSupportMessageDto(value: unknown): value is SupportMessageDto {
  if (!isRecord(value)) return false

  return hasString(value, 'id', true)
    && hasString(value, 'supportConversationId', true)
    && hasNullableString(value, 'userId')
    && hasNullableInteger(value, 'telegramUserId', 1)
    && hasString(value, 'direction', true)
    && supportDirectionValues.has(value.direction as string)
    && hasString(value, 'text', true)
    && hasString(value, 'attachmentsJson')
    && hasBoolean(value, 'isInternalNote')
    && value.isInternalNote === false
    && hasDateString(value, 'createdAt')
}

function isTelegramStatusDto(value: unknown): value is TelegramStatusDto {
  if (!isRecord(value) || !hasBoolean(value, 'isLinked') || !hasNullableString(value, 'username')) return false
  if (value.isLinked) {
    return hasInteger(value, 'telegramUserId', 1) && hasDateString(value, 'linkedAt')
  }
  return value.telegramUserId === null && value.linkedAt === null && value.username === null
}

function isJsonContentType(value: string | null) {
  const mediaType = value?.split(';', 1)[0]?.trim().toLowerCase() ?? ''
  return mediaType === 'application/json' || /^application\/[a-z0-9!#$&^_.+-]+\+json$/.test(mediaType)
}

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
  private readonly requestTimeoutMs: number

  constructor(private readonly baseUrl: string, requestTimeoutMs = defaultApiRequestTimeoutMs) {
    this.requestTimeoutMs = Number.isFinite(requestTimeoutMs) && requestTimeoutMs > 0
      ? requestTimeoutMs
      : defaultApiRequestTimeoutMs
  }

  private async fetchWithTimeout<T>(path: string, init: RequestInit, readResponse: (response: Response) => Promise<T>): Promise<T> {
    const controller = new AbortController()
    const externalSignal = init.signal
    let timeoutReached = false
    const forwardExternalAbort = () => controller.abort(externalSignal?.reason)

    if (externalSignal?.aborted) {
      forwardExternalAbort()
    } else {
      externalSignal?.addEventListener('abort', forwardExternalAbort, { once: true })
    }

    const timeoutId = setTimeout(() => {
      timeoutReached = true
      controller.abort()
    }, this.requestTimeoutMs)

    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        ...init,
        signal: controller.signal
      })
      return await readResponse(response)
    } catch (error) {
      if (timeoutReached) {
        throw new ApiClientError(apiRequestTimeoutMessage, 408, null)
      }
      throw error
    } finally {
      clearTimeout(timeoutId)
      externalSignal?.removeEventListener('abort', forwardExternalAbort)
    }
  }

  private async cancelResponseBody(response: Response): Promise<void> {
    try {
      await response.body?.cancel()
    } catch {
      // Keep the response contract violation as the caller-visible error.
    }
  }

  private async readResponseText(response: Response, maxBytes: number): Promise<string> {
    const declaredLength = response.headers.get('Content-Length')?.trim() ?? ''
    if (/^\d+$/.test(declaredLength) && Number(declaredLength) > maxBytes) {
      await this.cancelResponseBody(response)
      throw new ApiClientError(apiOversizedResponseMessage, 502, { maxBytes })
    }

    if (!response.body) return ''

    const reader = response.body.getReader()
    const chunks: Uint8Array[] = []
    let totalBytes = 0

    try {
      while (true) {
        const { done, value } = await reader.read()
        if (done) break
        if (!value) continue

        totalBytes += value.byteLength
        if (totalBytes > maxBytes) {
          try {
            await reader.cancel()
          } catch {
            // Keep the size violation as the caller-visible error.
          }
          throw new ApiClientError(apiOversizedResponseMessage, 502, { maxBytes })
        }
        chunks.push(value)
      }
    } finally {
      reader.releaseLock()
    }

    const bytes = new Uint8Array(totalBytes)
    let offset = 0
    for (const chunk of chunks) {
      bytes.set(chunk, offset)
      offset += chunk.byteLength
    }
    return new TextDecoder().decode(bytes)
  }

  private async request<T>(path: string, init?: RequestInit & { token?: string | null; errorMessage?: string }, expectedShape: 'object' | 'array' = 'object', responseValidator?: (payload: unknown) => boolean): Promise<T> {
    const { token, errorMessage, ...requestInit } = init ?? {}
    const headers = new Headers(requestInit.headers ?? {})

    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }

    if (requestInit.body && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }

    return this.fetchWithTimeout(path, { ...requestInit, headers }, async (response) => {
      if (!response.ok) {
        const text = await this.readResponseText(response, defaultApiErrorResponseMaxBytes)
        const payload = text ? (() => { try { return JSON.parse(text) } catch { return text } })() : null
        throw new ApiClientError(normalizeApiError(payload, errorMessage ?? apiFallbackErrorMessage), response.status, payload)
      }

      const contentType = response.headers.get('Content-Type')
      if (!isJsonContentType(contentType)) {
        await this.cancelResponseBody(response)
        throw new ApiClientError(apiUnsupportedResponseMessage, 502, { contentType })
      }
      const text = await this.readResponseText(response, defaultJsonResponseMaxBytes)
      if (!text.trim()) {
        throw new ApiClientError(apiEmptyResponseMessage, 502, null)
      }

      let payload: unknown
      try {
        payload = JSON.parse(text)
      } catch {
        throw new ApiClientError(apiInvalidJsonResponseMessage, 502, null)
      }

      if (typeof payload !== 'object' || payload === null) {
        throw new ApiClientError(apiInvalidJsonResponseMessage, 502, null)
      }
      if ((expectedShape === 'array') !== Array.isArray(payload)) {
        throw new ApiClientError(apiUnexpectedResponseShapeMessage, 502, { expectedShape })
      }
      if (responseValidator && !responseValidator(payload)) {
        throw new ApiClientError(apiInvalidResponseDataMessage, 502, { expectedShape })
      }

      return payload as T
    })
  }

  private requestArray<T>(path: string, init?: RequestInit & { token?: string | null; errorMessage?: string }, itemValidator?: (item: unknown) => item is T, collectionValidator?: (items: T[]) => boolean): Promise<T[]> {
    return this.request<T[]>(
      path,
      init,
      'array',
      itemValidator || collectionValidator
        ? (payload) => Array.isArray(payload)
          && (!itemValidator || payload.every((item) => itemValidator(item)))
          && (!collectionValidator || collectionValidator(payload as T[]))
        : undefined
    )
  }


  private async requestText(path: string, init?: RequestInit & { token?: string | null; errorMessage?: string; expectedContentType?: string; maxBytes?: number }): Promise<string> {
    const { token, errorMessage, expectedContentType, maxBytes = 1_000_000, ...requestInit } = init ?? {}
    const headers = new Headers(requestInit.headers ?? {})

    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }

    return this.fetchWithTimeout(path, { ...requestInit, headers }, async (response) => {
      if (!response.ok) {
        const text = await this.readResponseText(response, defaultApiErrorResponseMaxBytes)
        const payload = text ? (() => { try { return JSON.parse(text) } catch { return text } })() : null
        throw new ApiClientError(normalizeApiError(payload, errorMessage ?? apiFallbackErrorMessage), response.status, payload)
      }

      const contentType = response.headers.get('Content-Type')?.split(';', 1)[0]?.trim().toLowerCase() ?? ''
      if (expectedContentType && contentType !== expectedContentType.toLowerCase()) {
        await this.cancelResponseBody(response)
        throw new ApiClientError('QR-код пришел в неподдерживаемом формате.', 502, { contentType })
      }
      const text = await this.readResponseText(response, maxBytes)
      if (!text.trim()) {
        throw new ApiClientError('QR-код пустой.', 502, null)
      }

      return text
    })
  }

  getTariffs(): Promise<TariffDto[]> {
    return this.requestArray<TariffDto>(
      '/api/public/tariffs',
      { errorMessage: apiFallbackErrorMessage },
      isTariffDto,
      (items) => hasUniqueStringKey(items, 'id') && hasUniqueStringKey(items, 'slug')
    )
  }

  getFaq(): Promise<FaqItem[]> {
    return this.requestArray<FaqItem>('/api/public/content/faq', { errorMessage: apiFallbackErrorMessage }, isFaqItem, (items) => hasUniqueStringKey(items, 'id'))
  }

  getHomeFaq(): Promise<FaqItem[]> {
    return this.requestArray<FaqItem>('/api/public/content/faq?home=true', { errorMessage: apiFallbackErrorMessage }, isFaqItem, (items) => hasUniqueStringKey(items, 'id'))
  }

  getHomeContent(): Promise<SiteContentBlockDto[]> {
    return this.requestArray<SiteContentBlockDto>(
      '/api/public/content/home',
      { errorMessage: apiFallbackErrorMessage },
      isSiteContentBlockDto,
      (items) => hasUniqueStringKey(items, 'id') && hasUniqueStringKey(items, 'key')
    )
  }

  getPublicPaymentProviders(): Promise<PublicPaymentProviderDto[]> {
    return this.requestArray<PublicPaymentProviderDto>(
      '/api/public/payments/providers',
      { errorMessage: apiFallbackErrorMessage },
      isPublicPaymentProviderDto,
      (items) => hasUniqueStringKey(items, 'provider')
    )
  }

  register(email: string, password: string, displayName: string, referralCode?: string | null): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, displayName, referralCode: referralCode?.trim() || null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAuthResponse)
  }

  login(email: string, password: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAuthResponse)
  }

  refresh(refreshToken: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAuthResponse)
  }

  logout(token?: string | null, refreshToken?: string | null): Promise<{ status: string }> {
    return this.request<{ status: string }>('/api/auth/logout', {
      method: 'POST',
      token,
      body: JSON.stringify({ refreshToken: refreshToken ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is { status: string } => isRecord(value) && value.status === 'ok')
  }

  forgotPassword(email: string): Promise<ForgotPasswordResponse> {
    return this.request<ForgotPasswordResponse>('/api/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify({ email }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isForgotPasswordResponse)
  }

  resetPassword(resetToken: string, newPassword: string): Promise<{ status: string }> {
    return this.request<{ status: string }>('/api/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify({ token: resetToken, newPassword }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is { status: string } => isRecord(value) && value.status === 'password_changed')
  }

  getMe(token: string): Promise<UserProfileDto> {
    return this.request<UserProfileDto>('/api/me', { token, errorMessage: apiFallbackErrorMessage }, 'object', isUserProfileDto)
  }

  createCheckoutSession(payload: CreateCheckoutSessionPayload): Promise<CheckoutSessionDto> {
    return this.request<CheckoutSessionDto>('/api/public/checkout-sessions', {
      method: 'POST',
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is CheckoutSessionDto => isCheckoutSessionDto(value) && value.status === 'open')
  }

  getCheckoutSession(token: string): Promise<CheckoutSessionDto> {
    return this.request<CheckoutSessionDto>(`/api/public/checkout-sessions/${encodeURIComponent(token)}`, {
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is CheckoutSessionDto => isCheckoutSessionDto(value) && value.token === token)
  }

  claimCheckoutSession(token: string, checkoutToken: string): Promise<OrderDto> {
    return this.request<OrderDto>(`/api/me/checkout-sessions/${encodeURIComponent(checkoutToken)}/claim`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isOrderCommandDto)
  }

  createMyOrder(token: string, payload: CreateMyOrderPayload): Promise<OrderDto> {
    return this.request<OrderDto>('/api/me/orders', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isOrderCommandDto)
  }

  initMyPayment(token: string, orderId: string, provider: PaymentProvider, returnUrl?: string | null): Promise<PaymentInitResult> {
    return this.request<PaymentInitResult>(`/api/me/orders/${orderId}/payments/${provider}/init`, {
      method: 'POST',
      token,
      body: JSON.stringify({ returnUrl: returnUrl ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isPaymentInitResult)
  }

  getMySubscriptions(token: string): Promise<SubscriptionDto[]> {
    return this.requestArray<SubscriptionDto>('/api/me/subscriptions', { token, errorMessage: apiFallbackErrorMessage }, isCabinetSubscriptionDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getMyOrders(token: string): Promise<OrderDto[]> {
    return this.requestArray<OrderDto>('/api/me/orders', { token, errorMessage: apiFallbackErrorMessage }, isCabinetOrderDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getMyPayments(token: string): Promise<PaymentAttemptDto[]> {
    return this.requestArray<PaymentAttemptDto>('/api/me/payments', { token, errorMessage: apiFallbackErrorMessage }, isCabinetPaymentAttemptDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getMyPayment(token: string, paymentId: string): Promise<PaymentAttemptDto> {
    return this.request<PaymentAttemptDto>(`/api/me/payments/${paymentId}`, { token, errorMessage: apiFallbackErrorMessage }, 'object', isCabinetPaymentAttemptDto)
  }

  getMySupportConversations(token: string): Promise<SupportConversationDto[]> {
    return this.requestArray<SupportConversationDto>('/api/me/support/conversations', { token, errorMessage: apiFallbackErrorMessage }, isSupportConversationDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getMySupportMessages(token: string, conversationId: string): Promise<SupportMessageDto[]> {
    return this.requestArray<SupportMessageDto>(`/api/me/support/conversations/${conversationId}/messages`, { token, errorMessage: apiFallbackErrorMessage }, isSupportMessageDto, (items) => hasUniqueStringKey(items, 'id'))
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
    }, 'object', isSupportConversationDto)
  }

  replyMySupportConversation(token: string, conversationId: string, text: string, revision: number): Promise<SupportMessageDto> {
    return this.request<SupportMessageDto>(`/api/me/support/conversations/${conversationId}/reply`, {
      method: 'POST',
      token,
      body: JSON.stringify({ text, revision }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isSupportMessageDto)
  }

  updateMySupportConversationStatus(token: string, conversationId: string, status: 'open' | 'closed', revision: number): Promise<{ conversationId: string; status: string; revision: number }> {
    return this.request<{ conversationId: string; status: string; revision: number }>(`/api/me/support/conversations/${conversationId}/status`, {
      method: 'PATCH',
      token,
      body: JSON.stringify({ status, revision }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is { conversationId: string; status: string; revision: number } => isSupportMutationResult(value)
      && value.conversationId === conversationId
      && value.status === status)
  }


  createTelegramLinkToken(token: string): Promise<TelegramLinkTokenDto> {
    return this.request<TelegramLinkTokenDto>('/api/me/telegram/link-token', {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isTelegramLinkTokenDto)
  }

  getTelegramStatus(token: string): Promise<TelegramStatusDto> {
    return this.request<TelegramStatusDto>('/api/me/telegram/status', { token, errorMessage: apiFallbackErrorMessage }, 'object', isTelegramStatusDto)
  }

  unlinkTelegram(token: string): Promise<TelegramStatusDto> {
    return this.request<TelegramStatusDto>('/api/me/telegram/unlink', {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', isTelegramStatusDto)
  }

  getMyAccesses(token: string): Promise<AccessCredentialDto[]> {
    return this.requestArray<AccessCredentialDto>('/api/me/accesses', { token, errorMessage: apiFallbackErrorMessage }, isCabinetAccessCredentialDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getMyAccessQrSvg(token: string, id: string): Promise<string> {
    return this.requestText(`/api/cabinet/access/${id}/qr`, { token, errorMessage: apiFallbackErrorMessage, expectedContentType: 'image/svg+xml', maxBytes: 1_000_000 })
  }

  getMyReferrals(token: string): Promise<RewardLedgerDto[]> {
    return this.requestArray<RewardLedgerDto>('/api/me/referrals', { token, errorMessage: apiFallbackErrorMessage }, isRewardLedgerDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getLatestAppVersion(token: string): Promise<AppVersionLatestResponse> {
    return this.request<AppVersionLatestResponse>('/api/app-version/latest', { token, errorMessage: apiFallbackErrorMessage }, 'object', isAppVersionLatestResponse)
  }

  getAppVersionHistory(token: string): Promise<AppReleaseDto[]> {
    return this.requestArray<AppReleaseDto>('/api/app-version/history', { token, errorMessage: apiFallbackErrorMessage }, isAppReleaseDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  markAppVersionSeen(token: string, releaseId: string): Promise<{ releaseId: string; seen: boolean }> {
    return this.request<{ releaseId: string; seen: boolean }>('/api/app-version/mark-seen', {
      method: 'POST',
      token,
      body: JSON.stringify({ releaseId }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAppReleaseSeenResult)
  }

  getAdminDashboardSummary(token: string): Promise<AdminDashboardSummaryDto> {
    return this.request<AdminDashboardSummaryDto>('/api/admin/dashboard/summary', { token, errorMessage: apiFallbackErrorMessage }, 'object', isAdminDashboardSummaryDto)
  }

  getAdminSession(token: string): Promise<AdminSessionDto> {
    return this.request<AdminSessionDto>('/api/admin/session', { token, errorMessage: apiFallbackErrorMessage }, 'object', isAdminSessionDto)
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
    return this.requestArray<AdminAuditLogDto>(`/api/admin/audit-logs${query ? `?${query}` : ''}`, { token, errorMessage: apiFallbackErrorMessage }, isAdminAuditLogDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminNotificationDeliveries(token: string): Promise<AdminNotificationDeliveryDto[]> {
    return this.requestArray<AdminNotificationDeliveryDto>('/api/admin/notification-deliveries?limit=100', { token, errorMessage: apiFallbackErrorMessage }, isAdminNotificationDeliveryDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  retryAdminNotificationDelivery(token: string, deliveryId: string): Promise<{ id: string; status: string; nextAttemptAt?: string | null }> {
    return this.request<{ id: string; status: string; nextAttemptAt?: string | null }>(`/api/admin/notification-deliveries/${deliveryId}/retry`, {
      method: 'POST',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAdminNotificationRetryResult)
  }

  getAdminUsers(token: string, filters?: { search?: string; status?: string; role?: string }): Promise<AdminUserDto[]> {
    const params = new URLSearchParams()
    if (filters?.search) params.set('search', filters.search)
    if (filters?.status) params.set('status', filters.status)
    if (filters?.role) params.set('role', filters.role)
    const suffix = params.toString() ? `?${params.toString()}` : ''
    return this.requestArray<AdminUserDto>(`/api/admin/users${suffix}`, { token, errorMessage: apiFallbackErrorMessage }, isAdminUserDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminUserOverview(token: string, userId: string): Promise<AdminUserOverviewDto> {
    return this.request<AdminUserOverviewDto>(`/api/admin/users/${userId}/overview`, { token, errorMessage: apiFallbackErrorMessage }, 'object', isAdminUserOverviewDto)
  }

  getAdminSubscriptions(token: string): Promise<SubscriptionDto[]> {
    return this.requestArray<SubscriptionDto>('/api/admin/subscriptions', { token, errorMessage: apiFallbackErrorMessage }, isAdminSubscriptionDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  extendAdminSubscription(token: string, id: string, days: number, reason?: string | null): Promise<AdminSubscriptionExtendResultDto> {
    return this.request<AdminSubscriptionExtendResultDto>(`/api/admin/subscriptions/${id}/extend`, {
      method: 'POST',
      token,
      body: JSON.stringify({ days, reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is AdminSubscriptionExtendResultDto => isAdminSubscriptionExtendResultDto(value, id))
  }

  activateAdminSubscription(token: string, id: string, reason?: string | null): Promise<AdminSubscriptionActivateResultDto> {
    return this.request<AdminSubscriptionActivateResultDto>(`/api/admin/subscriptions/${id}/activate`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is AdminSubscriptionActivateResultDto => isAdminSubscriptionActivateResultDto(value, id))
  }

  blockAdminSubscription(token: string, id: string, reason?: string | null): Promise<AdminSubscriptionBlockResultDto> {
    return this.request<AdminSubscriptionBlockResultDto>(`/api/admin/subscriptions/${id}/block`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: apiFallbackErrorMessage }, 'object', (value): value is AdminSubscriptionBlockResultDto => isAdminSubscriptionBlockResultDto(value, id))
  }

  unblockAdminSubscription(token: string, id: string, reason?: string | null): Promise<AdminSubscriptionStatusResultDto> {
    return this.request<AdminSubscriptionStatusResultDto>(`/api/admin/subscriptions/${id}/unblock`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: apiFallbackErrorMessage }, 'object', (value): value is AdminSubscriptionStatusResultDto => isAdminSubscriptionStatusResultDto(value, id, new Set(['Active', 'Expired'])))
  }

  cancelAdminSubscription(token: string, id: string, reason?: string | null): Promise<AdminSubscriptionCancelResultDto> {
    return this.request<AdminSubscriptionCancelResultDto>(`/api/admin/subscriptions/${id}/cancel`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: apiFallbackErrorMessage }, 'object', (value): value is AdminSubscriptionCancelResultDto => isAdminSubscriptionCancelResultDto(value, id))
  }

  syncAdminSubscriptionAccess(token: string, id: string, reason?: string | null): Promise<AdminSubscriptionAccessSyncResultDto> {
    return this.request<AdminSubscriptionAccessSyncResultDto>(`/api/admin/subscriptions/${id}/sync-access`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is AdminSubscriptionAccessSyncResultDto => isAdminSubscriptionAccessSyncResultDto(value, id))
  }

  migrateAdminSubscription(token: string, id: string, targetNodeId: string | null): Promise<AdminSubscriptionMigrationResultDto> {
    return this.request<AdminSubscriptionMigrationResultDto>(`/api/admin/subscriptions/${id}/migrate`, {
      method: 'POST',
      token,
      body: JSON.stringify(targetNodeId),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is AdminSubscriptionMigrationResultDto => isAdminSubscriptionMigrationResultDto(value, id, targetNodeId))
  }

  getAdminAccesses(token: string): Promise<AccessCredentialDto[]> {
    return this.requestArray<AccessCredentialDto>('/api/admin/access-credentials', { token, errorMessage: apiFallbackErrorMessage }, isAdminAccessCredentialDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminAccessQrSvg(token: string, id: string): Promise<string> {
    return this.requestText(`/api/admin/access-credentials/${id}/qr`, { token, errorMessage: apiFallbackErrorMessage, expectedContentType: 'image/svg+xml', maxBytes: 1_000_000 })
  }

  enableAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/enable`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is AccessActionResultDto => isAccessActionResultDto(value, id))
  }

  disableAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/disable`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is AccessActionResultDto => isAccessActionResultDto(value, id))
  }


  syncAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/sync`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is AccessActionResultDto => isAccessActionResultDto(value, id))
  }

  resetAdminAccessTraffic(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/reset-traffic`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is AccessActionResultDto => isAccessActionResultDto(value, id))
  }

  getAdminOrders(token: string, filters: AdminOrderFilters = {}): Promise<OrderDto[]> {
    const params = new URLSearchParams()
    if (filters.status) params.set('status', filters.status)
    if (filters.search) params.set('search', filters.search)
    const query = params.toString()
    return this.requestArray<OrderDto>(`/api/admin/orders${query ? `?${query}` : ''}`, { token, errorMessage: apiFallbackErrorMessage }, isCabinetOrderDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminPayments(token: string): Promise<PaymentAttemptDto[]> {
    return this.requestArray<PaymentAttemptDto>('/api/admin/payments', { token, errorMessage: apiFallbackErrorMessage }, isAdminPaymentAttemptDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  recheckAdminPayment(token: string, paymentId: string): Promise<PaymentStatusResultDto> {
    return this.request<PaymentStatusResultDto>(`/api/admin/payments/${paymentId}/recheck`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isPaymentStatusResultDto)
  }

  recheckAdminOrderPayment(token: string, orderId: string): Promise<PaymentStatusResultDto> {
    return this.request<PaymentStatusResultDto>(`/api/admin/orders/${orderId}/recheck-payment`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isPaymentStatusResultDto)
  }

  refundAdminPayment(token: string, paymentId: string, amount: number, reason?: string): Promise<RefundDto> {
    return this.request<RefundDto>(`/api/admin/payments/${paymentId}/refund`, {
      method: 'POST',
      token,
      body: JSON.stringify({ amount, reason: reason ?? null }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isRefundDto)
  }

  getAdminPaymentProviderAccounts(token: string): Promise<PaymentProviderAccountDto[]> {
    return this.requestArray<PaymentProviderAccountDto>('/api/admin/payment-providers/accounts', { token, errorMessage: apiFallbackErrorMessage }, isPaymentProviderAccountDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  createAdminPaymentProviderAccount(token: string, payload: UpsertPaymentProviderAccountPayload): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>('/api/admin/payment-providers/accounts', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isPaymentProviderAccountDto)
  }

  updateAdminPaymentProviderAccount(token: string, id: string, payload: UpsertPaymentProviderAccountPayload): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>(`/api/admin/payment-providers/accounts/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isPaymentProviderAccountDto)
  }

  setAdminPaymentProviderAccountEnabled(token: string, id: string, enabled: boolean): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>(`/api/admin/payment-providers/accounts/${id}/enabled`, {
      method: 'POST',
      token,
      body: JSON.stringify({ enabled }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isPaymentProviderAccountDto)
  }

  checkAdminPaymentProviderAccount(token: string, id: string): Promise<PaymentProviderAccountCheckResultDto> {
    return this.request<PaymentProviderAccountCheckResultDto>(`/api/admin/payment-providers/accounts/${id}/check`, {
      method: 'POST',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', isPaymentProviderAccountCheckResultDto)
  }

  getAdminPaymentWebhookEvents(token: string): Promise<PaymentWebhookEventDto[]> {
    return this.requestArray<PaymentWebhookEventDto>('/api/admin/payment-webhook-events', { token, errorMessage: apiFallbackErrorMessage }, isPaymentWebhookEventDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminRefunds(token: string): Promise<RefundDto[]> {
    return this.requestArray<RefundDto>('/api/admin/refunds', { token, errorMessage: apiFallbackErrorMessage }, isRefundDto, (items) => hasUniqueStringKey(items, 'id'))
  }


  getAdminSupportConversations(token: string): Promise<SupportConversationDto[]> {
    return this.requestArray<SupportConversationDto>('/api/admin/support/conversations', { token, errorMessage: apiFallbackErrorMessage }, isSupportConversationDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminSupportMessages(token: string, conversationId: string): Promise<SupportMessageDto[]> {
    return this.requestArray<SupportMessageDto>(`/api/admin/support/conversations/${conversationId}/messages`, { token, errorMessage: apiFallbackErrorMessage }, isAdminSupportMessageDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  replyAdminSupportConversation(token: string, conversationId: string, text: string, revision: number): Promise<{ conversationId: string; status: string; revision: number }> {
    return this.request<{ conversationId: string; status: string; revision: number }>(`/api/admin/support/conversations/${conversationId}/reply`, {
      method: 'POST',
      token,
      body: JSON.stringify({ text, revision }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAdminSupportMutationResult)
  }

  updateAdminSupportConversationStatus(token: string, conversationId: string, status: string, revision: number, assignedToUserId?: string | null): Promise<{ conversationId: string; status: string; revision: number }> {
    return this.request<{ conversationId: string; status: string; revision: number }>(`/api/admin/support/conversations/${conversationId}/status`, {
      method: 'PATCH',
      token,
      body: JSON.stringify({ status, assignedToUserId: assignedToUserId ?? null, revision }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAdminSupportMutationResult)
  }

  addAdminSupportInternalNote(token: string, conversationId: string, text: string, revision: number): Promise<SupportMessageDto> {
    return this.request<SupportMessageDto>(`/api/admin/support/conversations/${conversationId}/notes`, {
      method: 'POST',
      token,
      body: JSON.stringify({ text, revision }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAdminSupportMessageDto)
  }

  getAdminVpnPanels(token: string): Promise<VpnPanelDto[]> {
    return this.requestArray<VpnPanelDto>('/api/admin/vpn-panels', { token, errorMessage: apiFallbackErrorMessage }, isVpnPanelDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  createAdminVpnPanel(token: string, payload: CreateVpnPanelPayload): Promise<VpnPanelDto> {
    return this.request<VpnPanelDto>('/api/admin/vpn-panels', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isVpnPanelDto)
  }

  updateAdminVpnPanel(token: string, id: string, payload: UpdateVpnPanelPayload): Promise<VpnPanelDto> {
    return this.request<VpnPanelDto>(`/api/admin/vpn-panels/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnPanelDto => isVpnPanelDto(value) && value.id === id)
  }

  deleteAdminVpnPanel(token: string, id: string): Promise<DeleteVpnPanelResult> {
    return this.request<DeleteVpnPanelResult>(`/api/admin/vpn-panels/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is DeleteVpnPanelResult => isDeleteVpnPanelResult(value) && value.id === id)
  }

  testAdminVpnPanel(token: string, id: string): Promise<PanelHealthCheckDto> {
    return this.request<PanelHealthCheckDto>(`/api/admin/vpn-panels/${id}/test-connection`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is PanelHealthCheckDto => isPanelHealthCheckDto(value) && value.vpnPanelId === id)
  }

  syncAdminVpnPanel(token: string, id: string): Promise<PanelSyncRunDto> {
    return this.request<PanelSyncRunDto>(`/api/admin/vpn-panels/${id}/sync`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is PanelSyncRunDto => isPanelSyncRunDto(value) && value.vpnPanelId === id)
  }

  getAdminVpnPanelInbounds(token: string, id: string): Promise<VpnInboundDto[]> {
    return this.requestArray<VpnInboundDto>(`/api/admin/vpn-panels/${id}/inbounds`, { token, errorMessage: apiFallbackErrorMessage }, isVpnInboundDto, (items) => hasUniqueStringKey(items, 'id')
      && hasUniqueStringKey(items, 'externalInboundId')
      && items.every((item) => item.vpnPanelId === id)
      && items.filter((item) => item.isDefault).length <= 1)
  }

  getAdminVpnInbounds(token: string): Promise<VpnInboundDto[]> {
    return this.requestArray<VpnInboundDto>('/api/admin/vpn-inbounds', { token, errorMessage: apiFallbackErrorMessage }, isVpnInboundDto, (items) => {
      const externalIds = new Set<string>()
      const defaultPanels = new Set<string>()
      return hasUniqueStringKey(items, 'id') && items.every((item) => {
        const externalId = `${item.vpnPanelId}\u0000${item.externalInboundId}`
        if (externalIds.has(externalId) || (item.isDefault && defaultPanels.has(item.vpnPanelId))) return false
        externalIds.add(externalId)
        if (item.isDefault) defaultPanels.add(item.vpnPanelId)
        return true
      })
    })
  }

  createAdminVpnPanelInbound(token: string, id: string, payload: CreateVpnInboundPayload): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-panels/${id}/inbounds`, {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnInboundDto => isVpnInboundDto(value) && value.vpnPanelId === id)
  }

  setAdminVpnInboundDefault(token: string, id: string): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-inbounds/${id}/set-default`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnInboundDto => isVpnInboundDto(value) && value.id === id)
  }

  updateAdminVpnInbound(token: string, id: string, payload: CreateVpnInboundPayload): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-inbounds/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnInboundDto => isVpnInboundDto(value) && value.id === id)
  }

  getAdminVpnPanelClients(token: string, id: string): Promise<VpnClientDto[]> {
    return this.requestArray<VpnClientDto>(`/api/admin/vpn-panels/${id}/clients`, { token, errorMessage: apiFallbackErrorMessage }, isVpnClientDto, (items) => hasUniqueStringKey(items, 'id')
      && hasUniqueStringKey(items, 'externalClientId')
      && items.every((item) => item.vpnPanelId === id))
  }

  enableAdminVpnClient(token: string, id: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/enable`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnClientDto => isVpnClientDto(value) && value.id === id)
  }

  disableAdminVpnClient(token: string, id: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/disable`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnClientDto => isVpnClientDto(value) && value.id === id)
  }

  syncAdminVpnClient(token: string, id: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/sync`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnClientDto => isVpnClientDto(value) && value.id === id)
  }

  resetAdminVpnClientTraffic(token: string, id: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/reset-traffic`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnClientDto => isVpnClientDto(value) && value.id === id)
  }

  migrateAdminVpnClient(token: string, id: string, targetInboundId: string): Promise<VpnClientDto> {
    return this.request<VpnClientDto>(`/api/admin/vpn-clients/${id}/migrate`, {
      method: 'POST',
      token,
      body: JSON.stringify({ targetInboundId }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnClientDto => isVpnClientDto(value) && value.id === id)
  }

  getAdminVpnPanelSyncRuns(token: string, id: string): Promise<PanelSyncRunDto[]> {
    return this.requestArray<PanelSyncRunDto>(`/api/admin/vpn-panels/${id}/sync-runs`, { token, errorMessage: apiFallbackErrorMessage }, isPanelSyncRunDto, (items) => hasUniqueStringKey(items, 'id')
      && items.every((item) => item.vpnPanelId === id))
  }

  getAdminVpnPanelSyncEvents(token: string, runId: string): Promise<PanelSyncEventDto[]> {
    return this.requestArray<PanelSyncEventDto>(`/api/admin/vpn-panel-sync-runs/${runId}/events`, { token, errorMessage: apiFallbackErrorMessage }, isPanelSyncEventDto, (items) => hasUniqueStringKey(items, 'id')
      && items.every((item) => item.panelSyncRunId === runId))
  }

  getAdminVpnPanelHealthChecks(token: string, id: string): Promise<PanelHealthCheckDto[]> {
    return this.requestArray<PanelHealthCheckDto>(`/api/admin/vpn-panels/${id}/health-checks`, { token, errorMessage: apiFallbackErrorMessage }, isPanelHealthCheckDto, (items) => hasUniqueStringKey(items, 'id')
      && items.every((item) => item.vpnPanelId === id))
  }

  getAdminTariffs(token: string): Promise<TariffDto[]> {
    return this.requestArray<TariffDto>('/api/admin/tariffs', { token, errorMessage: apiFallbackErrorMessage }, isTariffDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminReferralPrograms(token: string): Promise<AdminReferralProgramDto[]> {
    return this.requestArray<AdminReferralProgramDto>('/api/admin/referral-programs', { token, errorMessage: apiFallbackErrorMessage }, isAdminReferralProgramDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminReferralRewards(token: string): Promise<AdminRewardLedgerDto[]> {
    return this.requestArray<AdminRewardLedgerDto>('/api/admin/referrals', { token, errorMessage: apiFallbackErrorMessage }, isAdminRewardLedgerDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminAppReleases(token: string, filters: AppReleaseFilters = {}): Promise<AppReleaseDto[]> {
    const params = new URLSearchParams()
    if (filters.visibility && filters.visibility !== 'all') params.set('visibility', filters.visibility)
    if (filters.source && filters.source !== 'all') params.set('source', filters.source)
    if (filters.search) params.set('search', filters.search)
    const query = params.toString()
    return this.requestArray<AppReleaseDto>(`/api/app-version/admin/releases${query ? `?${query}` : ''}`, { token, errorMessage: apiFallbackErrorMessage }, isAppReleaseDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminAppReleaseOverview(token: string): Promise<AppReleaseOverviewDto> {
    return this.request<AppReleaseOverviewDto>('/api/app-version/admin/releases/overview', { token, errorMessage: apiFallbackErrorMessage }, 'object', isAppReleaseOverviewDto)
  }

  getAdminFaq(token: string, filters: AdminFaqFilters = {}): Promise<FaqItem[]> {
    const params = new URLSearchParams()
    if (filters.category && filters.category !== 'all') params.set('category', filters.category)
    if (filters.visibility && filters.visibility !== 'all') params.set('visibility', filters.visibility)
    if (filters.search) params.set('search', filters.search)
    const query = params.toString()
    return this.requestArray<FaqItem>(`/api/admin/faq${query ? `?${query}` : ''}`, { token, errorMessage: apiFallbackErrorMessage }, isFaqItem, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminFaqOverview(token: string): Promise<FaqOverviewDto> {
    return this.request<FaqOverviewDto>('/api/admin/faq/overview', { token, errorMessage: apiFallbackErrorMessage }, 'object', isFaqOverviewDto)
  }

  getAdminSiteContent(token: string, group = 'home'): Promise<SiteContentBlockDto[]> {
    const suffix = group ? `?group=${encodeURIComponent(group)}` : ''
    return this.requestArray<SiteContentBlockDto>(`/api/admin/site-content${suffix}`, { token, errorMessage: apiFallbackErrorMessage }, isSiteContentBlockDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  getAdminHomeContentReadiness(token: string): Promise<SiteContentReadinessDto> {
    return this.request<SiteContentReadinessDto>('/api/admin/site-content/home-readiness', { token, errorMessage: apiFallbackErrorMessage }, 'object', isSiteContentReadinessDto)
  }

  restoreAdminHomeContentDefaults(token: string): Promise<SiteContentDefaultsResultDto> {
    return this.request<SiteContentDefaultsResultDto>('/api/admin/site-content/home-defaults', {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isSiteContentDefaultsResultDto)
  }

  createAdminSiteContent(token: string, payload: SiteContentBlockUpsertPayload): Promise<SiteContentBlockDto> {
    return this.request<SiteContentBlockDto>('/api/admin/site-content', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isSiteContentBlockDto)
  }

  updateAdminSiteContent(token: string, id: string, payload: SiteContentBlockUpsertPayload): Promise<SiteContentBlockDto> {
    return this.request<SiteContentBlockDto>(`/api/admin/site-content/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isSiteContentBlockDto)
  }

  deleteAdminSiteContent(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/site-content/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', isDeleteResultDto)
  }

  getAdminWorkScenarios(token: string): Promise<WorkScenarioDto[]> {
    return this.requestArray<WorkScenarioDto>('/api/admin/work-scenarios', { token, errorMessage: apiFallbackErrorMessage }, isWorkScenarioDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  createAdminWorkScenario(token: string, payload: WorkScenarioUpsertPayload): Promise<WorkScenarioDto> {
    return this.request<WorkScenarioDto>('/api/admin/work-scenarios', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isWorkScenarioDto)
  }

  updateAdminWorkScenario(token: string, id: string, payload: WorkScenarioUpsertPayload): Promise<WorkScenarioDto> {
    return this.request<WorkScenarioDto>(`/api/admin/work-scenarios/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isWorkScenarioDto)
  }

  deleteAdminWorkScenario(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/work-scenarios/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', isDeleteResultDto)
  }

  createAdminFaq(token: string, payload: FaqUpsertPayload): Promise<FaqItem> {
    return this.request<FaqItem>('/api/admin/faq', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isFaqItem)
  }

  updateAdminFaq(token: string, id: string, payload: FaqUpsertPayload): Promise<FaqItem> {
    return this.request<FaqItem>(`/api/admin/faq/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isFaqItem)
  }

  deleteAdminFaq(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/faq/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', isDeleteResultDto)
  }

  createAdminAppRelease(token: string, payload: AppReleaseUpsertPayload): Promise<AppReleaseDto> {
    return this.request<AppReleaseDto>('/api/app-version/admin/releases', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAppReleaseDto)
  }

  updateAdminAppRelease(token: string, id: string, payload: AppReleaseUpsertPayload): Promise<AppReleaseDto> {
    return this.request<AppReleaseDto>(`/api/app-version/admin/releases/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAppReleaseDto)
  }

  deleteAdminAppRelease(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/app-version/admin/releases/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', isDeleteResultDto)
  }

  createAdminTariff(token: string, payload: UpdateTariffPayload): Promise<TariffDto> {
    return this.request<TariffDto>('/api/admin/tariffs', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isTariffDto)
  }

  updateAdminTariff(token: string, id: string, payload: UpdateTariffPayload): Promise<TariffDto> {
    return this.request<TariffDto>(`/api/admin/tariffs/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isTariffDto)
  }

  deleteAdminTariff(token: string, id: string): Promise<{ id: string; deleted: boolean; archived?: boolean }> {
    return this.request<{ id: string; deleted: boolean; archived?: boolean }>(`/api/admin/tariffs/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', isTariffDeleteResultDto)
  }

  createAdminReferralProgram(token: string, payload: ReferralProgramUpsertPayload): Promise<AdminReferralProgramDto> {
    return this.request<AdminReferralProgramDto>('/api/admin/referral-programs', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAdminReferralProgramDto)
  }

  updateAdminReferralProgram(token: string, id: string, payload: ReferralProgramUpsertPayload): Promise<AdminReferralProgramDto> {
    return this.request<AdminReferralProgramDto>(`/api/admin/referral-programs/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAdminReferralProgramDto)
  }

  getAdminServers(token: string): Promise<VpnNodeDto[]> {
    return this.requestArray<VpnNodeDto>('/api/admin/servers', { token, errorMessage: apiFallbackErrorMessage }, isVpnNodeDto, (items) => hasUniqueStringKey(items, 'id'))
  }

  createAdminServer(token: string, payload: CreateServerPayload): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>('/api/admin/servers', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isVpnNodeDto)
  }

  updateAdminServer(token: string, serverId: string, payload: CreateServerPayload): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is VpnNodeDto => isVpnNodeDto(value) && value.id === serverId)
  }

  deleteAdminServer(token: string, serverId: string): Promise<DeleteAdminServerResult> {
    return this.request<DeleteAdminServerResult>(`/api/admin/servers/${serverId}`, {
      method: 'DELETE',
      token,
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is DeleteAdminServerResult => isDeleteAdminServerResult(value) && value.id === serverId)
  }

  disableAdminServer(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/disable`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage }, 'object', (value): value is VpnNodeDto => isVpnNodeDto(value) && value.id === serverId)
  }

  checkAdminServerHealth(token: string, serverId: string): Promise<NodeHealthCheckDto> {
    return this.request<NodeHealthCheckDto>(`/api/admin/servers/${serverId}/health-check`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is NodeHealthCheckDto => isNodeHealthCheckDto(value) && value.nodeId === serverId)
  }

  getAdminServerHealthChecks(token: string, serverId: string): Promise<NodeHealthCheckDto[]> {
    return this.requestArray<NodeHealthCheckDto>(`/api/admin/servers/${serverId}/health-checks`, { token, errorMessage: apiFallbackErrorMessage }, isNodeHealthCheckDto, (items) => hasUniqueStringKey(items, 'id')
      && items.every((item) => item.nodeId === serverId))
  }

  enableAdminServerAllocation(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/enable-allocation`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage }, 'object', (value): value is VpnNodeDto => isVpnNodeDto(value) && value.id === serverId)
  }

  disableAdminServerAllocation(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/disable-allocation`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage }, 'object', (value): value is VpnNodeDto => isVpnNodeDto(value) && value.id === serverId)
  }

  enableAdminServerMaintenance(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/maintenance`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage }, 'object', (value): value is VpnNodeDto => isVpnNodeDto(value) && value.id === serverId)
  }

  disableAdminServerMaintenance(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/disable-maintenance`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: apiFallbackErrorMessage }, 'object', (value): value is VpnNodeDto => isVpnNodeDto(value) && value.id === serverId)
  }

  precheckAdminServer(token: string, serverId: string): Promise<ProvisioningCommandResponse> {
    return this.request<ProvisioningCommandResponse>(`/api/admin/servers/${serverId}/precheck`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is ProvisioningCommandResponse => isProvisioningCommandResponse(value) && value.serverId === serverId && value.dryRun === true)
  }

  queueAdminProvision(token: string, serverId: string, dryRun = false): Promise<ProvisioningCommandResponse> {
    return this.request<ProvisioningCommandResponse>(`/api/admin/servers/${serverId}/provision`, {
      method: 'POST',
      token,
      body: JSON.stringify({ dryRun }),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is ProvisioningCommandResponse => isProvisioningCommandResponse(value) && value.serverId === serverId && value.dryRun === dryRun)
  }

  getAdminProvisioningRuns(token: string): Promise<ProvisioningRunDto[]> {
    return this.requestArray<ProvisioningRunDto>('/api/admin/provisioning-runs', { token, errorMessage: apiFallbackErrorMessage }, isProvisioningRunDto, (items) => hasUniqueStringKey(items, 'id'))
  }


  getAdminProvisioningRun(token: string, runId: string): Promise<ProvisioningRunDetailsDto> {
    return this.request<ProvisioningRunDetailsDto>(`/api/admin/provisioning-runs/${runId}`, { token, errorMessage: apiFallbackErrorMessage }, 'object', (value): value is ProvisioningRunDetailsDto => isProvisioningRunDetailsDto(value) && value.run.id === runId)
  }

  retryAdminProvisioningRun(token: string, runId: string): Promise<ProvisioningCommandResponse> {
    return this.request<ProvisioningCommandResponse>(`/api/admin/provisioning-runs/${runId}/retry`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is ProvisioningCommandResponse => isProvisioningCommandResponse(value) && value.runId === runId)
  }

  deployAdminProvisioningRun(token: string, runId: string): Promise<ProvisioningCommandResponse> {
    return this.request<ProvisioningCommandResponse>(`/api/admin/provisioning-runs/${runId}/deploy`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is ProvisioningCommandResponse => isProvisioningCommandResponse(value) && value.runId === runId && value.dryRun === false)
  }

  cancelAdminProvisioningRun(token: string, runId: string): Promise<{ runId: string; status: string }> {
    return this.request<{ runId: string; status: string }>(`/api/admin/provisioning-runs/${runId}/cancel`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is { runId: string; status: string } => isProvisioningCancelResult(value) && value.runId === runId)
  }

  markAdminProvisioningSupportNeeded(token: string, runId: string): Promise<{ runId: string; supportConversationId: string }> {
    return this.request<{ runId: string; supportConversationId: string }>(`/api/admin/provisioning-runs/${runId}/support-needed`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', (value): value is { runId: string; supportConversationId: string } => isProvisioningSupportResult(value) && value.runId === runId)
  }

  getAdminTelegramBotSettings(token: string): Promise<AdminTelegramBotSettingsDto> {
    return this.request<AdminTelegramBotSettingsDto>('/api/admin/telegram-bot/settings', { token, errorMessage: apiFallbackErrorMessage }, 'object', isAdminTelegramBotSettingsDto)
  }

  testAdminTelegramBotSettings(token: string): Promise<AdminTelegramBotConnectionCheckDto> {
    return this.request<AdminTelegramBotConnectionCheckDto>('/api/admin/telegram-bot/settings/test', {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAdminTelegramBotConnectionCheckDto)
  }

  updateAdminTelegramBotSettings(token: string, payload: UpdateTelegramBotSettingsPayload): Promise<AdminTelegramBotSettingsDto> {
    return this.request<AdminTelegramBotSettingsDto>('/api/admin/telegram-bot/settings', {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: apiFallbackErrorMessage
    }, 'object', isAdminTelegramBotSettingsDto)
  }
}
