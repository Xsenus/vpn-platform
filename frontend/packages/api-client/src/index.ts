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
  linkedSubscriptionId?: string | null
  createdAt?: string
  updatedAt?: string
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
  hasSecretKey: boolean
  hasWebhookSecret: boolean
  useWebhookIpAllowList: boolean
  allowedWebhookIpRangesCsv: string
  extraSettingsJson: string
  healthStatus: string
  isCheckoutConfigured?: boolean
  checkoutConfigurationIssue?: string | null
  capabilitiesJson?: string
  createdAt: string
  updatedAt: string
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
  secretKey?: string | null
  webhookSecret?: string | null
  useWebhookIpAllowList: boolean
  allowedWebhookIpRangesCsv: string
  extraSettingsJson: string
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
  provisioningStatus?: string
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
  currentStep?: string | null
  requestedByUserId?: string | null
  dryRun: boolean
  startedAt: string
  finishedAt?: string | null
  errorSummary?: string | null
  executionLog: string
  executionLogPreview?: string | null
  createdAt: string
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
  generatedAt: string
}

export type AdminTelegramBotSettingsDto = {
  enabled: boolean
  mode: string
  publicBotUsername: string
  hasBotToken: boolean
  botTokenMasked: string
  webhookUrl: string
  hasSecretToken: boolean
  welcomeText: string
  instructionText: string
  supportText: string
  afterPaymentTextTemplate: string
  generatedAt: string
}

export type UpdateTelegramBotSettingsPayload = {
  welcomeText?: string | null
  instructionText?: string | null
  supportText?: string | null
  afterPaymentTextTemplate?: string | null
}

export type AdminUserOverviewDto = {
  user: Record<string, unknown>
  telegramAccounts: Array<Record<string, unknown>>
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
}

