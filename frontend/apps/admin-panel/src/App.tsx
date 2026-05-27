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
  OrderDto,
  PaymentAttemptDto,
  PaymentProvider,
  PaymentProviderAccountDto,
  PaymentProviderMode,
  PaymentWebhookEventDto,
  ProvisioningRunDto,
  RefundDto,
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
  durationDays: 30,
  price: 490,
  currency: 'RUB',
  maxDevices: 3,
  isActive: true,
  sortOrder: 100,
  category: 'default'
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

const defaultBotSettings: AdminTelegramBotSettingsDto = {
  enabled: false,
  mode: 'Polling',
  publicBotUsername: '',
  hasBotToken: false,
  botTokenMasked: '',
  webhookUrl: '',
  hasSecretToken: false,
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
  const [paymentWebhookEvents, setPaymentWebhookEvents] = useState<PaymentWebhookEventDto[]>([])
  const [refunds, setRefunds] = useState<RefundDto[]>([])
  const [supportConversations, setSupportConversations] = useState<SupportConversationDto[]>([])
  const [selectedSupportConversationId, setSelectedSupportConversationId] = useState('')
  const [supportMessages, setSupportMessages] = useState<SupportMessageDto[]>([])
  const [supportReplyText, setSupportReplyText] = useState('')
  const [supportNoteText, setSupportNoteText] = useState('')
  const [tariffs, setTariffs] = useState<TariffDto[]>([])
  const [tariffForm, setTariffForm] = useState<UpdateTariffPayload>(defaultTariffForm)
  const [appReleases, setAppReleases] = useState<AppReleaseDto[]>([])
  const [releaseForm, setReleaseForm] = useState<AppReleaseUpsertPayload>(defaultReleaseForm)
  const [editingReleaseId, setEditingReleaseId] = useState('')
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
  const [providerForm, setProviderForm] = useState<UpsertPaymentProviderAccountPayload>(defaultProviderForm)
  const [vpnPanelForm, setVpnPanelForm] = useState<CreateVpnPanelPayload>(defaultVpnPanelForm)
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
    setServers(nextServers)
    setProvisioningRuns(nextRuns)
    setVpnPanels(nextVpnPanels)
    setBotSettings(nextBotSettings)
    setBotSettingsForm({
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
    setPaymentWebhookEvents([])
    setRefunds([])
    setSupportConversations([])
    setSelectedSupportConversationId('')
    setSupportMessages([])
    setTariffs([])
    setAppReleases([])
    setReleaseForm(defaultReleaseForm)
    setEditingReleaseId('')
    setServers([])
    setProvisioningRuns([])
    setVpnPanels([])
    setSelectedVpnPanelId('')
    setVpnInbounds([])
    setVpnClients([])
    setVpnHealthChecks([])
    setVpnSyncRuns([])
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

  const handleCreateProviderAccount = async () => {
    if (!token) return
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const saved = await api.createAdminPaymentProviderAccount(token, providerForm)
      setNotice(`Способ оплаты ${saved.name} сохранен. Секреты не отображаются.`)
      setProviderForm({ ...defaultProviderForm, mode: providerForm.mode, returnUrl: providerForm.returnUrl })
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

  const handleCreateTariff = async () => {
    if (!token) return
    if (!tariffForm.name || n(tariffForm.price) < 0 || n(tariffForm.durationDays) <= 0) {
      setError('Тариф: название обязательно, цена >= 0, срок > 0 дней.')
      return
    }
    setBusy(true)
    setError('')
    try {
      await api.createAdminTariff(token, tariffForm)
      setTariffForm(defaultTariffForm)
      setNotice('Тариф создан.')
      await loadAll(token)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось создать тариф')
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

  const handleCreateVpnPanel = async () => {
    if (!token) return
    setBusy(true)
    setError('')
    try {
      const saved = await api.createAdminVpnPanel(token, vpnPanelForm)
      setNotice(`VPN-панель ${saved.name} сохранена.`)
      setSelectedVpnPanelId(saved.id)
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

  const handleCreateServer = async () => {
    if (!token) return
    setBusy(true)
    setError('')
    try {
      const created = await api.createAdminServer(token, serverForm)
      setNotice(`Сервер ${created.name} создан. Пароль панели не возвращается из API.`)
      setServerForm((current) => ({ ...defaultServerForm, provider: current.provider, region: current.region, country: current.country, datacenter: current.datacenter }))
      await loadAll(token)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось создать сервер')
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
    setNotice('Тексты Telegram-бота сохранены. Токен остается скрытым и здесь не редактируется.')
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
          <h3>Способы оплаты</h3>
          <p className="muted">Добавьте платежный аккаунт, включите его и проверьте готовность к оплатам. Секреты сохраняются скрыто.</p>
          <form aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleCreateProviderAccount() }}>
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
                <SecretField label="Секретный ключ" value={providerForm.secretKey ?? ''} onChange={(value) => updateProviderForm('secretKey', value)} />
                <SecretField label="Секрет webhook" value={providerForm.webhookSecret ?? ''} onChange={(value) => updateProviderForm('webhookSecret', value)} />
                <label><span>Адрес возврата после оплаты</span><input value={providerForm.returnUrl} onChange={(e) => updateProviderForm('returnUrl', e.target.value)} placeholder="https://example.com/checkout" type="url" inputMode="url" /></label>
              </div>
              <div className="toolbar">
                <label className="checkbox-row"><input checked={providerForm.isEnabled} onChange={(e) => updateProviderForm('isEnabled', e.target.checked)} type="checkbox" /> Включен</label>
                <label className="checkbox-row"><input checked={providerForm.isDefault} onChange={(e) => updateProviderForm('isDefault', e.target.checked)} type="checkbox" /> По умолчанию</label>
                <label className="checkbox-row"><input checked={providerForm.useWebhookIpAllowList} onChange={(e) => updateProviderForm('useWebhookIpAllowList', e.target.checked)} type="checkbox" /> Ограничить webhook по IP</label>
              </div>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || !token || !providerForm.name} title={adminDisabledTitle} aria-busy={busy}>Сохранить способ оплаты</PrimaryButton>
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
                    <div className="muted">Capabilities: {capabilities(account).join(', ') || '—'}</div>
                  </div>
                  <div className="status-stack">
                    <StatusBadge value={account.isEnabled ? 'Enabled' : 'Disabled'} />
                    <StatusBadge value={providerConfigured(account) ? 'Checkout ready' : 'Not configured'} />
                  </div>
                </div>
                <div className="muted">{providerIssue(account)}</div>
                {account.isEnabled ? <ConfirmButton className="button-danger" disabled={actionBusyId === account.id} message={`Отключить способ оплаты "${account.publicName}"? Пользователи больше не увидят его при оплате.`} onConfirm={() => void handleSetProviderEnabled(account, false)}>Выключить</ConfirmButton> : <PrimaryButton className="button-ghost" disabled={actionBusyId === account.id} onClick={() => void handleSetProviderEnabled(account, true)}>Включить</PrimaryButton>}
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div className="section card-list-two">
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
          <h3>Тарифы</h3>
          <form aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleCreateTariff() }}>
            <fieldset className="form-section">
              <legend>Цена и срок</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={tariffForm.name ?? ''} onChange={(e) => updateTariffForm('name', e.target.value)} placeholder="Например, Месяц VPN" required /></label>
                <label><span>Slug</span><input value={tariffForm.slug ?? ''} onChange={(e) => updateTariffForm('slug', e.target.value)} placeholder="month-vpn" /></label>
                <label><span>Цена</span><input value={tariffForm.price ?? 0} onChange={(e) => updateTariffForm('price', Number(e.target.value) || 0)} type="number" min={0} step="1" placeholder="490" /></label>
                <label><span>Валюта</span><input value={tariffForm.currency ?? 'RUB'} onChange={(e) => updateTariffForm('currency', e.target.value)} placeholder="RUB" /></label>
                <label><span>Срок, дней</span><input value={tariffForm.durationDays ?? 30} onChange={(e) => updateTariffForm('durationDays', Number(e.target.value) || 0)} type="number" min={1} step="1" placeholder="30" /></label>
                <label><span>Устройств</span><input value={tariffForm.maxDevices ?? 3} onChange={(e) => updateTariffForm('maxDevices', Number(e.target.value) || 0)} type="number" min={1} step="1" placeholder="3" /></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Порядок</span><input value={tariffForm.sortOrder ?? 100} onChange={(e) => updateTariffForm('sortOrder', Number(e.target.value) || 0)} type="number" min={0} step="1" placeholder="100" /></label>
                <label><span>Категория</span><input value={tariffForm.category ?? 'default'} onChange={(e) => updateTariffForm('category', e.target.value)} placeholder="default" /></label>
              </div>
              <label><span>Описание</span><textarea value={tariffForm.description ?? ''} onChange={(e) => updateTariffForm('description', e.target.value)} placeholder="Что получит пользователь" rows={3} /></label>
              <label className="checkbox-row"><input checked={tariffForm.isActive !== false} onChange={(e) => updateTariffForm('isActive', e.target.checked)} type="checkbox" /> Показывать пользователям</label>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || busy || !tariffForm.name} title={adminDisabledTitle} aria-busy={busy}>Создать тариф</PrimaryButton>
            </div>
          </form>
        </Card>
        <Card>
          <h3>Список тарифов</h3>
          <div className="list-stack">
            {tariffs.length === 0 && <EmptyState title="Тарифов нет" description="Создайте первый тариф, чтобы он появился на странице покупки." />}
            {tariffs.map((tariff) => <div key={tariff.id} className="list-item"><div><strong>{tariff.name}</strong><div className="muted">{tariff.description || '—'}</div><div className="muted">{tariff.durationDays} дней · порядок {tariff.sortOrder ?? 0} · выключенный тариф скрыт на сайте и в Telegram</div></div><div className="item-status"><strong>{tariff.price} {tariff.currency}</strong><StatusBadge value={tariff.isActive === false ? 'Disabled' : 'Enabled'} />{tariff.isActive === false ? <PrimaryButton className="button-ghost" disabled={actionBusyId === tariff.id} onClick={() => void handleToggleTariff(tariff)}>Включить</PrimaryButton> : <ConfirmButton className="button-secondary" disabled={actionBusyId === tariff.id} message={`Выключить тариф "${tariff.name}"? Он исчезнет с публичной витрины и из Telegram.`} onConfirm={() => void handleToggleTariff(tariff)}>Выключить</ConfirmButton>}</div></div>)}
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
            {servers.map((server) => <div key={server.id} className="list-item-vertical"><div className="item-head"><div><strong>{server.name}</strong><div className="muted">{server.region}/{server.country} · {server.provider} · {server.host}</div><div className="muted">Емкость: {server.usedCapacity}/{server.capacity} · новые пользователи: {server.isAvailableForNewUsers ? 'разрешены' : 'закрыты'} · пароль панели: {server.panelPasswordConfigured ? 'задан' : 'пусто'}</div><div className="muted">Панель: {server.panelBaseUrl || '—'} · SSH {server.sshUser ?? 'root'}:{server.sshPort ?? 22} · авторизация: {server.sshAuthMethod || '—'} · доступы: {server.sshCredentialConfigured ? 'заданы' : 'не заданы'}</div></div><div className="item-status"><StatusBadge value={server.status} /><StatusBadge value={server.healthStatus} /></div></div><div className="toolbar"><PrimaryButton onClick={() => void handleQueuePrecheck(server.id)}>Проверить</PrimaryButton><ConfirmButton className="button-danger" message="Запустить подготовку сервера? В рабочем режиме это может затронуть инфраструктуру." onConfirm={() => void handleQueueProvision(server.id)}>Подготовить</ConfirmButton><ConfirmButton className="button-secondary" message="Перевести сервер в обслуживание? Новые пользователи не должны попадать на него." onConfirm={() => void handleServerMode(server, 'maintenance')}>В обслуживание</ConfirmButton><PrimaryButton className="button-secondary" onClick={() => void handleServerMode(server, 'ready')}>Вернуть в работу</PrimaryButton><ConfirmButton className="button-secondary" message={`${server.isAvailableForNewUsers ? 'Закрыть набор на сервер' : 'Открыть набор на сервер'}? Это изменит распределение новых пользователей.`} onConfirm={() => void handleServerMode(server, server.isAvailableForNewUsers ? 'drain' : 'allocate')}>{server.isAvailableForNewUsers ? 'Закрыть набор' : 'Открыть набор'}</ConfirmButton></div></div>)}
          </div>
        </Card>
        <Card>
          <h3>Добавить VPN-сервер</h3>
          <form aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleCreateServer() }}>
            <fieldset className="form-section">
              <legend>Идентификация сервера</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={serverForm.name} onChange={(e) => updateServerForm('name', e.target.value)} placeholder="nl-01" required /></label>
                <label><span>Host / DNS</span><input value={serverForm.host} onChange={(e) => updateServerForm('host', e.target.value)} placeholder="vpn.example.com" required /></label>
                <label><span>IP-адрес</span><input value={serverForm.ipAddress} onChange={(e) => updateServerForm('ipAddress', e.target.value)} placeholder="203.0.113.10" /></label>
                <label><span>Провайдер</span><input value={serverForm.provider} onChange={(e) => updateServerForm('provider', e.target.value)} placeholder="hetzner" /></label>
                <label><span>Регион</span><input value={serverForm.region} onChange={(e) => updateServerForm('region', e.target.value)} placeholder="eu" /></label>
                <label><span>Страна</span><input value={serverForm.country} onChange={(e) => updateServerForm('country', e.target.value)} placeholder="NL" /></label>
                <label><span>Емкость</span><input value={serverForm.capacity} onChange={(e) => updateServerForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
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
              <PrimaryButton type="submit" disabled={busy || !token || !serverForm.name || !serverForm.host} title={adminDisabledTitle} aria-busy={busy}>Создать сервер</PrimaryButton>
            </div>
          </form>
        </Card>
      </div>

      <div id="panels" className="section card-list-two" hidden={activeSection !== 'panels'}>
        <Card>
          <h3>3x-ui панели</h3>
          <p className="safe-note">В проверочном режиме тест и синхронизация идут через безопасный путь без реального подключения к 3x-ui.</p>
          <form aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleCreateVpnPanel() }}>
            <fieldset className="form-section">
              <legend>Доступ к панели</legend>
              <div className="form-grid">
                <label><span>Название панели</span><input value={vpnPanelForm.name} onChange={(e) => updateVpnPanelForm('name', e.target.value)} placeholder="main-3xui" required /></label>
                <label><span>Адрес панели</span><input value={vpnPanelForm.baseUrl} onChange={(e) => updateVpnPanelForm('baseUrl', e.target.value)} placeholder="https://panel.example.com:2053" type="url" inputMode="url" required /></label>
                <label><span>Логин</span><input value={vpnPanelForm.login} onChange={(e) => updateVpnPanelForm('login', e.target.value)} placeholder="admin" /></label>
                <PasswordField label="Пароль панели" value={vpnPanelForm.password ?? ''} onChange={(value) => updateVpnPanelForm('password', value)} placeholder="Хранится зашифрованным" autoComplete="new-password" />
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Распределение нагрузки</legend>
              <div className="form-grid">
                <label><span>Регион</span><input value={vpnPanelForm.region} onChange={(e) => updateVpnPanelForm('region', e.target.value)} placeholder="eu" /></label>
                <label><span>Емкость</span><input value={vpnPanelForm.capacity} onChange={(e) => updateVpnPanelForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
              </div>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || !token || !vpnPanelForm.name || !vpnPanelForm.baseUrl} title={adminDisabledTitle} aria-busy={busy}>Добавить панель</PrimaryButton>
            </div>
          </form>
          <div className="list-stack mt-12">{vpnPanels.map((panel) => <div key={panel.id} className={`list-item-vertical${selectedVpnPanelId === panel.id ? ' selected-item' : ''}`}><div className="item-head"><div><strong>{panel.name}</strong><div className="muted">{panel.baseUrl} · логин {panel.login ? 'задан' : 'пусто'} · {panel.apiVariant}</div><div className="muted">Емкость {panel.usedCapacity}/{panel.capacity} · версия {panel.version || 'неизвестна'} · синхронизация {formatDate(panel.lastSyncAt)}</div></div><div className="item-status"><StatusBadge value={panel.status} /><StatusBadge value={panel.healthStatus} /></div></div><div className="toolbar"><PrimaryButton className={selectedVpnPanelId === panel.id ? 'button-secondary' : 'button-ghost'} onClick={() => setSelectedVpnPanelId(panel.id)}>{selectedVpnPanelId === panel.id ? 'Открыто' : 'Открыть'}</PrimaryButton><PrimaryButton className="button-secondary" onClick={() => void handleTestVpnPanel(panel.id)}>Проверить</PrimaryButton><PrimaryButton onClick={() => void handleSyncVpnPanel(panel.id)}>Синхронизировать</PrimaryButton></div></div>)}</div>
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
            <div className="muted">Режим {botSettings.mode} · bot token {botSettings.hasBotToken ? botSettings.botTokenMasked || 'скрыт' : 'пусто'} · secret token {botSettings.hasSecretToken ? 'задан' : 'пусто'}</div>
            <div className="muted">Webhook: {botSettings.webhookUrl || '—'} · исходный token никогда не возвращается API.</div>
          </div>
          <form aria-busy={actionBusyId === 'bot-settings'} onSubmit={(event) => { event.preventDefault(); void handleSaveBotSettings() }}>
            <fieldset className="form-section">
              <legend>Тексты сценариев</legend>
              <label><span>Приветствие</span><textarea value={botSettingsForm.welcomeText ?? ''} onChange={(e) => updateBotForm('welcomeText', e.target.value)} rows={3} /></label>
              <label><span>Инструкция</span><textarea value={botSettingsForm.instructionText ?? ''} onChange={(e) => updateBotForm('instructionText', e.target.value)} rows={3} /></label>
              <label><span>Текст поддержки</span><textarea value={botSettingsForm.supportText ?? ''} onChange={(e) => updateBotForm('supportText', e.target.value)} rows={3} /></label>
              <label><span>Шаблон после оплаты</span><textarea value={botSettingsForm.afterPaymentTextTemplate ?? ''} onChange={(e) => updateBotForm('afterPaymentTextTemplate', e.target.value)} rows={3} /></label>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || actionBusyId === 'bot-settings'} title={adminDisabledTitle} aria-busy={actionBusyId === 'bot-settings'}>Сохранить тексты бота</PrimaryButton>
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
