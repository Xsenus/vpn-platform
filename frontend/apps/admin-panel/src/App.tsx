import React, { useEffect, useMemo, useState } from 'react'
import {
  AccessCredentialDto,
  AdminDashboardSummaryDto,
  AdminTelegramBotSettingsDto,
  AdminUserOverviewDto,
  ApiClient,
  AppReleaseDto,
  AppReleaseUpsertPayload,
  CreateServerPayload,
  CreateVpnInboundPayload,
  CreateVpnPanelPayload,
  FaqItem,
  FaqUpsertPayload,
  OrderDto,
  PaymentAttemptDto,
  PaymentProvider,
  PaymentProviderAccountCheckResultDto,
  PaymentProviderAccountDto,
  PaymentProviderMode,
  PaymentWebhookEventDto,
  ProvisioningRunDto,
  RefundDto,
  SiteContentBlockDto,
  SiteContentBlockUpsertPayload,
  SubscriptionDto,
  SupportConversationDto,
  SupportMessageDto,
  TariffDto,
  UpdateTariffPayload,
  UpdateTelegramBotSettingsPayload,
  UpsertPaymentProviderAccountPayload,
  VpnClientDto,
  VpnInboundDto,
  VpnNodeDto,
  VpnPanelDto,
  WorkScenarioDto,
  WorkScenarioUpsertPayload,
  PanelHealthCheckDto,
  PanelSyncRunDto
} from '@vpn-platform/api-client'
import { Card, CodeBlock, ConfirmButton, CopyButton, EmptyState, ErrorBlock, LoadingBlock, PageShell, PasswordField, PrimaryButton, SecretField, SectionCard, SkipLink, StatTile, StatusBadge, ValidationModeBadge } from '@vpn-platform/ui'

const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080')
const TOKEN_STORAGE_KEY = 'vpn-platform-admin-token'
const yookassaAllowedIps = '185.71.76.0/27,185.71.77.0/27,77.75.153.0/25,77.75.156.11,77.75.156.35,77.75.154.128/25,2a02:5180::/32'
const paymentProviderOptions: PaymentProvider[] = ['YooKassa', 'RoboKassa', 'YooMoney', 'TelegramStars', 'CloudPayments', 'TBankAcquiring', 'Prodamus', 'Stripe', 'PayPal']
const adminAuthRequiredMessage = 'Войдите как администратор, чтобы включить загрузку данных и действия в разделах.'

function readSessionStorageItem(key: string) {
  try {
    return typeof sessionStorage === 'undefined' ? null : sessionStorage.getItem(key)
  } catch {
    return null
  }
}

function writeSessionStorageItem(key: string, value: string) {
  try {
    if (typeof sessionStorage !== 'undefined') sessionStorage.setItem(key, value)
  } catch {
    // Private browsing and embedded environments can block Web Storage.
  }
}

function removeSessionStorageItem(key: string) {
  try {
    if (typeof sessionStorage !== 'undefined') sessionStorage.removeItem(key)
  } catch {
    // Storage cleanup is best-effort when browser storage is unavailable.
  }
}


const adminSections = [
  ['dashboard', 'Дашборд'],
  ['users', 'Пользователи'],
  ['payments', 'Оплаты'],
  ['tariffs', 'Тарифы'],
  ['subscriptions', 'Подписки'],
  ['vpn', 'VPN-доступы'],
  ['nodes', 'Серверы'],
  ['panels', '3x-ui панели'],
  ['support', 'Поддержка'],
  ['bot', 'Telegram-бот'],
  ['releases', 'Что нового'],
  ['faq', 'FAQ'],
  ['content', 'Контент сайта'],
  ['scenarios', 'Сценарии'],
  ['provisioning', 'Подготовка VPS']
] as const

type AdminSectionId = typeof adminSections[number][0]

function readAdminSectionFromHash(): AdminSectionId {
  if (typeof window === 'undefined') return adminSections[0][0]

  const section = window.location.hash.replace('#', '')
  return adminSections.some(([id]) => id === section) ? (section as AdminSectionId) : adminSections[0][0]
}

type GenericUser = Record<string, unknown>
type ServerFormState = CreateServerPayload
type LoadError = { area: string; message: string }

function readTagValue(tagsCsv: string | null | undefined, key: string): string | null {
  const normalizedKey = key.toLowerCase()
  for (const rawTag of (tagsCsv ?? '').split(',')) {
    const [rawKey, ...rawValue] = rawTag.split(':')
    if (rawKey?.trim().toLowerCase() === normalizedKey) return rawValue.join(':').trim()
  }

  return null
}

const defaultServerForm: ServerFormState = {
  name: '',
  host: '',
  ipAddress: '',
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
  sshPrivateKeyPath: '',
  sshAuthMethod: 'ssh_key',
  sshCredential: '',
  validationMode: true,
  ownerType: 'admin',
  skipHostKeyChecking: true,
  panelBaseUrl: '',
  panelUsername: 'admin',
  panelPassword: '',
  panelInboundId: 1,
  publicHostname: '',
  publicPort: 443,
  nodeGroupId: null
}

const defaultProviderForm: UpsertPaymentProviderAccountPayload = {
  provider: 'YooKassa',
  mode: 'Sandbox',
  name: 'yookassa-sandbox',
  publicName: 'YooKassa',
  isEnabled: true,
  isDefault: true,
  shopId: '',
  apiBaseUrl: 'https://api.yookassa.ru/v3',
  returnUrl: '',
  webhookUrl: '',
  secretKey: '',
  webhookSecret: '',
  useWebhookIpAllowList: true,
  allowedWebhookIpRangesCsv: yookassaAllowedIps,
  extraSettingsJson: '{}'
}

const defaultVpnPanelForm: CreateVpnPanelPayload = {
  name: '',
  baseUrl: '',
  login: 'admin',
  password: '',
  region: 'eu',
  capacity: 5000,
  sslVerificationMode: 'Strict',
  apiVariant: 'X3UiOfficial',
  autoCreateInbound: false,
  defaultInboundTemplateJson: '{"remark":"default-vless","protocol":"vless","port":443,"listen":"","settings":{"clients":[]},"streamSettings":{"network":"tcp","security":"tls"},"sniffing":{}}'
}

const defaultInboundForm: CreateVpnInboundPayload = {
  name: 'default-vless',
  protocol: 'vless',
  port: 443,
  listen: '',
  settingsJson: '{"clients":[]}',
  streamSettingsJson: '{"network":"tcp","security":"tls"}',
  sniffingJson: '{}',
  isDefault: true,
  capacity: 5000
}

const defaultTariffForm: UpdateTariffPayload = {
  name: '',
  slug: '',
  description: '',
  fullDescription: '',
  featuresJson: '[]',
  badge: '',
  durationDays: 30,
  price: 490,
  currency: 'RUB',
  maxDevices: 3,
  trafficLimit: null,
  isTrial: false,
  isActive: true,
  sortOrder: 100,
  category: 'default',
  allowedRegionsCsv: '',
  allowedNodeGroupsCsv: '',
  isReferralEligible: true,
  provisioningScenario: 'auto',
  afterPaymentText: ''
}

const defaultReleaseForm: AppReleaseUpsertPayload = {
  releaseId: '',
  version: '0.1.0',
  releasedAt: new Date().toISOString(),
  title: '',
  summary: '',
  isActive: true,
  source: 'manual',
  items: [
    { type: 'new', text: '', sortOrder: 10 }
  ]
}

const defaultFaqForm: FaqUpsertPayload = {
  question: '',
  answer: '',
  category: 'Общее',
  isActive: true,
  showOnHome: true,
  showOnFaqPage: true,
  sortOrder: 100
}

const defaultSiteContentForm: SiteContentBlockUpsertPayload = {
  key: '',
  value: '',
  group: 'home',
  label: '',
  description: '',
  inputType: 'text',
  isActive: true,
  sortOrder: 100
}

const defaultWorkScenarioForm: WorkScenarioUpsertPayload = {
  name: '',
  key: '',
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
  cabinetText: '',
  telegramText: '',
  generateQrCode: true,
  maxDevices: 3,
  trafficLimit: null,
  sortOrder: 100
}

const defaultBotSettings: AdminTelegramBotSettingsDto = {
  enabled: false,
  mode: 'Polling',
  publicBotUsername: '',
  hasBotToken: false,
  botTokenMasked: '',
  webhookUrl: '',
  hasSecretToken: false,
  adminChatId: '',
  webAppUrl: '',
  welcomeText: '',
  instructionText: '',
  supportText: '',
  afterPaymentTextTemplate: '',
  generatedAt: ''
}

function s(value: unknown, fallback = '—') {
  return typeof value === 'string' && value.trim() ? value : fallback
}

function n(value: unknown, fallback = 0) {
  return typeof value === 'number' && Number.isFinite(value) ? value : fallback
}

function shortId(value?: string | null) {
  return value ? value.slice(0, 8) : '—'
}

function formatDate(value?: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}

function parseTariffFeatures(tariff: Pick<TariffDto, 'features' | 'featuresJson'> | UpdateTariffPayload) {
  const directFeatures = 'features' in tariff ? tariff.features : undefined
  if (Array.isArray(directFeatures) && directFeatures.length > 0) return directFeatures

  if (!tariff.featuresJson) return []

  try {
    const parsed = JSON.parse(tariff.featuresJson)
    return Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === 'string' && item.trim().length > 0) : []
  } catch {
    return []
  }
}

function featuresTextToJson(value: string) {
  return JSON.stringify(value.split('\n').map((item) => item.trim()).filter(Boolean))
}

function featuresToText(tariff: Pick<TariffDto, 'features' | 'featuresJson'> | UpdateTariffPayload) {
  return parseTariffFeatures(tariff).join('\n')
}

function toDateTimeLocalValue(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const offset = date.getTimezoneOffset()
  const local = new Date(date.getTime() - offset * 60_000)
  return local.toISOString().slice(0, 16)
}

function fromDateTimeLocalValue(value: string) {
  return value ? new Date(value).toISOString() : new Date().toISOString()
}

function providerConfigured(account: PaymentProviderAccountDto) {
  if (typeof account.isCheckoutConfigured === 'boolean') return account.isCheckoutConfigured
  if (!account.isEnabled || account.mode === 'Disabled') return false
  if (!account.shopId) return false
  if (account.provider !== 'TelegramStars' && account.mode === 'Production' && !account.hasSecretKey) return false
  return true
}

function providerIssue(account: PaymentProviderAccountDto) {
  if (account.checkoutConfigurationIssue) return account.checkoutConfigurationIssue
  if (providerConfigured(account)) return 'Готов к оплатам'
  if (!account.isEnabled) return 'Провайдер выключен.'
  if (account.mode === 'Disabled') return 'Режим провайдера: Disabled.'
  if (!account.shopId) return 'Нужен ShopId или merchant identifier.'
  if (account.provider !== 'TelegramStars' && account.mode === 'Production' && !account.hasSecretKey) return 'Для production нужен защищенный secret key.'
  return 'Не настроен.'
}

function capabilities(account: PaymentProviderAccountDto) {
  try {
    const parsed = JSON.parse(account.capabilitiesJson ?? '[]')
    return Array.isArray(parsed) ? parsed.map(String) : []
  } catch {
    return []
  }
}

async function copyToClipboard(text: string, setNotice: (value: string) => void) {
  await navigator.clipboard?.writeText(text)
  setNotice('Скопировано в буфер обмена.')
}

