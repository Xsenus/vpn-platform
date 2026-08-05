import React, { useEffect, useMemo, useState } from 'react'
import {
  AccessCredentialDto,
  AdminAuditLogDto,
  AdminNotificationDeliveryDto,
  AdminDashboardSummaryDto,
  AdminReferralProgramDto,
  AdminRewardLedgerDto,
  AdminSessionDto,
  AdminTelegramBotConnectionCheckDto,
  AdminTelegramBotSettingsDto,
  AdminUserDto,
  AdminUserOverviewDto,
  ApiClient,
  ApiClientError,
  AppReleaseOverviewDto,
  AppReleaseDto,
  AppReleaseUpsertPayload,
  CreateServerPayload,
  CreateVpnInboundPayload,
  CreateVpnPanelPayload,
  FaqOverviewDto,
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
  ReferralProgramUpsertPayload,
  SiteContentBlockUpsertPayload,
  SiteContentBlockDto,
  SiteContentReadinessDto,
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
import { Card, CodeBlock, ConfirmButton, CopyButton, EmptyState, ErrorBlock, FormValidationSummary, LoadingBlock, PageShell, PasswordField, PrimaryButton, SecretField, SectionCard, SkipLink, StatTile, StatusBadge, ValidationModeBadge } from '@vpn-platform/ui'
import { buildAdminUserOverviewStats, formatAdminMoney, telegramDisplayName } from './admin-users'
import { canAccessAdminSection, canWriteAdminSection, type AdminSectionId } from './admin-capabilities'
import { getAdminAccessCommandBlocker, getAdminAccessTerminalReason } from './admin-accesses'
import { getAdminSubscriptionActionAvailability, getAdminSubscriptionActionBlocker, type AdminSubscriptionAction } from './admin-subscriptions'
import { canCancelProvisioningRun, canRetryProvisioningRun, isProvisioningStateConflict } from './provisioning-state'

const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080')
const TOKEN_STORAGE_KEY = 'vpn-platform-admin-token'
const REFRESH_TOKEN_STORAGE_KEY = 'vpn-platform-admin-refresh-token'
const ADMIN_EMAIL_STORAGE_KEY = 'vpn-platform-admin-email'
const yookassaAllowedIps = '185.71.76.0/27,185.71.77.0/27,77.75.153.0/25,77.75.156.11,77.75.156.35,77.75.154.128/25,2a02:5180::/32'
const paymentProviderOptions: PaymentProvider[] = ['YooKassa', 'RoboKassa', 'YooMoney', 'TelegramStars', 'CloudPayments', 'TBankAcquiring', 'Prodamus', 'Stripe', 'PayPal']
const adminAuthRequiredMessage = 'Войдите как администратор, чтобы включить загрузку данных и действия в разделах.'
const adminAccessDeniedMessage = 'У этой учетной записи нет доступа к админ-панели. Войдите с административной ролью.'

type PaymentProviderSetup = {
  title: string
  userName: string
  channel: 'web' | 'telegram'
  summary: string
  shopIdLabel: string
  shopIdPlaceholder: string
  secretLabel: string
  secretPlaceholder: string
  webhookSecretLabel: string
  webhookSecretPlaceholder: string
  apiBaseUrl: string
  apiBaseUrlLabel: string
  returnUrlLabel: string
  webhookUrlLabel: string
  extraSettingsPlaceholder: string
  extraSettingsHint: string
  extraSettingsFields: PaymentProviderExtraSettingsField[]
  allowedIps: string
  useWebhookIpAllowList: boolean
}

type PaymentProviderExtraSettingsField = {
  key: string
  label: string
  placeholder: string
  hint: string
  inputMode?: 'text' | 'url'
  options?: Array<{ value: string; label: string }>
}

const paymentProviderSetup: Record<PaymentProvider, PaymentProviderSetup> = {
  YooKassa: {
    title: 'YooKassa',
    userName: 'YooKassa',
    channel: 'web',
    summary: 'Карты, СБП и кошельки через редирект YooKassa. Для production нужны Shop ID и секретный ключ.',
    shopIdLabel: 'Shop ID магазина',
    shopIdPlaceholder: 'Например, 123456',
    secretLabel: 'Секретный ключ',
    secretPlaceholder: 'Ключ API из кабинета YooKassa',
    webhookSecretLabel: 'Секрет webhook',
    webhookSecretPlaceholder: 'Обычно не нужен: статус перепроверяется через API',
    apiBaseUrl: 'https://api.yookassa.ru/v3',
    apiBaseUrlLabel: 'API YooKassa',
    returnUrlLabel: 'URL возврата после оплаты',
    webhookUrlLabel: 'URL webhook в YooKassa',
    extraSettingsPlaceholder: '{}',
    extraSettingsHint: 'Дополнительные настройки не обязательны.',
    extraSettingsFields: [],
    allowedIps: yookassaAllowedIps,
    useWebhookIpAllowList: true
  },
  RoboKassa: {
    title: 'RoboKassa',
    userName: 'RoboKassa',
    channel: 'web',
    summary: 'Редирект на Robokassa. Для production нужны логин магазина (MerchantLogin), пароль #1 и пароль #2 для ResultURL.',
    shopIdLabel: 'Логин магазина (MerchantLogin)',
    shopIdPlaceholder: 'Логин магазина Robokassa',
    secretLabel: 'Пароль #1',
    secretPlaceholder: 'Пароль для формирования ссылки оплаты',
    webhookSecretLabel: 'Пароль #2',
    webhookSecretPlaceholder: 'Пароль для проверки ResultURL',
    apiBaseUrl: 'https://auth.robokassa.ru/Merchant/Index.aspx',
    apiBaseUrlLabel: 'URL формы оплаты',
    returnUrlLabel: 'URL успеха и ошибки на сайте',
    webhookUrlLabel: 'ResultURL в Robokassa',
    extraSettingsPlaceholder: '{"hashAlgorithm":"MD5"}',
    extraSettingsHint: 'Можно задать hashAlgorithm: MD5 или SHA256.',
    extraSettingsFields: [
      { key: 'hashAlgorithm', label: 'Алгоритм подписи', placeholder: 'MD5', hint: 'Используется при формировании и проверке подписи Robokassa.', options: [{ value: 'MD5', label: 'MD5' }, { value: 'SHA256', label: 'SHA256' }] }
    ],
    allowedIps: '',
    useWebhookIpAllowList: false
  },
  YooMoney: {
    title: 'YooMoney',
    userName: 'YooMoney',
    channel: 'web',
    summary: 'Quickpay-форма YooMoney. Для production нужен receiver wallet и notification secret.',
    shopIdLabel: 'Кошелек получателя (Receiver)',
    shopIdPlaceholder: 'Например, 410011234567890',
    secretLabel: 'OAuth/API токен',
    secretPlaceholder: 'Не обязателен для quickpay-ссылки',
    webhookSecretLabel: 'Секрет уведомлений',
    webhookSecretPlaceholder: 'Секрет HTTP-уведомлений YooMoney',
    apiBaseUrl: 'https://yoomoney.ru/quickpay/confirm',
    apiBaseUrlLabel: 'URL quickpay',
    returnUrlLabel: 'URL успешной оплаты',
    webhookUrlLabel: 'URL HTTP-уведомлений',
    extraSettingsPlaceholder: '{}',
    extraSettingsHint: 'Для локального sandbox ключи не нужны.',
    extraSettingsFields: [],
    allowedIps: '',
    useWebhookIpAllowList: false
  },
  TelegramStars: {
    title: 'Telegram Stars',
    userName: 'Telegram Stars',
    channel: 'telegram',
    summary: 'Работает только в Telegram-боте через invoice. На публичном сайте и в кабинете скрывается.',
    shopIdLabel: 'Username Telegram-бота',
    shopIdPlaceholder: '@your_vpn_bot',
    secretLabel: 'Токен Telegram-бота',
    secretPlaceholder: 'Хранится в настройках Telegram-бота',
    webhookSecretLabel: 'Секрет webhook Telegram',
    webhookSecretPlaceholder: 'Опциональный secret token webhook',
    apiBaseUrl: '',
    apiBaseUrlLabel: 'Telegram API',
    returnUrlLabel: 'Не используется для Stars',
    webhookUrlLabel: 'Webhook бота',
    extraSettingsPlaceholder: '{"status":"bot-only"}',
    extraSettingsHint: 'Включайте только после настройки Telegram-бота и BotToken.',
    extraSettingsFields: [
      { key: 'status', label: 'Статус сценария Stars', placeholder: 'bot-only', hint: 'Используется как служебная пометка, что оплата доступна только внутри Telegram-бота.', options: [{ value: 'bot-only', label: 'Только Telegram-бот' }, { value: 'invoice-flow', label: 'Telegram invoice flow (полная оплата)' }] }
    ],
    allowedIps: '',
    useWebhookIpAllowList: false
  },
  CloudPayments: {
    title: 'CloudPayments',
    userName: 'CloudPayments',
    channel: 'web',
    summary: 'Для безопасного серверного сценария нужен URL страницы виджета магазина в дополнительных настройках.',
    shopIdLabel: 'Публичный ID магазина',
    shopIdPlaceholder: 'Public ID из CloudPayments',
    secretLabel: 'Пароль API',
    secretPlaceholder: 'Пароль API CloudPayments',
    webhookSecretLabel: 'Пароль webhook',
    webhookSecretPlaceholder: 'Пароль для проверки уведомлений',
    apiBaseUrl: '',
    apiBaseUrlLabel: 'API URL',
    returnUrlLabel: 'URL возврата после оплаты',
    webhookUrlLabel: 'URL уведомлений CloudPayments',
    extraSettingsPlaceholder: '{"hostedCheckoutUrl":"https://pay.example.com/cloudpayments"}',
    extraSettingsHint: 'Обязательно для production: hostedCheckoutUrl со страницей виджета магазина.',
    extraSettingsFields: [
      { key: 'hostedCheckoutUrl', label: 'URL страницы оплаты (hosted checkout)', placeholder: 'https://pay.example.com/cloudpayments', hint: 'Страница магазина, где открыт виджет CloudPayments.', inputMode: 'url' }
    ],
    allowedIps: '',
    useWebhookIpAllowList: false
  },
  TBankAcquiring: {
    title: 'TBank Acquiring',
    userName: 'TBank',
    channel: 'web',
    summary: 'Эквайринг TBank через Init/Confirm/Cancel API. Нужны TerminalKey и Password.',
    shopIdLabel: 'Ключ терминала (TerminalKey)',
    shopIdPlaceholder: 'TerminalKey магазина',
    secretLabel: 'Пароль терминала',
    secretPlaceholder: 'Пароль терминала TBank',
    webhookSecretLabel: 'Секрет webhook',
    webhookSecretPlaceholder: 'Обычно совпадает с Password или не используется',
    apiBaseUrl: 'https://securepay.tinkoff.ru',
    apiBaseUrlLabel: 'API TBank',
    returnUrlLabel: 'URL успешной оплаты',
    webhookUrlLabel: 'URL уведомлений',
    extraSettingsPlaceholder: '{}',
    extraSettingsHint: 'Для sandbox можно оставить ключи пустыми.',
    extraSettingsFields: [],
    allowedIps: '',
    useWebhookIpAllowList: false
  },
  Prodamus: {
    title: 'Prodamus',
    userName: 'Prodamus',
    channel: 'web',
    summary: 'Payform Prodamus. Нужны адрес payform и секрет подписи.',
    shopIdLabel: 'Магазин или аккаунт',
    shopIdPlaceholder: 'Идентификатор магазина',
    secretLabel: 'Секретный ключ',
    secretPlaceholder: 'Секрет подписи формы',
    webhookSecretLabel: 'Секрет webhook',
    webhookSecretPlaceholder: 'Секрет проверки уведомлений',
    apiBaseUrl: 'https://demo.payform.ru',
    apiBaseUrlLabel: 'URL платежной формы',
    returnUrlLabel: 'URL успешной оплаты',
    webhookUrlLabel: 'URL webhook',
    extraSettingsPlaceholder: '{}',
    extraSettingsHint: 'API возвратов и recheck включаются отдельно под конкретный аккаунт.',
    extraSettingsFields: [],
    allowedIps: '',
    useWebhookIpAllowList: false
  },
  Stripe: {
    title: 'Stripe',
    userName: 'Stripe',
    channel: 'web',
    summary: 'Stripe Checkout Sessions. Для production нужны секретный ключ и секрет endpoint webhook.',
    shopIdLabel: 'Аккаунт или merchant id',
    shopIdPlaceholder: 'acct_... или внутреннее имя',
    secretLabel: 'Секретный ключ',
    secretPlaceholder: 'Секретный ключ из Stripe Dashboard',
    webhookSecretLabel: 'Секрет endpoint webhook',
    webhookSecretPlaceholder: 'whsec_...',
    apiBaseUrl: 'https://api.stripe.com',
    apiBaseUrlLabel: 'Stripe API',
    returnUrlLabel: 'URL успеха и отмены',
    webhookUrlLabel: 'Endpoint webhook Stripe',
    extraSettingsPlaceholder: '{}',
    extraSettingsHint: 'Webhook должен отправлять checkout.session.completed и checkout.session.expired.',
    extraSettingsFields: [],
    allowedIps: '',
    useWebhookIpAllowList: false
  },
  PayPal: {
    title: 'PayPal',
    userName: 'PayPal',
    channel: 'web',
    summary: 'PayPal Orders API. Для production нужны client id, secret и webhook id.',
    shopIdLabel: 'ID клиента',
    shopIdPlaceholder: 'PayPal REST app client id',
    secretLabel: 'Секрет клиента',
    secretPlaceholder: 'PayPal REST app secret',
    webhookSecretLabel: 'ID webhook',
    webhookSecretPlaceholder: 'ID webhook из PayPal Developer',
    apiBaseUrl: 'https://api-m.paypal.com',
    apiBaseUrlLabel: 'PayPal API',
    returnUrlLabel: 'URL возврата',
    webhookUrlLabel: 'URL webhook PayPal',
    extraSettingsPlaceholder: '{}',
    extraSettingsHint: 'Для sandbox используйте https://api-m.sandbox.paypal.com.',
    extraSettingsFields: [],
    allowedIps: '',
    useWebhookIpAllowList: false
  }
}

function providerSetup(provider: PaymentProvider) {
  return paymentProviderSetup[provider]
}

function buildProviderForm(provider: PaymentProvider): UpsertPaymentProviderAccountPayload {
  const setup = providerSetup(provider)
  const slug = provider.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase()
  return {
    provider,
    mode: setup.channel === 'telegram' ? 'Disabled' : 'Sandbox',
    name: `${slug}-sandbox`,
    publicName: setup.userName,
    isEnabled: setup.channel !== 'telegram',
    isDefault: true,
    shopId: '',
    apiBaseUrl: setup.apiBaseUrl,
    returnUrl: '',
    webhookUrl: '',
    secretKey: '',
    webhookSecret: '',
    useWebhookIpAllowList: setup.useWebhookIpAllowList,
    allowedWebhookIpRangesCsv: setup.allowedIps,
    extraSettingsJson: setup.extraSettingsPlaceholder === '{}' ? '{}' : setup.extraSettingsPlaceholder
  }
}

function parseProviderExtraSettings(value?: string | null): Record<string, string> {
  if (!value?.trim()) return {}

  try {
    const parsed = JSON.parse(value)
    if (!parsed || Array.isArray(parsed) || typeof parsed !== 'object') return {}
    return Object.fromEntries(Object.entries(parsed).map(([key, item]) => [key, typeof item === 'string' ? item : String(item ?? '')]))
  } catch {
    return {}
  }
}

function providerExtraSettingValue(form: UpsertPaymentProviderAccountPayload, field: PaymentProviderExtraSettingsField) {
  return parseProviderExtraSettings(form.extraSettingsJson)[field.key] ?? ''
}

function setProviderExtraSettingValue(form: UpsertPaymentProviderAccountPayload, field: PaymentProviderExtraSettingsField, value: string): UpsertPaymentProviderAccountPayload {
  const values = parseProviderExtraSettings(form.extraSettingsJson)
  const nextValue = value.trim()
  if (nextValue) {
    values[field.key] = nextValue
  } else {
    delete values[field.key]
  }

  return { ...form, extraSettingsJson: Object.keys(values).length > 0 ? JSON.stringify(values) : '' }
}

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

function validateAdminLogin(email: string, password: string) {
  const errors: string[] = []
  const normalizedEmail = email.trim()
  if (!normalizedEmail) {
    errors.push('Укажите email администратора.')
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalizedEmail)) {
    errors.push('Email администратора должен быть корректным.')
  }
  if (!password) {
    errors.push('Введите пароль администратора.')
  } else if (password.length < 8) {
    errors.push('Пароль должен содержать минимум 8 символов.')
  }
  return errors
}


const adminSections = [
  ['dashboard', 'Дашборд'],
  ['users', 'Пользователи'],
  ['support', 'Поддержка'],
  ['audit', 'Аудит'],
  ['payments', 'Оплаты'],
  ['tariffs', 'Тарифы'],
  ['referrals', 'Рефералы'],
  ['subscriptions', 'Подписки'],
  ['vpn', 'VPN-доступы'],
  ['nodes', 'Серверы'],
  ['panels', '3x-ui панели'],
  ['provisioning', 'Подготовка VPS'],
  ['bot', 'Telegram-бот'],
  ['releases', 'Что нового'],
  ['faq', 'FAQ'],
  ['content', 'Контент сайта'],
  ['scenarios', 'Сценарии']
] as const

const adminSectionDescriptions: Record<AdminSectionId, string> = {
  dashboard: 'Сводка по продажам, инфраструктуре и очередям, чтобы быстро понять состояние платформы.',
  users: 'Пользователи, их подписки, платежи, VPN-доступы, обращения и реферальные начисления.',
  payments: 'Платежные провайдеры, готовность аккаунтов, платежи, возвраты и webhook-события.',
  tariffs: 'Тарифы, цены, описания, сценарии после оплаты и публикация на витрине.',
  referrals: 'Правила реферальной программы, периоды действия и журнал начислений участникам.',
  subscriptions: 'Активные и истекающие подписки, ручные продления и связь с заказами.',
  vpn: 'Выданные VPN-доступы, QR-коды и операции с пользовательскими ключами.',
  nodes: 'VPS-серверы, SSH-доступы, приоритеты, capacity и подготовка инфраструктуры.',
  panels: '3x-ui панели, inbound-ы, клиенты, синхронизация и проверки подключения.',
  support: 'Обращения пользователей, переписка, внутренние заметки и статусы поддержки.',
  audit: 'Журнал административных действий, платежных переходов, выдачи VPN-доступов и ротации секретов.',
  bot: 'Telegram-бот, webhook, тексты сценариев и проверка подключения.',
  releases: 'Раздел «Что нового»: публикации, история релизов и видимость обновлений.',
  faq: 'FAQ для главной страницы, кабинета и публичной страницы вопросов.',
  content: 'Контент главной страницы: hero, SEO, преимущества, отзывы и CTA.',
  scenarios: 'Сценарии работы платформы и привязка тарифов к разрешенным сценариям.',
  provisioning: 'Подготовка VPS, precheck, deploy, отмена запусков и запрос поддержки.'
}

const adminSectionGroups: Array<{ title: string; ids: AdminSectionId[] }> = [
  { title: 'Операции', ids: ['dashboard', 'users', 'support', 'audit'] },
  { title: 'Продажи', ids: ['payments', 'tariffs', 'referrals', 'subscriptions'] },
  { title: 'VPN', ids: ['vpn', 'nodes', 'panels', 'provisioning'] },
  { title: 'Контент', ids: ['bot', 'releases', 'faq', 'content', 'scenarios'] }
]

function adminSectionTabId(id: AdminSectionId) {
  return `admin-section-tab-${id}`
}

function adminSectionLabel(id: AdminSectionId) {
  return adminSections.find(([sectionId]) => sectionId === id)?.[1] ?? 'Раздел'
}

function releaseSourceLabel(source: string | null | undefined) {
  const normalized = String(source ?? '').toLowerCase()
  if (normalized === 'agent') return 'Агент'
  if (normalized === 'manual') return 'Вручную'
  return source || 'Не указан'
}

function provisioningModeLabel(mode: string | null | undefined) {
  const normalized = String(mode ?? '').toLowerCase()
  if (normalized === 'auto') return 'Автоматически'
  if (normalized === 'manual') return 'Вручную'
  if (normalized === 'hybrid') return 'Гибридно'
  return mode || 'Не указан'
}

function provisioningDeployModeLabel(mode: string | null | undefined) {
  const normalized = String(mode ?? '').toLowerCase()
  if (normalized === 'dry-run') return 'Dry-run precheck'
  if (normalized === 'validation-deploy') return 'Validation deploy'
  if (normalized === 'live-deploy') return 'Live deploy'
  if (normalized === 'live-deploy-blocked') return 'Live deploy заблокирован'
  if (normalized === 'unknown') return 'Режим не определён'
  return mode || 'Режим не определён'
}

function provisioningRiskLabel(risk: string | null | undefined) {
  const normalized = String(risk ?? '').toLowerCase()
  if (normalized === 'safe') return 'безопасно'
  if (normalized === 'low') return 'низкий риск'
  if (normalized === 'high') return 'высокий риск'
  if (normalized === 'blocked') return 'заблокировано'
  return risk || 'риск не указан'
}

function provisioningRiskBadge(risk: string | null | undefined) {
  const normalized = String(risk ?? '').toLowerCase()
  if (normalized === 'safe') return 'Safe'
  if (normalized === 'low') return 'Low risk'
  if (normalized === 'high') return 'High risk'
  if (normalized === 'blocked') return 'Blocked'
  return risk || 'Unknown'
}

function serverProvisioningMode(server: VpnNodeDto) {
  return server.provisioningMode || (readTagValue(server.tagsCsv, 'validation-mode') === 'false' ? 'live-deploy-blocked' : 'validation-deploy')
}

function serverProvisioningCanDeploy(server: VpnNodeDto) {
  return serverProvisioningMode(server) !== 'live-deploy-blocked'
}

const orderStatusOptions = [
  ['all', 'Все статусы'],
  ['Draft', 'Черновики'],
  ['PendingPayment', 'Ожидают оплату'],
  ['PaymentReceived', 'Оплата получена'],
  ['FulfillmentInProgress', 'Выдача'],
  ['Completed', 'Завершены'],
  ['Failed', 'Ошибки'],
  ['Cancelled', 'Отменены'],
  ['Expired', 'Истекли'],
  ['Refunded', 'Возвращены'],
  ['PartiallyProcessed', 'Частично обработаны']
] as const

function readAdminSectionFromHash(): AdminSectionId {
  if (typeof window === 'undefined') return adminSections[0][0]

  const section = window.location.hash.replace('#', '')
  return adminSections.some(([id]) => id === section) ? (section as AdminSectionId) : adminSections[0][0]
}

function adminSectionFromHref(href: string | null | undefined): AdminSectionId | null {
  const section = (href ?? '').replace('#', '')
  return adminSections.some(([id]) => id === section) ? (section as AdminSectionId) : null
}

type GenericUser = AdminUserDto
type ServerFormState = CreateServerPayload
type LoadError = { area: string; message: string }
type ReferralProgramFormState = {
  name: string
  status: 'draft' | 'active' | 'paused' | 'archived'
  startAt: string
  endAt: string
  firstPurchaseOnly: boolean
  minimumOrderAmount: number
  allowedChannels: string[]
  referrerEnabled: boolean
  referrerType: string
  referrerValue: number
  referrerUnit: string
  referrerAutoApprove: boolean
  referredEnabled: boolean
  referredType: string
  referredValue: number
  referredUnit: string
  referredAutoApprove: boolean
}

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