export function buildAuthHeaders(token?: string | null): HeadersInit {
  return token ? { Authorization: `Bearer ${token}` } : {}
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
      throw new Error(normalizeApiError(payload, errorMessage ?? 'Request failed'))
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
      throw new Error(normalizeApiError(payload, errorMessage ?? 'Request failed'))
    }

    return text
  }

  getTariffs(): Promise<TariffDto[]> {
    return this.request<TariffDto[]>('/api/public/tariffs', { errorMessage: 'Failed to load tariffs' })
  }

  getFaq(): Promise<FaqItem[]> {
    return this.request<FaqItem[]>('/api/public/content/faq', { errorMessage: 'Failed to load faq' })
  }

  getHomeFaq(): Promise<FaqItem[]> {
    return this.request<FaqItem[]>('/api/public/content/faq?home=true', { errorMessage: 'Failed to load faq' })
  }

  getHomeContent(): Promise<SiteContentBlockDto[]> {
    return this.request<SiteContentBlockDto[]>('/api/public/content/home', { errorMessage: 'Failed to load home content' })
  }

  getPublicPaymentProviders(): Promise<PublicPaymentProviderDto[]> {
    return this.request<PublicPaymentProviderDto[]>('/api/public/payments/providers', { errorMessage: 'Failed to load payment providers' })
  }

  register(email: string, password: string, displayName: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, displayName }),
      errorMessage: 'Registration failed'
    })
  }

  login(email: string, password: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      errorMessage: 'Login failed'
    })
  }

  refresh(refreshToken: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
      errorMessage: 'Refresh failed'
    })
  }

  logout(token?: string | null, refreshToken?: string | null): Promise<{ status: string }> {
    return this.request<{ status: string }>('/api/auth/logout', {
      method: 'POST',
      token,
      body: JSON.stringify({ refreshToken: refreshToken ?? null }),
      errorMessage: 'Logout failed'
    })
  }

  forgotPassword(email: string): Promise<ForgotPasswordResponse> {
    return this.request<ForgotPasswordResponse>('/api/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify({ email }),
      errorMessage: 'Password reset request failed'
    })
  }

  resetPassword(resetToken: string, newPassword: string): Promise<{ status: string }> {
    return this.request<{ status: string }>('/api/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify({ token: resetToken, newPassword }),
      errorMessage: 'Password reset failed'
    })
  }

  getMe(token: string): Promise<UserProfileDto> {
    return this.request<UserProfileDto>('/api/me', { token, errorMessage: 'Failed to load profile' })
  }

  createCheckoutSession(payload: CreateCheckoutSessionPayload): Promise<CheckoutSessionDto> {
    return this.request<CheckoutSessionDto>('/api/public/checkout-sessions', {
      method: 'POST',
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create checkout session'
    })
  }

  getCheckoutSession(token: string): Promise<CheckoutSessionDto> {
    return this.request<CheckoutSessionDto>(`/api/public/checkout-sessions/${encodeURIComponent(token)}`, {
      errorMessage: 'Failed to load checkout session'
    })
  }

  claimCheckoutSession(token: string, checkoutToken: string): Promise<OrderDto> {
    return this.request<OrderDto>(`/api/me/checkout-sessions/${encodeURIComponent(checkoutToken)}/claim`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to claim checkout session'
    })
  }

  createPublicOrder(payload: CreatePublicOrderPayload): Promise<OrderDto> {
    return this.request<OrderDto>('/api/public/orders', {
      method: 'POST',
      body: JSON.stringify(payload),
      errorMessage: 'Anonymous public orders are disabled. Use checkout sessions.'
    })
  }

  initPayment(orderId: string, provider: PaymentProvider): Promise<PaymentInitResult> {
    return this.request<PaymentInitResult>(`/api/public/payments/${provider}/init`, {
      method: 'POST',
      body: JSON.stringify({ orderId }),
      errorMessage: 'Anonymous payment initialization is disabled. Claim checkout session first.'
    })
  }

  getOrderStatus(orderId: string): Promise<OrderDto> {
    return this.request<OrderDto>(`/api/public/orders/${orderId}/status`, { errorMessage: 'Failed to load order status' })
  }

  createMyOrder(token: string, payload: CreateMyOrderPayload): Promise<OrderDto> {
    return this.request<OrderDto>('/api/me/orders', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create order'
    })
  }

  initMyPayment(token: string, orderId: string, provider: PaymentProvider, returnUrl?: string | null): Promise<PaymentInitResult> {
    return this.request<PaymentInitResult>(`/api/me/orders/${orderId}/payments/${provider}/init`, {
      method: 'POST',
      token,
      body: JSON.stringify({ returnUrl: returnUrl ?? null }),
      errorMessage: 'Failed to initialize payment'
    })
  }

  getMySubscriptions(token: string): Promise<SubscriptionDto[]> {
    return this.request<SubscriptionDto[]>('/api/me/subscriptions', { token, errorMessage: 'Failed to load subscriptions' })
  }

  getMyOrders(token: string): Promise<OrderDto[]> {
    return this.request<OrderDto[]>('/api/me/orders', { token, errorMessage: 'Failed to load orders' })
  }

  getMyPayments(token: string): Promise<PaymentAttemptDto[]> {
    return this.request<PaymentAttemptDto[]>('/api/me/payments', { token, errorMessage: 'Failed to load payments' })
  }

  getMyPayment(token: string, paymentId: string): Promise<PaymentAttemptDto> {
    return this.request<PaymentAttemptDto>(`/api/me/payments/${paymentId}`, { token, errorMessage: 'Failed to load payment' })
  }


  createTelegramLinkToken(token: string): Promise<TelegramLinkTokenDto> {
    return this.request<TelegramLinkTokenDto>('/api/me/telegram/link-token', {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to create Telegram link token'
    })
  }

  getTelegramStatus(token: string): Promise<TelegramStatusDto> {
    return this.request<TelegramStatusDto>('/api/me/telegram/status', { token, errorMessage: 'Failed to load Telegram status' })
  }

  unlinkTelegram(token: string): Promise<TelegramStatusDto> {
    return this.request<TelegramStatusDto>('/api/me/telegram/unlink', {
      method: 'DELETE',
      token,
      errorMessage: 'Failed to unlink Telegram'
    })
  }

  getMyAccesses(token: string): Promise<AccessCredentialDto[]> {
    return this.request<AccessCredentialDto[]>('/api/me/accesses', { token, errorMessage: 'Failed to load accesses' })
  }

  getMyAccessQrSvg(token: string, id: string): Promise<string> {
    return this.requestText(`/api/cabinet/access/${id}/qr`, { token, errorMessage: 'Failed to load VPN QR code' })
  }

  getMyReferrals(token: string): Promise<RewardLedgerDto[]> {
    return this.request<RewardLedgerDto[]>('/api/me/referrals', { token, errorMessage: 'Failed to load referrals' })
  }

  getLatestAppVersion(token: string): Promise<AppVersionLatestResponse> {
    return this.request<AppVersionLatestResponse>('/api/app-version/latest', { token, errorMessage: 'Failed to load latest app version' })
  }

  getAppVersionHistory(token: string): Promise<AppReleaseDto[]> {
    return this.request<AppReleaseDto[]>('/api/app-version/history', { token, errorMessage: 'Failed to load app version history' })
  }

  markAppVersionSeen(token: string, releaseId: string): Promise<{ releaseId: string; seen: boolean }> {
    return this.request<{ releaseId: string; seen: boolean }>('/api/app-version/mark-seen', {
      method: 'POST',
      token,
      body: JSON.stringify({ releaseId }),
      errorMessage: 'Failed to mark app version as seen'
    })
  }

  getAdminDashboardSummary(token: string): Promise<AdminDashboardSummaryDto> {
    return this.request<AdminDashboardSummaryDto>('/api/admin/dashboard/summary', { token, errorMessage: 'Failed to load dashboard summary' })
  }

  getAdminUsers(token: string, filters?: { search?: string; status?: string; role?: string }): Promise<Array<Record<string, unknown>>> {
    const params = new URLSearchParams()
    if (filters?.search) params.set('search', filters.search)
    if (filters?.status) params.set('status', filters.status)
    if (filters?.role) params.set('role', filters.role)
    const suffix = params.toString() ? `?${params.toString()}` : ''
    return this.request<Array<Record<string, unknown>>>(`/api/admin/users${suffix}`, { token, errorMessage: 'Failed to load users' })
  }

  getAdminUserOverview(token: string, userId: string): Promise<AdminUserOverviewDto> {
    return this.request<AdminUserOverviewDto>(`/api/admin/users/${userId}/overview`, { token, errorMessage: 'Failed to load user overview' })
  }

  getAdminSubscriptions(token: string): Promise<SubscriptionDto[]> {
    return this.request<SubscriptionDto[]>('/api/admin/subscriptions', { token, errorMessage: 'Failed to load subscriptions' })
  }

  extendAdminSubscription(token: string, id: string, days: number, reason?: string | null): Promise<{ id: string; status: string; endAt: string }> {
    return this.request<{ id: string; status: string; endAt: string }>(`/api/admin/subscriptions/${id}/extend`, {
      method: 'POST',
      token,
      body: JSON.stringify({ days, reason: reason ?? null }),
      errorMessage: 'Failed to extend subscription'
    })
  }

  blockAdminSubscription(token: string, id: string, reason?: string | null): Promise<{ id: string; status: string }> {
    return this.request<{ id: string; status: string }>(`/api/admin/subscriptions/${id}/block`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: 'Failed to block subscription' })
  }

  unblockAdminSubscription(token: string, id: string, reason?: string | null): Promise<{ id: string; status: string }> {
    return this.request<{ id: string; status: string }>(`/api/admin/subscriptions/${id}/unblock`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: 'Failed to unblock subscription' })
  }

  cancelAdminSubscription(token: string, id: string, reason?: string | null): Promise<{ id: string; status: string }> {
    return this.request<{ id: string; status: string }>(`/api/admin/subscriptions/${id}/cancel`, { method: 'POST', token, body: JSON.stringify({ reason: reason ?? null }), errorMessage: 'Failed to cancel subscription' })
  }

  getAdminAccesses(token: string): Promise<AccessCredentialDto[]> {
    return this.request<AccessCredentialDto[]>('/api/admin/access-credentials', { token, errorMessage: 'Failed to load VPN access credentials' })
  }

  getAdminAccessQrSvg(token: string, id: string): Promise<string> {
    return this.requestText(`/api/admin/access-credentials/${id}/qr`, { token, errorMessage: 'Failed to load admin VPN QR code' })
  }

  enableAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/enable`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: 'Failed to enable VPN access'
    })
  }

  disableAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/disable`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: 'Failed to disable VPN access'
    })
  }


  syncAdminAccess(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/sync`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: 'Failed to sync VPN access'
    })
  }

  resetAdminAccessTraffic(token: string, id: string, reason?: string | null): Promise<AccessActionResultDto> {
    return this.request<AccessActionResultDto>(`/api/admin/access-credentials/${id}/reset-traffic`, {
      method: 'POST',
      token,
      body: JSON.stringify({ reason: reason ?? null }),
      errorMessage: 'Failed to reset VPN access traffic'
    })
  }

  getAdminOrders(token: string): Promise<OrderDto[]> {
    return this.request<OrderDto[]>('/api/admin/orders', { token, errorMessage: 'Failed to load orders' })
  }

  getAdminPayments(token: string): Promise<PaymentAttemptDto[]> {
    return this.request<PaymentAttemptDto[]>('/api/admin/payments', { token, errorMessage: 'Failed to load payments' })
  }

  recheckAdminPayment(token: string, paymentId: string): Promise<PaymentAttemptDto> {
    return this.request<PaymentAttemptDto>(`/api/admin/payments/${paymentId}/recheck`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to recheck payment'
    })
  }

  refundAdminPayment(token: string, paymentId: string, amount: number, reason?: string): Promise<RefundDto> {
    return this.request<RefundDto>(`/api/admin/payments/${paymentId}/refund`, {
      method: 'POST',
      token,
      body: JSON.stringify({ amount, reason: reason ?? null }),
      errorMessage: 'Failed to refund payment'
    })
  }

  getAdminPaymentProviderAccounts(token: string): Promise<PaymentProviderAccountDto[]> {
    return this.request<PaymentProviderAccountDto[]>('/api/admin/payment-providers/accounts', { token, errorMessage: 'Failed to load payment provider accounts' })
  }

  createAdminPaymentProviderAccount(token: string, payload: UpsertPaymentProviderAccountPayload): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>('/api/admin/payment-providers/accounts', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create payment provider account'
    })
  }

  updateAdminPaymentProviderAccount(token: string, id: string, payload: UpsertPaymentProviderAccountPayload): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>(`/api/admin/payment-providers/accounts/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update payment provider account'
    })
  }

  setAdminPaymentProviderAccountEnabled(token: string, id: string, enabled: boolean): Promise<PaymentProviderAccountDto> {
    return this.request<PaymentProviderAccountDto>(`/api/admin/payment-providers/accounts/${id}/enabled`, {
      method: 'POST',
      token,
      body: JSON.stringify({ enabled }),
      errorMessage: 'Failed to change payment provider account state'
    })
  }

  getAdminPaymentWebhookEvents(token: string): Promise<PaymentWebhookEventDto[]> {
    return this.request<PaymentWebhookEventDto[]>('/api/admin/payment-webhook-events', { token, errorMessage: 'Failed to load payment webhook events' })
  }

  getAdminRefunds(token: string): Promise<RefundDto[]> {
    return this.request<RefundDto[]>('/api/admin/refunds', { token, errorMessage: 'Failed to load refunds' })
  }


  getAdminSupportConversations(token: string): Promise<SupportConversationDto[]> {
    return this.request<SupportConversationDto[]>('/api/admin/support/conversations', { token, errorMessage: 'Failed to load support conversations' })
  }

  getAdminSupportMessages(token: string, conversationId: string): Promise<SupportMessageDto[]> {
    return this.request<SupportMessageDto[]>(`/api/admin/support/conversations/${conversationId}/messages`, { token, errorMessage: 'Failed to load support messages' })
  }

  replyAdminSupportConversation(token: string, conversationId: string, text: string): Promise<{ conversationId: string; status: string }> {
    return this.request<{ conversationId: string; status: string }>(`/api/admin/support/conversations/${conversationId}/reply`, {
      method: 'POST',
      token,
      body: JSON.stringify({ text }),
      errorMessage: 'Failed to send support reply'
    })
  }

  updateAdminSupportConversationStatus(token: string, conversationId: string, status: string, assignedToUserId?: string | null): Promise<{ conversationId: string; status: string }> {
    return this.request<{ conversationId: string; status: string }>(`/api/admin/support/conversations/${conversationId}/status`, {
      method: 'PATCH',
      token,
      body: JSON.stringify({ status, assignedToUserId: assignedToUserId ?? null }),
      errorMessage: 'Failed to update support status'
    })
  }

  addAdminSupportInternalNote(token: string, conversationId: string, text: string): Promise<SupportMessageDto> {
    return this.request<SupportMessageDto>(`/api/admin/support/conversations/${conversationId}/notes`, {
      method: 'POST',
      token,
      body: JSON.stringify({ text }),
      errorMessage: 'Failed to add support note'
    })
  }

  getAdminVpnPanels(token: string): Promise<VpnPanelDto[]> {
    return this.request<VpnPanelDto[]>('/api/admin/vpn-panels', { token, errorMessage: 'Failed to load VPN panels' })
  }

  createAdminVpnPanel(token: string, payload: CreateVpnPanelPayload): Promise<VpnPanelDto> {
    return this.request<VpnPanelDto>('/api/admin/vpn-panels', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create VPN panel'
    })
  }

  updateAdminVpnPanel(token: string, id: string, payload: UpdateVpnPanelPayload): Promise<VpnPanelDto> {
    return this.request<VpnPanelDto>(`/api/admin/vpn-panels/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update VPN panel'
    })
  }

  testAdminVpnPanel(token: string, id: string): Promise<PanelHealthCheckDto> {
    return this.request<PanelHealthCheckDto>(`/api/admin/vpn-panels/${id}/test-connection`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to test VPN panel'
    })
  }

  syncAdminVpnPanel(token: string, id: string): Promise<PanelSyncRunDto> {
    return this.request<PanelSyncRunDto>(`/api/admin/vpn-panels/${id}/sync`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to sync VPN panel'
    })
  }

  getAdminVpnPanelInbounds(token: string, id: string): Promise<VpnInboundDto[]> {
    return this.request<VpnInboundDto[]>(`/api/admin/vpn-panels/${id}/inbounds`, { token, errorMessage: 'Failed to load VPN inbounds' })
  }

  createAdminVpnPanelInbound(token: string, id: string, payload: CreateVpnInboundPayload): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-panels/${id}/inbounds`, {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create VPN inbound'
    })
  }

  setAdminVpnInboundDefault(token: string, id: string): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-inbounds/${id}/set-default`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to set default inbound'
    })
  }

  updateAdminVpnInbound(token: string, id: string, payload: CreateVpnInboundPayload): Promise<VpnInboundDto> {
    return this.request<VpnInboundDto>(`/api/admin/vpn-inbounds/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update VPN inbound'
    })
  }

  getAdminVpnPanelClients(token: string, id: string): Promise<VpnClientDto[]> {
    return this.request<VpnClientDto[]>(`/api/admin/vpn-panels/${id}/clients`, { token, errorMessage: 'Failed to load VPN clients' })
  }

  getAdminVpnPanelSyncRuns(token: string, id: string): Promise<PanelSyncRunDto[]> {
    return this.request<PanelSyncRunDto[]>(`/api/admin/vpn-panels/${id}/sync-runs`, { token, errorMessage: 'Failed to load panel sync runs' })
  }

  getAdminVpnPanelSyncEvents(token: string, runId: string): Promise<PanelSyncEventDto[]> {
    return this.request<PanelSyncEventDto[]>(`/api/admin/vpn-panel-sync-runs/${runId}/events`, { token, errorMessage: 'Failed to load panel sync events' })
  }

  getAdminVpnPanelHealthChecks(token: string, id: string): Promise<PanelHealthCheckDto[]> {
    return this.request<PanelHealthCheckDto[]>(`/api/admin/vpn-panels/${id}/health-checks`, { token, errorMessage: 'Failed to load panel health checks' })
  }

  getAdminTariffs(token: string): Promise<TariffDto[]> {
    return this.request<TariffDto[]>('/api/admin/tariffs', { token, errorMessage: 'Failed to load tariffs' })
  }

  getAdminAppReleases(token: string): Promise<AppReleaseDto[]> {
    return this.request<AppReleaseDto[]>('/api/app-version/admin/releases', { token, errorMessage: 'Failed to load app releases' })
  }

  getAdminFaq(token: string): Promise<FaqItem[]> {
    return this.request<FaqItem[]>('/api/admin/faq', { token, errorMessage: 'Failed to load FAQ' })
  }

  getAdminSiteContent(token: string, group = 'home'): Promise<SiteContentBlockDto[]> {
    const suffix = group ? `?group=${encodeURIComponent(group)}` : ''
    return this.request<SiteContentBlockDto[]>(`/api/admin/site-content${suffix}`, { token, errorMessage: 'Failed to load site content' })
  }

  createAdminSiteContent(token: string, payload: SiteContentBlockUpsertPayload): Promise<SiteContentBlockDto> {
    return this.request<SiteContentBlockDto>('/api/admin/site-content', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create site content block'
    })
  }

  updateAdminSiteContent(token: string, id: string, payload: SiteContentBlockUpsertPayload): Promise<SiteContentBlockDto> {
    return this.request<SiteContentBlockDto>(`/api/admin/site-content/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update site content block'
    })
  }

  deleteAdminSiteContent(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/site-content/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: 'Failed to delete site content block'
    })
  }

  getAdminWorkScenarios(token: string): Promise<WorkScenarioDto[]> {
    return this.request<WorkScenarioDto[]>('/api/admin/work-scenarios', { token, errorMessage: 'Failed to load work scenarios' })
  }

  createAdminWorkScenario(token: string, payload: WorkScenarioUpsertPayload): Promise<WorkScenarioDto> {
    return this.request<WorkScenarioDto>('/api/admin/work-scenarios', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create work scenario'
    })
  }

  updateAdminWorkScenario(token: string, id: string, payload: WorkScenarioUpsertPayload): Promise<WorkScenarioDto> {
    return this.request<WorkScenarioDto>(`/api/admin/work-scenarios/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update work scenario'
    })
  }

  deleteAdminWorkScenario(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/work-scenarios/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: 'Failed to delete work scenario'
    })
  }

  createAdminFaq(token: string, payload: FaqUpsertPayload): Promise<FaqItem> {
    return this.request<FaqItem>('/api/admin/faq', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create FAQ item'
    })
  }

  updateAdminFaq(token: string, id: string, payload: FaqUpsertPayload): Promise<FaqItem> {
    return this.request<FaqItem>(`/api/admin/faq/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update FAQ item'
    })
  }

  deleteAdminFaq(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/faq/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: 'Failed to delete FAQ item'
    })
  }

  createAdminAppRelease(token: string, payload: AppReleaseUpsertPayload): Promise<AppReleaseDto> {
    return this.request<AppReleaseDto>('/api/app-version/admin/releases', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create app release'
    })
  }

  updateAdminAppRelease(token: string, id: string, payload: AppReleaseUpsertPayload): Promise<AppReleaseDto> {
    return this.request<AppReleaseDto>(`/api/app-version/admin/releases/${id}`, {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update app release'
    })
  }

  deleteAdminAppRelease(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/app-version/admin/releases/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: 'Failed to delete app release'
    })
  }

  createAdminTariff(token: string, payload: UpdateTariffPayload): Promise<TariffDto> {
    return this.request<TariffDto>('/api/admin/tariffs', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create tariff'
    })
  }

  updateAdminTariff(token: string, id: string, payload: UpdateTariffPayload): Promise<TariffDto> {
    return this.request<TariffDto>(`/api/admin/tariffs/${id}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update tariff'
    })
  }

  deleteAdminTariff(token: string, id: string): Promise<{ id: string; deleted: boolean }> {
    return this.request<{ id: string; deleted: boolean }>(`/api/admin/tariffs/${id}`, {
      method: 'DELETE',
      token,
      errorMessage: 'Failed to delete tariff'
    })
  }

  getAdminServers(token: string): Promise<VpnNodeDto[]> {
    return this.request<VpnNodeDto[]>('/api/admin/servers', { token, errorMessage: 'Failed to load servers' })
  }

  createAdminServer(token: string, payload: CreateServerPayload): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>('/api/admin/servers', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to create server'
    })
  }

  enableAdminServerAllocation(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/enable-allocation`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: 'Failed to enable allocation' })
  }

  disableAdminServerAllocation(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/disable-allocation`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: 'Failed to disable allocation' })
  }

  enableAdminServerMaintenance(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/maintenance`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: 'Failed to enable maintenance' })
  }

  disableAdminServerMaintenance(token: string, serverId: string): Promise<VpnNodeDto> {
    return this.request<VpnNodeDto>(`/api/admin/servers/${serverId}/disable-maintenance`, { method: 'POST', token, body: JSON.stringify({}), errorMessage: 'Failed to disable maintenance' })
  }

  precheckAdminServer(token: string, serverId: string): Promise<{ serverId: string; runId: string; status: string; dryRun: boolean }> {
    return this.request<{ serverId: string; runId: string; status: string; dryRun: boolean }>(`/api/admin/servers/${serverId}/precheck`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to queue server precheck'
    })
  }

  queueAdminProvision(token: string, serverId: string, dryRun = false): Promise<{ serverId: string; runId: string; status: string; dryRun: boolean }> {
    return this.request<{ serverId: string; runId: string; status: string; dryRun: boolean }>(`/api/admin/servers/${serverId}/provision`, {
      method: 'POST',
      token,
      body: JSON.stringify({ dryRun }),
      errorMessage: 'Failed to queue provisioning run'
    })
  }

  getAdminProvisioningRuns(token: string): Promise<ProvisioningRunDto[]> {
    return this.request<ProvisioningRunDto[]>('/api/admin/provisioning-runs', { token, errorMessage: 'Failed to load provisioning runs' })
  }


  getAdminProvisioningRun(token: string, runId: string): Promise<ProvisioningRunDetailsDto> {
    return this.request<ProvisioningRunDetailsDto>(`/api/admin/provisioning-runs/${runId}`, { token, errorMessage: 'Failed to load provisioning run details' })
  }

  retryAdminProvisioningRun(token: string, runId: string): Promise<{ runId: string; status: string; dryRun: boolean }> {
    return this.request<{ runId: string; status: string; dryRun: boolean }>(`/api/admin/provisioning-runs/${runId}/retry`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to retry provisioning run'
    })
  }

  deployAdminProvisioningRun(token: string, runId: string): Promise<{ runId: string; status: string; dryRun: boolean }> {
    return this.request<{ runId: string; status: string; dryRun: boolean }>(`/api/admin/provisioning-runs/${runId}/deploy`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to queue provisioning deploy'
    })
  }

  cancelAdminProvisioningRun(token: string, runId: string): Promise<{ runId: string; status: string }> {
    return this.request<{ runId: string; status: string }>(`/api/admin/provisioning-runs/${runId}/cancel`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to cancel provisioning run'
    })
  }

  markAdminProvisioningSupportNeeded(token: string, runId: string): Promise<{ runId: string; supportConversationId: string }> {
    return this.request<{ runId: string; supportConversationId: string }>(`/api/admin/provisioning-runs/${runId}/support-needed`, {
      method: 'POST',
      token,
      body: JSON.stringify({}),
      errorMessage: 'Failed to mark provisioning run support-needed'
    })
  }

  getAdminTelegramBotSettings(token: string): Promise<AdminTelegramBotSettingsDto> {
    return this.request<AdminTelegramBotSettingsDto>('/api/admin/telegram-bot/settings', { token, errorMessage: 'Failed to load Telegram bot settings' })
  }

  updateAdminTelegramBotSettings(token: string, payload: UpdateTelegramBotSettingsPayload): Promise<AdminTelegramBotSettingsDto> {
    return this.request<AdminTelegramBotSettingsDto>('/api/admin/telegram-bot/settings', {
      method: 'PATCH',
      token,
      body: JSON.stringify(payload),
      errorMessage: 'Failed to update Telegram bot settings'
    })
  }
}