export function App() {
  const [token, setToken] = useState(readSessionStorageItem(TOKEN_STORAGE_KEY) ?? '')
  const adminDisabledTitle = token ? undefined : adminAuthRequiredMessage
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [users, setUsers] = useState<GenericUser[]>([])
  const [userSearch, setUserSearch] = useState('')
  const [userStatusFilter, setUserStatusFilter] = useState('')
  const [selectedUserId, setSelectedUserId] = useState('')
  const [userOverview, setUserOverview] = useState<AdminUserOverviewDto | null>(null)
  const [summary, setSummary] = useState<AdminDashboardSummaryDto | null>(null)
  const [subscriptions, setSubscriptions] = useState<SubscriptionDto[]>([])
  const [accessCredentials, setAccessCredentials] = useState<AccessCredentialDto[]>([])
  const [adminQrSvgs, setAdminQrSvgs] = useState<Record<string, string>>({})
  const [orders, setOrders] = useState<OrderDto[]>([])
  const [payments, setPayments] = useState<PaymentAttemptDto[]>([])
  const [paymentProviderAccounts, setPaymentProviderAccounts] = useState<PaymentProviderAccountDto[]>([])
  const [providerCheckResults, setProviderCheckResults] = useState<Record<string, PaymentProviderAccountCheckResultDto>>({})
  const [paymentWebhookEvents, setPaymentWebhookEvents] = useState<PaymentWebhookEventDto[]>([])
  const [refunds, setRefunds] = useState<RefundDto[]>([])
  const [supportConversations, setSupportConversations] = useState<SupportConversationDto[]>([])
  const [selectedSupportConversationId, setSelectedSupportConversationId] = useState('')
  const [supportMessages, setSupportMessages] = useState<SupportMessageDto[]>([])
  const [supportReplyText, setSupportReplyText] = useState('')
  const [supportNoteText, setSupportNoteText] = useState('')
  const [tariffs, setTariffs] = useState<TariffDto[]>([])
  const [tariffForm, setTariffForm] = useState<UpdateTariffPayload>(defaultTariffForm)
  const [tariffFeaturesText, setTariffFeaturesText] = useState('')
  const [editingTariffId, setEditingTariffId] = useState('')
  const [editingProviderAccountId, setEditingProviderAccountId] = useState('')
  const [appReleases, setAppReleases] = useState<AppReleaseDto[]>([])
  const [releaseForm, setReleaseForm] = useState<AppReleaseUpsertPayload>(defaultReleaseForm)
  const [editingReleaseId, setEditingReleaseId] = useState('')
  const [faqEntries, setFaqEntries] = useState<FaqItem[]>([])
  const [faqForm, setFaqForm] = useState<FaqUpsertPayload>(defaultFaqForm)
  const [editingFaqId, setEditingFaqId] = useState('')
  const [siteContentBlocks, setSiteContentBlocks] = useState<SiteContentBlockDto[]>([])
  const [siteContentForm, setSiteContentForm] = useState<SiteContentBlockUpsertPayload>(defaultSiteContentForm)
  const [editingSiteContentId, setEditingSiteContentId] = useState('')
  const [workScenarios, setWorkScenarios] = useState<WorkScenarioDto[]>([])
  const [workScenarioForm, setWorkScenarioForm] = useState<WorkScenarioUpsertPayload>(defaultWorkScenarioForm)
  const [editingWorkScenarioId, setEditingWorkScenarioId] = useState('')
  const [servers, setServers] = useState<VpnNodeDto[]>([])
  const [provisioningRuns, setProvisioningRuns] = useState<ProvisioningRunDto[]>([])
  const [vpnPanels, setVpnPanels] = useState<VpnPanelDto[]>([])
  const [selectedVpnPanelId, setSelectedVpnPanelId] = useState('')
  const [vpnInbounds, setVpnInbounds] = useState<VpnInboundDto[]>([])
  const [vpnClients, setVpnClients] = useState<VpnClientDto[]>([])
  const [vpnHealthChecks, setVpnHealthChecks] = useState<PanelHealthCheckDto[]>([])
  const [vpnSyncRuns, setVpnSyncRuns] = useState<PanelSyncRunDto[]>([])
  const [botSettings, setBotSettings] = useState<AdminTelegramBotSettingsDto>(defaultBotSettings)
  const [botSettingsForm, setBotSettingsForm] = useState<UpdateTelegramBotSettingsPayload>({})
  const [loadErrors, setLoadErrors] = useState<LoadError[]>([])
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [actionBusyId, setActionBusyId] = useState('')
  const [serverForm, setServerForm] = useState<ServerFormState>(defaultServerForm)
  const [editingServerId, setEditingServerId] = useState<string | null>(null)
  const [providerForm, setProviderForm] = useState<UpsertPaymentProviderAccountPayload>(defaultProviderForm)
  const [vpnPanelForm, setVpnPanelForm] = useState<CreateVpnPanelPayload>(defaultVpnPanelForm)
  const [editingVpnPanelId, setEditingVpnPanelId] = useState<string | null>(null)
  const [inboundForm, setInboundForm] = useState<CreateVpnInboundPayload>(defaultInboundForm)
  const [subscriptionExtendDays, setSubscriptionExtendDays] = useState<Record<string, number>>({})
  const [activeSection, setActiveSection] = useState<AdminSectionId>(() => readAdminSectionFromHash())
  const activeSectionLabel = adminSections.find(([id]) => id === activeSection)?.[1] ?? 'Раздел'

  const derivedSummary = useMemo(() => ({
    totalUsers: summary?.totalUsers ?? users.length,
    telegramUsers: summary?.telegramUsers ?? users.filter((item) => s(item.authSource, '') === 'Telegram').length,
    activeSubscriptions: summary?.activeSubscriptions ?? subscriptions.filter((item) => item.status === 'Active').length,
    expiringSubscriptions: summary?.expiringSubscriptions ?? subscriptions.filter((item) => item.status === 'Active' && new Date(item.endAt).getTime() <= Date.now() + 7 * 24 * 60 * 60 * 1000).length,
    paidOrders: summary?.paidOrders ?? orders.filter((item) => item.status === 'PaymentReceived' || item.status === 'Completed').length,
    pendingOrders: summary?.pendingOrders ?? orders.filter((item) => item.status === 'PendingPayment' || item.status === 'Draft').length,
    failedPayments: summary?.failedPayments ?? payments.filter((item) => item.status === 'Failed' || item.status === 'Cancelled').length,
    recentPayments: summary?.recentPayments ?? payments.slice(0, 10).length,
    recentOrders: summary?.recentOrders ?? orders.slice(0, 10).length,
    vpnAccessesCount: summary?.vpnAccessesCount ?? accessCredentials.length,
    vpnNodesCount: summary?.vpnNodesCount ?? servers.length,
    healthyVpnNodes: summary?.healthyVpnNodes ?? servers.filter((item) => item.healthStatus === 'Healthy').length,
    vpnPanelsCount: summary?.vpnPanelsCount ?? vpnPanels.length,
    healthyVpnPanels: summary?.healthyVpnPanels ?? vpnPanels.filter((item) => item.healthStatus === 'Healthy').length,
    supportConversationsCount: summary?.supportConversationsCount ?? supportConversations.length,
    openSupportConversations: summary?.openSupportConversations ?? supportConversations.filter((item) => item.status === 'open' || item.status === 'pending').length,
    provisioningErrors: summary?.provisioningErrors ?? provisioningRuns.filter((item) => item.status === 'Failed').length
  }), [summary, users, subscriptions, accessCredentials, orders, payments, servers, provisioningRuns, supportConversations, vpnPanels])

  const safeLoad = async <T,>(area: string, loader: () => Promise<T>, fallback: T, errors: LoadError[]) => {
    try {
      return await loader()
    } catch (e) {
      errors.push({ area, message: e instanceof Error ? e.message : 'Failed to load' })
      return fallback
    }
  }

  const loadUsers = async (currentToken = token) => {
    if (!currentToken) return
    const nextUsers = await api.getAdminUsers(currentToken, { search: userSearch, status: userStatusFilter })
    setUsers(nextUsers)
    if (!selectedUserId && nextUsers.length > 0) setSelectedUserId(String(nextUsers[0].id ?? ''))
  }

  const loadAll = async (currentToken: string) => {
    if (!currentToken) return
    setBusy(true)
    setError('')
    const errors: LoadError[] = []

    const [
      nextSummary,
      nextUsers,
      nextSubscriptions,
      nextAccessCredentials,
      nextOrders,
      nextPayments,
      nextPaymentProviderAccounts,
      nextPaymentWebhookEvents,
      nextRefunds,
      nextSupportConversations,
      nextTariffs,
      nextAppReleases,
      nextFaqEntries,
      nextSiteContent,
      nextWorkScenarios,
      nextServers,
      nextRuns,
      nextVpnPanels,
      nextBotSettings
    ] = await Promise.all([
      safeLoad('dashboard', () => api.getAdminDashboardSummary(currentToken), null, errors),
      safeLoad('users', () => api.getAdminUsers(currentToken, { search: userSearch, status: userStatusFilter }), [], errors),
      safeLoad('subscriptions', () => api.getAdminSubscriptions(currentToken), [], errors),
      safeLoad('accesses', () => api.getAdminAccesses(currentToken), [], errors),
      safeLoad('orders', () => api.getAdminOrders(currentToken), [], errors),
      safeLoad('payments', () => api.getAdminPayments(currentToken), [], errors),
      safeLoad('способы оплаты', () => api.getAdminPaymentProviderAccounts(currentToken), [], errors),
      safeLoad('события оплат', () => api.getAdminPaymentWebhookEvents(currentToken), [], errors),
      safeLoad('refunds', () => api.getAdminRefunds(currentToken), [], errors),
      safeLoad('обращения поддержки', () => api.getAdminSupportConversations(currentToken), [], errors),
      safeLoad('tariffs', () => api.getAdminTariffs(currentToken), [], errors),
      safeLoad('Что нового', () => api.getAdminAppReleases(currentToken), [], errors),
      safeLoad('FAQ', () => api.getAdminFaq(currentToken), [], errors),
      safeLoad('контент сайта', () => api.getAdminSiteContent(currentToken, 'home'), [], errors),
      safeLoad('сценарии работы', () => api.getAdminWorkScenarios(currentToken), [], errors),
      safeLoad('servers', () => api.getAdminServers(currentToken), [], errors),
      safeLoad('подготовка серверов', () => api.getAdminProvisioningRuns(currentToken), [], errors),
      safeLoad('VPN-панели', () => api.getAdminVpnPanels(currentToken), [], errors),
      safeLoad('настройки Telegram-бота', () => api.getAdminTelegramBotSettings(currentToken), defaultBotSettings, errors)
    ])

    setSummary(nextSummary)
    setUsers(nextUsers)
    setSubscriptions(nextSubscriptions)
    setAccessCredentials(nextAccessCredentials)
    setOrders(nextOrders)
    setPayments(nextPayments)
    setPaymentProviderAccounts(nextPaymentProviderAccounts)
    setPaymentWebhookEvents(nextPaymentWebhookEvents)
    setRefunds(nextRefunds)
    setSupportConversations(nextSupportConversations)
    setTariffs(nextTariffs)
    setAppReleases(nextAppReleases)
    setFaqEntries(nextFaqEntries)
    setSiteContentBlocks(nextSiteContent)
    setWorkScenarios(nextWorkScenarios)
    setServers(nextServers)
    setProvisioningRuns(nextRuns)
    setVpnPanels(nextVpnPanels)
    setBotSettings(nextBotSettings)
    setBotSettingsForm({
      enabled: nextBotSettings.enabled,
      mode: nextBotSettings.mode,
      publicBotUsername: nextBotSettings.publicBotUsername,
      botToken: '',
      webhookUrl: nextBotSettings.webhookUrl,
      secretToken: '',
      adminChatId: nextBotSettings.adminChatId,
      webAppUrl: nextBotSettings.webAppUrl,
      welcomeText: nextBotSettings.welcomeText,
      instructionText: nextBotSettings.instructionText,
      supportText: nextBotSettings.supportText,
      afterPaymentTextTemplate: nextBotSettings.afterPaymentTextTemplate
    })
    setLoadErrors(errors)
    if (!selectedSupportConversationId && nextSupportConversations.length > 0) setSelectedSupportConversationId(nextSupportConversations[0].id)
    if (!selectedVpnPanelId && nextVpnPanels.length > 0) setSelectedVpnPanelId(nextVpnPanels[0].id)
    if (!selectedUserId && nextUsers.length > 0) setSelectedUserId(String(nextUsers[0].id ?? ''))
    setBusy(false)
  }

  useEffect(() => {
    void loadAll(token)
  }, [token])

  useEffect(() => {
    const syncActiveSection = () => setActiveSection(readAdminSectionFromHash())
    syncActiveSection()
    window.addEventListener('hashchange', syncActiveSection)

    return () => {
      window.removeEventListener('hashchange', syncActiveSection)
    }
  }, [])

  useEffect(() => {
    if (token && selectedVpnPanelId) void loadVpnPanelDetails(selectedVpnPanelId)
  }, [token, selectedVpnPanelId])

  useEffect(() => {
    if (token && selectedSupportConversationId) void loadSupportMessages(selectedSupportConversationId)
  }, [token, selectedSupportConversationId])

  useEffect(() => {
    if (token && selectedUserId) void loadUserOverview(selectedUserId)
  }, [token, selectedUserId])

  const updateServerForm = <K extends keyof ServerFormState>(key: K, value: ServerFormState[K]) => setServerForm((current) => ({ ...current, [key]: value }))
  const updateProviderForm = <K extends keyof UpsertPaymentProviderAccountPayload>(key: K, value: UpsertPaymentProviderAccountPayload[K]) => setProviderForm((current) => ({ ...current, [key]: value }))
  const updateVpnPanelForm = <K extends keyof CreateVpnPanelPayload>(key: K, value: CreateVpnPanelPayload[K]) => setVpnPanelForm((current) => ({ ...current, [key]: value }))
  const updateInboundForm = <K extends keyof CreateVpnInboundPayload>(key: K, value: CreateVpnInboundPayload[K]) => setInboundForm((current) => ({ ...current, [key]: value }))
  const updateTariffForm = <K extends keyof UpdateTariffPayload>(key: K, value: UpdateTariffPayload[K]) => setTariffForm((current) => ({ ...current, [key]: value }))
  const updateReleaseForm = <K extends keyof AppReleaseUpsertPayload>(key: K, value: AppReleaseUpsertPayload[K]) => setReleaseForm((current) => ({ ...current, [key]: value }))
  const updateFaqForm = <K extends keyof FaqUpsertPayload>(key: K, value: FaqUpsertPayload[K]) => setFaqForm((current) => ({ ...current, [key]: value }))
  const updateSiteContentForm = <K extends keyof SiteContentBlockUpsertPayload>(key: K, value: SiteContentBlockUpsertPayload[K]) => setSiteContentForm((current) => ({ ...current, [key]: value }))
  const updateWorkScenarioForm = <K extends keyof WorkScenarioUpsertPayload>(key: K, value: WorkScenarioUpsertPayload[K]) => setWorkScenarioForm((current) => ({ ...current, [key]: value }))
  const updateReleaseItem = (index: number, patch: Partial<AppReleaseUpsertPayload['items'][number]>) => setReleaseForm((current) => ({
    ...current,
    items: current.items.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item)
  }))
  const addReleaseItem = () => setReleaseForm((current) => ({
    ...current,
    items: [...current.items, { type: 'new', text: '', sortOrder: (current.items.length + 1) * 10 }]
  }))
  const removeReleaseItem = (index: number) => setReleaseForm((current) => ({
    ...current,
    items: current.items.length <= 1 ? current.items : current.items.filter((_, itemIndex) => itemIndex !== index)
  }))
  const updateBotForm = <K extends keyof UpdateTelegramBotSettingsPayload>(key: K, value: UpdateTelegramBotSettingsPayload[K]) => setBotSettingsForm((current) => ({ ...current, [key]: value }))

  const runAction = async (id: string, action: () => Promise<void>) => {
    setActionBusyId(id)
    setError('')
    setNotice('')
    try {
      await action()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Action failed')
    } finally {
      setActionBusyId('')
    }
  }

  const clearAdminSession = () => {
    removeSessionStorageItem(TOKEN_STORAGE_KEY)
    setToken('')
    setUsers([])
    setSelectedUserId('')
    setUserOverview(null)
    setSummary(null)
    setSubscriptions([])
    setAccessCredentials([])
    setAdminQrSvgs({})
    setOrders([])
    setPayments([])
    setPaymentProviderAccounts([])
    setProviderCheckResults({})
    setPaymentWebhookEvents([])
    setRefunds([])
    setSupportConversations([])
    setSelectedSupportConversationId('')
    setSupportMessages([])
    setTariffs([])
    setTariffForm(defaultTariffForm)
    setTariffFeaturesText('')
    setEditingTariffId('')
    setAppReleases([])
    setReleaseForm(defaultReleaseForm)
    setEditingReleaseId('')
    setFaqEntries([])
    setFaqForm(defaultFaqForm)
    setEditingFaqId('')
    setSiteContentBlocks([])
    setSiteContentForm(defaultSiteContentForm)
    setEditingSiteContentId('')
    setWorkScenarios([])
    setWorkScenarioForm(defaultWorkScenarioForm)
    setEditingWorkScenarioId('')
    setServers([])
    setServerForm(defaultServerForm)
    setEditingServerId(null)
    setProvisioningRuns([])
    setVpnPanels([])
    setSelectedVpnPanelId('')
    setVpnInbounds([])
    setVpnClients([])
    setVpnHealthChecks([])
    setVpnSyncRuns([])
    setVpnPanelForm(defaultVpnPanelForm)
    setEditingVpnPanelId(null)
    setBotSettings(defaultBotSettings)
    setBotSettingsForm({})
    setLoadErrors([])
    setError('')
    setActionBusyId('')
    setNotice('Сессия администратора завершена. Данные панели очищены.')
  }

  const handleLogin = async () => {
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const response = await api.login(email, password)
      writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
      setToken(response.accessToken)
      setNotice('Токен администратора сохранён в sessionStorage и не показывается в UI.')
      await loadAll(response.accessToken)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось получить admin token')
    } finally {
      setBusy(false)
    }
  }

  const loadUserOverview = async (userId: string) => {
    if (!token || !userId) return
    try {
      setUserOverview(await api.getAdminUserOverview(token, userId))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось загрузить карточку пользователя')
    }
  }

  const loadSupportMessages = async (conversationId: string) => {
    if (!token || !conversationId) return
    try {
      setSupportMessages(await api.getAdminSupportMessages(token, conversationId))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось загрузить сообщения поддержки')
    }
  }

  const loadVpnPanelDetails = async (panelId: string) => {
    if (!token || !panelId) return
    try {
      const [nextInbounds, nextClients, nextHealthChecks, nextSyncRuns] = await Promise.all([
        api.getAdminVpnPanelInbounds(token, panelId),
        api.getAdminVpnPanelClients(token, panelId),
        api.getAdminVpnPanelHealthChecks(token, panelId),
        api.getAdminVpnPanelSyncRuns(token, panelId)
      ])
      setVpnInbounds(nextInbounds)
      setVpnClients(nextClients)
      setVpnHealthChecks(nextHealthChecks)
      setVpnSyncRuns(nextSyncRuns)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось загрузить детали VPN-панели')
    }
  }

  const resetProviderForm = () => {
    setProviderForm(defaultProviderForm)
    setEditingProviderAccountId('')
  }

  const editProviderAccount = (account: PaymentProviderAccountDto) => {
    setEditingProviderAccountId(account.id)
    setProviderForm({
      provider: account.provider,
      mode: account.mode,
      name: account.name,
      publicName: account.publicName,
      isEnabled: account.isEnabled,
      isDefault: account.isDefault,
      shopId: account.shopId ?? '',
      apiBaseUrl: account.apiBaseUrl ?? '',
      returnUrl: account.returnUrl ?? '',
      webhookUrl: account.webhookUrl ?? '',
      secretKey: '',
      webhookSecret: '',
      useWebhookIpAllowList: account.useWebhookIpAllowList,
      allowedWebhookIpRangesCsv: account.allowedWebhookIpRangesCsv ?? '',
      extraSettingsJson: ''
    })
    setActiveSection('payments')
    if (typeof window !== 'undefined') window.location.hash = 'payments'
  }

  const handleSaveProviderAccount = async () => {
    if (!token) return
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const saved = editingProviderAccountId
        ? await api.updateAdminPaymentProviderAccount(token, editingProviderAccountId, providerForm)
        : await api.createAdminPaymentProviderAccount(token, providerForm)
      setNotice(`Способ оплаты ${saved.name} ${editingProviderAccountId ? 'обновлен' : 'сохранен'}. Секреты не отображаются.`)
      resetProviderForm()
      await loadAll(token)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось сохранить способ оплаты')
    } finally {
      setBusy(false)
    }
  }

  const handleSetProviderEnabled = async (account: PaymentProviderAccountDto, enabled: boolean) => {
    await runAction(account.id, async () => {
      await api.setAdminPaymentProviderAccountEnabled(token, account.id, enabled)
      setNotice(`${account.name}: ${enabled ? 'включен' : 'выключен'}`)
      await loadAll(token)
    })
  }

  const handleCheckProviderAccount = async (account: PaymentProviderAccountDto) => {
    await runAction(`provider-check-${account.id}`, async () => {
      const result = await api.checkAdminPaymentProviderAccount(token, account.id)
      setProviderCheckResults((current) => ({ ...current, [account.id]: result }))
      setPaymentProviderAccounts((current) => current.map((item) => item.id === account.id ? result.account : item))
      setNotice(`${account.name}: ${result.isReady ? 'проверка пройдена' : 'проверка нашла проблемы'}.`)
    })
  }

  const handleRecheckPayment = async (paymentId: string) => runAction(paymentId, async () => {
    const payment = await api.recheckAdminPayment(token, paymentId)
    setNotice(`Платеж ${payment.id.slice(0, 8)} проверен: ${payment.status}`)
    await loadAll(token)
  })

  const handleRefundPayment = async (payment: PaymentAttemptDto) => {
    await runAction(payment.id, async () => {
      const refund = await api.refundAdminPayment(token, payment.id, payment.amount, 'manual_admin_refund')
      setNotice(`Возврат ${refund.providerRefundId || refund.id}: ${refund.status}`)
      await loadAll(token)
    })
  }

  const resetTariffForm = () => {
    setTariffForm(defaultTariffForm)
    setTariffFeaturesText('')
    setEditingTariffId('')
  }

  const editTariff = (tariff: TariffDto) => {
    setEditingTariffId(tariff.id)
    setTariffForm({
      name: tariff.name,
      slug: tariff.slug,
      description: tariff.description,
      fullDescription: tariff.fullDescription ?? '',
      featuresJson: tariff.featuresJson ?? JSON.stringify(tariff.features ?? []),
      badge: tariff.badge ?? '',
      durationDays: tariff.durationDays,
      price: tariff.price,
      currency: tariff.currency,
      maxDevices: tariff.maxDevices,
      trafficLimit: tariff.trafficLimit ?? null,
      isTrial: tariff.isTrial ?? false,
      isActive: tariff.isActive !== false,
      sortOrder: tariff.sortOrder ?? 100,
      category: tariff.category,
      allowedRegionsCsv: tariff.allowedRegionsCsv ?? '',
      allowedNodeGroupsCsv: tariff.allowedNodeGroupsCsv ?? '',
      isReferralEligible: tariff.isReferralEligible !== false,
      provisioningScenario: tariff.provisioningScenario ?? 'auto',
      afterPaymentText: tariff.afterPaymentText ?? ''
    })
    setTariffFeaturesText(featuresToText(tariff))
    setActiveSection('tariffs')
    if (typeof window !== 'undefined') window.location.hash = 'tariffs'
  }

  const handleSaveTariff = async () => {
    if (!token) return
    if (!tariffForm.name || n(tariffForm.price) < 0 || n(tariffForm.durationDays) <= 0) {
      setError('Тариф: название обязательно, цена >= 0, срок > 0 дней.')
      return
    }
    const payload = { ...tariffForm, featuresJson: featuresTextToJson(tariffFeaturesText) }
    setBusy(true)
    setError('')
    try {
      if (editingTariffId) {
        await api.updateAdminTariff(token, editingTariffId, payload)
        setNotice('Тариф обновлён.')
      } else {
        await api.createAdminTariff(token, payload)
        setNotice('Тариф создан.')
      }
      resetTariffForm()
      await loadAll(token)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось сохранить тариф')
    } finally {
      setBusy(false)
    }
  }

  const handleToggleTariff = async (tariff: TariffDto) => {
    await runAction(tariff.id, async () => {
      await api.updateAdminTariff(token, tariff.id, { isActive: tariff.isActive === false })
      setNotice(`Тариф ${tariff.name} обновлён.`)
      await loadAll(token)
    })
  }

  const handleDeleteTariff = async (tariff: TariffDto) => {
    await runAction(`delete-${tariff.id}`, async () => {
      await api.deleteAdminTariff(token, tariff.id)
      if (editingTariffId === tariff.id) resetTariffForm()
      setNotice(`Тариф ${tariff.name} удалён.`)
      await loadAll(token)
    })
  }

  const resetReleaseForm = () => {
    setReleaseForm({ ...defaultReleaseForm, releasedAt: new Date().toISOString(), items: [{ type: 'new', text: '', sortOrder: 10 }] })
    setEditingReleaseId('')
  }

  const editRelease = (release: AppReleaseDto) => {
    setEditingReleaseId(release.id)
    setReleaseForm({
      releaseId: release.releaseId,
      version: release.version,
      releasedAt: release.releasedAt,
      title: release.title,
      summary: release.summary,
      isActive: release.isActive,
      source: release.source,
      items: release.items.length > 0 ? release.items.map((item, index) => ({
        id: item.id ?? null,
        type: item.type,
        text: item.text,
        sortOrder: item.sortOrder || (index + 1) * 10
      })) : [{ type: 'new', text: '', sortOrder: 10 }]
    })
    setActiveSection('releases')
    if (typeof window !== 'undefined') window.location.hash = 'releases'
  }

  const handleSaveRelease = async () => {
    if (!token) return
    const payload = {
      ...releaseForm,
      releaseId: releaseForm.releaseId.trim(),
      version: releaseForm.version.trim(),
      title: releaseForm.title.trim(),
      summary: releaseForm.summary.trim(),
      items: releaseForm.items
        .filter((item) => item.text.trim())
        .map((item, index) => ({ ...item, text: item.text.trim(), sortOrder: item.sortOrder || (index + 1) * 10 }))
    }

    if (!payload.releaseId || !payload.version || !payload.title || !payload.summary || payload.items.length === 0) {
      setError('Заполните releaseId, версию, заголовок, описание и хотя бы один пункт релиза.')
      return
    }

    await runAction(editingReleaseId ? `release-update-${editingReleaseId}` : 'release-create', async () => {
      if (editingReleaseId) {
        await api.updateAdminAppRelease(token, editingReleaseId, payload)
        setNotice('Релиз обновлен.')
      } else {
        await api.createAdminAppRelease(token, payload)
        setNotice('Релиз создан.')
      }
      resetReleaseForm()
      await loadAll(token)
    })
  }

  const handleDeleteRelease = async (release: AppReleaseDto) => {
    await runAction(`release-delete-${release.id}`, async () => {
      await api.deleteAdminAppRelease(token, release.id)
      if (editingReleaseId === release.id) resetReleaseForm()
      setNotice(`Релиз ${release.version} удален.`)
      await loadAll(token)
    })
  }

  const resetFaqForm = () => {
    setFaqForm(defaultFaqForm)
    setEditingFaqId('')
  }

  const editFaq = (entry: FaqItem) => {
    if (!entry.id) return
    setEditingFaqId(entry.id)
    setFaqForm({
      question: entry.question,
      answer: entry.answer,
      category: entry.category ?? 'Общее',
      isActive: entry.isActive !== false,
      showOnHome: entry.showOnHome !== false,
      showOnFaqPage: entry.showOnFaqPage !== false,
      sortOrder: entry.sortOrder ?? 100
    })
    setActiveSection('faq')
    if (typeof window !== 'undefined') window.location.hash = 'faq'
  }

  const handleSaveFaq = async () => {
    if (!token) return
    const payload: FaqUpsertPayload = {
      ...faqForm,
      question: faqForm.question.trim(),
      answer: faqForm.answer.trim(),
      category: faqForm.category?.trim() || 'Общее',
      sortOrder: Number(faqForm.sortOrder) || 0
    }

    if (!payload.question || !payload.answer) {
      setError('FAQ: заполните вопрос и ответ.')
      return
    }

    await runAction(editingFaqId ? `faq-update-${editingFaqId}` : 'faq-create', async () => {
      if (editingFaqId) {
        await api.updateAdminFaq(token, editingFaqId, payload)
        setNotice('Вопрос FAQ обновлен.')
      } else {
        await api.createAdminFaq(token, payload)
        setNotice('Вопрос FAQ создан.')
      }
      resetFaqForm()
      await loadAll(token)
    })
  }

  const handleDeleteFaq = async (entry: FaqItem) => {
    const faqId = entry.id
    if (!faqId) return
    await runAction(`faq-delete-${faqId}`, async () => {
      await api.deleteAdminFaq(token, faqId)
      if (editingFaqId === faqId) resetFaqForm()
      setNotice('Вопрос FAQ удален.')
      await loadAll(token)
    })
  }

  const resetSiteContentForm = () => {
    setSiteContentForm(defaultSiteContentForm)
    setEditingSiteContentId('')
  }

  const editSiteContent = (block: SiteContentBlockDto) => {
    setEditingSiteContentId(block.id)
    setSiteContentForm({
      key: block.key,
      value: block.value,
      group: block.group,
      label: block.label,
      description: block.description,
      inputType: block.inputType,
      isActive: block.isActive,
      sortOrder: block.sortOrder
    })
    setActiveSection('content')
    if (typeof window !== 'undefined') window.location.hash = 'content'
  }

  const handleSaveSiteContent = async () => {
    if (!token) return
    const key = siteContentForm.key.trim()
    const label = siteContentForm.label?.trim() || key
    if (!key || !label) {
      setError('Контент сайта: заполните ключ и название поля.')
      return
    }
    const payload: SiteContentBlockUpsertPayload = {
      ...siteContentForm,
      key,
      label,
      group: siteContentForm.group?.trim() || 'home',
      inputType: siteContentForm.inputType?.trim() || 'text',
      sortOrder: Number(siteContentForm.sortOrder) || 0
    }

    await runAction(editingSiteContentId ? `content-update-${editingSiteContentId}` : 'content-create', async () => {
      if (editingSiteContentId) {
        await api.updateAdminSiteContent(token, editingSiteContentId, payload)
        setNotice('Блок контента обновлен.')
      } else {
        await api.createAdminSiteContent(token, payload)
        setNotice('Блок контента создан.')
      }
      resetSiteContentForm()
      await loadAll(token)
    })
  }

  const handleDeleteSiteContent = async (block: SiteContentBlockDto) => {
    await runAction(`content-delete-${block.id}`, async () => {
      await api.deleteAdminSiteContent(token, block.id)
      if (editingSiteContentId === block.id) resetSiteContentForm()
      setNotice('Блок контента удален.')
      await loadAll(token)
    })
  }

  const resetWorkScenarioForm = () => {
    setWorkScenarioForm(defaultWorkScenarioForm)
    setEditingWorkScenarioId('')
  }

  const editWorkScenario = (scenario: WorkScenarioDto) => {
    setEditingWorkScenarioId(scenario.id)
    setWorkScenarioForm({
      name: scenario.name,
      key: scenario.key,
      isActive: scenario.isActive,
      allowedTariffIdsJson: scenario.allowedTariffIdsJson,
      vpnProtocol: scenario.vpnProtocol,
      serverSelectionRule: scenario.serverSelectionRule,
      inboundSelectionRule: scenario.inboundSelectionRule,
      provisioningMode: scenario.provisioningMode,
      onPaymentSucceeded: scenario.onPaymentSucceeded,
      onPaymentFailed: scenario.onPaymentFailed,
      onRefund: scenario.onRefund,
      onSubscriptionExpired: scenario.onSubscriptionExpired,
      onRenewal: scenario.onRenewal,
      cabinetText: scenario.cabinetText,
      telegramText: scenario.telegramText,
      generateQrCode: scenario.generateQrCode,
      maxDevices: scenario.maxDevices,
      trafficLimit: scenario.trafficLimit ?? null,
      sortOrder: scenario.sortOrder
    })
    setActiveSection('scenarios')
    if (typeof window !== 'undefined') window.location.hash = 'scenarios'
  }

  const handleSaveWorkScenario = async () => {
    if (!token) return
    if (!workScenarioForm.name.trim() || !workScenarioForm.key.trim()) {
      setError('Сценарий: заполните название и ключ.')
      return
    }

    const payload: WorkScenarioUpsertPayload = {
      ...workScenarioForm,
      name: workScenarioForm.name.trim(),
      key: workScenarioForm.key.trim(),
      allowedTariffIdsJson: workScenarioForm.allowedTariffIdsJson || '[]',
      maxDevices: Number(workScenarioForm.maxDevices) || 1,
      sortOrder: Number(workScenarioForm.sortOrder) || 0
    }

    await runAction(editingWorkScenarioId ? `scenario-update-${editingWorkScenarioId}` : 'scenario-create', async () => {
      if (editingWorkScenarioId) {
        await api.updateAdminWorkScenario(token, editingWorkScenarioId, payload)
        setNotice('Сценарий работы обновлен.')
      } else {
        await api.createAdminWorkScenario(token, payload)
        setNotice('Сценарий работы создан.')
      }
      resetWorkScenarioForm()
      await loadAll(token)
    })
  }

  const handleDeleteWorkScenario = async (scenario: WorkScenarioDto) => {
    await runAction(`scenario-delete-${scenario.id}`, async () => {
      await api.deleteAdminWorkScenario(token, scenario.id)
      if (editingWorkScenarioId === scenario.id) resetWorkScenarioForm()
      setNotice('Сценарий работы удален.')
      await loadAll(token)
    })
  }

  const handleSubscriptionAction = async (subscription: SubscriptionDto, action: 'extend' | 'block' | 'unblock' | 'cancel') => {
    if (action === 'extend') {
      const days = Number(subscriptionExtendDays[subscription.id] ?? 30)
      if (!Number.isFinite(days) || days <= 0) {
        setError('Укажите положительное количество дней для продления подписки.')
        return
      }
      await runAction(`${action}-${subscription.id}`, async () => {
        await api.extendAdminSubscription(token, subscription.id, days, 'manual_admin_extend')
        setNotice(`Подписка продлена на ${days} дней.`)
        await loadAll(token)
      })
      return
    }

    const map = {
      block: () => api.blockAdminSubscription(token, subscription.id, 'manual_admin_action'),
      unblock: () => api.unblockAdminSubscription(token, subscription.id, 'manual_admin_action'),
      cancel: () => api.cancelAdminSubscription(token, subscription.id, 'manual_admin_action')
    }
    await runAction(`${action}-${subscription.id}`, async () => {
      await map[action]()
      setNotice(`Подписка обновлена: ${shortId(subscription.id)}`)
      await loadAll(token)
    })
  }

  const handleAccessAction = async (access: AccessCredentialDto, enable: boolean) => {
    await runAction(`${enable ? 'enable' : 'disable'}-${access.id}`, async () => {
      if (enable) await api.enableAdminAccess(token, access.id, 'manual_admin_action')
      else await api.disableAdminAccess(token, access.id, 'manual_admin_action')
      setNotice(`VPN-доступ ${enable ? 'включен' : 'отключен'}.`)
      await loadAll(token)
    })
  }

  const handleAccessSync = async (access: AccessCredentialDto) => {
    await runAction(`sync-${access.id}`, async () => {
      await api.syncAdminAccess(token, access.id, 'manual_admin_sync')
      setNotice('VPN-доступ синхронизирован.')
      await loadAll(token)
    })
  }

  const handleAccessResetTraffic = async (access: AccessCredentialDto) => {
    await runAction(`reset-${access.id}`, async () => {
      await api.resetAdminAccessTraffic(token, access.id, 'manual_admin_reset_traffic')
      setNotice('Сброс трафика запрошен.')
      await loadAll(token)
    })
  }


  const handleAdminAccessQr = async (access: AccessCredentialDto) => {
    await runAction(`qr-${access.id}`, async () => {
      const svg = await api.getAdminAccessQrSvg(token, access.id)
      setAdminQrSvgs((current) => ({ ...current, [access.id]: svg }))
      setNotice('QR-код загружен. Он содержит ссылку подключения и не добавляет дополнительных секретов.')
    })
  }

  const handleReplySupport = async () => {
    if (!token || !selectedSupportConversationId || !supportReplyText.trim()) return
    await runAction(`support-reply-${selectedSupportConversationId}`, async () => {
      await api.replyAdminSupportConversation(token, selectedSupportConversationId, supportReplyText.trim())
      setSupportReplyText('')
      setNotice('Ответ сохранен и поставлен в очередь отправки Telegram.')
      await loadSupportMessages(selectedSupportConversationId)
      await loadAll(token)
    })
  }

  const handleSupportStatus = async (status: string, conversationId = selectedSupportConversationId) => {
    if (!token || !conversationId) return
    await runAction(`support-status-${conversationId}`, async () => {
      await api.updateAdminSupportConversationStatus(token, conversationId, status)
      setNotice(`Статус обращения обновлен: ${status}.`)
      await loadSupportMessages(conversationId)
      await loadAll(token)
    })
  }

  const handleSupportNote = async () => {
    if (!token || !selectedSupportConversationId || !supportNoteText.trim()) return
    await runAction(`support-note-${selectedSupportConversationId}`, async () => {
      await api.addAdminSupportInternalNote(token, selectedSupportConversationId, supportNoteText.trim())
      setSupportNoteText('')
      setNotice('Внутренняя заметка сохранена.')
      await loadSupportMessages(selectedSupportConversationId)
    })
  }

  const editVpnPanel = (panel: VpnPanelDto) => {
    setEditingVpnPanelId(panel.id)
    setSelectedVpnPanelId(panel.id)
    setVpnPanelForm({
      name: panel.name,
      baseUrl: panel.baseUrl,
      login: panel.login,
      password: '',
      region: panel.region,
      capacity: panel.capacity,
      sslVerificationMode: panel.sslVerificationMode || 'Strict',
      apiVariant: panel.apiVariant || 'X3UiOfficial',
      autoCreateInbound: panel.autoCreateInbound,
      defaultInboundTemplateJson: panel.defaultInboundTemplateJson || '{}'
    })
  }

  const cancelVpnPanelEdit = () => {
    setEditingVpnPanelId(null)
    setVpnPanelForm(defaultVpnPanelForm)
  }

  const handleSaveVpnPanel = async () => {
    if (!token) return
    setBusy(true)
    setError('')
    try {
      const saved = editingVpnPanelId
        ? await api.updateAdminVpnPanel(token, editingVpnPanelId, vpnPanelForm)
        : await api.createAdminVpnPanel(token, vpnPanelForm)
      setNotice(`VPN-панель ${saved.name} ${editingVpnPanelId ? 'обновлена' : 'сохранена'}. Пароль не возвращается из API.`)
      setSelectedVpnPanelId(saved.id)
      setEditingVpnPanelId(null)
      setVpnPanelForm({ ...defaultVpnPanelForm, region: vpnPanelForm.region })
      await loadAll(token)
      await loadVpnPanelDetails(saved.id)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось сохранить VPN-панель')
    } finally {
      setBusy(false)
    }
  }

  const handleTestVpnPanel = (panelId: string) => runAction(`test-${panelId}`, async () => {
    const result = await api.testAdminVpnPanel(token, panelId)
    setNotice(`Проверка панели: ${result.status} (${result.version || 'версия неизвестна'})`)
    await loadAll(token)
    await loadVpnPanelDetails(panelId)
  })

  const handleSyncVpnPanel = (panelId: string) => runAction(`sync-${panelId}`, async () => {
    const result = await api.syncAdminVpnPanel(token, panelId)
    setNotice(`Синхронизация ${result.status}: ${result.summaryJson || result.errorMessage}`)
    await loadAll(token)
    await loadVpnPanelDetails(panelId)
  })

  const handleCreateInbound = () => runAction('create-inbound', async () => {
    if (!selectedVpnPanelId) return
    const saved = await api.createAdminVpnPanelInbound(token, selectedVpnPanelId, inboundForm)
    setNotice(`Inbound-правило ${saved.name} создано.`)
    await loadVpnPanelDetails(selectedVpnPanelId)
  })

  const handleSetDefaultInbound = (inboundId: string) => runAction(inboundId, async () => {
    await api.setAdminVpnInboundDefault(token, inboundId)
    setNotice('Основное inbound-правило обновлено.')
    await loadVpnPanelDetails(selectedVpnPanelId)
  })

  const editServer = (server: VpnNodeDto) => {
    setEditingServerId(server.id)
    setServerForm({
      ...defaultServerForm,
      name: server.name,
      host: server.host,
      ipAddress: server.ipAddress,
      provider: server.provider,
      region: server.region,
      country: server.country,
      datacenter: server.datacenter,
      capacity: server.capacity,
      supportedProtocolsCsv: server.supportedProtocolsCsv,
      priority: server.priority,
      tagsCsv: server.tagsCsv,
      sshUser: server.sshUser ?? 'root',
      sshPort: server.sshPort ?? 22,
      sshPrivateKeyPath: '',
      sshAuthMethod: server.sshAuthMethod ?? 'ssh_key',
      sshCredential: '',
      validationMode: readTagValue(server.tagsCsv, 'validation-mode') !== 'false',
      ownerType: readTagValue(server.tagsCsv, 'owner') ?? 'admin',
      skipHostKeyChecking: server.skipHostKeyChecking ?? true,
      panelBaseUrl: server.panelBaseUrl ?? '',
      panelUsername: server.panelUsername ?? 'admin',
      panelPassword: '',
      panelInboundId: server.panelInboundId ?? 1,
      publicHostname: server.publicHostname ?? '',
      publicPort: server.publicPort ?? 443,
      nodeGroupId: server.nodeGroupId ?? null
    })
  }

  const cancelServerEdit = () => {
    setEditingServerId(null)
    setServerForm(defaultServerForm)
  }

  const handleSaveServer = async () => {
    if (!token) return
    setBusy(true)
    setError('')
    try {
      const saved = editingServerId
        ? await api.updateAdminServer(token, editingServerId, serverForm)
        : await api.createAdminServer(token, serverForm)
      setNotice(`Сервер ${saved.name} ${editingServerId ? 'обновлен' : 'создан'}. Секреты не возвращаются из API.`)
      setEditingServerId(null)
      setServerForm((current) => ({ ...defaultServerForm, provider: current.provider, region: current.region, country: current.country, datacenter: current.datacenter }))
      await loadAll(token)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось сохранить сервер')
    } finally {
      setBusy(false)
    }
  }

  const handleServerMode = async (server: VpnNodeDto, action: 'maintenance' | 'ready' | 'drain' | 'allocate') => {
    const actionLabel = action === 'maintenance' ? 'перевести в обслуживание' : action === 'ready' ? 'вернуть в работу' : action === 'drain' ? 'закрыть набор пользователей' : 'открыть набор пользователей'
    await runAction(`${action}-${server.id}`, async () => {
      if (action === 'maintenance') await api.enableAdminServerMaintenance(token, server.id)
      if (action === 'ready') await api.disableAdminServerMaintenance(token, server.id)
      if (action === 'drain') await api.disableAdminServerAllocation(token, server.id)
      if (action === 'allocate') await api.enableAdminServerAllocation(token, server.id)
      setNotice(`Сервер ${server.name}: ${actionLabel}.`)
      await loadAll(token)
    })
  }

  const handleQueuePrecheck = (serverId: string) => runAction(`precheck-${serverId}`, async () => {
    const response = await api.precheckAdminServer(token, serverId)
    setNotice(`Проверка поставлена в очередь. ID запуска: ${response.runId}`)
    await loadAll(token)
  })

  const handleQueueProvision = async (serverId: string) => {
    await runAction(`provision-${serverId}`, async () => {
      const response = await api.queueAdminProvision(token, serverId, false)
      setNotice(`Подготовка сервера поставлена в очередь. ID запуска: ${response.runId}`)
      await loadAll(token)
    })
  }

  const handleRetryProvisioningRun = (runId: string) => runAction(`retry-${runId}`, async () => {
    const response = await api.retryAdminProvisioningRun(token, runId)
    setNotice(`Повтор поставлен в очередь. Новый ID запуска: ${response.runId}`)
    await loadAll(token)
  })

  const handleDeployProvisioningRun = (runId: string) => {
    return runAction(`deploy-run-${runId}`, async () => {
      const response = await api.deployAdminProvisioningRun(token, runId)
      setNotice(`Развертывание поставлено в очередь. ID запуска: ${response.runId}`)
      await loadAll(token)
    })
  }

  const handleCancelProvisioningRun = (runId: string) => {
    return runAction(`cancel-run-${runId}`, async () => {
      await api.cancelAdminProvisioningRun(token, runId)
      setNotice('Запуск подготовки сервера отменен.')
      await loadAll(token)
    })
  }

  const handleProvisioningSupportNeeded = (runId: string) => runAction(`support-run-${runId}`, async () => {
    const response = await api.markAdminProvisioningSupportNeeded(token, runId)
    setNotice(`Обращение в поддержку: ${response.supportConversationId}`)
    await loadAll(token)
  })

  const handleSaveBotSettings = () => runAction('bot-settings', async () => {
    const saved = await api.updateAdminTelegramBotSettings(token, botSettingsForm)
    setBotSettings(saved)
    setBotSettingsForm((current) => ({ ...current, botToken: '', secretToken: '' }))
    setNotice('Настройки Telegram-бота сохранены. Токены остаются скрытыми и не возвращаются из API.')
  })

  if (!token) {
    return (
      <PageShell title="Админ-панель VPN Platform">
        <SkipLink href="#admin-login" />
        <main id="admin-login" className="admin-login-shell" tabIndex={-1}>
          <section className="admin-login-intro" aria-label="Возможности админ-панели">
            <p className="eyebrow">VPN Platform Admin</p>
            <h2>Единый центр управления продажей VPN</h2>
            <p>Настраивайте тарифы, платежных провайдеров, Telegram-ботов, VPN-серверы, панели 3x-ui и выдачу доступов из одной панели.</p>
            <div className="admin-login-metrics">
              <span><strong>11</strong> разделов</span>
              <span><strong>9</strong> провайдеров</span>
              <span><strong>24/7</strong> контроль</span>
            </div>
          </section>
          <Card>
            <div className="login-panel-header">
              <div>
                <p className="eyebrow">Управление платформой</p>
                <h2 className="page-heading">Вход администратора</h2>
              </div>
              <ValidationModeBadge label="Доступ только для администраторов" />
            </div>
            <form className="admin-login-form" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleLogin() }}>
              <label><span>Email</span><input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="admin@example.com" type="email" autoComplete="email" required /></label>
              <PasswordField label="Пароль" value={password} onChange={setPassword} placeholder="Пароль администратора" autoComplete="current-password" minLength={8} required />
              <PrimaryButton type="submit" disabled={busy || !email || !password} aria-busy={busy}>{busy ? 'Входим...' : 'Войти в админку'}</PrimaryButton>
            </form>
            <p className="safe-note" role="status">{adminAuthRequiredMessage}</p>
            {busy && <LoadingBlock label="Проверяем доступ..." />}
            {notice && <p className="toast-success" role="status" aria-live="polite">{notice}</p>}
            {error && <ErrorBlock message={error} />}
          </Card>
        </main>
      </PageShell>
    )
  }

  return (
    <PageShell title="Админ-панель VPN Platform">
      <SkipLink href="#admin-content" />
      <div className="admin-shell">
        <nav className="admin-sidebar" aria-label="Разделы админ-панели">
          <strong>Навигация</strong>
          <ValidationModeBadge label="Проверочный режим" />
          {adminSections.map(([id, label]) => (
            <a
              key={id}
              href={`#${id}`}
              className={activeSection === id ? 'active' : undefined}
              aria-current={activeSection === id ? 'page' : undefined}
              onClick={() => setActiveSection(id)}
            >
              {label}
            </a>
          ))}
          <small className="muted">Опасные действия требуют подтверждения. Секреты сохраняются скрыто и не возвращаются из API.</small>
        </nav>
        <div id="admin-content" className="admin-main" tabIndex={-1}>
      <div className="page-intro">
        <div>
          <p className="eyebrow">Администрирование</p>
          <h2 className="page-heading">{activeSectionLabel}</h2>
          <p className="muted no-margin-bottom">В каждой вкладке показаны только настройки и действия выбранного раздела.</p>
        </div>
        <div className="admin-session-actions">
          <ValidationModeBadge label="Внешние Telegram, оплаты, 3x-ui и VPS отключены" />
          <PrimaryButton type="button" disabled={busy} aria-busy={busy} className="button-secondary" onClick={() => void loadAll(token)}>Обновить данные</PrimaryButton>
          <PrimaryButton type="button" disabled={busy} aria-busy={busy} className="button-secondary" onClick={clearAdminSession}>Завершить сессию</PrimaryButton>
        </div>
      </div>

      {busy && <LoadingBlock label="Загружаем данные admin-panel..." />}
      {notice && <p className="toast-success" role="status" aria-live="polite">{notice}</p>}
      {error && <ErrorBlock message={error} />}
      {loadErrors.length > 0 && <CodeBlock>{loadErrors.map((item) => `${item.area}: ${item.message}`).join('\n')}</CodeBlock>}

      <div id="dashboard" className="grid section" hidden={activeSection !== 'dashboard'}>
        <StatTile label="Всего пользователей" value={derivedSummary.totalUsers} />
        <StatTile label="Telegram-пользователи" value={derivedSummary.telegramUsers} />
        <StatTile label="Активные подписки" value={derivedSummary.activeSubscriptions} />
        <StatTile label="Скоро истекают" value={derivedSummary.expiringSubscriptions} />
        <StatTile label="Оплачено / ожидает" value={`${derivedSummary.paidOrders} / ${derivedSummary.pendingOrders}`} />
        <StatTile label="Неуспешные платежи" value={derivedSummary.failedPayments} />
        <StatTile label="Свежие платежи / заказы" value={`${derivedSummary.recentPayments} / ${derivedSummary.recentOrders}`} />
        <StatTile label="VPN-доступы" value={derivedSummary.vpnAccessesCount} />
        <StatTile label="VPN-серверы" value={`${derivedSummary.healthyVpnNodes}/${derivedSummary.vpnNodesCount} OK`} />
        <StatTile label="3x-ui панели" value={`${derivedSummary.healthyVpnPanels}/${derivedSummary.vpnPanelsCount} OK`} />
        <StatTile label="Очередь поддержки" value={`${derivedSummary.openSupportConversations}/${derivedSummary.supportConversationsCount}`} />
        <StatTile label="Ошибки подготовки" value={derivedSummary.provisioningErrors} />
      </div>

      <div className="section card-list-two" hidden={activeSection !== 'dashboard'}>
        <SectionCard title="Последние заказы" description="Последние заказы с оплатой и связанной подпиской.">
          {orders.length === 0 ? <EmptyState title="Заказов пока нет" description="После покупок на сайте или в Telegram здесь появятся заказы." /> : (
            <div className="list-stack">
              {orders.slice(0, 5).map((order) => <div key={order.id} className="list-item"><div><strong>{order.tariffName || shortId(order.tariffId)}</strong><div className="muted">{order.userEmail || shortId(order.userId)} · {order.amount} {order.currency} · {formatDate(order.createdAt || order.expiresAt)}</div></div><StatusBadge value={order.status} /></div>)}
            </div>
          )}
        </SectionCard>
        <SectionCard title="Требует внимания" description="Быстрый список очередей, которые требуют реакции.">
          <div className="list-stack">
            {payments.filter((item) => item.status === 'Failed' || item.status === 'Cancelled').slice(0, 3).map((payment) => <div key={payment.id} className="list-item"><span>Платеж {shortId(payment.id)} · {payment.provider}</span><StatusBadge value={payment.status} /></div>)}
            {provisioningRuns.filter((run) => ['Failed', 'PrecheckFailed'].includes(run.status)).slice(0, 3).map((run) => <div key={run.id} className="list-item"><span>{run.targetHost || run.nodeName || shortId(run.id)} · {run.errorSummary || run.currentStep || 'нужна проверка'}</span><StatusBadge value={run.status} /></div>)}
            {supportConversations.filter((conversation) => conversation.status !== 'closed').slice(0, 3).map((conversation) => <div key={conversation.id} className="list-item"><span>{conversation.subject || 'Support'} · tg:{conversation.telegramUserId ?? '—'}</span><StatusBadge value={conversation.status} /></div>)}
            {payments.filter((item) => item.status === 'Failed' || item.status === 'Cancelled').length === 0 && provisioningRuns.filter((run) => ['Failed', 'PrecheckFailed'].includes(run.status)).length === 0 && supportConversations.filter((conversation) => conversation.status !== 'closed').length === 0 && <EmptyState title="Нет срочных проблем" description="Ошибок оплат, подготовки серверов и открытых обращений сейчас нет." />}
          </div>
        </SectionCard>
      </div>

      <div id="users" className="section card-list-two" hidden={activeSection !== 'users'}>
        <Card>
          <h3>Пользователи</h3>
          <form className="toolbar toolbar-form" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void loadUsers() }}>
            <label><span>Поиск</span><input value={userSearch} onChange={(e) => setUserSearch(e.target.value)} placeholder="email, имя или реферальный код" /></label>
            <label><span>Статус</span><select value={userStatusFilter} onChange={(e) => setUserStatusFilter(e.target.value)}><option value="">Все</option><option value="Active">Active</option><option value="Suspended">Suspended</option><option value="Deleted">Deleted</option><option value="New">New</option></select></label>
            <PrimaryButton type="submit" disabled={!token || busy} title={adminDisabledTitle} aria-busy={busy}>Применить</PrimaryButton>
          </form>
          <div className="list-stack mt-12">
            {users.length === 0 && <EmptyState title="Пользователи не найдены" description="Попробуйте изменить поиск или статус." />}
            {users.slice(0, 20).map((user) => (
              <div key={String(user.id)} className={`list-item${selectedUserId === String(user.id ?? '') ? ' selected-item' : ''}`}>
                <div>
                  <strong>{s(user.displayName, 'Без имени')}</strong>
                  <div className="muted">{s(user.email)} · {s(user.authSource)} · {s(user.referralCode)}</div>
                </div>
                <div className="actions">
                  <StatusBadge value={s(user.status, 'Unknown')} />
                  <StatusBadge value={s(user.rolesCsv, 'User')} />
                  <PrimaryButton className={selectedUserId === String(user.id ?? '') ? 'button-secondary' : 'button-ghost'} onClick={() => setSelectedUserId(String(user.id ?? ''))}>{selectedUserId === String(user.id ?? '') ? 'Открыто' : 'Открыть'}</PrimaryButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
        <Card>
          <h3>Карточка пользователя</h3>
          {!userOverview && <p className="muted">Выберите пользователя.</p>}
          {userOverview && <>
            <div className="list-item-vertical">
              <strong>{s(userOverview.user.displayName)}</strong>
              <div className="muted">{s(userOverview.user.email)} · roles {s(userOverview.user.rolesCsv)} · tg accounts {userOverview.telegramAccounts.length}</div>
              <div className="muted">orders {userOverview.orders.length} · payments {userOverview.payments.length} · subscriptions {userOverview.subscriptions.length} · accesses {userOverview.accessCredentials.length}</div>
            </div>
            <h4>Покупки и доступы</h4>
            <div className="list-stack">
              {userOverview.orders.slice(0, 4).map((order) => <div key={order.id} className="list-item"><span>{order.tariffName || shortId(order.tariffId)} · {order.amount} {order.currency}</span><StatusBadge value={order.status} /></div>)}
              {userOverview.accessCredentials.slice(0, 4).map((access) => <div key={access.id} className="list-item"><span>{access.providerType} · {access.serverName || shortId(access.serverId)}</span><StatusBadge value={access.status} /></div>)}
            </div>
          </>}
        </Card>
      </div>

      <div id="payments" className="section card-list-two" hidden={activeSection !== 'payments'}>
        <Card>
          <h3>{editingProviderAccountId ? 'Редактирование способа оплаты' : 'Способы оплаты'}</h3>
          <p className="muted">Добавьте платежный аккаунт, включите его и проверьте готовность к оплатам. Секреты сохраняются скрыто.</p>
          <form aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveProviderAccount() }}>
            <fieldset className="form-section">
              <legend>Основные параметры</legend>
              <div className="form-grid">
                <label><span>Платежная система</span><select value={providerForm.provider} onChange={(e) => updateProviderForm('provider', e.target.value as PaymentProvider)}>{paymentProviderOptions.map((item) => <option key={item} value={item}>{item}</option>)}</select></label>
                <label><span>Режим</span><select value={providerForm.mode} onChange={(e) => updateProviderForm('mode', e.target.value as PaymentProviderMode)}><option value="Disabled">Выключено</option><option value="Sandbox">Проверка</option><option value="Production">Рабочий</option></select></label>
                <label><span>Внутреннее имя</span><input value={providerForm.name} onChange={(e) => updateProviderForm('name', e.target.value)} placeholder="yookassa-sandbox" required /></label>
                <label><span>Название для пользователя</span><input value={providerForm.publicName} onChange={(e) => updateProviderForm('publicName', e.target.value)} placeholder="YooKassa" required /></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Подключение и безопасность</legend>
              <div className="form-grid">
                <label><span>ShopId / MerchantLogin</span><input value={providerForm.shopId} onChange={(e) => updateProviderForm('shopId', e.target.value)} placeholder="Идентификатор магазина" /></label>
                <label><span>API base URL</span><input value={providerForm.apiBaseUrl} onChange={(e) => updateProviderForm('apiBaseUrl', e.target.value)} placeholder="https://api.provider.example" type="url" inputMode="url" /></label>
                <SecretField label="Секретный ключ" value={providerForm.secretKey ?? ''} onChange={(value) => updateProviderForm('secretKey', value)} />
                <SecretField label="Секрет webhook" value={providerForm.webhookSecret ?? ''} onChange={(value) => updateProviderForm('webhookSecret', value)} />
                <label><span>Адрес возврата после оплаты</span><input value={providerForm.returnUrl} onChange={(e) => updateProviderForm('returnUrl', e.target.value)} placeholder="https://example.com/checkout" type="url" inputMode="url" /></label>
                <label><span>Webhook URL</span><input value={providerForm.webhookUrl} onChange={(e) => updateProviderForm('webhookUrl', e.target.value)} placeholder="https://api.example.com/api/webhooks/payments/provider" type="url" inputMode="url" /></label>
                <label><span>Allowed IP ranges</span><input value={providerForm.allowedWebhookIpRangesCsv} onChange={(e) => updateProviderForm('allowedWebhookIpRangesCsv', e.target.value)} placeholder="185.71.76.0/27, 185.71.77.0/27" /></label>
              </div>
              <label><span>Extra settings JSON</span><textarea value={providerForm.extraSettingsJson} onChange={(e) => updateProviderForm('extraSettingsJson', e.target.value)} placeholder={editingProviderAccountId ? 'Оставьте пустым, чтобы сохранить текущий JSON' : '{"hostedCheckoutUrl":"https://pay.example.test/widget"}'} rows={4} /></label>
              {editingProviderAccountId && <p className="muted">При редактировании пустые поля секретов и Extra settings JSON сохраняют текущие значения. Чтобы заменить их, введите новые значения явно.</p>}
              <div className="toolbar">
                <label className="checkbox-row"><input checked={providerForm.isEnabled} onChange={(e) => updateProviderForm('isEnabled', e.target.checked)} type="checkbox" /> Включен</label>
                <label className="checkbox-row"><input checked={providerForm.isDefault} onChange={(e) => updateProviderForm('isDefault', e.target.checked)} type="checkbox" /> По умолчанию</label>
                <label className="checkbox-row"><input checked={providerForm.useWebhookIpAllowList} onChange={(e) => updateProviderForm('useWebhookIpAllowList', e.target.checked)} type="checkbox" /> Ограничить webhook по IP</label>
              </div>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || !token || !providerForm.name} title={adminDisabledTitle} aria-busy={busy}>{editingProviderAccountId ? 'Сохранить изменения' : 'Сохранить способ оплаты'}</PrimaryButton>
              {editingProviderAccountId && <PrimaryButton type="button" className="button-ghost" onClick={resetProviderForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Готовность оплат</h3>
          <div className="list-stack">
            {paymentProviderAccounts.length === 0 && <EmptyState title="Способы оплаты не настроены" description="Добавьте проверочный или рабочий аккаунт. Пользователи увидят его только после готовности к оплатам." />}
            {paymentProviderAccounts.map((account) => (
              <div key={account.id} className="list-item-vertical">
                <div className="card-head">
                  <div>
                    <strong>{account.publicName}</strong>
                    <div className="muted">{account.provider} · {account.mode} · {account.name}</div>
                    <div className="muted">shopId: {account.shopId || '—'} · секрет: {account.hasSecretKey ? 'задан' : 'пусто'} · webhook: {account.hasWebhookSecret ? 'задан' : 'пусто'}</div>
                    <div className="muted">API: {account.apiBaseUrl || '—'} · return: {account.returnUrl || '—'} · webhook URL: {account.webhookUrl || '—'}</div>
                    <div className="muted">IP allow list: {account.useWebhookIpAllowList ? (account.allowedWebhookIpRangesCsv || 'включен, список пуст') : 'не используется'} · extra: {account.extraSettingsJson && account.extraSettingsJson !== '{}' ? 'задан' : 'пусто'}</div>
                    <div className="muted">Capabilities: {capabilities(account).join(', ') || '—'}</div>
                    {providerCheckResults[account.id] && <div className="muted">Последняя проверка: {providerCheckResults[account.id].healthStatus} · {providerCheckResults[account.id].details.join(' · ')}</div>}
                  </div>
                  <div className="status-stack">
                    <StatusBadge value={account.isEnabled ? 'Enabled' : 'Disabled'} />
                    <StatusBadge value={providerConfigured(account) ? 'Checkout ready' : 'Not configured'} />
                  </div>
                </div>
                <div className="muted">{providerIssue(account)}</div>
                <div className="toolbar">
                  <PrimaryButton className="button-secondary" onClick={() => editProviderAccount(account)}>Редактировать</PrimaryButton>
                  <PrimaryButton className="button-secondary" disabled={actionBusyId === `provider-check-${account.id}`} onClick={() => void handleCheckProviderAccount(account)}>Проверить</PrimaryButton>
                  {account.isEnabled ? <ConfirmButton className="button-danger" disabled={actionBusyId === account.id} message={`Отключить способ оплаты "${account.publicName}"? Пользователи больше не увидят его при оплате.`} onConfirm={() => void handleSetProviderEnabled(account, false)}>Выключить</ConfirmButton> : <PrimaryButton className="button-ghost" disabled={actionBusyId === account.id} onClick={() => void handleSetProviderEnabled(account, true)}>Включить</PrimaryButton>}
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div className="section card-list-two" hidden={activeSection !== 'payments'}>
        <Card>
          <h3>Заказы</h3>
          <div className="list-stack">
            {orders.length === 0 && <EmptyState title="Заказов нет" description="Новые покупки и продления появятся здесь." />}
            {orders.slice(0, 12).map((order) => <div key={order.id} className="list-item-vertical"><div className="item-head"><strong>{order.amount} {order.currency}</strong><StatusBadge value={order.status} /></div><div className="muted">Пользователь: {order.userDisplayName || shortId(order.userId)} · тариф: {order.tariffName || shortId(order.tariffId)} · канал: {order.channel ?? '—'} · оплата: {order.paymentProvider ?? '—'}</div><div className="muted">Оплачен: {formatDate(order.paidAt)} · попыток оплаты: {order.paymentAttemptsCount ?? 0} · подписка: {shortId(order.linkedSubscriptionId)}</div></div>)}
          </div>
        </Card>
        <Card>
          <h3>Платежи, вебхуки и возвраты</h3>
          <div className="list-stack">
            {payments.length === 0 && <EmptyState title="Платежей нет" description="История попыток оплаты появится после покупок." />}
            {payments.slice(0, 8).map((payment) => <div key={payment.id} className="list-item-vertical"><div className="item-head"><strong>{payment.provider} · {payment.amount} {payment.currency}</strong><StatusBadge value={payment.status} /></div><div className="muted">Заказ: {shortId(payment.orderId)} · транзакция: {payment.providerPaymentId || '—'} · активация: {payment.isActivationProcessed ? 'обработана' : 'ожидает'}</div><div className="toolbar"><PrimaryButton disabled={actionBusyId === payment.id} onClick={() => void handleRecheckPayment(payment.id)}>Проверить статус</PrimaryButton><ConfirmButton disabled={actionBusyId === payment.id || payment.status !== 'Succeeded'} className="button-secondary" message={`Вернуть платеж ${payment.amount} ${payment.currency}? Действие будет записано в аудит.`} onConfirm={() => void handleRefundPayment(payment)}>Вернуть платеж</ConfirmButton></div></div>)}
            {paymentWebhookEvents.slice(0, 4).map((event) => <div key={event.id} className="list-item"><span>{event.provider} · {event.eventType} · подпись {event.signatureValidated ? 'проверена' : 'не проверена'}</span><StatusBadge value={event.status} /></div>)}
            {refunds.slice(0, 4).map((refund) => <div key={refund.id} className="list-item"><span>Возврат {refund.amount} {refund.currency} · {refund.providerRefundId || shortId(refund.id)}</span><StatusBadge value={refund.status} /></div>)}
          </div>
        </Card>
      </div>

      <div id="tariffs" className="section card-list-two" hidden={activeSection !== 'tariffs'}>
        <Card>
          <h3>{editingTariffId ? 'Редактирование тарифа' : 'Новый тариф'}</h3>
          <form aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveTariff() }}>
            <fieldset className="form-section">
              <legend>Цена и срок</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={tariffForm.name ?? ''} onChange={(e) => updateTariffForm('name', e.target.value)} placeholder="Например, Месяц VPN" required /></label>
                <label><span>Slug</span><input value={tariffForm.slug ?? ''} onChange={(e) => updateTariffForm('slug', e.target.value)} placeholder="month-vpn" /></label>
                <label><span>Цена</span><input value={tariffForm.price ?? 0} onChange={(e) => updateTariffForm('price', Number(e.target.value) || 0)} type="number" min={0} step="1" placeholder="490" /></label>
                <label><span>Валюта</span><input value={tariffForm.currency ?? 'RUB'} onChange={(e) => updateTariffForm('currency', e.target.value)} placeholder="RUB" /></label>
                <label><span>Срок, дней</span><input value={tariffForm.durationDays ?? 30} onChange={(e) => updateTariffForm('durationDays', Number(e.target.value) || 0)} type="number" min={1} step="1" placeholder="30" /></label>
                <label><span>Устройств</span><input value={tariffForm.maxDevices ?? 3} onChange={(e) => updateTariffForm('maxDevices', Number(e.target.value) || 0)} type="number" min={1} step="1" placeholder="3" /></label>
                <label><span>Лимит трафика, ГБ</span><input value={tariffForm.trafficLimit ? Math.round(tariffForm.trafficLimit / 1024 / 1024 / 1024) : ''} onChange={(e) => updateTariffForm('trafficLimit', e.target.value ? Number(e.target.value) * 1024 * 1024 * 1024 : null)} type="number" min={0} step="1" placeholder="Без лимита" /></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Порядок</span><input value={tariffForm.sortOrder ?? 100} onChange={(e) => updateTariffForm('sortOrder', Number(e.target.value) || 0)} type="number" min={0} step="1" placeholder="100" /></label>
                <label><span>Категория</span><input value={tariffForm.category ?? 'default'} onChange={(e) => updateTariffForm('category', e.target.value)} placeholder="default" /></label>
                <label><span>Бейдж</span><input value={tariffForm.badge ?? ''} onChange={(e) => updateTariffForm('badge', e.target.value)} placeholder="Популярный, Выгодно, Семейный" /></label>
                <label><span>Сценарий выдачи</span><select value={tariffForm.provisioningScenario ?? 'auto'} onChange={(e) => updateTariffForm('provisioningScenario', e.target.value)}><option value="auto">auto</option>{workScenarios.map((scenario) => <option key={scenario.id} value={scenario.key}>{scenario.name} ({scenario.key})</option>)}</select></label>
              </div>
              <label><span>Короткое описание</span><textarea value={tariffForm.description ?? ''} onChange={(e) => updateTariffForm('description', e.target.value)} placeholder="Коротко для карточки тарифа" rows={3} /></label>
              <label><span>Полное описание</span><textarea value={tariffForm.fullDescription ?? ''} onChange={(e) => updateTariffForm('fullDescription', e.target.value)} placeholder="Подробное описание для публичной страницы" rows={4} /></label>
              <label><span>Преимущества, по одному в строке</span><textarea value={tariffFeaturesText} onChange={(e) => setTariffFeaturesText(e.target.value)} placeholder={'3 устройства\nАвтоматическая выдача\nQR-код в кабинете'} rows={5} /></label>
              <label><span>Текст после оплаты</span><textarea value={tariffForm.afterPaymentText ?? ''} onChange={(e) => updateTariffForm('afterPaymentText', e.target.value)} placeholder="Что увидит пользователь после покупки" rows={3} /></label>
              <div className="form-grid">
                <label><span>Разрешенные регионы</span><input value={tariffForm.allowedRegionsCsv ?? ''} onChange={(e) => updateTariffForm('allowedRegionsCsv', e.target.value)} placeholder="eu,us" /></label>
                <label><span>Группы серверов</span><input value={tariffForm.allowedNodeGroupsCsv ?? ''} onChange={(e) => updateTariffForm('allowedNodeGroupsCsv', e.target.value)} placeholder="default,premium" /></label>
              </div>
              <label className="checkbox-row"><input checked={tariffForm.isActive !== false} onChange={(e) => updateTariffForm('isActive', e.target.checked)} type="checkbox" /> Показывать пользователям</label>
              <label className="checkbox-row"><input checked={tariffForm.isTrial === true} onChange={(e) => updateTariffForm('isTrial', e.target.checked)} type="checkbox" /> Пробный тариф</label>
              <label className="checkbox-row"><input checked={tariffForm.isReferralEligible !== false} onChange={(e) => updateTariffForm('isReferralEligible', e.target.checked)} type="checkbox" /> Участвует в реферальной программе</label>
            </fieldset>
            <div className="tariff-preview">
              <div className="card-head">
                <div>
                  <strong>{tariffForm.name || 'Название тарифа'}</strong>
                  <div className="muted">{tariffForm.description || 'Короткое описание тарифа'}</div>
                </div>
                <div className="status-stack">
                  {tariffForm.badge && <StatusBadge value={tariffForm.badge} />}
                  <StatusBadge value={tariffForm.isActive === false ? 'Disabled' : 'Enabled'} />
                </div>
              </div>
              <div><strong>{tariffForm.price ?? 0} {tariffForm.currency ?? 'RUB'}</strong> / {tariffForm.durationDays ?? 0} дней</div>
              <div className="muted">{tariffForm.maxDevices ?? 0} устройств · сценарий {tariffForm.provisioningScenario || 'auto'}</div>
              {tariffFeaturesText && <ul className="feature-list compact-list">{tariffFeaturesText.split('\n').filter(Boolean).map((feature) => <li key={feature}>{feature}</li>)}</ul>}
            </div>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || busy || !tariffForm.name} title={adminDisabledTitle} aria-busy={busy}>{editingTariffId ? 'Сохранить тариф' : 'Создать тариф'}</PrimaryButton>
              {editingTariffId && <PrimaryButton type="button" className="button-ghost" onClick={resetTariffForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Список тарифов</h3>
          <div className="list-stack">
            {tariffs.length === 0 && <EmptyState title="Тарифов нет" description="Создайте первый тариф, чтобы он появился на странице покупки." />}
            {tariffs.map((tariff) => <div key={tariff.id} className="list-item-vertical"><div className="item-head"><div><strong>{tariff.name}</strong><div className="muted">{tariff.description || '—'}</div><div className="muted">{tariff.durationDays} дней · {tariff.maxDevices} устройств · порядок {tariff.sortOrder ?? 0} · сценарий {tariff.provisioningScenario || 'auto'}</div><div className="muted">{parseTariffFeatures(tariff).join(' · ') || 'Преимущества не заполнены'}</div></div><div className="item-status"><strong>{tariff.price} {tariff.currency}</strong>{tariff.badge && <StatusBadge value={tariff.badge} />}<StatusBadge value={tariff.isActive === false ? 'Disabled' : 'Enabled'} /></div></div><div className="toolbar"><PrimaryButton className="button-secondary" onClick={() => editTariff(tariff)}>Редактировать</PrimaryButton>{tariff.isActive === false ? <PrimaryButton className="button-ghost" disabled={actionBusyId === tariff.id} onClick={() => void handleToggleTariff(tariff)}>Включить</PrimaryButton> : <ConfirmButton className="button-secondary" disabled={actionBusyId === tariff.id} message={`Выключить тариф "${tariff.name}"? Он исчезнет с публичной витрины и из Telegram.`} onConfirm={() => void handleToggleTariff(tariff)}>Выключить</ConfirmButton>}<ConfirmButton className="button-danger" disabled={actionBusyId === `delete-${tariff.id}`} message={`Удалить тариф "${tariff.name}"? Если есть заказы или подписки, API не даст удалить его.`} onConfirm={() => void handleDeleteTariff(tariff)}>Удалить</ConfirmButton></div></div>)}
          </div>
        </Card>
      </div>

      <div id="subscriptions" className="section card-list-two" hidden={activeSection !== 'subscriptions'}>
        <Card>
          <h3>Подписки</h3>
          <div className="list-stack">
            {subscriptions.length === 0 && <EmptyState title="Подписок нет" description="После успешной оплаты подписка появится здесь." />}
            {subscriptions.slice(0, 12).map((subscription) => <div key={subscription.id} className="list-item-vertical"><div className="item-head"><strong>{subscription.tariffName || shortId(subscription.tariffId)}</strong><StatusBadge value={subscription.status} /></div><div className="muted">Пользователь: {shortId(subscription.userId)} · источник: {subscription.sourceChannel ?? '—'} · действует до: {formatDate(subscription.endAt)}</div><div className="muted">Доступ: {shortId(subscription.currentAccessId)} · заказ/платеж: {shortId(subscription.lastPaymentId)} · продлений: {subscription.renewalCount ?? 0}</div><div className="toolbar"><label className="inline-number-field"><span>Дней</span><input value={subscriptionExtendDays[subscription.id] ?? 30} onChange={(e) => setSubscriptionExtendDays((current) => ({ ...current, [subscription.id]: Number(e.target.value) || 0 }))} type="number" min={1} step="1" inputMode="numeric" /></label><PrimaryButton onClick={() => void handleSubscriptionAction(subscription, 'extend')}>Продлить</PrimaryButton><ConfirmButton className="button-secondary" message={`${subscription.status === 'Blocked' ? 'Разблокировать' : 'Заблокировать'} подписку? Это влияет на доступ пользователя.`} onConfirm={() => void handleSubscriptionAction(subscription, subscription.status === 'Blocked' ? 'unblock' : 'block')}>{subscription.status === 'Blocked' ? 'Разблокировать' : 'Заблокировать'}</ConfirmButton><ConfirmButton className="button-danger" message="Отменить подписку? Пользователь может потерять доступ после обработки." onConfirm={() => void handleSubscriptionAction(subscription, 'cancel')}>Отменить</ConfirmButton></div></div>)}
          </div>
        </Card>
      </div>

      <div id="vpn" className="section card-list-two" hidden={activeSection !== 'vpn'}>
        <Card>
          <h3>VPN-доступы</h3>
          <div className="list-stack">
            {accessCredentials.length === 0 && <EmptyState title="VPN-доступы пока не созданы" description="После оплаты здесь появится ссылка подключения, статус и история синхронизаций." />}
            {accessCredentials.slice(0, 12).map((access) => <div key={access.id} className="list-item-vertical"><div className="item-head"><strong>{access.providerType} · {access.providerAccessId || shortId(access.id)}</strong><StatusBadge value={access.status} /></div><div className="muted">Пользователь: {shortId(access.userId)} · подписка: {shortId(access.subscriptionId)} · сервер: {access.serverName || shortId(access.serverId)} · до: {formatDate(access.expiryDate)}</div><div className="muted">Последняя синхронизация: {formatDate(access.lastSyncedAt)} · версия: {access.revision ?? 0} · клиент провайдера: {access.providerAccessId || '—'}</div>{access.accessUri && <CodeBlock>{access.accessUri}</CodeBlock>}{access.history && access.history.length > 0 && <div className="muted">История: {access.history.slice(0, 3).map((h) => `${h.eventType} ${formatDate(h.createdAt)}`).join(' · ')}</div>}{adminQrSvgs[access.id] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: adminQrSvgs[access.id] }} />}<div className="toolbar"><CopyButton value={access.accessUri} label="Скопировать URI" disabled={!access.accessUri} /><PrimaryButton disabled={!access.accessUri || actionBusyId === `qr-${access.id}`} onClick={() => void handleAdminAccessQr(access)}>Показать QR</PrimaryButton>{access.status === 'Disabled' ? <PrimaryButton disabled={actionBusyId.includes(access.id)} className="button-secondary" onClick={() => void handleAccessAction(access, true)}>Включить</PrimaryButton> : <ConfirmButton disabled={actionBusyId.includes(access.id)} className="button-secondary" message="Отключить VPN-доступ? Пользователь потеряет возможность подключаться." onConfirm={() => void handleAccessAction(access, false)}>Отключить</ConfirmButton>}<PrimaryButton disabled={actionBusyId === `sync-${access.id}`} onClick={() => void handleAccessSync(access)}>Синхронизировать</PrimaryButton><ConfirmButton disabled={actionBusyId === `reset-${access.id}`} message="Сбросить трафик по доступу? Действие будет записано в аудит." onConfirm={() => void handleAccessResetTraffic(access)}>Сбросить трафик</ConfirmButton></div></div>)}
          </div>
        </Card>
      </div>

      <div id="nodes" className="section card-list-two" hidden={activeSection !== 'nodes'}>
        <Card>
          <h3>VPN-серверы</h3>
          <div className="list-stack">
            {servers.length === 0 && <EmptyState title="VPN-серверы не добавлены" description="Добавьте сервер или запустите проверку собственного VPS." />}
            {servers.map((server) => <div key={server.id} className="list-item-vertical"><div className="item-head"><div><strong>{server.name}</strong><div className="muted">{server.region}/{server.country} · {server.provider} · {server.host}</div><div className="muted">Datacenter: {server.datacenter || '—'} · приоритет {server.priority} · протоколы {server.supportedProtocolsCsv || '—'} · теги {server.tagsCsv || '—'}</div><div className="muted">Емкость: {server.usedCapacity}/{server.capacity} · новые пользователи: {server.isAvailableForNewUsers ? 'разрешены' : 'закрыты'} · пароль панели: {server.panelPasswordConfigured ? 'задан' : 'пусто'}</div><div className="muted">Панель: {server.panelBaseUrl || '—'} · SSH {server.sshUser ?? 'root'}:{server.sshPort ?? 22} · авторизация: {server.sshAuthMethod || '—'} · доступы: {server.sshCredentialConfigured ? 'заданы' : 'не заданы'}</div></div><div className="item-status"><StatusBadge value={server.status} /><StatusBadge value={server.healthStatus} /></div></div><div className="toolbar"><PrimaryButton className="button-secondary" onClick={() => editServer(server)}>Редактировать</PrimaryButton><PrimaryButton onClick={() => void handleQueuePrecheck(server.id)}>Проверить</PrimaryButton><ConfirmButton className="button-danger" message="Запустить подготовку сервера? В рабочем режиме это может затронуть инфраструктуру." onConfirm={() => void handleQueueProvision(server.id)}>Подготовить</ConfirmButton><ConfirmButton className="button-secondary" message="Перевести сервер в обслуживание? Новые пользователи не должны попадать на него." onConfirm={() => void handleServerMode(server, 'maintenance')}>В обслуживание</ConfirmButton><PrimaryButton className="button-secondary" onClick={() => void handleServerMode(server, 'ready')}>Вернуть в работу</PrimaryButton><ConfirmButton className="button-secondary" message={`${server.isAvailableForNewUsers ? 'Закрыть набор на сервер' : 'Открыть набор на сервер'}? Это изменит распределение новых пользователей.`} onConfirm={() => void handleServerMode(server, server.isAvailableForNewUsers ? 'drain' : 'allocate')}>{server.isAvailableForNewUsers ? 'Закрыть набор' : 'Открыть набор'}</ConfirmButton></div></div>)}
          </div>
        </Card>
        <Card>
          <h3>{editingServerId ? 'Редактировать VPN-сервер' : 'Добавить VPN-сервер'}</h3>
          <form aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveServer() }}>
            <fieldset className="form-section">
              <legend>Идентификация сервера</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={serverForm.name} onChange={(e) => updateServerForm('name', e.target.value)} placeholder="nl-01" required /></label>
                <label><span>Host / DNS</span><input value={serverForm.host} onChange={(e) => updateServerForm('host', e.target.value)} placeholder="vpn.example.com" required /></label>
                <label><span>IP-адрес</span><input value={serverForm.ipAddress} onChange={(e) => updateServerForm('ipAddress', e.target.value)} placeholder="203.0.113.10" /></label>
                <label><span>Провайдер</span><input value={serverForm.provider} onChange={(e) => updateServerForm('provider', e.target.value)} placeholder="hetzner" /></label>
                <label><span>Регион</span><input value={serverForm.region} onChange={(e) => updateServerForm('region', e.target.value)} placeholder="eu" /></label>
                <label><span>Страна</span><input value={serverForm.country} onChange={(e) => updateServerForm('country', e.target.value)} placeholder="NL" /></label>
                <label><span>Datacenter</span><input value={serverForm.datacenter} onChange={(e) => updateServerForm('datacenter', e.target.value)} placeholder="fsn1" /></label>
                <label><span>Емкость</span><input value={serverForm.capacity} onChange={(e) => updateServerForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
                <label><span>Приоритет</span><input value={serverForm.priority} onChange={(e) => updateServerForm('priority', Number(e.target.value) || 0)} placeholder="100" type="number" min={1} step="1" /></label>
                <label><span>Протоколы</span><input value={serverForm.supportedProtocolsCsv ?? ''} onChange={(e) => updateServerForm('supportedProtocolsCsv', e.target.value)} placeholder="vless,vmess,trojan" /></label>
                <label><span>Теги</span><input value={serverForm.tagsCsv ?? ''} onChange={(e) => updateServerForm('tagsCsv', e.target.value)} placeholder="tier:premium,city:amsterdam" /></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>SSH и режим запуска</legend>
              <div className="form-grid">
                <label><span>SSH-пользователь</span><input value={serverForm.sshUser ?? ''} onChange={(e) => updateServerForm('sshUser', e.target.value)} placeholder="root" /></label>
                <label><span>SSH-порт</span><input value={serverForm.sshPort} onChange={(e) => updateServerForm('sshPort', Number(e.target.value) || 22)} placeholder="22" type="number" min={1} max={65535} step="1" /></label>
                <label><span>Метод SSH</span><select value={serverForm.sshAuthMethod ?? 'ssh_key'} onChange={(e) => updateServerForm('sshAuthMethod', e.target.value)}><option value="ssh_key">SSH-ключ</option><option value="password">Пароль</option></select></label>
                <SecretField label="SSH credential" value={serverForm.sshCredential ?? ''} onChange={(value) => updateServerForm('sshCredential', value)} />
                <label><span>Режим запуска</span><select value={serverForm.validationMode ? 'true' : 'false'} onChange={(e) => updateServerForm('validationMode', e.target.value === 'true')}><option value="true">Проверка без реального деплоя</option><option value="false">Рабочий кандидат</option></select></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Панель управления</legend>
              <div className="form-grid">
                <label><span>URL панели</span><input value={serverForm.panelBaseUrl ?? ''} onChange={(e) => updateServerForm('panelBaseUrl', e.target.value)} placeholder="https://panel.example.com:2053" type="url" inputMode="url" /></label>
                <label><span>Логин панели</span><input value={serverForm.panelUsername ?? ''} onChange={(e) => updateServerForm('panelUsername', e.target.value)} placeholder="admin" /></label>
                <SecretField label="Пароль панели" value={serverForm.panelPassword ?? ''} onChange={(value) => updateServerForm('panelPassword', value)} />
              </div>
            </fieldset>
            <p className="muted">SSH-доступ защищается API и не возвращается обратно. Проверочный режим не выполняет реальный SSH-деплой.</p>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || !token || !serverForm.name || !serverForm.host} title={adminDisabledTitle} aria-busy={busy}>{editingServerId ? 'Сохранить сервер' : 'Создать сервер'}</PrimaryButton>
              {editingServerId && <PrimaryButton type="button" className="button-ghost" onClick={cancelServerEdit}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
      </div>

      <div id="panels" className="section card-list-two" hidden={activeSection !== 'panels'}>
        <Card>
          <h3>{editingVpnPanelId ? 'Редактировать 3x-ui панель' : '3x-ui панели'}</h3>
          <p className="safe-note">В проверочном режиме тест и синхронизация идут через безопасный путь без реального подключения к 3x-ui.</p>
          <form aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveVpnPanel() }}>
            <fieldset className="form-section">
              <legend>Доступ к панели</legend>
              <div className="form-grid">
                <label><span>Название панели</span><input value={vpnPanelForm.name} onChange={(e) => updateVpnPanelForm('name', e.target.value)} placeholder="main-3xui" required /></label>
                <label><span>Адрес панели</span><input value={vpnPanelForm.baseUrl} onChange={(e) => updateVpnPanelForm('baseUrl', e.target.value)} placeholder="https://panel.example.com:2053" type="url" inputMode="url" required /></label>
                <label><span>Логин</span><input value={vpnPanelForm.login} onChange={(e) => updateVpnPanelForm('login', e.target.value)} placeholder="admin" /></label>
                <PasswordField label="Пароль панели" value={vpnPanelForm.password ?? ''} onChange={(value) => updateVpnPanelForm('password', value)} placeholder={editingVpnPanelId ? 'Оставьте пустым, чтобы сохранить текущий пароль' : 'Хранится зашифрованным'} autoComplete="new-password" />
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Распределение нагрузки</legend>
              <div className="form-grid">
                <label><span>Регион</span><input value={vpnPanelForm.region} onChange={(e) => updateVpnPanelForm('region', e.target.value)} placeholder="eu" /></label>
                <label><span>Емкость</span><input value={vpnPanelForm.capacity} onChange={(e) => updateVpnPanelForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
                <label><span>SSL verification</span><select value={vpnPanelForm.sslVerificationMode} onChange={(e) => updateVpnPanelForm('sslVerificationMode', e.target.value)}><option value="Strict">Strict</option><option value="AllowSelfSigned">AllowSelfSigned</option><option value="Disabled">Disabled</option></select></label>
                <label><span>API variant</span><select value={vpnPanelForm.apiVariant} onChange={(e) => updateVpnPanelForm('apiVariant', e.target.value)}><option value="X3UiOfficial">X3UiOfficial</option><option value="ThreeXUi">ThreeXUi</option><option value="LegacyXUi">LegacyXUi</option><option value="Custom">Custom</option></select></label>
              </div>
              <label className="checkbox-row"><input checked={vpnPanelForm.autoCreateInbound} onChange={(e) => updateVpnPanelForm('autoCreateInbound', e.target.checked)} type="checkbox" /> Автоматически создавать inbound при выдаче доступа</label>
              <label><span>Шаблон inbound JSON</span><textarea value={vpnPanelForm.defaultInboundTemplateJson} onChange={(e) => updateVpnPanelForm('defaultInboundTemplateJson', e.target.value)} rows={4} placeholder='{"remark":"default-vless","protocol":"vless","port":443}' /></label>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || !token || !vpnPanelForm.name || !vpnPanelForm.baseUrl} title={adminDisabledTitle} aria-busy={busy}>{editingVpnPanelId ? 'Сохранить панель' : 'Добавить панель'}</PrimaryButton>
              {editingVpnPanelId && <PrimaryButton type="button" className="button-ghost" onClick={cancelVpnPanelEdit}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
          <div className="list-stack mt-12">{vpnPanels.map((panel) => <div key={panel.id} className={`list-item-vertical${selectedVpnPanelId === panel.id ? ' selected-item' : ''}`}><div className="item-head"><div><strong>{panel.name}</strong><div className="muted">{panel.baseUrl} · логин {panel.login ? 'задан' : 'пусто'} · {panel.apiVariant} · SSL {panel.sslVerificationMode}</div><div className="muted">Емкость {panel.usedCapacity}/{panel.capacity} · авто inbound: {panel.autoCreateInbound ? 'включен' : 'выключен'} · версия {panel.version || 'неизвестна'} · синхронизация {formatDate(panel.lastSyncAt)}</div></div><div className="item-status"><StatusBadge value={panel.status} /><StatusBadge value={panel.healthStatus} /></div></div><div className="toolbar"><PrimaryButton className={selectedVpnPanelId === panel.id ? 'button-secondary' : 'button-ghost'} onClick={() => setSelectedVpnPanelId(panel.id)}>{selectedVpnPanelId === panel.id ? 'Открыто' : 'Открыть'}</PrimaryButton><PrimaryButton className="button-secondary" onClick={() => editVpnPanel(panel)}>Редактировать</PrimaryButton><PrimaryButton className="button-secondary" onClick={() => void handleTestVpnPanel(panel.id)}>Проверить</PrimaryButton><PrimaryButton onClick={() => void handleSyncVpnPanel(panel.id)}>Синхронизировать</PrimaryButton></div></div>)}</div>
        </Card>
        <Card>
          <h3>Детали панели</h3>
          <label><span>Панель</span><select value={selectedVpnPanelId} onChange={(e) => setSelectedVpnPanelId(e.target.value)}><option value="">Не выбрана</option>{vpnPanels.map((panel) => <option key={panel.id} value={panel.id}>{panel.name}</option>)}</select></label>
          <h4>Inbound-правила</h4>
          <form aria-busy={actionBusyId === 'create-inbound'} onSubmit={(event) => { event.preventDefault(); void handleCreateInbound() }}>
            <fieldset className="form-section">
              <legend>Параметры правила</legend>
              <div className="form-grid">
                <label><span>Название inbound-правила</span><input value={inboundForm.name} onChange={(e) => updateInboundForm('name', e.target.value)} placeholder="default-vless" required /></label>
                <label><span>Протокол</span><input value={inboundForm.protocol} onChange={(e) => updateInboundForm('protocol', e.target.value)} placeholder="vless" /></label>
                <label><span>Порт</span><input value={inboundForm.port} onChange={(e) => updateInboundForm('port', Number(e.target.value) || 0)} placeholder="443" type="number" min={1} max={65535} step="1" /></label>
                <label><span>Емкость</span><input value={inboundForm.capacity} onChange={(e) => updateInboundForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
              </div>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!selectedVpnPanelId || actionBusyId === 'create-inbound'} aria-busy={actionBusyId === 'create-inbound'}>Создать inbound-правило</PrimaryButton>
            </div>
          </form>
          <div className="list-stack mt-12">{vpnInbounds.map((inbound) => <div key={inbound.id} className="list-item"><div><strong>{inbound.name}</strong><div className="muted">{inbound.protocol}:{inbound.port} · внешний ID {inbound.externalInboundId} · {inbound.usedCapacity}/{inbound.capacity}</div></div><div className="item-status"><StatusBadge value={inbound.isActive ? 'Active' : 'Inactive'} />{inbound.isDefault ? <StatusBadge value="Default" /> : <PrimaryButton disabled={actionBusyId === inbound.id} onClick={() => void handleSetDefaultInbound(inbound.id)}>Сделать основным</PrimaryButton>}</div></div>)}</div>
          <h4>Клиенты, здоровье и синхронизация</h4>
          <div className="list-stack">{vpnClients.slice(0, 5).map((client) => <div key={client.id} className="list-item"><span>{client.email} · истекает {formatDate(client.expiryTime)}</span><StatusBadge value={client.enable ? 'Enabled' : 'Disabled'} /></div>)}{vpnHealthChecks.slice(0, 3).map((check) => <div key={check.id} className="list-item"><span>{check.version || 'неизвестно'} · {check.latencyMs ?? 0}ms · {check.errorMessage || 'ok'}</span><StatusBadge value={check.status} /></div>)}{vpnSyncRuns.slice(0, 3).map((run) => <div key={run.id} className="list-item"><span>{run.summaryJson || run.errorMessage || shortId(run.id)}</span><StatusBadge value={run.status} /></div>)}</div>
        </Card>
      </div>

      <div id="support" className="section card-list-two" hidden={activeSection !== 'support'}>
        <Card>
          <h3>Обращения в поддержку</h3>
          <div className="list-stack">{supportConversations.length === 0 && <EmptyState title="Нет обращений" description="Сообщения из Telegram support появятся в этом списке." />}{supportConversations.slice(0, 12).map((conversation) => <div key={conversation.id} className={`list-item-vertical${selectedSupportConversationId === conversation.id ? ' selected-item' : ''}`}><div className="item-head"><div><strong>{conversation.subject || 'Обращение в поддержку'}</strong><div className="muted">{conversation.channel} · tg:{conversation.telegramUserId ?? '—'} · пользователь:{shortId(conversation.userId)}</div><div className="muted">Ответственный: {shortId(conversation.assignedToUserId)} · заметка: {conversation.internalNote || '—'}</div></div><StatusBadge value={conversation.status} /></div><div className="toolbar"><PrimaryButton className={selectedSupportConversationId === conversation.id ? 'button-secondary' : 'button-ghost'} onClick={() => setSelectedSupportConversationId(conversation.id)}>{selectedSupportConversationId === conversation.id ? 'Открыто' : 'Открыть'}</PrimaryButton><PrimaryButton className="button-secondary" onClick={() => void handleSupportStatus('pending', conversation.id)}>В ожидание</PrimaryButton><PrimaryButton className="button-secondary" onClick={() => void handleSupportStatus(conversation.status === 'closed' ? 'open' : 'closed', conversation.id)}>{conversation.status === 'closed' ? 'Переоткрыть' : 'Закрыть'}</PrimaryButton></div></div>)}</div>
        </Card>
        <Card>
          <h3>Диалог поддержки</h3>
          <label><span>Обращение</span><select value={selectedSupportConversationId} onChange={(e) => setSelectedSupportConversationId(e.target.value)}><option value="">Не выбрано</option>{supportConversations.map((conversation) => <option key={conversation.id} value={conversation.id}>{conversation.subject || shortId(conversation.id)}</option>)}</select></label>
          <div className="list-stack mt-12">{supportMessages.slice(-12).map((message) => <div key={message.id} className="list-item-vertical"><div className="card-head"><strong>{message.direction}{message.isInternalNote ? ' · внутренняя заметка' : ''}</strong><span className="muted">{formatDate(message.createdAt)}</span></div><div>{message.text}</div></div>)}</div>
          <form className="mt-12" aria-busy={actionBusyId === `support-reply-${selectedSupportConversationId}`} onSubmit={(event) => { event.preventDefault(); void handleReplySupport() }}>
            <label><span>Ответ пользователю</span><textarea value={supportReplyText} onChange={(e) => setSupportReplyText(e.target.value)} rows={3} placeholder="Текст ответа" /></label>
            <PrimaryButton type="submit" disabled={!selectedSupportConversationId || !supportReplyText.trim() || actionBusyId === `support-reply-${selectedSupportConversationId}`} aria-busy={actionBusyId === `support-reply-${selectedSupportConversationId}`}>Отправить через Telegram</PrimaryButton>
          </form>
          <form className="mt-12" aria-busy={actionBusyId === `support-note-${selectedSupportConversationId}`} onSubmit={(event) => { event.preventDefault(); void handleSupportNote() }}>
            <label><span>Внутренняя заметка</span><textarea value={supportNoteText} onChange={(e) => setSupportNoteText(e.target.value)} rows={2} placeholder="Видно только администраторам" /></label>
            <PrimaryButton type="submit" disabled={!selectedSupportConversationId || !supportNoteText.trim() || actionBusyId === `support-note-${selectedSupportConversationId}`} aria-busy={actionBusyId === `support-note-${selectedSupportConversationId}`} className="button-secondary">Добавить заметку</PrimaryButton>
          </form>
        </Card>
      </div>

      <div id="bot" className="section card-list-two" hidden={activeSection !== 'bot'}>
        <Card>
          <h3>Настройки Telegram-бота</h3>
          <div className="list-item-vertical">
            <div className="card-head"><strong>@{botSettings.publicBotUsername || 'не настроен'}</strong><StatusBadge value={botSettings.enabled ? 'Enabled' : 'Disabled'} /></div>
            <div className="muted">Режим {botSettings.mode} · bot token {botSettings.hasBotToken ? botSettings.botTokenMasked || 'скрыт' : 'пусто'} · secret token {botSettings.hasSecretToken ? 'задан' : 'пусто'} · admin chat {botSettings.adminChatId || '—'}</div>
            <div className="muted">Webhook: {botSettings.webhookUrl || '—'} · WebApp: {botSettings.webAppUrl || '—'} · исходные токены никогда не возвращаются API.</div>
          </div>
          <form aria-busy={actionBusyId === 'bot-settings'} onSubmit={(event) => { event.preventDefault(); void handleSaveBotSettings() }}>
            <fieldset className="form-section">
              <legend>Подключение Telegram</legend>
              <div className="form-grid">
                <label><span>Состояние</span><select value={botSettingsForm.enabled ? 'true' : 'false'} onChange={(e) => updateBotForm('enabled', e.target.value === 'true')}><option value="false">Выключен</option><option value="true">Включен</option></select></label>
                <label><span>Режим</span><select value={botSettingsForm.mode ?? 'LongPolling'} onChange={(e) => updateBotForm('mode', e.target.value)}><option value="LongPolling">Long polling</option><option value="Webhook">Webhook</option></select></label>
                <label><span>Public bot username</span><input value={botSettingsForm.publicBotUsername ?? ''} onChange={(e) => updateBotForm('publicBotUsername', e.target.value)} placeholder="vpnplatform_bot" /></label>
                <label><span>Webhook URL</span><input value={botSettingsForm.webhookUrl ?? ''} onChange={(e) => updateBotForm('webhookUrl', e.target.value)} placeholder="https://api.example.com/api/channels/telegram/webhook" type="url" inputMode="url" /></label>
                <label><span>Admin chat id</span><input value={botSettingsForm.adminChatId ?? ''} onChange={(e) => updateBotForm('adminChatId', e.target.value)} placeholder="-1001234567890" /></label>
                <label><span>WebApp URL</span><input value={botSettingsForm.webAppUrl ?? ''} onChange={(e) => updateBotForm('webAppUrl', e.target.value)} placeholder="https://cabinet.example.com" type="url" inputMode="url" /></label>
                <SecretField label="Bot token" configured={botSettings.hasBotToken} value={botSettingsForm.botToken ?? ''} onChange={(value) => updateBotForm('botToken', value)} />
                <SecretField label="Secret token" configured={botSettings.hasSecretToken} value={botSettingsForm.secretToken ?? ''} onChange={(value) => updateBotForm('secretToken', value)} />
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Тексты сценариев</legend>
              <label><span>Приветствие</span><textarea value={botSettingsForm.welcomeText ?? ''} onChange={(e) => updateBotForm('welcomeText', e.target.value)} rows={3} /></label>
              <label><span>Инструкция</span><textarea value={botSettingsForm.instructionText ?? ''} onChange={(e) => updateBotForm('instructionText', e.target.value)} rows={3} /></label>
              <label><span>Текст поддержки</span><textarea value={botSettingsForm.supportText ?? ''} onChange={(e) => updateBotForm('supportText', e.target.value)} rows={3} /></label>
              <label><span>Шаблон после оплаты</span><textarea value={botSettingsForm.afterPaymentTextTemplate ?? ''} onChange={(e) => updateBotForm('afterPaymentTextTemplate', e.target.value)} rows={3} /></label>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || actionBusyId === 'bot-settings'} title={adminDisabledTitle} aria-busy={actionBusyId === 'bot-settings'}>Сохранить настройки бота</PrimaryButton>
            </div>
          </form>
        </Card>
      </div>

      <div id="releases" className="section card-list-two" hidden={activeSection !== 'releases'}>
        <Card>
          <h3>{editingReleaseId ? 'Редактировать релиз' : 'Создать релиз'}</h3>
          <p className="muted">Эти записи показываются пользователям в окне «Что нового» после входа в личный кабинет. Будущие даты публикации не показываются до наступления времени.</p>
          <form aria-busy={actionBusyId === 'release-create' || actionBusyId === `release-update-${editingReleaseId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveRelease() }}>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Release ID</span><input value={releaseForm.releaseId} onChange={(e) => updateReleaseForm('releaseId', e.target.value)} placeholder="2026-05-27-whats-new-module" required /></label>
                <label><span>Версия</span><input value={releaseForm.version} onChange={(e) => updateReleaseForm('version', e.target.value)} placeholder="0.2.0" required /></label>
                <label><span>Дата публикации</span><input value={toDateTimeLocalValue(releaseForm.releasedAt)} onChange={(e) => updateReleaseForm('releasedAt', fromDateTimeLocalValue(e.target.value))} type="datetime-local" required /></label>
                <label><span>Источник</span><select value={releaseForm.source ?? 'manual'} onChange={(e) => updateReleaseForm('source', e.target.value)}><option value="manual">manual</option><option value="agent">agent</option></select></label>
              </div>
              <label className="checkbox-row"><input checked={releaseForm.isActive} onChange={(e) => updateReleaseForm('isActive', e.target.checked)} type="checkbox" /> Опубликован и виден пользователям</label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Описание для пользователей</legend>
              <label><span>Заголовок</span><input value={releaseForm.title} onChange={(e) => updateReleaseForm('title', e.target.value)} placeholder="Что изменилось" maxLength={200} required /></label>
              <label><span>Короткое описание</span><textarea value={releaseForm.summary} onChange={(e) => updateReleaseForm('summary', e.target.value)} rows={3} placeholder="Коротко объясните, где пользователь увидит изменения" required /></label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Пункты релиза</legend>
              <div className="list-stack">
                {releaseForm.items.map((item, index) => (
                  <div key={index} className="release-item-editor">
                    <label><span>Тип</span><select value={item.type} onChange={(e) => updateReleaseItem(index, { type: e.target.value })}><option value="new">Новое</option><option value="improved">Улучшено</option><option value="fixed">Исправлено</option><option value="important">Важно</option></select></label>
                    <label><span>Порядок</span><input value={item.sortOrder} onChange={(e) => updateReleaseItem(index, { sortOrder: Number(e.target.value) || 0 })} type="number" min={0} step="1" /></label>
                    <label className="release-item-text"><span>Текст</span><textarea value={item.text} onChange={(e) => updateReleaseItem(index, { text: e.target.value })} rows={2} placeholder="Пишите для пользователя, без названий файлов и коммитов" required /></label>
                    <PrimaryButton type="button" className="button-ghost" disabled={releaseForm.items.length <= 1} onClick={() => removeReleaseItem(index)}>Убрать</PrimaryButton>
                  </div>
                ))}
              </div>
              <PrimaryButton type="button" className="button-secondary mt-12" onClick={addReleaseItem}>Добавить пункт</PrimaryButton>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || !!actionBusyId || !releaseForm.releaseId || !releaseForm.title || !releaseForm.summary} title={adminDisabledTitle}>
                {editingReleaseId ? 'Сохранить релиз' : 'Создать релиз'}
              </PrimaryButton>
              {editingReleaseId && <PrimaryButton type="button" className="button-secondary" onClick={resetReleaseForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>История релизов</h3>
          <div className="list-stack">
            {appReleases.length === 0 && <EmptyState title="Релизов пока нет" description="Создайте первый релиз или проверьте seed-файл AppReleases/releases.json." />}
            {appReleases.map((release) => (
              <div key={release.id} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{release.title}</strong>
                    <div className="muted">Версия {release.version} · {release.releaseId} · публикация {formatDate(release.releasedAt)}</div>
                    <div className="muted">{release.summary}</div>
                  </div>
                  <div className="item-status"><StatusBadge value={release.isActive ? 'Published' : 'Hidden'} /><StatusBadge value={release.source} /></div>
                </div>
                <div className="list-stack mt-12">
                  {release.items.map((item, index) => <div key={`${release.id}-${index}`} className="list-item"><span>{item.type}: {item.text}</span></div>)}
                </div>
                <div className="toolbar">
                  <PrimaryButton className="button-secondary" onClick={() => editRelease(release)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `release-delete-${release.id}`} message={`Удалить релиз "${release.title}"? Пользователи больше не увидят его в истории.`} onConfirm={() => void handleDeleteRelease(release)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="faq" className="section card-list-two" hidden={activeSection !== 'faq'}>
        <Card>
          <h3>{editingFaqId ? 'Редактировать вопрос' : 'Создать вопрос FAQ'}</h3>
          <p className="muted">Эти вопросы показываются на публичной странице FAQ. Неактивные записи остаются в админке, но скрываются от пользователей.</p>
          <form aria-busy={actionBusyId === 'faq-create' || actionBusyId === `faq-update-${editingFaqId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveFaq() }}>
            <fieldset className="form-section">
              <legend>Содержание</legend>
              <label><span>Вопрос</span><input value={faqForm.question} onChange={(e) => updateFaqForm('question', e.target.value)} placeholder="Как подключиться?" maxLength={300} required /></label>
              <label><span>Ответ</span><textarea value={faqForm.answer} onChange={(e) => updateFaqForm('answer', e.target.value)} rows={5} placeholder="Короткий и понятный ответ для пользователя" required /></label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Категория</span><input value={faqForm.category ?? ''} onChange={(e) => updateFaqForm('category', e.target.value)} placeholder="Оплата" maxLength={120} /></label>
                <label><span>Порядок</span><input value={faqForm.sortOrder} onChange={(e) => updateFaqForm('sortOrder', Number(e.target.value) || 0)} type="number" step="1" /></label>
              </div>
              <label className="checkbox-row"><input checked={faqForm.isActive} onChange={(e) => updateFaqForm('isActive', e.target.checked)} type="checkbox" /> Активен</label>
              <label className="checkbox-row"><input checked={faqForm.showOnHome} onChange={(e) => updateFaqForm('showOnHome', e.target.checked)} type="checkbox" /> Показывать на главной</label>
              <label className="checkbox-row"><input checked={faqForm.showOnFaqPage} onChange={(e) => updateFaqForm('showOnFaqPage', e.target.checked)} type="checkbox" /> Показывать на странице FAQ</label>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || !!actionBusyId || !faqForm.question || !faqForm.answer} title={adminDisabledTitle}>
                {editingFaqId ? 'Сохранить вопрос' : 'Создать вопрос'}
              </PrimaryButton>
              {editingFaqId && <PrimaryButton type="button" className="button-secondary" onClick={resetFaqForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Вопросы FAQ</h3>
          <div className="list-stack">
            {faqEntries.length === 0 && <EmptyState title="FAQ пока пуст" description="Создайте первый вопрос, чтобы он появился на публичной странице." />}
            {faqEntries.map((entry) => (
              <div key={entry.id ?? entry.question} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{entry.question}</strong>
                    <div className="muted">{entry.category ?? 'Общее'} · порядок {entry.sortOrder ?? 0}</div>
                    <div className="muted">{entry.answer}</div>
                  </div>
                  <div className="item-status">
                    <StatusBadge value={entry.isActive === false ? 'Hidden' : 'Active'} />
                    {entry.showOnHome && <StatusBadge value="Home" />}
                    {entry.showOnFaqPage && <StatusBadge value="FAQ" />}
                  </div>
                </div>
                <div className="toolbar">
                  <PrimaryButton className="button-secondary" onClick={() => editFaq(entry)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `faq-delete-${entry.id}`} message={`Удалить вопрос "${entry.question}"?`} onConfirm={() => void handleDeleteFaq(entry)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="content" className="section card-list-two" hidden={activeSection !== 'content'}>
        <Card>
          <h3>{editingSiteContentId ? 'Редактировать блок контента' : 'Создать блок контента'}</h3>
          <p className="muted">Эти поля используются публичной главной страницей. Неактивные блоки остаются в админке, но не попадают в public API.</p>
          <form aria-busy={actionBusyId === 'content-create' || actionBusyId === `content-update-${editingSiteContentId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveSiteContent() }}>
            <fieldset className="form-section">
              <legend>Идентификация</legend>
              <div className="form-grid">
                <label><span>Ключ</span><input value={siteContentForm.key} onChange={(e) => updateSiteContentForm('key', e.target.value)} placeholder="home.hero.title" required /></label>
                <label><span>Группа</span><input value={siteContentForm.group ?? 'home'} onChange={(e) => updateSiteContentForm('group', e.target.value)} placeholder="home" /></label>
                <label><span>Название поля</span><input value={siteContentForm.label ?? ''} onChange={(e) => updateSiteContentForm('label', e.target.value)} placeholder="Hero title" required /></label>
                <label><span>Тип поля</span><select value={siteContentForm.inputType ?? 'text'} onChange={(e) => updateSiteContentForm('inputType', e.target.value)}><option value="text">text</option><option value="textarea">textarea</option><option value="url">url</option></select></label>
                <label><span>Порядок</span><input value={siteContentForm.sortOrder} onChange={(e) => updateSiteContentForm('sortOrder', Number(e.target.value) || 0)} type="number" step="1" /></label>
              </div>
              <label><span>Описание для администратора</span><textarea value={siteContentForm.description ?? ''} onChange={(e) => updateSiteContentForm('description', e.target.value)} rows={2} placeholder="Где используется этот текст" /></label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Текст</legend>
              <label><span>Значение</span><textarea value={siteContentForm.value} onChange={(e) => updateSiteContentForm('value', e.target.value)} rows={siteContentForm.inputType === 'textarea' ? 5 : 3} placeholder="Текст для сайта" /></label>
              <label className="checkbox-row"><input checked={siteContentForm.isActive} onChange={(e) => updateSiteContentForm('isActive', e.target.checked)} type="checkbox" /> Активен и опубликован</label>
            </fieldset>
            <div className="tariff-preview">
              <div className="muted">{siteContentForm.key || 'content.key'}</div>
              <strong>{siteContentForm.label || 'Название поля'}</strong>
              <p>{siteContentForm.value || 'Предпросмотр текста'}</p>
            </div>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || !!actionBusyId || !siteContentForm.key} title={adminDisabledTitle}>{editingSiteContentId ? 'Сохранить блок' : 'Создать блок'}</PrimaryButton>
              {editingSiteContentId && <PrimaryButton type="button" className="button-secondary" onClick={resetSiteContentForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Контент главной</h3>
          <div className="list-stack">
            {siteContentBlocks.length === 0 && <EmptyState title="Контент не настроен" description="Запустите seed или создайте первый блок для главной страницы." />}
            {siteContentBlocks.map((block) => (
              <div key={block.id} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{block.label}</strong>
                    <div className="muted">{block.key} · {block.group} · порядок {block.sortOrder}</div>
                    <div className="muted">{block.value || '—'}</div>
                  </div>
                  <div className="item-status"><StatusBadge value={block.isActive ? 'Published' : 'Hidden'} /><StatusBadge value={block.inputType} /></div>
                </div>
                <div className="toolbar">
                  <PrimaryButton className="button-secondary" onClick={() => editSiteContent(block)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `content-delete-${block.id}`} message={`Удалить блок "${block.label}"? На сайте будет использован fallback-текст из приложения.`} onConfirm={() => void handleDeleteSiteContent(block)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="scenarios" className="section card-list-two" hidden={activeSection !== 'scenarios'}>
        <Card>
          <h3>{editingWorkScenarioId ? 'Редактировать сценарий' : 'Создать сценарий работы'}</h3>
          <p className="muted">Сценарий описывает выдачу VPN после оплаты, поведение при ошибке, возврате, продлении и окончании подписки. Тариф выбирает сценарий по ключу.</p>
          <form aria-busy={actionBusyId === 'scenario-create' || actionBusyId === `scenario-update-${editingWorkScenarioId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveWorkScenario() }}>
            <fieldset className="form-section">
              <legend>Основные параметры</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={workScenarioForm.name} onChange={(e) => updateWorkScenarioForm('name', e.target.value)} placeholder="Автоматическая выдача VPN" required /></label>
                <label><span>Ключ</span><input value={workScenarioForm.key} onChange={(e) => updateWorkScenarioForm('key', e.target.value)} placeholder="auto" required /></label>
                <label><span>VPN-протокол</span><input value={workScenarioForm.vpnProtocol} onChange={(e) => updateWorkScenarioForm('vpnProtocol', e.target.value)} placeholder="vless" /></label>
                <label><span>Режим выдачи</span><select value={workScenarioForm.provisioningMode} onChange={(e) => updateWorkScenarioForm('provisioningMode', e.target.value)}><option value="auto">auto</option><option value="manual">manual</option><option value="hybrid">hybrid</option></select></label>
                <label><span>Правило сервера</span><input value={workScenarioForm.serverSelectionRule} onChange={(e) => updateWorkScenarioForm('serverSelectionRule', e.target.value)} placeholder="least-loaded" /></label>
                <label><span>Правило inbound</span><input value={workScenarioForm.inboundSelectionRule} onChange={(e) => updateWorkScenarioForm('inboundSelectionRule', e.target.value)} placeholder="default" /></label>
                <label><span>Устройств</span><input value={workScenarioForm.maxDevices} onChange={(e) => updateWorkScenarioForm('maxDevices', Number(e.target.value) || 1)} type="number" min={1} step="1" /></label>
                <label><span>Лимит трафика, ГБ</span><input value={workScenarioForm.trafficLimit ? Math.round(workScenarioForm.trafficLimit / 1024 / 1024 / 1024) : ''} onChange={(e) => updateWorkScenarioForm('trafficLimit', e.target.value ? Number(e.target.value) * 1024 * 1024 * 1024 : null)} type="number" min={0} step="1" placeholder="Без лимита" /></label>
                <label><span>Порядок</span><input value={workScenarioForm.sortOrder} onChange={(e) => updateWorkScenarioForm('sortOrder', Number(e.target.value) || 0)} type="number" step="1" /></label>
              </div>
              <label><span>Связанные тарифы JSON</span><textarea value={workScenarioForm.allowedTariffIdsJson} onChange={(e) => updateWorkScenarioForm('allowedTariffIdsJson', e.target.value)} rows={2} placeholder='["tariff-guid"] или []' /></label>
              <label className="checkbox-row"><input checked={workScenarioForm.isActive} onChange={(e) => updateWorkScenarioForm('isActive', e.target.checked)} type="checkbox" /> Активен</label>
              <label className="checkbox-row"><input checked={workScenarioForm.generateQrCode} onChange={(e) => updateWorkScenarioForm('generateQrCode', e.target.checked)} type="checkbox" /> Генерировать QR-код</label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Поведение системы</legend>
              <label><span>После успешной оплаты</span><textarea value={workScenarioForm.onPaymentSucceeded} onChange={(e) => updateWorkScenarioForm('onPaymentSucceeded', e.target.value)} rows={2} /></label>
              <label><span>После ошибки оплаты</span><textarea value={workScenarioForm.onPaymentFailed} onChange={(e) => updateWorkScenarioForm('onPaymentFailed', e.target.value)} rows={2} /></label>
              <label><span>После возврата</span><textarea value={workScenarioForm.onRefund} onChange={(e) => updateWorkScenarioForm('onRefund', e.target.value)} rows={2} /></label>
              <label><span>После окончания подписки</span><textarea value={workScenarioForm.onSubscriptionExpired} onChange={(e) => updateWorkScenarioForm('onSubscriptionExpired', e.target.value)} rows={2} /></label>
              <label><span>После продления</span><textarea value={workScenarioForm.onRenewal} onChange={(e) => updateWorkScenarioForm('onRenewal', e.target.value)} rows={2} /></label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Тексты для пользователя</legend>
              <label><span>Текст для кабинета</span><textarea value={workScenarioForm.cabinetText} onChange={(e) => updateWorkScenarioForm('cabinetText', e.target.value)} rows={3} /></label>
              <label><span>Текст для Telegram</span><textarea value={workScenarioForm.telegramText} onChange={(e) => updateWorkScenarioForm('telegramText', e.target.value)} rows={3} /></label>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || !!actionBusyId || !workScenarioForm.name || !workScenarioForm.key} title={adminDisabledTitle}>{editingWorkScenarioId ? 'Сохранить сценарий' : 'Создать сценарий'}</PrimaryButton>
              {editingWorkScenarioId && <PrimaryButton type="button" className="button-secondary" onClick={resetWorkScenarioForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Сценарии работы</h3>
          <div className="list-stack">
            {workScenarios.length === 0 && <EmptyState title="Сценарии не настроены" description="Создайте сценарий, затем выберите его в тарифе по ключу." />}
            {workScenarios.map((scenario) => (
              <div key={scenario.id} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{scenario.name}</strong>
                    <div className="muted">{scenario.key} · {scenario.vpnProtocol} · {scenario.provisioningMode} · сервер {scenario.serverSelectionRule}</div>
                    <div className="muted">Оплата: {scenario.onPaymentSucceeded} · продление: {scenario.onRenewal}</div>
                    <div className="muted">Тарифы: {tariffs.filter((tariff) => tariff.provisioningScenario === scenario.key).map((tariff) => tariff.name).join(', ') || 'не выбраны'}</div>
                  </div>
                  <div className="item-status"><StatusBadge value={scenario.isActive ? 'Active' : 'Hidden'} /><StatusBadge value={scenario.generateQrCode ? 'QR' : 'No QR'} /></div>
                </div>
                <div className="toolbar">
                  <PrimaryButton className="button-secondary" onClick={() => editWorkScenario(scenario)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `scenario-delete-${scenario.id}`} message={`Удалить сценарий "${scenario.name}"? Если он выбран в тарифе, API не даст удалить его.`} onConfirm={() => void handleDeleteWorkScenario(scenario)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="provisioning" className="section card-list-two" hidden={activeSection !== 'provisioning'}>
        <Card>
          <h3>Подготовка VPS</h3>
          <p className="safe-note">В проверочном режиме реальный SSH/Ansible-деплой выключен, пока это явно не разрешено настройками сервера.</p>
          <div className="list-stack">{provisioningRuns.length === 0 && <EmptyState title="Запусков подготовки нет" description="Проверки и подготовки VPS появятся здесь после Telegram или админ-сценария." />}{provisioningRuns.slice(0, 12).map((run) => <div key={run.id} className="list-item-vertical"><div className="item-head"><strong>{run.nodeName || shortId(run.nodeId)}</strong><StatusBadge value={run.status} /></div><div className="muted">Запуск: {shortId(run.id)} · источник {run.source || '—'} · владелец {run.owner || '—'} · шаг {run.currentStep || run.status}</div><div className="muted">Цель: {run.targetHost || shortId(run.nodeId)}:{run.sshPort ?? 22} · пользователь {run.username || 'root'} · авторизация {run.authMethod || '—'} · доступы {run.credentialsConfigured ? 'заданы' : 'не заданы'} · {run.validationMode ? 'режим проверки' : 'рабочий кандидат'}</div><div className="muted">{run.dryRun ? 'проверка без изменений' : 'развертывание'} · старт {formatDate(run.startedAt)} · финиш {formatDate(run.finishedAt)}</div><div className="muted">{run.errorSummary || run.executionLogPreview || run.executionLog || '—'}</div><div className="toolbar"><PrimaryButton disabled={!token || actionBusyId === `retry-${run.id}`} onClick={() => void handleRetryProvisioningRun(run.id)}>Повторить</PrimaryButton><ConfirmButton disabled={!token || actionBusyId === `deploy-run-${run.id}` || !['ReadyToDeploy', 'Succeeded'].includes(run.status)} className="button-danger" message="Развернуть VPS? В рабочем режиме это может выполнить реальные SSH/Ansible-действия." onConfirm={() => void handleDeployProvisioningRun(run.id)}>Развернуть</ConfirmButton><ConfirmButton disabled={!token || actionBusyId === `cancel-run-${run.id}` || ['Failed', 'PrecheckFailed', 'Deployed', 'Succeeded', 'Cancelled'].includes(run.status)} className="button-secondary" message="Отменить запуск подготовки VPS?" onConfirm={() => void handleCancelProvisioningRun(run.id)}>Отменить</ConfirmButton><PrimaryButton disabled={!token || actionBusyId === `support-run-${run.id}`} onClick={() => void handleProvisioningSupportNeeded(run.id)}>Нужна поддержка</PrimaryButton></div></div>)}</div>
        </Card>
      </div>
        </div>
      </div>
    </PageShell>
  )
}