const defaultProviderForm: UpsertPaymentProviderAccountPayload = buildProviderForm('YooKassa')

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
  capacity: 5000,
  isActive: true
}

const inboundToForm = (inbound: VpnInboundDto, patch: Partial<CreateVpnInboundPayload> = {}): CreateVpnInboundPayload => ({
  name: inbound.name,
  protocol: inbound.protocol,
  port: inbound.port,
  listen: inbound.listen ?? '',
  settingsJson: inbound.settingsJson || '{"clients":[]}',
  streamSettingsJson: inbound.streamSettingsJson || '{"network":"tcp","security":"tls"}',
  sniffingJson: inbound.sniffingJson || '{}',
  isDefault: inbound.isDefault,
  capacity: inbound.capacity,
  isActive: inbound.isActive,
  ...patch
})

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

const defaultReferralProgramForm: ReferralProgramFormState = {
  name: '',
  status: 'draft',
  startAt: '',
  endAt: '',
  firstPurchaseOnly: true,
  minimumOrderAmount: 0,
  allowedChannels: ['Web'],
  referrerEnabled: true,
  referrerType: 'bonus-days',
  referrerValue: 7,
  referrerUnit: 'days',
  referrerAutoApprove: true,
  referredEnabled: true,
  referredType: 'bonus-days',
  referredValue: 3,
  referredUnit: 'days',
  referredAutoApprove: true
}

function referralProgramToForm(program: AdminReferralProgramDto): ReferralProgramFormState {
  let rules: Record<string, unknown> = {}
  let rewards: Record<string, unknown> = {}
  try { rules = JSON.parse(program.ruleDefinition) as Record<string, unknown> } catch { /* Server validation prevents this for new data. */ }
  try { rewards = JSON.parse(program.rewardDefinition) as Record<string, unknown> } catch { /* Keep editable defaults for legacy data. */ }
  const referrer = typeof rewards.referrer === 'object' && rewards.referrer ? rewards.referrer as Record<string, unknown> : null
  const referred = typeof rewards.referred === 'object' && rewards.referred ? rewards.referred as Record<string, unknown> : null
  return {
    ...defaultReferralProgramForm,
    name: program.name,
    status: ['draft', 'active', 'paused', 'archived'].includes(program.status) ? program.status as ReferralProgramFormState['status'] : 'draft',
    startAt: program.startAt ? toDateTimeLocalValue(program.startAt) : '',
    endAt: program.endAt ? toDateTimeLocalValue(program.endAt) : '',
    firstPurchaseOnly: rules.firstPurchaseOnly !== false,
    minimumOrderAmount: Number(rules.minimumOrderAmount) || 0,
    allowedChannels: Array.isArray(rules.allowedChannels) ? rules.allowedChannels.map(String) : [],
    referrerEnabled: Boolean(referrer),
    referrerType: String(referrer?.type ?? 'bonus-days'),
    referrerValue: Number(referrer?.value) || 0,
    referrerUnit: String(referrer?.unit ?? 'days'),
    referrerAutoApprove: referrer?.autoApprove === true,
    referredEnabled: Boolean(referred),
    referredType: String(referred?.type ?? 'bonus-days'),
    referredValue: Number(referred?.value) || 0,
    referredUnit: String(referred?.unit ?? 'days'),
    referredAutoApprove: referred?.autoApprove === true
  }
}

function buildReferralProgramPayload(form: ReferralProgramFormState): ReferralProgramUpsertPayload {
  const rewardDefinition: Record<string, unknown> = {}
  if (form.referrerEnabled) rewardDefinition.referrer = { type: form.referrerType.trim(), value: form.referrerValue, unit: form.referrerUnit.trim(), autoApprove: form.referrerAutoApprove }
  if (form.referredEnabled) rewardDefinition.referred = { type: form.referredType.trim(), value: form.referredValue, unit: form.referredUnit.trim(), autoApprove: form.referredAutoApprove }
  return {
    name: form.name.trim(),
    status: form.status,
    startAt: form.startAt ? new Date(form.startAt).toISOString() : null,
    endAt: form.endAt ? new Date(form.endAt).toISOString() : null,
    ruleDefinition: JSON.stringify({ firstPurchaseOnly: form.firstPurchaseOnly, minimumOrderAmount: form.minimumOrderAmount, allowedChannels: form.allowedChannels }),
    rewardDefinition: JSON.stringify(rewardDefinition),
    antiFraudSettings: '{}'
  }
}

function validateReferralProgramForm(form: ReferralProgramFormState) {
  const errors: string[] = []
  if (!form.name.trim()) errors.push('Укажите название программы.')
  if (form.minimumOrderAmount < 0) errors.push('Минимальная сумма заказа не может быть отрицательной.')
  if (!form.referrerEnabled && !form.referredEnabled) errors.push('Выберите хотя бы одного получателя вознаграждения.')
  if (form.referrerEnabled && (form.referrerValue <= 0 || !form.referrerType.trim() || !form.referrerUnit.trim())) errors.push('Заполните вознаграждение пригласившему пользователю.')
  if (form.referredEnabled && (form.referredValue <= 0 || !form.referredType.trim() || !form.referredUnit.trim())) errors.push('Заполните вознаграждение приглашенному пользователю.')
  if (form.startAt && form.endAt && new Date(form.endAt) <= new Date(form.startAt)) errors.push('Дата окончания должна быть позже даты начала.')
  return errors
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
  mode: 'LongPolling',
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
  renewalTextTemplate: '',
  paymentFailedTextTemplate: '',
  subscriptionExpiredTextTemplate: '',
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

function getRefundableAmount(payment: PaymentAttemptDto) {
  return Math.max(0, payment.refundableAmount ?? (payment.amount - (payment.refundedAmount ?? 0)))
}

function canRefundPayment(payment: PaymentAttemptDto) {
  return payment.canRefund === true && getRefundableAmount(payment) > 0
}

function refundBlockerText(payment: PaymentAttemptDto) {
  if (payment.refundBlockers && payment.refundBlockers.length > 0) {
    return payment.refundBlockers.join(' · ')
  }

  if (payment.refundSupported === false) return 'Провайдер не поддерживает возвраты.'
  if (payment.status !== 'Succeeded' && payment.status !== 'PartiallyRefunded') return 'Возврат доступен только после успешной оплаты.'
  if (getRefundableAmount(payment) <= 0) return 'Сумма уже возвращена.'
  return ''
}

function homeContentIssueCount(readiness: SiteContentReadinessDto | null) {
  if (!readiness) return 0
  return readiness.missingKeys.length + readiness.inactiveKeys.length + readiness.emptyKeys.length + readiness.duplicateKeys.length
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

function validateTariffForm(form: UpdateTariffPayload) {
  const errors: string[] = []
  const currency = String(form.currency ?? '').trim().toUpperCase()
  const slug = String(form.slug ?? '').trim()

  if (!String(form.name ?? '').trim()) errors.push('Укажите название тарифа.')
  if (n(form.price) < 0) errors.push('Цена не может быть отрицательной.')
  if (n(form.durationDays) <= 0) errors.push('Срок тарифа должен быть больше 0 дней.')
  if (n(form.maxDevices) <= 0) errors.push('Количество устройств должно быть больше 0.')
  if (!/^[A-Z]{3}$/.test(currency)) errors.push('Валюта должна быть кодом из 3 латинских букв: RUB, USD или XTR.')
  if (slug && !/^[a-z0-9а-яё_-]+(?:-[a-z0-9а-яё_-]+)*$/i.test(slug)) errors.push('Slug может содержать буквы, цифры, дефис и подчёркивание.')

  return errors
}

function parseWorkScenarioTariffIds(value?: string | null) {
  if (!value) return []

  try {
    const parsed = JSON.parse(value)
    return Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === 'string' && item.trim().length > 0) : []
  } catch {
    return []
  }
}

function scenarioTariffIdsToJson(ids: string[]) {
  return JSON.stringify(Array.from(new Set(ids.filter(Boolean))))
}

function validateWorkScenarioForm(form: WorkScenarioUpsertPayload) {
  const errors: string[] = []
  const key = form.key.trim()

  if (!form.name.trim()) errors.push('Укажите название сценария.')
  if (!key) errors.push('Укажите ключ сценария.')
  if (key && !/^[a-z0-9_-]+(?:-[a-z0-9_-]+)*$/i.test(key)) errors.push('Ключ может содержать латинские буквы, цифры, дефис и подчёркивание.')
  if (Number(form.maxDevices) <= 0) errors.push('Количество устройств должно быть больше 0.')
  if (!['auto', 'manual', 'hybrid'].includes(String(form.provisioningMode || '').trim().toLowerCase())) errors.push('Режим выдачи должен быть одним из вариантов: автоматически, вручную или гибридно.')

  return errors
}

function isValidHttpUrl(value: string | null | undefined) {
  const normalized = String(value ?? '').trim()
  if (!normalized) return true
  try {
    const url = new URL(normalized)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}

function validatePaymentProviderForm(
  form: UpsertPaymentProviderAccountPayload,
  setup: PaymentProviderSetup,
  editingAccount?: PaymentProviderAccountDto
) {
  const errors: string[] = []
  const enabled = form.mode !== 'Disabled'
  if (!form.name.trim()) errors.push('Укажите внутреннее имя способа оплаты.')
  if (!form.publicName.trim()) errors.push('Укажите название способа оплаты для пользователя.')
  if (enabled && setup.channel === 'web' && !form.shopId.trim()) errors.push(`Заполните поле "${setup.shopIdLabel}".`)
  if (enabled && setup.channel === 'web' && !form.secretKey?.trim() && !editingAccount?.hasSecretKey) errors.push(`Заполните секрет "${setup.secretLabel}".`)
  if (!isValidHttpUrl(form.apiBaseUrl)) errors.push('API base URL должен быть корректным http/https адресом.')
  if (!isValidHttpUrl(form.returnUrl)) errors.push('Return URL должен быть корректным http/https адресом.')
  if (!isValidHttpUrl(form.webhookUrl)) errors.push('Webhook URL должен быть корректным http/https адресом.')
  return errors
}

function validateServerForm(form: ServerFormState) {
  const errors: string[] = []
  if (!form.name.trim()) errors.push('Укажите название VPN-сервера.')
  if (!form.host.trim()) errors.push('Укажите Host / DNS VPN-сервера.')
  if (Number(form.sshPort) <= 0 || Number(form.sshPort) > 65535) errors.push('SSH-порт должен быть в диапазоне 1-65535.')
  if (Number(form.capacity) <= 0) errors.push('Емкость сервера должна быть больше 0.')
  if (Number(form.priority) < 0) errors.push('Приоритет не может быть отрицательным.')
  return errors
}

function validateVpnPanelForm(form: CreateVpnPanelPayload) {
  const errors: string[] = []
  if (!form.name.trim()) errors.push('Укажите название 3x-ui панели.')
  if (!form.baseUrl.trim()) {
    errors.push('Укажите адрес 3x-ui панели.')
  } else if (!isValidHttpUrl(form.baseUrl)) {
    errors.push('Адрес 3x-ui панели должен быть корректным http/https URL.')
  }
  if (Number(form.capacity) <= 0) errors.push('Емкость 3x-ui панели должна быть больше 0.')
  return errors
}

function validateInboundForm(form: CreateVpnInboundPayload, selectedVpnPanelId: string) {
  const errors: string[] = []
  if (!selectedVpnPanelId) errors.push('Выберите 3x-ui панель перед созданием inbound.')
  if (!form.name.trim()) errors.push('Укажите название inbound-правила.')
  if (Number(form.port) <= 0 || Number(form.port) > 65535) errors.push('Порт inbound должен быть в диапазоне 1-65535.')
  if (!form.protocol.trim()) errors.push('Укажите протокол inbound.')
  return errors
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
  if (account.capabilities?.length) return account.capabilities.filter((item) => item.supported).map((item) => item.label || item.key)
  try {
    const parsed = JSON.parse(account.capabilitiesJson ?? '[]')
    return Array.isArray(parsed) ? parsed.map(String) : []
  } catch {
    return []
  }
}

function unsupportedCapabilities(account: PaymentProviderAccountDto) {
  return account.capabilities?.filter((item) => !item.supported).map((item) => item.label || item.key) ?? []
}

function requiredFieldSummary(account: PaymentProviderAccountDto) {
  return account.requiredFields?.filter((item) => item.required) ?? []
}

function providerCheckResultClass(result: PaymentProviderAccountCheckResultDto) {
  return result.isReady ? 'provider-check-result provider-check-result-ok' : 'provider-check-result provider-check-result-problem'
}

export function App() {
  const [token, setToken] = useState(readSessionStorageItem(TOKEN_STORAGE_KEY) ?? '')
  const [refreshToken, setRefreshToken] = useState(readSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY) ?? '')
  const [adminSession, setAdminSession] = useState<AdminSessionDto | null>(null)
  const adminAccessVerified = Boolean(adminSession)
  const adminDisabledTitle = adminAccessVerified ? undefined : adminAuthRequiredMessage
  const [email, setEmail] = useState(readSessionStorageItem(ADMIN_EMAIL_STORAGE_KEY) ?? '')
  const [password, setPassword] = useState('')
  const [rememberAdminEmail, setRememberAdminEmail] = useState(() => Boolean(readSessionStorageItem(ADMIN_EMAIL_STORAGE_KEY)))
  const [users, setUsers] = useState<GenericUser[]>([])
  const [userSearch, setUserSearch] = useState('')
  const [userStatusFilter, setUserStatusFilter] = useState('')
  const [selectedUserId, setSelectedUserId] = useState('')
  const [userOverview, setUserOverview] = useState<AdminUserOverviewDto | null>(null)
  const [summary, setSummary] = useState<AdminDashboardSummaryDto | null>(null)
  const [auditLogs, setAuditLogs] = useState<AdminAuditLogDto[]>([])
  const [notificationDeliveries, setNotificationDeliveries] = useState<AdminNotificationDeliveryDto[]>([])
  const [auditActionFilter, setAuditActionFilter] = useState('')
  const [auditEntityTypeFilter, setAuditEntityTypeFilter] = useState('')
  const [auditActorTypeFilter, setAuditActorTypeFilter] = useState('')
  const [auditSearch, setAuditSearch] = useState('')
  const [subscriptions, setSubscriptions] = useState<SubscriptionDto[]>([])
  const [accessCredentials, setAccessCredentials] = useState<AccessCredentialDto[]>([])
  const [adminQrSvgs, setAdminQrSvgs] = useState<Record<string, string>>({})
  const [orders, setOrders] = useState<OrderDto[]>([])
  const [orderStatusFilter, setOrderStatusFilter] = useState('all')
  const [orderSearch, setOrderSearch] = useState('')
  const [payments, setPayments] = useState<PaymentAttemptDto[]>([])
  const [refundAmounts, setRefundAmounts] = useState<Record<string, number>>({})
  const [refundReasons, setRefundReasons] = useState<Record<string, string>>({})
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
  const [referralPrograms, setReferralPrograms] = useState<AdminReferralProgramDto[]>([])
  const [referralRewards, setReferralRewards] = useState<AdminRewardLedgerDto[]>([])
  const [referralProgramForm, setReferralProgramForm] = useState<ReferralProgramFormState>(defaultReferralProgramForm)
  const [editingReferralProgramId, setEditingReferralProgramId] = useState('')
  const [editingProviderAccountId, setEditingProviderAccountId] = useState('')
  const [appReleases, setAppReleases] = useState<AppReleaseDto[]>([])
  const [appReleaseOverview, setAppReleaseOverview] = useState<AppReleaseOverviewDto | null>(null)
  const [releaseVisibilityFilter, setReleaseVisibilityFilter] = useState('all')
  const [releaseSourceFilter, setReleaseSourceFilter] = useState('all')
  const [releaseSearch, setReleaseSearch] = useState('')
  const [releaseForm, setReleaseForm] = useState<AppReleaseUpsertPayload>(defaultReleaseForm)
  const [editingReleaseId, setEditingReleaseId] = useState('')
  const [faqEntries, setFaqEntries] = useState<FaqItem[]>([])
  const [faqOverview, setFaqOverview] = useState<FaqOverviewDto | null>(null)
  const [faqCategoryFilter, setFaqCategoryFilter] = useState('all')
  const [faqVisibilityFilter, setFaqVisibilityFilter] = useState('all')
  const [faqSearch, setFaqSearch] = useState('')
  const [faqForm, setFaqForm] = useState<FaqUpsertPayload>(defaultFaqForm)
  const [editingFaqId, setEditingFaqId] = useState('')
  const [siteContentBlocks, setSiteContentBlocks] = useState<SiteContentBlockDto[]>([])
  const [homeContentReadiness, setHomeContentReadiness] = useState<SiteContentReadinessDto | null>(null)
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
  const [vpnClientMigrationTargets, setVpnClientMigrationTargets] = useState<Record<string, string>>({})
  const [vpnHealthChecks, setVpnHealthChecks] = useState<PanelHealthCheckDto[]>([])
  const [vpnSyncRuns, setVpnSyncRuns] = useState<PanelSyncRunDto[]>([])
  const [botSettings, setBotSettings] = useState<AdminTelegramBotSettingsDto>(defaultBotSettings)
  const [botSettingsForm, setBotSettingsForm] = useState<UpdateTelegramBotSettingsPayload>({})
  const [botSettingsCheck, setBotSettingsCheck] = useState<AdminTelegramBotConnectionCheckDto | null>(null)
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
  const [editingInboundId, setEditingInboundId] = useState<string | null>(null)
  const [subscriptionExtendDays, setSubscriptionExtendDays] = useState<Record<string, number>>({})
  const [activeSection, setActiveSection] = useState<AdminSectionId>(() => readAdminSectionFromHash())
  const availableAdminSections = useMemo(
    () => adminSession ? adminSections.filter(([id]) => canAccessAdminSection(adminSession.capabilities, id)) : [],
    [adminSession]
  )
  const availableAdminSectionIds = useMemo(() => new Set(availableAdminSections.map(([id]) => id)), [availableAdminSections])
  const activeSectionLabel = adminSectionLabel(activeSection)
  const activeSectionDescription = adminSectionDescriptions[activeSection]
  const activeSectionIndex = Math.max(0, availableAdminSections.findIndex(([id]) => id === activeSection))
  const previousAdminSection = activeSectionIndex > 0 ? availableAdminSections[activeSectionIndex - 1][0] : null
  const nextAdminSection = activeSectionIndex < availableAdminSections.length - 1 ? availableAdminSections[activeSectionIndex + 1][0] : null
  const canWriteSection = (section: AdminSectionId) => adminSession ? canWriteAdminSection(adminSession.capabilities, section) : false
  const canWriteActiveSection = canWriteSection(activeSection)
  const canReadFinance = adminSession?.capabilities.financeRead === true
  const canReadSupport = adminSession?.capabilities.supportRead === true
  const adminLoginErrors = useMemo(() => validateAdminLogin(email, password), [email, password])
  const showAdminLoginErrors = adminLoginErrors.length > 0 && Boolean(email || password)
  const referralProgramFormErrors = useMemo(() => validateReferralProgramForm(referralProgramForm), [referralProgramForm])

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
  const dashboardFailedPayments = canReadFinance ? payments.filter((item) => item.status === 'Failed' || item.status === 'Cancelled').slice(0, 3) : []
  const dashboardFailedProvisioningRuns = provisioningRuns.filter((run) => ['Failed', 'PrecheckFailed'].includes(run.status)).slice(0, 3)
  const dashboardOpenSupportConversations = canReadSupport ? supportConversations.filter((conversation) => conversation.status !== 'closed').slice(0, 3) : []
  const userOverviewStats = useMemo(() => buildAdminUserOverviewStats(userOverview), [userOverview])
  const filteredOrders = useMemo(() => {
    const searchText = orderSearch.trim().toLowerCase()
    return orders.filter((order) => {
      const statusMatches = orderStatusFilter === 'all' || order.status === orderStatusFilter
      if (!statusMatches) return false
      if (!searchText) return true
      return [
        order.id,
        order.userId,
        order.userDisplayName,
        order.userEmail,
        order.tariffId,
        order.tariffName,
        order.status,
        order.type,
        order.channel,
        order.paymentProvider,
        order.lastPaymentId,
        order.lastPaymentStatus,
        order.linkedSubscriptionId
      ].some((value) => s(value, '').toLowerCase().includes(searchText))
    })
  }, [orders, orderSearch, orderStatusFilter])

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

  const loadAll = async (currentToken: string, currentSession: AdminSessionDto | null = adminSession) => {
    if (!currentToken || !currentSession) return
    setBusy(true)
    setError('')
    const errors: LoadError[] = []
    const capabilities = currentSession.capabilities

    const [
      nextSummary,
      nextAuditLogs,
      nextNotificationDeliveries,
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
      nextReferralPrograms,
      nextReferralRewards,
      nextAppReleases,
      nextAppReleaseOverview,
      nextFaqEntries,
      nextFaqOverview,
      nextSiteContent,
      nextHomeContentReadiness,
      nextWorkScenarios,
      nextServers,
      nextRuns,
      nextVpnPanels,
      nextBotSettings
    ] = await Promise.all([
      safeLoad('dashboard', () => api.getAdminDashboardSummary(currentToken), null, errors),
      safeLoad('аудит', () => api.getAdminAuditLogs(currentToken, { action: auditActionFilter, entityType: auditEntityTypeFilter, actorType: auditActorTypeFilter, search: auditSearch, limit: 200 }), [], errors),
      safeLoad('email-уведомления', () => api.getAdminNotificationDeliveries(currentToken), [], errors),
      safeLoad('users', () => api.getAdminUsers(currentToken, { search: userSearch, status: userStatusFilter }), [], errors),
      safeLoad('subscriptions', () => api.getAdminSubscriptions(currentToken), [], errors),
      safeLoad('accesses', () => api.getAdminAccesses(currentToken), [], errors),
      capabilities.financeRead ? safeLoad('orders', () => api.getAdminOrders(currentToken), [], errors) : [],
      capabilities.financeRead ? safeLoad('payments', () => api.getAdminPayments(currentToken), [], errors) : [],
      capabilities.financeRead ? safeLoad('способы оплаты', () => api.getAdminPaymentProviderAccounts(currentToken), [], errors) : [],
      capabilities.financeRead ? safeLoad('события оплат', () => api.getAdminPaymentWebhookEvents(currentToken), [], errors) : [],
      capabilities.financeRead ? safeLoad('refunds', () => api.getAdminRefunds(currentToken), [], errors) : [],
      capabilities.supportRead ? safeLoad('обращения поддержки', () => api.getAdminSupportConversations(currentToken), [], errors) : [],
      safeLoad('tariffs', () => api.getAdminTariffs(currentToken), [], errors),
      safeLoad('реферальные программы', () => api.getAdminReferralPrograms(currentToken), [], errors),
      safeLoad('реферальные начисления', () => api.getAdminReferralRewards(currentToken), [], errors),
      safeLoad('Что нового', () => api.getAdminAppReleases(currentToken, { visibility: releaseVisibilityFilter, source: releaseSourceFilter, search: releaseSearch }), [], errors),
      safeLoad('сводка релизов', () => api.getAdminAppReleaseOverview(currentToken), null, errors),
      safeLoad('FAQ', () => api.getAdminFaq(currentToken, { category: faqCategoryFilter, visibility: faqVisibilityFilter, search: faqSearch }), [], errors),
      safeLoad('сводка FAQ', () => api.getAdminFaqOverview(currentToken), null, errors),
      safeLoad('контент сайта', () => api.getAdminSiteContent(currentToken, 'home'), [], errors),
      safeLoad('готовность главной', () => api.getAdminHomeContentReadiness(currentToken), null, errors),
      safeLoad('сценарии работы', () => api.getAdminWorkScenarios(currentToken), [], errors),
      safeLoad('servers', () => api.getAdminServers(currentToken), [], errors),
      safeLoad('подготовка серверов', () => api.getAdminProvisioningRuns(currentToken), [], errors),
      safeLoad('VPN-панели', () => api.getAdminVpnPanels(currentToken), [], errors),
      capabilities.botManage ? safeLoad('настройки Telegram-бота', () => api.getAdminTelegramBotSettings(currentToken), defaultBotSettings, errors) : defaultBotSettings
    ])

    setSummary(nextSummary)
    setAuditLogs(nextAuditLogs)
    setNotificationDeliveries(nextNotificationDeliveries)
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
    setReferralPrograms(nextReferralPrograms)
    setReferralRewards(nextReferralRewards)
    setAppReleases(nextAppReleases)
    setAppReleaseOverview(nextAppReleaseOverview)
    setFaqEntries(nextFaqEntries)
    setFaqOverview(nextFaqOverview)
    setSiteContentBlocks(nextSiteContent)
    setHomeContentReadiness(nextHomeContentReadiness)
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
      afterPaymentTextTemplate: nextBotSettings.afterPaymentTextTemplate,
      renewalTextTemplate: nextBotSettings.renewalTextTemplate,
      paymentFailedTextTemplate: nextBotSettings.paymentFailedTextTemplate,
      subscriptionExpiredTextTemplate: nextBotSettings.subscriptionExpiredTextTemplate
    })
    setLoadErrors(errors)
    if (!selectedSupportConversationId && nextSupportConversations.length > 0) setSelectedSupportConversationId(nextSupportConversations[0].id)
    if (!selectedVpnPanelId && nextVpnPanels.length > 0) setSelectedVpnPanelId(nextVpnPanels[0].id)
    if (!selectedUserId && nextUsers.length > 0) setSelectedUserId(String(nextUsers[0].id ?? ''))
    setBusy(false)
  }

  const isAdminAccessDenied = (error: unknown) =>
    error instanceof ApiClientError && (error.status === 401 || error.status === 403)

  const verifyAdminSession = async (accessToken: string, currentRefreshToken: string, revokeOnFailure = false) => {
    try {
      return await api.getAdminSession(accessToken)
    } catch (error) {
      if (revokeOnFailure || isAdminAccessDenied(error)) {
        try {
          await api.logout(accessToken, currentRefreshToken || null)
        } catch {
          // Local admission still fails closed when server-side cleanup cannot be confirmed.
        }
      }
      if (!isAdminAccessDenied(error)) throw error

      const denied = error as ApiClientError
      throw new ApiClientError(adminAccessDeniedMessage, denied.status, denied.payload)
    }
  }

  useEffect(() => {
    if (!token || adminSession) return
    let cancelled = false
    setBusy(true)
    setError('')
    void verifyAdminSession(token, refreshToken)
      .then(async (verifiedSession) => {
        if (cancelled) return
        setAdminSession(verifiedSession)
        await loadAll(token, verifiedSession)
      })
      .catch((error) => {
        if (cancelled) return
        setAdminSession(null)
        if (isAdminAccessDenied(error)) {
          removeSessionStorageItem(TOKEN_STORAGE_KEY)
          removeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY)
          setToken('')
          setRefreshToken('')
        }
        setError(error instanceof Error ? error.message : 'Не удалось проверить административный доступ')
      })
      .finally(() => {
        if (!cancelled) setBusy(false)
      })

    return () => { cancelled = true }
  }, [token, refreshToken, adminSession])

  useEffect(() => {
    if (!adminSession || availableAdminSections.length === 0 || availableAdminSectionIds.has(activeSection)) return
    const fallbackSection = availableAdminSections[0][0]
    setActiveSection(fallbackSection)
    window.history.replaceState(null, '', `#${fallbackSection}`)
  }, [activeSection, adminSession, availableAdminSectionIds, availableAdminSections])

  useEffect(() => {
    const syncActiveSection = () => setActiveSection(readAdminSectionFromHash())
    syncActiveSection()
    window.addEventListener('hashchange', syncActiveSection)

    return () => {
      window.removeEventListener('hashchange', syncActiveSection)
    }
  }, [])

  useEffect(() => {
    setEditingInboundId(null)
    setInboundForm(defaultInboundForm)
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
  const updateProviderExtraSetting = (field: PaymentProviderExtraSettingsField, value: string) => setProviderForm((current) => setProviderExtraSettingValue(current, field, value))
  const selectProviderForForm = (provider: PaymentProvider) => {
    setProviderForm((current) => ({
      ...buildProviderForm(provider),
      returnUrl: current.returnUrl,
      webhookUrl: current.webhookUrl
    }))
  }
  const updateVpnPanelForm = <K extends keyof CreateVpnPanelPayload>(key: K, value: CreateVpnPanelPayload[K]) => setVpnPanelForm((current) => ({ ...current, [key]: value }))
  const updateInboundForm = <K extends keyof CreateVpnInboundPayload>(key: K, value: CreateVpnInboundPayload[K]) => setInboundForm((current) => ({ ...current, [key]: value }))
  const updateTariffForm = <K extends keyof UpdateTariffPayload>(key: K, value: UpdateTariffPayload[K]) => setTariffForm((current) => ({ ...current, [key]: value }))
  const updateReleaseForm = <K extends keyof AppReleaseUpsertPayload>(key: K, value: AppReleaseUpsertPayload[K]) => setReleaseForm((current) => ({ ...current, [key]: value }))
  const updateFaqForm = <K extends keyof FaqUpsertPayload>(key: K, value: FaqUpsertPayload[K]) => setFaqForm((current) => ({ ...current, [key]: value }))
  const updateSiteContentForm = <K extends keyof SiteContentBlockUpsertPayload>(key: K, value: SiteContentBlockUpsertPayload[K]) => setSiteContentForm((current) => ({ ...current, [key]: value }))
  const updateWorkScenarioForm = <K extends keyof WorkScenarioUpsertPayload>(key: K, value: WorkScenarioUpsertPayload[K]) => setWorkScenarioForm((current) => ({ ...current, [key]: value }))
  const updateWorkScenarioTariffLink = (tariffId: string, checked: boolean) => setWorkScenarioForm((current) => {
    const currentIds = parseWorkScenarioTariffIds(current.allowedTariffIdsJson)
    const nextIds = checked ? [...currentIds, tariffId] : currentIds.filter((id) => id !== tariffId)
    return { ...current, allowedTariffIdsJson: scenarioTariffIdsToJson(nextIds) }
  })
  const isWorkScenarioTariffSelected = (tariffId: string) => parseWorkScenarioTariffIds(workScenarioForm.allowedTariffIdsJson).includes(tariffId)
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

  const focusAdminSectionTab = (sectionId: AdminSectionId) => {
    if (typeof document === 'undefined') return
    document.getElementById(adminSectionTabId(sectionId))?.focus()
  }

  const focusAdminContent = () => {
    if (typeof document === 'undefined') return
    document.getElementById('admin-content')?.focus()
  }

  const goToAdminSection = (sectionId: AdminSectionId, focusTarget: 'content' | 'tab' = 'content') => {
    setActiveSection(sectionId)
    if (typeof window !== 'undefined') {
      window.history.replaceState(null, '', `#${sectionId}`)
      window.requestAnimationFrame(() => {
        if (focusTarget === 'tab') focusAdminSectionTab(sectionId)
        else focusAdminContent()
      })
    }
  }

  const handleAdminSectionKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (!['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return
    event.preventDefault()
    const lastIndex = availableAdminSections.length - 1
    if (lastIndex < 0) return
    const nextIndex = event.key === 'Home'
      ? 0
      : event.key === 'End'
        ? lastIndex
        : event.key === 'ArrowDown' || event.key === 'ArrowRight'
          ? (activeSectionIndex + 1) > lastIndex ? 0 : activeSectionIndex + 1
          : (activeSectionIndex - 1) < 0 ? lastIndex : activeSectionIndex - 1
    goToAdminSection(availableAdminSections[nextIndex][0], 'tab')
  }

  const runAction = async (id: string, action: () => Promise<void>, requiresWrite = true) => {
    if (requiresWrite && !canWriteActiveSection) {
      setError(`Роль ${adminSession?.roles.join(', ') || 'текущей сессии'} не разрешает изменять раздел «${activeSectionLabel}».`)
      return
    }
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
    removeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY)
    setToken('')
    setRefreshToken('')
    setAdminSession(null)
    setPassword('')
    setUsers([])
    setSelectedUserId('')
    setUserOverview(null)
    setSummary(null)
    setNotificationDeliveries([])
    setSubscriptions([])
    setAccessCredentials([])
    setAdminQrSvgs({})
    setOrders([])
    setPayments([])
    setRefundAmounts({})
    setRefundReasons({})
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
    setReferralPrograms([])
    setReferralRewards([])
    setReferralProgramForm(defaultReferralProgramForm)
    setEditingReferralProgramId('')
    setAppReleases([])
    setAppReleaseOverview(null)
    setReleaseVisibilityFilter('all')
    setReleaseSourceFilter('all')
    setReleaseSearch('')
    setReleaseForm(defaultReleaseForm)
    setEditingReleaseId('')
    setFaqEntries([])
    setFaqOverview(null)
    setFaqCategoryFilter('all')
    setFaqVisibilityFilter('all')
    setFaqSearch('')
    setFaqForm(defaultFaqForm)
    setEditingFaqId('')
    setSiteContentBlocks([])
    setHomeContentReadiness(null)
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
    setBotSettingsCheck(null)
    setLoadErrors([])
    setActionBusyId('')
  }

  const handleLogin = async () => {
    const validationErrors = validateAdminLogin(email, password)
    if (validationErrors.length > 0) {
      setError(validationErrors.join(' '))
      return
    }

    setBusy(true)
    setError('')
    setNotice('')
    try {
      const normalizedEmail = email.trim()
      const response = await api.login(normalizedEmail, password)
      const verifiedSession = await verifyAdminSession(response.accessToken, response.refreshToken, true)
      writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
      writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
      if (rememberAdminEmail) {
        writeSessionStorageItem(ADMIN_EMAIL_STORAGE_KEY, normalizedEmail)
      } else {
        removeSessionStorageItem(ADMIN_EMAIL_STORAGE_KEY)
      }
      setAdminSession(verifiedSession)
      setToken(response.accessToken)
      setRefreshToken(response.refreshToken)
      setPassword('')
      setNotice('Сессия администратора открыта. Токены сохранены в sessionStorage и не показываются в UI.')
      await loadAll(response.accessToken, verifiedSession)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось получить admin token')
    } finally {
      setBusy(false)
    }
  }

  const handleRefreshSession = async () => {
    if (!refreshToken) {
      setError('Сессия не найдена. Войдите заново.')
      return
    }

    setBusy(true)
    setError('')
    setNotice('')
    let refreshRotated = false
    try {
      const response = await api.refresh(refreshToken)
      refreshRotated = true
      const verifiedSession = await verifyAdminSession(response.accessToken, response.refreshToken, true)
      writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
      writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
      setAdminSession(verifiedSession)
      setToken(response.accessToken)
      setRefreshToken(response.refreshToken)
      await loadAll(response.accessToken, verifiedSession)
      setNotice('Сессия администратора обновлена.')
    } catch (e) {
      if (refreshRotated || isAdminAccessDenied(e)) clearAdminSession()
      setError(e instanceof Error ? e.message : 'Не удалось обновить сессию администратора')
    } finally {
      setBusy(false)
    }
  }

  const handleLogout = async () => {
    setBusy(true)
    setError('')
    setNotice('')
    let logoutFailed = false
    try {
      await api.logout(token || null, refreshToken || null)
    } catch {
      logoutFailed = true
    } finally {
      clearAdminSession()
      if (logoutFailed) {
        setError('Локальная сессия завершена, но отзыв серверной сессии не подтверждён. На чужом устройстве измените пароль из доверенного браузера.')
      } else {
        setNotice('Сессия администратора завершена. Данные панели очищены.')
      }
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
      setVpnClientMigrationTargets((current) => {
        const next: Record<string, string> = {}
        for (const client of nextClients) {
          const currentTarget = current[client.id]
          const currentInbound = nextInbounds.find((inbound) => inbound.id === client.vpnInboundId)
          const fallbackTarget = nextInbounds.find((inbound) => inbound.id !== client.vpnInboundId && inbound.isActive && (!currentInbound || inbound.protocol.toLowerCase() === currentInbound.protocol.toLowerCase()))?.id
            ?? ''
          next[client.id] = currentTarget && nextInbounds.some((inbound) => inbound.id === currentTarget) ? currentTarget : fallbackTarget
        }
        return next
      })
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
    if (!token || !canWriteActiveSection) return
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
      setNotice(`${account.name}: ${result.isReady ? 'настройки готовы' : 'в настройках найдены проблемы'}.`)
    })
  }

  const handleRecheckPayment = async (paymentId: string) => runAction(paymentId, async () => {
    const payment = await api.recheckAdminPayment(token, paymentId)
    setNotice(`Платеж ${shortId(payment.paymentId)} проверен: ${payment.status}`)
    await loadAll(token)
  })

  const handleRecheckOrderPayment = async (order: OrderDto) => runAction(`order-recheck-${order.id}`, async () => {
    const payment = await api.recheckAdminOrderPayment(token, order.id)
    setNotice(`Заказ ${shortId(order.id)}: последний платеж ${shortId(payment.paymentId)} проверен, статус ${payment.status}.`)
    await loadAll(token)
  })

  const openOrderUser = (order: OrderDto) => {
    setSelectedUserId(order.userId)
    setActiveSection('users')
    if (typeof window !== 'undefined') window.location.hash = 'users'
    setNotice(`Открыта карточка пользователя ${order.userEmail || shortId(order.userId)} по заказу ${shortId(order.id)}.`)
  }

  const openOrderPayment = (order: OrderDto) => {
    setActiveSection('payments')
    if (typeof window !== 'undefined') window.location.hash = 'payments'
    if (order.lastPaymentId && typeof window !== 'undefined' && typeof document !== 'undefined') {
      window.setTimeout(() => document.getElementById(`payment-${order.lastPaymentId}`)?.scrollIntoView({ block: 'center', behavior: 'smooth' }), 0)
    }
    setNotice(order.lastPaymentId ? `Открыт связанный платеж ${shortId(order.lastPaymentId)}.` : 'У заказа пока нет платежной попытки.')
  }

  const openOrderSubscription = (order: OrderDto) => {
    setActiveSection('subscriptions')
    if (typeof window !== 'undefined') window.location.hash = 'subscriptions'
    if (order.linkedSubscriptionId && typeof window !== 'undefined' && typeof document !== 'undefined') {
      window.setTimeout(() => document.getElementById(`subscription-${order.linkedSubscriptionId}`)?.scrollIntoView({ block: 'center', behavior: 'smooth' }), 0)
    }
    setNotice(order.linkedSubscriptionId ? `Открыта связанная подписка ${shortId(order.linkedSubscriptionId)}.` : 'У заказа нет связанной подписки.')
  }

  const handleRefundPayment = async (payment: PaymentAttemptDto) => {
    await runAction(payment.id, async () => {
      const amount = refundAmounts[payment.id] ?? getRefundableAmount(payment)
      const reason = refundReasons[payment.id]?.trim() || 'manual_admin_refund'
      const refund = await api.refundAdminPayment(token, payment.id, amount, reason)
      setNotice(`Возврат ${refund.providerRefundId || refund.id}: ${refund.status}`)
      setRefundAmounts((current) => ({ ...current, [payment.id]: getRefundableAmount(payment) }))
      setRefundReasons((current) => ({ ...current, [payment.id]: '' }))
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
    if (!token || !canWriteActiveSection) return
    const validationErrors = validateTariffForm(tariffForm)
    if (validationErrors.length > 0) {
      setError(`Тариф: ${validationErrors.join(' ')}`)
      return
    }
    const payload = {
      ...tariffForm,
      name: String(tariffForm.name ?? '').trim(),
      slug: String(tariffForm.slug ?? '').trim(),
      currency: String(tariffForm.currency ?? 'RUB').trim().toUpperCase(),
      featuresJson: featuresTextToJson(tariffFeaturesText)
    }
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
      const result = await api.deleteAdminTariff(token, tariff.id)
      if (editingTariffId === tariff.id) resetTariffForm()
      setNotice(result.archived ? `Тариф ${tariff.name} архивирован и скрыт с витрины.` : `Тариф ${tariff.name} удалён.`)
      await loadAll(token)
    })
  }

  const resetReferralProgramForm = () => {
    setReferralProgramForm(defaultReferralProgramForm)
    setEditingReferralProgramId('')
  }

  const editReferralProgram = (program: AdminReferralProgramDto) => {
    setReferralProgramForm(referralProgramToForm(program))
    setEditingReferralProgramId(program.id)
    goToAdminSection('referrals')
  }

  const updateReferralProgramForm = <K extends keyof ReferralProgramFormState>(key: K, value: ReferralProgramFormState[K]) => {
    setReferralProgramForm((current) => ({ ...current, [key]: value }))
  }

  const toggleReferralChannel = (channel: string) => {
    setReferralProgramForm((current) => ({
      ...current,
      allowedChannels: current.allowedChannels.includes(channel)
        ? current.allowedChannels.filter((item) => item !== channel)
        : [...current.allowedChannels, channel]
    }))
  }

  const handleSaveReferralProgram = async () => {
    if (!token || !canWriteSection('referrals')) return
    const validationErrors = validateReferralProgramForm(referralProgramForm)
    if (validationErrors.length > 0) {
      setError(`Реферальная программа: ${validationErrors.join(' ')}`)
      return
    }

    setBusy(true)
    setError('')
    try {
      const payload = buildReferralProgramPayload(referralProgramForm)
      if (editingReferralProgramId) {
        await api.updateAdminReferralProgram(token, editingReferralProgramId, payload)
        setNotice('Реферальная программа обновлена.')
      } else {
        await api.createAdminReferralProgram(token, payload)
        setNotice('Реферальная программа создана.')
      }
      resetReferralProgramForm()
      await loadAll(token)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось сохранить реферальную программу')
    } finally {
      setBusy(false)
    }
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

  const handleRestoreHomeContentDefaults = async () => {
    if (!token) return
    await runAction('content-restore-defaults', async () => {
      const result = await api.restoreAdminHomeContentDefaults(token)
      setHomeContentReadiness(result.readiness)
      setNotice(`Главная обновлена: создано ${result.created}, восстановлено ${result.restored}.`)
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
    const validationErrors = validateWorkScenarioForm(workScenarioForm)
    if (validationErrors.length > 0) {
      setError(`Сценарий: ${validationErrors.join(' ')}`)
      return
    }

    const payload: WorkScenarioUpsertPayload = {
      ...workScenarioForm,
      name: workScenarioForm.name.trim(),
      key: workScenarioForm.key.trim().toLowerCase(),
      allowedTariffIdsJson: scenarioTariffIdsToJson(parseWorkScenarioTariffIds(workScenarioForm.allowedTariffIdsJson)),
      provisioningMode: workScenarioForm.provisioningMode.trim().toLowerCase(),
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

  const handleSubscriptionAction = async (subscription: SubscriptionDto, action: AdminSubscriptionAction) => {
    const blocker = getAdminSubscriptionActionBlocker(subscription, action)
    if (blocker) {
      setError(blocker)
      return
    }

    if (action === 'activate') {
      await runAction(`${action}-${subscription.id}`, async () => {
        await api.activateAdminSubscription(token, subscription.id, 'manual_subscription_activate')
        setNotice('Подписка активирована, текущий VPN-доступ включен при наличии.')
        await loadAll(token)
      })
      return
    }

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

    if (action === 'sync') {
      if (!subscription.currentAccessId) {
        setError('У подписки нет текущего VPN-доступа для синхронизации.')
        return
      }

      await runAction(`${action}-${subscription.id}`, async () => {
        await api.syncAdminSubscriptionAccess(token, subscription.id, 'manual_subscription_sync')
        setNotice('Текущий VPN-доступ подписки синхронизирован.')
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
      setNotice(action === 'cancel'
        ? 'Подписка отменена, VPN-доступ отозван и удален с сервера.'
        : `Подписка обновлена: ${shortId(subscription.id)}`)
      await loadAll(token)
    })
  }

  const handleAccessAction = async (access: AccessCredentialDto, enable: boolean) => {
    const blocker = getAdminAccessCommandBlocker(access, enable ? 'enable' : 'disable')
    if (blocker) {
      setError(blocker)
      return
    }

    await runAction(`${enable ? 'enable' : 'disable'}-${access.id}`, async () => {
      if (enable) await api.enableAdminAccess(token, access.id, 'manual_admin_action')
      else await api.disableAdminAccess(token, access.id, 'manual_admin_action')
      setNotice(`VPN-доступ ${enable ? 'включен' : 'отключен'}.`)
      await loadAll(token)
    })
  }

  const handleAccessSync = async (access: AccessCredentialDto) => {
    const blocker = getAdminAccessCommandBlocker(access, 'sync')
    if (blocker) {
      setError(blocker)
      return
    }

    await runAction(`sync-${access.id}`, async () => {
      await api.syncAdminAccess(token, access.id, 'manual_admin_sync')
      setNotice('VPN-доступ синхронизирован.')
      await loadAll(token)
    })
  }

  const handleAccessResetTraffic = async (access: AccessCredentialDto) => {
    const blocker = getAdminAccessCommandBlocker(access, 'reset')
    if (blocker) {
      setError(blocker)
      return
    }

    await runAction(`reset-${access.id}`, async () => {
      try {
        await api.resetAdminAccessTraffic(token, access.id, 'manual_admin_reset_traffic')
        setNotice('Трафик VPN-доступа сброшен.')
      } finally {
        await loadAll(token)
      }
    })
  }


  const handleAdminAccessQr = async (access: AccessCredentialDto) => {
    const blocker = getAdminAccessCommandBlocker(access, 'qr')
    if (blocker) {
      setError(blocker)
      return
    }

    await runAction(`qr-${access.id}`, async () => {
      const svg = await api.getAdminAccessQrSvg(token, access.id)
      setAdminQrSvgs((current) => ({ ...current, [access.id]: svg }))
      setNotice('QR-код загружен. Он содержит ссылку подключения и не добавляет дополнительных секретов.')
    }, false)
  }

  const handleReplySupport = async () => {
    if (!token || !selectedSupportConversationId || !supportReplyText.trim()) return
    const conversation = supportConversations.find((item) => item.id === selectedSupportConversationId)
    if (!conversation) return
    await runAction(`support-reply-${selectedSupportConversationId}`, async () => {
      try {
        await api.replyAdminSupportConversation(token, selectedSupportConversationId, supportReplyText.trim(), conversation.revision)
      } catch (error) {
        if (error instanceof ApiClientError && error.status === 409) await loadAll(token)
        throw error
      }
      setSupportReplyText('')
      setNotice('Ответ сохранен и поставлен в очередь отправки Telegram.')
      await loadSupportMessages(selectedSupportConversationId)
      await loadAll(token)
    })
  }

  const handleSupportStatus = async (status: string, conversationId = selectedSupportConversationId) => {
    if (!token || !conversationId) return
    const conversation = supportConversations.find((item) => item.id === conversationId)
    if (!conversation) return
    await runAction(`support-status-${conversationId}`, async () => {
      try {
        await api.updateAdminSupportConversationStatus(token, conversationId, status, conversation.revision)
      } catch (error) {
        if (error instanceof ApiClientError && error.status === 409) await loadAll(token)
        throw error
      }
      setNotice(`Статус обращения обновлен: ${status}.`)
      await loadSupportMessages(conversationId)
      await loadAll(token)
    })
  }

  const handleSupportNote = async () => {
    if (!token || !selectedSupportConversationId || !supportNoteText.trim()) return
    const conversation = supportConversations.find((item) => item.id === selectedSupportConversationId)
    if (!conversation) return
    await runAction(`support-note-${selectedSupportConversationId}`, async () => {
      try {
        await api.addAdminSupportInternalNote(token, selectedSupportConversationId, supportNoteText.trim(), conversation.revision)
      } catch (error) {
        if (error instanceof ApiClientError && error.status === 409) await loadAll(token)
        throw error
      }
      setSupportNoteText('')
      setNotice('Внутренняя заметка сохранена.')
      await loadSupportMessages(selectedSupportConversationId)
      await loadAll(token)
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
    if (!token || !canWriteActiveSection) return
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

  const handleSetVpnPanelStatus = (panel: VpnPanelDto, status: 'Active' | 'Disabled') => runAction(`panel-status-${panel.id}`, async () => {
    const saved = await api.updateAdminVpnPanel(token, panel.id, { status })
    setNotice(`Панель ${saved.name}: статус ${saved.status}.`)
    await loadAll(token)
    await loadVpnPanelDetails(panel.id)
  })

  const handleDeleteVpnPanel = (panel: VpnPanelDto) => runAction(`panel-delete-${panel.id}`, async () => {
    const result = await api.deleteAdminVpnPanel(token, panel.id)
    setNotice(result.archived
      ? `Панель ${panel.name} отключена и сохранена в истории: связей ${result.linkedInbounds + result.linkedClients + result.linkedSyncRuns + result.linkedHealthChecks}.`
      : `Панель ${panel.name} удалена.`)
    if (selectedVpnPanelId === panel.id && result.deleted) setSelectedVpnPanelId('')
    if (editingVpnPanelId === panel.id) cancelVpnPanelEdit()
    await loadAll(token)
  })

  const handleSaveInbound = () => runAction(editingInboundId ? `update-inbound-${editingInboundId}` : 'create-inbound', async () => {
    if (!selectedVpnPanelId) return
    const saved = editingInboundId
      ? await api.updateAdminVpnInbound(token, editingInboundId, inboundForm)
      : await api.createAdminVpnPanelInbound(token, selectedVpnPanelId, inboundForm)
    setNotice(editingInboundId ? `Inbound-правило ${saved.name} обновлено.` : `Inbound-правило ${saved.name} создано.`)
    setEditingInboundId(null)
    setInboundForm(defaultInboundForm)
    await loadVpnPanelDetails(selectedVpnPanelId)
  })

  const handleSetDefaultInbound = (inboundId: string) => runAction(inboundId, async () => {
    await api.setAdminVpnInboundDefault(token, inboundId)
    setNotice('Основное inbound-правило обновлено.')
    await loadVpnPanelDetails(selectedVpnPanelId)
  })

  const handleToggleInboundActive = (inbound: VpnInboundDto) => runAction(`toggle-inbound-${inbound.id}`, async () => {
    const nextIsActive = !inbound.isActive
    const saved = await api.updateAdminVpnInbound(token, inbound.id, inboundToForm(inbound, {
      isActive: nextIsActive,
      isDefault: nextIsActive ? inbound.isDefault : false
    }))
    setNotice(nextIsActive ? `Inbound-правило ${saved.name} включено.` : `Inbound-правило ${saved.name} выключено.`)
    if (editingInboundId === inbound.id) {
      setEditingInboundId(null)
      setInboundForm(defaultInboundForm)
    }
    await loadVpnPanelDetails(selectedVpnPanelId)
  })

  const editInbound = (inbound: VpnInboundDto) => {
    setEditingInboundId(inbound.id)
    setInboundForm(inboundToForm(inbound))
  }

  const cancelInboundEdit = () => {
    setEditingInboundId(null)
    setInboundForm(defaultInboundForm)
  }

  const handleVpnClientAction = (client: VpnClientDto, action: 'enable' | 'disable' | 'sync' | 'reset') => runAction(`vpn-client-${action}-${client.id}`, async () => {
    try {
      const saved = action === 'enable'
        ? await api.enableAdminVpnClient(token, client.id)
        : action === 'disable'
          ? await api.disableAdminVpnClient(token, client.id)
          : action === 'sync'
            ? await api.syncAdminVpnClient(token, client.id)
            : await api.resetAdminVpnClientTraffic(token, client.id)
      setNotice(`VPN-клиент ${saved.email} обновлен: ${saved.syncStatus}.`)
    } finally {
      await loadVpnPanelDetails(selectedVpnPanelId)
    }
  })

  const handleMigrateVpnClient = (client: VpnClientDto) => runAction(`vpn-client-migrate-${client.id}`, async () => {
    const targetInboundId = vpnClientMigrationTargets[client.id]
    if (!targetInboundId) return
    const saved = await api.migrateAdminVpnClient(token, client.id, targetInboundId)
    setNotice(`VPN-клиент ${saved.email} перенесен на выбранный inbound.`)
    await loadVpnPanelDetails(selectedVpnPanelId)
  })

  const updateVpnClientMigrationTarget = (clientId: string, targetInboundId: string) => setVpnClientMigrationTargets((current) => ({ ...current, [clientId]: targetInboundId }))
  const migrationOptionsForClient = (client: VpnClientDto) => {
    const currentInbound = vpnInbounds.find((inbound) => inbound.id === client.vpnInboundId)
    const selectedPanel = vpnPanels.find((panel) => panel.id === selectedVpnPanelId)
    if (!selectedPanel || selectedPanel.status !== 'Active' || selectedPanel.healthStatus === 'Unhealthy' || selectedPanel.usedCapacity >= selectedPanel.capacity) return []
    return vpnInbounds.filter((inbound) => inbound.id !== client.vpnInboundId && inbound.isActive && inbound.usedCapacity < inbound.capacity && (!currentInbound || inbound.protocol.toLowerCase() === currentInbound.protocol.toLowerCase()))
  }

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
    if (!token || !canWriteActiveSection) return
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

  const handleServerMode = async (server: VpnNodeDto, action: 'maintenance' | 'ready' | 'drain' | 'allocate' | 'disable') => {
    const actionLabel = action === 'maintenance' ? 'перевести в обслуживание' : action === 'ready' ? 'вернуть в работу' : action === 'drain' ? 'закрыть набор пользователей' : action === 'disable' ? 'отключить сервер' : 'открыть набор пользователей'
    await runAction(`${action}-${server.id}`, async () => {
      if (action === 'maintenance') await api.enableAdminServerMaintenance(token, server.id)
      if (action === 'ready') await api.disableAdminServerMaintenance(token, server.id)
      if (action === 'drain') await api.disableAdminServerAllocation(token, server.id)
      if (action === 'allocate') await api.enableAdminServerAllocation(token, server.id)
      if (action === 'disable') await api.disableAdminServer(token, server.id)
      setNotice(`Сервер ${server.name}: ${actionLabel}.`)
      await loadAll(token)
    })
  }

  const handleDeleteServer = (server: VpnNodeDto) => runAction(`delete-server-${server.id}`, async () => {
    const result = await api.deleteAdminServer(token, server.id)
    setNotice(result.archived
      ? `Сервер ${server.name} архивирован: связей ${result.linkedSubscriptions + result.linkedAccesses + result.linkedProvisioningRuns + result.linkedHealthChecks + result.linkedMigrationJobs}.`
      : `Сервер ${server.name} удалён.`)
    if (editingServerId === server.id) cancelServerEdit()
    await loadAll(token)
  })

  const handleCheckServerHealth = (server: VpnNodeDto) => runAction(`health-server-${server.id}`, async () => {
    const check = await api.checkAdminServerHealth(token, server.id)
    setNotice(`Health-check ${server.name}: ${check.status}${check.errorText ? ` · ${check.errorText}` : ''}`)
    await loadAll(token)
  })

  const handleQueuePrecheck = (serverId: string) => runAction(`precheck-${serverId}`, async () => {
    const response = await api.precheckAdminServer(token, serverId)
    setNotice(`Проверка поставлена в очередь. Режим: ${response.modeTitle || provisioningDeployModeLabel(response.mode)}. ID запуска: ${response.runId}`)
    await loadAll(token)
  })

  const handleQueueProvision = async (serverId: string) => {
    await runAction(`provision-${serverId}`, async () => {
      const response = await api.queueAdminProvision(token, serverId, false)
      setNotice(`Подготовка сервера поставлена в очередь. Режим: ${response.modeTitle || provisioningDeployModeLabel(response.mode)}; риск: ${provisioningRiskLabel(response.riskLevel)}. ID запуска: ${response.runId}`)
      await loadAll(token)
    })
  }

  const handleRetryProvisioningRun = (runId: string) => runAction(`retry-${runId}`, async () => {
    const response = await api.retryAdminProvisioningRun(token, runId)
    setNotice(`Повтор поставлен в очередь. Режим: ${response.modeTitle || provisioningDeployModeLabel(response.mode)}. Новый ID запуска: ${response.runId}`)
    await loadAll(token)
  })

  const handleRetryNotificationDelivery = (deliveryId: string) => runAction(`retry-notification-${deliveryId}`, async () => {
    await api.retryAdminNotificationDelivery(token, deliveryId)
    setNotice('Email-уведомление возвращено в очередь доставки.')
    await loadAll(token)
  })

  const handleDeployProvisioningRun = (runId: string) => {
    return runAction(`deploy-run-${runId}`, async () => {
      const response = await api.deployAdminProvisioningRun(token, runId)
      setNotice(`Развертывание поставлено в очередь. Режим: ${response.modeTitle || provisioningDeployModeLabel(response.mode)}; риск: ${provisioningRiskLabel(response.riskLevel)}. ID запуска: ${response.runId}`)
      await loadAll(token)
    })
  }

  const handleCancelProvisioningRun = (runId: string) => {
    return runAction(`cancel-run-${runId}`, async () => {
      try {
        await api.cancelAdminProvisioningRun(token, runId)
      } catch (error) {
        if (isProvisioningStateConflict(error)) await loadAll(token)
        throw error
      }
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
    setBotSettingsCheck(null)
    setNotice('Настройки Telegram-бота сохранены. Токены остаются скрытыми и не возвращаются из API.')
  })

  const handleTestBotSettings = () => runAction('bot-settings-test', async () => {
    const result = await api.testAdminTelegramBotSettings(token)
    setBotSettingsCheck(result)
    setNotice(result.isReady ? 'Telegram-бот готов к работе.' : 'Проверка Telegram-бота нашла настройки, которые нужно заполнить.')
  })

  const providerFormSetup = providerSetup(providerForm.provider)
  const editingProviderAccount = editingProviderAccountId
    ? paymentProviderAccounts.find((account) => account.id === editingProviderAccountId)
    : undefined
  const providerFormErrors = validatePaymentProviderForm(providerForm, providerFormSetup, editingProviderAccount)
  const tariffFormErrors = validateTariffForm(tariffForm)
  const serverFormErrors = validateServerForm(serverForm)
  const vpnPanelFormErrors = validateVpnPanelForm(vpnPanelForm)
  const inboundFormErrors = validateInboundForm(inboundForm, selectedVpnPanelId)
  const workScenarioFormErrors = validateWorkScenarioForm(workScenarioForm)

  if (!token || !adminAccessVerified) {
    return (
      <PageShell title="Админ-панель VPN Platform">
        <SkipLink href="#admin-login" />
        <main id="admin-login" className="admin-login-shell" tabIndex={-1}>
          <section className="admin-login-intro" aria-label="Возможности админ-панели">
            <p className="eyebrow">VPN Platform Admin</p>
            <h2>Единый центр управления продажей VPN</h2>
            <p>Настраивайте тарифы, платежных провайдеров, Telegram-ботов, VPN-серверы, панели 3x-ui и выдачу доступов из одной панели.</p>
            <div className="admin-login-metrics">
              <span><strong>{adminSections.length}</strong> разделов</span>
              <span><strong>9</strong> провайдеров</span>
              <span><strong>24/7</strong> контроль</span>
            </div>
          </section>
          <Card>
            <div className="login-panel-header">
              <div>
                <p className="eyebrow">Управление платформой</p>
                <h2 className="page-heading">Вход администратора</h2>
                <p className="muted no-margin-bottom">Используйте учетную запись с ролью администратора. Данные панели загрузятся только после успешной авторизации.</p>
              </div>
              <ValidationModeBadge label="Доступ только для администраторов" />
            </div>

            <div className="admin-login-session" role="status">
              <span>Сессия</span>
              <strong>{rememberAdminEmail ? 'Email будет сохранен на этом устройстве' : 'Email не сохраняется'}</strong>
            </div>

            <form className="admin-login-form" aria-busy={busy} aria-describedby="admin-login-help" onSubmit={(event) => { event.preventDefault(); void handleLogin() }}>
              <label>
                <span>Email</span>
                <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="admin@example.com" type="email" autoComplete="username" required />
                <small>Нужна учетная запись с административной ролью.</small>
              </label>
              <PasswordField label="Пароль" value={password} onChange={setPassword} placeholder="Пароль администратора" autoComplete="current-password" minLength={8} required help="Пароль не сохраняется и очищается после успешного входа." />
              {showAdminLoginErrors && (
                <ul className="validation-list" role="alert" aria-live="polite">
                  {adminLoginErrors.map((item) => <li key={item}>{item}</li>)}
                </ul>
              )}
              <label className="checkbox-row admin-remember-row">
                <input type="checkbox" checked={rememberAdminEmail} onChange={(e) => setRememberAdminEmail(e.target.checked)} />
                <span>Запомнить email на этом устройстве</span>
              </label>
              <PrimaryButton type="submit" disabled={busy || adminLoginErrors.length > 0} aria-busy={busy}>{busy ? 'Проверяем доступ...' : 'Войти в админку'}</PrimaryButton>
            </form>

            <div id="admin-login-help" className="admin-login-checklist" aria-label="Проверка перед входом">
              <span>Пароль не показывается и не сохраняется.</span>
              <span>Токены хранятся только в sessionStorage браузера.</span>
              <span>Опасные действия в админке требуют подтверждения.</span>
            </div>

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
          <label className="admin-section-select">
            <span>Раздел</span>
            <select value={activeSection} onChange={(event) => goToAdminSection(event.target.value as AdminSectionId)}>
              {adminSectionGroups.map((group) => ({ ...group, ids: group.ids.filter((id) => availableAdminSectionIds.has(id)) })).filter((group) => group.ids.length > 0).map((group) => (
                <optgroup key={group.title} label={group.title}>
                  {group.ids.map((id) => <option key={id} value={id}>{adminSectionLabel(id)}</option>)}
                </optgroup>
              ))}
            </select>
          </label>
          <div className="admin-section-tabs" role="tablist" aria-label="Разделы админ-панели" aria-orientation="vertical" onKeyDown={handleAdminSectionKeyDown}>
            {adminSectionGroups.map((group) => ({ ...group, ids: group.ids.filter((id) => availableAdminSectionIds.has(id)) })).filter((group) => group.ids.length > 0).map((group) => (
              <div key={group.title} className="admin-nav-group" role="presentation">
                <span className="admin-nav-group-title">{group.title}</span>
                {group.ids.map((id) => (
                  <a
                    key={id}
                    id={adminSectionTabId(id)}
                    href={`#${id}`}
                    role="tab"
                    className={activeSection === id ? 'active' : undefined}
                    aria-selected={activeSection === id}
                    aria-current={activeSection === id ? 'page' : undefined}
                    aria-controls={id}
                    tabIndex={activeSection === id ? 0 : -1}
                    title={adminSectionDescriptions[id]}
                    onClick={(event) => {
                      event.preventDefault()
                      goToAdminSection(id)
                    }}
                  >
                    {adminSectionLabel(id)}
                  </a>
                ))}
              </div>
            ))}
          </div>
          <small className="muted">Опасные действия требуют подтверждения. Секреты сохраняются скрыто и не возвращаются из API.</small>
        </nav>
        <div id="admin-content" className="admin-main" tabIndex={-1}>
      <div className="page-intro">
        <div>
          <p className="eyebrow">Администрирование</p>
          <h2 className="page-heading">{activeSectionLabel}</h2>
          <p className="muted no-margin-bottom">{activeSectionDescription}</p>
        </div>
        <div className="admin-session-actions">
          <span className="mini-pill">Раздел {activeSectionIndex + 1} из {availableAdminSections.length}</span>
          {!canWriteActiveSection && <span className="mini-pill">Только просмотр</span>}
          <PrimaryButton type="button" className="button-ghost" disabled={!previousAdminSection} onClick={() => previousAdminSection && goToAdminSection(previousAdminSection)}>Предыдущий</PrimaryButton>
          <PrimaryButton type="button" className="button-ghost" disabled={!nextAdminSection} onClick={() => nextAdminSection && goToAdminSection(nextAdminSection)}>Следующий</PrimaryButton>
          <ValidationModeBadge label="Внешние Telegram, оплаты, 3x-ui и VPS отключены" />
          <PrimaryButton type="button" disabled={busy} aria-busy={busy} className="button-secondary" onClick={() => void loadAll(token)}>Обновить данные</PrimaryButton>
          <PrimaryButton type="button" disabled={!refreshToken || busy} aria-busy={busy} className="button-secondary" onClick={() => void handleRefreshSession()}>Обновить сессию</PrimaryButton>
          <PrimaryButton type="button" disabled={busy} aria-busy={busy} className="button-secondary" onClick={() => void handleLogout()}>Завершить сессию</PrimaryButton>
        </div>
      </div>

      {busy && <LoadingBlock label="Загружаем данные admin-panel..." />}
      {notice && <p className="toast-success" role="status" aria-live="polite">{notice}</p>}
      {error && <ErrorBlock message={error} />}
      {loadErrors.length > 0 && <CodeBlock>{loadErrors.map((item) => `${item.area}: ${item.message}`).join('\n')}</CodeBlock>}

      <div id="dashboard" className="grid section" role="tabpanel" aria-labelledby={adminSectionTabId('dashboard')} hidden={activeSection !== 'dashboard'}>
        <StatTile label="Всего пользователей" value={derivedSummary.totalUsers} />
        <StatTile label="Telegram-пользователи" value={derivedSummary.telegramUsers} />
        <StatTile label="Активные подписки" value={derivedSummary.activeSubscriptions} />
        <StatTile label="Скоро истекают" value={derivedSummary.expiringSubscriptions} />
        {canReadFinance && <StatTile label="Оплачено / ожидает" value={`${derivedSummary.paidOrders} / ${derivedSummary.pendingOrders}`} />}
        {canReadFinance && <StatTile label="Неуспешные платежи" value={derivedSummary.failedPayments} />}
        {canReadFinance && <StatTile label="Свежие платежи / заказы" value={`${derivedSummary.recentPayments} / ${derivedSummary.recentOrders}`} />}
        <StatTile label="VPN-доступы" value={derivedSummary.vpnAccessesCount} />
        <StatTile label="VPN-серверы" value={`${derivedSummary.healthyVpnNodes}/${derivedSummary.vpnNodesCount} OK`} />
        <StatTile label="3x-ui панели" value={`${derivedSummary.healthyVpnPanels}/${derivedSummary.vpnPanelsCount} OK`} />
        {canReadSupport && <StatTile label="Очередь поддержки" value={`${derivedSummary.openSupportConversations}/${derivedSummary.supportConversationsCount}`} />}
        <StatTile label="Ошибки подготовки" value={derivedSummary.provisioningErrors} />
      </div>

      {summary?.productionReadiness && (
        <div className="section" hidden={activeSection !== 'dashboard'}>
          <SectionCard
            title={canReadFinance ? 'Готовность к live-продажам' : 'Готовность инфраструктуры'}
            description={canReadFinance
              ? 'Проверка показывает, можно ли принимать production-платежи и автоматически выдавать реальный VPN-доступ через 3x-ui.'
              : 'Показаны только инфраструктурные проверки, доступные текущей административной роли.'}
            actions={<StatusBadge value={summary.productionReadiness.status} />}
          >
            <div className="list-stack">
              {summary.productionReadiness.checks.map((check) => {
                const actionSection = adminSectionFromHref(check.actionHref)
                const canOpenAction = !actionSection || availableAdminSectionIds.has(actionSection)
                return <div key={check.key} className="list-item">
                  <div>
                    <div className="item-heading">
                      <strong>{check.label}</strong>
                      {check.category && <span className="mini-pill">{check.category}</span>}
                    </div>
                    <div className="muted">{check.message}</div>
                  </div>
                  <div className="row-actions">
                    {check.actionHref && canOpenAction && (
                      <a
                        className="button button-secondary"
                        href={check.actionHref}
                        onClick={() => {
                          const section = adminSectionFromHref(check.actionHref)
                          if (section) setActiveSection(section)
                        }}
                      >
                        {check.actionLabel || 'Открыть'}
                      </a>
                    )}
                    <StatusBadge value={check.status} />
                  </div>
                </div>
              })}
            </div>
          </SectionCard>
        </div>
      )}

      <div className="section card-list-two" hidden={activeSection !== 'dashboard'}>
        {canReadFinance && <SectionCard title="Последние заказы" description="Последние заказы с оплатой и связанной подпиской.">
          {orders.length === 0 ? <EmptyState title="Заказов пока нет" description="После покупок на сайте или в Telegram здесь появятся заказы." /> : (
            <div className="list-stack">
              {orders.slice(0, 5).map((order) => <div key={order.id} className="list-item"><div><strong>{order.tariffName || shortId(order.tariffId)}</strong><div className="muted">{order.userEmail || shortId(order.userId)} · {order.amount} {order.currency} · {formatDate(order.createdAt || order.expiresAt)}</div></div><StatusBadge value={order.status} /></div>)}
            </div>
          )}
        </SectionCard>}
        <SectionCard title="Требует внимания" description="Быстрый список очередей, которые требуют реакции.">
          <div className="list-stack">
            {dashboardFailedPayments.map((payment) => <div key={payment.id} className="list-item"><span>Платеж {shortId(payment.id)} · {payment.provider}</span><StatusBadge value={payment.status} /></div>)}
            {dashboardFailedProvisioningRuns.map((run) => <div key={run.id} className="list-item"><span>{run.targetHost || run.nodeName || shortId(run.id)} · {run.errorSummary || run.currentStep || 'нужна проверка'}</span><StatusBadge value={run.status} /></div>)}
            {dashboardOpenSupportConversations.map((conversation) => <div key={conversation.id} className="list-item"><span>{conversation.subject || 'Support'} · tg:{conversation.telegramUserId ?? '—'}</span><StatusBadge value={conversation.status} /></div>)}
            {dashboardFailedPayments.length === 0 && dashboardFailedProvisioningRuns.length === 0 && dashboardOpenSupportConversations.length === 0 && <EmptyState title="Нет срочных проблем" description="В доступных очередях сейчас нет ошибок, требующих реакции." />}
          </div>
        </SectionCard>
      </div>

      <div id="audit" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('audit')} hidden={activeSection !== 'audit'}>
        <Card>
          <h3>Журнал аудита</h3>
          <form className="toolbar toolbar-form" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void loadAll(token) }}>
            <label><span>Действие</span><input value={auditActionFilter} onChange={(e) => setAuditActionFilter(e.target.value)} placeholder="payment.status.changed" /></label>
            <label><span>Сущность</span><input value={auditEntityTypeFilter} onChange={(e) => setAuditEntityTypeFilter(e.target.value)} placeholder="PaymentAttempt" /></label>
            <label><span>Actor</span><select value={auditActorTypeFilter} onChange={(e) => setAuditActorTypeFilter(e.target.value)}><option value="">Все</option><option value="admin">admin</option><option value="system">system</option><option value="user">user</option></select></label>
            <label><span>Поиск</span><input value={auditSearch} onChange={(e) => setAuditSearch(e.target.value)} placeholder="id, actor или action" /></label>
            <PrimaryButton type="submit" disabled={!token || busy} title={adminDisabledTitle} aria-busy={busy}>Применить</PrimaryButton>
          </form>
          <div className="list-stack mt-12">
            {auditLogs.length === 0 && <EmptyState title="Записей аудита нет" description="Журнал пополняется после административных действий, платежных переходов и операций выдачи VPN-доступа." />}
            {auditLogs.slice(0, 50).map((entry) => (
              <div key={entry.id} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{entry.action}</strong>
                    <div className="muted">{entry.entityType} · {shortId(entry.entityId)} · {formatDate(entry.createdAt)}</div>
                    <div className="muted">actor: {entry.actorType}/{entry.actorId || 'unknown'} · IP: {entry.ip || '—'}</div>
                  </div>
                  <div className="item-status">
                    <StatusBadge value={entry.actorType || 'unknown'} />
                    <StatusBadge value={entry.entityType || 'entity'} />
                  </div>
                </div>
                <div className="card-list-two compact-grid">
                  <div>
                    <strong>До</strong>
                    <CodeBlock>{entry.beforeJson || '{}'}</CodeBlock>
                  </div>
                  <div>
                    <strong>После</strong>
                    <CodeBlock>{entry.afterJson || '{}'}</CodeBlock>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </Card>
        <Card>
          <h3>Очередь email-уведомлений</h3>
          <p className="muted">Адреса получателей маскируются, содержимое писем и одноразовые коды не выводятся.</p>
          <div className="list-stack mt-12">
            {notificationDeliveries.length === 0 && <EmptyState title="Уведомлений пока нет" description="После активации подписки или запроса сброса пароля здесь появится состояние доставки." />}
            {notificationDeliveries.slice(0, 20).map((delivery) => (
              <div key={delivery.id} className="list-item">
                <div>
                  <strong>{delivery.templateKey}</strong>
                  <div className="muted">{delivery.maskedToAddress || 'адрес скрыт'} · попыток: {delivery.attempts} · {formatDate(delivery.createdAt)}</div>
                  {delivery.nextAttemptAt && <div className="muted">следующая попытка: {formatDate(delivery.nextAttemptAt)}</div>}
                  {delivery.errorText && <div className="muted">{delivery.errorText}</div>}
                </div>
                <div className="row-actions">
                  <StatusBadge value={delivery.status} />
                  {delivery.status.toLowerCase() === 'failed' && adminSession?.capabilities.adminWrite && (
                    <PrimaryButton
                      className="button-ghost"
                      onClick={() => void handleRetryNotificationDelivery(delivery.id)}
                      disabled={actionBusyId === `retry-notification-${delivery.id}`}
                      aria-busy={actionBusyId === `retry-notification-${delivery.id}`}
                    >
                      Повторить
                    </PrimaryButton>
                  )}
                </div>
              </div>
            ))}
          </div>
        </Card>
        <Card>
          <h3>Доступные категории аудита</h3>
          <div className="list-stack">
            {canReadFinance && <div className="list-item"><span>Изменения платежных провайдеров и ротация секретов</span><StatusBadge value="finance" /></div>}
            {canReadFinance && <div className="list-item"><span>Переходы статусов платежей из webhook и recheck</span><StatusBadge value="system" /></div>}
            {canReadSupport && <div className="list-item"><span>Ответы, заметки и статусы обращений</span><StatusBadge value="support" /></div>}
            {adminSession?.capabilities.botManage && <div className="list-item"><span>Настройки и ротация секретов Telegram-бота</span><StatusBadge value="bot" /></div>}
            <div className="list-item"><span>VPN provisioning и lifecycle-действия доступа</span><StatusBadge value="vpn" /></div>
          </div>
        </Card>
      </div>

      <div id="users" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('users')} hidden={activeSection !== 'users'}>
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
              <div key={user.id} className={`list-item${selectedUserId === user.id ? ' selected-item' : ''}`}>
                <div>
                  <strong>{s(user.displayName, 'Без имени')}</strong>
                  <div className="muted">{s(user.email)} · {s(user.authSource)} · {s(user.referralCode)}</div>
                  <div className="muted">создан {formatDate(user.createdAt)} · вход {formatDate(user.lastLoginAt)}</div>
                </div>
                <div className="actions">
                  <StatusBadge value={user.isBlocked ? 'Blocked' : user.status} />
                  <StatusBadge value={s(user.rolesCsv, 'User')} />
                  <PrimaryButton className={selectedUserId === user.id ? 'button-secondary' : 'button-ghost'} onClick={() => setSelectedUserId(user.id)}>{selectedUserId === user.id ? 'Открыто' : 'Открыть'}</PrimaryButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
        <Card className="user-overview-card">
          <h3>Карточка пользователя</h3>
          {!userOverview && <p className="muted">Выберите пользователя.</p>}
          {userOverview && <>
            <div className="user-profile-head">
              <div>
                <strong>{s(userOverview.user.displayName, 'Без имени')}</strong>
                <div className="muted">{s(userOverview.user.email)} · {s(userOverview.user.authSource)} · язык {s(userOverview.user.preferredLanguage)}</div>
                <div className="muted">ID {shortId(userOverview.user.id)} · реферал {s(userOverview.user.referralCode)} · создан {formatDate(userOverview.user.createdAt)}</div>
              </div>
              <div className="row-actions">
                <StatusBadge value={userOverview.user.isBlocked ? 'Blocked' : userOverview.user.status} />
                <StatusBadge value={userOverview.user.emailConfirmed ? 'Email confirmed' : 'Email not confirmed'} />
              </div>
            </div>
            <div className="user-overview-stats">
              <div className="user-metric"><span>Заказы</span><strong>{userOverviewStats.ordersCount}</strong></div>
              <div className="user-metric"><span>Оплачено</span><strong>{formatAdminMoney(userOverviewStats.totalPaidAmount, userOverviewStats.currency)}</strong></div>
              <div className="user-metric"><span>Активные подписки</span><strong>{userOverviewStats.activeSubscriptionsCount}/{userOverviewStats.subscriptionsCount}</strong></div>
              <div className="user-metric"><span>VPN-доступы</span><strong>{userOverviewStats.activeAccessesCount}/{userOverviewStats.accessCredentialsCount}</strong></div>
            </div>
            {userOverviewStats.needsAttention && (
              <div className="provider-check-result provider-check-result-problem">
                <div className="provider-check-result-head"><strong>Нужно внимание оператора</strong><StatusBadge value="Attention" /></div>
                <ul className="provider-check-result-list">{userOverviewStats.attentionReasons.map((reason) => <li key={reason}>{reason}</li>)}</ul>
              </div>
            )}

            <div className="user-overview-section">
              <div className="card-head"><h4>Подписки</h4><StatusBadge value={`${userOverviewStats.activeSubscriptionsCount} active`} /></div>
              <div className="list-stack">
                {userOverview.subscriptions.length === 0 && <EmptyState title="Подписок нет" description="После оплаты тарифов подписки появятся здесь." />}
                {userOverview.subscriptions.slice(0, 5).map((subscription) => (
                  <div key={subscription.id} className="list-item">
                    <div>
                      <strong>{subscription.tariffName || shortId(subscription.tariffId)}</strong>
                      <div className="muted">{formatDate(subscription.startAt)} - {formatDate(subscription.endAt)} · {subscription.sourceChannel || '—'} · продлений {subscription.renewalCount ?? 0}</div>
                      <div className="muted">сервер {shortId(subscription.currentServerId)} · доступ {shortId(subscription.currentAccessId)}</div>
                    </div>
                    <StatusBadge value={subscription.status} />
                  </div>
                ))}
              </div>
            </div>

            <div className="user-overview-section">
              <div className="card-head"><h4>Заказы и платежи</h4><StatusBadge value={`${userOverviewStats.paymentsCount} payments`} /></div>
              <div className="list-stack">
                {userOverview.orders.length === 0 && userOverview.payments.length === 0 && <EmptyState title="Покупок нет" description="Заказы и платежи появятся после первого checkout." />}
                {userOverview.orders.slice(0, 4).map((order) => (
                  <div key={order.id} className="list-item">
                    <div>
                      <strong>{order.tariffName || shortId(order.tariffId)} · {order.amount} {order.currency}</strong>
                      <div className="muted">{order.channel || '—'} · {order.paymentProvider || '—'} · создан {formatDate(order.createdAt)} · оплачен {formatDate(order.paidAt)}</div>
                    </div>
                    <StatusBadge value={order.status} />
                  </div>
                ))}
                {userOverview.payments.slice(0, 4).map((payment) => (
                  <div key={payment.id} className="list-item">
                    <div>
                      <strong>{payment.provider} · {payment.amount} {payment.currency}</strong>
                      <div className="muted">payment {payment.providerPaymentId || shortId(payment.id)} · подпись {payment.signatureValidated ? 'проверена' : 'не проверена'} · активация {payment.isActivationProcessed ? 'выполнена' : 'ожидает'}</div>
                    </div>
                    <StatusBadge value={payment.status} />
                  </div>
                ))}
              </div>
            </div>

            <div className="user-overview-section">
              <div className="card-head"><h4>VPN-доступы</h4><StatusBadge value={`${userOverviewStats.activeAccessesCount} active`} /></div>
              <div className="list-stack">
                {userOverview.accessCredentials.length === 0 && <EmptyState title="VPN-доступов нет" description="Доступы создаются после успешной оплаты и сценария выдачи." />}
                {userOverview.accessCredentials.slice(0, 5).map((access) => (
                  <div key={access.id} className="list-item">
                    <div>
                      <strong>{access.providerType} · {access.serverName || shortId(access.serverId)}</strong>
                      <div className="muted">выдан {formatDate(access.issuedAt)} · sync {formatDate(access.lastSyncedAt)} · ревизия {access.revision}</div>
                      {getAdminAccessTerminalReason(access)
                        ? <div className="muted user-overview-link">Ключ скрыт: подписка или доступ завершены.</div>
                        : <div className="muted user-overview-link">{access.accessUri || 'URI не выдан'}</div>}
                    </div>
                    <div className="item-status"><StatusBadge value={access.status} />{access.subscriptionStatus && <StatusBadge value={access.subscriptionStatus} />}</div>
                  </div>
                ))}
              </div>
            </div>

            <div className="user-overview-section">
              <div className="card-head"><h4>Telegram и поддержка</h4><StatusBadge value={`${userOverviewStats.telegramAccountsCount} accounts`} /></div>
              <div className="list-stack">
                {userOverview.telegramAccounts.length === 0 && <EmptyState title="Telegram не привязан" description="После привязки аккаунта оператор увидит chat/user id и последний контакт." />}
                {userOverview.telegramAccounts.map((account) => (
                  <div key={account.id} className="list-item">
                    <div>
                      <strong>{telegramDisplayName(account)}</strong>
                      <div className="muted">tg:{account.telegramUserId} · {account.languageCode || '—'} · привязан {formatDate(account.linkedAt)} · был {formatDate(account.lastSeenAt)}</div>
                    </div>
                    <StatusBadge value={account.isBlocked ? 'Blocked' : 'Linked'} />
                  </div>
                ))}
                {userOverview.supportConversations.length === 0 && <EmptyState title="Обращений нет" description="Открытые обращения из кабинета и Telegram будут показаны здесь." />}
                {userOverview.supportConversations.slice(0, 5).map((conversation) => (
                  <div key={conversation.id} className="list-item">
                    <div>
                      <strong>{conversation.subject || 'Обращение'}</strong>
                      <div className="muted">{conversation.channel} · tg:{conversation.telegramUserId ?? '—'} · обновлено {formatDate(conversation.updatedAt)}</div>
                      {conversation.internalNote && <div className="muted">заметка: {conversation.internalNote}</div>}
                    </div>
                    <StatusBadge value={conversation.status} />
                  </div>
                ))}
              </div>
            </div>
          </>}
        </Card>
      </div>

      <div id="payments" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('payments')} hidden={activeSection !== 'payments'}>
        <Card>
          <h3>{editingProviderAccountId ? 'Редактирование способа оплаты' : 'Способы оплаты'}</h3>
          <p className="muted">Добавьте платежный аккаунт, включите его и проверьте готовность к оплатам. Секреты сохраняются скрыто.</p>
          <form hidden={!canWriteSection('payments')} aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveProviderAccount() }}>
            <fieldset className="form-section">
              <legend>Основные параметры</legend>
              <div className="provider-setup-note">
                <strong>{providerFormSetup.title}</strong>
                <span>{providerFormSetup.summary}</span>
                <StatusBadge value={providerFormSetup.channel === 'web' ? 'Оплата на сайте' : 'Только Telegram'} />
              </div>
              <div className="form-grid">
                <label><span>Платежная система</span><select value={providerForm.provider} onChange={(e) => selectProviderForForm(e.target.value as PaymentProvider)}>{paymentProviderOptions.map((item) => <option key={item} value={item}>{providerSetup(item).title}</option>)}</select></label>
                <label><span>Режим</span><select value={providerForm.mode} onChange={(e) => updateProviderForm('mode', e.target.value as PaymentProviderMode)}><option value="Disabled">Выключено</option><option value="Sandbox">Проверка</option><option value="Production">Рабочий</option></select></label>
                <label><span>Внутреннее имя</span><input value={providerForm.name} onChange={(e) => updateProviderForm('name', e.target.value)} placeholder="yookassa-sandbox" required /></label>
                <label><span>Название для пользователя</span><input value={providerForm.publicName} onChange={(e) => updateProviderForm('publicName', e.target.value)} placeholder="YooKassa" required /></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Подключение и безопасность</legend>
              <div className="form-grid">
                <label><span>{providerFormSetup.shopIdLabel}</span><input value={providerForm.shopId} onChange={(e) => updateProviderForm('shopId', e.target.value)} placeholder={providerFormSetup.shopIdPlaceholder} /></label>
                <label><span>{providerFormSetup.apiBaseUrlLabel}</span><input value={providerForm.apiBaseUrl} onChange={(e) => updateProviderForm('apiBaseUrl', e.target.value)} placeholder={providerFormSetup.apiBaseUrl || 'https://api.provider.example'} type="url" inputMode="url" /></label>
                <SecretField configured={editingProviderAccount?.hasSecretKey} label={providerFormSetup.secretLabel} placeholder={providerFormSetup.secretPlaceholder} value={providerForm.secretKey ?? ''} onChange={(value) => updateProviderForm('secretKey', value)} />
                <SecretField configured={editingProviderAccount?.hasWebhookSecret} label={providerFormSetup.webhookSecretLabel} placeholder={providerFormSetup.webhookSecretPlaceholder} value={providerForm.webhookSecret ?? ''} onChange={(value) => updateProviderForm('webhookSecret', value)} />
                <label><span>{providerFormSetup.returnUrlLabel}</span><input value={providerForm.returnUrl} onChange={(e) => updateProviderForm('returnUrl', e.target.value)} placeholder="https://example.com/checkout" type="url" inputMode="url" /></label>
                <label><span>{providerFormSetup.webhookUrlLabel}</span><input value={providerForm.webhookUrl} onChange={(e) => updateProviderForm('webhookUrl', e.target.value)} placeholder="https://api.example.com/api/webhooks/payments/provider" type="url" inputMode="url" /></label>
                <label><span>Разрешенные IP для webhook</span><input value={providerForm.allowedWebhookIpRangesCsv} onChange={(e) => updateProviderForm('allowedWebhookIpRangesCsv', e.target.value)} placeholder="185.71.76.0/27, 185.71.77.0/27" /></label>
              </div>
              <div className="provider-extra-settings">
                <span className="form-label">Дополнительные параметры {providerFormSetup.title}</span>
                {providerFormSetup.extraSettingsFields.length === 0 && <p className="muted">Для этого провайдера дополнительных параметров нет. Достаточно заполнить поля подключения выше.</p>}
                {providerFormSetup.extraSettingsFields.length > 0 && (
                  <div className="form-grid">
                    {providerFormSetup.extraSettingsFields.map((field) => (
                      <label key={field.key}>
                        <span>{field.label}</span>
                        {field.options ? (
                          <select value={providerExtraSettingValue(providerForm, field)} onChange={(e) => updateProviderExtraSetting(field, e.target.value)}>
                            <option value="">Не задано</option>
                            {field.options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                          </select>
                        ) : (
                          <input value={providerExtraSettingValue(providerForm, field)} onChange={(e) => updateProviderExtraSetting(field, e.target.value)} placeholder={field.placeholder} type={field.inputMode === 'url' ? 'url' : 'text'} inputMode={field.inputMode === 'url' ? 'url' : 'text'} />
                        )}
                        <small>{field.hint}</small>
                      </label>
                    ))}
                  </div>
                )}
                <p className="muted">{providerFormSetup.extraSettingsHint}</p>
                {editingProviderAccountId && <p className="muted">При редактировании пустые поля секретов и дополнительных параметров сохраняют текущие значения. Чтобы заменить их, введите новые значения явно.</p>}
              </div>
              <div className="toolbar">
                <label className="checkbox-row"><input checked={providerForm.isEnabled} onChange={(e) => updateProviderForm('isEnabled', e.target.checked)} type="checkbox" /> Включен</label>
                <label className="checkbox-row"><input checked={providerForm.isDefault} onChange={(e) => updateProviderForm('isDefault', e.target.checked)} type="checkbox" /> По умолчанию</label>
                <label className="checkbox-row"><input checked={providerForm.useWebhookIpAllowList} onChange={(e) => updateProviderForm('useWebhookIpAllowList', e.target.checked)} type="checkbox" /> Ограничить webhook по IP</label>
              </div>
            </fieldset>
            <FormValidationSummary errors={providerFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || !token || providerFormErrors.length > 0} title={adminDisabledTitle} aria-busy={busy}>{editingProviderAccountId ? 'Сохранить изменения' : 'Сохранить способ оплаты'}</PrimaryButton>
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
                    <div className="muted">{providerSetup(account.provider).title} · {account.mode} · {providerSetup(account.provider).channel === 'web' ? 'показывается в web после готовности' : 'только Telegram-бот'} · {account.name}</div>
                    <div className="muted">{providerSetup(account.provider).shopIdLabel}: {account.shopId || '—'} · {providerSetup(account.provider).secretLabel}: {account.hasSecretKey ? 'задан' : 'пусто'} · {providerSetup(account.provider).webhookSecretLabel}: {account.hasWebhookSecret ? 'задан' : 'пусто'}</div>
                    <div className="muted">API: {account.apiBaseUrl || '—'} · возврат: {account.returnUrl || '—'} · URL webhook: {account.webhookUrl || '—'}</div>
                    <div className="muted">Список разрешенных IP: {account.useWebhookIpAllowList ? (account.allowedWebhookIpRangesCsv || 'включен, список пуст') : 'не используется'} · дополнительные параметры: {account.extraSettingsJson && account.extraSettingsJson !== '{}' ? 'задан' : 'пусто'}</div>
                    <div className="muted">Пользователю: {account.isPubliclyAvailable ? 'показывается на сайте и в кабинете' : 'скрыт до готовности или доступен только в Telegram'}</div>
                    <div className="muted">Поддерживается: {capabilities(account).join(', ') || '—'}</div>
                    {unsupportedCapabilities(account).length > 0 && <div className="muted">Не поддерживается сейчас: {unsupportedCapabilities(account).join(', ')}</div>}
                    {requiredFieldSummary(account).length > 0 && <div className="muted">Обязательные поля: {requiredFieldSummary(account).map((field) => `${field.label}: ${field.configured ? 'заполнено' : 'не заполнено'}`).join(' · ')}</div>}
                    {account.readinessBlockers && account.readinessBlockers.length > 0 && <div className="safe-note">Блокеры: {account.readinessBlockers.join(' · ')}</div>}
                    {providerCheckResults[account.id] && (
                      <div className={providerCheckResultClass(providerCheckResults[account.id])} role="status" aria-live="polite">
                        <div className="provider-check-result-head">
                          <strong>{providerCheckResults[account.id].isReady ? 'Настройки готовы' : 'Нужно исправить настройки'}</strong>
                          <StatusBadge value={providerCheckResults[account.id].configurationStatus} />
                        </div>
                        <div className="muted">{providerCheckResults[account.id].message} · проверено {formatDate(providerCheckResults[account.id].checkedAt)}</div>
                        <ul className="provider-check-result-list">
                          {providerCheckResults[account.id].details.map((detail, index) => <li key={`${account.id}-check-${index}`}>{detail}</li>)}
                        </ul>
                      </div>
                    )}
                  </div>
                  <div className="status-stack">
                    <StatusBadge value={account.isEnabled ? 'Enabled' : 'Disabled'} />
                    <StatusBadge value={providerSetup(account.provider).channel === 'web' ? 'Web' : 'Telegram'} />
                    <StatusBadge value={providerConfigured(account) ? 'Checkout ready' : 'Not configured'} />
                  </div>
                </div>
                <div className="muted">{providerIssue(account)}</div>
                <div className="toolbar" hidden={!canWriteSection('payments')}>
                  <PrimaryButton className="button-secondary" onClick={() => editProviderAccount(account)}>Редактировать</PrimaryButton>
                  <PrimaryButton className="button-secondary" disabled={actionBusyId === `provider-check-${account.id}`} onClick={() => void handleCheckProviderAccount(account)}>Проверить настройки</PrimaryButton>
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
          <div className="toolbar">
            <label className="compact-field">
              <span>Статус</span>
              <select value={orderStatusFilter} onChange={(e) => setOrderStatusFilter(e.target.value)}>
                {orderStatusOptions.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
            <label className="compact-field flex-grow">
              <span>Поиск</span>
              <input value={orderSearch} onChange={(e) => setOrderSearch(e.target.value)} placeholder="email, тариф, ID заказа или платежа" />
            </label>
            {(orderStatusFilter !== 'all' || orderSearch) && <PrimaryButton className="button-ghost" onClick={() => { setOrderStatusFilter('all'); setOrderSearch('') }}>Сбросить</PrimaryButton>}
          </div>
          <div className="muted">Показано {filteredOrders.length} из {orders.length}. Фильтры помогают быстро найти зависшие оплаты, ошибки выдачи и возвраты.</div>
          <div className="list-stack">
            {orders.length === 0 && <EmptyState title="Заказов нет" description="Новые покупки и продления появятся здесь." />}
            {orders.length > 0 && filteredOrders.length === 0 && <EmptyState title="Заказы не найдены" description="Измените статус или поисковый запрос." />}
            {filteredOrders.slice(0, 40).map((order) => (
              <div key={order.id} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{order.amount} {order.currency} · {order.tariffName || shortId(order.tariffId)}</strong>
                    <div className="muted">Пользователь: {order.userDisplayName || order.userEmail || shortId(order.userId)} · канал: {order.channel ?? '—'} · тип: {order.type ?? '—'}</div>
                    <div className="muted">Создан: {formatDate(order.createdAt)} · истекает: {formatDate(order.expiresAt)} · оплачен: {formatDate(order.paidAt)}</div>
                    <div className="muted">Провайдер: {order.paymentProvider ?? '—'} · попыток оплаты: {order.paymentAttemptsCount ?? 0} · последний платеж: {shortId(order.lastPaymentId)} {order.lastPaymentStatus ? `(${order.lastPaymentStatus})` : ''}</div>
                    <div className="muted">Заказ: {shortId(order.id)} · подписка: {shortId(order.linkedSubscriptionId)}</div>
                  </div>
                  <div className="item-status">
                    <StatusBadge value={order.status} />
                    {order.lastPaymentStatus && <StatusBadge value={order.lastPaymentStatus} />}
                  </div>
                </div>
                <div className="toolbar">
                  <PrimaryButton className="button-secondary" onClick={() => openOrderUser(order)}>К пользователю</PrimaryButton>
                  <PrimaryButton className="button-secondary" disabled={!order.lastPaymentId} title={order.lastPaymentId ? undefined : 'У заказа нет платежной попытки'} onClick={() => openOrderPayment(order)}>К платежу</PrimaryButton>
                  <PrimaryButton className="button-secondary" disabled={!order.linkedSubscriptionId} title={order.linkedSubscriptionId ? undefined : 'У заказа нет связанной подписки'} onClick={() => openOrderSubscription(order)}>К подписке</PrimaryButton>
                  <PrimaryButton hidden={!canWriteSection('payments')} disabled={!order.lastPaymentId || actionBusyId === `order-recheck-${order.id}`} title={order.lastPaymentId ? undefined : 'Сначала нужна платежная попытка'} onClick={() => void handleRecheckOrderPayment(order)}>Проверить оплату</PrimaryButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
        <Card>
          <h3>Платежи, вебхуки и возвраты</h3>
          <div className="list-stack">
            {payments.length === 0 && <EmptyState title="Платежей нет" description="История попыток оплаты появится после покупок." />}
            {payments.slice(0, 8).map((payment) => {
              const refundableAmount = getRefundableAmount(payment)
              const refundAllowed = canRefundPayment(payment)
              const refundBlocker = refundBlockerText(payment)
              const refundAmount = refundAmounts[payment.id] ?? refundableAmount
              const refundReason = refundReasons[payment.id] ?? 'manual_admin_refund'
              return (
                <div id={`payment-${payment.id}`} key={payment.id} className="list-item-vertical">
                  <div className="item-head">
                    <div>
                      <strong>{payment.provider} · {payment.amount} {payment.currency}</strong>
                      <div className="muted">Заказ: {shortId(payment.orderId)} · транзакция: {payment.providerPaymentId || '—'} · режим {payment.providerMode ?? '—'}</div>
                      <div className="muted">Активация: {payment.isActivationProcessed ? 'обработана' : 'ожидает'} · возвращено {payment.refundedAmount ?? 0} {payment.currency} · доступно к возврату {refundableAmount} {payment.currency}</div>
                      {refundBlocker && <div className="safe-note">Возврат недоступен: {refundBlocker}</div>}
                    </div>
                    <div className="item-status">
                      <StatusBadge value={payment.status} />
                      <StatusBadge value={refundAllowed ? 'Refund ready' : 'Refund blocked'} />
                    </div>
                  </div>
                  <div className="toolbar" hidden={!canWriteSection('payments')}>
                    <PrimaryButton disabled={actionBusyId === payment.id} onClick={() => void handleRecheckPayment(payment.id)}>Проверить статус</PrimaryButton>
                    <label className="inline-number-field">
                      <span>Сумма</span>
                      <input value={refundAmount} onChange={(e) => setRefundAmounts((current) => ({ ...current, [payment.id]: Number(e.target.value) || 0 }))} type="number" min={0} max={refundableAmount} step="0.01" inputMode="decimal" disabled={!refundAllowed} />
                    </label>
                    <label className="compact-field">
                      <span>Причина</span>
                      <input value={refundReason} onChange={(e) => setRefundReasons((current) => ({ ...current, [payment.id]: e.target.value }))} placeholder="manual_admin_refund" disabled={!refundAllowed} />
                    </label>
                    <ConfirmButton disabled={actionBusyId === payment.id || !refundAllowed || refundAmount <= 0 || refundAmount > refundableAmount} className="button-secondary" message={`Вернуть ${refundAmount} ${payment.currency} по платежу ${shortId(payment.id)}? Действие будет записано в аудит.`} onConfirm={() => void handleRefundPayment(payment)}>Вернуть платеж</ConfirmButton>
                  </div>
                </div>
              )
            })}
            {paymentWebhookEvents.slice(0, 4).map((event) => <div key={event.id} className="list-item"><span>{event.provider} · {event.eventType} · подпись {event.signatureValidated ? 'проверена' : 'не проверена'}</span><StatusBadge value={event.status} /></div>)}
            {refunds.slice(0, 4).map((refund) => <div key={refund.id} className="list-item"><span>Возврат {refund.amount} {refund.currency} · {refund.providerRefundId || shortId(refund.id)}</span><StatusBadge value={refund.status} /></div>)}
          </div>
        </Card>
      </div>

      <div id="tariffs" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('tariffs')} hidden={activeSection !== 'tariffs'}>
        <Card>
          <h3>{editingTariffId ? 'Редактирование тарифа' : 'Новый тариф'}</h3>
          <form hidden={!canWriteSection('tariffs')} aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveTariff() }}>
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
                <label><span>Сценарий выдачи</span><select value={tariffForm.provisioningScenario ?? 'auto'} onChange={(e) => updateTariffForm('provisioningScenario', e.target.value)}><option value="auto">По умолчанию (auto)</option>{workScenarios.map((scenario) => <option key={scenario.id} value={scenario.key}>{scenario.name} ({scenario.key})</option>)}</select></label>
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
            <FormValidationSummary errors={tariffFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || busy || tariffFormErrors.length > 0} title={adminDisabledTitle} aria-busy={busy}>{editingTariffId ? 'Сохранить тариф' : 'Создать тариф'}</PrimaryButton>
              {editingTariffId && <PrimaryButton type="button" className="button-ghost" onClick={resetTariffForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Список тарифов</h3>
          <div className="list-stack">
            {tariffs.length === 0 && <EmptyState title="Тарифов нет" description="Создайте первый тариф, чтобы он появился на странице покупки." />}
            {tariffs.map((tariff) => <div key={tariff.id} className="list-item-vertical"><div className="item-head"><div><strong>{tariff.name}</strong><div className="muted">{tariff.description || '—'}</div><div className="muted">{tariff.durationDays} дней · {tariff.maxDevices} устройств · порядок {tariff.sortOrder ?? 0} · сценарий {tariff.provisioningScenario || 'auto'}</div><div className="muted">{parseTariffFeatures(tariff).join(' · ') || 'Преимущества не заполнены'}</div></div><div className="item-status"><strong>{tariff.price} {tariff.currency}</strong>{tariff.badge && <StatusBadge value={tariff.badge} />}<StatusBadge value={tariff.isActive === false ? 'Disabled' : 'Enabled'} /></div></div><div className="toolbar" hidden={!canWriteSection('tariffs')}><PrimaryButton className="button-secondary" onClick={() => editTariff(tariff)}>Редактировать</PrimaryButton>{tariff.isActive === false ? <PrimaryButton className="button-ghost" disabled={actionBusyId === tariff.id} onClick={() => void handleToggleTariff(tariff)}>Включить</PrimaryButton> : <ConfirmButton className="button-secondary" disabled={actionBusyId === tariff.id} message={`Выключить тариф "${tariff.name}"? Он исчезнет с публичной витрины и из Telegram.`} onConfirm={() => void handleToggleTariff(tariff)}>Выключить</ConfirmButton>}<ConfirmButton className="button-danger" disabled={actionBusyId === `delete-${tariff.id}`} message={`Удалить тариф "${tariff.name}"? Если есть заказы или подписки, тариф будет архивирован и скрыт с витрины.`} onConfirm={() => void handleDeleteTariff(tariff)}>Удалить</ConfirmButton></div></div>)}
          </div>
        </Card>
      </div>

      <div id="referrals" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('referrals')} hidden={activeSection !== 'referrals'}>
        <Card>
          <h3>{editingReferralProgramId ? 'Редактирование программы' : 'Новая реферальная программа'}</h3>
          <form hidden={!canWriteSection('referrals')} aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveReferralProgram() }}>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={referralProgramForm.name} onChange={(event) => updateReferralProgramForm('name', event.target.value)} required /></label>
                <label><span>Статус</span><select value={referralProgramForm.status} onChange={(event) => updateReferralProgramForm('status', event.target.value as ReferralProgramFormState['status'])}><option value="draft">Черновик</option><option value="active">Активна</option><option value="paused">Приостановлена</option><option value="archived">Архив</option></select></label>
                <label><span>Начало</span><input type="datetime-local" value={referralProgramForm.startAt} onChange={(event) => updateReferralProgramForm('startAt', event.target.value)} /></label>
                <label><span>Окончание</span><input type="datetime-local" value={referralProgramForm.endAt} onChange={(event) => updateReferralProgramForm('endAt', event.target.value)} /></label>
                <label><span>Минимальная сумма заказа</span><input type="number" min={0} step="1" value={referralProgramForm.minimumOrderAmount} onChange={(event) => updateReferralProgramForm('minimumOrderAmount', Number(event.target.value) || 0)} /></label>
              </div>
              <label className="checkbox-row"><input type="checkbox" checked={referralProgramForm.firstPurchaseOnly} onChange={(event) => updateReferralProgramForm('firstPurchaseOnly', event.target.checked)} /> Только первая покупка подписки</label>
              <div className="checkbox-grid" role="group" aria-label="Каналы продаж">
                {['Web', 'Telegram', 'Discord', 'Vk', 'WhatsApp', 'Email'].map((channel) => <label key={channel} className="checkbox-row"><input type="checkbox" checked={referralProgramForm.allowedChannels.includes(channel)} onChange={() => toggleReferralChannel(channel)} /> {channel}</label>)}
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Пригласивший пользователь</legend>
              <label className="checkbox-row"><input type="checkbox" checked={referralProgramForm.referrerEnabled} onChange={(event) => updateReferralProgramForm('referrerEnabled', event.target.checked)} /> Начислять вознаграждение</label>
              <div className="form-grid" hidden={!referralProgramForm.referrerEnabled}>
                <label><span>Тип</span><select value={referralProgramForm.referrerType} onChange={(event) => updateReferralProgramForm('referrerType', event.target.value)}><option value="bonus-days">Бонусные дни</option><option value="cashback">Кэшбэк</option><option value="discount">Скидка</option></select></label>
                <label><span>Значение</span><input type="number" min={0.01} step="0.01" value={referralProgramForm.referrerValue} onChange={(event) => updateReferralProgramForm('referrerValue', Number(event.target.value) || 0)} /></label>
                <label><span>Единица</span><input value={referralProgramForm.referrerUnit} onChange={(event) => updateReferralProgramForm('referrerUnit', event.target.value)} /></label>
              </div>
              <label className="checkbox-row" hidden={!referralProgramForm.referrerEnabled}><input type="checkbox" checked={referralProgramForm.referrerAutoApprove} onChange={(event) => updateReferralProgramForm('referrerAutoApprove', event.target.checked)} /> Подтверждать автоматически</label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Приглашенный пользователь</legend>
              <label className="checkbox-row"><input type="checkbox" checked={referralProgramForm.referredEnabled} onChange={(event) => updateReferralProgramForm('referredEnabled', event.target.checked)} /> Начислять вознаграждение</label>
              <div className="form-grid" hidden={!referralProgramForm.referredEnabled}>
                <label><span>Тип</span><select value={referralProgramForm.referredType} onChange={(event) => updateReferralProgramForm('referredType', event.target.value)}><option value="bonus-days">Бонусные дни</option><option value="cashback">Кэшбэк</option><option value="discount">Скидка</option></select></label>
                <label><span>Значение</span><input type="number" min={0.01} step="0.01" value={referralProgramForm.referredValue} onChange={(event) => updateReferralProgramForm('referredValue', Number(event.target.value) || 0)} /></label>
                <label><span>Единица</span><input value={referralProgramForm.referredUnit} onChange={(event) => updateReferralProgramForm('referredUnit', event.target.value)} /></label>
              </div>
              <label className="checkbox-row" hidden={!referralProgramForm.referredEnabled}><input type="checkbox" checked={referralProgramForm.referredAutoApprove} onChange={(event) => updateReferralProgramForm('referredAutoApprove', event.target.checked)} /> Подтверждать автоматически</label>
            </fieldset>
            <FormValidationSummary errors={referralProgramFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || referralProgramFormErrors.length > 0}>{editingReferralProgramId ? 'Сохранить программу' : 'Создать программу'}</PrimaryButton>
              {editingReferralProgramId && <PrimaryButton type="button" className="button-ghost" onClick={resetReferralProgramForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Программы</h3>
          <div className="list-stack">
            {referralPrograms.length === 0 && <EmptyState title="Программ нет" description="Создайте программу и активируйте ее для начислений после первой покупки." />}
            {referralPrograms.map((program) => <div key={program.id} className="list-item-vertical"><div className="item-head"><div><strong>{program.name}</strong><div className="muted">Период: {formatDate(program.startAt)} - {formatDate(program.endAt)}</div><div className="muted">Обновлена: {formatDate(program.updatedAt)}</div></div><StatusBadge value={program.status} /></div><div className="toolbar" hidden={!canWriteSection('referrals')}><PrimaryButton className="button-secondary" onClick={() => editReferralProgram(program)}>Редактировать</PrimaryButton></div></div>)}
          </div>
        </Card>
        <Card>
          <h3>Начисления</h3>
          <div className="list-stack">
            {referralRewards.length === 0 && <EmptyState title="Начислений нет" description="Начисления появятся после успешных покупок по реферальным приглашениям." />}
            {referralRewards.slice(0, 50).map((reward) => <div key={reward.id} className="list-item"><span><strong>{reward.type === 'bonus-days' ? 'Бонусные дни' : reward.type === 'cashback' ? 'Кэшбэк' : reward.type === 'discount' ? 'Скидка' : reward.type}</strong> · {reward.value} {reward.currencyOrUnit}<span className="muted"> · получатель {shortId(reward.userId)} · программа {shortId(reward.referralProgramId)} · {formatDate(reward.createdAt)}</span></span><StatusBadge value={reward.status} /></div>)}
          </div>
        </Card>
      </div>

      <div id="subscriptions" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('subscriptions')} hidden={activeSection !== 'subscriptions'}>
        <Card>
          <h3>Подписки</h3>
          <div className="list-stack">
            {subscriptions.length === 0 && <EmptyState title="Подписок нет" description="После успешной оплаты подписка появится здесь." />}
            {subscriptions.slice(0, 12).map((subscription) => {
              const isActionBusy = actionBusyId.endsWith(subscription.id)
              const actionAvailability = getAdminSubscriptionActionAvailability(subscription)
              return (
                <div id={`subscription-${subscription.id}`} key={subscription.id} className="list-item-vertical">
                  <div className="item-head">
                    <div>
                      <strong>{subscription.tariffName || shortId(subscription.tariffId)}</strong>
                      <div className="muted">Пользователь: {shortId(subscription.userId)} · источник: {subscription.sourceChannel ?? '—'} · период {formatDate(subscription.startAt)} - {formatDate(subscription.endAt)}</div>
                      <div className="muted">Доступ: {shortId(subscription.currentAccessId)} · сервер: {shortId(subscription.currentServerId)} · платеж: {shortId(subscription.lastPaymentId)} · продлений: {subscription.renewalCount ?? 0}</div>
                      <div className="muted">Льготный период до: {formatDate(subscription.gracePeriodEndAt)} · причина ограничения: {subscription.blockReason || '—'}</div>
                      {subscription.lifecycleLastError && <div className="error-text">Ошибка отключения: {subscription.lifecycleLastError} · попытка {subscription.lifecycleAttemptCount ?? 0} · повтор {formatDate(subscription.lifecycleNextAttemptAt)}</div>}
                    </div>
                    <div className="item-status">
                      <StatusBadge value={subscription.status} />
                      <StatusBadge value={subscription.currentAccessId ? 'Access linked' : 'No access'} />
                    </div>
                  </div>
                  {actionAvailability.reason && <p className="safe-note" role="status">{actionAvailability.reason}</p>}
                  {actionAvailability.canManage && <div className="toolbar" hidden={!canWriteSection('subscriptions')}>
                    <label className="inline-number-field">
                      <span>Дней</span>
                      <input value={subscriptionExtendDays[subscription.id] ?? 30} onChange={(e) => setSubscriptionExtendDays((current) => ({ ...current, [subscription.id]: Number(e.target.value) || 0 }))} type="number" min={1} step="1" inputMode="numeric" />
                    </label>
                    {actionAvailability.canActivate && (
                      <PrimaryButton disabled={isActionBusy} onClick={() => void handleSubscriptionAction(subscription, 'activate')}>Активировать</PrimaryButton>
                    )}
                    <PrimaryButton disabled={isActionBusy} onClick={() => void handleSubscriptionAction(subscription, 'extend')}>Продлить</PrimaryButton>
                    <PrimaryButton className="button-secondary" disabled={isActionBusy || !actionAvailability.canSync} title={actionAvailability.canSync ? undefined : 'У подписки нет текущего VPN-доступа'} onClick={() => void handleSubscriptionAction(subscription, 'sync')}>Синхронизировать доступ</PrimaryButton>
                    {actionAvailability.canToggleBlock && <ConfirmButton className="button-secondary" disabled={isActionBusy} message={`${subscription.status === 'Blocked' ? 'Разблокировать' : 'Заблокировать'} подписку? Это влияет на доступ пользователя.`} onConfirm={() => void handleSubscriptionAction(subscription, subscription.status === 'Blocked' ? 'unblock' : 'block')}>{subscription.status === 'Blocked' ? 'Разблокировать' : 'Заблокировать'}</ConfirmButton>}
                    {actionAvailability.canCancel && <ConfirmButton className="button-danger" disabled={isActionBusy} message="Отменить подписку без возможности восстановления? VPN-доступ будет отозван и удален с сервера, а занятый слот освободится." onConfirm={() => void handleSubscriptionAction(subscription, 'cancel')}>Отменить</ConfirmButton>}
                  </div>}
                </div>
              )
            })}
          </div>
        </Card>
      </div>

      <div id="vpn" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('vpn')} hidden={activeSection !== 'vpn'}>
        <Card>
          <h3>VPN-доступы</h3>
          <div className="list-stack">
            {accessCredentials.length === 0 && <EmptyState title="VPN-доступы пока не созданы" description="После оплаты здесь появится ссылка подключения, статус и история синхронизаций." />}
            {accessCredentials.slice(0, 12).map((access) => {
              const terminalReason = getAdminAccessTerminalReason(access)
              const isTerminal = Boolean(terminalReason)
              return <div key={access.id} className="list-item-vertical">
                <div className="item-head">
                  <strong>{access.providerType} · {isTerminal ? shortId(access.id) : (access.providerAccessId || shortId(access.id))}</strong>
                  <StatusBadge value={access.status} />
                </div>
                <div className="muted">Пользователь: {shortId(access.userId)} · подписка: {shortId(access.subscriptionId)} · сервер: {access.serverName || shortId(access.serverId)} · до: {formatDate(access.expiryDate)}</div>
                <div className="muted">Последняя синхронизация: {formatDate(access.lastSyncedAt)} · версия: {access.revision ?? 0} · клиент провайдера: {isTerminal ? 'скрыт' : (access.providerAccessId || '—')}</div>
                {isTerminal
                  ? <p className="safe-note" role="status">{terminalReason}</p>
                  : access.accessUri && <CodeBlock>{access.accessUri}</CodeBlock>}
                {access.history && access.history.length > 0 && <div className="muted">История: {access.history.slice(0, 3).map((h) => `${h.eventType} ${formatDate(h.createdAt)}`).join(' · ')}</div>}
                {!isTerminal && adminQrSvgs[access.id] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: adminQrSvgs[access.id] }} />}
                {!isTerminal && <div className="toolbar">
                  <CopyButton value={access.accessUri} label="Скопировать URI" disabled={!access.accessUri} />
                  <PrimaryButton disabled={!access.accessUri || actionBusyId === `qr-${access.id}`} onClick={() => void handleAdminAccessQr(access)}>Показать QR</PrimaryButton>
                  {canWriteSection('vpn') && <>
                    {access.status === 'Disabled'
                      ? <PrimaryButton disabled={actionBusyId.includes(access.id)} className="button-secondary" onClick={() => void handleAccessAction(access, true)}>Включить</PrimaryButton>
                      : <ConfirmButton disabled={actionBusyId.includes(access.id)} className="button-secondary" message="Отключить VPN-доступ? Пользователь потеряет возможность подключаться." onConfirm={() => void handleAccessAction(access, false)}>Отключить</ConfirmButton>}
                    <PrimaryButton disabled={actionBusyId === `sync-${access.id}`} onClick={() => void handleAccessSync(access)}>Синхронизировать</PrimaryButton>
                    <ConfirmButton disabled={actionBusyId === `reset-${access.id}`} message="Необратимо обнулить счётчики трафика у VPN-провайдера? При сетевой неопределённости доступ получит статус SyncRequired для ручной сверки." onConfirm={() => void handleAccessResetTraffic(access)}>Сбросить трафик</ConfirmButton>
                  </>}
                </div>}
              </div>
            })}
          </div>
        </Card>
      </div>

      <div id="nodes" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('nodes')} hidden={activeSection !== 'nodes'}>
        <Card>
          <h3>VPN-серверы</h3>
          <div className="list-stack">
            {servers.length === 0 && <EmptyState title="VPN-серверы не добавлены" description="Добавьте сервер или запустите проверку собственного VPS." />}
            {servers.map((server) => (
              <div key={server.id} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{server.name}</strong>
                    <div className="muted">{server.region}/{server.country} · {server.provider} · {server.host}</div>
                    <div className="muted">Дата-центр: {server.datacenter || '—'} · приоритет {server.priority} · протоколы {server.supportedProtocolsCsv || '—'} · теги {server.tagsCsv || '—'}</div>
                    <div className="muted">Емкость: {server.usedCapacity}/{server.capacity} · новые пользователи: {server.isAvailableForNewUsers ? 'разрешены' : 'закрыты'} · пароль панели: {server.panelPasswordConfigured ? 'задан' : 'пусто'}</div>
                    <div className="muted">Панель: {server.panelBaseUrl || '—'} · SSH {server.sshUser ?? 'root'}:{server.sshPort ?? 22} · авторизация: {server.sshAuthMethod || '—'} · доступы: {server.sshCredentialConfigured ? 'заданы' : 'не заданы'}</div>
                    <div className="muted">Provisioning: {server.provisioningModeTitle || provisioningDeployModeLabel(serverProvisioningMode(server))} · риск {provisioningRiskLabel(server.provisioningRiskLevel)} · live deploy {server.liveDeployAllowed ? 'разрешён' : 'закрыт'} · {server.provisioningNextAction || server.provisioningOperatorWarning || 'сначала выполните precheck'}</div>
                    <div className="muted">Последняя проверка: {formatDate(server.lastHealthCheckAt)} · latency {server.lastHealthLatencyMs ?? 0}ms · {server.lastHealthError || 'ошибок нет'}</div>
                  </div>
                  <div className="item-status"><StatusBadge value={server.status} /><StatusBadge value={server.healthStatus} /><StatusBadge value={provisioningRiskBadge(server.provisioningRiskLevel)} /></div>
                </div>
                {server.provisioningOperatorWarning && <div className="safe-note">{server.provisioningOperatorWarning}</div>}
                <div className="toolbar" hidden={!canWriteSection('nodes')}>
                  <PrimaryButton className="button-secondary" onClick={() => editServer(server)}>Редактировать</PrimaryButton>
                  <PrimaryButton disabled={actionBusyId === `health-server-${server.id}`} onClick={() => void handleCheckServerHealth(server)}>Health-check</PrimaryButton>
                  <PrimaryButton disabled={server.status === 'Archived'} onClick={() => void handleQueuePrecheck(server.id)}>Precheck VPS</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={!serverProvisioningCanDeploy(server)} message={`Запустить подготовку сервера "${server.name}"? Режим: ${server.provisioningModeTitle || provisioningDeployModeLabel(serverProvisioningMode(server))}. ${server.provisioningOperatorWarning || 'Проверьте precheck перед запуском.'}`} onConfirm={() => void handleQueueProvision(server.id)}>Подготовить</ConfirmButton>
                  <ConfirmButton className="button-secondary" disabled={server.status === 'Archived'} message="Перевести сервер в обслуживание? Новые пользователи не должны попадать на него." onConfirm={() => void handleServerMode(server, 'maintenance')}>В обслуживание</ConfirmButton>
                  <PrimaryButton className="button-secondary" disabled={server.status === 'Archived'} onClick={() => void handleServerMode(server, 'ready')}>Вернуть в работу</PrimaryButton>
                  <ConfirmButton className="button-secondary" disabled={server.status === 'Archived'} message={`${server.isAvailableForNewUsers ? 'Закрыть набор на сервер' : 'Открыть набор на сервер'}? Это изменит распределение новых пользователей.`} onConfirm={() => void handleServerMode(server, server.isAvailableForNewUsers ? 'drain' : 'allocate')}>{server.isAvailableForNewUsers ? 'Закрыть набор' : 'Открыть набор'}</ConfirmButton>
                  <ConfirmButton className="button-secondary" disabled={server.status === 'Disabled' || server.status === 'Archived'} message={`Отключить сервер "${server.name}"? Новые подключения и автоматическое распределение будут закрыты.`} onConfirm={() => void handleServerMode(server, 'disable')}>Отключить</ConfirmButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `delete-server-${server.id}`} message={`Удалить сервер "${server.name}"? При наличии подписок, VPN-доступов, запусков подготовки, health-check или миграций он будет архивирован.`} onConfirm={() => void handleDeleteServer(server)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
        <Card>
          <h3>{editingServerId ? 'Редактировать VPN-сервер' : 'Добавить VPN-сервер'}</h3>
          <form hidden={!canWriteSection('nodes')} aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveServer() }}>
            <fieldset className="form-section">
              <legend>Идентификация сервера</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={serverForm.name} onChange={(e) => updateServerForm('name', e.target.value)} placeholder="nl-01" required /></label>
                <label><span>Host или DNS</span><input value={serverForm.host} onChange={(e) => updateServerForm('host', e.target.value)} placeholder="vpn.example.com" required /></label>
                <label><span>IP-адрес</span><input value={serverForm.ipAddress} onChange={(e) => updateServerForm('ipAddress', e.target.value)} placeholder="203.0.113.10" /></label>
                <label><span>Провайдер</span><input value={serverForm.provider} onChange={(e) => updateServerForm('provider', e.target.value)} placeholder="hetzner" /></label>
                <label><span>Регион</span><input value={serverForm.region} onChange={(e) => updateServerForm('region', e.target.value)} placeholder="eu" /></label>
                <label><span>Страна</span><input value={serverForm.country} onChange={(e) => updateServerForm('country', e.target.value)} placeholder="NL" /></label>
                <label><span>Дата-центр</span><input value={serverForm.datacenter} onChange={(e) => updateServerForm('datacenter', e.target.value)} placeholder="fsn1" /></label>
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
                <SecretField label="SSH-доступ" value={serverForm.sshCredential ?? ''} onChange={(value) => updateServerForm('sshCredential', value)} />
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
            <FormValidationSummary errors={serverFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || !token || serverFormErrors.length > 0} title={adminDisabledTitle} aria-busy={busy}>{editingServerId ? 'Сохранить сервер' : 'Создать сервер'}</PrimaryButton>
              {editingServerId && <PrimaryButton type="button" className="button-ghost" onClick={cancelServerEdit}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
      </div>

      <div id="panels" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('panels')} hidden={activeSection !== 'panels'}>
        <Card>
          <h3>{editingVpnPanelId ? 'Редактировать 3x-ui панель' : '3x-ui панели'}</h3>
          <p className="safe-note">В проверочном режиме тест и синхронизация идут через безопасный путь без реального подключения к 3x-ui.</p>
          <form hidden={!canWriteSection('panels')} aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleSaveVpnPanel() }}>
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
                <label><span>Проверка SSL</span><select value={vpnPanelForm.sslVerificationMode} onChange={(e) => updateVpnPanelForm('sslVerificationMode', e.target.value)}><option value="Strict">Strict</option><option value="AllowSelfSigned">AllowSelfSigned</option><option value="Disabled">Disabled</option></select></label>
                <label><span>Вариант API</span><select value={vpnPanelForm.apiVariant} onChange={(e) => updateVpnPanelForm('apiVariant', e.target.value)}><option value="X3UiOfficial">X3UiOfficial</option><option value="ThreeXUi">ThreeXUi</option><option value="LegacyXUi">LegacyXUi</option><option value="Custom">Custom</option></select></label>
              </div>
              <label className="checkbox-row"><input checked={vpnPanelForm.autoCreateInbound} onChange={(e) => updateVpnPanelForm('autoCreateInbound', e.target.checked)} type="checkbox" /> Автоматически создавать inbound при выдаче доступа</label>
              <label><span>Шаблон inbound JSON</span><textarea value={vpnPanelForm.defaultInboundTemplateJson} onChange={(e) => updateVpnPanelForm('defaultInboundTemplateJson', e.target.value)} rows={4} placeholder='{"remark":"default-vless","protocol":"vless","port":443}' /></label>
            </fieldset>
            <FormValidationSummary errors={vpnPanelFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={busy || !token || vpnPanelFormErrors.length > 0} title={adminDisabledTitle} aria-busy={busy}>{editingVpnPanelId ? 'Сохранить панель' : 'Добавить панель'}</PrimaryButton>
              {editingVpnPanelId && <PrimaryButton type="button" className="button-ghost" onClick={cancelVpnPanelEdit}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
          <div className="list-stack mt-12">{vpnPanels.map((panel) => <div key={panel.id} className={`list-item-vertical${selectedVpnPanelId === panel.id ? ' selected-item' : ''}`}><div className="item-head"><div><strong>{panel.name}</strong><div className="muted">{panel.baseUrl} · логин {panel.login ? 'задан' : 'пусто'} · {panel.apiVariant} · SSL {panel.sslVerificationMode}</div><div className="muted">Емкость {panel.usedCapacity}/{panel.capacity} · авто inbound: {panel.autoCreateInbound ? 'включен' : 'выключен'} · версия {panel.version || 'неизвестна'} · проверка {formatDate(panel.lastHealthCheckAt)} · синхронизация {formatDate(panel.lastSyncAt)}</div>{panel.lastError && <div className="error-text">Последняя ошибка: {panel.lastError}</div>}</div><div className="item-status"><StatusBadge value={panel.status} /><StatusBadge value={panel.healthStatus} /></div></div><div className="toolbar"><PrimaryButton className={selectedVpnPanelId === panel.id ? 'button-secondary' : 'button-ghost'} onClick={() => setSelectedVpnPanelId(panel.id)}>{selectedVpnPanelId === panel.id ? 'Открыто' : 'Открыть'}</PrimaryButton>{canWriteSection('panels') && <><PrimaryButton className="button-secondary" onClick={() => editVpnPanel(panel)}>Редактировать</PrimaryButton><PrimaryButton className="button-secondary" onClick={() => void handleTestVpnPanel(panel.id)}>Проверить</PrimaryButton><PrimaryButton onClick={() => void handleSyncVpnPanel(panel.id)}>Синхронизировать</PrimaryButton>{panel.status === 'Disabled' ? <PrimaryButton className="button-ghost" disabled={actionBusyId === `panel-status-${panel.id}`} onClick={() => void handleSetVpnPanelStatus(panel, 'Active')}>Включить</PrimaryButton> : <ConfirmButton className="button-secondary" disabled={actionBusyId === `panel-status-${panel.id}`} message={`Отключить 3x-ui панель "${panel.name}"? Новые выдачи не должны выбирать эту панель.`} onConfirm={() => void handleSetVpnPanelStatus(panel, 'Disabled')}>Отключить</ConfirmButton>}<ConfirmButton className="button-danger" disabled={actionBusyId === `panel-delete-${panel.id}`} message={`Удалить 3x-ui панель "${panel.name}"? Если есть inbound-ы, клиенты или история синхронизаций, панель будет отключена и сохранена.`} onConfirm={() => void handleDeleteVpnPanel(panel)}>Удалить</ConfirmButton></>}</div></div>)}</div>
        </Card>
        <Card>
          <h3>Детали панели</h3>
          <label><span>Панель</span><select value={selectedVpnPanelId} onChange={(e) => setSelectedVpnPanelId(e.target.value)}><option value="">Не выбрана</option>{vpnPanels.map((panel) => <option key={panel.id} value={panel.id}>{panel.name}</option>)}</select></label>
          <h4>Inbound-правила</h4>
          <form hidden={!canWriteSection('panels')} aria-busy={actionBusyId === 'create-inbound' || actionBusyId === `update-inbound-${editingInboundId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveInbound() }}>
            <fieldset className="form-section">
              <legend>{editingInboundId ? 'Редактирование inbound-правила' : 'Параметры нового inbound-правила'}</legend>
              <div className="form-grid">
                <label><span>Название inbound-правила</span><input value={inboundForm.name} onChange={(e) => updateInboundForm('name', e.target.value)} placeholder="default-vless" required /></label>
                <label><span>Протокол</span><select value={inboundForm.protocol} onChange={(e) => updateInboundForm('protocol', e.target.value)}><option value="vless">VLESS</option><option value="vmess">VMess</option><option value="trojan">Trojan</option></select></label>
                <label><span>Порт</span><input value={inboundForm.port} onChange={(e) => updateInboundForm('port', Number(e.target.value) || 0)} placeholder="443" type="number" min={1} max={65535} step="1" /></label>
                <label><span>Listen</span><input value={inboundForm.listen} onChange={(e) => updateInboundForm('listen', e.target.value)} placeholder="0.0.0.0 или пусто" /></label>
                <label><span>Емкость</span><input value={inboundForm.capacity} onChange={(e) => updateInboundForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
              </div>
              <div className="form-grid mt-12">
                <label className="checkbox-row"><input checked={inboundForm.isActive} onChange={(e) => setInboundForm((current) => ({ ...current, isActive: e.target.checked, isDefault: e.target.checked ? current.isDefault : false }))} type="checkbox" /> Активен и доступен для выдачи</label>
                <label className="checkbox-row"><input checked={inboundForm.isDefault} disabled={!inboundForm.isActive} onChange={(e) => updateInboundForm('isDefault', e.target.checked)} type="checkbox" /> Основной inbound для панели</label>
              </div>
              <label className="mt-12"><span>settingsJson</span><textarea value={inboundForm.settingsJson} onChange={(e) => updateInboundForm('settingsJson', e.target.value)} rows={4} spellCheck={false} placeholder='{"clients":[]}' /></label>
              <label><span>streamSettingsJson</span><textarea value={inboundForm.streamSettingsJson} onChange={(e) => updateInboundForm('streamSettingsJson', e.target.value)} rows={4} spellCheck={false} placeholder='{"network":"tcp","security":"tls"}' /></label>
              <label><span>sniffingJson</span><textarea value={inboundForm.sniffingJson} onChange={(e) => updateInboundForm('sniffingJson', e.target.value)} rows={3} spellCheck={false} placeholder="{}" /></label>
            </fieldset>
            <FormValidationSummary errors={inboundFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={inboundFormErrors.length > 0 || actionBusyId === 'create-inbound' || actionBusyId === `update-inbound-${editingInboundId}`} aria-busy={actionBusyId === 'create-inbound' || actionBusyId === `update-inbound-${editingInboundId}`}>{editingInboundId ? 'Сохранить inbound-правило' : 'Создать inbound-правило'}</PrimaryButton>
              {editingInboundId && <PrimaryButton type="button" className="button-ghost" onClick={cancelInboundEdit}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
          <div className="list-stack mt-12">{vpnInbounds.map((inbound) => <div key={inbound.id} className="list-item-vertical"><div className="item-head"><div><strong>{inbound.name}</strong><div className="muted">{inbound.protocol}:{inbound.port} · внешний ID {inbound.externalInboundId} · емкость {inbound.usedCapacity}/{inbound.capacity}</div><div className="muted">stream: {inbound.streamSettingsJson}</div></div><div className="item-status"><StatusBadge value={inbound.isActive ? 'Active' : 'Inactive'} />{inbound.isDefault && <StatusBadge value="Default" />}</div></div><div className="toolbar" hidden={!canWriteSection('panels')}><PrimaryButton className="button-secondary" onClick={() => editInbound(inbound)}>Редактировать</PrimaryButton>{!inbound.isDefault && inbound.isActive && <PrimaryButton disabled={actionBusyId === inbound.id} onClick={() => void handleSetDefaultInbound(inbound.id)}>Сделать основным</PrimaryButton>}{inbound.isActive ? <ConfirmButton className="button-secondary" disabled={actionBusyId === `toggle-inbound-${inbound.id}`} message={`Выключить inbound-правило "${inbound.name}"? Новые VPN-доступы не будут использовать его для выдачи.`} onConfirm={() => void handleToggleInboundActive(inbound)}>Выключить</ConfirmButton> : <PrimaryButton className="button-ghost" disabled={actionBusyId === `toggle-inbound-${inbound.id}`} onClick={() => void handleToggleInboundActive(inbound)}>Включить</PrimaryButton>}</div></div>)}</div>
          <h4>Клиенты, здоровье и синхронизация</h4>
          <div className="list-stack">{vpnClients.map((client) => {
            const inbound = vpnInbounds.find((item) => item.id === client.vpnInboundId)
            const migrationOptions = migrationOptionsForClient(client)
            const clientNeedsReconciliation = client.syncStatus.includes('uncertain') || client.syncStatus.includes('compensation-failed')
            return <div key={client.id} className="list-item-vertical"><div className="item-head"><div><strong>{client.email}</strong><div className="muted">UUID {client.uuid} · inbound {inbound?.name ?? shortId(client.vpnInboundId)} · до {formatDate(client.expiryTime)}</div><div className="muted">Синхронизация: {client.syncStatus || 'unknown'} · {formatDate(client.lastSyncedAt)} · лимит устройств {client.limitIp ?? 0}</div></div><div className="item-status"><StatusBadge value={client.enable ? 'Enabled' : 'Disabled'} />{clientNeedsReconciliation && <StatusBadge value="SyncRequired" />}{inbound && <StatusBadge value={inbound.protocol} />}</div></div><div className="toolbar" hidden={!canWriteSection('panels')}>{client.enable ? <ConfirmButton className="button-secondary" disabled={actionBusyId === `vpn-client-disable-${client.id}`} message={`Отключить VPN-клиента "${client.email}"? Пользователь потеряет подключение.`} onConfirm={() => void handleVpnClientAction(client, 'disable')}>Отключить</ConfirmButton> : <PrimaryButton className="button-ghost" disabled={actionBusyId === `vpn-client-enable-${client.id}`} onClick={() => void handleVpnClientAction(client, 'enable')}>Включить</PrimaryButton>}<PrimaryButton disabled={actionBusyId === `vpn-client-sync-${client.id}`} onClick={() => void handleVpnClientAction(client, 'sync')}>Синхронизировать</PrimaryButton><ConfirmButton disabled={actionBusyId === `vpn-client-reset-${client.id}`} message={`Необратимо обнулить счётчики трафика VPN-клиента "${client.email}" в 3x-ui? При сетевой неопределённости клиент будет помечен для ручной сверки.`} onConfirm={() => void handleVpnClientAction(client, 'reset')}>Сбросить трафик</ConfirmButton>{migrationOptions.length > 0 && <><select aria-label={`Целевой inbound для ${client.email}`} value={vpnClientMigrationTargets[client.id] ?? ''} onChange={(e) => updateVpnClientMigrationTarget(client.id, e.target.value)}><option value="">Выберите inbound</option>{migrationOptions.map((option) => <option key={option.id} value={option.id}>{option.name} · {option.protocol}:{option.port} · {option.usedCapacity}/{option.capacity}</option>)}</select><ConfirmButton disabled={!vpnClientMigrationTargets[client.id] || actionBusyId === `vpn-client-migrate-${client.id}`} message={`Перенести VPN-клиента "${client.email}"? Сначала будет занято по одному временному slot панели и target inbound; после успешного удаления source-копии старые slots освободятся. При ошибке перенос будет отменён.`} onConfirm={() => void handleMigrateVpnClient(client)}>Перенести</ConfirmButton></>}</div></div>
          })}{vpnClients.length === 0 && <EmptyState title="Клиентов нет" description="После выдачи VPN-доступов клиенты 3x-ui появятся здесь." />}{vpnHealthChecks.slice(0, 3).map((check) => <div key={check.id} className="list-item"><span>{check.version || 'неизвестно'} · {check.latencyMs ?? 0}ms · {check.errorMessage || 'ok'}</span><StatusBadge value={check.status} /></div>)}{vpnSyncRuns.slice(0, 3).map((run) => <div key={run.id} className="list-item"><span>{run.errorMessage || (run.summaryJson !== '{}' ? run.summaryJson : '') || shortId(run.id)}</span><StatusBadge value={run.status} /></div>)}</div>
        </Card>
      </div>

      <div id="support" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('support')} hidden={activeSection !== 'support'}>
        <Card>
          <h3>Обращения в поддержку</h3>
          <div className="list-stack">{supportConversations.length === 0 && <EmptyState title="Нет обращений" description="Сообщения из Telegram support появятся в этом списке." />}{supportConversations.slice(0, 12).map((conversation) => <div key={conversation.id} className={`list-item-vertical${selectedSupportConversationId === conversation.id ? ' selected-item' : ''}`}><div className="item-head"><div><strong>{conversation.subject || 'Обращение в поддержку'}</strong><div className="muted">{conversation.channel} · tg:{conversation.telegramUserId ?? '—'} · пользователь:{shortId(conversation.userId)}</div><div className="muted">Ответственный: {shortId(conversation.assignedToUserId)} · заметка: {conversation.internalNote || '—'}</div></div><StatusBadge value={conversation.status} /></div><div className="toolbar"><PrimaryButton className={selectedSupportConversationId === conversation.id ? 'button-secondary' : 'button-ghost'} onClick={() => setSelectedSupportConversationId(conversation.id)}>{selectedSupportConversationId === conversation.id ? 'Открыто' : 'Открыть'}</PrimaryButton>{canWriteSection('support') && <><PrimaryButton className="button-secondary" onClick={() => void handleSupportStatus('pending', conversation.id)}>В ожидание</PrimaryButton><PrimaryButton className="button-secondary" onClick={() => void handleSupportStatus(conversation.status === 'closed' ? 'open' : 'closed', conversation.id)}>{conversation.status === 'closed' ? 'Переоткрыть' : 'Закрыть'}</PrimaryButton></>}</div></div>)}</div>
        </Card>
        <Card>
          <h3>Диалог поддержки</h3>
          <label><span>Обращение</span><select value={selectedSupportConversationId} onChange={(e) => setSelectedSupportConversationId(e.target.value)}><option value="">Не выбрано</option>{supportConversations.map((conversation) => <option key={conversation.id} value={conversation.id}>{conversation.subject || shortId(conversation.id)}</option>)}</select></label>
          <div className="list-stack mt-12">{supportMessages.slice(-12).map((message) => <div key={message.id} className="list-item-vertical"><div className="card-head"><strong>{message.direction}{message.isInternalNote ? ' · внутренняя заметка' : ''}</strong><span className="muted">{formatDate(message.createdAt)}</span></div><div>{message.text}</div></div>)}</div>
          <form hidden={!canWriteSection('support')} className="mt-12" aria-busy={actionBusyId === `support-reply-${selectedSupportConversationId}`} onSubmit={(event) => { event.preventDefault(); void handleReplySupport() }}>
            <label><span>Ответ пользователю</span><textarea value={supportReplyText} onChange={(e) => setSupportReplyText(e.target.value)} rows={3} placeholder="Текст ответа" /></label>
            <PrimaryButton type="submit" disabled={!selectedSupportConversationId || !supportReplyText.trim() || actionBusyId === `support-reply-${selectedSupportConversationId}`} aria-busy={actionBusyId === `support-reply-${selectedSupportConversationId}`}>Отправить через Telegram</PrimaryButton>
          </form>
          <form hidden={!canWriteSection('support')} className="mt-12" aria-busy={actionBusyId === `support-note-${selectedSupportConversationId}`} onSubmit={(event) => { event.preventDefault(); void handleSupportNote() }}>
            <label><span>Внутренняя заметка</span><textarea value={supportNoteText} onChange={(e) => setSupportNoteText(e.target.value)} rows={2} placeholder="Видно только администраторам" /></label>
            <PrimaryButton type="submit" disabled={!selectedSupportConversationId || !supportNoteText.trim() || actionBusyId === `support-note-${selectedSupportConversationId}`} aria-busy={actionBusyId === `support-note-${selectedSupportConversationId}`} className="button-secondary">Добавить заметку</PrimaryButton>
          </form>
        </Card>
      </div>

      <div id="bot" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('bot')} hidden={activeSection !== 'bot'}>
        <Card>
          <h3>Настройки Telegram-бота</h3>
          <div className="list-item-vertical">
            <div className="card-head"><strong>@{botSettings.publicBotUsername || 'не настроен'}</strong><StatusBadge value={botSettings.enabled ? 'Enabled' : 'Disabled'} /></div>
            <div className="muted">Режим {botSettings.mode === 'LongPolling' ? 'Опрос Telegram' : 'Webhook-уведомления'} · токен бота {botSettings.hasBotToken ? botSettings.botTokenMasked || 'скрыт' : 'пусто'} · секрет webhook {botSettings.hasSecretToken ? 'задан' : 'пусто'} · админский чат {botSettings.adminChatId || '—'}</div>
            <div className="muted">Webhook: {botSettings.webhookUrl || '—'} · WebApp: {botSettings.webAppUrl || '—'} · исходные токены никогда не возвращаются API.</div>
          </div>
          {botSettingsCheck && (
            <div className="list-item-vertical" role="status" aria-live="polite">
              <div className="card-head">
                <strong>Проверка подключения</strong>
                <StatusBadge value={botSettingsCheck.isReady ? 'Ready' : 'Needs configuration'} />
              </div>
              {botSettingsCheck.requiredActions.length > 0 && (
                <ul className="compact-list">
                  {botSettingsCheck.requiredActions.map((item) => <li key={item}>{item}</li>)}
                </ul>
              )}
              {botSettingsCheck.warnings.length > 0 && (
                <div className="muted">Предупреждения: {botSettingsCheck.warnings.join(' ')}</div>
              )}
              {botSettingsCheck.requiredActions.length === 0 && <div className="muted">Обязательные настройки заполнены. Можно проверять реальный диалог с ботом в Telegram.</div>}
            </div>
          )}
          <form aria-busy={actionBusyId === 'bot-settings'} onSubmit={(event) => { event.preventDefault(); void handleSaveBotSettings() }}>
            <fieldset className="form-section">
              <legend>Подключение Telegram</legend>
              <div className="form-grid">
                <label><span>Состояние</span><select value={botSettingsForm.enabled ? 'true' : 'false'} onChange={(e) => updateBotForm('enabled', e.target.value === 'true')}><option value="false">Выключен</option><option value="true">Включен</option></select></label>
                <label><span>Режим</span><select value={botSettingsForm.mode ?? 'LongPolling'} onChange={(e) => updateBotForm('mode', e.target.value)}><option value="LongPolling">Опрос Telegram</option><option value="Webhook">Webhook-уведомления</option></select></label>
                <label><span>Username публичного бота</span><input value={botSettingsForm.publicBotUsername ?? ''} onChange={(e) => updateBotForm('publicBotUsername', e.target.value)} placeholder="vpnplatform_bot" /></label>
                <label><span>URL webhook</span><input value={botSettingsForm.webhookUrl ?? ''} onChange={(e) => updateBotForm('webhookUrl', e.target.value)} placeholder="https://api.example.com/api/channels/telegram/webhook" type="url" inputMode="url" /></label>
                <label><span>ID админского чата</span><input value={botSettingsForm.adminChatId ?? ''} onChange={(e) => updateBotForm('adminChatId', e.target.value)} placeholder="-1001234567890" /></label>
                <label><span>URL WebApp</span><input value={botSettingsForm.webAppUrl ?? ''} onChange={(e) => updateBotForm('webAppUrl', e.target.value)} placeholder="https://cabinet.example.com" type="url" inputMode="url" /></label>
                <SecretField label="Токен бота" configured={botSettings.hasBotToken} value={botSettingsForm.botToken ?? ''} onChange={(value) => updateBotForm('botToken', value)} />
                <SecretField label="Секрет webhook" configured={botSettings.hasSecretToken} value={botSettingsForm.secretToken ?? ''} onChange={(value) => updateBotForm('secretToken', value)} />
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Тексты сценариев</legend>
              <label><span>Приветствие</span><textarea value={botSettingsForm.welcomeText ?? ''} onChange={(e) => updateBotForm('welcomeText', e.target.value)} rows={3} /></label>
              <label><span>Инструкция</span><textarea value={botSettingsForm.instructionText ?? ''} onChange={(e) => updateBotForm('instructionText', e.target.value)} rows={3} /></label>
              <label><span>Текст поддержки</span><textarea value={botSettingsForm.supportText ?? ''} onChange={(e) => updateBotForm('supportText', e.target.value)} rows={3} /></label>
              <label><span>Шаблон после оплаты</span><textarea value={botSettingsForm.afterPaymentTextTemplate ?? ''} onChange={(e) => updateBotForm('afterPaymentTextTemplate', e.target.value)} rows={3} /></label>
              <label><span>Шаблон продления</span><textarea value={botSettingsForm.renewalTextTemplate ?? ''} onChange={(e) => updateBotForm('renewalTextTemplate', e.target.value)} rows={3} /></label>
              <label><span>Шаблон ошибки оплаты</span><textarea value={botSettingsForm.paymentFailedTextTemplate ?? ''} onChange={(e) => updateBotForm('paymentFailedTextTemplate', e.target.value)} rows={3} /></label>
              <label><span>Шаблон окончания подписки</span><textarea value={botSettingsForm.subscriptionExpiredTextTemplate ?? ''} onChange={(e) => updateBotForm('subscriptionExpiredTextTemplate', e.target.value)} rows={3} /></label>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || actionBusyId === 'bot-settings'} title={adminDisabledTitle} aria-busy={actionBusyId === 'bot-settings'}>Сохранить настройки бота</PrimaryButton>
              <PrimaryButton className="button-secondary" type="button" disabled={!token || actionBusyId === 'bot-settings-test'} title={adminDisabledTitle} aria-busy={actionBusyId === 'bot-settings-test'} onClick={() => { void handleTestBotSettings() }}>Проверить подключение</PrimaryButton>
            </div>
          </form>
        </Card>
      </div>

      <div id="releases" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('releases')} hidden={activeSection !== 'releases'}>
        <Card>
          <h3>{editingReleaseId ? 'Редактировать релиз' : 'Создать релиз'}</h3>
          <p className="muted">Эти записи показываются пользователям в окне «Что нового» после входа в личный кабинет. Будущие даты публикации не показываются до наступления времени.</p>
          <form hidden={!canWriteSection('releases')} aria-busy={actionBusyId === 'release-create' || actionBusyId === `release-update-${editingReleaseId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveRelease() }}>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Release ID</span><input value={releaseForm.releaseId} onChange={(e) => updateReleaseForm('releaseId', e.target.value)} placeholder="2026-05-27-whats-new-module" required /></label>
                <label><span>Версия</span><input value={releaseForm.version} onChange={(e) => updateReleaseForm('version', e.target.value)} placeholder="0.2.0" required /></label>
                <label><span>Дата публикации</span><input value={toDateTimeLocalValue(releaseForm.releasedAt)} onChange={(e) => updateReleaseForm('releasedAt', fromDateTimeLocalValue(e.target.value))} type="datetime-local" required /></label>
                <label><span>Источник</span><select value={releaseForm.source ?? 'manual'} onChange={(e) => updateReleaseForm('source', e.target.value)}><option value="manual">Вручную</option><option value="agent">Агент</option></select></label>
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
          <div className="item-head">
            <div>
              <div className="muted">
                Всего: {appReleaseOverview?.totalCount ?? appReleases.length} · опубликовано: {appReleaseOverview?.publishedCount ?? 0} · запланировано: {appReleaseOverview?.upcomingCount ?? 0} · скрыто: {appReleaseOverview?.hiddenCount ?? 0}
              </div>
              <div className="muted">
                Последний релиз: {appReleaseOverview?.latestPublishedVersion ?? 'нет'} · просмотров: {appReleaseOverview?.seenCount ?? 0}
              </div>
            </div>
            <div className="item-status">
              <StatusBadge value={appReleaseOverview?.publishedCount ? 'Published' : 'Hidden'} />
              <StatusBadge value={`${appReleaseOverview?.agentCount ?? 0} от агента`} />
            </div>
          </div>
          {appReleaseOverview && appReleaseOverview.emptyReleaseIds.length > 0 && (
            <div className="safe-note">
              Релизы без пунктов: {appReleaseOverview.emptyReleaseIds.slice(0, 6).join(' · ')}{appReleaseOverview.emptyReleaseIds.length > 6 ? ' · ...' : ''}
            </div>
          )}
          <form className="toolbar toolbar-form" aria-label="Фильтры релизов" onSubmit={(event) => { event.preventDefault(); if (token) void loadAll(token) }}>
            <label><span>Поиск</span><input value={releaseSearch} onChange={(event) => setReleaseSearch(event.target.value)} placeholder="Версия, releaseId, заголовок" /></label>
            <label><span>Видимость</span><select value={releaseVisibilityFilter} onChange={(event) => setReleaseVisibilityFilter(event.target.value)}><option value="all">Все релизы</option><option value="published">Опубликованные</option><option value="upcoming">Запланированные</option><option value="hidden">Скрытые</option></select></label>
            <label><span>Источник</span><select value={releaseSourceFilter} onChange={(event) => setReleaseSourceFilter(event.target.value)}><option value="all">Все источники</option><option value="manual">Вручную</option><option value="agent">Агент</option></select></label>
            <PrimaryButton type="submit" disabled={!token || busy}>Применить</PrimaryButton>
          </form>
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
                  <div className="item-status"><StatusBadge value={release.isActive ? (new Date(release.releasedAt).getTime() > Date.now() ? 'Upcoming' : 'Published') : 'Hidden'} /><StatusBadge value={releaseSourceLabel(release.source)} /></div>
                </div>
                <div className="list-stack mt-12">
                  {release.items.map((item, index) => <div key={`${release.id}-${index}`} className="list-item"><span>{item.type}: {item.text}</span></div>)}
                </div>
                <div className="toolbar" hidden={!canWriteSection('releases')}>
                  <PrimaryButton className="button-secondary" onClick={() => editRelease(release)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `release-delete-${release.id}`} message={`Удалить релиз "${release.title}"? Пользователи больше не увидят его в истории.`} onConfirm={() => void handleDeleteRelease(release)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="faq" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('faq')} hidden={activeSection !== 'faq'}>
        <Card>
          <h3>{editingFaqId ? 'Редактировать вопрос' : 'Создать вопрос FAQ'}</h3>
          <p className="muted">Эти вопросы показываются на публичной странице FAQ. Неактивные записи остаются в админке, но скрываются от пользователей.</p>
          <form hidden={!canWriteSection('faq')} aria-busy={actionBusyId === 'faq-create' || actionBusyId === `faq-update-${editingFaqId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveFaq() }}>
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
          <div className="item-head">
            <div>
              <div className="muted">
                Всего: {faqOverview?.totalCount ?? faqEntries.length} · активных: {faqOverview?.activeCount ?? faqEntries.filter((entry) => entry.isActive !== false).length} · скрытых: {faqOverview?.hiddenCount ?? 0} · категорий: {faqOverview?.categoryCount ?? 0}
              </div>
              <div className="muted">
                На главной: {faqOverview?.homeCount ?? 0} · на странице FAQ: {faqOverview?.faqPageCount ?? 0}
              </div>
            </div>
            <div className="item-status">
              <StatusBadge value={faqOverview?.hasPublicFaq ? 'Published' : 'Hidden'} />
              <StatusBadge value={faqOverview?.hasHomeFaq ? 'Home' : 'Not configured'} />
            </div>
          </div>
          {faqOverview && faqOverview.duplicateQuestions.length > 0 && (
            <div className="safe-note">
              Дубли вопросов в категориях: {faqOverview.duplicateQuestions.slice(0, 6).join(' · ')}{faqOverview.duplicateQuestions.length > 6 ? ' · ...' : ''}
            </div>
          )}
          <form className="toolbar toolbar-form" aria-label="Фильтры FAQ" onSubmit={(event) => { event.preventDefault(); if (token) void loadAll(token) }}>
            <label><span>Поиск</span><input value={faqSearch} onChange={(event) => setFaqSearch(event.target.value)} placeholder="Вопрос, ответ или категория" /></label>
            <label><span>Категория</span><select value={faqCategoryFilter} onChange={(event) => setFaqCategoryFilter(event.target.value)}><option value="all">Все категории</option>{(faqOverview?.categories ?? []).map((category) => <option key={category} value={category}>{category}</option>)}</select></label>
            <label><span>Видимость</span><select value={faqVisibilityFilter} onChange={(event) => setFaqVisibilityFilter(event.target.value)}><option value="all">Все записи</option><option value="active">Активные</option><option value="hidden">Скрытые</option><option value="home">На главной</option><option value="faq">На странице FAQ</option></select></label>
            <PrimaryButton type="submit" disabled={!token || busy}>Применить</PrimaryButton>
          </form>
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
                <div className="toolbar" hidden={!canWriteSection('faq')}>
                  <PrimaryButton className="button-secondary" onClick={() => editFaq(entry)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `faq-delete-${entry.id}`} message={`Удалить вопрос "${entry.question}"?`} onConfirm={() => void handleDeleteFaq(entry)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="content" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('content')} hidden={activeSection !== 'content'}>
        <Card>
          <h3>{editingSiteContentId ? 'Редактировать блок контента' : 'Создать блок контента'}</h3>
          <p className="muted">Эти поля используются публичной главной страницей. Неактивные блоки остаются в админке, но не попадают в public API.</p>
          <div className="list-item-vertical">
            <div className="item-head">
              <div>
                <strong>Готовность главной страницы</strong>
                <div className="muted">
                  Обязательные блоки: {homeContentReadiness ? `${homeContentReadiness.activeRequiredCount}/${homeContentReadiness.requiredCount}` : 'проверка не выполнена'} · опубликовано: {homeContentReadiness?.publicBlocksCount ?? siteContentBlocks.filter((block) => block.isActive).length}
                </div>
              </div>
              <div className="item-status">
                <StatusBadge value={homeContentReadiness?.isReady ? 'Ready' : 'Attention'} />
                <StatusBadge value={`${homeContentIssueCount(homeContentReadiness)} проблем`} />
              </div>
            </div>
            {homeContentReadiness && homeContentIssueCount(homeContentReadiness) > 0 && (
              <div className="safe-note">
                {homeContentReadiness.missingKeys.length > 0 && <div>Нет блоков: {homeContentReadiness.missingKeys.slice(0, 8).join(' · ')}{homeContentReadiness.missingKeys.length > 8 ? ' · ...' : ''}</div>}
                {homeContentReadiness.inactiveKeys.length > 0 && <div>Выключены: {homeContentReadiness.inactiveKeys.slice(0, 8).join(' · ')}{homeContentReadiness.inactiveKeys.length > 8 ? ' · ...' : ''}</div>}
                {homeContentReadiness.emptyKeys.length > 0 && <div>Пустые значения: {homeContentReadiness.emptyKeys.slice(0, 8).join(' · ')}{homeContentReadiness.emptyKeys.length > 8 ? ' · ...' : ''}</div>}
                {homeContentReadiness.duplicateKeys.length > 0 && <div>Дубли ключей: {homeContentReadiness.duplicateKeys.slice(0, 8).join(' · ')}{homeContentReadiness.duplicateKeys.length > 8 ? ' · ...' : ''}</div>}
              </div>
            )}
            <div className="toolbar" hidden={!canWriteSection('content')}>
              <ConfirmButton
                className="button-secondary"
                disabled={!token || actionBusyId === 'content-restore-defaults'}
                message="Восстановить обязательные блоки главной? Недостающие блоки будут созданы, пустые или выключенные обязательные блоки получат безопасные значения по умолчанию."
                onConfirm={() => void handleRestoreHomeContentDefaults()}
              >
                Восстановить главную
              </ConfirmButton>
            </div>
          </div>
          <form hidden={!canWriteSection('content')} aria-busy={actionBusyId === 'content-create' || actionBusyId === `content-update-${editingSiteContentId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveSiteContent() }}>
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
                <div className="toolbar" hidden={!canWriteSection('content')}>
                  <PrimaryButton className="button-secondary" onClick={() => editSiteContent(block)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `content-delete-${block.id}`} message={`Удалить блок "${block.label}"? На сайте будет использован fallback-текст из приложения.`} onConfirm={() => void handleDeleteSiteContent(block)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="scenarios" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('scenarios')} hidden={activeSection !== 'scenarios'}>
        <Card>
          <h3>{editingWorkScenarioId ? 'Редактировать сценарий' : 'Создать сценарий работы'}</h3>
          <p className="muted">Сценарий описывает выдачу VPN после оплаты, поведение при ошибке, возврате, продлении и окончании подписки. Тариф выбирает сценарий по ключу.</p>
          <form hidden={!canWriteSection('scenarios')} aria-busy={actionBusyId === 'scenario-create' || actionBusyId === `scenario-update-${editingWorkScenarioId}`} onSubmit={(event) => { event.preventDefault(); void handleSaveWorkScenario() }}>
            <fieldset className="form-section">
              <legend>Основные параметры</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={workScenarioForm.name} onChange={(e) => updateWorkScenarioForm('name', e.target.value)} placeholder="Автоматическая выдача VPN" required /></label>
                <label><span>Ключ</span><input value={workScenarioForm.key} onChange={(e) => updateWorkScenarioForm('key', e.target.value)} placeholder="auto" required /></label>
                <label><span>VPN-протокол</span><input value={workScenarioForm.vpnProtocol} onChange={(e) => updateWorkScenarioForm('vpnProtocol', e.target.value)} placeholder="vless" /></label>
                <label><span>Режим выдачи</span><select value={workScenarioForm.provisioningMode} onChange={(e) => updateWorkScenarioForm('provisioningMode', e.target.value)}><option value="auto">Автоматически</option><option value="manual">Вручную</option><option value="hybrid">Гибридно</option></select></label>
                <label><span>Правило сервера</span><input value={workScenarioForm.serverSelectionRule} onChange={(e) => updateWorkScenarioForm('serverSelectionRule', e.target.value)} placeholder="least-loaded" /></label>
                <label><span>Правило inbound</span><input value={workScenarioForm.inboundSelectionRule} onChange={(e) => updateWorkScenarioForm('inboundSelectionRule', e.target.value)} placeholder="default" /></label>
                <label><span>Устройств</span><input value={workScenarioForm.maxDevices} onChange={(e) => updateWorkScenarioForm('maxDevices', Number(e.target.value) || 1)} type="number" min={1} step="1" /></label>
                <label><span>Лимит трафика, ГБ</span><input value={workScenarioForm.trafficLimit ? Math.round(workScenarioForm.trafficLimit / 1024 / 1024 / 1024) : ''} onChange={(e) => updateWorkScenarioForm('trafficLimit', e.target.value ? Number(e.target.value) * 1024 * 1024 * 1024 : null)} type="number" min={0} step="1" placeholder="Без лимита" /></label>
                <label><span>Порядок</span><input value={workScenarioForm.sortOrder} onChange={(e) => updateWorkScenarioForm('sortOrder', Number(e.target.value) || 0)} type="number" step="1" /></label>
              </div>
              <div className="scenario-tariff-picker">
                <span className="form-label">Тарифы, которым разрешен сценарий</span>
                <div className="checkbox-list">
                  {tariffs.length === 0 && <p className="muted">Сначала создайте тарифы. Без выбранных тарифов сценарий будет доступен для всех тарифов.</p>}
                  {tariffs.map((tariff) => (
                    <label key={tariff.id} className="checkbox-row">
                      <input checked={isWorkScenarioTariffSelected(tariff.id)} onChange={(e) => updateWorkScenarioTariffLink(tariff.id, e.target.checked)} type="checkbox" />
                      <span>{tariff.name}</span>
                      <small>{tariff.slug}</small>
                    </label>
                  ))}
                </div>
                <small>Если ничего не выбрано, сценарий доступен для всех тарифов. Тариф также может выбрать основной сценарий в своем редакторе.</small>
              </div>
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
            <FormValidationSummary errors={workScenarioFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || !!actionBusyId || workScenarioFormErrors.length > 0} title={adminDisabledTitle}>{editingWorkScenarioId ? 'Сохранить сценарий' : 'Создать сценарий'}</PrimaryButton>
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
                    <div className="muted">{scenario.key} · {scenario.vpnProtocol} · {provisioningModeLabel(scenario.provisioningMode)} · сервер {scenario.serverSelectionRule}</div>
                    <div className="muted">Оплата: {scenario.onPaymentSucceeded} · продление: {scenario.onRenewal}</div>
                    <div className="muted">Тарифы: {tariffs.filter((tariff) => tariff.provisioningScenario === scenario.key).map((tariff) => tariff.name).join(', ') || 'не выбраны'}</div>
                  </div>
                  <div className="item-status"><StatusBadge value={scenario.isActive ? 'Active' : 'Hidden'} /><StatusBadge value={scenario.generateQrCode ? 'QR' : 'No QR'} /></div>
                </div>
                <div className="toolbar" hidden={!canWriteSection('scenarios')}>
                  <PrimaryButton className="button-secondary" onClick={() => editWorkScenario(scenario)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={actionBusyId === `scenario-delete-${scenario.id}`} message={`Удалить сценарий "${scenario.name}"? Если он выбран в тарифе, API не даст удалить его.`} onConfirm={() => void handleDeleteWorkScenario(scenario)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="provisioning" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('provisioning')} hidden={activeSection !== 'provisioning'}>
        <Card>
          <h3>Подготовка VPS</h3>
          <p className="safe-note">В проверочном режиме реальный SSH/Ansible-деплой выключен, пока это явно не разрешено настройками сервера.</p>
          <div className="list-stack">
            {provisioningRuns.length === 0 && <EmptyState title="Запусков подготовки нет" description="Проверки и подготовки VPS появятся здесь после Telegram или админ-сценария." />}
            {provisioningRuns.slice(0, 12).map((run) => (
              <div key={run.id} className="list-item-vertical">
                <div className="item-head">
                  <strong>{run.nodeName || shortId(run.nodeId)}</strong>
                  <div className="item-status"><StatusBadge value={run.status} /><StatusBadge value={provisioningRiskBadge(run.riskLevel)} /></div>
                </div>
                <div className="muted">Запуск: {shortId(run.id)} · источник {run.source || '—'} · владелец {run.owner || '—'} · шаг {run.currentStep || run.status}</div>
                <div className="muted">Цель: {run.targetHost || shortId(run.nodeId)}:{run.sshPort ?? 22} · пользователь {run.username || 'root'} · авторизация {run.authMethod || '—'} · доступы {run.credentialsConfigured ? 'заданы' : 'не заданы'} · {run.validationMode ? 'validation node' : 'live candidate'}</div>
                <div className="muted">Режим запуска: {run.modeTitle || provisioningDeployModeLabel(run.mode)} · риск {provisioningRiskLabel(run.riskLevel)} · live deploy {run.liveDeployAllowed ? 'разрешён' : 'закрыт'} · {run.nextAction || 'проверьте результат перед следующим действием'}</div>
                <div className="muted">Следующий deploy: {run.deployModeTitle || provisioningDeployModeLabel(run.deployMode)} · риск {provisioningRiskLabel(run.deployRiskLevel)} · {run.deployNextAction || 'сначала выполните precheck'}</div>
                <div className="muted">{run.dryRun ? 'проверка без изменений' : 'развертывание'} · старт {formatDate(run.startedAt)} · финиш {formatDate(run.finishedAt)}</div>
                {(run.attemptCount ?? 0) > 0 && <div className="muted">Попытка {run.attemptCount} · обработка {formatDate(run.processingStartedAt)} · lease до {formatDate(run.leaseExpiresAt)}</div>}
                {run.operatorWarning && <div className="safe-note">{run.operatorWarning}</div>}
                {run.precheckReportPreview && <pre className="safe-note">{run.precheckReportPreview}</pre>}
                <div className="muted">{run.lastError || run.errorSummary || run.executionLogPreview || run.executionLog || '—'}</div>
                <div className="toolbar" hidden={!canWriteSection('provisioning')}>
                  <PrimaryButton disabled={!token || actionBusyId === `retry-${run.id}` || !canRetryProvisioningRun(run.status)} onClick={() => void handleRetryProvisioningRun(run.id)}>Повторить</PrimaryButton>
                  <ConfirmButton disabled={!token || actionBusyId === `deploy-run-${run.id}` || !['ReadyToDeploy', 'Succeeded'].includes(run.status) || run.deployMode === 'live-deploy-blocked'} className="button-danger" message={`Развернуть VPS? Режим: ${run.deployModeTitle || provisioningDeployModeLabel(run.deployMode)}. ${run.deployOperatorWarning || run.operatorWarning || 'В live-режиме это может выполнить реальные SSH/Ansible-действия.'}`} onConfirm={() => void handleDeployProvisioningRun(run.id)}>Развернуть</ConfirmButton>
                  <ConfirmButton disabled={!token || actionBusyId === `cancel-run-${run.id}` || !canCancelProvisioningRun(run.status)} className="button-secondary" message="Отменить запуск подготовки VPS?" onConfirm={() => void handleCancelProvisioningRun(run.id)}>Отменить</ConfirmButton>
                  <PrimaryButton disabled={!token || actionBusyId === `support-run-${run.id}`} onClick={() => void handleProvisioningSupportNeeded(run.id)}>Нужна поддержка</PrimaryButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>
        </div>
      </div>
    </PageShell>
  )
}
